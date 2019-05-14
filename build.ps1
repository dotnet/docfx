param ([switch]$noTest = $false)

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

    try {
        pushd test/docfx.Test

        Remove-Item ./TestResults -Force -Recurse -ErrorAction Ignore

        exec "dotnet test -c Debug" 2>&1
        exec "dotnet test -c Release --logger trx" 2>&1
        exec "dotnet reportgenerator -reports:coverage.cobertura.xml -reporttypes:HtmlInline_AzurePipelines -targetdir:TestResults/cobertura"

        # Check test coverage
        $coverage = Select-Xml -Path 'coverage.cobertura.xml' -XPath "//package[@name='docfx']" | select -exp Node | select -exp line-rate
        if ($coverage -lt 0.9) {
            throw ("Test code coverage MUST be > 0.9, but is now only $coverage")
        }
    } finally {
        popd
    }
}

function publish() {
    # Create NuGet package
    $commitSha = & { git rev-parse --short HEAD }
    $commitCount = & { git rev-list --count HEAD }
    $revision = $commitCount.ToString().PadLeft(5, '0')

    # CI triggered by v3
    $version = "3.0.0-beta-$revision-$commitSha"

    Remove-Item ./drop -Force -Recurse -ErrorAction Ignore
    exec "dotnet pack src\docfx -c Release -o $PSScriptRoot\drop /p:Version=$version /p:InformationalVersion=$version"
    exec "dotnet drop\docfx\docfx.dll --version"
}

try {
    pushd $PSScriptRoot
    test
    publish
} finally {
    popd
}