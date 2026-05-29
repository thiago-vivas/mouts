# DeveloperStore — Sales API

A complete CRUD REST API for **sales records**, built on the provided
`Ambev.DeveloperEvaluation` .NET 8 template following **DDD** with the
**External Identities** pattern, quantity-based **discount rules**, and
**domain events** (logged + persisted to MongoDB).

> Supporting challenge docs live in [`.doc/`](.doc/overview.md). **All application
> code lives under [`template/backend`](template/backend).**

---

## Contents
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Configure](#configure)
- [Run](#run)
- [Test](#test)
- [API](#api)
- [Business rules](#business-rules)
- [Domain events](#domain-events)
- [Seed data](#seed-data)
- [Project structure](#project-structure)

---

## Architecture

Layered / Clean architecture with CQRS via MediatR:

```
WebApi  ──>  Application (CQRS handlers) ──> Domain (entities, rules, events)
  │                                              ▲
  └──> IoC (DI) ──> ORM (EF Core + Mongo) ───────┘
```

- **Domain** — `Sale` aggregate root, `SaleItem`, `ExternalReference` value object,
  `DiscountPolicy`, domain events, repository/event-store interfaces. No infra dependencies.
- **Application** — one feature slice per use case (`Command`/`Query` + `Handler`
  + `Validator`), AutoMapper profiles, and notification handlers that publish events.
- **ORM** — EF Core `DefaultContext`, owned-type mappings, `SaleRepository`,
  `MongoEventStore`, migrations and the data seeder.
- **WebApi** — `SalesController`, request DTOs, and centralized error handling.

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
| Logging | Serilog |
| Tests | xUnit, NSubstitute, Bogus (Faker), FluentAssertions |
| Containers | Docker Compose |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (for PostgreSQL + MongoDB), or local instances
- `dotnet-ef` tool (only to manage migrations): `dotnet tool install --global dotnet-ef`

All commands below are run from `template/backend`.

## Configure

Connection strings live in
[`src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`](template/backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=developer_evaluation;Username=developer;Password=ev@luAt10n"
  },
  "MongoDb": {
    "ConnectionString": "mongodb://developer:ev%40luAt10n@localhost:27017/?authSource=admin",
    "Database": "developer_evaluation"
  }
}
```

These match the credentials in `docker-compose.yml`. MongoDB is optional — if it
is unreachable the API still runs and events are logged (the event store degrades
to a no-op).

## Run

### Docker databases + local API

```bash
cd template/backend
docker-compose up -d ambev.developerevaluation.database ambev.developerevaluation.nosql
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

On startup the API **applies EF migrations** and **seeds sample sales**
automatically. Swagger UI is at `https://localhost:<port>/swagger` (the port is
printed to the console; see `src/Ambev.DeveloperEvaluation.WebApi/Properties/launchSettings.json`).

### Everything in containers

```bash
cd template/backend
docker-compose up -d
```

## Test

```bash
cd template/backend
dotnet test                                                 # all suites (153 tests)
dotnet test tests/Ambev.DeveloperEvaluation.Unit            # unit only
dotnet test tests/Ambev.DeveloperEvaluation.Integration     # repository / EF / event store
dotnet test tests/Ambev.DeveloperEvaluation.Functional      # end-to-end HTTP
```

Three layers of tests, all runnable without external infrastructure:

- **Unit** (xUnit + NSubstitute + Bogus + FluentAssertions) — discount tiers and
  boundaries (3/4/9/10/20/21), max-20 rejection, total recalculation, item & sale
  cancellation, every CQRS handler (mocked repo/mediator), validators, value
  objects, events and the sale-number generator.
- **Integration** — `SaleRepository` and the seeder exercised through EF Core
  (CRUD, owned External-Identity mappings, cascade delete, pagination, filtering,
  ordering) and the Mongo event store's graceful-degradation path.
- **Functional** — the real API booted in-memory via `WebApplicationFactory`,
  driven over HTTP through the full pipeline (routing, validation, MediatR,
  error middleware, response envelopes) covering the create→get→list→update→
  cancel lifecycle plus error paths (400/404/domain-rule).

Integration and functional suites run on the EF Core **in-memory provider** so
they're hermetic in CI and locally (no Docker needed); the identical repository
code runs on PostgreSQL in production.

### Coverage

```bash
cd template/backend
dotnet test Ambev.DeveloperEvaluation.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:coverage/report \
  -reporttypes:TextSummary -filefilters:"-*Migrations*;-*Designer.cs;-*ModelSnapshot.cs"
cat coverage/report/Summary.txt
```

The Sales feature sits at **~98% line coverage**. The small remainder is the
MongoDB *insert* path (requires a live MongoDB) and defensive guard branches.
CI runs all three suites and publishes the coverage report as an artifact.

## API

Base route `api/sales`. Responses use the template envelopes
(`ApiResponseWithData<T>` / `PaginatedResponse<T>`).

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
`customerName`/`branchName` (partial match), `isCancelled`, `_minTotalAmount`/`_maxTotalAmount`.

**Errors** follow the documented `{ type, error, detail }` shape, with mapped
status codes for validation (400), not found (404) and business-rule
violations (400).

### Example — create a sale

```bash
curl -k -X POST https://localhost:5001/api/sales \
  -H "Content-Type: application/json" \
  -d '{
    "saleDate": "2026-05-28T10:00:00Z",
    "customer": { "id": "11111111-1111-1111-1111-111111111111", "name": "John Doe" },
    "branch":   { "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "name": "Downtown Store" },
    "items": [
      { "productId": "cccccccc-cccc-cccc-cccc-cccccccccccc", "productName": "Premium Lager 600ml", "quantity": 12, "unitPrice": 8.50 }
    ]
  }'
```

## Business rules

Quantity-based discounts, enforced in the domain (`DiscountPolicy`):

| Identical units | Discount |
|---|---|
| 1–3 | 0% |
| 4–9 | 10% |
| 10–20 | 20% |
| > 20 | **rejected** (max 20 per product) |

Discounts are **derived from quantity** — a client cannot send an arbitrary
discount. Per-item total = `unitPrice * quantity - discount`; sale total = sum of
active (non-cancelled) item totals.

## Domain events

Published in-process via MediatR notifications. Each event is **logged** (Serilog)
and **appended to MongoDB** (`sale_events` collection) as an immutable audit
record. No real message broker is used — this is the allowed alternative to
publishing to a bus. Events: `SaleCreated`, `SaleModified`, `SaleCancelled`,
`ItemCancelled`.

## Seed data

On first run (empty `Sales` table) the API seeds four representative sales —
covering the 0% / 10% / 20% discount tiers, the qty-4 boundary, a 20-unit bulk
order, and a cancelled sale — with deterministic IDs. See
[`DbSeeder`](template/backend/src/Ambev.DeveloperEvaluation.ORM/Seeding/DbSeeder.cs).

## Project structure

```
template/backend/
├── src/
│   ├── Ambev.DeveloperEvaluation.Domain         # entities, VOs, rules, events, interfaces
│   ├── Ambev.DeveloperEvaluation.Application     # CQRS slices, profiles, event handlers
│   ├── Ambev.DeveloperEvaluation.Common          # validation, logging, security
│   ├── Ambev.DeveloperEvaluation.ORM             # EF Core, Mongo event store, seeding, migrations
│   ├── Ambev.DeveloperEvaluation.IoC             # DI module initializers
│   └── Ambev.DeveloperEvaluation.WebApi          # controllers, DTOs, error handling, Program.cs
└── tests/
    ├── Ambev.DeveloperEvaluation.Unit            # domain rules, handlers, validators
    ├── Ambev.DeveloperEvaluation.Integration
    └── Ambev.DeveloperEvaluation.Functional
```
