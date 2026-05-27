# M5-H1 Adventurer Interest Forecast Diagnostics Evidence

## Scope delivered
- Exposed `RunAdventurerInterestForecastSummary` in Bootstrap developer diagnostics via a new forecast line on `GameRoot` and render wiring in `BootstrapOverlay`.
- Added localization key `ui.run.adventurer_interest_forecast_summary_format` in `Assets/_Project/Data/Bootstrap/string_table_en.json`.
- Added EditMode coverage for forecast display, key fallback, and stale-line clearing behavior.

## Explicit non-goals
- No adventurer AI, spawning, traffic simulation, party composition, pathing, combat, inventory, crafting, research, merchant traffic, economy loops, rewards distribution, production UI, or player-facing feature screens.
- No changes to loot generation, survival, extraction, heat cooling, attraction, or forecast math.

## Tests added
- `RefreshRunLine_AdventurerInterestForecastSummary_WithNullContent_UsesKeyFallbackSafely`
- `RefreshRunLine_AdventurerInterestForecastSummary_MissingLocalizationKey_UsesKeyFallbackSafely`
- `RefreshRunLine_AdventurerInterestForecastSummary_ValidOutcome_IsDisplayed`
- `RefreshRunLine_EmptyFeedback_ClearsStaleAdventurerInterestForecastLine`

## Tests to run before merge
- RunSimulationTests
- AdventurerInterestForecastResolverTests
- AdventurerAttractionResolverTests
- LootExtractionResolverTests
- LootHeatCoolingResolverTests
- LootRollResolverTests
- MigrationRunnerTests

## Manual Bootstrap smoke checklist
- [ ] Open Bootstrap scene.
- [ ] Enter Play Mode.
- [ ] Use dev run simulation control.
- [ ] Press F2 for run diagnostics focus mode.
- [ ] Confirm visible lines: Run, Run history, Loot, Survival, Extraction, Heat cooling, Attraction, Forecast.
- [ ] Confirm Forecast line displays resolved, error code, signal, score, band.
- [ ] Confirm no adventurer spawning or traffic behavior occurs.
- [ ] Confirm no localization keys appear during normal loaded-content play.
- [ ] Confirm no unexpected .meta files are created.

## Confirmations
- No gameplay math changed.
- No adventurer AI, spawning, traffic simulation, party composition, pathing, or combat was added.
- No runtime player-facing English was hardcoded in C# for this feature.
- No forecast-driven behavior was added.
- Forecast line appears only in developer diagnostics overlay.
