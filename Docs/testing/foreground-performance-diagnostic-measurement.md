# Foreground reconstruction and validation measurement

Source baseline: `e7667c4e57b7b1c9cfcf04ecea0084be37ac5f9c` (`origin/main`, fetched 2026-10-24; merged #249 and #250). The initial `feature/phase7-production-analysis` checkout was clean at `ca8093674feb7aafc8e0ef12785d26316b498b0d`; it remains preserved. Work began on `codex/foreground-validation-cost-instrumentation` directly from main.

Recommended configuration: **GPT-6.1 Sol, High; Complex**, focused cross-system instrumentation with established authorities. The user-specified availability fallbacks are GPT-6 Sol High, then GPT-5.6 Sol High. No fallback recommendation was selected. The active version-2 model policy treats this as advice, not an instruction to change the executing model.

## Objective and scope

Reconcile [the merged audit](foreground-performance-architecture-audit.md) with current code and measure the origins of validation/reconstruction. This PR implements no optimization. It changes no gameplay tuning, localization, configuration table, package, schema, migration, timestamp meaning, save frequency, ordering, authority, checkpoint encoding or admission/release/retirement obligation. It leaves the separate research persistence-acknowledgement issue unchanged.

Reviewed authorities include the save/time and migration specifications (17/28), cross-spec invariants, development rules, B2/B3A/B3B1/B3B2 and two-floor contracts, owner design lock, foreground root/service coordination, checkpoint readers, lifetime assurance, history authorization, canonical sessions, release lineage, atomic persistence, production fixtures, profile driver and preservation adapters. Later production/two-floor contracts supersede historical dormant-only descriptions; historical readers and budgets remain applicable. Writable schema remains 15.

The existing sustained profile bridge runs 68 completions. It has no bounded native selector. No historical adapter was edited, no sustained method selected, and no native player/build was launched. A new bounded **test** uses the approved production fixture and timing with one admission, the intact writer, and an in-memory deterministic filesystem. Its inspection call follows successful publication. The existing fixture supplies a no-op passive-tick callback; counters and horizons advance, but GameRoot passive-mana/research processing is not measured. It has no production scene/controller or real disk durability coverage.

## Measurement contract

Historical `Area` identifiers, order, counters and inclusive meanings remain intact, including AtomicWrite containing semantic readback and LifetimeProbe excluding the initial probe restoration. The additional system requires **both** `ForegroundQualificationMetrics.Enabled` and `DetailedEnabled`, and compiles activation only in Editor/Development. Both flags default false. It has no player UI, persisted state or gameplay consumer. The sustained driver still selects only its existing metrics. Detailed buffers and fixed-name markers initialize lazily on explicit diagnostic use.

Fixed markers use `DungeonLord.Foreground.<Boundary>`. Records contain boundary, inherited origin, party sequence, expected replay prefix depth, nesting level, parent record, inclusive ticks and selected-boundary self ticks. The limit is 4,096 records and 64 simultaneous scopes (technical storage limits, not gameplay limits). Dropped-record/scope counts are explicit; fixed origin/count aggregates continue after record saturation. Formatting, grouping, provenance hashes and byte comparisons in tests run outside intact timed actions. No extra inspect/export/serialization/parsing/hashing/reconstruction is performed to label runtime scopes.

`SelfTicks = elapsed ticks - immediate measured children's elapsed ticks`. The tests verify that conservation identity for every retained scope in each unsaturated action, including failures. This is **exclusive relative to the selected scopes**, measured with Stopwatch on the synchronous main thread. It includes unmarked calls, instrumentation overhead, GC and waits; it is not hardware CPU execution time or Unity Hierarchy self time. Recursive assurance totals overlap. Never sum inclusive rows as independent costs.

Supported-reader remainder includes envelope checking, decoding, hashing, exact re-export/comparison and other unmarked work. StartRecreation includes decoded snapshot/config reconstruction, engine setup and its original start comparison. PrefixReplay isolates the existing engine-step loop in formats 1/2/3. Pump self still includes private JSON cloning, demand/actual progression, consequences and other unmarked work; Stage self includes recognized capture, serialization and private projection. These components are not separately attributed in this PR.

DurableFileSystem encloses the existing atomic **protocol**, including comparisons, path/evidence work and hashing. Its selected self excludes the instrumented semantic callback. It is not a raw disk-I/O marker. SemanticReadback includes fresh session validation/reconstruction and projection. Rollback, stable failure codes, exact byte checks, release capability checks and publication order are unchanged.

The bounded Editor test attempts CPU Timeline (RawFrameDataView) and Hierarchy collection and exports those rows and a `.data` profile **only if Unity supplies a frame stream**. On this host, batch EditMode returned `-1..-1`; no CPU samples or `.data` profile were obtained. This is recorded as unavailable, never as zero CPU cost. A short attempted empty-scene play transition could not use the fixture and was removed; its failed attempt is retained. The final test always verifies enabled/disabled exact bytes, independently of profiler availability. Interactive CPU Timeline/Hierarchy nesting and exclusive CPU costs remain unverified. The marker scopes are ready for that capture without adding dynamic names. API references: [Unity RawFrameDataView](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/profiling/rawframedataview), [Unity profiler bindings](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ProfilerEditor/Public/ProfilerAPI.bindings.cs).

## Evidence and results

The committed evidence directory is [foreground-validation-cost-20261024-01](evidence/foreground-validation-cost-20261024-01). Its report pins exact test selections, commands, results, source/config/tool hashes, output hashes, availability and preservation. The ZIP contains NUnit reports, the exact regression command/filter and bounded diagnostic records. Raw Unity authentication/licensing logs, player saves and the complete private path inventory remain local.

Tested runtime/test commit: `231e0f549240d4e37257304950059878ae0734e0`. Final established Unity CLI selection: **55 passed, 0 failed, 0 skipped**, 60.899 seconds of reported test duration. The prior focused run had 11/11 passing; counts overlap and are not added. Exact names and commands are in `report.json` and the ZIP. The final source run includes the corrected record export.

```powershell
& TestResults/foreground-validation-cost-20261024-01/focused-regression-final.ps1
```

The archived script invokes Unity CLI `test C:/Dev/Dungeon-Lord --mode EditMode --filter <exact 27-selector semicolon filter> --output TestResults/foreground-validation-cost-20261024-01/focused-regression-final.xml --timeout 1800 --no-color -- -logFile TestResults/foreground-validation-cost-20261024-01/focused-regression-final.log`. It excludes sustained profiles, exporters and the bounded 128-opportunity production test. Raw logs remain at that local path and include normal licensing-client signature/debugger diagnostics; a passing verdict is not a warning-free log.

One production admission plus inspection: **17 complete validations = 1 initial + 7 prior + 7 candidate + 1 final predecessor + 1 durable readback**. Seven stages comprise 3 passive ticks, 1 opportunity and 3 closures. **138 supported-reader calls = 129 lifetime calls + 9 canonical-party restorations**. Normal lifetime prefixes: 72 calls, depths 1–72; alternative path: 57 calls, depths 18–74. The nine restorations split into four owner validations, two release checks, one initial admission, one execution and one post-publication inspection. There are 499 bounded records and zero dropped records/scopes.

| Boundary | Calls | Inclusive ms | Selected self ms |
| --- | ---: | ---: | ---: |
| StagePreparation | 7 | 295.029 | 68.679 |
| PriorValidation | 7 | 98.976 | 0.010 |
| CandidateValidation | 7 | 117.654 | 0.011 |
| CompleteValidation | 17 | 258.897 | 237.913 |
| Restoration | 9 | 43.666 | 0.036 |
| SupportedReader | 138 | 1101.803 | 302.614 |
| StartRecreation | 138 | 381.143 | 381.143 |
| PrefixReplay | 138 | 418.046 | 418.046 |
| AdmissionReconstruction | 1 | 1176.379 | 0.006 |
| AdmissionAssurance | 2 | 1681.405 | 110.898 |
| FinalPredecessor | 1 | 11.579 | 0.004 |
| SemanticReadback | 1 | 19.625 | 1.164 |
| DurableFileSystem | 1 | 20.408 | 0.783 |
| PostPublicationInspection | 1 | 7.407 | 2.518 |
| Feasibility | 1 | 8.469 | 8.469 |

The two AdmissionAssurance calls recurse (normal contains the alternative), so their inclusive aggregate overlaps itself and must not be compared as an independent total. StartRecreation and PrefixReplay are children of SupportedReader; the remainder is separately visible. FinalPredecessor/Prior/Candidate self is only wrapper overhead; their substantive validation remains nested.

Empty-scope calibration (4,096 scopes per sample): disabled 0.0483, 0.029 ms; enabled 0.2883, 0.2875 ms. The production ordered wall pair is enabled 1,725.713 ms and disabled 1,564.095 ms. This single pair is not a calibrated instrumentation overhead estimate or a gameplay performance comparison. No speedup was measured or implemented.

## Call-origin conclusions

1. Each stage independently validates its prior and candidate complete bytes, then performs the existing release-control check. Stage origins distinguish opportunity, passive tick, closure, recovery, terminal pin, settlement and release. Stage counts are not disk commit counts.
2. Complete-save owner validation reconstructs each retained party; the supported reader recreates captured start/setup and replays the expected ordinal. A release-control check can reconstruct parties again after candidate validation. Parent records distinguish these paths even when they share a stage origin.
3. Acceptance validates the owner directly; execution restores once per pump-local dictionary entry. Initial admission restoration and assurance are separate. Normal assurance verifies each prefix; authentic DESCEND also explores and verifies the alternative blocked/released path. The alternative is private hypothetical work, not another admitted party.
4. Terminal settlement has its own predecessor validation and retirement lineage reconstruction. The final writer validates the original predecessor independently. Fresh durable semantic readback opens/reconstructs the published bytes. Post-publication inspection separately restores retained parties with presentation steps.
5. The bounded driver reaches an opportunity through configured passive boundaries; those boundaries explain its seven private stages. This does not explain the historical sustained Editor/Windows stage-count difference. That difference remains an evidence gap; this PR does not rerun or reinterpret those campaigns.

## Equivalence and reuse authority

Enabled/disabled scenarios compare **full bytes**, not only hashes: canonical sessions, retained checkpoints, immutable start identities and reconstructed event traces. UTC is injected identically; no timestamp, revision, outcome or gameplay field is normalized. Cases cover ordinary/admission/settlement paths, authentic blocked/released states, stale predecessors, rejected semantic readback and failed replacement. Historical formats 1/2 re-encode exactly, with corrupt-input negatives. The production one-admission comparison also checks complete canonical bytes exactly.

No predecessor/context equivalence or reusable validation authority is established by these measurements. Runtime records intentionally retain no save/context objects. Provenance hashes and matching IDs do not prove reusable authority. Validation graphs expose mutable ownership, and checkpoint restore must create independent mutable execution. **Further isolated exact-input/context and alias analysis is necessary before any work may safely be eliminated.** No duplicate-predecessor elimination or cache is claimed safe here.

Exactly one proposed next optimization: **admission-local reuse of immutable decoded start inputs**, retaining every existing normal/alternative prefix replay, exact re-encoding, malformed-input rejection, work/byte limit and fresh durable verification. Start recreation is a repeated, independently measured boundary in the bounded production admission. This proposal requires a separate proof of complete input/context equality and independent mutable execution ownership before implementation; it is not authorization or a predicted speedup.

## Overhead, preservation and limitations

The interleaved empty-scope diagnostic reports enabled/disabled overhead separately in the evidence. It is a small synthetic calibration, not a whole-workload overhead correction. First-use fixed marker/buffer allocation happens before the first detailed span; the enclosing action may include it. Roughly 200 KiB of bounded record storage plus small arrays/marker metadata is retained after first explicit use. Per-scope struct recording requires no growing list or dynamic marker strings. Disabled calls still have branch/struct-call overhead.

Historical allocation counters retain their existing semantics; no allocation-site, retained-memory, per-boundary allocation, supported native-thread-allocation or GC-pause attribution is added. Short enabled/disabled wall-time pairs are order-sensitive and include normal noise/GC/JIT/Editor effects. They cannot establish a speedup or a reliable full instrumentation tax. No native, iOS, release-build, UI/render, lifecycle/shutdown or offline catch-up performance claim is made.

Preservation inventory started before runtime tests and completed before the first successful Editor import. Two licensing-only launch attempts produced no test verdict. The first completed run unexpectedly overwrote Unity's default owner-product `TestResults.xml` (a test report, not a save). Its exact original was recovered from retained `living-dungeon-b3b1-20261018/12-integration-play.xml`, verified against the pre-run SHA-256 `f9eccd6bb418bc6f4f03b71dd0272384262aa088d59675e7c99fd9d6362efc6e`, and restored with explicit filesystem approval. Later tests used a unique product name `Dungeon Lord Foreground Diagnostic 20261024 01`; original settings bytes were backed up and restored. This incident and failed attempts are disclosed rather than described as uninterrupted preservation.

After the final source-pinned run, **all 49,450 inventoried files reverified byte-for-byte, with zero mismatches and no previously missing historical paths**. Original project settings match the exact backup. No player save, prior build or historical evidence content changed at closure. The private inventory and raw logs remain local; committed evidence pins their verification and baseline SHA-256. All fresh diagnostic outputs have unique identities. The restored test-report incident above is the sole preservation deviation.

Architecture risks are diagnostic overhead, misleading inclusive/self arithmetic, incomplete CPU/backend coverage, exception nesting, origin inheritance, record saturation and accidentally enabling a campaign in ordinary play. Tests cover disabled/reset/bounds/exception behavior and nesting conservation. Scopes never authorize saves or consult metrics for gameplay decisions. No second writable authority, historical adapter changes, source-text substitution, production qualification campaign or schema/migration change is included.

## Changed files

- `Assets/_Project/Editor/DungeonSpatial/Tests/ForegroundPerformanceDiagnosticSceneTests.cs`
- `Assets/_Project/Editor/DungeonSpatial/Tests/ForegroundPerformanceDiagnosticSceneTests.cs.meta`
- `Assets/_Project/Scripts/Core/ForegroundQualificationMetrics.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/ExactCompleteSaveAtomicPersistence.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonContracts.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundLineage.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonLifetimeAssurance.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatCheckpointCodec.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatStartCapture.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunCheckpoint.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/TransferCheckpointCodec.cs`
- `Assets/_Project/Scripts/Services/SaveService.Foreground.cs`
- `Assets/_Project/Tests/EditMode/ForegroundPerformanceInstrumentationTests.cs`
- `Assets/_Project/Tests/EditMode/ForegroundPerformanceInstrumentationTests.cs.meta`
- `Docs/testing/evidence/foreground-validation-cost-20261024-01/diagnostic-reports.zip`
- `Docs/testing/evidence/foreground-validation-cost-20261024-01/report.json`
- `Docs/testing/foreground-performance-diagnostic-measurement.md`
