# CLAUDE.md

Guidance for Claude Code when working in this repo. This is the **global**-scope file (monorepo root). Each sub-project may have its own `CLAUDE.md` (`backend/CLAUDE.md`, `frontend/CLAUDE.md`).

## Overview

**TEDF** is a **Thesis Management System** for universities: it manages the full topic lifecycle from proposal, group formation, and registration through evaluation and reporting. Built with **Clean Architecture + DDD + CQRS**.

The monorepo has two parts:

| Directory   | Contents                          | Main stack                                                  |
| ----------- | --------------------------------- | ----------------------------------------------------------- |
| `backend/`  | API + business logic (`TEDF.sln`) | .NET 8, ASP.NET Core Minimal API, MediatR, EF Core, MongoDB |
| `frontend/` | Admin SPA                         | React 19, TypeScript 5.7, Vite 6, Tailwind CSS 3            |

Five user roles: **Admin, DepartmentHead, Mentor, Student, Evaluator**.

## Backend structure (Clean Architecture)

`backend/` contains 5 projects, with dependencies pointing inward (outer layers depend on inner layers, never the reverse):

- **`TEDF.Domain`** — Business core, **zero external dependencies**. 9 Aggregates, Value Objects, Enums, Domain Events, Specifications, repository interfaces (contracts).
- **`TEDF.Application`** — CQRS via MediatR. Features split by bounded context; Commands/Queries, Validators (FluentValidation), Pipeline Behaviors (Logging, Validation), `ICacheInvalidatingCommand`.
- **`TEDF.Persistence`** — Data access. `AppDbContext` (EF Core/SQL Server), Repositories, Interceptors (Auditable, SoftDelete, DomainEvent), MongoDB documents/repositories, Migrations, Seeds.
- **`TEDF.Infrastructure`** — Firebase Auth, Authorization handlers, SignalR Hubs, Hangfire jobs, Hybrid Caching (Redis), Domain Event Handlers, Email, FileStorage, HealthChecks.
- **`TEDF.API`** — Presentation. Minimal API endpoints (18 groups), middleware pipeline, Swagger, `Program.cs` (composition root).

> **Core principle:** The Domain has no external dependencies; it defines interfaces that outer layers implement. When adding a feature, follow the flow: Domain → Application (command/query + handler + validator) → Persistence/Infrastructure → API endpoint.

## Frontend structure

`frontend/src/` — `pages/` (per role), `components/` (including `layout/` for the 5 role layouts), `contexts/` (Auth, Theme), `lib/` (typed API clients), `hooks/`, `config/`, `types/`. Entry: `main.tsx` → `App.tsx` (React Router 7). Each role has its own `*_CONTEXT.md` description file in `src/`.

## Common commands

**Backend** (run inside `backend/`):

```powershell
dotnet build TEDF.sln
dotnet run --project TEDF.API
dotnet ef database update --project TEDF.Persistence --startup-project TEDF.API
```

API: HTTP `:5141`, HTTPS `:7176`. Swagger `/swagger`, Health `/health`, Hangfire `/hangfire`.

**Frontend** (run inside `frontend/`):

```powershell
npm install
npm run dev      # Vite dev server -> http://localhost:5173
npm run build    # tsc -b && vite build
npm run lint     # ESLint
```

> There are currently no test projects in the repo — do not assume `dotnet test` or any test runner exists.

## Infrastructure & services

- **SQL Server** (EF Core) — transactional data (Users, Projects, Groups, Semesters, Evaluations...).
- **MongoDB** (`TEDFLogs`) — chat, notifications, audit/activity logs (write-heavy). Three logging collections: `activity_logs` (request-scoped user-action audit via `IActivityLogRepository`), `error_logs`, and `system_audit_logs` (per-entity audit trail via `ISystemAuditLogWriteService`, powering the project audit-log page). Four legacy collections were consolidated into the first two on 2026-07-19.
- **Firebase Auth + JWT Bearer** — login via Firebase, the API issues a JWT (60 min) + refresh token (7 days). Five authorization handlers (Permission, ProjectOwner, GroupMember, MentorOfProject, SameDepartment).
- **SignalR** — 2 hubs: `/hubs/chat`, `/hubs/notifications` (auth via `?access_token=`).
- **Hangfire** — 7 recurring jobs (stored in SQL Server).
- **Caching** — Hybrid L1 (IMemoryCache) + L2 (Redis); invalidated via `ICacheInvalidatingCommand`.
- **Domain Events** — aggregates raise events, dispatched **after** `SaveChangesAsync` via `DomainEventInterceptor` → handlers in Infrastructure.

## Key business flow: Topic registration (two paths)

- **FromPool** (mentor-initiated): Mentor proposes a topic → student group registers → `PendingEvaluation` → DepartmentHead assigns evaluators → review. If `NeedsModification`: mentor edits & resubmits → evaluator results reset.
- **DirectRegistration** (student-initiated): Student group creates a topic → submits to mentor → mentor approves → `PendingEvaluation` → assign evaluators → review. If `NeedsModification`: returns to student to edit → resubmit to mentor → evaluator results reset.

Each project has 2 evaluators; if both agree → final result, if they conflict → DepartmentHead decides. A resubmission increments `SubmissionNumber` and resets the assignments.

## Commit convention

```
[TEDF][Action][Layer]: Description
```

- **Action:** Init, Refactor, Perf, Fix, Feat, Delete
- **Layer:** Domain, Application, Persistence, Infrastructure, API, Client, Foundation

Example: `[TEDF][Feat][Projects-admin]: Add project detail drawer with visibility button in list`

Branches: `main` (production), `dev` (integration), `feature/*`, `<developer-name>`.

## Reference docs

- [`README.md`](README.md) — feature overview, tech stack, run instructions.
- [`ARCHITECTURE.md`](ARCHITECTURE.md) — detailed diagrams (CQRS, domain model, DB, auth, SignalR, Hangfire, middleware, frontend, caching, deployment).
- [`docs/`](docs) — `API_SPEC.md`, `DATABASE.md`, `DEPLOYMENT.md`.
- [`backend/docs/`](backend/docs) — `PROJECT-RULES.md`, `PROJECT-STATUS.md`, `decisions/`.
- [`frontend/docs/`](frontend/docs) — `PROJECT-RULES.md`, `PROJECT-STATUS.md`, `decisions/`.

## Notes for Claude

- **Windows + PowerShell**: use PowerShell syntax (`$env:VAR`, `$null`, backtick for line continuation), not bash.
- The structure diagrams in `README.md`/`ARCHITECTURE.md` show the projects at the root, but in **reality** they live under `backend/` and `frontend/` — follow the actual layout when locating files.
- Respect Clean Architecture boundaries: do not make the Domain depend on outer layers; do not call DbContext directly from the API.
- Some features are still `// đang phát triển` (in development) — Reports PDF/Excel, real-time Chat — verify status before assuming they are complete. Email (SMTP) is **implemented**.
- **User schema (post-refactor):** Student-specific data lives in `Students` table (`StudentCode`, `ProgramId→Programs`, `ComboId→Combos`); lecturer-specific data in `Lecturers` (`EmployeeCode`, `AcademicTitle`). Both share PK with `Users`. Roles stored in normalized `Roles` lookup table; `UserRoles.RoleId int FK` replaces the old string `RoleName`. Use `User.Student?.StudentCode` / `User.Lecturer?.EmployeeCode` — never access flat columns that no longer exist on `User`.
