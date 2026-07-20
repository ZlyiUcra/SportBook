# Implementation Plan: Reviews only after a completed, confirmed game

**Branch**: `006-reviews-after-completed` | **Date**: 2026-07-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/006-reviews-after-completed/spec.md`

## Summary

Make a review mean "someone actually played here". Three slices: (1) gate review submission on a
completed game - a customer may create or replace a venue review only if they have at least one
booking on a court of that venue whose status is Confirmed and whose end time is in the past; every
other case (browsing, pending, future, cancelled, or no booking) is rejected server-side with one
clear reason. This is the same read-time "Completed" derivation feature 005 already uses (Confirmed
+ past end), now reused as an eligibility predicate. (2) Move the review entry off the venue detail
page onto the customer's "My bookings": the submission form is removed from the venue page (the
review LIST stays there as social proof for prospective customers), and each completed booking on
"My bookings" offers an action leading to the review entry for that booking's venue, pre-filled with
the customer's existing review if any. (3) Replace the rating dropdown with an interactive five-star
widget - hover previews the value, clicking picks 1-5 proportional to the position in the row, and
it stays operable by keyboard and touch. Reviews remain optional; nothing forces or nags. No schema
change and no migration - the eligibility rule reads existing Booking/Court navigations. All product
choices were made by the user in the 2026-07-20 discussion and are recorded in the spec's
Assumptions. The "played but never confirmed" case is explicitly deferred to a later feature.

## Technical Context

**Language/Version**: C# 14 / .NET 10 backend; TypeScript 6.0 frontend - unchanged from 001-005.

**Primary Dependencies**: No new packages, backend or frontend. Reuses EF Core navigations
(Booking -> Court -> Venue), the injected `TimeProvider` already in `ReviewService`, the existing
review create-or-replace path, and the shadcn UI kit already present. The five-star widget is built
in-house from existing primitives (no rating-widget dependency added).

**Storage**: Same SQL Server / EF Core. NO schema change and NO migration - eligibility is an
`AnyAsync` existence check over the existing Booking table joined to Court; the Review shape is
unchanged.

**Testing**: xUnit as in 001-005. Unit tests assert the eligibility predicate accepts a
Confirmed-and-past booking on the venue's court and rejects Pending / future / cancelled / no
booking and a booking on a different venue's court, and that the existing rating validation still
fires independently of eligibility. Integration tests cover the gated endpoint: 201/200 for an
eligible customer (create then replace), 409 for an ineligible one, existing 400 for a bad rating,
and that the venue review LIST endpoint is unchanged. Frontend: Vitest + RTL for the venue page no
longer rendering the submission form (list still renders), the review action appearing only on
completed bookings on "My bookings", and the star widget (hover preview, click-to-value, keyboard).

**Target Platform**: Unchanged - ASP.NET Core service + React SPA.

**Project Type**: Web application (backend API + frontend SPA), unchanged.

**Performance Goals**: The eligibility check is a single bounded `EXISTS` subquery (one user, one
venue's courts) evaluated server-side on write; it adds one indexed existence probe to a submission
that already writes a row, so cost is flat. The venue page loses a query (no per-user "my review"
prefill fetch there); "My bookings" already loads the customer's bookings, so the review action
reads data it already has.

**Constraints**: The eligibility predicate MUST be SQL-translatable and evaluated server-side (the
client cannot be trusted to self-certify a completed game); it MUST reuse the same
Confirmed-and-past definition of "Completed" as 005 (no new stored status, 001 invariant preserved).
The gate MUST NOT weaken the existing rating validation (a bad rating is still rejected on its own).
The review submission MUST stay optional - no forced modal, no repeat prompting (spec FR-006). The
star widget MUST be operable by keyboard and touch, not pointer-only (spec FR-009). ASCII-only
source files per repo rules.

**Scale/Scope**: 1 eligibility predicate added to `ReviewService.CreateOrReplaceAsync` (+ one new
rejection reason `REVIEW_NOT_ELIGIBLE`), no new endpoint and no changed response shape; frontend
changes to remove the review form Card from the venue detail page (keep the list), add a review
action to completed rows on the customer "My bookings" page leading to the (relocated) review entry,
and replace the `<select>` rating in the review form with a five-star widget. i18n additions in
en/uk/pt (ineligibility reason, review-action label, star-control aria labels). No schema, no
migration, no new dependency.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled bootstrap template - no ratified
principles, so the gate trivially passes pre- and post-design (same status as 001-005). The
standing recommendation to run `/speckit-constitution` remains open; not a blocker.

## Project Structure

### Documentation (this feature)

```text
specs/006-reviews-after-completed/
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
│   │                                # now surfaces the new 409 REVIEW_NOT_ELIGIBLE from the service.
│   ├── SportBook.Application/      # ReviewService.CreateOrReplaceAsync: add the eligibility check
│   │                                # (a Confirmed-and-past booking on a court of this venue) before
│   │                                # create-or-replace; keep the existing rating validation and the
│   │                                # one-review-per-user-per-venue semantics. Reuses injected
│   │                                # TimeProvider for `now`. New error code REVIEW_NOT_ELIGIBLE.
│   ├── SportBook.Domain/           # unchanged (reuses Booking/Court/Venue navigations, BookingStatus)
│   └── SportBook.Infrastructure/   # unchanged (no migration)
└── tests/
    ├── SportBook.UnitTests/        # eligibility predicate: accept Confirmed+past on venue court;
    │                                # reject Pending/future/cancelled/none/other-venue; rating
    │                                # validation still fires independently.
    └── SportBook.IntegrationTests/ # gated endpoint: 201/200 eligible (create then replace),
                                     # 409 ineligible, 400 bad rating; review LIST endpoint unchanged.

frontend/
├── src/
│   ├── pages/
│   │   ├── venue-detail/           # remove the review submission Card (ReviewForm); KEEP the review
│   │   │                            # list display and its query.
│   │   └── my-bookings/            # completed (Confirmed, past) rows gain a "review" action leading
│   │                                # to the review entry for that booking's venue.
│   ├── features/
│   │   └── review/create/          # ReviewForm: replace the <select> rating with a five-star widget
│   │                                # (hover preview, proportional click, keyboard + touch); the form
│   │                                # is now reached from My bookings, pre-filled with any existing
│   │                                # review. Star widget is a local component (no new dependency).
│   └── shared/
│       └── i18n/                   # + ineligibility reason, review-action label, star aria labels
│                                    # in en/uk/pt
└── tests/                          # venue page has no form (list stays); review action only on
                                     # completed rows; star widget hover/click/keyboard
```

**Structure Decision**: Same two-project layout as 001-005. The whole eligibility rule is one
predicate added to the existing `ReviewService.CreateOrReplaceAsync` - no new endpoint, no changed
response DTO - so the API surface is unchanged apart from a new rejection code. On the frontend the
review submission moves from the venue-detail page to the customer `my-bookings` journey while the
venue page keeps rendering the review list; the star widget is a local component inside the existing
review-create feature slice, reused wherever the form is rendered.

## Complexity Tracking

Not applicable - Constitution Check has no gates to violate (constitution.md is unfilled).
