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
// Endpoint tested (via API Gateway :5000):
//   POST /api/auth/login  — Employee login (authentication under heavy load)
//
// Why login?
//   The login endpoint is the GATEWAY to the entire ERP system.
//   Every employee must authenticate before accessing any feature.
//   If login collapses under load, the entire system becomes inaccessible.
//   This is the most critical endpoint to stress test.
//
// Run command:
//   k6 run tests/k6/stress_test.js
// ─────────────────────────────────────────────────────────────────────────────

import http          from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate }  from 'k6/metrics';

// ─── Custom Metrics ───────────────────────────────────────────────────────────
// Trend : records timing values — exposes avg, p90, p95, p99 in the report
// Rate  : records a boolean rate (pass/fail fraction)

/** Response time trend for every POST /api/auth/login attempt. */
const loginDuration = new Trend('login_duration_ms', true);

/** Fraction of check() assertions that returned false (i.e. failures). */
const errorRate = new Rate('stress_error_rate');

// ─── Stress Test Stages (Load Shape) ─────────────────────────────────────────
//
// Stage 1 — Warm-up      :  0 → 10 VUs over 10 s   (normal morning login)
// Stage 2 — Normal Load  : 10 → 50 VUs over 15 s   (typical shift start)
// Stage 3 — Stress Begins: 50 →100 VUs over 15 s   (above designed capacity)
// Stage 4 — Heavy Stress :100 →200 VUs over 15 s   (extreme — find the limit)
// Stage 5 — Ramp Down    :200 →  0 VUs over 17 s   (recovery observation)
//
export const options = {
  stages: [
    { duration: '10s', target: 10  },  // Warm-up
    { duration: '15s', target: 50  },  // Normal load
    { duration: '15s', target: 100 },  // Stress begins
    { duration: '15s', target: 200 },  // Heavy stress — peak
    { duration: '17s', target: 0   },  // Ramp down — total: ~72 s
  ],

  // ── Thresholds — pass/fail criteria for the whole test run ──────────────
  // The test is marked FAILED if any threshold is breached.
  thresholds: {
    // 95% of all login requests must complete within 2 seconds
    'http_req_duration': ['p(95)<2000'],
    // Overall HTTP failure rate (non-2xx) must stay below 10%
    'http_req_failed':   ['rate<0.10'],
    // Custom: 90th percentile login time must stay under 1.5 seconds
    'login_duration_ms': ['p(90)<1500'],
    // Custom: our check()-based error rate under 10%
    'stress_error_rate': ['rate<0.10'],
  },
};

// ─── Setup — runs ONCE before any VU starts ───────────────────────────────────
// Validates the login endpoint is reachable before launching the full test.
export function setup() {
  const res = http.post(
    'http://localhost:5000/api/auth/login',
    JSON.stringify({ Username: 'employee', Password: 'Employee@123' }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (res.status !== 200) {
    throw new Error(
      `[stress] Pre-flight login check failed — HTTP ${res.status}. ` +
      'Ensure the AuthService is running before starting the stress test.'
    );
  }

  console.log('[stress] Pre-flight login OK — starting stress test.');
}

// ─── Teardown — runs ONCE after all VUs finish ────────────────────────────────
export function teardown() {
  console.log('[stress] Test complete. Review thresholds above for pass/fail.');
}

// ─── Virtual User Scenario — executed repeatedly by each VU ──────────────────
// Each VU continuously calls POST /api/auth/login with employee credentials.
// This simulates many employees logging in concurrently during a busy shift.
export default function () {
  const BASE = 'http://localhost:5000';

  // ── POST /api/auth/login ───────────────────────────────────────────────
  const loginRes = http.post(
    `${BASE}/api/auth/login`,
    JSON.stringify({ Username: 'employee', Password: 'Employee@123' }),
    {
      headers: { 'Content-Type': 'application/json' },
      tags:    { name: 'STRESS_login' },
    }
  );

  // Record this login attempt's duration in our custom Trend metric
  loginDuration.add(loginRes.timings.duration);

  const loginOk = check(loginRes, {
    'login — HTTP 200':           (r) => r.status === 200,
    'login — token in response':  (r) => {
      try {
        const body = JSON.parse(r.body);
        return typeof body.token === 'string' && body.token.length > 0;
      } catch {
        return false;
      }
    },
    'login — under 2000ms':       (r) => r.timings.duration < 2000,
  });

  // If any check failed, count it as an error in our custom rate metric
  errorRate.add(!loginOk);

  // Simulate the time an employee takes between login and next action
  sleep(1);
}
