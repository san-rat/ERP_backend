# Product Service 📦

The **Product Service** is a core microservice in the InsightERP system responsible for managing product definitions, catalog details, and live inventory availability. It provides comprehensive APIs to add, edit, track, and deduct stock across all products in the ecosystem.

## 🚀 Core Features

- **Product Catalog Management**: Create, read, update, and delete (CRUD) product definitions including descriptions, pricing, and category assignments.
- **Data Isolation & Scoping**: Built fundamentally for multi-tenancy style isolation. Products and Stock ledgers are rigidly scoped to the specific User ID that created them using JWT `sub` Claim extraction. Users cannot view, update, or deduct stock from products they do not own.
- **Duplicate Prevention**: Natively intercepts duplicate SKU attempts during creation and updates, responding with a graceful `409 Conflict` instead of crashing the database.
- **Inventory Tracking**: Each product retains an associated, highly accurate inventory count that natively replicates between the `Products` table and the `Inventory` table to prevent de-syncs.
- **Stock Deduction & Reservations**: Offers a transactional endpoint (`POST /api/products/deduct-stock`) that allows the Order Service (or other clients) to confidently reserve and decrement stock.
- **Low Stock Thresholds**: Define customized low-stock tipping points for each product. The API dynamically flags low stock statuses (`isLowStock = true`) without expensive polling.
- **Role-Based Access**: 
  - `Admin` & `Employee`: General read/search access to their scoped data.
  - `Employee`: Full write, deduction, and creation access to their scoped data.

## 🗄️ Database & Schema

Product Service operates against its own defined schema in Azure SQL (local development utilizes Docker-hosted SQL Server).

### Key Tables
- `dbo.Products`: The main source of truth for items. Includes basic metadata (`Sku`, `Name`, `Price`), plus a cached `QuantityAvailable` tracker. Also rigidly enforces the `CreatedByUserId` Foreign Key for data isolation.
- `dbo.Inventory`: Granular stock details (`QuantityAvailable`, `QuantityReserved`, `LowStockThreshold`).
- `dbo.InventoryReservations`: A ledger tracking every successful stock deduction mapping a `ProductId`, quantity, and correlation `OrderId`.
- `dbo.LowStockAlerts`: Event ledger for products that dip below their configured low stock limit.
- `dbo.Categories`: Structured categorization for products.

> **Note on Migration**: The schema is strictly managed by raw SQL migration scripts inside `schemas/product/migrations/`. Entity Framework Core's `Migrate()` is disabled in this service to avoid clashing with the central CI script `setup-local-db.ps1`.

## 🔌 API Endpoints

### Products
- `GET /api/Products` — Retrieve paginated list of products. Accepts query parameters for pagination, name filtering, and category filtering.
- `GET /api/Products/{id}` — Lookup a single product.
- `POST /api/Products` — Create a new product and initialize its stock count in one atomic operation. Enforces UNIQUE SKU constraints.
- `PUT /api/Products/{id}` — Update item details and forcefully align stock. Responds with `409` if the updated SKU conflicts.
- `DELETE /api/Products/{id}` — Permanent removal of an item from the catalog.

### Stock & Inventory
- `GET /api/Products/stock` — Fetch the current live stock counts and low-stock flags across all of the authenticated user's active products.
- `GET /api/Products/{id}/stock` — Retrieve detailed stock ledger data for a specific item.
- `POST /api/Products/deduct-stock` — Strongly transactional stock decrement point. Evaluates available quantity, subtracts amounts, and creates an `InventoryReservation` record. Responds with `409 Conflict` if the requested quantity exceeds the current `QuantityAvailable`.

## 🏃‍♂️ Running Locally

1. Ensure the central Docker database stack is running via the `setup-local-db.ps1` script at the repository root. This guarantees the SQL container is up and running your migrations.
2. Build and run this service:
   ```bash
   cd src/ProductService
   dotnet run
   ```
3. Navigate to `http://localhost:5004/swagger` to interact with the API directly using Swagger UI. Always remember to append your Bearer JWT token from AuthService using the `Authorize` button.

## 🛡️ Authentication & Authorization

Every endpoint in this microservice securely enforces a valid JSON Web Token (JWT). The tokens must be issued specifically by the `InsightERP` issuer (configurable in `appsettings.json` via the `JwtSettings` block). The service locally validates the token's cryptographic signature without polling the initial Auth DB. Upon successful verification, the `ClaimTypes.NameIdentifier` (`sub` Claim) is extracted seamlessly to act as the `Guid? userId` argument spanning all Repository level operations.
