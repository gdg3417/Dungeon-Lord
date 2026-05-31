# M7-B0 Active-Session Research Progress Scaffold

## Scope delivered

M7-B0 adds the smallest deterministic active-session research progress scaffold after M7-A3. The scaffold resolves a read-only `ResearchProgressSummary` preview from the current single-slot `ResearchPendingState`, loaded content config, and active-session elapsed seconds derived from active simulation ticks. Bootstrap Systems Diagnostics renders the preview summary and refreshes it after the existing Set Research Pending and Clear Research Pending developer controls.

## Explicit non-goals

This milestone does not complete research, grant rewards, charge costs, unlock content, process offline progress, persist research progress, add production research UI, add multiple research slots, or add server verification. It does not add offline rewards or offline heat processing. It does not alter run simulation, loot extraction, structure simulation, or heat behavior.

## Config ownership notes

`Assets/_Project/Data/Bootstrap/content_bootstrap.json` owns the M7-B0 `researchProgressScaffold` tuning values:

- `enabled`
- `ruleSourceId`
- `progressPerActiveSecond`
- `maxActiveSessionElapsedSeconds`

Runtime C# consumes this typed loaded config. Missing config, disabled config, invalid config, invalid elapsed seconds, no pending research, and invalid pending state return deterministic safe summaries without mutation.

## Save compatibility notes

No save schema fields were added. Existing and legacy saves remain compatible because the resolver reads the existing optional `SaveData.researchPending` marker only. The save model defaults that marker to null and the existing Clear Research Pending developer control writes null. Diagnostics also safely normalize a legacy empty marker through `ResearchPendingResolver` without mutating the save. Research progress is not persisted. `SaveData.lastOfflineSummary` remains diagnostics-only and `OfflineSummary.WouldProcessOfflineProgress` remains false.

## Determinism notes

`ResearchProgressResolver.Resolve` is pure and deterministic for identical inputs. It accepts a pending marker, typed scaffold config, and integral active-session elapsed seconds. Integral seconds avoid non-finite elapsed input. Negative elapsed seconds fail safely. Elapsed seconds are capped by content-owned config before calculating the preview delta. Every returned summary keeps `WouldCompleteResearch` false.

## Diagnostics notes

Systems Diagnostics includes a localization-backed Research Progress Preview line. A valid pending marker renders a preview. The existing `ResearchPendingResolver` result is the diagnostics source of truth: when it reports `Pending == false`, including for nullable or legacy empty markers, Research Progress Preview renders the same safe no-pending summary without mutating the save. The line refreshes immediately after Set Research Pending and Clear Research Pending so cleared state does not display stale project data. Missing localization uses the existing stable localization-key fallback pattern.

## Test list

Added EditMode coverage:

- `ResearchProgressResolverTests`
  - deterministic repeated resolution
  - safe no-pending output
  - resolved and capped preview output
  - missing, disabled, invalid, and non-finite coefficient config handling
  - zero and negative elapsed handling
  - null and invalid pending state handling
  - read-only save, research marker, heat, mana, run history, structure runtime, total ticks, and last offline summary assertions
  - no completion assertion
- `ResearchProgressDiagnosticsTests`
  - localized zero and non-zero active-session preview formatting
  - nullable and legacy empty pending markers normalize to the same safe no-pending diagnostics output without save mutation
  - stable localization fallback
  - Systems Diagnostics placement
  - no raw localization key during normal localized diagnostics
  - Set Research Pending refresh
  - Clear Research Pending stale-data prevention
  - diagnostics read-only assertions
  - no visible diagnostic English hardcoded in runtime C#

Required regression EditMode suites before merge:

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
8. Confirm Research Pending still displays correctly.
9. Use Set Research Pending.
10. Confirm Research Pending shows `pending=True`.
11. Confirm Research Progress diagnostics show preview or safe no-progress scaffold output.
12. Confirm no research completion occurs.
13. Confirm no rewards, costs, unlocks, mana, loot, heat changes, structure ticks, or run history entries are granted.
14. Use Clear Research Pending.
15. Confirm Research Pending shows `pending=False`.
16. Confirm Research Progress diagnostics return to safe no-pending or no-progress output.
17. Confirm no offline heat behavior appears.
18. Confirm no M6 heat behavior changes.
19. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B0 does not change `LootHeatCoolingResolver`, active-run heat delta composition, run simulation math, loot extraction logic, structure simulation logic, heat tiers, or existing tuning values. The accepted M6-FU0 two-stage active-run cooling composition remains untouched.

## Scope confirmation

M7-B0 is active-session preview only. It does not implement offline rewards, offline heat, research completion, rewards, unlocks, costs, production UI, server verification, or multiple research slots. `allowOfflineProgression` remains dormant and `WouldProcessOfflineProgress` remains false.
