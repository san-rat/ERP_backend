# K6 Data-Driven Payload Testing — ERP AuthService

**Module:** SE3112 - Advanced Software Engineering  
**Tool:** K6 (Load & Performance Testing Category)  
**Feature:** Data-Driven Payload Testing from CSV  
**Student:** [Your Name] — Student 4  
**Endpoints Tested:** `POST /api/auth/login` · `POST /api/auth/register` · `GET /health`

---

## What This Does

Instead of hardcoding test values in the code, all test inputs are stored in CSV files.
K6 reads each row and sends a separate HTTP request — automatically covering valid users,
invalid credentials, missing fields, role violations, and edge cases.

After a successful login, the test extracts the JWT token and reuses it in an
authenticated follow-up request — testing the full auth workflow end-to-end.

---

## Folder Structure

```
k6-tests/
├── data/
│   ├── users.csv           ← 10 login scenarios
│   └── register.csv        ← 7 registration scenarios
├── data-driven-test.js     ← Main K6 test file
└── README.md               ← This file
```

---

## How to Run

### Step 1 — Start AuthService
```bash
# Option A: dotnet
cd src/AuthService
dotnet run

# Option B: Docker
docker-compose up authservice
```

### Step 2 — Run the K6 test
```bash
cd k6-tests
k6 run data-driven-test.js
```

---

## CSV Scenarios

### users.csv — Login Test Data (10 rows)

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Valid admin login | 200 ✅ |
| 2 | Valid manager login | 200 ✅ |
| 3 | Valid employee login | 200 ✅ |
| 4 | Invalid username | 401 ❌ |
| 5 | Wrong password | 401 ❌ |
| 6 | Completely fake credentials | 401 ❌ |
| 7 | Nonexistent user | 401 ❌ |
| 8 | Short wrong password | 401 ❌ |
| 9 | Random fake user | 401 ❌ |
| 10 | Uppercase username (case sensitivity) | 401 ❌ |

### register.csv — Registration Test Data (7 rows)

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Valid employee registration | 201 ✅ |
| 2 | Valid manager registration | 201 ✅ |
| 3 | Duplicate username conflict | 409 ❌ |
| 4 | Missing username field | 400 ❌ |
| 5 | Missing password field | 400 ❌ |
| 6 | Self-assign Admin role (blocked) | 400 ❌ |
| 7 | Invalid email format | 400 ❌ |

---

## What Gets Checked Per Request (Assertions)

**Login checks:**
1. HTTP status matches expected value from CSV
2. Response time is under 3 seconds
3. Response body is never empty
4. JWT token is present in successful (200) responses
5. Error message is present in failed (401) responses
6. Content-Type is always application/json

**Registration checks:**
1. HTTP status matches expected value from CSV
2. Response time is under 4 seconds
3. 201 response returns JWT token immediately (auto-login)
4. 400 response returns a validation error message
5. 409 response returns a conflict message
6. Content-Type is always application/json

**Token reuse checks (after successful login):**
1. Authenticated request reaches the server
2. Token is not rejected with 403 Forbidden
3. Authorization header was correctly formed

---

## Key Technical Concepts Used

- **SharedArray** — loads CSV once into shared memory for all virtual users
- **__ITER** — iteration counter used to cycle through CSV rows
- **Custom Metrics** — Trend, Rate, Counter for detailed reporting
- **Tags** — labels requests for filtering in dashboards
- **Thresholds** — automated pass/fail gates (CI/CD compatible)
- **Multiple Scenarios** — login and register run as separate parallel scenarios
- **JWT Token Reuse** — extracted token is injected into follow-up request
- **handleSummary** — custom terminal output after test completes

---

## Group Division of Work

| Student | Tool | Feature |
|---------|------|---------|
| Student 1 | Vitest | [Their feature] |
| Student 2 | Vitest | [Their feature] |
| Student 3 | K6 | Spike/Stress Testing |
| **Student 4 (You)** | **K6** | **Data-Driven Payload Testing from CSV** |