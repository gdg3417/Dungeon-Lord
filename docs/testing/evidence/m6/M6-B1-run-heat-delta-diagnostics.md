# M6-B1 Run Heat Delta Diagnostics Evidence

## Scope

This PR exposes the already-persisted `RunHeatDeltaSummary` through Bootstrap developer diagnostics only. The new localized Run Heat Delta line is rendered after Heat Cooling and before Attraction in the full Bootstrap overlay and the F2 run-diagnostics-only view.

## Guardrails preserved

- This PR does not apply the persisted run heat delta to global dungeon heat or current heat state.
- This PR does not alter `RunHeatDeltaResolver` behavior or heat math.
- This PR does not rebalance heat, loot, survival, attraction, forecast, or demand-budget calculations.
- Legacy outcomes that do not contain `RunHeatDeltaSummary`, unresolved summaries, and null outcomes leave the diagnostics line empty and clear any previously rendered value safely.
- The diagnostics format is stored in the Bootstrap English string table under `ui.run.heat_delta_summary_format`; runtime fallback remains localization-key safe when content or the key is unavailable.

## Automated EditMode coverage added

`RunSimulationTests` covers:

- resolved Run Heat Delta summary formatting;
- null-content localization-key fallback;
- missing-key localization-key fallback;
- legacy and unresolved summary clearing;
- stale-line clearing when switching to legacy and null outcomes;
- full Bootstrap diagnostics ordering; and
- F2 run-diagnostics-only ordering.

## Recommended Unity tests

Run these EditMode suites in Unity:

- `RunSimulationTests`
- `RunHeatDeltaResolverTests`
- `AdventurerDemandBudgetResolverTests`
- `AdventurerInterestForecastResolverTests`
- `AdventurerAttractionResolverTests`
- `LootExtractionResolverTests`
- `LootHeatCoolingResolverTests`
- `LootRollResolverTests`
- `MigrationRunnerTests`, if present

## Manual Bootstrap smoke test

1. Open the Bootstrap scene.
2. Run the usual smoke path.
3. Confirm run diagnostics show a Run Heat Delta line after Heat Cooling and before Attraction.
4. Confirm the F2 run-diagnostics focus still works.
5. Confirm no global heat application occurs from the persisted Run Heat Delta summary.
6. Confirm no stale Run Heat Delta line remains after switching to null or legacy outcome paths.
