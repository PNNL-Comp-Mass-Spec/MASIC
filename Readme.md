# MASIC (MS/MS Automated Selected Ion Chromatogram Generator)

MASIC generates selected ion chromatograms (SICs) for all of the parent ions 
chosen for fragmentation in an LC-MS/MS analysis. The SICs are generated 
using the LC-MS data, then each SIC is processed using a peak finding 
algorithm to characterize the chromatographic peaks, providing peak statistics 
including elution time of the peak apex, peak area, and peak signal/noise.
The program can read instrument data in the following formats:
* Thermo .Raw files
* mzML files (see notes below)
* mzXML files
* mzData files
* .cdf/.mgf combo files

Results are saved both as flat files (.txt) and in an XML file that can be read 
using the accompanying graphical results browser, MASIC Browser. The browser 
provides a fast, graphical method for browsing the SICs identified by MASIC, 
allowing the user to sort and filter the SIC list as desired.

The application note describing MASIC is:
Monroe ME et. al., Comput Biol Chem. 2008 Jun;32(3):215-217. More info is on 
[PubMed](https://www.ncbi.nlm.nih.gov/pubmed/18440872)

## Downloads

Download a .zip file with the installer from:
https://github.com/PNNL-Comp-Mass-Spec/MASIC/releases

The release page also includes a .zip file with MASIC_Console.exe

## Installation

Double click file MASIC_Installer.exe file to install.\
The program shortcut can be found at Start Menu -> Programs -> PAST Toolkit -> MASIC\
The MASIC browser shortcut can be found at PAST Toolkit -> MASIC Browser

### Console Version

The GUI version of MASIC, file MASIC.exe, includes a command line interface.
To see the options, use `MASIC.exe /?`\
See below for more information on the command line arguments.

Alternatively, use program MASIC_Console.exe, which is a command-line only version of MASIC.
MASIC_Console.exe can be run on Linux using [Mono](https://www.mono-project.com/download/stable/)

## Custom SIC Values

You can optionally define custom values to instruct MASIC to create SICs of
specific m/z values centered around a certain time point in the input file.
You can define the custom values via three methods:
1. Using the Custom SIC Options tab in the GUI
2. Using a tab-delimited text file, which you select in the GUI 
   or via the "CustomMZFile" setting in the "CustomSICValues" section of a MASIC XML parameter file
3. Using the "MZList", "ScanCenterList", and "ScanCommentList" settings in the "CustomSICValues" section of a MASIC XML parameter file

For all three methods, you can define three types of time modes:
* Absolute: absolute scan number
* Relative: relative time ranging from 0 (the first scan) to 1 (the last scan)
* AcquisitionTime: scan acquisition time, aka elution time (in minutes)

When defining custom SIC values, you must provide, at a minimum, the m/z value
to search for. If no other values are provided, the default m/z search
tolerance will be used (defined on the SIC Options tab in the GUI), and the 
entire dataset will be searched for the m/z value. You can optionally also define:
1. A custom m/z tolerance to use for each m/z
  * If 0, the default SIC tolerance will be used
2. The central scan number or scan acquisition time you want the search to be centered around
  * If 0, will be set to the mid-point of the dataset
3. The scan or time tolerance to search forward or backward from the central scan or time
  * If 0,  will use the default scan or time tolerance
4. A comment to associate with each custom m/z search value

If you use the GUI to define the Custom SIC values, then the column titles shown in the 
Custom SIC Values table will change based on the Tolerance Type defined.

If you define a tab-delimited text file from which to read the Custom SIC 
search details, the column headers present in the text file define the 
search tolerances to use. Files CustomMZList.txt and CustomMZList_AcqTime.txt,
which are installed with MASIC, show the proper format for a Custom SIC Values
file. At a minimum, the file must have a column titled "MZ" which specifies
the custom m/z values to search for. If no other columns are present,
a default tolerance is used for the m/z tolerance and the entire file will be
searched for each custom m/z file. If you add other columns of data to the
file, additional tolerances can be defined. Allowable column names are:
MZToleranceDa, ScanCenter, ScanTolerance, and Comment

The data in columns ScanCenter and ScanTolerance can be absolute scan numbers, 
relative scan numbers, or acquisition time values. The mode to use can be
defined in the GUI using the "Tolerance Type" option on the Custom SIC Options
tab. Alternatively, if running MASIC from the Command Line, the mode to
use must be defined using the "ScanType" setting in section "CustomSICValues"
of an XML parameter file ("ScanType" can be "Absolute", "Relative", or 
"AcquisitionTime").

Instead of using column names ScanCenter and ScanTolerance you can alternatively
use the column names ScanTime and TimeTolerance. When these columns are present,
then the values in those columns will be treated as AcquisitionTime values,
regardless of the global custom SIC Tolerance Type setting defined in the GUI
or in the XML parameter file.

## Output File Columns

### _ScanStats.txt file columns

| Column Name                | Description                      |
|----------------------------|----------------------------------|
| Dataset                    | Dataset name                     |
| ScanNumber                 | Scan number                      |
| ScanTime                   | Elution time (minutes)           |
| ScanType                   | 1 for MS1, 2 for MS2             |
| TotalIonIntensity          | Total Ion Intensity (TIC)        |
| BasePeakIntensity          | Base Peak Intensity (BPI)        |
| BasePeakMZ                 | m/z value of the base peak       |
| BasePeakSignalToNoiseRatio | S/N of the base peak             |
| IonCount                   | Number of ions (after filtering) |
| IonCountRaw                | Number of ions                   |
| ScanTypeName               | HMS, HCD-HMSn, CID-MSn, etc.     |

### _SICStats.txt file columns

| Column Name                     | Description                                                 |
|---------------------------------|-------------------------------------------------------------|
| Dataset                         | Dataset name                                                |
| ParentIonIndex                  | Index                                                       |
| MZ                              | Parent ion m/z                                              |
| SurveyScanNumber                | Precursor scan number                                       |
| FragScanNumber                  | MS2 scan number                                             |
| OptimalPeakApexScanNumber       | Scan number of the peak apex                                |
| PeakApexOverrideParentIonIndex  | Index of the parent ion that this SIC inherits from; -1 if not inherited |
| CustomSICPeak                   | 1 if a peak for a custom m/z search value                   |
| PeakScanStart                   | SIC peak start scan number                                  |
| PeakScanEnd                     | SIC peak end scan number                                    |
| PeakScanMaxIntensity            | SIC peak center scan number                                 |
| PeakMaxIntensity                | Maximum intensity of the SIC peak                           |
| PeakSignalToNoiseRatio          | Signal to noise ratio (S/N)                                 |
| FWHMInScans                     | Full width half max, in scans                               |
| PeakArea                        | Area of the SIC peak                                        |
| ParentIonIntensity              | Intensity of the parent ion                                 |
| PeakBaselineNoiseLevel          | Baseline noise level surrounding the peak                   |
| PeakBaselineNoiseStDev          | Baseline noise Standard Deviation                           |
| PeakBaselinePointsUsed          | Number of points used to determine the baseline noise level |
| StatMomentsArea                 | Area, as computed using statistical moments                 |
| CenterOfMassScan                | Central scan number, as determined via the center of mass (from statistical moments)  |
| PeakStDev                       | Standard deviation, from statistical moments                |
| PeakSkew                        | Skew, from statistical moments                              |
| PeakKSStat                      | KSStat, from statistical moments                            |
| StatMomentsDataCountUsed        | Data points used by statistical moments calculations        |
| InterferenceScore               | Parent ion interference score, measuring the fraction of observed peaks in the parent ion isolation window that are from the precursor |

## Reporter Ions

Enable the "Generate Reporter Ion Stats" option to instruct MASIC to look 
for standard reporter ion masses and to save their observed intensities in
file _ReporterIons.txt. Supported reporter ion modes are shown in the following table
The integer value corresponds to the number that appears in the MASIC settings file, e.g.
`<item key="ReporterIonMassMode" value="16" />` for TMT 11

| Integer Value | Name                    | Description                                 | Ions (m/z values)                                                                                                                       |
|---------------|-------------------------|---------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| 1             | iTraq                   | Original 4-plex iTRAQ                       | 114.1112, 115.1083, 116.1116, and 117.115                                                                                               |
| 2             | ITraq ETD               | iTRAQ ETD tags                              | 101.107, 102.104, and 104.1107                                                                                                          |
| 5             | ITraq 8 (high res)      | High resolution eight-plex iTRAQ            | 113.107873, 114.111228, 115.108263, 116.111618, 117.114973, 118.112008, 119.115363, and 121.122072                                      |
| 6             | ITraq 8 (low res)       | Low resolution eight-plex iTRAQ             | Same ions as Mode 5, but corrects for contamination from the immonium ion at 120.08131                                                  |
| 3             | TMT 2                   | TMT duplex                                  | 126.1283 and 127.1316                                                                                                                   |
| 4             | TMT 6                   | 6-plex TMT                                  | 126.1283, 127.1316, 128.135, 129.1383, 130.1417, and 131.1387                                                                           |
| 10            | TMT 10                  | 10-plex TMT                                 | 126.127726, 127.124761, 127.131081, 128.128116, 128.134436, 129.131471, 129.13779, 130.134825, 130.141145, and 131.138180               |
| 16            | TMT 11                  | 11-plex TMT                                 | 126.127726, 127.124761, 127.131081, 128.128116, 128.134436, 129.131471, 129.13779, 130.134825, 130.141145, 131.138180, and 131.144499   |
| 18            | TMT 16                  | 16-plex TMT (aka TMTpro)                    | 126.127726, 127.124761, 127.131081, 128.128116, 128.134436, 129.131471, 129.13779, 130.134825, 130.141145, 131.13818, 131.144499, 132.141535, 132.147855, 133.14489, 133.15121 and 134.148245 |
| 7             | PCGalnaz                | Custom reporter ions for Josh Alfaro        | 300.13 and 503.21 m/z                                                                                                                   |
| 8             | HemeCFragment           | Custom reporter ions for Eric Merkley       | 616.1767 and 617.1845                                                                                                                   |
| 9             | LycAcetFragment         | Custom reporter ions for Ernesto Nakayasu   | 126.09134 and 127.094695                                                                                                                |
| 11            | OGlcNAc                 | O-GlcNAc                                    | 204.0872, 300.13079, and 503.21017                                                                                                      |
| 12            | Fracking Amine 20160217 | Product ions associated with a Fracking Fluid amine | 157.089, 170.097, and 234.059                                                                                                   |
| 13            | FSFACustomCarbonyl      | Custom product ions from Chengdong Xu       | 171.104, 236.074, and 257.088                                                                                                           |
| 14            | FSFACustomCarboxylic    | Custom product ions from Chengdong Xu       | 171.104, 234.058, and 336.174                                                                                                           |
| 15            | FSFACustomHydroxyl      | Custom product ions from Chengdong Xu       | 151.063 and 166.087                                                                                                                     |
| 17            | Acetylation             | Peptides with acetylated lysine residues    | 126.09134 and 143.11789                                                                                                                 |
| 19            | NativeOGlcNAc           | Native O-GlcNAc                             | 126.055, 138.055, 144.065, 168.066, 186.076, 204.087, and 366.14                                                                        |

## MRM Data

When processing Thermo-Finnigan MRM data files, a file named _MRMSettings.txt 
will be created listing the parent and daughter m/z values monitored via 
selected reaction monitoring. You can optionally export detailed MRM 
intensity data using the "MRM Data List" and/or "MRM Intensity Crosstab" 
options. The MRM Data List file is a long, narrow file with only a few
columns: Scan, MRM_Parent_MZ, MRM_Daughter_MZ, and MRM_Daughter_Intensity.
In contrast, the MRM Intensity Crosstab file is a wide, rectangular file
that presents the data in a crosstab format (aka PivotTable). Here, each
column in the crosstab corresponds to the intensity values over time for a 
given parent m/z to daughter m/z transition being monitored.

If you convert a MRM file to the .mzML format using the MSConvert utility in ProteoWizard, 
you need to use command line switches `--srmAsSpectra` and `--simAsSpectra`.
Otherwise, the PSI_Interface utility which we use to read .mzML files cannot 
load the mass spectra data. Example command line:
```
msconvert.exe --32 --mzML --srmAsSpectra --simAsSpectra DatasetName.raw
```

## IonCountRaw vs. IonCount

In the ScanStats file, IonCountRaw is the number MASIC m/z values present in 
each spectrum. IonCount is the number of points that MASIC keeps in memory and 
examines when creating SICs. For LTQ-FT data with scans containing lots of high 
resolution data, MASIC compresses the data a bit to combine m/z values that are 
within ~0.05 m/z (thus, combine 1000.05 and 1000.052 as simply 1000.051).
Additionally, for scans with a lot of low quality, low intensity data, MASIC 
discards the low intensity data. The IonCount value would let you see which 
scans MASIC is discarding some data, for whatever reason.

## .mzML Support

Although MASIC can read .mzML files, when possible, you should process Thermo raw files directly with MASIC. 
The reason is that the m/z and intensity values obtained using ThermoRawFileReader.dll and ThermoFisher.CommonCore.Data.dll 
more accurately represent the true data, in particular for centroided data obtained from a profile mode scan.

The following observations are based on comparing results from .mzML files created using the following options
with a Thermo .raw file that has profile mode MS1 and MS2 scans

Profile mode .mzML file
```
msconvert.exe --32 --mzML DatasetName.raw
```

Centroid mode .mzML file
```
msconvert.exe --32 --mzML --filter "peakPicking true 1-" DatasetName.raw
```

Peak intensities (and thus areas) reported by MASIC
* .mzML profile  mode data has intensities and areas nearly identical to Thermo .raw files
* .mzML centroid mode data has intensities and areas ~2.5 fold smaller than Thermo .raw files

Reporter Ion Intensities
* .mzML profile  mode data has identical reporter ion intensities vs. Thermo .raw files
* .mzML centroid mode data has similar reporter ion intensities vs. Thermo .raw files (agreement within 2%)

Precursor Ion Interference Scores
* .mzML profile  mode data has similar interference scores only for higher abundance precursors; for lower abundance precursors, the centroiding algorithm does not perform well (m/z values deviate from their true values), and thus the interference scores do not correlate well with Thermo .raw file based scores
* .mzML centroid mode data has identical interference scores as Thermo .raw files

Based on the above observations, when reading .mzML files, centroided MS1 spectra work better for
interference score calculations, but intensities are 2.5 fold smaller. In contrast,
profile mode MS2 spectra more accurately represent reporter ion intensities.

MSConvert supports creating files with centroided MS1 spectra and profile mode MS2 spectra:
```
msconvert.exe --32 --mzML --filter "peakPicking true 1" DatasetName.raw
```

## Plots

The `PlotOptions` section in the MASIC parameter file has options for instructing MASIC to create various plots. When enabled, the following plots are created:

| Plot Title                            | Filename                               | Description                                                                                                                                                                    |
|---------------------------------------|----------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Peak area histogram                   | Dataset_PeakAreaHistogram.png          | Histogram of selected ion chromatogram (SIC) peak areas. Areas are log-10 transformed.                                                                                         |
| Peak width histogram                  | Dataset_PeakWidthHistogram.png         | Histogram of peak widths (FWHM) of selected ion chromatogram peaks (in seconds)                                                                                                |
| Reporter ion observation rate, top N% | Dataset_RepIonObsRateHighAbundance.png | Bar chart showing the percentage of MS/MS spectra for which a reporter ion was observed. Uses data from the top 80% of spectra, as sorted by SIC peak area (% is adjustable)   |
| Reporter ion observation rate         | Dataset_RepIonObsRate.png              | Bar chart showing the percentage of MS/MS spectra for which a reporter ion was observed, showing a separate bar for each channel.                                              |
| Reporter ion intensity stats, top N%  | Dataset_RepIonStatsHighAbundance.png   | Box and whiskers plot showing the distribution of reporter ion intensities, by channel. Uses data from the top 80% of spectra, as sorted by SIC peak area (% is adjustable).   |

The reporter ion based plots are only created if reporter ions were searched for.

Example plots are visible on GitHub
* [TMT Example](https://htmlpreview.github.io/?https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/ExampleData/TMT_Example/index.html)
* [Incomplete TMT labeling example](https://htmlpreview.github.io/?https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/ExampleData/TMT_Example_Bad_Labeling/index.html)
* [QExactive Example](https://htmlpreview.github.io/?https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/ExampleData/QExactive_Example/index.html)
* [QExactive HFX Example](https://htmlpreview.github.io/?https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/ExampleData/QEHFX01_PreMix_QC/index.html)
* [QExactive HF Example](https://htmlpreview.github.io/?https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/ExampleData/QExactHF03_PreMix_QC/index.html)


By default, plots are created using OxyPlot, which only works on Windows

To create plots with Python instead of OxyPlot, set `PlotWithPython` to `True` in the parameter file
* For example, see [TMT11_LTQ-FT_10ppm_ReporterTol0.003Da_2017-03-17.xml](https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/ExampleData/Parameter_Files/TMT11_LTQ-FT_10ppm_ReporterTol0.003Da_2017-03-17.xml)

On Windows, MASIC looks for `python.exe` in directories that start with "Python3" or "Python 3", searching below:
* C:\Program Files
* C:\Program Files (x86)
* C:\Users\Username\AppData\Local\Programs
* C:\ProgramData\Anaconda3
* C:\

On Linux, assumes Python is at `/usr/bin/python3`

Python plotting requires that three libraries be installed
* numpy
* matplotlib
* pandas

For Python library installation options, see the `Python_Setup.txt` file on GitHub
* https://github.com/PNNL-Comp-Mass-Spec/MASIC/blob/master/Python/Python_Setup.txt

The plot data can optionally be saved as tab-delimited text files
* Enable by setting `SaveHistogramData` and/or `SaveReporterIonObservationRateData` to True in the parameter file

Plots can be created from existing MASIC results by providing the _SICStats.txt or _ScanStats.txt file name as the input file

## Command Line Interface

Both MASIC.exe and MASIC_Console.exe include the same command line interface.
```
MASIC.exe
 /I:InputFilePath [/O:OutputDirectoryPath]
 [/P:ParamFilePath] [/D:DatasetID or DatasetLookupFilePath]
 [/S:[MaxLevel]] [/A:AlternateOutputDirectoryPath] [/R]
 [/L:[LogFilePath]] [/LogDir:LogDirPath] [/SF:StatusFileName] 
 [/CreateParamFile:FileName.xml] [/Q]
```

The input file path can contain the wildcard character *
* It will typically point to an instrument data file (e.g. Thermo .raw file)

If the input file name ends with _SICstats.txt, MASIC will create plots using existing results
* If a _ReporterIons.txt file is also present, the Reporter Ion Observation Rate plots will also be made

The output directory name is optional. If omitted, the output files will be
created in the same directory as the input file. If included, a subdirectory
will be created with the name OutputDirectoryName.

The parameter file switch `/P` is optional. If supplied, it should point to a valid 
MASIC XML parameter file. If omitted, defaults are used.
* Create a parameter file for MASIC using the MASIC GUI
* Alternatively, download a file from [GitHub](https://github.com/PNNL-Comp-Mass-Spec/MASIC/tree/master/ExampleData/Parameter_Files)

The `/D` switch can be used to specify the Dataset ID of the input file; if
omitted, 0 will be used

Alternatively, a lookup file can be specified with the `/D` switch (useful if
processing multiple files using * or `/S`). The lookup file is a comma, space, or
tab delimited file with two columns:\
Dataset Name and Dataset ID

Use `/S` to process all valid files in the input directory and subdirectories.
Include a number after `/S` (like `/S:2`) to limit the level of subdirectories to
examine.
* When using `/S`, you can redirect the output of the results using `/A`.
* When using `/S`, you can use `/R` to re-create the input directory hierarchy in the
alternate output directory (if defined).

Use `/L` to specify that a log file should be created. Use `/L:LogFilePath` to
specify the name (or full path) for the log file.

Use `/SF` to specify the name to use for the MASIC Status file (default is
MasicStatus.xml).

Use `/CreateParamFile` to create an example parameter file named MASIC_ExampleSettings.xml
* Include a colon and a filename to customize the filename

The optional `/Q` switch will prevent the progress window from being shown (only applicable to the GUI version, MASIC.exe)

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)\
Copyright 2019, Battelle Memorial Institute. All Rights Reserved.\
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov\
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics/

## License

MASIC is licensed under the 2-Clause BSD License; you may not use this program 
except in compliance with the License. You may obtain a copy of the License at 
https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved.
