# M5-G0 Adventurer Attraction Signal Scaffold Evidence

## Scope delivered
- Added deterministic, pure resolver seam: `AdventurerAttractionResolver`.
- Resolver consumes extraction-aware value from `RunLootExtractionSummary.TotalExtractedWorldValue`.
- Added `RunAdventurerAttractionSummary` persisted on `RunOutcomeRecord`.
- Added config-owned tuning fields for attraction rule source and scalar.

## Explicit non-goals respected
- No adventurer AI, composition, pathing, combat, inventory, crafting, research, merchant traffic, economy loops, or production UI.
- No changes to loot generation, survival, extraction, or heat cooling formulas.

## Tests added
- Determinism and extracted-world-value usage.
- Missing/failed extraction behavior.
- Invalid config behavior.
- Named error code behavior.
- Save/load persistence on LatestOutcome and RecentOutcomes including null legacy-safe entry.

## Manual validation expectations
- Run a simulation and inspect serialized run outcomes to confirm `AdventurerAttractionSummary` present for new runs.
- Load older saves lacking attraction summaries; verify runtime remains stable and outcomes inspectable.

## 2026-05-27 test execution update (PR 43 Safe Mode compile fix)
- Fixed Unity Safe Mode compile blocker by replacing non-existent `SaveStateV1` test usage with the repository's real save contract type `SaveData` in `AdventurerAttractionResolverTests`.
- Result: test now validates round-trip serialization via `SaveData` for both `LatestOutcome` and `RecentOutcomes`, including a legacy `null` `AdventurerAttractionSummary` entry.
- Environment note: Unity Editor/Safe Mode and EditMode test runner are not available in this CLI container, so compile/test execution must be verified in the project Unity environment.
