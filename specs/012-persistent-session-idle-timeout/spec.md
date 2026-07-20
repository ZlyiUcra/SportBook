# Feature Specification: Persistent session with idle-timeout auto-logout

**Feature Branch**: `012-persistent-session-idle-timeout`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "Persistent session with idle-timeout auto-logout. Already implemented
and verified on the frontend. Replaces the prior in-memory-only session (a page reload or tab close
always signed the user out) with a session persisted to localStorage (access token, refresh token,
user) - on app load, if a stored session exists, the app exchanges the stored refresh token for a
fresh access/refresh token pair via the existing refresh endpoint, since the stored access token may
already be expired. For security, since a session can now outlive a single browser tab, the app
tracks user activity; after 3 minutes with no activity, a dialog appears with a 30-second countdown
to automatic sign-out, offering two explicit choices - stay signed in (cancels the countdown and
resets the idle timer) or log out now (immediate sign-out). Reaching zero with no response signs the
user out automatically. Both automatic and manual sign-out now also revoke the refresh token
server-side, since a persisted refresh token left valid server-side after sign-out would be a real
credential leftover. Being redirected to the login page for lacking a session now remembers the page
the user was on, so a successful subsequent sign-in returns them there instead of always landing on
the default page. The About page documents this behavior in all four supported languages. No backend
changes - the feature only calls the already-existing refresh and logout endpoints."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A reload or a closed tab no longer signs me out (Priority: P1)

A signed-in user reloads the page (or the browser reloads it for them, such as during active
development) or closes and reopens the tab. Previously this always ended their session, forcing a
fresh login regardless of how recently they had been active.

**Why this priority**: This is the core value of the feature - the specific pain point that
prompted it, and everything else (idle timeout, return-to-page) exists to make persisting the
session safe and complete, not to replace this outcome.

**Independent Test**: Sign in, reload the page, and confirm the user is still signed in and sees
the same authenticated area they were in before the reload, without being asked to log in again.

**Acceptance Scenarios**:

1. **Given** a signed-in user, **When** they reload the page shortly after their last action,
   **Then** they remain signed in and see the authenticated area, not a login prompt.
2. **Given** a signed-in user whose short-lived access credential has since expired but who has
   remained within the idle-timeout window, **When** they reload the page, **Then** the app
   silently obtains a fresh credential and they remain signed in, with no visible interruption.
3. **Given** a user whose stored session can no longer be renewed (its long-lived credential has
   itself expired or been invalidated), **When** they reload the page, **Then** they are signed out
   and see the login page, the same outcome as before this feature existed.

---

### User Story 2 - An unattended, signed-in session ends itself (Priority: P1)

Because a session can now outlive a single browser tab, a user who steps away from a signed-in
device leaves a real window open for someone else to act as them. After a period with no activity,
the user is warned with a visible countdown and given an explicit choice; if nobody responds, the
session ends on its own.

**Why this priority**: This is the safety condition that makes User Story 1 acceptable - without
it, persisting the session would trade a minor inconvenience (repeated logins) for an unbounded
security exposure. Equal priority to Story 1 because neither is complete without the other.

**Independent Test**: Sign in, stop all interaction with the page, and confirm a warning appears
after the inactivity threshold with a visible countdown; confirm that taking no action signs the
user out automatically once the countdown reaches zero, and that choosing to stay cancels it.

**Acceptance Scenarios**:

1. **Given** a signed-in user who stops interacting with the page, **When** the inactivity
   threshold is reached, **Then** a warning appears showing a live countdown to automatic sign-out.
2. **Given** the warning is showing, **When** the user explicitly chooses to stay signed in,
   **Then** the countdown is cancelled and the inactivity threshold begins counting again from
   zero.
3. **Given** the warning is showing, **When** the user explicitly chooses to log out immediately,
   **Then** their session ends right away, without waiting for the countdown.
4. **Given** the warning is showing, **When** the countdown reaches zero with no response,
   **Then** the session ends automatically and the user sees the login page.
5. **Given** a session that has just ended (by any of the above three routes), **When** the
   session's long-lived credential is later inspected, **Then** it is no longer usable - ending a
   session invalidates it everywhere, not only on the device that ended it.

---

### User Story 3 - Signing back in returns me to where I was (Priority: P2)

A user who is sent to the login page because their session is no longer valid (rather than by
their own choice) wants to land back on the specific page they were using once they sign in again,
not always the app's default starting page.

**Why this priority**: A real quality-of-life improvement made necessary by the other two stories
- once sessions end automatically (idle timeout) or silently fail to renew (an unrenewable stored
session), losing your place becomes a real, recurring annoyance rather than a one-off.

**Independent Test**: While signed in, navigate to a specific non-default page; force a sign-out
(for example, via the idle-timeout warning's immediate-logout choice); sign back in and confirm
the app returns to that same page rather than the default one.

**Acceptance Scenarios**:

1. **Given** a user is sent to the login page because their session ended while they were on a
   specific page, **When** they sign in again, **Then** they land back on that same page.
2. **Given** a user navigates to the login page on their own (not because of a session ending),
   **When** they sign in, **Then** they land on the app's default starting page, as before this
   feature.

---

### Edge Cases

- What happens if the countdown reaches zero while the underlying long-lived credential has
  already become invalid for an unrelated reason? The sign-out proceeds the same way either way -
  the user ends up signed out and sees the login page, with no error state visible to them.
- What happens to the revoke-on-sign-out call (User Story 2, scenario 5) if the device is offline
  at that moment? The user's own device still signs out locally either way; the leftover
  server-side credential is a narrower, secondary concern than the user's own local session ending
  correctly, and is not something this feature blocks the local sign-out on.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST keep a user signed in across a page reload or a closed-and-reopened
  browser tab, for as long as their session remains valid.
- **FR-002**: On the first page load of a browser session where a previously-stored session
  exists, the system MUST attempt to renew it before treating the user as signed in, rather than
  trusting a potentially-expired stored credential at face value.
- **FR-003**: If a stored session cannot be renewed, the system MUST treat the user as signed out,
  the same outcome as if no session had ever been stored.
- **FR-004**: The system MUST track user activity while signed in and, after 3 minutes with no
  activity, MUST warn the user with a visible countdown before ending their session.
- **FR-005**: The warning MUST offer the user two explicit choices: stay signed in (cancelling the
  countdown) or log out immediately (ending the session right away).
- **FR-006**: If neither choice is made before the countdown (30 seconds) reaches zero, the system
  MUST end the session automatically.
- **FR-007**: Ending a session - whether by the user's own explicit choice, by the idle-timeout
  warning's immediate-logout choice, or by the countdown reaching zero - MUST invalidate that
  session's long-lived credential everywhere it could be used, not only on the device that ended
  it.
- **FR-008**: When a user is sent to the login page because their session is no longer valid
  (rather than by navigating there themselves), the system MUST return them to the page they were
  on after they sign in again.
- **FR-009**: The system MUST document this session behavior (persistence and automatic
  inactivity sign-out) in the application's own public-facing help content, in every language the
  application supports.

### Key Entities

- **Persisted session**: The signed-in state (identity plus the short-lived and long-lived
  credentials needed to keep it renewed) kept available across a page reload or closed tab, until
  it is explicitly or automatically ended.
- **Idle-timeout warning**: A visible countdown shown after a period of no user activity,
  requiring an explicit response (stay, or log out now) before it reaches zero.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user who reloads the page within the inactivity threshold of their last action
  never sees a login prompt they did not cause themselves.
- **SC-002**: 100% of session-ending events (manual, idle-timeout-immediate, or idle-timeout-
  automatic) leave the session's long-lived credential unusable afterward - none can be reused.
- **SC-003**: A user who takes no action for the full inactivity threshold plus the warning
  countdown is always signed out automatically - zero cases of an abandoned, still-active session
  persisting indefinitely.
- **SC-004**: A user redirected to login by their session ending, who then signs back in, lands on
  their prior page in 100% of cases; a user who navigates to login on their own lands on the
  default page, unchanged from before this feature.

## Assumptions

- This specification documents a feature already implemented and verified on the frontend before
  this spec was written - it captures the agreed intent and observed outcome, the same
  documentation-after-the-fact pattern used for specs 006-011.
- "Session" throughout this spec means the combination of the user's identity and the credentials
  that keep them signed in - the specific mechanism (tokens, cookies, or otherwise) is an
  implementation detail out of scope for this document.
- The specific inactivity threshold (3 minutes) and countdown length (30 seconds) are the values
  agreed for this feature; changing them later is a parameter tweak, not a re-specification.
- No backend change is required or included - this feature is a frontend consumer of session
  capabilities (renew, revoke) that already existed on the backend before this feature.
