# VS12: Player-facing structure impact preview

## Scope

- Added a compact, read-only impact preview for the currently selected MVP-safe structure in the Minimal MVP Actions panel.
- Preview resolution is driven by stable structure IDs and localization keys.
- Covered Mana Generator, Heat Scrubber, and Risk Lab with player-facing localized English string table entries.
- Preserved diagnostics behavior, including F2 Run Diagnostics Focus hiding player-facing panels and raw IDs remaining available in developer diagnostics where useful.

## Non-goals

- No gameplay systems were added.
- No simulation formulas or runtime behavior were added or changed.
- No new structure definitions were added.
- No save-state inspection or mutation is required to resolve previews.
- No tutorial framework or multi-step onboarding system was added.

## Changed files

- `Assets/_Project/Scripts/Services/MvpStructureImpactPreviewPresenter.cs`
- `Assets/_Project/Scripts/Services/MvpStructureImpactPreviewPresenter.cs.meta`
- `Assets/_Project/Scripts/Services/MinimalMvpActionPanelPresenter.cs`
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
- `Assets/_Project/Tests/EditMode/MvpStructureImpactPreviewPresenterTests.cs`
- `Assets/_Project/Tests/EditMode/MvpStructureImpactPreviewPresenterTests.cs.meta`
- `Assets/_Project/Tests/EditMode/MinimalMvpActionPanelPresenterTests.cs`
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
- `docs/testing/evidence/vs/vs12-player-facing-structure-impact-preview.md`

## Tests added or updated

- Added EditMode presenter coverage for Mana Generator, Heat Scrubber, Risk Lab, unknown fallback, localization-key fallback, and raw structure ID suppression in preview text.
- Updated Minimal MVP Actions presenter coverage to include preview labels and localization-key requests.
- Updated Bootstrap overlay coverage for the preview appearing with the selected structure, changing when the selected structure changes, preserving placement/run actions, hiding under F2 Run Diagnostics Focus, restoring after F2, keeping diagnostics paging behavior, preserving F1 Dev Panel toggling, and avoiding save mutation during view-only refresh/toggle/page/scroll calls.

## Manual smoke checklist

1. [ ] Launch Bootstrap.
2. [ ] Confirm default screen is player-facing mode.
3. [ ] Confirm MVP Loop Summary is visible.
4. [ ] Confirm Guided MVP Action is visible.
5. [ ] Confirm First-session completion/status line is visible.
6. [ ] Confirm Minimal MVP Actions is visible.
7. [ ] Confirm selected structure defaults to Mana Generator.
8. [ ] Confirm Mana Generator preview is visible and readable.
9. [ ] Select Heat Scrubber and confirm preview changes.
10. [ ] Select Risk Lab and confirm preview changes.
11. [ ] Confirm all selection buttons are fully visible.
12. [ ] Confirm the panel does not overlap key text.
13. [ ] Place each structure and confirm placement plus banner use localized labels.
14. [ ] Click run or observe dungeon.
15. [ ] Confirm latest run, mana, loot, heat, research, next action, guided action, and first-session status remain coherent.
16. [ ] Click Show diagnostics.
17. [ ] Confirm diagnostics page 1/9 appears.
18. [ ] Confirm diagnostics can still show raw IDs where useful.
19. [ ] Confirm F3 cycles all diagnostics pages and wraps.
20. [ ] Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
21. [ ] Confirm F2 again restores player-facing default, selected structure, and preview.
22. [ ] Confirm F1 opens and closes Dev Panel.
23. [ ] Confirm no Console errors.
24. [ ] Confirm no unexpected generated files.

## Explicit scope confirmation

This VS12 change did **not** add rewards, unlocks, costs, backend calls, offline research, offline heat, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, new structure definitions, new simulation behavior, or a tutorial framework.
