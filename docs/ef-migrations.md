# EF Core Migrations

## Commands

```bash
# Add an EF Core migration
dotnet ef migrations add <MigrationName> \
  --project SmartInventory.Inventories.Infra.Out.Repository \
  --startup-project SmartInventory.Inventories.WebApi

# Apply migrations manually (normally not needed — see below)
dotnet ef database update \
  --project SmartInventory.Inventories.Infra.Out.Repository \
  --startup-project SmartInventory.Inventories.WebApi
```

## Rules
- **IMPORTANT — migrations auto-apply at startup** via `app.MigrateDatabase()` 
- migrations live in `SmartInventory.Inventories.WebApi/Migrations/`. Generate new ones with `dotnet ef migrations add`, but **do not** run `dotnet ef database update` as a routine step startup handles it.
