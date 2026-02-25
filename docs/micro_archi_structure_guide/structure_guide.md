# AuthService Microservice Structure Guide

This document explains **every folder and file** in the `src/AuthService` microservice structure (as shown in the screenshot).  
For each item, it covers:

- **Why it exists / why it’s needed**
- **What type of files go inside**
- **How it affects the microservice and the overall system**

---

## Overview: What this structure achieves

This structure follows a **layered / clean architecture** style:

- **Controllers** handle HTTP (API endpoints)
- **Application** holds use-cases and business logic (service layer)
- **Domain** holds core business models (entities/value objects)
- **Infrastructure** contains external integrations (DB, JWT, external services)
- **Common** holds shared utilities (errors, middleware, response wrappers)

✅ This makes the service:
- easy to maintain
- easy to test
- easier to scale to more microservices
- safer to change without breaking other parts

---

# Folder-by-folder Breakdown

## `Application/`
### Why it’s needed
The **Application layer** contains the **service’s use-cases** (what the microservice actually *does*).  
It prevents your Controllers from becoming messy and prevents Infrastructure (DB/JWT) logic from mixing with business rules.

### What goes here
This layer typically contains:
- request/response models
- service logic (login/register)
- interfaces (contracts) for repositories and token providers
- validation rules

### Effect on microservice/system
- Keeps business rules consistent and reusable
- Allows swapping Infrastructure (DB implementation, token provider) without rewriting Controllers
- Improves testability (unit tests can target Application without real DB)

---

### `Application/DTOs/`
**DTO = Data Transfer Object**

#### Why needed
Controllers should not directly expose internal entities like `User`. DTOs help control what enters/exits your API.

#### What files go here
- `LoginRequest.cs`
- `LoginResponse.cs`
- `RegisterRequest.cs`
- `TokenResponse.cs`
- `UserDto.cs`

#### Effect
- Prevents security mistakes (like returning password hashes)
- Makes API contracts clear for frontend/mobile clients and other services
- Helps version API responses safely


---

### `Application/Interfaces/`
#### Why needed
This defines **contracts** the Application depends on, not implementations.

#### What files go here
- `IAuthService.cs`
- `IUserRepository.cs`
- `IJwtTokenService.cs`
- `IPasswordHasher.cs`

#### Effect
- Enables dependency injection properly
- Allows mocking in tests
- Lets you switch from Dapper → EF Core (or MySQL → Postgres) with minimal code changes

---

### `Application/Services/`
#### Why needed
Holds the actual **use-case logic**.

#### What files go here
- `AuthService.cs` (login/register logic)
- `PasswordService.cs`
- `TokenService.cs` (sometimes sits here, sometimes in Infrastructure)

#### Effect
- Controllers stay thin and clean
- Core business logic is centralized and consistent
- Makes future enhancements easier (refresh tokens, MFA, lockouts)

---

### `Application/Validators/`
#### Why needed
Prevents bad inputs from reaching your service logic/DB.

#### What files go here
- `LoginRequestValidator.cs`
- `RegisterRequestValidator.cs`

Typically used with FluentValidation or custom rules.

#### Effect
- Reduces runtime errors
- Improves API reliability
- Improves security (input sanitization, rule enforcement)

---

## `bin/`
#### Why it exists
Auto-generated output folder created when you build/run the project.

#### What’s inside
- compiled assemblies (`.dll`)
- runtime files

#### Effect
No effect on source logic. Not manually edited.

✅ Should be ignored by git.

---

## `Common/`
### Why it’s needed
This is a shared internal toolbox **within the microservice**.  
It keeps cross-cutting concerns out of Controllers/Application.

### What goes here
- exception handling helpers
- middleware
- standard response wrappers
- helper utilities

### Effect
- consistent error handling
- consistent API response format
- less duplication across controllers

---

## `Controllers/`
### Why it’s needed
Controllers define the **HTTP API endpoints** of the AuthService.

### What goes here
- `AuthController.cs`
- `HealthController.cs`

### Effect
- This is the “entry point” for clients (frontend, other services)
- Clean separation: Controllers only handle HTTP and call Application layer

✅ In a microservice system, other services should interact with AuthService through these endpoints (or via token validation).

---

## `Domain/`
### Why it’s needed
The Domain layer contains the **core business model** — your service’s “truth”.

### What goes here
- entities representing data + rules

### Effect
- prevents leaking DB-specific models everywhere
- supports future growth (roles, permissions, refresh tokens, sessions)

---

### `Domain/Entities/`
#### Why needed
Represents core objects in Auth domain.

#### What files go here
- `User.cs`
- `Role.cs`
- `UserRole.cs`
- `RefreshToken.cs`

#### Effect
- makes your domain stable even if DB structure changes
- avoids writing business rules inside controllers/SQL

---

### `Domain/ValueObjects/`
#### Why needed
Value objects represent “validated” domain concepts.

#### What files go here
- `Email.cs`
- `Password.cs` (not raw password, but rules/format)
- `JwtClaims.cs`

#### Effect
- makes domain safer (less invalid states)
- reduces repeated validation logic

⚠️ Optional (but good for maturity)

---

## `Infrastructure/`
### Why it’s needed
Infrastructure is where your system touches the outside world:
- DB
- JWT signing
- external APIs
- caching services

### What goes here
- repository implementations
- database connection and configuration
- JWT token generation logic

### Effect
- Application stays clean and independent
- Infrastructure can change without affecting business rules
- Supports real deployment environments (local docker DB, Azure MySQL)

---

### `Infrastructure/Persistence/`
#### Why needed
All database-related components live here.

#### What files go here
- `AuthDbContext.cs` (if EF Core)
OR
- `DbConnectionFactory.cs` (if Dapper/MySqlConnector)
- `UserRepository.cs`
- `RefreshTokenRepository.cs`
- `Migrations/` (optional if using EF or SQL scripts)

#### Effect
- clean separation between “business logic” and “data storage”
- DB changes don’t pollute controllers

---

### `Infrastructure/Security/`
#### Why needed
Security-related implementations such as JWT token creation and password hashing.

#### What files go here
- `JwtTokenService.cs`
- `PasswordHasher.cs`
- `TokenValidationParametersFactory.cs`

#### Effect
- keeps auth implementation consistent and reusable
- makes it easier to upgrade security (rotation, stronger hashing, claims changes)

---

## `obj/`
#### Why it exists
Auto-generated build temp folder.

#### What’s inside
- intermediate build files
- generated metadata

#### Effect
No effect on source logic.

✅ Must be ignored by git.

---

## `Properties/`
### Why it exists
Project configuration and local run settings.

### What goes here
Most commonly:
- `launchSettings.json` (local debugging ports, environment values)

### Effect
- Helps local development and debugging
- Does not affect Azure deployment unless you copy values manually

---

# File-by-file Breakdown

## `.dockerignore`
### Why needed
Prevents Docker from copying unnecessary files into the image.

### Common ignored files
- `bin/`
- `obj/`
- `.git/`

### Effect
- smaller images
- faster builds
- avoids leaking dev artifacts into production containers

---

## `appsettings.json`
### Why needed
Base configuration for the service.

### Common config values
- Logging
- JWT settings
- feature flags

### Effect
- central place for config defaults
- allows environment overrides

---

## `appsettings.Development.json`
### Why needed
Development-only overrides.

### Common values
- local DB connection string
- verbose logging

### Effect
- separates local settings from production
- avoids hardcoding dev-only values into production config

---

## `AuthService.csproj`
### Why needed
Defines the .NET project.

### Contains
- target framework
- package references (JWT, MySqlConnector, EF Core, etc.)
- build config

### Effect
Without this, the service cannot build.

---

## `AuthService.http`
### Why needed
Developer testing file (VS Code / Visual Studio).

### Contains
- sample HTTP requests for your endpoints

### Effect
No effect on production. Helps fast testing.

---

## `Dockerfile`
### Why needed
Defines how to containerize AuthService.

### Contains
- build step
- publish step
- runtime step
- port exposure

### Effect
Required for:
- CI/CD build + push to ACR
- Azure Container App deployment

---

## `Program.cs`
### Why needed
The service startup file.

### Contains
- dependency injection registrations
- middleware pipeline setup (routing, auth, swagger)
- controller mapping (`app.MapControllers()`)

### Effect
- connects Controllers + Application + Infrastructure together
- without correct wiring, API endpoints won’t work
- controls how requests flow through your service

---

# What this structure enables in a Microservice System

This structure helps you achieve:
- **independent deployability** (each microservice stands alone)
- **clean CI/CD** (build/test/deploy without coupling)
- **consistent patterns** (every future service can copy this template)
- **safer scaling** (you can add roles, refresh tokens, MFA later)

In short:
✅ This becomes your “gold standard template” for other microservices (DeliveryService, UserService, etc.)
