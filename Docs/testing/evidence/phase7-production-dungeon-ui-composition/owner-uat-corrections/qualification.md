# Owner UAT correction qualification

Unity 6000.3.2f1, installed Unity CLI beta 10, Windows 11. Qualified implementation: `691720b20e8226da0f110e5b44442a9d41be8407`. Later documentation/evidence changes do not change the four qualified input trees. No owner manual UI or Windows retest is claimed.

## Results

| Run | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| Isolated strict-input helper regressions | 12 | 12 | 0 | 0 |
| Reviewed baseline runtime captures | 1 | 1 | 0 | 0 |
| Final imported-art / composition / connection EditMode | 9 | 9 | 0 | 0 |
| Final production shell genuine PlayMode/Input System | 32 | 32 | 0 | 0 |
| Full EditMode | 1,617 | **1,616** | **0** | **1** |
| Full PlayMode | 3,054 | **3,044** | **0** | **10** |

`qualification-reports.zip` contains all ten raw XML reports, including intermediate failures. `report-summary.json` preserves their counts, failure stacks, skip reasons and layout/input observations. `unity-logs-redacted.zip` contains thirteen logs (authentication arguments removed); `log-index.json` records hashes and compiler diagnostics. No C# compiler warnings/errors occur in the final suites/build. Expected negative-test, licensing/startup and shutdown diagnostics remain in the logs.

Compared with the preceding strict qualification, four passing EditMode methods and three passing PlayMode adapters were added. No cases were removed or changed outcome (`test-outcome-comparison.json`, including duplicate parameterized names). The unchanged clipboard copy tests both pass; no PR #229 exception is used. Existing skips remain: the inverse Windows platform fixture in EditMode; eight synchronous fixtures, the inverse platform fixture and the native Windows player filesystem fixture in PlayMode. Exact names/reasons are retained in the summary.

The full suites include production scene/input, imported asset mapping/fallback/alignment, construction and movement, validation/invalid intent recovery, transactional Save/Discard/economic publication, occupancy and run snapshots, plus production content/build gates. New scene checks measure sheets and critical safe controls in portrait/landscape at all text sizes, long room cards, Display access, wheel bounds/anchoring, touch exclusion and individual saved-connection room boundaries. Existing pan, pinch, Focus/Fit, chrome interception and draft recovery cases remain unchanged except integration with moved Display controls.

Intermediate runs 01/02: 5 passed, 1 failed (new landscape Normal height requirement). Run 03: 6 passed, 1 failed (Large Normal height). PlayMode 04: 30 passed, 2 failed (theme test inspected newly hidden text controls without opening Display; compact landscape Large map was 144 units against the existing >150 requirement). PlayMode 05: 31 passed, 1 failed (same viewport requirement). Content-sized summary/HUD allocation fixes and opening Display before its existing theme assertions correct these; no viewport threshold was lowered. Final PlayMode 06 passes all 32. Helper execution initially failed fixture setup/cleanup inside the Windows sandbox; the unchanged isolated fixtures pass all twelve outside it. Destructive fixture operations target their own temporary repositories, never owner project/save paths.

## Strict inputs and preservation

`Temp/ui-composition-uat-validation` was initially absent. The baseline and implementation use this disposable checkout and unique `phase7-ui-composition-validation-01a11d08.json` save namespace. The established P1 helpers are unchanged: the complete tracked plus intended nonignored untracked Assets, Packages, ProjectSettings and ContentAuthoring inventory is checked independently. Target-only source files, missing/changed inputs and unsafe/reparse targets fail closed; preparation performs no deletion. Library/generated outputs are outside source roots and cannot excuse an extra source input.

Every pre-launch checkpoint establishes **1,188 expected / 1,188 actual / zero mismatches / zero extras**. The only permitted input transformations are checked save namespace replacement in bootstrap config and SaveService, config JSON formatting and text newline/BOM normalization. All nineteen full intermediate/final inventories and output diffs are retained in `source-inventories-and-output-diffs.zip`; the small index describes each result. The final full `qualified-source-manifest.json` passes all 1,188 inputs after restoring disposable Unity serialization output.

The full EditMode post-check is exact. PlayMode changes the disposable TMP fallback's empty texture/material serialization and ProjectSettings identifier order. Build changes URP shader prefilters, global runtime references, ProjectSettings serialization/default Standalone batching and UnityConnect settings. These two/four mismatches were explicitly detected, recorded as diffs and restored with strict preparation; they were not accepted as source changes. Intermediate working-source changes are likewise reported in their inventories, not represented as final qualified equality.

`owner-preservation.json` verifies byte equality for the primary save, both draft records, primary ProjectSettings and TMP settings, and the identical three-file primary/draft prefix inventory. Schema 13, migrations, transaction journal/evidence, recovery, economics, occupancy, snapshots, content geometry and Bootstrap onboarding are unchanged.

## Windows Development Build

Succeeded: StandaloneWindows64, Development, only `Assets/_Project/Scenes/Bootstrap.unity`; **0 errors / 1 warning** (no Unity Cloud token for native-symbol upload). Build report size: 181,149,624 bytes. All **50 referenced graphical PNGs** occur in actual packed asset reports. See `build-report.json`, `build-messages.txt`, `packed-dungeon-art.txt` and the complete packed source list.

Complete adjacent player: `C:/Dev/Dungeon-Lord/Builds/Phase7UIComposition-UAT-691720b-20261009/Windows/`, **303 files / 181,619,281 bytes**. `build-artifact-manifest.json` records every file/hash; the retained copy matches the built source without missing/extra files. It was not launched for owner UAT.

## Reproducible commands

Commands run from `C:/Dev/Dungeon-Lord`; the Python executable is `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. PowerShell variables below abbreviate the verified paths only. Preparation and independent verification precede every Unity launch, including repeated focused runs; raw inventories record each checkpoint.

```powershell
$uatPython = 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$uatUnity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$uatProject = 'C:/Dev/Dungeon-Lord/Temp/ui-composition-uat-validation'
& $uatPython Tools/Presentation/test_qualification_inputs.py
& $uatPython Tools/Presentation/prepare_validation.py ui-composition-uat-validation
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation TestResults/ui-uat-focused-06-source.json
& $uatUnity test $uatProject --mode PlayMode --filter PhaseSevenA4ProductionShellPlayModeTests --output C:/Dev/Dungeon-Lord/TestResults/ui-uat-focused-06-playmode.xml --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-uat-focused-06-playmode.log
& $uatPython Tools/Presentation/prepare_validation.py ui-composition-uat-validation
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation TestResults/ui-uat-focused-final-edit-source.json
& $uatUnity test $uatProject --mode EditMode --filter 'DungeonVisualCatalogTests;CompositionVisualCheckpoint;UatCorridorVisualCheckpoint;UatRoomBoundariesRespectSavedConnectionsAndRotation' --output C:/Dev/Dungeon-Lord/TestResults/ui-uat-focused-final-editmode.xml --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-uat-focused-final-editmode.log
& $uatPython Tools/Presentation/prepare_validation.py ui-composition-uat-validation
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation TestResults/ui-uat-pre-full-edit-source.json
& $uatUnity test $uatProject --mode EditMode --output C:/Dev/Dungeon-Lord/TestResults/ui-uat-full-editmode.xml --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-uat-full-editmode.log
& $uatPython Tools/Presentation/prepare_validation.py ui-composition-uat-validation
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation TestResults/ui-uat-pre-full-play-source.json
& $uatUnity test $uatProject --mode PlayMode --output C:/Dev/Dungeon-Lord/TestResults/ui-uat-full-playmode.xml --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-uat-full-playmode.log
& $uatPython Tools/Presentation/prepare_validation.py ui-composition-uat-validation
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation TestResults/ui-uat-pre-build-source.json
& $uatUnity run $uatProject --timeout 1800 --no-color -- -buildTarget StandaloneWindows64 -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildWindows -logFile C:/Dev/Dungeon-Lord/Temp/ui-uat-windows-build.log
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation TestResults/ui-uat-post-build-source.json
& $uatPython Tools/Presentation/prepare_validation.py ui-composition-uat-validation
& $uatPython Tools/Presentation/verify_qualified_source.py ui-composition-uat-validation Docs/testing/evidence/phase7-production-dungeon-ui-composition/owner-uat-corrections/qualified-source-manifest.json
& $uatPython Tools/Presentation/collect_uat_evidence.py
```

Actual expanded Unity arguments, timestamps and intermediate filters are in their logs. The retained manifest/index supplies exact checkpoint names. No physical-device/FPS/allocation claim is made. Existing bounded presentation observations retain reconstruction/pool counts across focus/fit/sheet/resource cycles and floor switches.
