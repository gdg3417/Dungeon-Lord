# M6-I0 Heat and Diagnostics Closeout Audit

## 1. Purpose

This documentation-only audit closes out the merged M6 MVP heat work after M6-C2 Bootstrap diagnostics paging (PR #55). It reviews the repository state at merge commit `480ccb0` and records whether M6 is ready to hand off to M7. This audit does not change C# runtime code, tests, JSON config, localization tables, Unity assets, save schema, gameplay tuning, simulation logic, heat math, loot logic, run outcomes, or diagnostics behavior.

## 2. M6 scope summary

M6 implements the active-run MVP heat slice only:

- deterministic Adventurer Demand Budget resolution and diagnostics;
- deterministic Run Heat Delta resolution and diagnostics;
- application of the resolved run heat delta during active run simulation;
- read-only Current Heat Tier diagnostics; and
- Bootstrap developer-diagnostics paging while preserving focused F2 run diagnostics.

The active MVP tier set is limited to stable identifiers for **Peace**, **Notice**, and **Concern**. M6 is not a post-MVP threat escalation, offline processing, or production UI milestone.

## 3. PR or increment inventory

| Increment | Merged delivery | Audit result |
| --- | --- | --- |
| M6-A0 Adventurer Demand Budget resolver scaffold | Resolver, persisted summary, config ownership, integration, and tests. Evidence: `M6-A0-adventurer-demand-budget-scaffold.md`. | Complete. |
| M6-A1 Adventurer Demand Budget diagnostics | Localized Bootstrap developer line, safe key fallback, stale-line clearing, and tests. Evidence: `M6-A1-adventurer-demand-budget-diagnostics.md`. | Complete. |
| M6-B0 Run Heat Delta resolver scaffold | Pure resolver, persisted summary, config ownership, integration, legacy safety, and tests. Evidence: `M6-B0-run-heat-delta-summary.md`. | Complete. |
| M6-B1 Run Heat Delta diagnostics | Localized Bootstrap developer line, safe key fallback, ordering, stale-line clearing, and tests. Evidence: `M6-B1-run-heat-delta-diagnostics.md`. | Complete. |
| M6-C0 active run heat application | Pure application resolver, active simulation mutation, persisted diagnostics summary, MVP tier bounds, compatibility coverage, and tests. Evidence: `M6-C0-apply-run-heat-delta.md`. | Complete. |
| M6-C1 Current Heat Tier diagnostics | Read-only runtime tier derivation, localized Bootstrap developer line, MVP range handling, and tests. Evidence: `M6-C1-current-heat-tier-diagnostics.md`. | Complete. |
| M6-C2 Bootstrap diagnostics paging | PR #55 (`480ccb0`), including localized four-page developer diagnostics, preserved F2 focus mode, preservation tests, safe fallback tests, and a non-mutation test. | Complete. No separate M6-C2 evidence Markdown file was committed before this closeout audit. |

## 4. What is complete

- `RunSimulationService.SimulateOnce` resolves demand budget, resolves run heat delta, resolves heat application, and writes `runtime.Heat` only when the application summary resolved successfully.
- `RunOutcomeRecord` persists demand-budget, run-heat-delta, and run-heat-application summaries additively.
- Current heat tier diagnostics derive from active runtime heat rather than a selected historical outcome.
- The full Bootstrap overlay is paged into Runtime Summary, Run Diagnostics, Heat Diagnostics, and Systems Diagnostics pages.
- F2 run-diagnostics-only mode remains available independently of the selected full-overlay page.
- The paged overlay keeps the previously delivered diagnostics available across its pages. The focused run view still includes Run, Run history, Loot, Survival, Extraction, Heat cooling, Run Heat Delta, Heat Application, Attraction, Forecast, and Demand Budget lines.

## 5. What is explicitly not implemented

M6 does **not** implement Hostile, Raid, raid warnings, raids, diplomacy, reputation axes, offline heat processing, offline rebound, events, seasons, leaderboards, monetization, or production heat UI. No prohibited-scope term is present in Bootstrap runtime/config implementation files when searched case-insensitively. Peace, Notice, and Concern are the only active tier IDs.

M6-B0 intentionally leaves elite-death heat inactive because the current outcome and survival models do not expose an explicit elite-death count. The config field exists, but the resolver uses an elite-death count of zero rather than inferring actual deaths from planning or forecast data.

## 6. Guardrail compliance review

**Result: PASS for the merged M6 slice.**

- Gameplay tuning introduced by M6 is stored in typed config fields and Bootstrap JSON content, not embedded as runtime heat values.
- M6 diagnostics use localization-table keys and safe key fallback patterns.
- Tier names are represented by stable IDs (`heat_tier.peace`, `heat_tier.notice`, and `heat_tier.concern`), not player-facing hardcoded English strings.
- This M6-I0 PR is documentation-only and introduces no gameplay or localization behavior.

Existing non-M6 Bootstrap scaffold code still contains older developer-facing literal fallback strings and explicit developer-control numeric deltas. Those predate this audit and were not modified because this ticket is constrained to documentation-only closeout. They remain general guardrail-debt candidates for a separately scoped follow-up if the repository guardrails are applied retroactively to legacy scaffold code.

## 7. Config ownership review

**Result: PASS.**

The typed `RunSimulationConfig` owns the M6 heat delta inputs (`RunHeatNormalDeathDelta`, `RunHeatEliteDeathDelta`, `RunHeatMultipleDeathBonusDelta`, `RunHeatSurvivorCoolingPerSurvivor`, `RunHeatLootCoolingPerExtractedValue`, delta minimum/maximum, and rule-source ID) and the MVP tier/application inputs (`HeatPeaceMinimum` through `HeatConcernMaximum` and application rule-source ID).

Bootstrap JSON owns the concrete values. At audit time it defines run heat delta bounds of `-10.0` to `10.0`, Peace `0.0` to `9.0`, Notice `10.0` to `24.0`, and Concern `25.0` to `49.0`. Resolver and Bootstrap validation paths reject invalid or non-finite configuration.

## 8. Localization review

**Result: PASS for M6 diagnostics.**

- Demand Budget, Run Heat Delta, Heat Application, and Current Heat Tier formats are stored under localization keys in `string_table_en.json`.
- M6 diagnostic builders use localization-key fallbacks if content or a key is unavailable, rather than introducing hardcoded player-facing English fallback text.
- M6-C2 page headers, page names, F3 hint, and F2 focused-view header are localized and tested for safe key fallback behavior.
- Adding another language remains a table/content task; the M6 diagnostic rendering code does not require language-specific code changes.

## 9. Determinism review

**Result: PASS.**

- Demand budget and run heat delta are pure resolver steps driven by config, persisted summaries, and the deterministic resolver seed.
- Heat application is a pure resolver step driven by config, current heat, and the resolved delta summary.
- Current tier classification is a pure read-only resolver step driven by config and active current heat.
- Regression coverage verifies deterministic repeated resolution, stable error summaries, finite persisted fields on failure, and equivalent run simulation results when separate runtime instances start with identical values.

## 10. Save and legacy compatibility review

**Result: PASS.**

- New run outcome summaries are additive nested fields. Legacy missing fields deserialize safely as null or safe unresolved defaults, depending on Unity `JsonUtility` behavior.
- Save migration only initializes run history and seeds `RecentOutcomes` from `LatestOutcome` when required. It does not resolve or apply heat.
- Loading a save, refreshing diagnostics, selecting old run outcomes, and displaying legacy outcomes do not reapply historical heat.
- The active Current Heat Tier is derived from current runtime heat and is not persisted into historical outcomes.

## 11. Diagnostics read-only review

**Result: PASS.**

- `RefreshRunLine`, current-tier refresh, overlay text rebuilding, history browsing, F2 focus toggling, and F3 page cycling are display/navigation paths.
- `BootstrapOverlayPagingTests.CycleFullDiagnosticsPage_DoesNotMutateSaveHeatRunHistoryOrStructureRuntime` explicitly verifies that repeated F3 page cycles and refreshes preserve the save object, structure runtime, heat, mana, crisis flag, run history, and history entry.
- `RunSimulationTests` and `CurrentHeatTierDiagnosticsTests` cover read-only refresh/history behavior and old-outcome display.

## 12. Active heat mutation review

**Result: PASS, with one follow-up confirmation recorded below.**

The new M6-C0 mutation occurs only in active `RunSimulationService.SimulateOnce`: after a resolved application summary, the service assigns `runtime.Heat = heatApplicationSummary.HeatAfter`. `GameRoot.SimulateRunOnce` then synchronizes `CurrentHeat`, runs the existing active-run loot heat cooling step, appends the outcome, refreshes diagnostics, and saves.

Other repository heat mutations are explicit existing developer/simulation controls: `GameRoot.ApplyHeatDelta`, the dev-panel Sim Heat button, and the explicit structure-tick simulation path. Diagnostics and load/migration paths do not call the M6 application resolver as a state mutation step.

## 13. Offline heat exclusion review

**Result: PASS.**

No offline heat processor, rebound rule, catch-up heat mutation, or load-time historical run replay exists in the reviewed M6 implementation. Offline mode in the Bootstrap overlay is an existing connectivity/dev banner control, not a heat simulation path. M6 heat application is reached through active run simulation only, alongside explicit existing developer/simulation controls.

## 14. MVP scope exclusion review

**Result: PASS.**

- Runtime/config search found no Hostile or Raid implementation and no raid-warning behavior.
- Application and tier resolution clamp/classify within the configured Peace, Notice, and Concern MVP range.
- No diplomacy, reputation-axis, event, season, leaderboard, monetization, or production UI implementation was added by M6.
- M7 must not start until this M6-I0 audit is merged.

## 15. Test coverage summary

This documentation-only audit does not require a Unity test rerun. The latest required Unity EditMode suite list for M6 validation is:

- `AdventurerDemandBudgetResolverTests`
- `RunHeatDeltaResolverTests`
- `RunHeatStateApplyResolverTests`
- `CurrentHeatTierResolverTests`
- `CurrentHeatTierDiagnosticsTests`
- `BootstrapOverlayPagingTests`
- `RunSimulationTests`
- `AdventurerInterestForecastResolverTests`
- `AdventurerAttractionResolverTests`
- `LootExtractionResolverTests`
- `LootHeatCoolingResolverTests`
- `LootRollResolverTests`
- `MigrationRunnerTests`, if present/relevant
- `HeatSystemTests`, if present/relevant

The merged tests cover resolver determinism and validation, config boundaries, active heat application and clamping, safe serialization defaults, legacy missing fields, non-reapplication during display/history browsing, localized formatting and safe fallback, paging wraparound, page composition, F2 preservation, and paging non-mutation.

## 16. Manual smoke coverage summary

The merged M6 evidence files define the following recommended Bootstrap smoke pass. A fresh Unity manual run was not performed as part of this documentation-only audit:

1. Open the Bootstrap scene and enter Play Mode.
2. Use the dev run simulation control.
3. Confirm the full developer overlay pages cycle with F3 and wrap from Systems Diagnostics back to Runtime Summary.
4. Confirm F2 focus mode remains available independently of the current full-overlay page.
5. Confirm focused run diagnostics retain Run, Run history, Loot, Survival, Extraction, Heat cooling, Run Heat Delta, Heat Application, Attraction, Forecast, and Demand Budget lines.
6. Confirm Current Heat Tier appears with current active heat and resolves Peace for `0..9`, Notice for `10..24`, and Concern for `25..49` under the Bootstrap config.
7. Refresh diagnostics, cycle pages, toggle F2, and browse old run history; confirm none of those actions change heat or reapply historical outcomes.
8. Confirm no Hostile, Raid, raid warning, diplomacy, reputation-axis, offline-heat, or production UI behavior appears.
9. Confirm no localization keys appear during normal loaded-content play and no unexpected `.meta` files are created.

## 17. Known risks or gaps

1. **Follow-up confirmation: active-run cooling composition.** The M6 Run Heat Delta summary includes loot cooling through `RunHeatLootCoolingPerExtractedValue`. After M6 application, `GameRoot.SimulateRunOnce` also executes the pre-existing `LootHeatCoolingResolver` step using its separately config-owned cooling values. Existing regression tests assert this order, so this audit does not treat it as an untested defect and does not change behavior. Before future tuning or M7 dependency work, record a product/design confirmation that both active-run cooling contributions are intentionally retained; otherwise open a separately scoped behavior ticket.
2. **Elite-death heat is config-owned but intentionally inactive.** No explicit elite-death count exists yet. Future activation requires an explicit modeled source and a separately scoped change; it must not be inferred from demand, attraction, or forecast signals.
3. **No dedicated M6-C2 evidence Markdown file predates this audit.** PR #55 code and tests provide the merged evidence. This closeout document records that inventory explicitly.
4. **Manual Unity smoke remains recommended.** This documentation-only audit did not run Unity Play Mode manually or rerun EditMode suites.
5. **Legacy scaffold guardrail debt is out of scope.** Older non-M6 Bootstrap developer fallbacks and explicit dev-control tuning literals may warrant a separate retroactive guardrail cleanup ticket.

## 18. Recommendation

**GO WITH FOLLOW-UP** for moving to M7 **after this M6-I0 audit is merged**.

M6 has the expected closeout shape: active heat delta calculation, active heat application, current-tier diagnostics, localized/read-only diagnostics, and Bootstrap diagnostics paging are implemented and covered by merged tests. No audit finding requires a documentation-only closeout to become NO-GO. The follow-up is to record an explicit product/design decision for the retained two-stage active-run cooling composition and, separately if desired, track legacy Bootstrap scaffold guardrail debt. Neither follow-up authorizes M7 work before this audit merges.
