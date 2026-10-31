# H1-F4 R1 recovery reporting qualification — 2026-10-30

R1 is implemented and its directly affected cohort is qualified. PR #259 remains draft. This is not full-suite, Player, performance, owner-UAT or merge qualification.

## Source and scope

- Branch `codex/h1-f4-recovery`; base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`.
- Implementation `91165c25d5e2af0039de1b8ba87d706cbc719360`; tested correction/source `2c90eca04cc8510672086dd9fe05130423bfc3d8`. Later documentation commits do not change the tested runtime.
- Recommended/used model: GPT-6.1 Sol High, Complex bounded reporting integration. GPT-6 Sol High fallback; no deliberate substitution or Astra use.
- [Accepted contract](h1-f4-recovery-reporting-decision-20261030.md) · [Compact exact evidence](h1-f4-r1-reporting-qualification-20261030.json).

## Demonstrated defect and correction

Canonical settlement already committed the wallet, monster effects and closure, but discarded grant evidence before the GameRoot summary. The legacy result remained null. R1 fixes this production presentation gap without changing settlement, atomic persistence semantics, configuration, schemas, migrations or historical readers.

The writer captures immutable reporting metadata for the exact final validated candidate: predecessor/candidate SHA-256, context, existing episode identity/phase, classified eligible 100 ns duration, computed/actual grant, effective rate, capacity limitation, actual recovery spending and final wallet. This metadata is provisional. One bounded SaveService aggregate accepts it only after the existing durable publication proof. It retains at most original/residual receipts plus the already bounded pending candidate evidence; it cannot grant, spend, classify time, retry or publish.

Proven rollback adds nothing. Repeated proof adds nothing. Uncertain acknowledgement retains candidate-linked evidence and promotes it only after independent exact candidate authority proof; predecessor proof discards it. The reconstructed uncertain-proof result's default zero spending is never a historical spending receipt. Missing original or spending evidence suppresses the full breakdown. Current-process proven closure instead renders localized neutral completion/current mana; a freshly loaded closed save has no historical summary. Ordinary refreshes retain a valid completed report and use the current canonical wallet. New recovery or incompatible/lost continuity clears stale evidence.

Duration is summed before display using exact fixed-size counters (decimal addition safely holds two signed-64-bit nonnegative durations). Known foreground stalls are excluded. Unequal rates use separate labels; no single invented rate. Actual capacity limitation and healing costs remain distinct. The four new stable keys are `ui.recovery.completed_format`, `ui.recovery.summary_format`, `ui.recovery.rate_format`, `ui.recovery.rates_format`. Missing translations never leak raw keys. Legacy OfflinePassiveManaPresenter behavior remains unchanged.

## Tests and evidence

Fresh discovery: campaign `fdbf8a236af1403aa1c78c4bca55e62c`, source `91165c25`, 2,819 EditMode/4,297 PlayMode leaves; no tests executed. All five stages and independent preservation passed. The subsequent correction changes only a test body; exact selected identities are reconciled against the final NUnit XML.

Final cohort: **306 passed /0 failed /0 skipped**, campaign `7f3d8b5c387b4b65aee0bd0e8a6394a9`, source `2c90eca04cc8510672086dd9fe05130423bfc3d8`. Complete selected passive-mana, recovery/clock, precise-calculation, monster-recovery and canonical-writer classes were included. All five stages passed; Editor/CLI, child/host and Job completion records were checked. Independent verification confirms all 1,494 original input/archive hashes, ten pinned tools, protected owner/save/history evidence and 286 prior evidence directories; settings/TMP restored exactly. Reservation is null with this exact campaign as previous. The local independent receipt is `TestResults/h1-f4-20261027-rr02/independent-completion01.json`; hashes and per-stage process identities are in the compact JSON.

All three original deferred identities passed:

1. `ColdStartCommitBootSaveAndFreshReopenCannotReplayInterval`.
2. `PauseResumeAppliesOnceAndRepeatedResumeDoesNotReplay`.
3. `ResumePersistenceFailureKeepsSimulationPausedAndRetryDoesNotDuplicate`.

Coverage includes same-process original/residual aggregation, exact fractional duration, excluded known stalls, distinct-rate presentation, actual capacity clamps, actual monster-healing spending (including reconstructed uncertainty proof), rollback/retry, confirmed candidate and predecessor proof, duplicate callbacks/retries, reopened residualPending without an original receipt, lost pending spending evidence, closed reopen, proven zero, localization fallback and display-only canonical-byte/operation invariance. Distinct-rate rendering uses clearly identified synthetic presentation-unit inputs; it is not a claim that a different runtime configuration was rebound during an episode. Existing legacy calculator/presenter tests remain intact.

The first R1 campaign `48ee840a893a42cea5ecca8f70c667fc` remains **FAILED**, raw 305/1/0 at `91165c25`. Its full exit records, unchanged qualified inputs and released reservation allowed existing guarded normal-failure finalization, with protected evidence and exact restoration verified. No exceptional recovery, force release or process termination occurred. The one failure was fixture-only: SaveService correctly refuses an open episode before invoking an action writer; it is not an explicit root retry. The corrected test uses the existing root action barrier, verifies the action remains rejected, then observes durable closure and no duplicate award. It does not fake pause state or change production guards. The lifecycle fixtures now author W/wallet before ownership and share the actual bound TimeService; L1 records return while the recovery barrier pauses simulation.

Compile-only probes are retained and never counted as tests. Probe 29 found the test's LINQ Count invocation defect; 30 passed. Probe 31 stopped at its stale rx11 protection baseline, before compilation; read-only comparison found only the known new isolated di03/rr01 directories. The current rr01 baseline matched exactly, and probe 32 passed. No owner/historical change was accepted or repaired.

## Remaining qualification

Original-84 reconciliation: **61 qualified /23 unqualified**, adding only these three identities to the prior 58. Thirteen original failures passed in this cohort, but ten already had qualified evidence and are not counted twice. The 23 remaining are 19 scene identities and four isolated Research/filesystem/Living Dungeon/journey identities. These are coverage counts, not a new full-suite failure total. Historical full 75bee8ad raw 2,728/84/1 remains INCOMPLETE/UNCERTAIN; every historical verdict remains unchanged.

Unit and selected EditMode SIT are applicable and passed. Full EditMode/PlayMode, remaining interruption/native reopen, isolated Windows runtime/shutdown, matched performance/memory and independently authorized owner UAT remain pending. No Windows, benchmark, full suite or owner UAT was launched here. Next: independently review R1 and continue the remaining established regression/qualification gates. No new design decision or authority contradiction was found.
