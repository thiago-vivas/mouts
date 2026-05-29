# DeveloperStore — Sales API

A complete CRUD REST API for **sales records**, built on the provided
`Ambev.DeveloperEvaluation` .NET 8 template following **DDD** with the
**External Identities** pattern, quantity-based **discount rules**, and
**domain events** (logged, persisted to MongoDB, and published to RabbitMQ via
Rebus). Includes an **Angular** frontend.

> Supporting challenge docs live in [`.doc/`](.doc/overview.md). The backend is in
> [`src/backend`](src/backend) and the Angular frontend in
> [`src/frontend`](src/frontend).

---

## Contents
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Configure](#configure)
- [Run](#run)
- [Test](#test)
- [API](#api)
- [Authentication](#authentication)
- [Business rules](#business-rules)
- [Domain events](#domain-events)
- [Seed data](#seed-data)
- [Project structure](#project-structure)
- [Design decisions & deviations](#design-decisions--deviations)
- [Open questions for the product team](#open-questions-for-the-product-team)

---

## Architecture

Layered / Clean architecture with CQRS via MediatR:

```
WebApi  ──>  Application (CQRS handlers) ──> Domain (entities, rules, events)
  │                                              ▲
  └──> IoC (DI) ──> ORM (EF Core + Mongo + Rebus)┘
```

- **Domain** — `Sale` aggregate root, `SaleItem`, `ExternalReference` value object,
  `DiscountPolicy`, domain events, repository/event-store/event-bus interfaces. No infra deps.
- **Application** — one feature slice per use case (`Command`/`Query` + `Handler`
  + `Validator`), AutoMapper profiles, and notification handlers that log, store and publish events.
- **ORM** — EF Core `DefaultContext`, owned-type mappings, `SaleRepository`,
  `MongoEventStore`, `RebusEventBus`, migrations and the data seeder.
- **WebApi** — `SalesController` (JWT-secured), request DTOs, centralized error handling, CORS.
- **frontend** — Angular 18 SPA consuming the API.

The **External Identities** pattern is modeled with the `ExternalReference`
value object (`{ Id, Name }`): Customer, Branch and Product are referenced by id
plus a denormalized name — no foreign keys to other aggregates.

## Tech stack

| Concern | Technology |
|---|---|
| Runtime | .NET 8 / C# |
| Web | ASP.NET Core + Swagger |
| Mediator / CQRS | MediatR |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Write database | PostgreSQL (EF Core / Npgsql) |
| Event store | MongoDB |
| Messaging | RabbitMQ via Rebus |
| Auth | JWT bearer |
| Logging | Serilog |
| Frontend | Angular 18 |
| Tests | xUnit, NSubstitute, Bogus (Faker), FluentAssertions; Jasmine/Karma (frontend) |
| Containers | Docker Compose |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.11+ or 22 LTS](https://nodejs.org/) (Angular 18 requires `^20.11.1 || >=22`; CI uses Node 22)
- [Docker](https://www.docker.com/) (PostgreSQL + MongoDB + RabbitMQ), or local instances
- `dotnet-ef` tool (only to manage migrations): `dotnet tool install --global dotnet-ef`

Backend commands below run from `src/backend`; frontend commands from `src/frontend`.

## Configure

Connection strings live in
[`src/backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`](src/backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://developer:ev%40luAt10n@localhost:27017/?authSource=admin",
    "Database": "developer_evaluation"
  },
  "RabbitMq": { "ConnectionString": "" }
}
```

Credentials match `docker-compose.yml`. **MongoDB** and **RabbitMQ** are optional:
if Mongo is unreachable the event store degrades to a no-op, and Rebus only
publishes when `RabbitMq:ConnectionString` is set (docker-compose sets it). Events
are always logged.

## Run

The whole stack runs in Docker, so you only need **Docker** and **Node.js** installed.

**1. Start the backend** — API + PostgreSQL + MongoDB + RabbitMQ:

```bash
cd src/backend
docker compose up --build
```

The API builds, applies its EF migrations, seeds a default admin + sample sales, and
serves on **http://localhost:8080** — open **http://localhost:8080/swagger**.

**2. Start the frontend:**

```bash
cd src/frontend
npm install
npm start                   # http://localhost:4200
```

**3. Log in** at http://localhost:4200:

```
email:    admin@developerstore.com
password: Admin@123
```

### Backend on the host (optional, for development)

To iterate on the API without rebuilding the image, run the databases in Docker and
the API with `dotnet run` (on port 8080, to match the frontend):

```bash
cd src/backend
docker compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql ambev.developerevaluation.queue
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi --urls "http://localhost:8080"
```

## Test

```bash
cd src/backend
dotnet test Ambev.DeveloperEvaluation.sln                                    # all suites (173 tests)
dotnet test ../../tests/backend/Ambev.DeveloperEvaluation.Unit               # unit only
dotnet test ../../tests/backend/Ambev.DeveloperEvaluation.Integration        # repository / EF / event store
dotnet test ../../tests/backend/Ambev.DeveloperEvaluation.Functional         # end-to-end HTTP
```

Three backend layers, all runnable without external infrastructure:

- **Unit** (xUnit + NSubstitute + Bogus + FluentAssertions) — discount tiers and
  boundaries (4/5/9/10/20/21), max-20 rejection, total recalculation, item & sale
  cancellation, every CQRS handler (mocked repo/mediator/bus), validators, value
  objects, events and the sale-number generator.
- **Integration** — `SaleRepository` and the seeder through EF Core (CRUD, owned
  External-Identity mappings, cascade delete, pagination, wildcard/exact filtering,
  date-range, ordering) and the Mongo event store's fallback path.
- **Functional** — the real API booted in-memory via `WebApplicationFactory` with a
  real JWT, driven over HTTP through the full pipeline (auth, validation, MediatR,
  error middleware, envelopes) covering the full lifecycle plus error paths (401/400/404).

Integration and functional suites use the EF Core **in-memory provider** so they're
hermetic (no Docker); the same repository runs on PostgreSQL in production.

**Frontend:** `cd src/frontend && npm test` (Jasmine/Karma — service/component specs
using `HttpTestingController`).

### Coverage

```bash
cd src/backend
dotnet test Ambev.DeveloperEvaluation.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:coverage/report \
  -reporttypes:TextSummary -filefilters:"-*Migrations*;-*Designer.cs;-*ModelSnapshot.cs"
cat coverage/report/Summary.txt
```

The Sales feature sits at **~98% line coverage**. CI runs both the backend suites
(with coverage artifact) and the frontend build + tests.

## API

Base route `api/sales` (all endpoints require a JWT — see [Authentication](#authentication)).
Responses use the template envelopes (`ApiResponseWithData<T>` / `PaginatedResponse<T>`).

| Method | Route | Purpose | Event |
|---|---|---|---|
| `POST` | `/api/sales` | Create a sale | `SaleCreated` |
| `GET` | `/api/sales` | List (paginated/filtered/sorted) | — |
| `GET` | `/api/sales/{id}` | Get one | — |
| `PUT` | `/api/sales/{id}` | Update a sale | `SaleModified` |
| `DELETE` | `/api/sales/{id}` | Delete a sale | — |
| `PATCH` | `/api/sales/{id}/cancel` | Cancel whole sale | `SaleCancelled` |
| `PATCH` | `/api/sales/{id}/items/{itemId}/cancel` | Cancel one item | `ItemCancelled` |

**List query parameters** (per [`.doc/general-api.md`](.doc/general-api.md)):
`_page` (default 1), `_size` (default 10), `_order` (e.g. `"saleDate desc, saleNumber asc"`),
`customerName`/`branchName` (exact, or partial with a leading/trailing `*`),
`isCancelled`, `_minTotalAmount`/`_maxTotalAmount`, `_minSaleDate`/`_maxSaleDate`.
The list envelope exposes `totalItems` (and `totalCount`), `currentPage`, `totalPages`.

**Errors** follow the documented `{ type, error, detail }` shape, with mapped
status codes: 400 validation, 401 unauthenticated, 404 not found, 400 business-rule.

## Authentication

Sales endpoints require a JWT bearer token. Obtain one from `POST /api/auth`:

```bash
curl -X POST http://localhost:8080/api/auth \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@developerstore.com", "password": "Admin@123" }'
# → { "data": { "token": "..." } }   then send: Authorization: Bearer <token>
```

A default user is seeded on startup: **admin@developerstore.com / Admin@123**.

## Business rules

Quantity-based discounts, enforced in the domain (`DiscountPolicy`):

| Identical units | Discount |
|---|---|
| 1–4 | 0% |
| 5–9 | 10% |
| 10–20 | 20% |
| > 20 | **rejected** (max 20 per product) |

The discount applies *above* 4 items, so a quantity of exactly **4 gets no
discount** (the brief's prose says "above 4"; we follow it over the summary's "4+").
Discounts are **derived from quantity** — never client-supplied. Per-item total =
`unitPrice * quantity - discount`; sale total = sum of active (non-cancelled) item totals.

## Domain events

`SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled` are raised in-process
via MediatR notifications. Each is **logged** (Serilog), **appended to MongoDB**
(`sale_events` — immutable audit log), and **published to RabbitMQ via Rebus**. When
no broker is configured the bus is a no-op, so the API runs without RabbitMQ.

## Seed data

On first run the API seeds a **default admin user** and four representative sales —
covering the 0% / 10% / 20% tiers, the above-4 boundary, a 20-unit bulk order, and a
cancelled sale — with deterministic IDs. See
[`DbSeeder`](src/backend/src/Ambev.DeveloperEvaluation.ORM/Seeding/DbSeeder.cs).

## Project structure

```
root
├── src/
│   ├── backend/                                  # .NET 8 solution
│   │   ├── Ambev.DeveloperEvaluation.sln
│   │   ├── src/
│   │   │   ├── Ambev.DeveloperEvaluation.Domain        # entities, VOs, rules, events, interfaces
│   │   │   ├── Ambev.DeveloperEvaluation.Application    # CQRS slices, profiles, event handlers
│   │   │   ├── Ambev.DeveloperEvaluation.Common         # validation, logging, security
│   │   │   ├── Ambev.DeveloperEvaluation.ORM            # EF Core, Mongo store, Rebus bus, seeding, migrations
│   │   │   ├── Ambev.DeveloperEvaluation.IoC            # DI module initializers
│   │   │   └── Ambev.DeveloperEvaluation.WebApi         # controllers, DTOs, error handling, Program.cs
│   │   └── docker-compose.yml · Dockerfile
│   └── frontend/                                 # Angular 18 SPA (Jasmine specs co-located)
├── tests/
│   └── backend/                                  # Unit / Integration / Functional (.NET)
├── .doc/
└── README.md
```

## Design decisions & deviations

- **Discount boundary** — "above 4", so quantity 4 = 0% (prose over the contradictory "4+" summary).
- **Identifiers** — `Guid` (client-generated in constructors), not the docs' integer ids — consistent with the template's `BaseEntity`/migrations.
- **Response envelope** — kept the template's `{ success, message, data }` wrapper; the list exposes `totalItems` to match the docs.
- **AutoMapper / NU1903** — the advisory's fix (AutoMapper 15.1.1) targets .NET 10 and is incompatible with this .NET 8 solution; the DoS is not exploitable with our shallow, acyclic mapping DTOs, so the warning is suppressed with a documented note in `Directory.Build.props`.
- **Scope** — the use case targets the **Sales** API; the Products/Carts specs in `.doc/` are reference context and are not implemented (Users/Auth ship with the template).

## Open questions for the product team

Clarifications we'd raise; each lists the interim assumption shipped.

1. **Freshness of denormalized data (External Identities)** — when a customer/branch/
   product is renamed upstream, do past sales keep the value at sale time or sync?
   *Assumption:* frozen at sale time (auditable record).
2. **Cancellation downstream effects** — should cancelling return stock / issue refunds?
   *Assumption:* soft cancel + events only; stock/refund are downstream consumers, out of scope here.
3. **20-item limit scope** — per line, per sale, or per customer?
   *Assumption:* per identical product line.
4. **Authorization granularity** — which roles may create/update/cancel; sale tied to the user's branch?
   *Assumption:* any authenticated user (JWT required); no per-role/branch rules yet.
