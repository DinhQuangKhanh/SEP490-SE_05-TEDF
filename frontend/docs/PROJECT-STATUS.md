# TEDF Frontend — Project Status

Snapshot of what is implemented in the TEDF admin SPA (`frontend/`), per page and per feature. Status is inferred from the source — whether a page is wired to the API layer (`lib/*Service.ts` + `apiClient`) or still renders static/mock data.

**Last updated:** 2026-06-01

> This is a living document. When a page moves from mock to live data (or a new feature lands), update its row here.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done — wired to the backend via a service, functional |
| 🚧 | Partial — some live data, but parts still static/mock or backend-incomplete |
| 📋 | Mock — UI built but rendering hardcoded/placeholder data, no API |
| ❌ | Not started — no frontend implementation |

## Summary

- **Core lifecycle is live:** auth, role routing, dashboards, users, projects, semesters, groups, topic pools, evaluations, and support tickets are wired to the API across all five roles.
- **Real-time notifications** work (SignalR + `NotificationDropdown`).
- **Still mock / not wired:** schedules & meetings, evaluator similarity, the mentor feedback page, and report exports. **Real-time chat** has no frontend yet.

---

## By Role

### Admin (`pages/admin/`)

| Page | Status | Notes |
|------|--------|-------|
| DashboardPage | ✅ | `dashboardService` |
| UsersPage | ✅ | `userService` |
| ProjectsPage | ✅ | `projectService` + `ProjectDetailDrawer` |
| SemestersPage | ✅ | semester service + Create/Edit modals |
| SupportPage | ✅ | support tickets |
| ActivityLogsPage | ✅ | `activityLogService` |
| SettingsPage | 🚧 | Theme color is functional (CSS vars + `localStorage`); no backend-persisted system settings |

### Department Head (`pages/department-head/`)

| Page | Status | Notes |
|------|--------|-------|
| DepartmentHeadDashboardPage | ✅ | `departmentHeadService` |
| AssignEvaluatorsPage | ✅ | evaluator assignment + final decision |

### Mentor (`pages/mentor/`)

| Page | Status | Notes |
|------|--------|-------|
| MentorDashboardPage | ✅ | `mentorTopicService` / dashboard |
| MentorGroupsPage | ✅ | assigned groups |
| MentorTopicsPage | ✅ | topic management |
| MentorTopicDetailPage | ✅ | topic detail / review |
| TopicPoolsPage | ✅ | `topicPoolService` |
| TopicPoolDetailPage | ✅ | pool detail + edit/resubmit |
| MentorSupportPage | ✅ | support tickets |
| MentorFeedbackPage | 📋 | Hardcoded `technologies` / `hardware` / `feedbackHistory` — no API |
| MentorSchedulePage | 📋 | Static calendar (hardcoded events/groups) |

### Student (`pages/student/`)

| Page | Status | Notes |
|------|--------|-------|
| StudentDashboardPage | ✅ | dashboard |
| StudentGroupPage | ✅ | `studentGroupService` (create/invite/join) |
| StudentMyTopicPage | ✅ | direct topic + documents |
| StudentTopicsPage | ✅ | browse/register topics |
| StudentSupportPage | ✅ | support tickets |
| StudentSchedulePage | 🚧 | Pulls group data via `studentGroupService`; calendar events still hardcoded |

### Evaluator (`pages/evaluator/`)

| Page | Status | Notes |
|------|--------|-------|
| EvaluatorDashboardPage | ✅ | `evaluatorService` |
| EvaluatorProjectsPage | ✅ | assigned projects |
| EvaluatorReviewPage | ✅ | review/submit result (`review/:id`) |
| EvaluatorHistoryPage | ✅ | evaluation history |
| EvaluatorSupportPage | ✅ | support tickets |
| EvaluatorSchedulePage | 📋 | Static calendar (hardcoded events) |
| EvaluatorSimilarityPage | 📋 | Mock similarity data only — no detection backend |

### Auth & Shared (`pages/auth/`, `pages/errors/`, root)

| Page | Status | Notes |
|------|--------|-------|
| LoginPage | ✅ | Firebase email/password + Google (`hd=fpt.edu.vn`); mock fallback when Firebase unconfigured |
| MaintenancePage | ✅ | gated by `MaintenanceContext` |
| NotFoundPage / AccessDeniedPage | ✅ | `*` → 404, `/403` → access denied |

---

## Cross-Cutting Features

| Feature | Status | Notes |
|---------|--------|-------|
| Authentication (Firebase + JWT roles) | ✅ | `AuthContext`, role parsed from token |
| Authorization / route guarding | ✅ | `ProtectedRoute`, multi-role, `allowedRoles` |
| Role switching | ✅ | `switchRole` for multi-role users |
| Dynamic theming | ✅ | `--color-primary*` from `localStorage["themeColor"]` |
| Real-time notifications | ✅ | `useSignalR` → `/hubs/notifications`, `NotificationDropdown` |
| File upload / download | ✅ | `apiClient.postForm` + `fileUploadUtils` |
| Support tickets | ✅ | implemented for admin, mentor, student, evaluator |
| Maintenance mode | ✅ | `MaintenanceContext`, non-admins redirected |
| Global error modal | ✅ | `SystemErrorContext` + `SystemErrorModal` |
| Schedules / meetings | 🚧 | Calendar UI exists but is static across roles; not wired to a meetings API |
| Similarity detection | 📋 | Evaluator UI renders mock data only |
| Report export (PDF/Excel) | ❌ | No frontend; backend reporting still in development |
| Real-time chat | ❌ | No chat page/component; `/hubs/chat` exists backend-side but is unused by the SPA |

---

## Service Layer Coverage

Wired API clients in `src/lib/` (10): `activityLog`, `dashboard`, `departmentHead`, `directTopic`, `evaluator`, `mentorTopic`, `project`, `studentGroup`, `topicPool`, `user`.

**No service module yet** for: meetings/schedule, chat, notifications list, and reports — these correspond to the 🚧 / 📋 / ❌ items above.

---

## Known Gaps / Follow-ups

- **Schedule pages** (mentor, evaluator, and the calendar half of student) render hardcoded events — needs a meetings/schedule service.
- **`EvaluatorSimilarityPage`** is a mock; depends on a backend similarity feature.
- **`MentorFeedbackPage`** shows hardcoded feedback/tech/hardware; needs wiring to real submission data.
- **`SettingsPage`** persists only the theme color locally; backend system-configuration is not connected.
- **Report export** and **real-time chat** have no SPA implementation yet.
