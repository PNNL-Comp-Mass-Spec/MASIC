@echo off
echo.
echo Be sure to build in Release mode
sleep 1

echo.
echo Copying to C:\DMS_Programs
@echo on

xcopy Console\Release\MASIC_Console.exe C:\DMS_Programs\MASIC /D /Y
xcopy Console\Release\*.dll             C:\DMS_Programs\MASIC /D /Y
xcopy Console\Release\*.py              C:\DMS_Programs\MASIC /D /Y
xcopy ..\Readme.md                      C:\DMS_Programs\MASIC /D /Y
xcopy ..\RevisionHistory.txt            C:\DMS_Programs\MASIC /D /Y

@echo off
echo.
echo Copying to \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution
@echo on

xcopy Console\Release\MASIC_Console.exe \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy Console\Release\*.dll             \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy Console\Release\*.py              \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy ..\Readme.md                      \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy ..\RevisionHistory.txt            \\pnl\projects\OmicsSW\DMS_Programs\AnalysisToolManagerDistribution\MASIC /D /Y

echo.
pause
