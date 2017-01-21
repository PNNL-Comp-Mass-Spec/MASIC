Imports MASIC.clsMASIC

Public Class clsMASICOptions
    Inherits clsEventNotifier

#Region "Constants and Enums"
    Public Const XML_SECTION_DATABASE_SETTINGS As String = "MasicDatabaseSettings"
    Public Const XML_SECTION_IMPORT_OPTIONS As String = "MasicImportOptions"
    Public Const XML_SECTION_EXPORT_OPTIONS As String = "MasicExportOptions"
    Public Const XML_SECTION_SIC_OPTIONS As String = "SICOptions"
    Public Const XML_SECTION_BINNING_OPTIONS As String = "BinningOptions"
    Public Const XML_SECTION_MEMORY_OPTIONS As String = "MemoryOptions"
    Public Const XML_SECTION_CUSTOM_SIC_VALUES As String = "CustomSICValues"

    Public Const DEFAULT_MASIC_STATUS_FILE_NAME As String = "MasicStatus.xml"
#End Region

#Region "Classwide Variables"

    ''' <summary>
    ''' Set options through the Property Functions or by passing strParameterFilePath to ProcessFile()
    ''' </summary>
    Public ReadOnly Property SICOptions As clsSICOptions

    ''' <summary>
    ''' Binning options for MS/MS spectra; only applies to spectrum similarity testing
    ''' </summary>
    Public ReadOnly Property BinningOptions As clsBinningOptions

    Public ReadOnly Property CustomSICList As New clsCustomSICList()

    Public Property AbortProcessing As Boolean

    Public Property DatabaseConnectionString As String
    Public Property DatasetInfoQuerySql As String
    Public Property DatasetLookupFilePath As String = String.Empty

    Public Property IncludeHeadersInExportFile As Boolean
    Public Property IncludeScanTimesInSICStatsFile As Boolean
    Public Property FastExistingXMLFileTest As Boolean

    ''' <summary>
    ''' Using this will reduce memory usage, but not as much as when mSkipSICAndRawDataProcessing = True
    ''' </summary>
    Public Property SkipMSMSProcessing As Boolean

    ''' <summary>
    ''' Using this will drastically reduce memory usage since raw mass spec data is not retained
    ''' </summary>
    Public Property SkipSICAndRawDataProcessing As Boolean

    ''' <summary>
    ''' When True, then will not create any SICs; automatically set to false if mSkipSICAndRawDataProcessing = True
    ''' </summary>
    Public Property ExportRawDataOnly As Boolean

    Public Property OutputFolderPath As String

    Public Property WriteDetailedSICDataFile As Boolean
    Public Property WriteMSMethodFile As Boolean
    Public Property WriteMSTuneFile As Boolean

    Public Property WriteExtendedStats As Boolean

    ''' <summary>
    ''' When enabled, the the scan filter text will also be included in the extended stats file 
    ''' (e.g. ITMS + c NSI Full ms [300.00-2000.00] or ITMS + c NSI d Full ms2 756.98@35.00 [195.00-2000.00])
    ''' </summary>
    Public Property WriteExtendedStatsIncludeScanFilterText As Boolean

    ''' <summary>
    ''' Adds a large number of additional columns with information like voltage, current, temperature, pressure, and gas flow rate
    ''' If StatusLogKeyNameFilterList contains any entries, then only the entries matching the specs in StatusLogKeyNameFilterList will be saved
    ''' </summary>
    Public Property WriteExtendedStatsStatusLog As Boolean

    Public Property ConsolidateConstantExtendedHeaderValues As Boolean

    Public Property WriteMRMDataList As Boolean
    Public Property WriteMRMIntensityCrosstab As Boolean

    ''' <summary>
    ''' If this is true, then an error will not be raised if the input file contains no parent ions or no survey scans
    ''' </summary>
    Public Property SuppressNoParentIonsError As Boolean

    Public ReadOnly Property RawDataExportOptions As clsRawDataExportOptions

    Public ReadOnly Property ReporterIons As clsReporterIons

    Public Property CDFTimeInSeconds As Boolean
    Public Property ParentIonDecoyMassDa As Double

    Public Property UseBase64DataEncoding As Boolean

    Public ReadOnly Property CacheOptions As clsSpectrumCacheOptions
    Public Property MASICStatusFilename As String = DEFAULT_MASIC_STATUS_FILE_NAME

    Public ReadOnly Property MASICVersion As String
    Public ReadOnly Property PeakFinderVersion As String

    Public Property LastParentIonProcessingLogTime As DateTime

#End Region

#Region "Properties"

    Public Property StatusMessage As String
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(strMasicVersion As String, strPeakFinderVersion As String)

        MASICVersion = strMasicVersion
        PeakFinderVersion = strPeakFinderVersion

        CacheOptions = New clsSpectrumCacheOptions()

        CustomSICList = New clsCustomSICList()
        RegisterEvents(CustomSICList)

        RawDataExportOptions = New clsRawDataExportOptions()

        ReporterIons = New clsReporterIons()

        BinningOptions = New clsBinningOptions()

        SICOptions = New clsSICOptions()

        StatusLogKeyNameFilterList = New SortedSet(Of String)
    End Sub

    Public Function GetScanToleranceTypeFromText(strScanType As String) As clsCustomSICList.eCustomSICScanTypeConstants

        If strScanType Is Nothing Then strScanType = String.Empty
        Dim strScanTypeTrimmed = strScanType.Trim()

        If String.Equals(strScanTypeTrimmed, clsCustomSICList.CUSTOM_SIC_TYPE_RELATIVE, StringComparison.InvariantCultureIgnoreCase) Then
            Return clsCustomSICList.eCustomSICScanTypeConstants.Relative

        ElseIf String.Equals(strScanTypeTrimmed, clsCustomSICList.CUSTOM_SIC_TYPE_ACQUISITION_TIME, StringComparison.InvariantCultureIgnoreCase) Then
            Return clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
        Else
            ' Assume absolute
            Return clsCustomSICList.eCustomSICScanTypeConstants.Absolute
        End If

    End Function

    Public ReadOnly Property StatusLogKeyNameFilterList() As SortedSet(Of String)

    ''' <summary>
    ''' Returns the contents of StatusLogKeyNameFilterList
    ''' </summary>
    ''' <param name="blnCommaSeparatedList">When true, then returns a comma separated list; when false, returns separates items with CrLf</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GetStatusLogKeyNameFilterListAsText(blnCommaSeparatedList As Boolean) As String

        If blnCommaSeparatedList Then
            Return String.Join(ControlChars.NewLine, StatusLogKeyNameFilterList)
        Else
            Return String.Join(", ", StatusLogKeyNameFilterList)
        End If

    End Function

    Public Sub InitializeVariables()

        AbortProcessing = False

        DatasetLookupFilePath = String.Empty
        DatabaseConnectionString = String.Empty
        DatasetInfoQuerySql = String.Empty

        IncludeHeadersInExportFile = True
        IncludeScanTimesInSICStatsFile = False
        FastExistingXMLFileTest = False

        SkipMSMSProcessing = False
        SkipSICAndRawDataProcessing = False
        ExportRawDataOnly = False

        SuppressNoParentIonsError = False

        WriteMSMethodFile = True
        WriteMSTuneFile = False

        WriteDetailedSICDataFile = False
        WriteExtendedStats = True
        WriteExtendedStatsIncludeScanFilterText = True
        WriteExtendedStatsStatusLog = True
        ConsolidateConstantExtendedHeaderValues = True

        SetStatusLogKeyNameFilterList("Source", ","c)

        WriteMRMDataList = False
        WriteMRMIntensityCrosstab = True

        RawDataExportOptions.Reset()

        CDFTimeInSeconds = True
        ParentIonDecoyMassDa = 0

        ' Enabling this gives files of nearly equivalent size, but with the data arrays base-64 encoded; thus, no advantage
        UseBase64DataEncoding = False

        SICOptions.Reset()

        BinningOptions.Reset()

        CacheOptions.Reset()

        CustomSICList.Reset()

    End Sub

    Public Function LoadParameterFileSettings(strParameterFilePath As String) As Boolean

        'Dim objSettingsFile As New XmlSettingsFileAccessor

        'Dim strMZList As String
        'Dim strMZToleranceDaList As String

        'Dim strScanCenterList As String
        'Dim strScanToleranceList As String

        'Dim strScanCommentList As String
        'Dim strScanTolerance As String
        'Dim strScanType As String
        'Dim strFilterList As String

        'Dim eReporterIonMassMode As clsReporterIons.eReporterIonMassModeConstants
        'Dim eReporterIonITraq4PlexCorrectionFactorType As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex

        'Dim dblSICTolerance As Double
        'Dim blnSICToleranceIsPPM As Boolean

        'Dim strErrorMessage As String
        'Dim blnNotPresent As Boolean

        'Dim blnSuccess As Boolean

        Try

            If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
                ' No parameter file specified; nothing to load
                ReportMessage("Parameter file not specified -- will use default settings")
                Return True
            Else
                ReportMessage("Loading parameter file: " & strParameterFilePath)
            End If


            If Not File.Exists(strParameterFilePath) Then
                ' See if strParameterFilePath points to a file in the same directory as the application
                strParameterFilePath = Path.Combine(GetAppFolderPath(), Path.GetFileName(strParameterFilePath))
                If Not File.Exists(strParameterFilePath) Then
                    ReportError("LoadParameterFileSettings", "Parameter file not found: " & strParameterFilePath, Nothing, True, False)
                    Return False
                End If
            End If

            Dim objSettingsFile = New XmlSettingsFileAccessor()

            ' Pass False to .LoadSettings() here to turn off case sensitive matching
            If Not objSettingsFile.LoadSettings(strParameterFilePath, False) Then
                ReportError("LoadParameterFileSettings",
                            "Error calling objSettingsFile.LoadSettings for " & strParameterFilePath, Nothing, True,
                            False, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End If

            With objSettingsFile

                If Not .SectionPresent(XML_SECTION_DATABASE_SETTINGS) Then
                    ' Database settings section not found; that's ok
                Else
                    DatabaseConnectionString = .GetParam(XML_SECTION_DATABASE_SETTINGS, "ConnectionString",
                                                         DatabaseConnectionString)
                    DatasetInfoQuerySql = .GetParam(XML_SECTION_DATABASE_SETTINGS, "DatasetInfoQuerySql",
                                                    DatasetInfoQuerySql)
                End If

                If Not .SectionPresent(XML_SECTION_IMPORT_OPTIONS) Then
                    ' Import options section not found; that's ok
                Else
                    CDFTimeInSeconds = .GetParam(XML_SECTION_IMPORT_OPTIONS, "CDFTimeInSeconds", CDFTimeInSeconds)
                    ParentIonDecoyMassDa = .GetParam(XML_SECTION_IMPORT_OPTIONS, "ParentIonDecoyMassDa",
                                                     ParentIonDecoyMassDa)
                End If

                ' Masic Export Options
                If Not .SectionPresent(XML_SECTION_EXPORT_OPTIONS) Then
                    ' Export options section not found; that's ok
                Else
                    IncludeHeadersInExportFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "IncludeHeaders",
                                                           IncludeHeadersInExportFile)

                    IncludeScanTimesInSICStatsFile = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                               "IncludeScanTimesInSICStatsFile",
                                                               IncludeScanTimesInSICStatsFile)

                    SkipMSMSProcessing = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SkipMSMSProcessing",
                                                   SkipMSMSProcessing)

                    ' Check for both "SkipSICProcessing" and "SkipSICAndRawDataProcessing" in the XML file
                    ' If either is true, then mExportRawDataOnly will be auto-set to false in function ProcessFiles
                    SkipSICAndRawDataProcessing = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SkipSICProcessing",
                                                            SkipSICAndRawDataProcessing)

                    SkipSICAndRawDataProcessing = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                            "SkipSICAndRawDataProcessing",
                                                            SkipSICAndRawDataProcessing)

                    ExportRawDataOnly = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataOnly", ExportRawDataOnly)

                    SuppressNoParentIonsError = .GetParam(XML_SECTION_EXPORT_OPTIONS, "SuppressNoParentIonsError",
                                                          SuppressNoParentIonsError)

                    WriteDetailedSICDataFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteDetailedSICDataFile",
                                                         WriteDetailedSICDataFile)

                    WriteMSMethodFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSMethodFile", WriteMSMethodFile)

                    WriteMSTuneFile = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMSTuneFile", WriteMSTuneFile)

                    WriteExtendedStats = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteExtendedStats",
                                                   WriteExtendedStats)

                    WriteExtendedStatsIncludeScanFilterText = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                        "WriteExtendedStatsIncludeScanFilterText",
                                                                        WriteExtendedStatsIncludeScanFilterText)

                    WriteExtendedStatsStatusLog = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                            "WriteExtendedStatsStatusLog",
                                                            WriteExtendedStatsStatusLog)

                    Dim strFilterList = .GetParam(XML_SECTION_EXPORT_OPTIONS, "StatusLogKeyNameFilterList",
                                                  String.Empty)
                    If Not strFilterList Is Nothing AndAlso strFilterList.Length > 0 Then
                        SetStatusLogKeyNameFilterList(strFilterList, ","c)
                    End If

                    ConsolidateConstantExtendedHeaderValues = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                        "ConsolidateConstantExtendedHeaderValues",
                                                                        ConsolidateConstantExtendedHeaderValues)

                    WriteMRMDataList = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMDataList", WriteMRMDataList)

                    WriteMRMIntensityCrosstab = .GetParam(XML_SECTION_EXPORT_OPTIONS, "WriteMRMIntensityCrosstab",
                                                          WriteMRMIntensityCrosstab)

                    FastExistingXMLFileTest = .GetParam(XML_SECTION_EXPORT_OPTIONS, "FastExistingXMLFileTest",
                                                        FastExistingXMLFileTest)

                    ReporterIons.ReporterIonStatsEnabled = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                     "ReporterIonStatsEnabled",
                                                                     ReporterIons.ReporterIonStatsEnabled)

                    Dim eReporterIonMassMode = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonMassMode",
                                                               CInt(ReporterIons.ReporterIonMassMode)),
                                                     clsReporterIons.eReporterIonMassModeConstants)

                    ReporterIons.ReporterIonToleranceDaDefault = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                           "ReporterIonToleranceDa",
                                                                           ReporterIons.ReporterIonToleranceDaDefault)

                    ReporterIons.ReporterIonApplyAbundanceCorrection = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                                 "ReporterIonApplyAbundanceCorrection",
                                                                                 ReporterIons.ReporterIonApplyAbundanceCorrection)

                    Dim eReporterIonITraq4PlexCorrectionFactorType = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                                     "ReporterIonITraq4PlexCorrectionFactorType",
                                                                                     CInt(ReporterIons.ReporterIonITraq4PlexCorrectionFactorType)),
                                                                           clsITraqIntensityCorrection.
                            eCorrectionFactorsiTRAQ4Plex)

                    ReporterIons.ReporterIonITraq4PlexCorrectionFactorType =
                        eReporterIonITraq4PlexCorrectionFactorType

                    ReporterIons.ReporterIonSaveObservedMasses = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                           "ReporterIonSaveObservedMasses",
                                                                           ReporterIons.ReporterIonSaveObservedMasses)

                    ReporterIons.ReporterIonSaveUncorrectedIntensities = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                                   "ReporterIonSaveUncorrectedIntensities",
                                                                                   ReporterIons.ReporterIonSaveUncorrectedIntensities)

                    ReporterIons.SetReporterIonMassMode(eReporterIonMassMode,
                                                        ReporterIons.ReporterIonToleranceDaDefault)

                    ' Raw data export options
                    RawDataExportOptions.ExportEnabled = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                   "ExportRawSpectraData",
                                                                   RawDataExportOptions.ExportEnabled)

                    RawDataExportOptions.FileFormat = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                      "ExportRawDataFileFormat",
                                                                      CInt(RawDataExportOptions.FileFormat)),
                                                            clsRawDataExportOptions.
                                                                eExportRawDataFileFormatConstants)

                    RawDataExportOptions.IncludeMSMS = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                 "ExportRawDataIncludeMSMS",
                                                                 RawDataExportOptions.IncludeMSMS)

                    RawDataExportOptions.RenumberScans = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                   "ExportRawDataRenumberScans",
                                                                   RawDataExportOptions.RenumberScans)

                    RawDataExportOptions.MinimumSignalToNoiseRatio = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                               "ExportRawDataMinimumSignalToNoiseRatio",
                                                                               RawDataExportOptions.MinimumSignalToNoiseRatio)

                    RawDataExportOptions.MaxIonCountPerScan = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                        "ExportRawDataMaxIonCountPerScan",
                                                                        RawDataExportOptions.MaxIonCountPerScan)

                    RawDataExportOptions.IntensityMinimum = .GetParam(XML_SECTION_EXPORT_OPTIONS,
                                                                      "ExportRawDataIntensityMinimum",
                                                                      RawDataExportOptions.IntensityMinimum)
                End If

                If Not .SectionPresent(XML_SECTION_SIC_OPTIONS) Then
                    Dim strErrorMessage = "The node '<section name=" & ControlChars.Quote & XML_SECTION_SIC_OPTIONS &
                                          ControlChars.Quote & "> was not found in the parameter file: " &
                                          strParameterFilePath
                    ReportError("LoadParameterFileSettings", strErrorMessage)
                    Return False
                Else
                    ' SIC Options
                    ' Note: Skipping .DatasetNumber since this must be provided at the command line or through the Property Function interface

                    Dim blnNotPresent As Boolean

                    ' Preferentially use "SICTolerance", if it is present
                    Dim dblSICTolerance = .GetParam(XML_SECTION_SIC_OPTIONS, "SICTolerance",
                                                    SICOptions.GetSICTolerance(), blnNotPresent)

                    If blnNotPresent Then
                        ' Check for "SICToleranceDa", which is a legacy setting
                        dblSICTolerance = .GetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceDa",
                                                    SICOptions.SICToleranceDa, blnNotPresent)

                        If Not blnNotPresent Then
                            SICOptions.SetSICTolerance(dblSICTolerance, False)
                        End If
                    Else
                        Dim blnSICToleranceIsPPM = .GetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceIsPPM", False)

                        SICOptions.SetSICTolerance(dblSICTolerance, blnSICToleranceIsPPM)
                    End If

                    SICOptions.RefineReportedParentIonMZ = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                     "RefineReportedParentIonMZ",
                                                                     SICOptions.RefineReportedParentIonMZ)

                    SICOptions.ScanRangeStart = .GetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeStart",
                                                          SICOptions.ScanRangeStart)

                    SICOptions.ScanRangeEnd = .GetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeEnd",
                                                        SICOptions.ScanRangeEnd)

                    SICOptions.RTRangeStart = .GetParam(XML_SECTION_SIC_OPTIONS, "RTRangeStart",
                                                        SICOptions.RTRangeStart)

                    SICOptions.RTRangeEnd = .GetParam(XML_SECTION_SIC_OPTIONS, "RTRangeEnd", SICOptions.RTRangeEnd)

                    SICOptions.CompressMSSpectraData = .GetParam(XML_SECTION_SIC_OPTIONS, "CompressMSSpectraData",
                                                                 SICOptions.CompressMSSpectraData)

                    SICOptions.CompressMSMSSpectraData = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                   "CompressMSMSSpectraData",
                                                                   SICOptions.CompressMSMSSpectraData)

                    SICOptions.CompressToleranceDivisorForDa = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                         "CompressToleranceDivisorForDa",
                                                                         SICOptions.CompressToleranceDivisorForDa)

                    SICOptions.CompressToleranceDivisorForPPM = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                          "CompressToleranceDivisorForPPM",
                                                                          SICOptions.CompressToleranceDivisorForPPM)

                    SICOptions.MaxSICPeakWidthMinutesBackward = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                          "MaxSICPeakWidthMinutesBackward",
                                                                          SICOptions.MaxSICPeakWidthMinutesBackward)

                    SICOptions.MaxSICPeakWidthMinutesForward = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                         "MaxSICPeakWidthMinutesForward",
                                                                         SICOptions.MaxSICPeakWidthMinutesForward)

                    SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdFractionMax",
                                  SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax)

                    SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdAbsoluteMinimum",
                                  SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum)

                    ' Peak Finding Options
                    SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode =
                        CType(.GetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdMode",
                                        CInt(
                                            SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.
                                           BaselineNoiseMode)),
                              MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)

                    SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdIntensity",
                                  SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute)

                    SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage =
                        .GetParam(XML_SECTION_SIC_OPTIONS,
                                  "SICNoiseFractionLowIntensityDataToAverage",
                                  SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage)

                    ' This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                    SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0

                    SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "MaxDistanceScansNoOverlap",
                                  SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap)

                    SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "MaxAllowedUpwardSpikeFractionMax",
                                  SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax)

                    SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansScaler",
                                  SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler)

                    SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansMaximum",
                                  SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)

                    SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "FindPeaksOnSmoothedData",
                                  SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData)

                    SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "SmoothDataRegardlessOfMinimumPeakWidth",
                                  SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth)

                    SICOptions.SICPeakFinderOptions.UseButterworthSmooth =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "UseButterworthSmooth",
                                  SICOptions.SICPeakFinderOptions.UseButterworthSmooth)

                    SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequency",
                                  SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency)

                    SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequencyDoubledForSIMData",
                                  SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData)

                    SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "UseSavitzkyGolaySmooth",
                                  SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth)

                    SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "SavitzkyGolayFilterOrder",
                                  SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder)

                    SICOptions.SaveSmoothedData = .GetParam(XML_SECTION_SIC_OPTIONS, "SaveSmoothedData",
                                                            SICOptions.SaveSmoothedData)

                    SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode =
                        CType(.GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdMode",
                                        CInt(SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode)),
                              MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)

                    SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdIntensity",
                                  SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.
                                     BaselineNoiseLevelAbsolute)

                    SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.
                        TrimmedMeanFractionLowIntensityDataToAverage =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseFractionLowIntensityDataToAverage",
                                  SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage)

                    SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseMinimumSignalToNoiseRatio ",
                                  SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio)

                    SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData =
                        .GetParam(XML_SECTION_SIC_OPTIONS, "ReplaceSICZeroesWithMinimumPositiveValueFromMSData",
                                  SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData)

                    ' Similarity Options
                    SICOptions.SimilarIonMZToleranceHalfWidth = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                          "SimilarIonMZToleranceHalfWidth",
                                                                          SICOptions.SimilarIonMZToleranceHalfWidth)

                    SICOptions.SimilarIonToleranceHalfWidthMinutes = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                               "SimilarIonToleranceHalfWidthMinutes",
                                                                               SICOptions.SimilarIonToleranceHalfWidthMinutes)

                    SICOptions.SpectrumSimilarityMinimum = .GetParam(XML_SECTION_SIC_OPTIONS,
                                                                     "SpectrumSimilarityMinimum",
                                                                     SICOptions.SpectrumSimilarityMinimum)

                End If

                ' Binning Options
                If Not .SectionPresent(XML_SECTION_BINNING_OPTIONS) Then
                    Dim strErrorMessage = "The node '<section name=" & ControlChars.Quote &
                                          XML_SECTION_BINNING_OPTIONS & ControlChars.Quote &
                                          "> was not found in the parameter file: " & strParameterFilePath
                    ReportError("LoadParameterFileSettings", strErrorMessage)

                    SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidParameterFile)
                    Return False
                Else
                    BinningOptions.StartX = .GetParam(XML_SECTION_BINNING_OPTIONS, "BinStartX",
                                                      BinningOptions.StartX)
                    BinningOptions.EndX = .GetParam(XML_SECTION_BINNING_OPTIONS, "BinEndX", BinningOptions.EndX)
                    BinningOptions.BinSize = .GetParam(XML_SECTION_BINNING_OPTIONS, "BinSize",
                                                       BinningOptions.BinSize)

                    BinningOptions.MaximumBinCount = .GetParam(XML_SECTION_BINNING_OPTIONS, "MaximumBinCount",
                                                               BinningOptions.MaximumBinCount)

                    BinningOptions.IntensityPrecisionPercent = .GetParam(XML_SECTION_BINNING_OPTIONS,
                                                                         "IntensityPrecisionPercent",
                                                                         BinningOptions.IntensityPrecisionPercent)
                    BinningOptions.Normalize = .GetParam(XML_SECTION_BINNING_OPTIONS, "Normalize",
                                                         BinningOptions.Normalize)
                    BinningOptions.SumAllIntensitiesForBin = .GetParam(XML_SECTION_BINNING_OPTIONS,
                                                                       "SumAllIntensitiesForBin",
                                                                       BinningOptions.SumAllIntensitiesForBin)
                End If

                ' Memory management options
                CacheOptions.DiskCachingAlwaysDisabled = .GetParam(XML_SECTION_MEMORY_OPTIONS,
                                                                   "DiskCachingAlwaysDisabled",
                                                                   CacheOptions.DiskCachingAlwaysDisabled)

                CacheOptions.FolderPath = .GetParam(XML_SECTION_MEMORY_OPTIONS, "CacheFolderPath",
                                                    CacheOptions.FolderPath)

                CacheOptions.SpectraToRetainInMemory = .GetParam(XML_SECTION_MEMORY_OPTIONS,
                                                                 "CacheSpectraToRetainInMemory",
                                                                 CacheOptions.SpectraToRetainInMemory)

            End With

            If Not objSettingsFile.SectionPresent(XML_SECTION_CUSTOM_SIC_VALUES) Then
                ' Custom SIC values section not found; that's ok
                ' No more settings to load so return true
                Return True
            End If

            CustomSICList.LimitSearchToCustomMZList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES,
                                                                               "LimitSearchToCustomMZList",
                                                                               CustomSICList.LimitSearchToCustomMZList)

            Dim strScanType = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", String.Empty)
            Dim strScanTolerance = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", String.Empty)

            With CustomSICList
                .ScanToleranceType = GetScanToleranceTypeFromText(strScanType)

                If strScanTolerance.Length > 0 AndAlso clsUtilities.IsNumber(strScanTolerance) Then
                    If .ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute Then
                        .ScanOrAcqTimeTolerance = CInt(strScanTolerance)
                    Else
                        ' Includes .Relative and .AcquisitionTime
                        .ScanOrAcqTimeTolerance = CSng(strScanTolerance)
                    End If
                Else
                    .ScanOrAcqTimeTolerance = 0
                End If
            End With

            CustomSICList.CustomSICListFileName = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES,
                                                                           "CustomMZFile", String.Empty)

            If CustomSICList.CustomSICListFileName.Length > 0 Then
                ' Clear mCustomSICList; we'll read the data from the file when ProcessFile is called()

                CustomSICList.ResetMzSearchValues()

                Return True
            Else
                Dim strMZList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZList", String.Empty)
                Dim strMZToleranceDaList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES,
                                                                    "MZToleranceDaList", String.Empty)

                Dim strScanCenterList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList",
                                                                 String.Empty)
                Dim strScanToleranceList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES,
                                                                    "ScanToleranceList", String.Empty)

                Dim strScanCommentList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES,
                                                                  "ScanCommentList", String.Empty)

                Dim blnSuccess = CustomSICList.ParseCustomSICList(strMZList, strMZToleranceDaList,
                                                                  strScanCenterList, strScanToleranceList,
                                                                  strScanCommentList)

                Return blnSuccess
            End If

        Catch ex As Exception
            ReportError("LoadParameterFileSettings", "Error in LoadParameterFileSettings", ex, True, False, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

    End Function

    Public Function SaveParameterFileSettings(strParameterFilePath As String) As Boolean

        Dim objSettingsFile As New XmlSettingsFileAccessor

        Try

            If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
                ' No parameter file specified; unable to save
                ReportError("SaveParameterFileSettings", "Empty parameter file path sent to SaveParameterFileSettings")
                Return False
            End If

            ' Pass True to .LoadSettings() here so that newly made Xml files will have the correct capitalization
            If Not objSettingsFile.LoadSettings(strParameterFilePath, True) Then
                ReportError("SaveParameterFileSettings", "LoadSettings returned false while initializing " & strParameterFilePath)
                Return False
            End If

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

                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonStatsEnabled", ReporterIons.ReporterIonStatsEnabled)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonMassMode", CInt(ReporterIons.ReporterIonMassMode))
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonToleranceDa", ReporterIons.ReporterIonToleranceDaDefault)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonApplyAbundanceCorrection", ReporterIons.ReporterIonApplyAbundanceCorrection)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonITraq4PlexCorrectionFactorType", ReporterIons.ReporterIonITraq4PlexCorrectionFactorType)

                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveObservedMasses", ReporterIons.ReporterIonSaveObservedMasses)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ReporterIonSaveUncorrectedIntensities", ReporterIons.ReporterIonSaveUncorrectedIntensities)

                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawSpectraData", RawDataExportOptions.ExportEnabled)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataFileFormat", RawDataExportOptions.FileFormat)

                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIncludeMSMS", RawDataExportOptions.IncludeMSMS)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataRenumberScans", RawDataExportOptions.RenumberScans)

                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMinimumSignalToNoiseRatio", RawDataExportOptions.MinimumSignalToNoiseRatio)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMaxIonCountPerScan", RawDataExportOptions.MaxIonCountPerScan)
                .SetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIntensityMinimum", RawDataExportOptions.IntensityMinimum)


                ' SIC Options
                ' Note: Skipping .DatasetNumber since this must be provided at the command line or through the Property Function interface

                ' "SICToleranceDa" is a legacy parameter.  If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                .SetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceDa", SICOptions.SICToleranceDa.ToString("0.0000"))

                Dim blnSICToleranceIsPPM As Boolean
                Dim dblSICTolerance = SICOptions.GetSICTolerance(blnSICToleranceIsPPM)
                .SetParam(XML_SECTION_SIC_OPTIONS, "SICTolerance", dblSICTolerance.ToString("0.0000"))
                .SetParam(XML_SECTION_SIC_OPTIONS, "SICToleranceIsPPM", blnSICToleranceIsPPM.ToString)

                .SetParam(XML_SECTION_SIC_OPTIONS, "RefineReportedParentIonMZ", SICOptions.RefineReportedParentIonMZ)
                .SetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeStart", SICOptions.ScanRangeStart)
                .SetParam(XML_SECTION_SIC_OPTIONS, "ScanRangeEnd", SICOptions.ScanRangeEnd)
                .SetParam(XML_SECTION_SIC_OPTIONS, "RTRangeStart", SICOptions.RTRangeStart)
                .SetParam(XML_SECTION_SIC_OPTIONS, "RTRangeEnd", SICOptions.RTRangeEnd)

                .SetParam(XML_SECTION_SIC_OPTIONS, "CompressMSSpectraData", SICOptions.CompressMSSpectraData)
                .SetParam(XML_SECTION_SIC_OPTIONS, "CompressMSMSSpectraData", SICOptions.CompressMSMSSpectraData)

                .SetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForDa", SICOptions.CompressToleranceDivisorForDa)
                .SetParam(XML_SECTION_SIC_OPTIONS, "CompressToleranceDivisorForPPM", SICOptions.CompressToleranceDivisorForPPM)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesBackward", SICOptions.MaxSICPeakWidthMinutesBackward)
                .SetParam(XML_SECTION_SIC_OPTIONS, "MaxSICPeakWidthMinutesForward", SICOptions.MaxSICPeakWidthMinutesForward)
                .SetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdFractionMax", SICOptions.SICPeakFinderOptions.IntensityThresholdFractionMax)
                .SetParam(XML_SECTION_SIC_OPTIONS, "IntensityThresholdAbsoluteMinimum", SICOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum)

                ' Peak Finding Options
                .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdMode", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseThresholdIntensity", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseFractionLowIntensityDataToAverage", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage)

                ' This value isn't utilized by MASIC for SICs so we'll force it to always be zero
                SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = 0

                .SetParam(XML_SECTION_SIC_OPTIONS, "SICNoiseMinimumSignalToNoiseRatio", SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MaxDistanceScansNoOverlap", SICOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MaxAllowedUpwardSpikeFractionMax", SICOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax)

                .SetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansScaler", SICOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler)

                .SetParam(XML_SECTION_SIC_OPTIONS, "InitialPeakWidthScansMaximum", SICOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum)

                .SetParam(XML_SECTION_SIC_OPTIONS, "FindPeaksOnSmoothedData", SICOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SmoothDataRegardlessOfMinimumPeakWidth", SICOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth)

                .SetParam(XML_SECTION_SIC_OPTIONS, "UseButterworthSmooth", SICOptions.SICPeakFinderOptions.UseButterworthSmooth)

                .SetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequency", SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequency)

                .SetParam(XML_SECTION_SIC_OPTIONS, "ButterworthSamplingFrequencyDoubledForSIMData", SICOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData)

                .SetParam(XML_SECTION_SIC_OPTIONS, "UseSavitzkyGolaySmooth", SICOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SavitzkyGolayFilterOrder", SICOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SaveSmoothedData", SICOptions.SaveSmoothedData)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdMode", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseThresholdIntensity", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseFractionLowIntensityDataToAverage", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage)

                .SetParam(XML_SECTION_SIC_OPTIONS, "MassSpectraNoiseMinimumSignalToNoiseRatio", SICOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio)

                .SetParam(XML_SECTION_SIC_OPTIONS, "ReplaceSICZeroesWithMinimumPositiveValueFromMSData", SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData)

                ' Similarity Options
                .SetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonMZToleranceHalfWidth", SICOptions.SimilarIonMZToleranceHalfWidth)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SimilarIonToleranceHalfWidthMinutes", SICOptions.SimilarIonToleranceHalfWidthMinutes)

                .SetParam(XML_SECTION_SIC_OPTIONS, "SpectrumSimilarityMinimum", SICOptions.SpectrumSimilarityMinimum)

                ' Binning Options
                .SetParam(XML_SECTION_BINNING_OPTIONS, "BinStartX", BinningOptions.StartX)
                .SetParam(XML_SECTION_BINNING_OPTIONS, "BinEndX", BinningOptions.EndX)
                .SetParam(XML_SECTION_BINNING_OPTIONS, "BinSize", BinningOptions.BinSize)
                .SetParam(XML_SECTION_BINNING_OPTIONS, "MaximumBinCount", BinningOptions.MaximumBinCount)

                .SetParam(XML_SECTION_BINNING_OPTIONS, "IntensityPrecisionPercent", BinningOptions.IntensityPrecisionPercent)
                .SetParam(XML_SECTION_BINNING_OPTIONS, "Normalize", BinningOptions.Normalize)
                .SetParam(XML_SECTION_BINNING_OPTIONS, "SumAllIntensitiesForBin", BinningOptions.SumAllIntensitiesForBin)


                ' Memory management options
                .SetParam(XML_SECTION_MEMORY_OPTIONS, "DiskCachingAlwaysDisabled", CacheOptions.DiskCachingAlwaysDisabled)
                .SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheFolderPath", CacheOptions.FolderPath)
                .SetParam(XML_SECTION_MEMORY_OPTIONS, "CacheSpectraToRetainInMemory", CacheOptions.SpectraToRetainInMemory)

            End With

            ' Construct the strRawText strings using mCustomSICList
            Dim blnScanCommentsDefined = False

            Dim lstMzValues = New List(Of String)
            Dim lstMzTolerances = New List(Of String)
            Dim lstScanCenters = New List(Of String)
            Dim lstScanTolerances = New List(Of String)
            Dim lstComments = New List(Of String)

            For Each mzSearchValue In CustomSICList.CustomMZSearchValues
                With mzSearchValue
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
            Next

            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZList", String.Join(ControlChars.Tab, lstMzValues))
            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZToleranceDaList", String.Join(ControlChars.Tab, lstMzTolerances))

            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList", String.Join(ControlChars.Tab, lstScanCenters))
            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanToleranceList", String.Join(ControlChars.Tab, lstScanTolerances))

            If blnScanCommentsDefined Then
                objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", String.Join(ControlChars.Tab, lstComments))
            Else
                objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", String.Empty)
            End If

            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", CustomSICList.ScanOrAcqTimeTolerance.ToString)

            Select Case CustomSICList.ScanToleranceType
                Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_RELATIVE)
                Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_ACQUISITION_TIME)
                Case Else
                    ' Assume absolute
                    objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_ABSOLUTE)
            End Select

            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", CustomSICList.CustomSICListFileName)

            objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "LimitSearchToCustomMZList", CustomSICList.LimitSearchToCustomMZList)


            objSettingsFile.SaveSettings()

        Catch ex As Exception
            ReportError("SaveParameterFileSettings", "Error in SaveParameterFileSettings", ex, True, False, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

    Public Sub SetStatusLogKeyNameFilterList(strMatchSpecList() As String)
        Try
            StatusLogKeyNameFilterList.Clear()

            If Not strMatchSpecList Is Nothing Then
                Dim query = (From item In strMatchSpecList Select item).Distinct()
                For Each item In query
                    StatusLogKeyNameFilterList.Add(item)
                Next
            End If
        Catch ex As Exception
            ' Ignore errors here
        End Try
    End Sub

    Public Sub SetStatusLogKeyNameFilterList(strMatchSpecList As String, chDelimiter As Char)
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

End Class
