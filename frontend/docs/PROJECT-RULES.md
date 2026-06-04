# TEDF Frontend — Project Rules

Coding conventions for the TEDF admin SPA (`frontend/`). Read this before adding or changing code. For *how the app is wired*, see [`ARCHITECTURE.md`](ARCHITECTURE.md); for global/monorepo rules see the root `CLAUDE.md`.

These rules describe the conventions the codebase already follows — match them rather than introducing a parallel style.

---

## 1. Language & Tooling

- **TypeScript only**, `strict` mode. No new `.js`/`.jsx` source files. Avoid `any`; prefer precise types, `unknown` + narrowing, or generics.
- Lint with `npm run lint` before considering work done; `npm run build` (`tsc -b && vite build`) must pass with **no type errors**.
- ESLint enforces `react-hooks` rules and `@typescript-eslint/no-unused-vars`. Unused symbols are errors at build time (`noUnusedLocals` / `noUnusedParameters`); prefix an intentionally-unused parameter with `_` (e.g. `_password`) to satisfy the `argsIgnorePattern: "^_"`.
- Respect the Rules of Hooks: call hooks unconditionally at the top level; provide correct dependency arrays. Wrap callbacks passed across renders in `useCallback`, and hold latest-callback refs (as `useSignalR` does) instead of reconnecting/re-subscribing.

## 2. Imports & Module Boundaries

- **Always import via the `@/` alias** (`@/*` → `src/*`). Do not use deep relative paths like `../../components/...`.
- **Import through barrels** where they exist: `@/pages`, `@/components/layout`, `@/components/mentor`, `@/pages/errors`. When you add a page or shared component, export it from the matching `index.ts` so it stays importable via `@/...`.
- Import **services from `@/lib`** (barrel) and **types from `@/types`** (barrel) — separately. Use `import type { … }` for type-only imports. **Never import type names from a service module**, and never define API types inside a page/service.
- Keep the dependency direction one-way: `pages` → `components` / `lib` / `hooks` / `contexts`; and within `lib`: `<domain>Service` → `@/types` + `lib/common`. Components and `lib` services must **not** import from `pages`.

## 3. File & Symbol Naming

| Kind | Convention | Example |
|------|------------|---------|
| Component / page file | `PascalCase.tsx` | `ProjectsPage.tsx`, `NotificationDropdown.tsx` |
| Component export | named `PascalCase` function | `export function ProjectsPage() {}` |
| Service file | `lib/<domain>/<domain>Service.ts` | `project/projectService.ts` |
| Service export | `export const <domain>Service = { … }` | `projectService` |
| Hook file / export | `useXxx.ts` / `useXxx` | `useSignalR.ts` → `useSignalR` |
| Context | `XxxContext.tsx` → `XxxProvider` + `useXxx` | `AuthContext.tsx` |
| Types file | `types/<domain>/<domain>.types.ts` | `projects/project.types.ts` |
| API type names | HTTP-method-driven (§5) | `…Response` / `…Request` / `…Dto` / `…Filters` / `…Option` |
| Constants | `UPPER_SNAKE_CASE` for module-level config | `PAGE_SIZE` |

Prefer **named exports** everywhere. Default exports are reserved for `App.tsx`.

## 4. Components & Pages

- A **page** is a route target under `src/pages/<role>/`; register it in the route table in `App.tsx` and export it from `src/pages/index.ts`.
- Keep pages role-scoped. Cross-role/shared UI goes in `components/common/` or `components/shared/`; feature widgets go in `components/<role>/`.
- Role **layouts** stay thin: sidebar + `<main><Outlet/></main>` (see `components/layout/AdminLayout.tsx`). Don't put page logic in layouts.
- Co-locate small presentational state with the component; lift to context only when genuinely cross-cutting (see §6).
- Use **Material Symbols** icon names (string tokens like `"edit_note"`, `"schedule"`) consistent with existing usage rather than importing icon component libraries.

## 5. Data Fetching & the API Layer

- **Never call `apiClient` (or `fetch`) directly from a page or component.** All HTTP goes through a `lib/<domain>/<domain>Service.ts` method. (`grep apiClient src/pages src/components` must stay empty.)
- **Build URLs from `lib/common/routes.ts`** (`routes.<domain>.*`) inside services — no raw URL strings anywhere, in services or pages.
- `apiClient` already handles: base URL (`VITE_API_BASE_URL`), the `Authorization: Bearer` header, the `X-Route-Path` header, unwrapping the `ApiResponse<T> = { success, message, data, errors }` envelope, and throwing a normalized `Error` on failure. Don't re-implement these.
- Use `apiClient.postForm(path, FormData)` for file uploads (let the browser set the multipart boundary) — see `lib/common/fileUploadUtils.ts`. Do not set `Content-Type` manually for uploads.
- **Types belong in `types/<domain>/<domain>.types.ts`**, imported via `@/types` — a service does not declare or re-export API types. Name them by HTTP method: GET top-level return → `…Response` (nested/shared shapes stay `…Dto`), query/pagination input → `…Filters` (or `…Request` for non-list query inputs); POST/PUT/PATCH → `…Request` body + `…Response` result (or `void` for 204). Select options → `…Option`.
- Treat thrown errors as user-facing messages: catch in the page and either show inline or call `useSystemError().showError(message)`. Don't swallow errors silently.

## 6. State Management

- No external state library. Use, in order of preference: local component state → a domain service call → a shared **Context**.
- Each context follows the established shape: a `XxxProvider` plus a `useXxx()` hook that **throws** if used outside its provider (see `AuthContext`, `MaintenanceContext`, `SystemErrorContext`).
- Persist only what must survive reloads, via `localStorage`, using the existing keys: `user`, `activeRole`, `maintenanceMode`, `themeColor`. Don't scatter new ad-hoc keys without need.
- Provider order in `App.tsx` is intentional (`Maintenance → Auth → SystemError`); preserve it when adding providers.

## 7. Authentication & Authorization

- Read auth state through `useAuth()` — never parse the token or touch Firebase directly from a page.
- Guard every authenticated route by nesting its layout in `<ProtectedRoute>`. Pass `allowedRoles` to restrict by role; omitting it allows any authenticated user (used deliberately, so be explicit about intent).
- Roles are the lowercased set `{ admin, mentor, evaluator, student, departmenthead }`. Support **multi-role** users: check membership with `user.roles.includes(role)` and respect `activeRole` for the current view. Use `switchRole` for role changes.
- The API token is sourced from `localStorage["user"].firebaseToken`; let `apiClient`/`useSignalR` read it. Don't cache tokens elsewhere.

## 8. Styling & Theming

- **Tailwind utility classes** are the styling mechanism; compose them inline. Avoid bespoke CSS files except global layers in `index.css`.
- Use the **semantic theme tokens** from `tailwind.config.js` instead of raw hex: `primary` / `primary-light` / `primary-dark` (driven by CSS variables), `success`, `error`, `warning`, `navy-header`. Font family `display`/`body` is Inter.
- **Never hardcode the brand color.** Theming flows through the `--color-primary*` CSS variables set at startup from `localStorage["themeColor"]`; reference `primary*` tokens so the UI re-themes correctly.
- For animation, use Framer Motion with the existing `container`/`item` variants pattern; `AnimatePresence` already wraps the route tree for transitions.

## 9. Real-time

- Use `useSignalR` for the notifications hub rather than constructing `HubConnection`s ad hoc. It handles the token via `accessTokenFactory`, automatic reconnect, and cleanup on unmount.
- Keep event handlers current via the callback-ref pattern; don't add a hub callback to a hook's dependency array in a way that forces reconnects.

## 10. Content & Copy

- User-facing strings are **Vietnamese** (labels, status text, error messages), matching existing pages. Keep code identifiers, types, and comments in English.
- Map backend enum values (e.g. project `status`) to localized labels via lookup objects co-located in the page (see the `statusConfig` pattern in `ProjectsPage.tsx`); don't hardcode scattered string comparisons.

## 11. Environment & Secrets

- Client env vars must be prefixed `VITE_` and read via `import.meta.env` (`VITE_API_BASE_URL`, `VITE_FIREBASE_*`). Never read `process.env` in client code.
- Do not commit real secrets or `.env` files. Firebase web config is public by design, but keep keys in env, not source.

## 12. Definition of Done

Before marking a change complete:

1. `npm run lint` passes (no new warnings you introduced).
2. `npm run build` passes (type-check + bundle, no errors).
3. New pages/components are routed in `App.tsx` (if applicable) and exported from the relevant barrel.
4. Data access goes through a `lib/<domain>` service (no `apiClient`/`fetch` in pages/components); URLs come from `routes.*`; API types live in `types/` and are imported from `@/types`.
5. Styling uses Tailwind semantic tokens; no hardcoded brand color.
6. User-facing copy is Vietnamese and consistent with sibling pages.
