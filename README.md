# TEDF - Thesis Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.7-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![MongoDB](https://img.shields.io/badge/MongoDB-6.0+-47A248?logo=mongodb)](https://www.mongodb.com/)
[![Firebase](https://img.shields.io/badge/Firebase-Auth-FFCA28?logo=firebase)](https://firebase.google.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-512BD4)](https://learn.microsoft.com/aspnet/signalr)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.4-06B6D4?logo=tailwindcss)](https://tailwindcss.com/)

A comprehensive **Thesis Management System** built for universities, enabling end-to-end management of the thesis lifecycle — from topic proposal and group formation to evaluation and reporting. Built with **Clean Architecture**, **Domain-Driven Design**, and **CQRS** patterns.

---

## Features

### Admin
- User management (Students, Mentors, Evaluators, Department Heads) — lock/unlock, assign department head
- Department & Major configuration
- Semester & Phase management (create, activate, close)
- **Semester roster management** — import eligible students/mentors (CSV), assign major programs, bulk-delete, publish (triggers batch email + SignalR notification)
- Activity logs and audit trail (grouped, severity filter, error detail)
- **System settings & branding** — primary color, header name, logo upload, registration rules, notification toggles, maintenance mode
- Support ticket management
- **User profile** — view and edit own profile
- System reports generation (PDF / Excel) // đang phát triển

### Department Head
- Department dashboard with project overview and conflict alerts
- Assign evaluators to projects within the department
- Submit final decisions on conflicting evaluations
- View department evaluators
- **My Topics** tab — own pool topics via shared Mentor panel

### Mentor
- Propose topics to the Topic Pool (multi-step wizard modal)
- Confirm / reject student registration requests (with real-time status push to students)
- View and manage assigned student groups
- Review student-submitted topics (DirectRegistration flow)
- Edit and resubmit pool topics after evaluator feedback (FromPool flow)
- **View supervised project history**
- **User profile** — view and edit own profile with Division (Bộ môn) field

### Student
- Create groups (elect Leader, manage members via invitations/join requests; bulk approve/reject join requests)
- Browse and register topics from the Topic Pool (rich-text note, file attachment)
- Create and submit topics directly (DirectRegistration flow)
- View topic registration status in real-time (SignalR)
- **User profile** — view and edit own profile with MajorProgram (chuyên ngành hẹp) field
- Support ticket management

### Evaluator
- Review assigned project submissions
- Approve / Request Modification / Reject projects
- View evaluation history
- **View supervised project history**
- **User profile** — view and edit own profile

### Cross-Cutting
- Real-time notifications (SignalR) — click-to-navigate, per-tab routing, unread count badge
- **Account access gate** — locked or ineligible accounts blocked with dedicated pages
- **Maintenance mode** — non-admins see maintenance page while admin can still access the system
- Email notifications (MailKit — batch emails on roster publish, evaluation results)
- File upload/download (Firebase Object Storage) with malware scan (ClamAV) + quarantine
- Health checks (SQL Server, MongoDB, Redis)
- Real-time chat messaging (SignalR) // đang phát triển
- PDF report generation (QuestPDF) // đang phát triển
- Excel export (ClosedXML) // đang phát triển

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | React 19, TypeScript 5.7, Vite 6, Tailwind CSS 3, Framer Motion, React Router 7 |
| **Backend** | .NET 8, ASP.NET Core, Minimal API |
| **CQRS / Mediator** | MediatR 12.4, FluentValidation 11.11 |
| **Primary Database** | SQL Server + Entity Framework Core 8 |
| **Document Database** | MongoDB (Chat, Notifications, Audit Logs) |
| **Authentication** | Firebase Admin SDK + JWT Bearer Tokens |
| **Real-time** | ASP.NET Core SignalR (NotificationHub, ChatHub) |
| **Background Jobs** | Hangfire (9 scheduled jobs) |
| **Caching** | Hybrid: In-Memory (L1) + Redis (L2) |
| **Email** | MailKit / MimeKit (SMTP with SSL) |
| **File Storage** | Firebase Object Storage |
| **Reporting** | QuestPDF (PDF), ClosedXML (Excel) |
| **Logging** | Serilog + Azure Application Insights |
| **Health Checks** | SQL Server, MongoDB, Redis |

---

## Project Structure

```
TEDF/
├── TEDF.sln
│
├── TEDF.Domain/                 # Core Domain Layer (zero external dependencies)
│   ├── Aggregates/                   #   9 Aggregates
│   │   ├── UserAggregate/            #     User, UserRole, Email (VO)
│   │   ├── ProjectAggregate/         #     Project, Document, ProjectMentor, ProjectName (VO)
│   │   ├── GroupAggregate/           #     Group, GroupMember, GroupCode (VO)
│   │   ├── TopicPoolAggregate/       #     TopicPool, TopicRegistration, ExpirationInfo (VO)
│   │   ├── EvaluationAggregate/      #     EvaluationSubmission, ProjectEvaluatorAssignment
│   │   ├── DefenseAggregate/         #     DefenseSchedule, CouncilMember
│   │   ├── MeetingAggregate/         #     MeetingSchedule, MeetingLocation (VO)
│   │   ├── SemesterAggregate/        #     Semester, SemesterPhase, AcademicYear (VO)
│   │   └── SupportAggregate/         #     SupportTicket, TicketCode (VO)
│   ├── Enums/                        #   14 enum categories (Project, Evaluation, Group, etc.)
│   ├── Specifications/               #   Query specifications (Specification Pattern)
│   ├── Services/                     #   Domain services
│   └── Common/                       #   Base classes (AggregateRoot, Entity, ValueObject),
│                                     #   interfaces, primitives, domain events
│
├── TEDF.Application/            # Application Layer (CQRS)
│   ├── Features/                     #   Feature slices organized by bounded context
│   │   ├── Admin/                    #     Admin commands & queries
│   │   ├── DirectRegistration/       #     Student direct topic registration flow
│   │   ├── DepartmentHead/           #     Department head evaluator assignment
│   │   ├── Evaluations/              #     Evaluation submission & review
│   │   ├── Mentor/                   #     Mentor pool topic management
│   │   ├── Projects/                 #     Project CRUD, submission, documents
│   │   ├── Semesters/                #     Semester lifecycle management
│   │   ├── StudentGroups/            #     Group creation, invitations, join requests
│   │   ├── TopicPools/               #     Topic pool browsing & registration
│   │   └── Users/                    #     User management
│   ├── Common/
│   │   ├── Abstractions/             #   ICommand, IQuery, ICacheInvalidatingCommand
│   │   ├── Behaviors/                #   LoggingBehavior, ValidationBehavior
│   │   └── Interfaces/              #   Service contracts (ICurrentUserService, etc.)
│   └── DependencyInjection.cs
│
├── TEDF.Persistence/            # Data Access Layer
│   ├── SqlServer/
│   │   ├── AppDbContext.cs           #   EF Core DbContext
│   │   ├── Configurations/           #   Entity type configurations (Fluent API)
│   │   ├── Repositories/             #   Repository implementations
│   │   ├── Interceptors/             #   AuditableEntity, SoftDelete, DomainEvent
│   │   ├── QueryServices/            #   Read-optimized query services
│   │   └── ValueConverters/          #   Custom value converters
│   ├── MongoDB/
│   │   ├── Documents/                #   9 document types (Chat, Notifications, Logs)
│   │   ├── Repositories/             #   MongoDB repository implementations
│   │   ├── Indexes/                  #   Index configurations
│   │   └── Serializers/              #   Custom BSON serializers
│   ├── Migrations/                   #   EF Core migrations
│   ├── Seeds/                        #   Development & load test data seeders
│   └── DependencyInjection.cs
│
├── TEDF.Infrastructure/         # Infrastructure Layer
│   ├── Authentication/               #   Firebase Auth integration
│   ├── Authorization/                #   Policy-based & resource-based auth
│   │   ├── Policies/                 #   Custom authorization policies
│   │   └── Requirements/            #   PermissionRequirement, MentorOfProject,
│   │                                #   GroupMember, ProjectOwner, SameDepartment
│   ├── BackgroundJobs/               #   Hangfire configuration
│   │   ├── Jobs/                     #   7 scheduled jobs
│   │   └── Scheduling/              #   Job scheduler & recurring config
│   ├── Caching/                      #   Hybrid caching (In-Memory L1 + Redis L2)
│   ├── EventHandlers/                #   Domain event handlers (40+ handlers)
│   │   ├── Evaluation/               #     6 handlers (assigned, submitted, completed, etc.)
│   │   ├── Group/                    #     9 handlers (created, invited, joined, removed, etc.)
│   │   ├── Project/                  #     5 handlers (created, submitted, approved, resubmitted)
│   │   ├── Semester/                 #     3 handlers (created, phase started, upcoming)
│   │   ├── TopicPool/                #     8 handlers (created, activated, registered, etc.)
│   │   └── User/                     #     2 handlers (Firebase claims sync)
│   ├── HealthChecks/                 #   SQL, MongoDB, Redis health checks
│   ├── Logging/                      #   Serilog configuration
│   ├── Middleware/                   #   CorrelationId, RequestLogging, ExceptionHandling,
│   │                                #   PerformanceMonitoring
│   ├── RealTime/
│   │   ├── Hubs/                     #   ChatHub, NotificationHub
│   │   ├── Models/                   #   Hub data models
│   │   └── Services/                #   Real-time notification & chat services
│   ├── Services/
│   │   ├── Email/                    #   MailKit email + HTML templates
│   │   ├── FileStorage/              #   Azure Blob + Google Cloud Storage
│   │   ├── Notification/             #   Notification service (MongoDB + SignalR)
│   │   └── Reporting/               #   QuestPDF (PDF) + ClosedXML (Excel)
│   └── DependencyInjection.cs
│
├── TEDF.API/                    # Presentation Layer
│   ├── Endpoints/                    #   18 Minimal API endpoint groups
│   │   ├── Admin/                    #     Admin management endpoints
│   │   ├── Authentications/          #     Login, token refresh, Firebase auth
│   │   ├── Chats/                    #     Real-time messaging
│   │   ├── DepartmentHead/           #     Evaluator assignment, final decisions
│   │   ├── Departments/              #     Department & Major management
│   │   ├── DirectRegistration/       #     Student direct topic flow
│   │   ├── Evaluations/              #     Evaluator review & results
│   │   ├── Meetings/                 #     Meeting scheduling
│   │   ├── Mentor/                   #     Pool topic management & review
│   │   ├── Notifications/            #     Real-time notification management
│   │   ├── Projects/                 #     Project CRUD & documents
│   │   ├── Reports/                  #     PDF/Excel report generation
│   │   ├── Semesters/                #     Semester lifecycle
│   │   ├── StudentGroups/            #     Group creation & membership
│   │   ├── Supports/                 #     Support ticket management
│   │   ├── TopicPools/               #     Topic pool browsing
│   │   ├── Topics/                   #     Topic management
│   │   └── Users/                    #     User CRUD & profiles
│   ├── Extensions/                   #   Endpoint auto-registration
│   ├── Configurations/               #   Swagger / OpenAPI config
│   ├── Program.cs                    #   Application entry point (Composition Root)
│   └── appsettings.json
│
└── TEDF.client/                 # Frontend (React SPA)
    ├── src/
    │   ├── pages/                    #   30+ pages across 5 roles
    │   │   ├── admin/                #     7 pages (Dashboard, Users, Projects, Semesters, etc.)
    │   │   ├── department-head/      #     2 pages (Dashboard, AssignEvaluators)
    │   │   ├── mentor/               #     9 pages (Dashboard, Groups, Topics, TopicPools, etc.)
    │   │   ├── student/              #     6 pages (Dashboard, Group, Topics, MyTopic, etc.)
    │   │   ├── evaluator/            #     7 pages (Dashboard, Projects, Review, History, etc.)
    │   │   └── auth/                 #     Login page
    │   ├── components/
    │   │   ├── layout/               #     5 role-based layouts + sidebars
    │   │   ├── admin/                #     Admin-specific components
    │   │   └── mentor/               #     Mentor-specific components
    │   ├── contexts/                 #   React context providers (Auth, Theme)
    │   ├── lib/                      #   Service layer (API clients, utilities)
    │   ├── App.tsx                   #   Root component + routing
    │   └── main.tsx                  #   Entry point
    ├── package.json
    ├── tailwind.config.js
    ├── vite.config.ts
    └── tsconfig.json
```

---

## Prerequisites

| Software | Version | Required |
|----------|---------|----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ | Yes |
| [Node.js](https://nodejs.org/) | 18+ | Yes |
| [SQL Server](https://www.microsoft.com/sql-server) | 2019+ | Yes |
| [MongoDB](https://www.mongodb.com/try/download) | 6.0+ | Yes |
| [Redis](https://redis.io/download) | 7.0+ | Optional (L2 caching) |

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/DinhQuangKhanh/TEDF.git
cd TEDF
```

### 2. Configure Backend

Copy and update the configuration file:

```bash
cp TEDF.API/appsettings.json TEDF.API/appsettings.Development.json
```

Update `appsettings.Development.json` with your settings:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TEDF;Trusted_Connection=True;TrustServerCertificate=True;",
    "HangfireConnection": "Server=localhost;Database=TEDFHangfire;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "TEDFLogs"
  },
  "JwtSettings": {
    "Secret": "<your-jwt-secret>",
    "Issuer": "TEDF.API",
    "Audience": "TEDF.Client",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

### 3. Run Database Migrations

```powershell
cd backend
dotnet ef database update --project TEDF.Persistence --startup-project TEDF.API
```

### 4. Start the Backend

```powershell
dotnet run --project TEDF.API
```

The API will be available at:
- HTTP: `http://localhost:5141`
- HTTPS: `https://localhost:7176`
- Swagger UI: `https://localhost:7176/swagger`
- Health Check: `https://localhost:7176/health`
- Hangfire Dashboard: `https://localhost:7176/hangfire`

### 5. Start the Frontend

```powershell
cd frontend
npm install
npm run dev
```

The frontend will be available at `http://localhost:5173`.

---

## API Endpoints

The API is organized into 18 endpoint groups using the Minimal API pattern:

| Group | Description |
|-------|-------------|
| **Authentications** | Login, token refresh, Firebase auth |
| **Users** | User CRUD, role assignment, profile management |
| **Departments** | Department & Major management |
| **Semesters** | Semester lifecycle, phase management |
| **TopicPools** | Topic pool browsing, registration, approval |
| **Topics** | Topic management |
| **StudentGroups** | Group creation, member management, invitations |
| **Projects** | Project CRUD, submission, document upload |
| **DirectRegistration** | Student direct topic creation & submission to mentor |
| **Mentor** | Pool topic management, review, edit & resubmit |
| **DepartmentHead** | Evaluator assignment, final decisions |
| **Evaluations** | Evaluator assignment, review, results |
| **Meetings** | Meeting scheduling |
| **Chats** | Real-time messaging, conversations |
| **Notifications** | Real-time notification management |
| **Reports** | PDF/Excel report generation |
| **Supports** | Support ticket management |
| **Admin** | Admin-specific management endpoints |

Full API documentation available via **Swagger UI** at `/swagger`.

---

## Key Business Flows

### Topic Registration (Two Paths)

**FromPool (Mentor-initiated):**
1. Mentor proposes topic to Topic Pool
2. Student group registers for topic
3. Project created with `PendingEvaluation` status
4. Department Head assigns evaluators
5. Evaluators review (Approve / NeedsModification / Reject)
6. If NeedsModification: Mentor edits and resubmits → evaluator results reset

**DirectRegistration (Student-initiated):**
1. Student group creates topic directly
2. Student submits to mentor for review
3. Mentor approves → project enters `PendingEvaluation`
4. Department Head assigns evaluators
5. Evaluators review (Approve / NeedsModification / Reject)
6. If NeedsModification: Project returns to student for editing → student resubmits to mentor → evaluator results reset

### Evaluation Flow
- 2 evaluators assigned per project
- Each evaluator submits: Approved, NeedsModification, or Rejected
- Auto-resolution: if both agree → final result; if conflict → Department Head decides
- Resubmission increments `SubmissionNumber` and resets evaluator assignments

---

## Architecture

This project follows **Clean Architecture** with **Domain-Driven Design** principles:

```
                    ┌─────────────────────┐
                    │    TEDF.API     │  Presentation
                    │  (Minimal API, MW)   │
                    └────────┬────────────┘
                             │
                    ┌────────▼────────────┐
                    │  TEDF.App      │  Application
                    │  (CQRS, MediatR)    │
                    └────────┬────────────┘
                             │
                    ┌────────▼────────────┐
                    │  TEDF.Domain   │  Domain (Core)
                    │  (Aggregates, DDD)  │
                    └──┬─────────────┬────┘
                       │             │
          ┌────────────▼──┐   ┌──────▼───────────┐
          │  Persistence  │   │  Infrastructure  │
          │  (EF + Mongo) │   │  (Auth, SignalR)  │
          └───────────────┘   └──────────────────┘
```

> For detailed architecture diagrams (CQRS flow, domain model, database design, auth flow, SignalR, Hangfire jobs, middleware pipeline, frontend architecture, and deployment), see **[ARCHITECTURE.md](ARCHITECTURE.md)**.

---

## Branching Strategy

| Branch | Purpose |
|--------|---------|
| `master` | Production-ready code |
| `dev` | Development integration |
| `feature/*` | Feature branches |
| `<developer-name>` | Developer working branches |

**Commit Convention:**

```
[TEDF][Action][Layer]: Description

Actions: Init, Refactor, Perf, Fix, Feat, Delete
Layers:  Domain, Application, Persistence, Infrastructure, API, Client, Foundation, TEDF.client
```

Example: `[TEDF][Feat][Projects-admin]: Add project detail drawer with visibility button in list`

---

## License

This project is developed as part of a university capstone project.
