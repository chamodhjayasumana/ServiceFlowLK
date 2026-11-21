# Local development

## Prerequisites

| Tool | Verified in this environment |
|---|---|
| .NET SDK | 9.0.x (projects target **net8.0** LTS) |
| Node.js | 22.x |
| Angular CLI | use `npx @angular/cli@19` (global CLI may be outdated) |
| SQL Server | SQL Server 2022 Developer (Windows Auth) |

Docker is optional and not required for local development when SQL Server is installed.

## Backend

```powershell
cd backend
dotnet restore ServiceFlow.sln
dotnet build ServiceFlow.sln
dotnet test ServiceFlow.sln
```

### Connection string

`appsettings.json` keeps `DefaultConnection` empty on purpose.

`appsettings.Development.json` uses Windows Trusted Connection against local SQL Server:

```text
Server=localhost;Database=ServiceFlowLK;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Override without editing files:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=ServiceFlowLK;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Or use user secrets from `ServiceFlow.Api`:

```powershell
cd src/ServiceFlow.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ServiceFlowLK;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

### Apply migrations (manual, reviewed)

Never enable automatic destructive database operations.

```powershell
cd backend
dotnet ef database update --project src/ServiceFlow.Infrastructure --startup-project src/ServiceFlow.Api
```

Review the generated SQL first if needed:

```powershell
dotnet ef migrations script --project src/ServiceFlow.Infrastructure --startup-project src/ServiceFlow.Api --output ../deployment/migrations-InitialCreate.sql
```

### Run API

```powershell
cd backend/src/ServiceFlow.Api
dotnet run
```

- Swagger UI: `https://localhost:7142/swagger` (or HTTP profile port)
- Health: `/health`
- API info: `/api/v1/info`

## Frontend

```powershell
cd frontend/serviceflow-web
npm install
npm start
```

App runs at `http://localhost:4200` and expects the API CORS origin configured in backend settings.

## Tenant security notes (Phase 1)

- `TenantId` is assigned by server-side domain factories / application services.
- Clients must never be trusted as the authority for tenant identity.
- EF Core global query filters hide tenant-owned rows unless a trusted `ITenantContext` is set.
- Cross-tenant writes are rejected in `ApplicationDbContext.SaveChangesAsync` when a tenant context is active.
- Authentication/session and tenant registration land in Phase 2.
