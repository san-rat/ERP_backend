# InsightERP Database Guide

This guide covers the database model, migration flow, and Azure deployment setup for the backend.

For the local Docker developer workflow, use [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md).

For the rationale behind using versioned migration files instead of editing one full SQL dump, use [migration-workflow.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/migration-workflow.md).

## Database Model

InsightERP uses a single SQL Server database, `insighterp_db`, with per-service schemas.

The actively used schema in the running code is:

| Schema | Microservice | Current status |
|---|---|---|
| `auth` | `AuthService` | Active and used by the running code |

Additional migration folders also exist under `schemas/` for `customer`, `product`, `order`, `prediction`, and `analytics`.

Notes:

- The `prediction` migration folder targets the `ml` SQL schema, so its tracking table is `ml.schema_migrations`.
- The current `analytics` migration file is an empty placeholder. The runner skips empty files without recording them.

Always reference tables with their schema name:

```sql
SELECT * FROM auth.users;
```

## Migration Flow

The Azure/CD migration runner is [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh).

It:

1. Resolves the target SQL schema from the folder name, or from an explicit override when they differ.
2. Creates the target schema and `[schema].schema_migrations` when it encounters the first non-empty migration file.
3. Applies `.sql` files in the target migration folder.
4. Skips files already recorded in the tracking table.
5. Skips empty or whitespace-only `.sql` files without recording them.

This makes the process idempotent. Existing data is preserved.

## Local Development

The local setup flow is documented separately to avoid repeating the step-by-step Docker instructions here.

Use:

- [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md)

That guide covers:

- starting the SQL Server container
- creating `insighterp_db`
- applying all local schema migrations in dependency order
- restoring demo seed data for `auth`, `customer`, `product`, and `order`
- connecting with Azure Data Studio
- configuring `AuthService`

Fresh local resets now seed these auth accounts for testing:

| Username | Password | Email | Role |
|---|---|---|---|
| `admin` | `Admin@123` | `admin@insighterp.local` | `ADMIN` |
| `testuser` | `Admin@123` | `testuser@insighterp.local` | `USER` |
| `manager` | `Admin@123` | `manager@insighterp.local` | `MANAGER` |

Current seed migration files:

- [005_seed_demo_users.sql](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/auth/migrations/005_seed_demo_users.sql)
- [002_seed_customers.sql](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/customer/migrations/002_seed_customers.sql)
- [002_seed_products.sql](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/product/migrations/002_seed_products.sql)
- [002_seed_order_domain_data.sql](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/order/migrations/002_seed_order_domain_data.sql)

## Azure CI/CD

On pushes to `dev`, the CD workflow applies all current schema migrations before deploying services.

Relevant files:

- [cd-dev.yml](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/.github/workflows/cd-dev.yml)
- [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh)

Current deploy flow:

1. Install `mssql-tools18`.
2. Read Azure SQL credentials from GitHub Secrets.
3. Run the SQL Server migration script for:
   `schemas/auth/migrations`,
   `schemas/customer/migrations`,
   `schemas/product/migrations`,
   `schemas/order/migrations`,
   `schemas/prediction/migrations` as `ml`,
   and `schemas/analytics/migrations`.
4. Build and deploy the services to Azure Container Apps.

For this student project, the current seed migrations are intentionally included in the `dev` deployment flow, so a fresh dev database can be provisioned with demo users and sample business data.

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
2. Add the new schema folder to the migration target list in [cd-dev.yml](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/.github/workflows/cd-dev.yml).
3. Add the corresponding connection string secret and container app configuration.

If the SQL schema name does not match the folder name, pass the real schema name as the second argument to [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh).

## Rules For Database Changes

- Never edit an already-applied migration file.
- Add a new numbered `.sql` file for every schema change.
- Do not rely on empty placeholder migrations for real DDL. Leave them empty, remove them, or add a new numbered migration file when the schema is ready.
- Keep table names schema-qualified.
- Treat `auth` as the reference pattern for SQL Server migrations in this repo.

## Quick Reference

| Task | How |
|---|---|
| Local Docker DB setup | Run `.\scripts\setup-local-db.ps1` |
| View local DB instructions | Open [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md) |
| Azure migration runner | [apply_sqlserver_migrations.sh](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/scripts/apply_sqlserver_migrations.sh) |
| AuthService local connection string | `ConnectionStrings:AuthDb` in [appsettings.Development.json](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/src/AuthService/appsettings.Development.json) |
