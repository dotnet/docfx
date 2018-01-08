param(
    [string] $command = "package",
    [string] $version
)

################################################################################################
# Usage:
# Run build.ps1
#   [-command Command]: package or publish, default to be package
#   [-version Version]: Publish version which should be newer than published version
################################################################################################
$ErrorActionPreference = 'Stop'
$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath
$configuration = "Release"
$framework = "net461"
$httpServiceFolder = "DfmHttpService"
$httpServiceFolderPath = "..\$httpServiceFolder"
$language_serverLocation = ".\src\servers\xrefIntellisenseServer"

function ProcessLastExitCode {
    param($exitCode, $msg)
    if ($exitCode -ne 0) {
        Write-Error "$msg, exit code: $exitCode"
        Pop-Location
        Exit 1
    }
}

Push-Location $scriptHome

Write-Host "Build $httpServiceFolder to the target folder"
$outputFolder = Join-Path $scriptHome $httpServiceFolder
& dotnet publish $httpServiceFolderPath -c $configuration -f $framework -o $outputFolder
ProcessLastExitCode $lastexitcode "Error occurs when building $httpServiceFolder"

Pop-Location
Push-Location $language_serverLocation

Write-Host "Build language server to the target folder"
## build server command
npm install
npm run compile
ProcessLastExitCode $lastexitcode "Error occurs when building language server"

Pop-Location
Push-Location $scriptHome
Write-Host "`n$command extension"
& vsce $command $version
ProcessLastExitCode $lastexitcode "Error occurs when $command extension"

Pop-Location
