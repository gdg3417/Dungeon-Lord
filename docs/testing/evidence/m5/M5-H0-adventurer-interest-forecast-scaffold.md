# M5-H0 Adventurer Interest Forecast Resolver Scaffold

## Scope delivered
- Added pure `AdventurerInterestForecastResolver` that consumes `RunAdventurerAttractionSummary` and produces deterministic forecast summary only.
- Added persisted `RunAdventurerInterestForecastSummary` and attached it to `RunOutcomeRecord` for save/load-safe serialization.
- Added config-owned forecast tuning fields in `RunSimulationConfig` and validation in `GameRoot.IsValidRunSimulationConfig`.
- Integrated resolver into `RunSimulationService` without spawning/scheduling behavior.

## Explicit non-goals
- No adventurer AI, spawning, traffic simulation, party/pathing/combat, or economy loop additions.
- No UI/Bootstrap overlay additions in this PR.
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

## Tests run
- `dotnet test Dungeon-Lord.sln --filter AdventurerInterestForecastResolverTests`
- `dotnet test Dungeon-Lord.sln --filter AdventurerAttractionResolverTests`
- `dotnet test Dungeon-Lord.sln --filter RunSimulationTests`
- `dotnet test Dungeon-Lord.sln --filter LootExtractionResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootHeatCoolingResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootRollResolverTests`
- `dotnet test Dungeon-Lord.sln --filter MigrationRunnerTests`

## Manual validation checklist
- [ ] Open Bootstrap scene.
- [ ] Enter Play Mode.
- [ ] Use dev run simulation control.
- [ ] Confirm existing run/loot/survival/extraction/heat-cooling/attraction lines display unchanged.
- [ ] Confirm no new forecast UI is visible.
- [ ] Confirm no localization keys appear during normal loaded-content play.
- [ ] Confirm no unexpected `.meta` files are created.

## Confirmations
- No gameplay math changes to loot generation, survival, extraction, heat cooling, or attraction.
- No adventurer AI/spawning/traffic/party/pathing/combat added.
- No runtime player-facing English strings were added.
- All new forecast tuning values are config-owned and validated.
