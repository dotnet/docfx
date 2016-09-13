param(
    [string] $configuration = "Release",
    [switch] $raw,
    [switch] $prod
)
$ErrorActionPreference = 'Stop'
$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath

Push-Location $scriptHome

# Check if dotnet cli exists globally
if ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)
{
   Write-Host "dotnet CLI is not successfully configured."
   Write-Host "Please follow https://www.microsoft.com/net/core to install .NET Core."
   Exit 1
}

# Check if nuget.exe exists
$nuget = "$env:APPDATA\Nuget\Nuget.exe"
if (-not(Test-Path $nuget)) {
    Write-Host "Downloading NuGet.exe..."
    $ProgressPreference = 'SilentlyContinue'
    [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nuget
}

if ($raw -eq $false) {
    & ".\UpdateTemplate.cmd"
    if ($lastexitcode -ne 0) { Write-Error "Update templte error, exit code: $lastexitcode"; Pop-Location }
}

if ($prod -eq $true) {
    & ".\UpdateVersion.cmd"
    if ($lastexitcode -ne 0) { Write-Error "Update version error, exit code: $lastexitcode"; Pop-Location }
}

# Restore package
Write-Host "Start to restore package"
foreach ($folder in @("src", "test", "tools"))
{
    Push-Location $folder
    & dotnet restore
    if ($lastexitcode -ne 0) { Write-Error "dotnet restore $folder error, exit code: $lastexitcode"; Pop-Location }
    Pop-Location
}

# Build project
Write-Host "Start to build project"
foreach ($folder in (dir "src"))
{
    if (Test-Path (Join-Path $folder.FullName "project.json")) {
        & dotnet publish $folder.FullName -o target\$configuration\$folder
        if ($lastexitcode -ne 0) { Write-Error "dotnet build $folder error, exit code: $lastexitcode"; Pop-Location }
    }
}

# Run unit test cases
Write-Host "Start to run unit test"
foreach ($folder in (dir "test"))
{
    if ((Test-Path (Join-Path $folder.FullName "project.json")) -and ($folder.Name -ne "Shared") -and ($folder.Name -ne "docfx.E2E.Tests")) {
        & dotnet test test\$folder
        if ($lastexitcode -ne 0) { Write-Error "dotnet test $folder error, exit code: $lastexitcode"; Pop-Location }
    }
}

# Build tools
Write-Host "Build tools"
foreach ($folder in (dir "tools"))
{
    if (Test-Path (Join-Path $folder.FullName "project.json")) {
        & dotnet publish $folder.FullName -o target\$configuration\$folder
        if ($lastexitcode -ne 0) { Write-Error "dotnet build $folder error, exit code: $lastexitcode"; Pop-Location }
    }
}

# Pack artifacts
Write-Host "Publish artifacts"
foreach ($folder in (dir "src"))
{
    if (Test-Path (Join-Path $folder.FullName "project.json")) {
        & dotnet pack $folder.FullName -c $configuration -o artifacts\$configuration
        if ($lastexitcode -ne 0) { Write-Error "dotnet pack $folder error, exit code: $lastexitcode"; Pop-Location }
    }
}

# Pack docfx.console
Copy-Item -Path "target\$configuration\docfx\*.dll" -Destination "src\nuspec\docfx.console\tools\"
Copy-Item -Path "target\$configuration\docfx\*.exe" -Destination "src\nuspec\docfx.console\tools\"
Copy-Item -Path "target\$configuration\docfx\*.exe.config" -Destination "src\nuspec\docfx.console\tools\"

$version = 1.0.0
if (Test-Path "TEMP/version.txt")
{
    $version = cat "TEMP/version.txt"
    $version = $version.Substring(1)
}
& $nuget pack "src\nuspec\docfx.console\docfx.console.nuspec" -Version $version -OutputDirectory artifacts\$configuration
if ($lastexitcode -ne 0) { Write-Error "nuget pack docfx.console error, exit code: $lastexitcode"; Pop-Location; Pop-Location }

Write-Host "Complete."
Pop-Location