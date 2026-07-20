# Tasks: Persistent session with idle-timeout auto-logout

**Input**: Design documents from `/specs/012-persistent-session-idle-timeout/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: No new automated test added - timer-driven UI verified manually per quickstart.md; the
pre-existing 39-test Vitest suite is the regression net for everything it already covered.

**Organization**: Tasks are grouped by user story (from spec.md). All tasks below are already
complete - this file documents the delivered work, the same close-out style used for specs
006-011.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Could have run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to `frontend/src/`

## Path Conventions

- Frontend only: `frontend/src/` - no backend changes (spec Assumptions)

---

## Phase 1: Setup (Shared Infrastructure)

No setup tasks - extends the existing, fully-scaffolded frontend; no new dependency (research.md
Decision 1).

---

## Phase 2: Foundational (Blocking Prerequisites)

No foundational phase - US1's own first task (the session-store rework) is what every later story
depends on; there is no separate cross-cutting prerequisite ahead of it.

---

## Phase 3: User Story 1 - A reload or a closed tab no longer signs me out (Priority: P1) 🎯 MVP

**Goal**: The session (access token, refresh token, user) persists to `localStorage` and is
renewed via `/auth/refresh` on app load, instead of being wiped by any page reload.

**Independent Test**: Sign in, reload the page, confirm still signed in with no login prompt.

### Implementation for User Story 1

- [x] T001 [US1] Rewrite `entities/session/model/store.ts` to read/write `localStorage`
      (`sportbook-session` key) instead of holding state in memory only, matching
      `shared/theme/model/store.ts`'s hand-rolled pattern (research.md Decision 1); `signIn` now
      takes `(accessToken, refreshToken, user)`; adds `updateStoredTokens` for post-renew updates
- [x] T002 [P] [US1] Add `features/auth/refresh/api/refresh.ts` -
      `POST /api/auth/refresh` wrapper, same `AuthResponse` shape as login/register (depends on T001
      for the type it feeds)
- [x] T003 [US1] Add `entities/session/model/useSessionBootstrap.ts` - on app mount, if a stored
      refresh token exists, exchanges it for a fresh pair; on failure, signs out (research.md
      Decision 2: stored `user` trusted optimistically, renew happens in the background) (depends
      on T001, T002)
- [x] T004 [US1] Wire `useSessionBootstrap()` into `app/App.tsx` (depends on T003)
- [x] T005 [P] [US1] Update `features/auth/login/ui/LoginForm.tsx` and
      `features/auth/register/ui/RegisterForm.tsx` to pass `data.refreshToken` into `signIn`'s new
      3-argument signature (depends on T001)

**Checkpoint**: A signed-in session survives a page reload; an unrenewable stored session falls
back to signed-out, same as before this feature.

---

## Phase 4: User Story 2 - An unattended, signed-in session ends itself (Priority: P1)

**Goal**: After 3 minutes of inactivity, a 30-second countdown warning appears (stay / log out
now); reaching zero signs the user out automatically, and every sign-out route revokes the
refresh token server-side.

**Independent Test**: Stop interacting with the page; confirm the warning appears after 3 minutes
with a live countdown; confirm both explicit choices and the zero-countdown case each end the
session correctly.

### Implementation for User Story 2

- [x] T006 [P] [US2] Add `features/auth/logout/api/logout.ts` - `POST /api/auth/logout` wrapper
- [x] T007 [US2] Add `features/auth/idle-logout/model/useIdleLogout.ts` - 3-minute idle timer,
      30-second countdown state, ref-mirrored activity listener to avoid a stale closure
      (research.md Decision 3), activity ignored while the countdown shows (research.md Decision
      4), `logoutNow` shared by both the zero-countdown path and the dialog's immediate-logout
      button, both calling T006's `logout` before `signOut()` (research.md Decision 6) (depends on
      T001, T006)
- [x] T008 [US2] Add `features/auth/idle-logout/ui/IdleLogoutDialog.tsx` - non-dismissible
      (Escape/outside-click suppressed) dialog with a large separate countdown digit and two
      buttons ("stay signed in", "log out now") (depends on T007)
- [x] T009 [US2] Wire `<IdleLogoutDialog />` into the authenticated layout in `app/App.tsx`
      (depends on T008)
- [x] T010 [US2] Update `features/auth/logout/model/useLogout.ts` (the existing manual sign-out)
      to also call `logout()` with the stored refresh token before clearing local state,
      tolerating a failed revoke call (research.md Decision 6) (depends on T006)
- [x] T011 [P] [US2] Add `auth.idleLogout.*` keys (title, message, stayButton, logoutButton) to
      all 4 locale files (`en`, `uk`, `pt`, `es`), including 4-form Ukrainian handling where the
      wording needed it

**Checkpoint**: An idle session always ends itself (automatically or by explicit choice); every
ending route leaves the refresh token unusable server-side.

---

## Phase 5: User Story 3 - Signing back in returns me to where I was (Priority: P2)

**Goal**: A user redirected to `/login` because their session ended lands back on their prior page
after signing in again, instead of always the default page.

**Independent Test**: Force a sign-out while on a non-default page; sign back in; confirm return
to that page. Confirm navigating to `/login` directly still lands on the default page.

### Implementation for User Story 3

- [x] T012 [US3] Update `app/providers/RequireAuth.tsx` to redirect with
      `state: { from: location }` (research.md Decision 5 - reuses React Router's standard
      redirect-state pattern rather than a separately-stored route)
- [x] T013 [US3] Update `features/auth/login/ui/LoginForm.tsx` to navigate to
      `location.state.from.pathname` when present, falling back to `/` otherwise (depends on T012)

**Checkpoint**: A forced sign-out/sign-in round trip returns to the original page; a voluntary
visit to `/login` still lands on the default page.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and end-to-end verification, spanning all three stories.

- [x] T014 [P] Add a "Staying signed in" section to `pages/about/ui/AboutPage.tsx` plus
      `about.sessionTitle`/`about.session` keys in all 4 locale files (spec FR-009)
- [x] T015 Run the full quickstart.md verification: `tsc -b` and `oxlint` clean, 39 Vitest tests
      green, manual walkthroughs of all three user stories against a live backend, refresh-token
      revocation confirmed in the `RefreshTokens` table

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** / **Foundational (Phase 2)**: empty
- **User Story 1 (Phase 3)**: no dependency on Setup/Foundational - T001 (the session-store
  rework) is itself the foundation every later story builds on
- **User Story 2 (Phase 4)**: depends on T001 (needs the persisted `refreshToken` to have
  something worth revoking, and `signOut`/`updateStoredTokens` to call)
- **User Story 3 (Phase 5)**: depends on T001/T005 (needs `LoginForm`'s post-sign-in navigation
  logic to already exist, to extend it with the `from` redirect)
- **Polish (Phase 6)**: depends on Phases 3-5 all being complete

### Parallel Opportunities

- US1: T002 and T005 (different files, both depend only on T001)
- US2: T006 and T011 (different files/concerns, independent of the T007-T010 chain)
- Polish: T014 (independent of T015, which is verification-only)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3: User Story 1 (persistence + renew-on-load)
2. **STOP and VALIDATE**: reload a signed-in session, confirm it survives
3. Demo: sign in, reload, still signed in

### Incremental Delivery (as actually shipped)

1. User Story 1 (persistence, renew-on-load) → validate → the core pain point resolved
2. User Story 2 (idle timeout, revoke-on-logout) → validate → closes the security gap User Story
   1 opened by making sessions outlive a tab
3. User Story 3 (return-to-page) → validate → quality-of-life follow-up, smaller and independent
4. Polish (About page documentation, full quickstart validation) → ready to commit

---

## Notes

- [P] tasks touch different files with no dependency on an incomplete task
- [Story] label maps each task to its spec.md user story for traceability
- This feature was implemented and verified before this tasks.md was written (spec.md
  Assumptions) - task order above reflects actual dependency structure, not a literal
  chronological log
