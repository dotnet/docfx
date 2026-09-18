# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackagePath,
    [string] $PublishDirectory,
    [string[]] $Frameworks = @('net8.0', 'net9.0', 'net10.0')
)

$ErrorActionPreference = 'Stop'
$PackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$templateDirectory = Join-Path $PSScriptRoot '../src/Docfx.App/templates'
$expectedTemplates = @(Get-ChildItem -LiteralPath $templateDirectory -Recurse -File)
if ($expectedTemplates.Count -eq 0) {
    throw 'Build the site templates before verifying the package.'
}

function Assert-Condition([bool] $Condition, [string] $Message) {
    if (-not $Condition) {
        throw $Message
    }
}

function Invoke-Checked([string] $Executable, [string[]] $Arguments) {
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$Executable $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }
}

function Read-ZipText($Entry) {
    $reader = [IO.StreamReader]::new($Entry.Open())
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Test-Tool([string] $Executable, [string] $Directory) {
    New-Item -ItemType Directory -Path $Directory | Out-Null
    $versionOutput = Invoke-Checked $Executable @('--version')
    Assert-Condition (($versionOutput -join "`n").StartsWith($version)) 'The wrong tool version was executed.'
    $templates = @(Invoke-Checked $Executable @('template', 'list'))
    foreach ($name in @('default', 'modern', 'statictoc')) {
        Assert-Condition ($templates -contains $name) "The $name template is missing."
    }

    $exportDirectory = Join-Path $Directory 'exported'
    Invoke-Checked $Executable @('template', 'export', '--all', '--output', $exportDirectory)
    foreach ($file in $expectedTemplates) {
        $relativePath = [IO.Path]::GetRelativePath($templateDirectory, $file.FullName)
        $exported = Join-Path $exportDirectory $relativePath
        Assert-Condition (Test-Path -LiteralPath $exported -PathType Leaf) "Template export lost $relativePath."
        Assert-Condition ((Get-FileHash -LiteralPath $exported).Hash -eq (Get-FileHash -LiteralPath $file.FullName).Hash) "Template export changed $relativePath."
    }

    [IO.File]::WriteAllText((Join-Path $Directory 'index.md'), "# Packaged templates`n`n**Shared assets work.**")
    [IO.File]::WriteAllText((Join-Path $Directory 'toc.yml'), "- name: Home`n  href: index.md")
    $configPath = Join-Path $Directory 'docfx.json'
    @{
        build = @{
            content = @(@{ files = @('index.md', 'toc.yml') })
            template = @('default', 'modern')
            output = '_site'
            globalMetadata = @{ _enableSearch = $true; pdf = $true }
        }
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $configPath -Encoding utf8

    Invoke-Checked $Executable @('build', $configPath, '--warningsAsErrors')
    $html = Get-Content -LiteralPath (Join-Path $Directory '_site/index.html') -Raw
    Assert-Condition ($html.Contains('<strong>Shared assets work.</strong>')) 'The generated HTML lost its Markdown content.'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $Directory '_site/public/docfx.min.js')) 'The generated site lost its JavaScript assets.'
    Invoke-Checked $Executable @('pdf', $configPath, '--warningsAsErrors')
    $pdf = [IO.File]::ReadAllBytes((Join-Path $Directory '_site/toc.pdf'))
    Assert-Condition ($pdf.Length -gt 1024 -and [Text.Encoding]::ASCII.GetString($pdf, 0, 5) -eq '%PDF-') 'PDF generation did not produce a PDF document.'
}

$archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    $errors = [Collections.Generic.List[string]]::new()
    $templates = @($archive.Entries | Where-Object { $_.FullName.StartsWith('templates/') -and -not $_.FullName.EndsWith('/') })
    if ($templates.Count -ne $expectedTemplates.Count) {
        $errors.Add("Expected $($expectedTemplates.Count) shared template files, found $($templates.Count).")
    }
    $duplicateTemplates = @($archive.Entries | Where-Object { $_.FullName -match '^tools/[^/]+/any/templates/' })
    if ($duplicateTemplates.Count -ne 0) {
        $errors.Add("Found $($duplicateTemplates.Count) framework-specific template entries.")
    }
    foreach ($framework in @('net8.0', 'net9.0', 'net10.0')) {
        $entry = $archive.GetEntry("tools/$framework/any/docfx.runtimeconfig.json")
        if ($null -eq $entry) {
            $errors.Add("Missing runtime configuration for $framework.")
            continue
        }
        $runtimeConfig = Read-ZipText $entry | ConvertFrom-Json -AsHashtable
        if ($runtimeConfig.runtimeOptions.configProperties['Docfx.DotnetToolMode'] -ne $true) {
            $errors.Add("Shared-template lookup is not enabled for $framework.")
        }
    }
    if ($errors.Count -gt 0) {
        throw ($errors -join "`n")
    }
    foreach ($file in $expectedTemplates) {
        $relativePath = [IO.Path]::GetRelativePath($templateDirectory, $file.FullName).Replace('\', '/')
        Assert-Condition ($null -ne $archive.GetEntry("templates/$relativePath")) "The package lost templates/$relativePath."
    }
    [xml] $nuspec = Read-ZipText ($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') } | Select-Object -First 1)
    $packageId = $nuspec.package.metadata.id
    $version = $nuspec.package.metadata.version
}
finally {
    $archive.Dispose()
}

$temp = Join-Path ([IO.Path]::GetTempPath()) "docfx-package-test-$([Guid]::NewGuid().ToString('N'))"
$previousPackages = $env:NUGET_PACKAGES
$previousRollForward = $env:DOTNET_ROLL_FORWARD
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    # Isolate installs so another package with the same test version cannot satisfy the restore.
    $env:NUGET_PACKAGES = Join-Path $temp 'packages'
    $env:DOTNET_ROLL_FORWARD = 'LatestPatch'
    $feed = [Security.SecurityElement]::Escape([IO.Path]::GetDirectoryName($PackagePath))
    $nugetConfig = Join-Path $temp 'NuGet.Config'
    [IO.File]::WriteAllText($nugetConfig, "<configuration><packageSources><clear/><add key=`"test-package`" value=`"$feed`"/></packageSources></configuration>")

    foreach ($framework in $Frameworks) {
        Write-Host "Verifying installed $packageId $version on $framework"
        $toolDirectory = Join-Path $temp "tool-$framework"
        Invoke-Checked dotnet @('tool', 'install', $packageId, '--version', $version, '--framework', $framework,
            '--tool-path', $toolDirectory, '--configfile', $nugetConfig)
        $executable = Join-Path $toolDirectory $(if ($IsWindows) { 'docfx.exe' } else { 'docfx' })
        Test-Tool $executable (Join-Path $temp "site-$framework")
    }

    if ($PublishDirectory) {
        $PublishDirectory = (Resolve-Path -LiteralPath $PublishDirectory).Path
        $runtimeConfig = Get-Content -LiteralPath (Join-Path $PublishDirectory 'docfx.runtimeconfig.json') -Raw | ConvertFrom-Json -AsHashtable
        Assert-Condition ($runtimeConfig.runtimeOptions.configProperties['Docfx.DotnetToolMode'] -ne $true) 'Tool packaging mode leaked into the normal publish output.'
        Assert-Condition (Test-Path -LiteralPath (Join-Path $PublishDirectory 'templates/modern/public/docfx.min.js')) 'Normal publish output lost its local templates.'
        $executable = Join-Path $PublishDirectory $(if ($IsWindows) { 'docfx.exe' } else { 'docfx' })
        Test-Tool $executable (Join-Path $temp 'site-publish')
    }
    Write-Host "Verified shared templates, installed tools ($($Frameworks -join ', ')), and supplied publish output."
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
    Remove-Item -LiteralPath $temp -Recurse -Force
}
