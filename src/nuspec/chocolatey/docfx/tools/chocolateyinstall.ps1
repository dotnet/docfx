$ErrorActionPreference = 'Stop';

$packageName= 'DocFX'
$version    = 'v2.1'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = "https://github.com/dotnet/docfx/releases/download/$version/docfx.zip"
$hash       = '7abd6dc579bdea1a74bed7beac1a770d13a88d8bcc44fadf509b8d5400fe1333'

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  url           = $url
  checksum      = $hash
  checksumType  = 'SHA256'
}

Install-ChocolateyZipPackage @packageArgs
