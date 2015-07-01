@ECHO OFF
PUSHD %~dp0

SETLOCAL

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
WHERE %DnuExe%

IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: build.cmd requires dnu installed gloablly. 
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

:RestorePackage
:: Restore inside each subfolder
FOR /D %%x IN ("src","docs","test") DO (
PUSHD %%x
CMD /C %DnuExe% restore --parallel
POPD
)

SET CachedNuget=%LocalAppData%\NuGet\NuGet.exe

IF EXIST %CachedNuget% GOTO COPYNUGET
ECHO Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet MD %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CachedNuget%'"

:COPYNUGET
IF EXIST .NuGet\NuGet.exe GOTO RESTORE
MD .NuGet
COPY %CachedNuget% .NuGet\NuGet.exe

:RESTORE
:: Currently has corpnet dependency
.NuGet\NuGet.exe restore "%BuildProj%"

:Exit
POPD
ECHO.
ECHO Exit Code = %ERRORLEVEL%
EXIT /B %ERRORLEVEL%
