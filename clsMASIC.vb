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

Imports System.Runtime.InteropServices

Public Class clsMASIC
    Inherits clsProcessFilesBaseClass

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        MyBase.mFileDate = "January 21, 2017"

        mLocalErrorCode = eMasicErrorCodes.NoError
        mStatusMessage = String.Empty

        mProcessingStats = New clsProcessingStats()
        InitializeMemoryManagementOptions(mProcessingStats)

        mMASICPeakFinder = New MASICPeakFinder.clsMASICPeakFinder()

        mOptions = New clsMASICOptions(Me.FileVersion(), mMASICPeakFinder.ProgramVersion)
        mOptions.InitializeVariables()
        RegisterEvents(mOptions)

        Try
            mFreeMemoryPerformanceCounter = New Diagnostics.PerformanceCounter("Memory", "Available MBytes")
            mFreeMemoryPerformanceCounter.ReadOnly = True
        Catch ex As Exception
            LogErrors("InitializeVariables", "Error instantiating the Memory->'Available MBytes' performance counter", ex, False, False, eMasicErrorCodes.NoError)
        End Try

    End Sub

#Region "Constants and Enums"

    ' Enabling this will result in SICs with less noise, which will hurt noise determination after finding the SICs
    Public Const DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD As Boolean = False

    ' Disabling this will slow down the correlation process (slightly)
    Public Const DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD As Boolean = True

    Private Const MINIMUM_STATUS_FILE_UPDATE_INTERVAL_SECONDS As Integer = 3

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
        UnspecifiedError = -1
    End Enum

#End Region

#Region "Classwide Variables"

    Private ReadOnly mOptions As clsMASICOptions

    Private ReadOnly mMASICPeakFinder As MASICPeakFinder.clsMASICPeakFinder

    Private ReadOnly mProcessingStats As clsProcessingStats

    ''' <summary>
    ''' Current processing step
    ''' </summary>
    Private mProcessingStep As eProcessingStepConstants

    ''' <summary>
    ''' Percent completion for the current sub task
    ''' </summary>
    Private mSubtaskProcessingStepPct As Short

    Private mSubtaskDescription As String = String.Empty

    Private mLocalErrorCode As eMasicErrorCodes
    Private mStatusMessage As String

    Private mFreeMemoryPerformanceCounter As PerformanceCounter

#End Region

#Region "Events"
    ''' <summary>
    ''' Use RaiseEvent MyBase.ProgressChanged when updating the overall progress
    ''' Use ProgressSubtaskChanged when updating the sub task progress
    ''' </summary>
    Public Event ProgressSubtaskChanged()

    Public Event ProgressResetKeypressAbort()

#End Region

#Region "Processing Options and File Path Interface Functions"
    <Obsolete("Use Property Options")>
    Public Property DatabaseConnectionString() As String
        Get
            Return mOptions.DatabaseConnectionString
        End Get
        Set(Value As String)
            mOptions.DatabaseConnectionString = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property DatasetInfoQuerySql() As String
        Get
            Return mOptions.DatasetInfoQuerySql
        End Get
        Set(Value As String)
            mOptions.DatasetInfoQuerySql = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property DatasetLookupFilePath() As String
        Get
            Return mOptions.DatasetLookupFilePath
        End Get
        Set(Value As String)
            mOptions.DatasetLookupFilePath = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property DatasetNumber() As Integer
        Get
            Return mOptions.SICOptions.DatasetNumber
        End Get
        Set(Value As Integer)
            mOptions.SICOptions.DatasetNumber = Value
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

    <Obsolete("Use Property Options")>
    Public Property MASICStatusFilename() As String
        Get
            Return mOptions.MASICStatusFilename
        End Get
        Set(value As String)
            If value Is Nothing OrElse value.Trim.Length = 0 Then
                mOptions.MASICStatusFilename = clsMASICOptions.DEFAULT_MASIC_STATUS_FILE_NAME
            Else
                mOptions.MASICStatusFilename = value
            End If
        End Set
    End Property

    Public ReadOnly Property Options() As clsMASICOptions
        Get
            Return mOptions
        End Get
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
    <Obsolete("Use Property Options")>
    Public Property CDFTimeInSeconds() As Boolean
        Get
            Return mOptions.CDFTimeInSeconds
        End Get
        Set(Value As Boolean)
            mOptions.CDFTimeInSeconds = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property CompressMSSpectraData() As Boolean
        Get
            Return mOptions.SICOptions.CompressMSSpectraData
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.CompressMSSpectraData = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property CompressMSMSSpectraData() As Boolean
        Get
            Return mOptions.SICOptions.CompressMSMSSpectraData
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.CompressMSMSSpectraData = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property CompressToleranceDivisorForDa() As Double
        Get
            Return mOptions.SICOptions.CompressToleranceDivisorForDa
        End Get
        Set(value As Double)
            mOptions.SICOptions.CompressToleranceDivisorForDa = value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property CompressToleranceDivisorForPPM() As Double
        Get
            Return mOptions.SICOptions.CompressToleranceDivisorForPPM
        End Get
        Set(value As Double)
            mOptions.SICOptions.CompressToleranceDivisorForPPM = value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ConsolidateConstantExtendedHeaderValues() As Boolean
        Get
            Return mOptions.ConsolidateConstantExtendedHeaderValues
        End Get
        Set(value As Boolean)
            mOptions.ConsolidateConstantExtendedHeaderValues = value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public ReadOnly Property CustomSICListScanType() As clsCustomSICList.eCustomSICScanTypeConstants
        Get
            Return mOptions.CustomSICList.ScanToleranceType
        End Get
    End Property

    <Obsolete("Use Property Options")>
    Public ReadOnly Property CustomSICListScanTolerance() As Single
        Get
            Return mOptions.CustomSICList.ScanOrAcqTimeTolerance
        End Get
    End Property

    <Obsolete("Use Property Options")>
    Public ReadOnly Property CustomSICListSearchValues() As List(Of clsCustomMZSearchSpec)
        Get
            Return mOptions.CustomSICList.CustomMZSearchValues
        End Get
    End Property

    <Obsolete("Use Property Options")>
    Public Property CustomSICListFileName() As String
        Get
            Return mOptions.CustomSICList.CustomSICListFileName
        End Get
        Set(Value As String)
            mOptions.CustomSICList.CustomSICListFileName = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataOnly() As Boolean
        Get
            Return mOptions.ExportRawDataOnly
        End Get
        Set(Value As Boolean)
            mOptions.ExportRawDataOnly = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property FastExistingXMLFileTest() As Boolean
        Get
            Return mOptions.FastExistingXMLFileTest
        End Get
        Set(Value As Boolean)
            mOptions.FastExistingXMLFileTest = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property IncludeHeadersInExportFile() As Boolean
        Get
            Return mOptions.IncludeHeadersInExportFile
        End Get
        Set(Value As Boolean)
            mOptions.IncludeHeadersInExportFile = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property IncludeScanTimesInSICStatsFile() As Boolean
        Get
            Return mOptions.IncludeScanTimesInSICStatsFile
        End Get
        Set(Value As Boolean)
            mOptions.IncludeScanTimesInSICStatsFile = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property LimitSearchToCustomMZList() As Boolean
        Get
            Return mOptions.CustomSICList.LimitSearchToCustomMZList
        End Get
        Set(Value As Boolean)
            mOptions.CustomSICList.LimitSearchToCustomMZList = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ParentIonDecoyMassDa() As Double
        Get
            Return mOptions.ParentIonDecoyMassDa
        End Get
        Set(value As Double)
            mOptions.ParentIonDecoyMassDa = value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SkipMSMSProcessing() As Boolean
        Get
            Return mOptions.SkipMSMSProcessing
        End Get
        Set(Value As Boolean)
            mOptions.SkipMSMSProcessing = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SkipSICAndRawDataProcessing() As Boolean
        Get
            Return mOptions.SkipSICAndRawDataProcessing
        End Get
        Set(Value As Boolean)
            mOptions.SkipSICAndRawDataProcessing = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SuppressNoParentIonsError() As Boolean
        Get
            Return mOptions.SuppressNoParentIonsError
        End Get
        Set(Value As Boolean)
            mOptions.SuppressNoParentIonsError = Value
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

    <Obsolete("Use Property Options")>
    Public Property WriteDetailedSICDataFile() As Boolean
        Get
            Return mOptions.WriteDetailedSICDataFile
        End Get
        Set(value As Boolean)
            mOptions.WriteDetailedSICDataFile = value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property WriteExtendedStats() As Boolean
        Get
            Return mOptions.WriteExtendedStats
        End Get
        Set(Value As Boolean)
            mOptions.WriteExtendedStats = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property WriteExtendedStatsIncludeScanFilterText() As Boolean
        Get
            Return mOptions.WriteExtendedStatsIncludeScanFilterText
        End Get
        Set(Value As Boolean)
            mOptions.WriteExtendedStatsIncludeScanFilterText = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property WriteExtendedStatsStatusLog() As Boolean
        Get
            Return mOptions.WriteExtendedStatsStatusLog
        End Get
        Set(Value As Boolean)
            mOptions.WriteExtendedStatsStatusLog = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property WriteMSMethodFile() As Boolean
        Get
            Return mOptions.WriteMSMethodFile
        End Get
        Set(Value As Boolean)
            mOptions.WriteMSMethodFile = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property WriteMSTuneFile() As Boolean
        Get
            Return mOptions.WriteMSTuneFile
        End Get
        Set(Value As Boolean)
            mOptions.WriteMSTuneFile = Value
        End Set
    End Property

    ''' <summary>
    ''' This property is included for historical reasons since SIC tolerance can now be Da or PPM
    ''' </summary>
    ''' <returns></returns>
    <Obsolete("Use Property Options.  Also, the SICToleranceDa setting should not be used; use SetSICTolerance and GetSICTolerance instead")>
    Public Property SICToleranceDa() As Double
        Get
            Return mOptions.SICOptions.SICToleranceDa
        End Get
        Set(value As Double)
            mOptions.SICOptions.SICToleranceDa = value
        End Set
    End Property

    <Obsolete("Use Property Options.SICOptions.GetSICTolerance")>
    Public Function GetSICTolerance() As Double
        Dim blnToleranceIsPPM As Boolean
        Return mOptions.SICOptions.GetSICTolerance(blnToleranceIsPPM)
    End Function

    <Obsolete("Use Property Options.SICOptions.GetSICTolerance")>
    Public Function GetSICTolerance(<Out()> ByRef blnSICToleranceIsPPM As Boolean) As Double
        Return mOptions.SICOptions.GetSICTolerance(blnSICToleranceIsPPM)
    End Function

    <Obsolete("Use Property Options.SICOptions.SetSICTolerance")>
    Public Sub SetSICTolerance(dblSICTolerance As Double, blnSICToleranceIsPPM As Boolean)
        mOptions.SICOptions.SetSICTolerance(dblSICTolerance, blnSICToleranceIsPPM)
    End Sub

    <Obsolete("Use Property Options")>
    Public Property SICToleranceIsPPM() As Boolean
        Get
            Return mOptions.SICOptions.SICToleranceIsPPM
        End Get
        Set(value As Boolean)
            mOptions.SICOptions.SICToleranceIsPPM = value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property RefineReportedParentIonMZ() As Boolean
        Get
            Return mOptions.SICOptions.RefineReportedParentIonMZ
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.RefineReportedParentIonMZ = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property RTRangeEnd() As Single
        Get
            Return mOptions.SICOptions.RTRangeEnd
        End Get
        Set(Value As Single)
            mOptions.SICOptions.RTRangeEnd = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property RTRangeStart() As Single
        Get
            Return mOptions.SICOptions.RTRangeStart
        End Get
        Set(Value As Single)
            mOptions.SICOptions.RTRangeStart = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ScanRangeEnd() As Integer
        Get
            Return mOptions.SICOptions.ScanRangeEnd
        End Get
        Set(Value As Integer)
            mOptions.SICOptions.ScanRangeEnd = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ScanRangeStart() As Integer
        Get
            Return mOptions.SICOptions.ScanRangeStart
        End Get
        Set(Value As Integer)
            mOptions.SICOptions.ScanRangeStart = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MaxSICPeakWidthMinutesBackward() As Single
        Get
            Return mOptions.SICOptions.MaxSICPeakWidthMinutesBackward
        End Get
        Set(Value As Single)
            mOptions.SICOptions.MaxSICPeakWidthMinutesBackward = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MaxSICPeakWidthMinutesForward() As Single
        Get
            Return mOptions.SICOptions.MaxSICPeakWidthMinutesForward
        End Get
        Set(Value As Single)
            mOptions.SICOptions.MaxSICPeakWidthMinutesForward = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SICNoiseFractionLowIntensityDataToAverage() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SICNoiseMinimumSignalToNoiseRatio() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio
        End Get
        Set(Value As Single)
            ' This value isn't utilized by MASIC for SICs so we'll force it to always be zero
            mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SICNoiseThresholdIntensity() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SICNoiseThresholdMode() As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode
        End Get
        Set(Value As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
            mOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MassSpectraNoiseFractionLowIntensityDataToAverage() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MassSpectraNoiseMinimumSignalToNoiseRatio() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MassSpectraNoiseThresholdIntensity() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute
        End Get
        Set(Value As Single)
            If Value < 0 Or Value > Single.MaxValue Then Value = 0
            mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MassSpectraNoiseThresholdMode() As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode
        End Get
        Set(Value As MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
            mOptions.SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ReplaceSICZeroesWithMinimumPositiveValueFromMSData() As Boolean
        Get
            Return mOptions.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData = Value
        End Set
    End Property
#End Region

#Region "Raw Data Export Options"

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataIncludeMSMS() As Boolean
        Get
            Return mOptions.RawDataExportOptions.IncludeMSMS
        End Get
        Set(Value As Boolean)
            mOptions.RawDataExportOptions.IncludeMSMS = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataRenumberScans() As Boolean
        Get
            Return mOptions.RawDataExportOptions.RenumberScans
        End Get
        Set(Value As Boolean)
            mOptions.RawDataExportOptions.RenumberScans = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataIntensityMinimum() As Single
        Get
            Return mOptions.RawDataExportOptions.IntensityMinimum
        End Get
        Set(Value As Single)
            mOptions.RawDataExportOptions.IntensityMinimum = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataMaxIonCountPerScan() As Integer
        Get
            Return mOptions.RawDataExportOptions.MaxIonCountPerScan
        End Get
        Set(Value As Integer)
            mOptions.RawDataExportOptions.MaxIonCountPerScan = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataFileFormat() As clsRawDataExportOptions.eExportRawDataFileFormatConstants
        Get
            Return mOptions.RawDataExportOptions.FileFormat
        End Get
        Set(Value As clsRawDataExportOptions.eExportRawDataFileFormatConstants)
            mOptions.RawDataExportOptions.FileFormat = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawDataMinimumSignalToNoiseRatio() As Single
        Get
            Return mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio
        End Get
        Set(Value As Single)
            mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ExportRawSpectraData() As Boolean
        Get
            Return mOptions.RawDataExportOptions.ExportEnabled
        End Get
        Set(Value As Boolean)
            mOptions.RawDataExportOptions.ExportEnabled = Value
        End Set
    End Property

#End Region

#Region "Peak Finding Options"
    <Obsolete("Use Property Options")>
    Public Property IntensityThresholdAbsoluteMinimum() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property IntensityThresholdFractionMax() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MaxDistanceScansNoOverlap() As Integer
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap
        End Get
        Set(Value As Integer)
            mOptions.SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property FindPeaksOnSmoothedData() As Boolean
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SmoothDataRegardlessOfMinimumPeakWidth() As Boolean
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property UseButterworthSmooth() As Boolean
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.UseButterworthSmooth
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.SICPeakFinderOptions.UseButterworthSmooth = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ButterworthSamplingFrequency() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency
        End Get
        Set(Value As Single)
            ' Value should be between 0.01 and 0.99; this is checked for in the filter, so we don't need to check here
            mOptions.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property ButterworthSamplingFrequencyDoubledForSIMData() As Boolean
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property UseSavitzkyGolaySmooth() As Boolean
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SavitzkyGolayFilterOrder() As Short
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder
        End Get
        Set(Value As Short)
            mOptions.SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SaveSmoothedData() As Boolean
        Get
            Return mOptions.SICOptions.SaveSmoothedData
        End Get
        Set(Value As Boolean)
            mOptions.SICOptions.SaveSmoothedData = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MaxAllowedUpwardSpikeFractionMax() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property InitialPeakWidthScansScaler() As Single
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property InitialPeakWidthScansMaximum() As Integer
        Get
            Return mOptions.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum
        End Get
        Set(Value As Integer)
            mOptions.SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum = Value
        End Set
    End Property
#End Region

#Region "Spectrum Similarity Options"
    <Obsolete("Use Property Options")>
    Public Property SimilarIonMZToleranceHalfWidth() As Single
        Get
            Return mOptions.SICOptions.SimilarIonMZToleranceHalfWidth
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SimilarIonMZToleranceHalfWidth = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SimilarIonToleranceHalfWidthMinutes() As Single
        Get
            Return mOptions.SICOptions.SimilarIonToleranceHalfWidthMinutes
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SimilarIonToleranceHalfWidthMinutes = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SpectrumSimilarityMinimum() As Single
        Get
            Return mOptions.SICOptions.SpectrumSimilarityMinimum
        End Get
        Set(Value As Single)
            mOptions.SICOptions.SpectrumSimilarityMinimum = Value
        End Set
    End Property
#End Region

#Region "Binning Options Interface Functions"

    <Obsolete("Use Property Options")>
    Public Property BinStartX() As Single
        Get
            Return mOptions.BinningOptions.StartX
        End Get
        Set(Value As Single)
            mOptions.BinningOptions.StartX = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property BinEndX() As Single
        Get
            Return mOptions.BinningOptions.EndX
        End Get
        Set(Value As Single)
            mOptions.BinningOptions.EndX = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property BinSize() As Single
        Get
            Return mOptions.BinningOptions.BinSize
        End Get
        Set(Value As Single)
            mOptions.BinningOptions.BinSize = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property BinnedDataIntensityPrecisionPercent() As Single
        Get
            Return mOptions.BinningOptions.IntensityPrecisionPercent
        End Get
        Set(Value As Single)
            mOptions.BinningOptions.IntensityPrecisionPercent = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property NormalizeBinnedData() As Boolean
        Get
            Return mOptions.BinningOptions.Normalize
        End Get
        Set(Value As Boolean)
            mOptions.BinningOptions.Normalize = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property SumAllIntensitiesForBin() As Boolean
        Get
            Return mOptions.BinningOptions.SumAllIntensitiesForBin
        End Get
        Set(Value As Boolean)
            mOptions.BinningOptions.SumAllIntensitiesForBin = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property MaximumBinCount() As Integer
        Get
            Return mOptions.BinningOptions.MaximumBinCount
        End Get
        Set(Value As Integer)
            mOptions.BinningOptions.MaximumBinCount = Value
        End Set
    End Property
#End Region

#Region "Memory Options Interface Functions"

    <Obsolete("Use Property Options")>
    Public Property DiskCachingAlwaysDisabled() As Boolean
        Get
            Return mOptions.CacheOptions.DiskCachingAlwaysDisabled
        End Get
        Set(Value As Boolean)
            mOptions.CacheOptions.DiskCachingAlwaysDisabled = Value
        End Set
    End Property

    <Obsolete("Use Property Options")>
    Public Property CacheFolderPath() As String
        Get
            Return mOptions.CacheOptions.FolderPath
        End Get
        Set(Value As String)
            mOptions.CacheOptions.FolderPath = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheMaximumMemoryUsageMB() As Single
        Get
            Return mOptions.CacheOptions.MaximumMemoryUsageMB
        End Get
        Set(Value As Single)
            mOptions.CacheOptions.MaximumMemoryUsageMB = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheMinimumFreeMemoryMB() As Single
        Get
            Return mOptions.CacheOptions.MinimumFreeMemoryMB
        End Get
        Set(Value As Single)
            If mOptions.CacheOptions.MinimumFreeMemoryMB < 10 Then
                mOptions.CacheOptions.MinimumFreeMemoryMB = 10
            End If
            mOptions.CacheOptions.MinimumFreeMemoryMB = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheSpectraToRetainInMemory() As Integer
        Get
            Return mOptions.CacheOptions.SpectraToRetainInMemory
        End Get
        Set(Value As Integer)
            mOptions.CacheOptions.SpectraToRetainInMemory = Value
        End Set
    End Property

#End Region

    Public Overrides Sub AbortProcessingNow()
        mAbortProcessing = True
        mOptions.AbortProcessing = True
    End Sub

    Public Overrides Function GetDefaultExtensionsToParse() As String()
        Return DataInput.clsDataImport.GetDefaultExtensionsToParse()
    End Function

    Public Overrides Function GetErrorMessage() As String
        ' Returns String.Empty if no error

        Dim strErrorMessage As String

        If MyBase.ErrorCode = eProcessFilesErrorCodes.LocalizedError OrElse
           MyBase.ErrorCode = eProcessFilesErrorCodes.NoError Then
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

    Private Function GetFreeMemoryMB() As Single
        ' Returns the amount of free memory, in MB

        If mFreeMemoryPerformanceCounter Is Nothing Then
            Return 0
        Else
            Return mFreeMemoryPerformanceCounter.NextValue()
        End If

    End Function

    Private Function GetProcessMemoryUsageMB() As Single

        ' Obtain a handle to the current process
        Dim objProcess = Process.GetCurrentProcess()

        ' The WorkingSet is the total physical memory usage 
        Return CSng(objProcess.WorkingSet64 / 1024 / 1024)

    End Function

    Private Function GetTotalProcessingTimeSec() As Single

        Dim objProcess = Process.GetCurrentProcess()

        Return CSng(objProcess.TotalProcessorTime().TotalSeconds)

    End Function

    Private Sub InitializeMemoryManagementOptions(processingStats As clsProcessingStats)

        With processingStats
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

    Public Function LoadParameterFileSettings(strParameterFilePath As String) As Boolean
        Dim success = mOptions.LoadParameterFileSettings(strParameterFilePath)
        Return success
    End Function

    Private Sub LogErrors(
      strSource As String,
      strMessage As String,
      ex As Exception,
      Optional blnAllowInformUser As Boolean = True,
      Optional blnAllowThrowingException As Boolean = True,
      Optional eNewErrorCode As eMasicErrorCodes = eMasicErrorCodes.NoError)

        Dim strMessageWithoutCRLF As String

        mOptions.StatusMessage = strMessage

        strMessageWithoutCRLF = mOptions.StatusMessage.Replace(ControlChars.NewLine, "; ")

        If ex Is Nothing Then
            ex = New Exception("Error")
        Else
            If Not ex.Message Is Nothing AndAlso ex.Message.Length > 0 AndAlso Not strMessage.Contains(ex.Message) Then
                strMessageWithoutCRLF &= "; " & ex.Message
            End If
        End If

        ' Show the message and log to the clsProcessFilesBaseClass logger
        ShowErrorMessage(strSource & ": " & strMessageWithoutCRLF, True)

        If Not eNewErrorCode = eMasicErrorCodes.NoError Then
            SetLocalErrorCode(eNewErrorCode, True)
        End If

        If MyBase.ShowMessages AndAlso blnAllowInformUser Then
            MessageBox.Show(mOptions.StatusMessage & ControlChars.NewLine & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        ElseIf blnAllowThrowingException Then
            Throw New Exception(mOptions.StatusMessage, ex)
        End If
    End Sub

    ' Main processing function
    Public Overloads Overrides Function ProcessFile(
      strInputFilePath As String,
      strOutputFolderPath As String,
      strParameterFilePath As String,
      blnResetErrorCode As Boolean) As Boolean

        Dim ioFileInfo As FileInfo

        Dim blnSuccess, blnDoNotProcess As Boolean
        Dim blnKeepRawMSSpectra As Boolean

        Dim strInputFilePathFull As String = String.Empty
        Dim strInputFileName As String = String.Empty

        Dim intSimilarParentIonUpdateCount As Integer

        If blnResetErrorCode Then
            SetLocalErrorCode(eMasicErrorCodes.NoError)
        End If

        mOptions.OutputFolderPath = strOutputFolderPath

        mSubtaskProcessingStepPct = 0
        UpdateProcessingStep(eProcessingStepConstants.NewTask, True)
        MyBase.ResetProgress("Starting calculations")

        mStatusMessage = String.Empty

        UpdateStatusFile(True)

        If Not mOptions.LoadParameterFileSettings(strParameterFilePath) Then
            MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidParameterFile)
            mStatusMessage = "Parameter file load error: " & strParameterFilePath

            If MyBase.ShowMessages Then
                MessageBox.Show(mStatusMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If

            MyBase.ShowErrorMessage(mStatusMessage)

            If MyBase.ErrorCode = eProcessFilesErrorCodes.NoError Then
                MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidParameterFile)
            End If
            UpdateProcessingStep(eProcessingStepConstants.Cancelled, True)

            LogMessage("Processing ended in error")
            Return False
        End If

        Dim dataOutputHandler = New DataOutput.clsDataOutput(mOptions)
        RegisterEvents(dataOutputHandler)

        Try
            ' If a Custom SICList file is defined, then load the custom SIC values now
            If mOptions.CustomSICList.CustomSICListFileName.Length > 0 Then
                Dim sicListReader = New DataInput.clsCustomSICListReader(mOptions.CustomSICList)
                RegisterEvents(sicListReader)

                LogMessage("ProcessFile: Reading custom SIC values file: " & mOptions.CustomSICList.CustomSICListFileName)
                blnSuccess = sicListReader.LoadCustomSICListFromFile(mOptions.CustomSICList.CustomSICListFileName)
                If Not blnSuccess Then
                    SetLocalErrorCode(eMasicErrorCodes.InvalidCustomSICValues)
                    Exit Try
                End If
            End If

            mOptions.ReporterIons.UpdateMZIntensityFilterIgnoreRange()

            LogMessage("Source data file: " & strInputFilePath)

            If strInputFilePath Is Nothing OrElse strInputFilePath.Length = 0 Then
                ShowErrorMessage("Input file name is empty")
                MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
                Exit Try
            End If


            mStatusMessage = "Parsing " & Path.GetFileName(strInputFilePath)
            Console.WriteLine()
            ShowMessage(mStatusMessage)

            blnSuccess = CleanupFilePaths(strInputFilePath, strOutputFolderPath)
            mOptions.OutputFolderPath = strOutputFolderPath

            If blnSuccess Then
                Dim dbAccessor = New clsDatabaseAccess(mOptions)
                RegisterEvents(dbAccessor)

                mOptions.SICOptions.DatasetNumber = dbAccessor.LookupDatasetNumber(strInputFilePath, mOptions.DatasetLookupFilePath, mOptions.SICOptions.DatasetNumber)

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
                If mLocalErrorCode = eMasicErrorCodes.NoError Then MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.FilePathError)
                Exit Try
            End If

            Dim xmlResultsWriter = New DataOutput.clsXMLResultsWriter(mOptions)
            RegisterEvents(xmlResultsWriter)

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

                blnDoNotProcess = dataOutputHandler.CheckForExistingResults(strInputFilePathFull, strOutputFolderPath, mOptions)

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
                File.Delete(strOutputFileTestPath)

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

            Dim objSpectraCache = New clsSpectraCache(mOptions.CacheOptions) With {
                .ShowMessages = MyBase.ShowMessages,
                .DiskCachingAlwaysDisabled = mOptions.CacheOptions.DiskCachingAlwaysDisabled,
                .CacheFolderPath = mOptions.CacheOptions.FolderPath,
                .CacheSpectraToRetainInMemory = mOptions.CacheOptions.SpectraToRetainInMemory
            }
            RegisterEvents(objSpectraCache)

            objSpectraCache.InitializeSpectraPool()

            Dim datasetFileInfo = new DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType

            Dim scanList = New clsScanList()
            RegisterEvents(scanList)

            Dim parentIonProcessor = New clsParentIonProcessing(mOptions.ReporterIons)
            RegisterEvents(parentIonProcessor)

            Dim scanTracking = New clsScanTracking(mOptions.ReporterIons, mMASICPeakFinder)
            RegisterEvents(scanTracking)

            Dim dataImporterBase As DataInput.clsDataImport = Nothing

            Try

                '---------------------------------------------------------
                ' Define strInputFileName (which is referenced several times below)
                '---------------------------------------------------------
                strInputFileName = Path.GetFileName(strInputFilePathFull)

                '---------------------------------------------------------
                ' Create the _ScanStats.txt file
                '---------------------------------------------------------
                dataOutputHandler.OpenOutputFileHandles(strInputFileName, strOutputFolderPath, mOptions.IncludeHeadersInExportFile)

                '---------------------------------------------------------
                ' Read the mass spectra from the input data file
                '---------------------------------------------------------

                UpdateProcessingStep(eProcessingStepConstants.ReadDataFile)
                SetSubtaskProcessingStepPct(0)
                UpdatePeakMemoryUsage()
                mStatusMessage = String.Empty

                If mOptions.SkipSICAndRawDataProcessing Then
                    mOptions.ExportRawDataOnly = False
                End If

                blnKeepRawMSSpectra = Not mOptions.SkipSICAndRawDataProcessing OrElse mOptions.ExportRawDataOnly

                mOptions.SICOptions.ValidateSICOptions()

                Select Case Path.GetExtension(strInputFilePath).ToUpper()
                    Case DataInput.clsDataImport.FINNIGAN_RAW_FILE_EXTENSION.ToUpper()

                        ' Open the .Raw file and obtain the scan information

                        Dim dataImporter = New DataInput.clsDataImportThermoRaw(mOptions, mMASICPeakFinder, parentIonProcessor, scanTracking)
                        RegisterDataImportEvents(dataImporter)
                        dataImporterBase = dataImporter

                        blnSuccess = dataImporter.ExtractScanInfoFromXcaliburDataFile(
                          strInputFilePathFull,
                          scanList, objSpectraCache, dataOutputHandler,
                          blnKeepRawMSSpectra,
                          Not mOptions.SkipMSMSProcessing)

                        datasetFileInfo = dataImporter.DatasetFileInfo

                    Case DataInput.clsDataImport.MZ_XML_FILE_EXTENSION1.ToUpper(),
                         DataInput.clsDataImport.MZ_XML_FILE_EXTENSION2.ToUpper()

                        ' Open the .mzXML file and obtain the scan information

                        Dim dataImporter = New DataInput.clsDataImportMSXml(mOptions, mMASICPeakFinder, parentIonProcessor, scanTracking)
                        RegisterDataImportEvents(dataImporter)
                        dataImporterBase = dataImporter

                        blnSuccess = dataImporter.ExtractScanInfoFromMZXMLDataFile(
                          strInputFilePathFull,
                          scanList, objSpectraCache, dataOutputHandler,
                          blnKeepRawMSSpectra,
                          Not mOptions.SkipMSMSProcessing)

                        datasetFileInfo = dataImporter.DatasetFileInfo

                    Case DataInput.clsDataImport.MZ_DATA_FILE_EXTENSION1.ToUpper(),
                         DataInput.clsDataImport.MZ_DATA_FILE_EXTENSION2.ToUpper()

                        ' Open the .mzData file and obtain the scan information

                        Dim dataImporter = New DataInput.clsDataImportMSXml(mOptions, mMASICPeakFinder, parentIonProcessor, scanTracking)
                        RegisterDataImportEvents(dataImporter)
                        dataImporterBase = dataImporter

                        blnSuccess = dataImporter.ExtractScanInfoFromMZDataFile(
                          strInputFilePathFull,
                          scanList, objSpectraCache, dataOutputHandler,
                          blnKeepRawMSSpectra, Not mOptions.SkipMSMSProcessing)

                        datasetFileInfo = dataImporter.DatasetFileInfo

                    Case DataInput.clsDataImport.AGILENT_MSMS_FILE_EXTENSION.ToUpper(),
                         DataInput.clsDataImport.AGILENT_MS_FILE_EXTENSION.ToUpper()

                        ' Open the .MGF and .CDF files to obtain the scan information

                        Dim dataImporter = New DataInput.clsDataImportMGFandCDF(mOptions, mMASICPeakFinder, parentIonProcessor, scanTracking)
                        RegisterDataImportEvents(dataImporter)
                        dataImporterBase = dataImporter

                        blnSuccess = dataImporter.ExtractScanInfoFromMGFandCDF(
                          strInputFilePathFull,
                          scanList, objSpectraCache, dataOutputHandler,
                          blnKeepRawMSSpectra, Not mOptions.SkipMSMSProcessing)

                        datasetFileInfo = dataImporter.DatasetFileInfo

                    Case Else
                        mStatusMessage = "Unknown file extension: " & Path.GetExtension(strInputFilePathFull)
                        SetLocalErrorCode(eMasicErrorCodes.UnknownFileExtension)
                        blnSuccess = False

                        ' Instantiate this object to avoid a warning below about the object potentially not being initialized
                        ' In reality, an Exit Try statement will be reached and the potentially problematic use will therefore not get encountered
                        datasetFileInfo = New DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType()
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
                    MessageBox.Show(mStatusMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    LogMessage(mStatusMessage, eMessageTypeConstants.ErrorMsg)
                Else
                    MyBase.ShowErrorMessage(mStatusMessage)
                End If
                Exit Try
            End If

            Try
                ' Make sure the arrays in scanList range from 0 to the Count-1
                With scanList
                    If .MasterScanOrderCount <> .MasterScanOrder.Length Then
                        ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
                        ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
                        ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)
                    End If

                    If .ParentIonInfoCount <> .ParentIons.Length Then ReDim Preserve .ParentIons(.ParentIonInfoCount - 1)
                End With
            Catch ex As Exception
                blnSuccess = False
                LogErrors("ProcessFile", "Error resizing the arrays in scanList", ex, True, False, eMasicErrorCodes.UnspecifiedError)
                Exit Try
            End Try

            Dim bpiWriter = New DataOutput.clsBPIWriter()
            RegisterEvents(bpiWriter)

            Try
                '---------------------------------------------------------
                ' Save the BPIs and TICs
                '---------------------------------------------------------

                UpdateProcessingStep(eProcessingStepConstants.SaveBPI)
                UpdateOverallProgress("Processing Data for " & strInputFileName)
                SetSubtaskProcessingStepPct(0, "Saving chromatograms to disk")
                UpdatePeakMemoryUsage()

                If mOptions.SkipSICAndRawDataProcessing OrElse Not mOptions.ExportRawDataOnly Then
                    LogMessage("ProcessFile: Call SaveBPIs")
                    bpiWriter.SaveBPIs(scanList, objSpectraCache, strInputFilePathFull, strOutputFolderPath)
                End If

                '---------------------------------------------------------
                ' Close the ScanStats file handle
                '---------------------------------------------------------
                Try
                    LogMessage("ProcessFile: Close outputFileHandles.ScanStats")

                    dataOutputHandler.OutputFileHandles.CloseScanStats()

                Catch ex As Exception
                    ' Ignore errors here
                End Try

                '---------------------------------------------------------
                ' Create the DatasetInfo XML file
                '---------------------------------------------------------

                LogMessage("ProcessFile: Create DatasetInfo File")
                dataOutputHandler.CreateDatasetInfoFile(strInputFileName, strOutputFolderPath, scanTracking, datasetFileInfo)

                If mOptions.SkipSICAndRawDataProcessing Then
                    LogMessage("ProcessFile: Skipping SIC Processing")

                    SetDefaultPeakLocValues(scanList)
                Else

                    '---------------------------------------------------------
                    ' Optionally, export the raw mass spectra data
                    '---------------------------------------------------------
                    If mOptions.RawDataExportOptions.ExportEnabled Then
                        Dim rawDataExporter = New DataOutput.clsSpectrumDataWriter(bpiWriter, mOptions)
                        RegisterEvents(rawDataExporter)

                        rawDataExporter.ExportRawDataToDisk(scanList, objSpectraCache, strInputFileName, strOutputFolderPath)
                    End If

                    If mOptions.ReporterIons.ReporterIonStatsEnabled Then
                        ' Look for Reporter Ions in the Fragmentation spectra

                        Dim reporterionProcessor = New clsReporterIonProcessor(mOptions)
                        RegisterEvents(reporterionProcessor)
                        reporterionProcessor.FindReporterIons(scanList, objSpectraCache, strInputFilePathFull, strOutputFolderPath)
                    End If

                    Dim mrmProcessor = New clsMRMProcessing(mOptions, dataOutputHandler)
                    RegisterEvents(mrmProcessor)

                    '---------------------------------------------------------
                    ' If MRM data is present, then save the MRM values to disk
                    '---------------------------------------------------------
                    If scanList.MRMDataPresent Then

                        If blnSuccess Then
                            mrmProcessor.ExportMRMDataToDisk(scanList, objSpectraCache, strInputFileName, strOutputFolderPath)
                        End If
                    End If


                    If Not mOptions.ExportRawDataOnly Then

                        '---------------------------------------------------------
                        ' Add the custom SIC values to scanList
                        '---------------------------------------------------------
                        mOptions.CustomSICList.AddCustomSICValues(scanList, mOptions.SICOptions.SICTolerance,
                                           mOptions.SICOptions.SICToleranceIsPPM, mOptions.CustomSICList.ScanOrAcqTimeTolerance)


                        '---------------------------------------------------------
                        ' Possibly create the Tab-separated values SIC details output file
                        '---------------------------------------------------------
                        If mOptions.WriteDetailedSICDataFile Then
                            blnSuccess = dataOutputHandler.InitializeSICDetailsTextFile(strInputFilePathFull, strOutputFolderPath)
                            If Not blnSuccess Then
                                SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError)
                                Exit Try
                            End If
                        End If

                        '---------------------------------------------------------
                        ' Create the XML output file
                        '---------------------------------------------------------
                        blnSuccess = xmlResultsWriter.XMLOutputFileInitialize(strInputFilePathFull, strOutputFolderPath, dataOutputHandler, scanList, objSpectraCache, mOptions.SICOptions, mOptions.BinningOptions)
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
                        Dim sicProcessor = New clsSICProcessing(mMASICPeakFinder, mrmProcessor)
                        RegisterEvents(sicProcessor)

                        blnSuccess = sicProcessor.CreateParentIonSICs(scanList, objSpectraCache, mOptions, dataOutputHandler, sicProcessor, xmlResultsWriter)

                        If Not blnSuccess Then
                            SetLocalErrorCode(eMasicErrorCodes.CreateSICsError, True)
                            Exit Try
                        End If

                    End If


                    If Not (mOptions.SkipMSMSProcessing OrElse mOptions.ExportRawDataOnly) Then

                        '---------------------------------------------------------
                        ' Find Similar Parent Ions
                        '---------------------------------------------------------

                        UpdateProcessingStep(eProcessingStepConstants.FindSimilarParentIons)
                        SetSubtaskProcessingStepPct(0)
                        UpdatePeakMemoryUsage()

                        LogMessage("ProcessFile: Call FindSimilarParentIons")
                        blnSuccess = parentIonProcessor.FindSimilarParentIons(scanList, objSpectraCache, mOptions, dataImporterBase, intSimilarParentIonUpdateCount)

                        If Not blnSuccess Then
                            SetLocalErrorCode(eMasicErrorCodes.FindSimilarParentIonsError, True)
                            Exit Try
                        End If
                    End If

                End If

                If mOptions.WriteExtendedStats AndAlso Not mOptions.ExportRawDataOnly Then
                    '---------------------------------------------------------
                    ' Save Extended Scan Stats Files
                    '---------------------------------------------------------

                    UpdateProcessingStep(eProcessingStepConstants.SaveExtendedScanStatsFiles)
                    SetSubtaskProcessingStepPct(0)
                    UpdatePeakMemoryUsage()

                    LogMessage("ProcessFile: Call SaveExtendedScanStatsFiles")
                    blnSuccess = dataOutputHandler.ExtendedStatsWriter.SaveExtendedScanStatsFiles(
                        scanList, strInputFileName, strOutputFolderPath, mOptions.IncludeHeadersInExportFile)

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

                If Not mOptions.ExportRawDataOnly Then

                    Dim sicStatsWriter = New DataOutput.clsSICStatsWriter()
                    RegisterEvents(sicStatsWriter)

                    LogMessage("ProcessFile: Call SaveSICStatsFlatFile")
                    blnSuccess = sicStatsWriter.SaveSICStatsFlatFile(scanList, strInputFileName, strOutputFolderPath, mOptions, dataOutputHandler)

                    If Not blnSuccess Then
                        SetLocalErrorCode(eMasicErrorCodes.OutputFileWriteError, True)
                        Exit Try
                    End If
                End If


                UpdateProcessingStep(eProcessingStepConstants.CloseOpenFileHandles)
                SetSubtaskProcessingStepPct(0)
                UpdatePeakMemoryUsage()

                If Not (mOptions.SkipSICAndRawDataProcessing OrElse mOptions.ExportRawDataOnly) Then

                    '---------------------------------------------------------
                    ' Write processing stats to the XML output file
                    '---------------------------------------------------------

                    LogMessage("ProcessFile: Call FinalizeXMLFile")
                    Dim processingTimeSec = GetTotalProcessingTimeSec()
                    blnSuccess = xmlResultsWriter.XMLOutputFileFinalize(dataOutputHandler, scanList, objSpectraCache, 
                                                                        mProcessingStats, processingTimeSec)

                End If

                '---------------------------------------------------------
                ' Close any open output files
                '---------------------------------------------------------
                dataOutputHandler.OutputFileHandles.CloseAll()

                '---------------------------------------------------------
                ' Save a text file containing the headers used in the text files
                '---------------------------------------------------------
                If Not mOptions.IncludeHeadersInExportFile Then
                    LogMessage("ProcessFile: Call SaveHeaderGlossary")
                    dataOutputHandler.SaveHeaderGlossary(scanList, strInputFileName, strOutputFolderPath)
                End If

                If Not (mOptions.SkipSICAndRawDataProcessing OrElse mOptions.ExportRawDataOnly) AndAlso intSimilarParentIonUpdateCount > 0 Then
                    '---------------------------------------------------------
                    ' Reopen the XML file and update the entries for those ions in scanList that had their
                    ' Optimal peak apex scan numbers updated
                    '---------------------------------------------------------

                    UpdateProcessingStep(eProcessingStepConstants.UpdateXMLFileWithNewOptimalPeakApexValues)
                    SetSubtaskProcessingStepPct(0)
                    UpdatePeakMemoryUsage()

                    LogMessage("ProcessFile: Call XmlOutputFileUpdateEntries")
                    xmlResultsWriter.XmlOutputFileUpdateEntries(scanList, strInputFileName, strOutputFolderPath)
                End If

            Catch ex As Exception
                blnSuccess = False
                LogErrors("ProcessFile", "Error creating or writing to the output file in folder: " & strOutputFolderPath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
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

            dataOutputHandler.OutputFileHandles.CloseAll()

            If mAbortProcessing AndAlso MyBase.ShowMessages Then
                MessageBox.Show("Cancelled processing", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information)
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
            mOptions.SICOptions.DatasetNumber += 1
        End If

        If blnSuccess Then
            UpdateProcessingStep(eProcessingStepConstants.Complete, True)
        Else
            UpdateProcessingStep(eProcessingStepConstants.Cancelled, True)
        End If

        Return blnSuccess

    End Function

    Private Sub RegisterDataImportEvents(dataImporter As DataInput.clsDataImport)
        RegisterEvents(dataImporter)
        AddHandler dataImporter.UpdateMemoryUsageEvent, AddressOf UpdateMemoryUsageEventHandler
    End Sub

    Private Sub RegisterEvents(oClass As clsEventNotifier)
        AddHandler oClass.MessageEvent, AddressOf MessageEventHandler
        AddHandler oClass.ErrorEvent, AddressOf ErrorEventHandler
        AddHandler oClass.WarningEvent, AddressOf WarningEventHandler
        AddHandler oClass.ProgressUpdate, AddressOf ProgressUpdateHandler
        AddHandler oClass.UpdateCacheStatsEvent, AddressOf UpdatedCacheStatsEventHandler
        AddHandler oClass.UpdateBaseClassErrorCodeEvent, AddressOf UpdateBaseClassErrorCodeEventHandler
        AddHandler oClass.UpdateErrorCodeEvent, AddressOf UpdateErrorCodeEventHandler
    End Sub

    Public Function SaveParameterFileSettings(strParameterFilePath As String) As Boolean
        Dim success = mOptions.SaveParameterFileSettings(strParameterFilePath)
        Return success
    End Function

    Public Sub SetReporterIons(dblReporterIonMZList() As Double)
        mOptions.ReporterIons.SetReporterIons(dblReporterIonMZList)
    End Sub

    Public Sub SetReporterIons(dblReporterIonMZList() As Double, dblMZToleranceDa As Double)
        mOptions.ReporterIons.SetReporterIons(dblReporterIonMZList, dblMZToleranceDa)
    End Sub

    Public Sub SetReporterIons(reporterIons As List(Of clsReporterIonInfo))
        mOptions.ReporterIons.SetReporterIons(reporterIons, True)
    End Sub

    Public Sub SetReporterIonMassMode(eReporterIonMassMode As clsReporterIons.eReporterIonMassModeConstants)
        mOptions.ReporterIons.SetReporterIonMassMode(eReporterIonMassMode)
    End Sub

    Public Sub SetReporterIonMassMode(
      eReporterIonMassMode As clsReporterIons.eReporterIonMassModeConstants,
      dblMZToleranceDa As Double)

        mOptions.ReporterIons.SetReporterIonMassMode(eReporterIonMassMode, dblMZToleranceDa)

    End Sub

    Private Sub SetDefaultPeakLocValues(scanList As clsScanList)

        Dim intParentIonIndex As Integer
        Dim intScanIndexObserved As Integer

        Try
            For intParentIonIndex = 0 To scanList.ParentIonInfoCount - 1
                With scanList.ParentIons(intParentIonIndex)
                    intScanIndexObserved = .SurveyScanIndex

                    With .SICStats
                        .ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.SurveyScan
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
                If MyBase.ErrorCode = eProcessFilesErrorCodes.LocalizedError Then
                    MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.NoError)
                End If
            Else
                MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.LocalizedError)
            End If
        End If

    End Sub

    ''' <summary>
    ''' Update subtask progress
    ''' </summary>
    ''' <param name="subtaskPercentComplete">Percent complete, between 0 and 100</param>
    Private Sub SetSubtaskProcessingStepPct(subtaskPercentComplete As Short)
        SetSubtaskProcessingStepPct(subtaskPercentComplete, False)
    End Sub

    ''' <summary>
    '''  Update subtask progress
    ''' </summary>
    ''' <param name="subtaskPercentComplete">Percent complete, between 0 and 100</param>
    ''' <param name="forceUpdate"></param>
    Private Sub SetSubtaskProcessingStepPct(subtaskPercentComplete As Short, forceUpdate As Boolean)
        Const MINIMUM_PROGRESS_UPDATE_INTERVAL_MILLISECONDS = 250

        Dim blnRaiseEvent As Boolean
        Static LastFileWriteTime As DateTime = DateTime.UtcNow

        If subtaskPercentComplete = 0 Then
            mAbortProcessing = False
            RaiseEvent ProgressResetKeypressAbort()
            blnRaiseEvent = True
        End If

        If subtaskPercentComplete <> mSubtaskProcessingStepPct Then
            blnRaiseEvent = True
            mSubtaskProcessingStepPct = subtaskPercentComplete
        End If

        If forceUpdate OrElse blnRaiseEvent OrElse 
            DateTime.UtcNow.Subtract(LastFileWriteTime).TotalMilliseconds >= MINIMUM_PROGRESS_UPDATE_INTERVAL_MILLISECONDS Then
            LastFileWriteTime = DateTime.UtcNow

            UpdateOverallProgress()
            UpdateStatusFile()
            RaiseEvent ProgressSubtaskChanged()
        End If
    End Sub

    ''' <summary>
    ''' Update subtask progress and description
    ''' </summary>
    ''' <param name="subtaskPercentComplete">Percent complete, between 0 and 100</param>
    ''' <param name="strSubtaskDescription"></param>
    Private Sub SetSubtaskProcessingStepPct(subtaskPercentComplete As Short, strSubtaskDescription As String)
        mSubtaskDescription = strSubtaskDescription
        SetSubtaskProcessingStepPct(subtaskPercentComplete, True)
    End Sub

    Private Sub UpdateOverallProgress()
        UpdateOverallProgress(MyBase.mProgressStepDescription)
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

        If mOptions.SkipMSMSProcessing Then
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
            LogErrors("UpdateOverallProgress", "Bug in UpdateOverallProgress", ex)
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
                strTempPath = Path.Combine(GetAppFolderPath(), "Temp_" & mOptions.MASICStatusFilename)
                strPath = Path.Combine(GetAppFolderPath(), mOptions.MASICStatusFilename)

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

    Protected Overrides Sub Finalize()
        If Not mFreeMemoryPerformanceCounter Is Nothing Then
            mFreeMemoryPerformanceCounter.Close()
            mFreeMemoryPerformanceCounter = Nothing
        End If

        MyBase.Finalize()
    End Sub

#Region "Event Handlers"

    Private Sub MessageEventHandler(message As String)
        LogMessage(message)
    End Sub

    Private Sub ErrorEventHandler(
      source As String,
      message As String,
      ex As Exception,
      allowInformUser As Boolean,
      allowThrowException As Boolean,
      eNewErrorCode As eMasicErrorCodes)
        LogErrors(source, message, ex, allowInformUser, allowThrowException, eNewErrorCode)
    End Sub

    Private Sub WarningEventHandler(source As String, message As String)
        LogMessage(message, eMessageTypeConstants.Warning)
    End Sub

    Private Sub ProgressUpdateHandler(percentComplete As Short, progressMessage As String)
        If String.IsNullOrEmpty(progressMessage) Then
            SetSubtaskProcessingStepPct(percentComplete)
        Else
            SetSubtaskProcessingStepPct(percentComplete, progressMessage)
        End If

    End Sub

    Private Sub UpdatedCacheStatsEventHandler(cacheEventCount As Integer, unCacheEventCount As Integer)
        mProcessingStats.CacheEventCount = cacheEventCount
        mProcessingStats.UnCacheEventCount = unCacheEventCount
    End Sub

    Private Sub UpdateBaseClassErrorCodeEventHandler(eErrorCode As eProcessFilesErrorCodes)
        SetBaseClassErrorCode(eErrorCode)
    End Sub

    Private Sub UpdateErrorCodeEventHandler(eErrorCode As eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
        SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub

    Private Sub UpdateMemoryUsageEventHandler()
        ' Record the current memory usage
        Dim memoryUsageMB = GetProcessMemoryUsageMB()
        If memoryUsageMB > mProcessingStats.MemoryUsageMBDuringLoad Then
            mProcessingStats.MemoryUsageMBDuringLoad = memoryUsageMB
        End If

    End Sub

#End Region

End Class
