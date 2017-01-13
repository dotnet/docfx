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
    param($folder, $version)
    $nuspec = Join-Path $folder.FullName ($folder.Name + ".nuspec")
    if (Test-Path $nuspec)
    {
        $basepath = Join-Path target (Join-Path $configuration $folder.Name)
        if ((Test-Path $basepath) -and (Test-Path (Join-Path $folder.FullName "project.json")))
        {
            & $nuget pack $nuspec -Version $version -OutputDirectory artifacts\$configuration -BasePath $basepath
        }
    }
    Else
    {
        DotnetPack($folder)
    }
}

function PackSelfContainProject {
    param($assemblyFolder, $nuspecPath)

    $nuspecFile = Get-Item $nuspecPath
    $nuspecName = $nuspecFile.Name
    $nuspecFolder = $nuspecFile.Directory.FullName
    $nuspecFolderName = $nuspecFile.Directory.Name
    $targetFolder = "PACK\$nuspecFolderName"

    if (Test-Path $targetFolder)
    {
        $null = Remove-Item $targetFolder -Force -Recurse
    }
    $null = New-Item -ItemType Directory -Force -Path $targetFolder
    $null = New-Item -ItemType Directory -Force -Path "$targetFolder\tools\"

    Copy-Item -Path "$nuspecFolder\**" -Destination "$targetFolder" -Force -Recurse
    Copy-Item -Path "$assemblyFolder\*.dll" -Destination "$targetFolder\tools\" -Force
    Copy-Item -Path "$assemblyFolder\*.exe" -Destination "$targetFolder\tools\" -Force
    Copy-Item -Path "$assemblyFolder\*.exe.config" -Destination "$targetFolder\tools\" -Force

    & $nuget pack "$targetFolder\$nuspecName" -Version $version -OutputDirectory artifacts\$configuration
    ProcessLastExitCode $lastexitcode "nuget pack error while packing $nuspecPath"
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
    CD $folder
    & dotnet restore
    ProcessLastExitCode $lastexitcode "dotnet restore $folder error"
    CD ..
}

# Build project
Write-Host "Start to build project"
foreach ($folder in (dir @("src", "plugins")))
{
    DotnetBuild($folder)
}

# Publish project
Write-Host "Start to publish project"
foreach ($folder in (dir @("src", "plugins")))
{
    DotnetPublish($folder)
}

# Run unit test cases
Write-Host "Start to run unit test"
foreach ($folder in (dir "test"))
{
    if ((Test-Path (Join-Path $folder.FullName "project.json")) -and ($folder.Name -ne "Shared") -and ($folder.Name -ne "docfx.E2E.Tests"))
    {
        & dotnet test test\$folder
        ProcessLastExitCode $lastexitcode "dotnet test $folder error"
    }
}

# Build tools
Write-Host "Build tools"
foreach ($folder in (dir "tools"))
{
    DotnetBuild($folder)
}

# Publish tools
Write-Host "Publish tools"
foreach ($folder in (dir "tools"))
{
    DotnetPublish($folder)
}

# Pack artifacts
Write-Host "Publish artifacts"
foreach ($folder in (dir "src"))
{
    DotnetPack($folder)
}

# Get version
$version = "1.0.0"
if (Test-Path "TEMP/version.txt")
{
    $version = cat "TEMP/version.txt"
    $version = $version.Substring(1)
}

# Pack plugins
foreach ($folder in (dir "plugins"))
{
    NugetPack $folder $version
}

# Pack docfx.console
PackSelfContainProject "target\$configuration\docfx" "src\nuspec\docfx.console\docfx.console.nuspec"

# Pack azure tools
PackSelfContainProject "target\$configuration\AzureMarkdownRewriterTool" "src\nuspec\AzureMarkdownRewriterTool\AzureMarkdownRewriterTool.nuspec"

# Pack DfmHttpService
PackSelfContainProject "target\$configuration\DfmHttpService" "src\nuspec\DfmHttpService\DfmHttpService.nuspec"

# Build VscPreviewExe
src\VscPreviewExtension\buildVscPreviewExe.cmd -c $configuration
ProcessLastExitCode $lastexitcode "build VscPreviewExe error"

Write-Host "Build completed."
Pop-Location
