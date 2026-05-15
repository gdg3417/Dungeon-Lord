# Issue: Research State Machine and Guards

Parent Ticket: S2-T01
Issue ID: S2-T01-I01
Sprint: Sprint 2
Priority: Must Have
Type: Feature
Status: Ready

## Goal
Implement a deterministic single-slot research lifecycle state machine with valid transition guards so research progression behavior is stable and enforceable.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T01)
- Docs/09 - Dungeon_Progression_and_Research.md
- Docs/30 - Formula_Framework_and_Modifier_Stacking_Rules.md

## Requirements
- Support Idle -> Active -> CompletedPendingVerification -> Confirmed/Claimed.
- Reject invalid transitions deterministically.
- Enforce one active research slot at a time.

## Acceptance Criteria
- Given an Idle state, when research start is requested, then state transitions to Active.
- Given Active state, when a second start request is made, then request is rejected with blocked status.
- Given invalid transition request, when processed, then state is unchanged and deterministic error is returned.

## Implementation Notes
- Keep transition table explicit (no implicit side effects).
- Use stable transition reason codes for diagnostics.
- This issue should not include persistence schema changes.

## Tests Required
- Transition map happy-path test.
- Single-slot exclusivity test.
- Invalid transition determinism test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- None.

## Non-Goals
- Save migration updates.
- UI messaging implementation.
