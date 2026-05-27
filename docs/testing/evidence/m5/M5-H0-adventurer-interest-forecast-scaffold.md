# M5-H0 Adventurer Interest Forecast Resolver Scaffold

## Scope delivered
- Added pure `AdventurerInterestForecastResolver` that consumes `RunAdventurerAttractionSummary` and produces deterministic forecast summary only.
- Added persisted `RunAdventurerInterestForecastSummary` and attached it to `RunOutcomeRecord` for save/load-safe serialization.
- Added config-owned forecast tuning fields in `RunSimulationConfig` and validation in `GameRoot.IsValidRunSimulationConfig`.
- Integrated resolver into `RunSimulationService` without spawning/scheduling behavior.
- Fixed `RunSimulationTests.SimulateOnce_AttachesResolvedAdventurerInterestForecastSummary_FromAttractionSummary` to assert forecast band using the computed score + configured thresholds (integration-safe expectation).
- Added a minimal Bootstrap diagnostics inspectability improvement: `F2` toggles a run-diagnostics-focused overlay mode so run, history, loot, survival, extraction, heat-cooling, and adventurer-attraction lines are directly viewable during smoke validation.

## Explicit non-goals
- No adventurer AI, spawning, traffic simulation, party/pathing/combat, or economy loop additions.
- No forecast UI, production UI, or player-facing feature screen additions. Only a dev diagnostics focus toggle was added to unblock smoke validation.
- No attraction, loot, extraction, survival, or heat-cooling formula changes.

## Tests added
- `AdventurerInterestForecastResolverTests`:
  - Determinism and attraction-input usage.
  - Null/missing/failed attraction summary handling.
  - Invalid forecast config handling.
  - Threshold edge band selection.
  - NaN/Infinity/overflow handling.
- `RunSimulationTests` additions:
  - Invalid adventurer interest forecast config validation cases.

## Tests to run before merge
- `dotnet test Dungeon-Lord.sln --filter AdventurerInterestForecastResolverTests`
- `dotnet test Dungeon-Lord.sln --filter AdventurerAttractionResolverTests`
- `dotnet test Dungeon-Lord.sln --filter RunSimulationTests`
- `dotnet test Dungeon-Lord.sln --filter LootExtractionResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootHeatCoolingResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootRollResolverTests`
- `dotnet test Dungeon-Lord.sln --filter MigrationRunnerTests`

## Unity rerun status
- Prior Unity result before this fix: 168 total, 167 passed, 1 failed (`SimulateOnce_AttachesResolvedAdventurerInterestForecastSummary_FromAttractionSummary`) due to outdated expected band.
- The integration test expectation was corrected so `ForecastBandId` is derived from computed score and configured thresholds.
- All required Unity test scripts passed locally.
- Bootstrap smoke test passed locally.
- F2 run diagnostics focus mode was verified in Play Mode.
- Run, history, loot, survival, extraction, heat cooling, and attraction lines are visible.
- No forecast UI appears.
- No localization keys appeared during normal loaded-content play.
- No unexpected `.meta` files were created.

## Manual validation checklist
- [x] Open Bootstrap scene.
- [x] Enter Play Mode.
- [x] Use dev run simulation control.
- [x] Confirm existing run/loot/survival/extraction/heat-cooling/attraction lines display unchanged.
- [x] Confirm no new forecast UI is visible.
- [x] Confirm no localization keys appear during normal loaded-content play.
- [x] Confirm no unexpected `.meta` files are created.
- [x] Press `F2` to toggle run-diagnostics focus mode and verify required lines are visible.

## Confirmations
- No gameplay math changes to loot generation, survival, extraction, heat cooling, or attraction.
- No adventurer AI/spawning/traffic/party/pathing/combat added.
- No runtime player-facing English strings were added.
- All new forecast tuning values are config-owned and validated.
