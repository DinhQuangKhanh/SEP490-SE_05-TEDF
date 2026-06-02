# TEDF Backend — Architecture

Architecture reference for the TEDF API (`backend/`, solution `TEDF.sln`). Built with **Clean Architecture + DDD + CQRS** on .NET 8 / ASP.NET Core Minimal API. This document focuses on *how the backend is wired*; for system-wide diagrams (domain model, DB tables, deployment) see the root [`../../ARCHITECTURE.md`](../../ARCHITECTURE.md).

---

## Table of Contents

1. [Layers & Dependency Rule](#1-layers--dependency-rule)
2. [Composition Root (`Program.cs`)](#2-composition-root-programcs)
3. [HTTP Request Pipeline](#3-http-request-pipeline)
4. [CQRS & the MediatR Pipeline](#4-cqrs--the-mediatr-pipeline)
5. [Domain Layer](#5-domain-layer)
6. [Domain Events](#6-domain-events)
7. [Persistence](#7-persistence)
8. [Caching](#8-caching)
9. [Authentication & Authorization](#9-authentication--authorization)
10. [Real-time, Background Jobs & Cross-cutting Services](#10-real-time-background-jobs--cross-cutting-services)
11. [API Endpoints](#11-api-endpoints)
12. [Error Handling & Response Envelope](#12-error-handling--response-envelope)

---

## 1. Layers & Dependency Rule

Five projects; dependencies point **inward** only. The Domain has zero external dependencies and defines the interfaces that outer layers implement.

```
        ┌──────────────────────────────────────────────┐
        │  TEDF.API  (Presentation)                     │  Minimal API endpoints, Program.cs
        └───────────────┬──────────────────────────────┘
                        │ depends on
   ┌────────────────────┼─────────────────────────────────┐
   ▼                    ▼                                   ▼
┌────────────────┐  ┌────────────────┐            ┌──────────────────┐
│ TEDF.Persistence│  │ TEDF.Infrastructure│        │  TEDF.Application │
│ EF Core, Mongo │  │ Auth, SignalR, …  │ ───────► │  CQRS / MediatR   │
└───────┬────────┘  └────────┬─────────┘            └─────────┬────────┘
        │                    │                                │
        └────────────────────┴───────────► TEDF.Domain ◄──────┘
                                            (no external deps)
```

| Project | Responsibility |
|---------|----------------|
| `TEDF.Domain` | Aggregates, entities, value objects, enums, domain events, business `Rules`, domain services, and **interfaces** (repository contracts, `IUnitOfWork`, `ICurrentUserService`, …). |
| `TEDF.Application` | CQRS use-cases: `Features/<Context>/{Commands,Queries,DTOs}`, MediatR handlers, FluentValidation validators, pipeline behaviors, and the `Common/Interfaces` service contracts it needs. |
| `TEDF.Persistence` | EF Core `AppDbContext` (SQL Server), repositories, interceptors, query services, MongoDB documents/repositories, migrations, seeds. |
| `TEDF.Infrastructure` | Firebase auth, authorization handlers, SignalR hubs, Hangfire jobs, hybrid caching, domain-event handlers, email, file storage, health checks, middleware. |
| `TEDF.API` | Composition root + Minimal API endpoints + Swagger. |

Each outer layer exposes a single `AddXxx` DI extension (`AddApplicationServices`, `AddPersistence`, `AddInfrastructure`) consumed by `Program.cs`.

---

## 2. Composition Root (`Program.cs`)

`Program.cs` is the only place layers are composed. In order, it:

1. **Loads env vars** from `.env` files (`DotEnvLoader.LoadForCurrentEnvironment`) before building configuration.
2. Registers **Swagger** with a JWT Bearer security definition.
3. Configures **CORS** policy `AllowFrontend` from `Cors:AllowedOrigins` (falls back to localhost dev origins), `AllowCredentials`.
4. Sets **upload limits & throttling** for the propose-topic upload path: `FormOptions.MultipartBodyLengthLimit` = 25 MB, a 60 s request-timeout policy, and a sliding-window **rate limiter** (`ProposeTopicUploadPolicy`, 5/min keyed by DB user id or IP) returning the `ApiResponse` envelope on rejection.
5. Registers **attachment malware scanning** (ClamAV) services and jobs (`IMalwareScanner`, `IAttachmentScanWorkflow`, `AttachmentScanJob`, `QuarantineRetryJob`).
6. Calls the three layer registrations: `AddApplicationServices()` → `AddPersistence(config)` → `AddInfrastructure(config)`.
7. Configures **ForwardedHeaders** so the real client IP is read behind a reverse proxy.
8. In **Development**, applies migrations + seeds via `InitializeDatabaseAsync()`, and enables Swagger UI.

---

## 3. HTTP Request Pipeline

Middleware order (from `Program.cs` + `UseInfrastructure`):

```
ForwardedHeaders            # real client IP (must be first)
└─ UseInfrastructure:
     CorrelationIdMiddleware        # X-Correlation-Id for tracing
     RequestLoggingMiddleware       # structured request logs (Serilog)
     ExceptionHandlingMiddleware    # maps exceptions → ApiResponse + status
     PerformanceMonitoringMiddleware# flags slow requests
     UseHangfireDashboard("/hangfire", admin-only)
     RecurringJobsConfiguration.ConfigureRecurringJobs()
RequestTimeouts
RateLimiter
CORS ("AllowFrontend")
HttpsRedirection
Authentication  →  Authorization
MapHealthChecks("/health")
MapHub<NotificationHub>("/hubs/notifications")
MapHub<ChatHub>("/hubs/chat")
MapEndpoints()                      # all IEndpoint groups, reflection-registered
RecurringJob "quarantine-retry"     # API-layer job, every 30 min
```

Hosted endpoints: Swagger `/swagger` (dev), Health `/health`, Hangfire `/hangfire` (Admin only), SignalR `/hubs/*`, REST under `/api/*`.

---

## 4. CQRS & the MediatR Pipeline

Every use-case is a MediatR request. `AddApplicationServices` scans the Application assembly for handlers and validators and registers the behavior chain **in execution order**:

```
Request ─► LoggingBehavior
        ─► CachingBehavior                    (queries: ICachedQuery — short-circuits on hit)
        ─► CacheInvalidationBehavior          (commands: ICacheInvalidatingCommand)
        ─► CacheInvalidationWithResultBehavior (commands with a result)
        ─► ValidationBehavior                 (FluentValidation)
        ─► Handler
```

**Abstractions** (`Application/Common/Abstractions`):

- `ICommand` / `ICommand<TResponse>` — state-changing intents (over `IRequest`).
- `IQuery<TResponse>` / `IQueryHandler` — reads.
- `ICacheInvalidatingCommand` (and `<TResponse>`) — exposes `CachePrefixesToInvalidate`; the invalidation behavior clears those prefixes from L1 + L2 after the command succeeds.
- `ICachedQuery<TResponse>` — exposes `CacheKey` (supports a `{userId}` placeholder; return `null` to skip caching, e.g. search), with per-query `L1Expiration` (default 2 min) and `L2Expiration` (default 15 min).

**Feature layout** — `Features/<Context>/` contains `Commands/<Name>/{Command,Handler,Validator}`, `Queries/<Name>/{Query,Handler}`, and `DTOs/`. Reads frequently bypass repositories in favor of read-optimized **query services** (`I*QueryService`, implemented in Persistence).

> Note: Infrastructure registers MediatR on **its own** assembly separately (so domain-event handlers are discovered); Application registers only Application handlers.

---

## 5. Domain Layer

- **Primitives** (`Common/Primitives`): `Entity<TId>`, `AggregateRoot<TId> : Entity<TId>, IHasDomainEvents`, `ValueObject`, `AuditableEntity`, plus marker interfaces `IIdentifiable`, `ISoftDeletable`.
- **Aggregates** (`Aggregates/<X>Aggregate/`): 9 roots, each owning its entities, value objects, domain `Events/`, and business `Rules/`.
- **Business rules**: implement `IBusinessRule` (`Message`, `Code` defaulting to the class name, `IsBroken()`). Aggregates enforce them via `CheckRule(rule)` / `CheckRules(...)`, which throw `BusinessRuleValidationException` on the first broken rule — keeping invariants inside the domain.
- **Domain services** (`IProjectDomainService`, `IEvaluationDomainService`, `ITopicPoolDomainService`, `ISemesterDomainService`, `IGroupDomainService`) hold cross-aggregate logic; implemented in Infrastructure.
- **Specifications** encapsulate reusable query predicates.

---

## 6. Domain Events

Aggregates call `RaiseDomainEvent(...)`; events are collected on the aggregate and dispatched **after** the transaction commits.

```
Aggregate.RaiseDomainEvent(e)
        │
UnitOfWork.SaveChangesAsync()
        │
DomainEventInterceptor.SavedChangesAsync()   # EF Core SaveChangesInterceptor
        │  • snapshots events from tracked IHasDomainEvents
        │  • clears them (prevents re-dispatch)
        │  • publishes each via MediatR IPublisher
        ▼
Domain event handlers (TEDF.Infrastructure/EventHandlers/<X>/)
        • send notifications (SignalR + MongoDB), emails
        • write logs, invalidate caches
        • may trigger further SaveChanges
```

Dispatch happens only on the **async** save path (the project always saves via `UnitOfWork` async) to avoid sync-over-async deadlocks.

---

## 7. Persistence

`AddPersistence` wires both stores plus the supporting services.

**SQL Server (EF Core)** — the transactional store:

- `AppDbContext` with `UseSqlServer` (`DefaultConnection`), `EnableRetryOnFailure(3)`, migrations in the Persistence assembly.
- **Interceptors** registered on the context:
  - `AuditableEntityInterceptor` — stamps `CreatedAt/UpdatedAt/CreatedBy`.
  - `SoftDeleteInterceptor` — converts deletes to an `IsDeleted` flag (no hard deletes).
  - `DomainEventInterceptor` — dispatches domain events (see §6).
- `IUnitOfWork` → `UnitOfWork`; one repository per aggregate (`IUserRepository`, `IProjectRepository`, `ITopicPoolRepository`, `IGroupRepository`, `ISemesterRepository`, `IEvaluationSubmissionRepository`, `ISupportTicketRepository`, `ITopicRegistrationRepository`, `IDepartmentRepository`, `IMajorReadRepository`, `IProjectEvaluatorAssignmentRepository`).
- **Query services** (read-optimized, bypass aggregates): student-group, evaluator, topic-pool, topic, and admin/mentor/department-head dashboards.

**MongoDB (`TEDFLogs`)** — write-heavy / append-mostly data: `IMongoClient` (singleton) + `MongoDbContext`; documents for `Conversation`, `Message`, `Notification`, `EvaluationLog`, `ProjectModificationHistory`, `QuarantinedAttachment`, `RequestLog`, `SystemAuditLog`, `UserActivityLog`, `ErrorLog`. Serializers configured once via `MongoSerializerConfiguration`; indexes created on startup via `MongoIndexConfiguration`.

**Startup init** (`InitializeDatabaseAsync`, dev only): applies pending migrations, runs `DevelopmentDataSeeder`; when the Firebase emulator is enabled also runs `LoadTestDataSeeder` + `FirebaseEmulatorSeeder`; then ensures Mongo indexes.

---

## 8. Caching

Hybrid two-tier cache, selected at startup based on config:

```
Query (ICachedQuery) ─► CachingBehavior ─► ICacheService
                                              │
                    L1: MemoryCacheService (IMemoryCache, in-process)
                                              │ miss
                    L2: RedisCacheService (StackExchange.Redis)
                                              │ miss
                                          Handler → DB, then populate L1 + L2
```

- When `CacheSettings:RedisConnectionString` is set → `HybridCacheService` (L1+L2) is the `ICacheService`, plus a `RedisCacheInvalidationListener` hosted service that keeps each instance's **L1 in sync across instances** via Redis pub/sub. With no Redis configured (dev) → memory-only fallback.
- `CachingBehavior` adds **stampede protection**: a per-key `SemaphoreSlim` ensures only one thread populates a missing key, with a double-check after acquiring the lock.
- Writes invalidate via `ICacheInvalidatingCommand.CachePrefixesToInvalidate` → `CacheInvalidationService` clears matching keys from both tiers.

---

## 9. Authentication & Authorization

**Authentication** — Firebase + JWT Bearer (`AddInfrastructure`):

- Firebase Admin SDK is initialized from `FirebaseSettings` (supports the Auth emulator).
- JWT Bearer validates Firebase ID tokens against issuer `https://securetoken.google.com/{projectId}` and audience `{projectId}` (emulator mode relaxes signing-key validation).
- `JwtBearerEvents`:
  - `OnMessageReceived` — for `/hubs/*` requests, reads the token from `?access_token=` (SignalR can't send headers).
  - `OnTokenValidated` — the Firebase `sub` is the Firebase UID, **not** the DB id. The handler resolves the user via `IUserRepository.GetByFirebaseUidAsync`, then injects extra claims: `DbUserId` (so `ICurrentUserService.UserId` returns the real Guid), `Name` (FullName), and one `Role` claim per active role.

**Authorization** — policy + resource based. Six `IAuthorizationHandler`s are registered:

| Handler / Requirement | Checks |
|-----------------------|--------|
| `PermissionAuthorizationHandler` (`PermissionRequirement`) | role/permission policy (`Permissions`, `PolicyNames`) |
| `ProjectOwnerAuthorizationHandler` (`ProjectOwnerRequirement`) | caller owns the project |
| `GroupMemberAuthorizationHandler` (`GroupMemberRequirement`) | caller is in the group |
| `GroupLeaderAuthorizationHandler` (`GroupLeaderRequirement`) | caller is the group leader |
| `MentorOfProjectAuthorizationHandler` (`MentorOfProjectRequirement`) | caller mentors the project |
| `DepartmentHeadOfDepartmentAuthorizationHandler` (`DepartmentHeadOfDepartmentRequirement`) | caller heads the department |

Policies are declared in `Authorization/Policies/` (`AuthorizationPolicies`, `Permissions`, `PolicyNames`). The Hangfire dashboard is gated by `HangfireAuthFilter` (authenticated **Admin** only).

---

## 10. Real-time, Background Jobs & Cross-cutting Services

- **SignalR** — `NotificationHub` (`/hubs/notifications`) and `ChatHub` (`/hubs/chat`); `IRealtimeNotificationService` + `INotificationService` persist to MongoDB and push to clients.
- **Hangfire** — SQL Server storage (`HangfireConnection`, falling back to `DefaultConnection`). Seven recurring jobs registered via `RecurringJobsConfiguration`: `TopicExpirationJob`, `EvaluationReminderJob`, `SemesterPhaseTransitionJob`, `DefenseScheduleReminderJob`, `MeetingReminderJob`, `GroupJoinRequestExpirationJob`, `DataCleanupJob` — plus the API-layer `QuarantineRetryJob` (every 30 min). `IBackgroundJobService` → `HangfireJobService` abstracts enqueuing.
- **Email** — `SmtpEmailService` (MailKit) + `EmailTemplateService` (HTML templates).
- **File storage** — `FirebaseStorageService` (`IFileStorageService`); `ExcelService` for spreadsheet export.
- **Health checks** — `/health` aggregates `sqlserver` + `mongodb` checks.
- **Time / identity** — `IDateTimeService` (singleton) and `ICurrentUserService` (scoped, reads claims via `IHttpContextAccessor`).

---

## 11. API Endpoints

Endpoints use the **Minimal API + `IEndpoint`** convention rather than controllers:

```csharp
public class GetActivityLogErrorDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/admin/activity-logs/errors", async (IUserActivityLogRepository repo, …) => Ok(result))
           .RequireAuthorization()
           .WithTags("Admin")
           .WithName("GetActivityLogErrorDetails")
           .Produces(200).Produces(401);
}
```

- Every endpoint implements `IEndpoint.MapEndpoint`. `MapEndpoints()` discovers all non-abstract `IEndpoint` types in the API assembly by reflection and maps them — no central route table to maintain.
- Endpoints are grouped into 18 folders under `Endpoints/` (Admin, Authentications, Chats, DepartmentHead, Departments, DirectRegistration, Evaluations, Meetings, Mentor, Notifications, Projects, Reports, Semesters, StudentGroups, Supports, TopicPools, Topics, Users).
- Handlers stay thin: resolve services from DI, send a MediatR command/query (or call a repository directly for trivial reads), and return via the `ApiResponse` helpers (`ApiResponseExtensions.Ok/Fail`). Authorization is attached per-endpoint with `RequireAuthorization(...)`.

The matching Application bounded contexts live in `Features/` (Authentications, Chats, Dashboard, Departments, DirectRegistration, Evaluations, Meetings, Mentor, Notifications, Projects, Reports, Semesters, StudentGroups, Supports, TopicPools, Topics, Users).

---

## 12. Error Handling & Response Envelope

- All responses use the `ApiResponse` / `ApiResponse<T>` envelope (`{ success, message, data?, errors? }`), produced via `ApiResponseExtensions`.
- `ExceptionHandlingMiddleware` (Infrastructure) maps exceptions to status codes and an `ApiResponse.Fail(...)` body:

  | Exception | HTTP |
  |-----------|------|
  | `EntityNotFoundException` | 404 |
  | `ValidationException` | 400 (+ field errors) |
  | `BusinessRuleValidationException` | 400 |
  | `ConcurrencyException` | 409 |
  | `UnauthorizedAccessException` | 403 |
  | `DomainException` | 400 |
  | *(unhandled)* | 500 — logged to Mongo `error_logs` + a summary in `user_activity_logs` |

- Error codes themselves are documented in [`../CLAUDE.md`](../CLAUDE.md) (§ Error Code Prefix) and [`PROJECT-RULES.md`](PROJECT-RULES.md). Validation failures are raised early by `ValidationBehavior` before the handler runs.
```
