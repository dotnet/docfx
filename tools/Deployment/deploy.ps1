param(
    [switch] $main = $false,
    [ValidateSet('build', 'pack', 'release')]
    [string[]] $targets = 'build'
)

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\config.ps1"
. "$PSScriptRoot\deploy-tasks.ps1"
. "$homeDir\common.ps1"

# Validate if tools are installed
$gitCommand = "git"
$chocoCommand = "choco"
$gitCommand,$chocoCommand | Foreach-Object {
    if (-not (ValidateCommand $_)) {
        ProcessLastExitCode 1 "$_ is required however it is not installed."
    }
}
$nugetCommand = GetNuGetCommandWithValidation $(GetOperatingSystemName) $true

Write-Host $($targets -join ",")
# Run release tasks
try {
    switch ($targets) {
        "build" {
            # Remove artifact folder and target folder if exist
            RemovePath $docfx.targetFolder,$docfx.artifactsFolder
            # Run build.ps1
            $buildScriptPath = (Resolve-Path "$homeDir\build.ps1").Path
            if ($main) {
                & $buildScriptPath -prod -release
            } else {
                & $buildScriptPath -prod
            }  
            # Run e2e test
            & $gitCommand clone $docfx.docfxSeedRepoUrl $docfx.docfxSeedHome -q
            & $docfx.exe "$($docfx.docfxSeedHome)\docfx.json"
            & $docfx.exe $docfx.docfxJson
            # Update github pages for main build if there is any change
            if ($main) {
                RemovePath "$($docfx.siteFolder)\.git"
                & $gitCommand config --global core.autocrlf false
                & $gitCommand clone $docfx.httpsRepoUrl -b gh-pages docfxsite -q
                Copy-item "docfxsite\.git" -Destination $docfx.siteFolder -Recurse -Force
                Push-Location $docfx.siteFolder
                $stdout = & $gitCommand status --porcelain
                if ($stdout) {
                    & $gitCommand config user.name $git.name
                    & $gitCommand config user.email $git.email
                    $repoUrlWithToken = $docfx.httpsRepoUrlWithToken -f $env:TOKEN
                    & $gitCommand remote set-url origin $repoUrlWithToken
                    & $gitCommand add .
                    & $gitCommand commit -m $git.message -q
                    & $gitCommand push origin gh-pages -q
                } else {
                    Write-Host "Skipped updating gh-pages due to no local change." -ForegroundColor Yellow
                }
                Pop-Location
            }
        }
        "pack" {
            $packScriptPath = (Resolve-Path "$homeDir\pack.ps1").Path
            & $packScriptPath
        }
        "release" {
            if ($main) {
                if (IsReleaseNoteVersionChanged $gitCommand $docfx.releaseNotePath) 
                {
                    PackAssetZip $docfx.releaseFolder $docfx.assetZipPath
                    PublishToNuget $nugetCommand $nuget."nuget.org" $docfx.artifactsFolder $env:NUGETAPIKEY
                    PublishToGithub $docfx.assetZipPath $docfx.releaseNotePath $docfx.sshRepoUrl $env:TOKEN
                    PublishToChocolatey $chocoCommand $docfx.releaseNotePath $docfx.assetZipPath $choco.chocoScript $choco.nuspec $choco.homeDir $env:CHOCO_TOKEN
                } else {
                    Write-Host "`$releaseNotePath $($docfx.releaseNotePath) hasn't been changed. Ignore to publish package." -ForegroundColor Yellow
                }
            }
        }
    }
} catch {   
    ProcessLastExitCode 1 "Process failed: $_"
}