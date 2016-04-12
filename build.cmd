@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

REM IF NOT DEFINED VisualStudioVersion (
REM     IF DEFINED VS140COMNTOOLS (
REM         CALL "%VS140COMNTOOLS%\VsDevCmd.bat"
REM         GOTO :EnvSet
REM     )

REM     ECHO Error: build.cmd requires Visual Studio 2015.
REM     SET ERRORLEVEL=1
REM     GOTO :Exit
REM )

WHERE dotnet >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: .Net Command Line Interface is not successfully configured.
    ECHO ERROR: Please follow https://github.com/dotnet/cli to install .Net Command Line Interface.
    GOTO :Exit
)

:EnvSet
REM SET BuildProj=%~dp0All.sln
SET Configuration=%1
IF '%Configuration%'=='' (
    SET Configuration=Release
)
SET Environment=%2
IF '%Environment%'=='PROD' (
    ECHO Updating version for PROD environment
    CALL UpdateVersion.cmd

    IF NOT '!ERRORLEVEL!'=='0' (
        ECHO ERROR: Error occurs when updating version
        GOTO :Exit
    )
)

:: Check if DNU exists globally
:: DNU is OPTIONAL
REM WHERE dnu >NUL
REM IF NOT '%ERRORLEVEL%'=='0' (
REM     ECHO ERROR: DNU is not successfully configured.
REM     ECHO ERROR: Please follow http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm to install dnvm.
REM     ECHO ERROR: If dnvm is installed, use `dnvm list` to show available dnx runtime, and use `dnvm use` to select the default dnx runtime
REM     GOTO :Exit
REM )

:: Restore packages for .csproj projects

CALL :RestorePackage

:: Log build command line
SET BuildLog=%~dp0msbuild.log
SET BuildPrefix=echo
SET BuildPostfix=^> "%BuildLog%"

CALL :Build %*

:: Build
SET BuildPrefix=
SET BuildPostfix=
CALL :Build %*

IF NOT '%ErrorLevel%'=='0' (
    GOTO :AfterBuild
)

POPD

:AfterBuild

:: Pull the build summary from the log file
REM ECHO.
REM ECHO === BUILD RESULT ===
REM findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%BuildLog%" & cd >nul

:: Pull xunit test result from the log file
REM ECHO.
REM ECHO === TEST EXECUTION SUMMARY ===
REM findstr /ir /c:"Total:.*Failed.*Skipped.*Time.*" "%BuildLog%" & cd >nul

ECHO Exit Code: %BuildErrorLevel%
SET ERRORLEVEL=%BuildErrorLevel%

GOTO :Exit

:Build
REM %BuildPrefix% msbuild "%BuildProj%" /p:Configuration=%Configuration% /nologo /maxcpucount:1 /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=d;LogFile="%BuildLog%"; %BuildPostfix%
FOR /D %%x IN ("src/*","tools/*") DO (
    PUSHD %%x
    CMD /C dotnet build -c Release
    POPD
)

SET BuildErrorLevel=%ERRORLEVEL%
EXIT /B %ERRORLEVEL%

:RestorePackage
FOR /D %%x IN ("src/*","tools/*") DO (
    PUSHD %%x
    CMD /C dotnet restore
    POPD
)

REM :RestoreDnuPackage
REM FOR /D %%x IN ("src","test","tools") DO (
REM     PUSHD %%x
REM     CMD /C dnu restore --parallel
REM     POPD
REM )

REM :RestoreNormalPackage
REM :: Currently version 3.3 is not compatible with our build, force to use v2.8.6
REM SET CachedNuget=%LocalAppData%\NuGet\v2.8.6\NuGet.exe
REM IF EXIST "%CachedNuget%" GOTO :Restore
REM ECHO Downloading NuGet.exe v2.8.6...
REM IF NOT EXIST "%LocalAppData%\NuGet\v2.8.6" MD "%LocalAppData%\NuGet\v2.8.6"
REM powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultCredentials; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/v2.8.6/nuget.exe' -OutFile '%CachedNuget%'"

REM IF NOT '%ErrorLevel%'=='0' (
REM     ECHO ERROR: Failed downloading NuGet.exe
REM     GOTO :Exit
REM )

REM :Restore
REM %CachedNuget% restore "%BuildProj%"

REM IF NOT '%ErrorLevel%'=='0' (
REM     ECHO ERROR: Error when restoring packages for %BuildProj%
REM     GOTO :Exit
REM )

:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
