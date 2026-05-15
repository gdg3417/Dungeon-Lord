# Sprint 2 Ticket Backlog (Playable Deterministic Core Loop)

## Sprint Scope and Guardrails
- Sprint focus: deterministic playable loop (research -> encounter -> loot -> reinvestment) with verification-safe behavior.
- Must preserve MVP constraints from locked specs and defer non-MVP systems.
- Sprint 1 closeout evidence remains a hard prerequisite. **Needs clarification**: confirm evidence packet links before kickoff.

## Ticket: Mana Tick and Reserve Pressure Foundation

Ticket ID: S2-T00
Epic: Playable Core Loop Vertical Slice
Feature Area: Resource Economy and Formula Framework
Priority: Must Have
Sprint: Sprint 2
Status: Added
Source References:
- docs/planning/actionable-backlog.md (Story: Deterministic Mana Tick and Reservation Flow)
- Docs/01 - Mana System v3.md
- Docs/02 - Heat System v3.md
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md
- Docs/29 - Time_Model_and_Tick_Resolution.md
- Docs/30 - Formula_Framework_and_Modifier_Stacking_Rules.md

User Story:
As a player,
I want mana generation and reservation pressure to resolve deterministically,
so that spending and progression decisions are predictable.

Functional Requirements:
- Calculate base mana generation each tick.
- Include floor contribution and adventurer death mana sources.
- Include skill spill mana hooks and elite multiplier handling where applicable.
- Apply heat efficiency modifier in the locked formula order.
- Maintain Total, Reserved, and Usable pools with invariant-safe updates.
- Surface fragile state when Reserved exceeds incoming generation.

Technical Requirements:
- Deterministic equal-input behavior for repeated runs.
- Save/load persistence for mana pools and fragile-state fields.
- Formula framework stacking order enforcement via Spec 30.

Acceptance Criteria:
- Given fixed inputs and seed, when two tick runs execute, then Total/Reserved/Usable outputs are identical.
- Given Reserved greater than incoming generation, when tick updates apply, then fragile state is set and usable mana never underflows below zero.
- Given all source inputs (base, floor, death, spill, elite, heat), when tick resolves, then output matches documented stacking order.

Implementation Tasks:
- Define deterministic mana tick contract with explicit source fields.
- Implement pool update ordering and fragile-state guard.
- Add save/load fields and migration fixture update requirements.

Test Cases:
- Equal-input deterministic mana tick replay test.
- Reserved pressure fragile-state boundary test.
- Source stacking order validation test.
- Save/load persistence round-trip test.

Dependencies:
- S2-T00A dungeon state baseline for floor contribution context.
- S2-T03 heat state feed.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- This ticket covers foundation behavior only; advanced economy tuning remains outside Sprint 2 implementation scope.

## Ticket: Dungeon Layout and Placement MVP Foundation

Ticket ID: S2-T00A
Epic: Playable Core Loop Vertical Slice
Feature Area: Dungeon State and Encounter Inputs
Priority: Must Have
Sprint: Sprint 2
Status: Added
Source References:
- docs/planning/actionable-backlog.md (Playable Core Loop Vertical Slice summary)
- Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md
- Docs/What is the smallest version of Dungeon Builder that proves the fantasy is fun.md
- Docs/05 - Loot and Itemization Framework.md
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md

User Story:
As a player,
I want a minimal editable dungeon layout and placement flow,
so that encounters consume valid dungeon state and save behavior is trustworthy.

Functional Requirements:
- Support minimal room or tile layout model required by encounter resolver.
- Support room placement/modification within MVP limits.
- Support monster placement only into valid rooms or tiles.
- Provide basic dungeon state contract consumed by encounter resolver.
- Trigger immediate save for tile placement and movement updates in edit mode.
- Enforce MVP limits: one main dungeon and up to five floors.

Technical Requirements:
- Deterministic layout serialization for equal inputs.
- Validation guards for invalid room/tile and placement targets.
- Save/load compatibility for layout and monster placement fields.

Acceptance Criteria:
- Given valid edit-mode placement action, when committed, then dungeon state updates and immediate save trigger fires.
- Given invalid placement target, when placement is attempted, then action is rejected with explicit reason and no partial state mutation.
- Given same initial layout and same placement actions, when replayed, then resulting dungeon state is identical.

Implementation Tasks:
- Define minimum dungeon layout and placement contracts.
- Add placement validation guards and encounter-consumable state projection.
- Define immediate-save trigger points for edit-mode placement/movement.

Test Cases:
- Valid room/tile placement plus immediate-save trigger test.
- Invalid placement rejection test.
- Encounter-consumable state projection schema test.
- Deterministic layout replay test.

Dependencies:
- Encounter contract finalization in S2-T05-I01.
- Save/state persistence standards from Spec 17.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- No advanced editor UX polish in MVP scope.

## Ticket: Research Queue State Machine (Single Slot)

Ticket ID: S2-T01
Epic: Research and Verification-Safe Progression
Feature Area: Research and Progression
Priority: Must Have
Sprint: Sprint 2
Status: Modified
Source References:
- docs/planning/actionable-backlog.md (Epic: Research and Verification-Safe Progression; Story: Single-Slot Research Lifecycle)
- Docs/09 - Dungeon_Progression_and_Research.md
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md
- Docs/28 - Save_Data_Model_Versioning_and_Migration.md

User Story:
As a player,
I want one active research slot with explicit lifecycle states,
so that progression remains understandable and enforceable.

Functional Requirements:
- Support Idle -> Active -> CompletedPendingVerification -> Confirmed/Claimed.
- Enforce single-slot exclusivity for research.
- Expose pending/blocked state to downstream UI/status surfaces.

Technical Requirements:
- Deterministic transition rules.
- Save/load persistence for all research state fields.
- Transition-level telemetry hooks.

Acceptance Criteria:
- Given one active research, when another start request is issued, then action is rejected with explicit blocked status.
- Given elapsed completion offline, when verification succeeds online, then state changes to Confirmed/Claimed.
- Given invalid state transition input, when transition call runs, then deterministic error response is returned and no state mutation occurs.

Implementation Tasks:
- Define state transition map and guard rules.
- Add serialized fields and migration fixture updates.
- Add deterministic transition and invalid-transition tests.

Test Cases:
- Single-slot exclusivity test.
- Offline completion -> pending -> confirm path test.
- Invalid transition rejection test.

Dependencies:
- S2-T02 verification queue behavior.
- Save migration fixture baseline.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Any additional research slots are out of MVP scope.

## Ticket: Verification Intent Queue and Idempotent Confirm

Ticket ID: S2-T02
Epic: Research and Verification-Safe Progression
Feature Area: Verification, Security, and Integrity
Priority: Must Have
Sprint: Sprint 2
Status: Split From Existing
Source References:
- docs/planning/actionable-backlog.md (Recommended Next Sprint item 3)
- Docs/25 - Security_Anti_cheat_and_Economy_Integrity.md
- Docs/34 - Backend_Services_and_API_Contract.md
- Docs/35 - Error_Handling_Player_Messaging_and_Trust.md

User Story:
As a system,
I want verification intents to be queued and reconciled idempotently,
so that restricted progression actions remain safe across retries and reconnects.

Functional Requirements:
- Queue economy-critical intents with stable IDs.
- Support queued -> sent -> confirmed/failed-retryable lifecycle.
- Reflect pending verification state to restricted-action gating.

Technical Requirements:
- Idempotent confirm handling for duplicate/out-of-order responses.
- Replay-safe reconnect behavior.
- Deterministic queue state transitions.

Acceptance Criteria:
- Given duplicate confirmation responses, when processed, then exactly one terminal success is recorded.
- Given reconnect after pending sends, when replay executes, then queue converges without duplicate grants.
- Given stale/unknown intent confirmation, when processed, then safe failure path is logged and state remains consistent.

Implementation Tasks:
- Define intent schema and stable ID generation rules.
- Implement queue lifecycle and reconcile handlers.
- Add replay/idempotency test harness cases.

Test Cases:
- Duplicate confirm idempotency test.
- Out-of-order response handling test.
- Reconnect replay convergence test.

Dependencies:
- Backend contract alignment (Spec 34).
- Restricted action gate integration points.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- **Needs clarification**: retry backoff policy and final failure UX copy ownership.

## Ticket: Heat System Event Application and Tier Boundaries

Ticket ID: S2-T03
Epic: Heat and Risk-Reward Regulation
Feature Area: Heat and World Response
Priority: Must Have
Sprint: Sprint 2
Status: Existing
Source References:
- docs/planning/actionable-backlog.md (Epic: Heat and Risk-Reward Regulation)
- Docs/02 - Heat System v3.md
- Docs/29 - Time_Model_and_Tick_Resolution.md

User Story:
As a player,
I want dungeon outcomes to impact heat in predictable ways,
so that risk-reward tradeoffs are legible.

Functional Requirements:
- Apply heat gain from normal/elite deaths and multi-death bonus.
- Apply heat reduction from full-party survival and tradeable loot value.
- Enforce heat tier/state boundaries.

Technical Requirements:
- Deterministic heat state transition logic.
- Data-driven threshold configuration.
- Offline-safe heat rebound constraints.

Acceptance Criteria:
- Given threshold boundaries, when heat crosses a boundary, then correct tier/state transition occurs.
- Given survival + extraction context, when reduction is computed, then party-size/depth/value modifiers apply as specified.
- Given out-of-range heat input, when applied, then value is clamped and invariant-safe state is persisted.

Implementation Tasks:
- Implement heat apply/decay operations.
- Wire encounter/loot event consumers.
- Add boundary clamp and invariant tests.

Test Cases:
- Tier boundary crossing test.
- Survival reduction scaling test.
- Clamp/underflow prevention test.

Dependencies:
- S2-T05 encounter outcomes.
- S2-T06 loot value mapping.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Locked MVP heat constants are sufficient for implementation; later tuning authority remains a non-blocking balance clarification.

## Ticket: Offline Progression Orchestrator (Ordered Deterministic Updates)

Ticket ID: S2-T04
Epic: Playable Core Loop Vertical Slice
Feature Area: Save/Load, Offline Simulation, and Time Model
Priority: Must Have
Sprint: Sprint 2
Status: Modified
Source References:
- docs/planning/actionable-backlog.md (Recommended Next Sprint item 4)
- Docs/01 - Mana System v3.md
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md
- Docs/29 - Time_Model_and_Tick_Resolution.md
- Docs/30 - Formula_Framework_and_Modifier_Stacking_Rules.md

User Story:
As a player,
I want offline progression to resolve deterministically with guardrails,
so that returning sessions are fair and predictable.

Functional Requirements:
- Apply ordered offline updates for mana, heat, and research elapsed state.
- Enforce elapsed-time caps and anomaly handling.
- Preserve locked stacking order and tier safety constraints.

Technical Requirements:
- Central orchestrator for elapsed application order.
- Deterministic execution for equal inputs.
- Boundary tests for caps/skew/anomalies.

Acceptance Criteria:
- Given equal input snapshots and elapsed time, when offline progression runs twice, then identical outputs are produced.
- Given elapsed time beyond cap, when processed, then capped grant/output is applied and flagged.
- Given invalid time anomaly input, when processed, then safe error handling path triggers without corrupted state.

Implementation Tasks:
- Define ordered orchestration pipeline.
- Integrate mana/heat/research update stages.
- Add cap/skew/anomaly tests.

Test Cases:
- Equal-input determinism test.
- Offline cap boundary test.
- Time anomaly safe-handling test.

Dependencies:
- S2-T01 research lifecycle.
- S2-T03 heat transitions.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Must preserve "offline heat rebound constrained within current tier" invariant.

## Ticket: Encounter Resolver Baseline (MVP Classes + Events)

Ticket ID: S2-T05
Epic: Playable Core Loop Vertical Slice
Feature Area: Adventurer/Encounter Simulation
Priority: Must Have
Sprint: Sprint 2
Status: Split From Existing
Source References:
- docs/planning/actionable-backlog.md (Story: Baseline Encounter Outcome Simulation)
- Docs/03 - Moster Stats and Upkeep v3.md
- Docs/04 - Adventurer Evaluation and Behavior v1.md
- Docs/06 - Adventurer Behavior. Evaluation. and Party AI.md
- Docs/08 - Raids_and_Escalation.md

User Story:
As a system,
I want deterministic encounter outcomes from party/dungeon inputs,
so that downstream systems consume stable event outputs.

Functional Requirements:
- Resolve encounter outcomes for MVP adventurer classes and elite cases.
- Emit normalized outcome events (survive/retreat/death and metadata).
- Respect baseline party evaluation behavior from specs.

Technical Requirements:
- Seeded deterministic resolver.
- Stable event IDs for downstream consumers.
- Boundary tests for spawn/outcome ranges.

Acceptance Criteria:
- Given fixed seed and identical inputs, when encounter resolves, then results/events are identical.
- Given elite-tagged encounter participants, when outcome effects are computed, then elite multipliers are applied where specified.
- Given malformed party input, when resolution is attempted, then explicit validation failure is returned.

Implementation Tasks:
- Define encounter domain contracts.
- Implement deterministic resolve pipeline.
- Add spawn-band and invalid-input tests.

Test Cases:
- Deterministic replay test.
- Elite multiplier outcome test.
- Invalid input validation test.

Dependencies:
- Monster content baselines.
- Formula order rules.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Advanced AI sophistication deferred; baseline only.

## Ticket: Loot Resolution and Inventory Handoff

Ticket ID: S2-T06
Epic: Playable Core Loop Vertical Slice
Feature Area: Inventory and Loot Lifecycle
Priority: Must Have
Sprint: Sprint 2
Status: Modified
Source References:
- docs/planning/actionable-backlog.md (Recommended Next Sprint item 6)
- Docs/05 - Loot Tables  Value  Attraction.md
- Docs/19 - Content_Pipeline_and_Data_Authoring.md
- Docs/23 - Player_Inventory_Storage_and_Item_Lifecycle.md

User Story:
As a player,
I want encounter outcomes to generate valid loot and inventory updates,
so that progression rewards are meaningful and consistent.

Functional Requirements:
- Resolve loot from data-driven tables.
- Map encounter outcome to extraction results.
- Apply inventory lifecycle updates for awarded loot.

Technical Requirements:
- Content reference integrity checks.
- Deterministic resolver output for fixed seed/input.
- Edge handling for empty/invalid/capped tables.

Acceptance Criteria:
- Given valid loot table references, when encounter reward resolves, then output loot matches table rules.
- Given empty or capped table edge case, when resolution runs, then deterministic fallback behavior is applied.
- Given invalid table reference, when resolution runs, then safe failure is surfaced and no corrupted inventory mutation occurs.

Implementation Tasks:
- Implement loot table resolver and extraction map.
- Integrate inventory handoff for resolved rewards.
- Add integrity and edge-case tests.

Test Cases:
- Valid table resolution test.
- Empty/capped edge-case behavior test.
- Invalid reference safe-failure test.

Dependencies:
- S2-T05 encounter outcomes.
- Content validation schema checks.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Loot tiers remain capped at MVP limits.

## Ticket: Trust UI + Localization for Pending/Restricted States

Ticket ID: S2-T07
Epic: Playable Core Loop Vertical Slice
Feature Area: UI/UX Transparency, Error Handling, and Onboarding
Priority: Must Have
Sprint: Sprint 2
Status: Existing
Source References:
- docs/planning/actionable-backlog.md (Recommended Next Sprint item 7)
- Docs/15 - UI Information Exposure and Player Trust.md
- Docs/27 - Localization_and_Text_System.md
- Docs/35 - Error_Handling_Player_Messaging_and_Trust.md

User Story:
As a player,
I want clear, localized messaging for pending/restricted actions and outcomes,
so that I understand what happened and what to do next.

Functional Requirements:
- Show pending verification states for research/restricted actions.
- Show core loop outcome summaries for encounter + loot.
- Route all new player-facing text through localization keys.

Technical Requirements:
- No hardcoded player-facing strings in new surfaces.
- Message-state mapping for pending/error/restricted states.
- UI verification checklist artifacts.

Acceptance Criteria:
- Given a pending verification action, when player views relevant UI, then pending state and next-step hint are displayed via localized text.
- Given restricted action attempt, when blocked, then reason and recovery message are shown consistently.
- Given missing localization key, when UI renders, then safe fallback path is used and key-coverage check fails build/test as configured.

Implementation Tasks:
- Implement pending/restricted/outcome UI bindings.
- Add/route localization keys.
- Add UI verification checklist + key coverage checks.

Test Cases:
- Pending-state message visibility test.
- Restricted-action explanation consistency test.
- Localization key completeness/fallback test.

Dependencies:
- S2-T01, S2-T02 state outputs.
- Localization pipeline checks.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Onboarding deep polish deferred to Sprint 4.
