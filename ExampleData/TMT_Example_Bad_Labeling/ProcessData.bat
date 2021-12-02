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

%ExePath% 03CPTAC_AYA_PreMixQC_PNNL_20200204_B2S3_08Feb20_Pippin_Rep-20-01-07_SICstats.txt /p:TMT10_LTQ-FT_10ppm_ReporterTol0.003Da_2014-08-06.xml /o:Compare

if not exist Compare_OxyPlot (mkdir Compare_OxyPlot)
%ExePath% 03CPTAC_AYA_PreMixQC_PNNL_20200204_B2S3_08Feb20_Pippin_Rep-20-01-07_SICstats.txt /o:Compare_OxyPlot

:Done
