# TEDF Backend — Project Rules

Coding conventions for the TEDF API (`backend/`). Read this before adding or changing code. For _how the system is wired_, see [`ARCHITECTURE.md`](ARCHITECTURE.md); for error codes and a quick map, see [`../CLAUDE.md`](../CLAUDE.md).

These rules describe conventions the codebase already follows — match them rather than introducing a parallel style.

---

## 1. Clean Architecture Boundaries (non-negotiable)

- **Dependencies point inward only:** `API` / `Persistence` / `Infrastructure` → `Application` → `Domain`. The **Domain references nothing external** — no EF Core, MongoDB, ASP.NET, MediatR, Firebase.
- The Domain **defines interfaces** (repository contracts, `IUnitOfWork`, domain-service interfaces); outer layers implement them.
- **Never call `AppDbContext` from the API or from Application handlers.** Go through repositories / query services / `IUnitOfWork`.
- The API must not contain business logic. Endpoints translate HTTP ⇄ MediatR; business rules live in the Domain, orchestration in Application handlers.
- When adding a feature, follow the flow: **Domain → Application (command/query + handler + validator) → Persistence/Infrastructure → API endpoint.**

## 2. Project Layout & Naming

- **File-scoped namespaces** that mirror the folder path (e.g. `namespace TEDF.Application.Features.Projects.Queries.GetProjects;`).
- One bounded context per folder, replicated across layers: `Application/Features/<Context>/`, `API/Endpoints/<Context>/`, and (if stateful) `Domain/Aggregates/<Context>Aggregate/`.
- Feature internals: `Commands/<Name>/{<Name>Command, <Name>CommandHandler, <Name>CommandValidator}`, `Queries/<Name>/{<Name>Query, <Name>QueryHandler}`, and `DTOs/`.
- Naming: `PascalCase` types/methods, `_camelCase` private fields, `I`-prefixed interfaces. Type names carry their role suffix (`…Command`, `…Query`, `…Handler`, `…Validator`, `…Dto`, `…Repository`, `…Rule`, `…Job`).
- Async methods end in `Async`.

## 3. CQRS — Commands & Queries

- Model requests as **`record`s** implementing the marker abstractions in `Application/Common/Abstractions`:
  - State changes → `ICommand` / `ICommand<TResponse>` (handler: `ICommandHandler<,>`).
  - Reads → `IQuery<TResponse>` (handler: `IQueryHandler<,>`).
- Handlers use **constructor injection**; keep one handler per request, no cross-handler calls (send another MediatR request instead).
- **Per-feature service pairing (enforced):** every feature has a `<Feature>DomainService` (writes) + `<Feature>QueryService` (reads). **A `CommandHandler` injects only its `I<Feature>DomainService`; a `QueryHandler` injects only its `I<Feature>QueryService`** (plus cross-cutting services such as `ICurrentUserService`). Handlers stay thin — read the caller, delegate to one service method. **No `IUnitOfWork`, `*Repository`, or `AppDbContext` in a handler** — those live behind the feature service.
- Validation lives in a sibling **`AbstractValidator<TCommand>`** (FluentValidation); it is auto-registered and runs in `ValidationBehavior` **before** the handler. Don't re-validate input shape inside the handler — handlers enforce _business_ preconditions, validators enforce _input_ shape.
- **Reads go through the feature's `I<Feature>QueryService`** (read-optimized DTO projections that bypass aggregates, implemented in Persistence); writes go through the feature's `I<Feature>DomainService` → aggregates (§5).

## 4. Caching (opt-in)

- A query opts into caching by implementing **`ICachedQuery<TResponse>`** and returning a `CacheKey`. **Return `null` to skip caching** — always do this when a free-text `Search` is present (avoids cache-key explosion), as in `GetProjectsQuery`.
- Build deterministic, namespaced keys (`"projects:list:sem:{…}:status:{…}:page:{…}"`); use the `{userId}` placeholder for per-user data. Set `L1Expiration` / `L2Expiration` deliberately (defaults 2 min / 15 min).
- A command that changes cached data implements **`ICacheInvalidatingCommand`** and lists `CachePrefixesToInvalidate`; the pipeline clears both L1 and L2 after success. Keep prefixes consistent with the keys queries produce.

## 5. Domain Layer

- **Encapsulate invariants in the aggregate.** Construct aggregates through **static factory methods** (e.g. `Group.Create(...)`, `GroupCode.Generate(...)`), not public constructors with setters. Keep setters private.
- **Business rules** implement `IBusinessRule` (`Message`, `IsBroken()`); enforce them inside aggregate methods via `CheckRule(rule)` / `CheckRules(...)`, which throw `BusinessRuleValidationException`. Don't scatter rule checks across handlers.
- Use **value objects** (`ProjectName`, `GroupCode`, `Email`, …) for typed, validated values; access the primitive via `.Value`.
- Aggregates signal side effects with **domain events** via `RaiseDomainEvent(...)` — never call infrastructure (email, SignalR, logging) from inside the Domain.
- **Domain services are per-feature** (`I<Feature>DomainService`): interfaces in `Domain/Services`, implementations in `Infrastructure/Services/DomainServices`. Each owns its feature's write use-cases plus any cross-aggregate logic; a command handler delegates to exactly one of its methods. (Read-only features keep a near-empty placeholder for the convention.)

## 6. Persistence & the Write Path

- **One repository per aggregate root**; load with member-aware methods when you need children (`GetWithMembersAsync`). Query services are read-only projections.
- **Persist via `IUnitOfWork.SaveChangesAsync`**, not `DbContext.SaveChanges`. The **domain service owns the unit of work** — it calls save once per use-case; handlers don't inject `IUnitOfWork`.
- **Soft delete is the default** (`SoftDeleteInterceptor`); never hard-delete. Auditing (`CreatedAt/By`, `UpdatedAt`) is applied automatically by `AuditableEntityInterceptor` — don't set these by hand.
- **Translate persistence faults into domain/Application exceptions.** Catch a unique-index `DbUpdateException` and rethrow a `ConcurrencyException` (see `StudentGroupsDomainService.CreateGroupAsync` translating `IX_Groups_Code`); don't let raw EF exceptions escape to the API.
- EF mappings go in `SqlServer/Configurations` (Fluent API). After changing the model, add a migration:
  ```powershell
  dotnet ef migrations add <Name> --project TEDF.Persistence --startup-project TEDF.API
  ```
- MongoDB is for write-heavy/append data (logs, chat, notifications). Add documents under `MongoDB/Documents` and indexes in `MongoDB/Indexes`.

## 7. Domain Event Handlers

- Handlers live in `Infrastructure/EventHandlers/<Context>/` and implement MediatR `INotificationHandler<TEvent>`.
- They run **after** `SaveChangesAsync` (dispatched by `DomainEventInterceptor`). Side effects (notifications, email, MongoDB logging, cache invalidation) belong here, not in command handlers.
- A handler that mutates state must call `SaveChangesAsync` itself. Keep handlers idempotent-friendly and isolated — one concern per handler.

## 8. Async, Cancellation & Performance

- **Async all the way.** Every I/O method is `async`/`await` and accepts a `CancellationToken`, threaded through to repositories, EF, and Mongo calls.
- Avoid N+1: batch lookups with `GetByIdsAsync` and build dictionaries/maps for projection (see `GetProjectsQueryHandler`), rather than querying inside a loop where a set-based call exists.
- Clamp pagination defensively in handlers (`page < 1 → 1`, `pageSize` out of range → default).

## 9. API Endpoints

Each domain's endpoints live in `Endpoints/<Domain>/`, organized as **one `sealed class <Domain>Endpoints : IEndpoint` per route group** — route map and handlers in a **single file** (no partial/Query/Command split). The type is **auto-discovered by reflection** (`MapEndpoints()`) and registered once — there is no central route table. A folder may hold **more than one** `IEndpoint` class if the domain spans multiple route groups (e.g. `Topics/TopicCatalogEndpoints` + `Topics/TopicPoolsEndpoints`).

**File template** (per route group):

| File                            | Contents                                                                                                                          |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `<Domain>Endpoints.cs`          | `public sealed class <Domain>Endpoints : IEndpoint` — `MapEndpoint(...)` builds the route group and maps every route inline, plus the `private static` handler methods. |
| `Requests/<Domain>Requests.cs`  | All request-body / `[AsParameters]` DTO `record`s, in the `…<Domain>.Requests` namespace (only if the domain has request bodies). |

The class builds the group and maps each route to a named handler:

```csharp
public sealed class <Domain>Endpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/<domain>").RequireAuthorization();

        group.MapGet("", GetThings).WithTags("<Domain>").WithName("GetThings").Produces(200).Produces(401);
        group.MapPost("", CreateThing).WithTags("<Domain>").WithName("CreateThing").Produces(201).Produces(400);
    }

    private static async Task<IResult> GetThings(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetThingsQuery(), ct));
}
```

Rules for the endpoint class:

- Map routes to **named `private static async Task<IResult>` handler methods** (method-group references), **not inline lambdas**. Handlers thread `CancellationToken ct` and return via the `ApiResponse` helpers (`using static ApiResponseExtensions` → `Ok` / `Created` / `NoContent`); expression-bodied one-liners are fine for thin reads.
- Keep endpoints **thin**: read the caller from `ICurrentUserService`/`HttpContext`/policies as needed, send one MediatR command/query, return. No business logic.
- Annotate each route: per-route `.RequireAuthorization(PolicyNames.…)` where it overrides the group default, `.WithTags("<Domain>")`, `.WithName(...)`, and `.Produces(...)` for documented status codes.
- Don't catch exceptions for control flow — let `ExceptionHandlingMiddleware` map them (§10).
- Request DTOs live **only** in `Requests/<Domain>Requests.cs`, never inline in the endpoint file.

> **Feature-based, not role-based.** `Endpoints/` is organised by feature; there is **no** `Mentor`, `DepartmentHead`, `Admin`, or `Departments` folder. A role action lives in the feature it belongs to (e.g. all dashboards under `Dashboard`, dept-head evaluator management under `Evaluations`, the admin + dept-head project lists under `Projects`, assign-department-head under `Users`). The folders are `ActivityLogs`, `Archives`, `Dashboard`, `DirectTopics`, `Evaluations`, `Groups`, `Majors`, `Notifications`, `Projects`, `Semesters`, `Settings`, `SupportTickets`, `Topics`, `Users`, and the frontend `src/lib`/`src/types` mirror them. See [`../../docs/API_SPEC.md`](../../docs/API_SPEC.md) for the full route list.

**Whenever you add, remove, or change an endpoint, also update the docs in the same change:**

- [`../../docs/API_SPEC.md`](../../docs/API_SPEC.md) — add/adjust the route row (method, path, auth, description).
- [`PROJECT-STATUS.md`](PROJECT-STATUS.md) — reflect the feature/endpoint status if it changed (✅ / 🚧 / 📋 / ❌).

## 10. Errors & Responses

- **All responses use the `ApiResponse` / `ApiResponse<T>` envelope** (`{ success, message, data?, errors? }`). Never return raw domain entities — map to a DTO `record`.
- Throw typed exceptions; the middleware maps them to status + envelope:
  | Throw | Result |
  |-------|--------|
  | `EntityNotFoundException` | 404 |
  | `ValidationException` | 400 (+ field errors) |
  | `BusinessRuleValidationException` | 400 |
  | `ConcurrencyException` | 409 |
  | `UnauthorizedAccessException` | 403 |
  | `DomainException` (base) | 400 |
- Pass an explicit **error `code`** to `DomainException` and reuse existing codes/rule names rather than inventing ad-hoc strings (see `../CLAUDE.md` § Error Code Prefix). `NotFoundException` is deprecated — use `EntityNotFoundException`.
- User-facing messages may be Vietnamese (matching existing copy); keep identifiers, types, and comments in English.

## 11. Auth & Current User

- Read the caller via **`ICurrentUserService`** (`UserId`, `Roles`, …); never parse claims or tokens directly in a handler. `UserId` is the **database Guid** (resolved from the Firebase UID at token validation), not the Firebase UID.
- A handler requiring a logged-in user does: `var id = _currentUser.UserId ?? throw new UnauthorizedAccessException(...);`.
- Resource ownership / role rules are enforced by the registered authorization handlers (project-owner, group-member, group-leader, mentor-of-project, department-head, permission) — attach the matching policy on the endpoint; don't reimplement these checks inline.

## 12. Dependency Injection

- Register services only in the layer's `DependencyInjection.cs` (`AddApplicationServices` / `AddPersistence` / `AddInfrastructure`). `Program.cs` composes the three.
- Default lifetime is **scoped** (repositories, query services, handlers, `ICurrentUserService`); use singleton only for stateless/config services (`IDateTimeService`, Mongo client, cache services).
- MediatR handlers/validators are assembly-scanned — no manual registration needed when you add one in the right place.

## 13. Definition of Done

Before marking a change complete:

1. `dotnet build TEDF.sln` passes with no new warnings.
2. New write paths go through an aggregate + `IUnitOfWork`; reads through a repository/query service — **no `DbContext` in API/Application**.
3. Commands have a validator; cacheable queries set a sensible `CacheKey` (null on search); mutating commands invalidate the right prefixes.
4. Schema changes ship with an EF migration.
5. Endpoints follow the one-class-per-group template (§9): a `sealed class <Domain>Endpoints : IEndpoint` mapping routes to named `private static` handlers, request DTOs in `Requests/<Domain>Requests.cs`, authorized, tagged, and returning the `ApiResponse` envelope.
6. Exceptions are typed so the middleware maps them correctly; no raw EF/Mongo exceptions escape.
7. **Docs are updated in the same change**: endpoint additions/changes are reflected in [`../../docs/API_SPEC.md`](../../docs/API_SPEC.md), and feature/endpoint status changes in [`PROJECT-STATUS.md`](PROJECT-STATUS.md).
