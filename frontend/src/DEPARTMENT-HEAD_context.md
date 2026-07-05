# Department Head Context — TEDF Frontend

Covers everything specific to the `departmenthead` role. Read this before touching `src/pages/department-head/` or the dept-head-only routes inside the Lecturer layout.

---

## Role & Layout

- **Role string:** `"departmenthead"`
- **Layout:** `LecturerLayout` (shared with mentor and evaluator)
- **Sidebar:** `LecturerSidebar` — renders dept-head nav items (Dashboard, Assign Evaluators, My Topics, Supervised Projects, Support, Profile)
- **Home route:** `/lecturer/dashboard`
- **Protected by:** `ProtectedRoute` with `allowedRoles={["mentor", "evaluator", "departmenthead"]}` on the Lecturer layout, plus inner guards for dept-head-only pages

Department Head is a **role granted on top of** the Mentor account — a user with `departmenthead` role typically also has `mentor` capabilities. The Lecturer layout checks `activeRole` to show/hide dept-head-only sidebar items.

---

## Route Map

| Route | Page | Status | Notes |
|---|---|---|---|
| `/lecturer/dashboard` | `DepartmentHeadDashboardPage` | ✅ | Dept-head only; shown via `activeRole === "departmenthead"` check |
| `/lecturer/assign` | `AssignEvaluatorsPage` | ✅ | Evaluator assignment + final decision |
| `/lecturer/assign/:tab` | `AssignEvaluatorsPage` | ✅ | Tab parameter: `pending` / `assign` / `final-decision` |
| `/lecturer` | `LecturerRepositoryPage` | ✅ | "My Topics" tab shared via `MentorTopicsPanel` |
| `/lecturer/supervised-projects` | `SupervisedProjectsPage` | ✅ | Dept-head's own supervised project history |
| `/lecturer/support` | `LecturerSupportPage` | ✅ | Support tickets |
| `/lecturer/profile` | `ProfilePage` (shared) | ✅ | Edit profile, Division field |

> All other Lecturer routes (`/lecturer/groups`, `/lecturer/create`, `/lecturer/moderate`, etc.) are still accessible if the dept-head user also holds the mentor or evaluator role.

---

## Pages

### `DepartmentHeadDashboardPage` — `/lecturer/dashboard`
Overview of the department's thesis lifecycle for the active semester.
- Projects pending evaluator assignment
- Projects in evaluation
- Conflict alerts (two evaluators disagree — needs final decision)
- Department evaluator list snapshot

API: `dashboardService.getDepartmentHeadDashboard()`

### `AssignEvaluatorsPage` — `/lecturer/assign` `/lecturer/assign/:tab`
Three-tab page managing evaluations within the department:

**Tab 1 — Pending Projects (`/lecturer/assign` or `?tab=pending`)**
- Projects in `PendingEvaluation` state waiting for evaluator assignment
- For each project: assign two evaluators from the dept's evaluator list

**Tab 2 — In-Progress (`?tab=assign`)**
- Projects already assigned; re-assign or change evaluators if needed

**Tab 3 — Final Decision (`?tab=final-decision`)**
- Projects where both evaluators have submitted conflicting results
- Dept-head submits the final decision (Approve / Reject)

API:
- `evaluatorService.getDepartmentEvaluators()` — list of evaluators in the dept
- `evaluatorService.getDepartmentHeadProjects()` — dept project list
- `evaluatorService.assignEvaluator()` — POST `/api/evaluations/assign-evaluator`
- `evaluatorService.submitFinalDecision(projectId)` — POST `/api/evaluations/projects/{id}/final-decision`

---

## "My Topics" Tab — Shared via `MentorTopicsPanel`

`LecturerRepositoryPage` renders `MentorTopicsPanel` for both mentor and dept-head. For a dept-head who is also a mentor, this tab shows their own proposed pool topics and any registration requests. The component is parameterized by role to show/hide certain actions.

---

## Key Concepts

### Role check pattern
The Lecturer layout and some pages read `useAuth().activeRole` to branch between mentor, evaluator, and dept-head behavior. Example:

```tsx
const { activeRole } = useAuth();
if (activeRole === "departmenthead") {
  // show dept-head sidebar items
}
```

### Evaluator assignment rules (enforced by backend)
- Each project needs exactly **2 evaluators**
- Evaluators must be from the **same department** as the project
- One evaluator cannot be assigned to the same project twice
- The dept-head themselves cannot evaluate a project they supervise

If both evaluators submit the same result → project moves to final state automatically (no dept-head action needed). If they conflict → dept-head sees the project in the "Final Decision" tab.

### Role switching for multi-role users
A user may hold `mentor + departmenthead`. `useAuth().switchRole(role)` changes `activeRole`, which re-renders the sidebar and enables/disables routes. The role switch does not require a new login.

---

## Services Used

| Service | Import | Notes |
|---|---|---|
| `dashboardService` | `@/lib` | `getDepartmentHeadDashboard()` |
| `evaluatorService` | `@/lib` | dept evaluators, assign, final-decision |
| `projectService` | `@/lib` | dept project list |
| `topicService` | `@/lib` | mentor topics (via `MentorTopicsPanel`) |
| `topicPoolService` | `@/lib` | pool registrations (via `MentorTopicsPanel`) |
| `supportService` | `@/lib` | support tickets |
| `userService` | `@/lib` | profile GET/PUT |
