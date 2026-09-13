# Returned-content redeployment validation

Exact clean starting baseline: merged PR #201, `eca6db1ec984fd476d86c4e9af1a55ecd6df3d20`.
Feature branch: `phase-4a-returned-content-redeployment`.

## Automated qualification

Unity executable: `C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe`; required editor version: `6000.3.2f1`.
Each invocation had a preceding Unity process check and separate one-time approval, and ran outside the sandbox. Existing Unity processes were never killed.

Commands actually invoked:

```powershell
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" test "C:\Dev\Dungeon-Lord" --mode EditMode --output "$env:TEMP\phase4a_redeploy_editmode.xml"
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" test "C:\Dev\Dungeon-Lord" --mode PlayMode --output "$env:TEMP\phase4a_redeploy_playmode.xml"
```

The EditMode command was invoked five times across discovery, correction and final qualification; the PlayMode command was invoked once after clean EditMode. The running editor path was verified as `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe`, matching ProjectVersion.txt (`6000.3.2f1`, revision `a9779f353c9b`).

The first two full EditMode runs passed the existing 712 cases (156.0583019 and 214.6381361 seconds), including the extended workload regression. Artifact inspection proved the new class was not yet discovered: core fixtures live in Assembly-CSharp and require the repository's editor subclass bridge. `PhaseFourRedeployment` now registers the new class through `PhaseFourEditModeFixtures.cs`. These first two results are not claimed as qualification of the new class. Their XML artifacts were retained separately as `phase4a_redeploy_editmode_initial.xml` and `phase4a_redeploy_editmode_preregistration.xml` in the user TEMP directory.

The first registered run discovered 734 cases and passed 733, with one stale-session failure (173.841511 seconds). The general writer intentionally accepts an already-durable identical candidate for retry. The new regression demonstrated that redeployment must instead reject a session whose original bytes no longer match disk. A redeployment-only freshness read now enforces that precondition before preparing custody movement; other write retry behavior remains unchanged. The failing artifact is retained as `phase4a_redeploy_editmode_stale_failure.xml` in user TEMP.

The next full run passed all 22 redeployment cases, including the freshness fix, but finished 734/735 (191.540114 seconds). The new production-boundary test incorrectly called Single on all floor assignments, although its existing lifecycle fixture retains starter-room assignments. The transition and unchanged record-count assertion had succeeded. The assertion now locates the moved item by durable ID and additionally compares every prior assignment unchanged. Its artifact is retained as `phase4a_redeploy_editmode_boundary_assertion_failure.xml` in user TEMP.

Final registered EditMode result: **735/735 passed**, zero failed, skipped or inconclusive; duration **174.1975717 seconds**. `PhaseFourRedeployment` is explicitly present in the final XML with **22/22 passed**. Result artifact: `C:\Users\gdg34\AppData\Local\Temp\phase4a_redeploy_editmode.xml`; SHA-256 `4E49C61EB933B344C4DB08E1E9483DA0319E1FBBB9781A6FA0E8183D81D530BF`.
Full PlayMode result: **2,235 passed, zero failed, 10 skipped/ignored, zero inconclusive out of 2,245 total**, duration **172.3759171 seconds**; Unity CLI exited zero. The XML root result is `Skipped:Ignored`, reflecting the ignored cases rather than a claim that every case executed. All **22/22** redeployment cases passed in PlayMode too. Result artifact: `C:\Users\gdg34\AppData\Local\Temp\phase4a_redeploy_playmode.xml`; SHA-256 `9A4E23FB3FAA069EB0A5AFDAE31383307861B38659F5B5F9DA14FE5403678BEF`.

The 10 ignores use existing mode/platform guards: eight `Gd66GameRootBootIntegrationTests` cases with `gd66.test.synchronous_edit_mode_fixture`, `CurrentNonWindowsRuntimeFailsClosed` with `gd66.test.windows_only_inverse`, and `WindowsStandalonePreflightAndNativeFilesystemQualification` with `gd66.test.windows_player_only`. No redeployment case was ignored; standalone qualification remains deferred.

The new fixture covers ownership identity/category/option preservation, canonical ordering and equivalent-state bytes, preserved/colliding/high sequences, overflow, missing custody/target, full capacity, same-option no-op, fresh acquisition identity without custody consumption, duplicate/assigned-and-returned corruption, invalid/unavailable content, persisted loot despite unresolved current removal policy, durable schema-9 reopen, exact wallet/investment invariance, injected atomic replace failure, stale supplied state, absent/stale sessions, and localized Bootstrap selection/actions for either room in a two-room canonical route.

The historical exact 64-record workload test now successfully redeploys one record from custody to assignment without increasing canonical record count, reopens the result, and refuses a below-bound workload without changing durable bytes. Measured result:

```text
historical-64-record-redeployed:rawBytes=132535,rawDepth=10,rawMembers=37,rawElements=39,rawStringBytes=69,rawScanWork=136374,candidateBytes=132535,strictInputBytes=132535,strictNodes=4409,strictRecords=1306,strictStringChars=108328,diagnostics=0,canonicalRecords=64,canonicalTiles=1,copiedBytes=118781,unknownCount=2,unknownBytes=27
```

These numbers are evidence, not production tuning or new limits. Production workload configuration is unchanged.

The production custody-array boundary test creates the authored maximum returned array of 128 through repeated real construction/placement/deletion, then redeploys one owned record through the writer. It proves unchanged canonical record count (149), decreased custody count (127), preserved identity, unchanged prior assignments, and successful production-profile reopen. Final artifact measurement:

```text
production-custody-array-boundary-redeployed:rawBytes=36962,rawDepth=10,rawMembers=37,rawElements=127,rawStringBytes=69,rawScanWork=38311,candidateBytes=36962,strictInputBytes=36962,strictNodes=1293,strictRecords=187,strictStringChars=30100,diagnostics=0,canonicalRecords=149,canonicalTiles=1,copiedBytes=6903,unknownCount=2,unknownBytes=27
```

## Unity-generated side effects

The first two runs left no persistent generated working-tree diffs. During the second run an untracked `Assets/_Project/Editor/DungeonSpatial/Tests/TempBuildGate.meta` appeared temporarily. The existing build-gate test SetUp creates that folder and refreshes AssetDatabase; TearDown deletes it and refreshes again. It disappeared through test teardown; no agent discard was performed. This is incidental test fixture metadata, not a required production asset.

The first registered run, the next corrected run and the final clean EditMode run also left no persistent generated working-tree diffs. Temporary build-gate metadata was observed during the corrected run and disappeared through existing teardown.

PlayMode generated `Assets/InitTestScene036b8f59-0674-47c8-8cc4-5f3c8e566d7f.unity` and its meta file temporarily. Exact contents showed the standard PlaymodeTestsController and test assembly configuration. The installed test framework's CreateBootstrapSceneTask creates the scene, and DeleteBootstrapSceneTask deletes it. Both files disappeared through runner cleanup; no agent discard was performed.

After PlayMode, `ProjectSettings/ProjectSettings.asset` differed only by reordering the identical `applicationIdentifier` entries: baseline Standalone then Android became Android then Standalone, both retaining `com.gdg3417.dungeonlord`. This was classified as incidental. A separate explicit approval authorized `git restore -- ProjectSettings/ProjectSettings.asset`; the approved restore removed that generated diff. No TMP, URP, GraphicsSettings or UnityConnectSettings changes were present. Final working tree contains only the intended implementation/test/documentation changes.

## Independent manual Unity review still required

1. Start from a clean canonical schema-9 save. If needed, use the development-only QA mana fill to set up structural edits.
2. Establish two usable rooms through normal structural construction. Record mana before each tested redeployment, separately from construction/deletion charges.
3. Select the leaf room as the normal content target and place reusable monster or trap content through ordinary placement.
4. Delete that leaf through production structural deletion. Select the leaf using the existing renovation/deletion target controls; the selected content destination is the separate canonical room target control.
5. Confirm returned content is visible by localized name. With multiple returned items, use Next returned item and verify deterministic sequence/AssignmentId order and wraparound.
6. Select a remaining compatible canonical room and use Redeploy into selected room. Confirm custody decreases and destination content appears.
7. Verify the same AssignmentId in test/inspector evidence, and confirm redeployment itself changes neither mana nor structural investment.
8. Save, close, reopen, and confirm the assignment persists and the returned copy does not reappear.
9. Repeat with an invalid/full destination and confirm localized feedback and retained custody. For a distinct-option full-capacity test, return Chilling Sigil while the destination Basic Room has Spike Trap and Snare Trap; monster capacity can encounter the same-option rule first.
10. Test same-option rejection, normal fresh placement while custody exists, and repeated redeployment after custody is exhausted. No original item can duplicate.

Current loot removal policy remains unresolved; this PR does not invent a production loot-return policy or manufacture production ownership for manual review. Legitimate previously persisted loot custody is still redeployable.

No standalone build or manual gameplay qualification is claimed. The existing one-floor Bootstrap MVP architecture and supported canonical persistence platform boundary remain. There is no general inventory, acquisition pricing, starting mana, online mana earning, offline mana, migration, schema change, or structural tuning change. The PR must remain unmerged for independent review.
