# Issue: Idempotent Confirmation and Replay Safety Tests

Parent Ticket: S2-T02
Issue ID: S2-T02-I02
Sprint: Sprint 2
Priority: Must Have
Type: Test
Status: Needs Clarification

## Goal
Prove that confirmation handling is idempotent and reconnect replay does not create duplicate grants.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T02)
- Docs/34 - Backend_Services_and_API_Contract.md
- Docs/37 - QA_Strategy_and_Test_Harness.md

## Requirements
- Handle duplicate and out-of-order confirmations safely.
- Replay pending queue after reconnect without grant duplication.
- Log stale/unknown confirmations via safe failure path.

## Acceptance Criteria
- Given duplicate confirmation responses, when processed, then only one terminal success is recorded.
- Given out-of-order confirmations, when replayed, then queue converges to correct state.
- Given unknown confirmation ID, when processed, then request is rejected safely and system remains consistent.

## Implementation Notes
- Use deterministic replay fixtures with controlled response ordering.
- Track applied grant IDs in assertions.
- Retry/backoff behavior specifics are pending product/engineering clarification.

## Tests Required
- Duplicate confirm idempotency test.
- Out-of-order confirmation replay test.
- Unknown confirmation safe-failure test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- Clarification needed for retry/backoff and terminal failure policy ownership.

## Non-Goals
- Full reconnect conflict taxonomy from Sprint 3.
