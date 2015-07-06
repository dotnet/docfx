param($installPath, $toolsPath, $package, $project)
Write-Host $installPath
Write-Host $toolsPath
$configPath = ($project.FullName | split-path) + '/xdoc.json';

# Set content to be relative path of xdoc.json
$content = ($installPath -replace '\\','/')+ "/content/$($package.Id)";

$root = $project.FullName | split-path
$current = $content
$tmp = Get-Location
Set-Location $root
$content = (Resolve-Path -relative $current) -replace '\\','/'
Set-Location $tmp

$refname = "externalReferences";
try{
  $config = gc $configPath -raw | ConvertFrom-Json
  if ($config.PSObject.Properties[$refname]){
      Write-Host "Removing '$content' from '$refname' section of '$configPath'";
    $exists = $false;
    # Check if $content already exists
    $configValue = $config.PSObject.Properties[$refname].Value;
    $references = @();
    foreach ($i in $configValue){
      if ($i -eq $content) { $exists = $true; }
      else {
        $references += $i
      }
    }

    if ($exists) {
      $config.externalReferences = $references;
      ConvertTo-Json $config -Depth 6 | sc $configPath
      Write-Host "Succuessfully removed '$content' from '$refname' section of '$configPath'"
    } else{
      Write-Host "'$content' has already been removed from  '$refname' section of '$configPath'"
    }
  }
}catch [System.Exception]{
  Write-Error "Error removing '$content' from '$refname' section of '$configPath' : $_";
  Write-Host "Please manaully remove '$content' from the '$refname' section of '$configPath'";
}