# MVP Sprint Completion Plan (Assuming Sprint 1 Passed)

Date: 2026-05-13  
Scope: Forward plan for Sprint 2 and Sprint 3 (plus Sprint 4 recommended) with explicit build steps per sprint.

## Planning assumption
This plan assumes Sprint 1 has passed all required testing and signoff gates, and the team is ready to execute feature work.

## Planning quality gates (applies to all sprint items)
Every sprint task in this plan must include:
1. Exact source spec IDs (from `00` to `37` and relevant design docs).
2. Primary owner role (`Engineering`, `Design`, `Data`, `QA`, `Release`).
3. Concrete artifact output (code module, content table, test fixture, UI surface, or checklist).
4. Acceptance criteria and a verification gate (unit/integration/UAT/CI evidence).
5. MVP scope status (`In-MVP`, `Deferred`, or `Post-MVP`).

## Source docs reviewed for this plan refresh (newly added Markdown set)
Reviewed against the newly added `Docs/*.md` conversion set from 2026-05-14, including:
- Master lock + glossary (`00`, `Cross_Spec_Glossary_of_Invariants_UPDATED`).
- System specs `01` through `37`.
- Core design docs:
  - `Dungeon Builder Game Design Doc v2`
  - `What is the smallest version of Dungeon Builder that proves the fantasy is fun`
  - `Dungeon_Builder_Architecture_Spec_v1`
  - `Dungeon_Builder_CoreTech_Spec_v1`
  - `Dungeon_Builder_Monsterology_Spec_v1`
  - `Dungeon_Builder_Trapcraft_Spec_v1`
  - `Dungeon_Builder_Arcanology_Spec_v1`
  - `Dungeon_Builder_Diplomacy_Spec_v1`

## Spec-to-sprint incorporation map (MVP-safe)

### Sprint 2 (direct implementation priority)
- `01` Mana, `02` Heat, `03` Monster stats/upkeep, `04` Adventurer evaluation, `05` Loot, `06` Party AI, `08` Raids baseline, `09` Progression/research.
- `15` UX trust exposure (minimum viable transparency surfaces).
- `17` Offline simulation, `18` telemetry baseline, `19` content pipeline baseline.
- `20` onboarding skeleton hooks.
- `23` inventory lifecycle (only required loop endpoints).
- `27` localization key routing.
- `29` time model operationalization.
- `30` formula + modifier stacking order enforcement.
- `35` user-facing error/trust messages for pending and failed states.

### Sprint 3 (hardening + production reliability)
- `25` anti-cheat/integrity checks (MVP-appropriate subset).
- `28` save model versioning + migration.
- `33` build/release environment workflow.
- `34` backend/API contract stabilization for restricted actions.
- `36` performance/memory/device targets.
- `37` QA strategy and harness automation.

### Sprint 4 (recommended polish + launch readiness)
- `14` retention loop polish.
- `16` external world economy tuning polish.
- `21` sink/deflation balancing.
- `26` accessibility/cognitive load pass.
- `31` dungeon identity/theming resolution.
- `32` event framework rule-override pass (MVP-safe subset).

### Deferred / explicitly excluded from MVP delivery (track only, do not build now)
- `12` monetization implementation (philosophy can inform UX language only).
- `13` live ops and world events (planning only).
- `22` prestige/seasonal resets.
- `24` social/competitive systems.
- `07` full political-economic reputation depth (limit to any minimal hooks already required by core loop).

---

## Sprint 2 — Build the Playable Core Loop

### Sprint 2 objective
Deliver the first end-to-end MVP gameplay loop: research progression, verification-safe action completion, encounter simulation, and loot outcomes.

### Sprint 2 build steps (in order)

#### S2-01: Research domain foundation
Owners: Engineering + QA
Specs: `09`, `17`, `28`, `30`
Artifacts: `ResearchQueueService`, save schema fields, deterministic transition tests
Acceptance gate:
- all state transitions covered by deterministic tests;
- invalid transitions rejected with explicit error states;
- save/load continuity verified with fixture roundtrip.
1. Define `ResearchQueueService` interfaces and data model.
2. Implement single-slot state machine:
   - `Idle`
   - `Active`
   - `CompletedPendingVerification`
   - `Confirmed/Claimed`
3. Add serialization fields needed for save/load continuity.
4. Add deterministic unit tests for all transitions and invalid transition guards.

#### S2-02: Verification intent pipeline
Owners: Engineering + QA
Specs: `25`, `34`, `35`
Artifacts: `VerificationIntentService`, intent queue model, replay test harness
Acceptance gate:
- idempotent confirm handling verified under duplicate/out-of-order responses;
- retry-safe behavior proven in reconnect replay tests;
- restricted action gates reflect pending verification state.
1. Create `VerificationIntentService` with stable intent IDs.
2. Implement local queue lifecycle (`queued -> sent -> confirmed/failed-retryable`).
3. Add idempotent confirm handling (duplicate/out-of-order response safety).
4. Integrate with `RestrictedActionGateService` so blocked actions align with pending verification state.
5. Add replay tests for retry/reconnect scenarios.

#### S2-03: Offline progression orchestrator
Owners: Engineering + QA
Specs: `01`, `02`, `17`, `29`, `30`
Artifacts: `OfflineProgressionOrchestrator`, elapsed-time rules, cap/guardrail tests
Acceptance gate:
- ordered progression pipeline is deterministic for equal seeds/inputs;
- offline cap and skew guard behavior validated at boundary conditions.
1. Implement `OfflineProgressionOrchestrator` to centralize elapsed-time application.
2. Apply ordered progression updates for mana, heat decay, and research elapsed state.
3. Enforce offline caps and skew guardrails from time/integrity rules.
4. Add tests for normal elapsed windows, cap edges, and anomaly inputs.

#### S2-04: Encounter simulation baseline
Owners: Engineering + Design + QA
Specs: `03`, `04`, `06`, `08`, `16`, `30`
Artifacts: encounter contracts, resolver implementation, event outputs, spawn/outcome tests
Acceptance gate:
- encounter outcomes deterministic for controlled input seeds;
- spawn band and party evaluation boundaries validated in tests.
1. Add encounter domain contracts (spawn config, party profile, outcome payload).
2. Implement deterministic encounter resolution (seeded/randomized via controlled input).
3. Emit encounter results as domain events for downstream systems.
4. Add tests for spawn-band boundaries and outcome consistency.

#### S2-05: Loot runtime and extraction
Owners: Engineering + Data + QA
Specs: `05`, `19`, `23`, `30`
Artifacts: data-driven loot resolver, extraction mapping rules, integrity test cases
Acceptance gate:
- loot table references pass integrity checks;
- edge-case behavior verified (empty/capped/invalid scenarios);
- outputs map cleanly to MVP economy value constraints.
1. Implement data-driven loot table resolution.
2. Implement extraction outcomes tied to encounter result states.
3. Convert loot results into economy-applicable values (within MVP constraints).
4. Add edge-case tests (empty tables, capped tiers, invalid references).

#### S2-06: UI trust and messaging
Owners: Engineering + Design + QA
Specs: `15`, `20`, `27`, `35`
Artifacts: placeholder/debug UI states, localization keys, UI verification checklist
Acceptance gate:
- no hardcoded player text for new surfaces;
- pending/restricted/error states visible and understandable;
- localization key coverage checklist complete.
1. Surface pending verification status for research and restricted actions.
2. Surface core loop outputs (encounter result + loot summary) in debug/placeholder UI.
3. Ensure all player-facing text resolves via localization keys.
4. Add basic UI verification checklist and snapshot captures.

#### S2-07: Sprint 2 stabilization and signoff
Owners: QA + Engineering + Release
Specs: `18`, `33`, `37`
Artifacts: QA execution packet, known-issues list, Sprint 2 signoff notes
Acceptance gate:
- Sprint 2 QA matrix fully executed with evidence;
- high-risk determinism/integrity issues resolved or explicitly waived by owner signoff.
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
Owners: Engineering + QA
Specs: `17`, `28`, `29`
Artifacts: partitioned save contracts, migration fixtures, rollback validation notes
1. Define explicit partition boundaries (core profile, progression, transient/runtime, verification queue).
2. Refactor save root/contracts to map fields into partitions.
3. Add migration fixtures for pre-partition -> partitioned schema evolution.
4. Validate backward compatibility and rollback safety.

#### S3-02: Reconciliation hardening
Owners: Engineering + QA
Specs: `25`, `34`, `37`
Artifacts: reconnect reconciliation pipeline, conflict handlers, replay assertions
1. Build reconciliation pass for queued intents after reconnect.
2. Handle conflict classes (duplicate confirm, stale response, missing ack).
3. Add deterministic replay tests for reconnect interruption patterns.
4. Add operational counters/telemetry for reconciliation outcomes.

#### S3-03: Performance and memory budgets
Owners: Engineering + QA
Specs: `18`, `36`, `37`
Artifacts: target profile budget sheet, stress harness, benchmark baseline outputs
1. Define target device profiles and budgets.
2. Add deterministic stress harnesses for long offline windows and high event counts.
3. Profile CPU/memory hotspots in simulation and save/reconcile paths.
4. Resolve budget breaches and re-run benchmark baselines.

#### S3-04: Content/data integrity gates
Owners: Data + Engineering + QA
Specs: `19`, `28`, `30`, `37`
Artifacts: schema/reference validators, migration coverage checks, CI fail-fast rules
1. Add automated checks for schema/manifest/version consistency.
2. Add FK/reference integrity checks across content assets.
3. Add migration-rule coverage checks for content ID replacements.
4. Fail CI for content integrity violations.

#### S3-05: Release readiness framework
Owners: Release + QA + Engineering
Specs: `33`, `34`, `36`, `37`
Artifacts: CI gate definitions, go/no-go dashboard, regression checklist and signoff workflow
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
Owners: Design + Engineering + QA + Release
Primary specs: `14`, `16`, `21`, `26`, `31`, `32`
1. Balance pass for mana/heat/encounter/loot loops using telemetry-backed tuning.
2. UX polish for clarity of risk/reward, pending states, and outcome explanations.
3. Onboarding/tutorial pass for first-session comprehension.
4. Accessibility and readability pass (font, contrast, pacing, cognitive load).
5. Soft-launch rehearsal checklist and release-ops dry run.

### Sprint 4 completion criteria
- New players can complete first core loop without confusion.
- Telemetry indicates healthy early-session progression.
- Launch rehearsal checklist completes without blockers.

---

## Appendix A — Sprint 2 ticket creation pack (tracker-ready)

Use this appendix to create board tickets directly for S2-01 through S2-07.

### S2-01 — Research domain foundation (START HERE)
- Priority: P0 (unblocks downstream systems)
- Owners: Engineering, QA
- Labels: `spec:09`, `spec:17`, `spec:28`, `spec:30`, `mvp:in`, `risk:high`
- Artifacts:
  - `ResearchQueueService` interfaces + data model
  - Single-slot state machine (`Idle`, `Active`, `CompletedPendingVerification`, `Confirmed/Claimed`)
  - Save/load serialization fields
  - Deterministic transition tests
- Acceptance criteria:
  1. All state transitions are covered by deterministic tests.
  2. Invalid transitions are rejected with explicit error states.
  3. Save/load continuity passes fixture roundtrip validation.

### S2-02 — Verification intent pipeline
- Priority: P0
- Owners: Engineering, QA
- Labels: `spec:25`, `spec:34`, `spec:35`, `mvp:in`, `risk:high`
- Artifacts:
  - `VerificationIntentService` with stable intent IDs
  - Local queue lifecycle (`queued -> sent -> confirmed/failed-retryable`)
  - Replay/reconnect test harness
- Acceptance criteria:
  1. Confirm handling is idempotent for duplicate/out-of-order responses.
  2. Retry-safe behavior is proven in reconnect replay tests.
  3. Restricted action gates match pending verification state.

### S2-03 — Offline progression orchestrator
- Priority: P0
- Owners: Engineering, QA
- Labels: `spec:01`, `spec:02`, `spec:17`, `spec:29`, `spec:30`, `mvp:in`, `risk:high`
- Artifacts:
  - `OfflineProgressionOrchestrator`
  - Ordered elapsed-time rules for mana/heat/research
  - Offline cap and skew guardrail tests
- Acceptance criteria:
  1. Ordered progression is deterministic for equal seeds/inputs.
  2. Offline cap and skew guard behavior pass boundary-condition tests.

### S2-04 — Encounter simulation baseline
- Priority: P1
- Owners: Engineering, Design, QA
- Labels: `spec:03`, `spec:04`, `spec:06`, `spec:08`, `spec:16`, `spec:30`, `mvp:in`, `risk:med`
- Artifacts:
  - Encounter contracts (spawn config, party profile, outcome payload)
  - Deterministic resolver implementation
  - Encounter domain events
  - Spawn/outcome test suite
- Acceptance criteria:
  1. Encounter outcomes are deterministic for controlled seeds.
  2. Spawn-band and party-evaluation boundaries pass test coverage.

### S2-05 — Loot runtime and extraction
- Priority: P1
- Owners: Engineering, Data, QA
- Labels: `spec:05`, `spec:19`, `spec:23`, `spec:30`, `mvp:in`, `risk:med`
- Artifacts:
  - Data-driven loot table resolver
  - Extraction mapping rules
  - Integrity and edge-case tests
- Acceptance criteria:
  1. Loot table references pass integrity checks.
  2. Edge cases (empty/capped/invalid) pass test expectations.
  3. Outputs map to MVP economy constraints.

### S2-06 — UI trust and messaging
- Priority: P1
- Owners: Engineering, Design, QA
- Labels: `spec:15`, `spec:20`, `spec:27`, `spec:35`, `mvp:in`, `risk:med`
- Artifacts:
  - Pending/restricted/error placeholder UI states
  - Localization keys for new player-facing text
- Acceptance criteria:
  1. No hardcoded player-facing text on added surfaces.
  2. Pending/restricted/error states are visible and understandable.
  3. Localization key coverage checklist is complete.

### S2-07 — Sprint 2 stabilization and signoff
- Priority: P0
- Owners: QA, Engineering, Release
- Labels: `spec:18`, `spec:33`, `spec:37`, `mvp:in`, `risk:high`
- Artifacts:
  - Executed Sprint 2 QA packet
  - Known-issues log
  - Sprint 2 release/signoff notes
- Acceptance criteria:
  1. Full Sprint 2 QA matrix is executed with evidence.
  2. High-risk determinism/integrity issues are fixed or explicitly waived by owner signoff.

### Pre-coding gate for Sprint 2
Before starting Sprint 2 feature coding, verify Sprint 1 closeout evidence is complete in:
- `Docs/Sprint1_Closeout_Checklist_2026-05-13.md`

Suggested board ticket:
- `S2-GATE-00 — Sprint 1 closeout evidence verification`
  - Labels: `mvp:in`, `risk:high`
  - Done when: all UAT-01..UAT-05 evidence is present and final signoff is filled.
