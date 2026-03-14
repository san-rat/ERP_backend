-- 1. Create the tables (T-SQL Version)
IF OBJECT_ID('forecast_runs', 'U') IS NULL
BEGIN
    CREATE TABLE forecast_runs (
        id             UNIQUEIDENTIFIER NOT NULL, -- CHAR(36) is usually UNIQUEIDENTIFIER in SQL Server
        run_by         UNIQUEIDENTIFIER,         -- References auth_db.users.id
        model_name     NVARCHAR(100)    NOT NULL,
        forecast_type  NVARCHAR(50)     NOT NULL, -- COMMENT: 'DEMAND, REVENUE, INVENTORY'
        status         NVARCHAR(20)     NOT NULL DEFAULT 'PENDING', -- COMMENT: 'PENDING, RUNNING, COMPLETED, FAILED'
        started_at     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        completed_at   DATETIME2,
        error_message  NVARCHAR(MAX),
        PRIMARY KEY (id)
    );
END

IF OBJECT_ID('forecast_results', 'U') IS NULL
BEGIN
    CREATE TABLE forecast_results (
        id               UNIQUEIDENTIFIER NOT NULL,
        forecast_run_id  UNIQUEIDENTIFIER NOT NULL,
        entity_id        UNIQUEIDENTIFIER,
        entity_type      NVARCHAR(50)     NOT NULL, -- COMMENT: 'PRODUCT, CUSTOMER, GLOBAL'
        forecast_date    DATE             NOT NULL,
        predicted_value  DECIMAL(15, 4)   NOT NULL,
        lower_bound      DECIMAL(15, 4),
        upper_bound      DECIMAL(15, 4),
        confidence_score DECIMAL(5, 4),             -- COMMENT: '0.0 to 1.0'
        created_at       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        PRIMARY KEY (id),
        CONSTRAINT fk_fr_run FOREIGN KEY (forecast_run_id) REFERENCES forecast_runs(id) ON DELETE CASCADE
    );
END

-- 2. Create Indexes separately (SQL Server style)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_fr_run' AND object_id = OBJECT_ID('forecast_results'))
BEGIN
    CREATE INDEX idx_fr_run ON forecast_results (forecast_run_id);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_fr_entity' AND object_id = OBJECT_ID('forecast_results'))
BEGIN
    CREATE INDEX idx_fr_entity ON forecast_results (entity_id, entity_type);
END

IF OBJECT_ID('forecast_actuals', 'U') IS NULL
BEGIN
    CREATE TABLE forecast_actuals (
        id           UNIQUEIDENTIFIER NOT NULL,
        entity_id    UNIQUEIDENTIFIER NOT NULL,
        entity_type  NVARCHAR(50)     NOT NULL,
        actual_date  DATE             NOT NULL,
        actual_value DECIMAL(15, 4)   NOT NULL,
        created_at   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        PRIMARY KEY (id),
        CONSTRAINT uq_actual UNIQUE (entity_id, entity_type, actual_date)
    );
END