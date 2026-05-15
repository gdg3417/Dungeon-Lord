# Actionable Game Development Backlog

## Source Documents Reviewed
- `README.md` - repository-level context (minimal project framing).
- `Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md` - authoritative lock status, global invariants, and implementation safety constraints.
- `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md` - normalized terminology and invariant definitions.
- `Docs/Dungeon Builder Game Design Doc v2.md` - high-level player fantasy, loop intent, and UX direction.
- `Docs/What is the smallest version of Dungeon Builder that proves the fantasy is fun.md` - MVP vertical-slice boundaries.
- `Docs/Dungeon_Builder_Architecture_Spec_v1.md` - runtime boundaries, system integration expectations.
- `Docs/Dungeon_Builder_CoreTech_Spec_v1.md` - core technical quality constraints and platform behavior.
- `Docs/Dungeon_Builder_Monsterology_Spec_v1.md` - monster taxonomy/system behavior references.
- `Docs/Dungeon_Builder_Trapcraft_Spec_v1.md` - trap and hazard behavior references for encounters.
- `Docs/Dungeon_Builder_Arcanology_Spec_v1.md` - research/magic progression references.
- `Docs/Dungeon_Builder_Diplomacy_Spec_v1.md` - identified as largely deferred for MVP.
- System Specs `Docs/01` through `Docs/37` - functional and technical requirements source-of-truth for game systems and engineering guardrails.
- `Docs/Sprint_Spec_Coverage_Matrix.md` - baseline sprint-to-spec mapping and current owner/artifact expectations.
- `Docs/Implementation_Gap_Assessment_2026-05-13.md` - current-state implementation gaps and sequencing risks.
- `Docs/Sprint1_Completion_Plan_2026-05-13.md` - baseline Sprint 2/3/4 execution plan.
- `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` - Sprint 1 closure gates and quality evidence requirements.

## Assumptions and Open Questions
1. Sprint planning baseline appears to be **post-Sprint-1 closeout**, but closure evidence is external; Sprint 2 start is contingent on full UAT signoff. **Needs clarification**.
2. Heat system numeric thresholds are marked "locked pending final numbers" in Spec 02 language; implementation framing is clear but exact tuning may still be pending. **Needs clarification**.
3. Reputation/politics depth (Spec 07, Diplomacy spec) conflicts with MVP deferment guidance in sprint planning artifacts; treat as deferred except minimal hooks. **Recommended control source**: Sprint spec matrix + MVP scope docs (more recent operational planning).
4. Event framework (Spec 32) is planned for Sprint 4, but encounter/loot balancing may need limited override hooks earlier for testability. **Needs clarification**.
5. Backend verification contracts (Spec 34) are required for economy-critical actions, but environment assumptions for offline-first QA flows are not fully explicit. **Needs clarification**.

## Feature Areas
- Core Gameplay Loop (mana -> encounters -> loot -> reinvestment)
- Resource Economy (mana generation, sinks, reservation, late-game stability)
- Heat and World Response
- Adventurer/Encounter Simulation (evaluation + party behavior + raids baseline)
- Monster/Trap Content Systems
- Research and Progression
- Inventory and Loot Lifecycle
- Save/Load, Offline Simulation, and Time Model
- Verification, Security, and Integrity
- UI/UX Transparency, Error Handling, and Onboarding
- Content Pipeline and Data Validation
- Telemetry, Analytics, Performance, and QA Infrastructure
- Build/Release and Environment Strategy

## Epics

### Epic: Playable Core Loop Vertical Slice
Feature Area: Core Gameplay Loop
Priority: Must Have
Sprint Target: Sprint 2
Status: Existing (Modified into smaller stories)

Summary:
Deliver the first deterministic end-to-end loop where the player gains mana, processes encounters, receives loot outcomes, and reinvests via research under MVP constraints.

Design Source:
- GDD v2 core fantasy and loop
- MVP smallest-fun version doc
- Specs 01, 03, 04, 05, 06, 08, 09

Technical Source:
- Specs 17, 19, 29, 30, 37
- Architecture/CoreTech specs
- Implementation Gap Assessment (missing core domain services)

Dependencies:
- Formula framework enforcement (Spec 30)
- Time/offline orchestration (Specs 17/29)
- Content schema integrity (Spec 19)

User Stories:

#### Story: Deterministic Mana Tick and Reservation Flow
As a player,
I want mana generation and usable/reserved mana to update deterministically,
so that strategic decisions are predictable and trustworthy.

Priority: Must Have
Estimate: 5 points
Sprint Recommendation: Sprint 2
Status: Existing (Refined)

Functional Requirements:
- Apply base + floor + encounter-derived mana per Spec 01.
- Maintain visible Total, Reserved, and Usable mana pools.
- Apply heat efficiency multiplier to all mana sources.

Technical Requirements:
- Evaluate modifiers via Spec 30 order.
- Deterministic tick processing for equal inputs/seeds.
- Serialize and restore all mana state fields.

Acceptance Criteria:
- Given fixed tick inputs, when simulation runs twice, then mana outputs are identical.
- Given Reserved > generation, when ticks continue, then fragile-state behavior is surfaced without negative usable underflow.
- Given heat tier changes, when mana updates, then source outputs reflect tier efficiency multiplier.

Implementation Tasks:
- Map Spec 01 formulas to runtime tick contract.
- Add edge handling for reservation pressure and pool visibility.
- Add deterministic tests for repeat runs and boundaries.

Test Cases:
- Equal-seed determinism regression test.
- Reserved mana overflow/fragility test.
- Heat multiplier cross-check test.

Dependencies:
- Formula engine order lock.
- Heat system state feed.

Notes:
- Any formula ambiguity defers to Spec 30 for ordering.

#### Story: Baseline Encounter Outcome Simulation
As a system,
I want deterministic encounter outcomes from adventurer party inputs,
so that heat, loot, and progression systems can consume stable events.

Priority: Must Have
Estimate: 8 points
Sprint Recommendation: Sprint 2
Status: Existing (Split from broad sprint item)

Functional Requirements:
- Consume party profile + dungeon state and output survive/retreat/death outcomes.
- Support MVP adventurer classes and elite behavior rules.
- Emit events consumable by heat and loot systems.

Technical Requirements:
- Controlled RNG or seeded deterministic resolver.
- Domain events with stable IDs.
- Boundary tests for spawn bands/outcomes.

Acceptance Criteria:
- Given identical input seed/config, when encounter resolves, then output event stream is identical.
- Given elite-tagged runs, when outcomes generate mana/heat impacts, then elite multipliers are applied where specified.
- Given invalid party config input, when resolver executes, then explicit validation error is returned.

Implementation Tasks:
- Define encounter contracts.
- Implement deterministic resolver skeleton.
- Wire event emission interfaces.

Test Cases:
- Determinism replay for encounters.
- Elite multiplier application test.
- Invalid input guard test.

Dependencies:
- Monster baseline data.
- Party AI rules.

Notes:
- Detailed AI sophistication beyond baseline remains out of scope for this story.

### Epic: Heat and Risk-Reward Regulation
Feature Area: Heat and World Response
Priority: Must Have
Sprint Target: Sprint 2
Status: Existing (High-priority gap)

Summary:
Implement world-response pressure using heat gain/decay and tier constraints, including offline invariants.

Design Source:
- Spec 02 (heat model)
- Specs 08, 11, 14 (risk/failure/retention interactions)

Technical Source:
- Specs 17, 29, 30, 37
- Gap Assessment highlights missing HeatSystem

Dependencies:
- Encounter outcomes feed
- Time/offline orchestration

User Stories:

#### Story: Heat Event Application and Tier Boundaries
As a player,
I want my dungeon actions to change heat predictably,
so that risk and rewards feel coherent.

Priority: Must Have
Estimate: 5 points
Sprint Recommendation: Sprint 2
Status: Existing

Functional Requirements:
- Apply heat gain from normal/elite deaths and multi-death bonus.
- Apply heat reduction from successful party extraction and tradeable loot value.
- Maintain tier/state model with bounded transitions.

Technical Requirements:
- Heat state machine with invariant guards.
- Configurable threshold table from data content.
- Boundary-condition tests.

Acceptance Criteria:
- Given configured thresholds, when heat crosses boundary, then tier/state transition matches table.
- Given a full-party survival run, when reduction applies, then depth/party-size scaling is honored.
- Given out-of-range reduction input, when processed, then clamped valid heat state is persisted.

Implementation Tasks:
- Implement HeatSystem ApplyEvent/Decay.
- Integrate with encounter + loot events.
- Add boundary and clamp tests.

Test Cases:
- Tier crossing test.
- Survival reduction scaling test.
- Clamp and underflow prevention test.

Dependencies:
- Encounter output schema.
- Loot value mapping.

Notes:
- Exact numeric balancing may require later tuning pass.

### Epic: Research and Verification-Safe Progression
Feature Area: Research and Progression
Priority: Must Have
Sprint Target: Sprint 2-3
Status: Existing (Modified sequencing)

Summary:
Implement single-slot research lifecycle with verification-safe completion semantics and offline elapsed handling.

Design Source:
- Spec 09 progression/research
- Spec 20 onboarding dependencies

Technical Source:
- Specs 17, 25, 28, 34, 35, 37
- Sprint completion plan S2-01/S2-02

Dependencies:
- Save migration scaffolding
- Verification intent queue

User Stories:

#### Story: Single-Slot Research Lifecycle
As a player,
I want one active research slot with clear states,
so that progression is understandable and enforceable.

Priority: Must Have
Estimate: 5 points
Sprint Recommendation: Sprint 2
Status: Existing

Functional Requirements:
- Support Idle -> Active -> CompletedPendingVerification -> Confirmed/Claimed.
- Prevent invalid concurrent research starts (single-slot only).
- Surface pending and blocked states in UI text.

Technical Requirements:
- Deterministic state machine transitions.
- Persist state fields across save/load.
- Transition audit telemetry events.

Acceptance Criteria:
- Given one active research, when player starts another, then action is blocked with explicit message.
- Given elapsed completion offline, when client returns online and confirmation succeeds, then state transitions to confirmed.
- Given invalid transition attempt, when API invoked, then deterministic error state is returned.

Implementation Tasks:
- Implement queue service + transition guards.
- Add save schema fields and migration handling.
- Add state-machine tests including invalid transitions.

Test Cases:
- Single-slot exclusivity test.
- Offline complete/pending/confirm path test.
- Invalid transition rejection test.

Dependencies:
- Verification service.
- Save versioning rules.

Notes:
- Pending-verification messaging must align with trust/error specs.

## Existing Sprint Plan Impact

### Sprint 1
Current Purpose:
Foundation systems, deterministic core, migration scaffolding, and UAT closure.

Recommended Purpose:
No scope expansion. Enforce closure evidence completion before Sprint 2 feature work.

Changes:
- Add: Explicit gate that Sprint 2 tickets cannot start until UAT-01..UAT-05 evidence packet is complete.
- Modify: Closeout checklist to link artifacts to ticket IDs.
- Remove or Defer: Any unplanned feature additions discovered during hardening.

Reasoning:
Current planning already assumes Sprint 1 passed; governance gap is evidence traceability.

Risks:
Starting Sprint 2 with incomplete determinism/migration validation risks compounding defects.

### Sprint 2
Current Purpose:
Playable core loop implementation.

Recommended Purpose:
Playable loop **with testable domain slices**: Heat, Research, Verification pipeline, Encounter baseline, Loot extraction, UI trust minimum.

Changes:
- Add: HeatSystem implementation and tests as first Sprint 2 task.
- Split: Broad "encounter simulation" into contract definition and deterministic resolver stories.
- Modify: UI work constrained to pending/restricted/loop-output transparency; no broad polish.
- Move: Any retention/balance-only work to Sprint 4 unless required to validate MVP loop.

Reasoning:
Gap assessment confirms heat/research/loot/verifier are missing and blocker dependencies.

Risks:
Without splitting, oversized stories will hide dependency failure until late sprint.

### Sprint 3
Current Purpose:
Hardening and production reliability.

Recommended Purpose:
Save partitioning, reconciliation robustness, performance budgets, content integrity CI gates, release readiness.

Changes:
- Add: Explicit conflict-policy tests for stale/duplicate verification responses.
- Modify: Save partition story to include backward compatibility matrix fixtures.
- Move: Any unresolved Sprint 2 must-have functional stories into Sprint 3 only with scope trade-off decision.

Reasoning:
Specs 25/28/33/34/36/37 are reliability-critical for MVP candidate quality.

Risks:
If unchanged, release hardening may be incomplete despite "feature complete" claims.

### Sprint 4 (Recommended)
Current Purpose:
Polish and launch prep.

Recommended Purpose:
Retention tuning + onboarding clarity + accessibility + thematic consistency + limited event override support.

Changes:
- Add: Explicit KPI-driven tuning stories tied to telemetry evidence.
- Modify: Keep event framework limited to MVP-safe override subset.
- Remove or Defer: Non-MVP monetization/social/live-ops implementations.

Reasoning:
Preserves design intent while avoiding post-MVP feature creep.

Risks:
Skipping Sprint 4 may ship a technically stable but confusing first-session experience.

## Backlog Items Not Yet Sprint-Ready
- Full political/economic/guild reputation depth (Spec 07): Needs clear MVP boundary and ownership.
- Event framework broad override system (Spec 32 complete vision): Defer full player-facing event framework; keep internal override subset only.
- External world economy full simulation depth (Spec 16 advanced behaviors): Needs balancing model + telemetry targets.
- MVP-MF01: Mana Farm Sub Dungeon Placeholder and Scope Gate: MVP scope retained, but deferred until main dungeon core loop stability is validated.
- Prestige/seasonal resets (Spec 22), social/competitive (Spec 24), live-ops events (Spec 13), monetization implementation (Spec 12): Deferred by MVP scope.

## Conflicts and Duplicates
- Conflict: Some docs include deep diplomacy/political systems while sprint matrix marks them deferred.
  - Recommendation: Sprint matrix + MVP smallest-fun doc controls for implementation now (more specific to delivery stage).
- Conflict: Spec 02 phrasing indicates lock but "pending final numbers"; behavior is locked, tuning values potentially not.
  - Recommendation: Implement behavior now; mark numeric tuning backlog under Sprint 4.
- Duplicate/Overlap: Encounter, loot, and heat requirements appear across Specs 03/04/05/06/08/16 and sprint tasks.
  - Resolution: Use single domain event contract to prevent duplicate implementations.
- Unclear ownership: Verification UX text can be split between gameplay and platform/service layers.
  - Recommendation: Engineering owns state plumbing; Design owns content/wording; QA owns acceptance checks.

## Recommended Next Sprint
Sprint goal:
Deliver deterministic playable MVP core loop with verification-safe progression and heat regulation.

Selected stories:
1. Dungeon layout and placement MVP foundation for encounter inputs.
2. Mana tick and reserve pressure foundation.
3. Encounter resolver baseline (deterministic contracts + event outputs).
4. Loot table resolution and inventory handoff baseline.
5. Heat event application and tier boundaries.
6. Single-slot research lifecycle.
7. Verification intent queue + idempotent confirmation.
8. Offline progression orchestrator (mana/heat/research ordering).
9. UI trust minimum for pending/restricted/error states.

Dependencies:
- Sprint 1 UAT closure evidence complete.
- Locked formula ordering and invariant glossary adopted in ticket acceptance criteria.
- Content schema/manifest validators available in CI.

Definition of done:
- All selected stories have passing deterministic tests and explicit acceptance evidence.
- No hardcoded user-facing text in newly exposed UI states.
- Reconnect/retry scenarios pass idempotency tests for verification queue.
- Core loop can be run end-to-end in test harness with stable outputs.

Items explicitly excluded from this sprint:
- Prestige/seasonal systems.
- Live-ops/event cadence systems beyond MVP-safe hooks.
- Social/competitive features.
- Advanced diplomacy depth and non-essential economy simulation layers.
