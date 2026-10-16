# Final correction qualification commands

Executed in `C:/Dev/Dungeon-Lord`; exact arguments and exit codes from the staged runner. Original C2 full-suite results remain separate.

## EditMode

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'test' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--mode' 'EditMode' '--filter' 'PhaseFourAcquisition;PhaseFourEconomy;ProductionSpatialContentBuildGateTests;CombatC2EditModeFixtures' '--output' 'C:\Dev\Dungeon-Lord\TestResults\combat-c2-reserve-ui-02-EditMode.xml' '--timeout' '1200' '--no-color' '--' '-logFile' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-reserve-ui-02-EditMode.log'
```

CLI exit: 0.

## PlayMode

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'test' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--mode' 'PlayMode' '--filter' 'CombatC2ReserveAffordabilityVisibleWithoutHover;RoomContentProductionParity;RoomContentUatMeasuredPlacementLayout;RoomContentCompactPlacementAndOverflow;CombatC2ConsecutiveRunsAndReadOnlyReplay' '--output' 'C:\Dev\Dungeon-Lord\TestResults\combat-c2-reserve-ui-03-PlayMode.xml' '--timeout' '1200' '--no-color' '--' '-logFile' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-reserve-ui-03-PlayMode.log'
```

CLI exit: 0.

## windows-build

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' 'build' 'C:\Dev\Dungeon-Lord\Temp\ui-composition-combat-c0-diagnostics-validation-r2' '--target' 'StandaloneWindows64' '--execute-method' 'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC2ReserveUiWindows' '--allow-dirty-build' '--timeout' '1800' '--no-tail' '--log-file' 'C:\Dev\Dungeon-Lord\Temp\combat-c2-reserve-ui-03-windows-build.log' '--no-color'
```

CLI exit: 0.
