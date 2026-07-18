# API Contracts: City Selection, Geolocation and Venue Map

Delta contract on top of `specs/001-sportbook-venue-booking/contracts/api.md`. Everything not
listed here is unchanged from 001. The **Venues** section below SUPERSEDES the 001 venue
endpoints - 001's `city` string parameter and request field no longer exist after this feature
ships. DTO naming, auth posture (JWT required everywhere, no anonymous browsing), and the
standard error shape `{ "error": { "code", "message" } }` all carry over from 001.

All endpoints in this contract require authentication - the city directory intentionally
introduces **no** `[AllowAnonymous]` exception; any future anonymization is a separate decision.

## Cities (new)

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/cities | Authenticated | Query: `query` (min 2 chars, max 100) | `CityResponse[]` - at most 10, ordered by population DESC |
| GET | /api/cities/nearest | Authenticated | Query: `lat`, `lng` | `CityResponse` - the nearest directory city |

`CityResponse { id, nameEn, nameUk, namePt, regionEn, regionUk, regionPt, latitude, longitude }` -
the client picks the display name by active locale; region names disambiguate same-named
settlements in suggestions. `population` is intentionally not exposed (whitelist DTO - ranking is
the server's job).

`GET /api/cities` matches `query` as a case-insensitive prefix/substring against ALL localized
name columns, so typing in any app language finds the city. Queries shorter than 2 characters
return 400. There is no endpoint returning the full list.

`GET /api/cities/nearest` contract MUSTs (consilium security verdict):

- `lat` in [-90, 90], `lng` in [-180, 180]; anything else is 400.
- The CLIENT rounds coordinates to 2 decimal places (~1.1 km) before calling; the endpoint's
  documented input precision is 2 decimals.
- The server resolves the nearest city, returns it, and neither persists nor logs the received
  coordinates. If request logging is ever introduced, this endpoint's query string is excluded.

## Venues (SUPERSEDES 001)

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/venues | Authenticated | Query: `cityId?, includeNearby=false, sportType?, page=1, pageSize=20` | `PagedResponse<VenueSummaryResponse>` |
| GET | /api/venues/{id} | Authenticated | - | `VenueDetailResponse` |
| POST | /api/venues | Authenticated (VenueOwner) | `CreateVenueRequest { name, cityId, address, description?, latitude?, longitude? }` | `VenueDetailResponse` (201) |
| PUT | /api/venues/{id} | Authenticated (owner only) | `UpdateVenueRequest { name, cityId, address, description?, latitude?, longitude? }` | `VenueDetailResponse` |
| DELETE | /api/venues/{id} | Authenticated (owner only) | - | unchanged from 001 |

Changes against 001, field by field:

- Search: `city` (string) is replaced by `cityId` (int). `includeNearby` (bool, default false)
  is honored only together with `cityId`: the server expands the filter to cities within the
  fixed 150 km radius - the radius is a server-side constant, not a parameter; clients cannot
  widen it. Each result carries its own city so cross-city results are labelled (spec US4).
- `VenueSummaryResponse` gains `city: CityResponse`, `latitude?`, `longitude?` (null when the
  owner has not set a pin - consumers must not substitute city coordinates; spec FR-009/FR-010).
- `VenueDetailResponse`: the `city` string becomes `city: CityResponse`; gains `latitude?`,
  `longitude?` with the same null semantics.
- `CreateVenueRequest`/`UpdateVenueRequest`: `city` string becomes `cityId` (must reference an
  existing city, else 400). `latitude`/`longitude` are optional, both-or-neither (400 if only
  one is present), range-validated like the cities endpoints. Omitting both on update clears
  the pin.

## Frontend map contract MUSTs (consilium security verdict)

Not HTTP, but contract-level all the same - these bind the map implementation:

- Map popup/tooltip content is rendered exclusively as react-leaflet JSX children (React-escaped).
  `bindPopup`/`bindTooltip`/`setContent` with strings are forbidden, as is `L.divIcon({ html })`
  fed from venue-derived fields - `Venue.Name`/`Description` are unvalidated user input and any
  raw-HTML render of them is stored XSS.
- The map stack (leaflet, react-leaflet, leaflet.css, marker assets) is loaded only via dynamic
  `import()` - it must not appear in the initial JS chunk of any route (spec SC-006).
- Tile URL and attribution are constants in `shared/config`; visible attribution for OSM tiles
  and GeoNames (CC BY 4.0) is mandatory (About page + map attribution control).
