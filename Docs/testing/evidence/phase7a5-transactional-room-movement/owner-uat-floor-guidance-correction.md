# A5 owner UAT: legal floor grid and movement guidance correction

The original A5 automated qualification did not establish player comprehension. Owner UAT on reviewed HEAD `84f2654181eae2bdeb906dea8f9e46bd7ea1d29f` found room selection and Move understandable, but placement required guessing. Screenshots showed out-of-bounds/fixed-overlap/connection failures without the full legal floor area or discoverable valid anchors. This was a blocking usability finding; it is not erased by the earlier passing tests or build.

Recommendation: GPT-6.1 Sol / Medium, focused meaningful correction to a reviewed complex PR (Standard under the canonical policy). Fallback GPT-6 Sol / Medium, then GPT-5.6 Sol / High if unavailable. No fallback or delegation was invoked. PR #227 remains unmerged.

## Root cause and scope

DungeonFloorWorldView derived both its grid and camera bounds from occupied tiles. Empty configured legal coordinates were invisible. No production targeting surface exposed PreviewMovement's valid alternatives. This correction implements the relevant A0 Decisions 2/3/13 without reopening the design.

Edit rendering resolves the selected saved floor's definition/index against ProductionSpatialContentSnapshot.Catalog.Floors, then uses its Bounds.Minimum/Width/Height/TileCount. Every legal coordinate receives a subordinate grid tile, with a separate outlined boundary. Edit entry/resume and Focus use that legal envelope; invalid footprints can extend the fit envelope while the legal outline stays fixed. Normal rendering hides grid/boundary and retains occupied-geometry bounds. Coordinate area is not FinalFloorSpaceCapacity.

The existing production-content MaximumMaterializedTiles bounds grid and candidate enumeration. It is separate from the canonical occupied-geometry limit, which still bounds every PreviewMovement. No limits/dimensions/capacity were changed. Hollow diamond markers carry meaning independently of color, preserve content and are cleared when targeting finishes or selection closes. Presentation-only sizes/colors are owned by Presentation.asset/DungeonPresentationPolicy; there is no overall screen restyle.

StructuralRenovationService.GetMovementGuidance is read-only. It prepares existing structural context, enumerates configured anchors in TileCoordinate order (X then Y), and delegates every alternative to PreviewMovement plus the existing strict draft production/occupancy validation. It owns no placement rules, persistence, economics or mutation. Current anchor is consistently excluded as a no-op. No alternatives receives explicit localized feedback. Invalid unresolved intent uses the unchanged last valid draft projection.

Guidance is computed when Move starts. A completed structural attempt ends targeting; the next Move recomputes from the new valid projection or unchanged projection after invalid intent. Selection/floor changes, discard and commit clear markers. Resume preserves intent, then selecting a room/Move reconstructs guidance. Update, wallet ticks and pending persistence acknowledgement do not enumerate candidates. Guidance is never serialized.

Worst-case enumeration is N-1 movement previews plus one existing context preparation, where N=Bounds.TileCount and N cannot exceed the configured production-content materialization limit. Each preview retains existing bounded serialization, canonical records, tile and semantic checks; result size is at most N-1. Current authored envelopes are 144 and 196 tiles; configured envelope ceiling is 4,096, canonical occupied materialization ceiling 64 and canonical record ceiling 165. Those numbers are read from content, not new runtime constants. Grid and markers are bounded per selected floor, and marker resources are reused between targeting sessions. Measured current authored guidance time is retained in the focused XML; native mobile latency remains owner/device qualification.

No draft format/record compatibility, invalid-intent durability, price/investment, schema/migration, save publication, run/knowledge/content identity, Bootstrap retirement or adjacent authoring capability changed. The existing economic/recovery/commit assertions remain intact.

## Qualification

Validation checkout: `C:/Dev/Dungeon-Lord/Temp/phase7a5-guidance-validation`, based on reviewed HEAD `84f2654181eae2bdeb906dea8f9e46bd7ea1d29f`. CLI: `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`; editor/project: `6000.3.2f1`.

### Intermediate failures and test-isolation incident

The initial focused launch failed compilation because NUnit's negative `Contain` overload treated a tile as a string; tests now check collection membership explicitly. No result XML was produced. The next two focused runs passed 10/10; screenshot review exposed green selected-room fill masking hollow targets, corrected by clearing selection fill when Move starts.

The first affected run passed 227/228 and failed the existing explicit-delete/fresh-boot regression. The nested coroutine unsubscribed its sceneLoaded isolation callback before async load completed, so fresh boot used the default save and encountered owner UAT draft evidence. Investigation first tried scene-root lookup, which failed because GameRoot.Awake moves the root to DontDestroyOnLoad. The singleton identifies the fresh root; an explicit async-load completion wait keeps isolation subscribed until scene loading finishes. New filename/callback assertions protect the existing fresh-boot assertion rather than weakening it.

These failed diagnostic boots logged normal passive/Boot/StateChange save activity against the default owner save path. This is a test-isolation incident, not proof that owner gameplay saves remained unchanged. Owner save/draft data was not deleted or rolled back. Subsequent diagnostics and qualification use a disposable fallback filename only in the isolated checkout's build_config.json; unique per-test isolation remains asserted. That temporary filename is restored exactly before the corrected Windows build and is excluded from the commit. No owner build configuration or ProjectSettings is changed.

An intermediate long-text test also saw a zero-height viewport after the failed restart. With explicit scene completion and a bounded layout settling wait, the unchanged positive viewport/text/button assertions pass. All intermediate XML counts are retained below; no skips or production validators were weakened.

### Exact commands and results

PowerShell test command form (output paths resolve to the owner repository TestResults directory under this wrapper):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/phase7a5-guidance-validation' --mode <mode> [--filter '<filter>'] --output TestResults/<report>.xml --timeout <seconds> --no-color
```

Filter aliases below are exact strings: Guidance=`MoveGuidance`; Isolation=`ExplicitDeleteQuiescesProductionShell;MoveGuidance`; Diagnostic=`PhaseSevenA5MoveGuidanceNoAlternativeIsLocalized`; Affected=`PhaseSevenA5;PhaseSevenA4;PhaseFourSpatial;Localization;LongText`; Shell=`PhaseSevenA4ProductionShell`; Gates=`ProductionSpatialContentBuildGateTests;ProductionSpatialContentExportTests;ProductionSpatialContentRecoveryTests;PhaseFourContent;PhaseSevenA4ProductionScene`. No filter means the full suite.

| Report under TestResults/ | Mode | Filter | Timeout | Total | Passed | Failed | Skipped |
|---|---|---|---:|---:|---:|---:|---:|
| phase7a5-guidance-first-focused.xml (not produced: compile error) | EditMode | Guidance | 1200 | — | — | — | — |
| phase7a5-guidance-second-focused.xml | EditMode | Guidance | 1200 | 10 | 10 | 0 | 0 |
| phase7a5-guidance-final-focused.xml | EditMode | Guidance | 1200 | 10 | 10 | 0 | 0 |
| phase7a5-guidance-affected.xml | EditMode | Affected | 1800 | 228 | 227 | 1 | 0 |
| phase7a5-guidance-isolation-focused.xml | EditMode | Isolation | 1200 | 11 | 7 | 4 | 0 |
| phase7a5-guidance-isolation-diagnostic.xml | EditMode | Diagnostic | 1200 | 1 | 0 | 1 | 0 |
| phase7a5-guidance-isolation-corrected.xml | EditMode | Isolation | 1200 | 11 | 9 | 2 | 0 |
| phase7a5-guidance-isolation-protected.xml | EditMode | Isolation | 1200 | 11 | 9 | 2 | 0 |
| phase7a5-guidance-isolation-wait.xml | EditMode | Isolation | 1200 | 11 | 11 | 0 | 0 |
| phase7a5-guidance-affected-final.xml | EditMode | Affected | 1800 | 228 | 228 | 0 | 0 |
| phase7a5-guidance-shell-playmode.xml | PlayMode | Shell | 1200 | 18 | 18 | 0 | 0 |
| phase7a5-guidance-full-editmode.xml | EditMode | none | 1800 | 1544 | 1543 | 0 | 1 |
| phase7a5-guidance-full-playmode.xml | PlayMode | none | 1800 | 2987 | 2977 | 0 | 10 |
| phase7a5-guidance-production-gates.xml | EditMode | Gates | 1800 | 288 | 288 | 0 | 0 |

Final full EditMode contains 52 passing A5 domain, 37 A4 domain, 29 A4 durability, 77 structural renovation/edit, 77 structural economy, 13 A2 positional, 36 A3 intraroom and 17 actual-scene cases. The complete 538-case original affected multiset, including 31 floor-knowledge cases, and 160-case save/session multiset are retained and passing. Localization/long-text and A4 content reposition flows pass in the actual scene and genuine PlayMode wrappers. Full PlayMode adds three new wrappers plus the seven shared domain cases; its production-shell fixture has 18 passing cases. These subsets overlap the full suites, not additional independent invocations.

Exact one-EditMode/ten-PlayMode skip names match reviewed A5 and the retained qualified A4 inventory: no new or changed skips. All intermediate failures are described above. Final logs: Temp/phase7a5-guidance-full-editmode.log, Temp/phase7a5-guidance-full-playmode.log; selected earlier logs: Temp/phase7a5-guidance-second-focused.log, Temp/phase7a5-guidance-final-focused.log and Temp/phase7a5-guidance-affected-failed.log. XML contains per-case output/failure traces. Source hashes for all nine intentional source/assets are TestResults/phase7a5-guidance-qualified-source-hashes.json. The final affected timing was 143 authoritative previews in 271 ms on this machine; this is not a worst-case/device guarantee.

The explicit production gates pass 288/288: build 65, export 112, recovery 57, loading 37 and actual scene 17; log Temp/phase7a5-guidance-production-gates.log. All 285 original gate case instances are also passing in the full EditMode multiset audit. Intentional `git diff --check` passes with the owner TMP excluded; owner ProjectSettings/packages are unchanged. No new/changed skips, canonical schema stays 13, no migration.

### Corrected Windows Development Build

Exact command, working directory C:/Dev/Dungeon-Lord/Temp/phase7a5-guidance-validation:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' build 'C:/Dev/Dungeon-Lord/Temp/phase7a5-guidance-validation' --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --allow-dirty-build --log-file 'C:/Dev/Dungeon-Lord/Temp/phase7a5-guidance-windows-development-build.log' --no-tail --timeout 1800 --no-color
```

Succeeded, CLI exit 0; Unity 6000.3.2f1, StandaloneWindows64, Development=true, only Assets/_Project/Scenes/Bootstrap.unity. The existing ProductionSpatialContentBuildPreprocessor runs the real production pre-build gate in BuildPlayer. Report: zero errors, one warning, 171,667,139 build bytes; retained complete folder including support/report files is 171,906,870 bytes.

Player: Builds/Phase7A5-Guidance-2026-10-07/Windows/Dungeon Lord.exe. Report: TestResults/phase7a5-guidance-windows-build-report.json. Log: Temp/phase7a5-guidance-windows-development-build.log. Final inspected captures: TestResults/phase7a5-guidance-screenshots/phase7a5-guidance-1080x1920.png, phase7a5-guidance-1920x1080.png and phase7a5-guidance-long-text.png. The original Windows player remains historical evidence.

The single build warning is unchanged: “Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.” The symbol uploader also logs unavailable Cloud Diagnostics credentials. Other diagnostics retained without suppression: licensing-client signature validation warning and access-token refresh failure followed by resolved entitlement/updated license; existing empty Assets/_Project/Tests 1/Tests 1.asmdef; Mono abort_threads and debugger port 3484 shutdown message. No C# compiler error or runtime theme warning was found.

Before build, the isolated fallback save filename and test-runner EditorBuildSettings/ShaderGraphSettings/ProjectSettings were restored exactly to reviewed baseline; packages/settings/build_config had no diff. Post-build Standalone batching/application-identifier ordering and UnityConnectSettings activation were inspected/restored only in the isolated checkout. All nine intentional source/assets still match qualification hashes. Owner settings/config/packages and unrelated TMP remain untouched. No build launch failed in this correction.

## Exact English copy changes

All copy remains in Assets/_Project/Data/Bootstrap/string_table_en.json. Existing stable structural reason codes retain their domain meaning; localized text is not draft authority.

| Key | Corrected English text |
|---|---|
| ui.dungeon.room_move_hint | Choose a highlighted diamond anchor for this room. The outlined grid is the legal floor area; required-route descendants move with the room. |
| ui.dungeon.room_move_none (new) | No valid alternative move location is available for this room in the current draft. |
| structural.edit.out_of_bounds | The moved structures would extend outside the outlined floor grid. Press Move and choose a highlighted diamond anchor. |
| structural.edit.room_overlap | The moved room would overlap another room. Press Move and choose a highlighted diamond anchor. |
| structural.edit.fixed_structure_overlap | The moved room would overlap a required structure. Press Move and choose a highlighted diamond anchor. |
| structural.edit.corridor_overlap | The moved room would overlap an existing corridor. Press Move and choose a highlighted diamond anchor. |
| structural.edit.connection_unavailable | This anchor cannot connect the room to the required route. Press Move and choose a highlighted diamond anchor. |
| structural.edit.required_route_disconnected | This placement would disconnect the required route. Press Move and choose a highlighted diamond anchor. |

## Exact changed files

Authority: Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralRenovationService.cs.

Production presentation: Assets/_Project/Scripts/UI/DungeonFloorWorldView.cs, DungeonPresentationPolicy.cs and ProductionDungeonController.cs; Assets/_Project/UI/ProductionDungeon/Presentation.asset; Assets/_Project/Data/Bootstrap/string_table_en.json.

Coverage: Assets/_Project/Tests/EditMode/PhaseSevenA5TransactionalRoomMovementTests.cs; Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenA4ProductionSceneTests.cs; Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs.

Evidence: Docs/testing/evidence/phase7a5-transactional-room-movement/owner-uat-floor-guidance-correction.md (new), final-owner-report.md, implementation.md, owner-uat.md, proposed-pr-description.md, qualification.md and test-and-localization-inventory.md. No UXML/USS/package/owner ProjectSettings changes, canonical schema remains 13, no migration, draft formats/records and A5 economic/persistence authorities unchanged.

## Owner comprehension recheck — do this first

Use the corrected reviewed PR commit and the new complete Windows player listed above. Use the prepared existing movable-room save from the original UAT; no reset or unrelated setup is required. This focused recheck has not been performed by the agent.

1. Enter Edit Mode.
2. Confirm the full legal floor grid is visible.
3. Select an existing movable room by tapping an empty tile in it.
4. Press Move.
5. Confirm hollow diamond destination anchors are immediately discoverable.
6. Confirm the outlined legal floor area is visually obvious.
7. Tap one highlighted diamond anchor.
8. Confirm movement succeeds and economics appear without spending draft mana.
9. Try one non-highlighted/out-of-bounds placement through Move.
10. Confirm the red attempted footprint and localized reason explain why it failed and how to correct it.
11. Press Move and correct it using a highlighted diamond anchor.
12. Confirm this flow feels understandable without guessing.

If this comprehension recheck fails, stop and report that finding before continuing the prior A5 UAT. If it passes, continue the existing economics/recovery/save/reopen/Windows checks in [owner UAT](owner-uat.md). Do not infer comprehension, fun or mobile/native qualification from automated tests/screenshots.

## Owner worktree preservation

The correction gate found the exact reviewed branch/HEAD, local main at `52c1eb00af241c4be9f8b117e96294a71dce82bb`, no staged changes, and only the unrelated TMP modification. Its SHA-256 remains `F53586850A70C4F308E7E71B99B63881A49F38DE4BD40F4EDB7FE7AE6ECF8A52`. The previously untracked Assets/_Project/Tests/ProductionDungeon.meta was already absent at this gate and was not recreated or otherwise modified by the correction. No owner resets, restores, stash, cleanup or settings/package edits were performed. Validation uses a fresh isolated checkout of reviewed HEAD with only correction source/assets copied in.
