# Issue: Offline Orchestrator Order and Cap Handling

Parent Ticket: S2-T04
Issue ID: S2-T04-I01
Sprint: Sprint 2
Priority: Must Have
Type: Infrastructure
Status: Ready

## Goal
Build the offline progression orchestration order for mana, heat, and research with deterministic elapsed-time cap handling.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T04)
- Docs/01 - Mana System v3.md
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md
- Docs/29 - Time_Model_and_Tick_Resolution.md

## Requirements
- Enforce explicit stage order for mana/heat/research offline updates.
- Apply elapsed-time cap behavior consistently.
- Preserve offline heat rebound constraints within current tier.

## Acceptance Criteria
- Given equal snapshots and elapsed time, when orchestrator runs twice, then outputs are identical.
- Given elapsed time above cap, when processed, then capped output is applied with cap signal.
- Given invalid elapsed input, when processed, then safe behavior prevents corrupted state.

## Implementation Notes
- Keep orchestration pipeline centralized.
- Use deterministic stage invocation order.
- Integrate with existing time anomaly indicators.

## Tests Required
- Equal-input deterministic orchestration test.
- Cap-boundary handling test.
- Invalid elapsed safe-handling test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- None.

## Non-Goals
- Performance profiling (Sprint 3 scope).
- Reconciliation conflict handling (Sprint 3 scope).
