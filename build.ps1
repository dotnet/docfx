param(
    [string] $configuration = "Release",
    [switch] $raw = $false,
    [switch] $prod = $false,
    [switch] $skipTests = $false,
    [switch] $release = $false
)
################################################################################################
# Usage:
# Run build.ps1
#   [-configuration Configuration]: Default to be Release
#   [-raw]: If it's set, the build process will skip updating template
#   [-prod]: If it's set, the build process will update version
#   [-skipTests]: If it's set, running unit tests will be skipped
################################################################################################

$ErrorActionPreference = 'Stop'
$releaseBranch = "master"
$dotnetCommand = "dotnet"
$gitCommand = "git"
$framework = "net462"
$packageVersion = "1.0.0"
$assemblyVersion = "1.0.0.0"

if ([environment]::OSVersion.Platform -eq "Win32NT") {
    $os = "Windows"
}
else {
    $os = "Linux"
}
Write-Host "Running on OS $os"

if ($os -eq "Windows") {
    $nugetCommand = "$env:LOCALAPPDATA/Nuget/Nuget.exe"
}
else {
    $nugetCommand = "nuget"
}

$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath
$versionCsFolderPath = $scriptHome + "/TEMP/"
$versionCsFilePath = $versionCsFolderPath + "version.cs"
$versionFsFilePath = $versionCsFolderPath + "version.fs"

$global:LASTEXITCODE = $null

Push-Location $scriptHome

function NugetPack {
    param($basepath, $nuspec, $version)
    if (Test-Path $nuspec) {
        & $nugetCommand pack $nuspec -Version $version -OutputDirectory artifacts/$configuration -BasePath $basepath
        ProcessLastExitCode $lastexitcode "$nugetCommand pack $nuspec -Version $version -OutputDirectory artifacts/$configuration -BasePath $basepath"
    }
}

function ProcessLastExitCode {
    param($exitCode, $msg)
    if ($exitCode -eq 0) {
        Write-Host "Success: $msg
        " -ForegroundColor Green
    }
    else {
        Write-Host "Error $($exitCode): $msg
        " -ForegroundColor Red
        Pop-Location
        Exit 1
    }
}

function ValidateCommand {
    param($command)
    return (Get-Command $command -ErrorAction SilentlyContinue) -ne $null
}

# Check if dotnet cli exists globally
if (-not(ValidateCommand("dotnet"))) {
    ProcessLastExitCode 1 "Dotnet CLI is not successfully configured. Please follow https://www.microsoft.com/net/core to install .NET Core."
}

# Check if nuget.exe exists
if (-not(ValidateCommand($nugetCommand))) {
    Write-Host "Downloading NuGet.exe..."
    mkdir -Path "$env:LOCALAPPDATA/Nuget" -Force
    $ProgressPreference = 'SilentlyContinue'
    [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetCommand
}

# Update template
if ($raw -eq $false) {
    ./UpdateTemplate.ps1
    ProcessLastExitCode $lastexitcode "Update template"
}
else {
    Write-Host "Skip updating template"
}

if ($prod -eq $true) {
    Write-Host "Updating version from ReleaseNote.md and GIT commit info"

    if (-not(ValidateCommand($gitCommand))) {
        ProcessLastExitCode 1 "Git is required however it is not installed."
    }

    if ($release -eq $false) {
        $branch = & $gitCommand rev-parse --abbrev-ref HEAD
        ProcessLastExitCode $lastexitcode "Get GIT branch name $branch"
    }
    else {
        $branch = "master";
        Write-Host "Release version using $branch branch"
    }

    $version = "v1", "0", "0"

    $firstLine = Get-Content ReleaseNote.md | Select-Object -First 1
    if ($firstLine -match ".*(v[0-9.]+)") {
        $mainVersion = ($matches[1] -split '\.')
        for ($i = 0; $i -lt $mainVersion.length -and $i -lt 3; $i++) {
            $version[$i] = $mainVersion[$i]
        }
    }

    $commitInfo = (& $gitCommand describe --tags) -split '-'

    ProcessLastExitCode $lastexitcode "Get GIT commit information $commitInfo"
    if ($commitInfo.length -gt 1) {
        $revision = (Get-Date -UFormat "%Y%m%d").Substring(2) + $commitInfo[1].PadLeft(3, '0')
    }
    else {
        $revision = '000000000'
    }

    $assemblyVersion = (($version + '0') -join '.').Substring(1)
    $assemblyFileVersion = (($version + $revision) -join '.').Substring(1)
    if ($branch -ne $releaseBranch) {
        $abbrev = $commitInfo[2].Substring(0, 7)
        $packageVersion = ((($version -join '.'), "b", $revision, $abbrev) -join '-').Substring(1)
    }
    else {
        $packageVersion = ($version -join ".").Substring(1)
    }

    if (-not(Test-Path -Path $versionCsFolderPath)) {
        New-Item -ItemType directory -Path $versionCsFolderPath
    }
    "
[assembly: System.Reflection.AssemblyVersionAttribute(""$assemblyVersion"")]
[assembly: System.Reflection.AssemblyFileVersionAttribute(""$assemblyFileVersion"")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""$assemblyFileVersion"")]
    " | Out-File -FilePath $versionCsFilePath
    Write-Host "Version file saved to $versionCsFilePath" -ForegroundColor Green

    "
namespace AssemblyInfo
[<assembly: System.Reflection.AssemblyVersionAttribute(""$assemblyVersion"")>]
[<assembly: System.Reflection.AssemblyFileVersionAttribute(""$assemblyFileVersion"")>]
[<assembly: System.Reflection.AssemblyInformationalVersionAttribute(""$assemblyFileVersion"")>]
do ()
    " | Out-File -FilePath $versionFsFilePath
    Write-Host "Version file saved to $versionFsFilePath" -ForegroundColor Green    
}

Write-Host "Using package version $packageVersion, and assembly version $assemblyVersion, assembly file version $assemblyFileVersion"

foreach ($sln in (Get-ChildItem *.sln)) {
    Write-Host "Start building $($sln.FullName)"

    & dotnet restore $sln.FullName /p:Version=$packageVersion
    ProcessLastExitCode $lastexitcode "dotnet restore $($sln.FullName) /p:Version=$packageVersion"

    if ($os -eq "Windows") {
        & dotnet build $sln.FullName -c $configuration -v n /m:1
        ProcessLastExitCode $lastexitcode "dotnet build $($sln.FullName) -c $configuration -v n /m:1"
    }
 else {
        & msbuild $sln.FullName /p:Configuration=$configuration /verbosity:n /m:1
        ProcessLastExitCode $lastexitcode "msbuild $($sln.FullName) /p:Configuration=$configuration /verbosity:n /m:1"        
    }
}

if (-not $skipTests) {
    # Download test tools for UNIX
    if (-not ($os -eq "Windows")) {
        & $nugetCommand install xunit.runner.console -OutputDirectory TestTools -ExcludeVersion -Version 2.3.1
        ProcessLastExitCode $lastexitcode "$nugetCommand install xunit.runner.console -OutputDirectory TestTools -ExcludeVersion -Version 2.3.1"
    }

    # Run unit test cases
    Write-Host "Start running unit test"
    $exclude = @("docfx.E2E.Tests.csproj", "NetCoreLibProject.fsproj", "NetCoreProject.fsproj")
    foreach ($proj in (Get-ChildItem "test" -Exclude $exclude -Include @("*.csproj", "*.fsproj") -Recurse)) {
        if ($os -eq "Windows") {
            & dotnet test $proj.FullName --no-build -c $configuration
            ProcessLastExitCode $lastexitcode "dotnet test $($proj.FullName) --no-build -c $configuration"
        }
        else {
            & mono ./TestTools/xunit.runner.console/tools/net452/xunit.console.exe "$($proj.DirectoryName)/bin/$configuration/$framework/$($proj.BaseName).dll"
            ProcessLastExitCode $lastexitcode "mono ./TestTools/xunit.runner.console/tools/net452/xunit.console.exe '$($proj.DirectoryName)/bin/$configuration/$framework/$($proj.BaseName).dll'"
        }
    }
}

# dotnet pack first
foreach ($proj in (Get-ChildItem -Path ("src", "plugins") -Include *.[cf]sproj -Exclude 'docfx.msbuild.csproj' -Recurse)) {
    if ($os -eq "Windows") {
        & dotnet pack $proj.FullName -c $configuration -o $scriptHome/artifacts/$configuration --no-build /p:Version=$packageVersion
        ProcessLastExitCode $lastexitcode "dotnet pack $($proj.FullName) -c $configuration -o $scriptHome/artifacts/$configuration --no-build /p:Version=$packageVersion"
    }
 else {
        & nuget pack $($proj.FullName) -Properties Configuration=$configuration -OutputDirectory $scriptHome/artifacts/$configuration -Version $packageVersion
        ProcessLastExitCode $lastexitcode "nuget pack $($proj.FullName) -Properties Configuration=$configuration -OutputDirectory $scriptHome/artifacts/$configuration -Version $packageVersion"
    }
}

# Pack docfx.console
$docfxTarget = "target/$configuration/docfx";
if (-not(Test-Path -path $docfxTarget)) {
    New-Item $docfxTarget -Type Directory
}

Copy-Item -Path "src/nuspec/docfx.console/build" -Destination $docfxTarget -Force -Recurse
Copy-Item -Path "src/nuspec/docfx.console/content" -Destination $docfxTarget -Force -Recurse

$packages = @{
    "docfx"                     = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/docfx.console/docfx.console.nuspec");
    };
    "AzureMarkdownRewriterTool" = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/AzureMarkdownRewriterTool/AzureMarkdownRewriterTool.nuspec");
    };
    "DfmHttpService"            = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/DfmHttpService/DfmHttpService.nuspec");
    };
    "MergeDeveloperComments"    = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/MergeDeveloperComments/MergeDeveloperComments.nuspec");
    };
    "MergeSourceInfo"           = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/MergeSourceInfo/MergeSourceInfo.nuspec");
    };
    "TocConverter"              = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/TocConverter/TocConverter.nuspec");
    };
    "MarkdownMigrateTool"       = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/MarkdownMigrateTool/MarkdownMigrateTool.nuspec");
    };
    "YamlSplitter"              = @{
        "proj"    = $null;
        "nuspecs" = @("src/nuspec/YamlSplitter/YamlSplitter.nuspec");
    };
}

# Pack plugins and tools
foreach ($proj in (Get-ChildItem -Path ("src", "plugins", "tools") -Include *.csproj -Recurse)) {
    $name = $proj.BaseName
    if ($packages.ContainsKey($name)) {
        $packages[$name].proj = $proj
    }
    $nuspecs = Join-Path $proj.DirectoryName "*.nuspec" -Resolve
    if ($nuspecs -ne $null) {
        if ($packages.ContainsKey($name)) {
            $packages[$name].nuspecs = $packages[$name].nuspecs + $nuspecs
        }
        else {
            $packages[$name] = @{
                nuspecs = $nuspecs;
                proj    = $proj;
            }
        }
    }
}

foreach ($name in $packages.Keys) {
    $val = $packages[$name]
    $proj = $val.proj

    if ($proj -eq $null) {
        Write-Host $package
        ProcessLastExitCode 1 "$name does not have project found"
    }

    $outputFolder = "$scriptHome/target/$configuration/$name"
    # publish to target folder before pack
    & dotnet publish $proj.FullName -c $configuration -f $framework -o $outputFolder
    ProcessLastExitCode $lastexitcode "dotnet publish $($proj.FullName) -c $configuration -f $framework -o $outputFolder"

    $nuspecs = $val.nuspecs
    foreach ($nuspec in $nuspecs) {
        NugetPack $outputFolder $nuspec $packageVersion
    }
}

Write-Host "Build succeeds." -ForegroundColor Green
Pop-Location

