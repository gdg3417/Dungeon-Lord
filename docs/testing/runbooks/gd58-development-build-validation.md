# GD58 Development Build Validation Runbook

Use this runbook to validate that the Editor-validated MVP journey starts and operates in built Unity players without adding production release scope.

> Workspace note: keep Unity build workspaces on fully local storage. Do not place generated `Library/` or PackageCache files in OneDrive Files On-Demand, iCloud placeholder storage, or equivalent cloud-placeholder folders; clone outside those locations before running builds.

## Build creation: Windows development player

1. Start from a clean repository and confirm no build outputs are tracked.
2. Open the project with the Unity version recorded in `ProjectSettings/ProjectVersion.txt`.
3. Run the menu item **Dungeon Lord > Build > Windows 64-bit Development Build** or batch mode:
   ```bash
   Unity -batchmode -quit -projectPath . -executeMethod DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment
   ```
4. Confirm the output is under `Builds/Development/Windows/`.
5. Confirm `Builds/Development/Windows/build-report.json` records UTC timestamp, Unity version, target, app version, development-build status, scenes, output path, result, size, errors, and warnings.
6. If the build fails, capture the Unity log and the generated build report. Do not claim build success.

## Windows development build manual validation

1. Launch `Builds/Development/Windows/Dungeon Lord.exe` outside Unity.
2. Confirm Bootstrap is the startup scene.
3. Confirm no SampleScene content appears.
4. Confirm player-facing text contains no raw localization keys or internal IDs.
5. Confirm exactly one authoritative Next instruction is visible.
6. Build the starter dungeon.
7. Run or observe the dungeon.
8. Complete the First Dungeon Contract requirements.
9. Test the greedier setup.
10. Apply a heat-control or safer adjustment.
11. Run again and verify the result changes coherently.
12. Use development diagnostics and smoke-copy controls in the development player.
13. Close the executable normally.
14. Relaunch and verify state persistence.
15. Record `Application.persistentDataPath` or the actual save-file path from logs or development diagnostics.
16. Inspect `Player.log` for unhandled exceptions, missing assets, missing scripts, or localization failures.
17. Validate 1280×720, 960×540, and 720×1280.
18. Resize dynamically when platform settings permit it and record clipping or overlap.

## Build creation: Android development APK

1. Run the menu item **Dungeon Lord > Build > Android Development APK** or batch mode:
   ```bash
   Unity -batchmode -quit -projectPath . -executeMethod DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildAndroidDevelopmentApk
   ```
2. Confirm the APK output is under `Builds/Development/Android/`.
3. Confirm `Builds/Development/Android/build-report.json` records the required build-report fields.
4. If Android build support, SDK, NDK, JDK, Gradle, adb, or platform tooling is missing, record the exact Unity error and do not claim success.

## Android physical-device validation

1. Install the APK on a physical Android device when available.
2. Confirm cold startup.
3. Confirm Bootstrap is the startup scene.
4. Confirm every required player action can be operated by touch.
5. Confirm scrolling works.
6. Check portrait and landscape behavior allowed by current settings.
7. Check display cutouts, rounded corners, navigation bars, and safe areas.
8. Check whether controls are large enough to use reliably.
9. Build and run the core MVP journey.
10. Background the application for at least 30 seconds and resume it.
11. Confirm no duplicated runs, loot, research completion, or objectives.
12. Force-stop the application and relaunch it.
13. Confirm saved state survives.
14. Inspect logcat for exceptions, missing assets, serialization errors, or IL2CPP failures.
15. Record the actual persistent save location when accessible.
16. Record approximate responsiveness, frame pacing, device heat, and battery concerns.

## Release-control validation

Create or run a non-development build configuration sufficient to verify the release boundary. A full production release package is not required.

1. Confirm developer panel access is unavailable.
2. Confirm developer mutation controls are unavailable.
3. Confirm diagnostic-only keyboard shortcuts do nothing.
4. Confirm full internal smoke output cannot be exposed.
5. Confirm normal player actions still work, including placing structures and running or observing the dungeon.

## Evidence capture

For each platform or blocker, capture:

- Git commit hash and Unity version.
- Build command or menu path used.
- Build output path and build-report path.
- Build result, error count, and warning count.
- Player log path or logcat command used.
- Device model, OS version, orientation, resolution, and input method.
- Screenshots or short clips for startup scene, MVP completion, diagnostics availability in development builds, and release-control absence in non-development builds.
- Exact blockers for any build or device validation step that could not be completed.
