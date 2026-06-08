# VS16 Player-facing MVP vertical slice closeout

## 1. Summary

The player-facing Bootstrap MVP vertical slice is now readable, clickable, and smoke-testable. It gives a first-session player a compact Bootstrap overlay that can be validated manually without relying on developer diagnostics as the primary surface.

The current slice supports selecting the MVP-safe structures Mana Generator, Heat Scrubber, and Risk Lab; placing or modifying the selected structure; observing dungeon runs; and viewing placement and run feedback from the player-facing overlay.

Developer diagnostics remain available for investigation, regression triage, and gameplay handoff evidence. VS16 does not add gameplay, UI features, tuning, rewards, unlocks, costs, formulas, backend calls, save schema changes, or new structure definitions.

## 2. Current player-facing loop

The current Bootstrap player-facing loop includes:

- Default player-facing mode on Bootstrap startup.
- MVP Loop Summary.
- Guided MVP Action.
- First-session completion/status summary.
- Minimal MVP Actions.
- Structure selector for Mana Generator, Heat Scrubber, and Risk Lab.
- Structure role preview for the selected MVP-safe structure.
- Placement feedback after placing or modifying the selected structure.
- Run result feedback after running or observing the dungeon.
- Show diagnostics button for intentionally entering the developer diagnostic view.

## 3. Controls

The Bootstrap overlay currently exposes these controls for the vertical slice and diagnostics:

- **Place or modify selected**: places or updates the currently selected MVP-safe structure.
- **Run or observe dungeon**: executes or observes the current dungeon run path and updates run feedback.
- **Show diagnostics / Hide diagnostics**: enters or exits the diagnostic view from the player-facing overlay.
- **F1**: toggles the Dev Panel.
- **F2**: toggles Run Diagnostics Focus.
- **F3**: cycles diagnostics pages.
- **Mouse wheel / PageUp / PageDown**: scroll diagnostics.

## 4. Validation checklist from clean main

Use this clean-main checklist before merge:

1. Fetch and pull `main` after PR89 has merged.
2. Confirm `git status --short` reports 0 changed files.
3. Open Unity on the clean repository state.
4. Run the full EditMode test suite.
5. Launch Bootstrap.
6. Execute the VS15 full smoke path from `docs/testing/evidence/vs/vs15-first-session-mvp-ux-hardening.md`.
7. Confirm there are no Unity console errors.
8. Exit Play Mode.
9. Confirm no generated Unity files remain in `git status --short`.

## 5. Diagnostics and raw-ID boundary

Player-facing text should not expose raw structure IDs, run IDs, or rule IDs. Player-facing labels and messages remain localization-owned so additional languages can be plugged in without code changes.

Developer diagnostics may show raw IDs where useful for debugging, triage, and handoff. This boundary is intentional: player-facing surfaces should be readable and localized, while diagnostics should preserve enough raw technical detail to make failures actionable.

Diagnostics pages remain available as pages 1/9 through 9/9.

## 6. Known limitations

- This is still a prototype Bootstrap overlay, not final UI.
- The current loop proves selection, placement, run observation, and feedback, but not deep long-term gameplay.
- Research remains limited, scaffolded, or unavailable depending on the current state of the repository.
- Structure differentiation may need stronger gameplay impact later.
- The loop is not yet a fully balanced 5- to 10-minute session.
- The UI still uses immediate-mode/dev-style overlay patterns and is not final production UX.
- Manual Game view smoke testing remains required for clipping and overflow.

## 7. Explicit non-goals preserved

VS16 preserves the existing locked scope. It adds no rewards, unlocks, costs, backend calls, offline research progression, offline heat processing, raids, Hostile/Raid heat tiers, seasons, leaderboards, monetization, new monster families, save schema changes, tuning changes, new structure definitions, new simulation behavior, formulas, or tutorial framework.

## 8. Recommended return-to-gameplay candidates

After VS16, prioritize gameplay/system work that makes the existing loop mechanically more interesting without reopening presentation-only slice expansion:

A. Meaningful structure differentiation using existing MVP-safe structures and existing data-driven simulation paths.

B. Research availability or verification progression, only if it is needed to support the MVP fantasy and stays within locked scope.

C. Small data/tuning pass to make the 5- to 10-minute loop feel better, using existing tables/configuration rather than hardcoded values.

D. Optional narrow run posture/risk choice only if supported by existing specs and without adding broad systems.

E. Save/load and restart-session polish only if it blocks playable validation.

## 9. Handoff rule

After VS16 is merged, return to gameplay/system development.

Do not continue adding presentation-only slices unless a smoke blocker or usability blocker is found.

The next PR after VS16 should be gameplay-facing, data-driven, and scoped to making the loop mechanically more interesting.
