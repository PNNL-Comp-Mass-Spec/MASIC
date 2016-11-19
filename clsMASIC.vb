Option Strict On

' This class will read an LC-MS/MS data file and create selected ion chromatograms
'   for each of the parent ion masses chosen for fragmentation
' It will create several output files, including a BPI for the survey scan,
'   a BPI for the fragmentation scans, an XML file containing the SIC data
'   for each parent ion, and a "flat file" ready for import into the database
'   containing summaries of the SIC data statistics
' Supported file types are Finnigan .Raw files (LCQ, LTQ, LTQ-FT), 
'   Agilent Ion Trap (.MGF and .CDF files), and mzXML files

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 11, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute, 
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
' Department of Energy (DOE).  All rights in the computer software are reserved 
' by DOE on behalf of the United States Government and the Contractor as 
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
' SOFTWARE.  This notice including this sentence must appear on any copies of 
' this computer software.

Imports ThermoRawFileReader
Imports PNNLOmics.Utilities
Imports System.Runtime.InteropServices

Public Class clsMASIC
    Inherits clsProcessFilesBaseClass

    Public Sub New()
        MyBase.mFileDate = "June 13, 2016"
        InitializeVariables()
    End Sub

#Region "Constants and Enums"
    Public Const XML_SECTION_DATABASE_SETTINGS As String = "MasicDatabaseSettings"
    Public Const XML_SECTION_IMPORT_OPTIONS As String = "MasicImportOptions"
    Public Const XML_SECTION_EXPORT_OPTIONS As String = "MasicExportOptions"
    Public Const XML_SECTION_SIC_OPTIONS As String = "SICOptions"
    Public Const XML_SECTION_BINNING_OPTIONS As String = "BinningOptions"
    Public Const XML_SECTION_MEMORY_OPTIONS As String = "MemoryOptions"
    Public Const XML_SECTION_CUSTOM_SIC_VALUES As String = "CustomSICValues"

    Public Const DATABASE_CONNECTION_STRING_DEFAULT As String = "Data Source=Pogo;Initial Catalog=Prism_IFC;User=mtuser;Password=mt4fun"
    Public Const DATABASE_DATASET_INFO_QUERY_DEFAULT As String = "Select Dataset, ID FROM Prism_IFC..V_DMS_Dataset_Summary"

    Private Const FINNIGAN_RAW_FILE_EXTENSION As String = ".RAW"
    Private Const MZ_XML_FILE_EXTENSION1 As String = ".MZXML"
    Private Const MZ_XML_FILE_EXTENSION2 As String = "MZXML.XML"
    Private Const MZ_DATA_FILE_EXTENSION1 As String = ".MZDATA"
    Private Const MZ_DATA_FILE_EXTENSION2 As String = "MZDATA.XML"
    Private Const AGILENT_MSMS_FILE_EXTENSION As String = ".MGF"    ' Agilent files must have been exported to a .MGF and .CDF file pair prior to using MASIC
    Private Const AGILENT_MS_FILE_EXTENSION As String = ".CDF"

    Private Const CUSTOM_SIC_TYPE_ABSOLUTE As String = "Absolute"
    Private Const CUSTOM_SIC_TYPE_RELATIVE As String = "Relative"
    Private Const CUSTOM_SIC_TYPE_ACQUISITION_TIME As String = "AcquisitionTime"

    Public Const CUSTOM_SIC_COLUMN_MZ As String = "MZ"
    Public Const CUSTOM_SIC_COLUMN_MZ_TOLERANCE As String = "MZToleranceDa"
    Public Const CUSTOM_SIC_COLUMN_SCAN_CENTER As String = "ScanCenter"
    Public Const CUSTOM_SIC_COLUMN_SCAN_TOLERNACE As String = "ScanTolerance"
    Public Const CUSTOM_SIC_COLUMN_SCAN_TIME As String = "ScanTime"
    Public Const CUSTOM_SIC_COLUMN_TIME_TOLERANCE As String = "TimeTolerance"
    Public Const CUSTOM_SIC_COLUMN_COMMENT As String = "Comment"

    Private Const EXTENDED_STATS_HEADER_COLLISION_MODE As String = "Collision Mode"
    Private Const EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT As String = "Scan Filter Text"

    Private Const DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD As Boolean = False          ' Enabling this will result in SICs with less noise, which will hurt noise determination after finding the SICs
    Private Const DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD As Boolean = True         ' Disabling this will slow down the correlation process (slightly)

    Public Const DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA As Double = 5
    Public Const DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM As Double = 3

    Private Const MAX_ALLOWABLE_ION_COUNT As Integer = 50000                        ' Absolute maximum number of ions that will be tracked for a mass spectrum

    Public Const DEFAULT_MASIC_STATUS_FILE_NAME As String = "MasicStatus.xml"
    Private Const MINIMUM_STATUS_FILE_UPDATE_INTERVAL_SECONDS As Integer = 3

    Public Const REPORTER_ION_TOLERANCE_DA_DEFAULT As Double = 0.5
    Public Const REPORTER_ION_TOLERANCE_DA_MINIMUM As Double = 0.001

    Public Const REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES As Double = 0.015


    Public Const CHARGE_CARRIER_MASS_MONOISO As Double = 1.00727649

    Public Enum eCustomSICScanTypeConstants
        Absolute = 0            ' Absolute scan number
        Relative = 1            ' Relative scan number (ranging from 0 to 1, where 0 is the first scan and 1 is the last scan)
        AcquisitionTime = 2     ' The scan's acquisition time (aka elution time if using liquid chromatography)
        Undefined = 3
    End Enum

    Protected Enum eCustomSICFileColumns
        MZ = 0
        MZToleranceDa = 1
        ScanCenter = 2              ' Absolute scan or Relative Scan, or Acquisition Time
        ScanTolerance = 3           ' Absolute scan or Relative Scan, or Acquisition Time
        ScanTime = 4                ' Only used for acquisition Time
        TimeTolerance = 5           ' Only used for acquisition Time
        Comment = 6
    End Enum

    Public Enum eProcessingStepConstants
        NewTask = 0
        ReadDataFile = 1
        SaveBPI = 2
        CreateSICsAndFindPeaks = 3
        FindSimilarParentIons = 4
        SaveExtendedScanStatsFiles = 5
        SaveSICStatsFlatFile = 6
        CloseOpenFileHandles = 7
        UpdateXMLFileWithNewOptimalPeakApexValues = 8
        Cancelled = 99
        Complete = 100
    End Enum

    Public Enum eMasicErrorCodes
        NoError = 0
        InvalidDatasetLookupFilePath = 1
        UnknownFileExtension = 2            ' This error code matches the identical code in clsFilterMsMsSpectra
        InputFileAccessError = 4            ' This error code matches the identical code in clsFilterMsMsSpectra
        InvalidDatasetNumber = 8
        CreateSICsError = 16
        FindSICPeaksError = 32
        InvalidCustomSICValues = 64
        NoParentIonsFoundInInputFile = 128
        NoSurveyScansFoundInInputFile = 256
        FindSimilarParentIonsError = 512
        InputFileDataReadError = 1024
        OutputFileWriteError = 2048
        FileIOPermissionsError = 4096
        ErrorCreatingSpectrumCacheFolder = 8192
        ErrorCachingSpectrum = 16384
        ErrorUncachingSpectrum = 32768
        ErrorDeletingCachedSpectrumFiles = 65536
        InvalidCustomSICHeaders = 131072
        UnspecifiedError = -1
    End Enum

    Protected Enum eScanTypeConstants
        SurveyScan = 0
        FragScan = 1
    End Enum

    Protected Enum eOutputFileTypeConstants
        XMLFile = 0
        ScanStatsFlatFile = 1
        ScanStatsExtendedFlatFile = 2
        ScanStatsExtendedConstantFlatFile = 3
        SICStatsFlatFile = 4
        BPIFile = 5
        FragBPIFile = 6
        TICFile = 7
        ICRToolsFragTICChromatogramByScan = 8
        ICRToolsBPIChromatogramByScan = 9
        ICRToolsBPIChromatogramByTime = 10
        ICRToolsTICChromatogramByScan = 11
        PEKFile = 12
        HeaderGlossary = 13
        DeconToolsScansFile = 14
        DeconToolsIsosFile = 15
        DeconToolsMSChromatogramFile = 16
        DeconToolsMSMSChromatogramFile = 17
        MSMethodFile = 18
        MSTuneFile = 19
        ReporterIonsFile = 20
        MRMSettingsFile = 21
        MRMDatafile = 22
        MRMCrosstabFile = 23
        DatasetInfoFile = 24
        SICDataFile = 25
    End Enum

    ' ToDo: Add XML
    Public Enum eExportRawDataFileFormatConstants
        PEKFile = 0
        CSVFile = 1
    End Enum

    Public Enum eReporterIonMassModeConstants
        CustomOrNone = 0
        ITraqFourMZ = 1
        ITraqETDThreeMZ = 2
        TMTTwoMZ = 3
        TMTSixMZ = 4
        ITraqEightMZHighRes = 5     ' This version of 8-plex iTraq should be used when the reporter ion search tolerance is +/-0.03 Da or smaller
        ITraqEightMZLowRes = 6      ' This version of 8-plex iTraq will account for immonium loss from phenylalanine
        PCGalnaz = 7
        HemeCFragment = 8
        LycAcetFragment = 9
        TMTTenMZ = 10               ' Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
        OGlcNAc = 11
        FrackingAmine20160217 = 12
        FSFACustomCarbonyl = 13
        FSFACustomCarboxylic = 14
        FSFACustomHydroxyl = 15
    End Enum

#End Region

#Region "Structures"

    Protected Structure udtSICOptionsType
        Public DatasetNumber As Integer                     ' Provided by the user at the command line or through the Property Function Interface; 0 if unknown
        Public SICTolerance As Double                       ' Defaults to 10 ppm
        Public SICToleranceIsPPM As Boolean                 ' When true, then SICToleranceDa is treated as a PPM value

        Public RefineReportedParentIonMZ As Boolean         ' If True, then will look through the m/z values in the parent ion spectrum data to find the closest match (within SICToleranceDa / udtSICOptions.CompressToleranceDivisorForDa); will update the reported m/z value to the one found
        Public ScanRangeStart As Integer                    ' If both ScanRangeStart >=0 and ScanRangeEnd > 0 then will only process data between those scan numbers
        Public ScanRangeEnd As Integer                      '

        Public RTRangeStart As Single                       ' If both RTRangeStart >=0 and RTRangeEnd > RTRangeStart then will only process data between those that scan range (in minutes)
        Public RTRangeEnd As Single

        Public CompressMSSpectraData As Boolean             ' If true, then combines data points that have similar m/z values (within tolerance) when loading; tolerance is udtSICOptions.SICToleranceDa / udtSICOptions.CompressToleranceDivisorForDa (or divided by udtSICOptions.CompressToleranceDivisorForPPM if udtSICOptions.SICToleranceIsPPM=True)
        Public CompressMSMSSpectraData As Boolean           ' If true, then combines data points that have similar m/z values (within tolerance) when loading; tolerance is udtBinningOptions.BinSize / udtSICOptions.CompressToleranceDivisorForDa

        Public CompressToleranceDivisorForDa As Double      ' When compressing spectra, udtSICOptions.SICTolerance and udtBinningOptions.BinSize will be divided by this value to determine the resolution to compress the data to
        Public CompressToleranceDivisorForPPM As Double     ' If udtSICOptions.SICToleranceIsPPM is True, then this divisor is used instead of CompressToleranceDivisorForDa

        ' The SIC is extended left and right until:
        '      1) the SIC intensity falls below IntensityThresholdAbsoluteMinimum, 
        '      2) the SIC intensity falls below the maximum value observed times IntensityThresholdFractionMax, 
        '   or 3) the distance exceeds MaxSICPeakWidthMinutesBackward or MaxSICPeakWidthMinutesForward


        Public MaxSICPeakWidthMinutesBackward As Single     ' 3
        Public MaxSICPeakWidthMinutesForward As Single      ' 3

        Public SICPeakFinderOptions As MASICPeakFinder.clsMASICPeakFinder.udtSICPeakFinderOptionsType
        Public ReplaceSICZeroesWithMinimumPositiveValueFromMSData As Boolean

        Public SaveSmoothedData As Boolean

        Public SimilarIonMZToleranceHalfWidth As Single         ' 0.1       m/z Tolerance for finding similar parent ions; full tolerance is +/- this value
        Public SimilarIonToleranceHalfWidthMinutes As Single    ' 5         Time Tolerance (in minutes) for finding similar parent ions; full tolerance is +/- this value
        Public SpectrumSimilarityMinimum As Single              ' 0.8
    End Structure

    Protected Structure udtSICStatsType
        Public Peak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

        Public ScanTypeForPeakIndices As eScanTypeConstants
        Public PeakScanIndexStart As Integer              ' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        Public PeakScanIndexEnd As Integer                ' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        Public PeakScanIndexMax As Integer                ' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum

        Public SICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
    End Structure

    Protected Structure udtSICStatsDetailsType
        Public SICDataCount As Integer

        Public SICScanType As eScanTypeConstants            ' Indicates the type of scans that the SICScanIndices() array points to. Will normally be "SurveyScan", but for MRM data will be "FragScan"
        Public SICScanIndices() As Integer                  ' This array is necessary since SIMScan data uses non-adjacent survey scans

        Public SICScanNumbers() As Integer                  ' Populated as a convenience since necessary to pass to various functions
        Public SICData() As Single                          ' SIC Abundances
        Public SICMasses() As Double                        ' SIC Masses

    End Structure

    Protected Structure udtRawDataExportOptionsType
        Public ExportEnabled As Boolean
        Public FileFormat As eExportRawDataFileFormatConstants

        Public IncludeMSMS As Boolean
        Public RenumberScans As Boolean

        Public MinimumSignalToNoiseRatio As Single
        Public MaxIonCountPerScan As Integer
        Public IntensityMinimum As Single

    End Structure

    Protected Structure udtParentIonInfoType
        Public MZ As Double                                 ' m/z value
        Public SurveyScanIndex As Integer                   ' Survey scan that this parent ion was observed in; Pointer to entry in .SurveyScans(); For custom SIC values, this is the closest survey scan to .ScanCenter
        Public OptimalPeakApexScanNumber As Integer         ' Scan number of the peak apex for this parent ion; originally the scan number of the first fragmentation spectrum; later updated to the scan number of the SIC data Peak apex; possibly updated later in FindSimilarParentIons()
        Public PeakApexOverrideParentIonIndex As Integer    ' If OptimalPeakApexScanNumber is inherited from another parent ion, then this is set to that parent ion's index; otherwise, this is -1
        Public FragScanIndexCount As Integer                ' Number of fragmentation scans attributable to this parent ion; normally just 1; for custom SIC values, there are no associated fragmentation scans, but we still set this value to 1
        Public FragScanIndices() As Integer                 ' Pointers to entries in .FragScans(); for custom SIC values, points to the next MS2 scan that occurs after the ScanCenter search value
        Public SICStats As udtSICStatsType
        Public CustomSICPeak As Boolean                     ' True if this is a custom SIC-based parent ion
        Public CustomSICPeakComment As String               ' Only applies to custom SIC-based parent ions
        Public CustomSICPeakMZToleranceDa As Double         ' Only applies to custom SIC-based parent ions
        Public CustomSICPeakScanOrAcqTimeTolerance As Single         ' Only applies to custom SIC-based parent ions
        Public MRMDaughterMZ As Double                      ' Only applicable to MRM scans
        Public MRMToleranceHalfWidth As Double              ' Only applicable to MRM scans
    End Structure

    Protected Structure udtMRMScanInfoType
        Public ParentIonMZ As Double
        Public MRMMassCount As Integer                      ' List of mass ranges monitored by the first quadrupole
        Public MRMMassList As List(Of udtMRMMassRangeType)  ' Daughter m/z values monitored for this parent m/z
        Public ScanCount As Integer                         ' Number of spectra that used these MRM search values
        Public ParentIonInfoIndex As Integer
    End Structure

    Protected Structure FragScanInfoType
        Public ParentIonInfoIndex As Integer                ' Pointer to an entry in the ParentIons() array; -1 if undefined
        Public FragScanNumber As Integer                    ' Usually 1, 2, or 3
        Public MSLevel As Integer                           ' 2 for MS/MS, 3 for MS/MS/MS
        Public CollisionMode As String
    End Structure

    Protected Structure udtScanInfoType
        Public ScanNumber As Integer                        ' Ranges from 1 to the number of scans in the datafile
        Public ScanTime As Single                           ' Retention (elution) Time (in minutes)
        Public ScanHeaderText As String                     ' String description of the scan mode for the given scan; only used for Finnigan .Raw files; typical values are: FTMS + p NSI Full ms, ITMS + c ESI Full ms, ITMS + p ESI d Z ms, ITMS + c NSI d Full ms2, ITMS + c NSI d Full ms2, ITMS + c NSI d Full ms2, FTMS + c NSI d Full ms2, ITMS + c NSI d Full ms3
        Public ScanTypeName As String                       ' Typical values: MS, HMS, Zoom, CID-MSn, or PQD-MSn
        Public BasePeakIonMZ As Double                      ' mz of the most intense ion in this scan
        Public BasePeakIonIntensity As Single               ' intensity of the most intense ion in this scan
        Public TotalIonIntensity As Single                  ' intensity of all of the ions for this scan
        Public MinimumPositiveIntensity As Single           ' minimum intensity > 0 in this scan

        Public ZoomScan As Boolean                          ' True if the scan is a Zoom scan
        Public SIMScan As Boolean                           ' True if the scan was a SIM scan
        Public MRMScanType As MRMScanTypeConstants
        Public SIMIndex As Integer                          ' For SIM scans, allows one to quickly find all of the SIM scans with the same mass range, since they'll all have the same SIMIndex
        Public LowMass As Double                            ' Useful for SIMScans to find similar SIM scans
        Public HighMass As Double                           ' Useful for SIMScans to find similar SIM scans

        Public FragScanInfo As FragScanInfoType             ' Information specific to fragmentation scans
        Public MRMScanInfo As udtMRMScanInfoType            ' Information specific to MRM/SRM scans

        ' Keys are ID values pointing to mExtendedHeaderInfo (where the name is defined); values are the string or numeric values for the settings
        Public ExtendedHeaderInfo As Dictionary(Of Integer, String)

        ' Note: the mass spectral data for this scan is tracked by a clsSpectraCache object
        Public IonCount As Integer
        Public IonCountRaw As Integer

        Public BaselineNoiseStats As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseStatsType            ' Intensity level of the noise in the MS data for this scan

    End Structure

    Protected Structure udtScanOrderPointerType
        Public ScanType As eScanTypeConstants
        Public ScanIndexPointer As Integer                  ' Pointer to entry into udtScanList.SurveyScans() or udtScanList.FragScans()
    End Structure

    ' Note: We're keeping the Survey Scans separate from the Fragmentation Scans to make the creation of the
    '         survey scan based SIC's easier (and faster)
    '       The MasterScanOrder array allows us to step through the data scan-by-scan, using both SurveyScans and FragScans
    Protected Structure udtScanListType
        Public SurveyScanCount As Integer
        Public SurveyScans() As udtScanInfoType                 ' 0-based array, holding survey scans, the order is the same as in the original data file, and thus is by increasing scan number
        Public FragScanCount As Integer
        Public FragScans() As udtScanInfoType                   ' 0-based array, holding fragmentation scans, the order is the same as in the original data file, and thus is by increasing scan number

        Public MasterScanOrderCount As Integer
        Public MasterScanOrder() As udtScanOrderPointerType     ' 0-based array, holding pointers to either the SurveyScans() or FragScans() arrays, in order of scan number
        Public MasterScanNumList() As Integer                   ' 0-based array; parallel to MasterScanOrder
        Public MasterScanTimeList() As Single                   ' 0-based array; parallel to MasterScanOrder

        Public ParentIonInfoCount As Integer
        Public ParentIons() As udtParentIonInfoType             ' 0-based array, ranging from 0 to ParentIonInfoCount-1
        Public ProcessingIncomplete As Boolean                  ' Set to true if the user cancels any of the processing steps
        Public SIMDataPresent As Boolean
        Public MRMDataPresent As Boolean
    End Structure

    Protected Structure udtBinnedDataType
        Public BinnedDataStartX As Single
        Public BinSize As Single
        Public BinCount As Integer
        Public BinnedIntensities() As Single                ' 0-based array, ranging from 0 to BinCount-1; First bin starts at BinnedDataStartX
        Public BinnedIntensitiesOffset() As Single          ' 0-based array, ranging from 0 to BinCount-1; First bin starts at BinnedDataStartX + BinSize/2
    End Structure

    Protected Structure udtUniqueMZListType
        Public MZAvg As Double
        Public MaxIntensity As Single                   ' Highest intensity value of the similar parent ions
        Public MaxPeakArea As Single                    ' Largest peak intensity value of the similar parent ions
        Public ScanNumberMaxIntensity As Integer        ' Scan number of the parent ion with the highest intensity
        Public ScanTimeMaxIntensity As Single           ' Elution time of the parent ion with the highest intensity
        Public ParentIonIndexMaxIntensity As Integer    ' Pointer to an entry in .ParentIons()
        Public ParentIonIndexMaxPeakArea As Integer     ' Pointer to an entry in .ParentIons()
        Public MatchCount As Integer
        Public MatchIndices() As Integer            ' Pointer to an entry in .ParentIons()
    End Structure

    Protected Structure udtFindPeaksDataType

        Public OriginalPeakLocationIndex As Integer

        Public SourceDataCount As Integer
        Public XData() As Double
        Public YData() As Double
        Public SmoothedYData() As Double

        Public PeakCount As Integer
        Public PeakLocs() As Integer
        Public PeakEdgesLeft() As Integer
        Public PeakEdgesRight() As Integer
        Public PeakAreas() As Double
        Public PeakIsValid() As Boolean

        Public PeakWidthPointsMinimum As Integer
        Public MaxAllowedUpwardSpikeFractionMax As Single
        Public BestPeakIndex As Integer
        Public BestPeakArea As Single

    End Structure

    Protected Structure udtFindSimilarIonsDataType
        Public MZPointerArray() As Integer
        Public IonInUseCount As Integer
        Public IonUsed() As Boolean
        Public UniqueMZListCount As Integer
        Public UniqueMZList() As udtUniqueMZListType
    End Structure

    Public Structure udtCustomMZSearchSpecType
        Public MZ As Double
        Public MZToleranceDa As Double               ' If 0, then uses the global search tolerance defined
        Public ScanOrAcqTimeCenter As Single         ' This is an Integer if ScanType = eCustomSICScanTypeConstants.Absolute; it is a Single if ScanType = .Relative or ScanType = .AcquisitionTime
        Public ScanOrAcqTimeTolerance As Single      ' This is an Integer if ScanType = eCustomSICScanTypeConstants.Absolute; it is a Single if ScanType = .Relative or ScanType = .AcquisitionTime; set to 0 to search the entire file for the given mass
        Public Comment As String
    End Structure

    Protected Structure udtCustomMZSearchListType
        Public ScanToleranceType As eCustomSICScanTypeConstants
        Public ScanOrAcqTimeTolerance As Single                         ' This is an Integer if ScanToleranceType = eCustomSICScanTypeConstants.Absolute; it is a Single if ScanToleranceType = .Relative or ScanToleranceType = .AcquisitionTime; set to 0 to search the entire file for the given mass
        Public CustomMZSearchValues() As udtCustomMZSearchSpecType
        Public LimitSearchToCustomMZList As Boolean                     ' When True, then will only search for the m/z values listed in the custom m/z list
        Public RawTextMZList As String
        Public RawTextMZToleranceDaList As String
        Public RawTextScanOrAcqTimeCenterList As String
        Public RawTextScanOrAcqTimeToleranceList As String
    End Structure

    Protected Structure udtProcessingStatsType
        Public PeakMemoryUsageMB As Single
        Public TotalProcessingTimeAtStart As Single
        Public CacheEventCount As Integer
        Public UnCacheEventCount As Integer

        Public FileLoadStartTime As DateTime
        Public FileLoadEndTime As DateTime

        Public ProcessingStartTime As DateTime
        Public ProcessingEndTime As DateTime

        Public MemoryUsageMBAtStart As Single
        Public MemoryUsageMBDuringLoad As Single
        Public MemoryUsageMBAtEnd As Single

    End Structure

    Protected Structure udtOutputFileHandlesType
        Public ScanStats As StreamWriter
        Public SICDataFile As StreamWriter
        Public XMLFileForSICs As Xml.XmlTextWriter
        Public MSMethodFilePathBase As String
        Public MSTuneFilePathBase As String
    End Structure

    Protected Structure udtMZSearchInfoType
        Public SearchMZ As Double

        Public MZIndexStart As Integer
        Public MZIndexEnd As Integer
        Public MZIndexMidpoint As Integer

        Public MZTolerance As Double
        Public MZToleranceIsPPM As Boolean

        Public MaximumIntensity As Single
        Public ScanIndexMax As Integer

        Public BaselineNoiseStatSegments() As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseStatSegmentsType
    End Structure

    Protected Structure udtMZBinListType
        Public MZ As Double
        Public MZTolerance As Double
        Public MZToleranceIsPPM As Boolean
    End Structure

    Public Structure udtReporterIonInfoType
        Public MZ As Double
        Public MZToleranceDa As Double
        Public ContaminantIon As Boolean        ' Should be False for Reporter Ions and True for other ions, e.g. immonium loss from phenylalanine
    End Structure

    Private Structure udtSRMListType
        Public ParentIonMZ As Double
        Public CentralMass As Double
    End Structure

#End Region

#Region "Classwide Variables"

    Private mSICOptions As udtSICOptionsType                            ' Set options through the Property Functions or by passing strParameterFilePath to ProcessFile()
    Private mBinningOptions As clsCorrelation.udtBinningOptionsType     ' Binning options for MS/MS spectra; only applies to spectrum similarity testing
    Private mMASICPeakFinder As MASICPeakFinder.clsMASICPeakFinder

    Private mCustomSICListFileName As String
    Private mCustomSICList As udtCustomMZSearchListType

    ' Keys are strings of extended info names
    ' values are the assigned ID value for the extended info name; the order of the values defines the appropriate output order for the names
    Private mExtendedHeaderInfo As List(Of KeyValuePair(Of String, Integer))

    Private mDatabaseConnectionString As String
    Private mDatasetInfoQuerySql As String
    Private mDatasetLookupFilePath As String = String.Empty

    Private mIncludeHeadersInExportFile As Boolean
    Private mIncludeScanTimesInSICStatsFile As Boolean
    Private mFastExistingXMLFileTest As Boolean

    Private mSkipMSMSProcessing As Boolean                      ' Using this will reduce memory usage, but not as much as when mSkipSICAndRawDataProcessing = True
    Private mSkipSICAndRawDataProcessing As Boolean             ' Using this will drastically reduce memory usage since raw mass spec data is not retained
    Private mExportRawDataOnly As Boolean                       ' When True, then will not create any SICs; automatically set to false if mSkipSICAndRawDataProcessing = True

    Private mWriteDetailedSICDataFile As Boolean
    Private mWriteMSMethodFile As Boolean
    Private mWriteMSTuneFile As Boolean

    Private mWriteExtendedStats As Boolean
    Private mWriteExtendedStatsIncludeScanFilterText As Boolean     ' When enabled, the the scan filter text will also be included in the extended stats file (e.g. ITMS + c NSI Full ms [300.00-2000.00] or ITMS + c NSI d Full ms2 756.98@35.00 [195.00-2000.00])
    Private mWriteExtendedStatsStatusLog As Boolean                 ' Adds a large number of additional columns with information like voltage, current, temperature, pressure, and gas flow rate; if mStatusLogKeyNameFilterList contains any entries, then only the entries matching the specs in mStatusLogKeyNameFilterList will be saved
    Private mConsolidateConstantExtendedHeaderValues As Boolean

    ' Since there are so many values listed in the Status Log, use mStatusLogKeyNameFilterList to limit the items saved 
    '  to only those matching the specs in mStatusLogKeyNameFilterList
    ' When parsing the entries in mStatusLogKeyNameFilterList, if any part of the text in mStatusLogKeyNameFilterList() matches the status log key name, that key name is saved (key names are not case sensitive)
    Private mStatusLogKeyNameFilterList() As String

    Private mWriteMRMDataList As Boolean
    Private mWriteMRMIntensityCrosstab As Boolean

    Private mSuppressNoParentIonsError As Boolean                   ' If this is true, then an error will not be raised if the input file contains no parent ions or no survey scans

    Private mRawDataExportOptions As udtRawDataExportOptionsType

    Private mReporterIonStatsEnabled As Boolean
    Private mReporterIonMassMode As eReporterIonMassModeConstants

    ' When mReporterIonStatsEnabled = True, these variables will be populated with the m/z range of the reporter ions being processed
    Private mMZIntensityFilterIgnoreRangeStart As Double
    Private mMZIntensityFilterIgnoreRangeEnd As Double

    Private mReporterIonToleranceDaDefault As Double
    Private mReporterIonApplyAbundanceCorrection As Boolean
    Private mReporterIonITraq4PlexCorrectionFactorType As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex

    Private mReporterIonSaveObservedMasses As Boolean
    Private mReporterIonSaveUncorrectedIntensities As Boolean       ' This is ignored if mReporterIonApplyAbundanceCorrection is False

    Private mReporterIonCount As Integer
    Private mReporterIonInfo() As udtReporterIonInfoType

    Private mCDFTimeInSeconds As Boolean
    Private mParentIonDecoyMassDa As Double

    Private mUseBase64DataEncoding As Boolean

    Private mProcessingStats As udtProcessingStatsType
    Private mCacheOptions As clsSpectraCache.udtSpectrumCacheOptionsType
    Private mMASICStatusFilename As String = DEFAULT_MASIC_STATUS_FILE_NAME

    Private mProcessingStep As eProcessingStepConstants         ' Current processing step
    Private mSubtaskProcessingStepPct As Short                         ' Percent completion for the current sub task
    Private mSubtaskDescription As String = String.Empty

    Private mLocalErrorCode As eMasicErrorCodes
    Private mStatusMessage As String

    Private mFreeMemoryPerformanceCounter As PerformanceCounter
    Private mLastParentIonProcessingLogTime As DateTime

    Private mScanStats As List(Of DSSummarizer.clsScanStatsEntry)
    Private mDatasetFileInfo As DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType

    Protected WithEvents mXcaliburAccessor As XRawFileIO

    ' Note: Use RaiseEvent MyBase.ProgressChanged when updating the overall progress
    ' Use ProgressSubtaskChanged when updating the sub task progress
    Public Event ProgressSubtaskChanged()
    Public Event ProgressResetKeypressAbort()

#End Region

#Region "Processing Options and File Path Interface Functions"
    Public Property DatabaseConnectionString() As String
        Get
            Return mDatabaseConnectionString
        End Get
        Set(Value As String)
            mDatabaseConnectionString = Value
        End Set
    End Property

    Public Property DatasetInfoQuerySql() As String
        Get
            Return mDatasetInfoQuerySql
        End Get
        Set(Value As String)
            mDatasetInfoQuerySql = Value
        End Set
    End Property

    Public Property DatasetLookupFilePath() As String
        Get
            Return mDatasetLookupFilePath
        End Get
        Set(Value As String)
            mDatasetLookupFilePath = Value
        End Set
    End Property

    Public Property DatasetNumber() As Integer
        Get
            Return mSICOptions.DatasetNumber
        End Get
        Set(Value As Integer)
            mSICOptions.DatasetNumber = Value
        End Set
    End Property

    Public ReadOnly Property LocalErrorCode() As eMasicErrorCodes
        Get
            Return mLocalErrorCode
        End Get
    End Property

    Public ReadOnly Property MASICPeakFinderDllVersion() As String
        Get
            If Not mMASICPeakFinder Is Nothing Then
                Return mMASICPeakFinder.ProgramVersion
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public Property MASICStatusFilename() As String
        Get
            Return mMASICStatusFilename
        End Get
        Set(value As String)
            If value Is Nothing OrElse value.Trim.Length = 0 Then
                mMASICStatusFilename = DEFAULT_MASIC_STATUS_FILE_NAME
            Else
                mMASICStatusFilename = value
            End If
        End Set
    End Property
    Public ReadOnly Property ProcessStep() As eProcessingStepConstants
        Get
            Return mProcessingStep
        End Get
    End Property

    ' Subtask progress percent complete
    Public ReadOnly Property SubtaskProgressPercentComplete() As Single
        Get
            Return mSubtaskProcessingStepPct
        End Get
    End Property

    Public ReadOnly Property SubtaskDescription() As String
        Get
            Return mSubtaskDescription
        End Get
    End Property

    Public ReadOnly Property StatusMessage() As String
        Get
            Return mStatusMessage
        End Get
    End Property
#End Region

#Region "SIC Options Interface Functions"
    Public Property CDFTimeInSeconds() As Boolean
        Get
            Return mCDFTimeInSeconds
        End Get
        Set(Value As Boolean)
            mCDFTimeInSeconds = Value
        End Set
    End Property

    Public Property CompressMSSpectraData() As Boolean
        Get
            Return mSICOptions.CompressMSSpectraData
        End Get
        Set(Value As Boolean)
            mSICOptions.CompressMSSpectraData = Value
        End Set
    End Property

    Public Property CompressMSMSSpectraData() As Boolean
        Get
            Return mSICOptions.CompressMSMSSpectraData
        End Get
        Set(Value As Boolean)
            mSICOptions.CompressMSMSSpectraData = Value
        End Set
    End Property

    Public Property CompressToleranceDivisorForDa() As Double
        Get
            Return mSICOptions.CompressToleranceDivisorForDa
        End Get
        Set(value As Double)
            mSICOptions.CompressToleranceDivisorForDa = value
        End Set
    End Property

    Public Property CompressToleranceDivisorForPPM() As Double
        Get
            Return mSICOptions.CompressToleranceDivisorForPPM
        End Get
        Set(value As Double)
            mSICOptions.CompressToleranceDivisorForPPM = value
        End Set
    End Property

    Public Property ConsolidateConstantExtendedHeaderValues() As Boolean
        Get
            Return mConsolidateConstantExtendedHeaderValues
        End Get
        Set(value As Boolean)
            mConsolidateConstantExtendedHeaderValues = value
        End Set
    End Property

    Public ReadOnly Property CustomSICListScanType() As eCustomSICScanTypeConstants
        Get
            Return mCustomSICList.ScanToleranceType
        End Get
    End Property

    Public ReadOnly Property CustomSICListScanTolerance() As Single
        Get
            Return mCustomSICList.ScanOrAcqTimeTolerance
        End Get
    End Property

    Public ReadOnly Property CustomSICListSearchValues() As udtCustomMZSearchSpecType()
        Get
            Return mCustomSICList.CustomMZSearchValues
        End Get
    End Property

    Public Property CustomSICListFileName() As String
        Get
            If mCustomSICListFileName Is Nothing Then
                Return String.Empty
            Else
                Return mCustomSICListFileName
            End If
        End Get
        Set(Value As String)
            If Value Is Nothing OrElse Value.Trim.Length = 0 Then
                mCustomSICListFileName = String.Empty
            Else
                mCustomSICListFileName = Value
            End If

        End Set
    End Property

    Public Property ExportRawDataOnly() As Boolean
        Get
            Return mExportRawDataOnly
        End Get
        Set(Value As Boolean)
            mExportRawDataOnly = Value
        End Set
    End Property

    Public Property FastExistingXMLFileTest() As Boolean
        Get
            Return mFastExistingXMLFileTest
        End Get
        Set(Value As Boolean)
            mFastExistingXMLFileTest = Value
        End Set
    End Property

    Public Property IncludeHeadersInExportFile() As Boolean
        Get
            Return mIncludeHeadersInExportFile
        End Get
        Set(Value As Boolean)
            mIncludeHeadersInExportFile = Value
        End Set
    End Property

    Public Property IncludeScanTimesInSICStatsFile() As Boolean
        Get
            Return mIncludeScanTimesInSICStatsFile
        End Get
        Set(Value As Boolean)
            mIncludeScanTimesInSICStatsFile = Value
        End Set
    End Property

    Public Property LimitSearchToCustomMZList() As Boolean
        Get
            Return mCustomSICList.LimitSearchToCustomMZList
        End Get
        Set(Value As Boolean)
            mCustomSICList.LimitSearchToCustomMZList = Value
        End Set
    End Property

    Public Property ParentIonDecoyMassDa() As Double
        Get
            Return mParentIonDecoyMassDa
        End Get
        Set(value As Double)
            mParentIonDecoyMassDa = value
        End Set
    End Property

    Public Property SkipMSMSProcessing() As Boolean
        Get
            Return mSkipMSMSProcessing
        End Get
        Set(Value As Boolean)
            mSkipMSMSProcessing = Value
        End Set
    End Property

    Public Property SkipSICAndRawDataProcessing() As Boolean
        Get
            Return mSkipSICAndRawDataProcessing
        End Get
        Set(Value As Boolean)
            mSkipSICAndRawDataProcessing = Value
        End Set
    End Property

    Public Property SuppressNoParentIonsError() As Boolean
        Get
            Return mSuppressNoParentIonsError
        End Get
        Set(Value As Boolean)
            mSuppressNoParentIonsError = Value
        End Set
    End Property

    <Obsolete("No longer supported")>
    Public Property UseFinniganXRawAccessorFunctions() As Boolean
        Get
            Return True
        End Get
        Set(Value As Boolean)

        End Set
    End Property

    Public Property WriteDetailedSICDataFile() As Boolean
        Get
            Return mWriteDetailedSICDataFile
        End Get
        Set(value As Boolean)
            mWriteDetailedSICDataFile = value
        End Set
    End Property

    Public Property WriteExtendedStats() As Boolean
        Get
            Return mWriteExtendedStats
        End Get
        Set(Value As Boolean)
            mWriteExtendedStats = Value
        End Set
    End Property

    Public Property WriteExtendedStatsIncludeScanFilterText() As Boolean
        Get
            Return mWriteExtendedStatsIncludeScanFilterText
        End Get
        Set(Value As Boolean)
            mWriteExtendedStatsIncludeScanFilterText = Value
        End Set
    End Property

    Public Property WriteExtendedStatsStatusLog() As Boolean
        Get
            Return mWriteExtendedStatsStatusLog
        End Get
        Set(Value As Boolean)
            mWriteExtendedStatsStatusLog = Value
        End Set
    End Property

    Public Property WriteMRMDataList() As Boolean
        Get
            Return mWriteMRMDataList
        End Get
        Set(value As Boolean)
            mWriteMRMDataList = value
        End Set
    End Property

    Public Property WriteMRMIntensityCrosstab() As Boolean
        Get
            Return mWriteMRMIntensityCrosstab
        End Get
        Set(value As Boolean)
            mWriteMRMIntensityCrosstab = value
        End Set
    End Property

    Public Property WriteMSMethodFile() As Boolean
        Get
            Return mWriteMSMethodFile
        End Get
        Set(Value As Boolean)
            mWriteMSMethodFile = Value
        End Set
    End Property

    Public Property WriteMSTuneFile() As Boolean
        Get
            Return mWriteMSTuneFile
        End Get
        Set(Value As Boolean)
            mWriteMSTuneFile = Value
        End Set
    End Property

    ' This property is included for historical reasons since SIC tolerance can now be Da or PPM
    Public Property SICToleranceDa() As Double
        Get
            If mSICOptions.SICToleranceIsPPM Then
                ' Return the Da tolerance value that will result for the given ppm tolerance at 1000 m/z
                Return GetParentIonToleranceDa(mSICOptions, 1000)
            Else
                Return mSICOptions.SICTolerance
            End If
        End Get
        Set(value As Double)
            SetSICTolerance(value, False)
        End Set
    End Property

    Public Function GetSICTolerance() As Double
        Dim blnToleranceIsPPM As Boolean
        Return GetSICTolerance(blnToleranceIsPPM)
    End Function

    Public Function GetSICTolerance(<Out()> ByRef blnSICToleranceIsPPM As Boolean) As Double
        blnSICToleranceIsPPM = mSICOptions.SICToleranceIsPPM
        Return mSICOptions.SICTolerance
    End Function

    Public Sub SetSICTolerance(dblSICTolerance As Double, blnSICToleranceIsPPM As Boolean)
        mSICOptions.SICToleranceIsPPM = blnSICToleranceIsPPM

        If mSICOptions.SICToleranceIsPPM Then
            If dblSICTolerance < 0 Or dblSICTolerance > 1000000 Then dblSICTolerance = 100
        Else
            If dblSICTolerance < 0 Or dblSICTolerance > 10000 Then dblSICTolerance = 0.6
        End If
        mSICOptions.SICTolerance = dblSICTolerance
    End Sub

    Public Property SICToleranceIsPPM() As Boolean
        Get
            Return mSICOptions.SICToleranceIsPPM
        End Get
        Set(value As Boolean)
            mSICOptions.SICToleranceIsPPM = value
        End Set
    End Property

    Public Property RefineReportedParentIonMZ() As Boolean
        Get
            Return mSICOptions.RefineReportedParentIonMZ
        End Get
        Set(Value As Boolean)
            mSICOptions.RefineReportedParentIonMZ = Value
        End Set
    End Property

    Public Property RTRangeEnd() As Single
        Get
            Return mSICOptions.RTRangeEnd
        End Get
        Set(Value As Single)
            mSICOptions.RTRangeEnd = Value
        End Set
    End Property

    Public Property RTRangeStart() As Single
        Get
            Return mSICOptions.RTRangeStart
        End Get
        Set(Value As Single)
            mSICOptions.RTRangeStart = Value
        End Set
    End Property

    Public Property ScanRangeEnd() As Integer
        Get
            Return mSICOptions.ScanRangeEnd
        End Get
        Set(Value As Integer)
            mSICOptions.ScanRangeEnd = Value
        End Set
    End Property

    Public Property ScanRangeStart() As Integer
        Get
            Return mSICOptions.ScanRangeStart
        End Get
        Set(Value As Integer)
            mSICOptions.ScanRangeStart = Value
        End Set
    End Property

    Public Property MaxSICPeakWidthMinutesBackward() As Single
        Get
            Return mSICOptions.MaxSICPeakWidthMinutesBackward
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 10000 Then Value = 3
            mSICOptions.MaxSICPeakWidthMinutesBackward = Value
        End Set
    End Property

    Public Property MaxSICPeakWidthMinutesForward() As Single
        Get
            Return mSICOptions.MaxSICPeakWidthMinutesForward
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 10000 Then Value = 3
            mSICOptions.MaxSICPeakWidthMinutesForward = Value
        End Set
    End Property

    Public Property SICNoiseFractionLowIntensityDataToAverage() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
        End Get
        Set(Value As Single)
            mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = Value
        End Set
    End Property

    Public Property SICNoiseMinimumSignalToNoiseRatio() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio
        End Get
        Set(Value As Single)
            ' This value isn't utilized by MASIC for SICs so we'll force it to always be zero
            mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0
            ''mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = Value
        End Set
    End Property

    Public Property SICNoiseThresholdIntensity() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > Single.MaxValue Then Value = 50000
            mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = Value
        End Set
    End Property

    Public Property SICNoiseThresholdMode() As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes
        Get
            Return mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode
        End Get
        Set(Value As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
            mSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = Value
        End Set
    End Property

    Public Property MassSpectraNoiseFractionLowIntensityDataToAverage() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage
        End Get
        Set(Value As Single)
            mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = Value
        End Set
    End Property

    Public Property MassSpectraNoiseMinimumSignalToNoiseRatio() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio
        End Get
        Set(Value As Single)
            mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = Value
        End Set
    End Property

    Public Property MassSpectraNoiseThresholdIntensity() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > Single.MaxValue Then Value = 0
            mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = Value
        End Set
    End Property

    Public Property MassSpectraNoiseThresholdMode() As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes
        Get
            Return mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode
        End Get
        Set(Value As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
            mSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = Value
        End Set
    End Property

    Public Property ReplaceSICZeroesWithMinimumPositiveValueFromMSData() As Boolean
        Get
            Return mSICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData
        End Get
        Set(Value As Boolean)
            mSICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = Value
        End Set
    End Property
#End Region

#Region "Raw Data Export Options"

    Public Property ExportRawDataIncludeMSMS() As Boolean
        Get
            Return mRawDataExportOptions.IncludeMSMS
        End Get
        Set(Value As Boolean)
            mRawDataExportOptions.IncludeMSMS = Value
        End Set
    End Property

    Public Property ExportRawDataRenumberScans() As Boolean
        Get
            Return mRawDataExportOptions.RenumberScans
        End Get
        Set(Value As Boolean)
            mRawDataExportOptions.RenumberScans = Value
        End Set
    End Property

    Public Property ExportRawDataIntensityMinimum() As Single
        Get
            Return mRawDataExportOptions.IntensityMinimum
        End Get
        Set(Value As Single)
            mRawDataExportOptions.IntensityMinimum = Value
        End Set
    End Property

    Public Property ExportRawDataMaxIonCountPerScan() As Integer
        Get
            Return mRawDataExportOptions.MaxIonCountPerScan
        End Get
        Set(Value As Integer)
            mRawDataExportOptions.MaxIonCountPerScan = Value
        End Set
    End Property

    Public Property ExportRawDataFileFormat() As eExportRawDataFileFormatConstants
        Get
            Return mRawDataExportOptions.FileFormat
        End Get
        Set(Value As eExportRawDataFileFormatConstants)
            mRawDataExportOptions.FileFormat = Value
        End Set
    End Property

    Public Property ExportRawDataMinimumSignalToNoiseRatio() As Single
        Get
            Return mRawDataExportOptions.MinimumSignalToNoiseRatio
        End Get
        Set(Value As Single)
            mRawDataExportOptions.MinimumSignalToNoiseRatio = Value
        End Set
    End Property

    Public Property ExportRawSpectraData() As Boolean
        Get
            Return mRawDataExportOptions.ExportEnabled
        End Get
        Set(Value As Boolean)
            mRawDataExportOptions.ExportEnabled = Value
        End Set
    End Property

    Public Property ReporterIonStatsEnabled() As Boolean
        Get
            Return mReporterIonStatsEnabled
        End Get
        Set(Value As Boolean)
            mReporterIonStatsEnabled = Value
        End Set
    End Property

    Public Property ReporterIonApplyAbundanceCorrection() As Boolean
        Get
            Return mReporterIonApplyAbundanceCorrection
        End Get
        Set(value As Boolean)
            mReporterIonApplyAbundanceCorrection = value
        End Set
    End Property

    Public Property ReporterIonITraq4PlexCorrectionFactorType() As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex
        Get
            Return mReporterIonITraq4PlexCorrectionFactorType
        End Get
        Set(value As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex)
            mReporterIonITraq4PlexCorrectionFactorType = value
        End Set
    End Property

    Public Property ReporterIonSaveUncorrectedIntensities() As Boolean
        Get
            Return mReporterIonSaveUncorrectedIntensities
        End Get
        Set(value As Boolean)
            mReporterIonSaveUncorrectedIntensities = value
        End Set
    End Property

    Public Property ReporterIonMassMode() As eReporterIonMassModeConstants
        Get
            Return mReporterIonMassMode
        End Get
        Set(Value As eReporterIonMassModeConstants)
            SetReporterIonMassMode(Value)
        End Set
    End Property

    Public Property ReporterIonSaveObservedMasses() As Boolean
        Get
            Return mReporterIonSaveObservedMasses
        End Get
        Set(Value As Boolean)
            mReporterIonSaveObservedMasses = Value
        End Set
    End Property

    Public Property ReporterIonToleranceDaDefault() As Double
        Get
            If mReporterIonToleranceDaDefault < Double.Epsilon Then mReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT
            Return mReporterIonToleranceDaDefault
        End Get
        Set(Value As Double)
            If Value < Double.Epsilon Then Value = REPORTER_ION_TOLERANCE_DA_DEFAULT
            mReporterIonToleranceDaDefault = Value
        End Set
    End Property

#End Region

#Region "Peak Finding Options"
    Public Property IntensityThresholdAbsoluteMinimum() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > Single.MaxValue Then Value = 0
            mSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum = Value
        End Set
    End Property

    Public Property IntensityThresholdFractionMax() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 1 Then Value = 0.01
            mSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax = Value
        End Set
    End Property

    Public Property MaxDistanceScansNoOverlap() As Integer
        Get
            Return mSICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap
        End Get
        Set(Value As Integer)
            If Value < 0 Or Value > 10000 Then Value = 0
            mSICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap = Value
        End Set
    End Property

    Public Property FindPeaksOnSmoothedData() As Boolean
        Get
            Return mSICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData
        End Get
        Set(Value As Boolean)
            mSICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData = Value
        End Set
    End Property

    Public Property SmoothDataRegardlessOfMinimumPeakWidth() As Boolean
        Get
            Return mSICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth
        End Get
        Set(Value As Boolean)
            mSICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = Value
        End Set
    End Property

    Public Property UseButterworthSmooth() As Boolean
        Get
            Return mSICOptions.SICPeakFinderOptions.UseButterworthSmooth
        End Get
        Set(Value As Boolean)
            mSICOptions.SICPeakFinderOptions.UseButterworthSmooth = Value
        End Set
    End Property

    Public Property ButterworthSamplingFrequency() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency
        End Get
        Set(Value As Single)
            ' Value should be between 0.01 and 0.99; this is checked for in the filter, so we don't need to check here
            mSICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency = Value
        End Set
    End Property

    Public Property ButterworthSamplingFrequencyDoubledForSIMData() As Boolean
        Get
            Return mSICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData
        End Get
        Set(Value As Boolean)
            mSICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = Value
        End Set
    End Property

    Public Property UseSavitzkyGolaySmooth() As Boolean
        Get
            Return mSICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth
        End Get
        Set(Value As Boolean)
            mSICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth = Value
        End Set
    End Property

    Public Property SavitzkyGolayFilterOrder() As Short
        Get
            Return mSICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder
        End Get
        Set(Value As Short)

            ' Polynomial order should be between 0 and 6
            If Value < 0 Or Value > 6 Then Value = 0

            ' Polynomial order should be even
            If Value Mod 2 <> 0 Then Value -= CShort(1)
            If Value < 0 Then Value = 0

            mSICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder = Value
        End Set
    End Property

    Public Property SaveSmoothedData() As Boolean
        Get
            Return mSICOptions.SaveSmoothedData
        End Get
        Set(Value As Boolean)
            mSICOptions.SaveSmoothedData = Value
        End Set
    End Property

    Public Property MaxAllowedUpwardSpikeFractionMax() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 1 Then Value = 0.2
            mSICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax = Value
        End Set
    End Property

    Public Property InitialPeakWidthScansScaler() As Single
        Get
            Return mSICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler
        End Get
        Set(Value As Single)
            If Value < 0.001 Or Value > 1000 Then Value = 0.5
            mSICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler = Value
        End Set
    End Property

    Public Property InitialPeakWidthScansMaximum() As Integer
        Get
            Return mSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum
        End Get
        Set(Value As Integer)
            If Value < 3 Or Value > 1000 Then Value = 6
            mSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum = Value
        End Set
    End Property
#End Region

#Region "Spectrum Similarity Options"
    Public Property SimilarIonMZToleranceHalfWidth() As Single
        Get
            Return mSICOptions.SimilarIonMZToleranceHalfWidth
        End Get
        Set(Value As Single)
            If Value < 0.001 Or Value > 100 Then Value = 0.1
            mSICOptions.SimilarIonMZToleranceHalfWidth = Value
        End Set
    End Property

    Public Property SimilarIonToleranceHalfWidthMinutes() As Single
        Get
            Return mSICOptions.SimilarIonToleranceHalfWidthMinutes
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 100000 Then Value = 5
            mSICOptions.SimilarIonToleranceHalfWidthMinutes = Value
        End Set
    End Property

    Public Property SpectrumSimilarityMinimum() As Single
        Get
            Return mSICOptions.SpectrumSimilarityMinimum
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 1 Then Value = 0.8
            mSICOptions.SpectrumSimilarityMinimum = Value
        End Set
    End Property
#End Region

#Region "Binning Options Interface Functions"

    Public Property BinStartX() As Single
        Get
            Return mBinningOptions.StartX
        End Get
        Set(Value As Single)
            mBinningOptions.StartX = Value
        End Set
    End Property

    Public Property BinEndX() As Single
        Get
            Return mBinningOptions.EndX
        End Get
        Set(Value As Single)
            mBinningOptions.EndX = Value
        End Set
    End Property

    Public Property BinSize() As Single
        Get
            Return mBinningOptions.BinSize
        End Get
        Set(Value As Single)
            If Value <= 0 Then Value = 1
            mBinningOptions.BinSize = Value
        End Set
    End Property

    Public Property BinnedDataIntensityPrecisionPercent() As Single
        Get
            Return mBinningOptions.IntensityPrecisionPercent
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > 100 Then Value = 1
            mBinningOptions.IntensityPrecisionPercent = Value
        End Set
    End Property

    Public Property NormalizeBinnedData() As Boolean
        Get
            Return mBinningOptions.Normalize
        End Get
        Set(Value As Boolean)
            mBinningOptions.Normalize = Value
        End Set
    End Property

    Public Property SumAllIntensitiesForBin() As Boolean
        Get
            Return mBinningOptions.SumAllIntensitiesForBin
        End Get
        Set(Value As Boolean)
            mBinningOptions.SumAllIntensitiesForBin = Value
        End Set
    End Property

    Public Property MaximumBinCount() As Integer
        Get
            Return mBinningOptions.MaximumBinCount
        End Get
        Set(Value As Integer)
            If Value < 2 Then Value = 10
            If Value > 1000000 Then Value = 1000000
            mBinningOptions.MaximumBinCount = Value
        End Set
    End Property
#End Region

#Region "Memory Options Interface Functions"

    Public Property DiskCachingAlwaysDisabled() As Boolean
        Get
            Return mCacheOptions.DiskCachingAlwaysDisabled
        End Get
        Set(Value As Boolean)
            mCacheOptions.DiskCachingAlwaysDisabled = Value
        End Set
    End Property

    Public Property CacheFolderPath() As String
        Get
            Return mCacheOptions.FolderPath
        End Get
        Set(Value As String)
            mCacheOptions.FolderPath = Value
        End Set
    End Property

    Public Property CacheMaximumMemoryUsageMB() As Single
        Get
            Return mCacheOptions.MaximumMemoryUsageMB
        End Get
        Set(Value As Single)
            mCacheOptions.MaximumMemoryUsageMB = Value
        End Set
    End Property

    Public Property CacheMinimumFreeMemoryMB() As Single
        Get
            Return mCacheOptions.MinimumFreeMemoryMB
        End Get
        Set(Value As Single)
            If mCacheOptions.MinimumFreeMemoryMB < 10 Then
                mCacheOptions.MinimumFreeMemoryMB = 10
            End If
            mCacheOptions.MinimumFreeMemoryMB = Value
        End Set
    End Property

    Public Property CacheSpectraToRetainInMemory() As Integer
        Get
            Return mCacheOptions.SpectraToRetainInMemory
        End Get
        Set(Value As Integer)
            If Value < 100 Then Value = 100
            mCacheOptions.SpectraToRetainInMemory = Value
        End Set
    End Property

#End Region

    Private Sub AddCustomSICValues(
      ByRef udtScanList As udtScanListType,
      dblDefaultSICTolerance As Double,
      blnSICToleranceIsPPM As Boolean,
      sngDefaultScanOrAcqTimeTolerance As Single)

        Dim intIndex As Integer

        Dim intScanOrAcqTimeSumCount = 0
        Dim sngScanOrAcqTimeSumForAveraging As Single = 0

        Try
            If mCustomSICList.CustomMZSearchValues.Length > 0 Then
                ReDim Preserve udtScanList.ParentIons(udtScanList.ParentIonInfoCount + mCustomSICList.CustomMZSearchValues.Length - 1)

                For intIndex = 0 To mCustomSICList.CustomMZSearchValues.Length - 1

                    ' Add a new parent ion entry to .ParentIons() for this custom MZ value
                    With udtScanList
                        With .ParentIons(.ParentIonInfoCount)
                            .MZ = mCustomSICList.CustomMZSearchValues(intIndex).MZ

                            If mCustomSICList.CustomMZSearchValues(intIndex).ScanOrAcqTimeCenter < Single.Epsilon Then
                                ' Set the SurveyScanIndex to the center of the analysis
                                .SurveyScanIndex = FindNearestSurveyScanIndex(udtScanList, 0.5, eCustomSICScanTypeConstants.Relative)
                            Else
                                .SurveyScanIndex = FindNearestSurveyScanIndex(udtScanList, mCustomSICList.CustomMZSearchValues(intIndex).ScanOrAcqTimeCenter, mCustomSICList.ScanToleranceType)
                            End If

                            ' Find the next MS2 scan that occurs after the survey scan (parent scan)
                            Dim surveyScanNumberAbsolute = 0
                            If .SurveyScanIndex < udtScanList.SurveyScanCount Then
                                surveyScanNumberAbsolute = udtScanList.SurveyScans(.SurveyScanIndex).ScanNumber + 1
                            End If

                            If udtScanList.MasterScanOrderCount = 0 Then
                                ReDim .FragScanIndices(0)
                                .FragScanIndices(0) = 0
                            Else
                                Dim fragScanIndexMatch = clsBinarySearch.BinarySearchFindNearest(udtScanList.MasterScanNumList, surveyScanNumberAbsolute, udtScanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)

                                While fragScanIndexMatch < udtScanList.MasterScanOrderCount AndAlso udtScanList.MasterScanOrder(fragScanIndexMatch).ScanType = eScanTypeConstants.SurveyScan
                                    fragScanIndexMatch += 1
                                End While

                                If fragScanIndexMatch = udtScanList.MasterScanOrderCount Then
                                    ' Did not find the next frag scan; find the previous frag scan
                                    fragScanIndexMatch -= 1
                                    While fragScanIndexMatch > 0 AndAlso udtScanList.MasterScanOrder(fragScanIndexMatch).ScanType = eScanTypeConstants.SurveyScan
                                        fragScanIndexMatch -= 1
                                    End While
                                    If fragScanIndexMatch < 0 Then fragScanIndexMatch = 0
                                End If

                                ' This is a custom SIC-based parent ion
                                ' Prior to August 2014, we set .FragScanIndices(0) = 0, which made it appear that the fragmentation scan was the first MS2 spectrum in the dataset for all custom SICs
                                ' This caused undesirable display results in MASIC browser, so we now set it to the next MS2 scan that occurs after the survey scan (parent scan)
                                ReDim .FragScanIndices(0)
                                If udtScanList.MasterScanOrder(fragScanIndexMatch).ScanType = eScanTypeConstants.FragScan Then
                                    .FragScanIndices(0) = udtScanList.MasterScanOrder(fragScanIndexMatch).ScanIndexPointer
                                Else
                                    .FragScanIndices(0) = 0
                                End If
                            End If

                            .FragScanIndexCount = 1
                            .CustomSICPeak = True
                            .CustomSICPeakComment = mCustomSICList.CustomMZSearchValues(intIndex).Comment
                            .CustomSICPeakMZToleranceDa = mCustomSICList.CustomMZSearchValues(intIndex).MZToleranceDa
                            .CustomSICPeakScanOrAcqTimeTolerance = mCustomSICList.CustomMZSearchValues(intIndex).ScanOrAcqTimeTolerance

                            If .CustomSICPeakMZToleranceDa < Double.Epsilon Then
                                If blnSICToleranceIsPPM Then
                                    .CustomSICPeakMZToleranceDa = PPMToMass(dblDefaultSICTolerance, .MZ)
                                Else
                                    .CustomSICPeakMZToleranceDa = dblDefaultSICTolerance
                                End If
                            End If

                            If .CustomSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                                .CustomSICPeakScanOrAcqTimeTolerance = sngDefaultScanOrAcqTimeTolerance
                            Else
                                sngScanOrAcqTimeSumForAveraging += .CustomSICPeakScanOrAcqTimeTolerance
                                intScanOrAcqTimeSumCount += 1
                            End If

                        End With

                        If .ParentIons(.ParentIonInfoCount).SurveyScanIndex < .SurveyScans.Count Then
                            .ParentIons(.ParentIonInfoCount).OptimalPeakApexScanNumber = .SurveyScans(.ParentIons(.ParentIonInfoCount).SurveyScanIndex).ScanNumber
                        Else
                            .ParentIons(.ParentIonInfoCount).OptimalPeakApexScanNumber = 1
                        End If

                        .ParentIons(.ParentIonInfoCount).PeakApexOverrideParentIonIndex = -1
                        .ParentIonInfoCount += 1
                    End With

                Next intIndex

                If intScanOrAcqTimeSumCount = mCustomSICList.CustomMZSearchValues.Length AndAlso sngScanOrAcqTimeSumForAveraging > 0 Then
                    ' All of the entries had a custom scan or acq time tolerance defined
                    ' Update mCustomSICList.ScanOrAcqTimeTolerance to the average of the values
                    mCustomSICList.ScanOrAcqTimeTolerance = CSng(Math.Round(sngScanOrAcqTimeSumForAveraging / intScanOrAcqTimeSumCount, 4))
                End If
            End If

        Catch ex As Exception
            LogErrors("AddCustomSICValues", "Error in AddCustomSICValues", ex, True, False)
        End Try

    End Sub

    Private Function AddFakeSurveyScan(ByRef udtScanList As udtScanListType) As Integer
        Const intScanNumber = 0
        Const sngScanTime As Single = 0

        Return AddFakeSurveyScan(udtScanList, intScanNumber, sngScanTime)
    End Function

    Private Function AddFakeSurveyScan(
      ByRef udtScanList As udtScanListType,
      intScanNumber As Integer,
      sngScanTime As Single) As Integer

        ' Adds a "fake" survey scan with the given scan number and scan time
        ' Returns the index in udtScanList.SurveyScans() at which the new scan was added

        Dim intSurveyScanIndex As Integer

        With udtScanList.SurveyScans(udtScanList.SurveyScanCount)
            .ScanNumber = intScanNumber
            .ScanTime = sngScanTime
            .ScanHeaderText = "Full ms"
            .ScanTypeName = "MS"

            .BasePeakIonMZ = 0
            .BasePeakIonIntensity = 0
            .FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here
            .TotalIonIntensity = 0

            .ZoomScan = False
            .SIMScan = False
            .MRMScanType = MRMScanTypeConstants.NotMRM

            .LowMass = 0
            .HighMass = 0

            ' Store the collision mode and possibly the scan filter text
            .FragScanInfo.CollisionMode = String.Empty
        End With

        intSurveyScanIndex = udtScanList.SurveyScanCount
        udtScanList.SurveyScanCount += 1

        AddMasterScanEntry(udtScanList, eScanTypeConstants.SurveyScan, intSurveyScanIndex)

        Return intSurveyScanIndex
    End Function

    Private Sub AddMasterScanEntry(ByRef udtScanList As udtScanListType, eScanType As eScanTypeConstants, intScanIndex As Integer)
        ' Adds a new entry to .MasterScanOrder using an existing entry in udtScanList.SurveyScans() or udtScanList.FragScans()

        If eScanType = eScanTypeConstants.SurveyScan Then
            If udtScanList.SurveyScanCount > 0 AndAlso intScanIndex < udtScanList.SurveyScanCount Then
                AddMasterScanEntry(udtScanList, eScanType, intScanIndex, udtScanList.SurveyScans(intScanIndex).ScanNumber, udtScanList.SurveyScans(intScanIndex).ScanTime)
            Else
                ' This code shouldn't normally be reached
                AddMasterScanEntry(udtScanList, eScanType, intScanIndex, 0, 0)
            End If

        ElseIf eScanType = eScanTypeConstants.FragScan Then
            If udtScanList.FragScanCount > 0 AndAlso intScanIndex < udtScanList.FragScanCount Then
                AddMasterScanEntry(udtScanList, eScanType, intScanIndex, udtScanList.FragScans(intScanIndex).ScanNumber, udtScanList.FragScans(intScanIndex).ScanTime)
            Else
                ' This code shouldn't normally be reached
                AddMasterScanEntry(udtScanList, eScanType, intScanIndex, 0, 0)
            End If

        Else
            ' Unknown type; cannot add
            LogErrors("AddMasterScanEntry", "Programming error: unknown value for eScanType", Nothing, True, False)
        End If

    End Sub

    Private Sub AddMasterScanEntry(
       ByRef udtScanList As udtScanListType,
       eScanType As eScanTypeConstants,
       intScanIndex As Integer,
       intScanNumber As Integer,
       sngScanTime As Single)

        With udtScanList
            If .MasterScanOrder Is Nothing Then
                ReDim .MasterScanOrder(99)
                ReDim .MasterScanNumList(99)
                ReDim .MasterScanTimeList(99)
            ElseIf .MasterScanOrderCount >= .MasterScanOrder.Length Then
                ReDim Preserve .MasterScanOrder(.MasterScanOrderCount + 100)
                ReDim Preserve .MasterScanNumList(.MasterScanOrder.Length - 1)
                ReDim Preserve .MasterScanTimeList(.MasterScanOrder.Length - 1)
            End If

            With .MasterScanOrder(.MasterScanOrderCount)
                .ScanType = eScanType
                .ScanIndexPointer = intScanIndex
            End With

            .MasterScanNumList(.MasterScanOrderCount) = intScanNumber
            .MasterScanTimeList(.MasterScanOrderCount) = sngScanTime

            .MasterScanOrderCount += 1
        End With

    End Sub

    Private Sub AddUpdateParentIons(
      ByRef udtScanList As udtScanListType,
      intSurveyScanIndex As Integer,
      dblParentIonMZ As Double,
      intFragScanIndex As Integer,
      objSpectraCache As clsSpectraCache,
      udtSICOptions As udtSICOptionsType)

        AddUpdateParentIons(udtScanList, intSurveyScanIndex, dblParentIonMZ, 0, 0, intFragScanIndex, objSpectraCache, udtSICOptions)
    End Sub

    Private Sub AddUpdateParentIons(
      ByRef udtScanList As udtScanListType,
      intSurveyScanIndex As Integer,
      dblParentIonMZ As Double,
      ByRef udtMRMInfo As udtMRMScanInfoType,
      objSpectraCache As clsSpectraCache,
      ByRef udtSICOptions As udtSICOptionsType)

        Dim intMRMIndex As Integer
        Dim dblMRMDaughterMZ As Double
        Dim dblMRMToleranceHalfWidth As Double


        For intMRMIndex = 0 To udtMRMInfo.MRMMassCount - 1
            dblMRMDaughterMZ = udtMRMInfo.MRMMassList(intMRMIndex).CentralMass
            dblMRMToleranceHalfWidth = Math.Round((udtMRMInfo.MRMMassList(intMRMIndex).EndMass - udtMRMInfo.MRMMassList(intMRMIndex).StartMass) / 2, 6)
            If dblMRMToleranceHalfWidth < 0.001 Then
                dblMRMToleranceHalfWidth = 0.001
            End If

            AddUpdateParentIons(udtScanList, intSurveyScanIndex, dblParentIonMZ, dblMRMDaughterMZ, dblMRMToleranceHalfWidth, udtScanList.FragScanCount - 1, objSpectraCache, udtSICOptions)
        Next intMRMIndex

    End Sub

    Private Sub AddUpdateParentIons(
      ByRef udtScanList As udtScanListType,
      intSurveyScanIndex As Integer,
      dblParentIonMZ As Double,
      dblMRMDaughterMZ As Double,
      dblMRMToleranceHalfWidth As Double,
      intFragScanIndex As Integer,
      objSpectraCache As clsSpectraCache,
      udtSICOptions As udtSICOptionsType)

        Const MINIMUM_TOLERANCE_PPM = 0.01
        Const MINIMUM_TOLERANCE_DA = 0.0001

        ' Checks to see if the parent ion specified by intSurveyScanIndex and dblParentIonMZ exists in .ParentIons()
        ' If dblMRMDaughterMZ is > 0, then also considers that value when determining uniqueness
        ' 
        ' If the parent ion entry already exists, then adds an entry to .FragScanIndices()
        ' If it does not exist, then adds a new entry to .ParentIons()
        ' Note that typically intFragScanIndex will equal udtScanList.FragScanCount - 1

        ' If intSurveyScanIndex < 0 then the first scan(s) in the file occurred before we encountered a survey scan
        ' In this case, we cannot properly associate the fragmentation scan with a survey scan

        Dim intParentIonIndex As Integer
        Dim intIndex As Integer
        Dim dblParentIonTolerance As Double
        Dim dblParentIonToleranceDa As Double

        Dim dblParentIonMZMatch As Double

        Dim blnMatchFound As Boolean

        If udtSICOptions.SICToleranceIsPPM Then
            dblParentIonTolerance = udtSICOptions.SICTolerance / udtSICOptions.CompressToleranceDivisorForPPM
            If dblParentIonTolerance < MINIMUM_TOLERANCE_PPM Then
                dblParentIonTolerance = MINIMUM_TOLERANCE_PPM
            End If
        Else
            dblParentIonTolerance = udtSICOptions.SICTolerance / udtSICOptions.CompressToleranceDivisorForDa
            If dblParentIonTolerance < MINIMUM_TOLERANCE_DA Then
                dblParentIonTolerance = MINIMUM_TOLERANCE_DA
            End If
        End If

        ' See if an entry exists yet in .ParentIons for the parent ion for this fragmentation scan
        blnMatchFound = False

        If dblMRMDaughterMZ > 0 Then
            If udtSICOptions.SICToleranceIsPPM Then
                ' Force the tolerances to 0.01 m/z units
                dblParentIonTolerance = MINIMUM_TOLERANCE_PPM
            Else
                ' Force the tolerances to 0.01 m/z units
                dblParentIonTolerance = MINIMUM_TOLERANCE_DA
            End If
        End If

        If dblParentIonMZ > 0 Then

            dblParentIonToleranceDa = GetParentIonToleranceDa(udtSICOptions, dblParentIonMZ, dblParentIonTolerance)

            For intParentIonIndex = udtScanList.ParentIonInfoCount - 1 To 0 Step -1
                If udtScanList.ParentIons(intParentIonIndex).SurveyScanIndex >= intSurveyScanIndex Then
                    If Math.Abs(udtScanList.ParentIons(intParentIonIndex).MZ - dblParentIonMZ) <= dblParentIonToleranceDa Then
                        If dblMRMDaughterMZ < Double.Epsilon OrElse
                          Math.Abs(udtScanList.ParentIons(intParentIonIndex).MRMDaughterMZ - dblMRMDaughterMZ) <= dblParentIonToleranceDa Then
                            blnMatchFound = True
                            Exit For
                        End If
                    End If
                Else
                    Exit For
                End If
            Next intParentIonIndex
        End If


        If Not blnMatchFound Then
            ' Add a new parent ion entry to .ParentIons(), but only if intSurveyScanIndex >= 0

            If intSurveyScanIndex >= 0 Then
                With udtScanList
                    If .ParentIonInfoCount >= .ParentIons.Length Then
                        ReDim Preserve .ParentIons(.ParentIonInfoCount + 100)
                        For intIndex = .ParentIonInfoCount To .ParentIons.Length - 1
                            ReDim .ParentIons(intIndex).FragScanIndices(0)
                        Next intIndex
                    End If

                    With .ParentIons(.ParentIonInfoCount)
                        .MZ = dblParentIonMZ
                        .SurveyScanIndex = intSurveyScanIndex

                        .FragScanIndices(0) = intFragScanIndex
                        .FragScanIndexCount = 1
                        .CustomSICPeak = False

                        .MRMDaughterMZ = dblMRMDaughterMZ
                        .MRMToleranceHalfWidth = dblMRMToleranceHalfWidth
                    End With
                    .ParentIons(.ParentIonInfoCount).OptimalPeakApexScanNumber = .SurveyScans(intSurveyScanIndex).ScanNumber        ' Was: .FragScans(intFragScanIndex).ScanNumber
                    .ParentIons(.ParentIonInfoCount).PeakApexOverrideParentIonIndex = -1
                    .FragScans(intFragScanIndex).FragScanInfo.ParentIonInfoIndex = .ParentIonInfoCount

                    ' Look for .MZ in the survey scan, using a tolerance of dblParentIonTolerance
                    ' If found, then update the mass to the matched ion
                    ' This is done to determine the parent ion mass more precisely
                    If udtSICOptions.RefineReportedParentIonMZ Then
                        If FindClosestMZ(objSpectraCache, .SurveyScans, intSurveyScanIndex, dblParentIonMZ, dblParentIonTolerance, dblParentIonMZMatch) Then
                            .ParentIons(.ParentIonInfoCount).MZ = dblParentIonMZMatch
                        End If
                    End If

                    .ParentIonInfoCount += 1
                End With
            End If
        Else
            ' Add a new entry to .FragScanIndices() for the matching parent ion
            ' However, do not add a new entry if this is an MRM scan

            If dblMRMDaughterMZ < Double.Epsilon Then
                With udtScanList
                    With .ParentIons(intParentIonIndex)
                        ReDim Preserve .FragScanIndices(.FragScanIndexCount)
                        .FragScanIndices(.FragScanIndexCount) = intFragScanIndex
                        .FragScanIndexCount += 1
                    End With
                    .FragScans(intFragScanIndex).FragScanInfo.ParentIonInfoIndex = intParentIonIndex
                End With
            End If
        End If

    End Sub

    Private Sub AppendParentIonToUniqueMZEntry(
      ByRef udtScanList As udtScanListType,
      intParentIonIndex As Integer,
      ByRef udtMZListEntry As udtUniqueMZListType,
      dblSearchMZOffset As Double)

        With udtScanList.ParentIons(intParentIonIndex)
            If udtMZListEntry.MatchCount = 0 Then
                udtMZListEntry.MZAvg = .MZ - dblSearchMZOffset
                udtMZListEntry.MatchCount = 1

                ' Note that .MatchIndices() was initialized in InitializeUniqueMZListMatchIndices()
                udtMZListEntry.MatchIndices(0) = intParentIonIndex
            Else
                ' Update the average MZ: NewAvg = (OldAvg * OldCount + NewValue) / NewCount
                udtMZListEntry.MZAvg = (udtMZListEntry.MZAvg * udtMZListEntry.MatchCount + (.MZ - dblSearchMZOffset)) / (udtMZListEntry.MatchCount + 1)

                ReDim Preserve udtMZListEntry.MatchIndices(udtMZListEntry.MatchCount)
                udtMZListEntry.MatchIndices(udtMZListEntry.MatchCount) = intParentIonIndex
                udtMZListEntry.MatchCount += 1
            End If

            With .SICStats
                If .Peak.MaxIntensityValue > udtMZListEntry.MaxIntensity OrElse udtMZListEntry.MatchCount = 1 Then
                    udtMZListEntry.MaxIntensity = .Peak.MaxIntensityValue
                    If .ScanTypeForPeakIndices = eScanTypeConstants.FragScan Then
                        udtMZListEntry.ScanNumberMaxIntensity = udtScanList.FragScans(.PeakScanIndexMax).ScanNumber
                        udtMZListEntry.ScanTimeMaxIntensity = udtScanList.FragScans(.PeakScanIndexMax).ScanTime
                    Else
                        udtMZListEntry.ScanNumberMaxIntensity = udtScanList.SurveyScans(.PeakScanIndexMax).ScanNumber
                        udtMZListEntry.ScanTimeMaxIntensity = udtScanList.SurveyScans(.PeakScanIndexMax).ScanTime
                    End If
                    udtMZListEntry.ParentIonIndexMaxIntensity = intParentIonIndex
                End If

                If .Peak.Area > udtMZListEntry.MaxPeakArea OrElse udtMZListEntry.MatchCount = 1 Then
                    udtMZListEntry.MaxPeakArea = .Peak.Area
                    udtMZListEntry.ParentIonIndexMaxPeakArea = intParentIonIndex
                End If
            End With

        End With

    End Sub

    Private Function CheckForExistingResults(
      strInputFilePathFull As String,
      strOutputFolderPath As String,
      udtSICOptions As udtSICOptionsType,
      udtBinningOptions As clsCorrelation.udtBinningOptionsType) As Boolean

        ' Returns True if existing results already exist for the given input file path, SIC Options, and Binning options

        Dim strFilePathToCheck As String

        Dim objXMLDoc As Xml.XmlDocument
        Dim objMatchingNodeList As Xml.XmlNodeList
        Dim objValueNode As Xml.XmlNode

        Dim udtSICOptionsCompare As udtSICOptionsType
        Dim udtBinningOptionsCompare As clsCorrelation.udtBinningOptionsType
        Dim udtCustomMZListCompare = New udtCustomMZSearchListType

        Dim blnValidExistingResultsFound As Boolean

        Dim lngSourceFileSizeBytes As Int64
        Dim strSourceFilePathCheck As String = String.Empty
        Dim strMASICVersion As String = String.Empty
        Dim strMASICPeakFinderDllVersion As String = String.Empty
        Dim strSourceFileDateTimeCheck As String = String.Empty
        Dim dtSourceFileDateTime As Date

        Dim blnSkipMSMSProcessing As Boolean

        blnValidExistingResultsFound = False
        Try
            ' Don't even look for the XML file if mSkipSICAndRawDataProcessing = True
            If mSkipSICAndRawDataProcessing Then
                Return False
            End If

            ' Obtain the output XML filename
            strFilePathToCheck = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.XMLFile)

            ' See if the file exists
            If File.Exists(strFilePathToCheck) Then

                If mFastExistingXMLFileTest Then
                    ' XML File found; do not check the settings or version to see if they match the current ones
                    Return True
                End If

                ' Open the XML file and look for the "ProcessingComplete" node
                objXMLDoc = New Xml.XmlDocument
                Try
                    objXMLDoc.Load(strFilePathToCheck)
                Catch ex As Exception
                    ' Invalid XML file; do not continue
                    Return False
                End Try

                ' If we get here, the file opened successfully
                Dim objRootElement As Xml.XmlElement = objXMLDoc.DocumentElement

                If objRootElement.Name = "SICData" Then
                    ' See if the ProcessingComplete node has a value of True
                    objMatchingNodeList = objRootElement.GetElementsByTagName("ProcessingComplete")
                    If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then Exit Try
                    If objMatchingNodeList.Item(0).InnerText.ToLower <> "true" Then Exit Try

                    ' Read the ProcessingSummary and populate
                    objMatchingNodeList = objRootElement.GetElementsByTagName("ProcessingSummary")
                    If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then Exit Try

                    For Each objValueNode In objMatchingNodeList(0).ChildNodes
                        With udtSICOptionsCompare
                            Select Case objValueNode.Name
                                Case "DatasetNumber" : .DatasetNumber = CInt(objValueNode.InnerText)
                                Case "SourceFilePath" : strSourceFilePathCheck = objValueNode.InnerText
                                Case "SourceFileDateTime" : strSourceFileDateTimeCheck = objValueNode.InnerText
                                Case "SourceFileSizeBytes" : lngSourceFileSizeBytes = CLng(objValueNode.InnerText)
                                Case "MASICVersion" : strMASICVersion = objValueNode.InnerText
                                Case "MASICPeakFinderDllVersion" : strMASICPeakFinderDllVersion = objValueNode.InnerText
                                Case "SkipMSMSProcessing" : blnSkipMSMSProcessing = CBool(objValueNode.InnerText)
                            End Select
                        End With
                    Next objValueNode

                    If strMASICVersion Is Nothing Then strMASICVersion = String.Empty
                    If strMASICPeakFinderDllVersion Is Nothing Then strMASICPeakFinderDllVersion = String.Empty

                    ' Check if the MASIC version matches
                    If strMASICVersion <> MyBase.FileVersion() Then Exit Try

                    If strMASICPeakFinderDllVersion <> mMASICPeakFinder.ProgramVersion Then Exit Try

                    ' Check the dataset number
                    If udtSICOptionsCompare.DatasetNumber <> udtSICOptions.DatasetNumber Then Exit Try

                    ' Check the filename in strSourceFilePathCheck
                    If Path.GetFileName(strSourceFilePathCheck) <> Path.GetFileName(strInputFilePathFull) Then Exit Try

                    ' Check if the source file stats match
                    Dim ioFileInfo As New FileInfo(strInputFilePathFull)
                    dtSourceFileDateTime = ioFileInfo.LastWriteTime()
                    If strSourceFileDateTimeCheck <> (dtSourceFileDateTime.ToShortDateString & " " & dtSourceFileDateTime.ToShortTimeString) Then Exit Try
                    If lngSourceFileSizeBytes <> ioFileInfo.Length Then Exit Try

                    ' Check that blnSkipMSMSProcessing matches
                    If blnSkipMSMSProcessing <> mSkipMSMSProcessing Then Exit Try

                    ' Read the ProcessingOptions and populate
                    objMatchingNodeList = objRootElement.GetElementsByTagName("ProcessingOptions")
                    If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then Exit Try

                    For Each objValueNode In objMatchingNodeList(0).ChildNodes
                        With udtSICOptionsCompare
                            Select Case objValueNode.Name
                                Case "SICToleranceDa" : .SICTolerance = CDbl(objValueNode.InnerText)            ' Legacy name
                                Case "SICTolerance" : .SICTolerance = CDbl(objValueNode.InnerText)
                                Case "SICToleranceIsPPM" : .SICToleranceIsPPM = CBool(objValueNode.InnerText)
                                Case "RefineReportedParentIonMZ" : .RefineReportedParentIonMZ = CBool(objValueNode.InnerText)
                                Case "ScanRangeEnd" : .ScanRangeEnd = CInt(objValueNode.InnerText)
                                Case "ScanRangeStart" : .ScanRangeStart = CInt(objValueNode.InnerText)
                                Case "RTRangeEnd" : .RTRangeEnd = CSng(objValueNode.InnerText)
                                Case "RTRangeStart" : .RTRangeStart = CSng(objValueNode.InnerText)

                                Case "CompressMSSpectraData" : .CompressMSSpectraData = CBool(objValueNode.InnerText)
                                Case "CompressMSMSSpectraData" : .CompressMSMSSpectraData = CBool(objValueNode.InnerText)

                                Case "CompressToleranceDivisorForDa" : .CompressToleranceDivisorForDa = CDbl(objValueNode.InnerText)
                                Case "CompressToleranceDivisorForPPM" : .CompressToleranceDivisorForPPM = CDbl(objValueNode.InnerText)

                                Case "MaxSICPeakWidthMinutesBackward" : .MaxSICPeakWidthMinutesBackward = CSng(objValueNode.InnerText)
                                Case "MaxSICPeakWidthMinutesForward" : .MaxSICPeakWidthMinutesForward = CSng(objValueNode.InnerText)

                                Case "ReplaceSICZeroesWithMinimumPositiveValueFromMSData" : .ReplaceSICZeroesWithMinimumPositiveValueFromMSData = CBool(objValueNode.InnerText)
                                Case "SaveSmoothedData" : .SaveSmoothedData = CBool(objValueNode.InnerText)

                                Case "SimilarIonMZToleranceHalfWidth" : .SimilarIonMZToleranceHalfWidth = CSng(objValueNode.InnerText)
                                Case "SimilarIonToleranceHalfWidthMinutes" : .SimilarIonToleranceHalfWidthMinutes = CSng(objValueNode.InnerText)
                                Case "SpectrumSimilarityMinimum" : .SpectrumSimilarityMinimum = CSng(objValueNode.InnerText)
                                Case Else
                                    With .SICPeakFinderOptions
                                        Select Case objValueNode.Name
                                            Case "IntensityThresholdFractionMax" : .IntensityThresholdFractionMax = CSng(objValueNode.InnerText)
                                            Case "IntensityThresholdAbsoluteMinimum" : .IntensityThresholdAbsoluteMinimum = CSng(objValueNode.InnerText)

                                            Case "SICNoiseThresholdMode" : .SICBaselineNoiseOptions.BaselineNoiseMode = CType(objValueNode.InnerText, MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
                                            Case "SICNoiseThresholdIntensity" : .SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = CSng(objValueNode.InnerText)
                                            Case "SICNoiseFractionLowIntensityDataToAverage" : .SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = CSng(objValueNode.InnerText)
                                            Case "SICNoiseMinimumSignalToNoiseRatio" : .SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = CSng(objValueNode.InnerText)

                                            Case "MaxDistanceScansNoOverlap" : .MaxDistanceScansNoOverlap = CInt(objValueNode.InnerText)
                                            Case "MaxAllowedUpwardSpikeFractionMax" : .MaxAllowedUpwardSpikeFractionMax = CSng(objValueNode.InnerText)
                                            Case "InitialPeakWidthScansScaler" : .InitialPeakWidthScansScaler = CSng(objValueNode.InnerText)
                                            Case "InitialPeakWidthScansMaximum" : .InitialPeakWidthScansMaximum = CInt(objValueNode.InnerText)

                                            Case "FindPeaksOnSmoothedData" : .FindPeaksOnSmoothedData = CBool(objValueNode.InnerText)
                                            Case "SmoothDataRegardlessOfMinimumPeakWidth" : .SmoothDataRegardlessOfMinimumPeakWidth = CBool(objValueNode.InnerText)
                                            Case "UseButterworthSmooth" : .UseButterworthSmooth = CBool(objValueNode.InnerText)
                                            Case "ButterworthSamplingFrequency" : .ButterworthSamplingFrequency = CSng(objValueNode.InnerText)
                                            Case "ButterworthSamplingFrequencyDoubledForSIMData" : .ButterworthSamplingFrequencyDoubledForSIMData = CBool(objValueNode.InnerText)

                                            Case "UseSavitzkyGolaySmooth" : .UseSavitzkyGolaySmooth = CBool(objValueNode.InnerText)
                                            Case "SavitzkyGolayFilterOrder" : .SavitzkyGolayFilterOrder = CShort(objValueNode.InnerText)

                                            Case "MassSpectraNoiseThresholdMode" : .MassSpectraNoiseThresholdOptions.BaselineNoiseMode = CType(objValueNode.InnerText, MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
                                            Case "MassSpectraNoiseThresholdIntensity" : .MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = CSng(objValueNode.InnerText)
                                            Case "MassSpectraNoiseFractionLowIntensityDataToAverage" : .MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = CSng(objValueNode.InnerText)
                                            Case "MassSpectraNoiseMinimumSignalToNoiseRatio" : .MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = CSng(objValueNode.InnerText)
                                        End Select
                                    End With
                            End Select
                        End With
                    Next objValueNode

                    ' Read the BinningOptions and populate
                    objMatchingNodeList = objRootElement.GetElementsByTagName("BinningOptions")
                    If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then Exit Try

                    For Each objValueNode In objMatchingNodeList(0).ChildNodes
                        With udtBinningOptionsCompare
                            Select Case objValueNode.Name
                                Case "BinStartX" : .StartX = CSng(objValueNode.InnerText)
                                Case "BinEndX" : .EndX = CSng(objValueNode.InnerText)
                                Case "BinSize" : .BinSize = CSng(objValueNode.InnerText)
                                Case "MaximumBinCount" : .MaximumBinCount = CInt(objValueNode.InnerText)

                                Case "IntensityPrecisionPercent" : .IntensityPrecisionPercent = CSng(objValueNode.InnerText)
                                Case "Normalize" : .Normalize = CBool(objValueNode.InnerText)
                                Case "SumAllIntensitiesForBin" : .SumAllIntensitiesForBin = CBool(objValueNode.InnerText)
                            End Select
                        End With
                    Next objValueNode

                    ' Read the CustomSICValues and populate
                    InitializeCustomMZList(udtCustomMZListCompare)

                    objMatchingNodeList = objRootElement.GetElementsByTagName("CustomSICValues")
                    If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then
                        ' Custom values not defined; that's OK
                    Else
                        For Each objValueNode In objMatchingNodeList(0).ChildNodes
                            With udtCustomMZListCompare
                                Select Case objValueNode.Name
                                    Case "MZList" : .RawTextMZList = objValueNode.InnerText
                                    Case "MZToleranceDaList" : .RawTextMZToleranceDaList = objValueNode.InnerText
                                    Case "ScanCenterList" : .RawTextScanOrAcqTimeCenterList = objValueNode.InnerText
                                    Case "ScanToleranceList" : .RawTextScanOrAcqTimeToleranceList = objValueNode.InnerText
                                    Case "ScanTolerance" : .ScanOrAcqTimeTolerance = CSng(objValueNode.InnerText)
                                    Case "ScanType"
                                        .ScanToleranceType = GetScanToleranceTypeFromText(objValueNode.InnerText)
                                End Select
                            End With
                        Next objValueNode
                    End If

                    ' Check if the processing options match
                    With udtSICOptionsCompare
                        If ValuesMatch(.SICTolerance, udtSICOptions.SICTolerance, 3) AndAlso
                         .SICToleranceIsPPM = udtSICOptions.SICToleranceIsPPM AndAlso
                         .RefineReportedParentIonMZ = udtSICOptions.RefineReportedParentIonMZ AndAlso
                         .ScanRangeStart = udtSICOptions.ScanRangeStart AndAlso
                         .ScanRangeEnd = udtSICOptions.ScanRangeEnd AndAlso
                         ValuesMatch(.RTRangeStart, udtSICOptions.RTRangeStart, 2) AndAlso
                         ValuesMatch(.RTRangeEnd, udtSICOptions.RTRangeEnd, 2) AndAlso
                         .CompressMSSpectraData = udtSICOptions.CompressMSSpectraData AndAlso
                         .CompressMSMSSpectraData = udtSICOptions.CompressMSMSSpectraData AndAlso
                         ValuesMatch(.CompressToleranceDivisorForDa, udtSICOptions.CompressToleranceDivisorForDa, 2) AndAlso
                         ValuesMatch(.CompressToleranceDivisorForPPM, udtSICOptions.CompressToleranceDivisorForDa, 2) AndAlso
                         ValuesMatch(.MaxSICPeakWidthMinutesBackward, udtSICOptions.MaxSICPeakWidthMinutesBackward, 2) AndAlso
                         ValuesMatch(.MaxSICPeakWidthMinutesForward, udtSICOptions.MaxSICPeakWidthMinutesForward, 2) AndAlso
                         .ReplaceSICZeroesWithMinimumPositiveValueFromMSData = udtSICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData AndAlso
                         ValuesMatch(.SICPeakFinderOptions.IntensityThresholdFractionMax, udtSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax) AndAlso
                         ValuesMatch(.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum, udtSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum) AndAlso
                         .SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = udtSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode AndAlso
                         ValuesMatch(.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute, udtSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute) AndAlso
                         ValuesMatch(.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, udtSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage) AndAlso
                         ValuesMatch(.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio, udtSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio) AndAlso
                         .SICPeakFinderOptions.MaxDistanceScansNoOverlap = udtSICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap AndAlso
                         ValuesMatch(.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, udtSICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax) AndAlso
                         ValuesMatch(.SICPeakFinderOptions.InitialPeakWidthScansScaler, udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler) AndAlso
                         .SICPeakFinderOptions.InitialPeakWidthScansMaximum = udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum AndAlso
                         .SICPeakFinderOptions.FindPeaksOnSmoothedData = udtSICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData AndAlso
                         .SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = udtSICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth AndAlso
                         .SICPeakFinderOptions.UseButterworthSmooth = udtSICOptions.SICPeakFinderOptions.UseButterworthSmooth AndAlso
                         ValuesMatch(.SICPeakFinderOptions.ButterworthSamplingFrequency, udtSICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency) AndAlso
                         .SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = udtSICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData AndAlso
                         .SICPeakFinderOptions.UseSavitzkyGolaySmooth = udtSICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth AndAlso
                         .SICPeakFinderOptions.SavitzkyGolayFilterOrder = udtSICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder AndAlso
                         .SaveSmoothedData = udtSICOptions.SaveSmoothedData AndAlso
                         .SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode AndAlso
                         ValuesMatch(.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute, udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute) AndAlso
                         ValuesMatch(.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage) AndAlso
                         ValuesMatch(.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio, udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio) AndAlso
                         ValuesMatch(.SimilarIonMZToleranceHalfWidth, udtSICOptions.SimilarIonMZToleranceHalfWidth) AndAlso
                         ValuesMatch(.SimilarIonToleranceHalfWidthMinutes, udtSICOptions.SimilarIonToleranceHalfWidthMinutes) AndAlso
                         ValuesMatch(.SpectrumSimilarityMinimum, udtSICOptions.SpectrumSimilarityMinimum) Then
                            blnValidExistingResultsFound = True
                        Else
                            blnValidExistingResultsFound = False
                        End If
                    End With

                    If blnValidExistingResultsFound Then
                        ' Check if the binning options match
                        With udtBinningOptionsCompare
                            If ValuesMatch(.StartX, udtBinningOptions.StartX) AndAlso
                             ValuesMatch(.EndX, udtBinningOptions.EndX) AndAlso
                             ValuesMatch(.BinSize, udtBinningOptions.BinSize) AndAlso
                             .MaximumBinCount = udtBinningOptions.MaximumBinCount AndAlso
                             ValuesMatch(.IntensityPrecisionPercent, udtBinningOptions.IntensityPrecisionPercent) AndAlso
                             .Normalize = udtBinningOptions.Normalize AndAlso
                             .SumAllIntensitiesForBin = udtBinningOptions.SumAllIntensitiesForBin Then

                                blnValidExistingResultsFound = True
                            Else
                                blnValidExistingResultsFound = False
                            End If
                        End With
                    End If

                    If blnValidExistingResultsFound Then
                        ' Check if the Custom MZ options match
                        With udtCustomMZListCompare
                            If .RawTextMZList = mCustomSICList.RawTextMZList AndAlso
                               .RawTextMZToleranceDaList = mCustomSICList.RawTextMZToleranceDaList AndAlso
                               .RawTextScanOrAcqTimeCenterList = mCustomSICList.RawTextScanOrAcqTimeCenterList AndAlso
                               .RawTextScanOrAcqTimeToleranceList = mCustomSICList.RawTextScanOrAcqTimeToleranceList AndAlso
                               ValuesMatch(.ScanOrAcqTimeTolerance, mCustomSICList.ScanOrAcqTimeTolerance) AndAlso
                               .ScanToleranceType = mCustomSICList.ScanToleranceType Then

                                blnValidExistingResultsFound = True
                            Else
                                blnValidExistingResultsFound = False
                            End If
                        End With
                    End If

                    If blnValidExistingResultsFound Then
                        ' All of the options match, make sure the other output files exist
                        blnValidExistingResultsFound = False

                        strFilePathToCheck = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsFlatFile)
                        If Not File.Exists(strFilePathToCheck) Then Exit Try

                        strFilePathToCheck = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.SICStatsFlatFile)
                        If Not File.Exists(strFilePathToCheck) Then Exit Try

                        strFilePathToCheck = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.BPIFile)
                        If Not File.Exists(strFilePathToCheck) Then Exit Try

                        blnValidExistingResultsFound = True
                    End If
                End If
            End If
        Catch ex As Exception
            LogErrors("CheckForExistingResults", "There may be a programming error in CheckForExistingResults", ex, True, False)
            blnValidExistingResultsFound = False
        End Try

        Return blnValidExistingResultsFound

    End Function

    Private Function ValuesMatch(sngValue1 As Single, sngValue2 As Single) As Boolean
        Return ValuesMatch(sngValue1, sngValue2, -1)
    End Function

    Private Function ValuesMatch(sngValue1 As Single, sngValue2 As Single, digitsOfPrecision As Integer) As Boolean

        If digitsOfPrecision < 0 Then
            If Math.Abs(sngValue1 - sngValue2) < Single.Epsilon Then
                Return True
            End If
        Else
            If Math.Abs(Math.Round(sngValue1, digitsOfPrecision) - Math.Round(sngValue2, digitsOfPrecision)) < Single.Epsilon Then
                Return True
            End If

        End If

        Return False

    End Function

    Private Function ValuesMatch(dblValue1 As Double, dblValue2 As Double, digitsOfPrecision As Integer) As Boolean
        If digitsOfPrecision < 0 Then
            If Math.Abs(dblValue1 - dblValue2) < Double.Epsilon Then
                Return True
            End If
        Else
            If Math.Abs(Math.Round(dblValue1, digitsOfPrecision) - Math.Round(dblValue2, digitsOfPrecision)) < Double.Epsilon Then
                Return True
            End If

        End If

        Return False
    End Function

    Private Function CheckPointInMZIgnoreRange(
      dblMZ As Double,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double) As Boolean

        If dblMZIgnoreRangeStart > 0 OrElse dblMZIgnoreRangeEnd > 0 Then
            If dblMZ <= dblMZIgnoreRangeEnd AndAlso dblMZ >= dblMZIgnoreRangeStart Then
                ' The m/z value is between dblMZIgnoreRangeStart and dblMZIgnoreRangeEnd
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If

    End Function

    Private Function CheckScanInRange(
      intScanNumber As Integer,
      dblRetentionTime As Double,
      ByRef udtSICOptions As udtSICOptionsType) As Boolean

        ' Returns True if filtering is disabled, or if the ScanNumber is between the scan limits 
        ' and/or the retention time is between the time limits

        Dim blnInRange As Boolean

        blnInRange = True

        With udtSICOptions
            If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                If intScanNumber < .ScanRangeStart OrElse intScanNumber > .ScanRangeEnd Then
                    blnInRange = False
                End If
            End If

            If blnInRange Then
                If .RTRangeStart >= 0 AndAlso .RTRangeEnd > .RTRangeStart Then
                    If dblRetentionTime < .RTRangeStart OrElse dblRetentionTime > .RTRangeEnd Then
                        blnInRange = False
                    End If
                End If
            End If
        End With

        Return blnInRange

    End Function

    Private Function CloseOutputFileHandles(ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean

        Try
            With udtOutputFileHandles
                If Not .ScanStats Is Nothing Then
                    .ScanStats.Close()
                    .ScanStats = Nothing
                End If

                If Not .SICDataFile Is Nothing Then
                    .SICDataFile.Close()
                    .SICDataFile = Nothing
                End If

                If Not .XMLFileForSICs Is Nothing Then
                    .XMLFileForSICs.Close()
                    .XMLFileForSICs = Nothing
                End If
            End With

            Return True
        Catch ex As Exception
            LogErrors("CloseOutputFileHandles", "Error in CloseOutputFileHandles", ex, True, False)
            Return False
        End Try

    End Function

    Private Function CompareFragSpectraForParentIons(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      intParentIonIndex1 As Integer,
      intParentIonIndex2 As Integer,
      udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      udtNoiseThresholdOptions As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseOptionsType) As Single

        ' Compare the fragmentation spectra for the two parent ions
        ' Returns the highest similarity score (ranging from 0 to 1)
        ' Returns 0 if no similarity or no spectra to compare
        ' Returns -1 if an error

        Dim intFragIndex1, intFragIndex2 As Integer
        Dim intFragSpectrumIndex1, intFragSpectrumIndex2 As Integer
        Dim intPoolIndex1, intPoolIndex2 As Integer
        Dim sngSimilarityScore, sngHighestSimilarityScore As Single

        Try
            If udtScanList.ParentIons(intParentIonIndex1).CustomSICPeak OrElse udtScanList.ParentIons(intParentIonIndex2).CustomSICPeak Then
                ' Custom SIC values do not have fragmentation spectra; nothing to compare
                sngHighestSimilarityScore = 0
            ElseIf udtScanList.ParentIons(intParentIonIndex1).MRMDaughterMZ > 0 OrElse udtScanList.ParentIons(intParentIonIndex2).MRMDaughterMZ > 0 Then
                ' MRM Spectra should not be compared
                sngHighestSimilarityScore = 0
            Else
                sngHighestSimilarityScore = 0
                For intFragIndex1 = 0 To udtScanList.ParentIons(intParentIonIndex1).FragScanIndexCount - 1
                    intFragSpectrumIndex1 = udtScanList.ParentIons(intParentIonIndex1).FragScanIndices(intFragIndex1)

                    If Not objSpectraCache.ValidateSpectrumInPool(udtScanList.FragScans(intFragSpectrumIndex1).ScanNumber, intPoolIndex1) Then
                        SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                        Return -1
                    End If

                    If Not DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD Then
                        DiscardDataBelowNoiseThreshold(objSpectraCache.SpectraPool(intPoolIndex1), udtScanList.FragScans(intFragSpectrumIndex1).BaselineNoiseStats.NoiseLevel, 0, 0, udtNoiseThresholdOptions)
                    End If

                    For intFragIndex2 = 0 To udtScanList.ParentIons(intParentIonIndex2).FragScanIndexCount - 1

                        intFragSpectrumIndex2 = udtScanList.ParentIons(intParentIonIndex2).FragScanIndices(intFragIndex2)

                        If Not objSpectraCache.ValidateSpectrumInPool(udtScanList.FragScans(intFragSpectrumIndex2).ScanNumber, intPoolIndex2) Then
                            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                            Return -1
                        End If

                        If Not DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD Then
                            DiscardDataBelowNoiseThreshold(objSpectraCache.SpectraPool(intPoolIndex2), udtScanList.FragScans(intFragSpectrumIndex2).BaselineNoiseStats.NoiseLevel, 0, 0, udtNoiseThresholdOptions)
                        End If

                        sngSimilarityScore = CompareSpectra(objSpectraCache.SpectraPool(intPoolIndex1), objSpectraCache.SpectraPool(intPoolIndex2), udtBinningOptions)

                        If sngSimilarityScore > sngHighestSimilarityScore Then
                            sngHighestSimilarityScore = sngSimilarityScore
                        End If
                    Next intFragIndex2
                Next intFragIndex1

            End If
        Catch ex As Exception
            LogErrors("CompareFragSpectraForParentIons", "Error in CompareFragSpectraForParentIons", ex, True, False)
            Return -1
        End Try

        Return sngHighestSimilarityScore

    End Function

    Private Function CompareSpectra(
      ByRef udtFragSpectrum1 As clsMSSpectrum,
      ByRef udtFragSpectrum2 As clsMSSpectrum,
      udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      Optional blnConsiderOffsetBinnedData As Boolean = True) As Single

        ' Compares the two spectra and returns a similarity score (ranging from 0 to 1)
        ' Perfect match is 1; no similarity is 0
        ' Note that both the standard binned data and the offset binned data are compared
        ' If blnConsiderOffsetBinnedData = True, then the larger of the two scores is returned
        '  similarity scores is returned
        '
        ' If an error, returns -1

        Dim udtBinnedSpectrum1 = New udtBinnedDataType
        Dim udtBinnedSpectrum2 = New udtBinnedDataType

        Dim blnSuccess As Boolean

        Try

            Dim objCorrelate As New clsCorrelation
            objCorrelate.SetBinningOptions(udtBinningOptions)

            Const eCorrelationMethod = clsCorrelation.cmCorrelationMethodConstants.Pearson

            ' Bin the data in the first spectrum
            blnSuccess = CompareSpectraBinData(objCorrelate, udtFragSpectrum1, udtBinnedSpectrum1)
            If Not blnSuccess Then Return -1

            ' Bin the data in the second spectrum
            blnSuccess = CompareSpectraBinData(objCorrelate, udtFragSpectrum2, udtBinnedSpectrum2)
            If Not blnSuccess Then Return -1

            ' Now compare the binned spectra
            Dim sngSimilarity1 = objCorrelate.Correlate(udtBinnedSpectrum1.BinnedIntensities, udtBinnedSpectrum2.BinnedIntensities, eCorrelationMethod)

            If Not blnConsiderOffsetBinnedData Then
                Return sngSimilarity1
            End If

            Dim sngSimilarity2 = objCorrelate.Correlate(udtBinnedSpectrum1.BinnedIntensitiesOffset, udtBinnedSpectrum2.BinnedIntensitiesOffset, eCorrelationMethod)
            Return Math.Max(sngSimilarity1, sngSimilarity2)

        Catch ex As Exception
            LogErrors("CompareSectra", "Error in clsMasic->CompareSpectra", ex, True, False)
            Return -1
        End Try

    End Function

    Private Function CompareSpectraBinData(
      objCorrelate As clsCorrelation,
      ByRef udtFragSpectrum As clsMSSpectrum,
      ByRef udtBinnedSpectrum As udtBinnedDataType) As Boolean

        Dim intIndex As Integer
        Dim sngXData As Single()
        Dim sngYData As Single()


        ' Make a copy of the data, excluding any Reporter Ion data
        Dim filteredDataCount = 0
        ReDim sngXData(udtFragSpectrum.IonCount - 1)
        ReDim sngYData(udtFragSpectrum.IonCount - 1)

        For intIndex = 0 To udtFragSpectrum.IonCount - 1
            If Not CheckPointInMZIgnoreRange(udtFragSpectrum.IonsMZ(intIndex), mMZIntensityFilterIgnoreRangeStart, mMZIntensityFilterIgnoreRangeEnd) Then
                sngXData(filteredDataCount) = CSng(udtFragSpectrum.IonsMZ(intIndex))
                sngYData(filteredDataCount) = udtFragSpectrum.IonsIntensity(intIndex)
                filteredDataCount += 1
            End If
        Next intIndex

        If filteredDataCount > 0 Then
            ReDim Preserve sngXData(filteredDataCount - 1)
            ReDim Preserve sngYData(filteredDataCount - 1)
        Else
            ReDim Preserve sngXData(0)
            ReDim Preserve sngYData(0)
        End If

        udtBinnedSpectrum.BinnedDataStartX = objCorrelate.BinStartX
        udtBinnedSpectrum.BinSize = objCorrelate.BinSize

        udtBinnedSpectrum.BinnedIntensities = Nothing
        udtBinnedSpectrum.BinnedIntensitiesOffset = Nothing

        ' Note that the data in sngXData and sngYData should have already been filtered to discard data points below the noise threshold intensity
        Dim blnSuccess = objCorrelate.BinData(sngXData, sngYData, udtBinnedSpectrum.BinnedIntensities, udtBinnedSpectrum.BinnedIntensitiesOffset, udtBinnedSpectrum.BinCount)

        Return blnSuccess

    End Function

    Private Sub ComputeNoiseLevelForMassSpectrum(
      ByRef udtScanInfo As udtScanInfoType,
      objMSSpectrum As clsMSSpectrum,
      udtNoiseThresholdOptions As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseOptionsType)

        Const IGNORE_NON_POSITIVE_DATA = True

        MASICPeakFinder.clsMASICPeakFinder.InitializeBaselineNoiseStats(udtScanInfo.BaselineNoiseStats, 0, udtNoiseThresholdOptions.BaselineNoiseMode)

        If udtNoiseThresholdOptions.BaselineNoiseMode = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold Then
            With udtScanInfo.BaselineNoiseStats
                .NoiseLevel = udtNoiseThresholdOptions.BaselineNoiseLevelAbsolute
                .PointsUsed = 1
            End With
        Else
            If objMSSpectrum.IonCount > 0 Then
                mMASICPeakFinder.ComputeTrimmedNoiseLevel(objMSSpectrum.IonsIntensity, 0, objMSSpectrum.IonCount - 1, udtNoiseThresholdOptions, IGNORE_NON_POSITIVE_DATA, udtScanInfo.BaselineNoiseStats)
            End If
        End If

    End Sub

    Private Sub CompressSpectraData(
      objMSSpectrum As clsMSSpectrum,
      dblMSDataResolution As Double,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double)

        ' First, look for blocks of data points that consecutively have an intensity value of 0
        ' For each block of data found, reduce the data to only retain the first data point and last data point in the block
        '
        ' Next, look for data points in objMSSpectrum that are within dblMSDataResolution units of one another (m/z units)
        ' If found, combine into just one data point, keeping the largest intensity and the m/z value corresponding to the largest intensity

        Dim intIndex As Integer
        Dim intComparisonIndex As Integer
        Dim intTargetIndex As Integer

        Dim intCountCombined As Integer
        Dim dblBestMZ As Double

        Dim blnPointInIgnoreRange As Boolean

        With objMSSpectrum
            If .IonCount > 1 Then

                ' Look for blocks of data points that all have an intensity value of 0
                intTargetIndex = 0
                intIndex = 0

                Do While intIndex < .IonCount
                    If .IonsIntensity(intIndex) < Single.Epsilon Then
                        intCountCombined = 0
                        For intComparisonIndex = intIndex + 1 To .IonCount - 1
                            If .IonsIntensity(intComparisonIndex) < Single.Epsilon Then
                                intCountCombined += 1
                            Else
                                Exit For
                            End If
                        Next intComparisonIndex

                        If intCountCombined > 1 Then
                            ' Only keep the first and last data point in the block

                            .IonsMZ(intTargetIndex) = .IonsMZ(intIndex)
                            .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)

                            intTargetIndex += 1
                            .IonsMZ(intTargetIndex) = .IonsMZ(intIndex + intCountCombined)
                            .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex + intCountCombined)

                            intIndex += intCountCombined
                        Else
                            ' Keep this data point since a single zero
                            If intTargetIndex <> intIndex Then
                                .IonsMZ(intTargetIndex) = .IonsMZ(intIndex)
                                .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)
                            End If
                        End If
                    Else
                        ' Note: intTargetIndex will be the same as intIndex until the first time that data is combined (intCountCombined > 0)
                        ' After that, intTargetIndex will always be less than intIndex and we will thus always need to copy data
                        If intTargetIndex <> intIndex Then
                            .IonsMZ(intTargetIndex) = .IonsMZ(intIndex)
                            .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)
                        End If
                    End If

                    intIndex += 1
                    intTargetIndex += 1

                Loop

                ' Update .IonCount with the new data count
                .IonCount = intTargetIndex

                ' Step through the data, consolidating data within dblMSDataResolution
                ' Note that we're copying in place rather than making a new, duplicate array
                ' If the m/z value is between dblMZIgnoreRangeStart and dblMZIgnoreRangeEnd, then we will not compress the data

                intTargetIndex = 0
                intIndex = 0

                Do While intIndex < .IonCount
                    intCountCombined = 0
                    dblBestMZ = .IonsMZ(intIndex)

                    ' Only combine data if the first data point has a positive intensity value
                    If .IonsIntensity(intIndex) > 0 Then

                        blnPointInIgnoreRange = CheckPointInMZIgnoreRange(.IonsMZ(intIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                        If Not blnPointInIgnoreRange Then
                            For intComparisonIndex = intIndex + 1 To .IonCount - 1
                                If CheckPointInMZIgnoreRange(.IonsMZ(intComparisonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd) Then
                                    ' Reached the ignore range; do not allow to be combined with the current data point
                                    Exit For
                                End If

                                If .IonsMZ(intComparisonIndex) - .IonsMZ(intIndex) < dblMSDataResolution Then
                                    If .IonsIntensity(intComparisonIndex) > .IonsIntensity(intIndex) Then
                                        .IonsIntensity(intIndex) = .IonsIntensity(intComparisonIndex)
                                        dblBestMZ = .IonsMZ(intComparisonIndex)
                                    End If
                                    intCountCombined += 1
                                Else
                                    Exit For
                                End If
                            Next intComparisonIndex
                        End If

                    End If

                    ' Note: intTargetIndex will be the same as intIndex until the first time that data is combined (intCountCombined > 0)
                    ' After that, intTargetIndex will always be less than intIndex and we will thus always need to copy data
                    If intTargetIndex <> intIndex OrElse intCountCombined > 0 Then
                        .IonsMZ(intTargetIndex) = dblBestMZ
                        .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)

                        intIndex += intCountCombined
                    End If

                    intIndex += 1
                    intTargetIndex += 1
                Loop

                ' Update .IonCount with the new data count
                .IonCount = intTargetIndex
            End If
        End With

    End Sub

    Private Function ConcatenateExtendedStats(
       ByRef intNonConstantHeaderIDs() As Integer,
       intDatasetID As Integer,
       intScanNumber As Integer,
       ExtendedHeaderInfo As Dictionary(Of Integer, String),
       cColDelimiter As Char) As String

        Dim strOutLine As String
        Dim strValue As String = String.Empty

        Dim intIndex As Integer

        strOutLine = intDatasetID.ToString & cColDelimiter & intScanNumber.ToString & cColDelimiter

        If Not ExtendedHeaderInfo Is Nothing AndAlso Not intNonConstantHeaderIDs Is Nothing Then
            For intIndex = 0 To intNonConstantHeaderIDs.Length - 1
                If ExtendedHeaderInfo.TryGetValue(intNonConstantHeaderIDs(intIndex), strValue) Then
                    If IsNumber(strValue) Then
                        If Math.Abs(Val(strValue)) < Single.Epsilon Then strValue = "0"
                    Else
                        Select Case strValue
                            Case "ff" : strValue = "Off"
                            Case "n" : strValue = "On"
                            Case "eady" : strValue = "Ready"""
                            Case "cquiring" : strValue = "Acquiring"
                            Case "oad" : strValue = "Load"
                        End Select
                    End If
                    strOutLine &= strValue & cColDelimiter
                Else
                    strOutLine &= "0" & cColDelimiter
                End If
            Next intIndex

            ' remove the trailing delimiter
            If strOutLine.Length > 0 Then
                strOutLine = strOutLine.TrimEnd(cColDelimiter)
            End If

        End If

        Return strOutLine

    End Function

    Private Function ConvoluteMass(
      dblMassMZ As Double,
      intCurrentCharge As Short,
      Optional intDesiredCharge As Short = 1,
      Optional dblChargeCarrierMass As Double = 0) As Double

        ' Converts dblMassMZ to the MZ that would appear at the given intDesiredCharge
        ' To return the neutral mass, set intDesiredCharge to 0

        ' If dblChargeCarrierMass is 0, then uses CHARGE_CARRIER_MASS_MONOISO
        'Const CHARGE_CARRIER_MASS_AVG As Double = 1.00739
        'Const CHARGE_CARRIER_MASS_MONOISO As Double = 1.00727649

        Dim dblNewMZ As Double

        If Math.Abs(dblChargeCarrierMass) < Double.Epsilon Then dblChargeCarrierMass = CHARGE_CARRIER_MASS_MONOISO

        If intCurrentCharge = intDesiredCharge Then
            dblNewMZ = dblMassMZ
        Else
            If intCurrentCharge = 1 Then
                dblNewMZ = dblMassMZ
            ElseIf intCurrentCharge > 1 Then
                ' Convert dblMassMZ to M+H
                dblNewMZ = (dblMassMZ * intCurrentCharge) - dblChargeCarrierMass * (intCurrentCharge - 1)
            ElseIf intCurrentCharge = 0 Then
                ' Convert dblMassMZ (which is neutral) to M+H and store in dblNewMZ
                dblNewMZ = dblMassMZ + dblChargeCarrierMass
            Else
                ' Negative charges are not supported; return 0
                Return 0
            End If

            If intDesiredCharge > 1 Then
                dblNewMZ = (dblNewMZ + dblChargeCarrierMass * (intDesiredCharge - 1)) / intDesiredCharge
            ElseIf intDesiredCharge = 1 Then
                ' Return M+H, which is currently stored in dblNewMZ
            ElseIf intDesiredCharge = 0 Then
                ' Return the neutral mass
                dblNewMZ -= dblChargeCarrierMass
            Else
                ' Negative charges are not supported; return 0
                dblNewMZ = 0
            End If
        End If

        Return dblNewMZ

    End Function

    Private Function ConstructExtendedStatsHeaders(cColDelimiter As Char) As String

        Dim cTrimChars = New Char() {":"c, " "c}
        Dim strHeaderNames() As String

        Dim intIndex As Integer
        Dim strHeaders As String

        Dim intHeaderIDs() As Integer
        Dim intHeaderCount As Integer

        strHeaders = "Dataset" & cColDelimiter & "ScanNumber" & cColDelimiter

        ' Populate strHeaders

        If Not mExtendedHeaderInfo Is Nothing Then
            ReDim strHeaderNames(mExtendedHeaderInfo.Count - 1)
            ReDim intHeaderIDs(mExtendedHeaderInfo.Count - 1)

            intHeaderCount = 0
            For Each item In mExtendedHeaderInfo
                strHeaderNames(intHeaderCount) = item.Key
                intHeaderIDs(intHeaderCount) = item.Value
                intHeaderCount += 1
            Next

            Array.Sort(intHeaderIDs, strHeaderNames)

            For intIndex = 0 To intHeaderCount - 1
                strHeaders &= strHeaderNames(intIndex).TrimEnd(cTrimChars) & cColDelimiter
            Next intIndex

            ' remove the trailing delimiter
            If strHeaders.Length > 0 Then
                strHeaders = strHeaders.TrimEnd(cColDelimiter)
            End If
        End If

        Return strHeaders

    End Function

    Private Function ConstructOutputFilePath(
      strInputFileName As String,
      strOutputFolderPath As String,
      eFileType As eOutputFileTypeConstants,
      Optional intFragTypeNumber As Integer = 1) As String

        Dim strOutputFilePath As String

        strOutputFilePath = Path.Combine(strOutputFolderPath, Path.GetFileNameWithoutExtension(strInputFileName))
        Select Case eFileType
            Case eOutputFileTypeConstants.XMLFile
                strOutputFilePath &= "_SICs.xml"
            Case eOutputFileTypeConstants.ScanStatsFlatFile
                strOutputFilePath &= "_ScanStats.txt"
            Case eOutputFileTypeConstants.ScanStatsExtendedFlatFile
                strOutputFilePath &= "_ScanStatsEx.txt"
            Case eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile
                strOutputFilePath &= "_ScanStatsConstant.txt"
            Case eOutputFileTypeConstants.SICStatsFlatFile
                strOutputFilePath &= "_SICstats.txt"
            Case eOutputFileTypeConstants.BPIFile
                strOutputFilePath &= "_BPI.txt"
            Case eOutputFileTypeConstants.FragBPIFile
                strOutputFilePath &= "_Frag" & intFragTypeNumber.ToString & "_BPI.txt"
            Case eOutputFileTypeConstants.TICFile
                strOutputFilePath &= "_TIC.txt"
            Case eOutputFileTypeConstants.ICRToolsBPIChromatogramByScan
                strOutputFilePath &= "_BPI_Scan.tic"
            Case eOutputFileTypeConstants.ICRToolsBPIChromatogramByTime
                strOutputFilePath &= "_BPI_Time.tic"
            Case eOutputFileTypeConstants.ICRToolsTICChromatogramByScan
                strOutputFilePath &= "_TIC_Scan.tic"
            Case eOutputFileTypeConstants.ICRToolsFragTICChromatogramByScan
                strOutputFilePath &= "_TIC_MSMS_Scan.tic"
            Case eOutputFileTypeConstants.DeconToolsMSChromatogramFile
                strOutputFilePath &= "_MS_scans.csv"
            Case eOutputFileTypeConstants.DeconToolsMSMSChromatogramFile
                strOutputFilePath &= "_MSMS_scans.csv"
            Case eOutputFileTypeConstants.PEKFile
                strOutputFilePath &= ".pek"
            Case eOutputFileTypeConstants.HeaderGlossary
                strOutputFilePath = Path.Combine(strOutputFolderPath, "Header_Glossary_Readme.txt")
            Case eOutputFileTypeConstants.DeconToolsIsosFile
                strOutputFilePath &= "_isos.csv"
            Case eOutputFileTypeConstants.DeconToolsScansFile
                strOutputFilePath &= "_scans.csv"
            Case eOutputFileTypeConstants.MSMethodFile
                strOutputFilePath &= "_MSMethod"
            Case eOutputFileTypeConstants.MSTuneFile
                strOutputFilePath &= "_MSTuneSettings"
            Case eOutputFileTypeConstants.ReporterIonsFile
                strOutputFilePath &= "_ReporterIons.txt"
            Case eOutputFileTypeConstants.MRMSettingsFile
                strOutputFilePath &= "_MRMSettings.txt"
            Case eOutputFileTypeConstants.MRMDatafile
                strOutputFilePath &= "_MRMData.txt"
            Case eOutputFileTypeConstants.MRMCrosstabFile
                strOutputFilePath &= "_MRMCrosstab.txt"
            Case eOutputFileTypeConstants.DatasetInfoFile
                strOutputFilePath &= "_DatasetInfo.xml"
            Case eOutputFileTypeConstants.SICDataFile
                strOutputFilePath &= "_SICdata.txt"
            Case Else
                Debug.Assert(False, "Unknown Output File Type found in clsMASIC->ConstructOutputFilePath")
                strOutputFilePath &= "_Unknown.txt"
        End Select

        Return strOutputFilePath

    End Function

    Private Function ConstructSRMMapKey(udtSRMListEntry As udtSRMListType) As String
        Return ConstructSRMMapKey(udtSRMListEntry.ParentIonMZ, udtSRMListEntry.CentralMass)
    End Function

    Private Function ConstructSRMMapKey(dblParentIonMZ As Double, dblCentralMass As Double) As String
        Dim strMapKey As String

        strMapKey = dblParentIonMZ.ToString("0.000") & "_to_" & dblCentralMass.ToString("0.000")

        Return strMapKey
    End Function

    Private Function CreateDatasetInfoFile(strInputFileName As String, strOutputFolderPath As String) As Boolean

        Dim blnSuccess As Boolean

        Dim strDatasetName As String
        Dim strDatasetInfoFilePath As String

        Dim objDatasetStatsSummarizer As DSSummarizer.clsDatasetStatsSummarizer
        Dim udtSampleInfo = New DSSummarizer.clsDatasetStatsSummarizer.udtSampleInfoType
        udtSampleInfo.Clear()

        Try
            strDatasetName = Path.GetFileNameWithoutExtension(strInputFileName)
            strDatasetInfoFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DatasetInfoFile)

            objDatasetStatsSummarizer = New DSSummarizer.clsDatasetStatsSummarizer

            blnSuccess = objDatasetStatsSummarizer.CreateDatasetInfoFile(
              strDatasetName, strDatasetInfoFilePath,
              mScanStats, mDatasetFileInfo, udtSampleInfo)

            If Not blnSuccess Then
                LogErrors("Error calling objDatasetStatsSummarizer.CreateDatasetInfoFile", objDatasetStatsSummarizer.ErrorMessage, New Exception("Error calling objDatasetStatsSummarizer.CreateDatasetInfoFile: " & objDatasetStatsSummarizer.ErrorMessage), True, False)
            End If

        Catch ex As Exception
            LogErrors("CreateDatasetInfoFile", "Error creating dataset info file", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function CreateMZLookupList(
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtScanList As udtScanListType,
      ByRef udtMZBinList() As udtMZBinListType,
      ByRef intParentIonIndices() As Integer,
      blnProcessSIMScans As Boolean,
      intSIMIndex As Integer) As Boolean

        Dim intParentIonIndex As Integer
        Dim intMZListCount As Integer

        Dim blnIncludeParentIon As Boolean

        intMZListCount = 0
        ReDim udtMZBinList(udtScanList.ParentIonInfoCount - 1)
        ReDim intParentIonIndices(udtScanList.ParentIonInfoCount - 1)

        For intParentIonIndex = 0 To udtScanList.ParentIonInfoCount - 1

            If udtScanList.ParentIons(intParentIonIndex).MRMDaughterMZ > 0 Then
                blnIncludeParentIon = False
            Else
                If mCustomSICList.LimitSearchToCustomMZList Then
                    ' Always include CustomSICPeak entries
                    blnIncludeParentIon = udtScanList.ParentIons(intParentIonIndex).CustomSICPeak
                Else
                    ' Use blnProcessingSIMScans and .SIMScan to decide whether or not to include the entry
                    With udtScanList.SurveyScans(udtScanList.ParentIons(intParentIonIndex).SurveyScanIndex)
                        If blnProcessSIMScans Then
                            If .SIMScan Then
                                If .SIMIndex = intSIMIndex Then
                                    blnIncludeParentIon = True
                                Else
                                    blnIncludeParentIon = False
                                End If
                            Else
                                blnIncludeParentIon = False
                            End If
                        Else
                            blnIncludeParentIon = Not .SIMScan
                        End If
                    End With
                End If
            End If

            If blnIncludeParentIon Then
                udtMZBinList(intMZListCount).MZ = udtScanList.ParentIons(intParentIonIndex).MZ
                If udtScanList.ParentIons(intParentIonIndex).CustomSICPeak Then
                    udtMZBinList(intMZListCount).MZTolerance = udtScanList.ParentIons(intParentIonIndex).CustomSICPeakMZToleranceDa
                    udtMZBinList(intMZListCount).MZToleranceIsPPM = False
                Else
                    udtMZBinList(intMZListCount).MZTolerance = udtSICOptions.SICTolerance
                    udtMZBinList(intMZListCount).MZToleranceIsPPM = udtSICOptions.SICToleranceIsPPM
                End If
                intParentIonIndices(intMZListCount) = intParentIonIndex
                intMZListCount += 1
            End If
        Next intParentIonIndex

        If intMZListCount > 0 Then
            If intMZListCount < udtScanList.ParentIonInfoCount Then
                ReDim Preserve udtMZBinList(intMZListCount - 1)
                ReDim Preserve intParentIonIndices(intMZListCount - 1)
            End If

            ' Sort udtMZBinList ascending and sort intParentIonIndices in parallel
            Array.Sort(udtMZBinList, intParentIonIndices, New clsMZBinListComparer)
            Return True
        Else
            Return False
        End If

    End Function

    Private Function CreateParentIonSICs(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      udtSICOptions As udtSICOptionsType,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean

        Dim blnSuccess As Boolean
        Dim intParentIonIndex As Integer
        Dim intParentIonsProcessed As Integer

        If udtScanList.ParentIonInfoCount <= 0 Then
            ' No parent ions
            If mSuppressNoParentIonsError Then
                Return True
            Else
                SetLocalErrorCode(eMasicErrorCodes.NoParentIonsFoundInInputFile)
                Return False
            End If
        ElseIf udtScanList.SurveyScanCount <= 0 Then
            ' No survey scans
            If mSuppressNoParentIonsError Then
                Return True
            Else
                SetLocalErrorCode(eMasicErrorCodes.NoSurveyScansFoundInInputFile)
                Return False
            End If
        End If

        Try
            intParentIonsProcessed = 0
            mLastParentIonProcessingLogTime = DateTime.UtcNow

            SetSubtaskProcessingStepPct(0, "Creating SIC's for the parent ions")
            Console.Write("Creating SIC's for parent ions ")
            LogMessage("Creating SIC's for parent ions")

            ' Create an array of m/z values in udtScanList.ParentIons, then sort by m/z
            ' Next, step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance

            Dim udtMZBinList() As udtMZBinListType = Nothing

            Dim intParentIonIndices() As Integer = Nothing
            Dim intSIMIndex As Integer
            Dim intSIMIndexMax As Integer

            ' First process the non SIM, non MRM scans
            ' If this file only has MRM scans, then CreateMZLookupList will return False
            If CreateMZLookupList(udtSICOptions, udtScanList, udtMZBinList, intParentIonIndices, False, 0) Then
                blnSuccess = ProcessMZList(udtScanList, objSpectraCache, udtSICOptions, udtOutputFileHandles, udtMZBinList, intParentIonIndices, False, 0, intParentIonsProcessed)
            End If

            If blnSuccess And Not mCustomSICList.LimitSearchToCustomMZList Then
                ' Now process the SIM scans (if any)
                ' First, see if any SIMScans are present and determine the maximum SIM Index
                intSIMIndexMax = -1
                For intParentIonIndex = 0 To udtScanList.ParentIonInfoCount - 1
                    With udtScanList.SurveyScans(udtScanList.ParentIons(intParentIonIndex).SurveyScanIndex)
                        If .SIMScan Then
                            If .SIMIndex > intSIMIndexMax Then
                                intSIMIndexMax = .SIMIndex
                            End If
                        End If
                    End With
                Next intParentIonIndex

                ' Now process each SIM Scan type
                For intSIMIndex = 0 To intSIMIndexMax
                    If CreateMZLookupList(udtSICOptions, udtScanList, udtMZBinList, intParentIonIndices, True, intSIMIndex) Then
                        blnSuccess = ProcessMZList(udtScanList, objSpectraCache, udtSICOptions, udtOutputFileHandles, udtMZBinList, intParentIonIndices, True, intSIMIndex, intParentIonsProcessed)
                    End If
                Next intSIMIndex
            End If

            ' Lastly, process the MRM scans (if any)
            If udtScanList.MRMDataPresent Then
                blnSuccess = ProcessMRMList(udtScanList, objSpectraCache, udtSICOptions, udtOutputFileHandles, intParentIonsProcessed)
            End If

            Console.WriteLine()
            blnSuccess = True

        Catch ex As Exception
            LogErrors("CreateParentIonSICs", "Error creating Parent Ion SICs", ex, True, True, eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function DeepCopyHeaderInfoDictionary(sourceTable As Dictionary(Of Integer, String)) As Dictionary(Of Integer, String)
        Dim newTable = New Dictionary(Of Integer, String)

        For Each item In sourceTable
            newTable.Add(item.Key, item.Value)
        Next

        Return newTable

    End Function

    Private Function ExtractSICDetailsFromFullSIC(
      intMZIndexWork As Integer,
      ByRef udtMZSearchChunk() As udtMZSearchInfoType,
      intFullSICDataCount As Integer,
      ByRef intFullSICScanIndices(,) As Integer,
      ByRef sngFullSICIntensities(,) As Single,
      ByRef dblFullSICMasses(,) As Double,
      ByRef udtScanList As udtScanListType,
      intScanIndexObservedInFullSIC As Integer,
      ByRef udtSICDetails As udtSICStatsDetailsType,
      ByRef udtSICPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType,
      udtSICOptions As udtSICOptionsType,
      blnCustomSICPeak As Boolean,
      sngCustomSICPeakScanOrAcqTimeTolerance As Single) As Boolean

        ' Minimum number of scans to extend left or right of the scan that meets the minimum intensity threshold requirement
        Const MINIMUM_NOISE_SCANS_TO_INCLUDE = 10

        Dim sngCustomSICScanToleranceMinutesHalfWidth As Single

        ' Pointers to entries in intFullSICScanIndices() and sngFullSICIntensities()
        Dim intScanIndexStart As Integer, intScanIndexEnd As Integer

        Dim intScanIndexMax As Integer
        Dim intScanIndex As Integer

        ' The index of the first scan found to be below threshold (on the left)
        Dim intScanIndexBelowThresholdLeft As Integer

        ' The index of the first scan found to be below threshold (on the right)
        Dim intScanIndexBelowThresholdRight As Integer

        Dim sngMaximumIntensity As Single

        Dim blnLeftDone As Boolean, blnRightDone As Boolean

        Dim sngPeakWidthMinutesBackward As Single
        Dim sngPeakWidthMinutesForward As Single

        Dim sngMaxSICPeakWidthMinutesBackward As Single
        Dim sngMaxSICPeakWidthMinutesForward As Single


        ' Initialize the peak
        udtSICPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

        ' Update .BaselineNoiseStats in udtSICPeak
        udtSICPeak.BaselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(intScanIndexObservedInFullSIC, udtMZSearchChunk(intMZIndexWork).BaselineNoiseStatSegments)

        ' Initialize the values for the maximum width of the SIC peak; these might get altered for custom SIC values
        sngMaxSICPeakWidthMinutesBackward = udtSICOptions.MaxSICPeakWidthMinutesBackward
        sngMaxSICPeakWidthMinutesForward = udtSICOptions.MaxSICPeakWidthMinutesForward

        ' Limit the data examined to a portion of intFullSICScanIndices() and intFullSICIntensities, populating udtSICDetails
        Try

            ' Initialize intCustomSICScanToleranceHalfWidth
            With mCustomSICList
                If sngCustomSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    sngCustomSICPeakScanOrAcqTimeTolerance = .ScanOrAcqTimeTolerance
                End If

                If sngCustomSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    ' Use the entire SIC
                    ' Specify this by setting sngCustomSICScanToleranceMinutesHalfWidth to the maximum scan time in .MasterScanTimeList()
                    With udtScanList
                        If .MasterScanOrderCount > 0 Then
                            sngCustomSICScanToleranceMinutesHalfWidth = .MasterScanTimeList(.MasterScanOrderCount - 1)
                        Else
                            sngCustomSICScanToleranceMinutesHalfWidth = Single.MaxValue
                        End If
                    End With
                Else
                    If .ScanToleranceType = eCustomSICScanTypeConstants.Relative AndAlso sngCustomSICPeakScanOrAcqTimeTolerance > 10 Then
                        ' Relative scan time should only range from 0 to 1; we'll allow values up to 10
                        sngCustomSICPeakScanOrAcqTimeTolerance = 10
                    End If

                    sngCustomSICScanToleranceMinutesHalfWidth = ScanOrAcqTimeToScanTime(udtScanList, sngCustomSICPeakScanOrAcqTimeTolerance / 2, .ScanToleranceType, True)
                End If

                If blnCustomSICPeak Then
                    If sngMaxSICPeakWidthMinutesBackward < sngCustomSICScanToleranceMinutesHalfWidth Then
                        sngMaxSICPeakWidthMinutesBackward = sngCustomSICScanToleranceMinutesHalfWidth
                    End If

                    If sngMaxSICPeakWidthMinutesForward < sngCustomSICScanToleranceMinutesHalfWidth Then
                        sngMaxSICPeakWidthMinutesForward = sngCustomSICScanToleranceMinutesHalfWidth
                    End If
                End If
            End With

            ' Initially use just 3 survey scans, centered around intScanIndexObservedInFullSIC
            If intScanIndexObservedInFullSIC > 0 Then
                intScanIndexStart = intScanIndexObservedInFullSIC - 1
                intScanIndexEnd = intScanIndexObservedInFullSIC + 1
            Else
                intScanIndexStart = 0
                intScanIndexEnd = 1
                intScanIndexObservedInFullSIC = 0
            End If

            If intScanIndexEnd >= intFullSICDataCount Then intScanIndexEnd = intFullSICDataCount - 1

        Catch ex As Exception
            LogErrors("ExtractSICDetailsFromFullSIC", "Error initializing SIC start/end indices", ex, True, True, eMasicErrorCodes.CreateSICsError)
        End Try

        If intScanIndexEnd >= intScanIndexStart Then

            Try
                ' Start by using the 3 survey scans centered around intScanIndexObservedInFullSIC
                sngMaximumIntensity = -1
                intScanIndexMax = -1
                For intScanIndex = intScanIndexStart To intScanIndexEnd
                    If sngFullSICIntensities(intMZIndexWork, intScanIndex) > sngMaximumIntensity Then
                        sngMaximumIntensity = sngFullSICIntensities(intMZIndexWork, intScanIndex)
                        intScanIndexMax = intScanIndex
                    End If
                Next intScanIndex
            Catch ex As Exception
                LogErrors("ExtractSICDetailsFromFullSIC", "Error while creating initial SIC", ex, True, True, eMasicErrorCodes.CreateSICsError)
            End Try

            ' Now extend the SIC, stepping left and right until a threshold is reached
            blnLeftDone = False
            blnRightDone = False
            intScanIndexBelowThresholdLeft = -1
            intScanIndexBelowThresholdRight = -1


            Do While (intScanIndexStart > 0 AndAlso Not blnLeftDone) OrElse (intScanIndexEnd < intFullSICDataCount - 1 AndAlso Not blnRightDone)
                Try
                    ' Extend the SIC to the left until the threshold is reached
                    If intScanIndexStart > 0 AndAlso Not blnLeftDone Then
                        If sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < udtSICPeak.BaselineNoiseStats.NoiseLevel Then
                            If intScanIndexBelowThresholdLeft < 0 Then
                                intScanIndexBelowThresholdLeft = intScanIndexStart
                            Else
                                If intScanIndexStart <= intScanIndexBelowThresholdLeft - MINIMUM_NOISE_SCANS_TO_INCLUDE Then
                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    ' Stop creating the SIC to the left
                                    blnLeftDone = True
                                End If
                            End If
                        Else
                            intScanIndexBelowThresholdLeft = -1
                        End If

                        sngPeakWidthMinutesBackward = udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime -
                           udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexStart)).ScanTime

                        If blnLeftDone Then
                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the left of intScanIndexObservedInFullSIC and to the left of intScanIndexMax
                            If intScanIndexObservedInFullSIC - intScanIndexStart < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnLeftDone = False
                            If intScanIndexMax - intScanIndexStart < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnLeftDone = False

                            ' For custom SIC values, make sure the scan range has been satisfied
                            If blnLeftDone AndAlso blnCustomSICPeak Then
                                If sngPeakWidthMinutesBackward < sngCustomSICScanToleranceMinutesHalfWidth Then
                                    blnLeftDone = False
                                End If
                            End If
                        End If

                        If Not blnLeftDone Then
                            If intScanIndexStart = 0 Then
                                blnLeftDone = True
                            Else
                                intScanIndexStart -= 1
                                If sngFullSICIntensities(intMZIndexWork, intScanIndexStart) > sngMaximumIntensity Then
                                    sngMaximumIntensity = sngFullSICIntensities(intMZIndexWork, intScanIndexStart)
                                    intScanIndexMax = intScanIndexStart
                                End If
                            End If
                        End If

                        sngPeakWidthMinutesBackward = udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime -
                           udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexStart)).ScanTime

                        If sngPeakWidthMinutesBackward >= sngMaxSICPeakWidthMinutesBackward Then
                            blnLeftDone = True
                        End If

                    End If

                Catch ex As Exception
                    LogErrors("ExtractSICDetailsFromFullSIC", "Error extending SIC to the left", ex, True, True, eMasicErrorCodes.CreateSICsError)
                End Try

                Try
                    ' Extend the SIC to the right until the threshold is reached
                    If intScanIndexEnd < intFullSICDataCount - 1 AndAlso Not blnRightDone Then
                        If sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < udtSICPeak.BaselineNoiseStats.NoiseLevel Then
                            If intScanIndexBelowThresholdRight < 0 Then
                                intScanIndexBelowThresholdRight = intScanIndexEnd
                            Else
                                If intScanIndexEnd >= intScanIndexBelowThresholdRight + MINIMUM_NOISE_SCANS_TO_INCLUDE Then
                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    ' Stop creating the SIC to the right
                                    blnRightDone = True
                                End If
                            End If
                        Else
                            intScanIndexBelowThresholdRight = -1
                        End If

                        sngPeakWidthMinutesForward = udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexEnd)).ScanTime -
                          udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime

                        If blnRightDone Then
                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the right of intScanIndexObservedInFullSIC and to the Rigth of intScanIndexMax
                            If intScanIndexEnd - intScanIndexObservedInFullSIC < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnRightDone = False
                            If intScanIndexEnd - intScanIndexMax < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnRightDone = False

                            ' For custom SIC values, make sure the scan range has been satisfied
                            If blnRightDone AndAlso blnCustomSICPeak Then
                                If sngPeakWidthMinutesForward < sngCustomSICScanToleranceMinutesHalfWidth Then
                                    blnRightDone = False
                                End If
                            End If
                        End If

                        If Not blnRightDone Then
                            If intScanIndexEnd = intFullSICDataCount - 1 Then
                                blnRightDone = True
                            Else
                                intScanIndexEnd += 1
                                If sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) > sngMaximumIntensity Then
                                    sngMaximumIntensity = sngFullSICIntensities(intMZIndexWork, intScanIndexEnd)
                                    intScanIndexMax = intScanIndexEnd
                                End If
                            End If
                        End If

                        sngPeakWidthMinutesForward = udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexEnd)).ScanTime -
                          udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime

                        If sngPeakWidthMinutesForward >= sngMaxSICPeakWidthMinutesForward Then
                            blnRightDone = True
                        End If
                    End If

                Catch ex As Exception
                    LogErrors("ExtractSICDetailsFromFullSIC", "Error extending SIC to the right", ex, True, True, eMasicErrorCodes.CreateSICsError)
                End Try

            Loop    ' While Not LeftDone and Not RightDone

        End If

        ' Populate udtSICDetails with the data between intScanIndexStart and intScanIndexEnd
        If intScanIndexStart < 0 Then intScanIndexStart = 0
        If intScanIndexEnd >= intFullSICDataCount Then intScanIndexEnd = intFullSICDataCount - 1

        If intScanIndexEnd < intScanIndexStart Then
            LogErrors("ExtractSICDetailsFromFullSIC", "Programming error: intScanIndexEnd < intScanIndexStart", Nothing, True, True, eMasicErrorCodes.FindSICPeaksError)
            intScanIndexEnd = intScanIndexStart
        End If

        Try

            ' Copy the scan index values from intFullSICScanIndices to .SICScanIndices()
            ' Copy the intensity values from sngFullSICIntensities() to .SICData()
            ' Copy the mz values from dblFullSICMasses() to .SICMasses()

            With udtSICDetails
                .SICDataCount = intScanIndexEnd - intScanIndexStart + 1
                .SICScanType = eScanTypeConstants.SurveyScan

                If .SICDataCount > .SICScanIndices.Length Then
                    ReDim .SICScanIndices(udtSICDetails.SICDataCount - 1)
                    ReDim .SICScanNumbers(udtSICDetails.SICDataCount - 1)
                    ReDim .SICData(udtSICDetails.SICDataCount - 1)
                    ReDim .SICMasses(udtSICDetails.SICDataCount - 1)
                End If

                udtSICPeak.IndexObserved = 0
                .SICDataCount = 0
                For intScanIndex = intScanIndexStart To intScanIndexEnd
                    If intFullSICScanIndices(intMZIndexWork, intScanIndex) >= 0 Then
                        .SICScanIndices(.SICDataCount) = intFullSICScanIndices(intMZIndexWork, intScanIndex)
                        .SICScanNumbers(.SICDataCount) = udtScanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndex)).ScanNumber
                        .SICData(.SICDataCount) = sngFullSICIntensities(intMZIndexWork, intScanIndex)
                        .SICMasses(.SICDataCount) = dblFullSICMasses(intMZIndexWork, intScanIndex)

                        If intScanIndex = intScanIndexObservedInFullSIC Then
                            udtSICPeak.IndexObserved = .SICDataCount
                        End If
                        .SICDataCount += 1
                    Else
                        ' This shouldn't happen
                    End If
                Next intScanIndex
            End With

        Catch ex As Exception
            LogErrors("ExtractSICDetailsFromFullSIC", "Error populating .SICScanIndices, .SICData, and .SICMasses", ex, True, True, eMasicErrorCodes.CreateSICsError)
        End Try

        Return True

    End Function

    ''Private Function CreateParentIonSICsWork(ByRef udtScanList As udtScanListType, objSpectraCache As clsSpectraCache, intParentIonIndex As Integer, ByRef udtSICDetails As udtSICStatsDetailsType, udtSICOptions As udtSICOptionsType) As Boolean

    ''    Const MINIMUM_NOISE_SCANS_TO_INCLUDE As Integer = 5             ' Minimum number of scans to extend left or right of the scan that meets the minimum intensity threshold requirement

    ''    Dim intSurveyScanIndex As Integer

    ''    Dim dblSearchMZ As Double
    ''    Dim intScanIndexStart As Integer, intScanIndexEnd As Integer    ' Pointers to entries in .SurveyScans()
    ''    Dim intNewScanIndex As Integer

    ''    Dim intScanIndexObserved As Integer                             ' Survey scan index that the parent ion was first observed in
    ''    Dim intScanIndexMax As Integer                                  ' Survey scan index containing the maximum for this parent ion

    ''    Dim intScanIndexBelowThresholdLeft As Integer                   ' The index of the first scan found to be below threshold (on the left)
    ''    Dim intScanIndexBelowThresholdRight As Integer                  ' The index of the first scan found to be below threshold (on the right)
    ''    Dim blnLeftDone As Boolean, blnRightDone As Boolean

    ''    Dim intSpectrumProcessCount As Integer

    ''    Dim intIonMatchCount As Integer
    ''    Dim sngIonSum As Single
    ''    Dim sngMaximumIntensity As Single
    ''    Dim dblClosestMZ As Single

    ''    Dim intSICScanIndices() As Integer
    ''    Dim sngSICIntensities() As Single
    ''    Dim sngSICMasses() As Single

    ''    Dim intCustomSICScanToleranceHalfWidth As Integer

    ''    Dim blnCustomSICPeak As Boolean
    ''    Dim blnNoSurveyScan As Boolean
    ''    Dim blnUseScan As Boolean

    ''    Dim blnSIMScan As Boolean
    ''    Dim intSIMIndex As Integer
    ''    Dim intPoolIndex As Integer

    ''    Try

    ''        ' Initialize intCustomSICScanToleranceHalfWidth
    ''        With mCustomSICList
    ''            If .ScanTolerance <= 0 Then
    ''                ' Create the SIC over the entire run
    ''                intCustomSICScanToleranceHalfWidth = ScanOrAcqTimeToAbsolute(udtScanList, 1, eCustomSICScanTypeConstants.Relative, True)
    ''            Else
    ''                intCustomSICScanToleranceHalfWidth = ScanOrAcqTimeToAbsolute(udtScanList, .ScanTolerance / 2, .ScanToleranceType, True)
    ''            End If
    ''        End With

    ''        ' Initially create a SIC using just 3 survey scans, centered around .SurveyScanIndex
    ''        With udtScanList.ParentIons(intParentIonIndex)
    ''            dblSearchMZ = .MZ
    ''            blnSIMScan = udtScanList.SurveyScans(.SurveyScanIndex).SIMScan
    ''            intSIMIndex = udtScanList.SurveyScans(.SurveyScanIndex).SIMIndex

    ''            If .SurveyScanIndex > 0 Then
    ''                intScanIndexStart = GetPreviousSurveyScanIndex(udtScanList.SurveyScans, .SurveyScanIndex)
    ''                intScanIndexEnd = GetNextSurveyScanIndex(udtScanList.SurveyScans, .SurveyScanIndex)
    ''                intScanIndexObserved = .SurveyScanIndex
    ''            Else
    ''                intScanIndexStart = 0
    ''                intScanIndexEnd = GetNextSurveyScanIndex(udtScanList.SurveyScans, 0)
    ''                intScanIndexObserved = 0
    ''            End If

    ''            blnCustomSICPeak = .CustomSICPeak
    ''        End With

    ''        If intScanIndexEnd >= udtScanList.SurveyScanCount Then intScanIndexEnd = udtScanList.SurveyScanCount - 1
    ''    Catch ex As Exception
    ''        LogErrors("CreateParentIonSICsWork", "Error initializing SIC start/end indices", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''    End Try

    ''    If intScanIndexEnd >= intScanIndexStart AndAlso dblSearchMZ > 0 Then

    ''        Try
    ''            ' Reserve room in intSICScanIndices for at most .SurveyScanCount items
    ''            ReDim intSICScanIndices(udtScanList.SurveyScanCount - 1)
    ''            ReDim sngSICIntensities(udtScanList.SurveyScanCount - 1)
    ''            ReDim sngSICMasses(udtScanList.SurveyScanCount - 1)

    ''            ' Initialize the entries in intSICScanIndices to -1; when we actually use an entry, we'll store the actual scan index
    ''            For intSurveyScanIndex = 0 To udtScanList.SurveyScanCount - 1
    ''                intSICScanIndices(intSurveyScanIndex) = -1
    ''            Next intSurveyScanIndex

    ''        Catch ex As Exception
    ''            LogErrors("CreateParentIonSICsWork", "Error reserving room in intSICScanIndices", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''        End Try


    ''        Try
    ''            ' Actually create the SIC using the 3 survey scans centered around intScanIndexObserved
    ''            sngMaximumIntensity = -1
    ''            intScanIndexMax = -1
    ''            For intSurveyScanIndex = intScanIndexStart To intScanIndexEnd
    ''                If blnSIMScan Then
    ''                    If udtScanList.SurveyScans(intSurveyScanIndex).SIMScan AndAlso
    ''                       udtScanList.SurveyScans(intSurveyScanIndex).SIMIndex = intSIMIndex Then
    ''                        blnUseScan = True
    ''                    Else
    ''                        blnUseScan = False
    ''                    End If
    ''                Else
    ''                    blnUseScan = True
    ''                End If

    ''                If blnUseScan Then
    ''                    If Not objSpectraCache.ValidateSpectrumInPool(udtScanList.SurveyScans(intSurveyScanIndex).ScanNumber, intPoolIndex) Then
    ''                        SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
    ''                        Return False
    ''                    End If

    ''                    sngIonSum = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex), dblSearchMZ, udtSICOptions.SICToleranceDa, intIonMatchCount, dblClosestMZ, False)

    ''                    intSICScanIndices(intSurveyScanIndex) = intSurveyScanIndex
    ''                    sngSICIntensities(intSurveyScanIndex) = sngIonSum
    ''                    sngSICMasses(intSurveyScanIndex) = dblClosestMZ
    ''                    If sngIonSum > sngMaximumIntensity Then
    ''                        sngMaximumIntensity = sngIonSum
    ''                        intScanIndexMax = intSurveyScanIndex
    ''                    End If

    ''                End If
    ''            Next intSurveyScanIndex
    ''        Catch ex As Exception
    ''            LogErrors("CreateParentIonSICsWork", "Error while creating initial SIC", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''        End Try

    ''        ' Now extend the SIC, stepping left and right until a threshold is reached
    ''        blnLeftDone = False
    ''        blnRightDone = False
    ''        intScanIndexBelowThresholdLeft = -1
    ''        intScanIndexBelowThresholdRight = -1
    ''        intSpectrumProcessCount = 3

    ''        With udtScanList
    ''            Do While (intScanIndexStart > 0 AndAlso Not blnLeftDone) OrElse (intScanIndexEnd < .SurveyScanCount - 1 AndAlso Not blnRightDone)
    ''                Try
    ''                    ' Extend the SIC to the left until the threshold is reached
    ''                    If intScanIndexStart > 0 AndAlso Not blnLeftDone Then
    ''                        If sngSICIntensities(intScanIndexStart) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
    ''                           sngSICIntensities(intScanIndexStart) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity Then
    ''                            If intScanIndexBelowThresholdLeft < 0 Then
    ''                                intScanIndexBelowThresholdLeft = intScanIndexStart
    ''                            Else
    ''                                If intScanIndexStart <= intScanIndexBelowThresholdLeft - MINIMUM_NOISE_SCANS_TO_INCLUDE Then
    ''                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
    ''                                    ' Stop creating the SIC to the left
    ''                                    blnLeftDone = True
    ''                                End If
    ''                            End If
    ''                        Else
    ''                            intScanIndexBelowThresholdLeft = -1
    ''                        End If

    ''                        If blnLeftDone Then
    ''                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the left of intScanIndexObserved and to the left of intScanIndexMax
    ''                            If intScanIndexObserved - intScanIndexStart < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnLeftDone = False
    ''                            If intScanIndexMax - intScanIndexStart < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnLeftDone = False

    ''                            ' For custom SIC values, make sure the scan range has been satisfied
    ''                            If blnLeftDone AndAlso blnCustomSICPeak Then
    ''                                If intScanIndexObserved - intScanIndexStart < intCustomSICScanToleranceHalfWidth Then blnLeftDone = False
    ''                            End If
    ''                        End If

    ''                        If Not blnLeftDone Then
    ''                            intNewScanIndex = GetPreviousSurveyScanIndex(.SurveyScans, intScanIndexStart)
    ''                            If intNewScanIndex = intScanIndexStart Then
    ''                                blnLeftDone = True
    ''                            Else
    ''                                intScanIndexStart = intNewScanIndex

    ''                                intSpectrumProcessCount += 1

    ''                                If Not objSpectraCache.ValidateSpectrumInPool(udtScanList.SurveyScans(intScanIndexStart).ScanNumber, intPoolIndex) Then
    ''                                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
    ''                                    Return False
    ''                                End If

    ''                                sngIonSum = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex), dblSearchMZ, udtSICOptions.SICToleranceDa, intIonMatchCount, dblClosestMZ, False)
    ''                                intSICScanIndices(intScanIndexStart) = intScanIndexStart
    ''                                sngSICIntensities(intScanIndexStart) = sngIonSum
    ''                                sngSICMasses(intScanIndexStart) = dblClosestMZ
    ''                                If sngIonSum > sngMaximumIntensity Then
    ''                                    sngMaximumIntensity = sngIonSum
    ''                                    intScanIndexMax = intScanIndexStart
    ''                                End If
    ''                            End If
    ''                        End If

    ''                        If .SurveyScans(intScanIndexObserved).ScanTime - .SurveyScans(intScanIndexStart).ScanTime >= udtSICOptions.MaxSICPeakWidthMinutesBackward Then
    ''                            blnLeftDone = True
    ''                            If blnCustomSICPeak AndAlso intScanIndexObserved - intScanIndexStart < intCustomSICScanToleranceHalfWidth Then blnLeftDone = False
    ''                        End If

    ''                    End If

    ''                Catch ex As Exception
    ''                    LogErrors("CreateParentIonSICsWork", "Error extending SIC to the left", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''                End Try

    ''                Try
    ''                    ' Extend the SIC to the right until the threshold is reached
    ''                    If intScanIndexEnd < .SurveyScanCount - 1 AndAlso Not blnRightDone Then
    ''                        If sngSICIntensities(intScanIndexEnd) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
    ''                           sngSICIntensities(intScanIndexEnd) < udtSICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity Then
    ''                            If intScanIndexBelowThresholdRight < 0 Then
    ''                                intScanIndexBelowThresholdRight = intScanIndexEnd
    ''                            Else
    ''                                If intScanIndexEnd >= intScanIndexBelowThresholdRight + MINIMUM_NOISE_SCANS_TO_INCLUDE Then
    ''                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
    ''                                    ' Stop creating the SIC to the right
    ''                                    blnRightDone = True
    ''                                End If
    ''                            End If
    ''                        Else
    ''                            intScanIndexBelowThresholdRight = -1
    ''                        End If

    ''                        If blnRightDone Then
    ''                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the Rigth of intScanIndexObserved and to the Rigth of intScanIndexMax
    ''                            If intScanIndexEnd - intScanIndexObserved < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnRightDone = False
    ''                            If intScanIndexEnd - intScanIndexMax < udtSICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnRightDone = False

    ''                            ' For custom SIC values, make sure the scan range has been satisfied
    ''                            If blnRightDone AndAlso blnCustomSICPeak Then
    ''                                If intScanIndexEnd - intScanIndexObserved < intCustomSICScanToleranceHalfWidth Then blnRightDone = False
    ''                            End If
    ''                        End If

    ''                        If Not blnRightDone Then
    ''                            intNewScanIndex = GetNextSurveyScanIndex(.SurveyScans, intScanIndexEnd)
    ''                            If intNewScanIndex = intScanIndexEnd Then
    ''                                blnRightDone = True
    ''                            Else
    ''                                intScanIndexEnd = intNewScanIndex

    ''                                intSpectrumProcessCount += 1

    ''                                If Not objSpectraCache.ValidateSpectrumInPool(udtScanList.SurveyScans(intScanIndexEnd).ScanNumber, intPoolIndex) Then
    ''                                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
    ''                                    Return False
    ''                                End If

    ''                                sngIonSum = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex), dblSearchMZ, udtSICOptions.SICToleranceDa, intIonMatchCount, dblClosestMZ, False)
    ''                                intSICScanIndices(intScanIndexEnd) = intScanIndexEnd
    ''                                sngSICIntensities(intScanIndexEnd) = sngIonSum
    ''                                sngSICMasses(intScanIndexEnd) = dblClosestMZ
    ''                                If sngIonSum > sngMaximumIntensity Then
    ''                                    sngMaximumIntensity = sngIonSum
    ''                                    intScanIndexMax = intScanIndexEnd
    ''                                End If
    ''                            End If

    ''                        End If

    ''                        If .SurveyScans(intScanIndexEnd).ScanTime - .SurveyScans(intScanIndexObserved).ScanTime >= udtSICOptions.MaxSICPeakWidthMinutesForward Then
    ''                            blnRightDone = True
    ''                            If blnCustomSICPeak AndAlso intScanIndexEnd - intScanIndexObserved < intCustomSICScanToleranceHalfWidth Then blnRightDone = False
    ''                        End If

    ''                    End If

    ''                Catch ex As Exception
    ''                    LogErrors("CreateParentIonSICsWork", "Error extending SIC to the right", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''                End Try

    ''            Loop
    ''        End With

    ''        If intScanIndexStart < 0 Then intScanIndexStart = 0
    ''        If intScanIndexEnd >= udtScanList.SurveyScanCount Then intScanIndexEnd = udtScanList.SurveyScanCount - 1

    ''        Try
    ''            ' Copy the scan index values from intSICScanIndices to .SICScanIndices()
    ''            ' Copy the intensity values from sngSICIntensities() to .SICData()
    ''            ' Copy the mz values from sngSICMasses() to .SICMasses()

    ''            ' .SICDataCount will get revised below if blnSIMScan = True
    ''            With udtSICDetails
    ''                .SICDataCount = intScanIndexEnd - intScanIndexStart + 1
    ''                .SICScanType = eScanTypeConstants.SurveyScan

    ''                If .SICDataCount > .SICScanIndices.Length Then
    ''                    ReDim .SICScanIndices(udtSICDetails.SICDataCount - 1)
    ''                    ReDim .SICData(udtSICDetails.SICDataCount - 1)
    ''                    ReDim .SICMasses(udtSICDetails.SICDataCount - 1)
    ''                End If

    ''                .SICDataCount = 0
    ''                For intSurveyScanIndex = intScanIndexStart To intScanIndexEnd
    ''                    If intSICScanIndices(intSurveyScanIndex) >= 0 Then
    ''                        .SICScanIndices(.SICDataCount) = intSICScanIndices(intSurveyScanIndex)
    ''                        .SICData(.SICDataCount) = sngSICIntensities(intSurveyScanIndex)
    ''                        .SICMasses(.SICDataCount) = sngSICMasses(intSurveyScanIndex)

    ''                        .SICDataCount += 1
    ''                    End If
    ''                Next intSurveyScanIndex

    ''            End With


    ''        Catch ex As Exception
    ''            LogErrors("CreateParentIonSICsWork", "Error populating .SICScanIndices, .SICData, and .SICMasses", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''        End Try

    ''    Else
    ''        If dblSearchMZ <> 0 Then
    ''            Debug.Assert(False, "This shouldn't happen (clsMasic->CreateParentIonSICsWork)")
    ''        End If

    ''        Try
    ''            udtSICDetails.SICDataCount = 0
    ''        Catch ex As Exception
    ''            LogErrors("CreateParentIonSICsWork", "Error resizing the udtSICDetails arrays to length -1", ex, True, True, True, eMasicErrorCodes.CreateSICsError)
    ''        End Try
    ''    End If

    ''    Return True

    ''End Function

    Private Function DetermineMRMSettings(
      udtScanList As udtScanListType,
      ByRef udtMRMSettings() As udtMRMScanInfoType,
      ByRef udtSRMList() As udtSRMListType) As Boolean

        ' Returns true if this dataset has MRM data and if it is parsed successfully
        ' Returns false if the dataset does not have MRM data, or if an error occurs

        Dim intMRMSettingsCount As Integer
        Dim intSRMListCount As Integer

        Dim strMRMInfoHash As String

        Dim intScanIndex As Integer
        Dim intMRMInfoIndex As Integer
        Dim intMRMMassIndex As Integer
        Dim intSRMIndex As Integer

        Dim blnMRMDataPresent As Boolean
        Dim blnMatchFound As Boolean
        Dim blnSuccess As Boolean

        Try
            blnMRMDataPresent = False

            SetSubtaskProcessingStepPct(0, "Determining MRM settings")

            ' Initialize the tracking arrays
            intMRMSettingsCount = 0
            ReDim udtMRMSettings(9)

            Dim mrmHashToIndexMap = New Dictionary(Of String, Integer)

            intSRMListCount = 0
            ReDim udtSRMList(9)

            ' Construct a list of the MRM search values used
            For intScanIndex = 0 To udtScanList.FragScanCount - 1
                If udtScanList.FragScans(intScanIndex).MRMScanType = MRMScanTypeConstants.SRM Then
                    blnMRMDataPresent = True

                    With udtScanList.FragScans(intScanIndex)
                        ' See if this MRM spec is already in udtMRMScanInfoType

                        strMRMInfoHash = GenerateMRMInfoHash(.MRMScanInfo)

                        If Not mrmHashToIndexMap.TryGetValue(strMRMInfoHash, intMRMInfoIndex) Then

                            If intMRMSettingsCount >= udtMRMSettings.Length Then
                                ReDim Preserve udtMRMSettings(udtMRMSettings.Length * 2 - 1)
                            End If

                            DuplicateMRMInfo(.MRMScanInfo, udtMRMSettings(intMRMSettingsCount))
                            udtMRMSettings(intMRMSettingsCount).ScanCount = 1
                            udtMRMSettings(intMRMSettingsCount).ParentIonInfoIndex = .FragScanInfo.ParentIonInfoIndex

                            mrmHashToIndexMap.Add(strMRMInfoHash, intMRMSettingsCount)

                            intMRMSettingsCount += 1

                            ' Append the new entries to udtSRMList
                            intMRMInfoIndex = intMRMSettingsCount - 1

                            For intMRMMassIndex = 0 To udtMRMSettings(intMRMInfoIndex).MRMMassCount - 1
                                ' Add this new transition to udtSRMList() only if not already present
                                blnMatchFound = False
                                For intSRMIndex = 0 To intSRMListCount - 1
                                    If MRMParentDaughterMatch(udtSRMList(intSRMIndex), udtMRMSettings(intMRMInfoIndex), intMRMMassIndex) Then
                                        blnMatchFound = True
                                        Exit For
                                    End If
                                Next intSRMIndex

                                If Not blnMatchFound Then
                                    ' Entry is not yet present; add it
                                    If intSRMListCount >= udtSRMList.Length Then
                                        ReDim Preserve udtSRMList(udtSRMList.Length * 2 - 1)
                                    End If

                                    udtSRMList(intSRMListCount).ParentIonMZ = udtMRMSettings(intMRMInfoIndex).ParentIonMZ
                                    udtSRMList(intSRMListCount).CentralMass = udtMRMSettings(intMRMInfoIndex).MRMMassList(intMRMMassIndex).CentralMass

                                    intSRMListCount += 1
                                End If
                            Next intMRMMassIndex
                        Else
                            udtMRMSettings(intMRMInfoIndex).ScanCount += 1
                        End If

                    End With
                End If
            Next intScanIndex

            If blnMRMDataPresent Then
                ' Shrink the MRM arrays to match the number of items in each
                ReDim Preserve udtMRMSettings(intMRMSettingsCount - 1)
                ReDim Preserve udtSRMList(intSRMListCount - 1)

                blnSuccess = True
            Else
                ReDim udtMRMSettings(0)
                ReDim udtSRMList(0)
                blnSuccess = False
            End If

        Catch ex As Exception
            LogErrors("DetermineMRMSettings", "Error determining the MRM settings", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Sub DiscardDataBelowNoiseThreshold(
      objMSSpectrum As clsMSSpectrum,
      sngNoiseThresholdIntensity As Single,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double,
      udtNoiseThresholdOptions As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseOptionsType)

        Dim intIonCountNew As Integer
        Dim intIonIndex As Integer
        Dim blnPointPassesFilter As Boolean

        Try
            With objMSSpectrum
                Select Case udtNoiseThresholdOptions.BaselineNoiseMode
                    Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold
                        If udtNoiseThresholdOptions.BaselineNoiseLevelAbsolute > 0 Then
                            intIonCountNew = 0
                            For intIonIndex = 0 To .IonCount - 1

                                blnPointPassesFilter = CheckPointInMZIgnoreRange(.IonsMZ(intIonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                                If Not blnPointPassesFilter Then
                                    ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                    If .IonsIntensity(intIonIndex) >= udtNoiseThresholdOptions.BaselineNoiseLevelAbsolute Then
                                        blnPointPassesFilter = True
                                    End If
                                End If

                                If blnPointPassesFilter Then
                                    .IonsMZ(intIonCountNew) = .IonsMZ(intIonIndex)
                                    .IonsIntensity(intIonCountNew) = .IonsIntensity(intIonIndex)
                                    intIonCountNew += 1
                                End If

                            Next intIonIndex
                        Else
                            intIonCountNew = .IonCount
                        End If
                    Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance,
                      MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount,
                      MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                        If udtNoiseThresholdOptions.MinimumSignalToNoiseRatio > 0 Then
                            intIonCountNew = 0
                            For intIonIndex = 0 To .IonCount - 1

                                blnPointPassesFilter = CheckPointInMZIgnoreRange(.IonsMZ(intIonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                                If Not blnPointPassesFilter Then
                                    ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                    If MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(.IonsIntensity(intIonIndex), sngNoiseThresholdIntensity) >= udtNoiseThresholdOptions.MinimumSignalToNoiseRatio Then
                                        blnPointPassesFilter = True
                                    End If
                                End If

                                If blnPointPassesFilter Then
                                    .IonsMZ(intIonCountNew) = .IonsMZ(intIonIndex)
                                    .IonsIntensity(intIonCountNew) = .IonsIntensity(intIonIndex)
                                    intIonCountNew += 1
                                End If

                            Next intIonIndex
                        Else
                            intIonCountNew = .IonCount
                        End If
                    Case Else
                        Debug.Assert(False, "Unknown BaselineNoiseMode encountered in DiscardDataBelowNoiseThreshold: " & udtNoiseThresholdOptions.BaselineNoiseMode.ToString)
                End Select

                If intIonCountNew < .IonCount Then
                    .IonCount = intIonCountNew
                End If
            End With
        Catch ex As Exception
            LogErrors("DiscardDataBelowNoiseThreshold", "Error discarding data below the noise threshold", ex, True, True, eMasicErrorCodes.UnspecifiedError)
        End Try

    End Sub

    Private Sub DiscardDataToLimitIonCount(
      objMSSpectrum As clsMSSpectrum,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double,
      intMaxIonCountToRetain As Integer)

        Dim intIonCountNew As Integer
        Dim intIonIndex As Integer
        Dim blnPointPassesFilter As Boolean

        Dim objFilterDataArray As clsFilterDataArrayMaxCount

        ' When this is true, then will write a text file of the mass spectrum before before and after it is filtered
        ' Used for debugging
        Dim blnWriteDebugData As Boolean
        Dim srOutFile As StreamWriter = Nothing

        Try

            With objMSSpectrum

                If objMSSpectrum.IonCount > intMaxIonCountToRetain Then
                    objFilterDataArray = New clsFilterDataArrayMaxCount

                    objFilterDataArray.MaximumDataCountToLoad = intMaxIonCountToRetain
                    objFilterDataArray.TotalIntensityPercentageFilterEnabled = False

                    blnWriteDebugData = False
                    If blnWriteDebugData Then
                        srOutFile = New StreamWriter(New FileStream(Path.Combine(mOutputFolderPath, "DataDump_" & objMSSpectrum.ScanNumber.ToString & "_BeforeFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                        srOutFile.WriteLine("m/z" & ControlChars.Tab & "Intensity")
                    End If

                    ' Store the intensity values in objFilterDataArray
                    For intIonIndex = 0 To .IonCount - 1
                        objFilterDataArray.AddDataPoint(.IonsIntensity(intIonIndex), intIonIndex)
                        If blnWriteDebugData Then
                            srOutFile.WriteLine(.IonsMZ(intIonIndex) & ControlChars.Tab & .IonsIntensity(intIonIndex))
                        End If
                    Next

                    If blnWriteDebugData Then
                        srOutFile.Close()
                    End If


                    ' Call .FilterData, which will determine which data points to keep
                    objFilterDataArray.FilterData()

                    intIonCountNew = 0
                    For intIonIndex = 0 To .IonCount - 1

                        blnPointPassesFilter = CheckPointInMZIgnoreRange(.IonsMZ(intIonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                        If Not blnPointPassesFilter Then
                            ' See if the point's intensity is negative
                            If objFilterDataArray.GetAbundanceByIndex(intIonIndex) >= 0 Then
                                blnPointPassesFilter = True
                            End If
                        End If

                        If blnPointPassesFilter Then
                            .IonsMZ(intIonCountNew) = .IonsMZ(intIonIndex)
                            .IonsIntensity(intIonCountNew) = .IonsIntensity(intIonIndex)
                            intIonCountNew += 1
                        End If

                    Next intIonIndex
                Else
                    intIonCountNew = .IonCount
                End If

                If intIonCountNew < .IonCount Then
                    .IonCount = intIonCountNew
                End If

                If blnWriteDebugData Then
                    srOutFile = New StreamWriter(New FileStream(Path.Combine(mOutputFolderPath, "DataDump_" & objMSSpectrum.ScanNumber.ToString & "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                    srOutFile.WriteLine("m/z" & ControlChars.Tab & "Intensity")

                    ' Store the intensity values in objFilterDataArray
                    For intIonIndex = 0 To .IonCount - 1
                        srOutFile.WriteLine(.IonsMZ(intIonIndex) & ControlChars.Tab & .IonsIntensity(intIonIndex))
                    Next
                    srOutFile.Close()
                End If

            End With
        Catch ex As Exception
            LogErrors("DiscardDataToLimitIonCount", "Error limiting the number of data points to " & intMaxIonCountToRetain.ToString, ex, True, True, eMasicErrorCodes.UnspecifiedError)
        End Try

    End Sub

    Private Sub DuplicateMRMInfo(
      ByRef udtSource As MRMInfo,
      dblParentIonMZ As Double,
      ByRef udtTarget As udtMRMScanInfoType)

        With udtSource
            udtTarget.ParentIonMZ = dblParentIonMZ
            udtTarget.MRMMassCount = .MRMMassList.Count

            If .MRMMassList Is Nothing Then
                udtTarget.MRMMassList = New List(Of udtMRMMassRangeType)()
            Else
                udtTarget.MRMMassList = New List(Of udtMRMMassRangeType)(.MRMMassList.Count)
                udtTarget.MRMMassList.AddRange(.MRMMassList)
            End If

            udtTarget.ScanCount = 0
            udtTarget.ParentIonInfoIndex = -1
        End With
    End Sub

    Private Sub DuplicateMRMInfo(ByRef udtSource As udtMRMScanInfoType, ByRef udtTarget As udtMRMScanInfoType)
        With udtSource
            udtTarget.ParentIonMZ = .ParentIonMZ
            udtTarget.MRMMassCount = .MRMMassCount

            If .MRMMassList Is Nothing Then
                udtTarget.MRMMassList = New List(Of udtMRMMassRangeType)()
            Else
                udtTarget.MRMMassList = New List(Of udtMRMMassRangeType)(.MRMMassList.Count)
                udtTarget.MRMMassList.AddRange(.MRMMassList)
            End If

            udtTarget.ScanCount = udtSource.ScanCount
            udtTarget.ParentIonInfoIndex = udtSource.ParentIonInfoIndex
        End With
    End Sub

    Private Function ExportMRMDataToDisk(
      udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtMRMSettings() As udtMRMScanInfoType,
      ByRef udtSRMList() As udtSRMListType,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean

        ' Returns true if the MRM data is successfully written to disk
        ' Note that it will also return true if udtMRMSettings() is empty

        Const cColDelimiter As Char = ControlChars.Tab

        Dim srDataOutfile As StreamWriter = Nothing
        Dim srCrosstabOutfile As StreamWriter = Nothing

        Dim intScanFirst As Integer
        Dim sngScanTimeFirst As Single
        Dim sngCrosstabColumnValue() As Single
        Dim blnCrosstabColumnFlag() As Boolean

        Dim strMRMSettingsFilePath As String
        Dim strDataFilePath As String
        Dim strCrosstabFilePath As String

        Dim strCrosstabHeaders As String = String.Empty
        Dim strLineStart As String
        Dim strOutLine As String
        Dim strSRMMapKey As String

        Dim intScanIndex As Integer
        Dim intMRMInfoIndex As Integer
        Dim intMRMMassIndex As Integer
        Dim intSRMIndex As Integer
        Dim intSRMIndexLast As Integer

        Dim dblMZStart As Double
        Dim dblMZEnd As Double
        Dim dblMRMToleranceHalfWidth As Double

        Dim dblClosestMZ As Double
        Dim sngMatchIntensity As Single

        Dim blnMatchFound As Boolean
        Dim blnSuccess As Boolean

        Try
            ' Only write this data if 1 or more fragmentation spectra are of type SRM
            If udtMRMSettings Is Nothing OrElse udtMRMSettings.Length = 0 Then
                blnSuccess = True
                Exit Try
            End If

            SetSubtaskProcessingStepPct(0, "Exporting MRM data")

            ' Write out the MRM Settings
            strMRMSettingsFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MRMSettingsFile)
            Using srSettingsOutFile = New StreamWriter(strMRMSettingsFilePath)

                srSettingsOutFile.WriteLine(GetHeadersForOutputFile(udtScanList, eOutputFileTypeConstants.MRMSettingsFile))

                For intMRMInfoIndex = 0 To udtMRMSettings.Length - 1
                    With udtMRMSettings(intMRMInfoIndex)
                        For intMRMMassIndex = 0 To .MRMMassCount - 1
                            strOutLine = intMRMInfoIndex & cColDelimiter &
                             .ParentIonMZ.ToString("0.000") & cColDelimiter &
                             .MRMMassList(intMRMMassIndex).CentralMass & cColDelimiter &
                             .MRMMassList(intMRMMassIndex).StartMass & cColDelimiter &
                             .MRMMassList(intMRMMassIndex).EndMass & cColDelimiter &
                             .ScanCount.ToString

                            srSettingsOutFile.WriteLine(strOutLine)
                        Next

                    End With
                Next intMRMInfoIndex


                If Me.WriteMRMDataList Or Me.WriteMRMIntensityCrosstab Then

                    ' Populate srmKeyToIndexMap
                    Dim srmKeyToIndexMap = New Dictionary(Of String, Integer)
                    For intSRMIndex = 0 To udtSRMList.Length - 1
                        srmKeyToIndexMap.Add(ConstructSRMMapKey(udtSRMList(intSRMIndex)), intSRMIndex)
                    Next intSRMIndex

                    If Me.WriteMRMDataList Then
                        ' Write out the raw MRM Data
                        strDataFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MRMDatafile)
                        srDataOutfile = New StreamWriter(strDataFilePath)

                        ' Write the file headers
                        srDataOutfile.WriteLine(GetHeadersForOutputFile(udtScanList, eOutputFileTypeConstants.MRMDatafile))
                    End If


                    If Me.WriteMRMIntensityCrosstab Then
                        ' Write out the raw MRM Data
                        strCrosstabFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MRMCrosstabFile)
                        srCrosstabOutfile = New StreamWriter(strCrosstabFilePath)

                        ' Initialize the crosstab header variable using the data in udtSRMList()
                        strCrosstabHeaders = "Scan_First" & cColDelimiter & "ScanTime"

                        For intSRMIndex = 0 To udtSRMList.Length - 1
                            strCrosstabHeaders &= cColDelimiter & ConstructSRMMapKey(udtSRMList(intSRMIndex))
                        Next intSRMIndex

                        srCrosstabOutfile.WriteLine(strCrosstabHeaders)
                    End If

                    intScanFirst = Integer.MinValue
                    intSRMIndexLast = 0
                    ReDim sngCrosstabColumnValue(udtSRMList.Length - 1)
                    ReDim blnCrosstabColumnFlag(udtSRMList.Length - 1)

                    For intScanIndex = 0 To udtScanList.FragScanCount - 1
                        If udtScanList.FragScans(intScanIndex).MRMScanType = MRMScanTypeConstants.SRM Then
                            With udtScanList.FragScans(intScanIndex)

                                If intScanFirst = Integer.MinValue Then
                                    intScanFirst = .ScanNumber
                                    sngScanTimeFirst = .ScanTime
                                End If

                                strLineStart = .ScanNumber & cColDelimiter &
                                   .MRMScanInfo.ParentIonMZ.ToString("0.000") & cColDelimiter

                                ' Look for each of the m/z values specified in .MRMScanInfo.MRMMassList
                                For intMRMMassIndex = 0 To .MRMScanInfo.MRMMassCount - 1
                                    ' Find the maximum value between .StartMass and .EndMass
                                    ' Need to define a tolerance to account for numeric rounding artifacts in the variables

                                    dblMZStart = .MRMScanInfo.MRMMassList(intMRMMassIndex).StartMass
                                    dblMZEnd = .MRMScanInfo.MRMMassList(intMRMMassIndex).EndMass
                                    dblMRMToleranceHalfWidth = Math.Round((dblMZEnd - dblMZStart) / 2, 6)
                                    If dblMRMToleranceHalfWidth < 0.001 Then
                                        dblMRMToleranceHalfWidth = 0.001
                                    End If

                                    blnMatchFound = FindMaxValueInMZRange(objSpectraCache, udtScanList.FragScans, intScanIndex, dblMZStart - dblMRMToleranceHalfWidth, dblMZEnd + dblMRMToleranceHalfWidth, dblClosestMZ, sngMatchIntensity)

                                    If Me.WriteMRMDataList Then
                                        If blnMatchFound Then
                                            strOutLine = strLineStart & .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass.ToString("0.000") &
                                             cColDelimiter & sngMatchIntensity.ToString("0.000")

                                        Else
                                            strOutLine = strLineStart & .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass.ToString("0.000") &
                                             cColDelimiter & "0"
                                        End If

                                        srDataOutfile.WriteLine(strOutLine)
                                    End If


                                    If Me.WriteMRMIntensityCrosstab Then
                                        strSRMMapKey = ConstructSRMMapKey(.MRMScanInfo.ParentIonMZ, .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass)

                                        ' Use srmKeyToIndexMap to determine the appropriate column index for strSRMMapKey
                                        If srmKeyToIndexMap.TryGetValue(strSRMMapKey, intSRMIndex) Then

                                            If blnCrosstabColumnFlag(intSRMIndex) OrElse
                                               (intSRMIndex = 0 And intSRMIndexLast = udtSRMList.Length - 1) Then
                                                ' Either the column is already populated, or the SRMIndex has cycled back to zero; write out the current crosstab line and reset the crosstab column arrays
                                                ExportMRMDataWriteLine(srCrosstabOutfile, intScanFirst, sngScanTimeFirst, sngCrosstabColumnValue, blnCrosstabColumnFlag, cColDelimiter, True)

                                                intScanFirst = .ScanNumber
                                                sngScanTimeFirst = .ScanTime
                                            End If

                                            If blnMatchFound Then
                                                sngCrosstabColumnValue(intSRMIndex) = sngMatchIntensity
                                            End If
                                            blnCrosstabColumnFlag(intSRMIndex) = True
                                            intSRMIndexLast = intSRMIndex
                                        Else
                                            ' Unknown combination of parent ion m/z and daughter m/z; this is unexpected
                                            ' We won't write this entry out
                                            intSRMIndexLast = intSRMIndexLast
                                        End If
                                    End If

                                Next intMRMMassIndex

                            End With

                        End If

                        UpdateOverallProgress(objSpectraCache)
                        If mAbortProcessing Then
                            Exit For
                        End If
                    Next intScanIndex

                    If Me.WriteMRMIntensityCrosstab Then
                        ' Write out any remaining crosstab values
                        ExportMRMDataWriteLine(srCrosstabOutfile, intScanFirst, sngScanTimeFirst, sngCrosstabColumnValue, blnCrosstabColumnFlag, cColDelimiter, False)
                    End If

                End If

            End Using

            blnSuccess = True

        Catch ex As Exception
            LogErrors("ExportMRMDataToDisk", "Error writing the SRM data to disk", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        Finally
            If Not srDataOutfile Is Nothing Then
                srDataOutfile.Close()
            End If

            If Not srCrosstabOutfile Is Nothing Then
                srCrosstabOutfile.Close()
            End If
        End Try

        Return blnSuccess

    End Function

    Private Sub ExportMRMDataWriteLine(
      srCrosstabOutfile As StreamWriter,
      intScanFirst As Integer,
      sngScanTimeFirst As Single,
      sngCrosstabColumnValue() As Single,
      blnCrosstabColumnFlag() As Boolean,
      cColDelimiter As Char,
      blnForceWrite As Boolean)

        ' If blnForceWrite = False, then will only write out the line if 1 or more columns is non-zero

        Dim intIndex As Integer
        Dim intNonZeroCount As Integer

        Dim strOutLine As String

        intNonZeroCount = 0
        strOutLine = intScanFirst.ToString & cColDelimiter &
         Math.Round(sngScanTimeFirst, 5).ToString

        ' Construct a tab-delimited list of the values
        ' At the same time, clear the arrays
        For intIndex = 0 To sngCrosstabColumnValue.Length - 1
            If sngCrosstabColumnValue(intIndex) > 0 Then
                strOutLine &= cColDelimiter & sngCrosstabColumnValue(intIndex).ToString("0.000")
                intNonZeroCount += 1
            Else
                strOutLine &= cColDelimiter & "0"
            End If

            sngCrosstabColumnValue(intIndex) = 0
            blnCrosstabColumnFlag(intIndex) = False
        Next intIndex

        If intNonZeroCount > 0 OrElse blnForceWrite Then
            srCrosstabOutfile.WriteLine(strOutLine)
        End If

    End Sub

    Private Function ExportRawDataToDisk(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean

        Dim strOutputFilePath = "??"
        Dim strOutputFilePath2 As String

        Dim srDataOutfile As StreamWriter
        Dim srScanInfoOutFile As StreamWriter = Nothing

        Dim intMasterOrderIndex As Integer
        Dim intScanPointer As Integer

        Dim udtRawDataExportOptions As udtRawDataExportOptionsType
        Dim intSpectrumExportCount As Integer

        Dim blnSuccess As Boolean

        Try

            udtRawDataExportOptions = mRawDataExportOptions

            Select Case udtRawDataExportOptions.FileFormat
                Case eExportRawDataFileFormatConstants.PEKFile
                    strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.PEKFile)
                    srDataOutfile = New StreamWriter(strOutputFilePath)
                Case eExportRawDataFileFormatConstants.CSVFile
                    strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsIsosFile)
                    strOutputFilePath2 = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsScansFile)

                    srDataOutfile = New StreamWriter(strOutputFilePath)
                    srScanInfoOutFile = New StreamWriter(strOutputFilePath2)

                    ' Write the file headers
                    WriteDecon2LSIsosFileHeaders(srDataOutfile)
                    WriteDecon2LSScanFileHeaders(srScanInfoOutFile)

                Case Else
                    ' Unknown format
                    mStatusMessage = "Unknown raw data file format: " & udtRawDataExportOptions.FileFormat.ToString
                    ShowErrorMessage(mStatusMessage)

                    If MyBase.ShowMessages Then
                        Windows.Forms.MessageBox.Show(mStatusMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Else
                        Throw New Exception(mStatusMessage)
                    End If
                    blnSuccess = False
                    Exit Try
            End Select

            intSpectrumExportCount = 0

            If Not udtRawDataExportOptions.IncludeMSMS AndAlso udtRawDataExportOptions.RenumberScans Then
                udtRawDataExportOptions.RenumberScans = True
            Else
                udtRawDataExportOptions.RenumberScans = False
            End If

            SetSubtaskProcessingStepPct(0, "Exporting raw data")

            For intMasterOrderIndex = 0 To udtScanList.MasterScanOrderCount - 1
                intScanPointer = udtScanList.MasterScanOrder(intMasterOrderIndex).ScanIndexPointer
                If udtScanList.MasterScanOrder(intMasterOrderIndex).ScanType = eScanTypeConstants.SurveyScan Then
                    SaveRawDatatoDiskWork(srDataOutfile, srScanInfoOutFile, udtScanList.SurveyScans(intScanPointer), objSpectraCache, strInputFileName, False, intSpectrumExportCount, udtRawDataExportOptions)
                Else
                    If udtRawDataExportOptions.IncludeMSMS OrElse
                      Not udtScanList.FragScans(intScanPointer).MRMScanType = MRMScanTypeConstants.NotMRM Then
                        ' Either we're writing out MS/MS data or this is an MRM scan
                        SaveRawDatatoDiskWork(srDataOutfile, srScanInfoOutFile, udtScanList.FragScans(intScanPointer), objSpectraCache, strInputFileName, True, intSpectrumExportCount, udtRawDataExportOptions)
                    End If
                End If

                If udtScanList.MasterScanOrderCount > 1 Then
                    SetSubtaskProcessingStepPct(CShort(intMasterOrderIndex / (udtScanList.MasterScanOrderCount - 1) * 100))
                Else
                    SetSubtaskProcessingStepPct(0)
                End If

                UpdateOverallProgress(objSpectraCache)
                If mAbortProcessing Then
                    Exit For
                End If
            Next intMasterOrderIndex

            If Not srDataOutfile Is Nothing Then srDataOutfile.Close()
            If Not srScanInfoOutFile Is Nothing Then srScanInfoOutFile.Close()

            blnSuccess = True

        Catch ex As Exception
            LogErrors("ExportRawDataToDisk", "Error writing the raw spectra data to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

    Private Function ExtractConstantExtendedHeaderValues(
      ByRef intNonConstantHeaderIDs() As Integer,
      ByRef udtSurveyScans() As udtScanInfoType,
      ByRef udtFragScans() As udtScanInfoType,
      cColDelimiter As Char) As String

        ' Looks through udtSurveyScans and udtFragScans for ExtendedHeaderInfo values that are constant across all scans
        ' Returns a string containing the header values that are constant, tab delimited, and their constant values, also tab delimited
        ' intNonConstantHeaderIDs() returns the ID values of the header values that are not constant
        ' mExtendedHeaderInfo is updated so that constant header values are removed from it

        Dim cTrimChars = New Char() {":"c, " "c}

        Dim intNonConstantCount As Integer

        Dim strValue As String = String.Empty

        Dim htConsolidatedValues As Dictionary(Of Integer, String)

        Dim intConstantHeaderIDs() As Integer

        Dim intScanFilterTextHeaderID As Integer

        ' Initialize this for now; it will get re-dimmed below if any constant values are found
        ReDim intNonConstantHeaderIDs(mExtendedHeaderInfo.Count - 1)
        For intIndex = 0 To mExtendedHeaderInfo.Count - 1
            intNonConstantHeaderIDs(intIndex) = intIndex
        Next intIndex

        If Not mConsolidateConstantExtendedHeaderValues Then
            ' Do not try to consolidate anything
            Return String.Empty
        End If

        If udtSurveyScans.Length > 0 Then
            htConsolidatedValues = DeepCopyHeaderInfoDictionary(udtSurveyScans(0).ExtendedHeaderInfo)
        ElseIf udtFragScans.Length > 0 Then
            htConsolidatedValues = DeepCopyHeaderInfoDictionary(udtFragScans(0).ExtendedHeaderInfo)
        Else
            Return String.Empty
        End If

        If htConsolidatedValues Is Nothing Then
            Return String.Empty
        End If

        ' Look for "Scan Filter Text" in mExtendedHeaderInfo
        If TryGetExtendedHeaderInfoValue(EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, intScanFilterTextHeaderID) Then
            ' Match found

            ' Now look for and remove the HeaderID value from htConsolidatedValues to prevent the scan filter text from being included in the consolidated values file
            If htConsolidatedValues.ContainsKey(intScanFilterTextHeaderID) Then
                htConsolidatedValues.Remove(intScanFilterTextHeaderID)
            End If
        End If

        ' Examine the values in .ExtendedHeaderInfo() in the survey scans and compare them
        ' to the values in htConsolidatedValues, looking to see if they match
        For surveyScanIndex = 0 To udtSurveyScans.Length - 1
            If Not udtSurveyScans(surveyScanIndex).ExtendedHeaderInfo Is Nothing Then
                For Each dataItem In udtSurveyScans(surveyScanIndex).ExtendedHeaderInfo
                    If htConsolidatedValues.TryGetValue(dataItem.Key, strValue) Then
                        If String.Equals(strValue, dataItem.Value) Then
                            ' Value matches; nothing to do
                        Else
                            ' Value differs; remove key from htConsolidatedValues
                            htConsolidatedValues.Remove(dataItem.Key)
                        End If
                    End If
                Next
            End If
        Next surveyScanIndex

        ' Examine the values in .ExtendedHeaderInfo() in the frag scans and compare them
        ' to the values in htConsolidatedValues, looking to see if they match
        For fragScanIndex = 0 To udtFragScans.Length - 1
            If Not udtFragScans(fragScanIndex).ExtendedHeaderInfo Is Nothing Then
                For Each item In udtFragScans(fragScanIndex).ExtendedHeaderInfo
                    If htConsolidatedValues.TryGetValue(item.Key, strValue) Then
                        If String.Equals(strValue, item.Value) Then
                            ' Value matches; nothing to do
                        Else
                            ' Value differs; remove key from htConsolidatedValues
                            htConsolidatedValues.Remove(item.Key)
                        End If
                    End If
                Next
            End If
        Next fragScanIndex

        If htConsolidatedValues Is Nothing OrElse htConsolidatedValues.Count = 0 Then
            Return String.Empty
        End If


        ' Populate strConsolidatedValues with the values in htConsolidatedValues, 
        '  separating each header and value with a tab and separating each pair of values with a NewLine character
        ' Need to first populate intConstantHeaderIDs with the ID values and sort the list so that the values are
        '  stored in strConsolidatedValueList in the correct order

        Dim strConsolidatedValueList = "Setting" & cColDelimiter & "Value" & ControlChars.NewLine

        ReDim intConstantHeaderIDs(htConsolidatedValues.Count - 1)

        Dim targetIndex = 0
        For Each item In htConsolidatedValues
            intConstantHeaderIDs(targetIndex) = item.Key
            targetIndex += 1
        Next

        Array.Sort(intConstantHeaderIDs)

        Dim htKeysToRemove = New List(Of String)

        For headerIndex = 0 To intConstantHeaderIDs.Length - 1

            For Each item In mExtendedHeaderInfo
                If item.Value = intConstantHeaderIDs(headerIndex) Then
                    strConsolidatedValueList &= item.Key.TrimEnd(cTrimChars) & cColDelimiter
                    strConsolidatedValueList &= htConsolidatedValues(intConstantHeaderIDs(headerIndex)) & ControlChars.NewLine
                    htKeysToRemove.Add(item.Key)
                    Exit For
                End If
            Next
        Next

        ' Remove the elements from mExtendedHeaderInfo that were included in strConsolidatedValueList;
        '  we couldn't remove these above since that would invalidate the iHeaderEnum enumerator

        For Each keyName In htKeysToRemove
            For headerIndex = 0 To mExtendedHeaderInfo.Count - 1
                If mExtendedHeaderInfo(headerIndex).Key = keyName Then
                    mExtendedHeaderInfo.RemoveAt(headerIndex)
                    Exit For
                End If
            Next
        Next

        ReDim intNonConstantHeaderIDs(mExtendedHeaderInfo.Count - 1)
        intNonConstantCount = 0

        ' Populate intNonConstantHeaderIDs with the ID values in mExtendedHeaderInfo
        For Each item In mExtendedHeaderInfo
            intNonConstantHeaderIDs(intNonConstantCount) = item.Value
            intNonConstantCount += 1
        Next

        Array.Sort(intNonConstantHeaderIDs)

        Return strConsolidatedValueList

    End Function

    Private Function ExtractScanInfoFromMGFandCDF(
      strFilePath As String,
      intDatasetID As Integer,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      ByRef strStatusMessage As String,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean) As Boolean

        ' Returns True if Success, False if failure
        ' Note: This function assumes strFilePath exists
        '
        ' This function can be used to read a pair of MGF and NetCDF files that contain MS/MS and MS-only parent ion scans, respectively
        ' Typically, this will apply to LC-MS/MS analyses acquired using an Agilent mass spectrometer running DataAnalysis software
        ' strFilePath can contain the path to the MGF or to the CDF file; the extension will be removed in order to determine the base file name,
        '  then the two files will be looked for separately

        Dim ioFileInfo As FileInfo
        Dim strMGFInputFilePathFull As String
        Dim strCDFInputFilePathFull As String

        Dim intMsScanCount As Integer
        Dim intMsScanIndex As Integer
        Dim intLastSurveyScanIndex As Integer
        Dim intScanNumberCorrection As Integer
        Dim intSurveyScanIndex As Integer

        ' The following variables apply to MS-only data
        Dim intScanNumber As Integer
        Dim intIonIndex As Integer
        Dim intFragScanIteration As Integer

        Dim eScanType As eScanTypeConstants
        Dim intScanIndex As Integer
        Dim udtCurrentScan As udtScanInfoType

        Dim dblScanTotalIntensity, dblScanTime, dblMassMin, dblMassMax As Double

        Dim sngMZ() As Single = Nothing
        Dim objMSSpectrum As New clsMSSpectrum

        Dim dblMZMin As Double, dblMZMax As Double
        Dim dblMSDataResolution As Double

        ' The following variables apply to MS/MS data
        Dim objSpectrumInfo As MSDataFileReader.clsSpectrumInfo = Nothing

        Dim objCDFReader As New NetCDFReader.clsMSNetCdf
        Dim objMGFReader As New MSDataFileReader.clsMGFFileReader

        Dim blnFragScanFound As Boolean
        Dim blnValidFragScan As Boolean
        Dim blnSuccess As Boolean

        Try
            Console.Write("Reading CDF/MGF data files ")
            LogMessage("Reading CDF/MGF data files")

            SetSubtaskProcessingStepPct(0, "Opening data file: " & ControlChars.NewLine & Path.GetFileName(strFilePath))

            ' Obtain the full path to the file
            ioFileInfo = New FileInfo(strFilePath)
            strMGFInputFilePathFull = ioFileInfo.FullName

            ' Make sure the extension for strMGFInputFilePathFull is .MGF
            strMGFInputFilePathFull = Path.ChangeExtension(strMGFInputFilePathFull, AGILENT_MSMS_FILE_EXTENSION)
            strCDFInputFilePathFull = Path.ChangeExtension(strMGFInputFilePathFull, AGILENT_MS_FILE_EXTENSION)

            blnSuccess = UpdateDatasetFileStats(mDatasetFileInfo, ioFileInfo, intDatasetID)
            mDatasetFileInfo.ScanCount = 0

            ' Open a handle to each data file
            If Not objCDFReader.OpenMSCdfFile(strCDFInputFilePathFull) Then
                strStatusMessage = "Error opening input data file: " & strCDFInputFilePathFull
                ShowErrorMessage(strStatusMessage)
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            If Not objMGFReader.OpenFile(strMGFInputFilePathFull) Then
                strStatusMessage = "Error opening input data file: " & strMGFInputFilePathFull
                ShowErrorMessage(strStatusMessage)
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            intMsScanCount = objCDFReader.GetScanCount()
            mDatasetFileInfo.ScanCount = intMsScanCount

            If intMsScanCount <= 0 Then
                ' No scans found
                strStatusMessage = "No scans found in the input file: " & strCDFInputFilePathFull
                ShowErrorMessage(strStatusMessage)
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            Else

                ' Reserve memory for all of the the Survey Scan data
                InitializeScanList(udtScanList, intMsScanCount, 0)
                intLastSurveyScanIndex = -1

                UpdateOverallProgress("Reading CDF/MGF data (" & intMsScanCount.ToString & " scans)" & ControlChars.NewLine & Path.GetFileName(strFilePath))
                LogMessage("Reading CDF/MGF data; Total MS scan count: " & intMsScanCount.ToString)

                ' Read all of the Survey scans from the CDF file
                ' CDF files created by the Agilent XCT list the first scan number as 0; use intScanNumberCorrection to correct for this
                intScanNumberCorrection = 0
                For intMsScanIndex = 0 To intMsScanCount - 1
                    blnSuccess = objCDFReader.GetScanInfo(intMsScanIndex, intScanNumber, dblScanTotalIntensity, dblScanTime, dblMassMin, dblMassMax)

                    If intMsScanIndex = 0 AndAlso intScanNumber = 0 Then
                        intScanNumberCorrection = 1
                    End If

                    If blnSuccess Then
                        If intScanNumberCorrection > 0 Then intScanNumber += intScanNumberCorrection

                        If CheckScanInRange(intScanNumber, dblScanTime, udtSICOptions) Then

                            With udtScanList
                                If .SurveyScanCount + 1 >= .SurveyScans.Length Then
                                    ' This shouldn't really ever be reached, so we'll redim exactly to .SurveyScanCount
                                    ReDim Preserve .SurveyScans(.SurveyScanCount)
                                End If

                                With .SurveyScans(.SurveyScanCount)
                                    .ScanNumber = intScanNumber
                                    If mCDFTimeInSeconds Then
                                        .ScanTime = CSng(dblScanTime / 60)
                                    Else
                                        .ScanTime = CSng(dblScanTime)
                                    End If

                                    ' Copy the Total Scan Intensity to .TotalIonIntensity
                                    .TotalIonIntensity = CSng(dblScanTotalIntensity)

                                    .FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here

                                    .ScanHeaderText = String.Empty
                                    .ScanTypeName = "MS"
                                End With

                                With objMSSpectrum
                                    blnSuccess = objCDFReader.GetMassSpectrum(intMsScanIndex, sngMZ, .IonsIntensity, .IonCount)
                                End With

                                If blnSuccess AndAlso objMSSpectrum.IonCount > 0 Then
                                    objMSSpectrum.ScanNumber = .SurveyScans(.SurveyScanCount).ScanNumber

                                    With objMSSpectrum
                                        ReDim .IonsMZ(.IonCount - 1)
                                        sngMZ.CopyTo(.IonsMZ, 0)

                                        If .IonsMZ.GetLength(0) < .IonCount Then
                                            ' Error with objCDFReader
                                            Debug.Assert(False, "objCDFReader returned an array of data that was shorter than expected")
                                            .IonCount = .IonsMZ.GetLength(0)
                                        End If

                                        If .IonsIntensity.GetLength(0) < .IonCount Then
                                            ' Error with objCDFReader
                                            Debug.Assert(False, "objCDFReader returned an array of data that was shorter than expected")
                                            .IonCount = .IonsIntensity.GetLength(0)
                                        End If

                                    End With

                                    With .SurveyScans(.SurveyScanCount)
                                        .IonCount = objMSSpectrum.IonCount
                                        .IonCountRaw = .IonCount

                                        ' Find the base peak ion mass and intensity
                                        .BasePeakIonMZ = FindBasePeakIon(objMSSpectrum.IonsMZ, objMSSpectrum.IonsIntensity, objMSSpectrum.IonCount, .BasePeakIonIntensity, dblMZMin, dblMZMax)

                                        ' Determine the minimum positive intensity in this scan
                                        .MinimumPositiveIntensity = mMASICPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)
                                    End With

                                    If udtSICOptions.SICToleranceIsPPM Then
                                        ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by COMPRESS_TOLERANCE_DIVISOR
                                        ' However, if the lowest m/z value is < 100, then use 100 m/z
                                        If dblMZMin < 100 Then
                                            dblMSDataResolution = GetParentIonToleranceDa(udtSICOptions, 100) / udtSICOptions.CompressToleranceDivisorForPPM
                                        Else
                                            dblMSDataResolution = GetParentIonToleranceDa(udtSICOptions, dblMZMin) / udtSICOptions.CompressToleranceDivisorForPPM
                                        End If
                                    Else
                                        dblMSDataResolution = udtSICOptions.SICTolerance / udtSICOptions.CompressToleranceDivisorForDa
                                    End If

                                    ProcessAndStoreSpectrum(.SurveyScans(.SurveyScanCount), objSpectraCache, objMSSpectrum, udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD, udtSICOptions.CompressMSSpectraData, dblMSDataResolution, blnKeepRawSpectra)

                                Else
                                    With .SurveyScans(.SurveyScanCount)
                                        .IonCount = 0
                                        .IonCountRaw = 0
                                    End With
                                End If

                                ' Note: Since we're reading all of the Survey Scan data, we cannot update .MasterScanOrder() at this time
                                .SurveyScanCount += 1

                            End With
                        End If

                    Else
                        ' Error reading CDF file
                        strStatusMessage = "Error obtaining data from CDF file: " & strCDFInputFilePathFull
                        SetLocalErrorCode(eMasicErrorCodes.InputFileDataReadError)
                        Return False
                    End If

                    ' Note: We need to take intMsScanCount * 2 since we have to read two different files
                    If intMsScanCount > 1 Then
                        SetSubtaskProcessingStepPct(CShort(intMsScanIndex / (intMsScanCount * 2 - 1) * 100))
                    Else
                        SetSubtaskProcessingStepPct(0)
                    End If

                    UpdateOverallProgress(objSpectraCache)
                    If mAbortProcessing Then
                        udtScanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If intMsScanIndex Mod 100 = 0 Then
                        LogMessage("Reading MS scan index: " & intMsScanIndex.ToString)
                        Console.Write(".")
                    End If

                Next intMsScanIndex

                ' Record the current memory usage (before we close the .CDF file)
                mProcessingStats.MemoryUsageMBDuringLoad = GetProcessMemoryUsageMB()

                objCDFReader.CloseMSCdfFile()

                ' We loaded all of the survey scan data above
                ' We can now initialize .MasterScanOrder()
                intLastSurveyScanIndex = 0
                udtScanList.MasterScanOrderCount = 0
                AddMasterScanEntry(udtScanList, eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)

                Dim surveyScansRecorded = New SortedSet(Of Integer)
                surveyScansRecorded.Add(intLastSurveyScanIndex)

                ' Reset intScanNumberCorrection; we might also apply it to MS/MS data
                intScanNumberCorrection = 0

                ' Now read the MS/MS data from the MGF file
                Do
                    blnFragScanFound = objMGFReader.ReadNextSpectrum(objSpectrumInfo)
                    If blnFragScanFound Then

                        mDatasetFileInfo.ScanCount += 1

                        If objSpectrumInfo.ScanNumber < udtScanList.SurveyScans(intLastSurveyScanIndex).ScanNumber Then
                            ' The scan number for the current MS/MS spectrum is less than the last survey scan index scan number
                            ' This can happen, due to oddities with combining scans when creating the .MGF file
                            ' Need to decrement intLastSurveyScanIndex until we find the appropriate survey scan
                            Do
                                intLastSurveyScanIndex -= 1
                                If intLastSurveyScanIndex = 0 Then Exit Do
                            Loop While objSpectrumInfo.ScanNumber < udtScanList.SurveyScans(intLastSurveyScanIndex).ScanNumber

                        End If

                        If intScanNumberCorrection = 0 Then
                            ' See if udtSpectrumHeaderInfo.ScanNumberStart is equivalent to one of the survey scan numbers, yielding conflicting scan numbers
                            ' If it is, then there is an indexing error in the .MGF file; this error was present in .MGF files generated with
                            '  an older version of Agilent Chemstation.  These files typically have lines like ###MSMS: #13-29 instead of ###MSMS: #13/29/
                            ' If this indexing error is found, then we'll set intScanNumberCorrection = 1 and apply it to all subsequent MS/MS scans;
                            '  we'll also need to correct prior MS/MS scans
                            For intSurveyScanIndex = intLastSurveyScanIndex To udtScanList.SurveyScanCount - 1
                                If udtScanList.SurveyScans(intSurveyScanIndex).ScanNumber = objSpectrumInfo.ScanNumber Then
                                    ' Conflicting scan numbers were found
                                    intScanNumberCorrection = 1

                                    ' Need to update prior MS/MS scans
                                    For intScanIndex = 0 To udtScanList.FragScanCount - 1

                                        With udtScanList.FragScans(intScanIndex)
                                            .ScanNumber += intScanNumberCorrection
                                            dblScanTime = InterpolateRTandFragScanNumber(udtScanList.SurveyScans, udtScanList.SurveyScanCount, 0, .ScanNumber, .FragScanInfo.FragScanNumber)
                                            .ScanTime = CSng(dblScanTime)
                                        End With

                                    Next intScanIndex
                                    Exit For
                                ElseIf udtScanList.SurveyScans(intSurveyScanIndex).ScanNumber > objSpectrumInfo.ScanNumber Then
                                    Exit For
                                End If
                            Next intSurveyScanIndex
                        End If

                        If intScanNumberCorrection > 0 Then
                            objSpectrumInfo.ScanNumber += intScanNumberCorrection
                            objSpectrumInfo.ScanNumberEnd += intScanNumberCorrection
                        End If

                        dblScanTime = InterpolateRTandFragScanNumber(udtScanList.SurveyScans, udtScanList.SurveyScanCount, intLastSurveyScanIndex, objSpectrumInfo.ScanNumber, intFragScanIteration)

                        ' Make sure this fragmentation scan isn't present yet in udtScanList.FragScans
                        ' This can occur in Agilent .MGF files if the scan is listed both singly and grouped with other MS/MS scans
                        blnValidFragScan = True
                        For intScanIndex = 0 To udtScanList.FragScanCount - 1
                            If udtScanList.FragScans(intScanIndex).ScanNumber = objSpectrumInfo.ScanNumber Then
                                ' Duplicate found
                                blnValidFragScan = False
                                Exit For
                            End If
                        Next intScanIndex

                        If blnValidFragScan AndAlso CheckScanInRange(objSpectrumInfo.ScanNumber, dblScanTime, udtSICOptions) Then
                            With udtScanList

                                ' See if intLastSurveyScanIndex needs to be updated
                                ' At the same time, populate .MasterScanOrder
                                Do While intLastSurveyScanIndex < .SurveyScanCount - 1 AndAlso
                                   objSpectrumInfo.ScanNumber > .SurveyScans(intLastSurveyScanIndex + 1).ScanNumber

                                    intLastSurveyScanIndex += 1

                                    ' Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                                    If Not surveyScansRecorded.Contains(intLastSurveyScanIndex) Then
                                        surveyScansRecorded.Add(intLastSurveyScanIndex)

                                        AddMasterScanEntry(udtScanList, eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)
                                    End If
                                Loop

                                AddMasterScanEntry(udtScanList, eScanTypeConstants.FragScan, .FragScanCount, objSpectrumInfo.ScanNumber, CSng(dblScanTime))

                                If .FragScanCount + 1 >= .FragScans.Length Then
                                    ReDim Preserve .FragScans(.FragScanCount + 100)
                                End If

                                With .FragScans(.FragScanCount)
                                    .ScanNumber = objSpectrumInfo.ScanNumber
                                    .ScanTime = CSng(dblScanTime)
                                    .FragScanInfo.FragScanNumber = intFragScanIteration
                                    .FragScanInfo.MSLevel = 2
                                    .MRMScanInfo.MRMMassCount = 0

                                    .ScanHeaderText = String.Empty
                                    .ScanTypeName = "MSn"

                                End With

                                objMSSpectrum.IonCount = objSpectrumInfo.DataCount

                                If objMSSpectrum.IonCount > 0 Then
                                    objMSSpectrum.ScanNumber = .FragScans(.FragScanCount).ScanNumber
                                    With objMSSpectrum
                                        ReDim .IonsMZ(.IonCount - 1)
                                        ReDim .IonsIntensity(.IonCount - 1)

                                        objSpectrumInfo.MZList.CopyTo(.IonsMZ, 0)
                                        objSpectrumInfo.IntensityList.CopyTo(.IonsMZ, 0)
                                    End With

                                    With .FragScans(.FragScanCount)
                                        .IonCount = objMSSpectrum.IonCount
                                        .IonCountRaw = .IonCount

                                        ' Find the base peak ion mass and intensity
                                        .BasePeakIonMZ = FindBasePeakIon(objMSSpectrum.IonsMZ, objMSSpectrum.IonsIntensity, objMSSpectrum.IonCount, .BasePeakIonIntensity, dblMZMin, dblMZMax)

                                        ' Compute the total scan intensity
                                        .TotalIonIntensity = 0
                                        For intIonIndex = 0 To .IonCount - 1
                                            .TotalIonIntensity += objMSSpectrum.IonsIntensity(intIonIndex)
                                        Next intIonIndex

                                        ' Determine the minimum positive intensity in this scan
                                        .MinimumPositiveIntensity = mMASICPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

                                    End With

                                    ProcessAndStoreSpectrum(
                                       .FragScans(.FragScanCount),
                                       objSpectraCache,
                                       objMSSpectrum,
                                       udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                       DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                                       udtSICOptions.CompressMSMSSpectraData,
                                       udtBinningOptions.BinSize / udtSICOptions.CompressToleranceDivisorForDa,
                                       blnKeepRawSpectra And blnKeepMSMSSpectra)

                                Else
                                    With .FragScans(.FragScanCount)
                                        .IonCount = 0
                                        .IonCountRaw = 0
                                        .TotalIonIntensity = 0
                                    End With
                                End If

                                .FragScanCount += 1


                            End With

                            AddUpdateParentIons(udtScanList, intLastSurveyScanIndex, objSpectrumInfo.ParentIonMZ, udtScanList.FragScanCount - 1, objSpectraCache, udtSICOptions)
                        End If

                        ' Note: We need to take intMsScanCount * 2, in addition to adding intMsScanCount to intLastSurveyScanIndex, since we have to read two different files
                        If intMsScanCount > 1 Then
                            SetSubtaskProcessingStepPct(CShort((intLastSurveyScanIndex + intMsScanCount) / (intMsScanCount * 2 - 1) * 100))
                        Else
                            SetSubtaskProcessingStepPct(0)
                        End If

                        UpdateOverallProgress(objSpectraCache)
                        If mAbortProcessing Then
                            udtScanList.ProcessingIncomplete = True
                            Exit Do
                        End If

                        If udtScanList.FragScanCount Mod 100 = 0 Then
                            LogMessage("Reading MSMS scan index: " & udtScanList.FragScanCount.ToString)
                            Console.Write(".")
                        End If

                    End If
                Loop While blnFragScanFound

                ' Record the current memory usage (before we close the .MGF file)
                mProcessingStats.MemoryUsageMBDuringLoad = Math.Max(mProcessingStats.MemoryUsageMBDuringLoad, GetProcessMemoryUsageMB())

                objMGFReader.CloseFile()

                ' Check for any other survey scans that need to be added to be added to MasterScanOrder
                With udtScanList
                    ' See if intLastSurveyScanIndex needs to be updated
                    ' At the same time, populate .MasterScanOrder
                    Do While intLastSurveyScanIndex < .SurveyScanCount - 1

                        intLastSurveyScanIndex += 1

                        If CheckScanInRange(.SurveyScans(intLastSurveyScanIndex).ScanNumber, dblScanTime, udtSICOptions) Then

                            ' Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                            If Not surveyScansRecorded.Contains(intLastSurveyScanIndex) Then
                                surveyScansRecorded.Add(intLastSurveyScanIndex)

                                AddMasterScanEntry(udtScanList, eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)
                            End If
                        End If
                    Loop
                End With

                ' Shrink the memory usage of the udtScanList arrays
                With udtScanList
                    ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)

                    ReDim Preserve .SurveyScans(.SurveyScanCount - 1)
                    ReDim Preserve .FragScans(.FragScanCount - 1)
                End With

                ' Make sure that MasterScanOrder really is sorted by scan number
                ValidateMasterScanOrderSorting(udtScanList)

                ' Now that all of the data has been read, write out to the scan stats file, in order of scan number
                For intScanIndex = 0 To udtScanList.MasterScanOrderCount - 1

                    eScanType = udtScanList.MasterScanOrder(intScanIndex).ScanType
                    If eScanType = eScanTypeConstants.SurveyScan Then
                        ' Survey scan
                        udtCurrentScan = udtScanList.SurveyScans(udtScanList.MasterScanOrder(intScanIndex).ScanIndexPointer)
                    Else
                        ' Frag Scan
                        udtCurrentScan = udtScanList.FragScans(udtScanList.MasterScanOrder(intScanIndex).ScanIndexPointer)
                    End If

                    SaveScanStatEntry(udtOutputFileHandles.ScanStats, eScanType, udtCurrentScan, udtSICOptions.DatasetNumber)
                Next intScanIndex

                Console.WriteLine()
            End If

            Return blnSuccess
        Catch ex As Exception
            LogErrors("ExtractScanInfoFromMGFandCDF", "Error in ExtractScanInfoFromMGFandCDF", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

    End Function

    Private Function ExtractScanInfoFromXcaliburDataFile(
      strFilePath As String,
      intDatasetID As Integer,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      ByRef strStatusMessage As String,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean) As Boolean
        ' Returns True if Success, False if failure
        ' Note: This function assumes strFilePath exists

        Dim ioFileInfo As FileInfo
        Dim strInputFileFullPath As String

        Dim intScanCount As Integer
        Dim intLastNonZoomSurveyScanIndex As Integer

        Dim intScanStart As Integer
        Dim intScanEnd As Integer
        Dim intScanNumber As Integer

        Dim blnSuccess As Boolean

        Dim strIOMode As String
        Dim dtLastLogTime As DateTime

        ' Use Xraw to read the .Raw files
        mXcaliburAccessor = New XRawFileIO
        strIOMode = "Xraw"

        ' Assume success for now
        blnSuccess = True

        Try
            Console.Write("Reading Xcalibur data file ")
            LogMessage("Reading Xcalibur data file")

            SetSubtaskProcessingStepPct(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(strFilePath))

            ' Obtain the full path to the file
            ioFileInfo = New FileInfo(strFilePath)
            strInputFileFullPath = ioFileInfo.FullName

            mXcaliburAccessor.LoadMSMethodInfo = mWriteMSMethodFile
            mXcaliburAccessor.LoadMSTuneInfo = mWriteMSTuneFile

            ' Open a handle to the data file
            If Not mXcaliburAccessor.OpenRawFile(strInputFileFullPath) Then
                strStatusMessage = "Error opening input data file: " & strInputFileFullPath & " (mXcaliburAccessor.OpenRawFile returned False)"
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            If mXcaliburAccessor Is Nothing Then
                strStatusMessage = "Error opening input data file: " & strInputFileFullPath & " (mXcaliburAccessor is Nothing)"
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            blnSuccess = UpdateDatasetFileStats(mDatasetFileInfo, ioFileInfo, intDatasetID, mXcaliburAccessor)

            If mWriteMSMethodFile Then SaveMSMethodFile(mXcaliburAccessor, udtOutputFileHandles)

            If mWriteMSTuneFile Then SaveMSTuneFile(mXcaliburAccessor, udtOutputFileHandles)

            intScanCount = mXcaliburAccessor.GetNumScans()

            If intScanCount <= 0 Then
                ' No scans found
                strStatusMessage = "No scans found in the input file: " & strFilePath
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            Else

                intScanStart = mXcaliburAccessor.FileInfo.ScanStart
                intScanEnd = mXcaliburAccessor.FileInfo.ScanEnd

                With udtSICOptions
                    If .ScanRangeStart > 0 And .ScanRangeEnd = 0 Then
                        .ScanRangeEnd = Integer.MaxValue
                    End If

                    If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                        intScanStart = Math.Max(intScanStart, .ScanRangeStart)
                        intScanEnd = Math.Min(intScanEnd, .ScanRangeEnd)
                    End If
                End With

                UpdateOverallProgress("Reading Xcalibur data with " & strIOMode & " (" & intScanCount.ToString & " scans)" & ControlChars.NewLine & Path.GetFileName(strFilePath))
                LogMessage("Reading Xcalibur data with " & strIOMode & "; Total scan count: " & intScanCount.ToString)
                dtLastLogTime = DateTime.UtcNow

                ' Pre-reserve memory for the maximum number of scans that might be loaded
                ' Re-dimming after loading each scan is extremly slow and uses additional memory
                InitializeScanList(udtScanList, intScanEnd - intScanStart + 1, intScanEnd - intScanStart + 1)
                intLastNonZoomSurveyScanIndex = -1

                Dim htSIMScanMapping = New Dictionary(Of String, Integer)
                udtScanList.SIMDataPresent = False
                udtScanList.MRMDataPresent = False

                For intScanNumber = intScanStart To intScanEnd

                    Dim scanInfo As clsScanInfo = Nothing

                    blnSuccess = mXcaliburAccessor.GetScanInfo(intScanNumber, scanInfo)

                    If Not blnSuccess Then
                        ' GetScanInfo returned false
                        LogMessage("mXcaliburAccessor.GetScanInfo returned false for scan " & intScanNumber.ToString & "; aborting read", eMessageTypeConstants.Warning)
                        Exit For
                    End If

                    If CheckScanInRange(intScanNumber, scanInfo.RetentionTime, udtSICOptions) Then

                        If scanInfo.ParentIonMZ > 0 AndAlso Math.Abs(mParentIonDecoyMassDa) > 0 Then
                            scanInfo.ParentIonMZ += mParentIonDecoyMassDa
                        End If

                        ' Determine if this was an MS/MS scan
                        ' If yes, determine the scan number of the survey scan
                        If scanInfo.MSLevel <= 1 Then
                            ' Survey Scan
                            blnSuccess = ExtractXcaliburSurveyScan(
                               udtScanList, objSpectraCache, udtOutputFileHandles, udtSICOptions,
                               blnKeepRawSpectra, scanInfo, htSIMScanMapping,
                               intLastNonZoomSurveyScanIndex, intScanNumber)

                        Else

                            ' Fragmentation Scan
                            blnSuccess = ExtractXcaliburFragmentationScan(
                               udtScanList, objSpectraCache, udtOutputFileHandles, udtSICOptions, udtBinningOptions,
                               blnKeepRawSpectra, blnKeepMSMSSpectra, scanInfo,
                               intLastNonZoomSurveyScanIndex, intScanNumber)

                        End If

                    End If

                    If intScanCount > 0 Then
                        If intScanNumber Mod 10 = 0 Then
                            SetSubtaskProcessingStepPct(CShort(intScanNumber / intScanCount * 100))
                        End If
                    Else
                        SetSubtaskProcessingStepPct(0)
                    End If

                    UpdateOverallProgress(objSpectraCache)
                    If mAbortProcessing Then
                        udtScanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If intScanNumber Mod 100 = 0 Then
                        If DateTime.UtcNow.Subtract(dtLastLogTime).TotalSeconds >= 10 OrElse intScanNumber Mod 500 = 0 Then
                            LogMessage("Reading scan: " & intScanNumber.ToString)
                            Console.Write(".")
                            dtLastLogTime = DateTime.UtcNow
                        End If

                        ' Call the garbage collector every 100 spectra
                        GC.Collect()
                        GC.WaitForPendingFinalizers()
                        Threading.Thread.Sleep(50)
                    End If

                Next intScanNumber
                Console.WriteLine()

                ' Shrink the memory usage of the udtScanList arrays
                With udtScanList
                    ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)

                    ReDim Preserve .SurveyScans(.SurveyScanCount - 1)
                    ReDim Preserve .FragScans(.FragScanCount - 1)
                End With

            End If

        Catch ex As Exception
            LogErrors("ExtractScanInfoFromXcaliburDataFile", "Error in ExtractScanInfoFromXcaliburDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
        End Try

        ' Record the current memory usage (before we close the .Raw file)
        mProcessingStats.MemoryUsageMBDuringLoad = GetProcessMemoryUsageMB()

        ' Close the handle to the data file
        mXcaliburAccessor.CloseRawFile()
        mXcaliburAccessor = Nothing

        Return blnSuccess

    End Function

    Protected Function ExtractXcaliburSurveyScan(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      udtOutputFileHandles As udtOutputFileHandlesType,
      udtSICOptions As udtSICOptionsType,
      blnKeepRawSpectra As Boolean,
      ByRef scanInfo As clsScanInfo,
      htSIMScanMapping As Dictionary(Of String, Integer),
      ByRef intLastNonZoomSurveyScanIndex As Integer,
      intScanNumber As Integer) As Boolean

        If udtScanList.SurveyScanCount + 1 >= udtScanList.SurveyScans.Length Then
            ' This shouldn't normally happen, as the call to InitializeScanList() should have reserved sufficient memory
            ' Still, if we do reach this code, we'll increase the amount reserved to 25% more than .SurveyScanCount
            ReDim Preserve udtScanList.SurveyScans(CInt(udtScanList.SurveyScanCount * 1.25))
        End If

        With udtScanList.SurveyScans(udtScanList.SurveyScanCount)
            .ScanNumber = intScanNumber
            .ScanTime = CSng(scanInfo.RetentionTime)

            .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(scanInfo.FilterText)
            .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.FilterText)

            .BasePeakIonMZ = scanInfo.BasePeakMZ
            .BasePeakIonIntensity = Math.Min(CSng(scanInfo.BasePeakIntensity), Single.MaxValue)

            .FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here
            .TotalIonIntensity = Math.Min(CSng(scanInfo.TotalIonCurrent), Single.MaxValue)

            ' This will be determined in LoadSpectraForFinniganDataFile
            .MinimumPositiveIntensity = 0

            .ZoomScan = scanInfo.ZoomScan
            .SIMScan = scanInfo.SIMScan
            .MRMScanType = scanInfo.MRMScanType

            If Not .MRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is an MRM scan
                udtScanList.MRMDataPresent = True
            End If

            .LowMass = scanInfo.LowMass
            .HighMass = scanInfo.HighMass

            If .SIMScan Then
                udtScanList.SIMDataPresent = True
                Dim strSIMKey = .LowMass & "_" & .HighMass
                Dim simIndex As Integer

                If htSIMScanMapping.TryGetValue(strSIMKey, simIndex) Then
                    .SIMIndex = simIndex
                Else
                    .SIMIndex = htSIMScanMapping.Count
                    htSIMScanMapping.Add(strSIMKey, htSIMScanMapping.Count)
                End If
            End If

            ' Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(.ExtendedHeaderInfo, scanInfo.ScanEvents)

            ' Store the collision mode and possibly the scan filter text
            .FragScanInfo.CollisionMode = scanInfo.CollisionMode
            StoreExtendedHeaderInfo(.ExtendedHeaderInfo, EXTENDED_STATS_HEADER_COLLISION_MODE, scanInfo.CollisionMode)
            If mWriteExtendedStatsIncludeScanFilterText Then
                StoreExtendedHeaderInfo(.ExtendedHeaderInfo, EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanInfo.FilterText)
            End If

            If mWriteExtendedStatsStatusLog Then
                ' Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(.ExtendedHeaderInfo, scanInfo.StatusLog, mStatusLogKeyNameFilterList)
            End If
        End With

        With udtScanList
            If Not udtScanList.SurveyScans(udtScanList.SurveyScanCount).ZoomScan Then
                intLastNonZoomSurveyScanIndex = .SurveyScanCount
            End If
            .SurveyScanCount += 1

            AddMasterScanEntry(udtScanList, eScanTypeConstants.SurveyScan, .SurveyScanCount - 1)

            Dim dblMSDataResolution As Double

            If udtSICOptions.SICToleranceIsPPM Then
                ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by udtSICOptions.CompressToleranceDivisorForPPM
                ' However, if the lowest m/z value is < 100, then use 100 m/z
                If scanInfo.LowMass < 100 Then
                    dblMSDataResolution = GetParentIonToleranceDa(udtSICOptions, 100) / udtSICOptions.CompressToleranceDivisorForPPM
                Else
                    dblMSDataResolution = GetParentIonToleranceDa(udtSICOptions, scanInfo.LowMass) / udtSICOptions.CompressToleranceDivisorForPPM
                End If
            Else
                dblMSDataResolution = udtSICOptions.SICTolerance / udtSICOptions.CompressToleranceDivisorForDa
            End If

            ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            Dim blnSuccess = LoadSpectraForFinniganDataFile(mXcaliburAccessor, objSpectraCache, intScanNumber, .SurveyScans(.SurveyScanCount - 1), udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD, udtSICOptions.CompressMSSpectraData, dblMSDataResolution, blnKeepRawSpectra)
            If Not blnSuccess Then Return False

            SaveScanStatEntry(udtOutputFileHandles.ScanStats, eScanTypeConstants.SurveyScan, .SurveyScans(.SurveyScanCount - 1), udtSICOptions.DatasetNumber)

        End With

        Return True

    End Function

    Private Function ExtractXcaliburFragmentationScan(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      udtOutputFileHandles As udtOutputFileHandlesType,
      udtSICOptions As udtSICOptionsType,
      udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean,
      ByRef scanInfo As clsScanInfo,
      ByRef intLastNonZoomSurveyScanIndex As Integer,
      intScanNumber As Integer) As Boolean

        If udtScanList.FragScanCount + 1 >= udtScanList.FragScans.Length Then
            ' This shouldn't normally happen, as the call to InitializeScanList() should have reserved sufficient memory
            ' Still, if we do reach this code, we'll increase the amount reserved to 25% more than .FragScanCount
            ReDim Preserve udtScanList.FragScans(CInt(udtScanList.FragScanCount * 1.25))
        End If

        With udtScanList.FragScans(udtScanList.FragScanCount)
            .ScanNumber = intScanNumber
            .ScanTime = CSng(scanInfo.RetentionTime)

            .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(scanInfo.FilterText)
            .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.FilterText)

            .BasePeakIonMZ = scanInfo.BasePeakMZ
            .BasePeakIonIntensity = Math.Min(CSng(scanInfo.BasePeakIntensity), Single.MaxValue)

            .FragScanInfo.FragScanNumber = scanInfo.EventNumber - 1                                      ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.

            ' The .EventNumber value is sometimes wrong; need to check for this
            If udtScanList.FragScanCount > 0 Then
                If udtScanList.FragScans(udtScanList.FragScanCount - 1).ScanNumber = .ScanNumber - 1 Then
                    If .FragScanInfo.FragScanNumber <= udtScanList.FragScans(udtScanList.FragScanCount - 1).FragScanInfo.FragScanNumber Then
                        .FragScanInfo.FragScanNumber = udtScanList.FragScans(udtScanList.FragScanCount - 1).FragScanInfo.FragScanNumber + 1
                    End If
                End If
            End If

            .FragScanInfo.MSLevel = scanInfo.MSLevel

            .TotalIonIntensity = Math.Min(CSng(scanInfo.TotalIonCurrent), Single.MaxValue)

            ' This will be determined in LoadSpectraForFinniganDataFile
            .MinimumPositiveIntensity = 0

            .ZoomScan = scanInfo.ZoomScan
            .SIMScan = scanInfo.SIMScan
            .MRMScanType = scanInfo.MRMScanType
        End With

        If Not udtScanList.FragScans(udtScanList.FragScanCount).MRMScanType = MRMScanTypeConstants.NotMRM Then
            ' This is an MRM scan
            udtScanList.MRMDataPresent = True

            DuplicateMRMInfo(scanInfo.MRMInfo, scanInfo.ParentIonMZ, udtScanList.FragScans(udtScanList.FragScanCount).MRMScanInfo)

            If udtScanList.SurveyScanCount = 0 Then
                ' Need to add a "fake" survey scan that we can map this parent ion to
                intLastNonZoomSurveyScanIndex = AddFakeSurveyScan(udtScanList)
            End If
        Else
            udtScanList.FragScans(udtScanList.FragScanCount).MRMScanInfo.MRMMassCount = 0
        End If

        With udtScanList.FragScans(udtScanList.FragScanCount)
            .LowMass = scanInfo.LowMass
            .HighMass = scanInfo.HighMass

            ' Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(.ExtendedHeaderInfo, scanInfo.ScanEvents)

            ' Store the collision mode and possibly the scan filter text
            .FragScanInfo.CollisionMode = scanInfo.CollisionMode
            StoreExtendedHeaderInfo(.ExtendedHeaderInfo, EXTENDED_STATS_HEADER_COLLISION_MODE, scanInfo.CollisionMode)
            If mWriteExtendedStatsIncludeScanFilterText Then
                StoreExtendedHeaderInfo(.ExtendedHeaderInfo, EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanInfo.FilterText)
            End If

            If mWriteExtendedStatsStatusLog Then
                ' Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(.ExtendedHeaderInfo, scanInfo.StatusLog, mStatusLogKeyNameFilterList)
            End If
        End With

        With udtScanList
            .FragScanCount += 1

            AddMasterScanEntry(udtScanList, eScanTypeConstants.FragScan, .FragScanCount - 1)

            ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            Dim dblMSDataResolution = udtBinningOptions.BinSize / udtSICOptions.CompressToleranceDivisorForDa

            Dim blnSuccess = LoadSpectraForFinniganDataFile(
              mXcaliburAccessor,
              objSpectraCache,
              intScanNumber,
              .FragScans(.FragScanCount - 1),
              udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
              DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
              udtSICOptions.CompressMSMSSpectraData,
              dblMSDataResolution,
              blnKeepRawSpectra And blnKeepMSMSSpectra)

            If Not blnSuccess Then Return False

            SaveScanStatEntry(udtOutputFileHandles.ScanStats, eScanTypeConstants.FragScan, .FragScans(.FragScanCount - 1), udtSICOptions.DatasetNumber)

        End With

        If scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
            ' This is not an MRM scan
            AddUpdateParentIons(udtScanList, intLastNonZoomSurveyScanIndex, scanInfo.ParentIonMZ, udtScanList.FragScanCount - 1, objSpectraCache, udtSICOptions)
        Else
            ' This is an MRM scan
            AddUpdateParentIons(udtScanList, intLastNonZoomSurveyScanIndex, scanInfo.ParentIonMZ, udtScanList.FragScans(udtScanList.FragScanCount - 1).MRMScanInfo, objSpectraCache, udtSICOptions)
        End If

        Return True

    End Function


    Private Function ExtractScanInfoFromMZXMLDataFile(
      strFilePath As String,
      intDatasetID As Integer,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean) As Boolean

        Dim objXMLReader As MSDataFileReader.clsMSDataFileReaderBaseClass

        Try
            objXMLReader = New MSDataFileReader.clsMzXMLFileReader
            Return ExtractScanInfoFromMSXMLDataFile(strFilePath, intDatasetID, objXMLReader, udtScanList, objSpectraCache, udtOutputFileHandles, udtSICOptions, udtBinningOptions, mStatusMessage, blnKeepRawSpectra, blnKeepMSMSSpectra)

        Catch ex As Exception
            LogErrors("ExtractScanInfoFromMZXMLDataFile", "Error in ExtractScanInfoFromMZXMLDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

    End Function

    Private Function ExtractScanInfoFromMZDataFile(
      strFilePath As String,
      intDatasetID As Integer,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean) As Boolean

        Dim objXMLReader As MSDataFileReader.clsMSDataFileReaderBaseClass

        Try
            objXMLReader = New MSDataFileReader.clsMzDataFileReader
            Return ExtractScanInfoFromMSXMLDataFile(strFilePath, intDatasetID, objXMLReader, udtScanList, objSpectraCache, udtOutputFileHandles, udtSICOptions, udtBinningOptions, mStatusMessage, blnKeepRawSpectra, blnKeepMSMSSpectra)

        Catch ex As Exception
            LogErrors("ExtractScanInfoFromMZDataFile", "Error in ExtractScanInfoFromMZDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

    End Function

    Private Function ExtractScanInfoFromMSXMLDataFile(
      strFilePath As String,
      intDatasetID As Integer,
      ByRef objXMLReader As MSDataFileReader.clsMSDataFileReaderBaseClass,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      ByRef strStatusMessage As String,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean) As Boolean

        ' Returns True if Success, False if failure
        ' Note: This function assumes strFilePath exists

        Dim ioFileInfo As FileInfo
        Dim strInputFileFullPath As String

        Dim intLastSurveyScanIndex As Integer
        Dim intLastSurveyScanIndexInMasterSeqOrder As Integer
        Dim intLastNonZoomSurveyScanIndex As Integer
        Dim intWarnCount = 0

        Dim eMRMScanType As MRMScanTypeConstants

        Dim objSpectrumInfo As MSDataFileReader.clsSpectrumInfo = Nothing
        Dim objMZXmlSpectrumInfo As MSDataFileReader.clsSpectrumInfoMzXML = Nothing

        Dim objMSSpectrum As New clsMSSpectrum

        ' ReSharper disable once NotAccessedVariable
        Dim dblMSDataResolution As Double

        Dim blnScanFound As Boolean
        Dim blnSuccess As Boolean
        Dim blnIsMzXML As Boolean

        Try
            Console.Write("Reading MSXml data file ")
            LogMessage("Reading MSXml data file")

            SetSubtaskProcessingStepPct(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(strFilePath))

            ' Obtain the full path to the file
            ioFileInfo = New FileInfo(strFilePath)
            strInputFileFullPath = ioFileInfo.FullName

            blnSuccess = UpdateDatasetFileStats(mDatasetFileInfo, ioFileInfo, intDatasetID)
            mDatasetFileInfo.ScanCount = 0

            ' Open a handle to the data file
            If Not objXMLReader.OpenFile(strInputFileFullPath) Then
                strStatusMessage = "Error opening input data file: " & strInputFileFullPath
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            ' We won't know the total scan count until we have read all the data
            ' Thus, initially reserve space for 1000 scans

            InitializeScanList(udtScanList, 1000, 1000)
            intLastSurveyScanIndex = -1
            intLastSurveyScanIndexInMasterSeqOrder = -1
            intLastNonZoomSurveyScanIndex = -1

            udtScanList.SIMDataPresent = False
            udtScanList.MRMDataPresent = False

            UpdateOverallProgress("Reading XML data" & ControlChars.NewLine & Path.GetFileName(strFilePath))
            LogMessage("Reading XML data from " & strFilePath)

            Do
                blnScanFound = objXMLReader.ReadNextSpectrum(objSpectrumInfo)

                If blnScanFound Then
                    mDatasetFileInfo.ScanCount += 1

                    With objMSSpectrum
                        .IonCount = objSpectrumInfo.DataCount

                        ReDim .IonsMZ(.IonCount - 1)
                        ReDim .IonsIntensity(.IonCount - 1)

                        objSpectrumInfo.MZList.CopyTo(.IonsMZ, 0)
                        objSpectrumInfo.IntensityList.CopyTo(.IonsIntensity, 0)
                    End With

                    ' No Error
                    If CheckScanInRange(objSpectrumInfo.ScanNumber, objSpectrumInfo.RetentionTimeMin, udtSICOptions) Then

                        If TypeOf (objSpectrumInfo) Is MSDataFileReader.clsSpectrumInfoMzXML Then
                            objMZXmlSpectrumInfo = CType(objSpectrumInfo, MSDataFileReader.clsSpectrumInfoMzXML)
                            blnIsMzXML = True
                        Else
                            blnIsMzXML = False
                        End If

                        ' Determine if this was an MS/MS scan
                        ' If yes, determine the scan number of the survey scan
                        If objSpectrumInfo.MSLevel <= 1 Then
                            ' Survey Scan
                            If udtScanList.SurveyScanCount + 1 >= udtScanList.SurveyScans.Length Then
                                ' Double the space reserved for .SurveyScans
                                ReDim Preserve udtScanList.SurveyScans(udtScanList.SurveyScans.Length * 2 - 1)
                            End If

                            With udtScanList.SurveyScans(udtScanList.SurveyScanCount)
                                .ScanNumber = objSpectrumInfo.ScanNumber
                                .ScanTime = objSpectrumInfo.RetentionTimeMin

                                ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                                .ScanHeaderText = String.Empty
                                .ScanTypeName = "MS"                ' This may get updated via the call to UpdateMSXmlScanType()

                                .BasePeakIonMZ = objSpectrumInfo.BasePeakMZ
                                .BasePeakIonIntensity = objSpectrumInfo.BasePeakIntensity

                                .FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here
                                .TotalIonIntensity = CSng(Math.Min(objSpectrumInfo.TotalIonCurrent, Single.MaxValue))

                                ' Determine the minimum positive intensity in this scan
                                .MinimumPositiveIntensity = mMASICPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

                                ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                                .ZoomScan = False
                                .SIMScan = False
                                .MRMScanType = MRMScanTypeConstants.NotMRM

                                .LowMass = objSpectrumInfo.mzRangeStart
                                .HighMass = objSpectrumInfo.mzRangeEnd

                            End With

                            UpdateMSXmlScanType(udtScanList.SurveyScans(udtScanList.SurveyScanCount), objSpectrumInfo.MSLevel, "MS", blnIsMzXML, objMZXmlSpectrumInfo)

                            With udtScanList
                                intLastSurveyScanIndex = .SurveyScanCount
                                .SurveyScanCount += 1

                                AddMasterScanEntry(udtScanList, eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)
                                intLastSurveyScanIndexInMasterSeqOrder = .MasterScanOrderCount - 1

                                If udtSICOptions.SICToleranceIsPPM Then
                                    ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by udtSICOptions.CompressToleranceDivisorForPPM
                                    ' However, if the lowest m/z value is < 100, then use 100 m/z
                                    If objSpectrumInfo.mzRangeStart < 100 Then
                                        dblMSDataResolution = GetParentIonToleranceDa(udtSICOptions, 100) / udtSICOptions.CompressToleranceDivisorForPPM
                                    Else
                                        dblMSDataResolution = GetParentIonToleranceDa(udtSICOptions, objSpectrumInfo.mzRangeStart) / udtSICOptions.CompressToleranceDivisorForPPM
                                    End If
                                Else
                                    dblMSDataResolution = udtSICOptions.SICTolerance / udtSICOptions.CompressToleranceDivisorForDa
                                End If


                                ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
                                StoreMzXmlSpectrum(
                                 objMSSpectrum,
                                 .SurveyScans(.SurveyScanCount - 1),
                                 objSpectraCache,
                                 udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                 DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                                 udtSICOptions.CompressMSSpectraData,
                                 udtSICOptions.SimilarIonMZToleranceHalfWidth / udtSICOptions.CompressToleranceDivisorForDa,
                                 blnKeepRawSpectra)

                                SaveScanStatEntry(udtOutputFileHandles.ScanStats, eScanTypeConstants.SurveyScan, .SurveyScans(.SurveyScanCount - 1), udtSICOptions.DatasetNumber)

                            End With
                        Else
                            ' Fragmentation Scan

                            If udtScanList.FragScanCount + 1 >= udtScanList.FragScans.Length Then
                                ' Double the space reserved for .FragScans
                                ReDim Preserve udtScanList.FragScans(udtScanList.FragScans.Length * 2 - 1)
                            End If

                            With udtScanList.FragScans(udtScanList.FragScanCount)
                                .ScanNumber = objSpectrumInfo.ScanNumber
                                .ScanTime = objSpectrumInfo.RetentionTimeMin

                                ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                                .ScanHeaderText = String.Empty
                                .ScanTypeName = "MSn"               ' This may get updated via the call to UpdateMSXmlScanType()

                                .BasePeakIonMZ = objSpectrumInfo.BasePeakMZ
                                .BasePeakIonIntensity = objSpectrumInfo.BasePeakIntensity

                                .FragScanInfo.FragScanNumber = (udtScanList.MasterScanOrderCount - 1) - intLastSurveyScanIndexInMasterSeqOrder      ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
                                .FragScanInfo.MSLevel = objSpectrumInfo.MSLevel

                                .TotalIonIntensity = CSng(Math.Min(objSpectrumInfo.TotalIonCurrent, Single.MaxValue))

                                ' Determine the minimum positive intensity in this scan
                                .MinimumPositiveIntensity = mMASICPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

                                ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                                .ZoomScan = False
                                .SIMScan = False
                                .MRMScanType = MRMScanTypeConstants.NotMRM

                                .MRMScanInfo.MRMMassCount = 0

                            End With

                            UpdateMSXmlScanType(udtScanList.FragScans(udtScanList.FragScanCount), objSpectrumInfo.MSLevel, "MSn", blnIsMzXML, objMZXmlSpectrumInfo)

                            eMRMScanType = udtScanList.FragScans(udtScanList.FragScanCount).MRMScanType
                            If Not eMRMScanType = MRMScanTypeConstants.NotMRM Then
                                ' This is an MRM scan
                                udtScanList.MRMDataPresent = True

                                Dim scanInfo = New clsScanInfo(objSpectrumInfo.SpectrumID)

                                With scanInfo
                                    .FilterText = udtScanList.FragScans(udtScanList.FragScanCount).ScanHeaderText
                                    .MRMScanType = eMRMScanType
                                    .MRMInfo = New MRMInfo

                                    If Not String.IsNullOrEmpty(.FilterText) Then
                                        ' Parse out the MRM_QMS or SRM information for this scan
                                        XRawFileIO.ExtractMRMMasses(.FilterText, .MRMScanType, .MRMInfo)
                                    Else
                                        ' .MZRangeStart and .MZRangeEnd should be equivalent, and they should define the m/z of the MRM transition

                                        If objSpectrumInfo.mzRangeEnd - objSpectrumInfo.mzRangeStart >= 0.5 Then
                                            ' The data is likely MRM and not SRM
                                            ' We cannot currently handle data like this (would need to examine the mass values and find the clumps of data to infer the transitions present
                                            intWarnCount += 1
                                            If intWarnCount <= 5 Then
                                                ShowErrorMessage("Warning: m/z range for SRM scan " & objSpectrumInfo.ScanNumber & " is " & (objSpectrumInfo.mzRangeEnd - objSpectrumInfo.mzRangeStart).ToString("0.0") & " m/z; this is likely a MRM scan, but MASIC doesn't support inferring the MRM transition masses from the observed m/z values.  Results will likely not be meaningful", True)
                                                If intWarnCount = 5 Then
                                                    ShowMessage("Additional m/z range warnings will not be shown", True)
                                                End If
                                            End If
                                        End If

                                        Dim mRMMassRange As udtMRMMassRangeType
                                        mRMMassRange = New udtMRMMassRangeType()
                                        With mRMMassRange
                                            .StartMass = objSpectrumInfo.mzRangeStart
                                            .EndMass = objSpectrumInfo.mzRangeEnd
                                            .CentralMass = Math.Round(.StartMass + (.EndMass - .StartMass) / 2, 6)
                                        End With
                                        .MRMInfo.MRMMassList.Add(mRMMassRange)

                                    End If
                                End With

                                DuplicateMRMInfo(scanInfo.MRMInfo, objSpectrumInfo.ParentIonMZ, udtScanList.FragScans(udtScanList.FragScanCount).MRMScanInfo)


                                If udtScanList.SurveyScanCount = 0 Then
                                    ' Need to add a "fake" survey scan that we can map this parent ion to
                                    intLastNonZoomSurveyScanIndex = AddFakeSurveyScan(udtScanList)
                                End If
                            Else
                                udtScanList.FragScans(udtScanList.FragScanCount).MRMScanInfo.MRMMassCount = 0
                            End If

                            With udtScanList.FragScans(udtScanList.FragScanCount)
                                .LowMass = objSpectrumInfo.mzRangeStart
                                .HighMass = objSpectrumInfo.mzRangeEnd
                            End With

                            With udtScanList
                                .FragScanCount += 1

                                AddMasterScanEntry(udtScanList, eScanTypeConstants.FragScan, .FragScanCount - 1)

                                ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
                                StoreMzXmlSpectrum(
                                objMSSpectrum,
                                 .FragScans(.FragScanCount - 1),
                                 objSpectraCache,
                                 udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                 DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                                 udtSICOptions.CompressMSMSSpectraData,
                                 udtBinningOptions.BinSize / udtSICOptions.CompressToleranceDivisorForDa,
                                 blnKeepRawSpectra And blnKeepMSMSSpectra)

                                SaveScanStatEntry(udtOutputFileHandles.ScanStats, eScanTypeConstants.FragScan, .FragScans(.FragScanCount - 1), udtSICOptions.DatasetNumber)

                            End With

                            If eMRMScanType = MRMScanTypeConstants.NotMRM Then
                                ' This is not an MRM scan
                                AddUpdateParentIons(udtScanList, intLastSurveyScanIndex, objSpectrumInfo.ParentIonMZ, udtScanList.FragScanCount - 1, objSpectraCache, udtSICOptions)
                            Else
                                ' This is an MRM scan
                                AddUpdateParentIons(udtScanList, intLastNonZoomSurveyScanIndex, objSpectrumInfo.ParentIonMZ, udtScanList.FragScans(udtScanList.FragScanCount - 1).MRMScanInfo, objSpectraCache, udtSICOptions)
                            End If

                        End If

                    End If

                    SetSubtaskProcessingStepPct(CShort(Math.Round(objXMLReader.ProgressPercentComplete, 0)))

                    UpdateOverallProgress(objSpectraCache)
                    If mAbortProcessing Then
                        udtScanList.ProcessingIncomplete = True
                        Exit Do
                    End If

                    If (udtScanList.MasterScanOrderCount - 1) Mod 100 = 0 Then
                        LogMessage("Reading scan index: " & (udtScanList.MasterScanOrderCount - 1).ToString)
                        Console.Write(".")
                    End If

                End If
            Loop While blnScanFound

            ' Shrink the memory usage of the udtScanList arrays
            With udtScanList
                ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
                ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
                ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)

                ReDim Preserve .SurveyScans(.SurveyScanCount - 1)
                ReDim Preserve .FragScans(.FragScanCount - 1)
            End With

            If udtScanList.MasterScanOrderCount <= 0 Then
                ' No scans found
                strStatusMessage = "No scans found in the input file: " & strFilePath
                SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            blnSuccess = True

            Console.WriteLine()
        Catch ex As Exception
            LogErrors("ExtractScanInfoFromMSXMLDataFile", "Error in ExtractScanInfoFromMSXMLDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
        End Try

        ' Record the current memory usage (before we close the .mzXML file)
        mProcessingStats.MemoryUsageMBDuringLoad = GetProcessMemoryUsageMB()

        ' Close the handle to the data file
        If Not objXMLReader Is Nothing Then
            Try
                objXMLReader.CloseFile()
                objXMLReader = Nothing
            Catch ex As Exception
                ' Ignore errors here
            End Try
        End If

        Return blnSuccess

    End Function

    Private Sub UpdateMSXmlScanType(
      ByRef udtScanInfo As udtScanInfoType,
      intMSLevel As Integer,
      strDefaultScanType As String,
      blnIsMzXML As Boolean,
      ByRef objMZXmlSpectrumInfo As MSDataFileReader.clsSpectrumInfoMzXML)

        Dim intMSLevelFromFilter As Integer

        With udtScanInfo
            If blnIsMzXML Then
                ' Store the filter line text in .ScanHeaderText
                ' Only Thermo files processed with ReadW will have a FilterLine

                .ScanHeaderText = objMZXmlSpectrumInfo.FilterLine

                If Not String.IsNullOrEmpty(.ScanHeaderText) Then
                    ' This is a Thermo file; auto define .ScanTypeName using the FilterLine text
                    .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(.ScanHeaderText)

                    ' Now populate .SIMScan, .MRMScanType and .ZoomScan
                    Dim blnValidScan = XRawFileIO.ValidateMSScan(.ScanHeaderText, intMSLevelFromFilter, .SIMScan, .MRMScanType, .ZoomScan)

                Else
                    .ScanHeaderText = String.Empty
                    .ScanTypeName = objMZXmlSpectrumInfo.ScanType

                    If String.IsNullOrEmpty(.ScanTypeName) Then
                        .ScanTypeName = strDefaultScanType
                    Else
                        ' Possibly update .ScanTypeName to match the values returned by XRawFileIO.GetScanTypeNameFromFinniganScanFilterText()
                        Select Case .ScanTypeName.ToLower
                            Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.Full.ToLower
                                If intMSLevel <= 1 Then
                                    .ScanTypeName = "MS"
                                Else
                                    .ScanTypeName = "MSn"
                                End If

                            Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.zoom.ToLower
                                .ScanTypeName = "Zoom-MS"

                            Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.MRM.ToLower
                                .ScanTypeName = "MRM"
                                .MRMScanType = MRMScanTypeConstants.SRM

                            Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.SRM.ToLower
                                .ScanTypeName = "CID-SRM"
                                .MRMScanType = MRMScanTypeConstants.SRM
                            Case Else
                                ' Leave .ScanTypeName unchanged
                        End Select
                    End If

                    If Not String.IsNullOrWhiteSpace(objMZXmlSpectrumInfo.ActivationMethod) Then
                        ' Update ScanTypeName to include the activation method, 
                        ' For example, to be CID-MSn instead of simply MSn
                        .ScanTypeName = objMZXmlSpectrumInfo.ActivationMethod & "-" & .ScanTypeName

                        If .ScanTypeName = "HCD-MSn" Then
                            ' HCD spectra are always high res; auto-update things
                            .ScanTypeName = "HCD-HMSn"
                        End If

                    End If
                End If

            Else
                ' Not a .mzXML file
                ' Use the defaults
                .ScanHeaderText = String.Empty
                .ScanTypeName = strDefaultScanType
            End If
        End With

    End Sub

    Private Function FindBasePeakIon(
      ByRef dblMZList() As Double,
      ByRef sngIonIntensity() As Single,
      intIonCount As Integer,
      ByRef sngBasePeakIonIntensity As Single,
      ByRef dblMZMin As Double,
      ByRef dblMZMax As Double) As Double

        ' Finds the base peak ion
        ' Also determines the minimum and maximum m/z values in dblMZList
        Dim intBasePeakIndex As Integer
        Dim intDataIndex As Integer

        Try
            dblMZMin = dblMZList(0)
            dblMZMax = dblMZList(0)

            intBasePeakIndex = 0
            For intDataIndex = 0 To intIonCount - 1
                If sngIonIntensity(intDataIndex) > sngIonIntensity(intBasePeakIndex) Then
                    intBasePeakIndex = intDataIndex
                End If

                If dblMZList(intDataIndex) < dblMZMin Then
                    dblMZMin = dblMZList(intDataIndex)
                End If

                If dblMZList(intDataIndex) > dblMZMax Then
                    dblMZMax = dblMZList(intDataIndex)
                End If

            Next intDataIndex

            sngBasePeakIonIntensity = sngIonIntensity(intBasePeakIndex)
            Return dblMZList(intBasePeakIndex)

        Catch ex As Exception
            LogErrors("FindBasePeakIon", "Error in FindBasePeakIon", ex, True, False)
            sngBasePeakIonIntensity = 0
            Return 0
        End Try

    End Function

    Private Function FindClosestMZ(
      objSpectraCache As clsSpectraCache,
      ByRef udtScanInfo() As udtScanInfoType,
      intSpectrumIndex As Integer,
      dblSearchMZ As Double,
      dblToleranceMZ As Double,
      <Out()> ByRef dblBestMatchMZ As Double) As Boolean

        Dim intPoolIndex As Integer
        Dim blnSuccess As Boolean

        dblBestMatchMZ = 0

        Try
            If udtScanInfo(intSpectrumIndex).IonCount = 0 And udtScanInfo(intSpectrumIndex).IonCountRaw = 0 Then
                ' No data in this spectrum
                blnSuccess = False
            Else
                If Not objSpectraCache.ValidateSpectrumInPool(udtScanInfo(intSpectrumIndex).ScanNumber, intPoolIndex) Then
                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                    blnSuccess = False
                Else
                    With objSpectraCache.SpectraPool(intPoolIndex)
                        blnSuccess = FindClosestMZ(.IonsMZ, .IonCount, dblSearchMZ, dblToleranceMZ, dblBestMatchMZ)
                    End With
                End If

            End If
        Catch ex As Exception
            LogErrors("FindClosestMZ_SpectraCache", "Error in FindClosestMZ", ex, True, False)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function FindClosestMZ(
      ByRef dblMZList() As Double,
      intIonCount As Integer,
      dblSearchMZ As Double,
      dblToleranceMZ As Double,
      <Out()> ByRef dblBestMatchMZ As Double) As Boolean

        ' Searches dblMZList for the closest match to dblSearchMZ within tolerance dblBestMatchMZ
        ' If a match is found, then updates dblBestMatchMZ to the m/z of the match and returns True

        Dim intDataIndex As Integer
        Dim intClosestMatchIndex As Integer
        Dim dblMassDifferenceAbs As Double
        Dim dblBestMassDifferenceAbs As Double

        Try
            intClosestMatchIndex = -1
            For intDataIndex = 0 To intIonCount - 1
                dblMassDifferenceAbs = Math.Abs(dblMZList(intDataIndex) - dblSearchMZ)
                If dblMassDifferenceAbs <= dblToleranceMZ Then
                    If intClosestMatchIndex < 0 OrElse dblMassDifferenceAbs < dblBestMassDifferenceAbs Then
                        intClosestMatchIndex = intDataIndex
                        dblBestMassDifferenceAbs = dblMassDifferenceAbs
                    End If
                End If
            Next intDataIndex

        Catch ex As Exception
            LogErrors("FindClosestMZ", "Error in FindClosestMZ", ex, True, False)
            intClosestMatchIndex = -1
        End Try

        If intClosestMatchIndex >= 0 Then
            dblBestMatchMZ = dblMZList(intClosestMatchIndex)
            Return True
        Else
            dblBestMatchMZ = 0
            Return False
        End If

    End Function

    Private Function FindMaxValueInMZRange(
      objSpectraCache As clsSpectraCache,
      ByRef udtScanInfo() As udtScanInfoType,
      intSpectrumIndex As Integer,
      dblMZStart As Double,
      dblMZEnd As Double,
      ByRef dblBestMatchMZ As Double,
      ByRef sngMatchIntensity As Single) As Boolean

        ' Searches udtScanInfo(intSpectrumIndex).IonsMZ for the maximum value between dblMZStart and dblMZEnd
        ' If a match is found, then updates dblBestMatchMZ to the m/z of the match, updates sngMatchIntensity to its intensity,
        '  and returns True
        '
        ' Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
        '  and bad for spectra with > 10 data points
        ' As an alternative to this function, use AggregateIonsInRange

        Dim intPoolIndex As Integer
        Dim blnSuccess As Boolean

        Try
            If Not objSpectraCache.ValidateSpectrumInPool(udtScanInfo(intSpectrumIndex).ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                blnSuccess = False
            Else
                With objSpectraCache.SpectraPool(intPoolIndex)
                    blnSuccess = FindMaxValueInMZRange(.IonsMZ, .IonsIntensity, .IonCount, dblMZStart, dblMZEnd, dblBestMatchMZ, sngMatchIntensity)
                End With
            End If
        Catch ex As Exception
            LogErrors("FindClosestMZ_SpectraCache", "Error in FindMaxValueInMZRange", ex, True, False)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function FindMaxValueInMZRange(
      ByRef dblMZList() As Double,
      ByRef sngIntensityList() As Single,
      intIonCount As Integer,
      dblMZStart As Double,
      dblMZEnd As Double,
      ByRef dblBestMatchMZ As Double,
      ByRef sngMatchIntensity As Single) As Boolean

        ' Searches dblMZList for the maximum value between dblMZStart and dblMZEnd
        ' If a match is found, then updates dblBestMatchMZ to the m/z of the match, updates sngMatchIntensity to its intensity,
        '  and returns True
        '
        ' Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
        '  and bad for spectra with > 10 data points
        ' As an alternative to this function, use AggregateIonsInRange

        Dim intDataIndex As Integer
        Dim intClosestMatchIndex As Integer
        Dim sngHighestIntensity As Single

        Try
            intClosestMatchIndex = -1
            sngHighestIntensity = 0

            For intDataIndex = 0 To intIonCount - 1
                If dblMZList(intDataIndex) >= dblMZStart AndAlso dblMZList(intDataIndex) <= dblMZEnd Then
                    If intClosestMatchIndex < 0 Then
                        intClosestMatchIndex = intDataIndex
                        sngHighestIntensity = sngIntensityList(intDataIndex)
                    ElseIf sngIntensityList(intDataIndex) > sngHighestIntensity Then
                        intClosestMatchIndex = intDataIndex
                        sngHighestIntensity = sngIntensityList(intDataIndex)
                    End If
                End If
            Next intDataIndex

        Catch ex As Exception
            LogErrors("FindClosestMZ", "Error in FindMaxValueInMZRange", ex, True, False)
            intClosestMatchIndex = -1
        End Try

        If intClosestMatchIndex >= 0 Then
            dblBestMatchMZ = dblMZList(intClosestMatchIndex)
            sngMatchIntensity = sngHighestIntensity
            Return True
        Else
            dblBestMatchMZ = 0
            sngMatchIntensity = 0
            Return False
        End If

    End Function

    Private Function FindNearestSurveyScanIndex(
      ByRef udtScanList As udtScanListType,
      sngScanOrAcqTime As Single,
      eScanType As eCustomSICScanTypeConstants) As Integer

        ' Finds the index of the survey scan closest to sngScanOrAcqTime
        ' Note that sngScanOrAcqTime can be absolute, relative, or AcquisitionTime; eScanType specifies which it is

        Dim intIndex As Integer
        Dim intScanNumberToFind As Integer
        Dim intSurveyScanIndexMatch As Integer


        Try
            intSurveyScanIndexMatch = -1
            intScanNumberToFind = ScanOrAcqTimeToAbsolute(udtScanList, sngScanOrAcqTime, eScanType, False)
            For intIndex = 0 To udtScanList.SurveyScanCount - 1
                If udtScanList.SurveyScans(intIndex).ScanNumber >= intScanNumberToFind Then
                    intSurveyScanIndexMatch = intIndex
                    If udtScanList.SurveyScans(intIndex).ScanNumber <> intScanNumberToFind AndAlso intIndex < udtScanList.SurveyScanCount - 1 Then
                        ' Didn't find an exact match; determine which survey scan is closer
                        If Math.Abs(udtScanList.SurveyScans(intIndex + 1).ScanNumber - intScanNumberToFind) <
                           Math.Abs(udtScanList.SurveyScans(intIndex).ScanNumber - intScanNumberToFind) Then
                            intSurveyScanIndexMatch += 1
                        End If
                    End If
                    Exit For
                End If
            Next intIndex

            If intSurveyScanIndexMatch < 0 Then
                ' Match not found; return either the first or the last survey scan
                If udtScanList.SurveyScanCount > 0 Then
                    intSurveyScanIndexMatch = udtScanList.SurveyScanCount - 1
                Else
                    intSurveyScanIndexMatch = 0
                End If
            End If
        Catch ex As Exception
            LogErrors("FindNearestSurveyScanIndex", "Error in FindNearestSurveyScanIndex", ex, True, False)
            intSurveyScanIndexMatch = 0
        End Try

        Return intSurveyScanIndexMatch

    End Function

    ''' <summary>
    ''' Returns the index of the scan closest to sngScanOrAcqTime (searching both Survey and Frag Scans using the MasterScanList)
    ''' </summary>
    ''' <param name="udtScanList"></param>
    ''' <param name="sngScanOrAcqTime">can be absolute, relative, or AcquisitionTime</param>
    ''' <param name="eScanType">Specifies what type of value value sngScanOrAcqTime is; 0=absolute, 1=relative, 2=acquisition time (aka elution time)</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function FindNearestScanNumIndex(
      ByRef udtScanList As udtScanListType,
      sngScanOrAcqTime As Single,
      eScanType As eCustomSICScanTypeConstants) As Integer

        Dim intAbsoluteScanNumber As Integer
        Dim intScanIndexMatch As Integer

        Try
            If eScanType = eCustomSICScanTypeConstants.Absolute Or eScanType = eCustomSICScanTypeConstants.Relative Then
                intAbsoluteScanNumber = ScanOrAcqTimeToAbsolute(udtScanList, sngScanOrAcqTime, eScanType, False)
                intScanIndexMatch = clsBinarySearch.BinarySearchFindNearest(udtScanList.MasterScanNumList, intAbsoluteScanNumber, udtScanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)
            Else
                ' eScanType = eCustomSICScanTypeConstants.AcquisitionTime
                ' Find the closest match in udtScanList.MasterScanTimeList
                intScanIndexMatch = clsBinarySearch.BinarySearchFindNearest(udtScanList.MasterScanTimeList, sngScanOrAcqTime, udtScanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)
            End If

        Catch ex As Exception
            LogErrors("FindNearestScanNumIndex", "Error in FindNearestScanNumIndex", ex, True, False)
            intScanIndexMatch = 0
        End Try

        Return intScanIndexMatch
    End Function

    ''Private Sub FindMinimumPotentialPeakAreaInRegion(ByRef udtScanList As udtScanListType, intParentIonIndexStart As Integer, intParentIonIndexEnd As Integer, ByRef udtSICPotentialAreaStatsForRegion As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType)
    ''    ' This function finds the minimum potential peak area in the parent ions between
    ''    '  intParentIonIndexStart and intParentIonIndexEnd
    ''    ' However, the summed intensity is not used if the number of points >= .SICNoiseThresholdIntensity is less than MASICPeakFinder.clsMASICPeakFinder.MINIMUM_PEAK_WIDTH

    ''    Dim intParentIonIndex As Integer
    ''    Dim intIndex As Integer

    ''    With udtSICPotentialAreaStatsForRegion
    ''        .MinimumPotentialPeakArea = Double.MaxValue
    ''        .PeakCountBasisForMinimumPotentialArea = 0
    ''    End With

    ''    For intParentIonIndex = intParentIonIndexStart To intParentIonIndexEnd
    ''        With udtScanList.ParentIons(intParentIonIndex).SICStats.SICPotentialAreaStatsForPeak

    ''            If .MinimumPotentialPeakArea > 0 AndAlso .PeakCountBasisForMinimumPotentialArea >= MASICPeakFinder.clsMASICPeakFinder.MINIMUM_PEAK_WIDTH Then
    ''                If .PeakCountBasisForMinimumPotentialArea > udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
    ''                    ' The non valid peak count value is larger than the one associated with the current
    ''                    '  minimum potential peak area; update the minimum peak area to dblPotentialPeakArea
    ''                    udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
    ''                    udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
    ''                Else
    ''                    If .MinimumPotentialPeakArea < udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea AndAlso
    ''                       .PeakCountBasisForMinimumPotentialArea >= udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
    ''                        udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
    ''                        udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
    ''                    End If
    ''                End If
    ''            End If

    ''        End With
    ''    Next intParentIonIndex

    ''    If udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = Double.MaxValue Then
    ''        udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = 1
    ''    End If

    ''End Sub

    ''Private Function FindSICPeakAndAreaForParentIon(ByRef udtScanList As udtScanListType, intParentIonIndex As Integer, ByRef udtSICDetails As udtSICStatsDetailsType, ByRef udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType, udtSICOptions As udtSICOptionsType) As Boolean

    ''    Const RECOMPUTE_NOISE_LEVEL As Boolean = True

    ''    Dim intSurveyScanIndex As Integer
    ''    Dim intDataIndex As Integer
    ''    Dim intIndexPointer As Integer
    ''    Dim intParentIonIndexStart As Integer

    ''    Dim intScanIndexObserved As Integer
    ''    Dim intScanDelta As Integer

    ''    Dim intFragScanNumber As Integer

    ''    Dim intAreaDataCount As Integer
    ''    Dim intAreaDataBaseIndex As Integer

    ''    Dim sngFWHMScanStart, sngFWHMScanEnd As Single

    ''    Dim udtSICPotentialAreaStatsForRegion As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
    ''    Dim sngMaxIntensityValueRawData As Single

    ''    Dim intSICScanNumbers() As Integer
    ''    Dim sngSICIntensities() As Single

    ''    Dim blnCustomSICPeak As Boolean
    ''    Dim blnSuccess As Boolean

    ''    Try

    ''        ' Determine the minimum potential peak area in the last 500 scans
    ''        intParentIonIndexStart = intParentIonIndex - 500
    ''        If intParentIonIndexStart < 0 Then intParentIonIndexStart = 0
    ''        FindMinimumPotentialPeakAreaInRegion(udtScanList, intParentIonIndexStart, intParentIonIndex, udtSICPotentialAreaStatsForRegion)

    ''        With udtScanList
    ''            With .ParentIons(intParentIonIndex)
    ''                intScanIndexObserved = .SurveyScanIndex
    ''                If intScanIndexObserved < 0 Then intScanIndexObserved = 0
    ''                blnCustomSICPeak = .CustomSICPeak
    ''            End With

    ''            If udtSICDetails.SICData Is Nothing OrElse udtSICDetails.SICDataCount = 0 Then
    ''                ' Either .SICData is nothing or no SIC data exists
    ''                ' Cannot find peaks for this parent ion
    ''                With .ParentIons(intParentIonIndex).SICStats
    ''                    With .Peak
    ''                        .IndexObserved = 0
    ''                        .IndexBaseLeft = .IndexObserved
    ''                        .IndexBaseRight = .IndexObserved
    ''                        .IndexMax = .IndexObserved
    ''                    End With
    ''                End With
    ''            Else
    ''                With .ParentIons(intParentIonIndex).SICStats
    ''                    ' Record the index (of data in .SICData) that the parent ion mass was first observed

    ''                    .Peak.ScanTypeForPeakIndices = eScanTypeConstants.SurveyScan

    ''                    ' Search for intScanIndexObserved in udtSICDetails.SICScanIndices()
    ''                    .Peak.IndexObserved = -1
    ''                    For intSurveyScanIndex = 0 To udtSICDetails.SICDataCount - 1
    ''                        If udtSICDetails.SICScanIndices(intSurveyScanIndex) = intScanIndexObserved Then
    ''                            .Peak.IndexObserved = intSurveyScanIndex
    ''                            Exit For
    ''                        End If
    ''                    Next intSurveyScanIndex

    ''                    If .Peak.IndexObserved = -1 Then
    ''                        ' Match wasn't found; this is unexpected
    ''                        LogErrors("FindSICPeakAndAreaForParentIon", "Programming error: survey scan index not found", Nothing, True, True, True, eMasicErrorCodes.FindSICPeaksError)
    ''                        .Peak.IndexObserved = 0
    ''                    End If

    ''                    ' Populate intSICScanNumbers() with the scan numbers that the SICData corresponds to
    ''                    ' At the same time, populate udtSICStats.SICDataScanIntervals with the scan intervals between each of the data points

    ''                    If udtSICDetails.SICDataCount > udtSICDetails.SICDataScanIntervals.Length Then
    ''                        ReDim udtSICDetails.SICDataScanIntervals(udtSICDetails.SICDataCount - 1)
    ''                    End If

    ''                    ReDim intSICScanNumbers(udtSICDetails.SICDataCount - 1)
    ''                    For intSurveyScanIndex = 0 To udtSICDetails.SICDataCount - 1
    ''                        intSICScanNumbers(intSurveyScanIndex) = udtScanList.SurveyScans(udtSICDetails.SICScanIndices(intSurveyScanIndex)).ScanNumber
    ''                        If intSurveyScanIndex > 0 Then
    ''                            intScanDelta = intSICScanNumbers(intSurveyScanIndex) - intSICScanNumbers(intSurveyScanIndex - 1)
    ''                            udtSICDetails.SICDataScanIntervals(intSurveyScanIndex) = CByte(Math.Min(Byte.MaxValue, intScanDelta))        ' Make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
    ''                        End If
    ''                    Next intSurveyScanIndex

    ''                    ' Record the fragmentation scan number
    ''                    intFragScanNumber = udtScanList.FragScans(udtScanList.ParentIons(intParentIonIndex).FragScanIndices(0)).ScanNumber

    ''                    ' Determine the value for .ParentIonIntensity
    ''                    blnSuccess = mMASICPeakFinder.ComputeParentIonIntensity(udtSICDetails.SICDataCount, intSICScanNumbers, udtSICDetails.SICData, .Peak, intFragScanNumber)

    ''                    blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(udtSICDetails.SICDataCount, intSICScanNumbers, udtSICDetails.SICData, .SICPotentialAreaStatsForPeak, .Peak,
    ''                                                                     udtSmoothedYDataSubset, udtSICOptions.SICPeakFinderOptions,
    ''                                                                     udtSICPotentialAreaStatsForRegion,
    ''                                                                     Not blnCustomSICPeak, udtScanList.SIMDataPresent, RECOMPUTE_NOISE_LEVEL)

    ''                    If blnSuccess Then
    ''                        ' Record the survey scan indices of the peak max, start, and end
    ''                        ' Note that .ScanTypeForPeakIndices was set earlier in this function
    ''                        .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
    ''                        .PeakScanIndexStart = udtSICDetails.SICScanIndices(.Peak.IndexBaseLeft)
    ''                        .PeakScanIndexEnd = udtSICDetails.SICScanIndices(.Peak.IndexBaseRight)
    ''                    Else
    ''                        ' No peak found
    ''                        .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
    ''                        .PeakScanIndexStart = .PeakScanIndexMax
    ''                        .PeakScanIndexEnd = .PeakScanIndexMax

    ''                        With .Peak
    ''                            .MaxIntensityValue = udtSICDetails.SICData(.IndexMax)
    ''                            .IndexBaseLeft = .IndexMax
    ''                            .IndexBaseRight = .IndexMax
    ''                            .FWHMScanWidth = 1
    ''                            ' Assign the intensity of the peak at the observed maximum to the area
    ''                            .Area = .MaxIntensityValue

    ''                            .SignalToNoiseRatio = mMASICPeakFinder.ComputeSignalToNoise(.MaxIntensityValue, .BaselineNoiseStats.NoiseLevel)
    ''                        End With
    ''                    End If
    ''                End With

    ''                ' Update .OptimalPeakApexScanNumber
    ''                ' Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
    ''                With .ParentIons(intParentIonIndex)
    ''                    .OptimalPeakApexScanNumber = udtScanList.SurveyScans(udtSICDetails.SICScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
    ''                End With

    ''            End If

    ''        End With

    ''        blnSuccess = True

    ''    Catch ex As Exception
    ''        LogErrors("FindSICPeakAndAreaForParentIon", "Error finding SIC peaks and their areas", ex, True, False, True, eMasicErrorCodes.FindSICPeaksError)
    ''        blnSuccess = False
    ''    End Try

    ''    Return blnSuccess

    ''End Function

    ''' <summary>
    ''' Looks for the reporter ion peaks using FindReporterIonsWork
    ''' </summary>
    ''' <param name="udtSICOptions"></param>
    ''' <param name="udtScanList"></param>
    ''' <param name="objSpectraCache"></param>
    ''' <param name="strInputFileName"></param>
    ''' <param name="strOutputFolderPath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function FindReporterIons(
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean

        Const cColDelimiter As Char = ControlChars.Tab

        Dim strOutputFilePath = "??"

        Dim srOutFile As StreamWriter

        Dim intMasterOrderIndex As Integer
        Dim intScanPointer As Integer

        Dim udtReporterIonInfo() As udtReporterIonInfoType

        Dim strHeaderLine As String
        Dim strMZValue As String

        Dim strObsMZHeaders As String
        Dim strUncorrectedIntensityHeaders As String

        Dim intReporterIonIndex As Integer
        Dim blnSuccess As Boolean
        Dim blnSaveUncorrectedIntensities As Boolean

        Try

            If mReporterIonCount = 0 Then
                ' No reporter ions defined; default to ITraq
                SetReporterIonMassMode(eReporterIonMassModeConstants.ITraqFourMZ)
            End If

            ' Populate udtReporterIonInfo
            ReDim udtReporterIonInfo(mReporterIonCount - 1)
            For intReporterIonIndex = 0 To mReporterIonCount - 1
                udtReporterIonInfo(intReporterIonIndex) = mReporterIonInfo(intReporterIonIndex)
            Next intReporterIonIndex

            Array.Sort(udtReporterIonInfo, New clsReportIonInfoComparer)

            strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ReporterIonsFile)
            srOutFile = New StreamWriter(strOutputFilePath)

            ' Write the file headers
            Dim reporterIonMZsUnique = New SortedSet(Of String)
            strHeaderLine = "Dataset" & cColDelimiter & "ScanNumber" & cColDelimiter & "Collision Mode" & cColDelimiter & "ParentIonMZ" & cColDelimiter & "BasePeakIntensity" & cColDelimiter & "BasePeakMZ" & cColDelimiter & "ReporterIonIntensityMax"

            strObsMZHeaders = String.Empty
            strUncorrectedIntensityHeaders = String.Empty

            If mReporterIonApplyAbundanceCorrection AndAlso mReporterIonSaveUncorrectedIntensities Then
                blnSaveUncorrectedIntensities = True
            Else
                blnSaveUncorrectedIntensities = False
            End If

            For intReporterIonIndex = 0 To udtReporterIonInfo.Length - 1

                If Not udtReporterIonInfo(intReporterIonIndex).ContaminantIon OrElse blnSaveUncorrectedIntensities Then
                    ' Contruct the reporter ion intensity header
                    ' We skip contaminant ions, unless blnSaveUncorrectedIntensities is True, then we include them

                    If (mReporterIonMassMode = eReporterIonMassModeConstants.TMTTenMZ) Then
                        strMZValue = udtReporterIonInfo(intReporterIonIndex).MZ.ToString("#0.000")
                    Else
                        strMZValue = CInt(udtReporterIonInfo(intReporterIonIndex).MZ).ToString
                    End If

                    If reporterIonMZsUnique.Contains(strMZValue) Then
                        ' Uniquify the m/z value
                        strMZValue &= "_" & intReporterIonIndex.ToString()
                    End If

                    Try
                        reporterIonMZsUnique.Add(strMZValue)
                    Catch ex As Exception
                        ' Error updating the sortedset; 
                        ' this shouldn't happen based on the .ContainsKey test above
                    End Try

                    ' Append the reporter ion intensity title to the headers
                    strHeaderLine &= cColDelimiter & "Ion_" & strMZValue

                    If mReporterIonSaveObservedMasses Then
                        strObsMZHeaders &= cColDelimiter & "Ion_" & strMZValue & "_ObsMZ"
                    End If

                    If blnSaveUncorrectedIntensities Then
                        strUncorrectedIntensityHeaders &= cColDelimiter & "Ion_" & strMZValue & "_OriginalIntensity"
                    End If
                End If

            Next intReporterIonIndex

            strHeaderLine &= cColDelimiter & "Weighted Avg Pct Intensity Correction"

            If mReporterIonSaveObservedMasses Then
                strHeaderLine &= strObsMZHeaders
            End If

            If blnSaveUncorrectedIntensities Then
                strHeaderLine &= strUncorrectedIntensityHeaders
            End If

            srOutFile.WriteLine(strHeaderLine)


            SetSubtaskProcessingStepPct(0, "Searching for reporter ions")

            For intMasterOrderIndex = 0 To udtScanList.MasterScanOrderCount - 1
                intScanPointer = udtScanList.MasterScanOrder(intMasterOrderIndex).ScanIndexPointer
                If udtScanList.MasterScanOrder(intMasterOrderIndex).ScanType = eScanTypeConstants.SurveyScan Then
                    ' Skip Survey Scans
                Else

                    FindReporterIonsWork(
                       udtSICOptions,
                       udtScanList,
                       objSpectraCache,
                       udtScanList.FragScans(intScanPointer),
                       srOutFile,
                       udtReporterIonInfo,
                       cColDelimiter,
                       blnSaveUncorrectedIntensities)

                End If

                If udtScanList.MasterScanOrderCount > 1 Then
                    SetSubtaskProcessingStepPct(CShort(intMasterOrderIndex / (udtScanList.MasterScanOrderCount - 1) * 100))
                Else
                    SetSubtaskProcessingStepPct(0)
                End If

                UpdateOverallProgress(objSpectraCache)
                If mAbortProcessing Then
                    Exit For
                End If

            Next intMasterOrderIndex

            srOutFile.Close()

            blnSuccess = True

        Catch ex As Exception
            LogErrors("FindReporterIons", "Error writing the reporter ions to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    ''' <summary>
    ''' Looks for the reporter ion m/z values, +/- a tolerance
    ''' Calls AggregateIonsInRange with blnReturnMax = True, meaning we're reporting the maximum ion abundance for each reporter ion m/z
    ''' </summary>
    ''' <param name="udtSICOptions"></param>
    ''' <param name="udtScanList"></param>
    ''' <param name="objSpectraCache"></param>
    ''' <param name="udtScan"></param>
    ''' <param name="srOutFile"></param>
    ''' <param name="udtReporterIonInfo"></param>
    ''' <param name="cColDelimiter"></param>
    ''' <param name="blnSaveUncorrectedIntensities"></param>
    ''' <remarks></remarks>
    Private Sub FindReporterIonsWork(
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtScan As udtScanInfoType,
      ByRef srOutFile As StreamWriter,
      ByRef udtReporterIonInfo() As udtReporterIonInfoType,
      cColDelimiter As Char,
      blnSaveUncorrectedIntensities As Boolean)

        Const USE_MAX_ABUNDANCE_IN_WINDOW = True

        Static objITraqIntensityCorrector As New clsITraqIntensityCorrection(eReporterIonMassModeConstants.ITraqEightMZHighRes, clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex.ABSciex)

        Dim intReporterIonIndex As Integer
        Dim intPoolIndex As Integer

        Dim intIonMatchCount As Integer

        Dim dblParentIonMZ As Double
        Dim strOutLine As String
        Dim strReporterIntensityList As String
        Dim strObsMZList As String
        Dim strUncorrectedIntensityList As String

        Dim sngReporterIntensities() As Single
        Dim sngReporterIntensitiesCorrected() As Single
        Dim dblClosestMZ() As Double

        Dim sngReporterIntensityMax As Single

        Dim intPositiveCount As Integer

        Dim dblPctChange As Double
        Dim dblPctChangeSum As Double
        Dim dblOriginalIntensitySum As Double

        ' The following will be a value between 0 and 100
        ' Using Absolute Value of percent change to avoid averaging both negative and positive values
        Dim sngWeightedAvgPctIntensityCorrection As Single

        If udtScan.FragScanInfo.ParentIonInfoIndex >= 0 AndAlso udtScan.FragScanInfo.ParentIonInfoIndex < udtScanList.ParentIons.Count Then
            dblParentIonMZ = udtScanList.ParentIons(udtScan.FragScanInfo.ParentIonInfoIndex).MZ
        Else
            dblParentIonMZ = 0
        End If

        If Not objSpectraCache.ValidateSpectrumInPool(udtScan.ScanNumber, intPoolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        ' Initialize the arrays used to track the observed reporter ion values
        ReDim sngReporterIntensities(udtReporterIonInfo.Length - 1)
        ReDim sngReporterIntensitiesCorrected(udtReporterIonInfo.Length - 1)
        ReDim dblClosestMZ(udtReporterIonInfo.Length - 1)

        ' Initialize the output variables
        strOutLine = udtSICOptions.DatasetNumber.ToString & cColDelimiter &
         udtScan.ScanNumber.ToString & cColDelimiter &
         udtScan.FragScanInfo.CollisionMode & cColDelimiter &
         Math.Round(dblParentIonMZ, 2).ToString & cColDelimiter &
         Math.Round(udtScan.BasePeakIonIntensity, 2).ToString & cColDelimiter &
         Math.Round(udtScan.BasePeakIonMZ, 4).ToString

        strReporterIntensityList = String.Empty
        strObsMZList = String.Empty
        strUncorrectedIntensityList = String.Empty
        sngReporterIntensityMax = 0


        ' Find the reporter ion intensities
        ' Also keep track of the closest m/z for each reporter ion
        ' Note that we're using the maximum intensity in the range (not the sum)
        For intReporterIonIndex = 0 To udtReporterIonInfo.Length - 1

            With udtReporterIonInfo(intReporterIonIndex)
                ' Search for the reporter ion MZ in this mass spectrum
                sngReporterIntensities(intReporterIonIndex) = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex),
                 .MZ,
                 .MZToleranceDa,
                 intIonMatchCount,
                 dblClosestMZ(intReporterIonIndex),
                 USE_MAX_ABUNDANCE_IN_WINDOW)
            End With
        Next intReporterIonIndex

        ' Populate sngReporterIntensitiesCorrected with the data in sngReporterIntensities
        Array.Copy(sngReporterIntensities, sngReporterIntensitiesCorrected, sngReporterIntensities.Length)

        If mReporterIonApplyAbundanceCorrection Then

            If mReporterIonMassMode = eReporterIonMassModeConstants.ITraqFourMZ Or
               mReporterIonMassMode = eReporterIonMassModeConstants.ITraqEightMZHighRes Or
               mReporterIonMassMode = eReporterIonMassModeConstants.ITraqEightMZLowRes Then

                ' Correct the reporter ion intensities using the ITraq Intensity Corrector class

                If objITraqIntensityCorrector.ITraqMode <> mReporterIonMassMode OrElse
                   objITraqIntensityCorrector.ITraq4PlexCorrectionFactorType <> mReporterIonITraq4PlexCorrectionFactorType Then
                    objITraqIntensityCorrector.UpdateITraqMode(mReporterIonMassMode, mReporterIonITraq4PlexCorrectionFactorType)
                End If

                ' Make sure at least one of two of the points in sngReporterIntensitiesCorrected() is non-zero
                ' If not, then no correction can be applied
                intPositiveCount = 0
                For intReporterIonIndex = 0 To udtReporterIonInfo.Length - 1
                    If sngReporterIntensitiesCorrected(intReporterIonIndex) > 0 Then
                        intPositiveCount += 1
                    End If
                Next

                If intPositiveCount >= 2 Then
                    objITraqIntensityCorrector.ApplyCorrection(sngReporterIntensitiesCorrected)
                End If

            End If

        End If


        ' Now construct the string of intensity values, delimited by cColDelimiter
        ' Will also compute the percent change in intensities

        ' Initialize the variables used to compute the weighted average percent change
        dblPctChangeSum = 0
        dblOriginalIntensitySum = 0

        For intReporterIonIndex = 0 To udtReporterIonInfo.Length - 1

            If Not udtReporterIonInfo(intReporterIonIndex).ContaminantIon Then
                ' Update the PctChange variables and the IntensityMax variable only if this is not a Contaminant Ion

                dblOriginalIntensitySum += sngReporterIntensities(intReporterIonIndex)

                If sngReporterIntensities(intReporterIonIndex) > 0 Then
                    ' Compute the percent change, then update dblPctChangeSum
                    dblPctChange = (sngReporterIntensitiesCorrected(intReporterIonIndex) - sngReporterIntensities(intReporterIonIndex)) / sngReporterIntensities(intReporterIonIndex)

                    ' Using Absolute Value here to prevent negative changes from cancelling out positive changes
                    dblPctChangeSum += Math.Abs(dblPctChange * sngReporterIntensities(intReporterIonIndex))
                End If

                If sngReporterIntensitiesCorrected(intReporterIonIndex) > sngReporterIntensityMax Then
                    sngReporterIntensityMax = sngReporterIntensitiesCorrected(intReporterIonIndex)
                End If
            End If

            If Not udtReporterIonInfo(intReporterIonIndex).ContaminantIon OrElse blnSaveUncorrectedIntensities Then
                ' Append the reporter ion intensity to strReporterIntensityList
                ' We skip contaminant ions, unless blnSaveUncorrectedIntensities is True, then we include them

                strReporterIntensityList &= cColDelimiter & Math.Round(sngReporterIntensitiesCorrected(intReporterIonIndex), 2).ToString

                If mReporterIonSaveObservedMasses Then
                    ' Append the observed reporter mass value to strObsMZList
                    strObsMZList &= cColDelimiter & Math.Round(dblClosestMZ(intReporterIonIndex), 3).ToString
                End If


                If blnSaveUncorrectedIntensities Then
                    ' Append the original, uncorrected intensity value
                    strUncorrectedIntensityList &= cColDelimiter & Math.Round(sngReporterIntensities(intReporterIonIndex), 2).ToString
                End If

            End If

        Next

        ' Compute the weighted average percent intensity correction value
        If dblOriginalIntensitySum > 0 Then
            sngWeightedAvgPctIntensityCorrection = CSng(dblPctChangeSum / dblOriginalIntensitySum * 100)
        Else
            sngWeightedAvgPctIntensityCorrection = 0
        End If

        ' Append the maximum reporter ion intensity then the individual reporter ion intensities
        strOutLine &= cColDelimiter & Math.Round(sngReporterIntensityMax, 2).ToString & strReporterIntensityList

        ' Append the weighted average percent intensity correction
        If sngWeightedAvgPctIntensityCorrection < Single.Epsilon Then
            strOutLine &= cColDelimiter & "0"
        Else
            strOutLine &= cColDelimiter & Math.Round(sngWeightedAvgPctIntensityCorrection, 1).ToString
        End If

        If mReporterIonSaveObservedMasses Then
            strOutLine &= strObsMZList
        End If

        If blnSaveUncorrectedIntensities Then
            strOutLine &= strUncorrectedIntensityList
        End If

        srOutFile.WriteLine(strOutLine)
    End Sub

    Private Function FindSimilarParentIons(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      udtSICOptions As udtSICOptionsType,
      udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      ByRef intIonUpdateCount As Integer) As Boolean

        ' Look for parent ions that have similar m/z values and are nearby one another in time
        ' For the groups of similar ions, assign the scan number of the highest intensity parent ion to the other similar parent ions

        Dim intMatchIndex As Integer
        Dim intParentIonIndex As Integer
        Dim intOriginalIndex As Integer

        Dim dblMZList() As Double                   ' Original m/z values, rounded to 2 decimal places

        Dim dblCurrentMZ As Double

        Dim intFindSimilarIonsDataCount As Integer

        Dim udtFindSimilarIonsData As udtFindSimilarIonsDataType
        Dim intIntensityPointerArray() As Integer
        Dim sngIntensityList() As Single

        Dim intIonInUseCountOriginal As Integer

        Dim intUniqueMZIndex As Integer

        Dim objSearchRange As clsSearchRange

        Dim dtLastLogTime As DateTime

        Dim blnIncludeParentIon As Boolean
        Dim blnSuccess As Boolean

        Try
            intIonUpdateCount = 0

            If udtScanList.ParentIonInfoCount <= 0 Then
                If mSuppressNoParentIonsError Then
                    Return True
                Else
                    Return False
                End If
            End If

            Console.Write("Finding similar parent ions ")
            LogMessage("Finding similar parent ions")
            SetSubtaskProcessingStepPct(0, "Finding similar parent ions")

            With udtScanList
                ' Populate udtFindSimilarIonsData.MZPointerArray and dblMZList, plus intIntensityPointerArray and sngIntensityList()
                ReDim udtFindSimilarIonsData.MZPointerArray(.ParentIonInfoCount - 1)
                ReDim udtFindSimilarIonsData.IonUsed(.ParentIonInfoCount - 1)

                ReDim dblMZList(.ParentIonInfoCount - 1)
                ReDim intIntensityPointerArray(.ParentIonInfoCount - 1)
                ReDim sngIntensityList(.ParentIonInfoCount - 1)

                intFindSimilarIonsDataCount = 0
                For intParentIonIndex = 0 To .ParentIonInfoCount - 1
                    If .ParentIons(intParentIonIndex).MRMDaughterMZ > 0 Then
                        blnIncludeParentIon = False
                    Else
                        If Me.LimitSearchToCustomMZList Then
                            blnIncludeParentIon = .ParentIons(intParentIonIndex).CustomSICPeak
                        Else
                            blnIncludeParentIon = True
                        End If
                    End If

                    If blnIncludeParentIon Then
                        udtFindSimilarIonsData.MZPointerArray(intFindSimilarIonsDataCount) = intParentIonIndex
                        dblMZList(intFindSimilarIonsDataCount) = Math.Round(.ParentIons(intParentIonIndex).MZ, 2)

                        intIntensityPointerArray(intFindSimilarIonsDataCount) = intParentIonIndex
                        sngIntensityList(intFindSimilarIonsDataCount) = .ParentIons(intParentIonIndex).SICStats.Peak.MaxIntensityValue
                        intFindSimilarIonsDataCount += 1
                    End If
                Next intParentIonIndex

                If udtFindSimilarIonsData.MZPointerArray.Length <> intFindSimilarIonsDataCount AndAlso intFindSimilarIonsDataCount > 0 Then
                    ReDim Preserve udtFindSimilarIonsData.MZPointerArray(intFindSimilarIonsDataCount - 1)
                    ReDim Preserve dblMZList(intFindSimilarIonsDataCount - 1)
                    ReDim Preserve intIntensityPointerArray(intFindSimilarIonsDataCount - 1)
                    ReDim Preserve sngIntensityList(intFindSimilarIonsDataCount - 1)
                End If
            End With

            If intFindSimilarIonsDataCount = 0 Then
                If mSuppressNoParentIonsError Then
                    Return True
                Else
                    If udtScanList.MRMDataPresent Then
                        Return True
                    Else
                        Return False
                    End If
                End If
            End If

            LogMessage("FindSimilarParentIons: Sorting the mz arrays")

            ' Sort the MZ arrays
            Array.Sort(dblMZList, udtFindSimilarIonsData.MZPointerArray)

            LogMessage("FindSimilarParentIons: Populate objSearchRange")

            ' Populate objSearchRange
            objSearchRange = New clsSearchRange

            ' Set to false to prevent sorting the input array when calling .FillWithData (saves memory)
            ' Array was already above
            objSearchRange.UsePointerIndexArray = False

            blnSuccess = objSearchRange.FillWithData(dblMZList)

            LogMessage("FindSimilarParentIons: Sort the intensity arrays")

            ' Sort the Intensity arrays
            Array.Sort(sngIntensityList, intIntensityPointerArray)

            ' Reverse the order of intIntensityPointerArray so that it is ordered from the most intense to the least intense ion
            ' Note: We don't really need to reverse sngIntensityList since we're done using it, but 
            ' it doesn't take long, it won't hurt, and it will keep sngIntensityList sync'd with intIntensityPointerArray
            Array.Reverse(intIntensityPointerArray)
            Array.Reverse(sngIntensityList)


            ' Initialize udtUniqueMZList
            ' Pre-reserve enough space for intFindSimilarIonsDataCount entries to avoid repeated use of Redim Preserve
            udtFindSimilarIonsData.UniqueMZListCount = 0
            ReDim udtFindSimilarIonsData.UniqueMZList(intFindSimilarIonsDataCount - 1)

            ' Initialize the .UniqueMZList().MatchIndices() arrays
            InitializeUniqueMZListMatchIndices(udtFindSimilarIonsData.UniqueMZList, 0, intFindSimilarIonsDataCount - 1)

            LogMessage("FindSimilarParentIons: Look for similar parent ions by using m/z and scan")
            dtLastLogTime = DateTime.UtcNow

            ' Look for similar parent ions by using m/z and scan
            ' Step through the ions by decreasing intensity
            intParentIonIndex = 0
            Do
                intOriginalIndex = intIntensityPointerArray(intParentIonIndex)
                If udtFindSimilarIonsData.IonUsed(intOriginalIndex) Then
                    ' Parent ion was already used; move onto the next one
                    intParentIonIndex += 1
                Else
                    With udtFindSimilarIonsData
                        If .UniqueMZListCount >= udtFindSimilarIonsData.UniqueMZList.Length Then
                            ReDim Preserve .UniqueMZList(.UniqueMZListCount + 100)

                            ' Initialize the .UniqueMZList().MatchIndices() arrays
                            InitializeUniqueMZListMatchIndices(udtFindSimilarIonsData.UniqueMZList, .UniqueMZListCount, .UniqueMZList.Length - 1)

                        End If
                        AppendParentIonToUniqueMZEntry(udtScanList, intOriginalIndex, .UniqueMZList(.UniqueMZListCount), 0)
                        .UniqueMZListCount += 1

                        .IonUsed(intOriginalIndex) = True
                        .IonInUseCount = 1
                    End With

                    ' Look for other parent ions with m/z values in tolerance (must be within mass tolerance and scan tolerance)
                    ' If new values are added, then repeat the search using the updated udtUniqueMZList().MZAvg value
                    Do
                        intIonInUseCountOriginal = udtFindSimilarIonsData.IonInUseCount
                        With udtFindSimilarIonsData
                            dblCurrentMZ = .UniqueMZList(.UniqueMZListCount - 1).MZAvg
                        End With

                        If dblCurrentMZ > 0 Then
                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)

                            ' Look for similar 1+ spaced m/z values
                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 1, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)
                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -1, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)

                            ' Look for similar 2+ spaced m/z values
                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0.5, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)
                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -0.5, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)

                            Dim parentIonToleranceDa = GetParentIonToleranceDa(udtSICOptions, dblCurrentMZ)

                            If parentIonToleranceDa <= 0.25 AndAlso udtSICOptions.SimilarIonMZToleranceHalfWidth <= 0.15 Then
                                ' Also look for similar 3+ spaced m/z values
                                FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0.666, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)
                                FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0.333, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)
                                FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -0.333, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)
                                FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -0.666, intOriginalIndex, udtScanList, udtFindSimilarIonsData, udtSICOptions, udtBinningOptions, objSearchRange)
                            End If

                        End If
                    Loop While udtFindSimilarIonsData.IonInUseCount > intIonInUseCountOriginal

                    intParentIonIndex += 1
                End If

                If intFindSimilarIonsDataCount > 1 Then
                    If intParentIonIndex Mod 100 = 0 Then
                        SetSubtaskProcessingStepPct(CShort(intParentIonIndex / (intFindSimilarIonsDataCount - 1) * 100))
                    End If
                Else
                    SetSubtaskProcessingStepPct(1)
                End If

                UpdateOverallProgress(objSpectraCache)
                If mAbortProcessing Then
                    udtScanList.ProcessingIncomplete = True
                    Exit Do
                End If

                UpdateStatusFile()

                If intParentIonIndex Mod 100 = 0 Then
                    If DateTime.UtcNow.Subtract(dtLastLogTime).TotalSeconds >= 10 OrElse intParentIonIndex Mod 500 = 0 Then
                        LogMessage("Parent Ion Index: " & intParentIonIndex.ToString)
                        Console.Write(".")
                        dtLastLogTime = DateTime.UtcNow
                    End If
                End If

            Loop While intParentIonIndex < intFindSimilarIonsDataCount

            Console.WriteLine()

            ' Shrink the .UniqueMZList array to the appropriate length
            ReDim Preserve udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1)

            LogMessage("FindSimilarParentIons: Update the scan numbers for the unique ions")

            ' Update the optimal peak apex scan numbers for the unique ions
            intIonUpdateCount = 0
            For intUniqueMZIndex = 0 To udtFindSimilarIonsData.UniqueMZListCount - 1
                With udtFindSimilarIonsData.UniqueMZList(intUniqueMZIndex)
                    For intMatchIndex = 0 To .MatchCount - 1
                        intParentIonIndex = .MatchIndices(intMatchIndex)

                        If udtScanList.ParentIons(intParentIonIndex).MZ > 0 Then
                            If udtScanList.ParentIons(intParentIonIndex).OptimalPeakApexScanNumber <> .ScanNumberMaxIntensity Then
                                intIonUpdateCount += 1
                                udtScanList.ParentIons(intParentIonIndex).OptimalPeakApexScanNumber = .ScanNumberMaxIntensity
                                udtScanList.ParentIons(intParentIonIndex).PeakApexOverrideParentIonIndex = .ParentIonIndexMaxIntensity
                            End If
                        End If
                    Next intMatchIndex
                End With
            Next intUniqueMZIndex

            blnSuccess = True

        Catch ex As Exception
            LogErrors("FindSimilarParentIons", "Error in FindSimilarParentIons", ex, True, False, eMasicErrorCodes.FindSimilarParentIonsError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

    Private Sub FindSimilarParentIonsWork(
      objSpectraCache As clsSpectraCache,
      dblSearchMZ As Double,
      dblSearchMZOffset As Double,
      intOriginalIndex As Integer,
      ByRef udtScanList As udtScanListType,
      ByRef udtFindSimilarIonsData As udtFindSimilarIonsDataType,
      udtSICOptions As udtSICOptionsType,
      udtBinningOptions As clsCorrelation.udtBinningOptionsType,
      ByRef objSearchRange As clsSearchRange)

        Dim intMatchIndex As Integer
        Dim intMatchOriginalIndex As Integer
        Dim sngTimeDiff As Single
        Dim intIndexFirst As Integer
        Dim intIndexLast As Integer

        If objSearchRange.FindValueRange(dblSearchMZ + dblSearchMZOffset, udtSICOptions.SimilarIonMZToleranceHalfWidth, intIndexFirst, intIndexLast) Then

            For intMatchIndex = intIndexFirst To intIndexLast
                ' See if the matches are unused and within the scan tolerance
                intMatchOriginalIndex = udtFindSimilarIonsData.MZPointerArray(intMatchIndex)

                If Not udtFindSimilarIonsData.IonUsed(intMatchOriginalIndex) Then
                    With udtScanList.ParentIons(intMatchOriginalIndex)

                        If .SICStats.ScanTypeForPeakIndices = eScanTypeConstants.FragScan Then
                            If udtScanList.FragScans(.SICStats.PeakScanIndexMax).ScanTime < Single.Epsilon AndAlso
                              udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity < Single.Epsilon Then
                                ' Both elution times are 0; instead of computing the difference in scan time, compute the difference in scan number, then convert to minutes assuming the acquisition rate is 1 Hz (which is obviously a big assumption)
                                sngTimeDiff = CSng(Math.Abs(udtScanList.FragScans(.SICStats.PeakScanIndexMax).ScanNumber - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanNumberMaxIntensity) / 60.0)
                            Else
                                sngTimeDiff = Math.Abs(udtScanList.FragScans(.SICStats.PeakScanIndexMax).ScanTime - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity)
                            End If
                        Else
                            If udtScanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanTime < Single.Epsilon AndAlso
                              udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity < Single.Epsilon Then
                                ' Both elution times are 0; instead of computing the difference in scan time, compute the difference in scan number, then convert to minutes assuming the acquisition rate is 1 Hz (which is obviously a big assumption)
                                sngTimeDiff = CSng(Math.Abs(udtScanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanNumber - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanNumberMaxIntensity) / 60.0)
                            Else
                                sngTimeDiff = Math.Abs(udtScanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanTime - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity)
                            End If
                        End If

                        If sngTimeDiff <= udtSICOptions.SimilarIonToleranceHalfWidthMinutes Then
                            ' Match is within m/z and time difference; see if the fragmentation spectra patterns are similar

                            Dim similarityScore = CompareFragSpectraForParentIons(udtScanList, objSpectraCache, intOriginalIndex, intMatchOriginalIndex, udtBinningOptions, udtSICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions)

                            If similarityScore > udtSICOptions.SpectrumSimilarityMinimum Then
                                ' Yes, the spectra are similar
                                ' Add this parent ion to udtUniqueMZList(intUniqueMZListCount - 1)
                                With udtFindSimilarIonsData
                                    AppendParentIonToUniqueMZEntry(udtScanList, intMatchOriginalIndex, .UniqueMZList(.UniqueMZListCount - 1), dblSearchMZOffset)
                                End With
                                udtFindSimilarIonsData.IonUsed(intMatchOriginalIndex) = True
                                udtFindSimilarIonsData.IonInUseCount += 1
                            End If
                        End If
                    End With
                End If
            Next intMatchIndex
        End If

    End Sub

    Private Function GenerateMRMInfoHash(udtMRMScanInfo As udtMRMScanInfoType) As String
        Dim strHash As String
        Dim intIndex As Integer

        With udtMRMScanInfo
            strHash = .ParentIonMZ & "_" & .MRMMassCount

            For intIndex = 0 To .MRMMassCount - 1
                strHash &= "_" &
                  .MRMMassList(intIndex).CentralMass.ToString("0.000") & "_" &
                  .MRMMassList(intIndex).StartMass.ToString("0.000") & "_" &
                  .MRMMassList(intIndex).EndMass.ToString("0.000")

            Next intIndex
        End With

        Return strHash

    End Function

    Public Overrides Function GetDefaultExtensionsToParse() As String()
        Dim strExtensionsToParse(5) As String

        strExtensionsToParse(0) = FINNIGAN_RAW_FILE_EXTENSION
        strExtensionsToParse(1) = MZ_XML_FILE_EXTENSION1
        strExtensionsToParse(2) = MZ_XML_FILE_EXTENSION2
        strExtensionsToParse(3) = MZ_DATA_FILE_EXTENSION1
        strExtensionsToParse(4) = MZ_DATA_FILE_EXTENSION2
        strExtensionsToParse(5) = AGILENT_MSMS_FILE_EXTENSION

        Return strExtensionsToParse
    End Function

    Public Overrides Function GetErrorMessage() As String
        ' Returns String.Empty if no error

        Dim strErrorMessage As String

        If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError OrElse
           MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError Then
            Select Case mLocalErrorCode
                Case eMasicErrorCodes.NoError
                    strErrorMessage = String.Empty
                Case eMasicErrorCodes.InvalidDatasetLookupFilePath
                    strErrorMessage = "Invalid dataset lookup file path"
                Case eMasicErrorCodes.UnknownFileExtension
                    strErrorMessage = "Unknown file extension"
                Case eMasicErrorCodes.InputFileAccessError
                    strErrorMessage = "Input file access error"
                Case eMasicErrorCodes.InvalidDatasetNumber
                    strErrorMessage = "Invalid dataset number"
                Case eMasicErrorCodes.CreateSICsError
                    strErrorMessage = "Create SIC's error"
                Case eMasicErrorCodes.FindSICPeaksError
                    strErrorMessage = "Error finding SIC peaks"
                Case eMasicErrorCodes.InvalidCustomSICValues
                    strErrorMessage = "Invalid custom SIC values"
                Case eMasicErrorCodes.NoParentIonsFoundInInputFile
                    strErrorMessage = "No parent ions were found in the input file (additionally, no custom SIC values were defined)"
                Case eMasicErrorCodes.NoSurveyScansFoundInInputFile
                    strErrorMessage = "No survey scans were found in the input file (do you have a Scan Range filter defined?)"
                Case eMasicErrorCodes.FindSimilarParentIonsError
                    strErrorMessage = "Find similar parent ions error"
                Case eMasicErrorCodes.FindSimilarParentIonsError
                    strErrorMessage = "Find similar parent ions error"
                Case eMasicErrorCodes.InputFileDataReadError
                    strErrorMessage = "Error reading data from input file"
                Case eMasicErrorCodes.OutputFileWriteError
                    strErrorMessage = "Error writing data to output file"
                Case eMasicErrorCodes.FileIOPermissionsError
                    strErrorMessage = "File IO Permissions Error"
                Case eMasicErrorCodes.ErrorCreatingSpectrumCacheFolder
                    strErrorMessage = "Error creating spectrum cache folder"
                Case eMasicErrorCodes.ErrorCachingSpectrum
                    strErrorMessage = "Error caching spectrum"
                Case eMasicErrorCodes.ErrorUncachingSpectrum
                    strErrorMessage = "Error uncaching spectrum"
                Case eMasicErrorCodes.ErrorDeletingCachedSpectrumFiles
                    strErrorMessage = "Error deleting cached spectrum files"
                Case eMasicErrorCodes.InvalidCustomSICHeaders
                    strErrorMessage = "Invalid custom SIC list file headers"
                Case eMasicErrorCodes.UnspecifiedError
                    strErrorMessage = "Unspecified localized error"
                Case Else
                    ' This shouldn't happen
                    strErrorMessage = "Unknown error state"
            End Select
        Else
            strErrorMessage = MyBase.GetBaseClassErrorMessage()
        End If

        Return strErrorMessage

    End Function

    Private Function GetFilePathPrefixChar() As String
        If Me.ShowMessages Then
            Return ControlChars.NewLine
        Else
            Return ": "
        End If
    End Function

    Private Function GetFreeMemoryMB() As Single
        ' Returns the amount of free memory, in MB

        If mFreeMemoryPerformanceCounter Is Nothing Then
            Return 0
        Else
            Return mFreeMemoryPerformanceCounter.NextValue()
        End If

    End Function

    Public Shared Function GetCustomMZFileColumnHeaders(
      Optional cColDelimiter As String = ", ",
      Optional blnIncludeAndBeforeLastItem As Boolean = True) As String

        Dim strHeaders As String

        strHeaders = CUSTOM_SIC_COLUMN_MZ & cColDelimiter &
         CUSTOM_SIC_COLUMN_MZ_TOLERANCE & cColDelimiter &
         CUSTOM_SIC_COLUMN_SCAN_CENTER & cColDelimiter &
         CUSTOM_SIC_COLUMN_SCAN_TOLERNACE & cColDelimiter &
         CUSTOM_SIC_COLUMN_SCAN_TIME & cColDelimiter &
         CUSTOM_SIC_COLUMN_TIME_TOLERANCE & cColDelimiter

        If blnIncludeAndBeforeLastItem Then
            strHeaders &= " and "
        End If

        strHeaders &= CUSTOM_SIC_COLUMN_COMMENT

        Return strHeaders

    End Function

    Private Function GetHeadersForOutputFile(ByRef udtScanList As udtScanListType, eOutputFileType As eOutputFileTypeConstants) As String
        Return GetHeadersForOutputFile(udtScanList, eOutputFileType, ControlChars.Tab)
    End Function

    Private Function GetHeadersForOutputFile(ByRef udtScanList As udtScanListType, eOutputFileType As eOutputFileTypeConstants, cColDelimiter As Char) As String
        Dim strHeaders As String
        Dim intNonConstantHeaderIDs() As Integer = Nothing

        Select Case eOutputFileType
            Case eOutputFileTypeConstants.ScanStatsFlatFile
                strHeaders = "Dataset" & cColDelimiter & "ScanNumber" & cColDelimiter & "ScanTime" & cColDelimiter &
                   "ScanType" & cColDelimiter & "TotalIonIntensity" & cColDelimiter & "BasePeakIntensity" & cColDelimiter &
                   "BasePeakMZ" & cColDelimiter & "BasePeakSignalToNoiseRatio" & cColDelimiter &
                   "IonCount" & cColDelimiter & "IonCountRaw" & cColDelimiter & "ScanTypeName"

            Case eOutputFileTypeConstants.ScanStatsExtendedFlatFile

                If Not mExtendedHeaderInfo Is Nothing Then

                    ' Lookup extended stats values that are constants for all scans
                    ' The following will also remove the constant header values from htExtendedHeaderInfo
                    ExtractConstantExtendedHeaderValues(intNonConstantHeaderIDs, udtScanList.SurveyScans, udtScanList.FragScans, cColDelimiter)

                    strHeaders = ConstructExtendedStatsHeaders(cColDelimiter)
                Else
                    strHeaders = String.Empty
                End If

            Case eOutputFileTypeConstants.SICStatsFlatFile
                strHeaders = "Dataset" & cColDelimiter & "ParentIonIndex" & cColDelimiter & "MZ" & cColDelimiter & "SurveyScanNumber" & cColDelimiter & "FragScanNumber" & cColDelimiter & "OptimalPeakApexScanNumber" & cColDelimiter & "PeakApexOverrideParentIonIndex" & cColDelimiter &
                   "CustomSICPeak" & cColDelimiter & "PeakScanStart" & cColDelimiter & "PeakScanEnd" & cColDelimiter & "PeakScanMaxIntensity" & cColDelimiter &
                   "PeakMaxIntensity" & cColDelimiter & "PeakSignalToNoiseRatio" & cColDelimiter & "FWHMInScans" & cColDelimiter & "PeakArea" & cColDelimiter & "ParentIonIntensity" & cColDelimiter &
                   "PeakBaselineNoiseLevel" & cColDelimiter & "PeakBaselineNoiseStDev" & cColDelimiter & "PeakBaselinePointsUsed" & cColDelimiter &
                   "StatMomentsArea" & cColDelimiter & "CenterOfMassScan" & cColDelimiter & "PeakStDev" & cColDelimiter & "PeakSkew" & cColDelimiter & "PeakKSStat" & cColDelimiter & "StatMomentsDataCountUsed"

                If mIncludeScanTimesInSICStatsFile Then
                    strHeaders &= cColDelimiter & "SurveyScanTime" & cColDelimiter & "FragScanTime" & cColDelimiter & "OptimalPeakApexScanTime"
                End If

            Case eOutputFileTypeConstants.MRMSettingsFile
                strHeaders = "Parent_Index" & cColDelimiter & "Parent_MZ" & cColDelimiter & "Daughter_MZ" & cColDelimiter & "MZ_Start" & cColDelimiter & "MZ_End" & cColDelimiter & "Scan_Count"

            Case eOutputFileTypeConstants.MRMDatafile
                strHeaders = "Scan" & cColDelimiter & "MRM_Parent_MZ" & cColDelimiter & "MRM_Daughter_MZ" & cColDelimiter & "MRM_Daughter_Intensity"

            Case Else
                strHeaders = "Unknown header column names"
        End Select

        Return strHeaders
    End Function

    Private Function GetNextSurveyScanIndex(ByRef SurveyScans() As udtScanInfoType, intSurveyScanIndex As Integer) As Integer
        ' Returns the next adjacent survey scan index
        ' If the given survey scan is not a SIM scan, then simply returns the next index
        ' If the given survey scan is a SIM scan, then returns the next survey scan with the same .SIMIndex
        ' If no appropriate next survey scan index is found, then returns intSurveyScanIndex instead

        Dim intNextSurveyScanIndex As Integer
        Dim intSIMIndex As Integer

        Try
            If intSurveyScanIndex < SurveyScans.Length - 1 AndAlso intSurveyScanIndex >= 0 Then
                If SurveyScans(intSurveyScanIndex).SIMScan Then
                    intSIMIndex = SurveyScans(intSurveyScanIndex).SIMIndex

                    intNextSurveyScanIndex = intSurveyScanIndex + 1
                    Do While intNextSurveyScanIndex < SurveyScans.Length
                        If SurveyScans(intNextSurveyScanIndex).SIMIndex = intSIMIndex Then
                            Exit Do
                        Else
                            intNextSurveyScanIndex += 1
                        End If
                    Loop

                    If intNextSurveyScanIndex = SurveyScans.Length Then
                        ' Match was not found
                        intNextSurveyScanIndex = intSurveyScanIndex
                    End If

                    Return intNextSurveyScanIndex
                Else
                    ' Not a SIM Scan
                    Return intSurveyScanIndex + 1
                End If
            Else
                ' intSurveyScanIndex is the final survey scan or is less than 0
                Return intSurveyScanIndex
            End If
        Catch ex As Exception
            ' Error occurred; simply return intSurveyScanIndex
            Return intSurveyScanIndex
        End Try

    End Function

    Private Function GetPreviousSurveyScanIndex(ByRef SurveyScans() As udtScanInfoType, intSurveyScanIndex As Integer) As Integer
        ' Returns the previous adjacent survey scan index
        ' If the given survey scan is not a SIM scan, then simply returns the previous index
        ' If the given survey scan is a SIM scan, then returns the previous survey scan with the same .SIMIndex
        ' If no appropriate next survey scan index is found, then returns intSurveyScanIndex instead

        Dim intPreviousSurveyScanIndex As Integer
        Dim intSIMIndex As Integer

        Try
            If intSurveyScanIndex > 0 AndAlso intSurveyScanIndex < SurveyScans.Length Then
                If SurveyScans(intSurveyScanIndex).SIMScan Then
                    intSIMIndex = SurveyScans(intSurveyScanIndex).SIMIndex

                    intPreviousSurveyScanIndex = intSurveyScanIndex - 1
                    Do While intPreviousSurveyScanIndex >= 0
                        If SurveyScans(intPreviousSurveyScanIndex).SIMIndex = intSIMIndex Then
                            Exit Do
                        Else
                            intPreviousSurveyScanIndex -= 1
                        End If
                    Loop

                    If intPreviousSurveyScanIndex < 0 Then
                        ' Match was not found
                        intPreviousSurveyScanIndex = intSurveyScanIndex
                    End If

                    Return intPreviousSurveyScanIndex
                Else
                    ' Not a SIM Scan
                    Return intSurveyScanIndex - 1
                End If
            Else
                ' intSurveyScanIndex is the first survey scan or is less than 0
                Return intSurveyScanIndex
            End If
        Catch ex As Exception
            ' Error occurred; simply return intSurveyScanIndex
            Return intSurveyScanIndex
        End Try

    End Function

    Private Function GetProcessMemoryUsageMB() As Single

        ' Obtain a handle to the current process
        Dim objProcess As Diagnostics.Process
        objProcess = Diagnostics.Process.GetCurrentProcess()

        ' The WorkingSet is the total physical memory usage 
        Return CSng(objProcess.WorkingSet64 / 1024 / 1024)

    End Function

    Public Shared Function GetDefaultReporterIons(eReporterIonMassMode As eReporterIonMassModeConstants) As udtReporterIonInfoType()
        If eReporterIonMassMode = eReporterIonMassModeConstants.ITraqEightMZHighRes Then
            Return GetDefaultReporterIons(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES)
        Else
            Return GetDefaultReporterIons(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT)
        End If
    End Function

    Public Shared Function GetDefaultReporterIons(
      eReporterIonMassMode As eReporterIonMassModeConstants,
      dblMZToleranceDa As Double) As udtReporterIonInfoType()

        Dim intIndex As Integer
        Dim udtReporterIonInfo() As udtReporterIonInfoType

        Select Case eReporterIonMassMode
            Case eReporterIonMassModeConstants.ITraqFourMZ
                ' ITRAQ
                ReDim udtReporterIonInfo(3)
                udtReporterIonInfo(0).MZ = 114.1112
                udtReporterIonInfo(1).MZ = 115.1083
                udtReporterIonInfo(2).MZ = 116.1116
                udtReporterIonInfo(3).MZ = 117.115

            Case eReporterIonMassModeConstants.ITraqETDThreeMZ
                ' ITRAQ ETD tags
                ReDim udtReporterIonInfo(2)
                udtReporterIonInfo(0).MZ = 101.107
                udtReporterIonInfo(1).MZ = 102.104
                udtReporterIonInfo(2).MZ = 104.1107

            Case eReporterIonMassModeConstants.TMTTwoMZ
                ' TMT duplex Isobaric tags (from Thermo)
                ReDim udtReporterIonInfo(1)
                udtReporterIonInfo(0).MZ = 126.1283
                udtReporterIonInfo(1).MZ = 127.1316

            Case eReporterIonMassModeConstants.TMTSixMZ
                ' TMT sixplex Isobaric tags (from Thermo)
                ' These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                ReDim udtReporterIonInfo(5)                 ' Old values:
                udtReporterIonInfo(0).MZ = 126.127725       ' 126.1283
                udtReporterIonInfo(1).MZ = 127.12476       ' 127.1316
                udtReporterIonInfo(2).MZ = 128.134433       ' 128.135
                udtReporterIonInfo(3).MZ = 129.131468       ' 129.1383
                udtReporterIonInfo(4).MZ = 130.141141       ' 130.1417
                udtReporterIonInfo(5).MZ = 131.138176       ' 131.1387

            Case eReporterIonMassModeConstants.TMTTenMZ
                ' TMT 10-plex Isobaric tags (from Thermo)
                ' These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                ' Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                ReDim udtReporterIonInfo(9)
                udtReporterIonInfo(0).MZ = 126.127725    ' 126.127725	
                udtReporterIonInfo(1).MZ = 127.12476     ' 127.12476
                udtReporterIonInfo(2).MZ = 127.131079    ' 127.131079
                udtReporterIonInfo(3).MZ = 128.128114    ' 128.128114
                udtReporterIonInfo(4).MZ = 128.134433    ' 128.134433
                udtReporterIonInfo(5).MZ = 129.131468    ' 129.131468
                udtReporterIonInfo(6).MZ = 129.137787    ' 129.137787
                udtReporterIonInfo(7).MZ = 130.134822    ' 130.134822
                udtReporterIonInfo(8).MZ = 130.141141    ' 130.141141
                udtReporterIonInfo(9).MZ = 131.138176    ' 131.138176

            Case eReporterIonMassModeConstants.ITraqEightMZHighRes

                ' ITRAQ eight-plex Isobaric tags, Low-Res MS/MS
                ReDim udtReporterIonInfo(7)
                udtReporterIonInfo(0).MZ = 113.107873
                udtReporterIonInfo(1).MZ = 114.111228
                udtReporterIonInfo(2).MZ = 115.108263
                udtReporterIonInfo(3).MZ = 116.111618
                udtReporterIonInfo(4).MZ = 117.114973
                udtReporterIonInfo(5).MZ = 118.112008
                udtReporterIonInfo(6).MZ = 119.115363
                udtReporterIonInfo(7).MZ = 121.122072


            Case eReporterIonMassModeConstants.ITraqEightMZLowRes

                ' ITRAQ eight-plex Isobaric tags, Low-Res MS/MS
                ReDim udtReporterIonInfo(8)
                udtReporterIonInfo(0).MZ = 113.107873
                udtReporterIonInfo(1).MZ = 114.111228
                udtReporterIonInfo(2).MZ = 115.108263
                udtReporterIonInfo(3).MZ = 116.111618
                udtReporterIonInfo(4).MZ = 117.114973
                udtReporterIonInfo(5).MZ = 118.112008
                udtReporterIonInfo(6).MZ = 119.115363

                udtReporterIonInfo(7).MZ = 120.08131   ' This corresponds to immonium ion loss from Phenylalanine (147.06841 - 26.9871 since Immonium is CO minus H)
                udtReporterIonInfo(7).ContaminantIon = True

                udtReporterIonInfo(8).MZ = 121.122072

            Case eReporterIonMassModeConstants.PCGalnaz

                ' Custom reporter ions for Josh Alfaro
                ReDim udtReporterIonInfo(2)
                udtReporterIonInfo(0).MZ = 204.0871934      ' C8H14NO5
                udtReporterIonInfo(1).MZ = 300.130787       ' C11H18N5O5
                udtReporterIonInfo(2).MZ = 503.2101566      ' C19H31N6O10

            Case eReporterIonMassModeConstants.HemeCFragment

                ' Custom reporter ions for Eric Merkley
                ReDim udtReporterIonInfo(1)
                udtReporterIonInfo(0).MZ = 616.1767
                udtReporterIonInfo(1).MZ = 617.1845

            Case eReporterIonMassModeConstants.LycAcetFragment

                ' Custom reporter ions for Ernesto Nakayasu
                ReDim udtReporterIonInfo(1)
                udtReporterIonInfo(0).MZ = 126.09134
                udtReporterIonInfo(1).MZ = 127.094695

            Case eReporterIonMassModeConstants.OGlcNAc
                ' O-GlcNAc
                ReDim udtReporterIonInfo(2)
                udtReporterIonInfo(0).MZ = 204.0872
                udtReporterIonInfo(1).MZ = 300.13079
                udtReporterIonInfo(2).MZ = 503.21017

            Case eReporterIonMassModeConstants.FrackingAmine20160217
                ' Product ions associated with FrackingFluid_amine_1_02172016
                ReDim udtReporterIonInfo(2)
                udtReporterIonInfo(0).MZ = 157.089
                udtReporterIonInfo(1).MZ = 170.097
                udtReporterIonInfo(2).MZ = 234.059

            Case eReporterIonMassModeConstants.FSFACustomCarbonyl
                ' Custom product ions from Chengdong Xu
                ReDim udtReporterIonInfo(2)
                udtReporterIonInfo(0).MZ = 171.104
                udtReporterIonInfo(1).MZ = 236.074
                udtReporterIonInfo(2).MZ = 257.088

            Case eReporterIonMassModeConstants.FSFACustomCarboxylic
                ' Custom product ions from Chengdong Xu
                ReDim udtReporterIonInfo(2)
                udtReporterIonInfo(0).MZ = 171.104
                udtReporterIonInfo(1).MZ = 234.058
                udtReporterIonInfo(2).MZ = 336.174

            Case eReporterIonMassModeConstants.FSFACustomHydroxyl
                ' Custom product ions from Chengdong Xu
                ReDim udtReporterIonInfo(1)
                udtReporterIonInfo(0).MZ = 151.063
                udtReporterIonInfo(1).MZ = 166.087

            Case Else
                ' Includes eReporterIonMassModeConstants.CustomOrNone
                ReDim udtReporterIonInfo(-1)
        End Select

        For intIndex = 0 To udtReporterIonInfo.Length - 1
            udtReporterIonInfo(intIndex).MZToleranceDa = dblMZToleranceDa
        Next intIndex

        Return udtReporterIonInfo

    End Function

    Public Function GetReporterIons() As udtReporterIonInfoType()
        Dim udtReporterIonInfo() As udtReporterIonInfoType

        If mReporterIonCount <= 0 Then
            ReDim udtReporterIonInfo(-1)
        Else
            ReDim udtReporterIonInfo(mReporterIonCount - 1)
            Array.Copy(mReporterIonInfo, udtReporterIonInfo, mReporterIonCount)
        End If

        Return udtReporterIonInfo
    End Function

    Public Shared Function GetReporterIonModeDescription(eReporterIonMode As eReporterIonMassModeConstants) As String

        Select Case eReporterIonMode
            Case eReporterIonMassModeConstants.CustomOrNone
                Return "Custom/None"
            Case eReporterIonMassModeConstants.ITraqFourMZ
                Return "4-plex iTraq"
            Case eReporterIonMassModeConstants.ITraqETDThreeMZ
                Return "3-plex ETD iTraq"
            Case eReporterIonMassModeConstants.TMTTwoMZ
                Return "2-plex TMT"
            Case eReporterIonMassModeConstants.TMTSixMZ
                Return "6-plex TMT"
            Case eReporterIonMassModeConstants.TMTTenMZ
                Return "10-plex TMT"
            Case eReporterIonMassModeConstants.ITraqEightMZHighRes
                Return "8-plex iTraq (High Res MS/MS)"
            Case eReporterIonMassModeConstants.ITraqEightMZLowRes
                Return "8-plex iTraq (Low Res MS/MS)"
            Case eReporterIonMassModeConstants.PCGalnaz
                Return "PCGalnaz (300.13 m/z and 503.21 m/z)"
            Case eReporterIonMassModeConstants.HemeCFragment
                Return "Heme C (616.18 m/z and 616.19 m/z)"
            Case eReporterIonMassModeConstants.LycAcetFragment
                Return "Lys Acet (126.091 m/z and 127.095 m/z)"
            Case eReporterIonMassModeConstants.OGlcNAc
                Return "O-GlcNAc (204.087, 300.13, and 503.21 m/z)"
            Case eReporterIonMassModeConstants.FrackingAmine20160217
                Return "Fracking Amine 20160217 (157.089, 170.097, and 234.059 m/z)"
            Case eReporterIonMassModeConstants.FSFACustomCarbonyl
                Return "FSFA Custom Carbonyl (171.104, 236.074, 157.088 m/z)"
            Case eReporterIonMassModeConstants.FSFACustomCarboxylic
                Return "FSFA Custom Carboxylic (171.104, 234.058, 336.174 m/z)"
            Case eReporterIonMassModeConstants.FSFACustomHydroxyl
                Return "FSFA Custom Hydroxyl (151.063 and 166.087 m/z)"
            Case Else
                Return "Unknown mode"
        End Select

    End Function

	Protected Function GetParentIonToleranceDa(udtSICOptions As udtSICOptionsType, dblParentIonMZ As Double) As Double
		Return GetParentIonToleranceDa(udtSICOptions, dblParentIonMZ, udtSICOptions.SICTolerance)
	End Function

	Protected Function GetParentIonToleranceDa(udtSICOptions As udtSICOptionsType, dblParentIonMZ As Double, dblParentIonTolerance As Double) As Double
		If udtSICOptions.SICToleranceIsPPM Then
			Return PPMToMass(dblParentIonTolerance, dblParentIonMZ)
		Else
			Return dblParentIonTolerance
		End If
	End Function

	Private Function GetScanByMasterScanIndex(ByRef udtScanList As udtScanListType, intMasterScanIndex As Integer) As udtScanInfoType
		Dim udtCurrentScan As udtScanInfoType

		udtCurrentScan = New udtScanInfoType
		If Not udtScanList.MasterScanOrder Is Nothing Then
			If intMasterScanIndex < 0 Then
				intMasterScanIndex = 0
			ElseIf intMasterScanIndex >= udtScanList.MasterScanOrderCount Then
				intMasterScanIndex = udtScanList.MasterScanOrderCount - 1
			End If

			Select Case udtScanList.MasterScanOrder(intMasterScanIndex).ScanType
				Case eScanTypeConstants.SurveyScan
					' Survey scan
					udtCurrentScan = udtScanList.SurveyScans(udtScanList.MasterScanOrder(intMasterScanIndex).ScanIndexPointer)
				Case eScanTypeConstants.FragScan
					' Frag Scan
					udtCurrentScan = udtScanList.FragScans(udtScanList.MasterScanOrder(intMasterScanIndex).ScanIndexPointer)
				Case Else
					' Unkown scan type
			End Select
		End If

		Return udtCurrentScan

	End Function

	Private Function GetScanToleranceTypeFromText(strScanType As String) As eCustomSICScanTypeConstants
		Dim strScanTypeLowerCase As String

		If strScanType Is Nothing Then strScanType = String.Empty
		strScanTypeLowerCase = strScanType.ToLower.Trim

		If strScanTypeLowerCase = CUSTOM_SIC_TYPE_RELATIVE.ToLower Then
			Return eCustomSICScanTypeConstants.Relative
		ElseIf strScanTypeLowerCase = CUSTOM_SIC_TYPE_ACQUISITION_TIME.ToLower Then
			Return eCustomSICScanTypeConstants.AcquisitionTime
		Else
			' Assume absolute
			Return eCustomSICScanTypeConstants.Absolute
		End If

	End Function

	Private Function GetScanTypeName(eScanType As eScanTypeConstants) As String
		Select Case eScanType
			Case eScanTypeConstants.SurveyScan
				Return "survey scan"
			Case eScanTypeConstants.FragScan
				Return "frag scan"
			Case Else
				Return "unknown scan type"
		End Select
	End Function

	Public Function GetStatusLogKeyNameFilterList() As String()
		If mStatusLogKeyNameFilterList Is Nothing Then
			ReDim mStatusLogKeyNameFilterList(-1)
		End If

		Return mStatusLogKeyNameFilterList
	End Function

	''' <summary>
	''' Returns the contents of mStatusLogKeyNameFilterList
	''' </summary>
	''' <param name="blnCommaSeparatedList">When true, then returns a comma separated list; when false, returns separates items with CrLf</param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function GetStatusLogKeyNameFilterListAsText(blnCommaSeparatedList As Boolean) As String
		If mStatusLogKeyNameFilterList Is Nothing Then
			ReDim mStatusLogKeyNameFilterList(-1)
		End If

		Dim intIndex As Integer
		Dim strList As String
		strList = String.Empty

		For intIndex = 0 To mStatusLogKeyNameFilterList.Length - 1
			If intIndex > 0 Then
				If blnCommaSeparatedList Then
					strList &= ", "
				Else
					strList &= ControlChars.NewLine
				End If
			End If
			strList &= mStatusLogKeyNameFilterList(intIndex)
		Next

		Return strList
	End Function

	Private Function GetTotalProcessingTimeSec() As Single

		Dim objProcess As Diagnostics.Process
		objProcess = Diagnostics.Process.GetCurrentProcess()

		Return CSng(objProcess.TotalProcessorTime().TotalSeconds)

	End Function

	Private Sub InitializeScanList(
	  ByRef udtScanList As udtScanListType, 
	  intSurveyScanCountToAllocate As Integer, 
	  intFragScanCountToAllocate As Integer)

		Dim intMasterScanOrderCountToAllocate As Integer
		Dim intIndex As Integer

		If intSurveyScanCountToAllocate < 1 Then intSurveyScanCountToAllocate = 1
		If intFragScanCountToAllocate < 1 Then intFragScanCountToAllocate = 1

		intMasterScanOrderCountToAllocate = intSurveyScanCountToAllocate + intFragScanCountToAllocate

		With udtScanList
			.SurveyScanCount = 0
			ReDim .SurveyScans(intSurveyScanCountToAllocate - 1)

			.FragScanCount = 0
			ReDim .FragScans(intFragScanCountToAllocate - 1)

			.MasterScanOrderCount = 0
			ReDim .MasterScanOrder(intMasterScanOrderCountToAllocate - 1)
			ReDim .MasterScanNumList(intMasterScanOrderCountToAllocate - 1)
			ReDim .MasterScanTimeList(intMasterScanOrderCountToAllocate - 1)

			InitializeSingleScan(.SurveyScans(0))
			InitializeSingleScan(.FragScans(0))

			.ParentIonInfoCount = 0
			ReDim .ParentIons(intFragScanCountToAllocate - 1)
			For intIndex = 0 To intFragScanCountToAllocate - 1
				ReDim .ParentIons(intIndex).FragScanIndices(0)
			Next intIndex

		End With
	End Sub

	''Private Sub ExpandScanList(ByRef udtScanList As udtScanListType, sngSurveyScanExpansionFactor As Single, sngFragScanExpansionFactor As Integer)
	''    ' Set sngSurveyScanExpansionFactor to 2 to double the amount of memory reserved for survey scans
	''    ' Set sngFragScanExpansionFactor to 2 to double the amount of memory reserved for fragmentation scans

	''    ' If sngSurveyScanExpansionFactor is <= 1 then the memory reserved will not be changed
	''    ' If sngFragScanExpansionFactor is <= 1 then the memory reserved will not be changed

	''    Dim intNewSurveyScanCount As Integer
	''    Dim intNewFragScanCount As Integer
	''    Dim intNewMasterScanOrderCount As Integer

	''    Dim intIndex As Integer

	''    Dim blnUpdateMasterScanOrder As Boolean

	''    If sngSurveyScanExpansionFactor < 1 Then sngSurveyScanExpansionFactor = 1
	''    If sngFragScanExpansionFactor < 1 Then sngFragScanExpansionFactor = 1

	''    With udtScanList
	''        If sngSurveyScanExpansionFactor > 1 Then
	''            intNewSurveyScanCount = CInt(.SurveyScans.Length * sngSurveyScanExpansionFactor)
	''            ReDim Preserve .SurveyScans(intNewSurveyScanCount - 1)
	''            blnUpdateMasterScanOrder = True
	''        End If

	''        If sngFragScanExpansionFactor > 1 Then
	''            intNewFragScanCount = CInt(.FragScans.Length * sngFragScanExpansionFactor)
	''            ReDim Preserve .FragScans(intNewFragScanCount - 1)
	''            blnUpdateMasterScanOrder = True

	''            ReDim Preserve .ParentIons(intNewFragScanCount - 1)
	''            For intIndex = .ParentIonInfoCount To intNewFragScanCount - 1
	''                ReDim .ParentIons(intIndex).FragScanIndices(0)
	''            Next intIndex
	''        End If

	''        If blnUpdateMasterScanOrder Then
	''            intNewMasterScanOrderCount = .SurveyScans.Length + .FragScans.Length
	''            If intNewMasterScanOrderCount < .MasterScanOrderCount Then
	''                ' This shouldn't normally happen
	''                intNewMasterScanOrderCount = .MasterScanOrderCount
	''            End If

	''            ReDim Preserve .MasterScanOrder(intNewMasterScanOrderCount - 1)
	''        End If

	''    End With
	''End Sub

	Private Function InitializeSICDetailsTextFile(
	  strInputFilePathFull As String, 
	  strOutputFolderPath As String, 
	  ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean
	  
		Dim strOutputFilePath As String = String.Empty

		Try

			If mWriteDetailedSICDataFile Then
				strOutputFilePath = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.SICDataFile)

				udtOutputFileHandles.SICDataFile = New StreamWriter(New FileStream(strOutputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

				' Write the header line
                udtOutputFileHandles.SICDataFile.WriteLine("Dataset" & ControlChars.Tab &
                 "ParentIonIndex" & ControlChars.Tab &
                 "FragScanIndex" & ControlChars.Tab &
                 "ParentIonMZ" & ControlChars.Tab &
                 "Scan" & ControlChars.Tab &
                 "MZ" & ControlChars.Tab &
                 "Intensity")
			End If

		Catch ex As Exception
			LogErrors("InitializeSICDetailsTextFile", "Error initializing the XML output file" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
			Return False
		End Try

		Return True

	End Function

	Private Sub InitializeSingleScan(ByRef udtScan As udtScanInfoType)
		With udtScan
			.FragScanInfo.ParentIonInfoIndex = -1			 ' -1 means undefined; only used for fragmentation scans
			.IonCount = 0
			.IonCountRaw = 0
		End With
	End Sub

	Private Sub InitializeUniqueMZListMatchIndices(
	  ByRef udtUniqueMZList() As udtUniqueMZListType, 
	  intStartIndex As Integer, 
	  intEndIndex As Integer)
	  
		Dim intIndex As Integer

		For intIndex = intStartIndex To intEndIndex
			ReDim udtUniqueMZList(intIndex).MatchIndices(0)
			udtUniqueMZList(intIndex).MatchCount = 0
		Next intIndex

	End Sub

	Private Sub InitializeVariables()
		mLocalErrorCode = eMasicErrorCodes.NoError
		mStatusMessage = String.Empty

		mDatasetLookupFilePath = String.Empty
		mDatabaseConnectionString = String.Empty
		mDatasetInfoQuerySql = String.Empty

		mIncludeHeadersInExportFile = True
		mIncludeScanTimesInSICStatsFile = False
		mFastExistingXMLFileTest = False

		mSkipMSMSProcessing = False
		mSkipSICAndRawDataProcessing = False
		mExportRawDataOnly = False

		mSuppressNoParentIonsError = False

		mWriteMSMethodFile = True
		mWriteMSTuneFile = False

		mWriteDetailedSICDataFile = False
		mWriteExtendedStats = True
		mWriteExtendedStatsIncludeScanFilterText = True
        mWriteExtendedStatsStatusLog = True
		mConsolidateConstantExtendedHeaderValues = True

		ReDim mStatusLogKeyNameFilterList(-1)
        SetStatusLogKeyNameFilterList("Source", ","c)

		mWriteMRMDataList = False
		mWriteMRMIntensityCrosstab = True

		With mRawDataExportOptions
			.ExportEnabled = False

			.FileFormat = eExportRawDataFileFormatConstants.CSVFile
			.IncludeMSMS = False
			.RenumberScans = False

			.MinimumSignalToNoiseRatio = 1
			.MaxIonCountPerScan = 200
			.IntensityMinimum = 0
		End With

		mCDFTimeInSeconds = True
        mParentIonDecoyMassDa = 0

		mUseBase64DataEncoding = False						' Enabling this gives files of nearly equivalent size, but with the data arrays base-64 encoded; thus, no advantage

		Try
			mFreeMemoryPerformanceCounter = New Diagnostics.PerformanceCounter("Memory", "Available MBytes")
			mFreeMemoryPerformanceCounter.ReadOnly = True
		Catch ex As Exception
			LogErrors("InitializeVariables", "Error instantiating the Memory->'Available MBytes' performance counter", ex, False, False, eMasicErrorCodes.NoError)
		End Try

		mMASICPeakFinder = New MASICPeakFinder.clsMASICPeakFinder

		InitializeSICOptions(mSICOptions)
		InitializeBinningOptions(mBinningOptions)

		InitializeMemoryManagementOptions(mProcessingStats)
		clsSpectraCache.ResetCacheOptions(mCacheOptions)

		InitializeCustomMZList(mCustomSICList)

		InitializeReporterIonInfo()
	End Sub

	Private Sub InitializeBinningOptions(ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType)
		clsCorrelation.InitializeBinningOptions(udtBinningOptions)
	End Sub

	Private Sub InitializeCustomMZList(ByRef udtCustomMZList As udtCustomMZSearchListType)
		InitializeCustomMZList(udtCustomMZList, True)
	End Sub

	Private Sub InitializeCustomMZList(ByRef udtCustomMZList As udtCustomMZSearchListType, blnResetTolerances As Boolean)
		With udtCustomMZList
			If blnResetTolerances Then
				.ScanToleranceType = eCustomSICScanTypeConstants.Absolute
				.ScanOrAcqTimeTolerance = 1000
			End If

			ReDim .CustomMZSearchValues(-1)
			.RawTextMZList = String.Empty
			.RawTextMZToleranceDaList = String.Empty
			.RawTextScanOrAcqTimeCenterList = String.Empty
			.RawTextScanOrAcqTimeToleranceList = String.Empty
		End With
	End Sub

	Private Sub InitializeReporterIonInfo()
		mReporterIonCount = 0
		ReDim mReporterIonInfo(0)

		SetReporterIonMassMode(eReporterIonMassModeConstants.CustomOrNone)

		Me.ReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT
		Me.ReporterIonApplyAbundanceCorrection = True
		Me.ReporterIonITraq4PlexCorrectionFactorType = clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex.ABSciex

		Me.ReporterIonSaveObservedMasses = False
		Me.ReporterIonSaveUncorrectedIntensities = False

	End Sub

	Private Sub InitializeSICOptions(ByRef udtSICOptions As udtSICOptionsType)
		With udtSICOptions
            .SICTolerance = 10
            .SICToleranceIsPPM = True

			.RefineReportedParentIonMZ = False			' Typically only useful when using a small value for .SICTolerance

			.ScanRangeStart = 0
			.ScanRangeEnd = 0
			.RTRangeStart = 0
			.RTRangeEnd = 0

			.CompressMSSpectraData = True
			.CompressMSMSSpectraData = True

			.CompressToleranceDivisorForDa = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA
			.CompressToleranceDivisorForPPM = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM

			.MaxSICPeakWidthMinutesBackward = 5
			.MaxSICPeakWidthMinutesForward = 5

			.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = True

			.SICPeakFinderOptions = MASICPeakFinder.clsMASICPeakFinder.GetDefaultSICPeakFinderOptions

			.SaveSmoothedData = False

			' Note: When using narrow SIC tolerances, be sure to SimilarIonMZToleranceHalfWidth to a smaller value
			' However, with very small values, the SpectraCache file will be much larger
			' The default for SimilarIonMZToleranceHalfWidth is 0.1
			' Consider using 0.05 when using ppm-based SIC tolerances
            .SimilarIonMZToleranceHalfWidth = 0.05

			' .SimilarIonScanToleranceHalfWidth = 100
			.SimilarIonToleranceHalfWidthMinutes = 5
			.SpectrumSimilarityMinimum = 0.8
		End With
	End Sub

	Private Sub InitializeMemoryManagementOptions(ByRef udtMemoryOptions As udtProcessingStatsType)

		With udtMemoryOptions
			.PeakMemoryUsageMB = GetProcessMemoryUsageMB()
			.TotalProcessingTimeAtStart = GetTotalProcessingTimeSec()
			.CacheEventCount = 0
			.UnCacheEventCount = 0

			.FileLoadStartTime = DateTime.UtcNow
			.FileLoadEndTime = .FileLoadStartTime

			.ProcessingStartTime = .FileLoadStartTime
			.ProcessingEndTime = .FileLoadStartTime

			.MemoryUsageMBAtStart = .PeakMemoryUsageMB
			.MemoryUsageMBDuringLoad = .PeakMemoryUsageMB
			.MemoryUsageMBAtEnd = .PeakMemoryUsageMB
		End With

	End Sub

	Private Function InterpolateRTandFragScanNumber(
	  ByRef udtSurveyScans() As udtScanInfoType, 
	  intSurveyScanCount As Integer, 
	  intLastSurveyScanIndex As Integer, 
	  intFragScanNumber As Integer, 
	  ByRef intFragScanIteration As Integer) As Single
	  
		' Examine the scan numbers in udtSurveyScans, starting at intLastSurveyScanIndex, to find the survey scans on either side of intFragScanNumber
		' Interpolate the retention time that corresponds to intFragScanNumber
		' Determine intFragScanNumber, which is generally 1, 2, or 3, indicating if this is the 1st, 2nd, or 3rd MS/MS scan after the survey scan

		Dim sngRT As Single
		Dim sngPrevScanRT As Single
		Dim sngNextScanRT As Single
		Dim intScanDiff As Integer

		Try
			intFragScanIteration = 1

			' Decrement intLastSurveyScanIndex if the corresponding SurveyScan's scan number is larger than intFragScanNumber
			Do While intLastSurveyScanIndex > 0 AndAlso udtSurveyScans(intLastSurveyScanIndex).ScanNumber > intFragScanNumber
				' This code will generally not be reached, provided the calling function passed the correct intLastSurveyScanIndex value to this function
				intLastSurveyScanIndex -= 1
			Loop

			' Increment intLastSurveyScanIndex if the next SurveyScan's scan number is smaller than intFragScanNumber
			Do While intLastSurveyScanIndex < intSurveyScanCount - 1 AndAlso udtSurveyScans(intLastSurveyScanIndex + 1).ScanNumber < intFragScanNumber
				' This code will generally not be reached, provided the calling function passed the correct intLastSurveyScanIndex value to this function
				intLastSurveyScanIndex += 1
			Loop

			If intLastSurveyScanIndex >= intSurveyScanCount - 1 Then
				' Cannot easily interpolate since FragScanNumber is greater than the last survey scan number
				If intSurveyScanCount > 0 Then
					If intSurveyScanCount >= 2 Then
						' Use the scan numbers of the last 2 survey scans to extrapolate the scan number for this fragmentation scan

						intLastSurveyScanIndex = intSurveyScanCount - 1
						With udtSurveyScans(intLastSurveyScanIndex)
							intScanDiff = .ScanNumber - udtSurveyScans(intLastSurveyScanIndex - 1).ScanNumber
							sngPrevScanRT = udtSurveyScans(intLastSurveyScanIndex - 1).ScanTime

							' Compute intFragScanIteration
							intFragScanIteration = intFragScanNumber - .ScanNumber

							If intScanDiff > 0 AndAlso intFragScanIteration > 0 Then
								sngRT = CSng(.ScanTime + (intFragScanIteration / intScanDiff * (.ScanTime - sngPrevScanRT)))
							Else
								' Adjacent survey scans have the same scan number
								' This shouldn't happen
								sngRT = udtSurveyScans(intLastSurveyScanIndex).ScanTime
							End If

							If intFragScanIteration < 1 Then intFragScanIteration = 1

						End With
					Else
						' Use the scan time of the highest survey scan in memory
						sngRT = udtSurveyScans(intSurveyScanCount - 1).ScanTime
					End If
				Else
					sngRT = 0
				End If
			Else
				' Interpolate retention time
				With udtSurveyScans(intLastSurveyScanIndex)
					intScanDiff = udtSurveyScans(intLastSurveyScanIndex + 1).ScanNumber - .ScanNumber
					sngNextScanRT = udtSurveyScans(intLastSurveyScanIndex + 1).ScanTime

					' Compute intFragScanIteration
					intFragScanIteration = intFragScanNumber - .ScanNumber

					If intScanDiff > 0 AndAlso intFragScanIteration > 0 Then
						sngRT = CSng(.ScanTime + (intFragScanIteration / intScanDiff * (sngNextScanRT - .ScanTime)))
					Else
						' Adjacent survey scans have the same scan number
						' This shouldn't happen
						sngRT = .ScanTime
					End If

					If intFragScanIteration < 1 Then intFragScanIteration = 1

				End With

			End If

		Catch ex As Exception
			' Ignore any errors that occur in this function
			LogErrors("InterpolateRTandFragScanNumber", "Error in InterpolateRTandFragScanNumber", ex, True, False)
		End Try

		Return sngRT

	End Function

	Private Function InterpolateX(
      ByRef sngInterpolatedXValue As Single,
      X1 As Integer, 
      X2 As Integer, 
      Y1 As Single, 
      Y2 As Single, 
      sngTargetY As Single) As Boolean

		' Checks if Y1 or Y2 is less than sngTargetY
		' If it is, then determines the X value that corresponds to sngTargetY by interpolating the line between (X1, Y1) and (X2, Y2)
		'
		' Returns True if a match is found; otherwise, returns false

		Dim sngDeltaY As Single
		Dim sngFraction As Single
		Dim intDeltaX As Integer
		Dim sngTargetX As Single

		If Y1 < sngTargetY OrElse Y2 < sngTargetY Then
			If Y1 < sngTargetY AndAlso Y2 < sngTargetY Then
				' Both of the Y values are less than sngTargetY
				' We cannot interpolate
				Debug.Assert(False, "This code should normally not be reached (clsMasic->InterpolateX)")
				Return False
			Else
				sngDeltaY = Y2 - Y1									' Yes, this is y-two minus y-one
				sngFraction = (sngTargetY - Y1) / sngDeltaY
				intDeltaX = X2 - X1									' Yes, this is x-two minus x-one

				sngTargetX = sngFraction * intDeltaX + X1

				If Math.Abs(sngTargetX - X1) >= 0 AndAlso Math.Abs(sngTargetX - X2) >= 0 Then
					sngInterpolatedXValue = sngTargetX
					Return True
				Else
					Debug.Assert(False, "TargetX is not between X1 and X2; this shouldn't happen (clsMasic->InterpolateX)")
					Return False
				End If

			End If
		Else
			Return False
		End If

	End Function

	Protected Function IsNumber(strValue As String) As Boolean
		Try
			Return Double.TryParse(strValue, 0)
		Catch ex As Exception
			Return False
		End Try
	End Function

	Public Function LoadParameterFileSettings(strParameterFilePath As String) As Boolean

		Dim objSettingsFile As New XmlSettingsFileAccessor

		Dim strMZList As String
		Dim strMZToleranceDaList As String

        Dim strScanCenterList As String
        Dim strScanToleranceList As String

		Dim strScanCommentList As String
		Dim strScanTolerance As String
		Dim strScanType As String
		Dim strFilterList As String

		Dim eReporterIonMassMode As eReporterIonMassModeConstants
		Dim eReporterIonITraq4PlexCorrectionFactorType As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex

		Dim dblSICTolerance As Double
		Dim blnSICToleranceIsPPM As Boolean

		Dim strErrorMessage As String
		Dim blnNotPresent As Boolean

		Dim blnSuccess As Boolean

		Try

			If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
				' No parameter file specified; nothing to load
				LogMessage("Parameter file not specified -- will use default settings")
				Return True
			Else
				LogMessage("Loading parameter file: " & strParameterFilePath)
			End If


			If Not File.Exists(strParameterFilePath) Then
				' See if strParameterFilePath points to a file in the same directory as the application
				strParameterFilePath = Path.Combine(GetAppFolderPath(), Path.GetFileName(strParameterFilePath))
				If Not File.Exists(strParameterFilePath) Then
					LogErrors("LoadParameterFileSettings", "Parameter file not found: " & strParameterFilePath, Nothing, True, False)
					MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.ParameterFileNotFound)
					Return False
				End If
			End If

			' Pass False to .LoadSettings() here to turn off case sensitive matching
			If objSettingsFile.LoadSettings(strParameterFilePath, False) Then
				With objSettingsFile

					If Not .SectionPresent(XML_SECTION_DATABASE_SETTINGS) Then
						' Database settings section not found; that's ok
					Else
						Me.DatabaseConnectionString = .GetParam(XML_SECTION_DATABASE_SETTINGS, "ConnectionString", Me.DatabaseConnectionString)
						Me.DatasetInfoQuerySql = .GetParam(XML_SECTION_DATABASE_SETTINGS, "DatasetInfoQuerySql", Me.DatasetInfoQuerySql)
					End If

					If Not .SectionPresent(XML_SECTION_IMPORT_OPTIONS) Then
						' Import options section not found; that's ok
					Else
						Me.CDFTimeInSeconds = .GetParam(XML_SECTION_IMPORT_OPTIONS, "CDFTimeInSeconds", Me.CDFTimeInSeconds)
                        Me.ParentIonDecoyMassDa = .GetParam(XML_SECTION_IMPORT_OPTIONS, "ParentIonDecoyMassDa", Me.ParentIonDecoyMassDa)
					End If

					' Masic Export Options
					If Not .SectionPresent(XML_SECTION_EXPORT_OPTIONS) Then
						' Export options section not found; that's ok
					Else
						Me.IncludeHeadersInExportFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeHeaders", Me.IncludeHeadersInExportFile)
						Me.IncludeScanTimesInSICStatsFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeScanTimesInSICStatsFile", Me.IncludeScanTimesInSICStatsFile)
						Me.SkipMSMSProcessing = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SkipMSMSProcessing", Me.SkipMSMSProcessing)

						' Check for both "SkipSICProcessing" and "SkipSICAndRawDataProcessing" in the XML file
						' If either is true, then mExportRawDataOnly will be auto-set to false in function ProcessFiles
						Me.SkipSICAndRawDataProcessing = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SkipSICProcessing", Me.SkipSICAndRawDataProcessing)
						Me.SkipSICAndRawDataProcessing = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SkipSICAndRawDataProcessing", Me.SkipSICAndRawDataProcessing)

						Me.ExportRawDataOnly = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataOnly", Me.ExportRawDataOnly)

						Me.SuppressNoParentIonsError = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SuppressNoParentIonsError", Me.SuppressNoParentIonsError)

						Me.WriteDetailedSICDataFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteDetailedSICDataFile", Me.WriteDetailedSICDataFile)
						Me.WriteMSMethodFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSMethodFile", Me.WriteMSMethodFile)
						Me.WriteMSTuneFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSTuneFile", Me.WriteMSTuneFile)

						Me.WriteExtendedStats = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStats", Me.WriteExtendedStats)
						Me.WriteExtendedStatsIncludeScanFilterText = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsIncludeScanFilterText", Me.WriteExtendedStatsIncludeScanFilterText)
						Me.WriteExtendedStatsStatusLog = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsStatusLog", Me.WriteExtendedStatsStatusLog)
						strFilterList = .GetParam(XML_SECTION_EXPORT_OPTIONS, "StatusLogKeyNameFilterList", String.Empty)
						If Not strFilterList Is Nothing AndAlso strFilterList.Length > 0 Then
							SetStatusLogKeyNameFilterList(strFilterList, ","c)
						End If

						Me.ConsolidateConstantExtendedHeaderValues = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ConsolidateConstantExtendedHeaderValues", Me.ConsolidateConstantExtendedHeaderValues)

						Me.WriteMRMDataList = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMDataList", Me.WriteMRMDataList)
						Me.WriteMRMIntensityCrosstab = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMIntensityCrosstab", Me.WriteMRMIntensityCrosstab)

						Me.FastExistingXMLFileTest = .GetParam(XML_SECTION_EXPORT_OPTIONS, "FastExistingXMLFileTest", Me.FastExistingXMLFileTest)

						Me.ReporterIonStatsEnabled = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonStatsEnabled", Me.ReporterIonStatsEnabled)
						eReporterIonMassMode = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonMassMode", CInt(Me.ReporterIonMassMode)), eReporterIonMassModeConstants)
						Me.ReporterIonToleranceDaDefault = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonToleranceDa", Me.ReporterIonToleranceDaDefault)
						Me.ReporterIonApplyAbundanceCorrection = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonApplyAbundanceCorrection", Me.ReporterIonApplyAbundanceCorrection)

						eReporterIonITraq4PlexCorrectionFactorType = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonITraq4PlexCorrectionFactorType", CInt(Me.ReporterIonITraq4PlexCorrectionFactorType)), clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex)
						Me.ReporterIonITraq4PlexCorrectionFactorType = eReporterIonITraq4PlexCorrectionFactorType

						Me.ReporterIonSaveObservedMasses = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveObservedMasses", Me.ReporterIonSaveObservedMasses)
						Me.ReporterIonSaveUncorrectedIntensities = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveUncorrectedIntensities", Me.ReporterIonSaveUncorrectedIntensities)

						SetReporterIonMassMode(eReporterIonMassMode, Me.ReporterIonToleranceDaDefault)

						' Raw data export options
						Me.ExportRawSpectraData = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawSpectraData", Me.ExportRawSpectraData)
						Me.ExportRawDataFileFormat = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataFileFormat", CInt(Me.ExportRawDataFileFormat)), eExportRawDataFileFormatConstants)

						Me.ExportRawDataIncludeMSMS = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIncludeMSMS", Me.ExportRawDataIncludeMSMS)
						Me.ExportRawDataRenumberScans = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataRenumberScans", Me.ExportRawDataRenumberScans)

						Me.ExportRawDataMinimumSignalToNoiseRatio = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMinimumSignalToNoiseRatio", Me.ExportRawDataMinimumSignalToNoiseRatio)
						Me.ExportRawDataMaxIonCountPerScan = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMaxIonCountPerScan", Me.ExportRawDataMaxIonCountPerScan)
						Me.ExportRawDataIntensityMinimum = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIntensityMinimum", Me.ExportRawDataIntensityMinimum)
					End If

					If Not .SectionPresent(XML_SECTION_SIC_OPTIONS) Then
						strErrorMessage = "The node '<section name=" & ControlChars.Quote & XML_SECTION_SIC_OPTIONS & ControlChars.Quote & "> was not found in the parameter file: " & strParameterFilePath
						If MyBase.ShowMessages Then
							Windows.Forms.MessageBox.Show(strErrorMessage, "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
							LogMessage(strErrorMessage, eMessageTypeConstants.ErrorMsg)
						Else
							ShowErrorMessage(strErrorMessage)
						End If
						MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
						Return False
					Else
						' SIC Options
						' Note: Skipping .DatasetNumber since this must be provided at the command line or through the Property Function interface

						' Preferentially use "SICTolerance", if it is present
						dblSICTolerance = .GetParam(XML_SECTION_SIC_OPTIONS, "SICTolerance", GetSICTolerance(), blnNotPresent)

						If blnNotPresent Then
							' Check for "SICToleranceDa", which is a legacy setting
							dblSICTolerance = .GetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceDa", Me.SICToleranceDa, blnNotPresent)

							If Not blnNotPresent Then
								SetSICTolerance(dblSICTolerance, False)
							End If
						Else
							blnSICToleranceIsPPM = .GetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceIsPPM", False)

							SetSICTolerance(dblSICTolerance, blnSICToleranceIsPPM)
						End If

						Me.RefineReportedParentIonMZ = .GetParam(XML_SECTION_SIC_OPTIONS, "RefineReportedParentIonMZ", Me.RefineReportedParentIonMZ)
						Me.ScanRangeStart = .GetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeStart", Me.ScanRangeStart)
						Me.ScanRangeEnd = .GetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeEnd", Me.ScanRangeEnd)
						Me.RTRangeStart = .GetParam(XML_SECTION_SIC_OPTIONS, "RTRangeStart", Me.RTRangeStart)
						Me.RTRangeEnd = .GetParam(XML_SECTION_SIC_OPTIONS, "RTRangeEnd", Me.RTRangeEnd)

						Me.CompressMSSpectraData = .GetParam(XML_SECTION_SIC_OPTIONS, "CompressMSSpectraData", Me.CompressMSSpectraData)
						Me.CompressMSMSSpectraData = .GetParam(XML_SECTION_SIC_OPTIONS, "CompressMSMSSpectraData", Me.CompressMSMSSpectraData)

						Me.CompressToleranceDivisorForDa = .GetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForDa", Me.CompressToleranceDivisorForDa)
						Me.CompressToleranceDivisorForPPM = .GetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForPPM", Me.CompressToleranceDivisorForPPM)

						Me.MaxSICPeakWidthMinutesBackward = .GetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesBackward", Me.MaxSICPeakWidthMinutesBackward)
						Me.MaxSICPeakWidthMinutesForward = .GetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesForward", Me.MaxSICPeakWidthMinutesForward)
						Me.IntensityThresholdFractionMax = .GetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdFractionMax", Me.IntensityThresholdFractionMax)
						Me.IntensityThresholdAbsoluteMinimum = .GetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdAbsoluteMinimum", Me.IntensityThresholdAbsoluteMinimum)

						' Peak Finding Options
						Me.SICNoiseThresholdMode = CType(.GetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdMode", CInt(Me.SICNoiseThresholdMode)), MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
						Me.SICNoiseThresholdIntensity = .GetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdIntensity", Me.SICNoiseThresholdIntensity)
						Me.SICNoiseFractionLowIntensityDataToAverage = .GetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseFractionLowIntensityDataToAverage", Me.SICNoiseFractionLowIntensityDataToAverage)
						Me.SICNoiseMinimumSignalToNoiseRatio = .GetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseMinimumSignalToNoiseRatio", Me.SICNoiseMinimumSignalToNoiseRatio)

						Me.MaxDistanceScansNoOverlap = .GetParam(XML_SECTION_SIC_OPTIONS, "MaxDistanceScansNoOverlap", Me.MaxDistanceScansNoOverlap)
						Me.MaxAllowedUpwardSpikeFractionMax = .GetParam(XML_SECTION_SIC_OPTIONS, "MaxAllowedUpwardSpikeFractionMax", Me.MaxAllowedUpwardSpikeFractionMax)
						Me.InitialPeakWidthScansScaler = .GetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansScaler", Me.InitialPeakWidthScansScaler)
						Me.InitialPeakWidthScansMaximum = .GetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansMaximum", Me.InitialPeakWidthScansMaximum)

						Me.FindPeaksOnSmoothedData = .GetParam(XML_SECTION_SIC_OPTIONS, "FindPeaksOnSmoothedData", Me.FindPeaksOnSmoothedData)
						Me.SmoothDataRegardlessOfMinimumPeakWidth = .GetParam(XML_SECTION_SIC_OPTIONS, "SmoothDataRegardlessOfMinimumPeakWidth", Me.SmoothDataRegardlessOfMinimumPeakWidth)
						Me.UseButterworthSmooth = .GetParam(XML_SECTION_SIC_OPTIONS, "UseButterworthSmooth", Me.UseButterworthSmooth)
						Me.ButterworthSamplingFrequency = .GetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequency", Me.ButterworthSamplingFrequency)
						Me.ButterworthSamplingFrequencyDoubledForSIMData = .GetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequencyDoubledForSIMData", Me.ButterworthSamplingFrequencyDoubledForSIMData)

						Me.UseSavitzkyGolaySmooth = .GetParam(XML_SECTION_SIC_OPTIONS, "UseSavitzkyGolaySmooth", Me.UseSavitzkyGolaySmooth)
						Me.SavitzkyGolayFilterOrder = .GetParam(XML_SECTION_SIC_OPTIONS, "SavitzkyGolayFilterOrder", Me.SavitzkyGolayFilterOrder)
						Me.SaveSmoothedData = .GetParam(XML_SECTION_SIC_OPTIONS, "SaveSmoothedData", Me.SaveSmoothedData)

						Me.MassSpectraNoiseThresholdMode = CType(.GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdMode", CInt(Me.MassSpectraNoiseThresholdMode)), MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
						Me.MassSpectraNoiseThresholdIntensity = .GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdIntensity", Me.MassSpectraNoiseThresholdIntensity)
						Me.MassSpectraNoiseFractionLowIntensityDataToAverage = .GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseFractionLowIntensityDataToAverage", Me.MassSpectraNoiseFractionLowIntensityDataToAverage)
						Me.MassSpectraNoiseMinimumSignalToNoiseRatio = .GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseMinimumSignalToNoiseRatio ", Me.MassSpectraNoiseMinimumSignalToNoiseRatio)

						Me.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = .GetParam(XML_SECTION_SIC_OPTIONS, "ReplaceSICZeroesWithMinimumPositiveValueFromMSData", Me.ReplaceSICZeroesWithMinimumPositiveValueFromMSData)

						' Similarity Options
						Me.SimilarIonMZToleranceHalfWidth = .GetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonMZToleranceHalfWidth", Me.SimilarIonMZToleranceHalfWidth)
						Me.SimilarIonToleranceHalfWidthMinutes = .GetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonToleranceHalfWidthMinutes", Me.SimilarIonToleranceHalfWidthMinutes)
						Me.SpectrumSimilarityMinimum = .GetParam(XML_SECTION_SIC_OPTIONS, "SpectrumSimilarityMinimum", Me.SpectrumSimilarityMinimum)

					End If

					' Binning Options
					If Not .SectionPresent(XML_SECTION_BINNING_OPTIONS) Then
						strErrorMessage = "The node '<section name=" & ControlChars.Quote & XML_SECTION_BINNING_OPTIONS & ControlChars.Quote & "> was not found in the parameter file: " & strParameterFilePath
						If MyBase.ShowMessages Then
							Windows.Forms.MessageBox.Show(strErrorMessage, "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
							LogMessage(strErrorMessage, eMessageTypeConstants.ErrorMsg)
						Else
							ShowErrorMessage(strErrorMessage)
						End If

						MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
						Return False
					Else
						Me.BinStartX = .GetParam(XML_SECTION_BINNING_OPTIONS, "BinStartX", Me.BinStartX)
						Me.BinEndX = .GetParam(XML_SECTION_BINNING_OPTIONS, "BinEndX", Me.BinEndX)
						Me.BinSize = .GetParam(XML_SECTION_BINNING_OPTIONS, "BinSize", Me.BinSize)
						Me.MaximumBinCount = .GetParam(XML_SECTION_BINNING_OPTIONS, "MaximumBinCount", Me.MaximumBinCount)

						Me.BinnedDataIntensityPrecisionPercent = .GetParam(XML_SECTION_BINNING_OPTIONS, "IntensityPrecisionPercent", Me.BinnedDataIntensityPrecisionPercent)
						Me.NormalizeBinnedData = .GetParam(XML_SECTION_BINNING_OPTIONS, "Normalize", Me.NormalizeBinnedData)
						Me.SumAllIntensitiesForBin = .GetParam(XML_SECTION_BINNING_OPTIONS, "SumAllIntensitiesForBin", Me.SumAllIntensitiesForBin)
					End If

					' Memory management options
					Me.DiskCachingAlwaysDisabled = .GetParam(XML_SECTION_MEMORY_OPTIONS, "DiskCachingAlwaysDisabled", Me.DiskCachingAlwaysDisabled)
					Me.CacheFolderPath = .GetParam(XML_SECTION_MEMORY_OPTIONS, "CacheFolderPath", Me.CacheFolderPath)

					Me.CacheSpectraToRetainInMemory = .GetParam(XML_SECTION_MEMORY_OPTIONS, "CacheSpectraToRetainInMemory", Me.CacheSpectraToRetainInMemory)
					Me.CacheMinimumFreeMemoryMB = .GetParam(XML_SECTION_MEMORY_OPTIONS, "CacheMinimumFreeMemoryMB", Me.CacheMinimumFreeMemoryMB)
					Me.CacheMaximumMemoryUsageMB = .GetParam(XML_SECTION_MEMORY_OPTIONS, "CacheMaximumMemoryUsageMB", Me.CacheMaximumMemoryUsageMB)

				End With

				If Not objSettingsFile.SectionPresent(XML_SECTION_CUSTOM_SIC_VALUES) Then
					' Custom SIC values section not found; that's ok
				Else
					Me.LimitSearchToCustomMZList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "LimitSearchToCustomMZList", Me.LimitSearchToCustomMZList)

					strScanType = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", String.Empty)
					strScanTolerance = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", String.Empty)

					With mCustomSICList
						.ScanToleranceType = GetScanToleranceTypeFromText(strScanType)

						If strScanTolerance.Length > 0 AndAlso IsNumber(strScanTolerance) Then
							If .ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
								.ScanOrAcqTimeTolerance = CInt(strScanTolerance)
							Else
								' Includes .Relative and .AcquisitionTime
								.ScanOrAcqTimeTolerance = CSng(strScanTolerance)
							End If
						Else
							.ScanOrAcqTimeTolerance = 0
						End If
					End With

					Me.CustomSICListFileName = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", String.Empty)

					If Me.CustomSICListFileName.Length > 0 Then
						' Clear mCustomSICList; we'll read the data from the file when ProcessFile is called()

						InitializeCustomMZList(mCustomSICList, False)

						blnSuccess = True
					Else
						strMZList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZList", String.Empty)
						strMZToleranceDaList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZToleranceDaList", String.Empty)

                        strScanCenterList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList", String.Empty)
                        strScanToleranceList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanToleranceList", String.Empty)

						strScanCommentList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", String.Empty)

                        blnSuccess = ParseCustomSICList(strMZList, strMZToleranceDaList, strScanCenterList, strScanToleranceList, strScanCommentList, strParameterFilePath)
					End If

					If Not blnSuccess Then
						Return False
					End If

				End If
			Else
				LogErrors("LoadParameterFileSettings", "Error calling objSettingsFile.LoadSettings for " & strParameterFilePath, Nothing, True, False, eMasicErrorCodes.InputFileDataReadError)
				Return False
			End If

		Catch ex As Exception
			LogErrors("LoadParameterFileSettings", "Error in LoadParameterFileSettings", ex, True, False, eMasicErrorCodes.InputFileDataReadError)
			Return False
		End Try

		Return True

	End Function

	Protected Function LoadCustomSICListFromFile(strCustomSICValuesFileName As String) As Boolean

		Dim strLineIn As String
		Dim strSplitLine() As String

		Dim strDelimList() As Char = New Char() {ControlChars.Tab}

		Dim strErrorMessage As String

		Dim blnSuccess As Boolean
		Dim blnMZHeaderFound As Boolean

		Dim blnScanTimeHeaderFound As Boolean
		Dim blnTimeToleranceHeaderFound As Boolean
		Dim blnForceAcquisitionTimeMode As Boolean

		Dim intLinesRead As Integer
		Dim intColIndex As Integer

		Dim eColumnMapping() As Integer
		Dim intCustomMZCount As Integer

		Try
			blnSuccess = True
			blnMZHeaderFound = False
			blnScanTimeHeaderFound = False
			blnTimeToleranceHeaderFound = False

			InitializeCustomMZList(mCustomSICList, False)

			' Initially reserve space for 10 custom m/z values
			ReDim Preserve mCustomSICList.CustomMZSearchValues(9)
			intCustomMZCount = 0

			' eColumnMapping will be initialized when the headers are read
			ReDim eColumnMapping(-1)

			If Not File.Exists(strCustomSICValuesFileName) Then
				' Custom SIC file not found

				strErrorMessage = "Custom MZ List file not found: " & strCustomSICValuesFileName
				If MyBase.ShowMessages Then
					Windows.Forms.MessageBox.Show(strErrorMessage, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
					LogMessage(strErrorMessage, eMessageTypeConstants.ErrorMsg)
				Else
					ShowErrorMessage(strErrorMessage)
				End If

				SetLocalErrorCode(eMasicErrorCodes.InvalidCustomSICValues)

				blnSuccess = False
				Exit Try
			End If

            Using srInFile = New StreamReader(New FileStream(strCustomSICValuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read))

                intLinesRead = 0
                Do While Not srInFile.EndOfStream
                    strLineIn = srInFile.ReadLine
                    If Not strLineIn Is Nothing Then

                        If intLinesRead = 0 AndAlso Not strLineIn.Contains(ControlChars.Tab) Then
                            ' Split on commas instead of tab characters
                            strDelimList = New Char() {","c}
                        End If

                        strSplitLine = strLineIn.Split(strDelimList)

                        If Not strSplitLine Is Nothing AndAlso strSplitLine.Length > 0 Then
                            If intLinesRead = 0 Then

                                ' Initialize eColumnMapping, setting the value for each column to -1, indicating the column is not present
                                ReDim eColumnMapping(strSplitLine.Length - 1)
                                For intColIndex = 0 To eColumnMapping.Length - 1
                                    eColumnMapping(intColIndex) = -1
                                Next intColIndex

                                ' The first row must be the header row; parse the values
                                For intColIndex = 0 To strSplitLine.Length - 1
                                    Select Case strSplitLine(intColIndex).ToUpper
                                        Case CUSTOM_SIC_COLUMN_MZ.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.MZ
                                            blnMZHeaderFound = True

                                        Case CUSTOM_SIC_COLUMN_MZ_TOLERANCE.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.MZToleranceDa

                                        Case CUSTOM_SIC_COLUMN_SCAN_CENTER.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.ScanCenter

                                        Case CUSTOM_SIC_COLUMN_SCAN_TOLERNACE.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.ScanTolerance

                                        Case CUSTOM_SIC_COLUMN_SCAN_TIME.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.ScanTime
                                            blnScanTimeHeaderFound = True

                                        Case CUSTOM_SIC_COLUMN_TIME_TOLERANCE.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.TimeTolerance
                                            blnTimeToleranceHeaderFound = True

                                        Case CUSTOM_SIC_COLUMN_COMMENT.ToUpper
                                            eColumnMapping(intColIndex) = eCustomSICFileColumns.Comment

                                        Case Else
                                            ' Unknown column name; ignore it
                                    End Select
                                Next intColIndex

                                ' Make sure that, at a minimum, the MZ column is present
                                If Not blnMZHeaderFound Then

                                    strErrorMessage = "Custom M/Z List file " & strCustomSICValuesFileName & "does not have a column header named " & CUSTOM_SIC_COLUMN_MZ & " in the first row; this header is required (valid column headers are: " & GetCustomMZFileColumnHeaders() & ")"
                                    If MyBase.ShowMessages Then
                                        Windows.Forms.MessageBox.Show(strErrorMessage, "Invalid Headers", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                                        LogMessage(strErrorMessage, eMessageTypeConstants.ErrorMsg)
                                    Else
                                        ShowErrorMessage(strErrorMessage)
                                    End If

                                    SetLocalErrorCode(eMasicErrorCodes.InvalidCustomSICHeaders)
                                    blnSuccess = False
                                    Exit Try
                                End If

                                If blnScanTimeHeaderFound AndAlso blnTimeToleranceHeaderFound Then
                                    blnForceAcquisitionTimeMode = True
                                    mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.AcquisitionTime
                                Else
                                    blnForceAcquisitionTimeMode = False
                                End If
                            ElseIf IsNumber(strSplitLine(0)) Then
                                ' Parse this line's data if strSplitLine(0) is numeric

                                With mCustomSICList
                                    If intCustomMZCount >= .CustomMZSearchValues.Length Then
                                        ReDim Preserve .CustomMZSearchValues(.CustomMZSearchValues.Length * 2 - 1)
                                    End If
                                End With

                                With mCustomSICList.CustomMZSearchValues(intCustomMZCount)
                                    .MZ = 0
                                    .MZToleranceDa = 0
                                    .ScanOrAcqTimeCenter = 0
                                    .ScanOrAcqTimeTolerance = 0

                                    For intColIndex = 0 To strSplitLine.Length - 1

                                        If intColIndex >= eColumnMapping.Length Then Exit For

                                        Select Case eColumnMapping(intColIndex)
                                            Case eCustomSICFileColumns.MZ
                                                If Not Double.TryParse(strSplitLine(intColIndex), .MZ) Then
                                                    Throw New InvalidCastException("Non-numeric value for the MZ column in row " & intLinesRead + 1 & ", column " & intColIndex + 1)
                                                End If

                                            Case eCustomSICFileColumns.MZToleranceDa
                                                If Not Double.TryParse(strSplitLine(intColIndex), .MZToleranceDa) Then
                                                    Throw New InvalidCastException("Non-numeric value for the MZToleranceDa column in row " & intLinesRead + 1 & ", column " & intColIndex + 1)
                                                End If

                                            Case eCustomSICFileColumns.ScanCenter
                                                ' Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                                If Not blnForceAcquisitionTimeMode Then
                                                    If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeCenter) Then
                                                        Throw New InvalidCastException("Non-numeric value for the ScanCenter column in row " & intLinesRead + 1 & ", column " & intColIndex + 1)
                                                    End If
                                                End If

                                            Case eCustomSICFileColumns.ScanTolerance
                                                ' Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                                If Not blnForceAcquisitionTimeMode Then
                                                    If mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                                                        .ScanOrAcqTimeTolerance = CInt(strSplitLine(intColIndex))
                                                    Else
                                                        ' Includes .Relative and .AcquisitionTime
                                                        If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeTolerance) Then
                                                            Throw New InvalidCastException("Non-numeric value for the ScanTolerance column in row " & intLinesRead + 1 & ", column " & intColIndex + 1)
                                                        End If
                                                    End If
                                                End If

                                            Case eCustomSICFileColumns.ScanTime
                                                ' Only use this value if both the ScanTime and the TimeTolerance columns were present
                                                If blnForceAcquisitionTimeMode Then
                                                    If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeCenter) Then
                                                        Throw New InvalidCastException("Non-numeric value for the ScanTime column in row " & intLinesRead + 1 & ", column " & intColIndex + 1)
                                                    End If
                                                End If

                                            Case eCustomSICFileColumns.TimeTolerance
                                                ' Only use this value if both the ScanTime and the TimeTolerance columns were present
                                                If blnForceAcquisitionTimeMode Then
                                                    If Not Single.TryParse(strSplitLine(intColIndex), .ScanOrAcqTimeTolerance) Then
                                                        Throw New InvalidCastException("Non-numeric value for the TimeTolerance column in row " & intLinesRead + 1 & ", column " & intColIndex + 1)
                                                    End If
                                                End If

                                            Case eCustomSICFileColumns.Comment
                                                .Comment = String.Copy(strSplitLine(intColIndex))
                                            Case Else
                                                ' Unknown column code
                                        End Select

                                    Next intColIndex

                                End With

                                With mCustomSICList
                                    If intCustomMZCount > 0 Then
                                        .RawTextMZList &= ","c
                                        .RawTextMZToleranceDaList &= ","c
                                        .RawTextScanOrAcqTimeCenterList &= ","c
                                        .RawTextScanOrAcqTimeToleranceList &= ","c
                                    End If

                                    .RawTextMZList &= .CustomMZSearchValues(intCustomMZCount).MZ.ToString
                                    .RawTextMZToleranceDaList &= .CustomMZSearchValues(intCustomMZCount).MZToleranceDa.ToString
                                    .RawTextScanOrAcqTimeCenterList &= .CustomMZSearchValues(intCustomMZCount).ScanOrAcqTimeCenter.ToString
                                    .RawTextScanOrAcqTimeToleranceList &= .CustomMZSearchValues(intCustomMZCount).ScanOrAcqTimeTolerance.ToString

                                End With

                                intCustomMZCount += 1

                            End If

                            intLinesRead += 1
                        End If
                    End If
                Loop

            End Using

		Catch ex As Exception
			LogErrors("LoadCustomSICListFromFile", "Error in LoadCustomSICListFromFile", ex, True, True, eMasicErrorCodes.InvalidCustomSICValues)
			blnSuccess = False
		End Try

		Try
			If blnSuccess Then
				' Shrink the custom mz search values array
				ReDim Preserve mCustomSICList.CustomMZSearchValues(intCustomMZCount - 1)

				If Not blnForceAcquisitionTimeMode Then
					ValidateCustomSICList()
				End If
			Else
				' Clear the custom mz search values array
				ReDim mCustomSICList.CustomMZSearchValues(-1)
			End If

		Catch ex As Exception
			' Ignore errors here
		End Try

		Return blnSuccess

	End Function

    Protected Function ParseCustomSICList(
      strMZList As String,
      strMZToleranceDaList As String,
      strScanCenterList As String,
      strScanToleranceList As String,
      strScanCommentList As String,
      strParameterFilePath As String) As Boolean

        Dim strDelimList() As Char = New Char() {","c, ControlChars.Tab}

        Dim intCustomMZCount As Integer

        Dim blnSuccess As Boolean

        blnSuccess = True

        ' Trim any trailing tab characters
        strMZList = strMZList.TrimEnd(ControlChars.Tab)
        strMZToleranceDaList = strMZToleranceDaList.TrimEnd(ControlChars.Tab)
        strScanCenterList = strScanCenterList.TrimEnd(ControlChars.Tab)
        strScanCommentList = strScanCommentList.TrimEnd(strDelimList)

        Dim lstMZs = strMZList.Split(strDelimList).ToList()
        Dim lstMZToleranceDa = strMZToleranceDaList.Split(strDelimList).ToList()
        Dim lstScanCenters = strScanCenterList.Split(strDelimList).ToList()
        Dim lstScanTolerances = strScanToleranceList.Split(strDelimList).ToList()
        Dim lstScanComments As List(Of String)

        If strScanCommentList.Length > 0 Then
            lstScanComments = strScanCommentList.Split(strDelimList).ToList()
        Else
            lstScanComments = New List(Of String)
        End If


        If lstMZs.Count > 0 Then

            mCustomSICList.RawTextMZList = strMZList
            mCustomSICList.RawTextMZToleranceDaList = strMZToleranceDaList
            mCustomSICList.RawTextScanOrAcqTimeCenterList = strScanCenterList
            mCustomSICList.RawTextScanOrAcqTimeToleranceList = strScanToleranceList

            ReDim mCustomSICList.CustomMZSearchValues(lstMZs.Count - 1)

            intCustomMZCount = 0
            For intIndex = 0 To lstMZs.Count - 1

                If IsNumber(lstMZs.Item(intIndex)) Then
                    Dim udtCustomSearchInfo As New udtCustomMZSearchSpecType

                    udtCustomSearchInfo.MZ = CDbl(lstMZs.Item(intIndex))
                    udtCustomSearchInfo.MZToleranceDa = 0
                    udtCustomSearchInfo.ScanOrAcqTimeCenter = 0         ' Set to 0 to indicate that the entire file should be searched
                    udtCustomSearchInfo.ScanOrAcqTimeTolerance = 0

                    If lstScanCenters.Count > intIndex Then
                        If IsNumber(lstScanCenters.Item(intIndex)) Then
                            If mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                                udtCustomSearchInfo.ScanOrAcqTimeCenter = CInt(lstScanCenters.Item(intIndex))
                            Else
                                ' Includes .Relative and .AcquisitionTime
                                udtCustomSearchInfo.ScanOrAcqTimeCenter = CSng(lstScanCenters.Item(intIndex))
                            End If
                        End If
                    End If

                    If lstScanTolerances.Count > intIndex Then
                        If IsNumber(lstScanTolerances.Item(intIndex)) Then
                            If mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                                udtCustomSearchInfo.ScanOrAcqTimeTolerance = CInt(lstScanTolerances.Item(intIndex))
                            Else
                                ' Includes .Relative and .AcquisitionTime
                                udtCustomSearchInfo.ScanOrAcqTimeTolerance = CSng(lstScanTolerances.Item(intIndex))
                            End If
                        End If
                    End If

                    If lstMZToleranceDa.Count > intIndex Then
                        If IsNumber(lstMZToleranceDa.Item(intIndex)) Then
                            udtCustomSearchInfo.MZToleranceDa = CDbl(lstMZToleranceDa.Item(intIndex))
                        End If
                    End If

                    If lstScanComments.Count > intIndex Then
                        udtCustomSearchInfo.Comment = lstScanComments.Item(intIndex)
                    Else
                        udtCustomSearchInfo.Comment = String.Empty
                    End If

                    mCustomSICList.CustomMZSearchValues(intCustomMZCount) = udtCustomSearchInfo

                    intCustomMZCount += 1
                End If

            Next

            If intCustomMZCount < mCustomSICList.CustomMZSearchValues.Length Then
                ReDim Preserve mCustomSICList.CustomMZSearchValues(intCustomMZCount - 1)
            End If

        Else
            ReDim mCustomSICList.CustomMZSearchValues(-1)
            mCustomSICList.RawTextMZList = String.Empty
            mCustomSICList.RawTextMZToleranceDaList = String.Empty
            mCustomSICList.RawTextScanOrAcqTimeCenterList = String.Empty
            mCustomSICList.RawTextScanOrAcqTimeToleranceList = String.Empty
        End If

        Return blnSuccess

    End Function

    Private Function LoadSpectraForFinniganDataFile(
      objXcaliburAccessor As XRawFileIO,
      objSpectraCache As clsSpectraCache,
      intScanNumber As Integer,
      ByRef udtScanInfo As udtScanInfoType,
      ByRef udtNoiseThresholdOptions As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseOptionsType,
      blnDiscardLowIntensityData As Boolean,
      blnCompressSpectraData As Boolean,
      dblMSDataResolution As Double,
      blnKeepRawSpectrum As Boolean) As Boolean


        Dim intIonIndex As Integer

        Dim dblTIC As Double

        Dim objMSSpectrum As New clsMSSpectrum
        Dim dblIntensityList() As Double = Nothing

        Dim blnDiscardLowIntensityDataWork As Boolean
        Dim blnCompressSpectraDataWork As Boolean

        Dim strLastKnownLocation As String = "Start"

        Try

            ' Load the ions for this scan

            strLastKnownLocation = "objXcaliburAccessor.GetScanData for scan " & intScanNumber

            ' Start a new thread to load the data, in case MSFileReader encounters a corrupt scan

            objMSSpectrum.IonCount = objXcaliburAccessor.GetScanData(intScanNumber, objMSSpectrum.IonsMZ, dblIntensityList)

            With udtScanInfo
                .IonCount = objMSSpectrum.IonCount
                .IonCountRaw = .IonCount
            End With

            If objMSSpectrum.IonCount > 0 Then
                If objMSSpectrum.IonCount <> objMSSpectrum.IonsMZ.Length Then
                    If objMSSpectrum.IonCount = 0 Then
                        Debug.WriteLine("LoadSpectraForFinniganDataFile: Survey Scan has IonCount = 0 -- Scan " & intScanNumber, "LoadSpectraForFinniganDataFile")
                    Else
                        Debug.WriteLine("LoadSpectraForFinniganDataFile: Survey Scan found where IonCount <> dblMZList.Length -- Scan " & intScanNumber, "LoadSpectraForFinniganDataFile")
                    End If
                End If

                Dim sortRequired As Boolean = False

                For intIndex = 1 To objMSSpectrum.IonCount - 1
                    ' Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z, 
                    ' we have observed a few cases in certain scans of certain datasets that points with 
                    ' similar m/z values are swapped and ths slightly out of order
                    ' The following if statement checks for this
                    If (objMSSpectrum.IonsMZ(intIndex) < objMSSpectrum.IonsMZ(intIndex - 1)) Then
                        sortRequired = True
                        Exit For
                    End If
                Next intIndex

                If sortRequired Then
                    Array.Sort(objMSSpectrum.IonsMZ, dblIntensityList)
                End If

            Else
                objMSSpectrum.IonCount = 0
            End If

            With objMSSpectrum
                .ScanNumber = intScanNumber

                strLastKnownLocation = "Redim .IonsIntensity(" & .IonCount.ToString & " - 1)"
                ReDim .IonsIntensity(.IonCount - 1)

                ' Copy the intensity data; and compute the total scan intensity
                dblTIC = 0
                For intIonIndex = 0 To .IonCount - 1
                    .IonsIntensity(intIonIndex) = CSng(dblIntensityList(intIonIndex))
                    dblTIC += dblIntensityList(intIonIndex)
                Next intIonIndex
            End With

            ' Determine the minimum positive intensity in this scan
            strLastKnownLocation = "Call mMASICPeakFinder.FindMinimumPositiveValue"
            udtScanInfo.MinimumPositiveIntensity = mMASICPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

            If objMSSpectrum.IonCount > 0 Then
                With udtScanInfo
                    If .TotalIonIntensity < Single.Epsilon Then
                        .TotalIonIntensity = CSng(Math.Min(dblTIC, Single.MaxValue))
                    End If
                End With


                If udtScanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                    blnDiscardLowIntensityDataWork = blnDiscardLowIntensityData
                    blnCompressSpectraDataWork = blnCompressSpectraData
                Else
                    blnDiscardLowIntensityDataWork = False
                    blnCompressSpectraDataWork = False
                End If

                strLastKnownLocation = "Call ProcessAndStoreSpectrum"
                ProcessAndStoreSpectrum(udtScanInfo, objSpectraCache, objMSSpectrum, udtNoiseThresholdOptions, blnDiscardLowIntensityDataWork, blnCompressSpectraDataWork, dblMSDataResolution, blnKeepRawSpectrum)
            Else
                udtScanInfo.TotalIonIntensity = 0
            End If

        Catch ex As Exception
            LogErrors("LoadSpectraForFinniganDataFile", "Error in LoadSpectraForFinniganDataFile (LastKnownLocation: " & strLastKnownLocation & ")", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

        Return True

    End Function

	Private Sub LogErrors(
	  strSource As String, 
	  strMessage As String, 
	  ex As Exception, 
	  Optional blnAllowInformUser As Boolean = True, 
	  Optional blnAllowThrowingException As Boolean = True, 
	  Optional eNewErrorCode As eMasicErrorCodes = eMasicErrorCodes.NoError)
	  
		Dim strMessageWithoutCRLF As String

		mStatusMessage = String.Copy(strMessage)

		strMessageWithoutCRLF = mStatusMessage.Replace(ControlChars.NewLine, "; ")

		If ex Is Nothing Then
			ex = New Exception("Error")
		Else
			If Not ex.Message Is Nothing AndAlso ex.Message.Length > 0 Then
				strMessageWithoutCRLF &= "; " & ex.Message
			End If
		End If

		' Show the message and log to the clsProcessFilesBaseClass logger
		ShowErrorMessage(strSource & ": " & strMessageWithoutCRLF, True)

		If Not eNewErrorCode = eMasicErrorCodes.NoError Then
			SetLocalErrorCode(eNewErrorCode, True)
		End If

		If MyBase.ShowMessages AndAlso blnAllowInformUser Then
			Windows.Forms.MessageBox.Show(mStatusMessage & ControlChars.NewLine & ex.Message, "Error", Windows.Forms.MessageBoxButtons.OK, Windows.Forms.MessageBoxIcon.Exclamation)
		ElseIf blnAllowThrowingException Then
			Throw New Exception(mStatusMessage, ex)
		End If
	End Sub

	Private Function LookupDatasetNumber(
      strInputFilePath As String,
      strDatasetLookupFilePath As String,
      intDefaultDatasetNumber As Integer) As Integer

		' First tries to poll the database for the dataset number
		' If this doesn't work, then looks for the dataset name in mDatasetLookupFilePath

		Dim strFileNameCompare As String
		Dim intNewDatasetNumber As Integer

		Dim strAvoidErrorMessage As String

		Dim blnDatasetFoundInDB As Boolean
		Dim objDBTools As PRISM.DataBase.clsDBTools
		Dim objDBLogger As PRISM.Logging.ILogger = Nothing

		Dim intRowCount As Integer
		Dim dsDatasetInfo As DataSet = Nothing
		Dim objRow As DataRow

		' Initialize intNewDatasetNumber and strFileNameCompare
		strFileNameCompare = Path.GetFileNameWithoutExtension(strInputFilePath).ToUpper
		intNewDatasetNumber = intDefaultDatasetNumber

		strAvoidErrorMessage = "To avoid seeing this message in the future, clear the 'SQL Server Connection String' and 'Dataset Info Query SQL' entries on the Advanced tab."

		blnDatasetFoundInDB = False

		If Not mDatabaseConnectionString Is Nothing AndAlso mDatabaseConnectionString.Length > 0 Then
			' Attempt to lookup the dataset number in the database
			Try
				objDBTools = New PRISM.DataBase.clsDBTools(objDBLogger, mDatabaseConnectionString)

				Dim intTextCol As Integer = -1
				Dim intDatasetIDCol As Integer = -1
				Dim blnQueryingSingleDataset As Boolean = False

				Dim strQuery = String.Copy(mDatasetInfoQuerySql)
				If strQuery.ToUpper.StartsWith("SELECT DATASET") Then
					' Add a where clause to the query
					strQuery &= " WHERE Dataset = '" & strFileNameCompare & "'"
					blnQueryingSingleDataset = True
				End If

				If objDBTools.GetDiscDataSet(strQuery, dsDatasetInfo, intRowCount) Then
					If intRowCount > 0 Then
						With dsDatasetInfo.Tables(0)
							If .Columns(0).DataType Is Type.GetType("System.String") Then
								' First column is text; make sure the second is a number
								If Not .Columns(1).DataType Is Type.GetType("System.String") Then
									intTextCol = 0
									intDatasetIDCol = 1
								End If
							Else
								' First column is not text; make sure the second is text
								If .Columns(1).DataType Is Type.GetType("System.String") Then
									intTextCol = 1
									intDatasetIDCol = 0
								End If
							End If
						End With

						If intTextCol >= 0 Then
							' Find the row in the datatable that matches strFileNameCompare
							For Each objRow In dsDatasetInfo.Tables(0).Rows
								If CStr(objRow.Item(intTextCol)).ToUpper = strFileNameCompare Then
									' Match found
									Try
										intNewDatasetNumber = CInt(objRow.Item(intDatasetIDCol))
										blnDatasetFoundInDB = True
									Catch ex As Exception
										Try
											LogErrors("LookupDatasetNumber", "Error converting '" & objRow.Item(intDatasetIDCol).ToString & "' to a dataset ID", ex, True, False, eMasicErrorCodes.InvalidDatasetNumber)
										Catch ex2 As Exception
											LogErrors("LookupDatasetNumber", "Error converting column " & intDatasetIDCol.ToString & " from the dataset report to a dataset ID", ex, True, False, eMasicErrorCodes.InvalidDatasetNumber)
										End Try
										blnDatasetFoundInDB = False
									End Try
									Exit For
								End If
							Next objRow
						End If

						If Not blnDatasetFoundInDB AndAlso blnQueryingSingleDataset Then
							Try
								Integer.TryParse(dsDatasetInfo.Tables(0).Rows(0).Item(1).ToString(), intNewDatasetNumber)
								blnDatasetFoundInDB = True
							Catch ex As Exception
								' Ignore errors here
							End Try

						End If
					End If
				End If

			Catch ex2 As NullReferenceException
				LogErrors("LookupDatasetNumber", "Error connecting to database: " & mDatabaseConnectionString & ControlChars.NewLine & strAvoidErrorMessage, Nothing, True, False, eMasicErrorCodes.InvalidDatasetNumber)
				blnDatasetFoundInDB = False
			Catch ex As Exception
				LogErrors("LookupDatasetNumber", "Error connecting to database: " & mDatabaseConnectionString & ControlChars.NewLine & strAvoidErrorMessage, ex, True, False, eMasicErrorCodes.InvalidDatasetNumber)
				blnDatasetFoundInDB = False
			End Try
		End If

		If Not blnDatasetFoundInDB AndAlso Not String.IsNullOrWhiteSpace(strDatasetLookupFilePath) Then

			' Lookup the dataset number in the dataset lookup file

			Dim strLineIn As String
			Dim strSplitLine() As String

            Dim strDelimList = New Char() {" "c, ","c, ControlChars.Tab}

			Try
                Using srInFile = New StreamReader(strDatasetLookupFilePath)
                    Do While Not srInFile.EndOfStream
                        strLineIn = srInFile.ReadLine
                        If strLineIn Is Nothing Then Continue Do

                        If strLineIn.Length < strFileNameCompare.Length Then Continue Do

                        If strLineIn.Substring(0, strFileNameCompare.Length).ToUpper() <> strFileNameCompare Then Continue Do

                        strSplitLine = strLineIn.Split(strDelimList)
                        If strSplitLine.Length < 2 Then Continue Do

                        If IsNumber(strSplitLine(1)) Then
                            intNewDatasetNumber = CInt(strSplitLine(1))
                            Exit Do
                        Else
                            SetLocalErrorCode(eMasicErrorCodes.InvalidDatasetNumber)
                            Exit Do
                        End If

                    Loop
                End Using

			Catch ex As Exception
				SetLocalErrorCode(eMasicErrorCodes.InvalidDatasetLookupFilePath)
			End Try

		End If

		Return intNewDatasetNumber

	End Function

	Private Function LookupRTByScanNumber(
      ByRef udtScanList() As udtScanInfoType,
      intScanListCount As Integer, 
      intScanListArray() As Integer, 
      intScanNumberToFind As Integer) As Single

		' intScanListArray() must be populated with the scan numbers in udtScanList() before calling this function

		Dim intScanIndex As Integer
		Dim intMatchIndex As Integer

		Try
			intMatchIndex = Array.IndexOf(intScanListArray, intScanNumberToFind)

			If intMatchIndex < 0 Then
				' Need to find the closest scan with this scan number
				intMatchIndex = 0
				For intScanIndex = 0 To intScanListCount - 1
					If udtScanList(intScanIndex).ScanNumber <= intScanNumberToFind Then intMatchIndex = intScanIndex
				Next intScanIndex
			End If

			Return udtScanList(intMatchIndex).ScanTime
		Catch ex As Exception
			' Ignore any errors that occur in this function
			LogErrors("LookupRTByScanNumber", "Error in LookupRTByScanNumber", ex, True, False)
			Return 0
		End Try

	End Function

	Private Function MRMParentDaughterMatch(
      ByRef udtSRMListEntry As udtSRMListType,
      ByRef udtMRMSettingEntry As udtMRMScanInfoType,
      intMRMMassIndex As Integer) As Boolean

        Return MRMParentDaughterMatch(
          udtSRMListEntry.ParentIonMZ, 
          udtSRMListEntry.CentralMass,
          udtMRMSettingEntry.ParentIonMZ, 
          udtMRMSettingEntry.MRMMassList(intMRMMassIndex).CentralMass)
	End Function

	Private Function MRMParentDaughterMatch(
      ByRef udtSRMListEntry As udtSRMListType,
      dblParentIonMZ As Double,
      dblMRMDaughterMZ As Double) As Boolean

        Return MRMParentDaughterMatch(
          udtSRMListEntry.ParentIonMZ, 
          udtSRMListEntry.CentralMass,
          dblParentIonMZ, 
          dblMRMDaughterMZ)
	End Function

	Private Function MRMParentDaughterMatch(
      dblParentIonMZ1 As Double,
      dblMRMDaughterMZ1 As Double,
      dblParentIonMZ2 As Double,
      dblMRMDaughterMZ2 As Double) As Boolean


		Const COMPARISON_TOLERANCE As Double = 0.01

        If Math.Abs(dblParentIonMZ1 - dblParentIonMZ2) <= COMPARISON_TOLERANCE AndAlso
           Math.Abs(dblMRMDaughterMZ1 - dblMRMDaughterMZ2) <= COMPARISON_TOLERANCE Then
            Return True
        Else
            Return False
        End If

	End Function

	Private Sub OpenOutputFileHandles(
	  strInputFileName As String, 
	  strOutputFolderPath As String, 
	  ByRef udtOutputFileHandles As udtOutputFileHandlesType, 
	  blnWriteHeaders As Boolean)

		Dim strOutputFilePath As String

		With udtOutputFileHandles

			' Scan Stats file
			strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsFlatFile)
			.ScanStats = New StreamWriter(strOutputFilePath, False)
			If blnWriteHeaders Then .ScanStats.WriteLine(GetHeadersForOutputFile(Nothing, eOutputFileTypeConstants.ScanStatsFlatFile))

			.MSMethodFilePathBase = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MSMethodFile)
			.MSTuneFilePathBase = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MSTuneFile)
		End With

	End Sub

	Private Sub PopulateScanListPointerArray(
	  ByRef udtSurveyScans() As udtScanInfoType, 
	  intSurveyScanCount As Integer, 
	  ByRef intScanListArray() As Integer)

		Dim intIndex As Integer

		If intSurveyScanCount > 0 Then
			ReDim intScanListArray(intSurveyScanCount - 1)

			For intIndex = 0 To intSurveyScanCount - 1
				intScanListArray(intIndex) = udtSurveyScans(intIndex).ScanNumber
			Next intIndex
		Else
			ReDim intScanListArray(0)
		End If

	End Sub

    Private Function ProcessAndStoreSpectrum(
      ByRef udtScanInfo As udtScanInfoType,
      ByRef objSpectraCache As clsSpectraCache,
      objMSSpectrum As clsMSSpectrum,
      ByRef udtNoiseThresholdOptions As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseOptionsType,
      blnDiscardLowIntensityData As Boolean,
      blnCompressSpectraData As Boolean,
      dblMSDataResolution As Double,
      blnKeepRawSpectrum As Boolean) As Boolean


        Dim blnSuccess As Boolean
        Dim intMaxAllowableIonCount As Integer

        Static intSpectraStored As Integer = 0
        Static intSpectraFoundExceedingMaxIonCount As Integer = 0
        Static intMaxIonCountReported As Integer = 0

        Dim strLastKnownLocation As String = "Start"

        Try
            blnSuccess = False

            ' Determine the noise threshold intensity for this spectrum
            strLastKnownLocation = "Call ComputeNoiseLevelForMassSpectrum"
            ComputeNoiseLevelForMassSpectrum(udtScanInfo, objMSSpectrum, udtNoiseThresholdOptions)

            If blnKeepRawSpectrum Then

                ' Do not discard low intensity data for MRM scans
                If udtScanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                    If blnDiscardLowIntensityData Then
                        ' Discard data below the noise level or below the minimum S/N level
                        ' If we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                        strLastKnownLocation = "Call DiscardDataBelowNoiseThreshold"
                        DiscardDataBelowNoiseThreshold(objMSSpectrum, udtScanInfo.BaselineNoiseStats.NoiseLevel, mMZIntensityFilterIgnoreRangeStart, mMZIntensityFilterIgnoreRangeEnd, udtNoiseThresholdOptions)
                        udtScanInfo.IonCount = objMSSpectrum.IonCount
                    End If
                End If

                If blnCompressSpectraData Then
                    strLastKnownLocation = "Call CompressSpectraData"
                    ' Again, if we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                    CompressSpectraData(objMSSpectrum, dblMSDataResolution, mMZIntensityFilterIgnoreRangeStart, mMZIntensityFilterIgnoreRangeEnd)
                End If

                intMaxAllowableIonCount = MAX_ALLOWABLE_ION_COUNT
                If objMSSpectrum.IonCount > intMaxAllowableIonCount Then
                    ' Do not keep more than 50,000 ions
                    strLastKnownLocation = "Call DiscardDataToLimitIonCount"
                    intSpectraFoundExceedingMaxIonCount += 1

                    ' Display a message at the console the first 10 times we encounter spectra with over intMaxAllowableIonCount ions
                    ' In addition, display a new message every time a new max value is encountered
                    If intSpectraFoundExceedingMaxIonCount <= 10 OrElse objMSSpectrum.IonCount > intMaxIonCountReported Then
                        Console.WriteLine()
                        Console.WriteLine("Note: Scan " & udtScanInfo.ScanNumber & " has " & objMSSpectrum.IonCount & " ions; will only retain " & intMaxAllowableIonCount & " (trimmed " & intSpectraFoundExceedingMaxIonCount.ToString & " spectra)")

                        intMaxIonCountReported = objMSSpectrum.IonCount
                    End If

                    DiscardDataToLimitIonCount(objMSSpectrum, mMZIntensityFilterIgnoreRangeStart, mMZIntensityFilterIgnoreRangeEnd, intMaxAllowableIonCount)
                    udtScanInfo.IonCount = objMSSpectrum.IonCount
                End If

                strLastKnownLocation = "Call AddSpectrumToPool"
                blnSuccess = objSpectraCache.AddSpectrumToPool(objMSSpectrum, udtScanInfo.ScanNumber, 0)
            Else
                blnSuccess = True
            End If

        Catch ex As Exception
            LogErrors("ProcessAndStoreSpectrum", "Error in ProcessAndStoreSpectrum (LastKnownLocation: " & strLastKnownLocation & ")", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
        End Try

        Return blnSuccess

    End Function

	' Main processing function
	Public Overloads Overrides Function ProcessFile(
      strInputFilePath As String,
      strOutputFolderPath As String,
      strParameterFilePath As String,
      blnResetErrorCode As Boolean) As Boolean


		Dim udtSICOptions As udtSICOptionsType
		Dim udtBinningOptions As clsCorrelation.udtBinningOptionsType

        Dim udtScanList = New udtScanListType
        Dim udtMRMSettings() As udtMRMScanInfoType = Nothing
		Dim udtSRMList() As udtSRMListType = Nothing

		Dim ioFileInfo As FileInfo

		Dim blnSuccess, blnDoNotProcess As Boolean
		Dim blnKeepRawMSSpectra As Boolean

		Dim strInputFilePathFull As String = String.Empty
		Dim strInputFileName As String = String.Empty
		Dim udtOutputFileHandles As udtOutputFileHandlesType = New udtOutputFileHandlesType
		Dim intSimilarParentIonUpdateCount As Integer

		If blnResetErrorCode Then
			SetLocalErrorCode(eMasicErrorCodes.NoError)
		End If

		mSubtaskProcessingStepPct = 0
		UpdateProcessingStep(eProcessingStepConstants.NewTask, True)
		MyBase.ResetProgress("Starting calculations")

		mStatusMessage = String.Empty

		mScanStats = New List(Of DSSummarizer.clsScanStatsEntry)

        mExtendedHeaderInfo = New List(Of KeyValuePair(Of String, Integer))

		UpdateStatusFile(True)

		If Not LoadParameterFileSettings(strParameterFilePath) Then
			mStatusMessage = "Parameter file load error: " & strParameterFilePath
			If MyBase.ShowMessages Then
				Windows.Forms.MessageBox.Show(mStatusMessage, "Error", Windows.Forms.MessageBoxButtons.OK, Windows.Forms.MessageBoxIcon.Exclamation)
			End If

			MyBase.ShowErrorMessage(mStatusMessage)

			If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError Then
				MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidParameterFile)
			End If
			UpdateProcessingStep(eProcessingStepConstants.Cancelled, True)

			LogMessage("Processing ended in error")
			Return False
		End If

		' Copy these to avoid issues with users making changes to mSICOptions in the middle of processing
		udtSICOptions = mSICOptions
		udtBinningOptions = mBinningOptions

		Try
			' If a Custom SICList file is defined, then load the custom SIC values now
			If Me.CustomSICListFileName.Length > 0 Then
				LogMessage("ProcessFile: Reading custom SIC values file: " & Me.CustomSICListFileName)
				blnSuccess = LoadCustomSICListFromFile(Me.CustomSICListFileName)
				If Not blnSuccess Then
					If mLocalErrorCode = eMasicErrorCodes.NoError Then MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.UnspecifiedError)
					Exit Try
				End If
			End If

			UpdateMZIntensityFilterIgnoreRange()

			LogMessage("Source data file: " & strInputFilePath)

			If strInputFilePath Is Nothing OrElse strInputFilePath.Length = 0 Then
				ShowErrorMessage("Input file name is empty")
				MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
				Exit Try
			End If


			mStatusMessage = "Parsing " & Path.GetFileName(strInputFilePath)
			Console.WriteLine()
			ShowMessage(mStatusMessage)

			blnSuccess = CleanupFilePaths(strInputFilePath, strOutputFolderPath)

			If blnSuccess Then
				udtSICOptions.DatasetNumber = LookupDatasetNumber(strInputFilePath, mDatasetLookupFilePath, udtSICOptions.DatasetNumber)
				mSICOptions.DatasetNumber = udtSICOptions.DatasetNumber

				If Me.LocalErrorCode <> eMasicErrorCodes.NoError Then
					If Me.LocalErrorCode = eMasicErrorCodes.InvalidDatasetNumber OrElse Me.LocalErrorCode = eMasicErrorCodes.InvalidDatasetLookupFilePath Then
						' Ignore this error
						Me.SetLocalErrorCode(eMasicErrorCodes.NoError)
						blnSuccess = True
					Else
						blnSuccess = False
					End If
				End If
			End If

			If Not blnSuccess Then
				If mLocalErrorCode = eMasicErrorCodes.NoError Then MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.FilePathError)
				Exit Try
			End If

			Try
				'---------------------------------------------------------
				' See if an output XML file already exists
				' If it does, open it and read the parameters used
				' If they match the current analysis parameters, and if the input file specs match the input file, then
				'  do not reprocess
				'---------------------------------------------------------

				' Obtain the full path to the input file
				ioFileInfo = New FileInfo(strInputFilePath)
				strInputFilePathFull = ioFileInfo.FullName

				LogMessage("Checking for existing results in the output path: " & strOutputFolderPath)

				blnDoNotProcess = CheckForExistingResults(strInputFilePathFull, strOutputFolderPath, udtSICOptions, udtBinningOptions)

				If blnDoNotProcess Then
					LogMessage("Existing results found; data will not be reprocessed")
				End If

			Catch ex As Exception
				blnSuccess = False
				LogErrors("ProcessFile", "Error checking for existing results file", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
			End Try

			If blnDoNotProcess Then
				blnSuccess = True
				Exit Try
			End If

			Try
				'---------------------------------------------------------
				' Verify that we have write access to the output folder
				'---------------------------------------------------------

				' The following should work for testing access permissions, but it doesn't
				'Dim objFilePermissionTest As New Security.Permissions.FileIOPermission(Security.Permissions.FileIOPermissionAccess.AllAccess, strOutputFolderPath)
				'' The following should throw an exception if the current user doesn't have read/write access; however, no exception is thrown for me
				'objFilePermissionTest.Demand()
				'objFilePermissionTest.Assert()

				LogMessage("Checking for write permission in the output path: " & strOutputFolderPath)

				Dim strOutputFileTestPath As String
				strOutputFileTestPath = Path.Combine(strOutputFolderPath, "TestOutputFile" & DateTime.UtcNow.Ticks & ".tmp")

				Dim fsOutFileTest As New StreamWriter(strOutputFileTestPath, False)

				fsOutFileTest.WriteLine("Test")
				fsOutFileTest.Flush()
				fsOutFileTest.Close()

				' Wait 250 msec, then delete the file
				Threading.Thread.Sleep(250)
				IO.File.Delete(strOutputFileTestPath)

			Catch ex As Exception
				blnSuccess = False
				LogErrors("ProcessFile", "The current user does not have write permission for the output folder: " & strOutputFolderPath, ex, True, False, eMasicErrorCodes.FileIOPermissionsError)
			End Try

			If Not blnSuccess Then
				SetLocalErrorCode(eMasicErrorCodes.FileIOPermissionsError)
				Exit Try
			End If

			'---------------------------------------------------------
			' Reset the processing stats
			'---------------------------------------------------------

			InitializeMemoryManagementOptions(mProcessingStats)

            Dim objSpectraCache = New clsSpectraCache
			With objSpectraCache
				.ShowMessages = MyBase.ShowMessages
				.DiskCachingAlwaysDisabled = Me.DiskCachingAlwaysDisabled
				.CacheFolderPath = Me.CacheFolderPath
				.CacheSpectraToRetainInMemory = Me.CacheSpectraToRetainInMemory
				.CacheMinimumFreeMemoryMB = Me.CacheMinimumFreeMemoryMB
				.CacheMaximumMemoryUsageMB = Me.CacheMaximumMemoryUsageMB

				.InitializeSpectraPool()
			End With

			Try
				'---------------------------------------------------------
				' Define strInputFileName (which is referenced several times below)
				'---------------------------------------------------------
				strInputFileName = Path.GetFileName(strInputFilePathFull)

				'---------------------------------------------------------
				' Create the _ScanStats.txt file
				'---------------------------------------------------------
				OpenOutputFileHandles(strInputFileName, strOutputFolderPath, udtOutputFileHandles, mIncludeHeadersInExportFile)

				'---------------------------------------------------------
				' Read the mass spectra from the input data file
				'---------------------------------------------------------

				UpdateProcessingStep(eProcessingStepConstants.ReadDataFile)
				SetSubtaskProcessingStepPct(0)
				UpdatePeakMemoryUsage()
				mStatusMessage = String.Empty

				If mSkipSICAndRawDataProcessing Then
					mExportRawDataOnly = False
				End If

				blnKeepRawMSSpectra = Not mSkipSICAndRawDataProcessing OrElse mExportRawDataOnly

				ValidateSICOptions(udtSICOptions)

				Select Case Path.GetExtension(strInputFilePath).ToUpper
					Case FINNIGAN_RAW_FILE_EXTENSION.ToUpper
						' Open the .Raw file and obtain the scan information

                        blnSuccess = ExtractScanInfoFromXcaliburDataFile(
                          strInputFilePathFull, udtSICOptions.DatasetNumber,
                          udtScanList, objSpectraCache, udtOutputFileHandles,
                          udtSICOptions, udtBinningOptions, mStatusMessage,
                          blnKeepRawMSSpectra, Not mSkipMSMSProcessing)

					Case MZ_XML_FILE_EXTENSION1.ToUpper, MZ_XML_FILE_EXTENSION2.ToUpper
						' Open the .mzXML file and obtain the scan information
                        blnSuccess = ExtractScanInfoFromMZXMLDataFile(
                          strInputFilePathFull, udtSICOptions.DatasetNumber,
                          udtScanList, objSpectraCache, udtOutputFileHandles,
                          udtSICOptions, udtBinningOptions,
                          blnKeepRawMSSpectra, Not mSkipMSMSProcessing)

					Case MZ_DATA_FILE_EXTENSION1.ToUpper, MZ_DATA_FILE_EXTENSION2.ToUpper
						' Open the .mzXML file and obtain the scan information
                        blnSuccess = ExtractScanInfoFromMZDataFile(
                          strInputFilePathFull, udtSICOptions.DatasetNumber,
                          udtScanList, objSpectraCache, udtOutputFileHandles,
                          udtSICOptions, udtBinningOptions,
                          blnKeepRawMSSpectra, Not mSkipMSMSProcessing)

					Case AGILENT_MSMS_FILE_EXTENSION.ToUpper, AGILENT_MS_FILE_EXTENSION.ToUpper
						' Open the .MGF and .CDF files to obtain the scan information
                        blnSuccess = ExtractScanInfoFromMGFandCDF(
                          strInputFilePathFull, udtSICOptions.DatasetNumber,
                          udtScanList, objSpectraCache, udtOutputFileHandles,
                          udtSICOptions, udtBinningOptions, mStatusMessage,
                          blnKeepRawMSSpectra, Not mSkipMSMSProcessing)
					Case Else
						mStatusMessage = "Unknown file extension: " & Path.GetExtension(strInputFilePathFull)
						SetLocalErrorCode(eMasicErrorCodes.UnknownFileExtension)
						blnSuccess = False
				End Select

				If Not blnSuccess Then
					SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError, True)
				End If
			Catch ex As Exception
				blnSuccess = False
				LogErrors("ProcessFile", "Error accessing input data file: " & strInputFilePathFull, ex, True, True, eMasicErrorCodes.InputFileDataReadError)
			End Try

			' Record that the file is finished loading
			mProcessingStats.FileLoadEndTime = DateTime.UtcNow

            If Not blnSuccess Then
                If mStatusMessage Is Nothing OrElse mStatusMessage.Length = 0 Then
                    mStatusMessage = "Unable to parse file; unknown error"
                Else
                    mStatusMessage = "Unable to parse file: " & mStatusMessage
                End If
                If MyBase.ShowMessages Then
                    Windows.Forms.MessageBox.Show(mStatusMessage, "Error", Windows.Forms.MessageBoxButtons.OK, Windows.Forms.MessageBoxIcon.Exclamation)
                    LogMessage(mStatusMessage, eMessageTypeConstants.ErrorMsg)
                Else
                    MyBase.ShowErrorMessage(mStatusMessage)
                End If
                Exit Try
            End If

			Try
				' Make sure the arrays in udtScanList range from 0 to the Count-1
				With udtScanList
					If .SurveyScanCount <> .SurveyScans.Length Then ReDim Preserve .SurveyScans(.SurveyScanCount - 1)
					If .FragScanCount <> .FragScans.Length Then ReDim Preserve .FragScans(.FragScanCount - 1)
					If .MasterScanOrderCount <> .MasterScanOrder.Length Then
						ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
						ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
						ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)
					End If

					If .ParentIonInfoCount <> .ParentIons.Length Then ReDim Preserve .ParentIons(.ParentIonInfoCount - 1)
				End With
			Catch ex As Exception
				blnSuccess = False
				LogErrors("ProcessFile", "Error resizing the arrays in udtScanList", ex, True, False, eMasicErrorCodes.UnspecifiedError)
				Exit Try
			End Try

			Try
				'---------------------------------------------------------
				' Save the BPIs and TICs
				'---------------------------------------------------------

				UpdateProcessingStep(eProcessingStepConstants.SaveBPI)
				UpdateOverallProgress("Processing Data for " & strInputFileName)
				SetSubtaskProcessingStepPct(0, "Saving chromatograms to disk")
				UpdatePeakMemoryUsage()

				If mSkipSICAndRawDataProcessing OrElse Not mExportRawDataOnly Then
					LogMessage("ProcessFile: Call SaveBPIs")
					SaveBPIs(udtScanList, objSpectraCache, strInputFilePathFull, strOutputFolderPath)
				End If

				'---------------------------------------------------------
				' Close the ScanStats file handle
				'---------------------------------------------------------
				Try
					LogMessage("ProcessFile: Close udtOutputFileHandles.ScanStats")

					If Not udtOutputFileHandles.ScanStats Is Nothing Then
						udtOutputFileHandles.ScanStats.Close()
					End If

				Catch ex As Exception
					' Ignore errors here
				End Try

				'---------------------------------------------------------
				' Create the DatasetInfo XML file
				'---------------------------------------------------------

				LogMessage("ProcessFile: Create DatasetInfo File")
				CreateDatasetInfoFile(strInputFileName, strOutputFolderPath)

				If mSkipSICAndRawDataProcessing Then
					LogMessage("ProcessFile: Skipping SIC Processing")

					SetDefaultPeakLocValues(udtScanList)
				Else

					'---------------------------------------------------------
					' Optionally, export the raw mass spectra data
					'---------------------------------------------------------
					If mRawDataExportOptions.ExportEnabled Then
						ExportRawDataToDisk(udtScanList, objSpectraCache, strInputFileName, strOutputFolderPath)
					End If

					If mReporterIonStatsEnabled Then
						' Look for Reporter Ions in the Fragmentation spectra
						FindReporterIons(udtSICOptions, udtScanList, objSpectraCache, strInputFileName, strOutputFolderPath)
					End If

					'---------------------------------------------------------
					' If MRM data is present, then save the MRM values to disk
					'---------------------------------------------------------
					If udtScanList.MRMDataPresent Then
						blnSuccess = DetermineMRMSettings(udtScanList, udtMRMSettings, udtSRMList)

						If blnSuccess Then
							ExportMRMDataToDisk(udtScanList, objSpectraCache, udtMRMSettings, udtSRMList, strInputFileName, strOutputFolderPath)
						End If
					End If


					If Not mExportRawDataOnly Then

						'---------------------------------------------------------
						' Add the custom SIC values to udtScanList
						'---------------------------------------------------------
						AddCustomSICValues(udtScanList, udtSICOptions.SICTolerance, udtSICOptions.SICToleranceIsPPM, mCustomSICList.ScanOrAcqTimeTolerance)


						'---------------------------------------------------------
						' Possibly create the Tab-separated values SIC details output file
						'---------------------------------------------------------
						blnSuccess = InitializeSICDetailsTextFile(strInputFilePathFull, strOutputFolderPath, udtOutputFileHandles)
						If Not blnSuccess Then
							SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError)
							Exit Try
						End If

						'---------------------------------------------------------
						' Create the XML output file
						'---------------------------------------------------------
						blnSuccess = XMLOutputFileInitialize(strInputFilePathFull, strOutputFolderPath, udtOutputFileHandles, udtScanList, objSpectraCache, udtSICOptions, udtBinningOptions)
						If Not blnSuccess Then
							SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError)
							Exit Try
						End If

						'---------------------------------------------------------
						' Create the SICs
						' For each one, find the peaks and make an entry to the XML output file
						'---------------------------------------------------------

						UpdateProcessingStep(eProcessingStepConstants.CreateSICsAndFindPeaks)
						SetSubtaskProcessingStepPct(0)
						UpdatePeakMemoryUsage()

						LogMessage("ProcessFile: Call CreateParentIonSICs")
						blnSuccess = CreateParentIonSICs(udtScanList, objSpectraCache, udtSICOptions, udtOutputFileHandles)

						If Not blnSuccess Then
							SetLocalErrorCode(eMasicErrorCodes.CreateSICsError, True)
							Exit Try
						End If

					End If


					If Not (mSkipMSMSProcessing OrElse mExportRawDataOnly) Then

						'---------------------------------------------------------
						' Find Similar Parent Ions
						'---------------------------------------------------------

						UpdateProcessingStep(eProcessingStepConstants.FindSimilarParentIons)
						SetSubtaskProcessingStepPct(0)
						UpdatePeakMemoryUsage()

						LogMessage("ProcessFile: Call FindSimilarParentIons")
						blnSuccess = FindSimilarParentIons(udtScanList, objSpectraCache, udtSICOptions, udtBinningOptions, intSimilarParentIonUpdateCount)

						If Not blnSuccess Then
							SetLocalErrorCode(eMasicErrorCodes.FindSimilarParentIonsError, True)
							Exit Try
						End If
					End If

				End If

                If mWriteExtendedStats AndAlso Not mExportRawDataOnly Then
                    '---------------------------------------------------------
                    ' Save Extended Scan Stats Files
                    '---------------------------------------------------------

                    UpdateProcessingStep(eProcessingStepConstants.SaveExtendedScanStatsFiles)
                    SetSubtaskProcessingStepPct(0)
                    UpdatePeakMemoryUsage()

                    LogMessage("ProcessFile: Call SaveExtendedScanStatsFiles")
                    blnSuccess = SaveExtendedScanStatsFiles(udtScanList, strInputFileName, strOutputFolderPath, udtSICOptions, mIncludeHeadersInExportFile)

                    If Not blnSuccess Then
                        SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError, True)
                        Exit Try
                    End If
                End If


				'---------------------------------------------------------
				' Save SIC Stats Flat File
				'---------------------------------------------------------

				UpdateProcessingStep(eProcessingStepConstants.SaveSICStatsFlatFile)
				SetSubtaskProcessingStepPct(0)
				UpdatePeakMemoryUsage()

				If Not mExportRawDataOnly Then
					LogMessage("ProcessFile: Call SaveSICStatsFlatFile")
					blnSuccess = SaveSICStatsFlatFile(udtScanList, strInputFileName, strOutputFolderPath, udtSICOptions, mIncludeHeadersInExportFile, mIncludeScanTimesInSICStatsFile)

					If Not blnSuccess Then
						SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError, True)
						Exit Try
					End If
				End If


				UpdateProcessingStep(eProcessingStepConstants.CloseOpenFileHandles)
				SetSubtaskProcessingStepPct(0)
				UpdatePeakMemoryUsage()

				If Not (mSkipSICAndRawDataProcessing OrElse mExportRawDataOnly) Then

					'---------------------------------------------------------
					' Write processing stats to the XML output file
					'---------------------------------------------------------

					LogMessage("ProcessFile: Call FinalizeXMLFile")
					blnSuccess = XMLOutputFileFinalize(udtOutputFileHandles, udtScanList, objSpectraCache)

				End If

				'---------------------------------------------------------
				' Close any open output files
				'---------------------------------------------------------
				CloseOutputFileHandles(udtOutputFileHandles)

				'---------------------------------------------------------
				' Save a text file containing the headers used in the text files
				'---------------------------------------------------------
				If Not mIncludeHeadersInExportFile Then
					LogMessage("ProcessFile: Call SaveHeaderGlossary")
					SaveHeaderGlossary(udtScanList, strInputFileName, strOutputFolderPath)
				End If

				If Not (mSkipSICAndRawDataProcessing OrElse mExportRawDataOnly) AndAlso intSimilarParentIonUpdateCount > 0 Then
					'---------------------------------------------------------
					' Reopen the XML file and update the entries for those ions in udtScanList that had their
					' Optimal peak apex scan numbers updated
					'---------------------------------------------------------

					UpdateProcessingStep(eProcessingStepConstants.UpdateXMLFileWithNewOptimalPeakApexValues)
					SetSubtaskProcessingStepPct(0)
					UpdatePeakMemoryUsage()

					LogMessage("ProcessFile: Call XmlOutputFileUpdateEntries")
					XmlOutputFileUpdateEntries(udtScanList, strInputFileName, strOutputFolderPath)
				End If

			Catch ex As Exception
				blnSuccess = False
				LogErrors("ProcessFile", "Error creating or writing to the output file in folder" & GetFilePathPrefixChar() & strOutputFolderPath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
			End Try



		Catch ex As Exception
			blnSuccess = False
			LogErrors("ProcessFile", "Error in ProcessFile", ex, True, False, eMasicErrorCodes.UnspecifiedError)
		Finally

			' Record the final processing stats (before the output file handles are closed)
			With mProcessingStats
				.ProcessingEndTime = DateTime.UtcNow
				.MemoryUsageMBAtEnd = GetProcessMemoryUsageMB()
			End With

			'---------------------------------------------------------
			' Make sure the output file handles are closed
			'---------------------------------------------------------

			CloseOutputFileHandles(udtOutputFileHandles)

			If mAbortProcessing AndAlso MyBase.ShowMessages Then
				Windows.Forms.MessageBox.Show("Cancelled processing", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
			End If
		End Try

		Try
			'---------------------------------------------------------
			' Cleanup after processing or error
			'---------------------------------------------------------

			LogMessage("ProcessFile: Processing nearly complete")

			Console.WriteLine()
			If blnDoNotProcess Then
				mStatusMessage = "Existing valid results were found; processing was not repeated."
				ShowMessage(mStatusMessage)
			ElseIf blnSuccess Then
				mStatusMessage = "Processing complete.  Results can be found in folder: " & strOutputFolderPath
				ShowMessage(mStatusMessage)
			Else
				If Me.LocalErrorCode = eMasicErrorCodes.NoError Then
					mStatusMessage = "Error Code " & MyBase.ErrorCode & ": " & Me.GetErrorMessage()
					ShowErrorMessage(mStatusMessage)
				Else
					mStatusMessage = "Error Code " & Me.LocalErrorCode & ": " & Me.GetErrorMessage()
					ShowErrorMessage(mStatusMessage)
				End If
			End If

			With mProcessingStats
				LogMessage("ProcessingStats: Memory Usage At Start (MB) = " & .MemoryUsageMBAtStart.ToString)
				LogMessage("ProcessingStats: Peak memory usage (MB) = " & .PeakMemoryUsageMB.ToString)

				LogMessage("ProcessingStats: File Load Time (seconds) = " & .FileLoadEndTime.Subtract(.FileLoadStartTime).TotalSeconds.ToString)
				LogMessage("ProcessingStats: Memory Usage During Load (MB) = " & .MemoryUsageMBDuringLoad.ToString)

				LogMessage("ProcessingStats: Processing Time (seconds) = " & .ProcessingEndTime.Subtract(.ProcessingStartTime).TotalSeconds.ToString)
				LogMessage("ProcessingStats: Memory Usage At End (MB) = " & .MemoryUsageMBAtEnd.ToString)

				LogMessage("ProcessingStats: Cache Event Count = " & .CacheEventCount.ToString)
				LogMessage("ProcessingStats: UncCache Event Count = " & .UnCacheEventCount.ToString)
			End With

			If blnSuccess Then
				LogMessage("Processing complete")
			Else
				LogMessage("Processing ended in error")
			End If

		Catch ex As Exception
			blnSuccess = False
			LogErrors("ProcessFile", "Error in ProcessFile (Cleanup)", ex, True, False, eMasicErrorCodes.UnspecifiedError)
		End Try

		If blnSuccess Then
			udtSICOptions.DatasetNumber += 1
		End If

		If blnSuccess Then
			UpdateProcessingStep(eProcessingStepConstants.Complete, True)
		Else
			UpdateProcessingStep(eProcessingStepConstants.Cancelled, True)
		End If

		Return blnSuccess

	End Function

	Private Function ProcessMZList(
      ByRef udtScanList As udtScanListType,
      ByRef objSpectraCache As clsSpectraCache,
      udtSICOptions As udtSICOptionsType,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtMZBinList() As udtMZBinListType, 
      ByRef intParentIonIndices() As Integer, 
      blnProcessSIMScans As Boolean, 
      intSIMIndex As Integer, 
      ByRef intParentIonsProcessed As Integer) As Boolean


		' Step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance
		' Note that udtMZBinList() and intParentIonIndices() are parallel arrays, with udtMZBinList() sorted on ascending m/z
		Const MAX_RAW_DATA_MEMORY_USAGE_MB As Integer = 50
		Const DATA_COUNT_MEMORY_RESERVE As Integer = 200

		Dim intMZIndex As Integer
		Dim intMZIndexWork As Integer
		Dim intMaxMZCountInChunk As Integer

		Dim intSurveyScanIndex As Integer
		Dim intParentIonIndexPointer As Integer
		Dim intDataIndex As Integer
		Dim intScanIndexObservedInFullSIC As Integer

		Dim intPoolIndex As Integer

		Dim sngIonSum As Single
		Dim dblClosestMZ As Double
		Dim intIonMatchCount As Integer

		Dim dblMZToleranceDa As Double

		' Ranges from 0 to intMZSearchChunkCount-1
		Dim intMZSearchChunkCount As Integer
		Dim udtMZSearchChunk() As udtMZSearchInfoType

		' The following are 2D arrays, ranging from 0 to intMZSearchChunkCount-1 in the first dimension and 0 to .SurveyScanCount - 1 in the second dimension
		' I could have included these in udtMZSearchChunk but memory management is more efficient if I use 2D arrays for this data
		Dim intFullSICScanIndices(,) As Integer		' Pointer into .SurveyScans
		Dim sngFullSICIntensities(,) As Single
		Dim dblFullSICMasses(,) As Double
		Dim intFullSICDataCount() As Integer		' Count of the number of valid entries in the second dimension of the above 3 arrays

		' The following is a 1D array, containing the SIC intensities for a single m/z group
		Dim sngFullSICIntensities1D() As Single

		Dim udtSICPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType
		Dim udtSICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
		Dim udtSICPotentialAreaStatsInFullSIC As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

		' Note: The arrays in this variable contain valid data from index 0 to .SICDataCount-1
		'       Do not assume that the amount of usable data is from index 0 to .SICData.Length -1, since these arrays are increased in length when needed, but never decreased in length (to reduce the number of times ReDim is called)
		Dim udtSICDetails As udtSICStatsDetailsType
		Dim udtSmoothedYData As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType
		Dim udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType

		Dim blnParentIonUpdated() As Boolean

		Dim blnUseScan As Boolean
		Dim blnStorePeakInParentIon As Boolean
		Dim blnLargestPeakFound As Boolean
		Dim blnSuccess As Boolean

		Const DebugParentIonIndexToFind As Integer = 3139
		Const DebugMZToFind As Single = 488.47

		Try
			' Determine the maximum number of m/z values to process simultaneously
			' Limit the total memory usage to ~50 MB
			' Each m/z value will require 12 bytes per scan

			If udtScanList.SurveyScanCount > 0 Then
				intMaxMZCountInChunk = CInt((MAX_RAW_DATA_MEMORY_USAGE_MB * 1024 * 1024) / (udtScanList.SurveyScanCount * 12))
			Else
				intMaxMZCountInChunk = 1
			End If

			If intMaxMZCountInChunk > udtMZBinList.Length Then
				intMaxMZCountInChunk = udtMZBinList.Length
			End If
			If intMaxMZCountInChunk < 1 Then intMaxMZCountInChunk = 1

			' Reserve room in dblSearchMZs
			ReDim udtMZSearchChunk(intMaxMZCountInChunk - 1)

			' Reserve room in intFullSICScanIndices for at most intMaxMZCountInChunk values and .SurveyScanCount scans
			ReDim intFullSICDataCount(intMaxMZCountInChunk - 1)
			ReDim intFullSICScanIndices(intMaxMZCountInChunk - 1, udtScanList.SurveyScanCount - 1)
			ReDim sngFullSICIntensities(intMaxMZCountInChunk - 1, udtScanList.SurveyScanCount - 1)
			ReDim dblFullSICMasses(intMaxMZCountInChunk - 1, udtScanList.SurveyScanCount - 1)

			ReDim sngFullSICIntensities1D(udtScanList.SurveyScanCount - 1)

			' Pre-reserve space in the arrays in udtSICDetails
			With udtSICDetails
				.SICDataCount = 0
				.SICScanType = eScanTypeConstants.SurveyScan

				ReDim .SICScanIndices(DATA_COUNT_MEMORY_RESERVE)
				ReDim .SICScanNumbers(DATA_COUNT_MEMORY_RESERVE)
				ReDim .SICData(DATA_COUNT_MEMORY_RESERVE)
				ReDim .SICMasses(DATA_COUNT_MEMORY_RESERVE)

			End With

			' Reserve room in udtSmoothedYData and udtSmoothedYDataSubset
			With udtSmoothedYData
				.DataCount = 0
				ReDim .Data(DATA_COUNT_MEMORY_RESERVE)
			End With

			With udtSmoothedYDataSubset
				.DataCount = 0
				ReDim .Data(DATA_COUNT_MEMORY_RESERVE)
			End With

			' Reserve room in blnParentIonUpdated
			ReDim blnParentIonUpdated(intParentIonIndices.Length - 1)

		Catch ex As Exception
			LogErrors("ProcessMZList", "Error reserving memory for the m/z chunks", ex, True, True, eMasicErrorCodes.CreateSICsError)
			Return False
		End Try

		Try

			' Uncomment the following to debug ScanOrAcqTimeToAbsolute and ScanOrAcqTimeToScanTime
			'TestScanConversions(udtScanList)

			intMZSearchChunkCount = 0
			intMZIndex = 0
			Do While intMZIndex < udtMZBinList.Length

				'---------------------------------------------------------
				' Find the next group of m/z values to use, starting with intMZIndex
				'---------------------------------------------------------
				With udtMZSearchChunk(intMZSearchChunkCount)
					' Initially set the MZIndexStart to intMZIndex
					.MZIndexStart = intMZIndex


					' Look for adjacent m/z values within udtMZBinList(.MZIndexStart).MZToleranceDa / 2 
					'  of the m/z value that starts this group
					' Only group m/z values with the same udtMZBinList().MZTolerance and udtMZBinList().MZToleranceIsPPM values
					.MZTolerance = udtMZBinList(.MZIndexStart).MZTolerance
					.MZToleranceIsPPM = udtMZBinList(.MZIndexStart).MZToleranceIsPPM

					If .MZToleranceIsPPM Then
						dblMZToleranceDa = PPMToMass(.MZTolerance, udtMZBinList(.MZIndexStart).MZ)
					Else
						dblMZToleranceDa = .MZTolerance
					End If

                    Do While intMZIndex < udtMZBinList.Length - 2 AndAlso
                     Math.Abs(udtMZBinList(intMZIndex + 1).MZTolerance - .MZTolerance) < Double.Epsilon AndAlso
                     udtMZBinList(intMZIndex + 1).MZToleranceIsPPM = .MZToleranceIsPPM AndAlso
                     udtMZBinList(intMZIndex + 1).MZ - udtMZBinList(.MZIndexStart).MZ <= dblMZToleranceDa / 2
                        intMZIndex += 1
                    Loop
					.MZIndexEnd = intMZIndex

					If .MZIndexEnd = .MZIndexStart Then
						.MZIndexMidpoint = .MZIndexEnd
						.SearchMZ = udtMZBinList(.MZIndexStart).MZ
					Else
						' Determine the median m/z of the members in the m/z group
						If (.MZIndexEnd - .MZIndexStart) Mod 2 = 0 Then
							' Odd number of points; use the m/z value of the midpoint
							.MZIndexMidpoint = .MZIndexStart + CInt((.MZIndexEnd - .MZIndexStart) / 2)
							.SearchMZ = udtMZBinList(.MZIndexMidpoint).MZ
						Else
							' Even number of points; average the values on either side of (.mzIndexEnd - .mzIndexStart / 2)
							.MZIndexMidpoint = .MZIndexStart + CInt(Math.Floor((.MZIndexEnd - .MZIndexStart) / 2))
							.SearchMZ = (udtMZBinList(.MZIndexMidpoint).MZ + udtMZBinList(.MZIndexMidpoint + 1).MZ) / 2
						End If
					End If

				End With
				intMZSearchChunkCount += 1

				If intMZSearchChunkCount >= intMaxMZCountInChunk OrElse intMZIndex = udtMZBinList.Length - 1 Then
					'---------------------------------------------------------
					' Reached intMaxMZCountInChunk m/z value
					' Process all of the m/z values in udtMZSearchChunk
					'---------------------------------------------------------

					' Initialize .MaximumIntensity and .ScanIndexMax
					' Additionally, reset intFullSICDataCount() and, for safety, set intFullSICScanIndices() to -1
					For intMZIndexWork = 0 To intMZSearchChunkCount - 1
						With udtMZSearchChunk(intMZIndexWork)
							.MaximumIntensity = 0
							.ScanIndexMax = 0
						End With

						intFullSICDataCount(intMZIndexWork) = 0
						For intSurveyScanIndex = 0 To udtScanList.SurveyScanCount - 1
							intFullSICScanIndices(intMZIndexWork, intSurveyScanIndex) = -1
						Next intSurveyScanIndex
					Next intMZIndexWork

					'---------------------------------------------------------
					' Step through udtScanList to obtain the scan numbers and intensity data for each .SearchMZ in udtMZSearchChunk
					' We're stepping scan by scan since the process of loading a scan from disk is slower than the process of searching for each m/z in the scan
					'---------------------------------------------------------
					For intSurveyScanIndex = 0 To udtScanList.SurveyScanCount - 1
						If blnProcessSIMScans Then
                            If udtScanList.SurveyScans(intSurveyScanIndex).SIMScan AndAlso
                               udtScanList.SurveyScans(intSurveyScanIndex).SIMIndex = intSIMIndex Then
                                blnUseScan = True
                            Else
                                blnUseScan = False
                            End If
						Else
							blnUseScan = Not udtScanList.SurveyScans(intSurveyScanIndex).SIMScan

							If udtScanList.SurveyScans(intSurveyScanIndex).ZoomScan Then
								blnUseScan = False
							End If
						End If

						If blnUseScan Then
							If Not objSpectraCache.ValidateSpectrumInPool(udtScanList.SurveyScans(intSurveyScanIndex).ScanNumber, intPoolIndex) Then
								SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
								Return False
							End If

							For intMZIndexWork = 0 To intMZSearchChunkCount - 1
								With udtMZSearchChunk(intMZIndexWork)
									If .MZToleranceIsPPM Then
										dblMZToleranceDa = PPMToMass(.MZTolerance, .SearchMZ)
									Else
										dblMZToleranceDa = .MZTolerance
									End If

									sngIonSum = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex), .SearchMZ, dblMZToleranceDa, intIonMatchCount, dblClosestMZ, False)

									intDataIndex = intFullSICDataCount(intMZIndexWork)
									intFullSICScanIndices(intMZIndexWork, intDataIndex) = intSurveyScanIndex
									sngFullSICIntensities(intMZIndexWork, intDataIndex) = sngIonSum

									If sngIonSum < Single.Epsilon AndAlso mSICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData Then
										sngFullSICIntensities(intMZIndexWork, intDataIndex) = udtScanList.SurveyScans(intSurveyScanIndex).MinimumPositiveIntensity									
									End If

									dblFullSICMasses(intMZIndexWork, intDataIndex) = dblClosestMZ
									If sngIonSum > .MaximumIntensity Then
										.MaximumIntensity = sngIonSum
										.ScanIndexMax = intDataIndex
									End If

									intFullSICDataCount(intMZIndexWork) += 1
								End With
							Next intMZIndexWork
						End If

						If intSurveyScanIndex Mod 100 = 0 Then
							SetSubtaskProcessingStepPct(CShort(Me.SubtaskProgressPercentComplete), "Loading raw SIC data: " & intSurveyScanIndex.ToString & " / " & udtScanList.SurveyScanCount.ToString)
							If mAbortProcessing Then
								udtScanList.ProcessingIncomplete = True
								Exit Do
							End If
						End If
					Next intSurveyScanIndex

					SetSubtaskProcessingStepPct(CShort(Me.SubtaskProgressPercentComplete), "Creating SIC's for the parent ions")
					If mAbortProcessing Then
						udtScanList.ProcessingIncomplete = True
						Exit Do
					End If

					'---------------------------------------------------------
					' Compute the noise level in sngFullSICIntensities() for each m/z in udtMZSearchChunk
					' Also, find the peaks for each m/z in udtMZSearchChunk and retain the largest peak found
					'---------------------------------------------------------
					For intMZIndexWork = 0 To intMZSearchChunkCount - 1

						' Use this for debugging
						If Math.Abs(udtMZSearchChunk(intMZIndexWork).SearchMZ - DebugMZToFind) < 0.1 Then
							intParentIonIndexPointer = udtMZSearchChunk(intMZIndexWork).MZIndexStart
						End If

						' Copy the data for this m/z into sngFullSICIntensities1D()
						For intDataIndex = 0 To intFullSICDataCount(intMZIndexWork) - 1
							sngFullSICIntensities1D(intDataIndex) = sngFullSICIntensities(intMZIndexWork, intDataIndex)
						Next intDataIndex

						' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
						blnSuccess = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(sngFullSICIntensities1D, 0, intFullSICDataCount(intMZIndexWork) - 1, udtSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions, udtMZSearchChunk(intMZIndexWork).BaselineNoiseStatSegments)

						If Not blnSuccess Then
							SetLocalErrorCode(eMasicErrorCodes.FindSICPeaksError, True)
							Exit Try
						End If

						' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
						mMASICPeakFinder.FindPotentialPeakArea(intFullSICDataCount(intMZIndexWork), sngFullSICIntensities1D, udtSICPotentialAreaStatsInFullSIC, udtSICOptions.SICPeakFinderOptions)

						' Clear udtSICPotentialAreaStatsForPeak
						udtSICPotentialAreaStatsForPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

						intScanIndexObservedInFullSIC = udtMZSearchChunk(intMZIndexWork).ScanIndexMax

						' Populate udtSICDetails using the data centered around the highest intensity in intFullSICIntensities
						' Note that this function will update udtSICPeak.IndexObserved
						blnSuccess = ExtractSICDetailsFromFullSIC(intMZIndexWork, udtMZSearchChunk, intFullSICDataCount(intMZIndexWork), intFullSICScanIndices, sngFullSICIntensities, dblFullSICMasses, udtScanList, intScanIndexObservedInFullSIC, udtSICDetails, udtSICPeak, udtSICOptions, False, 0)

						' Find the largest peak in the SIC for this m/z
                        blnLargestPeakFound = mMASICPeakFinder.FindSICPeakAndArea(
                           udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData,
                           udtSICPotentialAreaStatsForPeak, udtSICPeak,
                           udtSmoothedYDataSubset, udtSICOptions.SICPeakFinderOptions,
                           udtSICPotentialAreaStatsInFullSIC,
                           True, udtScanList.SIMDataPresent, False)

						If blnLargestPeakFound Then
							'--------------------------------------------------------
							' Step through the parent ions and see if .SurveyScanIndex is contained in udtSICPeak
							' If it is, then assign the stats of the largest peak to the given parent ion
							'--------------------------------------------------------
							For intParentIonIndexPointer = udtMZSearchChunk(intMZIndexWork).MZIndexStart To udtMZSearchChunk(intMZIndexWork).MZIndexEnd
								' Use this for debugging
								If intParentIonIndices(intParentIonIndexPointer) = DebugParentIonIndexToFind Then
									intScanIndexObservedInFullSIC = -1
								End If

								blnStorePeakInParentIon = False
								If udtScanList.ParentIons(intParentIonIndices(intParentIonIndexPointer)).CustomSICPeak Then Continue For

								' Assign the stats of the largest peak to each parent ion with .SurveyScanIndex contained in the peak
								With udtScanList.ParentIons(intParentIonIndices(intParentIonIndexPointer))
                                    If .SurveyScanIndex >= udtSICDetails.SICScanIndices(udtSICPeak.IndexBaseLeft) AndAlso
                                       .SurveyScanIndex <= udtSICDetails.SICScanIndices(udtSICPeak.IndexBaseRight) Then

                                        blnStorePeakInParentIon = True
                                    End If
								End With


								If blnStorePeakInParentIon Then
									blnSuccess = StorePeakInParentIon(udtScanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, udtSICPotentialAreaStatsForPeak, udtSICPeak, True)

									' Possibly save the stats for this SIC to the SICData file
									SaveSICDataToText(udtSICOptions, udtScanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, udtOutputFileHandles)

									' Save the stats for this SIC to the XML file
									SaveDataToXML(udtScanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, udtSmoothedYDataSubset, udtOutputFileHandles)

									blnParentIonUpdated(intParentIonIndexPointer) = True
									intParentIonsProcessed += 1

								End If


							Next intParentIonIndexPointer
						End If

						'--------------------------------------------------------
						' Now step through the parent ions and process those that were not updated using udtSICPeak
						' For each, search for the closest peak in sngSICIntensity
						'--------------------------------------------------------
						For intParentIonIndexPointer = udtMZSearchChunk(intMZIndexWork).MZIndexStart To udtMZSearchChunk(intMZIndexWork).MZIndexEnd

							If Not blnParentIonUpdated(intParentIonIndexPointer) Then
								If intParentIonIndices(intParentIonIndexPointer) = DebugParentIonIndexToFind Then
									intScanIndexObservedInFullSIC = -1
								End If

								With udtScanList.ParentIons(intParentIonIndices(intParentIonIndexPointer))
									' Clear udtSICPotentialAreaStatsForPeak
									.SICStats.SICPotentialAreaStatsForPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

									' Record the index in the Full SIC that the parent ion mass was first observed
									' Search for .SurveyScanIndex in intFullSICScanIndices
									intScanIndexObservedInFullSIC = -1
									For intDataIndex = 0 To intFullSICDataCount(intMZIndexWork) - 1
										If intFullSICScanIndices(intMZIndexWork, intDataIndex) >= .SurveyScanIndex Then
											intScanIndexObservedInFullSIC = intDataIndex
											Exit For
										End If
									Next intDataIndex

									If intScanIndexObservedInFullSIC = -1 Then
										' Match wasn't found; this is unexpected
										LogErrors("ProcessMZList", "Programming error: survey scan index not found in intFullSICScanIndices()", Nothing, True, True, eMasicErrorCodes.FindSICPeaksError)
										intScanIndexObservedInFullSIC = 0
									End If

									' Populate udtSICDetails using the data centered around intScanIndexObservedInFullSIC
									' Note that this function will update udtSICPeak.IndexObserved
									blnSuccess = ExtractSICDetailsFromFullSIC(intMZIndexWork, udtMZSearchChunk, intFullSICDataCount(intMZIndexWork), intFullSICScanIndices, sngFullSICIntensities, dblFullSICMasses, udtScanList, intScanIndexObservedInFullSIC, udtSICDetails, .SICStats.Peak, udtSICOptions, .CustomSICPeak, .CustomSICPeakScanOrAcqTimeTolerance)

                                    blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(
                                     udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData,
                                     .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak,
                                     udtSmoothedYDataSubset, udtSICOptions.SICPeakFinderOptions,
                                     udtSICPotentialAreaStatsInFullSIC,
                                     Not .CustomSICPeak, udtScanList.SIMDataPresent, False)


									blnSuccess = StorePeakInParentIon(udtScanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak, blnSuccess)
								End With

								' Possibly save the stats for this SIC to the SICData file
								SaveSICDataToText(udtSICOptions, udtScanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, udtOutputFileHandles)

								' Save the stats for this SIC to the XML file
								SaveDataToXML(udtScanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, udtSmoothedYDataSubset, udtOutputFileHandles)

								blnParentIonUpdated(intParentIonIndexPointer) = True
								intParentIonsProcessed += 1

							End If
						Next intParentIonIndexPointer


						'---------------------------------------------------------
						' Update progress
						'---------------------------------------------------------
						Try

							If udtScanList.ParentIonInfoCount > 1 Then
								SetSubtaskProcessingStepPct(CShort(intParentIonsProcessed / (udtScanList.ParentIonInfoCount - 1) * 100))
							Else
								SetSubtaskProcessingStepPct(0)
							End If

							UpdateOverallProgress(objSpectraCache)
							If mAbortProcessing Then
								udtScanList.ProcessingIncomplete = True
								Exit For
							End If

							If intParentIonsProcessed Mod 100 = 0 Then
								If DateTime.UtcNow.Subtract(mLastParentIonProcessingLogTime).TotalSeconds >= 10 OrElse intParentIonsProcessed Mod 500 = 0 Then
									LogMessage("Parent Ions Processed: " & intParentIonsProcessed.ToString)
									Console.Write(".")
									mLastParentIonProcessingLogTime = DateTime.UtcNow
								End If
							End If

						Catch ex As Exception
							LogErrors("ProcessMZList", "Error updating progress", ex, True, True, eMasicErrorCodes.CreateSICsError)
						End Try

					Next intMZIndexWork

					' Reset intMZSearchChunkCount to 0
					intMZSearchChunkCount = 0
				End If

				If mAbortProcessing Then
					udtScanList.ProcessingIncomplete = True
					Exit Do
				End If

				intMZIndex += 1
			Loop

			blnSuccess = True
		Catch ex As Exception
			LogErrors("ProcessMZList", "Error processing the m/z chunks to create the SIC data", ex, True, True, eMasicErrorCodes.CreateSICsError)
			blnSuccess = False
		End Try


		Return blnSuccess

	End Function

    Private Function ProcessMRMList(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      udtSICOptions As udtSICOptionsType,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef intParentIonsProcessed As Integer) As Boolean


        Dim intParentIonIndex As Integer

        Dim intScanIndex As Integer
        Dim intMRMMassIndex As Integer


        Dim dblParentIonMZ As Double
        Dim dblMRMDaughterMZ As Double

        Dim dblSearchToleranceHalfWidth As Double
        Dim dblClosestMZ As Double

        Dim sngMatchIntensity As Single

        Dim sngMaximumIntensity As Single

        Dim udtSICDetails As udtSICStatsDetailsType

        Dim udtSICPotentialAreaStatsInFullSIC As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

        Dim udtSmoothedYData As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType
        Dim udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType

        Dim udtBaselineNoiseStatSegments() As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseStatSegmentsType

        Dim blnUseScan As Boolean
        ' ReSharper disable once NotAccessedVariable
        Dim blnMatchFound As Boolean
        Dim blnSuccess As Boolean

        Try
            blnSuccess = True

            ' Initialize udtSICDetails
            With udtSICDetails
                .SICDataCount = 0
                .SICScanType = eScanTypeConstants.FragScan

                ReDim .SICScanIndices(udtScanList.FragScanCount)
                ReDim .SICScanNumbers(udtScanList.FragScanCount)
                ReDim .SICData(udtScanList.FragScanCount)
                ReDim .SICMasses(udtScanList.FragScanCount)
            End With

            ' Reserve room in udtSmoothedYData and udtSmoothedYDataSubset
            With udtSmoothedYData
                .DataCount = 0
                ReDim .Data(udtScanList.FragScanCount)
            End With

            With udtSmoothedYDataSubset
                .DataCount = 0
                ReDim .Data(udtScanList.FragScanCount)
            End With

            ReDim udtBaselineNoiseStatSegments(0)

            For intParentIonIndex = 0 To udtScanList.ParentIonInfoCount - 1

                If udtScanList.ParentIons(intParentIonIndex).MRMDaughterMZ > 0 Then
                    ' Step 1: Create the SIC for this MRM Parent/Daughter pair

                    dblParentIonMZ = udtScanList.ParentIons(intParentIonIndex).MZ
                    dblMRMDaughterMZ = udtScanList.ParentIons(intParentIonIndex).MRMDaughterMZ
                    dblSearchToleranceHalfWidth = udtScanList.ParentIons(intParentIonIndex).MRMToleranceHalfWidth

                    ' Reset udtSICDetails 
                    udtSICDetails.SICDataCount = 0

                    ' Step through the fragmentation spectra, finding those that have matching parent and daughter ion m/z values
                    For intScanIndex = 0 To udtScanList.FragScanCount - 1
                        If udtScanList.FragScans(intScanIndex).MRMScanType = MRMScanTypeConstants.SRM Then
                            With udtScanList.FragScans(intScanIndex)

                                blnUseScan = False
                                For intMRMMassIndex = 0 To .MRMScanInfo.MRMMassCount - 1
                                    If MRMParentDaughterMatch(
                                      .MRMScanInfo.ParentIonMZ,
                                      .MRMScanInfo.MRMMassList(intMRMMassIndex).CentralMass,
                                      dblParentIonMZ, dblMRMDaughterMZ) Then
                                        blnUseScan = True
                                        Exit For
                                    End If
                                Next

                                If blnUseScan Then
                                    ' Include this scan in the SIC for this parent ion

                                    'sngMatchIntensity = AggregateIonsInRange(objSpectraCache, udtScanList.FragScans, intScanIndex, dblMRMDaughterMZ, dblSearchToleranceHalfWidth, intIonMatchCount, dblClosestMZ, True)
                                    blnMatchFound = FindMaxValueInMZRange(objSpectraCache, udtScanList.FragScans, intScanIndex, dblMRMDaughterMZ - dblSearchToleranceHalfWidth, dblMRMDaughterMZ + dblSearchToleranceHalfWidth, dblClosestMZ, sngMatchIntensity)

                                    If udtSICDetails.SICDataCount >= udtSICDetails.SICData.Length Then
                                        ReDim Preserve udtSICDetails.SICScanIndices(udtSICDetails.SICScanIndices.Length * 2 - 1)
                                        ReDim Preserve udtSICDetails.SICScanNumbers(udtSICDetails.SICScanIndices.Length - 1)
                                        ReDim Preserve udtSICDetails.SICData(udtSICDetails.SICScanIndices.Length - 1)
                                        ReDim Preserve udtSICDetails.SICMasses(udtSICDetails.SICScanIndices.Length - 1)
                                    End If

                                    udtSICDetails.SICScanIndices(udtSICDetails.SICDataCount) = intScanIndex
                                    udtSICDetails.SICScanNumbers(udtSICDetails.SICDataCount) = .ScanNumber
                                    udtSICDetails.SICData(udtSICDetails.SICDataCount) = sngMatchIntensity
                                    udtSICDetails.SICMasses(udtSICDetails.SICDataCount) = dblClosestMZ

                                    udtSICDetails.SICDataCount += 1
                                End If

                            End With
                        End If
                    Next intScanIndex


                    ' Step 2: Find the largest peak in the SIC

                    ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                    blnSuccess = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(udtSICDetails.SICData, 0, udtSICDetails.SICDataCount - 1, udtSICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions, udtBaselineNoiseStatSegments)

                    If Not blnSuccess Then
                        SetLocalErrorCode(eMasicErrorCodes.FindSICPeaksError, True)
                        Exit Try
                    End If

                    ' Initialize the peak
                    udtScanList.ParentIons(intParentIonIndex).SICStats.Peak = New MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

                    ' Find the data point with the maximum intensity
                    sngMaximumIntensity = 0
                    udtScanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved = 0
                    For intScanIndex = 0 To udtSICDetails.SICDataCount - 1
                        If udtSICDetails.SICData(intScanIndex) > sngMaximumIntensity Then
                            sngMaximumIntensity = udtSICDetails.SICData(intScanIndex)
                            udtScanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved = intScanIndex
                        End If
                    Next intScanIndex


                    ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                    mMASICPeakFinder.FindPotentialPeakArea(udtSICDetails.SICDataCount, udtSICDetails.SICData, udtSICPotentialAreaStatsInFullSIC, udtSICOptions.SICPeakFinderOptions)

                    ' Update .BaselineNoiseStats in udtScanList.ParentIons(intParentIonIndex).SICStats.Peak
                    udtScanList.ParentIons(intParentIonIndex).SICStats.Peak.BaselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(udtScanList.ParentIons(intParentIonIndex).SICStats.Peak.IndexObserved, udtBaselineNoiseStatSegments)

                    With udtScanList.ParentIons(intParentIonIndex)

                        ' Clear udtSICPotentialAreaStatsForPeak
                        .SICStats.SICPotentialAreaStatsForPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

                        blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(
                         udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData,
                         .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak,
                         udtSmoothedYDataSubset, udtSICOptions.SICPeakFinderOptions,
                         udtSICPotentialAreaStatsInFullSIC,
                         False, udtScanList.SIMDataPresent, False)


                        blnSuccess = StorePeakInParentIon(udtScanList, intParentIonIndex, udtSICDetails, .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak, blnSuccess)
                    End With


                    ' Step 3: store the results

                    ' Possibly save the stats for this SIC to the SICData file
                    SaveSICDataToText(udtSICOptions, udtScanList, intParentIonIndex, udtSICDetails, udtOutputFileHandles)

                    ' Save the stats for this SIC to the XML file
                    SaveDataToXML(udtScanList, intParentIonIndex, udtSICDetails, udtSmoothedYDataSubset, udtOutputFileHandles)

                    intParentIonsProcessed += 1

                End If


                '---------------------------------------------------------
                ' Update progress
                '---------------------------------------------------------
                Try

                    If udtScanList.ParentIonInfoCount > 1 Then
                        SetSubtaskProcessingStepPct(CShort(intParentIonsProcessed / (udtScanList.ParentIonInfoCount - 1) * 100))
                    Else
                        SetSubtaskProcessingStepPct(0)
                    End If

                    UpdateOverallProgress(objSpectraCache)
                    If mAbortProcessing Then
                        udtScanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If intParentIonsProcessed Mod 100 = 0 Then
                        If DateTime.UtcNow.Subtract(mLastParentIonProcessingLogTime).TotalSeconds >= 10 OrElse intParentIonsProcessed Mod 500 = 0 Then
                            LogMessage("Parent Ions Processed: " & intParentIonsProcessed.ToString)
                            Console.Write(".")
                            mLastParentIonProcessingLogTime = DateTime.UtcNow
                        End If
                    End If

                Catch ex As Exception
                    LogErrors("ProcessMRMList", "Error updating progress", ex, True, True, eMasicErrorCodes.CreateSICsError)
                End Try

            Next intParentIonIndex

        Catch ex As Exception
            LogErrors("ProcessMRMList", "Error creating SICs for MRM spectra", ex, True, True, eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

    Private Function SaveBPIs(
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      strInputFilePathFull As String,
      strOutputFolderPath As String) As Boolean

        ' This function creates an ICR-2LS compatible .TIC file (using only the MS1 scans), plus
        ' two Decon2LS compatible .CSV files (one for the MS1 scans and one for the MS2, MS3, etc. scans)

        ' Note: Note that SaveExtendedScanStatsFiles() creates a tab-delimited text file with the BPI and TIC information for each scan

        Dim intBPIStepCount As Integer
        Dim intStepsCompleted As Integer

        Dim strInputFileName As String
        Dim strOutputFilePath As String = String.Empty

        Dim blnSuccess As Boolean

        Try
            intBPIStepCount = 3

            SetSubtaskProcessingStepPct(0, "Saving chromatograms to disk")
            intStepsCompleted = 0

            strInputFileName = Path.GetFileName(strInputFilePathFull)

            ' Disabled in April 2015 since not used
            '' First, write a true TIC file (in ICR-2LS format)
            'strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ICRToolsTICChromatogramByScan)
            'LogMessage("Saving ICR Tools TIC to " & Path.GetFileName(strOutputFilePath))

            'SaveICRToolsChromatogramByScan(udtScanList.SurveyScans, udtScanList.SurveyScanCount, strOutputFilePath, False, True, strInputFilePathFull)

            intStepsCompleted += 1
            SetSubtaskProcessingStepPct(CShort(intStepsCompleted / intBPIStepCount * 100))


            ' Second, write an MS-based _scans.csv file (readable with Decon2LS)
            strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsMSChromatogramFile)
            LogMessage("Saving Decon2LS MS Chromatogram File to " & Path.GetFileName(strOutputFilePath))

            SaveDecon2LSChromatogram(udtScanList.SurveyScans, udtScanList.SurveyScanCount, objSpectraCache, strOutputFilePath)

            intStepsCompleted += 1
            SetSubtaskProcessingStepPct(CShort(intStepsCompleted / intBPIStepCount * 100))


            ' Third, write an MSMS-based _scans.csv file (readable with Decon2LS)
            strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsMSMSChromatogramFile)
            LogMessage("Saving Decon2LS MSMS Chromatogram File to " & Path.GetFileName(strOutputFilePath))

            SaveDecon2LSChromatogram(udtScanList.FragScans, udtScanList.FragScanCount, objSpectraCache, strOutputFilePath)

            SetSubtaskProcessingStepPct(100)
            blnSuccess = True

        Catch ex As Exception
            LogErrors("SaveBPIs", "Error writing the BPI to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

	Private Sub SaveBPIWork(
	  ByRef udtScans() As udtScanInfoType, 
	  intScanCount As Integer, 
	  strOutputFilePath As String, 
	  blnSaveTIC As Boolean, 
	  cColDelimiter As Char)

		Dim srOutFile As StreamWriter
		Dim intScanIndex As Integer

		srOutFile = New StreamWriter(strOutputFilePath)

		If blnSaveTIC Then
			srOutFile.WriteLine("Time" & cColDelimiter & "TotalIonIntensity")
		Else
			srOutFile.WriteLine("Time" & cColDelimiter & "BasePeakIntensity" & cColDelimiter & "m/z")
		End If

		For intScanIndex = 0 To intScanCount - 1
			With udtScans(intScanIndex)
				If blnSaveTIC Then
                    srOutFile.WriteLine(Math.Round(.ScanTime, 5).ToString & cColDelimiter &
                      Math.Round(.TotalIonIntensity, 2).ToString)
				Else
                    srOutFile.WriteLine(Math.Round(.ScanTime, 5).ToString & cColDelimiter &
                      Math.Round(.BasePeakIonIntensity, 2).ToString & cColDelimiter &
                      Math.Round(.BasePeakIonMZ, 4).ToString)
				End If

			End With
		Next intScanIndex

		srOutFile.Close()

	End Sub

    Private Sub SaveDecon2LSChromatogram(
      ByRef udtScans() As udtScanInfoType,
      intScanCount As Integer,
      objSpectraCache As clsSpectraCache,
      strOutputFilePath As String)

        Dim srScanInfoOutfile As StreamWriter
        Dim intScanIndex As Integer

        srScanInfoOutfile = New StreamWriter(strOutputFilePath)

        ' Write the file headers
        WriteDecon2LSScanFileHeaders(srScanInfoOutfile)

        ' Step through the scans and write each one
        For intScanIndex = 0 To intScanCount - 1
            WriteDecon2LSScanFileEntry(srScanInfoOutfile, udtScans(intScanIndex), objSpectraCache)

            If intScanIndex Mod 250 = 0 Then
                UpdateOverallProgress(objSpectraCache)
                UpdateStatusFile()
            End If
        Next intScanIndex

        srScanInfoOutfile.Close()

    End Sub

    <Obsolete("No longer used")>
    Private Sub SaveICRToolsChromatogramByScan(
      ByRef udtScans() As udtScanInfoType, 
      intScanCount As Integer, 
      strOutputFilePath As String, 
      blnSaveElutionTimeInsteadOfScan As Boolean, 
      blnSaveTICInsteadOfBPI As Boolean, 
      strInputFilePathFull As String)

        Dim srOutFile As StreamWriter
        Dim fsBinaryOutStream As FileStream
        Dim srBinaryOut As BinaryWriter

        Dim intBufferLength As Integer
        Dim intScanIndex As Integer

        srOutFile = New StreamWriter(strOutputFilePath)

        ' Write the Header text
        srOutFile.WriteLine("ICR-2LS Data File (GA Anderson & JE Bruce); output from MASIC by Matthew E Monroe")
        srOutFile.WriteLine("Version " & MyBase.FileVersion & "; " & MyBase.mFileDate)
        srOutFile.WriteLine("FileName:")
        If blnSaveTICInsteadOfBPI Then
            srOutFile.WriteLine("title:" & Path.GetFileName(strInputFilePathFull) & " TIC")
            srOutFile.WriteLine("Ytitle:Amplitude (TIC)")
        Else
            srOutFile.WriteLine("title:" & Path.GetFileName(strInputFilePathFull) & " BPI")
            srOutFile.WriteLine("Ytitle:Amplitude (BPI)")
        End If

        If blnSaveElutionTimeInsteadOfScan Then
            srOutFile.WriteLine("Xtitle:Time (Minutes)")
        Else
            srOutFile.WriteLine("Xtitle:Scan #")
        End If

        srOutFile.WriteLine("Comment:")
        srOutFile.WriteLine("LCQfilename: " & strInputFilePathFull)
        srOutFile.WriteLine()
        srOutFile.WriteLine("CommentEnd")
        srOutFile.WriteLine("FileType: 5 ")
        srOutFile.WriteLine(" ValidTypes:1=Time,2=Freq,3=Mass;4=TimeSeriesWithCalibrationFn;5=XYPairs")
        srOutFile.WriteLine("DataType: 3 ")
        srOutFile.WriteLine(" ValidTypes:1=Integer,no header,2=Floats,Sun Extrel,3=Floats with header,4=Excite waveform")
        srOutFile.WriteLine("Appodization: 0")
        srOutFile.WriteLine(" ValidFunctions:0=Square,1=Parzen,2=Hanning,3=Welch")
        srOutFile.WriteLine("ZeroFills: 0 ")

        ' Since we're using XY pairs, the buffer length needs to be two times intScanCount
        intBufferLength = intScanCount * 2
        If intBufferLength < 1 Then intBufferLength = 1

        srOutFile.WriteLine("NumberOfSamples: " & intBufferLength.ToString & " ")
        srOutFile.WriteLine("SampleRate: 1 ")
        srOutFile.WriteLine("LowMassFreq: 0 ")
        srOutFile.WriteLine("FreqShift: 0 ")
        srOutFile.WriteLine("NumberSegments: 0 ")
        srOutFile.WriteLine("MaxPoint: 0 ")
        srOutFile.WriteLine("CalType: 0 ")
        srOutFile.WriteLine("CalA: 108205311.2284 ")
        srOutFile.WriteLine("CalB:-1767155067.018 ")
        srOutFile.WriteLine("CalC: 29669467490280 ")
        srOutFile.WriteLine("Intensity: 0 ")
        srOutFile.WriteLine("CurrentXmin: 0 ")
        If intScanCount > 0 Then
            If blnSaveElutionTimeInsteadOfScan Then
                srOutFile.WriteLine("CurrentXmax: " & udtScans(intScanCount - 1).ScanTime.ToString & " ")
            Else
                srOutFile.WriteLine("CurrentXmax: " & udtScans(intScanCount - 1).ScanNumber.ToString & " ")
            End If
        Else
            srOutFile.WriteLine("CurrentXmax: 0")
        End If

        srOutFile.WriteLine("Tags:")
        srOutFile.WriteLine("TagsEnd")
        srOutFile.WriteLine("End")

        srOutFile.Close()

        ' Wait 500 msec, then re-open the file using Binary IO
        Threading.Thread.Sleep(500)

        fsBinaryOutStream = New FileStream(strOutputFilePath, FileMode.Append)
        srBinaryOut = New BinaryWriter(fsBinaryOutStream)


        ' Write an Escape character (Byte 1B)
        srBinaryOut.Write(CByte(27))

        For intScanIndex = 0 To intScanCount - 1
            With udtScans(intScanIndex)
                ' Note: Using CSng to assure that we write out single precision numbers
                ' .TotalIonIntensity and .BasePeakIonIntensity are actually already singles

                If blnSaveElutionTimeInsteadOfScan Then
                    srBinaryOut.Write(CSng(.ScanTime))
                Else
                    srBinaryOut.Write(CSng(.ScanNumber))
                End If

                If blnSaveTICInsteadOfBPI Then
                    srBinaryOut.Write(CSng(.TotalIonIntensity))
                Else
                    srBinaryOut.Write(CSng(.BasePeakIonIntensity))
                End If

            End With
        Next intScanIndex

        srBinaryOut.Close()

    End Sub

	Private Function SaveSICDataToText(
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtScanList As udtScanListType,
      intParentIonIndex As Integer,
      ByRef udtSICDetails As udtSICStatsDetailsType,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean


		Dim intFragScanIndex As Integer
		Dim strPrefix As String

		Try

			If udtOutputFileHandles.SICDataFile Is Nothing Then
				Return True
			End If

			' Write the detailed SIC values for the given parent ion to the text file

			For intFragScanIndex = 0 To udtScanList.ParentIons(intParentIonIndex).FragScanIndexCount - 1

				' "Dataset  ParentIonIndex  FragScanIndex  ParentIonMZ
                strPrefix = udtSICOptions.DatasetNumber.ToString & ControlChars.Tab &
                   intParentIonIndex.ToString & ControlChars.Tab &
                   intFragScanIndex.ToString & ControlChars.Tab &
                   Math.Round(udtScanList.ParentIons(intParentIonIndex).MZ, 4).ToString & ControlChars.Tab

				With udtSICDetails
					If .SICDataCount = 0 Then
						' Nothing to write
						udtOutputFileHandles.SICDataFile.WriteLine(strPrefix & "0" & ControlChars.Tab & "0" & ControlChars.Tab & "0")
					Else
						For intScanIndex As Integer = 0 To .SICDataCount - 1
							udtOutputFileHandles.SICDataFile.WriteLine(strPrefix & .SICScanNumbers(intScanIndex) & ControlChars.Tab & .SICMasses(intScanIndex) & ControlChars.Tab & .SICData(intScanIndex))
						Next intScanIndex
					End If
				End With

			Next intFragScanIndex

		Catch ex As Exception
			LogErrors("SaveSICDataToText", "Error writing to detailed SIC data text file", ex, True, False, eMasicErrorCodes.OutputFileWriteError)
			Return False
		End Try

		Return True


	End Function

	Private Function SaveDataToXML(
      ByRef udtScanList As udtScanListType,
      intParentIonIndex As Integer,
      ByRef udtSICDetails As udtSICStatsDetailsType,
      ByRef udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean


		Dim SICDataScanIntervals() As Byte				 ' Numbers between 0 and 255 that specify the distance (in scans) between each of the data points in SICData(); the first scan number is given by SICScanIndices(0)

		Dim intScanIndex As Integer
		Dim intScanDelta As Integer

		Dim strScanIntervalList As String
		Dim sbIntensityDataList As Text.StringBuilder
		Dim sbMassDataList As Text.StringBuilder
		Dim sbPeakYDataSmoothed As Text.StringBuilder

		Dim intFragScanIndex As Integer
		Dim intScanIntervalIndex As Integer

		Dim intSICDataIndex As Integer
		Dim intIndex As Integer

		Dim strLastGoodLoc As String = "Start"
		Dim blnIntensityDataListWritten As Boolean
		Dim blnMassDataList As Boolean

		Dim objXMLOut As Xml.XmlTextWriter

		Try
			' Populate udtSICStats.SICDataScanIntervals with the scan intervals between each of the data points
			With udtSICDetails
				If .SICDataCount = 0 Then
					ReDim SICDataScanIntervals(0)
				Else
					ReDim SICDataScanIntervals(.SICDataCount - 1)
					For intScanIndex = 1 To .SICDataCount - 1
						intScanDelta = .SICScanNumbers(intScanIndex) - .SICScanNumbers(intScanIndex - 1)
						' When storing in SICDataScanIntervals, make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
						' However, for MRM data, field size can be much larger
						SICDataScanIntervals(intScanIndex) = CByte(Math.Min(Byte.MaxValue, intScanDelta))
					Next intScanIndex
				End If
			End With

			objXMLOut = udtOutputFileHandles.XMLFileForSICs
			If objXMLOut Is Nothing Then Return False

			' Initialize the StringBuilder objects
			sbIntensityDataList = New Text.StringBuilder
			sbMassDataList = New Text.StringBuilder
			sbPeakYDataSmoothed = New Text.StringBuilder

			' Write the SIC's and computed peak stats and areas to the XML file for the given parent ion
			For intFragScanIndex = 0 To udtScanList.ParentIons(intParentIonIndex).FragScanIndexCount - 1
				strLastGoodLoc = "intFragScanIndex=" & intFragScanIndex.ToString

				objXMLOut.WriteStartElement("ParentIon")
				objXMLOut.WriteAttributeString("Index", intParentIonIndex.ToString)				' Parent ion Index
				objXMLOut.WriteAttributeString("FragScanIndex", intFragScanIndex.ToString)		' Frag Scan Index

				strLastGoodLoc = "With udtScanList.ParentIons(intParentIonIndex)"
				With udtScanList.ParentIons(intParentIonIndex)
					objXMLOut.WriteElementString("MZ", Math.Round(.MZ, 4).ToString)

					If .SurveyScanIndex >= 0 AndAlso .SurveyScanIndex < udtScanList.SurveyScans.Length Then
						objXMLOut.WriteElementString("SurveyScanNumber", udtScanList.SurveyScans(.SurveyScanIndex).ScanNumber.ToString)
					Else
						objXMLOut.WriteElementString("SurveyScanNumber", "-1")
					End If

					strLastGoodLoc = "Write FragScanNumber"
					If intFragScanIndex < udtScanList.FragScanCount Then
						objXMLOut.WriteElementString("FragScanNumber", udtScanList.FragScans(.FragScanIndices(intFragScanIndex)).ScanNumber.ToString)
						objXMLOut.WriteElementString("FragScanTime", udtScanList.FragScans(.FragScanIndices(intFragScanIndex)).ScanTime.ToString)
					Else
						' Fragmentation scan does not exist
						objXMLOut.WriteElementString("FragScanNumber", "0")
						objXMLOut.WriteElementString("FragScanTime", "0")
					End If

					objXMLOut.WriteElementString("OptimalPeakApexScanNumber", .OptimalPeakApexScanNumber.ToString)
					objXMLOut.WriteElementString("PeakApexOverrideParentIonIndex", .PeakApexOverrideParentIonIndex.ToString)
					objXMLOut.WriteElementString("CustomSICPeak", .CustomSICPeak.ToString)

					If .CustomSICPeak Then
						objXMLOut.WriteElementString("CustomSICPeakComment", .CustomSICPeakComment)
						objXMLOut.WriteElementString("CustomSICPeakMZToleranceDa", .CustomSICPeakMZToleranceDa.ToString)
						objXMLOut.WriteElementString("CustomSICPeakScanTolerance", .CustomSICPeakScanOrAcqTimeTolerance.ToString)
						objXMLOut.WriteElementString("CustomSICPeakScanToleranceType", mCustomSICList.ScanToleranceType.ToString)
					End If

					strLastGoodLoc = "With .SICStats"
					With .SICStats
						With .Peak
							If udtSICDetails.SICScanType = eScanTypeConstants.FragScan Then
								objXMLOut.WriteElementString("SICScanType", "FragScan")
								objXMLOut.WriteElementString("PeakScanStart", udtScanList.FragScans(udtSICDetails.SICScanIndices(.IndexBaseLeft)).ScanNumber.ToString)
								objXMLOut.WriteElementString("PeakScanEnd", udtScanList.FragScans(udtSICDetails.SICScanIndices(.IndexBaseRight)).ScanNumber.ToString)
								objXMLOut.WriteElementString("PeakScanMaxIntensity", udtScanList.FragScans(udtSICDetails.SICScanIndices(.IndexMax)).ScanNumber.ToString)
							Else
								objXMLOut.WriteElementString("SICScanType", "SurveyScan")
								objXMLOut.WriteElementString("PeakScanStart", udtScanList.SurveyScans(udtSICDetails.SICScanIndices(.IndexBaseLeft)).ScanNumber.ToString)
								objXMLOut.WriteElementString("PeakScanEnd", udtScanList.SurveyScans(udtSICDetails.SICScanIndices(.IndexBaseRight)).ScanNumber.ToString)
								objXMLOut.WriteElementString("PeakScanMaxIntensity", udtScanList.SurveyScans(udtSICDetails.SICScanIndices(.IndexMax)).ScanNumber.ToString)
							End If

                            objXMLOut.WriteElementString("PeakIntensity", StringUtilities.ValueToString(.MaxIntensityValue, 5))
                            objXMLOut.WriteElementString("PeakSignalToNoiseRatio", StringUtilities.ValueToString(.SignalToNoiseRatio, 4))
                            objXMLOut.WriteElementString("FWHMInScans", .FWHMScanWidth.ToString)
                            objXMLOut.WriteElementString("PeakArea", StringUtilities.ValueToString(.Area, 5))
                            objXMLOut.WriteElementString("ShoulderCount", .ShoulderCount.ToString)

                            objXMLOut.WriteElementString("ParentIonIntensity", StringUtilities.ValueToString(.ParentIonIntensity, 5))

                            With .BaselineNoiseStats
                                objXMLOut.WriteElementString("PeakBaselineNoiseLevel", StringUtilities.ValueToString(.NoiseLevel, 5))
                                objXMLOut.WriteElementString("PeakBaselineNoiseStDev", StringUtilities.ValueToString(.NoiseStDev, 3))
                                objXMLOut.WriteElementString("PeakBaselinePointsUsed", .PointsUsed.ToString)
                                objXMLOut.WriteElementString("NoiseThresholdModeUsed", CInt(.NoiseThresholdModeUsed).ToString)
                            End With

                            With .StatisticalMoments
                                objXMLOut.WriteElementString("StatMomentsArea", StringUtilities.ValueToString(.Area, 5))
                                objXMLOut.WriteElementString("CenterOfMassScan", .CenterOfMassScan.ToString)
                                objXMLOut.WriteElementString("PeakStDev", StringUtilities.ValueToString(.StDev, 3))
                                objXMLOut.WriteElementString("PeakSkew", StringUtilities.ValueToString(.Skew, 4))
                                objXMLOut.WriteElementString("PeakKSStat", StringUtilities.ValueToString(.KSStat, 4))
                                objXMLOut.WriteElementString("StatMomentsDataCountUsed", .DataCountUsed.ToString)
                            End With

                        End With

                        If udtSICDetails.SICScanType = eScanTypeConstants.FragScan Then
                            objXMLOut.WriteElementString("SICScanStart", udtScanList.FragScans(udtSICDetails.SICScanIndices(0)).ScanNumber.ToString)
                        Else
                            objXMLOut.WriteElementString("SICScanStart", udtScanList.SurveyScans(udtSICDetails.SICScanIndices(0)).ScanNumber.ToString)
                        End If

                        If mUseBase64DataEncoding Then
                            ' Save scan interval list as base-64 encoded strings
                            strLastGoodLoc = "Call SaveDataToXMLEncodeArray with SICScanIntervals"
                            SaveDataToXMLEncodeArray(objXMLOut, "SICScanIntervals", SICDataScanIntervals)
                        Else
                            ' Save scan interval list as long list of numbers
                            ' There are no tab delimiters, since we require that all 
                            '  of the SICDataScanInterval values be <= 61
                            '   If the interval is <=9, then the interval is stored as a number
                            '   For intervals between 10 and 35, uses letters A to Z
                            '   For intervals between 36 and 61, uses letters A to Z

                            strLastGoodLoc = "Populate strScanIntervalList"
                            strScanIntervalList = String.Empty
                            If Not SICDataScanIntervals Is Nothing Then
                                For intScanIntervalIndex = 0 To udtSICDetails.SICDataCount - 1
                                    If SICDataScanIntervals(intScanIntervalIndex) <= 9 Then
                                        strScanIntervalList &= SICDataScanIntervals(intScanIntervalIndex)
                                    ElseIf SICDataScanIntervals(intScanIntervalIndex) <= 35 Then
                                        strScanIntervalList &= Chr(SICDataScanIntervals(intScanIntervalIndex) + 55)     ' 55 = -10 + 65
                                    ElseIf SICDataScanIntervals(intScanIntervalIndex) <= 61 Then
                                        strScanIntervalList &= Chr(SICDataScanIntervals(intScanIntervalIndex) + 61)     ' 61 = -36 + 97
                                    Else
                                        strScanIntervalList &= "z"
                                    End If
                                Next intScanIntervalIndex
                            End If
                            objXMLOut.WriteElementString("SICScanIntervals", strScanIntervalList)
                        End If

                        strLastGoodLoc = "Write SICPeakIndexStart"
                        objXMLOut.WriteElementString("SICPeakIndexStart", .Peak.IndexBaseLeft.ToString)
                        objXMLOut.WriteElementString("SICPeakIndexEnd", .Peak.IndexBaseRight.ToString)
                        objXMLOut.WriteElementString("SICDataCount", udtSICDetails.SICDataCount.ToString)

                        If SaveSmoothedData Then
                            objXMLOut.WriteElementString("SICSmoothedYDataIndexStart", udtSmoothedYDataSubset.DataStartIndex.ToString)
                        End If

                        If mUseBase64DataEncoding Then
                            Dim DataArray() As Single
                            ReDim DataArray(udtSICDetails.SICDataCount - 1)

                            ' Save intensity and mass data lists as base-64 encoded strings
                            ' Note that these field names are purposely different than the DataList names used below for comma separated lists
                            strLastGoodLoc = "Call SaveDataToXMLEncodeArray with SICIntensityData"
                            Array.Copy(udtSICDetails.SICData, DataArray, udtSICDetails.SICDataCount)
                            SaveDataToXMLEncodeArray(objXMLOut, "SICIntensityData", DataArray)

                            strLastGoodLoc = "Call SaveDataToXMLEncodeArray with SICMassData"
                            For intSICDataIndex = 0 To udtSICDetails.SICDataCount - 1
                                DataArray(intSICDataIndex) = CSng(udtSICDetails.SICMasses(intSICDataIndex))
                            Next intSICDataIndex
                            SaveDataToXMLEncodeArray(objXMLOut, "SICMassData", DataArray)

                            If SaveSmoothedData Then
                                ' Need to copy the data into an array with the correct number of elements
                                ReDim DataArray(udtSmoothedYDataSubset.DataCount - 1)
                                Array.Copy(udtSmoothedYDataSubset.Data, DataArray, udtSmoothedYDataSubset.DataCount)

                                SaveDataToXMLEncodeArray(objXMLOut, "SICSmoothedYData", DataArray)
                            End If
                        Else
                            ' Save intensity and mass data lists as tab-delimited text list

                            blnIntensityDataListWritten = False
                            blnMassDataList = False

                            Try
                                strLastGoodLoc = "Populate sbIntensityDataList"
                                sbIntensityDataList.Length = 0
                                sbMassDataList.Length = 0

                                If Not udtSICDetails.SICData Is Nothing AndAlso udtSICDetails.SICDataCount > 0 Then
                                    For intSICDataIndex = 0 To udtSICDetails.SICDataCount - 1
                                        If udtSICDetails.SICData(intSICDataIndex) > 0 Then
                                            sbIntensityDataList.Append(Math.Round(udtSICDetails.SICData(intSICDataIndex), 1).ToString & ",")
                                        Else
                                            sbIntensityDataList.Append(","c)     ' Do not output any number if the intensity is 0
                                        End If

                                        If udtSICDetails.SICMasses(intSICDataIndex) > 0 Then
                                            sbMassDataList.Append(Math.Round(udtSICDetails.SICMasses(intSICDataIndex), 3).ToString & ",")
                                        Else
                                            sbMassDataList.Append(","c)     ' Do not output any number if the mass is 0
                                        End If
                                    Next intSICDataIndex


                                    ' Trim the trailing comma
                                    If sbIntensityDataList.Chars(sbIntensityDataList.Length - 1) = ","c Then
                                        sbIntensityDataList.Length -= 1
                                        sbMassDataList.Length -= 1
                                    End If

                                End If

                                objXMLOut.WriteElementString("IntensityDataList", sbIntensityDataList.ToString)
                                blnIntensityDataListWritten = True

                                objXMLOut.WriteElementString("MassDataList", sbMassDataList.ToString)
                                blnMassDataList = True

                            Catch ex As OutOfMemoryException
                                ' Ignore the exception if this is an Out of Memory exception

                                If Not blnIntensityDataListWritten Then
                                    objXMLOut.WriteElementString("IntensityDataList", "")
                                End If

                                If Not blnMassDataList Then
                                    objXMLOut.WriteElementString("MassDataList", "")
                                End If

                            End Try


                            If SaveSmoothedData Then
                                Try
                                    strLastGoodLoc = "Populate sbPeakYDataSmoothed"
                                    sbPeakYDataSmoothed.Length = 0

                                    If Not udtSmoothedYDataSubset.Data Is Nothing AndAlso udtSmoothedYDataSubset.DataCount > 0 Then
                                        For intIndex = 0 To udtSmoothedYDataSubset.DataCount - 1
                                            sbPeakYDataSmoothed.Append(Math.Round(udtSmoothedYDataSubset.Data(intIndex)).ToString & ",")
                                        Next intIndex

                                        ' Trim the trailing comma
                                        sbPeakYDataSmoothed.Length -= 1
                                    End If

                                    objXMLOut.WriteElementString("SmoothedYDataList", sbPeakYDataSmoothed.ToString)

                                Catch ex As OutOfMemoryException
                                    ' Ignore the exception if this is an Out of Memory exception
                                    objXMLOut.WriteElementString("SmoothedYDataList", "")
                                End Try

                            End If

                        End If

                    End With
                End With
                objXMLOut.WriteEndElement()
            Next intFragScanIndex

        Catch ex As Exception
            LogErrors("SaveDataToXML", "Error writing the XML data to the output file; Last good location: " & strLastGoodLoc, ex, True, False, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Private Sub SaveDataToXMLEncodeArray(
      ByRef objXMLOut As Xml.XmlTextWriter,
      strElementName As String,
      ByRef dataArray() As Byte)


        Dim intPrecisionBits As Integer
        Dim strDataTypeName As String = String.Empty

        Dim strEncodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, intPrecisionBits, strDataTypeName)

        With objXMLOut
            .WriteStartElement(strElementName)
            .WriteAttributeString("precision", intPrecisionBits.ToString)        ' Store the precision, in bits
            .WriteAttributeString("type", strDataTypeName)
            .WriteString(strEncodedValues)
            .WriteEndElement()
        End With

    End Sub

    Private Sub SaveDataToXMLEncodeArray(
      ByRef objXMLOut As Xml.XmlTextWriter,
      strElementName As String,
      ByRef dataArray() As Single)


        Dim intPrecisionBits As Integer
        Dim strDataTypeName As String = String.Empty

        Dim strEncodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, intPrecisionBits, strDataTypeName)

        With objXMLOut
            .WriteStartElement(strElementName)
            .WriteAttributeString("precision", intPrecisionBits.ToString)        ' Store the precision, in bits
            .WriteAttributeString("type", strDataTypeName)
            .WriteString(strEncodedValues)
            .WriteEndElement()
        End With

    End Sub

    Private Function SaveExtendedScanStatsFiles(
      ByRef udtScanList As udtScanListType,
      strInputFileName As String,
      strOutputFolderPath As String,
      udtSICOptions As udtSICOptionsType,
      blnIncludeHeaders As Boolean) As Boolean

        ' Writes out a flat file containing the extended scan stats

        Dim strExtendedConstantHeaderOutputFilePath As String
        Dim strExtendedNonConstantHeaderOutputFilePath As String = String.Empty

        Const cColDelimiter As Char = ControlChars.Tab

        Dim intNonConstantHeaderIDs() As Integer = Nothing

        Dim udtCurrentScan As udtScanInfoType
        Try
            SetSubtaskProcessingStepPct(0, "Saving extended scan stats to flat file")

            strExtendedConstantHeaderOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile)
            strExtendedNonConstantHeaderOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsExtendedFlatFile)

            LogMessage("Saving extended scan stats flat file to disk: " & Path.GetFileName(strExtendedNonConstantHeaderOutputFilePath))

            If mExtendedHeaderInfo.Count = 0 Then
                ' No extended stats to write; exit the function
                Exit Try
            End If

            ' Lookup extended stats values that are constants for all scans
            ' The following will also remove the constant header values from mExtendedHeaderInfo
            Dim strConstantExtendedHeaderValues = ExtractConstantExtendedHeaderValues(intNonConstantHeaderIDs, udtScanList.SurveyScans, udtScanList.FragScans, cColDelimiter)
            If strConstantExtendedHeaderValues Is Nothing Then strConstantExtendedHeaderValues = String.Empty

            ' Write the constant extended stats values to a text file
            Using srOutFile = New StreamWriter(strExtendedConstantHeaderOutputFilePath, False)
                srOutFile.WriteLine(strConstantExtendedHeaderValues)
            End Using

            ' Now open another output file for the non-constant extended stats
            Using srOutFile = New StreamWriter(strExtendedNonConstantHeaderOutputFilePath, False)

                If blnIncludeHeaders Then
                    Dim strOutLine = ConstructExtendedStatsHeaders(cColDelimiter)
                    srOutFile.WriteLine(strOutLine)
                End If

                For intScanIndex = 0 To udtScanList.MasterScanOrderCount - 1

                    udtCurrentScan = GetScanByMasterScanIndex(udtScanList, intScanIndex)

                    Dim strOutLine = ConcatenateExtendedStats(intNonConstantHeaderIDs, udtSICOptions.DatasetNumber, udtCurrentScan.ScanNumber, udtCurrentScan.ExtendedHeaderInfo, cColDelimiter)
                    srOutFile.WriteLine(strOutLine)

                    If intScanIndex Mod 100 = 0 Then
                        SetSubtaskProcessingStepPct(CShort(intScanIndex / (udtScanList.MasterScanOrderCount - 1) * 100))
                    End If
                Next intScanIndex

            End Using

        Catch ex As Exception
            LogErrors("SaveExtendedScanStatsFiles", "Error writing the Extended Scan Stats to" & GetFilePathPrefixChar() & strExtendedNonConstantHeaderOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        SetSubtaskProcessingStepPct(100)
        Return True

    End Function

    Private Function SaveHeaderGlossary(
      ByRef udtScanList As udtScanListType,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean


        Dim srOutFile As StreamWriter

        Dim strHeaders As String
        Dim strOutputFilePath As String = "?undefinedfile?"

        Try
            strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.HeaderGlossary)
            LogMessage("Saving Header Glossary to " & Path.GetFileName(strOutputFilePath))

            srOutFile = New StreamWriter(strOutputFilePath, False)

            ' ScanStats
            srOutFile.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.ScanStatsFlatFile) & ":")
            srOutFile.WriteLine(GetHeadersForOutputFile(udtScanList, eOutputFileTypeConstants.ScanStatsFlatFile))
            srOutFile.WriteLine()

            ' SICStats
            srOutFile.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.SICStatsFlatFile) & ":")
            srOutFile.WriteLine(GetHeadersForOutputFile(udtScanList, eOutputFileTypeConstants.SICStatsFlatFile))
            srOutFile.WriteLine()

            ' ScanStatsExtended
            strHeaders = GetHeadersForOutputFile(udtScanList, eOutputFileTypeConstants.ScanStatsExtendedFlatFile)
            If Not strHeaders Is Nothing AndAlso strHeaders.Length > 0 Then
                srOutFile.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.ScanStatsExtendedFlatFile) & ":")
                srOutFile.WriteLine(strHeaders)
            End If

            srOutFile.Close()

        Catch ex As Exception
            LogErrors("SaveHeaderGlossary", "Error writing the Header Glossary to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Private Function SaveMSMethodFile(
      ByRef objXcaliburAccessor As XRawFileIO,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean

        Dim intInstMethodCount As Integer
        Dim intIndex As Integer
        Dim strOutputFilePath As String = "?undefinedfile?"
        Dim strMethodNum As String

        Dim srOutfile As StreamWriter

        Try
            intInstMethodCount = objXcaliburAccessor.FileInfo.InstMethods.Count
        Catch ex As Exception
            LogErrors("SaveMSMethodFile", "Error looking up InstMethod length in objXcaliburAccessor.FileInfo", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Try
            For intIndex = 0 To intInstMethodCount - 1
                If intIndex = 0 And objXcaliburAccessor.FileInfo.InstMethods.Count = 1 Then
                    strMethodNum = String.Empty
                Else
                    strMethodNum = (intIndex + 1).ToString.Trim
                End If
                strOutputFilePath = udtOutputFileHandles.MSMethodFilePathBase & strMethodNum & ".txt"

                srOutfile = New StreamWriter(strOutputFilePath, False)

                With objXcaliburAccessor.FileInfo
                    srOutfile.WriteLine("Instrument model: " & .InstModel)
                    srOutfile.WriteLine("Instrument name: " & .InstName)
                    srOutfile.WriteLine("Instrument description: " & .InstrumentDescription)
                    srOutfile.WriteLine("Instrument serial number: " & .InstSerialNumber)
                    srOutfile.WriteLine()

                    srOutfile.WriteLine(objXcaliburAccessor.FileInfo.InstMethods(intIndex))
                End With

                srOutfile.Close()
            Next intIndex

        Catch ex As Exception
            LogErrors("SaveMSMethodFile", "Error writing the MS Method to " & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Private Function SaveMSTuneFile(
      ByRef objXcaliburAccessor As XRawFileIO,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType) As Boolean

        Const cColDelimiter As Char = ControlChars.Tab

        Dim intTuneMethodCount As Integer
        Dim intIndex As Integer
        Dim strOutputFilePath As String = "?undefinedfile?"
        Dim strTuneInfoNum As String

        Dim srOutfile As StreamWriter

        Try
            intTuneMethodCount = objXcaliburAccessor.FileInfo.TuneMethods.Count
        Catch ex As Exception
            LogErrors("SaveMSMethodFile", "Error looking up TuneMethods length in objXcaliburAccessor.FileInfo", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Try
            For intIndex = 0 To intTuneMethodCount - 1
                If intIndex = 0 And objXcaliburAccessor.FileInfo.TuneMethods.Count = 1 Then
                    strTuneInfoNum = String.Empty
                Else
                    strTuneInfoNum = (intIndex + 1).ToString.Trim
                End If
                strOutputFilePath = udtOutputFileHandles.MSTuneFilePathBase & strTuneInfoNum & ".txt"

                srOutfile = New StreamWriter(strOutputFilePath, False)

                srOutfile.WriteLine("Category" & cColDelimiter & "Name" & cColDelimiter & "Value")

                With objXcaliburAccessor.FileInfo.TuneMethods(intIndex)
                    For Each setting As udtTuneMethodSetting In .Settings
                        srOutfile.WriteLine(setting.Category & cColDelimiter & setting.Name & cColDelimiter & setting.Value)
                    Next
                    srOutfile.WriteLine()
                End With

                srOutfile.Close()
            Next intIndex

        Catch ex As Exception
            LogErrors("SaveMSTuneFile", "Error writing the MS Tune Settings to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True
    End Function

    Public Function SaveParameterFileSettings(strParameterFilePath As String) As Boolean

        Dim objSettingsFile As New XmlSettingsFileAccessor

        Dim intIndex As Integer
        Dim blnScanCommentsDefined As Boolean

        Dim dblSICTolerance As Double, blnSICToleranceIsPPM As Boolean

        Try

            If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
                ' No parameter file specified; unable to save
                Return False
            End If

            ' Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
            If objSettingsFile.LoadSettings(strParameterFilePath, True) Then
                With objSettingsFile

                    ' Database settings
                    .SetParam(XML_SECTION_DATABASE_SETTINGS, "ConnectionString", Me.DatabaseConnectionString)
                    .SetParam(XML_SECTION_DATABASE_SETTINGS, "DatasetInfoQuerySql", Me.DatasetInfoQuerySql)


                    ' Import Options
                    .SetParam(XML_SECTION_IMPORT_OPTIONS, "CDFTimeInSeconds", Me.CDFTimeInSeconds)
                    .SetParam(XML_SECTION_IMPORT_OPTIONS, "ParentIonDecoyMassDa", Me.ParentIonDecoyMassDa)


                    ' Masic Export Options
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeHeaders", Me.IncludeHeadersInExportFile)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeScanTimesInSICStatsFile", Me.IncludeScanTimesInSICStatsFile)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "SkipMSMSProcessing", Me.SkipMSMSProcessing)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "SkipSICAndRawDataProcessing", Me.SkipSICAndRawDataProcessing)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataOnly", Me.ExportRawDataOnly)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "SuppressNoParentIonsError", Me.SuppressNoParentIonsError)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStats", Me.WriteExtendedStats)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsIncludeScanFilterText", Me.WriteExtendedStatsIncludeScanFilterText)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStatsStatusLog", Me.WriteExtendedStatsStatusLog)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "StatusLogKeyNameFilterList", Me.GetStatusLogKeyNameFilterListAsText(True))
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ConsolidateConstantExtendedHeaderValues", Me.ConsolidateConstantExtendedHeaderValues)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteDetailedSICDataFile", Me.WriteDetailedSICDataFile)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSMethodFile", Me.WriteMSMethodFile)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSTuneFile", Me.WriteMSTuneFile)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMDataList", Me.WriteMRMDataList)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMIntensityCrosstab", Me.WriteMRMIntensityCrosstab)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "FastExistingXMLFileTest", Me.FastExistingXMLFileTest)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonStatsEnabled", Me.ReporterIonStatsEnabled)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonMassMode", CInt(Me.ReporterIonMassMode))
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonToleranceDa", Me.ReporterIonToleranceDaDefault)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonApplyAbundanceCorrection", Me.ReporterIonApplyAbundanceCorrection)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonITraq4PlexCorrectionFactorType", Me.ReporterIonITraq4PlexCorrectionFactorType)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveObservedMasses", Me.ReporterIonSaveObservedMasses)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveUncorrectedIntensities", Me.ReporterIonSaveUncorrectedIntensities)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawSpectraData", Me.ExportRawSpectraData)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataFileFormat", Me.ExportRawDataFileFormat)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIncludeMSMS", Me.ExportRawDataIncludeMSMS)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataRenumberScans", Me.ExportRawDataRenumberScans)

                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMinimumSignalToNoiseRatio", Me.ExportRawDataMinimumSignalToNoiseRatio)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMaxIonCountPerScan", Me.ExportRawDataMaxIonCountPerScan)
                    .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIntensityMinimum", Me.ExportRawDataIntensityMinimum)


                    ' SIC Options
                    ' Note: Skipping .DatasetNumber since this must be provided at the command line or through the Property Function interface

                    ' "SICToleranceDa" is a legacy parameter.  If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceDa", Me.SICToleranceDa.ToString("0.0000"))

                    dblSICTolerance = GetSICTolerance(blnSICToleranceIsPPM)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICTolerance", dblSICTolerance.ToString("0.0000"))
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceIsPPM", blnSICToleranceIsPPM.ToString)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "RefineReportedParentIonMZ", Me.RefineReportedParentIonMZ)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeStart", Me.ScanRangeStart)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeEnd", Me.ScanRangeEnd)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "RTRangeStart", Me.RTRangeStart)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "RTRangeEnd", Me.RTRangeEnd)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "CompressMSSpectraData", Me.CompressMSSpectraData)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "CompressMSMSSpectraData", Me.CompressMSMSSpectraData)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForDa", Me.CompressToleranceDivisorForDa)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForPPM", Me.CompressToleranceDivisorForPPM)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesBackward", Me.MaxSICPeakWidthMinutesBackward)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesForward", Me.MaxSICPeakWidthMinutesForward)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdFractionMax", Me.IntensityThresholdFractionMax)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdAbsoluteMinimum", Me.IntensityThresholdAbsoluteMinimum)

                    ' Peak Finding Options
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdMode", Me.SICNoiseThresholdMode)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdIntensity", Me.SICNoiseThresholdIntensity)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseFractionLowIntensityDataToAverage", Me.SICNoiseFractionLowIntensityDataToAverage)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseMinimumSignalToNoiseRatio", Me.SICNoiseMinimumSignalToNoiseRatio)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "MaxDistanceScansNoOverlap", Me.MaxDistanceScansNoOverlap)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "MaxAllowedUpwardSpikeFractionMax", Me.MaxAllowedUpwardSpikeFractionMax)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansScaler", Me.InitialPeakWidthScansScaler)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansMaximum", Me.InitialPeakWidthScansMaximum)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "FindPeaksOnSmoothedData", Me.FindPeaksOnSmoothedData)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SmoothDataRegardlessOfMinimumPeakWidth", Me.SmoothDataRegardlessOfMinimumPeakWidth)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "UseButterworthSmooth", Me.UseButterworthSmooth)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequency", Me.ButterworthSamplingFrequency)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequencyDoubledForSIMData", Me.ButterworthSamplingFrequencyDoubledForSIMData)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "UseSavitzkyGolaySmooth", Me.UseSavitzkyGolaySmooth)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SavitzkyGolayFilterOrder", Me.SavitzkyGolayFilterOrder)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SaveSmoothedData", Me.SaveSmoothedData)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdMode", Me.MassSpectraNoiseThresholdMode)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdIntensity", Me.MassSpectraNoiseThresholdIntensity)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseFractionLowIntensityDataToAverage", Me.MassSpectraNoiseFractionLowIntensityDataToAverage)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseMinimumSignalToNoiseRatio", Me.MassSpectraNoiseMinimumSignalToNoiseRatio)

                    .SetParam(XML_SECTION_SIC_OPTIONS, "ReplaceSICZeroesWithMinimumPositiveValueFromMSData", Me.ReplaceSICZeroesWithMinimumPositiveValueFromMSData)

                    ' Similarity Options
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonMZToleranceHalfWidth", Me.SimilarIonMZToleranceHalfWidth)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonToleranceHalfWidthMinutes", Me.SimilarIonToleranceHalfWidthMinutes)
                    .SetParam(XML_SECTION_SIC_OPTIONS, "SpectrumSimilarityMinimum", Me.SpectrumSimilarityMinimum)



                    ' Binning Options
                    .SetParam(XML_SECTION_BINNING_OPTIONS, "BinStartX", Me.BinStartX)
                    .SetParam(XML_SECTION_BINNING_OPTIONS, "BinEndX", Me.BinEndX)
                    .SetParam(XML_SECTION_BINNING_OPTIONS, "BinSize", Me.BinSize)
                    .SetParam(XML_SECTION_BINNING_OPTIONS, "MaximumBinCount", Me.MaximumBinCount)

                    .SetParam(XML_SECTION_BINNING_OPTIONS, "IntensityPrecisionPercent", Me.BinnedDataIntensityPrecisionPercent)
                    .SetParam(XML_SECTION_BINNING_OPTIONS, "Normalize", Me.NormalizeBinnedData)
                    .SetParam(XML_SECTION_BINNING_OPTIONS, "SumAllIntensitiesForBin", Me.SumAllIntensitiesForBin)


                    ' Memory management options
                    .SetParam(XML_SECTION_MEMORY_OPTIONS, "DiskCachingAlwaysDisabled", Me.DiskCachingAlwaysDisabled)
                    .SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheFolderPath", Me.CacheFolderPath)
                    .SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheSpectraToRetainInMemory", Me.CacheSpectraToRetainInMemory)
                    .SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheMinimumFreeMemoryMB", Me.CacheMinimumFreeMemoryMB)
                    .SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheMaximumMemoryUsageMB", Me.CacheMaximumMemoryUsageMB)

                End With

                ' Construct the strRawText strings using mCustomSICList
                With mCustomSICList
                    blnScanCommentsDefined = False

                    Dim lstMzValues = New List(Of String)
                    Dim lstMzTolerances = New List(Of String)
                    Dim lstScanCenters = New List(Of String)
                    Dim lstScanTolerances = New List(Of String)
                    Dim lstComments = New List(Of String)

                    For intIndex = 0 To .CustomMZSearchValues.Length - 1
                        With .CustomMZSearchValues(intIndex)
                            lstMzValues.Add(.MZ.ToString())
                            lstMzTolerances.Add(.MZToleranceDa.ToString())

                            lstScanCenters.Add(.ScanOrAcqTimeCenter.ToString())
                            lstScanTolerances.Add(.ScanOrAcqTimeTolerance.ToString())

                            If .Comment Is Nothing Then
                                lstComments.Add(String.Empty)
                            Else
                                If .Comment.Length > 0 Then
                                    blnScanCommentsDefined = True
                                End If
                                lstComments.Add(.Comment)
                            End If

                        End With
                    Next intIndex

                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZList", String.Join(ControlChars.Tab, lstMzValues))
                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZToleranceDaList", String.Join(ControlChars.Tab, lstMzTolerances))

                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList", String.Join(ControlChars.Tab, lstScanCenters))
                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanToleranceList", String.Join(ControlChars.Tab, lstScanTolerances))

                    If blnScanCommentsDefined Then
                        objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", String.Join(ControlChars.Tab, lstComments))
                    Else
                        objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", String.Empty)
                    End If

                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", .ScanOrAcqTimeTolerance.ToString)

                    Select Case .ScanToleranceType
                        Case eCustomSICScanTypeConstants.Relative
                            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", CUSTOM_SIC_TYPE_RELATIVE)
                        Case eCustomSICScanTypeConstants.AcquisitionTime
                            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", CUSTOM_SIC_TYPE_ACQUISITION_TIME)
                        Case Else
                            ' Assume absolute
                            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", CUSTOM_SIC_TYPE_ABSOLUTE)
                    End Select

                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", Me.CustomSICListFileName)

                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "LimitSearchToCustomMZList", .LimitSearchToCustomMZList)
                End With

                objSettingsFile.SaveSettings()

            End If

        Catch ex As Exception
            LogErrors("SaveParameterFileSettings", "Error in SaveParameterFileSettings", ex, True, False, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Private Function SaveSICStatsFlatFile(
      ByRef udtScanList As udtScanListType,
      strInputFileName As String,
      strOutputFolderPath As String,
      udtSICOptions As udtSICOptionsType,
      blnIncludeHeaders As Boolean,
      blnIncludeScanTimesInSICStatsFile As Boolean) As Boolean

        ' Writes out a flat file containing identified peaks and statistics

        Dim strOutputFilePath As String = String.Empty

        Const cColDelimiter As Char = ControlChars.Tab
        Dim sbOutLine As Text.StringBuilder

        Dim intParentIonIndex, intFragScanIndex As Integer

        Dim intScanListArray() As Integer = Nothing
        Dim blnIncludeParentIon As Boolean

        Dim sngSurveyScanTime, sngFragScanTime, sngOptimalPeakApexScanTime As Single

        ' Populate intScanListArray with the scan numbers in udtScanList.SurveyScans
        PopulateScanListPointerArray(udtScanList.SurveyScans, udtScanList.SurveyScanCount, intScanListArray)

        Try
            sbOutLine = New Text.StringBuilder

            SetSubtaskProcessingStepPct(0, "Saving SIC data to flat file")

            strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.SICStatsFlatFile)
            LogMessage("Saving SIC flat file to disk: " & Path.GetFileName(strOutputFilePath))

            Using srOutfile = New StreamWriter(strOutputFilePath, False)

                ' Write the SIC stats to the output file
                ' The file is tab delimited
                ' If blnIncludeHeaders = True, then headers are included
                If blnIncludeHeaders Then
                    srOutfile.WriteLine(GetHeadersForOutputFile(udtScanList, eOutputFileTypeConstants.SICStatsFlatFile, cColDelimiter))
                End If

                For intParentIonIndex = 0 To udtScanList.ParentIonInfoCount - 1
                    If Me.LimitSearchToCustomMZList Then
                        blnIncludeParentIon = udtScanList.ParentIons(intParentIonIndex).CustomSICPeak
                    Else
                        blnIncludeParentIon = True
                    End If

                    If blnIncludeParentIon Then
                        For intFragScanIndex = 0 To udtScanList.ParentIons(intParentIonIndex).FragScanIndexCount - 1
                            sbOutLine.Length = 0
                            With udtScanList.ParentIons(intParentIonIndex)
                                sbOutLine.Append(udtSICOptions.DatasetNumber.ToString & cColDelimiter)                               ' Dataset number
                                sbOutLine.Append(intParentIonIndex.ToString & cColDelimiter)                                         ' Parent Ion Index

                                sbOutLine.Append(Math.Round(.MZ, 4).ToString & cColDelimiter)                                        ' MZ
                                If .SurveyScanIndex >= 0 AndAlso .SurveyScanIndex < udtScanList.SurveyScans.Length Then
                                    sbOutLine.Append(udtScanList.SurveyScans(.SurveyScanIndex).ScanNumber.ToString & cColDelimiter)  ' Survey scan number
                                    sngSurveyScanTime = udtScanList.SurveyScans(.SurveyScanIndex).ScanTime
                                Else
                                    sbOutLine.Append("-1" & cColDelimiter)      ' Survey scan number
                                    sngSurveyScanTime = 0
                                End If

                                If intFragScanIndex < udtScanList.FragScanCount Then
                                    sbOutLine.Append(udtScanList.FragScans(.FragScanIndices(intFragScanIndex)).ScanNumber.ToString & cColDelimiter)  ' Fragmentation scan number
                                Else
                                    sbOutLine.Append("0" & cColDelimiter)    ' Fragmentation scan does not exist
                                End If

                                sbOutLine.Append(.OptimalPeakApexScanNumber.ToString & cColDelimiter)                ' Optimal peak apex scan number

                                If blnIncludeScanTimesInSICStatsFile Then
                                    If intFragScanIndex < udtScanList.FragScanCount Then
                                        sngFragScanTime = udtScanList.FragScans(.FragScanIndices(intFragScanIndex)).ScanTime
                                    Else
                                        sngFragScanTime = 0                ' Fragmentation scan does not exist
                                    End If

                                    sngOptimalPeakApexScanTime = ScanNumberToScanTime(udtScanList, .OptimalPeakApexScanNumber)
                                End If

                                sbOutLine.Append(.PeakApexOverrideParentIonIndex.ToString & cColDelimiter)           ' Parent Ion Index that supplied the optimal peak apex scan number
                                If .CustomSICPeak Then
                                    sbOutLine.Append("1" & cColDelimiter)                                            ' Custom SIC peak, record 1
                                Else
                                    sbOutLine.Append("0" & cColDelimiter)                                            ' Not a Custom SIC peak, record 0
                                End If

                                With .SICStats
                                    If .ScanTypeForPeakIndices = eScanTypeConstants.FragScan Then
                                        sbOutLine.Append(udtScanList.FragScans(.PeakScanIndexStart).ScanNumber.ToString & cColDelimiter)   ' Peak Scan Start
                                        sbOutLine.Append(udtScanList.FragScans(.PeakScanIndexEnd).ScanNumber.ToString & cColDelimiter)     ' Peak Scan End
                                        sbOutLine.Append(udtScanList.FragScans(.PeakScanIndexMax).ScanNumber.ToString & cColDelimiter)     ' Peak Scan Max Intensity
                                    Else
                                        sbOutLine.Append(udtScanList.SurveyScans(.PeakScanIndexStart).ScanNumber.ToString & cColDelimiter)   ' Peak Scan Start
                                        sbOutLine.Append(udtScanList.SurveyScans(.PeakScanIndexEnd).ScanNumber.ToString & cColDelimiter)     ' Peak Scan End
                                        sbOutLine.Append(udtScanList.SurveyScans(.PeakScanIndexMax).ScanNumber.ToString & cColDelimiter)     ' Peak Scan Max Intensity
                                    End If
                                    With .Peak
                                        sbOutLine.Append(StringUtilities.ValueToString(.MaxIntensityValue, 5) & cColDelimiter)           ' Peak Intensity
                                        sbOutLine.Append(StringUtilities.ValueToString(.SignalToNoiseRatio, 4) & cColDelimiter)          ' Peak signal to noise ratio
                                        sbOutLine.Append(.FWHMScanWidth.ToString & cColDelimiter)                                       ' Full width at half max (in scans)
                                        sbOutLine.Append(StringUtilities.ValueToString(.Area, 5) & cColDelimiter)                       ' Peak area

                                        sbOutLine.Append(StringUtilities.ValueToString(.ParentIonIntensity, 5) & cColDelimiter)          ' Intensity of the parent ion (just before the fragmentation scan)
                                        With .BaselineNoiseStats
                                            sbOutLine.Append(StringUtilities.ValueToString(.NoiseLevel, 5) & cColDelimiter)
                                            sbOutLine.Append(StringUtilities.ValueToString(.NoiseStDev, 3) & cColDelimiter)
                                            sbOutLine.Append(.PointsUsed.ToString & cColDelimiter)
                                        End With

                                        With .StatisticalMoments
                                            sbOutLine.Append(StringUtilities.ValueToString(.Area, 5) & cColDelimiter)
                                            sbOutLine.Append(.CenterOfMassScan.ToString & cColDelimiter)
                                            sbOutLine.Append(StringUtilities.ValueToString(.StDev, 3) & cColDelimiter)
                                            sbOutLine.Append(StringUtilities.ValueToString(.Skew, 4) & cColDelimiter)
                                            sbOutLine.Append(StringUtilities.ValueToString(.KSStat, 4) & cColDelimiter)
                                            sbOutLine.Append(.DataCountUsed.ToString)
                                        End With

                                    End With
                                End With
                            End With

                            If blnIncludeScanTimesInSICStatsFile Then
                                sbOutLine.Append(cColDelimiter)
                                sbOutLine.Append(Math.Round(sngSurveyScanTime, 5).ToString & cColDelimiter)             ' SurveyScanTime
                                sbOutLine.Append(Math.Round(sngFragScanTime, 5).ToString & cColDelimiter)               ' FragScanTime
                                sbOutLine.Append(Math.Round(sngOptimalPeakApexScanTime, 5).ToString & cColDelimiter)    ' OptimalPeakApexScanTime
                            End If

                            srOutfile.WriteLine(sbOutLine.ToString)
                        Next intFragScanIndex
                    End If

                    If udtScanList.ParentIonInfoCount > 1 Then
                        If intParentIonIndex Mod 100 = 0 Then
                            SetSubtaskProcessingStepPct(CShort(intParentIonIndex / (udtScanList.ParentIonInfoCount - 1) * 100))
                        End If
                    Else
                        SetSubtaskProcessingStepPct(1)
                    End If
                    If mAbortProcessing Then
                        udtScanList.ProcessingIncomplete = True
                        Exit For
                    End If
                Next intParentIonIndex

            End Using

        Catch ex As Exception
            LogErrors("SaveSICStatsFlatFile", "Error writing the Peak Stats to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Private Sub SaveRawDatatoDiskWork(
      ByRef srDataOutFile As StreamWriter,
      ByRef srScanInfoOutfile As StreamWriter,
      ByRef udtScan As udtScanInfoType,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      blnFragmentationScan As Boolean,
      ByRef intSpectrumExportCount As Integer,
      udtRawDataExportOptions As udtRawDataExportOptionsType)

        Select Case udtRawDataExportOptions.FileFormat
            Case eExportRawDataFileFormatConstants.PEKFile
                SavePEKFileToDiskWork(srDataOutFile, udtScan, objSpectraCache, strInputFileName, blnFragmentationScan, intSpectrumExportCount, udtRawDataExportOptions)
            Case eExportRawDataFileFormatConstants.CSVFile
                SaveCSVFilesToDiskWork(srDataOutFile, srScanInfoOutfile, udtScan, objSpectraCache, blnFragmentationScan, intSpectrumExportCount, udtRawDataExportOptions)
            Case Else
                ' Unknown format
                ' This code should never be reached
        End Select
    End Sub

    Private Sub SaveCSVFilesToDiskWork(
      ByRef srDataOutFile As StreamWriter,
      ByRef srScanInfoOutfile As StreamWriter,
      ByRef udtScan As udtScanInfoType,
      objSpectraCache As clsSpectraCache,
      blnFragmentationScan As Boolean,
      ByRef intSpectrumExportCount As Integer,
      udtRawDataExportOptions As udtRawDataExportOptionsType)

        Dim intIonIndex As Integer
        Dim sngIntensities() As Single
        Dim intPointerArray() As Integer

        Dim intScanNumber As Integer
        Dim intMSLevel As Integer
        Dim intPoolIndex As Integer

        Dim intStartIndex As Integer, intExportCount As Integer
        Dim sngMinimumIntensityCurrentScan As Single

        Dim intNumIsotopicSignatures As Integer
        Dim intNumPeaks As Integer
        Dim sngBaselineNoiseLevel As Single

        Dim intCharge As Integer
        Dim sngFit As Single
        Dim dblMass As Double
        Dim sngFWHM As Single
        Dim sngSignalToNoise As Single
        Dim sngMonoisotopicAbu As Single
        Dim sngMonoPlus2Abu As Single

        intExportCount = 0

        If Not objSpectraCache.ValidateSpectrumInPool(udtScan.ScanNumber, intPoolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        intSpectrumExportCount += 1

        With udtScan
            ' First, write an entry to the "_scans.csv" file

            If udtRawDataExportOptions.RenumberScans Then
                intScanNumber = intSpectrumExportCount
            Else
                intScanNumber = .ScanNumber
            End If

            If blnFragmentationScan Then
                intMSLevel = .FragScanInfo.MSLevel
            Else
                intMSLevel = 1
            End If

            intNumIsotopicSignatures = 0
            intNumPeaks = objSpectraCache.SpectraPool(intPoolIndex).IonCount

            sngBaselineNoiseLevel = .BaselineNoiseStats.NoiseLevel
            If sngBaselineNoiseLevel < 1 Then sngBaselineNoiseLevel = 1

            ' Old Column Order:
            ''strLineOut = intScanNumber.ToString & "," &
            ''             .ScanTime.ToString("0.0000") &
            ''             intMSLevel & "," &
            ''             intNumIsotopicSignatures & "," &
            ''             intNumPeaks & "," &
            ''             .TotalIonIntensity.ToString & "," &
            ''             .BasePeakIonMZ & "," &
            ''             .BasePeakIonIntensity & "," &
            ''             sngTimeDomainIntensity & "," &
            ''             sngPeakIntensityThreshold & "," &
            ''             sngPeptideIntensityThreshold

            WriteDecon2LSScanFileEntry(srScanInfoOutfile, udtScan, intScanNumber, intMSLevel, intNumPeaks, intNumIsotopicSignatures)
        End With


        With objSpectraCache.SpectraPool(intPoolIndex)
            ' Now write an entry to the "_isos.csv" file

            If .IonCount > 0 Then
                ' Populate sngIntensities and intPointerArray()
                ReDim sngIntensities(.IonCount - 1)
                ReDim intPointerArray(.IonCount - 1)
                For intIonIndex = 0 To .IonCount - 1
                    sngIntensities(intIonIndex) = .IonsIntensity(intIonIndex)
                    intPointerArray(intIonIndex) = intIonIndex
                Next intIonIndex

                ' Sort intPointerArray() based on the intensities in sngIntensities
                Array.Sort(sngIntensities, intPointerArray)

                If udtRawDataExportOptions.MaxIonCountPerScan > 0 Then
                    ' Possibly limit the number of ions to intMaxIonCount
                    intStartIndex = .IonCount - udtRawDataExportOptions.MaxIonCountPerScan
                    If intStartIndex < 0 Then intStartIndex = 0
                Else
                    intStartIndex = 0
                End If

                ' Define the minimum data point intensity value
                sngMinimumIntensityCurrentScan = .IonsIntensity(intPointerArray(intStartIndex))

                ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, udtRawDataExportOptions.IntensityMinimum)

                ' If udtRawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update sngMinimumIntensityCurrentScan
                If udtRawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                    sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, udtScan.BaselineNoiseStats.NoiseLevel * udtRawDataExportOptions.MinimumSignalToNoiseRatio)
                End If

                intExportCount = 0
                For intIonIndex = 0 To .IonCount - 1
                    If .IonsIntensity(intIonIndex) >= sngMinimumIntensityCurrentScan Then

                        intCharge = 1
                        sngFit = 0
                        dblMass = ConvoluteMass(.IonsMZ(intIonIndex), 1, 0)
                        sngFWHM = 0
                        sngSignalToNoise = .IonsIntensity(intIonIndex) / sngBaselineNoiseLevel
                        sngMonoisotopicAbu = -10
                        sngMonoPlus2Abu = -10

                        WriteDecon2LSIsosFileEntry(
                          srDataOutFile, intScanNumber, intCharge,
                          .IonsIntensity(intIonIndex), .IonsMZ(intIonIndex),
                          sngFit, dblMass, dblMass, dblMass,
                          sngFWHM, sngSignalToNoise, sngMonoisotopicAbu, sngMonoPlus2Abu)

                        intExportCount += 1
                    End If
                Next intIonIndex
            End If

        End With

    End Sub

    Private Sub SavePEKFileToDiskWork(
      ByRef srOutFile As StreamWriter,
      ByRef udtScan As udtScanInfoType,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      blnFragmentationScan As Boolean,
      ByRef intSpectrumExportCount As Integer,
      udtRawDataExportOptions As udtRawDataExportOptionsType)

        Dim strLineOut As String
        Dim intIonIndex As Integer
        Dim sngIntensities() As Single
        Dim intPointerArray() As Integer

        Dim intScanNumber As Integer
        Dim intPoolIndex As Integer

        Dim intStartIndex As Integer, intExportCount As Integer
        Dim sngMinimumIntensityCurrentScan As Single

        intExportCount = 0

        If Not objSpectraCache.ValidateSpectrumInPool(udtScan.ScanNumber, intPoolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        intSpectrumExportCount += 1

        With udtScan

            srOutFile.WriteLine("Time domain signal level:" & ControlChars.Tab & .BasePeakIonIntensity.ToString)          ' Store the base peak ion intensity as the time domain signal level value

            srOutFile.WriteLine("MASIC " & MyBase.FileVersion)                     ' Software version
            strLineOut = "MS/MS-based PEK file"
            If udtRawDataExportOptions.IncludeMSMS Then
                strLineOut &= " (includes both survey scans and fragmentation spectra)"
            Else
                strLineOut &= " (includes only survey scans)"
            End If
            srOutFile.WriteLine(strLineOut)

            If udtRawDataExportOptions.RenumberScans Then
                intScanNumber = intSpectrumExportCount
            Else
                intScanNumber = .ScanNumber
            End If
            strLineOut = "Filename: " & strInputFileName & "." & intScanNumber.ToString("00000")
            srOutFile.WriteLine(strLineOut)

            If blnFragmentationScan Then
                srOutFile.WriteLine("ScanType: Fragmentation Scan")
            Else
                srOutFile.WriteLine("ScanType: Survey Scan")
            End If

            srOutFile.WriteLine("Charge state mass transform results:")
            srOutFile.WriteLine("First CS,    Number of CS,   Abundance,   Mass,   Standard deviation")
        End With

        With objSpectraCache.SpectraPool(intPoolIndex)

            If .IonCount > 0 Then
                ' Populate sngIntensities and intPointerArray()
                ReDim sngIntensities(.IonCount - 1)
                ReDim intPointerArray(.IonCount - 1)
                For intIonIndex = 0 To .IonCount - 1
                    sngIntensities(intIonIndex) = .IonsIntensity(intIonIndex)
                    intPointerArray(intIonIndex) = intIonIndex
                Next intIonIndex

                ' Sort intPointerArray() based on the intensities in sngIntensities
                Array.Sort(sngIntensities, intPointerArray)

                If udtRawDataExportOptions.MaxIonCountPerScan > 0 Then
                    ' Possibly limit the number of ions to intMaxIonCount
                    intStartIndex = .IonCount - udtRawDataExportOptions.MaxIonCountPerScan
                    If intStartIndex < 0 Then intStartIndex = 0
                Else
                    intStartIndex = 0
                End If


                ' Define the minimum data point intensity value
                sngMinimumIntensityCurrentScan = .IonsIntensity(intPointerArray(intStartIndex))

                ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, udtRawDataExportOptions.IntensityMinimum)

                ' If udtRawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update sngMinimumIntensityCurrentScan
                If udtRawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                    sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, udtScan.BaselineNoiseStats.NoiseLevel * udtRawDataExportOptions.MinimumSignalToNoiseRatio)
                End If

                intExportCount = 0
                For intIonIndex = 0 To .IonCount - 1
                    If .IonsIntensity(intIonIndex) >= sngMinimumIntensityCurrentScan Then
                        strLineOut = "1" & ControlChars.Tab & "1" & ControlChars.Tab & .IonsIntensity(intIonIndex) & ControlChars.Tab & .IonsMZ(intIonIndex) & ControlChars.Tab & "0"
                        srOutFile.WriteLine(strLineOut)
                        intExportCount += 1
                    End If
                Next intIonIndex
            End If

            srOutFile.WriteLine("Number of peaks in spectrum = " & .IonCount.ToString)
            srOutFile.WriteLine("Number of isotopic distributions detected = " & intExportCount.ToString)
            srOutFile.WriteLine()

        End With

    End Sub

    Private Sub SaveScanStatEntry(
      swOutFile As StreamWriter,
      eScanType As eScanTypeConstants,
      ByRef udtCurrentScan As udtScanInfoType,
      intDatasetNumber As Integer)

        Const cColDelimiter As Char = ControlChars.Tab
        Dim strLineOut As String

        Dim objScanStatsEntry As New DSSummarizer.clsScanStatsEntry

        objScanStatsEntry.ScanNumber = udtCurrentScan.ScanNumber

        If eScanType = eScanTypeConstants.SurveyScan Then
            objScanStatsEntry.ScanType = 1
            objScanStatsEntry.ScanTypeName = String.Copy(udtCurrentScan.ScanTypeName)
        Else
            If udtCurrentScan.FragScanInfo.MSLevel <= 1 Then
                ' This is a fragmentation scan, so it must have a scan type of at least 2
                objScanStatsEntry.ScanType = 2
            Else
                ' .MSLevel is 2 or higher, record the actual MSLevel value
                objScanStatsEntry.ScanType = udtCurrentScan.FragScanInfo.MSLevel
            End If
            objScanStatsEntry.ScanTypeName = String.Copy(udtCurrentScan.ScanTypeName)
        End If

        objScanStatsEntry.ScanFilterText = udtCurrentScan.ScanHeaderText

        objScanStatsEntry.ElutionTime = udtCurrentScan.ScanTime.ToString("0.0000")
        objScanStatsEntry.TotalIonIntensity = StringUtilities.ValueToString(udtCurrentScan.TotalIonIntensity, 5)
        objScanStatsEntry.BasePeakIntensity = StringUtilities.ValueToString(udtCurrentScan.BasePeakIonIntensity, 5)
        objScanStatsEntry.BasePeakMZ = Math.Round(udtCurrentScan.BasePeakIonMZ, 4).ToString

        ' Base peak signal to noise ratio
        objScanStatsEntry.BasePeakSignalToNoiseRatio = StringUtilities.ValueToString(MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(udtCurrentScan.BasePeakIonIntensity, udtCurrentScan.BaselineNoiseStats.NoiseLevel), 4)

        objScanStatsEntry.IonCount = udtCurrentScan.IonCount
        objScanStatsEntry.IonCountRaw = udtCurrentScan.IonCountRaw

        mScanStats.Add(objScanStatsEntry)

        strLineOut = String.Empty
        strLineOut &= intDatasetNumber.ToString & cColDelimiter                     ' Dataset number
        strLineOut &= objScanStatsEntry.ScanNumber.ToString & cColDelimiter         ' Scan number
        strLineOut &= objScanStatsEntry.ElutionTime & cColDelimiter                 ' Scan time (minutes)
        strLineOut &= objScanStatsEntry.ScanType.ToString & cColDelimiter           ' Scan type (1 for MS, 2 for MS2, etc.)
        strLineOut &= objScanStatsEntry.TotalIonIntensity & cColDelimiter           ' Total ion intensity
        strLineOut &= objScanStatsEntry.BasePeakIntensity & cColDelimiter           ' Base peak ion intensity
        strLineOut &= objScanStatsEntry.BasePeakMZ & cColDelimiter                  ' Base peak ion m/z
        strLineOut &= objScanStatsEntry.BasePeakSignalToNoiseRatio & cColDelimiter  ' Base peak signal to noise ratio
        strLineOut &= objScanStatsEntry.IonCount.ToString & cColDelimiter           ' Number of peaks (aka ions) in the spectrum
        strLineOut &= objScanStatsEntry.IonCountRaw.ToString & cColDelimiter        ' Number of peaks (aka ions) in the spectrum prior to any filtering
        strLineOut &= objScanStatsEntry.ScanTypeName                                ' Scan type name

        swOutFile.WriteLine(strLineOut)

    End Sub

    ''' <summary>
    ''' Converts a scan number of acquisition time to an actual scan number
    ''' </summary>
    ''' <param name="udtScanList"></param>
    ''' <param name="sngScanOrAcqTime">Value to convert</param>
    ''' <param name="eScanType">Type of the value to convert; 0=Absolute, 1=Relative, 2=Acquisition Time (aka elution time)</param>
    ''' <param name="blnConvertingRangeOrTolerance">True when converting a range</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function ScanOrAcqTimeToAbsolute(
      ByRef udtScanList As udtScanListType,
      sngScanOrAcqTime As Single,
      eScanType As eCustomSICScanTypeConstants,
      blnConvertingRangeOrTolerance As Boolean) As Integer


        Dim intTotalScanRange As Integer
        Dim intAbsoluteScanNumber As Integer
        Dim intMasterScanIndex As Integer

        Dim sngTotalRunTime As Single
        Dim sngRelativeTime As Single

        Try
            Select Case eScanType
                Case eCustomSICScanTypeConstants.Absolute
                    ' sngScanOrAcqTime is an absolute scan number (or range of scan numbers)
                    ' No conversion needed; simply return the value
                    intAbsoluteScanNumber = CInt(sngScanOrAcqTime)

                Case eCustomSICScanTypeConstants.Relative
                    ' sngScanOrAcqTime is a fraction of the total number of scans (for example, 0.5)

                    ' Use the total range of scan numbers
                    With udtScanList
                        If .MasterScanOrderCount > 0 Then
                            intTotalScanRange = .MasterScanNumList(.MasterScanOrderCount - 1) - .MasterScanNumList(0)

                            intAbsoluteScanNumber = CInt(sngScanOrAcqTime * intTotalScanRange + .MasterScanNumList(0))
                        Else
                            intAbsoluteScanNumber = 0
                        End If
                    End With

                Case eCustomSICScanTypeConstants.AcquisitionTime
                    ' sngScanOrAcqTime is an elution time value
                    ' If blnConvertingRangeOrTolerance = False, then look for the scan that is nearest to sngScanOrAcqTime
                    ' If blnConvertingRangeOrTolerance = True, then Convert sngScanOrAcqTime to a relative scan range and then
                    '   call this function again with that relative time

                    If blnConvertingRangeOrTolerance Then
                        With udtScanList
                            sngTotalRunTime = .MasterScanTimeList(.MasterScanOrderCount - 1) - .MasterScanTimeList(0)
                            If sngTotalRunTime < 0.1 Then
                                sngTotalRunTime = 1
                            End If

                            sngRelativeTime = sngScanOrAcqTime / sngTotalRunTime
                        End With

                        intAbsoluteScanNumber = ScanOrAcqTimeToAbsolute(udtScanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, True)
                    Else
                        intMasterScanIndex = FindNearestScanNumIndex(udtScanList, sngScanOrAcqTime, eScanType)
                        If intMasterScanIndex >= 0 AndAlso udtScanList.MasterScanOrderCount > 0 Then
                            intAbsoluteScanNumber = udtScanList.MasterScanNumList(intMasterScanIndex)
                        End If
                    End If


                Case Else
                    ' Unknown type; assume absolute scan number
                    intAbsoluteScanNumber = CInt(sngScanOrAcqTime)
            End Select


        Catch ex As Exception
            LogErrors("ScanOrAcqTimeToAbsolute", "Error in clsMasic->ScanOrAcqTimeToAbsolute ", ex, True, False)
            intAbsoluteScanNumber = 0
        End Try

        Return intAbsoluteScanNumber

    End Function

    Private Function ScanOrAcqTimeToScanTime(
      ByRef udtScanList As udtScanListType,
      sngScanOrAcqTime As Single,
      eScanType As eCustomSICScanTypeConstants,
      blnConvertingRangeOrTolerance As Boolean) As Single


        Dim intMasterScanIndex As Integer

        Dim sngTotalRunTime As Single
        Dim sngRelativeTime As Single

        Dim sngComputedScanTime As Single

        Try
            Select Case eScanType
                Case eCustomSICScanTypeConstants.Absolute
                    ' sngScanOrAcqTime is an absolute scan number (or range of scan numbers)

                    ' If blnConvertingRangeOrTolerance = False, then look for the scan that is nearest to sngScanOrAcqTime
                    ' If blnConvertingRangeOrTolerance = True, then Convert sngScanOrAcqTime to a relative scan range and then
                    '   call this function again with that relative time

                    If blnConvertingRangeOrTolerance Then
                        With udtScanList
                            Dim intTotalScans As Integer
                            intTotalScans = .MasterScanNumList(.MasterScanOrderCount - 1) - .MasterScanNumList(0)
                            If intTotalScans < 1 Then
                                intTotalScans = 1
                            End If

                            sngRelativeTime = sngScanOrAcqTime / intTotalScans
                        End With

                        sngComputedScanTime = ScanOrAcqTimeToScanTime(udtScanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, True)
                    Else
                        intMasterScanIndex = FindNearestScanNumIndex(udtScanList, sngScanOrAcqTime, eScanType)
                        If intMasterScanIndex >= 0 AndAlso udtScanList.MasterScanOrderCount > 0 Then
                            sngComputedScanTime = udtScanList.MasterScanTimeList(intMasterScanIndex)
                        End If
                    End If

                Case eCustomSICScanTypeConstants.Relative
                    ' sngScanOrAcqTime is a fraction of the total number of scans (for example, 0.5)

                    ' Use the total range of scan times
                    With udtScanList
                        If .MasterScanOrderCount > 0 Then
                            sngTotalRunTime = .MasterScanTimeList(.MasterScanOrderCount - 1) - .MasterScanTimeList(0)

                            sngComputedScanTime = CSng(sngScanOrAcqTime * sngTotalRunTime + .MasterScanTimeList(0))
                        Else
                            sngComputedScanTime = 0
                        End If
                    End With

                Case eCustomSICScanTypeConstants.AcquisitionTime
                    ' sngScanOrAcqTime is an elution time value (or elution time range)
                    ' No conversion needed; simply return the value
                    sngComputedScanTime = sngScanOrAcqTime

                Case Else
                    ' Unknown type; assume already a scan time
                    sngComputedScanTime = sngScanOrAcqTime
            End Select


        Catch ex As Exception
            LogErrors("ScanOrAcqTimeToAbsolute", "Error in clsMasic->ScanOrAcqTimeToScanTime ", ex, True, False)
            sngComputedScanTime = 0
        End Try

        Return sngComputedScanTime

    End Function

    Protected Function ScanNumberToIndex(
      ByRef udtScanList As udtScanListType,
      intScanNumber As Integer,
      ByRef eScanType As eScanTypeConstants) As Integer

        ' Looks for intScanNumber in udtScanList and returns the index containing the scan number
        ' Returns -1 if no match

        Dim intIndex As Integer
        Dim intScanIndexMatch As Integer
        Static objScanInfoComparer As clsScanInfoScanNumComparer

        If objScanInfoComparer Is Nothing Then
            objScanInfoComparer = New clsScanInfoScanNumComparer
        End If

        intScanIndexMatch = -1
        eScanType = eScanTypeConstants.SurveyScan

        intScanIndexMatch = Array.BinarySearch(udtScanList.SurveyScans, 0, udtScanList.SurveyScanCount, intScanNumber, objScanInfoComparer)
        If intScanIndexMatch >= 0 Then
            eScanType = eScanTypeConstants.SurveyScan
        Else
            intScanIndexMatch = Array.BinarySearch(udtScanList.FragScans, 0, udtScanList.FragScanCount, intScanNumber, objScanInfoComparer)
            If intScanIndexMatch >= 0 Then
                eScanType = eScanTypeConstants.FragScan
            Else
                ' Match still not found; brute force search udtScanList.SurveyScans for intScanNumber
                For intIndex = 0 To udtScanList.SurveyScanCount - 1
                    If udtScanList.SurveyScans(intIndex).ScanNumber = intScanNumber Then
                        intScanIndexMatch = intIndex
                        eScanType = eScanTypeConstants.SurveyScan
                        Exit For
                    End If
                Next intIndex

                If intScanIndexMatch < 0 Then
                    ' Still no match; brute force search & udtScanList.FragScans for intScanNumber
                    For intIndex = 0 To udtScanList.FragScanCount - 1
                        If udtScanList.FragScans(intIndex).ScanNumber = intScanNumber Then
                            intScanIndexMatch = intIndex
                            eScanType = eScanTypeConstants.FragScan
                            Exit For
                        End If
                    Next intIndex
                End If
            End If
        End If

        Return intScanIndexMatch

    End Function

    Protected Function ScanNumberToScanTime(
      ByRef udtScanList As udtScanListType,
      intScanNumber As Integer) As Single

        Dim intScanIndexMatch As Integer
        Dim eScanTypeMatch As eScanTypeConstants

        Dim sngScantime As Single

        sngScantime = 0

        intScanIndexMatch = ScanNumberToIndex(udtScanList, intScanNumber, eScanTypeMatch)
        If intScanIndexMatch >= 0 Then
            Select Case eScanTypeMatch
                Case eScanTypeConstants.SurveyScan
                    sngScantime = udtScanList.SurveyScans(intScanIndexMatch).ScanTime
                Case eScanTypeConstants.FragScan
                    sngScantime = udtScanList.FragScans(intScanIndexMatch).ScanTime
                Case Else
                    ' Unknown scan type
            End Select
        End If

        Return sngScantime

    End Function

    Public Function SetCustomSICListValues(
      eScanType As eCustomSICScanTypeConstants,
      dblMZToleranceDa As Double,
      sngScanOrAcqTimeTolerance As Single,
      dblMZList() As Double,
      dblMZToleranceList() As Double,
      sngScanOrAcqTimeCenterList() As Single,
      sngScanOrAcqTimeToleranceList() As Single,
      strScanComments() As String) As Boolean

        ' Returns True if sucess

        Dim intIndex As Integer

        If dblMZToleranceList.Length > 0 AndAlso dblMZToleranceList.Length <> dblMZList.Length Then
            ' Invalid Custom SIC comment list; number of entries doesn't match
            Return False
        ElseIf sngScanOrAcqTimeCenterList.Length > 0 AndAlso sngScanOrAcqTimeCenterList.Length <> dblMZList.Length Then
            ' Invalid Custom SIC scan center list; number of entries doesn't match
            Return False
        ElseIf sngScanOrAcqTimeToleranceList.Length > 0 AndAlso sngScanOrAcqTimeToleranceList.Length <> dblMZList.Length Then
            ' Invalid Custom SIC scan center list; number of entries doesn't match
            Return False
        ElseIf strScanComments.Length > 0 AndAlso strScanComments.Length <> dblMZList.Length Then
            ' Invalid Custom SIC comment list; number of entries doesn't match
            Return False
        End If


        With mCustomSICList
            .ScanToleranceType = eScanType
            .ScanOrAcqTimeTolerance = sngScanOrAcqTimeTolerance     ' This value is used if sngScanOrAcqTimeToleranceList is blank or for any entries in sngScanOrAcqTimeToleranceList() that are zero

            If dblMZList.Length > 0 Then
                .RawTextMZList = String.Empty
                .RawTextMZToleranceDaList = String.Empty
                .RawTextScanOrAcqTimeCenterList = String.Empty
                .RawTextScanOrAcqTimeToleranceList = String.Empty

                ReDim .CustomMZSearchValues(dblMZList.Length - 1)

                For intIndex = 0 To dblMZList.Length - 1

                    With .CustomMZSearchValues(intIndex)
                        .MZ = dblMZList(intIndex)

                        If dblMZToleranceList.Length > intIndex AndAlso dblMZToleranceList(intIndex) > 0 Then
                            .MZToleranceDa = dblMZToleranceList(intIndex)
                        Else
                            .MZToleranceDa = dblMZToleranceDa
                        End If

                        If sngScanOrAcqTimeCenterList.Length > intIndex Then
                            .ScanOrAcqTimeCenter = sngScanOrAcqTimeCenterList(intIndex)
                        Else
                            .ScanOrAcqTimeCenter = 0         ' Set to 0 to indicate that the entire file should be searched
                        End If

                        If sngScanOrAcqTimeToleranceList.Length > intIndex AndAlso sngScanOrAcqTimeToleranceList(intIndex) > 0 Then
                            .ScanOrAcqTimeTolerance = sngScanOrAcqTimeToleranceList(intIndex)
                        Else
                            .ScanOrAcqTimeTolerance = sngScanOrAcqTimeTolerance
                        End If

                        If strScanComments.Length > 0 AndAlso strScanComments.Length > intIndex Then
                            .Comment = strScanComments(intIndex)
                        Else
                            .Comment = String.Empty
                        End If

                        If intIndex = 0 Then
                            mCustomSICList.RawTextMZList = .MZ.ToString
                            mCustomSICList.RawTextMZToleranceDaList = .MZToleranceDa.ToString
                            mCustomSICList.RawTextScanOrAcqTimeCenterList = .ScanOrAcqTimeCenter.ToString
                            mCustomSICList.RawTextScanOrAcqTimeToleranceList = .ScanOrAcqTimeTolerance.ToString
                        Else
                            mCustomSICList.RawTextMZList &= ","c & .MZ.ToString
                            mCustomSICList.RawTextMZToleranceDaList &= ","c & .MZToleranceDa.ToString
                            mCustomSICList.RawTextScanOrAcqTimeCenterList &= ","c & .ScanOrAcqTimeCenter.ToString
                            mCustomSICList.RawTextScanOrAcqTimeToleranceList &= ","c & .ScanOrAcqTimeTolerance.ToString
                        End If

                    End With

                Next intIndex

                ValidateCustomSICList()
            Else
                ReDim .CustomMZSearchValues(-1)
            End If
        End With

        Return True

    End Function

    Public Sub SetReporterIons(dblReporterIonMZList() As Double)
        SetReporterIons(dblReporterIonMZList, REPORTER_ION_TOLERANCE_DA_DEFAULT)
    End Sub

    Public Sub SetReporterIons(dblReporterIonMZList() As Double, dblMZToleranceDa As Double)
        SetReporterIons(dblReporterIonMZList, dblMZToleranceDa, True)
    End Sub

    Public Sub SetReporterIons(udtReporterIonInfo() As udtReporterIonInfoType)
        SetReporterIons(udtReporterIonInfo, True)
    End Sub

    Protected Sub SetReporterIons(
      udtReporterIonInfo() As udtReporterIonInfoType,
      blnCustomReporterIons As Boolean)

        Dim intIndex As Integer

        If udtReporterIonInfo Is Nothing OrElse udtReporterIonInfo.Length = 0 Then
            mReporterIonCount = 0
            ReDim mReporterIonInfo(0)
        Else
            mReporterIonCount = udtReporterIonInfo.Length
            ReDim mReporterIonInfo(mReporterIonCount - 1)

            Array.Copy(udtReporterIonInfo, mReporterIonInfo, udtReporterIonInfo.Length)

            For intIndex = 0 To mReporterIonCount - 1
                If mReporterIonInfo(intIndex).MZToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM Then
                    mReporterIonInfo(intIndex).MZToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM
                End If
            Next

        End If

        If blnCustomReporterIons Then
            mReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone
        End If

    End Sub

    Protected Sub SetReporterIons(
      dblReporterIonMZList() As Double,
      dblMZToleranceDa As Double, blnCustomReporterIons As Boolean)

        ' dblMZToleranceDa is the search tolerance (half width)
        Dim intIndex As Integer

        If dblMZToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM Then
            dblMZToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM
        End If

        If dblReporterIonMZList Is Nothing OrElse dblReporterIonMZList.Length = 0 Then
            mReporterIonCount = 0
            ReDim mReporterIonInfo(0)
            mReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone
        Else
            mReporterIonCount = dblReporterIonMZList.Length
            ReDim mReporterIonInfo(mReporterIonCount - 1)

            For intIndex = 0 To mReporterIonCount - 1
                mReporterIonInfo(intIndex).MZ = dblReporterIonMZList(intIndex)
                mReporterIonInfo(intIndex).MZToleranceDa = dblMZToleranceDa
            Next
        End If

        If blnCustomReporterIons Then
            mReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone
        End If
    End Sub

    Public Sub SetReporterIonMassMode(eReporterIonMassMode As eReporterIonMassModeConstants)
        If eReporterIonMassMode = eReporterIonMassModeConstants.ITraqEightMZHighRes Then
            SetReporterIonMassMode(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES)
        Else
            SetReporterIonMassMode(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT)
        End If
    End Sub

    Public Sub SetReporterIonMassMode(
      eReporterIonMassMode As eReporterIonMassModeConstants,
      dblMZToleranceDa As Double)

        ' Note: If eReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone then nothing is changed

        Dim udtReporterIonInfo() As udtReporterIonInfoType

        If eReporterIonMassMode <> eReporterIonMassModeConstants.CustomOrNone Then
            Me.ReporterIonToleranceDaDefault = dblMZToleranceDa

            udtReporterIonInfo = GetDefaultReporterIons(eReporterIonMassMode, dblMZToleranceDa)

            SetReporterIons(udtReporterIonInfo, False)
            mReporterIonMassMode = eReporterIonMassMode
        End If

    End Sub

    Private Sub SetDefaultPeakLocValues(ByRef udtScanList As udtScanListType)

        Dim intParentIonIndex As Integer
        Dim intScanIndexObserved As Integer

        Try
            For intParentIonIndex = 0 To udtScanList.ParentIonInfoCount - 1
                With udtScanList.ParentIons(intParentIonIndex)
                    intScanIndexObserved = .SurveyScanIndex

                    With .SICStats
                        .ScanTypeForPeakIndices = eScanTypeConstants.SurveyScan
                        .PeakScanIndexStart = intScanIndexObserved
                        .PeakScanIndexEnd = intScanIndexObserved
                        .PeakScanIndexMax = intScanIndexObserved
                    End With
                End With
            Next intParentIonIndex
        Catch ex As Exception
            LogErrors("SetDefaultPeakLocValues", "Error in clsMasic->SetDefaultPeakLocValues ", ex, True, False)
        End Try

    End Sub

    Private Sub SetLocalErrorCode(eNewErrorCode As eMasicErrorCodes)
        SetLocalErrorCode(eNewErrorCode, False)
    End Sub

    Private Sub SetLocalErrorCode(
      eNewErrorCode As eMasicErrorCodes,
      blnLeaveExistingErrorCodeUnchanged As Boolean)


        If blnLeaveExistingErrorCodeUnchanged AndAlso mLocalErrorCode <> eMasicErrorCodes.NoError Then
            ' An error code is already defined; do not change it
        Else
            mLocalErrorCode = eNewErrorCode

            If eNewErrorCode = eMasicErrorCodes.NoError Then
                If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError Then
                    MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError)
                End If
            Else
                MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError)
            End If
        End If

    End Sub

    Private Sub SetSubtaskProcessingStepPct(NewStepPct As Short)
        SetSubtaskProcessingStepPct(NewStepPct, False)
    End Sub

    Private Sub SetSubtaskProcessingStepPct(NewStepPct As Short, blnForceUpdate As Boolean)
        Const MINIMUM_PROGRESS_UPDATE_INTERVAL_MILLISECONDS = 250

        Dim blnRaiseEvent As Boolean
        Static LastFileWriteTime As DateTime = DateTime.UtcNow

        If NewStepPct = 0 Then
            mAbortProcessing = False
            RaiseEvent ProgressResetKeypressAbort()
            blnRaiseEvent = True
        End If

        If NewStepPct <> mSubtaskProcessingStepPct Then
            blnRaiseEvent = True
            mSubtaskProcessingStepPct = NewStepPct
        End If

        If blnForceUpdate OrElse blnRaiseEvent OrElse DateTime.UtcNow.Subtract(LastFileWriteTime).TotalMilliseconds >= MINIMUM_PROGRESS_UPDATE_INTERVAL_MILLISECONDS Then
            LastFileWriteTime = DateTime.UtcNow

            UpdateOverallProgress()
            UpdateStatusFile()
            RaiseEvent ProgressSubtaskChanged()
        End If
    End Sub

    Private Sub SetSubtaskProcessingStepPct(NewStepPct As Short, strSubtaskDescription As String)
        mSubtaskDescription = strSubtaskDescription
        SetSubtaskProcessingStepPct(NewStepPct, True)
    End Sub

    Public Sub SetStatusLogKeyNameFilterList(ByRef strMatchSpecList As String, chDelimiter As Char)
        Dim strItems() As String
        Dim intIndex As Integer
        Dim intTargetIndex As Integer

        Try
            ' Split on the user-specified delimiter, plus also CR and LF
            strItems = strMatchSpecList.Split(New Char() {chDelimiter, ControlChars.Cr, ControlChars.Lf})

            If strItems.Length > 0 Then

                ' Make sure no blank entries are present in strItems
                intTargetIndex = 0
                For intIndex = 0 To strItems.Length - 1
                    strItems(intIndex) = strItems(intIndex).Trim
                    If strItems(intIndex).Length > 0 Then
                        If intTargetIndex <> intIndex Then
                            strItems(intTargetIndex) = String.Copy(strItems(intIndex))
                        End If
                        intTargetIndex += 1
                    End If
                Next intIndex

                If intTargetIndex < strItems.Length Then
                    ReDim Preserve strItems(intTargetIndex - 1)
                End If

                SetStatusLogKeyNameFilterList(strItems)
            End If
        Catch ex As Exception
            ' Error parsing strMatchSpecList
            ' Ignore errors here
        End Try
    End Sub

    Public Sub SetStatusLogKeyNameFilterList(ByRef strMatchSpecList() As String)
        Try
            If Not strMatchSpecList Is Nothing Then
                ReDim mStatusLogKeyNameFilterList(strMatchSpecList.Length - 1)
                Array.Copy(strMatchSpecList, mStatusLogKeyNameFilterList, strMatchSpecList.Length)
                Array.Sort(mStatusLogKeyNameFilterList)
            End If
        Catch ex As Exception
            ' Ignore errors here
        End Try
    End Sub

    Private Sub StoreExtendedHeaderInfo(
      ByRef htExtendedHeaderInfo As Dictionary(Of Integer, String),
      strEntryName As String,
      strEntryValue As String)


        If strEntryValue Is Nothing Then
            strEntryValue = String.Empty
        End If

        Dim statusEntries = New List(Of KeyValuePair(Of String, String))
        statusEntries.Add(New KeyValuePair(Of String, String)(strEntryName, strEntryValue))

        StoreExtendedHeaderInfo(htExtendedHeaderInfo, statusEntries)

    End Sub

    Private Sub StoreExtendedHeaderInfo(
      ByRef htExtendedHeaderInfo As Dictionary(Of Integer, String),
      statusEntries As List(Of KeyValuePair(Of String, String)))

        StoreExtendedHeaderInfo(htExtendedHeaderInfo, statusEntries, New String() {})
    End Sub

    Private Sub StoreExtendedHeaderInfo(
      ByRef htExtendedHeaderInfo As Dictionary(Of Integer, String),
      statusEntries As List(Of KeyValuePair(Of String, String)),
      ByRef strKeyNameFilterList() As String)

        Dim intIndex As Integer
        Dim intIDValue As Integer
        Dim intFilterIndex As Integer

        Dim blnFilterItems As Boolean
        Dim blnSaveItem As Boolean

        If htExtendedHeaderInfo Is Nothing Then
            htExtendedHeaderInfo = New Dictionary(Of Integer, String)
        End If

        Try
            If (statusEntries Is Nothing) Then Exit Sub

            If Not strKeyNameFilterList Is Nothing AndAlso strKeyNameFilterList.Length > 0 Then
                For intIndex = 0 To strKeyNameFilterList.Length - 1
                    If strKeyNameFilterList(intIndex).Length > 0 Then
                        blnFilterItems = True
                        Exit For
                    End If
                Next
            End If

            For Each statusEntry In statusEntries
                If String.IsNullOrWhiteSpace(statusEntry.Key) Then
                    ' Empty entry name; do not add
                    Continue For
                End If

                If blnFilterItems Then
                    blnSaveItem = False
                    For intFilterIndex = 0 To strKeyNameFilterList.Length - 1
                        If statusEntry.Key.ToLower().Contains(strKeyNameFilterList(intFilterIndex).ToLower()) Then
                            blnSaveItem = True
                            Exit For
                        End If
                    Next intFilterIndex
                Else
                    blnSaveItem = True
                End If

                If blnSaveItem Then
                    If TryGetExtendedHeaderInfoValue(statusEntry.Key, intIDValue) Then
                        ' Match found
                    Else
                        intIDValue = mExtendedHeaderInfo.Count
                        mExtendedHeaderInfo.Add(New KeyValuePair(Of String, Integer)(statusEntry.Key, intIDValue))
                    End If

                    ' Add or update the value for intIDValue
                    htExtendedHeaderInfo(intIDValue) = statusEntry.Value
                End If

            Next

        Catch ex As Exception
            ' Ignore any errors here
        End Try

    End Sub

    Private Sub StoreMzXmlSpectrum(
      objMSSpectrum As clsMSSpectrum,
      ByRef udtScanInfo As udtScanInfoType,
      objSpectraCache As clsSpectraCache,
      udtNoiseThresholdOptions As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseOptionsType,
      blnDiscardLowIntensityData As Boolean,
      blnCompressSpectraData As Boolean,
      dblMSDataResolution As Double,
      blnKeepRawSpectrum As Boolean)

        Dim intIonIndex As Integer
        Dim sngTotalIonIntensity As Single

        Try

            If objMSSpectrum.IonsMZ Is Nothing OrElse objMSSpectrum.IonsIntensity Is Nothing Then
                udtScanInfo.IonCount = 0
                udtScanInfo.IonCountRaw = 0
            Else
                objMSSpectrum.IonCount = objMSSpectrum.IonsMZ.Length

                With udtScanInfo
                    .IonCount = objMSSpectrum.IonCount
                    .IonCountRaw = .IonCount
                End With
            End If

            objMSSpectrum.ScanNumber = udtScanInfo.ScanNumber

            If udtScanInfo.IonCount > 0 Then
                With udtScanInfo
                    ' Confirm the total scan intensity stored in the mzXML file
                    sngTotalIonIntensity = 0
                    For intIonIndex = 0 To objMSSpectrum.IonCount - 1
                        sngTotalIonIntensity += objMSSpectrum.IonsIntensity(intIonIndex)
                    Next intIonIndex

                    If .TotalIonIntensity < Single.Epsilon Then
                        .TotalIonIntensity = sngTotalIonIntensity
                    End If

                End With

                ProcessAndStoreSpectrum(udtScanInfo, objSpectraCache, objMSSpectrum, udtNoiseThresholdOptions, blnDiscardLowIntensityData, blnCompressSpectraData, dblMSDataResolution, blnKeepRawSpectrum)
            Else
                udtScanInfo.TotalIonIntensity = 0
            End If

        Catch ex As Exception
            LogErrors("StoreMzXMLSpectrum", "Error in clsMasic->StoreMzXMLSpectrum ", ex, True, True)
        End Try

    End Sub

    Private Function StorePeakInParentIon(
      ByRef udtScanList As udtScanListType,
      intParentIonIndex As Integer,
      ByRef udtSICDetails As udtSICStatsDetailsType,
      ByRef udtSICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType,
      ByRef udtSICPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType,
      blnPeakIsValid As Boolean) As Boolean


        Dim intDataIndex As Integer
        Dim intScanIndexObserved As Integer
        Dim intFragScanNumber As Integer

        Dim blnProcessingMRMPeak As Boolean
        Dim blnSuccess As Boolean

        Try

            With udtScanList
                If udtSICDetails.SICData Is Nothing OrElse udtSICDetails.SICDataCount = 0 Then
                    ' Either .SICData is nothing or no SIC data exists
                    ' Cannot find peaks for this parent ion
                    With .ParentIons(intParentIonIndex).SICStats
                        With .Peak
                            .IndexObserved = 0
                            .IndexBaseLeft = .IndexObserved
                            .IndexBaseRight = .IndexObserved
                            .IndexMax = .IndexObserved
                        End With
                    End With
                Else
                    With .ParentIons(intParentIonIndex)
                        intScanIndexObserved = .SurveyScanIndex
                        If intScanIndexObserved < 0 Then intScanIndexObserved = 0

                        If .MRMDaughterMZ > 0 Then
                            blnProcessingMRMPeak = True
                        Else
                            blnProcessingMRMPeak = False
                        End If

                        With .SICStats

                            .SICPotentialAreaStatsForPeak = udtSICPotentialAreaStatsForPeak
                            .Peak = udtSICPeak

                            .ScanTypeForPeakIndices = udtSICDetails.SICScanType
                            If blnProcessingMRMPeak Then
                                If .ScanTypeForPeakIndices <> eScanTypeConstants.FragScan Then
                                    ' ScanType is not FragScan; this is unexpected
                                    LogErrors("StorePeakInParentIon", "Programming error: udtSICDetails.SICScanType is not FragScan even though we're processing an MRM peak", Nothing, True, True, eMasicErrorCodes.FindSICPeaksError)
                                    .ScanTypeForPeakIndices = eScanTypeConstants.FragScan
                                End If
                            End If

                            If blnProcessingMRMPeak Then
                                .Peak.IndexObserved = 0
                            Else
                                ' Record the index (of data in .SICData) that the parent ion mass was first observed
                                ' This is not necessarily the same as udtSICPeak.IndexObserved, so we need to search for it here

                                ' Search for intScanIndexObserved in udtSICDetails.SICScanIndices()
                                .Peak.IndexObserved = -1
                                For intDataIndex = 0 To udtSICDetails.SICDataCount - 1
                                    If udtSICDetails.SICScanIndices(intDataIndex) = intScanIndexObserved Then
                                        .Peak.IndexObserved = intDataIndex
                                        Exit For
                                    End If
                                Next intDataIndex

                                If .Peak.IndexObserved = -1 Then
                                    ' Match wasn't found; this is unexpected
                                    LogErrors("StorePeakInParentIon", "Programming error: survey scan index not found in udtSICDetails.SICScanIndices", Nothing, True, True, eMasicErrorCodes.FindSICPeaksError)
                                    .Peak.IndexObserved = 0
                                End If
                            End If

                            If udtScanList.FragScanCount > 0 AndAlso udtScanList.ParentIons(intParentIonIndex).FragScanIndices(0) < udtScanList.FragScanCount Then
                                ' Record the fragmentation scan number
                                intFragScanNumber = udtScanList.FragScans(udtScanList.ParentIons(intParentIonIndex).FragScanIndices(0)).ScanNumber
                            Else
                                ' Use the parent scan number as the fragmentation scan number
                                ' This is OK based on how mMASICPeakFinder.ComputeParentIonIntensity() uses intFragScanNumber
                                intFragScanNumber = udtScanList.SurveyScans(udtScanList.ParentIons(intParentIonIndex).SurveyScanIndex).ScanNumber
                            End If

                            If blnProcessingMRMPeak Then
                                udtSICPeak.ParentIonIntensity = 0
                            Else
                                ' Determine the value for .ParentIonIntensity
                                blnSuccess = mMASICPeakFinder.ComputeParentIonIntensity(udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData, .Peak, intFragScanNumber)
                            End If

                            If blnPeakIsValid Then
                                ' Record the survey scan indices of the peak max, start, and end
                                ' Note that .ScanTypeForPeakIndices was set earlier in this function
                                .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
                                .PeakScanIndexStart = udtSICDetails.SICScanIndices(.Peak.IndexBaseLeft)
                                .PeakScanIndexEnd = udtSICDetails.SICScanIndices(.Peak.IndexBaseRight)
                            Else
                                ' No peak found
                                .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
                                .PeakScanIndexStart = .PeakScanIndexMax
                                .PeakScanIndexEnd = .PeakScanIndexMax

                                With .Peak
                                    .MaxIntensityValue = udtSICDetails.SICData(.IndexMax)
                                    .IndexBaseLeft = .IndexMax
                                    .IndexBaseRight = .IndexMax
                                    .FWHMScanWidth = 1
                                    ' Assign the intensity of the peak at the observed maximum to the area
                                    .Area = .MaxIntensityValue

                                    .SignalToNoiseRatio = MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(.MaxIntensityValue, .BaselineNoiseStats.NoiseLevel)
                                End With
                            End If
                        End With
                    End With

                    ' Update .OptimalPeakApexScanNumber
                    ' Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
                    With .ParentIons(intParentIonIndex)
                        If blnProcessingMRMPeak Then
                            .OptimalPeakApexScanNumber = udtScanList.FragScans(udtSICDetails.SICScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
                        Else
                            .OptimalPeakApexScanNumber = udtScanList.SurveyScans(udtSICDetails.SICScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
                        End If
                    End With

                End If

            End With

            blnSuccess = True

        Catch ex As Exception

            LogErrors("StorePeakInParentIon", "Error finding SIC peaks and their areas", ex, True, False, eMasicErrorCodes.FindSICPeaksError)
            blnSuccess = False

        End Try

        Return blnSuccess

    End Function

    Private Function AggregateIonsInRange(
      objSpectraCache As clsSpectraCache,
      ByRef udtScanInfo() As udtScanInfoType,
      intSpectrumIndex As Integer,
      dblSearchMZ As Double,
      dblSearchToleranceHalfWidth As Double,
      ByRef intIonMatchCount As Integer,
      ByRef dblClosestMZ As Double,
      blnReturnMax As Boolean) As Single


        Dim intPoolIndex As Integer
        Dim sngIonSumOrMax As Single

        Try
            intIonMatchCount = 0
            sngIonSumOrMax = 0

            If Not objSpectraCache.ValidateSpectrumInPool(udtScanInfo(intSpectrumIndex).ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Else
                sngIonSumOrMax = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex), dblSearchMZ, dblSearchToleranceHalfWidth, intIonMatchCount, dblClosestMZ, blnReturnMax)
            End If
        Catch ex As Exception
            LogErrors("AggregateIonsInRange_SpectraCache", "Error in AggregateIonsInRange", ex, True, False)
            intIonMatchCount = 0
        End Try

        Return sngIonSumOrMax

    End Function

    Private Function AggregateIonsInRange(
      objMSSpectrum As clsMSSpectrum,
      dblSearchMZ As Double,
      dblSearchToleranceHalfWidth As Double,
      ByRef intIonMatchCount As Integer,
      ByRef dblClosestMZ As Double,
      blnReturnMax As Boolean) As Single

        ' Returns the sum of the data within the search mass tolerance (if blnReturnMax = False)
        ' Returns the maximum of the data within the search mass tolerance (if blnReturnMax = True)
        ' Returns intIonMatchCount = 0 if no matches
        '
        ' Note that this function performs a recursive search of objMSSpectrum.IonsMZ; it is therefore very efficient regardless
        '  of the number of data points in the spectrum
        ' For sparse spectra, you can alternatively use FindMaxValueInMZRange

        Dim intIonIndex As Integer
        Dim intIndexFirst, intIndexLast As Integer
        Dim sngIonSumOrMax As Single
        Dim dblTestDifference As Double
        Dim dblSmallestDifference As Double

        Try

            sngIonSumOrMax = 0
            intIonMatchCount = 0
            dblClosestMZ = 0
            dblSmallestDifference = Double.MaxValue

            If Not objMSSpectrum.IonsMZ Is Nothing AndAlso objMSSpectrum.IonCount > 0 Then
                If SumIonsFindValueInRange(objMSSpectrum.IonsMZ, objMSSpectrum.IonCount, dblSearchMZ, dblSearchToleranceHalfWidth, intIndexFirst, intIndexLast) Then
                    For intIonIndex = intIndexFirst To intIndexLast
                        If blnReturnMax Then
                            ' Return max
                            If objMSSpectrum.IonsIntensity(intIonIndex) > sngIonSumOrMax Then
                                sngIonSumOrMax = objMSSpectrum.IonsIntensity(intIonIndex)
                            End If
                        Else
                            ' Return sum
                            sngIonSumOrMax += objMSSpectrum.IonsIntensity(intIonIndex)
                        End If

                        dblTestDifference = Math.Abs(objMSSpectrum.IonsMZ(intIonIndex) - dblSearchMZ)
                        If dblTestDifference < dblSmallestDifference Then
                            dblSmallestDifference = dblTestDifference
                            dblClosestMZ = objMSSpectrum.IonsMZ(intIonIndex)
                        End If
                    Next intIonIndex
                    intIonMatchCount = intIndexLast - intIndexFirst + 1
                End If
            End If

        Catch ex As Exception
            intIonMatchCount = 0
        End Try

        Return sngIonSumOrMax

    End Function

    Private Function SumIonsFindValueInRange(
      ByRef DataDouble() As Double,
      intDataCount As Integer,
      dblSearchValue As Double,
      dblToleranceHalfWidth As Double,
      Optional ByRef intMatchIndexStart As Integer = 0,
      Optional ByRef intMatchIndexEnd As Integer = 0) As Boolean

        ' Searches DataDouble for dblSearchValue with a tolerance of +/-dblToleranceHalfWidth
        ' Returns True if a match is found; in addition, populates intMatchIndexStart and intMatchIndexEnd
        ' Otherwise, returns false

        Dim blnMatchFound As Boolean

        intMatchIndexStart = 0
        intMatchIndexEnd = intDataCount - 1

        If intDataCount = 0 Then
            intMatchIndexEnd = -1
        ElseIf intDataCount = 1 Then
            If Math.Abs(dblSearchValue - DataDouble(0)) > dblToleranceHalfWidth Then
                ' Only one data point, and it is not within tolerance
                intMatchIndexEnd = -1
            End If
        Else
            SumIonsBinarySearchRangeDbl(DataDouble, dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        End If

        If intMatchIndexStart > intMatchIndexEnd Then
            intMatchIndexStart = -1
            intMatchIndexEnd = -1
            blnMatchFound = False
        Else
            blnMatchFound = True
        End If

        Return blnMatchFound
    End Function

    Private Sub SumIonsBinarySearchRangeDbl(
      ByRef DataDouble() As Double,
      dblSearchValue As Double,
      dblToleranceHalfWidth As Double,
      ByRef intMatchIndexStart As Integer,
      ByRef intMatchIndexEnd As Integer)

        ' Recursive search function

        Dim intIndexMidpoint As Integer
        Dim blnLeftDone As Boolean
        Dim blnRightDone As Boolean
        Dim intLeftIndex As Integer
        Dim intRightIndex As Integer

        intIndexMidpoint = (intMatchIndexStart + intMatchIndexEnd) \ 2
        If intIndexMidpoint = intMatchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(dblSearchValue - DataDouble(intMatchIndexStart)) > dblToleranceHalfWidth Then intMatchIndexStart = intMatchIndexEnd
            If Math.Abs(dblSearchValue - DataDouble(intMatchIndexEnd)) > dblToleranceHalfWidth Then intMatchIndexEnd = intIndexMidpoint
            Exit Sub
        End If

        If DataDouble(intIndexMidpoint) > dblSearchValue + dblToleranceHalfWidth Then
            ' Out of range on the right
            intMatchIndexEnd = intIndexMidpoint
            SumIonsBinarySearchRangeDbl(DataDouble, dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        ElseIf DataDouble(intIndexMidpoint) < dblSearchValue - dblToleranceHalfWidth Then
            ' Out of range on the left
            intMatchIndexStart = intIndexMidpoint
            SumIonsBinarySearchRangeDbl(DataDouble, dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        Else
            ' Inside range; figure out the borders
            intLeftIndex = intIndexMidpoint
            Do
                intLeftIndex = intLeftIndex - 1
                If intLeftIndex < intMatchIndexStart Then
                    blnLeftDone = True
                Else
                    If Math.Abs(dblSearchValue - DataDouble(intLeftIndex)) > dblToleranceHalfWidth Then blnLeftDone = True
                End If
            Loop While Not blnLeftDone
            intRightIndex = intIndexMidpoint

            Do
                intRightIndex = intRightIndex + 1
                If intRightIndex > intMatchIndexEnd Then
                    blnRightDone = True
                Else
                    If Math.Abs(dblSearchValue - DataDouble(intRightIndex)) > dblToleranceHalfWidth Then blnRightDone = True
                End If
            Loop While Not blnRightDone

            intMatchIndexStart = intLeftIndex + 1
            intMatchIndexEnd = intRightIndex - 1
        End If

    End Sub

    Private Sub TestScanConversions(ByRef udtScanList As udtScanListType)

        Dim intScanNumber As Integer
        Dim sngRelativeTime As Single
        Dim sngScanTime As Single

        Dim sngResult As Single

        Try
            ' Convert absolute values
            intScanNumber = 500         ' Scan 500
            sngRelativeTime = 0.5       ' Relative scan 0.5
            sngScanTime = 30            ' The scan at 30 minutes

            ' Find the scan number corresponding to each of these values
            sngResult = ScanOrAcqTimeToAbsolute(udtScanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, False)
            sngResult = ScanOrAcqTimeToAbsolute(udtScanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, False)
            sngResult = ScanOrAcqTimeToAbsolute(udtScanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, False)


            ' Convert ranges
            intScanNumber = 50          ' 50 scans wide
            sngRelativeTime = 0.1       ' 10% of the run
            sngScanTime = 5             ' 5 minutes

            ' Convert each of these ranges to a scan time range in minutes
            sngResult = ScanOrAcqTimeToAbsolute(udtScanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, True)
            sngResult = ScanOrAcqTimeToAbsolute(udtScanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, True)
            sngResult = ScanOrAcqTimeToAbsolute(udtScanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, True)



            ' Convert absolute values
            intScanNumber = 500         ' Scan 500
            sngRelativeTime = 0.5       ' Relative scan 0.5
            sngScanTime = 30            ' The scan at 30 minutes

            ' Find the scan number corresponding to each of these values
            sngResult = ScanOrAcqTimeToScanTime(udtScanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, False)
            sngResult = ScanOrAcqTimeToScanTime(udtScanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, False)
            sngResult = ScanOrAcqTimeToScanTime(udtScanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, False)


            ' Convert ranges
            intScanNumber = 50          ' 50 scans wide
            sngRelativeTime = 0.1       ' 10% of the run
            sngScanTime = 5             ' 5 minutes

            ' Convert each of these ranges to a scan time range in minutes
            sngResult = ScanOrAcqTimeToScanTime(udtScanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, True)
            sngResult = ScanOrAcqTimeToScanTime(udtScanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, True)
            sngResult = ScanOrAcqTimeToScanTime(udtScanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, True)


        Catch ex As Exception
            Console.WriteLine("Error caught: " & ex.Message)
        End Try

    End Sub

    Private Function TryGetExtendedHeaderInfoValue(keyName As String, <Out()> ByRef headerIndex As Integer) As Boolean

        Dim query = (From item In mExtendedHeaderInfo Where item.Key = keyName Select item.Value).ToList()
        headerIndex = 0

        If query.Count = 0 Then
            Return False
        End If

        headerIndex = query.First()
        Return True

    End Function

    Private Function UpdateDatasetFileStats(
      ByRef udtDatasetFileInfo As DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType,
      ByRef ioFileInfo As FileInfo,
      intDatasetID As Integer) As Boolean

        Try
            If Not ioFileInfo.Exists Then Return False

            ' Record the file size and Dataset ID
            With udtDatasetFileInfo
                .FileSystemCreationTime = ioFileInfo.CreationTime
                .FileSystemModificationTime = ioFileInfo.LastWriteTime

                .AcqTimeStart = .FileSystemModificationTime
                .AcqTimeEnd = .FileSystemModificationTime

                .DatasetID = intDatasetID
                .DatasetName = Path.GetFileNameWithoutExtension(ioFileInfo.Name)
                .FileExtension = ioFileInfo.Extension
                .FileSizeBytes = ioFileInfo.Length

                .ScanCount = 0
            End With

        Catch ex As Exception
            Return False
        End Try

        Return True

    End Function

    Private Function UpdateDatasetFileStats(
      ByRef udtDatasetFileInfo As DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType,
      ByRef ioFileInfo As FileInfo,
      intDatasetID As Integer,
      ByRef objXcaliburAccessor As XRawFileIO) As Boolean

        Dim scanInfo = New clsScanInfo(0)

        Dim intScanEnd As Integer
        Dim blnSuccess As Boolean

        ' Read the file info from the file system
        blnSuccess = UpdateDatasetFileStats(udtDatasetFileInfo, ioFileInfo, intDatasetID)

        If Not blnSuccess Then Return False

        ' Read the file info using the Xcalibur Accessor
        Try
            udtDatasetFileInfo.AcqTimeStart = objXcaliburAccessor.FileInfo.CreationDate
        Catch ex As Exception
            ' Read error
            blnSuccess = False
        End Try

        If blnSuccess Then
            Try
                ' Look up the end scan time then compute .AcqTimeEnd
                intScanEnd = objXcaliburAccessor.FileInfo.ScanEnd
                objXcaliburAccessor.GetScanInfo(intScanEnd, scanInfo)

                With udtDatasetFileInfo
                    .AcqTimeEnd = .AcqTimeStart.AddMinutes(scanInfo.RetentionTime)
                    .ScanCount = objXcaliburAccessor.GetNumScans()
                End With

            Catch ex As Exception
                ' Error; use default values
                With udtDatasetFileInfo
                    .AcqTimeEnd = .AcqTimeStart
                    .ScanCount = 0
                End With
            End Try
        End If

        Return blnSuccess

    End Function

    Private Sub UpdateMZIntensityFilterIgnoreRange()
        ' Look at the m/z values in mReporterIonInfo to determine the minimum and maximum m/z values
        ' Update mMZIntensityFilterIgnoreRangeStart and mMZIntensityFilterIgnoreRangeEnd to be
        '  2x .MZToleranceDa away from the minimum and maximum

        Dim intIndex As Integer
        Dim dblValue As Double

        If mReporterIonStatsEnabled AndAlso mReporterIonCount > 0 Then
            mMZIntensityFilterIgnoreRangeStart = mReporterIonInfo(0).MZ - mReporterIonInfo(0).MZToleranceDa * 2
            mMZIntensityFilterIgnoreRangeEnd = mReporterIonInfo(0).MZ + mReporterIonInfo(0).MZToleranceDa * 2

            For intIndex = 1 To mReporterIonCount - 1
                dblValue = mReporterIonInfo(intIndex).MZ - mReporterIonInfo(intIndex).MZToleranceDa * 2
                If dblValue < mMZIntensityFilterIgnoreRangeStart Then mMZIntensityFilterIgnoreRangeStart = dblValue

                dblValue = mReporterIonInfo(intIndex).MZ + mReporterIonInfo(intIndex).MZToleranceDa * 2
                If dblValue > mMZIntensityFilterIgnoreRangeEnd Then mMZIntensityFilterIgnoreRangeEnd = dblValue

            Next intIndex
        Else
            mMZIntensityFilterIgnoreRangeStart = 0
            mMZIntensityFilterIgnoreRangeEnd = 0
        End If

    End Sub

    Private Sub UpdateOverallProgress()
        UpdateOverallProgress(MyBase.mProgressStepDescription)
    End Sub

    Private Sub UpdateOverallProgress(objSpectraCache As clsSpectraCache)
        UpdateOverallProgress()
        mProcessingStats.CacheEventCount = objSpectraCache.CacheEventCount
        mProcessingStats.UnCacheEventCount = objSpectraCache.UnCacheEventCount
    End Sub

    Private Sub UpdateOverallProgress(strProgressStepDescription As String)

        ' Update the processing progress, storing the value in mProgressPercentComplete

        'NewTask = 0
        'ReadDataFile = 1
        'SaveBPI = 2
        'CreateSICsAndFindPeaks = 3
        'FindSimilarParentIons = 4
        'SaveExtendedScanStatsFiles = 5
        'SaveSICStatsFlatFile = 6
        'CloseOpenFileHandles = 7
        'UpdateXMLFileWithNewOptimalPeakApexValues = 8
        'Cancelled = 99
        'Complete = 100

        Dim sngWeightingFactors() As Single

        If mSkipMSMSProcessing Then
            ' Step                              0   1     2     3  4   5      6      7      8     
            sngWeightingFactors = New Single() {0, 0.97, 0.002, 0, 0, 0.001, 0.025, 0.001, 0.001}            ' The sum of these factors should be 1.00
        Else
            ' Step                              0   1      2      3     4     5      6      7      8     
            sngWeightingFactors = New Single() {0, 0.194, 0.003, 0.65, 0.09, 0.001, 0.006, 0.001, 0.055}     ' The sum of these factors should be 1.00
        End If

        Dim intCurrentStep, intIndex As Integer
        Dim sngOverallPctCompleted As Single

        Try
            intCurrentStep = mProcessingStep
            If intCurrentStep >= sngWeightingFactors.Length Then intCurrentStep = sngWeightingFactors.Length - 1

            sngOverallPctCompleted = 0
            For intIndex = 0 To intCurrentStep - 1
                sngOverallPctCompleted += sngWeightingFactors(intIndex) * 100
            Next intIndex

            sngOverallPctCompleted += sngWeightingFactors(intCurrentStep) * mSubtaskProcessingStepPct

            mProgressPercentComplete = sngOverallPctCompleted

        Catch ex As Exception
            Debug.Assert(False, "Bug in UpdateOverallProgress")
        End Try

        MyBase.UpdateProgress(strProgressStepDescription, mProgressPercentComplete)
    End Sub

    Private Sub UpdatePeakMemoryUsage()

        Dim sngMemoryUsageMB As Single

        sngMemoryUsageMB = GetProcessMemoryUsageMB()
        If sngMemoryUsageMB > mProcessingStats.PeakMemoryUsageMB Then
            mProcessingStats.PeakMemoryUsageMB = sngMemoryUsageMB
        End If

    End Sub

    Private Sub UpdateProcessingStep(eNewProcessingStep As eProcessingStepConstants, Optional blnForceStatusFileUpdate As Boolean = False)

        mProcessingStep = eNewProcessingStep
        UpdateStatusFile(blnForceStatusFileUpdate)

    End Sub

    Private Sub UpdateStatusFile(Optional blnForceUpdate As Boolean = False)

        Dim strPath As String
        Dim strTempPath As String
        Dim objXMLOut As Xml.XmlTextWriter

        Static LastFileWriteTime As DateTime = DateTime.UtcNow

        If blnForceUpdate OrElse DateTime.UtcNow.Subtract(LastFileWriteTime).TotalSeconds >= MINIMUM_STATUS_FILE_UPDATE_INTERVAL_SECONDS Then
            LastFileWriteTime = DateTime.UtcNow

            Try
                strTempPath = Path.Combine(GetAppFolderPath(), "Temp_" & mMASICStatusFilename)
                strPath = Path.Combine(GetAppFolderPath(), mMASICStatusFilename)

                objXMLOut = New Xml.XmlTextWriter(strTempPath, Text.Encoding.UTF8)
                objXMLOut.Formatting = Xml.Formatting.Indented
                objXMLOut.Indentation = 2

                objXMLOut.WriteStartDocument(True)
                objXMLOut.WriteComment("MASIC processing status")

                'Write the beginning of the "Root" element.
                objXMLOut.WriteStartElement("Root")

                objXMLOut.WriteStartElement("General")
                objXMLOut.WriteElementString("LastUpdate", DateTime.Now.ToString)
                objXMLOut.WriteElementString("ProcessingStep", mProcessingStep.ToString)
                objXMLOut.WriteElementString("Progress", Math.Round(mProgressPercentComplete, 2).ToString)
                objXMLOut.WriteElementString("Error", GetErrorMessage())
                objXMLOut.WriteEndElement()

                objXMLOut.WriteStartElement("Statistics")
                objXMLOut.WriteElementString("FreeMemoryMB", Math.Round(GetFreeMemoryMB, 1).ToString)
                objXMLOut.WriteElementString("MemoryUsageMB", Math.Round(GetProcessMemoryUsageMB, 1).ToString)
                objXMLOut.WriteElementString("PeakMemoryUsageMB", Math.Round(mProcessingStats.PeakMemoryUsageMB, 1).ToString)

                With mProcessingStats
                    objXMLOut.WriteElementString("CacheEventCount", .CacheEventCount.ToString)
                    objXMLOut.WriteElementString("UnCacheEventCount", .UnCacheEventCount.ToString)
                End With

                objXMLOut.WriteElementString("ProcessingTimeSec", Math.Round(GetTotalProcessingTimeSec(), 2).ToString)
                objXMLOut.WriteEndElement()

                objXMLOut.WriteEndElement()  'End the "Root" element.
                objXMLOut.WriteEndDocument() 'End the document

                objXMLOut.Close()

                GC.Collect()
                GC.WaitForPendingFinalizers()
                Application.DoEvents()

                'Copy the temporary file to the real one
                File.Copy(strTempPath, strPath, True)
                File.Delete(strTempPath)

            Catch ex As Exception
                ' Ignore any errors
            End Try

        End If

    End Sub

    Private Sub ValidateCustomSICList()
        Dim intIndex As Integer
        Dim intCountBetweenZeroAndOne As Integer
        Dim intCountOverOne As Integer

        If Not mCustomSICList.CustomMZSearchValues Is Nothing AndAlso
           mCustomSICList.CustomMZSearchValues.Length > 0 Then
            ' Check whether all of the values are between 0 and 1
            ' If they are, then auto-switch .ScanToleranceType to "Relative"

            intCountBetweenZeroAndOne = 0
            intCountOverOne = 0

            For intIndex = 0 To mCustomSICList.CustomMZSearchValues.Length - 1
                If mCustomSICList.CustomMZSearchValues(intIndex).ScanOrAcqTimeCenter > 1 Then
                    intCountOverOne += 1
                Else
                    intCountBetweenZeroAndOne += 1
                End If
            Next intIndex

            If intCountOverOne = 0 And intCountBetweenZeroAndOne > 0 Then
                If mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                    ' No values were greater than 1 but at least one value is between 0 and 1
                    ' Change the ScanToleranceType mode from Absolute to Relative
                    mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Relative
                End If
            End If

            If intCountOverOne > 0 And intCountBetweenZeroAndOne = 0 Then
                If mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Relative Then
                    ' The ScanOrAcqTimeCenter values cannot be relative
                    ' Change the ScanToleranceType mode from Relative to Absolute
                    mCustomSICList.ScanToleranceType = eCustomSICScanTypeConstants.Absolute
                End If
            End If
        End If

    End Sub
    Private Sub ValidateMasterScanOrderSorting(ByRef udtScanList As udtScanListType)
        ' Validate that .MasterScanOrder() really is sorted by scan number
        ' Cannot use an IComparer because .MasterScanOrder points into other arrays

        Dim intMasterScanOrderIndices() As Integer
        Dim udtMasterScanOrderListCopy() As udtScanOrderPointerType
        Dim sngMasterScanTimeListCopy() As Single

        Dim intIndex As Integer

        Dim blnListWasSorted As Boolean

        With udtScanList

            ReDim intMasterScanOrderIndices(.MasterScanOrderCount - 1)

            For intIndex = 0 To .MasterScanOrderCount - 1
                intMasterScanOrderIndices(intIndex) = intIndex
            Next intIndex

            ' Sort .MasterScanNumList ascending, sorting the scan order indices array in parallel
            Array.Sort(.MasterScanNumList, intMasterScanOrderIndices)

            ' Check whether we need to re-populate the lists
            blnListWasSorted = False
            For intIndex = 1 To .MasterScanOrderCount - 1
                If intMasterScanOrderIndices(intIndex) < intMasterScanOrderIndices(intIndex - 1) Then
                    blnListWasSorted = True
                End If
            Next intIndex

            If blnListWasSorted Then
                ' Reorder .MasterScanOrder
                ReDim udtMasterScanOrderListCopy(.MasterScanOrder.Length - 1)
                ReDim sngMasterScanTimeListCopy(.MasterScanOrder.Length - 1)

                Array.Copy(.MasterScanOrder, udtMasterScanOrderListCopy, .MasterScanOrderCount)
                Array.Copy(.MasterScanTimeList, sngMasterScanTimeListCopy, .MasterScanOrderCount)

                For intIndex = 0 To .MasterScanOrderCount - 1
                    .MasterScanOrder(intIndex) = udtMasterScanOrderListCopy(intMasterScanOrderIndices(intIndex))
                    .MasterScanTimeList(intIndex) = sngMasterScanTimeListCopy(intMasterScanOrderIndices(intIndex))
                Next intIndex
            End If


        End With
    End Sub

    Private Sub ValidateSICOptions(ByRef udtSICOptions As udtSICOptionsType)

        If udtSICOptions.CompressToleranceDivisorForDa < 1 Then
            udtSICOptions.CompressToleranceDivisorForDa = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA
        End If

        If udtSICOptions.CompressToleranceDivisorForPPM < 1 Then
            udtSICOptions.CompressToleranceDivisorForPPM = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM
        End If

    End Sub

    Private Function ValidateXRawAccessor() As Boolean

        Static blnValidated As Boolean
        Static blnValidationSaved As Boolean

        If blnValidated Then
            Return blnValidationSaved
        End If

        Try
            Dim objXRawAccess As New XRawFileIO

            blnValidationSaved = objXRawAccess.CheckFunctionality()
        Catch ex As Exception
            blnValidationSaved = False
        End Try

        Return blnValidationSaved

    End Function

    Public Sub TestValueToString()

        Const intDigitsOfPrecision As Integer = 5

        Console.WriteLine(StringUtilities.ValueToString(1.2301, 3, 100000))
        Console.WriteLine(StringUtilities.ValueToString(1.2, 3, 100000))
        Console.WriteLine(StringUtilities.ValueToString(1.003, 3, 100000))
        Console.WriteLine(StringUtilities.ValueToString(999.995, 9, 100000))
        Console.WriteLine(StringUtilities.ValueToString(999.995, 8, 100000))
        Console.WriteLine(StringUtilities.ValueToString(999.995, 7, 100000))
        Console.WriteLine(StringUtilities.ValueToString(999.995, 6, 100000))
        Console.WriteLine(StringUtilities.ValueToString(999.995, 5, 100000))
        Console.WriteLine(StringUtilities.ValueToString(999.995, 4, 100000))
        Console.WriteLine(StringUtilities.ValueToString(1000.995, 3, 100000))
        Console.WriteLine(StringUtilities.ValueToString(1000.995, 2, 100000))
        Console.WriteLine(StringUtilities.ValueToString(1.003, 5))

        Console.WriteLine(StringUtilities.ValueToString(1.23123, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(12.3123, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(123.123, intDigitsOfPrecision))

        Console.WriteLine(StringUtilities.ValueToString(1231.23, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(12312.3, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(123123, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(1231234, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(12312345, intDigitsOfPrecision))
        Console.WriteLine(StringUtilities.ValueToString(123123456, intDigitsOfPrecision))

    End Sub

	Private Sub WriteDecon2LSIsosFileHeaders(srOutFile As StreamWriter)
		srOutFile.WriteLine("scan_num,charge,abundance,mz,fit,average_mw,monoisotopic_mw,mostabundant_mw,fwhm,signal_noise,mono_abundance,mono_plus2_abundance")
	End Sub

	Private Sub WriteDecon2LSIsosFileEntry(
	  ByRef srIsosOutFile As StreamWriter, 
	  intScanNumber As Integer, 
	  intCharge As Integer, 
	  sngIntensity As Single, 
	  dblIonMZ As Double, 
	  sngFit As Single, 
	  dblAverageMass As Double, 
	  dblMonoisotopicMass As Double, 
	  dblMostAbundanctMass As Double, 
	  sngFWHM As Single, 
	  sngSignalToNoise As Single, 
	  sngMonoisotopicAbu As Single, 
	  sngMonoPlus2Abu As Single)

		Dim strLineOut As String

        strLineOut = intScanNumber & "," &
         intCharge & "," &
         sngIntensity & "," &
         dblIonMZ.ToString("0.00000") & "," &
         sngFit & "," &
         dblAverageMass.ToString("0.00000") & "," &
         dblMonoisotopicMass.ToString("0.00000") & "," &
         dblMostAbundanctMass.ToString("0.00000") & "," &
         sngFWHM & "," &
         sngSignalToNoise & "," &
         sngMonoisotopicAbu & "," &
         sngMonoPlus2Abu

		srIsosOutFile.WriteLine(strLineOut)

	End Sub

	Private Sub WriteDecon2LSScanFileHeaders(srOutFile As StreamWriter)
		srOutFile.WriteLine("scan_num,scan_time,type,bpi,bpi_mz,tic,num_peaks,num_deisotoped")

		' Old Headers:      "scan_num,time,type,num_isotopic_signatures,num_peaks,tic,bpi_mz,bpi,time_domain_signal,peak_intensity_threshold,peptide_intensity_threshold")

	End Sub

    Private Sub WriteDecon2LSScanFileEntry(
      ByRef srScanInfoOutFile As StreamWriter,
      ByRef udtScan As udtScanInfoType,
      objSpectraCache As clsSpectraCache)

        Dim intPoolIndex As Integer
        Dim intNumPeaks As Integer

        Dim intScanNumber As Integer
        Dim intMSLevel As Integer
        Dim intNumIsotopicSignatures As Integer

        If objSpectraCache Is Nothing Then
            intNumPeaks = 0
        Else
            If Not objSpectraCache.ValidateSpectrumInPool(udtScan.ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                Exit Sub
            End If
            intNumPeaks = objSpectraCache.SpectraPool(intPoolIndex).IonCount
        End If

        With udtScan
            intScanNumber = udtScan.ScanNumber

            intMSLevel = .FragScanInfo.MSLevel
            If intMSLevel < 1 Then intMSLevel = 1

        End With

        intNumIsotopicSignatures = 0

        WriteDecon2LSScanFileEntry(srScanInfoOutFile, udtScan, intScanNumber, intMSLevel, intNumPeaks, intNumIsotopicSignatures)

    End Sub

	Private Sub WriteDecon2LSScanFileEntry(
	  ByRef srScanInfoOutFile As StreamWriter, 
	  udtScan As udtScanInfoType, 
	  intScanNumber As Integer, 
	  intMSLevel As Integer, 
	  intNumPeaks As Integer, 
	  intNumIsotopicSignatures As Integer)

		Dim strLineOut As String

		With udtScan
            strLineOut = intScanNumber.ToString & "," &
             .ScanTime.ToString("0.0000") & "," &
             intMSLevel & "," &
             .BasePeakIonIntensity & "," &
             .BasePeakIonMZ.ToString("0.00000") & "," &
             .TotalIonIntensity.ToString & "," &
             intNumPeaks & "," &
             intNumIsotopicSignatures
		End With

		srScanInfoOutFile.WriteLine(strLineOut)

	End Sub


    Private Function XMLOutputFileFinalize(
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache) As Boolean


        Dim objXMLOut As Xml.XmlTextWriter
        objXMLOut = udtOutputFileHandles.XMLFileForSICs
        If objXMLOut Is Nothing Then Return False

        Try
            objXMLOut.WriteStartElement("ProcessingStats")
            With objSpectraCache
                objXMLOut.WriteElementString("CacheEventCount", .CacheEventCount.ToString)
                objXMLOut.WriteElementString("UnCacheEventCount", .UnCacheEventCount.ToString)
            End With

            With mProcessingStats
                objXMLOut.WriteElementString("PeakMemoryUsageMB", Math.Round(.PeakMemoryUsageMB, 2).ToString)
                objXMLOut.WriteElementString("TotalProcessingTimeSeconds", Math.Round(GetTotalProcessingTimeSec() - .TotalProcessingTimeAtStart, 2).ToString)
            End With
            objXMLOut.WriteEndElement()

            If udtScanList.ProcessingIncomplete Then
                objXMLOut.WriteElementString("ProcessingComplete", "False")
            Else
                objXMLOut.WriteElementString("ProcessingComplete", "True")
            End If

            objXMLOut.WriteEndElement()     ' Close out the <SICData> element
            objXMLOut.WriteEndDocument()
            objXMLOut.Close()

        Catch ex As Exception
            LogErrors("XMLOutputFileFinalize", "Error finalizing the XML output file", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Private Function XMLOutputFileInitialize(
      strInputFilePathFull As String,
      strOutputFolderPath As String,
      ByRef udtOutputFileHandles As udtOutputFileHandlesType,
      ByRef udtScanList As udtScanListType,
      objSpectraCache As clsSpectraCache,
      ByRef udtSICOptions As udtSICOptionsType,
      ByRef udtBinningOptions As clsCorrelation.udtBinningOptionsType) As Boolean


        Dim strXMLOutputFilePath As String = String.Empty

        Dim ioFileInfo As FileInfo

        Dim LastModTime As Date
        Dim strLastModTime As String
        Dim strFileSizeBytes As String

        Dim objXMLOut As Xml.XmlTextWriter

        Try

            strXMLOutputFilePath = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.XMLFile)

            udtOutputFileHandles.XMLFileForSICs = New Xml.XmlTextWriter(strXMLOutputFilePath, Text.Encoding.UTF8)
            objXMLOut = udtOutputFileHandles.XMLFileForSICs

            With objXMLOut
                .Formatting = Xml.Formatting.Indented
                .Indentation = 1
            End With

            objXMLOut.WriteStartDocument(True)
            objXMLOut.WriteStartElement("SICData")

            objXMLOut.WriteStartElement("ProcessingSummary")
            objXMLOut.WriteElementString("DatasetNumber", udtSICOptions.DatasetNumber.ToString)
            objXMLOut.WriteElementString("SourceFilePath", strInputFilePathFull)

            Try
                ioFileInfo = New FileInfo(strInputFilePathFull)
                LastModTime = ioFileInfo.LastWriteTime()
                strLastModTime = LastModTime.ToShortDateString & " " & LastModTime.ToShortTimeString
                strFileSizeBytes = ioFileInfo.Length.ToString
            Catch ex As Exception
                strLastModTime = String.Empty
                strFileSizeBytes = "0"
            End Try

            objXMLOut.WriteElementString("SourceFileDateTime", strLastModTime)
            objXMLOut.WriteElementString("SourceFileSizeBytes", strFileSizeBytes)

            objXMLOut.WriteElementString("MASICProcessingDate", DateTime.Now.ToShortDateString & " " & DateTime.Now.ToLongTimeString)
            objXMLOut.WriteElementString("MASICVersion", MyBase.FileVersion)
            objXMLOut.WriteElementString("MASICPeakFinderDllVersion", mMASICPeakFinder.ProgramVersion)
            objXMLOut.WriteElementString("ScanCountTotal", udtScanList.MasterScanOrderCount.ToString)
            objXMLOut.WriteElementString("SurveyScanCount", udtScanList.SurveyScanCount.ToString)
            objXMLOut.WriteElementString("FragScanCount", udtScanList.FragScanCount.ToString)
            objXMLOut.WriteElementString("SkipMSMSProcessing", mSkipMSMSProcessing.ToString)

            objXMLOut.WriteElementString("ParentIonDecoyMassDa", mParentIonDecoyMassDa.ToString("0.0000"))

            objXMLOut.WriteEndElement()

            objXMLOut.WriteStartElement("MemoryOptions")
            With objSpectraCache

                objXMLOut.WriteElementString("CacheAlwaysDisabled", .DiskCachingAlwaysDisabled.ToString)
                objXMLOut.WriteElementString("CacheSpectraToRetainInMemory", .CacheSpectraToRetainInMemory.ToString)
                objXMLOut.WriteElementString("CacheMinimumFreeMemoryMB", .CacheMinimumFreeMemoryMB.ToString)
                objXMLOut.WriteElementString("CacheMaximumMemoryUsageMB", .CacheMaximumMemoryUsageMB.ToString)

            End With
            objXMLOut.WriteEndElement()


            objXMLOut.WriteStartElement("SICOptions")
            With udtSICOptions
                ' SIC Options

                ' "SICToleranceDa" is a legacy parameter; If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                objXMLOut.WriteElementString("SICToleranceDa", GetParentIonToleranceDa(udtSICOptions, 1000).ToString("0.0000"))

                objXMLOut.WriteElementString("SICTolerance", .SICTolerance.ToString("0.0000"))
                objXMLOut.WriteElementString("SICToleranceIsPPM", .SICToleranceIsPPM.ToString)

                objXMLOut.WriteElementString("RefineReportedParentIonMZ", .RefineReportedParentIonMZ.ToString)

                objXMLOut.WriteElementString("ScanRangeStart", .ScanRangeStart.ToString)
                objXMLOut.WriteElementString("ScanRangeEnd", .ScanRangeEnd.ToString)
                objXMLOut.WriteElementString("RTRangeStart", .RTRangeStart.ToString)
                objXMLOut.WriteElementString("RTRangeEnd", .RTRangeEnd.ToString)

                objXMLOut.WriteElementString("CompressMSSpectraData", .CompressMSSpectraData.ToString)
                objXMLOut.WriteElementString("CompressMSMSSpectraData", .CompressMSMSSpectraData.ToString)

                objXMLOut.WriteElementString("CompressToleranceDivisorForDa", .CompressToleranceDivisorForDa.ToString("0.0"))
                objXMLOut.WriteElementString("CompressToleranceDivisorForPPM", .CompressToleranceDivisorForPPM.ToString("0.0"))

                objXMLOut.WriteElementString("MaxSICPeakWidthMinutesBackward", .MaxSICPeakWidthMinutesBackward.ToString)
                objXMLOut.WriteElementString("MaxSICPeakWidthMinutesForward", .MaxSICPeakWidthMinutesForward.ToString)

                With .SICPeakFinderOptions
                    objXMLOut.WriteElementString("IntensityThresholdFractionMax", .IntensityThresholdFractionMax.ToString)
                    objXMLOut.WriteElementString("IntensityThresholdAbsoluteMinimum", .IntensityThresholdAbsoluteMinimum.ToString)

                    ' Peak Finding Options
                    With .SICBaselineNoiseOptions
                        objXMLOut.WriteElementString("SICNoiseThresholdMode", .BaselineNoiseMode.ToString)
                        objXMLOut.WriteElementString("SICNoiseThresholdIntensity", .BaselineNoiseLevelAbsolute.ToString)
                        objXMLOut.WriteElementString("SICNoiseFractionLowIntensityDataToAverage", .TrimmedMeanFractionLowIntensityDataToAverage.ToString)
                        objXMLOut.WriteElementString("SICNoiseMinimumSignalToNoiseRatio", .MinimumSignalToNoiseRatio.ToString)
                    End With

                    objXMLOut.WriteElementString("MaxDistanceScansNoOverlap", .MaxDistanceScansNoOverlap.ToString)
                    objXMLOut.WriteElementString("MaxAllowedUpwardSpikeFractionMax", .MaxAllowedUpwardSpikeFractionMax.ToString)
                    objXMLOut.WriteElementString("InitialPeakWidthScansScaler", .InitialPeakWidthScansScaler.ToString)
                    objXMLOut.WriteElementString("InitialPeakWidthScansMaximum", .InitialPeakWidthScansMaximum.ToString)

                    objXMLOut.WriteElementString("FindPeaksOnSmoothedData", .FindPeaksOnSmoothedData.ToString)
                    objXMLOut.WriteElementString("SmoothDataRegardlessOfMinimumPeakWidth", .SmoothDataRegardlessOfMinimumPeakWidth.ToString)
                    objXMLOut.WriteElementString("UseButterworthSmooth", .UseButterworthSmooth.ToString)
                    objXMLOut.WriteElementString("ButterworthSamplingFrequency", .ButterworthSamplingFrequency.ToString)
                    objXMLOut.WriteElementString("ButterworthSamplingFrequencyDoubledForSIMData", .ButterworthSamplingFrequencyDoubledForSIMData.ToString)

                    objXMLOut.WriteElementString("UseSavitzkyGolaySmooth", .UseSavitzkyGolaySmooth.ToString)
                    objXMLOut.WriteElementString("SavitzkyGolayFilterOrder", .SavitzkyGolayFilterOrder.ToString)

                    With .MassSpectraNoiseThresholdOptions
                        objXMLOut.WriteElementString("MassSpectraNoiseThresholdMode", .BaselineNoiseMode.ToString)
                        objXMLOut.WriteElementString("MassSpectraNoiseThresholdIntensity", .BaselineNoiseLevelAbsolute.ToString)
                        objXMLOut.WriteElementString("MassSpectraNoiseFractionLowIntensityDataToAverage", .TrimmedMeanFractionLowIntensityDataToAverage.ToString)
                        objXMLOut.WriteElementString("MassSpectraNoiseMinimumSignalToNoiseRatio", .MinimumSignalToNoiseRatio.ToString)
                    End With
                End With

                objXMLOut.WriteElementString("ReplaceSICZeroesWithMinimumPositiveValueFromMSData", .ReplaceSICZeroesWithMinimumPositiveValueFromMSData.ToString)

                objXMLOut.WriteElementString("SaveSmoothedData", .SaveSmoothedData.ToString)

                ' Similarity options
                objXMLOut.WriteElementString("SimilarIonMZToleranceHalfWidth", .SimilarIonMZToleranceHalfWidth.ToString)
                objXMLOut.WriteElementString("SimilarIonToleranceHalfWidthMinutes", .SimilarIonToleranceHalfWidthMinutes.ToString)
                objXMLOut.WriteElementString("SpectrumSimilarityMinimum", .SpectrumSimilarityMinimum.ToString)
            End With
            objXMLOut.WriteEndElement()

            objXMLOut.WriteStartElement("BinningOptions")
            With udtBinningOptions
                objXMLOut.WriteElementString("BinStartX", .StartX.ToString)
                objXMLOut.WriteElementString("BinEndX", .EndX.ToString)
                objXMLOut.WriteElementString("BinSize", .BinSize.ToString)
                objXMLOut.WriteElementString("MaximumBinCount", .MaximumBinCount.ToString)

                objXMLOut.WriteElementString("IntensityPrecisionPercent", .IntensityPrecisionPercent.ToString)
                objXMLOut.WriteElementString("Normalize", .Normalize.ToString)
                objXMLOut.WriteElementString("SumAllIntensitiesForBin", .SumAllIntensitiesForBin.ToString)

            End With
            objXMLOut.WriteEndElement()

            objXMLOut.WriteStartElement("CustomSICValues")
            With mCustomSICList
                objXMLOut.WriteElementString("MZList", .RawTextMZList)
                objXMLOut.WriteElementString("MZToleranceDaList", .RawTextMZToleranceDaList)
                objXMLOut.WriteElementString("ScanCenterList", .RawTextScanOrAcqTimeCenterList)
                objXMLOut.WriteElementString("ScanToleranceList", .RawTextScanOrAcqTimeToleranceList)
                objXMLOut.WriteElementString("ScanTolerance", .ScanOrAcqTimeTolerance.ToString)
                objXMLOut.WriteElementString("ScanType", .ScanToleranceType.ToString)
                objXMLOut.WriteElementString("LimitSearchToCustomMZList", .LimitSearchToCustomMZList.ToString)
            End With
            objXMLOut.WriteEndElement()


        Catch ex As Exception
            LogErrors("XMLOutputFileFinalize", "Error initializing the XML output file" & GetFilePathPrefixChar() & strXMLOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

	Private Function XmlOutputFileUpdateEntries(
      ByRef udtScanList As udtScanListType,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean


        Const PARENT_ION_TAG_START_LCASE = "<parention"     ' Note: this needs to be lowercase
        Const INDEX_ATTRIBUTE_LCASE = "index="              ' Note: this needs to be lowercase

        Const OPTIMAL_PEAK_APEX_TAG_NAME = "OptimalPeakApexScanNumber"
        Const PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME = "PeakApexOverrideParentIonIndex"

		Dim strXMLReadFilePath As String
		Dim strXMLOutputFilePath As String

		Dim strLineIn As String
		Dim strLineInTrimmedAndLower As String
		Dim strWork As String

		Dim intCharIndex As Integer
		Dim intParentIonIndex As Integer
		Dim intParentIonsProcessed As Integer

		strXMLReadFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.XMLFile)

		strXMLOutputFilePath = Path.Combine(strOutputFolderPath, "__temp__MASICOutputFile.xml")

		Try
			' Wait 2 seconds before reopening the file, to make sure the handle is closed
			Threading.Thread.Sleep(2000)

			If Not File.Exists(strXMLReadFilePath) Then
				' XML file not found, exit the function
				Return True
			End If

            Using srInFile = New StreamReader(strXMLReadFilePath),
                  srOutFile = New StreamWriter(strXMLOutputFilePath, False)

                SetSubtaskProcessingStepPct(0, "Updating XML file with optimal peak apex values")

                intParentIonIndex = -1
                intParentIonsProcessed = 0
                Do While Not srInFile.EndOfStream
                    strLineIn = srInFile.ReadLine()
                    If Not strLineIn Is Nothing Then
                        strLineInTrimmedAndLower = strLineIn.Trim.ToLower

                        If strLineInTrimmedAndLower.StartsWith(PARENT_ION_TAG_START_LCASE) Then
                            intCharIndex = strLineInTrimmedAndLower.IndexOf(INDEX_ATTRIBUTE_LCASE, StringComparison.CurrentCultureIgnoreCase)
                            If intCharIndex > 0 Then
                                strWork = strLineInTrimmedAndLower.Substring(intCharIndex + INDEX_ATTRIBUTE_LCASE.Length + 1)
                                intCharIndex = strWork.IndexOf(ControlChars.Quote)
                                If intCharIndex > 0 Then
                                    strWork = strWork.Substring(0, intCharIndex)
                                    If IsNumber(strWork) Then
                                        intParentIonIndex = CInt(strWork)
                                        intParentIonsProcessed += 1

                                        ' Update progress
                                        If udtScanList.ParentIonInfoCount > 1 Then
                                            If intParentIonsProcessed Mod 100 = 0 Then
                                                SetSubtaskProcessingStepPct(CShort(intParentIonsProcessed / (udtScanList.ParentIonInfoCount - 1) * 100))
                                            End If
                                        Else
                                            SetSubtaskProcessingStepPct(0)
                                        End If

                                        If mAbortProcessing Then
                                            udtScanList.ProcessingIncomplete = True
                                            Exit Do
                                        End If

                                    End If
                                End If
                            End If

                            srOutFile.WriteLine(strLineIn)

                        ElseIf strLineInTrimmedAndLower.StartsWith("<" & OPTIMAL_PEAK_APEX_TAG_NAME.ToLower) AndAlso intParentIonIndex >= 0 Then
                            If intParentIonIndex < udtScanList.ParentIonInfoCount Then
                                XmlOutputFileReplaceSetting(srOutFile, strLineIn, OPTIMAL_PEAK_APEX_TAG_NAME, udtScanList.ParentIons(intParentIonIndex).OptimalPeakApexScanNumber)
                            End If
                        ElseIf strLineInTrimmedAndLower.StartsWith("<" & PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME.ToLower) AndAlso intParentIonIndex >= 0 Then
                            If intParentIonIndex < udtScanList.ParentIonInfoCount Then
                                XmlOutputFileReplaceSetting(srOutFile, strLineIn, PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME, udtScanList.ParentIons(intParentIonIndex).PeakApexOverrideParentIonIndex)
                            End If
                        Else
                            srOutFile.WriteLine(strLineIn)
                        End If
                    End If
                Loop

            End Using

            Try
                ' Wait 2 seconds, then delete the original file and rename the temp one to the original one
                Threading.Thread.Sleep(2000)

                If File.Exists(strXMLOutputFilePath) Then
                    If File.Exists(strXMLReadFilePath) Then
                        File.Delete(strXMLReadFilePath)
                        Threading.Thread.Sleep(500)
                    End If

                    File.Move(strXMLOutputFilePath, strXMLReadFilePath)
                End If

            Catch ex As Exception
                LogErrors("XmlOutputFileUpdateEntries", "Error renaming XML output file from temp name to" & GetFilePathPrefixChar() & strXMLReadFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            SetSubtaskProcessingStepPct(100)
            Windows.Forms.Application.DoEvents()

        Catch ex As Exception
            LogErrors("XmlOutputFileUpdateEntries", "Error updating the XML output file" & GetFilePathPrefixChar() & strXMLReadFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

		Return True

	End Function

	Private Sub XmlOutputFileReplaceSetting(
	  srOutFile As StreamWriter, 
	  strLineIn As String, 
	  strXMLElementName As String, 
	  intNewValueToSave As Integer)
	  
		' strXMLElementName should be the properly capitalized element name and should not start with "<"

		Dim strWork As String
		Dim intCharIndex As Integer
		Dim intCurrentValue As Integer

		' Need to add two since strXMLElementName doesn't include "<" at the beginning
		strWork = strLineIn.Trim.ToLower.Substring(strXMLElementName.Length + 2)

		' Look for the "<" after the number
		intCharIndex = strWork.IndexOf("<", StringComparison.Ordinal)
		If intCharIndex > 0 Then
			' Isolate the number
			strWork = strWork.Substring(0, intCharIndex)
			If IsNumber(strWork) Then
				intCurrentValue = CInt(strWork)

				If intNewValueToSave <> intCurrentValue Then
					strLineIn = "  <" & strXMLElementName & ">"
					strLineIn &= intNewValueToSave.ToString
					strLineIn &= "</" & strXMLElementName & ">"

				End If
			End If
		End If

		srOutFile.WriteLine(strLineIn)

	End Sub

	Protected Overrides Sub Finalize()
		If Not mFreeMemoryPerformanceCounter Is Nothing Then
			mFreeMemoryPerformanceCounter.Close()
			mFreeMemoryPerformanceCounter = Nothing
		End If

		MyBase.Finalize()
	End Sub

#Region "PPMToMassConversion"
	Public Shared Function MassToPPM(dblMassToConvert As Double, dblCurrentMZ As Double) As Double
		' Converts dblMassToConvert to ppm, based on the value of dblCurrentMZ

		Return dblMassToConvert * 1000000.0 / dblCurrentMZ
	End Function

	Public Shared Function PPMToMass(dblPPMToConvert As Double, dblCurrentMZ As Double) As Double
		' Converts dblPPMToConvert to a mass value, which is dependent on dblCurrentMZ

		Return dblPPMToConvert / 1000000.0 * dblCurrentMZ
	End Function
#End Region

	Protected Class clsScanInfoScanNumComparer
		Implements IComparer

		Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
			Dim udtScanInfo As udtScanInfoType
			Dim intScanNumber As Integer

			udtScanInfo = DirectCast(x, udtScanInfoType)
			intScanNumber = DirectCast(y, Integer)

			If udtScanInfo.ScanNumber > intScanNumber Then
				Return 1
			ElseIf udtScanInfo.ScanNumber < intScanNumber Then
				Return -1
			Else
				Return 0
			End If
		End Function
	End Class

	Protected Class clsMZBinListComparer
		Implements IComparer

		Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
			Dim udtMZBinListA As udtMZBinListType
			Dim udtMZBinListB As udtMZBinListType

			udtMZBinListA = DirectCast(x, udtMZBinListType)
			udtMZBinListB = DirectCast(y, udtMZBinListType)

			If udtMZBinListA.MZ > udtMZBinListB.MZ Then
				Return 1
			ElseIf udtMZBinListA.MZ < udtMZBinListB.MZ Then
				Return -1
			Else
				Return 0
			End If
		End Function
	End Class

	Protected Class clsReportIonInfoComparer
		Implements IComparer

		Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
			Dim udtIonInfoA As udtReporterIonInfoType
			Dim udtIonInfoB As udtReporterIonInfoType

			udtIonInfoA = DirectCast(x, udtReporterIonInfoType)
			udtIonInfoB = DirectCast(y, udtReporterIonInfoType)

			If udtIonInfoA.MZ > udtIonInfoB.MZ Then
				Return 1
			ElseIf udtIonInfoA.MZ < udtIonInfoB.MZ Then
				Return -1
			Else
				Return 0
			End If
		End Function
	End Class

	Private Sub mXcaliburAccessor_ReportError(strMessage As String) Handles mXcaliburAccessor.ReportError
		Console.WriteLine(strMessage)
		LogErrors("XcaliburAccessor", strMessage, Nothing, True, False, eMasicErrorCodes.InputFileDataReadError)
	End Sub

	Private Sub mXcaliburAccessor_ReportWarning(strMessage As String) Handles mXcaliburAccessor.ReportWarning
		Console.WriteLine(strMessage)
		LogErrors("XcaliburAccessor", strMessage, Nothing, False, False, eMasicErrorCodes.InputFileDataReadError)
	End Sub
End Class
