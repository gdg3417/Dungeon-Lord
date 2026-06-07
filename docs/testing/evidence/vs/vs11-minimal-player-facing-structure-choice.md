# VS11 Minimal Player-Facing Structure Choice Evidence

## Scope
- Add a compact player-facing structure selector to the existing Bootstrap Minimal MVP Actions overlay.
- Keep Mana Generator as the default selected structure.
- Allow the tester to select only the existing MVP-safe structure options:
  - Mana Generator
  - Heat Scrubber
  - Risk Lab
- Route the existing player-facing place-or-modify action through the already-existing placement path using the selected structure ID.
- Keep MVP Loop Summary, Guided MVP Action, and First-session status presentation coherent after selecting, placing, and running.
- Keep player-facing labels and banners localization-owned.

## Non-goals
- No gameplay systems.
- No new structure definitions.
- No new simulation behavior.
- No rewards, unlocks, costs, multi-slot research, offline research progression, offline heat processing, backend calls, raids, Hostile/Raid heat tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, or tutorial framework.
- No Unity scene, prefab, project settings, TextMesh Pro asset, generated test scene, or unrelated asset changes.

## Changed files
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
  - Added in-memory selected MVP structure state defaulting to Mana Generator.
  - Added compact selector controls for Mana Generator, Heat Scrubber, and Risk Lab.
  - Updated player placement to place or modify the selected structure and show a localized selected-structure name in the banner.
  - Preserved diagnostics focus behavior so F2 hides player-facing structure choice controls.
- `Assets/_Project/Scripts/Services/MinimalMvpActionPanelPresenter.cs`
  - Added localized selected-structure label and option labels for the Minimal MVP Actions panel.
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
  - Added/updated English localization entries for selected structure labels and compact action panel formatting.
- `Assets/_Project/Tests/EditMode/MinimalMvpActionPanelPresenterTests.cs`
  - Updated presenter coverage for selected structure localization keys.
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
  - Added/updated overlay coverage for selection defaults, all three MVP-safe choices, selected placement, localized banners/summaries, diagnostics raw ID visibility, F2 restoration, and save-state immutability for view-only controls.
- `docs/testing/evidence/vs/vs11-minimal-player-facing-structure-choice.md`
  - Added this scope, non-goals, test, and smoke evidence checklist.

## Tests added or updated
- EditMode coverage added/updated for:
  - Selected structure defaults to Mana Generator.
  - Selecting Mana Generator, Heat Scrubber, and Risk Lab.
  - Placement uses the selected structure ID through the existing MVP place-or-modify path.
  - Placement banner uses the localized selected structure name.
  - MVP Loop Summary placement uses localized labels after each selection.
  - Raw structure IDs do not appear in player-facing panel text or banners after selected placement.
  - Diagnostics still preserve raw structure IDs where useful.
  - F2 Run Diagnostics Focus hides player-facing structure choice controls.
  - F2 again restores player-facing default with selected structure state preserved.
  - Show diagnostics still opens diagnostics page 1/9.
  - F3 cycles all diagnostics pages and wraps.
  - F1 Dev Panel still opens and closes.
  - Selecting structure plus diagnostics toggle, F2, F3, PageUp/PageDown-style scroll, and mouse-wheel-equivalent scroll calls do not mutate save state unless placement or run is explicitly invoked.
  - Placement and run actions still work in default player-facing mode.

## Manual smoke checklist
1. Launch Bootstrap.
2. Confirm default screen is player-facing mode.
3. Confirm MVP Loop Summary is visible.
4. Confirm Guided MVP Action is visible.
5. Confirm First-session completion/status line is visible.
6. Confirm Minimal MVP Actions is visible.
7. Confirm selected structure defaults to Mana Generator.
8. Select Mana Generator and click place or modify.
9. Confirm placement and banner show Mana Generator.
10. Select Heat Scrubber and click place or modify.
11. Confirm placement and banner show Heat Scrubber.
12. Select Risk Lab and click place or modify.
13. Confirm placement and banner show Risk Lab.
14. Click run or observe dungeon.
15. Confirm latest run, mana, loot, heat, research, next action, guided action, and first-session status remain coherent.
16. Click Show diagnostics.
17. Confirm diagnostics page 1/9 appears.
18. Confirm diagnostics can still show raw IDs where useful.
19. Confirm F3 cycles all diagnostics pages and wraps.
20. Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
21. Confirm F2 again restores player-facing default and selected structure state.
22. Confirm F1 opens and closes Dev Panel.
23. Confirm no Console errors.
24. Confirm no unexpected generated files.

## Explicit scope confirmation
VS11 did not add rewards, unlocks, costs, backend calls, offline research, offline heat, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, new structure definitions, or a tutorial framework.
