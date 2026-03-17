# 🔧 DevOps Sprint 1 — Troubleshooting Log
**Author:** Sanuk (DevOps Lead)
**Project:** InsightERP
**Date:** March 2026

> This document records every significant problem encountered during Sprint 1, why it happened, and exactly how it was fixed. It's written so that future team members (or future-you) can understand the reasoning — not just copy-paste the solution.

---

## Problem 1 — `unauthorized: authentication required` During Docker Push

### What happened
The CD pipeline was failing at the "Build and Push all images" step. The error was:
```
unauthorized: authentication required
```
Azure Container Apps was refusing to pull the Docker image from our private Azure Container Registry (ACR).

### Why it happened
When a Container App is first created, it doesn't automatically know the password to your private image registry. It's like telling someone to pick up a package from a locked warehouse but not giving them the key.

We were pushing images to ACR successfully, but when Azure Container Apps tried to *pull* the image to run it, it didn't have credentials configured.

### Fix
Added `az containerapp registry set` before each deployment. This command gives the Container App the credentials to unlock the ACR warehouse:
```bash
az containerapp registry set \
  --name "$APP" \
  --resource-group erp-rg \
  --server "$ACR_LOGIN_SERVER" \
  --username "$ACR_USERNAME" \
  --password "$ACR_PASSWORD"
```

**Lesson:** Creating a Container App and configuring its image registry credentials are two separate steps. Always do both.

---

## Problem 2 — Azure Login Failing (Permission Issues with Service Principal)

### What happened
The pipeline was failing when trying to log into Azure. The service principal (a kind of "robot account" for automation) didn't have sufficient permissions to deploy Container Apps.

### Why it happened
The service principal was created with limited permissions and didn't have the `Contributor` role on the resource group — meaning it could see Azure resources but couldn't modify them.

### Fix
Switched from a traditional service principal with a secret password to **OIDC (OpenID Connect)** — a modern, passwordless Azure authentication method:

1. Created a **User-Assigned Managed Identity** (`erp-github-mi`) in Azure
2. Gave it `Contributor` role on the `erp-rg` resource group
3. Configured a **Federated Credential** — this is the link that says "trust GitHub Actions from the `san-rat/ERP_backend` repository on the `dev` branch"
4. Added three GitHub secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
5. Updated the pipeline to use `azure/login@v2` with OIDC instead of a password

**Lesson:** OIDC is the modern, more secure way to authenticate GitHub Actions with Azure. No passwords means no passwords can be stolen.

---

## Problem 3 — Smoke Test Failing With 401 Error (Wrong Endpoint)

### What happened
The smoke test step was failing with:
```
curl: (22) The requested URL returned error: 401
```
But confusingly, the output also showed:
```
AuthService is running - sanuk is testing - dulain is watching.
```

### Why it happened
The smoke test was hitting **two** endpoints:
```bash
curl -fsS "$AUTH_SERVICE_URL/health"    # ← [AllowAnonymous] returns 200 ✅
curl -fsS "$AUTH_SERVICE_URL/db-check"  # ← [Authorize] returns 401 ❌
```

The `/health` endpoint worked fine — that's the message we saw. But `/db-check` has `[Authorize]` on it (requires a JWT token), so it always returns 401. GitHub Actions doesn't have a JWT token, so it always fails.

The stdout (health response) and stderr (curl error) appeared in interleaved order in the logs, making it look like `/health` was failing when `/health` was actually fine.

### Fix
Removed `/db-check` from the smoke test. Health checks in CI pipelines should only use unauthenticated endpoints:
```bash
# Only test the public health endpoint
curl -fsS "$AUTH_SERVICE_URL/health"
```

**Lesson:** Smoke tests in CI/CD pipelines must use public (unauthenticated) endpoints. Never test authenticated endpoints in automated pipelines without proper token handling.

---

## Problem 4 — ApiGateway Timing Out (Port Mismatch)

### What happened
After deploying, the ApiGateway URL was completely unreachable. Requests would spin in the browser for over a minute and timeout with no response.

### Why it happened
There was a port conflict in our ApiGateway `program.cs`:

```csharp
// This line was in program.cs:
builder.WebHost.UseUrls("http://*:5000");
```

And in the Dockerfile:
```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
```

In ASP.NET Core, `UseUrls()` in code **overrides** the `ASPNETCORE_URLS` environment variable. So even though the Dockerfile told the app to listen on port 8080, the hardcoded `UseUrls("5000")` made it listen on port 5000 instead.

Azure Container Apps was configured to forward traffic to **port 8080**. But the container was actually listening on **port 5000**. Azure would try to connect to 8080, get nothing back, and eventually timeout.

It was like knocking on a door at address 8080 when the person is actually sitting inside address 5000.

### Fix
Removed the `UseUrls()` line entirely:
```csharp
// REMOVED: builder.WebHost.UseUrls("http://*:5000");
```

Now:
- **Locally** (`dotnet run`): uses `Properties/launchSettings.json` which sets port 5000
- **In Azure Docker container**: uses `ASPNETCORE_URLS=http://+:8080` from the Dockerfile

**Lesson:** `UseUrls()` in code overrides environment variables. For containerized apps, let environment variables control the port — don't hardcode it in the program.

---

## Problem 5 — ApiGateway Routing to `localhost` in Azure

### What happened
Even after fixing the port, the ApiGateway was returning 502 Bad Gateway errors for every route (e.g., `/api/auth/login`).

### Why it happened
The `ocelot.json` routing config was written for local development:
```json
"DownstreamHostAndPorts": [{ "Host": "localhost", "Port": 5001 }]
```

But in Azure, **there is no `localhost:5001`** running AuthService. Each service runs in its own separate container with its own URL. "Localhost" inside the ApiGateway container only refers to the ApiGateway itself.

### Fix
Created a separate `ocelot.Production.json` file with the actual Azure internal hostnames:
```json
"DownstreamHostAndPorts": [{
  "Host": "authservice-dev.internal.victoriouscliff-19d215bb.southeastasia.azurecontainerapps.io",
  "Port": 443
}]
```

And updated `program.cs` to automatically load environment-specific Ocelot config:
```csharp
.AddJsonFile("ocelot.json", optional: false)
.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true)
```

When running in Azure (environment = `Production`), it loads `ocelot.Production.json` which overrides the base localhost settings.

**Lesson:** Configuration files should have environment-specific versions. Never hardcode hostnames that are different between dev and production.

---

## Problem 6 — Cold Start Timeouts (Scale to Zero)

### What happened
After periods of inactivity, the first request to the ApiGateway or AuthService would take 30-60+ seconds to respond. Sometimes it would timeout entirely.

### Why it happened
Azure Container Apps has a feature called **scale to zero** (`minReplicas: 0`). When no traffic hits a service for a while, Azure shuts down the container completely to save money. The first request after this happens triggers a **cold start** — Azure has to spin up a new container from scratch, which takes time.

With 8 services all potentially scaled to zero, even the gateway itself could be cold-started, making the whole system appear unresponsive.

### Fix
Set `minReplicas: 1` for the two most critical services — the ones the frontend always hits first:
```bash
az containerapp update \
  --name "apigateway-dev" \
  --resource-group erp-rg \
  --min-replicas 1

az containerapp update \
  --name "authservice-dev" \
  --resource-group erp-rg \
  --min-replicas 1
```

The other 6 internal services remain at `minReplicas: 0` — they only cold-start when the gateway first talks to them (which is less noticeable since the gateway already responded quickly).

**Lesson:** For public-facing services, `minReplicas: 1` eliminates cold starts at a small cost (~$5-15/month extra per service). Balance cost vs. user experience based on importance.

---

## Problem 7 — AuthService DB Connection String Not Set

### What happened
The AuthService was running, but its `/db-check` endpoint was returning 500 errors when tested manually — it couldn't connect to the Azure SQL database.

### Why it happened
The Container App was deployed with the Docker image, but the **runtime connection string** (how to connect to the database) was never configured on the Container App. The Dockerfile doesn't contain DB passwords — those must be injected at runtime for security reasons.

### Why we couldn't just use a plain environment variable
Storing a database connection string (which contains a password) as a plain env var is bad practice — it can show up in Azure portal logs, deployment outputs, and debug screens.

### Fix
Used Azure's **secret reference** pattern — a two-step process:
1. Store the connection string as an **encrypted secret** on the Container App
2. Create an **environment variable** that references the secret by name (the value is never exposed directly)

```bash
# Step 1: Store as encrypted secret
az containerapp secret set \
  --name authservice-dev \
  --secrets "auth-db-conn=$AUTH_DB_CONN"

# Step 2: Env var references the secret (never exposes the value)
az containerapp update \
  --name authservice-dev \
  --set-env-vars "ConnectionStrings__AuthDb=secretref:auth-db-conn"
```

The `AUTH_DB_CONN` value comes from a GitHub secret (`AUTH_DB_CONNECTION_STRING`), so the actual password is never visible in pipeline logs.

**Lesson:** Never store passwords in plain environment variables. Use the platform's secret management (Azure Container App secrets, AWS Secrets Manager, etc.) and reference them by name.

---

## Problem 8 — `ocelot.Production.json` Not Included in Docker Image

### What happened
After creating `ocelot.Production.json`, it wasn't being loaded by the ApiGateway in Azure — the gateway was still using the localhost config.

### Why it happened
The `.csproj` file only had `ocelot.json` listed to be copied to the output folder. `ocelot.Production.json` existed in the source directory but wasn't being packaged into the Docker image.

```xml
<!-- Only this was in the .csproj: -->
<Content Update="ocelot.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

When `dotnet publish` ran, it only copied `ocelot.json` to the output. The Docker image was built from that output — so `ocelot.Production.json` never made it into the container.

### Fix
Added the production file to the `.csproj`:
```xml
<Content Update="ocelot.Production.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

**Lesson:** Any JSON config file that ASP.NET Core needs at runtime must be marked for copy to output in the `.csproj` file. Otherwise it exists in your source code but not in the deployed application.

---

*Troubleshooting log by Sanuk — InsightERP DevOps Sprint 1 — March 2026*
