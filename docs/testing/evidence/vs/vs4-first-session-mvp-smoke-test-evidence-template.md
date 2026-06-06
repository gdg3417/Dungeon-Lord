# VS4 First-Session MVP Smoke Test Evidence Template

## Test run metadata

| Field | Value |
| --- | --- |
| Tester |  |
| Date |  |
| Unity version |  |
| OS |  |
| Branch |  |
| Commit SHA |  |
| PR number |  |
| Test run start time |  |
| Test run end time |  |
| Test script result | PASS / FAIL / NOT RERUN - existing EditMode pass evidence attached |
| Smoke test result | PASS / FAIL |

## Evidence naming reminder

Use `vs4_<PR-number>_<commit-short-sha>_<step-id>_<artifact>_<YYYYMMDD-HHMMSS>.<extension>` for screenshots, exported test results, and terminal captures.

## Smoke section evidence tables

### Setup

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| SETUP-01 | Record Unity version, OS, branch, commit SHA, PR number, and start time. | Metadata is complete before Play Mode. |  |  |  |
| SETUP-02 | Confirm `git status --short` is clean before testing. | No modified, added, deleted, untracked, or unexpected `.meta` files. |  |  |  |
| SETUP-03 | Open `Assets/_Project/Scenes/Bootstrap.unity`, clear Console, and enter Play Mode. | Bootstrap launches, overlay appears, and Console has no errors. |  |  |  |

### Default overlay

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| OVERLAY-01 | Observe the default overlay before pressing F1, F2, or F3. | **MVP Loop Summary** is visible above diagnostics. |  |  |  |
| OVERLAY-02 | Read the MVP Loop Summary lines. | Placement, latest run, mana, loot, heat, research, and next-step lines are readable. |  |  |  |
| OVERLAY-03 | Check default diagnostics header. | **Runtime Summary (1/7)** is visible and readable. |  |  |  |

### Guided action initial state

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| GUIDED-01 | Observe the panel below MVP Loop Summary. | **Guided MVP Action** is visible. |  |  |  |
| GUIDED-02 | Count guided action lines. | Exactly one **Step:** line, one **Status:** line, and one **Next action:** line are visible. |  |  |  |
| GUIDED-03 | Read the initial next action. | The next action recommends one placement/modification action or one run/observe action, depending on current save state. |  |  |  |

### Placement or modification

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| PLACE-01 | Press F1 to open Dev Panel. | **Dev Panel** opens. |  |  |  |
| PLACE-02 | Click **Place Mana Generator**, or click **Select Next Slot** and then one structure button. | One structure is placed or one selected slot is modified. |  |  |  |
| PLACE-03 | Press F1 to close Dev Panel and review overlay. | MVP Loop Summary remains visible and placement state updates or remains valid. |  |  |  |

### Run or observe

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| RUN-01 | Press F1 to open Dev Panel. | **Dev Panel** opens. |  |  |  |
| RUN-02 | Click **Run Test Adventure** once. | One dungeon run is observed without Console errors. |  |  |  |
| RUN-03 | Press F1 to close Dev Panel and review latest run. | **Latest run:** no longer reads **No run yet**. |  |  |  |

### MVP Loop Summary after run

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| SUMMARY-01 | Read **Mana reserve:** after the run. | Mana value is visible and numeric. |  |  |  |
| SUMMARY-02 | Read **Loot:** after the run. | Generated, extracted, and tradeable loot values are visible. |  |  |  |
| SUMMARY-03 | Read **Heat:** after the run. | Before/after heat values and heat tier are visible. |  |  |  |
| SUMMARY-04 | Read **Research:** after the run. | Research state text is visible. |  |  |  |
| SUMMARY-05 | Read **Next:** and **Next action:** after the run. | Exactly one next improvement step/action is recommended. |  |  |  |

### Diagnostics paging

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| DIAG-01 | Confirm full diagnostics mode is active. | Header is not **Diagnostics: Run Diagnostics Focus**. |  |  |  |
| DIAG-02 | Confirm page 1. | **Runtime Summary (1/7)** is visible. |  |  |  |
| DIAG-03 | Press F3 once. | **Run Diagnostics (2/7)** is visible. |  |  |  |
| DIAG-04 | Press F3 once. | **Heat Diagnostics (3/7)** is visible. |  |  |  |
| DIAG-05 | Press F3 once. | **Systems Diagnostics (4/7)** is visible. |  |  |  |
| DIAG-06 | Press F3 once. | **Research Diagnostics (5/7)** is visible. |  |  |  |
| DIAG-07 | Press F3 once. | **Research Status Diagnostics (6/7)** is visible. |  |  |  |
| DIAG-08 | Press F3 once. | **Research Verification Diagnostics (7/7)** is visible. |  |  |  |
| DIAG-09 | Press F3 once from page 7/7. | F3 wraps back to **Runtime Summary (1/7)**. |  |  |  |

### F2 run diagnostics focus

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| F2-01 | Press F2 once. | Header changes to **Diagnostics: Run Diagnostics Focus**. |  |  |  |
| F2-02 | Observe player-facing panels during F2 focus. | MVP Loop Summary and Guided MVP Action are hidden. |  |  |  |
| F2-03 | Read run diagnostics during F2 focus. | Run diagnostics lines are readable. |  |  |  |
| F2-04 | Press F2 again. | MVP Loop Summary and Guided MVP Action return. |  |  |  |

### F1 dev panel

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| F1-01 | Press F1 once. | **Dev Panel** opens. |  |  |  |
| F1-02 | Confirm expected non-destructive controls are visible. | Buttons such as **Save Now**, **Clear Banner**, **Run Structure Tick**, and **Run Test Adventure** are visible. |  |  |  |
| F1-03 | Press F1 once. | **Dev Panel** closes and overlay remains readable. |  |  |  |

### Scrolling

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| SCROLL-01 | Navigate to a diagnostics page with more than four body lines. | A scrollable diagnostics body is visible. |  |  |  |
| SCROLL-02 | Move mouse wheel down and up. | Diagnostics lines move down and back up when content is available. |  |  |  |
| SCROLL-03 | Press PageDown and PageUp. | Diagnostics lines move by a page and return when content is available. |  |  |  |
| SCROLL-04 | Observe overlay after scrolling. | Text remains readable and gameplay state does not mutate from scrolling. |  |  |  |

### Console

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| CONSOLE-01 | Review Unity Console after launch. | No Console errors. |  |  |  |
| CONSOLE-02 | Review Unity Console after placement/modification. | No Console errors. |  |  |  |
| CONSOLE-03 | Review Unity Console after run/observe. | No Console errors. |  |  |  |
| CONSOLE-04 | Review Unity Console after F1/F2/F3/scrolling checks. | No Console errors. |  |  |  |

### Generated files check

| Step ID | Action | Expected result | Pass/Fail | Evidence filename or link | Notes |
| --- | --- | --- | --- | --- | --- |
| FILES-01 | Exit Play Mode and wait for Unity to settle. | Editor returns to Edit Mode without errors. |  |  |  |
| FILES-02 | Run `git status --short`. | No unexpected generated files. |  |  |  |
| FILES-03 | Inspect any listed files. | No unexpected `.meta` files or runtime/config/localization/asset changes are present. |  |  |  |

## Required evidence inventory

| Required artifact | Filename or link | Captured? | Notes |
| --- | --- | --- | --- |
| Default overlay screenshot |  |  |  |
| Guided action before run screenshot |  |  |  |
| After placement or modification screenshot |  |  |  |
| After run screenshot |  |  |  |
| F2 Run Diagnostics Focus screenshot |  |  |  |
| Research Diagnostics page screenshot |  |  |  |
| Research Status Diagnostics page screenshot |  |  |  |
| Research Verification Diagnostics page screenshot |  |  |  |
| Unity Test Runner EditMode pass screenshot or export |  |  |  |
| Post-run `git status --short` capture |  |  |  |

## Stop condition review

| Stop condition | Occurred? | Defect ID or notes |
| --- | --- | --- |
| Test scripts failed. |  |  |
| Overlay text overflowed and could not be read. |  |  |
| Console errors appeared. |  |  |
| Guided action recommended more than one next action. |  |  |
| Player-facing text showed raw localization keys unexpectedly. |  |  |
| Gameplay state mutated from only viewing panels or using F2/F3/PageUp/PageDown/mouse wheel. |  |  |
| Unexpected generated files or `.meta` files appeared. |  |  |

## Final gate

Choose exactly one:

- [ ] **GO** - All required checks passed, all required evidence is attached, and no blocking follow-up is required.
- [ ] **GO WITH FOLLOW-UP** - First-session MVP smoke passed, but non-blocking documentation, evidence, or minor cleanup follow-up is recommended.
- [ ] **NO-GO** - One or more required checks failed, a stop condition occurred, or evidence is insufficient for review.

Gate rationale:


## Open defects

| Defect ID | Severity | Title | Step ID | Evidence filename or link | Owner | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |  |  |

## Follow-up PR recommendations

| Recommendation ID | Priority | Recommendation | Rationale | Blocking final gate? | Notes |
| --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |
