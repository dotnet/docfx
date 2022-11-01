$homeDir = (Resolve-Path "$PSScriptRoot\..\..").Path

$docfx = @{
    httpsRepoUrl = "https://github.com/dotnet/docfx.git"
    httpsRepoUrlWithToken = "https://{0}@github.com/dotnet/docfx.git"
    sshRepoUrl = "git@github.com-ci:dotnet/docfx.git"
    docfxSeedRepoUrl = "https://github.com/docascode/docfx-seed.git"
    docfxSeedHome = "$homeDir\test\docfx-seed"
    targetFolder = "$homeDir\target"
    artifactsFolder = "$homeDir\artifacts"
    exe = "$homeDir\target\Release\docfx\docfx.exe"
    releaseNotePath = "$homeDir\RELEASENOTE.md"
    releaseFolder = "$homeDir\target\Release\docfx"
    assetZipPath = "$homeDir\Documentation\tutorial\artifacts\docfx.zip"
    siteFolder = "$homeDir\Documentation\_site"
    docfxJson = "$homeDir\Documentation\docfx.json"
}

$choco = @{
    homeDir = "$homeDir\src\nuspec\chocolatey\docfx"
    nuspec = "$homeDir\src\nuspec\chocolatey\docfx\docfx.nuspec"
    chocoScript = "$homeDir\src\nuspec\chocolatey\docfx\tools\chocolateyinstall.ps1"
}

$nuget = @{
    "nuget.org" = "https://api.nuget.org/v3/index.json"
}

$git = @{
    name = "DocFX CI"
    email = "vscopbld@microsoft.com"
    message = "Update gh-pages"
}