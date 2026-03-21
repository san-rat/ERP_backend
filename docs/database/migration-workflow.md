# Database Migration Workflow

This document explains why `InsightERP` uses migration files instead of one full SQL dump that gets edited and re-run.

## Short Answer

Yes. Using versioned migration files is the more professional approach.

A single large `.sql` file that you keep editing and re-running is acceptable for:

- early prototyping
- disposable local databases
- solo experiments

It stops being a good workflow once you have:

- shared development databases
- deployed environments
- real data that should not be deleted
- multiple developers changing the schema over time

## Why Migration Files Are Better

Migration files give you a clear history of database changes.

Instead of rewriting the same file, you keep a sequence such as:

```text
001_init.sql
002_add_username.sql
003_create_orders.sql
004_seed_admin.sql
005_seed_demo_users.sql
```

That gives you:

- repeatable deployments
- safer schema evolution
- a record of what changed and when
- easier team collaboration
- a way to upgrade databases without wiping existing data

## Why Rewriting One SQL File Causes Problems

The old approach usually looks like this:

1. Put the whole database schema in one `.sql` file.
2. Change that same file whenever the schema changes.
3. Drop the database or delete data.
4. Re-run the full file.

That works only while the database is disposable.

It becomes risky because:

- you lose data during every reset
- deployed environments cannot be updated safely
- teammates can end up with different database states
- there is no trustworthy schema history
- you cannot tell which change caused a regression

## Professional Rule Of Thumb

Use resets only for disposable local environments.

Use forward-only migrations for any shared or deployed database.

That means:

- local Docker DB: resetting is acceptable when needed
- `dev` Azure SQL: prefer adding a new migration, not rewriting an old one
- future staging or production DBs: never rely on dropping and rebuilding the whole database

## How This Repo Should Work

`InsightERP` now follows a migration-based structure under `schemas/*/migrations`.

Examples:

- [schemas/auth/migrations](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/auth/migrations)
- [schemas/customer/migrations](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/customer/migrations)
- [schemas/product/migrations](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/product/migrations)
- [schemas/order/migrations](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/order/migrations)
- [schemas/prediction/migrations](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/prediction/migrations)
- [schemas/analytics/migrations](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/schemas/analytics/migrations)

Each schema tracks applied files in its own `[schema].schema_migrations` table.

Current example:

- `auth.schema_migrations`
- `customer.schema_migrations`
- `product.schema_migrations`
- `[order].schema_migrations`
- `ml.schema_migrations`

## Working Rules For This Repo

1. Never edit an already-applied migration file in normal workflow.
2. Add a new numbered migration file for every schema change.
3. Keep schema creation and schema changes in migration files.
4. Keep demo or reference data in seed migrations when that data should be reproducible.
5. Use local reset scripts only for local developer convenience.
6. Treat deployed databases as upgrade targets, not rebuild targets.

## What To Do When The Schema Changes

If you need to change the database:

1. Do not rewrite `001_init.sql` if it has already been applied anywhere important.
2. Create a new migration file in the correct folder.
3. Make the change in that new file.
4. Apply migrations locally.
5. Let CI/CD apply the same migration sequence in Azure.

Example:

```text
schemas/auth/migrations/006_add_last_login_column.sql
schemas/product/migrations/003_add_sku_index.sql
```

## What To Do With Seed Data

Seed data is separate from the core idea of schema migration, but it can still live in migrations when the data must exist consistently.

In this repo:

- `auth`, `customer`, `product`, and `order` currently have seed migrations
- `dev` deployment is intentionally allowed to include demo seed data because this is a student project

That is acceptable here, but the important rule is still:

- do not keep changing old applied seed files
- add a new migration when seed behavior must change

## When A Full Reset Is Still Fine

A full reset is still useful for local development when:

- you want a clean start
- the local database is only test data
- you are verifying that all migrations can rebuild the schema from zero

That is what [local-database-setup.md](/mnt/c/Users/User/Desktop/coding/projects/2026/ERP_backend/docs/database/local-database-setup.md) and `docker compose down -v` are for.

## Recommended Team Mindset

Think of database changes the same way you think of source code history:

- do not rewrite shared history casually
- add the next change in sequence
- make deployments reproducible
- keep destructive resets limited to local development

## Bottom Line

The migration-based workflow you are moving to is the professional direction.

Editing one big SQL file and recreating the whole database is acceptable only for throwaway local work. Once the database matters to a team or a deployed environment, versioned migration files are the safer and more maintainable approach.
