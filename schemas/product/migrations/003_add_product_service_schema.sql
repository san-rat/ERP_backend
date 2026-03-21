-- Migration 003: Add ProductService support schema without changing existing tables destructively

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'created_by_user_id'
)
BEGIN
    ALTER TABLE dbo.products
    ADD created_by_user_id UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'quantity_available'
)
BEGIN
    ALTER TABLE dbo.products
    ADD quantity_available INT NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'quantity_available'
)
BEGIN
    UPDATE dbo.products
    SET quantity_available = 0
    WHERE quantity_available IS NULL;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[products]')
      AND name = 'quantity_available'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.products
    ALTER COLUMN quantity_available INT NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[products]')
      AND c.name = 'quantity_available'
)
BEGIN
    ALTER TABLE dbo.products
    ADD CONSTRAINT df_products_quantity_available DEFAULT (0) FOR quantity_available;
END
GO

IF OBJECT_ID(N'[dbo].[categories]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.categories (
        id INT NOT NULL PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(500) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[categories]')
      AND name = 'uq_categories_name'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX uq_categories_name
    ON dbo.categories(name);
END
GO

IF OBJECT_ID(N'[dbo].[inventory]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.inventory (
        product_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        quantity_available INT NOT NULL DEFAULT (0),
        quantity_reserved INT NOT NULL DEFAULT (0),
        low_stock_threshold INT NOT NULL DEFAULT (10),
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT chk_inventory_quantity_available_nonnegative CHECK (quantity_available >= 0),
        CONSTRAINT chk_inventory_quantity_reserved_nonnegative CHECK (quantity_reserved >= 0),
        CONSTRAINT chk_inventory_low_stock_threshold_nonnegative CHECK (low_stock_threshold >= 0),
        CONSTRAINT fk_inventory_product FOREIGN KEY (product_id) REFERENCES dbo.products(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'[dbo].[inventory_reservations]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.inventory_reservations (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        product_id UNIQUEIDENTIFIER NOT NULL,
        order_id UNIQUEIDENTIFIER NOT NULL,
        quantity INT NOT NULL,
        status NVARCHAR(30) NOT NULL DEFAULT N'DEDUCTED',
        reserved_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT chk_inventory_reservations_quantity_positive CHECK (quantity > 0),
        CONSTRAINT fk_inventory_reservations_product FOREIGN KEY (product_id) REFERENCES dbo.products(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'[dbo].[low_stock_alerts]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.low_stock_alerts (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        product_id UNIQUEIDENTIFIER NOT NULL,
        quantity_at_alert INT NOT NULL,
        is_resolved BIT NOT NULL DEFAULT (0),
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        resolved_at DATETIME2 NULL,
        CONSTRAINT chk_low_stock_alerts_quantity_nonnegative CHECK (quantity_at_alert >= 0),
        CONSTRAINT fk_low_stock_alerts_product FOREIGN KEY (product_id) REFERENCES dbo.products(id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
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

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[inventory_reservations]')
      AND name = 'idx_inventory_reservations_product'
)
BEGIN
    CREATE INDEX idx_inventory_reservations_product
    ON dbo.inventory_reservations(product_id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[inventory_reservations]')
      AND name = 'idx_inventory_reservations_order'
)
BEGIN
    CREATE INDEX idx_inventory_reservations_order
    ON dbo.inventory_reservations(order_id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[low_stock_alerts]')
      AND name = 'idx_low_stock_alerts_product_resolved'
)
BEGIN
    CREATE INDEX idx_low_stock_alerts_product_resolved
    ON dbo.low_stock_alerts(product_id, is_resolved);
END
GO
