. (Join-Path $PSScriptRoot 'common.ps1')
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'review', 'check') + $args
Invoke-Dotnet $commandArgs
