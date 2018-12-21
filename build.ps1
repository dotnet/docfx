function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

# running tests
exec "dotnet run -p tools/CreateJsonSchema"
exec "dotnet test test\docfx.Test"
exec "dotnet test test\docfx.Test -c Release"

# packing
$featureBranchPrefix = "feature/"
$branch = & {git rev-parse --abbrev-ref HEAD}
$commitSha = & { git describe --always }
$commitCount = & { git rev-list --count HEAD }
$revision = $commitCount.ToString().PadLeft(5, '0')

if ($branch -eq "v3") {
    $version = "3.0.0-beta-$revision-$commitSha"
} elseif ($branch.StartsWith($featureBranchPrefix)) {
    $feature = $branch.SubString($featureBranchPrefix.length)
    $version = "3.0.0-alpha-$feature-$revision-$commitSha"
} else {
    exit 0
}

Remove-Item ./drop -Force -Recurse -ErrorAction Ignore
exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version"
