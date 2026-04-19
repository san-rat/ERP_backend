# Microservice Architecture Guide — InsightERP Backend

This document outlines all microservices in the InsightERP backend system, their responsibilities, local development ports, container ports, and Azure deployment details.

---

## Local Development Architecture

```
                        ┌─────────────────────┐
                        │     API Gateway      │
                        │   localhost:5000     │
                        │  (Ocelot Proxy)      │
                        └──────────┬──────────┘
                                   │
          ┌────────────────────────┼────────────────────────┐
          │                        │                         │
   ┌──────▼──────┐        ┌────────▼───────┐       ┌────────▼───────┐
   │ AuthService │        │CustomerService │       │  OrderService  │
   │   :5001     │        │    :5002       │       │    :5003       │
   └─────────────┘        └────────────────┘       └────────────────┘

   ┌─────────────┐        ┌────────────────┐       ┌────────────────┐
   │ProductService│       │AnalyticsService│       │ForecastService │
   │   :5004     │        │    :5007       │       │    :5005       │
   └─────────────┘        └────────────────┘       └────────────────┘

                          ┌────────────────┐
                          │PredictionService│
                          │    :5006       │
                          └────────────────┘
```

> All services listen on port `8080` inside Docker containers.
> The ports above (5000–5007) are only used during local development with `dotnet run`.

---

## Azure Cloud Architecture (Deployed)

```
                     ┌──────────────────────────────────────┐
                     │          VERCEL FRONTEND              │
                     │     (React app — your-app.vercel.app) │
                     └──────────────────────────────────────┘
                                       │
                              HTTPS (public internet)
                                       │
                     ┌─────────────────▼────────────────────┐
                     │           API GATEWAY                 │
                     │   apigateway-dev.victoriouscliff-     │
                     │   19d215bb.southeastasia.             │
                     │   azurecontainerapps.io               │
                     │      (External Ingress — public)      │
                     └─────────────────┬────────────────────┘
                                       │
                    Azure Container Apps Internal Network
                    (HTTPS — only reachable within Azure)
                                       │
          ┌────────────────────────────┼──────────────────────────┐
          │                            │                           │
   ┌──────▼──────────┐    ┌────────────▼──────┐    ┌─────────────▼──────┐
   │  authservice-dev │    │customerservice-dev│    │ orderservice-dev   │
   │  (external too) │    │   (internal only) │    │  (internal only)   │
   └──────────────────┘    └───────────────────┘    └────────────────────┘

   ┌──────────────────┐    ┌───────────────────┐    ┌────────────────────┐
   │productservice-dev│    │analyticsservice-  │    │forecastservice-dev │
   │ (internal only) │    │dev (internal only)│    │  (internal only)   │
   └──────────────────┘    └───────────────────┘    └────────────────────┘

                           ┌───────────────────┐
                           │predictionservice- │
                           │dev (internal only)│
                           └───────────────────┘
```

> **External** = reachable from the internet (browser/frontend can call it directly)
> **Internal** = only reachable from within the Azure Container Apps environment (other services or the gateway)

---

## Services

### 1. API Gateway
| Property | Value |
|---|---|
| **Path** | `src/ApiGateway` |
| **Framework** | Ocelot reverse proxy |
| **Local Port (HTTP)** | `5000` |
| **Container Port** | `8080` |
| **Azure App Name** | `apigateway-dev` |
| **Azure Ingress** | External (public) |
| **Azure URL** | `https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io` |
| **Health Endpoint** | `GET /health` |
| **Swagger** | `GET /swagger` |
| **Responsibility** | Single entry point — routes all incoming requests to the correct downstream microservice. Validates JWT tokens on protected routes. |

---

### 2. AuthService
| Property | Value |
|---|---|
| **Path** | `src/AuthService` |
| **Local Port (HTTP)** | `5001` |
| **Container Port** | `8080` |
| **Azure App Name** | `authservice-dev` |
| **Azure Ingress** | External (public) |
| **Azure URL** | `https://authservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io` |
| **Health Endpoint** | `GET /health` |
| **Swagger** | `GET /swagger` |
| **Responsibility** | Handles user authentication. Issues JWT tokens on login and registration. Pre-seeded accounts: `admin`, `manager`, `employee`. |

---

### 3. CustomerService
| Property | Value |
|---|---|
| **Path** | `src/CustomerService` |
| **Local Port (HTTP)** | `5002` |
| **Container Port** | `8080` |
| **Azure App Name** | `customerservice-dev` |
| **Azure Ingress** | External |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | External ecommerce backend — customer auth, addresses, cart, storefront catalog, checkout, and customer-safe order lifecycle |

---

### 4. OrderService
| Property | Value |
|---|---|
| **Path** | `src/OrderService` |
| **Local Port (HTTP)** | `5003` |
| **Container Port** | `8080` |
| **Azure App Name** | `orderservice-dev` |
| **Azure Ingress** | Internal only |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Manages the full order lifecycle — creation, status tracking, history |

---

### 5. ProductService
| Property | Value |
|---|---|
| **Path** | `src/ProductService` |
| **Local Port (HTTP)** | `5004` |
| **Container Port** | `8080` |
| **Azure App Name** | `productservice-dev` |
| **Azure Ingress** | Internal only |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Manages product catalog — listings, inventory, pricing |

---

### 6. ForecastService
| Property | Value |
|---|---|
| **Path** | `src/ForecastService` |
| **Local Port (HTTP)** | `5005` |
| **Container Port** | `8080` |
| **Azure App Name** | `forecastservice-dev` |
| **Azure Ingress** | Internal only |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Generates demand and sales forecasts using historical data trends |

---

### 7. PredictionService
| Property | Value |
|---|---|
| **Path** | `src/PredictionService` |
| **Local Port (HTTP)** | `5006` |
| **Container Port** | `8080` |
| **Azure App Name** | `predictionservice-dev` |
| **Azure Ingress** | Internal only |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | ML-driven predictions: churn, demand, customer segmentation |

---

### 8. AnalyticsService
| Property | Value |
|---|---|
| **Path** | `src/AnalyticsService` |
| **Local Port (HTTP)** | `5007` |
| **Container Port** | `8080` |
| **Azure App Name** | `analyticsservice-dev` |
| **Azure Ingress** | Internal only |
| **Health Endpoint** | `GET /health` |
| **Responsibility** | Aggregates business data — dashboards, KPI metrics, reports |

---

## Port Reference Summary

| Service | Local HTTP | Container | Azure App Name | Access |
|---|---|---|---|---|
| ApiGateway | `5000` | `8080` | `apigateway-dev` | External |
| AuthService | `5001` | `8080` | `authservice-dev` | External |
| CustomerService | `5002` | `8080` | `customerservice-dev` | External |
| OrderService | `5003` | `8080` | `orderservice-dev` | Internal |
| ProductService | `5004` | `8080` | `productservice-dev` | Internal |
| ForecastService | `5005` | `8080` | `forecastservice-dev` | Internal |
| PredictionService | `5006` | `8080` | `predictionservice-dev` | Internal |
| AnalyticsService | `5007` | `8080` | `analyticsservice-dev` | Internal |

> **Rule:** All containers listen on `8080`. Local ports (5000–5007) are set in `Properties/launchSettings.json` and are used only during `dotnet run`. In Azure, each service gets its own unique hostname — port conflicts are not an issue.

---

## Azure Infrastructure

| Resource | Name | Details |
|---|---|---|
| Resource Group | `erp-rg` | Southeast Asia region |
| Container Apps Environment | `erp-dev-env` | Shared network for all 8 services |
| Container Registry | ACR (private) | Stores all 8 Docker images |
| Azure SQL Server | `insighterp-sqlserver.database.windows.net` | Hosts `insighterp_db` |
| Managed Identity | `erp-github-mi` | Used for OIDC GitHub Actions deployment |

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 (.NET 9) |
| API Gateway | Ocelot `24.1.0` |
| API Docs | Swashbuckle (Swagger UI) `6.9.0` |
| Containerisation | Docker (multi-stage build) |
| Base Images | `mcr.microsoft.com/dotnet/sdk:9.0` (build), `mcr.microsoft.com/dotnet/aspnet:9.0` (runtime) |
| CI/CD | GitHub Actions |
| Image Registry | Azure Container Registry (ACR) |
| Cloud Hosting | Azure Container Apps |
| Database | Azure SQL Database (`insighterp_db`) with per-service schemas |
| Auth | JWT Bearer tokens (HMAC-SHA256) |
| Azure Auth | OIDC (OpenID Connect) — passwordless |
