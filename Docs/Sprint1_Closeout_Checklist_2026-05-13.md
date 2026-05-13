# Sprint 1 Close-out Checklist (S1-08)

Date: 2026-05-13  
Scope: Execute Sprint 1 close-out checklist and record readiness status.

## Checklist results

1. **Run full EditMode test suite**  
   **Status:** ⚠️ Blocked in this environment. Unity Editor / batchmode runner is not available in the container session, so EditMode tests cannot be executed here.

2. **Run determinism fixture multiple times**  
   **Status:** ⚠️ Blocked in this environment for the same reason (Unity EditMode runner unavailable).

3. **Validate pause/resume + offline elapsed path does not break determinism guarantees**  
   **Status:** ⚠️ Pending runtime verification in Unity PlayMode/EditMode automation.

4. **Validate save migration roundtrip on fixture matrix**  
   **Status:** ✅ Implemented at test level via fixture coverage in `MigrationRunnerTests` (`old schema`, `current schema`, `malformed`). Execution still pending Unity test runner availability.

5. **Confirm debug panel displays mana/heat/tick clearly**  
   **Status:** ✅ Implemented in code:
   - KPI line (mana-related) already present.
   - Heat line now displayed in overlay.
   - tick telemetry/event wiring in place.

## Sprint 1 completion gate summary

### Completed implementation items
- Formula engine contract coverage expanded.
- Mana + heat deterministic simulation components implemented.
- Tick-level replay harness added.
- Save migration fixture coverage expanded.
- Heat runtime config wiring added with defaults and non-fatal warnings.

### Remaining blockers to mark Sprint 1 as complete
1. Execute EditMode test suite in Unity runner and archive results.
2. Execute deterministic replay test repeatedly and archive pass logs.
3. Run targeted pause/resume skew/offline path verification (automated or scripted manual test) and document outcomes.

## Suggested immediate next action
Run Unity EditMode tests in CI or on a workstation with Unity installed, then update this checklist with exact command logs and pass/fail artifacts.
