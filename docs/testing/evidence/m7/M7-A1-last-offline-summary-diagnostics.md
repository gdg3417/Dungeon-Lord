# M7-A1 Last Offline Summary Diagnostics

## Scope delivered

M7-A1 is a diagnostics-persistence-only follow-up to the M7-A0 offline summary scaffold:

- Added the additive, nullable `SaveData.lastOfflineSummary` diagnostics snapshot field.
- Captured one `OfflineSummaryResolver` result during boot after save load and before the existing boot save advances `lastSavedUtcUnix`.
- Updated Bootstrap Systems Diagnostics refresh to prefer the persisted last-observed snapshot when one exists.
- Preserved M7-A0 resolver fallback behavior when a legacy save or test fixture has no persisted snapshot.
- Extended Unity EditMode coverage for capture timing, snapshot serialization, legacy fallback, stable localization-key fallback, and no-gameplay-mutation guarantees.

## Explicit non-goals

M7-A1 does **not**:

- process offline progression;
- grant offline mana, loot, or rewards;
- apply offline heat processing or heat mutation;
- tick structures during offline time;
- add run history entries during offline time;
- advance or complete research;
- add multiple research slots;
- add production UI;
- add server verification;
- add Hostile, Raid, raid warning, diplomacy, reputation-axis, event, season, leaderboard, or monetization systems.

## Save compatibility notes

`SaveData.lastOfflineSummary` is additive and nullable. Legacy saves that do not contain the field deserialize safely with no persisted snapshot. When the snapshot is absent, Systems Diagnostics uses the existing M7-A0 resolver fallback. No save schema version bump, migration rewrite, or `SaveService.Save` timestamp change is required.

The stored value is diagnostics-only save state. It is a serializable observation snapshot, not gameplay progress, tuning, or a source of gameplay effects.

## Diagnostics persistence notes

At boot, `GameRoot.InitializeServicesAndData` loads or creates the save, constructs `OfflineSummaryResolver`, captures the summary snapshot, refreshes the localized diagnostics lines, and then continues the existing initialization path. The existing boot save still advances `lastSavedUtcUnix` normally.

Later diagnostics refreshes prefer `SaveData.lastOfflineSummary`, so the last observed boot/session offline window remains visible instead of being recomputed as a misleading near-zero window after the boot save timestamp advances. If no persisted snapshot exists, refresh retains M7-A0's safe resolver fallback.

The existing localized diagnostics keys remain sufficient; M7-A1 adds no new visible line labels:

- `ui.dev.offline_summary_format`
- `ui.dev.research_pending_format`

Missing localization entries retain the existing safe stable-key fallback behavior.

## Determinism notes

M7-A1 reuses the deterministic, injected-clock `OfflineSummaryResolver`. Capture resolves exactly one summary and stores that output. Rendering a persisted snapshot performs no time-based recomputation and causes no gameplay mutations.

`WouldProcessOfflineProgress` remains `false`. Capturing or displaying the snapshot does not process offline heat, rewards, research, structures, loot, mana, or run history.

## Test list

Unity EditMode tests added or updated in `OfflineSummaryDiagnosticsTests`:

- `SystemsDiagnostics_IncludesLocalizedOfflineSummaryAndResearchPendingLines`
- `CaptureBeforeTimestampAdvance_PersistsObservedSnapshotForLaterDiagnostics`
- `LegacySaveWithoutPersistedSummary_RemainsSafeAndUsesResolverFallback`
- `PersistedSummary_RoundTripsThroughSaveSerialization`
- `RefreshAndPageCycling_DoNotApplyOfflineRewardsResearchCompletionOrHeatMutation`
- `MissingLocalization_UsesStableKeysAsSafeFallback`
- `RuntimeCSharp_DoesNotHardcodeNewVisibleDiagnosticEnglish`

Before merge, run all Unity EditMode tests, including the existing `OfflineSummaryResolverTests`, `LootHeatCoolingResolverTests`, `RunHeatDeltaResolverTests`, `RunHeatStateApplyResolverTests`, `RunSimulationTests`, loot suites, and structure suites.

## Manual Bootstrap smoke checklist

- [ ] Open the Bootstrap scene.
- [ ] Enter Play Mode.
- [ ] Confirm F1 dev panel still works.
- [ ] Confirm F2 run diagnostics focus still works.
- [ ] Confirm F3 diagnostics page cycling still works.
- [ ] Confirm Systems Diagnostics shows the last observed offline summary.
- [ ] Confirm the shown offline summary does not disappear or become misleading immediately because the boot save advanced `lastSavedUtcUnix`.
- [ ] Confirm Research Pending remains scaffold-only.
- [ ] Confirm no mana, loot, research progress, heat changes, structure ticks, or run history entries are granted from offline time.
- [ ] Confirm no research completes.
- [ ] Confirm no offline heat behavior appears.
- [ ] Confirm no M6 heat behavior changes.
- [ ] Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-A1 does not modify active-run heat delta composition, the accepted two-stage active-run cooling composition from M6-FU0, `LootHeatCoolingResolver`, run simulation math, loot logic, structure simulation logic, or existing tuning values. All M6 heat behavior remains unchanged.

## M7-A1 confirmation

M7-A1 is diagnostics persistence only. It does not implement offline rewards, research completion, offline heat, production UI, or server verification.
