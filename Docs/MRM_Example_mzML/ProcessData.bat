..\..\bin\Console\Debug\MASIC_Console.exe QC18PepsR1_4Apr18.mzML /p:Default_2008-08-22.xml /o:Compare

rem This dataset only has MS1 spectra; there is no neeed to test oxyplot

rem xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
rem xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
rem xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
rem ..\..\bin\Console\Debug\MASIC_Console.exe Compare_OxyPlot\*_SICStats.txt
