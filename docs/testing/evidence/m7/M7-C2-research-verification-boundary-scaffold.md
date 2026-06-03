# M7-C2 Research Verification Boundary Scaffold

## Scope delivered

M7-C2 adds a smallest deterministic, read-only research verification boundary scaffold for the existing single-slot research lifecycle. The scaffold adds:

- `ResearchVerificationScaffoldConfig`, a content-owned config record with `enabled`, `ruleSourceId`, and `verificationMode`.
- `ResearchVerificationBoundaryResolver`, a pure resolver that summarizes how future online production verification is represented while remaining safely blocked.
- Research Status Diagnostics Page 6/6 lines for `Research Verification Boundary` and `Research Verification Safety` using localization-backed format strings.
- EditMode coverage for resolver safe outputs, deterministic repetition, no mutation, diagnostics display, fallback localization, stale-ID cleanup, duplicate-completed blocking, and safety flags.

## Explicit non-goals

M7-C2 does not add production research UI, production claim controls, backend calls, server verification, rewards, unlocks, costs, multiple active research slots, offline research progression, offline heat, or M6 heat behavior changes.

## Online verification boundary notes

The scaffold intentionally does not verify anything. `disabled` and `unavailable` modes are blocked, and `localDevPlaceholder` only reports that a local placeholder mode exists for diagnostics. `VerificationSatisfied` remains false, `CanClaimProduction` remains false, and `WouldCallServer` remains false in all resolver outputs.

No backend URL, token, server auth, API path, HTTP client, server clock check, or network behavior was added.

## Read-only behavior notes

`ResearchVerificationBoundaryResolver` reads the current pending research, progress state, completed research state, completion eligibility scaffold config, and verification scaffold config. It does not mutate save data, research pending/progress/completed state, diagnostics state, heat, mana, loot, run history, structure runtime, total ticks, or offline summary.

Already-completed active projects are treated as duplicate-safe blocked output and are never production-claimable.

## Save compatibility notes

No save fields, save migrations, stable ID renames, or serialized research lifecycle behavior changes were added. Existing `researchPending`, `researchProgress`, and `completedResearch` behavior remains unchanged. Legacy saves with missing active research state remain safe no-op output.

## Diagnostics notes

Research Diagnostics Page 5/6 remains focused on the existing research lifecycle lines. Research Status Diagnostics Page 6/6 now contains:

- Research Status Presentation
- Research Status Safety
- Research Verification Boundary
- Research Verification Safety

Diagnostics strings use localization keys with the existing safe fallback-key behavior. Page count remains 6, F3 cycles all six pages, F2 run diagnostics focus remains independent, and the existing scroll reset/clamp behavior is preserved.

## Test list

Added or updated EditMode coverage for:

- `ResearchVerificationBoundaryResolverTests`
- `ResearchCompletionEligibilityDiagnosticsTests`
- `BootstrapOverlayPagingTests`

Regression suites that should remain green before merge:

- `ResearchStatusPresenterTests`
- `ResearchCompletionEligibilityDiagnosticsTests`
- `BootstrapOverlayPagingTests`
- `ResearchCompletionClaimApplyResolverTests`
- `ResearchPendingDevControlsTests`
- `CompletedResearchStateResolverTests`
- `ResearchCompletionClaimReadinessResolverTests`
- `ResearchCompletionPendingApplyResolverTests`
- `ResearchCompletionEligibilityResolverTests`
- `ResearchProgressApplyResolverTests`
- `ResearchProgressActiveTickIntegrationTests`
- `ResearchProgressStateResolverTests`
- `ResearchProgressResolverTests`
- `ResearchProgressDiagnosticsTests`
- `OfflineSummaryResolverTests`
- `OfflineSummaryDiagnosticsTests`
- `RunSimulationTests`
- `RunHeatDeltaResolverTests`
- `RunHeatStateApplyResolverTests`
- `LootHeatCoolingResolverTests`
- `LootExtractionResolverTests`
- `StructureSimulationTests`
- `MigrationRunnerTests`, if present

## Manual Bootstrap smoke checklist

1. Open Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 developer panel toggle still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 cycles through 6 diagnostics pages.
6. Confirm Research Diagnostics Page 5/6 is readable.
7. Confirm Research Status Diagnostics Page 6/6 is readable.
8. Confirm Research Status Presentation displays.
9. Confirm Research Verification Boundary displays.
10. Confirm no-pending output is safe.
11. Use Set Research Pending.
12. Confirm below requirement does not allow production claim.
13. Let active ticks reach `CompletionPending=True`.
14. Confirm status shows `VerificationRequired`.
15. Confirm `verificationRequired=True`.
16. Confirm `verificationSatisfied=False`.
17. Confirm `canClaimProduction=False`.
18. Confirm `wouldCallServer=False`.
19. Confirm no production claim button exists.
20. Confirm no rewards, costs, unlocks, mana, loot, heat changes, run history entries, offline progression, or offline heat.
21. Use developer-only Claim Research Completion.
22. Confirm active pending and progress clear.
23. Confirm completed research state updates.
24. Confirm Research Verification Boundary returns safe no-pending output.
25. Use Set Research Pending again.
26. Confirm completed duplicate active project is not production-claimable.
27. Use Clear Research Pending.
28. Confirm no stale active project IDs remain.
29. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-C2 does not touch run simulation math, heat delta/application logic, heat cooling, loot extraction, structure simulation, or offline heat. Verification diagnostics only report false safety flags and do not interact with M6 heat systems.

## Scope confirmation

M7-C2 does not add production UI, a production claim button, server verification, backend API, server calls, rewards, unlocks, costs, offline progression, offline heat, or multiple active research slots. `VerificationSatisfied`, `CanClaimProduction`, `WouldCallServer`, `WouldGrantRewards`, `WouldUnlockContent`, `WouldChargeCosts`, and `WouldProcessOfflineProgress` remain false in the verification boundary scaffold.
