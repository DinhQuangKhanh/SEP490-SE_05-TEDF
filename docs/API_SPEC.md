# TEDF — API Specification

REST API reference for the TEDF Thesis Management System backend (`TEDF.API`, ASP.NET Core Minimal API). This document is the human-readable index of endpoints; the **authoritative, always-current schema is Swagger** at `/swagger` (dev) — request/response bodies are generated there from the actual types.

- **Base URL (dev):** `http://localhost:5141` / `https://localhost:7176`
- **Swagger UI:** `/swagger` · **Health:** `/health` · **Hangfire:** `/hangfire` (Admin only)
- **Realtime hubs:** `/hubs/notifications`, `/hubs/chat`

---

## Conventions

### Response envelope

All endpoints return the `ApiResponse` / `ApiResponse<T>` envelope:

```jsonc
{
  "success": true,
  "message": "OK",
  "data": {
    /* payload, when success */
  },
  "errors": { "field": ["error message"] }, // when validation fails
}
```

Clients unwrap `data` on success and treat `success: false` (or a non-2xx status) as an error using `message`.

### Authentication

- **Bearer JWT** (Firebase ID token): `Authorization: Bearer <token>`.
- The API validates the Firebase token, resolves it to the database user, and injects `DbUserId` + role claims.
- **SignalR** hubs read the token from the query string: `/hubs/notifications?access_token=<token>`.
- Most endpoints require authentication; see the **Auth** column per endpoint.

### Authorization policies

| Auth column value | Meaning                                                        |
| ----------------- | -------------------------------------------------------------- |
| `Anonymous`       | no authentication required                                     |
| `Authenticated`   | any logged-in user (`RequireAuthorization()`)                  |
| `Admin`           | `RequireAdmin` policy (Admin role)                             |
| `Evaluator`       | `RequireEvaluator` policy (Evaluator role)                     |
| `MentorOfProject` | resource policy — caller mentors the target project            |
| `GroupLeader`     | resource policy — caller is the leader of the target group     |
| `DeptHeadOfDept`  | resource policy — caller heads the target project's department |

> The `Mentor` (`RequireMentor`) role policy still exists but is currently **unused** — the mentor-specific endpoints that consumed it have not yet been re-migrated (see the reorganization note below).

### Errors

`ExceptionHandlingMiddleware` maps exceptions to status codes, all wrapped in the envelope:

| Status | Cause                                                                   |
| ------ | ----------------------------------------------------------------------- |
| 400    | Validation error (with `errors`), business-rule violation, domain error |
| 401    | Missing/invalid token                                                   |
| 403    | Authenticated but not authorized (policy/`UnauthorizedAccessException`) |
| 404    | Entity not found                                                        |
|        | ArgumentException (e.g. End Date before Start Date)                     |
| 409    | Concurrency conflict (e.g. unique-code collision)                       |
| 429    | Rate limit exceeded (upload endpoints)                                  |
| 500    | Unhandled error (logged to Mongo `error_logs`)                          |

### Pagination & filtering

List endpoints accept query params such as `search`, `page` (default 1), `pageSize` (default 20, max 100), plus context filters (`semesterId`, `status`, `majorId`, …). Paged responses include `{ items, totalCount, page, pageSize, totalPages }`. Free-text `search` bypasses server caching.

---

## Endpoints

> **Feature-based layout.** `TEDF.API/Endpoints/` is organised by **feature**, not by role — there is no longer a `Mentor`, `DepartmentHead`, `Admin`, or `Departments` folder. Each folder holds a `sealed class <Feature>Endpoints : IEndpoint` (the `Topics` folder holds two: `TopicCatalogEndpoints` + `TopicPoolsEndpoints`), auto-registered by reflection. Folders: `ActivityLogs`, `Archives`, `Dashboard`, `DirectTopics`, `Evaluations`, `Groups`, `Majors`, `Notifications`, `Projects`, `Semesters`, `Settings`, `SupportTickets`, `Topics`, `Users`. The frontend mirrors this exactly under `frontend/src/lib/<feature>/` and `frontend/src/types/<feature>/` (+ `lib/common` for shared infra).
>
> **Where role endpoints went.** Every role action now lives in its feature folder: **all per-role dashboards** are unified under `/api/dashboard/{admin,mentor,department-head,evaluator}` (`Dashboard`); the admin project list **and** the department-head project list live under `/api/projects` (`Projects`); department-head evaluator management (`evaluators`, `assign-evaluator`, final-decision) moved into `/api/evaluations` (`Evaluations`); assign-department-head moved into `/api/users` (`Users`); the mentor topic list moved to `/api/topics/mentor` (`Topics`) and mentor topic update/resubmit to `/api/topic-pools/topics/{projectId}/…` (`Topics`/`TopicPoolsEndpoints`); the mentor's groups list is `/api/groups/mentor` (`Groups`). Prefix renames from the earlier pass also apply: `/api/student-groups`→`/api/groups`, `/api/supports`→`/api/support-tickets`, `/api/evaluator`→`/api/evaluations`, and the admin-only groups dropped `/api/admin/` (`users`, `semesters`, `settings`, `archives`, `activity-logs`). Trust `/swagger` for the live request/response schemas.

### Authentication

Login is handled **client-side via Firebase Auth**; the SPA sends the resulting Firebase ID token to the API as a Bearer token. There is no `/api/auth/login` endpoint — the API only validates tokens.

### Dashboard · `/api/dashboard`

Per-role dashboards, unified under one feature group (policy applied per route).

| Method | Path                             | Auth      | Description                  |
| ------ | -------------------------------- | --------- | ---------------------------- |
| GET    | `/api/dashboard/admin`           | Admin     | System overview & statistics |
| GET    | `/api/dashboard/mentor`          | Mentor    | Mentor overview              |
| GET    | `/api/dashboard/department-head` | Authenticated | Department overview      |
| GET    | `/api/dashboard/evaluator`       | Evaluator | Evaluator overview           |

### Users · `/api/users`

Base policy: `Admin`.

| Method | Path                                          | Auth  | Description                          |
| ------ | --------------------------------------------- | ----- | ------------------------------------ |
| GET    | `/api/users`                                  | Admin | List users                          |
| PUT    | `/api/users/{userId}/lock`                    | Admin | Lock a user account                 |
| PUT    | `/api/users/{userId}/unlock`                  | Admin | Unlock a user account               |
| POST   | `/api/users/departments/{departmentId}/head`  | Admin | Assign a user as head of a department |

### Activity Logs · `/api/activity-logs`

| Method | Path                                  | Auth  | Description                     |
| ------ | ------------------------------------- | ----- | ------------------------------- |
| GET    | `/api/activity-logs`                  | Admin | Activity log feed               |
| GET    | `/api/activity-logs/grouped`          | Admin | Activity logs grouped by action |
| GET    | `/api/activity-logs/severity-summary` | Admin | Counts by severity              |
| GET    | `/api/activity-logs/errors`           | Admin | Error occurrences for an action |
| GET    | `/api/activity-logs/errors/{id}`      | Admin | Full error-log detail           |

### Semesters · `/api/semesters`

| Method | Path                                           | Auth          | Description                          |
| ------ | ---------------------------------------------- | ------------- | ------------------------------------ |
| GET    | `/api/semesters`                               | Admin         | List semesters (admin)               |
| GET    | `/api/semesters`                               | Authenticated | List semesters (public, lightweight) |
| GET    | `/api/semesters/active`                        | Admin         | Active semester                      |
| GET    | `/api/semesters/{id}`                          | Admin         | Semester by id                       |
| POST   | `/api/semesters`                               | Admin         | Create semester                      |
| PUT    | `/api/semesters/{id}`                          | Admin         | Update semester                      |
| DELETE | `/api/semesters/{id}`                          | Admin         | Delete semester                      |
| POST   | `/api/semesters/{id}/eligible-students/import` | Admin         | Import eligible students             |

### Settings · `/api/settings`

| Method | Path                       | Auth      | Description                          |
| ------ | -------------------------- | --------- | ------------------------------------ |
| GET    | `/api/settings/public`     | Anonymous | Public branding + maintenance subset |
| GET    | `/api/settings`            | Admin     | Full system settings list            |
| PUT    | `/api/settings`            | Admin     | Upsert system settings               |
| POST   | `/api/settings/test-email` | Admin     | Send a test email                    |
| POST   | `/api/settings/logo`       | Admin     | Upload a new system logo             |

### Archives · `/api/archives`

| Method | Path                          | Auth  | Description                        |
| ------ | ----------------------------- | ----- | --------------------------------- |
| GET    | `/api/archives`               | Admin | Archived projects by academic year |
| GET    | `/api/archives/{id}/download` | Admin | Download an archived project      |

### Majors · `/api/majors`

| Method | Path          | Auth          | Description |
| ------ | ------------- | ------------- | ----------- |
| GET    | `/api/majors` | Authenticated | List majors |

### Projects · `/api/projects`

Base policy: `Authenticated` (per-route policy noted). *(Department-head dashboard, evaluator management, and the mentor area no longer have their own route folders — see the layout note above.)*

| Method | Path                       | Auth           | Description                            |
| ------ | -------------------------- | -------------- | -------------------------------------- |
| GET    | `/api/projects`            | Admin          | List all projects (admin oversight)    |
| GET    | `/api/projects/department` | DeptHeadOfDept | Projects within the caller's department |

### Groups · `/api/groups`

Base policy: `Authenticated` (overrides noted). *(Renamed from `/api/student-groups`.)*

| Method | Path                                                      | Auth          | Description                            |
| ------ | --------------------------------------------------------- | ------------- | -------------------------------------- |
| POST   | `/api/groups`                                             | Authenticated | Create a group (caller becomes leader) |
| GET    | `/api/groups/mentor`                                      | Mentor        | Groups supervised by the mentor        |
| GET    | `/api/groups/my-group`                                    | Authenticated | Caller's current group                 |
| GET    | `/api/groups/open`                                        | Authenticated | Open groups to join                    |
| GET    | `/api/groups/my-invitations`                              | Authenticated | Caller's invitations                   |
| GET    | `/api/groups/my-pending-join-request`                     | Authenticated | Caller's pending join request          |
| GET    | `/api/groups/{groupId}/join-requests`                     | Authenticated | Join requests for the group            |
| GET    | `/api/groups/{groupId}/invitable-students`                | Authenticated | Students not yet in a group            |
| POST   | `/api/groups/{groupId}/invitations`                       | GroupLeader   | Invite a member                        |
| POST   | `/api/groups/{groupId}/join-requests`                     | Authenticated | Request to join a group                |
| PUT    | `/api/groups/{groupId}/invitations/{invitationId}/accept` | Authenticated | Accept an invitation                   |
| PUT    | `/api/groups/{groupId}/invitations/{invitationId}/reject` | Authenticated | Reject an invitation                   |
| PUT    | `/api/groups/{groupId}/join-requests/{requestId}/approve` | GroupLeader   | Approve a join request                 |
| PUT    | `/api/groups/{groupId}/join-requests/{requestId}/reject`  | GroupLeader   | Reject a join request                  |

### Direct Topics · `/api/direct-topics`

Student-initiated topic flow + mentor review. *(Renamed from `/api/student/...`.)*

| Method | Path                                                        | Auth            | Description                                   |
| ------ | ---------------------------------------------------------- | --------------- | --------------------------------------------- |
| GET    | `/api/direct-topics/available-mentors`                     | Authenticated   | Mentors available for direct registration     |
| POST   | `/api/direct-topics/{groupId}`                             | GroupLeader     | Create a direct topic for the group           |
| PUT    | `/api/direct-topics/{projectId}`                           | GroupLeader     | Edit a direct topic (after NeedsModification)  |
| POST   | `/api/direct-topics/{projectId}/submit-to-mentor/{groupId}`| GroupLeader     | Submit the topic to the mentor                |
| PUT    | `/api/direct-topics/{projectId}/review`                    | MentorOfProject | Mentor approves / requests modification       |

### Topics · `/api/topics`

| Method | Path                              | Auth          | Description         |
| ------ | --------------------------------- | ------------- | ------------------- |
| GET    | `/api/topics`                     | Authenticated | List topics in pool             |
| GET    | `/api/topics/mentor`              | Mentor        | Topics owned by the current mentor |
| GET    | `/api/topics/{topicId}`           | Authenticated | Topic detail                    |
| GET    | `/api/topics/{topicId}/documents` | Authenticated | Topic documents                 |

### Topic Pools · `/api/topic-pools`

| Method | Path                                             | Auth          | Description                                                              |
| ------ | ------------------------------------------------ | ------------- | ------------------------------------------------------------------------ |
| GET    | `/api/topic-pools`                               | Authenticated | Browse topic pools                                                       |
| GET    | `/api/topic-pools/by-department`                 | Authenticated | Pools grouped by department                                              |
| GET    | `/api/topic-pools/{id}`                          | Authenticated | Pool detail                                                              |
| GET    | `/api/topic-pools/{id}/statistics`               | Authenticated | Pool statistics                                                          |
| POST   | `/api/topic-pools/{poolId}/propose`              | Authenticated | Mentor proposes a topic (file upload; rate-limited, 60s timeout, ≤25 MB) |
| POST   | `/api/topic-pools/{groupId}/topic-registrations` | GroupLeader   | Group registers for a pool topic                                        |
| PUT    | `/api/topic-pools/registrations/{id}/confirm`    | Authenticated   | Confirm a topic registration                                          |
| PUT    | `/api/topic-pools/registrations/{id}/reject`     | Authenticated   | Reject a topic registration                                           |
| PUT    | `/api/topic-pools/topics/{projectId}/update`     | MentorOfProject | Mentor edits a pool topic (after NeedsModification)                   |
| PUT    | `/api/topic-pools/topics/{projectId}/resubmit`   | MentorOfProject | Mentor resubmits a pool topic for evaluation                          |

### Evaluations · `/api/evaluations`

Mixed audience: **evaluator self-service** + **department-head evaluation management** (the dept-head routes moved here from the old `DepartmentHead` folder). Policy is applied per route, not on the group. The evaluator dashboard moved to `/api/dashboard/evaluator`. *(Renamed from `/api/evaluator`.)*

| Method | Path                                               | Auth           | Description                                      |
| ------ | -------------------------------------------------- | -------------- | ------------------------------------------------ |
| GET    | `/api/evaluations/filter-options`                  | Evaluator      | Filter option metadata                           |
| GET    | `/api/evaluations/history`                         | Evaluator      | Evaluation history                               |
| GET    | `/api/evaluations/projects`                        | Evaluator      | Assigned projects                                |
| GET    | `/api/evaluations/projects/{projectId}/review`     | Evaluator      | Project review detail                            |
| GET    | `/api/evaluations/projects/{projectId}/similarity` | Evaluator      | Title/content similarity check                   |
| POST   | `/api/evaluations/projects/{projectId}/evaluate`   | Evaluator      | Submit an evaluation result                      |
| GET    | `/api/evaluations/evaluators`                      | DeptHeadOfDept | Evaluators in the caller's department            |
| POST   | `/api/evaluations/assign-evaluator`                | DeptHeadOfDept | Assign an evaluator to a project                 |
| POST   | `/api/evaluations/projects/{projectId}/final-decision` | DeptHeadOfDept | Submit final decision on a conflicted evaluation |

### Notifications · `/api/notifications`

| Method | Path                              | Auth          | Description        |
| ------ | --------------------------------- | ------------- | ------------------ |
| GET    | `/api/notifications`              | Authenticated | List notifications |
| GET    | `/api/notifications/unread-count` | Authenticated | Unread count       |
| PUT    | `/api/notifications/{id}/read`    | Authenticated | Mark one as read   |
| PUT    | `/api/notifications/read-all`     | Authenticated | Mark all as read   |

Real-time delivery: the `NotificationHub` (`/hubs/notifications`) pushes a `ReceiveNotification` event to connected clients.

### Support Tickets · `/api/support-tickets`

Base policy: `Authenticated`. *(Renamed from `/api/supports`.)*

| Method | Path                               | Auth          | Description             |
| ------ | ---------------------------------- | ------------- | ----------------------- |
| GET    | `/api/support-tickets`             | Authenticated | List support tickets    |
| GET    | `/api/support-tickets/stats`       | Authenticated | Ticket statistics       |
| GET    | `/api/support-tickets/{id}`        | Authenticated | Ticket detail           |
| POST   | `/api/support-tickets`             | Authenticated | Create a support ticket |
| POST   | `/api/support-tickets/{id}/reply`  | Authenticated | Reply to a ticket       |
| PATCH  | `/api/support-tickets/{id}/status` | Authenticated | Change ticket status    |

---

## Realtime (SignalR)

| Hub           | Path                  | Auth                  | Notes                                                      |
| ------------- | --------------------- | --------------------- | ---------------------------------------------------------- |
| Notifications | `/hubs/notifications` | `?access_token=<jwt>` | Server → client `ReceiveNotification`                      |
| Chat          | `/hubs/chat`          | `?access_token=<jwt>` | Hub + storage exist; chat feature not yet exposed via REST |

## Operational endpoints

| Path        | Description                         |
| ----------- | ----------------------------------- |
| `/health`   | Health check — SQL Server + MongoDB |
| `/swagger`  | OpenAPI UI (Development only)       |
| `/hangfire` | Hangfire dashboard (Admin only)     |

---

> **Maintenance note:** endpoints are defined as `sealed class <Domain>Endpoints : IEndpoint` under `TEDF.API/Endpoints/<Domain>/` (one class per route group; a folder may hold more than one when it spans groups, e.g. `Topics/TopicCatalogEndpoints` + `Topics/TopicPoolsEndpoints`) and auto-registered by reflection. When adding or changing an endpoint, update this file and rely on `/swagger` for exact request/response schemas.
