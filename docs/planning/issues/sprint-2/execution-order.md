# Sprint 2 Execution Order

## Parent Tickets Covered
- S2-T00, S2-T00A, and S2-T01 through S2-T07 from `docs/planning/sprint-2-ticket-backlog.md`.

## Sprint Readiness Gate (Must Pass Before Feature Start)
1. Confirm Sprint 1 closeout evidence is complete for UAT-01 through UAT-05 and linked.
2. If incomplete, all Sprint 2 feature issues remain blocked.

## Recommended Sequencing

### Phase 0: Gate Validation
1. Validate Sprint 1 evidence gate (planning/QA check).

### Phase 1: Core State Foundations
2. S2-T00A-I01 - Dungeon Layout and Placement MVP Foundation.
3. S2-T00-I01 - Mana Tick and Reserve Pressure Foundation.
4. S2-T05-I01 - Encounter Contracts and Seeded Resolution.
5. S2-T05-I02 - Encounter Event Emission for Downstream Systems.
6. S2-T06-I01 - Loot Table Resolution and Validation.
7. S2-T03-I01 - Heat Core Rules and Boundary Enforcement.

### Phase 2: Progression Safety and Contract Consumers
8. S2-T01-I01 - Research State Machine and Guards.
9. S2-T01-I02 - Research Save Fields and Migration Fixtures.
10. S2-T02-I01 - Verification Intent Model and Queue Lifecycle.
11. S2-T06-I02 - Inventory Handoff and Loot Lifecycle Updates.
12. S2-T07-I01 - Pending/Restricted State UI Bindings.
13. S2-T04-I01 - Offline Orchestrator Order and Cap Handling.
14. S2-T02-I02 - Idempotent Confirmation and Replay Safety Tests.
15. S2-T07-I02 - Localization Keys and Coverage Checks.

## Dependency Rationale
- Encounter contracts are sequenced before heat, loot integration, UI bindings, and offline orchestration because those systems consume encounter outputs and payload shape.
- Layout and mana foundations are sequenced first so encounter inputs and resource outputs are stable before downstream integration.
- Verification safety and localization remain in Sprint 2, but after deterministic loop contracts are validated.
- Research persistence is sequenced before offline orchestration because offline research state must survive save/load boundaries.

## Sprint 2A / 2B Recommendation
### Sprint 2A (proof-of-loop slice)
- S2-T00A-I01 minimum dungeon layout state.
- S2-T00-I01 mana tick foundation.
- S2-T05-I01 encounter contracts and seeded resolution.
- S2-T05-I02 encounter event emission.
- S2-T06-I01 loot table resolution.
- S2-T03-I01 heat core rules.
- Minimal deterministic test harness coverage for run-to-outcome replay.

### Sprint 2B (stabilization and trust depth)
- S2-T01-I01 and S2-T01-I02 research lifecycle and persistence.
- S2-T02-I01 and S2-T02-I02 verification queue and replay safety.
- S2-T06-I02 inventory handoff.
- S2-T07-I01 and S2-T07-I02 UI and localization.
- S2-T04-I01 offline orchestrator.
- Spillover integration fixes.

Rationale:
- Sprint 2A proves the deterministic run-to-outcome loop.
- Sprint 2B adds progression safety, offline orchestration, inventory persistence depth, and player-facing trust polish.
