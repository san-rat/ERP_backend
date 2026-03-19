-- Create orders table (depends on dbo.customers)
CREATE TABLE dbo.orders (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    customer_id UNIQUEIDENTIFIER NOT NULL,
    status NVARCHAR(30) NOT NULL DEFAULT 'PENDING',
    total_amount DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
    currency CHAR(3) NOT NULL DEFAULT 'USD',
    notes NVARCHAR(MAX),
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT fk_orders_customer FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE
);
 
-- Create order_items table (depends on dbo.orders, dbo.products)
CREATE TABLE dbo.order_items (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    order_id UNIQUEIDENTIFIER NOT NULL,
    product_id UNIQUEIDENTIFIER NOT NULL,
    product_name NVARCHAR(255) NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(12, 2) NOT NULL,
    total_price DECIMAL(12, 2) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT fk_orderitems_order FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
    CONSTRAINT fk_orderitems_product FOREIGN KEY (product_id) REFERENCES dbo.products(id)
);
 
-- Create returns table (depends on dbo.orders)
CREATE TABLE dbo.returns (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    order_id UNIQUEIDENTIFIER NOT NULL,
    reason NVARCHAR(MAX),
    status NVARCHAR(30) NOT NULL DEFAULT 'REQUESTED',
    refund_amount DECIMAL(12, 2),
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT fk_returns_order FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE
);

 