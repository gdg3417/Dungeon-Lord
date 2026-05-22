# Sprint 1 Evidence Template

Use this template to record Sprint 1 closeout execution evidence for UAT-01 through UAT-05.

## Test execution metadata
- Unity version:
- Branch tested:
- Commit tested:
- Test date and time (UTC):
- Tester name:

## UAT-01 result and evidence links
- Result (PASS, PARTIAL, FAIL, or BLOCKED):
- Evidence links:
- Notes:

## UAT-02 result and evidence links
- Result (PASS, PARTIAL, FAIL, or BLOCKED):
- Evidence links:
- Notes:

## UAT-03 result and evidence links
- Result (PASS, PARTIAL, FAIL, or BLOCKED):
- Evidence links:
- Notes:

## UAT-04 result and evidence links
- Result (PASS, PARTIAL, FAIL, or BLOCKED):
- Evidence links:
- Notes:

## UAT-05 result and evidence links
- Result (PASS, PARTIAL, FAIL, or BLOCKED):
- Evidence links:
- Notes:

## Open defects
- Defect ID:
- Severity:
- Summary:
- Owner:
- Status:

## Final signoff
- Signoff result (APPROVED or BLOCKED):
- Approver name:
- Approver date (UTC):
- Additional comments:

## Sprint 2A gate decision
- Gate decision (BLOCKED or UNBLOCKED):
- Decision rationale:
- Linked Sprint 1 closeout evidence:

## Latest known Sprint 1 export paths (2026-05-21)
- UAT-01 run-all export:
  - `docs/testing/evidence/sprint-1/TestResults_20260521_162303.xml`
- UAT-02 determinism exports:
  - `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`
  - `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`
  - `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml`
- UAT-03/UAT-04/UAT-05 screenshots:
  - Expected under `docs/testing/evidence/sprint-1/` using Sprint 1 naming conventions from the runbook.
  - If screenshots are hosted outside the repo, add direct URL(s) and owner/location details before signoff.


## Verification wording standard (Sprint 1)
Use these exact verification labels in Sprint 1 closeout docs:
- UAT case IDs: `UAT-01` through `UAT-05`.
- UAT case outcomes: `PASS`, `PARTIAL`, `FAIL`, `BLOCKED`.
- Sprint 1 UAT approval: `APPROVED` when all five UAT cases are PASS and evidence is linked; otherwise `BLOCKED`.
- Sprint 2A gate state: `UNBLOCKED` only when Sprint 1 UAT is APPROVED; otherwise `BLOCKED`.

## Sprint 1 evidence index
Primary evidence index for reviewers:
- `docs/testing/evidence/sprint-1/README.md`


## Current committed evidence snapshot (authoritative folder scan)
- Source folder: `docs/testing/evidence/sprint-1/`
- UAT-01: `COMMITTED`
- UAT-02: `COMMITTED`
- UAT-03: `MISSING` (no screenshot file committed in folder)
- UAT-04: `MISSING` for screenshot evidence (XML export is committed)
- UAT-05: `MISSING` (no screenshot file committed in folder)

Use `docs/testing/evidence/sprint-1/README.md` as the source-of-truth ledger for exact committed file names.
