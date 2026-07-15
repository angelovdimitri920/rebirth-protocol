param(
    [string] $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Stop"

$sourceRoot = Join-Path $ProjectRoot "3D_models"
$targetRoot = Join-Path $ProjectRoot "ReferenceOnly\Extracted"
$reportDir = Join-Path $ProjectRoot "ReferenceOnly\Reports"
$docsDir = Join-Path $ProjectRoot "docs"
$jsonReport = Join-Path $reportDir "reference_inventory.json"
$mdReport = Join-Path $docsDir "ASSET_REFERENCE_INVENTORY.md"

if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw "Missing source folder: $sourceRoot"
}

New-Item -ItemType Directory -Path $targetRoot, $reportDir, $docsDir -Force | Out-Null

$resolvedTargetRoot = (Resolve-Path -LiteralPath $targetRoot).Path

function Assert-PathInside {
    param(
        [string] $ChildPath,
        [string] $ParentPath
    )

    $fullChild = [System.IO.Path]::GetFullPath($ChildPath)
    $fullParent = [System.IO.Path]::GetFullPath($ParentPath)
    if (-not $fullParent.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $fullParent += [System.IO.Path]::DirectorySeparatorChar
    }

    if (-not $fullChild.StartsWith($fullParent, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside extraction root: $fullChild"
    }
}

function Convert-ToSafeName {
    param([string] $Name)
    $safe = $Name -replace '^GameCube - Custom Robo - ', ''
    $safe = $safe -replace '[\\/:*?"<>|]', '-'
    $safe = $safe -replace '\s+', ' '
    return $safe.Trim()
}

$items = @()

Get-ChildItem -LiteralPath $sourceRoot -Directory | Sort-Object Name | ForEach-Object {
    $category = $_.Name
    $categoryTarget = Join-Path $targetRoot $category
    New-Item -ItemType Directory -Path $categoryTarget -Force | Out-Null

    Get-ChildItem -LiteralPath $_.FullName -Filter *.zip -File | Sort-Object Name | ForEach-Object {
        $zip = $_
        $assetName = Convert-ToSafeName ([System.IO.Path]::GetFileNameWithoutExtension($zip.Name))
        $assetTarget = Join-Path $categoryTarget $assetName
        Assert-PathInside -ChildPath $assetTarget -ParentPath $resolvedTargetRoot

        if (Test-Path -LiteralPath $assetTarget) {
            Remove-Item -LiteralPath $assetTarget -Recurse -Force
        }

        New-Item -ItemType Directory -Path $assetTarget -Force | Out-Null
        Expand-Archive -LiteralPath $zip.FullName -DestinationPath $assetTarget -Force

        $files = Get-ChildItem -LiteralPath $assetTarget -Recurse -File | Sort-Object FullName
        $extensionCounts = @{}
        foreach ($file in $files) {
            $extension = [System.IO.Path]::GetExtension($file.Name).ToLowerInvariant()
            if ([string]::IsNullOrWhiteSpace($extension)) {
                $extension = "[none]"
            }
            if (-not $extensionCounts.ContainsKey($extension)) {
                $extensionCounts[$extension] = 0
            }
            $extensionCounts[$extension] += 1
        }

        $items += [pscustomobject]@{
            category = $category
            assetName = $assetName
            sourceZip = $zip.FullName
            extractedPath = $assetTarget
            fileCount = $files.Count
            extensions = $extensionCounts
            objFiles = @($files | Where-Object { $_.Extension -ieq ".obj" } | ForEach-Object { $_.FullName })
            textureFiles = @($files | Where-Object { $_.Extension -match '^\.(png|jpg|jpeg|tga)$' } | ForEach-Object { $_.FullName })
        }
    }
}

$items | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonReport -Encoding utf8

$lines = @()
$lines += "# Asset Reference Inventory"
$lines += ""
$lines += "Source folder: ``$sourceRoot``"
$lines += ""
$lines += "Extracted working copies: ``$targetRoot``"
$lines += ""
$lines += "These archives are reference-only. Do not import ripped or unclear-license assets into production Unity assets."
$lines += ""
$lines += "| Category | Asset | Files | OBJ | Textures | Extracted Path |"
$lines += "| --- | --- | ---: | ---: | ---: | --- |"
foreach ($item in $items) {
    $objCount = @($item.objFiles).Count
    $textureCount = @($item.textureFiles).Count
    $lines += "| $($item.category) | $($item.assetName) | $($item.fileCount) | $objCount | $textureCount | ``$($item.extractedPath)`` |"
}

$lines += ""
$lines += "Totals:"
$lines += ""
$items | Group-Object category | Sort-Object Name | ForEach-Object {
    $lines += "- ``$($_.Name)``: $($_.Count) archives"
}

$lines | Set-Content -LiteralPath $mdReport -Encoding utf8

Write-Host "Extracted $($items.Count) archives."
Write-Host "JSON report: $jsonReport"
Write-Host "Markdown report: $mdReport"
