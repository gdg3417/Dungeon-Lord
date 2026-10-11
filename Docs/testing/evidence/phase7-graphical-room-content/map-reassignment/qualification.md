# PR 231 map-based explicit reassignment correction

Starting local/fetched PR HEAD: `ed06ad9e6f7fd8693b5ff9867b45d469e4ac395e`. Main/base: `5932a9fe0b7cec87c81f09e8e7d619df62c53101`. Branch: `codex/phase7-graphical-room-content-management`. Initial working tree was clean; all subsequent edits belong to this correction. Frozen implementation: `ff5e0c255143c8c4a5e3694f8ca148cd283400ec`. Later evidence-only commits preserve its four Unity input trees. Existing PR #231 stays open; no new PR or merge.

Policy recommendation: **GPT-6.1 Sol, Medium; Standard correction**. The root cause is known and the work reuses existing UI, draft and canonical authorities. Policy fallback: GPT-6 Sol Medium, then GPT-5.6 Sol Medium. This is the recommendation for the current task, not a silent session-model change. AGENTS.md, current model policy, Phase 7A0 Decision 41, mobile UI vision, production input/content code, transaction invariants, existing scene tests and previous qualification were reconciled. No new specification or ownership decision is required.

## Root cause and fix

`BeginContentReassignment` stores `reassigning` but leaves `contentIntent` null while requesting a destination. `TapWorld` intercepted only `IsPlacingContent`, so destination taps reached ordinary selection and `ClearContentSelection`, losing the original identity. Previous scene tests called destination selection directly and therefore bypassed this defect.

`TapWorld` now gives pending explicit reassignment precedence over ordinary map/object selection. A small private adapter uses the existing current-floor `SelectContentRoom` hit detection and `SelectContentDestination`, then the existing `PreviewRoomContent` for that exact tapped cell. It does not create a command. Occupied/full rooms still produce authoritative invalid previews that can be corrected before Confirm.

Destination selection rejects the source room and missing rooms/floors, clears stale overlays and presents a localized blocker without clearing the pending AssignmentId/category. The contextual information label now displays that feedback while waiting. The localized instruction explicitly advertises map taps and the retained destination buttons. Cancel retains the existing preview/grid cleanup. Once positioning starts, its existing fixed-target behavior is retained; changing destination again uses Cancel/Reassign. Destination buttons still support other floors through the same method. Map hit detection is scoped to the displayed floor. Move remains same-room; New/Owned retains its accepted retargeting behavior.

Confirm Placement records one acknowledged durable draft command. Save Changes uses the existing whole-dungeon transaction. Domain mutation/validation, purchase eligibility, pricing, returned custody, identity/category/option/sequence, exact room-local geometry, occupancy, capacities, floor knowledge and immutable current-run snapshots are unchanged. Save schema **13**, journal format **5** and immutable record version **4** remain unchanged. No implicit Move-to-Reassign, cross-floor map placement, Unassign prerequisite, second controller/authority, artwork change or panel redesign is introduced.

## Changed files

- `ProductionDungeonController.cs`: pending reassignment input precedence.
- `ProductionDungeonRoomContent.cs`: map-to-destination adapter, recoverable invalid destination feedback and its presentation.
- `string_table_en.json`: discoverable destination instruction and localized invalid-destination key.
- `ProductionDungeonRoomContentSceneTests.cs`: three actual-controller/input regressions and registered-click coverage for the existing destination button.
- `PhaseSevenA4ProductionShellPlayModeTests.cs`: three production PlayMode adapters.
- `DungeonVisualBuildQualification.cs`: separate Windows output slot preserving three historical players.
- `collect_map_reassignment_evidence.py`: bounded new evidence packet, unchanged-outcome comparison, source/save/build preservation audit.
- Focused execution-plan amendment and this evidence directory.

## Automated qualification

The new tests were run against the original routing before testing the fix. Only this task's own three routing/localization edits were temporarily restored to reviewed HEAD bytes, with corrected bytes retained, and strict source equality was checked. Original-defect reproduction: **3 tests, 0 passed, 3 failed**. The failures prove pending state is cleared by an invalid map tap, a destination click fails to start positioning, and the actual production mouse tap fails. Original XML, assertion locations and logs are retained; these expected red failures are not passing qualification. Corrected bytes were then restored and requalified.

Focused PlayMode: **47 total, 47 passed, 0 failed, 0 skipped**. It includes all three new tests and existing room-content/domain/production cases matched by the filter. The new cases cover monsters/traps/loot, occupied cells, exact valid positions, category capacity, outside/source-room/missing-floor rejection, cancel/retry, original ID/category/option/sequence, unchanged custody and free reassignment, no draft mutation before Confirm, one acknowledged command afterward, canonical bytes/wallet unchanged before Save, Save/reopen, Discard/recovery, registered destination buttons, Move/New/Owned and real production Input System mouse/touch routing including touch Confirm.

Full EditMode: **1,672 total, 1,671 passed, zero failed, one skipped** (the established non-Windows inverse on Windows)..
Full PlayMode: **3,103 total, 3,093 passed, zero failed, ten skipped**. Established skips are eight synchronous EditMode fixtures discovered in PlayMode, the non-Windows inverse on Windows, and one Windows-player-only filesystem preflight..
Existing-case/outcome comparison: **No existing test case or outcome is removed or changed; three tests are added per mode** versus the preceding qualified PR HEAD..
All failed/skipped test names, messages and dispositions are retained in [report-summary.json](report-summary.json) and [original XML](qualification-reports.zip). No old geometry, transaction or input assertion was weakened. [Redacted logs](unity-logs-redacted.zip) and [log index](log-index.json) retain actual execution diagnostics; startup licensing messages did not prevent these test runs.

## Actual visual evidence and build

The focused production screenshot was inspected before full qualification. [Final capture and review](screenshots.md) is generated again during final PlayMode and checked for freshness. It shows explicit Reassign, Room 2 capacity, exact valid cell and reachable Cancel/Confirm/Save/Discard. The original Skeleton remains in Room 1 during preview. The displayed 100-mana draft quote is construction of Room 2, not a reassignment purchase; tests assert zero content acquisitions/cost. Compact accepted layout, warm transparent grid and artwork mappings are unchanged.

Windows Development build: **Succeeded, StandaloneWindows64 Development, Unity 6000.3.2f1, zero errors, one warning**. The warning is an empty Unity Cloud token for native symbol upload; symbols were not uploaded. Final full-test/build logs contain zero C# compiler errors/warnings. All **52** referenced art assets are packed; the final player contains **303 files / 181,827,605 bytes** including reports..
Final player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-map-reassignment/Dungeon Lord.exe`.
[Build report](build-report.json), [original reports](build-reports-original.zip), [messages](build-messages.txt), packed source/art lists and [SHA-256 player manifest](windows-artifact-manifest.json) are retained. This qualifies a built Windows player; it does not claim automated standalone runtime interaction UAT.

## Source, saves and disk preservation

The required reusable checkout initially had the owner's Editor PID 47484 and its lock. It was not refreshed or tested until the owner closed it and a read-only process/lock check confirmed closure. Reused project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation`, initial **2.859 GiB**, imported Library **2.224 GiB** retained. No new Unity clone, deletion or quarantine. [Closure precondition](editor-precondition.json) records the observed absence of Editor/lock before refresh. [Initial project inventory](project-inventory-before.json) records all eight exact Temp project paths, sizes, Library and lock states; the selected lock records the initial owner UAT session, not permission to run concurrently. The unused room-construction checkout's lock is not touched.

All **1,208 inputs**, including generated metadata, under Assets/Packages/ProjectSettings/ContentAuthoring are compared. Existing strict helpers and reviewed save-namespace/text-normalization transformations are unchanged; unexpected target-only files are rejected. Test fixtures arm unique isolated namespaces before GameRoot startup. The owner project was never run for qualification.

Pre/post checks immediately surround both full suites and the build. Every mismatch is archived before refreshing disposable input bytes; Library and outputs are retained. Post-EditMode equality was exact. Post-PlayMode detected generated TMP fallback-font and ProjectSettings serialization changes; post-build detected four URP/settings serialization changes. All six diffs were captured before restoring disposable inputs. Final equality is **1,208 expected/actual inputs, zero mismatches, zero target-only files**. Nineteen source inventories retain focused/red/full/build stages.. [Inventory/diff archive](source-inventories-and-diffs.zip), [checkpoint index](source-checkpoint-index.json) and [qualified four input trees](qualified-input-trees.json) retain hashes and exact transformations. Published HEAD equality is checked again after documentation.

**All eight audited files retain their before SHA-256 hashes, and both save-prefix filename inventories are unchanged**. Fresh read-only hashes cover both current primary/isolated save-prefix filename inventories and six save/sidecar files, plus two primary source settings. This is a file preservation audit, not a whole-registry claim. [Owner preservation](owner-preservation.json). **All 909 files in the three earlier Windows builds retain their hashes and counts**. [Historical player preservation](earlier-build-preservation.json).

Final disk use: reused project **3.032 GiB**, retained Library **2.227 GiB**, new player **0.169 GiB**. All three prior players remain preserved. Cleanup inventory, without deletion: the seven other imported Temp checkouts, three historical players, imported/cache outputs and historical test/log evidence. The new qualified player and concise new packet are retained. Owner authorization is required for deletion; no cleanup is performed here. Final read-only process/lock verification found no Unity Editor or checkout invocation and no validation lock.

The old stale-footprint review thread `PRRT_kwDOQxUKE86rHSUp` remains resolved. Its existing invalid-preview cleanup is unchanged and requalified by existing grid/multicell tests. Before publication, main/remote PR refs were fetched again and remained at the stated main/starting PR baseline; no intervening implementation invalidated this correction..

## Limited owner rerun

Run the isolated Editor and final Windows player sequentially, against the isolated validation save:

1. Select a monster, trap and loot object; click Reassign; tap a different constructed room. Verify the same click starts an exact-cell preview with the chosen content identity. Try an occupied cell and a full destination; adjust or cancel/retry.
2. While choosing a destination, tap outside rooms and the source room. Verify readable feedback and retained choice; then choose a valid room. Check the destination-button alternative, including another-floor choices where available.
3. Confirm once, review Save Changes, save and reopen. Start another reassignment and Discard; confirm the original canonical state is retained. Check map interaction with mouse/touch and that normal Move and accepted New/Owned targeting still behave as before.

Accepted placeholder mappings remain unchanged: Goblin, Chilling Sigil, Snare Trap, Hidden Cache and Basic Loot Node. No new art, balancing, content type, floor lifecycle, drag-and-drop, protocol/schema revision, right-side inspector or broad refactor. Final status: **automated qualification complete; ready for external review and the limited owner Editor/Windows UAT below**. Do not merge.
