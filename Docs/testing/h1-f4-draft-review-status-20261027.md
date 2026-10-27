# H1-F4 draft implementation review checkpoint — 2026-10-27

Branch `codex/h1-f4-recovery`, targeting `main`. Original and remotely checked
main base: `3eb6ccf93558b8145d64e9e63cbefcbd73484602` (PR #258).
The owner explicitly authorizes this unfinished draft before full implementation
or passing all automated gates. Subsequent work continues on this same branch/PR.
This authorization changes draft timing, not recovery semantics or merge/UAT gates.

## Current implementation

- Successor schema 17 is active in source; schema-16 validation/resource dispatch
  and historical serializers remain separately frozen. Unproven historical A>C
  activation fails closed without assuming a producer from schema/defaults.
- Approved format-2 bounded episode, persistent twelve-input context-v3 digest,
  exact retained validated bootstrap bytes (including CombatC0), 590-byte owner
  envelope and expanded resource ceilings are implemented and have focused tests.
- Common predecessor-bound canonical transitions retain W through ordinary saves;
  recovery entry retains R, drains accepted A>C, settles original and grouped
  residual mana/monster effects, then closes the episode atomically at S.
- TimeService owns monotonic S/Q eligibility and L1 lifecycle observations.
  Boot/lifecycle and native-creation paths are integrated and under qualification.
- The latest runtime correction refuses ordinary reentrant saves during an active
  canonical publication. Its test's write-count assertion was corrected to account
  for the established rollback-plus-candidate protocol and still needs a fresh run.

These are implementation descriptions, not full correctness or release claims.

## Source-pinned qualification and failures

| Source | Campaign | Actual result |
| --- | --- | --- |
| c74f8a989501a56ab73cb11d6348bcb32de21fb7 | 1d496e09c71944d6be592056b99c3d86 | Focused 102 passed, 0 failed, 0 skipped; all stages passed |
| 16955152fcdd5a5f8af006dc38887a75f1f83266 | 61c2a2cd452e4a5c837496834f680e55 | Raw full EditMode 1966 passed, 805 failed, 1 skipped; campaign INCOMPLETE/UNCERTAIN |
| d55139dacc338c08636bf607991c86d1b474297a | 09e4ca62452a4a6ba781debd85766018 | Focused 104 passed, 0 failed, 0 skipped; all stages passed |
| 57096bebfc930595668dba372ae84b832007a8a8 | 19fdd2a03134484191251af755dc047d | Focused 104 passed, 1 failed, 0 skipped; terminal FAILED, verified exit, separately verified restoration/preservation |

Ten preparation-tool unit tests passed at each of the two latest correction stages.
They are tooling checks, not gameplay/Unity qualification. The assertion correction
included at this draft checkpoint has not been rerun. No passing result is relabelled
for a newer commit.

## Unresolved work and review priorities

1. Requalify the reentrancy guard and investigate the source-backed ordinary-save
   failure/deferred-time branch. Known foreground stalls must never become offline
   mana or trigger per-frame storage retries.
2. Resolve the 805 full-suite failures: shared first-write/context fixtures, actual
   lifecycle and Research regressions, four null references, explicit historical
   versus current-target assertions and remaining behavioral failures. A passing
   focused gate does not clear these failures. Do not relax strict validation.
3. Complete affected Unit/SIT and full EditMode regressions, active v1/v2 historical
   proof/refusal cases, complete protocol-cut/repeated-crash qualification and
   ordinary/native/lifecycle/Research save-close-reopen scenarios.
4. Complete PlayMode and isolated Windows Development Player qualification, ordinary
   and abrupt shutdown/reopen, exact process/descendant completion and preservation.
   The known intermittent native Windows shutdown issue remains separately open.
5. Run the mandatory three-way performance comparison: retained pre-optimization
   3040ddc4b87b0f70eac4046c3a7a4c48c5ec12da evidence, fresh main 3eb6ccf..., and
   final qualified H1-F4 source. No fresh comparison or speedup claim exists yet.
6. Prepare the normal isolated owner-UAT handoff only after applicable automated
   gates pass and independent review authorizes starting owner UAT.

No new policy decision is requested. No owner UAT, merge, H1-F5, H2 or O1 begins.
H2 batching, offline adventurer processing, new tuning and unrelated UI are excluded.

## Preservation and evidence

Both historical incomplete campaigns remain INCOMPLETE/UNCERTAIN permanently.
Their separately approved recoveries do not prove missing descendant evidence.
The most recent four-file recovery was independently verified complete; its
[receipt](h1-f4-fulledit-recovery-receipt-20261027.json) includes exact preserved and
restored hashes and released reservation ownership. No process termination,
force unlock, supervisor-rule change or evidence deletion occurred.

The managed checkout is the only project checkout edited. Owner saves, normal
checkout/planning changes, historical fixtures/builds and stopped campaigns remain
protected by the exact preservation authorities. The current reservation is released;
the last failed campaign's temporary settings were restored and final checks passed.
Raw TestResults campaigns, logs, build outputs, generated assets, Library/Temp and
temporary ProjectSettings are not included in this draft. Existing versioned design
evidence, compatibility test fixtures and sanitized reports remain intentional docs.

Review the [approved design](../planning/h1-f4-recovery-design-gate.md),
[dependency audit](h1-f4-context-v2-dependency-audit.md), and
[prioritized failure analysis](h1-f4-editmode-failure-analysis-20261027.md).
The GitHub PR base/head SHAs identify this exact review checkpoint.

Model recommendation: GPT-6.1 Sol High, Complex implementation under approved
authority; availability fallback GPT-6 Sol High. No Astra escalation or new design
approval cycle was introduced at this checkpoint.
