# Issue: Localization Keys and Coverage Checks for Sprint 2 States

Parent Ticket: S2-T07
Issue ID: S2-T07-I02
Sprint: Sprint 2
Priority: Must Have
Type: Content
Status: Ready

## Goal
Route all newly exposed Sprint 2 player-facing state text through localization keys and validate key coverage.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T07)
- Docs/27 - Localization_and_Text_System.md
- Docs/35 - Error_Handling_Player_Messaging_and_Trust.md

## Requirements
- Add localization keys for pending/restricted/outcome messages.
- Remove hardcoded player-facing strings in new Sprint 2 surfaces.
- Add/execute key coverage checks for new keys.

## Acceptance Criteria
- Given a Sprint 2 state message, when rendered, then text resolves from localization key path.
- Given missing key, when UI renders, then safe fallback behavior occurs and coverage check fails appropriately.
- Given invalid key mapping, when validation runs, then error is surfaced with actionable key reference.

## Implementation Notes
- Coordinate key naming with existing conventions.
- Keep fallback behavior consistent with trust/error standards.
- Can run in parallel with UI bindings after agreed key contract.

## Tests Required
- Key resolution test for each new state message group.
- Missing key fallback + validation fail test.
- Invalid mapping detection test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- None.

## Non-Goals
- New language packs beyond current scope.
- Broad localization framework redesign.
