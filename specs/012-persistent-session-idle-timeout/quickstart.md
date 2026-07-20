# Quickstart: Persistent session with idle-timeout auto-logout

End-to-end validation that the feature behaves as specified. Frontend-only; no backend changes to
validate.

## Prerequisites

- Backend running (`dotnet run --project backend/src/SportBook.Api`) - this feature calls the
  real `/auth/refresh` and `/auth/logout` endpoints.
- Frontend dev server running (`yarn dev` from `frontend/`).

## Automated checks

```powershell
cd frontend
npx tsc -b
npx oxlint
npx vitest run
```

EXPECT: clean type-check, clean lint (only the pre-existing `button.tsx` Fast Refresh warning,
unrelated to this feature), 39 tests green - the same count as before this feature, since it adds
no new automated test (timer-driven UI, verified manually below).

## Manual verification (US1 - reload survives)

1. Sign in with valid credentials.
2. Reload the page. EXPECT: still signed in, no login prompt.
3. Inspect `localStorage['sportbook-session']` in devtools. EXPECT: a JSON object with
   `accessToken`, `refreshToken`, and `user`.
4. Manually corrupt or delete the stored `refreshToken` value, then reload. EXPECT: signed out,
   redirected to `/login` (simulates an unrenewable stored session).

## Manual verification (US2 - idle timeout)

1. Sign in, then stop interacting with the page entirely.
2. Wait 3 minutes. EXPECT: a dialog appears titled "Ви ще тут?" (or the active language's
   equivalent) with a countdown starting at 30 and a large, separately-displayed number.
3. Click "Залишитися в системі" (stay). EXPECT: dialog closes; waiting another 3 minutes
   reproduces step 2 (the idle timer restarted, not merely paused).
4. Repeat to reach the dialog again; this time click "Вийти зараз" (log out now). EXPECT:
   immediate sign-out, redirected to `/login`, without waiting for the countdown.
5. Repeat once more; this time take no action until the countdown reaches 0. EXPECT: automatic
   sign-out, redirected to `/login`.
6. After any of steps 4-5, inspect the backend's `RefreshTokens` table for that session's token
   row. EXPECT: `RevokedAt` is set (confirms the server-side revoke call fired).

## Manual verification (US3 - return to page)

1. Sign in, navigate to a non-default authenticated page (for example `/bookings`).
2. Trigger a forced sign-out (idle-timeout "log out now" is the fastest repeatable way).
3. Sign back in. EXPECT: landed back on `/bookings`, not the default `/` page.
4. Separately, sign out manually via the header's sign-out button, then sign back in from
   `/login` reached by navigating there directly (not via a redirect). EXPECT: landed on the
   default `/` page, confirming the redirect-state pattern does not fire when it shouldn't
   (spec Acceptance Scenario 2 of User Story 3).

## About page

Visit `/about` in each of the four supported languages and confirm the "Staying signed in" /
"Збереження входу в систему" / "Manter a sessão iniciada" / "Mantener la sesión iniciada" section
renders with the expected translated text.
