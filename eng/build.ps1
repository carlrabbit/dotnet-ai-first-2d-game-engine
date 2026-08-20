. (Join-Path $PSScriptRoot 'common.ps1')
Invoke-Dotnet @('build', (Join-Path $repoRoot 'dotnet-ai-first-2d-game-engine.slnx'), '--no-restore')
