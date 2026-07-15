$ErrorActionPreference = "Stop"
$ProjectPath = Split-Path -Parent $PSScriptRoot
$Log = Join-Path $env:TEMP "rebirth-unity-editmode-tests.log"
$Results = Join-Path $ProjectPath "TestResults\editmode-results.xml"

New-Item -ItemType Directory -Path (Split-Path -Parent $Results) -Force | Out-Null
& (Join-Path $PSScriptRoot "Invoke-UnityBatch.ps1") `
    -UnityArgs @("-batchmode", "-nographics", "-projectPath", $ProjectPath, "-runTests", "-testPlatform", "EditMode", "-testResults", $Results, "-logFile", $Log) `
    -Log $Log
