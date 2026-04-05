# InsightERP Production Setup Guide

This guide explains how to create a production environment for the `main` branch from zero in a separate Azure account, while keeping it as close as possible to the current dev deployment.

This version assumes the colleague will create Azure resources manually in the Azure Portal. The repo will still use GitHub Actions for CI/CD after the one-time setup is complete.

## Target Outcome

- `main` becomes the production deployment branch
- production runs in separate Azure resources
- only `apigateway-prod` is public
- all services use one Azure SQL database with separate schemas
- demo seed migrations stay enabled
- deploys happen automatically on pushes and merges to `main`
- GitHub Actions uses repository secrets with `PROD_` prefixes

## What This Guide Assumes

- The colleague has access to the same GitHub repository
- The colleague can create resources in his own Azure subscription
- The colleague can add GitHub Actions repository secrets
- The backend code stays in the same repo
- The production frontend will call the production API Gateway URL
- Azure resources will be created in the Azure Portal, not by Azure CLI

## Important Repo Facts Before Starting

1. CI is already separate from CD. Keep one CI workflow and add a separate `cd-prod.yml`.
2. The current dev CD workflow updates existing Azure resources. It does not create them.
3. The current API Gateway production config is dev-specific today. It must be updated before production can work.
4. The ApiGateway Dockerfile path in this repo is `src/ApiGateway/dockerfile` with a lowercase `d`. Do not change that path by accident in Linux-based commands or GitHub Actions.

## Do We Need To Change ApiGateway Code?

Short answer: no C# logic change is required in `program.cs`.

What does need to change is the gateway routing configuration.

Why:

- The gateway is just a router.
- For each upstream route, it must know where the downstream service lives.
- The current `src/ApiGateway/ocelot.Production.json` points to the dev internal Azure hostnames like `authservice-dev.internal...`.
- If production lives in a different Azure account and a different Container Apps environment, those dev hostnames are wrong.

Recommended production approach in this guide:

- Keep the existing C# startup logic.
- Update `src/ApiGateway/ocelot.Production.json` so production routes by Container App name inside the Container Apps environment.
- Example: use `authservice-prod` instead of `authservice-dev.internal.<env>.<region>.azurecontainerapps.io`.

Why this is better:

- it avoids hardcoding Azure environment-specific random FQDN suffixes
- it keeps the config easier to maintain
- it matches Microsoft guidance that apps in the same Container Apps environment can call each other by app name

For production routing in `ocelot.Production.json`, use:

- `DownstreamScheme: "http"`
- host names like `authservice-prod`, `customerservice-prod`, `orderservice-prod`
- `Port: 80`

The Azure ingress layer still forwards traffic to each container's real internal target port `8080`.

## Recommended Production Naming

Use these names or keep the same pattern:

| Resource | Recommended name |
|---|---|
| Resource group | `erp-rg-prod` |
| Container Apps environment | `erp-prod-env` |
| Managed identity for GitHub | `erp-github-mi-prod` |
| API Gateway app | `apigateway-prod` |
| AuthService app | `authservice-prod` |
| CustomerService app | `customerservice-prod` |
| OrderService app | `orderservice-prod` |
| ProductService app | `productservice-prod` |
| ForecastService app | `forecastservice-prod` |
| PredictionService app | `predictionservice-prod` |
| AnalyticsService app | `analyticsservice-prod` |
| AdminService app | `adminservice-prod` |
| Azure SQL server | `insighterp-sql-prod-<unique>` |
| Azure SQL database | `insighterp_db_prod` |
| Azure Container Registry | `erpprodacr<unique>` |

Notes:

- ACR names must be globally unique, lowercase, and cannot contain dashes.
- Azure SQL server names must be globally unique.
- Container App names should stay under 32 characters.

## Prerequisites

- Azure subscription access
- Azure Portal access
- Docker installed locally
- Git installed locally
- Access to the backend repository
- Permission to add GitHub Actions repository secrets

Optional but useful:

- Azure CLI
- `gh` CLI
- `sqlcmd`

## Step 1: Plan And Record The Production Values

Before creating anything, decide the production names and record them in one place.

Use a table like this:

| Item | Example value | Where it will be used |
|---|---|---|
| Resource group | `erp-rg-prod` | Portal resource container, workflow |
| Region | `Southeast Asia` | All Azure resources |
| Container Apps environment | `erp-prod-env` | All 9 apps |
| Container Registry | `erpprodacr1234` | Docker image storage |
| SQL server | `insighterp-sql-prod-1234` | Production DB server |
| SQL database | `insighterp_db_prod` | Shared production DB |
| Managed identity | `erp-github-mi-prod` | GitHub OIDC deployment |
| GitHub org/user | `<owner>` | Federated credential |
| GitHub repo | `ERP_backend` | Federated credential |
| Branch | `main` | Federated credential and prod CD trigger |

## Step 2: Create The Resource Group In The Azure Portal

In the Azure Portal:

1. Search for `Resource groups`
2. Click `Create`
3. Select the production subscription
4. Enter the production resource group name
5. Choose the production region
6. Review and create

Recommended:

- keep all production resources in the same resource group
- use the same region for ACR, SQL, Container Apps, and the Container Apps environment

## Step 3: Create The Azure Container Registry In The Azure Portal

In the Azure Portal:

1. Search for `Container registries`
2. Click `Create`
3. Select the production subscription and resource group
4. Enter the production ACR name
5. Select the production region
6. Choose `Basic` SKU to mirror the current student setup unless the team wants higher capacity
7. Create the registry

After creation:

1. Open the registry
2. Go to `Access keys`
3. Enable `Admin user`
4. Record:
   - `Login server`
   - `Username`
   - `Password`

These values are needed for:

- the one-time bootstrap image push
- GitHub Actions repository secrets
- Container Apps image pull settings

## Step 4: Create The Production Azure SQL Server And Database In The Azure Portal

In the Azure Portal:

1. Search for `SQL databases`
2. Click `Create`
3. Select the production subscription and resource group
4. Enter the database name
5. For `Server`, choose `Create new`
6. Enter a globally unique SQL server name
7. Create a SQL admin login and password
8. Choose the same production region
9. Select a simple pricing tier that matches the project budget
10. Review and create

After creation:

1. Open the SQL server resource
2. Go to networking or firewall settings
3. Enable the option that allows Azure services and resources to access the server
4. Optionally add the colleague's public IP temporarily if manual DB inspection is needed from a local machine

Record:

- SQL server host: `<server-name>.database.windows.net`
- database name
- SQL admin username
- SQL admin password

Important:

- production keeps the same database model as dev: one Azure SQL database with multiple schemas
- demo seed migrations remain enabled in production because that is what you requested

## Step 5: Create The Container Apps Environment In The Azure Portal

In the Azure Portal:

1. Search for `Container Apps Environments`
2. Click `Create`
3. Select the production subscription and resource group
4. Enter the production environment name
5. Choose the same production region
6. Review and create

This environment is the shared internal network for all nine production services.

## Step 6: Create The GitHub Deployment Managed Identity In The Azure Portal

Create the identity:

1. Search for `Managed Identities`
2. Click `Create`
3. Select the production subscription and resource group
4. Choose the same production region
5. Enter the managed identity name
6. Create it

Record from the identity overview page:

- `Client ID`
- `Principal ID`

Also record:

- Azure `Tenant ID`
- Azure `Subscription ID`

The tenant ID and subscription ID can be found from the subscription or Microsoft Entra overview screens.

## Step 7: Grant The Managed Identity Access To The Production Resource Group

In the Azure Portal:

1. Open the production resource group
2. Go to `Access control (IAM)`
3. Click `Add role assignment`
4. Select the `Contributor` role
5. Assign access to a managed identity
6. Choose the production managed identity you created
7. Save

This allows GitHub Actions to update production resources during deployment.

## Step 8: Add The GitHub Federated Credential In The Azure Portal

In the Azure Portal:

1. Open the managed identity
2. Go to `Federated credentials`
3. Click `Add credential`
4. Choose the GitHub Actions scenario if the portal offers it
5. Set the GitHub organization or owner
6. Set the repository name
7. Set the entity type to `Branch`
8. Set the branch to `main`
9. Save

This is the production trust relationship that allows `azure/login` in GitHub Actions to authenticate by OIDC without a stored Azure password.

## Step 9: Add A Dev-Specific Ocelot File And Set Dev Environment

To avoid breaking the dev deployment, the dev Container Apps must use a different Ocelot file.

Actions:

- A new file now exists: `src/ApiGateway/ocelot.Dev.json` (dev internal hostnames).
- Set `ASPNETCORE_ENVIRONMENT=Dev` on the **apigateway-dev** Container App (and optionally all dev apps).
- Keep production on `ASPNETCORE_ENVIRONMENT=Production`.

This makes the gateway load:

- `ocelot.Dev.json` in dev
- `ocelot.Production.json` in prod

## Step 10: Update `ocelot.Production.json` For Production

This is the key gateway change.

Edit `src/ApiGateway/ocelot.Production.json` on the `main` branch when you are ready for production routing changes.

Make these changes:

1. Change every `DownstreamScheme` from `https` to `http`
2. Change every `Port` from `443` to `80`
3. Replace these hosts:

| Old host | New host |
|---|---|
| `authservice-dev.internal...` | `authservice-prod` |
| `customerservice-dev.internal...` | `customerservice-prod` |
| `orderservice-dev.internal...` | `orderservice-prod` |
| `productservice-dev.internal...` | `productservice-prod` |
| `forecastservice-dev.internal...` | `forecastservice-prod` |
| `predictionservice-dev.internal...` | `predictionservice-prod` |
| `analyticsservice-dev.internal...` | `analyticsservice-prod` |
| `adminservice-dev.internal...` | `adminservice-prod` |

4. Update `GlobalConfiguration.BaseUrl` to the real production gateway URL after the gateway app is created:

```json
"BaseUrl": "https://<YOUR_PROD_GATEWAY_FQDN>"
```

Why this works:

- all of these apps live in the same Container Apps environment
- the gateway can call them by app name
- this removes the need to hardcode the Azure environment-specific internal FQDN suffix

## Step 11: Build And Push The Initial Bootstrap Images To ACR

The Container Apps must exist before the production workflow can update them.

Because this repo has no infrastructure-as-code for app creation, the easiest bootstrap is:

1. build all images locally once
2. push them to the new ACR
3. create the Container Apps in the portal using those images
4. after that, let `cd-prod.yml` take over

Log in to ACR with the access-key values copied from the portal:

```bash
docker login <ACR_LOGIN_SERVER> -u <ACR_USERNAME> -p <ACR_PASSWORD>
```

From the repo root, build and push all nine images:

```bash
ACR_LOGIN_SERVER="<ACR_LOGIN_SERVER_FROM_PORTAL>"

SERVICES=(apigateway adminservice analyticsservice authservice customerservice forecastservice orderservice predictionservice productservice)
SRC_DIRS=(ApiGateway AdminService AnalyticsService AuthService CustomerService ForecastService OrderService PredictionService ProductService)
DOCKERFILES=(src/ApiGateway/dockerfile src/AdminService/Dockerfile src/AnalyticsService/Dockerfile src/AuthService/Dockerfile src/CustomerService/Dockerfile src/ForecastService/Dockerfile src/OrderService/Dockerfile src/PredictionService/Dockerfile src/ProductService/Dockerfile)

for i in "${!SERVICES[@]}"; do
  svc="${SERVICES[$i]}"
  dir="${SRC_DIRS[$i]}"
  dockerfile="${DOCKERFILES[$i]}"
  image="${ACR_LOGIN_SERVER}/${svc}:bootstrap"

  echo ">>> Building ${svc}"
  docker build -t "$image" -f "$dockerfile" "src/${dir}"

  echo ">>> Pushing ${svc}"
  docker push "$image"
done
```

## Step 12: Create The Nine Container Apps In The Azure Portal

Create the apps manually in the Azure Portal after the `bootstrap` images exist in ACR.

Important:

- `apigateway-prod` must use external ingress
- all other services must use internal ingress
- do not disable ingress entirely on the internal services, because the gateway must still reach them inside the Container Apps environment
- all services should use target port `8080`

Recommended production container apps:

| App | Image | Ingress | Target port | Min replicas |
|---|---|---|---|---|
| `apigateway-prod` | `apigateway:bootstrap` | External | `8080` | `1` |
| `authservice-prod` | `authservice:bootstrap` | Internal | `8080` | `1` |
| `customerservice-prod` | `customerservice:bootstrap` | Internal | `8080` | `0` |
| `orderservice-prod` | `orderservice:bootstrap` | Internal | `8080` | `0` |
| `productservice-prod` | `productservice:bootstrap` | Internal | `8080` | `0` |
| `forecastservice-prod` | `forecastservice:bootstrap` | Internal | `8080` | `0` |
| `predictionservice-prod` | `predictionservice:bootstrap` | Internal | `8080` | `0` |
| `analyticsservice-prod` | `analyticsservice:bootstrap` | Internal | `8080` | `0` |

For each app in the portal:

1. Search for `Container Apps`
2. Click `Create`
3. Select the production subscription and resource group
4. Set the container app name
5. Choose the existing production Container Apps environment
6. Choose image source from Azure Container Registry
7. Select the production ACR
8. Select the matching `bootstrap` image
9. Configure ingress as shown in the table above
10. Set the target port to `8080`
11. Set the minimum replicas as shown in the table above
12. Review and create

## Step 13: Configure Container App Secrets And Environment Variables In The Portal

After the apps exist, configure runtime secrets and env vars.

Use one SQL connection string for the production database:

```text
Server=tcp:<SQL_SERVER>.database.windows.net,1433;Initial Catalog=<SQL_DATABASE>;Persist Security Info=False;User ID=<SQL_ADMIN_USER>;Password=<SQL_ADMIN_PASSWORD>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Also generate one strong JWT secret for production.

Recommended rule:

- use a long random secret with at least 32 characters
- use the same JWT secret for AuthService and ApiGateway
- use the same issuer and audience values already used in the repo unless the team explicitly changes them everywhere

In the Azure Portal, for each container app:

1. Open the container app
2. Go to `Secrets`
3. Add the required secrets
4. Then update the container environment variables so they reference those secrets
5. Save and let Azure create a new revision if prompted

Required values:

| Container app | Secret name | Env var name | Env var value |
|---|---|---|---|
| `authservice-prod` | `auth-db-conn` | `ConnectionStrings__AuthDb` | `secretref:auth-db-conn` |
| `authservice-prod` | `jwt-secret` | `JWT_SECRET` | `secretref:jwt-secret` |
| `apigateway-prod` | `jwt-secret` | `JWT_SECRET` | `secretref:jwt-secret` |
| `productservice-prod` | `product-db-conn` | `ConnectionStrings__DefaultConnection` | `secretref:product-db-conn` |
| `productservice-prod` | `jwt-secret` | `JwtSettings__SecretKey` | `secretref:jwt-secret` |
| `predictionservice-prod` | `prediction-db-conn` | `ConnectionStrings__ChurnDb` | `secretref:prediction-db-conn` |

Optional but recommended:

| Container app | Secret name | Env var name | Env var value |
|---|---|---|---|
| `forecastservice-prod` | `forecast-db-conn` | `ConnectionStrings__insighterp_db` | `secretref:forecast-db-conn` |

Notes:

- The current dev workflow configures DB secrets for AuthService, ProductService, and PredictionService.
- ForecastService repository code can also use a DB connection string, even though the current dev workflow does not set one.
- If you want production to behave as close as possible to dev, the ForecastService DB env var can be skipped.
- If you want a safer production setup, keep the ForecastService DB env var and the real JWT secret.

## Step 14: Capture The Production Gateway URL

After `apigateway-prod` is created:

1. Open `apigateway-prod` in the Azure Portal
2. Copy the public FQDN from the overview or ingress section
3. Record the URL as:

```text
https://<PROD_GATEWAY_FQDN>
```

Update `src/ApiGateway/ocelot.Production.json`:

```json
"BaseUrl": "https://<PROD_GATEWAY_FQDN>"
```

## Step 15: Create The Production GitHub Repository Secrets

Go to:

`GitHub repo -> Settings -> Secrets and variables -> Actions -> Repository secrets`

Add these secrets:

| Secret name | Value |
|---|---|
| `PROD_AZURE_CLIENT_ID` | Managed identity `Client ID` |
| `PROD_AZURE_TENANT_ID` | Azure tenant ID |
| `PROD_AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `PROD_ACR_LOGIN_SERVER` | ACR login server |
| `PROD_ACR_USERNAME` | ACR admin username |
| `PROD_ACR_PASSWORD` | ACR admin password |
| `PROD_AZURE_SQL_SERVER` | `<sql-server>.database.windows.net` |
| `PROD_AZURE_SQL_DATABASE` | Production database name |
| `PROD_AZURE_SQL_USER` | Production SQL admin username |
| `PROD_AZURE_SQL_PASSWORD` | Production SQL admin password |
| `PROD_SQL_CONNECTION_STRING_AZURE` | Full production SQL connection string |
| `PROD_JWT_SECRET` | The production JWT secret |

Notes:

- All services share the same Azure SQL database, so one connection string secret is enough.
- If you decide not to inject ForecastService DB settings, you can keep the same single connection string secret and just skip wiring it to ForecastService.

## Step 16: Add `cd-prod.yml`

Create `.github/workflows/cd-prod.yml` by copying `cd-dev.yml`.

Change these items:

1. Trigger branch:
   - from `dev`
   - to `main`
2. All secret references:
   - from `secrets.AZURE_*`
   - to `secrets.PROD_AZURE_*`
   - from `secrets.ACR_*`
   - to `secrets.PROD_ACR_*`
3. App names:
   - from `*-dev`
   - to `*-prod`
4. Resource group:
   - from `erp-rg`
   - to your chosen production resource group name
5. Gateway URL resolution:
   - query `apigateway-prod`
6. DB secret names:
   - use the `PROD_*` repository secrets
7. JWT secret injection:
   - add steps to set the JWT secret on `authservice-prod`, `apigateway-prod`, and `productservice-prod`
8. Keep smoke tests through the gateway
9. Keep migrations enabled for all existing schema folders

Important:

- keep CI as a separate file
- do not merge CI and CD into one workflow
- in the prod workflow, use `src/ApiGateway/dockerfile`, not `src/ApiGateway/Dockerfile`

## Step 17: Push To `main` To Trigger The First Production Deployment

Once these are ready:

- Azure resources exist
- ACR contains the bootstrap images
- Container Apps exist
- `ocelot.Production.json` is updated
- repository secrets exist
- `cd-prod.yml` exists

Push to `main` or merge into `main`.

That first run should:

1. apply SQL migrations to the new production database
2. build and push the real images
3. update all `*-prod` Container Apps
4. set connection-string secrets
5. run smoke tests through the production gateway

## Step 18: Post-Deploy Smoke Tests

Test these URLs:

```text
https://<PROD_GATEWAY_FQDN>/health
https://<PROD_GATEWAY_FQDN>/auth/health
https://<PROD_GATEWAY_FQDN>/admin/health
https://<PROD_GATEWAY_FQDN>/forecast/health
https://<PROD_GATEWAY_FQDN>/prediction/health
```

AuthService should not be publicly exposed directly in production.

The production frontend should use:

```text
VITE_API_BASE_URL=https://<PROD_GATEWAY_FQDN>
```

## Step 19: Production Checklist

- `main` triggers `cd-prod.yml`
- only `apigateway-prod` has external ingress
- all other apps have internal ingress
- internal services are not direct-public
- `ocelot.Production.json` points to `*-prod` app names
- production SQL server and DB are separate from dev
- migrations run against the production DB
- production repository secrets exist
- gateway URL is recorded for the production frontend

## Known Differences From Current Dev

These are intentional or recommended:

1. AuthService is internal only in production.
2. The guide recommends setting a real JWT secret instead of relying on the hardcoded default.
3. The guide recommends app-name routing inside Container Apps instead of hardcoded internal FQDNs.
4. The guide keeps Azure resource creation manual in the Portal, then hands deployment over to GitHub Actions.
5. The guide adds an optional Forecast DB connection string because the code can use one even though the current dev workflow does not set it.

## Official References Used To Validate The Azure Setup Approach

- Azure Container Apps app-to-app communication:
  `https://learn.microsoft.com/en-us/azure/container-apps/connect-apps`
- Azure Container Apps quickstart:
  `https://learn.microsoft.com/en-us/azure/container-apps/get-started`
- Azure Container Registry quickstart:
  `https://learn.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal`
- Azure SQL Database portal quickstart:
  `https://learn.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart`
- Azure user-assigned managed identity:
  `https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/manage-user-assigned-managed-identities-azure-portal`
- Azure deployment from GitHub Actions:
  `https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions`
