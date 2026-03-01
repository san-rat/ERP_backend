CREATE TABLE IF NOT EXISTS forecast_runs (
  id           CHAR(36)    NOT NULL,
  run_by       CHAR(36)    COMMENT 'References auth_db.users.id',
  model_name   VARCHAR(100) NOT NULL,
  forecast_type VARCHAR(50) NOT NULL COMMENT 'DEMAND, REVENUE, INVENTORY',
  status       VARCHAR(20) NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING, RUNNING, COMPLETED, FAILED',
  started_at   TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  completed_at TIMESTAMP,
  error_message TEXT,
  PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS forecast_results (
  id               CHAR(36)       NOT NULL,
  forecast_run_id  CHAR(36)       NOT NULL,
  entity_id        CHAR(36)       COMMENT 'product_id or customer_id depending on forecast type',
  entity_type      VARCHAR(50)    NOT NULL COMMENT 'PRODUCT, CUSTOMER, GLOBAL',
  forecast_date    DATE           NOT NULL,
  predicted_value  DECIMAL(15, 4) NOT NULL,
  lower_bound      DECIMAL(15, 4),
  upper_bound      DECIMAL(15, 4),
  confidence_score DECIMAL(5, 4)  COMMENT '0.0 to 1.0',
  created_at       TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_fr_run    (forecast_run_id),
  INDEX idx_fr_entity (entity_id, entity_type),
  CONSTRAINT fk_fr_run FOREIGN KEY (forecast_run_id) REFERENCES forecast_runs(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS forecast_actuals (
  id           CHAR(36)       NOT NULL,
  entity_id    CHAR(36)       NOT NULL,
  entity_type  VARCHAR(50)    NOT NULL,
  actual_date  DATE           NOT NULL,
  actual_value DECIMAL(15, 4) NOT NULL,
  created_at   TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_actual (entity_id, entity_type, actual_date)
);
