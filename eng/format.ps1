. (Join-Path $PSScriptRoot 'common.ps1')
$verify = if ($args -contains '--verify') { '--verify-no-changes' } else { $null }
$items = @('format', (Join-Path $repoRoot 'dotnet-ai-first-2d-game-engine.slnx'))
if ($null -ne $verify) { $items += $verify }
Invoke-Dotnet $items
