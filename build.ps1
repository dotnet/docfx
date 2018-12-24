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
$featureBranchPrefix = "refs/heads/feature/"
$commitSha = & { git describe --always }
$commitCount = & { git rev-list --count HEAD }
$revision = $commitCount.ToString().PadLeft(5, '0')
$branch = & {git rev-parse --abbrev-ref HEAD}
if ([string]::IsNullOrEmpty($branch)) {
    #https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=vsts&tabs=yaml%2Cbatch#working-with-variables
    $branch = $env:BUILD_SOURCEBRANCH
}

if ($branch -eq "refs/heads/v3") {
    # CI triggered by v3
    $version = "3.0.0-beta-$revision-$commitSha"
} elseif ($branch.StartsWith($featureBranchPrefix)) {
    # CI triggered by feature/*
    $feature = $branch.SubString($featureBranchPrefix.length)
    $version = "3.0.0-alpha-$feature-$revision-$commitSha"
} else {
    # local run
    $branch = $branch.replace('/', '-')
    $version = "3.0.0-alpha-$revision-$commitSha"
}

Remove-Item ./drop -Force -Recurse -ErrorAction Ignore
exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version"
