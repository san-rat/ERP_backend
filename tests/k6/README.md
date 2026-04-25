# K6 Load Tests — InsightERP Backend

## Module: SE3112 — Advanced Software Engineering
## Category: Load / Performance Testing
## Tool: [K6](https://k6.io/)

---

## Student Ownership

| Student | Tool | Feature |
|---------|------|---------|
| Student 3 | K6 | **Spike & Stress Testing Configurations** |

---

## Feature Under Test — Employee Login (`POST /api/auth/login`)

Both the stress test and spike test target the **Employee Login endpoint** of the InsightERP AuthService.

**Endpoint:** `POST /api/auth/login` via API Gateway (`http://localhost:5000`)

**Why this endpoint?**
The login endpoint is the **gateway to the entire ERP system**. Every employee must successfully authenticate before accessing any feature — products, orders, stock, alerts, or dashboards. If the login endpoint collapses under load, the entire system becomes inaccessible to all users. This makes it the most critical and realistic endpoint to performance test.

**Scenario simulated:**
- **Stress test** → Employees gradually logging in during a busy business day (morning shift ramp-up)
- **Spike test** → All employees logging in at the exact same moment (shift start surge / system restart after downtime)

**Request payload:**
```json
{ "Username": "employee", "Password": "Employee@123" }
```

**Expected response (HTTP 200):**
```json
{
  "token": "<JWT Bearer token>",
  "username": "employee",
  "role": "Employee",
  "expiresAt": "..."
}
```

---

## Folder Structure

```
tests/k6/
├── stress_test.js       ← Gradual ramp-up stress test on POST /api/auth/login
├── spike_test.js        ← Instant burst spike test on POST /api/auth/login
├── helpers/
│   └── auth.js          ← Pre-flight login validator (called in setup())
└── README.md            ← This file
```

---

## Prerequisites

### 1. Install K6

```powershell
# Option A — via winget (recommended on Windows)
winget install k6 --source winget

# Option B — via Chocolatey
choco install k6

# Verify installation
k6 version
```

### 2. Start the Backend Services

The tests target the **API Gateway on port 5000**, which proxies to the **AuthService on port 5001**.

```powershell
# From ERP_backend root — start with Docker Compose (local profile)
docker-compose -f docker-compose.local.yml up
```

Or start the ApiGateway and AuthService individually from their respective `src/` directories.

### 3. Configure Test Credentials

Both test files use:
```js
{ Username: 'employee', Password: 'Employee@123' }
```

Update these values in `stress_test.js` and `spike_test.js` to match a real Employee account in your local database.

---

## Running the Tests

### Stress Test (~1m 12s)
```powershell
k6 run tests/k6/stress_test.js
```

### Spike Test (~40s)
```powershell
k6 run tests/k6/spike_test.js
```

### With JSON output (for submission evidence)
```powershell
k6 run --out json=stress_results.json tests/k6/stress_test.js
k6 run --out json=spike_results.json  tests/k6/spike_test.js
```

---

## What Each Test Does

### `stress_test.js` — Stress Testing on Employee Login

Gradually increases Virtual Users (VUs) from **10 → 50 → 100 → 200** over ~72 seconds, simulating a growing number of employees logging in during a busy shift.

| Stage | Duration | VUs | Purpose |
|-------|----------|-----|---------|
| Warm-up | 10s | 10 | A few employees logging in — baseline |
| Normal Load | 15s | 50 | Typical shift start traffic |
| Stress Begins | 15s | 100 | Above designed login capacity |
| Heavy Stress | 15s | 200 | Extreme load — find the breaking point |
| Ramp Down | 17s | 0 | Recovery observation |

**Custom Metrics:**
- `login_duration_ms` — Trend of response times for every `POST /api/auth/login` call
- `stress_error_rate` — Rate of failed `check()` assertions

**Checks (assertions per request):**
- `login — HTTP 200` — login returned success
- `login — token in response` — a valid JWT token was actually issued
- `login — under 2000ms` — login responded within the time limit

**Thresholds (pass/fail criteria):**
- `p(95) < 2000ms` on all login requests
- `p(90) < 1500ms` on the custom `login_duration_ms` metric
- Error rate `< 10%`

---

### `spike_test.js` — Spike Testing on Employee Login

Instantly jumps from **5 → 200 VUs in just 3 seconds**, simulating all employees logging in at the same moment (e.g. start of a work shift or system restart).

| Stage | Duration | VUs | Purpose |
|-------|----------|-----|---------|
| Baseline | 5s | 5 | Calm — a few employees logging in |
| **SPIKE** | 3s | 200 | Instant surge — shift start scenario |
| Hold | 15s | 200 | System under extreme concurrent login load |
| **DROP** | 3s | 5 | Surge ends — most employees are in |
| Recovery | 10s | 5 | Observe system returning to normal |
| Wind Down | 4s | 0 | Test teardown |

**Custom Metrics:**
- `login_spike_duration_ms` — Trend of response times during the spike
- `spike_error_rate` — Rate of failed checks
- `successful_logins_after_spike` — Counter of HTTP 200 responses with a valid token (rising count after the drop = system has recovered)

**Checks (assertions per request):**
- `login — 200 or 503 (no crash)` — server responded gracefully (200=OK, 503=overloaded but alive)
- `login — not a 500 (server error)` — server did not crash internally
- `login — responded within 5s` — never completely timed out

**Thresholds:**
- `p(95) < 3000ms` (relaxed for spike — degradation is expected)
- `p(99) < 5000ms` (absolute worst case)
- Spike error rate `< 20%`
- HTTP failure rate `< 30%` (system must not fully crash)

---

## Key Concepts (VIVA Reference)

| Concept | Explanation |
|---------|-------------|
| **Virtual User (VU)** | A simulated concurrent user that repeatedly runs the `default()` function |
| **Stage** | A `{ duration, target }` pair defining how K6 ramps VUs over time |
| **Threshold** | A pass/fail assertion on a metric — the test fails if breached |
| **Trend** | K6 metric type — records timing values; exposes `avg`, `p90`, `p95`, `p99` |
| **Rate** | K6 metric type — records a fraction (pass/fail ratio) |
| **Counter** | K6 metric type — monotonically increasing count |
| **setup()** | Runs once before any VU starts — used here to validate the endpoint is reachable |
| **p95 latency** | 95% of requests completed faster than this value |
| **503 vs 500** | 503 = server overloaded but alive; 500 = server crashed. Spike tests accept 503. |
| **Stress vs Spike** | Stress = slow gradual ramp to find breaking point. Spike = instant surge to test resilience. |
