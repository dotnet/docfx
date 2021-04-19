param([string] $configuration = "Release")

# Include
$scriptRoot = $($MyInvocation.MyCommand.Definition) | Split-Path
. "$scriptRoot/common.ps1"

$ErrorActionPreference = 'Stop'
$packageVersionFilePath = ".\package_version_temp.txt" # build.ps1 saves the package version to this temp file

if (Test-Path $packageVersionFilePath){
    $packageVersion = Get-Content -Path $packageVersionFilePath
    Write-Host "Package version: $packageVersion"
}
else{
    ProcessLastExitCode 1 "$packageVersionFilePath is not found. Please run build.ps1 to generate the file."
}

$os = GetOperatingSystemName
Write-Host "Running on OS $os"
$nugetCommand = GetNuGetCommand ($os)
$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath

$global:LASTEXITCODE = $null

Push-Location $scriptHome

function NugetPack {
    param($basepath, $nuspec, $version)
    if (Test-Path $nuspec) {
        & $nugetCommand pack $nuspec -Version $version -OutputDirectory artifacts/$configuration -BasePath $basepath
        ProcessLastExitCode $lastexitcode "$nugetCommand pack $nuspec -Version $version -OutputDirectory artifacts/$configuration -BasePath $basepath"
    }
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

# dotnet pack first
foreach ($proj in (Get-ChildItem -Path ("src", "plugins") -Include *.[cf]sproj -Exclude 'docfx.msbuild.csproj' -Recurse)) {
    if ($os -eq "Windows") {
        if ($proj.FullName -like "*.csproj"){
            & dotnet pack $proj.FullName -c $configuration --no-build -o $scriptHome/artifacts/$configuration /p:Version=$packageVersion /p:OutputPath=$scriptHome/target/$configuration/$($proj.BaseName)
            ProcessLastExitCode $lastexitcode "dotnet pack $($proj.FullName) -c $configuration --no-build -o $scriptHome/artifacts/$configuration /p:Version=$packageVersion /p:OutputPath=$scriptHome/target/$configuration/$($proj.BaseName)"    
        }
        else {
            & dotnet pack $proj.FullName -c $configuration -o $scriptHome/artifacts/$configuration /p:Version=$packageVersion
            ProcessLastExitCode $lastexitcode "dotnet pack $($proj.FullName) -c $configuration -o $scriptHome/artifacts/$configuration /p:Version=$packageVersion"
        }
    }
    else {
        & nuget pack $($proj.FullName) -Properties Configuration=$configuration -OutputDirectory $scriptHome/artifacts/$configuration -Version $packageVersion -BasePath $scriptHome/target/$configuration/$($proj.BaseName)
        ProcessLastExitCode $lastexitcode "nuget pack $($proj.FullName) -Properties Configuration=$configuration -OutputDirectory $scriptHome/artifacts/$configuration -Version $packageVersion -BasePath $scriptHome/target/$configuration/$($proj.BaseName)"
    }
}

# Pack docfx.console
$docfxTarget = "target/$configuration/docfx";
if (-not(Test-Path -path $docfxTarget)) {
    New-Item $docfxTarget -Type Directory
}

Copy-Item -Path "src/nuspec/docfx.console/build" -Destination $docfxTarget -Force -Recurse
Copy-Item -Path "src/nuspec/docfx.console/content" -Destination $docfxTarget -Force -Recurse
Copy-Item -Path "LICENSE" -Destination $docfxTarget -Force

$packages = @{
    "docfx" = @{
        "proj" = $null;
        "nuspecs" = @("src/nuspec/docfx.console/docfx.console.nuspec");
    };
    "MergeDeveloperComments" = @{
        "proj" = $null;
        "nuspecs" = @("src/nuspec/MergeDeveloperComments/MergeDeveloperComments.nuspec");
    };
    "MergeSourceInfo" = @{
        "proj" = $null;
        "nuspecs" = @("src/nuspec/MergeSourceInfo/MergeSourceInfo.nuspec");
    };
    "TocConverter" = @{
        "proj" = $null;
        "nuspecs" = @("src/nuspec/TocConverter/TocConverter.nuspec");
    };
    "MarkdownMigrateTool" = @{
        "proj" = $null;
        "nuspecs" = @("src/nuspec/MarkdownMigrateTool/MarkdownMigrateTool.nuspec");
    };
    "YamlSplitter" = @{
        "proj" = $null;
        "nuspecs" = @("src/nuspec/YamlSplitter/YamlSplitter.nuspec");
    };
    "SandcastleRefMapper" = @{
		"proj" = $null;
		"nuspecs" = @("src/nuspec/SandcastleRefMapper/SandcastleRefMapper.nuspec")
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
    $nuspecs = $val.nuspecs
    foreach ($nuspec in $nuspecs) {
        NugetPack $outputFolder $nuspec $packageVersion
    }
}

Write-Host "Pack succeeds." -ForegroundColor Green
Pop-Location