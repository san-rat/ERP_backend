#!/usr/bin/env pwsh
# InsightERP Direct Setup - SQL Server Compatible

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "InsightERP Database Setup" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Creating database..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -C -Q "CREATE DATABASE [insighterp_db];"

Write-Host "Creating auth schema and tables..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q @"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth') EXEC('CREATE SCHEMA auth');
GO
CREATE TABLE auth.users (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), username NVARCHAR(255) NOT NULL UNIQUE, email NVARCHAR(255) NOT NULL UNIQUE, password_hash NVARCHAR(MAX), is_active BIT DEFAULT 1, created_at DATETIME2 DEFAULT GETUTCDATE(), updated_at DATETIME2 DEFAULT GETUTCDATE());
CREATE TABLE auth.roles (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), role_name NVARCHAR(100) NOT NULL UNIQUE, created_at DATETIME2 DEFAULT GETUTCDATE());
INSERT INTO auth.roles (role_name) VALUES ('admin');
INSERT INTO auth.roles (role_name) VALUES ('user');
INSERT INTO auth.roles (role_name) VALUES ('manager');
INSERT INTO auth.users (username, email, password_hash, is_active) VALUES ('admin', 'admin@insighterp.local', 'hash', 1);
INSERT INTO auth.users (username, email, password_hash, is_active) VALUES ('testuser', 'testuser@insighterp.local', 'hash', 1);
INSERT INTO auth.users (username, email, password_hash, is_active) VALUES ('manager', 'manager@insighterp.local', 'hash', 1);
"@

Write-Host "Creating customers table..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q @"
CREATE TABLE dbo.customers (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), email NVARCHAR(255) NOT NULL UNIQUE, first_name NVARCHAR(100) NOT NULL, last_name NVARCHAR(100) NOT NULL, phone NVARCHAR(20), created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440000', 'john.doe@example.com', 'John', 'Doe', '555-1234', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440001', 'jane.smith@example.com', 'Jane', 'Smith', '555-5678', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440002', 'alice.johnson@example.com', 'Alice', 'Johnson', '555-9012', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440003', 'bob.williams@example.com', 'Bob', 'Williams', '555-3456', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440004', 'carol.davis@example.com', 'Carol', 'Davis', '555-7890', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440005', 'david.miller@example.com', 'David', 'Miller', '555-2341', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440006', 'emma.wilson@example.com', 'Emma', 'Wilson', '555-6789', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440007', 'frank.moore@example.com', 'Frank', 'Moore', '555-0123', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440008', 'grace.taylor@example.com', 'Grace', 'Taylor', '555-4567', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.customers VALUES ('550e8400-e29b-41d4-a716-446655440009', 'henry.anderson@example.com', 'Henry', 'Anderson', '555-8901', GETUTCDATE(), GETUTCDATE());
"@

Write-Host "Creating products table..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q @"
CREATE TABLE dbo.products (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), category_id INT, sku NVARCHAR(100) NOT NULL UNIQUE, name NVARCHAR(255) NOT NULL, description NVARCHAR(MAX), price DECIMAL(12, 2) NOT NULL, is_active BIT NOT NULL DEFAULT 1, created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440000', 1, 'LAPTOP-001', 'Dell Laptop 15', 'High-performance laptop', 999.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440001', 1, 'PHONE-001', 'iPhone 15 Pro', 'Latest Apple smartphone', 999.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440002', 1, 'MONITOR-001', 'LG 4K Monitor', '27-inch 4K display', 599.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440003', 2, 'SHIRT-001', 'Cotton T-Shirt Blue', 'Comfortable cotton t-shirt', 29.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440004', 2, 'JEANS-001', 'Blue Jeans Classic', 'Classic blue denim jeans', 79.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440005', 3, 'COFFEE-001', 'Premium Coffee Beans', 'Freshly roasted arabica beans', 24.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440006', 3, 'TEA-001', 'Green Tea Assortment', 'Variety pack of green teas', 15.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440007', 4, 'DESK-001', 'Standing Office Desk', 'Adjustable height office desk', 499.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440008', 4, 'CHAIR-001', 'Ergonomic Office Chair', 'Comfortable ergonomic chair', 299.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440009', 5, 'MOUSE-001', 'Wireless Mouse Pro', 'Wireless gaming mouse', 49.99, 1, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.products VALUES ('650e8400-e29b-41d4-a716-446655440010', 5, 'KEYBOARD-001', 'Mechanical Keyboard', 'RGB mechanical keyboard', 149.99, 1, GETUTCDATE(), GETUTCDATE());
"@

Write-Host "Creating orders table..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q @"
CREATE TABLE dbo.orders (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), customer_id UNIQUEIDENTIFIER NOT NULL, status NVARCHAR(30) NOT NULL DEFAULT 'PENDING', total_amount DECIMAL(12, 2) NOT NULL DEFAULT 0.00, currency CHAR(3) NOT NULL DEFAULT 'USD', notes NVARCHAR(MAX), created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), CONSTRAINT fk_orders_customer FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE);
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440000', 'DELIVERED', 1049.98, 'USD', 'Fast delivery', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440000', 'DELIVERED', 99.99, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440001', 'DELIVERED', 1549.97, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440003', '550e8400-e29b-41d4-a716-446655440001', 'CANCELLED', 249.98, 'USD', 'Customer request', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440004', '550e8400-e29b-41d4-a716-446655440002', 'DELIVERED', 799.98, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440005', '550e8400-e29b-41d4-a716-446655440002', 'CANCELLED', 49.99, 'USD', 'Out of stock', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440006', '550e8400-e29b-41d4-a716-446655440003', 'DELIVERED', 1449.97, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440007', '550e8400-e29b-41d4-a716-446655440003', 'DELIVERED', 799.99, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440008', '550e8400-e29b-41d4-a716-446655440004', 'DELIVERED', 1399.97, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440009', '550e8400-e29b-41d4-a716-446655440005', 'DELIVERED', 649.98, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440010', '550e8400-e29b-41d4-a716-446655440006', 'DELIVERED', 749.98, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440011', '550e8400-e29b-41d4-a716-446655440007', 'CANCELLED', 99.99, 'USD', 'Wrong item', GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440012', '550e8400-e29b-41d4-a716-446655440008', 'DELIVERED', 1649.97, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440013', '550e8400-e29b-41d4-a716-446655440009', 'DELIVERED', 449.98, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440014', '550e8400-e29b-41d4-a716-446655440000', 'DELIVERED', 999.99, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440015', '550e8400-e29b-41d4-a716-446655440001', 'DELIVERED', 449.98, 'USD', NULL, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.orders VALUES ('750e8400-e29b-41d4-a716-446655440016', '550e8400-e29b-41d4-a716-446655440005', 'CANCELLED', 149.99, 'USD', 'Changed mind', GETUTCDATE(), GETUTCDATE());
"@

Write-Host "Creating order_items table..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q @"
CREATE TABLE dbo.order_items (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), order_id UNIQUEIDENTIFIER NOT NULL, product_id UNIQUEIDENTIFIER NOT NULL, product_name NVARCHAR(255) NOT NULL, quantity INT NOT NULL, unit_price DECIMAL(12, 2) NOT NULL, total_price DECIMAL(12, 2) NOT NULL, created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), CONSTRAINT fk_orderitems_order FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE, CONSTRAINT fk_orderitems_product FOREIGN KEY (product_id) REFERENCES dbo.products(id));
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440000', '750e8400-e29b-41d4-a716-446655440000', '650e8400-e29b-41d4-a716-446655440000', 'Dell Laptop 15', 1, 999.99, 999.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440001', '750e8400-e29b-41d4-a716-446655440000', '650e8400-e29b-41d4-a716-446655440009', 'Wireless Mouse Pro', 1, 49.99, 49.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440002', '750e8400-e29b-41d4-a716-446655440001', '650e8400-e29b-41d4-a716-446655440003', 'Cotton T-Shirt Blue', 3, 29.99, 89.97, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440003', '750e8400-e29b-41d4-a716-446655440001', '650e8400-e29b-41d4-a716-446655440004', 'Blue Jeans Classic', 1, 79.99, 79.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440004', '750e8400-e29b-41d4-a716-446655440002', '650e8400-e29b-41d4-a716-446655440001', 'iPhone 15 Pro', 1, 999.99, 999.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440005', '750e8400-e29b-41d4-a716-446655440002', '650e8400-e29b-41d4-a716-446655440002', 'LG 4K Monitor', 1, 599.99, 599.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440006', '750e8400-e29b-41d4-a716-446655440003', '650e8400-e29b-41d4-a716-446655440008', 'Ergonomic Office Chair', 2, 149.99, 299.98, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440007', '750e8400-e29b-41d4-a716-446655440004', '650e8400-e29b-41d4-a716-446655440007', 'Standing Office Desk', 1, 499.99, 499.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440008', '750e8400-e29b-41d4-a716-446655440004', '650e8400-e29b-41d4-a716-446655440010', 'Mechanical Keyboard', 1, 149.99, 149.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440009', '750e8400-e29b-41d4-a716-446655440005', '650e8400-e29b-41d4-a716-446655440005', 'Premium Coffee Beans', 2, 24.99, 49.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440010', '750e8400-e29b-41d4-a716-446655440006', '650e8400-e29b-41d4-a716-446655440001', 'iPhone 15 Pro', 1, 999.99, 999.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440011', '750e8400-e29b-41d4-a716-446655440006', '650e8400-e29b-41d4-a716-446655440009', 'Wireless Mouse Pro', 1, 49.99, 49.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440012', '750e8400-e29b-41d4-a716-446655440006', '650e8400-e29b-41d4-a716-446655440010', 'Mechanical Keyboard', 1, 149.99, 149.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440013', '750e8400-e29b-41d4-a716-446655440007', '650e8400-e29b-41d4-a716-446655440002', 'LG 4K Monitor', 1, 599.99, 599.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440014', '750e8400-e29b-41d4-a716-446655440007', '650e8400-e29b-41d4-a716-446655440006', 'Green Tea Assortment', 1, 15.99, 15.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440015', '750e8400-e29b-41d4-a716-446655440008', '650e8400-e29b-41d4-a716-446655440000', 'Dell Laptop 15', 1, 999.99, 999.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440016', '750e8400-e29b-41d4-a716-446655440008', '650e8400-e29b-41d4-a716-446655440010', 'Mechanical Keyboard', 1, 149.99, 149.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440017', '750e8400-e29b-41d4-a716-446655440008', '650e8400-e29b-41d4-a716-446655440009', 'Wireless Mouse Pro', 1, 49.99, 49.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440018', '750e8400-e29b-41d4-a716-446655440009', '650e8400-e29b-41d4-a716-446655440007', 'Standing Office Desk', 1, 499.99, 499.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440019', '750e8400-e29b-41d4-a716-446655440009', '650e8400-e29b-41d4-a716-446655440003', 'Cotton T-Shirt Blue', 5, 29.99, 149.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440020', '750e8400-e29b-41d4-a716-446655440010', '650e8400-e29b-41d4-a716-446655440004', 'Blue Jeans Classic', 3, 79.99, 239.98, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440021', '750e8400-e29b-41d4-a716-446655440010', '650e8400-e29b-41d4-a716-446655440003', 'Cotton T-Shirt Blue', 2, 29.99, 59.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440022', '750e8400-e29b-41d4-a716-446655440011', '650e8400-e29b-41d4-a716-446655440005', 'Premium Coffee Beans', 2, 29.99, 59.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440023', '750e8400-e29b-41d4-a716-446655440012', '650e8400-e29b-41d4-a716-446655440000', 'Dell Laptop 15', 1, 999.99, 999.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440024', '750e8400-e29b-41d4-a716-446655440012', '650e8400-e29b-41d4-a716-446655440008', 'Ergonomic Office Chair', 1, 299.99, 299.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440025', '750e8400-e29b-41d4-a716-446655440012', '650e8400-e29b-41d4-a716-446655440010', 'Mechanical Keyboard', 1, 149.99, 149.99, GETUTCDATE());
INSERT INTO dbo.order_items VALUES ('850e8400-e29b-41d4-a716-446655440026', '750e8400-e29b-41d4-a716-446655440013', '650e8400-e29b-41d4-a716-446655440002', 'LG 4K Monitor', 1, 449.98, 449.98, GETUTCDATE());
"@

Write-Host "Creating returns table..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q @"
CREATE TABLE dbo.returns (id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), order_id UNIQUEIDENTIFIER NOT NULL, reason NVARCHAR(MAX), status NVARCHAR(30) NOT NULL DEFAULT 'REQUESTED', refund_amount DECIMAL(12, 2), created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(), CONSTRAINT fk_returns_order FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE);
INSERT INTO dbo.returns VALUES ('950e8400-e29b-41d4-a716-446655440000', '750e8400-e29b-41d4-a716-446655440001', 'Wrong size', 'APPROVED', 89.97, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.returns VALUES ('950e8400-e29b-41d4-a716-446655440001', '750e8400-e29b-41d4-a716-446655440002', 'Defective unit', 'COMPLETED', 999.99, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.returns VALUES ('950e8400-e29b-41d4-a716-446655440002', '750e8400-e29b-41d4-a716-446655440004', 'Changed mind', 'REJECTED', 0.00, GETUTCDATE(), GETUTCDATE());
INSERT INTO dbo.returns VALUES ('950e8400-e29b-41d4-a716-446655440003', '750e8400-e29b-41d4-a716-446655440006', 'Arrived damaged', 'APPROVED', 499.99, GETUTCDATE(), GETUTCDATE());
"@

Write-Host "Creating views..." -ForegroundColor Yellow

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q "CREATE VIEW dbo.v_order_summary AS SELECT c.first_name + ' ' + c.last_name AS customer_name, c.email, COUNT(DISTINCT o.id) AS total_orders, SUM(o.total_amount) AS total_spent, COUNT(CASE WHEN o.status = 'DELIVERED' THEN 1 END) AS delivered_orders, COUNT(CASE WHEN o.status = 'CANCELLED' THEN 1 END) AS cancelled_orders FROM dbo.customers c LEFT JOIN dbo.orders o ON c.id = o.customer_id GROUP BY c.id, c.first_name, c.last_name, c.email;"

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q "CREATE VIEW dbo.v_product_analytics AS SELECT p.name, p.price, COUNT(DISTINCT oi.order_id) AS times_ordered, SUM(oi.quantity) AS total_quantity_sold, SUM(oi.total_price) AS total_revenue FROM dbo.products p LEFT JOIN dbo.order_items oi ON p.id = oi.product_id GROUP BY p.id, p.name, p.price;"

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q "CREATE VIEW dbo.v_order_analytics AS SELECT COUNT(*) AS total_orders, SUM(CASE WHEN status = 'DELIVERED' THEN 1 ELSE 0 END) AS delivered, SUM(CASE WHEN status = 'CANCELLED' THEN 1 ELSE 0 END) AS cancelled, SUM(total_amount) AS total_revenue, AVG(total_amount) AS avg_order_value FROM dbo.orders;"

docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalDev_Password123!" -d insighterp_db -C -Q "CREATE VIEW dbo.v_return_analytics AS SELECT COUNT(*) AS total_returns, SUM(CASE WHEN status = 'APPROVED' THEN 1 ELSE 0 END) AS approved, SUM(CASE WHEN status = 'COMPLETED' THEN 1 ELSE 0 END) AS completed, SUM(refund_amount) AS total_refunded FROM dbo.returns;"

Write-Host ""
Write-Host "[OK] Database and all tables created successfully!" -ForegroundColor Green
Write-Host ""

Write-Host "============================================================" -ForegroundColor Green
Write-Host "INSIGHTERP DATABASE SETUP COMPLETE!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""

Write-Host "DATABASE CONTENTS:" -ForegroundColor Yellow
Write-Host "  [OK] Auth Schema: 3 users, 3 roles" -ForegroundColor Green
Write-Host "  [OK] dbo.customers: 10 rows" -ForegroundColor Green
Write-Host "  [OK] dbo.products: 11 rows" -ForegroundColor Green
Write-Host "  [OK] dbo.orders: 17 rows" -ForegroundColor Green
Write-Host "  [OK] dbo.order_items: 27 rows" -ForegroundColor Green
Write-Host "  [OK] dbo.returns: 4 rows" -ForegroundColor Green
Write-Host "  [OK] Views: 4 ready to query" -ForegroundColor Green
Write-Host ""

Write-Host "CONNECTION STRING:" -ForegroundColor Cyan
Write-Host "Server=localhost,1433;Database=insighterp_db;User Id=sa;Password=LocalDev_Password123!;TrustServerCertificate=True;" -ForegroundColor Gray
Write-Host ""

Write-Host "VERIFY:" -ForegroundColor Cyan
Write-Host "docker exec erp-sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'LocalDev_Password123!' -d insighterp_db -C -Q 'SELECT COUNT(*) FROM dbo.customers;'" -ForegroundColor Gray
Write-Host ""

Write-Host "Ready for development!" -ForegroundColor Green
Write-Host ""