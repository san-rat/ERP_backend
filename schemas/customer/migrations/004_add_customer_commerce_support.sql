IF COL_LENGTH('dbo.customers', 'password_hash') IS NULL
BEGIN
    ALTER TABLE dbo.customers
        ADD password_hash NVARCHAR(255) NULL;
END
GO

IF OBJECT_ID('dbo.customer_addresses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer_addresses (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        customer_id UNIQUEIDENTIFIER NOT NULL,
        full_name NVARCHAR(150) NOT NULL,
        phone NVARCHAR(20) NULL,
        address_line_1 NVARCHAR(255) NOT NULL,
        address_line_2 NVARCHAR(255) NULL,
        city NVARCHAR(100) NOT NULL,
        state NVARCHAR(100) NULL,
        postal_code NVARCHAR(30) NULL,
        country NVARCHAR(100) NOT NULL,
        is_default BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT fk_customer_addresses_customer FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.customer_carts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer_carts (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        customer_id UNIQUEIDENTIFIER NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT uq_customer_carts_customer UNIQUE (customer_id),
        CONSTRAINT fk_customer_carts_customer FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.customer_cart_items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer_cart_items (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        customer_cart_id UNIQUEIDENTIFIER NOT NULL,
        product_id UNIQUEIDENTIFIER NOT NULL,
        quantity INT NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT fk_customer_cart_items_cart FOREIGN KEY (customer_cart_id) REFERENCES dbo.customer_carts(id) ON DELETE CASCADE,
        CONSTRAINT fk_customer_cart_items_product FOREIGN KEY (product_id) REFERENCES dbo.products(id)
    );
END
GO

IF OBJECT_ID('dbo.customer_order_references', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer_order_references (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        customer_id UNIQUEIDENTIFIER NOT NULL,
        erp_order_id UNIQUEIDENTIFIER NOT NULL,
        external_order_id NVARCHAR(100) NOT NULL,
        total_amount DECIMAL(12, 2) NOT NULL,
        currency CHAR(3) NOT NULL DEFAULT 'USD',
        payment_method NVARCHAR(50) NOT NULL,
        status NVARCHAR(30) NOT NULL,
        shipping_full_name NVARCHAR(150) NOT NULL,
        shipping_phone NVARCHAR(20) NULL,
        shipping_address_line_1 NVARCHAR(255) NOT NULL,
        shipping_address_line_2 NVARCHAR(255) NULL,
        shipping_city NVARCHAR(100) NOT NULL,
        shipping_state NVARCHAR(100) NULL,
        shipping_postal_code NVARCHAR(30) NULL,
        shipping_country NVARCHAR(100) NOT NULL,
        notes NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT uq_customer_order_references_order UNIQUE (erp_order_id),
        CONSTRAINT fk_customer_order_references_customer FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE,
        -- SQL Server rejects multiple cascading paths from customers -> orders -> customer_order_references.
        CONSTRAINT fk_customer_order_references_order FOREIGN KEY (erp_order_id) REFERENCES dbo.orders(id) ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID('dbo.customer_order_reference_items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer_order_reference_items (
        id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        customer_order_reference_id UNIQUEIDENTIFIER NOT NULL,
        product_id UNIQUEIDENTIFIER NOT NULL,
        product_name NVARCHAR(255) NOT NULL,
        quantity INT NOT NULL,
        unit_price DECIMAL(12, 2) NOT NULL,
        total_price DECIMAL(12, 2) NOT NULL,
        CONSTRAINT fk_customer_order_reference_items_reference FOREIGN KEY (customer_order_reference_id) REFERENCES dbo.customer_order_references(id) ON DELETE CASCADE,
        CONSTRAINT fk_customer_order_reference_items_product FOREIGN KEY (product_id) REFERENCES dbo.products(id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_addresses_customer' AND object_id = OBJECT_ID('dbo.customer_addresses'))
BEGIN
    CREATE INDEX idx_customer_addresses_customer ON dbo.customer_addresses(customer_id, is_default, updated_at DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_cart_items_cart_product' AND object_id = OBJECT_ID('dbo.customer_cart_items'))
BEGIN
    CREATE UNIQUE INDEX idx_customer_cart_items_cart_product ON dbo.customer_cart_items(customer_cart_id, product_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_order_references_customer_created' AND object_id = OBJECT_ID('dbo.customer_order_references'))
BEGIN
    CREATE INDEX idx_customer_order_references_customer_created ON dbo.customer_order_references(customer_id, created_at DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_customer_order_reference_items_reference' AND object_id = OBJECT_ID('dbo.customer_order_reference_items'))
BEGIN
    CREATE INDEX idx_customer_order_reference_items_reference ON dbo.customer_order_reference_items(customer_order_reference_id);
END
GO
