# Quickstart: SportBook Venue Booking

Validation guide for proving the feature works end-to-end once implemented. Not implementation
code - see `contracts/api.md` for exact request/response shapes and `data-model.md` for entities.

## Prerequisites

- .NET 10 SDK
- Docker (Desktop or Engine) - SQL Server 2025 Developer edition runs via `docker-compose.yml` at
  the repo root, no native SQL Server install needed (Developer edition is dev/test-only; see
  research.md for the licensing boundary)
- Node.js + yarn (per `CLAUDE.md` JS tooling preference)

## Setup

```powershell
# Database (from repo root); first start takes tens of seconds - wait for "healthy"
docker compose up -d
docker compose ps

# Backend
cd backend
dotnet restore
dotnet ef database update --project src/SportBook.Infrastructure --startup-project src/SportBook.Api

# Frontend
cd ../frontend
yarn install
```

The container creates no application database - the first `dotnet ef database update` creates it,
so the database name lives only in the connection string. The dev connection string targets
`127.0.0.1,14330` and needs `TrustServerCertificate=True` for the container's self-signed
certificate - that flag is dev-only and must never appear in a non-local connection string.

## Run

```powershell
# Database (from repo root, if not already up)
docker compose up -d

# Backend (from backend/)
dotnet run --project src/SportBook.Api

# Frontend (from frontend/)
yarn dev
```

To stop and remove the database container: `docker compose down` (add `-v` to also drop the data
volume, e.g. to reset local state).

## Validation scenarios

Each scenario maps to an acceptance scenario in `spec.md`. Run via the frontend, or via `curl`/an
HTTP client against the running API.

### 1. Book a court end-to-end (User Story 1, spec Acceptance Scenario 1)

1. Register a Customer account, then a VenueOwner account.
2. As the VenueOwner, create a Venue and a Court with a `pricePerHour` and operating hours
   covering the next day.
3. As the Customer, `GET /api/courts/{id}/availability?date=<tomorrow>` and confirm the expected
   free hourly slots are returned.
4. `POST /api/bookings` for one of those slots.
5. **Expect**: `201`, `status: "Pending"`, `totalPrice` equal to `pricePerHour * hours`, and the
   booking appears in `GET /api/bookings` for that customer.

### 2. Reject overlapping bookings (spec Acceptance Scenario 2, FR-004)

1. Using the booking from Scenario 1, attempt a second `POST /api/bookings` for the same court and
   an overlapping time range (different customer).
2. **Expect**: `409`, no second booking row created. Optionally, fire both requests concurrently
   to confirm only one succeeds (validates the concurrency-safe overlap check from
   `data-model.md`).

### 3. Owner confirms a booking (User Story 2, Acceptance Scenario 3)

1. As the VenueOwner from Scenario 1, `PUT /api/bookings/{id}/confirm` for the Pending booking.
2. **Expect**: `200`, `status: "Confirmed"`.
3. As a different VenueOwner (no relationship to this venue), attempt the same call.
4. **Expect**: `403`.

### 4. Cancellation cutoff (FR-005)

1. Create a booking starting less than 2 hours from now (adjust court operating hours as needed
   for the test).
2. As the owning customer, `PUT /api/bookings/{id}/cancel`.
3. **Expect**: `409` (inside the cutoff).
4. Create a booking starting more than 2 hours from now and repeat.
5. **Expect**: `200`, `status: "Cancelled"`, and the slot reappears in
   `GET /api/courts/{id}/availability`.

### 5. Ownership boundaries (SC-004, consilium authorization checklist)

1. As VenueOwner A, attempt `PUT /api/venues/{venueId}` for a venue owned by VenueOwner B.
2. **Expect**: `403`.
3. As Customer A, attempt `GET /api/bookings/{id}` for a booking made by Customer B.
4. **Expect**: `403` or `404` (do not leak existence of another customer's booking).

### 6. Reviews (User Story 3)

1. As any authenticated user, `POST /api/venues/{id}/reviews` with a rating and comment.
2. **Expect**: `201`, review appears in `GET /api/venues/{id}/reviews`, and the venue's
   `averageRating` on `GET /api/venues/{id}` updates accordingly.

## Automated coverage

- `SportBook.UnitTests`: overlap check, price computation, cancellation cutoff, ownership checks -
  run without a real database (EF Core Sqlite in-memory).
- `SportBook.IntegrationTests`: the six scenarios above via `WebApplicationFactory`, including the
  concurrent double-booking attempt in Scenario 2.
- Frontend: Vitest component tests for booking flow and owner dashboard; no E2E runner is in scope
  for this iteration.
