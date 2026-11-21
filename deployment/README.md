# Deployment

Phase 1 keeps deployment artifacts minimal.

## Guidelines

- Apply EF Core migrations through reviewed scripts or explicit `dotnet ef database update` in each environment.
- Do not enable automatic schema drops or destructive migrate-on-startup.
- Store production connection strings and secrets in managed secret storage (Azure Key Vault or equivalent).
- Never commit production credentials.

## Suggested next artifacts (later phases)

- Dockerfile for API
- Azure Bicep/Terraform for App Service + Azure SQL + private blob storage
- GitHub Actions deploy workflow gated on approval
