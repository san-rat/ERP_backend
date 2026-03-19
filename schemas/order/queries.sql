-- Indexes for order service
CREATE INDEX idx_orders_customer ON dbo.orders(customer_id);
CREATE INDEX idx_orders_status ON dbo.orders(status);
CREATE INDEX idx_orders_created ON dbo.orders(created_at);
CREATE INDEX idx_orderitems_order ON dbo.order_items(order_id);
CREATE INDEX idx_orderitems_product ON dbo.order_items(product_id);
CREATE INDEX idx_returns_order ON dbo.returns(order_id);