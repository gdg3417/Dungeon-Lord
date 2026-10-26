$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskEvidence = Join-Path $taskRoot 'TestResults/h1-f2'
$taskRuntime = Join-Path $taskRoot 'Assets/_Project/H1F2WindowsQualification.cs'
$taskEditor = Join-Path $taskRoot 'Assets/_Project/Editor/H1F2WindowsBuildQualification.cs'
$taskSettings = Join-Path $taskRoot 'ProjectSettings/ProjectSettings.asset'
$taskBuildRecord = Join-Path $taskEvidence 'windows-command.json'
foreach ($taskPath in @($taskRuntime,$taskEditor,$taskBuildRecord,(Join-Path $taskEvidence 'windows-build-report.json'))) {
    if (Test-Path -LiteralPath $taskPath) { throw "Preserve existing artifact: $taskPath" }
}
$taskOriginalSettings = [IO.File]::ReadAllBytes($taskSettings)
$taskRecord = [ordered]@{ sourceCommit=(git -C $taskRoot rev-parse HEAD); startedUtc=[DateTime]::UtcNow.ToString('O'); phases=@() }
try {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'h1-f2-windows-runtime.cs.txt') -Destination $taskRuntime
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'h1-f2-windows-build.cs.txt') -Destination $taskEditor
    $taskIdentity = [Text.Encoding]::UTF8.GetString($taskOriginalSettings).Replace('  productName: Dungeon Lord', '  productName: Dungeon Lord H1F2 Windows 20261025')
    [IO.File]::WriteAllText($taskSettings,$taskIdentity)
    $taskSource = Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets') -Recurse -File | Where-Object Extension -in '.cs','.json','.unity','.asmdef' | ForEach-Object {
        [ordered]@{ path=[IO.Path]::GetRelativePath($taskRoot,$_.FullName); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    }
    $taskSource | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskEvidence 'windows-source.json')
    $taskCli = 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe'
    $taskBuildArgs = @('build',$taskRoot,'--target','StandaloneWindows64','--execute-method','H1F2WindowsBuildQualification.Build',
        '--allow-dirty-build','--timeout','1800',
        '--log-file',(Join-Path $taskEvidence 'windows-build.log'),'--no-tail','--no-color')
    $taskRecord.buildArguments = $taskBuildArgs
    Push-Location -LiteralPath $taskRoot
    try { & $taskCli @taskBuildArgs; $taskRecord.buildExit = $LASTEXITCODE } finally { Pop-Location }
    if ($taskRecord.buildExit -ne 0) { throw 'Windows qualification build failed' }
    $taskPlayer = Join-Path $taskRoot 'Builds/Development/Windows-h1-f2/Dungeon Lord.exe'
    $taskRecord.binarySha256 = (Get-FileHash -LiteralPath $taskPlayer -Algorithm SHA256).Hash
    $taskPersistent = Join-Path $env:USERPROFILE 'AppData/LocalLow/gdg3417/Dungeon Lord H1F2 Windows 20261025'
    if (Test-Path -LiteralPath $taskPersistent) { throw "Refuse reuse of existing isolated product saves: $taskPersistent" }
    $taskSave = $null
    foreach ($taskPhase in @('create','reopen','migrate15','reopen16','future')) {
        if ($taskPhase -eq 'migrate15') {
            $taskText = [IO.File]::ReadAllText($taskSave)
            Copy-Item -LiteralPath $taskSave -Destination (Join-Path $taskEvidence 'before-legacy-projection.json')
            $taskText = $taskText.Replace('"schemaVersion":16','"schemaVersion":15').Replace(',"timeProvenance":{"FormatVersion":1,"Mode":"legacyWholeSeconds","AccountedFraction100ns":0}','')
            [IO.File]::WriteAllText($taskSave,$taskText)
            Copy-Item -LiteralPath $taskSave -Destination (Join-Path $taskEvidence 'schema15-input.json')
        }
        if ($taskPhase -eq 'future') {
            $taskText = [IO.File]::ReadAllText($taskSave).Replace('"schemaVersion":16','"schemaVersion":17')
            [IO.File]::WriteAllText($taskSave,$taskText)
            $taskFutureHash = (Get-FileHash -LiteralPath $taskSave -Algorithm SHA256).Hash
        }
        $taskReceiptPath = Join-Path $taskEvidence ("windows-$taskPhase.json")
        $taskPlayerArgs = @('-h1f2-phase',$taskPhase,'-h1f2-evidence',$taskReceiptPath,'-logFile',(Join-Path $taskEvidence "windows-$taskPhase.log"))
        if ($taskSave) { $taskPlayerArgs += @('-h1f2-savepath',$taskSave) }
        $taskProcess = Start-Process -FilePath $taskPlayer -ArgumentList ($taskPlayerArgs | ForEach-Object { '"' + $_ + '"' }) -WindowStyle Hidden -PassThru
        if (-not $taskProcess.WaitForExit(90000)) { Stop-Process -Id $taskProcess.Id; throw "Qualification phase timeout: $taskPhase" }
        $taskReceipt = Get-Content -Raw -LiteralPath $taskReceiptPath | ConvertFrom-Json
        $taskRecord.phases += [ordered]@{ phase=$taskPhase; exit=$taskProcess.ExitCode; receipt=$taskReceipt; arguments=$taskPlayerArgs }
        if ($taskProcess.ExitCode -ne 0 -or -not $taskReceipt.Passed) { throw "Qualification phase failed: $taskPhase" }
        $taskSave = $taskReceipt.SavePath
        if ($taskPhase -ne 'future') {
            $taskPayload = Get-Content -Raw -LiteralPath $taskSave | ConvertFrom-Json
            if ($taskPayload.schemaVersion -ne 16 -or $taskPayload.primary.lastSavedUtcUnix -le 0 -or
                $taskPayload.primary.timeProvenance.Mode -ne 'legacyWholeSeconds' -or $taskPayload.primary.timeProvenance.AccountedFraction100ns -ne 0) {
                throw "Invalid post-close time owner: $taskPhase"
            }
        }
        Copy-Item -LiteralPath $taskSave -Destination (Join-Path $taskEvidence "windows-$taskPhase-save.json")
        if ($taskPhase -eq 'future' -and (Get-FileHash -LiteralPath $taskSave -Algorithm SHA256).Hash -ne $taskFutureHash) { throw 'Future source was modified' }
    }
} finally {
    [IO.File]::WriteAllBytes($taskSettings,$taskOriginalSettings)
    foreach ($taskPath in @($taskRuntime,($taskRuntime + '.meta'),$taskEditor,($taskEditor + '.meta'))) {
        $taskResolved = [IO.Path]::GetFullPath($taskPath)
        if (-not $taskResolved.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid cleanup path' }
        if (Test-Path -LiteralPath $taskResolved) { Remove-Item -LiteralPath $taskResolved }
    }
    $taskRecord.finishedUtc = [DateTime]::UtcNow.ToString('O')
    [IO.File]::WriteAllText($taskBuildRecord,($taskRecord | ConvertTo-Json -Depth 12))
}
