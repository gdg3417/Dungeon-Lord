param(
    [ValidateSet('focused','regression','full-edit','full-play','build')][string]$Stage = 'focused',
    [ValidatePattern('^[a-z0-9-]+$')][string]$Attempt = '01'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$evidence = Join-Path $taskRoot 'TestResults/living-dungeon-b3a-20261017'
$xml = Join-Path $evidence ($Attempt+'-'+$Stage+'.xml')
$log = Join-Path $evidence ($Attempt+'-'+$Stage+'.log')
$recordPath = Join-Path $evidence ($Attempt+'-'+$Stage+'-command.json')
foreach ($path in @($xml,$log,$recordPath)) { if (Test-Path -LiteralPath $path) { throw "Preserve existing evidence: $path" } }
$cliArgs = @('test',$taskRoot,'--mode','EditMode')
if ($Stage -eq 'focused') { $cliArgs += @('--filter','LivingDungeonPhaseB3A') }
if ($Stage -eq 'regression') { $cliArgs += @('--filter','LivingDungeonPhaseA;LivingDungeonPhaseB1;LivingDungeonPhaseB2;RunHeat;LootHeat;BranchKnowledge;FloorKnowledge;PhaseFiveBBranchIntegration;PhaseSix;CombatC2;DetachedCanonical;SaveSpatialMigration;Gd66Windows;RunHistory') }
if ($Stage -eq 'full-play') { $cliArgs = @('test',$taskRoot,'--mode','PlayMode') }
$cliArgs += @('--output',$xml,'--timeout','1800','--no-color','--','-logFile',$log)
if ($Stage -eq 'build') {
    $player = Join-Path $taskRoot 'Builds/Development/Windows-living-dungeon-b3a-20261017/Dungeon Lord.exe'
    if (Test-Path -LiteralPath $player) { throw 'Preserve the existing B3A player' }
    $cliArgs = @('build',$taskRoot,'--target','StandaloneWindows64','--execute-method',
        'DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildLivingDungeonB3AWindows',
        '--allow-dirty-build','--timeout','1800','--no-tail','--log-file',$log,'--no-color')
}
$record = [ordered]@{ executable=$unity; arguments=$cliArgs; startedUtc=[DateTime]::UtcNow.ToString('O'); sourceCommit=(git rev-parse HEAD) }
& $unity @cliArgs
$record.exitCode = $LASTEXITCODE
$record.finishedUtc = [DateTime]::UtcNow.ToString('O')
if (Test-Path -LiteralPath $xml) {
    [xml]$report = Get-Content -Raw -LiteralPath $xml
    $record.verdict = $report.'test-run' | Select-Object total,passed,failed,skipped
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding utf8
Write-Output ($record | ConvertTo-Json -Depth 8)
exit $record.exitCode
