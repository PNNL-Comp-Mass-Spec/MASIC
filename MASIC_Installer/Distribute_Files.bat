@echo off
echo.
echo Be sure to build in Release mode
pause
@echo on

xcopy Output\MASIC_Installer.exe \\floyd\software\MASIC\ /D /Y
xcopy ..\Readme.md               \\floyd\software\MASIC\ /D /Y
xcopy ..\RevisionHistory.txt     \\floyd\software\MASIC\ /D /Y


xcopy ..\bin\Release\MASIC.*                     \\floyd\software\MASIC\Exe_Only\ /D /Y
xcopy ..\bin\Release\*.dll                       \\floyd\software\MASIC\Exe_Only\ /D /Y
xcopy ..\MASICBrowser\bin\Release\MASICBrowser.* \\floyd\software\MASIC\Exe_Only\ /D /Y
xcopy ..\MASICBrowser\bin\Release\*.dll          \\floyd\software\MASIC\Exe_Only\ /D /Y

xcopy ..\bin\Console\Release\MASIC_Console.*     \\floyd\software\MASIC\Exe_Only\ /D /Y

@echo off
echo.
echo.
echo Copying to \\Proto-3\DMS_Programs_Dist\AnalysisToolManagerDistribution
pause

pushd ..\bin\
call DistributeMASIC.bat
popd
