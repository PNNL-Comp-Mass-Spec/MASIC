@echo off
echo Command line used with MSConvert:
echo C:\ProteoWizard\msconvert.exe --32 --mzML --srmAsSpectra --filter "scanNumber [5000,6000]" QC18PepsR1_4Apr18_legolas3.raw
echo.
@echo on

..\..\bin\Console\Debug\MASIC_Console.exe QC18PepsR1_4Apr18.mzML /p:Default_2008-08-22_Scans5000to6000.xml /o:Compare_Excerpt

xcopy Compare_Excerpt\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare_Excerpt\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare_Excerpt\*_ScanStats.txt      Compare_OxyPlot\ /Y
..\..\bin\Console\Debug\MASIC_Console.exe Compare_OxyPlot\*_SICStats.txt
