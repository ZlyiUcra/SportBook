# Research: Persistent session with idle-timeout auto-logout

## Decision 1: Hand-rolled `localStorage` read/write, not zustand's `persist` middleware

**Decision**: `entities/session/model/store.ts` reads/writes `localStorage` directly
(`readStoredSession`/`writeStoredSession`), the same pattern `shared/theme/model/store.ts`
already used - not zustand's built-in `persist` middleware, even though it is available
(`zustand` 5.0.14 ships it) and would have worked.

**Rationale**: Matching an existing, established pattern in the codebase over introducing a
second one for the same job. The session also needs custom on-load behavior (renew via
`/auth/refresh` before trusting the stored value, not just rehydrate-and-trust) that `persist`'s
automatic rehydration doesn't provide out of the box - a hand-rolled read gives a single, explicit
place to reason about that distinction.

**Alternatives considered**: zustand `persist` middleware (rejected - inconsistent with the
existing theme-store precedent, and would still need custom logic layered on top for the
refresh-before-trust behavior, so it saves little).

## Decision 2: Optimistic trust of the stored access token, renewed in the background

**Decision**: On load, the stored `user` is trusted immediately (so `RequireAuth` does not bounce
to `/login` while a renew call is in flight), while `useSessionBootstrap` exchanges the stored
refresh token for a fresh pair in the background; only an explicit renew failure signs the user
out.

**Rationale**: Avoids a loading-gate/flicker for the common case (a reload minutes after the last
action, well within both the access-token and idle-timeout windows) at the cost of a narrow edge
case: any request fired between mount and the renew call resolving, if the stored access token
happened to already be expired, gets one 401 that self-corrects once the renew completes. Treated
as an acceptable simplification given how narrow the window is (only reachable when the token was
already expired at load time), rather than building a request-queue-during-refresh mechanism.

**Alternatives considered**: A loading gate that blocks all authenticated content until the renew
call resolves (rejected - adds a visible delay/flicker to the overwhelmingly common successful
case, to protect against a narrow edge case with a self-correcting failure mode).

## Decision 3: Idle detection via a ref-mirrored value, not the raw React state, inside the activity listener

**Decision**: `useIdleLogout` mirrors its `secondsLeft` state into a ref (`secondsLeftRef`) and
has the `window`-level activity listener read the ref, not the state variable, to decide whether
to reset the idle timer.

**Rationale**: The activity listener is attached once (empty-dependency-shaped effect, stabilized
via `useCallback`) for the lifetime of the mount, so a listener closing directly over the
`secondsLeft` state variable would only ever see its value from the render that created the
listener (a classic stale-closure bug) - it would never notice the warning had started showing.
The ref sidesteps this without re-attaching the listener on every countdown tick.

**Alternatives considered**: Re-attaching the activity listener on every `secondsLeft` change
(rejected - churns six `addEventListener`/`removeEventListener` pairs every second while the
countdown is visible, for no benefit over a ref read).

## Decision 4: Activity during the warning does not itself cancel it

**Decision**: While the countdown is showing, ordinary activity events (mouse movement, scroll)
do NOT reset the idle timer - only the dialog's explicit "stay signed in" button does.

**Rationale**: Directly specified during the feature's discussion - a passive mouse twitch
elsewhere on the page (for example, the pointer resting near the dialog) must not silently
dismiss a warning the user hasn't consciously acknowledged; an explicit choice is required either
way (stay, or log out now).

**Alternatives considered**: Any activity cancels the countdown, matching typical idle-timeout UX
elsewhere (rejected - not what was asked for this feature; would also make the "log out now"
choice pointless, since a user reaching for that button would first cancel the countdown by
moving the mouse to it).

## Decision 5: "Return to previous page" is the existing React Router `state.from` pattern, not a separate stored route

**Decision**: `RequireAuth` passes `state: { from: location }` on its redirect to `/login`;
`LoginForm` reads `location.state.from` after a successful sign-in and navigates there instead of
always to the default route. No route is separately persisted to `localStorage`.

**Rationale**: The browser's own URL already survives a plain page reload (React Router reads
`window.location` fresh on every mount) - once the session itself persists (User Story 1), the
common "still on the same page after reload" case is already handled with no additional code. A
separately-stored "last route" would only matter for the case this feature actually needs to
solve: landing back on the right page after being bounced to `/login` by an ended session, which
is exactly what the standard redirect-with-state pattern is for.

**Alternatives considered**: Continuously writing the current route to `localStorage` on every
navigation, restored on load (rejected - redundant with the URL bar during a live session, and
introduces a second source of truth that could disagree with the actual URL; the redirect-state
pattern already covers the only scenario where the URL itself is not enough).

## Decision 6: Manual and idle-triggered sign-out both revoke the refresh token server-side

**Decision**: `useLogout` (the existing manual sign-out) and `useIdleLogout`'s auto-logout path
both call the existing `POST /api/auth/logout` with the stored refresh token before clearing
local state, tolerating failure (network error, already-expired token) without blocking the local
sign-out.

**Rationale**: Before this feature, the refresh token was never persisted client-side, so there
was nothing meaningful to revoke on sign-out. Persisting it (User Story 1) creates a real
leftover-credential concern this feature must close, not a pre-existing gap it can ignore -
covered directly by spec FR-007/SC-002.

**Alternatives considered**: Leaving revocation for a later feature (rejected - persisting a
refresh token without ever revoking it on sign-out is a regression this feature itself
introduces, not a separate concern).
