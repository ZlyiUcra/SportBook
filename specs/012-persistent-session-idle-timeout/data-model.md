# Data Model: Persistent session with idle-timeout auto-logout

This feature adds no backend entity, database schema, or migration - it is a frontend-only
consumer of session capabilities (`/auth/refresh`, `/auth/logout`) that already existed before it.
The "entities" below are the two structural units spec.md's Key Entities section names, mapped
onto their real, shipped shape.

## Persisted session

**Represents**: The signed-in state kept available across a page reload or closed tab.

**Real shape**: A JSON object under the `localStorage` key `sportbook-session`
(`entities/session/model/store.ts`), holding `accessToken` (string), `refreshToken` (string), and
`user` (the existing `User` type - id, name, email, role, subscription tier, created-at). Read
once synchronously at module load (so the Zustand store's initial state is never a flash of
"signed out"); written on every `signIn` and cleared on every `signOut`; its `accessToken`/
`refreshToken` pair is overwritten (not merged field-by-field with the old pair) after every
successful `/auth/refresh` call, since the backend rotates the refresh token on every use.

**Invariant**: The stored `user`/`refreshToken` are never trusted as proof of a currently-valid
session on their own - `useSessionBootstrap` always attempts a renew on app load, and only a
successful renew (or the complete absence of a stored session) is treated as "signed in."

## Idle-timeout warning

**Represents**: The visible countdown shown after a period of no user activity.

**Real shape**: In-memory-only React state (`useIdleLogout`'s `secondsLeft: number | null`) - null
means no warning is showing; `30` down to `1` is the live countdown; reaching the equivalent of
`0` triggers sign-out rather than being observed as a rendered state. Two timers back it: a
single 3-minute `setTimeout` (the idle wait) that arms a 1-second `setInterval` (the countdown)
when it fires. Neither timer nor the warning state is persisted - a page reload resets the idle
clock, which is the correct behavior (a fresh page load is itself activity).

**Invariant**: The warning, once showing, is dismissed only by an explicit user choice (`stay` or
`logoutNow`) - never by passive activity elsewhere on the page (research.md Decision 4).
