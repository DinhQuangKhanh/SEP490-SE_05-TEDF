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
| `Authenticated`   | any logged-in user (`RequireAuthorization()`)                  |
| `Admin`           | `RequireAdmin` policy (Admin role)                             |
| `Mentor`          | `RequireMentor` policy (Mentor role)                           |
| `Evaluator`       | `RequireEvaluator` policy (Evaluator role)                     |
| `MentorOfProject` | resource policy — caller mentors the target project            |
| `GroupLeader`     | resource policy — caller is the leader of the target group     |
| `DeptHeadOfDept`  | resource policy — caller heads the target project's department |

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

### Authentication

Login is handled **client-side via Firebase Auth**; the SPA sends the resulting Firebase ID token to the API as a Bearer token. There is no `/api/auth/login` endpoint — the API only validates tokens. (The `Endpoints/Authentications` group is reserved/empty.)

### Admin · `/api/admin`

| Method | Path                                         | Auth          | Description                     |
| ------ | -------------------------------------------- | ------------- | ------------------------------- |
| GET    | `/api/admin/dashboard`                       | Admin         | System overview & statistics    |
| GET    | `/api/admin/users`                           | Admin         | List users                      |
| PUT    | `/api/admin/users/{userId}/lock`             | Admin         | Lock a user account             |
| PUT    | `/api/admin/users/{userId}/unlock`           | Admin         | Unlock a user account           |
| GET    | `/api/admin/projects`                        | Admin         | List/oversee all projects       |
| POST   | `/api/admin/departments/{departmentId}/head` | Admin         | Assign a department head        |
| GET    | `/api/admin/activity-logs`                   | Authenticated | Activity log feed               |
| GET    | `/api/admin/activity-logs/grouped`           | Authenticated | Activity logs grouped by action |
| GET    | `/api/admin/activity-logs/errors`            | Authenticated | Error occurrences for an action |
| GET    | `/api/admin/activity-logs/severity-summary`  | Authenticated | Counts by severity              |
| GET    | `/api/admin/error-logs/{id}`                 | Authenticated | Full error-log detail           |

### Semesters · `/api/semesters`, `/api/admin/semesters`

| Method | Path                                                 | Auth          | Description              |
| ------ | ---------------------------------------------------- | ------------- | ------------------------ |
| GET    | `/api/semesters`                                     | Authenticated | List semesters (general) |
| GET    | `/api/admin/semesters`                               | Admin         | List semesters (admin)   |
| GET    | `/api/admin/semesters/active`                        | Admin         | Active semester          |
| GET    | `/api/admin/semesters/{id}`                          | Admin         | Semester by id           |
| POST   | `/api/admin/semesters`                               | Admin         | Create semester          |
| PUT    | `/api/admin/semesters/{id}`                          | Admin         | Update semester          |
| DELETE | `/api/admin/semesters/{id}`                          | Admin         | Delete semester          |
| POST   | `/api/admin/semesters/{id}/eligible-students/import` | Admin         | Import eligible students |

### Departments / Majors

| Method | Path          | Auth          | Description |
| ------ | ------------- | ------------- | ----------- |
| GET    | `/api/majors` | Authenticated | List majors |

> Department CRUD is administered via the Admin group (e.g. assign head, above).

### Department Head · `/api/department-head`

| Method | Path                                                       | Auth           | Description                                      |
| ------ | ---------------------------------------------------------- | -------------- | ------------------------------------------------ |
| GET    | `/api/department-head/dashboard`                           | Authenticated  | Department overview                              |
| GET    | `/api/department-head/evaluators`                          | DeptHeadOfDept | Evaluators in the department                     |
| GET    | `/api/department-head/projects`                            | DeptHeadOfDept | Department projects                              |
| POST   | `/api/department-head/assign-evaluator`                    | DeptHeadOfDept | Assign an evaluator to a project                 |
| POST   | `/api/department-head/projects/{projectId}/final-decision` | DeptHeadOfDept | Submit final decision on a conflicted evaluation |

### Topic Pools · `/api/topic-pools`

| Method | Path                                                | Auth          | Description                                                              |
| ------ | --------------------------------------------------- | ------------- | ------------------------------------------------------------------------ |
| GET    | `/api/topic-pools`                                  | Authenticated | Browse topic pools                                                       |
| GET    | `/api/topic-pools/by-department`                    | Authenticated | Pools grouped by department                                              |
| GET    | `/api/topic-pools/{id}`                             | Authenticated | Pool detail                                                              |
| GET    | `/api/topic-pools/{id}/statistics`                  | Authenticated | Pool statistics                                                          |
| POST   | `/api/topic-pools/{poolId}/propose`                 | Authenticated | Mentor proposes a topic (file upload; rate-limited, 60s timeout, ≤25 MB) |
| POST   | `/api/student-groups/{groupId}/topic-registrations` | GroupLeader   | Group registers for a pool topic                                         |
| PUT    | `/api/topic-pools/registrations/{id}/confirm`       | Authenticated | Confirm a topic registration                                             |
| PUT    | `/api/topic-pools/registrations/{id}/reject`        | Authenticated | Reject a topic registration                                              |

### Topics · `/api/topics`

| Method | Path                              | Auth          | Description             |
| ------ | --------------------------------- | ------------- | ----------------------- |
| GET    | `/api/topics`                     | Authenticated | List topics             |
| GET    | `/api/topics/{topicId}`           | Authenticated | Topic detail            |
| GET    | `/api/topics/{topicId}/documents` | Authenticated | Topic documents         |
| POST   | `/api/topics/{topicId}/documents` | Authenticated | Upload a topic document |

### Direct Registration · `/api/student`

Student-initiated topic flow.

| Method | Path                                                               | Auth          | Description                                        |
| ------ | ------------------------------------------------------------------ | ------------- | -------------------------------------------------- |
| GET    | `/api/student/available-mentors`                                   | Authenticated | Mentors available for direct registration          |
| POST   | `/api/student/{groupId}/direct-topic`                              | GroupLeader   | Create a direct topic for the group                |
| PUT    | `/api/student/direct-topic/{groupId}/{projectId}/submit-to-mentor` | GroupLeader   | Submit the topic to the mentor                     |
| PUT    | `/api/student/direct-topic/{projectId}/update`                     | GroupLeader   | Edit a direct topic (e.g. after NeedsModification) |

### Student Groups · `/api/student-groups`

Base policy: `Authenticated` (overrides noted).

| Method | Path                                                              | Auth          | Description                            |
| ------ | ----------------------------------------------------------------- | ------------- | -------------------------------------- |
| POST   | `/api/student-groups`                                             | Authenticated | Create a group (caller becomes leader) |
| GET    | `/api/student-groups/my-group`                                    | Authenticated | Caller's current group                 |
| GET    | `/api/student-groups/open`                                        | Authenticated | Open groups to join                    |
| GET    | `/api/student-groups/my-invitations`                              | Authenticated | Caller's invitations                   |
| GET    | `/api/student-groups/my-pending-join-request`                     | Authenticated | Caller's pending join request          |
| GET    | `/api/student-groups/{groupId}/join-requests`                     | GroupLeader   | Join requests for the group            |
| POST   | `/api/student-groups/{groupId}/invitations`                       | GroupLeader   | Invite a member                        |
| PUT    | `/api/student-groups/{groupId}/invitations/{invitationId}/accept` | Authenticated | Accept an invitation                   |
| PUT    | `/api/student-groups/{groupId}/invitations/{invitationId}/reject` | Authenticated | Reject an invitation                   |
| POST   | `/api/student-groups/{groupId}/join-requests`                     | Authenticated | Request to join a group                |
| PUT    | `/api/student-groups/{groupId}/join-requests/{requestId}/approve` | GroupLeader   | Approve a join request                 |
| PUT    | `/api/student-groups/{groupId}/join-requests/{requestId}/reject`  | GroupLeader   | Reject a join request                  |
| GET    | `/api/student-groups/mentor`                                      | Mentor        | Groups assigned to the mentor          |

### Mentor · `/api/mentor`

| Method | Path                                      | Auth            | Description                                          |
| ------ | ----------------------------------------- | --------------- | ---------------------------------------------------- |
| GET    | `/api/mentor/dashboard`                   | Mentor          | Mentor overview                                      |
| GET    | `/api/mentor/topics`                      | Mentor          | Topics/projects the mentor owns                      |
| PUT    | `/api/mentor/topics/{projectId}/review`   | MentorOfProject | Review a student-submitted topic                     |
| PUT    | `/api/mentor/topics/{projectId}/update`   | MentorOfProject | Edit a pool topic                                    |
| PUT    | `/api/mentor/topics/{projectId}/resubmit` | MentorOfProject | Resubmit after NeedsModification (resets evaluators) |

### Evaluator · `/api/evaluator`

All require the `Evaluator` policy.

| Method | Path                                             | Auth      | Description                    |
| ------ | ------------------------------------------------ | --------- | ------------------------------ |
| GET    | `/api/evaluator/dashboard`                       | Evaluator | Evaluator overview             |
| GET    | `/api/evaluator/projects`                        | Evaluator | Assigned projects              |
| GET    | `/api/evaluator/filter-options`                  | Evaluator | Filter option metadata         |
| GET    | `/api/evaluator/history`                         | Evaluator | Evaluation history             |
| GET    | `/api/evaluator/projects/{projectId}/review`     | Evaluator | Project review detail          |
| GET    | `/api/evaluator/projects/{projectId}/similarity` | Evaluator | Title/content similarity check |
| POST   | `/api/evaluator/projects/{projectId}/evaluate`   | Evaluator | Submit an evaluation result    |

### Notifications · `/api/notifications`

| Method | Path                              | Auth          | Description        |
| ------ | --------------------------------- | ------------- | ------------------ |
| GET    | `/api/notifications`              | Authenticated | List notifications |
| GET    | `/api/notifications/unread-count` | Authenticated | Unread count       |
| PUT    | `/api/notifications/{id}/read`    | Authenticated | Mark one as read   |
| PUT    | `/api/notifications/read-all`     | Authenticated | Mark all as read   |

Real-time delivery: the `NotificationHub` (`/hubs/notifications`) pushes a `ReceiveNotification` event to connected clients.

### Supports · `/api/supports`

Base policy: `Authenticated`.

| Method | Path                        | Auth          | Description             |
| ------ | --------------------------- | ------------- | ----------------------- |
| GET    | `/api/supports`             | Authenticated | List support tickets    |
| POST   | `/api/supports`             | Authenticated | Create a support ticket |
| GET    | `/api/supports/stats`       | Authenticated | Ticket statistics       |
| GET    | `/api/supports/{id}`        | Authenticated | Ticket detail           |
| POST   | `/api/supports/{id}/reply`  | Authenticated | Reply to a ticket       |
| PATCH  | `/api/supports/{id}/status` | Authenticated | Change ticket status    |

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

> **Maintenance note:** endpoints are defined as `IEndpoint` classes under `TEDF.API/Endpoints/<Group>/` and auto-registered by reflection. When adding or changing an endpoint, update this file and rely on `/swagger` for exact request/response schemas.
