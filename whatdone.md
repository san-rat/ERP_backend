# Project Progress Summary — InsightERP Backend

---

### 1) Repository & Docs Structure
- Created a clean repository structure for documentation and future services.
- Added `/docs` folder and stored the SRS PDF.
- Added a root `README.md` to describe the backend.
- Added `.github/pull_request_template.md` for standardised PRs.

---

### 2) First Microservice: AuthService
- Created the first backend microservice: `src/AuthService`
- Implemented endpoints:
  - `GET /health` — service health check
  - `GET /db-check` — database connectivity check (requires auth)
  - `POST /api/auth/login` — login with username/password, returns JWT
  - `POST /api/auth/register` — register new user, returns JWT
- Enabled Swagger UI for API testing.
- JWT token generation with SHA-256 password hashing.
- Pre-seeded accounts: `admin/Admin@123`, `manager/Manager@123`, `employee/Employee@123`
- Packages: `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `MySql.Data`
- Local port: `http://localhost:5001`

---

### 3) CI Pipeline (GitHub Actions)
A GitHub Actions workflow (`.github/workflows/backend-ci.yml`) runs on every push/PR:
- Verifies required repo structure
- Restores .NET dependencies
- Builds the full solution (`ERP_Backend.slnx`)
- Runs all automated tests (`dotnet test`)
- Builds Docker images for **all 8 services** (validates containerisation)

---

### 4) CustomerService Microservice
- Created `src/CustomerService` with the standard microservice baseline:
  - `GET /health` — health check endpoint
  - Swagger UI enabled
  - Dockerised with multi-stage build (port `8080`)
- Folder structure: `Controller/`, `Models/`, `Services/`, `Common/`
- Local port: `http://localhost:5002`

---

### 5) OrderService Microservice
- Created `src/OrderService`:
  - `GET /health` — health check endpoint
  - Swagger UI enabled
  - Dockerised (port `8080`)
- Folder structure: `Controller/`, `Models/`, `Services/`, `Common/`
- Local port: `http://localhost:5003`

---

### 6) ProductService Microservice
- Created `src/ProductService`:
  - `GET /health` — health check endpoint
  - Swagger UI enabled
  - Dockerised (port `8080`)
- Folder structure: `Controller/`, `Models/`, `Services/`, `Common/`
- Local port: `http://localhost:5004`

---

### 7) AnalyticsService, ForecastService & PredictionService Microservices
- Created all three services with the standard baseline:
  - `GET /health` per service
  - Swagger UI
  - Dockerised (port `8080`)
- Local ports:
  - AnalyticsService: `http://localhost:5007`
  - ForecastService: `http://localhost:5005`
  - PredictionService: `http://localhost:5006`

---

### 8) API Gateway
- Fully implemented `src/ApiGateway` using **Ocelot** reverse proxy.
- Routes all incoming frontend requests to the correct downstream service.
- JWT validation built in — authenticated routes require a valid Bearer token.
- Swagger UI available at `/swagger`.
- Local port: `http://localhost:5000`

---

### 9) Docker Compose (Local Dev Database)
- `docker-compose.yml` at repo root spins up a local MySQL 8.0 instance:
  - Container name: `erp-mysql-local`
  - External port: `3307` → internal `3306`
  - Database: `auth_db`, User: `auth_user`

---

### 10) Documentation
- `docs/SRS/` — System Requirements Specification PDF
- `docs/micro_archi_structure_guide/micro_archi.md` — Microservice architecture guide
- `docs/micro_archi_structure_guide/structure_guide.md` — AuthService folder/file breakdown
- `docs/contribution_doc/` — Contribution guidelines
- `docs/security/` — Security documentation

---

### 11) DevOps Sprint 1 — Full CI/CD Implementation
- **CI pipeline** (`.github/workflows/backend-ci.yml`) expanded to cover all 8 services:
  - Runs `dotnet test` to validate all tests pass
  - Builds Docker images for all 8 services as a validation step
- **CD pipeline** (`.github/workflows/cd-dev.yml`) fully built from scratch:
  - Applies database migrations automatically on every deploy
  - Builds and pushes Docker images for all 8 services to ACR
  - Configures ACR credentials on each Container App
  - Deploys all 8 services to Azure Container Apps
  - Configures AuthService database connection string (stored as encrypted secret)
  - Smoke tests AuthService health after deployment
  - Sets `minReplicas=1` on ApiGateway and AuthService to prevent cold starts

---

### 12) Azure Container Registry (ACR)
- Set up a private Docker image registry on Azure.
- All 8 service images are tagged with both the Git commit SHA and `:latest`.
- CI/CD pipeline authenticates and pushes images automatically.
- Credentials stored securely as GitHub Actions secrets.

---

### 13) Azure Container Apps — Cloud Deployment
- Created a Container Apps Environment (`erp-dev-env`) in Southeast Asia.
- Deployed all 8 microservices as individual Azure Container Apps:

| Container App | Ingress | Azure URL |
|---|---|---|
| `apigateway-dev` | External (public) | `apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io` |
| `authservice-dev` | External (public) | `authservice-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io` |
| `customerservice-dev` | Internal only | — |
| `orderservice-dev` | Internal only | — |
| `productservice-dev` | Internal only | — |
| `forecastservice-dev` | Internal only | — |
| `predictionservice-dev` | Internal only | — |
| `analyticsservice-dev` | Internal only | — |

---

### 14) Azure OIDC — Passwordless Authentication
- Replaced legacy service principal credentials with **OIDC (OpenID Connect)**.
- Created a User-Assigned Managed Identity (`erp-github-mi`) with `Contributor` role on `erp-rg`.
- Configured a Federated Credential to trust GitHub Actions from `san-rat/ERP_backend` on `dev`.
- GitHub Secrets added: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
- No passwords stored for Azure authentication — cryptographic proof only.

---

### 15) API Gateway — Production Ocelot Configuration
- Created `src/ApiGateway/ocelot.Production.json` with Azure internal hostnames.
- `program.cs` updated to auto-load `ocelot.{Environment}.json`:
  - Local (`Development`): uses `ocelot.json` → routes to `localhost:PORT`
  - Azure (`Production`): uses `ocelot.Production.json` → routes to `service-dev.internal.*.azurecontainerapps.io:443`
- Fixed port mismatch: removed hardcoded `UseUrls("http://*:5000")` that was overriding the Docker `ASPNETCORE_URLS=http://+:8080`.

---

### 16) AuthService Database Connection (Azure MySQL)
- Configured `ConnectionStrings__AuthDb` on `authservice-dev` Container App.
- Used Azure's secret reference pattern:
  1. Connection string stored as an encrypted Container App secret (`auth-db-conn`)
  2. Env var references the secret by name — password never exposed in logs
- Azure MySQL server: `erp-mysql-dev.mysql.database.azure.com`

---

### 17) Frontend Connectivity (Vercel)
- Frontend repo connected to Vercel for automatic deployment.
- Environment variable `VITE_API_BASE_URL` set to ApiGateway public URL.
- Frontend uses `import.meta.env.VITE_API_BASE_URL` for all API calls — no hardcoded URLs.
- Auth flow: Frontend → ApiGateway → AuthService → JWT returned → stored in sessionStorage.

---

### 18) DevOps Documentation
- `docs/DevOps/Sprint1/devops-sprint1.md` — Full sprint walkthrough (layman-friendly)
- `docs/DevOps/Sprint1/troubleshooting-sprint1.md` — All problems encountered and fixed

---

## 📁 Current Project Structure

```text
ERP_backend/
├── .github/
│   ├── workflows/
│   │   ├── backend-ci.yml        # CI — build, test, Docker validate (all 8 services)
│   │   └── cd-dev.yml            # CD — migrate, build, push, deploy to Azure
│   └── pull_request_template.md
├── docs/
│   ├── SRS/SRS.pdf
│   ├── micro_archi_structure_guide/
│   │   ├── micro_archi.md
│   │   └── structure_guide.md
│   ├── DevOps/
│   │   └── Sprint1/
│   │       ├── devops-sprint1.md
│   │       └── troubleshooting-sprint1.md
│   ├── contribution_doc/
│   └── security/
├── src/
│   ├── ApiGateway/               # Ocelot API Gateway          :5000
│   │   ├── ocelot.json           #   → local routing config
│   │   └── ocelot.Production.json#   → Azure routing config
│   ├── AuthService/              # Auth + JWT                  :5001
│   ├── CustomerService/          # Customer management         :5002
│   ├── OrderService/             # Order lifecycle             :5003
│   ├── ProductService/           # Product catalog             :5004
│   ├── ForecastService/          # Demand forecasting          :5005
│   ├── PredictionService/        # ML predictions              :5006
│   └── AnalyticsService/         # Business analytics          :5007
├── db/
│   └── auth/
│       └── migrations/           # SQL migration scripts
├── scripts/
│   └── apply_migrations.sh       # Migration runner script
├── tests/
├── docker-compose.yml            # Local MySQL dev DB
├── ERP_Backend.slnx              # Solution file
└── README.md
```

---

## 🔌 Service Port Reference

| Service | Local HTTP | Container | Azure Container App |
|---|---|---|---|
| ApiGateway | `5000` | `8080` | `apigateway-dev` (external) |
| AuthService | `5001` | `8080` | `authservice-dev` (external) |
| CustomerService | `5002` | `8080` | `customerservice-dev` (internal) |
| OrderService | `5003` | `8080` | `orderservice-dev` (internal) |
| ProductService | `5004` | `8080` | `productservice-dev` (internal) |
| ForecastService | `5005` | `8080` | `forecastservice-dev` (internal) |
| PredictionService | `5006` | `8080` | `predictionservice-dev` (internal) |
| AnalyticsService | `5007` | `8080` | `analyticsservice-dev` (internal) |

---

## ▶️ How To Run A Service Locally

### Run using .NET
```bash
cd src/<ServiceName>
dotnet run
```
Then open `http://localhost:<PORT>/swagger`

### Run using Docker
```bash
cd src/<ServiceName>
docker build -t <service-name> .
docker run -p 5000:8080 <service-name>
```
Then open `http://localhost:5000/swagger`

### Run local database (MySQL)
```bash
docker-compose up -d
```

---

## 🚧 Sprint 2 Backlog

- [ ] `JWT_SECRET` as GitHub secret — both ApiGateway and AuthService must read from env var (currently falls back to hardcoded default)
- [ ] CD blocked by CI — CD should only run if CI tests pass first
- [ ] Smoke test through ApiGateway — test `/auth/health` via gateway URL to verify routing
- [ ] Production pipeline (`cd-prod.yml`) for the `main` branch
- [ ] Database-backed AuthService — users currently in-memory (lost on restart), need MySQL persistence
- [ ] DB connections for CustomerService, OrderService, ProductService (when DB schemas are defined)
- [ ] Docker build layer caching — reduce build time by ~60%
- [ ] Deployment notifications (Slack/Teams on success/failure)
- [ ] CORS — update ApiGateway to allow specific Vercel domain instead of `AllowAnyOrigin`

---

### 19) SQL Server Local Database Migration & Seed Refactor (March 19, 2026)
- Added a SQL Server-first local database setup flow through `scripts/setup-local-db.ps1`.
- Local setup now discovers and applies all `schemas/*/migrations` folders in dependency order.
- `schemas/prediction/migrations` is tracked under `ml.schema_migrations`.
- Empty analytics migration placeholders are skipped safely and are not recorded as applied.
- Extracted demo data from `scripts/setup-working.ps1` into real migration files:
  - `schemas/auth/migrations/005_seed_demo_users.sql`
  - `schemas/customer/migrations/002_seed_customers.sql`
  - `schemas/product/migrations/002_seed_products.sql`
  - `schemas/order/migrations/002_seed_order_domain_data.sql`
- Fresh local resets now restore:
  - auth users: `admin`, `testuser`, `manager`
  - `dbo.customers`: 10 rows
  - `dbo.products`: 11 rows
  - `dbo.orders`: 17 rows
  - `dbo.order_items`: 27 rows
  - `dbo.returns`: 4 rows
- Local auth test credentials:
  - `admin / Admin@123 / admin@insighterp.local / ADMIN`
  - `testuser / Admin@123 / testuser@insighterp.local / USER`
  - `manager / Admin@123 / manager@insighterp.local / MANAGER`
- This update supersedes the earlier local MySQL-based dev database note in this file.

### 20) Dev Deployment Multi-Schema Azure SQL Migration Update (March 20, 2026)
- Updated `.github/workflows/cd-dev.yml` so the `dev` deployment no longer migrates only `auth`.
- The deployment workflow now applies all current schema migration folders before building and deploying services:
  - `schemas/auth/migrations`
  - `schemas/customer/migrations`
  - `schemas/product/migrations`
  - `schemas/order/migrations`
  - `schemas/prediction/migrations` mapped to SQL schema `ml`
  - `schemas/analytics/migrations`
- This keeps the deployed Azure SQL database aligned with the same multi-schema migration order used in local setup.
- The current student-project decision is to allow demo seed migrations in the `dev` deployment environment as well.
- Updated `docs/database/database-guide.md` to reflect the new `dev` deployment migration flow.
