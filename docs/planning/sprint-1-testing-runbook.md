# Sprint 1 Testing Runbook

## 1. Purpose
This runbook defines how to execute Sprint 1 UAT-01 through UAT-05 on a Unity-capable machine and capture evidence for closeout.

This runbook does not itself close Sprint 1 and does not authorize Sprint 2A start.

## 2. Environment and prerequisites
- Unity Editor version: **6000.3.2f1**.
- Repo checked out locally.
- Clean working tree for branch under test.
- Access to artifact storage for screenshots/XML/logs.
- Tester available for full UAT sequence.

## 3. Open project and verify Unity version
1. Open Unity Hub.
2. In **Projects**, add/open the repo root folder: `Dungeon-Lord`.
3. Ensure editor selection for this project is **6000.3.2f1**.
4. Open project and wait until import/compile fully completes.
5. In Unity, verify **Help -> About Unity** shows `6000.3.2f1`.
6. Open windows: **Console, Project, Hierarchy, Inspector, Game, Test Runner**.
7. Create evidence folder (UTC timestamp) and record branch + commit + Unity version.

## 4. Current test discovery behavior (important)
- Source test files are located under `Assets/_Project/Tests/EditMode`.
- In current repo behavior, Sprint 1 tests are discovered in **Test Runner PlayMode tab**.
- If EditMode is empty, this is expected for this branch; run Sprint 1 tests from PlayMode and note that in evidence.

## 5. UAT execution steps

### UAT-01: Run full Sprint 1 suite
1. Open **Window -> General -> Test Runner**.
2. Select Sprint 1 test tab (**PlayMode** currently).
3. Click **Run All**.
4. Wait for completion.
5. Capture Test Runner summary screenshot.
6. Export XML/log and store in evidence folder.

Pass expectation:
- All discovered Sprint 1 tests pass and no blocking Console errors.

### UAT-02: Determinism replay (3 consecutive runs)
1. In Test Runner (PlayMode), search `SimulationDeterminismTests`.
2. Select only determinism tests.
3. Run Selected (Run 1), screenshot results.
4. Export XML immediately with unique filename: `...run1...xml`.
5. Repeat for Run 2 and Run 3 using unique screenshot and XML filenames.
6. Confirm all 3 runs pass and no drift assertions appear.

Pass expectation:
- 3/3 consecutive determinism passes with preserved per-run evidence.

### UAT-03: Pause/resume invariants via Dev Panel
1. Open scene: `Assets/_Project/Scenes/Bootstrap.unity`.
2. In Hierarchy select `GameRoot`.
3. In Inspector verify assigned fields:
   - `contentBootstrapJson`, `buildConfigJson`, `schemaVersionsJson`, `contentManifestJson`, `devCommandsJson`, `stringTableJson`, `heatRuntimeJson`, `overlay`.
4. Enter Play Mode.
5. Confirm overlay is visible in top-left Game view.
6. Confirm lines are present: `Build`, `State`, `Pending`, `Gate`, `KPI`, `Heat`, `Tick`, `Mana`, `Save`, `Pause`.
7. Press **F1** to show Dev Panel.
8. Click **Toggle Pause/Resume (UAT)** -> expect `Pause: Paused`.
9. Wait ~10–20s.
10. Click **Toggle Pause/Resume (UAT)** again -> expect `Pause: Running`.
11. Confirm Tick resumes increasing; no invalid value spikes.
12. Capture before/after screenshots.

Pass expectation:
- Pause line transitions correctly and runtime invariants stay intact.

### UAT-04: Migration matrix
1. In Test Runner (PlayMode), locate `MigrationRunnerTests`.
2. Run Selected on migration group.
3. Confirm old/current/malformed fixture assertions pass.
4. Capture screenshot and export XML/log.

Pass expectation:
- Migration tests pass for expected fixture matrix.

### UAT-05: Debug visibility
1. Open `Assets/_Project/Scenes/Bootstrap.unity` and enter Play.
2. Confirm readable overlay and required lines: `Tick`, `Mana`, `Heat`, `Save`, `Pause`, `Pending`.
3. Let values update naturally.
4. Press F1 and toggle one Dev Panel action to show observable line update.
5. Capture screenshot(s).

Pass expectation:
- Required indicators are visible and update in Play Mode.
- `Mana` line is a Sprint 1 debug proxy (current KPI average mana per tick), not persisted save mana.

## 6. Required evidence artifacts
- UAT-01: Run-all screenshot + XML/log.
- UAT-02: 3 screenshots + 3 XML files (no overwrite).
- UAT-03: Before/after overlay screenshots (+ warning screenshot if applicable).
- UAT-04: Migration run screenshot + XML/log.
- UAT-05: Overlay readability screenshot(s).

Suggested naming (UTC):
- `sprint1_uat-01_runall_YYYYMMDDTHHMMSSZ.*`
- `sprint1_uat-02_determinism_run{1|2|3}_YYYYMMDDTHHMMSSZ.*`
- `sprint1_uat-03_pause-resume_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-04_migration_YYYYMMDDTHHMMSSZ.*`
- `sprint1_uat-05_debug-visibility_YYYYMMDDTHHMMSSZ.png`

## 7. PASS / PARTIAL / FAIL / BLOCKED definitions
- **PASS**: Acceptance criteria met + evidence complete.
- **PARTIAL**: Execution attempted, but evidence incomplete or criteria partly met.
- **FAIL**: Criteria not met.
- **BLOCKED**: Cannot execute due to environment/configuration problem.

## 8. Troubleshooting and interpretation
- `[ERROR] Missing JSON asset: content_manifest`
  - Treat as FAIL/BLOCKED for closeout.
  - Fix `GameRoot.contentManifestJson` assignment in `Bootstrap.unity`.
- Overlay missing/unreadable
  - Treat UAT-03/UAT-05 as BLOCKED.
  - Verify `GameRoot.overlay` and `BootstrapOverlay.overlayText` assignments.
- Tests only appear in PlayMode
  - Expected on this branch; run there and annotate evidence.
- No tests appear in any tab
  - BLOCKED. Resolve compile/import errors and reopen Test Runner.

## 9. Gate reminder
Sprint 2A must not start until UAT-01..UAT-05 are PASS with linked evidence.

## 10. Required links
- Sprint 1 closeout checklist: `Docs/Sprint1_Closeout_Checklist_2026-05-13.md`
- Build promotion policy: `docs/planning/build-promotion-policy.md`
