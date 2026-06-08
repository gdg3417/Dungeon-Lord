# VS14 Player-facing run result feedback evidence

## Scope

- Added a read-only player-facing run result feedback presenter for the MVP vertical slice.
- Added overlay-memory run feedback after the player clicks **Run or observe dungeon**.
- Feedback is derived from the existing MVP player loop summary before and after the existing run path.
- Feedback communicates run status, mana after the run, loot generated/extracted/tradeable, heat before and after the run, and a concise localized interpretation.
- Feedback is displayed near the player-facing status/banner/placement feedback area and is hidden by F2 Run Diagnostics Focus with the rest of the player-facing panels.

## Non-goals

- No gameplay systems were added.
- No simulation behavior was added or changed.
- No formulas or tuning were added or changed.
- No rewards, unlocks, costs, raids, Hostile/Raid heat tiers, backend calls, offline research progression, offline heat processing, seasons, leaderboards, monetization, new monster families, or tutorial framework were added.
- No save schema changes were made.
- No Unity scenes, prefabs, project settings, TextMesh Pro assets, generated test scenes, or unrelated assets were modified.

## Changed files

- `Assets/_Project/Scripts/Services/MvpRunResultFeedbackPresenter.cs`
- `Assets/_Project/Scripts/Services/MvpRunResultFeedbackPresenter.cs.meta`
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
- `Assets/_Project/Tests/EditMode/MvpRunResultFeedbackPresenterTests.cs`
- `Assets/_Project/Tests/EditMode/MvpRunResultFeedbackPresenterTests.cs.meta`
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
- `docs/testing/evidence/vs/vs14-player-facing-run-result-feedback.md`

## Tests added or updated

- Added `MvpRunResultFeedbackPresenterTests` covering:
  - successful run with stable heat
  - successful run with lower heat
  - successful run with higher heat
  - failed run fallback and raw ID exclusion
  - unavailable run fallback and raw ID exclusion
  - localization key usage
- Updated `BootstrapOverlayPagingTests` covering:
  - run feedback appears after **Run or observe dungeon**
  - placement feedback remains visible after run feedback appears
  - run feedback updates after each run
  - F2 Run Diagnostics Focus hides run feedback and restores player-facing default safely
  - new run feedback excludes raw run/rule/structure IDs while diagnostics may still expose raw IDs where useful


## PR88 focused test fix note

- Initial local Unity validation reported one failure in `BootstrapOverlayPagingTests.DefaultPlayerFacingMode_PlayerPlacementAndRunActionsRemainAvailable` because the test assumed run feedback always began with the localized success message.
- The fixture starts from heat/crisis state that can validly produce failed run feedback; the runtime behavior was correct because it reported the actual run result.
- The test was updated to assert valid localized run feedback for either success or failure while still requiring non-empty feedback, player-facing overlay visibility, default player-facing mode, the run-simulated banner, and exclusion of raw run IDs, raw structure IDs, and raw rule IDs.

## Manual smoke checklist

1. Launch Bootstrap.
2. Confirm default screen is player-facing mode.
3. Confirm MVP Loop Summary is visible.
4. Confirm Guided MVP Action is visible.
5. Confirm First-session completion/status line is visible.
6. Confirm Minimal MVP Actions is visible.
7. Confirm all structure selection buttons, preview text, and action buttons are fully visible.
8. Select a structure and click Place or modify selected.
9. Confirm placement feedback appears.
10. Click Run or observe dungeon.
11. Confirm run result feedback appears.
12. Confirm latest run, mana, loot, heat, research, next action, guided action, first-session status, placement feedback, and run feedback remain coherent.
13. Confirm no raw structure IDs or raw rule IDs appear in the new run feedback.
14. Click Show diagnostics.
15. Confirm diagnostics page 1/9 appears.
16. Confirm diagnostics can still show raw IDs where useful.
17. Confirm F3 cycles all diagnostics pages and wraps.
18. Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
19. Confirm F2 again restores player-facing default.
20. Confirm F1 opens and closes Dev Panel.
21. Confirm no Console errors.
22. Confirm no unexpected generated files.

## Explicit scope confirmation

Confirmed: this change did **not** add rewards, unlocks, costs, backend calls, offline research, offline heat, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, new structure definitions, new simulation behavior, formulas, or a tutorial framework.
