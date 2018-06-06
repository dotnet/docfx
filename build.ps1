function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

# After first v3 release, $version is just `git describe`
$commitSha = & { git describe --always }
$commitCount = & { git rev-list --count HEAD }
$version = "3.0.0-preview1-$commitCount-$commitSha"

exec "dotnet test test\docfx.Test"
exec "dotnet test test\docfx.Test -c Release"
exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version /p:PackAsTool=true"
