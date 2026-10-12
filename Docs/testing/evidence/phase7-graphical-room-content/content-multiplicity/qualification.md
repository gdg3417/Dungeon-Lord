# PR 231 owner-approved room-content multiplicity

Starting clean local/fetched PR HEAD: `c4d17bf82dec8984fc46fc7bdbde9ade28d7f46f`. Main/base: `5932a9fe0b7cec87c81f09e8e7d619df62c53101`. Branch: `codex/phase7-graphical-room-content-management`. PR #231 was verified open/unmerged. Frozen implementation: `992dcc932eb78180bb5da7c7f030aebc340fcfb9`; later documentation/evidence commits preserve its four Unity input trees. No new PR or merge.

Policy recommendation: **GPT-6.1 Sol Medium, Standard implementation correction**, because the owner-approved rule is clear and existing authorities represent repeated instances. Fallbacks: GPT-6 Sol Medium, then GPT-5.6 Sol Medium. No silent session-model change or model escalation. AGENTS.md, current policy, Spec 38, Phase 7A0 Decision 41, mobile UI vision, structural economy, transaction invariants, existing canonical/draft/simulation tests and prior qualification were reconciled. The Unity CLI skill is used for isolated qualification. No unresolved simulation/save-authority conflict was found.

## Exact approved rule

**Monsters, traps and loot nodes may repeat the same option type within one room. Limits are the existing category capacity and physical placement rules, not per-option uniqueness.**

Spec 38 now states this policy and its identity, economics and simulation boundaries. The structural-economy amendment and execution-plan note supersede the earlier trap/loot uniqueness boundary. Historical decisions, XML and qualification packets remain unchanged. This user instruction is the explicit gameplay approval previously deferred; it authorizes no new tuning or balancing.

Only two canonical conditions are removed: the non-monster matching-OptionId no-op in `PlaceContent` and in `RedeployContent`. All configured category capacity, option/category authorization, geometry, occupied-cell sharing/collision, stable assignment allocation, sequence collision handling, canonical validation and ordering remain unchanged. Explicit reassignment already composes unassignment/redeployment through these same authorities and therefore supports matching options without a second mutation owner. Same-room movement and individual unassignment continue targeting AssignmentId.

The production and Bootstrap presenters retire unreachable duplicate-type rejection mappings and three localization keys. Existing localized category-full and invalid-cell messages remain. New/Owned semantics, exact returned AssignmentId selection, compact sheet, grid, responsive styles, camera/input and artwork are unchanged.

## Economics, persistence and simulation

Each new instance pays its configured price once. Two matching new instances have two IDs and two charges. Existing owned redeployment/reassignment remains free; unassignment has no new cost. Combined draft quoting still identifies acquisitions by identity and commits exact validated bytes with durable readback through the existing whole-dungeon writer. Restricted/pending purchases, insufficient mana and injected persistence failure leave canonical bytes, wallet and custody unchanged. Preview creates no command; Confirm changes only the durable draft; Save is the canonical boundary.

Save schema **13**, draft format **5**, immutable record version **4**, ordering, recovery/predecessors, stale/durability safeguards, floor knowledge and existing run snapshots are retained. No journal revision is required: earlier accepted commands replay to the same states; previously rejected duplicate-option commands never entered those journals. Historical journals are not rewritten. Invalid repeated AssignmentIds still fail validation even though repeated OptionIds are now allowed.

Inspection verified `DetachedCanonicalProductionSemanticValidation` counts instances and validates identity/position/occupancy, without enforcing OptionId uniqueness. `CanonicalMvpRouteProjection` preserves all assignments and repeated option entries. `IntraroomTraversal` indexes occupied cells by AssignmentId, schedules each encountered trap and reached/unreachable loot instance, and orders events by path step, category, sequence and ordinal identity. `RunSimulationService.Spatial` includes each reached instance once; `MvpPlacementEffectsResolver` resolves one configured definition per option and processes every reached placement entry. Its definition lookup is not instance deduplication.

No simulation code is changed. Repeated effects remain governed by existing configuration, encounter and reached/cleared reward contracts. There is no new stacking bonus, diminishing return, loot/damage formula, RNG or capacity. Trap tests prove only physically encountered cells trigger; the bypassed copy produces no false encounter/effect. Loot tests prove separate reached IDs, an unreachable copy's separate evidence and exclusion from reached/cleared effects, and zero rewards when the room is not cleared. Repeated/permuted runs remain deterministic. Active canonical snapshots carry both identities, while a prior snapshot remains unchanged during a second-instance draft and after publication.

## Regression coverage

- All six current trap/loot options: two legal independent instances; third rejected at configured category capacity; overlapping copy rejected even with capacity; IDs and sequences distinct/canonical; move/unassign one without changing its sibling; returned redeployment retains ID/sequence and is free; journal recovery, Save/reopen and Discard.
- Canonical production-price tests now buy a second copy of every supported option, including both monsters, at its configured price and reject a third at capacity. The former duplicate no-op expectation becomes a physical-overlap expectation at the same cell. New duplicate-purchase restriction fixtures cover unaffordable, offline, verification-pending and atomic replace failure.
- Spatial simulation: three trap options each with two encountered copies and with one bypassed copy; three loot options each with two reached identities and with one unreachable copy; existing effects contribution, no repeated event/grant from one identity, failed room-clear reward exclusion, deterministic repeat/permutation, canonical active-floor projection and snapshot isolation during editing.
- Actual production/controller flows buy two Spike Traps and two Hoards, verify labels 0/2, 1/2 and 2/2, reject a third with localized capacity feedback, map-select each ID, move one, unassign/redeploy the other through Owned, explicitly reassign both into a destination already holding the first copy, recover the draft, Save/reopen and Discard. They verify no pre-Confirm command or pre-Save canonical/wallet mutation, exact preserved identities/sequences and zero owned-reuse quote. Existing New/Owned, mouse/touch, geometry, grid and three-text-size portrait/landscape/narrow-landscape tests are retained.

The first focused run was **56 total, 55 passed, one failed**. The new canonical snapshot fixture placed its second trap off the actual production traversal, so the correct simulator triggered only one. The fixture was moved onto the real path with an explicit path-membership assertion; runtime simulation was not changed. Corrected focused EditMode: **52/52 passed**. Focused PlayMode: **197/197 passed**. Final focused snapshot test after adding a draft-edit isolation assertion: **2/2 passed**. The original failed XML/log is retained with this test-fixture disposition. CLI licensing diagnostics were incidental; XML confirms the actual assertion failure rather than an infrastructure-only failure.

## Changed files

- `DetachedCanonicalSpatialMutation.cs`: remove the two OptionId uniqueness checks only.
- `ProductionDungeonRoomContent.cs`, `GameRoot.cs`, `string_table_en.json`: retire obsolete duplicate-type explanations, keeping capacity/position feedback.
- `TransactionalRoomContentTests.cs`, `ContentAcquisitionEconomyTests.cs`, `ReturnedContentRedeploymentTests.cs`, `PhaseSevenA3IntraroomTests.cs`: approve multiplicity and cover identity, purchase/custody/persistence and individual spatial simulation.
- `ProductionDungeonRoomContentSceneTests.cs`, `PhaseSevenA4ProductionShellPlayModeTests.cs`: real production map flows and test registration; revise the previous duplicate-Hoard restriction fixture without weakening capacity/collision assertions.
- `DungeonVisualBuildQualification.cs`: one new Windows output slot, preserving five historical players.
- `collect_content_multiplicity_evidence.py`: concise new evidence packet and fail-closed source/save/build/outcome audit.
- Spec 38, structural-economy amendment, focused execution-plan note and this qualification packet.

No content/config tuning, room lifecycle, schema, journal protocol, graphics/layout assets or simulation/economic calculation code is changed.

## Complete qualification and Windows build

Full EditMode: **1,705 total, 1,704 passed, zero failed, one skipped**. Full PlayMode: **3,136 total, 3,126 passed, zero failed, ten skipped**. Against the prior loot-reassignment qualification, each mode retires exactly three approved-rule cases (two `SameOptionTrapAndLootCannotDuplicate` cases and `FullCategoryAndNoOpNeverCharge`) and adds 33 cases, a net gain of 30. The renamed/revised assertions now permit distinct-cell repeated options and reject physical overlap. **No unrelated existing case is removed and no existing outcome changes.** [Exact comparison](test-outcome-comparison.json). No test is disabled or unrelated assertion weakened.

Established skips remain the non-Windows inverse on Windows, eight synchronous EditMode fixtures discovered in PlayMode, and one Windows-player-only filesystem preflight. [All six XML reports](qualification-reports.zip), [every failure/skip and measured output](report-summary.json), [redacted logs](unity-logs-redacted.zip) and [log index](log-index.json) are retained. All seven run logs contain zero C# compiler errors/warnings. The single failed investigation fixture has the explicit disposition above; it is not passing qualification.

Both [fresh final production captures](screenshots.md) were visually reviewed before the build. The collector verifies their timestamps against the final PlayMode run and retains hashes. They show two distinct copies, 2/2 capacity, 40/50-mana quotes, a third-copy capacity rejection, warm transparent grid and reachable portrait/landscape actions.

Windows **StandaloneWindows64 Development**, Unity **6000.3.2f1**: **Succeeded, zero errors, one warning**. The warning is an invalid empty URI while obtaining the Unity native-symbol upload token; symbols were not uploaded. All **52** referenced art assets are packed. Final player: **303 files / 181,828,700 bytes**, including qualification reports. [Build report](build-report.json), [original reports](build-reports-original.zip), [messages](build-messages.txt), packed source/art lists and [complete SHA-256 manifest](windows-artifact-manifest.json) are retained.

Final player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-content-multiplicity/Dungeon Lord.exe`. This qualifies a built player; owner standalone interaction UAT remains required.

## Isolation, source and disk preservation

Initial process/lock verification found no Unity invocation and no selected checkout lock. The [eight-project inventory](project-inventory-before.json) was collected before qualification: selected project **3.206 GiB**, Library **2.230 GiB**, unlocked. The old room-construction checkout's lock remains untouched. Only `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation` was reused; imported Library retained. No new imported clone, deletion, quarantine or historical replacement. Final process/path checks found no Unity invocation or selected lock after a transient post-build process exited.

Strict unchanged helpers compare all **1,208 inputs** and generated metadata in Assets, Packages, ProjectSettings and ContentAuthoring. Approved namespace/text-normalization transformations remain unchanged; unexpected target-only files remain rejected. Test namespaces are armed before GameRoot startup; the owner project was never run for qualification.

Immediate pre/post checks surround both full suites and the build. Post-EditMode equality was exact. Post-PlayMode generated TMP fallback-font/settings changes; post-build generated four URP/settings changes. Every mismatch was exported before refreshing only disposable inputs, preserving Library and outputs. [Twenty-four source inventories and exported diffs](source-inventories-and-diffs.zip), [checkpoint index](source-checkpoint-index.json) and [frozen input trees](qualified-input-trees.json) retain hashes. Final equality: **1,208 expected/actual, zero mismatches, zero target-only files**. Published HEAD is checked again after evidence-only commits and must preserve all four qualified Git input trees.

Fresh read-only audit: **all eight primary/isolated save, sidecar and primary-settings files retain hashes; both save-prefix filename inventories are unchanged**. [Owner preservation](owner-preservation.json). This is a file preservation audit, not a whole-registry claim. **All 1,515 files in five historical Windows players retain hashes and counts**. [Historical build preservation](earlier-build-preservation.json).

Final selected project **3.380 GiB**, retained Library **2.233 GiB**, new player **0.169 GiB**. [Exact bytes and lock state](final-disk-and-lock.json). Cleanup inventory, with no deletion: seven other inventoried imported checkouts, five historical players, caches and old test/log outputs. This new qualified player and concise evidence packet are retained.

Status: **automated qualification complete; ready for external review and targeted owner Editor/Windows UAT**. No unresolved authority conflict or new approval requirement. Do not merge PR #231.

## Targeted owner UAT

Run the isolated Editor and final Windows player sequentially against the isolated save:

1. In a Basic Room, buy two identical traps and two identical loot nodes on distinct legal cells. Verify each configured price is charged once, 1/2 then 2/2 labels, and a third gets category-full feedback. Try an occupied cell before the room is full.
2. Map-select each copy, move one and verify its sibling stays put. Unassign one, select its distinct Owned identity, and redeploy it beside the matching copy for free.
3. Explicitly Reassign a copy into another room already containing the same option and with capacity/cell available. Confirm, review the free quote, recover an uncommitted draft, Save/reopen and verify both IDs persist. Start another edit and Discard.
4. Start a run with separate trap cells actually on the traversed path and separate reachable loot nodes. Verify distinct interaction evidence and no off-path trap trigger. Check portrait/landscape readability; use existing accepted input/grid behavior.

Accepted placeholder mappings remain unchanged. Automated Editor tests and a qualified Windows build do not establish standalone owner interaction UAT or future language-pack/physical-device comprehension. No new artwork, content, balancing, floor lifecycle, drag-and-drop, right-side inspector or general UI refactor is included. Do not merge.
