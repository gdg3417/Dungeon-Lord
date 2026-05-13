# MVP Sprint Completion Plan (Assuming Sprint 1 Passed)

Date: 2026-05-13  
Scope: Forward plan for Sprint 2 and Sprint 3 (plus Sprint 4 recommended) with explicit build steps per sprint.

## Planning assumption
This plan assumes Sprint 1 has passed all required testing and signoff gates, and the team is ready to execute feature work.

---

## Sprint 2 — Build the Playable Core Loop

### Sprint 2 objective
Deliver the first end-to-end MVP gameplay loop: research progression, verification-safe action completion, encounter simulation, and loot outcomes.

### Sprint 2 build steps (in order)

#### S2-01: Research domain foundation
1. Define `ResearchQueueService` interfaces and data model.
2. Implement single-slot state machine:
   - `Idle`
   - `Active`
   - `CompletedPendingVerification`
   - `Confirmed/Claimed`
3. Add serialization fields needed for save/load continuity.
4. Add deterministic unit tests for all transitions and invalid transition guards.

#### S2-02: Verification intent pipeline
1. Create `VerificationIntentService` with stable intent IDs.
2. Implement local queue lifecycle (`queued -> sent -> confirmed/failed-retryable`).
3. Add idempotent confirm handling (duplicate/out-of-order response safety).
4. Integrate with `RestrictedActionGateService` so blocked actions align with pending verification state.
5. Add replay tests for retry/reconnect scenarios.

#### S2-03: Offline progression orchestrator
1. Implement `OfflineProgressionOrchestrator` to centralize elapsed-time application.
2. Apply ordered progression updates for mana, heat decay, and research elapsed state.
3. Enforce offline caps and skew guardrails from time/integrity rules.
4. Add tests for normal elapsed windows, cap edges, and anomaly inputs.

#### S2-04: Encounter simulation baseline
1. Add encounter domain contracts (spawn config, party profile, outcome payload).
2. Implement deterministic encounter resolution (seeded/randomized via controlled input).
3. Emit encounter results as domain events for downstream systems.
4. Add tests for spawn-band boundaries and outcome consistency.

#### S2-05: Loot runtime and extraction
1. Implement data-driven loot table resolution.
2. Implement extraction outcomes tied to encounter result states.
3. Convert loot results into economy-applicable values (within MVP constraints).
4. Add edge-case tests (empty tables, capped tiers, invalid references).

#### S2-06: UI trust and messaging
1. Surface pending verification status for research and restricted actions.
2. Surface core loop outputs (encounter result + loot summary) in debug/placeholder UI.
3. Ensure all player-facing text resolves via localization keys.
4. Add basic UI verification checklist and snapshot captures.

#### S2-07: Sprint 2 stabilization and signoff
1. Execute Sprint 2 QA matrix:
   - research transitions
   - verification idempotency
   - offline caps
   - encounter determinism
   - loot extraction rules
2. Fix highest-risk defects first (determinism/integrity before UX polish).
3. Publish Sprint 2 release notes + known-issues list.

### Sprint 2 completion criteria
- End-to-end loop from research progression to encounter/loot result is playable.
- Verification pipeline is retry-safe and idempotent.
- Offline progression logic respects caps/guardrails.
- Sprint 2 QA matrix passes in Unity runner.

---

## Sprint 3 — Harden, Partition, and Scale Reliability

### Sprint 3 objective
Turn the playable loop into a robust MVP candidate with resilient save architecture, reconciliation safety, and production-level quality gates.

### Sprint 3 build steps (in order)

#### S3-01: Save partition architecture
1. Define explicit partition boundaries (core profile, progression, transient/runtime, verification queue).
2. Refactor save root/contracts to map fields into partitions.
3. Add migration fixtures for pre-partition -> partitioned schema evolution.
4. Validate backward compatibility and rollback safety.

#### S3-02: Reconciliation hardening
1. Build reconciliation pass for queued intents after reconnect.
2. Handle conflict classes (duplicate confirm, stale response, missing ack).
3. Add deterministic replay tests for reconnect interruption patterns.
4. Add operational counters/telemetry for reconciliation outcomes.

#### S3-03: Performance and memory budgets
1. Define target device profiles and budgets.
2. Add deterministic stress harnesses for long offline windows and high event counts.
3. Profile CPU/memory hotspots in simulation and save/reconcile paths.
4. Resolve budget breaches and re-run benchmark baselines.

#### S3-04: Content/data integrity gates
1. Add automated checks for schema/manifest/version consistency.
2. Add FK/reference integrity checks across content assets.
3. Add migration-rule coverage checks for content ID replacements.
4. Fail CI for content integrity violations.

#### S3-05: Release readiness framework
1. Establish CI gates for tests, deterministic replay, and content integrity.
2. Define go/no-go dashboard metrics from telemetry/KPI outputs.
3. Produce MVP release checklist with ownership and signoff workflow.
4. Run full regression and freeze build candidates.

### Sprint 3 completion criteria
- Save/reconciliation behavior is resilient under reconnect/failure scenarios.
- Performance and memory budgets are met on target profile(s).
- CI gates enforce deterministic and data integrity protections.
- MVP candidate build passes full regression checklist.

---

## Sprint 4 (Recommended) — MVP Vertical Slice Polish and Launch Prep

### Why Sprint 4 is recommended
If Sprint 2 and Sprint 3 focus on core loop + hardening, an additional sprint reduces launch risk by concentrating on UX trust, balance, and onboarding polish.

### Sprint 4 build steps (in order)
1. Balance pass for mana/heat/encounter/loot loops using telemetry-backed tuning.
2. UX polish for clarity of risk/reward, pending states, and outcome explanations.
3. Onboarding/tutorial pass for first-session comprehension.
4. Accessibility and readability pass (font, contrast, pacing, cognitive load).
5. Soft-launch rehearsal checklist and release-ops dry run.

### Sprint 4 completion criteria
- New players can complete first core loop without confusion.
- Telemetry indicates healthy early-session progression.
- Launch rehearsal checklist completes without blockers.
