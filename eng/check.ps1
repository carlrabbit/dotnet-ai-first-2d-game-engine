. (Join-Path $PSScriptRoot 'common.ps1')
& (Join-Path $PSScriptRoot 'restore.ps1'); if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'build.ps1'); if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'test.ps1'); if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'format.ps1') --verify; exit $LASTEXITCODE
