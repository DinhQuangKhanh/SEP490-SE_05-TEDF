# TEDF - Architecture Documentation

Detailed architecture diagrams for the TEDF Thesis Management System.

---

## Table of Contents

1. [Clean Architecture Layers](#1-clean-architecture-layers)
2. [High-Level System Architecture](#2-high-level-system-architecture)
3. [CQRS + MediatR Pipeline](#3-cqrs--mediatr-pipeline)
4. [Domain Model (Aggregates)](#4-domain-model-aggregates)
5. [Database Architecture (Polyglot Persistence)](#5-database-architecture-polyglot-persistence)
6. [Authentication & Authorization](#6-authentication--authorization)
7. [Real-time Communication (SignalR)](#7-real-time-communication-signalr)
8. [Background Jobs (Hangfire)](#8-background-jobs-hangfire)
9. [Request Pipeline (Middleware)](#9-request-pipeline-middleware)
10. [Frontend Architecture](#10-frontend-architecture)
11. [Caching Architecture](#11-caching-architecture)
12. [Domain Event System](#12-domain-event-system)
13. [Topic Registration Flows](#13-topic-registration-flows)
14. [Deployment Architecture](#14-deployment-architecture)

---

## 1. Clean Architecture Layers

The project follows Clean Architecture with strict dependency inversion. Outer layers depend on inner layers, never the reverse.

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│   ┌──────────────────────────────────────────────────────────────┐   │
│   │                                                              │   │
│   │   ┌──────────────────────────────────────────────────────┐   │   │
│   │   │                                                      │   │   │
│   │   │   ┌──────────────────────────────────────────────┐   │   │   │
│   │   │   │                                              │   │   │   │
│   │   │   │             TEDF.Domain                  │   │   │   │
│   │   │   │                                              │   │   │   │
│   │   │   │   - Aggregates (9)     - Domain Events       │   │   │   │
│   │   │   │   - Entities           - Business Rules      │   │   │   │
│   │   │   │   - Value Objects      - Specifications      │   │   │   │
│   │   │   │   - Enums (14 cats)    - Domain Services     │   │   │   │
│   │   │   │   - Repository Interfaces (Contracts)        │   │   │   │
│   │   │   │                                              │   │   │   │
│   │   │   └──────────────────────────────────────────────┘   │   │   │
│   │   │                                                      │   │   │
│   │   │                TEDF.Application                  │   │   │
│   │   │                                                      │   │   │
│   │   │   - Commands & Queries (CQRS)   - DTOs               │   │   │
│   │   │   - Pipeline Behaviors          - Validators         │   │   │
│   │   │   - Service Interfaces          - Event Handlers     │   │   │
│   │   │   - ICacheInvalidatingCommand                        │   │   │
│   │   │                                                      │   │   │
│   │   └──────────────────────────────────────────────────────┘   │   │
│   │                                                              │   │
│   │   TEDF.Persistence          TEDF.Infrastructure    │   │
│   │                                                              │   │
│   │   - AppDbContext (EF Core)       - Firebase Authentication   │   │
│   │   - MongoDbContext               - Authorization Handlers    │   │
│   │   - Repository Implementations   - SignalR Hubs              │   │
│   │   - Migrations & Seeds           - Hangfire Background Jobs  │   │
│   │   - Interceptors (3)             - Email / File Storage      │   │
│   │   - Query Services               - Hybrid Caching (Redis)   │   │
│   │                                  - Domain Event Handlers     │   │
│   │                                  - Health Checks             │   │
│   └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                        TEDF.API                                  │
│                                                                      │
│   - Minimal API Endpoints (18 groups)  - Middleware Pipeline         │
│   - Swagger / OpenAPI Configuration    - CORS Configuration         │
│   - Program.cs (Composition Root)      - Error Handling              │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘

           Dependency Direction:  API ──► Application ──► Domain
                                  Persistence ──────────► Domain
                                  Infrastructure ───────► Domain
```

**Key Principle:** The Domain layer has ZERO external dependencies. It defines interfaces (contracts) that outer layers implement.

---

## 2. High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT (Browser)                               │
│                                                                             │
│         React 19  +  TypeScript 5.7  +  Vite 6  +  Tailwind CSS 3         │
│         React Router 7  +  Framer Motion  +  React PDF                     │
│                                                                             │
└──────────────┬────────────────────────────────────┬─────────────────────────┘
               │                                    │
               │ HTTP/REST (JSON)                   │ WebSocket
               │ Port: 5141 / 7176                  │ (SignalR)
               │                                    │
┌──────────────▼────────────────────────────────────▼─────────────────────────┐
│                           TEDF.API (.NET 8)                            │
│                                                                             │
│  ┌─────────────┐  ┌──────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  Endpoints   │  │ Swagger  │  │ Middleware   │  │ SignalR Hubs        │  │
│  │  (18 groups) │  │ /swagger │  │  Pipeline    │  │ /hubs/chat          │  │
│  │              │  │          │  │              │  │ /hubs/notifications  │  │
│  └──────┬───────┘  └──────────┘  └──────────────┘  └─────────────────────┘  │
│         │                                                                   │
├─────────▼───────────────────────────────────────────────────────────────────┤
│                       TEDF.Application                                 │
│                                                                             │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────┐  │
│  │    MediatR        │  │  FluentValidation │  │  Pipeline Behaviors     │  │
│  │  Commands/Queries │  │  Request Validators│  │  Logging + Validation  │  │
│  └────────┬─────────┘  └──────────────────┘  └──────────────────────────┘  │
│           │                                                                 │
├───────────▼─────────────────────────────────────────────────────────────────┤
│                         TEDF.Domain                                    │
│                                                                             │
│      9 Aggregates  │  50+ Domain Events  │  25+ Business Rules             │
│      14 Enum Cats  │  Specifications     │  Domain Services                │
│                                                                             │
├─────────────────────────────────┬───────────────────────────────────────────┤
│      TEDF.Persistence      │         TEDF.Infrastructure          │
└───────┬──────────┬──────────────┘──────┬────────┬────────┬────────┬────────┘
        │          │                     │        │        │        │
        ▼          ▼                     ▼        ▼        ▼        ▼
┌──────────┐ ┌──────────┐      ┌──────────┐ ┌────────┐ ┌──────┐ ┌────────┐
│SQL Server│ │ MongoDB  │      │ Firebase │ │Hangfire│ │Redis │ │  SMTP  │
│ (EF Core │ │          │      │   Auth   │ │  Jobs  │ │Cache │ │ Email  │
│  8.0.23) │ │ Driver   │      │  Admin   │ │ (SQL)  │ │(L2)  │ │MailKit │
│          │ │  3.6.0   │      │  SDK     │ │ 1.8.22 │ │      │ │        │
└──────────┘ └──────────┘      └──────────┘ └────────┘ └──────┘ └────────┘
                                     │
                                     ▼
                              ┌────────────┐
                              │Azure Blob  │
                              │  Storage   │
                              │+ GCS       │
                              └────────────┘
```

---

## 3. CQRS + MediatR Pipeline

Commands (write operations) and Queries (read operations) follow separate paths through the MediatR pipeline.

```
                         HTTP Request (POST/PUT/DELETE)
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │   Minimal API        │
                         │   Endpoint           │
                         │                      │
                         │   var result = await  │
                         │   mediator.Send(cmd); │
                         └──────────┬────────────┘
                                    │
                      ┌─────────────▼─────────────┐
                      │     MediatR Pipeline       │
                      │                            │
                      │  ┌──────────────────────┐  │
                      │  │ ValidationBehavior   │  │    Runs FluentValidation
                      │  │ (IPipelineBehavior)  │  │    validators. Throws
                      │  │                      │  │    ValidationException
                      │  └──────────┬───────────┘  │    if invalid.
                      │             │              │
                      │  ┌──────────▼───────────┐  │
                      │  │  LoggingBehavior     │  │    Logs request name,
                      │  │  (IPipelineBehavior) │  │    user info, and
                      │  │                      │  │    execution time.
                      │  └──────────┬───────────┘  │
                      │             │              │
                      └─────────────┼──────────────┘
                                    │
                     ┌──────────────┴──────────────┐
                     │                             │
            ┌────────▼────────┐          ┌─────────▼────────┐
            │  Command Path   │          │   Query Path     │
            │                 │          │                  │
            │ ICommandHandler │          │  IQueryHandler   │
            │                 │          │                  │
            │ - Validate rules│          │ - Build spec     │
            │ - Modify domain │          │ - Execute query  │
            │ - Raise events  │          │ - Map to DTO     │
            │ - Save via UoW  │          │ - Return result  │
            └────────┬────────┘          └─────────┬────────┘
                     │                             │
            ┌────────▼────────┐          ┌─────────▼────────┐
            │   Repository    │          │  Query Service   │
            │  + Unit of Work │          │  (Read-optimized)│
            └────────┬────────┘          └─────────┬────────┘
                     │                             │
                     └──────────────┬──────────────┘
                                    │
                           ┌────────▼────────┐
                           │    Database     │
                           │  (SQL Server)   │
                           └─────────────────┘
```

### Cache Invalidation in CQRS

Commands that implement `ICacheInvalidatingCommand` specify cache key prefixes. After successful execution, the caching infrastructure automatically invalidates matching cache entries:

```
  Command ──► Handler ──► SaveChanges ──► Cache Invalidation
                                              │
                                    Clears L1 (Memory) + L2 (Redis)
                                    based on CachePrefixesToInvalidate
```

---

## 4. Domain Model (Aggregates)

9 Aggregate Roots with their entities, value objects, and relationships.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DOMAIN MODEL                                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────┐         ┌──────────────────────┐                          │
│  │  UserAggregate   │         │  SemesterAggregate    │                          │
│  │─────────────────│         │──────────────────────│                          │
│  │ User (Root)      │         │ Semester (Root)       │                          │
│  │ ├── UserRole     │         │ ├── SemesterPhase     │                          │
│  │ └── Email (VO)   │         │ ├── AcademicYear (VO) │                          │
│  │                  │         │ ├── DateRange (VO)    │                          │
│  │ Rules:           │         │ └── SemesterCode (VO) │                          │
│  │  EmailMustBeFpt  │         │                      │                          │
│  └────────┬─────────┘         │ Rules:               │                          │
│           │                   │  PhasesMustNotOverlap │                          │
│           │ creates           │  DatesMustBeValid     │                          │
│           │                   └──────────────────────┘                          │
│  ┌────────▼─────────┐                                                           │
│  │  GroupAggregate   │◄─────────────── registers ──────────────────┐            │
│  │─────────────────│                                              │            │
│  │ Group (Root)      │         ┌──────────────────────┐            │            │
│  │ ├── GroupMember   │         │ TopicPoolAggregate    │            │            │
│  │ └── GroupCode(VO) │         │──────────────────────│            │            │
│  │                  │         │ TopicPool (Root)      │            │            │
│  │ Rules:           │         │ ├── TopicRegistration ├────────────┘            │
│  │  MaxMembers      │         │ ├── ExpirationInfo(VO)│                          │
│  │  MustHaveLeader  │         │ └── TopicCode (VO)   │                          │
│  │  NoMultipleGroups│         │                      │                          │
│  └────────┬─────────┘         │ Rules:               │                          │
│           │                   │  MustBeAvailable      │                          │
│           │ owns              │  MaxActiveTopics      │                          │
│           │                   └──────────────────────┘                          │
│  ┌────────▼─────────────────────────────────────────────────────────┐           │
│  │  ProjectAggregate                                                │           │
│  │─────────────────────────────────────────────────────────────────│           │
│  │ Project (Root)                                                   │           │
│  │ ├── Document              ├── ProjectMentor                      │           │
│  │ ├── ProjectCode (VO)      ├── ProjectName (VO)                   │           │
│  │ └── TechnologyStack (VO)                                        │           │
│  │                                                                  │           │
│  │ SourceType: FromPool (0) | DirectRegistration (1)                │           │
│  │ Status: Draft → PendingMentorReview → PendingEvaluation →       │           │
│  │         NeedsModification → Approved → InProgress → Completed   │           │
│  │                                                                  │           │
│  │ Events: Created, Submitted, MentorApproved, Resubmitted,        │           │
│  │         Approved, Rejected, Completed, DocumentUploaded          │           │
│  │ Rules:  OnlySubmitWhenDraft, MaxMentors, SourceTypeGuards       │           │
│  └──────┬──────────────────────┬─────────────────────┬──────────────┘           │
│         │                      │                     │                          │
│         │ evaluated by         │ defended at          │ has meetings            │
│         │                      │                     │                          │
│  ┌──────▼───────────┐  ┌──────▼───────────┐  ┌──────▼───────────┐             │
│  │ EvaluationAgg.   │  │  DefenseAgg.     │  │  MeetingAgg.     │             │
│  │─────────────────│  │─────────────────│  │─────────────────│             │
│  │ EvaluationSub-   │  │ DefenseSchedule  │  │ MeetingSchedule  │             │
│  │   mission (Root) │  │   (Root)         │  │   (Root)         │             │
│  │ ├── ProjectEval- │  │ ├── CouncilMbr   │  │ └── MeetingLoc-  │             │
│  │ │   uatorAssign  │  │ └── DefenseLoc-  │  │     ation (VO)   │             │
│  │ ├── ProjectSnap- │  │     ation (VO)   │  │                  │             │
│  │ │   shot (VO)    │  │                  │  │ Events:          │             │
│  │ └── Submission-  │  │ Rules:           │  │  Requested       │             │
│  │     Number (VO)  │  │  MustHaveChairman│  │  Approved        │             │
│  │                  │  │  MustHaveCouncil │  │  Completed       │             │
│  │ Rules:           │  │                  │  │  Cancelled       │             │
│  │  2 Evaluators    │  └──────────────────┘  └──────────────────┘             │
│  │  CannotEvalOwn   │                                                          │
│  │  MaxResubmission │                                                          │
│  │  ResetEvaluation │                                                          │
│  └──────────────────┘                                                          │
│                                                                                 │
│  ┌──────────────────┐                                                          │
│  │  SupportAgg.     │     (Standalone: Department, Major, SystemConfiguration, │
│  │─────────────────│                  Report, ProjectArchive)                  │
│  │ SupportTicket    │                                                          │
│  │   (Root)         │                                                          │
│  │ └── TicketCode   │                                                          │
│  │     (VO)         │                                                          │
│  └──────────────────┘                                                          │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Key Domain Enums

| Category | Enums |
|----------|-------|
| **Project** | ProjectStatus (9 states), ProjectSourceType (FromPool, DirectRegistration) |
| **Evaluation** | EvaluationResult (Pending, Approved, NeedsModification, Rejected), EvaluationStatus |
| **Group** | GroupStatus, GroupMemberRole, InvitationStatus, JoinRequestStatus |
| **Semester** | SemesterStatus, PhaseType |
| **User** | UserRole (Admin, Mentor, Student, Evaluator, DepartmentHead) |
| **TopicPool** | TopicPoolStatus, RegistrationStatus |
| **Notification** | NotificationCategory, NotificationType |

---

## 5. Database Architecture (Polyglot Persistence)

SQL Server handles transactional data with ACID guarantees. MongoDB handles high-throughput logs and real-time messaging data.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        SQL SERVER (Entity Framework Core)                    │
│                     Transactional Data with ACID Guarantees                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────────────┐  │
│  │    Users     │ │   Groups     │ │  Projects    │ │    TopicPools      │  │
│  │─────────────│ │──────────────│ │──────────────│ │────────────────────│  │
│  │ Id          │ │ Id           │ │ Id           │ │ Id                 │  │
│  │ Email       │ │ GroupCode    │ │ ProjectCode  │ │ TopicCode          │  │
│  │ FullName    │ │ Status       │ │ SourceType   │ │ Title              │  │
│  │ Status      │ │ CreatedAt    │ │ Status       │ │ Status             │  │
│  │             │ │              │ │ TechStack    │ │ ExpirationDate     │  │
│  │ ┌─────────┐│ │ ┌──────────┐ │ │ ┌──────────┐ │ │ ┌────────────────┐ │  │
│  │ │UserRoles││ │ │GroupMmbrs│ │ │ │Documents │ │ │ │TopicRegistrtns│ │  │
│  │ └─────────┘│ │ └──────────┘ │ │ │ProjectMtr│ │ │ └────────────────┘ │  │
│  └─────────────┘ └──────────────┘ │ └──────────┘ │ └────────────────────┘  │
│                                   └──────────────┘                          │
│  ┌───────────────┐ ┌───────────────┐ ┌──────────────┐ ┌──────────────────┐ │
│  │  Semesters     │ │  Evaluations  │ │  Defenses    │ │   Meetings       │ │
│  │───────────────│ │───────────────│ │──────────────│ │──────────────────│ │
│  │ Id            │ │ Id            │ │ Id           │ │ Id               │ │
│  │ SemesterCode  │ │ Status        │ │ ScheduleDate │ │ ScheduleDate     │ │
│  │ Status        │ │ SubmissionNum │ │ Status       │ │ Status           │ │
│  │               │ │               │ │              │ │ Location         │ │
│  │ ┌───────────┐ │ │ ┌───────────┐ │ │ ┌──────────┐│ │                  │ │
│  │ │SemPhases  │ │ │ │ProjEvalAs│ │ │ │CouncilMbr││ └──────────────────┘ │
│  │ └───────────┘ │ │ └───────────┘ │ │ └──────────┘│                     │
│  └───────────────┘ └───────────────┘ └──────────────┘ ┌──────────────────┐ │
│                                                        │ SupportTickets   │ │
│  Standalone: Department, Major, SystemConfiguration,   │──────────────────│ │
│              Report, ProjectArchive                    │ Id, Code, Status │ │
│                                                        └──────────────────┘ │
│  Interceptors:                                                              │
│    - AuditableEntityInterceptor (CreatedAt, UpdatedAt, CreatedBy)           │
│    - SoftDeleteInterceptor (IsDeleted flag, no hard deletes)                │
│    - DomainEventInterceptor (publishes events AFTER SaveChanges)            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                       MONGODB (Document Store)                              │
│              High-Throughput Logs, Chat & Real-time Data                    │
│              Database: "TEDFLogs"                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐                        │
│  │  Conversations       │  │  Messages            │                        │
│  │──────────────────────│  │──────────────────────│                        │
│  │ _id, Participants,   │  │ _id, ConversationId, │   Real-time Chat       │
│  │ Type, CreatedAt      │  │ SenderId, Content,   │   Messaging Data       │
│  │                      │  │ Type, SentAt         │                        │
│  └──────────────────────┘  └──────────────────────┘                        │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐                        │
│  │  Notifications       │  │  EvaluationLogs      │                        │
│  │──────────────────────│  │──────────────────────│                        │
│  │ _id, UserId, Title,  │  │ _id, EvaluationId,   │   Event Tracking       │
│  │ Message, Category,   │  │ Action, Timestamp,   │   & Audit Trail        │
│  │ IsRead, CreatedAt    │  │ PerformedBy, Details │                        │
│  └──────────────────────┘  └──────────────────────┘                        │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐  │
│  │  ProjectModHistory   │  │  UserActivityLogs    │  │ SystemAuditLogs  │  │
│  │──────────────────────│  │──────────────────────│  │──────────────────│  │
│  │ _id, ProjectId,      │  │ _id, UserId, Action, │  │ _id, Action,     │  │
│  │ FieldChanged,        │  │ Timestamp, IpAddress,│  │ EntityType,      │  │
│  │ OldValue, NewValue,  │  │ UserAgent, Details   │  │ Timestamp,       │  │
│  │ ModifiedBy, At       │  │                      │  │ Details          │  │
│  └──────────────────────┘  └──────────────────────┘  └──────────────────┘  │
│                                                                             │
│  ┌──────────────────────┐  ┌──────────────────────┐                        │
│  │  RequestLogs         │  │  QuarantinedAttach.  │                        │
│  │──────────────────────│  │──────────────────────│                        │
│  │ _id, Method, Path,   │  │ _id, FileName,       │   Request logging      │
│  │ StatusCode, Duration,│  │ Reason, UploadedBy,  │   & file quarantine   │
│  │ UserId, Timestamp    │  │ QuarantinedAt        │                        │
│  └──────────────────────┘  └──────────────────────┘                        │
│                                                                             │
│  Why MongoDB?                                                               │
│    - High write throughput for logs (no joins needed)                       │
│    - Flexible schema for varying log structures                            │
│    - TTL indexes for automatic log cleanup                                 │
│    - Optimized for append-heavy, read-light workloads                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Authentication & Authorization

```
                            ┌──────────────────┐
                            │   Client App     │
                            │   (React)        │
                            └────────┬─────────┘
                                     │
                          1. Login with credentials
                                     │
                            ┌────────▼─────────┐
                            │  Firebase Auth   │
                            │  (Google Cloud)  │
                            └────────┬─────────┘
                                     │
                          2. Firebase ID Token
                                     │
                            ┌────────▼─────────┐
                            │  TEDF API   │
                            │  /auth/login     │
                            └────────┬─────────┘
                                     │
                   3. Validate Firebase token via Admin SDK
                      Generate JWT (60 min) + Refresh Token (7 days)
                                     │
                            ┌────────▼─────────┐
                            │  JWT Bearer      │
                            │  Token Response  │
                            │  { token,        │
                            │    refreshToken } │
                            └────────┬─────────┘
                                     │
                     ┌───────────────┴────────────────┐
                     │    Subsequent API Requests      │
                     │    Authorization: Bearer <jwt>  │
                     └───────────────┬────────────────┘
                                     │
                            ┌────────▼──────────────────────────────────┐
                            │        AUTHORIZATION PIPELINE             │
                            │                                           │
                            │  ┌─────────────────────────────────────┐  │
                            │  │ 1. PermissionAuthorizationHandler   │  │
                            │  │    Check role-based permissions     │  │
                            │  ├─────────────────────────────────────┤  │
                            │  │ 2. ProjectOwnerAuthorizationHandler │  │
                            │  │    Verify user owns the project     │  │
                            │  ├─────────────────────────────────────┤  │
                            │  │ 3. GroupMemberAuthorizationHandler  │  │
                            │  │    Verify user is in the group      │  │
                            │  ├─────────────────────────────────────┤  │
                            │  │ 4. MentorOfProjectAuthHandler      │  │
                            │  │    Verify user mentors the project  │  │
                            │  ├─────────────────────────────────────┤  │
                            │  │ 5. SameDepartmentAuthHandler       │  │
                            │  │    Verify user is in same dept      │  │
                            │  └─────────────────────────────────────┘  │
                            │                                           │
                            │  Requirements:                            │
                            │   - PermissionRequirement                 │
                            │   - GroupMemberRequirement                │
                            │   - MentorOfProjectRequirement            │
                            │   - ProjectOwnerRequirement               │
                            │   - SameDepartmentRequirement             │
                            └───────────────────────────────────────────┘

    Roles: Admin  |  Mentor  |  Student  |  Evaluator  |  DepartmentHead
```

---

## 7. Real-time Communication (SignalR)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        SIGNALR ARCHITECTURE                              │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Client A                         Client B                              │
│   (Student)                        (Mentor)                              │
│   ┌──────────┐                     ┌──────────┐                          │
│   │ React    │                     │ React    │                          │
│   │ SignalR  │                     │ SignalR  │                          │
│   │ Client   │                     │ Client   │                          │
│   └────┬─────┘                     └────┬─────┘                          │
│        │                                │                                │
│        │ ws://host/hubs/chat            │ ws://host/hubs/chat            │
│        │ ?access_token=<jwt>            │ ?access_token=<jwt>            │
│        │                                │                                │
│   ┌────▼────────────────────────────────▼────┐                           │
│   │            SignalR Hub Server             │                           │
│   │                                           │                           │
│   │  ┌─────────────────────────────────────┐  │                           │
│   │  │         ChatHub                     │  │                           │
│   │  │         /hubs/chat                  │  │                           │
│   │  │                                     │  │                           │
│   │  │  - SendMessage(convId, content)     │  │                           │
│   │  │  - JoinConversation(convId)         │  │                           │
│   │  │  - LeaveConversation(convId)        │  │                           │
│   │  │  - OnConnected / OnDisconnected     │  │                           │
│   │  │                                     │  │                           │
│   │  │  Groups:                            │  │                           │
│   │  │    "conversation_{id}" per chat     │  │                           │
│   │  │    "user_{id}" per user             │  │                           │
│   │  └─────────────────────────────────────┘  │                           │
│   │                                           │                           │
│   │  ┌─────────────────────────────────────┐  │                           │
│   │  │      NotificationHub               │  │                           │
│   │  │      /hubs/notifications            │  │                           │
│   │  │                                     │  │                           │
│   │  │  - OnConnected (join user group)    │  │                           │
│   │  │  - ReceiveNotification (client)     │  │                           │
│   │  │                                     │  │                           │
│   │  │  Groups:                            │  │                           │
│   │  │    "user_{id}" per user             │  │                           │
│   │  │    "project_{id}" per project       │  │                           │
│   │  └─────────────────────────────────────┘  │                           │
│   │                                           │                           │
│   └───────────────────────────────────────────┘                           │
│                     │                                                     │
│                     ▼                                                     │
│             ┌───────────────┐                                             │
│             │   MongoDB     │  Messages & Notifications                   │
│             │   Storage     │  persisted for history                      │
│             └───────────────┘                                             │
│                                                                          │
│   Auth: JWT token passed via query string (?access_token=...)            │
│   Transport: WebSocket (primary), Server-Sent Events, Long Polling       │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Background Jobs (Hangfire)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     HANGFIRE BACKGROUND JOBS                             │
│                                                                          │
│   Dashboard: /hangfire                                                   │
│   Storage:   SQL Server (HangfireConnection)                            │
│   Service:   HangfireJobService : IBackgroundJobService                 │
│                                                                          │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌────────────────────────────────────────────────────────────────┐     │
│   │  RECURRING JOBS (Scheduled by RecurringJobsConfiguration)     │     │
│   ├────────────────────────────────────────────────────────────────┤     │
│   │                                                                │     │
│   │  1. TopicExpirationJob                                        │     │
│   │     Schedule: Daily                                            │     │
│   │     Action:   Check topic pool expiration dates,               │     │
│   │               auto-close expired topics                        │     │
│   │                                                                │     │
│   │  2. EvaluationReminderJob                                     │     │
│   │     Schedule: Daily                                            │     │
│   │     Action:   Send email reminders to evaluators               │     │
│   │               with pending reviews                             │     │
│   │                                                                │     │
│   │  3. SemesterPhaseTransitionJob                                 │     │
│   │     Schedule: Daily                                            │     │
│   │     Action:   Auto-transition semester phases based            │     │
│   │               on configured date ranges                        │     │
│   │                                                                │     │
│   │  4. DefenseScheduleReminderJob                                │     │
│   │     Schedule: Daily                                            │     │
│   │     Action:   Notify council members and students              │     │
│   │               of upcoming defense sessions                     │     │
│   │                                                                │     │
│   │  5. MeetingReminderJob                                        │     │
│   │     Schedule: Hourly                                           │     │
│   │     Action:   Send reminders for upcoming meetings             │     │
│   │               to mentors and students                          │     │
│   │                                                                │     │
│   │  6. GroupJoinRequestExpirationJob                              │     │
│   │     Schedule: Daily                                            │     │
│   │     Action:   Auto-expire pending group join requests          │     │
│   │               that have not been responded to                  │     │
│   │                                                                │     │
│   │  7. DataCleanupJob                                            │     │
│   │     Schedule: Weekly                                           │     │
│   │     Action:   Clean up expired tokens, old logs,               │     │
│   │               temporary files                                  │     │
│   │                                                                │     │
│   └────────────────────────────────────────────────────────────────┘     │
│                                                                          │
│   Job Flow:                                                              │
│                                                                          │
│   ┌──────────┐    ┌──────────────┐    ┌────────────┐    ┌────────────┐  │
│   │ Scheduler │───►│ Hangfire SQL  │───►│ Job Runner │───►│ Services   │  │
│   │ (Cron)    │    │   Storage    │    │ (Worker)   │    │ (Email,    │  │
│   │           │    │              │    │            │    │  SignalR,  │  │
│   └──────────┘    └──────────────┘    └────────────┘    │  DB)       │  │
│                                                         └────────────┘  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 9. Request Pipeline (Middleware)

The order of middleware is critical. Each request flows through this pipeline from top to bottom.

```
                        Incoming HTTP Request
                                │
                                ▼
                ┌───────────────────────────────┐
                │     HTTPS Redirection         │    Redirect HTTP to HTTPS
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │     CORS Middleware           │    Validate AllowedOrigins
                │     (Cors.AllowedOrigins)     │    Allow credentials
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │     Authentication            │    Validate JWT Bearer token
                │     (JWT Bearer)              │    Set HttpContext.User
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │     Authorization             │    Check policies & requirements
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │     CorrelationIdMiddleware   │    Add X-Correlation-Id header
                │                               │    for request tracing
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │     RequestLoggingMiddleware  │    Log request details
                │     (Serilog)                 │    with structured logging
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │   ExceptionHandlingMiddleware │    Catch unhandled exceptions
                │                               │    Return ProblemDetails JSON
                └───────────────┬───────────────┘
                                │
                                ▼
                ┌───────────────────────────────┐
                │  PerformanceMonitoringMW      │    Track response times
                │  (Application Insights)       │    Flag slow requests
                └───────────────┬───────────────┘
                                │
                     ┌──────────┴──────────┐
                     │                     │
                     ▼                     ▼
        ┌────────────────────┐  ┌─────────────────────┐
        │  /health           │  │  /hubs/*             │
        │  Health Checks     │  │  SignalR Hubs        │
        │  (SQL, Mongo,      │  │  - /hubs/chat        │
        │   Redis)           │  │  - /hubs/notif.      │
        └────────────────────┘  └─────────────────────┘
                     │
                     ▼
        ┌────────────────────────────────┐
        │  Minimal API Endpoints         │
        │  (18 groups auto-registered)   │
        │                                │
        │  /api/auth/*                   │
        │  /api/users/*                  │
        │  /api/projects/*               │
        │  /api/groups/*                 │
        │  /api/topics/*                 │
        │  /api/evaluations/*            │
        │  /api/meetings/*               │
        │  /api/semesters/*              │
        │  /api/departments/*            │
        │  /api/notifications/*          │
        │  /api/chats/*                  │
        │  /api/reports/*                │
        │  /api/supports/*               │
        │  /api/mentor/*                 │
        │  /api/direct-registration/*    │
        │  /api/department-head/*        │
        │  /api/admin/*                  │
        └────────────────────────────────┘
                     │
                     ▼
              HTTP Response
```

---

## 10. Frontend Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     FRONTEND ARCHITECTURE                                │
│                     React 19 + TypeScript 5.7 + Vite 6                  │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Entry Point: main.tsx ──► App.tsx (Router)                            │
│                                                                          │
│   ┌──────────────────────────────────────────────────────────────────┐   │
│   │                     ROUTING (React Router 7)                     │   │
│   ├──────────────────────────────────────────────────────────────────┤   │
│   │                                                                  │   │
│   │   /login ──────────────────────────► LoginPage                  │   │
│   │   /maintenance ────────────────────► MaintenancePage            │   │
│   │                                                                  │   │
│   │   /admin/* ──► AdminLayout                                      │   │
│   │   │  /admin             ──► DashboardPage                       │   │
│   │   │  /admin/users       ──► UsersPage                           │   │
│   │   │  /admin/projects    ──► ProjectsPage                        │   │
│   │   │  /admin/semesters   ──► SemestersPage                       │   │
│   │   │  /admin/settings    ──► SettingsPage                        │   │
│   │   │  /admin/support     ──► SupportPage                         │   │
│   │   │  /admin/activity    ──► ActivityLogsPage                    │   │
│   │                                                                  │   │
│   │   /department-head/* ──► DepartmentHeadLayout                   │   │
│   │   │  /department-head              ──► DashboardPage            │   │
│   │   │  /department-head/evaluators   ──► AssignEvaluatorsPage     │   │
│   │                                                                  │   │
│   │   /mentor/* ──► MentorLayout                                    │   │
│   │   │  /mentor            ──► MentorDashboardPage                 │   │
│   │   │  /mentor/groups     ──► MentorGroupsPage                   │   │
│   │   │  /mentor/groups/:id ──► MentorTopicDetailPage              │   │
│   │   │  /mentor/topics     ──► MentorTopicsPage                   │   │
│   │   │  /mentor/topics/:id ──► MentorFeedbackPage                 │   │
│   │   │  /mentor/topic-pools      ──► TopicPoolsPage               │   │
│   │   │  /mentor/topic-pools/:id  ──► TopicPoolDetailPage          │   │
│   │   │  /mentor/schedule   ──► MentorSchedulePage                 │   │
│   │   │  /mentor/support    ──► MentorSupportPage                  │   │
│   │                                                                  │   │
│   │   /student/* ──► StudentLayout                                  │   │
│   │   │  /student           ──► StudentDashboardPage               │   │
│   │   │  /student/group     ──► StudentGroupPage                   │   │
│   │   │  /student/my-topic  ──► StudentMyTopicPage                 │   │
│   │   │  /student/topics    ──► StudentTopicsPage                  │   │
│   │   │  /student/schedule  ──► StudentSchedulePage                │   │
│   │   │  /student/support   ──► StudentSupportPage                 │   │
│   │                                                                  │   │
│   │   /evaluator/* ──► EvaluatorLayout                              │   │
│   │   │  /evaluator             ──► EvaluatorDashboardPage         │   │
│   │   │  /evaluator/projects    ──► EvaluatorProjectsPage          │   │
│   │   │  /evaluator/schedule    ──► EvaluatorSchedulePage          │   │
│   │   │  /evaluator/history     ──► EvaluatorHistoryPage           │   │
│   │   │  /evaluator/review/:id  ──► EvaluatorReviewPage            │   │
│   │   │  /evaluator/similarity  ──► EvaluatorSimilarityPage        │   │
│   │   │  /evaluator/support     ──► EvaluatorSupportPage           │   │
│   │                                                                  │   │
│   └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│   ┌──────────────────────────────────────────────────────────────────┐   │
│   │                     COMPONENT HIERARCHY                          │   │
│   ├──────────────────────────────────────────────────────────────────┤   │
│   │                                                                  │   │
│   │   App.tsx                                                        │   │
│   │   ├── ProtectedRoute (auth guard)                               │   │
│   │   ├── Layouts (role-specific)                                   │   │
│   │   │   ├── AdminLayout         ──► Header + Sidebar + Content    │   │
│   │   │   ├── DeptHeadLayout      ──► Header + Sidebar + Content    │   │
│   │   │   ├── MentorLayout        ──► Header + Sidebar + Content    │   │
│   │   │   ├── StudentLayout       ──► Header + Sidebar + Content    │   │
│   │   │   └── EvaluatorLayout     ──► Header + Sidebar + Content    │   │
│   │   ├── Header                                                    │   │
│   │   │   └── NotificationDropdown (SignalR real-time)              │   │
│   │   └── Modals                                                    │   │
│   │       ├── TopicDetailModal (edit/resubmit for pool topics)      │   │
│   │       └── RegisterTopicModal                                    │   │
│   │                                                                  │   │
│   └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│   ┌──────────────────────────────────────────────────────────────────┐   │
│   │                     TECH DETAILS                                 │   │
│   ├──────────────────────────────────────────────────────────────────┤   │
│   │   Build:       Vite 6 (dev server + production bundler)         │   │
│   │   Styling:     Tailwind CSS 3 + PostCSS                         │   │
│   │   Animation:   Framer Motion                                    │   │
│   │   PDF Viewer:  react-pdf + pdfjs-dist                           │   │
│   │   Theming:     CSS custom properties (--color-primary)          │   │
│   │                stored in localStorage                           │   │
│   │   Linting:     ESLint 9 + typescript-eslint                     │   │
│   │   API Layer:   Service files in src/lib/ (typed API clients)    │   │
│   └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 11. Caching Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     HYBRID CACHING (L1 + L2)                             │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Query Request                                                          │
│       │                                                                  │
│       ▼                                                                  │
│   ┌─────────────────┐     HIT     ┌─────────────────┐                   │
│   │  L1: In-Memory  │ ──────────► │  Return Cached   │                   │
│   │  (IMemoryCache) │             │  Response        │                   │
│   └────────┬────────┘             └─────────────────┘                   │
│            │ MISS                                                        │
│            ▼                                                             │
│   ┌─────────────────┐     HIT     ┌─────────────────┐                   │
│   │  L2: Redis      │ ──────────► │  Populate L1     │                   │
│   │  (IDistributed  │             │  Return Response │                   │
│   │   Cache)        │             └─────────────────┘                   │
│   └────────┬────────┘                                                    │
│            │ MISS                                                        │
│            ▼                                                             │
│   ┌─────────────────┐             ┌─────────────────┐                   │
│   │  Database       │ ──────────► │  Populate L1+L2  │                   │
│   │  (SQL Server)   │             │  Return Response │                   │
│   └─────────────────┘             └─────────────────┘                   │
│                                                                          │
│   Cache Invalidation (on Commands):                                      │
│                                                                          │
│   Command implements ICacheInvalidatingCommand                           │
│       │                                                                  │
│       ▼                                                                  │
│   CachePrefixesToInvalidate: ["projects", "evaluations", ...]           │
│       │                                                                  │
│       ▼                                                                  │
│   Clear matching keys from both L1 and L2                               │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 12. Domain Event System

Domain events are raised by aggregates and dispatched AFTER `SaveChangesAsync` completes, via `DomainEventInterceptor`.

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     DOMAIN EVENT FLOW                                    │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   1. Aggregate raises event                                              │
│      project.Raise(new ProjectResubmittedEvent(project.Id));            │
│                                                                          │
│   2. DomainEventInterceptor.SavedChangesAsync()                         │
│      - Collects all events from tracked entities                        │
│      - Clears events from entities                                      │
│      - Publishes via MediatR (INotification)                            │
│      - Events fire AFTER the transaction commits                        │
│                                                                          │
│   3. Event Handlers (in Infrastructure layer)                            │
│      - Can modify entities (requires explicit SaveChangesAsync)         │
│      - Send notifications (SignalR + MongoDB)                           │
│      - Send emails                                                      │
│      - Log to MongoDB                                                   │
│      - Invalidate caches                                                │
│                                                                          │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Event Handlers by Domain:                                              │
│                                                                          │
│   Evaluation (6):                                                        │
│     EvaluationAssigned, EvaluatorAssignedToProject,                     │
│     EvaluatorSubmittedResult, EvaluationCompleted,                      │
│     EvaluationCancelled, DepartmentHeadFinalDecision                    │
│                                                                          │
│   Group (9):                                                             │
│     GroupCreated, MemberInvited, InvitationAccepted/Rejected,           │
│     JoinRequested, JoinRequestApproved/Rejected,                        │
│     MemberAdded, MemberRemoved                                          │
│                                                                          │
│   Project (5):                                                           │
│     ProjectCreated, ProjectSubmitted, ProjectApproved,                  │
│     ProjectRejected, ProjectResubmitted                                 │
│                                                                          │
│   Semester (3):                                                          │
│     SemesterCreated, PhaseStarted, PhaseUpcoming                        │
│                                                                          │
│   TopicPool (8):                                                         │
│     TopicPoolCreated, TopicPoolActivated, TopicPoolSuspended,           │
│     TopicRegistrationRequested, TopicRegistrationConfirmed,             │
│     TopicRegistrationRejected, TopicRegistrationCancelled,              │
│     PoolTopicExpired                                                    │
│                                                                          │
│   User (2):                                                              │
│     SyncFirebaseClaimsOnUserCreated, SyncFirebaseClaimsOnRoleAssigned   │
│                                                                          │
│   DirectRegistration (1):                                                │
│     ProjectMentorApproved (resets evaluator results on resubmission)    │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 13. Topic Registration Flows

### FromPool Flow (Mentor-initiated)

```
  Mentor                    System                    Evaluators
    │                         │                          │
    │  1. Propose Topic       │                          │
    │  (CreateFromPool)       │                          │
    │ ───────────────────────►│                          │
    │                         │  Status: PendingEvaluation
    │                         │                          │
    │                         │  2. Dept Head assigns    │
    │                         │     evaluators           │
    │                         │ ────────────────────────►│
    │                         │                          │
    │                         │  3. Evaluators review    │
    │                         │◄────────────────────────│
    │                         │                          │
    │       ┌─────────────────┼──────────────────────┐  │
    │       │ If NeedsModification                    │  │
    │       │                 │                        │  │
    │  4. Edit topic          │                        │  │
    │  (MentorUpdatePoolTopic)│                        │  │
    │ ───────────────────────►│                        │  │
    │                         │                        │  │
    │  5. Resubmit            │                        │  │
    │  (MentorResubmitPool)   │                        │  │
    │ ───────────────────────►│                        │  │
    │                         │  Reset evaluator        │  │
    │                         │  assignments            │  │
    │                         │ ────────────────────────►│
    │                         │  Notify: re-evaluate     │
    │       └─────────────────┼──────────────────────┘  │
    │                         │                          │
```

### DirectRegistration Flow (Student-initiated)

```
  Student          Mentor              System              Evaluators
    │                │                   │                     │
    │ 1. Create      │                   │                     │
    │ (CreateDirect) │                   │                     │
    │───────────────►│                   │                     │
    │                │  Status: Draft    │                     │
    │                │                   │                     │
    │ 2. Submit to   │                   │                     │
    │    mentor      │                   │                     │
    │───────────────►│                   │                     │
    │                │ Status: PendingMentorReview             │
    │                │                   │                     │
    │                │ 3. Approve        │                     │
    │                │──────────────────►│                     │
    │                │                   │ Status: PendingEval │
    │                │                   │                     │
    │                │                   │ 4. Dept Head assigns│
    │                │                   │────────────────────►│
    │                │                   │                     │
    │                │                   │ 5. Evaluators review│
    │                │                   │◄────────────────────│
    │                │                   │                     │
    │  ┌─────────────┼───────────────────┼─────────────────┐  │
    │  │ If NeedsModification            │                  │  │
    │  │             │                   │                  │  │
    │ 6. Edit topic  │                   │                  │  │
    │ (UpdateDirect) │                   │                  │  │
    │───────────────►│                   │                  │  │
    │                │                   │                  │  │
    │ 7. Resubmit    │                   │                  │  │
    │    to mentor   │                   │                  │  │
    │───────────────►│                   │                  │  │
    │                │ 8. Approve again  │                  │  │
    │                │──────────────────►│                  │  │
    │                │                   │ Reset evaluator  │  │
    │                │                   │ assignments      │  │
    │                │                   │────────────────────►│
    │  └─────────────┼───────────────────┼─────────────────┘  │
    │                │                   │                     │
```

---

## 14. Deployment Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                  CURRENT: LOCAL DEVELOPMENT SETUP                        │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Developer Machine                                                      │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │                                                                 │   │
│   │  ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐  │   │
│   │  │   Vite Dev   │    │  .NET 8 API  │    │  SQL Server      │  │   │
│   │  │   Server     │    │  Kestrel     │    │  (LocalDB /      │  │   │
│   │  │              │    │              │    │   Express)        │  │   │
│   │  │  :5173       │───►│  :5141 HTTP  │───►│  :1433           │  │   │
│   │  │  (React)     │    │  :7176 HTTPS │    │                  │  │   │
│   │  └──────────────┘    │              │    └──────────────────┘  │   │
│   │                      │  /swagger    │                          │   │
│   │                      │  /health     │    ┌──────────────────┐  │   │
│   │                      │  /hubs/*     │───►│  MongoDB         │  │   │
│   │                      │  /hangfire   │    │  Community       │  │   │
│   │                      └──────────────┘    │  :27017          │  │   │
│   │                                          └──────────────────┘  │   │
│   │                                                                 │   │
│   │                      External Services (Cloud):                │   │
│   │                      ┌──────────────────┐                      │   │
│   │                      │  Firebase Auth   │                      │   │
│   │                      │  Azure Blob      │                      │   │
│   │                      │  Google Cloud    │                      │   │
│   │                      │  SMTP Server     │                      │   │
│   │                      └──────────────────┘                      │   │
│   │                                                                 │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘


┌──────────────────────────────────────────────────────────────────────────┐
│                  TARGET: CLOUD DEPLOYMENT                                │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌──────────────┐         ┌──────────────────────────────────────────┐  │
│   │   CDN        │         │        Cloud Platform (Azure / GCP)     │  │
│   │  (Static     │         │                                          │  │
│   │   Assets)    │         │  ┌──────────────┐  ┌──────────────────┐  │  │
│   │              │         │  │  App Service  │  │  Azure SQL /     │  │  │
│   │  React SPA   │────────►│  │  / Cloud Run  │  │  Cloud SQL       │  │  │
│   │  Build       │         │  │              │──►│                  │  │  │
│   └──────────────┘         │  │  .NET 8 API  │  └──────────────────┘  │  │
│                            │  │              │                        │  │
│                            │  │              │  ┌──────────────────┐  │  │
│                            │  │              │──►│  MongoDB Atlas   │  │  │
│                            │  └──────┬───────┘  └──────────────────┘  │  │
│                            │         │                                │  │
│                            │         │          ┌──────────────────┐  │  │
│                            │         ├─────────►│  Azure Redis     │  │  │
│                            │         │          └──────────────────┘  │  │
│                            │         │                                │  │
│                            │         │          ┌──────────────────┐  │  │
│                            │         ├─────────►│  Azure Blob      │  │  │
│                            │         │          │  Storage         │  │  │
│                            │         │          └──────────────────┘  │  │
│                            │         │                                │  │
│                            │         │          ┌──────────────────┐  │  │
│                            │         └─────────►│  Application     │  │  │
│                            │                    │  Insights        │  │  │
│                            │                    └──────────────────┘  │  │
│                            │                                          │  │
│                            │  ┌──────────────────────────────────┐    │  │
│                            │  │  Firebase Auth (managed by GCP)  │    │  │
│                            │  └──────────────────────────────────┘    │  │
│                            │                                          │  │
│                            └──────────────────────────────────────────┘  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Summary

| Aspect | Details |
|--------|---------|
| **Architecture** | Clean Architecture + DDD |
| **Backend** | .NET 8, ASP.NET Core, Minimal API |
| **Frontend** | React 19, TypeScript, Vite, Tailwind |
| **Databases** | SQL Server (OLTP) + MongoDB (Logs/Chat) |
| **Auth** | Firebase + JWT + 5 Authorization Handlers |
| **Real-time** | SignalR (2 Hubs: Chat, Notifications) |
| **Background** | Hangfire (7 Scheduled Jobs) |
| **Caching** | Hybrid L1 (Memory) + L2 (Redis) |
| **Aggregates** | 9 DDD Aggregates, 50+ Domain Events, 33+ Event Handlers |
| **API** | 18 Endpoint Groups (Minimal API) |
| **Frontend Pages** | 30+ Pages across 5 Role Layouts |
| **Roles** | Admin, DepartmentHead, Mentor, Student, Evaluator |
