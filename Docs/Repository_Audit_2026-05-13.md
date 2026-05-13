# Repository Audit (2026-05-13)

## Scope
This audit reviewed runtime architecture, gameplay domain coverage, and edit-mode test coverage for the current MVP branch.

## What is in good shape

1. **Core bootstrap and service wiring are present**
   - `GameRoot` initializes content, save, time, telemetry, and KPI services and enters app states.
   - Boot and home stub states are implemented for the startup flow.

2. **Deterministic economy primitives are implemented**
   - Formula engine supports ordered buckets and deterministic evaluation.
   - Mana system consumes simulation inputs and applies formula output predictably.
   - Heat system includes event application and decay with lower-bound clamping.

3. **Save and integrity scaffolding exists**
   - Save migration runner exists and is covered by tests.
   - Restricted action gating is implemented to model pending verification constraints.
   - Simulation clock/time services exist with deterministic-style tick progression support.

4. **Baseline observability and tests exist**
   - Telemetry and KPI service implementations are present with tests.
   - EditMode test suite covers formula/mana/heat/clock/migration/restricted-action/telemetry/KPI.

## Gaps and risks

1. **Research lifecycle is incomplete as a dedicated domain flow**
   - No clear single-slot research queue state machine with pending-verification completion semantics.

2. **Encounter and loot domains are still missing**
   - Adventurer run simulation and loot extraction/value-flow systems are not yet first-class runtime services.

3. **Verification depth may be insufficient for later milestones**
   - Existing restricted-action gate is useful, but full intent queue + idempotent reconciliation behavior is not obvious as an isolated service layer.

4. **Offline progression orchestration is fragmented**
   - Time and simulation pieces exist, but a unified offline grant coordinator with explicit caps/guards is not obvious.

5. **Test matrix is strong for current foundations but not complete for Sprint 2+ risks**
   - Missing stress/edge suites around reconnect idempotency, partitioned save conflicts, and full pending-confirm workflows.

## Recommended next steps (priority order)

1. **Implement `ResearchQueueService` (single slot) with explicit states**
   - States: Idle -> Active -> CompletedPendingVerification -> Confirmed/Claimed.
   - Add deterministic tests for transition guards and replay safety.

2. **Introduce `VerificationIntentService`**
   - Queue action intents with stable IDs.
   - Support retry-safe confirmation and duplicate-response handling.
   - Add tests for idempotency and out-of-order responses.

3. **Add `OfflineProgressionOrchestrator`**
   - Centralize elapsed-time replay, heat decay, mana accrual, and research progression caps.
   - Add tests for maximum offline windows and anomaly/skew scenarios.

4. **Land encounter skeleton + loot extraction contracts**
   - Keep interfaces narrow and data-driven to protect determinism.
   - Add test seams for spawn bands/outcomes and extraction edge cases.

5. **Expand save model toward partition-ready structure before feature growth**
   - Define partition boundaries and migration fixtures now to reduce later migration risk.

6. **Upgrade quality gates**
   - Add CI checks for schema-version/content-manifest consistency and regression test execution policy.

## Suggested execution plan

- **Sprint closeout (short-term):** steps 1-3.
- **Next sprint kickoff:** step 4.
- **Hardening track:** steps 5-6 in parallel with domain feature work.
