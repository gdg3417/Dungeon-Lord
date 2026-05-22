# Sprint 1 UAT Evidence Index

This folder is the authoritative Sprint 1 evidence source.

## Evidence naming policy
- Naming conventions in the runbook are preferred for consistency, but they are **not mandatory** for validity.
- Evidence is validated by committed artifact content and reviewer intent, not strict filename pattern matching.

## Authoritative committed files in this folder
The following files are treated as committed Sprint 1 evidence artifacts:
- `docs/testing/evidence/sprint-1/README.md`
- `docs/testing/evidence/sprint-1/TestResults_20260521_162303.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-04_20260521.xml`
- `docs/testing/evidence/sprint-1/UAT-03 Evidence.png`
- `docs/testing/evidence/sprint-1/UAT-05 Evidence.png`

## Status and gate terms
- **UAT approval status**: `APPROVED` only when UAT-01 through UAT-05 are all PASS with required evidence.
- **Sprint 2A gate**: `UNBLOCKED` only when Sprint 1 UAT is APPROVED.
- If any required UAT evidence is missing or any UAT is not PASS, the gate is `BLOCKED`.

## UAT evidence ledger (mapped from committed files)

| UAT ID | Evidence files found in folder | Evidence status | UAT result |
|---|---|---|---|
| UAT-01 | `TestResults_20260521_162303.xml` (full Sprint 1 suite, PlayMode, 24/24 passed) | `COMMITTED` | `PASS` |
| UAT-02 | `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`, `sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`, `sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml` (SimulationDeterminismTests passed in all three runs) | `COMMITTED` | `PASS` |
| UAT-03 | `UAT-03 Evidence.png` (pause/resume overlay validation screenshot evidence) | `COMMITTED` | `PASS` |
| UAT-04 | `sprint1_uat-04_20260521.xml` (MigrationRunnerTests, 5/5 passed) | `COMMITTED` | `PASS` |
| UAT-05 | `UAT-05 Evidence.png` (debug overlay visibility screenshot evidence) | `COMMITTED` | `PASS` |

## Final Sprint 1 closeout state from committed evidence
- Sprint 1 UAT status: `APPROVED`.
- Sprint 2A gate status: `UNBLOCKED`.

## Quick reviewer instructions
1. Validate evidence using committed files in this folder first.
2. Use `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` for UAT execution steps.
3. Use `docs/planning/sprint-1-testing-runbook.md` for expected artifact requirements.
4. Accept valid committed evidence even when filenames differ from the preferred naming convention.
