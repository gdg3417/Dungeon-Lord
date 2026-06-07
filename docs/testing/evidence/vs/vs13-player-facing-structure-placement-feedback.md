# VS13 Player-facing structure placement feedback evidence

## Scope
- Added a compact, read-only player-facing feedback summary that appears after **Place or modify selected**.
- The feedback summarizes the prior selected-slot label, the new selected-slot label, and the localized role/impact line for the selected MVP structure.
- The feedback is held in overlay memory only and is rebuilt from existing placement state plus the existing placement result.

## Non-goals
- No gameplay systems were added.
- No structure definitions were added.
- No simulation behavior, formulas, tuning, costs, rewards, unlocks, backend calls, raids, offline systems, multi-slot research, or tutorial framework were added.
- No save schema changes were made; feedback resets with the overlay/scene lifecycle.
- No Unity scenes, prefabs, project settings, TextMesh Pro assets, generated test scenes, or unrelated assets were modified.

## Changed files
- `Assets/_Project/Scripts/Services/MvpStructurePlacementFeedbackPresenter.cs`
  - New read-only presenter for compact placement feedback text.
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
  - Captures prior and new selected-slot structure IDs around the existing MVP placement path and displays the in-memory feedback line in player-facing mode.
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
  - Adds English localization entries for the empty-slot fallback and changed-summary format.
- `Assets/_Project/Tests/EditMode/MvpStructurePlacementFeedbackPresenterTests.cs`
  - Adds presenter tests for empty-slot placement, replacement feedback, localized labels, role text, raw-ID safety, and fallback behavior.
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
  - Adds overlay tests for feedback display, updates after each placement, run banner coexistence, diagnostics focus hiding/restoring, and safe prior fallback behavior.

## Tests added or updated
- `MvpStructurePlacementFeedbackPresenterTests.EmptySlotToManaGenerator_UsesLocalizedLabelsAndPreviewRole`
- `MvpStructurePlacementFeedbackPresenterTests.ManaGeneratorToHeatScrubber_UsesLocalizedLabelsAndPreviewRole`
- `MvpStructurePlacementFeedbackPresenterTests.HeatScrubberToRiskLab_UsesLocalizedLabelsAndPreviewRole`
- `MvpStructurePlacementFeedbackPresenterTests.Feedback_DoesNotExposeRawStructureIds`
- `MvpStructurePlacementFeedbackPresenterTests.EmptyOrUnknownPriorPlacement_UsesSafeLocalizedEmptySlotFallback`
- `MvpStructurePlacementFeedbackPresenterTests.NullLocalizer_FallsBackToStableLocalizationKeys`
- `BootstrapOverlayPagingTests.PlacementFeedback_EmptySlotToManaGenerator_AppearsAfterPlaceOrModifySelected`
- `BootstrapOverlayPagingTests.PlacementFeedback_UpdatesAfterEachSelectedStructurePlacement`
- `BootstrapOverlayPagingTests.PlacementFeedback_UnknownPriorPlacementUsesSafeFallback`
- `BootstrapOverlayPagingTests.RunOrObserveDungeon_DoesNotOverwritePlacementFeedback`
- `BootstrapOverlayPagingTests.RunDiagnosticsFocus_HidesPlacementFeedbackAndRestoresSafely`

## Manual smoke checklist
1. Launch Bootstrap.
2. Confirm default screen is player-facing mode.
3. Confirm MVP Loop Summary is visible.
4. Confirm Guided MVP Action is visible.
5. Confirm First-session completion/status line is visible.
6. Confirm Minimal MVP Actions is visible.
7. Confirm all structure selection buttons and preview text are fully visible.
8. Select Mana Generator and click Place or modify selected.
9. Confirm placement feedback shows Empty slot or prior placement to Mana Generator.
10. Select Heat Scrubber and click Place or modify selected.
11. Confirm placement feedback shows Mana Generator to Heat Scrubber.
12. Select Risk Lab and click Place or modify selected.
13. Confirm placement feedback shows Heat Scrubber to Risk Lab.
14. Confirm no raw structure IDs appear in player-facing feedback.
15. Click Run or observe dungeon.
16. Confirm latest run, mana, loot, heat, research, next action, guided action, first-session status, and feedback remain coherent.
17. Click Show diagnostics.
18. Confirm diagnostics page 1/9 appears.
19. Confirm diagnostics can still show raw IDs where useful.
20. Confirm F3 cycles all diagnostics pages and wraps.
21. Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
22. Confirm F2 again restores player-facing default.
23. Confirm F1 opens and closes Dev Panel.
24. Confirm no Console errors.
25. Confirm no unexpected generated files.

## Explicit scope confirmation
Confirmed: VS13 adds no rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, new structure definitions, new simulation behavior, formulas, or tutorial framework.
