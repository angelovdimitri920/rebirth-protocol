# Balance harness (COMBAT_DOCTRINE §13): seeded AI-vs-AI batch runs across
# a matchup matrix, reporting win rate / TTK / knockdowns with doctrine-band
# flags. Writes TestResults\balance-report.md (+ .json). Rosters:
#   default  = loadout-shapes 4×4 + bodies 4×4 (the two clean axes)
#   shapes | bodies | cross (the 16-build sweep — slow, run deliberately)
param(
    [string] $Roster = "default",
    [int] $FightsPerPair = 24,
    [int] $BaseSeed = 101,
    [string] $Arenas = "0,1,2,3",
    [int] $MaxFightSeconds = 240,
    [int] $StepsPerFrame = 600,
    [int] $TimeoutSeconds = 5400
)

$ErrorActionPreference = "Stop"
$ProjectPath = Split-Path -Parent $PSScriptRoot
$Log = Join-Path $env:TEMP "rebirth-unity-balance-harness.log"
$Results = Join-Path $ProjectPath "TestResults\balance-harness-results.xml"

New-Item -ItemType Directory -Path (Split-Path -Parent $Results) -Force | Out-Null
& (Join-Path $PSScriptRoot "Invoke-UnityBatch.ps1") `
    -UnityArgs @(
        "-batchmode", "-nographics", "-projectPath", $ProjectPath,
        "-runTests", "-testPlatform", "PlayMode",
        "-assemblyNames", "RebirthProtocol.BalanceHarness.Tests",
        "-testResults", $Results, "-logFile", $Log,
        "-balanceRoster", $Roster,
        "-balanceFights", "$FightsPerPair",
        "-balanceSeed", "$BaseSeed",
        "-balanceArenas", $Arenas,
        "-balanceMaxFightSeconds", "$MaxFightSeconds",
        "-balanceStepsPerFrame", "$StepsPerFrame") `
    -Log $Log -TimeoutSeconds $TimeoutSeconds

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n--- balance-report.md ---`n"
    Get-Content (Join-Path $ProjectPath "TestResults\balance-report.md")
}
exit $LASTEXITCODE
