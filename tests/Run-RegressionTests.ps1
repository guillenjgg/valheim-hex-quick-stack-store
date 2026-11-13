$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$sourceFiles = @(
    'tests/RegressionTests.cs',
    'HexQuickStackStorage/Services/PlayerInventoryService.cs',
    'HexQuickStackStorage/Services/QuickStackService.cs',
    'HexQuickStackStorage/Services/InventorySortService.cs',
    'HexQuickStackStorage/Patches/ContainerPatch.cs'
)
$sources = foreach ($sourceFile in $sourceFiles) {
    [System.IO.File]::ReadAllText((Join-Path $repoRoot $sourceFile))
}
$combined = $sources -join "`n"
$usings = [regex]::Matches($combined, '(?m)^using [^;]+;') | ForEach-Object Value | Sort-Object -Unique
$body = [regex]::Replace($combined, '(?m)^using [^;]+;', '')
Add-Type -TypeDefinition (($usings -join "`n") + "`n" + $body) -Language CSharp
[RegressionTests]::Run()
