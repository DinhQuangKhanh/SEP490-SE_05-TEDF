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
| **User** | `Users`, `UserRoles`, `Students`, `Lecturers` |
| **Project** | `Projects`, `ProjectMentors`, `Documents` |
| **TopicPool** | `TopicPools`, `TopicRegistrations` |
| **Group** | `Groups`, `GroupMembers`, `GroupInvitations`, `GroupJoinRequests` |
| **Semester** | `Semesters`, `SemesterPhases`, `EligibleStudents` |
| **Evaluation** | `EvaluationSubmissions`, `ProjectEvaluatorAssignments` |
| **EvaluationChecklist** | `ChecklistConfigs`, `ChecklistCriteria`, `ProjectEvaluationChecklists`, `ChecklistResultItems` |
| **Support** | `SupportTickets`, `TicketMessages`, `SupportTicketAttachments`, `TicketMessageAttachments` |
| **Standalone / reference** | `Departments`, `Majors`, `SystemConfigurations`, `ProjectArchives`, `Roles`, `Programs`, `Combos` |

> **Not persisted:** the `Defense` and `Meeting` aggregates exist in the Domain but have **no tables / `DbSet`s** yet — those features are not wired (see `backend/docs/PROJECT-STATUS.md`).

### Table Reference

#### User aggregate

- **`Users`** — application user (Firebase-authenticated; no ASP.NET Identity). Shared profile only: `FullName`, `Email`, `AvatarUrl`, `PhoneNumber`, `BirthDate`, `PrivacySettings`, `DepartmentId`, `Status`, `FirebaseUid`. Student-specific and lecturer-specific data live in separate tables below.
- **`UserRoles`** — role assignments per user. `RoleId int FK → Roles.Id` (unique index `(UserId, RoleId)`). Drives the role claims injected at token validation.
- **`Students`** — student-specific profile (shared-PK pattern: `Id uniqueidentifier` is both PK and FK → `Users.Id` cascade). Columns: `StudentCode nvarchar(20) UNIQUE`, `ProgramId int? FK → Programs` (set-null), `ComboId int? FK → Combos` (set-null). Access via `User.Student` navigation property.
- **`Lecturers`** — lecturer/staff-specific profile (shared-PK: `Id` PK + FK → `Users.Id` cascade). Columns: `EmployeeCode nvarchar(20) UNIQUE`, `AcademicTitle nvarchar(50)?`. Access via `User.Lecturer` navigation property.

#### Project aggregate

- **`Projects`** — core thesis topic/project. Key columns: `Code` (`ProjectCode`, unique, ≤20), `NameVi`/`NameEn` (`ProjectName`, ≤350), `Description`/`Objectives`/`Scope`/`ExpectedResults` (≤2000), `Technologies` (`TechnologyStack`, ≤500), `Status`/`Priority`/`SourceType`/`RegistrationType` (int enums), `PoolStatus` (string enum), `SemesterId`, `MajorId`, `GroupId?`, `MentorFeedback nvarchar(max)?` (mentor's review note for DirectRegistration topics).
- **`ProjectMentors`** — mentors on a project (FK `ProjectId` → cascade), with `IsActive`.
- **`Documents`** — uploaded project documents (FK `ProjectId` → cascade, `UploadedBy` → restrict). Has an explicit soft-delete query filter.

#### TopicPool aggregate

- **`TopicPools`** — mentor-proposed topics available for registration; status + expiration.
- **`TopicRegistrations`** — a group's registration against a pool topic; status lifecycle (requested → confirmed/rejected/cancelled).

#### Group aggregate

- **`Groups`** — student group. `Code` (`GroupCode`, **unique**, `nvarchar(30)`), `Name nvarchar(20) NOT NULL`, `DisplayName nvarchar(200)?`, `Status`, `SemesterId` (restrict), `LeaderId` (set-null), `ProjectId?` (set-null). Indexed on `Status`, `SemesterId`, `LeaderId`, `ProjectId`.

  **Code / Name format (enforced since `EnforceGroupCodeFormat`, 2026-07-25):**

  | Column | Format | Example |
  |---|---|---|
  | `Code` | `{SemesterCode}-SE_{NN}` | `SUMMER2026-SE_01` |
  | `Name` | `SE_{NN}` — the tail of `Code` | `SE_01` |
  | `DisplayName` | free text, optional | `Team TEDF` |

  - The sequence is **at least two digits** and **restarts at 01 in every semester** (`IGroupRepository.GetNextSequenceAsync(semesterId)` takes `MAX(sequence)` within the semester, and counts soft-deleted rows so a code is never reused). Past 99 it simply grows: `SE_100`.
  - `Name` is **derived** from `Code` in `Group.Create` (`code.NamePart`) — it is not settable, so the two cannot drift apart. `DisplayName` is the students' own nickname and is the only part they choose (`Group.SetDisplayName`).
  - The format is validated by a regex inside `GroupCode.Create`, and `GroupCodeConverter` routes through that same method — so **an out-of-format code throws on read as well as on write**. Any row written outside the app (seeders, manual SQL) must follow the scheme or EF will fail to materialise the group.
  - `MaxLength` is 30 = longest `SemesterCode` (20) + `-SE_` + a 4-digit sequence.
- **`GroupMembers`** — membership (FK `GroupId` cascade, `StudentId` restrict), `IsActive`, role (leader/member). Filtered unique index **`UX_GroupMembers_GroupId_StudentId_Active`** on `(GroupId, StudentId)` with filter `[Status] = 0` (= `Active`), added 2026-07-23 — a student cannot hold two active memberships in the same group, while historical left/removed rows are still allowed.
- **`GroupInvitations`** — leader-issued invitations (`InviterId`/`InviteeId` restrict). Indexed on `(GroupId, InviteeId, Status)`.
- **`GroupJoinRequests`** — student-initiated join requests; indexed on `(Status, ExpiresAt)` and `(GroupId, StudentId, Status)`. Expired by a Hangfire job.

#### EvaluationChecklist aggregate

Added by the `AddEvaluationChecklist` migration (2026-07-14), scoring columns by `AddChecklistScoring` (2026-07-20).

- **`ChecklistConfigs`** — a reusable set of grading criteria, scoped to a semester (`SemesterId` FK → `Semesters`, **restrict**) with an active/inactive flag. Populated by hand or via Excel import.
- **`ChecklistCriteria`** — the criteria rows of a config (`ChecklistConfigId` FK → **cascade**). Carry `MaxScore` / `PassScore`.
- **`ProjectEvaluationChecklists`** — one evaluator's filled-in checklist for one project. FKs to `Projects`, `Users` (the evaluator) and `ChecklistConfigs` are all **restrict**, so those rows cannot be deleted while a checklist references them.
- **`ChecklistResultItems`** — per-criterion result (`Score`, `Comment`) belonging to a `ProjectEvaluationChecklists` row (**cascade**).

> The restrict-FKs matter for any teardown routine: delete `ChecklistResultItems` → `ProjectEvaluationChecklists` → `ChecklistCriteria` → `ChecklistConfigs` *before* touching `Projects`, `Users` or `Semesters`.

#### Semester aggregate

- **`Semesters`** — academic semester (`SemesterCode`, `AcademicYear`), status.
- **`SemesterPhases`** — phases within a semester (date ranges; must not overlap).
- **`EligibleStudents`** — students eligible to participate in a semester (bulk-imported via the admin import endpoint).

#### Evaluation aggregate

- **`EvaluationSubmissions`** — an evaluation round for a project. Unique index on `(ProjectId, SubmissionNumber)`; indexed on `AssignedEvaluatorId`, `SubmittedBy`, `Status`, `Result`, `SubmittedAt`. FK `ProjectId` (no-action). `PhaseId int NOT NULL` (added 2026-07-20) records which semester phase the evaluation belongs to.
- **`ProjectEvaluatorAssignments`** — the (2) evaluators assigned to a project per round; `EvaluatorOrder`, `IndividualResult`, `IsActive`, `PhaseId int NOT NULL`. Indexed on `(ProjectId, EvaluatorId)` and `(ProjectId, EvaluatorOrder)`.

> `PhaseId` on both evaluation tables is a **plain int with no FK to `SemesterPhases`** and was backfilled with `0` for existing rows — treat `0` as "recorded before phase tracking existed", not as a valid phase.

#### Support aggregate

- **`SupportTickets`** — support ticket (`TicketCode`), status, category. Owns:
  - **`TicketMessages`** — thread messages (owned collection),
  - **`SupportTicketAttachments`** / **`TicketMessageAttachments`** — file attachments (owned collections).

#### Standalone / reference

- **`Departments`** — `Code` (unique), `IsActive`, `HeadOfDepartmentId` (FK → `Users`, set-null).
- **`Majors`** — `Code` (unique), `DepartmentId` (restrict), `IsActive`.
- **`SystemConfigurations`** — key/value config; `Key` (unique), `Category`.
- **`ProjectArchives`** — archived project snapshots; indexed on `MajorId`, `AcademicYear`, `(ProjectName, Tags)`.
- **`Roles`** — normalized role lookup (5 seed rows with stable IDs: Admin=1, Mentor=2, Student=3, Evaluator=4, DepartmentHead=5). `Name nvarchar(50) UNIQUE`. Referenced by `UserRoles.RoleId`. Constants exposed via `DomainRoleIds` and `DomainRoleNames`.
- **`Programs`** — training programs (formerly `MajorPrograms`). 24 seed rows. Columns: `Code nvarchar(50) UNIQUE`, `Name nvarchar(500)`, `Description nvarchar(max)`, `TotalCredit int`. Referenced by `Students.ProgramId` (set-null).
- **`Combos`** — narrow specialization tracks within a program. 10 seed rows with explicit IDs. Columns: `Name nvarchar(500)`, `Abbr nvarchar(20)`. Referenced by `Students.ComboId` (set-null). The display string is formed as `{ProgramCode without 'K'}_{Abbr}` (e.g. `BIT_SE_18C_.NET`).

### Referential Integrity (delete behavior)

EF configures FK delete behavior explicitly per relationship:

| Behavior | Used for (examples) |
|----------|---------------------|
| **Cascade** | aggregate children — `ProjectMentors`/`Documents` → `Projects`; `GroupMembers`/`GroupInvitations`/`GroupJoinRequests` → `Groups`; assignments → evaluation |
| **Restrict** | references that must not orphan — `Majors` → `Departments`; `Projects` → `Semester`/`Major`; member/inviter/invitee/student → `Users` |
| **SetNull** | optional links — `Departments.HeadOfDepartmentId`, `Groups.LeaderId`, `Groups.ProjectId`, `EvaluationSubmissions.AssignedBy` |
| **NoAction** | `EvaluationSubmissions` → `Projects` / `AssignedEvaluatorId` (avoid multiple-cascade-path conflicts) |

### Indexes (selected)

- **Unique:** `Departments.Code`, `Majors.Code`, `Groups.Code`, `SystemConfigurations.Key`, `EvaluationSubmissions(ProjectId, SubmissionNumber)`, `Students.StudentCode`, `Lecturers.EmployeeCode`, `Roles.Name`, `Programs.Code`, `UserRoles(UserId, RoleId)`.
- **Lookup/filter:** status and foreign-key columns across most tables (`Status`, `SemesterId`, `ProjectId`, `EvaluatorId`, `AssignedEvaluatorId`, …) plus composite indexes for invitation/join-request/registration uniqueness-style lookups.

---

## Part 2 — MongoDB (`TEDFLogs`)

Document store for high-write, append-mostly, and flexible-schema data. Collection names are `snake_case`; serializers are configured once at startup (`MongoSerializerConfiguration`) and indexes created on startup (`MongoIndexConfiguration`).

| Collection | Document | Purpose |
|------------|----------|---------|
| `conversations` | `ConversationDocument` | Chat conversations (participants, type) |
| `messages` | `MessageDocument` | Chat messages (conversation, sender, content, sentAt) |
| `notifications` | `NotificationDocument` | User notifications (category, isRead) — paired with SignalR |
| `activity_logs` | `ActivityLogDocument` | Consolidated **request-scoped** user-action audit: `ActionCode` (command class name), `ActionName` (human-readable), `FeatureCategory`, `Role`, `RequestPath`, `EntityType/EntityId`, `Status`, `DurationMs`, `CorrelationId`, `IpAddress`. Replaces the former `user_activity_logs`, `evaluation_logs`, `project_modifications`, and `request_logs` collections. |
| `error_logs` | `ErrorLogDocument` | Full unhandled-exception detail (stack trace, inner chain, correlation id). Linked to an `activity_logs` entry via `CorrelationId` for drill-down from the admin log view. |
| `system_audit_logs` | `SystemAuditLogDocument` | **Per-entity** audit trail: `EntityType` + `EntityId`, `Action`, `PerformedBy`, `OldValuesJson`/`NewValuesJson`, free-form `Metadata`. Written through `ISystemAuditLogWriteService`; read by `GET /api/projects/{id}/audit-logs`. |
| `quarantined_attachments` | `QuarantinedAttachmentDocument` | Uploaded files quarantined by the ClamAV malware scan |

Collection-name constants live in `MongoDbContext.Collections` (`ActivityLogs`, `ErrorLogs`, `SystemAuditLogs`, `Notifications`, `Conversations`, `Messages`).

> **Removed collections (2026-07-19 logging refactor):** `user_activity_logs`, `evaluation_logs`, `project_modifications`, `request_logs` were replaced by `activity_logs`. The `IActivityLogRepository` + `ActivityLogService` pair now handles all write paths previously spread across four separate repositories.
>
> **`system_audit_logs` was removed in that refactor and restored on 2026-07-25**, once the project audit-log feature (PR #96) was built on it. The two are not interchangeable: `activity_logs` answers *"what requests did this user make"*, `system_audit_logs` answers *"who changed this specific entity"*. Keep both.

### Logging linkage

On an unhandled exception, `ExceptionHandlingMiddleware` writes the full detail to `error_logs` and stamps the linked `activity_logs` entry (same `CorrelationId`) with `Status = "Failure"`, so the admin log page can drill into the underlying error without a separate query.

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

- **Migrations added in July 2026** (newest last):

  | Migration | What it did |
  |---|---|
  | `AddProjectMentorFeedback` (07-13) | `Projects.MentorFeedback` for DirectRegistration review notes |
  | `AddEvaluationChecklist` (07-14) | The four `Checklist*` tables |
  | `AddPhaseIdToEvaluations` (07-20) | `PhaseId` on `EvaluationSubmissions` + `ProjectEvaluatorAssignments` |
  | `AddChecklistScoring` (07-20) | `Score`/`MaxScore`/`PassScore`/`Comment` on the checklist tables |
  | `RefactorUserSchema` (07-20) | Split `Students`/`Lecturers`/`Roles`/`Combos` out of `Users`; `Programs` replaces `MajorPrograms` |
  | `AddGroupMemberUniqueActiveIndex` (07-23) | Filtered unique index for one active membership per (group, student) |
  | `EnforceGroupCodeFormat` (07-25) | `Groups.Code` → `nvarchar(30)`, new `DisplayName`, `Name` → `nvarchar(20) NOT NULL`; **backfills every row** to `{SemesterCode}-SE_NN` / `SE_NN` |

  > `EnforceGroupCodeFormat` is not a pure schema migration — its `Up()` renumbers all existing groups (`ROW_NUMBER()` per semester, ordered by `CreatedAt`) and moves the old free-text `Name` into `DisplayName`. It must run before the app starts, otherwise `GroupCodeConverter` rejects the legacy codes on read. The step order inside `Up()` is deliberate; do not reorder it.

---

> **Maintenance note:** when you change the domain model, update the matching `IEntityTypeConfiguration<T>`, add an EF migration, and update this file. For exact column types and lengths, the configuration classes under `TEDF.Persistence/SqlServer/Configurations/` are the source of truth.
