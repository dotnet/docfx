# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $MatrixPath,
    [Parameter(Mandatory)][string] $OutputDirectory,
    [string] $NightlyPackage,
    [string] $NuGetConfig,
    [string] $StableVersion,
    [string] $StableVersionReason,
    [string] $WorkDirectory,
    [switch] $SkipStable,
    [switch] $FailOnIncompatible
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if ($StableVersion -and ($StableVersion -notmatch '^\d+\.\d+\.\d+$' -or -not $StableVersionReason -or $SkipStable -or $env:GITHUB_ACTIONS -eq 'true')) { throw 'An explicit stable version requires a reason and is allowed only for local released-package measurements.' }
$matrix = @(Get-Content -LiteralPath $MatrixPath -Raw | ConvertFrom-Json)
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $OutputDirectory) -and @(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count -gt 0) { throw 'Output directory must be empty to prevent mixing evidence from different runs.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$logs = Join-Path $OutputDirectory 'logs'
New-Item -ItemType Directory -Force $logs | Out-Null
if ($NuGetConfig) { $NuGetConfig = (Resolve-Path -LiteralPath $NuGetConfig).Path }
if ($NightlyPackage) { $NightlyPackage = (Resolve-Path -LiteralPath $NightlyPackage).Path }
$work = if ($WorkDirectory) { [IO.Path]::GetFullPath($WorkDirectory) } else { Join-Path ([IO.Path]::GetTempPath()) "docfx-compatibility-$([Guid]::NewGuid().ToString('N'))" }
if (Test-Path -LiteralPath $work) { throw 'Work directory must not already exist; package and SDK selection require isolation.' }
New-Item -ItemType Directory $work | Out-Null
$previousRollForward = $env:DOTNET_ROLL_FORWARD
$previousPackages = $env:NUGET_PACKAGES
$previousMSBuildSDKsPath = $env:MSBuildSDKsPath
$env:DOTNET_ROLL_FORWARD = 'LatestPatch'
$env:NUGET_PACKAGES = Join-Path $work 'packages'
$env:MSBuildSDKsPath = $null
$rows = [Collections.Generic.List[object]]::new()
$tools = [Collections.Generic.List[object]]::new()
$sha = (git rev-parse HEAD).Trim()
$dirty = [bool] (git status --porcelain)
if ($dirty) {
    $sourceState = git status --porcelain=v1 --untracked-files=all
    $sourceState | Set-Content (Join-Path $logs 'source-state.log') -Encoding utf8
    Write-Warning ("Source tree is dirty:`n" + ($sourceState -join "`n"))
}
$measurementId = [Guid]::NewGuid().ToString('N')
$scenarios = @('basic', 'razor')

function Invoke-Logged([string[]] $Arguments, [string] $Log) {
    $output = (& dotnet @Arguments 2>&1 | Out-String)
    $code = $LASTEXITCODE
    Add-Content -LiteralPath $Log -Value ("dotnet $($Arguments -join ' ')`n$output") -Encoding utf8
    return @{ code = $code; text = $output }
}

function Install-Tool([string] $Channel, [string] $Package) {
    $tool = @{ channel = $Channel; version = $null; runtimeTfm = $null; packageSha256 = $null; dll = $null; error = $null; selection = $null }
    $log = Join-Path $logs "$Channel-install.log"
    try {
        $expectedVersion = $null
        $install = @('tool', 'install', 'docfx', '--framework', 'net10.0', '--tool-path', (Join-Path $work $Channel))
        if ($Channel -eq 'stable') {
            $tool.selection = @{ mode = 'latest-release'; requestedVersion = $null; reason = 'Latest stable GitHub release; no older-version fallback.' }
            if ($StableVersion) {
                $tool.selection = @{ mode = 'explicit-version'; requestedVersion = $StableVersion; reason = $StableVersionReason }
            } else {
                $discovery = (& node (Join-Path $PSScriptRoot 'report.mjs') stable-version 2>&1 | Out-String)
                $code = $LASTEXITCODE
                Add-Content -LiteralPath $log -Value $discovery
                if ($code -ne 0) { throw 'Could not discover the latest stable release; no package fallback was attempted.' }
                $tool.selection = $discovery | ConvertFrom-Json
            }
            $expectedVersion = $tool.selection.requestedVersion
            $install += @('--version', $expectedVersion)
        }
        if ($Package) {
            $archive = [IO.Compression.ZipFile]::OpenRead($Package)
            try {
                $entry = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
                if ($entry.Count -ne 1) { throw 'Expected one package manifest.' }
                $reader = [IO.StreamReader]::new($entry[0].Open())
                try { [xml] $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
                if ($manifest.package.metadata.id -ne 'docfx') { throw 'Not a DocFX tool package.' }
                $expectedVersion = [string] $manifest.package.metadata.version
            } finally { $archive.Dispose() }
            $config = Join-Path $work "$Channel.config"
            $feed = [Security.SecurityElement]::Escape([IO.Path]::GetDirectoryName($Package))
            "<configuration><packageSources><clear/><add key=`"exact-package`" value=`"$feed`"/></packageSources></configuration>" | Set-Content $config
            $install += @('--version', $expectedVersion, '--configfile', $config)
        } elseif ($NuGetConfig) {
            $install += @('--configfile', $NuGetConfig)
        }
        $result = Invoke-Logged $install $log
        if ($result.code -ne 0) { throw 'Tool installation failed; see installation log.' }
        $store = Join-Path $work "$Channel/.store/docfx"
        $versions = @(Get-ChildItem -LiteralPath $store -Directory)
        if ($versions.Count -ne 1) { throw 'Expected exactly one installed package version.' }
        if ($expectedVersion -ne $versions[0].Name) { throw 'Installed package version differs from the exact requested version.' }
        $tool.version = $versions[0].Name
        if ($Channel -eq 'stable' -and $tool.version.Contains('-')) { throw 'Stable installation selected a prerelease.' }
        $configs = @(Get-ChildItem -LiteralPath $store -Filter docfx.runtimeconfig.json -Recurse | Where-Object { $_.FullName -match '[\\/]tools[\\/]net10\.0[\\/]any[\\/]' })
        if ($configs.Count -ne 1) { throw 'Expected one installed net10.0 tool runtime configuration.' }
        $tool.runtimeTfm = (Get-Content $configs[0].FullName -Raw | ConvertFrom-Json).runtimeOptions.tfm
        $tool.dll = Join-Path $configs[0].DirectoryName 'docfx.dll'
        $nupkg = Join-Path $store "$($tool.version)/docfx/$($tool.version)/docfx.$($tool.version).nupkg"
        if (-not (Test-Path -LiteralPath $nupkg)) { throw 'Installed package archive is missing from the isolated tool store.' }
        $tool.packageSha256 = (Get-FileHash $nupkg -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($Package -and $tool.packageSha256 -ne (Get-FileHash $Package -Algorithm SHA256).Hash.ToLowerInvariant()) { throw 'Installed package bytes differ from supplied nightly package.' }
        $result = Invoke-Logged @($tool.dll, '--version') $log
        if ($result.code -ne 0 -or $result.text.Trim().Split('+')[0] -ne $tool.version) { throw 'Packaged tool could not run or reported a different version.' }
    } catch {
        $tool.error = $_.Exception.Message
        Add-Content -LiteralPath $log -Value $tool.error
    }
    return $tool
}

try {
    Push-Location $work
    try {
        Invoke-Logged @('--info') (Join-Path $logs 'environment.log') | Out-Null
        if (-not $SkipStable) { $tools.Add((Install-Tool 'stable' '')) }
        if ($NightlyPackage) { $tools.Add((Install-Tool 'nightly' $NightlyPackage)) }
        if ($tools.Count -eq 0) { throw 'Select at least one tool package.' }
    } finally { Pop-Location }

    foreach ($target in $matrix) {
        if ($target.sdk -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$' -or $target.projectTfm -notmatch '^net\d+\.0$') { throw 'Invalid SDK matrix entry.' }
        foreach ($tool in $tools) {
            foreach ($scenario in $scenarios) {
                $id = "$($tool.channel)-$($target.sdk)-$($target.projectTfm)-$scenario"
                $logName = "logs/$measurementId-$id.log"
                $log = Join-Path $OutputDirectory $logName
                $row = [ordered]@{
                    sdk = $target.sdk; selectedSdk = $null; projectTfm = $target.projectTfm
                    channel = $tool.channel; toolVersion = $tool.version; toolRuntimeTfm = $tool.runtimeTfm
                    packageSha256 = $tool.packageSha256; os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
                    scenario = $scenario; testedAt = [DateTime]::UtcNow.ToString('o'); outcome = 'infrastructure-error'
                    diagnostics = ''; log = $logName
                }
                $directory = Join-Path $work $id
                New-Item -ItemType Directory $directory | Out-Null
                Copy-Item (Join-Path $PSScriptRoot "fixtures/$scenario/*") $directory -Recurse
                "<Project><PropertyGroup><TargetFramework>$($target.projectTfm)</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>" | Set-Content (Join-Path $directory 'Directory.Build.props')
                '<Project />' | Set-Content (Join-Path $directory 'Directory.Build.targets')
                '<Project />' | Set-Content (Join-Path $directory 'Directory.Packages.props')
                @{ sdk = @{ version = $target.sdk; rollForward = 'disable'; allowPrerelease = $true } } | ConvertTo-Json | Set-Content (Join-Path $directory 'global.json')
                if ($NuGetConfig) { Copy-Item $NuGetConfig (Join-Path $directory 'NuGet.Config') }
                Push-Location $directory
                try {
                    $selection = Invoke-Logged @('--version') $log
                    if ($selection.code -ne 0) {
                        $row.outcome = 'unavailable'; $row.diagnostics = 'Requested SDK is not installed.'
                        continue
                    }
                    $row.selectedSdk = $selection.text.Trim()
                    if ($row.selectedSdk -ne $target.sdk) { throw 'Actual selected SDK does not match requested SDK.' }
                    if ($tool.error) { throw "$($tool.error) See logs/$($tool.channel)-install.log." }
                    $result = Invoke-Logged @('restore', '--nologo') $log
                    if ($result.code -ne 0) { throw 'Fixture restore failed; compatibility was not measured.' }
                    $result = Invoke-Logged @('build', '--no-restore', '--nologo', '-warnaserror') $log
                    if ($result.code -ne 0) { throw 'Fixture build failed; compatibility was not measured.' }
                    # Remove compiled output: metadata must run generators itself, not consume a prebuilt fixture DLL.
                    Remove-Item 'bin' -Recurse -Force
                    $project = @(Get-ChildItem -Filter '*.csproj')[0].Name
                    @{ metadata = @(@{ src = @(@{ files = @($project) }); dest = 'api'; properties = @{ TargetFramework = $target.projectTfm }; includePrivateMembers = $false }) } |
                        ConvertTo-Json -Depth 8 | Set-Content 'docfx.json'
                    $result = Invoke-Logged @($tool.dll, 'metadata', 'docfx.json', '--warningsAsErrors') $log
                    if ($result.code -ne 0 -or $result.text -match 'ReferencesNewerCompiler|CS9057|CS8785|CS0115') {
                        $row.outcome = 'incompatible'; $row.diagnostics = 'Metadata extraction or compiler/generator diagnostics failed; see log.'
                        continue
                    }
                    if (-not (Test-Path -LiteralPath 'api')) { throw 'API assertion failed: metadata directory is missing.' }
                    $yaml = (Get-ChildItem api -Filter '*.yml' | ForEach-Object { ((Get-Content $_.FullName -Raw) -split '(?m)^references:')[0] }) -join "`n"
                    $expected = if ($scenario -eq 'basic') { @('Compatibility.Basic.Calculator', 'Compatibility.Basic.Calculator.Add(System.Int32,System.Int32)') } else { @('Compatibility.Razor.Widget', 'Compatibility.Razor.Widget.Describe', 'Compatibility.Razor.WidgetBase', 'Compatibility.Razor.Widget.BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder)') }
                    foreach ($uid in $expected) {
                        if ($yaml -notmatch "(?m)^- uid: $([Regex]::Escape($uid))\r?$") { throw "API assertion failed: missing $uid." }
                        Add-Content -LiteralPath $log -Value "Verified API item: $uid"
                    }
                    if ($scenario -eq 'razor' -and $yaml -notmatch 'overridden: Compatibility\.Razor\.WidgetBase\.Describe') {
                        $row.outcome = 'incompatible'; $row.diagnostics = 'Generated inheritance/override relationship is missing from API output.'
                        continue
                    }
                    $row.outcome = 'passed'; $row.diagnostics = 'Fixture compiled; metadata and expected API relationships verified.'
                } catch {
                    $row.diagnostics = $_.Exception.Message
                    if ($row.diagnostics.StartsWith('API assertion failed:')) { $row.outcome = 'incompatible' }
                    Add-Content -LiteralPath $log -Value $row.diagnostics
                } finally {
                    Pop-Location
                    $rows.Add($row)
                    Write-Host "$id : $($row.outcome)"
                }
            }
        }
    }
} finally {
    $report = [ordered]@{
        schemaVersion = 1; generatedAt = [DateTime]::UtcNow.ToString('o')
        source = @{ repository = $(if ($env:GITHUB_REPOSITORY) { $env:GITHUB_REPOSITORY } else { 'local' }); sha = $sha
            dirty = $dirty
            runId = $(if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { $null }); runAttempt = $(if ($env:GITHUB_RUN_ATTEMPT) { $env:GITHUB_RUN_ATTEMPT } else { $null }) }
        stableSelection = $(if ($SkipStable) { $null } else { ($tools | Where-Object { $_.channel -eq 'stable' } | Select-Object -First 1).selection })
        matrix = $matrix; channels = @($tools | ForEach-Object { $_.channel }); scenarios = $scenarios; results = @($rows.ToArray())
    }
    $report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputDirectory 'compatibility-report.json') -Encoding utf8
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
    $env:NUGET_PACKAGES = $previousPackages
    $env:MSBuildSDKsPath = $previousMSBuildSDKsPath
    if (-not $WorkDirectory) { Remove-Item -LiteralPath $work -Recurse -Force }
}
# Use the report policy, not the last fixture's native exit code (also consumed by the Actions pwsh wrapper).
$checkArguments = @('check', (Join-Path $OutputDirectory 'compatibility-report.json'))
if ($FailOnIncompatible) { $checkArguments += '--strict' }
& node (Join-Path $PSScriptRoot 'report.mjs') @checkArguments
exit $LASTEXITCODE
