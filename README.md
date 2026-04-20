# InsightERP — Backend

A .NET microservices backend for the InsightERP platform, using an Ocelot API Gateway, Azure SQL Server, and a React + Vite frontend deployed on Vercel.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API |
| API Gateway | Ocelot `24.1.0` |
| Auth | JWT Bearer tokens (HMAC-SHA256) |
| Database | Azure SQL Server / Azure SQL Database (single DB, per-service schemas) |
| Frontend | React + Vite (deployed on Vercel) |
| Containerization | Docker (multi-stage builds) |
| Image Registry | Azure Container Registry (ACR) |
| Cloud Hosting | Azure Container Apps |
| CI/CD | GitHub Actions |
| Azure Auth | OIDC — passwordless, no stored credentials |

---

## Architecture

```
Frontend (Vite :5173 locally / Vercel in cloud)
        │
        ├──────────────────────────────────────────┐
        ▼                                          ▼
API Gateway (:5000)                    CustomerService (:5002)
(ERP staff & internal traffic)         (ecommerce / customer-facing)
        │
        ├── AuthService        (:5001)
        ├── OrderService       (:5003)
        ├── ProductService     (:5004)
        ├── ForecastService    (:5005)
        ├── PredictionService  (:5006)
        ├── AnalyticsService   (:5007)
        └── AdminService       (:5011)
                │
                ▼
         Azure SQL DB      
```

> **CustomerService** runs independently from the API Gateway for ecommerce traffic. It has its own JWT auth stack, CORS policy, and direct database connection, and is exposed publicly on both local `:5002` and Azure.

---

## Services

| Service | Port | Responsibility |
|---|---|---|
| **ApiGateway** | 5000 | Single entry point — routes, JWT validation, Swagger aggregation |
| **AuthService** | 5001 | ERP staff login/register, JWT issuance |
| **CustomerService** | 5002 | Ecommerce customer auth, cart, products, orders, addresses, account |
| **OrderService** | 5003 | Internal ERP order lifecycle management |
| **ProductService** | 5004 | Internal ERP product catalog |
| **ForecastService** | 5005 | Demand forecasting |
| **PredictionService** | 5006 | ML-based predictions |
| **AnalyticsService** | 5007 | Business analytics and dashboard metrics |
| **AdminService** | 5011 | Staff/user management, admin dashboard overview |

---

## CustomerService — Ecommerce API

CustomerService is the customer-facing commerce service. It runs independently (not behind the API Gateway) and exposes the following endpoints:

| Controller | Endpoint | Description |
|---|---|---|
| Auth | `POST /api/commerce/auth/register` | Register a new customer account |
| Auth | `POST /api/commerce/auth/login` | Customer login, returns JWT |
| Auth | `GET /api/commerce/auth/profile` | Get current customer's profile |
| Account | `GET /api/commerce/account` | Get account details |
| Account | `PUT /api/commerce/account` | Update account details |
| Account | `DELETE /api/commerce/account` | Delete customer account |
| Addresses | `GET /api/commerce/addresses` | List saved addresses |
| Addresses | `POST /api/commerce/addresses` | Add a new address |
| Addresses | `PUT /api/commerce/addresses/{id}` | Update an address |
| Addresses | `DELETE /api/commerce/addresses/{id}` | Delete an address |
| Products | `GET /api/commerce/products` | Browse product catalog |
| Products | `GET /api/commerce/products/{id}` | Get product details |
| Cart | `GET /api/commerce/cart` | View cart |
| Cart | `POST /api/commerce/cart/items` | Add item to cart |
| Cart | `PUT /api/commerce/cart/items/{itemId}` | Update cart item quantity |
| Cart | `DELETE /api/commerce/cart/items/{itemId}` | Remove cart item |
| Cart | `DELETE /api/commerce/cart` | Clear entire cart |
| Orders | `POST /api/commerce/checkout` | Checkout and place order |
| Orders | `GET /api/commerce/orders` | List customer's orders |
| Orders | `GET /api/commerce/orders/{id}` | Get order details |
| Orders | `POST /api/commerce/orders/{id}/cancel` | Cancel an order |
| Health | `GET /health` | Service health check |

---

## Frontend

The frontend is a **React + Vite** single-page application stored in a separate repository (`ERP_frontend`).

### Running Locally

```powershell
# From the ERP_frontend folder
npm install
npm run dev
# Runs on http://localhost:5173
```

### Configuration

The frontend uses a single environment variable for all API calls:

```env
VITE_API_BASE_URL=http://localhost:5000      # local ERP/internal traffic
VITE_CUSTOMER_API_URL=http://localhost:5002  # local ecommerce traffic
```

In production (Vercel), these are set to the Azure Container Apps public URLs.

### Auth Flow — ERP Staff

```
Frontend → POST /api/auth/login (ApiGateway :5000)
        ← JWT returned
        → JWT stored in sessionStorage
        → Subsequent requests include Authorization: Bearer <token>
```

### Auth Flow — Ecommerce Customers

```
Frontend → POST /api/commerce/auth/login (CustomerService :5002)
        ← JWT returned (separate issuer/audience from ERP JWT)
        → JWT stored in sessionStorage
        → Subsequent requests to /api/commerce/* include Bearer token
```

### Deployment (Vercel)

- The frontend repo is connected to Vercel for automatic deployment on push.
- `VITE_API_BASE_URL` is set to the ApiGateway public Azure URL.
- `VITE_CUSTOMER_API_URL` is set to the CustomerService public Azure URL.
- No hardcoded URLs — all API calls use `import.meta.env.VITE_*`.

---

## 🌐 Live Environment (Azure — dev)

| Resource | URL |
|---|---|
| **API Gateway** | https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io |
| **AuthService** | https://authservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io |
| **CustomerService** | https://customerservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io |
| **Gateway Swagger** | https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/swagger |
| **Auth Swagger** | https://authservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/swagger |
| **Customer Swagger** | https://customerservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/swagger |
| **Gateway Health** | https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/auth/health |
| **Customer Health** | https://customerservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/health |

> All other services (Order, Product, Forecast, Prediction, Analytics, Admin) are **internal only** — accessible only through the API Gateway, not directly from the internet.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js](https://nodejs.org/) (for the frontend)

---

## 🚀 Starting the System Locally

`docker compose up -d` in this repo starts only the local SQL Server container. It does not start the API Gateway or any microservice containers.

### Option 1 — Database only

Use this when you only need the local SQL Server instance and schema migrations.

```powershell
.\scripts\setup-local-db.ps1
```

This starts `sqlserver`, creates `insighterp_db` if needed, and applies all local migrations.

### Option 2 — Host `dotnet run` workflow

Use this for the fastest per-service debugging loop from PowerShell or your IDE.

**Auth + Gateway only**
```powershell
docker compose up -d
dotnet run --project src/AuthService
dotnet run --project src/ApiGateway
```

**CustomerService (ecommerce) only**
```powershell
docker compose up -d
dotnet run --project src/CustomerService
# Swagger at http://localhost:5002/swagger
```

**Full host-run stack**
```powershell
docker compose up -d
.\scripts\run-all-services.ps1
```

This opens each service in its own PowerShell window.

### Option 3 — Full Docker-local stack

Use this when you want the API Gateway and all implemented microservices running in Docker with one command.

**Full stack**
```powershell
.\scripts\run-docker-services.ps1
```

This runs `setup-local-db.ps1`, starts `apigateway` first, then starts the remaining selected services with:

```powershell
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build ...
```

**Subset stack**
```powershell
.\scripts\run-docker-services.ps1 -Services apigateway,authservice
```

Supported service aliases include:

- `gateway`
- `auth`
- `customer`
- `order`
- `product`
- `forecast`
- `prediction`
- `analytics`
- `admin`

**Skip DB setup on repeated runs**
```powershell
.\scripts\run-docker-services.ps1 -SkipDbSetup
```

### Frontend

```powershell
# Run from the ERP_frontend folder
npm run dev
# Runs on http://localhost:5173
```

You can log in to the ERP staff portal with the default seeded admin account after the database setup completes:

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@123` | Admin |
| `manager` | `Admin@123` | Manager |
| `testuser` | `Admin@123` | User |

For ecommerce customer accounts, register via `POST /api/commerce/auth/register` on CustomerService.

---

## 🛑 Stopping the System

**Stop all host-run microservices:**
```powershell
.\scripts\stop-all-services.ps1
```

**Stop all Docker-local app containers:**
```powershell
.\scripts\stop-docker-services.ps1
```

**Stop Docker-local app containers and SQL Server:**
```powershell
.\scripts\stop-docker-services.ps1 -IncludeDb
```

**Stop the database with Compose directly:**
```powershell
docker compose down
```

> Use `docker compose down -v` to also **delete all database data** (destructive ⚠️).

---

## Service URLs

### Local Development — Host `dotnet run`

| Service | Local URL | Swagger |
|---|---|---|
| API Gateway | http://localhost:5000 | http://localhost:5000/swagger |
| AuthService | http://localhost:5001 | http://localhost:5001/swagger |
| CustomerService | http://localhost:5002 | http://localhost:5002/swagger |
| OrderService | http://localhost:5003 | http://localhost:5003/swagger |
| ProductService | http://localhost:5004 | http://localhost:5004/swagger |
| ForecastService | http://localhost:5005 | http://localhost:5005/swagger |
| PredictionService | http://localhost:5006 | http://localhost:5006/swagger |
| AnalyticsService | http://localhost:5007 | http://localhost:5007/swagger |
| AdminService | http://localhost:5011 | http://localhost:5011/swagger |
| Frontend | http://localhost:5173 | — |

---

### Local Development — Docker-local

`ApiGateway` is exposed on `http://localhost:5000`, and `CustomerService` is exposed directly on `http://localhost:5002` for ecommerce traffic.

| Service Surface | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| Gateway health | http://localhost:5000/health |
| AuthService Swagger | http://localhost:5000/auth/swagger |
| CustomerService Swagger | http://localhost:5002/swagger |
| CustomerService Health | http://localhost:5002/health |
| OrderService Swagger | http://localhost:5000/order/swagger |
| ProductService Swagger | http://localhost:5000/product/swagger |
| ForecastService Swagger | http://localhost:5000/forecast/swagger |
| PredictionService Swagger | http://localhost:5000/prediction/swagger |
| AnalyticsService Swagger | http://localhost:5000/analytics/swagger |
| AdminService Swagger | http://localhost:5000/admin/swagger |

---

## Gateway Health Checks

### Local
```
http://localhost:5000/auth/health
http://localhost:5000/order/health
http://localhost:5000/product/health
http://localhost:5000/admin/health
http://localhost:5000/forecast/health
http://localhost:5000/prediction/health
http://localhost:5000/analytics/health
http://localhost:5002/health
```

### Azure (dev)
```
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/auth/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/order/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/product/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/admin/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/forecast/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/prediction/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/analytics/health
https://customerservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/health
```

---

## ⚙️ CI/CD Pipelines

Both pipelines live in `.github/workflows/`.

### CI — `backend-ci.yml`
**Triggers:** Every push to any branch, every pull request

Runs automatically to catch broken code early:
1. Restores all .NET packages
2. Builds the full solution
3. Runs all automated tests (`dotnet test`)
4. Builds Docker images for all 9 services (validates Dockerfiles)

### CD — `cd-dev.yml`
**Triggers:** Push to the `dev` branch only

Deploys everything to Azure automatically:
1. Applies T-SQL database migrations against **Azure SQL** (`insighterp_db`) using `sqlcmd` — all schema folders in dependency order
2. Builds and pushes all 9 Docker images to Azure Container Registry
3. Logs into Azure using **OIDC** (passwordless — no stored credentials)
4. Configures ACR credentials on each Container App
5. Deploys updated images to all 9 Azure Container Apps
6. Sets shared DB connection-string secrets for AuthService, AdminService, PredictionService, ProductService, and CustomerService
7. Smoke tests core service `/health` endpoints through the API Gateway and CustomerService directly

---

## 📁 Documentation

| Document | Description |
|---|---|
| [`whatdone.md`](./whatdone.md) | Full project progress log — everything built so far |
| [`docs/database/database-guide.md`](./docs/database/database-guide.md) | Database architecture, local setup, Azure deployment, how to add schemas, migration rules |
| [`docs/micro_archi_structure_guide/micro_archi.md`](./docs/micro_archi_structure_guide/micro_archi.md) | Microservice architecture, ports, and Azure deployment details |
| [`docs/micro_archi_structure_guide/structure_guide.md`](./docs/micro_archi_structure_guide/structure_guide.md) | AuthService folder/file breakdown |
| [`src/AdminService/docs/README_ADMIN.md`](./src/AdminService/docs/README_ADMIN.md) | AdminService endpoints, responsibilities, and local verification steps |
| [`docs/DevOps/Sprint1/devops-sprint1.md`](./docs/DevOps/Sprint1/devops-sprint1.md) | DevOps Sprint 1 — full CI/CD implementation walkthrough |
| [`docs/DevOps/Sprint1/troubleshooting-sprint1.md`](./docs/DevOps/Sprint1/troubleshooting-sprint1.md) | All problems encountered in Sprint 1 and how they were fixed |
| [`docs/security/SECURITY_BEST_PRACTICES.md`](./docs/security/SECURITY_BEST_PRACTICES.md) | Security guidelines |
| [`docs/contribution_doc/`](./docs/contribution_doc/) | Contribution guidelines |
