function GetOperatingSystemName() 
{
    if ([environment]::OSVersion.Platform -eq "Win32NT") {
        return "Windows"
    }
    else {
        return "Linux"
    }
}

function GetNuGetCommand([string]$os) 
{
    if ($os -eq "Windows") {
        return "$env:LOCALAPPDATA/Nuget/Nuget.exe"
    }
    else {
        return "nuget"
    }
}

function ProcessLastExitCode {
    param($exitCode, $msg)
    if ($exitCode -eq 0) {
        Write-Host "Success: $msg
        " -ForegroundColor Green
    }
    else {
        Write-Host "Error $($exitCode): $msg
        " -ForegroundColor Red
        Pop-Location
        Exit 1
    }
}

function ValidateCommand {
    param($command)
    return (Get-Command $command -ErrorAction SilentlyContinue) -ne $null
}