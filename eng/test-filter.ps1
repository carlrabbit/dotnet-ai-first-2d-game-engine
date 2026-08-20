. (Join-Path $PSScriptRoot 'common.ps1')
if ($args.Count -ne 1) { throw 'usage: pwsh ./eng/test-filter.ps1 <filter>' }
Invoke-Dotnet @('test', (Join-Path $repoRoot 'tests/unit/Agentic2D.Tests.Unit/Agentic2D.Tests.Unit.csproj'), '--no-build', '--treenode-filter', "/*/*/*/*[contains(@name,'$($args[0])')]")
