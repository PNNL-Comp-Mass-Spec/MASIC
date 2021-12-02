@echo off
echo Command line used with MSConvert:
echo C:\ProteoWizard\msconvert.exe --32 --mzML --srmAsSpectra --filter "scanNumber [5000,6000]" QC18PepsR1_4Apr18_legolas3.raw
echo.

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

%ExePath% QC18PepsR1_4Apr18.mzML /p:Default_2008-08-22_Scans5000to6000.xml /o:Compare_Excerpt

xcopy Compare_Excerpt\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare_Excerpt\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare_Excerpt\*_ScanStats.txt      Compare_OxyPlot\ /Y
%ExePath% Compare_OxyPlot\*_SICStats.txt

:Done
