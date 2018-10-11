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

Write-Host "Use docfx at: $env:DOCFX_PATH"

[System.IO.Directory]::CreateDirectory('D:/docfx-impact')

pushd D:/docfx-impact

$DevOpsPATBase64 = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$($env:DEVOPS_PAT)"))
$env:DEVOPS_GIT_AUTH = "-c http.https://ceapex.visualstudio.com.extraheader=""AUTHORIZATION: basic $DevOpsPATBase64"""

$GitHubPATBase64 = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("$($env:GITHUB_PAT)"))
$env:GITHUB_GIT_AUTH = "-c http.https://github.com.extraheader=""AUTHORIZATION: basic $GitHubPATBase64"""

exec "git init"
git remote add origin https://ceapex.visualstudio.com/Engineering/_git/Docs.DocFX.Impact
exec "git $env:GITHUB_GIT_AUTH $env:DEVOPS_GIT_AUTH fetch --progress"
exec "git checkout origin/impact-ci --force --progress"
exec "git $env:GITHUB_GIT_AUTH $env:DEVOPS_GIT_AUTH submodule update --init --progress"

exec "npm install"
exec "npm run impact -- --push"

popd
