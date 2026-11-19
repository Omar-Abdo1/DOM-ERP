# DomCoreERP — Backend MVP

A lightweight multi-tenant ERP backend for **DOM CORE agency**, built with ASP.NET Core 9 and Clean/Onion architecture.

Two independent companies operate under one system with strict data isolation. A single SuperAdmin owns the system; each company has its own admin and employees.

---

## Table of Contents

- [What it does](#what-it-does)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Setup](#setup)
- [Configuration](#configuration)
- [Database](#database)
- [Running](#running)
- [Demo accounts](#demo-accounts)
- [API documentation](#api-documentation)
- [Multi-tenancy model](#multi-tenancy-model)
- [Testing](#testing)
- [Important decisions](#important-decisions)
- [Project structure](#project-structure)
- [Known limitations / post-MVP backlog](#known-limitations--post-mvp-backlog)

---

## What it does

- **Authentication** — JWT access tokens (30 min) + refresh tokens (7 days) with rotation and reuse detection.
- **Multi-tenancy** — SuperAdmin + CompanyAdmin + Employee; each user belongs to one tenant (or none, for SA).
- **Products** — per-tenant CRUD; SKU unique per company; low-stock detection.
- **Customers** — per-tenant CRUD; same name/phone can exist across tenants.
- **Inventory** — ledger-based stock changes with manual adjustments; RowVersion + retry for concurrency.
- **Invoices** — Draft → Issued → Paid/PartiallyPaid → Cancelled; snapshots preserve historical prices.
- **Payments** — partial and full; overpayment rejected; RowVersion + retry for concurrent payments.
- **Activity Logs** — every mutation writes an audit row in the same transaction; real actor preserved even when SA operates inside a company workspace.
- **Dashboard** — system-wide (SA) and per-tenant (Admin/Employee) aggregates.

---

## Tech stack

| Component | Choice | Why |
|---|---|---|
| **Runtime** | .NET 9 | Latest LTS-track; required by the task |
| **API** | ASP.NET Core Web API | Standard for JSON APIs |
| **Database** | SQL Server 2022 | ACID transactions, `rowversion` for optimistic concurrency, mature |
| **ORM** | EF Core 9 | Change tracking, LINQ, migrations, query filters |
| **Auth** | ASP.NET Identity Core + JWT | Battle-tested password hashing (PBKDF2 + 100k iterations), lockout, security stamps |
| **Validation** | DataAnnotations | Minimal footprint for MVP; FluentValidation was deliberately not used to keep scope small |
| **Logging** | Serilog → Console + rolling file | Structured logs; `LogContext` for correlation |
| **API docs** | Swashbuckle | Automatic Swagger UI from attributes |
| **Testing** | xUnit + FluentAssertions + `WebApplicationFactory` | Integration tests against the real HTTP pipeline |

---

## Architecture

Four projects, dependency-inverted (Onion/Clean):

```
API  →  Service  →  Repository  →  Core
 │                                    ▲
 └────────────────────────────────────┘
```

| Layer | Responsibility |
|---|---|
| **Core** | Entities, enums, DTOs, service interfaces, repository interfaces, pagination, exceptions. **No EF Core, no ASP.NET Core.** |
| **Repository** | `DomCoreContext : IdentityDbContext`, EF configurations, generic repository, `UnitOfWork`, migrations, seeders. |
| **Service** | Business logic — pure C#, depends on Core + Repository via abstractions. |
| **API** | Controllers (thin), middleware, `CurrentUserService` (adapter from `HttpContext` → `ICurrentUserService`), Swagger, `Program.cs`. |

### SOLID principles applied

- **S**ingle Responsibility — one service per aggregate (`ProductService`, `InvoiceService`, etc.); one repository method per concern.
- **O**pen/Closed — new statuses (invoice state machine, inventory movement types) extend via enums, not by editing `switch` statements.
- **L**iskov Substitution — `IGenericRepository<T>` honored by `GenericRepository<T>` and `TestGenericRepository<T>`.
- **I**nterface Segregation — small focused interfaces (`ICurrentUserService`, `ITokenService`, `IRetryService`, `IActivityLogger`) rather than a God-object "service."
- **D**ependency Inversion — services depend on interfaces; EF Core only appears in Repository/API layers.

---

## Prerequisites

- **.NET 9 SDK** — https://dotnet.microsoft.com/download
- **SQL Server 2022** (or newer) — local instance or Docker:
  ```bash
  docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123!" \
    -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
  ```
- **`dotnet-ef` CLI tool** — `dotnet tool install --global dotnet-ef`
- **Optional:** DBeaver or SSMS to inspect the DB

---

## Setup

```bash
git clone https://github.com/Omar-Abdo1/DOM-ERP.git
cd DOM-ERP

dotnet restore
dotnet build
```

---

## Configuration

### Two config files

| File | Contents | Committed? |
|---|---|---|
| `appsettings.json` | Placeholder values only | ✅ Yes |
| `appsettings.Development.json` | Real SQL password, JWT secret, seed credentials | ❌ **Gitignored** |

**Create `appsettings.Development.json`** if missing:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=DomCoreERP;User Id=sa;Password=YOUR_SA_PASSWORD;TrustServerCertificate=True;"
  },
  "JWT": {
    "IssuerIP": "DomCoreERP",
    "AudienceIP": "DomCoreERP.Frontend",
    "SecretKey": "REPLACE_WITH_64_CHAR_RANDOM_STRING",
    "DurationInMinutes": 30,
    "RefreshTokenDays": 7
  },
  "Seed": {
    "SuperAdmin": {
      "Email": "omar@domcore.com",
      "Password": "ChangeMe!123",
      "FullName": "Omar Abdo"
    },
    "DemoPassword": "Demo!123"
  }
}
```

**Generate a strong JWT secret:**
```bash
openssl rand -base64 64 | tr -d '\n'
```

### Configuration keys reference

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | SQL Server connection string |
| `JWT:IssuerIP` / `JWT:AudienceIP` | JWT iss/aud claims (any string; must match validator) |
| `JWT:SecretKey` | HS256 signing key (≥ 32 chars) |
| `JWT:DurationInMinutes` | Access token lifetime |
| `JWT:RefreshTokenDays` | Refresh token lifetime |
| `Seed:SuperAdmin:Email/Password` | SuperAdmin credentials (never hardcoded) |
| `Seed:DemoPassword` | Password for seeded CompanyAdmins and Employees |

---

## Database

### Apply migrations + seed

Run the app — migrations and seeding run automatically on startup:

```bash
dotnet run --project src/DomCoreERP.API
```

First run output:
```
info: Applying migrations...
info: Migrations applied.
info: Seeding roles...
info: Seeding Super Admin...
info: Seeding demo data...
info: Database ready.
info: Now listening on: http://localhost:8000
```

### Reset the demo environment

```bash
dotnet ef database drop --project src/DomCoreERP.Repository --force
dotnet run --project src/DomCoreERP.API
```

Fresh DB + reseeded data in ~10 seconds.

### Manually apply migrations (optional)

```bash
dotnet ef database update \
  --project src/DomCoreERP.Repository
```

---

## Running

```bash
dotnet run --project src/DomCoreERP.API
```

- HTTP: `http://localhost:8000`
- Swagger: `http://localhost:8000/swagger`

The `launchSettings.json` profile fixes the port to 8000. If you need a different port:

```bash
dotnet run --project src/DomCoreERP.API --urls "http://localhost:5000"
```

---

## Demo accounts

Seeded automatically on first startup:

| Role | Email | Password | Company |
|---|---|---|---|
| **SuperAdmin** | `omar@domcore.com` | `ChangeMe!123` | — (system-wide) |
| **CompanyAdmin A** | `admin.a@companya.com` | `Demo!123` | Company A |
| **CompanyAdmin B** | `admin.b@companyb.com` | `Demo!123` | Company B |
| **Employee A1–A5** | `emp.a1@companya.com` … `emp.a5@companya.com` | `Demo!123` | Company A |
| **Employee B1–B5** | `emp.b1@companyb.com` … `emp.b5@companyb.com` | `Demo!123` | Company B |

**Seed data:**
- Company A: iPhone 15, AirPods Pro, iPad Air (3 products); Mohamed Ahmed, Fatima Ali (2 customers)
- Company B: Samsung S24, Galaxy Buds (2 products); Mohamed Ahmed, Khaled Ibrahim (2 customers)
- "Mohamed Ahmed" appears in both tenants with the **same phone** — this demonstrates tenant isolation at the data layer (composite uniqueness is per-tenant).

---

## API documentation

**Swagger UI:** `http://localhost:8000/swagger`

All endpoints require `Authorization: Bearer <token>` except `/api/auth/login` and `/api/auth/refresh`.

### Endpoint groups

| Group | Path | Roles |
|---|---|---|
| Auth | `/api/auth/*` | Public / authenticated |
| Users | `/api/users/*` | SA, CompanyAdmin |
| Companies | `/api/companies/*` | SA (writes); any authenticated (own read) |
| Products | `/api/products/*` | Any read; SA + CompanyAdmin write |
| Customers | `/api/customers/*` | Any read; SA + CompanyAdmin write |
| Inventory | `/api/inventory/*` | Any read; SA + CompanyAdmin adjust |
| Invoices | `/api/invoices/*` | Any read; all roles create/issue; SA + CompanyAdmin cancel |
| Payments | `/api/payments/*` | Any read; all roles record |
| Activity Logs | `/api/activity-logs` | SA + CompanyAdmin |
| Dashboard | `/api/dashboard/*` | SA (system); any (company) |

### Example: login and fetch products

```bash
# Login
TOKEN=$(curl -s -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin.a@companya.com","password":"Demo!123"}' | jq -r .accessToken)

# Products (only Company A)
curl -s http://localhost:8000/api/products \
  -H "Authorization: Bearer $TOKEN" | jq
```

### SuperAdmin workspace switching

SuperAdmin can scope any request to a single company via the `X-Company-Id` header:

```bash
SA_TOKEN=<superadmin-token>
COMPANY_A_ID=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

# All products (no header — SA sees everything)
curl -s http://localhost:8000/api/products \
  -H "Authorization: Bearer $SA_TOKEN" | jq .totalCount   # → 5

# Only Company A's products
curl -s http://localhost:8000/api/products \
  -H "Authorization: Bearer $SA_TOKEN" \
  -H "X-Company-Id: $COMPANY_A_ID" | jq .totalCount       # → 3
```

**The header is ignored for CompanyAdmin and Employee** — they always see only their own company.

---

## Multi-tenancy model

### Tenant identifier

Every tenant-scoped entity has a `CompanyId` column. The 8 entity types are:

`User`, `Product`, `Customer`, `Invoice`, `Payment`, `InventoryMovement`, `ActivityLog` (+ `RefreshToken` transitively via `User`).

`Company` itself is not tenant-scoped — it **is** the tenant.

### Two layers of defence

**Layer 1 — EF Core global query filters** (primary)

Every read/update/delete on a tenant-scoped entity automatically gets `WHERE CompanyId = @resolvedCompanyId` appended by `DomCoreContext.OnModelCreating`. No code can forget it — the filter is on the model.

**Layer 2 — explicit ownership checks** (belt-and-suspenders)

Service methods that **link** entities across aggregates validate that both sides belong to the same company:

```csharp
// InvoiceService.CreateDraftAsync
if (customer.CompanyId != invoice.CompanyId) throw new ForbiddenException(...);
if (product.CompanyId != invoice.CompanyId)  throw new ForbiddenException(...);
```

This is required because the query filter works per-query, not across joins.

### The two identifiers

| Identifier | Source | Semantics |
|---|---|---|
| `CompanyId` (in JWT) | `user.CompanyId` claim | **Structural** — which company the user belongs to (permanent) |
| `ResolvedCompanyId` | `X-Company-Id` header (SA only) else `CompanyId` | **Request-scoped** — which tenant's data this request should see |

**CompanyAdmin / Employee** — always `ResolvedCompanyId == CompanyId`. Header has no effect.

**SuperAdmin**:
- No header → `ResolvedCompanyId = null` → filter allows all tenants (system dashboard view)
- With header → `ResolvedCompanyId = header` → filter scoped to that company (workspace view)

### The 404 vs 403 rule

| Case | Response | Why |
|---|---|---|
| Cross-tenant **entity read** (filtered entity) | **404** | Tenant filter hides the row — don't leak existence |
| Cross-tenant access to **Company** (unfiltered entity) | **403** | Company exists; caller explicitly not permitted |
| Cross-tenant **link** (invoice → foreign customer) | **403** | Both exist; the *association* is forbidden |

### Identity preservation guarantee

When SuperAdmin operates inside a company workspace:

- The activity log records `ActorUserId = SuperAdmin's ID`
- The activity log records `ActorRole = "SuperAdmin"` (not "CompanyAdmin")
- The activity log records `CompanyId = affected company`

The spec's requirement — *"The system must never disguise the Super Admin's identity as the Company Admin"* — is enforced by construction: `CurrentUserService` reads claims from the JWT, never from the header. The header only affects `ResolvedCompanyId`, not `UserId` or `Role`.

---

## Testing

### Setup

```bash
export DOMCORETEST_SA_PASSWORD='your_sa_password'
dotnet test tests/DomCoreERP.Tests
```

Tests use `WebApplicationFactory<Program>` — they run against a **real ASP.NET Core pipeline** with a **fresh SQL Server test DB** per test collection (auto-dropped on dispose).

### Coverage

| Suite | Tests | Proves |
|---|---|---|
| `MultiTenancy/ProductsIsolationTests` | 6 | Read scoping, cross-tenant 404, DTO CompanyId ignored, header spoof ignored, Employee role |
| `MultiTenancy/CustomersIsolationTests` | 3 | Read scoping, cross-tenant 404, Employee role |
| `MultiTenancy/UsersIsolationTests` | 4 | List scoping, cross-tenant 404, DTO coercion, Employee 403 |
| `MultiTenancy/InvoicesAndPaymentsIsolationTests` | 5 | Invoice list scoping, cross-tenant 404, cross-tenant pay, cross-tenant customer/product in invoice |
| `MultiTenancy/SuperAdminWorkspaceTests` | 5 | No-header sees-all, header scoping, real-actor preservation in activity log, system-log isolation |
| `BusinessLogic/InvoiceTests` | 5 | Totals math, draft no-deduct, issue deducts, qty validation, cancellation restores |
| `BusinessLogic/PaymentTests` | 5 | Partial, full, overpay rejected, draft rejected, cancel-with-payments rejected |

**Total: 33 tests.**

---

## Important decisions

### JWT access + refresh tokens (not session cookies)

- **Access token** is stateless — 30 min, verified by HMAC signature only. No DB lookup per request. Scales horizontally.
- **Refresh token** is stateful — random 44-char string, stored in `RefreshTokens` table, 7-day lifetime, revocable.
- **Rotation on every refresh** — the old refresh token is revoked and replaced. If a revoked token is re-presented, the entire chain is killed (theft detection). Standard Auth0/Okta pattern.

**Why not just long-lived access tokens?** Because they can't be revoked before expiry. Short access + long refresh gives you stateless speed with revocable sessions.

### RowVersion on Product, Invoice, Payment

SQL Server's `rowversion` column auto-increments on every UPDATE. EF Core uses it as an optimistic concurrency token — concurrent writes get `DbUpdateConcurrencyException` on the second one instead of silently overwriting.

**Critical for ERP.** Two simultaneous invoice issuances for the same product would otherwise both read `CurrentStock = 50` and both write `48` — the second overwrites the first, losing 2 units. With RowVersion, one succeeds and the other retries with fresh data.

### RetryService for transient conflicts

`IRetryService.ExecuteOnConflictAsync` wraps mutating operations with:
- **Catch:** `DbUpdateConcurrencyException` (RowVersion) + `DbUpdateException` with unique-violation inner (invoice number collisions)
- **Backoff:** exponential with jitter, floor 25ms, cap 5s
- **Max 5 attempts** → then throw `503 Service Unavailable` (honest "try again later")

**Crucially:** the retry closure re-runs **all business validations** from scratch on each attempt. This is why overpayment retries don't accidentally double-charge — the second attempt sees the updated `RemainingAmount` and throws `400 Overpayment`.

### Invoice snapshots

`InvoiceItem` stores `ProductName`, `ProductCode`, `UnitPrice` as **snapshots** at draft creation. If the product is later renamed or repriced, historical invoices remain accurate. `ProductId` is kept for traceability, but display always uses the snapshot.

**This is why `InvoiceService.GetByIdAsync` never reads `product.SellingPrice`** — it reads `item.UnitPrice` from the frozen snapshot.

### Ledger-based inventory

`InventoryMovement` is an append-only ledger. Every change to `Product.CurrentStock` is paired with a movement row recording:
- `MovementType` (Opening, Sale, Adjustment, CancellationRestore)
- Signed `Quantity` (+in / -out)
- `PreviousStock` / `NewStock` checkpoint
- `ReferenceType` + `ReferenceId` (pointer to source, e.g., "Invoice", invoice GUID)

**Invariant:** `Product.CurrentStock == SUM(InventoryMovement.Quantity)` for every product. Verified manually after every operation.

### ActivityLog written in the same transaction

Every service mutation calls `_activityLogger.Log(...)` **before** `_uow.CompleteAsync()`. The log entity is queued in the same `DbContext`, so a single `SaveChangesAsync()` persists the mutation AND its log atomically. If the log insert fails, the business change is rolled back too — **no mutation without an audit trail**.

### No hard deletes on business entities

`Product`, `Customer`, `User`, `Company` use `IsActive` / `Status` flags. `Invoice` uses `Status = Cancelled`. Deactivation cascades appropriately:
- Deactivate company → revokes all its users' refresh tokens (forces logout)
- Deactivate user → revokes all their refresh tokens
- Cancel invoice → restores stock via `CancellationRestore` movements

`onDelete: Restrict` on all FKs prevents accidental cascade deletes. Only `InvoiceItems → Invoices` and `RefreshTokens → Users` cascade (both are truly owned children).

### No CQRS, no MediatR, no AutoMapper runtime mapping

- **No CQRS** — overkill for MVP; services handle both reads and writes.
- **No MediatR** — the extra indirection adds no value at this scale.
- **AutoMapper** is referenced but not heavily used; DTOs are mapped in `ToDetail`-style methods for transparency.

This is deliberate — the spec says *"Avoid unnecessary complexity and overengineering."*

---

## Project structure

```
DomCoreERP.sln
├── src/
│   ├── DomCoreERP.Core/
│   │   ├── Arguments/           PaginationArg
│   │   ├── Config/              JwtSettings, SeedSettings
│   │   ├── Constants/           Roles, ActivityActions, ActivityEntityTypes
│   │   ├── DTOS/                Request/response shapes (no entities leak)
│   │   ├── Entities/            Domain models (User : IdentityUser<Guid>)
│   │   ├── Enums/               CompanyStatus, InvoiceStatus, PaymentMethod, etc.
│   │   ├── Exceptions/          ApiException + subclasses (NotFound, Forbidden, ...)
│   │   ├── Extensions/          CurrentUserExtensions
│   │   ├── Pagination/          PagedResult<T>
│   │   ├── Repositories/        IGenericRepository<T>
│   │   ├── Services/            Service interfaces
│   │   └── IUnitOfWork.cs
│   ├── DomCoreERP.Repository/
│   │   ├── Configurations/      EF fluent configs per entity
│   │   ├── DataSeeding/         AdminSeeder, DbSeeder, RoleSeeder
│   │   ├── DesignTime/          EF design-time factory + stub current user
│   │   ├── Migrations/          Initial migration
│   │   ├── DomCoreContext.cs    IdentityDbContext + tenant filters
│   │   ├── GenericRepository.cs
│   │   └── UnitOfWork.cs
│   ├── DomCoreERP.Service/
│   │   ├── ActivityLogService.cs
│   │   ├── ActivityLogger.cs
│   │   ├── AuthService.cs
│   │   ├── CompanyService.cs
│   │   ├── CustomerService.cs
│   │   ├── DashboardService.cs
│   │   ├── InventoryService.cs
│   │   ├── InvoiceService.cs
│   │   ├── PaymentService.cs
│   │   ├── ProductService.cs
│   │   ├── RetryService.cs
│   │   ├── TokenService.cs
│   │   └── UserService.cs
│   └── DomCoreERP.API/
│       ├── Controllers/
│       ├── ErrorResponses/
│       ├── ExtensionsMethod/    DI wiring, JWT, Swagger, DB init
│       ├── Helpers/             Pagination helper
│       ├── MiddleWares/         ExceptionMiddleWare
│       ├── Services/            CurrentUserService (adapter)
│       ├── appsettings.json     Placeholder template
│       └── Program.cs
└── tests/
    └── DomCoreERP.Tests/
        ├── Fixture/             TestWebApplicationFactory
        ├── Helpers/             TestAuth, TestConstants
        ├── MultiTenancy/        23 isolation tests
        └── BusinessLogic/       10 invoice/payment tests
```

---

## Known limitations / post-MVP backlog

These are **intentionally out of scope** per the task spec, and safe to add later without rewrites:

### Feature gaps
- **No purchase/supplier management** — only sales side implemented
- **No returns/refunds** — cancel blocks paid invoices; refunds are a future flow
- **No multi-warehouse** — one stock location per company
- **No expenses/accounting** — only invoice totals, no GL
- **No configurable permission engine** — roles are hardcoded

### Hardening (post-demo)
- **`CancellationToken` propagation** — currently services ignore `HttpContext.RequestAborted`; add in a hardening pass
- **Refresh token race fix** — two concurrent refreshes with the same token both succeed in the current implementation; use a conditional UPDATE or add `RowVersion` on `RefreshToken`
- **Invoice number race** — retry handles it; a sequence table per tenant would eliminate it entirely
- **AutoMapper removal** — manual mapping is fine at this scale; drop the dependency if unused

### Analytics (nice-to-have)
- **Most-sales product** — `GROUP BY ProductId ORDER BY SUM(Quantity) DESC`
- **Most-valuable customer** — `GROUP BY CustomerId ORDER BY SUM(Total) DESC`
- **Sales-over-time chart** — needs a date-bucketing helper

These were deliberately not built for the MVP — spec says *"Focus on implementing the requested functionality correctly."*

---

## License

Private project — DOM CORE agency. All rights reserved.
