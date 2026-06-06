# VS9: Default Bootstrap to player-facing first-session view

## Scope

- Default the Bootstrap overlay to the player-facing first-session view.
- Keep the MVP Loop Summary, Guided MVP Action, Minimal MVP Actions, and player-facing banner/status visible by default.
- Keep diagnostics hidden by default while preserving the existing on-screen diagnostics toggle.
- Preserve F1 Dev Panel, F2 Run Diagnostics Focus, F3 diagnostics paging, PageUp/PageDown scrolling, and mouse-wheel-equivalent diagnostic scroll behavior.

## Non-goals

- No gameplay systems were added.
- No simulation behavior changes were made.
- No scene, prefab, project setting, TextMesh Pro asset, generated test scene, or unrelated asset changes were made.
- No tutorial framework was added.
- No save schema, tuning, content economy, backend, raid, live-ops, or monetization behavior was changed.

## Changed files

- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
  - Bootstrap diagnostics visibility now starts hidden so the first-session player-facing view is the default.
  - Showing diagnostics through the existing toggle resets the full diagnostics page to Runtime Summary page 1/9.
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
  - Updated Bootstrap overlay EditMode coverage for player-facing defaults, diagnostics toggling, F2 focus restore, F3 paging, no-mutation view-only controls, F1 Dev Panel state, localized player-facing labels, and default action availability.
- `Assets/_Project/Tests/EditMode/CurrentHeatTierDiagnosticsTests.cs`, `Assets/_Project/Tests/EditMode/OfflineSummaryDiagnosticsTests.cs`, `Assets/_Project/Tests/EditMode/ResearchCompletionEligibilityDiagnosticsTests.cs`, `Assets/_Project/Tests/EditMode/ResearchProgressDiagnosticsTests.cs`, and `Assets/_Project/Tests/EditMode/RunSimulationTests.cs`
  - Updated older diagnostics tests to explicitly opt into diagnostics mode before asserting diagnostics content.
- `docs/testing/evidence/vs/vs9-default-player-facing-first-session-view.md`
  - Added this evidence note and manual smoke checklist.

## Tests added or updated

- Updated default visibility coverage so `DiagnosticsVisible` is false by default and player-facing panels/status/banner are visible by default.
- Updated diagnostics toggle coverage so toggling diagnostics on shows Runtime Summary page 1/9 with diagnostics header/body/hints, and toggling diagnostics off hides diagnostics again.
- Added F3 hidden-diagnostics coverage confirming it changes only hidden page state and does not visibly change player-facing text.
- Updated F2 Run Diagnostics Focus coverage so F2 from the player-facing default hides player-facing panels and F2 again restores player-facing default mode.
- Updated F3 visible-diagnostics coverage for all 9 pages and wraparound.
- Kept F1 Dev Panel toggle coverage.
- Kept and updated view-only no-mutation coverage for refresh, diagnostics visibility toggle, F2 focus, F3 paging, PageUp/PageDown-sized scrolling, and mouse-wheel-sized scrolling.
- Added default player-facing action availability coverage for placement and run actions.
- Kept player-facing label coverage through localization-key-backed presenter labels.

## Follow-up diagnostic test compatibility fix

- The initial local Unity EditMode run for PR83 failed because older diagnostics tests assumed Bootstrap diagnostics were visible immediately after `RefreshOverlayText()`.
- Those tests now explicitly opt into diagnostics mode before asserting diagnostics headers, bodies, line ordering, or page-specific content.
- The runtime default remains player-facing: Bootstrap diagnostics are still hidden by default, and the existing Show diagnostics path remains the opt-in route to diagnostics.

## Manual smoke checklist

1. [ ] Launch Bootstrap.
2. [ ] Confirm default screen is player-facing mode.
3. [ ] Confirm diagnostics header/body/F1-F2-F3 hints are hidden by default.
4. [ ] Confirm MVP Loop Summary is visible.
5. [ ] Confirm Guided MVP Action is visible.
6. [ ] Confirm Minimal MVP Actions is visible.
7. [ ] Confirm the visible diagnostics toggle says Show diagnostics.
8. [ ] Click placement action and confirm summary updates.
9. [ ] Click run action and confirm summary updates.
10. [ ] Click Show diagnostics.
11. [ ] Confirm diagnostics page 1/9 appears.
12. [ ] Press F3 through all diagnostics pages and confirm wraparound.
13. [ ] Press F2 and confirm Run Diagnostics Focus appears.
14. [ ] Press F2 again and confirm player-facing default returns.
15. [ ] Press F1 and confirm Dev Panel opens and closes.
16. [ ] Confirm no Console errors.
17. [ ] Confirm no unexpected generated files.

## Explicit scope confirmation

This change did **not** add rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile heat tiers, Raid heat tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, or a tutorial framework.
