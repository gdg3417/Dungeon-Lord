# VS5 Minimal Player-Facing Action Panel Evidence

## Scope

- Adds a compact player-facing Minimal MVP Action GUI panel in the upper-right Game view area, separate from the MVP Loop Summary, Guided MVP Action, and diagnostics text.
- Exposes only two MVP vertical-slice actions:
  1. place or modify one selected-slot MVP-safe structure through the existing placement path;
  2. run or observe one dungeon run through the existing run simulation path.
- Splits long research status and verification diagnostics into four focused pages so pages no longer rely on scrolling for visually wrapped long lines.
- Keeps the F1 Dev Panel available for developer controls.
- Hides the MVP Loop Summary, Guided MVP Action, and Minimal MVP Action panel while F2 Run Diagnostics Focus is active.

## Manual findings addressed

- Before this fix, the floating Minimal MVP Action GUI panel overlapped the MVP Loop Summary text during manual smoke testing.
- Before this fix, diagnostics pages 6/7 and 7/7 contained long visually wrapped research diagnostics lines, but scrolling was based only on newline-delimited logical lines, so bottom text could be cut off while scrolling stayed disabled.

## Fix summary

- Moved the floating Minimal MVP Action GUI panel to an upper-right fixed rect and removed the duplicate compact action line from the overlay text stack, leaving one clear player-facing action surface.
- Increased diagnostics from 7 to 9 pages by splitting the previous Research Status and Research Verification pages into:
  1. Research Status Presentation Diagnostics;
  2. Research Status Safety Diagnostics;
  3. Research Verification Boundary Diagnostics;
  4. Research Verification Safety Diagnostics.
- Preserved all existing diagnostics content, F1 Dev Panel behavior, F2 Run Diagnostics Focus behavior, and F3 page cycling/wraparound.

## Non-goals

- No rewards.
- No unlocks.
- No costs.
- No multi-slot research.
- No offline research progression.
- No offline heat processing.
- No backend calls.
- No raids.
- No Hostile or Raid heat tiers.
- No seasons, leaderboards, monetization, or new monster families.
- No broad tutorial framework.
- No new simulation behavior.
- No Unity scene, prefab, or project settings changes.

## Changed files

- `Assets/_Project/Scripts/Services/MinimalMvpActionPanelPresenter.cs`
- `Assets/_Project/Scripts/Services/MinimalMvpActionPanelPresenter.cs.meta`
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
- `Assets/_Project/Scripts/Core/GameRoot.cs`
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
- `Assets/_Project/Tests/EditMode/MinimalMvpActionPanelPresenterTests.cs`
- `Assets/_Project/Tests/EditMode/MinimalMvpActionPanelPresenterTests.cs.meta`
- `Assets/_Project/Tests/EditMode/MinimalMvpActionGameRootTests.cs`
- `Assets/_Project/Tests/EditMode/MinimalMvpActionGameRootTests.cs.meta`
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
- `docs/testing/evidence/vs/vs5-minimal-player-facing-action-panel.md`

## Tests added or updated

- Added `MinimalMvpActionPanelPresenterTests` to verify the panel presenter uses localization keys for its title and button labels and falls back to stable keys when no localizer is provided.
- Added `MinimalMvpActionGameRootTests` to verify the player-facing placement action reuses the placement path and can modify the selected slot without changing the existing Dev Panel placement behavior.
- Updated `BootstrapOverlayPagingTests` to verify:
  - the Minimal MVP Action panel appears with the MVP Loop Summary and Guided MVP Action;
  - F2 Run Diagnostics Focus hides all player-facing panels;
  - F2 again restores player-facing panels;
  - F3 still cycles pages 1/9 through 9/9 and wraps back to 1/9;
  - the F1 Dev Panel visibility toggle state remains preserved;
  - view-only refresh, F2 focus toggling, F3 paging, PageUp/PageDown-equivalent scroll calls, and mouse-wheel-equivalent scroll calls do not mutate save state.

## Manual smoke checklist

1. Launch Bootstrap.
2. Confirm MVP Loop Summary visible.
3. Confirm Guided MVP Action visible.
4. Confirm Minimal MVP Action panel visible.
5. Click the player-facing placement or modification action.
6. Confirm placement changes in MVP Loop Summary.
7. Click the player-facing run or observe action.
8. Confirm latest run, mana, loot, heat, research, and next action update.
9. Confirm F1 still opens and closes Dev Panel.
10. Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
11. Confirm F2 again restores player-facing panels.
12. Confirm F3 cycles all 9 diagnostics pages.
13. Confirm no Console errors.
14. Confirm no unexpected generated files.

## Explicit MVP scope confirmation

Confirmed: VS5 adds no rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid heat tiers, seasons, leaderboards, monetization, new monster families, or broad tutorial framework. The change reuses existing placement, run simulation, save-safe, localization, MVP Loop Summary, and Guided MVP Action patterns and does not introduce new simulation behavior.
