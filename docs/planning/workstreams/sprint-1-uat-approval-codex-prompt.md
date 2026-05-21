# Prompt: Sprint 1 UAT Approval and Evidence Linkage

Use this prompt to start a new Codex workstream that finalizes Sprint 1 UAT closeout.

---

You are working in repo `Dungeon-Lord`.

Goal: set **Sprint 1 UAT status to APPROVED** and update Sprint 1 documentation with concrete evidence file paths collected on **2026-05-21**.

## Required outcomes
1. Mark Sprint 1 UAT as approved in the relevant Sprint 1 closeout documentation.
2. Add/update explicit evidence links/paths for UAT-01 through UAT-05 (where available) so reviewers can find exported artifacts quickly.
3. Keep scope documentation-only (no gameplay/runtime code changes).

## Evidence paths to include
- `docs/testing/evidence/sprint-1/TestResults_20260521_162303.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run1_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run2_YYYYMMDDTHHMMSSZ.xml`
- `docs/testing/evidence/sprint-1/sprint1_uat-02_determinism_run3_YYYYMMDDTHHMMSSZ.xml`

Also include references to the screenshot evidence supplied during UAT review (UAT-03, UAT-04, UAT-05) and note their storage location if they are not yet committed in-repo.

## Suggested docs to update
- `docs/planning/sprint-1-evidence-template.md` (fill with concrete Sprint 1 run details or add a completed example section)
- `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` (update UAT approval/gate status if still pending)
- Optionally add a concise ledger file under `docs/testing/evidence/sprint-1/` summarizing each UAT case and exact artifact paths.

## Constraints
- Do not invent files that do not exist.
- If an artifact is external (for example local screenshots), label it clearly as external and provide expected destination path.
- Preserve MVP scope constraints; no Sprint 2A implementation work.

## Acceptance criteria
- Sprint 1 UAT is explicitly marked APPROVED in the closeout doc(s).
- UAT-01..UAT-05 each have a traceable evidence pointer (in-repo path or explicit external placeholder).
- A reviewer can complete verification without searching chat history.

## Output format
- Provide a short summary of files changed.
- Provide a bullet list of UAT-01..UAT-05 evidence links.
- State final gate recommendation (`UNBLOCKED` only if evidence is complete and approvals are present).
