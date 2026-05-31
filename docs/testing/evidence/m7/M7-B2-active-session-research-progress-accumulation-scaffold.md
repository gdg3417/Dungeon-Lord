# M7-B2 Active-Session Research Progress Accumulation Scaffold

## Scope delivered

M7-B2 adds the smallest active-session accumulation slice after M7-B1:

- A pure deterministic `ResearchProgressApplyResolver` computes one active simulation tick's progress delta from the current single-slot pending state, current saved progress state, loaded `ResearchProgressScaffoldConfig`, and configured tick seconds.
- `GameRoot.HandleSimulationTick` invokes the resolver once per active simulation tick and writes only `Save.researchProgress.ProgressUnits` when the returned summary is resolved.
- Existing M7-B0 Research Progress Preview diagnostics remain cumulative active-session preview diagnostics.
- Existing M7-B1 Research Progress State diagnostics reflect saved accumulated progress after active ticks.

## Explicit non-goals

M7-B2 does not complete research, grant rewards, charge costs, unlock content, process offline progress, add offline rewards, add offline heat processing, add production research UI, add multiple research slots, add server verification, add duration or prerequisite systems, or add any post-MVP system.

## Config ownership notes

The per-tick formula is content-owned. Runtime code consumes the existing typed `ResearchProgressScaffoldConfig.progressPerActiveSecond` coefficient and `ContentBootstrap.tickSeconds`; it does not add gameplay tuning literals. The existing `maxActiveSessionElapsedSeconds` field remains owned by the M7-B0 preview scaffold and is validated as part of the shared config contract, but it does not cap individual active tick application.

## Save compatibility notes

No save field and no migration were added. M7-B2 continues using additive M7-B1 `SaveData.researchProgress`. Legacy saves with no `researchProgress`, and Unity `JsonUtility` empty default `ResearchProgressState` objects, resolve to safe no-apply output. Active ticks do not create progress state. The existing Set Research Pending developer control remains the explicit path that initializes a real zero-progress state.

## Determinism notes

`ResearchProgressApplyResolver` is pure and deterministic. It performs no save mutation and returns a summary containing the deterministic error code, elapsed tick seconds used, previous progress, applied delta, next progress, stable IDs, rule source, and `WouldCompleteResearch = false`. Repeated identical inputs return identical serialized summaries.

## Mutation boundaries

The active-tick integration writes only `Save.researchProgress.ProgressUnits` after a resolved apply summary. It does not mutate `Save.researchPending`, `CompletionPending`, heat, mana, loot, run history, structure runtime, total ticks, last offline summary, unlocks, costs, or rewards. Existing simulation tick behavior outside research application remains unchanged.

## Diagnostics notes

No new diagnostics line or localization key was added. Existing localization-backed Systems Diagnostics lines remain in place:

- Research Progress Preview continues to display the M7-B0 cumulative active-session preview.
- Research Progress State continues to display M7-B1 saved state and now reflects accumulated progress after active ticks.
- Clear Research Pending returns state diagnostics to safe no-pending output without stale project IDs.

## Developer control notes

Existing developer controls remain coherent:

- Set Research Pending initializes the single saved progress state at zero.
- Clear Research Pending clears both pending and progress state.
- Neither developer control completes research, grants rewards, charges costs, unlocks content, or processes offline progress.

## Test list

Added EditMode coverage:

- `ResearchProgressApplyResolverTests`
  - Safe null pending and missing progress output.
  - Matching state application summary.
  - Deterministic repeated inputs.
  - Missing, disabled, and invalid config handling.
  - Zero and negative elapsed seconds.
  - Non-finite or negative coefficient handling.
  - Invalid existing progress handling.
  - Overflow handling.
  - Slot and project mismatch handling.
  - Completion-pending safe rejection.
  - Whole-save non-mutation proof for the pure resolver.
- `ResearchProgressActiveTickIntegrationTests`
  - Explicit Set Research Pending zero initialization.
  - Exact one-tick and multi-tick accumulation.
  - No cumulative-preview double count.
  - Clear Research Pending stop behavior.
  - Missing, mismatched, and disabled safe no-apply behavior.
  - Large progress does not complete research.
  - Active apply mutation boundary assertions.
  - Existing preview/state diagnostics after active tick and no stale ID after clear.

Regression suites to run before merge:

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
11. Confirm no-pending state shows safe no-pending output.
12. Use Set Research Pending.
13. Confirm pending becomes true.
14. Confirm progress state starts at zero.
15. Let at least one active simulation tick occur.
16. Confirm saved progress state increases by the expected per-tick amount.
17. Confirm no research completion occurs.
18. Confirm no rewards, costs, unlocks, mana, loot, heat changes, extra structure mutation, or run history entries are granted by research progress.
19. Use Clear Research Pending.
20. Confirm pending becomes false.
21. Confirm progress state diagnostics return to no-pending or no-active-progress output.
22. Confirm no stale project ID remains.
23. Confirm no offline heat behavior appears.
24. Confirm no offline progression appears.
25. Confirm no M6 heat behavior changes.
26. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B2 does not modify `LootHeatCoolingResolver`, active-run heat delta composition, run simulation math, loot extraction logic, structure simulation logic, or any heat, loot, run, or structure tuning value. The pre-existing active tick heat decay path remains in place and unchanged apart from the adjacent research-only invocation.

## Scope confirmation

M7-B2 is active-session single-slot research progress accumulation scaffold only. It does not implement offline progression, offline heat, research completion, rewards, unlocks, costs, production UI, server verification, or multiple research slots. `OfflineSummary.WouldProcessOfflineProgress` remains false and `allowOfflineProgression` remains dormant.
