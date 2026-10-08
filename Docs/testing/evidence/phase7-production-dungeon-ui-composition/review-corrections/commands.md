# Exact qualification commands

Python executable: `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.
Unity CLI: `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`, 1.0.0-beta.10; Editor 6000.3.2f1.
Unity launches require approved execution because sandbox licensing/hardware initialization fails.

Before **every** launch, source comparison must exit zero. Preparation also compares the complete input set and exits nonzero on stale inputs. No deletion is performed. Initial target `Temp/ui-composition-review-validation` did not exist; its first Unity launch performed a fresh import/Library creation.

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/test_qualification_inputs.py
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/prepare_validation.py ui-composition-review-validation
if ($LASTEXITCODE -ne 0) { throw 'Preparation failed' }
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/verify_qualified_source.py ui-composition-review-validation Docs/testing/evidence/phase7-production-dungeon-ui-composition/review-corrections/pre-unity-source.json
if ($LASTEXITCODE -ne 0) { throw 'Input equality failed' }
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --mode EditMode --filter 'PhaseSevenA4ProductionScene;DungeonVisualCatalogTests' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-review-focused-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-focused-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --mode EditMode --filter 'PhaseSevenA4ProductionScene;DungeonVisualCatalogTests' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-review-focused-final-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-focused-final-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --mode EditMode --filter 'CompositionFloorLayoutSummary;CompositionNormalInspection;CompositionLayoutsAndPresentation' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-review-corrected-focused-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-corrected-focused-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-review-focused-playmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-focused-playmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --mode EditMode --output 'C:/Dev/Dungeon-Lord/TestResults/ui-review-full-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-full-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --mode PlayMode --output 'C:/Dev/Dungeon-Lord/TestResults/ui-review-full-playmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-full-playmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' run 'C:/Dev/Dungeon-Lord/Temp/ui-composition-review-validation' --timeout 1800 --no-color -- -buildTarget StandaloneWindows64 -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildWindows -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-review-windows-build.log'
```

The second focused EditMode filename contains `final` but is an intermediate failed fixture run, retained unchanged. Follow the report index and final full-suite results. Comparison manifests separately record each pre-launch state and each detected serialization drift. Preparation was rerun after documented drift and final source changes; no Unity launch proceeded after a failed comparison. XML duration resets across EditMode scene/domain reloads and is not wall-clock qualification time.
