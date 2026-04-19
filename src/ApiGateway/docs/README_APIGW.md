# InsightERP — API Gateway

> Single entry point for all InsightERP microservices. Built with **Ocelot** on **.NET 9**, handling request routing, JWT authentication, CORS, and health monitoring.

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Configuration Files Explained](#configuration-files-explained)
- [Running Locally](#running-locally)
- [Running with Docker](#running-with-docker)
- [Testing the Gateway](#testing-the-gateway)
- [Registered Services & Routes](#registered-services--routes)
- [Adding a New Route](#adding-a-new-route)
- [Troubleshooting](#troubleshooting)

---

## Overview

```
Client Request
      │
      ▼
┌─────────────────────────────┐
│     API Gateway :5000       │
│  - JWT Auth check           │
│  - Route matching           │
│  - Forward to downstream    │
└─────────────────────────────┘
      │
      ├──► authentication-service  :5001  (public)
      ├──► core-erp-service        :5002  (JWT required)
      ├──► ml-service              :5005  (JWT required)
      ├──► report-service          :5006  (JWT required)
      ├──► chatbot-service         :5007  (JWT required)
      ├──► notification-service    :5008  (JWT required)
      └──► dashboard-service       :5009  (JWT required)
```

---

## Prerequisites

Make sure you have the following installed before running the gateway:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 9.0+ | https://dotnet.microsoft.com/download/dotnet/9 |
| Git | Any | https://git-scm.com |
| Docker Desktop | Any | https://www.docker.com/products/docker-desktop (optional) |

Verify your .NET version:
```bash
dotnet --version
# Should output 9.x.x
```

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/san-rat/ERP_backend.git
cd ERP_backend
```

### 2. Switch to the develop branch

```bash
git checkout develop
git pull origin develop
```

### 3. Navigate to the gateway

```bash
cd src/ApiGateway
```

---

## Project Structure

```
src/ApiGateway/
│
├── Controllers/
│   └── GatewayController.cs      # Diagnostic endpoints (/gateway/health, /info, /routes etc.)
│
├── Properties/
│   └── launchSettings.json       # Local VS / VS Code run config
│
├── api-gateway.csproj            # NuGet packages and build config
├── api-gateway.sln               # Visual Studio solution file
│
├── appsettings.json              # Base config — JWT settings, logging
├── appsettings.Development.json  # Dev overrides — verbose (Debug) logging
├── appsettings.Production.json   # Prod overrides — quiet (Warning) logging
│
├── ocelot.json                   # Route definitions — maps URLs to downstream services
├── program.cs                    # App entry point — wires up all middleware
├── dockerfile                    # Multi-stage Docker build
└── README.md                     # This file
```

---

## Configuration Files Explained

| File | Purpose |
|---|---|
| `appsettings.json` | Base config loaded in all environments. Contains JWT secret and default logging. |
| `appsettings.Development.json` | Overrides for local dev. Sets verbose Debug logging so you can see every request. |
| `appsettings.Production.json` | Overrides for production. Sets Warning-only logging to reduce noise. |
| `ocelot.json` | The route table. Tells Ocelot where to forward each incoming request. |
| `program.cs` | Startup pipeline — registers Ocelot, JWT, Swagger, CORS, and health checks. |
| `GatewayController.cs` | Your own `/gateway/*` endpoints for health checks and diagnostics. |
| `dockerfile` | Packages the app into a container using a 2-stage build. |

---

## Running Locally

> The gateway runs standalone. Your microservices do **not** need to be running for the gateway to start.

### Step 1 — Restore packages

```bash
dotnet restore
```

### Step 2 — Run

```bash
dotnet run
```

You should see output like:
```
[10:00:00 INF] ========== InsightERP API Gateway Starting ==========
[10:00:00 INF] Ocelot configured with static service registry
[10:00:01 INF] Gateway running  → http://localhost:5000
[10:00:01 INF] Swagger UI       → http://localhost:5000/swagger
```

### Step 3 — Open Swagger

Navigate to **http://localhost:5000/swagger** in your browser to see all gateway endpoints.

---

## Running with Docker

### Build the image

```bash
# From inside src/ApiGateway/
docker build -t api-gateway .
```

### Run the container

```bash
docker run -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e JwtSettings__SecretKey=your-secret-key-here \
  api-gateway
```

> **Important for Docker:** When running in a container, downstream services are reached by their **container name** not `localhost`. The `ocelot.json` in this repo already uses container names (e.g. `authentication-service`, `core-erp-service`) so it is Docker-ready out of the box.

---

## Testing the Gateway

### Quick health check

```bash
curl http://localhost:5000/gateway/health
```

Expected response:
```json
{
  "service": "ApiGateway",
  "status": "healthy",
  "environment": "Development",
  "timestamp": "2025-01-01T10:00:00Z",
  "uptime": "0h 1m 30s"
}
```

---

### Test 1 — Gateway diagnostic endpoints (no token needed)

```bash
# Health
curl http://localhost:5000/gateway/health

# Info — gateway version, features
curl http://localhost:5000/gateway/info

# All registered downstream services
curl http://localhost:5000/gateway/services

# Full route table
curl http://localhost:5000/gateway/routes

# Kubernetes liveness probe
curl http://localhost:5000/gateway/live

# Kubernetes readiness probe
curl http://localhost:5000/gateway/ready
```

All of these should return **200 OK**.

---

### Test 2 — JWT authentication is blocking protected routes

```bash
curl http://localhost:5000/api/orders
```

Expected: **401 Unauthorized** — confirms the gateway is correctly requiring a JWT token before forwarding the request.

---

### Test 3 — Routing is working (even without downstream services running)

```bash
curl -X POST http://localhost:5000/api/auth/login
```

Expected: **502 Bad Gateway** — this is correct behaviour. It means Ocelot found the `/api/auth/*` route and tried to forward the request to `authentication-service:5001`, but that service is not running yet. The routing is working correctly.

---

### Test 4 — Unknown routes return 404

```bash
curl http://localhost:5000/api/nonexistent
```

Expected: **404 Not Found** — confirms only your mapped routes are exposed.

---

### Test summary

| Test | Endpoint | Expected | What it confirms |
|---|---|---|---|
| Health check | `GET /gateway/health` | 200 ✅ | Gateway is up |
| Protected route, no token | `GET /api/orders` | 401 ✅ | JWT auth working |
| Public route, service down | `POST /api/auth/login` | 502 ✅ | Routing is working |
| Unknown route | `GET /api/nonexistent` | 404 ✅ | Only mapped routes exposed |

---

## Registered Services & Routes

| Route | Downstream Service | Port | Auth Required |
|---|---|---|---|
| `/api/auth/*` | authentication-service | 5001 | ❌ Public |
| `/api/products/*` | product-service | 5004 | ✅ JWT |
| `/api/orders/*` | order-service | 5003 | ✅ JWT |
| `/api/ml/churn/*` | ml-service | 5005 | ✅ JWT |
| `/api/ml/segmentation/*` | ml-service | 5005 | ✅ JWT |
| `/api/ml/forecast/*` | ml-service | 5005 | ✅ JWT |
| `/api/reports/*` | report-service | 5006 | ✅ JWT |
| `/api/chatbot/*` | chatbot-service | 5007 | ✅ JWT |
| `/api/notifications/*` | notification-service | 5008 | ✅ JWT |
| `/api/dashboard/*` | dashboard-service | 5009 | ✅ JWT |

---

## Adding a New Route

Open `ocelot.json` and add a new entry inside the `Routes` array:

```json
{
  "DownstreamPathTemplate": "/api/your-service/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    { "Host": "your-service-container-name", "Port": 5010 }
  ],
  "UpstreamPathTemplate": "/api/your-service/{everything}",
  "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE", "OPTIONS" ],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }
}
```

Remove the `AuthenticationOptions` block entirely if the route should be **public** (no JWT required), like `/api/auth/*`.

Restart the gateway after saving — `ocelot.json` changes are picked up on restart.

---

## Troubleshooting

**Gateway fails to start — JSON error**
```
System.Text.Json.JsonException: '//' is invalid...
```
Your `ocelot.json` contains `//` comments. JSON does not support comments — remove all comment lines and restart.

---

**All microservice routes return 502**

The downstream service on that port is not running. Start the relevant service first. A 502 means routing is working — the gateway just has no service to forward to yet.

---

**Getting 401 on all routes**

You need a JWT token in your request header:
```bash
curl http://localhost:5000/api/orders \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```
Get a token by hitting `/api/auth/login` on the authentication service directly (port 5001).

---

**Port 5000 already in use**

```powershell
# Windows — find what is using port 5000
netstat -ano | findstr :5000

# Kill it (replace <PID> with the number shown above)
taskkill /PID <PID> /F
```

```bash
# Mac/Linux
lsof -i :5000
kill -9 <PID>
```

---

**`dotnet restore` fails — package not found**

Clear the NuGet cache and try again:
```bash
dotnet nuget locals all --clear
dotnet restore
```

---

## Related

- [Contribution Guidelines](../../docs/contribution_doc/)
- [Microservice Architecture Guide](../../docs/micro_archi_structure_guide/)
- [AuthService](../AuthService/)
