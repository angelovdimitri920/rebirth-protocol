param(
    [Parameter(Mandatory = $true)]
    [string[]] $UnityArgs,

    [Parameter(Mandatory = $true)]
    [string] $Log,

    [int] $TimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"
$Unity = "D:\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe"

Remove-Item -LiteralPath $Log -Force -ErrorAction SilentlyContinue
$process = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -PassThru

if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $Log) {
        Get-Content -LiteralPath $Log -Tail 240
    }
    throw "Unity batch command timed out after $TimeoutSeconds seconds."
}

if (Test-Path -LiteralPath $Log) {
    Get-Content -LiteralPath $Log -Tail 120
}

exit $process.ExitCode
