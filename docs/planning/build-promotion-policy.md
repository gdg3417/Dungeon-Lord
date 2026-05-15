# Build Promotion Policy for Sprint Governance

Date: 2026-05-15
Scope: Planning governance for Unit Testing, SIT, UAT, and build gate evidence across Sprint 1 through Sprint 4.

## 1. When a branch can become a PR
A branch can become a PR only when:
1. Ticket acceptance criteria for included scope are marked implemented.
2. Required Unit and SIT checks for the included ticket set are PASS.
3. Evidence links are attached for each completed check.
4. Open defects are triaged with severity and disposition.

For documentation-only planning PRs, Unit and SIT evidence may be marked Not Applicable when no executable code, game content, schemas, or build configuration changed. Markdown validation and human review evidence are still required.

## 2. When a PR can merge
Implementation PRs must satisfy docs/development/codex-development-rules.md before merge.

A PR can merge only when:
1. Required checks in CI are green.
2. Required governance checklist items for the active sprint are PASS.
3. No unresolved blocker defects exist for included ticket scope.
4. Build gate items marked blocker are all PASS.

For documentation-only planning PRs, Unit and SIT evidence may be marked Not Applicable when no executable code, game content, schemas, or build configuration changed. Markdown validation and human review evidence are still required.

## 3. When a sprint can close
A sprint can close only when:
1. Sprint closeout checklist is complete.
2. Required Unit, SIT, UAT, and Build Gate evidence artifacts are linked.
3. Any conditional checks are either PASS or marked Not Applicable with justification.
4. Sprint signoff fields include approver name and date.

## 4. When a build can be promoted to internal test
Internal test promotion requires:
1. Unit checks PASS for implemented tickets.
2. SIT checks PASS for cross-system flows.
3. No known blocker defects in implemented sprint scope.
4. Build gate blocker checks required for internal test are PASS.

## 5. When a build can become a release candidate
Release candidate promotion requires:
1. Internal test promotion requirements already satisfied.
2. UAT checks PASS for all player-facing scope included in the build.
3. Release-oriented build gates PASS.
4. Warning-only findings are documented with owner and resolution date.

## 6. Required evidence by stage
- Unit Testing evidence: test logs, deterministic replay outputs, and failing-case resolution links.
- SIT evidence: integrated scenario traces, state snapshots, and cross-system assertions.
- UAT evidence: scripted walkthrough notes, screenshots or recordings, and pass or fail dispositions.
- Build Gate evidence: CI job links, dashboards, gate decision records, and signoff entries.

## 7. Warning versus blocker rules
- Blocker: missing required evidence for blocker-designated checks, failed deterministic or integrity checks, failed release governance checks.
- Warning only: non-critical polish findings that do not invalidate deterministic correctness, integrity, or player trust gates.
- Not applicable: allowed only with explicit written reason and reviewer signoff.

## 8. Cross-sprint dependency gates
- Missing Sprint 1 closeout evidence blocks Sprint 2 feature execution.
- Missing Sprint 2 closeout evidence blocks Sprint 3 hardening claims.
- Missing Sprint 3 closeout evidence blocks Sprint 4 polish claims.
- Missing Sprint 4 closeout evidence blocks MVP release candidate claims.

## 9. Policy references
- `Docs/Sprint1_Closeout_Checklist_2026-05-13.md`
- `docs/planning/test-stage-matrix.md`
- `docs/planning/sprint-2-closeout-checklist.md`
- `docs/planning/sprint-3-closeout-checklist.md`
- `docs/planning/sprint-4-closeout-checklist.md`
