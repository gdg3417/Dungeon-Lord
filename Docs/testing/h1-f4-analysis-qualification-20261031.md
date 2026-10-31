# H1-F4 Analysis readiness and ownership qualification — 2026-10-31

Both requested identities and the complete selected acceptance scope are qualified. PR #259 remains draft; full suites, native/runtime and performance gates remain incomplete.

## Source and correction

Branch `codex/h1-f4-recovery`; base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Start `92d91003d9656734e650c68db3b26505f65a00bf`; bounded diagnostic `173bff0358f54e693c0f5954f761059b1eaed993`; corrected/tested source **`596837f29377d4c3a9312384b7b8be975b306b2a`**. GPT-6.1 Sol Medium recommended, Standard bounded fixture work; GPT-6 Sol Medium fallback. No production authority modification or escalation.

Only source file changed: `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonAnalysisSceneTests.cs`. Added a separate test-owned bootstrap readiness procedure, bounded read-only phone ownership diagnostics and exact post-restoration assertions. Original gameplay, floor, navigation, invalid/stale modal, scroll/accessibility and exact-byte assertions remain. Production source, recovery/save semantics, W/R/A/C/S/Q, schemas 7–17, tuning, localization assets, R1, Combat C1 and P2–P5/P7 are unchanged. No runner/package/supervisor modification or new framework.

## Reopen: first failure, correction and evidence

The baseline guard correctly rejected an active recovery/boot obligation after reload. `Save != null` and a constructed World were insufficient evidence of completed boot. The correction waits separately for the existing coordinator, keeping the real root and its prebound TimeService enabled. It supplies current foreground/resume observations only when required through the existing `OnApplicationPause(false)` / `OnApplicationFocus(true)` lifecycle callbacks, at the unchanged validated controlled cut. It neither backdates callbacks nor changes recovery flags, clocks or time boundaries.

The explicit completion condition requires boot/pumping/dirty/publication flags clear, no active/deferred recovery, null episode, closed foreground state and observed foreground. The wait is bounded to 64 normal Unity frames, a fixture orchestration ceiling rather than a gameplay/recovery limit. Persistence retry/failure/stall/uncertain-authority states fail rather than being retried or bypassed. The existing strict `StabilizeReadOnlySceneBaseline` is unchanged and still refuses unresolved recovery.

For this fixed-clock zero-eligible reopen, a copied monster state and the **original bound** validation context's recovery rules predict legitimate readiness spending using existing `MonsterContinuity.Recover`; no configuration/resource reload, alternative formula or wallet write is used. Applying readiness again to that expected copy must debit nothing further. After real boot closure, assertions check the predicted wallet/monster state, exact W/context identity, A/C, total ticks and complete run history. Expected state is never published. This permits legitimate recovery effects while detecting replay, loss or duplicate spending in this scenario.

Exact session/disk agreement is required after closure. Before/after file hashes are evidence, **not an equality requirement across recovery**. At the observed fixed cut the final hashes happened to match; intermediate valid episode publications were allowed. A fresh exact-byte baseline is captured only afterward, and subsequent Analysis navigation retains the original exact-byte comparison. The companion JSON retains bootstrap and baseline traces. This does not replace nonzero-duration, crash or protocol-cut qualification.

## Phone: first hidden failure and ownership

The unchanged phone identity independently passed **1/0/0** at starting source. Running navigation immediately before phone at diagnostic source reproduced **0/2/0**. The bounded trace first resumes phone after its second `AnalysisPhoneOpenFromEdit` (yield 8), with captured root/content-owner/string-map/UI-document references all null while the current scene root, content service and controller remain live. It has no entry trace. Immediately after the before-yield-9 marker, the source evaluates `ui.Q<Button>("confirm")`; this is the first null dereference before the later `strings[...]` restoration throws and masks it. That first-access conclusion is source/trace-derived, not a fabricated NUnit stack.

The installed Unity Test Framework source saves the active test enumerator's program counter before a domain reload and restores it into the resumed test enumerator. A predecessor failure inside a nested iterator can leave a positive continuation. `EditModeRunner`, `EnumeratorStepHelper`, `EditorEnumeratorTestWorkItem` and `TestEnumerator` provide the source-backed explanation for the observed mid-body resume with uninitialized captured locals. Their exact read-only file hashes are recorded in JSON. No private iterator state was forced and no framework fix/upgrade was attempted.

`ContentService` creates its string map as a readonly dictionary and clears/refills it in place. There is no demonstrated production map replacement or UI-lifetime defect here. Correcting the predecessor's premature readiness assertion removes this sequence's trigger. The phone algorithm and its temporary localized changes remain intact; cleanup has **no null guard**. It restores the exact original values and then asserts the same root, ContentService, dictionary and UIDocument ownership plus exact restored strings. Nothing catches or suppresses an NUnit assertion/log failure.

Corrected focused and ordered runs both start phone at entry, retain non-null matching owners through every traced yield, execute invalid/stale modal/navigation/accessibility coverage and reach exact restoration. Read-only traces are bounded to 128 records in this one method and contain identities/state only, not raw saves/auth data. They are retained to distinguish future continuation corruption from genuine UI failures. The broader runner limitation after other failing tests remains a triage risk; no historical verdict is reclassified and no universal framework defect fix is claimed.

## Automated qualification

| Campaign | Source | Pass / fail / skip | Meaning |
| --- | --- | --- | --- |
| an01 `1f88123a8a9e42d4bcd16de0fd98385c` | `92d91003` | 1 /0 /0 | Phone independently; all five stages passed |
| an02 `ee42963b62d241508715f87d0d3fbe66` | `173bff03` | 0 /2 /0 | Original ordered failure reproduced; permanently FAILED; guarded normal-failure finalization verified |
| an03 `a703ae8f1cb24c119750ce615bd177a4` | `596837f2` | 7 /0 /0 | Both corrected targets +5 direct controls; all five stages passed |
| an04 `8a8dc6e0e4ca448dab9a193940d40792` | `596837f2` | **21 /0 /0** | All 13 Analysis identities in normal relative order +8 controls; all five stages and independent verification passed |

The final scope is not narrowed: every declared leaf passed with no skip/failure/inconclusive result. Compile-only diagnostics are not test passes; overlapping cohorts are not summed. The old discovery inventory still uniquely identifies these unchanged method names; actual XML leaf names/IDs and counts are independently reconciled.

All final names have NUnit prefix `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.`:

- `AnalysisCleanAndModifiedDraftContinueDiscardSave` — Passed
- `AnalysisDoesNotInterruptAuthoritativeForegroundAdmissionAndSettlement` — Passed
- `AnalysisFailedDiscardAndFailedSaveNeverNavigateOrLoseDraft` — Passed
- `AnalysisInvalidDraftOffersOnlyDiscardAndContinue` — Passed
- `AnalysisMissingLocalizationUsesExistingAvailabilityFeedback` — Passed
- `AnalysisNavigationRetainsFloorAndReadOnlySaveAcrossShellReopen` — Passed
- `AnalysisPendingAndStaleSaveRemainDisabledWithDraftIntact` — Passed
- `AnalysisPhoneInvalidAndStaleDialogsRemainAccessible` — Passed
- `AnalysisPhoneNavigationSafeAreaAndCoveredInput` — Passed
- `AnalysisPhoneOpeningDuringReplayClosesPresentationOnly` — Passed
- `AnalysisPhoneValidDraftChoicesUseTouchButtons` — Passed
- `AnalysisReplayEntryImmediatelyUsesCompactHeader` — Passed
- `AnalysisResponsiveSafeAreaLocalizationAndScroll` — Passed
- `LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish` — Passed
- `LivingDungeonB3B2ProvenRollbackRetriesThroughRealSaveBarrier` — Passed
- `SceneClockInvalidMonotonicFailsClosed` — Passed
- `SceneClockInvalidUtcFailsClosed` — Passed
- `SceneClockNativeFractionAndOwnerRemainBound` — Passed
- `SceneClockRepeatedFractionalOrdinaryCutsDoNotDuplicateCredit` — Passed
- `UnresolvedRecoveryRetainsResolutionAndFailedDeleteCanRetry` — Passed
- `ValidRecoveryResumeKeepsCanonicalAndManaUnchanged` — Passed

## Preservation and remaining work

Independent receipt: `TestResults/h1-f4-20261027-an04/independent-completion01.json`. It verifies all four original ZIPs and their 1,494 members/hashes, final original inputs, pinned tools, exact selected identities, host/child exit identities and owned Job completion/absence, settings/TMP restoration, protected owner/historical evidence and 331 prior evidence directories. Phone entry/owner/restoration and bootstrap completion proof are independently checked, not inferred solely from pass counts. Reservation is null, previous `8a8dc6e0e4ca448dab9a193940d40792`.

The failed ordered diagnostic retains its original FAILED verdict and raw XML; its later three supervisor stages did not run. Its separate guarded normal-failure finalization preserved current settings and verified exact restoration under the existing Lease. No exceptional recovery, force release, process termination, owner-save restoration or evidence deletion occurred. A preparation rejection checked old-source current applicability rather than recorded completion; it stopped before changes/launch and is preserved separately. No historical FAILED/INCOMPLETE/UNCERTAIN verdict changed.

Original-84 exact-name ledger: **78 qualified /6 still without qualified passing evidence**, adding exactly these two identities to the prior 76. This is not a new full-suite failure total. Remaining six: Combat C1 target feedback, Combat C1 party traversal, graphical-floor invalidation, stale graphical-floor confirmation, room-content mouse/touch recovery and historical visual replay. Exact names are in JSON. Next dependency-correct packet: investigate the two Combat C1 fixture configuration paths without post-binding mutation; do not assume other scene failures share the runner symptom.

Unit: fixture invariants and existing invalid-clock/duplicate-credit/publication negatives passed. SIT: actual isolated Windows Editor production scenes, real canonical recovery/bootstrap publication, filesystem readback, UI/draft/lifecycle and save/reopen controls passed. No production defect was established or modified. UAT remains a later independent-authorized gate and was not started. Full EditMode, applicable full PlayMode/wrappers, remaining interruption/repeated-crash/reopen, isolated native Windows startup/gameplay/shutdown and matched performance/memory remain pending. No sustained profile, full suite, Player, benchmark or owner UAT ran in this packet. PR #259 stays draft; no ready-state change or merge.
