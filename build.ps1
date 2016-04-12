param(
    [string] $Configuration="Release"
)

if ((Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) -eq $null) 
{ 
   Write-Host "Unable to find dotnet.exe in your PATH"
   Write-Host "Please follow https://github.com/dotnet/cli to install .Net Command Line Interface."
   Exit 1
}

$srcHome = [IO.Path]::GetFullPath("src")
$toolHome = [IO.Path]::GetFullPath("tools")
$artifacts = [IO.Path]::GetFullPath("artifacts")

if ($Configuration -eq "PROD") 
{
    Write-Host "Updating version for PROD environment"
    & .\UpdateVersion.cmd
    if($LASTEXITCODE -ne 0) 
    { 
        throw "ERROR: Error occurs when updating version"
    }
}

foreach($folder in ($srcHome, $toolHome)) 
{
    # restore dependency
    pushd $folder
    dotnet restore
    popd
    
    # build & pack
    dir $folder | foreach-object {
        $project = [IO.Path]::Combine($folder, $_.name, "project.json")
        if (Test-Path($project))
        {
            $output = Join-Path $artifacts $_.name  
            $target45 = Join-Path $output "net45"
            $target462 = Join-Path $output "net462"
            dotnet build $project -c Release -o $target45 -f net45
            dotnet build $project -c Release -o $target462 -f net462       
            dotnet pack $project -o $output
        }
    }
}