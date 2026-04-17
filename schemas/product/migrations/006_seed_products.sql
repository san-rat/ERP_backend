-- Migration 006: Seed 15 new products with inventory
BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Define the 15 new products in a CTE and INSERT in same batch
    ;WITH new_product_seed AS (
        SELECT *
        FROM (VALUES
            -- Electronics (Category 1)
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440011'), 1, N'TABLET-001', N'iPad Air', N'10.9-inch Liquid Retina display', 599.00, 50),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440012'), 1, N'HEADPHONE-001', N'Sony WH-1000XM5', N'Noise cancelling headphones', 349.99, 30),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440013'), 1, N'WATCH-001', N'Apple Watch Series 9', N'Advanced health features', 399.00, 25),
            
            -- Apparel (Category 2)
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440014'), 2, N'HOODIE-001', N'Oversized Fleece Hoodie', N'Warm winter hoodie', 55.00, 100),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440015'), 2, N'SHOES-001', N'Running Sneakers', N'Lightweight athletic shoes', 120.00, 40),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440016'), 2, N'JACKET-001', N'Waterproof Windbreaker', N'Outdoor weather protection', 85.50, 15),

            -- Beverages (Category 3)
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440017'), 3, N'ENERGY-001', N'Monster Energy Pack', N'12-pack sugar-free energy drinks', 22.00, 200),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440018'), 3, N'JUICE-001', N'Organic Orange Juice', N'100% pure squeezed juice', 5.99, 60),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440019'), 3, N'WATER-001', N'Sparkling Mineral Water', N'Naturally carbonated 750ml', 3.50, 150),

            -- Furniture (Category 4)
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440020'), 4, N'LAMP-001', N'LED Desk Lamp', N'Touch control with wireless charging', 45.00, 80),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440021'), 4, N'SHELF-001', N'5-Tier Bookshelf', N'Modern oak wood finish', 150.00, 12),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440022'), 4, N'CABINET-001', N'Filing Cabinet', N'3-drawer locking metal cabinet', 110.00, 10),

            -- Accessories (Category 5)
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440023'), 5, N'WEBCAM-001', N'Logitech C920 HD', N'Full HD 1080p video calling', 79.99, 45),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440024'), 5, N'USBC-HUB-001', N'7-in-1 USB-C Hub', N'4K HDMI and SD card reader', 35.00, 120),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440025'), 5, N'PAD-001', N'Extended Mouse Pad', N'Extra large desk mat', 19.99, 300)
        ) AS v(id, category_id, sku, name, description, price, stock)
    )
    INSERT INTO dbo.products (id, category_id, sku, name, description, price, is_active, quantity_available, created_at, updated_at)
    SELECT
        s.id, s.category_id, s.sku, s.name, s.description, s.price, 1, s.stock, GETUTCDATE(), GETUTCDATE()
    FROM new_product_seed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.products p WHERE p.sku = s.sku OR p.id = s.id);

    -- 2. Backfill inventory table - use separate CTE for this batch
    ;WITH inventory_seed AS (
        SELECT *
        FROM (VALUES
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440011'), 50),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440012'), 30),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440013'), 25),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440014'), 100),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440015'), 40),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440016'), 15),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440017'), 200),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440018'), 60),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440019'), 150),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440020'), 80),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440021'), 12),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440022'), 10),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440023'), 45),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440024'), 120),
            (CONVERT(UNIQUEIDENTIFIER, '650e8400-e29b-41d4-a716-446655440025'), 300)
        ) AS v(product_id, stock)
    )
    INSERT INTO dbo.inventory (product_id, quantity_available, quantity_reserved, low_stock_threshold, created_at, updated_at)
    SELECT
        s.product_id, s.stock, 0, 10, GETUTCDATE(), GETUTCDATE()
    FROM inventory_seed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.inventory i WHERE i.product_id = s.product_id);

    COMMIT TRANSACTION;
    PRINT '✓ 15 new products and their inventory records added successfully.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH