# ADR 001 — ORM Choice

**Status:** Accepted
**Date:** 2026-06-01
**Applies to:** `backend/` — `TEDF.Persistence` (data access layer)

## Context

TEDF follows Clean Architecture + DDD: the Domain models 9 aggregates with rich value objects (`ProjectName`, `GroupCode`, `Email`, `AcademicYear`, …) and must stay free of any persistence dependency. The persistence layer has to:

- Map DDD aggregates — including value objects and private setters — to a **relational** store without leaking persistence concerns into the Domain.
- Support the patterns the rest of the codebase already relies on: a per-aggregate **repository** abstraction, a **Unit of Work** (one transaction per command), **soft delete**, **auditing**, and **domain-event dispatch** tied to `SaveChanges`.
- Provide first-class **migrations** for schema evolution (the project ships EF migrations and seeds).
- Coexist with **MongoDB**, which holds write-heavy/append data (chat, notifications, audit/error/activity logs) — see ADR 002 context and the polyglot-persistence design.

We needed to choose how the relational store (SQL Server) is accessed.

## Decision

**Use Entity Framework Core 8 (SQL Server provider) as the ORM for the relational store. Access MongoDB through the official `MongoDB.Driver` directly — no ORM for the document store.**

Concretely, in `TEDF.Persistence`:

1. **EF Core 8.0.23** (`Microsoft.EntityFrameworkCore` + `.SqlServer` + `.Design`/`.Tools`) backs `AppDbContext`, configured with `EnableRetryOnFailure(3)` and migrations kept in the Persistence assembly.

2. **Fluent API configurations** (`SqlServer/Configurations`) map every aggregate — no data annotations on Domain types, so the Domain stays persistence-ignorant.

3. **Value objects are mapped via EF Core value converters** (`ValueConverters/` — `GroupCodeConverter`, `ProjectNameConverter`, `AcademicYearConverter`, `ProjectSnapshotConverter`, etc.), so DDD types persist as primitive columns transparently.

4. **EF Core `SaveChangesInterceptor`s** implement cross-cutting persistence behavior: `AuditableEntityInterceptor` (timestamps/actor), `SoftDeleteInterceptor` (no hard deletes), and `DomainEventInterceptor` (dispatch domain events after commit). This is the main reason a change-tracking ORM is a strong fit — these behaviors hook the unit of work centrally.

5. **Repositories + `IUnitOfWork`** wrap the `DbContext`; the Domain depends only on the interfaces. Read-heavy paths use **query services** that issue LINQ projections for read-optimized DTOs.

6. **MongoDB** uses `MongoDB.Driver` 3.6.0 directly with hand-written documents/repositories — appropriate for schemaless, append-mostly data where an ORM adds no value.

No micro-ORM (Dapper) or hand-rolled ADO.NET is used for application data.

## Consequences

### Positive

- **Clean DDD mapping** — value converters + Fluent API keep the Domain free of persistence attributes while still mapping value objects and encapsulated state (private setters, backing fields).
- **Centralized cross-cutting behavior** — change tracking lets soft delete, auditing, and domain-event dispatch be implemented once as interceptors, instead of being repeated in every repository.
- **Productive schema evolution** — `dotnet ef migrations` gives versioned, reviewable schema changes and a scripted dev seed/init path.
- **LINQ + provider safety** — compile-time-checked queries and parameterized SQL by default; less boilerplate than raw ADO.NET.
- **Unit of Work out of the box** — `DbContext` is a natural UoW, matching the "one command = one transaction = one `SaveChangesAsync`" rule.

### Negative / trade-offs

- **Abstraction cost / surprises** — EF can generate inefficient SQL or trigger N+1 if used naively; mitigated by query services, batched `GetByIdsAsync` + projection, and reviewing generated SQL on hot paths.
- **Change-tracking overhead** — heavier than a thin mapper for large read sets; mitigated by `AsNoTracking`-style read paths in query services.
- **Learning curve** — value converters, owned types, and interceptor semantics require EF familiarity to use correctly.
- **Two data-access styles in one layer** — EF Core for SQL, raw driver for Mongo. Accepted deliberately (polyglot persistence); contributors must know which store a given repository targets.

## Alternatives Considered

- **Dapper (micro-ORM)** — rejected as the primary mapper: fast and explicit, but no change tracking, so soft delete / auditing / domain-event dispatch and Unit of Work would all be hand-rolled, and DDD value-object mapping would be manual. It remains a viable *escape hatch* for a specific hot query if EF ever proves too slow there.
- **Raw ADO.NET / stored procedures** — rejected: maximal control but maximal boilerplate; loses migrations, LINQ, and the interceptor model that the architecture leans on.
- **NHibernate** — rejected: capable and DDD-friendly, but a smaller modern .NET ecosystem, heavier configuration, and weaker first-party tooling than EF Core on .NET 8.
- **An ORM/ODM for MongoDB** — rejected: the official driver is sufficient for our log/chat documents; an extra abstraction would add cost without benefit for schemaless append data.

## Revisit When

- A specific read path is measurably too slow under EF — introduce **Dapper** (or `FromSqlRaw`) for that query only, keeping EF as the default.
- The relational provider changes (e.g. PostgreSQL) — EF Core's provider model makes this mostly a configuration/migration concern.
- Domain mapping outgrows value converters (complex owned graphs) — revisit owned-entity vs. JSON-column strategies.
