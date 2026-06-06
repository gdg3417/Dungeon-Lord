# VS3: Guided MVP Action Path Evidence

## Scope

- Added a narrow, read-only guided MVP action path presenter that consumes save state and the existing MVP Loop Summary output.
- The presenter returns the current guided step ID, current guided step status localization key, next action localization key, completion state, and explicit safety flags.
- Added a small localized Guided MVP Action panel under the MVP Loop Summary panel so the next obvious action is visible near the mana, loot, heat, and research readout.
- The guided path reacts to existing state only:
  - no placement suggests placing or modifying one structure;
  - placement with no run suggests running or observing the dungeon;
  - heat pressure suggests reducing heat pressure;
  - poor loot extraction suggests improving survivability or layout;
  - research completion pending suggests verifying research status;
  - otherwise it marks the path complete and suggests repeat/improve.

## Non-goals

- No rewards were added.
- No unlocks were added.
- No costs were added.
- No multi-slot research was added.
- No offline research progression was added.
- No offline heat processing was added.
- No backend calls were added.
- No raids were added.
- No Hostile or Raid heat tiers were added.
- No seasons, leaderboards, monetization, or new monster families were added.
- No broad tutorial framework was added.
- No parallel simulation behavior was added.

## Changed files

- `Assets/_Project/Scripts/Services/GuidedMvpActionPathPresenter.cs`
- `Assets/_Project/Scripts/Services/GuidedMvpActionPathPanelPresenter.cs`
- `Assets/_Project/Scripts/Util/Models.cs`
- `Assets/_Project/Scripts/Core/GameRoot.cs`
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
- `Assets/_Project/Tests/EditMode/GuidedMvpActionPathPresenterTests.cs`
- `Assets/_Project/Tests/EditMode/GuidedMvpActionPathPanelPresenterTests.cs`
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`

## Tests added or updated

- Added EditMode coverage for deterministic guided step selection:
  - no placement state;
  - placement exists but no run state;
  - latest run exists with heat pressure;
  - latest run exists with poor loot extraction;
  - completion-pending research state;
  - complete/repeat-improve state;
  - no save and missing optional state;
  - no mutation of save, heat, mana, run history, research, or offline summary.
- Added panel presenter localization-key coverage for the Guided MVP Action panel.
- Updated Bootstrap overlay paging tests to verify the guided panel appears under the MVP Loop Summary and remains hidden during F2 Run Diagnostics focus.

## Manual smoke checklist

1. Start from a save with no placed structure.
2. Confirm the MVP Loop Summary appears and the Guided MVP Action panel suggests placing or modifying one structure.
3. Place one existing structure through the existing placement/dev action path.
4. Confirm the guided panel suggests running or observing the dungeon.
5. Run the dungeon through the existing run/dev action path.
6. Confirm mana, loot, heat, and research remain visible in the MVP Loop Summary.
7. Confirm the guided panel suggests exactly one next improvement step based on the summary state.
8. Press F2 and confirm Run Diagnostics focus hides the MVP Loop Summary and guided panel while preserving diagnostics readability/focus behavior.
9. Press F2 again and confirm the MVP Loop Summary and guided panel return.

## Explicit scope confirmation

This change is read-only guidance over existing save and MVP Loop Summary state. It does not add rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid heat tiers, seasons, leaderboards, monetization, new monster families, broad tutorial framework behavior, or parallel simulation behavior.
