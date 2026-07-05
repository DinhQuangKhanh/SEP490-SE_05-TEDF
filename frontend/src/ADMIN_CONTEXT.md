# Admin Context — TEDF Frontend

Covers everything inside the `admin` role area. Read this before touching pages, components, or services that live under `src/pages/admin/` or are consumed exclusively by the admin role.

---

## Role & Layout

- **Role string:** `"admin"`
- **Layout:** `AdminLayout` (`src/components/layout/AdminLayout.tsx`)
- **Sidebar:** `Sidebar` (shared shell, `src/components/layout/Sidebar.tsx` + `SidebarShell.tsx`)
- **Home route:** `/admin` → redirects to `/admin/semesters`
- **Protected by:** `ProtectedRoute allowedRoles={["admin"]}`

---

## Route Map

| Route | Page | Status | API service |
|---|---|---|---|
| `/admin` (index) | redirect → `/admin/semesters` | — | — |
| `/admin/semesters` | `SemestersPage` | ✅ | `semesterService` |
| `/admin/semesters/:id/roster` | `SemesterRosterPage` | ✅ | `semesterService` (roster sub-routes) |
| `/admin/users` | `UsersPage` | ✅ | `userService` |
| `/admin/activity-logs` | `ActivityLogsPage` | ✅ | `activityLogService` |
| `/admin/settings` | `SettingsPage` | ✅ | `settingsService` |
| `/admin/support` | `SupportPage` | ✅ | `supportService` |
| `/admin/profile` | `ProfilePage` (shared) | ✅ | `userService` |

---

## Pages

### `SemestersPage` — `/admin/semesters`
The admin home page. Displays all semesters with their phases. Admins can:
- Create a new semester (modal: name, start/end dates, phases)
- Edit an existing semester
- Delete a semester
- Navigate to the semester's roster page

API: `semesterService.getAll()`, `semesterService.create()`, `semesterService.update()`, `semesterService.delete()`

### `SemesterRosterPage` — `/admin/semesters/:id/roster`
Manages the eligible-student and eligible-mentor roster for a semester.
- Two tabs: **Students** and **Mentors**
- Import via CSV (`POST .../eligible-students/import` / `.../eligible-mentors/import`)
- Search + pagination
- Assign major program per mentor (`PUT .../eligible-mentors/{mentorId}/major`)
- Checkbox selection + **bulk delete** (`POST .../eligible-students/bulk-delete`)
- **Publish roster** (`POST .../roster/publish`) — triggers batch email to eligible students + SignalR notification to mentors

API: `semesterService.getEligibleStudents()`, `semesterService.getEligibleMentors()`, `semesterService.importStudents()`, `semesterService.importMentors()`, `semesterService.bulkDeleteStudents()`, `semesterService.bulkDeleteMentors()`, `semesterService.updateMentorMajor()`, `semesterService.publishRoster()`

### `UsersPage` — `/admin/users`
Full user list with search and pagination.
- Lock / unlock accounts (PUT `.../lock`, `.../unlock`)
- Assign department head role (POST `/api/users/departments/{id}/head`)

API: `userService.getUsers()`, `userService.lockUser()`, `userService.unlockUser()`, `userService.assignDepartmentHead()`

### `ActivityLogsPage` — `/admin/activity-logs`
Multi-tab activity and error log viewer.
- Grouped activity logs with severity filter
- Individual error log detail modal

API: `activityLogService.getLogs()`, `activityLogService.getGroupedLogs()`, `activityLogService.getSeveritySummary()`, `activityLogService.getErrorDetails()`, `activityLogService.getErrorLogDetail(id)`

### `SettingsPage` — `/admin/settings`
System-wide configuration panel.
- **Branding:** primary color picker (writes CSS var + `localStorage`; calls `PUT /api/settings` to persist), header name, logo upload (`POST /api/settings/logo`)
- **Registration rules:** `MaxGroupMembers`, `AllowDirectRegistration`, `RequireOutlineApproval`, `MaxTopicsPerMentor`
- **Notification toggles:** `EmailOnEvaluationResult`, `NotifyMentorOnRegistration`
- **Maintenance mode** toggle
- **Test email** button (`POST /api/settings/test-email`)

API: `settingsService.getSettings()`, `settingsService.updateSettings()`, `settingsService.uploadLogo()`, `settingsService.sendTestEmail()`

### `SupportPage` — `/admin/support`
Support ticket management for admin.
- Ticket list with stats summary
- Ticket detail + reply
- Status change (assign, resolve, close, reopen)

API: `supportService.getStats()`, `supportService.getTickets()`, `supportService.getTicket(id)`, `supportService.reply()`, `supportService.updateStatus()`

### `ProfilePage` (shared) — `/admin/profile`
Shared profile page rendered for all roles. See `src/STUDENT_CONTEXT.md` for the common profile spec.
For admin: shows admin-specific fields (no MajorProgram, no Division).

---

## Key Concepts

### No admin DashboardPage
The old admin `DashboardPage` and `ProjectsPage` were removed. The admin's entry point is `SemestersPage`. Project oversight is handled through the department-head flow (`/api/projects/department`) and individual project actions within evaluation pages.

### Semester roster publication flow
```
Admin imports CSV → reviews eligible students/mentors
→ assigns major programs to mentors (if needed)
→ bulk-deletes incorrect entries
→ clicks Publish
    → backend: EnqueueStudentEmailsOnRosterPublishedHandler (batch email job)
    → backend: NotifyMentorsOnRosterPublishedHandler (SignalR)
    → students and mentors gain access (AccountAccessGate checks roster)
```

### System settings are backend-persisted
Settings are stored in `SystemConfiguration` (SQL Server), cached in Redis/Memory, and served via `/api/settings`. The frontend `SettingsContext` (BrandingProvider) fetches `/api/settings/public` on app load and applies branding system-wide. Admins see the full settings object via `/api/settings` (authenticated).

---

## Services Used

| Service | Import | Notes |
|---|---|---|
| `semesterService` | `@/lib` | Semester + full roster sub-routes |
| `userService` | `@/lib` | User list, lock/unlock, assign dept-head, profile |
| `activityLogService` | `@/lib` | Log queries |
| `settingsService` | `@/lib` | System config CRUD |
| `supportService` | `@/lib` | Support ticket management |

---

## Components (admin-specific, `src/components/admin/`)

Check `src/components/admin/` for admin-only components (modals, drawers, tables). Common layout components live in `src/components/layout/`.
