-- Migration 005: Safely normalize legacy Product schema objects to snake_case.
--
-- Fresh installs created by migrations 001-004 already use the expected schema for
-- ProductService. This migration only fixes older PascalCase leftovers on shared
-- tables and ensures the required foreign keys/index exist without duplicating
-- constraints that are already present.

-- ============================================================
-- Step 1: Drop duplicate snake_case columns added by migration 003
-- only when the original PascalCase legacy column still exists.
-- ============================================================

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'created_by_user_id'
)
AND EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'CreatedByUserId'
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'[dbo].[products]')
          AND name = 'idx_products_created_by_user'
    )
    BEGIN
        DROP INDEX idx_products_created_by_user ON dbo.products;
    END

    ALTER TABLE dbo.products DROP COLUMN created_by_user_id;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'quantity_available'
)
AND EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'QuantityAvailable'
)
BEGIN
    DECLARE @dfName NVARCHAR(256);
    DECLARE @dropDefaultSql NVARCHAR(MAX);

    SELECT @dfName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c
      ON dc.parent_object_id = c.object_id
     AND dc.parent_column_id = c.column_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[products]')
      AND c.name = 'quantity_available';

    IF @dfName IS NOT NULL
    BEGIN
        SET @dropDefaultSql = N'ALTER TABLE dbo.products DROP CONSTRAINT ' + QUOTENAME(@dfName) + N';';
        EXEC sp_executesql @dropDefaultSql;
    END

    ALTER TABLE dbo.products DROP COLUMN quantity_available;
END
GO

-- ============================================================
-- Step 2: Rename legacy PascalCase columns on shared tables
-- only when the snake_case target column does not already exist.
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'CategoryId')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'category_id')
    EXEC sp_rename 'dbo.products.CategoryId', 'category_id', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'IsActive')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'is_active')
    EXEC sp_rename 'dbo.products.IsActive', 'is_active', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'CreatedAt')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'created_at')
    EXEC sp_rename 'dbo.products.CreatedAt', 'created_at', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'UpdatedAt')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'updated_at')
    EXEC sp_rename 'dbo.products.UpdatedAt', 'updated_at', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'CreatedByUserId')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'created_by_user_id')
    EXEC sp_rename 'dbo.products.CreatedByUserId', 'created_by_user_id', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'QuantityAvailable')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'quantity_available')
    EXEC sp_rename 'dbo.products.QuantityAvailable', 'quantity_available', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'CreatedAt')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'created_at')
    EXEC sp_rename 'dbo.categories.CreatedAt', 'created_at', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'UpdatedAt')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'updated_at')
    EXEC sp_rename 'dbo.categories.UpdatedAt', 'updated_at', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'ProductId')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'product_id')
    EXEC sp_rename 'dbo.inventory.ProductId', 'product_id', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'QuantityAvailable')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'quantity_available')
    EXEC sp_rename 'dbo.inventory.QuantityAvailable', 'quantity_available', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'QuantityReserved')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'quantity_reserved')
    EXEC sp_rename 'dbo.inventory.QuantityReserved', 'quantity_reserved', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'LowStockThreshold')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'low_stock_threshold')
    EXEC sp_rename 'dbo.inventory.LowStockThreshold', 'low_stock_threshold', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'UpdatedAt')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'updated_at')
    EXEC sp_rename 'dbo.inventory.UpdatedAt', 'updated_at', 'COLUMN';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'CreatedAt')
AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'created_at')
    EXEC sp_rename 'dbo.inventory.CreatedAt', 'created_at', 'COLUMN';
GO

-- ============================================================
-- Step 3: Ensure required foreign keys exist on the current
-- snake_case schema without duplicating existing relationships.
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'category_id')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'id')
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_key_columns fkc
    JOIN sys.columns pc
      ON pc.object_id = fkc.parent_object_id
     AND pc.column_id = fkc.parent_column_id
    JOIN sys.columns rc
      ON rc.object_id = fkc.referenced_object_id
     AND rc.column_id = fkc.referenced_column_id
    WHERE fkc.parent_object_id = OBJECT_ID(N'[dbo].[products]')
      AND fkc.referenced_object_id = OBJECT_ID(N'[dbo].[categories]')
      AND pc.name = 'category_id'
      AND rc.name = 'id'
)
BEGIN
    ALTER TABLE dbo.products
    ADD CONSTRAINT FK_Prod_Category
    FOREIGN KEY (category_id) REFERENCES dbo.categories(id);
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'product_id')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'id')
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_key_columns fkc
    JOIN sys.columns pc
      ON pc.object_id = fkc.parent_object_id
     AND pc.column_id = fkc.parent_column_id
    JOIN sys.columns rc
      ON rc.object_id = fkc.referenced_object_id
     AND rc.column_id = fkc.referenced_column_id
    WHERE fkc.parent_object_id = OBJECT_ID(N'[dbo].[inventory]')
      AND fkc.referenced_object_id = OBJECT_ID(N'[dbo].[products]')
      AND pc.name = 'product_id'
      AND rc.name = 'id'
)
BEGIN
    ALTER TABLE dbo.inventory
    ADD CONSTRAINT FK_Inv_Product
    FOREIGN KEY (product_id) REFERENCES dbo.products(id) ON DELETE CASCADE;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory_reservations]') AND name = 'product_id')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'id')
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_key_columns fkc
    JOIN sys.columns pc
      ON pc.object_id = fkc.parent_object_id
     AND pc.column_id = fkc.parent_column_id
    JOIN sys.columns rc
      ON rc.object_id = fkc.referenced_object_id
     AND rc.column_id = fkc.referenced_column_id
    WHERE fkc.parent_object_id = OBJECT_ID(N'[dbo].[inventory_reservations]')
      AND fkc.referenced_object_id = OBJECT_ID(N'[dbo].[products]')
      AND pc.name = 'product_id'
      AND rc.name = 'id'
)
BEGIN
    ALTER TABLE dbo.inventory_reservations
    ADD CONSTRAINT FK_Res_Product
    FOREIGN KEY (product_id) REFERENCES dbo.products(id) ON DELETE CASCADE;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[low_stock_alerts]') AND name = 'product_id')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'id')
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_key_columns fkc
    JOIN sys.columns pc
      ON pc.object_id = fkc.parent_object_id
     AND pc.column_id = fkc.parent_column_id
    JOIN sys.columns rc
      ON rc.object_id = fkc.referenced_object_id
     AND rc.column_id = fkc.referenced_column_id
    WHERE fkc.parent_object_id = OBJECT_ID(N'[dbo].[low_stock_alerts]')
      AND fkc.referenced_object_id = OBJECT_ID(N'[dbo].[products]')
      AND pc.name = 'product_id'
      AND rc.name = 'id'
)
BEGIN
    ALTER TABLE dbo.low_stock_alerts
    ADD CONSTRAINT FK_Lsa_Product
    FOREIGN KEY (product_id) REFERENCES dbo.products(id) ON DELETE CASCADE;
END
GO

-- ============================================================
-- Step 4: Ensure the supporting index still exists after any
-- duplicate-column cleanup above.
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'created_by_user_id')
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'idx_products_created_by_user'
)
BEGIN
    CREATE INDEX idx_products_created_by_user
    ON dbo.products(created_by_user_id);
END
GO
