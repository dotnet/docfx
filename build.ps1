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
    publishBinaryPackages
}

function publishBinaryPackages() {
    $packagesBasePath = "$PSScriptRoot\drop\docfx-bin"
    New-Item -Path $packagesBasePath -ItemType "directory" -ErrorAction SilentlyContinue
    $stagingPath = "$packagesBasePath\staging"
    New-Item -Path $stagingPath -ItemType "directory" -ErrorAction SilentlyContinue

    $rids = @("win7-x64", "osx-x64", "linux-x64") # Microsoft.ChakraCore doesn't provide win-x64 runtime build, using win7-x64
    foreach ($rid in $rids) {
        $packageName = "docfx-$rid-$version"
        exec "dotnet publish src\docfx\docfx.csproj -c release -r $rid -o $packagesBasePath/$rid /p:Version=$version /p:InformationalVersion=$version /p:PackAsTool=false"
        Compress-Archive -Path "$packagesBasePath/$rid/*" -DestinationPath "$stagingPath/$packageName.zip" -Update
        New-Item -Path "$stagingPath" -Name "$packageName.zip.sha256" -Force -ItemType "file" -Value (Get-FileHash "$stagingPath/$packageName.zip").Hash
        Copy-Item "$stagingPath/$packageName.zip" "$stagingPath/docfx-$rid-latest.zip" 
        Copy-Item "$stagingPath/$packageName.zip.sha256" "$stagingPath/docfx-$rid-latest.zip.sha256" 
    }
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
    if ($env:BUILD_REASON -ne "PullRequest") {
        publish
        testNuGet
    }
} finally {
    popd
}
