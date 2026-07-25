# Frontend: TEDF Admin SPA

Guidance for Claude Code when working inside `frontend/`. This is the **frontend**-scope file; the monorepo root `CLAUDE.md` covers global context.

The frontend is a single React SPA serving all five roles (**Admin, DepartmentHead, Mentor, Student, Evaluator**). Each role gets its own layout, sidebar, and route subtree, guarded by `ProtectedRoute`. Entry: `main.tsx` → `App.tsx` (route table + theme bootstrap + context providers).

## Tech Stack

| Concern | Choice |
|---------|--------|
| Framework | React 19 |
| Language | TypeScript 5.7 (`strict`, `noUnusedLocals`, `noUnusedParameters`) |
| Build/dev | Vite 6 (dev server on `http://localhost:5173`) |
| Routing | React Router 7 (`react-router-dom`) |
| Styling | Tailwind CSS 3.4 + PostCSS; theming via CSS custom properties (`--color-primary`) persisted in `localStorage` |
| Animation | Framer Motion 11 (`AnimatePresence` on the route tree) |
| Real-time | `@microsoft/signalr` 10 (chat + notifications hubs) |
| Auth | Firebase 12 (Firebase Auth → API issues JWT) |
| PDF | `react-pdf` 10 + `pdfjs-dist` 5 |
| Dates | `date-fns` 4 |
| Lint | ESLint 9 + `typescript-eslint` 8 |
| Path alias | `@/*` → `./src/*` (configured in `tsconfig.app.json`) |

Commands (run inside `frontend/`):
```powershell
npm install
npm run dev      # Vite dev server -> http://localhost:5173
npm run build    # tsc -b && vite build
npm run lint     # ESLint
npm run preview  # preview the production build
```
> No test runner is configured — do not assume one exists.

## Documentation

### Must Read

- [`src/AUTH_CONTEXT.md`](src/AUTH_CONTEXT.md) — auth flow, `AuthContext`, roles, and route guarding.
- Per-role context files in `src/`, read the one matching the area you are touching:
  - [`src/ADMIN_CONTEXT.md`](src/ADMIN_CONTEXT.md)
  - [`src/DEPARTMENT-HEAD_context.md`](src/DEPARTMENT-HEAD_context.md)
  - [`src/LECTURER_CONTEXT.md`](src/LECTURER_CONTEXT.md)
  - [`src/STUDENT_CONTEXT.md`](src/STUDENT_CONTEXT.md)

### Reference

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — frontend architecture details.
- [`docs/PROJECT-RULES.md`](docs/PROJECT-RULES.md) — conventions and coding rules.
- [`docs/PROJECT-STATUS.md`](docs/PROJECT-STATUS.md) — current feature status.
- [`docs/decisions/`](docs/decisions) — ADRs: `001-state-management.md`, `002-styling-approach.md`.
- Root [`../ARCHITECTURE.md`](../ARCHITECTURE.md) §10 (Frontend Architecture) — full route map and component hierarchy.

> Some of the docs above are placeholders being filled in — verify content before relying on them.

## Quick Reference

### Feature Location

| Concern | Location |
|---------|----------|
| Route table + theme bootstrap | `src/App.tsx` (entry `src/main.tsx`) |
| Pages (by role) | `src/pages/{admin,department-head,lecturer,student}/`; also `auth/`, `errors/` (Mentor + Evaluator are unified under `lecturer/`) |
| Role layouts + sidebars + header | `src/components/layout/` |
| Route guard | `src/components/auth/ProtectedRoute.tsx` |
| Other components | `src/components/{admin,auth,common,layout,lecturer,mentor,shared,student,support}/` |
| API clients (one per domain) | `src/lib/<domain>/<domain>Service.ts` over `src/lib/common/apiClient.ts`; barrel `src/lib/index.ts` (`@/lib`) |
| API route registry (single source of truth) | `src/lib/common/routes.ts` — services build URLs from `routes.*`, never raw strings |
| File upload helpers | `src/lib/common/fileUploadUtils.ts` |
| Global state | `src/contexts/` — `AuthContext`, `MaintenanceContext`, `SettingsContext` (server-side branding), `SystemErrorContext` |
| Hooks | `src/hooks/` — `useSignalR`, `signalREvents`, `useNotificationTargetRefresh`, `useUnreadSupportCount`, `useWishlist` |
| Firebase setup | `src/config/firebase.ts` |
| Shared types | `src/types/<domain>/<domain>.types.ts`; barrel `src/types/index.ts` (`@/types`) |
| Static assets | `src/assets/` (e.g. `logo/`) |

Role → home route map (from `App.tsx`): `admin → /admin`, `mentor → /mentor`, `evaluator → /evaluator`, `student → /student`, `departmenthead → /department-head`. Public routes: `/login`, `/maintenance`, `/403`; unmatched → `NotFoundPage`.

### Public Exports

Prefer importing from barrels via the `@/` alias rather than deep file paths:

- `@/pages` — all page components (`src/pages/index.ts`; re-exports the `lecturer`, `student`, `errors` sub-barrels).
- `@/components/layout` — layouts, sidebars, `Header`, `NotificationDropdown` (+ `UserRole` type) (`src/components/layout/index.ts`).
- `@/components/lecturer` — `RegisterTopicModal` (`src/components/lecturer/index.ts`).
- `@/pages/errors` — `NotFoundPage`, `AccessDeniedPage`.
- Contexts expose a provider + hook pair, e.g. `@/contexts/AuthContext` → `AuthProvider`, `useAuth`.
- Service modules in `@/lib` (barrel) export named API objects per domain (`studentGroupService`, `supportService`, …).
- Shared types live in `@/types` (barrel). **Type names are imported from `@/types`, never from a service module.**

### Frontend data-access rules (enforced)

- **Pages/components never call `apiClient` directly** — every request goes through a domain service in `src/lib/<domain>/<domain>Service.ts`. (`grep apiClient src/pages src/components` must be empty.)
- **Services own the API call + URL** (`routes.*`) and return typed results; **types are declared in `src/types/`**, not inside service or page files.
- **Naming convention** (HTTP-method-driven): GET → top-level return `…Response` (nested/shared shapes keep `…Dto`), optional query object `…Request`; POST/PUT/PATCH → `…Request` body + `…Response` result (a 204 mutation returns `void`). Query/pagination objects stay `…Filters`; select options stay `…Option`.

When adding a page or shared component, export it from the matching barrel so it stays importable through `@/...`.
