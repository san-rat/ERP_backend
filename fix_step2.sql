-- Fix Step 2: Handle remaining column rename issues

-- ============================================================
-- Fix products table: drop the duplicate snake_case column
-- (migration 003 added 'created_by_user_id' as extra column,
--  but migration 005 already renamed 'CreatedByUserId' to 'created_by_user_id_renamed'
--  and the original snake_case one still exists)
-- Current state: CreatedByUserId still PascalCase, created_by_user_id is the 003 duplicate
-- ============================================================

-- Drop the index blocking the duplicate drop
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'idx_products_created_by_user')
    DROP INDEX idx_products_created_by_user ON dbo.products;
GO

-- Drop FK blocking operations
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Prod_Category')
    ALTER TABLE dbo.products DROP CONSTRAINT FK_Prod_Category;
GO

-- Now drop the duplicate created_by_user_id (the one from migration 003)
-- We need to identify which one is the duplicate vs the renamed PascalCase one
-- Current products columns: Id, category_id, Sku, Name, Description, Price, is_active, 
--   created_at, updated_at, CreatedByUserId, quantity_available, created_by_user_id
-- 'CreatedByUserId' is the original PascalCase from EF; created_by_user_id is 003 duplicate

-- Drop any default constraint on the 003-added quantity_available (if it exists as a constraint)
DECLARE @dfName NVARCHAR(256);
SELECT TOP 1 @dfName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[products]') AND c.name = 'created_by_user_id';
IF @dfName IS NOT NULL
    EXEC('ALTER TABLE dbo.products DROP CONSTRAINT [' + @dfName + ']');
GO

-- Drop the duplicate (003-added) created_by_user_id column
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'created_by_user_id')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'CreatedByUserId')
BEGIN
    ALTER TABLE dbo.products DROP COLUMN created_by_user_id;
END
GO

-- Rename the PascalCase CreatedByUserId to created_by_user_id
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'CreatedByUserId')
    EXEC sp_rename 'dbo.products.CreatedByUserId', 'created_by_user_id', 'COLUMN';
GO

-- Rename remaining PascalCase columns in products (Sku, Name, Description, Price, Id)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'Id')
    EXEC sp_rename 'dbo.products.Id', 'id', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'Sku')
    EXEC sp_rename 'dbo.products.Sku', 'sku', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'Name')
    EXEC sp_rename 'dbo.products.Name', 'name', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'Description')
    EXEC sp_rename 'dbo.products.Description', 'description', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'Price')
    EXEC sp_rename 'dbo.products.Price', 'price', 'COLUMN';
GO

-- ============================================================
-- Fix categories table: rename remaining PascalCase columns
-- Current: Id, Name, ParentId, Description, created_at, updated_at
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'Id')
    EXEC sp_rename 'dbo.categories.Id', 'id', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'Name')
    EXEC sp_rename 'dbo.categories.Name', 'name', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'ParentId')
    EXEC sp_rename 'dbo.categories.ParentId', 'parent_id', 'COLUMN';
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[categories]') AND name = 'Description')
    EXEC sp_rename 'dbo.categories.Description', 'description', 'COLUMN';
GO

-- ============================================================
-- Fix inventory table: rename id_old (it was 'Id', now 'id_old' from step 005)
-- The primary key is on ProductId (now product_id) per EF context
-- ============================================================

-- Drop primary key on id_old if it's the PK (inventory PK should be on product_id)
DECLARE @pkName NVARCHAR(256);
SELECT @pkName = kc.CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS kc ON kcu.CONSTRAINT_NAME = kc.CONSTRAINT_NAME
WHERE kcu.TABLE_NAME = 'inventory' AND kc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND kcu.COLUMN_NAME = 'id_old';
IF @pkName IS NOT NULL
    EXEC('ALTER TABLE dbo.inventory DROP CONSTRAINT [' + @pkName + ']');
GO

-- Drop the id_old column (it was extra from EF, EF context uses product_id as key)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[inventory]') AND name = 'id_old')
BEGIN
    ALTER TABLE dbo.inventory DROP COLUMN id_old;
END
GO

-- Add primary key on product_id if not already set
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'inventory' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND kcu.COLUMN_NAME = 'product_id'
)
BEGIN
    ALTER TABLE dbo.inventory ADD CONSTRAINT PK_inventory PRIMARY KEY (product_id);
END
GO

-- ============================================================
-- Fix InventoryReservations table: rename Id column
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[InventoryReservations]') AND name = 'Id')
    EXEC sp_rename 'dbo.InventoryReservations.Id', 'id', 'COLUMN';
GO

-- ============================================================
-- Fix LowStockAlerts table: rename Id column
-- ============================================================

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[LowStockAlerts]') AND name = 'Id')
    EXEC sp_rename 'dbo.LowStockAlerts.Id', 'id', 'COLUMN';
GO

-- ============================================================
-- Re-add foreign key constraint for products -> categories
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Prod_Category')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'category_id')
BEGIN
    ALTER TABLE dbo.products
    ADD CONSTRAINT FK_Prod_Category
    FOREIGN KEY (category_id) REFERENCES dbo.categories(id);
END
GO

-- Recreate index on created_by_user_id
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'idx_products_created_by_user')
AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[products]') AND name = 'created_by_user_id')
BEGIN
    CREATE INDEX idx_products_created_by_user ON dbo.products(created_by_user_id);
END
GO

PRINT 'Fix step 2 complete.';
