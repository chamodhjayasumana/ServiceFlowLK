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

`appsettings.json` keeps `DefaultConnection` empty on purpose. **Never commit SQL passwords.**

Preferred local setup: user secrets (overrides Development settings):

```powershell
cd backend/src/ServiceFlow.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=ServiceFlowLK;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true;Encrypt=False"
```

`appsettings.Development.json` may use Windows Trusted Connection as a non-secret fallback.

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

## Auth notes (Phase 2)

- Register: `POST /api/v1/tenants/register`
- Login/logout/me: `POST /api/v1/auth/login`, `POST /api/v1/auth/logout`, `GET /api/v1/me`
- CSRF: `GET /api/v1/auth/csrf` sets `XSRF-TOKEN`; send header `X-XSRF-TOKEN` on mutating authenticated calls
- Angular uses `proxy.conf.json` so cookies are same-site in local HTTP development
- Do not store tokens in `localStorage`

## Tenant security notes

- `TenantId` is assigned by server-side workflows only.
- `TenantContextMiddleware` derives tenant from authenticated user membership.
- EF query filters + save-time checks enforce isolation.
- Invitations are hashed, single-use, expiring; Owner cannot be assigned by invitation.
