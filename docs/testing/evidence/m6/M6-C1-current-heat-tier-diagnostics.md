# M6-C1 Current Heat Tier Snapshot and Bootstrap Diagnostics

## Scope

This PR derives the current MVP heat tier from active runtime heat with a deterministic, config-driven, read-only resolver. Bootstrap developer diagnostics render that current snapshot near the existing current runtime heat metric so the active state is visible independently from historical run outcomes.

The resolver consumes the existing config-owned Peace, Notice, and Concern bounds and the existing run heat application rule source identifier. It does not clamp or mutate heat. Non-finite values, invalid config, and values outside the configured MVP range return stable unresolved error codes.

## Explicit non-goals

- This PR is read-only and does not mutate heat.
- This PR does not implement raids, Hostile, Raid, raid warnings, offline heat processing, offline rebound, diplomacy, or reputation axes.
- This PR does not add gameplay effects from the resolved tier.
- Peace, Notice, and Concern remain the only active MVP heat tiers.
- Legacy saves remain compatible because the current tier is derived at runtime rather than persisted into save history.

## Recommended Unity EditMode tests

Run the Unity EditMode suite, with particular attention to:

- `CurrentHeatTierResolverTests`
- `CurrentHeatTierDiagnosticsTests`
- `RunSimulationTests`
- `RunHeatStateApplyResolverTests`
- `RunHeatDeltaResolverTests`
- `AdventurerDemandBudgetResolverTests`
- `AdventurerInterestForecastResolverTests`
- `AdventurerAttractionResolverTests`
- `LootExtractionResolverTests`
- `LootHeatCoolingResolverTests`
- `LootRollResolverTests`
- `MigrationRunnerTests`, if present
- `HeatSystemTests`, if present

## Manual Unity smoke test

1. Open the Bootstrap scene.
2. Run the usual active-run smoke path.
3. Confirm current heat tier diagnostics are visible near the current heat metric in full developer diagnostics.
4. Confirm the displayed tier matches active current heat: Peace for 0 to 9, Notice for 10 to 24, and Concern for 25 to 49.
5. Confirm run diagnostics still show Heat cooling, Run Heat Delta, Heat Application, and Attraction.
6. Refresh diagnostics and toggle F2; confirm neither action changes heat.
7. Browse older run history; confirm the current heat tier line remains derived from active runtime heat rather than the historical outcome.
8. Confirm no Hostile, Raid, raid warning, diplomacy, reputation-axis, or offline-heat behavior appears.
