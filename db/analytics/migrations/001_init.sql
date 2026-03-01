CREATE TABLE IF NOT EXISTS kpi_snapshots (
  id           CHAR(36)       NOT NULL,
  kpi_name     VARCHAR(100)   NOT NULL,
  kpi_value    DECIMAL(18, 4) NOT NULL,
  kpi_unit     VARCHAR(50)    COMMENT 'e.g. USD, COUNT, PERCENT',
  snapshot_date DATE          NOT NULL,
  created_at   TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_kpi_name (kpi_name),
  INDEX idx_kpi_date (snapshot_date)
);

CREATE TABLE IF NOT EXISTS sales_summaries (
  id              CHAR(36)       NOT NULL,
  summary_date    DATE           NOT NULL,
  total_orders    INT            NOT NULL DEFAULT 0,
  total_revenue   DECIMAL(15, 2) NOT NULL DEFAULT 0.00,
  avg_order_value DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
  new_customers   INT            NOT NULL DEFAULT 0,
  created_at      TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_summary_date (summary_date)
);

CREATE TABLE IF NOT EXISTS product_performance (
  id               CHAR(36)       NOT NULL,
  product_id       CHAR(36)       NOT NULL COMMENT 'References product_db.products.id',
  summary_date     DATE           NOT NULL,
  units_sold       INT            NOT NULL DEFAULT 0,
  revenue          DECIMAL(15, 2) NOT NULL DEFAULT 0.00,
  return_count     INT            NOT NULL DEFAULT 0,
  created_at       TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_prod_perf (product_id, summary_date),
  INDEX idx_pp_product (product_id)
);

CREATE TABLE IF NOT EXISTS customer_activity (
  id              CHAR(36)  NOT NULL,
  customer_id     CHAR(36)  NOT NULL COMMENT 'References customer_db.customers.id',
  activity_date   DATE      NOT NULL,
  total_orders    INT       NOT NULL DEFAULT 0,
  total_spent     DECIMAL(15, 2) NOT NULL DEFAULT 0.00,
  last_order_date DATE,
  created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_cust_activity (customer_id, activity_date),
  INDEX idx_ca_customer (customer_id)
);

CREATE TABLE IF NOT EXISTS dashboard_widgets (
  id           CHAR(36)     NOT NULL,
  widget_name  VARCHAR(100) NOT NULL,
  widget_type  VARCHAR(50)  NOT NULL COMMENT 'LINE_CHART, BAR_CHART, PIE_CHART, KPI_CARD, TABLE',
  config       JSON,
  display_order INT         NOT NULL DEFAULT 0,
  is_active    TINYINT(1)   NOT NULL DEFAULT 1,
  created_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id)
);

-- Seed default KPI names
INSERT IGNORE INTO dashboard_widgets (id, widget_name, widget_type, display_order) VALUES
  (UUID(), 'Total Revenue',       'KPI_CARD',   1),
  (UUID(), 'Total Orders',        'KPI_CARD',   2),
  (UUID(), 'New Customers',       'KPI_CARD',   3),
  (UUID(), 'Avg Order Value',     'KPI_CARD',   4),
  (UUID(), 'Revenue Over Time',   'LINE_CHART', 5),
  (UUID(), 'Orders by Status',    'PIE_CHART',  6),
  (UUID(), 'Top Products',        'BAR_CHART',  7),
  (UUID(), 'Customer Activity',   'TABLE',      8);
