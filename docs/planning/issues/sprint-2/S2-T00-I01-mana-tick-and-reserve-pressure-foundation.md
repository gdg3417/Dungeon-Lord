# Issue: Mana Tick and Reserve Pressure Foundation

Parent Ticket: S2-T00
Issue ID: S2-T00-I01
Sprint: Sprint 2
Priority: Must Have
Type: Feature
Status: Ready

## Goal
Define and validate deterministic mana tick behavior with reserve pressure guards, formula-order compliance, and persistence-safe state updates.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T00)
- Docs/01 - Mana System v3.md
- Docs/02 - Heat System v3.md
- Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md
- Docs/29 - Time_Model_and_Tick_Resolution.md
- Docs/30 - Formula_Framework_and_Modifier_Stacking_Rules.md

## Requirements
- Compute base mana generation, floor contribution, adventurer death mana, and skill spill hooks.
- Apply elite multiplier handling where applicable.
- Apply heat efficiency modifier in locked stacking order.
- Maintain Total, Reserved, and Usable pools with invariant-safe updates.
- Set fragile state when Reserved exceeds incoming generation.
- Persist and restore mana pools and fragile-state fields.

## Acceptance Criteria
- Given fixed input state and seed, when mana tick runs twice, then Total/Reserved/Usable outputs match exactly.
- Given Reserved greater than incoming generation, when tick applies, then fragile state is set and usable mana is never negative.
- Given mixed source inputs, when tick resolves, then source stacking order follows Spec 30.
- Given save/load cycle, when state is restored, then mana fields and fragile-state flags remain unchanged.

## Implementation Notes
- Keep source contribution fields explicit for debugging and traceability.
- Avoid adding non-MVP mana systems.
- Keep formulas data-driven where practical.

## Tests Required
- Deterministic equal-input replay test.
- Reserved pressure fragile-state boundary test.
- Source stacking order conformance test.
- Save/load round-trip persistence test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- Sprint 1 evidence gate.
- Heat state input contract or provisional MVP heat constants; full HeatSystem implementation remains S2-T03.

## Non-Goals
- Late-game economy tuning passes.
- Additional sub-dungeon mana systems.
