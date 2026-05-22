# Sprint 1 UAT Evidence Index

This folder is the authoritative Sprint 1 evidence source.

## Authoritative committed files in this folder
The following files are currently committed under `docs/testing/evidence/sprint-1/`:
- `docs/testing/evidence/sprint-1/README.md`
- `docs/testing/evidence/sprint-1/TestResults_20260521_162303.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-04_20260521.xml`

## Status and gate terms
- **UAT approval status**: `APPROVED` only when UAT-01 through UAT-05 are all PASS with required evidence.
- **Sprint 2A gate**: `UNBLOCKED` only when Sprint 1 UAT is APPROVED.
- If any required UAT evidence is missing or any UAT is not PASS, the gate is `BLOCKED`.

## UAT evidence ledger (mapped from actual committed files)

| UAT ID | Evidence files found in folder | Evidence status | UAT result |
|---|---|---|---|
| UAT-01 | `TestResults_20260521_162303.xml` (full Sprint 1 suite, PlayMode, 24/24 passed) | `COMMITTED` | `PASS` |
| UAT-02 | `sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`, `sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`, `sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml` (SimulationDeterminismTests passed in all three runs) | `COMMITTED` | `PASS` |
| UAT-03 | No committed screenshot or equivalent artifact proving pause/resume overlay validation | `MISSING` | `BLOCKED` |
| UAT-04 | `sprint1_uat-04_20260521.xml` (MigrationRunnerTests, 5/5 passed) | `COMMITTED` | `PASS` |
| UAT-05 | No committed screenshot or equivalent artifact proving debug overlay visibility validation | `MISSING` | `BLOCKED` |

## Final Sprint 1 closeout state from current committed evidence
- Sprint 1 UAT status: `BLOCKED`.
- Sprint 2A gate status: `BLOCKED`.
- Reason: UAT-03 and UAT-05 evidence remains missing in committed folder contents.

## Quick reviewer instructions
1. Validate evidence using committed files in this folder first.
2. Use `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` for UAT execution steps.
3. Use `docs/planning/sprint-1-testing-runbook.md` for expected artifact requirements.
4. Do not mark Sprint 1 as `APPROVED` until UAT-01 through UAT-05 each have required evidence and PASS outcomes.
