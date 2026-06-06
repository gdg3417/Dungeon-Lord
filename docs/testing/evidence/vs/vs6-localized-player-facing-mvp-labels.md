# VS6: Localized player-facing MVP labels evidence

## 1. Scope

VS6 replaces raw internal IDs on player-facing MVP surfaces with localization-owned display labels where the MVP vertical slice can safely resolve the ID:

- MVP Loop Summary placement labels.
- Minimal MVP Action placement banners.
- Shared MVP player-facing label resolution for the current vertical-slice structure IDs:
  - `structure.mana_generator.basic`
  - `structure.heat_scrubber.basic`
  - `structure.risk_lab.basic`
- Safe player-facing fallback labels for unknown structure IDs and unknown research status labels.

Developer diagnostics remain developer-facing and may continue to show stable IDs for auditability.

## 2. Non-goals

This change is readability cleanup only. It does not add gameplay, progression, tuning, or onboarding systems. Specifically, it does not add:

- Gameplay systems.
- Rewards.
- Unlocks.
- Costs.
- Multi-slot research.
- Offline research progression.
- Offline heat processing.
- Backend calls.
- Raids.
- Hostile or Raid heat tiers.
- Seasons, leaderboards, monetization, or new monster families.
- A broad tutorial framework.

## 3. Changed files

- `Assets/_Project/Scripts/Services/MvpPlayerFacingLabelResolver.cs` — new MVP player-facing label resolver that maps known stable IDs to localization keys and supplies safe fallback keys.
- `Assets/_Project/Scripts/Services/MvpLoopSummaryPanelPresenter.cs` — resolves placement and unknown research labels through the shared MVP player-facing label resolver.
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs` — formats Minimal MVP Action placement banners with a localized structure display label while preserving developer placement banners as ID-based diagnostics.
- `Assets/_Project/Data/Bootstrap/string_table_en.json` — adds English string table entries for MVP structure display names and the safe unknown-structure fallback.
- `Assets/_Project/Tests/EditMode/MvpPlayerFacingLabelResolverTests.cs` — adds EditMode coverage for known structure mappings, unknown structure fallback, and unknown research fallback.
- `Assets/_Project/Tests/EditMode/MvpLoopSummaryPanelPresenterTests.cs` — updates and expands MVP Loop Summary panel tests to assert localized placement labels and safe research fallback behavior.
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs` — updates overlay coverage for localized MVP summary labels, localized Minimal MVP Action placement banner text, diagnostics ID preservation, F2 focus, F3 paging, and view-only non-mutation.
- `docs/testing/evidence/vs/vs6-localized-player-facing-mvp-labels.md` — this evidence artifact.

## 4. Tests added or updated

Added or updated EditMode coverage includes:

- Known MVP structure IDs resolve to localization keys instead of raw IDs.
- Unknown structure IDs resolve to a safe localized fallback instead of exposing raw IDs.
- Unknown research status in the MVP Loop Summary resolves to a safe localized fallback instead of exposing raw project IDs.
- MVP Loop Summary placement text uses `Mana Generator` / mapped display labels instead of raw structure IDs.
- Minimal MVP Action placement banner uses the localized structure name and does not expose `structure.mana_generator.basic`.
- Systems diagnostics continue to preserve the raw selected structure ID for developer-facing detail.
- F2 Run Diagnostics Focus still hides player-facing panels.
- F3 still cycles through all diagnostics pages and wraps.
- Refresh, F2, F3, PageUp/PageDown-equivalent scroll calls do not mutate save state.

## 5. Manual smoke checklist

Use the first-session Bootstrap smoke path:

1. [ ] Launch Bootstrap.
2. [ ] Confirm MVP Loop Summary is visible.
3. [ ] Confirm Placement shows a readable structure name, not a raw structure ID.
4. [ ] Click Minimal MVP Action placement button.
5. [ ] Confirm placement updates and the banner does not show a raw structure ID.
6. [ ] Click Minimal MVP Action run button.
7. [ ] Confirm latest run, mana, loot, heat, research, next action, and guided action remain readable.
8. [ ] Confirm diagnostics still show detailed developer information.
9. [ ] Confirm F1 opens and closes Dev Panel.
10. [ ] Confirm F2 hides player-facing panels and shows Run Diagnostics Focus.
11. [ ] Confirm F2 again restores player-facing panels.
12. [ ] Confirm F3 cycles all diagnostics pages and wraps.
13. [ ] Confirm no Console errors.
14. [ ] Confirm no unexpected generated files.

## 6. Explicit scope confirmation

Confirmed: VS6 did not add rewards, unlocks, costs, backend calls, offline research, offline heat, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, or a broad tutorial framework.
