. (Join-Path $PSScriptRoot 'common.ps1')
Invoke-Dotnet @('test', (Join-Path $repoRoot 'tests/unit/Agentic2D.Tests.Unit/Agentic2D.Tests.Unit.csproj'), '--no-build')
