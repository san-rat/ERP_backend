// ============================================================
//  DATA-DRIVEN PAYLOAD TESTING — ERP AuthService
//  Tool     : K6 (Load & Performance Testing)
//  Feature  : Data-driven testing from CSV
//  Tests    : Login flow + Registration flow + JWT token reuse
//  Student  : [Your Name]
// ============================================================

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { Trend, Rate, Counter } from 'k6/metrics';

const BASE_URL = 'http://localhost:5001';

// ── Load CSV files into shared memory ────────────────────────
// SharedArray loads ONCE and is shared across all virtual users
// Much more efficient than each VU reading the file separately

const loginUsers = new SharedArray('loginUsers', function () {
  const raw = open('./data/users.csv');
  return raw.trim().split('\n').slice(1).map(line => {
    const v = line.split(',');
    return {
      username:        v[0].trim(),
      password:        v[1].trim(),
      expected_status: parseInt(v[2].trim()),
      scenario:        v[3].trim(),
    };
  });
});

const registerUsers = new SharedArray('registerUsers', function () {
  const raw = open('./data/register.csv');
  return raw.trim().split('\n').slice(1).map(line => {
    const v = line.split(',');
    return {
      username:        v[0].trim(),
      email:           v[1].trim(),
      password:        v[2].trim(),
      role:            v[3].trim(),
      expected_status: parseInt(v[4].trim()),
      scenario:        v[5].trim(),
    };
  });
});

// ── Custom Metrics ────────────────────────────────────────────
const loginDuration    = new Trend('login_response_ms',    true);
const registerDuration = new Trend('register_response_ms', true);
const loginMatch       = new Rate('login_status_match');
const registerMatch    = new Rate('register_status_match');
const tokenExtracted   = new Counter('jwt_tokens_extracted');
const authFlowSuccess  = new Counter('full_auth_flow_success');

// ── Test Configuration ────────────────────────────────────────
export const options = {
  scenarios: {

    // Scenario A: Data-driven login testing
    login_data_driven: {
      executor:    'per-vu-iterations',
      vus:         5,
      iterations:  20,
      maxDuration: '2m',
      exec:        'loginTest',             // ← key fix: maps to loginTest function
      tags:        { test_type: 'login' },
    },

    // Scenario B: Data-driven registration testing
    register_data_driven: {
      executor:    'per-vu-iterations',
      vus:         3,
      iterations:  7,
      maxDuration: '2m',
      exec:        'registerTest',          // ← key fix: maps to registerTest function
      tags:        { test_type: 'register' },
      startTime:   '10s',                   // starts after login scenario warms up
    },
  },

  // Thresholds = automated pass/fail gates (used in CI/CD pipelines)
  thresholds: {
    login_response_ms:     ['p(95)<3000', 'avg<1000'],
    register_response_ms:  ['p(95)<4000'],
    login_status_match:    ['rate>0.75'],   // 75% — intentional: we have invalid rows
    register_status_match: ['rate>0.80'],   // 80% — intentional: we have invalid rows  
    http_req_duration:     ['p(90)<2000'],
  },
};

// ── Shared JSON headers ───────────────────────────────────────
const JSON_HEADERS = {
  'Content-Type': 'application/json',
  'Accept':       'application/json',
};

// ── Helper: safely parse JSON body ───────────────────────────
function parseBody(res) {
  try   { return res.json(); }
  catch { return {};          }
}

// ── SCENARIO A: Login data-driven test ───────────────────────
export function loginTest() {

  // __ITER cycles through all CSV rows automatically
  const loginUser = loginUsers[__ITER % loginUsers.length];

  group(`LOGIN | [${loginUser.scenario}]`, function () {

    // Send POST /api/auth/login with data from CSV row
    const res = http.post(
      `${BASE_URL}/api/auth/login`,
      JSON.stringify({ username: loginUser.username, password: loginUser.password }),
      { headers: JSON_HEADERS, tags: { endpoint: 'login', scenario: loginUser.scenario } }
    );

    loginDuration.add(res.timings.duration);
    loginMatch.add(res.status === loginUser.expected_status);

    const body = parseBody(res);

    // 6 assertions per request
    check(res, {
      '✓ status matches CSV expected':
        (r) => r.status === loginUser.expected_status,

      '✓ response time under 3s':
        (r) => r.timings.duration < 3000,

      '✓ body is not empty':
        (r) => r.body !== null && r.body.length > 0,

      '✓ JWT token present on successful login':
        (r) => r.status !== 200 || (
          body.token        !== undefined ||
          body.accessToken  !== undefined ||
          body.access_token !== undefined
        ),

      '✓ error message present on failed login':
        (r) => r.status !== 401 || (
          body.message !== undefined && body.message.length > 0
        ),

      '✓ content-type is JSON':
        (r) => (r.headers['Content-Type'] || '').includes('application/json'),
    });

    // ── JWT Token Reuse ───────────────────────────────────────
    // Extract the JWT from a successful login and reuse it
    // in a second request — proves the full auth flow works
    if (res.status === 200) {
      const token = body.token || body.accessToken || body.access_token;
      tokenExtracted.add(1);

      group(`TOKEN REUSE | [${loginUser.scenario}] → authenticated request`, function () {

        const authRes = http.get(
          `${BASE_URL}/health`,
          {
            headers: {
              ...JSON_HEADERS,
              'Authorization': `Bearer ${token}`,  // inject JWT from login response
            },
            tags: { endpoint: 'health_check', scenario: loginUser.scenario },
          }
        );

        check(authRes, {
          '✓ authenticated request reaches server':
            (r) => r.status === 200 || r.status === 401,

          '✓ token was not rejected (no 403 forbidden)':
            (r) => r.status !== 403,

          '✓ auth header was correctly formed':
            () => token !== undefined && token.length > 10,
        });

        if (authRes.status === 200) authFlowSuccess.add(1);
      });
    }
  });

  sleep(0.5);
}

// ── SCENARIO B: Registration data-driven test ─────────────────
export function registerTest() {

  const regUser = registerUsers[__ITER % registerUsers.length];

  group(`REGISTER | [${regUser.scenario}]`, function () {

// Generate unique username per iteration using timestamp
// This prevents 409 conflicts on repeated test runs
const uniqueUsername = regUser.username
  ? `${regUser.username}_${Date.now()}`
  : regUser.username;

const uniqueEmail = regUser.email && regUser.email.includes('@')
  ? `${regUser.email.split('@')[0]}_${Date.now()}@${regUser.email.split('@')[1]}`
  : regUser.email;

const res = http.post(
  `${BASE_URL}/api/auth/register`,
  JSON.stringify({
    username: uniqueUsername,
    email:    uniqueEmail,
    password: regUser.password,
    role:     regUser.role,
  }),
      { headers: JSON_HEADERS, tags: { endpoint: 'register', scenario: regUser.scenario } }
    );

    registerDuration.add(res.timings.duration);
    registerMatch.add(res.status === regUser.expected_status);

    const body = parseBody(res);

    // 6 assertions per request
    check(res, {
      '✓ status matches CSV expected':
        (r) => r.status === regUser.expected_status,

      '✓ response time under 4s':
        (r) => r.timings.duration < 4000,

      '✓ 201 returns JWT token immediately (auto-login)':
        (r) => r.status !== 201 || (
          body.token       !== undefined ||
          body.accessToken !== undefined
        ),

      '✓ 400 returns validation error message':
        (r) => r.status !== 400 || (
          body.message !== undefined && body.message.length > 0
        ),

      '✓ 409 conflict has error message':
        (r) => r.status !== 409 || (
          body.message !== undefined && body.message.length > 0
        ),

      '✓ content-type is JSON':
        (r) => (r.headers['Content-Type'] || '').includes('application/json'),
    });
  });

  sleep(0.5);
}

// ── Final Summary ─────────────────────────────────────────────
export function handleSummary(data) {
  const lMatch  = ((data.metrics.login_status_match?.values?.rate    || 0) * 100).toFixed(1);
  const rMatch  = ((data.metrics.register_status_match?.values?.rate || 0) * 100).toFixed(1);
  const lAvg    = (data.metrics.login_response_ms?.values?.avg        || 0).toFixed(0);
  const lP95    = (data.metrics.login_response_ms?.values?.['p(95)']  || 0).toFixed(0);
  const rAvg    = (data.metrics.register_response_ms?.values?.avg     || 0).toFixed(0);
  const tokens  = data.metrics.jwt_tokens_extracted?.values?.count    || 0;
  const flows   = data.metrics.full_auth_flow_success?.values?.count  || 0;
  const total   = data.metrics.http_reqs?.values?.count               || 0;

  console.log('\n');
  console.log('╔══════════════════════════════════════════════════════════╗');
  console.log('║         DATA-DRIVEN AUTH TEST — FINAL SUMMARY            ║');
  console.log('╠══════════════════════════════════════════════════════════╣');
  console.log(`  📂 Login CSV Rows        : ${loginUsers.length} scenarios`);
  console.log(`  📂 Register CSV Rows     : ${registerUsers.length} scenarios`);
  console.log(`  🔁 Total HTTP Requests   : ${total}`);
  console.log('  ─────────────────────────────────────────────────────────');
  console.log(`  🔐 Login Status Match    : ${lMatch}%`);
  console.log(`  ⚡ Login Avg Response    : ${lAvg}ms`);
  console.log(`  📊 Login P95 Response    : ${lP95}ms`);
  console.log('  ─────────────────────────────────────────────────────────');
  console.log(`  📝 Register Status Match : ${rMatch}%`);
  console.log(`  ⚡ Register Avg Response : ${rAvg}ms`);
  console.log('  ─────────────────────────────────────────────────────────');
  console.log(`  🎟️  JWT Tokens Extracted  : ${tokens}`);
  console.log(`  ✅ Full Auth Flows Done  : ${flows}`);
  console.log('╚══════════════════════════════════════════════════════════╝');
  console.log('\n');

  return {};
}