# ADR 001 — State Management

**Status:** Accepted
**Date:** 2026-06-01
**Applies to:** `frontend/` (TEDF admin SPA)

## Context

The TEDF SPA serves five roles from a single React 19 + TypeScript app. The state it manages falls into a few distinct buckets:

- **Session / auth state** — the current user, the full role set, and the currently `activeRole`. Must survive page reloads and stay in sync with Firebase Auth.
- **A small amount of cross-cutting UI state** — maintenance-mode flag, a global system-error modal, and the theme color.
- **Server data** — projects, groups, topic pools, evaluations, etc. This is the bulk of the data, but it is read per-page, mostly list/detail views behind role-specific pages.

What we explicitly do **not** have: deeply shared, frequently-mutated client state that multiple distant components edit simultaneously. Server data is fetched where it is displayed and is naturally scoped to a page.

We needed to decide how to manage state without over-engineering for a problem we don't have.

## Decision

**Use React's built-in primitives — Context + hooks + `localStorage` — and fetch server data on demand through the service layer. Do not add an external state-management or data-fetching/caching library.**

Concretely:

1. **Global app state lives in React Context**, one provider per concern, composed in `App.tsx` in a deliberate order (`MaintenanceProvider → AuthProvider → SystemErrorProvider`):
   - `AuthContext` — `user`, `activeRole`, `login`/`logout`, `switchRole`, `isLoading`.
   - `MaintenanceContext` — maintenance-mode flag.
   - `SystemErrorContext` — `showError(message)` driving a global modal.

2. **Each context exports a `Provider` + a `useXxx()` hook** that throws if used outside its provider. Consumers never read context directly.

3. **Persistence is `localStorage`**, with a fixed, small set of keys: `user`, `activeRole`, `maintenanceMode`, `themeColor`. State is re-hydrated from these on load (lazy `useState` initializers); `AuthContext` additionally reconciles with Firebase via `onAuthStateChanged`.

4. **Server data is not held in a global store.** Pages call `lib/<domain>Service.ts` methods (over `apiClient`) and keep the result in local component state (`useState`/`useEffect`). There is no client-side cache layer.

5. **Default to local state.** Order of preference: local component state → a service call → a shared Context. Promote to Context only when state is genuinely cross-cutting.

## Consequences

### Positive

- **Zero extra dependencies / bundle cost** for state management; less to learn and maintain.
- **Clear ownership** — each piece of global state has exactly one provider and one hook; the throw-on-misuse guard catches wiring mistakes early.
- **Predictable persistence** — a small, enumerated set of `localStorage` keys, easy to reason about and clear on logout.
- **Server data stays simple** — it lives next to the UI that shows it, scoped to the page's lifecycle, with no cache-invalidation problem to manage.

### Negative / trade-offs

- **No automatic server-cache** — refetching, deduping, and background revalidation are not provided. Pages re-fetch on mount; shared server data may be fetched more than once across pages.
- **Manual loading/error handling** — each page wires its own `useState` loading/error flags (the thrown `Error` from `apiClient` is caught locally or surfaced via `useSystemError`).
- **Context re-render granularity** — a context update re-renders all consumers. Acceptable here because the global contexts are small and change infrequently; if a future context becomes hot, split it or memoize.
- **`localStorage` is synchronous and unencrypted** — fine for the user profile + non-sensitive flags we store; the auth token rides on the user object and is treated as a bearer credential, not a secret at rest.

## Alternatives Considered

- **Redux / Redux Toolkit** — rejected: heavyweight for the small amount of truly-global state; most of our data is server-owned and page-scoped, so a global store would mostly hold boilerplate.
- **Zustand / Jotai** — lighter than Redux and reasonable, but Context already covers our few global concerns with no dependency. Revisit if global, frequently-mutated state grows.
- **React Query / SWR (server-state cache)** — the strongest candidate, since it would remove manual fetch/loading boilerplate and add caching. Deferred, not dismissed: current pages are simple list/detail reads where the cost of manual fetching is low. **This is the most likely thing to adopt later** if cross-page data sharing and refetch churn become painful.

## Revisit When

- Multiple pages need to share and keep in sync the *same* server data (consider React Query/SWR).
- A global context becomes a re-render hotspot (split the context or introduce a lightweight store like Zustand for that slice).
- Persistence needs outgrow a handful of `localStorage` keys or require migrations/encryption.
