# Implementation Plan: Return-to-search navigation and viewport-synced venue list

**Branch**: `004-search-return-viewport-list` | **Date**: 2026-07-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-search-return-viewport-list/spec.md`

## Summary

Make the 003 radius search a place the customer can leave and come back to, and make its list
follow the map. Three slices: (1) the venue search state (reference point, sport filter) moves out
of page-component state into a session-scoped in-memory store, so returning from a venue page - via
a new always-visible "back to search" action or the browser's own back - restores the search with
no re-typing and no geolocation prompt, with the map re-framing to the default full-radius view;
(2) the results list is filtered to the venues inside the current map viewport (updated when a
zoom/pan gesture ends), nearest-first, with a dedicated "no venues in view" empty state, while the
emphasized marker stays the overall nearest; (3) the list is paginated client-side at 10 per page,
resetting to page 1 whenever the visible set changes. No backend change of any kind - everything
operates on the already-delivered in-range set. All contested choices were made by the user in the
2026-07-19 discussion and are recorded in the spec's Assumptions; research.md pins the mechanisms.

## Technical Context

**Language/Version**: TypeScript 6.0 frontend only - the backend (C# 14 / .NET 10) is untouched.

**Primary Dependencies**: No new packages. Zustand 5 (already in the repo for the session store)
hosts the search state; react-leaflet 5 / leaflet 1.9 (already in the lazy MapView chunk) provide
the viewport events. Everything else is existing code.

**Storage**: None. The search state lives in a plain in-memory Zustand store WITHOUT the persist
middleware - device coordinates must never reach localStorage/sessionStorage (spec FR-006). A full
page reload therefore also clears it, which is stricter than the spec requires and acceptable.

**Testing**: Vitest + RTL as in 003. The MapView mock grows an `onViewportChange` knob so tests can
drive bounds; the store gets a reset between tests. Covered: state restore across unmount/remount
with no geolocation call, viewport filtering, the two distinct empty states, pagination and its
reset rules.

**Target Platform**: Unchanged - React SPA for evergreen browsers.

**Project Type**: Web application (backend API + frontend SPA), unchanged; frontend-only feature.

**Performance Goals**: Trivial by construction - filtering and paginating happen over the already
in-memory in-range set (<= 100 rows, spec 003 cap). Viewport updates fire once per completed
gesture (`moveend`/`zoomend`), never continuously (spec FR-008).

**Constraints**: Carried from the spec: no geolocation prompt on restore (the store is read, the
Geolocation API is only ever called from the explicit "near me" action - unchanged from 003); no
persistence of coordinates (in-memory store only, no URL parameters either - coordinates in the URL
would leak into browser history); the map remains the only Leaflet consumer (`shared/ui/map`), so
viewport bounds cross its boundary as a plain serializable type, not a Leaflet class. ASCII-only
source files per repo rules.

**Scale/Scope**: ~6 frontend files touched (search store new; VenueSearchPage, VenueDetailPage,
MapView, useReferencePoint/useGeolocation wiring, i18n x3), plus tests. No schema, no endpoint, no
dependency changes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-003). The
standing recommendation to run `/speckit-constitution` remains open; not a blocker.

## Project Structure

### Documentation (this feature)

```text
specs/004-search-return-viewport-list/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/                             # UNCHANGED - no server work in this feature

frontend/
├── src/
│   ├── pages/
│   │   ├── venues/
│   │   │   ├── model/               # + searchStore (new): session-scoped in-memory Zustand store
│   │   │   │                        #   holding city, sportType, device coords; referencePoint
│   │   │   │                        #   derived by the same precedence as 003
│   │   │   └── ui/                  # VenueSearchPage: reads/writes the store instead of local
│   │   │                            #   state; viewport-filters the list; adds pagination (10/page)
│   │   │                            #   and the "no venues in view" empty state
│   │   └── venue-detail/            # VenueDetailPage: + always-visible "back to search" action
│   ├── shared/
│   │   ├── lib/                     # useReferencePoint reworked to read the store (same
│   │   │                            #   precedence); useGeolocation unchanged as the permission
│   │   │                            #   machine - granted coords are written into the store
│   │   ├── ui/map/                  # MapView: + onViewportChange callback (moveend/zoomend ->
│   │   │                            #   plain MapBounds type; fires once after initial framing)
│   │   └── i18n/                    # + back-to-search / no-venues-in-view keys in en/uk/pt
└── tests/                           # store restore, viewport filter, empty states, pagination
```

**Structure Decision**: Same two-project layout; this feature only touches the frontend. The store
lives under `pages/venues/model/` because the search page is its only consumer (the venue page
merely links back); `shared/ui/map/MapView` stays the single Leaflet boundary and exports bounds as
a plain type so no Leaflet type leaks into pages.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
