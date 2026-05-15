# Issue: Research Save Fields and Migration Fixtures

Parent Ticket: S2-T01
Issue ID: S2-T01-I02
Sprint: Sprint 2
Priority: Must Have
Type: Infrastructure
Status: Ready

## Goal
Persist research lifecycle state across save/load and ensure fixture-backed compatibility with current migration strategy.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T01)
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md
- Docs/28 - Save_Data_Model_Versioning_and_Migration.md

## Requirements
- Add required serialized research fields for all lifecycle states.
- Ensure load/save roundtrip preserves research data.
- Add migration fixture coverage for research state fields.

## Acceptance Criteria
- Given a save with Active research, when loaded and re-saved, then research fields remain consistent.
- Given a fixture without newly required research fields, when migration runs, then fallback/migration behavior is applied safely.
- Given malformed research payload, when load occurs, then safe failure/fallback path executes without corrupting global save.

## Implementation Notes
- Coordinate field naming with migration conventions.
- Add fixture cases for old/current/malformed payload variants.
- Keep changes limited to research-related save data.

## Tests Required
- Save/load roundtrip test for each research state.
- Pre-change fixture migration test.
- Malformed payload safe-handling test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- None.

## Non-Goals
- Research verification queue logic.
- On-screen pending status UI.
