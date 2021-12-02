@echo off

set ExePath=MASIC_Console.exe

if exist %ExePath% goto DoWork
if exist ..\%ExePath% set ExePath=..\%ExePath% && goto DoWork
if exist ..\..\bin\Console\Debug\%ExePath% set ExePath=..\..\bin\Console\Debug\%ExePath% && goto DoWork

echo Executable not found: %ExePath%
goto Done

:DoWork
echo.
echo Processing with %ExePath%
echo.
@echo On

%ExePath% QC18PepsR1_4Apr18.raw /p:Default_2008-08-22.xml /o:Compare
rem ..\..\..\bin\Console\Release\MASIC_Console.exe QC18PepsR1_4Apr18.raw /p:Default_2008-08-22.xml /o:CompareRelease

xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
%ExePath% Compare_OxyPlot\*_SICStats.txt

:Done
