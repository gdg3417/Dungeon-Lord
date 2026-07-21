# GD60 ordered two-room run smoke runbook

## Editor path

1. Start the MVP from a clean save and verify the Console is clear.
2. Build and run a one-room dungeon; record its aggregate result.
3. Add Room 2 and configure distinct monster, trap, and loot content in both rooms.
4. Run once and verify Room 1 resolves before Room 2 and that Room 1 survivors enter Room 2.
5. Put the more dangerous configuration in Room 1 and the reward-heavy configuration in Room 2. Verify casualties can prevent or weaken the Room 2 attempt.
6. Swap the configurations, repeat with the same starting conditions, and verify the deterministic route outcome can change.
7. Close and reopen the game during the test. Confirm both assignments, selected room, ordered latest room results, survivors, loot, heat, run history, research completion, and First Dungeon Contract state persist.
8. Hide diagnostics. Confirm compact player output contains no raw option IDs, rule IDs, localization keys, or unresolved format placeholders.
9. Confirm the Post-Contract Greed Trial and GD59 research claim flow remain functional.
10. Confirm there are no Unity Console errors.

## Player build

Repeat the essential one-room, two-room, swapped-room, restart, and diagnostics-hidden path in a Windows non-development build. Record build version and observed outcomes without committing Player logs, saves, screenshots, or generated output.
