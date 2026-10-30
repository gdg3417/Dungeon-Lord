# H1-F4 passive recovery / clock fixtures

Qualified source `4401358047aeb8cf3127e1a7680c3f86e44fb695`, campaign `812a0af79c65454a9887a4a81217747b`: **64 passed, 0 failed, 0 skipped**, all five stages and independent verification passed. This is the exact selection retained from rx08, not a full-suite result. Ten original failures now pass; combined with the prior groups, **54 of the original 84 have qualified evidence; 30 remain unqualified**. See the adjacent JSON for exact identities, hashes, remaining classes and receipts.

## Corrections

Only `OfflinePassiveManaTests.cs` and `RecoveryEntryAndPrecisePassiveManaTests.cs` changed. Commits: `cbac2ea3`, `06191bbf`, `285121d4`, `44013580`. Production, schemas 7–17, frozen readers/migrations, tuning, localization and supervisor rules are unchanged.

- Initial W and wallet are authored before the tested recovery. Services use the consumed input bundle, production limits and actual prebound load path. Root fixtures attach that same observation owner once, following the existing qualified H1-F4 RootScope pattern.
- Direct legacy grants are replaced by actual entry/original/residual coordinator transactions. The precision test explicitly rejects the old grant API before proving publication through the authorized coordinator; pure calculations remain nonmutating.
- The fault is applied after durable entry, so rollback preserves originalPending, stable R/D0, exact predecessor bytes and the same session. W already covers R at that point; it does not imply the original grant was paid. Retry, closure and reopen preserve the expected wallet without duplicate grant.
- Stale-disk refusal reaches the actual recovery writer and retains its existing reason, source session and no-write assertions.
- Backward-clock integration cases install the real root foreground barrier. A standalone metadata no-op can preserve W without observing a clock; it is not evidence of invalid-time progression. The root's actual observation rejects backward time before any save-reason writer executes, preserves bytes/wallet/W, and records blocked state. Reopening at a valid clock then permits only the genuine new interval.
- Reopen uses the existing `ReopenWithSupportedForegroundProfiles` helper. The old dormant-only context rejected the production scheduler created by the root; the supported helper verifies unchanged original configuration, including CombatC0, before constructing its read context. No current context is stamped onto historical accepted work.

The focused selection explicitly defers these three existing cases, without adding Ignore/Skip, changing discovery or removing them from the mandatory full suite:

- `ColdStartCommitBootSaveAndFreshReopenCannotReplayInterval` — corrected coordinator reproduction exposes a production summary gap.
- `PauseResumeAppliesOnceAndRepeatedResumeDoesNotReplay` — lifecycle fixture/report integration remains to be completed.
- `ResumePersistenceFailureKeepsSimulationPausedAndRetryDoesNotDuplicate` — lifecycle fixture/report integration remains to be completed.

The last two have not been established as reporting-only defects. Their original assertions remain; they are not claimed fixed.

## Retained failures and preservation

Diagnostic campaign `544e9c1243b64539b1445e3193ebef47` at `cbac2ea3` remains FAILED, **1/1/0**: the coordinator control passes, while cold recovery reaches its correct durable wallet and closure before the summary assertion fails. The full stack is retained in its XML and the compact evidence JSON. This is diagnostic evidence, not a passing gate.

Campaigns `e8c49459f3a54a24b6bfdd44d17451ef` at `06191bbf` and `4d6726e6815448199d1dd010935dbb5a` at `285121d4` remain FAILED, **58/6/0** each. The first exposed the missing root barrier; the second reached the dormant-only reopen guard. Each has completed process/Job evidence, unchanged qualified inputs, a released reservation and a successful guarded normal-restoration receipt. No exceptional recovery occurred in this continuation.

Final independent verification checks all 1,494 original input/archive members, pinned tools, exact NUnit leaf identities, recorded stage exits/Job completion, settings/TMP restoration, actual released reservation and prior/protected evidence. Historical FAILED and INCOMPLETE / UNCERTAIN verdicts remain unchanged. Compile-only probes 24–27 are not NUnit passes. All owned artifacts are retained for review.

Unit/SIT: applicable and covered by this cohort. Full EditMode, applicable PlayMode, remaining interruption/reopen, isolated Windows runtime/shutdown and matched main/H1-F4 performance remain pending. No merge or owner UAT.

The separate [reporting-contract proposal](h1-f4-recovery-reporting-decision-20261030.md) identifies the demonstrated production gap and a display-only decision; it does not reopen W/R/S/Q, lifecycle or settlement policy.
