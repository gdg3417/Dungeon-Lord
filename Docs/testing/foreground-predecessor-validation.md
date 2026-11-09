# Foreground predecessor validation: focused review checkpoint

Draft PR [#261](https://github.com/gdg3417/Dungeon-Lord/pull/261), branch `codex/h2-foreground-predecessor-validation`. Main baseline: `01ff418143c0400d8e6fafba7a260f399bcdb2aa`. Qualified PR source: **`13797005244cd78f7ba8594c0ca713421554997b`**. Core implementation first qualified at `a9c8965376922d1225e78e19a2b386c62517d651` and is byte unchanged afterward. Matched baseline source: `dfe6808f21c7298d5d994f93e888433f7aabbdc2`, verified main plus the identical diagnostic harness only. Later report/evidence commits do not change Unity inputs. No merge, broad qualification, Windows player qualification or UAT is authorized by this packet. Future hybrid policy approvals are independent.

[Receipts, exact control hashes and artifact ledger](evidence/foreground-predecessor-validation-20261108/report.json) · [Matched comparison](evidence/foreground-predecessor-validation-20261108/comparison.json) · [Synthetic raw profiles/seeds/final saves](evidence/foreground-predecessor-validation-20261108/raw-profiles.zip) · [NUnit, plans, preservation receipts and tools](evidence/foreground-predecessor-validation-20261108/qualification-evidence.zip).

Recommended model: GPT-6.1 Sol, High; Complex. Owner-specified fallbacks: GPT-6 Sol High, then GPT-5.6 Sol High. No model switch is claimed. The Unity CLI skill was used; CLI 1.0.0-beta.17 was verified current, Unity 6000.3.2f1.

## Initial proof and exact reuse boundary

Before code edits, inspection covered AGENTS.md, development rules, invariant amendments, approved two-floor and time/recovery contracts, recent #251/#252 optimization history and #260's retained evidence, plus canonical sessions, complete validators, foreground scheduling, writer, runtime projection and atomic readback. The repeated operation is `PrepareReplacement`'s complete validation of its immutable predecessor bytes: each foreground Stage already has a fresh complete result from pump entry or the preceding candidate validation. Historical #251 measured seven prior validations at 98.976 ms inclusive in a bounded fixture. #260's matched main one-floor trace recorded 1,568 stages / 150.913 s inclusive and 147.551 s in Prepare. These scopes overlap; they cannot be summed or translated into promised frame savings.

All seven preparation entry points were traced: spatial-only, live recognized state, Living Dungeon owner replacement, manual run, Editor-only durable settlement, foreground replacement and recovery replacement. Only foreground replacement with explicit monster continuity can consume the new local proof. Every other entry keeps its default fresh complete predecessor validation and its established reconciliation/failure behavior.

The foreground writer's initial validation now issues proof through the session. Its constructor is private; the only factory performs a fresh complete contextual validation before copying evidence. Each successful candidate validation can likewise issue the next Stage's proof before returning exposed graphs. No factory accepts an externally supplied validation result.

This matters because `runtime.livingDungeon = owned.LivingDungeon` deliberately aliases and mutates validation-backed state. Reusing that graph as a pristine predecessor would be unsafe. Instead, proof holds its exact immutable session and source bytes, copied profile references/order and private deep copies of prior party metadata/checkpoint bytes. It stores no exposed spatial, monster or scheduler graph. Party lookups return another copy. The proof is carried only in local/result objects within one synchronous transaction and never stored on a canonical session, SaveService or a cross-pump cache.

Context equivalence is narrow and explicit. The session's context and limits are readonly. Production content and compatibility snapshots retain private canonical bytes and return deserialized copies; legacy configuration is privately copied and reparsed. Monster recovery rules and profile/timing values expose no mutation path. The profile collection's entries can be replaced, so proof verifies the captured references and order. Recovery/context binding cannot be rebound after its one-time activation. The existing consumed-value guard checks mutable configuration, combat DTO, bootstrap/Research inputs, loot and occupancy at proof use, preserving the check fresh validation would perform.

The skipped prior result was used only to establish validity, perform general continuity reconciliation, and look up old party checkpoints for release verification. Explicit foreground continuity makes the general reconciliation unnecessary. Copied prior records provide the same old release inputs; independent old/new supported readers and exact lineage permits still verify release. Full candidate validation authenticates the current proposed graphs and all consequences. Terminal predecessor validation, settlement/history authorization, final predecessor validation and fresh durable semantic reopening are unchanged.

Wrong session, identical-byte reopen, changed source bytes, replaced profile or changed consumed values cannot reuse the proof. A different context cannot issue proof for that session. Missing or mismatching evidence takes the original fresh-validation path. Malformed/corrupt bytes cannot issue evidence at all. Mutation of exposed validation objects cannot alter proof; invalid proposed checkpoint state still fails candidate validation. Disk/session comparison and the atomic protocol independently reject foreign disk bytes and distinguish proven rollback from uncertain publication. This change does not turn proof into publication or settlement authority.

## Focused qualification

Initial campaign `b0312f18e857458e87593dbe62b4919c`: **15/15 EditMode passed**, zero failed/skipped, 16.983 s reported test duration. All five supervisor stages passed; 1,498 exact Unity inputs restored and protected owner/save/historical evidence unchanged.

| Identical-input progression | Prior validations, fresh → reuse | Complete validations, fresh → reuse |
| --- | --- | --- |
| One floor, unsplit | 25 → 0 | 60 → 35 |
| One floor, split pumps | 25 → 0 | 69 → 44 |
| Two floors, unsplit | 37 → 0 | 84 → 47 |
| Two floors, split pumps | 37 → 0 | 93 → 56 |

Complete canonical bytes match after every corresponding publication, including revision/provenance: nothing is normalized away. Stage, canonical write and durable readback counts remain identical. The reference supplies a distinct but equivalent context to force the unchanged validation path; it is a correctness/count reference, not a frame-performance baseline. Cases cover multiple stages, ticks, admission, terminal/recovery, waiting/causal release, alias mutation, stale/reopened/context/profile/configuration mismatch, malformed save/corrupt checkpoint, and shared spatial/live defaults.

Narrow follow-up `5fa7899129d34d8e8671827236ccfe85`: **27/27 passed**, zero failed/skipped, 29.320 s. This repeats the 15 new cases and adds existing terminal/release rollback and uncertainty tests, rounded-away recovery debit, commit-before-ack original/residual recovery and post-commit evidence cleanup. Counts overlap; 15 + 27 are not 42 unique tests. Final preservation receipt/status is recorded in the evidence manifest.

Final focused gate `ef6c5409cc6d45a2908ce809060263e8`, source `13797005`: **31/31 passed**, zero failed/skipped, 41.780 s. Four additional cases explicitly prove durable A>C draining against fresh validation and exact history/wallet/monster/branch/floor owner consequences across pump partitions. Full bytes still match the reference within each partition; partition-dependent bookkeeping is not normalized away. All five supervisor stages passed, with all 1,498 inputs exactly restored. There are **31 unique focused cases**, not a sum of the overlapping gates.

Unit and SIT apply. No complete suite, Windows player result or owner acceptance is claimed. Player-facing UAT applicability remains for independent review; no new UI contract is introduced.

## Bounded performance comparison

The same host ran the existing scene profiles at 1280×720, Default text, four completions per workload/source. Separate bounded selectors retain the historical 68-run defaults. Fixed test-only seed UTC is authored before ownership; detailed existing metrics supply prior/complete/readback totals. No new benchmark framework or runtime policy is introduced.

Valid matched baseline: campaign `eca7367938034a89840776ec741d0b96`, `C:/Users/gdg34/.codex/worktrees/fgpv-main/Dungeon-Lord`, source `dfe6808f`, 2/2 profiles passed in 70.446 s. PR: `e381f9285f3b4e75b140839f53e2ab2d`, source `13797005`, 2/2 passed in 62.601 s. All five stages passed in both, exact original settings restored (1,494 baseline / 1,498 PR inputs), process ownership/completion and protected evidence verified. These durations include test setup and are not frame-speedup measurements.

**Both complete workloads match:** 664 one-floor / 1,020 two-floor frames; every Frame/Sequence/Ordinal/Category/PartyCount/CheckpointBytes signature; exact seed bytes; exact final complete-save bytes; identical dimensions, Stage/write/readback counts. Nothing is aligned, dropped or normalized. There are 176 raw-byte-identical content/config/package/authoring/driver controls, including all twelve recovery inputs and the three diagnostic source files. Bootstrap scene YAML has a separately recorded checkout CRLF/LF difference only; its Git blob and serialized values are identical. Actual raw hashes remain in the manifest.

| Four-run workload, main → PR | Prior validations | Complete validations | Stages | Canonical writes / durable readbacks |
| --- | --- | --- | --- | --- |
| One floor | 96 → 0 | 397 → 301 | 96 → 96 | 67/67 → 67/67 |
| Two floors | 240 → 0 | 1,066 → 826 | 240 → 240 | 194/194 → 194/194 |

| Inclusive elapsed totals, main → PR | Stage ms | Candidate Prepare ms |
| --- | --- | --- |
| One floor | 4,495.90 → 3,031.29 | 4,384.22 → 2,930.81 |
| Two floors | 21,306.64 → 14,234.85 | 20,715.05 → 13,656.40 |

Stage contains Prepare; atomic protocol contains readback. Do not add overlapping inclusive scopes.

| Full-frame latency in ms, main → PR | Mean | p95 | Maximum |
| --- | --- | --- | --- |
| One floor, all 664 frames | 20.55 → 17.98 | 162.46 → 142.48 | 354.47 → 282.39 |
| One floor, 67 work frames | 181.34 → 156.19 | 324.24 → 268.95 | 354.47 → 282.39 |
| Two floors, all 1,020 frames | 62.89 → 55.55 | 331.81 → 285.25 | 1,657.09 → 1,612.16 |
| Two floors, 194 work frames | 318.89 → 280.16 | 505.58 → 491.48 | 1,657.09 → 1,612.16 |

Work-frame means fall 13.9% / 12.1%; one-floor work p95 falls 17.1%, two-floor only 2.8%. Ordinary event-frame p95 falls 209.70→179.32 ms / 427.85→329.25 ms. The two-floor admission maximum remains approximately **1.6 seconds**: admission assurance and other mandatory work remain. This supports a focused ordinary-work improvement, not resolution of overall foreground stalls or an established two-floor tail benefit.

Natural whole-profile Editor GC increments: 31→26 / 166→144. Responsive whole-frame allocation recorder work means: 62.84→52.66 decimal MB / 116.69→101.18 MB. The existing frame recorder has no isolated live-allocation calibration; these are whole-Editor observations, not calibrated per-operation savings. The thread allocation probe remains unsupported, not zero. Managed heap peaks: 858,759,168→878,968,832 B / 960,806,912→951,066,624 B; global Editor/report growth prevents an isolated retained-heap conclusion.

This is one valid ordered sample per source, four completions each; the PR was sampled before the final byte-aligned baseline. No statistical, FPS, mobile/device or Windows player qualification is claimed. Recommend independent review of the correctness proof and ordinary-event cost reduction before broader qualification. The proposed earlier 10% ordinary-work p95 review target is not demonstrated for aggregate two-floor work p95; do not treat that tail as a passed gate or expand optimization to chase it.

### Preserved unsuccessful and unmatched preparations

Initial baseline `1ab9bbdba8a7485eb4246a9df23617bd` remains FAILED (0 passed / 2 failed): its 261-character exported manifest path exceeded Mono's Windows path limit before profiling. No Unity inputs changed beyond prepared settings. It completed with a normally released reservation; separate restoration `60352a706881473b81bdff2b20ba846c` passed all three stages and restored all 1,494 inputs. The long checkout and every failed artifact remain preserved.

Short-checkout baseline `353693297e044a439f6f1499cf19f64a` passed 2/2, but the raw control audit found ten checkout line-ending differences. They changed only RecoveryContextSha256 in seed/final saves; all frame signatures and every other saved value matched. Its raw profiles, [unmatched comparison](evidence/foreground-predecessor-validation-20261108/unmatched-comparison.json) and [exact save differences](evidence/foreground-predecessor-validation-20261108/unmatched-save-differences.json) remain unchanged and are excluded from the primary matched result.

For the final baseline, each of those ten differences was proven CRLF/LF-only, its original bytes archived, and the owned checkout received the PR's exact existing bytes, including its mixed terminal ending. No production file or gameplay/configuration value changed. The [alignment record](evidence/foreground-predecessor-validation-20261108/raw-control-alignment.json) pins every before/after hash. An initial over-strict alignment preflight rejected the mixed terminal ending before any writes; its source is retained. A partial evidence collector output is also preserved locally; the complete packet explicitly distinguishes raw scene hashes from its identical Git blob. No raw save is rewritten and no historical verdict is relabelled.

## Save impact, risks and preservation

Schema 17, frozen readers, historical saves, canonical event order, IDs, configuration/tuning, localization and stable failure reasons are unchanged. No cross-pump execution retention, batching, provisional UI, new numeric policy, second writer or offline adventurer behavior is included.

Changed implementation/test files:

* `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs`
* `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs`
* `Assets/_Project/Tests/EditMode/ForegroundPredecessorValidationTests.cs` and its `.meta`
* `Assets/_Project/Editor/DungeonSpatial/Tests/ForegroundPredecessorValidationEditModeFixtures.cs` and its `.meta`
* `Assets/_Project/Scripts/Core/GameRoot.ForegroundProfile.cs`
* `Assets/_Project/Tests/EditMode/LivingDungeonTwoFloorTests.cs`
* `Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonTwoFloorProductionProfileSceneTests.cs`

The remaining files are this report and its sanitized evidence. No production Resources/Data/Packages/scene/settings inputs change in the PR.

Remaining risks: the proof depends on the documented immutability and consumed-value checks if context types evolve; private copied evidence adds bounded transaction-local allocation; small source-pinned profiles do not establish broad/platform qualification or statistically reliable performance. Independent review should inspect factory provenance, alias isolation, default-writer behavior and intact terminal/release/readback paths before authorizing any expansion.

All preparation archives, raw logs, isolated saves, failed/incomplete historical campaigns and original PR #260 commits/worktrees remain preserved. No owner save, qualified build or unrelated process was altered or terminated. The original Unity Hub server was left running. Exact new paths, owners, purposes and retention statuses are in the accompanying manifest. Both created baseline checkouts remain needed for review/reproduction; neither is eligible for archive/cleanup yet.

**Next checkpoint: independent review only.** Assess the proof, default writers, performance/control corrections and limited two-floor p95 result. No optimization expansion, broad suite, Windows qualification, owner UAT, ready-for-review transition or merge was initiated. Prepare the normal isolated owner handoff after the requested review/qualification gates if UAT applies.
