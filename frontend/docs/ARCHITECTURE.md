# TEDF Frontend — Architecture

Architecture reference for the TEDF admin SPA (`frontend/`). A single React application serves all five roles (**Admin, DepartmentHead, Mentor, Student, Evaluator**); each role gets its own layout, sidebar, and guarded route subtree. For backend and cross-cutting context see the root [`../../ARCHITECTURE.md`](../../ARCHITECTURE.md).

---

## Table of Contents

1. [Tech Stack](#1-tech-stack)
2. [Directory Structure](#2-directory-structure)
3. [Application Bootstrap](#3-application-bootstrap)
4. [Routing & Layouts](#4-routing--layouts)
5. [Authentication & Authorization](#5-authentication--authorization)
6. [State Management](#6-state-management)
7. [API Layer](#7-api-layer)
8. [Real-time (SignalR)](#8-real-time-signalr)
9. [Styling & Theming](#9-styling--theming)
10. [Error Handling](#10-error-handling)
11. [Environment Variables](#11-environment-variables)
12. [Build & Tooling](#12-build--tooling)

---

## 1. Tech Stack

| Concern | Choice |
|---------|--------|
| Framework | React 19 |
| Language | TypeScript 5.7 (`strict`, `noUnusedLocals`, `noUnusedParameters`, `noFallthroughCasesInSwitch`) |
| Build / dev server | Vite 6 (`http://localhost:5173`) |
| Routing | React Router 7 (`react-router-dom`) |
| Styling | Tailwind CSS 3.4 + PostCSS; theming via CSS custom properties |
| Animation | Framer Motion 11 (`AnimatePresence` wrapping the route tree) |
| Real-time | `@microsoft/signalr` 10 |
| Auth | Firebase 12 (Firebase Auth → backend-issued JWT) |
| PDF viewing | `react-pdf` 10 + `pdfjs-dist` 5 |
| Dates | `date-fns` 4 |
| Lint | ESLint 9 + `typescript-eslint` 8 |
| Path alias | `@/*` → `./src/*` (`tsconfig.app.json`) |

There is no test runner configured.

---

## 2. Directory Structure

```
src/
├── main.tsx                  # Entry: mounts <App/> into #root
├── App.tsx                   # Provider tree + route table + theme bootstrap
├── index.css                 # Tailwind layers + CSS variables
│
├── pages/                    # Route targets, grouped by role (barrel: pages/index.ts)
│   ├── admin/                #   7 pages   (Dashboard, Users, Projects, Semesters, Settings, Support, ActivityLogs)
│   ├── department-head/      #   2 pages   (Dashboard, AssignEvaluators)
│   ├── mentor/               #   9 pages   (Dashboard, Groups, Topics, TopicPools, Feedback, Schedule, …)
│   ├── student/              #   6 pages   (Dashboard, MyTopic, Topics, Group, Schedule, Support)
│   ├── evaluator/            #   7 pages   (Dashboard, Projects, Review, History, Similarity, Schedule, Support)
│   ├── auth/                 #   LoginPage
│   └── errors/               #   NotFoundPage, AccessDeniedPage
│
├── components/
│   ├── layout/               # 5 role layouts + sidebars + Header + NotificationDropdown (barrel: index.ts)
│   ├── auth/                 # ProtectedRoute (route guard)
│   ├── common/               # Cross-role primitives (e.g. SystemErrorModal)
│   ├── shared/               # Shared widgets
│   ├── admin/ mentor/ student/ support/   # Feature-scoped components
│
├── contexts/                 # AuthContext, MaintenanceContext, SystemErrorContext
├── lib/                      # apiClient + one *Service.ts per domain + fileUploadUtils
├── hooks/                    # useSignalR, useUnreadSupportCount, useWishlist
├── config/                   # firebase.ts (Firebase SDK init)
├── types/                    # admin.types.ts, support.types.ts
└── assets/                   # logo, static assets
```

Per-role narrative docs live in `src/*_CONTEXT.md` (one per role).

---

## 3. Application Bootstrap

`main.tsx` mounts `<App/>`. `App.tsx` establishes the provider tree (order matters — outer providers are available to inner ones and to all routes) and bootstraps the theme.

```
main.tsx
  └─ <App/>
       └─ <MaintenanceProvider>          # maintenance flag (localStorage)
            └─ <AuthProvider>            # user + activeRole, Firebase listener
                 └─ <SystemErrorProvider>  # global error modal
                      └─ <AnimatePresence mode="wait">   # route transitions
                           └─ <Routes> … </Routes>
```

On mount, `App` reads `localStorage["themeColor"]` (default `#2c6090`) and sets three CSS custom properties on `:root` — `--color-primary`, `--color-primary-dark`, `--color-primary-light` — derived via a brightness-adjust helper. Tailwind classes reference these variables, so the whole UI re-themes from a single stored color.

---

## 4. Routing & Layouts

Routing is centralized in `App.tsx`. Public routes are flat; each role mounts a **layout route** wrapped in `ProtectedRoute`, with child routes rendered through the layout's `<Outlet/>`.

```
/login                         → LoginPage              (public)
/maintenance                   → MaintenancePage        (public)
/403                           → AccessDeniedPage       (public)

/admin            → ProtectedRoute(admin)             → AdminLayout
  index settings semesters users projects activity-logs support

/evaluator        → ProtectedRoute(evaluator, mentor) → EvaluatorLayout
  index projects schedule history review/:id review similarity support

/mentor           → ProtectedRoute(mentor, evaluator) → MentorLayout
  index groups groups/:id topics topics/:id schedule support topic-pools topic-pools/:id

/student          → ProtectedRoute(student)           → StudentLayout
  index my-topic topics groups schedule support

/department-head  → ProtectedRoute()                  → DepartmentHeadLayout
  index assign

/                 → RoleBasedRedirect (→ role home, else /login)
*                 → NotFoundPage
```

**Layout shape** — every role layout is a thin shell: a role sidebar plus a `<main>` containing `<Outlet/>` (see `components/layout/`). The `Header` and `NotificationDropdown` provide the top bar and real-time notification feed.

**Role home map** (`App.tsx`): `admin → /admin`, `mentor → /mentor`, `evaluator → /evaluator`, `student → /student`, `departmenthead → /department-head`.

> **Cross-role access:** the `/mentor` and `/evaluator` subtrees each allow *both* `mentor` and `evaluator` roles, matching the domain rule that a user may hold both. The `/department-head` route uses `ProtectedRoute` with no `allowedRoles`, so any authenticated user passes the guard.

---

## 5. Authentication & Authorization

Authentication is delegated to **Firebase Auth**; authorization is driven by **roles parsed from the Firebase ID token (JWT)**. State lives in `AuthContext` (`contexts/AuthContext.tsx`).

```
              ┌─────────────────────────────────────────────┐
  Login  ───► │ Firebase Auth                               │
              │  • signInWithEmailAndPassword               │
              │  • signInWithPopup(Google, hd=fpt.edu.vn)   │
              └───────────────────┬─────────────────────────┘
                                  │  ID token (JWT)
                                  ▼
              ┌─────────────────────────────────────────────┐
              │ AuthContext                                 │
              │  parseRolesFromToken(jwt)  → roles[]        │
              │  firebaseUserToUser()      → User           │
              │  persist user + activeRole to localStorage  │
              │  onAuthStateChanged keeps state in sync     │
              └───────────────────┬─────────────────────────┘
                                  ▼
              ┌─────────────────────────────────────────────┐
              │ ProtectedRoute (per layout route)           │
              │  isLoading        → render nothing          │
              │  !authenticated   → /login                  │
              │  maintenance & !admin → /maintenance        │
              │  allowedRoles miss → /403                   │
              └─────────────────────────────────────────────┘
```

Key points:

- **Roles from the token.** `parseRolesFromToken` base64-decodes the JWT payload and reads `role` / `roles` / the WS-Federation role claim. Values are lower-cased and filtered against the valid set `{admin, mentor, evaluator, student, departmenthead}`; it falls back to `['student']`.
- **Multi-role + active role.** `User.roles` is the full set; `activeRole` is the currently selected one. `switchRole(role)` validates membership, persists, and navigates to that role's home. Both `user` and `activeRole` are persisted in `localStorage` and re-hydrated on load.
- **Auth state sync.** When Firebase is configured, `onAuthStateChanged` refreshes the user and token; `isLoading` gates rendering until the first resolution (so guards don't prematurely redirect).
- **Mock fallback.** When Firebase env is not configured (`useFirebase` false), `login()` derives a mock user from the username — for local development only.
- **Token transport.** The ID token is stored on `user.firebaseToken` and read by the API client and SignalR hook (see below).

---

## 6. State Management

No external state library — state is React Context + `localStorage` + per-page local state (server data is fetched on demand through services). See ADR [`decisions/001-state-management.md`](decisions/001-state-management.md).

| Context | Responsibility | Hook | Persistence |
|---------|----------------|------|-------------|
| `AuthContext` | user, `activeRole`, login/logout, `switchRole`, `isLoading` | `useAuth()` | `localStorage`: `user`, `activeRole` |
| `MaintenanceContext` | maintenance-mode flag | `useMaintenance()` | `localStorage`: `maintenanceMode` |
| `SystemErrorContext` | global error modal (`showError`) | `useSystemError()` | — (in-memory) |

Each context exports a `Provider` + a `use…()` hook that throws if used outside its provider. Theme color is also persisted (`localStorage["themeColor"]`) but applied directly as CSS variables rather than via context.

---

## 7. API Layer

All HTTP traffic goes through `lib/apiClient.ts`, a thin typed wrapper over `fetch`. Domain modules (`lib/<domain>Service.ts`) build typed request/response interfaces and call `apiClient`.

```
Page/Component ─► lib/<domain>Service.ts ─► apiClient ─► fetch ─► TEDF.API
                                                │
                                  unwraps ApiResponse envelope
```

`apiClient` behavior:

- **Base URL** from `VITE_API_BASE_URL` (defaults to `""`, i.e. same origin / Vite proxy).
- **Auth header** — reads the token from `localStorage["user"].firebaseToken` and sends `Authorization: Bearer <token>`.
- **`X-Route-Path` header** — current `window.location.pathname`, used by the backend for activity/error logging.
- **Response envelope** — the backend returns `ApiResponse<T> = { success, message, data?, errors? }`. The client detects the envelope, throws `Error(message)` when `success === false`, and otherwise returns the unwrapped `data`. Non-envelope JSON is returned as-is; empty bodies resolve to `{}`.
- **Error normalization** — on a non-2xx response it parses `{ message, errors }` and throws a single `Error` whose message includes flattened field errors.
- **Verbs** — `get/post/put/patch/delete`, plus `postForm(path, FormData)` for file uploads (lets the browser set the multipart `Content-Type`; see `lib/fileUploadUtils.ts`).

Services follow a consistent shape: exported TypeScript interfaces for payloads/results next to thin functions, e.g. `lib/projectService.ts` exports `ProjectListItem`, `ProjectFilters`, `ProjectDetail`, etc. Current services: `activityLog`, `dashboard`, `departmentHead`, `directTopic`, `evaluator`, `mentorTopic`, `project`, `studentGroup`, `topicPool`, `user`.

---

## 8. Real-time (SignalR)

`hooks/useSignalR.ts` manages a connection to the backend notifications hub.

- Connects to `${VITE_API_BASE_URL}/hubs/notifications` with `accessTokenFactory` returning the stored Firebase token (auth via `?access_token=`).
- `withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` and `LogLevel.Warning`.
- Subscribes to the server event `ReceiveNotification`; the latest `onReceiveNotification` callback is held in a ref so handlers stay current without reconnecting.
- Starts on mount and stops/cleans up on unmount. The connection is skipped when no token is present.

The notification UI (`components/layout/NotificationDropdown`) consumes this to render the live feed. The backend also exposes a `/hubs/chat` hub for messaging.

---

## 9. Styling & Theming

- **Tailwind CSS 3.4** is the styling system (see ADR [`decisions/002-styling-approach.md`](decisions/002-styling-approach.md)); utilities are composed inline in components. Global layers and CSS variables live in `index.css`.
- **Dynamic theming** uses CSS custom properties set on `:root` at startup: `--color-primary`, `--color-primary-dark`, `--color-primary-light`. Changing `localStorage["themeColor"]` and re-applying these variables re-skins the app without a rebuild.
- **Animation** — `AnimatePresence` (Framer Motion) wraps `<Routes>` for page transitions; individual components use `motion.*` elements.

---

## 10. Error Handling

Two complementary layers:

1. **API-level** — `apiClient` converts both transport failures and `success:false` envelopes into thrown `Error`s with human-readable messages (including server field errors). Callers surface these in-page or via `useSystemError().showError(message)`.
2. **Global modal** — `SystemErrorProvider` renders a `SystemErrorModal` and exposes `showError(message)` for unexpected/system-level failures, decoupled from any single page.

Route-level fallbacks: unknown paths render `NotFoundPage`, forbidden access redirects to `/403` (`AccessDeniedPage`), and maintenance mode redirects non-admins to `/maintenance`.

---

## 11. Environment Variables

Vite env vars (prefix `VITE_`, read via `import.meta.env`):

| Variable | Purpose |
|----------|---------|
| `VITE_API_BASE_URL` | Base URL for REST + SignalR; empty = same origin |
| `VITE_FIREBASE_API_KEY` | Firebase web API key (also gates real vs. mock auth) |
| `VITE_FIREBASE_PROJECT_ID` | Firebase project id |
| `VITE_USE_FIREBASE_EMULATOR` | `"true"` to target the Firebase Auth emulator |
| `VITE_FIREBASE_EMULATOR_HOST` | Emulator host when emulator mode is on |

When no real Firebase API key is present, the app falls back to mock login (development only).

---

## 12. Build & Tooling

```powershell
npm install
npm run dev      # Vite dev server -> http://localhost:5173
npm run build    # tsc -b && vite build  (type-check then bundle)
npm run lint     # ESLint
npm run preview  # serve the production build locally
```

- **TypeScript** runs in `strict` mode with unused-symbol checks; `build` fails on type errors (`tsc -b` precedes `vite build`).
- **Path alias** `@/*` resolves to `src/*` (keep imports alias-based, not deep relative paths).
- **Barrels** — export new pages/components from the relevant `index.ts` so they remain importable via `@/pages`, `@/components/layout`, etc.
