# Implementation Plan: Preserve map viewport across venue-detail navigation and visible-venue count

**Branch**: `008-preserve-search-viewport` | **Date**: 2026-07-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/008-preserve-search-viewport/spec.md`

## Summary

Two frontend-only slices that amend spec 004's search page. (1) Preserve the customer's map viewport
(zoom and pan) across a venue-detail detour: store the map's center+zoom in the existing in-memory
`useSearchStore` (which already keeps city/sport/deviceCoords across navigation), and on the search
page's remount pass that saved center+zoom into `MapView` so `MapContainer` mounts directly at the
saved view - reversing 004 FR-004, which discarded the viewport on return. The saved viewport is
cleared only when the reference point changes (a genuinely new search); a sport-filter change keeps
it (spec FR-002). (2) Show the count of venues currently visible in the viewport above the results
list, reusing the already-computed visible set (004 FR-007) - `visibleVenues.length` with locale-
aware plural forms. No backend change, no schema or migration, no new dependency, no new HTTP
surface.

## Technical Context

**Language/Version**: TypeScript 6.0 frontend - unchanged. C#/.NET backend untouched by this feature.

**Primary Dependencies**: No new packages. Reuses the in-memory Zustand `useSearchStore`
(`pages/venues/model/searchStore.ts`), the `MapView` react-leaflet wrapper
(`shared/ui/map/MapView.tsx`), and i18next (already plural-aware) for the count label.

**Storage**: NONE added - no table, no column, no browser storage. The viewport joins the existing
in-memory searchStore, which has no `persist` middleware (004 contract MUST): it dies with the
session and on a full page reload. Device coordinates remain the only privacy-sensitive input and
are unchanged.

**Testing**: Vitest + React Testing Library, frontend-only (no backend change to cover). Tests
assert: the searchStore viewport is set from a viewport report and cleared on reference-point change
but NOT on sport-filter change; VenueSearchPage restores the saved center/zoom on remount and renders
the count equal to the visible-set length with the correct plural form; MapView reports center+zoom
alongside bounds on `moveend`/`zoomend` and on mount. The mocked MapView emits the enriched report.

**Target Platform**: React SPA, unchanged.

**Project Type**: Web application (backend API + frontend SPA); this feature is frontend-only.

**Performance Goals**: Trivial - one extra field in an in-memory store, one `Array.length` for the
count, and a center/zoom read that already happens in the existing `moveend`/`zoomend` handler. No
extra query, no extra render cost beyond a count line.

**Constraints**: The viewport MUST be in-memory only - never localStorage/sessionStorage/cookies/URL
(004 contract MUST, spec FR-005); restoring it MUST NOT call the Geolocation API (spec FR-003, 004
FR-003); it MUST clear on reference-point change and survive a sport-filter change (spec FR-002); the
count MUST equal the visible set (004 FR-007) and update on gesture-end only (004 FR-008); the count
label MUST follow each locale's plural rules (spec FR-009); Leaflet types/instances MUST NOT leak
across `shared/ui/map` (003 single-Leaflet-consumer rule, 004 contract MUST). ASCII-only source.

**Scale/Scope**: One store field (+setter) added; VenueSearchPage wiring for restore + count;
MapView's `onViewportChange` payload enriched with center+zoom; four locale files get a plural key.
No backend, no schema, no migration, no dependency.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified principles,
so the gate trivially passes pre- and post-design (same status as 001-007). The standing
recommendation to run `/speckit-constitution` remains open; not a blocker.

## Project Structure

### Documentation (this feature)

```text
specs/008-preserve-search-viewport/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── pages/
│   │   └── venues/
│   │       ├── model/searchStore.ts   # + viewport: { lat, lng, zoom } | null and setViewport.
│   │       │                           # Cleared on reference-point change (a new city or a fresh
│   │       │                           # "near me"); NOT cleared on a sport-filter change (FR-002).
│   │       │                           # In-memory only, no persist (004 contract MUST).
│   │       └── ui/VenueSearchPage.tsx  # Restore: pass saved center/zoom to MapView on remount and
│   │                                   # withhold fitBoundsKey while restoring (so MapContainer
│   │                                   # mounts at the saved view and is not auto-reframed). Save
│   │                                   # center+zoom from each viewport report. fitBoundsKey becomes
│   │                                   # reference-only (drops venue ids). Count: render
│   │                                   # visibleVenues.length above the list with plural-aware i18n.
│   ├── shared/
│   │   ├── ui/map/MapView.tsx          # onViewportChange payload enriched to report center+zoom
│   │   │                               # alongside bounds (so the store can persist the restorable
│   │   │                               # view while the list/count keep using bounds). fitBoundsKey
│   │   │                               # already conditional on `!== undefined`.
│   │   └── i18n/locales/{en,uk,pt,es}.json  # + venues.visibleCount with per-locale plural forms
└── tests/                              # searchStore viewport set/clear semantics; VenueSearchPage
                                        # restores saved center/zoom and renders the count with the
                                        # correct plural; MapView reports center+zoom.
```

**Structure Decision**: Same frontend slice as 004's search page. The only state addition is a
viewport field on the existing in-memory `useSearchStore` (center+zoom - exactly the restorable view,
matching spec FR-001's "zoom level and pan position"); bounds for the list/count filter stay
ephemeral page state, repopulated on mount by the existing ViewportReporter. `MapView`'s
`onViewportChange` payload is enriched (not replaced) so both consumers - the list filter (bounds)
and the store (center+zoom) - read one report. The count is a pure render of the already-computed
`visibleVenues.length`. No backend file is touched.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
