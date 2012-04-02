@echo off

SET ROOT=%~d0%~p0%
SET BINARYDIR="%ROOT%build_output"
SET DEPLOYDIR="%ROOT%ReleaseBinaries"

IF EXIST %BINARYDIR% (
  rmdir /Q /S %BINARYDIR%
)
mkdir %BINARYDIR%

IF EXIST %DEPLOYDIR% (
  rmdir /Q /S %DEPLOYDIR%
)
mkdir %DEPLOYDIR%

%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %SOURCEDIR%FSWatcher.sln  /property:OutDir=%BINARYDIR%\;Configuration=Release /target:rebuild

copy %BINARYDIR%\FSWatcher.dll %DEPLOYDIR%\FSWatcher.dll
copy %BINARYDIR%\FSWatcher.pdb %DEPLOYDIR%\FSWatcher.pdb
copy %BINARYDIR%\FSWatcher.Console.exe %DEPLOYDIR%\FSWatcher.Console.exe
copy %BINARYDIR%\FSWatcher.Console.pdb %DEPLOYDIR%\FSWatcher.Console.pdb
