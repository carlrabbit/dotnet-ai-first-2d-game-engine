. (Join-Path $PSScriptRoot 'common.ps1')

function Assert-PropertyNames($Value, [string[]] $Expected, [string] $Context) {
    $actual = @($Value.PSObject.Properties.Name | Sort-Object -Unique)
    $expectedSorted = @($Expected | Sort-Object -Unique)
    $difference = @(Compare-Object -ReferenceObject $expectedSorted -DifferenceObject $actual -CaseSensitive)
    if ($difference.Count -ne 0) {
        throw "$Context properties are invalid. Expected [$($expectedSorted -join ', ')], found [$($actual -join ', ')]"
    }
}

$profilePath = Join-Path $repoRoot '.guide-profile.json'
$profile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
if ($profile.schemaVersion -isnot [long] -or $profile.schemaVersion -ne 2) { throw 'schemaVersion must be integer 2.' }
Assert-PropertyNames $profile.guideSystem @('repository', 'version') 'guideSystem'
if ($profile.guideSystem.repository -cne 'carlrabbit/agentic-project-guides') { throw 'guideSystem.repository is not the configured guide repository.' }
if ($profile.guideSystem.version -cne '0.9.1') { throw 'guideSystem.version must be 0.9.1.' }

$legacyRootFields = @(
    'guideVersion', 'guideSystemVersion', 'guideRepository',
    'baseProfileVersion', 'appliedProfileVersion', 'base', 'applied',
    'repositoryProfiles', 'selectedProfiles', 'profileSelections', 'testedPlatforms'
)
foreach ($field in $legacyRootFields) {
    if ($profile.PSObject.Properties.Name -ccontains $field) { throw "Legacy root field '$field' is not allowed." }
}

$expected = @{
    'base' = 'repository'
    'artifact-first-runtime' = 'repository'
    'dotnet-library' = 'component:engine-libraries'
    'cli-tool' = 'surface:product-cli'
}
if ($profile.appliedProfiles -isnot [System.Array]) { throw 'appliedProfiles must be an array.' }
$entries = @($profile.appliedProfiles)
if ($entries.Count -ne $expected.Count) { throw "Expected exactly $($expected.Count) applied profiles; found $($entries.Count)." }

$seen = @{}
foreach ($entry in $entries) {
    Assert-PropertyNames $entry @('id', 'version', 'scopes') 'Applied profile'
    if ([string]::IsNullOrWhiteSpace([string]$entry.id)) { throw 'Every applied profile requires a non-empty id.' }
    if ($entry.version -cne '0.9.1') { throw "Profile '$($entry.id)' must declare version 0.9.1." }
    if ($expected.Keys -cnotcontains [string]$entry.id) { throw "Unexpected profile '$($entry.id)'." }
    if ($seen.ContainsKey([string]$entry.id)) { throw "Duplicate profile '$($entry.id)'." }

    if ($entry.scopes -isnot [System.Array]) { throw "Profile '$($entry.id)' scopes must be an array." }
    $scopes = @($entry.scopes)
    if ($scopes.Count -ne 1) { throw "Profile '$($entry.id)' must have exactly one scope." }
    $scope = $scopes[0]
    if ($scope.PSObject.Properties.Name -ccontains 'type') { throw "Legacy scope.type is not allowed for '$($entry.id)'." }
    if ($scope.PSObject.Properties.Name -ccontains 'scope') { throw "Legacy scope wrapper is not allowed for '$($entry.id)'." }

    $expectedScope = [string]$expected[[string]$entry.id]
    if ($expectedScope -eq 'repository') {
        Assert-PropertyNames $scope @('kind') "Scope for '$($entry.id)'"
        if ($scope.kind -cne 'repository') { throw "Profile '$($entry.id)' requires repository scope." }
        $seen[[string]$entry.id] = 'repository'
    } else {
        $parts = $expectedScope.Split(':', 2)
        Assert-PropertyNames $scope @('kind', 'ids') "Scope for '$($entry.id)'"
        if ($scope.kind -cne $parts[0]) { throw "Profile '$($entry.id)' requires $($parts[0]) scope." }
        if ($scope.ids -isnot [System.Array]) { throw "Profile '$($entry.id)' scope ids must be an array." }
        $ids = @($scope.ids)
        if ($ids.Count -ne 1 -or $ids[0] -cne $parts[1]) {
            throw "Profile '$($entry.id)' requires ids [$($parts[1])]."
        }
        $seen[[string]$entry.id] = "$($scope.kind):$($ids[0])"
    }
}

if ($seen.Count -ne $expected.Count) { throw 'The applied profile assignment set is incomplete.' }
foreach ($id in $expected.Keys) {
    $actual = [string]$seen[$id]
    if ($actual -cne [string]$expected[$id]) { throw "Profile '$id' has assignment '$actual'; expected '$($expected[$id])'." }
}

Write-Host 'Guide profile schema and exact 0.9.1 assignments passed.'
