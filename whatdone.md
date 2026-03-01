# Project Progress Summary

---

### 1) Repository & Docs Structure
- Created a clean repository structure for documentation and future services.
- Added `/docs` folder and stored the SRS PDF.
- Added a root `README.md` to describe the backend.
- Added `.github/pull_request_template.md` for standardised PRs.

---

### 2) First Microservice: AuthService
- Created the first backend microservice: `src/AuthService`
- Implemented minimal endpoints:
  - `GET /health` — service health check
  - `POST /login` — basic login endpoint (placeholder JWT, to be replaced)
- Enabled Swagger UI for API testing.
- Packages: `Swashbuckle.AspNetCore 6.9.0`, `Microsoft.AspNetCore.Authentication.JwtBearer 9.0.2`, `MySql.Data 9.6.0`
- Local port: `http://localhost:5288`

---

### 3) CI Pipeline (GitHub Actions)
A GitHub Actions workflow (`backend-ci.yml`) is set up to run on every push/pull request to `main` and `dev`:
- Verifies required repo structure (README + docs folder)
- Restores .NET dependencies
- Builds the backend solution (`ERP_Backend.slnx`)
- Builds the Docker image for AuthService (validates containerization)

A CD pipeline (`cd-dev.yml`) is also present for deployment automation.

---

### 4) CustomerService Microservice
- Created `src/CustomerService` with the standard microservice baseline framework:
  - `GET /health` — health check endpoint
  - Swagger UI enabled
  - Dockerised with multi-stage build (port `8080`)
- Fixed a `Microsoft.OpenApi` version conflict — downgraded `Swashbuckle.AspNetCore` from `10.1.4` → `6.9.0` and removed `Microsoft.AspNetCore.OpenApi 9.0.13`.
- Folder structure: `Controller/`, `Models/`, `Services/`, `Common/`
- Local port: `http://localhost:5071`

---

### 5) OrderService Microservice
- Created `src/OrderService` with the standard microservice baseline framework:
  - `GET /health` — health check endpoint
  - Swagger UI enabled
  - Dockerised with multi-stage build (port `8080`)
- Fixed the same `Microsoft.OpenApi` package conflict as CustomerService.
- Fixed Dockerfile copy-paste errors (was referencing `CustomerService` files).
- Fixed `Program.cs` — replaced default WeatherForecast scaffolding with clean controller setup.
- Folder structure: `Controller/`, `Models/`, `Services/`, `Common/`
- Local port: `http://localhost:5113`

---

### 6) ProductService Microservice
- Created `src/ProductService` with the standard microservice baseline framework:
  - `GET /health` — health check endpoint
  - Swagger UI enabled
  - Dockerised with multi-stage build (port `8080`)
- Fixed the same `Microsoft.OpenApi` package conflict.
- Fixed Dockerfile copy-paste errors (was referencing `OrderService` files).
- Fixed `Program.cs` — replaced default WeatherForecast scaffolding with clean controller setup.
- Folder structure: `Controller/`, `Models/`, `Services/`, `Common/`
- Local port: `http://localhost:5038`

---

### 7) AnalyticsService, ForecastService & PredictionService Microservices
- Created all three services with the standard microservice baseline framework:
  - `GET /health` — health check endpoint per service
  - Swagger UI enabled on each
  - Dockerised with multi-stage build (port `8080`)
- Fixed `Microsoft.OpenApi` package conflict on all three.
- Fixed Dockerfile copy-paste errors on all three (were all referencing `ProductService` files).
- Fixed `Program.cs` on all three — replaced WeatherForecast scaffolding with clean controller setup.
- Folder structure (each): `Controller/`, `Models/`, `Services/`, `Common/`
- Local ports:
  - AnalyticsService: `http://localhost:5199`
  - ForecastService: `http://localhost:5044`
  - PredictionService: `http://localhost:5197`

---

### 8) API Gateway
- `src/ApiGateway` exists as a service skeleton.
- Local port: `http://localhost:5279` / `https://localhost:7126`

---

### 9) Docker Compose (Local Dev Database)
- `docker-compose.yml` at repo root spins up a local MySQL 8.0 instance:
  - Container name: `erp-mysql-local`
  - External port: `3307` → internal `3306`
  - Database: `auth_db`, User: `auth_user`

---

### 10) Documentation
- `docs/SRS/` — System Requirements Specification PDF
- `docs/micro_archi_structure_guide/micro_archi.md` — Microservice architecture guide with all port numbers, responsibilities, and architecture diagram
- `docs/contribution_doc/` — Contribution guidelines
- `docs/security/` — Security documentation

---

## 📁 Current Project Structure

```text
ERP_backend/
├── .github/
│   ├── workflows/
│   │   ├── backend-ci.yml        # CI pipeline (build + Docker validate)
│   │   └── cd-dev.yml            # CD pipeline
│   └── pull_request_template.md
├── docs/
│   ├── SRS/SRS.pdf
│   ├── micro_archi_structure_guide/micro_archi.md
│   ├── contribution_doc/
│   └── security/
├── src/
│   ├── ApiGateway/               # API Gateway skeleton   :5279
│   ├── AuthService/              # Auth + JWT             :5288
│   ├── CustomerService/          # Customer management    :5071
│   ├── OrderService/             # Order lifecycle        :5113
│   ├── ProductService/           # Product catalog        :5038
│   ├── AnalyticsService/         # Business analytics     :5199
│   ├── ForecastService/          # Demand forecasting     :5044
│   └── PredictionService/        # ML predictions         :5197
├── db/
├── scripts/
├── tests/
├── docker-compose.yml            # Local MySQL dev DB
├── ERP_Backend.slnx              # Solution file
└── README.md
```

---

## � Service Port Reference

| Service | Local HTTP | Local HTTPS | Container |
|---|---|---|---|
| ApiGateway | `5279` | `7126` | `8080` |
| AuthService | `5288` | `7009` | `8080` |
| CustomerService | `5071` | — | `8080` |
| OrderService | `5113` | — | `8080` |
| ProductService | `5038` | — | `8080` |
| AnalyticsService | `5199` | — | `8080` |
| ForecastService | `5044` | — | `8080` |
| PredictionService | `5197` | — | `8080` |

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

## 🚧 Next Planned Steps
- Add Azure Container Registry (ACR)
- Push Docker images to ACR via CD pipeline
- Deploy services to Azure Container Apps
- Implement JWT validation in AuthService
- Connect services through the API Gateway
- Add database connections per service
