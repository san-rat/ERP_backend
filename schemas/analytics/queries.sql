-- Check all tables exist and have data
SELECT 'Customers' as [Table], COUNT(*) as [Count] FROM dbo.customers
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.products
UNION ALL SELECT 'Orders', COUNT(*) FROM dbo.orders
UNION ALL SELECT 'Order Items', COUNT(*) FROM dbo.order_items
UNION ALL SELECT 'Returns', COUNT(*) FROM dbo.returns
UNION ALL SELECT 'ML Features', COUNT(*) FROM ml.v_customer_features_for_prediction;

-- ============================================
-- CUSTOMER ANALYTICS
-- ============================================

-- Top customers by spending
SELECT TOP 10
    c.id,
    c.email,
    c.first_name,
    cf.total_spent,
    cf.total_orders,
    cf.avg_order_value
FROM ml.v_customer_features_for_prediction cf
JOIN dbo.customers c ON cf.customer_id = c.id
ORDER BY cf.total_spent DESC;

-- Customer lifetime by months
SELECT 
    DATEDIFF(MONTH, c.created_at, GETUTCDATE()) as months_as_customer,
    COUNT(*) as customer_count,
    AVG(cf.total_orders) as avg_orders,
    AVG(cf.total_spent) as avg_spent
FROM dbo.customers c
JOIN ml.v_customer_features_for_prediction cf ON c.id = cf.customer_id
GROUP BY DATEDIFF(MONTH, c.created_at, GETUTCDATE())
ORDER BY months_as_customer DESC;

-- High-risk customers
SELECT 
    c.email,
    cf.days_since_last_order,
    cf.return_rate,
    cf.cancellation_rate,
    cf.inactive_flag
FROM ml.v_customer_features_for_prediction cf
JOIN dbo.customers c ON cf.customer_id = c.id
WHERE cf.days_since_last_order > 180
   OR cf.return_rate > 0.5
   OR cf.cancellation_rate > 0.3;

-- ============================================
-- PRODUCT ANALYTICS
-- ============================================

-- Product performance analysis
SELECT 
    p.name,
    p.price,
    COUNT(DISTINCT oi.order_id) as purchase_count,
    SUM(oi.quantity) as total_quantity,
    SUM(oi.total_price) as total_revenue
FROM dbo.products p
LEFT JOIN dbo.order_items oi ON p.id = oi.product_id
GROUP BY p.id, p.name, p.price
ORDER BY total_revenue DESC;

-- Products by category
SELECT 
    p.category_id,
    COUNT(DISTINCT oi.order_id) as num_orders,
    SUM(oi.quantity) as total_units,
    SUM(oi.total_price) as total_revenue,
    ROUND(AVG(p.price), 2) as avg_price
FROM dbo.products p
LEFT JOIN dbo.order_items oi ON p.id = oi.product_id
GROUP BY p.category_id
ORDER BY total_revenue DESC;

-- ============================================
-- ORDER ANALYTICS
-- ============================================

-- Order success rate
SELECT 
    COUNT(CASE WHEN status = 'DELIVERED' THEN 1 END) as delivered_orders,
    COUNT(CASE WHEN status = 'CANCELLED' THEN 1 END) as cancelled_orders,
    COUNT(*) as total_orders,
    ROUND(CAST(COUNT(CASE WHEN status = 'DELIVERED' THEN 1 END) AS FLOAT) / COUNT(*) * 100, 2) as success_rate_percent
FROM dbo.orders;

-- Orders by month
SELECT 
    YEAR(o.created_at) as year,
    MONTH(o.created_at) as month,
    COUNT(*) as order_count,
    SUM(o.total_amount) as total_revenue,
    AVG(o.total_amount) as avg_order_value
FROM dbo.orders o
WHERE o.status = 'DELIVERED'
GROUP BY YEAR(o.created_at), MONTH(o.created_at)
ORDER BY year DESC, month DESC;

-- ============================================
-- RETURN ANALYTICS
-- ============================================

-- Return analysis
SELECT 
    COUNT(*) as total_returns,
    SUM(CASE WHEN status = 'APPROVED' THEN 1 ELSE 0 END) as approved_returns,
    SUM(CASE WHEN status = 'COMPLETED' THEN 1 ELSE 0 END) as completed_returns,
    COALESCE(SUM(refund_amount), 0) as total_refunded
FROM dbo.returns;

-- Return rate by customer
SELECT TOP 10
    c.email,
    COUNT(r.id) as return_count,
    COUNT(DISTINCT o.id) as order_count,
    ROUND(CAST(COUNT(r.id) AS FLOAT) / COUNT(DISTINCT o.id) * 100, 2) as return_rate_percent
FROM dbo.customers c
LEFT JOIN dbo.orders o ON c.id = o.customer_id
LEFT JOIN dbo.returns r ON o.id = r.order_id AND r.status IN ('APPROVED', 'COMPLETED')
GROUP BY c.id, c.email
HAVING COUNT(r.id) > 0
ORDER BY return_count DESC;

-- ============================================
-- ML ANALYTICS (For Prediction Microservice)
-- ============================================

-- Feature statistics
SELECT 
    COUNT(*) as total_customers,
    AVG(CAST(days_since_last_order AS FLOAT)) as avg_days_inactive,
    AVG(CAST(total_orders AS FLOAT)) as avg_orders,
    AVG(total_spent) as avg_customer_value,
    SUM(CASE WHEN inactive_flag = 1 THEN 1 ELSE 0 END) as inactive_customers
FROM ml.v_customer_features_for_prediction;

-- Feature distribution
SELECT 
    customer_id,
    days_since_last_order,
    total_orders,
    total_spent,
    return_rate,
    inactive_flag
FROM ml.v_customer_features_for_prediction
ORDER BY inactive_flag DESC, days_since_last_order DESC;

-- ============================================
-- DATA QUALITY CHECKS
-- ============================================

-- Check referential integrity
SELECT 
    'Order without customer' as issue,
    COUNT(*) as count
FROM dbo.orders o
WHERE NOT EXISTS (SELECT 1 FROM dbo.customers c WHERE c.id = o.customer_id)
UNION ALL
SELECT 'Order Item without order', COUNT(*)
FROM dbo.order_items oi
WHERE NOT EXISTS (SELECT 1 FROM dbo.orders o WHERE o.id = oi.order_id)
UNION ALL
SELECT 'Order Item without product', COUNT(*)
FROM dbo.order_items oi
WHERE NOT EXISTS (SELECT 1 FROM dbo.products p WHERE p.id = oi.product_id)
UNION ALL
SELECT 'Return without order', COUNT(*)
FROM dbo.returns r
WHERE NOT EXISTS (SELECT 1 FROM dbo.orders o WHERE o.id = r.order_id);

-- Data quality summary
SELECT 
    'Customers' as table_name,
    COUNT(*) as row_count,
    SUM(CASE WHEN email IS NULL THEN 1 ELSE 0 END) as null_count
FROM dbo.customers
UNION ALL
SELECT 'Orders', COUNT(*), SUM(CASE WHEN customer_id IS NULL THEN 1 ELSE 0 END)
FROM dbo.orders
UNION ALL
SELECT 'Products', COUNT(*), SUM(CASE WHEN name IS NULL THEN 1 ELSE 0 END)
FROM dbo.products;