# ADR 002 — Styling Approach

**Status:** Accepted
**Date:** 2026-06-01
**Applies to:** `frontend/` (TEDF admin SPA)

## Context

The TEDF SPA is a content-dense admin app: five role dashboards, list/detail tables, drawers, modals, calendars, and forms. The styling system needs to:

- Support fast, consistent UI construction across many pages and contributors.
- Provide a **per-deployment / per-user brand color** — the app re-themes from a single primary color chosen in Settings, without a rebuild.
- Stay lightweight (no heavy runtime CSS-in-JS) and play well with Vite + React 19 + TypeScript.
- Offer a small, shared visual vocabulary (status colors, cards, inputs) so pages look uniform.

## Decision

**Use Tailwind CSS 3 as the single styling system, drive theming through CSS custom properties exposed as Tailwind tokens, and keep a minimal set of shared component classes in `index.css`.**

Concretely:

1. **Tailwind utilities, composed inline** in components are the default styling mechanism. Tailwind scans `index.html` and `src/**/*.{js,ts,jsx,tsx}` (`tailwind.config.js` `content`).

2. **Dynamic theming via CSS variables.** `index.css` seeds `:root` with `--color-primary` / `--color-primary-light` / `--color-primary-dark`. `tailwind.config.js` maps these to the `primary`, `primary-light`, and `primary-dark` color tokens (`"primary": "var(--color-primary)"`). At startup, `App.tsx` reads `localStorage["themeColor"]` (default `#2c6090`) and overwrites those variables — `primary-dark`/`primary-light` are computed by a brightness-adjust helper — so the entire UI re-themes from one stored color.

3. **Semantic color tokens, not raw hex.** `tailwind.config.js` defines `primary*`, `success` (`#5F8F61`), `error` (`#A64B4B`), `warning` (`#eab308`), and `navy-header` (`#1e3a8a`). Components reference these tokens (`text-success`, `bg-primary`) rather than literal colors.

4. **One type family + icon font.** `Inter` (loaded from Google Fonts in `index.html`) is the `display` and `body` font family. Icons use the **Material Symbols Outlined** font (also CDN-loaded); icons are referenced by name strings (e.g. `"edit_note"`, `"schedule"`) rather than an icon-component library.

5. **A small layer of shared component classes** in `index.css` using `@apply` — `.bento-card`, `.input-field`, plus utilities `.scrollbar-hide`, `.custom-scrollbar`, `.academic-pattern`. These exist only for genuinely repeated, multi-utility patterns; they are the exception, not the norm.

6. **Custom design tokens** (`borderRadius`, `boxShadow.soft`, `boxShadow.bento`, `fontFamily`) live in `tailwind.config.js` `theme.extend` so spacing/elevation stay consistent.

7. **Animation** is handled by Framer Motion (`motion.*`, `AnimatePresence`), not CSS keyframes, for page/element transitions — see [ARCHITECTURE.md](../ARCHITECTURE.md).

## Consequences

### Positive

- **Fast, consistent authoring** — utilities co-locate styling with markup; no separate stylesheet to hunt through or naming scheme to invent.
- **Rebuild-free theming** — the CSS-variable-as-Tailwind-token bridge means a user can change the brand color at runtime and the whole app follows, including hover/focus states that reference `primary`.
- **No runtime CSS-in-JS cost** — Tailwind emits static CSS; only the used utilities ship (purged via `content`).
- **Shared vocabulary** — semantic tokens and the few `@apply` component classes keep status colors, cards, and inputs uniform across roles.

### Negative / trade-offs

- **Verbose markup** — long `className` strings; mitigated by extracting components and a handful of `@apply` classes for true repetition.
- **Two sources of theme truth** — `index.css` seeds initial CSS variable values and `App.tsx` overwrites them at runtime; the seeded values must be kept sensible since they show for the first paint before JS runs.
- **CDN font/icon dependency** — Inter and Material Symbols load from Google Fonts; offline/air-gapped environments would need self-hosting. Icon names are untyped strings, so typos aren't caught by the compiler.
- **Discipline required** — the value of semantic tokens collapses if contributors drop in raw hex or one-off colors; this is enforced by convention (see PROJECT-RULES.md §8), not tooling.

## Alternatives Considered

- **CSS-in-JS (styled-components / Emotion)** — rejected: runtime cost and extra dependency; dynamic theming is already solved more cheaply with CSS variables.
- **CSS Modules / plain SCSS** — rejected: loses the utility-first speed and the purge-based small output; would reintroduce class-naming overhead.
- **A component library (MUI, Chakra, Ant Design)** — rejected: heavier bundle and opinionated theming that fights a custom brand-color system; the app needs bespoke, dense admin layouts that are quicker to build directly with utilities.
- **Theming via a Tailwind `darkMode`/class strategy instead of CSS variables** — insufficient: we need an *arbitrary* user-chosen primary color, not a fixed set of themes, which CSS variables handle naturally.

## Revisit When

- The set of `@apply` component classes in `index.css` grows large — that signals a need for proper reusable components (or a thin internal component kit).
- Offline/self-hosted deployments are required (self-host Inter + Material Symbols).
- Icon usage would benefit from compile-time safety (introduce a typed icon-name union or an icon component).
