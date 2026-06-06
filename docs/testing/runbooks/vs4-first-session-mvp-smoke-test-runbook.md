# VS4 First-Session MVP Smoke Test Runbook

## Purpose and scope

This runbook gives a click-by-click Unity smoke test for the first-session MVP vertical slice after VS1, VS2, and VS3. A non-coder reviewer should be able to follow it to confirm that the Bootstrap scene exposes the MVP Loop Summary, the Guided MVP Action path, one placement or modification action, one dungeon run observation, mana/loot/heat/research signals, diagnostics paging, and evidence capture.

This is a documentation-only validation workflow. It does not request or authorize gameplay changes, new tuning, new strings, new assets, new tests, backend calls, rewards, unlocks, costs, offline systems, raids, seasons, leaderboards, monetization, monster families, or a broad tutorial framework.

## Prerequisites

Before starting, record these values in the evidence template:

- Unity version in use: record the exact Unity Editor version shown in **Unity Hub** or **Help** -> **About Unity**.
- Branch and commit SHA: run `git branch --show-current` and `git rev-parse HEAD`, or ask the PR owner for the exact values.
- Clean working tree: run `git status --short`; it must print no modified, added, deleted, untracked, or unexpected `.meta` files before the smoke test begins.
- PR under test: record the PR number and title.
- Latest Unity EditMode tests pass: confirm the PR owner has attached current Unity Test Runner EditMode pass evidence, or run the EditMode suite if requested by the reviewer.
- Bootstrap scene available: confirm `Assets/_Project/Scenes/Bootstrap.unity` exists in the Unity **Project** window.
- Evidence folder available: create or choose a local folder for screenshots and exported test results before entering Play Mode.

## Pass/fail rules

A smoke test result is **PASS** only when all of the following are true:

1. The Bootstrap scene launches in Play Mode without console errors.
2. The default overlay shows **MVP Loop Summary** and **Guided MVP Action**.
3. The guided action shows exactly one **Next action:** line at a time.
4. One structure can be placed or the selected slot can be modified through the existing Dev Panel buttons.
5. One dungeon run can be run or observed through the existing Dev Panel button.
6. The MVP Loop Summary displays readable placement, latest run, mana, loot, heat, research, and next-step lines after the run.
7. Diagnostics pages 1/7 through 7/7 are readable, and F3 wraps from 7/7 back to 1/7.
8. F2 Run Diagnostics Focus hides the MVP Loop Summary and Guided MVP Action, and pressing F2 again restores both panels.
9. F1 opens and closes the Dev Panel.
10. Mouse wheel, PageUp, and PageDown scrolling move diagnostics content when the current diagnostics page has more than four body lines.
11. The Unity Console has no errors.
12. Evidence files are captured with the required names.
13. `git status --short` after the run shows no unexpected generated files, especially no unexpected `.meta` files.

A smoke test result is **FAIL** if any required pass condition fails or any stop condition below occurs. Use **GO WITH FOLLOW-UP** only for a non-blocking documentation or evidence issue that does not affect first-session playability, readability, diagnostics function, localization display, or state safety.

## Stop conditions

Stop immediately, record a **NO-GO** result, and log a defect if any of these occur:

- Test scripts fail while preparing or confirming the latest Unity EditMode pass evidence.
- Overlay text overflows, clips, or becomes unreadable and cannot be read after using mouse wheel, PageUp, or PageDown.
- Unity Console errors appear during scene launch, placement/modification, run simulation, panel toggles, paging, or scrolling.
- Any Guided MVP Action state recommends more than one next action at the same time.
- Player-facing text unexpectedly shows raw localization keys, such as `ui.mvp_loop.panel.title` or `guided_mvp.action.run_dungeon`, instead of localized text.
- Gameplay state mutates from just viewing panels, pressing F2, pressing F3, pressing PageUp, pressing PageDown, or using the mouse wheel.

## Evidence file naming conventions

Use this exact pattern for every evidence artifact:

`vs4_<PR-number>_<commit-short-sha>_<step-id>_<artifact>_<YYYYMMDD-HHMMSS>.png`

Use `.xml`, `.txt`, or `.md` instead of `.png` only when the artifact is not a screenshot.

Examples:

- `vs4_PR78_abc1234_01_default-overlay_20260606-143000.png`
- `vs4_PR78_abc1234_05_after-run_20260606-143500.png`
- `vs4_PR78_abc1234_10_unity-test-runner-editmode-pass_20260606-144000.png`
- `vs4_PR78_abc1234_12_git-status-after_20260606-144300.txt`

Required screenshots or evidence exports:

1. Screenshot default overlay.
2. Screenshot guided action before run.
3. Screenshot after placement or modification.
4. Screenshot after run.
5. Screenshot F2 Run Diagnostics Focus.
6. Screenshot Research Diagnostics page.
7. Screenshot Research Status Diagnostics page.
8. Screenshot Research Verification Diagnostics page.
9. Screenshot Unity Test Runner pass results.
10. Text capture or screenshot showing clean `git status --short` after the run.

## Click-by-click smoke test steps

### 1. Setup and launch scene

1. Open **Unity Hub**.
2. Select the Dungeon-Lord project.
3. Click **Open** and wait for the Unity Editor to finish importing and compiling.
4. In Unity, open the **Project** window.
5. Navigate to **Assets** -> **_Project** -> **Scenes**.
6. Double-click **Bootstrap.unity**.
7. Confirm the **Hierarchy** window shows the Bootstrap scene and a `GameRoot` object.
8. Open **Window** -> **General** -> **Console**.
9. Click **Clear** in the Console toolbar.
10. Confirm the Console error counter is zero.
11. Click the Unity **Play** button.
12. Wait until the overlay text appears.
13. Confirm no Console errors appear.

Expected result: Play Mode starts on Bootstrap with a readable overlay and no Console errors.

Evidence: capture `vs4_<PR>_<sha>_00_scene-launch_<timestamp>.png`.

### 2. Confirm default MVP Loop Summary overlay

1. Do not press any function keys yet.
2. Read the top overlay panel.
3. Confirm the heading **MVP Loop Summary** is visible.
4. Confirm the summary includes readable lines beginning with **Placement:**, **Latest run:**, **Mana reserve:**, **Loot:**, **Heat:**, **Research:**, and **Next:**.
5. Confirm no raw localization keys are visible in those lines.
6. Confirm the diagnostics header below the panels shows **Runtime Summary (1/7)**.

Expected result: the default overlay shows the player-facing MVP Loop Summary above page 1/7 diagnostics.

Evidence: capture `vs4_<PR>_<sha>_01_default-overlay_<timestamp>.png`.

### 3. Confirm Guided MVP Action initial state before run

1. Still in the default overlay, look immediately below **MVP Loop Summary**.
2. Confirm the heading **Guided MVP Action** is visible.
3. Confirm the panel includes exactly one **Step:** line.
4. Confirm the panel includes exactly one **Status:** line.
5. Confirm the panel includes exactly one **Next action:** line.
6. If no structure is currently placed, the expected next action is **Place one structure, or modify the selected slot.**
7. If a structure is already placed but no latest run is present, the expected next action is **Run the dungeon and watch the MVP Loop Summary update.**
8. Confirm no second or conflicting **Next action:** line is visible.

Expected result: the guided panel gives one clear next action before the run.

Evidence: capture `vs4_<PR>_<sha>_02_guided-action-before-run_<timestamp>.png`.

### 4. Place or modify one structure

1. Press **F1** once.
2. Confirm the **Dev Panel** appears on the left side of the Game view.
3. If the guided action asks for placement, click **Place Mana Generator** once.
4. If the selected slot already contains a structure and the reviewer wants a modification instead, click **Select Next Slot** once, then click exactly one of these structure buttons once: **Place Mana Generator**, **Place Heat Scrubber**, or **Place Risk Lab**.
5. Do not click more than one placement button unless the first click clearly fails and the defect is being investigated.
6. Press **F1** once to close the Dev Panel.
7. Confirm **MVP Loop Summary** is still visible.
8. Confirm **Placement:** no longer reads **No structure placed**, or confirm the visible selected slot/placement line changed after the modification.
9. Confirm **Guided MVP Action** still shows exactly one **Next action:** line.
10. Confirm no Console errors appear.

Expected result: one existing structure is placed or one selected slot is modified using existing Dev Panel controls, and the overlay remains readable.

Evidence: capture `vs4_<PR>_<sha>_03_after-placement-or-modification_<timestamp>.png`.

### 5. Run or observe one dungeon run

1. Press **F1** once.
2. Confirm the **Dev Panel** appears.
3. Click **Run Test Adventure** once.
4. If the button reports a failure banner, stop and log a defect unless the failure is already explained by a required prerequisite miss.
5. Press **F1** once to close the Dev Panel.
6. Confirm **MVP Loop Summary** is visible.
7. Confirm **Latest run:** no longer reads **No run yet**.
8. Confirm no Console errors appear.

Expected result: one existing test adventure run is observed, and the latest run line updates.

Evidence: capture `vs4_<PR>_<sha>_04_run-or-observe_<timestamp>.png`.

### 6. Observe mana, loot, heat, research, and one next improvement step

1. In **MVP Loop Summary**, read **Mana reserve:** and confirm it is visible and numeric.
2. Read **Loot:** and confirm generated, extracted, and tradeable values are visible.
3. Read **Heat:** and confirm before/after heat values and a heat tier label are visible.
4. Read **Research:** and confirm the research state text is visible.
5. Read **Next:** and confirm it recommends exactly one next improvement step.
6. In **Guided MVP Action**, read **Next action:** and confirm it recommends exactly one action.
7. Confirm the summary and guided action do not contradict each other.
8. Confirm no raw localization keys are visible.

Expected result: mana, loot, heat, research, and one next improvement step are visible after the run.

Evidence: capture `vs4_<PR>_<sha>_05_after-run-summary-and-next-step_<timestamp>.png`.

### 7. Verify diagnostics paging with F3

1. Ensure F2 Run Diagnostics Focus is off. If the header says **Diagnostics: Run Diagnostics Focus**, press **F2** once to return to full diagnostics.
2. Confirm the header shows **Runtime Summary (1/7)**.
3. Press **F3** once and confirm **Run Diagnostics (2/7)**.
4. Press **F3** once and confirm **Heat Diagnostics (3/7)**.
5. Press **F3** once and confirm **Systems Diagnostics (4/7)**.
6. Press **F3** once and confirm **Research Diagnostics (5/7)**.
7. Capture the Research Diagnostics screenshot.
8. Press **F3** once and confirm **Research Status Diagnostics (6/7)**.
9. Capture the Research Status Diagnostics screenshot.
10. Press **F3** once and confirm **Research Verification Diagnostics (7/7)**.
11. Capture the Research Verification Diagnostics screenshot.
12. Press **F3** once and confirm F3 wraparound returns to **Runtime Summary (1/7)**.
13. Confirm **MVP Loop Summary** and **Guided MVP Action** remain visible while paging.
14. Confirm no Console errors appear.

Expected result: all seven diagnostics pages are reachable, page names and counts are readable, and F3 wraps from 7/7 to 1/7.

Evidence:

- `vs4_<PR>_<sha>_06_research-diagnostics-page_<timestamp>.png`
- `vs4_<PR>_<sha>_07_research-status-diagnostics-page_<timestamp>.png`
- `vs4_<PR>_<sha>_08_research-verification-diagnostics-page_<timestamp>.png`
- Optional wraparound screenshot: `vs4_<PR>_<sha>_09_f3-wraparound-runtime-summary_<timestamp>.png`

### 8. Verify F2 Run Diagnostics Focus

1. From any full diagnostics page, press **F2** once.
2. Confirm the header changes to **Diagnostics: Run Diagnostics Focus**.
3. Confirm **MVP Loop Summary** is hidden.
4. Confirm **Guided MVP Action** is hidden.
5. Confirm run diagnostics lines remain readable, including **Run**, **Run History**, **Run Loot**, **Run Survival**, **Run Extraction**, and heat-related run lines if present.
6. Capture the F2 focus screenshot.
7. Press **F2** once again.
8. Confirm **MVP Loop Summary** returns.
9. Confirm **Guided MVP Action** returns.
10. Confirm no Console errors appear.

Expected result: F2 hides player-facing panels only while focused run diagnostics are active, and F2 again restores both panels.

Evidence: capture `vs4_<PR>_<sha>_10_f2-run-diagnostics-focus_<timestamp>.png`.

### 9. Verify F1 Dev Panel opens and closes

1. Press **F1** once.
2. Confirm the **Dev Panel** opens.
3. Confirm the panel contains buttons such as **Save Now**, **Delete Save**, **Clear Banner**, **Run Structure Tick**, and **Run Test Adventure**.
4. Do not click destructive controls such as **Delete Save** during this check.
5. Press **F1** once.
6. Confirm the **Dev Panel** closes.
7. Confirm **MVP Loop Summary** and **Guided MVP Action** remain visible after the panel closes.
8. Confirm no Console errors appear.

Expected result: F1 opens and closes the Dev Panel without breaking the overlay.

Evidence: capture `vs4_<PR>_<sha>_11_f1-dev-panel_<timestamp>.png` while the panel is open.

### 10. Verify scrolling controls

1. Press **F3** until a diagnostics page with more than four body lines is visible, such as **Run Diagnostics (2/7)** or **Research Diagnostics (5/7)**.
2. Note the first visible diagnostics body line below the hint lines.
3. Scroll the mouse wheel down one notch.
4. Confirm the visible diagnostics body lines move down by one line when more content is available.
5. Scroll the mouse wheel up one notch.
6. Confirm the visible diagnostics body lines move back up by one line when prior content is available.
7. Press **PageDown** once.
8. Confirm the visible diagnostics body lines move down by one page when more content is available.
9. Press **PageUp** once.
10. Confirm the visible diagnostics body lines move back up by one page when prior content is available.
11. Confirm the overlay remains readable and no Console errors appear.

Expected result: mouse wheel, PageUp, and PageDown scroll diagnostics content without mutating gameplay state.

Evidence: capture `vs4_<PR>_<sha>_12_scrolling-controls_<timestamp>.png`.

### 11. Verify Console and generated files

1. Open **Window** -> **General** -> **Console**.
2. Confirm the Console error counter is zero.
3. Click the Unity **Play** button to exit Play Mode.
4. Wait for Unity to finish leaving Play Mode.
5. In a terminal at the repository root, run `git status --short`.
6. Confirm no unexpected generated files appear.
7. Confirm no unexpected `.meta` files appear.
8. If Unity modified expected local-only files, record them in the evidence notes and ask the PR owner before discarding or committing anything.

Expected result: no Console errors and no unexpected generated files.

Evidence:

- `vs4_<PR>_<sha>_13_console-no-errors_<timestamp>.png`
- `vs4_<PR>_<sha>_14_git-status-after_<timestamp>.txt`

### 12. Capture Unity Test Runner pass evidence

1. Open **Window** -> **General** -> **Test Runner**.
2. Select the **EditMode** tab.
3. Confirm the latest EditMode run result shows all tests passing for the PR under test.
4. If the reviewer must re-run the suite, click **Run All** in the EditMode tab and wait for completion.
5. Capture the Unity Test Runner pass results.
6. If any EditMode test fails, stop and record **NO-GO**.

Expected result: latest Unity EditMode tests pass evidence is attached to the smoke test record.

Evidence: capture `vs4_<PR>_<sha>_15_unity-test-runner-editmode-pass_<timestamp>.png`.

## Defect logging

Log every defect with:

- defect ID;
- title;
- severity;
- environment: Unity version, OS, branch, commit SHA, and PR number;
- exact smoke step ID;
- expected result;
- actual result;
- reproduction steps;
- evidence filename or link;
- Console error text if present;
- whether the test stopped.

Severity definitions:

- **S0 Blocker:** prevents launching Bootstrap, corrupts or mutates state from view-only panel actions, causes test script failure, or blocks any further smoke testing.
- **S1 Critical:** breaks MVP Loop Summary, Guided MVP Action, placement/modification, run observation, diagnostics focus, diagnostics paging, localization display, or causes Console errors.
- **S2 Major:** evidence-required information is present but unreadable without workaround, scrolling is unreliable, F1/F2/F3 behavior is inconsistent, or generated files appear unexpectedly.
- **S3 Minor:** typo, documentation ambiguity, screenshot naming mistake, or non-blocking evidence organization issue.

## Final checklist

- [ ] Unity version recorded.
- [ ] Branch recorded.
- [ ] Commit SHA recorded.
- [ ] PR number recorded.
- [ ] Clean working tree confirmed before test.
- [ ] Latest Unity EditMode tests pass evidence captured.
- [ ] Bootstrap scene launched.
- [ ] Default overlay screenshot captured.
- [ ] Guided action before run screenshot captured.
- [ ] Placement or modification screenshot captured.
- [ ] Run or observe screenshot captured.
- [ ] Mana signal visible.
- [ ] Loot signal visible.
- [ ] Heat signal visible.
- [ ] Research signal visible.
- [ ] One next improvement step visible.
- [ ] Page 1/7 Runtime Summary confirmed via F3 flow.
- [ ] Page 2/7 Run Diagnostics confirmed via F3 flow.
- [ ] Page 3/7 Heat Diagnostics confirmed via F3 flow.
- [ ] Page 4/7 Systems Diagnostics confirmed via F3 flow.
- [ ] Page 5/7 Research Diagnostics confirmed via F3 flow.
- [ ] Page 6/7 Research Status Diagnostics confirmed via F3 flow.
- [ ] Page 7/7 Research Verification Diagnostics confirmed via F3 flow.
- [ ] F3 wraparound from 7/7 back to 1/7 confirmed.
- [ ] F2 focus hides MVP Loop Summary and Guided MVP Action.
- [ ] F2 again restores MVP Loop Summary and Guided MVP Action.
- [ ] F1 opens and closes Dev Panel.
- [ ] Mouse wheel scrolling confirmed.
- [ ] PageUp scrolling confirmed.
- [ ] PageDown scrolling confirmed.
- [ ] Console has no errors.
- [ ] No unexpected generated files.
- [ ] No unexpected `.meta` files.
- [ ] Evidence template completed.
- [ ] Final gate recorded as GO, GO WITH FOLLOW-UP, or NO-GO.
