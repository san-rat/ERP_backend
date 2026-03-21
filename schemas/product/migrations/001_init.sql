-- Create products table
CREATE TABLE dbo.products (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    category_id INT,
    sku NVARCHAR(100) NOT NULL UNIQUE,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    price DECIMAL(12, 2) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
