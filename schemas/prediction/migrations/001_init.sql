-- 1. Create churn_predictions Table
IF OBJECT_ID('churn_predictions', 'U') IS NULL
BEGIN
    CREATE TABLE churn_predictions (
        id                UNIQUEIDENTIFIER NOT NULL,
        customer_id       UNIQUEIDENTIFIER NOT NULL, -- References customer_db.customers.id
        churn_probability DECIMAL(5, 4)    NOT NULL, -- 0.0 to 1.0
        churn_risk_label  NVARCHAR(20)     NOT NULL, -- LOW, MEDIUM, HIGH
        model_version     NVARCHAR(50)     NOT NULL,
        predicted_at      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        PRIMARY KEY (id)
    );
END

-- Create Indexes for churn_predictions
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_churn_customer' AND object_id = OBJECT_ID('churn_predictions'))
    CREATE INDEX idx_churn_customer ON churn_predictions (customer_id);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_churn_risk' AND object_id = OBJECT_ID('churn_predictions'))
    CREATE INDEX idx_churn_risk ON churn_predictions (churn_risk_label);


-- 2. Create churn_factors Table
IF OBJECT_ID('churn_factors', 'U') IS NULL
BEGIN
    CREATE TABLE churn_factors (
        id                  UNIQUEIDENTIFIER NOT NULL,
        churn_prediction_id UNIQUEIDENTIFIER NOT NULL,
        factor_name         NVARCHAR(100)    NOT NULL,
        factor_weight       DECIMAL(5, 4)    NOT NULL, -- Feature importance score
        PRIMARY KEY (id),
        CONSTRAINT fk_cf_prediction FOREIGN KEY (churn_prediction_id) REFERENCES churn_predictions(id) ON DELETE CASCADE
    );
END


-- 3. Create segmentation_runs Table
IF OBJECT_ID('segmentation_runs', 'U') IS NULL
BEGIN
    CREATE TABLE segmentation_runs (
        id             UNIQUEIDENTIFIER NOT NULL,
        run_by         UNIQUEIDENTIFIER,         -- References auth_db.users.id
        model_name     NVARCHAR(100)    NOT NULL,
        num_segments   INT              NOT NULL,
        status         NVARCHAR(20)     NOT NULL DEFAULT 'PENDING', -- PENDING, RUNNING, COMPLETED, FAILED
        started_at     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        completed_at   DATETIME2,
        error_message  NVARCHAR(MAX),
        PRIMARY KEY (id)
    );
END


-- 4. Create customer_segmentation_results Table
IF OBJECT_ID('customer_segmentation_results', 'U') IS NULL
BEGIN
    CREATE TABLE customer_segmentation_results (
        id                  UNIQUEIDENTIFIER NOT NULL,
        segmentation_run_id UNIQUEIDENTIFIER NOT NULL,
        customer_id         UNIQUEIDENTIFIER NOT NULL, -- References customer_db.customers.id
        segment_label       NVARCHAR(100)    NOT NULL,
        confidence_score    DECIMAL(5, 4),
        assigned_at         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        PRIMARY KEY (id),
        CONSTRAINT fk_seg_run FOREIGN KEY (segmentation_run_id) REFERENCES segmentation_runs(id) ON DELETE CASCADE
    );
END

-- Create Index for customer_segmentation_results
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_seg_customer' AND object_id = OBJECT_ID('customer_segmentation_results'))
    CREATE INDEX idx_seg_customer ON customer_segmentation_results (customer_id);