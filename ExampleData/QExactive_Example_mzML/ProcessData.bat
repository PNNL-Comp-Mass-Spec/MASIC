..\..\bin\Console\Debug\MASIC_Console.exe QC_Shew_18_02_Excerpt.mzML /p:LTQ-FT_10ppm_2014-08-06.xml /o:Compare

if not exist Compare_OxyPlot (mkdir Compare_OxyPlot)
xcopy Compare\*_SICstats.txt       Compare_OxyPlot\ /Y
xcopy Compare\*_ReporterIons.txt   Compare_OxyPlot\ /Y
xcopy Compare\*_ScanStats.txt      Compare_OxyPlot\ /Y
..\..\bin\Console\Debug\MASIC_Console.exe Compare_OxyPlot\*_SICStats.txt
