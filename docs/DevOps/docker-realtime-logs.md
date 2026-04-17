# Real-Time Docker Logs for Microservices

After running:

```powershell
.\scripts\run-docker-services.ps1
```

run these commands from the repository root:

```powershell
cd C:\Users\User\Desktop\coding\projects\2026\ERP_backend
docker compose -p erp_backend -f docker-compose.yml -f docker-compose.local.yml ps
docker compose -p erp_backend -f docker-compose.yml -f docker-compose.local.yml logs -f --tail=0 authservice
```

Replace `authservice` with any of these service names:

- `apigateway`
- `authservice`
- `customerservice`
- `orderservice`
- `productservice`
- `forecastservice`
- `predictionservice`
- `analyticsservice`
- `adminservice`
- `sqlserver`

## Useful Variants

Show the last 100 lines and keep following:

```powershell
docker compose -p erp_backend -f docker-compose.yml -f docker-compose.local.yml logs -f --tail=100 authservice
```

Follow logs for all services:

```powershell
docker compose -p erp_backend -f docker-compose.yml -f docker-compose.local.yml logs -f --tail=0
```

## Direct Container Commands

If you want to target containers directly:

```powershell
docker logs -f erp_backend-authservice-1
docker logs -f erp_backend-apigateway-1
docker logs -f erp-sqlserver-local
```

## Recommendation

Prefer:

```powershell
docker compose -p erp_backend -f docker-compose.yml -f docker-compose.local.yml logs -f <service-name>
```

This is more stable than hardcoding container names.
