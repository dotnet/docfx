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
    account = "openpublishbuild"
    releaseNotePath = "$homeDir\RELEASENOTE.md"
    releaseFolder = "$homeDir\target\Release\docfx"
    assetZipPath = "$homeDir\Documentation\tutorial\artifacts\docfx.zip"
    siteFolder = "$homeDir\Documentation\_site"
    docfxJson = "$homeDir\Documentation\docfx.json"
}

$azdevops = @{
    ppeName = "docs-build-v2-ppe"
    ppeUrl = "https://docfx.pkgs.visualstudio.com/docfx/_packaging/docs-build-v2-ppe/nuget/v3/index.json"
    prodName = "docs-build-v2-prod"
    prodUrl = "https://docfx.pkgs.visualstudio.com/docfx/_packaging/docs-build-v2-prod/nuget/v3/index.json"
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

$sync = @{
    fromBranch = "dev"
    targetBranch = "stable"
}