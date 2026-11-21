# Architecture overview

ServiceFlow LK is a **modular monolith** using **Clean Architecture**.

```text
Angular SPA
    |
 HTTPS /api/v1
    |
 ServiceFlow.Api
    |
 ServiceFlow.Application  (use cases, validators, abstractions)
    |
 ServiceFlow.Domain       (entities, rules; no infrastructure deps)
    ^
    |
 ServiceFlow.Infrastructure (EF Core, Identity, SQL Server adapters)
```

## Projects

| Project | Responsibility |
|---|---|
| `ServiceFlow.Domain` | Tenant/Customer entities, enums, tenant-owned marker |
| `ServiceFlow.Application` | Abstractions `IApplicationDbContext`, `ITenantContext`, DI registration |
| `ServiceFlow.Infrastructure` | EF Core DbContext, Identity stores, migrations, tenant context |
| `ServiceFlow.Api` | HTTP host, OpenAPI, health checks, Problem Details middleware |
| `ServiceFlow.UnitTests` | Domain/unit rules |
| `ServiceFlow.IntegrationTests` | Persistence isolation + API smoke tests |

## Multi-tenancy (shared DB / shared schema)

1. Authenticate user (Phase 2).
2. Resolve `TenantId` from trusted membership into `ITenantContext`.
3. Query filters + save-time checks enforce isolation.
4. Related-record ownership checks are added with each feature phase.

## Phase boundary

Phase 1 delivers the runnable foundation only. Business registration, login, customer APIs, and repair tickets are intentionally deferred.
