function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

# After first v3 release, $version is just `git describe`
$version = & { git describe --always }
$version = "3.0.0-preview1-$version"

exec "dotnet test test\docfx.Test -c Release"
exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version"
