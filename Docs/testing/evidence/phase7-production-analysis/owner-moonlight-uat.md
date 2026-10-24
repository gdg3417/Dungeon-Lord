# PR #249: prepared Moonlight acceptance

Prepared 2026-10-23. **Owner UAT has not been performed.** PR #249 remains draft and unmerged. Independent review should confirm the functional evidence and the explicit coverage limits in [functional qualification](functional-qualification.md) before owner handoff.

## Launch

Connect to this Windows desktop through Moonlight and double-click **PR249 Analysis UAT** on the desktop. The launcher opens with five scenarios and three window sizes. It does not start a game until you click **Start fresh scenario**.

Package: `C:\Dev\Dungeon-Lord\Temp\pr249-analysis-owner-f1`.

The folder also contains **Start Analysis UAT.cmd** as an alternate double-click entry. No commands, JSON editing, save-file movement, Unity setup or Task Manager operation is needed.

This is an isolated qualification copy of the existing Development build, artifact `P249R2-fe8daae0264348579db73d3402254215`, qualified source `c870c5dc92aee87e854af350aa0a73653d045ff2`. Its 303 build files are unchanged. The visible product name deliberately remains **DL B3B2 Smoke Two Floor P249R2**; it is not an ordinary player distribution.

## Sequential acceptance

1. **Reporting and automatic parties.** Choose **Main dungeon**, **Desktop 1280×720**, then **Start fresh scenario**. Confirm Dungeon is the landing screen. Open the compact Analysis entry. Check that the report describes the most recent retained completed runs, shows coverage and explains the limits of historical detail. Leave it open while automatic parties progress; the retained count or totals should eventually update. The fixture starts with seven genuine completed records and a foreground party. Timing depends on the continuing normal simulation.

2. **Return and replay.** Return to Dungeon. Once an automatic run completes in this session, open **Replay Run**. Open Analysis from the compact entry, then return. The replay should close, the normal header should return and replay should not restart itself. Immediately after reopening a saved fixture, replay may correctly be unavailable until a compatible current-session run completes; saved history does not recreate detailed evidence.

3. **Finish a session and reopen.** Close the game through its normal Windows close control. Return to the launcher and wait for its final status. If Windows remains alive after a successful AppQuit save, the launcher waits 30 seconds, records a failed graceful exit and closes only that verified saved campaign process. It then archives the session and restores the original isolated namespace. Choose **Reopen last saved session** for the same scenario. Reporting should retain prior results without duplicating completed runs; legitimate new automatic outcomes may increase counts. Do not start another game until the launcher enables its buttons.

4. **Valid draft choices.** Close the game and wait for launcher completion. Choose **Funded editing / empty history**, then **Start fresh scenario**. This fixture has 1000 mana and one room with a skeleton. Enter Edit, select the skeleton, choose **Move**, select a different valid cell and **Confirm Placement**. Open **Display → Analysis**. Try **Continue Editing** first: the edit must remain. Reopen the review and choose **Discard Changes**: Analysis should open and the original position should remain after returning. Repeat the move, choose **Save Changes**, then return: the new position should be committed and normal Dungeon controls restored. Content repositioning is free; no purchase is necessary. This fixture intentionally has an incomplete adventurer path, so use Main dungeon for automatic-party acceptance.

5. **Invalid and recovered draft.** Close the game and wait for completion. Choose **Recover an invalid draft** and start fresh. Choose **Resume Editing**. Open **Display → Analysis**. The dialog should explain the out-of-bounds room intent and offer **Discard Changes** and **Continue Editing**, without Save. Continue should preserve the invalid draft. Reopen the review and Discard; Analysis should open. Closing an acknowledged draft and reopening its last saved session should offer recovery rather than silently lose it.

6. **Partial history and stale protection.** Try **Partial historical summaries**. It is an explicitly synthetic historical-optional-field fixture: initially only one of two records supports each metric. Missing summaries must be excluded, with independent coverage, rather than invented as zeros or causes. New genuine outcomes can increase coverage. Then try **Stale draft safety**. It combines exact authority-written artifacts from different canonical baselines; the old journal must not resume or merge. Read the localized reason and discard through the offered control. Save must not be offered for that stale recovery.

7. **Phone layouts and Large text.** Close each game and wait for completion before changing window size. Repeat navigation at **Portrait 360×640** and **Phone landscape 640×360**. Open Display from Analysis and compare Default/Large text. Use scrolling where needed to reach report guidance or bounded Dungeon panels. Confirm the compact Analysis entry, Dungeon return and Display controls remain reachable. Record whether actual phone taps, scrolling and readability are comfortable. Native Windows mouse checks and the earlier injected-safe-area automated tests do not establish physical phone acceptance.

8. **Record your verdict.** Capture Moonlight screenshots or a short recording of any obscured control, unexpected navigation, lost draft, inconsistent report or responsiveness problem. Report the scenario, window size, text size and action taken. The launcher retains technical logs, exact fixture identities, process-exit results and save snapshots automatically. Owner acceptance is your observation; it is not automatically declared by the launcher.

## Windows closure limitation

The owner reports that the game-window closing problem predates PR #249 and has historically required Task Manager. Baseline technical smoke code also documents native shutdown stalls. Identical root cause has not been established. Every recorded failed graceful exit remains failed, and the graceful-shutdown gate is not marked passed.

The launcher handles the limited saved-process termination procedure. It never treats window disappearance alone as exit and never replaces or snapshots a save as successful before both native process handles confirm termination. A missing AppQuit success marker, incomplete draft receipt, uncertain identity, unexpected save contents or preservation difference stops the launcher for Codex review. Do not work around a STOP message or manually alter files.

If a Windows network-permission dialog appears, Cancel is sufficient; no firewall permission or security change is required for this local acceptance task.

## Coverage and readiness limits

Five scenario copies, a desktop shortcut, an unchanged build mirror and a tested save/process-isolation launcher are prepared. The launcher window and scenario list were visually checked without starting an owner game. Its preservation dry run and native child exit-code check passed.

Transient pending durability, authoritative save rejection and in-session stale navigation retain passing qualified automated coverage; those exact states were not directly observed through native desktop input in this continuation. Native stale recovery and filesystem-failed Discard/retry were exercised separately. Review must not relabel them as identical cases. Physical Moonlight acceptance and final merge approval remain outstanding.
