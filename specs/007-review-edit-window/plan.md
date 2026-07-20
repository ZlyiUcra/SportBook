# Implementation Plan: Review edit window and minimum edit comment length

**Branch**: `007-review-edit-window` | **Date**: 2026-07-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/007-review-edit-window/spec.md`

## Summary

Tighten how an existing venue review can be changed. Two slices: (1) a review is editable by its
author only within 24 hours of its original creation - the window is measured from the review's
`CreatedAt`, which is already set once at creation and never touched on replace (001/006), so the
"a replace never resets the clock" rule falls out for free; after the window a replace is rejected
server-side while the review keeps displaying to everyone. (2) When replacing an already-existing
review (inside the window), the comment must be present and at least 10 characters; a first-time
submission's comment stays optional (006 behaviour). Both rules live in the existing
`ReviewService.CreateOrReplaceAsync`, apply only on the replace branch (an existing review present),
and are independent of the 006 eligibility gate, which is unchanged. No schema change, no migration,
no DTO change (the frontend already receives `createdAt` to compute the window), no new dependency.
All product choices were confirmed by the user in the 2026-07-20 discussion and are recorded in the
spec's Assumptions.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript 6.0 frontend - unchanged from 001-006.

**Primary Dependencies**: No new packages, backend or frontend. Reuses the injected `TimeProvider`
already in `ReviewService`, the existing create-or-replace path, the review-create form's RHF+zod
setup, and the shadcn UI kit already present.

**Storage**: Same SQL Server / EF Core. NO schema change and NO migration - the 24-hour window is
computed from the existing `Review.CreatedAt`; the comment rule is a request-time validation. No
new column, table, or index.

**Testing**: xUnit as in 001-006. Unit tests assert the edit-window predicate (a replace inside 24h
of creation is allowed; a replace after 24h is rejected; a replace never advances the window) and
the edit-comment rule (empty/under-10 rejected on a replace; >=10 accepted; a first-time submission
with no comment still accepted), each independent of eligibility. Integration tests cover the gated
endpoint: replace within window OK, replace after a seeded old creation time rejected, empty/short
comment on a replace rejected, first-time no-comment accepted. Frontend: Vitest + RTL for the edit
form enforcing the min-length only in edit mode and for the edit action being unavailable once the
window has closed.

**Target Platform**: Unchanged - ASP.NET Core service + React SPA.

**Project Type**: Web application (backend API + frontend SPA), unchanged.

**Performance Goals**: Both rules are in-memory checks on data already loaded (the existing review
row and the request body) - no extra query, no extra round-trip. Cost is unchanged from 006.

**Constraints**: Both new rules MUST be enforced server-side (the client cannot be trusted to
self-limit its own edit window); the 24-hour window MUST be measured from the original `CreatedAt`
and MUST NOT be reset by a replace (guaranteed by never writing `CreatedAt` on the replace branch,
001/006 invariant); the rules apply ONLY to the replace branch (existing review present), never to a
first-time submission (spec FR-006); the two rejection reasons MUST be distinct error codes so a
caller can tell "window closed" from "comment too short" (spec SC-004); the 006 eligibility gate is
untouched and checked independently. ASCII-only source files per repo rules.

**Scale/Scope**: Two guard clauses added to `ReviewService.CreateOrReplaceAsync` (+ two new error
codes), no new endpoint and no changed response shape; frontend changes to enforce the edit-only
min-comment rule in the review form and to hide/disable the edit action once the window has closed,
plus surfacing the two new rejection messages. i18n additions in en/uk/pt. No schema, no migration,
no new dependency.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-006). The
standing recommendation to run `/speckit-constitution` remains open; not a blocker.

## Project Structure

### Documentation (this feature)

```text
specs/007-review-edit-window/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── SportBook.Api/              # ReviewsController: unchanged surface; the create/replace action
│   │                                # now surfaces the two new rejection codes from the service.
│   ├── SportBook.Application/      # ReviewService.CreateOrReplaceAsync: on the replace branch
│   │                                # (existing review present), reject when now is past
│   │                                # CreatedAt + 24h (REVIEW_EDIT_WINDOW_CLOSED) and when the
│   │                                # comment is missing/empty/under 10 chars
│   │                                # (REVIEW_COMMENT_TOO_SHORT). Reuses the injected TimeProvider.
│   │                                # The 006 eligibility gate and first-time-create path unchanged.
│   ├── SportBook.Domain/           # unchanged (Review.CreatedAt already exists, set once)
│   └── SportBook.Infrastructure/   # unchanged (no migration)
└── tests/
    ├── SportBook.UnitTests/        # edit-window predicate + edit-comment rule, each independent of
    │                                # eligibility; a replace never advances the window.
    └── SportBook.IntegrationTests/ # replace within window OK, replace after seeded-old CreatedAt
                                     # rejected, empty/short comment on replace rejected, first-time
                                     # no-comment accepted; the review LIST endpoint unchanged.

frontend/
├── src/
│   ├── features/
│   │   └── review/create/          # ReviewForm: enforce the min-10-char, non-empty comment only in
│   │                                # edit mode (an existing review is being replaced); a first-time
│   │                                # submission keeps the optional comment. schema/model updated.
│   ├── pages/
│   │   └── my-bookings/            # the review action: when the caller's existing review is older
│   │                                # than 24h, present it read-only (no edit) rather than an edit
│   │                                # form; surface the two new rejection messages via ApiRequestError.
│   └── shared/
│       └── i18n/                   # + edit-window-closed and comment-too-short messages in en/uk/pt
└── tests/                          # edit form enforces min length only when editing; edit action
                                     # unavailable once the window has closed
```

**Structure Decision**: Same two-project layout as 001-006. Both rules are guard clauses inside the
existing `ReviewService.CreateOrReplaceAsync`, on its replace branch only - no new endpoint, no
changed response DTO, so the API surface is unchanged apart from two new rejection codes. On the
frontend the review-create feature slice's form gains an edit-mode-only comment rule, and the
My-bookings review action (the only entry point to editing, per 006) decides edit-vs-read-only from
the existing review's `createdAt`. No backend field is added - `createdAt` already rides the review
response.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
