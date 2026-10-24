# Admission-local adventurer start preparation

Starting main: `5510f17b7bdd95d8f247c8a679da6684f0231010` (fresh fetch, merged #251).
Recommendation: GPT-6.1 Sol High, Complex: specified performance work with locked deterministic/persistence semantics. First availability fallback: GPT-6 Sol High, then GPT-6 Astra Low under policy v2. No model change requested.

## Proof recorded before execution-path implementation

Authorities reviewed: AGENTS, model policy, foreground architecture audit and diagnostic measurement/report, Living Dungeon owner lock, automatic two-floor contract, Specs 17/28, cross-spec invariants, GD66 migration design, merged #247/#244/#245/#251 source/history. Older schema/stage status prose is historical; current writable schema is 15. #247 already removed the exact duplicate predecessor validation and repeated same-event inspection serialization; neither is repeated here. #251 measures work, not hardware CPU costs.

### Complete inputs and context

`LivingDungeonLifetimeAssurance.Check` advances one privately restored probe. Every Verify exports that probe and calls RestoreSupported; at authentic DESCEND a separately restored alternative exports Blocked, Released and all remaining prefixes. No operation changes StartInputs. EnableTransfers runs only at ordinal zero; BlockTransfer/ReleaseTransfer mutate execution control state, never captured inputs. The normal and alternative paths therefore contain the exact same inner combat-start bytes and wrapped format-3 start. This is also checked dynamically by full byte comparison before any reuse.

CombatStartCapture.Capture includes version/executable, sequence, effective event bound, all five timing fields, and ActiveFloorRunSnapshot.WriteCheckpoint: serialized complete run config, loot config/table, branch/floor knowledge, initial runtime, posture, tick, ordered floors with stable identity/index/fingerprint and complete plan/spatial/connection strings and connection bound. It additionally includes explicit validated combat profiles/target order and work/cardinality limits, room bounds, and persistent monster assignment/option/level/HP/revival/readiness inputs. No current dungeon, current configuration, wall clock, resource wallet, live knowledge or external service is consulted by reconstruction. JSON configuration accessors materialize defensive copies. Sequence/identity is contained in the compared bytes, not used as a proxy for equality.

Eligibility requires exact equality of ALL inner bytes, executable string, MaximumBytes, MaximumReplayEvents and MaximumCombatWork. Transfer reader independently parses/checks contract, format, maximum and complete wrapping on EVERY call, invokes EnableTransfers on the fresh execution and compares the exact wrapped bytes. Presentation retention is per execution. Gates, ordinal, accepted deadline, consumed flag, committed digest and inspection are prefix-specific and never retained in preparation. Different inputs/limits/executable take the original decoding path without replacing the one owned preparation.

### Ownership

Shareable leaves: strings and scalars; RunnableFloorSnapshot's readonly strings/scalars (MaterializePlan, InspectSpatialInputs and traversal JSON decoding create new graphs); read-only floor collection; RunLogicalTiming's getter-only scalars; CombatC0QualificationConfig and CombatC0AttackProfile getter-only values and defensively owned read-only collections. CombatC0RoomBounds is a readonly struct containing only strings/integers.

ActiveFloorRunSnapshot as a whole is NOT immutable: CombatBounds is an exposed internal array, RestoreCombatInputs has setters, and MonsterContinuityRecord has writable health/revival. Preparation must privately retain a decoded snapshot that is NEVER handed to an execution. Each execution receives a new snapshot shell with cloned bounds and options and deep-copied monster records; immutable floor wrappers/profiles alone can be shared. InitialRuntime, Configuration, LootConfiguration, BranchKnowledge and FloorKnowledge remain fresh JSON graphs. StartTimedCombatSnapshot/CreateExecution still create private config, route, runtime, BranchTraversal/workload, party/members, combat execution/kernel/actors/dictionaries, iterator, event list, outcome, presentation list, prefix/start byte arrays and transfer controls. No RunExecution, kernel, iterator, party or writable HP graph is retained by preparation.

The smallest useful reuse is the parsed capture fields and immutable leaves with private copy templates. The first reconstruction retains both existing validation passes. On a hit, the decoder's validation of identical privately retained inputs is reusable; StartTimedCombatSnapshot still independently validates the fresh copy. Route planning, simulation and all prefix replay remain independent. This deliberately leaves further planning/JSON optimizations outside scope.

### Rejection and cleanup

RestoreSupported retains its byte envelope, format dispatch and checkpoint identity checks. Formats 1/2 remain on their existing readers. Format 3 retains payload hash, contract/evidence/start identity, ordinal/time/deadline/terminal/consumption checks, gate vacancy bound, wrapping and maximum checks, every replayed control/event, committed digest/inspection and exact final re-encoding. Preparation is input decoding, never proof that any prefix is valid. Invalid format-3 states retain `run.checkpoint.transfer_evidence_mismatch`; existing format-1/2 reason codes remain their own authorities.

On mismatched or malformed starts, decoding/validation rejects exactly as before; no entry is replaced. Tighter byte/replay limits reject before reconstruction; changed combat limits miss reuse and are checked by decoding and fresh execution creation. Combat planning/advancement still enforces captured work budgets. A bad prefix following a good prefix cannot use the prior prefix's evidence. Each failed candidate is abandoned by its existing reader; alternative execution remains abandoned in finally. The owning Check disposes preparation in finally, dropping its references on success or exception. No persistent/global cache, canonical truth, gameplay effects or publication capability is added.

## Predeclared measurement procedure

Harness commit is recorded separately from the subsequent optimization. `AdmissionStartBenchmark.OneProductionAdmissionRepeatedSamples` uses #251's production fixture, two captured active floors, injected fixed UTC and in-memory canonical filesystem. Restore identical complete input bytes before each action. Time one intact admission plus post-publication inspection, excluding fixture/reopen/setup and result formatting. One warmup per mode, five samples per mode, alternating enabled/disabled order by pair. No forced GC. Compare full canonical output bytes across every sample and across before/after; no normalization. Enabled samples require exactly 17 complete validations, 138 supported readers and 138 prefix replays. Save bytes include immutable start/checkpoint bytes; focused tests additionally inspect every normal/alternative prefix and complete traces/outcomes.

Existing StartRecreation remains the entire fresh execution reconstruction, including any eligible decoding reuse. Existing Area counters and boundaries retain their meanings/order. New appended StartPreparation counts/measures complete cold recreation (decoding, validation and fresh setup), while AdmissionStartReuse counts/measures fresh setup on an eligible hit. Their addition makes StartRecreation self relative to a larger selected set; compare its INCLUSIVE time across sources, or explicitly reconstruct the old self boundary. Stopwatch wall times include GC, instrumentation and Editor noise; they are not hardware CPU measurements. Thread allocation counts require calibration and will be reported unavailable if unreliable. No native/iOS/release or full production-frame claim.

Preservation inventory precedes tests; a unique product namespace and output identity protect owner TestResults.xml and saves. No sustained profile, build, owner test or shutdown correction is authorized. Qualification results, exact commands, source pins and preservation verification follow in separate evidence.

## PR #252 focused qualification correction (2026-10-24)

Recommended Codex configuration: GPT-6.1 Sol, Medium. Task classification: Standard.
Reason: bounded follow-up test coverage using established fixtures and known acceptance criteria.
Fallback if unavailable: GPT-6 Sol Medium, then GPT-5.6 Sol Medium.

Continuation began with a clean working tree on `codex/optimize-admission-start-reconstruction` at reviewed HEAD `d2ff9d7739aed5ea7781b6ecbfe67ede9038224a`; no partial prior edits were present. Ordinary workspace `apply_patch` succeeded without elevation. No rejected patch was retried through another editor or outside the sandbox. Git metadata writes required the existing approved staging/commit permissions because `.git` is read-only in the sandbox.

Test source: `0348ed5e52e3f14f93e586b7096ce21159364b3a`. Only `AdmissionStartReconstructionTests.cs` changed in that commit; production simulation and existing fixture infrastructure are unchanged. This documentation append follows the tested commit.

The outer lifetime-operation case now explicitly captures one active floor in addition to the existing two-floor EXIT/DESCEND cases. It executes the real `LivingDungeonLifetimeAssurance.Check`, verifies terminal completion, one cold preparation, reuse for every subsequent supported reader, every prefix replay, normal/alternative reader counts, and unchanged canonical bytes.

The new combat case measures the first encounter from the existing kernel, injects only test configuration, warms preparation at the initial checkpoint, and exports the real committed prefix immediately before the encounter. Both warm and reference readers restore the exact legal checkpoint bytes before future advancement. No checkpoint ordinal, start, digest or inspection is fabricated to request impossible replay beyond the captured ceiling. A boundary restore explicitly requires one preparation and one reuse hit.

| Captured work ceiling | Legal checkpoint ordinal | Pending work | Encounter cost | Failure ordinal | Committed / charged work at failure |
| --- | ---: | ---: | ---: | ---: | ---: |
| 2412 (cost minus one) | 2 | 2013 | 2413 | 2 | 1875 / 2013 |
| 2413 (exact cost) | 2 | 2013 | 2413 | 3 | 2413 / 2413 |
| 2414 (cost plus one) | 2 | 2013 | 2413 | 3 | 2413 / 2414 |

All three compare warm/reference future steps, prefix digests, committed inspection, combat/presentation events, monster state and charged/committed work. Both fail with `combat.c0.workload_exceeded` at the same ordinal. The failing event leaves committed inspection, digest and receipts unchanged; subsequent checkpoint export, outcome extraction and due-time probing are rejected. Canonical save bytes and published runtime are unchanged.

### Focused result and reproduction

Unity `6000.3.2f1`, CLI `1.0.0-beta.14`: **18 passed, 0 failed, 0 skipped**, NUnit duration **16.8741339 seconds**. This reruns the existing reconstruction fixture and adds four cases; it does not repeat the benchmark, broader regressions, PlayMode, profiling or builds. The first sandboxed launch stalled before tests during licensing WMI reads (`UnauthorizedAccessException` for BIOS/network information). The runner was interrupted, its exact temporary product-name delta was checked and original settings bytes restored, and absence of the prior Editor was confirmed. One unrestricted retry completed successfully. Existing Mono abort-thread shutdown diagnostics remain in the passing log; no compile error or NUnit failure occurred.

```powershell
& TestResults/admission-start-20261024-01/run.ps1 `
  -Source 0348ed5e52e3f14f93e586b7096ce21159364b3a `
  -Stage correction-focused-r2 `
  -Filter 'DungeonBuilder.M0.Editor.Tests.AdmissionStartReconstruction'
```

The unchanged wrapper pins source, rejects reused output identities, uses product `Dungeon Lord Admission Start Qualification 20261024 01`, and restores original settings/environment. Raw reports and compact evidence remain under `TestResults/admission-start-20261024-01/`: `correction-focused-r2.xml`, `correction-focused-r2.log`, `correction-focused-r2-command.json`, `correction-summary.json`, and `correction-preservation-before.json` / `correction-preservation-after.json`. The failed launch is separately recorded as `correction-focused.log` / `correction-focused-command.json` and is not counted as a test verdict.

| Evidence | SHA-256 |
| --- | --- |
| Tested `AdmissionStartReconstructionTests.cs` | `0f7197459ccdb09b7e0b7c097fcbb21d21640c105a236cbdbf2f7ba2dd8d8f33` |
| Unchanged `LivingDungeonTwoFloorTests.cs` fixture | `d6532adc4a78a1fc98c81bd93a18f83a3589750c77b7194c31609a1f3a66a15a` |
| Unchanged `run.ps1` wrapper | `ca16a635a6aa192a5943d8b44e33eeaa6db15fc701f4aed7381c920c850a9d3c` |
| Passing NUnit report | `f5d5aab3d346405dec6555c7473d6689beca400c79a3cf795193699a949dc64c` |
| Passing Unity log | `3b121b2166a83b6c8d9d0040173252cf8af613f7a58fed9092d5b7648f519bd8` |
| Targeted post-run preservation record | `7f70c57ed8a4cb513bcd44665254373ca1a671ce66b1c092ae81e76ac3b30c75` |

Targeted before/after hashing reused paths from the existing preservation manifest for owner files, configuration/fixtures and original settings, plus prior admission campaign evidence: **579 files, zero missing files or byte mismatches**. The prior full preservation inventory/result were themselves preserved; no new build scan or full-file inventory was performed. Original ProjectSettings bytes match their backup and Git is clean after the test wrapper. These bounded checks do not claim a fresh full rehash of existing builds.

The two reported combined-coverage gaps are closed by legal engine-generated cases. Stop for independent ChatGPT review; do not merge or begin owner testing.
