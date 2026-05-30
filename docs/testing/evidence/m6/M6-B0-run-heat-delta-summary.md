# M6-B0 Run Heat Delta Summary Evidence

## Motivation
Add deterministic, config-driven run heat delta summary calculation persisted on each run outcome without mutating global heat.

## Implemented
- Added `RunHeatDeltaResolver` pure resolver.
- Added persisted `RunHeatDeltaSummary` and attached it to `RunOutcomeRecord`.
- Added config fields for run heat delta tuning and rule source id.
- Integrated resolver invocation in `RunSimulationService` after survival, loot extraction, and adventurer demand budget summaries are available.
- Added config validation entries in `GameRoot.IsValidRunSimulationConfig`.
- Added JSON round-trip and legacy missing field safety test coverage.

## Corrections Before Merge
- Removed the incorrect elite-death inference from adventurer demand budget. Demand budget remains a planning/forecast signal, not actual death data.
- Elite death heat remains `0` in this PR because no explicit elite-death count exists on the current run outcome or survival model. The configured elite death heat value will activate only when a future PR adds explicit elite death data.
- Survival cooling only applies when the current survival summary proves the full party survived (`PartySize > 0` and `SurvivorCount == PartySize`). Partial survival no longer creates survival cooling.
- Loot cooling still applies when survivors extracted tradeable loot, and full wipes still suppress loot cooling.
- Overflow failures return stable failed summaries with finite numeric fields so persisted data remains save/load safe.
- Bootstrap defaults now match the locked heat spec values for normal death, elite death, and multiple-death bonus.

## Notes
- This ticket computes and persists a per-run heat delta only.
- This ticket does not apply the delta to current/global dungeon heat state.
- Legacy saves with outcomes lacking `RunHeatDeltaSummary` deserialize safely without manual migration; Unity may produce either null or a default unresolved summary with finite numeric fields.
