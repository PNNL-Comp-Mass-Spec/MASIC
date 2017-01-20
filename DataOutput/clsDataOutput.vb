Imports MASIC.clsMASIC

Namespace DataOutput

    Public Class clsDataOutput
        Inherits clsEventNotifier

#Region "Constants and Enums"

        Public Enum eOutputFileTypeConstants
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
#End Region

#Region "Properties"
        Private ReadOnly mOptions As clsMASICOptions

        Public ReadOnly Property OutputFileHandles As clsOutputFileHandles

        Public ReadOnly Property ExtendedStatsWriter As clsExtendedStatsWriter

#End Region

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New(masicOptions As clsMASICOptions)

            mOptions = masicOptions

            OutputFileHandles = New clsOutputFileHandles()
            RegisterEvents(OutputFileHandles)

            ExtendedStatsWriter = New clsExtendedStatsWriter(mOptions)
            RegisterEvents(ExtendedStatsWriter)

        End Sub

        Public Function CheckForExistingResults(
          strInputFilePathFull As String,
          strOutputFolderPath As String,
          masicOptions As clsMASICOptions) As Boolean

            ' Returns True if existing results already exist for the given input file path, SIC Options, and Binning options

            Dim strFilePathToCheck As String

            Dim objXMLDoc As Xml.XmlDocument
            Dim objMatchingNodeList As Xml.XmlNodeList
            Dim objValueNode As Xml.XmlNode

            Dim sicOptionsCompare = New clsSICOptions()
            Dim binningOptionsCompare = New clsBinningOptions

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
                If masicOptions.SkipSICAndRawDataProcessing Then
                    Return False
                End If

                ' Obtain the output XML filename
                strFilePathToCheck = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.XMLFile)

                ' See if the file exists
                If File.Exists(strFilePathToCheck) Then

                    If masicOptions.FastExistingXMLFileTest Then
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
                            With sicOptionsCompare
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
                        If strMASICVersion <> masicOptions.MASICVersion() Then Exit Try

                        If strMASICPeakFinderDllVersion <> masicOptions.PeakFinderVersion Then Exit Try

                        ' Check the dataset number
                        If sicOptionsCompare.DatasetNumber <> masicOptions.SICOptions.DatasetNumber Then Exit Try

                        ' Check the filename in strSourceFilePathCheck
                        If Path.GetFileName(strSourceFilePathCheck) <> Path.GetFileName(strInputFilePathFull) Then Exit Try

                        ' Check if the source file stats match
                        Dim ioFileInfo As New FileInfo(strInputFilePathFull)
                        dtSourceFileDateTime = ioFileInfo.LastWriteTime()
                        If strSourceFileDateTimeCheck <> (dtSourceFileDateTime.ToShortDateString & " " & dtSourceFileDateTime.ToShortTimeString) Then Exit Try
                        If lngSourceFileSizeBytes <> ioFileInfo.Length Then Exit Try

                        ' Check that blnSkipMSMSProcessing matches
                        If blnSkipMSMSProcessing <> masicOptions.SkipMSMSProcessing Then Exit Try

                        ' Read the ProcessingOptions and populate
                        objMatchingNodeList = objRootElement.GetElementsByTagName("ProcessingOptions")
                        If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then Exit Try

                        For Each objValueNode In objMatchingNodeList(0).ChildNodes
                            With sicOptionsCompare
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
                            With binningOptionsCompare
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

                        Dim customSICListCompare = New clsCustomSICList()

                        objMatchingNodeList = objRootElement.GetElementsByTagName("CustomSICValues")
                        If objMatchingNodeList Is Nothing OrElse objMatchingNodeList.Count <> 1 Then
                            ' Custom values not defined; that's OK
                        Else
                            For Each objValueNode In objMatchingNodeList(0).ChildNodes
                                With customSICListCompare
                                    Select Case objValueNode.Name
                                        Case "MZList" : .RawTextMZList = objValueNode.InnerText
                                        Case "MZToleranceDaList" : .RawTextMZToleranceDaList = objValueNode.InnerText
                                        Case "ScanCenterList" : .RawTextScanOrAcqTimeCenterList = objValueNode.InnerText
                                        Case "ScanToleranceList" : .RawTextScanOrAcqTimeToleranceList = objValueNode.InnerText
                                        Case "ScanTolerance" : .ScanOrAcqTimeTolerance = CSng(objValueNode.InnerText)
                                        Case "ScanType"
                                            .ScanToleranceType = masicOptions.GetScanToleranceTypeFromText(objValueNode.InnerText)
                                    End Select
                                End With
                            Next objValueNode
                        End If

                        Dim sicOptions = masicOptions.SICOptions

                        ' Check if the processing options match
                        With sicOptionsCompare
                            If clsUtilities.ValuesMatch(.SICTolerance, sicOptions.SICTolerance, 3) AndAlso
                             .SICToleranceIsPPM = sicOptions.SICToleranceIsPPM AndAlso
                             .RefineReportedParentIonMZ = sicOptions.RefineReportedParentIonMZ AndAlso
                             .ScanRangeStart = sicOptions.ScanRangeStart AndAlso
                             .ScanRangeEnd = sicOptions.ScanRangeEnd AndAlso
                             clsUtilities.ValuesMatch(.RTRangeStart, sicOptions.RTRangeStart, 2) AndAlso
                             clsUtilities.ValuesMatch(.RTRangeEnd, sicOptions.RTRangeEnd, 2) AndAlso
                             .CompressMSSpectraData = sicOptions.CompressMSSpectraData AndAlso
                             .CompressMSMSSpectraData = sicOptions.CompressMSMSSpectraData AndAlso
                             clsUtilities.ValuesMatch(.CompressToleranceDivisorForDa, sicOptions.CompressToleranceDivisorForDa, 2) AndAlso
                             clsUtilities.ValuesMatch(.CompressToleranceDivisorForPPM, sicOptions.CompressToleranceDivisorForDa, 2) AndAlso
                             clsUtilities.ValuesMatch(.MaxSICPeakWidthMinutesBackward, sicOptions.MaxSICPeakWidthMinutesBackward, 2) AndAlso
                             clsUtilities.ValuesMatch(.MaxSICPeakWidthMinutesForward, sicOptions.MaxSICPeakWidthMinutesForward, 2) AndAlso
                             .ReplaceSICZeroesWithMinimumPositiveValueFromMSData = sicOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.IntensityThresholdFractionMax, sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax) AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum, sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum) AndAlso
                             .SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode = sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseMode AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute) AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage) AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.SICBaselineNoiseOptions.MinimumSignalToNoiseRatio) AndAlso
                             .SICPeakFinderOptions.MaxDistanceScansNoOverlap = sicOptions.SICPeakFinderOptions.MaxDistanceScansNoOverlap AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax, sicOptions.SICPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax) AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.InitialPeakWidthScansScaler, sicOptions.SICPeakFinderOptions.InitialPeakWidthScansScaler) AndAlso
                             .SICPeakFinderOptions.InitialPeakWidthScansMaximum = sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum AndAlso
                             .SICPeakFinderOptions.FindPeaksOnSmoothedData = sicOptions.SICPeakFinderOptions.FindPeaksOnSmoothedData AndAlso
                             .SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth = sicOptions.SICPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth AndAlso
                             .SICPeakFinderOptions.UseButterworthSmooth = sicOptions.SICPeakFinderOptions.UseButterworthSmooth AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.ButterworthSamplingFrequency, sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequency) AndAlso
                             .SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData = sicOptions.SICPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData AndAlso
                             .SICPeakFinderOptions.UseSavitzkyGolaySmooth = sicOptions.SICPeakFinderOptions.UseSavitzkyGolaySmooth AndAlso
                             .SICPeakFinderOptions.SavitzkyGolayFilterOrder = sicOptions.SICPeakFinderOptions.SavitzkyGolayFilterOrder AndAlso
                             .SaveSmoothedData = sicOptions.SaveSmoothedData AndAlso
                             .SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode = sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseMode AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute) AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage) AndAlso
                             clsUtilities.ValuesMatch(.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio) AndAlso
                             clsUtilities.ValuesMatch(.SimilarIonMZToleranceHalfWidth, sicOptions.SimilarIonMZToleranceHalfWidth) AndAlso
                             clsUtilities.ValuesMatch(.SimilarIonToleranceHalfWidthMinutes, sicOptions.SimilarIonToleranceHalfWidthMinutes) AndAlso
                             clsUtilities.ValuesMatch(.SpectrumSimilarityMinimum, sicOptions.SpectrumSimilarityMinimum) Then
                                blnValidExistingResultsFound = True
                            Else
                                blnValidExistingResultsFound = False
                            End If
                        End With

                        If blnValidExistingResultsFound Then
                            ' Check if the binning options match
                            Dim binningOptions = masicOptions.BinningOptions

                            With binningOptionsCompare
                                If clsUtilities.ValuesMatch(.StartX, binningOptions.StartX) AndAlso
                                 clsUtilities.ValuesMatch(.EndX, binningOptions.EndX) AndAlso
                                 clsUtilities.ValuesMatch(.BinSize, binningOptions.BinSize) AndAlso
                                 .MaximumBinCount = binningOptions.MaximumBinCount AndAlso
                                 clsUtilities.ValuesMatch(.IntensityPrecisionPercent, binningOptions.IntensityPrecisionPercent) AndAlso
                                 .Normalize = binningOptions.Normalize AndAlso
                                 .SumAllIntensitiesForBin = binningOptions.SumAllIntensitiesForBin Then

                                    blnValidExistingResultsFound = True
                                Else
                                    blnValidExistingResultsFound = False
                                End If
                            End With
                        End If

                        If blnValidExistingResultsFound Then
                            ' Check if the Custom MZ options match
                            With customSICListCompare
                                If .RawTextMZList = masicOptions.CustomSICList.RawTextMZList AndAlso
                                   .RawTextMZToleranceDaList = masicOptions.CustomSICList.RawTextMZToleranceDaList AndAlso
                                   .RawTextScanOrAcqTimeCenterList = masicOptions.CustomSICList.RawTextScanOrAcqTimeCenterList AndAlso
                                   .RawTextScanOrAcqTimeToleranceList = masicOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList AndAlso
                                   clsUtilities.ValuesMatch(.ScanOrAcqTimeTolerance, masicOptions.CustomSICList.ScanOrAcqTimeTolerance) AndAlso
                                   .ScanToleranceType = masicOptions.CustomSICList.ScanToleranceType Then

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
                ReportError("CheckForExistingResults", "There may be a programming error in CheckForExistingResults", ex, True, False)
                blnValidExistingResultsFound = False
            End Try

            Return blnValidExistingResultsFound

        End Function

        Public Shared Function ConstructOutputFilePath(
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
                    strOutputFilePath &= "_Frag" & intFragTypeNumber.ToString() & "_BPI.txt"
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
                    Throw New ArgumentOutOfRangeException(NameOf(eFileType), "Unknown Output File Type found in clsDataOutput.ConstructOutputFilePath")
            End Select

            Return strOutputFilePath

        End Function

        Public Function CreateDatasetInfoFile(
          strInputFileName As String,
          strOutputFolderPath As String,
          scanTracking As clsScanTracking,
          datasetFileInfo As DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType) As Boolean

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
                  scanTracking.ScanStats, datasetFileInfo, udtSampleInfo)

                If Not blnSuccess Then
                    ReportError("Error calling objDatasetStatsSummarizer.CreateDatasetInfoFile", objDatasetStatsSummarizer.ErrorMessage, New Exception("Error calling objDatasetStatsSummarizer.CreateDatasetInfoFile: " & objDatasetStatsSummarizer.ErrorMessage), True, False)
                End If

            Catch ex As Exception
                ReportError("CreateDatasetInfoFile", "Error creating dataset info file", ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                blnSuccess = False
            End Try

            Return blnSuccess

        End Function

        Public Function GetHeadersForOutputFile(scanList As clsScanList, eOutputFileType As eOutputFileTypeConstants) As String
            Return GetHeadersForOutputFile(scanList, eOutputFileType, ControlChars.Tab)
        End Function

        Private Function GetHeadersForOutputFile(
          scanList As clsScanList, eOutputFileType As eOutputFileTypeConstants, cColDelimiter As Char) As String

            Dim strHeaders As String
            Dim intNonConstantHeaderIDs() As Integer = Nothing

            Select Case eOutputFileType
                Case eOutputFileTypeConstants.ScanStatsFlatFile
                    strHeaders = "Dataset" & cColDelimiter & "ScanNumber" & cColDelimiter & "ScanTime" & cColDelimiter &
                       "ScanType" & cColDelimiter & "TotalIonIntensity" & cColDelimiter & "BasePeakIntensity" & cColDelimiter &
                       "BasePeakMZ" & cColDelimiter & "BasePeakSignalToNoiseRatio" & cColDelimiter &
                       "IonCount" & cColDelimiter & "IonCountRaw" & cColDelimiter & "ScanTypeName"

                Case eOutputFileTypeConstants.ScanStatsExtendedFlatFile

                    If Not ExtendedStatsWriter.ExtendedHeaderNameCount > 0 Then

                        ' Lookup extended stats values that are constants for all scans
                        ' The following will also remove the constant header values from htExtendedHeaderInfo
                        ExtendedStatsWriter.ExtractConstantExtendedHeaderValues(intNonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, cColDelimiter)

                        strHeaders = ExtendedStatsWriter.ConstructExtendedStatsHeaders(cColDelimiter)
                    Else
                        strHeaders = String.Empty
                    End If

                Case eOutputFileTypeConstants.SICStatsFlatFile
                    strHeaders = "Dataset" & cColDelimiter & "ParentIonIndex" & cColDelimiter & "MZ" & cColDelimiter & "SurveyScanNumber" & cColDelimiter & "FragScanNumber" & cColDelimiter & "OptimalPeakApexScanNumber" & cColDelimiter & "PeakApexOverrideParentIonIndex" & cColDelimiter &
                       "CustomSICPeak" & cColDelimiter & "PeakScanStart" & cColDelimiter & "PeakScanEnd" & cColDelimiter & "PeakScanMaxIntensity" & cColDelimiter &
                       "PeakMaxIntensity" & cColDelimiter & "PeakSignalToNoiseRatio" & cColDelimiter & "FWHMInScans" & cColDelimiter & "PeakArea" & cColDelimiter & "ParentIonIntensity" & cColDelimiter &
                       "PeakBaselineNoiseLevel" & cColDelimiter & "PeakBaselineNoiseStDev" & cColDelimiter & "PeakBaselinePointsUsed" & cColDelimiter &
                       "StatMomentsArea" & cColDelimiter & "CenterOfMassScan" & cColDelimiter & "PeakStDev" & cColDelimiter & "PeakSkew" & cColDelimiter & "PeakKSStat" & cColDelimiter & "StatMomentsDataCountUsed"

                    If mOptions.IncludeScanTimesInSICStatsFile Then
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

        Public Function InitializeSICDetailsTextFile(
          strInputFilePathFull As String,
          strOutputFolderPath As String) As Boolean

            Dim strOutputFilePath As String = String.Empty

            Try

                strOutputFilePath = ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, eOutputFileTypeConstants.SICDataFile)

                OutputFileHandles.SICDataFile = New StreamWriter(New FileStream(strOutputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

                ' Write the header line
                OutputFileHandles.SICDataFile.WriteLine("Dataset" & ControlChars.Tab &
                 "ParentIonIndex" & ControlChars.Tab &
                 "FragScanIndex" & ControlChars.Tab &
                 "ParentIonMZ" & ControlChars.Tab &
                 "Scan" & ControlChars.Tab &
                 "MZ" & ControlChars.Tab &
                 "Intensity")

            Catch ex As Exception
                ReportError("InitializeSICDetailsTextFile", "Error initializing the XML output file: " & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Sub OpenOutputFileHandles(
          strInputFileName As String,
          strOutputFolderPath As String,
          blnWriteHeaders As Boolean)

            Dim strOutputFilePath As String

            With OutputFileHandles

                ' Scan Stats file
                strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ScanStatsFlatFile)
                .ScanStats = New StreamWriter(strOutputFilePath, False)
                If blnWriteHeaders Then .ScanStats.WriteLine(GetHeadersForOutputFile(Nothing, eOutputFileTypeConstants.ScanStatsFlatFile))

                .MSMethodFilePathBase = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MSMethodFile)
                .MSTuneFilePathBase = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.MSTuneFile)
            End With

        End Sub

        Public Function SaveHeaderGlossary(
          scanList As clsScanList,
          strInputFileName As String,
          strOutputFolderPath As String) As Boolean


            Dim srOutFile As StreamWriter

            Dim strHeaders As String
            Dim strOutputFilePath = "?undefinedfile?"

            Try
                strOutputFilePath = ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.HeaderGlossary)
                ReportMessage("Saving Header Glossary to " & Path.GetFileName(strOutputFilePath))

                srOutFile = New StreamWriter(strOutputFilePath, False)

                ' ScanStats
                srOutFile.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.ScanStatsFlatFile) & ":")
                srOutFile.WriteLine(GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsFlatFile))
                srOutFile.WriteLine()

                ' SICStats
                srOutFile.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.SICStatsFlatFile) & ":")
                srOutFile.WriteLine(GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.SICStatsFlatFile))
                srOutFile.WriteLine()

                ' ScanStatsExtended
                strHeaders = GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsExtendedFlatFile)
                If Not strHeaders Is Nothing AndAlso strHeaders.Length > 0 Then
                    srOutFile.WriteLine(clsDataOutput.ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.ScanStatsExtendedFlatFile) & ":")
                    srOutFile.WriteLine(strHeaders)
                End If

                srOutFile.Close()

            Catch ex As Exception
                ReportError("SaveHeaderGlossary", "Error writing the Header Glossary to: " & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Function SaveSICDataToText(
          sicOptions As clsSICOptions,
          scanList As clsScanList,
          intParentIonIndex As Integer,
          ByRef udtSICDetails As clsDataObjects.udtSICStatsDetailsType) As Boolean


            Dim intFragScanIndex As Integer
            Dim strPrefix As String

            Try

                If OutputFileHandles.SICDataFile Is Nothing Then
                    Return True
                End If

                ' Write the detailed SIC values for the given parent ion to the text file

                For intFragScanIndex = 0 To scanList.ParentIons(intParentIonIndex).FragScanIndexCount - 1

                    ' "Dataset  ParentIonIndex  FragScanIndex  ParentIonMZ
                    strPrefix = sicOptions.DatasetNumber.ToString() & ControlChars.Tab &
                       intParentIonIndex.ToString() & ControlChars.Tab &
                       intFragScanIndex.ToString() & ControlChars.Tab &
                       Math.Round(scanList.ParentIons(intParentIonIndex).MZ, 4).ToString() & ControlChars.Tab

                    With udtSICDetails
                        If .SICDataCount = 0 Then
                            ' Nothing to write
                            OutputFileHandles.SICDataFile.WriteLine(strPrefix & "0" & ControlChars.Tab & "0" & ControlChars.Tab & "0")
                        Else
                            For intScanIndex = 0 To .SICDataCount - 1
                                OutputFileHandles.SICDataFile.WriteLine(strPrefix & .SICScanNumbers(intScanIndex) & ControlChars.Tab & .SICMasses(intScanIndex) & ControlChars.Tab & .SICData(intScanIndex))
                            Next intScanIndex
                        End If
                    End With

                Next intFragScanIndex

            Catch ex As Exception
                ReportError("SaveSICDataToText", "Error writing to detailed SIC data text file", ex, True, False, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

    End Class

End Namespace
