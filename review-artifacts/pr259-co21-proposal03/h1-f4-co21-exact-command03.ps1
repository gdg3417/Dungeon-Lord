# REVIEW ONLY. Do not run without separate authorization.
# Working directory: C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord
$ErrorActionPreference = 'Stop'
$launcher = 'C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord/TestResults/h1-f4-co21-recovery-launcher03.ps1'
$bytes = [IO.File]::ReadAllBytes($launcher)
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $digest = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
} finally {
    $sha.Dispose()
}
if ($digest -cne '80CCFDB70938DE4667322AFD894C292AA31E0F1D117A79CD3DF35BFDA8EA3EE7') {
    throw 'Co21 launcher hash mismatch'
}
& ([scriptblock]::Create([Text.Encoding]::UTF8.GetString($bytes))) -Approval 'owner-approved-co21-25da41d400e343f3b53c560cbe1cfd52-attempt03'
