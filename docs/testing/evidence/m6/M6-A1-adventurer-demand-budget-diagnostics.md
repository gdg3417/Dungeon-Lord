# M6-A1 Adventurer Demand Budget Diagnostics

## Scope delivered
- Added a Bootstrap developer diagnostics line for Adventurer Demand Budget summary state.
- Wired the new line into both full diagnostics overlay and F2 run-diagnostics-only mode, positioned after Forecast.
- Added localization key `ui.run.adventurer_demand_budget_summary_format` in Bootstrap string table.
- Added/updated EditMode tests for localized formatting, key fallback behavior, null-summary safety, and stale-line clearing when switching outcomes.

## Explicit non-goals
- No adventurer AI, spawning, traffic simulation, party composition, pathing, or combat.
- No production UI or player-facing feature screens.
- No demand-driven gameplay effects or economy loop behavior.

## Tests added
- `RefreshRunLine_AdventurerDemandBudgetSummary_WithNullContent_UsesKeyFallbackSafely`
- `RefreshRunLine_AdventurerDemandBudgetSummary_MissingLocalizationKey_UsesKeyFallbackSafely`
- `RefreshRunLine_AdventurerDemandBudgetSummary_ValidOutcome_IsDisplayed`
- `RefreshRunLine_SwitchingOutcomes_UpdatesAndClearsAdventurerDemandBudgetLine`
- Updated `RefreshRunLine_LegacyNullLootSummary_IsSafeAndEmpty` to assert the demand budget line is also empty.

## Tests to run before merge
- RunSimulationTests (EditMode)
- AdventurerDemandBudgetResolverTests
- AdventurerInterestForecastResolverTests
- AdventurerAttractionResolverTests
- LootExtractionResolverTests
- LootHeatCoolingResolverTests
- LootRollResolverTests
- MigrationRunnerTests (if present/relevant)

## Manual Bootstrap smoke checklist
1. Open Bootstrap scene.
2. Enter Play Mode.
3. Use dev run simulation control.
4. Press F2 for run diagnostics focus mode.
5. Confirm visible lines: Run, Run history, Loot, Survival, Extraction, Heat cooling, Attraction, Forecast, Demand Budget.
6. Confirm Demand Budget line displays resolved status, error code, forecast score, forecast band, demand score, and demand band.
7. Confirm no adventurer spawning or traffic behavior occurs.
8. Confirm no localization keys appear during normal loaded-content play.
9. Confirm no unexpected `.meta` files are created.

## Confirmations
- No gameplay math changed (loot, survival, extraction, heat cooling, attraction, forecast, or demand budget formulas unchanged).
- No adventurer AI/spawning/traffic/party/pathing/combat added.
- No runtime player-facing English hardcoded in C# for this feature; diagnostics format is localized via string table key.
- No demand-driven behavior added.
- Demand Budget line appears only in Bootstrap developer diagnostics surfaces.
