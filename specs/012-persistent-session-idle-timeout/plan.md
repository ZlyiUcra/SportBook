# Implementation Plan: Persistent session with idle-timeout auto-logout

**Branch**: `012-persistent-session-idle-timeout` | **Date**: 2026-07-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/012-persistent-session-idle-timeout/spec.md`

**Note**: This plan documents a feature already implemented and verified (see spec.md
Assumptions) - it records the technical approach actually taken, not a forward-looking design.

## Summary

Persist the frontend's session (access token, refresh token, user) to `localStorage` instead of
keeping it in memory only, with an app-load bootstrap that renews it via the existing
`POST /api/auth/refresh` endpoint before trusting it. Add a 3-minute idle-activity timer that
shows a 30-second countdown warning (stay / log out now) before auto sign-out, wired to also call
the existing `POST /api/auth/logout` endpoint so the persisted refresh token is revoked
server-side. Remember the pre-redirect location so a forced re-login returns the user to it.

## Technical Context

**Language/Version**: TypeScript / React 19, existing frontend stack

**Primary Dependencies**: None new - built entirely on already-installed packages (`zustand`,
`react-router-dom`, `react-i18next`, the existing `axiosInstance`, the existing `shared/ui/dialog`
Radix-based component)

**Storage**: Browser `localStorage` (client-side only) - no database/schema involvement; the
backend's existing refresh-token table is unchanged and only reached through its existing
`/auth/refresh`/`/auth/logout` endpoints

**Testing**: Vitest (existing frontend suite) - no new automated test added for this feature
(timer-driven UI, verified manually per quickstart.md); `tsc -b` and `oxlint` as the compile/lint
gate

**Target Platform**: Browser (existing SPA, unchanged deployment)

**Project Type**: Web application frontend (no backend change - reuses two endpoints that already
existed before this feature)

**Performance Goals**: None new - a handful of DOM event listeners and two timers per authenticated
session, negligible against existing page workload

**Constraints**: No new npm dependency (spec discussion resolved this before implementation);
no backend change (both endpoints used already existed); refresh-token rotation must be respected
(every renew response carries a new refresh token that must overwrite the stored one, not just
the access token)

**Scale/Scope**: One entity (`entities/session`) reworked, one new feature slice
(`features/auth/idle-logout`), two new API call wrappers (`features/auth/refresh`,
`features/auth/logout/api`), three existing files touched for the return-to-page behavior
(`RequireAuth`, `LoginForm`, and the route it protects), four locale files, one About-page section

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-011).

## Project Structure

### Documentation (this feature)

```text
specs/012-persistent-session-idle-timeout/
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
├── entities/session/model/
│   ├── store.ts                    # Session state - now reads/writes localStorage directly
│   │                                # (matching shared/theme/model/store.ts's hand-rolled
│   │                                # pattern, not zustand's `persist` middleware)
│   └── useSessionBootstrap.ts      # NEW - renews a stored session via /auth/refresh on app mount
├── features/auth/
│   ├── refresh/api/refresh.ts      # NEW - POST /api/auth/refresh wrapper
│   ├── logout/
│   │   ├── api/logout.ts           # NEW - POST /api/auth/logout wrapper
│   │   └── model/useLogout.ts      # Now also revokes the refresh token server-side
│   ├── idle-logout/                # NEW feature slice
│   │   ├── model/useIdleLogout.ts  # Activity tracking, idle timer, 30s countdown, logoutNow
│   │   └── ui/IdleLogoutDialog.tsx # Warning dialog (stay / log out now), big countdown digit
│   ├── login/ui/LoginForm.tsx      # Passes refreshToken to signIn; returns to location.state.from
│   └── register/ui/RegisterForm.tsx # Passes refreshToken to signIn
├── app/
│   ├── App.tsx                     # Mounts useSessionBootstrap() and <IdleLogoutDialog />
│   └── providers/RequireAuth.tsx   # Redirects to /login with state: { from: location }
├── pages/about/ui/AboutPage.tsx    # New "Staying signed in" section
└── shared/i18n/locales/*.json      # New auth.idleLogout.* and about.session* keys (4 languages)
```

**Structure Decision**: Follows the existing Feature-Sliced Design layout without introducing a
new layer - the idle-timeout logic is a `features/auth/` slice like `login`/`register`/`logout`
already were; session persistence stays inside `entities/session`, matching where the session
already lived.

## Complexity Tracking

*No Constitution Check violations - table intentionally omitted.*
