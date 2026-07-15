$ErrorActionPreference = "Stop"
$ProjectPath = Split-Path -Parent $PSScriptRoot
$Log = Join-Path $env:TEMP "rebirth-unity-build-windows.log"

& (Join-Path $PSScriptRoot "Invoke-UnityBatch.ps1") `
    -UnityArgs @("-batchmode", "-nographics", "-quit", "-projectPath", $ProjectPath, "-executeMethod", "RebirthProtocol.Editor.RebirthBuildTools.BuildWindowsDevelopment", "-logFile", $Log) `
    -Log $Log `
    -TimeoutSeconds 1800
