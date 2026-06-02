# ADR 002 — Caching Strategy

**Status:** Accepted
**Date:** 2026-06-01
**Applies to:** `backend/` — `TEDF.Infrastructure/Caching` + the Application caching pipeline

## Context

Many TEDF reads are repeated and read-heavy: dashboards (admin/mentor/department-head), topic-pool and project lists, evaluator filter options, etc. These hit SQL Server with the same parameters frequently, and the data changes far less often than it is read.

Requirements for a cache:

- **Low latency** on hot reads — ideally no network hop for the hottest keys.
- **Shared across API instances** — the target deployment runs multiple API instances behind a load balancer (see root `ARCHITECTURE.md` §14), so a purely in-process cache would be inconsistent between instances and cold after restarts.
- **Cross-instance coherence on writes** — when one instance mutates data and invalidates the cache, the others must not keep serving stale data from their local memory.
- **Opt-in and explicit** — caching should be a deliberate per-query decision (CQRS), with explicit invalidation on the commands that change the data.
- **Frictionless in development** — a single dev instance must work without standing up Redis.

## Decision

**Use a two-tier hybrid cache — L1 in-process (`IMemoryCache`) + L2 distributed (Redis) — selected by configuration, opt-in via CQRS marker interfaces, with prefix-based invalidation and Redis pub/sub to keep each instance's L1 coherent.** When Redis is not configured, fall back to L1-only.

### Tiers (`TEDF.Infrastructure/Caching`)

- **L1 — `MemoryCacheService`** (`IMemoryCache`): fastest, per-instance, no network.
- **L2 — `RedisCacheService`** (StackExchange.Redis): shared across instances, survives instance restarts.
- **`HybridCacheService`** composes them. Read flow: **L1 → L2 → source**; on an L2 hit it **backfills L1**. Writes go to both tiers. If Redis is down it degrades gracefully to L1-only.

### Provider selection (`CacheSettings`, DI in `AddInfrastructure`)

- If `CacheSettings:RedisConnectionString` is set → register `HybridCacheService` as `ICacheService`, plus the `RedisCacheInvalidationListener` hosted service.
- If not (dev) → register `MemoryCacheService` as `ICacheService` (memory-only).

### Opt-in via CQRS markers (Application pipeline)

- A query caches by implementing **`ICachedQuery<TResponse>`** with a `CacheKey` (namespaced; supports a `{userId}` placeholder; **return `null` to skip caching**, e.g. free-text search). Per-query TTLs via `L1Expiration` (default 2 min) / `L2Expiration` (default 15 min).
- `CachingBehavior` serves hits and short-circuits the pipeline; on a miss it runs the handler and populates both tiers. It includes **stampede protection**: a per-key `SemaphoreSlim` with a double-check so only one thread repopulates a hot key.
- A command implements **`ICacheInvalidatingCommand`** exposing `CachePrefixesToInvalidate`; `CacheInvalidationBehavior` clears those prefixes from both tiers after the command succeeds.

### Cross-instance L1 coherence

This is the hard part of keeping an in-process L1 in a multi-instance deployment. On `RemoveByPrefixAsync`, `HybridCacheService` clears its own L1+L2 **and publishes** `"{InstanceId}|{prefix}"` to the Redis channel `CacheSettings:InvalidationChannel` (default `cache:invalidate`). Every instance runs `RedisCacheInvalidationListener` (a `BackgroundService`) subscribed to that channel; on receipt it **skips its own messages** (by `InstanceId`) and clears the matching prefix from its **local L1**. Result: an invalidation on one instance propagates to every instance's L1.

## Consequences

### Positive

- **Best of both tiers** — hot keys served from in-process L1 with no network cost; L2 gives a shared cache that cuts DB load across instances and survives restarts.
- **Coherent across instances** — pub/sub L1 invalidation prevents the classic "stale local memory" bug of in-process caches behind a load balancer.
- **Explicit and reviewable** — caching and invalidation are visible in the query/command types, not hidden in handlers; keys and invalidation prefixes live together.
- **Resilient & dev-friendly** — degrades to L1-only if Redis is unavailable, and runs memory-only with zero infra in development.
- **Stampede-safe** — per-key locking avoids a thundering herd when a popular key expires.

### Negative / trade-offs

- **Eventual consistency on L1** — there is a small window between a write/invalidation and other instances clearing their L1 via pub/sub. Bounded deliberately by a **short L1 TTL (2 min default)**; acceptable for dashboards/lists, not for strongly-consistent reads.
- **Operational complexity** — two tiers, a hosted listener, instance IDs, and a pub/sub channel are more moving parts than a single cache.
- **Invalidation discipline required** — `CachePrefixesToInvalidate` must stay aligned with the keys queries produce; a mismatch leaves stale entries until TTL. Prefix scans also have a cost on large key spaces.
- **Per-instance memory** — L1 duplicates hot entries in every instance's process memory.
- **No per-query L1 TTL on L2 backfill** — an L2 hit backfills L1 with the default L1 TTL (the per-query L1 TTL is only applied at original `Set` time).

## Alternatives Considered

- **In-memory only (`IMemoryCache`)** — simplest and fastest, but not shared across instances and goes stale when another instance writes. **Rejected as the production strategy; kept as the dev/no-Redis fallback.**
- **Distributed only (Redis)** — shared and coherent, but every read pays a network round-trip even for the hottest keys. **Rejected as the sole tier; it serves as L2 underneath L1.**
- **No caching** — rejected: the read-heavy dashboards and list endpoints would repeatedly hammer SQL Server with identical queries.
- **.NET `HybridCache` (BCL, .NET 9)** — conceptually similar (L1+L2+stampede protection). Not adopted now: the custom service gives explicit control over **pub/sub L1 invalidation** and **separate per-query L1/L2 TTLs**. A strong candidate to revisit (see below).

## Revisit When

- Upgrading toolchain — evaluate replacing the custom hybrid with the framework **`HybridCache`** if it covers cross-instance L1 invalidation and per-tier TTLs.
- L1 memory pressure across instances grows — consider Redis-only for large/cold key sets while keeping L1 for the hottest.
- Invalidation becomes error-prone — introduce **tag-based** invalidation instead of string prefixes.
- Staleness windows matter for a specific read — give that query a shorter TTL or bypass cache (`CacheKey => null`).
