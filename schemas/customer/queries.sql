 -- Indexes for customer service
CREATE INDEX idx_customers_email ON dbo.customers(email);
CREATE INDEX idx_customers_created ON dbo.customers(created_at);