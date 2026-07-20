# Tasks: Frontend resilience and continuity polish

**Input**: Design documents from `/specs/013-frontend-resilience-continuity/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: 2 new regression tests added for the mount-settle-report bug (research.md Decision 5);
the pre-existing 39-test Vitest suite is the regression net for everything else.

**Organization**: Tasks are grouped by user story (from spec.md). All tasks below are already
complete - this file documents the delivered work, the same close-out style used for specs
006-012.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to `frontend/`

## Path Conventions

- Frontend only: `frontend/` - no backend changes (spec Assumptions)

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - extends the existing, fully-scaffolded frontend; no new dependency beyond an
already-installed icon package.

---

## Phase 2: Foundational (Blocking Prerequisites)

No foundational phase - the three user stories are independent pieces with no shared prerequisite
beyond the existing app shell.

---

## Phase 3: User Story 1 - A page-rendering error never shows a blank screen (Priority: P1) 🎯 MVP

**Goal**: A top-level error boundary replaces a blank white screen with a message and a reload
action.

**Independent Test**: Force a rendering error anywhere in the app; confirm a visible message and
a working recovery action appear instead of a blank page.

### Implementation for User Story 1

- [x] T001 [US1] Add `src/app/providers/ErrorBoundary.tsx` - class component
      (`getDerivedStateFromError`/`componentDidCatch`, React has no hook-based error-boundary API),
      wrapped with `withTranslation` since hooks aren't usable in a class component (research.md
      Decision 1)
- [x] T002 [US1] Wrap `<App />` with `<ErrorBoundary>` in `src/main.tsx` (depends on T001)
- [x] T003 [P] [US1] Add `common.somethingWentWrong`/`common.reloadPage` keys to all 4 locale
      files (`en`, `uk`, `pt`, `es`)

**Checkpoint**: An unhandled render error anywhere in the app shows a recovery message instead of
a blank screen.

---

## Phase 4: User Story 2 - Every page's loading moment looks and behaves the same (Priority: P2)

**Goal**: One universal `PageLoader` (full-viewport blurred overlay + spinner) replaces every
page's inconsistent inline "Loading..." text.

**Independent Test**: Trigger a data-loading moment on several different pages; confirm each
shows the same visually distinct, centered loading indicator.

### Implementation for User Story 2

- [x] T004 [P] [US2] Add `src/shared/ui/page-loader.tsx` - `fixed inset-0` overlay reusing
      `DialogOverlay`'s exact backdrop-blur treatment (research.md Decision 2), centered
      `Loader2` (lucide-react) spinner
- [x] T005 [US2] Replace all 12 `{t('common.loading')}` call sites (plain text and `Suspense`
      fallbacks) with `<PageLoader />` across `VenueForm.tsx`, `MyBookingsPage.tsx`,
      `OwnerBookingsPage.tsx` (x2), `OwnerDashboardPage.tsx` (x2), `VenueDetailPage.tsx` (x4),
      `VenueSearchPage.tsx` (x2) (depends on T004)

**Checkpoint**: Every page's loading moment renders the identical `PageLoader`, zero remaining
inline loading text.

---

## Phase 5: User Story 3 - Reloading the venue search page returns to the same search (Priority: P2)

**Goal**: The venue search page's search state (city, sport filter, map viewport, results page)
persists to `localStorage` and is restored on reload, excluding raw device GPS coordinates;
restoration also works, without a page reload, when returning from a venue's detail page.

**Independent Test**: Search for venues, pan/zoom, page to a later results page, reload; confirm
the same city/filter/position/page are restored. Separately, open a venue from page 2 and return;
confirm still on page 2.

### Implementation for User Story 3

- [x] T006 [US3] Rewrite `src/pages/venues/model/searchStore.ts` to read/write `localStorage`
      (key `sportbook-venue-search`) for `city`/`sportType`/`viewport`/`page`, matching
      `shared/theme/model/store.ts`'s hand-rolled pattern (research.md Decision 3);
      `deviceCoords` deliberately excluded (research.md Decision 4)
- [x] T007 [US3] Move `page` from `VenueSearchPage`'s local `React.useState` into the search
      store (`page`/`setPage`), so it is restored both via `localStorage` (reload) and via the
      pre-existing in-memory store surviving an SPA-internal venue-detail round trip (research.md
      Decision 5) (depends on T006)
- [x] T008 [US3] Add a page-number clamp: if a restored `page` exceeds `totalPages` once real
      results have loaded, reset to `totalPages` instead of showing an empty list (spec FR-006)
      (depends on T007)
- [x] T009 [US3] Fix the results-page reset effect in `VenueSearchPage.tsx`: distinguish
      `MapView`'s contractual once-on-mount viewport report (which merely confirms an
      already-restored camera) from a genuine pan/zoom, via `hadRestoredViewportRef` captured
      once before any report can arrive - only a report that is NOT the mount-settle case resets
      the page (spec FR-007, research.md Decision 5 gotcha; found via a real-world scenario -
      returning from a venue on results page 2 was incorrectly bouncing back to page 1) (depends
      on T007)
- [x] T010 [P] [US3] Add 2 regression tests to `tests/pages/VenueSearchReturn.test.tsx`
      ("pagination restore (013)" describe block): a restored page survives the mount-settle
      report, and a genuinely fresh search (no restored viewport) still resets to page 1 on its
      first report (depends on T009)

**Checkpoint**: A reload and a venue-detail round trip both restore city/filter/viewport/page
identically; only a genuine viewport change (not the map's own mount-time confirmation) resets
pagination; raw device GPS never reaches `localStorage`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T011 Run the full quickstart.md verification: `tsc -b` and `oxlint` clean, 41 Vitest tests
      green (39 pre-existing + 2 new), manual walkthroughs of all three user stories including the
      venue-detail-return pagination case

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** / **Foundational (Phase 2)**: empty
- **User Story 1 (Phase 3)**: no dependency on Setup/Foundational
- **User Story 2 (Phase 4)**: no dependency on User Story 1
- **User Story 3 (Phase 5)**: no dependency on User Stories 1 or 2 - all three are independent
  pieces bundled into one feature
- **Polish (Phase 6)**: depends on Phases 3-5 all being complete

### Parallel Opportunities

- User Stories 1, 2, and 3 are mutually independent and could have proceeded in parallel
- Within US1: T003 is independent of T001/T002
- Within US2: T004 before T005 (T005 needs the component to exist)
- Within US3: T006 → T007 → T008/T009 → T010 is a real dependency chain (each builds on the
  previous), not parallelizable within the story

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: User Story 1 (error boundary)
2. **STOP and VALIDATE**: force a render error, confirm the fallback shows
3. Demo: a broken page shows a recovery message, not a blank screen

### Incremental Delivery (as actually shipped)

1. User Story 1 (error boundary) → validate → the highest-value, lowest-effort fix
2. User Story 2 (universal page loader) → validate → consistency polish
3. User Story 3 (persisted search state) → validate, including the mount-settle-report bug found
   and fixed mid-story via a real usage scenario → ready to commit

---

## Notes

- [P] tasks touch different files with no dependency on an incomplete task
- [Story] label maps each task to its spec.md user story for traceability
- T009's bug and fix were discovered AFTER T007/T008 were initially believed complete, via a
  specific real-world scenario description rather than automated test failure - T010's regression
  tests close that gap for the future
