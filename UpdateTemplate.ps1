#Requires -Version 7.0
$ErrorActionPreference = "Stop"

# Check if node exists globally
if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: UpdateTemplate.sh requires node installed globally."
    exit 1
}

# https://github.com/PowerShell/Phosphor/issues/26#issuecomment-299702987
$logLevelParam = if ($env:TF_BUILD -eq "True") { "--loglevel=error" } else { "" }

Push-Location $PSScriptRoot

$templateHome ="$PSScriptRoot\templates"
$defaultTemplate ="$templateHome\default"

# Prepare default templates
Set-Location $defaultTemplate
npm install $logLevelParam
$cleanCssCommand="$defaultTemplate\node_modules\clean-css-cli\bin\cleancss"
$terserCommand="$defaultTemplate\node_modules\terser\bin\terser"
$vendor = @{
    css = @(
        'node_modules\bootstrap\dist\css\bootstrap.css',
        'node_modules\highlightjs\styles\github-gist.css'
    );
    js = @(
        'node_modules\jquery\dist\jquery.min.js',
        'node_modules\bootstrap\dist\js\bootstrap.min.js',
        'node_modules\highlightjs\highlight.pack.min.js',
        'node_modules\@websanova\url\dist\url.min.js',
        'node_modules\twbs-pagination\jquery.twbsPagination.min.js',
        'node_modules\mark.js\dist\jquery.mark.min.js',
        'node_modules\anchor-js\anchor.min.js'
    );
    font = 'node_modules\bootstrap\dist\fonts\*';
    lunr = 'node_modules\lunr\lunr.js'
}
node $cleanCssCommand $Vendor.css -o ".\styles\docfx.vendor.css" --format "keep-breaks" -O2
node $terserCommand $Vendor.js -o ".\styles\docfx.vendor.js" --comments "false"
node $terserCommand $Vendor.lunr -o ".\styles\lunr.min.js" --comments "false"
Copy-Item -Path $vendor.font -Destination (New-Item ".\fonts" -Type Container -Force) -Force -Recurse
Copy-Item -Path $vendor.lunr -Destination (New-Item ".\styles" -Type Container -Force) -Force -Recurse


Pop-Location