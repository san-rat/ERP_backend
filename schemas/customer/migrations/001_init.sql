/*CREATE TABLE IF NOT EXISTS customers (
  id           CHAR(36)     NOT NULL,
  user_id      CHAR(36)     NOT NULL COMMENT 'References auth_db.users.id',
  first_name   VARCHAR(100) NOT NULL,
  last_name    VARCHAR(100) NOT NULL,
  email        VARCHAR(255) NOT NULL,
  phone        VARCHAR(30),
  company_name VARCHAR(255),
  is_active    TINYINT(1)   NOT NULL DEFAULT 1,
  created_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_customer_email (email),
  INDEX idx_customer_user (user_id)
);

CREATE TABLE IF NOT EXISTS customer_addresses (
  id            CHAR(36)     NOT NULL,
  customer_id   CHAR(36)     NOT NULL,
  address_type  VARCHAR(20)  NOT NULL COMMENT 'BILLING, SHIPPING',
  street        VARCHAR(255) NOT NULL,
  city          VARCHAR(100) NOT NULL,
  state         VARCHAR(100),
  postal_code   VARCHAR(20),
  country       VARCHAR(100) NOT NULL,
  is_default    TINYINT(1)   NOT NULL DEFAULT 0,
  created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_ca_customer FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS customer_segments (
  id           INT         NOT NULL AUTO_INCREMENT,
  segment_name VARCHAR(100) NOT NULL,
  description  VARCHAR(255),
  created_at   TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_segment_name (segment_name)
);

CREATE TABLE IF NOT EXISTS customer_segment_assignments (
  customer_id CHAR(36) NOT NULL,
  segment_id  INT      NOT NULL,
  assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (customer_id, segment_id),
  CONSTRAINT fk_csa_customer FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE,
  CONSTRAINT fk_csa_segment  FOREIGN KEY (segment_id)  REFERENCES customer_segments(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS customer_attributes (
  id            CHAR(36)     NOT NULL,
  customer_id   CHAR(36)     NOT NULL,
  attribute_key VARCHAR(100) NOT NULL,
  attribute_val TEXT,
  created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_customer_attr (customer_id, attribute_key),
  CONSTRAINT fk_cattr_customer FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE
);

-- Seed default segments
INSERT IGNORE INTO customer_segments (segment_name, description) VALUES
  ('VIP',         'High-value customers'),
  ('At Risk',     'Customers likely to churn'),
  ('New',         'Recently acquired customers'),
  ('Loyal',       'Long-term active customers'),
  ('Inactive',    'No activity in 90+ days');
*/