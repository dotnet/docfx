function RemovePath {
    param($pathToClean)
    $pathToClean | Foreach-Object {
        if (Test-Path $_) {
            Remove-Item $_ -Recurse -Force
            Write-Host "Removed $_"
        }
    }
}

function GetCurrentVersionFromGitTag {
    param($gitCommand)
    $stdout = & $gitCommand describe --abbrev=0 --tags
    return $stdout ? $stdout.Trim() : ''
}

function GetVersionFromReleaseNote {
    param($releaseNotePath)
    if (Test-Path -Path $releaseNotePath) {
        $regex = "\(Current\s+Version:\s+v([\d\.]+)\)"
        $match = [regex]::Match($(Get-Content $releaseNotePath), $regex)
        if ($match.Success -and ($match.Groups.Count -eq 2)) {
            return $match.Groups[1].Value.Trim();
        } else {
            throw "Can't parse version from `$releaseNotePath '$releaseNotePath' in current version part."
        }
    } else {
        throw "`$releaseNotePath '$releaseNotePath' doesn't exist."
    }
}

function IsReleaseNoteVersionChanged {
    param($gitCommand, $releaseNotePath)
    $versionFromTag = GetCurrentVersionFromGitTag $gitCommand
    $versionFromReleaseNote = GetVersionFromReleaseNote $releaseNotePath
    Write-Host "Version from tag is '$versionFromTag', version from release note is 'v$versionFromReleaseNote'"
    return ("v$versionFromReleaseNote".ToLower() -ne $versionFromTag.ToLower())
}

function PackAssetZip {
    Param($releaseFolder, $assetZipPath)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.AppContext]::SetSwitch('Switch.System.IO.Compression.ZipFile.UseBackslash', $false)
    try {
        Write-Host "Start packing asset zip.."
        $zip = [System.IO.Compression.ZipFile]::Open($assetZipPath, 'update')
        Get-ChildItem "$releaseFolder\*" -File -Exclude '*.xml','*.pdb' | Foreach-Object {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, (Split-Path $_.FullName -Leaf), [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } catch {
        throw "Failed to pack asset zip: $_"
    } finally {
        $zip.Dispose()
    }
}

function PublishToNuget {
    param($nugetCommand, $sourceUrl, $artifactsFolder, $apiKey = "anything")
    Write-Host "Start publishing packages to $sourceUrl.."
    Get-ChildItem "$artifactsFolder/*.nupkg" -Recurse -Exclude "*.symbols.nupkg" | Foreach-Object -Parallel {
        & $using:nugetCommand push $_ $using:apiKey -Source $using:sourceUrl -SkipDuplicate
    }
}

function PublishToAzureDevOps {
    param($nugetCommand, $sourceName, $sourceUrl, $artifactsFolder, $token)
    & $nugetCommand sources add -Name $sourceName -Source $sourceUrl -Username anything -Password $token
    PublishToNuget $nugetCommand $sourceUrl $artifactsFolder
}

function UpdateChocoConfig {
    param($chocoScriptPath, $chocoNuspecPath, $version, $hash)
    $chocoScript = Get-Content $chocoScriptPath -Encoding UTF8 -Raw
    $chocoScript = [Regex]::Replace($chocoScript, 'v[\d\.]+', "v$version")
    $chocoScript = [Regex]::Replace($chocoScript, '(\$hash\s*=\s*[''"])([\d\w]+)([''"])', "`$hash       = '$hash'")
    $chocoScript | Set-Content $chocoScriptPath -Force -Encoding UTF8
    
    $chocoNuspec = Get-Content $chocoNuspecPath -Encoding UTF8 -Raw
    $chocoNuspec = [Regex]::Replace($chocoNuspec, '(<version>)[\d\.]+(<\/version>)', "<version>$version</version>")
    $chocoNuspec | Set-Content $chocoNuspecPath -Force -Encoding UTF8
}

function PublishToChocolatey {
    param($chocoCommand, $releaseNotePath, $assetZipPath, $chocoScript, $chocoNuspecPath, $chocoHomeDir, $token)
    Write-Host "Start publihsing to Chocolatey.."
    $version = GetVersionFromReleaseNote $releaseNotePath
    $nupkgName = "docfx.$version.nupkg"
    $hash = ($assetZipPath | Get-FileHash -Algorithm SHA256).Hash.ToLower()
    Write-Host "Use hash '$hash' for chocolatey package verification"
    UpdateChocoConfig $chocoScript $chocoNuspecPath $version $hash

    Push-Location $chocoHomeDir
    & $chocoCommand pack
    & $chocoCommand apiKey -k $token -source https://push.chocolatey.org/

    $chocoLogFile = "$PSScriptRoot\choco-push.log"
    & $chocoCommand push $nupkgName --log-file=$chocoLogFile
    if ($LastExitCode -ne 0) {
        Write-Host "choco push failed."
        if (Test-Path $chocoLogFile) {
            Write-Host "Get detailed errors from choco log:`r`n$(Get-Content $chocoLogFile -Raw -Encoding UTF8)"
        }
    }
    Pop-Location
}

function GetDescriptionFromReleaseNote {
    param($releaseNotePath)
    if (Test-Path -Path $releaseNotePath) {
        $regex = "\n\s*v[\d\.]+\s*\r?\n-{3,}\r?\n([\s\S]+?)(?:\r?\n\s*v[\d\.]+\s*\r?\n-{3,}|$)"
        $regexOptions = [Text.RegularExpressions.RegexOptions]::IgnoreCase
        $match = [Regex]::Match($(Get-Content $releaseNotePath -Raw), $regex, $regexOptions)
        if ($match.Success -and ($match.Groups.Count -eq 2)) {
            return $match.Groups[1].Value.Trim();
        } else {
            throw "Can't parse description from `$releaseNotePath '$releaseNotePath' in current version part."
        }
    } else {
        throw "`$releaseNotePath '$releaseNotePath' doesn't exist."
    }
}

function GetReleaseDescription {
    param($releaseNotePath)
    $version = GetVersionFromReleaseNote $releaseNotePath
    $description = GetDescriptionFromReleaseNote $releaseNotePath
    $releaseDescription = @{
        "tag_name" = "v$version"
        "target_commitish" = "main"
        "name" = "Version $version"
        "body" = $description
    }
    return $releaseDescription
}

$gitApiBaseUrl = "https://api.github.com"
function GetUserAndRepoFromGitSshUrl {
    param($url)
    $regex = "^git@(.+):(.+?)(\.git)?$"
    $match = [regex]::Match($url, $regex)
    if ($match.Success -and ($match.Groups.Count -eq 4)) {
        return $match.Groups[2].Value.Trim();
    } else {
        throw "Can't parse user and repo from '$url'"
    }
}

function GetGithubLatestRelease {
    param($userAndRepo, $headers)
    $params = @{
        Method = "GET"
        Uri = "$gitApiBaseUrl/repos/$($userAndRepo)/releases/latest"
        Headers = $headers
    }
    return Invoke-RestMethod @params
}

function CreateGithubRelease {
    param($description, $userAndRepo, $headers)
    $params = @{
        Method = "POST"
        Uri = "$gitApiBaseUrl/repos/$($userAndRepo)/releases"
        Headers = $headers
        Body = $description | ConvertTo-Json
        ContentType = "application/json"
    }
    return Invoke-RestMethod @params
}

function PublishGithubRelease {
    param($description, $userAndRepo, $headers)
    try {
        Write-Host "Getting latest github release.."
        $latestRelease = GetGithubLatestRelease $userAndRepo $headers
    } catch {
        if ($_.Exception.Response.StatusCode -ne 404) {
            throw "Get github latest release failed($($_.Exception.Response.StatusCode.value__)): $($_.ErrorDetails.Message)"
        }
    }
    if ($latestRelease.tag_name -eq $description.tag_name) {
        throw "The release to create '$($description.tag_name)' has already existed on Github: '$($latestRelease.tag_name)' with id '$($latestRelease.id)'"
    }
    Write-Host "Latest release is '$($latestRelease.tag_name)', creating new github release '$($description.tag_name)'.."
    $release = CreateGithubRelease $description $userAndRepo $headers
    return $release.id
}

function UploadAsset {
    param($id, $assetInfo, $userAndRepo, $headers)
    $params = @{
        Uri = "https://uploads.github.com/repos/$userAndRepo/releases/$id/assets?name=$($assetInfo.name)"
        Method = 'POST'
        Headers = $headers
        ContentType = $assetInfo.contentType ?? 'application/zip'
        Body = $assetInfo.data
    }
    return Invoke-WebRequest @params
}

function PublishGithubAssets {
    param($assetZipPath, $releaseId, $userAndRepo, $headers)  
    $assetInfo = @{
        contentType = "application/zip"
        name = Split-Path $assetZipPath -leaf
        data = [System.IO.File]::ReadAllBytes($assetZipPath)
    }
    Write-Host "Uploading asset to release '$releaseId'.."
    UploadAsset $releaseId $assetInfo $userAndRepo $headers
}

function PublishToGithub {
    param($assetZipPath, $releaseNotePath, $sshRepoUrl, $token)

    $userAndRepo = GetUserAndRepoFromGitSshUrl $sshRepoUrl
    $headers = @{ 
        "Accept" = "application/vnd.github.v3+json"
        "Authorization" = "token $token"
    }

    $releaseDescription = GetReleaseDescription $releaseNotePath
    $releaseId = PublishGithubRelease $releaseDescription $userAndRepo $headers
    if ($releaseId) {
        PublishGithubAssets $assetZipPath $releaseToUploadAsset.id $userAndRepo $headers
    } else {
        throw "Invalid github release id '$releaseId' for release '$($releaseDescription.tag_name)'"
    }
}