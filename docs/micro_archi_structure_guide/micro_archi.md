# Microservice Architecture Guide

This document outlines all microservices in the ERP backend system, their responsibilities, local development ports, and container ports.

---

## Architecture Overview

```
                        ┌─────────────────┐
                        │   API Gateway   │
                        │  :5279 / :7126  │
                        └────────┬────────┘
                                 │
          ┌──────────────────────┼──────────────────────┐
          │                      │                       │
   ┌──────▼──────┐       ┌───────▼──────┐       ┌───────▼───────┐
   │ AuthService │       │CustomerService│       │ OrderService  │
   │    :5288    │       │    :5071      │       │    :5113      │
   └─────────────┘       └──────────────┘       └───────────────┘
          │                      │                       │
   ┌──────▼──────┐       ┌───────▼──────┐       ┌───────▼───────┐
   │ProductService│      │AnalyticsService│      │ForecastService│
   │    :5038    │       │    :5199      │       │    :5044      │
   └─────────────┘       └──────────────┘       └───────────────┘
                                 │
                        ┌────────▼────────┐
                        │PredictionService│
                        │     :5197       │
                        └─────────────────┘
```

---

## Services

### 1. API Gateway
| Property | Value |
|---|---|
| **Path** | `src/ApiGateway` |
| **Local Port (HTTP)** | `5279` |
| **Local Port (HTTPS)** | `7126` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Single entry point — routes all incoming requests to the appropriate downstream microservices |

---

### 2. AuthService
| Property | Value |
|---|---|
| **Path** | `src/AuthService` |
| **Local Port (HTTP)** | `5288` |
| **Local Port (HTTPS)** | `7009` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Handles user authentication and JWT token issuance/validation |

---

### 3. CustomerService
| Property | Value |
|---|---|
| **Path** | `src/CustomerService` |
| **Local Port (HTTP)** | `5071` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Manages customer data — creation, retrieval, updates, and deletion of customer records |

---

### 4. OrderService
| Property | Value |
|---|---|
| **Path** | `src/OrderService` |
| **Local Port (HTTP)** | `5113` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Manages the full order lifecycle — order creation, status tracking, and order history |

---

### 5. ProductService
| Property | Value |
|---|---|
| **Path** | `src/ProductService` |
| **Local Port (HTTP)** | `5038` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Manages product catalog — product listings, inventory, pricing, and product details |

---

### 6. AnalyticsService
| Property | Value |
|---|---|
| **Path** | `src/AnalyticsService` |
| **Local Port (HTTP)** | `5199` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Aggregates and processes business data to produce reports, dashboards, and KPI metrics |

---

### 7. ForecastService
| Property | Value |
|---|---|
| **Path** | `src/ForecastService` |
| **Local Port (HTTP)** | `5044` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Generates demand and sales forecasts using historical data trends |

---

### 8. PredictionService
| Property | Value |
|---|---|
| **Path** | `src/PredictionService` |
| **Local Port (HTTP)** | `5197` |
| **Container Port** | `8080` |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Provides ML-driven predictions (e.g. churn, demand, risk scoring) to support business decisions |

---

## Port Reference Summary

| Service | Local HTTP Port | Local HTTPS Port | Container Port |
|---|---|---|---|
| ApiGateway | `5279` | `7126` | `8080` |
| AuthService | `5288` | `7009` | `8080` |
| CustomerService | `5071` | — | `8080` |
| OrderService | `5113` | — | `8080` |
| ProductService | `5038` | — | `8080` |
| AnalyticsService | `5199` | — | `8080` |
| ForecastService | `5044` | — | `8080` |
| PredictionService | `5197` | — | `8080` |

> **Note:** All services use port `8080` internally inside Docker containers. The local ports are only used during development (`dotnet run`). In production (Azure Container Apps), each service gets its own unique hostname — port conflicts are not an issue.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 (.NET 9) |
| API Docs | Swashbuckle (Swagger UI) — `6.9.0` |
| Containerization | Docker (multi-stage build) |
| Base Images | `mcr.microsoft.com/dotnet/sdk:9.0` (build), `mcr.microsoft.com/dotnet/aspnet:9.0` (runtime) |
| Target Hosting | Azure Container Apps |
