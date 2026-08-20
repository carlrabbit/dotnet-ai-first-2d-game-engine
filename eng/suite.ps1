. (Join-Path $PSScriptRoot 'common.ps1')
if ($args.Count -lt 1) { throw 'usage: pwsh ./eng/suite.ps1 <suite-id> [suite arguments]' }
$suite = $args[0]
$commandArgs = @('run', '--no-build', '--project', (Join-Path $repoRoot 'src/Agentic2D.Engineering'), '--', 'suite', $suite)
if ($args.Count -gt 1) { $commandArgs += $args[1..($args.Count-1)] }
Invoke-Dotnet $commandArgs
