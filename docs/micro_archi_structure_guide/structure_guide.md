# AuthService — Structure Guide

This document explains **every folder and file** in the `src/AuthService` microservice as it currently exists.

For each item you'll find:
- **What it is** (plainly explained)
- **What's inside it**
- **Why it matters**

---

## Current Folder Structure

```
src/AuthService/
├── Controllers/
│   ├── AuthController.cs       ← Login + Register endpoints
│   └── HealthController.cs     ← Health check + DB check endpoints
├── Models/
│   ├── AuthResponse.cs         ← What the API returns after login/register
│   ├── LoginRequest.cs         ← What the API expects for login
│   └── RegisterRequest.cs      ← What the API expects for register
├── Services/
│   └── JwtTokenService.cs      ← Generates JWT tokens
├── Properties/
│   └── launchSettings.json     ← Local development port config
├── Program.cs                  ← Service startup and configuration
├── Dockerfile                  ← Instructions to build the Docker container
├── appsettings.json            ← Base configuration
├── appsettings.Development.json← Local dev overrides
├── AuthService.csproj          ← Project definition (packages, target framework)
└── AuthService.http            ← Quick HTTP test file for developers
```

---

## Folder Breakdown

### `Controllers/`

**What it is:** The "front desk" of the service. Every HTTP request from the frontend or the API Gateway hits a Controller first.

**Files inside:**

#### `AuthController.cs`
Handles the main auth endpoints:
- `POST /api/auth/login` — accepts `{ username, password }`, returns a JWT token
- `POST /api/auth/register` — accepts `{ username, password, role }`, creates a user, returns a JWT

The controller now uses `IUserRepository` to read and write users in the `auth` schema of SQL Server / Azure SQL.

Registration validates role rules, checks username uniqueness in the database, hashes the password with SHA-256, stores the user in `auth.users`, and issues a JWT immediately.

Login looks up the user in the database, compares the stored password hash, checks `is_active`, and issues a JWT on success.

#### `HealthController.cs`
Two diagnostic endpoints:
- `GET /health` — returns `"AuthService is running..."`. Public, no token required. Used by the CI/CD smoke test.
- `GET /db-check` — connects to the configured SQL Server / Azure SQL database and verifies connectivity. Requires a valid JWT token (`[Authorize]`).

---

### `Models/`

**What it is:** Simple C# classes that define the **shape of data** going in and out of the API. Think of them as forms — the API expects data in a specific format.

**Files inside:**

#### `LoginRequest.cs`
Defines what a login request body must look like:
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

#### `RegisterRequest.cs`
Defines what a registration request body must look like:
```json
{
  "username": "john",
  "password": "Secret@123",
  "role": "Employee"
}
```
Role is optional — defaults to `Employee` if not provided. `Admin` cannot be self-assigned.

#### `AuthResponse.cs`
Defines what the API gives back after a successful login or register:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-03-03T10:00:00Z"
}
```
The frontend stores this token and attaches it to all future requests.

---

### `Services/`

**What it is:** The business logic layer — code that does actual work, not just receieving/sending HTTP.

**Files inside:**

#### `JwtTokenService.cs`
Responsible for **generating JWT (JSON Web Token)** tokens.

When a user logs in successfully, this service creates a signed token containing:
- The user's ID
- The user's username
- The user's role (Admin/Manager/Employee)
- An expiry time

The token is signed with a secret key (`JWT_SECRET`). Without the secret key, someone cannot forge a valid token.

The API Gateway uses the same secret key to **verify** that a token is genuine before allowing requests through.

---

### `Properties/`

**What it is:** Project settings for local development.

**Files inside:**

#### `launchSettings.json`
Tells `dotnet run` which port to use when you start the service locally.
```json
"applicationUrl": "http://localhost:5001"
```
> This file is only used during local development. In Azure, the `ASPNETCORE_URLS=http://+:8080` environment variable from the Dockerfile takes over.

---

## File Breakdown

### `Program.cs`
The **startup file** — the very first thing that runs when the service starts.

It wires everything together:
1. Reads the `JWT_SECRET` from env var (or falls back to `appsettings.json`)
2. Registers JWT Bearer authentication
3. Sets up CORS to allow the frontend to call the API
4. Enables Swagger UI
5. Configures the middleware pipeline (auth → routing → controllers)

If anything is misconfigured here, the entire service won't work.

---

### `Dockerfile`
**Instructions for building the Docker container.** Uses a two-stage build:

**Stage 1 — Build** (uses the heavy .NET SDK image):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN dotnet publish -c Release -o /app/publish
```
Compiles the application.

**Stage 2 — Runtime** (uses the lightweight ASP.NET runtime image):
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
```
Takes only the compiled output — final image is lean and production-ready.

The `ASPNETCORE_URLS=http://+:8080` line tells the app to listen on port 8080, which Azure Container Apps expects.

---

### `appsettings.json`
Default configuration that applies in all environments:
```json
{
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "InsightERP",
    "Audience": "InsightERP-Users"
  },
  "ConnectionStrings": {
    "AuthDb": ""    ← overridden by Azure secret at runtime
  }
}
```

---

### `appsettings.Development.json`
Local development overrides. Values here override `appsettings.json` when running with `dotnet run` (environment = `Development`).

Useful for: local database connection strings, more verbose logging, etc.

---

### `AuthService.csproj`
The **.NET project file** — defines what this project is and what it depends on.

Key contents:
- `<TargetFramework>net9.0</TargetFramework>` — uses .NET 9
- Package references: JWT bearer, `Microsoft.Data.SqlClient`, Swagger, etc.

Without this file, the project cannot be built or run.

---

### `AuthService.http`
A developer convenience file — contains sample HTTP requests you can run directly in VS Code (with REST Client extension) or Visual Studio.

Example:
```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{ "username": "admin", "password": "Admin@123" }
```

No effect on production. Just for quick manual testing during development.

---

## How an Auth Request Flows Through This Structure

```
Frontend sends: POST /api/auth/login { username, password }
        ↓
[ApiGateway] routes /api/auth/* to AuthService
        ↓
[AuthController.cs] receives the request
        ↓
Reads LoginRequest model (validates shape)
        ↓
Looks up user via IUserRepository in auth.users
        ↓
Compares SHA-256 hash of password
        ↓
[JwtTokenService.cs] generates a signed JWT token
        ↓
[AuthResponse.cs] is returned with the token
        ↓
Frontend stores the token, uses it for all future requests
```

---

## Sprint 2 Planned Improvements

| Current (Sprint 1) | Planned (Sprint 2) |
|---|---|
| Users stored in SQL Server / Azure SQL | Stronger auth management and broader schema rollout |
| SHA-256 password hashing | BCrypt or Argon2 hashing |
| No refresh tokens | Refresh token support |
| Database-seeded or manually registered users | Better user lifecycle management |
| `ConnectionStrings__AuthDb` drives the repository layer | Expand the same DB pattern to additional services |
