# Local Database Setup

This is the recommended local database flow for `InsightERP`.

It uses the repo's Dockerized SQL Server instance, creates `insighterp_db` if needed, and applies the `auth` schema migrations without requiring `sqlcmd` or `bash` on your Windows machine.

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
- it applies only migrations that are not yet recorded in `auth.schema_migrations`
- it does not wipe the Docker volume

## What The Script Does

`setup-local-db.ps1` performs these steps:

1. Runs `docker compose up -d sqlserver`.
2. Waits until `sqlcmd` inside the container can connect successfully.
3. Ensures the local database `insighterp_db` exists.
4. Copies `scripts/apply_sqlserver_migrations.sh` and `schemas/auth/migrations` into a temporary workspace inside the container.
5. Executes the existing migration runner inside the container, targeting `schemas/auth/migrations`.
6. Prints the local `AuthService` connection string when setup is complete.

This keeps the local setup aligned with the same SQL Server migration flow used in CI/CD, while avoiding extra developer machine setup.

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
SELECT username, email, is_active FROM auth.users;
SELECT role_name FROM auth.roles;
```

## Query The Database From The Terminal

If you want to interact with the local database directly without installing `sqlcmd` on Windows, run it inside the Docker container:

```powershell
docker exec -it erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -C -d insighterp_db
```

Once connected, you can run queries like:

```sql
SELECT * FROM auth.schema_migrations ORDER BY applied_at;
SELECT username, email, is_active FROM auth.users;
SELECT * FROM auth.roles;
GO
```

Type `EXIT` to leave the `sqlcmd` session.

## Re-Run Safely

You can run the setup script again at any time:

```powershell
.\scripts\setup-local-db.ps1
```

Already applied migrations are skipped.

## Full Reset

If you want to destroy the local SQL Server data and start from scratch:

```powershell
docker compose down -v
.\scripts\setup-local-db.ps1
```

`docker compose down -v` is destructive. It deletes the local SQL Server volume.
