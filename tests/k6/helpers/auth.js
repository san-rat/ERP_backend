// tests/k6/helpers/auth.js
// ─────────────────────────────────────────────────────────────────────────────
// Shared authentication helper for K6 tests.
// Called in setup() so that all Virtual Users share one token,
// rather than each VU hammering the /login endpoint individually.
// ─────────────────────────────────────────────────────────────────────────────

import http from 'k6/http';

/**
 * Logs in to the InsightERP AuthService via the API Gateway and returns a
 * raw Bearer token string.
 *
 * @param {string} [baseUrl='http://localhost:5000'] - API Gateway base URL.
 * @param {string} [email='admin@insighterp.com']    - Test account email.
 * @param {string} [password='Admin@1234']           - Test account password.
 * @returns {string} JWT access token.
 */
export function getAuthToken(
  baseUrl  = 'http://localhost:5000',
  email    = 'admin@insighterp.com',
  password = 'Admin@1234'
) {
  const res = http.post(
    `${baseUrl}/api/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (res.status !== 200) {
    throw new Error(
      `[auth] Login failed — HTTP ${res.status}: ${res.body}`
    );
  }

  const body = JSON.parse(res.body);

  // The AuthService returns { token: "..." } — adjust the key if needed.
  const token = body.token || body.accessToken || body.access_token;
  if (!token) {
    throw new Error('[auth] No token found in login response: ' + res.body);
  }

  console.log('[auth] Login successful — token acquired.');
  return token;
}
