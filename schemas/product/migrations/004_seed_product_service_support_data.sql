-- Migration 004: Seed categories and backfill inventory rows for existing products

;WITH category_seed AS (
    SELECT *
    FROM (VALUES
        (1, N'Electronics', N'Phones, laptops, monitors, and related devices'),
        (2, N'Apparel', N'Clothing and wearable items'),
        (3, N'Beverages', N'Drink and refreshment products'),
        (4, N'Furniture', N'Desks, chairs, and workplace furniture'),
        (5, N'Accessories', N'Computer accessories and peripherals')
    ) AS v(id, name, description)
)
INSERT INTO dbo.categories (id, name, description, created_at, updated_at)
SELECT
    s.id,
    s.name,
    s.description,
    GETUTCDATE(),
    GETUTCDATE()
FROM category_seed s
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.categories c
    WHERE c.id = s.id
       OR c.name = s.name
);
GO

INSERT INTO dbo.inventory (product_id, quantity_available, quantity_reserved, low_stock_threshold, created_at, updated_at)
SELECT
    p.id,
    COALESCE(p.quantity_available, 0),
    0,
    10,
    GETUTCDATE(),
    GETUTCDATE()
FROM dbo.products p
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.inventory i
    WHERE i.product_id = p.id
);
GO
