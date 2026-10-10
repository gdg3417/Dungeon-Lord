# Floor HUD strict disposable qualification

Qualified Unity/content implementation: `b85daa637cf500f66bf3cbd50b5782f979046e51`. Four frozen input trees are in `qualified-input-trees.json`. Reviewed start/main and exact source changes are in [README](README.md). Evidence-only commits preserve those four trees. Existing PR #230 continued; no merge.

## Workflow and commands

`Temp/ui-composition-floorhud-validation` was initially absent, with a fresh Library/import. Unchanged strict helpers inventory **all 1,196 inputs** under Assets/Packages/ProjectSettings/ContentAuthoring, tracked plus intended nonignored untracked files and metadata. Missing/changed/target-only inputs, unsafe traversal and symlink/reparse redirection fail closed. Preparation performs no deletion and targets only the explicit disposable directory. Generated Library/Logs/Builds/TestResults are outputs outside input roots. Checked transformations remain the isolated `phase7-ui-composition-validation-01a11d08.json` filename in SaveService/config, config JSON formatting and text BOM/newline normalization. Owner primary save and sidecars cannot be fallback fixture paths.

PowerShell from `C:/Dev/Dungeon-Lord`; bundled Python, installed Unity CLI beta 10, Editor **6000.3.2f1**. Every launch has a separate complete inventory checkpoint. XML/log names below resolve to root TestResults/Temp. Editor authentication arguments alone are redacted in the shared log archive.

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py -v
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/prepare_validation.py ui-composition-floorhud-validation
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/verify_qualified_source.py ui-composition-floorhud-validation TestResults/ui-floorhud-pre-full-edit-source.json
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation' --mode EditMode --filter 'FloorHudStatesAndInputCapture' --output 'TestResults/ui-floorhud-baseline-02.xml' --timeout 1200 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-floorhud-baseline-02.log
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation' --mode EditMode --filter 'FloorHudStatesAndInputCapture;OverlayFullWidthSafeAreasAndTransparentInput;CompositionNormalInspectionFocusRailAndCollapseAreReadOnly' --output 'TestResults/ui-floorhud-focused-editmode.xml' --timeout 1200 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-floorhud-focused-editmode.log
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests' --output 'TestResults/ui-floorhud-focused-playmode.xml' --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-floorhud-focused-playmode.log
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation' --mode EditMode --output 'TestResults/ui-floorhud-full-editmode.xml' --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-floorhud-full-editmode.log
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation' --mode PlayMode --output 'TestResults/ui-floorhud-full-playmode.xml' --timeout 1800 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/ui-floorhud-full-playmode.log
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' run 'C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation' --timeout 1800 --no-color -- -buildTarget StandaloneWindows64 -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildWindows -logFile C:/Dev/Dungeon-Lord/Temp/ui-floorhud-windows-build.log
```

Baseline executes the added capture-only fixture before any production change; its reviewed runtime/USS inputs are inventoried and compare exactly after capture. The only addition at that point is test code, not a claimed unchanged full Git tree. Focused EditMode subsequently uses the corrected USS; the frozen implementation strengthens its background assertion to reject every Background image kind and adds the PlayMode adapter. Focused PlayMode and full suites/build consume the frozen trees.

## Observed runs and retained failures

| Run | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Isolated helper regressions outside sandbox | 12 | 0 | 0 |
| Successful reviewed-style baseline fixture | 1 | 0 | 0 |
| Focused presentation EditMode | 3 | 0 | 0 |
| Frozen genuine production PlayMode/Input System | 35 | 0 | 0 |
| Full EditMode | 1,620 | 0 | 1 |
| Full PlayMode | 3,047 | 0 | 10 |

Raw XML attributes, failures/skips, scene layout/alpha/hit observations and case-by-case previous outcome comparisons are retained in `qualification-reports.zip`, `report-summary.json` and `test-outcome-comparison.json`. Existing tests are not removed, skipped or weakened. The new fixture measures 720×1280 and 1920×1080 across Small/Default/Large, simulated safe insets, expanded/collapsed, active/inactive selection, edited/blocked text, opaque foreground and translucent image-free surfaces. Actual Input System touch/click floor controls and no navigation-induced draft/canonical/mana change are asserted. Retained tests cover full-width transparent padding pan/zoom, chrome exclusion, camera/Focus/Fit, construction/movement, invalid durability/recovery and publication.

Full suite totals are 1,621 EditMode and 3,057 PlayMode. Both unchanged clipboard tests pass: `CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition` and `DiagnosticsGate_BlocksAndPermitsOverlayDiagnosticsAndSmokeCopy`. No clipboard exception is applied. Full EditMode/PlayMode CLI processes exited 0.

Compared with reviewed `4d3ed8b` qualification: one added passing EditMode fixture and one added passing PlayMode adapter, **no removed cases or changed outcomes**. Duplicate parameterized test names are retained as lists in the comparison. Final full suites and build have no C# compiler warnings/errors.

The first helper attempt reports 12 tests with 24 fixture errors due to Windows sandbox temporary-directory access/cleanup denial. It is retained as `ui-floorhud-helper-tests.log`, not called passing; the same isolated helper command outside that sandbox passes 12 in `ui-floorhud-helper-tests-unsandboxed.log`. No owner path is a helper target.

First baseline Unity fixture: **0 passed / 1 failed**, expected simulated safe width 680 but received real width 720. Test applied the inset before floor selection; selection's existing real-safe-area layout superseded it. Moving only the test inset setup after selection fixes the fixture. No production layout/camera change or tolerance reduction. Successful second baseline: 1 passed and 30 real captures. Both reports/logs remain.

Post-focused PlayMode rejects two disposable serialization outputs (TMP fallback and ProjectSettings). Complete diffs are retained. Strict preparation restores intended clone inputs; pre-full EditMode again reports **1,196 exact inputs, zero failures/target-only files**. Output modifications are never claimed as source equality or committed to owner source.

Post-full EditMode verifies exact equality. Post-full PlayMode again rejects TMP fallback and ProjectSettings serialization; both diffs are retained before restoring only disposable inputs. The pre-build inventory verifies all **1,196 inputs, zero failures/target-only files**.

## Windows build and final inputs

**Succeeded**, StandaloneWindows64 Development, Bootstrap entry scene only; **0 errors / 1 warning**. Warning: Unity services native-symbol upload has an empty URI, retained verbatim in `build-messages.txt`. Build report size **181,301,611 bytes**. Production prebuild/content gates ran through the existing build utility. All **52 referenced art PNGs**, including both doorway/corridor thresholds, are present in packed source assets.

Complete adjacent player retained at `C:/Dev/Dungeon-Lord/Builds/Phase7FloorHud-b85daa6-20261009/Windows/`: **303 files / 181,772,726 bytes**, complete-copy/file-hash verified against the disposable build. Exact report, packed lists and file hashes are retained here. Build CLI exit 0; shutdown `abort_threads` / debugger-listen diagnostics remain in the log archive, not suppressed.

Post-build comparison rejects four serialized output changes: UniversalRP, URP global settings, ProjectSettings and UnityConnectSettings. Complete diffs are archived before strict preparation restores clone inputs. Final comparison again reports **1,196 expected = actual, zero mismatches and zero target-only files**. No generated setting is silently accepted as source. Owner primary save, both draft sidecars, primary ProjectSettings and TMP settings are all byte-identical, with unchanged three-file primary-save prefix inventory (`owner-preservation.json`). Evidence-only final commits preserve all four frozen input trees.

## Integrity and limitations

Owner-file hashes and primary-save prefix inventory are rechecked by the evidence collector; four input trees are rechecked at final HEAD. Schema 13, migration/draft/save protocols, canonical geometry/content/economics/simulation and camera authority remain unchanged. No new graphical assets or gameplay capabilities.

Translucent interactive controls still intercept input within their minimum hit regions. Physical-device readability/performance/font coverage and owner Windows UAT remain pending; desktop screenshots do not prove them. No FPS or allocation claims. External review and explicit owner approval are required before merge.
