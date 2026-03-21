# 🚀 DevOps Sprint 1 — InsightERP
**Author:** Sanuk (DevOps Lead)
**Project:** InsightERP — University Project
**Date:** March 2026
**Sprint Goal:** Build a fully automated pipeline that tests, packages, and deploys all 8 backend microservices to the cloud whenever code is pushed to GitHub.

---

## 🧠 First Things First — What Even Is DevOps?

Imagine you're baking cakes for a bakery. Every day you have to:
1. Check if the recipe still works (testing)
2. Bake the cake (build)
3. Package it into a box (containerize)
4. Deliver it to the shop (deploy)

Now imagine you have to do this **manually** every single time someone changes the recipe. Exhausting, right?

**DevOps** is the practice of automating all of that. You change the recipe (code), push it to GitHub, and a robot does everything else — testing, building, packaging, and delivering — automatically.

That robot is called a **CI/CD Pipeline**.

---

## 🏗️ What We Had Before This Sprint

Before this sprint, deploying the InsightERP backend was a completely manual process:
- A developer would build Docker images on their own laptop
- They'd upload them to the cloud manually
- If something broke, nobody got notified automatically
- Only **AuthService** had any automation — the other 7 services were completely manual

**The problem:** 8 microservices × manual deployment = chaos every time someone pushed code.

---

## 🎯 What We Built This Sprint

By the end of this sprint, every time a developer pushes code to the `dev` branch on GitHub:

1. ✅ All 8 services are automatically **tested**
2. ✅ All 8 Docker images are automatically **built**
3. ✅ All 8 images are automatically **uploaded** to Azure's private image registry
4. ✅ All 8 services are automatically **deployed** to Azure cloud
5. ✅ A health check **smoke test** confirms the deployment worked
6. ✅ The frontend on Vercel can immediately **talk to the new backend**

Zero manual steps. Push code → it's live.

---

## 🧩 Understanding the Pieces (The Tools We Used)

Think of this like a production line in a factory. Each tool plays a specific role:

### 🐙 GitHub Actions — *"The Manager"*
GitHub Actions is a built-in robot that lives inside GitHub. Whenever you push code, it reads a set of instructions (called a **workflow file**) and follows them step by step.

Our workflow files live in:
```
.github/workflows/
  backend-ci.yml   ← runs on every branch push
  cd-dev.yml       ← runs only when code is pushed to 'dev'
```

### 🐳 Docker — *"The Packaging Machine"*
Docker takes your application and wraps it in a **container** — a self-contained box that has everything the app needs to run (code, libraries, settings). 

Think of it like vacuum-sealing food. You can send that sealed package anywhere — any computer, any cloud server — and it'll work exactly the same.

Every service has a `Dockerfile` that describes how to build its container:
```
src/
  AuthService/Dockerfile
  ApiGateway/Dockerfile
  CustomerService/Dockerfile
  ... (all 8 services)
```

### 🗃️ Azure Container Registry (ACR) — *"The Warehouse"*
After Docker builds the container images, they need to be stored somewhere. ACR is Azure's private storage warehouse for Docker images. Think of it as a private USB drive in the cloud.

When a new image is built, it's pushed (uploaded) to ACR with a unique label:
```
erp.azurecr.io/authservice:abc123def   ← tagged with the exact commit SHA
erp.azurecr.io/authservice:latest      ← always points to the newest version
```

### ☁️ Azure Container Apps — *"The Cloud Servers"*
Azure Container Apps is where the actual microservices run. It takes the Docker image from ACR, starts it up as a live running server, and handles:
- Giving it a URL so it can receive requests
- Automatically scaling up if traffic increases
- Restarting it if it crashes

We have 8 Container Apps — one per microservice:

| Container App | Purpose |
|---|---|
| `apigateway-dev` | The front door — all requests go through here |
| `authservice-dev` | Login, registration, JWT tokens |
| `customerservice-dev` | Customer data management |
| `orderservice-dev` | Orders management |
| `productservice-dev` | Product catalog |
| `forecastservice-dev` | ML-based sales forecasting |
| `predictionservice-dev` | ML churn & segmentation |
| `analyticsservice-dev` | Dashboard analytics |

### 🛡️ OIDC Authentication — *"The Keyless Entry"*
To deploy to Azure, GitHub Actions needs permission. Instead of using a password (which is risky — passwords can be stolen), we used **OIDC (OpenID Connect)** — a modern, passwordless system.

Think of it like a face-recognition door lock. GitHub Actions proves its identity to Azure using a cryptographic proof, and Azure lets it in — no password ever stored anywhere.

### 🔀 Ocelot API Gateway — *"The Receptionist"*
In InsightERP, there are 8 backend services. The frontend doesn't need to know where each one lives — it just talks to one single URL (the API Gateway), and the Gateway figures out which service to forward the request to.

```
Frontend asks: "GET /api/customers/123"
   ↓
ApiGateway sees the /api/customers/ prefix
   ↓
ApiGateway internally forwards to CustomerService
   ↓
CustomerService responds, Gateway sends it back to the frontend
```

---

## 🔄 The CI Pipeline — *"The Quality Checker"*

**File:** `.github/workflows/backend-ci.yml`
**Triggers:** Every push to any branch

The CI (Continuous Integration) pipeline runs first and acts as a **quality gate** — if the code doesn't pass, you know immediately before it goes anywhere near production.

### What it does, step by step:

```
1. Checkout code from GitHub
        ↓
2. Restore all .NET packages (like npm install but for C#)
        ↓
3. Build the entire .NET solution
   → If this fails: broken code, deployment blocked
        ↓
4. Run all automated tests
   → If tests fail: deployment blocked
        ↓
5. Build Docker images for all 8 services
   → Proves the Dockerfiles aren't broken
```

---

## 🚢 The CD Pipeline — *"The Auto-Deployer"*

**File:** `.github/workflows/cd-dev.yml`
**Triggers:** Only when code is pushed to the `dev` branch

The CD (Continuous Deployment) pipeline takes what CI verified and actually ships it to Azure.

### What it does, step by step:

```
Step 1: Checkout code
        ↓
Step 2: Apply database migrations
        Run auth schema T-SQL migration scripts against Azure SQL
        (Only AuthService has a DB right now)
        ↓
Step 3: Login to Azure Container Registry (ACR)
        Docker needs a username/password to push images
        ↓
Step 4: Build & Push all 8 Docker images to ACR
        Loops through all 8 services:
        - Builds the Docker image using the Dockerfile
        - Tags it with the exact Git commit SHA
        - Pushes it to ACR
        ↓
Step 5: Login to Azure using OIDC (passwordless)
        ↓
Step 6: For each of the 8 Container Apps:
        a. Configure ACR credentials (so Container App can pull the image)
        b. Deploy the new image
        c. If gateway or auth: set minReplicas=1 (always keep warm)
        ↓
Step 7: Set the AuthService DB Connection String
        Saves it as an encrypted secret on the Container App
        Sets env var to reference it (password never exposed in logs)
        ↓
Step 8: Smoke Test
        Wait 30 seconds for the new revision to activate
        Hit the /health endpoint on AuthService
        If it responds 200 OK → deployment confirmed ✅
        If it fails → pipeline fails, team is notified ❌
```

---

## 🔑 GitHub Secrets — *"The Vault"*

Passwords, API keys, and connection strings should **never** be written directly in code files (which go to GitHub and could be seen by anyone). Instead, we store them as encrypted secrets in GitHub.

In the pipeline, they're referenced as `${{ secrets.SECRET_NAME }}` — GitHub replaces them with the real value at runtime, and they never appear in logs.

| Secret Name | What It Stores |
|---|---|
| `ACR_LOGIN_SERVER` | URL of the Azure Container Registry |
| `ACR_USERNAME` | Username to log in to ACR |
| `ACR_PASSWORD` | Password to log in to ACR |
| `AZURE_CLIENT_ID` | ID of the Azure Managed Identity (for OIDC) |
| `AZURE_TENANT_ID` | Azure directory/tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_SQL_SERVER` | Azure SQL Server hostname |
| `AZURE_SQL_DATABASE` | Database name (`insighterp_db`) |
| `AZURE_SQL_USER` | SQL user for running migrations |
| `AZURE_SQL_PASSWORD` | Password for the migration user |
| `AUTH_SERVICE_URL` | Public URL of the AuthService (for smoke test) |
| `AUTH_DB_CONNECTION_STRING_AZURE` | Full SQL Server connection string for AuthService at runtime |

---

## 🌐 Networking: Internal vs External

Not all services need to be reachable from the internet. We set:

| Service | Access | Why |
|---|---|---|
| `apigateway-dev` | **External** (public URL) | Frontend talks to this |
| `authservice-dev` | **External** (public URL) | Smoke tests + direct testing |
| All other 6 services | **Internal only** | Only reachable from within Azure, through the gateway |

This is a security best practice — you minimize the "attack surface" by only exposing what needs to be public.

---

## 📦 Docker: How the Containers Are Built

Every service follows the same Dockerfile pattern (except ApiGateway which has extras):

```dockerfile
# Stage 1: Build (uses the heavy SDK image)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY [ServiceName].csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime (uses the lightweight runtime image)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080   ← tells the app to listen on port 8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ServiceName.dll"]
```

**Why port 8080?** Azure Container Apps expects your app to listen on port 8080 by default. All services are configured to match this.

---

## 🗺️ Ocelot Routing: Local vs Azure

Ocelot needs different routing config depending on where it runs:

**Local development** (`ocelot.json`):
```
AuthService → localhost:5001
CustomerService → localhost:5002
...
```

**Azure production** (`ocelot.Production.json`):
```
AuthService → authservice-dev.internal.*.azurecontainerapps.io:443
CustomerService → customerservice-dev.internal.*.azurecontainerapps.io:443
...
```

The `Program.cs` loads the right file automatically based on the `ASPNETCORE_ENVIRONMENT` variable:
- In Azure: defaults to `Production` → loads `ocelot.Production.json`
- Locally with `dotnet run`: defaults to `Development` → loads `ocelot.json`

---

## 🌍 Frontend Connectivity (Vercel)

The frontend is deployed separately on Vercel. It connects to the backend through one single environment variable:

```
VITE_API_BASE_URL = https://apigateway-dev.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io
```

All API calls from the React frontend hit the ApiGateway URL. The gateway handles routing to the correct backend service. No frontend code needs to know individual service URLs.

**The flow for a login:**
```
User clicks Login on Vercel frontend
        ↓
POST /api/auth/login sent to ApiGateway URL
        ↓
ApiGateway (Ocelot) forwards to AuthService internally
        ↓
AuthService validates credentials → returns JWT token
        ↓
Frontend stores JWT in sessionStorage
        ↓
All future requests include: Authorization: Bearer <token>
```

---

## 📊 What Was Achieved

| Before Sprint 1 | After Sprint 1 |
|---|---|
| Manual deployment of 1 service | Automated deployment of all 8 services |
| No testing in pipeline | .NET build + tests run automatically |
| Hardcoded Azure credentials | Secure OIDC passwordless auth |
| Gateway pointed to localhost | Gateway uses Azure internal hostnames |
| No smoke testing | Health check confirms every deployment |
| Frontend talks to localhost | Frontend connects to Azure via gateway URL |
| Cold starts on all services | API Gateway always warm (minReplicas=1) |

---

## 🔮 What's Next (Sprint 2 Backlog)

1. **`JWT_SECRET` as a proper secret** — Currently falls back to a hardcoded default. Needs to be a GitHub secret injected at deploy time.
2. **CD blocked by CI** — CD should only run if CI (tests) passes first.
3. **Smoke test through gateway** — Test `/auth/health` via the gateway URL, not just directly.
4. **Production pipeline** — A separate `cd-prod.yml` for the `main` branch deploying to a production environment.
5. **Database-backed AuthService** — Now uses SQL Server / Azure SQL. Next step is extending the same persistence pattern to the remaining services.
6. **DB connections for other services** — CustomerService, OrderService, etc. will need their own DB connection strings.
7. **Build caching** — Cache Docker layers to cut build time by ~60%.

---

## 📁 Key Files Reference

| File | What It Does |
|---|---|
| `.github/workflows/backend-ci.yml` | CI pipeline — builds and tests all services |
| `.github/workflows/cd-dev.yml` | CD pipeline — deploys all services to Azure |
| `src/ApiGateway/ocelot.json` | API Gateway routing rules (local dev) |
| `src/ApiGateway/ocelot.Production.json` | API Gateway routing rules (Azure production) |
| `src/ApiGateway/program.cs` | Gateway startup — loads config, sets up Ocelot |
| `src/*/Dockerfile` | Container build instructions for each service |
| `db/auth/migrations/*.sql` | SQL scripts to set up the auth database schema |
| `scripts/apply_migrations.sh` | Shell script that applies SQL migrations in order |

---

*Documentation written by Sanuk — InsightERP DevOps Sprint 1 — March 2026*
