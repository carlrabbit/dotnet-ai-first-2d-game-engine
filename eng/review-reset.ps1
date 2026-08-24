. (Join-Path $PSScriptRoot 'common.ps1')
if ($args.Count -ne 2 -or $args[0] -ne '--milestone') { throw 'usage: pwsh ./eng/review-reset.ps1 --milestone <id>' }
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'review', 'reset', '--milestone', $args[1])
Invoke-Dotnet $commandArgs
