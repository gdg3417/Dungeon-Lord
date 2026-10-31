# H1-F4 draft implementation review checkpoint — 2026-10-27

Branch `codex/h1-f4-recovery`, targeting `main`. Original and remotely checked
main base: `3eb6ccf93558b8145d64e9e63cbefcbd73484602` (PR #258).
The owner explicitly authorizes this unfinished draft before full implementation
or passing all automated gates. Subsequent work continues on this same branch/PR.
This authorization changes draft timing, not recovery semantics or merge/UAT gates.

Review 1 milestone: the ordinary-save foreground-stall defect was reproduced and
corrected, and the affected recovery suite passed 107/107 at runtime/test source
`5c52f301bbe919611966347ef15dfe3640cfd358`. See the
[source-pinned correction and remaining blockers](h1-f4-review1-ordinary-save-correction-20261027.md).
The last full EditMode 805-failure report remains unresolved.
The subsequent fixture-only source 9319d4bc passed the explicit 243-case recovery,
structural-preview and canonical-write cohort, including 87 previously failing
cases. This is still not a full-suite pass.
The latest authority/Research source b9e455a4 passed 268/268, with all 21 Research
cases and additional dormant/reentrant ordinary-publication coverage. See the
review-1 report for the reproduced runtime defects and preserved intermediate failures.

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
- Runtime corrections refuse ordinary reentrant saves during an active canonical
  publication and keep known foreground save stalls behind explicit retry. The
  reviewed-head 105-case and corrected 107-case suites passed with preservation.

These are implementation descriptions, not full correctness or release claims.

## Source-pinned qualification and failures

| Source | Campaign | Actual result |
| --- | --- | --- |
| c74f8a989501a56ab73cb11d6348bcb32de21fb7 | 1d496e09c71944d6be592056b99c3d86 | Focused 102 passed, 0 failed, 0 skipped; all stages passed |
| 16955152fcdd5a5f8af006dc38887a75f1f83266 | 61c2a2cd452e4a5c837496834f680e55 | Raw full EditMode 1966 passed, 805 failed, 1 skipped; campaign INCOMPLETE/UNCERTAIN |
| d55139dacc338c08636bf607991c86d1b474297a | 09e4ca62452a4a6ba781debd85766018 | Focused 104 passed, 0 failed, 0 skipped; all stages passed |
| 57096bebfc930595668dba372ae84b832007a8a8 | 19fdd2a03134484191251af755dc047d | Focused 104 passed, 1 failed, 0 skipped; terminal FAILED, verified exit, separately verified restoration/preservation |
| eb95f2506b9982e675c5c53adc5545c1fc133205 | f87a3cd4f3594da0a320c88ad1054d19 | Focused 105 passed, 0 failed, 0 skipped; all stages passed |
| 5c52f301bbe919611966347ef15dfe3640cfd358 | 6e05f8f9c07c46b4aacd601a1d754051 | Focused 107 passed, 0 failed, 0 skipped; all stages passed |

Ten preparation-tool unit tests passed at each of the two latest correction stages.
They are tooling checks, not gameplay/Unity qualification. No passing result is
relabelled for a newer commit. The review-1 report also retains the intermediate
105/106 defect reproduction and 106/107 assertion-failure campaigns as FAILED.

## Unresolved work and review priorities

1. Broaden the qualified P0 stall/reentrancy and authority-resolution paths through
   accepted work, lifecycle persistence and the remaining protocol-cut matrix.
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
