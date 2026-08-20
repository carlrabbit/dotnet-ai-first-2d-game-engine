$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot
function Invoke-Dotnet([string[]] $Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
