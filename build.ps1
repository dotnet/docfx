param(
    [string] $configuration = "Release",
    [switch] $raw = $false,
    [switch] $prod = $false
)

################################################################################################
# Usage:
# Run build.ps1
#   [-configuration Configuration]: Default to be Release
#   [-raw]: If it's set, the build process will skip updating template
#   [-prod]: If it's set, the build process will update version
################################################################################################

$ErrorActionPreference = 'Stop'
$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath

# Get version
$version = "1.0.0"
if (Test-Path "TEMP\version.txt")
{
    $version = Get-Content "TEMP\version.txt"
    $version = $version.Substring(1)
}

function DotnetBuild {
    param($folder)
    if (Test-Path (Join-Path $folder.FullName "project.json"))
    {
        & dotnet build $folder.FullName -c $configuration -f net452
        ProcessLastExitCode $lastexitcode "dotnet build $folder error"
    }
}

function DotnetPublish {
    param($folder)
    if (Test-Path (Join-Path $folder.FullName "project.json"))
    {
        & dotnet publish $folder.FullName -c $configuration -f net452 -o target\$configuration\$folder
        ProcessLastExitCode $lastexitcode "dotnet publish $folder error"
    }
}

function DotnetPack {
    param($folder)
    if (Test-Path (Join-Path $folder.FullName "project.json"))
    {
        & dotnet pack $folder.FullName -c $configuration -o artifacts\$configuration
        ProcessLastExitCode $lastexitcode "dotnet pack $folder error"
    }
}

function NugetPack {
    param($basepath, $nuspec)
    if (Test-Path $nuspec)
    {
        & $nuget pack $nuspec -Version $version -OutputDirectory artifacts\$configuration -BasePath $basepath
        ProcessLastExitCode $lastexitcode "nuget pack $nuspec error"
    }
}

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

# Check if dotnet cli exists globally
if ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)
{
   Write-Host "dotnet CLI is not successfully configured."
   Write-Host "Please follow https://www.microsoft.com/net/core to install .NET Core."
   Pop-Location
   Exit 1
}

# Check if nuget.exe exists
$nuget = "$env:LOCALAPPDATA\Nuget\Nuget.exe"
if (-not(Test-Path $nuget))
{
    Write-Host "Downloading NuGet.exe..."
    mkdir -Path "$env:LOCALAPPDATA\Nuget" -Force
    $ProgressPreference = 'SilentlyContinue'
    [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nuget
}

if ($raw -eq $false)
{
    & ".\UpdateTemplate.cmd"
    ProcessLastExitCode $lastexitcode "Update templte error"
}
else
{
    Write-Host "Skip updating template"
}

if ($prod -eq $true)
{
    & ".\UpdateVersion.cmd"
    ProcessLastExitCode $lastexitcode "Update version error"
}

# Restore package
Write-Host "Start to restore package"
foreach ($folder in @("src", "test", "tools", "plugins"))
{
    Set-Location $folder
    & dotnet restore
    ProcessLastExitCode $lastexitcode "dotnet restore $folder error"
    Set-Location ..
}

# Build project
Write-Host "Start to build project"
foreach ($folder in (Get-ChildItem @("src", "plugins")))
{
    DotnetBuild($folder)
}

# Publish project
Write-Host "Start to publish project"
foreach ($folder in (Get-ChildItem @("src", "plugins")))
{
    DotnetPublish($folder)
}

# Run unit test cases
Write-Host "Start to run unit test"
foreach ($folder in (Get-ChildItem "test"))
{
    if ((Test-Path (Join-Path $folder.FullName "project.json")) -and ($folder.Name -ne "Shared") -and ($folder.Name -ne "docfx.E2E.Tests"))
    {
        & dotnet test test\$folder
        ProcessLastExitCode $lastexitcode "dotnet test $folder error"
    }
}

# Build tools
Write-Host "Build tools"
foreach ($folder in (Get-ChildItem "tools"))
{
    DotnetBuild($folder)
}

# Publish tools
Write-Host "Publish tools"
foreach ($folder in (Get-ChildItem "tools"))
{
    DotnetPublish($folder)
}

# Pack artifacts
Write-Host "Publish artifacts"
foreach ($folder in (Get-ChildItem "src"))
{
    DotnetPack($folder)
}

# Pack plugins
foreach ($folder in (Get-ChildItem "plugins"))
{
    $nuspecs = Join-Path $folder.FullName "*.nuspec" -Resolve
    if ($nuspecs)
    {
       foreach ($nuspec in $nuspecs)
       {
           NugetPack "target\$configuration\$($folder.Name)" $nuspec
       }
    }
    else
    {
        DotnetPack($folder)
    }
}

# Pack docfx.console
Copy-Item -Path "src\nuspec\docfx.console\build" -Destination "target\$configuration\docfx" -Force -Recurse
Copy-Item -Path "src\nuspec\docfx.console\content" -Destination "target\$configuration\docfx" -Force -Recurse
NugetPack "target\$configuration\docfx" "src\nuspec\docfx.console\docfx.console.nuspec"

# Pack azure tools
NugetPack "target\$configuration\AzureMarkdownRewriterTool" "src\nuspec\AzureMarkdownRewriterTool\AzureMarkdownRewriterTool.nuspec"

# Pack DfmHttpService
NugetPack "target\$configuration\DfmHttpService" "src\nuspec\DfmHttpService\DfmHttpService.nuspec"

# Build VscPreviewExe
src\VscPreviewExtension\buildVscPreviewExe.cmd -c $configuration
ProcessLastExitCode $lastexitcode "build VscPreviewExe error"

Write-Host "Build completed."
Pop-Location
