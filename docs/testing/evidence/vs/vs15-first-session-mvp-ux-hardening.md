# VS15 First-Session MVP UX Hardening Evidence

## Scope

VS15 hardens the existing first-session MVP loop after PR75 through PR88. The loop remains focused on the current player-facing Bootstrap vertical slice:

1. default player-facing Bootstrap startup view;
2. MVP Loop Summary visibility;
3. Guided MVP Action visibility;
4. first-session completion/status visibility;
5. Minimal MVP Actions visibility;
6. localized structure selection for Mana Generator, Heat Scrubber, and Risk Lab;
7. compact placement preview and placement feedback;
8. compact run result feedback;
9. Show diagnostics, F1 Dev Panel, F2 Run Diagnostics Focus, and F3 diagnostics paging behavior; and
10. raw-ID boundaries between player-facing UX and developer diagnostics.

This is a hardening and smoke-coverage consolidation PR. It does not add gameplay.

## Non-goals

VS15 does not add a tutorial framework, new player-facing panels, new structure definitions, new structure options, new actions, new simulation systems, new formulas, new economy behavior, new progression, new rewards, new unlocks, new costs, multi-slot research, offline research progression, offline heat processing, backend calls, raids, Hostile or Raid heat tiers, seasons, leaderboards, monetization, or new monster families.

VS15 also does not change the save schema, Unity scenes, prefabs, project settings, TextMesh Pro assets, generated test scenes, unrelated assets, tuning values, formulas, or balance data.

## Changed files

- `Assets/_Project/Scripts/Services/MvpLoopSummaryPanelPresenter.cs`
  - Kept the player-facing latest-run line status-only so raw run IDs remain out of the MVP Loop Summary while diagnostics can still carry raw IDs.
- `Assets/_Project/Tests/EditMode/MvpLoopSummaryPanelPresenterTests.cs`
  - Updated presenter regression coverage to assert latest-run status stays localized and no raw run ID appears in the player-facing summary.
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
  - Added an end-to-end VS15 first-session UX regression that exercises default player-facing mode, the default Mana Generator selection, all three MVP-safe structure choices, placement feedback, run feedback, F2 hiding/restoration, Show diagnostics page 1/9, F3 all-page wraparound, F1 Dev Panel open/close behavior, player-facing raw-ID exclusion, and diagnostics raw-ID preservation.
  - Added a compact raw-ID assertion helper scoped to player-facing feedback/overlay checks.
- `docs/testing/evidence/vs/vs15-first-session-mvp-ux-hardening.md`
  - Added this consolidated VS15 evidence and manual smoke checklist.

## Tests added or updated

Updated `MvpLoopSummaryPanelPresenterTests` to verify raw run IDs stay out of the player-facing latest-run line.

Updated `BootstrapOverlayPagingTests` with `FirstSessionMvpUxFlow_HardensSelectionPlacementRunDiagnosticsAndRawIdBoundaries`, covering:

- default player-facing mode;
- selected structure defaults to Mana Generator;
- Mana Generator selection, preview, placement, and feedback;
- Heat Scrubber selection, preview, placement, and feedback;
- Risk Lab selection, preview, placement, and feedback;
- run action creates run result feedback;
- placement feedback and run feedback coexist;
- player-facing feedback and overlay text exclude raw structure IDs, run IDs, and run rule IDs;
- F2 Run Diagnostics Focus hides player-facing panels, selection controls, placement feedback, and run feedback;
- F2 again restores the player-facing default safely;
- Show diagnostics returns diagnostics page 1/9;
- F3 cycles all 9 diagnostics pages and wraps;
- F1 Dev Panel still opens and closes; and
- diagnostics still preserve raw structure IDs where useful.

Existing coverage continues to verify that view-only operations do not mutate save state, including `RefreshOverlayText`, diagnostics visibility toggle, F2 focus toggle, F3 page cycling, PageUp/PageDown-style scroll calls, mouse-wheel-equivalent scroll calls, and structure selection without placement.

## Full manual smoke checklist

1. [ ] Launch Bootstrap.
2. [ ] Confirm default screen is player-facing mode.
3. [ ] Confirm diagnostics header/body/F1-F2-F3 hints are hidden by default.
4. [ ] Confirm MVP Loop Summary is visible.
5. [ ] Confirm Guided MVP Action is visible.
6. [ ] Confirm First-session completion/status line is visible.
7. [ ] Confirm Minimal MVP Actions is visible.
8. [ ] Confirm selected structure defaults to Mana Generator.
9. [ ] Confirm all structure selection buttons are fully visible:
   - Mana Generator
   - Heat Scrubber
   - Risk Lab
10. [ ] Confirm preview text is visible and compact.
11. [ ] Confirm Place or modify selected is fully visible.
12. [ ] Confirm Run or observe dungeon is fully visible.
13. [ ] Confirm Show diagnostics is fully visible.
14. [ ] Select Mana Generator.
15. [ ] Confirm preview shows Mana Generator role.
16. [ ] Click Place or modify selected.
17. [ ] Confirm placement feedback appears and uses Mana Generator.
18. [ ] Select Heat Scrubber.
19. [ ] Confirm preview updates to Heat Scrubber role.
20. [ ] Click Place or modify selected.
21. [ ] Confirm placement feedback shows Mana Generator to Heat Scrubber or prior placement to Heat Scrubber.
22. [ ] Select Risk Lab.
23. [ ] Confirm preview updates to Risk Lab role.
24. [ ] Click Place or modify selected.
25. [ ] Confirm placement feedback shows Heat Scrubber to Risk Lab or prior placement to Risk Lab.
26. [ ] Confirm no raw structure IDs appear in player-facing summary, preview, placement feedback, or banner.
27. [ ] Click Run or observe dungeon.
28. [ ] Confirm run result feedback appears.
29. [ ] Confirm run result feedback does not show raw run IDs, raw structure IDs, or raw rule IDs.
30. [ ] Confirm latest run, mana, loot, heat, research, next action, guided action, first-session status, placement feedback, and run feedback remain coherent.
31. [ ] Confirm no player-facing text is clipped or overflowing at the smoke-test Game view size.
32. [ ] Click Show diagnostics.
33. [ ] Confirm diagnostics page 1/9 appears.
34. [ ] Confirm diagnostics remain readable.
35. [ ] Confirm diagnostics may show raw IDs where useful.
36. [ ] Press F3 through all 9 diagnostics pages and confirm wraparound.
37. [ ] Press F2 and confirm Run Diagnostics Focus appears.
38. [ ] Confirm F2 hides player-facing panels, selection controls, preview, placement feedback, and run feedback.
39. [ ] Press F2 again and confirm player-facing default returns.
40. [ ] Press F1 and confirm Dev Panel opens and closes.
41. [ ] Confirm no Console errors.
42. [ ] Exit Play Mode.
43. [ ] Confirm GitHub Desktop shows no unexpected generated files.
44. [ ] Discard any generated Unity files if they appear.

## Known limitations

VS15 does not add deeper gameplay, economy, progression, research completion, new content, or balancing. It only hardens the current first-session MVP UX and smoke coverage.

Manual Game view overflow and clipping validation still requires running the Bootstrap scene in Unity at the target smoke-test Game view size. The automated EditMode regression checks compact panel dimensions and player-facing text boundaries, but it does not replace the manual visual smoke pass.

## Explicit MVP scope confirmation

Confirmed: VS15 added no rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, new structure definitions, new simulation behavior, formulas, or tutorial framework.

Confirmed: VS15 preserves raw IDs in developer diagnostics where useful and keeps player-facing text localization-owned by reusing existing localization keys and test-scoped fake localization strings only in EditMode fixtures.
