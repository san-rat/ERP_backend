-- Migration 002: Create ML sub-views required by v_customer_features_for_prediction
-- These 5 sub-views must exist before the main prediction view works.

-- 1. RFM METRICS
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_rfm]'))
BEGIN
    EXEC sp_executesql N'
    CREATE VIEW ml.v_customer_rfm AS
    SELECT
        c.id AS customer_id,
        COALESCE(DATEDIFF(DAY, MAX(o.created_at), GETUTCDATE()), 999) AS days_since_last_order,
        COUNT(DISTINCT o.id) AS total_orders,
        COALESCE(SUM(o.total_amount), 0) AS total_spent,
        COALESCE(AVG(o.total_amount), 0) AS avg_order_value,
        DATEDIFF(DAY, c.created_at, GETUTCDATE()) AS customer_tenure_days
    FROM dbo.customers c
    LEFT JOIN dbo.orders o ON c.id = o.customer_id AND o.status != ''CANCELLED''
    GROUP BY c.id, c.created_at;';
END
GO

-- 2. PRODUCT DIVERSITY
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_product_diversity]'))
BEGIN
    EXEC sp_executesql N'
    CREATE VIEW ml.v_customer_product_diversity AS
    SELECT
        c.id AS customer_id,
        COUNT(DISTINCT oi.product_id) AS unique_products_purchased,
        COUNT(DISTINCT p.category_id) AS unique_categories_purchased,
        CASE
            WHEN COUNT(DISTINCT o.id) = 0 THEN 0
            ELSE CAST(COUNT(oi.id) AS DECIMAL(10,4)) / COUNT(DISTINCT o.id)
        END AS avg_products_per_order
    FROM dbo.customers c
    LEFT JOIN dbo.orders o ON c.id = o.customer_id AND o.status != ''CANCELLED''
    LEFT JOIN dbo.order_items oi ON o.id = oi.order_id
    LEFT JOIN dbo.products p ON oi.product_id = p.id
    GROUP BY c.id;';
END
GO

-- 3. RETURN BEHAVIOR
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_return_behavior]'))
BEGIN
    EXEC sp_executesql N'
    CREATE VIEW ml.v_customer_return_behavior AS
    SELECT
        c.id AS customer_id,
        COUNT(DISTINCT r.id) AS total_returns,
        CASE
            WHEN COUNT(DISTINCT o.id) = 0 THEN 0
            ELSE CAST(COUNT(DISTINCT r.id) AS DECIMAL(10,4)) / COUNT(DISTINCT o.id)
        END AS return_rate,
        COALESCE(SUM(r.refund_amount), 0) AS total_refunded
    FROM dbo.customers c
    LEFT JOIN dbo.orders o ON c.id = o.customer_id
    LEFT JOIN dbo.returns r ON o.id = r.order_id AND r.status IN (''APPROVED'', ''COMPLETED'')
    GROUP BY c.id;';
END
GO

-- 4. ORDER STATUS
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_order_status]'))
BEGIN
    EXEC sp_executesql N'
    CREATE VIEW ml.v_customer_order_status AS
    SELECT
        c.id AS customer_id,
        SUM(CASE WHEN o.status = ''DELIVERED'' THEN 1 ELSE 0 END) AS completed_orders,
        SUM(CASE WHEN o.status = ''CANCELLED'' THEN 1 ELSE 0 END) AS cancelled_orders,
        CASE
            WHEN COUNT(o.id) = 0 THEN 0
            ELSE CAST(SUM(CASE WHEN o.status = ''CANCELLED'' THEN 1 ELSE 0 END) AS DECIMAL(10,4)) / COUNT(o.id)
        END AS cancellation_rate
    FROM dbo.customers c
    LEFT JOIN dbo.orders o ON c.id = o.customer_id
    GROUP BY c.id;';
END
GO

-- 5. ENGAGEMENT
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_engagement]'))
BEGIN
    EXEC sp_executesql N'
    CREATE VIEW ml.v_customer_engagement AS
    SELECT
        c.id AS customer_id,
        DATEDIFF(DAY, c.created_at, GETUTCDATE()) AS account_age_days,
        DATEDIFF(DAY, c.updated_at, GETUTCDATE()) AS days_since_last_update,
        CASE
            WHEN DATEDIFF(DAY, c.updated_at, GETUTCDATE()) > 180 THEN 1
            ELSE 0
        END AS inactive_flag
    FROM dbo.customers c;';
END
GO

-- 6. Drop and recreate the main feature view to ensure it picks up all sub-views
IF EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_features_for_prediction]'))
BEGIN
    DROP VIEW ml.v_customer_features_for_prediction;
END
GO

CREATE VIEW ml.v_customer_features_for_prediction AS
SELECT
    ISNULL(rfm.customer_id, ISNULL(pd.customer_id, ISNULL(rb.customer_id,
        ISNULL(os.customer_id, eg.customer_id))))  AS customer_id,
    ISNULL(rfm.days_since_last_order, 999)         AS days_since_last_order,
    ISNULL(rfm.total_orders, 0)                    AS total_orders,
    ISNULL(rfm.total_spent, 0)                     AS total_spent,
    ISNULL(rfm.avg_order_value, 0)                 AS avg_order_value,
    ISNULL(rfm.customer_tenure_days, 0)            AS customer_tenure_days,
    ISNULL(pd.unique_products_purchased, 0)        AS unique_products_purchased,
    ISNULL(pd.unique_categories_purchased, 0)      AS unique_categories_purchased,
    ISNULL(pd.avg_products_per_order, 0)           AS avg_products_per_order,
    ISNULL(rb.total_returns, 0)                    AS total_returns,
    ISNULL(rb.return_rate, 0)                      AS return_rate,
    ISNULL(rb.total_refunded, 0)                   AS total_refunded,
    ISNULL(os.completed_orders, 0)                 AS completed_orders,
    ISNULL(os.cancelled_orders, 0)                 AS cancelled_orders,
    ISNULL(os.cancellation_rate, 0)                AS cancellation_rate,
    ISNULL(eg.account_age_days, 0)                 AS account_age_days,
    ISNULL(eg.days_since_last_update, 999)         AS days_since_last_activity,
    ISNULL(eg.inactive_flag, 0)                    AS inactive_flag
FROM ml.v_customer_rfm rfm
FULL OUTER JOIN ml.v_customer_product_diversity pd ON rfm.customer_id = pd.customer_id
FULL OUTER JOIN ml.v_customer_return_behavior   rb ON rfm.customer_id = rb.customer_id
FULL OUTER JOIN ml.v_customer_order_status      os ON rfm.customer_id = os.customer_id
FULL OUTER JOIN ml.v_customer_engagement        eg ON rfm.customer_id = eg.customer_id
WHERE ISNULL(rfm.customer_id, ISNULL(pd.customer_id, ISNULL(rb.customer_id,
    ISNULL(os.customer_id, eg.customer_id)))) IS NOT NULL;
GO

-- Verify: should return a count equal to your total customers
SELECT COUNT(*) AS total_customers_in_view FROM ml.v_customer_features_for_prediction;
GO