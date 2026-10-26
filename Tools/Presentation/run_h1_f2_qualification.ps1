param(
    [ValidateSet('focused','migration','integration','research','schema','play')][string]$Stage = 'focused',
    [ValidatePattern('^[0-9]+$')][string]$Attempt = '01'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskEvidence = Join-Path $taskRoot 'TestResults/h1-f2'
New-Item -ItemType Directory -Force $taskEvidence | Out-Null
$taskXml = Join-Path $taskEvidence "$Stage-$Attempt.xml"
$taskLog = Join-Path $taskEvidence "$Stage-$Attempt.log"
$taskRecordPath = Join-Path $taskEvidence "$Stage-$Attempt-command.json"
foreach ($taskFile in @($taskXml,$taskLog,$taskRecordPath)) {
    if (Test-Path -LiteralPath $taskFile) { throw "Preserve existing evidence: $taskFile" }
}
$taskFilter = switch ($Stage) {
    focused { 'H1F2;FrozenSchemaFifteen' }
    migration { 'FrozenSchemaFifteenMigrationRegression;PhaseSevenA2WindowsSpatialMigration;PhaseFourSessions;PhaseFourWrites;PhaseFourLoad;PhaseFourCompleteSave;PhaseFourSaveSemantics' }
    integration { 'R0Research;LivingDungeonB3B2\.;LivingDungeonTwoFloor\.;PhaseFourPassiveOnlineMana;PhaseFourOfflinePassiveMana;PhaseFourOfflineClock;PhaseFourOfflineSaveLifecycle;PhaseFourAcquisition;PhaseFourEconomy' }
    research { 'R0Research' }
    schema { 'PhaseFourCompatibility;CanonicalManualRunPublishesC0OnceKeepsManaHistoryAndTransientEvidenceOnly;SchemaSixAndOrdinarySaveJsonRemainWithoutCanonicalMembers;EveryAuthoredReusableOptionMovesExactIdentityWithoutOtherStateChanges;SchemaTwelveContractRemainsFrozenWhileSchemaThirteenActivatesPosition;EnterAppliesOneTrapOneLootReturnsAndLearnsDurablyWithoutNewSchema;CanonicalPublicationRetainsSpatialEvidenceAndReopenOmitsItWithoutSchemaChange;DurabilityMatrix_SuccessHasIndependentCommitChainAndCanonicalCommitAfterRecovery;RepositionPreservesIdentityWalletCanonicalBytesAndReopensExactPosition;CurrentConstantsAndNativeEmptyCreationUseThirteen;EveryPublishedGraphIsDetachedAndDoesNotBecomeSavePersistenceAuthority;DurableWriteReopensSchemaNineWithoutManaOrInvestmentEffectsOrReturnedCopy;PreviewConfirmDurableRecoverAndAtomicPurchaseRetainExactIdentity;DormantSchemaThirteenPreservesConflictingAliasAndActivationRejectsWithoutMutation;RealWindowsMigrationAndUnfinishedPrefixReopenUseOneAtomicAuthority;NearLimitFrozenSchemaThirteenMigratesWithReservedHeadroom;MigrationAddsHealthyStateWithoutChangingAnyHistoricalOwner' }
    play { 'H1F2;ResearchDurable;LivingDungeonB3B2;LivingDungeonTwoFloor;AdmissionStartReconstruction' }
}
$taskMode = if ($Stage -eq 'play') { 'PlayMode' } else { 'EditMode' }
$taskUnity = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
$taskArgs = @('test',$taskRoot,'--mode',$taskMode,'--filter',$taskFilter,'--output',$taskXml,
    '--timeout','1800','--no-color','--','-logFile',$taskLog)
$taskRecord = [ordered]@{ executable=$taskUnity; arguments=$taskArgs; startedUtc=[DateTime]::UtcNow.ToString('O'); sourceCommit=(git -C $taskRoot rev-parse HEAD) }
$taskManifest = Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets') -Recurse -File | Where-Object Extension -in '.cs','.json','.unity','.asmdef' | ForEach-Object {
    [ordered]@{ path=[IO.Path]::GetRelativePath($taskRoot,$_.FullName); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
$taskManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskEvidence "$Stage-$Attempt-source.json")
$taskSettings = Join-Path $taskRoot 'ProjectSettings/ProjectSettings.asset'
$taskOriginal = [IO.File]::ReadAllBytes($taskSettings)
$taskIdentity = [Text.Encoding]::UTF8.GetString($taskOriginal).Replace('  productName: Dungeon Lord', '  productName: Dungeon Lord B3B2 Smoke H1F2 20261025')
[IO.File]::WriteAllText($taskSettings,$taskIdentity)
try {
    Push-Location -LiteralPath $taskRoot
    try { & $taskUnity @taskArgs; $taskExitCode = $LASTEXITCODE } finally { Pop-Location }
} finally { [IO.File]::WriteAllBytes($taskSettings,$taskOriginal) }
$taskRecord.exitCode = $taskExitCode
$taskRecord.finishedUtc = [DateTime]::UtcNow.ToString('O')
if (Test-Path -LiteralPath $taskXml) {
    [xml]$taskReport = Get-Content -Raw -LiteralPath $taskXml
    $taskRecord.verdict = $taskReport.'test-run' | Select-Object total,passed,failed,skipped
    if ([int]$taskReport.'test-run'.total -eq 0) { $taskExitCode = 1 }
}
[IO.File]::WriteAllText($taskRecordPath, ($taskRecord | ConvertTo-Json -Depth 8))
Write-Output ($taskRecord | ConvertTo-Json -Depth 8)
exit $taskExitCode
