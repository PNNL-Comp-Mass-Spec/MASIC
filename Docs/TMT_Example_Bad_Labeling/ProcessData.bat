..\..\bin\Console\Debug\MASIC_Console.exe 03CPTAC_AYA_PreMixQC_PNNL_20200204_B2S3_08Feb20_Pippin_Rep-20-01-07_SICstats.txt /p:TMT10_LTQ-FT_10ppm_ReporterTol0.003Da_2014-08-06.xml /o:Compare

if not exist Compare_OxyPlot (mkdir Compare_OxyPlot)
..\..\bin\Console\Debug\MASIC_Console.exe 03CPTAC_AYA_PreMixQC_PNNL_20200204_B2S3_08Feb20_Pippin_Rep-20-01-07_SICstats.txt /o:Compare_OxyPlot
