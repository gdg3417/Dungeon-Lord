param(
    [ValidateSet('focused','regression','full-edit','full-play','integration-play','build','baseline-ui')][string]$Stage = 'focused',
    [ValidatePattern('^[a-z0-9-]+$')][string]$Attempt = '01'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$python = 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Read-only: never install substitutions, create evidence, or launch Unity on rejection.
& $python -B (Join-Path $PSScriptRoot 'qualify_b3a.py') preflight $Stage $Attempt
if ($LASTEXITCODE -ne 0) { throw 'B3A isolation is not proven. Resolve the preflight diagnostic before running Unity; preflight does not prepare sources.' }
$unity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$evidence = Join-Path $taskRoot 'TestResults/living-dungeon-b3a-20261017'
$xml = Join-Path $evidence ($Attempt+'-'+$Stage+'.xml')
$log = Join-Path $evidence ($Attempt+'-'+$Stage+'.log')
$recordPath = Join-Path $evidence ($Attempt+'-'+$Stage+'-command.json')
foreach ($path in @($xml,$log,$recordPath)) { if (Test-Path -LiteralPath $path) { throw "Preserve existing evidence: $path" } }
$cliArgs = @('test',$taskRoot,'--mode','EditMode')
if ($Stage -eq 'focused') { $cliArgs += @('--filter','LivingDungeonPhaseB3A') }
if ($Stage -eq 'regression') { $cliArgs += @('--filter','LivingDungeonPhaseA;LivingDungeonPhaseB1;LivingDungeonPhaseB2;RunHeat;LootHeat;BranchKnowledge;FloorKnowledge;PhaseFiveBBranchIntegration;PhaseSix;CombatC2;DetachedCanonical;SaveSpatialMigration;Gd66Windows;RunHistory') }
if ($Stage -eq 'baseline-ui') { $cliArgs += @('--filter','RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated;TransactionalRoomConstructionLayoutsLocalizationAndResume') }
if ($Stage -eq 'full-play') { $cliArgs = @('test',$taskRoot,'--mode','PlayMode') }
if ($Stage -eq 'integration-play') { $cliArgs = @('test',$taskRoot,'--mode','PlayMode','--filter',
    'LivingDungeonPhaseAPlayModeTests;LivingDungeonPhaseB1PlayModeTests;LivingDungeonPhaseB2PlayModeTests;LivingDungeonPhaseB3APlayModeTests;CombatC2PlayModeTests') }
$cliArgs += @('--output',$xml,'--timeout','1800','--no-color','--','-logFile',$log)
if ($Stage -eq 'build') {
    $player = Join-Path $taskRoot 'Builds/Development/Windows-living-dungeon-b3a-20261017/Dungeon Lord.exe'
    if (Test-Path -LiteralPath $player) { throw 'Preserve the existing B3A player' }
    $cliArgs = @('build',$taskRoot,'--target','StandaloneWindows64','--execute-method',
        'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonB3AWindows',
        '--allow-dirty-build','--timeout','1800','--no-tail','--log-file',$log,'--no-color')
}
$record = [ordered]@{ executable=$unity; arguments=$cliArgs; startedUtc=[DateTime]::UtcNow.ToString('O'); sourceCommit=(git -C $taskRoot rev-parse HEAD) }
Push-Location -LiteralPath $taskRoot
try { & $unity @cliArgs } finally { Pop-Location }
$record.exitCode = $LASTEXITCODE
$record.finishedUtc = [DateTime]::UtcNow.ToString('O')
if (Test-Path -LiteralPath $xml) {
    [xml]$report = Get-Content -Raw -LiteralPath $xml
    $record.verdict = $report.'test-run' | Select-Object total,passed,failed,skipped
}
$recordBytes = [Text.Encoding]::UTF8.GetBytes(($record | ConvertTo-Json -Depth 8))
$recordFile = [IO.File]::Open($recordPath, [IO.FileMode]::CreateNew)
try { $recordFile.Write($recordBytes, 0, $recordBytes.Length) } finally { $recordFile.Dispose() }
Write-Output ($record | ConvertTo-Json -Depth 8)
exit $record.exitCode
