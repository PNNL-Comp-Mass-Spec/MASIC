@echo off

echo.
echo Copying to C:\DMS_Programs
xcopy MASIC.exe C:\DMS_Programs\MASIC /D /Y
xcopy *.dll     C:\DMS_Programs\MASIC /D /Y

echo.
echo Copying to \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution
xcopy MASIC.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy *.dll     \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y

echo.
pause