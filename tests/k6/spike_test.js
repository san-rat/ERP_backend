// tests/k6/spike_test.js
// ─────────────────────────────────────────────────────────────────────────────
// SE3112 — Load/Performance Testing | Student 3
// Feature : Spike Testing Configuration
//
// Purpose : Simulate a sudden, near-instant surge of traffic (e.g. a flash
//           sale or a DDoS burst) and then immediately drop back. Unlike
//           stress testing (which ramps gradually), spike testing measures:
//             • How gracefully the system handles instant overload
//             • How quickly it RECOVERS after the load disappears
//             • Whether it returns 503 (overloaded) vs 500 (crashed)
//
// Endpoints tested (via API Gateway :5000):
//   GET  /api/products         — primary spike target (high read load)
//   GET  /api/products/stock   — secondary target (inventory queries)
//
// Run command:
//   k6 run tests/k6/spike_test.js
// ─────────────────────────────────────────────────────────────────────────────

import http              from 'k6/http';
import { check, sleep }  from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { getAuthToken }  from './helpers/auth.js';

// ─── Custom Metrics ───────────────────────────────────────────────────────────

/** Response duration for every request in the spike scenario. */
const spikeDuration = new Trend('spike_response_duration_ms', true);

/** Fraction of failed checks during the spike window. */
const spikeErrorRate = new Rate('spike_error_rate');

/**
 * Counter: increments every time a 200 OK is returned.
 * Used to observe the RECOVERY phase — rising count after the spike drops
 * means the system has recovered.
 */
const recoveredRequests = new Counter('successful_requests_after_spike');

// ─── Spike Test Stages (Load Shape) ──────────────────────────────────────────
//
// Stage 1 — Baseline     :  5 VUs for 15 s   (calm, normal traffic)
// Stage 2 — SPIKE        :  5 → 200 VUs in 5s (instant massive surge)
// Stage 3 — Hold Spike   : 200 VUs for 30 s  (system under extreme load)
// Stage 4 — Drop         : 200 →  5 VUs in 5s (load disappears suddenly)
// Stage 5 — Recovery     :  5 VUs for 30 s   (observe system returning to normal)
// Stage 6 — Wind Down    :  5 →  0 VUs in 10s
//
export const options = {
  stages: [
    { duration: '15s', target: 5   },  // Baseline calm traffic
    { duration: '5s',  target: 200 },  // <<< THE SPIKE — instant surge
    { duration: '30s', target: 200 },  // Hold the spike
    { duration: '5s',  target: 5   },  // <<< DROP — load disappears
    { duration: '30s', target: 5   },  // Recovery observation window
    { duration: '10s', target: 0   },  // Wind down
  ],

  // ── Thresholds ──────────────────────────────────────────────────────────
  // Thresholds for spike are more lenient than stress — spikes WILL cause
  // degradation; the key question is whether the system survives gracefully.
  thresholds: {
    // 95% of requests should respond within 3 seconds (even during spike)
    'http_req_duration':          ['p(95)<3000'],
    // The absolute worst-case (p99) should not exceed 5 seconds
    'spike_response_duration_ms': ['p(99)<5000'],
    // Spike error rate can go up to 20% during the burst — it is expected
    'spike_error_rate':           ['rate<0.20'],
    // The system should NOT completely fail (hard limit: no more than 30% raw HTTP failures)
    'http_req_failed':            ['rate<0.30'],
  },
};

// ─── Setup — runs ONCE before any VU starts ───────────────────────────────────
export function setup() {
  const token = getAuthToken();
  console.log('[spike] Setup complete — starting spike scenario.');
  return { token };
}

// ─── Teardown ─────────────────────────────────────────────────────────────────
export function teardown(data) {
  console.log(
    '[spike] Test complete. Check spike_error_rate and successful_requests_after_spike ' +
    'in the summary to assess recovery behaviour.'
  );
}

// ─── Virtual User Scenario ────────────────────────────────────────────────────
export default function (data) {
  const BASE = 'http://localhost:5000';
  const headers = {
    Authorization: `Bearer ${data.token}`,
    'Content-Type': 'application/json',
  };

  // ── Primary Spike Target: GET /api/products ────────────────────────────
  // This is the most queried endpoint in the ERP system.
  // Under spike conditions it will be hit by 200 concurrent VUs.
  const productRes = http.get(
    `${BASE}/api/products?pageNumber=1&pageSize=20`,
    { headers, tags: { name: 'SPIKE_products' } }
  );

  spikeDuration.add(productRes.timings.duration);

  const productOk = check(productRes, {
    // 200 = healthy, 503 = graceful overload — both acceptable during spike
    // 500 = server crash — NEVER acceptable
    'products — 200 or 503 (no crash)':   (r) => r.status === 200 || r.status === 503,
    'products — not a 500 (server error)': (r) => r.status !== 500,
    'products — responded within 5s':      (r) => r.timings.duration < 5000,
  });

  spikeErrorRate.add(!productOk);

  // Only count as a "recovered" request if we got a full 200 OK.
  // During the spike, this count will plateau; after the drop, it will rise —
  // which is the recovery curve you explain in the demo.
  if (productRes.status === 200) {
    recoveredRequests.add(1);
  }

  sleep(0.5); // Shorter sleep = more aggressive — intentional for spike testing

  // ── Secondary Target: GET /api/products/stock ──────────────────────────
  const stockRes = http.get(
    `${BASE}/api/products/stock`,
    { headers, tags: { name: 'SPIKE_stock' } }
  );

  spikeDuration.add(stockRes.timings.duration);

  const stockOk = check(stockRes, {
    'stock — 200 or 503 (no crash)':    (r) => r.status === 200 || r.status === 503,
    'stock — not a 500 (server error)': (r) => r.status !== 500,
    'stock — responded within 5s':      (r) => r.timings.duration < 5000,
  });

  spikeErrorRate.add(!stockOk);

  if (stockRes.status === 200) {
    recoveredRequests.add(1);
  }

  sleep(0.5);
}
