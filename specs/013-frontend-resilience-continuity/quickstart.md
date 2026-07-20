# Quickstart: Frontend resilience and continuity polish

End-to-end validation that the feature behaves as specified. Frontend-only; no backend changes to
validate.

## Prerequisites

- Backend running (`dotnet run --project backend/src/SportBook.Api`) - the venue search story
  needs real venue data.
- Frontend dev server running (`yarn dev` from `frontend/`).

## Automated checks

```powershell
cd frontend
npx tsc -b
npx oxlint
npx vitest run
```

EXPECT: clean type-check, clean lint (only the pre-existing `button.tsx` Fast Refresh warning),
41 tests green (39 pre-existing + 2 new regression tests for the mount-settle-report fix).

## Manual verification (US1 - error boundary)

1. Temporarily throw an error inside any page component's render (for example, `throw new
   Error('test')` at the top of a page component) and reload.
2. EXPECT: a centered "Something went wrong." message with a "Reload page" button, not a blank
   screen.
3. Click the button. EXPECT: the app reloads and returns to normal.
4. Revert the temporary throw.

## Manual verification (US2 - page loader)

1. Visit any page that fetches data (for example `/bookings`) with network throttled enough to
   observe the loading moment.
2. EXPECT: a full-viewport blurred overlay with a centered spinner, identical in appearance to
   every other page's loading moment and to the idle-timeout warning dialog's backdrop treatment.

## Manual verification (US3 - persisted search + return-to-search)

1. On the venues page, pick a city, apply a sport filter, pan/zoom the map, and navigate to
   results page 2 (requires 11+ results in view - use a city/radius with enough venues).
2. Reload the page. EXPECT: same city, filter, map position, and page 2 all restored - no empty
   "pick a reference point" prompt.
3. Inspect `localStorage['sportbook-venue-search']` in devtools. EXPECT: a JSON object with
   `city`, `sportType`, `viewport`, `page` - and no `deviceCoords`/GPS data anywhere in it.
4. From page 2, click into a venue's detail page, then click "back to search". EXPECT: still on
   page 2, same city/filter/map position - not bounced back to page 1 or the initial zoom (spec
   FR-007, the mount-settle-report fix).
5. From page 2, actually pan or zoom the map. EXPECT: this time it DOES reset to page 1 (a
   genuine viewport change, unlike step 4's mount-time settle).
6. Use "near me" once, then reload. EXPECT: the device-derived reference point does not silently
   reappear from storage - only the previously-explained persisted fields do.
