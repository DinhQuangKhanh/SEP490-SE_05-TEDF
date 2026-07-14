# Student Context — TEDF Frontend

Covers everything inside the `student` role area. Read this before touching pages, components, or services consumed by students.

---

## Role & Layout

- **Role string:** `"student"`
- **Layout:** `StudentLayout` (`src/components/layout/StudentLayout.tsx`)
- **Sidebar:** `StudentSidebar` (`src/components/layout/StudentSidebar.tsx`)
- **Home route:** `/student` → `StudentDashboardPage`
- **Protected by:** `ProtectedRoute allowedRoles={["student"]}`

---

## Route Map

| Route | Page | Status | API service |
|---|---|---|---|
| `/student` (index) | `StudentDashboardPage` | ✅ | `dashboardService` |
| `/student/groups` | `StudentGroupPage` | ✅ | `studentGroupService` |
| `/student/groups/:tab` | `StudentGroupPage` (tab parameter) | ✅ | `studentGroupService` |
| `/student/my-topic` | `StudentMyTopicPage` | ✅ | `proposedTopicService`, `topicPoolService` |
| `/student/topics` | `StudentTopicsPage` | ✅ | `topicService`, `topicPoolService` |
| `/student/support` | `StudentSupportPage` | ✅ | `supportService` |
| `/student/profile` | `ProfilePage` (shared) | ✅ | `userService` |

---

## Pages

### `StudentDashboardPage` — `/student`
Overview of the student's current semester status.
- Active semester info, group status, topic registration status
- Quick links to active tasks

API: `dashboardService.getStudentDashboard()` (via role-based endpoint)

### `StudentGroupPage` — `/student/groups` `/student/groups/:tab`
Full group management UI. Tabs:
- **My Group** — if the student is in a group: group info, member list, invite member, remove member, leave group
- **Join** — if not in a group: browse open groups, send join request, or create a new group
- **Invitations** — pending invitations from other groups (accept/decline)
- **Join Requests** — (for group leader only) list of pending join requests; bulk approve / bulk reject

API: `studentGroupService.getMyGroup()`, `studentGroupService.getOpenGroups()`, `studentGroupService.createGroup()`, `studentGroupService.invite()`, `studentGroupService.respondToInvitation()`, `studentGroupService.requestJoin()`, `studentGroupService.getJoinRequests()`, `studentGroupService.bulkApproveJoinRequests()`, `studentGroupService.bulkRejectJoinRequests()`, `studentGroupService.getInvitableStudents()`

### `StudentMyTopicPage` — `/student/my-topic`
The student's active topic/project view. Handles both registration paths:

**DirectRegistration path:**
- Create/edit direct topic form (title, description, technology, mentor selection)
- Submit to mentor
- View mentor feedback (modification requests)
- Re-edit and re-submit
- Topic documents/files

**Pool registration path:**
- View the group's current pool-topic registration(s)
- Registration detail: note (rich text), attachment(s)
- Registration status (pending / confirmed / rejected / cancelled) — updated in real-time via SignalR
- Cancel a pending registration

Both pending and rejected views are unified in a single component (no separate pages).

API: `proposedTopicService.create()`, `proposedTopicService.update()`, `proposedTopicService.submit()`, `proposedTopicService.getAvailableMentors()`, `topicPoolService.getMyRegistrations()`, `topicPoolService.cancelRegistration()`

### `StudentTopicsPage` — `/student/topics`
Browse and register from the Topic Pool.
- Filter by department, search by title
- Topic card with mentor info, tech stack, available slots
- Registration modal: rich-text note, file upload, one active registration per group enforced
- View own registration status

Rules enforced on frontend:
- A group can have only one pending pool registration at a time
- Student must match the topic's required major (enforced by backend `GET /api/direct-topics/available-mentors`)

API: `topicService.getTopics()`, `topicPoolService.getTopicPools()`, `topicPoolService.register()`, `topicPoolService.uploadNoteAttachment()`

### `StudentSupportPage` — `/student/support`
Support ticket management for students.
- Create ticket, view ticket list, reply, view status

API: `supportService.getStats()`, `supportService.getTickets()`, `supportService.getTicket(id)`, `supportService.createTicket()`, `supportService.reply()`

### `ProfilePage` (shared) — `/student/profile`
Profile view/edit shared across all roles.

**Student-specific fields:**
- `MajorProgram` (chuyên ngành hẹp) — display-only; assigned by admin on semester roster
- `Division` — not applicable for students

**Common fields (all roles):**
- Full name (display-only; managed via Firebase)
- Phone number (editable)
- Birth date (editable)
- Email (display-only)
- Privacy settings toggle (show/hide phone, birth date to other users)

**Edit flow:** click "Edit" → modal opens → PUT `/api/users/me` → profile refreshes.

---

## Key Concepts

### Two topic registration paths
A student group can pursue only one path at a time (enforced by backend):

| Path | Entry point | Flow |
|---|---|---|
| **FromPool** | `StudentTopicsPage` → register | register → mentor confirms/rejects → if confirmed → project created → evaluation |
| **DirectRegistration** | `StudentMyTopicPage` → create | create → submit to mentor → mentor approves → evaluation; if modification → re-edit → re-submit |

### Ineligibility gate
If a student is not on the published semester roster, the backend returns `403 NOT_ELIGIBLE` on protected endpoints. `AuthContext` catches this on app load and redirects to `IneligiblePage` (`/ineligible`).

### Real-time pool registration status
`StudentMyTopicPage` (or `LecturerRepositoryPage` on the mentor side) listens to `RegistrationUpdate` SignalR events from `NotificationHub`. When a mentor confirms or rejects a registration, the page re-fetches and updates the status badge without a full reload.

---

## Services Used

| Service | Import | Notes |
|---|---|---|
| `dashboardService` | `@/lib` | Student dashboard data |
| `studentGroupService` | `@/lib` | Full group lifecycle |
| `proposedTopicService` | `@/lib` | Direct-registration topic CRUD + submit |
| `topicService` | `@/lib` | Topic catalog (browse pool topics) |
| `topicPoolService` | `@/lib` | Pool registration + note-attachment upload |
| `supportService` | `@/lib` | Support tickets |
| `userService` | `@/lib` | Profile GET/PUT |
