# M7-A0 Offline Summary and Research Pending Scaffold

## Scope delivered

M7-A0 starts M7 with a deliberately small, deterministic scaffold:

- Added a serializable `OfflineSummary` diagnostic model.
- Added an additive, nullable `ResearchPendingState` save field representing one pending research slot.
- Added a read-only `OfflineSummaryResolver` that observes elapsed time from an injected clock, the last saved timestamp, the current pending research scaffold, and config-owned offline rules.
- Added localized Bootstrap Systems Diagnostics lines for the offline summary and research pending state.
- Added EditMode coverage for determinism, compatibility, localization fallback, diagnostics placement, clamping, invalid timestamp handling, and no-mutation guarantees.

## Explicit non-goals

This scaffold does **not**:

- grant offline rewards or mana;
- apply offline heat processing or heat mutation;
- tick structures during offline time;
- add run history or loot during offline time;
- advance or complete research;
- add multiple research slots;
- add production UI;
- add server verification;
- add Hostile, Raid, raid warning, diplomacy, reputation-axis, event, season, leaderboard, or monetization systems.

## Data and save compatibility notes

`SaveData.researchPending` is additive and nullable. Legacy saves that do not contain the field deserialize with `researchPending == null`; the resolver reports `ResearchPending == false` safely. No save schema version bump or migration rewrite is required for this nullable scaffold field.

The existing `timeRules.maxOfflineSeconds` config owns the observed-window cap. M7-A0 adds `timeRules.offlineSummaryRuleSourceId` as a stable config-owned diagnostic source ID. No existing gameplay tuning value was changed.

## Determinism notes

`OfflineSummaryResolver` accepts `ITimeSource`, which allows EditMode tests to inject a fixed timestamp. For identical save data, time rules, and fixed clock input, repeated resolution returns identical summaries.

The resolver is read-only. It reports `WouldProcessOfflineProgress == false` for every path. Integer Unix timestamps avoid non-finite floating-point elapsed-time inputs; invalid current timestamps, missing last-save timestamps, and clock reversal are handled as deterministic error results with zero observed seconds.

## Diagnostics notes

The Bootstrap overlay displays the new localized lines on **Systems Diagnostics** (F3 page 4/4):

- `ui.dev.offline_summary_format`
- `ui.dev.research_pending_format`

If either localization entry is missing, the existing stable-key fallback pattern is used. Diagnostic refresh and page cycling do not invoke offline progression.

## Test list

Unity EditMode tests added:

- `OfflineSummaryResolverTests.Resolve_IdenticalInputs_ReturnsDeterministicReadOnlySummary`
- `OfflineSummaryResolverTests.Resolve_ElapsedBeyondConfigLimit_ReportsClampedObservedWindowWithoutProcessingProgress`
- `OfflineSummaryResolverTests.Resolve_MissingRules_ReturnsSafeDeterministicErrorWithoutOfflineEffects`
- `OfflineSummaryResolverTests.Resolve_InvalidElapsedWindow_ReturnsSafeDeterministicError`
- `OfflineSummaryResolverTests.Resolve_LegacySaveWithoutResearchPendingState_RemainsSafeAndReportsNoPendingResearch`
- `OfflineSummaryResolverTests.Resolve_SingleResearchSlot_ReportsPendingWithoutCompletingOrMutatingResearch`
- `OfflineSummaryDiagnosticsTests.SystemsDiagnostics_IncludesLocalizedOfflineSummaryAndResearchPendingLines`
- `OfflineSummaryDiagnosticsTests.RefreshAndPageCycling_DoNotApplyOfflineRewardsResearchCompletionOrHeatMutation`
- `OfflineSummaryDiagnosticsTests.MissingLocalization_UsesStableKeysAsSafeFallback`
- `OfflineSummaryDiagnosticsTests.RuntimeCSharp_DoesNotHardcodeNewVisibleDiagnosticEnglish`

Before merge, run all Unity EditMode tests, including the existing heat, run simulation, loot, and structure suites.

## Manual Bootstrap smoke checklist

- [ ] Open the Bootstrap scene.
- [ ] Enter Play Mode.
- [ ] Confirm the existing F1 dev panel still works.
- [ ] Confirm the existing F2 run diagnostics focus still works.
- [ ] Confirm the existing F3 diagnostics page cycling still works.
- [ ] Confirm the offline summary and research pending diagnostics appear on Systems Diagnostics (page 4/4).
- [ ] Confirm the new diagnostics are readable and localized.
- [ ] Confirm entering Play Mode or refreshing diagnostics does not grant mana, loot, research progress, heat changes, or run history entries.
- [ ] Confirm no research completes.
- [ ] Confirm no offline heat behavior appears.
- [ ] Confirm no M6 heat behavior changes.
- [ ] Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-A0 does not modify active-run heat delta composition, `LootHeatCoolingResolver`, run simulation math, loot logic, structure simulation logic, or any M6 heat behavior. The current intentional two-stage active-run cooling composition remains unchanged.

## Scaffold confirmation

M7-A0 is scaffold-level only. It does not implement offline rewards, research completion, offline heat, production UI, or server verification.
