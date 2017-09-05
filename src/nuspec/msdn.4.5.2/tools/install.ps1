param($installPath, $toolsPath, $package, $project)
Write-Host $installPath
Write-Host $toolsPath
$configPath = ($project.FullName | split-path) + '/docfx.json';

# Set content to be relative path of docfx.json
$content = ($installPath -replace '\\','/')+ "/content/msdn.4.5.2.zip";

$root = $project.FullName | split-path
$current = $content
Push-Location .
Set-Location $root
$content = (Resolve-Path -relative $current) -replace '\\','/'
Pop-Location

$refname = "xref";
try{
  $config = gc $configPath -raw | ConvertFrom-Json
  if ($config.PSObject.Properties[$refname]){
    # Check if $content already exists
    $exists = $false;
    $configValue = $config.PSObject.Properties[$refname].Value;

    # DONOT use pipeline as it auto unbox one string item...
    foreach ($i in $configValue){
      if ($i -eq $content) {
          Write-Host "Already Exists: $i '$content'";
        $exists = $true;
        break;
      }
    }

    if (!$exists) {
        Write-Host "Adding '$content' to '$refname' section of '$configPath'";
      $config.externalReferences += $content;
      ConvertTo-Json $config -Depth 6 | sc $configPath
      Write-Host "Succuessfully added '$content' to '$refname' section of '$configPath'"
    } else {
        Write-Host "'$content' already exists in '$refname' section of '$configPath'";
    }
  } else{
      Write-Host "Creating '$refname' : ['$content'] to '$refname' section of '$configPath'"
      $config.build | Add-Member -name $refname -value @($content) -membertype NoteProperty
      ConvertTo-Json $config -Depth 6 | sc $configPath
  }}catch [System.Exception]{
  Write-Error "Unable to add '$content' to '$refname' section of '$configPath' : $_";
  Write-Host "To manaully set '$content' as '$refname', add '$content' to the '$refname' section of '$configPath'";
}
