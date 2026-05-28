# M6-B0 Run Heat Delta Summary Evidence

## Motivation
Add deterministic, config-driven run heat delta summary calculation persisted on each run outcome without mutating global heat.

## Implemented
- Added `RunHeatDeltaResolver` pure resolver.
- Added persisted `RunHeatDeltaSummary` and attached it to `RunOutcomeRecord`.
- Added config fields for run heat delta tuning and rule source id.
- Integrated resolver invocation in `RunSimulationService` after survival/loot extraction and demand budget summaries are available.
- Added config validation entries in `GameRoot.IsValidRunSimulationConfig`.
- Added JSON round-trip and legacy missing field safety test coverage.

## Notes
- This ticket does not apply the delta to current heat state.
- Legacy saves with outcomes lacking `RunHeatDeltaSummary` continue loading with null summary.
