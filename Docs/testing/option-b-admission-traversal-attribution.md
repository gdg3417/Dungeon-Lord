# Option B: admission-local immutable traversal attribution

**Recommendation: STOP B at this measurement checkpoint and return this packet for independent ChatGPT review.** Repeated preparation is measurable, but its eligible measured envelope is only **96.313–106.612 ms (7.54–8.47%)** of a **1,252.208–1,279.323 ms** intact admission action. Most of that envelope creates route graphs that still need independent mutable copies. Traversal planning itself accounts for **20.390–30.097 ms**. Copying, binding, validation and exact work accounting would consume part of any avoided computation. The evidence does not justify the additional ownership, memory and full qualification cost for this optional optimization ahead of returning to A.

This is an engineering recommendation, **not an invented numerical acceptance threshold** or a claim that all other work is irreducible. A small admission-frame benefit is plausible if an implementation produces positive net savings, but no optimization, counterfactual copying measurement or speedup is demonstrated. Independent review may assess materiality differently. Do not implement B or begin A automatically.

[Sealed evidence manifest](evidence/option-b-admission-20261110/report.json) · [All attributed distributions](evidence/option-b-admission-20261110/attribution.json) · [Raw benchmark, NUnit and reproducible analysis packet](evidence/option-b-admission-20261110/diagnostic-records.zip).

**Identity and scope.** Freshly fetched GitHub/default `main`: `1a477ed94b44a3227e00af9c8c16c8600c916400` (#264). Recent merged history was #264, #262, #261, #259, #258; no open PR existed at preflight. #260 and #263 were verified closed/unmerged. The owner checkout stayed on clean local main `4908722e7b275dbe59059d8b1b7d199ba9ef991a`; it was not advanced. Worktree: `C:/Dev/Dungeon-Lord/Temp/option-b-admission-attribution`; branch: `codex/option-b-admission-attribution`. **Tested diagnostic source/head: `4f47c07d596765d6b7e58f50f6f6a5fcf9735f0e`.** The earlier untested seven-marker commit `fd5b9f83d538dff48a3fc429f3d2d42ddf577d81` and all original preparation versions remain retained. Later report-only head identity belongs in the final review handoff; its Unity-input applicability must be compared with the tested map.

Recommended configuration: GPT-6.1 Sol, High; Complex, deterministic performance attribution and immutable ownership analysis. Fallback: GPT-6 Sol, High; no automatic Astra escalation. The executing session identifies itself as GPT-6-based Codex; its precise variant and reasoning setting are not exposed. No model switch or substitution is claimed.

Windows 11 `10.0.26300`, AMD64, Intel i7-12700KF, 20 logical CPUs; Unity **6000.3.2f1 / a9779f353c9b**, CLI **1.0.0-beta.17**, verified current; Python **3.14.0**, Git **2.54.0.windows.1**. The existing Unity CLI skill and unchanged qualification supervisor/H1-F4 adapter/input/process authorities were used. No build or standalone/mobile result is inferred.

**Protocol and exact identities.** The existing `AdmissionStartBenchmark.OneProductionAdmissionRepeatedSamples` ran without a fixture, timing, content, oracle or harness replacement. `LivingDungeonTwoFloorTests.ProductionDungeon` uses production run/content inputs, four rooms on two active floors, ten assignments and 64 traversable room tiles. Captured route-plan JSON totals 7,555 UTF-8 bytes; the route graphs include mutable arrays, assignments and nested room/spatial data. The injected saved UTC is 1000. The action advances from the closed zero boundary to the configured first opportunity at 30,000 logical units, then inspects published foreground state. The filesystem is the established in-memory fixture, and its passive-tick callback is a no-op; this does not measure real disk durability, rendering or GameRoot passive-mana/Research processing.

One warmup per enabled/disabled mode, then five measured samples per mode with alternating order. Identical complete saved input bytes were restored and compared before every action. Every output, including both warmups, matched full canonical bytes without normalization. There was no forced GC in the timed action. Calibration, assertions, grouping, hashes, JSON formatting and export ran outside it.

Every enabled warmup/sample retained **137 supported readers, 137 prefix replays, 10 complete validations, seven private stages, seven candidate validations, zero repeated prior validations, one final predecessor validation and one independent semantic readback**. These expectations were not changed.

| Retained fixture bytes | Length | SHA-256 |
| --- | ---: | --- |
| Complete input | 15,599 | `32cf5f732778db477bba7bc365abdafd96e257974464e2b809de23e25babb97a` |
| Complete output | 64,009 | `8c249829b25df2724a5a8182a09401f8405538e133ab5e31489e7a24eef54334` |
| Output checkpoint, format 3 / ordinal 0 | 36,092 | `740fd319e38c5aff68924a591b2540cfcb034b5f5d7170c05401acd3989583ee` |
| Wrapped start | 35,209 | `8778a966e255c898a69471e6e5f9783e1323576d259f153594a9524f750d62b9` |
| Inner combat start | 35,153 | `151a0fbd5015af494fabedcfc237cf6ebba94755b215521ec1bc7138263513b5` |

| Pair | Execution order | Enabled wall ms | Disabled wall ms | Supported thread allocation |
| --- | --- | ---: | ---: | --- |
| 0 | enabled, disabled | 1279.323 | 1268.381 | Unavailable |
| 1 | disabled, enabled | 1258.319 | 1293.242 | Unavailable |
| 2 | enabled, disabled | 1263.200 | 1502.011 | Unavailable |
| 3 | disabled, enabled | 1252.208 | 1257.680 | Unavailable |
| 4 | enabled, disabled | 1277.498 | 1264.591 | Unavailable |

Enabled mean/median: **1266.109 / 1263.200 ms**; disabled: **1317.181 / 1268.381 ms**. The disabled outlier and ordered pairs prohibit treating their difference as a calibrated instrumentation tax or speedup. A deliberate 65,536-byte allocation returned zero from `GC.GetAllocatedBytesForCurrentThread`; raw per-action zero readings therefore mean **unsupported**, not zero allocation. Per-boundary allocation, allocation overhead, retained heap, native memory and GC-pause attribution are unavailable.

**Selected exclusive attribution.** All following five-value distributions are milliseconds in enabled sample order 0–4. Calls were identical in all five samples. `SelfTicks` subtracts immediate measured children only; it still includes unmarked work, instrumentation, GC and waits. It is not hardware CPU time.

| Boundary | Calls/sample | Selected self ms, samples 0–4 |
| --- | ---: | --- |
| CapturedPlanMaterialization | 1793 | 85.004, 92.134, 81.525, 80.624, 82.809 |
| IntraroomPlanning | 905 | 23.178, 22.432, 31.364, 23.985, 21.666 |
| MutableExecutionSetup | 138 | 96.169, 92.935, 92.804, 89.707, 90.083 |
| StartValidation | 147 | 27.753, 25.627, 26.048, 24.996, 25.811 |
| StartEncoding | 138 | 11.489, 9.591, 10.243, 6.392, 12.220 |
| AdmissionStartReuse | 128 | 83.952, 84.227, 83.290, 83.187, 83.919 |
| StartRecreation | 137 | 63.464, 63.055, 62.861, 62.512, 63.133 |
| PrefixReplay | 137 | 282.361, 289.994, 263.482, 274.724, 267.921 |
| SupportedReader | 137 | 314.801, 309.076, 307.992, 294.348, 305.627 |

Inclusive parent ranges: StartRecreation **326.785–350.130 ms**; AdmissionStartReuse **244.075–262.057 ms**; PrefixReplay **308.533–344.074 ms**; SupportedReader **943.248–992.912 ms**. These overlap each other and the table's child rows. CompleteValidation has ten calls, **105.078–134.864 ms inclusive**, **90.784–119.770 ms selected self**. All boundary/origin/call distributions and retained parent records are in the evidence; inclusive rows are not added as independent costs.

Restricting the candidate estimate to exact preparation hits and supported prefix readers inside this admission's normal/alternative assurance lifetime gives:

| Eligible component | Calls/sample | Selected exclusive ms, samples 0–4 |
| --- | ---: | --- |
| Route materialization during prepared-hit setup/validation | 896 | 40.716, 40.845, 39.133, 38.819, 39.739 |
| Traversal planning during prepared-hit setup | 512 | 12.124, 11.687, 20.817, 10.682, 11.247 |
| Route materialization during assurance readers' replay | 751 | 36.464, 44.618, 35.772, 35.270, 36.183 |
| Traversal planning during assurance readers' replay | 338 | 9.157, 9.462, 9.279, 12.121, 9.143 |
| **Disjoint candidate envelope** | **2497** | **98.461, 106.612, 105.002, 96.892, 96.313** |

The envelope's mean is **100.656 ms**: **77.512 ms route materialization**, **23.144 ms planning**. It excludes the cold template-producing setup, the original probe's own progress and readers outside assurance. It is a **gross measured preparation envelope**, not all safely removable time: fresh graphs, charge replay, schedule rebinding and template construction remain costs. Materialization nested in StartValidation is counted once in its child, never again through inclusive validation.

**Normal versus authentic DESCEND.** Each sample has **72 normal readers/replays at depths 1–72**, **57 alternative readers/replays at depths 18–74**, one LifetimeBranch and two nested AdmissionAssurance calls. The unchanged production path restores the genuine transfer prefix, applies BlockTransfer/Verify and ReleaseTransfer/Verify, then recursively advances/validates the alternative to terminal. Normal and alternative committed combat work must match. No forced-descent tuning or synthetic shortcut was introduced.

Disjoint selected-origin contribution: normal **567.309, 591.900, 586.188, 555.513, 555.235 ms**; alternative **481.270, 474.011, 474.985, 472.232, 477.058 ms**. The alternative contribution equals the full LifetimeBranch inclusive scope; the normal contribution excludes that child. Their total is **1027.745–1065.911 ms**. The sum of both AdmissionAssurance inclusive calls (**1478.964–1517.568 ms**) overlaps recursively and is not an action total. Coordinator TransferBlocked/TransferReleased counters are zero in this single-admission fixture; the private hypothetical controls are authenticated by the above reader prefixes and source path, not by those coordinator markers.

**Ownership and work that remains.** Cold recreation decodes binary snapshot/config/profile inputs, validates them, creates a fresh execution, re-encodes exact inner bytes, compares them and Remember retains private copied preparation. A hit compares **all inner/wrapped bytes, executable and all three checkpoint limits**, copies the snapshot shell and repeats fresh validation, setup, capture/hash and byte comparison. Nine cold StartPreparation calls and 128 preparation hits remain across the full action. Only the cold decode/first validation is already reused within the lifetime; no prefix validity is cached.

`CopyForExecution` shares immutable strings, readonly floor wrappers/list and immutable combat profile leaves, clones bounds/options and deep-copies monster records. Config/runtime/loot/knowledge JSON accessors still return fresh graphs. The whole snapshot, route graph, forks/suffixes, assignments, scheduled interactions and traversal arrays are **not immutable**. StartCapturedSnapshot/CreateExecution create fresh config/runtime/room copies, assignment/posture validation, BranchTraversal/workload/objective, party/members, combat kernel/actors/dictionaries, controls, events and lazy iterator. Prefix replay materializes more routes and plans actual rooms, while dynamic positions/HP/resolved sets and combat remain independent.

Potential safe ownership would be one privately owned immutable scalar/string/value-array template per exact admission, never exposed, with fresh DTO/array copies where mutable consumers receive data. Scheduled interactions must use validated stable assignment identity/ordinal and bind to each execution's newly copied assignments; they must not retain shared RunRoomAssignment references. Path/movement arrays need a proven private-copy policy. No execution, runtime, party/member, kernel, monster, iterator, workload, control or mutable graph may cross executions. Mismatches use the existing fresh path; disposal/exception/failed-candidate abandonment drops preparation. No sharing across admissions, pumps, sessions, threads or saves is contemplated.

Setup planning's temporary paths/interactions are discarded after event-bound calculation; its exact checked ChargePlanning accumulation and kernel initialization/geometry charge remain. Replay Plan charges the execution kernel incrementally. DeterministicRoomPaths charges enumeration, sort cardinality, indexing, adjacency, BFS reset/visits/edges, destination scans and path reconstruction; Plan also charges validation/path/interaction cardinalities. Cached results would need the original exact charges and failure boundaries, not a guessed constant or merely a matching successful final total. All fresh start validation, malformed-input rejection, byte/event/replay/combat limits, wrapped-start checks, digest/inspection/re-encoding, complete candidate validation, final predecessor authorization, canonical publication and independent durable semantic readback remain mandatory. No such replacement is implemented or proved by this diagnostic.

**Memory, copying and residual limits.** A template would retain at least another privately owned decoded representation and schedule data during overlapping normal/alternative construction. Fresh copies cost O(route records + nested arrays + path/interactions); that work cannot be counted as a saving. The 7,555-byte fixture route JSON is not its heap size or a maximum-valid-input measurement. Existing bounds are 65,536 checkpoint bytes, 2,048 party replay events, 1,000,000 combat work and 64 canonical materialized tiles; they do not establish a template heap budget. Nested suffix duplication, adjacency arrays, assignment copies and peak template construction require capacity accounting. One million 64-bit ledger entries alone would occupy 8 MB before overhead—illustrative arithmetic, not a proven attainable valid-input case or an approved allocation. A compact exact charge representation would need separate proof. Maximum-valid-input retained/native memory and copying latency remain unqualified.

Fresh snapshot copying is unisolated in StartRecreation/AdmissionStartReuse residuals. CombatC0Geometry construction remains unisolated in replay: immutable sorted topology is mixed with fresh kernel-bound wrapper/BFS scratch, while Collect depends on live HP/positions. Connection JSON decoding, assignment/effects work, hashing and checkpoint wrapping/export also remain in selected parent self time. No savings estimate is assigned to these residuals. Wall time outside every retained root scope is **1.574, 1.593, 1.653, 1.660, 4.889 ms**. Wall minus the selected candidate envelope is **1151.707–1181.185 ms**, which includes both mandatory work and the explicitly mixed/unisolated residuals.

The same collector retains its 4096-record/64-depth bounds: **3732 records/sample, maximum nesting level 11, zero dropped records/scopes and zero active depth**. Every retained scope passed exact parent/child timing conservation. Five identifiers were appended without changing main's existing values or activation/storage behavior. Empty-scope calibration for 4096 scopes was disabled **0.0813 / 0.0299 ms**, enabled **0.2882 / 0.2902 ms**. This isolates small empty-scope timing only; it cannot correct full-workload timings or quantify allocation/GC overhead. Default-disabled call overhead is still present in both modes. The initial seven-marker source was narrowed before any Unity execution to retain headroom; no saturated or failed sample was hidden.

**Qualification and review gate.** Campaign `90dd31ce31424724bad1eb5949120182` **PASSED all five stages**, including exact restoration and final preservation, through the unchanged supervisor. The test stage passed **20 discovered / 20 passed / 0 failed / 0 skipped**, with CLI and owned job exit zero; NUnit duration **38.3316769 seconds**. No C# compiler diagnostics were found. No test failure or automatic retry occurred. Tests are the benchmark, 18 existing AdmissionStartReconstruction cases and `ForegroundPerformanceDiagnostics.DisabledResetBoundsAndExceptionNesting`. All recorded campaign process identities are absent, the reservation released normally (`campaign:null`) and all 1498 original inputs/protected artifacts match. These passes belong to source `4f47c07d`; no broader or new exact-HEAD qualification is claimed for later report-only commits.

| Gate | Classification and disposition |
| --- | --- |
| Fetch/branch/worktree/environment/source preflight; source/ownership review; oracle/bytes/branch/scope integrity; raw preservation; whitespace/compilation review | **Required. Passed**, including exact restoration and final preservation. |
| Existing bounded benchmark | **Required diagnostic evidence for this task.** One warmup plus five measured samples/mode; passed. |
| Focused Unit/SIT | **Required.** 19 existing directly applicable reconstruction/metrics cases passed; benchmark adds intact canonical integration. |
| Historical #251/#252/#259/#261 evidence | Comparative/governance inspection only. **No reused passing test gate for this source.** #259 Co23 abrupt interruption remains not run under its accepted exception. |
| Additional CPU/retained-memory or copy microbenchmarks | **Optional Diagnostic, not run.** No replacement harness or speculative implementation. Limitations above are retained. |
| Full EditMode/PlayMode; Windows native filesystem; standalone build/lifecycle/save/reopen; sustained 68-completion profiles | **Not Applicable at the authorized attribution checkpoint; not run or claimed passed.** No gameplay/persistence implementation; later B implementation would require its full applicable gates. |
| Owner gameplay UAT | **Not Applicable proposed for independent review.** Diagnostic-only, no player-facing gameplay/text change; no owner UAT started. |
| Independent ChatGPT review | **Required next step.** This packet is not implementation authorization, full regression qualification or merge readiness. |

Exact CLI filter:

```text
DungeonBuilder.M0.Editor.Tests.AdmissionStartBenchmark.OneProductionAdmissionRepeatedSamples;DungeonBuilder.M0.Editor.Tests.AdmissionStartReconstruction;DungeonBuilder.M0.Editor.Tests.ForegroundPerformanceDiagnostics.DisabledResetBoundsAndExceptionNesting
```

The exact executable/argument array is retained in `focused/invocation.json`: Unity CLI `test C:/Dev/Dungeon-Lord/Temp/option-b-admission-attribution --mode EditMode --filter <the exact filter above> --output <campaign>/focused/results.xml --timeout 900 --no-color -- -logFile <campaign>/focused/unity.log`. `<campaign>` is the absolute campaign path in the ledger below. Export variables are sealed in `focused/benchmark/ownership.json`; the adapter sets documented child-local `UNITY_NO_AUTH_BROKER=1`. The plan pins Python, Git, PowerShell, Editor, CLI and the complete unchanged helper chain. `preparation/plan.json`, original/qualified maps, original input ZIP, source-transition receipt, source/tool hashes, raw XML/logs and atomic process/exit/evidence receipts remain local.

**Exact changed Unity files and purpose.** No gameplay/persistence algorithm changed; no schema, migration, tuning, localization, resources, package, historical reader or fixture changed.

| Path | Necessary change |
| --- | --- |
| `Assets/_Project/Scripts/Core/ForegroundQualificationMetrics.cs` | Append five fixed diagnostic boundary identifiers; existing collector/storage/activation preserved. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs` | Time the existing MaterializePlan JSON operation. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatStartCapture.cs` | Time existing start validation and inner capture/encoding. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/IntraroomTraversal.cs` | Time existing traversal/geometry preparation and its unchanged charges. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.Execution.cs` | Time existing fresh execution setup with measured children subtracted. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/AdmissionStartBenchmark.cs` | Add outside-action allocation/empty-scope calibration, input equality, branch/conservation checks and saturation fields to the same benchmark. Existing fixture/protocol/oracles preserved. |

The remaining tracked files are `Docs/testing/option-b-admission-traversal-attribution.md` and `Docs/testing/evidence/option-b-admission-20261110/attribution.json`, `eligible-costs.json`, `report.json`, `diagnostic-records.zip`: the report, exact distributions/derivation, source/tool/preservation manifest and sanitized raw review packet. Complete static call/ownership analysis is retained in the packet, including actual replay MaterializePlan calls, noncombat/historical behavior, ResolveCombatRoom and geometry construction. Governing review includes current AGENTS/development rules, invariants/amendments, H2 decision, hybrid/H1-F4/two-floor contracts, admission/predecessor/diagnostic records, #262 supervisor efficiency, test-stage/build/UAT/artifact rules and model policy. Historical proposals did not supersede approved amendments or merged schema-17 authorities.

**Artifact ledger and preservation.** All paths below are retained; no cleanup is authorized or performed.

| Absolute path/identity | Owner, purpose and retention |
| --- | --- |
| `C:/Dev/Dungeon-Lord/Temp/option-b-admission-attribution` | Codex B diagnostic; isolated branch and generated Library/Temp/Logs/UserSettings. Preserve for review. |
| `C:/Dev/Dungeon-Lord/Temp/option-b-admission-attribution/TestResults/b-admission-20261110` | Codex B diagnostic; adapters, initial/current source/input/plan versions, archived originals, protected maps, environment/repository receipts, offline analysis, fixture bytes and report derivation. Preserve. |
| `C:/Dev/Dungeon-Lord/Temp/option-b-admission-attribution/TestResults/qualification-supervisor/90dd31ce31424724bad1eb5949120182` | Existing supervisor; raw NUnit XML, logs, benchmark JSON, command/source/tool/input/evidence manifests, generated-input and process/Job Object/exit/restoration/preservation receipts. Preserve; raw logs stay private. |
| `C:/Dev/Dungeon-Lord/Temp/option-b-admission-attribution/TestResults/qualification-supervisor-reservation` | Existing supervisor; this isolated campaign's ownership/normal release receipt. Preserve. |
| `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B Admission 20261110 01` | Codex B diagnostic; unique test product/output namespace. Preserve; owner namespace not used. |
| `C:/Dev/Dungeon-Lord` owner inputs/TestResults/Builds; existing FGPV history/builds; retained F2 build; 252 other save namespaces | Their original owners. Protected inventories include 1498 owner inputs, 19138 owner test artifacts, 12540 owner build files, 430 FGPV history files, 299 F2 build files and 1276 other-save files, plus prior corpus/review/failure maps. Preserve unchanged; counts are inventory entries and may overlap. |

Initial owner reservation was free; exact-command inspection found only the existing Unity Hub serve broker and no conflicting Editor/player. Campaign ownership used the unchanged supervisor's handles, executable/creation identities and Job Objects; all eleven recorded host/child/supervisor identities are absent at closeout. The unique product and outputs were unused before preparation. All 1498 original Unity inputs were archived before the declared isolated productName/known import-order/line-ending preparation. No unexpected Unity input rewrite occurred in the test stage (`changed:[]`). Exact original-input restoration and final protected-artifact verification **passed**. No owner save, qualified build, #263 failure/recovery artifact, unfamiliar process or historical verdict was modified, cleaned or terminated.

Sealed packet SHA-256: `8ee328dd79c3aa738bb388c824e54708419ea861f5fcd5fd2645b3bfb0488b5a`. Original-input ZIP: `6e9f27279331263fdaf7a26c8fb436ead7aad7d0e0861ba653b577664cb8342e`; exact restored input map: `ca3032b4af07d3ce3dc3dcb227c5398eb0abd771a7769fe86cdb7ed154781c0d`; protected inventory: `d6ca5c80675671dbd850b081d7ec081aea680a7eb2cd837ac79f499802fbea45`. Raw logs and private preservation inventories stay local. No historical result was relabeled.

**Next action:** independent ChatGPT review of the measurements, ownership/copy/charge limitations, STOP recommendation and final preservation manifest. If the reviewer accepts STOP, conclude B and return to A's unresolved design/limit/recovery approvals. This diagnostic supplies no authorization to implement either option.
