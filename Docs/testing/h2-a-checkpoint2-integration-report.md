# H2-A checkpoint 2 — integration and qualification-readiness packet

PR #260 remains a draft. This packet supports independent qualification-readiness review; it does not authorize full-suite/Windows qualification, owner UAT, release or merge.

[Campaign receipts and artifact ledger](evidence/h2-a-checkpoint2-20261108/report.json) · [Sanitized cases, plans and preservation receipts](evidence/h2-a-checkpoint2-20261108/qualification-evidence.zip) · [Raw profile artifacts](evidence/h2-a-checkpoint2-20261108/raw-profiles.zip) · [One-floor comparison](evidence/h2-a-checkpoint2-20261108/one-floor-comparison.json) · [Two-floor comparison](evidence/h2-a-checkpoint2-20261108/two-floor-comparison.json) · [Unmodified two-floor divergence](evidence/h2-a-checkpoint2-20261108/two-floor-divergence.json) · [Exact control/seed identities](evidence/h2-a-checkpoint2-20261108/workload-comparability.json).

Branch: `codex/h2-a-retained-foreground-execution`. Baseline: `01ff418143c0400d8e6fafba7a260f399bcdb2aa`. Integration gate source: `8875a52cb5ffc5c8da169811ac2474fb9df3bd16`. Subsequent source `e265d76309f55a867ad514816259d6baad6edd03` strengthens two partition assertions using canonical owner writers; its runtime, resources, data, packages and project settings are byte unchanged from the integration gate. Later evidence/documentation commits do not change Unity inputs.

## Cleanup determination and correction

The cleanup boundary was not guaranteed nonthrowing. `Retain` called `Clear` before its protective try, and called `Clear` from its catch. The previous `RunExecution.Abandon` called iterator disposal before revoking usability. A disposal exception could escape after the writer had confirmed durable publication, misleading the caller about an already committed save and leaving later parties unprocessed. `Clear` also allocated a token outside the protective try. This is a boundary-contract finding, not an observed production save failure: current `ExecuteAndFinish`/`ExecuteRoute`, room/branch and geometry iterators use compiler-generated disposal of their nested enumerators, with no authored disposal-time publication or external resource action. The tests deliberately inject a throwing disposer to verify the required cleanup guarantee.

`Abandon` now detaches the cursor and marks failure before calling its disposer, and catches only that abandonment disposal. `Clear` revokes the binding/epoch, detaches the entire dictionary, abandons every party and clears the detached dictionary without allocating a replacement. Writer-finally cleanup enumerates the dictionary directly. Borrow issues a new epoch; Retain requires a surviving non-null epoch and creates its replacement set inside its existing protective try. A late lifecycle invalidation still prevents rebind. Partial Retain failure abandons both moved and remaining executions.

Actual simulation advancement and terminal disposal, canonical preparation/validation, atomic persistence and independent readback retain their existing failure behavior. The change does not catch exceptions around those authorities. Tests inject disposal failure into private execution cursors through reflection; no runtime fault-injection seam was added. Successful durable bytes remain exactly on disk, every discarded execution becomes unusable, disposal is not repeated, two-party cleanup finishes, and terminal settlement/reopen produces one outcome.

## Service integration and canonical equivalence

Twenty-three cases extend the original eight, using the existing service/dungeon fixtures. Real `SaveService.AdvanceForeground` pumps prove reuse, installation of the exact returned durable session, publication callbacks, attached runtime/clock and exact persisted bytes. A canonical loot edit while active preserves the historical start/checkpoint, invalidates retention and reconstructs on the subsequent pump. Proven rollback stalls until explicit retry; uncertain publication remains recovery-blocked and writes nothing on another pump. Identical-byte reopen and configure/recovery/delete/disable/quit clear process-local executions.

Negative cases alter private start bytes, ordinal, prefix and executable identity, replace an equivalent profile object, corrupt persisted checkpoint bytes, and change consumed combat configuration. Private mismatches reconstruct; invalid canonical/configuration inputs fail closed. Two parties have independent mutable graphs. Existing waiting/causal release/terminal tests remain in the 31-case set. There is no completed-execution archive.

For both profiles and both pump partitions, complete canonical save bytes match the reconstruction reference after every compared pump. Across partitions, exact canonical history, wallet, monster continuity, branch knowledge and floor knowledge consequences match. Existing revision bookkeeping differs by partition; the tests do not normalize it or claim identical whole saves across different partitions. Internal owner objects use their canonical writers, because JsonUtility omits their internal fields.

## Executed gates

| Campaign | Source / result |
| --- | --- |
| `5b140d1a20754f0c96a17fc3fcd2e61c` | `d1c9212`; initial 29/29 focused EditMode passed; all five preservation stages passed. |
| `b1d0fe38d61f4d489a79684407ed959a` | `9fb9452`; discovery only, all five stages passed. Exact selection: 299 EditMode, 14 PlayMode. |
| `d097076751414278ac7710f28346a151` | `8875a52`; cold heap 1/1, B3B1 51/51, remaining targeted EditMode 248/248, relevant PlayMode 14/14; all 11 stages passed. 299 unique EditMode plus one overlapping cold repetition, zero failures/skips. |
| `d82e30053fa043a2ad5bcc5df2cabe2f` | `e265d76`; strengthened partition cases 2/2, all five stages passed. |
| `ca7fc8906b1c449492923e69a909b406` | Exact current-main; both existing 68-completion profiles passed, all five stages passed. |
| `4862ecc09db74edf85c97020a217c147` | `e265d76`; both profiles passed in a fresh detached benchmark checkout, all five stages passed. |

Passing targeted workload durations: cold heap 9.128s; B3B1 61.673s; remaining EditMode 390.677s; PlayMode 28.372s. They cover H2-A, B3B1, B3B2, two-floor, foreground diagnostics and H1-F4 recovery contracts, plus the 14 selected production-shell integrations. They are targeted Unit/SIT evidence, not complete suites. Exact 1,502 Unity input files were restored; owner/save/evidence protection and owned-process completion passed.

Two unsuccessful targeted attempts remain preserved, with original FAILED verdicts and normal reservation release: `9db056bd3206478a9e79a9404e9956da` (297/299; isolated product lacked the existing export prefix), and `6f23090cc5104a4ab67ddba28f0e6a5d` (296/299; wrong B3B1 export stage, long product exceeded sidecar path limit, conservative-GC test stack retained the final pair). Exact separate restoration/preservation passed for each. The final adapter uses the existing required export stage and a short valid isolated product. The heap test ends the releasing helper's stack frame before checking reachability; no assertion was weakened and cold/warm executions passed. Historical checkpoint-1 failed/incomplete campaigns and approved recovery remain unchanged.

## Memory and GC interpretation

The representative two-party waiting fixture holds ordinals 14/14, presentation steps 14/14, combat events 16/17 and 27,346 start-input bytes per party. Three samples each repeat 12 pumps without accumulating parties or these buffers. Every weak execution reference becomes dead after Clear and forced collection. The cold gate's released-pair deltas were 417,792 / 180,224 / 184,320 bytes; the 4MiB live heap probe responded by 3,682,304 bytes amid Editor heap noise. Warm and earlier runs also show approximately 176–180KiB steady released pairs. This is a representative global managed-heap estimate, not a maximum retained-byte budget. First-use/JIT/global heap noise may contribute to the larger cold estimate; it cannot be assigned entirely to retention.

`GC.GetAllocatedBytesForCurrentThread` failed the live allocation calibration on this Mono runtime. Zero readings are unsupported, not zero allocation. The heap test's GC count includes explicit collections (28 in the cold case), so it is not natural GC pressure. Production-profile GC/allocated-frame evidence is reported separately below.

Retention remains capped by existing one-/two-party production profiles, active nonterminal ownership and existing execution/checkpoint/work envelopes. No new tuning threshold is introduced. Worst-envelope heap, mobile memory and standalone allocation qualification remain unproven.

## Matched performance and value judgment

Full-frame latency, milliseconds, main → PR. Work frames are admission/event/settlement/passive categories; all frames include idle/between-event frames. The prefix is the exact common beginning, without dropping or aligning any frames.

| Workload/sample | Frames main / PR | Mean | p95 | Maximum |
| --- | --- | --- | --- | --- |
| One floor, all | 8,344 / 8,344 | 41.74 → 42.74 | 277.43 → 285.41 | 748.94 → 755.81 |
| One floor, work | 1,091 / 1,091 | 301.32 → 308.78 | 544.16 → 559.45 | 748.94 → 755.81 |
| Two floors, exact common prefix, all | 6,721 / 6,721 | 118.30 → 126.87 | 516.68 → 560.48 | 2,056.96 → 2,284.95 |
| Two floors, exact common prefix, work | 1,588 / 1,588 | 490.83 → 526.63 | 878.25 → 915.68 | 2,056.96 → 2,284.95 |
| Two floors, full traces **not exactly matched**, all | 12,544 / 12,540 | 126.79 → 133.85 | 531.77 → 567.72 | 2,143.95 → 2,301.29 |
| Two floors, full traces **not exactly matched**, work | 3,014 / 3,010 | 517.80 → 547.46 | 905.74 → 949.75 | 2,143.95 → 2,301.29 |

One-floor signatures match exactly for every frame: Frame, Sequence, Ordinal, Category, PartyCount and CheckpointBytes. Both complete 68 runs and perform 1,091 atomic writes and inspections. Restore calls fall 9,452 → 8,500 (inclusive 37.82 → 34.67s); all checkpoint replay calls fall 10,676 → 9,724 (43.06 → 39.58s). Post-publication inspection remains 1,091 calls (5.51 → 5.67s). Atomic-write inclusive time is 59.04 → 60.27s. These scopes overlap; reductions cannot be summed. The saved reconstruction work did not produce a measured whole-frame improvement.

The two-floor comparison has a material comparability failure. Main records four additional passive frames at indexes 6,721–6,724, with atomic publication/recovery-shaped work and unchanged ordinal/party checkpoint lengths; PR proceeds without those frames. Thereafter the traces are displaced. Both report the same counts of admissions (69), event frames (2,869), settlements (68), idle-empty (119) and between-events (9,411), but passive frames are 8 versus 4. This is preserved, not normalized away. The exact common prefix covers 35 settlements. Its measurements also show no improvement. The cause of the four extra baseline frames is unproven; existing Editor focus/lifecycle callbacks can affect the disabled manually driven root, but the evidence does not establish that attribution. Full two-floor statistics cannot support a causal performance or write-frequency claim. Source-level unchanged persistence boundaries and targeted exact-byte/write-count tests remain the write-frequency evidence.

For completeness, the full two-floor traces record Restore 45,723 → 41,145 calls (333.83 → 315.23s), CheckpointReplay 54,624 → 50,046 (400.61 → 384.51s), Inspect 3,011 → 3,010 (41.16 → 43.95s), and AtomicWrite 3,014 → 3,010 (236.83 → 252.39s). Those are descriptive, unmatched totals; the four writes are not an H2-A batching result.

Natural profile GC collections: one floor 889 → 891; full two-floor traces 4,394 → 4,357 (unmatched); exact two-floor prefix 2,216 → 2,201. The existing frame-allocation recorder is valid and responds; work-frame means are 120.93 → 121.07 decimal MB for one floor and 218.12 → 217.06 MB for the unmatched full two-floor traces. It has no isolated live-allocation calibration in this unchanged harness, so these are whole-Editor-frame recorder observations, not calibrated per-operation allocation savings. Thread allocation remains unsupported. Global managed-heap peaks are 1,036,296,192 → 1,064,935,424 bytes on one floor and 1,102,897,152 → 1,123,909,632 on two; baseline starting heaps also differ, and the report sample arrays contribute growth. These values do not isolate retention and establish no memory budget compliance.

Both sources use unchanged production scene/profile instrumentation, existing exported workload fixtures, 1280×720 Default text, Unity 6000.3.2f1 / CLI 1.0.0-beta.17, one-floor then two-floor order and 68 completed runs each. Both benchmark worktrees start with fresh imports/processes. All recorded Resources/Data/Packages/ProjectSettings, Bootstrap, profile-driver and seed-producer control hashes match. The existing producer authors each seed at its own UTC: exact seeds differ in lastSavedUtcUnix, lastPausedUtcUnix and the two derived migration descriptor/transaction identities. All four differences and exact seed hashes are recorded without normalization; no gameplay/configuration field differs. Isolated product/save paths differ. Full frames include actual controller/map/layout/render and Editor scheduling. One ordered campaign per source provides descriptive measurements, not a statistically established causal speedup, regression or FPS claim. The full two-floor mismatch further limits that comparison.

**Recommendation: reconsider the retention implementation before expensive final qualification.** Reuse and reconstruction-call reductions are real, and the representative retained pair is small and collectible. A player-relevant frame benefit is not demonstrated: the exactly matched one-floor sample and exactly matched two-floor prefix both have slower PR point estimates, with no convincing GC/allocation improvement. One ordered run cannot establish a causal regression, but it supplies no evidence that the added binding/epoch/lifecycle complexity pays for itself. Preserve this draft and the independent cleanup correction for review; do not broaden, tune, implement H2-B or run full final qualification to chase this result. Independent review should decide whether to remove/narrow retention or request a separately scoped exact two-floor comparability investigation.

## Scope, risks and next gate

Schema 17, frozen codecs/readers, historical compatibility, canonical write frequency, reserve accounting, independent readback, event/transfer ordering, atomic terminal settlement, one writable authority, production config and localization remain unchanged. No offline adventurer processing or capacity expansion is introduced.

Targeted correctness evidence has no unresolved production failure. Remaining risks are the private mutable working set's binding/lifecycle complexity, unmeasured worst-envelope heap, broader unexecuted compatibility/lifecycle suites, platform-specific player behavior, and the unexplained two-floor trace mismatch. Independent review should inspect the exact-binding/epoch and cleanup proof and decide whether the implementation merits further qualification; this packet recommends against proceeding directly to final qualification. Full final suites, Windows Development/player close/reopen and owner UAT have not run. UAT remains applicable to eventual player-facing acceptance but is explicitly unauthorized at this checkpoint; no acceptance is inferred from Editor automation.

H2-B checkpoint batching/coalescing, provisional activity, new rollback policy, safe cuts, provisional-versus-confirmed UX and offline adventurer processing are deferred. No related scaffold or persistence cadence change is included.

All raw campaigns, preparation archives, failure reproductions, isolated namespaces and three worktrees are preserved with exact paths in the artifact ledger. No owner save/build/evidence was deleted, overwritten or launched. The previously unproven adb process remains outside any termination authorization. No cleanup is eligible while evidence dependencies remain.
