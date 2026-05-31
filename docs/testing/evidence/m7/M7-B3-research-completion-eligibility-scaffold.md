# M7-B3 Research Completion Eligibility Scaffold

## Scope delivered

M7-B3 adds the smallest read-only research completion eligibility scaffold after M7-B2. A pure deterministic resolver compares the current single-slot saved research progress with a content-owned required-progress rule and returns whether the saved progress is eligible for a future completion step. M7-B3 also adds one localization-backed Systems Diagnostics line for the eligibility summary.

## Explicit non-goals

M7-B3 does not complete research, set completion pending, grant rewards, charge costs, unlock content, add prerequisites, process offline research progress, process offline heat, add production research UI, add multiple research slots, add server verification, or alter M6 behavior. A later PR may decide whether and how an eligible research item becomes completion pending.

## Config ownership notes

`ContentBootstrap.researchCompletionEligibilityScaffold` owns the M7-B3 rule. Its JSON content record supplies `enabled`, stable `ruleSourceId`, scaffold `projectId`, and `requiredProgressUnits`. Runtime C# contains no research-completion tuning value. Missing, disabled, malformed, non-positive, non-finite, and project-mismatched config returns deterministic safe output.

## Save compatibility notes

No save field and no migration were added. M7-B3 reads the existing additive M7-B1 `SaveData.researchProgress` state. Legacy saves with no `researchProgress`, and Unity `JsonUtility` empty-default `ResearchProgressState` objects, resolve safely as missing progress state. Existing M7-B2 active-session accumulation continues to own progress mutation.

## Determinism notes

`ResearchCompletionEligibilityResolver` is pure and deterministic. It consumes the current pending state, saved progress state, and loaded typed config. Its output reports resolution status, deterministic error code, stable IDs, progress, requirement, clamped remaining progress, eligibility, rule source, `WouldSetCompletionPending = false`, and `WouldCompleteResearch = false`. Repeated identical inputs return identical serialized summaries.

## Mutation boundaries

The M7-B3 resolver performs no mutation. Diagnostics perform no mutation. M7-B3 does not write `SaveData`, `SaveData.researchPending`, `SaveData.researchProgress`, `ResearchProgressState.CompletionPending`, heat, mana, loot, run history, structure runtime, total ticks, or last offline summary. Existing M7-B2 active-tick progress accumulation remains the only adjacent research-progress write path.

## Diagnostics notes

Systems Diagnostics now includes one localization-backed `Research Completion Eligibility` line showing: resolved, error, pending, hasState, slot, project, progress, required, remaining, eligible, wouldSetCompletionPending, wouldComplete, and ruleSource. No-pending output is safe and contains no stale project ID. Missing localization follows the existing safe raw-key fallback pattern.

## Developer control notes

Existing developer controls remain coherent and unchanged:

- Set Research Pending initializes the existing single-slot saved progress state at zero.
- Clear Research Pending clears both pending and progress state.
- Clear Research Pending returns eligibility diagnostics to safe no-pending output with no stale project ID.
- Neither control completes research, sets completion pending, grants rewards, charges costs, unlocks content, or processes offline progress.

## Test list

Added EditMode coverage:

- `ResearchCompletionEligibilityResolverTests`
  - Null pending, null progress, and empty-default progress safe output.
  - Below, equal, and above requirement eligibility behavior with remaining-progress clamp.
  - Repeated-input determinism.
  - Missing, disabled, invalid, invalid-required-progress, invalid-existing-progress, slot-mismatch, progress-project-mismatch, and config-project-mismatch handling.
  - `WouldSetCompletionPending = false` and `WouldCompleteResearch = false` invariants.
  - Whole-save non-mutation proof covering pending, progress, completion pending, heat, mana, run history, structure runtime, total ticks, and last offline summary.
- `ResearchCompletionEligibilityDiagnosticsTests`
  - Existing M7-B0 and M7-B1 diagnostics remain visible beside localized M7-B3 eligibility.
  - Normal loaded-content diagnostics avoid the raw M7-B3 localization key.
  - Safe no-pending diagnostics and localization fallback.
  - Below-requirement and eligible diagnostics after active-tick progress application.
  - Eligibility does not set completion pending, complete research, or mutate adjacent reward-related state.
  - Clear Research Pending removes stale project output.
  - Diagnostics refresh does not mutate save state.

Regression suites to run before merge:

- `ResearchCompletionEligibilityResolverTests`
- `ResearchCompletionEligibilityDiagnosticsTests`
- `ResearchProgressApplyResolverTests`
- `ResearchProgressActiveTickIntegrationTests`
- `ResearchProgressStateResolverTests`
- `ResearchProgressResolverTests`
- `ResearchProgressDiagnosticsTests`
- `ResearchPendingResolverTests`
- `ResearchPendingDevControlsTests`
- `OfflineSummaryResolverTests`
- `OfflineSummaryDiagnosticsTests`
- `BootstrapOverlayPagingTests`
- `RunSimulationTests`
- `RunHeatDeltaResolverTests`
- `RunHeatStateApplyResolverTests`
- `LootHeatCoolingResolverTests`
- `LootExtractionResolverTests`
- `StructureSimulationTests`
- `MigrationRunnerTests`

## Manual Bootstrap smoke checklist

1. Open Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 developer panel toggle still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 diagnostics page cycling still works.
6. Go to Systems Diagnostics.
7. Confirm Offline Summary still shows `wouldProcess=False`.
8. Confirm Research Pending displays correctly.
9. Confirm Research Progress Preview from M7-B0 still displays correctly.
10. Confirm Research Progress State from M7-B1 still displays correctly.
11. Confirm Research Completion Eligibility displays safely.
12. Confirm no-pending state shows safe no-pending output.
13. Use Set Research Pending.
14. Confirm pending becomes true.
15. Confirm progress state starts at zero.
16. Confirm eligibility is false when progress is below requirement.
17. Let active simulation ticks accumulate enough progress to meet or exceed the content-owned requirement, or adjust only test config if manual smoke would take too long.
18. Confirm eligibility becomes true.
19. Confirm `CompletionPending` remains false.
20. Confirm no research completion occurs.
21. Confirm no rewards, costs, unlocks, mana, loot, heat changes, extra structure mutation, or run history entries are granted by eligibility.
22. Use Clear Research Pending.
23. Confirm pending becomes false.
24. Confirm eligibility diagnostics return to no-pending output.
25. Confirm no stale project ID remains.
26. Confirm no offline heat behavior appears.
27. Confirm no offline progression appears.
28. Confirm no M6 heat behavior changes.
29. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B3 does not modify `LootHeatCoolingResolver`, active-run heat delta composition, run simulation math, loot extraction logic, structure simulation logic, or any heat, loot, run, or structure tuning value. Existing M6 active heat behavior remains unchanged.

## Scope confirmation

M7-B3 is read-only single-slot research completion eligibility scaffolding only. It does not implement offline progression, offline heat, research completion, completion pending mutation, rewards, unlocks, costs, production UI, server verification, or multiple research slots. `OfflineSummary.WouldProcessOfflineProgress` remains false and `allowOfflineProgression` remains dormant.
