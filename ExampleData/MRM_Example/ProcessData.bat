..\..\bin\Console\Debug\MASIC_Console.exe QC18PepsR1_4Apr18.raw /p:Default_2008-08-22.xml /o:Compare
rem ..\..\bin\Console\Release\MASIC_Console.exe QC18PepsR1_4Apr18.raw /p:Default_2008-08-22.xml /o:CompareRelease

xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
..\..\bin\Console\Debug\MASIC_Console.exe Compare_OxyPlot\*_SICStats.txt
