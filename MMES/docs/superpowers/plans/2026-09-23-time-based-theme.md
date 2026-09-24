# Time-based Light/Dark Theme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the 6 dashboard/business pages (Home, Quality x3, SMT x2) switch automatically between a light theme (6:00–17:59 local browser time) and the existing dark theme (18:00–5:59), with no manual toggle, no flash of the wrong theme on load, and no text that becomes unreadable against its background in either theme.

**Architecture:** A single shared token file (`wwwroot/css/theme.css`) declares every themed color as a CSS custom property, light values under `:root` (default) and dark values under `:root[data-theme="dark"]` (copied unchanged from today's palette). A small script (`wwwroot/js/theme-clock.js`) reads the browser's local hour and sets `data-theme` on `<html>`, re-checked every 60s. Each in-scope page loads an inline blocking snippet in `<head>` (sets `data-theme` before first paint) plus `theme.css`, and `theme-clock.js` before `</body>` (owns the 60s re-check). `dashboard.css`, `quality-block-release.css`, `production-rate.css`, and `oqc-scanout-matrix.css` are refactored so every hardcoded color becomes `var(--token)`.

**Tech Stack:** ASP.NET Core MVC (Razor views, `Layout = null` per page), plain CSS custom properties, vanilla JS (no bundler, no framework). No test framework exists in this repo (no `.csproj` test project, no `package.json`); verification uses Node.js (`node`, confirmed present, v24.11.1) for ad-hoc logic checks and manual browser checks for visuals.

**Spec:** `docs/superpowers/specs/2026-09-23-time-based-theme-design.md`

## Global Constraints

- Theme window: local browser hour `>= 6 and < 18` → light; otherwise → dark. No manual override control.
- `Views/Account/Login.cshtml` / `wwwroot/css/account.css` / `wwwroot/css/site.css` / `Views/Shared/_Layout.cshtml` are out of scope — do not modify them.
- Dark mode must render pixel-identical to today after the refactor (dark token values are copied verbatim from current hardcoded colors).
- All UI copy (labels, headings, buttons, aria-labels, validation/tooltip text) must stay in English; input `placeholder` text stays in Mexican Spanish (es-MX). This task adds no new UI copy, but if any step needs a new attribute/label, follow this rule.
- Every primary-text/background and label/background pairing must meet WCAG AA (4.5:1 normal text, 3:1 large text/borders) — the token values below were chosen to satisfy this; do not substitute ad-hoc colors during implementation.

---

## File Structure

| File | Responsibility |
|---|---|
| `wwwroot/css/theme.css` (new) | All themed design tokens (light default, dark override) + the handful of unthemed tokens (radius, fonts). Loaded by every in-scope page, before the page's own stylesheet(s). |
| `wwwroot/js/theme-clock.js` (new) | Pure `computeTheme(hour)` / `getLocalHour()` logic + DOM wiring (`applyTheme`, 60s interval). Loaded near the end of `<body>` on every in-scope page. |
| `wwwroot/css/dashboard.css` (modified) | Drop its private `:root` token block; every hardcoded color becomes `var(--token)` from `theme.css`. Loaded by all 6 in-scope pages. |
| `wwwroot/css/quality-block-release.css` (modified) | Same refactor, page-specific to `BlockRelease.cshtml`. |
| `wwwroot/css/production-rate.css` (modified) | Same refactor, page-specific to `ProductionRate.cshtml`. |
| `wwwroot/css/oqc-scanout-matrix.css` (modified) | Same refactor for consistency, even though no view currently links this file. |
| `Views/Home/Index.cshtml`, `Views/Quality/Index.cshtml`, `Views/Quality/BlockRelease.cshtml`, `Views/Quality/OqcScanout.cshtml`, `Views/Smt/Index.cshtml`, `Views/Smt/ProductionRate.cshtml` (modified) | Add the theme wiring block to `<head>` and the clock script before `</body>`. |

---

### Task 1: `theme-clock.js` — time → theme resolution

**Files:**
- Create: `wwwroot/js/theme-clock.js`

**Interfaces:**
- Consumes: nothing.
- Produces: `computeTheme(hour: number): 'light' | 'dark'`, `getLocalHour(): number` (both attached as plain functions in the script, and additionally exported via `module.exports` when running under Node so they can be verified with `node`). Side effect in a browser: sets `document.documentElement.dataset.theme`.

- [ ] **Step 1: Write the failing verification command**

Run (this will fail because the file doesn't exist yet):

```bash
node -e "const {computeTheme} = require('./wwwroot/js/theme-clock.js'); console.log(computeTheme(6));"
```

Expected: FAIL with `Error: Cannot find module './wwwroot/js/theme-clock.js'`

- [ ] **Step 2: Write the implementation**

```js
function getLocalHour() {
  return new Date().getHours();
}

function computeTheme(hour) {
  return (hour >= 6 && hour < 18) ? 'light' : 'dark';
}

function applyTheme() {
  document.documentElement.setAttribute('data-theme', computeTheme(getLocalHour()));
}

if (typeof document !== 'undefined') {
  applyTheme();
  setInterval(applyTheme, 60000);
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = { computeTheme, getLocalHour };
}
```

- [ ] **Step 3: Run the boundary-condition check**

```bash
node -e "
const {computeTheme} = require('./wwwroot/js/theme-clock.js');
const cases = [[0,'dark'],[5,'dark'],[6,'light'],[12,'light'],[17,'light'],[18,'dark'],[23,'dark']];
let ok = true;
for (const [hour, expected] of cases) {
  const actual = computeTheme(hour);
  if (actual !== expected) { ok = false; console.error('FAIL hour='+hour+' expected='+expected+' actual='+actual); }
}
console.log(ok ? 'ALL PASS' : 'FAILURES ABOVE');
process.exit(ok ? 0 : 1);
"
```

Expected: prints `ALL PASS`, exit code 0. This checks both boundaries (6 and 18) land on the correct side.

- [ ] **Step 4: Commit**

```bash
git add wwwroot/js/theme-clock.js
git commit -m "feat: add time-based theme resolution script"
```

---

### Task 2: `theme.css` — design tokens

**Files:**
- Create: `wwwroot/css/theme.css`

**Interfaces:**
- Consumes: nothing.
- Produces: every CSS custom property listed below, consumed by Tasks 3, 5, 6, 7 and by all 6 wired pages.

- [ ] **Step 1: Write the failing verification command**

```bash
node -e "require('fs').readFileSync('wwwroot/css/theme.css','utf8')"
```

Expected: FAIL with `Error: ENOENT: no such file or directory`

- [ ] **Step 2: Write `theme.css`**

```css
/* ============================================================
   Design tokens: light theme (default) / dark theme override.
   Dark values are unchanged from the pre-existing dashboard.css
   :root block; light values are new, tuned for WCAG AA contrast
   against a white/near-white surface.
   ============================================================ */
:root {
  /* surfaces */
  --bg-app: #F5F6F8;
  --bg-app-rgb: 245,246,248;
  --bg-surface: #FFFFFF;
  --bg-surface-rgb: 255,255,255;
  --bg-surface-inset: #F0F2F5;
  --bg-surface-inset-rgb: 240,242,245;
  --border-subtle: #E2E5EA;
  --border-strong: #C7CCD4;

  /* text */
  --text-primary: #12161C;
  --text-secondary: #5B6472;
  --text-secondary-rgb: 91,100,114;
  --text-tertiary: #838B99;

  /* brand / accents */
  --accent-gold: #9A6A22;
  --accent-gold-rgb: 154,106,34;
  --accent-gold-soft: rgba(154,106,34,.10);
  --accent-blue: #0B72B9;
  --accent-blue-rgb: 11,114,185;

  /* status */
  --danger: #B42318;
  --danger-rgb: 180,35,24;
  --danger-soft: rgba(180,35,24,.08);
  --danger-strong: #C4281A;
  --success: #157F5C;
  --success-rgb: 21,127,92;
  --success-soft: rgba(21,127,92,.08);
  --warning: #92600A;
  --warning-soft: rgba(146,96,10,.10);

  /* effects */
  --hover-tint-rgb: 18,22,28;
  --overlay-scrim: rgba(255,255,255,0.98);
  --hero-image-filter: grayscale(15%) brightness(1.05);
  --hero-overlay-gradient:
    radial-gradient(1100px 560px at 82% -8%, rgba(154,106,34,.10), transparent 60%),
    linear-gradient(165deg, rgba(255,255,255,.85) 0%, rgba(245,246,248,.92) 45%, rgba(245,246,248,.97) 100%);
  --card-image-overlay-gradient:
    linear-gradient(180deg, rgba(255,255,255,0.15) 0%, rgba(255,255,255,0.92) 100%);

  /* unthemed — identical in both modes */
  --text-on-accent: #0E1117;
  --radius: 6px;
  --font-display: 'Segoe UI Semibold','Segoe UI',Arial,sans-serif;
  --font-body: 'Segoe UI',Arial,system-ui,sans-serif;
  --font-mono: Consolas,'Courier New',monospace;
}

:root[data-theme="dark"] {
  --bg-app: #0B0C0E;
  --bg-app-rgb: 11,12,14;
  --bg-surface: #161B22;
  --bg-surface-rgb: 22,27,34;
  --bg-surface-inset: #0E1117;
  --bg-surface-inset-rgb: 14,17,23;
  --border-subtle: #21262D;
  --border-strong: #30363D;

  --text-primary: #F0F6FC;
  --text-secondary: #8B949E;
  --text-secondary-rgb: 139,148,158;
  --text-tertiary: #6E7681;

  --accent-gold: #D9A65A;
  --accent-gold-rgb: 217,166,90;
  --accent-gold-soft: rgba(217,166,90,.12);
  --accent-blue: #38BDF8;
  --accent-blue-rgb: 56,189,248;

  --danger: #F85149;
  --danger-rgb: 248,81,73;
  --danger-soft: rgba(198,67,60,.16);
  --danger-strong: #FF6B63;
  --success: #4FB6A0;
  --success-rgb: 79,182,160;
  --success-soft: rgba(79,182,160,.16);
  --warning: #FBBF24;
  --warning-soft: rgba(245,158,11,.15);

  --hover-tint-rgb: 255,255,255;
  --overlay-scrim: rgba(22,27,34,0.98);
  --hero-image-filter: grayscale(35%) brightness(.4);
  --hero-overlay-gradient:
    radial-gradient(1100px 560px at 82% -8%, rgba(217,166,90,.14), transparent 60%),
    linear-gradient(165deg, rgba(43,46,51,.94) 0%, rgba(30,32,36,.95) 45%, rgba(11,12,14,.97) 100%);
  --card-image-overlay-gradient:
    linear-gradient(180deg, rgba(22,27,34,0.2) 0%, rgba(22,27,34,0.95) 100%);
}
```

- [ ] **Step 3: Run the token-parity check**

This confirms every themed token declared in the light block has a dark override (and vice versa) — the class of bug this catches is a typo'd or forgotten override that silently falls back to the light value inside `:root[data-theme="dark"]`.

```bash
node -e "
const fs = require('fs');
const css = fs.readFileSync('wwwroot/css/theme.css', 'utf8');
const unthemed = ['text-on-accent','radius','font-display','font-body','font-mono','text-on-danger'];
function names(re) {
  const m = css.match(re);
  if (!m) throw new Error('block not found for ' + re);
  return [...m[1].matchAll(/--([a-z0-9-]+)\s*:/g)].map(x => x[1]).filter(n => !unthemed.includes(n));
}
const light = names(/:root\s*{([\s\S]*?)}/);
const dark = names(/:root\[data-theme=\"dark\"\]\s*{([\s\S]*?)}/);
const missingInDark = light.filter(t => !dark.includes(t));
const extraInDark = dark.filter(t => !light.includes(t));
if (missingInDark.length || extraInDark.length) {
  console.error('MISMATCH missing-in-dark:', missingInDark, 'extra-in-dark:', extraInDark);
  process.exit(1);
}
console.log('PARITY OK,', light.length, 'themed tokens');
"
```

Expected: prints `PARITY OK, 31 themed tokens`, exit code 0.

- [ ] **Step 4: Commit**

```bash
git add wwwroot/css/theme.css
git commit -m "feat: add light/dark design token stylesheet"
```

---

### Task 3: Wire `Home/Index.cshtml` + refactor `dashboard.css`

**Files:**
- Modify: `Views/Home/Index.cshtml`
- Modify: `wwwroot/css/dashboard.css` (currently 742 lines; delete lines 1–23, the private `:root` block, and replace every hardcoded color with the matching token)
- Modify: `Views/Shared/_DashboardHeader.cshtml` — no change needed (it's a partial rendered inside the host page's `<html>`, so it inherits `data-theme` automatically)

**Interfaces:**
- Consumes: `wwwroot/css/theme.css` tokens from Task 2, `wwwroot/js/theme-clock.js` from Task 1.
- Produces: the reference pattern (head wiring block + refactored stylesheet) that Tasks 4–7 repeat.

- [ ] **Step 1: Add the theme wiring block to `Views/Home/Index.cshtml`'s `<head>`**

In `Views/Home/Index.cshtml`, insert this immediately before the existing `<link rel="stylesheet" href="~/css/dashboard.css" ... />` line:

```html
    <script>
        (function () {
            var hour = new Date().getHours();
            document.documentElement.setAttribute('data-theme', (hour >= 6 && hour < 18) ? 'light' : 'dark');
        })();
    </script>
    <link rel="stylesheet" href="~/css/theme.css" asp-append-version="true" />
```

And immediately before the closing `</body>` tag, add:

```html
    <script src="~/js/theme-clock.js" asp-append-version="true"></script>
```

- [ ] **Step 2: Run the failing "stale literal" check against `dashboard.css`**

```bash
node -e "
const fs = require('fs');
const css = fs.readFileSync('wwwroot/css/dashboard.css', 'utf8').toLowerCase();
const stale = ['#d9a65a','#8b949e','#f0f6fc','#30363d','#21262d','#38bdf8','#161b22','#0e1117',
  '#ffffff','#f85149','#f2f1ec','#c9d1d9','#c6433c','#b8863a','#9a9ca3','#6e7681','#6b6e74',
  '#4fb6a0','#484f58','#2b2e33','#1e2024','#0b0c0e','rgba(217, 166, 90','rgba(184,134,58',
  'rgba(255,255,255,.055','rgba(255,255,255,.09','rgba(255,255,255,.12','rgba(255,255,255,.22',
  'rgba(255, 255, 255, 0.04)','rgba(14, 17, 23, 0.85)','rgba(56, 189, 248','rgba(139, 148, 158',
  'rgba(22, 27, 34'];
const found = stale.filter(s => css.includes(s.toLowerCase()));
console.log(found.length ? 'STALE: ' + found.join(', ') : 'CLEAN');
process.exit(found.length ? 1 : 0);
"
```

Expected: FAIL, printing the full `STALE: ...` list (every literal below is still present before the refactor).

- [ ] **Step 3: Refactor `dashboard.css`**

Delete lines 1–23 (the old `:root { --ink: ...; ... }` block — all of its color tokens are superseded by `theme.css`; `--radius`/`--font-*` now come from there too).

Then apply this exact replacement table. `var(--ink)` and `var(--text-on-dark)` refer to the two usages on `html`/`body.dashboard-page` that consumed the deleted local tokens.

| Literal(s) in file | Replace with |
|---|---|
| `var(--ink)` (×2: `html { background: ... }`, `body.dashboard-page { background: ... }`) | `var(--bg-app)` |
| `var(--text-on-dark)` (×1: `body.dashboard-page { color: ... }`) | `var(--text-primary)` |
| `#D9A65A` (all 17 occurrences) | `var(--accent-gold)` |
| `#8B949E` (all 13) | `var(--text-secondary)` |
| `#F0F6FC` (all 12) | `var(--text-primary)` |
| `#30363D` (all 11) | `var(--border-strong)` |
| `#21262D` (all 9) | `var(--border-subtle)` |
| `#38BDF8` (all 6) | `var(--accent-blue)` |
| `#161B22` (all 6) | `var(--bg-surface)` |
| `#0E1117` used as a `background` (5 of 6 occurrences) | `var(--bg-surface-inset)` |
| `#0E1117` used as `color` inside `.dept-action-btn.primary:hover` (the dark ink text sitting on the solid gold button fill) | `var(--text-on-accent)` — do **not** use `--bg-surface-inset` here, it becomes near-white in light mode and would make this text invisible against the gold button |
| `#FFFFFF` in `.dept-dropdown.is-open .dept-dropdown-trigger { color: ... }` | `var(--text-primary)` — this text sits on a translucent gold tint over the header surface, not a solid fill; a literal white would be unreadable on the light-mode white header |
| `#FFFFFF` in `.dept-card.is-featured .dept-monogram { color: ... }` | `var(--text-primary)` — same reasoning (translucent `rgba(217,166,90,0.2)` tint, not a solid fill) |
| `#F85149` (`.logout-btn:hover { color: ... }`) | `var(--danger)` |
| `#C9D1D9` (`.hero-motto { color: ... }`) | `var(--text-secondary)` |
| `#6E7681` (`.eyebrow-sub`-adjacent selector, `color: ...`) | `var(--text-tertiary)` |
| `#484F58` (`.dept-action-btn.disabled { color: ... }`) | `var(--text-tertiary)` |
| `rgba(217, 166, 90, 0.04)` / `0.08` / `0.12` / `0.15` / `0.2` / `0.25` / `0.3` / `0.4` (every occurrence) | `rgba(var(--accent-gold-rgb), <same alpha>)` |
| `rgba(56, 189, 248, 0.12)` / `0.3` | `rgba(var(--accent-blue-rgb), <same alpha>)` |
| `rgba(139, 148, 158, 0.1)` / `0.2` | `rgba(var(--text-secondary-rgb), <same alpha>)` |
| `rgba(255, 255, 255, 0.04)` (`.nav-tab:hover { background: ... }`) | `rgba(var(--hover-tint-rgb), 0.04)` |
| `rgba(14, 17, 23, 0.85)` (`.dept-monogram { background: ... }`) | `rgba(var(--bg-surface-inset-rgb), 0.85)` |
| `.card-image-overlay { background: linear-gradient(180deg, rgba(22, 27, 34, 0.2) 0%, rgba(22, 27, 34, 0.95) 100%); }` | `background: var(--card-image-overlay-gradient);` |
| `body.dashboard-page::before { ... filter: grayscale(35%) brightness(.4); }` | `filter: var(--hero-image-filter);` |
| `body.dashboard-page::after { ... background: radial-gradient(1100px 560px at 82% -8%, rgba(184,134,58,.14), transparent 60%), linear-gradient(165deg, rgba(43,46,51,.94) 0%, rgba(30,32,36,.95) 45%, rgba(11,12,14,.97) 100%); }` | `background: var(--hero-overlay-gradient);` |
| `rgba(0, 0, 0, 0.4)`, `rgba(0, 0, 0, 0.6)`, `rgba(0,0,0,0.3)`, `rgba(0,0,0,0.5)` (all box-shadow uses) | leave unchanged — pure black drop-shadows read correctly under both themes |

- [ ] **Step 4: Re-run the "stale literal" check**

Run the same command from Step 2.

Expected: prints `CLEAN`, exit code 0.

- [ ] **Step 5: Manual browser verification**

Start the app (`dotnet run` or via your usual `start_mes.bat`), sign in, open the Home dashboard.

In the browser devtools console, force each theme and confirm no flash on load and no low-contrast text:

```js
document.documentElement.setAttribute('data-theme', 'light');
```
```js
document.documentElement.setAttribute('data-theme', 'dark');
```

Check specifically: header nav-tab labels, the SMT/QUALITY dropdown menu when open, hero title/description/motto, the 4 fact-card numbers, department card text and buttons, plant-status-bar metrics. Confirm dark mode looks unchanged from before this task.

- [ ] **Step 6: Commit**

```bash
git add Views/Home/Index.cshtml wwwroot/css/dashboard.css
git commit -m "feat: wire time-based theme into Home dashboard and tokenize dashboard.css"
```

---

### Task 4: Wire the remaining 5 pages

**Files:**
- Modify: `Views/Quality/Index.cshtml`
- Modify: `Views/Quality/BlockRelease.cshtml`
- Modify: `Views/Quality/OqcScanout.cshtml`
- Modify: `Views/Smt/Index.cshtml`
- Modify: `Views/Smt/ProductionRate.cshtml`

**Interfaces:**
- Consumes: `theme.css` (Task 2), `theme-clock.js` (Task 1), tokenized `dashboard.css` (Task 3).

`dashboard.css` is already tokenized, so these 5 pages only need the same wiring block Task 3 added to Home/Index — no CSS changes in this task.

- [ ] **Step 1: Wire each page**

For each of the 5 files, insert the same inline script + `theme.css` link used in Task 3, immediately before that file's existing `<link rel="stylesheet" href="~/css/dashboard.css" ... />` line, and the `theme-clock.js` include immediately before `</body>`:

```html
    <script>
        (function () {
            var hour = new Date().getHours();
            document.documentElement.setAttribute('data-theme', (hour >= 6 && hour < 18) ? 'light' : 'dark');
        })();
    </script>
    <link rel="stylesheet" href="~/css/theme.css" asp-append-version="true" />
```

```html
    <script src="~/js/theme-clock.js" asp-append-version="true"></script>
```

(`Views/Quality/OqcScanout.cshtml` already has an inline `<script>` block for a Tailwind CDN console-warning suppressor right after its `dashboard.css` link — put the theme wiring block *before* the existing `dashboard.css` link, same as the other pages, leaving the Tailwind suppressor script untouched.)

- [ ] **Step 2: Run the failing verification**

```bash
node -e "
const fs = require('fs');
const files = [
  'Views/Quality/Index.cshtml',
  'Views/Quality/BlockRelease.cshtml',
  'Views/Quality/OqcScanout.cshtml',
  'Views/Smt/Index.cshtml',
  'Views/Smt/ProductionRate.cshtml'
];
const missing = files.filter(f => !fs.readFileSync(f, 'utf8').includes('css/theme.css'));
console.log(missing.length ? 'MISSING WIRING: ' + missing.join(', ') : 'ALL WIRED');
process.exit(missing.length ? 1 : 0);
"
```

Run this once before Step 1 (expect `MISSING WIRING: ...` listing all 5 files) and again after (expect `ALL WIRED`).

- [ ] **Step 3: Manual browser verification**

For each of the 5 pages, sign in and navigate to it, then repeat the light/dark console check from Task 3 Step 5. Confirm no flash-of-wrong-theme and that all visible text (page-specific content is still unstyled by tokens at this point, since the page-specific stylesheets aren't refactored until Tasks 5–6 — only `dashboard.css`-styled chrome like the header and plant-status-bar should already look correct in both themes here).

- [ ] **Step 4: Commit**

```bash
git add Views/Quality/Index.cshtml Views/Quality/BlockRelease.cshtml Views/Quality/OqcScanout.cshtml Views/Smt/Index.cshtml Views/Smt/ProductionRate.cshtml
git commit -m "feat: wire time-based theme into Quality and SMT pages"
```

---

### Task 5: Refactor `quality-block-release.css`

**Files:**
- Modify: `wwwroot/css/quality-block-release.css`

**Interfaces:**
- Consumes: `theme.css` tokens from Task 2. Page wiring already done in Task 4.

- [ ] **Step 1: Run the failing "stale literal" check**

```bash
node -e "
const fs = require('fs');
const css = fs.readFileSync('wwwroot/css/quality-block-release.css', 'utf8').toLowerCase();
const stale = ['#f0f6fc','#30363d','#8b949e','#d9a65a','#0e1117','#38bdf8','#f85149','#21262d',
  '#161b22','#c9d1d9','#6e7681','#ffffff','#ff7b72','#ff6b63','#e5b770','#7ee7fc',
  'rgba(217, 166, 90','rgba(22, 27, 34, 0.98','rgba(248, 81, 73','rgba(255, 255, 255, 0.03','rgba(56, 189, 248'];
const found = stale.filter(s => css.includes(s.toLowerCase()));
console.log(found.length ? 'STALE: ' + found.join(', ') : 'CLEAN');
process.exit(found.length ? 1 : 0);
"
```

Expected: FAIL, listing all the literals below.

- [ ] **Step 2: Refactor**

| Literal(s) | Replace with |
|---|---|
| `#F0F6FC` (all 10) | `var(--text-primary)` |
| `#30363D` (all 10) | `var(--border-strong)` |
| `#8B949E` (all 9) | `var(--text-secondary)` |
| `#D9A65A` (all 7) | `var(--accent-gold)` |
| `#0E1117` (all 7) | `var(--bg-surface-inset)` — **except** if any occurrence is `color` text sitting directly on a solid `var(--accent-gold)` fill (same pattern as Task 3): use `var(--text-on-accent)` there instead. Check each usage's surrounding rule before replacing. |
| `#38BDF8` (all 5) | `var(--accent-blue)` |
| `#F85149` (all 4) | `var(--danger)` |
| `#21262D` (all 4) | `var(--border-subtle)` |
| `#161B22` (all 4) | `var(--bg-surface)` |
| `#C9D1D9` (all 3) | `var(--text-secondary)` |
| `#6E7681` (all 3) | `var(--text-tertiary)` |
| `#FFFFFF` (1) | `var(--text-primary)` unless it's text on a solid accent fill (check context, same rule as above) — use `var(--text-on-accent)` in that case |
| `#FF7B72`, `#FF6B63` (1 each) | `var(--danger-strong)` |
| `#E5B770` (1) | `var(--accent-gold)` |
| `#7EE7FC` (1) | `var(--accent-blue)` |
| `rgba(217, 166, 90, 0.15)` / `0.25` | `rgba(var(--accent-gold-rgb), <same alpha>)` |
| `rgba(22, 27, 34, 0.98)` (full-opacity scrim/overlay background) | `var(--overlay-scrim)` |
| `rgba(248, 81, 73, 0.05)` / `0.08` / `0.15` / `0.2` / `0.3` / `0.4` | `rgba(var(--danger-rgb), <same alpha>)` |
| `rgba(255, 255, 255, 0.03)` | `rgba(var(--hover-tint-rgb), 0.03)` |
| `rgba(56, 189, 248, 0.03)` / `0.08` / `0.15` / `0.2` / `0.4` | `rgba(var(--accent-blue-rgb), <same alpha>)` |
| `rgba(0, 0, 0, 0.3)`, `rgba(0, 0, 0, 0.6)` | leave unchanged |

- [ ] **Step 3: Re-run the check from Step 1**

Expected: `CLEAN`, exit code 0.

- [ ] **Step 4: Manual browser verification**

Sign in, open Quality → Block/Release Production. Toggle `data-theme` between `'light'` and `'dark'` in devtools console as in Task 3 Step 5. Check the PID lock/unlock table, toast notifications, status badges, and form inputs for legible text in both themes.

- [ ] **Step 5: Commit**

```bash
git add wwwroot/css/quality-block-release.css
git commit -m "feat: tokenize quality-block-release.css for light/dark theme"
```

---

### Task 6: Refactor `production-rate.css`

**Files:**
- Modify: `wwwroot/css/production-rate.css`

**Interfaces:**
- Consumes: `theme.css` tokens from Task 2. Page wiring already done in Task 4.

- [ ] **Step 1: Run the failing "stale literal" check**

```bash
node -e "
const fs = require('fs');
const css = fs.readFileSync('wwwroot/css/production-rate.css', 'utf8').toLowerCase();
const stale = ['#d9a65a','#fff','#9a9ca3','#f85149','#ff6b6b','#c9d1d9','#4fb6a0','#30363d',
  '#0e1117','#f3e5ab','#f0f6fc','#38bdf8','#21262d','#161b22',
  'rgba(14, 18, 22, 0.6','rgba(18, 22, 28, 0.9','rgba(22, 26, 32, 0.75',
  'rgba(184, 134, 58','rgba(217, 166, 90','rgba(220, 53, 69','rgba(248, 81, 73, 0.8',
  'rgba(255, 255, 255,','rgba(255,255,255,'];
const found = stale.filter(s => css.includes(s.toLowerCase()));
console.log(found.length ? 'STALE: ' + found.join(', ') : 'CLEAN');
process.exit(found.length ? 1 : 0);
"
```

Note: `#fff` is a substring of longer hex codes only if they start with `#fff...` — double-check any match here is the standalone 3-digit literal (`#fff`), not a false positive; there are no 6-digit colors starting `#fff` elsewhere in this file per the current source.

Expected: FAIL, listing the literals below.

- [ ] **Step 2: Refactor**

| Literal(s) | Replace with |
|---|---|
| `#D9A65A` (all 12) | `var(--accent-gold)` |
| `#fff` (all 9) | `var(--text-primary)` unless it's text on a solid accent fill — use `var(--text-on-accent)` there (check context) |
| `#9A9CA3` (all 8) | `var(--text-secondary)` |
| `#F85149` (all 5) | `var(--danger)` |
| `#ff6b6b` (both) | `var(--danger-strong)` |
| `#C9D1D9` (both) | `var(--text-secondary)` |
| `#4FB6A0` (both) | `var(--success)` |
| `#30363D` (both) | `var(--border-strong)` |
| `#0E1117` (both) | `var(--bg-surface-inset)` unless text-on-accent-fill (check context) |
| `#f3e5ab` (1) | `var(--warning)` |
| `#F0F6FC` (1) | `var(--text-primary)` |
| `#38BDF8` (1) | `var(--accent-blue)` |
| `#21262D` (1) | `var(--border-subtle)` |
| `#161B22` (1) | `var(--bg-surface)` |
| `rgba(14, 18, 22, 0.6)`, `rgba(18, 22, 28, 0.9)`, `rgba(22, 26, 32, 0.75)` | `rgba(var(--bg-surface-inset-rgb), <same alpha>)` |
| `rgba(184, 134, 58, .2)` / `.22` / `.25` / `.4` / `.6` | `rgba(var(--accent-gold-rgb), <same alpha>)` |
| `rgba(217, 166, 90, 0.05)` / `0.3` | `rgba(var(--accent-gold-rgb), <same alpha>)` |
| `rgba(220, 53, 69, 0.2)` / `0.22` / `0.45` / `0.5` / `0.7` | `rgba(var(--danger-rgb), <same alpha>)` |
| `rgba(248, 81, 73, 0.8)` | `rgba(var(--danger-rgb), 0.8)` |
| Every `rgba(255, 255, 255, X)` / `rgba(255,255,255,X)` occurrence (`.04`–`.25` and `0.06`–`0.16` variants alike) | `rgba(var(--hover-tint-rgb), <same alpha>)` |
| `rgba(0, 0, 0, 0.45)`, `rgba(0,0,0,0.4)` | leave unchanged |

- [ ] **Step 3: Re-run the check from Step 1**

Expected: `CLEAN`, exit code 0.

- [ ] **Step 4: Manual browser verification**

Sign in, open SMT → SMT Pass Rate. Toggle `data-theme` as before. Check the KPI toolbar, charts/tables, and any warning/danger status text for legibility in both themes.

- [ ] **Step 5: Commit**

```bash
git add wwwroot/css/production-rate.css
git commit -m "feat: tokenize production-rate.css for light/dark theme"
```

---

### Task 7: Refactor `oqc-scanout-matrix.css`

**Files:**
- Modify: `wwwroot/css/oqc-scanout-matrix.css`

**Interfaces:**
- Consumes: `theme.css` tokens from Task 2.

This file is not currently linked from any view (`Views/Quality/OqcScanout.cshtml` only loads `dashboard.css` today), so this task is CSS-only — no manual browser check is possible or required. It's tokenized for consistency so it's ready if a future page wires it in.

- [ ] **Step 1: Run the failing "stale literal" check**

```bash
node -e "
const fs = require('fs');
const css = fs.readFileSync('wwwroot/css/oqc-scanout-matrix.css', 'utf8').toLowerCase();
const stale = ['#0f172a','#34d399','#1e293b','#131b2a','#fca5a5','#fbbf24','#f87171','#f1f5f9',
  '#94a3b8','#475569','#38bdf8','#334155','#0b0f17',
  'rgba(16, 185, 129','rgba(19, 27, 42','rgba(239, 68, 68','rgba(245, 158, 11','rgba(6, 182, 212'];
const found = stale.filter(s => css.includes(s.toLowerCase()));
console.log(found.length ? 'STALE: ' + found.join(', ') : 'CLEAN');
process.exit(found.length ? 1 : 0);
"
```

Expected: FAIL, listing the literals below.

- [ ] **Step 2: Refactor**

| Literal(s) | Replace with |
|---|---|
| `#0F172A` | `var(--bg-surface-inset)` |
| `#34D399` | `var(--success)` |
| `#1E293B` | `var(--bg-surface)` |
| `#131B2A` | `var(--bg-surface)` |
| `#FCA5A5` | `var(--danger)` |
| `#FBBF24` | `var(--warning)` |
| `#F87171` | `var(--danger)` |
| `#F1F5F9` | `var(--text-primary)` |
| `#94A3B8` | `var(--text-secondary)` |
| `#475569` | `var(--border-strong)` |
| `#38BDF8` | `var(--accent-blue)` |
| `#334155` | `var(--border-subtle)` |
| `#0B0F17` | `var(--bg-app)` |
| `rgba(0, 0, 0, 0.5)` | leave unchanged |
| `rgba(16, 185, 129, 0.15)` | `rgba(var(--success-rgb), 0.15)` |
| `rgba(19, 27, 42, 0.9)` | `rgba(var(--bg-app-rgb), 0.9)` |
| `rgba(239, 68, 68, 0.18)` | `rgba(var(--danger-rgb), 0.18)` |
| `rgba(245, 158, 11, 0.15)` | `var(--warning-soft)` |
| `rgba(6, 182, 212, 0.12)` | `rgba(var(--accent-blue-rgb), 0.12)` |

- [ ] **Step 3: Re-run the check from Step 1**

Expected: `CLEAN`, exit code 0.

- [ ] **Step 4: Commit**

```bash
git add wwwroot/css/oqc-scanout-matrix.css
git commit -m "feat: tokenize oqc-scanout-matrix.css for light/dark theme"
```

---

### Task 8: Final regression sweep

**Files:** none modified — verification only.

**Interfaces:**
- Consumes: everything from Tasks 1–7.

- [ ] **Step 1: Automated full-repo sweep**

```bash
node -e "
const fs = require('fs');
const files = ['wwwroot/css/dashboard.css','wwwroot/css/quality-block-release.css','wwwroot/css/production-rate.css','wwwroot/css/oqc-scanout-matrix.css'];
const themedHex = ['#d9a65a','#8b949e','#f0f6fc','#30363d','#21262d','#38bdf8','#161b22','#0e1117','#f85149','#4fb6a0'];
let bad = [];
for (const f of files) {
  const css = fs.readFileSync(f, 'utf8').toLowerCase();
  for (const h of themedHex) if (css.includes(h)) bad.push(f + ' -> ' + h);
}
console.log(bad.length ? 'REMAINING: ' + bad.join(', ') : 'ALL FOUR FILES CLEAN');
process.exit(bad.length ? 1 : 0);
"
```

Expected: `ALL FOUR FILES CLEAN`.

- [ ] **Step 2: Confirm Login is untouched**

```bash
git diff --stat -- Views/Account/Login.cshtml wwwroot/css/account.css wwwroot/css/site.css Views/Shared/_Layout.cshtml
```

Expected: empty output (no changes to any of these files across the whole feature).

- [ ] **Step 3: Manual cross-page sweep**

Sign in and, for each of the 6 in-scope pages (Home, Quality Index, Quality Block/Release, Quality OQC Scanout, SMT Index, SMT Production Rate):
1. Load the page normally at a few different real wall-clock times over the course of testing, or force `data-theme` via devtools before first paint is easiest — confirm there's no visible flash of the wrong theme.
2. Set `data-theme` to `'light'`, then `'dark'`, in the console; confirm every visible piece of text stays legible against its background in both.
3. Confirm dark mode is visually indistinguishable from the app's appearance before this feature (spot-check 3–4 elements per page against your memory/a pre-change screenshot if you took one).
4. Load the Login page and confirm it always renders dark regardless of the forced `data-theme` state on other pages (it doesn't read `data-theme` at all, since it wasn't wired in).

- [ ] **Step 4: Commit (if Step 1's script needs to be kept)**

This task produces no file changes on its own; if all checks pass, there's nothing to commit. If any check fails, return to the relevant earlier task, fix it, and re-run that task's own verification before re-running this sweep.

---

## Self-Review Notes

- **Spec coverage:** every requirement in the design spec (scope of 6 pages, Login/site.css/_Layout untouched, client-clock 6–18 window, real-time 60s re-check, no manual toggle, dark-mode-unchanged, palette table, hero banner overlay treatment, manual testing approach) maps to a task above.
- **Extended tokens beyond the spec's summary table:** the mechanical refactor surfaced several literal colors (bright red/amber "strong" variants, RGB-triplet forms for alpha-blended overlays, a fixed dark-on-gold-button text color, a full-opacity scrim) that the spec's 15-row summary table didn't itemize individually. Task 2's `theme.css` extends the token set to cover all of them, following the same design intent (preserve hue family, ensure AA contrast) — this is filling in the spec's stated intent at full implementation detail, not a scope change.
- **Type/name consistency:** every `var(--token)` referenced in Tasks 3, 5, 6, 7's mapping tables exists in Task 2's `theme.css`; cross-checked name-by-name while writing this plan.
