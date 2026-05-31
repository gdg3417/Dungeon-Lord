# M7-B4 Research Completion-Pending Marker Scaffold

## Scope delivered

M7-B4 adds the smallest active-session mutation slice after M7-B3. A pure deterministic `ResearchCompletionPendingApplyResolver` evaluates the current single-slot pending research, the persisted research progress state, and the existing content-owned completion eligibility scaffold. During active simulation ticks only, `GameRoot` invokes the resolver after M7-B2 progress accumulation and may set `Save.researchProgress.CompletionPending = true` when the resolver reports `WouldSetCompletionPending = true`.

## Explicit non-goals

This slice does not complete research, clear pending research, clear progress state, grant rewards, charge costs, unlock content, add prerequisites, add production research UI, add server verification, add multiple research slots, process offline progress, process offline heat, or change M6 heat behavior.

## Config ownership notes

The marker resolver consumes the existing `ResearchCompletionEligibilityScaffoldConfig`. The enabled flag, project ID, rule source ID, and required progress units remain content-owned. No gameplay tuning value was added to runtime C#.

## Save compatibility notes

No save field or migration was added. The marker uses the existing additive `SaveData.researchProgress.CompletionPending` field. Legacy saves without `researchProgress`, including Unity `JsonUtility` empty-default progress objects, resolve to a safe missing-state no-op.

## Determinism notes

`ResearchCompletionPendingApplyResolver` is pure and deterministic. Repeated identical inputs produce identical summaries. It does not read clocks, perform I/O, process offline time, or mutate save/runtime objects.

## Mutation boundaries

The only new allowed save mutation is setting `Save.researchProgress.CompletionPending = true` after an active tick when pending state, progress state, and content config are valid and current accumulated progress meets or exceeds the content-owned requirement. The integration does not mutate `researchPending`, slot ID, project ID, progress units, mana, loot, heat, run history, structure runtime, total ticks, or the last offline summary. Existing M7-B2 accumulation remains responsible for progress-unit changes.

## Diagnostics notes

Systems Diagnostics now includes a localization-backed `Research Completion Pending Apply` line showing resolved state, deterministic error, pending state, progress-state presence, slot, project, accumulated progress, required progress, eligibility, already-pending state, whether the marker would be set, the always-false completion output, and rule source. The read-only M7-B3 eligibility diagnostic treats a valid `CompletionPending = true` marker as resolved eligibility state rather than an inactive-state error while continuing to report `WouldSetCompletionPending = false` and `WouldCompleteResearch = false`. Current-state diagnostics intentionally avoid persisted last-apply architecture. Clearing research pending returns diagnostics to safe no-pending output without a stale project ID.

## Developer control notes

Existing developer controls remain coherent. Set Research Pending initializes zero progress with `CompletionPending = false`. Clear Research Pending clears both pending and progress scaffold state. Neither control completes research or grants anything.

## Test list

- `ResearchCompletionPendingApplyResolverTests`
- Updated `ResearchProgressActiveTickIntegrationTests`
- Existing M7-B0 through M7-B3 research resolver and diagnostics suites
- Existing offline summary, paging, M6 heat, run, loot, structure, and migration regression suites

## Manual Bootstrap smoke checklist

1. Open the Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 developer panel toggle still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 diagnostics page cycling still works.
6. Go to Systems Diagnostics.
7. Confirm Offline Summary still shows `wouldProcess=False`.
8. Confirm Research Pending displays correctly.
9. Confirm Research Progress Preview from M7-B0 still displays correctly.
10. Confirm Research Progress State from M7-B1 still displays correctly.
11. Confirm Research Completion Eligibility from M7-B3 still displays correctly.
12. Confirm Research Completion Pending Apply displays safely.
13. Confirm no-pending state shows safe no-pending output.
14. Use Set Research Pending.
15. Confirm pending becomes true.
16. Confirm progress state starts at zero and `CompletionPending=False`.
17. Confirm completion pending apply is false below requirement.
18. Let active simulation ticks accumulate enough progress to meet or exceed the content-owned requirement, or adjust only test config if manual smoke would take too long.
19. Confirm `CompletionPending=True`.
20. Confirm `researchPending` remains present.
21. Confirm progress is not reset.
22. Confirm no research completion occurs.
23. Confirm no rewards, costs, unlocks, mana, loot, heat changes, extra structure mutation, or run history entries are granted by the pending marker.
24. Use Clear Research Pending.
25. Confirm pending becomes false.
26. Confirm progress state and pending diagnostics return to no-pending output.
27. Confirm no stale project ID remains.
28. Confirm no offline heat behavior appears.
29. Confirm no offline progression appears.
30. Confirm no M6 heat behavior changes.
31. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B4 does not modify M6 heat systems, active-run heat delta composition, `LootHeatCoolingResolver`, run simulation math, loot extraction logic, structure simulation logic, or existing heat/loot/run/structure tuning.

## Scope confirmation

M7-B4 does not implement offline progression, offline heat, research completion, rewards, unlocks, costs, production UI, server verification, or multiple research slots. `WouldProcessOfflineProgress` remains false and any `allowOfflineProgression` content remains dormant.
