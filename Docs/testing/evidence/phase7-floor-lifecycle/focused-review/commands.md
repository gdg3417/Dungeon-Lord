# Focused review — exact executed commands

PowerShell, Unity 6000.3.2f1, primary project only, sequentially. All four invocations exited 0. No unrelated full suite or Windows build was run.

Baseline probe on reviewed production code, with test-only instrumentation added (1/1):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode PlayMode --filter 'GraphicalFloorReviewTickProbe' --output 'TestResults/phase7-floor-review-baseline-01.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-review/baseline-01.log'
```

Binding, existing lifecycle/Bootstrap, localization/UXML and production content gates (154/154):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --filter 'PhaseSevenFloorLifecycleTests;PhaseSixA4;ProductionSpatialContentBuildGateTests' --output 'TestResults/phase7-floor-review-editmode-01.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-review/editmode-01.log'
```

Original graphical floor integration and three focused regression scenarios (7/7):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode PlayMode --filter 'GraphicalFloors;GraphicalFloorReview' --output 'TestResults/phase7-floor-review-playmode-01.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-review/playmode-01.log'
```

Only the two affected scenarios after the final resource-blocker reason correction (2/2):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode PlayMode --filter 'GraphicalFloorReviewMeaningfulInvalidations;GraphicalFloorsConstructionRequirementsBlockersRefreshAndReopen' --output 'TestResults/phase7-floor-review-playmode-02.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-review/playmode-02.log'
```

[Manifest](qualification-manifest.json) contains every exact executed test name, count and measurement. [Archive](qualification-reports.zip) preserves all four actual reports; originals remain at the listed TestResults paths. Logs remain at the listed local paths and are not published because Unity emits authentication arguments. See [findings and applicability](review-and-validation.md) for the distinction between baseline instrumentation, the 154/7 runs and the final 2-test correction.
