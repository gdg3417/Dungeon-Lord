# M5-G1 Adventurer Attraction Diagnostic Closeout

## Scope delivered
- Added a Bootstrap developer overlay diagnostic line for persisted `RunAdventurerAttractionSummary` display.
- Wired the attraction line through `GameRoot.RefreshRunLine()` and `BootstrapOverlay` so it is visibly rendered in the developer diagnostics surface.
- Added localization key `ui.run.adventurer_attraction_summary_format` to English string table and used key-safe fallback behavior.
- Added EditMode coverage for valid display, null Content fallback, missing-key fallback, legacy null-summary safety, and stale-line prevention.

## Explicit non-goals
- No adventurer AI, traffic simulation, party composition, pathing, combat, merchant systems, inventory, crafting, research, economy loops, or production UI were implemented.
- No changes were made to run success math, loot generation, survival resolution, extraction rules, heat cooling rules, or adventurer attraction resolver math.

## Tests added
- `RefreshRunLine_AdventurerAttractionSummary_ValidOutcome_IsDisplayed`
- `RefreshRunLine_AdventurerAttractionSummary_WithNullContent_UsesKeyFallbackSafely`
- `RefreshRunLine_AdventurerAttractionSummary_MissingLocalizationKey_UsesKeyFallbackSafely`
- `RefreshRunLine_EmptyFeedback_ClearsStaleAdventurerAttractionLine`
- Updated legacy null summary safety assertions to include attraction line clearing.

## Tests run
- `dotnet test Dungeon-Lord.sln --filter RunSimulationTests`
- `dotnet test Dungeon-Lord.sln --filter AdventurerAttractionResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootExtractionResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootHeatCoolingResolverTests`
- `dotnet test Dungeon-Lord.sln --filter LootRollResolverTests`
- `dotnet test Dungeon-Lord.sln --filter MigrationRunnerTests`

## Manual Bootstrap smoke test checklist
- [ ] Open Bootstrap scene.
- [ ] Enter Play Mode.
- [ ] Use dev run simulation control.
- [ ] Confirm run, loot, survival, extraction, heat cooling, and attraction diagnostic lines display.
- [ ] Confirm no localization keys appear during normal loaded-content play.
- [ ] Confirm localization key fallback appears only in null Content or missing-key test scenarios.
- [ ] Confirm no unexpected `.meta` files were created.

## Compliance confirmations
- No gameplay math changed.
- No hardcoded runtime player-facing English added; attraction diagnostics resolve through localization key.
- Gameplay tuning remains config-owned; no new runtime tuning constants were added.
