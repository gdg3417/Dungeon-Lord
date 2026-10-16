param(
    [ValidateSet('tests','play','build')][string]$Stage = 'tests',
    [ValidatePattern('^[a-z0-9-]+$')][string]$Attempt = '01'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $taskRoot 'Temp/ui-composition-combat-c0-diagnostics-validation-r2'
$unity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$python = 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$evidence = Join-Path $taskRoot 'TestResults/combat-c2-reserve-ui'
Set-Location -LiteralPath $taskRoot
$runs = @()
function Invoke-Stage([string]$Name, [string[]]$Arguments, [string]$Xml) {
    $record = [ordered]@{ name=$Name; executable=$unity; arguments=$Arguments; project=$project; startedUtc=[DateTime]::UtcNow.ToString('O') }
    & $unity @Arguments
    $record.exitCode = $LASTEXITCODE
    $record.finishedUtc = [DateTime]::UtcNow.ToString('O')
    if ($Xml -and (Test-Path -LiteralPath $Xml)) {
        [xml]$report = Get-Content -Raw -LiteralPath $Xml
        $record.xml = $Xml
        foreach ($key in @('total','passed','failed','skipped')) { $record[$key] = [int]$report.'test-run'.$key }
    }
    $script:runs += $record
    $script:runs | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence ('commands-'+$Attempt+'-'+$Stage+'.json')) -Encoding utf8
    if ($record.exitCode -ne 0 -or ($Xml -and ($record.total -le 0 -or $record.failed -ne 0))) { throw "Stage $Name failed" }
    & $python Tools/Presentation/qualify_combat_c2_reserve_ui.py checkpoint ($Attempt+'-'+$Name)
    if ($LASTEXITCODE -ne 0) { throw 'Exact source checkpoint failed' }
}
& $python Tools/Presentation/qualify_combat_c2_reserve_ui.py check
if ($LASTEXITCODE -ne 0) { throw 'Qualified inputs must match source' }
if ($Stage -in @('tests','play')) {
    $modes = if ($Stage -eq 'play') { @('PlayMode') } else { @('EditMode','PlayMode') }
    foreach ($mode in $modes) {
        $filter = if ($mode -eq 'EditMode') { 'PhaseFourAcquisition;PhaseFourEconomy;ProductionSpatialContentBuildGateTests;CombatC2EditModeFixtures' } else {
            'CombatC2ReserveAffordabilityVisibleWithoutHover;RoomContentProductionParity;RoomContentUatMeasuredPlacementLayout;RoomContentCompactPlacementAndOverflow;CombatC2ConsecutiveRunsAndReadOnlyReplay' }
        $xml = Join-Path $taskRoot ('TestResults/combat-c2-reserve-ui-'+$Attempt+'-'+$mode+'.xml')
        $log = Join-Path $taskRoot ('Temp/combat-c2-reserve-ui-'+$Attempt+'-'+$mode+'.log')
        if ((Test-Path -LiteralPath $xml) -or (Test-Path -LiteralPath $log)) { throw 'Preserve previous evidence; use another attempt' }
        Invoke-Stage $mode @('test',$project,'--mode',$mode,'--filter',$filter,'--output',$xml,'--timeout','1200','--no-color','--','-logFile',$log) $xml
    }
} else {
    $player = Join-Path $project 'Builds/Development/Windows-combat-c2-reserve-ui-20261016/Dungeon Lord.exe'
    if (Test-Path -LiteralPath $player) { throw 'Preserve existing player' }
    $log = Join-Path $taskRoot ('Temp/combat-c2-reserve-ui-'+$Attempt+'-windows-build.log')
    Invoke-Stage 'windows-build' @('build',$project,'--target','StandaloneWindows64','--execute-method',
        'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC2ReserveUiWindows','--allow-dirty-build',
        '--timeout','1800','--no-tail','--log-file',$log,'--no-color') ''
    if (-not (Test-Path -LiteralPath $player)) { throw 'No Windows executable produced' }
}
