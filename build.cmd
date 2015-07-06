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
SET DnuExe=dnu
SET BuildProj=%~dp0All.sln

:: Check if DNU exists globally
:: DNU is OPTIONAL
SET BuildDnxProjects=1
WHERE dnu

IF NOT '%ERRORLEVEL%'=='0' (
    ECHO WARNING: DNU is not installed globally, DNX related projects will not be built!
    SET BuildDnxProjects=0
    SET BuildProj=%~dp0NonDnx.sln
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

GOTO :AfterBuild

:AfterBuild

:: Pull the build summary from the log file
ECHO.
ECHO === BUILD RESULT === 
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%BuildLog%"

:: Pull xunit test result from the log file
ECHO.
ECHO === TEST EXECUTION SUMMARY === 
findstr /ir /c:"Total:.*Failed.*Skipped.*Time.*" "%BuildLog%"

GOTO :Exit

:Build
%BuildPrefix% msbuild "%BuildProj%" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%BuildLog%";Append %* %BuildPostfix%

GOTO :Exit

:RestorePackage
:: Restore inside each subfolder
IF '%BuildDnxProjects%'=='0' (
GOTO :RestoreNormalPackage
)

FOR /D %%x IN ("src","docs","test") DO (
PUSHD %%x
CMD /C "%DnuExe%" restore --parallel
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
"%CachedNuget%" restore "%BuildProj%"

:Exit
POPD
ECHO.
ECHO Exit Code = %ERRORLEVEL%
EXIT /B %ERRORLEVEL%
