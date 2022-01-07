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

%ExePath% *_SICstats.txt /p:TMT16_10ppm_ReporterTol0.003Da_2019-10-07.xml /o:Compare

%ExePath% *_SICstats.txt /o:Compare_OxyPlot

:Done
