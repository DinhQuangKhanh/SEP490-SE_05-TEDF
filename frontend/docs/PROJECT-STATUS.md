# TEDF Frontend — Project Status

Snapshot of what is implemented in the TEDF SPA (`frontend/`), per page and per feature. Status is inferred from the source — whether a page is wired to the API layer (a `lib/<domain>/<domain>Service.ts` over `apiClient`) or still renders static/mock data.

**Last updated:** 2026-07-05

> This is a living document. When a page moves from mock to live data (or a new feature lands), update its row here.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done — wired to the backend via a service, functional |
| 🚧 | Partial — some live data, but parts still static/mock or backend-incomplete |
| 📋 | Mock — UI built but rendering hardcoded/placeholder data, no API |
| ❌ | Not started — no frontend implementation |

## Summary

- **Core lifecycle is live:** auth, role routing, dashboards (mentor/dept-head/evaluator), users, projects, semesters, groups, topic pools, evaluations, support tickets, and real-time notifications are wired to the API across all five roles.
- **Profile page is live** (shared `ProfilePage` component, routed at `/<role>/profile`) with edit modal, privacy toggle, role-specific fields (MajorProgram for students, Division for mentors/evaluators), and supervised-projects list.
- **Semester roster is live** (`SemesterRosterPage` at `/admin/semesters/:id/roster`): import eligible students/mentors, assign major programs, bulk-delete, and publish roster.
- **Admin home redirects to Semesters** (`/admin → /admin/semesters`): the old DashboardPage and ProjectsPage for admin have been removed; their content is accessible through dedicated pages.
- **Account access gate** is enforced: locked or ineligible accounts are redirected to `AccountBlockedPage` or `IneligiblePage` at login.
- **Real-time notifications** work (SignalR `NotificationHub` + `NotificationDropdown` + click-to-navigate).
- **`lib/` + `types/` are feature-based**, mirroring the backend `TEDF.API/Endpoints/` folders (camelCase) with barrels (`@/lib`, `@/types`); URLs centralized in `lib/common/routes.ts`. Pages/components go through services only — no direct `apiClient` calls. (See [`PROJECT-RULES.md`](PROJECT-RULES.md) §5.)

---

## By Role

### Admin (`pages/admin/`)

| Page | Route | Status | Notes |
|------|-------|--------|-------|
| ~~DashboardPage~~ | ~~`/admin`~~ | **Removed** | Admin home now redirects to `/admin/semesters` |
| SemestersPage | `/admin/semesters` | ✅ | semester service + Create/Edit modals; home page for admin |
| SemesterRosterPage | `/admin/semesters/:id/roster` | ✅ | Import eligible students/mentors, assign major programs, bulk-delete, publish |
| UsersPage | `/admin/users` | ✅ | `userService` — list, lock/unlock, assign department head |
| ~~ProjectsPage~~ | ~~`/admin/projects`~~ | **Removed** | Project oversight moved to dept-head flow |
| ActivityLogsPage | `/admin/activity-logs` | ✅ | `activityLogService` |
| SettingsPage | `/admin/settings` | ✅ | `settingsService` — system config, branding (color/logo/header), test email; theme color persisted in `localStorage` |
| SupportPage | `/admin/support` | ✅ | `supportService` |
| ProfilePage | `/admin/profile` | ✅ | Shared `ProfilePage`: view/edit profile, privacy toggle, supervised-projects list |

### Department Head (`pages/department-head/`)

Rendered inside the Lecturer layout; routed at `/lecturer/*` for accounts granted the DepartmentHead role.

| Page | Route | Status | Notes |
|------|-------|--------|-------|
| DepartmentHeadDashboardPage | `/lecturer/dashboard` | ✅ | overview + alerts (DepartmentHead only) |
| AssignEvaluatorsPage | `/lecturer/assign` `/lecturer/assign/:tab` | ✅ | evaluator assignment + final decision, tabbed |

### Lecturer (`pages/lecturer/`) — Mentor + Evaluator, unified

| Page | Route | Status | Notes |
|------|-------|--------|-------|
| LecturerRepositoryPage | `/lecturer` `/lecturer/registrations` | ✅ | Own topics + "My topics" tab for dept-head; pool-topic registration status (real-time via SignalR) |
| LecturerGroupsPage | `/lecturer/groups` | ✅ | assigned groups |
| LecturerGroupDetailPage | `/lecturer/groups/:id` | 📋 | group/topic detail — hardcoded sample data |
| TopicCreatePage | `/lecturer/create` | ✅ | Multi-step wizard modal (`proposeTopic`); separate description field |
| LecturerModerationPage | `/lecturer/moderate` | ✅ | evaluation queue (`evaluatorService`) |
| LecturerReviewPage | `/lecturer/moderate/:id` | ✅ | review / submit result |
| LecturerHistoryPage | `/lecturer/history` | ✅ | evaluation history |
| SupervisedProjectsPage | `/lecturer/supervised-projects` | ✅ | mentor's supervised project list/detail (`projectService.supervisedProjects`) |
| LecturerSupportPage | `/lecturer/support` | ✅ | support tickets |
| ProfilePage | `/lecturer/profile` | ✅ | Shared `ProfilePage`: edit, privacy, Division field, supervised-projects list |

> There is **no separate evaluator page folder** — evaluators use the Lecturer layout, routes, and pages.

### Student (`pages/student/`)

| Page | Route | Status | Notes |
|------|-------|--------|-------|
| StudentDashboardPage | `/student` | ✅ | dashboard |
| StudentGroupPage | `/student/groups` `/student/groups/:tab` | ✅ | `studentGroupService` (create/invite/join); bulk approve/reject join requests |
| StudentMyTopicPage | `/student/my-topic` | ✅ | direct topic + pool registration detail view + in-place file preview; pending/rejected views unified |
| StudentTopicsPage | `/student/topics` | ✅ | browse/register from pool; rich-text note + file viewer; one pending registration per group enforced |
| StudentSupportPage | `/student/support` | ✅ | support tickets |
| ProfilePage | `/student/profile` | ✅ | Shared `ProfilePage`: edit, privacy, MajorProgram (chuyên ngành hẹp) field |

### Auth & Shared (`pages/auth/`, `pages/errors/`, root)

| Page | Route | Status | Notes |
|------|-------|--------|-------|
| LoginPage | `/login` | ✅ | Firebase email/password + Google (`hd=fpt.edu.vn`); mock fallback when Firebase unconfigured |
| IneligiblePage | `/ineligible` | ✅ | Shown when student/mentor is not on the semester roster |
| MaintenancePage | `/maintenance` | ✅ | gated by `MaintenanceContext` |
| NotFoundPage | `*` | ✅ | 404 |
| AccessDeniedPage | `/403` | ✅ | access denied |
| AccountBlockedPage | `/blocked` | ✅ | shown when account is locked or ineligible; added by AccountAccessGate |

---

## Cross-Cutting Features

| Feature | Status | Notes |
|---------|--------|-------|
| Authentication (Firebase + JWT roles) | ✅ | `AuthContext`, role parsed from token |
| Authorization / route guarding | ✅ | `ProtectedRoute`, multi-role, `allowedRoles` |
| Role switching | ✅ | `switchRole` for multi-role users |
| Dynamic theming | ✅ | `--color-primary*` from `localStorage["themeColor"]` + `SettingsContext` (branding from backend) |
| Real-time notifications | ✅ | `useSignalR` → `/hubs/notifications`, `NotificationDropdown`, click-to-navigate, per-tab routing |
| Real-time project status sync | ✅ | `useSignalR` + `signalREvents.ts` → `RegistrationUpdate` listener on `LecturerRepositoryPage` |
| Unread notification count sync | ✅ | SignalR-driven badge on `NotificationDropdown` |
| File upload / download | ✅ | `apiClient.postForm` + `fileUploadUtils` |
| Support tickets | ✅ | implemented for admin, mentor, student, evaluator |
| Maintenance mode | ✅ | `MaintenanceContext`, non-admins redirected |
| Account access gate | ✅ | Locked/ineligible accounts blocked in `AuthContext` → `AccountBlockedPage` / `IneligiblePage` |
| Global error modal | ✅ | `SystemErrorContext` + `SystemErrorModal` |
| User profile (view + edit) | ✅ | Shared `ProfilePage` across all roles; edit modal, privacy toggle, supervised-projects list |
| Semester roster management | ✅ | `SemesterRosterPage`: import CSV, assign majors, bulk-delete, publish |
| Supervised projects | ✅ | `SupervisedProjectsPage` for mentor/evaluator; modal detail view |
| Topic pool registration (student) | ✅ | Rich-text note, file viewer, mentor/dept-head request tab (real-time); cancel registration |
| Propose topic — multi-step wizard | ✅ | `TopicCreatePage` rebuilt as wizard modal; separate description field |
| Bulk join-request approve/reject | ✅ | `StudentGroupPage` — group leader bulk-manages join requests |
| Schedules / meetings | 🚧 | Calendar UI exists but is static across roles; not wired to a meetings API |
| Similarity detection | 📋 | Evaluator UI renders mock data only |
| Report export (PDF/Excel) | ❌ | No frontend; backend reporting still in development |
| Real-time chat | ❌ | No chat page/component; `/hubs/chat` exists backend-side but is unused by the SPA |

---

## Service Layer Coverage

Wired API services in `src/lib/<feature>/` (barrel `@/lib`), mirroring the backend feature folders:

| Service module | Key exports | Notes |
|---|---|---|
| `activityLogService` | log list, grouped, severity, error detail | admin only |
| `archiveService` | archive list, download | admin only |
| `authService` | session check | auth flow |
| `dashboardService` | admin/mentor/departmentHead/evaluator dashboards | all four role dashboards |
| `proposedTopicService` | directRegistration CRUD, submit-to-mentor, available-mentors | student direct-registration flow |
| `evaluatorService` | evaluator self-service + dept-head evaluator management | |
| `studentGroupService` | create/invite/join + bulk approve/reject + invitable-students picker | |
| `majorService` | major list | |
| `notificationService` | list, unread count, mark read/all-read | |
| `projectService` | admin project list, dept-head dept projects, mentor supervised projects | |
| `semesterService` + `semesterValidation` | semester CRUD + roster (import, bulk-delete, publish, major update) | |
| `settingsService` | system config GET/PUT, logo upload, test email, public settings | |
| `supportService` | ticket CRUD, reply, status change, stats | |
| `topicService` + `topicStatus` | topic catalog, mentor topics, topic detail/documents | |
| `topicPoolService` | pool list, propose, registration CRUD, note-attachment, mentor registrations | |
| `userService` | user list, lock/unlock, assign dept-head, profile GET/PUT | |

**No service module yet** for: meetings/schedule, chat, and reports — these correspond to the 🚧 / ❌ items above.

---

## Known Gaps / Follow-ups

- **Schedule pages** (mentor, evaluator, and the calendar half of student) render hardcoded events — needs a meetings/schedule service once the backend Meetings feature is built.
- **`LecturerGroupDetailPage`** shows hardcoded group/topic data — needs wiring to a real group-detail endpoint.
- **`EvaluatorSimilarityPage`** (`/lecturer/moderate/:id` similarity tab) is a mock; depends on a backend title-similarity feature endpoint.
- **Report export** and **real-time chat** have no SPA implementation yet.
