# M6-A0 Adventurer Demand Budget Resolver Scaffold

## Scope delivered
- Added pure `AdventurerDemandBudgetResolver` that consumes `RunAdventurerInterestForecastSummary` and emits `RunAdventurerDemandBudgetSummary`.
- Added persisted summary + named error code enum and attached summary to `RunOutcomeRecord`.
- Added config-owned demand budget rule id, scalar, and thresholds in `RunSimulationConfig` and bootstrap run config json.
- Integrated resolver in `RunSimulationService` after forecast resolution using same deterministic seed.
- Extended run simulation config validation for demand budget config fields.

## Explicit non-goals
- No adventurer AI, spawning, traffic simulation, party composition, pathing, combat, inventory, crafting, research, merchant, or economy loop behavior.
- No production UI or demand budget diagnostics lines.
- No forecast-driven gameplay effects.

## Tests added
- `AdventurerDemandBudgetResolverTests` (determinism, config validation, missing/failed forecast handling, threshold boundaries including non-monotonic medium/high rejection, overflow handling, persistence round-trip with strengthened `RecentOutcomes` value assertions, legacy-missing-field safety).
- `RunSimulationTests` updates for integration attach + strengthened derivation assertions from forecast summary/config + config validation.

## Tests to run before merge
- AdventurerDemandBudgetResolverTests
- AdventurerInterestForecastResolverTests
- AdventurerAttractionResolverTests
- RunSimulationTests
- LootExtractionResolverTests
- LootHeatCoolingResolverTests
- LootRollResolverTests
- MigrationRunnerTests (if present/relevant)

## Manual validation checklist
- [ ] Open Bootstrap scene.
- [ ] Enter Play Mode.
- [ ] Use dev run simulation control.
- [ ] Press F2 for run diagnostics focus mode.
- [ ] Confirm Run/Run history/Loot/Survival/Extraction/Heat cooling/Attraction/Forecast lines still display.
- [ ] Confirm no Demand Budget UI appears.
- [ ] Confirm no adventurer spawning/AI/traffic simulation/demand-driven behavior.
- [ ] Confirm no localization keys appear in normal loaded-content play.
- [ ] Confirm no unexpected `.meta` files were created.

## Guardrail confirmations
- No gameplay math changes to loot, survival, extraction, heat cooling, attraction, or interest forecast.
- No runtime player-facing English text hardcoded.
- All new demand budget tuning is config-owned.
- No demand-driven gameplay behavior was added.
