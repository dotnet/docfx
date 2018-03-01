$ErrorActionPreference = 'Stop';

$packageName= 'DocFX'
$version    = 'v2.1'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/dotnet/docfx/releases/download/$version/docfx.zip"
$sha1       = 'B301E4B421B8CD6FCA8A3E915E8A9D3CA1A0ED0D'

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  url           = $url
  checksum      = $sha1
  checksumType  = 'sha1'
}

Install-ChocolateyZipPackage @packageArgs
