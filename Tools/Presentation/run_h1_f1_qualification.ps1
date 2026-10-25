param(
    [ValidateSet('focused','regression','migration','scene-profile')][string]$Stage = 'focused',
    [ValidatePattern('^[0-9]+$')][string]$Attempt = '01'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$evidence = Join-Path $taskRoot 'TestResults/h1-f1'
New-Item -ItemType Directory -Force $evidence | Out-Null
$xml = Join-Path $evidence "$Stage-$Attempt.xml"
$log = Join-Path $evidence "$Stage-$Attempt.log"
$recordPath = Join-Path $evidence "$Stage-$Attempt-command.json"
foreach ($path in @($xml,$log,$recordPath)) {
    if (Test-Path -LiteralPath $path) { throw "Preserve existing evidence: $path" }
}
$filter = if ($Stage -eq 'focused') { 'FrozenSchemaFifteen' }
elseif ($Stage -eq 'migration') { 'FrozenSchemaFifteen;PhaseSevenA2WindowsSpatialMigration' }
elseif ($Stage -eq 'scene-profile') { 'LivingDungeonTwoFloorProductionProfileSceneTests' }
else {
    'Gd66;PhaseFour;PhaseFiveADurableBranch;PhaseSix;CombatC2;LivingDungeonPhaseB2;LivingDungeonB3B2;LivingDungeonTwoFloor;SaveSpatialMigration;DetachedCanonical;MigrationRunner'
}
$unity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$cliArgs = @('test',$taskRoot,'--mode','EditMode','--filter',$filter,'--output',$xml,
    '--timeout','1800','--no-color','--','-logFile',$log)
$record = [ordered]@{ executable=$unity; arguments=$cliArgs; startedUtc=[DateTime]::UtcNow.ToString('O'); sourceCommit=(git -C $taskRoot rev-parse HEAD) }
Push-Location -LiteralPath $taskRoot
try { & $unity @cliArgs } finally { Pop-Location }
$taskExitCode = $LASTEXITCODE
$record.exitCode = $taskExitCode
$record.finishedUtc = [DateTime]::UtcNow.ToString('O')
if (Test-Path -LiteralPath $xml) {
    [xml]$report = Get-Content -Raw -LiteralPath $xml
    $record.verdict = $report.'test-run' | Select-Object total,passed,failed,skipped
    if ([int]$report.'test-run'.total -eq 0) { $taskExitCode = 1 }
}
[IO.File]::WriteAllText($recordPath, ($record | ConvertTo-Json -Depth 8))
Write-Output ($record | ConvertTo-Json -Depth 8)
exit $taskExitCode
