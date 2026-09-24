# Time-based Light/Dark Theme — Design

## Goal

The app is currently dark-themed everywhere, which hurts readability in bright
environments. Add an automatic theme switch driven by the viewer's local
clock: **light theme from 6:00 to 17:59, dark theme from 18:00 to 5:59**, no
manual override, applied in real time (a page left open across the boundary
switches without a reload).

## Scope

**In scope (theme-aware):**
- `Views/Home/Index.cshtml`
- `Views/Quality/Index.cshtml`, `Views/Quality/BlockRelease.cshtml`, `Views/Quality/OqcScanout.cshtml`
- `Views/Smt/Index.cshtml`, `Views/Smt/ProductionRate.cshtml`
- `Views/Shared/_DashboardHeader.cshtml` (partial, inherits host page's theme — no direct change needed)
- `wwwroot/css/dashboard.css`, `wwwroot/css/quality-block-release.css`,
  `wwwroot/css/production-rate.css`, `wwwroot/css/oqc-scanout-matrix.css`
  (tokenized for consistency even though the last one isn't linked from any
  view today)

**Out of scope (stays permanently dark, unchanged):**
- `Views/Account/Login.cshtml` / `wwwroot/css/account.css` — the animated 3D
  background and neon styling are dark-only by design.
- `wwwroot/css/site.css`, `Views/Shared/_Layout.cshtml` — this scaffold
  layout is not used by any real page (every view sets `Layout = null`), so
  it is left as-is.

## Architecture

### 1. `wwwroot/css/theme.css` (new)

Single source of truth for every themed color, expressed as CSS custom
properties. Light values are the default `:root` (no attribute needed —
if JS fails to run for any reason, the page still renders legibly in
light mode). Dark values live under `:root[data-theme="dark"]` and are
copied from the current hardcoded dark palette unchanged, so dark mode is
pixel-identical to today.

Token set (see Palette below for exact values):
`--bg-app`, `--bg-surface`, `--bg-surface-inset`, `--border-subtle`,
`--border-strong`, `--text-primary`, `--text-secondary`, `--text-tertiary`,
`--accent-gold`, `--accent-gold-soft`, `--accent-blue`, `--danger`,
`--danger-soft`, `--success`, `--success-soft`, plus the two hero-banner
overlay tokens described below.

`theme.css` is linked before each page's own stylesheet(s), so page CSS can
consume the tokens.

### 2. `wwwroot/js/theme-clock.js` (new)

```js
function getLocalHour() { return new Date().getHours(); }
function computeTheme(hour) { return (hour >= 6 && hour < 18) ? 'light' : 'dark'; }
function applyTheme() {
  document.documentElement.setAttribute('data-theme', computeTheme(getLocalHour()));
}
applyTheme();
setInterval(applyTheme, 60000);
```

- `getLocalHour()` is a separate, trivially-overridable function — during
  manual testing it can be stubbed from devtools to simulate any hour
  without changing the system clock.
- The script is included **inline, synchronously, in `<head>`, before the
  stylesheet `<link>`** on each in-scope page, so `data-theme` is set before
  first paint (no flash-of-wrong-theme).
- Re-applies every 60 seconds so a page left open across 6:00/18:00 switches
  live.

### 3. Per-page wiring

Each of the 6 in-scope `.cshtml` files gets, near the top of `<head>`:

```html
<script>
  (function () {
    function getLocalHour() { return new Date().getHours(); }
    document.documentElement.setAttribute(
      'data-theme',
      (getLocalHour() >= 6 && getLocalHour() < 18) ? 'light' : 'dark');
  })();
</script>
<link rel="stylesheet" href="~/css/theme.css" asp-append-version="true" />
```

plus a `<script src="~/js/theme-clock.js" asp-append-version="true"></script>`
near the end of `<body>` (or in a `Scripts` section) to own the 60s
re-check — the inline snippet only handles the first-paint case.

### 4. Stylesheet refactor

Every hardcoded hex color in the four in-scope CSS files (including ones
currently pinned with `!important`) is replaced with the matching
`var(--token)`. Each `!important` is re-evaluated during the edit — kept
only if removing it causes a visible regression against whatever rule it
was originally overriding.

## Palette

| Token | Dark (unchanged) | Light (new) | Notes |
|---|---|---|---|
| `--bg-app` | `#0B0C0E` | `#F5F6F8` | page background |
| `--bg-surface` | `#161B22` | `#FFFFFF` | header, cards, dropdown menu |
| `--bg-surface-inset` | `#0E1117` | `#F0F2F5` | fact-card / nested tiles |
| `--border-subtle` | `#21262D` | `#E2E5EA` | |
| `--border-strong` | `#30363D` | `#C7CCD4` | |
| `--text-primary` | `#F0F6FC` | `#12161C` | headings, values |
| `--text-secondary` | `#8B949E` | `#5B6472` | labels, descriptions (≥4.5:1 on white) |
| `--text-tertiary` | `#6E7681` | `#838B99` | mono meta text — large text/labels only |
| `--accent-gold` | `#D9A65A` | `#9A6A22` | darkened for ≥4.5:1 on white |
| `--accent-gold-soft` | `rgba(217,166,90,.12)` | `rgba(154,106,34,.10)` | |
| `--accent-blue` | `#38BDF8` | `#0B72B9` | "live" status — darkened for contrast on white |
| `--danger` | `#F85149` / `#C6433C` | `#B42318` | |
| `--danger-soft` | `rgba(198,67,60,.16)` | `rgba(180,35,24,.08)` | |
| `--success` | `#4FB6A0` | `#157F5C` | |
| `--success-soft` | `rgba(79,182,160,.16)` | `rgba(21,127,92,.08)` | |

All primary-text/background and label/background pairings above meet WCAG
AA (4.5:1 normal text, 3:1 large text/borders) in both themes.

### Hero banner background (`body.dashboard-page::before` / `::after`)

Currently: real photo, `grayscale(35%) brightness(.4)`, under a near-black
gradient (`::after`). This is visible in the gaps between opaque surfaces
(header, hero card, plant-status-bar all have their own opaque
`--bg-surface` fill, so the photo only shows through page padding/margins).

New tokens:
- `--hero-image-filter`: `grayscale(35%) brightness(.4)` (dark) /
  `grayscale(15%) brightness(1.05)` (light)
- `--hero-overlay-gradient`: the existing dark radial/linear gradient (dark) /
  an equivalent gradient built from `--bg-app` at matching opacities (light)

so the same photo reads moody in dark mode and airy in light mode, while
never sitting directly under body text (that text is always inside an
opaque `--bg-surface` card).

## Testing

- No automated UI test harness exists in this repo; verification is manual.
- Simulate each theme window by stubbing `getLocalHour()` in the browser
  console (both the inline head snippet and `theme-clock.js` call the same
  function name, so overriding `window` isn't needed — reload after editing
  the value in devtools' local override, or temporarily hardcode a return
  value while testing).
- For each of the 6 in-scope pages, check both themes for: no
  flash-of-wrong-theme on load, no low-contrast text (hero title/desc, fact
  numbers, dept-card text, nav-tab labels, status badges, table content),
  and that dark mode is visually unchanged from the current app.
- Confirm the Login page is unaffected (always dark) in both time windows.

## Non-goals

- No manual theme toggle/override control.
- No `prefers-color-scheme` / OS-theme integration — the switch is driven
  solely by local clock hour.
- No change to Login page, `site.css`, or the unused `_Layout.cshtml`.
