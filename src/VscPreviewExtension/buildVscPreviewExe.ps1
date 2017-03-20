param(
    [string] $configuration = "Release"
)

################################################################################################
# Usage:
# Run build.ps1
#   [-configuration Configuration]: Default to be Release
################################################################################################
$ErrorActionPreference = 'Stop'
$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath

function ProcessLastExitCode {
    param($exitCode, $msg)
    if ($exitCode -ne 0)
    {
        Write-Error "$msg, exit code: $exitCode"
        Pop-Location
        Exit 1
    }
}

Push-Location $scriptHome

Write-Host "Build VscPreviewExe to the target folder"
$outputFolder = Join-Path $scriptHome "DfmParse"
& dotnet publish ".\Microsoft.DocAsCode.Dfm.VscPreview" -c $configuration -f net452 -o $outputFolder
ProcessLastExitCode $lastexitcode "Error occurs when building Microsoft.DocAsCode.Dfm.VscPreview"
Pop-Location
