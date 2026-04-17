-- Migration 001: Create ML schema, tables, indexes, and feature views

-- Create ML schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ml')
BEGIN
    EXEC sp_executesql N'CREATE SCHEMA ml';
END;
GO

-- ============================================
-- TABLES
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ml].[churn_predictions]') AND type = N'U')
BEGIN
    CREATE TABLE ml.churn_predictions (
        id                UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWID(),
        customer_id       UNIQUEIDENTIFIER  NOT NULL,
        churn_probability DECIMAL(5, 4)     NOT NULL,
        churn_risk_label  NVARCHAR(20)      NOT NULL,
        model_version     NVARCHAR(50)      NOT NULL,
        predicted_at      DATETIME2         NOT NULL,
        created_at        DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT chk_probability  CHECK (churn_probability >= 0 AND churn_probability <= 1),
        CONSTRAINT chk_risk_label CHECK (churn_risk_label IN ('LOW', 'MEDIUM', 'HIGH', 'UNKNOWN', 'ERROR')),
        CONSTRAINT fk_predictions_customer
            FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ml].[churn_factors]') AND type = N'U')
BEGIN
    CREATE TABLE ml.churn_factors (
        id                    UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWID(),
        churn_prediction_id   UNIQUEIDENTIFIER  NOT NULL,
        factor_name           NVARCHAR(255)     NOT NULL,
        factor_weight         DECIMAL(10, 6),
        feature_value         NVARCHAR(MAX),
        created_at            DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT fk_factors_prediction
            FOREIGN KEY (churn_prediction_id) REFERENCES ml.churn_predictions(id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ml].[model_versions]') AND type = N'U')
BEGIN
    CREATE TABLE ml.model_versions (
        id                   UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWID(),
        model_version        NVARCHAR(50)      NOT NULL UNIQUE,
        algorithm            NVARCHAR(100)     NOT NULL,
        training_date        DATETIME2         NOT NULL,
        training_data_count  INT,
        accuracy             DECIMAL(10, 6),
        precision            DECIMAL(10, 6),
        recall               DECIMAL(10, 6),
        auc_roc              DECIMAL(10, 6),
        total_features       INT,
        is_active            BIT               NOT NULL DEFAULT 0,
        created_at           DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
        updated_at           DATETIME2         NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ml].[training_history]') AND type = N'U')
BEGIN
    CREATE TABLE ml.training_history (
        id                   UNIQUEIDENTIFIER  PRIMARY KEY DEFAULT NEWID(),
        -- NULL is intentional: history is created before a model version exists
        model_version_id     UNIQUEIDENTIFIER  NULL,
        training_start_time  DATETIME2         NOT NULL,
        training_end_time    DATETIME2,
        training_status      NVARCHAR(30)      NOT NULL,
        total_records_used   INT,
        churned_count        INT,
        non_churned_count    INT,
        error_message        NVARCHAR(MAX),
        triggered_by         NVARCHAR(255),
        created_at           DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT fk_history_modelversion
            FOREIGN KEY (model_version_id) REFERENCES ml.model_versions(id)
    );
END
GO

-- ============================================
-- SAFE COLUMN ADDITIONS (idempotent upgrades)
-- Handles existing DBs that were created with
-- the old schema before this migration was fixed
-- ============================================

-- Allow model_version_id to be NULL on existing DBs
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ml].[training_history]')
      AND name = 'model_version_id'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE ml.training_history ALTER COLUMN model_version_id UNIQUEIDENTIFIER NULL;
END
GO

-- Add total_features if missing
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ml].[model_versions]')
      AND name = 'total_features'
)
BEGIN
    ALTER TABLE ml.model_versions ADD total_features INT NULL;
END
GO

-- Widen model_version column if it was created as NVARCHAR(20)
-- C# generates versions like 'v20250416_123456' which can exceed 20 chars
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ml].[model_versions]')
      AND name = 'model_version'
      AND max_length < 100
)
BEGIN
    ALTER TABLE ml.model_versions ALTER COLUMN model_version NVARCHAR(50) NOT NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ml].[churn_predictions]')
      AND name = 'model_version'
      AND max_length < 100
)
BEGIN
    ALTER TABLE ml.churn_predictions ALTER COLUMN model_version NVARCHAR(50) NOT NULL;
END
GO

-- Widen factor_weight if it was DECIMAL(5,4) — values can exceed 1.0
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ml].[churn_factors]')
      AND name = 'factor_weight'
      AND precision = 5
)
BEGIN
    ALTER TABLE ml.churn_factors ALTER COLUMN factor_weight DECIMAL(10, 6) NULL;
END
GO

-- Widen accuracy/precision/recall/auc_roc on model_versions if DECIMAL(5,4)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ml].[model_versions]')
      AND name = 'accuracy'
      AND precision = 5
)
BEGIN
    ALTER TABLE ml.model_versions ALTER COLUMN accuracy   DECIMAL(10, 6) NULL;
    ALTER TABLE ml.model_versions ALTER COLUMN precision  DECIMAL(10, 6) NULL;
    ALTER TABLE ml.model_versions ALTER COLUMN recall     DECIMAL(10, 6) NULL;
    ALTER TABLE ml.model_versions ALTER COLUMN auc_roc    DECIMAL(10, 6) NULL;
END
GO

-- ============================================
-- INDEXES
-- ============================================

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[churn_predictions]') AND name = 'idx_predictions_customer')
    CREATE INDEX idx_predictions_customer ON ml.churn_predictions(customer_id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[churn_predictions]') AND name = 'idx_predictions_date')
    CREATE INDEX idx_predictions_date ON ml.churn_predictions(predicted_at);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[churn_predictions]') AND name = 'idx_predictions_risk')
    CREATE INDEX idx_predictions_risk ON ml.churn_predictions(churn_risk_label);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[churn_factors]') AND name = 'idx_factors_prediction')
    CREATE INDEX idx_factors_prediction ON ml.churn_factors(churn_prediction_id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[model_versions]') AND name = 'idx_modelversions_active')
    CREATE INDEX idx_modelversions_active ON ml.model_versions(is_active);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[model_versions]') AND name = 'idx_modelversions_date')
    CREATE INDEX idx_modelversions_date ON ml.model_versions(training_date);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[training_history]') AND name = 'idx_history_modelversion')
    CREATE INDEX idx_history_modelversion ON ml.training_history(model_version_id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[training_history]') AND name = 'idx_history_status')
    CREATE INDEX idx_history_status ON ml.training_history(training_status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ml].[training_history]') AND name = 'idx_history_starttime')
    CREATE INDEX idx_history_starttime ON ml.training_history(training_start_time);
GO

-- ============================================
-- VIEWS
-- ============================================

-- 1. RFM METRICS
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[ml].[v_customer_rfm]'))
BEGIN
    EXEC sp_executesql N'
    CREATE VIEW ml.v_customer_rfm AS
    SELECT
        c.id AS customer_id,
        COALESCE(DATEDIFF(DAY, MAX(o.created_at), GETUTCDATE()), 999) AS days_since_last_order,
        COUNT(DISTINCT o.id)            AS total_orders,
        COALESCE(SUM(o.total_amount), 0)  AS total_spent,
        COALESCE(AVG(o.total_amount), 0)  AS avg_order_value,
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
        COUNT(DISTINCT oi.product_id)        AS unique_products_purchased,
        COUNT(DISTINCT p.category_id)        AS unique_categories_purchased,
        CASE
            WHEN COUNT(DISTINCT o.id) = 0 THEN 0
            ELSE CAST(COUNT(oi.id) AS DECIMAL(10,4)) / COUNT(DISTINCT o.id)
        END AS avg_products_per_order
    FROM dbo.customers c
    LEFT JOIN dbo.orders o      ON c.id = o.customer_id AND o.status != ''CANCELLED''
    LEFT JOIN dbo.order_items oi ON o.id = oi.order_id
    LEFT JOIN dbo.products p    ON oi.product_id = p.id
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
    LEFT JOIN dbo.orders o  ON c.id = o.customer_id
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
        SUM(CASE WHEN o.status = ''DELIVERED''  THEN 1 ELSE 0 END) AS completed_orders,
        SUM(CASE WHEN o.status = ''CANCELLED''  THEN 1 ELSE 0 END) AS cancelled_orders,
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
        DATEDIFF(DAY, c.created_at, GETUTCDATE())  AS account_age_days,
        DATEDIFF(DAY, c.updated_at, GETUTCDATE())  AS days_since_last_update,
        CASE
            WHEN DATEDIFF(DAY, c.updated_at, GETUTCDATE()) > 180 THEN 1
            ELSE 0
        END AS inactive_flag
    FROM dbo.customers c;';
END
GO

-- 6. MAIN FEATURE VIEW (drop and recreate to pick up all sub-views)
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