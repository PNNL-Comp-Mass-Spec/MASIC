@echo off

rem Parameter files associated with the following batch files:
rem
rem Orbitrap_Example\LTQ-FT_10ppm_2014-08-06.xml
rem QExactive_Example_ThermoRaw\LTQ-FT_10ppm_Scans_45004-46000.xml
rem TMT_Example\TMT10_LTQ-FT_10ppm_ReporterTol0.003Da_2014-08-06.xml
rem QExactive_Example_mzML\LTQ-FT_10ppm_2014-08-06.xml
rem QExactive_Example\LTQ-FT_10ppm_2014-08-06.xml
rem QExactive_Example_ThermoRaw_Full\LTQ-FT_10ppm_2014-08-06.xml
rem MRM_Example_mzML_ChromAsSpectra\Default_2008-08-22_Scans5000to6000.xml
rem MRM_Example_mzML\ Default_2008-08-22.xml
rem MRM_Example_mzML\Default_2008-08-22_Scans5000to6000.xml
rem MRM_Example\Default_2008-08-22.xml
rem MRM_Example\Default_2008-08-22_Scans5000to6000.xml
rem Lumos_QC_Mam\TMT6_LTQ-FT_10ppm_ReporterTol0.015Da_SkipSimilarityTesting_Scans22000-23000_2019-09-05.xml
rem Lumos_Example\LTQ-FT_10ppm_2014-08-06.xml


echo.
echo About to run ProcessData.bat in various subdirectories
echo Make sure MASIC_Console.exe is up-to-date in
echo ..\bin\Console\Debug\MASIC_Console.exe
pause

cd LTQ_Example
call ProcessData.bat
cd ..


cd Lumos_Example
call ProcessData.bat
cd ..


cd Lumos_QC_Mam
call ProcessData.bat
cd ..


cd MRM_Example
call ProcessData.bat
cd ..


cd MRM_Example_mzML
call ProcessData.bat
cd ..


cd MRM_Example_mzML_ChromAsSpectra
call ProcessData.bat
cd ..

cd QEHFX01_PreMix_QC
call ProcessData_use_SICstats.bat
cd ..

cd QExactHF03_PreMix_QC
call ProcessData_use_SICstats.bat
cd ..

cd Orbitrap_Example
call ProcessData.bat
cd ..


cd QExactive_Example
call ProcessData.bat
cd ..


cd QExactive_Example_mzML
call ProcessData.bat
cd ..


cd QExactive_Example_ThermoRaw
call ProcessData.bat
cd ..


cd TMT_Example
call ProcessData.bat
cd ..


cd TMT_Example_Bad_Labeling
call ProcessData_use_SICstats.bat
cd ..


@echo off
echo.
echo.
echo ###########################################################
echo Running a full analysis; this will take several minutes
echo ###########################################################
echo.
echo.
pause

cd QExactive_Example_ThermoRaw_Full
call ProcessData.bat
cd ..
