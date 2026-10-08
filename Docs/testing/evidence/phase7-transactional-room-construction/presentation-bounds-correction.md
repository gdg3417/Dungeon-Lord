# Invalid construction presentation bounds correction — 2026-10-08

## Finding and correction

After the earlier PR #229 merge approval, review identified that `PresentConstructionIntents()` painted durable invalid construction footprints without extending `DungeonFloorWorldView.Bounds`. `ProductionDungeonController.RebuildFloor()` configures `DungeonViewport` from those bounds after rendering the legal floor grid and both invalid-intent layers. An out-of-bounds confirmed construction could therefore be clipped or unreachable after recovery.

The correction updates only presentation bounds while painting unique footprint cells, in ordinal intent-ID order. It uses the existing bounded `Footprint()` resolver and the same cell-to-`Rect` expansion used by `PresentRoomIntents()`. `LegalBounds`, its grid and boundary, committed geometry, validation, draft persistence, and economics are unchanged.

The new production-scene regression finds an out-of-bounds construction preview from an anchor inside the configured legal floor, keeps it into the acknowledged draft, restarts the production controller, resumes the draft, and checks that every footprint tile renders and `Bounds` contains it while `LegalBounds` and the legal grid remain unchanged. An actual Input System touch reaches the invalid cell through the camera viewport, selects the intent, corrects it to a legal guided anchor, and saves it. Canonical save bytes and mana remain unchanged through confirmation, recovery and correction, until Save. Existing invalid room-movement bounds and ordinary construction guidance regressions run in the same focused suite.

## Observed qualification

| Check | Total | Passed | Failed | Skipped | Report |
|---|---:|---:|---:|---:|---|
| Focused Phase 7A4 EditMode regressions (including production scene) | 87 | 87 | 0 | 0 | `TestResults/room-construction-bounds-final2-a4.xml` |
| Phase 7A5 room movement EditMode regressions | 52 | 52 | 0 | 0 | `TestResults/room-construction-bounds-final2-a5.xml` |
| Transactional room construction EditMode regressions | 57 | 57 | 0 | 0 | `TestResults/room-construction-bounds-final2-construction.xml` |
| Genuine production-shell PlayMode/input | 23 | 23 | 0 | 0 | `TestResults/room-construction-bounds-final2-playmode.xml` |

The first two EditMode attempts and earlier 163-case/23-case passing reports are retained as historical evidence. The first attempt exposed a test baseline captured before entering Edit Mode; the second exposed an insufficient test wallet at the Save assertion. The final regression seeds the disposable runtime to the authoritative quote before the draft begins, then proves draft confirmation, recovery and correction preserve that balance until Save. Historical full PlayMode qualification remains 3,032 passed / 2 existing clipboard failures / 10 established skips; no clipboard tests were run or changed for this correction.

The final report logs include Unity's existing licensing-client validation warning in this environment, but both final NUnit reports contain complete passing results. The focused report counts, rather than the editor process exit message, are the test verdict.

## Windows Development Build

The existing `DevelopmentBuildUtility.BuildWindowsDevelopment` pipeline was rerun after the final focused tests from the isolated project with `StandaloneWindows64`, Development Build, Bootstrap-only scene, **0 errors and 1 warning**. The warning was Unity Cloud native-symbol upload unavailable because the access token is empty; no C# compiler warning occurred.

- Player: `Builds/Phase7RoomConstruction-BoundsFix-186ba37-20261008-final/Windows/Dungeon Lord.exe`
- Build report: `Builds/Phase7RoomConstruction-BoundsFix-186ba37-20261008-final/Windows/build-report.json`
- Reported build size: 171,736,960 bytes.
- Build log: `Temp/room-construction-bounds-final2-windows-build.log`.

## Replacement player save-path verification — 2026-10-08

Inspected the exact checkout used by the build and matched its executable to the delivered player. Bootstrap serializes a reference to `Assets/_Project/Data/Bootstrap/build_config.json` (asset GUID `d9b02ea1f0aeab24dae23698b794d61c`); this validation-copy asset explicitly sets `save.fileName` to `phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json` (SHA-256 `22E2381420C7EEE98D3ADEAAF328443AF5C2B514145870056376CBCDF958F865`). `GameRoot` passes the loaded `BuildConfig.save` to `SaveService`. The fallback in the same isolated copy of `SaveService.cs` is that same UAT filename (source SHA-256 `86CC373ADB487D3C1AF27FD38E769B249906EE918C6EF436822DA853407B8762`).

The built Windows player identity is `gdg3417` / `Dungeon Lord` with Standalone application identifier `com.gdg3417.dungeonlord` in the isolated checkout. Under the current Windows account its effective canonical save path is `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord/phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json`. Draft and recovery records are rooted at that same filename plus `.editor-draft`; they do not use `save_primary.json` or its draft prefix. The production repository still has `save_primary.json` in its build config and the `save_primary.json` `SaveService` fallback; neither production file was changed.

The final `BuildWindowsDevelopment` log names the isolated checkout as `-projectPath`, uses `DevelopmentBuildUtility.BuildWindowsDevelopment`, and records a successful Bootstrap-only Windows Development Build. The report is timestamped `2026-10-08T17:19:30.4072905Z`. SHA-256 of both the isolated build output and delivered `Dungeon Lord.exe` is `ABC8179E345B70C9D7CA423C54E02739ED9B9ADCA668C2C745FFB81AADC05CC6`. The player was not launched during this inspection. No rebuild was needed; the owner can use the existing disposable UAT namespace without resetting its progress.

The recorded before/after owner save and draft hash manifests match. This verification changed no Unity configuration, save file, or gameplay code.

## Isolation and owner data

The passing qualification used `C:/Dev/Dungeon-Lord/Temp/room-construction-validation-bounds-186ba37`, a fresh disposable project containing the validated `Assets`, `Packages` and `ProjectSettings`. The final passing EditMode and PlayMode runs used the unique clone identity `CodexRoomConstructionValidation186ba37/RoomConstruction`; the build used canonical identity within that clone. The scene fixture installs `phase7a4-scene-<guid>.json` before `SaveService` starts, asserts the actual path, and cleans only that GUID prefix. Two earlier failed EditMode fixture attempts used the default product identity and Unity wrote its supplemental `TestResults.xml` under LocalLow; that report matches the retained failed NUnit XML in the workspace. An initial sandboxed Unity launch against the earlier validation checkout exited before results because licensing initialization failed; an elevated retry reported that checkout already open by another Editor, which we left untouched. No Unity run targeted the owner project.

The canonical `save_primary.json` and its recorded draft sidecars match the prior hash evidence. The disposable owner UAT save's current file timestamp predates these runs; it differs from the older qualification hash snapshot and its earlier draft-chain files were already absent. The tests did not target that filename. The supplemental default `TestResults.xml` from the failed fixture rerun matches the retained NUnit XML report in the workspace; the final passing test runs used the unique clone identity. Root project settings and unrelated files remain unchanged.

## Scope

Changed runtime/test files for the correction were limited to `DungeonFloorWorldView.cs`, `PhaseSevenA4ProductionSceneTests.cs`, and `PhaseSevenA4ProductionShellPlayModeTests.cs`. At the time this evidence was first recorded, no owner UAT of the additional fix was claimed. The owner subsequently confirmed that the final bounds-fix recovery test passed in Windows standalone on October 8, 2026; the accepted result and temporary landscape-panel limitation are recorded in [owner-uat.md](owner-uat.md). The earlier first-room Bootstrap and clipboard acceptances remain unchanged.
