# Sprint 1 UAT Evidence Index

This index is the reviewer entry point for Sprint 1 UAT evidence (UAT-01 through UAT-05).

## Status and gate terms
- **UAT approval status**: `APPROVED` only when UAT-01 through UAT-05 are all PASS with evidence linked.
- **Sprint 2A gate**: `UNBLOCKED` only when Sprint 1 UAT is APPROVED.
- If any required UAT evidence is missing or any UAT is not PASS, the gate is `BLOCKED`.

## Artifact naming conventions
- XML exports and screenshots use UTC timestamp naming from the Sprint 1 runbook.
- Pattern examples:
  - `sprint1_uat-01_runall_YYYYMMDDTHHMMSSZ.xml`
  - `sprint1_uat-02_determinism_run{1|2|3}_YYYYMMDDTHHMMSSZ.xml`
  - `sprint1_uat-03_pause-resume_YYYYMMDDTHHMMSSZ.png`
  - `sprint1_uat-04_migration_YYYYMMDDTHHMMSSZ.xml`
  - `sprint1_uat-05_debug-visibility_YYYYMMDDTHHMMSSZ.png`

## UAT evidence ledger

| UAT ID | Artifact purpose | Expected filename(s) | Evidence location type | Current reference |
|---|---|---|---|---|
| UAT-01 | Full Sprint 1 suite result export | `TestResults_20260521_162303.xml` (or `sprint1_uat-01_runall_*.xml` for future reruns) | Committed in repo | `docs/testing/evidence/sprint-1/TestResults_20260521_162303.xml` |
| UAT-02 | Determinism replay exports for run1/run2/run3 | `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`, `...run2...`, `...run3...` | Committed in repo | `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`, `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`, `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml` |
| UAT-03 | Pause/resume overlay verification screenshots | `sprint1_uat-03_pause-resume_YYYYMMDDTHHMMSSZ.png` | Commit state must be verified in current checkout | Check for `sprint1_uat-03_pause-resume_*.png` under `docs/testing/evidence/sprint-1/`; if absent, treat as missing evidence |
| UAT-04 | Migration matrix export and screenshot evidence | `sprint1_uat-04_20260521.xml` (current) or `sprint1_uat-04_migration_YYYYMMDDTHHMMSSZ.xml` (runbook convention) | XML committed; screenshot commit state must be verified | `docs/testing/evidence/sprint-1/sprint1_uat-04_20260521.xml` (plus `sprint1_uat-04_migration_*.png` if present) |
| UAT-05 | Overlay visibility verification screenshots | `sprint1_uat-05_debug-visibility_YYYYMMDDTHHMMSSZ.png` | Commit state must be verified in current checkout | Check for `sprint1_uat-05_debug-visibility_*.png` under `docs/testing/evidence/sprint-1/`; if absent, treat as missing evidence |

## Quick reviewer instructions
1. Open `Assets/_Project/Scenes/Bootstrap.unity` in Unity 6000.3.2f1.
2. Enter Play Mode and verify overlay + F1 Dev Panel interactions.
3. Confirm no Input System exception appears in Console.
4. Confirm Pause/Resume updates `Pause:` and `Tick:` lines as expected.
5. Validate UAT-01..UAT-05 evidence pointers in this index and in `docs/planning/sprint-1-evidence-template.md`.
6. Use `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` for click-by-click validation script.


## Current checkout evidence audit
- In this repo checkout on **2026-05-21 (UTC)**, the Sprint 1 evidence folder contains XML exports and no UAT screenshot image files.
- If screenshot files have been added on GitHub after this checkout, pull/sync to include them locally before final signoff.

## Completion rule for screenshot-dependent UATs
- UAT-03 is evidence-complete when at least one file matching `sprint1_uat-03_pause-resume_*.png` exists.
- UAT-04 screenshot evidence is complete when at least one file matching `sprint1_uat-04_migration_*.png` exists (XML already present in-repo).
- UAT-05 is evidence-complete when at least one file matching `sprint1_uat-05_debug-visibility_*.png` exists.
- Once UAT-01 through UAT-05 all have required evidence and PASS outcomes, set Sprint 1 UAT status to `APPROVED` and Sprint 2A gate to `UNBLOCKED`.
