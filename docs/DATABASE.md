# TEDF — Database

Data model reference for the TEDF Thesis Management System. TEDF uses **polyglot persistence**:

| Store | Engine | Holds | Access |
|-------|--------|-------|--------|
| **Relational** | SQL Server (EF Core 8) | Transactional domain data — users, projects, groups, topic pools, semesters, evaluations, supports | `AppDbContext` + repositories |
| **Document** | MongoDB (`TEDFLogs`) | Write-heavy / append data — chat, notifications, audit / activity / error / request logs, quarantined attachments | `MongoDbContext` + Mongo repositories |

> The authoritative schema is the EF Core model (`TEDF.Persistence/SqlServer/Configurations/`) + migrations, and the MongoDB document classes (`TEDF.Persistence/MongoDB/Documents/`). This document summarizes them.

---

## Part 1 — SQL Server (Relational)

### Conventions

- **Primary keys:** `Guid` for aggregate roots and their entities (User, Project, Group, TopicPool, Evaluation, Support); `int` identity for reference/config data (Department, Major, Semester, SemesterPhase) and integer child keys (invitation / join-request ids).
- **Auditing:** entities deriving from `AuditableEntity` carry `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`. These are stamped automatically by `AuditableEntityInterceptor` — never set them by hand.
- **Soft delete:** deletes are converted to an `IsDeleted` flag by `SoftDeleteInterceptor` (no hard deletes); query filters exclude soft-deleted rows.
- **Enums** are stored as `int` via `HasConversion<int>` (e.g. `Status`, `Priority`, `SourceType`, `RegistrationType`). A few are stored as strings where readability matters (e.g. `Project.PoolStatus` → `string`).
- **Value objects** map to plain columns via custom EF value converters (`ValueConverters/`): `ProjectCode`, `ProjectName`, `TechnologyStack`, `GroupCode`, `SemesterCode`, `AcademicYear`, `TicketCode`, `SubmissionNumber`, `ProjectSnapshot`, `DateOnly`.
- **Domain events** are not persisted — they are dispatched after `SaveChanges` by `DomainEventInterceptor`.
- All `OnModelCreating` config is applied from `IEntityTypeConfiguration<T>` classes via `ApplyConfigurationsFromAssembly`.

### Aggregates → Tables

| Aggregate (root) | Tables |
|------------------|--------|
| **User** | `Users`, `UserRoles` |
| **Project** | `Projects`, `ProjectMentors`, `Documents` |
| **TopicPool** | `TopicPools`, `TopicRegistrations` |
| **Group** | `Groups`, `GroupMembers`, `GroupInvitations`, `GroupJoinRequests` |
| **Semester** | `Semesters`, `SemesterPhases`, `EligibleStudents` |
| **Evaluation** | `EvaluationSubmissions`, `ProjectEvaluatorAssignments` |
| **Support** | `SupportTickets`, `TicketMessages`, `SupportTicketAttachments`, `TicketMessageAttachments` |
| **Standalone** | `Departments`, `Majors`, `SystemConfigurations`, `ProjectArchives` |

> **Not persisted:** the `Defense` and `Meeting` aggregates exist in the Domain but have **no tables / `DbSet`s** yet — those features are not wired (see `backend/docs/PROJECT-STATUS.md`).

### Table Reference

#### User aggregate

- **`Users`** — application user (Firebase-authenticated; no ASP.NET Identity). Holds profile, `FullName`, email, status. One user → many `UserRoles`.
- **`UserRoles`** — role assignments per user (Admin, Mentor, Student, Evaluator, DepartmentHead). Drives the role claims injected at token validation.

#### Project aggregate

- **`Projects`** — core thesis topic/project. Key columns: `Code` (`ProjectCode`, unique, ≤20), `NameVi`/`NameEn` (`ProjectName`, ≤350), `Description`/`Objectives`/`Scope`/`ExpectedResults` (≤2000), `Technologies` (`TechnologyStack`, ≤500), `Status`/`Priority`/`SourceType`/`RegistrationType` (int enums), `PoolStatus` (string enum), `SemesterId`, `MajorId`, `GroupId?`.
- **`ProjectMentors`** — mentors on a project (FK `ProjectId` → cascade), with `IsActive`.
- **`Documents`** — uploaded project documents (FK `ProjectId` → cascade, `UploadedBy` → restrict). Has an explicit soft-delete query filter.

#### TopicPool aggregate

- **`TopicPools`** — mentor-proposed topics available for registration; status + expiration.
- **`TopicRegistrations`** — a group's registration against a pool topic; status lifecycle (requested → confirmed/rejected/cancelled).

#### Group aggregate

- **`Groups`** — student group. `Code` (`GroupCode`, **unique**), `Status`, `SemesterId` (restrict), `LeaderId` (set-null), `ProjectId?` (set-null). Indexed on `Status`, `SemesterId`, `LeaderId`, `ProjectId`.
- **`GroupMembers`** — membership (FK `GroupId` cascade, `StudentId` restrict), `IsActive`, role (leader/member).
- **`GroupInvitations`** — leader-issued invitations (`InviterId`/`InviteeId` restrict). Indexed on `(GroupId, InviteeId, Status)`.
- **`GroupJoinRequests`** — student-initiated join requests; indexed on `(Status, ExpiresAt)` and `(GroupId, StudentId, Status)`. Expired by a Hangfire job.

#### Semester aggregate

- **`Semesters`** — academic semester (`SemesterCode`, `AcademicYear`), status.
- **`SemesterPhases`** — phases within a semester (date ranges; must not overlap).
- **`EligibleStudents`** — students eligible to participate in a semester (bulk-imported via the admin import endpoint).

#### Evaluation aggregate

- **`EvaluationSubmissions`** — an evaluation round for a project. Unique index on `(ProjectId, SubmissionNumber)`; indexed on `AssignedEvaluatorId`, `SubmittedBy`, `Status`, `Result`, `SubmittedAt`. FK `ProjectId` (no-action).
- **`ProjectEvaluatorAssignments`** — the (2) evaluators assigned to a project per round; `EvaluatorOrder`, `IndividualResult`, `IsActive`. Indexed on `(ProjectId, EvaluatorId)` and `(ProjectId, EvaluatorOrder)`.

#### Support aggregate

- **`SupportTickets`** — support ticket (`TicketCode`), status, category. Owns:
  - **`TicketMessages`** — thread messages (owned collection),
  - **`SupportTicketAttachments`** / **`TicketMessageAttachments`** — file attachments (owned collections).

#### Standalone / reference

- **`Departments`** — `Code` (unique), `IsActive`, `HeadOfDepartmentId` (FK → `Users`, set-null).
- **`Majors`** — `Code` (unique), `DepartmentId` (restrict), `IsActive`.
- **`SystemConfigurations`** — key/value config; `Key` (unique), `Category`.
- **`ProjectArchives`** — archived project snapshots; indexed on `MajorId`, `AcademicYear`, `(ProjectName, Tags)`.

### Referential Integrity (delete behavior)

EF configures FK delete behavior explicitly per relationship:

| Behavior | Used for (examples) |
|----------|---------------------|
| **Cascade** | aggregate children — `ProjectMentors`/`Documents` → `Projects`; `GroupMembers`/`GroupInvitations`/`GroupJoinRequests` → `Groups`; assignments → evaluation |
| **Restrict** | references that must not orphan — `Majors` → `Departments`; `Projects` → `Semester`/`Major`; member/inviter/invitee/student → `Users` |
| **SetNull** | optional links — `Departments.HeadOfDepartmentId`, `Groups.LeaderId`, `Groups.ProjectId`, `EvaluationSubmissions.AssignedBy` |
| **NoAction** | `EvaluationSubmissions` → `Projects` / `AssignedEvaluatorId` (avoid multiple-cascade-path conflicts) |

### Indexes (selected)

- **Unique:** `Departments.Code`, `Majors.Code`, `Groups.Code`, `SystemConfigurations.Key`, `EvaluationSubmissions(ProjectId, SubmissionNumber)`.
- **Lookup/filter:** status and foreign-key columns across most tables (`Status`, `SemesterId`, `ProjectId`, `EvaluatorId`, `AssignedEvaluatorId`, …) plus composite indexes for invitation/join-request/registration uniqueness-style lookups.

---

## Part 2 — MongoDB (`TEDFLogs`)

Document store for high-write, append-mostly, and flexible-schema data. Collection names are `snake_case`; serializers are configured once at startup (`MongoSerializerConfiguration`) and indexes created on startup (`MongoIndexConfiguration`).

| Collection | Document | Purpose |
|------------|----------|---------|
| `conversations` | `ConversationDocument` | Chat conversations (participants, type) |
| `messages` | `MessageDocument` | Chat messages (conversation, sender, content, sentAt) |
| `notifications` | `NotificationDocument` | User notifications (category, isRead) — paired with SignalR |
| `evaluation_logs` | `EvaluationLogDocument` | Evaluation action/audit trail |
| `project_modifications` | `ProjectModificationHistoryDocument` | Field-level project change history (old/new value, by, at) |
| `user_activity_logs` | `UserActivityLogDocument` | User activity audit (action, role, route, severity) |
| `system_audit_logs` | `SystemAuditLogDocument` | System-level audit events |
| `error_logs` | `ErrorLogDocument` | Full unhandled-exception detail (stack trace, inner chain, correlation id) |
| `request_logs` | `RequestLogDocument` | HTTP request logs (method, path, status, duration) |
| `quarantined_attachments` | `QuarantinedAttachmentDocument` | Uploaded files quarantined by the ClamAV malware scan |

Collection-name constants for the core set live in `MongoDbContext.Collections`.

### Logging linkage

On an unhandled exception, `ExceptionHandlingMiddleware` writes the full detail to `error_logs` and a linked summary entry to `user_activity_logs` (with the `ErrorLogId`), so the admin activity feed can drill into the underlying error.

### Why MongoDB here

- High write throughput for logs/chat without relational joins.
- Flexible schema for varying log shapes.
- TTL indexes for automatic cleanup of old entries.
- Append-heavy, read-light access pattern.

---

## Part 3 — Migrations & Seeding

- **Migrations** live in `TEDF.Persistence/Migrations` (the Persistence assembly is the migrations assembly). The SQL connection uses `EnableRetryOnFailure(3)`.

  ```powershell
  # add a migration after changing the model
  dotnet ef migrations add <Name> --project TEDF.Persistence --startup-project TEDF.API

  # apply migrations
  dotnet ef database update --project TEDF.Persistence --startup-project TEDF.API
  ```

- **Startup init** (`InitializeDatabaseAsync`, Development only): applies pending migrations, runs `DevelopmentDataSeeder` (idempotent), and ensures MongoDB indexes. When the Firebase emulator is enabled, it also runs `LoadTestDataSeeder` (≈1000 users + relationships) and `FirebaseEmulatorSeeder`.

- **Connection strings** (`appsettings`): `DefaultConnection` (SQL Server), `HangfireConnection` (Hangfire job storage — separate SQL DB), and `MongoDbSettings:{ConnectionString, DatabaseName}` (`TEDFLogs`).

---

> **Maintenance note:** when you change the domain model, update the matching `IEntityTypeConfiguration<T>`, add an EF migration, and update this file. For exact column types and lengths, the configuration classes under `TEDF.Persistence/SqlServer/Configurations/` are the source of truth.
