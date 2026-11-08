# H2-A retained foreground execution — checkpoint 1

Initial working implementation and focused gate passed. Independent architecture review is next; this is not final H2-A qualification or merge readiness.

Base: 01ff418143c0400d8e6fafba7a260f399bcdb2aa (merged PR #259; no newer main commit or overlapping open PR at recheck). Branch: codex/h2-a-retained-foreground-execution. Tested source: 5d585c07e083369925a199a6f2154a72fe824419. Later evidence/documentation commits do not change Unity inputs.

## Ownership and exact reuse proof

SaveService owns one private LivingDungeonForegroundExecutions working set. Its synchronous writer borrows the dictionary exclusively, removing the old binding before mutation. Parties retain independent execution graphs; scheduling still orders by absolute deadline, numeric sequence and ordinal. No mutable handle reaches UI or another state owner.

The binding requires the exact immutable canonical session object, validation context, policy, profile, configuration, production content, loot and serialization limits. The session owns complete predecessor bytes, including scheduler, admission, recovery, settlement and unknown members. The writer still compares disk bytes and independently validates that predecessor before borrowing. H1-F4 consumed-value guards and supported profile checks remain mandatory; matching references or RunId alone are insufficient.

Each borrowed execution must export bytes identical to the independently validated party checkpoint, including start identity, ordinal, executable, timing and transfer controls. A mismatch abandons that execution and uses the supported reader. NextDueTime may prepare iterator lookahead but commits no event. Existing replay/export checks authenticate the committed prefix and controls.

After the original writer confirms persistence and fresh semantic readback, compatible nonterminal executions can bind to the returned durable session. Final exported bytes must again equal the fresh validated checkpoints. Only confirmed runtime publishes. An opaque invalidation token must survive the loan: a disable/quit callback during synchronous IO cannot be undone by late successful rebind. Optimization failure after a confirmed commit causes later reconstruction, not a false persistence failure.

All speculative executions are abandoned on failed preparation, validation or persistence. A=C and no recovery episode are required for cross-pump retention; accepted-but-unclosed work reconstructs. External canonical actions preserve the earlier-work flush and clear the set. Replaced sessions, configure/load, recovery entry, delete, disable and quit invalidate it. Historical starts remain in canonical checkpoints, so edits affect later admissions. Every session assignment uses a private invalidating setter; three existing reflection setup calls were adapted accordingly.

Terminal pinning, outcome consumption, reconciliation, history authorization and retirement are unchanged. The consumed execution is abandoned immediately after lineage verification. Vacancy/release capabilities remain transaction-local and never survive a pump. Waiting retains source occupancy; Released and transfer completion remain one atomic timestamp. Every detached stage, complete-save validation, existing write boundary, reserve check and independent durable readback remains.

## Bounds and inspection findings

Production v1/v2 permit one/two active parties. Retention keeps only active, nonterminal executions, with no completed archive, per-frame maintenance or background worker. Approved envelopes remain four canonical retained records, 65,536 checkpoint bytes per party, 2,048 replay events per party, 8,192 aggregate replay events, 1,000,000 combat work and 357,996 owner bytes, plus existing content/geometry limits. No numeric policy is added or changed.

PresentationSteps grows with bounded ordinals. Combat events, participant/encounter collections, traversal state and iterator lookahead survive with each execution. Production combat also bounds 32,768 events, 32 rounds, 30 encounters and 64 participants. Lifetime admission assurance still verifies its existing prefixes. These are finite execution envelopes, **not a measured heap-byte budget**. Representative retained-heap, GC and boundary-envelope evidence remains required before final qualification.

## Executed focused evidence

Supervisor campaign 94389388133a4f39a6f61e12f616a7f1: **8 passed, 0 failed, 0 skipped**, 13.926 seconds. Unity 6000.3.2f1; CLI updated and verified at 1.0.0-beta.17. All five stages passed: preflight, focused tests, restore preflight, exact restoration and final preservation. Existing authorities verified 1,500 input files, protected owner/save/evidence state and owned-process completion. [Report and case names](evidence/h2-a-checkpoint1-20261107/report.json); [NUnit and receipts](evidence/h2-a-checkpoint1-20261107/focused-evidence.zip).

Cases exercise actual cross-pump reuse with complete byte equivalence, one/two-floor terminal settlement, waiting and genuine blocker retirement/release, proven and uncertain rollback, changed disk bytes/consumed inputs, identical-byte reopen invalidation and invalidation during publication. Equivalence cases assert unchanged complete-validation and canonical-write counts. No UTC, gameplay or save field is normalized.

| Six-pump in-memory series | Coordinator restorations: reference → retained | All checkpoint replays: reference → retained | Series ms: reference → retained |
| --- | --- | --- | --- |
| One floor (v1) | 5 → 1 | 52 → 48 | 954.05 → 909.66 |
| Two floors (v2) | 5 → 1 | 146 → 142 | 1968.27 → 1911.83 |

This is one ordered sample per mode against the reconstruction reference in the tested source, not a matched main/branch frame benchmark. Validation/readback/lifetime replay remains expensive. No FPS, p95, frame-latency benefit, memory saving or mobile readiness is established. The thread allocation counter returned zero without calibration; it is not evidence of zero allocation.

## Preserved unsuccessful attempts

| Campaign | Recorded result and disposition |
| --- | --- |
| ff0c1234cd734ea39b501f7c7332bee7, source 67f1d5b0 | INCOMPLETE / UNCERTAIN, zero selected tests. Unity ignored a newly authored fixture's malformed 33-character meta GUID; corrected to 32. Descendant completion was separately uncertain. Owner approved exact three-settings restoration/reservation release; execution and separate postflight passed, preserving all 20 campaign files. A surviving SDK adb process has unproven ownership and was preserved without termination. Original verdict unchanged. |
| 73fc1c2962e34677b3daf1797150e8fa, source c192ca14 | FAILED, 7 passed/1 failed, verified owned completion and normal reservation release. Earlier synthetic filesystem evidence consumed the injected first flush, producing proven rather than uncertain rollback. The fixture now resets its filesystem as established tests do. Exact restoration and separate preservation passed. The local adapter final mode was corrected to the existing restored-input preflight authority; no historical helper changed. |

Raw logs, original inputs, product namespaces, caches and failure states remain local and preserved. No build or owner save was deleted, overwritten or launched. The exact-path ownership/retention ledger and preparation locations are in [the evidence report](evidence/h2-a-checkpoint1-20261107/report.json); no artifact is currently eligible for cleanup.

## Remaining gates and scope

Unit/SIT: the eight focused EditMode cases above passed. Broader start/profile/ordinal/corrupt-checkpoint negatives, canonical action/service lifecycle integration, partition equivalence, complete failure matrix and retained-buffer/heap measurements remain. Initial review should assess the ownership/rebind proof and execution envelope before expanding that matrix.

PlayMode, full relevant EditMode/PlayMode suites, Windows Development build and ordinary standalone close/reopen, matched current-main/branch performance and final independent review have not run. Owner UAT has not begun; any risk-based gameplay smoke must be prepared and independently authorized after integration/qualification. No release or merge approval is claimed.

Schema 17, frozen readers, migrations, gameplay tuning, configuration values, localization and flags are unchanged. No sub-dungeon, offline party processing or capacity increase is introduced. H2-B cadence/coalescing, provisional advancement/rollback policy, safe cuts and provisional-versus-confirmed UX remain deferred. H2-A does not delay or remove existing stages or durable publication.
