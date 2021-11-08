"C:\Program Files\7-Zip\7z.exe" a Zipped\MASIC_Console_Program.zip ..\bin\Console\Release\MASIC_Console.exe ..\bin\Console\Release\MASIC_Console.pdb ..\bin\Console\Release\*.dll ..\bin\Console\Release\MASIC_Plotter.py ..\Readme.md  ..\RevisionHistory.txt ..\ExampleData\MASICParameters.xml ..\Lib\RawFileReaderLicense.doc ..\ExampleData\CustomMZList_AcqTime.txt ..\ExampleData\CustomMZList.txt ..\ExampleData\LTQ_Example\Default_2008-08-22.xml ..\ExampleData\QExactive_Example\LTQ-FT_10ppm_2014-08-06.xml

cd Zipped
"C:\Program Files\7-Zip\7z.exe" a MASIC_Installer.zip ..\Output\MASIC_Installer.exe ..\..\Readme.md
cd ..

pause
