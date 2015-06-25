param(
    [string]$gitVersion,
    [string]$xprojPath
)
$ErrorActionPreference = "Stop";

# gitVersion as similar to v0.3.0-251-g76143b9
if (!$gitVersion -or !$xprojPath) {
    return;
}
$rawVersion = $gitVersion.Trim().Split('-');
$versionStart = $rawVersion[0];
$first = "$versionStart".Split('.');
$second = '0';
if ($rawVersion.Length -gt 1){
    $second = $rawVersion[1];
}
$major = $first[0].Substring(1);
$minor = $first[1];
$version = "$major.$minor.$second.0";

$refname = "version";
Write-Host "Updating '$refname' to '$version' in '$xprojPath'";

try{
	$config = gc $xprojPath -raw | ConvertFrom-Json
	if ($config.PSObject.Properties[$refname]){
	 	$config.PSObject.Properties[$refname].Value = $version;
		ConvertTo-Json $config -Depth 6 | sc $xprojPath
	    Write-Host "Updated '$refname' to '$version' in '$xprojPath'";
	}
}catch{
    Write-Warning "Unable to update '$refname' of '$xprojPath' to '$version' : $_";
}