# Automotive Workshop Management System

Phase 1 MVP — Angular frontend + ASP.NET Core Web API backend.

## Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 19, Angular Material, SCSS |
| Backend | ASP.NET Core 10, Clean Architecture |
| Database | PostgreSQL 16 |
| Auth | ASP.NET Identity + JWT |

## Project Structure

```
automotive-workshop/
├── src/
│   ├── AutomotiveWorkshop.Api/           # REST API controllers
│   ├── AutomotiveWorkshop.Application/   # Services, DTOs, validators
│   ├── AutomotiveWorkshop.Domain/        # Entities, enums
│   └── AutomotiveWorkshop.Infrastructure/# EF Core, Identity, JWT, email
├── tests/
│   └── AutomotiveWorkshop.UnitTests/
├── web/                                  # Angular SPA
├── docker/
│   └── Dockerfile.api
└── docker-compose.yml
```

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for PostgreSQL)

## Quick Start

### 1. Start PostgreSQL

```bash
docker compose up postgres -d
```

### 2. Run the API

```bash
dotnet run --project src/AutomotiveWorkshop.Api
```

API runs at `http://localhost:5001`. On first launch in Development, migrations run and seed data is applied.

**Default admin credentials:**
- Email: `admin@workshop.local`
- Password: `Admin123!`

### 3. Run the Angular app

```bash
cd web
npm install
npm start
```

App runs at `http://localhost:4200`.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/v1/auth/login` | Login |
| GET | `/api/v1/auth/me` | Current user |
| GET/POST/PUT/DELETE | `/api/v1/customers` | Customer CRUD |
| GET/POST/PUT/DELETE | `/api/v1/vehicles` | Vehicle CRUD |
| GET | `/api/v1/service-catalog` | Service catalog items (for line-item picker) |
| GET/POST | `/api/v1/estimates` | List / create estimates |
| GET | `/api/v1/estimates/{id}` | Estimate detail |
| PUT | `/api/v1/estimates/{id}` | Edit estimate (notes, validity, line items) |
| PATCH | `/api/v1/estimates/{id}/status` | Send / approve / decline / expire |
| POST | `/api/v1/estimates/{id}/convert` | Convert approved estimate → work order |
| GET | `/api/v1/estimates/{id}/pdf` | Estimate PDF |
| GET | `/api/v1/work-orders` | List work orders |
| GET | `/api/v1/work-orders/{id}` | Work order detail (schedule, time entries) |
| GET | `/api/v1/work-orders/{id}/pdf` | Work order PDF |
| GET/POST | `/api/v1/work-orders/{id}/time-entries/clock-in` | Technician time tracking |
| PATCH | `/api/v1/time-entries/{id}/clock-out` | Close time entry |
| GET | `/api/v1/schedule` | Calendar events (`from`, `to`) |
| PATCH | `/api/v1/schedule/work-orders/{id}` | Reschedule / assign bay |
| GET/POST/PUT/DELETE | `/api/v1/parts` | Parts inventory CRUD |
| POST | `/api/v1/parts/{id}/adjust-stock` | Stock receive/issue/adjust |
| GET | `/api/v1/reports/sales` | Sales report |
| GET | `/api/v1/reports/tax` | Tax report |
| GET | `/api/v1/reports/technician-productivity` | Tech hours & jobs |
| GET | `/api/v1/users/technicians` | Technicians for assignment |
| GET | `/api/v1/invoices` | List invoices |
| GET | `/api/v1/invoices/{id}/pdf` | Invoice PDF |
| GET | `/api/v1/dashboard/summary` | Dashboard KPIs |
| GET | `/health` | Health check |

### Document workflow (M0a — "Shop Manager" basics)

`Estimate → (approve) → Work Order → (complete) → Invoice`, each printable as a
PDF. Write endpoints are role-gated (`Admin`, `Manager`, `Receptionist`;
technicians may update work-order status).

### M0b — Core operational layer

- **Scheduler**: day / week / month views; drag jobs with **30-minute** time snapping; move between days
- **Parts & inventory**: SKU, cost/retail, on-hand qty, reorder alerts, stock adjustments; **link parts to work-order lines** and issue stock from the job
- **Time tracking**: clock in/out per work order; logged hours on work order detail
- **Reporting**: sales, tax, and technician productivity for a date range
- **Dashboard**: clickable KPIs → open work orders, invoices, low-stock parts

Demo technicians: `tech1@workshop.local` / `tech2@workshop.local` (password `Tech123!`).

OpenAPI spec available at `/openapi/v1.json` in Development.

## Docker (API + DB)

```bash
docker compose up --build
```

## MVP Features Implemented

- [x] Solution scaffold (Clean Architecture)
- [x] JWT authentication with role-based access
- [x] Customer & vehicle CRUD APIs
- [x] Work order tracking with status workflow
- [x] Invoice generation from work orders
- [x] Analytics dashboard summary
- [x] Email notification logging (SendGrid/SMTP ready to wire)
- [x] Angular admin shell with feature pages
- [x] Add/search/delete customers & vehicles from the UI
- [x] Create work orders (with line items) and change status from the UI
- [x] Generate invoices from completed work orders and update invoice status
- [x] Searchable lists, status filters, and colored status badges
- [x] **Estimates**: create/edit, send→approve→convert workflow, document-centric detail page
- [x] **Estimate → Work Order → Invoice** conversion pipeline
- [x] **Service catalog picker** in estimate & work-order line items
- [x] **PDF generation** for estimates, work orders, and invoices (QuestPDF)
- [x] **Inline editing** of customers & vehicles
- [x] **Role-based authorization** enforced on write endpoints
- [x] **SMTP email** sending (configurable; logs when `Email:SmtpHost` is empty)
- [x] **Scheduler** with day/week/month views and drag-to-schedule
- [x] **Parts inventory** with stock tracking and low-stock filter
- [x] **Technician time tracking** on work orders
- [x] **Business reports** (sales, tax, technician productivity)

## Configuration

Key settings in `src/AutomotiveWorkshop.Api/appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection` — PostgreSQL connection
- `Jwt:Secret` — JWT signing key (change in production)
- `Cors:Origins` — Allowed frontend origins
- `Email:SmtpHost` etc. — SMTP settings; when `SmtpHost` is empty, emails are logged only

## Development Commands

```bash
# Build entire solution
dotnet build

# Add EF migration
dotnet ef migrations add <Name> \
  --project src/AutomotiveWorkshop.Infrastructure \
  --startup-project src/AutomotiveWorkshop.Api \
  --output-dir Persistence/Migrations

# Run tests
dotnet test

# Build Angular for production
cd web && npm run build
```
