# PR #247 foreground performance correction

Status: bounded correction technically qualified; substantial synchronous stalls remain. Draft PR #247 is pending independent review, not owner performance acceptance.

Recommended configuration: GPT-6.1 Sol, High. Classification: Complex. This is a bounded correction to the already approved two-floor contract, with no automatic model fallback.

## Source and scope

- PR: <https://github.com/gdg3417/Dungeon-Lord/pull/247>, Draft and unmerged.
- Branch: `codex/living-dungeon-two-floor-foreground`.
- Reviewed implementation: `4008980ffbd42ff9d25969dcca3a2663556d2177`.
- Runtime correction and qualification source: `c3a21ba58a80174e2732c497b2ce1e0361a26fc3`.
- Prior qualified Unity source: `3040ddc4b87b0f70eac4046c3a7a4c48c5ec12da`; its Unity inputs equal the reviewed head's.
- Fresh isolated campaign: `TestResults/living-dungeon-two-floor-perf-20261021`.

## Measured redundancy and correction

The reviewed two-floor Windows capture recorded 3,727 detached preparations (880,415 ms inclusive), 51,166 restores (386,355 ms), 60,067 checkpoint-reader calls (455,677 ms), 69 admission lifetime probes (78,560 ms), and 3,010 atomic-write scopes (258,694 ms). These scopes overlap; their times must not be summed. Detached preparation and replay dominate the recorded work.

Two exact duplications were removed:

1. `DetachedCanonicalSaveSession.PrepareReplacement` fully validated the same predecessor bytes under the same context twice: once to reconcile monster continuity, then again to inspect release provenance. The session owns a readonly cloned byte array and returns copies. Reconciliation copies retained monster records, and serialization only reads the detached result. The second use now reuses that method-local validated predecessor. Candidate validation, exact release permits, predecessor checks, atomic publication and durable readback remain intact. Nothing survives this preparation call.
2. Each format-3 event fold serialized the same combat inspection twice: inside the historical combat fold and again inside the transfer envelope. The combat fold now returns its freshly serialized inspection to the immediate transfer-envelope caller. The original overload delegates to the same routine. No execution, inspection or validation cache survives the call; the outer evidence domain and bytes remain unchanged.

No transfer timing, event count, admission lifetime probe, workload envelope, event order, revision rule, schema, checkpoint version, release authorization, recovery rule or gameplay configuration was changed. Read-only foreground inspection still validates through its established path. The correction does not batch or defer authoritative consequences.

## Equivalence and integrity

Three new tests compare every prefix against the pre-correction format-3 composition for authentic EXIT, vacant DESCEND, and blocked/released DESCEND. They compare digests, complete checkpoint bytes, restored/re-encoded bytes and unchanged StartIdentity; the diagnostic folding loop performs no additional combat work. Existing historical golden, source-occupancy, authenticated-vacancy, overlapping-snapshot, equal-time, v1, reopen and failure-recovery tests remain required.

The focused `40-boundary-edit` run passed 208/208 at the correction source. Its safeguard index contains 55 relevant passing cases. Diagnostic fold timings are not a system performance threshold.

## Qualification attempts and preservation

The existing approved preservation adapters are reused with a fresh product, save namespace, output directory and build method. The wrapper additionally redirects both two-floor evidence-producing test files into this campaign. The preflight still checks exact source applicability, protected files, historical evidence and unused output paths.

- CLI remains `1.0.0-beta.13`; Editor remains `6000.3.2f1 (a9779f353c9b)`. No other tool upgrade.
- Preparation archived 1,410 Unity inputs and hashed 12,294 protected save/build files plus 10,624 historical evidence files.
- Attempt `41-full-edit` was rejected before launch because the passing focused UI tests generated a different TMP fallback asset. The existing checkpoint procedure archived/restored that asset and generated ProjectSettings bytes. No test ran in attempt 41 and no guard was relaxed. Retry is attempt 42.
- Matched native inputs copy the exact prior Windows seed bytes, with only external product/save-file manifest metadata changed. One-floor seed SHA-256: `5eadd0feb3881e38283189b423491204b683a514d642b49de833792679d1380c`; two-floor seed: `46216ab8b95be6e8843875def43b75123e8f6ac778c3ea704c77e393694a4084`.

The completed runtime gates are recorded below. Final preservation closure is recorded separately. The original campaign and failed-attempt evidence remain preserved. Owner gameplay testing is pending independent review.


## Completed technical gates

| Gate at `c3a21ba` | Verdict |
| --- | --- |
| Focused EditMode | 208 passed, 0 failed, 0 skipped |
| Full EditMode | 2,331 passed, 0 failed, 1 existing explicit skip / 2,332 |
| Full PlayMode | 3,728 passed, 0 failed, 10 existing explicit skips / 3,738 |
| Fresh Windows Development | Success; CLI beta.13, Editor 6000.3.2f1 |
| Native wait / reopen-release / reopen-finish | All passed; original A/B identities retained across each continuation, active edit, authenticated retirement/release and exactly-once history |
| Native unfinished v1 | Passed; original execution, single settlement, suppression, future opportunity and automatic v2 transition |
| Matched profiles | 68 completed runs in each of four Editor/Windows one/two-floor cases |

The counts overlap between modes/filters. They are not a unique-test total. Full EditMode took 74m07s, PlayMode 20m42s, and the Windows build 1m02s, excluding the separate preservation preflights. The build succeeded with retained licensing-client validation, symbol/debugger and other unfiltered diagnostics; success does not mean a warning-free log. Exact commands, XML hashes, skip names/reasons, build provenance and native launch records are in [the correction ledger](evidence/living-dungeon-two-floor/performance-correction/qualification-ledger.json).

Reproduction uses the bundled Python and `Tools/Presentation/qualify_two_floor_performance.py`: `prepare`, `isolate-identity-test`, `run boundary-edit 40`, `checkpoint 40-boundary-passed`, `all-required 42`, the six recorded `two-floor-smoke` invocations, then `finish`. Attempt 41 remains recorded as a prelaunch rejection. A new run needs fresh unused namespaces/outputs; do not reopen a closed campaign or use owner saves.

```text
unity test C:\Dev\Dungeon-Lord --mode EditMode --output <campaign>/42-full-edit.xml --timeout 7200 --no-color -- -logFile <campaign>/42-full-edit.log
unity test C:\Dev\Dungeon-Lord --mode PlayMode --output <campaign>/42-full-play.xml --timeout 7200 --no-color -- -logFile <campaign>/42-full-play.log
unity build C:\Dev\Dungeon-Lord --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonTwoFloorPerformanceWindows --allow-dirty-build --timeout 7200 --no-tail --log-file <campaign>/42-build.log --no-color
```

Here `<campaign>` is `TestResults/living-dungeon-two-floor-perf-20261021`. Source dirtiness was the documented isolated product/save/evidence substitution, plus this report; applicability independently verifies the exact committed Unity source and allowed transformations.

## Matched timing results

All values below are milliseconds, mean / p95 / maximum. The instrumentation, production timing, 1280×720 Default text layout, quarter-second injected qualification clock, 68-run target and configured validation/persistence bounds are unchanged. These are observations, not a new performance acceptance threshold.

| Platform/workload | Before event update | Corrected event update |
| --- | --- | --- |
| editor-one | 414.739 / 552.165 / 768.314 | 351.347 / 454.941 / 616.096 |
| editor-two | 744.966 / 991.702 / 1,482.216 | 622.350 / 840.093 / 1,256.639 |
| windows-one | 307.957 / 445.714 / 481.043 | 267.554 / 372.134 / 404.420 |
| windows-two | 538.089 / 732.171 / 1,168.427 | 468.954 / 618.602 / 1,043.496 |

| Windows phase | Before | Corrected |
| --- | --- | --- |
| One-floor admission | 615.273 / 647.767 / 657.641 | 519.358 / 549.944 / 559.202 |
| Two-floor admission | 2,138.105 / 2,241.040 / 2,282.163 | 1,919.193 / 2,011.483 / 2,029.486 |
| Two-floor release-tagged | 1,761.764 / 1,870.769 / 1,893.522 | 1,422.660 / 1,537.420 / 1,569.740 |

| Complete-frame maximum | Before | Corrected |
| --- | --- | --- |
| editor-one | 949.508 | 788.404 |
| editor-two | 2,522.892 | 2,364.702 |
| windows-one | 1,056.805 | 1,016.453 |
| windows-two | 2,282.511 | 2,030.011 |

| Event-frame allocation (decimal MB), mean / p95 / maximum | Before | Corrected |
| --- | --- | --- |
| editor-one | 177.699 / 244.490 / 246.308 | 148.117 / 199.801 / 201.281 |
| editor-two | 331.715 / 423.961 / 622.600 | 276.100 / 346.957 / 499.047 |
| windows-one | 117.516 / 181.213 / 182.573 | 102.127 / 151.381 / 152.518 |
| windows-two | 222.441 / 315.015 / 497.246 | 192.601 / 263.335 / 404.018 |

Thread allocation counters are unavailable and remain null, not zero. Frame allocations are supported but include engine/controller/render work. Complete-frame, main-thread, idle, passive, admission, settlement, recovery and release distributions remain in the four linked summaries; phase tags can overlap. Scope costs are inclusive and nested.

## Workload and verification counts

Counts are before → corrected. Atomic-write counts are instrumented scopes, not an independent OS disk trace.

| Case | Prepare | Restore | Checkpoint reader | Atomic-write scopes | Lifetime probes | Inspect |
| --- | --- | --- | --- | --- | --- | --- |
| editor-one | 2,657 → 2,657 | 14,416 → 12,308 | 15,640 → 13,532 | 1,091 → 1,091 | 68 → 68 | 1,091 → 1,091 |
| editor-two | 6,735 → 6,735 | 70,636 → 60,614 | 79,537 → 69,515 | 3,010 → 3,010 | 69 → 69 | 3,010 → 3,010 |
| windows-one | 1,568 → 1,568 | 10,608 → 9,452 | 11,832 → 10,676 | 1,091 → 1,091 | 68 → 68 | 1,091 → 1,091 |
| windows-two | 3,727 → 3,727 | 51,166 → 45,646 | 60,067 → 54,547 | 3,010 → 3,010 | 69 → 69 | 3,010 → 3,010 |

The native two-floor workload retains 2,869 event-category frames, 69 admissions, 68 settlements, 6,256 concurrent frames and 34 blocks/releases. Native one-floor retains 884 event-category frames. Retained checkpoint sums remain 24,242 / 73,719 bytes (the latter is two checkpoints, each below its individual envelope); final complete saves remain 94,759 / 216,802 bytes. No workload limit was enlarged. Native one-floor took 5m35s versus 6m31s; two-floor took 26m10s versus 30m04s. Those execution times are distinct from preflight, orchestration and monitoring time.

## Exact output and checkpoint comparison

Both native profile final saves match the baseline **byte-for-byte after only** `primary.lastSavedUtcUnix`, `primary.lastPausedUtcUnix` and `primary.lastResumedUtcUnix` are replaced with zero for comparison. No gameplay field, Revision, checkpoint identity, StartIdentity, checkpoint chunk, monster condition, history entry, allocator, horizon or opportunity is normalized. The fixed QA funding step precedes admissions, so later real boot UTC does not change foreground profile inputs. The input seed bytes match their original hashes.

All four profile boundary projections match exactly on category, numeric sequence, ordinal, party count and checkpoint byte count: 1,210 one-floor and 3,129 two-floor rows per platform. Only `between-events` rows are excluded from this projection; no committed gameplay boundary is excluded. Both native frame counts also match completely; Editor idle-frame counts differ with startup/frame partitioning. The exact normalized hashes and source-report hashes are in [comparison.json](evidence/living-dungeon-two-floor/performance-correction/comparison.json). Every-prefix reference tests separately compare full format-3 evidence/digests and re-encoded checkpoints, including Blocked and Released; historical format-1/format-2 goldens also pass.

The separate continuation smoke does **not** reset funding. Its old and corrected launches received legitimate different offline passive mana at different wall UTC; mana, captured start identities and mana-derived scores therefore differ across those independent scenarios. Logs record both persisted offline grants. Those scenarios establish functional continuation and preservation of each run’s own identities, not cross-build byte equivalence. These differences are not erased or presented as passing exact comparisons. The controlled native profiles provide the complete-save equivalence evidence.

## Remaining responsiveness risk

Native two-floor event mean decreases about 12.8%, admission mean about 10.2%, release mean about 19.2%, event-frame allocation mean about 13.4%, and maximum complete frame about 11.1%. This bounded correction is measurable, but it does not resolve responsiveness: normal event frames still average 469 ms, admissions average 1.92 s, releases average 1.42 s, and the observed complete-frame maximum remains 2.03 s. No mobile claim or owner performance acceptance follows from functional passes.

The remaining work is still synchronous full preparation/validation and authenticated prefix replay, plus admission lifetime assurance and durable persistence/readback. Eliminating more would require a separately proven reuse boundary; this correction introduces no cross-transaction or cross-horizon cache. Options for review are to accept this bounded reduction with the recorded limitation, or authorize another narrowly measured reuse investigation with equivalent proof and requalification. No speculative refactor or weaker verification is included.

## Changed implementation and qualification files

- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatCheckpointCodec.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/TransferCheckpointCodec.cs`
- `Assets/_Project/Tests/EditMode/LivingDungeonTwoFloorTests.cs`
- `Assets/_Project/Editor/DungeonVisualBuildQualification.cs`
- `Tools/Presentation/qualify_two_floor_performance.py`

This report, the original qualification cross-reference, the complete changed-file inventory and the seven correction evidence files document the result. There is no new gameplay/config/localization asset or schema migration. The PR remains Draft/unmerged for independent review; owner gameplay testing has not been requested.

## Final preservation and publication

All 1410 Unity input files exactly match their pre-campaign bytes, including 740 settings/package/importer inputs. All 12,294 protected save/build files and 10,624 historical evidence files are byte-identical, with zero exceptions. All temporary substitutions are restored, and all 303 corrected-build seal files reverify. Existing builds and original evidence were not overwritten. See [preservation-summary.json](evidence/living-dungeon-two-floor/performance-correction/preservation-summary.json).

Current player: `Builds/Development/Windows-living-dungeon-two-floor-perf-20261021/Dungeon Lord.exe`. Gameplay DLL SHA-256: `e92de8776ae20b6dad5299fc710f4b5eecc2f230890095f7cdf44fc7e7dcd5cc`. Packed managers: `2da3ad42d02b0dc63ddf55d186fb788d9b3d95a54272d104f671132d65505257`. Complete seal SHA-256: `1c3bc25caa6e4c720a6092ff9605211da3fb189babe62de2595dfcb46588229f`.

Latest origin/main was fetched again and remains `76f78c5c2d17d3aab916f297aecb5c25cf146d76`. Runtime/build input source remains `c3a21ba58a80174e2732c497b2ce1e0361a26fc3`; closeout changes only documentation/evidence. The final PR head is recorded in PR metadata and the final report. The [complete file inventory](living-dungeon-two-floor-changed-files.md) lists every feature path. Writable schema 15, frozen formats, configuration ownership, v1 draining, source occupancy and authenticated release are unchanged. No merge or owner gameplay request is authorized by these results.
