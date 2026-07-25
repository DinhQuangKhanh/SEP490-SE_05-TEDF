# Backend: TEDF API

Guidance for Claude Code when working inside `backend/`. This is the **backend**-scope file; the monorepo root `CLAUDE.md` covers global context.

Solution `TEDF.sln`, 5 projects following **Clean Architecture + DDD + CQRS** with dependencies pointing inward (`API`/`Persistence`/`Infrastructure` → `Application` → `Domain`; the Domain has **zero external dependencies**):

| Project | Role |
|---------|------|
| `TEDF.Domain` | Business core: 9 Aggregates, Value Objects, Enums, Domain Events, Specifications, business Rules, repository interfaces. |
| `TEDF.Application` | CQRS via MediatR: Commands/Queries + handlers, FluentValidation validators, pipeline behaviors, `ICacheInvalidatingCommand`. |
| `TEDF.Persistence` | EF Core `AppDbContext` (SQL Server), repositories, interceptors, MongoDB documents/repos, migrations, seeds. |
| `TEDF.Infrastructure` | Firebase Auth, authorization handlers, SignalR hubs, Hangfire jobs, hybrid caching, domain event handlers, email, file storage, health checks, middleware. |
| `TEDF.API` | Minimal API endpoints (18 groups), `Program.cs` (composition root), Swagger. |

> When adding a feature, follow the flow: Domain → Application (command/query + handler + validator) → Persistence/Infrastructure → API endpoint. Never make the Domain depend on outer layers; never call `AppDbContext` directly from the API.

## Tech Stack

| Concern | Choice |
|---------|--------|
| Runtime | .NET 8, ASP.NET Core, Minimal API |
| CQRS / mediator | MediatR 12.4 |
| Validation | FluentValidation 11.11 (via `ValidationBehavior`) |
| ORM (SQL) | Entity Framework Core 8 — SQL Server |
| Document store | MongoDB driver 3.6 — database `TEDFLogs` (chat, notifications, audit/error/activity logs) |
| Auth | Firebase Admin SDK → API-issued JWT Bearer (60 min) + refresh token (7 days) |
| Real-time | SignalR — `/hubs/chat`, `/hubs/notifications` (auth via `?access_token=`) |
| Background jobs | Hangfire 1.8 (7 recurring jobs, SQL Server storage) |
| Caching | Hybrid L1 `IMemoryCache` + L2 Redis; invalidated via `ICacheInvalidatingCommand` |
| Logging | Serilog (+ Application Insights) |
| Email / files | MailKit (SMTP) / Firebase Object Storage |

Commands (run inside `backend/`):
```powershell
dotnet build TEDF.sln
dotnet run --project TEDF.API
dotnet ef database update --project TEDF.Persistence --startup-project TEDF.API
```
API: HTTP `:5141`, HTTPS `:7176`. Swagger `/swagger`, Health `/health`, Hangfire `/hangfire`.
> No test project exists — do not assume `dotnet test` works.

## Documentation

### Must Read

- [`docs/PROJECT-RULES.md`](docs/PROJECT-RULES.md) — coding conventions and architectural rules to follow before writing code.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — backend architecture details.

### Reference

- [`docs/PROJECT-STATUS.md`](docs/PROJECT-STATUS.md) — current feature status (some features are still in development).
- [`docs/decisions/`](docs/decisions) — ADRs: `001-orm-choice.md`, `002-caching-strategy.md`.
- Root [`../ARCHITECTURE.md`](../ARCHITECTURE.md) — full diagrams (CQRS pipeline, domain model, DB, auth, SignalR, Hangfire, middleware, caching, domain events).
- Root [`../docs/`](../docs) — `API_SPEC.md`, `DATABASE.md`, `DEPLOYMENT.md`.

> The `docs/*.md` files above are placeholders being filled in — verify content before relying on them.

## Quick Reference

### Feature Location

Each bounded context exists as a sibling folder in three layers — `Application/Features/<X>`, `API/Endpoints/<X>`, and (where stateful) a `Domain/Aggregates/<X>Aggregate`.

| Concern | Location |
|---------|----------|
| Composition root / DI wiring | `TEDF.API/Program.cs`; per-layer `DependencyInjection.cs` |
| API endpoints (18 groups) | `TEDF.API/Endpoints/<Feature>/` (auto-registered) |
| Commands/Queries + handlers + validators | `TEDF.Application/Features/<Feature>/` |
| Pipeline behaviors (Logging, Validation) | `TEDF.Application/Common/Behaviors/` |
| Service & repository interfaces | `TEDF.Domain/Common/Interfaces/`, per-aggregate `Repositories/` |
| Aggregates, entities, value objects | `TEDF.Domain/Aggregates/<X>Aggregate/` |
| Business rules | `TEDF.Domain/Aggregates/<X>Aggregate/Rules/` |
| Domain events | `TEDF.Domain/Aggregates/<X>Aggregate/Events/` |
| Domain event handlers | `TEDF.Infrastructure/EventHandlers/<X>/` |
| EF config / repositories / interceptors | `TEDF.Persistence/SqlServer/` |
| MongoDB documents / repositories | `TEDF.Persistence/MongoDB/` |
| Migrations & seeds | `TEDF.Persistence/Migrations/`, `TEDF.Persistence/Seeds/` |
| Auth / authorization handlers | `TEDF.Infrastructure/Authentication/`, `TEDF.Infrastructure/Authorization/` |
| SignalR hubs | `TEDF.Infrastructure/RealTime/Hubs/` |
| Hangfire jobs | `TEDF.Infrastructure/BackgroundJobs/` |
| Caching | `TEDF.Infrastructure/Caching/` |
| Middleware (exception, correlation, logging, perf) | `TEDF.Infrastructure/Middleware/` |

**`Application/Features/`** (15): `Archives`, `Authentications`, `Dashboard`, `DirectTopics`, `EvaluationChecklists`, `Evaluations`, `Notifications`, `Projects`, `Semesters`, `Settings`, `StudentGroups`, `Supports`, `TopicPools`, `Topics`, `Users`.

**`API/Endpoints/`** (15): `ActivityLogs`, `Archives`, `Authentications`, `Dashboard`, `DirectTopics`, `Evaluations`, `Groups`, `Majors`, `Notifications`, `Projects`, `Semesters`, `Settings`, `SupportTickets`, `Topics`, `Users`.

> The two lists do **not** line up one-to-one — `EvaluationChecklists` commands/queries are exposed by `Endpoints/Evaluations/ChecklistEndpoints.cs`, `ActivityLogs`/`Majors` endpoints read repositories directly with no Application feature, and `StudentGroups` is served by `Endpoints/Groups/`. Check both trees before assuming a feature is missing.

### Error Code Prefix

Errors carry a string `Code`. `ExceptionHandlingMiddleware` (`TEDF.Infrastructure/Middleware/`) maps the exception type → HTTP status and returns `ApiResponse.Fail(message)`. Two code styles are used:

**Domain exceptions** (`TEDF.Domain/Common/Exceptions/`) — `SCREAMING_SNAKE_CASE`:

| Exception | `Code` | HTTP |
|-----------|--------|------|
| `EntityNotFoundException` | `ENTITY_NOT_FOUND` | 404 |
| `ValidationException` | `VALIDATION_ERROR` | 400 |
| `BusinessRuleValidationException` | broken rule's `Code` (defaults to the rule class name, e.g. `EmailMustBeFptDomainRule`) or `BUSINESS_RULE_VIOLATION` | 400 |
| `ConcurrencyException` | `CONCURRENCY_CONFLICT` | 409 |
| `DomainException` (base/default) | `DOMAIN_ERROR` | 400 |
| `UnauthorizedAccessException` | — | 403 |
| *(any unhandled)* | — | 500 (logged to Mongo `error_logs`; the request itself is recorded in `activity_logs` with `Status = Failure` and the same `CorrelationId`) |

> `NotFoundException` is **deprecated** — use `EntityNotFoundException`.

**Infrastructure exceptions** (`TEDF.Infrastructure/Common/`) — dotted `Area.Error`:

| Exception | `Code` |
|-----------|--------|
| `EmailException` | `Email.Error` |
| `FileStorageException` | `FileStorage.Error` |
| `ExternalServiceException` | `ExternalService.Error` |
| `InfrastructureException` (base) | caller-supplied code |

**Business rule codes:** an `IBusinessRule.Code` defaults to `GetType().Name` (PascalCase rule class name). When throwing a new domain error, pass an explicit `code` to `DomainException` and reuse an existing rule/exception code rather than inventing ad-hoc strings.
