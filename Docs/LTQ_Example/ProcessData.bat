..\..\bin\Console\Debug\MASIC_Console.exe QC_Standards_Excerpt.mzXML /p:Default_2008-08-22.xml /o:Compare

xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
..\..\bin\Console\Debug\MASIC_Console.exe Compare_OxyPlot\*_SICStats.txt
