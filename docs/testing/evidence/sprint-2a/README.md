# Sprint 2A Evidence

- PR-A foundation completed.
- PR #19 added deterministic slot/layout placement foundation.
- PR #20 fixed save model integration.
- Unity Safe Mode cleared.
- PlacementDeterminismTests passed 8/8.
- Evidence file location: `docs/testing/evidence/sprint-2a/pr-a/S2-T00A-I01_PlacementDeterminismTests_PASS.xml`.

## PR-D UAT Checklist (Heat Crisis Lockout)

1. Place `structure.mana_generator.basic`, `structure.heat_scrubber.basic`, and `structure.risk_lab.basic`.
2. Run structure ticks until heat crisis enters.
3. Verify placement is blocked during crisis.
4. Verify risk lab is paused by crisis.
5. Continue running ticks with heat scrubber active until crisis recovers.
6. Verify placement is allowed again after recovery.
7. Save, exit play mode, re-enter play mode, and verify crisis/runtime state persisted.

## PR-D Evidence (Closeout)

- PR number: **#24**
- Feature: **Heat Crisis Lockout behavior and recovery loop**
- EditMode tests passed:
  - `StructureSimulationTests`
  - `PlacementDeterminismTests`
- Manual UAT passed:
  - Placed mana generator, heat scrubber, and risk lab.
  - Ran structure ticks until Heat Crisis became active.
  - Confirmed placement was blocked during Heat Crisis.
  - Continued ticking until Heat Crisis recovered.
  - Confirmed placement was allowed again after recovery.
  - Saved, exited Play Mode, re-entered Play Mode, and confirmed runtime state persisted.
- Evidence screenshots were captured manually by tester.
- Sprint 2A PR-D status: **PASS**
