# Self-Host CDN Assets on OqcScanout and Document pages — Design

## Goal

`Views/Quality/OqcScanout.cshtml` and `Views/FileStorage/Index.cshtml` load
Tailwind CSS, Chart.js (+ its datalabels plugin), and Font Awesome from
public CDNs, synchronously in `<head>`. On a weak/high-latency network each
of these is a blocking round-trip to a third-party host before the page can
paint anything — this is why these two pages load dramatically slower than
every other page in the app, which serves only local static assets. Remove
the external dependency: vendor each library's files under `wwwroot/` and
point the existing `<script src>`/`<link href>` tags at the local copies
instead.

This is a pure asset-hosting change. No markup, no CSS class usage
(Tailwind utility classes, `fa-*` icon classes), and no chart configuration
changes — only *where* the library files are fetched from.

## Scope

**In scope:**
- `Views/Quality/OqcScanout.cshtml` — 3 CDN `<script src>` tags
  (`cdn.tailwindcss.com`, `cdn.jsdelivr.net/npm/chart.js`,
  `cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2`)
- `Views/FileStorage/Index.cshtml` — 1 CDN `<script src>`
  (`cdn.tailwindcss.com`) + 1 CDN `<link href>`
  (`cdnjs.cloudflare.com/.../font-awesome/6.4.0/css/all.min.css`)
- New vendored files under `wwwroot/js/` and `wwwroot/css/fontawesome/`

**Out of scope:**
- Converting Tailwind's ~200 utility classes on each page to hand-written
  CSS, or introducing a Tailwind CLI build step — rejected during design:
  this project has no Node/npm build tooling today, and adding one is a
  separate, larger architectural decision. Self-hosting the CDN's runtime
  JIT-compiler script keeps today's zero-build-step workflow unchanged.
- Converting the 21 `fa-*` icon usages to inline SVG — rejected during
  design in favor of self-hosting the Font Awesome CSS+font files as-is,
  since it requires zero markup changes.
- Any other page in the app — none of them use these CDNs.
- The theme feature (separate, already-completed work).

## Approach

Three independent, same-shaped substitutions:

### 1. Tailwind runtime script (both pages)

Download the exact file currently served at `https://cdn.tailwindcss.com`
and commit it as `wwwroot/js/tailwind-runtime.js`. Change both pages'
```html
<script src="https://cdn.tailwindcss.com"></script>
```
to
```html
<script src="~/js/tailwind-runtime.js" asp-append-version="true"></script>
```
The script still JIT-compiles Tailwind's utility CSS from whatever classes
it finds in the page's DOM at load time — that behavior is unchanged, only
the network hop to fetch the compiler itself is removed. The existing
`console.warn` suppressor (which filters a `cdn.tailwindcss.com`-specific
warning string) can stay — it's harmless if the warning never fires again
locally, but removing it is a reasonable one-line cleanup during
implementation.

### 2. Chart.js + chartjs-plugin-datalabels (OqcScanout only)

Download the **exact versions currently resolved by the unpinned CDN URLs**
(`https://cdn.jsdelivr.net/npm/chart.js` and
`https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2` both resolve to
"latest within range" at request time, not a fixed version — the
implementation step must first determine which concrete version each
currently resolves to, e.g. by inspecting the response or jsdelivr's
resolved-version redirect/headers, then download that exact pinned
version) and commit them as `wwwroot/js/chart.min.js` and
`wwwroot/js/chartjs-plugin-datalabels.min.js`. Change:
```html
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chartjs-plugin-datalabels@2"></script>
```
to
```html
<script src="~/js/chart.min.js" asp-append-version="true"></script>
<script src="~/js/chartjs-plugin-datalabels.min.js" asp-append-version="true"></script>
```
Pinning to today's exact resolved version (rather than "latest") guarantees
zero behavior change in the 14 Chart.js usages on this page — no API
surface can have shifted between versions.

### 3. Font Awesome (Document/FileStorage only)

Download Font Awesome's free CSS+webfont bundle (the standard
`css/all.min.css` plus its `webfonts/*.woff2` files — the version currently
pinned in the CDN URL, `6.4.0`, so this one's already an exact version, no
resolution step needed) and commit under `wwwroot/css/fontawesome/`
(`css/all.min.css` + `webfonts/`). Change:
```html
<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet" />
```
to
```html
<link href="~/css/fontawesome/css/all.min.css" rel="stylesheet" asp-append-version="true" />
```
All 21 `fa-*` icon classes already used in the markup work unchanged, since
the vendored CSS is byte-for-byte the same file, just served locally.

## Testing

No build step, no automated test framework in this repo (consistent with
the rest of the app) — verification is:
1. Load each page with the browser devtools Network tab open, filter to
   the CDN hostnames (`cdn.tailwindcss.com`, `cdn.jsdelivr.net`,
   `cdnjs.cloudflare.com`) — confirm zero requests to any of them.
2. Visual check: both pages render identically to before (Tailwind
   utility styling, the OqcScanout charts, the 21 FileStorage icons) —
   since the vendored files are byte-identical to what the CDN was
   already serving, this should be a non-event, but worth a quick
   before/after screenshot comparison per page.
3. Confirm no console errors (a missing/misnamed local file would 404 and
   break the page instantly — easy to catch).

## Non-goals

- No Tailwind CLI / static-CSS build step (see Scope — deferred; the
  upgrade path if the in-browser JIT-compile cost ever proves too slow on
  its own).
- No conversion of Font Awesome icons to inline SVG.
- No change to any page's markup, styling, or behavior beyond asset
  source location.
