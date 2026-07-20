# Implementation Plan: Frontend resilience and continuity polish

**Branch**: `013-frontend-resilience-continuity` | **Date**: 2026-07-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/013-frontend-resilience-continuity/spec.md`

**Note**: This plan documents work already implemented and verified (see spec.md Assumptions) -
it records the technical approach actually taken, not a forward-looking design.

## Summary

Three related frontend pieces. (1) A top-level `ErrorBoundary` (class component, React has no
hook-based error-boundary API) wraps the whole app, replacing blank-white-screen render failures
with a message and a reload action. (2) A single `PageLoader` component (full-viewport
blurred-backdrop overlay + spinner, matching the existing modal dialogs' visual treatment)
replaces 12 scattered inline "Loading..." usages across 6 pages/components. (3) The venue search
page's search state (city, sport filter, map viewport, results page number) moves from
in-memory-only to `localStorage`-backed, excluding raw device GPS coordinates; a related bug in
the results-page restoration (the map's contractual once-on-mount viewport report was
misidentified as a genuine change, resetting a restored page back to 1) was found and fixed, with
two regression tests added.

## Technical Context

**Language/Version**: TypeScript / React 19, existing frontend stack

**Primary Dependencies**: `lucide-react` (`Loader2` icon, already installed) for `PageLoader`; no
new dependency for the error boundary or search-state persistence

**Storage**: Browser `localStorage` (client-side only, key `sportbook-venue-search`) - no
database/schema involvement, no backend change

**Testing**: Vitest (existing frontend suite) - 2 new regression tests added
(`tests/pages/VenueSearchReturn.test.tsx`, "pagination restore (013)" describe block) for the
mount-settle-report bug; `tsc -b` and `oxlint` as the compile/lint gate

**Target Platform**: Browser (existing SPA, unchanged deployment)

**Project Type**: Web application frontend (no backend change)

**Performance Goals**: None new

**Constraints**: No new npm dependency beyond an already-installed icon; raw device GPS
coordinates must never reach `localStorage` (revises, but does not fully lift, the blanket
in-memory-only rule from specs 003/004/008); a restored results page number must degrade
gracefully (clamp to nearest valid page) rather than show an empty list

**Scale/Scope**: 1 new provider (`ErrorBoundary`), 1 new shared component (`PageLoader`) replacing
12 call sites across 6 files, 1 store rework (`searchStore.ts`) adding persistence and a `page`
field, 1 page-reset-effect bug fix in `VenueSearchPage.tsx`, 2 new locale keys x 4 languages, 2
new regression tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-012).

## Project Structure

### Documentation (this feature)

```text
specs/013-frontend-resilience-continuity/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/            # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks - not created by this command)
```

### Source Code (repository root)

```text
frontend/src/
├── app/
│   ├── providers/ErrorBoundary.tsx   # NEW - class component, componentDidCatch, withTranslation
│   └── main.tsx (repo root src/)     # Wraps <App /> with <ErrorBoundary>
├── shared/
│   ├── ui/page-loader.tsx            # NEW - full-viewport overlay + Loader2 spinner
│   └── i18n/locales/*.json           # New common.somethingWentWrong/reloadPage keys (4 languages)
├── pages/venues/
│   ├── model/searchStore.ts          # Persists city/sportType/viewport/page to localStorage;
│   │                                 # deviceCoords stays in-memory only
│   └── ui/VenueSearchPage.tsx        # page moved from component state to the store; page-reset
│                                     # effect fixed to distinguish the map's mount-settle report
│                                     # from a genuine viewport change (hadRestoredViewportRef)
└── [6 files with PageLoader call sites]:
    features/venue-management/venue/ui/VenueForm.tsx
    pages/my-bookings/ui/MyBookingsPage.tsx
    pages/owner-bookings/ui/OwnerBookingsPage.tsx
    pages/owner-dashboard/ui/OwnerDashboardPage.tsx
    pages/venue-detail/ui/VenueDetailPage.tsx
    pages/venues/ui/VenueSearchPage.tsx

frontend/tests/pages/VenueSearchReturn.test.tsx  # +2 tests, "pagination restore (013)" block
```

**Structure Decision**: Follows the existing Feature-Sliced Design layout - `ErrorBoundary` joins
the existing `app/providers/` (alongside `RequireAuth`), `PageLoader` joins `shared/ui/` (alongside
`dialog.tsx`, whose visual treatment it reuses), and the search-state changes stay inside the
existing `pages/venues/model/searchStore.ts` rather than introducing a new layer.

## Complexity Tracking

*No Constitution Check violations - table intentionally omitted.*
