# GD66 save-spatial migration workload sizing evidence

## Phase 3 retained-custody requalification (2026-09-09)

**Local closeout proposal, measured in Unity 6000.3.2f1; not a new owner-approval claim.** The historical GD66 evidence below remains intact. Its 26-record R2 model excluded schema-8 floor lifecycle and returned custody. Deleting a room frees physical tiles but preserves reusable assignments as separate custody records; the record budget therefore grows across valid cycles. There is no custody-consumption path in Phase 3.

The production profile change in this working tree is **`MaximumCanonicalSpatialRecords: 64 → 165` only**. The other sixteen fields remain at their previously approved values. This is a measured requirement for a bounded workload, with no additional record headroom. Schema 8, migrations, serialized field shapes, identity formats, and ownership semantics are unchanged.

### Supported workload and derivation

The horizon comes from the existing owner-approved **128-element raw array ceiling**, not an invented lifetime allowance. The harness executes 32 cycles with four reusable Basic Room contents per cycle (two distinct monsters and two distinct traps), accumulating 128 distinct returned assignments. It constructs Basic Room at `(0,7)`, orientation Zero, terminal east; deletes the tail; verifies the repaired `(1,6)` physical corridor; reopens the complete save; and reconstructs. Loot-bearing rooms are never deleted because shipped loot removal remains unresolved.

After those cycles it constructs two more Basic Rooms at `(5,2)` and `(5,6)`, fills all three rooms to production 2/2/2 capacities, and retains the actual one-tile Straight Stone Corridor. This is a production-valid 59/60 floor-space layout. The structural/content count is **37** (`1 floor + 1 floor lifecycle + 3 rooms + 5 nodes + 4 edges + 2 fixed structures + 3 room semantics + 18 assignments`). Adding 128 custody records requires **165 canonical records**. Production floor capacity allows at most three current rooms: four minimum-size Rectangle Rooms plus fixed structures already require 70 units against 60. The three-Basic layout is the largest current record combination; current distinct content options also bound the larger rooms' usable assignment counts. This models the supported Phase 3 required route, without branching or corridor-content placement.

The complete-save fixture reuses GD66's representative populated 5 × 6 economic layout, runtime counters/timestamps, offline summary, production loot/run assets, and research resolvers. It generates ten actual `RunOutcomeRecord`s from the current canonical route through `RunSimulationService` and retains the configured history count. Coherent active-research and completed-research/objective rows are separate. Unknown root `[1,true]` and primary `{"note":"preserve"}` values survive every write and reopen. Frozen legacy spatial evidence remains independently retained by the existing session.

`Gd66SaveWorkloadMeasurementTests` extends the existing minimum-success binary searches rather than replacing the historical 30-row harness. Current rows use schema-8 parsing and include both lifecycle collections in the domain record counter. They measure raw scanner depth/member/array/string/work thresholds, complete-save strict node/record/string thresholds, candidate/copy/unknown limits through actual session replacement, and exact canonical record minima. Each minimum search verifies success at its threshold and refusal immediately below it. Raw accounting uses the same scanner as boot. Canonical owners are excluded from unknown-preservation totals; the frozen legacy classifier labels those current owners as unknown, so counting them as preservation would be incorrect.

### Exact Phase 3 measurements

These values were emitted by the Unity measurement fixture, not estimated from source JSON or a substitute serializer. The active-research row is the largest byte/node/string case; completed research uses 2,411 strict collection records versus 2,409 in the active row.

| Limit/accounting | Measured Phase 3 high water | Production proposal | Evidence/policy distinction |
|---|---:|---:|---|
| Raw save bytes | 204,461 | 524,288 | Existing approved headroom; historical GD66 raw high water remains larger at 266,907 |
| Raw nesting depth | 10 | 32 | Existing approved headroom |
| Raw object members (per object) | 37 | 64 | Existing approved headroom |
| Raw array elements (per array) | 128 | 128 | Exact retained-custody boundary; no added array headroom |
| Raw string bytes (per token) | 69 | 4,096 | Existing approved headroom |
| Raw scan work | 209,358 | 1,048,576 | Actual scanner minimum, independent of bytes; historical rows still peak at 270,356 |
| Strict serialized input bytes | 204,461 | 262,144 | 57,683 bytes remaining for this fixture |
| Strict parsed nodes | 6,381 | 8,192 | 1,811 nodes remaining |
| Strict collection records | 2,411 | 4,096 | 1,685 records remaining |
| Strict decoded string characters | 171,488 | 262,144 | 90,656 UTF-16 units remaining |
| Retained diagnostics | 0 (valid inputs) | 64 | Existing bounded diagnostic policy and tests retained |
| Canonical spatial records | 165 | **165 (was 64)** | Exact measured/model-derived requirement; no new policy headroom |
| Canonical saved corridor tiles | 1 | 64 | Existing allowance; modeled maximum 8 for two rooms (two downstream edges × length 4); three rooms leave at most 5 floor-space units for corridors, and the entrance edge remains direct. Room/fixed tiles are different accounting |
| Whole-save candidate bytes | 204,461 | 262,144 | Actual complete-session replacement threshold |
| Copied recognized value bytes | 171,568 | 524,288 | Actual session copy threshold; historical GD66 source-copy high water remains 266,195 |
| Unknown member count | 2 | 64 | Measured preservation fixture versus inherited extension policy |
| Unknown value bytes | 27 | 131,072 | Measured preservation fixture versus inherited extension policy |

| Measured row | Complete bytes | Canonical records | Returned records | Saved corridor tiles |
|---|---:|---:|---:|---:|
| Cycle 1 constructed | 128,999 | 25 | 0 | 1 |
| Cycle 1 returned | 128,183 | 21 | 4 | 1 |
| Cycle 32 constructed | 152,871 | 149 | 124 | 1 |
| Cycle 32 returned | 152,056 | 145 | 128 | 1 |
| R3, active research | 204,461 | 165 | 128 | 1 |
| R3, completed research/objective | 204,416 | 165 | 128 | 1 |
| Historical 64-record refusal fixture | 131,983 | 64 | 40 | 1 |

This does **not** promise the Cartesian product of all independent ceilings. For example, adding the entire 131,072-byte unknown allowance to the largest measured current save exceeds the whole-save byte limit. Such combinations must refuse without mutation. Historical GD66 rows and their limits remain regression coverage alongside the new current-state rows.

### Refusal and persistence

Increasing only the record budget would expose another interaction within this workload: the previous complete-save session did not run the raw boot scanner on its emitted candidate. A 129th returned record could then be written under 165 canonical records yet refused on the next raw load. The closeout reuses `RawSavePayloadClassifier.Scanner` through a bounded validation entry point, called by `DetachedCanonicalSaveSession` before creating a replacement update. All six raw budgets now constrain complete-save candidates, with existing `gd66.payload.workload_exceeded` refusal. It changes neither the save contract nor migration behavior and performs no persistence itself.

The production boundary regression performs 32 real construction/content/deletion cycles, reconstructs, assigns one reusable content, and attempts deletion yielding custody record 129. Commit refuses before durable bytes, runtime/session publication, lifecycle advancement, or ownership changes. The content stays assigned and the prior 128 custody records remain intact after reopen. The historical 64/65 case remains explicit: ten four-content cycles followed by reconstruction and three placements reach 64; the fourth placement refuses at 65. No custody is discarded, merged, recycled, or reused to meet a limit.

The new record value provides the full existing array envelope with current geometry. Further lifetime growth still requires an explicit policy/content decision; this closeout does not provide an unbounded inventory. The extra raw scan is bounded by the existing profile and avoids cloning candidate bytes; on-device allocation/timing and filesystem qualification remain outside this Windows Editor measurement.

### Local validation and remaining checks

Unity 6000.3.2f1 executed the standard suites on 2026-09-09: **PlayMode 2,149 total / 2,139 passed / 0 failed / 10 skipped**, and **EditMode 224 / 224 passed / 0 failed / 0 skipped**. EditMode includes 55 production-content build-gate tests, 57 recovery tests, and 112 export tests; these are editor guards, not a player build. PlayMode skips comprise eight `gd66.test.synchronous_edit_mode_fixture` GameRoot cases, the non-Windows inverse case on this Windows host, and the Windows-player-only qualification case in Editor. The synchronous GameRoot cases are not discovered by this repository's standard EditMode assembly path and remain unexecuted; their behaviors require separate synchronous Editor validation. The installed framework's `runSynchronously` option only supports EditMode tests and does not change discovery. No test assembly restructuring was included. Production-content reconstruction/renovation, complete-save writing, durability/recovery, raw boundaries, and all 30 historical plus seven Phase 3 measurement rows executed successfully in the applicable suites.

Local XML/log artifacts are under `Logs/phase3-full-playmode.*` and `Logs/phase3-full-editmode.*`; they are ignored validation artifacts. The test invocation is the installed Unity executable with `-batchmode -nographics -projectPath C:\Dev\Dungeon-Lord -runTests -testPlatform PlayMode` (or `EditMode`) plus explicit `-testResults` and `-logFile` paths. The established test sources require `PlayerSettings.playModeTestRunnerEnabled`; it was enabled only for validation and restored afterward from a backup verified against HEAD. `runPlayModeTestAsEditModeTest` remained off for the successful runs. Earlier attempts produced a compile failure without NUnit references, a licensing interruption, a zero-test mode-converted run, and one mis-targeted cleanup-flush injection (138 passed / 1 failed); none is counted as passing validation. The corrected focused suite passed 160/160 before the full suites. After adding explicit terminal-position/accounting assertions, the final focused run passed **147/147**; after strengthening the surviving-corridor negative to use a fully valid production starting layout, the geometry suite passed **76/76**. Both final reruns had zero failures or skips. Their artifacts are `Logs/phase3-final-focused.*` and `Logs/phase3-final-production-geometry.*`.

On 2026-09-10, final persistence and boot-agreement assertions passed in **167/167 focused tests, zero failures or skips** (`Logs/phase3-closeout-final.xml` and `.log`). This includes 76 structural, 59 write-authority, 13 measurement, 11 profile, and eight session tests. The unrelated-corridor fixture starts from a reopened, production-validated two-room layout with an assigned monster and surviving corridor `(4,3)` through `(7,3)`, whose destination is not Completion. Rectangle construction in that gap refuses with corridor overlap; submitting the invalid preview also refuses. Disk/session bytes, runtime publication, and complete canonical ownership/topology/high-water snapshots remain unchanged, including after reopen. The custody-boundary test separately emits the 129-entry complete candidate under test-only measurement raw limits, without persisting it, and proves the production boot scanner refuses the same payload as the writer. Measurements reproduced the exact rows above. Only tests and documentation changed after the full suites, so those completed results remain applicable. After Unity exited, only the temporary runner flag and Unity's application-identifier ordering were restored to their exact baseline values; no ProjectSettings diff remains.

Manual checks remain: the eight synchronous GameRoot cases (including both reconstruction shapes and upstream Basic-to-Rectangle replacement), player-visible preview/reason/terminal placement and reopen smoke in Bootstrap, and Windows Standalone player/build qualification. No standalone build, player qualification, mobile qualification, or usability approval is claimed. Re-run `Gd66SaveWorkloadMeasurementTests` whenever changing content, history, lifecycle, serialization, or any interacting profile field. No production sizing measurement remains deferred for lack of Unity access; player-facing smoke and platform qualification are separate from these deterministic measurements.

The exact unexecuted synchronous cases in `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests` are:

- `BootstrapDeletionPresentationLocalizesReturnedRemovedAndAllBlockingContentWithoutRawIds`
- `BootstrapRenovationPresentationDisclosesLocalizedMovementReplacementAndCapacityConsequences`
- `StructuralConstructionThroughRealRootPersistsPublishesAndClearsPreview`
- `StructuralDeletionMissingRuntimePolicyFailsClosedThroughRealRoot`
- `StructuralDeletionThroughRealRootPersistsPublishesAndPresents(6,"north",DirectDoorway,"Direct Doorway","")`
- `StructuralDeletionThroughRealRootPersistsPublishesAndPresents(7,"east",PhysicalCorridor,"Straight Stone Corridor","(1,6)")`
- `StructuralReplacementThroughRealRootPersistsPublishesAndReopens(False)`
- `StructuralReplacementThroughRealRootPersistsPublishesAndReopens(True)`

The other two skips are `Gd66WindowsSpatialMigrationFileSystemTests.CurrentNonWindowsRuntimeFailsClosed` (`gd66.test.windows_only_inverse`, inapplicable on Windows) and `Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification` (`gd66.test.windows_player_only`, requires Windows Player). Skips are not executed passes. Manual player testing was not attempted during this closeout.

## Historical GD66 qualification

**Status (2026-08-21): OWNER APPROVED AND ACTIVE.** The Unity measurement harness executed all 30 rows. The final values below are the dedicated production-owned save-migration workload profile consumed by the live schema-7 load, migration, native-creation, validation, and write path. Their earlier approval did not by itself activate schema 7; PR #195 subsequently completed activation and required validation at `c4ba1f68985c18c2a6a62bcfd84c217e0cf07b06`. The values were not copied from or derived from `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`.

## Method and evidence boundary

The accounting implementations were inspected directly in `RawSavePayloadClassifier.Scanner`, `ContractJsonWorkloadBudget`, `CanonicalSpatialSaveContracts.TryCanonicalizeCore`, and `DetachedWholeSaveCandidateSerializer`. The committed harness covers wrapped schemas 1–6 and unwrapped v1, empty and populated migration preparation, R1/R2 semantic fixtures, content-only implicit containers, whole-save unknown root/primary preservation, descriptor/journal/receipt contracts, complete-save validation, repository-model lifecycle and research states, and the production compatibility profile and catalog.

Capacity sizing evidence is deliberately split. **Legacy-source runtime high water** comes from `run_simulation_config.json`: Basic rooms resolve through `MvpOrderedRoomRouteResolver` at 1 monster, 1 trap, and 1 loot node per room. **Canonical target high water** comes from the production DungeonSpatial `RoomSpatialDefinition` selected by the active compatibility geometry: Basic rooms allow 2 monsters, 2 traps, and 2 loot assignments per room, or 12 assignments across R2. The measurement harness’s canonical-target route copies those capacities from the loaded production definition and feeds all approved assignments to the unchanged `RunSimulationService`; the live canonical projection now also consumes the production-owned 2/2/2 values, while historical legacy-source sizing remains 1/1/1.

Both paths generate real `RunOutcomeRecord` instances through `RunSimulationService` using production run and loot assets, retain exactly the configured 10 outcomes through `RunHistoryState.AppendOutcome`, and serialize the containing `SaveData` with `JsonUtility`. The legacy row is schema-6 source-fixture evidence. Canonical-target rows remain sizing evidence; separate PR #195 runtime and integration validation establishes live canonical authority.

Every representative legacy-source row mirrors the legacy persistence representation without invoking filesystem behavior: it constructs `SaveRoot`, sets `schemaVersion = SaveMigration.LegacyCompatibilitySchemaVersion` (currently 6), assigns `primary`, and uses `JsonUtility.ToJson(root, true)` before UTF-8 encoding. The GD66 target authority remains schema 7. A focused regression compares the helper byte-for-byte with that equivalent pretty-printed expression and rejects compact serialization.

Before case-specific history or research state is added, the representative schema-6 base is passed through `SaveMigration.MigrateToLatest`. The fixture asserts preservation of the populated 5 × 6 economic layout and intended room-slot authority, and asserts the migration-owned `mvpDungeonPlacements`, `mvpDungeonFloorLayout`, `mvpRoomSlotAssignments`, `structureRuntime`, `runHistory`, and `completedObjectives` shells and their required collections are present.

The current production contract proves: schemas 1–6 migrate to 7; the active geometry is R1/R2; R2 has two rooms, two fixed endpoints, four route nodes, and three direct edges; Basic rooms each permit two monster, two trap, and two loot assignments; and production run history retains 10 outcomes. Thus the modeled maximum current spatial state has 12 content assignments and **26 canonical records** (`1 floor + 2 rooms + 4 nodes + 3 edges + 2 fixed + 12 assignments + 2 semantics`). Direct-doorway R1/R2 edges carry no corridor footprint tiles, so the current migration model requires zero saved edge-footprint tiles; the approved tile limit deliberately allows future valid serialized footprints without adopting Phase 3.

Source-reconstructable exact raw fixtures measured without Unity were: empty wrapped v6 **53 bytes, depth 2, 3 object members**; unwrapped v1 **31 bytes, depth 1, 2 members**; and the whole-save unknown-root/primary preservation fixture **161 bytes, depth 4, 5 members in one object, 2 array elements, 13 raw bytes in its longest classifier string token (member name `schemaVersion`); its longest string value remains 9 bytes, 13 parsed values, 12 object/array records, and 91 decoded UTF-16 name/value units under the report’s independent JSON walk (not the strict contract record counter)**. The production profile is **7,168 bytes**, the production catalog **8,947 bytes**, canonical legacy gameplay configuration **11,080 source JSON bytes**, and the largest repository JSON asset is Bootstrap English at **89,802 bytes**; these data assets are comparison evidence only, not save-limit inputs.

The Unity harness executed all 30 retained rows. The measured representative high waters include **266,907 raw bytes**, **266,195 copied-source value bytes**, **141,504 candidate bytes**, **4,552 parsed nodes**, **1,610 collection records**, **117,146 decoded UTF-16 string characters**, **26 canonical spatial records**, and **0 canonical footprint tiles**. The approved limits retain explicit bounded headroom over those repository-owned fixtures.

The prominent whole-save rows are `canonical-target-full-save-high-water-active-research` and `canonical-target-full-save-high-water-completed-research-objective`. Both contain canonical-target maximum-content R2, ten service-generated outcomes, the normal persisted 5 × 6 economic layout with all 30 slots populated by the three existing allowed structure IDs, populated structure runtime, deterministic realistic-width counters/timestamps, and offline summary. The first adds coherent active pending+progress research from production Bootstrap IDs/rules; the second adds completed research and the completed first-session objective. They remain separate because completing the only current research project clears pending/progress, and re-pending that already completed project would be an invalid synthetic union. Pending-only, active-progress, completion-pending, and completed-research states also remain separate lifecycle rows. Each active progress row is checked through the repository progress/eligibility or claim-readiness resolver before serialization.

**Activated capacity boundary:** canonical runtime room capacity comes from the validated canonical production spatial content authority. `CanonicalMvpRouteProjection` does not source canonical room capacity from the legacy `RunSimulationConfig.MvpRoomSlotCapacities` table. Basic Room canonical placement is additive up to the production-owned 2/2/2 limits; duplicate placement is rejected without mutation. Native R1→R2 structural construction remains outside this sizing packet and deferred to Phase 3.

## Seventeen-field approved profile

| Field | Exact consumer/accounting | Largest proven or modeled current-MVP requirement | Approved value | Absolute headroom / multiplier | Why and boundary behavior |
|---|---|---:|---:|---:|---|
| `MaximumRawSaveBytes` | `RawSavePayloadClassifier`; exact active payload byte length before cloning/parsing | No archived maximum save; modeled full state is required to remain below 128 KiB | **524288 bytes** | 257381 over measured / 1.96× | Caps clone and raw tree input; a bloated or appended save stops before deserialization. Test exact 524288 and 524289 bytes with otherwise valid padded unknown evidence. |
| `MaximumRawNestingDepth` | scanner stack; simultaneously open object/array containers | depth 4 exact preservation fixture; modeled full save contracts remain below 16 | **32** | 16 / 2× modeled | Supports current nested outcomes/summaries while stopping stack/deep-container attacks. Test 32 accepted and 33 rejected with valid nested unknown arrays. |
| `MaximumRawObjectMembers` | scanner `Frame.Names.Count`; independently per object | 21 recognized legacy primary names; schema-7 adds 2 owners; modeled primary ≤23 | **64** | 41 / 2.78× | Allows unknown extension members without permitting huge hash sets. Test a valid primary with 64 unique members and 65 rejection. |
| `MaximumRawArrayElements` | scanner elements; independently per array | 12 canonical-target assignments; 10 run outcomes; legacy runtime allows 6 contents across R2 | **128** | 116 / 10.67× assignments | Allows extension arrays and current history, blocks extremely broad arrays. Exact/plus-one array fixture required. |
| `MaximumRawStringBytes` | scanner bytes between quotes, including escape bytes, before UTF-8 decoding | longest exact fixture token 9 bytes; stable hashes 64 bytes; current identifiers are far smaller than 1 KiB | **4096 bytes** | 4032 / 64× hash | Accommodates extension strings/local notes without allowing one token to dominate memory. Test 4096 raw ASCII/escapes and 4097; separately test multibyte UTF-8. |
| `MaximumRawScanWork` | every consumed lexical byte plus delimiter checks and failed alternatives | exact value must be emitted by scanner instrumentation; valid JSON work is greater than bytes | **1048576 charges** | policy ceiling; measured multiplier pending | Independent provisional cap, not derived from raw bytes. The Unity harness binary-searches the real scanner success threshold for every retained raw fixture; approval requires the measured worst case and exact/+1 synthetic boundary. |
| `MaximumSerializedInputBytes` | strict parser/writer for canonical members, complete save, descriptor, journal, receipts/intents | no archived maximum emitted candidate; comparison inputs ≤11,080 bytes; modeled complete save <128 KiB | **262144 bytes** | ≥131072 / 2× modeled | One strict contract cannot allocate/emit beyond 256 KiB. Test each largest contract at its measured minimum and a padded valid complete save at limit/+1. |
| `MaximumSerializedParsedNodes` | `ContractJsonWorkloadBudget.TryNode`; every JSON value node | exact preservation fixture 13; modeled maximum-history/R2 save expected well below 2048 | **8192 nodes** | ≥6144 / ≥4× modeled | Separately bounds parse object graph even for tiny tokens. Binary-search boundary tests for complete save, canonical members, descriptor and journal plus synthetic 8192/8193-node arrays. |
| `MaximumSerializedCollectionRecords` | explicit `Record()` calls for contract-owned array/object records, not all nodes | canonical modeled records 26; descriptor/journal record counts are smaller | **4096 records** | 2486 over measured / 2.54× | Allows preserved independent state and future compatible records without equating records to nodes. Test contract-specific exact minima and 4096/4097 record fixtures. |
| `MaximumSerializedStringCharacters` | cumulative decoded property names and string values in UTF-16 units | exact preservation fixture 91; hashes/IDs dominate strict sidecars; no archived maximum full save | **262144 UTF-16 units** | 144998 over measured / 2.24× | Independent from raw UTF-8 bytes, including surrogate-pair behavior. Test 262144 and 262145 units and non-BMP strings. |
| `MaximumSerializedDiagnostics` | `SpatialIssueCollector`; retained issues before final slot becomes `WorkloadExceeded` | valid inputs produce 0; contracts can report multiple structural issues | **64 issues** | 64 over valid / bounded ~256-byte enum payload plus list overhead | Gives actionable deterministic diagnostics without unbounded error accumulation. Test 64 distinct issues and 65th replacement/exhaustion semantics. |
| `MaximumCanonicalSpatialRecords` | canonicalizer sum of floors, rooms, nodes, edges, fixed structures, assignments, semantics | **26** modeled maximum supported R2 content combination | **64 records** | 38 / 2.46× | Compatibility/lossless headroom for the approved MVP only; it does not authorize more rooms or Phase-3 topology. Test canonical 64 and 65 record structurally valid synthetic states, plus production R1/R2. |
| `MaximumCanonicalMaterializedTiles` | sum of saved `edge.Footprint.OccupiedTiles` only | **0** for current direct-doorway R1/R2; production layout occupied totals 26/42 are different accounting | **64 tiles** | 64 over current | Lossless compatibility allowance for pre-existing serialized footprints only; never derived from content validation tiles and not authority for corridor construction. Test 64/65 unique footprint entries plus R1/R2 zero-footprint regression. |
| `MaximumWholeSaveCandidateBytes` | `BoundedOutput`; entire emitted schema-7 root | no archived maximum candidate; must include copied primary, unknowns, canonical owners and envelope | **262144 bytes** | policy ceiling; measured expansion pending | Must not exceed the strict complete-save input ceiling because `BuildPrepared` immediately parses the whole candidate. Exact/+1 construction must keep every other applicable budget permissive. |
| `MaximumCopiedSourceValueBytes` | cumulative raw bytes of recognized legacy primary values copied losslessly | strictly less than raw bytes because envelope, names, colons, and commas are not copied values | **524288 bytes** | 258093 over measured / 1.97× | Its full-profile success boundary is unreachable. It remains an independent defense-in-depth guard for future profiles/composed contexts and produces a stable workload failure if configured lower; approval must use measured representative `copiedBytes` before deciding whether to lower it. Do not claim an exact 262144-byte full-pipeline success test. |
| `MaximumUnknownMembers` | combined unknown root plus primary member count | exact preservation fixture has 3 (`rootBefore`, `unknown`, `rootAfter`) | **64 members** | 61 / 21.33× | Allows forward-compatible evidence but bounds lists/names; independent from bytes. Test split root/primary totals of 64 and 65. |
| `MaximumUnknownMemberBytes` | cumulative raw JSON value bytes for unknown root plus primary members | exact preservation fixture values total 22 bytes (`[1,{"x":true}]`, `1.00`, `false`) | **131072 bytes** | 131050 / 5957× exact fixture | Allows meaningful future extension evidence while bounding retained raw slices. Test one and many members totaling exactly 131072 and 131073 bytes. |

## Required cross-field configuration invariants

1. `MaximumWholeSaveCandidateBytes <= MaximumSerializedInputBytes` is mandatory. `BuildPrepared` immediately passes the complete candidate to `DetachedCompleteSaveContract.ParseValidateAndRoundTrip`, so any larger advertised candidate-success boundary is unreachable.
2. A production-valid candidate must independently satisfy serialized input bytes, parsed nodes, collection records, decoded string characters, diagnostics, canonical records, and canonical footprint tiles. Configuration validation must reject a profile whose advertised whole-candidate boundary cannot be exercised under the other strict ceilings.
3. Candidate construction needs expansion allowance for the schema-7 envelope, member names, `canonicalSpatialAuthority`, and `spatialFloors`. This is not expressible as a fixed equality: copied recognized values and unknown values are both subsets of the raw payload, while canonical bytes vary by R1/R2/content. Approval must use the harness’s measured `candidateBytes - rawBytes`, `candidateBytes - copiedBytes - unknownBytes`, and maximum-content candidate results.
4. `MaximumCopiedSourceValueBytes` and `MaximumUnknownMemberBytes` must each be no greater than the raw payload ceiling for reachable inputs, but their sum need not be accepted for every raw payload because envelope/name bytes also consume that ceiling. With the approved equal raw/copied profile, the copied ceiling is intentionally nonbinding and its exact boundary is unreachable in the full pipeline; it remains separately configurable rather than being removed or inferred. A raw-valid payload that exceeds a candidate/copy/unknown workload budget must fail with the stable `gd66.payload.workload_exceeded`, never incidental `gd66.transaction.candidate_invalid`.
5. Cross-field checks validate coherence only. They do not derive a missing field, provide fallback values, or erase the fields’ distinct accounting semantics.

## Independence and memory implications

These values intentionally do not derive from one another. Raw scan work counts parser operations, not bytes. Raw string bytes include JSON escapes and UTF-8 bytes, while serialized string accounting uses decoded UTF-16 units. Parsed nodes count all values; collection records count only explicit contract records. Canonical records use the spatial domain sum above; canonical tiles count saved edge-footprint entries only. Candidate bytes include envelope, member names, canonical owners, and unknowns; copied-source bytes count recognized raw values only. Unknown count and unknown bytes constrain different attacks.

At the approved ceilings, the largest mandatory byte buffers are approximately 512 KiB raw input, its 512 KiB owned clone, and a 256 KiB candidate output (about 1.25 MiB of byte payload at peak before parser objects and temporary strings). `BoundedOutput` currently uses `List<byte>`, so transient backing-array growth may approach another candidate-sized allocation. An 8,192-node object graph and 4,096 records can add several MiB depending on Mono/IL2CPP object overhead; this remains subject to platform profiling. The profile is “mobile-conscious,” not mobile-qualified; GD66 durability support remains Windows-only.

## Measured requirements versus policy decisions

Repository-proven/model-derived requirements currently exist for raw structural depth/member shapes, the 10-entry history policy, the legacy runtime R2 1/1/1 capacity, the canonical-target R2 2/2/2 capacity and 26-record model, the persisted 5 × 6 economic layout, and zero saved direct-edge footprint tiles. The committed Unity harness will replace source-only measurements with the exact consumer thresholds for every retained fixture.

The executed harness establishes the measured high waters above. Capacity beyond those rows remains deliberate owner-approved policy headroom rather than a claim that the current MVP requires every boundary value. The 64-tile allowance is only lossless-compatibility policy; it is not current-MVP geometry demand and does not authorize Phase 3. Large unknown-member headroom is forward-compatible evidence policy, not current gameplay demand.

## Approved production profile

```json
{
  "Schema": "save_spatial_migration_limits",
  "SchemaVersion": 1,
  "MaximumRawSaveBytes": 524288,
  "MaximumRawNestingDepth": 32,
  "MaximumRawObjectMembers": 64,
  "MaximumRawArrayElements": 128,
  "MaximumRawStringBytes": 4096,
  "MaximumRawScanWork": 1048576,
  "MaximumSerializedInputBytes": 262144,
  "MaximumSerializedParsedNodes": 8192,
  "MaximumSerializedCollectionRecords": 4096,
  "MaximumSerializedStringCharacters": 262144,
  "MaximumSerializedDiagnostics": 64,
  "MaximumCanonicalSpatialRecords": 64,
  "MaximumCanonicalMaterializedTiles": 64,
  "MaximumWholeSaveCandidateBytes": 262144,
  "MaximumCopiedSourceValueBytes": 524288,
  "MaximumUnknownMembers": 64,
  "MaximumUnknownMemberBytes": 131072
}
```

**OWNER APPROVED.** The canonical production copy is `Assets/_Project/Data/Production/Save/save_spatial_migration_limits.json`; runtime consumers must use its strict loader and must not fall back to Bootstrap or DungeonSpatial content-validation limits.

## Approval and boundary-test gate

The committed `Gd66SaveWorkloadMeasurementTests` harness uses normalized schema-6 bases and the pretty-printed `SaveRoot` representation written by legacy `SaveService`; it emits exact counters (by binary-searching the production consumers’ acceptance boundaries) for: wrapped schemas 1–6; unwrapped v1; migrated empty, R1, canonical-target maximum-content R2, and content-only implicit-container candidates; a current legacy-runtime 1/1/1 R2 ten-result history resolved through `MvpOrderedRoomRouteResolver`; a test-only canonical-target 2/2/2 R2 ten-result history whose capacity is loaded from production DungeonSpatial content; 5 × 6 fully populated economic layouts and structure runtime; separate resolver-validated pending, active-progress, completion-pending, and completed research/objective lifecycle saves; two canonical-target full-save high-water rows covering the mutually exclusive active-research and completed-research/objective coexistence states; unknown root/primary evidence; a test-only empty native-canonical complete candidate selected from the active schema-7 contract and starter profile; descriptor; every journal stage; receipt; restoration intent; and complete-save round trips. The native measurement is an isolated strict-contract candidate, not live native creation architecture. Persist the measured table as assertions or reviewed evidence, not runtime defaults.

Every approved field needs an exact-limit success and limit-plus-one fail-closed test where a structurally valid fixture can reach the boundary. Diagnostic exhaustion instead asserts the stable `WorkloadExceeded` replacement rule. Raw scan work must expose a test-only counter because input bytes cannot predict delimiter charges. Canonical tile tests must synthesize valid saved edge footprints without changing production R1/R2 or enabling corridor gameplay. Whole-candidate/copied/unknown tests must vary each budget independently and prove prior bytes remain unchanged on failure.

The committed harness must remain regression evidence for the approved profile. Future increases require new measured fixtures and owner approval; the approved values are not permission to broaden gameplay topology or Phase 3 scope.
