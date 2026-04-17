# Docker ML Routing and Swagger Fix

**Project:** InsightERP  
**Date:** April 16, 2026  
**Scope:** Docker-local stack behind `http://localhost:5000`

This document explains the issue that broke Customer Insights, Product Insights, and some ML Swagger flows in Docker, and what was changed to fix it without renaming the microservice controllers.

## Summary

The main problem was not that the ML services could not reach the database.

The actual failure was a contract mismatch between:

- the frontend API clients
- the ApiGateway Ocelot route mappings
- the service-local Swagger documents being exposed through the gateway

Because of that mismatch:

- Customer Insights was sending churn requests to the wrong URL
- Product Insights was sending a forecast payload that the backend rejected
- Docker gateway routes for PredictionService pointed to downstream paths that do not exist
- `/forecast/swagger` and `/prediction/swagger` loaded HTML, but Swagger "Try it out" used invalid paths from the gateway host and returned `404`

## User-Visible Symptoms

The broken behavior in Docker included:

- repeated `404` errors for `POST /api/ml/churn/churn`
- `400 Bad Request` for `POST /api/ml/forecast/forecast/product`
- ML endpoints failing from the frontend even when the same services seemed healthy when run locally outside Docker
- `/prediction/swagger` and `/forecast/swagger` opening in the browser, but interactive Swagger requests failing

## What Was Not The Root Cause

The Docker investigation showed the ML services themselves were reachable and the DB path was working:

- PredictionService `GET /db-check` returned `200`
- ForecastService analytics and retraining endpoints returned `200`
- Forecast generation returned `200` when called with the correct payload

So the main issue was not "Docker cannot access SQL Server" for these flows.

## Root Cause

### 1. Frontend churn URL mismatch

The frontend churn client called:

- `POST /api/ml/churn/churn`

The backend contract is:

- `POST /api/ml/churn`

That caused the repeated `404` errors in Customer Insights.

### 2. Frontend forecast payload mismatch

The frontend forecast generator sent:

- `confidenceLevel: 0.95`

The ForecastService contract expects an integer percentage form:

- `confidenceLevel: 95`

That caused the `400 Bad Request` on forecast generation.

### 3. Docker gateway route drift for PredictionService

The Docker Ocelot file contained incorrect downstream mappings:

- `/api/ml/churn/{everything}` -> `/api/ml/predictions/{everything}`
- `/api/ml/model-management/{everything}` -> `/api/ml/modelmanagement/{everything}`

Those downstream paths do not match the implemented PredictionService routes.

### 4. Non-Docker Ocelot files drifted from Docker

The non-Docker route files also had inconsistent Forecast routing.  
`/api/ml/forecast/{everything}` was not consistently mapped to the real downstream ForecastService path:

- `/api/forecasting/{everything}`

That created environment drift and made the contract fragile across local, Docker, dev, and production.

### 5. Raw downstream Swagger was misleading behind the gateway

The proxied service Swagger pages exposed the downstream OpenAPI as-is.

Examples:

- PredictionService publishes `/db-check`, `/api/ml/status`, `/api/ml/retrain`
- ForecastService publishes `/api/forecasting/...`

When those docs were opened through `http://localhost:5000`, Swagger UI still tried to call the downstream-local paths directly. Those are not valid gateway upstream paths, so "Try it out" returned `404`.

### 6. Customer Insights swallowed partial failures

Customer Insights used `Promise.allSettled(...)` and did not surface a clear UI error when churn calls failed.  
That made the page look broken without showing one obvious failure state.

## Fix Strategy

The fix was intentionally biased toward:

- minimal backend controller change
- gateway and frontend adaptation
- keeping the public service Swagger URLs alive

Microservice controller routes were left unchanged.

## What Was Changed

### Frontend

Updated the frontend API contract to match the actual gateway and service routes:

- `src/api/mlClient.js`
  - changed churn prediction to `POST /api/ml/churn`
- `src/api/forecastingClient.js`
  - kept forecast traffic on `/api/ml/forecast/*`
  - changed forecast generation payload to `confidenceLevel: 95`
- `src/pages/manager/CustomerInsightsPage.jsx`
  - added visible churn-analysis error reporting instead of silent failure
- updated API client tests to assert the corrected URLs and payloads

### ApiGateway Ocelot Contract

Aligned all gateway configs:

- `src/ApiGateway/ocelot.json`
- `src/ApiGateway/ocelot.Docker.json`
- `src/ApiGateway/ocelot.Dev.json`
- `src/ApiGateway/ocelot.Production.json`

Key routing fixes:

- `/api/ml/forecast/{everything}` -> `/api/forecasting/{everything}`
- `/api/ml/churn/{everything}` -> `/api/ml/churn/{everything}`
- `/api/ml/model-management/{everything}` -> `/api/ml/{everything}`
- `/api/ml/{everything}` -> `/api/ml/{everything}`
- `/auth/db-check` -> `/db-check`
- `/prediction/db-check` -> `/db-check`

Swagger proxy routes were also standardized for:

- `/auth/swagger/{everything}`
- `/customer/swagger/{everything}`
- `/order/swagger/{everything}`
- `/product/swagger/{everything}`
- `/forecast/swagger/{everything}`
- `/prediction/swagger/{everything}`
- `/analytics/swagger/{everything}`
- `/admin/swagger/{everything}`

### Gateway-Owned Swagger Pages

Added a gateway-side Swagger controller so the following URLs remain valid and interactive:

- `/swagger`
- `/apigateway/swagger`
- `/auth/swagger`
- `/customer/swagger`
- `/order/swagger`
- `/product/swagger`
- `/forecast/swagger`
- `/prediction/swagger`
- `/analytics/swagger`
- `/admin/swagger`

Instead of using the raw downstream HTML as the main entry page, the gateway now:

- serves its own Swagger UI page for each service
- fetches the downstream `swagger/v1/swagger.json`
- rewrites the `servers` block to the gateway host
- rewrites `paths` to the correct gateway upstream routes
- applies Bearer security only to protected operations
- leaves public health and db-check operations public in the docs

This keeps the service URLs the user expects while making Swagger "Try it out" work correctly through the gateway.

### Route-Contract Regression Coverage

Added tests so the Ocelot files do not drift again:

- `tests/src/ApiGateway.Tests/OcelotRouteContractTests.cs`

This checks that the ML, db-check, and Swagger-critical aliases exist in all four Ocelot files and that specific routes appear before the generic `/api/ml/{everything}` route.

## Files Most Relevant To The Fix

Backend:

- `src/ApiGateway/Controllers/ServiceSwaggerController.cs`
- `src/ApiGateway/Controllers/SwaggerRedirectsController.cs`
- `src/ApiGateway/Controllers/GatewayController.cs`
- `src/ApiGateway/program.cs`
- `src/ApiGateway/ocelot.json`
- `src/ApiGateway/ocelot.Docker.json`
- `src/ApiGateway/ocelot.Dev.json`
- `src/ApiGateway/ocelot.Production.json`
- `tests/src/ApiGateway.Tests/OcelotRouteContractTests.cs`

Frontend:

- `../ERP_frontend/src/api/mlClient.js`
- `../ERP_frontend/src/api/forecastingClient.js`
- `../ERP_frontend/src/pages/manager/CustomerInsightsPage.jsx`
- `../ERP_frontend/src/api/__tests__/mlClient.test.js`
- `../ERP_frontend/src/api/__tests__/forecastingClient.test.js`

## Verification Completed

The fix was verified in the Docker-local workflow on April 16, 2026.

### Build Verification

- `docker compose ... build apigateway` succeeded

### Swagger Verification

These URLs returned `200`:

- `http://localhost:5000/swagger`
- `http://localhost:5000/apigateway/swagger`
- `http://localhost:5000/auth/swagger`
- `http://localhost:5000/customer/swagger`
- `http://localhost:5000/order/swagger`
- `http://localhost:5000/product/swagger`
- `http://localhost:5000/forecast/swagger`
- `http://localhost:5000/prediction/swagger`
- `http://localhost:5000/analytics/swagger`
- `http://localhost:5000/admin/swagger`

Also verified:

- `/forecast/swagger/v1/swagger.json` exposes gateway-valid `/api/ml/forecast/...` paths
- `/prediction/swagger/v1/swagger.json` exposes gateway-valid `/api/ml/...` and `/prediction/db-check` paths
- rewritten OpenAPI docs use `http://localhost:5000` as the server URL

### Public Diagnostic Verification

These returned `200` without requiring a token:

- `GET /auth/db-check`
- `GET /prediction/db-check`

### Authenticated Gateway Smoke Verification

Verified with seeded local users:

- `manager / Admin@123`
- `admin / Admin@123`

Manager flow checks:

- `GET /api/products/alerts`
- `GET /api/ml/status`
- `POST /api/ml/retrain`
- `GET /api/ml/forecast/retraining/status`
- `POST /api/ml/forecast/forecast/product`
- `POST /api/ml/churn`
- `GET /api/ml/model-management/status`
- `POST /api/ml/model-management/retrain`

Admin flow checks:

- `GET /api/admin/dashboard/overview`
- `GET /api/admin/users`

All of the above returned `200`.

## Expected Behavior After The Fix

After these changes:

- Customer Insights should stop spamming `404` churn requests
- Product Insights should load product metrics from Docker through the gateway
- Forecast generation should stop returning `400` for the default frontend payload
- `/forecast/swagger` and `/prediction/swagger` should remain available and Swagger "Try it out" should call valid gateway routes
- local, Docker, dev, and production gateway configs should use the same ML route contract

## Remaining Limitation

One local verification issue still exists outside the routing fix itself:

- the frontend Vitest run is currently blocked by a missing Rollup optional dependency: `@rollup/rollup-linux-x64-gnu`

That affects local frontend test execution only. It does not change the Docker gateway fix described in this document.
