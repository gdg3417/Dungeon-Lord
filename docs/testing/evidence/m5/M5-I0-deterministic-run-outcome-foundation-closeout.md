# M5-I0 Deterministic Run-Outcome Foundation Closeout Audit

## Executive summary

This audit reviewed the full M5 deterministic run-outcome chain from loot roll to forecast diagnostics, plus config ownership, localization behavior, determinism, persistence safety, and regression coverage.

**Conclusion:** **Go** for M5 closeout, with known non-goal limitations intentionally preserved. No runtime blockers were found that require code changes for M5 scope.

## M5 scope delivered

Delivered, inspectable chain (runtime + diagnostics + tests):

1. Loot resolver foundation (`LootRollResolver` + run loot summary)
2. Survival summary
3. Loot extraction summary
4. Loot heat cooling summary and heat application step
5. Adventurer attraction summary
6. Adventurer interest forecast summary
7. Bootstrap diagnostics visibility in full overlay and F2 diagnostics focus mode
8. Save/load compatibility for latest + historical outcomes, with explicit round-trip + legacy-missing-field coverage for newly added summaries

## Explicit non-goals intentionally unbuilt

The following remain intentionally unbuilt and were verified as still not introduced by this closeout audit:

- Adventurer AI, spawning, traffic simulation, party composition, pathing
- Combat, inventory, crafting, research, merchant traffic, economy loops
- Reward distribution systems and production player-facing feature screens
- Forecast-driven gameplay behavior (forecast remains diagnostics/instrumentation only)

## System chain diagram (text)

`RunSimulationService.SimulateOnce(...)`
→ computes deterministic run chance + score + reason/feedback
→ `BuildLootSummary(...)` using `LootRollResolver.Resolve(...)`
→ `BuildSurvivalSummary(...)`
→ `LootExtractionResolver.Resolve(...)`
→ `AdventurerAttractionResolver.Resolve(...)`
→ `AdventurerInterestForecastResolver.Resolve(...)`
→ returns `RunOutcomeRecord` with deterministic summaries
→ `GameRoot` resolves/applies `LootHeatCoolingResolver` from extraction summary and records heat cooling summary
→ `RunOutcomeRecord` / `RunHistoryState` persistence
→ `GameRoot` run diagnostic lines
→ `BootstrapOverlay` full panel and F2 diagnostics focus mode.

## Per-layer closeout audit

| Area | Status | Evidence | Remaining risk |
|---|---|---|---|
| Loot resolver foundation | Complete | `LootRollResolver` deterministic seed combine, weighted selection, guarded error codes; covered by `LootRollResolverTests`. | None observed in M5 scope. |
| Run loot summary | Complete | `RunSimulationService.BuildLootSummary` writes resolver seed, success/error, counts, generated IDs, value totals into `RunLootSummary`; diagnostics format key present. | None observed. |
| Survival summary | Complete | `BuildSurvivalSummary` validates bounds/ratios and emits deterministic error codes + seed + source id. | None observed. |
| Extraction summary | Complete | `LootExtractionResolver` validates dependencies, deterministic subset selection, value aggregates, deterministic failure codes; tests present. | None observed. |
| Heat cooling summary + heat application | Complete | `LootHeatCoolingResolver` returns rule/effective deltas with error codes; applied in run flow and surfaced in diagnostics. | None observed. |
| Attraction summary | Complete | `AdventurerAttractionResolver` uses extracted world value + config scalar with deterministic guardrails; tests include save round-trip + legacy safety. | None observed. |
| Interest forecast summary | Complete | `AdventurerInterestForecastResolver` validates thresholds and computes score + band IDs; tests include determinism/config validation/round-trip/legacy safety. | None observed. |
| Bootstrap diagnostics visibility | Complete | `GameRoot` maintains run diagnostics lines including loot/survival/extraction/heat/attraction/forecast; `BootstrapOverlay` renders all lines. | None observed. |
| F2 run diagnostics focus mode | Complete | `BootstrapOverlay` F2 branch shows Run, Run history, Loot, Survival, Extraction, Heat cooling, Attraction, Forecast lines only (+ hints). | None observed. |
| Localization safety | Complete for M5 scope | Diagnostics labels and run reason/feedback strings resolve through string table keys; no player-facing run text hardcoded in run chain. | Legacy UI strings outside M5 chain remain pre-existing and out of ticket scope. |
| Config ownership | Complete | Run tunables and resolver thresholds/scalars owned by `run_simulation_config.json`; runtime consumes injected/loaded `RunSimulationConfig`. | None observed. |
| Save safety | Complete | New summaries exist on `RunOutcomeRecord`; tests cover JSON round-trip and legacy missing-field handling for attraction and forecast. | Continue adding migration fixtures for any future summary schema changes. |
| Determinism | Complete | Seed derivation from run sequence + tick; pure resolvers consume deterministic inputs; no `GetHashCode` usage in audited chain. | Cross-platform floating-point nuance is low risk but always monitor. |
| Regression tests | Complete for requested M5 chain | Focused EditMode suites exist for run simulation, loot roll/extraction/heat cooling, attraction, forecast, migration runner. | Keep adding edge-case tests only when new summary fields are introduced. |
| Manual smoke coverage | Complete (recorded from PR 46 validation) | User validation confirms Bootstrap/F2 diagnostic visibility, forecast fields, no adventurer behavior, no unexpected `.meta`. | Re-run smoke on future runtime UI changes. |

## Config ownership audit

| Concern | Evidence | Result |
|---|---|---|
| Survival party bounds and survivor ratios are config-owned | `RunSimulationConfig` in `run_simulation_config.json` owns `MinPartySize`, `MaxPartySize`, `MaxAllowedPartySize`, `SuccessSurvivorRatio`, and `FailureSurvivorRatio`. | Pass |
| Extraction/heat cooling/attraction/forecast tuning is config-owned | `RunSimulationConfig` in `run_simulation_config.json` owns extraction, heat cooling, attraction, and forecast rule IDs/thresholds/scalars. | Pass |
| Survival `RuleSourceId` ownership is correctly characterized | Survival uses a stable code identifier (`"run.survival.rule.v1"`) as rule provenance; it is not treated as tunable config. | Pass |
| Runtime consumes config instead of hardcoded tuning | `RunSimulationService` and resolvers use `RunSimulationConfig` values (not embedded gameplay tuning literals). No gameplay tuning values were found hardcoded in the audited M5 chain. | Pass |
| Resolver rule provenance retained | Summary objects include `RuleSourceId` + deterministic seed and error code. | Pass |

## Localization ownership audit

| Concern | Evidence | Result |
|---|---|---|
| Diagnostics lines localizable | Format keys present in `string_table_en.json`: loot/survival/extraction/heat/attraction/forecast diagnostics. | Pass |
| Reason/feedback text localizable | Run reason + feedback keys exist and are used in run outcome display flow. | Pass |
| Missing-key fallback behavior | Runtime uses `Content.GetString(key, key)` patterns in overlay and run diagnostics flow, matching key-fallback guardrail. | Pass |
| No new hardcoded player-facing runtime English in audited additions | Forecast/attraction/extraction/heat diagnostics labels come from string table keys in M5 flow. | Pass |

## Determinism audit

| Check | Evidence | Result |
|---|---|---|
| Stable seed generation for run chain | `ComputeResolverSeed(runSequence, tickStarted)` produces deterministic integer seed. | Pass |
| Deterministic loot resolver behavior | `LootRollResolver` uses explicit deterministic RNG and stable seed/table combine; no `GetHashCode`. | Pass |
| Deterministic extraction subset | Extraction ranks items deterministically from seed + item id + index. | Pass |
| Deterministic attraction/forecast outputs | Pure arithmetic from deterministic prior summaries + config thresholds/scalars. | Pass |
| Regression proof | `RunSimulationTests`, `Loot*ResolverTests`, attraction and forecast resolver tests assert deterministic behavior and stable outputs. | Pass |

## Persistence and legacy-save audit

| Check | Evidence | Result |
|---|---|---|
| LatestOutcome persistence | `RunOutcomeRecord` includes loot/survival/extraction/heat/attraction/forecast summaries. | Pass |
| RecentOutcomes persistence | `RunHistoryState.RecentOutcomes` stores same `RunOutcomeRecord` shape. | Pass |
| Explicit round-trip coverage for newly added summaries | `AdventurerAttractionResolverTests` and `AdventurerInterestForecastResolverTests` include JSON round-trip tests for populated `LatestOutcome` + `RecentOutcomes` summary data. | Pass |
| Explicit legacy-missing-field coverage for newly added summaries | `AdventurerAttractionResolverTests` and `AdventurerInterestForecastResolverTests` include legacy missing-field deserialization safety checks. | Pass |
| Earlier summary persistence path | Loot/survival/extraction/heat summaries share the same `RunOutcomeRecord` storage path and are exercised by run simulation + diagnostics test flows. | Pass |
| Future schema extension expectation | Future summary additions should continue adding explicit `LatestOutcome` and `RecentOutcomes` round-trip tests. | Pass |

## Diagnostics and smoke-test audit

### Runtime diagnostics implementation status

- Full overlay includes run, history, breakdown, feedback, loot, survival, extraction, heat cooling, attraction, and forecast lines.
- F2 run-diagnostics mode includes required focused lines for audit visibility.

### Recorded manual smoke evidence (from PR 46 validation)

1. All Unity script tests passed.
2. Bootstrap smoke test passed.
3. F2 diagnostics focus mode showed: Run, Run history, Loot, Survival, Extraction, Heat cooling, Attraction, Forecast.
4. Forecast line displayed resolved status, error code, signal, score, and band.
5. No localization keys appeared during normal loaded-content play.
6. No adventurer spawning/traffic/AI/forecast-driven behavior observed.
7. No unexpected `.meta` files observed.

## Test coverage inventory

Primary M5 chain tests present:

- `RunSimulationTests`
- `LootRollResolverTests`
- `LootExtractionResolverTests`
- `LootHeatCoolingResolverTests`
- `AdventurerAttractionResolverTests`
- `AdventurerInterestForecastResolverTests`
- `MigrationRunnerTests` (present; relevant for schema safety posture)

Coverage includes:

- Deterministic same-input outcomes
- Resolver config validation/error-code paths
- Summary attachment to outcomes
- JSON round-trip and legacy-missing-field safety for new persisted summaries
- Diagnostics formatting key usage via run display tests

## Known limitations

1. Forecast is intentionally diagnostic-only; no gameplay effect is applied (by design for current milestone).
2. Manual smoke evidence is inherited from PR 46 validation and should be re-run when diagnostics rendering changes.
3. Localization audit in this ticket is focused on M5 chain; non-M5 pre-existing UI strings are out of scope.

## Recommended next milestone

Proceed to next milestone work that **consumes** (not reworks) these deterministic summaries as inputs for future systems, while preserving:

- Config-owned tuning,
- Localization-key-only player text,
- Save-safe extension patterns (latest + history + legacy-safe behavior),
- Resolver-seam-first architecture.

## Go / no-go recommendation for closing M5

**Recommendation: GO (close M5).**

Rationale:

- Deterministic run-outcome chain is complete and inspectable end-to-end.
- All audited summaries are config-owned and localization-safe in diagnostics.
- Persistence and legacy-save safety are test-backed for new summary fields.
- Regression suites covering the required resolver chain are present.
- Manual smoke evidence confirms runtime diagnostics visibility and non-goal behavior boundaries.

No M5 blockers were found in this audit.
