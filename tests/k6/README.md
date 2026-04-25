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

## Folder Structure

```
tests/k6/
├── stress_test.js       ← Gradual ramp-up to find the system breaking point
├── spike_test.js        ← Sudden burst to test resilience and recovery
├── helpers/
│   └── auth.js          ← Shared JWT login helper (called in setup())
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

The tests target the **API Gateway on port 5000**, which proxies to the **ProductService on port 5004** and **AuthService on port 5001**.

```powershell
# From ERP_backend root — start with Docker Compose (local profile)
docker-compose -f docker-compose.local.yml up
```

Or start each service individually from their respective `src/` directories.

### 3. Configure Test Credentials

Open `helpers/auth.js` and update the default `email` and `password` parameters to match a real test account in your local database.

---

## Running the Tests

### Stress Test
```powershell
k6 run tests/k6/stress_test.js
```

### Spike Test
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

### `stress_test.js` — Stress Testing
Gradually increases Virtual Users (VUs) from **10 → 50 → 100 → 200** over ~4 minutes.

| Stage | Duration | VUs | Purpose |
|-------|----------|-----|---------|
| Warm-up | 30s | 10 | Establish baseline |
| Normal Load | 1m | 50 | Typical peak traffic |
| Stress Begins | 1m | 100 | Above designed capacity |
| Heavy Stress | 1m | 200 | Find the breaking point |
| Ramp Down | 30s | 0 | Recovery observation |

**Custom Metrics:**
- `product_list_duration_ms` — Trend of response times for `GET /api/products`
- `stock_duration_ms` — Trend of response times for `GET /api/products/stock`
- `stress_error_rate` — Rate of failed `check()` assertions

**Thresholds (pass/fail criteria):**
- `p(95) < 2000ms` on all requests
- `p(90) < 1500ms` on product list specifically
- Error rate `< 10%`

---

### `spike_test.js` — Spike Testing
Instantly jumps from **5 → 200 VUs in just 5 seconds**, holds, then drops back.

| Stage | Duration | VUs | Purpose |
|-------|----------|-----|---------|
| Baseline | 15s | 5 | Calm normal traffic |
| **SPIKE** | 5s | 200 | Instant surge (flash sale scenario) |
| Hold | 30s | 200 | System under extreme load |
| **DROP** | 5s | 5 | Load disappears instantly |
| Recovery | 30s | 5 | Observe return to normal |
| Wind Down | 10s | 0 | Test teardown |

**Custom Metrics:**
- `spike_response_duration_ms` — Trend tracking all response times during spike
- `spike_error_rate` — Rate of failed checks
- `successful_requests_after_spike` — Counter tracking 200 OK responses (shows recovery curve)

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
| **setup()** | Runs once before any VU starts — used here to acquire a JWT token once |
| **p95 latency** | 95% of requests completed faster than this value |
| **503 vs 500** | 503 = server overloaded but alive; 500 = server crashed. Spike tests accept 503. |
