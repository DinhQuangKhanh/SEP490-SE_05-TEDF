# TEDF Backend — Project Status

Snapshot of what is implemented in the TEDF API (`backend/`), by bounded context and by cross-cutting concern. Status is inferred from the source — whether an Application feature has handlers, whether endpoints are mapped, and whether jobs/handlers contain real logic vs. `TODO` stubs.

**Last updated:** 2026-07-25

> Living document. Update a row when a feature gains endpoints, a job is implemented, or a stub is filled in.

## Legend

| Symbol | Meaning                                                                      |
| ------ | ---------------------------------------------------------------------------- |
| ✅     | Done — Application handlers + mapped endpoints, functional                   |
| 🚧     | Partial — built at one layer but not fully wired, or a stub with real intent |
| 📋     | Scaffolded — folder exists but empty / no implementation                     |
| ❌     | Not started                                                                  |

## Summary

- **Core lifecycle is live:** topic pools, direct registration, evaluations, student groups, semesters, supports, notifications, dashboards, and admin/department-head/mentor management are implemented end to end (Application + endpoints).
- **User profile is live:** `GET /api/users/me` (GetMyProfileQuery) + `PUT /api/users/me` (UpdateMyProfileCommand) — phone number, birth date, privacy settings, Programs/Combos display (via `Student.ProgramId` / `Student.ComboId`). Supervisor endpoint `GET /api/projects/supervised` also live.
- **Semester roster is live:** 8 additional endpoints on `/api/semesters/{id}/` — import eligible students/mentors (CSV), assign major program per mentor, bulk-delete, and publish roster. Roster-published domain events enqueue the roster emails (students + lecturers) and notify mentors.
- **Transactional email is live (Firestore Trigger Email):** six flows send mail — roster publish (students + lecturers), topic proposed to the department head, evaluator assigned, evaluator finished, evaluator consensus, and the department head's final decision. Handlers compose one message per recipient and hand them to Hangfire; `FirestoreMailQueue` writes them to the `mail` collection under a deterministic document id, which is what makes delivery exactly-once across retries.
- **User schema normalized (RefactorUserSchema migration):** Student/lecturer/role data extracted from `User` into dedicated `Students`, `Lecturers`, `Roles` tables; `MajorPrograms` replaced by `Programs` + `Combos`; `User` retains only shared profile fields (`FullName`, email, `DepartmentId`, `PhoneNumber`, `BirthDate`, `PrivacySettings`, `FirebaseUid`). Navigation properties `User.Student?` / `User.Lecturer?` loaded via EF LEFT JOIN.
- **Admin system settings are live:** the `SystemConfiguration` store is wired (`ISystemSettingsService` + cache), with admin settings CRUD, public branding (color/header/logo), logo upload, test email, backend-enforced maintenance mode, and the Project Archives feature. Registration rules (`MaxGroupMembers`, `AllowDirectRegistration`) are enforced from settings.
- **Pool-topic registration flow is fully wired:** registration request → mentor confirm/reject → notifications (SignalR + MongoDB) + real-time status updates to the student SPA. Cancel-registration endpoint live.
- **DirectRegistration flow extended (PR #88):** `MentorFeedback` field added to `Projects`; mentor now attaches a review note when approving or requesting modification. Semester eligibility check wired into `EvaluationsDomainService` and `StudentGroupsDomainService`; `ISemesterRepository` extended with an eligibility lookup.
- **AccountAccessGate middleware** is live: returns 403 with `ACCOUNT_LOCKED` or `NOT_ELIGIBLE` codes for locked/ineligible accounts on non-public routes.
- **MongoDB logging consolidated (2026-07-19):** four legacy collections (`user_activity_logs`, `evaluation_logs`, `project_modifications`, `request_logs`) replaced by `activity_logs` (`ActivityLogDocument` — unified command audit with `ActionCode`, `ActionName`, `FeatureCategory`, `CorrelationId`) and `error_logs`. `IActivityLogRepository` + `ActivityLogService` are the single write path.
- **`system_audit_logs` kept (2026-07-25):** the per-entity audit trail was restored after PR #96 built the project audit-log feature on it. It answers "who changed what on *this* project/group", which `activity_logs` (a request-scoped log) does not. Three collections total: `activity_logs`, `error_logs`, `system_audit_logs`.
- **Evaluation checklist is live (merged 2026-07-23):** `EvaluationChecklists` Application feature (7 commands / 6 queries), `ChecklistEndpoints` — evaluator per-project checklist (`GET`/`PUT /api/evaluations/…/checklist`), dept-head review of another evaluator's checklist, and `/api/checklist-configs` CRUD with Excel import + preview, copy, activate/deactivate. Four new tables: `ChecklistConfigs`, `ChecklistCriteria`, `ProjectEvaluationChecklists`, `ChecklistResultItems`.
- **Project audit logs (2026-07-24):** `GET /api/projects/{id}/audit-logs` backed by `ISystemAuditLogWriteService` → `system_audit_logs`; surfaced in the dept-head SPA.
- **Evaluation phase tracking (2026-07-24):** `PhaseId` added to evaluations (`AddPhaseIdToEvaluations`), tying a submission to the semester phase it belongs to.
- **Group identity standardised (2026-07-25):** `Groups.Code` is now `{SemesterCode}-SE_NN` (e.g. `SUMMER2026-SE_01`) and `Groups.Name` is the `SE_NN` tail, derived from the code rather than typed by the student. The nickname students used to enter moved to a new `DisplayName` column. Sequence numbering restarts per semester. Enforced by a regex in `GroupCode.Create` — which `GroupCodeConverter` also uses, so the rule holds on read as well as on write. Migration `EnforceGroupCodeFormat` backfills existing rows.
- **Auth, caching (L1/L2), domain events, SignalR, and Hangfire scheduling** are wired.
- **Endpoint structure:** `Endpoints/` is feature-based — `ActivityLogs`, `Archives`, `Authentications`, `Dashboard`, `DirectTopics`, `Evaluations` (`EvaluationsEndpoints` + `ChecklistEndpoints`), `Groups`, `Majors`, `Notifications`, `Projects`, `Semesters`, `Settings`, `SupportTickets`, `Topics` (`TopicCatalogEndpoints` + `TopicPoolsEndpoints`), `Users`. Note `EvaluationChecklists` is an Application feature folder but its endpoints live under `Endpoints/Evaluations/`.
- **Not built / in progress:** Reports (PDF/Excel), real-time Chat feature, and Meetings — their folders are scaffolded but empty.

---

## By Bounded Context

| Context            | App files | Endpoints                | Status | Notes                                                                                                 |
| ------------------ | --------- | ------------------------ | ------ | ----------------------------------------------------------------------------------------------------- |
| TopicPools         | 19        | 11 (`Topics`)            | ✅     | Pool CRUD, registration lifecycle (request/confirm/reject/cancel + note-attachment), mentor topic update/resubmit; registration notifications & real-time |
| Evaluations        | 19+       | 12 (`Evaluations`)       | ✅     | Evaluator self-service (submit, history, filter-options, projects, review, similarity) + dept-head management (evaluators, assign-evaluator, final-decision) + 3 checklist routes (`ChecklistEndpoints`). Submissions carry `PhaseId`. |
| EvaluationChecklists | 13      | 8 (`/api/checklist-configs`) | ✅ | Checklist config CRUD + Excel import/preview + copy + activate/deactivate; per-project checklist scoring (`Score`, `MaxScore`, `PassScore`, `Comment`). Merged 2026-07-23. |
| StudentGroups      | 37        | 13 (`Groups`)            | ✅     | Create/invite/join + bulk-approve/reject join requests + invitable-students picker (`/api/groups`). Code/Name are server-generated (`{SemesterCode}-SE_NN` / `SE_NN`); the create payload's `name` is stored as the optional `DisplayName` nickname |
| Supports           | 20        | 6 (`SupportTickets`)     | ✅     | Support tickets (stats, list, detail, create, reply, status) — full event-handler coverage for all ticket lifecycle events |
| Semesters          | 15+       | 16 (`Semesters`)         | ✅     | Semester + phase lifecycle + full roster: `eligible-students`, `eligible-mentors`, `import`, `bulk-delete`, `major-update`, `roster/publish`; roster-published event handlers live |
| DirectTopics       | 14        | 5 (`DirectTopics`)       | ✅     | Student-initiated topic flow + mentor review/modification-request — all three event handlers wired (`Submitted`, `MentorApproved`, `MentorRequestedModification`) |
| Dashboard          | 9         | 4 (`Dashboard`)          | ✅     | Per-role dashboards unified: `/api/dashboard/{admin,mentor,department-head,evaluator}` |
| Notifications      | 9         | 5 (`Notifications`)      | ✅     | Notification management (list, unread-count, mark-read, mark-all-read) + SignalR |
| Topics             | 7         | 4 (`Topics`)             | ✅     | Topic catalog: list, mentor topics (`/api/topics/mentor`), detail, documents |
| Projects           | 3+        | 4 (`Projects`)           | ✅     | Admin project list, dept-head department projects, **mentor supervised projects** (`/api/projects/supervised`), **per-project audit trail** (`GET /api/projects/{id}/audit-logs`) |
| Users              | 7+        | 6 (`Users`)              | ✅     | List + lock/unlock + assign-department-head + **`GET /api/users/me`** (GetMyProfile) + **`PUT /api/users/me`** (UpdateMyProfile) |
| ActivityLogs       | —         | 5 (`ActivityLogs`)       | ✅     | Activity/error logs (`/api/activity-logs`). Admin SPA page has tabbed view (activity / error), filters by role/feature/status, pagination, and clear-log action. |
| Settings           | 13        | 5 (`Settings`)           | ✅     | `SystemConfiguration` store + cache; admin GET/PUT settings, public branding, logo upload, test email |
| Archives           | 3         | 2 (`Archives`)           | ✅     | Project archive list (by year) + download |
| Majors             | —         | 1 (`Majors`)             | ✅     | Major list endpoint (`/api/majors`) |
| Authentications    | 0         | 1 (`Authentications`)    | ✅     | Auth session endpoint (`GET /api/auth/session`); auth itself is Firebase + JWT middleware |
| Reports            | 0         | 0 (`Reports`)            | 📋     | Empty feature + empty endpoint folder; PDF/Excel reporting not built |
| Chats              | 0         | 0 (`Chats`)              | 🚧     | No feature/endpoints, but `ChatHub` + Conversation/Message repos + Mongo docs exist |
| Meetings           | 0         | 0 (`Meetings`)           | 📋     | Empty feature + empty endpoint folder |

---

## Cross-Cutting Infrastructure

| Concern                          | Status | Notes                                                                                                       |
| -------------------------------- | ------ | ----------------------------------------------------------------------------------------------------------- |
| Firebase auth + JWT bearer       | ✅     | Token validation, Firebase UID → DB user claim resolution, role claims. Firebase Auth emulator seed script available. |
| Authorization handlers           | ✅     | Permission, ProjectOwner, GroupMember, GroupLeader, MentorOfProject, DepartmentHeadOfDepartment             |
| AccountAccessGate middleware      | ✅     | Blocks locked (`ACCOUNT_LOCKED`) and ineligible (`NOT_ELIGIBLE`) accounts on all non-public routes; allowlists `/api/auth/session`, `/api/settings/public`, `/health` |
| CQRS pipeline (MediatR)          | ✅     | Logging → Caching → CacheInvalidation → Validation behaviors                                                |
| Hybrid caching (L1 + L2)         | ✅     | Memory + Redis with cross-instance L1 invalidation; memory-only fallback when no Redis                      |
| Domain events                    | ✅     | Dispatched after `SaveChangesAsync` via `DomainEventInterceptor`                                            |
| SignalR                          | ✅     | `NotificationHub` (`/hubs/notifications`), `ChatHub` (`/hubs/chat`) mapped; `RealtimeNotificationService` drives real-time pushes |
| Real-time notifications          | ✅     | `RealtimeNotificationService` + MongoDB persistence; `ProjectStatusUpdate` model for pool-registration status |
| Real-time chat                   | 🚧     | Hub + repositories + documents exist; no feature/endpoints to drive it                                      |
| Transactional email (Firestore)  | ✅     | `FirestoreMailQueue` writes one document per recipient to the `mail` collection; the `firebase/firestore-send-email` extension renders `emailTemplates` and does the SMTP delivery. Configured under `FirestoreMail`. Exactly-once via deterministic document ids |
| Email (SMTP, direct)             | ✅     | `SmtpEmailService` (MailKit) + `EmailTemplateService` — now used only by the admin "send test email" action |
| Batch email (roster publish)     | ✅     | `SendRosterPublishedMailJob` enqueued by `EnqueueRosterMailsOnRosterPublishedHandler`; emails eligible students **and** assigned lecturers |
| File storage                     | ✅     | `FirebaseStorageService`                                                                                    |
| Excel import/export              | 🚧     | `ExcelService` consumed by semester roster import and checklist-config import/preview. No Reports feature/endpoints yet |
| PDF reports                      | ❌     | Not implemented                                                                                             |
| Attachment malware scan (ClamAV) | ✅     | Scan workflow + quarantine; degrades gracefully to "unavailable" if ClamAV unreachable                      |
| Upload hardening                 | ✅     | 25 MB limit, request timeout, sliding-window rate limiter on propose-topic upload                           |
| System settings store            | ✅     | `SystemConfiguration` + `ISystemSettingsService` (cached); admin CRUD + anonymous public-settings endpoint  |
| Branding (system-wide)           | ✅     | Primary color / header name / logo stored server-side and applied for all users at startup                  |
| Maintenance mode                 | ✅     | `MaintenanceModeMiddleware` returns 503 to non-admins when enabled (allowlists auth/public-settings/health) |
| Logging / audit (MongoDB)        | ✅     | Three collections: `activity_logs` (request-scoped command audit via `IActivityLogRepository`), `error_logs`, and `system_audit_logs` (per-entity audit trail via `ISystemAuditLogWriteService`). Former `user_activity_logs`, `evaluation_logs`, `project_modifications`, `request_logs` removed. |
| Group member uniqueness          | ✅     | Filtered unique index on `GroupMembers (GroupId, StudentId) WHERE Status = Active` — a student cannot hold two active memberships in the same group (`AddGroupMemberUniqueActiveIndex`) |
| Health checks                    | 🚧     | `sqlserver` + `mongodb` registered; **Redis health check is a `TODO` stub**                                 |
| User profile endpoints           | ✅     | `GET /api/users/me` + `PUT /api/users/me`; fields: PhoneNumber, BirthDate, PrivacySettings                 |
| Students / Lecturers tables      | ✅     | Schema refactored: `Students (Id PK+FK, StudentCode, ProgramId, ComboId)` và `Lecturers (Id PK+FK, EmployeeCode, AcademicTitle)` tách khỏi `Users`; navigation props `User.Student?` / `User.Lecturer?` loaded via EF LEFT JOIN |
| Programs + Combos (chuyên ngành) | ✅     | `Programs` table thay thế `MajorPrograms`; `Combos` cho chuyên ngành hẹp (Abbr); `Student.ProgramId` / `Student.ComboId` FK; exposed qua `MyProfileDto.ProgramId/ProgramCode/ProgramName/ComboId/ComboName` |
| Roles table                      | ✅     | Normalized `Roles` lookup (5 rows, Id cố định: Admin=1, Mentor=2, Student=3, Evaluator=4, DepartmentHead=5); `UserRoles.RoleId int FK` thay thế chuỗi `RoleName`; `DomainRoleIds` constants trong Domain |

---

## Hangfire Jobs

| Job                            | Status | Notes                                                     |
| ------------------------------ | ------ | --------------------------------------------------------- |
| TopicExpirationJob             | ✅     | Auto-closes expired pool topics                           |
| SemesterPhaseTransitionJob     | ✅     | Advances semester phases by date                          |
| GroupJoinRequestExpirationJob  | ✅     | Expires stale join requests                               |
| QuarantineRetryJob (API layer) | ✅     | Re-scans quarantined attachments every 30 min             |
| SendRosterPublishedMailJob     | ✅     | Roster-publish email to eligible students + assigned lecturers |
| MailDispatchJob                | ✅     | Delivers handler-composed emails to the Firestore `mail` collection |
| EvaluationReminderJob          | 🚧     | Registered & scheduled, body is a `TODO` stub             |
| DefenseScheduleReminderJob     | 🚧     | Stub — depends on the (unbuilt) defense feature           |
| MeetingReminderJob             | 🚧     | Stub — depends on the (unbuilt) meetings feature          |
| DataCleanupJob                 | 🚧     | Stub — log/temp-file cleanup not implemented              |

Job scheduling is centralized in `Infrastructure/BackgroundJobs/Scheduling/RecurringJobsConfiguration.cs`.

---

## Domain Event Handlers

### Fully Implemented

| Subfolder | Handlers |
|---|---|
| `Project/` | `ProjectCreatedEventHandler`, `SendTopicProposedMailHandler`, `ProjectSubmittedEventHandler`, `ProjectApprovedEventHandler`, `ProjectRejectedEventHandler`, `ProjectResubmittedEventHandler`, `ProjectModificationRequestedEventHandler`, `ProjectStatusRealtimeNotifier`, `ProjectChecklistSavedRealtimeHandler` |
| `Group/` | `GroupCreatedEventHandler`, `MemberInvitedEventHandler`, `InvitationAcceptedEventHandler`, `InvitationRejectedEventHandler`, `JoinRequestedEventHandler`, `JoinRequestApprovedEventHandler`, `JoinRequestRejectedEventHandler`, `MemberAddedEventHandler`, `MemberRemovedEventHandler` |
| `Evaluation/` | `EvaluationAssignedEventHandler`, `EvaluatorAssignedToProjectEventHandler`, `SendEvaluationAssignedMailHandler`, `EvaluatorSubmittedResultEventHandler`, `SendEvaluationOutcomeMailsHandler`, `EvaluationCompletedEventHandler`, `EvaluationCancelledEventHandler`, `DepartmentHeadFinalDecisionEventHandler`, `SendFinalDecisionMailHandler` |
| `DirectTopic/` | `ProjectSubmittedToMentorEventHandler`, `ProjectMentorApprovedEventHandler`, `ProjectMentorRequestedModificationEventHandler` |
| `Semester/` | `SemesterCreatedEventHandler`, `PhaseStartedEventHandler`, `PhaseUpcomingEventHandler` |
| `Semesters/` | `EnqueueRosterMailsOnRosterPublishedHandler`, `NotifyMentorsOnRosterPublishedHandler` |
| `Support/` | `TicketCreatedEventHandler`, `TicketAssignedEventHandler`, `TicketMessageAddedEventHandler`, `TicketStatusChangedEventHandler`, `TicketClosedEventHandler`, `TicketResolvedEventHandler`, `TicketReopenedEventHandler` |
| `TopicPool/` | `TopicPoolCreatedEventHandler`, `TopicPoolActivatedEventHandler`, `TopicPoolSuspendedEventHandler`, `TopicRegistrationCancelledEventHandler`, `TopicRegistrationConfirmedEventHandler`, `TopicRegistrationRejectedEventHandler`, `TopicRegistrationOutcomeEventHandlerBase` |
| `User/` | `SyncFirebaseClaimsOnUserCreatedHandler`, `SyncFirebaseClaimsOnRoleAssignedHandler` |

### Known Stubs in `TopicPool/`

- `PoolTopicExpiredEventHandler` — `TODO: Notify mentor about expired topic`
- `TopicRegistrationRequestedEventHandler` — `TODO: Notify mentor about pending registration`

These handlers are wired into the event pipeline but currently no-op on the notification side.

`EvaluatorSubmittedResultEventHandler` respects the `EmailOnEvaluationResult` system setting (admin Notifications toggle) before sending the finalized-result notification. The `NotifyMentorOnRegistration` toggle is stored but not yet enforced (its target handler is one of the stubs above).

---

## Known Gaps / Follow-ups

- **Reports (PDF/Excel)** — no feature or endpoints; `ExcelService` exists but is unused. PDF generation not started.
- **Real-time chat** — hub, repositories, and Mongo documents exist, but no `Chats` Application feature or endpoints to expose conversations/messages.
- **Meetings** — feature and endpoint folders are empty; `MeetingReminderJob` is stubbed pending this.
- **Defense schedule** — no aggregate/feature wired; `DefenseScheduleReminderJob` is stubbed.
- **Reminder jobs** (`EvaluationReminderJob`, `DataCleanupJob`) are scheduled but empty — they run on cron but do nothing yet.
- **TopicPool notification stubs** (2 above) need their notification bodies implemented.
- **Redis health check** is a `TODO`; only SQL Server and MongoDB are health-checked.
- **Settings stored but not yet enforced:** `RequireOutlineApproval`, `MaxTopicsPerMentor`, and `NotifyMentorOnRegistration` are persisted and editable in admin settings but not wired into the corresponding flows/handlers yet.
