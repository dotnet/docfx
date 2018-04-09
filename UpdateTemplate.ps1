$ErrorActionPreference = "Stop"

# Check if node exists globally
if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: UpdateTemplate.sh requires node installed globally."
    exit 1
}

Push-Location $PSScriptRoot

$TemplateHome="$PSScriptRoot/src/docfx.website.themes/"
$DefaultTemplate="${TemplateHome}default/"
$GulpCommand="${DefaultTemplate}node_modules/gulp/bin/gulp"

Set-Location "$DefaultTemplate"
npm install
node ./node_modules/bower/bin/bower install
node "$GulpCommand"

Set-Location "$TemplateHome"
npm install
node "$GulpCommand"

Pop-Location
