$ErrorActionPreference = "Stop"

# Check if node exists globally
if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: UpdateTemplate.sh requires node installed globally."
    exit 1
}

# https://github.com/PowerShell/Phosphor/issues/26#issuecomment-299702987
$logLevelParam = if ($env:TF_BUILD -eq "True") { "--loglevel=error" } else { "" }

Push-Location $PSScriptRoot

$templateHome ="$PSScriptRoot\src\docfx.website.themes"
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

# Pack templates
$templateFiles = @('layout\', 'partials\', '*.js', '*.tmpl', '*.liquid', 'token.json')
$webpageFiles = @('fonts\', 'styles\', 'favicon.ico', 'logo.svg', 'search-stopwords.json')
$files = $TemplateFiles + $WebpageFiles
$packs =  @{
    common = @(
        @{ files = $files; }
    );
    default = @(
        @{ files = $files; cwd = 'common'; },
        @{ files = $files; }
    );
    'default(zh-cn)' = @(
        @{ files = $files; }
    );
    statictoc = @(
        @{ files = $files; cwd = 'common'; },
        @{ files = $files; cwd = 'default'; excluder = @('toc.html.*') },
        @{ files = $files }
    );
    'pdf.default' = @(
        @{ files = $files; cwd = 'common'; },
        @{ files = $templateFiles + 'fonts\'; cwd = 'default';},
        @{ files = $files } # Overrides the former one if file name is the same
    );
};
$packs.Keys | Foreach-Object -Parallel {
    $tempFolder = "$using:PSScriptRoot\src\docfx\Template\$_"
    $destPath =  "$using:PSScriptRoot\src\docfx\Template\$_.zip"
    $packs = $using:packs
    foreach ($fileGroup in $packs[$_]) {
        $baseDir = "$using:templateHome\$($fileGroup.cwd ?? $_)"
        Set-Location $baseDir
        $fileGroup.files | % { 
            if (Test-Path($_)) {
                Copy-Item -Path $_ -Destination (New-Item $tempFolder -Type Container -Force) -Exclude $fileGroup.excluder -Recurse -Force
            }
        }   
    }
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.AppContext]::SetSwitch('Switch.System.IO.Compression.ZipFile.UseBackslash', $false)
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempFolder, $destPath)
    Remove-Item -Path $tempFolder -Recurse -Force
}

Pop-Location