// tests/k6/stress_test.js
// ─────────────────────────────────────────────────────────────────────────────
// SE3112 — Load/Performance Testing | Student 3
// Feature : Stress Testing Configuration
//
// Purpose : Gradually ramp Virtual Users (VUs) from a normal baseline up to
//           a level that exceeds the system's designed capacity. The goal is
//           to discover the BREAKING POINT — the load at which response times
//           degrade unacceptably or errors begin appearing.
//
// Endpoints tested (via API Gateway :5000):
//   GET  /api/products         — paginated product list (most common read)
//   GET  /api/products/stock   — full inventory stock view
//   GET  /api/products/alerts  — low-stock alert list
//
// Run command:
//   k6 run tests/k6/stress_test.js
// ─────────────────────────────────────────────────────────────────────────────

import http          from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate }  from 'k6/metrics';
import { getAuthToken } from './helpers/auth.js';

// ─── Custom Metrics ───────────────────────────────────────────────────────────
// Trend  : records timing values — exposes p90, p95, p99 in the report
// Rate   : records a boolean rate (pass/fail fraction)

/** Response time trend for the GET /api/products endpoint specifically. */
const productListDuration = new Trend('product_list_duration_ms', true);

/** Response time trend for the GET /api/products/stock endpoint. */
const stockDuration = new Trend('stock_duration_ms', true);

/** Fraction of check() assertions that returned false (i.e. failures). */
const errorRate = new Rate('stress_error_rate');

// ─── Stress Test Stages (Load Shape) ─────────────────────────────────────────
//
// Stage 1 — Warm-up      :  0 → 10 VUs over 30 s   (normal baseline)
// Stage 2 — Normal Load  : 10 → 50 VUs over  1 min (typical peak)
// Stage 3 — Stress Begins: 50 →100 VUs over  1 min (above designed capacity)
// Stage 4 — Heavy Stress :100 →200 VUs over  1 min (extreme — find limit)
// Stage 5 — Ramp Down    :200 →  0 VUs over 30 s   (recovery observation)
//
export const options = {
  stages: [
    { duration: '30s', target: 10  },
    { duration: '1m',  target: 50  },
    { duration: '1m',  target: 100 },
    { duration: '1m',  target: 200 },
    { duration: '30s', target: 0   },
  ],

  // ── Thresholds — pass/fail criteria for the whole test run ──────────────
  // The test is marked FAILED if any threshold is breached.
  thresholds: {
    // 95% of ALL requests must respond within 2 seconds
    'http_req_duration':       ['p(95)<2000'],
    // Overall HTTP failure rate (non-2xx) must stay below 10%
    'http_req_failed':         ['rate<0.10'],
    // Custom: 90th percentile for the product list specifically under 1.5 s
    'product_list_duration_ms':['p(90)<1500'],
    // Custom: our check()-based error rate under 10%
    'stress_error_rate':       ['rate<0.10'],
  },
};

// ─── Setup — runs ONCE before any VU starts ───────────────────────────────────
// Returns data that is passed to every VU's default() function.
export function setup() {
  const token = getAuthToken();
  return { token };
}

// ─── Teardown — runs ONCE after all VUs finish ────────────────────────────────
export function teardown(data) {
  console.log('[stress] Test complete. Review thresholds above for pass/fail.');
}

// ─── Virtual User Scenario — executed repeatedly by each VU ──────────────────
export default function (data) {
  const BASE = 'http://localhost:5000';
  const headers = {
    Authorization: `Bearer ${data.token}`,
    'Content-Type': 'application/json',
  };

  // ── Request 1: GET /api/products (paginated list) ──────────────────────
  const productRes = http.get(
    `${BASE}/api/products?pageNumber=1&pageSize=10`,
    { headers, tags: { name: 'GET_products' } }
  );

  // Record this endpoint's duration in our custom Trend metric
  productListDuration.add(productRes.timings.duration);

  const productChecks = check(productRes, {
    'products — HTTP 200':          (r) => r.status === 200,
    'products — body not empty':    (r) => r.body && r.body.length > 2,
    'products — under 2000ms':      (r) => r.timings.duration < 2000,
  });

  // If any check failed, count it as an error
  errorRate.add(!productChecks);

  sleep(1); // simulate think time between user actions

  // ── Request 2: GET /api/products/stock ─────────────────────────────────
  const stockRes = http.get(
    `${BASE}/api/products/stock`,
    { headers, tags: { name: 'GET_stock' } }
  );

  stockDuration.add(stockRes.timings.duration);

  const stockChecks = check(stockRes, {
    'stock — HTTP 200':             (r) => r.status === 200,
    'stock — under 2000ms':         (r) => r.timings.duration < 2000,
  });

  errorRate.add(!stockChecks);

  sleep(1);

  // ── Request 3: GET /api/products/alerts ────────────────────────────────
  const alertRes = http.get(
    `${BASE}/api/products/alerts`,
    { headers, tags: { name: 'GET_alerts' } }
  );

  check(alertRes, {
    'alerts — HTTP 200':            (r) => r.status === 200,
    'alerts — under 2000ms':        (r) => r.timings.duration < 2000,
  });

  sleep(1);
}
