# InsightERP Database Guide

This guide covers the database model, migration flow, and Azure deployment setup for the backend.

For the local Docker developer workflow, use [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md).

## Database Model

InsightERP uses a single SQL Server database, `insighterp_db`, with per-service schemas.

The active schema in the current implementation is:

| Schema | Microservice | Current status |
|---|---|---|
| `auth` | `AuthService` | Active and used by the running code |

Additional schema folders exist under `schemas/` for future services, but those are still being normalized to the SQL Server migration standard.

Always reference tables with their schema name:

```sql
SELECT * FROM auth.users;
```

## Migration Flow

The Azure/CD migration runner is [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh).

It:

1. Creates the target schema if it does not exist.
2. Creates `[schema].schema_migrations` if it does not exist.
3. Applies `.sql` files in the target migration folder.
4. Skips files already recorded in the tracking table.

This makes the process idempotent. Existing data is preserved.

## Local Development

The local setup flow is documented separately to avoid repeating the step-by-step Docker instructions here.

Use:

- [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md)

That guide covers:

- starting the SQL Server container
- creating `insighterp_db`
- applying `auth` migrations
- connecting with Azure Data Studio
- configuring `AuthService`

## Azure CI/CD

On pushes to `dev`, the CD workflow applies auth migrations before deploying services.

Relevant files:

- [cd-dev.yml](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/.github/workflows/cd-dev.yml)
- [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh)

Current deploy flow:

1. Install `mssql-tools18`.
2. Read Azure SQL credentials from GitHub Secrets.
3. Run the SQL Server migration script for `schemas/auth/migrations`.
4. Build and deploy the services to Azure Container Apps.

## Adding a New Schema

When a new service is ready for a real database, add:

```text
schemas/
  <service>/
    migrations/
      001_init.sql
```

Then:

1. Write the migration in T-SQL for SQL Server/Azure SQL.
2. Add a migration step in [cd-dev.yml](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/.github/workflows/cd-dev.yml).
3. Add the corresponding connection string secret and container app configuration.

## Rules For Database Changes

- Never edit an already-applied migration file.
- Add a new numbered `.sql` file for every schema change.
- Keep table names schema-qualified.
- Treat `auth` as the reference pattern for SQL Server migrations in this repo.

## Quick Reference

| Task | How |
|---|---|
| Local Docker DB setup | Run `.\scripts\setup-local-db.ps1` |
| View local DB instructions | Open [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md) |
| Azure migration runner | [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh) |
| AuthService local connection string | `ConnectionStrings:AuthDb` in [appsettings.Development.json](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/src/AuthService/appsettings.Development.json) |
