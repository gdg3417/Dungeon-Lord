# Phase 6A2 — Floor 2 construction and inactive editing

Status: merged as PR #216 at `45ac7da34bb4527b9b45fcaeb9ec5ec01d816e7f`. External review, automated qualification, owner Editor qualification, and Windows standalone qualification passed.

## Merge closeout

- PR: #216, **Phase 6A2: Construct Floor 2 and enable floor-targeted inactive editing**.
- Merge commit: `45ac7da34bb4527b9b45fcaeb9ec5ec01d816e7f`.
- External review and correction re-review passed before merge.
- The automated suites and production gates recorded below passed.
- Owner Editor qualification and Windows standalone qualification recorded below passed.
- The known local-only `ProjectSettings/UnityConnectSettings.asset` `m_Enabled: 0 -> 1` environment change was not part of PR #216.

## Baseline and continuation audit

- Exact verified starting baseline: `87245ed2eb68d20b6559631f6e9dba3fb5f715fc`, merged PR #215, **Phase 6A1: Persist schema 11 floor activation authority**.
- Branch: `codex/phase-6a2-floor2-construction-inactive-editing`.
- This continued the interrupted local implementation, not a fresh checkout. At continuation, HEAD was the exact baseline, with nine modified files and four untracked implementation/configuration files. Those changes were inspected and retained. No reset, checkout, stash, or alternate implementation was used.
- The original run verified clean main and refreshed origin before creating the branch. Continuation compared HEAD, origin/main, all tracked diffs, and new files against the exact baseline; there were no already-committed Phase 6A2 changes to overlook.
- Read repository guardrails, model policy, Phase 6 lock/execution plan, Specs 28/38, invariant glossary, actual merged #215 implementation and evidence, and production authoring/export, research, investment, detached persistence, editing, run, passive-mana, and Bootstrap authorities.
- Initial audit: profile/content, lifecycle-aware validation, construction writer, ledger extension, and initial SaveService/GameRoot hooks were partial. Explicit floor targeting, empty-shell room editing, run isolation, usable Bootstrap controls, localization, production export, and Phase 6A2 tests/evidence were incomplete or untested. The continuation finished and reconciled those existing paths.
- Recommendation: **GPT-6 Sol, High; Complex**. The owner explicitly changed the optimization goal toward usage conservation, with locked architecture and recent consumption evidence justifying the exception to the older Complex default. No model-governance change is included.

## External-review corrections

External review of PR #216 at `e85e7edc1277bfcb5f2e01f99ff6ecbdb0d2ecd2` found two bounded authority defects. The correction used the requested **GPT-5.6 Sol, Medium; Standard** configuration and changed no Phase 6 architecture.

1. The selected production Dungeon Spatial content version was stale at `0.1.0`. Phase 6A2 adds compatible Floor 2 catalog and allowlist records, so the already-approved semantic version policy requires `0.2.0`. The canonical authoring manifest and strict parser selection now require `0.2.0`; canonical export updates only catalog/manifest content-version fields. Catalog schema remains 1, string-table schema remains 1, save schema remains 11, and no migration or stable-ID change is added.
2. Floor construction permission incorrectly required exactly one raw `ac_100` entry. It now consumes the existing `CompletedResearchStateResolver` duplicate-safe, ordinal, read-only membership semantics: at least one valid `ac_100` grants one permission. Duplicate production node/effect configuration remains invalid, repeated completed IDs do not mutate save state, do not construct automatically, and cannot construct a second Floor 2.

## Owner qualification

Owner qualification passed after implementation external re-review. A temporary local-only QA helper persisted `ac_100` solely for UAT; it was never committed or pushed, was removed before PR qualification, and `ac_100` remained persisted after its removal and a clean PR-code reload. It did not construct a floor or grant mana.

Editor qualification passed all prescribed steps 1–20: Floor 2 was Locked without `ac_100`, construction was blocked while Locked, then the persisted permit showed the Unlocked/unconstructed state. Insufficient-mana presentation and gating passed. QA mana was filled to 1000; construction charged exactly 450 mana, leaving 550 before later ordinary changes, created one Constructed Inactive Floor 2, and repeat construction neither created a second floor nor charged again. Passive mana remained based on one Active floor; save/close/reopen preserved the shell and Inactive state.

Explicit Floor 2 selection passed. A first Basic Room at `(1,2)`, `Zero` orientation, and east completion connection passed, as did representative monster, trap, and loot placement, unassignment/redeployment, and Floor 1/Floor 2 edit isolation. Normal runs remained Floor 1-only; edited Floor 2 state survived reopen. No Activate/Deactivate control was exposed, and no blocking localization, clipping, raw player-facing ID, or Console-error issue was reported.

Windows 64-bit Development Build qualification passed. The standalone loaded the durable two-floor state with Floor 1 Active and Floor 2 Constructed Inactive; Floor 2 rooms/content persisted, switching floors worked, and Floor 1 remained intact. Run/Observe remained Floor 1-only: no traversal, descend/exit choice, survivor transfer, or other deferred multi-floor behavior appeared, and Floor 2 remained unchanged after a Floor 1 run. Full close/relaunch preserved both floors and Floor 2 edits; no repeated shell charge or duplicate Floor 2 occurred, and no blocking standalone UI, localization, crash, or persistence issue was reported. Qualification was performed remotely through Moonlight; this records the Unity/Game view and standalone behavior tested, not phone-streaming display scaling as native-resolution qualification.

Post-build worktree note: Windows qualification materialized the repository’s documented URP prefilter/runtime and Standalone batching diffs; those generated files were inspected and restored to HEAD. TMP fallback serialization noise was likewise restored. The known local-only `ProjectSettings/UnityConnectSettings.asset` `m_Enabled: 0 -> 1` environment setting remains intentionally outside this PR and was not staged.

## New implementation and retained authorities

### Production floor and dedicated construction profile

Canonical authoring tables add `spatial.floor.02`, index **1** (player-facing Floor 2), minimum `(0,0)`, bounds **14 × 14**, base `FinalFloorSpaceCapacity` **80**, and authored `OptionalBranchAllowance` **1**. Existing `ac_300` still owns effective branch permission/allowance.

Allowlisted rooms are `spatial.room.basic`, `spatial.room.large_chamber`, and `spatial.room.rectangle`; the corridor is `spatial.corridor.straight_stone`. Fixed definitions remain `spatial.fixed.entrance_hall` and `spatial.fixed.completion_terminal`. There are no new exclusive encounters, rooms, corridors, monsters, traps, or loot, and no floor-number difficulty bonus.

Dedicated Resource configuration `floor_construction_profiles.json` owns exactly these shell-operation values:

| Field | Authored value |
| --- | --- |
| Profile identity/version | `floor.construction.02.v1` / `1` |
| Production floor | `spatial.floor.02` |
| Required research | `ac_100` |
| Shell mana cost | `450` |
| Entrance anchor/orientation | `(1,0)` / `CardinalOrientation.Zero` (serialized enum `0`) |
| Completion anchor/orientation | `(11,12)` / `CardinalOrientation.Zero` (serialized enum `0`) |

Both footprints fit, do not overlap, use allowed orientations, and preserve useful space. With the authored north-facing Entrance socket, a Basic Room at `(1,2)`, Zero orientation and east terminal connection is one deterministically tested first-room placement. The existing socket/door/corridor algorithm can move the same Completion Terminal when building; the shell profile is not reapplied during later editing or reopening. Deleting the final room leaves the current legal fixed-structure layout, not a reset to original profile coordinates.

The profile does not duplicate bounds, capacities, or allowlists and is neither a migration compatibility profile nor a canonical starter. Strict bounded JSON/type/property validation, unique profile/floor resolution, production definition checks, orientation/bounds/footprint checks, and no-overlap validation fail closed.

The existing CSV exporter generated the committed catalog. The canonical authoring release advances from `0.1.0` to **`0.2.0`** for the additive compatible Floor 2 definition and allowlist records. The authoring parser remains strict and rejects unsupported selections; external review identified and corrected its stale `0.1.0` pin. Generated `dungeon_spatial_content.json` and `content_manifest.json` now report `0.2.0`; generated `string_table_en.json` remains byte-identical. Content schema/registration remain 1. Generated JSON was not used as writable authority.

### Research permission

`FloorConstructionResearchAuthority` resolves exactly one production Architecture `ac_100` node with `unlock_type=effect`, target `max_floors`, and effect profile `eff_ac_floor_2_permit`, then exactly one matching `max_floors_set` effect with integer unit/value. Relevant raw property types and duplicate properties/records are checked before deserialization; malformed, contradictory, missing, or duplicated configuration fails closed. The resolved maximum floor count must agree with the profile's production index. Permission uses the established completed-research authority's ordinal, duplicate-safe membership query: one or more valid `ac_100` entries mean completed, while null, blank, or nonmatching state remains locked. Resolution does not normalize, reorder, deduplicate, or otherwise mutate the saved state.

Completion creates no floor. Reopening a completed-research save with no Floor 2 remains unlocked/unconstructed. No research-completion lifecycle or completion writer is added. The existing player research screen remains the scaffold project, not a production Architecture progression interface; qualification of the permit uses an approved completed-state fixture/existing save. This limitation is not disguised as a new research screen.

### Identity, schema, investment, and transaction

Schema remains **11**. No save field, parallel investment collection, migration, wallet, activation authority, or writer is added. Frozen schemas 7–10 and 10→11 behavior remain unchanged; no migration manufactures Floor 2 or a Floor 1 shell investment. Unknown complete-save extension preservation remains owned by the existing lossless session pipeline.

Native additional-floor identities use one shared deterministic convention: `canonical.floor.` plus zero-based production index formatted `D2`. Floor 2 is `canonical.floor.01`; fixed IDs end `.fixed.entrance` / `.fixed.completion`, and nodes end `.node.entrance` / `.node.completion`. Construction collision-checks proposed persistent identities. Room/edge allocation continues using each floor's existing monotonic lifecycle, initially room ordinal `0`, edge ordinal `0` for the empty shell. Returned content and assignment sequencing retain existing custody/allocation authorities.

The existing `StructuralInvestmentRecord` owner now recognizes a shell record for native constructed additional-floor identities: `canonical.floor.01.shell`, `ConstructionMana=450`, other investment components initially zero. Historical compatibility identities/frozen records retain their room/edge-only shape and no Floor 1 shell record is fabricated. Normal structural changes copy this stable record unchanged; refunds still apply only to the selected removable rooms/edges. No shell refund or whole-floor demolition action exists. The record concept extends to later native additional-floor identities without implementing additional floors now.

Construction uses a focused `DetachedCanonicalWriteAuthority.ConstructFloor` request through SaveService. It validates the owned current complete session, exact active-file bytes, canonical baseline fingerprint, permission, single configured production/profile target, absence of Floor 2, valid affordable wallet, and collision-safe identities. It refreshes the preview and compares all profile values/version, then prepares a detached Inactive floor with exactly two fixed structures, two route nodes, empty room/edge/content collections, and its per-floor lifecycle. Exactly the configured cost is subtracted from the existing mana reserve and recorded through the existing investment owner.

The retained complete-save path validates/canonicalizes the candidate, validates reopening those exact candidate bytes, prepares a detached runtime projection, and invokes the existing exact atomic persistence/readback/rollback authority. Only successful exact durable equality releases the already-validated reopened session and runtime projection for live publication. This retains established preflight/reopen validation plus exact durable-byte equivalence; it does not introduce an unprotected fallible post-install read or a second save writer. Tested write, flush, replace, readback, stale-preview/session, malformed state/configuration, insufficient-funds, and repeat-construction failures publish nothing. Existing transaction recovery behavior is not replaced.

### Lifecycle-aware validity and explicit editing targets

`FloorLayoutValidationMode` makes the distinction explicit. Inactive construction mode tolerates a missing complete Entrance→Completion required route; Active mode retains the existing full route requirements. Both retain fixed-structure cardinality/definition/geometry, bounds, overlaps, capacity, room/corridor definitions, graph reference/connection/reachability checks, same-floor ownership, content capacities, lifecycle, investment, stable IDs, canonical ordering, and configured workload limits. Invalid mode values do not weaken full graph validation. No activation command or eligibility writer is added.

The shared `CanonicalEditFloorTarget` resolves an explicit stable floor ID; omission is compatible only when exactly one persisted floor is unambiguous. Structural construction/movement/replacement/deletion and room placement/redeployment/unassignment now validate the target floor and room ownership. Optional-branch/corridor requests retain their existing explicit stable-ID pattern. Ambiguous, duplicate, absent, stale, and mismatched targets fail closed without array/UI-index authority. Canonicalization happens before validation/publication, and floor sorting uses index then stable identity.

The first room on an empty Inactive shell uses the Entrance's authored footprint/socket as an adapter to the existing room connection solver. Existing corridor geometry, capacity, identity allocation, mana prices, and validation remain authoritative. Subsequent construction/movement/replacement/deletion uses the same paths as Floor 1. Deliberate last-room deletion returns a legal Inactive shell, preserves fixed IDs/current positions and lifecycle high-water marks, resolves contents under existing removal policies, retains shell investment, and reports correct configured capacity. Active Floor 1 minimum-room deletion rules are unchanged.

### Runs, mana, and Bootstrap

Current runs select the unique Active floor at canonical index 0, by identity/index, with all deeper persisted floors Inactive. A second Active floor is rejected, not silently ignored. Phase 5B fork projection and GameRoot branch gating exclude Inactive floor branches/content. An equivalent-input durable run-history comparison verifies Floor 1 output is identical with edited/inhabited Inactive Floor 2. The comparison normalizes the existing wallet through its established QA writer because real shell/edit spending necessarily changes wallet input; it does not hide or refund that spending in production.

Phase 6A1 active-only online/offline passive mana authority and tuning are unchanged. Constructing, editing, and reopening Floor 2 retains one Active floor.

The temporary Bootstrap action scroll panel adds localized Locked, Unlocked/unconstructed, Constructed Inactive, and Active presentation, configured price/affordability, eligibility refresh, construction, and stable-ID existing-floor selection. All structural/content/branch/corridor controls target the selected floor. Editing availability no longer depends on successful one-floor run projection. A read-only selected-room adapter feeds existing localized capacity/fit presenters; it is not a writable layout owner. Twenty-five new English-table keys cover statuses/actions/costs/results/reasons; branch display uses ordinals instead of raw persistent IDs. No Activate/Deactivate control, production editor framework, scene, prefab, or UI package is introduced. Unity UI/IMGUI skills guided reuse of the existing debug surface rather than a new editor architecture.

## Automated qualification

Final NUnit XML proves the following results. All three CLI test commands completed with exit code 0; no inconclusive results or unexpected skips occurred.

| Suite | Total | Passed | Failed | Skipped | Inconclusive |
| --- | ---: | ---: | ---: | ---: | ---: |
| Correction focused EditMode | 271 | 271 | 0 | 0 | 0 |
| Correction complete EditMode | 1180 | 1180 | 0 | 0 | 0 |
| Correction complete PlayMode | 2679 | 2669 | 0 | 10 | 0 |

The correction-focused expression includes the Phase 6A2 Editor bridge, completed-research resolver, production exporter/authoring, inherited production loading bridge, and build gate. The Phase 6A2 fixture now contributes **63** cases. All **59** production build-gate cases pass, including `ProductionGate_ValidInstalledSetAndExactBootstrapAssignmentsPass` and `ProductionGate_SuccessPreservesRequiredFilesByteForByte`.

Local XML SHA-256:

- `TestResults/phase6a2-correction-focused.xml`: `3FE26579A022F482D80E42A19FCB200AAA9A8D2395E6F729334EA777610B122C`
- `TestResults/phase6a2-correction-full-editmode.xml`: `862522D7B3FDBCB1B46443DCB81C57513EBE3AB4028C0A623472B47FC76FE53F`
- `TestResults/phase6a2-correction-full-playmode.xml`: `FE0FF17AFA941516F34981D5706EB48939EA9F33690B2039633CD59A1CB162A0`

Two final canonical export repetitions completed with exit code 0 and preserved all three output hashes exactly:

- Generated catalog: `5909423440D0923DB58C5F2433807F9A3E943CBEEDC51C220BAFE9950610BB4B`
- Generated English: `37ECE030181BB51FF887A75AA41A2293BDECE9EED8D292EF6EC40E48BEEF3262`
- Generated manifest: `D50B6BEC4E378AC486D813A3C01C7CDC2A56C138EDDF50E2EAE224C8C758D227`

`git diff --check` passes. Bootstrap English JSON contains **939** entries, zero duplicate keys (25 added to the baseline 914). No frozen schema/descriptor/migration/compatibility-profile file changed. Unity's incidental `applicationIdentifier` key reordering was restored; there is no intended ProjectSettings, scene, prefab, or generated test-scene change. Generated outputs are deterministic; no unexpected byte changes remain.

Focused command (actual EditMode bridge classes included):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --filter 'PhaseSixA2FloorConstruction|CompletedResearchStateResolver|ProductionSpatialContentExportTests|PhaseFourContent|ProductionSpatialContentBuildGateTests' --output 'TestResults/phase6a2-correction-focused.xml' --timeout 1200 --no-color
```

Complete commands:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --output 'TestResults/phase6a2-correction-full-editmode.xml' --timeout 1200 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode PlayMode --output 'TestResults/phase6a2-correction-full-playmode.xml' --timeout 1200 --no-color
```

Deterministic production export command:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' run 'C:/Dev/Dungeon-Lord' --timeout 600 --no-color -- -executeMethod DungeonBuilder.M0.Editor.DungeonSpatial.ProductionSpatialContentExportCommand.ExportProductionSpatialContentCommandLine
```

New `PhaseSixA2FloorConstructionTests` plus its Editor bridge cover production/profile/research validation, empty shell, stable identity, exact spend/investment, deterministic previews, refusal/failure atomicity, lifecycle-aware geometry/reference rejection, first/subsequent room construction, movement/replacement/deletion/rebuild, per-floor ordinals, monster/trap/loot acquisition/unassignment/cross-floor custody/redeployment, optional branch/corridor content/removal, wrong/omitted targets, exact save/reopen, active-only mana, Floor 1 run isolation, localization/selection/no activation control, and configured record/investment workload boundaries.

Retained complete suites cover frozen schema/migration/extensions, Windows persistence/recovery, structural economy, acquisition/custody, branches, deterministic runs, mana, localization, Bootstrap layout/pagination, content pipeline/build gate, and workload measurements. Production-dependent old fixture counts are updated for one additional floor/four allowlist records, retaining exact-boundary/one-over assertions and explicit original Floor 1 expectations.

Failures encountered and corrected during qualification:

- Sandbox Unity licensing/config access required an approved unrestricted CLI run; the blocked launch supplied no test verdict.
- Compile corrections included duplicate pattern variable/out-parameter lambda capture and an incorrect test option/helper name; no incomplete launch was counted as a pass.
- Early focused runs exposed one-floor UI compatibility/reason regressions and changed production record counts; those were corrected while retaining one-floor compatibility and exact workload boundaries.
- An inactive corridor-removal test initially attempted deletion while assigned content remained. Production correctly blocked it; the test now performs explicit unassignment first and preserves that safety rule.
- The first complete EditMode run was **1170 total / 1156 passed / 14 failed / 0 skipped**. Production-dependent test fixtures using catalog `.Single()` now explicitly select Floor 1; exact loader nested budgets increase by four records. Fresh native QA wallet availability is separated from floor-local editing so a valid empty canonical save still supports the existing wallet controls and reason presentation.
- Final review restricted construction-mode selection to **explicitly Inactive** floors through the shared mode resolver. Historical activation-less records therefore retain full graph checks; a focused regression exercises the incomplete-shell historical case. All final suites were rerun after this correction.
- The first historical regression run placed the test mutation before its valid-shell baseline assertion. The mutation was moved to the invalid-state switch; final 57-case Phase 6A2 and complete-suite XML prove both baseline acceptance and historical/full-route rejection. No production check was weakened to pass it.
- The staged whitespace check caught three trailing spaces in Unity-generated empty importer fields of the new profile `.meta`; they were removed without changing metadata values. Both staged and working-tree `git diff --check` then passed. Remaining Git LF→CRLF notices describe the repository's line-ending conversion, not whitespace errors.

Generated test XML remains local under `TestResults/`, now ignored like other generated Unity outputs; evidence records exact paths/results/hashes rather than committing generated test noise.

Expected PlayMode skips remain the baseline ten, with no additional skip introduced:

- Eight synchronous EditMode-only `Gd66GameRootBootIntegrationTests` cases (`gd66.test.synchronous_edit_mode_fixture`): localized deletion presentation; localized renovation presentation; structural construction through the real root; missing deletion policy; both direct/corridor deletion cases; both replacement cases.
- `Gd66WindowsSpatialMigrationFileSystemTests.CurrentNonWindowsRuntimeFailsClosed` (`gd66.test.windows_only_inverse`) on this Windows host.
- `Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification` (`gd66.test.windows_player_only`) remains an expected automated Editor-run skip; separate owner Windows standalone qualification passed as recorded above.

## Changed files by responsibility

Production authoring/configuration/localization:

- `ContentAuthoring/DungeonSpatial/authoring_manifest.json`
- `Assets/_Project/Editor/DungeonSpatial/DungeonSpatialAuthoringPackage.cs`
- `ContentAuthoring/DungeonSpatial/tables/floors.csv`
- `ContentAuthoring/DungeonSpatial/tables/floor_allowed_rooms.csv`
- `ContentAuthoring/DungeonSpatial/tables/floor_allowed_corridors.csv`
- `Assets/_Project/Data/Production/DungeonSpatial/dungeon_spatial_content.json` (generated)
- `Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json` (generated)
- `Assets/_Project/Resources/floor_construction_profiles.json`
- `Assets/_Project/Resources/floor_construction_profiles.json.meta`
- `Assets/_Project/Data/Bootstrap/string_table_en.json`

Canonical mutation, validation, economy, editing, and publication:

- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorConstructionAuthority.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorConstructionAuthority.cs.meta`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalProductionSemanticValidation.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSpatialMutation.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLayoutValidation.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralDeletionService.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralEditService.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralInvestment.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralRenovationService.cs`
- `Assets/_Project/Scripts/Services/CompletedResearchStateResolver.cs`
- `Assets/_Project/Scripts/Services/SaveService.cs`
- `Assets/_Project/Scripts/Core/GameRoot.cs`

Run isolation and temporary Bootstrap presentation:

- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalMvpRouteProjection.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseFiveBRouteProjection.cs`
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`

Build gate and tests:

- `Assets/_Project/Editor/DungeonSpatial/ProductionSpatialContentBuildGate.cs`
- `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSixAEditModeFixtures.cs`
- `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionSpatialContentExportTests.cs`
- `Assets/_Project/Tests/EditMode/CompletedResearchStateResolverTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA2FloorConstructionTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA2FloorConstructionTests.cs.meta`
- `Assets/_Project/Tests/EditMode/Gd66SaveWorkloadMeasurementTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseFiveADurableBranchTests.cs`
- `Assets/_Project/Tests/EditMode/ProductionSpatialContentLoadingTests.cs`
- `Assets/_Project/Tests/EditMode/StructuralEditServiceTests.cs`

Current status/evidence and generated-test hygiene:

- `Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md`
- `Docs/19 - Content_Pipeline_and_Data_Authoring.md`
- `Docs/28 - Save_Data_Model_Versioning_and_Migration.md`
- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`
- `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md`
- `docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md`
- `docs/planning/post-gd60-mvp-execution-plan.md`
- `docs/planning/gd65b-production-spatial-content-approval.md`
- `Docs/testing/evidence/phase6a2-floor2-construction-inactive-editing/implementation-evidence.md`
- `README.md`
- `.gitignore`

## Known limits and final review state

No requirement needs schema 12. No activation/deactivation, lifecycle cascade, additional deeper floor, multi-floor traversal, descend/exit decision, survivor transfer, multi-floor settlement/reporting/knowledge, Floor-2-exclusive content, shell demolition/refund, run-event mana, backend/concurrency redesign, or Phase 7 editor is implemented. Research progression UI remains the existing scaffold. Selected floor/room is transient editor presentation, not persisted identity authority; reopen uses deterministic default selection.

Owner Editor and Windows standalone qualification passed as recorded above. **FINAL EXTERNAL CHATGPT MERGE REVIEW PENDING — do not merge yet.**

This packet is not declared ready to merge. Stop for final external ChatGPT review.
