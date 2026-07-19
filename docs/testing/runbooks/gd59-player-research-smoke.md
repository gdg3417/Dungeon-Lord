# GD59 Player Research Smoke and Validation Evidence

## Authority and scope

GD59 makes the configured Adventurer Activity Analysis project completable from the normal MVP action panel. The content-owned `claimAuthorityMode: localMvp` policy permits this prototype claim only through the local MVP boundary. Production verification remains unavailable, `CanClaimProduction` remains false, and no server is called. Local claim still requires the existing online/no-verification-pending restricted-action gate. The implementation adds no offline research progression, rewards, costs, projects, slots, backend, or research screen.

Malformed research saves are preserved rather than repaired by gameplay: orphaned progress, pending without progress, slot/project mismatches, and another active project must show a localized blocked state and expose neither Start nor Claim. Migration remains the only appropriate owner for future repair policy.

## Editor validation

1. Open the Bootstrap scene.
2. Enter Play Mode.
3. Use Clean MVP validation reset.
4. Hide diagnostics.
5. Confirm research is blocked before a valid run.
6. Build the starter dungeon.
7. Run the dungeon.
8. Confirm `Next:` says to start Activity Analysis.
9. Start research from the normal action panel.
10. Confirm Start disappears.
11. Confirm progress is visible.
12. Let several ticks occur without completing research.
13. Exit Play Mode or close the application through a valid save lifecycle.
14. Reopen and confirm the exact partial progress remains and completion-pending is still false.
15. Continue until research is ready.
16. Confirm `Next:` says Claim.
17. Confirm the action panel shows Claim and does not show Start or `Research unavailable`.
18. Set offline state and confirm Claim is blocked with localized feedback.
19. Restore online state.
20. Set verification pending and confirm Claim is blocked with localized feedback.
21. Clear verification pending.
22. Claim research.
23. Confirm pending and progress clear.
24. Confirm completed research records exactly one project.
25. Confirm Basic Run Analysis unlocks.
26. Confirm the First Dungeon Contract completes when its other requirements are satisfied.
27. Close and reopen.
28. Confirm completed research, unlock, contract, dungeon, run history, mana, loot, and heat remain coherent.
29. Confirm there are no console exceptions or missing references.

Use focused fixtures to validate malformed-state preservation; do not edit a normal player save manually for smoke acceptance. Confirm the existing research-status diagnostics distinguish `canClaimLocalMvp` from `canClaimProduction` and continue to report `wouldCallServer=False`.

## Windows non-development validation

1. Build or use a non-development Windows build.
2. Confirm F1 through F6 diagnostics are inaccessible.
3. Start from a clean save.
4. Complete the starter dungeon and first valid run.
5. Start Activity Analysis.
6. Confirm research progresses through normal play.
7. Close the build during partial progress.
8. Reopen and confirm the exact progress persists.
9. Reach ready-to-claim.
10. Confirm Claim is available without diagnostics.
11. Claim.
12. Confirm Basic Run Analysis unlocks.
13. Confirm the First Dungeon Contract completes.
14. Close and reopen.
15. Confirm persistence.
16. Confirm no raw IDs, localization keys, rule sources, or diagnostic values appear.

## Evidence status

- Automated EditMode coverage targets the public production tick boundary and covers shared authority, malformed-state preservation, restricted claim, deterministic progress, exactly-once completion-pending, actual `SaveService` partial/ready/completed round trips, canonical diagnostics-disabled play, unlock, contract, and reset behavior.
- Manual Editor smoke is **pending developer execution** because this environment does not provide interactive Unity Editor operation.
- Windows non-development build execution is **pending developer execution**; do not promote GD59 based on automated/static evidence alone.

## Next priority

Ordered two-room run resolution remains the next gameplay priority after GD59.
