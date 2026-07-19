# SportBook

SportBook is a platform for booking sports venues (courts and fields). Customers search venues by
city and sport type, view available time slots, and book or cancel bookings. Venue owners manage
their own venues and courts, confirm incoming bookings, and see who booked what. Authenticated
users leave reviews on venues they have used. The product is designed to support future
monetization on both sides of the marketplace (see `specs/001-sportbook-venue-booking/spec.md` for
the full specification).

## About

SportBook exists to solve one problem: a sports venue (a padel court, a tennis club, a football
field) needs a simple way to publish its availability and let people book a specific hour without
a phone call, and the venue's staff need a simple way to see who is coming and confirm it. The
product is two-sided by design - the same account model and the same UI serve both sides, there is
no separate "business" signup:

- **As a customer**, you search venues by city and sport, see a court's free hourly slots for a
  given day, and book one. The price is always computed server-side from the court's
  `pricePerHour` - the app never trusts a client-supplied price. A booking can be cancelled up
  until 2 hours before its start; inside that window cancellation is refused so a venue is not
  left with an empty, unbillable slot at the last minute.
- **As a venue owner**, you list your own venues and courts (name, address, sport type, price per
  hour, opening/closing hours), see the bookings made against them, and confirm each pending
  booking. A venue or court cannot be deleted while it still has an upcoming, non-cancelled
  booking against it, so a customer's booking can never silently disappear.
- **Reviews** are open to any authenticated user who wants to rate a venue (1-5 stars plus an
  optional comment): one review per user per venue - submitting again replaces your previous
  rating and comment rather than creating a duplicate, and the venue's average rating updates
  immediately.

Every account can act as both a customer and a venue owner at the same time - registration always
creates a plain account, and there is currently no separate "become a venue owner" step or
approval process; whether you use the customer-facing search/booking pages or the owner dashboard
is simply a matter of which nav link you click. Everything in the app requires being logged in
(there is no anonymous browsing) - see `specs/001-sportbook-venue-booking/spec.md` (FR-014) for
why that's a deliberate constraint, not an oversight.

## Current status

All three `001` user stories are implemented end to end (backend + frontend):

- Book a court: search venues, view availability, book, cancel (2-hour cutoff).
- Manage venue and courts: venue owners create/edit/delete their own venues and courts, confirm
  pending bookings, view their own venue's bookings.
- Build trust through reviews: authenticated users rate and review a venue (one review per user
  per venue), see the venue's average rating.

`002` replaces free-text city search with a structured, GeoNames-backed city directory: customers
pick a city from suggestions (typeable in any of the app's three languages) instead of typing free
text, can detect their nearest city from the browser's geolocation, and can widen search to venues
within 150km. Venue owners pick their venue's city from the same directory and can optionally place
a precise map pin; the search results page and a pinned venue's own page show it on a lazily loaded
map - see `specs/002-city-geolocation-map/spec.md`.

`003` replaces that page-based results map and the 150km city-neighbor toggle with a single
reference-point radius view: a customer's device location (via an explicit "near me" action) or
their selected directory city becomes the center of a fixed 75km search - the map shows the
in-range venues clustered (nearest emphasized, auto-framed to fit), and the results list below
shows the same set, nearest first. No map is shown when there is neither a granted location nor a
selected city - see `specs/003-venue-radius-map/spec.md`.

Automated tests cover the booking flow (25 tests: 11 unit, 14 integration against a real SQL Server
instance) plus the city/map feature's suggestion ranking, nearest-city resolution, nearby-radius
enforcement, and venue location validation, plus the venue radius feature's distance/order/cap
logic, SQL-translation guard, and the nearby endpoint's range validation and radius enforcement.
Tests for venue management and reviews (001) are deferred to that feature's polish phase - see
`specs/001-sportbook-venue-booking/tasks.md`.

## Components

```text
backend/
  src/
    SportBook.Api             ASP.NET Core Web API - controllers, JWT auth, DI wiring
    SportBook.Application     Services (business logic), DTOs, request/response mapping
    SportBook.Domain          Entities, enums - no framework dependencies
    SportBook.Infrastructure  EF Core DbContext, migrations, SQL Server provider registration,
                              committed City reference data (Data/cities.csv, GeoNames CC BY 4.0)
  tests/
    SportBook.UnitTests         xUnit + EF Core Sqlite in-memory - no real database needed
    SportBook.IntegrationTests  xUnit + WebApplicationFactory - runs against the real SQL Server
                                 container (booking-overlap concurrency needs the real engine)

frontend/
  src/                       Feature-Sliced Design layering
    app/                     Routes, layouts, providers
    pages/                   One folder per route (ui/<Route>Page.tsx)
    features/                One user action per slice (ui + model + api), including
                              city-select (directory combobox, "near me" geolocation)
    entities/                Domain data (types, read API calls), including city
    shared/                  UI kit (shadcn/ui), Axios instance, i18n, theme store, utils, and
                              ui/map (the only module importing leaflet/react-leaflet/clustering,
                              always lazy-loaded)

scripts/convert-geonames-cities.ps1
                             One-time GeoNames-to-cities.csv dataset conversion (see backend/README.md)
docker-compose.yml           SQL Server 2025 Developer edition for local development
specs/001-sportbook-venue-booking/
                             Full spec, plan, data model, API contracts, task breakdown
specs/002-city-geolocation-map/
                             City directory, geolocation and venue map - spec, plan, task breakdown
specs/003-venue-radius-map/
                             Reference-point radius map of nearby venues - spec, plan, task breakdown
```

**Backend stack**: C# / .NET 10, ASP.NET Core Web API (MVC controllers), EF Core 10 +
`Microsoft.EntityFrameworkCore.SqlServer`, JWT bearer authentication, xUnit.

**Frontend stack**: React 19, Vite, TypeScript, TanStack Query, Zustand, React Hook Form + Zod,
Axios, Tailwind CSS + shadcn/ui, i18next (English, Ukrainian, Portuguese), Vitest, Leaflet +
react-leaflet + react-leaflet-cluster (lazy-loaded map), `cmdk` (city combobox).

## Prerequisites

- Docker (Desktop or Engine) - runs SQL Server 2025 Developer edition locally, no native SQL
  Server install needed. Developer edition is licensed for development/test only, never for
  production (see `specs/001-sportbook-venue-booking/research.md`).
- .NET 10 SDK.
- The `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef` (or
  `dotnet tool update --global dotnet-ef` if an older version is already installed).
- Node.js and yarn.

## Local setup

### Quick start (scripts)

`scripts/start.ps1` runs the one-time setup below (idempotent - safe to re-run) and then opens
the backend and frontend dev servers each in their own PowerShell window:

```powershell
powershell -File scripts/start.ps1
```

It fills in sample values for the SQL login password and JWT signing key on first run (see
`scripts/setup.ps1` - the two variables at the top) so nothing needs to be typed in manually.
Those sample values are fine for solo local development; change them if this machine is ever
shared with anyone else.

Each dev server's window stays open only until it responds - once a service is confirmed up, its
window is hidden automatically (the process keeps running in the background; only the window
disappears). If a service fails to start, its window is left visible so you can read the error.

To stop both:

```powershell
powershell -File scripts/start.ps1 -Stop
```

This finds whatever is listening on the backend/frontend ports and stops it, including the
hidden windows - works even if you started them some other way.

Running just the one-time setup without starting the dev servers:

```powershell
powershell -File scripts/setup.ps1
```

Or step through the setup manually:

### 1. Start the database

```powershell
docker compose up -d
docker compose ps
```

First start takes tens of seconds while SQL Server initializes - wait until the `mssql` service
shows `healthy` before continuing.

### 2. Create the application's SQL login

The container only bootstraps the `sa` (sysadmin) login. The application is meant to connect with
a least-privilege login instead - `scripts/setup.ps1` does this step automatically; the command
below is what it runs, shown here for anyone stepping through setup manually:

```powershell
docker exec sportbook-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SportBook_Dev_Passw0rd" -C -b -Q "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'sportbook_app') BEGIN CREATE LOGIN sportbook_app WITH PASSWORD = 'Sb_App_Login_Dev9!', CHECK_POLICY = ON; END; ALTER SERVER ROLE dbcreator ADD MEMBER sportbook_app;"
```

Feel free to change the password in that command - just use the same value in the connection
string in the next step.

### 3. Configure backend secrets

Nothing secret is committed to the repo. `appsettings.json` only documents the shape of the
required configuration with empty values; set the real values locally via `dotnet user-secrets`:

```powershell
cd backend/src/SportBook.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,14330;Database=SportBookDb;User Id=sportbook_app;Password=Sb_App_Login_Dev9!;TrustServerCertificate=True;Encrypt=True"
dotnet user-secrets set "Jwt:Key" "<any long random string, e.g. output of a password generator>"
```

`TrustServerCertificate=True` is required locally because the container presents a self-signed
certificate - this flag must never be used outside local development.

### 4. Create the database and run the backend

```powershell
cd backend
dotnet restore
dotnet ef database update --project src/SportBook.Infrastructure --startup-project src/SportBook.Api
dotnet run --project src/SportBook.Api
```

The API listens on `http://localhost:5217` by default (see
`backend/src/SportBook.Api/Properties/launchSettings.json`). The first `dotnet ef database
update` run creates the `SportBookDb` database - nothing else creates it.

### 5. Run the frontend

```powershell
cd frontend
yarn install
yarn dev
```

The dev server listens on `http://localhost:5173` and expects the API at
`http://localhost:5217/api` (see `frontend/.env.development`).

## Using the application

Once both the backend (`http://localhost:5217`) and frontend (`http://localhost:5173`) are
running (see "Local setup" above), open the frontend URL in a browser. Every screen except
Login/Register requires being signed in - you're redirected to Login if you aren't.

### Account and settings

- **Register** (`/register`): name, email, password. This is the only way to create an account -
  there is no separate "sign up as a venue owner" flow (see "About" above). You're signed in
  immediately after registering.
- **Login** (`/login`): email + password. A successful login (or registration) stores an access
  token and a refresh token; the access token is attached to every API request automatically, and
  is refreshed transparently when it expires - you don't need to log in again mid-session.
- **Settings menu** (top-right on every screen, including the anonymous Login/Register pages): a
  combined language and theme switcher. Language: English, Ukrainian, or Portuguese. Theme: light,
  dark (the default), or blue. Both choices persist across reloads (stored in the browser).
- On narrow screens (below the `md` breakpoint) the top navigation collapses into a hamburger menu;
  the current page is always highlighted, in both the desktop nav and the mobile drawer.

### Booking a court (customer flow)

1. The home page (`/`) is venue search: pick a city from the directory combobox (type in any of
   the three app languages) and/or a sport type, or tap "Near me" to center the search on your
   device's location instead. Either way you get the same fixed 75km radius view - a clustered map
   (the nearest venue emphasized, all pins auto-framed to fit) plus a distance-ordered results list
   of the same in-range venues; no map is shown until you pick a city or use "Near me".
2. Open a venue (`/venues/:id`) to see its address, description, average rating and existing
   reviews, its list of courts (name, sport, price per hour, opening/closing hours), and a map
   with its pin if the owner has set one.
3. Pick a court and a date; the page fetches that court's free whole-hour slots for the day (slots
   already booked - by anyone, Pending or Confirmed - don't appear).
4. Pick a free slot and book it. The booking is created as **Pending** with its price already
   computed (`pricePerHour x hours`) - nothing about the price is entered by you.
5. **My Bookings** (`/bookings`) lists every booking you've made, with its current status
   (Pending, Confirmed, Cancelled) and price. You can cancel a booking from here as long as it
   starts more than 2 hours from now; inside that window the cancel action is refused.
6. From a venue's page you can also leave a review (1-5 stars + an optional comment) once you're
   signed in - submitting a second review for the same venue overwrites your first one instead of
   adding a new one, and the venue's average rating updates right away.

### Managing your own venues (owner flow)

1. **Owner Dashboard** (`/owner/venues`): create a venue (name, a city picked from the same
   directory combobox customers search with, address, optional description, and an optional
   precise location pin placed on a map), then add one or more courts to it (name, sport type,
   price per hour, opening and closing time). Existing venues/courts can be edited here too
   (including moving or removing the location pin), and a court can be deactivated (`isActive`)
   without deleting it if it's temporarily out of service.
2. Deleting a venue or a court is blocked while it still has an upcoming, non-cancelled booking
   against it - cancel or wait out those bookings first if you need to remove it.
3. **Owner Bookings** (`/owner/bookings`): see every booking made against your own venues (any
   court, any customer) and confirm each **Pending** one, moving it to **Confirmed**. Only the
   owner of the venue a booking belongs to can see or confirm it - another owner's dashboard never
   shows it.

## Running tests

```powershell
dotnet test backend/tests/SportBook.UnitTests/SportBook.UnitTests.csproj
dotnet test backend/tests/SportBook.IntegrationTests/SportBook.IntegrationTests.csproj
```

Unit tests use an in-memory Sqlite database and need nothing else running. Integration tests
need the SQL Server container from step 1 running and reachable - they create and drop their own
`SportBookDb_Tests` database, separate from the one the app itself uses.

## Further reading

- `specs/001-sportbook-venue-booking/spec.md` - full feature specification.
- `specs/001-sportbook-venue-booking/plan.md` - technical plan and architecture decisions.
- `specs/001-sportbook-venue-booking/data-model.md` - entity definitions and validation rules.
- `specs/001-sportbook-venue-booking/contracts/api.md` - HTTP API contract.
- `specs/001-sportbook-venue-booking/tasks.md` - full task breakdown and current progress.
- `specs/002-city-geolocation-map/spec.md` - city directory, geolocation and venue map spec.
- `specs/002-city-geolocation-map/plan.md` - technical plan (map loading boundary, seeding
  strategy, coordinate modeling).
- `specs/002-city-geolocation-map/data-model.md` - City entity, Venue changes, migration chain.
- `specs/002-city-geolocation-map/contracts/api.md` - Cities endpoints and the reshaped Venues
  contract (supersedes `001`'s venue endpoints).
- `specs/002-city-geolocation-map/tasks.md` - full task breakdown and current progress.
- `specs/003-venue-radius-map/spec.md` - reference-point radius map of nearby venues spec.
- `specs/003-venue-radius-map/plan.md` - technical plan (in-memory haversine, clustering library
  choice, fitBounds behaviour).
- `specs/003-venue-radius-map/data-model.md` - `NearbyVenueResponse` DTO and the distance
  computation shape.
- `specs/003-venue-radius-map/contracts/api.md` - nearby endpoint contract and consilium MUSTs.
- `specs/003-venue-radius-map/tasks.md` - full task breakdown and current progress.
