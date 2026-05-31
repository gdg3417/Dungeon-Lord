# M7-B6 Completed Research Save-State Scaffold

## Scope delivered

M7-B6 adds the smallest durable, additive save-state contract for completed research. `SaveData.completedResearch` stores a `CompletedResearchState` with completed project IDs, the last completed project ID, and the last completion rule-source ID. A pure `CompletedResearchStateResolver` produces read-only deterministic summary diagnostics. Research Diagnostics Page 5/5 now appends one localization-backed **Completed Research State** line.

## Explicit non-goals

This scaffold does not claim or complete research. It does not mutate completed research state from ticks, diagnostics, or developer controls. It does not clear pending research or progress beyond the existing Clear Research Pending developer behavior. It does not add rewards, reward tables, unlocks, unlock tables, costs, production research UI, server verification, offline progression, offline rewards, offline heat, or multiple active research slots.

## Save compatibility notes

`SaveData.completedResearch` is an additive nullable field. No schema migration is required. Existing save fields and existing single-slot pending/progress contracts are unchanged.

## Legacy save behavior

Legacy JSON with no `completedResearch` field deserializes with a null completed state. The resolver safely reports `resolved=True`, `error=0`, `hasState=False`, and `completedCount=0`. Empty default objects and null or empty project-ID arrays produce the same safe empty completed-state summary.

## Determinism notes

The resolver is pure and deterministic. It filters null, empty, and whitespace-only completed IDs and counts unique valid project IDs with ordinal stable-ID comparison. Duplicate completed IDs therefore produce one deterministic count entry. Repeated identical inputs return identical summaries.

## Mutation boundaries

The resolver does not mutate saves, completed state, pending state, progress state, heat, mana, loot/run history, structure runtime, total ticks, or the persisted offline summary. `WouldBlockClaimAsDuplicate`, `WouldGrantRewards`, and `WouldUnlockContent` remain false. No claim mutation path exists in M7-B6.

## Diagnostics notes

Research Diagnostics Page 5/5 preserves the M7-B0 through M7-B5 lines and adds the localized `ui.dev.completed_research_state_format` line. It reports completed count, safe last-completed metadata, current pending and progress IDs, a read-only already-completed preview, false mutation/reward/unlock flags, and the completed-state rule source. After existing Clear Research Pending behavior clears pending and progress state, the diagnostics resolver receives no active project IDs and does not display stale active-project output.

## Developer control notes

Existing Set Research Pending still initializes a single pending slot and zero progress with `CompletionPending=False`. Existing Clear Research Pending still clears pending and progress state. Neither control writes completed research state, completes research, grants rewards, charges costs, unlocks content, or processes offline progress.

## Test list

- `CompletedResearchStateResolverTests`
- `ResearchCompletionEligibilityDiagnosticsTests`
- `ResearchPendingDevControlsTests`
- `BootstrapOverlayPagingTests`
- Existing M7-B0 through M7-B5 research resolver and diagnostics regression suites
- Existing offline-summary, migration, run, heat, loot, and structure regression suites

## Manual Bootstrap smoke checklist

1. Open Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 developer panel toggle still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 diagnostics page cycling still works.
6. Go to Research Diagnostics Page 5/5.
7. Confirm Research Diagnostics scrolling still works.
8. Confirm M7-B0 through M7-B5 research diagnostics still display.
9. Confirm Completed Research State displays safely.
10. Confirm no completed state shows completed count 0.
11. Use Set Research Pending.
12. Confirm pending becomes true.
13. Confirm completed research state is unchanged.
14. Let active ticks reach `CompletionPending=True` and `readyForClaim=True`.
15. Confirm completed research state is still unchanged.
16. Confirm no research completion occurs.
17. Confirm no rewards, costs, unlocks, mana, loot, heat changes, run history entries, offline progression, or offline heat.
18. Use Clear Research Pending.
19. Confirm pending becomes false.
20. Confirm progress state and claim readiness diagnostics return to no-pending output.
21. Confirm Completed Research State remains safe and does not show stale pending project IDs.
22. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B6 does not modify M6 heat behavior, active-run heat delta composition, `LootHeatCoolingResolver`, run simulation math, loot extraction logic, structure simulation logic, or existing heat/loot/run/structure tuning values. `WouldProcessOfflineProgress` remains observational and false. `allowOfflineProgression` remains dormant for research rewards and heat.

## Confirmation

M7-B6 is completed research save-state scaffold only. It does not implement claim mutation, research completion, rewards, unlocks, costs, production UI, server verification, offline progression, offline heat, or multiple active research slots.
