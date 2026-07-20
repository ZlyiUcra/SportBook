# Feature Specification: Colocate single-use DTOs into their owning Features folders

**Feature Branch**: `011-colocate-dtos`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "Colocate DTOs into their owning Features/<UseCase>/ folders to
complete the vertical-slice rework from spec 009. Currently,
backend/src/SportBook.Application/Dtos/ still holds every request/response record centrally
(AuthDtos.cs, BookingDtos.cs, CityDtos.cs, ReviewDtos.cs, VenueDtos.cs), grouped by resource
rather than by use case - a leftover from before the Features/ slice reorganization. Move every
DTO record that is used by exactly one action into that action's Features/<Resource>/<UseCase>/
folder, colocated with its Command/Query and Handler. DTOs genuinely used by two or more actions
stay in a shared location, mirroring spec 009's FR-006 treatment of genuinely-shared Services/
logic - they are not duplicated per-slice and not force-fit into a single action's folder. No
behavior change for any client - this is a pure code-organization move; wire shapes, field names,
and JSON structure stay byte-identical."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See an action's complete request/response shape without leaving its folder (Priority: P1)

A developer opens one action's `Features/<Resource>/<UseCase>/` folder to understand or change
what it accepts and returns. Today, the Command/Query and Handler live there, but the DTO records
defining the actual wire shape still live in a separate, multi-purpose `Dtos/` file grouped by
resource - so understanding one action still means opening a file that also defines several
unrelated actions' shapes, the same fragmentation spec 009 already solved for handler logic.

**Why this priority**: This is the entire content of this feature - completing spec 009's
colocation goal for the one artifact type it left out.

**Independent Test**: Pick any action whose request/response DTO is used by that action alone
(for example, "create a booking"); confirm its DTO record is defined in the same folder as its
Command/Query and Handler, with no separate file to open.

**Acceptance Scenarios**:

1. **Given** a developer opens the folder for an action whose DTO is single-use, **When** they
   look for that action's request or response shape, **Then** they find it defined in the same
   folder as the action's Command/Query and Handler - not in a separate, multi-purpose file.
2. **Given** a developer opens the folder for an action whose response shape is genuinely used by
   more than one action (for example, a booking's response shape, read by six different booking
   actions), **When** they look for that shape's definition, **Then** they find it in one shared,
   clearly-identifiable location - not duplicated across every action that uses it, and not
   arbitrarily assigned to only one of them.

---

### Edge Cases

- What happens to a DTO used by exactly one action today, if a future action starts reusing it?
  Out of scope for this feature - moving it back to a shared location at that point is a normal,
  small follow-up edit, not something this feature needs to anticipate.
- What happens to a DTO type that is only ever referenced from inside another DTO (for example, a
  small value type embedded in a larger response), rather than directly by a Handler? It is
  treated the same as any other DTO: single-use if only one action's response tree embeds it,
  shared if more than one does.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every DTO record used by exactly one action MUST be defined in that action's
  `Features/<Resource>/<UseCase>/` folder, not in a separate shared file.
- **FR-002**: Every DTO record genuinely used by two or more actions MUST remain in a single
  shared location, not be duplicated into each using action's folder and not be arbitrarily
  assigned to only one of them.
- **FR-003**: This feature MUST NOT change any HTTP endpoint's route, verb, response status code,
  request shape, response shape, or field name - the move is strictly a code-organization change.
- **FR-004**: The decision of whether a given DTO is single-use or shared MUST be based on actual
  current usage (how many actions reference it today), not on a guess about possible future reuse.

### Key Entities

- **Single-use DTO**: A request or response record referenced by exactly one action's Handler (or
  by that action's other DTOs). Moves into that action's `Features/<Resource>/<UseCase>/` folder.
- **Shared DTO**: A request or response record referenced by two or more actions. Stays in a
  single shared location, the same treatment spec 009's FR-006 already gives to genuinely-shared
  non-DTO logic.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of single-use DTO records (by the definition in FR-004) live in their owning
  action's `Features/<Resource>/<UseCase>/` folder; zero remain in a separate shared file.
- **SC-002**: Every existing automated check that exercised the backend before this move still
  passes afterward, with zero changes to what behavior those checks assert.
- **SC-003**: A developer picking any single action whose DTO is single-use can find its complete
  request/response shape, within one minute, in the same folder as that action's Command/Query
  and Handler - without opening a second file.

## Assumptions

- "Used by exactly one action" is determined by counting distinct `Features/<Resource>/<UseCase>/`
  Handler folders (plus the HTTP endpoint files that construct request bodies) that reference a
  given DTO type, not by counting individual lines of code.
- Shared DTOs' single location is the existing `Dtos/` folder, unchanged in name and purpose -
  this feature does not invent a new shared-DTO location, it only removes single-use DTOs from it.
- This feature covers `SportBook.Application`'s `Dtos/` folder only; DTOs, if any, that live
  outside that folder are out of scope.
