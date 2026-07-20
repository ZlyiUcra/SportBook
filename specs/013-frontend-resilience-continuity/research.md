# Research: Frontend resilience and continuity polish

## Decision 1: Class-component error boundary, wrapped with `withTranslation`

**Decision**: `ErrorBoundary` is a `React.Component` using `static getDerivedStateFromError` +
`componentDidCatch`, wrapped in `withTranslation()` rather than using `useTranslation` directly.

**Rationale**: React has no hook-based error-boundary API as of React 19 - `componentDidCatch`
still requires a class component, and hooks are not usable inside a class component, hence
`withTranslation`'s HOC form instead of the hook form used everywhere else in the app.

**Alternatives considered**: A third-party error-boundary library (rejected - the class-component
pattern is a handful of lines, well-documented in React's own docs, not worth a dependency).

## Decision 2: One `PageLoader`, reusing the Dialog overlay's exact visual treatment

**Decision**: `shared/ui/page-loader.tsx` is a `fixed inset-0` full-viewport overlay with
`bg-black/10` + `backdrop-blur-xs`, matching `shared/ui/dialog.tsx`'s `DialogOverlay` classes
exactly, plus a centered `Loader2` (lucide-react) spinner and the existing `common.loading` text.

**Rationale**: Directly requested - the user described wanting it to look like the idle-timeout
countdown modal (spec 012). Reusing the same Tailwind classes (rather than introducing a second,
slightly different overlay treatment) keeps the two visually and technically identical, and
required no new dependency since `lucide-react` was already installed and already used elsewhere
in the app (`AppHeader`'s `Menu` icon, `dialog.tsx`'s `XIcon`).

**Alternatives considered**: A smaller, page-content-scoped loader (not covering the header/nav)
- considered, but the user's own comparison to the modal dialog (a full-viewport overlay) settled
on the simpler, single visual language instead.

## Decision 3: Search-state persistence via hand-rolled `localStorage`, not zustand `persist`

**Decision**: `pages/venues/model/searchStore.ts` reads/writes `localStorage` directly
(`readPersistedSearch`/`writePersistedSearch`), the same pattern already used by
`shared/theme/model/store.ts` and (spec 012) `entities/session/model/store.ts` - not zustand's
`persist` middleware.

**Rationale**: Consistency with the two other stores in the codebase that already made this same
choice, for the same reason each time (recorded in spec 012's research.md Decision 1) - no new
pattern introduced for a third store that does not need `persist`'s extra machinery.

**Alternatives considered**: zustand `persist` middleware (rejected - same reasoning as spec 012).

## Decision 4: Only `deviceCoords` stays excluded from persistence, not the whole store

**Decision**: Of the search store's fields, only `deviceCoords` (raw GPS from "near me") is
excluded from `localStorage`; `city`, `sportType`, `viewport`, and the new `page` field are all
persisted.

**Rationale**: The store's original in-memory-only design (specs 003/004 FR-006, spec 008 FR-005)
was protecting one specific thing - raw device location data reaching persistent storage - not a
blanket "nothing about search state may ever be persisted" rule. A selected city, a map camera
position derived from a city or from a (never-stored) device fix, a sport filter, and a page
number carry materially less sensitivity than the raw coordinate pair itself. Revisiting the
scope of the original constraint (not its substance) once its actual target is identified this
precisely is the same kind of correction spec 010 made for the mediator-library license concern.

**Alternatives considered**: Persisting nothing (status quo, rejected - directly what this
feature exists to fix); persisting everything including `deviceCoords` (rejected - this is
exactly the specific data the original constraint was protecting, and the constraint's underlying
concern is unchanged by this feature).

## Decision 5: A restored results page survives a venue-detail round trip, not only a page reload

**Decision**: The `page` field lives in the same in-memory Zustand store as `city`/`sportType`/
`viewport`, not as component-local state - so it is already restored across an SPA-internal
"open a venue, then return to search" round trip (via `<Link to="/">`, no page reload involved),
the same mechanism specs 004/008 already used for the other search inputs, in addition to
surviving an actual page reload via the new `localStorage` persistence.

**Rationale**: The user's own follow-up scenario (page a search list, open a venue visible on
page 2, return, expect to still be on page 2) demonstrated that "restoration" needed to cover both
paths uniformly - a user does not experience these as two different features.

**Gotcha found during implementation**: `MapView` reports its viewport once, unconditionally,
right after mounting (its own documented contract, `MapView.tsx`) - including when it mounts with
an already-restored camera (returning from venue detail, or a reload with a persisted viewport).
The results-page reset effect's first implementation could not tell this "settling at an already-
known position" report apart from a genuine subsequent pan/zoom, and reset a correctly-restored
page back to 1. Fixed by capturing, once per mount (before any report can arrive), whether a
`viewport` already existed (`hadRestoredViewportRef`); only the very first `viewportBounds` report
is exempted from resetting the page, and only when that ref is true - every subsequent report,
and every first report when no viewport was restored (a genuinely fresh search), still resets the
page as before. Two regression tests were added (`VenueSearchReturn.test.tsx`, "pagination
restore (013)") covering both the exempted and the still-resetting case; the fix was found because
the existing `T010` test in `VenueRadiusView.test.tsx` (whose mock `MapView` does not auto-fire a
mount-time report, unlike the real component) caught the first, overly-broad version of the fix
as a regression.
