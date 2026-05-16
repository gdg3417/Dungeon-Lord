# Implementation Gap Assessment (as of 2026-05-13)

## Summary
Compared against the provided implementation map, 3-sprint backlog, test matrix, and execution tasks, the repository appears to have a **solid Sprint 1 foundation** (formula engine, mana system, clock/time, save migration skeleton, content schema gates, telemetry stubs, restricted-action gate). The main gaps are in **Heat**, **Research lifecycle**, **Adventurer/Encounter**, **Loot domain**, and deeper **verification/save partitioning** needed for Sprint 2+.

## What is already present (mapped to your plan)

### Bootstrap & Runtime Orchestration
- `GameRoot` composes and initializes content, save, time, telemetry, KPI, then enters state flow (`BootState` -> `HomeStubState`).
- Startup loads bootstrap/config/schema/manifest/string tables and applies manifest/schema version checks.
- Save load/create and migration runner invocation are wired.

### Economy Simulation Core
- `FormulaEngine` exists with ordered bucket evaluation and support for additive, multiplicative, clamp, soft-cap, and rounding buckets.
- `ManaSystem` exists with deterministic tick input shape and modifier application.
- `Upkeep` reservation support is included in mana calculation inputs.

### Save, Time, Integrity
- `SimulationClock` and `TimeService` are present with online/offline style elapsed handling and skew/anomaly signaling.
- Save root is versioned and migration skeleton is present (`MigrationRunner`, migration map).
- Restricted action gate service exists and already models pending-verification blocking.

### Content Pipeline and Contracts
- Bootstrap JSON and content manifest/schema version assets exist.
- Schema files for mana/heat/research modifiers and a schema bundle are present.
- Content service enforces schema gate checks against manifest requirements.

### Telemetry + basic observability
- Telemetry service exists and key events (`tick_processed`, `mana_generated`, `research_outcome`, verification pending toggles) are already tracked.
- KPI helper and tests exist for dashboard-level metrics.

### Tests currently aligned with Sprint 1 baseline
- EditMode coverage exists for FormulaEngine, ManaSystem, SimulationClock, MigrationRunner, RestrictedActionGate, KPI, and Telemetry.

## Gaps vs your target architecture/backlog

### Missing/partial domain systems (high priority)
1. **HeatSystem runtime implementation** is not present (event apply + decay + tier/offline safety rules).
2. **ResearchQueue single-slot lifecycle** (active -> elapsed -> pending verify -> confirmed) is not implemented as a dedicated domain service.
3. **Adventurer & Encounter simulation** is missing (spawn bands, run outcomes, retreat/survival/death model).
4. **Loot domain** is missing (room tables, extraction rules, value integration).

### Verification and integrity depth (medium-high)
5. Verification logic is currently represented as a gate/pending flag, but not yet a full intent queue + confirm/retry/idempotent reconciliation pipeline.
6. Save partitioning by primary/sub/account/season with conflict policy is not yet visible.

### UI/UX trust requirements (medium)
7. Current overlay/debug surfaces baseline state, but does not yet expose full pending lifecycle for research completion or richer restricted-action message matrix from specs.
8. Advanced formula transparency view and localized key-complete coverage are not yet evident.

### Test matrix gaps (high)
9. No dedicated tests yet for heat safety invariants, offline heat tier boundaries, research pending-confirm paths, loot extraction edge cases, reconnect idempotency, or performance ceiling harnesses from your matrix.

## Recommended next steps (ordered)

1. Implement `HeatSystem` (`ApplyEvent`, `Decay`) + invariant tests first.
2. Add `ResearchQueue` single-slot domain model with pending verification transitions and confirmation APIs.
3. Wire a `VerificationService` with idempotent write intents and reconciliation hooks (local queue + confirmation replay).
4. Expand save model into explicit partitions and add migration fixtures for partitioned schema evolution.
5. Implement Adventurer run outcome model (spawn -> simulate -> emit events) behind interfaces to keep future AI expansion isolated.
6. Implement loot table runtime generator + survivor extraction rules; connect value output to mana/reputation/heat adapters.
7. Add offline progression grant orchestrator that clamps by your time/heat/research constraints.
8. Extend UI/dashboard for pending lifecycle visibility and standardized message keys across all restricted actions.
9. Add the missing test matrix suites (heat/research verification/save migration coverage/perf budgets).
10. Add content tooling checks for migration-rule coverage (`replacement_id`/`migration_rule_id`) and cross-table FK integrity.

## Proposed sprint realignment from current code state
- **Remainder of Sprint 1 (close-out):** HeatSystem + tests, tighten save migration fixtures, deterministic replay harness.
- **Sprint 2 start:** ResearchQueue + verification pending/confirm semantics, then Adventurer+Loot playable loop.
- **Sprint 3 start:** partitioned save, reconciliation robustness, perf + QA harness hardening.

## Delivery risk callouts
- The biggest risk to timeline is implementing Adventurer/Loot/Research while preserving deterministic replay and offline verification semantics.
- A second risk is introducing save partitioning after feature logic grows; consider landing partition scaffolding before full encounter/loot complexity.
