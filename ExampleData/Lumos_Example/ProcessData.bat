..\..\bin\Console\Debug\MASIC_Console.exe Angiotensin_AllScans.raw /p:LTQ-FT_10ppm_2014-08-06.xml /o:Compare

xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
..\..\bin\Console\Debug\MASIC_Console.exe Compare_OxyPlot\*_SICStats.txt
