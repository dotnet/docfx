param ([switch]$noTest = $false)

function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

function publish() {
    Remove-Item ./drop -Force -Recurse -ErrorAction Ignore
    exec "dotnet pack -c Release -o $PSScriptRoot\drop"
    publishBinaryPackages
}

function publishBinaryPackages() {
    $packagesBasePath = "$PSScriptRoot\drop\docfx-bin"
    New-Item -Path $packagesBasePath -ItemType "directory" -ErrorAction SilentlyContinue
    $stagingPath = "$packagesBasePath\staging"
    New-Item -Path $stagingPath -ItemType "directory" -ErrorAction SilentlyContinue

    $rids = @("win7-x64", "osx-x64", "linux-x64") # Microsoft.ChakraCore doesn't provide win-x64 runtime build, using win7-x64
    foreach ($rid in $rids) {
        exec "dotnet publish src\docfx\docfx.csproj -c release -r $rid -o $packagesBasePath/$rid /p:PackAsTool=false"
        if ($rid -eq "win7-x64") {
            $version = Invoke-Expression "$packagesBasePath/win7-x64/docfx.exe --version"
            Write-Host "package version: $version"
        }
        $packageName = "docfx-$rid-$version"
        Compress-Archive -Path "$packagesBasePath/$rid/*" -DestinationPath "$stagingPath/$packageName.zip" -Update
        New-Item -Path "$stagingPath" -Name "$packageName.zip.sha256" -Force -ItemType "file" -Value (Get-FileHash "$stagingPath/$packageName.zip").Hash
    }
}

try {
    pushd $PSScriptRoot
    if ($env:BUILD_REASON -ne "PullRequest") {
        publish
    }
} finally {
    popd
}
