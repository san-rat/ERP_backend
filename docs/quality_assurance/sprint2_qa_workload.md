# Sprint 2 — QA Workload

**Branch:** `IMS-205-qa-start`  
**Role:** Quality Assurance  
**Sprint Duration:** Sprint 2  
**Last Updated:** 2026-03-13

---

## Overview

This document outlines the QA responsibilities and planned tasks for Sprint 2. Unit testing remains the responsibility of the developers. The QA role covers integration testing, end-to-end (E2E) automation, test documentation, and CI/CD gating.

> **Note:** The system uses **Swagger** for interactive API exploration and manual endpoint verification. Postman is not required.

---

## Planned Tasks

### 1. Manual API Testing via Swagger

**Goal:** Verify that each microservice exposes correct API behavior through the Ocelot API Gateway.

**Scope:**
- `AuthService` — Login, Register, Token refresh, Unauthorized access
- `OrderService` — Create, Read, Update, Delete orders
- `ProductService` — CRUD operations, stock validation
- `CustomerService` — CRUD operations
- `ApiGateway` — Correct routing to downstream services, rate limiting

**Test Checks:**
- Correct HTTP status codes (`200`, `201`, `400`, `401`, `404`, `500`)
- Response body matches expected schema
- Edge cases: empty body, missing fields, invalid IDs, expired/missing JWT

**Output:** Completed test case log (see Section 5)

---

### 2. End-to-End (E2E) Testing with Selenium

**Goal:** Automate critical user flows on the frontend that interact with the backend APIs.

**Tech Stack:** Selenium + C# + NUnit

**Project Location:** `tests/E2E/`

**Planned Test Flows:**

| Flow | Steps |
|---|---|
| Login Flow | Navigate to login → Enter credentials → Verify redirect |
| Unauthorized Access | Access protected page without login → Expect redirect to login |
| Token Expiry | Simulate expired session → Verify logout/redirect behavior |
| Order Flow | Login → Browse products → Place order → Verify order history |

**Setup Tasks:**
- [ ] Scaffold Selenium + NUnit project under `tests/E2E/`
- [ ] Configure WebDriver (ChromeDriver)
- [ ] Write page object models (POM) for Login, Dashboard, Order pages
- [ ] Implement test cases listed above

---

### 3. Integration Testing (Cross-Service Flows)

**Goal:** Verify that microservices work correctly together through the API Gateway.

**Tech Stack:** xUnit + `HttpClient` / `WebApplicationFactory`

**Project Location:** `tests/Integration/`

**Planned Test Scenarios:**

| Scenario | Services Involved |
|---|---|
| Authenticated request reaches downstream service | ApiGateway + AuthService |
| Place an order end-to-end | AuthService + ProductService + OrderService |
| Unauthenticated request is blocked at gateway | ApiGateway + AuthService |
| Invalid product ID in order | OrderService + ProductService |

**Setup Tasks:**
- [ ] Scaffold integration test project under `tests/Integration/`
- [ ] Configure test HTTP client to point at the API Gateway
- [ ] Implement scenarios listed above

---

### 4. Test Documentation

**Goal:** Maintain a structured record of all test cases, results, and defects.

**Deliverables:**

- **Test Case Document** — Covers all manual and automated test cases with:
  - Feature being tested
  - Input / Preconditions
  - Expected result
  - Actual result
  - Pass / Fail / Blocked status

- **Defect Log** — Filed as GitHub Issues with labels:
  - `bug` — confirmed defect
  - `qa-blocked` — QA blocked on a developer fix
  - `question` — clarification needed from dev team

---

### 5. CI/CD — Automated Test Gating

**Goal:** Ensure QA tests run automatically on every pull request and block merges on failure.

**Pipeline Location:** `.github/workflows/`

**Tasks:**
- [ ] Add a dedicated QA job in the GitHub Actions workflow
- [ ] Run Selenium E2E tests on PR to `main` / `develop`
- [ ] Run integration tests on PR to `main` / `develop`
- [ ] Configure pipeline to fail PR if any QA test fails

---

### 6. Basic Security Testing

**Goal:** Verify that the system enforces authentication and handles malicious input gracefully.

**Checks:**
- Access protected endpoints without a JWT → expect `401`
- Send malformed/expired JWT → expect `401`
- Submit unexpected input types (e.g., strings in numeric fields, SQL injection-style payloads)
- Verify `AuthService` password hashing is not reversible
- Confirm no sensitive data (connection strings, secrets) is returned in API error responses

---

## Test Case Log Template

| # | Service | Endpoint | Input | Expected | Actual | Status |
|---|---|---|---|---|---|---|
| 1 | AuthService | `POST /auth/login` | Valid credentials | `200 + JWT token` | | |
| 2 | AuthService | `POST /auth/login` | Wrong password | `401 Unauthorized` | | |
| 3 | OrderService | `POST /orders` | Valid order, valid JWT | `201 Created` | | |
| 4 | OrderService | `POST /orders` | No JWT | `401 Unauthorized` | | |
| 5 | ProductService | `GET /products/{id}` | Invalid ID | `404 Not Found` | | |
| 6 | ApiGateway | Any route | No JWT | `401 Unauthorized` | | |

> Expand this table as new endpoints are implemented during Sprint 2.

---

## Folder Structure (Target)

```
tests/
├── E2E/
│   └── ERP.E2E.Tests/
│       ├── Pages/          # Page Object Models
│       └── Tests/          # Selenium test classes
├── Integration/
│   └── ERP.Integration.Tests/
│       └── Scenarios/      # Cross-service test scenarios
```

---

## Out of Scope for Sprint 2

- Performance / load testing (planned for a later sprint)
- Full security penetration testing (basic checks only this sprint)
- Mobile testing
