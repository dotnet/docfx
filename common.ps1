function GetOperatingSystemName() 
{
    if ([environment]::OSVersion.Platform -eq "Win32NT") {
        return "Windows"
    }
    else {
        return "Linux"
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

function GetNuGetCommandWithValidation([string]$os, [bool]$downloadIfNotExist = $false) 
{
    $nugetCommand = $null
    if (ValidateCommand("nuget")) {
        $nugetCommand = "nuget"
    } elseIf ($os -eq "Windows") {
        $localNugetExe = "$env:LOCALAPPDATA\Nuget\nuget.exe"
        if (ValidateCommand($localNugetExe)) {
            $nugetCommand = $localNugetExe
        } elseIf ($downloadIfNotExist) {
            Write-Host "Downloading NuGet.exe..."
            mkdir -Path $(Split-Path $localNugetExe) -Force
            $ProgressPreference = 'SilentlyContinue'
            [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials
            
            # Pin Nuget version to v5.9.1 to workaround for Nuget issue: https://github.com/NuGet/Home/issues/11125
            # Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile $nugetCommand
            Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/v5.9.1/nuget.exe' -OutFile $localNugetExe
            $nugetCommand = $localNugetExe
        }
    }
    if ($nugetCommand) {
        Write-Host "Using Nuget Command: $nugetCommand, $(& $nugetCommand help | Select -First 1)"
        return $nugetCommand
    }
    ProcessLastExitCode 1 "Nuget is required however it is not installed."
}