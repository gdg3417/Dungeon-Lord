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

## UAT evidence ledger (actual committed artifacts)

| UAT ID | Required artifact(s) | Committed file evidence | Evidence status |
|---|---|---|---|
| UAT-01 | Run-all test export (XML/log) | `docs/testing/evidence/sprint-1/TestResults_20260521_162303.xml` | `COMMITTED` |
| UAT-02 | Determinism replay run1/run2/run3 exports | `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`, `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`, `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml` | `COMMITTED` |
| UAT-03 | Pause/resume overlay screenshot evidence | No matching screenshot file committed in this folder | `MISSING` |
| UAT-04 | Migration export + migration screenshot evidence | XML committed: `docs/testing/evidence/sprint-1/sprint1_uat-04_20260521.xml`; no matching migration screenshot file committed in this folder | `MISSING` |
| UAT-05 | Debug visibility overlay screenshot evidence | No matching screenshot file committed in this folder | `MISSING` |

## Quick reviewer instructions
1. Validate evidence using committed files in this folder first.
2. Use `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` for UAT execution steps.
3. Use `docs/planning/sprint-1-testing-runbook.md` for expected artifact requirements.
4. Do not mark Sprint 1 as `APPROVED` until UAT-01 through UAT-05 each have required evidence and PASS outcomes.
