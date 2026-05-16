# Sprint 1 Close-out Checklist (Execution Tracker + UAT Script)

Date: 2026-05-13  
Scope: Final evidence tracker and click-by-click UAT script required to mark Sprint 1 complete.

## Completion rule
Sprint 1 is **not closed** until every item below is marked PASS with evidence links/log snippets.

## Pre-flight (do this before running checks)
1. Install/open the project in the agreed Unity Editor version for the branch.
2. Confirm local branch is up to date and clean.
3. Ensure these windows are visible: **Console**, **Project**, **Test Runner**, **Game**.
4. Clear old console logs so results are easy to capture.
5. Create a timestamped evidence folder (for screenshots/logs) under your team artifact location.

---

## UAT-01: Run full Unity EditMode suite

### Goal
Prove baseline Sprint 1 EditMode tests pass in a real Unity runner.

### Click-by-click steps
1. Open Unity Hub.
2. Click **Projects**.
3. Select this repo project and click **Open**.
4. Wait for asset/script import to complete.
5. In Unity top menu, click **Window** -> **General** -> **Test Runner**.
6. In the Test Runner panel, open the tab where Sprint 1 tests are listed (current repo configuration: **PlayMode** tab).
7. Click **Run All**.
8. Wait for execution to finish.
9. In Console, confirm no unexpected runtime/editor errors occurred during test run.
10. Capture screenshot of Test Runner summary (passed/failed/skipped totals).
11. Save/export logs to artifact folder.

### Pass criteria
- All required Sprint 1 EditMode tests pass.
- No blocking exceptions in Console tied to Sprint 1 systems.

### Evidence required
- Screenshot of Test Runner totals.
- Full test log artifact path.
- Timestamp and tester name.

---

## UAT-02: Run deterministic replay test 3 consecutive times

### Goal
Prove deterministic replay stability across repeated executions.

### Click-by-click steps
1. Keep Unity open with **Window** -> **General** -> **Test Runner**.
2. In the test list tab used for Sprint 1 tests (currently **PlayMode**), locate the determinism test group (`SimulationDeterminismTests`).
3. Select only the determinism test(s) for targeted reruns.
4. Click **Run Selected** (Run #1).
5. Record result and capture screenshot named `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.png`.
6. Click **Run Selected** again (Run #2).
7. Record result and capture screenshot named `sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.png`.
8. Click **Run Selected** again (Run #3).
9. Record result and capture screenshot named `sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.png`.
10. Export test results XML after each run and save as:
    - `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`
    - `sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`
    - `sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml`
11. Confirm all three runs passed with no divergent checkpoint assertions.

### Pass criteria
- 3/3 consecutive determinism runs pass.
- No nondeterministic assertion drift between runs.

### Evidence required
- Three run screenshots or a combined run log showing 3 passes.
- Notes confirming checkpoints remained identical.

---

## UAT-03: Validate pause/resume + offline elapsed invariants

### Goal
Verify time progression behavior does not violate determinism/offline rules.

### Click-by-click steps
1. In Unity, open the bootstrap scene used for runtime start (Project window -> locate `Bootstrap.unity` -> double-click).
2. Click **Play**.
3. Let simulation run briefly and observe the top-left overlay lines for `Tick`, `Mana`, `Heat`, `Save`, `Pause`, and `Pending/Gate`.
4. Click **Pause** in the Play controls.
5. Wait ~10–20 seconds.
6. Click **Play** (resume).
7. Verify `Tick` keeps increasing, `Pause` returns to `Running`, and `Heat`/`Mana` do not jump to impossible values.
8. Stop Play mode.
9. Re-enter Play mode, then pause Unity Editor play for ~15 seconds and resume.
10. Confirm `Pause` line changes `Paused` -> `Running` and check banner/console for skew warning only when large time change is detected.
11. Capture before/after screenshots of overlay values and one Console screenshot if a warning banner appears.

### Pass criteria
- Pause/resume does not create invalid jumps or invariant breaks.
- Offline elapsed path stays within expected caps/guardrails.

### Evidence required
- Before/after screenshots.
- Short scenario notes (inputs, elapsed time, expected vs actual).

---

## UAT-04: Validate save migration fixture matrix

### Goal
Confirm migration behavior for old/current/malformed fixtures.

### Click-by-click steps
1. Open **Window** -> **General** -> **Test Runner**.
2. In the tab where Sprint 1 tests are listed (currently **PlayMode**), locate migration tests (e.g., `MigrationRunnerTests`).
3. Select migration fixture test group.
4. Click **Run Selected**.
5. Verify old-schema fixture path passes.
6. Verify current-schema fixture path passes.
7. Verify malformed/partial fixture path follows expected fallback behavior.
8. Capture Test Runner result screenshot and relevant Console lines.

### Pass criteria
- All migration fixture assertions pass.
- Fallback behavior for malformed fixture matches expected contract.

### Evidence required
- Test Runner screenshot for migration group.
- Log snippet showing fixture cases.

---

## UAT-05: Validate debug visibility for verification metrics

### Goal
Ensure runtime visibility for Sprint 1 verification signals.

### Click-by-click steps
1. Open bootstrap/runtime scene.
2. Click **Play**.
3. Confirm on-screen/debug overlay contains at minimum:
   - Tick/time progression indicator.
   - Mana indicator.
   - Heat indicator.
   - Save/load status line.
   - Pause/resume status line.
   - Pending verification status indicator.
4. Interact with minimal runtime loop long enough to observe value updates.
5. Capture at least one clear screenshot of overlay values.
6. Stop Play mode.

### Pass criteria
- Required indicators are visible and updating.

### Evidence required
- Screenshot(s) showing required indicators.
- Brief note confirming observed updates.

### Note on content manifest
- `content_manifest.json` should be assigned in `GameRoot` on `Bootstrap.unity`.
- Treat as FAIL if Console shows a red `[ERROR] Missing JSON asset: content_manifest`.

---

## Closeout packet template (fill during execution)
- Unity version:
- Branch/commit tested:
- Runner command(s) (if CI/batchmode used):
- Execution date/time (UTC):
- UAT-01 result:
- UAT-02 result:
- UAT-03 result:
- UAT-04 result:
- UAT-05 result:
- Artifacts location(s):
- Final signoff (name/date):

## Next sprint handoff constraint
Only begin Sprint 2 feature implementation after this file is updated with complete PASS evidence for all five UAT checks.
