# M6-C0 Apply Resolved Run Heat Delta

## Motivation

Resolved run outcomes already persist `RunHeatDeltaSummary.FinalHeatDelta`. This ticket adds the first MVP state-mutation step so active run simulation applies that resolved delta to current dungeon heat without introducing offline processing or post-MVP heat states.

## Implementation

- Added `RunHeatStateApplyResolver`, a deterministic pure resolver that consumes current heat, the resolved delta summary, and config-owned Peace, Notice, and Concern boundaries.
- Added persisted `RunHeatApplicationSummary` diagnostics on `RunOutcomeRecord` with additive serialization for legacy-save compatibility. Default or legacy-created summaries are explicitly unresolved with `LegacyDefaultUnresolved` and finite zero numeric fields.
- Applied resolved deltas exactly once inside active `RunSimulationService.SimulateOnce`; diagnostics refresh, history browsing, and legacy outcome display remain read-only.
- Added config entries for MVP heat bounds and `RunHeatApplicationRuleSourceId` in the Bootstrap run simulation content JSON. Blank application rule-source IDs are rejected by both resolver and Bootstrap config validation.
- Added localized Bootstrap diagnostics after Run Heat Delta and before Attraction in both the full overlay and F2 run-diagnostics-only view.

## Explicit non-goals

This change does **not** implement offline heat processing, offline rebound, raids, raid warnings, Hostile, Raid, diplomacy, reputation axes, events, leaderboards, online verification, monetization, seasons, or production heat UI. It does not change Run Heat Delta, Heat Cooling, loot, survival, extraction, attraction, forecast, or demand budget math.

## Automated test coverage added or updated

- `RunHeatStateApplyResolverTests`
  - Positive and negative deltas within and across MVP tiers.
  - Peace minimum and Concern maximum clamps.
  - Tier-before, tier-after, and tier-changed diagnostics.
  - Missing, unresolved, non-finite, and invalid-config errors.
  - Populated and legacy-missing save serialization.
- `RunSimulationTests`
  - Active simulation attaches the application summary and mutates runtime heat once.
  - Repeated diagnostics refresh, Bootstrap overlay update, history browsing, and old or legacy outcome display do not mutate heat.
  - Legacy or unresolved summaries clear stale diagnostics safely.
  - Existing Run Heat Delta diagnostics remain present.
  - Full and F2 diagnostics order is Heat Cooling, Run Heat Delta, Heat Application, Attraction.

## PR 52 Unity EditMode failure corrections

The corrective update addresses the three failing Unity EditMode tests by name:

1. `RunHeatStateApplyResolverTests.RunOutcomeRecord_ApplicationSummary_RoundTrips_AndLegacyMissingFieldIsSafe`
   - Removed the null-only legacy expectation. Unity `JsonUtility` compatibility now accepts either a null missing nested summary or a non-null safe default summary that is unresolved, carries `LegacyDefaultUnresolved`, has finite numeric fields, and applies zero delta.
2. `RunSimulationTests.SimulateRunOnce_AttachesCoolingSummary_AndAppliesHeat_WhenResolvedAndNonZero`
   - Strengthened ordering assertions so application `HeatBefore` is the active pre-run heat, cooling `HeatBeforeCooling` equals application `HeatAfter`, cooling `HeatAfterCooling` equals cooling-before plus actual cooling delta, and `CurrentHeat` equals cooling `HeatAfterCooling`.
3. `RunSimulationTests.SimulateRunOnce_CoolingClampToZero_RecordsActualAppliedDelta`
   - Updated the clamp fixture and assertions for the intended order: active run heat application first, then loot heat cooling. The test now verifies that cooling starts from application `HeatAfter`, clamps to zero, and records the actual applied cooling delta as the negative of `HeatBeforeCooling`.

Additional regression coverage verifies safe default summaries, blank `RunHeatApplicationRuleSourceId` rejection, read-only `RefreshRunLine`, read-only `BootstrapOverlay.Update`, read-only history browsing, and legacy outcome display after load.

## Codex environment results

- Unity EditMode execution was not available in the Codex container because no Unity editor binary is installed.
- JSON parsing and repository static checks should be run in the Codex environment.

## Manual Unity smoke recommendation

1. Open the Bootstrap scene.
2. Follow the usual active run smoke path.
3. Confirm diagnostics show Heat cooling, Run Heat Delta, Heat Application, then Attraction.
4. Confirm active run simulation changes current heat once and overlay refresh/history browsing do not reapply it.
5. Confirm F2 run diagnostics focus keeps the same line order.
6. Confirm null, unresolved, and legacy outcomes do not retain a stale Heat Application line.
7. Confirm no Hostile, Raid, raid warning, diplomacy, or reputation-axis behavior appears.
