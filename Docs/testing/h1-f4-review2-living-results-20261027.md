# H1-F4 Review 2 — Living Dungeon affected-cohort qualification

Status: ready for independent PR review of this correction milestone. This is
not full H1-F4 qualification, merge readiness, or owner-UAT authorization.

Branch `codex/h1-f4-recovery`; existing draft PR #259. Base remains
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Qualified test/tool source:
`3b63ff49a12496b97d7ed2e6e5ecba6bac11350e`. The subsequent report-only commit
must not be described as a separately executed Unity qualification source.
Recommended model: GPT-6.1 Sol High, Complex; GPT-6 Sol High availability fallback.
No Astra escalation, delegation, production-code change, gameplay tuning, schema
change, validator relaxation or new writable authority was needed for these fixes.

## Confirmed common cause and deterministic representative proofs

The direct one-floor/two-floor helpers supplied wall seconds 1000 without a
`CanonicalTimeTransition`. `LivingDungeonForeground.AdvanceForeground` copied
those seconds into its first private Stage's recognized snapshot. Schema 17's
`DetachedCanonicalSaveSession.PrepareReplacement` requires supplied
`lastSavedUtcUnix` to equal prior W or the authorized successor W. With no
transition, successor W equals prior W, so that exact timestamp guard rejects
1000. It returns `time.recovery.invalid_state`; the foreground writer's existing
exception mapping returns the generic `living.foreground.invalid_state`.

Two new parameterized cases construct valid exact predecessors, reproduce that
failed direct call without writes, and isolate the first Stage. They assert the
specific preparation reason and that the recognized snapshot itself is valid.
The same Stage succeeds when the only missing authority is supplied via the
existing source-bound foreground observation/transition; the real writer then
publishes and closes it successfully. No production diagnostic hook or reason-code
change was added. These are synthetic current owners, not historical producer proof.

Representative campaign `018acc3079bf4e7d89b79789e99b4433`, source
`6dbfc4a108c32cd0e14193187a0d0b4238633f1b`: **2 passed, 0 failed, 0 skipped,
0 inconclusive**, all five supervisor stages PASSED. Both cases also pass again
in the complete affected cohort at the corrected source.

The fix is test-scoped: `PhaseFourTestSupport.PumpForeground` uses the existing
`ForegroundObservation.ForDeterministicServiceTest` and
`CanonicalTimeTransition.Foreground` APIs, bound to exact predecessor bytes and
A/C. It retains W while draining older A>C. Production SaveService/TimeService
already own this seam; the fixture no longer bypasses their required capability.

## Other fixture corrections and preserved assertions

- Replacement bounded policy objects had failed strict consumed-input identity.
  Both policy byte inputs are now authored before the synthetic owner; pumps use
  the consumed instances. No persistent context is rebound. The custom terminal
  tick cadence is likewise authored before owner creation, with the original
  no-coincident-tick condition and saved-boundary assertions retained.
- Two native/lifecycle fixtures supplied only whole-second `ITimeSource`, while
  precise native publication/lifecycle observation requires `IPreciseTimeSource`.
  The precise source is now injected before ConfigureRecovery/load. The root uses
  that same bound TimeService, rather than replacing the owner after attachment.
- The generic historical-reader budget cannot construct supported production
  recovery profiles. That test now explicitly asserts refusal before load/write,
  dormant state and identical predecessor bytes. Historical readers and limits
  are unchanged; unsupported inputs do not silently acquire production execution.
- Reinspection had called a publishing foreground API with a new eligible wall
  observation. It now calls the actual read-only InspectForeground API and proves
  identical save bytes/W. The expected 2000 remains; the subsequent new eligible
  progression still advances W to 9000. No expected value was changed to hide a
  publication or time-accounting error.
- The pre-pause test captures its full eligible horizon through the real TimeService
  before pause. Later suspended pumps rebind the retained endpoint 3000 while
  background UTC is 9000; the old accepted horizon drains first. The original A/C
  and W assertions pass, excluding later background from that foreground cut.
- Partial-recovery funding previously cloned nullable Research owners with
  JsonUtility's null-class defaults. Funding now snapshots the original owners,
  stages only the explicit synthetic wallet amount and restores the old in-memory
  amount before accepting the new projection. Production serialization is unchanged.
- The waiting-release fault test now reaches its intended existing Flush/rollback
  cut once the time capability is present. Original recovery-required, no-partial-
  publication and party/reward assertions remain and pass.

Partition equivalence compares every canonical byte except its already-normalized
scheduler revision and the intentional policy-context digest difference. Both
fixtures independently prove their persisted digest equals their own immutable
producer identity and that the two contexts differ. This comparison normalization
never edits a save or authorizes rebinding. All other outcome/checkpoint/ordering,
capacity/healing, terminal, transfer, exact-once and reopen assertions remain.
No production runtime defect was confirmed or changed in resolving these 46 cases;
this is not a claim that all unqualified runtime paths are correct.

## Exact affected-suite result and source/input/process evidence

Campaign `b09a3fc9877b4b1ea6ca8fb435512052`, preparation `r2-living01`:
**358 passed, 0 failed, 0 skipped, 0 inconclusive**. Approved plan count, root
count and all 358 unique NUnit leaf IDs match. The original 356 cases all pass;
the two additional provenance cases account for the expanded count. No tests
were removed, ignored or filtered out of the established affected selection.

| Cohort | Passed | Failed |
| --- | ---: | ---: |
| H1-F4 recovery contract | 111 | 0 |
| Structural preview | 77 | 0 |
| Canonical write | 59 | 0 |
| Research durability | 18 | 0 |
| Research save integration | 3 | 0 |
| One-floor B3B2, including two new provenance cases | 53 | 0 |
| Two-floor | 37 | 0 |

Unity 6000.3.2f1 and CLI beta.14 remain pinned. All nine exact tool hashes matched.
Product: `Dungeon Lord B3B2 Smoke H1F4 20261027 r2-living01`; save namespace is
`C:/Users/gdg34/AppData/LocalLow/gdg3417/` plus that product name. Generated seeds
remain under this campaign's `living-regressions/fixture-inputs/two-floor`; no
historical input directory was reused or changed. Seventeen preparation-tool unit
tests passed; those are separate from Unity qualification.

All five selected stages PASSED: workload preflight, test, restore-preflight,
restore and preservation. Each has host/runner exit 0 and the unchanged supervisor's
owned Job completion proof. Workload host PID 54400, creation FILETIME
134376195707567866; runner PID 53348, creation FILETIME 134376195708756120;
Job `Local\DLQ-7a76ec829cde431f86917f632b4aa630`.
Workload completion was recorded at `2026-10-28T00:12:46.510605+00:00`;
final preservation at `2026-10-28T00:13:15.769647+00:00`.
NUnit XML SHA-256:
`06948eff61b0e8a2c4899ba6510f5374e9a83fc8285cb329bcf8b95984a755ff`.

The [source-pinned JSON](h1-f4-review2-living-qualification-20261027.json) contains
all stage records, exact tool/input/archive/evidence hashes, both representative
proofs, each old failure's exact name and current NUnit ID/result, and the independent
final verification. Raw Unity logs stay in the owned campaign because they can
contain authentication arguments. Raw artifact owner is H1-F4; preservation and
retention status is preserve, with no cleanup authorized.

Fresh independent verification at `2026-10-28T02:13:47.739521+00:00` confirmed all
1,494 original inputs restored, exact settings/font hashes, unchanged protected
owner/save/build/historical evidence, previous incomplete/failed campaign manifests
and recovery receipts. Current recorded process identities are absent and all
stage Job Object queries return absent/error 2. Those later queries supplement
this campaign's actual recorded completion; they never reinterpret missing
historical descendant evidence. Original INCOMPLETE/UNCERTAIN and FAILED verdicts
remain unchanged. No exceptional recovery was needed for this passing campaign.

Reservation is released: `campaign=null`, `previous` equals this exact campaign.
Restored input-map identity:
`dcf129f2e907b48cf4f18d11f8d4631506b66e128660183c18f92ab0255af419`.
Reservation SHA-256 at verification:
`0b3ed990a8f85c5431798666b943eefa56189af5e3e3b033703cc25f861b926e`.
All paths and manifests are retained in:
`TestResults/qualification-supervisor/b09a3fc9877b4b1ea6ca8fb435512052`,
`TestResults/h1-f4-20261027-r2-living01`, and the corresponding `018acc...` /
`r2-repro01` representative campaign/preparation directories.

This passing execution performs work that previously failed early; its duration
is not a matched performance comparison or a speedup/regression claim.

## Exact status of all previous 46 failures

Every row below maps by the exact prior fully qualified NUnit name in the JSON.
One-floor labels mean LivingDungeonB3B2; two-floor labels mean LivingDungeonTwoFloor.

| Fixture and case | Current result |
| --- | --- |
| One-floor: `AcceptedButUnclosedHorizonReopensWithoutDroppingTimeOrDuplicatingHistory` | PASSED |
| One-floor: `ActualActiveRunAllowsSameFloorDraftCommitAndUsesSnapshotUntilSettlement` | PASSED |
| One-floor: `ActualAdmissionRetainsAnUnfinishedTimedC2PrefixThenSettlesAndRetiresExactlyOnce` | PASSED |
| One-floor: `ActualTerminalReconcilesCustodyOrRedeploymentAndRecoversOnlyAssignedVacantIdentity(False)` | PASSED |
| One-floor: `ActualTerminalReconcilesCustodyOrRedeploymentAndRecoversOnlyAssignedVacantIdentity(True)` | PASSED |
| One-floor: `AutomaticSettlementRetainsSessionReplayWithoutChangingHistoricalSaveContract` | PASSED |
| One-floor: `CurrentRecoveryBeforeSettlementIsChargedOnceAndDamageIsThenAppliedToRecoveredIdentity` | PASSED |
| One-floor: `DeferredPrePauseSampleDoesNotConsumeLaterOfflineInterval` | PASSED |
| One-floor: `DirtyClosedActionBarrierRetainsSessionAndDisabledPauseDoesNotPublish` | PASSED |
| One-floor: `FailedTerminalPumpPublishesNothingAndProvenRollbackRetriesExactlyOnce(Flush,1)` | PASSED |
| One-floor: `FailedTerminalPumpPublishesNothingAndProvenRollbackRetriesExactlyOnce(Replace,1)` | PASSED |
| One-floor: `FailedTerminalPumpPublishesNothingAndProvenRollbackRetriesExactlyOnce(Write,2)` | PASSED |
| One-floor: `FirstOpportunityAndAllOutcomesAreIndependentOfForegroundPartitionAndReopen` | PASSED |
| One-floor: `FreshNativeSessionBindsProductionProfilesBeforeDormantActivation` | PASSED |
| One-floor: `IndependentlyInjectedHistoricalLimitsDoNotAcquireProductionScheduling` | PASSED |
| One-floor: `ManualC2CanFinishBeforeAnOlderAutomaticReservationWithoutDiscardingEitherResult` | PASSED |
| One-floor: `MoveToAnotherFloorPreservesIdentityAndOrdersCurrentVacantRecoveryBeforeHistoricalDamage(False)` | PASSED |
| One-floor: `MoveToAnotherFloorPreservesIdentityAndOrdersCurrentVacantRecoveryBeforeHistoricalDamage(True)` | PASSED |
| One-floor: `PresentationObserverCannotChangeCheckpointBytesWorkReceiptsOrOutcome` | PASSED |
| One-floor: `QualificationCannotConfigureProductionProfileAndTwoValidActivePrefixesCannotBypassCapacity` | PASSED |
| One-floor: `QualifiedProductionLifetimeMeasuresCheckpointReplayAndBoundaryWork` | PASSED |
| One-floor: `RecoveryConditionChangesAreStagedBeforeAdmissionWhenDoubleWalletDebitRoundsAway` | PASSED |
| One-floor: `ReinspectionAndPausedHorizonDrainRetainOfflineBoundary` | PASSED |
| One-floor: `ServiceLatchesRecoveryRequiredAndBlocksAllLaterForegroundPublication` | PASSED |
| One-floor: `TerminalSettlementPreservesTheCurrentSavedBoundary` | PASSED |
| One-floor: `UnprovenRollbackRequiresRecoveryAndDoesNotPublishTerminalEffects` | PASSED |
| One-floor: `UnsupportedCombatWorkConsumesOpportunityWithoutRunAllocationOrGameplayDamage` | PASSED |
| Two-floor: `AutomaticProductionPartyCanChooseASurvivingFloorOneExitWithTwoCapturedFloors` | PASSED |
| Two-floor: `AutomaticTwoFloorAdmissionCompletesAndRetiresExactlyOnce` | PASSED |
| Two-floor: `CurrentDeactivationDoesNotFabricateVacancyOrRewriteCapturedRoute` | PASSED |
| Two-floor: `EmptyUnclosedV1DrainConsumesDueOpportunityWithoutReplacementAndRetainsTheFutureIdentity` | PASSED |
| Two-floor: `EqualTimeOrdinaryTransferAndBlockedReleaseRespectSequenceAndRecoveryPhases(True)` | PASSED |
| Two-floor: `EqualTimeOrdinaryTransferAndBlockedReleaseRespectSequenceAndRecoveryPhases(False)` | PASSED |
| Two-floor: `FailedReleaseTransactionPublishesNoSettlementOrTransferAndRetryIsExactlyOnce(Write,2)` | PASSED |
| Two-floor: `FailedReleaseTransactionPublishesNoSettlementOrTransferAndRetryIsExactlyOnce(Replace,1)` | PASSED |
| Two-floor: `FailedReleaseTransactionPublishesNoSettlementOrTransferAndRetryIsExactlyOnce(Flush,1)` | PASSED |
| Two-floor: `HistoricalV1DrainsOnceAndUpgradesWithoutEligibility(False)` | PASSED |
| Two-floor: `HistoricalV1DrainsOnceAndUpgradesWithoutEligibility(True)` | PASSED |
| Two-floor: `OneBoundaryPumpsAndReopenMatchTheSameAcceptedTwoPartyHorizon` | PASSED |
| Two-floor: `OverlappingSnapshotsPreservePriorDamageAndPartialRecoveryAtBothSettlements(False)` | PASSED |
| Two-floor: `OverlappingSnapshotsPreservePriorDamageAndPartialRecoveryAtBothSettlements(True)` | PASSED |
| Two-floor: `ReopenOneUnitBeforeVacancyAndImmediatelyAfterReleasePreservesExactPrefixes` | PASSED |
| Two-floor: `UnprovenWaitingReleaseRollbackRequiresRecoveryWithoutPublishingEitherParty` | PASSED |
| Two-floor: `WaitingProtectsBothFloorsFromRecoveryAndThrottlesAdmissionAfterReopen` | PASSED |
| Two-floor: `WaitingReleaseIsEquivalentAcrossForegroundPartitionsAndReopen` | PASSED |
| Two-floor: `WaitingReopensAndRealRetirementReleasesItWithAuthenticatedCause` | PASSED |

## Exact source changes and next independent review checkpoint

- `6dbfc4a108c32cd0e14193187a0d0b4238633f1b`: two representative provenance
  cases in LivingDungeonB3B2Tests, explicit two-case supervised selection and
  affected/discovery-count updates in qualify_h1_f4.py/test_qualify_h1_f4.py.
- `3b63ff49a12496b97d7ed2e6e5ecba6bac11350e`: test-only fixes in
  PhaseFourTestSupport.cs, LivingDungeonB3B2Tests.cs and LivingDungeonTwoFloorTests.cs.
- The next report-only commit adds this file and its JSON evidence; it does not
  alter or requalify runtime/test inputs. No source change was necessary on resume.

No failing or skipped case remains in the selected 358. The previous full-suite
report at `16955152` remains 1,966 passed / 805 failed / 1 skipped, INCOMPLETE /
UNCERTAIN. This selected pass does not establish a new full-suite failure total.
Unselected shared setup/configuration assertions, the fourth production-scene null
reference, broad scene/lifecycle behavior and historical schema-16/current-schema-17
expectations still require their own coherent qualification. Current synthetic
active-v1/v2 and frozen checkpoint cases in this cohort do not prove arbitrary
historical producer compatibility or Windows reopen behavior.

Request independent ChatGPT review of this milestone **before another expensive
full EditMode campaign**. After that review clears the known blocking issues,
refresh the exact full-suite inventory and proceed through full EditMode,
applicable PlayMode and protocol-cut/repeated-crash qualification, isolated Windows
Development Player ordinary/abrupt close-reopen, then the required three-way
historical/current-main/final-H1-F4 performance comparison. The known intermittent
native Windows shutdown issue remains unresolved and is not classified away by
this Editor pass.

Do not merge, initiate owner UAT or begin H1-F5, H2, O1, Phase 8, unrelated UI or
speculative optimization. The development sequence remains H1 → H2 → O1 → reassessment.

READY FOR INDEPENDENT PR REVIEW
