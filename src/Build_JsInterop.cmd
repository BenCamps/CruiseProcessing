::Boilderplate
@ECHO OFF
SETLOCAL ENABLEEXTENSIONS

::name of this script
SET me=%~n0
::directory of script
SET parent=%~dp0

ECHO %me%
::::::::::::::

SET msbuild="%parent%tools\msbuild.cmd"


IF NOT DEFINED build_config SET build_config="Release"

dotnet build 	"%parent%\CruiseProcessing.Processing.JsInterop\CruiseProcessing.Processing.JsInterop.csproj" ^
				--no-incremental ^
				-c:%build_config% ^
				-p:SolutionDir=%parent%\

::End Boilderplate
EXIT /B %errorlevel%