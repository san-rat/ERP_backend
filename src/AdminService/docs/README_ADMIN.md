# InsightERP - Admin Service

AdminService is the administrative microservice for staff management and admin-only dashboard data.

## Responsibilities

- Manage managers and employees in the shared `auth.users` and `auth.user_roles` tables
- Normalize legacy role rows (`ADMIN`, `MANAGER`, `USER`) to API-facing values (`Admin`, `Manager`, `Employee`)
- Bootstrap canonical role rows (`Admin`, `Manager`, `Employee`) with insert-only checks during create and update operations
- Reset user passwords directly by updating `auth.users.password_hash`
- Expose admin dashboard overview metrics from the shared ERP database

## Local Run

```bash
dotnet run --project src/AdminService
```

Swagger UI:

```text
http://localhost:5011/swagger
```

## Endpoints

- `POST /api/admin/users/managers`
- `POST /api/admin/users/employees`
- `GET /api/admin/users`
- `PUT /api/admin/users/{id}`
- `PATCH /api/admin/users/{id}/status`
- `POST /api/admin/users/{id}/reset-password`
- `GET /api/admin/dashboard/overview`
- `GET /health`

All non-health endpoints require an authenticated admin token. The authorization layer accepts both legacy `ADMIN` and canonical `Admin` role claims.

`AdminService` uses the same shared `ConnectionStrings:AuthDb` database connection contract as `AuthService`.
