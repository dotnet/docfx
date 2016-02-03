@ECHO OFF
PUSHD %~dp0

SETLOCAL
SETLOCAL ENABLEDELAYEDEXPANSION

IF NOT DEFINED VisualStudioVersion (
    IF DEFINED VS140COMNTOOLS (
        CALL "%VS140COMNTOOLS%\VsDevCmd.bat"
        GOTO :EnvSet
    )

    ECHO Error: build.cmd requires Visual Studio 2015.
    SET ERRORLEVEL=1
    GOTO :Exit
)

:EnvSet
SET BuildProj=%~dp0All.sln
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

SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe

:: node.js nuget wrapper requires nuget.exe path in %PATH%
SET PATH=%PATH%;%LocalAppData%\NuGet

:: Check if DNU exists globally
:: DNU is OPTIONAL
WHERE dnu >NUL
IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: DNU is not successfully configured.
    ECHO ERROR: Please follow http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm to install dnvm.
    ECHO ERROR: If dnvm is installed, use `dnvm list` to show available dnx runtime, and use `dnvm use` to select the default dnx runtime
    GOTO :Exit
)

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
ECHO.
ECHO === BUILD RESULT ===
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%BuildLog%" & cd >nul

:: Pull xunit test result from the log file
ECHO.
ECHO === TEST EXECUTION SUMMARY ===
findstr /ir /c:"Total:.*Failed.*Skipped.*Time.*" "%BuildLog%" & cd >nul

ECHO Exit Code: %BuildErrorLevel%
SET ERRORLEVEL=%BuildErrorLevel%

GOTO :Exit

:Build
%BuildPrefix% msbuild "%BuildProj%" /p:Configuration=%Configuration% /nologo /maxcpucount:1 /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=d;LogFile="%BuildLog%"; %BuildPostfix%
SET BuildErrorLevel=%ERRORLEVEL%
EXIT /B %ERRORLEVEL%

:RestorePackage

:RestoreDnuPackage
FOR /D %%x IN ("src","docs","test") DO (
PUSHD %%x
CMD /C dnu restore --parallel
POPD
)

:RestoreNormalPackage
SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe
IF EXIST "%CachedNuget%" GOTO :Restore
ECHO Downloading latest version of NuGet.exe...
IF NOT EXIST "%LocalAppData%\NuGet" MD "%LocalAppData%\NuGet"
powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CachedNuget%'"

:Restore
:: Currently has corpnet dependency
nuget restore "%BuildProj%"

:Exit
POPD
ECHO.

EXIT /B %ERRORLEVEL%
