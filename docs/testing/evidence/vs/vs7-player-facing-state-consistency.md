# VS7: Player-Facing MVP State Consistency Evidence

## Scope

- Aligned MVP player-facing research status presentation so the MVP Loop Summary and Guided MVP Action are driven by the same actionable verification state.
- Kept the MVP Loop Summary, Guided MVP Action, Minimal MVP Actions, and player-facing banners within the existing vertical slice presentation surfaces.
- Preserved developer diagnostics, including detailed research status presentation and research verification boundary diagnostics, so raw scaffold IDs and deterministic error details remain available outside player-facing panels.

## Non-goals

- No gameplay systems were added.
- No simulation behavior was changed.
- No save schema changes were made.
- No tuning values, costs, unlocks, rewards, or progression rules were changed.
- No scenes, prefabs, project settings, TextMeshPro assets, generated test scenes, or unrelated assets were modified.
- No tutorial framework was created.

## Changed files

- `Assets/_Project/Scripts/Services/MvpPlayerLoopSummaryPresenter.cs`
  - Added a narrow actionable-verification gate for player-facing research status and summary suggestions.
  - Completion-pending research now surfaces `ui.research.status.verification_required` only when the verification boundary is resolved and available.
  - Unavailable, invalid, blocked, or unresolved verification states surface `ui.research.status.blocked_or_invalid` and do not select verify-research guidance.
- `Assets/_Project/Scripts/Services/GuidedMvpActionPathPresenter.cs`
  - Requires the shared summary verification state to be available before selecting the verify-research guided action.
- `Assets/_Project/Tests/EditMode/MvpPlayerLoopSummaryPresenterTests.cs`
  - Added coverage for completion-pending research with unavailable verification showing the localized unavailable status and avoiding the verify suggestion.
  - Kept valid completion-pending verification coverage for the localized verification-required status.
- `Assets/_Project/Tests/EditMode/GuidedMvpActionPathPresenterTests.cs`
  - Added coverage that unavailable research verification does not choose the verify-research guided action.
  - Strengthened no-placement and placement-without-run priority coverage so they still win before research guidance.
  - Kept deterministic heat-pressure and poor-loot-extraction guidance coverage.
- `docs/testing/evidence/vs/vs7-player-facing-state-consistency.md`
  - Added this evidence record and manual smoke checklist.

## Tests added or updated

- `MvpPlayerLoopSummaryPresenterTests.Resolve_CompletionPendingWithUnavailableVerification_ReportsResearchUnavailableAndDoesNotSuggestVerify`
- `GuidedMvpActionPathPresenterTests.Resolve_CompletionPendingWithUnavailableResearch_DoesNotSuggestVerifyingResearchStatus`
- Updated `GuidedMvpActionPathPresenterTests.Resolve_NoPlacementState_SuggestsPlacingStructure` to prove no-placement guidance wins before research guidance.
- Updated `GuidedMvpActionPathPresenterTests.Resolve_PlacementExistsButNoRunState_SuggestsRunningDungeon` to prove run-or-observe guidance wins before research guidance.
- Existing retained coverage verifies:
  - valid completion-pending verification selects verify-research guidance;
  - heat-pressure guidance remains deterministic;
  - poor-loot-extraction guidance remains deterministic;
  - MVP Loop Summary and Guided MVP Action panels resolve player-facing text through localization keys;
  - detailed research status and verification diagnostics remain exposed;
  - F2 hides player-facing panels and restores them;
  - F3 cycles all diagnostics pages and wraps;
  - view-only refresh, F2, F3, PageUp/PageDown-equivalent scroll calls, and mouse-wheel-equivalent scroll calls do not mutate save state.

## Manual smoke checklist

1. Launch Bootstrap.
2. Confirm MVP Loop Summary is visible.
3. Confirm Guided MVP Action is visible.
4. Confirm Minimal MVP Actions is visible.
5. Confirm the Research line and Guided MVP Action do not contradict each other.
6. Click placement action.
7. Click run action.
8. Confirm latest run, mana, loot, heat, research, next action, and guided action remain coherent.
9. Confirm placement and banners use localized labels.
10. Confirm diagnostics still show detailed developer information.
11. Confirm F1 opens and closes Dev Panel.
12. Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
13. Confirm F2 again restores player-facing panels.
14. Confirm F3 cycles all diagnostics pages and wraps.
15. Confirm no Console errors.
16. Confirm no unexpected generated files.

## Explicit MVP scope confirmation

Confirmed: this VS7 change adds no rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid heat tiers, seasons, leaderboards, monetization, new monster families, or tutorial framework.
