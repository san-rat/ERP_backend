-- Indexes for product service
CREATE INDEX idx_products_category ON dbo.products(category_id);
CREATE INDEX idx_products_sku ON dbo.products(sku);