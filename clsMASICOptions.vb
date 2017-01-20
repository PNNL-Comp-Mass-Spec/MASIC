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
    ''' If mStatusLogKeyNameFilterList contains any entries, then only the entries matching the specs in mStatusLogKeyNameFilterList will be saved
    ''' </summary>
    Public Property WriteExtendedStatsStatusLog As Boolean

    Public Property ConsolidateConstantExtendedHeaderValues As Boolean

    ''' <summary>
    ''' Since there are so many values listed in the Status Log, this is used to limit the items saved to only those matching the specs in mStatusLogKeyNameFilterList
    ''' </summary>
    ''' <remarks>
    ''' When parsing the entries in mStatusLogKeyNameFilterList, if any part of the text in mStatusLogKeyNameFilterList() matches the status log key name, 
    ''' that key name is saved (key names are not case sensitive)
    ''' </remarks>
    Private mStatusLogKeyNameFilterList() As String

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

        ReDim mStatusLogKeyNameFilterList(-1)
        SetStatusLogKeyNameFilterList("Source", ","c)

        WriteMRMDataList = False
        WriteMRMIntensityCrosstab = True

        RawDataExportOptions.Reset()

        CDFTimeInSeconds = True
        ParentIonDecoyMassDa = 0

        ' Enabling this gives files of nearly equivalent size, but with the data arrays base-64 encoded; thus, no advantage
        UseBase64DataEncoding = False

        SICOptions.Reset()

        BinningOptions = clsCorrelation.GetDefaultBinningOptions()

        CacheOptions = clsSpectraCache.GetDefaultCacheOptions()

        CustomSICList.Reset()

    End Sub

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

        Dim eReporterIonMassMode As clsReporterIons.eReporterIonMassModeConstants
        Dim eReporterIonITraq4PlexCorrectionFactorType As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex

        Dim dblSICTolerance As Double
        Dim blnSICToleranceIsPPM As Boolean

        Dim strErrorMessage As String
        Dim blnNotPresent As Boolean

        Dim blnSuccess As Boolean

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
                strParameterFilePath = Path.Combine(clsProcessFilesOrFoldersBase.GetAppFolderPath(), Path.GetFileName(strParameterFilePath))
                If Not File.Exists(strParameterFilePath) Then
                    ReportError("LoadParameterFileSettings", "Parameter file not found: " & strParameterFilePath, Nothing, True, False)
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
                        Me.ExportRawDataFileFormat = CType(.GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataFileFormat", CInt(Me.ExportRawDataFileFormat)), clsRawDataExportOptions.eExportRawDataFileFormatConstants)

                        Me.ExportRawDataIncludeMSMS = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIncludeMSMS", Me.ExportRawDataIncludeMSMS)
                        Me.ExportRawDataRenumberScans = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataRenumberScans", Me.ExportRawDataRenumberScans)

                        Me.ExportRawDataMinimumSignalToNoiseRatio = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMinimumSignalToNoiseRatio", Me.ExportRawDataMinimumSignalToNoiseRatio)
                        Me.ExportRawDataMaxIonCountPerScan = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataMaxIonCountPerScan", Me.ExportRawDataMaxIonCountPerScan)
                        Me.ExportRawDataIntensityMinimum = .GetParam(XML_SECTION_EXPORT_OPTIONS, "ExportRawDataIntensityMinimum", Me.ExportRawDataIntensityMinimum)
                    End If

                    If Not .SectionPresent(XML_SECTION_SIC_OPTIONS) Then
                        strErrorMessage = "The node '<section name=" & ControlChars.Quote & XML_SECTION_SIC_OPTIONS & ControlChars.Quote & "> was not found in the parameter file: " & strParameterFilePath
                        ReportError("LoadParameterFileSettings", strErrorMessage)
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

                        MyBase.SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidParameterFile)
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

                    CustomSICList.CustomSICListFileName = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", String.Empty)

                    If CustomSICList.CustomSICListFileName.Length > 0 Then
                        ' Clear mCustomSICList; we'll read the data from the file when ProcessFile is called()

                        CustomSICList.ResetMzSearchValues()

                        blnSuccess = True
                    Else
                        strMZList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZList", String.Empty)
                        strMZToleranceDaList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "MZToleranceDaList", String.Empty)

                        strScanCenterList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCenterList", String.Empty)
                        strScanToleranceList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanToleranceList", String.Empty)

                        strScanCommentList = objSettingsFile.GetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanCommentList", String.Empty)

                        blnSuccess = CustomSICList.ParseCustomSICList(strMZList, strMZToleranceDaList, strScanCenterList, strScanToleranceList, strScanCommentList)
                    End If

                    If Not blnSuccess Then
                        Return False
                    End If

                End If
            Else
                ReportError("LoadParameterFileSettings", "Error calling objSettingsFile.LoadSettings for " & strParameterFilePath, Nothing, True, False, clsMASIC.eMasicErrorCodes.InputFileDataReadError)
                Return False
            End If

        Catch ex As Exception
            ReportError("LoadParameterFileSettings", "Error in LoadParameterFileSettings", ex, True, False, clsMASIC.eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

        Return True

    End Function

    Public Function SaveParameterFileSettings(strParameterFilePath As String) As Boolean

        Dim objSettingsFile As New XmlSettingsFileAccessor

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

                End With

                ' Construct the strRawText strings using mCustomSICList
                blnScanCommentsDefined = False

                Dim lstMzValues = New List(Of String)
                Dim lstMzTolerances = New List(Of String)
                Dim lstScanCenters = New List(Of String)
                Dim lstScanTolerances = New List(Of String)
                Dim lstComments = New List(Of String)

                For Each mzSearchValue In mCustomSICList.CustomMZSearchValues
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

                objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanTolerance", mCustomSICList.ScanOrAcqTimeTolerance.ToString)

                Select Case mCustomSICList.ScanToleranceType
                    Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                        objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_RELATIVE)
                    Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                        objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_ACQUISITION_TIME)
                    Case Else
                        ' Assume absolute
                        objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "ScanType", clsCustomSICList.CUSTOM_SIC_TYPE_ABSOLUTE)
                End Select

                objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "CustomMZFile", mCustomSICList.CustomSICListFileName)

                objSettingsFile.SetParam(XML_SECTION_CUSTOM_SIC_VALUES, "LimitSearchToCustomMZList", mCustomSICList.LimitSearchToCustomMZList)


                objSettingsFile.SaveSettings()

            End If

        Catch ex As Exception
            ReportError("SaveParameterFileSettings", "Error in SaveParameterFileSettings", ex, True, False, clsMASIC.eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

        Return True

    End Function

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
