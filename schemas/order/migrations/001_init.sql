/*CREATE TABLE IF NOT EXISTS orders (
  id              CHAR(36)       NOT NULL,
  customer_id     CHAR(36)       NOT NULL COMMENT 'References customer_db.customers.id',
  status          VARCHAR(30)    NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING, CONFIRMED, PROCESSING, SHIPPED, DELIVERED, CANCELLED',
  total_amount    DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
  currency        CHAR(3)        NOT NULL DEFAULT 'USD',
  shipping_address_snapshot JSON COMMENT 'Snapshot of address at time of order',
  notes           TEXT,
  created_at      TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at      TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  INDEX idx_order_customer (customer_id),
  INDEX idx_order_status   (status)
);

CREATE TABLE IF NOT EXISTS order_items (
  id          CHAR(36)       NOT NULL,
  order_id    CHAR(36)       NOT NULL,
  product_id  CHAR(36)       NOT NULL COMMENT 'References product_db.products.id',
  product_name VARCHAR(255)  NOT NULL COMMENT 'Snapshot of product name at time of order',
  quantity    INT            NOT NULL,
  unit_price  DECIMAL(12, 2) NOT NULL,
  total_price DECIMAL(12, 2) NOT NULL,
  created_at  TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_oi_order FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
  INDEX idx_oi_product (product_id)
);

CREATE TABLE IF NOT EXISTS order_status_history (
  id         CHAR(36)    NOT NULL,
  order_id   CHAR(36)    NOT NULL,
  old_status VARCHAR(30),
  new_status VARCHAR(30) NOT NULL,
  changed_by CHAR(36)    COMMENT 'References auth_db.users.id',
  changed_at TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  note       TEXT,
  PRIMARY KEY (id),
  CONSTRAINT fk_osh_order FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS returns (
  id          CHAR(36)       NOT NULL,
  order_id    CHAR(36)       NOT NULL,
  reason      TEXT,
  status      VARCHAR(30)    NOT NULL DEFAULT 'REQUESTED' COMMENT 'REQUESTED, APPROVED, REJECTED, COMPLETED',
  refund_amount DECIMAL(12, 2),
  created_at  TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at  TIMESTAMP      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_ret_order FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS return_items (
  id          CHAR(36) NOT NULL,
  return_id   CHAR(36) NOT NULL,
  order_item_id CHAR(36) NOT NULL,
  quantity    INT      NOT NULL,
  reason      TEXT,
  PRIMARY KEY (id),
  CONSTRAINT fk_ri_return     FOREIGN KEY (return_id)    REFERENCES returns(id)     ON DELETE CASCADE,
  CONSTRAINT fk_ri_order_item FOREIGN KEY (order_item_id) REFERENCES order_items(id) ON DELETE CASCADE
);
*