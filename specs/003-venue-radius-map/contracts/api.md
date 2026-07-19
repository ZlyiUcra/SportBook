# API Contracts: Geolocation-centered radius map of nearby venues

Delta contract on top of `specs/002-city-geolocation-map/contracts/api.md`. Everything not listed
here is unchanged from 002. DTO naming, auth posture (JWT required everywhere, no anonymous
browsing), and the standard error shape `{ "error": { "code", "message" } }` carry over.

All endpoints in this contract require authentication - no `[AllowAnonymous]` exception is
introduced.

## Venues - nearby (new)

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | /api/venues/nearby | Authenticated | Query: `lat`, `lng`, `sportType?` | `NearbyVenueResponse[]` - nearest first, capped at 100 |

`NearbyVenueResponse { id, name, city: CityResponse, address, description?, latitude, longitude,
distanceKm }` - the venue summary plus `distanceKm` (great-circle distance in km from the reference
point). The array is ordered by `distanceKm` ascending and is the single source for both the map
pins and the results list (they never diverge).

`GET /api/venues/nearby` contract MUSTs (consilium security verdict):

- Returns only venues that have BOTH `latitude` and `longitude` set and lie within a fixed 75 km of
  `(lat, lng)`. The 75 km radius is a SERVER-side constant; there is no radius parameter and a
  client cannot widen it.
- `lat` in [-90, 90], `lng` in [-180, 180]; anything else is 400.
- The CLIENT rounds device coordinates to 2 decimal places (~1.1 km) before calling; the endpoint's
  documented input precision is 2 decimals. When the reference point is a selected city, the client
  passes that city's coordinates.
- The server resolves the nearby venues, returns them, and neither persists nor logs the received
  coordinates. If request logging is ever introduced, this endpoint's query string is excluded (same
  rule as `GET /api/cities/nearest`).
- `sportType` (optional) narrows the in-range set to venues having an active court of that sport
  (same predicate as the existing `GET /api/venues` sport filter).
- Distance math runs in the Application layer over the materialized coordinate-bearing candidates
  (no trigonometry pushed to SQL); the only server-side query work is a translatable
  `Latitude != null` (+ optional sport) filter.

## Venues - superseded 002 behavior on the search page

The venue search page moves to the reference-point radius view. The following 002 pieces are
superseded (their endpoints are not removed, but the search page no longer drives them):

- The page-based results map (`VenueSearchMap`, pins of the current results page) is replaced by the
  radius map fed by `GET /api/venues/nearby`.
- The `includeNearby` city-neighbor expansion (150 km, on `GET /api/venues`) is replaced by the
  75 km point-radius model. `GET /api/venues?includeNearby=` remains in the 002 contract but is no
  longer used by the search page.
- The "My city" geolocation button is replaced by the "near me" action, which centers the radius on
  the device location instead of resolving to a city.

## Frontend contract MUSTs (consilium security verdict)

Not HTTP, but contract-level all the same - these bind the map implementation:

- Map marker/popup content is rendered exclusively as react-leaflet JSX children (React-escaped).
  The "nearest bigger" emphasis is a second `L.icon`; `L.divIcon({ html })` fed from venue-derived
  fields (name/description) is forbidden - those are unvalidated user input and any raw-HTML render
  is stored XSS.
- The map stack (leaflet, react-leaflet, react-leaflet-cluster, leaflet.markercluster, their CSS,
  marker assets) is loaded only via the existing lazy `MapView` chunk - it must not appear in any
  route's initial JS chunk (spec SC-006).
- The map obtains the device location only through an explicit "near me" action - no automatic
  geolocation prompt on page or map load.
- Tile URL and attribution stay the `shared/config` constants; visible attribution for OSM tiles and
  GeoNames (CC BY 4.0) remains mandatory (About page + map attribution control).
