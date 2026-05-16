# Sprint 1 Close-out Checklist (Execution Tracker + UAT Script)

Date: 2026-05-13  
Scope: Final evidence tracker and click-by-click UAT script required to mark Sprint 1 complete.

## Completion rule
Sprint 1 is **not closed** until every UAT item below is marked **PASS** with linked evidence.

## Status definitions (use these exact labels)
- **PASS**: Acceptance criteria met and required evidence captured.
- **PARTIAL**: Some steps executed, but required evidence and/or acceptance criteria incomplete.
- **FAIL**: Steps executed and acceptance criteria not met.
- **BLOCKED**: Could not execute due to environment/configuration issue.

## Unity 6000.3.2f1 pre-flight
1. Open **Unity Hub**.
2. In **Projects**, add/open repo root folder: `Dungeon-Lord`.
3. In Hub, confirm this project is set to **Unity Editor 6000.3.2f1** before opening.
4. Open project and wait for import/compile to finish (no spinner in lower-right).
5. In Unity Editor, verify version in **Help -> About Unity** is `6000.3.2f1`.
6. Open and keep visible: **Console**, **Project**, **Hierarchy**, **Inspector**, **Game**, **Test Runner**.
7. Create a UTC evidence folder in your artifact location.
8. Clear Console before each UAT flow.

---

## UAT-01: Run full Sprint 1 test suite from Test Runner

### Goal
Prove baseline Sprint 1 tests pass in current Unity runner configuration.

### Click-by-click steps
1. Open **Window -> General -> Test Runner**.
2. Select the tab where Sprint 1 tests are discovered. **Current repo behavior is PlayMode tab** (even though tests are stored under `Assets/_Project/Tests/EditMode`).
3. Click **Run All**.
4. Wait for execution to finish.
5. In Console, confirm no blocking errors tied to Sprint 1 systems.
6. Capture Test Runner totals screenshot.
7. Export/save test log/XML into evidence folder.

### Pass criteria
- All Sprint 1 tests discovered in current runner tab pass.
- No blocking runtime/editor errors.

### Evidence required
- Test Runner summary screenshot.
- Exported XML/log path.
- UTC timestamp + tester name.

---

## UAT-02: Run SimulationDeterminismTests 3 consecutive times

### Goal
Prove deterministic replay stability across repeated runs.

### Click-by-click steps
1. Keep **Test Runner** open.
2. Use the active Sprint 1 tab (currently **PlayMode**).
3. In search/filter, enter `SimulationDeterminismTests`.
4. Select only `SimulationDeterminismTests` group/test(s).
5. Run #1: click **Run Selected**.
6. Capture screenshot: `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.png`.
7. Export XML immediately and save as: `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`.
8. Run #2: click **Run Selected** again.
9. Capture screenshot + export XML as run2 filenames.
10. Run #3: click **Run Selected** again.
11. Capture screenshot + export XML as run3 filenames.
12. Verify all three runs passed with no divergence.

### Pass criteria
- 3/3 consecutive passes.
- No nondeterministic assertion drift.

### Evidence required
- Three run screenshots.
- Three XML exports (one per run, unique filenames).

---

## UAT-03: Validate pause/resume invariants from Bootstrap scene

### Goal
Verify pause/resume path is manually testable and does not violate runtime invariants.

### Click-by-click steps
1. In Project window, open exact scene path: `Assets/_Project/Scenes/Bootstrap.unity`.
2. In Hierarchy, select `GameRoot`.
3. In Inspector, verify these `GameRoot` fields are assigned:
   - `contentBootstrapJson`
   - `buildConfigJson`
   - `schemaVersionsJson`
   - `contentManifestJson`
   - `devCommandsJson`
   - `stringTableJson`
   - `heatRuntimeJson`
   - `overlay` (reference to `BootstrapOverlay`)
4. Enter Play Mode.
5. Confirm overlay appears in the **top-left of Game view**.
6. Confirm overlay includes lines for:
   - `Build:`
   - `State:`
   - `Pending:`
   - `Gate:`
   - `KPI:`
   - `Heat:`
   - `Tick:`
   - `Mana:`
   - `Save:`
   - `Pause:`
7. Press **F1** to open Dev Panel.
8. Click **Toggle Pause/Resume (UAT)** once; verify `Pause: Paused`.
9. Wait 10–20 seconds.
10. Click **Toggle Pause/Resume (UAT)** again; verify `Pause: Running`.
11. Confirm `Tick` continues increasing after resume and no impossible Mana/Heat jump appears.
12. Capture before/after screenshots (+ Console screenshot if warning appears).
13. Exit Play Mode.

### Pass criteria
- Pause line transitions correctly via Dev Panel control.
- Tick progression resumes without invariant break.

### Evidence required
- Before/after overlay screenshots.
- Scenario notes (elapsed wait + observed values).

---

## UAT-04: Validate save migration fixture matrix

### Goal
Confirm migration behavior for old/current/malformed fixtures.

### Click-by-click steps
1. Open **Window -> General -> Test Runner**.
2. In active Sprint 1 tab (currently **PlayMode**), locate `MigrationRunnerTests`.
3. Select migration test group.
4. Click **Run Selected**.
5. Verify old-schema/current-schema/malformed fixture cases follow expected assertions.
6. Capture Test Runner result screenshot and relevant Console lines.

### Pass criteria
- All migration assertions pass.

### Evidence required
- Test Runner screenshot.
- Log/XML path.

---

## UAT-05: Validate debug visibility for verification metrics

### Goal
Ensure Sprint 1 runtime verification signals are visible to non-developers.

### Click-by-click steps
1. Open `Assets/_Project/Scenes/Bootstrap.unity`.
2. Enter Play Mode.
3. Confirm overlay is visible/readable in top-left of Game view.
4. Confirm required lines are visible: `Tick`, `Mana`, `Heat`, `Save`, `Pause`, `Pending`.
5. Let runtime run long enough to observe updates.
6. Press **F1** to open Dev Panel and toggle at least one control (recommended: `Toggle Verification Pending`) to verify line updates.
7. Capture clear screenshot(s).
8. Exit Play Mode.

### Pass criteria
- Required indicators are visible and updating.

### Evidence required
- Screenshot(s) showing required indicators and readability.

---

## Common error interpretation
- **`[ERROR] Missing JSON asset: content_manifest`**: FAIL/BLOCKED until `GameRoot.contentManifestJson` assignment is fixed in `Bootstrap.unity`.
- **No overlay visible**: FAIL/BLOCKED for UAT-03/UAT-05; check `GameRoot.overlay` and `BootstrapOverlay.overlayText` assignments in scene.
- **No tests visible in EditMode tab**: Expected in current repo behavior; use **PlayMode** tab and record this in evidence notes.
- **No Sprint 1 tests visible in either tab**: BLOCKED; reimport scripts, check compile errors, reopen Test Runner.

## Closeout packet template (fill during execution)
- Unity version:
- Branch/commit tested:
- Execution date/time (UTC):
- UAT-01 result (PASS/PARTIAL/FAIL/BLOCKED):
- UAT-02 result (PASS/PARTIAL/FAIL/BLOCKED):
- UAT-03 result (PASS/PARTIAL/FAIL/BLOCKED):
- UAT-04 result (PASS/PARTIAL/FAIL/BLOCKED):
- UAT-05 result (PASS/PARTIAL/FAIL/BLOCKED):
- Artifacts location(s):
- Final signoff (name/date):

## Next sprint handoff constraint
Only begin Sprint 2 feature implementation after this file is updated with complete PASS evidence for all five UAT checks.
