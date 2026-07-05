# Auth Context — TEDF Frontend

Covers the authentication flow, `AuthContext`, role model, route guarding, and account-state gates. Read this before touching anything in `src/contexts/AuthContext.tsx`, `src/components/auth/ProtectedRoute.tsx`, `src/pages/auth/`, or `src/config/firebase.ts`.

---

## Auth Flow

```
Browser
  │
  │  1. User clicks Login
  ▼
Firebase SDK (src/config/firebase.ts)
  │  • email/password  OR  Google OAuth (hd=fpt.edu.vn)
  │  • returns Firebase ID Token
  ▼
POST /api/auth/session   (authService.getSession)
  │  body: { idToken }
  │  backend validates token with Firebase Admin SDK,
  │  looks up User in SQL Server, issues:
  │  ├─ JWT access token (60 min) — stored in memory (AuthContext state)
  │  └─ Refresh token (7 days) — stored in httpOnly cookie (backend sets)
  ▼
AuthContext.login(jwt)
  │  • decodes JWT → extracts { userId, roles[], email, name }
  │  • sets currentUser state
  │  • triggers RoleBasedRedirect
  ▼
Role-based home route
  admin        → /admin/semesters
  mentor       → /lecturer
  evaluator    → /lecturer
  student      → /student
  departmenthead → /lecturer/dashboard
```

### Token refresh

The `apiClient` (`src/lib/common/apiClient.ts`) intercepts 401 responses and calls `POST /api/auth/refresh` (cookie-based). On success it retries the original request. On failure it calls `AuthContext.logout()`.

---

## AuthContext (`src/contexts/AuthContext.tsx`)

**Provider:** `AuthProvider` — wrap the app root; reads JWT from memory, not localStorage.

**Hook:** `useAuth()` — returns:

| Field / method | Type | Description |
|---|---|---|
| `currentUser` | `AuthUser \| null` | Decoded JWT payload; `null` when unauthenticated |
| `isLoading` | `boolean` | `true` during initial session hydration |
| `activeRole` | `UserRole \| null` | Currently active role (relevant for multi-role users) |
| `login(jwt)` | `(token: string) => void` | Store token, decode user, set state |
| `logout()` | `() => Promise<void>` | Clear state + call backend logout endpoint |
| `switchRole(role)` | `(role: UserRole) => void` | Switch active role for multi-role users |

### `AuthUser` shape

```ts
{
  userId: string
  email: string
  name: string
  roles: UserRole[]        // all roles assigned to this user
  departmentId?: string    // present for DepartmentHead accounts
}
```

### `UserRole` enum values

```ts
"admin" | "mentor" | "evaluator" | "student" | "departmenthead"
```

---

## Route Guarding (`ProtectedRoute`)

`src/components/auth/ProtectedRoute.tsx` — wraps every role subtree in `App.tsx`.

```tsx
<ProtectedRoute allowedRoles={["admin"]}>
  <AdminLayout />
</ProtectedRoute>
```

- If unauthenticated → redirect to `/login`
- If authenticated but wrong role → redirect to `/403`
- If `isLoading` → render nothing (avoids flash)

---

## Account-State Gates

Two conditions block access beyond the login screen:

| Condition | HTTP code from backend | Frontend destination |
|---|---|---|
| Account is locked (`isActive = false`) | 403 `ACCOUNT_LOCKED` | `/blocked` → `AccountBlockedPage` |
| User is not on the semester roster (student/mentor) | 403 `NOT_ELIGIBLE` | `/ineligible` → `IneligiblePage` |

`AuthContext` checks the `GET /api/auth/session` response on app load. If the backend returns one of these codes, `AuthContext` sets the appropriate error state and the router renders the blocked/ineligible page instead of the role home.

---

## MaintenanceContext (`src/contexts/MaintenanceContext.tsx`)

Checks `GET /api/settings/public` on app load (unauthenticated endpoint). If `maintenanceMode: true`:
- Non-admin users → redirect to `/maintenance` (`MaintenancePage`)
- Admin users → allowed through (admin can always access the system)

---

## SystemErrorContext (`src/contexts/SystemErrorContext.tsx`)

Global unhandled-error surface. `apiClient` catches unrecoverable errors (5xx, network failure) and calls `SystemErrorContext.showError(message)`. This renders `SystemErrorModal` over the current page.

---

## SettingsContext / BrandingProvider (`src/contexts/SettingsContext.tsx`)

Fetches `GET /api/settings/public` (branding: primary color, header name, logo URL) on app load and applies:
- CSS custom property `--color-primary` (and its tint variants) to `document.documentElement`
- `localStorage["themeColor"]` as the persisted source across reloads

Local theme override (color picker in SettingsPage) writes to `localStorage` and calls the same CSS-var setter.

---

## Public Routes (no auth required)

| Route | Page |
|---|---|
| `/login` | `LoginPage` |
| `/maintenance` | `MaintenancePage` |
| `/403` | `AccessDeniedPage` |
| `/blocked` | `AccountBlockedPage` |
| `/ineligible` | `IneligiblePage` |
| `*` | `NotFoundPage` |

---

## Firebase Config (`src/config/firebase.ts`)

Firebase is initialized here with `initializeApp(firebaseConfig)`. If `firebaseConfig` is missing/empty (local dev without `.env`), `LoginPage` falls back to a mock login that issues a hardcoded JWT for each role (dev convenience only — never used in production).

Firebase Auth emulator is supported: set `VITE_FIREBASE_USE_EMULATOR=true` in `.env.local` to point the SDK at `localhost:9099`.
