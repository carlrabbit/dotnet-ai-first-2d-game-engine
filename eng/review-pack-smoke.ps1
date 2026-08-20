. (Join-Path $PSScriptRoot 'common.ps1')
$toolsProject = Join-Path $repoRoot 'src/Agentic2D.Tools/Agentic2D.Tools.csproj'
$outputDir = Join-Path $repoRoot 'artifacts/review/latest'
$inputDir = Join-Path ([System.IO.Path]::GetTempPath()) ('agentic2d-review-pack-' + [guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Force -Path (Join-Path $inputDir 'scenarios'), (Join-Path $inputDir 'content'), (Join-Path $inputDir 'assets') | Out-Null
    $scenarioOutput = Join-Path $repoRoot 'artifacts/scenarios/runtime-smoke'
    $contentScenarioOutput = Join-Path $repoRoot 'artifacts/content/scenarios'
    $contentAssetOutput = Join-Path $repoRoot 'artifacts/content/assets'
    $assetOutput = Join-Path $repoRoot 'artifacts/assets/tile-atlas-smoke'
    Invoke-Dotnet @('run','--no-build','--project',$toolsProject,'--','scenario','run','game/scenarios/smoke/runtime-smoke.json','--output',$scenarioOutput)
    Invoke-Dotnet @('run','--no-build','--project',$toolsProject,'--','content','validate','scenarios','--output',$contentScenarioOutput)
    Invoke-Dotnet @('run','--no-build','--project',$toolsProject,'--','content','validate','assets','--output',$contentAssetOutput)
    Invoke-Dotnet @('run','--no-build','--project',$toolsProject,'--','asset','inspect','asset.tile-atlas-smoke','--output',$assetOutput)
    Copy-Item (Join-Path $repoRoot 'artifacts/scenarios/runtime-smoke') (Join-Path $inputDir 'scenarios') -Recurse -Force
    Copy-Item (Join-Path $repoRoot 'artifacts/content/scenarios') (Join-Path $inputDir 'content') -Recurse -Force
    Copy-Item (Join-Path $repoRoot 'artifacts/content/assets') (Join-Path $inputDir 'content') -Recurse -Force
    Copy-Item (Join-Path $repoRoot 'artifacts/assets/tile-atlas-smoke') (Join-Path $inputDir 'assets') -Recurse -Force
    Invoke-Dotnet @('run','--no-build','--project',$toolsProject,'--','review','pack','--input',$inputDir,'--output',$outputDir)
    foreach ($file in @('review-summary.md','review-manifest.json','diagnostics.json')) {
        if (-not (Test-Path (Join-Path $outputDir $file))) { throw "required review-pack artifact is missing: $file" }
    }
}
finally { if (Test-Path $inputDir) { Remove-Item $inputDir -Recurse -Force } }
