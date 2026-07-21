# Actionable Game Development Backlog

> **Historical / partially completed / superseded execution order (GD61).** This backlog preserves acceptance criteria and source traceability. Its post-GD9 GD10-GD15 sequence was delivered and extended through GD60, but it is no longer the authoritative forward order. Current disposition and remaining work are governed by the [post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md) and [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md). Items without repository evidence require confirmation; do not infer completion from a planned status.

## GD61 disposition

| Historical workstream | Implementation status / GD evidence | Remaining gap | Current disposition |
|---|---|---|---|
| GD10-GD15 placement, outcomes, layout, simple editor, loot, research bridge | Completed in PRs #101, #103, #105-#108 | Spatial construction and production editor were explicit non-goals | Historical; superseded by Phases 1-8 |
| Room-slot and two-room evolution | Partially completed in PRs #130-#142 and #158 (GD60) | Physical footprints, corridors, graph persistence, reachability and capacity | Migrate only after Phase 1 contracts |
| Player-facing loop and analysis | Completed/expanded through PRs #113-#157 (GD20-GD59) | Graphical construction parity, onboarding and external fun evidence | Retain; validate in Phases 7-9 |
| Mana/offline, multiple floors, content breadth | Partial or remaining; exact old-ticket closure is not consistently evidenced here | Construction economy, additional-floor traversal, breadth and balance | Re-sequenced into Phases 4, 6, 8 and 9 |
| Backend, live operations, prestige, monetization, release dashboards | Deferred | Not needed to prove the core fantasy | Post-MVP unless a concrete MVP dependency is demonstrated |

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


## Planning Update: Post-GD9 Gameplay Direction

_Status basis: PR #99 merged; GD9 is complete._

GD9 completed the MVP dungeon placement category scaffold. The current placement categories are now represented as:

- Room
- Monster
- Trap
- Loot node

This is a scaffold, not the final dungeon editor. Future work must make these categories mechanically meaningful by connecting dungeon composition to placement effects, run outcomes, loot, heat, mana pressure, and research progression. The next recommended implementation PR is **GD10: Deterministic MVP placement effects resolver**.

### Stop doing endless scaffolding

Future PRs should usually answer: **“What can the player do now that feels more like building or running a dungeon?”**

Avoid scaffold-only PRs unless they directly unlock a playable feature in the next one or two PRs. Diagnostics, smoke helpers, internal-only labels, future-ready models, and Bootstrap-only presentation cleanup should support playable dungeon-building validation, not dominate the roadmap. Runtime work must continue to obey repository guardrails: gameplay tuning stays in config/data or typed config assets, player-facing text stays localization-owned, deterministic tests remain required for deterministic systems, save compatibility is preserved, and post-MVP systems remain deferred.

### Revised near-term roadmap: GD10-GD15

#### GD10: Deterministic MVP placement effects resolver

Goal:
Make Room, Monster, Trap, and Loot node mechanically affect the next run.

Player-facing value:
The player should see that dungeon composition changes mana, loot, heat, danger, and/or adventurer results.

Scope:
- Add a deterministic placement effects resolver.
- Room contributes path/capacity context.
- Monster contributes danger and mana pressure.
- Trap contributes danger, heat, and path pressure.
- Loot node contributes loot and adventurer attraction context.
- Show effects in summary, compact smoke, copied smoke text, and run explanation.

Non-goals:
- No combat AI.
- No grid editor.
- No drag/drop.
- No new monster families.
- No research unlocks yet.
- No raids or `Hostile`/`Raid` heat tiers.
- No production UI rewrite.

Acceptance criteria:
- Given the same save state, placement catalog/config, posture, party inputs, and deterministic seed or no-RNG path, when placement effects resolve twice, then outputs are identical.
- Given at least one Room, Monster, Trap, and Loot node starter placement, when the next run summary is generated, then the summary exposes each category's contribution to mana pressure, loot, heat, danger, path/capacity, attraction, or adventurer-result context as applicable.
- Given placement effects are shown in summary, compact smoke, copied smoke text, and run explanation, when text is player-facing, then it uses localization keys or table references rather than hardcoded English.
- Given tuning values for placement effects, when runtime resolves effects, then numeric gameplay tuning is consumed from config/data or typed config assets, not hardcoded in runtime logic.
- Given a legacy or empty placement state, when effects resolve, then the resolver returns a safe deterministic fallback and preserves save compatibility.

Test expectations:
- Add or update deterministic EditMode coverage for resolver repeatability and category contribution mapping.
- Add or update tests proving empty/legacy placement states remain safe.
- Add or update coverage for summary/run-explanation output paths touched by GD10, including copied/compact smoke text if those paths are code-backed.
- Add or update localization/text guard coverage where player-facing strings change.
- Run existing Unity EditMode tests relevant to placement, run summary, smoke text, and save compatibility.

Merge gate:
- GD10 may merge only if Room, Monster, Trap, and Loot node all affect at least one observable run-facing output through deterministic, config-owned logic; all new player-facing text is localization-owned; no runtime tuning constants are introduced; no grid editor, combat AI, research unlock, loot-table expansion, raid tier, or UI rewrite scope is added; and relevant deterministic/save/localization tests pass or have documented environment-only limitations.

#### GD11: Run outcome uses dungeon composition

Goal:
Make run outcome resolve from dungeon composition instead of only existing Bootstrap run assumptions.

Player-facing value:
The player should understand how their room/monster/trap/loot setup affected the run.

Scope:
- Consume placement effects.
- Consume posture.
- Consume adventurer party.
- Resolve deterministic outcome.
- Explain loot, heat, mana, success/failure, and adventurer result.

Non-goals:
- No animated combat.
- No advanced AI.
- No full encounter timeline.
- No elite/hero systems unless already present and safe.

Acceptance criteria:
- Given different valid dungeon compositions with the same posture and party, when runs resolve, then run outcomes and explanations reflect the composition differences.
- Given the same composition, posture, party, config, and seed or deterministic path, when runs resolve twice, then outcome and explanation are identical.
- Given success, failure, retreat, or equivalent MVP adventurer-result states supported by existing systems, when displayed, then loot, heat, mana, and adventurer-result causes are explained with localized text.

Test expectations:
- Deterministic composition-to-outcome resolver tests.
- Composition/posture/party interaction tests for at least the MVP-supported outcome bands.
- Summary and explanation text tests for localization-key usage where applicable.
- Save compatibility regression coverage for run history or summary state touched by the PR.

Merge gate:
- GD11 may merge only if dungeon composition is an actual input to run resolution, the result is deterministic and explainable, and no advanced combat timeline, advanced AI, hero/elite expansion, or post-MVP feature is introduced.

#### GD12: Basic floor/node layout representation

Goal:
Introduce a simple floor/node layout representation.

Player-facing value:
The dungeon starts to feel spatial instead of being only a list of categories.

Scope:
- One floor.
- Small fixed number of nodes.
- Each node can hold one placement entry.
- Ordered path affects run resolver.
- Summary shows node/path order.

Non-goals:
- No production grid editor.
- No drag/drop.
- No pathfinding UI.
- No multi-floor UI yet.

Acceptance criteria:
- Given the MVP floor state, when placements are assigned to nodes, then each node holds no more than one placement entry.
- Given different ordered paths with the same entries, when the resolver runs, then path order can affect the deterministic run-facing summary or outcome.
- Given the floor summary is shown, when text is player-facing, then node/path order is communicated through localization-owned strings.
- Given existing saves, when loaded after GD12, then default node/floor state is additive and migration-safe.

Test expectations:
- Node occupancy and ordered-path determinism tests.
- Resolver interaction tests for path order.
- Save migration/default-state compatibility tests.
- UI/summary smoke coverage if a visible path-order surface changes.

Merge gate:
- GD12 may merge only if one-floor node/path data is deterministic, save-compatible, visibly summarized, and still avoids production grid, drag/drop, pathfinding UI, and multi-floor scope.

#### GD13: First simple dungeon editor view

Goal:
Create the first simple dungeon editor view.

Player-facing value:
The player can interact with a dungeon editing screen instead of only Bootstrap buttons.

Scope:
- Select a node.
- Choose Room, Monster, Trap, or Loot node.
- Place/replace starter option.
- Show current composition.
- Keep visuals simple.

Non-goals:
- No art polish requirement.
- No drag/drop unless trivial.
- No full production UI framework.
- No expanded content library.

Acceptance criteria:
- Given the editor view, when a player selects a node and starter category option, then the node placement updates or replaces deterministically.
- Given current composition, when the editor renders, then the player can see the placed categories and selected node state through localized text.
- Given invalid or unavailable placement options, when selected, then the view fails safely with localized feedback and does not corrupt save state.

Test expectations:
- Presenter/view-model tests for node selection, placement, replacement, and invalid selection.
- Localization coverage for new player-facing editor text.
- Manual smoke evidence for the editor flow if visual UI changes are made.
- Existing deterministic placement/run tests remain passing.

Merge gate:
- GD13 may merge only if the player can perform a basic node-select and place/replace flow without broad UI-framework work, art-polish requirements, expanded content libraries, or non-MVP interaction systems.

#### GD14: Loot table MVP

Goal:
Make loot node produce loot from a simple MVP loot table.

Player-facing value:
Loot becomes a real system rather than a generic output number.

Scope:
- Basic loot table.
- Deterministic generated/extracted/tradeable values.
- Visible loot profile.
- Loot affects heat/economy hooks.

Non-goals:
- No full inventory UI.
- No monetization.
- No marketplace.
- No advanced crafting.

Acceptance criteria:
- Given a Basic Loot Node or MVP-equivalent loot placement, when a run resolves, then generated, extracted, and tradeable loot values come from a data/config-owned loot table.
- Given identical inputs, when loot resolves twice, then outputs are deterministic.
- Given loot affects heat/economy hooks, when summary/explanation renders, then the player can see the loot profile and relevant consequences through localized text.

Test expectations:
- Loot-table resolution determinism tests.
- Generated/extracted/tradeable value mapping tests.
- Heat/economy hook regression tests for loot impact.
- Localization/text guard coverage for visible loot profile text.

Merge gate:
- GD14 may merge only if loot-node output is table-driven, deterministic, visible, and connected to existing heat/economy hooks without inventory UI, monetization, marketplace, or advanced crafting scope.

#### GD15: Research unlock bridge

Goal:
Connect research to dungeon-building progression.

Player-facing value:
Research unlocks or improves something real in the dungeon.

Scope:
- Single-slot research remains.
- One unlock or upgrade only.
- Example: improve Skeleton, Spike Trap, or Basic Loot Node.
- Summary shows unlocked/improved placement option.

Non-goals:
- No full tech tree UI.
- No multi-queue research.
- No online verification expansion.
- No monetization.

Acceptance criteria:
- Given the single MVP research unlock/upgrade completes through the existing safe lifecycle, when the dungeon summary/editor is shown, then one placement option is unlocked or improved.
- Given pre-unlock and post-unlock states, when placement effects or run summaries resolve, then the improvement is deterministic and data/config-owned.
- Given the unlock/improvement is visible, when text is player-facing, then it uses localization keys or table references.
- Given legacy saves without the research bridge state, when loaded, then they remain valid and default to the pre-unlock state.

Test expectations:
- Single-slot research bridge state tests.
- Pre/post-unlock placement effect tests.
- Save compatibility tests for missing bridge state.
- Localization guard coverage for unlock/improvement summaries.

Merge gate:
- GD15 may merge only if exactly one research unlock or upgrade is connected to dungeon building through the existing single-slot lifecycle, without full tech tree UI, multi-queue research, online verification expansion, monetization, or other post-MVP scope.

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
