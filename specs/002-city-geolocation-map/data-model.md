# Phase 1 Data Model: City Selection, Geolocation and Venue Map

Changes to the 001 data model, as agreed in the consilium artifact
(`.specify/consilium/2026-07-18-city-geolocation-map.md`) and the spec Key Entities section. Field
names use C# PascalCase; implementation types (EF Core configuration, SQL types) belong to the
Infrastructure layer, with one binding convention: all latitude/longitude fields are `decimal`
with explicit `HasPrecision(9, 6)` (~0.11 m resolution - far below any product need, stable
across providers). The 001 conventions (UTC timestamps, explicit precision for decimals) stand.

## City (new)

A settlement from the GeoNames-derived reference directory. Reference data: rows change only via
migration, never at runtime, and the table is small enough (~3-6k rows) to cache in memory
indefinitely.

| Field | Type | Notes |
|---|---|---|
| Id | int | Primary key = GeoNames `geonameid`. Natural, globally stable external key; no identity column - seed data carries explicit IDs, which keeps the seed deterministic and future dataset refreshes diffable |
| NameEn | string | Required, max 200 - GeoNames primary name |
| NameUk | string | Required, max 200 - from alternatenames (falls back to NameEn when absent) |
| NamePt | string | Required, max 200 - from alternatenames (falls back to NameEn when absent) |
| CountryCode | string | Required, ISO 3166-1 alpha-2 ("UA" for the entire v1 dataset); single source of country truth - no Country table |
| RegionEn | string | Required - admin1 display name for suggestion disambiguation |
| RegionUk | string | Required - admin1 display name, localized like city names |
| RegionPt | string | Required - admin1 display name, localized like city names |
| Latitude | decimal | Required, [-90, 90], precision (9,6) |
| Longitude | decimal | Required, [-180, 180], precision (9,6) |
| Population | int | Required, >= 500 by dataset threshold; used only for suggestion ranking (population DESC) - not exposed in DTOs |

**Relationships**: One City has many Venues.

**Indexes**: PK on Id. Suggestion matching filters on the three name columns with a leading-
substring match over ~3-6k rows - index the name columns only if measurement ever demands it (the
table fits in a single buffer-pool page span; do not add speculative indexes).

**Validation rules**: The directory is read-only at runtime - no API creates, updates, or deletes
cities. Dataset refreshes are a new migration produced by re-running the conversion script
(recorded as a follow-up story, not part of this feature's runtime).

## Venue (changed)

| Field | Change | Notes |
|---|---|---|
| City | **removed** | Legacy free-text column; dropped by the final migration of this feature |
| CityId | **added**: int (FK -> City) | Required (NOT NULL) at the end of the migration chain; replaces the string as the only city linkage. Requests referencing a non-existent CityId are rejected with 400 |
| Latitude | **added**: decimal? | Optional, [-90, 90], precision (9,6); set only via the owner's pin |
| Longitude | **added**: decimal? | Optional, [-180, 180], precision (9,6) |

**Validation rules**: `Latitude`/`Longitude` are both-or-neither - a venue has either a complete
precise location or none (enforced in the Application layer on Create/Update; removing the pin
nulls both). No coordinate is ever derived from the city automatically - a venue without a pin
has NULL coordinates and simply does not appear on maps (spec FR-009/FR-010: no city-centre
fallback).

**Relationships**: Venue now belongs to exactly one City. All other 001 relationships unchanged.

## Migration chain (Infrastructure)

Three migrations, in order, all inside this feature:

1. **CreateAndSeedCities**: creates `Cities`; reads the committed city data file (embedded
   resource of Infrastructure, produced offline by the dataset conversion script) and emits
   deterministic INSERT batches. No `HasData`.
2. **AddVenueCityIdAndCoordinates** (match-or-fail, transactional):
   - add `Venues.CityId` as nullable FK + index, add nullable `Latitude`/`Longitude`;
   - backfill: `UPDATE` by exact string match of the legacy `Venues.City` value against city
     names (EN and UK columns);
   - guard: if any `Venues.CityId IS NULL` remains, `THROW` with the list of unmatched city
     strings in the message - the transaction rolls back, the operator fixes the handful of dev
     rows and re-runs;
   - `ALTER` `CityId` to NOT NULL.
3. **DropVenueLegacyCity**: drops the `Venues.City` string column. Separate migration by design
   (the agreed drop-timing compromise), same feature.

Raw SQL in migrations 2-3 is the established Infrastructure-only exception to the no-raw-SQL
rule. Fresh databases (integration test host, new dev machines) have zero venues, so the guard
passes trivially; there is no production database.

## Neighbor computation (Application, not schema)

No persisted neighbor data. The neighbor set of a city (cities within the 150 km server-side
constant) is computed in Application from the cached city list - bounding-box prefilter, then
exact haversine (pure, unit-tested function) - and cached per city for the process lifetime.
Consumers: nearest-city resolution (`GET /api/cities/nearest`) and search expansion
(`includeNearby`). The venue query then filters `CityId IN` the computed set, which EF translates
to an OPENJSON parameter on SqlServer (measured worst case: 722 IDs, ~6KB) and which stays
translatable on the Sqlite unit-test provider - assert with `ToQueryString()` in a unit test.
