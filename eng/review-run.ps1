. (Join-Path $PSScriptRoot 'common.ps1')
if ($args.Count -eq 2 -and $args[0] -eq '--milestone') { $targetArgs = @('--milestone', $args[1]) }
elseif ($args.Count -eq 1) { $targetArgs = @($args[0]) }
else { throw 'usage: pwsh ./eng/review-run.ps1 --milestone <id> | <review-id-or-alias>' }
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'review', 'run') + $targetArgs
Invoke-Dotnet $commandArgs
