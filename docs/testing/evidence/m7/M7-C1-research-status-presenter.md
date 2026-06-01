# M7-C1 Research Status Presenter

## Scope delivered

M7-C1 adds the smallest production-safe, read-only research status presenter after M7-C0. `ResearchStatusPresenter` deterministically summarizes the current MVP single-slot research lifecycle into `ResearchStatusPresentation` without mutating save data or runtime state. The presentation distinguishes no research, active progress, active completion pending, verification required, completed research, and blocked or invalid inputs.

## Explicit non-goals

M7-C1 does not add production research UI, a production claim button, server calls, backend API code, online verification implementation, rewards, unlocks, costs, multiple active slots, offline research progression, offline heat, or changes to active research progress math, completion-pending application, developer-only claim mutation, completed-research saves, loot, run simulation, structure simulation, or M6 heat behavior.

## Read-only productionization boundary notes

The presenter is a pure deterministic service. It accepts existing research state and the existing typed completion-eligibility scaffold config, then returns an in-memory presentation model. It does not create save fields, migrations, production actions, or cross-system writes. `CanClaimProduction`, `WouldGrantRewards`, `WouldUnlockContent`, `WouldChargeCosts`, and `WouldProcessOfflineProgress` remain false for every presentation.

## Online verification boundary notes

Future production research start and completion require online verification. This PR intentionally does not implement verification services, availability flags, server verification, backend APIs, or server calls. A completion-pending project is surfaced as `VerificationRequired`; it is never surfaced as production-claimable.

## Save compatibility notes

No save fields, migrations, or stable ID renames are added. Legacy saves with no research fields remain safe. Existing `researchPending`, `researchProgress`, and `completedResearch` behavior is unchanged.

## Diagnostics notes

Research Diagnostics Page 5/5 retains its existing M7-B0 through M7-B7 lines and adds one localization-backed `Research Status Presentation` line. The line reports state, existing-state presence, slot/project IDs, progress, requirement, completion and verification state, production-claim safety flags, status localization key, and rule source. Diagnostics scrolling remains enabled. Cleared active state does not display stale active slot or project IDs; developer-only claim displays completed state safely.

## Test list

- `ResearchStatusPresenterTests`
- Updated `ResearchCompletionEligibilityDiagnosticsTests`
- Updated `BootstrapOverlayPagingTests`
- Existing research resolver, offline summary, M6 heat, loot, run simulation, structure simulation, and migration regression suites listed in the PR description

## Manual Bootstrap smoke checklist

1. Open Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 developer panel toggle still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 diagnostics page cycling still works.
6. Go to Research Diagnostics Page 5/5.
7. Confirm Research Diagnostics scrolling still works.
8. Confirm M7-B0 through M7-B7 research diagnostics still display.
9. Confirm Research Status Presentation displays safely.
10. Confirm no-pending state shows `NoResearch` or the intended safe no-pending output.
11. Use Set Research Pending.
12. Confirm pending becomes true.
13. Confirm below requirement shows `ActiveInProgress`.
14. Let active ticks reach `CompletionPending=True`.
15. Confirm production status shows `VerificationRequired`.
16. Confirm `CanClaimProduction=False`.
17. Confirm no production claim button exists.
18. Confirm no rewards, costs, unlocks, mana, loot, heat changes, run history entries, offline progression, or offline heat.
19. Use developer-only Claim Research Completion.
20. Confirm active pending and progress clear.
21. Confirm completed research state updates.
22. Confirm Research Status Presentation shows completed or safe no-pending output without stale pending or progress project IDs.
23. Use Set Research Pending again.
24. Confirm completed research is not cleared.
25. Use Clear Research Pending.
26. Confirm no stale project IDs remain.
27. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-C1 does not alter heat configuration, heat resolution, heat application, loot cooling, run heat deltas, offline summaries, or any M6 runtime path. The presenter reads only research lifecycle inputs.

## Confirmation

M7-C1 does not add production UI, production claim behavior, rewards, unlocks, costs, offline progression, offline heat, server verification, backend calls, or multiple active research slots. `WouldProcessOfflineProgress` remains false.
