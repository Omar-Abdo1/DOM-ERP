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
- [Testing](#DomCoreERP.Tests)
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

## DomCoreERP.Tests

Integration and regression test suite for **DOM-ERP** (`DomCoreERP.API` / `DomCoreERP.Service` / `DomCoreERP.Repository`), built with **xUnit**, **FluentAssertions**, and `WebApplicationFactory<Program>` against a real **SQL Server** instance.

**46 tests** covering multi-tenancy isolation, SuperAdmin workspace scoping, authentication, invoicing, payments, and inventory guardrails.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Running the Tests](#running-the-tests)
- [Test Suite Overview](#test-suite-overview)
- [What Is Tested & How DOM-ERP Handles It](#what-is-tested--how-dom-erp-handles-it)
- [Detailed Test Reference](#detailed-test-reference)

---

## Prerequisites

- .NET SDK (matching the solution's target framework)
- A SQL Server instance reachable at `localhost,1433`
- The `sa` password exposed through the `DOMCORETEST_SA_PASSWORD` environment variable

## Running the Tests

```bash
export DOMCORETEST_SA_PASSWORD="YourSaPasswordHere"
dotnet test tests/DomCoreERP.Tests/DomCoreERP.Tests.csproj
```

> **Note:** All test classes use `[Collection("TestApi")]` and share a single `TestWebApplicationFactory` database per test run. The temporary test database is dropped automatically on disposal. Because tests share a live database, tests that create records use unique `Guid` suffixes and assert membership/invariants rather than brittle hardcoded counts.

---

## Test Suite Overview

| Test Class | Focus Area | Tests |
| --- | --- | :---: |
| `ProductsIsolationTests` | Product tenant isolation, role checks & spoofing guards | 6 |
| `CustomersIsolationTests` | Customer tenant isolation & role authorization | 3 |
| `InvoicesAndPaymentsIsolationTests` | Cross-tenant invoice & payment isolation | 5 |
| `UsersIsolationTests` | User directory isolation, creation rules & role checks | 4 |
| `SuperAdminWorkspaceTests` | SuperAdmin global vs. workspace scoping & audit logs | 5 |
| `WorkspaceActorAndAuthTests` | Platform actor joins, `/auth/me`, logout audit & email uniqueness | 4 |
| `MvpGuardrailTests` | High-risk MVP regression & state-machine guardrails | 9 |
| `InvoiceTests` | Draft totals, stock deduction, cancellation & stock validation | 5 |
| `PaymentTests` | Partial/full payments, overpayment & lifecycle payment guards | 5 |
| **Total** | | **46** |

---

## What Is Tested & How DOM-ERP Handles It

### 1. Multi-Tenancy & Global Query Filters

**Tenant data isolation** (`ProductsIsolationTests`, `CustomersIsolationTests`, `InvoicesAndPaymentsIsolationTests`, `UsersIsolationTests`)

- **Tested:** Company admins and employees only see resources from their own company. Requesting another company's product, customer, invoice, or user by ID returns `404 NotFound`. Employees are blocked (`403 Forbidden`) from restricted management/creation endpoints.
- **Implementation:** `DomCoreContext.OnModelCreating` configures EF Core global query filters (`HasQueryFilter`) on tenant-scoped entities (`Product`, `Customer`, `Invoice`, `Payment`, `InventoryMovement`, `User`, `ActivityLog`) based on `ICurrentUserService`.

**Spoofing & write-target protection** (`ProductsIsolationTests`, `UsersIsolationTests`)

- **Tested:** Non-SuperAdmin users cannot escalate into another tenant via a forged `X-Company-Id` header or by passing another `CompanyId` in a request DTO.
- **Implementation:** `CurrentUserService.ResolvedCompanyId` only reads `X-Company-Id` when `IsSuperAdmin` is `true` and the user is authenticated; otherwise it falls back to the user's own `CompanyId` claim. `ResolveCompanyIdForWrite(...)` enforces tenant ownership on writes.

**SuperAdmin workspace scoping** (`SuperAdminWorkspaceTests`, `MvpGuardrailTests`)

- **Tested:** A SuperAdmin without `X-Company-Id` sees cross-tenant records and gets `400 BadRequest` on `/api/dashboard/company`, which requires an explicit company. With `X-Company-Id` set, reads and writes are scoped to that company and other companies' entities return `404 NotFound`.
- **Implementation:** When `IsSuperAdmin && ResolvedCompanyId == null`, filters allow system-wide access; when `ResolvedCompanyId` is set, filters scope to that company. `DashboardService` throws a `400` `ApiException` if no company context is resolved.

### 2. Platform Actor Lookups & Cross-Tenant Edge Cases

**Preserving real actor identity inside a workspace**

- **Tested:** When a SuperAdmin (`CompanyId = null`) acts inside a company workspace — creating products, issuing invoices, recording payments, generating inventory movements — `ActivityLog`, `Invoice`, `Payment`, and `InventoryMovement` preserve the `SUPERADMIN` role, the workspace tenant, and the actor's name without dropping rows during `User` joins.
- **Implementation:** The `User` global query filter includes `|| u.CompanyId == null`, so foreign-key joins (`ActorUser`, `CreatedByUser`, `ReceivedByUser`) still resolve platform accounts. `UserService.GetPagedAsync`/`GetByIdAsync` explicitly restrict tenant user listings so platform users don't leak into company lists. `ActivityLogService.GetPagedAsync` uses `.IgnoreQueryFilters()` and applies the `ActivityLog` tenant filter manually.

**Authenticated profile access with workspace header**

- **Tested:** A SuperAdmin with `X-Company-Id` set can still call `GET /api/auth/me`.
- **Implementation:** `AuthService.GetCurrentUserAsync` resolves the user without being blocked by a workspace company filter.

**Global email uniqueness & per-company SKU uniqueness**

- **Tested:** Creating a user with an email used in another tenant returns `409 Conflict`. Duplicate SKUs are rejected (`409`) within a company but allowed across companies (`201`).
- **Implementation:** `UserService.CreateAsync` checks normalized email across all tenants via `.IgnoreQueryFilters()`; `ProductService.CreateAsync` checks SKU uniqueness scoped to `companyId`.

**Logout audit logging**

- **Tested:** Logging out records a `Logout` `ActivityLog` entry under the user's company.
- **Implementation:** `AuthService.LogoutAsync` loads the refresh token and its user (`.IgnoreQueryFilters()`) and logs `companyId: token.User.CompanyId`.

### 3. Business Logic, Stock & Financial Invariants

**Invoice totals & draft vs. issued behavior**

- **Tested:** Draft creation calculates `Subtotal`, `Total` (`Subtotal - Discount + Tax`), `PaidAmount = 0`, `RemainingAmount = Total`, status `Draft`, and does not deduct stock. Duplicate product lines exceeding combined stock are rejected (`400`).
- **Implementation:** `InvoiceService.CreateDraftAsync` validates customer/product state and aggregates requested quantities per `ProductId` before persisting.

**Stock deduction, cancellation & immutability**

- **Tested:** Issuing deducts stock; cancelling restores it; deleting an issued invoice is rejected (`409`); negative stock adjustments below zero are rejected (`400`).
- **Implementation:** `InvoiceService.IssueAsync` and `CancelAsync` run inside `RetryService.ExecuteOnConflictAsync` to handle optimistic concurrency (`RowVersion`) and record `InventoryMovement` entries atomically with stock updates.

**Payments: partial, full & overpayment guards**

- **Tested:** Partial payments set `PartiallyPaid`; full payment sets `Paid` with `RemainingAmount = 0`; paying a `Draft` invoice, overpaying, or paying an already-`Paid` invoice is rejected (`400`); cancelling an invoice with payments is rejected (`409`).
- **Implementation:** `PaymentService.RecordAsync` validates invoice status and remaining balance inside the retry/concurrency loop and updates `Payment` and `Invoice` in a single `SaveChanges` call.

**Refresh token rotation & company lifecycle**

- **Tested:** Refreshing rotates the token and replaying the old one returns `401`. Deactivating a company blocks its users from logging in (`401`); reactivating restores login (`200`).
- **Implementation:** `AuthService.RefreshAsync` detects reuse of `"Rotated on refresh"` tokens and revokes live tokens. `CompanyService.DeactivateAsync`/`ActivateAsync` toggle `Company.Status` and revoke active refresh tokens on deactivation.

---

## Detailed Test Reference

<details>
<summary><strong>1. ProductsIsolationTests (6)</strong> — <code>MultiTenancy/ProductsIsolationTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 1 | `AdminA_ListsProducts_SeesOnlyCompanyA` | Company A admin sees A's seeded products (`iPhone 15`, `AirPods Pro`, `iPad Air`), not B's (`Samsung S24`, `Galaxy Buds`) | Global query filter on `Product` using `ResolvedCompanyId ?? CompanyId` |
| 2 | `AdminB_ListsProducts_SeesOnlyCompanyB` | Company B admin sees only B's products | Same filter scoped to Company B |
| 3 | `AdminA_CannotReadCompanyBProduct_ReturnsNotFound` | Fetching B's product by ID as A returns `404` | `ProductService.GetByIdAsync` throws `NotFoundException`, avoiding existence leaks |
| 4 | `AdminA_CannotCreateProductInCompanyB_BySettingDtoCompanyId` | Payload `CompanyId = CompanyB` still creates under `CompanyA` (`201`) | `ResolveCompanyIdForWrite(...)` ignores non-SuperAdmin `dto.CompanyId` |
| 5 | `AdminA_CannotSpoofXCompanyIdHeader_ToSeeCompanyB` | `X-Company-Id: CompanyB` with A's token is ignored | `ResolvedCompanyId` reads the header only for SuperAdmin |
| 6 | `Employee_CannotCreateProduct_ReturnsForbidden` | Employee `POST /api/products` returns `403` | Role/policy authorization on creation |

</details>

<details>
<summary><strong>2. CustomersIsolationTests (3)</strong> — <code>MultiTenancy/CustomersIsolationTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 7 | `AdminA_ListsCustomers_SeesOnlyOwnCompany` | A sees `Mohamed Ahmed`, not B's `Khaled Ibrahim` | Global query filter on `Customer` |
| 8 | `AdminA_CannotReadCompanyBCustomer_ReturnsNotFound` | Cross-tenant customer fetch returns `404` | `CustomerService.GetByIdAsync` respects the filter |
| 9 | `Employee_CannotCreateCustomer_ReturnsForbidden` | Employee `POST /api/customers` returns `403` | Authorization restricts creation to admin roles |

</details>

<details>
<summary><strong>3. InvoicesAndPaymentsIsolationTests (5)</strong> — <code>MultiTenancy/InvoicesAndPaymentsIsolationTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 10 | `AdminB_ListsInvoices_DoesNotSeeCompanyAInvoices` | B's invoice list excludes A's issued invoice | Global query filter on `Invoice` |
| 11 | `AdminB_CannotReadCompanyAInvoiceById_ReturnsNotFound` | Cross-tenant invoice fetch returns `404` | `InvoiceService.GetByIdAsync` applies tenant filter |
| 12 | `AdminB_CannotPayCompanyAInvoice_ReturnsNotFound` | Paying A's invoice as B returns `404` | `PaymentService.RecordAsync` looks up invoice via filtered repository |
| 13 | `AdminA_CannotUseCompanyBCustomerInInvoice` | Invoice with B's customer ID returns `404` | `CreateDraftAsync` validates `c.Id == dto.CustomerId && c.CompanyId == companyId` |
| 14 | `AdminA_CannotUseCompanyBProductInInvoice` | Invoice with B's product ID returns `404` | `CreateDraftAsync` loads products scoped to `companyId` |

</details>

<details>
<summary><strong>4. UsersIsolationTests (4)</strong> — <code>MultiTenancy/UsersIsolationTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 15 | `AdminA_ListsUsers_SeesOnlyCompanyA` | A sees only `@companya.com` users; no platform (`CompanyId = null`) or other-tenant users | `UserService.GetPagedAsync` filters `u.CompanyId == targetCompanyId` for non-global admins |
| 16 | `AdminA_CannotReadAdminB_ReturnsNotFound` | Fetching B's admin by ID returns `404` | `UserService.GetByIdAsync` enforces tenant boundaries |
| 17 | `AdminA_CannotCreateEmployeeInCompanyB` | A cannot create an employee in B (`403` or forced into A) | `CreateAsync` restricts non-SuperAdmins to own company and `EMPLOYEE` role |
| 18 | `Employee_CannotListUsers_ReturnsForbidden` | Employee `GET /api/users` returns `403` | User management requires admin/SuperAdmin |

</details>

<details>
<summary><strong>5. SuperAdminWorkspaceTests (5)</strong> — <code>MultiTenancy/SuperAdminWorkspaceTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 19 | `SuperAdmin_NoHeader_SeesAllProducts` | Without `X-Company-Id`, products from A and B are visible | `IsSuperAdmin && ResolvedCompanyId == null` allows system-wide visibility |
| 20 | `SuperAdmin_HeaderCompanyA_SeesOnlyCompanyA` | Header `CompanyA` shows only A's products | `ResolvedCompanyId` scopes the `Product` filter |
| 21 | `SuperAdmin_HeaderCompanyB_SeesOnlyCompanyB` | Header `CompanyB` shows only B's products | Same, scoped to B |
| 22 | `SuperAdmin_InsideWorkspace_PreservesRealActorInActivityLog` | Product created in workspace A logs `CompanyId = CompanyA`, `ActorRole = "SUPERADMIN"`, `ActorName = "Omar Abdo"` | `ActivityLogService.GetPagedAsync` uses `.IgnoreQueryFilters()` with a manual company filter |
| 23 | `CompanyAdmin_CannotSeeSystemWideLogs` | Company A admin can't see `CompanyId = null` logs (e.g. company creation) | Non-SuperAdmin logs are filtered to own `CompanyId`, excluding `null` |

</details>

<details>
<summary><strong>6. WorkspaceActorAndAuthTests (4)</strong> — <code>MultiTenancy/WorkspaceActorAndAuthTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 24 | `SuperAdmin_InsideWorkspace_PreservesActorAcrossPaymentAndCompanyDashboard` | In workspace A, SuperAdmin can issue an invoice, record a payment (`ReceivedByName == "Omar Abdo"`), fetch it, and load `/api/dashboard/company` | `User` filter includes `\|\| u.CompanyId == null` so joins still resolve platform accounts |
| 25 | `SuperAdmin_WithWorkspaceHeader_CanCallAuthMe` | `GET /api/auth/me` works with workspace header (`CompanyId = null`) | `AuthService.GetCurrentUserAsync` isn't excluded by workspace filter |
| 26 | `AdminA_Logout_LogsActivityUnderCompanyA` | Logout writes a `Logout` activity log under `CompanyA` | `LogoutAsync` loads token + user (`.IgnoreQueryFilters()`), logs `token.User.CompanyId` |
| 27 | `AdminA_CannotCreateUserWithCompanyBEmail_ReturnsConflict` | Duplicate email across tenants returns `409` | `CreateAsync` checks normalized email with `.IgnoreQueryFilters()` |

</details>

<details>
<summary><strong>7. MvpGuardrailTests (9)</strong> — <code>BusinessLogic/MvpGuardrailTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 28 | `SuperAdmin_ScopedToCompanyA_CannotReadCompanyBProductOrInvoiceById` | SuperAdmin scoped to A gets `404` for B's product | `ResolvedCompanyId` overrides system-wide access |
| 29 | `SuperAdmin_WithoutWorkspaceHeader_GetsBadRequestOnCompanyDashboard` | `/api/dashboard/company` without header returns `400` | `DashboardService.ResolveTargetCompanyId()` throws `ApiException` |
| 30 | `SameSku_AllowedAcrossDifferentCompanies_RejectedWithinSameCompany` | Same SKU: `409` within a company, `201` across companies | `ProductService.CreateAsync` checks SKU scoped to `companyId` |
| 31 | `SuperAdmin_InsideWorkspace_CanRecordPaymentAndViewMovementsWithoutMissingActor` | `"Omar Abdo"` preserved on `PaymentDto.ReceivedByName` and `InventoryMovementDto.CreatedByName` | `User` filter allows `CompanyId == null` in FK joins |
| 32 | `DuplicateProductLinesExceedingCombinedStock_RejectedOnDraftCreation` | Two lines for one product exceeding stock returns `400` | `CreateDraftAsync` groups by `ProductId` and sums quantities |
| 33 | `IssuedInvoice_CannotBeDeletedOrEditedAsDraft_AndCannotPayTwiceWhenPaid` | Delete issued → `409`; full payment → `Paid`; further payment → `400` | `DeleteDraftAsync` rejects non-drafts; `RecordAsync` rejects `Paid` |
| 34 | `NegativeStockAdjustmentBelowZero_IsRejected` | Adjustment below zero returns `400`, stock unchanged | `InventoryService.RecordAdjustmentAsync` validates `newStock < 0` |
| 35 | `RefreshTokenRotation_RejectsReusedToken` | Replaying a rotated refresh token returns `401` | `AuthService.RefreshAsync` detects reuse, revokes live tokens |
| 36 | `DeactivatingCompany_BlocksUserLogin_AndReactivatingRestoresIt` | Deactivated company → login `401`; reactivated → `200` | `DeactivateAsync` sets `Status = Inactive` and revokes tokens; `LoginAsync` requires `Active` |

</details>

<details>
<summary><strong>8. InvoiceTests (5)</strong> — <code>BusinessLogic/InvoiceTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 37 | `CreateDraft_CalculatesTotalsCorrectly` | `Subtotal`, `Total`, `PaidAmount = 0`, `RemainingAmount = Total`, `Status = Draft` | Line and invoice totals computed with away-from-zero rounding |
| 38 | `CreateDraft_DoesNotDeductStock` | Draft leaves `CurrentStock` unchanged | Deduction deferred to `IssueAsync` |
| 39 | `IssueInvoice_DeductsStock` | Issuing decreases `CurrentStock` by invoiced quantity | `IssueAsync` deducts stock and records `Sale` movements in `RetryService.ExecuteOnConflictAsync` |
| 40 | `CreateDraft_RejectsQuantityOverStock` | Quantity over stock returns `400` | `CreateDraftAsync` checks `CurrentStock < totalQty` |
| 41 | `CancelIssuedInvoice_RestoresStock` | Cancelling restores prior stock | `CancelAsync` increments stock and records `CancellationRestore` movements atomically |

</details>

<details>
<summary><strong>9. PaymentTests (5)</strong> — <code>BusinessLogic/PaymentTests.cs</code></summary>

| # | Test | What it verifies | Mechanism |
| :-: | --- | --- | --- |
| 42 | `PartialPayment_UpdatesBalancesAndStatus` | Updates `PaidAmount`/`RemainingAmount`, status `PartiallyPaid` | Invoice and `Payment` updated in one transaction |
| 43 | `FullPayment_MarksInvoicePaid` | `PaidAmount = Total`, `RemainingAmount = 0`, `Status = Paid` | Transition to `Paid` when `RemainingAmount == 0` |
| 44 | `Overpayment_RejectedWith400` | Payment above `RemainingAmount` returns `400` | Checks `dto.Amount > invoice.RemainingAmount` |
| 45 | `CannotPayDraftInvoice_Returns400` | Paying a `Draft` invoice returns `400` | Blocks `InvoiceStatus.Draft`, instructs to issue first |
| 46 | `CannotCancelInvoiceWithPayments` | Cancelling an invoice with payments returns `409` | `CancelAsync` checks `PaidAmount > 0`, throws `ConflictException` |

</details>

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
