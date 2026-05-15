# Sprint 2 Execution Order

## Parent Tickets Covered
- S2-T01 through S2-T07 from `docs/planning/sprint-2-ticket-backlog.md`.

## Sprint Readiness Gate (Must Pass Before Feature Start)
1. Confirm Sprint 1 closeout evidence is complete for UAT-01 through UAT-05 and linked.
2. If incomplete, all Sprint 2 feature issues remain blocked.

## Recommended Sequencing

### Phase 0: Gate Validation
1. Validate Sprint 1 evidence gate (planning/QA check).

### Phase 1: Foundation State and Queue
2. S2-T02-I01 - Verification Intent Model and Queue Lifecycle.
3. S2-T01-I01 - Research State Machine and Guards.
4. S2-T01-I02 - Research Save Fields and Migration Fixtures.
5. S2-T02-I02 - Idempotent Confirmation and Replay Safety Tests (starts once queue lifecycle exists).

### Phase 2: Core Simulation Domains
6. S2-T05-I01 - Encounter Contracts and Seeded Resolution.
7. S2-T03-I01 - Heat Core Rules and Boundary Enforcement.
8. S2-T06-I01 - Loot Table Resolution and Validation.
9. S2-T05-I02 - Encounter Event Emission for Downstream Systems.
10. S2-T06-I02 - Inventory Handoff and Loot Lifecycle Updates.

### Phase 3: Orchestration and Player-Facing Trust Surfaces
11. S2-T04-I01 - Offline Orchestrator Order and Cap Handling.
12. S2-T07-I02 - Localization Keys and Coverage Checks.
13. S2-T07-I01 - Pending/Restricted State UI Bindings.

## Parallelization Plan

### Can run in parallel
- S2-T02-I01 and S2-T01-I01 (different domains; coordinate contracts).
- S2-T05-I01 and S2-T03-I01 once queue/research foundations are stable.
- S2-T06-I01 can run in parallel with S2-T05-I01, but integration waits on encounter event schema.
- S2-T07-I02 can begin once message state contract is drafted, before UI final wiring.

### Must wait dependencies
- S2-T01-I02 waits for S2-T01-I01 state field definition.
- S2-T02-I02 waits for S2-T02-I01.
- S2-T05-I02 waits for S2-T05-I01.
- S2-T06-I02 waits for S2-T06-I01 and event schema from S2-T05-I02.
- S2-T04-I01 waits for initial outputs from S2-T01-I01 and S2-T03-I01.
- S2-T07-I01 waits for core state/event outputs from S2-T02-I01, S2-T05-I02, and S2-T06-I02.

## Minimum Vertical Slice (Proof of Loop)
A minimal Sprint 2 proof loop requires:
1. S2-T01-I01 (research lifecycle),
2. S2-T02-I01 (verification queue baseline),
3. S2-T05-I01 + S2-T05-I02 (encounter resolve + event output),
4. S2-T06-I01 + S2-T06-I02 (loot resolve + inventory handoff),
5. S2-T03-I01 (heat response),
6. S2-T07-I01 (player-visible pending/restricted/outcome states).

## Sprint 2A / 2B Recommendation
If capacity is constrained, split execution:

### Sprint 2A (must-have playable core)
- S2-T01-I01, S2-T01-I02
- S2-T02-I01
- S2-T05-I01, S2-T05-I02
- S2-T06-I01, S2-T06-I02
- S2-T03-I01 (with provisional thresholds if approved)
- S2-T07-I01 (minimum state visibility)

### Sprint 2B (stabilization and guardrail depth)
- S2-T02-I02
- S2-T04-I01
- S2-T07-I02
- Spillover fixes from 2A integration/testing
