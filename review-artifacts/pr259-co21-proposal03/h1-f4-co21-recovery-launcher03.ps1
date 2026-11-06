# Co21 only. Authenticate these exact bytes externally before invocation.
param([Parameter(Mandatory=$true)][string]$Approval)
$ErrorActionPreference = 'Stop'
$root = 'C:/Users/gdg34/.codex/worktrees/h1-f4-recovery/Dungeon-Lord'
$pythonRoot = 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python'
$python = "$pythonRoot/python.exe"
$script = "$root/TestResults/h1-f4-co21-recovery-proposal03.py"
if ($Approval -cne 'owner-approved-co21-25da41d400e343f3b53c560cbe1cfd52-attempt03' -or
    [IO.Path]::GetFullPath((Get-Location).Path) -ne [IO.Path]::GetFullPath($root)) {
    throw 'Wrong Co21 approval identity or working directory.'
}
$pins = [ordered]@{}
$pins[$script] = '369D3DC3DC4F19402135C8314E8C4426F92FF57DC16DF696010AA528EDACCC33'
$pins[$python] = 'F8B45D63047DA9D9ADF09892E4CC4317B859E86A93A478CC9616BC69D3B176E5'
$pins["$pythonRoot/python312.dll"] = '0CA3ACBF9CE7165A4E3779FA9DDECECA652AF781B22C2BD2BA377D0D6B4C9C01'
$pins["$pythonRoot/python3.dll"] = '418CBD58EC56B6AC75B3000B30428CA33339425685B980FE444F0BFDCAE7D94E'
$pins["$pythonRoot/vcruntime140.dll"] = 'D5E4D9A3E835FA679450145D6A7D94E36573A509317111904D9B3712C30D9066'
$pins["$pythonRoot/vcruntime140_1.dll"] = '1F2D41C4AA5DB0BC33EBF7B66D72943A817D7CE6CBE880502A9403823633093F'
$pins["$pythonRoot/DLLs/_ctypes.pyd"] = '61546CBF4D68AFDCDC6ADB7C092D08776C738062B8696DD8E12B14092CC68C24'
$pins["$pythonRoot/DLLs/_hashlib.pyd"] = '7947984CD95AC581DEA8DE194ECC3C8E7DE05065B6FCCA44746DD1D259AD126C'
$pins["$pythonRoot/DLLs/libcrypto-3-x64.dll"] = 'C1823E279B5CFD40C5C46E082D14B5D20D2884102079254B2B016F59266D76C7'
$pins["$pythonRoot/DLLs/libffi-8.dll"] = 'C5AE4DAD3CFD76B1CA59B87505079382072F8EB1CDE719FCD7C729FCEEBF898B'
$pins['C:/Program Files/Git/cmd/git.EXE'] = '81EF35AE005CA9318018D18E3327578CE939FB99FEAAD6B2D7C8AB15F3DE8DB5'
$pins['C:/Program Files/Git/mingw64/bin/git.exe'] = 'CAB4C4EEA1D869CF9F7BE73868DC9A90AD2DF1B1B673E5F8C8714A576C25EA96'
$pins['C:/Windows/System32/WindowsPowerShell/v1.0/powershell.EXE'] = 'D8F182560210D43BAAF767D482AD1DC1207372522E91413B9D5E515A875E552D'
$handles = [Collections.Generic.List[IO.FileStream]]::new()
try {
    foreach ($entry in $pins.GetEnumerator()) {
        $file = Get-Item -LiteralPath $entry.Key -Force
        for ($node = $file; $null -ne $node; $node = $node.Parent) {
            if ($node.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Redirected executable input: $($entry.Key)"
            }
            if ($node -is [IO.FileInfo]) {
                $node = $node.Directory
                if ($node.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                    throw "Redirected executable input directory: $($entry.Key)"
                }
            }
        }
        $handle = [IO.File]::Open($entry.Key, 'Open', 'Read', 'Read')
        $handles.Add($handle)
        if ((Get-FileHash -InputStream $handle -Algorithm SHA256).Hash -cne $entry.Value) {
            throw "Executable input hash mismatch: $($entry.Key)"
        }
    }
    & $python -I -S -B $script $Approval
    if ($LASTEXITCODE -ne 0) { throw "Co21 procedure exited $LASTEXITCODE. Do not retry." }
}
finally {
    foreach ($handle in $handles) { $handle.Dispose() }
}
