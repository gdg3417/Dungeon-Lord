# H1-F4 scene draft / lifecycle clock investigation

**Status: stopped at a safe checkpoint. A supported production-scene prebinding clock seam is missing; no speculative production hook or runtime correction was implemented. The historical draft loss was not reproduced and is not claimed fixed.**

PR: <https://github.com/gdg3417/Dungeon-Lord/pull/259>, still draft. Branch `codex/h1-f4-recovery`; base `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Starting head `bdc88cd418a85a6bd5770a244d6484d4685b8fcf`. Diagnostic/test/tool source: **`895e40f990e902052dcc76c0f163a96d9631742b`**.

Model recommendation: GPT-6.1 Sol High, Complex, for the specified cross-system scene/lifecycle investigation; GPT-6 Sol High availability fallback. No Astra use or recommendation change.

## What changed

Only read-only diagnostics and an explicit two-case supervisor selector were committed:

- `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenA4ProductionSceneTests.cs`: trace controller/draft state, root enable/focus/pause and recovery flags, feedback, save/session references and canonical SHA, W/A/C, and both TimeService references. Observe canonical publications and relevant UI clicks for the selected cases only.
- `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonRoomContentSceneTests.cs`: trace before EnterEdit, after construction confirmation and before/after each of the original twelve yields. The nested coroutine and original construction/capacity/reassignment/move/custody/discard/canonical assertions remain.
- `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonForegroundSceneTests.cs`: trace immediately before and after its existing clock replacement. Replacement behavior was not changed or approved by this diagnostic commit.
- `Tools/Presentation/qualify_h1_f4.py`: explicit `plan-scene-repro` / `run-scene-repro`, selecting exactly the two methods below through the existing supervisor.

Diagnostics never call a time sampler, barrier, writer or mutation. Reference hashes are process-local diagnostic identities, not stable save IDs. No production runtime, save fields, schemas, frozen readers, migrations, policy/tuning or profiling implementation changed. No assertion was removed, ignored or replaced by a null guard.

## Exact test outcomes

| Exact NUnit identity, after `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.` | Historical `16955152` XML | Diagnostic `895e40f9` |
| --- | --- | --- |
| `RoomContentMapReassignmentCapacityMoveOwnedAndDiscard` | Failed: NullReferenceException at historical line 304 | **Passed** |
| `LivingDungeonB3B2FramesBeforeCachedBoundaryDoNotReconstructOrPublish` | Passed | **Passed** |

Campaign **`2c2f6e2c5b0d477ba951871ba02d08e4`**: **2 passed / 0 failed / 0 skipped / 0 inconclusive**, exactly two discovered/root/leaf cases. All five selected stages PASSED. The cached-boundary case is a small diagnostic caller; its pass does not establish lifecycle-owner correctness.

The historical scene stack remains:

```text
System.NullReferenceException : Object reference not set to an instance of an object
DungeonBuilder.M0.Tests.PhaseSevenA4ProductionSceneTests+
  <RoomContentMapReassignmentCapacityMoveOwnedAndDiscard>d__164.MoveNext()
  ProductionDungeonRoomContentSceneTests.cs:304
UnityEngine.TestTools.TestEnumerator+<Execute>d__7.MoveNext()
  UnityEngine.TestRunner/NUnitExtensions/Attributes/TestEnumerator.cs:44
```

The machine-readable [investigation report](h1-f4-scene-clock-investigation-20261028.json) retains the complete original stack, both historical results, exact current test identities, all 41 diagnostic observations, stage/process/tool/input evidence and the independent verification receipt. Raw Unity logs remain local.

## A. Draft-loss evidence and its limits

Current run:

1. Root was enabled before setup disabled it, then remained disabled throughout construction. Root TimeService and SaveService's bound recoveryTime were the same object throughout this case. No recovery-required or foreground-failure flag appeared.
2. Before EnterEdit, Draft was absent as expected. After confirmation it was object `692215016`, one command, Pending, with `ui.dungeon.construction.confirmed` feedback.
3. At the first yield, the **same object** became Acknowledged. It remained available through all twelve yields and immediately after the nested coroutine returned.
4. The canonical session/save identities and canonical bytes remained stable throughout that yield interval. The original capacity, reassignment, Move-not-Reassign, unassignment, custody and discard/no-wallet/no-canonical-publication assertions then passed.

Thus **disabling GameRoot alone did not reproduce the historical loss**. No first draft-destroying operation was observed, so its exact historical root cause remains unknown. This is not evidence that a disabled root is always safe, that the historical failure was spurious, or that a production defect has been fixed. Diagnostic timing can differ from the original full run.

The two relevant scene test files had no changes between `16955152` and the reviewed `bdc88cd4` head. Three intervening runtime corrections touched relevant barriers (`57096beb`, `3bb229b9`, `b9e455a4`); source proximity does not prove that any one fixed this failure. The historical XML orders `RoomContentLootRejectionReasons` immediately before the failed capacity case. A bounded test-order/input reproduction is a possible next investigation; it has not been executed or claimed causal. There is no reason to rerun the other 556 historical names or the full suite for this investigation.

The corresponding PlayMode wrapper is `DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.RoomContentMapReassignmentCapacity`. Its direct neighboring save/reopen wrapper is `RoomContentMapReassignmentCategories`. Neither was launched in this checkpoint. No corrected scene behavior or complete PlayMode gate is claimed.

## B. Confirmed clock ownership mismatch

The trace around `InstallForegroundBoundaryTestClock` proves the mismatch without changing the helper's behavior:

| Observation | GameRoot.TimeService | SaveService.recoveryTime | Root clock deferred recovery |
| --- | ---: | ---: | --- |
| Before replacement | `1996960050` | `1996960050` | True |
| After replacement | `1332210424` | `1996960050` | False |

Save/session identity and canonical bytes are unchanged by replacement. The helper creates a new clock, attaches the already loaded save, initializes a horizon and uses reflection to install it only on GameRoot. SaveService still owns the first clock, including its classification/publication state. Replacing the root clock also loses its process-local deferred state. A passing cached-frame assertion does not exercise pause/recovery/ordinary publication sufficiently to detect this split.

This is a **confirmed fixture authority defect**, not a newly reproduced normal-production boot defect. The normal root constructs one TimeService and supplies that same instance to SaveService. No failure has been fabricated to turn the historical scene result into a runtime diagnosis.

### Existing seam audit

- `GameRoot.Recovery.cs:14–41`: `TryConfigureRecoveryRuntime` refuses a pre-existing TimeService/recovery bundle, constructs its own TimeService using SystemTimeSource (and the separately gated smoke monotonic source), begins the recovery foreground clock, then immediately calls `SaveService.ConfigureRecovery(inputs, TimeService)`.
- `SaveService.Recovery.cs:148–160`: ConfigureRecovery binds that observation owner before load and refuses existing canonical sessions or an existing recovery binding. Post-load reconfiguration is not a supported correction.
- `TimeService.cs` and `SimulationClock.cs`: the clock and UTC/monotonic providers are readonly constructor dependencies. There is no supported post-binding provider setter.
- `GameRoot.SetOfflineTimeSourceForTests` affects the older offline resolver. It does not feed the schema-17 bootstrap constructor or inject a monotonic provider.
- `AttachSaveServiceForTests`, `CompleteSuccessfulBootForTests` and the existing synthetic integration RootScope can attach already assembled services for focused integration tests. They do not inject providers into the normal Bootstrap-scene construction path; substituting such a synthetic boot would not qualify the requested production boot path.
- `LivingDungeonForegroundDevelopmentSmoke.ReopenClock` is restricted to separately identified smoke products/save names/CLI phases and returns a constant monotonic epoch. It provides neither controlled precise UTC nor an advancing scene-test pair. Broadening it or starting its smoke/profiling controller would exceed this milestone.

There is therefore **no existing supported normal-scene prebinding seam** for both required providers. Reflection edits of readonly clock state or post-binding replacement/reconfiguration are not acceptable substitutes.

The audit found **13 helper call sites** across ProductionDungeonForegroundSceneTests, ProductionDungeonForegroundReadabilityTests and ProductionDungeonAnalysisSceneTests, including shared ResearchActionFailure and ForegroundFailureRetry helpers. The JSON inventories each exact path/line/caller. The shared helpers reach the five Research rollback/uncertainty/foreground-failure scenarios and both foreground failure scenarios. Existing PlayMode shell wrappers invoke the same scene methods for Research and Analysis; they inherit this concern and remain pending. The profiling clock replacement has the same source pattern; it was reviewed only, with no benchmark or profiling refactor.

## Smallest proposal for independent review — not implemented

The user's instruction requires reporting when a safe prebinding seam is unavailable rather than inventing a production hook. Review either an established alternative or this narrowly scoped dependency seam:

1. Instance-local, `UNITY_EDITOR`-only, one-shot injection of **IPreciseTimeSource and Func<double> monotonicSeconds**, while the isolated scene root has no SaveService, TimeService or recovery bundle, before Start. Require the isolated product and unique `phase7a4-scene-<guid>.json` identity. Refuse late/duplicate injection. No static/global override.
2. The existing root constructor path creates **one** TimeService from the provider pair; its existing ConfigureRecovery/load/bind-loaded-horizon/attach paths retain that exact object. The test helper thereafter only advances the injected provider; it cannot replace/rebind owners or edit W/R/A/C.
3. Ordinary production behavior and release builds keep the existing system UTC/Stopwatch sources. Invalid samples continue failing through the current precise observation guards. No save field, schema, migration, tuning or persistence framework changes.
4. Author any directly affected synthetic policies before constructing the owned input bundle; never replace a consumed policy. Keep the provider pair and same owner through scene reopen and lifecycle observations as appropriate to each isolated scenario.

This requires review because it is a new scene-construction seam, even though it does not change recovery policy or add writable authority. **No such setter/provider hook is present in this commit.** Once cleared, add identity/late-injection/invalid-clock tests and qualify the corrected helper plus adjacent pause/focus/return/stall/ordinary-save/Research/fault/reopen cases and their direct PlayMode wrappers. Do not normalize expectations or fabricate observations.

## Qualification and preservation receipt

- Preparation: `TestResults/h1-f4-20261027-scene-repro01`; its inherited dated prefix is the actual explicit identity, not the execution date. Product: `Dungeon Lord B3B2 Smoke H1F4 20261027 scene-repro01`.
- Source `895e40f990e902052dcc76c0f163a96d9631742b`; Unity 6000.3.2f1; installed pinned CLI beta.14; nine exact executable/helper hashes retained in the plan/report.
- Workload ran `2026-10-28T11:17:05.084940+00:00` through `11:18:10.033835+00:00`. Host PID 52620, creation FILETIME 134376598250934577; runner PID 8612, creation 134376598252069336. Job `Local\DLQ-7c46bd5123a049428835b86b493da04b`. Host/runner/CLI exited zero with recorded owned Job completion. All five supervisor stages PASSED.
- NUnit XML SHA-256: `d85336a244d0de5c37d1f09f306d87fd0d3b5fd91fabb713ed76864db80db58e`.
- Independent final receipt: `TestResults/h1-f4-20261027-scene-repro01/independent-final-verification01.json`, SHA-256 `8704ad249f8b9308c8d1854761d6f9989072c8f8712c53a50babfbc3c7538b73`.
- Independently verified all 1,494 original inputs, three settings restored, unchanged font, nine tools, current process/Job absence, protected owner/save/historical maps and **73 prior campaign/preparation evidence directories**. No Unity input changed during the test stage; no font restoration exception was needed.
- Reservation is `campaign=null`, `previous=.../2c2f6e2c5b0d477ba951871ba02d08e4`. No uncertain reservation, exceptional recovery, force release or process termination occurred.
- All old FAILED/INCOMPLETE verdicts remain unchanged. Historical full XML remains SHA-256 `aac1bab11bf2ba8b942c5c9828250f02cec636f29456b70921c87a2abc0a1f53`. Later absence checks never repair missing historical descendant completion.
- Artifact inventory: `TestResults/h1-f4-20261027-scene-repro01/artifact-inventory01.json`; retain the exact campaign, original archive, input/tool maps, isolated namespace and observations through review. No cleanup or evidence deletion.

No full EditMode/PlayMode, Windows, benchmark, owner UAT, merge, unrelated factory or H2/O1 work started. Original/residual settlement, W/R/S/Q, accepted work, context binding and frozen schemas 7–16 remain unchanged.

**Next dependency:** independent review of the missing prebinding seam. The narrow next affected cohort should cover this same capacity case and its invalid-placement/save/reopen neighbor, then the corrected clock helper's pause/focus/stall/Research paths and direct PlayMode counterparts. The historical first draft-loss operation remains an explicit open investigation, not a resolved regression.
