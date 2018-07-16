function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

$commitSha = & { git describe --always }
$commitCount = & { git rev-list --count HEAD }
$revision = $commitCount.ToString().PadLeft(5, '0')
$version = "3.0.0-alpha-$revision-$commitSha"

exec "git rev-parse --abbrev-ref HEAD"
exec "git config --get remote.origin.url"

exec "dotnet test test\docfx.Test"
exec "dotnet test test\docfx.Test -c Release"
exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version /p:PackAsTool=true"
