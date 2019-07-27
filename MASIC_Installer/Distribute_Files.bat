xcopy Output\MASIC_Installer.exe \\floyd\software\MASIC\ /D /Y
xcopy ..\Readme.md               \\floyd\software\MASIC\ /D /Y
xcopy ..\RevisionHistory.txt     \\floyd\software\MASIC\ /D /Y


xcopy ..\bin\MASIC.*                     \\floyd\software\MASIC\Exe_Only\ /D /Y
xcopy ..\bin\*.dll                       \\floyd\software\MASIC\Exe_Only\ /D /Y
xcopy ..\MASICBrowser\bin\MASICBrowser.* \\floyd\software\MASIC\Exe_Only\ /D /Y
xcopy ..\MASICBrowser\bin\*.dll          \\floyd\software\MASIC\Exe_Only\ /D /Y

pause
