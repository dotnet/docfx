param ([switch]$noTest = $false)

# Create NuGet package
$commitSha = & { git rev-parse --short HEAD }
$commitCount = & { git rev-list --count HEAD }
$revision = $commitCount.ToString().PadLeft(5, '0')

# CI triggered by v3
$version = "3.0.0-beta-$revision-$commitSha"

function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

function test() {
    if ($noTest) {
        return
    }

    exec "dotnet test -c Release --logger trx /p:CollectCoverage=true /p:CoverletOutputFormat=opencover"

    if ($env:CODECOV_TOKEN) {
        exec "$env:USERPROFILE\.nuget\packages\codecov\1.9.0\tools\codecov.exe -f ./test/docfx.Test/coverage.opencover.xml"
    }
}

function publish() {
    Remove-Item ./drop -Force -Recurse -ErrorAction Ignore
    exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version"
    if ($env:BUILD_SOURCEBRANCH -eq "refs/heads/v3-release")
    {
        publishLocalBuildPackage
    }
}

function publishLocalBuildPackage() {
    $localBuildPackagePath = "$PSScriptRoot\drop\local-build-packages"
    $blobUrl = "https://opbuildstorageprod.blob.core.windows.net/docfx-local-build-packages/"
    $windowsRuntime = "win-x64"
    $osxRuntime = "osx-x64"
    Remove-Item $localBuildPackagePath -Force -Recurse -ErrorAction Ignore

    exec "dotnet publish src\docfx\docfx.csproj -c release -r $windowsRuntime -o $localBuildPackagePath/$windowsRuntime /p:Version=$version /p:InformationalVersion=$version"
    exec "dotnet publish src\docfx\docfx.csproj -c release -r $osxRuntime -o $localBuildPackagePath/$osxRuntime /p:Version=$version /p:InformationalVersion=$version"
    Compress-Archive "$localBuildPackagePath/$windowsRuntime" -DestinationPath "$localBuildPackagePath/$windowsRuntime-$version.zip"
    Compress-Archive "$localBuildPackagePath/$osxRuntime" -DestinationPath "$localBuildPackagePath/$osxRuntime-$version.zip"

    $windowsPackageHash = (Get-FileHash "$localBuildPackagePath/$windowsRuntime-$version.zip").Hash
    $osxPackageHash = (Get-FileHash "$localBuildPackagePath/$osxRuntime-$version.zip").Hash
    (
    @{id="docfx-$windowsRuntime";url="$blobUrl/$windowsRuntime-$version.zip";integrity=$windowsPackageHash},
    @{id="docfx-$osxRuntime";url="$blobUrl/$osxRuntime-$version.zip";integrity=$osxPackageHash}
    ) |
    ConvertTo-Json |
    Out-File -FilePath "$localBuildPackagePath/manifest-$version.json"
}



function testNuGet() {
    if ($noTest) {
        return
    }

    exec "dotnet tool install docfx --version $version --add-source drop --tool-path drop"
    exec "drop\docfx --version"
}

try {
    pushd $PSScriptRoot
    test
    publish
    testNuGet
    publishLocalBuildPackage
} finally {
    popd
}
