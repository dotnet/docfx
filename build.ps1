param(
    [string] $Configuration="Release" # Options: "Debug", "Release", "PROD"
)
$ErrorActionPreference = 'Stop'

$scriptPath = $MyInvocation.MyCommand.Path
$scriptHome = Split-Path $scriptPath

Push-Location $scriptHome

# Prerequisite
$VS140COMNTOOLS = $Env:VS140COMNTOOLS
if (Test-Path $VS140COMNTOOLS)
{
    $msbuild = "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
} else {
    Write-Host "build.cmd requires Visual Studio 2015."
    Exit 1
    Pop-Location
}

if ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)
{
   Write-Host "Unable to find dotnet.exe in your PATH"
   Write-Host "Please follow https://github.com/dotnet/cli to install .Net Command Line Interface."
   Exit 1
   Pop-Location
}

$logFile = "build.log"
if (Test-Path $logFile) {
    rm $logFile
}

# run dotnet cli
$srcHome = [IO.Path]::GetFullPath("src")
$toolHome = [IO.Path]::GetFullPath("tools")
$artifacts = [IO.Path]::GetFullPath("artifacts")

if (-Not (Test-Path($artifacts)))
{
    New-Item -ItemType directory -Path $artifacts
}

# update version
if ($Configuration -eq "PROD")
{
    Write-Host "Updating version for PROD environment"
    & .\UpdateVersion.cmd
    $Configuration = "Release"
    if($LASTEXITCODE -ne 0)
    {
        throw "ERROR: Error occurs when updating version"
        Pop-Location
    }
}

$docfxOutputSrc = Join-Path $srcHome "docfx\bin\$Configuration\*"
$docfxOutputDest = [IO.Path]::GetFullPath("target\$Configuration\docfx.cli")

foreach($folder in ($srcHome, $toolHome))
{
    # restore dependency
    dir $folder | ForEach-Object {
        $subProject = Join-Path $folder $_.name
        $projectJson = Join-Path $subProject "project.json"

        if (Test-Path($projectJson)) {
            pushd $subProject
            dotnet restore 3>&1 | Tee-Object -FilePath $logFile -Append
            popd
        }
    }

    # build & pack
    dir $folder | foreach-object {
        $subProject = Join-Path $folder $_.name
        $projectJson = Join-Path $subProject "project.json"
        $destTarget = Join-Path $artifacts $Configuration


        if (Test-Path($projectJson))
        {
            if (-Not (Test-Path($destTarget)))
            {
                New-Item -ItemType directory -Path $destTarget
            }
            dotnet pack $projectJson -c $Configuration -o $destTarget 3>&1 | Tee-Object -FilePath $logFile -Append
        }
    }
}

if (-Not (Test-Path($docfxOutputDest)))
{
    New-Item -ItemType directory -Path $docfxOutputDest
}
Copy-Item -Path $docfxOutputSrc -Destination $docfxOutputDest -Recurse -Force

# restore msbuild package
$sln = "All.sln"
$cacheFolder = "$Env:LocalAppData\NuGet\v2.8.6"
$cacheNuget = Join-Path $cacheFolder "NuGet.exe"
if (-Not (Test-Path $cacheNuget))
{
    Write-Host "Downloading NuGet.exe v2.8.6"
    if (-Not (Test-Path $cacheFolder))
    {
         New-Item -ItemType directory -Path $cacheFolder
    }
    $ProgressPreference = 'SilentlyContinue'
    [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials
    Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/v2.8.6/nuget.exe' -OutFile $cacheNuget
}
& $cacheNuget restore $sln

# run msbuild
$msbuldArgs = @($sln, "/p:Configuration=$Configuration", "/nologo", "/maxcpucount:1", "/verbosity:minimal", "/nodeReuse:false", "/fileloggerparameters:Verbosity=d")
& $msbuild $msbuldArgs 3>&1 | Tee-Object -FilePath $logFile -Append

cat $logFile | Write-Host -ForegroundColor "Yellow"

Pop-Location
