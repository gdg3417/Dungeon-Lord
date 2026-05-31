# M7-B7 Research Completion Claim State Transition Scaffold

## Scope delivered

M7-B7 adds the smallest developer-only research completion claim state transition after M7-B6. A content-owned `researchCompletionClaimScaffold` supplies the enabled gate and stable claim rule-source ID. The pure deterministic `ResearchCompletionClaimApplyResolver` previews whether the current single-slot research project may be claimed. A separate Bootstrap developer control applies the previewed transition only when research is ready for claim.

A successful developer-only claim performs exactly these new mutations:

1. Append the current project ID to `Save.completedResearch.ProjectIds` if it is not already present.
2. Set `Save.completedResearch.LastCompletedProjectId`.
3. Set `Save.completedResearch.LastCompletionRuleSourceId` from content-owned claim config.
4. Clear `Save.researchPending`.
5. Clear `Save.researchProgress`.

## Explicit non-goals

M7-B7 does not grant rewards, charge costs, unlock content, process offline progress, process offline heat, add reward tables, add cost tables, add unlock tables, add prerequisites, add production research UI, add multiple active research slots, add automatic claiming, or add server verification. It does not expand M6 heat behavior.

## Save compatibility notes

M7-B7 reuses the additive nullable `SaveData.completedResearch` field introduced in M7-B6. It does not add another save field and does not require a migration. Existing pending and progress contracts remain single-slot and unchanged.

## Legacy save behavior

Legacy saves with no serialized `completedResearch` state remain safe. The pure resolver treats missing completed state as empty. The developer-only successful claim path creates `CompletedResearchState` only when applying a valid ready claim. If `ProjectIds` is null, the successful claim path treats it as empty and initializes the appended array.

## Determinism notes

`ResearchCompletionClaimApplyResolver` is pure and deterministic. It uses ordinal stable-ID comparisons, validates pending/progress/config input, returns deterministic no-op error codes for invalid or not-ready states, never mutates save input, and returns would-apply output only for a valid ready non-duplicate project. The developer control preserves existing completed-project order and appends the new project once.

## Mutation boundaries

The successful developer-only claim control mutates only `Save.completedResearch`, `Save.researchPending`, and `Save.researchProgress`. Resolver previews and diagnostics do not mutate save state. The claim scaffold does not mutate heat, mana, loot, run history, structure runtime, total ticks except the already-established developer save pattern if a save service is bound, or persisted offline summary. `WouldGrantRewards`, `WouldUnlockContent`, `WouldChargeCosts`, and `WouldProcessOfflineProgress` always remain false.

## Diagnostics notes

Research Diagnostics Page 5/5 preserves the M7-B0 through M7-B6 lines and appends the localization-backed **Research Completion Claim Apply** line. It shows resolved state, deterministic error, pending/progress/completed-state presence, slot and project IDs, progress and requirement, completion-pending/eligibility/readiness flags, duplicate status, would-record and would-clear flags, false reward/unlock/cost/offline flags, and the content-owned claim rule source. No-pending and cleared states do not retain stale active project IDs. Existing scrolling remains intact with the additional line.

## Developer control notes

Bootstrap developer controls now include the localization-backed **Claim Research Completion** action. It is separate from Set Research Pending and Clear Research Pending. Set Research Pending continues to create zero progress with `CompletionPending=False`. Clear Research Pending continues to clear pending and progress without clearing completed research. Claim Research Completion resolves readiness before mutation and safely no-ops for no pending state, below-threshold progress, inactive completion-pending state, invalid config, or duplicate completed projects.

## Test list

- `ResearchCompletionClaimApplyResolverTests`
- `ResearchPendingDevControlsTests`
- `ResearchCompletionEligibilityDiagnosticsTests`
- `BootstrapOverlayPagingTests`
- `CompletedResearchStateResolverTests`
- `ResearchCompletionClaimReadinessResolverTests`
- Existing M7-B0 through M7-B6 research regression suites
- Existing offline-summary, migration, run, heat, loot, and structure regression suites

## Manual Bootstrap smoke checklist

1. Open Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 developer panel toggle still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 diagnostics page cycling still works.
6. Go to Research Diagnostics Page 5/5.
7. Confirm Research Diagnostics scrolling still works.
8. Confirm M7-B0 through M7-B6 research diagnostics still display.
9. Confirm Research Completion Claim Apply displays safely.
10. Confirm no-pending state shows safe no-op output.
11. Use Set Research Pending.
12. Confirm pending becomes true.
13. Confirm completed research state still shows completed count 0.
14. Confirm claim apply is no-op below requirement.
15. Let active ticks reach `CompletionPending=True` and `readyForClaim=True`.
16. Confirm claim apply shows it would record completed research and clear pending and progress, while reward and unlock flags remain false.
17. Use Claim Research Completion.
18. Confirm Research Pending becomes false.
19. Confirm Research Progress State returns to no-pending or no-state output.
20. Confirm Completed Research State shows completed count 1.
21. Confirm last completed project matches the claimed project.
22. Confirm no stale pending project ID remains.
23. Confirm no stale progress project ID remains.
24. Confirm no rewards, costs, unlocks, mana, loot, heat changes, run history entries, offline progression, or offline heat.
25. Use Set Research Pending again.
26. Confirm completed research state is not cleared.
27. Use Clear Research Pending.
28. Confirm completed research state is still not cleared.
29. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-B7 does not modify M6 heat behavior, active-run heat delta composition, `LootHeatCoolingResolver`, run simulation math, loot extraction logic, structure simulation logic, or existing heat/loot/run/structure tuning values. `WouldProcessOfflineProgress` remains false. `allowOfflineProgression` remains dormant for research claims, rewards, and heat.

## Confirmation

M7-B7 is a developer-only research completion claim state transition scaffold. The only allowed new mutation is recording completed research and clearing active pending and progress state on a successful developer-only claim. It does not implement rewards, unlocks, costs, production UI, server verification, offline progression, offline heat, or multiple active research slots.
