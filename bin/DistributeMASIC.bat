@echo off

echo.
echo Copying to C:\DMS_Programs
xcopy Console\Debug\MASIC_Console.exe C:\DMS_Programs\MASIC /D /Y
xcopy Console\Debug\*.dll             C:\DMS_Programs\MASIC /D /Y

echo.
echo Copying to \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution
xcopy Console\Debug\MASIC_Console.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy Console\Debug\*.dll             \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y

echo.
pause