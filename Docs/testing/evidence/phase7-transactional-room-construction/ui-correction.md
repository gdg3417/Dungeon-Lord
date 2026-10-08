# Construction comprehension correction — 2026-10-08

Recommended configuration: GPT-6.1 Sol / Medium, Standard, demonstrated presentation corrections within established authorities; fallback GPT-6 Sol / Medium. Started from PR #229 HEAD `08058572c5be130b39ee3ae1933ece2f93483c8b` on the existing branch. Corrected source commit: `482632bcf1162219ae882b44cacb12ca1640d89a`.

Owner Editor testing confirmed starter setup, authored Rooms selection, legal guidance, orientation changes, footprint preview and economic consequences. That partial evidence does not approve the revised comprehension gate or the whole UAT.

## Interaction changes

Confirm Placement / Keep Invalid Attempt and Cancel Placement occupy a fixed placement row outside every ScrollView and outside collapsed details. A divider and a separate current-draft quote identify the session Save Changes / Discard Draft controls. Placement can still be confirmed into an aspirational draft; insufficient mana blocks final Save, not confirmation.

The compact proposal shows the selected room, **complete hypothetical draft** cost and resulting mana, incoming authored connection/corridor tiles and downstream terminal relocation. The current-draft summary excludes the transient proposal until confirmation. Values come directly from the existing SaveService/StructuralEconomyService previews and balance refresh; the UI adds no arithmetic, price formula, resource reservation or economic authority. Detailed costs, refunds, capacities, consequences and explanatory feedback remain in the existing expandable scrolling area. Empty current drafts explicitly say there are no confirmed changes.

Construction details start collapsed and retain the player's collapse choice while tapping anchors. Authored orientation and terminal-side options remain outside details in one horizontal strip. Categories and placement actions stay visible. Long invalid reasons remain visible with highlighted footprints and correction controls. Room selection explicitly reopens the existing A4/A5 contextual sheet after construction, so Move remains discoverable.

Construction-only HUD/layout rules reclaim space; landscape uses the same contextual construction sheet on the right. Capacity is pinned outside the HUD scroll. Text settings move into the HUD area. All text still uses the configured Small/Default/Large font sizes and minimum hit regions; no gameplay tuning changed. This is capability-specific composition, not implementation of the broader UI vision. Starter Bootstrap dependency and all non-goals remain unchanged.

## Observed checks

| Check | Total | Passed | Failed | Skipped | Report |
|---|---:|---:|---:|---:|---|
| A4/A5/construction + initial corrected production scenes | 162 | 162 | 0 | 0 | `TestResults/room-construction-ui-regression-editmode.xml` |
| Final production EditMode scenes | 20 | 20 | 0 | 0 | `TestResults/room-construction-ui-final-scene-editmode.xml` |
| Final genuine production PlayMode/input | 22 | 22 | 0 | 0 | `TestResults/room-construction-ui-final-scene-playmode.xml` |

The 162-case run contains A4 transactional editor 37, A5 movement 52, construction 53 and production scenes 20. Later final scene runs cover the pinned capacity and construction-to-room-selection corrections. Tests verify pointer hit-testing of placement/session buttons with collapsed and scrolled details, safe areas, 720×1280 / 1280×720 / 1080×1920 / 1920×1080, all three text sizes, expanded Japanese-ready information and a long localized invalid reason. The viewport-space assertion remains intact. Actual Input System touches select an anchor and press Confirm; wallet ticks retain option-button identities. Tests compare displayed quotes to authoritative previews, preserve canonical bytes/mana during confirmation, and verify Save/recovery/discard and A4/A5 controls.

First focused run: 3 cases, 0 passed / 3 failed (empty-draft text, collapse expectation, insufficient viewport). Second: 3 cases, 2 passed / 1 viewport failure. Targeted layout attempt: 1 failed. These reports/logs remain retained; failures prompted bounded presentation corrections, not weaker assertions. [Report summary](ui-report-summary.json) records counts, hashes, durations and failure details. [Source manifest](ui-qualified-source-manifest.json) binds the six corrected files to the tested copy.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode EditMode --filter 'PhaseSevenA4ProductionScene;TransactionalRoomConstruction;PhaseSevenA4TransactionalEditor;PhaseSevenA5' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-ui-regression-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-ui-regression-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode EditMode --filter 'PhaseSevenA4ProductionScene' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-ui-final-scene-editmode.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-ui-final-scene-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-ui-final-scene-playmode.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-ui-final-scene-playmode.log'
```

Prior full-suite evidence remains historical: EditMode 1,600 / 1,599 passed / 0 failed / 1 skip; PlayMode **3,044 / 3,032 passed / 2 unchanged clipboard failures / 10 skips**. These full suites were not rerun or represented as passing for the correction. The owner's clipboard sequencing exception continues; no clipboard diagnostics, fake clipboard, assertion weakening or new skips were introduced. Existing domain/persistence/production-gate evidence remains applicable to unchanged authorities.

## Visual evidence and safety

Inspected [720×1280 Large](screenshots/ui-correction/construction-ui-corrected-large-720x1280.png), [1280×720 Large](screenshots/ui-correction/construction-ui-corrected-large-1280x720.png), [1080×1920 Large](screenshots/ui-correction/construction-ui-corrected-large-1080x1920.png), [1920×1080 Large](screenshots/ui-correction/construction-ui-corrected-large-1920x1080.png) and [long invalid reason](screenshots/ui-correction/construction-ui-invalid-long-720x1280.png). These are actual isolated production-scene captures, not art imports or owner approval.

Scene fixtures arm GUID save isolation before Bootstrap Start, verify the exact SaveService path, retain it through reload/shutdown and clean only their GUID prefix. Before the final scene runs the validation config/fallback were additionally switched to a fresh test-only namespace, then restored to the existing disposable UAT namespace for the manual player build. Owner primary and all existing disposable UAT save/draft hashes are recorded read-only before qualification at `TestResults/room-construction-ui-owner-hashes-before.json`. No owner save or draft is deleted, reset or mutated. Root ProjectSettings/TMP settings and unrelated local files are preserved. Schema 13, journal 4 / record 3 compatibility and final-state/atomic-save economics are unchanged.

At the time this UI correction evidence was recorded, renewed owner comprehension and standalone UAT, external review and owner acceptance of the temporary first-room limitation remained required. This was the status at that point; the later owner UAT and decision are recorded in the final closeout below.

## New isolated Windows Development Build

Built the corrected source `482632bcf1162219ae882b44cacb12ca1640d89a` after the affected checks passed, with existing Unity CLI `1.0.0-beta.10`, Editor `6000.3.2f1`, `DevelopmentBuildUtility.BuildWindowsDevelopment`, StandaloneWindows64, Development, Bootstrap-only. CLI exited 0. [Actual report](ui-windows-build-report.json): **Succeeded / 171,724,024 bytes / 0 errors / 1 warning**, timestamp `2026-10-08T15:21:02.7800147Z`.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' build 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --allow-dirty-build --log-file 'C:/Dev/Dungeon-Lord/Temp/room-construction-ui-windows-build.log' --no-tail --timeout 1800 --no-color
```

New player: `C:/Dev/Dungeon-Lord/Builds/Phase7RoomConstruction-UI-482632b-20261008/Windows/Dungeon Lord.exe` (667,648-byte executable, all adjacent files retained). [Artifact manifest](ui-windows-artifact-manifest.json) records paths, sizes and SHA256 hashes. The original previous build remains preserved at `Builds/Phase7RoomConstruction-2cd413b-20261008`; it is superseded for visual qualification. The new player has not been manually launched/tested. Redacted build log is retained under `Temp/room-construction-ui-windows-build.log` and beside the preserved Windows directory.

Exact warning: `Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.` The symbol uploader separately logs `Unable to upload symbols to Unity Cloud Diagnostics: Unity Cloud Diagnostics credentials unavailable. Please provide an auth token with USYM_UPLOAD_AUTH_TOKEN environment variable`. These external symbol-upload diagnostics do not change the successful build result. No credentials/security settings changed. The log also retains the existing empty `Assets/_Project/Tests 1/Tests 1.asmdef` assembly notice and shutdown `debugger-agent: Unable to listen on 3516`. No C# compiler warning or build-blocking error was observed.

The validation-only config and SaveService fallback use the existing disposable owner UAT filename `phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json`, allowing intentional manual Resume Draft against the preserved UAT evidence. Root production config/fallback remain unchanged. Ten protected owner primary/UAT save and draft files were verified byte-identical after tests/build; read-only post-build hashes are at `TestResults/room-construction-ui-owner-hashes-after-build.json`. Root ProjectSettings SHA256 remains `34DA6D701E4C4629CA7B1CECB638F801D9C5EA4F40D33B09FECA46777073B993`; TMP Settings blob remains `92a60536387caf4a8caaed785b4c07b48abdf201`. The validation ProjectSettings text matches root (encoding/line-ending differences only), so no unrelated settings were overwritten.

## Final owner UAT and status — 2026-10-08

The owner subsequently completed the agreed Editor and Windows standalone gameplay qualification and approved PR #229 for merge. See [owner-uat.md](owner-uat.md) for the owner-reported steps, evidence boundaries and explicit acceptance. This supersedes the earlier statements in this dated correction record that renewed UAT, standalone launch, external review and acceptance were pending at the time this record was written; those earlier statements are preserved as historical context.

The full PlayMode clipboard failures remain 2 failures, with the 10 established skips unchanged. The owner accepted the narrow exception for this PR. The fresh-game first-room Bootstrap dependency is also explicitly accepted for this PR and remains an outstanding onboarding capability. The corrected build warning remains one Unity Cloud symbol-upload warning; the build succeeded with zero errors.

The capability-specific production UI is accepted for this PR, while visual convergence with the production UI vision is deferred to a dedicated future composition assessment. Implementation and agreed qualification are complete, the owner approved merge, and the PR is not yet merged.

## Additional post-approval review correction — 2026-10-08

A newly opened review finding identified that persisted invalid construction footprints did not expand rendered `Bounds`, which could make them inaccessible after draft recovery. The focused `PresentConstructionIntents()` bounds correction and the new recovery/camera-input production-scene regression are recorded in [presentation-bounds-correction.md](presentation-bounds-correction.md). Final affected EditMode runs passed 87 Phase 7A4, 52 Phase 7A5 and 57 construction cases; the genuine production-shell PlayMode/input run passed 23/23. The replacement Windows Development Build succeeded with 0 errors and 1 Unity Cloud symbol-upload warning. The artifact is `Builds/Phase7RoomConstruction-BoundsFix-186ba37-20261008-final/Windows/Dungeon Lord.exe`.

This correction was required after the earlier October 8 merge approval. At the time this evidence was written, no manual owner UAT was claimed for it. The owner subsequently confirmed the final bounds-fix recovery and correction workflow passed in Windows standalone on October 8, 2026. The owner also accepted the observed temporary landscape contextual-panel inconsistency for PR #229 only; see [owner-uat.md](owner-uat.md). This does not revise the Phase 7A0 design lock or alter the accepted first-room Bootstrap limitation or clipboard exception.

The fixture was tightened so the disposable wallet is set to the exact authoritative quote before draft creation. It now verifies that confirmation, restart recovery and correction preserve that balance until Save. The final reports and replacement build details are in [presentation-bounds-correction.md](presentation-bounds-correction.md).
