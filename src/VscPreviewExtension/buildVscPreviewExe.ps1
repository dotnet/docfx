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
& dotnet build ".\Microsoft.DocAsCode.Dfm.VscPreview" -c $configuration -f net452
ProcessLastExitCode $lastexitcode "previewExe build error"
MD DfmParse -force
Copy-Item -Path ".\Microsoft.DocAsCode.Dfm.VscPreview\bin\$configuration\net452\win7-x64\*.dll" -Destination ".\DfmParse\"
Copy-Item -Path ".\Microsoft.DocAsCode.Dfm.VscPreview\bin\$configuration\net452\win7-x64\*.exe" -Destination ".\DfmParse\"
Pop-Location
