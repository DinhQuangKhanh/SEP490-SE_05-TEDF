# TEDF Backend — Project Status

Snapshot of what is implemented in the TEDF API (`backend/`), by bounded context and by cross-cutting concern. Status is inferred from the source — whether an Application feature has handlers, whether endpoints are mapped, and whether jobs/handlers contain real logic vs. `TODO` stubs.

**Last updated:** 2026-06-01

> Living document. Update a row when a feature gains endpoints, a job is implemented, or a stub is filled in.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done — Application handlers + mapped endpoints, functional |
| 🚧 | Partial — built at one layer but not fully wired, or a stub with real intent |
| 📋 | Scaffolded — folder exists but empty / no implementation |
| ❌ | Not started |

## Summary

- **Core lifecycle is live:** topic pools, direct registration, evaluations, student groups, semesters, supports, notifications, dashboards, and admin/department-head/mentor management are implemented end to end (Application + endpoints).
- **Auth, caching (L1/L2), domain events, SignalR, and most Hangfire scheduling** are wired.
- **Not built / in progress:** Reports (PDF/Excel), real-time Chat feature, and Meetings — their folders are scaffolded but empty. Several reminder jobs and a few TopicPool notification handlers are `TODO` stubs. Redis health check is a stub.

---

## By Bounded Context

Endpoint counts are HTTP endpoints under `TEDF.API/Endpoints/`; "App files" is the rough size of the Application feature (`Features/<Context>`). Some contexts are exposed through the **Admin** endpoint group rather than a same-named folder.

| Context | App files | Endpoints | Status | Notes |
|---------|-----------|-----------|--------|-------|
| TopicPools | 19 | 10 (`TopicPools`) | ✅ | Pool CRUD, registration, approval lifecycle |
| Evaluations | 19 | 7 (`Evaluations`) | ✅ | Assignment, review, results |
| StudentGroups | 34 | 3 (`StudentGroups`) | ✅ | Create/invite/join; large feature, also drives other groups' endpoints |
| Supports | 20 | 2 (`Supports`) | ✅ | Support tickets |
| Semesters | 15 | 3 (`Semesters`) | ✅ | Semester + phase lifecycle |
| DirectRegistration | 14 | 6 (`DirectRegistration`) | ✅ | Student-initiated topic flow |
| Departments | 11 | — | ✅ | Department/Major management (exposed via `Admin`) |
| Dashboard | 9 | — | ✅ | Admin/Mentor/DeptHead dashboards (via role groups) |
| Notifications | 9 | 4 (`Notifications`) | ✅ | Notification management (+ SignalR) |
| Mentor | 7 | 5 (`Mentor`) | ✅ | Pool topic management & review |
| Topics | 7 | 4 (`Topics`) | ✅ | Topic management |
| Users | 7 | — | ✅ | User CRUD (exposed via `Admin`) |
| Projects | 3 | — | ✅ | Project listing via `Admin/GetProjectsEndpoint`; `Endpoints/Projects` folder empty |
| DepartmentHead | — | 7 (`DepartmentHead`) | ✅ | Evaluator assignment, final decisions |
| Admin | — | 12 (`Admin`) | ✅ | Cross-cutting admin (users, projects, activity logs, …) |
| Reports | 0 | 0 (`Reports`) | 📋 | Empty feature + empty endpoint folder; PDF/Excel reporting not built |
| Chats | 0 | 0 (`Chats`) | 🚧 | No feature/endpoints, but `ChatHub` + Conversation/Message repos + Mongo docs exist |
| Meetings | 0 | 0 (`Meetings`) | 📋 | Empty feature + empty endpoint folder |
| Authentications | 0 | 0 (`Authentications`) | ✅ | Intentionally empty — auth is Firebase + JWT middleware, no app-layer feature |

---

## Cross-Cutting Infrastructure

| Concern | Status | Notes |
|---------|--------|-------|
| Firebase auth + JWT bearer | ✅ | Token validation, Firebase UID → DB user claim resolution, role claims |
| Authorization handlers | ✅ | Permission, ProjectOwner, GroupMember, GroupLeader, MentorOfProject, DepartmentHeadOfDepartment |
| CQRS pipeline (MediatR) | ✅ | Logging → Caching → CacheInvalidation → Validation behaviors |
| Hybrid caching (L1 + L2) | ✅ | Memory + Redis with cross-instance L1 invalidation; memory-only fallback when no Redis |
| Domain events | ✅ | Dispatched after `SaveChangesAsync` via `DomainEventInterceptor` |
| SignalR | ✅ | `NotificationHub` (`/hubs/notifications`), `ChatHub` (`/hubs/chat`) mapped |
| Real-time notifications | ✅ | `RealtimeNotificationService` + MongoDB persistence |
| Real-time chat | 🚧 | Hub + repositories + documents exist; no feature/endpoints to drive it |
| Email (SMTP) | ✅ | `SmtpEmailService` (MailKit) + HTML templates |
| File storage | ✅ | `FirebaseStorageService` |
| Excel export | 🚧 | `ExcelService` registered; no Reports feature/endpoints consuming it yet |
| PDF reports | ❌ | Not implemented |
| Attachment malware scan (ClamAV) | ✅ | Scan workflow + quarantine; degrades gracefully to "unavailable" if ClamAV unreachable |
| Upload hardening | ✅ | 25 MB limit, request timeout, sliding-window rate limiter on propose-topic upload |
| Logging / audit (MongoDB) | ✅ | Request, activity, system audit, error logs |
| Health checks | 🚧 | `sqlserver` + `mongodb` registered; **Redis health check is a `TODO` stub** |

---

## Hangfire Jobs

| Job | Status | Notes |
|-----|--------|-------|
| TopicExpirationJob | ✅ | Auto-closes expired pool topics |
| SemesterPhaseTransitionJob | ✅ | Advances semester phases by date |
| GroupJoinRequestExpirationJob | ✅ | Expires stale join requests |
| QuarantineRetryJob (API layer) | ✅ | Re-scans quarantined attachments every 30 min |
| EvaluationReminderJob | 🚧 | Registered & scheduled, body is a `TODO` stub |
| DefenseScheduleReminderJob | 🚧 | Stub — depends on the (unbuilt) defense feature |
| MeetingReminderJob | 🚧 | Stub — depends on the (unbuilt) meetings feature |
| DataCleanupJob | 🚧 | Stub — log/temp-file cleanup not implemented |

---

## Domain Event Handlers

Most handlers (Project, Group, Evaluation, Semester, User) are implemented. Known stubs in `Infrastructure/EventHandlers/TopicPool/`:

- `PoolTopicExpiredEventHandler` — `TODO: Notify mentor about expired topic`
- `TopicRegistrationRequestedEventHandler` — `TODO: Notify mentor about pending registration`
- `TopicRegistrationConfirmedEventHandler` — `TODO: Notify group about confirmation`
- `TopicRegistrationRejectedEventHandler` — `TODO: Notify group about rejection`

These handlers are wired into the event pipeline but currently no-op on the notification side.

---

## Known Gaps / Follow-ups

- **Reports (PDF/Excel)** — no feature or endpoints; `ExcelService` exists but is unused. PDF generation not started.
- **Real-time chat** — hub, repositories, and Mongo documents exist, but no `Chats` Application feature or endpoints to expose conversations/messages.
- **Meetings** — feature and endpoint folders are empty; `MeetingReminderJob` is stubbed pending this.
- **Defense schedule** — no aggregate/feature wired; `DefenseScheduleReminderJob` is stubbed.
- **Reminder jobs** (`EvaluationReminderJob`, `DataCleanupJob`) are scheduled but empty — they run on cron but do nothing yet.
- **TopicPool notification handlers** (4 above) need their notification bodies implemented.
- **Redis health check** is a `TODO`; only SQL Server and MongoDB are health-checked.
