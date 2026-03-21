# Current Database Schema

This document describes the database structure defined by the SQL migrations currently checked into this branch.

It is a migration-based snapshot, not a live database introspection. If a local or deployed database has extra manual changes or has not applied all migrations, it may differ from this document.

## Overview

Business data currently lives across these schemas:

- `dbo`: customers, products, categories, inventory, inventory reservations, low-stock alerts, orders, order items, returns
- `auth`: users, roles, role mapping, migration test table
- `ml`: prediction tables and prediction views
- `analytics`: schema exists, but no business tables are currently created by migrations

The migration runner also creates schema-specific migration bookkeeping tables such as `auth.schema_migrations`, `customer.schema_migrations`, `product.schema_migrations`, `[order].schema_migrations`, `ml.schema_migrations`, and `analytics.schema_migrations`.

## `auth` Schema

### `auth.users`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | None | Primary key |
| `email` | `NVARCHAR(255)` | No | None | Unique index |
| `password_hash` | `NVARCHAR(255)` | No | None |  |
| `full_name` | `NVARCHAR(255)` | Yes | None |  |
| `is_active` | `BIT` | No | `1` |  |
| `created_at` | `DATETIME` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME` | No | `GETUTCDATE()` |  |
| `username` | `NVARCHAR(100)` | No | None | Added in migration `003`, unique index |

### `auth.roles`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `INT IDENTITY(1,1)` | No | Identity | Primary key |
| `role_name` | `NVARCHAR(50)` | No | None | Unique index |

Seeded roles from migrations:

- `ADMIN`
- `USER`
- `MANAGER`

### `auth.user_roles`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `user_id` | `UNIQUEIDENTIFIER` | No | None | PK part, FK to `auth.users(id)` |
| `role_id` | `INT` | No | None | PK part, FK to `auth.roles(id)` |

Composite primary key: (`user_id`, `role_id`)

### `auth.migration_test`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `INT IDENTITY(1,1)` | No | Identity | Primary key |
| `note` | `NVARCHAR(50)` | Yes | None |  |

## `dbo` Schema

### `dbo.customers`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `email` | `NVARCHAR(255)` | No | None | Unique constraint in migration |
| `first_name` | `NVARCHAR(100)` | No | None |  |
| `last_name` | `NVARCHAR(100)` | No | None |  |
| `phone` | `NVARCHAR(20)` | Yes | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `dbo.products`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `category_id` | `INT` | Yes | None | Indexed, used by analytics and prediction views |
| `sku` | `NVARCHAR(100)` | No | None | Unique constraint, indexed |
| `name` | `NVARCHAR(255)` | No | None |  |
| `description` | `NVARCHAR(MAX)` | Yes | None |  |
| `price` | `DECIMAL(12,2)` | No | None |  |
| `is_active` | `BIT` | No | `1` |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `created_by_user_id` | `UNIQUEIDENTIFIER` | Yes | None | Added in product migration `003`, indexed |
| `quantity_available` | `INT` | No | `0` | Added in product migration `003`, backfilled to `0` before being made `NOT NULL` |

### `dbo.categories`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `INT` | No | None | Primary key |
| `name` | `NVARCHAR(100)` | No | None | Unique index |
| `description` | `NVARCHAR(500)` | Yes | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

Seeded category rows from migration `004`:

- `1`: `Electronics`
- `2`: `Apparel`
- `3`: `Beverages`
- `4`: `Furniture`
- `5`: `Accessories`

### `dbo.inventory`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `product_id` | `UNIQUEIDENTIFIER` | No | None | Primary key, FK to `dbo.products(id)` |
| `quantity_available` | `INT` | No | `0` | Check `>= 0` |
| `quantity_reserved` | `INT` | No | `0` | Check `>= 0` |
| `low_stock_threshold` | `INT` | No | `10` | Check `>= 0` |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `dbo.inventory_reservations`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `product_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.products(id)`, indexed |
| `order_id` | `UNIQUEIDENTIFIER` | No | None | Indexed, no FK currently |
| `quantity` | `INT` | No | None | Check `> 0` |
| `status` | `NVARCHAR(30)` | No | `N'DEDUCTED'` |  |
| `reserved_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `dbo.low_stock_alerts`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `product_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.products(id)`, indexed with `is_resolved` |
| `quantity_at_alert` | `INT` | No | None | Check `>= 0` |
| `is_resolved` | `BIT` | No | `0` |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `resolved_at` | `DATETIME2` | Yes | None |  |

### `dbo.orders`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `customer_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.customers(id)` |
| `status` | `NVARCHAR(30)` | No | `'PENDING'` |  |
| `total_amount` | `DECIMAL(12,2)` | No | `0.00` |  |
| `currency` | `CHAR(3)` | No | `'USD'` |  |
| `notes` | `NVARCHAR(MAX)` | Yes | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `dbo.order_items`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `order_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.orders(id)` |
| `product_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.products(id)` |
| `product_name` | `NVARCHAR(255)` | No | None | Snapshot name |
| `quantity` | `INT` | No | None |  |
| `unit_price` | `DECIMAL(12,2)` | No | None |  |
| `total_price` | `DECIMAL(12,2)` | No | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `dbo.returns`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `order_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.orders(id)` |
| `reason` | `NVARCHAR(MAX)` | Yes | None |  |
| `status` | `NVARCHAR(30)` | No | `'REQUESTED'` |  |
| `refund_amount` | `DECIMAL(12,2)` | Yes | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

## `ml` Schema

### `ml.churn_predictions`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `customer_id` | `UNIQUEIDENTIFIER` | No | None | FK to `dbo.customers(id)` |
| `churn_probability` | `DECIMAL(5,4)` | No | None | Check between `0` and `1` |
| `churn_risk_label` | `NVARCHAR(20)` | No | None | Check in `LOW`, `MEDIUM`, `HIGH` |
| `model_version` | `NVARCHAR(20)` | No | None |  |
| `predicted_at` | `DATETIME2` | No | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `ml.churn_factors`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `churn_prediction_id` | `UNIQUEIDENTIFIER` | No | None | FK to `ml.churn_predictions(id)` |
| `factor_name` | `NVARCHAR(255)` | No | None |  |
| `factor_weight` | `DECIMAL(5,4)` | Yes | None |  |
| `feature_value` | `NVARCHAR(MAX)` | Yes | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `ml.model_versions`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `model_version` | `NVARCHAR(20)` | No | None | Unique |
| `algorithm` | `NVARCHAR(100)` | No | None |  |
| `training_date` | `DATETIME2` | No | None |  |
| `training_data_count` | `INT` | Yes | None |  |
| `accuracy` | `DECIMAL(5,4)` | Yes | None |  |
| `precision` | `DECIMAL(5,4)` | Yes | None |  |
| `recall` | `DECIMAL(5,4)` | Yes | None |  |
| `auc_roc` | `DECIMAL(5,4)` | Yes | None |  |
| `is_active` | `BIT` | No | `0` |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |
| `updated_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

### `ml.training_history`

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `UNIQUEIDENTIFIER` | No | `NEWID()` | Primary key |
| `model_version_id` | `UNIQUEIDENTIFIER` | No | None | FK to `ml.model_versions(id)` |
| `training_start_time` | `DATETIME2` | No | None |  |
| `training_end_time` | `DATETIME2` | Yes | None |  |
| `training_status` | `NVARCHAR(30)` | No | None |  |
| `total_records_used` | `INT` | Yes | None |  |
| `churned_count` | `INT` | Yes | None |  |
| `non_churned_count` | `INT` | Yes | None |  |
| `error_message` | `NVARCHAR(MAX)` | Yes | None |  |
| `triggered_by` | `NVARCHAR(255)` | Yes | None |  |
| `created_at` | `DATETIME2` | No | `GETUTCDATE()` |  |

## `ml` Views

### `ml.v_customer_rfm`

Columns:

- `customer_id`
- `days_since_last_order`
- `total_orders`
- `total_spent`
- `avg_order_value`
- `customer_tenure_days`

### `ml.v_customer_product_diversity`

Columns:

- `customer_id`
- `unique_products_purchased`
- `unique_categories_purchased`
- `avg_products_per_order`

### `ml.v_customer_return_behavior`

Columns:

- `customer_id`
- `total_returns`
- `return_rate`
- `total_refunded`

### `ml.v_customer_order_status`

Columns:

- `customer_id`
- `completed_orders`
- `cancelled_orders`
- `cancellation_rate`

### `ml.v_customer_engagement`

Columns:

- `customer_id`
- `account_age_days`
- `days_since_last_update`
- `inactive_flag`

### `ml.v_customer_features_for_prediction`

Columns:

- `customer_id`
- `days_since_last_order`
- `total_orders`
- `total_spent`
- `avg_order_value`
- `customer_tenure_days`
- `unique_products_purchased`
- `unique_categories_purchased`
- `avg_products_per_order`
- `total_returns`
- `return_rate`
- `total_refunded`
- `completed_orders`
- `cancelled_orders`
- `cancellation_rate`
- `account_age_days`
- `days_since_last_activity`
- `inactive_flag`

## `analytics` Schema

Current migrations only create the `analytics` schema itself. No business tables are currently created under `analytics`.

## Seeded Data Summary

The current migrations also seed reference/demo data into several tables:

- `auth.roles`: baseline roles plus `MANAGER`
- `auth.users`: default admin plus demo `testuser`, `manager`, and `admin` records depending on prior state
- `dbo.customers`: 10 demo customers
- `dbo.products`: 11 demo products
- `dbo.categories`: 5 reference categories
- `dbo.orders`: 17 demo orders
- `dbo.order_items`: demo order-item rows tied to seeded orders/products
- `dbo.returns`: demo return rows tied to seeded orders
- `dbo.inventory`: one backfilled row per product after product migration `004`

## Migration Tracking Tables

When the repo migration runner applies schema folders, it also creates a `schema_migrations` table in the tracking schema for that folder.

Each of those tracking tables has the same structure:

| Column | Type | Null | Default | Notes |
|---|---|---|---|---|
| `id` | `INT IDENTITY(1,1)` | No | Identity | Primary key |
| `filename` | `NVARCHAR(255)` | No | None | Unique |
| `applied_at` | `DATETIME` | No | `GETDATE()` |  |

Expected tracking tables after full setup:

- `auth.schema_migrations`
- `customer.schema_migrations`
- `product.schema_migrations`
- `[order].schema_migrations`
- `ml.schema_migrations`
- `analytics.schema_migrations`
