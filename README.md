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
| GET | `/api/v1/customers` | List customers |
| GET | `/api/v1/vehicles` | List vehicles |
| GET | `/api/v1/work-orders` | List work orders |
| GET | `/api/v1/invoices` | List invoices |
| GET | `/api/v1/dashboard/summary` | Dashboard KPIs |
| GET | `/health` | Health check |

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
- [ ] PDF export (Phase 1 — next sprint)
- [ ] Inline editing of existing records (create + delete shipped)

## Configuration

Key settings in `src/AutomotiveWorkshop.Api/appsettings.Development.json`:

- `ConnectionStrings:DefaultConnection` — PostgreSQL connection
- `Jwt:Secret` — JWT signing key (change in production)
- `Cors:Origins` — Allowed frontend origins

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
