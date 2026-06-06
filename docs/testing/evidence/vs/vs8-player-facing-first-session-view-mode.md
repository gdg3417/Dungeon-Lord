# VS8: Player-facing first-session view mode

## Scope

- Added a narrow diagnostics visibility mode to `BootstrapOverlay` for first-session readability.
- Player-facing mode keeps the MVP Loop Summary, Guided MVP Action, Minimal MVP Actions, and banner/status feedback visible.
- Diagnostics-visible mode keeps the existing nine diagnostics pages, F1 Dev Panel toggle, F2 Run Diagnostics Focus, F3 page cycling, and diagnostics scrolling available.
- Added localized labels/status strings for diagnostics visibility controls and view-mode status.

## Non-goals

- No gameplay systems were added.
- No tutorial framework was added.
- No Unity scenes, prefabs, project settings, TextMesh Pro assets, generated test scenes, or unrelated assets were changed.
- No simulation behavior, save schema, or tuning values were changed.

## Changed files

- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
  - Added diagnostics visibility state, a toggle entry point, player-facing status rendering, and a localized on-screen Minimal MVP Actions button to show/hide diagnostics.
- `Assets/_Project/Scripts/Services/MinimalMvpActionPanelPresenter.cs`
  - Added localized Minimal MVP Actions labels for showing and hiding diagnostics.
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
  - Added localization keys for diagnostics toggle labels and view-mode status feedback.
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
  - Added coverage for default diagnostics mode, player-facing diagnostics hiding, diagnostics restoration, F2 focus restoration, Minimal MVP Action label availability, and view-only save-state safety.
- `Assets/_Project/Tests/EditMode/MinimalMvpActionPanelPresenterTests.cs`
  - Added coverage for diagnostics toggle localization keys and stable-key fallback labels.

## Tests added or updated

- `DiagnosticsVisibility_DefaultMode_ShowsDiagnosticsAndPlayerFacingPanels`
- `ToggleDiagnosticsVisibility_PlayerFacingMode_HidesDiagnosticsHeaderBodyAndHintsButKeepsPlayerPanelsAndStatus`
- `ToggleDiagnosticsVisibility_RestoresDiagnosticsHeaderBodyAndHints`
- `RunDiagnosticsFocus_FromPlayerFacingMode_RestoresPriorPlayerFacingModeSafely`
- `PlayerFacingMode_MinimalMvpActionLabelsIncludePlacementRunAndDiagnosticsToggleKeys`
- `ViewOnlyRefreshFocusPagingAndScroll_DoNotMutateSaveState` updated to include diagnostics visibility toggling and hidden-mode scroll calls.
- `MinimalMvpActionPanelPresenterTests` updated to verify new diagnostics toggle localization keys and key fallbacks.

## Manual smoke checklist

1. Launch Bootstrap.
2. Confirm MVP Loop Summary is visible.
3. Confirm Guided MVP Action is visible.
4. Confirm Minimal MVP Actions is visible.
5. Toggle player-facing view mode.
6. Confirm diagnostics header, diagnostics body, and F1/F2/F3 hint lines are hidden.
7. Confirm placement button still works.
8. Confirm run button still works.
9. Confirm latest run, mana, loot, heat, research, next action, and guided action remain coherent.
10. Toggle diagnostics visible again.
11. Confirm diagnostics page 1/9 appears.
12. Confirm F3 cycles all diagnostics pages and wraps.
13. Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
14. Confirm F2 again restores normal view.
15. Confirm F1 opens and closes Dev Panel.
16. Confirm no Console errors.
17. Confirm no unexpected generated files.

## Explicit MVP scope confirmation

Confirmed: this VS8 change did not add rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, or a tutorial framework.
