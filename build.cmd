@ECHO OFF
CD %~dp0

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
SET DNU_EXE=dnu
SET _buildproj=%~dp0All.sln

:: Check if DNU exists globally
WHERE %DNU_EXE%

IF NOT '%ERRORLEVEL%'=='0' (
    ECHO ERROR: build.cmd requires dnu installed gloablly. 
    GOTO :Exit
)

:: Restore packages for .csproj projects

CALL :RestorePackage

:: Log build command line
SET _buildlog=%~dp0msbuild.log
SET _buildprefix=echo
SET _buildpostfix=^> "%_buildlog%"
CALL :Build %*

:: Build
SET _buildprefix=
SET _buildpostfix=
CALL :Build %*

GOTO :AfterBuild

:AfterBuild

:: Pull the build summary from the log file
ECHO.
ECHO === BUILD RESULT === 
findstr /ir /c:".*Warning(s)" /c:".*Error(s)" /c:"Time Elapsed.*" "%_buildlog%"

:: Pull xunit test result from the log file
ECHO.
ECHO === TEST EXECUTION SUMMARY === 
findstr /ir /c:"Total:.*Failed.*Skipped.*Time.*" "%_buildlog%"

GOTO :Exit

:Build
%_buildprefix% msbuild "%_buildproj%" /nologo /maxcpucount /verbosity:minimal /nodeReuse:false /fileloggerparameters:Verbosity=diag;LogFile="%_buildlog%";Append %* %_buildpostfix%

:RestorePackage
:: Restore inside each subfolder
FOR /D %%x IN ("src","docs","test") DO (
PUSHD %%x
CMD /C %DNU_EXE% restore --parallel
POPD
)

SET CACHED_NUGET=%LocalAppData%\NuGet\NuGet.exe

IF EXIST %CACHED_NUGET% GOTO COPYNUGET
ECHO Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet MD %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy UnRestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/nuget.exe' -OutFile '%CACHED_NUGET%'"

:COPYNUGET
IF EXIST .NuGet\NuGet.exe GOTO RESTORE
MD .NuGet
COPY %CACHED_NUGET% .NuGet\NuGet.exe

:RESTORE
:: Currently has corpnet dependency
.NuGet\NuGet.exe restore "%_buildproj%"

:Exit
ECHO.
ECHO Exit Code = %ERRORLEVEL%
EXIT /B %ERRORLEVEL%
