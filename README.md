# Anders Warehouse MVP (.NET 8)

Production-ready MVP scaffold for multi-tenant warehouse management on ASP.NET Core Razor Pages + API.

## Architecture
- **Domain**: entities with TenantId and audit fields.
- **Application**: service abstractions and orchestration.
- **Infrastructure**: EF Core DbContext, parser pipeline, blob/image/price-source adapters.
- **Web**: Razor UI + API endpoints for end-to-end flows.

## Implemented MVP Flows
1. Create tenant/user (seeded demo tenant + admin).
2. Upload purchase PDF -> parse heuristically/OCR fallback stub -> save invoice + lines.
3. Approve lines -> create/link product -> auto image fetch pipeline (Google-ready abstraction) -> save image blob URL.
4. Inventory IN from approved purchase.
5. Register sold OUT transaction.
6. Upload agreement + rules (entity support) -> validate next invoice and create deviation report.
7. Price monitor definitions + Hangfire weekly runner -> snapshots + notifications.

## Local Run
```bash
# if .NET SDK is installed
cd src/Anders.Warehouse.Web
# optional explicit environment
export ASPNETCORE_ENVIRONMENT=Development

dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Environment-specific config:
- `appsettings.Development.json`: uses `Sqlite` + `Hangfire:Enabled=false`
- `Database:ForceSqlServerInDevelopment`: keep `false` for local safety (set `true` only if local SQL Server is intentionally used)
- `appsettings.Production.json`: expects `SqlServer` + `Hangfire:Enabled=true`

App URLs:
- `/` dashboard
- `/swagger` API docs
- `/hangfire` Hangfire dashboard

## Key Config Values
Set in `appsettings.json` / user-secrets / Azure App Configuration:
- `ConnectionStrings:DefaultConnection` (Azure SQL in prod)
- `DatabaseProvider` (set per environment in `appsettings.Development.json` / `appsettings.Production.json`)
- `Database:ForceSqlServerInDevelopment` (overrides dev safety guard when set to `true`)
- `Storage:BlobConnectionString`
- `Storage:BlobBaseUrl`
- `GoogleSearch:ApiKey`
- `GoogleSearch:Cx`
- `KeyVault:VaultUri`
- `Hangfire:Enabled` (set per environment; false in Development, true in Production)
- `Secrets:*` (local fallback for credentials used by price source)

## Azure Deployment Notes
1. Azure App Service + Azure SQL + Azure Blob Storage + Key Vault.
2. Set `DatabaseProvider=SqlServer` and SQL connection string.
3. Run migrations on deploy pipeline (`dotnet ef database update`).
4. Enable Managed Identity and Key Vault access policies.
5. Configure Hangfire storage to SQL and keep always-on enabled.

## Integration-ready Interfaces (TODO)
- `IShopifyClient`
- `IMagentoClient`
