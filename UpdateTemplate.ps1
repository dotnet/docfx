$ErrorActionPreference = "Stop"

# Check if node exists globally
if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: UpdateTemplate.sh requires node installed globally."
    exit 1
}

# https://github.com/PowerShell/Phosphor/issues/26#issuecomment-299702987
$logLevelParam = if ($env:TF_BUILD -eq "True") { "--loglevel=error" } else { "" }

Push-Location $PSScriptRoot

$TemplateHome="$PSScriptRoot/src/docfx.website.themes/"
$DefaultTemplate="${TemplateHome}default/"
$GulpCommand="${DefaultTemplate}node_modules/gulp/bin/gulp"

Set-Location "$DefaultTemplate"
npm install $logLevelParam
node ./node_modules/bower/bin/bower install $logLevelParam
node "$GulpCommand"

Set-Location "$TemplateHome"
npm install $logLevelParam
node "$GulpCommand"

Pop-Location
