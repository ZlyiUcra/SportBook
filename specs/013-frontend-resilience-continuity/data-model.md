# Data Model: Frontend resilience and continuity polish

No backend entity, database schema, or migration is involved - all three pieces are frontend-only.
The "entities" below are the structural units spec.md's Key Entities section names, mapped onto
their real, shipped shape.

## Error fallback

**Represents**: The message and recovery action shown in place of the app's normal content when a
rendering error occurs.

**Real shape**: `app/providers/ErrorBoundary.tsx`'s render output when `state.hasError` is true - a
centered message (`common.somethingWentWrong`) and a single button (`common.reloadPage`) calling
`window.location.reload()`. No retry-without-reload option, no error detail shown to the user
(the error itself is logged via `console.error`).

## Page loading indicator

**Represents**: The single, consistent visual signal shown while any page is waiting on data.

**Real shape**: `shared/ui/page-loader.tsx`'s `PageLoader` component - a `fixed inset-0` overlay
(same Tailwind classes as `DialogOverlay`) with a centered `Loader2` spinner and the
`common.loading` text. Stateless, takes no props; each call site conditionally renders it in place
of its previous inline "Loading..." text (12 call sites across 6 files).

## Restorable search state

**Represents**: The subset of the venue search page's inputs that survives a page reload or an
SPA-internal venue-detail round trip.

**Real shape**: `pages/venues/model/searchStore.ts`'s `SearchState` - `city` (`City | null`),
`sportType` (`SportType | ''`), `viewport` (`SearchViewport | null` - center + zoom), and `page`
(`number`, new in this feature). Persisted to `localStorage` under the key
`sportbook-venue-search` on every setter call. `deviceCoords` (`DeviceCoords | null`) is the one
field in the same store deliberately excluded from persistence (research.md Decision 4) - it
stays `SearchState` member, but never reaches `writePersistedSearch`.

**Invariant**: A results page number that no longer corresponds to any actual result (fewer
venues than before) is clamped to the nearest valid page rather than shown as an empty list (spec
FR-006); a results page number restored via either path (reload or venue-detail return) is not
reset by the map's own mount-time viewport report confirming an already-known position (spec
FR-007, research.md Decision 5).
