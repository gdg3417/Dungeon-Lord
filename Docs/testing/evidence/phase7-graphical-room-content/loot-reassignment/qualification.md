# PR 231 sequential loot reassignment investigation

Starting local/fetched PR HEAD: `ee1ff9a5d72ed1e46e5c6e61e744d77d1bd7c976`. Main/base: `5932a9fe0b7cec87c81f09e8e7d619df62c53101`. Branch: `codex/phase7-graphical-room-content-management`. Initial working tree was clean. Frozen implementation: `89a60250e8cf06eedb2decdb0c703525eb06f6b9`. Later evidence-only commits preserve its four Unity input trees. Existing PR #231 remains open; no new PR or merge.

Policy recommendation: **GPT-6.1 Sol, Medium; Standard correction**, because this is a bounded investigation of established authorities and UI projections. Policy fallbacks: GPT-6 Sol Medium, then GPT-5.6 Sol Medium. This recommendation does not silently change the session model. AGENTS.md, model policy, Phase 7A0 Decision 41, mobile UI vision, approved content rules, existing transaction invariants, production tests and prior qualification were reconciled.

## Confirmed finding

The requested valid two-step exchange already passes on the reviewed implementation, including actual `TapWorld` destination interaction and capacity labels. No stale assignment-count, draft projection, canonical projection or Save/reopen defect was reproduced in Case A. Confirm rebuilds the floor from the latest `draft.ReadModel`; capacity display counts that projection. No count or mutation authority change is warranted by these fixtures.

The reproducible defect is **misleading rejection presentation**. The existing authority rejects duplicate loot options with `gd66.diagnostic.canonical_write_noop` and category capacity with `gd66.content.room_capacity_exceeded`. These untranslated keys both displayed **Unavailable**, leaving the player unable to distinguish approved restrictions. An occupied cell has the distinct authoritative `content.position.invalid` reason. Duplicate trap/loot options remain prohibited; repeated supported monster acquisitions remain allowed.

`ProductionDungeonRoomContent` now maps those existing preview reasons to localized, actionable messages: the room already contains this loot/trap type; the category is full; or the cell cannot hold the content. Both contextual feedback paths use the same mapping. Validation order and all gameplay decisions remain in the existing authorities. The owner's specific dungeon configuration was not inspected, so these results do not claim which restriction caused that particular attempted move.

## Deterministic regression evidence

All fixtures use configured canonical purchases only to prepare unique isolated test saves before Edit Mode; preservation baselines are captured after fixture purchases. Actual reassignment uses the controller, map taps, existing preview, Confirm and the whole-dungeon Save. No assertion was weakened.

- **A, both Save variants:** A starts with Basic Loot Node + Hidden Cache (2/2), B with Glittering Hoard (1/2). Confirm Hidden A-to-B yields A 1/2 and B 2/2. Confirm Glittering B-to-A yields A 2/2 and B 1/2. Actual labels and draft counts agree after each Confirm, recovery, Save and close/reopen. A second variant saves and reopens between moves. Both preserve AssignmentId/category/option/sequence/exact local positions, zero acquisition count/cost, wallet and empty returned custody. Pre-Confirm commands and pre-Save canonical bytes remain unchanged.
- **B, exact duplicate fixture:** each room contains only one Glittering Hoard. Reassigning into the other room is rejected at **1/2**, with the approved duplicate-option reason and a specific localized explanation. Confirm cannot mutate the draft or canonical state.
- **C, full room:** B contains two distinct loot nodes. Another distinct node is rejected at **2/2** with the capacity reason and category-full explanation; no command is added.
- **D, collision:** B has capacity available at **1/2**, but the tapped cell contains Glittering Hoard. The preview has the position reason and cell-specific explanation; no capacity claim or command. Cancel and Discard retain canonical bytes and wallet.

Against the reviewed runtime, the first investigation run was **50 total, 49 passed, one failed**: both valid sequential tests passed; the blocker test failed on the missing localized explanation after checking all three authority reasons. The first corrected focused run was **50/50 passed**. Tightening Case B to exactly one Hoard per room exposed one **test-fixture baseline error**, not a production failure: preparing the subsequent C/D fixture purchased Hidden Cache at its configured price, but retained the earlier wallet baseline. Capturing the baseline after that setup purchase corrected the fixture; the final exact blocker rerun is **1/1 passed**. Original failure XML and logs are retained with these dispositions.

## Scope

Changed files: `ProductionDungeonRoomContent.cs` (reason-to-localization presentation only), `string_table_en.json` (four explanatory keys), `ProductionDungeonRoomContentSceneTests.cs` (three deterministic actual-controller regressions), `PhaseSevenA4ProductionShellPlayModeTests.cs` (three adapters), `DungeonVisualBuildQualification.cs` (separate historical-preserving output slot), `collect_loot_reassignment_evidence.py` (new bounded evidence packet), a focused execution-plan amendment and this evidence directory.

No changes to `TransactionalDungeonDraftContent`, detached canonical mutation, occupancy authority, `RebuildFloor`, room capacities, loot definitions, duplicate rules, prices, custody or save projections. Save schema **13**, draft format **5**, immutable record version **4**, atomic Save, Discard/recovery, floor knowledge and run snapshots remain unchanged. Accepted New/Owned browsing, compact placement sheet, warm grid, explicit reassignment/map input and artwork mappings remain unchanged. No new ownership policy or specification approval is required.

## Complete qualification and build

Final full EditMode: **1,675 total, 1,674 passed, zero failed, one skipped**. Final full PlayMode: **3,106 total, 3,096 passed, zero failed, ten skipped**. Existing outcomes are unchanged versus the previous map-reassignment qualification: three tests added per mode, none removed or changed. Established skips are the non-Windows inverse on Windows, eight synchronous EditMode fixtures discovered in PlayMode, and one Windows-player-only filesystem preflight. [Every failure/skip and measured fixture output](report-summary.json), [original six XML reports](qualification-reports.zip), [outcome comparison](test-outcome-comparison.json) and [redacted execution logs](unity-logs-redacted.zip) are retained. All seven run logs have zero C# compiler errors/warnings.

Focused screenshots were reviewed before complete qualification; [two fresh final production captures](screenshots.md) were reviewed again before building. The duplicate fixture visibly reads 1/2 with the specific rule explanation; the completed exchange visibly reads Room 2 1/2 and zero draft cost. The collector checks capture freshness against final PlayMode and retains hashes.

Windows **StandaloneWindows64 Development**, Unity **6000.3.2f1**: **Succeeded, zero errors, one warning**. The warning is an invalid empty URI when obtaining the Unity native-symbol upload token; native symbols were not uploaded. All **52** referenced presentation art assets are packed. The new player contains **303 files / 181,829,705 bytes**, including qualification reports. [Original build reports](build-reports-original.zip), [build report](build-report.json), [messages](build-messages.txt), packed source/art lists and [complete SHA-256 manifest](windows-artifact-manifest.json) are retained.

Final player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-loot-reassignment/Dungeon Lord.exe`. The four previous players are preserved; this is build qualification, not a claim of standalone interaction UAT.

## Isolation, source and preservation

Before the first refresh, the process check found no Unity invocation and the selected checkout lock was absent. Only the existing `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation` was reused; its imported Library was retained. No new Unity clone, deletion, quarantine or historical replacement. The [eight-project inventory](project-inventory-before.json) was collected during the task's owned headless investigative run: the selected project's `locked: true` reflects that run, not an owner's concurrent Editor or permission to refresh it. Another old room-construction checkout's lock is left untouched. Final checks found no Unity invocation or selected lock after a transient process completed its exit.

All **1,208 inputs** under Assets, Packages, ProjectSettings and ContentAuthoring, including generated metadata, are compared by the unchanged strict helpers. Unexpected target-only files remain rejected. Fixtures arm unique isolated save namespaces before GameRoot startup; the owner project was never run for qualification. Reviewed namespace and text-normalization transformations remain unchanged.

Immediate pre/post checks surround full EditMode, full PlayMode and the build. Post-EditMode equality was exact. Post-PlayMode generated TMP fallback-font/settings changes; post-build generated four URP/settings changes. Each mismatch was exported before restoring disposable inputs, retaining Library and outputs. [Twenty-eight source inventories and exported diffs](source-inventories-and-diffs.zip), [checkpoint index](source-checkpoint-index.json) and [frozen Unity input trees](qualified-input-trees.json) retain evidence. Final equality: **1,208 expected/actual, zero mismatches, zero target-only files**. Publication is checked again after evidence-only changes.

Fresh read-only hashes show **all eight audited save/sidecar and primary-settings files unchanged**, with both save-prefix filename inventories unchanged. [Owner preservation](owner-preservation.json). This is a file audit, not a whole-registry audit. **All 1,212 files in the four historical Windows players retain their hashes and counts**. [Historical build preservation](earlier-build-preservation.json).

Selected project inventory during investigation: **3.034 GiB**, Library **2.228 GiB**. Final project: **3.206 GiB**, retained Library **2.230 GiB**, new player **0.169 GiB**. [Exact final bytes and lock state](final-disk-and-lock.json). Cleanup candidates only, without deletion: seven other inventoried Temp copies, four historical players, imported/cache outputs and old test/log evidence. The new qualified player and concise new packet are retained. The two failed investigation reports are retained with their explicit dispositions above.

Status: **ready for external review and limited owner Editor/Windows rerun**. No stale-state defect was reproduced by the requested fixtures; the original owner's particular combination still needs the focused check below. No new authority/persistence ambiguity requiring a policy escalation was discovered. PR #231 is not merged.

## Limited owner rerun

In the isolated Editor and the new Windows player, sequentially:

1. Use A: Basic + Hidden Cache; B: Glittering Hoard. Reassign Hidden to B and Confirm; verify A 1/2, B 2/2. Reassign Hoard to the freed cell in A; verify A 2/2, B 1/2 and zero acquisition cost. Save and reopen. Repeat once with Save/reopen between moves.
2. Attempt to move Hoard into a room already containing Hoard at 1/2. Verify the duplicate-type explanation. Try a genuinely full distinct-option room and an occupied cell in a non-full room; verify different explanations and no mutation.
3. Cancel an invalid preview and Discard an uncommitted exchange; verify canonical ownership and mana remain unchanged. If the original configuration still rejects an otherwise valid move, record the destination room, its assigned option names, capacity label, selected cell and exact explanation.

Accepted placeholder art remains unchanged. No general UI redesign, new content, balancing, implicit cross-room Move, domain reassignment protocol, save/journal revision, floor lifecycle or right-side inspector. Owner interaction UAT is still required; automated Editor tests plus a Windows build do not establish standalone runtime UAT.
