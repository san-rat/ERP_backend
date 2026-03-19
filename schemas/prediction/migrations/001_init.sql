-- Create ML schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ml')
BEGIN
    EXEC sp_executesql N'CREATE SCHEMA ml';
END;
GO

CREATE TABLE ml.churn_predictions (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    customer_id UNIQUEIDENTIFIER NOT NULL,
    churn_probability DECIMAL(5, 4) NOT NULL,
    churn_risk_label NVARCHAR(20) NOT NULL,
    model_version NVARCHAR(20) NOT NULL,
    predicted_at DATETIME2 NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT chk_probability CHECK (churn_probability >= 0 AND churn_probability <= 1),
    CONSTRAINT chk_risk_label CHECK (churn_risk_label IN ('LOW', 'MEDIUM', 'HIGH')),
    CONSTRAINT fk_predictions_customer FOREIGN KEY (customer_id) REFERENCES dbo.customers(id) ON DELETE CASCADE
);

CREATE TABLE ml.churn_factors (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    churn_prediction_id UNIQUEIDENTIFIER NOT NULL,
    factor_name NVARCHAR(255) NOT NULL,
    factor_weight DECIMAL(5, 4),
    feature_value NVARCHAR(MAX),
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT fk_factors_prediction FOREIGN KEY (churn_prediction_id) REFERENCES ml.churn_predictions(id) ON DELETE CASCADE
);

CREATE TABLE ml.model_versions (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    model_version NVARCHAR(20) NOT NULL UNIQUE,
    algorithm NVARCHAR(100) NOT NULL,
    training_date DATETIME2 NOT NULL,
    training_data_count INT,
    accuracy DECIMAL(5, 4),
    precision DECIMAL(5, 4),
    recall DECIMAL(5, 4),
    auc_roc DECIMAL(5, 4),
    is_active BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE ml.training_history (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    model_version_id UNIQUEIDENTIFIER NOT NULL,
    training_start_time DATETIME2 NOT NULL,
    training_end_time DATETIME2,
    training_status NVARCHAR(30) NOT NULL,
    total_records_used INT,
    churned_count INT,
    non_churned_count INT,
    error_message NVARCHAR(MAX),
    triggered_by NVARCHAR(255),
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT fk_history_modelversion FOREIGN KEY (model_version_id) REFERENCES ml.model_versions(id) ON DELETE CASCADE
);
