# Foreground performance architecture audit

## 1. Executive finding and confidence

**Dungeon Lord's small visible dungeon triggers a much larger synchronous verification workload.** The strongest evidence points to repeated complete-save preparation and checkpoint reconstruction, amplified by deeper prefixes, additional captured content, concurrent parties and private admission assurance. Necessary route/combat calculation is repeatedly executed inside verification as well as during actual progression. This is not evidence that a handful of rooms intrinsically requires hundreds of milliseconds of gameplay computation.

Confidence is **high** in the execution path, recorded stalls, replay amplification mechanism and need to measure smaller boundaries; **medium** in the ranking of remaining optimization opportunities; **low** in any estimate of their eventual speedup. No new performance measurements were made. Existing inclusive scopes cannot establish exclusive CPU, serialization, disk or rendering shares.

Three principal findings:

1. The corrected Windows workload still averages **267.554 / 468.954 ms** for one/two-floor event-category updates and **519.358 / 1,919.193 ms** for admission-category updates. The observed two-floor complete-frame maximum is **2,030.011 ms**. These are Windows Development observations, not iOS measurements or release acceptance.
2. Across 68 completed runs, the corrected two-floor capture performs **3,727 preparations, 45,646 canonical-party restorations and 54,547 supported-reader calls**. Readers recreate captured execution and replay from its start. V2 admission additionally exports and reconstructs every tested prefix on normal and hypothetical blocked/released paths. Few simultaneous parties therefore do not imply little total work.
3. Publication is correctly conservative: detached stages, pinned terminal evidence, exact predecessor/settlement/release authorization, atomic replacement, fresh durable semantic readback, then runtime publication. The valuable target is repeated work within a proven boundary, not deletion of those obligations. Mutable objects inside validation results make broad reuse unsafe without further proof.

The highest-confidence opportunity category is **bounded reuse of exact repeated predecessor/input work**, following the narrow precedent already qualified in #247. Remaining sites are candidates, not proven safe replacements. **Exactly one next PR is recommended: measurement-only foreground call-origin and exclusive-cost instrumentation**, specified in section 12. This audit implements none of the candidates.

## 2. Verified source baseline and scope

Audit date: **2026-10-22**. Repository: `gdg3417/Dungeon-Lord`, local root `C:\Dev\Dungeon-Lord`.

| Check | Verified result |
| --- | --- |
| Initial checkout | Clean `feature/phase7-production-analysis`, HEAD `d3e3c5c0e948834aac7f3bfa767ea3aaf728f151`; unrelated committed work preserved on that branch |
| Remote refresh | Successful `git fetch origin`; latest `origin/main` is `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`, meeting the requested minimum |
| Audit branch | `codex/foreground-performance-architecture-audit`, created directly from that `origin/main`; no reset, discard or overwrite of unrelated work |
| PR #247 | GitHub reports merged; squash commit `575d553b800e04600b2853899891cd55767cb8cd`; final feature head `423e9e413470f41d499ae9f1163f6b519a5b01de` |
| PR #248 | GitHub reports merged; commit `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`; completion-driven qualification tooling, no runtime optimization |
| Measurement source | Corrected Unity source `c3a21ba58a80174e2732c497b2ce1e0361a26fc3`; `git diff` against audit main shows **no differences in Assets, Packages or ProjectSettings** |
| Evidence integrity | All eight before/after summary SHA-256 values in correction `comparison.json` match the referenced committed summaries |
| Intended change | Only this new Markdown file |

Recommended configuration remains **GPT-6.1 Sol, High; Complex**, as requested: known objective and constrained cross-system investigation. Requested availability fallbacks are GPT-6 Sol High, then GPT-5.6 Sol High; no fallback was selected. This is substantive analysis despite its documentation-only output. The canonical policy governs recommendations and does not itself change the executing model.

Read authorities include `AGENTS.md`, `Docs/process/AI_Model_Selection_Policy.md`, development rules, the post-GD60 execution plan, Phase 7A0 lock (including Decision 92 and responsive-editing addendum), Living Dungeon owner lock and B2/B3A/B3B1/B3B2/two-floor contracts, Specs 17/28 and the cross-spec invariants. Current merged implementation and later approved contracts reconcile historical status text. In particular:

- The performance correction report and parts of the qualification/planning records still say #247 is Draft/unmerged or awaiting owner testing. GitHub's merged state and #247's final owner/independent-review closeout supersede those **status** claims. Their measurements and failed-attempt history retain their original source applicability.
- B2's dormant-only production description, B3A's Editor-only synthetic settlement and B3B2's initial one-floor restriction are historical stages. Current production two-floor foreground ownership is in `LivingDungeonForeground.cs`; it does not call the Editor-only `LivingDungeonTerminalSettlement` producer.
- Writable schema is **15**. Frozen checkpoint formats 1/2 coexist with format 3 and separate production profiles. Historical schema references do not authorize a new migration.
- Offline adventurer catch-up, Floors 3–5 and broader Phase 7/8 delivery remain outside the implemented slice. This advisory audit changes no approved dependency sequence, design lock or tuning.

Method: read source and recorded JSON/Markdown evidence, inspect Git history and GitHub PR metadata, verify selected evidence hashes, and conduct bounded public research. No Unity Editor launch/import, tests, benchmark, qualification campaign, build or owner gameplay request occurred. Normal owner saves were not opened. Existing reports, fixtures and outputs were not edited.

## 3. Current foreground execution-path map

Paths below are relative to `Assets/_Project/Scripts/`. Line ranges refer to the verified main above; the reference index in section 14 resolves full paths.

```text
Unity main thread
  Core/GameRoot.cs Update (271–282)
    Core/GameRoot.Foreground.cs UpdateForeground (58–69)
      Services/TimeService.Foreground.cs SampleForeground
      [dirty / missing inspection / deadline due / accepted horizon unclosed]
      PumpForeground (77–114)
        Services/SaveService.Foreground.cs AdvanceForeground (27–53)
          Gameplay/DungeonSpatial/LivingDungeonForeground.cs
            DetachedCanonicalWriteAuthority.AdvanceForeground (14–304)
              exact on-disk predecessor comparison; ValidateSession
              detached SaveData JSON clone; mutable private working owner
              chronological bounded timestamp processing:
                passive tick -> ProcessParties -> immediate settlement
                -> Recover -> ReleaseWaiting -> opportunity/newborn work -> close
              Stage (47–66), potentially several times per timestamp:
                DetachedRecognizedSaveStateSnapshot.Capture
                working.PrepareForegroundReplacement -> PrepareReplacement
                  serialize recognized values/retained canonical/unknown members
                  validate prior complete bytes; validate candidate complete bytes
                  replay/profile/occupancy/history/continuity/release checks
                retain validated candidate + new private session
                CanonicalMvpRouteProjection.TryPublishValidated (private projection)
              PrepareAndPersist (DetachedCanonicalWriteAuthority.cs 392–474)
                final predecessor validation, reserve/recovery checks, projection
                two-active-floor snapshot feasibility check
                ExactCompleteSaveAtomicPersistence.Persist
                  verify predecessor/evidence; durable rollback + candidate
                  atomic replace; active-byte readback; full semantic readback
                return durable session and projection
          install session -> CanonicalRuntimePublished event
            GameRoot.PublishCanonicalRuntime (786–806)
            ProductionDungeonController.CanonicalPublished (365–388)
        SaveService.InspectForeground (55–113)
          restore each retained party with presentation steps; derive next boundary
          publish read-only ForegroundActivity
        telemetry / dashboard lines
  UI/ProductionDungeonController.Update (63–85)
    changed balance/tick HUD, draft work, input, replay, foreground presentation
    selected-floor interpolation/label placement each frame
```

**Scheduling versus frames.** `GameRoot.Update` samples the monotonic foreground source each frame, but the full pump runs only under the conditions shown. Dirty state and unclosed accepted horizons are exceptions to deadline-only execution. Between-event Windows two-floor update mean is approximately **0.000106 ms** in the qualification harness; this says nothing about ordinary render cost on a phone. Sampling floors the absolute monotonic value to configured integer units before subtraction. Pause/focus gating controls eligibility; neither render delta nor UTC is foreground simulation authority.

**Ordinary events.** `Execution(p)` restores each party at most once into a pump-local dictionary. `NextDueTime` can prepare the iterator's next announcement; it is not necessarily a field lookup. `ProcessParties` selects by absolute due time, numeric sequence and the party's next ordinal, advances one event, pins its checkpoint and changes revision. Format-3 folding includes combat inspection and transfer state. Final timestamp closure calls `Stage`; no general atomic disk write occurs inside `Stage`.

**Admission.** At a due opportunity, `DemandInterval` derives configured route/Heat/arrival/intent/traffic summaries. Floor 1 vacancy, capacity, eligibility and allocator availability gate capture. `ActiveFloorRunSnapshot.Create` captures active floors; `StartTimedCombatSnapshot` creates the C2 execution; `EnableTransfers` installs the versioned transfer contract. `LivingDungeonScheduling.Pin` exports the newborn checkpoint. A restored private probe runs `LivingDungeonLifetimeAssurance.Check` before the party is admitted and the allocator increments. Unsupported admission consumes the opportunity with a stable reason rather than allocating a failed run. Admission, newborn zero-time work, recovery/release and closure can require separate stages.

**Terminal settlement.** `ProcessParties` first pins and stages an unconsumed terminal in an unclosed private owner, then independently `ValidateSession`s that predecessor. It consumes the outcome, applies `LivingDungeonForegroundReconciliation.Apply` and `LivingDungeonOutcomeConsequences.Apply`, retires the party, creates `DurableSettlementHistory.CreateForeground`, stages the authorized history/condition/knowledge result and calls `lineage.Retired`. Current HP receives verified net damage against stable identities; it is not overwritten with captured historical final HP. Shared knowledge respects current applicability and captured observations.

**Blocked/released transfers.** An occupied transfer emits Blocked without consuming the iterator transfer; waiting retains source occupancy and has no actionable deadline. Release requires transaction-local terminal/retirement vacancy causality. `LivingDungeonForegroundLineage.Release` commits Released and completion before staging, retaining exact prefix evidence but never publishing half a transfer. `ReleaseWaiting` processes newly enabled events and recovery until stable. Ordinary due transfers precede recovery in their established event phase; previously blocked releases follow recovery. Current geometry edits or a valid digest alone cannot establish vacancy.

**Lifecycle/action barriers.** Focus/pause/disable/quit and canonical edit/manual/save paths call `FlushForeground`. Its bounded loop can invoke multiple pumps, and ordinary `SaveService.Save` can subsequently perform a recognized-state save. These are potentially additional transactions, not evidence of writes every render frame. A proven rollback failure becomes retry-pending until an explicit barrier; uncertain persistence becomes recovery-required. Do not turn either into an automatic per-frame retry loop. The existing periodic-save path is separate from foreground event publication.

**Where measurements attach.** `Stage` encloses `Prepare` and private `Projection`; `Restore` encloses `CheckpointReplay` when entered through `LivingDungeonContracts.Restore`. Direct supported-reader calls, including lifetime verification, also increment `CheckpointReplay`. `AtomicWrite` encloses filesystem work **and semantic readback/replay**. Settlement/recovery tags enclose some stages; TransferReleased's small inner scope excludes much of the enclosing release-tagged frame. `Publication`/`Present` cover controller work. `GameRoot.ForegroundProfile.ProfileForeground` times an explicit `UpdateForeground`, yields a real controller/render frame, then records frame/main-thread/allocation counters. These boundaries overlap.

## 4. Measured performance and allocation evidence

Authoritative corrected summaries are under `Docs/testing/evidence/living-dungeon-two-floor/performance-correction/`. Each case completes 68 runs. The before values below come from the `final-*-summary.json` files pinned by `comparison.json`, not earlier superseded attempts.

| Update category, ms mean / p95 / maximum | Reviewed Windows | Corrected Windows |
| --- | --- | --- |
| One-floor event | 307.957 / 445.714 / 481.043 | **267.554 / 372.134 / 404.420** |
| Two-floor event | 538.089 / 732.171 / 1,168.427 | **468.954 / 618.602 / 1,043.496** |
| One-floor admission | 615.273 / 647.767 / 657.641 | **519.358 / 549.944 / 559.202** |
| Two-floor admission | 2,138.105 / 2,241.040 / 2,282.163 | **1,919.193 / 2,011.483 / 2,029.486** |
| Two-floor release-tagged | 1,761.764 / 1,870.769 / 1,893.522 | **1,422.660 / 1,537.420 / 1,569.740** |

| Additional corrected observations | One floor | Two floors |
| --- | --- | --- |
| Windows settlement update, mean / p95 / maximum ms | 559.138 / 593.621 / 618.574 | 1,246.272 / 1,503.273 / 1,569.740 |
| Windows blocked-tagged update, mean ms | No samples | 488.578 |
| Windows maximum complete frame, before → corrected ms | 1,056.805 → 1,016.453 | 2,282.511 → 2,030.011 |
| Windows event-frame allocation, corrected mean / p95 / maximum decimal MB | 102.127 / 151.381 / 152.518 | **192.601 / 263.335 / 404.018** |
| Windows admission-frame allocation, corrected mean decimal MB | 199.107 | 693.881 |
| Editor event update, before → corrected mean ms | 414.739 → 351.347 | 744.966 → 622.350 |
| Editor corrected maximum complete frame ms | 788.404 | 2,364.702 |
| Windows event-category frames | 884 | 2,869 |
| Windows observed frames / concurrent frames | 8,344 / 0 | 12,540 / 6,256 |
| Admissions / settlements | 68 / 68 | 69 / 68 |
| Blocked / released tags | 0 / 0 | 34 / 34 |
| Maximum aggregate retained checkpoint bytes | 24,242 | 73,719 |
| Final Windows complete-save bytes | 94,759 | 216,802 |

73,719 is the sum of two retained checkpoints, each within its individual 65,536-byte envelope. Frame allocation is temporary allocation during the observed Unity/controller/render frame, **not retained live memory**, a save's size or the simulator's exclusive allocation. Thread allocation counters calibrated unavailable and remain null. The frame and main-thread samples are separate measurements: for example, two-floor admission maximum main-thread time is 2,030.032 ms, versus complete-frame maximum 2,030.011 ms. Do not substitute one for the other.

| Windows instrumented calls, before → corrected | One floor | Two floors |
| --- | --- | --- |
| Prepare | 1,568 → 1,568 | 3,727 → 3,727 |
| Restore | 10,608 → **9,452** | 51,166 → **45,646** |
| CheckpointReplay (supported reader) | 11,832 → **10,676** | 60,067 → **54,547** |
| AtomicWrite scopes | 1,091 → 1,091 | 3,010 → 3,010 |
| LifetimeProbe | 68 → 68 | 69 → 69 |
| Inspect | 1,091 → 1,091 | 3,010 → 3,010 |

| Corrected Windows inclusive scope milliseconds | One floor | Two floors |
| --- | --- | --- |
| Stage | 155,792.866 | 680,145.768 |
| Prepare | 152,538.309 | 664,117.752 |
| Restore | 37,638.164 | 333,034.971 |
| CheckpointReplay | 42,400.007 | 399,334.532 |
| LifetimeProbe | 5,665.161 | 75,433.056 |
| AtomicWrite | 63,135.040 | 255,438.291 |
| Inspect | 5,542.949 | 41,163.485 |
| Publication | 5,693.995 | 15,375.100 |
| Present | 3,000.299 | 8,163.006 |

**Never sum these rows as independent costs.** In particular Prepare contains replay; AtomicWrite contains durable semantic replay; Publication contains Present; phase tags can coincide. The release-tagged frame mean of 1,422.660 ms is not the inner TransferReleased scope, whose entire 34-call inclusive total is only 35.937 ms. Zero calls for reserved Open/Validate areas mean those sites lack independent instrumentation; they do not mean validation is free. WorldRender records one Windows reconstruction per scenario and zero Editor calls within these windows; it does not cover all rendering.

#247 already removed two measured exact duplications: the second predecessor validation inside one `PrepareReplacement`, and the second combat-inspection serialization during the immediate format-3 fold. Output/checkpoint every-prefix tests, unchanged workload counts and normalized complete-save comparisons support that bounded correction. Do not recommend those already removed duplications again. The correction retained 1,210/3,129 one/two-floor boundary projection rows per platform; Windows final saves match the prior implementation after normalizing **only three lifecycle UTC fields**, not gameplay/checkpoint state. This is historical equivalence evidence, not new validation of a proposed change.

## 5. Workload comparability and attribution limits

The matched setup holds Floor 1 content constant and constructs the same authored Floor 2 before writing inactive/active seeds. Activating Floor 2 makes an additional **three-room route and its content** available to captured execution, enables concurrent parties and introduces transfer/waiting behavior. The inactive seed still contains the constructed floor, which matters to complete-save parsing even when that floor does not execute. Floor 1 has two caches; comparison to #245's older simpler single-cache scenario is not controlled.

The source exporter is `LivingDungeonTwoFloorTests.ProductionDungeon` (625–657) and `ExportProductionTwoFloorStandaloneInputsUsingApprovedContentAndTiming` (659–693). The Editor adapter disables normal root Update before driving the profile (121–179). The harness injects 0.25 foreground seconds per sampled frame after frame 120, uses unchanged production timing and fixed QA funding, and stops at 68 completions. Windows batchmode requests 1280×720 Default text on the recorded i7-12700KF/RTX 3070 Ti machine. It is a repeatable accelerated logical workload, not a 68-run observation of natural player pacing.

Established: event-frame counts and composition differ, as do prefix depth, checkpoints, admission count, persistence scopes and captured content. Two-floor costs are not attributable solely to rooms, party count or transfers. The two-floor case's 69th admission also prevents treating 68 completions as identical admission work.

Inference: larger captured inputs and longer histories increase each reconstruction's cost; more retained parties multiply reconstruction sites; more boundaries/stages multiply complete-save preparation. The source explains all three mechanisms, but existing totals do not identify their separate coefficients. The reader-call excess over Restore (1,224 one floor; 8,901 two floors) includes direct reader paths such as lifetime verification; it is not an exact exclusive accounting of those paths.

Editor preparation counts differ materially from Windows (corrected 2,657/6,735 versus 1,568/3,727), even though committed boundary projections match. The evidence does not establish the exact source of that extra private staging. Frame/pause/test-driver partitioning is a hypothesis to instrument, not a verified duplicate-Update diagnosis. Compare optimization effects within the same platform/harness first.

Causality requires matched **event-kind, prefix-ordinal, captured-size and party-count** cases, plus separate admission and persistence measurements (section 11). Reopening an unrelated owner QA save or watching smooth reference-game footage supplies none of that control.

## 6. Simulation versus validation versus persistence costs

### Necessary simulation and repeated construction

`RunSimulationService` uses the same iterator engine for timed advancement and replay. `RunExecution.AdvanceCore` consumes announcements/effects, stamps ordinal/time, records combat receipts and folds evidence. Room traversal, path planning, combat targeting, loot and decision resolution have real content-dependent work. One atomic monster engagement may contain multiple combat actions under one run-event ordinal; an event count is not a uniform CPU unit.

`CreateExecution` (Execution.cs 56–129) JSON-clones configuration, loot, initial runtime and route, normalizes null discriminators, sorts rooms, validates assignments, materializes all floor plans and calculates geometry/event/combat bounds. Snapshot capture stores private JSON strings; getters reconstruct copies. `CombatStartCapture.Recreate` reads captured rules/snapshot/monsters, validates them, starts the same timed engine, and requires exact start-byte equality. Reconstruction repeats setup/planning as well as the subsequent prefix simulation.

These copies satisfy isolation: later edits/config mutations cannot rewrite an admitted party; failed detached work cannot mutate canonical state; old executable/start contracts remain replayable. Replacing JSON with shallow references violates that purpose. Null versus empty distinctions and transient presentation evidence require deliberate preservation.

Specific repeated JSON sites:

- Pump entry: `LivingDungeonForeground.cs` 26–41 clones `SaveData`, restores nullable fields/transient evidence, then installs strict detached owners.
- Every Stage: `DetachedRecognizedSaveStateSnapshot.Capture` (SaveSession.cs 24–50) serializes the runtime, strict-parses it, emits recognized member bytes; `PrepareReplacement` parses retained bytes and constructs the whole candidate.
- Private/final/readback projection: `CanonicalMvpRouteProjection.TryPublishValidated` 40–83 deserializes a complete `SaveRoot`, then replaces authority fields with strict-parser objects.
- Every recreated execution: `ActiveFloorRunSnapshot.Configuration`, `LootConfiguration`, `InitialRuntime`, knowledge and floor-plan/connection accessors deserialize copies; `CreateExecution` clones several of those again.
- `RunnableFloorSnapshot.TraverseConnection`, `TraverseBranchApproach`, `BranchSocket` deserialize the captured connection representation on access (79–94). Immutable source strings are unchanged; decoded graphs/arrays are mutable. Reusing a decoded graph requires ownership proof.

### Why restoration and preparation counts grow

`LivingDungeonContracts.TryRead -> ValidateCore` reconstructs **every retained party** to verify executable/profile/timing, current ordinal/deadline, terminal restrictions, occupancy and waiter/blocker consistency. It checks aggregate ordinal limits before reconstruction. `DetachedCompleteSaveContract.ParseValidateAndRoundTripCore` also checks canonical owners, continuity identity, allocator/history, independent base-save budget through a frozen-schema-13 round trip, and exact canonical byte re-encoding. Current production semantic validation follows. This is more than JSON parsing.

For each private Stage, `PrepareReplacement` validates prior bytes and candidate bytes. Format 3 additionally restores candidate parties to inspect release gates and, where released, prior parties to compare control evidence or require an exact release permit. Final persistence validates its original predecessor again. Durable readback invokes a fresh complete semantic validation; post-publication `InspectForeground` reconstructs parties again with presentation retention. Settlement adds independent terminal-predecessor validation and lineage restoration. These checks explain amplification; their exact per-origin counts are not recorded.

If a party has prefix length `k`, restoring it performs setup plus `k` engine steps and exact export comparison. Repeating reconstruction at consecutive prefixes through `n` events entails a sum proportional to `1 + ... + n` for the replay component, even before multiple validation sites. This is a structural deduction, **not a fitted complexity claim for total runtime**. Admission's v2 `Verify` explicitly restores each prefix; on authentic descent it explores a private blocked/released continuation and verifies those prefixes too. V1 lifetime assurance exports without the new per-prefix restore branch; historical workloads must remain distinguished.

Only the **current checkpoint per retained party** is stored, not a persisted list of all historical prefixes. Admission visits many prefixes transiently. Pruned completed-run history is bounded and does not itself cause replay of every past settled party. Current run prefix depth, captured dungeon size and retained outcome serialization are separate costs.

| Scaling dimension | Source-supported effect | What remains unmeasured |
| --- | --- | --- |
| Retained party count | Per-party loops in validation, inspection, release checks; occupancy cross-checks | Exclusive incremental cost at a fixed prefix/content |
| Committed event count/depth | Restore replays to ordinal; repeated event checkpoints accumulate repeated prefix work | Cost by event kind and ordinal |
| Captured rooms/assignments/geometry | Snapshot decoding, plan materialization, path/combat setup and start-byte hashing grow | One-room/multi-room slope at equal events |
| Prefixes tested at admission | Normal + authentic blocked/released future branches export/reconstruct | Exclusive assurance setup/replay/encoding shares |
| Staged transactions | Complete recognized capture, prior/candidate semantic validation and private projection per Stage | Exact duplicate byte/context pairs and call origins |
| Completed history/base-save size | Recognized capture, history checks, serialization and frozen-base validation | Copy/parse cost independently of active checkpoints |
| Atomic publication frequency | One final Persist per successful pump, plus separate save/barrier transactions | OS I/O versus semantic readback/volume probing |

### Persistence placement and correctness

There are **3,727 stages versus 3,010 atomic scopes** in corrected two-floor Windows evidence. Private stages are not separate disk commits. The pump publishes one final exact predecessor-to-candidate transaction after bounded timestamp processing, sometimes retaining an accepted but unclosed horizon. Removing stages might save CPU without reducing disk writes, but terminal pinning and authorization depend on particular intermediate states.

`ExactCompleteSaveAtomicPersistence.Persist` checks active bytes, settles bounded owned evidence, durably writes rollback and candidate, verifies both, replaces atomically and reads back active bytes. The callback performs full semantic reconstruction and projection. Proven rollback yields atomic-save-failed; uncertainty yields recovery-required. The Windows filesystem uses write-through handles and `FlushFileBuffers`. Its `FlushDirectory` rechecks supported nonredirected local NTFS capability; it is **not a claimed POSIX directory fsync**. Atomic scope time cannot be labeled raw disk latency.

The two-active-floor final path also calls `ActiveFloorRunSnapshot.Create` for current feasibility before persistence (WriteAuthority.cs 450–454), even without an admission. That can repeat geometry/config preparation, but it protects acceptance of a runnable current dungeon. Its exclusive cost and safe narrower equivalent are unproven.

## 7. Unity-specific tool and architecture assessment

Verified versions: Editor **6000.3.2f1 / a9779f353c9b**, URP **17.3.0**, Burst **1.8.26** at dependency depth 2, Collections **2.6.2** at depth 1, Mathematics **1.3.3**. `Packages/manifest.json` does not directly declare Burst or Collections; lockfile paths include 2D Animation/Common and render-pipeline dependencies. Existing `Library/PackageCache` package metadata confirms Burst/Collections are locally present; no import/install was performed. Searches of project runtime scripts found no `BurstCompile`, `IJob`, `NativeArray`, `Unity.Jobs`, `Unity.Collections`, `Task.Run` or `ProfilerMarker` integration. Package presence does not compile ordinary managed simulation through Burst. Memory Profiler is not declared in the project manifest/lock inspected here.

| Capability | Applicable hotspot and decision | Constraints / evidence needed |
| --- | --- | --- |
| Custom `ProfilerMarker` | **Adopt next** around existing pump/stage/replay/clone/readback origins. Current stopwatch aggregate areas lack CPU Timeline nesting and uninstrumented Validate subscopes. | Fixed marker names; no per-event strings or gameplay control. Separate marker overhead from improvement. [Unity 6000.3 API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerMarker.html). |
| CPU Timeline / Hierarchy / Raw Hierarchy | **Adopt** for main-thread stalls, worker/render waits and nested versus self time; allocation call stacks can locate JSON/byte-array churn. | Prefer short captures without Deep Profiling initially; its overhead distorts totals. All-thread Timeline and self time prevent inclusive double counting. [Unity CPU profiler](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/profiler/visualizing-data/cpu/introduction). |
| Memory Profiler / allocation tracking | **Use allocation tracking first; defer package installation/snapshots to a scoped future change if needed.** | Frame churn and retained snapshots are different questions. Snapshot retained managed/native ownership between comparable boundaries; do not interpret 192.601 MB/frame as a leak. Package/version approval and overhead assessment precede installation. [Memory Profiler](https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/index.html). |
| Burst | **Defer**, potentially useful only for a separately measured pure numerical/path kernel. | Current strings, class graphs, JSON, managed collections and iterators cannot simply be annotated. Preserve integer order, PRNG draws, floating-point bit behavior and old evidence on Windows/IL2CPP/device. [Burst type support](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-type-support.html). The 1.8 documentation currently resolves to a newer patch than installed; it is general API guidance, not patch-level qualification. |
| C# Job System | **Defer** pending independent pure work and representation proof. | Global timestamp events, recovery, admission and settlement are causally ordered. Parallel party mutation is not valid merely because parties occupy distinct floors. Scheduling then immediately completing can preserve stalls. [Unity 6000.3 overview](https://docs.unity3d.com/6000.3/Documentation/Manual/job-system-overview.html), [thread-safe types](https://docs.unity.com/en-us/engine/6000.0/manual/scripting/optimization/job-system/thread-safe-types). |
| Unity.Collections/native data | **Defer**; possible scratch data for a proven hot kernel, not a new save representation. | Conversion/copy costs, allocator lifetime, safety/disposal, stable ID mapping and exact checkpoint bytes must be measured. Native containers alone remove neither replay nor persistence. |
| UI Toolkit change/redraw control | **Adopt measurement; retain existing guards.** Candidate: avoid full informative `Present` on publications without relevant changes. | Controller already gates HUD on wallet/Heat/tick, geometry rebuild on fingerprints, foreground text on view/edit/replay/floor changes; world interpolation is separate. Profile actual layout/repaint first. [6000.3 runtime UI guidance](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-performance-consideration-runtime.html). |
| Incremental/frame-budgeted processing | **Defer further scheduling changes** until private resumability is designed and measured. Existing pump already bounds timestamps/events. | May yield only while keeping candidate private, current predecessor pinned and publication atomic. Must not expose half timestamps, lose sampled time or weaken pause/edit closure. A single engagement/lifetime probe/validation can exceed a budget. Coroutine yields alone neither parallelize nor reduce CPU work. |
| Background managed worker | **Plausible later**, especially detached immutable-input verification/preparation; not currently integrated. | Unity documents plain-data `JsonUtility.ToJson` and `FromJson` as background-callable. JSON is not itself a universal thread barrier. However UI, MonoBehaviours, Unity objects/resource loading, mutable contexts, static metric arrays and event callbacks need explicit ownership. Capture/freeze inputs, isolate workers, bound/cancel work, recheck exact predecessor and context before publication, marshal publication to main thread. [ToJson](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.ToJson.html), [FromJson](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/JsonUtility.FromJson.html). |
| Windows and future iOS Development profiling | **Windows first for controlled attribution; actual iOS required for mobile conclusions.** | Record backend/device/thermal/storage/GC/render conditions and profiler overhead. Windows Mono desktop results do not predict IL2CPP phone timings. No platform durability capability is implied by Windows success. [Unity collection guidance](https://docs.unity3d.com/6000.3/Documentation/Manual/profiler-profiling-applications.html). |

The architecture permits Unity profiling today; absent fine markers are an observability gap, not a prohibition. Its managed byte/JSON/iterator representation and single synchronous transaction coordinator prevent direct Jobs/Burst adoption. CPU reuse and reduced allocation should be evaluated before a representation rewrite. Decision 92 permits reduced cosmetic work before authority changes; no simulation approximation follows from it.

## 8. Relevant lessons from mobile reference games

Bounded public research accessed the supplied developer sources, official Unity guidance and a publisher case study on 2026-10-22. No binaries, device data, private code or iPhone access were used. No frame-rate or proprietary save/replay claim is inferred from footage.

| Reference / confidence | What the source establishes | Technique/domain and transferability | Differences / limits |
| --- | --- | --- | --- |
| **Dungeon Warfare — high for documented engine, low for performance internals** | Developer Singlecore's 2015 Unity post names Unity 3D Pro, RenderTexture, 2D Toolkit, JSON.Net and SoundManager Pro. | Confirms relevant Unity tooling in a dungeon game; RenderTexture concerns presentation. No reusable CPU/persistence optimization is documented there. [Developer discussion](https://discussions.unity.com/t/dungeon-warfare-hybrid-tower-defense-strategy-game/565714). | The post describes an early PC direction after an initial mobile plan. It proves neither current mobile architecture nor caching, Jobs, simulation complexity or transaction frequency. |
| **Magic Research 2 — high for documented calculation caching** | Developer modding concepts describe TypeScript-facing entities, singleton data stores, JSON-persisted state excluding `temp`, tick/permanent transformation caches and explicit invalidation. | Calculation/UI principle: use a cache only within a declared validity lifetime; avoid overly broad listeners. [Developer concepts](https://github.com/mcolotto/magic-research-2-modding-example/blob/main/docs/concepts.md). | The author warns that incomplete invalidation causes stale effects and excessive clearing slows the game. This does not establish Unity, checkpoint authentication, transaction atomicity or save frequency. Dungeon Lord's safe unit would need exact input/context identity and ownership proof. |
| **Pocket City / Pocket City 2 — high for PC2 rendering, unverified for original PC engine here** | Bobby's 2023 PC2 dev note explicitly uses Unity and compares billboards, atlases, combined meshes and chunk LODs. | Rendering: shared materials, combined chunk meshes/LODs, generate visible chunks; choose the measured bottleneck. [PC2 developer note](https://blog.pocketcitygame.com/dev-notes-rendering-large-3d-cities-on-mobile-for-pocket-city-2/). | Billboards reduced polygons but hurt draw calls/memory; free-roam preferred individual culling. This illustrates context-dependent tradeoffs, not Dungeon Lord replay/persistence improvement. Do not apply large-city mesh techniques to the current small tilemap without render evidence. Original Pocket City internals are not established by this PC2 article. |
| **Hero Making Tycoon — high for publisher attribution, low for engineering detail** | TapNation's case study identifies developer Hello Games and publisher TapNation and describes the idle loop. | No confirmed engine, calculation-cache, rendering, UI scheduling or persistence technique in the checked primary source. **No technical optimization is transferred.** [Publisher case study](https://www.tap-nation.io/blog/hero-making-tycoon-a-hybrid-casual-idle-mobile-game-case-study/). | Its “Performance Metrics” are commercial metrics, not CPU or frame measurements. Search results for a similarly named Unity “dressing” asset do not establish the shipped game's implementation. Large visible crowds are not evidence of equivalent deterministic verification. |

Reusable principles are bounded validity, selective updates and measurement of the actual bottleneck. The sources do not establish that any comparison game performs Dungeon Lord's every-prefix reconstruction and exact complete-save durability protocol.

## 9. Ranked optimization opportunities

Ranking combines evidence of cost, likely narrowness and correctness risk; it is not a promised implementation order or speedup. **P** = established repeated operation, **H** = plausible removable work whose safety/benefit is unproven. #247's two removals are proven historical examples, not remaining work. All future code changes require Windows comparison and eventual device validation before mobile acceptance.

### Common invariant proof required for every candidate

| Protected obligation | Proof required before removal/reuse/deferment |
| --- | --- |
| Deterministic execution/canonical order | Same input bytes, event kinds/order/ordinals/times, PRNG consumption, digests and outcomes across horizons/partitions; retain documented revision semantics |
| Stable IDs/exact start identity | No recreated ID, allocator advance or capture refresh; identical StartIdentity/start bytes and correct ownership |
| Versioned backward compatibility | Frozen formats 1/2, historical profiles and rejected unsupported versions remain unchanged; format-3 exact re-encoding |
| Side-effect-free validation | No mutation of canonical owners, receipts, wallet, history or cached objects from rejected/speculative work; alias/mutation probes |
| Exact predecessor | Capability binds complete original bytes and validation context/limits, not just revision/hash/RunId; stale disk/session fails with same reason |
| Release/settlement lineage | Pinned terminal + authorized retirement + transaction-local vacancy + exact release permit still required; self-consistent forged release fails |
| Monster/shared-state reconciliation | Net damage applies once to current stable identity/condition; custody/replacement/retirement, partial recovery and knowledge applicability unchanged |
| Atomic/durable publication | Fresh durable readback and semantic checks remain; failure publishes nothing; uncertain rollback remains recovery-required |
| Crash/reopen/idempotence | Same proven committed prefix after interruption/reopen, no duplicate settlement/recovery/release/admission; duplicate notifications no-op |
| Workload/rejection bounds | Same byte/node/replay/combat/owner limits and aggregate prechecks; no unbounded retained reuse, cache poisoning or changed negative semantics |

These obligations are cumulative. Preserving a digest alone proves neither authority nor vacancy. The candidate-specific controls below supplement this table.

### 1. Exact predecessor validation reuse inside one pump/preparation boundary

- **Subsystem/methods:** `AdvanceForeground.Stage`, `DetachedCanonicalSaveSession.PrepareReplacement`, `PrepareAndPersist.ValidateSession`, terminal `ValidateSession` and `lineage.Retired`.
- **Current behavior / evidence:** P: prior/candidate complete validation repeats as private sessions advance; corrected two-floor Prepare and Replay totals are large. Some identical predecessor bytes are validated at multiple sites. H: exact removable count and self time are unknown.
- **Narrow concept:** measure byte/context-identical predecessor validations, then consider passing a privately owned, unmodified proof/result to the immediately dependent consumer. Never substitute a mutated working owner for the original byte-bound predecessor. No cross-pump/global cache.
- **Benefit:** fewer full parses, planning/replays and allocations. **Difficulty:** medium/high. **Confidence:** high in repetition, medium in opportunity, low in a specific safe implementation before call-origin data.
- **Risks / tests:** `owned.LivingDungeon`, monsters and projections can alias mutable graphs. Add mutation-isolation and changed-context/limits negatives; retain stale lineage, forged release, closed-terminal, rollback/readback/reopen tests. Fresh durable bytes must still validate independently. All common invariants apply.
- **Dependencies / sequence:** first establish section 12's call origins and ownership; qualify one reuse edge at a time. Windows exact outputs plus device validation later.

### 2. Decode immutable captured plans/config/connections once per owned operation

- **Subsystem/methods:** `RunnableFloorSnapshot.MaterializePlan/TraverseConnection/TraverseBranchApproach/BranchSocket`, `ActiveFloorRunSnapshot.Configuration/InitialRuntime/LootConfiguration`, `CombatStartCapture.Validate/Recreate`, `RunSimulationService.StartCapturedSnapshot/CreateExecution`.
- **Current behavior / evidence:** P: accessors JSON-decode unchanged private strings repeatedly; execution creation clones already decoded values and materializes plans more than once. H: individual accessor/clone self cost is not recorded; replay multiplies these calls.
- **Narrow concept:** an operation-local decoded read model or explicit ownership transfer for one reconstruction, preserving copy-returning public boundaries. Reuse an immutable decoded input only where callers cannot mutate it; do not share a mutable `RunExecution`.
- **Benefit:** less JSON/setup allocation and repeated geometry preparation. **Difficulty:** medium. **Confidence:** medium.
- **Risks / tests:** route/traversal mutations, null/empty legacy distinctions and old config/start capture semantics. Every-prefix byte/golden tests, edits/config mutation after capture, independent simultaneous executions, malformed plans and exact work-limit negatives must pass. Retain common publication/lineage safeguards unchanged.
- **Dependencies / sequence:** accessor allocation stacks and call counts first; separate this from complete-save validation reuse. Windows before/after plus eventual iOS/backend equivalence.

### 3. Avoid a separate presentation reconstruction when an equivalent committed inspection can be derived safely

- **Subsystem/methods:** `SaveService.InspectForeground`, `LivingDungeonContracts.Restore(retainPresentation: true)`, pump-local `Execution`, readback projection.
- **Current behavior / evidence:** P: every successful pump reinspects through fresh party replay; corrected Windows two-floor Inspect is 3,010 calls / 41,163.485 inclusive ms. Existing live executions already contain presentation steps within that pump.
- **Narrow concept:** after durable success, derive immutable presentation values from a demonstrably matching execution/result, bound to the final party checkpoint and geometry. Keep normal restore on boot/reopen and when matching evidence is absent; retain validation of durable bytes.
- **Benefit:** fewer replay/setup allocations on publication. **Difficulty:** medium/high. **Confidence:** medium.
- **Risks / tests:** runtime executions can differ from final readback or have consumed terminal state; same-session presentation cannot become authority. Test byte/ordinal mismatch fallback, geometry edit hiding, selected floors, pause/reopen, failed persistence producing no new view, waiting/release and full presentation equivalence. Common invariants remain in the writer.
- **Dependencies / sequence:** call-origin data, then a private immutable inspection contract; do not retain live cursors across publications. Windows scene comparison and eventual device UI validation.

### 4. Reduce whole-runtime JSON capture/projection churn without changing complete-save authority

- **Subsystem/methods:** pump-entry `SaveData` clone, `DetachedRecognizedSaveStateSnapshot.Capture`, `CanonicalMvpRouteProjection.TryPublishValidated`, recognized-history merge.
- **Current behavior / evidence:** P: complete runtime JSON encode/parse and root deserialization recur per Stage/projection; frame allocations are large. H: how much belongs to these sites versus checkpoint replay is unknown.
- **Narrow concept:** measure first; then consider explicit typed detached copying or reuse of unchanged recognized member bytes within one transaction. Keep unknown/lexical/null extension preservation in the strict writer and exact final serialization.
- **Benefit:** lower transient allocations/GC pressure and serialization time. **Difficulty:** high because SaveData is broad. **Confidence:** medium in churn, low in the smallest safe replacement.
- **Risks / tests:** omitted fields, future recognized-member drift, shared arrays, transient evidence, exact numeric/null encodings and settlement's selective merge. Require recognized-state contract/semantic tests, unknown-member lexical preservation, null versus default, failed transaction isolation and complete-save byte equality. All common invariants apply.
- **Dependencies / sequence:** allocation attribution and field inventory; prefer a bounded field/subtree experiment over replacing all serialization. Windows and eventual device allocation/GC evidence required.

### 5. Remove only provably redundant private staging or current-feasibility preparation

- **Subsystem/methods:** `AdvanceForeground.Stage` around passive/opportunity/recovery/closure, terminal staging, `PrepareAndPersist` multi-floor snapshot feasibility (450–454).
- **Current behavior / evidence:** P: multiple complete candidates can be prepared before one disk commit; a fresh current snapshot is built for multi-active-floor final candidates. H: semantic equivalence of collapsing any boundary is unproven.
- **Narrow concept:** identify adjacent identical complete candidates/context or unchanged feasibility inputs; eliminate only an exact duplicate or reuse a bounded feasibility proof. Do not remove the unconsumed-terminal stage or settlement/release authorization based on timing proximity.
- **Benefit:** fewer complete candidate preparations; may not reduce disk writes. **Difficulty:** high. **Confidence:** low/medium.
- **Risks / tests:** altered revisions, authorization predecessor, passive-before-event and recovery-before-release/admission ordering, changed rejection timing. Equal-time both-sequence tests, one-boundary versus full-horizon/reopen, exact stale permits, active edits, and failed-persistence negatives are mandatory. Common invariants all apply.
- **Dependencies / sequence:** stage-reason and byte-identity measurements; separate proof of each collapsed edge. Windows and eventual device validation.

### 6. Bound reused admission setup while retaining every required prefix verification

- **Subsystem/methods:** `LivingDungeonLifetimeAssurance.Check/Verify`, `CombatStartCapture.Recreate`, supported readers.
- **Current behavior / evidence:** P: normal and authentic alternative transfer paths repeatedly reconstruct starts and prefixes; 69 probes consume 75,433.056 inclusive ms in corrected Windows two-floor capture. H: avoiding any full-prefix check safely is unproven.
- **Narrow concept:** investigate reuse of immutable decoded start/setup only. Do not skip a prefix, replace reconstruction with checksum acceptance or reuse a probe from another RunId/condition snapshot. Each prefix must still meet the existing executable/byte/work proof.
- **Benefit:** admission CPU/allocation reduction. **Difficulty:** high. **Confidence:** medium for setup reuse, low for incremental validation substitution.
- **Risks / tests:** future checkpoint becomes unpersistable, alternative-only byte/work overflow, waiting duration/control width, terminal prefixes and stable admission rejection. Test every normal/EXIT/DESCEND/blocked/released prefix, corrupt evidence, all current envelopes and no private-probe canonical effects. All common invariants apply.
- **Dependencies / sequence:** separate setup, replay, export and byte-comparison costs; candidate 2 may supply a narrower proof first. Windows admission distributions and eventual device validation required.

### 7. Narrow UI publication refreshes and repeated fingerprints

- **Subsystem/methods:** `ProductionDungeonController.CanonicalPublished/Present`, `GameRoot.PublishCanonicalRuntime/PumpForeground`, foreground presentation guards and spatial fingerprints.
- **Current behavior / evidence:** P: publication always calls broad `Present`; root refreshes some informative lines during publication and again after the pump. Geometry rebuild and foreground text already have guards. Publication/Present totals are much smaller than enclosing preparation/replay; no evidence they explain second-long admissions.
- **Narrow concept:** after profiling, refresh only read models whose semantic inputs changed; avoid duplicate formatted strings/fingerprints within one publication. Preserve selected-floor motion updates and dirty state after edits/language/text-size changes.
- **Benefit:** smaller main-thread/UI allocation cost, especially between events. **Difficulty:** low/medium. **Confidence:** medium for repeated calls, low for large total benefit.
- **Risks / tests:** stale condition/mana/reserve/eligibility, lost accessibility/localization updates and edit/replay controls. Focused scene/readability/resolution/text/language/floor-switch tests and failed-write unchanged-view tests; common simulation/save invariants stay untouched.
- **Dependencies / sequence:** UI marker/call-stack evidence; independent from writer optimization. Windows manual editor/standalone checks and eventual device safe-area/touch validation.

### 8. Detached background preparation or private frame-budgeted continuation

- **Subsystem/methods:** foreground coordinator, candidate validation, admission assurance, persistence/publication barrier.
- **Current behavior / evidence:** P: synchronous chain blocks main thread. H: a safe and beneficial asynchronous boundary has not been demonstrated.
- **Narrow concept:** only after reduced duplicate work, prototype private immutable-input preparation with one serialized publication owner. Use exact predecessor/context recheck and explicit cancellation/lifecycle behavior; retain fresh durable verification. A scheduling change is a separate architecture decision, not a cache implementation detail.
- **Benefit:** responsiveness/main-thread scheduling; total CPU may remain or increase. **Difficulty:** high. **Confidence:** low until isolation and lifecycle proofs exist.
- **Risks / tests:** edits during work, focus/pause/quit deadlines, stale publication, duplicate work, out-of-order results, worker exceptions and static metrics races. Require deterministic schedule permutations, worker interruption/reopen/failure injection, bounded backlog and main-thread-only UI publication. Every common invariant applies, including stable rejection outcomes and no partial timestamps.
- **Dependencies / sequence:** all relevant ownership and exclusive-cost evidence first. Controlled Windows total CPU/wall/main-thread comparison and eventual actual-device thermal/memory/lifecycle qualification are essential.

## 10. Unsafe or premature optimization approaches

- **DOTS/ECS rewrite:** no evidence entity dispatch dominates; would replace representation and multiply compatibility risk before measuring duplicate work.
- **Burst/Jobs across the existing coordinator:** managed JSON/iterators are unsuitable as-is; parties share causal wallet/knowledge/recovery/vacancy effects. Distinct floors do not prove independence.
- **Global validation cache keyed only by revision, RunId, hash or “unchanged dungeon”:** insufficient context, mutable aliases, historical profiles, byte pins and release capabilities. A cache hit must not authorize settlement or bypass fresh durable readback.
- **Replace replay with digest/hash checks or drop admission lifetime checks:** self-consistent evidence does not establish causality; late byte/work failure could make an admitted run unpersistable.
- **Batch disk commits across gameplay timestamps or remove lifecycle flush/readback:** changes crash-loss and edit ordering contracts; not a performance-only correction. First distinguish private Stage CPU from actual disk cost.
- **Coroutines as a CPU optimization:** they can partition permitted private work but do not supply parallelism or reduce calculation. Yielding inside a published timestamp violates closure.
- **Approximate events, reduce simulation frequency, alter timings/party capacity or loosen validation limits:** changes the workload/outcomes instead of preserving correctness. Cosmetic degradation is a separately permitted presentation tool, not authority reduction.
- **Aggressive pooling/native conversion of the entire state:** ownership/leak/reset risk without allocation-site evidence. Shared pooled buffers must never alter historical checkpoint bytes.
- **Mesh batching/LOD because PC2 uses it:** only justified after a measured rendering bottleneck. The observed pump stall already contains substantial CPU work before rendering.
- **Treat #248 as a runtime speedup:** it reduces model/process-supervision dependence, not Unity computation. It also does not automatically prepare safe new campaigns from frozen historical adapters.

## 11. Proposed controlled measurement plan

**Design only; execute none of this for the audit PR.** Begin with small explicit cases; do not select sustained profiles or full suites implicitly. Use fresh isolated product/save/output identities and the current preservation authorities. Historical campaigns are immutable. #248 can supervise reviewed commands; frozen #247 adapters need a reviewed fresh entry point, not source-text substitution or a reopened campaign.

Record source/config/tool hashes, exact inputs, backend/platform, event trace, start/checkpoint/predecessor identities, ordinal/depth, captured byte/tile/room/assignment counts, stage reasons, party count, main-thread and wall time, managed allocation availability and GC collections. Record marker nesting and **exclusive/self** work as well as inclusive totals. Gate recording outside normal gameplay; avoid diagnostic serialization that recreates the hotspot being measured. Warm-up and profiler-enabled/disabled repetitions must be separated.

| Minimal comparison | Hold fixed / vary | Question answered |
| --- | --- | --- |
| One versus several legal rooms | Same executable/config and party/start policy; authored geometry/content variants, measured by matched step kind and prefix ordinal | Setup/path/room cost independently of concurrency; report unavoidable route-length changes explicitly |
| Same constructed content, one versus two Active floors | Same IDs, Floor 1, Floor 2 content and backend; activation state varies | Cost of including Floor 2 in capture/feasibility, not content construction |
| More content with one retained party | Legal extra assignments; test-only direct capture/step harness progresses a single authentic party without admitting another | Content/setup cost independently of concurrent canonical scheduling; label this diagnostic workload |
| One versus two retained parties | Replay identical pinned per-party inputs at matched early/middle/late ordinals; compare each alone and both in legal canonical state | Per-party validation multiplier and shared-state overhead; do not force otherwise illegal occupancy |
| One prefix read versus complete candidate preparation versus publication | Same exact pinned bytes/context and bounded event; record nested clone/parse/setup/replay/export/projection and durable callback/I/O subscopes | Simulation calculation versus verification versus filesystem and readback |
| Event kinds | Explicit normal movement, loot/trap/combat, admission, terminal settlement, Blocked, Released and passive tick cases | Avoid aggregate means hiding expensive composition; retain overlapping tag relationships |
| Prefix depth | Same captured start, selected early/middle/late legal checkpoints | Setup intercept versus replay depth; count actual replayed engine/combat steps |
| Main thread versus wall | Timeline thread identity, render waits, active CPU, I/O and full-frame recorder | CPU stall versus waiting versus total processing; later asynchronous comparison must report both |
| Allocation versus retention | GC.Alloc stacks around short cases, collection counts; optional separately approved snapshots at stable before/after boundaries | Temporary JSON/array churn versus retained memory and GC pause |
| Windows versus eventual iOS | Same logical trace/input hashes and release-compatible executable, separate device/backend/environment runs | Platform-specific performance; no desktop scaling formula |

A read-only pure replay harness can isolate computation without production writes; an in-memory qualified filesystem can isolate CPU preparation from OS persistence. Such results are **diagnostic component comparisons**, never replacement production durability qualification or a weakened performance score. Keep an intact end-to-end write/readback case using the same logical input and established atomic writer.

Start with one representative bounded case per phase and a small predeclared repetition set for variability; expand only if variability/coverage prevents a conclusion. Do not call that sample a meaningful p95 without enough observations. Preserve original event traces and hard work limits. No owner performance threshold is invented by this plan.

Focused correctness selection for each candidate is in section 9. Shared anchors include these existing `LivingDungeonTwoFloorTests` methods: `InspectionReusePreservesOriginalFoldAndCheckpointAtEveryPrefix`, `HistoricalGoldenRestoresAndReencodesExactPinnedBytes`, `SelfConsistentReleaseWithoutVacancyCannotPublishThroughSession`, `EmptyProjectionAndStaleLineageCannotAuthorizeRelease`, `FailedReleaseTransactionPublishesNoSettlementOrTransferAndRetryIsExactlyOnce`, `WaitingReleaseIsEquivalentAcrossForegroundPartitionsAndReopen`, `OverlappingSnapshotsPreservePriorDamageAndPartialRecoveryAtBothSettlements`, `TerminalPrefixOnlyFitsAnUnclosedV2Boundary`, and `EqualTimeOrdinaryTransferAndBlockedReleaseRespectSequenceAndRecoveryPhases`. Add alias/context/limit negatives for any actual reuse implementation rather than tests that merely mirror its code.

For an eventual **behavior-changing optimization merge**, retain affected B1/B2/B3A/B3B1/B3B2/C0/C1/C2 and save/migration/atomic failure tests; complete EditMode/PlayMode gates with exact allowed skips and source applicability; fresh Windows Development build; real wait/reopen/release/finish and historical-v1 continuation; exact output and preservation comparisons; and relevant editor/standalone UI inspection. Sustained profiles remain final gates where the existing contract requires them, not an early investigation loop. A new source change cannot inherit old test passes. Actual iOS validation remains necessary for mobile acceptance, but does not need to precede a Windows-only diagnostic PR.

For the **measurement-only next PR**, use focused diagnostic-neutrality tests and a bounded Windows standalone capture; broader full suites/campaigns require a demonstrated behavioral surface or explicit final-gate applicability. The existing complete gates are not removed or renamed to manufacture a pass. This documentation-only PR needs none of those Unity executions.

## 12. Recommended first correction PR

**One recommendation: “Measure foreground validation origins and exclusive preparation costs.”** This is a measurement-only implementation PR, not an optimization or architecture rewrite.

**Objective / why now:** resolve the missing attribution required to choose one safe reuse edge. Record why complete validation/replay occurs, whether identical complete bytes/context are repeatedly processed, and how much self time belongs to JSON/copy, snapshot setup, actual engine replay, comparison and durable I/O/readback. Existing source proves repetition and substantial inclusive cost, but not which mutable result can be reused safely. The two already proven narrow removals are merged. Guessing a broader cache now would cross a new ownership boundary.

**Dependencies:** latest main and #247/#248; this audit; approved deterministic/save/two-floor contracts; reviewed fresh diagnostic isolation/entry point using current preservation authorities. No Phase 8 feature, new package, save migration or backend is required.

**Exact intended files/systems** (relative to repository root; additions may include their required Unity metadata):

- `Assets/_Project/Scripts/Core/ForegroundQualificationMetrics.cs`: fixed named marker scopes and bounded opt-in origin/depth counters; preserve existing area meanings.
- `Assets/_Project/Scripts/Core/GameRoot.ForegroundProfile.cs`: explicit small-case reporting separate from the frozen sustained workload; keep original profile behavior/counts untouched.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs`: stage-reason and pump/terminal origin scopes only.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs`: recognized capture, predecessor/candidate validation and release-check scopes only.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs`: final predecessor, feasibility and durable callback scopes only.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/ExactCompleteSaveAtomicPersistence.cs`: separately measure existing durable write/read/replace operations and callback; no filesystem protocol change.
- `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatCheckpointCodec.cs`, `TransferCheckpointCodec.cs`, `CombatStartCapture.cs`, `RunSimulationService.Execution.cs`: reconstruct/setup/replay/export/compare and JSON clone scopes only.
- New `Assets/_Project/Tests/EditMode/ForegroundPerformanceInstrumentationTests.cs` and `Assets/_Project/Editor/DungeonSpatial/Tests/ForegroundPerformanceDiagnosticSceneTests.cs`: bounded neutrality/accounting cases, no sustained campaign hidden in a test.
- New `Docs/testing/foreground-performance-diagnostic-measurement.md`: commands, source identities, attribution results, limitations and preservation. No edits to historical reports/fixtures.

This is a reviewable proposed file envelope, not authorization to implement it during the audit. If a new native diagnostic entry point is necessary beyond the existing development profile bridge, review that scope explicitly; do not expand the historical qualification adapters by substitution.

**In scope:** markers with static names; bounded diagnostic records for stage/reader origins and prefix depth; exact byte/context repetition accounting performed outside timed spans or calibrated for overhead; short component and intact end-to-end cases from section 11. Instrumentation must observe existing calls, not call `Inspect`, export, hash or serialize again inside a measured region merely to label it.

**Non-goals:** validation removal, caches, worker scheduling, Jobs/Burst/Collections conversion, UI refresh changes, tuning, new services, changing admission lifetime probes, writes/readback frequency, sustained 68-run campaigns, roadmap changes or a claimed speedup.

**Protected invariants:** every obligation in section 9; exact event/timing/random/order/ID/start and checkpoint semantics; existing stable rejection/rollback behavior. Diagnostics cannot authorize gameplay. Keep metrics disabled outside explicit diagnostics and bound records before collection; do not add thread-shared mutable counters in anticipation of workers.

**Save/schema/migration:** no field, byte format, writer target, schema, migration or persisted metric changes. Writable target stays 15; frozen readers and profile definitions remain identical. **Determinism/localization:** diagnostics never influence decisions; no new tuning literals or player-visible text. Technical marker names are internal diagnostics, not localization substitutes.

**Acceptance criteria:**

1. The same bounded scenarios with diagnostics enabled/disabled produce exact checkpoints/start identities, canonical bytes and committed event traces; compare lifecycle UTC only under the previously documented narrow normalization when real clock differences require it.
2. Report complete validation and reader counts by call origin, party and ordinal; reconcile nested/inclusive versus self time and explain any remaining unassigned counts. Determine whether Editor extra Stage calls arise from driver/closure partitioning or another path.
3. Attribute clone/capture/projection/setup/replay/export and durable filesystem/callback work without summing overlapping scopes; unsupported counters remain unavailable. Measure instrumentation overhead separately.
4. Identify at least one exact repeated byte/context edge and its mutation/ownership exposure, or explicitly conclude no safe elimination is established. No numerical speedup is an acceptance condition.
5. Original sustained profile selection, correctness tests/allowed skips and preservation controls remain unchanged; reports belong only to new unused outputs.

**Focused automated tests:** new metric reset/disabled/bounded-record/count nesting tests; bounded enabled-versus-disabled byte equivalence for normal, admission, terminal and blocked/released cases; historical formats 1/2 and corrupt/over-budget inputs; stale predecessor/release/readback failure negatives. Reuse the named section 11 safeguards with explicit filters, not broad fixtures that implicitly run sustained methods.

**Controlled comparison:** a short Windows Development capture with the production controller and intact writer, paired with the same pinned input in CPU-only replay/preparation diagnostics. Record fixed source/input and event-kind/ordinal/size dimensions, not just completed-run means. This is new future evidence; do not reuse the old sealed player as if it contained markers.

**Manual expectations:** developer checks marker nesting in Editor and Windows standalone, idle versus each selected boundary, floor switch/edit/pause and localized feedback continuity; real isolated close/reopen if the bridge touches lifecycle driving. No owner gameplay session is needed merely to approve internal diagnostic output. No mobile performance claim until an eventual real iOS profile with supported platform persistence.

**Main risks / merge evidence:** diagnostic overhead, extra serialization for labels, exception paths with unbalanced scopes, counters changing behavior, misleading self-time accounting and accidental profile selection expansion. Merge evidence must include focused tests, enabled/disabled equivalence, source/input/output hashes, overhead disclosure, representative Timeline/allocation evidence, count reconciliation and zero-exception preservation. If the instrumentation changes behavior, stop treating it as measurement-only and apply the appropriate complete correctness gates before merge. Owner authorization for that future PR remains separate from this advisory recommendation.

## 13. Risks, open questions and evidence gaps

- No exclusive breakdown currently separates actual simulation, duplicated setup/serialization, semantic validation or OS I/O. Inclusive totals establish hotspots, not independent percentages.
- No per-origin tally proves how many remaining validations consume identical bytes/context. Session byte immutability does not make exposed validation graphs immutable. Stage mutates private owner/projection objects; alias analysis is essential.
- Per-prefix reader setup, repeated route/config decoding and planning appear promising, but malformed-input behavior and isolation constrain reuse. Validation cached by start identity alone cannot cover checkpoint progress, profile limits or canonical predecessor semantics.
- The extra Editor private Stage count is unexplained. The adapter explicitly disables normal root Update, so “double Unity Update” is not an established cause.
- No independent OS write/flush trace exists in the cited summaries. Readback semantics, Windows capability probing and filesystem latency share AtomicWrite time.
- Thread allocation counters are unavailable. Recorded frame allocations do not distinguish live retention, allocation sources or GC pause cost. No claim of a memory leak is made.
- Selected-floor rendering and UI are already guarded, and their recorded scopes are smaller; total rendering is not exhaustively instrumented. Desktop batchmode graphics cannot settle mobile rendering questions.
- No iOS runtime, IL2CPP, device thermal/storage/lifecycle or native durability qualification was performed. Current Windows NTFS implementation is not evidence of an iOS save backend. Future platform work must preserve the same authority contract with a separately qualified implementation.
- The historical normalized-save equivalence proves #247's correction only. It does not prequalify any candidate in this audit. Functional prototype acceptance does not establish release responsiveness.
- Public reference games disclose limited internals. Hero Making Tycoon supplies no verified technical technique; PC2's rendering choices and MR2's cache warnings cannot establish Dungeon Lord speedups.
- No design decision is needed to merge this documentation. Starting the proposed follow-up, adding a profiling package, adopting asynchronous execution or changing any persistence/scheduling contract requires its own reviewed scope; none is authorized or implemented here.

## 14. Sources and exact repository references

All repository ranges below refer to `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`; links are relative to this report. Public sources were accessed on 2026-10-22 and are linked beside their claims above.

| Repository source | Methods / relevant lines |
| --- | --- |
| [AGENTS.md](../../AGENTS.md), [model policy](../process/AI_Model_Selection_Policy.md), [development rules](../development/codex-development-rules.md) | Repository guardrails, classification/defaults and implementation governance |
| [Active execution plan](../planning/post-gd60-mvp-execution-plan.md) | Phase 7 235–267; later precedence/two-floor notes 386–398, including historical premerge wording |
| [Phase 7A0 lock](../planning/phase-7a0-graphical-dungeon-editor-design-lock.md) | Decision 92 899–913; Living Dungeon/responsive-editing addenda 1211–1229 |
| [Living Dungeon owner lock](../planning/decisions/living-dungeon-owner-design-lock.md), [Phase 8 dependency plan](../planning/phase-8-dependency-roadmap.md) | Immutable starts, authoritative events, selected-floor presentation; future capabilities versus completion |
| [B2 contract](../planning/living-dungeon-phase-b2-implementation-contract.md), [B3A](../planning/living-dungeon-phase-b3a-implementation-contract.md), [B3B1](../planning/living-dungeon-phase-b3b1-implementation-contract.md), [B3B2](../planning/living-dungeon-phase-b3b2-implementation-contract.md) | Versioned bounded persistence, synthetic versus production settlement, timed C2 compatibility and foreground integration |
| [Two-floor contract](../planning/living-dungeon-two-floor-foreground-contract.md) | Complete approved transfer/ordering/lineage/lifetime/schema contract |
| [Spec 17](../17%20-%20Save_State_Offline_Simulation_and_Time_Handling.md), [Spec 28](../28%20-%20Save_Data_Model_Versioning_and_Migration.md), [invariants](../Cross_Spec_Glossary_of_Invariants_UPDATED.md) | Deterministic time/catch-up, detached atomic readback, migration compatibility, INV-12 draft safety and INV-15 stable spatial authority |
| [GameRoot.cs](../../Assets/_Project/Scripts/Core/GameRoot.cs) | Update 271–282; lifecycle 285–366; runtime publication 786–806 |
| [GameRoot.Foreground.cs](../../Assets/_Project/Scripts/Core/GameRoot.Foreground.cs) | Initialize 32–44; Update/Pump 58–114; Flush 118–130; passive tick 132–141 |
| [TimeService.Foreground.cs](../../Assets/_Project/Scripts/Services/TimeService.Foreground.cs) | Begin/sample/focus 14–40 |
| [SaveService.Foreground.cs](../../Assets/_Project/Scripts/Services/SaveService.Foreground.cs), [SaveService.cs](../../Assets/_Project/Scripts/Services/SaveService.cs) | Action barrier/advance/inspect 15–113; ordinary canonical Save 407–429 |
| [LivingDungeonForeground.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForeground.cs) | Entry/clone 14–45; Stage 47–66; execution reuse/order/settlement 84–154; release 156–170; demand/recovery/boundaries/admission 172–288; final Persist 292–298 |
| [DetachedCanonicalSaveSession.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs) | Recognized Capture 24–50; protected session/Open 157–189; PrepareReplacement 211–404; prior/candidate/release replay 341–392; foreground adapter 452–459 |
| [DetachedCompleteSaveContract.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs) | Contextual validation 167–195; parse/owners 258–367; allocator/base budget/frozen validation/byte round trip 368–416 |
| [LivingDungeonContracts.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonContracts.cs) | Owner representation 60–84; chunk write/read 93–199; Restore 203–221; aggregate/profile/occupancy validation 223–279 |
| [LivingDungeonLifetimeAssurance.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonLifetimeAssurance.cs) | Check/alternative branch 8–33; per-prefix Verify 35–43 |
| [LivingDungeonForegroundLineage.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundLineage.cs) | Exact transaction bytes 21–27; Retired 28–45; Release and permit 47–75 |
| [LivingDungeonScheduling.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonScheduling.cs) | Horizon/allocator rules 13–105; Pin 106–112; historical technical scheduler 121 onward, distinct from production coordinator |
| [Foreground reconciliation](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonForegroundReconciliation.cs), [outcome consequences](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonOutcomeConsequences.cs), [DurableSettlementHistory](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DurableSettlementHistory.cs) | Current-state net damage/receipt verification 14–68; Heat/knowledge/history composition 16–73; exact history authorization/merge 15–136 |
| [DetachedCanonicalWriteAuthority.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs) | PrepareAndPersist 392–474; ValidateSession 477–479 |
| [ExactCompleteSaveAtomicPersistence.cs](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/ExactCompleteSaveAtomicPersistence.cs), [Windows filesystem](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/WindowsSpatialMigrationFileSystem.cs) | Persist 11–78; evidence/rollback 80–138; Windows durable writes/replace/capability boundary 157–195 |
| [CanonicalMvpRouteProjection.cs](../../Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalMvpRouteProjection.cs) | TryPublishValidated 40–83; active-floor projection 144–171 |
| [RunSimulationService.cs](../../Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.cs), [Execution partial](../../Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.Execution.cs), [Combat partial](../../Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.CombatC0.cs) | Shared route iterator; timed start 25–34; captured creation/cloning/planning 37–129; ordered engagement resolution |
| [RunExecution.cs](../../Assets/_Project/Scripts/Gameplay/RunSimulation/RunExecution.cs), [transfers](../../Assets/_Project/Scripts/Gameplay/RunSimulation/RunExecution.Transfers.cs) | Inspect/advance/deadline/announcement/fold 145–229; export/metadata 261–272; versioned transfer state transitions |
| [ActiveFloorRunSnapshot.cs](../../Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs), [CombatStartCapture.cs](../../Assets/_Project/Scripts/Gameplay/RunSimulation/CombatStartCapture.cs) | Snapshot JSON access/connection reconstruction 15–122; Create 157 onward; start Recreate 71–110 and Validate 112–158 |
| [CombatCheckpointCodec.cs](../../Assets/_Project/Scripts/Gameplay/RunSimulation/CombatCheckpointCodec.cs), [TransferCheckpointCodec.cs](../../Assets/_Project/Scripts/Gameplay/RunSimulation/TransferCheckpointCodec.cs) | Combat inspection/fold/export 15–75; supported dispatch/replay 77–136; format-3 fold/export/restore 34–111 |
| [ProductionDungeonController.cs](../../Assets/_Project/Scripts/UI/ProductionDungeonController.cs), [foreground presentation](../../Assets/_Project/Scripts/UI/ProductionDungeonForeground.cs), [world foreground](../../Assets/_Project/Scripts/UI/DungeonFloorWorldForeground.cs) | Update 63–85; publication 365–388; Present 412–451; foreground dirty guards/interpolation |
| [ForegroundQualificationMetrics.cs](../../Assets/_Project/Scripts/Core/ForegroundQualificationMetrics.cs), [GameRoot.ForegroundProfile.cs](../../Assets/_Project/Scripts/Core/GameRoot.ForegroundProfile.cs) | Aggregate scope definitions 9–36; harness/recorders/classification 34–111 |
| [LivingDungeonTwoFloorTests.cs](../../Assets/_Project/Tests/EditMode/LivingDungeonTwoFloorTests.cs), [profile scene adapter](../../Assets/_Project/Editor/DungeonSpatial/Tests/LivingDungeonTwoFloorProductionProfileSceneTests.cs) | Every-prefix/golden 43–98; negative/order/reopen/condition 161–566; three-room production fixture 625–657; seed exporter 659–693; Editor driver 121–179 |
| [ProjectVersion](../../ProjectSettings/ProjectVersion.txt), [manifest](../../Packages/manifest.json), [lockfile](../../Packages/packages-lock.json) | Editor and resolved dependency versions; local PackageCache metadata corroborated read-only |

Evidence and history:

- [Performance correction report](living-dungeon-two-floor-performance-correction.md), [initial/final qualification narrative](living-dungeon-two-floor-qualification.md), [supervisor workflow](qualification-supervisor.md).
- [Corrected Windows one-floor summary](evidence/living-dungeon-two-floor/performance-correction/windows-one-summary.json), [two-floor summary](evidence/living-dungeon-two-floor/performance-correction/windows-two-summary.json): `categories`, `taggedFrames`, `areas`, `workload` and counter availability.
- [Corrected Editor one-floor](evidence/living-dungeon-two-floor/performance-correction/editor-one-summary.json), [two-floor](evidence/living-dungeon-two-floor/performance-correction/editor-two-summary.json).
- [Comparison and exact equivalence](evidence/living-dungeon-two-floor/performance-correction/comparison.json): pins all before/after summary paths and hashes, boundary projections and narrow lifecycle normalization.
- [Correction qualification ledger](evidence/living-dungeon-two-floor/performance-correction/qualification-ledger.json), [preservation summary](evidence/living-dungeon-two-floor/performance-correction/preservation-summary.json): source/build/applicability, commands and prior correctness evidence; inspected, not rerun.
- [PR #247](https://github.com/gdg3417/Dungeon-Lord/pull/247): merged feature/correction, final owner Windows prototype acceptance and remaining stall limitation. [PR #248](https://github.com/gdg3417/Dungeon-Lord/pull/248): merged completion-driven supervisor. Their merged Git history was inspected locally; this audit does not rewrite historical report status.

Audit validation: verified baseline and runtime-tree applicability, eight pinned summary hashes, numerical claims against committed JSON, source/method references, Markdown links, changed-file scope and Git whitespace checks. No runtime qualification is claimed by this document. The sole advisory next implementation PR is the measurement-only packet in section 12.
