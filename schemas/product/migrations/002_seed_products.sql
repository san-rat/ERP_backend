-- Migration 002: Seed demo products

;WITH seed_data AS (
    SELECT *
    FROM (VALUES
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440000'), 1, N'LAPTOP-001', N'Dell Laptop 15', N'High-performance laptop', 999.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440001'), 1, N'PHONE-001', N'iPhone 15 Pro', N'Latest Apple smartphone', 999.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440002'), 1, N'MONITOR-001', N'LG 4K Monitor', N'27-inch 4K display', 599.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440003'), 2, N'SHIRT-001', N'Cotton T-Shirt Blue', N'Comfortable cotton t-shirt', 29.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440004'), 2, N'JEANS-001', N'Blue Jeans Classic', N'Classic blue denim jeans', 79.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440005'), 3, N'COFFEE-001', N'Premium Coffee Beans', N'Freshly roasted arabica beans', 24.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440006'), 3, N'TEA-001', N'Green Tea Assortment', N'Variety pack of green teas', 15.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440007'), 4, N'DESK-001', N'Standing Office Desk', N'Adjustable height office desk', 499.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440008'), 4, N'CHAIR-001', N'Ergonomic Office Chair', N'Comfortable ergonomic chair', 299.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440009'), 5, N'MOUSE-001', N'Wireless Mouse Pro', N'Wireless gaming mouse', 49.99, 1),
        (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440010'), 5, N'KEYBOARD-001', N'Mechanical Keyboard', N'RGB mechanical keyboard', 149.99, 1)
    ) AS v(id, category_id, sku, name, description, price, is_active)
)
INSERT INTO dbo.products (id, category_id, sku, name, description, price, is_active, created_at, updated_at)
SELECT
    s.id,
    s.category_id,
    s.sku,
    s.name,
    s.description,
    s.price,
    s.is_active,
    GETUTCDATE(),
    GETUTCDATE()
FROM seed_data s
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.products p
    WHERE p.id = s.id OR p.sku = s.sku
);
