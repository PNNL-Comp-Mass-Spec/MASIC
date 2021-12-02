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

%ExePath% TMT_QCPool_23Jun14.mzML /p:TMT10_LTQ-FT_10ppm_ReporterTol0.003Da_2014-08-06.xml /o:Compare

if not exist Compare_OxyPlot (mkdir Compare_OxyPlot)
xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
%ExePath% Compare_OxyPlot\*_SICStats.txt

:Done
