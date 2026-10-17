# Exact Unity invocations

All commands target the primary project, sequentially, under PowerShell. The first attempt ran sandboxed; subsequent attempts used approved escalation for Unity licensing. No new Unity project or alternate validation checkout was used.

## EditMode 01

Infrastructure blocked: sandbox licensing hardware access; cancelled. CLI exit 1.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'EditMode' --filter 'PhaseSevenFloorLifecycleTests;PhaseSixA2;PhaseSixA3;PhaseSixA4;ProductionSpatialContentBuildGateTests' --output 'TestResults/phase7-floor-lifecycle-editmode-01.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/phase7-floor-lifecycle-editmode-01.log'
```

## EditMode 02

Compilation failed: test-only SaveData.schemaVersion assertion. CLI exit 1.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'EditMode' --filter 'PhaseSevenFloorLifecycleTests;PhaseSixA2;PhaseSixA3;PhaseSixA4;ProductionSpatialContentBuildGateTests' --output 'TestResults/phase7-floor-lifecycle-editmode-02.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/phase7-floor-lifecycle-editmode-02.log'
```

## EditMode 03

Passed. CLI exit 0.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'EditMode' --filter 'PhaseSevenFloorLifecycleTests;PhaseSixA2;PhaseSixA3;PhaseSixA4;ProductionSpatialContentBuildGateTests' --output 'TestResults/phase7-floor-lifecycle-editmode-03.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/phase7-floor-lifecycle-editmode-03.log'
```

## PlayMode 01

Failed: normal large-text landscape height and unset inline modal style assertion. CLI exit 1.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'PlayMode' --filter 'GraphicalFloors;UatContentHeight;OverlayFullWidth;CombatC2ReserveAffordabilityVisibleWithoutHover' --output 'TestResults/phase7-floor-lifecycle-playmode-01.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/phase7-floor-lifecycle-playmode-01.log'
```

## PlayMode 02

No tests: batch scene-backup recovery dialog; cancelled. CLI exit 1.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'PlayMode' --filter 'GraphicalFloors;UatContentHeight;OverlayFullWidth;CombatC2ReserveAffordabilityVisibleWithoutHover' --output 'TestResults/phase7-floor-lifecycle-playmode-02.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-lifecycle/playmode-02.log'
```

## PlayMode 03

No tests: recovery dialog also triggers on empty backup folder; cancelled. CLI exit 1.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'PlayMode' --filter 'GraphicalFloors;UatContentHeight;OverlayFullWidth;CombatC2ReserveAffordabilityVisibleWithoutHover' --output 'TestResults/phase7-floor-lifecycle-playmode-03.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-lifecycle/playmode-03.log'
```

## PlayMode 04

Passed. CLI exit 0.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'PlayMode' --filter 'GraphicalFloors;UatContentHeight;OverlayFullWidth;CombatC2ReserveAffordabilityVisibleWithoutHover' --output 'TestResults/phase7-floor-lifecycle-playmode-04.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-lifecycle/playmode-04.log'
```

## EditMode 04

Passed. CLI exit 0.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'EditMode' --filter 'PhaseSevenFloorLifecycleTests' --output 'TestResults/phase7-floor-lifecycle-editmode-04.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-lifecycle/editmode-04.log'
```

## PlayMode 05

Passed. CLI exit 0.

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode 'PlayMode' --filter 'GraphicalFloors' --output 'TestResults/phase7-floor-lifecycle-playmode-05.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Logs/phase7-floor-lifecycle/playmode-05.log'
```

EditMode 01 and PlayMode 02/03 were cancelled with Ctrl+C after their reported infrastructure blockage; no test verdict exists for them. EditMode 02 compiled no tests. Raw initial Temp logs were cleared by Unity startup; available later logs are retained locally under Logs/phase7-floor-lifecycle. PlayMode 01 was relocated there before subsequent launches. Raw logs are not published because Unity includes authentication arguments.

The five actual NUnit XML reports are preserved in qualification-reports.zip. Local originals remain in TestResults with the names above. Final-stage source applicability and the temporary save-namespace substitution are documented in implementation-and-qualification.md and tested-inputs.json. No standalone build was run.
