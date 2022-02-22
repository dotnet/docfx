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
    return ("v$versionFromReleaseNote".ToLower() -ne $versionFromTag.ToLower())
}

function PackAssetZip {
    Param($releaseFolder, $assetZipPath)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.AppContext]::SetSwitch('Switch.System.IO.Compression.ZipFile.UseBackslash', $false)
    try {
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
    Get-ChildItem "$artifactsFolder/*.nupkg" -Recurse -Exclude "*.symbols.nupkg" | Foreach-Object -Parallel {
        & $using:nugetCommand push $_ $using:apiKey -Source $using:sourceUrl -SkipDuplicate
    }
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
    $version = GetVersionFromReleaseNote $releaseNotePath
    $nupkgName = "docfx.$version.nupkg"
    $hash = ($assetZipPath | Get-FileHash -Algorithm SHA256).Hash.ToLower()
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
    return Invoke-WebRequest @params
}

function UpdateGithubRelease {
    param($id, $description, $userAndRepo, $headers)
    $params = @{
        Method = "PATCH"
        Uri = "$gitApiBaseUrl/repos/$($userAndRepo)/releases/$id"
        Headers = $headers
        Body = $description | ConvertTo-Json
        ContentType = "application/json"
    }
    return Invoke-WebRequest @params
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
    return Invoke-WebRequest @params
}

function PublishGithubRelease {
    param($description, $userAndRepo, $headers)
    try {
        $latestReleaseInfo = GetGithubLatestRelease $userAndRepo $headers
    } catch {
        if ($_.Exception.Response.StatusCode -ne 404) {
            throw "Get github latest release failed($($_.Exception.Response.StatusCode.value__)): $($_.ErrorDetails.Message)"
        }
    }
    if ($latestReleaseInfo.Content) {
        $latestRelease = $latestReleaseInfo.Content | ConvertFrom-Json
        if ($latestRelease.tag_name -eq $description.tag_name) {
            return UpdateGithubRelease $latestRelease.id $description $userAndRepo $headers
        }
        Write-host $latestRelease.tag_name
    }
    return CreateGithubRelease $description $userAndRepo $headers
}

function DeleteAssetByUrl {
    param($assetUrl, $headers)
    $params = @{
        Method = "DELETE"
        Uri = $assetUrl
        Headers = $headers
    }
    Invoke-WebRequest @params
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
    param($assetZipPath, $userAndRepo, $headers)  
    $assetInfo = @{
        contentType = "application/zip"
        name = Split-Path $assetZipPath -leaf
        data = [System.IO.File]::ReadAllBytes($assetZipPath)
    }

    $latestReleaseInfo = GetGithubLatestRelease $userAndRepo $headers
    if ($latestReleaseInfo) {
        $latestRelease = $latestReleaseInfo.Content | ConvertFrom-Json
        $latestRelease.assets | Foreach-Object {
            if ($_.name -eq $assetInfo.name) {
                DeleteAssetByUrl $_.url $headers
            }
        }
        UploadAsset $latestRelease.id $assetInfo $userAndRepo $headers
    } else {
        throw "Cannot find any release to upload assets."
    }
}

function PublishToGithub {
    param($assetZipPath, $releaseNotePath, $sshRepoUrl, $token)

    $userAndRepo = GetUserAndRepoFromGitSshUrl $sshRepoUrl
    $headers = @{ 
        "Accept" = "application/vnd.github.v3+json"
        "Authorization" = "token $token"
    }

    $releaseDescription = GetReleaseDescription $releaseNotePath
    PublishGithubRelease $releaseDescription $userAndRepo $headers
    PublishGithubAssets $assetZipPath $userAndRepo $headers
}