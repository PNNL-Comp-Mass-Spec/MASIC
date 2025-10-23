@echo off
echo.
echo Be sure to build in Release mode
sleep 1

echo.
echo Copying to C:\DMS_Programs
@echo on

xcopy Console\Release\MASIC_Console.exe C:\DMS_Programs\MASIC /D /Y
xcopy Console\Release\MASIC_Console.pdb C:\DMS_Programs\MASIC /D /Y
xcopy Console\Release\*.dll             C:\DMS_Programs\MASIC /D /Y
xcopy Console\Release\*.py              C:\DMS_Programs\MASIC /D /Y
xcopy ..\Readme.md                      C:\DMS_Programs\MASIC /D /Y
xcopy ..\RevisionHistory.txt            C:\DMS_Programs\MASIC /D /Y

@echo off
echo.
echo Copying to \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution
@echo on

xcopy Console\Release\MASIC_Console.exe \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy Console\Release\MASIC_Console.pdb \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy Console\Release\*.dll             \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy Console\Release\*.py              \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy ..\Readme.md                      \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MASIC /D /Y
xcopy ..\RevisionHistory.txt            \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution\MASIC /D /Y

echo.
if not "%1"=="NoPause" pause
