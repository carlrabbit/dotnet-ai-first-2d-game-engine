. (Join-Path $PSScriptRoot 'common.ps1')
$reviewPack = Join-Path $repoRoot 'artifacts/review/latest'
$outputDir = Join-Path $repoRoot 'artifacts/workbench/asset-curation'
& (Join-Path $PSScriptRoot 'review-pack-smoke.ps1')
Invoke-Dotnet @('run','--no-build','--project',(Join-Path $repoRoot 'src/Agentic2D.Tools/Agentic2D.Tools.csproj'),'--','asset','curate','--asset','asset.tile-atlas-smoke','--review-pack',$reviewPack,'--output',$outputDir)
foreach ($file in @('index.html','review-data.json','diagnostics.json')) {
    if (-not (Test-Path (Join-Path $outputDir $file))) { throw "required asset-curation artifact is missing: $file" }
}
