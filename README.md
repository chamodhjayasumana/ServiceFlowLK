# ServiceFlow LK

Multi-tenant repair management SaaS for Sri Lankan device-repair shops.

This repository currently contains **Phase 1 — Repository and foundation**. See [`PROJECT_MASTER_PLAN.md`](./PROJECT_MASTER_PLAN.md) for the full product specification.

## Stack

- ASP.NET Core Web API (**net8.0** LTS)
- Angular 19 + TypeScript
- SQL Server + EF Core 8
- Clean Architecture modular monolith
- ASP.NET Core Identity (stores wired; auth flows in Phase 2)
- xUnit

## Repository layout

```text
ServiceFlowLK/
├── PROJECT_MASTER_PLAN.md
├── README.md
├── backend/                 # .NET solution
├── frontend/serviceflow-web # Angular SPA
├── docs/                    # Development docs
└── deployment/              # Migration/deploy notes
```

## Quick start

### Backend

```powershell
cd backend
dotnet restore ServiceFlow.sln
dotnet ef database update --project src/ServiceFlow.Infrastructure --startup-project src/ServiceFlow.Api
dotnet run --project src/ServiceFlow.Api
```

- Health: `/health`
- OpenAPI: `/swagger` (Development)
- Info: `/api/v1/info`

### Frontend

```powershell
cd frontend/serviceflow-web
npm install
npm start
```

## Security baselines already in place

- No production secrets committed; Development uses local Windows Auth SQL connection.
- `TenantId` is server-assigned on tenant-owned entities.
- EF global query filters + save-time cross-tenant write rejection.
- Migrations are generated for review; nothing destructive runs automatically on startup.

## Documentation

- [Local development](./docs/local-development.md)
- [Architecture](./docs/architecture.md)
- [Project master plan](./PROJECT_MASTER_PLAN.md)

## Current status

**Phase 1 complete when verified:** solution builds, tests pass, health endpoint available, Angular app scaffolds with routing.

**Next:** Phase 2 — Identity, tenant registration, permissions, and cross-tenant isolation integration tests against authenticated routes.
