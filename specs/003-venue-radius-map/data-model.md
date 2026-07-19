# Phase 1 Data Model: Geolocation-centered radius map of nearby venues

This feature adds NO persistent schema (no new table, column, or migration). It reuses the
existing entities from 002 and introduces one transport shape and one transient runtime value.
Field names use C# PascalCase; the 001/002 conventions stand.

## Venue (unchanged, reused)

No change. The feature reads the existing nullable `Latitude`/`Longitude` (`decimal?`,
precision (9,6)) added in 002. Only venues where both are set participate in the radius view;
venues with null coordinates are excluded from the nearby query and from the map/list.

## City (unchanged, reused)

No change. When the reference point is a selected city, the client already holds the city's
`Latitude`/`Longitude` from the `CityResponse` (002) and sends them as the nearby query's `lat`/
`lng`. The server does not distinguish a city-sourced point from a device-sourced one.

## Reference point (transient, not persisted)

The center of the 75 km search. Resolved on the client by precedence:

1. Device location from an explicit "near me" action - `latitude`/`longitude` rounded to 2 decimals
   (~1.1 km) before leaving the device.
2. The explicitly selected directory city's `latitude`/`longitude`.
3. None - no query is issued and no map is shown.

It exists only for the duration of a request/render. The server neither stores nor logs the
received coordinates (contract MUST). There is no entity, table, or column for it.

## NearbyVenueResponse (new transport DTO)

The nearby endpoint's item shape - the existing venue summary fields plus the computed distance.
It is the single source that drives BOTH the map pins and the results list (spec FR-013), so the
two never diverge.

| Field | Type | Notes |
|---|---|---|
| Id | Guid | Venue id |
| Name | string | Venue name |
| City | CityResponse | Nested city (same shape as 002's `VenueSummaryResponse.City`) |
| Address | string | |
| Description | string? | |
| Latitude | decimal | Non-null here by construction (only coordinate-bearing venues are returned) |
| Longitude | decimal | Non-null here by construction |
| DistanceKm | decimal | Great-circle distance from the reference point, used to order the list and mark the nearest venue; rounded for display stability |

**Ordering & cap**: the endpoint returns the list ordered by `DistanceKm` ascending (nearest
first) and capped at the nearest 100 (a stated contract cap, not an emergent property of the
current row count). `Population` and other non-whitelisted fields never appear (same DTO
discipline as 002).

## Distance computation (Application, not schema)

No persisted distance. For a request `(lat, lng[, sportType])`:

1. Materialize candidate venues via a translatable query: `Venues` where `Latitude != null` (and
   `sportType` filter if supplied, reusing the existing active-court sport predicate), including the
   `City` for the response - this is the only server-side work and it stays SQL-translatable (assert
   with `ToQueryString()`; no trigonometry in SQL).
2. In C#, compute `CityDistance.DistanceKm(lat, lng, venue.Latitude, venue.Longitude)` per candidate,
   filter to `<= VenueRadiusKm (75)`, order ascending, take the nearest 100, project to
   `NearbyVenueResponse` with `DistanceKm`.

Consumers: the venue search page's radius map (clustered pins, nearest emphasized, auto-framed) and
its distance-ordered results list - the same list for both.
