// tests/k6/spike_test.js
// ─────────────────────────────────────────────────────────────────────────────
// SE3112 — Load/Performance Testing | Student 3
// Feature : Spike Testing Configuration
//
// Purpose : Simulate a sudden, near-instant surge of employees logging in
//           at the same time (e.g. start of a work shift where all staff
//           log in within seconds of each other). Unlike stress testing
//           (which ramps gradually), spike testing measures:
//             • How gracefully the AuthService handles an instant login flood
//             • Whether JWT tokens are still issued correctly under burst load
//             • How quickly the system RECOVERS after the surge drops
//             • Whether it returns 503 (overloaded) vs 500 (crashed/broken)
//
// Endpoint tested (via API Gateway :5000):
//   POST /api/auth/login  — Employee login under instant spike load
//
// Run command:
//   k6 run tests/k6/spike_test.js
// ─────────────────────────────────────────────────────────────────────────────

import http              from 'k6/http';
import { check, sleep }  from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';

// ─── Custom Metrics ───────────────────────────────────────────────────────────

/** Response duration for every login attempt in the spike scenario. */
const loginSpikeDuration = new Trend('login_spike_duration_ms', true);

/** Fraction of failed checks during the spike window. */
const spikeErrorRate = new Rate('spike_error_rate');

/**
 * Counter: increments every time a login returns HTTP 200 with a valid token.
 * During the spike, this count will plateau (system overwhelmed).
 * After the drop, it will rise again — this rising count shows RECOVERY.
 */
const successfulLogins = new Counter('successful_logins_after_spike');

// ─── Spike Test Stages (Load Shape) ──────────────────────────────────────────
//
// Stage 1 — Baseline     :   5 VUs for  5 s   (calm — a few employees logging in)
// Stage 2 — SPIKE        :   5 → 200 VUs in 3s (instant surge — shift start)
// Stage 3 — Hold Spike   : 200 VUs for 15 s   (system under extreme login load)
// Stage 4 — Drop         : 200 →   5 VUs in 3s (surge ends — most employees in)
// Stage 5 — Recovery     :   5 VUs for 10 s   (observe system returning to normal)
// Stage 6 — Wind Down    :   5 →   0 VUs in  4s  — total: ~40 s
//
export const options = {
  stages: [
    { duration: '5s',  target: 5   },  // Baseline — calm login traffic
    { duration: '3s',  target: 200 },  // <<< THE SPIKE — all employees login at once
    { duration: '15s', target: 200 },  // Hold the spike
    { duration: '3s',  target: 5   },  // <<< DROP — surge ends
    { duration: '10s', target: 5   },  // Recovery observation window
    { duration: '4s',  target: 0   },  // Wind down — total: ~40 s
  ],

  // ── Thresholds ──────────────────────────────────────────────────────────
  // Thresholds for spike are more lenient than stress — spikes WILL cause
  // degradation. The key question is: does the system survive without crashing?
  thresholds: {
    // 95% of login requests must respond within 3 seconds even during spike
    'http_req_duration':       ['p(95)<3000'],
    // The absolute worst-case (p99) must not exceed 5 seconds
    'login_spike_duration_ms': ['p(99)<5000'],
    // Spike error rate can reach up to 20% during the burst — it is expected
    'spike_error_rate':        ['rate<0.20'],
    // Hard limit: no more than 30% raw HTTP failures (system must not fully crash)
    'http_req_failed':         ['rate<0.30'],
  },
};

// ─── Setup — runs ONCE before any VU starts ───────────────────────────────────
// Validates the login endpoint is reachable before launching the spike.
export function setup() {
  const res = http.post(
    'http://localhost:5000/api/auth/login',
    JSON.stringify({ Username: 'employee', Password: 'Employee@123' }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (res.status !== 200) {
    throw new Error(
      `[spike] Pre-flight login check failed — HTTP ${res.status}. ` +
      'Ensure the AuthService is running before starting the spike test.'
    );
  }

  console.log('[spike] Setup complete — starting spike scenario.');
}

// ─── Teardown ─────────────────────────────────────────────────────────────────
export function teardown() {
  console.log(
    '[spike] Test complete. Check spike_error_rate and successful_logins_after_spike ' +
    'in the summary to assess recovery behaviour.'
  );
}

// ─── Virtual User Scenario ────────────────────────────────────────────────────
// Each VU continuously calls POST /api/auth/login with employee credentials.
// During the spike, 200 VUs will be doing this simultaneously —
// simulating all employees logging in the moment their shift begins.
export default function () {
  const BASE = 'http://localhost:5000';

  // ── POST /api/auth/login (employee credentials) ────────────────────────
  const loginRes = http.post(
    `${BASE}/api/auth/login`,
    JSON.stringify({ Username: 'employee', Password: 'Employee@123' }),
    {
      headers: { 'Content-Type': 'application/json' },
      tags:    { name: 'SPIKE_login' },
    }
  );

  loginSpikeDuration.add(loginRes.timings.duration);

  const loginOk = check(loginRes, {
    // 200 = login succeeded, 503 = server overloaded but alive — both acceptable
    // 500 = server crashed internally — NEVER acceptable
    'login — 200 or 503 (no crash)':    (r) => r.status === 200 || r.status === 503,
    'login — not a 500 (server error)': (r) => r.status !== 500,
    'login — responded within 5s':      (r) => r.timings.duration < 5000,
  });

  spikeErrorRate.add(!loginOk);

  // Only count as a successful login if we received a full HTTP 200 with a token.
  // During the spike, this plateaus as the server struggles.
  // After the drop, the rising count shows the system has recovered.
  if (loginRes.status === 200) {
    try {
      const body = JSON.parse(loginRes.body);
      if (body.token) {
        successfulLogins.add(1);
      }
    } catch {
      // body was not valid JSON — count it as a non-recovery
    }
  }

  // Shorter sleep = more aggressive concurrent load — intentional for spike
  sleep(0.5);
}
