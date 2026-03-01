CREATE TABLE IF NOT EXISTS churn_predictions (
  id               CHAR(36)      NOT NULL,
  customer_id      CHAR(36)      NOT NULL COMMENT 'References customer_db.customers.id',
  churn_probability DECIMAL(5,4) NOT NULL COMMENT '0.0 to 1.0',
  churn_risk_label VARCHAR(20)   NOT NULL COMMENT 'LOW, MEDIUM, HIGH',
  model_version    VARCHAR(50)   NOT NULL,
  predicted_at     TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_churn_customer (customer_id),
  INDEX idx_churn_risk     (churn_risk_label)
);

CREATE TABLE IF NOT EXISTS churn_factors (
  id                  CHAR(36)       NOT NULL,
  churn_prediction_id CHAR(36)       NOT NULL,
  factor_name         VARCHAR(100)   NOT NULL,
  factor_weight       DECIMAL(5, 4)  NOT NULL COMMENT 'Feature importance score',
  PRIMARY KEY (id),
  CONSTRAINT fk_cf_prediction FOREIGN KEY (churn_prediction_id) REFERENCES churn_predictions(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS segmentation_runs (
  id            CHAR(36)     NOT NULL,
  run_by        CHAR(36)     COMMENT 'References auth_db.users.id',
  model_name    VARCHAR(100) NOT NULL,
  num_segments  INT          NOT NULL,
  status        VARCHAR(20)  NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING, RUNNING, COMPLETED, FAILED',
  started_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  completed_at  TIMESTAMP,
  error_message TEXT,
  PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS customer_segmentation_results (
  id                  CHAR(36)    NOT NULL,
  segmentation_run_id CHAR(36)    NOT NULL,
  customer_id         CHAR(36)    NOT NULL COMMENT 'References customer_db.customers.id',
  segment_label       VARCHAR(100) NOT NULL,
  confidence_score    DECIMAL(5, 4),
  assigned_at         TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_seg_customer (customer_id),
  CONSTRAINT fk_seg_run FOREIGN KEY (segmentation_run_id) REFERENCES segmentation_runs(id) ON DELETE CASCADE
);
