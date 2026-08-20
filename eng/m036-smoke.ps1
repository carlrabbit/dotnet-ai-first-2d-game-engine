. (Join-Path $PSScriptRoot 'common.ps1')
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'suite', 'm036-smoke') + $args
Invoke-Dotnet $commandArgs
