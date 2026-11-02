# H1-F4 Review 1 — recovery receipt and 356-case qualification

Branch `codex/h1-f4-recovery`, draft PR #259. Base remains
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. The tested correction is
`233d3fbe7516c542d1b1c570e8cad0afa8ba2048`: replace unavailable NUnit `Is.AnyOf`
with the supported equality/or constraint. This one-line test-only correction
changes no runtime, configuration, gameplay tuning, validator or frozen serializer.
Recommended model remains GPT-6.1 Sol High, Complex; GPT-6 Sol High fallback.

## Exceptional recovery completed and independently verified

The owner approved the exact revised script and diagnostic for campaign
`80df1a3213bb4a93b3182030bdd754ad` at source
`eaec790f58ddf8248353c529afad90423a6d9d2d`. Script SHA-256:
`127e4e9ecf5889e34410be594400413a6d12b05ab30478922bec025fa42b713d`;
diagnostic `501e506c7631655a1cd22cf3d83bd7f1251033e552013505385f28892dd58488`;
guard proof `541b7be6bb85d4db72ee6d179095451c19e739fd2848dc43d5d85b621ccb60b6`.

Receipt UTC `2026-10-27T23:32:08.930984+00:00`: phase
`post-release-verification`, prechecks/postchecks/success/released all true.
Only the three authorized archived settings were restored after preserving and
hash-verifying all three current versions. TMP font was unchanged. Existing Lease
ownership spanned restoration and release; the locked range was not separately
read. No supervisor rule changed, process was terminated or evidence deleted.

Independent verification at `2026-10-27T23:33:34.189101+00:00` passed all 19 checks:
all 1,494 original inputs, exact restored hashes, current-version preservation,
font, campaign/prior receipt/historical/protected manifests and actual released
reservation ownership. Receipt SHA-256:
`3a636bc635c569e36e313ab99bbfa9111135d6b613406b88f0e3609c81d620ed`.
The [sanitized receipt and verification](h1-f4-living-recovery-receipt-20261027.json)
retain all actual fields. Raw directory:
`TestResults/h1-f4-20261027-living01/owner-approved-recovery02`.

The original `80df...` campaign remains **INCOMPLETE / UNCERTAIN permanently**.
The earlier failed `owner-approved-recovery01` receipt and all old evidence are
unchanged. Later process absence never establishes historical descendant completion.
The [original blocker](h1-f4-living-qualification-blocker-20261027.md) is retained
as the historical pre-approval proposal, not a statement of current recovery status.

## Fresh 356-case affected qualification — FAILED

Campaign `d8bd16dcfa604edfb082e525ccf91038`, preparation `living02`, tested exactly
`233d3fbe7516c542d1b1c570e8cad0afa8ba2048`. Unity 6000.3.2f1, CLI beta.14,
nine pinned tools. Product `Dungeon Lord B3B2 Smoke H1F4 20261027 living02`;
isolated save namespace under `AppData/LocalLow/gdg3417` with that product name.
Historical inputs and owner state were protected. No full-suite, Windows or
benchmark workload was launched.

**356 total: 310 passed, 46 failed, 0 skipped, 0 inconclusive.**

| Selected cohort | Passed | Failed |
| --- | ---: | ---: |
| H1-F4 recovery contract | 111 | 0 |
| Structural preview | 77 | 0 |
| Canonical write | 59 | 0 |
| Research durability | 18 | 0 |
| Research save integration | 3 | 0 |
| One-floor B3B2 | 24 | 27 |
| Two-floor | 18 | 19 |

Compilation passed; the isolated seed exporter also passed, retaining six files
under the fresh campaign's `living-regressions/fixture-inputs/two-floor` directory.
They are test artifacts, not qualified Player inputs or owner-UAT builds.
Historical input paths were unchanged.

Preflight PASSED, workload FAILED (`CHILD_NONZERO`); the three planned subsequent
restoration/final stages correctly remained NOT_STARTED. The unchanged supervisor
recorded verified owned completion: host exit 0, runner/CLI exit 8, Job
`Local\DLQ-5b9081f5b40f465bafc5b2f3b37c30c4`. Host PID 53044, creation FILETIME
134376177221879357; runner PID 34664, creation FILETIME 134376177223025091.
Exact identities, result/exit records and evidence hashes are in the
[source-pinned report](h1-f4-review1-living-qualification-20261027.json).

Raw NUnit root is `Failed(Child)`, with an end time and all 356 unique leaf IDs
accounted for. The supervisor's passing-verdict validator additionally records
`Incomplete NUnit test-run`; this is retained verbatim, not overridden. The
nonzero-child/FAILED result and all 46 failed assertions remain blockers.
XML SHA-256: `46029e89d7690eea88e15cd8ac1936ab972d40bafff0da1b12167fe1143ecf80`.

Qualified inputs were unchanged (`generated-inputs.changed=[]`). With terminal
process completion and reservation released, a distinct fresh post-failure
preflight passed, the existing helper restored only three archived settings, and
a distinct final preservation check passed all 1,494 original inputs and protected
evidence. Reports are in `TestResults/h1-f4-20261027-living02/post-failure-*01`.
These separate passing preservation checks do not change the FAILED campaign.

## Remaining failure groups and next correctness gate

| Observed group | Count | Current evidence / next action |
| --- | ---: | --- |
| `living.foreground.invalid_state` | 38 | Shared direct-writer/pump path; trace the exact failing stage and time-transition capability before classifying as runtime or fixture. The fixtures supply wall timestamps directly; production must retain the single W contract. |
| `time.recovery.context_mismatch` | 2 | Tests supply replacement policy instances after binding. `LivingDungeonForeground.cs:25` requires the consumed policy and tick cadence. Author the fixture inputs before owner creation. |
| `living.foreground.clock_invalid` | 2 | Native/lifecycle fixture clock paths; prove a single precise clock and binding rather than replacing its owner or relaxing validation. |
| `living.profile.invalid` | 1 | Generic historical limits reach production recovery profile construction in `ConfigureRecovery`; distinguish reader-only fixture scope from supported runtime inputs. |
| W reinspection assertion | 1 | Actual 9000 versus expected 2000; trace observation/transition ownership. This is not waived as an obsolete expectation. |
| `gd66.transaction.candidate_invalid` | 1 | Partial-recovery funding clone fails publication; inspect retained owner and recognized metadata. |
| Expected recovery-required reason | 1 | Waiting-release test receives foreground-invalid before its intended fault cut; establish a valid predecessor before fault qualification. |

These are investigation groups, not a declaration that every failure is fixture-only.
No production validation, bound context, frozen history or assertion was weakened.
All 268 previously passing recovery/Research/structural/write cases pass at this
new source. An exact name mapping finds 130 originally failed full-suite positions
now passing in the selected cohort, and zero originally passing positions failing.
This is **not a new full-suite failure count**. The last full report remains
1,966 passed / 805 failed / 1 skipped at `16955152`, INCOMPLETE / UNCERTAIN.

Next: correct and qualify coherent shared foreground/time/configuration fixture
or runtime defects using deterministic reproductions; retain all current failures.
The fourth scene null reference, lifecycle/clock/deferred behavior, historical
schema-16/current-schema-17 expectations and remaining individual full-suite failures
also remain open. Do not repeatedly launch the full suite against these known groups.

Full EditMode, applicable SIT/protocol-cut and repeated-crash tests, PlayMode,
isolated Windows ordinary/abrupt reopen and the historical/current/H1-F4 performance
comparison remain pending. No speedup or merge readiness is claimed. Broad Windows
and final benchmarks remain held while correctness blockers are open; the known
intermittent native Windows shutdown issue is not reclassified by this Editor result.

Keep draft PR #259. No owner UAT, merge, H1-F5, H2 or O1. Artifact owner is H1-F4;
preserve both named campaigns, all preparations, archives, prior receipts, lock
proofs, generated seeds and failure evidence. No cleanup of these retained artifacts
is authorized. The reporting commit changes documentation only and does not relabel
the tested source or any historical campaign.
