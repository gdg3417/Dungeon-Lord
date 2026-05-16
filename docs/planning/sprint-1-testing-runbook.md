# Sprint 1 Testing Runbook

## 1. Purpose
This runbook defines how to execute Sprint 1 UAT-01 through UAT-05 on a Unity-capable machine and how to capture evidence needed for Sprint 1 closeout.

This document is execution scaffolding only. It does not mark Sprint 1 complete and does not authorize Sprint 2A start by itself.

## 2. Prerequisites
- Access to a Unity-capable workstation with the project checked out.
- Access to the agreed artifact storage location for screenshots and logs.
- Access to the Sprint 1 closeout checklist and this runbook during execution.
- Clean working tree for the branch under test.
- Tester assigned and available for full UAT sequence.

## 3. Unity setup checklist
1. Open Unity Hub.
2. Open this repository project using the agreed Unity Editor version for the branch.
3. Wait for import/compilation to fully finish.
4. Open and keep visible: Console, Project, Test Runner, and Game windows.
5. Clear Console before each UAT flow where useful to reduce noise.
6. Create a UTC-timestamped evidence folder before first test execution.
7. Record Unity version, branch, and commit in the evidence template before running tests.

## 4. Exact execution steps for UAT-01 through UAT-05

### UAT-01: Run full Unity EditMode suite
1. In Unity, open Window -> General -> Test Runner.
2. In Test Runner, select EditMode.
3. Click Run All.
4. Wait for completion.
5. Verify no unexpected blocking errors in Console tied to Sprint 1 scope.
6. Capture Test Runner summary screenshot.
7. Save or export logs to evidence folder.

Pass expectation:
- Required Sprint 1 EditMode tests pass with no blocking Sprint 1 console exceptions.

### UAT-02: Run deterministic replay test 3 consecutive times
1. Keep Test Runner open in EditMode.
2. Locate determinism tests (for example, SimulationDeterminismTests group).
3. Select only determinism test(s).
4. Run Selected for run 1.
5. Capture result evidence.
6. Run Selected for run 2.
7. Capture result evidence.
8. Run Selected for run 3.
9. Capture result evidence.
10. Verify all 3 runs pass with no checkpoint divergence.

Pass expectation:
- Three consecutive passes and no nondeterministic drift.

### UAT-03: Validate pause/resume plus offline elapsed invariants
1. Open bootstrap runtime scene (for example, Bootstrap.unity).
2. Enter Play mode.
3. Note baseline values for visible time or tick, mana, and heat indicators.
4. Pause Play mode.
5. Wait about 10 to 20 seconds.
6. Resume Play mode.
7. Verify values continue without invalid spikes or invariant breaks.
8. Exit Play mode.
9. Re-enter Play mode and execute the project-approved offline elapsed debug or test path.
10. Verify elapsed handling obeys expected caps and guardrails.
11. Capture before and after evidence.

Pass expectation:
- Pause/resume and offline elapsed behavior stay within expected invariants.

### UAT-04: Validate save migration fixture matrix
1. Open Window -> General -> Test Runner.
2. In EditMode, locate migration fixture tests (for example, MigrationRunnerTests group).
3. Select migration group.
4. Click Run Selected.
5. Verify old-schema fixture path passes.
6. Verify current-schema fixture path passes.
7. Verify malformed or partial fixture path follows expected fallback behavior.
8. Capture Test Runner and relevant Console evidence.

Pass expectation:
- Migration assertions pass for old/current fixtures and malformed fallback behavior matches contract.

### UAT-05: Validate debug visibility for verification metrics
1. Open bootstrap runtime scene.
2. Enter Play mode.
3. Confirm visible indicators include tick or time progression, mana, heat, and pending verification status indicator if present in Sprint 1 scope.
4. Let runtime update long enough to confirm indicators are changing.
5. Capture screenshot(s).
6. Exit Play mode.

Pass expectation:
- Required indicators are visible and updating.

## 5. Evidence capture instructions
- Capture UTC timestamps for each UAT result entry.
- Capture at least one screenshot per UAT item, plus additional screenshots where required by the checklist.
- Preserve raw test logs for UAT-01, UAT-02, and UAT-04.
- Add short notes for setup, inputs, and observed outcomes.
- Store all artifact paths in the evidence template and closeout checklist.

## 6. Expected artifact naming conventions
Use ASCII-only names and UTC timestamps:
- `sprint1_uat-01_editmode_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-01_editmode_YYYYMMDDTHHMMSSZ.log`
- `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-03_pause-resume_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-03_offline-elapsed_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-04_migration_YYYYMMDDTHHMMSSZ.png`
- `sprint1_uat-04_migration_YYYYMMDDTHHMMSSZ.log`
- `sprint1_uat-05_debug-visibility_YYYYMMDDTHHMMSSZ.png`

## 7. Troubleshooting notes for common failures
- Unity import or compile still running:
  - Wait for full completion before starting tests.
- Test Runner hangs or stalls:
  - Re-open Test Runner, rerun selected scope, and capture Console output.
- Determinism test intermittent failure:
  - Re-run exact same selected test group and document run-to-run differences.
- Missing debug indicators in UAT-05:
  - Confirm correct scene and debug display path for Sprint 1 scope.
- Migration fixture failure:
  - Confirm fixture set is current for old/current/malformed cases and capture exact failing assertion.

## 8. Pass/fail decision rules
- PASS: All acceptance conditions for UAT-01 through UAT-05 are met and evidence is complete, readable, and linked.
- FAIL: Any required UAT item fails, is not executed, or lacks required evidence artifacts.
- INCOMPLETE: Execution started but evidence packet lacks required fields, links, or signoff.

A Sprint 1 closeout decision requires PASS disposition for all five UAT checks, plus linked evidence.

## 9. Sprint 2A dependency gate
Sprint 2A cannot start until Sprint 1 closeout evidence for UAT-01 through UAT-05 is complete and linked in closeout records.

## 10. Required policy and checklist links
- Sprint 1 closeout checklist: `Docs/Sprint1_Closeout_Checklist_2026-05-13.md`
- Build promotion policy: `docs/planning/build-promotion-policy.md`
