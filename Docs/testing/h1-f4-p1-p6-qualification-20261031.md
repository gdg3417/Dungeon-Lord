# H1-F4 P1/P6 partial qualification — 2026-10-31

PR #259 stays draft. **The complete 14-identity packet is not qualified.** Ten P1 identities, P6 and six controls passed a separately declared 17-case gate. Three affected identities expose other failures and remain unresolved; no skips, removed assertions or production workarounds were used.

## Source and changes

Starting HEAD `b50e18ee2ba6ccaa63e38cb37aec6f5a2cb5205b`; diagnostic checkpoint `ff6d7170edfe5c4c434ae50a72217354b115b375`; corrected/tested source `3b60809b97076abd1335e01bc84a96fd57daf215`. Branch `codex/h1-f4-recovery`, base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. GPT-6.1 Sol Medium recommended for Standard fixture correction; GPT-6 Sol Medium fallback. No production authority change or escalation.

Changed test files only:

- `PhaseSevenA4ProductionSceneTests.cs`: one bounded baseline helper, hash-only diagnostics and one exact owned screenshot-log expectation.
- `ProductionDungeonAnalysisSceneTests.cs`: P1 baseline calls, removed temporary raw save-window tracing, removed the P1 reopen's root disabling. Its now-visible recovery barrier is retained as a failure.
- `CombatC1ReplaySceneTests.cs`: baseline calls in the two P1 methods; existing later configuration mutations are unchanged.
- `ProductionDungeonRoomContentSceneTests.cs`: baseline calls in the five P1 methods; geometry funding now includes its existing reason in the assertion message.
- `ProductionDungeonOverlaySceneTests.cs`: FloorHud baseline call.

No P2–P5/P7 method body, production behavior, save field, schema, migration, tuning, localization, clock provider or R1 reporting changed. R1's accepted 306/0/0 checkpoint remains closed.

## Baseline contract and byte evidence

The initial probe campaign reproduced the decimal-only first mismatch for 12 P1 identities. Geometry stopped at funding there, then the focused run proved the same exact decimal suffix before accepting its baseline. Thus all 13 have whole-file proof in JSON: original/accepted SHA-256, lengths, first offset and `walletDecimalSuffixOnly=True`. Only `ManaReserve`'s integer spelling gains `.0`; no other bytes differ in those observations. No raw save content or auth arguments are logged.

The helper keeps GameRoot enabled and the same TimeService bound. It observes existing dirty/pumping/boot/publication flags read-only and yields through normal Unity Update for at most eight frames, stopping on the explicit completed condition. It never pumps private methods, changes clock providers, forces recovery flags or advances time. Pending setup must produce exactly one publication; already settled setup produces zero. It rejects unresolved recovery/stalls/retry/failure and an open episode/accepted horizon.

Before/after checks require exact session/disk equality, unchanged controlled UTC and monotonic value, unchanged foreground horizon and identical serialized **runtime** SaveData. That separate runtime snapshot checks wallet, ticks, W, A/C, rewards and recovery state; it is not a transformation of either canonical comparison array. The raw before/after files must be identical or contain only the independently demonstrated wallet decimal suffix. Original exact canonical-byte and gameplay/UI assertions remain intact after the baseline. No array normalization or canonical reserialization is performed by the fixture.

The bounded hash/counter diagnostic is retained to prove setup publication and distinguish later legitimate writes from UI-only assertions. It stores one current baseline and emits no raw save values. No per-frame reconstruction or production allocation was added. Qualified UI-only intervals preserve exact bytes and existing wallet assertions; this is not a claim that every action in these compound tests is read-only. The three unresolved tests have no completed-path no-write claim.

P6 expects exactly one `LogType.Log` with an anchored regex: the campaign-owned `fixture-inputs/screenshots` directory, eight lowercase hex characters and `phase7a4-theme-normal-1080x1920.png`. The capture utility's existing ownership/reparse/isolation checks still run, a real nonempty PNG is independently verified, and `LogAssert.NoUnexpectedReceived` remains for every other message. Theme/dependency/localization/rendered-text/layout assertions are retained.

## Exact outcomes

All names have prefix `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.`. The JSON records every original identity, original message, each selected attempt's result/stack, baseline proof and exact remaining-original set.

| Identity | Packet | Final evidence |
| --- | --- | --- |
| `AnalysisNavigationRetainsFloorAndReadOnlySaveAcrossShellReopen` | P1 | Unqualified; new failure retained |
| `AnalysisPhoneInvalidAndStaleDialogsRemainAccessible` | P1 | Unqualified; new failure retained |
| `AnalysisPhoneNavigationSafeAreaAndCoveredInput` | P1 | Qualified pass |
| `AnalysisPhoneOpeningDuringReplayClosesPresentationOnly` | P1 | Qualified pass |
| `CombatC1MultiDefenderPlaybackControlsAndResponsiveEvidence` | P1 | Qualified pass |
| `CombatC1TargetFeedbackTracksRecordedHitsAndDefeats` | P1 | Unqualified; new failure retained |
| `FloorHudStatesAndInputCapture` | P1 | Qualified pass |
| `RoomContentCompactAcquisitionOwnedReassignAndOverflow` | P1 | Qualified pass |
| `RoomContentExplicitReassignmentWithConstructionAndInspectionReuse` | P1 | Qualified pass |
| `RoomContentProductionPreviewCustodySaveDiscardAndLayouts` | P1 | Qualified pass |
| `RoomContentUatBrowserIdentityAndDirectTargetSwitch` | P1 | Qualified pass |
| `RoomContentUatPlacementGeometryAndLocalizedFeedback` | P1 | Qualified pass |
| `RuntimeThemeAndLocalizedTextRenderInActualScene` | P6 | Qualified pass |
| `UnresolvedRecoveryRetainsResolutionAndFailedDeleteCanRetry` | P1 | Qualified pass |

| Attempt / campaign | Tested source | Pass / fail / skip | Supervisor |
| --- | --- | --- | --- |
| pb01 `b77e00e12dad4b46a3a3d3a62c156eca` | `ff6d7170` | 1 /13 /0 | FAILED; normal guarded finalization verified |
| pb02 `71e128c853854884a88356edd024df99` | `3b60809b` | 3 /2 /0 | FAILED; normal guarded finalization verified |
| pb03 `7c4c9237cb4e49e89f2ad55a5f870352` | `3b60809b` | 17 /3 /0 | FAILED; full 14 targets +6 controls; guarded finalization verified |
| pb04 `2617beb9783a41568358c9f47baee809` | `3b60809b` | 17 /0 /0 | PASSED; explicitly narrower 11 targets +6 controls; all five stages and independent verification passed |

The six controls cover original owner/fraction binding, repeated fractional ordinary cuts, invalid UTC, invalid monotonic time, cached-boundary no-publication and proven rollback through the real root save barrier. Compile-only checks passed and are not NUnit passes. Overlapping campaign counts are not added together.

## Unresolved failures and stability risks

1. `AnalysisNavigationRetainsFloorAndReadOnlySaveAcrossShellReopen`: its initial read-only interval now passes with one decimal-only setup publication. After real root reload the existing `Save != null` wait does not prove completed boot recovery. The helper refuses that active lifecycle/recovery obligation before accepting another baseline. No false closure or suppressed grant was introduced. A separate bounded real-bootstrap/lifecycle readiness correction is needed.
2. `AnalysisPhoneInvalidAndStaleDialogsRemainAccessible`: the wider run reports a NullReferenceException restoring strings in its `finally` block (source line 259). The earlier failure/cleanup interaction is not established; no null guard or weakened assertion was added. Diagnose that first condition separately.
3. `CombatC1TargetFeedbackTracksRecordedHitsAndDefeats`: the original first-scenario decimal baseline is proven. A later `SimulateRunOnce` assertion fails at line 257 after existing post-binding combat/health configuration changes. Those changes were not moved, rebound or relaxed in this packet.

Focused multi-defender replay also produced a missing-label NullReferenceException at line 59, then passed at the same source in the 20- and 17-case runs. Geometry funding failed before probing in pb01, then passed without funding behavior changes in later runs. These remain explicit order/environment risks; their causes are not declared fixed. No new production persistence/recovery/UI defect has been confirmed.

P2–P5/P7 remain unchanged: stale modal mutation; prebinding party/branch combat configuration; loot invalidation/input ownership; direct-pause-before-funding; historical B1 presentation versus current C2 writer separation. Do not treat the baseline helper as a solution to those contracts.

## Preservation and handoff

Independent receipt: `TestResults/h1-f4-20261027-pb04/independent-completion01.json`. All four original ZIPs and their 1,494 members/hashes, pinned tools, exact NUnit leaf sets, host/child identities, CLI exits, Job completion/absence, original inputs, settings/TMP, screenshot, protected owner saves and historical evidence were verified. The last campaign's before-map covers all prior campaign/preparation evidence. Successor isolated save namespaces are explicitly identified; all pre-existing save entries remain unchanged. Reservation is null with previous `2617beb9783a41568358c9f47baee809`.

Each failed campaign remains permanently FAILED. Its preflight/test ran, and its later three supervisor stages did not run; guarded normal-failure restoration/final verification is separately recorded. No exceptional recovery, force unlock, process termination, historical verdict change, owner-save restore or evidence deletion occurred. Raw failed XML remains unchanged. Artifact ownership/retention and exact process/input hashes are in JSON and the linked local receipt; failed saves/draft sidecars are retained.

Original-84 reconciliation: **76 with direct qualified passing evidence /8 still without it**, adding exactly 10 P1 identities and P6 to the previous 65. This is not a new full-suite result. The remaining eight are the five untouched packets plus the three above. The whole P1/P6 packet remains incomplete despite the passing partial gate.

Unit: fixture guard and existing invalid-clock/duplicate-credit/publication assertions. SIT: real production scenes and canonical files through normal Unity lifecycle in selected Windows Editor EditMode UnityTests. UAT has not started. No full EditMode, full/applicable PlayMode wrapper campaign, Player, sustained 68-run profile, interruption/reopen final gate, performance/memory benchmark or owner UAT ran here. Those gates remain pending; no native shutdown or performance claim is made. PR #259 stays draft, with no merge or owner-UAT authorization.
