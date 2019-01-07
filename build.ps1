function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

function getBranchName() {
    $branchPrefix = "refs/heads/"
    # https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=vsts&tabs=yaml%2Cbatch#working-with-variables
    $branch = $env:BUILD_SOURCEBRANCH
    if ([string]::IsNullOrEmpty($branch)) {
        # Local detached head
        return & {git rev-parse --abbrev-ref HEAD}
    }
    if ($branch.StartsWith($branchPrefix)) {
        return $branch.SubString($branchPrefix.length)
    }
    return $branch
}

function runTests() {
    try {
        pushd test/docfx.Test

        Remove-Item ./TestResults -Force -Recurse -ErrorAction Ignore

        exec "dotnet test -c Debug"
        exec "dotnet test -c Release --logger trx"
        exec "dotnet reportgenerator -reports:coverage.cobertura.xml -reporttypes:HtmlInline_AzurePipelines -targetdir:TestResults/cobertura"

        # Check test coverage
        $coverage = Select-Xml -Path 'coverage.cobertura.xml' -XPath "//package[@name='docfx']" | select -exp Node | select -exp line-rate
        if ($coverage -lt 0.8) {
            throw ("Test code coverage MUST be > 0.8, but is now only $coverage")
        }
    } finally {
        popd
    }
}

function checkSchema() {
    exec "dotnet run -p tools/CreateJsonSchema"
}

function createNuGetPackage() {
    # Create NuGet package
    $commitSha = & { git describe --always }
    $commitCount = & { git rev-list --count HEAD }
    $revision = $commitCount.ToString().PadLeft(5, '0')
    $branch = getBranchName

    if ($branch -eq "v3") {
        # CI triggered by v3
        $version = "3.0.0-beta-$revision-$commitSha"
    } else {
        # Local run
        $branch = $branch.Replace('/', '-')
        $version = "3.0.0-alpha-$branch-$revision-$commitSha"
    }

    Remove-Item ./drop -Force -Recurse -ErrorAction Ignore
    exec "dotnet pack src\docfx -c Release -o drop /p:Version=$version /p:InformationalVersion=$version"
    exec "dotnet tool install docfx --version 3.0.0-* --add-source drop --tool-path drop"
    exec ".\drop\docfx --version"
}

runTests
checkSchema
createNuGetPackage
