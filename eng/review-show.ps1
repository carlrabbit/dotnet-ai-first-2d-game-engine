. (Join-Path $PSScriptRoot 'common.ps1')
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'review', 'show') + $args
Invoke-Dotnet $commandArgs
