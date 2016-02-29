$ErrorActionPreference = 'Stop';

$packageName= 'DocFX'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://github.com/dotnet/docfx/releases/download/v1.4/docfx.zip'

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  url           = $url
  checksum      = 'B301E4B421B8CD6FCA8A3E915E8A9D3CA1A0ED0D'
  checksumType  = 'sha1'
}

Install-ChocolateyZipPackage @packageArgs
