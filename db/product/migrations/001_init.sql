CREATE TABLE IF NOT EXISTS categories (
  id            INT          NOT NULL AUTO_INCREMENT,
  name          VARCHAR(100) NOT NULL,
  parent_id     INT          COMMENT 'Self-referencing for sub-categories',
  description   VARCHAR(255),
  created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_category_name (name),
  CONSTRAINT fk_cat_parent FOREIGN KEY (parent_id) REFERENCES categories(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS products (
  id           CHAR(36)       NOT NULL,
  category_id  INT,
  sku          VARCHAR(100)   NOT NULL,
  name         VARCHAR(255)   NOT NULL,
  description  TEXT,
  price        DECIMAL(12, 2) NOT NULL,
  is_active    TINYINT(1)     NOT NULL DEFAULT 1,
  created_at   TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at   TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_product_sku (sku),
  INDEX idx_product_category (category_id),
  CONSTRAINT fk_prod_category FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS inventory (
  id                 CHAR(36) NOT NULL,
  product_id         CHAR(36) NOT NULL,
  quantity_available INT      NOT NULL DEFAULT 0,
  quantity_reserved  INT      NOT NULL DEFAULT 0,
  low_stock_threshold INT     NOT NULL DEFAULT 10,
  updated_at         TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_inventory_product (product_id),
  CONSTRAINT fk_inv_product FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS inventory_reservations (
  id          CHAR(36)  NOT NULL,
  product_id  CHAR(36)  NOT NULL,
  order_id    CHAR(36)  NOT NULL COMMENT 'References order_db.orders.id',
  quantity    INT       NOT NULL,
  status      VARCHAR(20) NOT NULL DEFAULT 'RESERVED' COMMENT 'RESERVED, RELEASED, FULFILLED',
  reserved_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  released_at TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_res_product (product_id),
  INDEX idx_res_order   (order_id),
  CONSTRAINT fk_res_product FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS low_stock_alerts (
  id          CHAR(36)   NOT NULL,
  product_id  CHAR(36)   NOT NULL,
  quantity_at_alert INT  NOT NULL,
  is_resolved TINYINT(1) NOT NULL DEFAULT 0,
  alerted_at  TIMESTAMP  NOT NULL DEFAULT CURRENT_TIMESTAMP,
  resolved_at TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_lsa_product (product_id),
  CONSTRAINT fk_lsa_product FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
);

INSERT IGNORE INTO categories (name) VALUES
  ('Electronics'),
  ('Clothing'),
  ('Food & Beverage'),
  ('Office Supplies'),
  ('Machinery');
