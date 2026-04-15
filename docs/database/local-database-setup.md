# Local Database Setup

This is the recommended local database flow for `InsightERP`.

It uses the repo's Dockerized SQL Server instance, creates `insighterp_db` if needed, and applies all discovered `schemas/*/migrations` folders in dependency order without requiring `sqlcmd` or `bash` on your Windows machine.

This document covers the database setup only. Starting the app stack is a separate step.

## Prerequisite

- Docker Desktop

## Run It

From the repo root:

```powershell
.\scripts\setup-local-db.ps1
```

The script is idempotent:

- it starts `erp-sqlserver-local` if needed
- it waits for SQL Server to be ready
- it creates `insighterp_db` only if it does not already exist
- it applies only migrations that are not yet recorded in each schema's `schema_migrations` table
- it skips empty or whitespace-only `.sql` files with a reminder and does not record them
- it does not wipe the Docker volume

## What This Does Not Start

`setup-local-db.ps1` does not start:

- `ApiGateway`
- any microservice container
- any `dotnet run` service process

If you run `docker compose up -d` from the repo root, that also starts only the `sqlserver` service.

## Start The App Stack After DB Setup

After the database is ready, choose one local app workflow.

### Host `dotnet run` workflow

For the existing PowerShell windows workflow:

```powershell
.\scripts\run-all-services.ps1
```

Stop it with:

```powershell
.\scripts\stop-all-services.ps1
```

### Docker-local app workflow

For the Docker-local stack with the API Gateway exposed on `http://localhost:5000`:

```powershell
.\scripts\run-docker-services.ps1
```

For a smaller subset:

```powershell
.\scripts\run-docker-services.ps1 -Services apigateway,authservice
```

For repeated runs when the database is already prepared:

```powershell
.\scripts\run-docker-services.ps1 -SkipDbSetup
```

Stop only the Docker-local app containers:

```powershell
.\scripts\stop-docker-services.ps1
```

Stop the Docker-local app containers and `sqlserver`:

```powershell
.\scripts\stop-docker-services.ps1 -IncludeDb
```

Gateway-backed Swagger entrypoints for the Docker-local workflow:

- `http://localhost:5000/auth/swagger`
- `http://localhost:5000/customer/swagger`
- `http://localhost:5000/order/swagger`
- `http://localhost:5000/product/swagger`
- `http://localhost:5000/forecast/swagger`
- `http://localhost:5000/prediction/swagger`
- `http://localhost:5000/analytics/swagger`
- `http://localhost:5000/admin/swagger`

## What The Script Does

`setup-local-db.ps1` performs these steps:

1. Runs `docker compose up -d sqlserver`.
2. Waits until `sqlcmd` inside the container can connect successfully.
3. Ensures the local database `insighterp_db` exists.
4. Discovers all `schemas/<name>/migrations` folders, then runs them in this default order: `auth`, `customer`, `product`, `order`, `prediction`, `analytics`.
5. Copies `scripts/apply_sqlserver_migrations.sh` and the repo `schemas/` folder into a temporary workspace inside the container.
6. Executes the existing migration runner once per migrations folder.
7. Prints a migration summary and the local `AuthService` connection string when setup is complete.

This keeps the local setup aligned with the same SQL Server migration flow used in CI/CD, while avoiding extra developer machine setup.

Because `auth`, `customer`, `product`, and `order` now include seed migrations, a fresh local reset also restores demo accounts and sample business data automatically.

## Current Schema Mapping

The folder name and the SQL schema name usually match. The current exception is:

- `schemas/prediction/migrations` is tracked in `ml.schema_migrations`, because the migration creates `ml.*` objects.

The current `analytics` placeholder migration file is empty. The setup script will print a reminder for it, skip it, and continue without error.

## Service Configuration

The database connection string belongs in `AuthService`, not in `ApiGateway`.

The current local development file is [appsettings.Development.json](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/src/AuthService/appsettings.Development.json).

It should contain:

```json
{
  "ConnectionStrings": {
    "AuthDb": "Server=localhost,1433;Database=insighterp_db;User Id=sa;Password=LocalDev_Password123!;TrustServerCertificate=True;"
  }
}
```

`ApiGateway` does not need a database connection string for this setup.

## Seeded Demo Data

A fresh local reset seeds these local auth accounts:

| Username | Password | Email | Role |
|---|---|---|---|
| `admin` | `Admin@123` | `admin@insighterp.local` | `ADMIN` |
| `testuser` | `Admin@123` | `testuser@insighterp.local` | `USER` |
| `manager` | `Admin@123` | `manager@insighterp.local` | `MANAGER` |

Other sample data restored by the seed migrations:

- `dbo.customers`: 10 rows
- `dbo.products`: 11 rows
- `dbo.orders`: 17 rows
- `dbo.order_items`: 27 rows
- `dbo.returns`: 4 rows

## Connect With Azure Data Studio

Use these values:

- Server: `localhost,1433`
- Database: `insighterp_db`
- Authentication: `SQL Login`
- User: `sa`
- Password: `LocalDev_Password123!`
- Trust server certificate: enabled

Useful verification queries:

```sql
SELECT * FROM auth.schema_migrations ORDER BY applied_at;
SELECT * FROM customer.schema_migrations ORDER BY applied_at;
SELECT * FROM product.schema_migrations ORDER BY applied_at;
SELECT * FROM [order].schema_migrations ORDER BY applied_at;
SELECT * FROM ml.schema_migrations ORDER BY applied_at;
SELECT username, email, is_active FROM auth.users ORDER BY username;
SELECT u.username, r.role_name
FROM auth.user_roles ur
JOIN auth.users u ON ur.user_id = u.id
JOIN auth.roles r ON ur.role_id = r.id
ORDER BY u.username, r.role_name;
SELECT COUNT(*) AS customer_count FROM dbo.customers;
SELECT COUNT(*) AS product_count FROM dbo.products;
SELECT COUNT(*) AS order_count FROM dbo.orders;
SELECT COUNT(*) AS order_item_count FROM dbo.order_items;
SELECT COUNT(*) AS return_count FROM dbo.returns;
```

## Query The Database From The Terminal

If you want to interact with the local database directly without installing `sqlcmd` on Windows, run it inside the Docker container:

```powershell
docker exec -it erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -C -d insighterp_db
```

Once connected, you can run queries like:

```sql
SELECT * FROM auth.schema_migrations ORDER BY applied_at;
SELECT * FROM customer.schema_migrations ORDER BY applied_at;
SELECT * FROM product.schema_migrations ORDER BY applied_at;
SELECT * FROM [order].schema_migrations ORDER BY applied_at;
SELECT * FROM ml.schema_migrations ORDER BY applied_at;
SELECT username, email, is_active FROM auth.users ORDER BY username;
SELECT u.username, r.role_name
FROM auth.user_roles ur
JOIN auth.users u ON ur.user_id = u.id
JOIN auth.roles r ON ur.role_id = r.id
ORDER BY u.username, r.role_name;
GO
```

Type `EXIT` to leave the `sqlcmd` session.

## Re-Run Safely

You can run the setup script again at any time:

```powershell
.\scripts\setup-local-db.ps1
```

Already applied migrations are skipped.

If you want to run only a subset of migrations, you can pass one or more folders explicitly:

```powershell
.\scripts\setup-local-db.ps1 -MigrationsPath schemas/auth/migrations,schemas/product/migrations
```

## Full Reset

If you want to destroy the local SQL Server data and start from scratch:

```powershell
docker compose down -v
.\scripts\setup-local-db.ps1
```

`docker compose down -v` is destructive. It deletes the local SQL Server volume.
