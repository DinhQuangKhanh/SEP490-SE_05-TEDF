# Lecturer Context — TEDF Frontend

Covers everything inside the Lecturer layout, which is shared by three roles: **Mentor**, **Evaluator**, and **DepartmentHead**. Read this before touching `src/pages/lecturer/`, `src/pages/department-head/`, or `src/components/lecturer/`.

---

## Roles & Layout

- **Role strings:** `"mentor"` | `"evaluator"` | `"departmenthead"`
- **Layout:** `LecturerLayout` (`src/components/layout/LecturerLayout.tsx`)
- **Sidebar:** `LecturerSidebar` (`src/components/layout/LecturerSidebar.tsx`) — renders different nav items per active role
- **Home routes:**
  - mentor → `/lecturer` (LecturerRepositoryPage — "My Topics")
  - evaluator → `/lecturer` (LecturerRepositoryPage — re-used as moderator entry, redirects to `/lecturer/moderate`)
  - departmenthead → `/lecturer/dashboard`
- **Protected by:** `ProtectedRoute allowedRoles={["mentor", "evaluator", "departmenthead"]}`

There is **no separate evaluator page folder** — evaluators share the Lecturer pages. DepartmentHead-only pages live in `src/pages/department-head/` but are rendered inside the Lecturer layout.

---

## Route Map

| Route | Page | Roles | Status | Notes |
|---|---|---|---|---|
| `/lecturer` | `LecturerRepositoryPage` | mentor, depthead | ✅ | "My Topics" tab; dept-head also sees "My Topics" tab via `MentorTopicsPanel` |
| `/lecturer/registrations` | `LecturerRepositoryPage` | mentor | ✅ | Pool-topic registration requests tab (real-time) |
| `/lecturer/groups` | `LecturerGroupsPage` | mentor | ✅ | Assigned student groups |
| `/lecturer/groups/:id` | `LecturerGroupDetailPage` | mentor | 📋 | Group/topic detail — hardcoded sample data |
| `/lecturer/create` | `TopicCreatePage` | mentor | ✅ | Multi-step wizard modal to propose a pool topic |
| `/lecturer/moderate` | `LecturerModerationPage` | evaluator | ✅ | Evaluation queue |
| `/lecturer/moderate/:id` | `LecturerReviewPage` | evaluator | ✅ | Review + submit result |
| `/lecturer/history` | `LecturerHistoryPage` | evaluator | ✅ | Evaluation history |
| `/lecturer/supervised-projects` | `SupervisedProjectsPage` | mentor, evaluator | ✅ | Mentor's supervised project list + detail modal |
| `/lecturer/support` | `LecturerSupportPage` | mentor, evaluator, depthead | ✅ | Support tickets |
| `/lecturer/profile` | `ProfilePage` (shared) | mentor, evaluator, depthead | ✅ | Edit profile, Division field, supervised-projects list |
| `/lecturer/dashboard` | `DepartmentHeadDashboardPage` | **depthead only** | ✅ | Dept overview + alerts |
| `/lecturer/assign` | `AssignEvaluatorsPage` | **depthead only** | ✅ | Evaluator assignment + final decision |
| `/lecturer/assign/:tab` | `AssignEvaluatorsPage` | **depthead only** | ✅ | Tabbed: pending-projects / assign-evaluator / final-decision |

---

## Pages (Mentor-focused)

### `LecturerRepositoryPage` — `/lecturer` `/lecturer/registrations`
Two tabs:
1. **My Topics** — list of pool topics the mentor has proposed; status (active/suspended/expired); topic edit/resubmit actions after evaluator feedback (`topicPoolService.updateTopic()`, `.resubmitTopic()`)
2. **Registrations** — student group registration requests on the mentor's pool topics; confirm/reject with real-time status update (`topicPoolService.confirmRegistration()`, `.rejectRegistration()`)

For DepartmentHead, the "My Topics" tab is rendered via the shared `MentorTopicsPanel` component.

API: `topicService.getMentorTopics()`, `topicPoolService.getMentorRegistrations()`, `topicPoolService.confirmRegistration()`, `topicPoolService.rejectRegistration()`, `topicPoolService.updateTopic()`, `topicPoolService.resubmitTopic()`

### `LecturerGroupsPage` — `/lecturer/groups`
List of student groups assigned to this mentor (groups whose project lists this mentor).

API: `studentGroupService.getMentorGroups()`

### `LecturerGroupDetailPage` — `/lecturer/groups/:id`
Group detail with topic info. **Currently renders hardcoded sample data** — not wired to any service yet.

### `TopicCreatePage` — `/lecturer/create`
Multi-step wizard modal for proposing a topic to the pool:
1. **Step 1:** Basic info (title, description as separate field, technology stack)
2. **Step 2:** Requirements (major, max students, deadline)
3. **Step 3:** Review + submit

API: `topicPoolService.proposeTopic()`

### `SupervisedProjectsPage` — `/lecturer/supervised-projects`
List of all projects the mentor has supervised (past and present), with search and pagination. Clicking a project opens a detail modal (`SupervisedProjectModal`).

API: `projectService.getSupervisedProjects()`

---

## Pages (Evaluator-focused)

### `LecturerModerationPage` — `/lecturer/moderate`
Evaluation queue — projects assigned to this evaluator that are pending review.
- Filter by semester, status
- Navigate to review page

API: `evaluatorService.getProjects()`, `evaluatorService.getFilterOptions()`

### `LecturerReviewPage` — `/lecturer/moderate/:id`
Full project review page:
- Project details, documents
- Similarity check tab (currently mock data)
- Submit evaluation result: **Approve** / **Needs Modification** (with feedback) / **Reject**

API: `evaluatorService.getProjectForReview(id)`, `evaluatorService.evaluate(id)`

### `LecturerHistoryPage` — `/lecturer/history`
Past evaluations submitted by this evaluator.

API: `evaluatorService.getHistory()`

---

## Key Concepts

### Topic propose — multi-step wizard
The old single-form `RegisterTopicModal` was rebuilt as a multi-step wizard in `TopicCreatePage`. The description field is now separate from the title. File/attachment upload consolidated into one attachment area with image thumbnails.

### Pool-topic registration flow (mentor side)
```
Student registers (note + attachment) → mentor sees request in Registrations tab
→ Mentor confirms  → project created, student SPA updates in real-time (SignalR)
→ Mentor rejects   → student sees rejection reason
```

### NeedsModification resubmit flow
```
Evaluator requests modification → topic pool topic moves to NeedsModification state
→ Mentor edits topic (LecturerRepositoryPage → edit modal)
→ Mentor resubmits (topicPoolService.resubmitTopic())
→ submissionNumber++ on backend, evaluator results reset
```

### Direct-registration review (mentor side)
Student submits a direct topic → mentor sees it via `proposedTopicService` / backend endpoint `POST /api/direct-topics/{projectId}/review` → mentor approves or requests modification.

API route: `mentor.directRegistrationReview(projectId)` in `routes.ts`

### Supervision history
`SupervisedProjectsPage` uses `GET /api/projects/supervised` — returns all projects (with pagination/search) where this mentor is listed as a supervisor, including completed semesters.

---

## Services Used

| Service | Import | Roles |
|---|---|---|
| `dashboardService` | `@/lib` | mentor dashboard |
| `topicService` | `@/lib` | mentor topics list |
| `topicPoolService` | `@/lib` | pool CRUD, registrations, update/resubmit |
| `studentGroupService` | `@/lib` | mentor's groups |
| `proposedTopicService` | `@/lib` | direct-registration review |
| `evaluatorService` | `@/lib` | evaluation queue, review, history |
| `projectService` | `@/lib` | supervised projects |
| `supportService` | `@/lib` | support tickets |
| `userService` | `@/lib` | profile GET/PUT |

---

## Components (lecturer-specific, `src/components/lecturer/`)

- `RegisterTopicModal` — exported from `@/components/lecturer`; now wraps the multi-step wizard
- `MentorTopicsPanel` — shared panel used in both `LecturerRepositoryPage` and `DepartmentHeadDashboard` for "My Topics" tab
- `SupervisedProjectModal` — detail modal used in `SupervisedProjectsPage`
