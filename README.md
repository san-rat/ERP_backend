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
        ▼
API Gateway (:5000)   ← single entry point for all requests
        │
        ├── AuthService        (:5001)
        ├── CustomerService    (:5002)
        ├── OrderService       (:5003)
        ├── ProductService     (:5004)
        ├── ForecastService    (:5005)
        ├── PredictionService  (:5006)
        └── AnalyticsService   (:5007)
                │
                ▼
         Azure SQL DB      ← insighterp_db on insighterp-sqlserver.database.windows.net
                           (single database, isolated per service by SQL schema)
```

---

## 🌐 Live Environment (Azure — dev)

| Resource | URL |
|---|---|
| **API Gateway** | https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io |
| **AuthService** | https://authservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io |
| **Gateway Swagger** | https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/swagger |
| **Auth Swagger** | https://authservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/swagger |
| **Gateway Health** | https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/auth/health |

> All other services (Customer, Order, Product, Forecast, Prediction, Analytics) are **internal only** — accessible only through the API Gateway, not directly from the internet.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js](https://nodejs.org/) (for the frontend)

---

## 🚀 Starting the System Locally

### Option 1 — Login / Auth only (minimal setup)

Use this when you only need the login page and auth to work.

**Step 1 — Start the database**
```powershell
# Run from the repo root
docker compose up -d
```

**Step 2 — Start the AuthService**
```powershell
dotnet run --project src/AuthService
# Runs on http://localhost:5001
```

**Step 3 — Start the API Gateway**
```powershell
dotnet run --project src/ApiGateway
# Runs on http://localhost:5000
```

**Step 4 — Start the frontend**
```powershell
# Run from the ERP_frontend folder
npm run dev
# Runs on http://localhost:5173
```

You can now log in with the default seeded admin account:

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@123` | Admin |

> Additional users can be created via `POST /api/auth/register`. Credentials are stored in Azure SQL (`auth.users`).

---

### Option 2 — Full system (all microservices)

Use this when you need all services running at once.

**Step 1 — Start the database**
```powershell
docker compose up -d
```

**Step 2 — Start all microservices + gateway**
```powershell
# Run from the repo root
.\scripts\run-all-services.ps1
```
This opens each service in its own PowerShell window.

**Step 3 — Start the frontend**
```powershell
# Run from the ERP_frontend folder
npm run dev
```

---

## 🛑 Stopping the System

**Stop all microservices:**
```powershell
.\scripts\stop-all-services.ps1
```

**Stop the database:**
```powershell
docker compose down
```

> Use `docker compose down -v` to also **delete all database data** (destructive ⚠️).

---

## Service URLs

### Local Development

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
| Frontend | http://localhost:5173 | — |

---

## Gateway Health Checks

### Local
```
http://localhost:5000/auth/health
http://localhost:5000/customer/health
http://localhost:5000/order/health
http://localhost:5000/product/health
http://localhost:5000/forecast/health
http://localhost:5000/prediction/health
http://localhost:5000/analytics/health
```

### Azure (dev)
```
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/auth/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/customer/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/order/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/product/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/forecast/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/prediction/health
https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io/analytics/health
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
4. Builds Docker images for all 8 services (validates Dockerfiles)

### CD — `cd-dev.yml`
**Triggers:** Push to the `dev` branch only

Deploys everything to Azure automatically:
1. Applies T-SQL database migrations against **Azure SQL** (`insighterp_db`) using `sqlcmd`
2. Builds and pushes all 8 Docker images to Azure Container Registry
3. Logs into Azure using **OIDC** (passwordless — no stored credentials)
4. Configures ACR credentials on each Container App
5. Deploys updated images to all 8 Azure Container Apps
6. Sets the AuthService DB connection string (`AUTH_DB_CONNECTION_STRING_AZURE`) as an encrypted secret
7. Smoke tests the AuthService `/health` endpoint to confirm deployment

---

## 📁 Documentation

| Document | Description |
|---|---|
| [`whatdone.md`](./whatdone.md) | Full project progress log — everything built so far |
| [`docs/database/database-guide.md`](./docs/database/database-guide.md) | 🗄️ **Database guide** — architecture, local setup, Azure deployment, how to add schemas, migration rules |
| [`docs/micro_archi_structure_guide/micro_archi.md`](./docs/micro_archi_structure_guide/micro_archi.md) | Microservice architecture, ports, and Azure deployment details |
| [`docs/micro_archi_structure_guide/structure_guide.md`](./docs/micro_archi_structure_guide/structure_guide.md) | AuthService folder/file breakdown |
| [`docs/DevOps/Sprint1/devops-sprint1.md`](./docs/DevOps/Sprint1/devops-sprint1.md) | DevOps Sprint 1 — full CI/CD implementation walkthrough |
| [`docs/DevOps/Sprint1/troubleshooting-sprint1.md`](./docs/DevOps/Sprint1/troubleshooting-sprint1.md) | All problems encountered in Sprint 1 and how they were fixed |
| [`docs/security/SECURITY_BEST_PRACTICES.md`](./docs/security/SECURITY_BEST_PRACTICES.md) | Security guidelines |
| [`docs/contribution_doc/`](./docs/contribution_doc/) | Contribution guidelines |
