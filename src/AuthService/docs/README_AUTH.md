# InsightERP — Authentication Service

> The **sole issuer of JWTs** in the InsightERP platform. All other services only validate — they never generate — tokens. Built on **.NET 9**.

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Running Locally](#running-locally)
- [Running with Docker](#running-with-docker)
- [API Reference](#api-reference)
- [Role Rules](#role-rules)
- [Seeded Test Users](#seeded-test-users)
- [Testing the Service](#testing-the-service)
- [Troubleshooting](#troubleshooting)

---

## Overview

```
Client
  │
  ▼
POST /api/auth/register  ──► creates user + returns JWT
POST /api/auth/login     ──► validates credentials + returns JWT

All other services validate the JWT — they never generate one.
```

**Token claims included in every JWT:**

| Claim | Value |
|-------|-------|
| `sub` | User ID (GUID) |
| `name` | Username |
| `role` | Admin / Manager / Employee |
| `jti` | Unique token ID |
| `iss` | `InsightERP` |
| `aud` | `InsightERP-Users` |

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Git | Any |
| Docker Desktop | Any (optional) |

```bash
dotnet --version
# Should output 9.x.x
```

---

## Getting Started

```bash
git clone https://github.com/san-rat/ERP_backend.git
cd ERP_backend
cd src/AuthService
dotnet restore
dotnet run
```

Swagger UI → **http://localhost:5001/swagger**

---

## Project Structure

```
src/AuthService/
│
├── Controllers/
│   └── AuthController.cs        # POST /api/auth/register and /login
│
├── Models/
│   ├── RegisterRequest.cs       # { Username, Password, Role? }
│   ├── LoginRequest.cs          # { Username, Password }
│   └── AuthResponse.cs          # { Token, Username, Role, ExpiresAt }
│
├── Services/
│   └── JwtTokenService.cs       # Centralized JWT generation logic
│
├── docs/
│   └── README_AUTH.md           # This file
│
├── AuthService.csproj
├── Program.cs                   # App entry point — JWT, Swagger, DI
└── appsettings.json             # JWT settings (secret, issuer, audience, expiry)
```

---

## Configuration

All JWT settings live under `JwtSettings` in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey":      "your-super-secret-key-at-least-32-chars",
    "Issuer":         "InsightERP",
    "Audience":       "InsightERP-Users",
    "ExpiryMinutes":  60
  }
}
```

**Override with an environment variable (recommended for production):**

```bash
# The env var takes priority over appsettings.json
export JWT_SECRET=your-production-secret-key
```

> Never commit the real production key. Use Azure Key Vault, AWS Secrets Manager, or a CI/CD secret store.

---

## Running Locally

```bash
# From the repo root
dotnet run --project src/AuthService
```

Expected startup output:
```
info: AuthService is starting...
info: Now listening on: http://localhost:5001
```

---

## Running with Docker

```bash
# Build
docker build -t auth-service .

# Run
docker run -p 5001:5001 \
  -e JWT_SECRET=your-secret-key-here \
  auth-service
```

---

## API Reference

### `POST /api/auth/register`

Create a new user account. Returns a JWT immediately (auto-login on register).

**Request body:**
```json
{
  "username": "jsmith",
  "password": "Secure@123",
  "role":     "Employee"
}
```

> `role` is **optional**. If omitted, defaults to `Employee`.

**Responses:**

| Status | Meaning |
|--------|---------|
| `201 Created` | User created, JWT returned |
| `400 Bad Request` | Missing fields, invalid role, or Admin self-assignment attempt |
| `409 Conflict` | Username already taken |

**Success response body:**
```json
{
  "token":     "eyJhbGci...",
  "username":  "jsmith",
  "role":      "Employee",
  "expiresAt": "2025-01-01T11:00:00Z"
}
```

---

### `POST /api/auth/login`

Authenticate with existing credentials. Returns a JWT.

**Request body:**
```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

**Responses:**

| Status | Meaning |
|--------|---------|
| `200 OK` | Login successful, JWT returned |
| `400 Bad Request` | Missing username or password |
| `401 Unauthorized` | Wrong credentials |

---

## Role Rules

| Scenario | Result |
|---------|--------|
| `role` field omitted | Defaults to `Employee` ✅ |
| `role: "Employee"` | Accepted ✅ |
| `role: "Manager"` | Accepted ✅ |
| `role: "Admin"` | **400** — Admin cannot be self-assigned |
| `role: "superuser"` | **400** — Unknown role |

Admin accounts can only be created by an existing Admin via a privileged endpoint (to be implemented).

The `allowedRoles` field in the 400 response body always lists the valid options:
```json
{
  "message":      "'superuser' is not a valid role.",
  "allowedRoles": ["Employee", "Manager"],
  "hint":         "Omit the Role field to default to Employee."
}
```

---

## Seeded Test Users

The service starts with three in-memory accounts ready to use immediately:

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin |
| `manager` | `Manager@123` | Manager |
| `employee` | `Employee@123` | Employee |

> These users live in memory and reset on every restart. Replace with a database before going to production.

---

## Testing the Service

### 1 — Register a new user

```bash
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"newstaff","password":"Xyz@456"}'
```

### 2 — Login with seeded admin

```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

### 3 — Attempt to self-assign Admin role (expect 400)

```bash
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"hacker","password":"Xyz@456","role":"Admin"}'
```

Expected:
```json
{
  "message": "Role 'Admin' cannot be self-assigned during registration.",
  "allowedRoles": ["Employee", "Manager"],
  "hint": "Admin accounts must be created by an existing Admin."
}
```

### 4 — Verify your token at [jwt.io](https://jwt.io)

Paste the returned token and confirm:
- `iss` = `InsightERP`
- `aud` = `InsightERP-Users`
- `role` claim is present

---

## Troubleshooting

**`401 Unauthorized` on login with correct credentials**

Password hashing is SHA-256. Ensure the password was not changed in `AuthController.cs` after seeding.

---

**`409 Conflict` on register**

Username is already taken. Choose a different username or restart the service (resets in-memory store).

---

**Token rejected by API Gateway**

Both services must share the **same** `JWT_SECRET`, `Issuer`, and `Audience`. Check both `appsettings.json` files.

---

**Port 5001 already in use**

```powershell
# Windows
netstat -ano | findstr :5001
taskkill /PID <PID> /F
```

```bash
# Mac/Linux
lsof -i :5001
kill -9 <PID>
```

---

## Related

- [API Gateway README](../../ApiGateway/docs/README_APIGW.md)
- [CoreErpService README](../../CoreErpService/docs/README_COREERP.md)
- [Security Best Practices](../../../docs/SECURITY_BEST_PRACTICES.md)
