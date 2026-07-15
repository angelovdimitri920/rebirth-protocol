$ErrorActionPreference = "Stop"
$ProjectPath = Split-Path -Parent $PSScriptRoot
$Log = Join-Path $env:TEMP "rebirth-unity-compile.log"

& (Join-Path $PSScriptRoot "Invoke-UnityBatch.ps1") `
    -UnityArgs @("-batchmode", "-nographics", "-quit", "-projectPath", $ProjectPath, "-executeMethod", "RebirthProtocol.Editor.RebirthProjectSetup.ConfigureProject", "-logFile", $Log) `
    -Log $Log
