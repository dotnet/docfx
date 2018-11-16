function exec([string] $cmd) {
    Write-Host $cmd -ForegroundColor Green
    & ([scriptblock]::Create($cmd))
    if ($lastexitcode -ne 0) {
        throw ("Error: " + $cmd)
    }
}

# Disable prompt for credentials on build server
$env:GIT_TERMINAL_PROMPT = 0
$env:DOCFX_APPDATA_PATH = "D:/appdata"
$env:DOCFX_PATH = [System.IO.Directory]::GetCurrentDirectory()

[System.IO.Directory]::CreateDirectory('D:/docfx-impact')

pushd D:/docfx-impact

$devopsAuth = "-c http.https://ceapex.visualstudio.com.extraheader=""AUTHORIZATION: bearer $env:SYSTEM_ACCESS_TOKEN"""
$githubAuth = "-c http.https://github.com.extraheader=""AUTHORIZATION: basic $env:GITHUB_BASIC_AUTH"""

exec "git init"
git remote add origin https://ceapex.visualstudio.com/Engineering/_git/Docs.DocFX.Impact
exec "git $devopsAuth $githubAuth fetch --prune --progress"
exec "git checkout origin/master --force --progress"
exec "git $devopsAuth $githubAuth submodule update --init --progress"
exec "git clean -xdf"
exec "git status"

exec "npm install"
exec "npm run impact -- --push --pack"

popd
