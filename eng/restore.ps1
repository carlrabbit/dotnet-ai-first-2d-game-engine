. (Join-Path $PSScriptRoot 'common.ps1')
Invoke-Dotnet @('restore', (Join-Path $repoRoot 'dotnet-ai-first-2d-game-engine.slnx'))
