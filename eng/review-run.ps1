. (Join-Path $PSScriptRoot 'common.ps1')
if ($args.Count -ne 1) { throw 'usage: pwsh ./eng/review-run.ps1 <review-id-or-alias>' }
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'review', 'run', $args[0])
Invoke-Dotnet $commandArgs
