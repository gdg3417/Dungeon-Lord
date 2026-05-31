# M7-B1 Research Progress Save-State Scaffold

## Scope delivered

M7-B1 adds the smallest save-compatible single-slot research progress state contract after M7-B0. `SaveData.researchProgress` is an additive nullable `ResearchProgressState` field with slot ID, project ID, progress units, completion-pending marker, and rule-source ID. A pure `ResearchProgressStateResolver` reports the saved state safely for diagnostics. The existing developer-only Set Research Pending control initializes a matching zero-progress state, and Clear Research Pending clears both markers to prevent stale diagnostics.

## Explicit non-goals

M7-B1 does not accumulate progress from active-session ticks. It does not complete research, grant rewards, charge costs, unlock content, process offline progress, add offline rewards, process offline heat, add production research UI, add multiple research slots, add server verification, or change M6 heat behavior. It does not alter run simulation math, loot extraction logic, structure simulation logic, or existing tuning values.

## Save compatibility notes

`SaveData.researchProgress` is additive and nullable. No save schema version increment or migration backfill is required: Unity JSON deserialization leaves the missing field null for legacy saves, and the resolver reports a deterministic missing-state summary when research is pending. Existing saves do not require manual repair. New saves may round-trip the state without altering the existing `researchPending` marker.

## Legacy save behavior

A legacy save without `researchProgress` loads safely. If it has no research pending marker, diagnostics show safe no-pending output. If it has an existing `researchPending` marker, diagnostics show safe missing-progress-state output. The resolver does not synthesize, mutate, or repair save data during reads.

## Determinism notes

`ResearchProgressStateResolver.Resolve` is pure and deterministic for identical inputs. It performs ordinal slot and project ID comparisons and returns deterministic error codes for no pending research, a missing saved state, invalid pending state, stale slot, stale project, invalid progress units, and a completion-pending marker. It does not consume elapsed time, ticks, offline time, or config tuning. It does not mutate save data or complete research. `CompletionPending` remains false when the developer scaffold initializes a state.

## Diagnostics notes

Systems Diagnostics now includes a localization-backed Research Progress State line after the M7-B0 Research Progress Preview line. It renders safe no-pending, missing-state, valid zero-progress, stale or mismatched, invalid-progress, and completion-pending summaries. Cleared state cannot render a stale project ID because the developer clear control clears both saved research markers. Missing localization uses the existing stable localization-key fallback pattern.

## Developer control notes

The existing developer-only Set Research Pending control now initializes `researchProgress` for the same single slot and project with zero progress units, `CompletionPending == false`, and the loaded M7-B0 rule-source ID when present. The existing Clear Research Pending control clears `researchProgress` with `researchPending`. Neither control accumulates progress, completes research, charges costs, grants rewards, unlocks content, or changes simulation state.

## Test list

Added or updated EditMode coverage:

- `ResearchProgressStateResolverTests`
  - null pending and null state safe no-pending output
  - pending with null state safe missing-state output
  - matching zero-progress state resolved output
  - stale slot and stale project deterministic mismatch output
  - no-pending state does not expose saved progress as active
  - negative, NaN, and infinite progress-unit safe handling
  - completion-pending marker safe reporting without completion
  - repeated identical input determinism
  - read-only save, pending state, progress state, heat, mana, loot container, run history, structure runtime, total ticks, and last offline summary assertions
  - legacy JSON without `researchProgress`
  - additive JSON round-trip with `researchProgress`
- `ResearchProgressDiagnosticsTests`
  - localized Research Progress State line
  - safe no-pending, missing-state, valid zero-progress, and stale-state output
  - stable localization fallback
  - developer set/clear diagnostics coherence and stale-project prevention
  - diagnostics read-only behavior
  - runtime C# visible-English guardrail
- `ResearchPendingDevControlsTests`
  - developer set initializes a matching zero-progress scaffold
  - developer clear removes both saved research markers
  - unrelated save state remains unchanged

Required regression EditMode suites before merge:

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
10. Confirm new Research Progress State diagnostics display safely.
11. Confirm no pending state shows safe no-pending output.
12. Use Set Research Pending.
13. Confirm pending becomes true.
14. Confirm progress state diagnostics are coherent for the pending slot and project.
15. Confirm no research completion occurs.
16. Confirm no progress accumulation from ticks occurs.
17. Confirm no rewards, costs, unlocks, mana, loot, heat changes, structure ticks, or run history entries are granted.
18. Use Clear Research Pending.
19. Confirm pending becomes false.
20. Confirm progress state diagnostics return to no-pending or no-active-progress output.
21. Confirm no stale project ID remains.
22. Confirm no offline heat behavior appears.
23. Confirm no offline progression appears.
24. Confirm no M6 heat behavior changes.
25. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B1 does not change `LootHeatCoolingResolver`, active-run heat delta composition, run simulation math, loot extraction logic, structure simulation logic, heat tiers, or existing tuning values. No offline heat processing was added. M6 heat behavior is preserved.

## Scope confirmation

M7-B1 is a single-slot save-state contract scaffold only. It does not implement progress accumulation, offline progression, offline heat, research completion, rewards, unlocks, costs, production UI, server verification, or multiple research slots. `allowOfflineProgression` remains dormant and `WouldProcessOfflineProgress` remains false.
