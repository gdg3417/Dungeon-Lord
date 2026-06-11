# Sprint 2A Kickoff Plan - Dungeon Lord Vertical Slice

> **Post-GD9 planning note (PR #99 merged):** This Sprint 2A plan is historical context for the earlier structure-slot foundation. Active near-term implementation planning is now the GD10-GD15 sequence in `docs/planning/actionable-backlog.md`, starting with GD10: Deterministic MVP placement effects resolver. The current MVP placement categories are Room, Monster, Trap, and Loot node; future work should make those categories mechanically meaningful rather than continue generic structure-slot or Bootstrap-only scaffolding.


## Active Ticket and Scope Anchor
- **Active Ticket**: `S2-T00A-I01` (Dungeon layout and placement MVP foundation).
- **Sprint Goal**: Deliver the minimum playable dungeon-management gameplay loop while preserving all Sprint 1 deterministic and infrastructure invariants.
- **Non-goals**: Procedural generation, content scale-up, deep combat sim, production UI polish, monetization, meta progression, multiplayer/networking.

## Sprint 2A Execution Rules
1. Work must be completed strictly in execution order; later-phase work cannot start early.
2. PR-B cannot begin until PR-A is merged and validated.
3. PR-C cannot begin until PR-B is merged and validated.
4. Preserve Sprint 1 determinism, save, and migration invariants at all times.
5. Any feature not documented in this plan is out of scope for Sprint 2A.
6. Procedural generation remains forbidden during Sprint 2A.
7. Sprint gating is mandatory: Sprint 1 closeout evidence must be confirmed before Sprint 2A implementation starts.
8. Gameplay tuning values are data-owned and must come from content/config, not runtime constants.
9. Player-facing text is localization-owned and must come from localization/string tables.
10. Hardcoded English strings in runtime/UI are forbidden unless they are test-only or internal non-user logs.

## Active Sprint 2A Scope

### Phase 1: Data + Domain Foundations
1. Add deterministic dungeon slot model (`DungeonSlotState`) with fixed-size index-based slots.
2. Add structure catalog with exactly 3 initial structures:
   - `structure.mana_generator.basic`
   - `structure.heat_scrubber.basic`
   - `structure.risk_lab.basic`
3. Move all structure tuning into data/config (not hardcoded gameplay constants).
4. Extend save schema to persist:
   - slot occupancy and structure IDs
   - per-structure activation state
   - structure cooldown/tick bookkeeping if needed
5. Add migration to safely default existing saves to empty slot layout state.

### Phase 2: Deterministic Simulation Integration
1. Register structure systems in existing simulation order under `SimulationClock`.
2. Add deterministic per-tick structure resolution:
   - mana generator yields mana at configured cadence
   - heat scrubber reduces heat at configured cadence/cost
   - risk lab increases heat + advances gate/progression pressure
3. Enforce stable ordering rules:
   - slot index ascending
   - structure stable ID tie-break
4. Route all state mutation through existing gate/restriction and telemetry hooks.

### Phase 3: Pressure + Failure State
1. Implement one hard pressure mode: **Heat Crisis Lockout**.
2. Trigger condition: heat >= crisis threshold for N consecutive ticks.
3. Effect:
   - placement temporarily disabled
   - risk lab auto-paused
   - mana upkeep drain increased
4. Recovery condition: heat < recovery threshold for M ticks.
5. Ensure this interacts with verification gating and is fully save/load safe.

### Phase 4: Minimal Interaction Layer
1. Extend dev/debug overlay with a simple dungeon panel:
   - select slot
   - place structure
   - activate/deactivate
   - show current heat/mana/crisis status
2. Use localization keys for any player-facing strings.
3. Keep implementation temporary/debug-styled and explicitly non-polished.

## Deferred Sprint 2A Extensions

### Future Sprint 2A Extension (Post S2-T00A-I01 Scope)
These items are intentionally deferred until the deterministic layout and placement foundation is stable and validated.
1. Add first milestone: `milestone.structure_slot_4_unlock`.
2. Unlock condition example (data-driven): survive X ticks while maintaining heat below crisis threshold and spending Y mana.
3. Unlock effect: enable one additional slot **or** unlock `risk_lab` (pick exactly one in implementation ticket and keep scope fixed).

## 2) Architectural Notes

### Domain Boundaries
- **Simulation domain**: slot state, structure resolution, pressure/failure logic.
- **Infrastructure domain**: save root, migration runner, telemetry/event wiring, gate services.
- **Presentation domain**: debug overlay input/output only; no gameplay logic.

### Determinism Strategy
- Tick-driven updates only via `SimulationClock`.
- No wall-clock reads in domain logic.
- No unordered iteration over maps/dictionaries without canonical ordering.
- All random behavior forbidden in Sprint 2A systems.

### Data Ownership
- Structure definitions and tuning live in content assets/tables.
- Tuning values are data-owned and consumed via injected/loaded config models.
- Player-facing text is localization-owned; runtime/UI cannot hardcode English user-visible strings.
- Save stores stable IDs + dynamic state only.
- Migrations own schema transition logic; gameplay systems do not backfill legacy data.

## 3) Risk List
1. **Scope Creep Risk**: Adding extra structures/mechanics.
   - Mitigation: hard cap at 3 structures + 1 pressure state.
2. **Determinism Regression**: nondeterministic iteration or UI-trigger race.
   - Mitigation: command queue applied at tick boundary + deterministic ordering tests.
3. **Save Compatibility Risk**: schema drift breaking Sprint 1 saves.
   - Mitigation: explicit migration + regression fixture from Sprint 1 save.
4. **Tuning Instability Risk**: early loop feels flat or impossible.
   - Mitigation: config-driven tuning and one bounded rebalance pass only.
5. **Gate Integration Risk**: bypassing verification/restriction path.
   - Mitigation: enforce all actions through gate service adapter and telemetry assertions.

## 4) Proposed File Structure (Incremental)

- `Runtime/Gameplay/Dungeon/`
  - `DungeonSlotState.*`
  - `DungeonLayoutState.*`
  - `DungeonPlacementService.*`
- `Runtime/Gameplay/Structures/`
  - `StructureDefinition.*`
  - `StructureRuntimeState.*`
  - `StructureSimulationSystem.*`
- `Runtime/Gameplay/Pressure/`
  - `HeatCrisisService.*`
- `Runtime/Gameplay/Progression/`
  - `MilestoneService.*` (deferred post `S2-T00A-I01`)
- `Runtime/Save/`
  - `SaveRoot` extensions for slot/structure/progression state
- `Runtime/Save/Migrations/`
  - `Migration_00X_Sprint2A_DungeonSlots.*`
- `Runtime/UI/DevOverlay/`
  - `DungeonSlicePanel.*`
- `Content/SourceTables/`
  - `structures.csv` (or project equivalent)
  - `milestones.csv` (deferred post `S2-T00A-I01`)
- `Tests/Unit/Gameplay/`
  - `DungeonPlacementDeterminismTests.*`
  - `StructureSimulationDeterminismTests.*`
- `Tests/Unit/Save/`
  - `DungeonSliceSaveLoadTests.*`
- `Tests/SIT/`
  - `Sprint2A_DungeonSliceFlowTests.*`

## 5) Ordered Execution Tasks
1. Finalize ticket acceptance boundaries and invariants checklist.
2. Implement slot model + serialization skeleton.
3. Add migration from Sprint 1 saves.
4. Add structure definitions and deterministic simulation pass.
5. Add crisis pressure state integration.
6. Wire minimal overlay interaction panel.
7. Add/expand telemetry events for placement, crisis enter/exit, and core structure actions.
8. Implement determinism + save/load + SIT tests.
9. Run unit/SIT, collect UAT evidence, and gate against Sprint matrix.

## 6) Suggested PR Breakdown
1. **PR-A (Foundation)**
   - Slot state + save schema + migration.
   - Determinism tests for placement ordering.
2. **PR-B (Structures + Pressure)**
   - 3 structures + simulation integration + heat crisis.
   - Unit tests for reproducibility.
3. **PR-C (Playable Slice UI + Progression)**
   - Debug interaction panel + SIT flow.
   - UAT evidence + sprint checklist links.

> Keep each PR small, test-backed, and linked to Sprint 2 execution order.

## PR Dependency Contracts
PR-A dependencies:
- none

PR-B dependencies:
- merged PR-A
- passing determinism tests
- validated migration behavior

PR-C dependencies:
- merged PR-B
- validated pressure state behavior
- SIT pass for structure simulation

## Tests

### Unit
- Deterministic slot placement with identical command sequence yields identical state hash.
- Structure tick outcomes reproducible over fixed tick count.
- Crisis threshold enter/exit hysteresis behaves exactly at boundary values.

### Save/Migration
- Sprint 1 save migrates to Sprint 2A schema with empty default slots.
- Save/load round-trip preserves slot occupancy, activation, and crisis counters.

### SIT
- End-to-end flow: place generator -> add risk lab -> trigger crisis -> recover with heat scrubber.

### UAT (evidence)
- Manual scripted playthrough proving meaningful decision:
  - short-term mana gain via risk lab vs long-term stability via heat scrubber.

## Acceptance Criteria
Sprint 2A is done when all are true:
1. Player can place/activate structures in deterministic slot model.
2. At least one emergent decision exists (greed vs stability).
3. Heat-driven failure/pressure state can be entered and recovered from.
4. State fully survives save/load and migration from Sprint 1.
5. Repeated fixed-input simulations are reproducible.
6. Vertical slice is measurably more interactive and engaging than Sprint 1 infrastructure runtime.

## Sprint 2A Definition of Done
Sprint 2A is complete only when all conditions are true:
1. All required Unit, Save/Migration, SIT, and UAT evidence checks pass.
2. Deterministic replay/reproducibility is verified for fixed-input runs.
3. Save migration from Sprint 1 succeeds with validated post-migration state.
4. UAT evidence artifacts are committed according to sprint evidence workflow.
5. PR-A, PR-B, and PR-C are all merged in required execution order.
6. No blocked Sprint 2A dependencies remain.

## Explicitly Deferred Systems
The following systems are intentionally excluded from Sprint 2A:
1. Procedural generation.
2. Enemy AI expansion.
3. Production UI polish.
4. Multiplayer/networking.
5. Monetization systems.
6. Meta progression systems.
7. Automation scripting.
8. Large content pipelines.

## 9) Architectural Decision Record Notes (to capture during implementation)
- ADR: deterministic slot index ordering contract.
- ADR: crisis lockout as sole Sprint 2A pressure mode.
- ADR: progression unlock limited to exactly one additional strategic option.

## 10) Immediate Next Action
After Sprint 1 closeout evidence is confirmed and Sprint 2A is formally UNBLOCKED, begin `S2-T00A-I01` implementation branch work for deterministic dungeon layout and placement foundation before structure behavior expansion.
