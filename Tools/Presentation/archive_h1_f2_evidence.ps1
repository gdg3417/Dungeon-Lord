param([Parameter(Mandatory=$true)][string]$ImplementationCommit)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskSource = Join-Path $taskRoot 'TestResults/h1-f2'
$taskDestination = Join-Path $taskRoot 'Docs/testing/evidence/h1-f2'
$taskArchive = Join-Path $taskDestination 'qualification-reports.zip'
$taskStaging = Join-Path $taskSource 'publication-copy'
if (Test-Path -LiteralPath $taskArchive) { throw 'Preserve existing qualification archive' }
if (Test-Path -LiteralPath $taskStaging) { throw 'Preserve existing publication copy' }
New-Item -ItemType Directory -Force $taskDestination,$taskStaging | Out-Null
$taskFiles = Get-ChildItem -LiteralPath $taskSource -File | Where-Object Extension -in '.xml','.json','.log','.txt' | Sort-Object Name
$taskSeals = @()
foreach ($taskFile in $taskFiles) {
    $taskTarget = Join-Path $taskStaging $taskFile.Name
    $taskText = [IO.File]::ReadAllText($taskFile.FullName)
    # Unity CLI injects short-lived authentication arguments into Editor logs.
    # Keep originals local; redact credential lines from publication copies.
    $taskLines = $taskText -split "`r?`n"
    $taskOutput = [Collections.Generic.List[string]]::new()
    $taskRedactions = 0
    for ($taskIndex = 0; $taskIndex -lt $taskLines.Length; $taskIndex++) {
        $taskLine = $taskLines[$taskIndex]
        if ($taskLine -match '(?i)accessToken|authorization:\s*bearer') {
            if ($taskLine.Trim() -eq '-accessToken') { $taskIndex++ }
            $taskOutput.Add('[REDACTED authentication argument]'); $taskRedactions++
        } else { $taskOutput.Add($taskLine) }
    }
    if ($taskRedactions -gt 0) { [IO.File]::WriteAllText($taskTarget,($taskOutput -join "`n")) }
    else { Copy-Item -LiteralPath $taskFile.FullName -Destination $taskTarget }
    $taskSeals += [ordered]@{ name=$taskFile.Name; originalSha256=(Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash;
        publishedSha256=(Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash; redactedLines=$taskRedactions }
}
$taskRuns = @(Get-ChildItem -LiteralPath $taskSource -Filter '*-command.json' | Sort-Object Name | ForEach-Object {
    $taskRecord = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
    [ordered]@{ record=$_.Name; sourceCommit=$taskRecord.sourceCommit; exitCode=$taskRecord.exitCode; verdict=$taskRecord.verdict }
})
$taskLatest = @('focused-08','migration-03','research-02','integration-03','play-02','schema-02')
$taskRuntimeMatches = @(foreach ($taskName in $taskLatest) {
    $taskManifest = Get-Content -Raw -LiteralPath (Join-Path $taskSource "$taskName-source.json") | ConvertFrom-Json
    $taskRuntime = @($taskManifest | Where-Object { $_.path -match '^Assets[\\/]_Project[\\/](Scripts|Data|Resources|Scenes)[\\/]' })
    $taskMismatches = @($taskRuntime | Where-Object { (Get-FileHash -LiteralPath (Join-Path $taskRoot $_.path) -Algorithm SHA256).Hash -ne $_.sha256 })
    [ordered]@{ run=$taskName; comparedFiles=$taskRuntime.Count; mismatches=@($taskMismatches | ForEach-Object path) }
})
$taskManifest = [ordered]@{ implementationCommit=$ImplementationCommit; archivedUtc=[DateTime]::UtcNow.ToString('O');
    originalEvidence=$taskSource; runs=$taskRuns; runtimeInputMatches=$taskRuntimeMatches; fileSeals=$taskSeals }
[IO.File]::WriteAllText((Join-Path $taskDestination 'evidence-manifest.json'),($taskManifest | ConvertTo-Json -Depth 12))
Compress-Archive -Path (Join-Path $taskStaging '*') -DestinationPath $taskArchive -CompressionLevel Optimal
Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256
