# M7-B5 Research Completion Claim Readiness Scaffold

## Scope delivered

M7-B5 adds a smallest-possible read-only scaffold that resolves whether the existing single-slot research state is ready for a future completion claim action. `ResearchCompletionClaimReadinessResolver` reads the current pending state, progress state, and existing content-owned `ResearchCompletionEligibilityScaffoldConfig`. It reports deterministic diagnostics only.

## Explicit non-goals

This change does not implement research completion, a completion claim mutation, rewards, unlocks, costs, production UI, server verification, completed research history, completed project storage, reward tables, unlock tables, or multiple research slots. It does not add or process offline research progression, offline rewards, or offline heat.

## Config ownership notes

The resolver reuses `ResearchCompletionEligibilityScaffoldConfig`. The required progress units, project ID, enable flag, and rule source ID remain content/config-owned. M7-B5 adds no runtime gameplay tuning constants and no new config assets.

## Save compatibility notes

M7-B5 adds no save field and no migration. Legacy saves with no `researchProgress`, and Unity `JsonUtility` empty-default `ResearchProgressState` values, resolve safely as missing progress state and not ready for claim.

## Determinism notes

The resolver is pure and deterministic. Identical pending state, progress state, and config inputs produce identical summaries. Readiness is true only when all validated state/config inputs match, progress meets or exceeds the content-owned requirement, and `CompletionPending` is already true.

## Mutation boundaries

The resolver does not mutate `SaveData`, `researchPending`, `researchProgress`, `CompletionPending`, heat, mana, loot, run history, structure runtime, total ticks, or the last offline summary. All future mutation flags (`WouldCompleteResearch`, `WouldGrantRewards`, `WouldUnlockContent`, and `WouldClearPending`) remain false.

## Diagnostics notes

Systems Diagnostics adds one localization-backed `Research Completion Claim Readiness` line. It displays resolved/error status, pending and progress-state presence, slot/project IDs, progress and required units, completion-pending status, eligibility, readiness, all future mutation flags, and rule source. Missing localization follows the existing safe fallback-key behavior. Clearing research pending returns the line to no-pending output without stale project IDs.

## Developer control notes

Existing Set Research Pending and Clear Research Pending behavior remains unchanged. Set Research Pending initializes zero progress with `CompletionPending=false`. Clear Research Pending clears the pending and progress state. Neither control completes research or grants anything.

## Test list

Added `ResearchCompletionClaimReadinessResolverTests` for no-pending, missing/default state, below/equal/above requirement, completion-pending gating, deterministic repetition, disabled/missing/invalid config, invalid required progress, invalid existing progress, slot/project mismatch, immutable future-mutation flags, and save/adjacent-runtime non-mutation.

Updated `ResearchCompletionEligibilityDiagnosticsTests` to verify localization-backed Systems Diagnostics visibility, no raw localization key during loaded-content play, fallback-key behavior, safe no-pending output, below-requirement not-ready output, eligible-but-not-completion-pending not-ready output, ready output after the existing completion-pending apply scaffold, no mutation, and no stale IDs after clear.

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
11. Confirm Research Completion Eligibility from M7-B3 displays correctly.
12. Confirm Research Completion Pending Apply from M7-B4 displays correctly.
13. Confirm Research Completion Claim Readiness displays safely.
14. Confirm no-pending state shows safe no-pending output.
15. Use Set Research Pending.
16. Confirm pending becomes true.
17. Confirm progress state starts at zero and `CompletionPending=False`.
18. Confirm claim readiness is false below requirement.
19. Let active simulation ticks accumulate enough progress to meet or exceed the content-owned requirement.
20. Confirm `CompletionPending=True`.
21. Confirm claim readiness becomes true.
22. Confirm `researchPending` remains present.
23. Confirm `researchProgress` remains present.
24. Confirm no research completion occurs.
25. Confirm no rewards, costs, unlocks, mana, loot, heat changes, extra structure mutation, or run history entries are granted.
26. Use Clear Research Pending.
27. Confirm pending becomes false.
28. Confirm progress state and claim readiness diagnostics return to no-pending output.
29. Confirm no stale project ID remains.
30. Confirm no offline heat behavior appears.
31. Confirm no offline progression appears.
32. Confirm no M6 heat behavior changes.
33. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B5 does not change M6 heat behavior, active-run heat delta composition, `LootHeatCoolingResolver`, run simulation math, loot extraction logic, structure simulation logic, or existing heat/loot/run/structure tuning values.

## Confirmation

M7-B5 does not implement offline progression, offline heat, research completion, completion claim mutation, rewards, unlocks, costs, production UI, server verification, completed research history, or multiple research slots. `WouldProcessOfflineProgress` remains false and `allowOfflineProgression`, if present, remains dormant.
