param(
    [ValidateSet('all','focused','remaining','play-build','full-edit','full-play','build')][string]$Stage = 'all',
    [ValidatePattern('^[a-z0-9-]+$')][string]$Attempt = 'final-01',
    [string]$FocusedEditFilter = 'PhaseFour;PhaseSixA;LivingDungeonPhaseB2;CombatC2;CombatC0Integration;CombatC1Replay',
    [string]$FocusedPlayFilter = 'CombatC2;CombatC1'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $taskRoot 'Temp/ui-composition-combat-c0-diagnostics-validation-r2'
$unity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$python = 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$evidence = Join-Path $taskRoot 'TestResults/combat-c2'
Set-Location -LiteralPath $taskRoot
$runs = @()
function Invoke-QualifiedStage([string]$Name, [string[]]$Arguments, [string]$Xml) {
    $record = [ordered]@{ name=$Name; executable=$unity; arguments=$Arguments; project=$project; startedUtc=[DateTime]::UtcNow.ToString('O') }
    & $unity @Arguments
    $code = $LASTEXITCODE
    $record.exitCode = $code
    $record.finishedUtc = [DateTime]::UtcNow.ToString('O')
    if ($Xml -and (Test-Path -LiteralPath $Xml)) {
        [xml]$report = Get-Content -Raw -LiteralPath $Xml
        $record.xml = $Xml
        $record.total = [int]$report.'test-run'.total
        $record.passed = [int]$report.'test-run'.passed
        $record.failed = [int]$report.'test-run'.failed
        $record.skipped = [int]$report.'test-run'.skipped
    }
    $script:runs += $record
    $script:runs | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidence ('commands-'+$Attempt+'.json')) -Encoding utf8
    if ($code -ne 0) { throw "Qualification stage $Name failed with CLI exit $code" }
    if ($Xml -and ($record.total -le 0 -or $record.failed -ne 0)) { throw "Qualification stage $Name has no successful test verdict" }
    & $python Tools/Presentation/qualify_combat_c2.py checkpoint ($Attempt+'-'+$Name)
    if ($LASTEXITCODE -ne 0) { throw "Input checkpoint failed after $Name" }
}
function Invoke-Tests([string]$Name, [string]$Mode, [string]$Filter) {
    $xml = Join-Path $taskRoot ('TestResults/combat-c2-'+$Attempt+'-'+$Name+'.xml')
    $log = Join-Path $taskRoot ('Temp/combat-c2-'+$Attempt+'-'+$Name+'.log')
    if ((Test-Path -LiteralPath $xml) -or (Test-Path -LiteralPath $log)) { throw "Preserve existing evidence for $Name; choose another attempt name" }
    $arguments = @('test',$project,'--mode',$Mode)
    if ($Filter) { $arguments += @('--filter',$Filter) }
    $arguments += @('--output',$xml,'--timeout','1200','--no-color','--','-logFile',$log)
    Invoke-QualifiedStage $Name $arguments $xml
}
& $python Tools/Presentation/qualify_combat_c2.py check
if ($LASTEXITCODE -ne 0) { throw 'Qualified project inputs must match the reviewed root source' }
if ($Stage -in @('all','focused')) {
    Invoke-Tests 'focused-editmode' 'EditMode' $FocusedEditFilter
    Invoke-Tests 'focused-playmode' 'PlayMode' $FocusedPlayFilter
}
if ($Stage -in @('all','remaining','full-edit')) { Invoke-Tests 'full-editmode' 'EditMode' '' }
if ($Stage -in @('all','remaining','play-build','full-play')) { Invoke-Tests 'full-playmode' 'PlayMode' '' }
if ($Stage -in @('all','remaining','play-build','build')) {
    $player = Join-Path $project 'Builds/Development/Windows-combat-c2-20261016/Dungeon Lord.exe'
    if (Test-Path -LiteralPath $player) { throw 'Preserve the existing C2 player; do not overwrite it' }
    $log = Join-Path $taskRoot ('Temp/combat-c2-'+$Attempt+'-windows-build.log')
    Invoke-QualifiedStage 'windows-build' @('build',$project,'--target','StandaloneWindows64','--execute-method',
        'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC2Windows','--allow-dirty-build',
        '--timeout','1800','--no-tail','--log-file',$log,'--no-color') ''
    if (-not (Test-Path -LiteralPath $player)) { throw 'Build produced no Windows executable' }
}
