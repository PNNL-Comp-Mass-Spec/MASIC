Imports MASIC.clsMASIC
Imports PRISM

Namespace DataOutput

    Public Class clsDataOutput
        Inherits clsMasicEventNotifier

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
          inputFilePathFull As String,
          outputDirectoryPath As String,
          masicOptions As clsMASICOptions) As Boolean

            ' Returns True if existing results already exist for the given input file path, SIC Options, and Binning options

            Dim filePathToCheck As String

            Dim sicOptionsCompare = New clsSICOptions()
            Dim binningOptionsCompare = New clsBinningOptions

            Dim validExistingResultsFound As Boolean

            Dim sourceFileSizeBytes As Int64
            Dim sourceFilePathCheck As String = String.Empty
            Dim masicVersion As String = String.Empty
            Dim masicPeakFinderDllVersion As String = String.Empty
            Dim sourceFileDateTimeCheck As String = String.Empty
            Dim sourceFileDateTime As Date

            Dim skipMSMSProcessing As Boolean

            validExistingResultsFound = False
            Try
                ' Don't even look for the XML file if mSkipSICAndRawDataProcessing = True
                If masicOptions.SkipSICAndRawDataProcessing Then
                    Return False
                End If

                ' Obtain the output XML filename
                filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.XMLFile)

                ' See if the file exists
                If File.Exists(filePathToCheck) Then

                    If masicOptions.FastExistingXMLFileTest Then
                        ' XML File found; do not check the settings or version to see if they match the current ones
                        Return True
                    End If

                    ' Open the XML file and look for the "ProcessingComplete" node
                    Dim xmlDoc = New Xml.XmlDocument
                    Try
                        xmlDoc.Load(filePathToCheck)
                    Catch ex As Exception
                        ' Invalid XML file; do not continue
                        Return False
                    End Try

                    ' If we get here, the file opened successfully
                    Dim rootElement As Xml.XmlElement = xmlDoc.DocumentElement

                    If rootElement.Name = "SICData" Then
                        ' See if the ProcessingComplete node has a value of True
                        Dim matchingNodeList = rootElement.GetElementsByTagName("ProcessingComplete")
                        If matchingNodeList Is Nothing OrElse matchingNodeList.Count <> 1 Then Exit Try
                        If matchingNodeList.Item(0).InnerText.ToLower <> "true" Then Exit Try

                        ' Read the ProcessingSummary and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingSummary")
                        If matchingNodeList Is Nothing OrElse matchingNodeList.Count <> 1 Then Exit Try

                        For Each valueNode As Xml.XmlNode In matchingNodeList(0).ChildNodes
                            With sicOptionsCompare
                                Select Case valueNode.Name
                                    Case "DatasetNumber" : .DatasetNumber = CInt(valueNode.InnerText)
                                    Case "SourceFilePath" : sourceFilePathCheck = valueNode.InnerText
                                    Case "SourceFileDateTime" : sourceFileDateTimeCheck = valueNode.InnerText
                                    Case "SourceFileSizeBytes" : sourceFileSizeBytes = CLng(valueNode.InnerText)
                                    Case "MASICVersion" : masicVersion = valueNode.InnerText
                                    Case "MASICPeakFinderDllVersion" : masicPeakFinderDllVersion = valueNode.InnerText
                                    Case "SkipMSMSProcessing" : skipMSMSProcessing = CBool(valueNode.InnerText)
                                End Select
                            End With
                        Next valueNode

                        If masicVersion Is Nothing Then masicVersion = String.Empty
                        If masicPeakFinderDllVersion Is Nothing Then masicPeakFinderDllVersion = String.Empty

                        ' Check if the MASIC version matches
                        If masicVersion <> masicOptions.MASICVersion() Then Exit Try

                        If masicPeakFinderDllVersion <> masicOptions.PeakFinderVersion Then Exit Try

                        ' Check the dataset number
                        If sicOptionsCompare.DatasetNumber <> masicOptions.SICOptions.DatasetNumber Then Exit Try

                        ' Check the filename in sourceFilePathCheck
                        If Path.GetFileName(sourceFilePathCheck) <> Path.GetFileName(inputFilePathFull) Then Exit Try

                        ' Check if the source file stats match
                        Dim inputFileInfo As New FileInfo(inputFilePathFull)
                        sourceFileDateTime = inputFileInfo.LastWriteTime()
                        If sourceFileDateTimeCheck <> (sourceFileDateTime.ToShortDateString() & " " & sourceFileDateTime.ToShortTimeString()) Then Exit Try
                        If sourceFileSizeBytes <> inputFileInfo.Length Then Exit Try

                        ' Check that skipMSMSProcessing matches
                        If skipMSMSProcessing <> masicOptions.SkipMSMSProcessing Then Exit Try

                        ' Read the ProcessingOptions and populate
                        matchingNodeList = rootElement.GetElementsByTagName("ProcessingOptions")
                        If matchingNodeList Is Nothing OrElse matchingNodeList.Count <> 1 Then Exit Try

                        For Each valueNode As Xml.XmlNode In matchingNodeList(0).ChildNodes
                            With sicOptionsCompare
                                Select Case valueNode.Name
                                    Case "SICToleranceDa" : .SICTolerance = CDbl(valueNode.InnerText)            ' Legacy name
                                    Case "SICTolerance" : .SICTolerance = CDbl(valueNode.InnerText)
                                    Case "SICToleranceIsPPM" : .SICToleranceIsPPM = CBool(valueNode.InnerText)
                                    Case "RefineReportedParentIonMZ" : .RefineReportedParentIonMZ = CBool(valueNode.InnerText)
                                    Case "ScanRangeEnd" : .ScanRangeEnd = CInt(valueNode.InnerText)
                                    Case "ScanRangeStart" : .ScanRangeStart = CInt(valueNode.InnerText)
                                    Case "RTRangeEnd" : .RTRangeEnd = CSng(valueNode.InnerText)
                                    Case "RTRangeStart" : .RTRangeStart = CSng(valueNode.InnerText)

                                    Case "CompressMSSpectraData" : .CompressMSSpectraData = CBool(valueNode.InnerText)
                                    Case "CompressMSMSSpectraData" : .CompressMSMSSpectraData = CBool(valueNode.InnerText)

                                    Case "CompressToleranceDivisorForDa" : .CompressToleranceDivisorForDa = CDbl(valueNode.InnerText)
                                    Case "CompressToleranceDivisorForPPM" : .CompressToleranceDivisorForPPM = CDbl(valueNode.InnerText)

                                    Case "MaxSICPeakWidthMinutesBackward" : .MaxSICPeakWidthMinutesBackward = CSng(valueNode.InnerText)
                                    Case "MaxSICPeakWidthMinutesForward" : .MaxSICPeakWidthMinutesForward = CSng(valueNode.InnerText)

                                    Case "ReplaceSICZeroesWithMinimumPositiveValueFromMSData" : .ReplaceSICZeroesWithMinimumPositiveValueFromMSData = CBool(valueNode.InnerText)
                                    Case "SaveSmoothedData" : .SaveSmoothedData = CBool(valueNode.InnerText)

                                    Case "SimilarIonMZToleranceHalfWidth" : .SimilarIonMZToleranceHalfWidth = CSng(valueNode.InnerText)
                                    Case "SimilarIonToleranceHalfWidthMinutes" : .SimilarIonToleranceHalfWidthMinutes = CSng(valueNode.InnerText)
                                    Case "SpectrumSimilarityMinimum" : .SpectrumSimilarityMinimum = CSng(valueNode.InnerText)
                                    Case Else
                                        With .SICPeakFinderOptions
                                            Select Case valueNode.Name
                                                Case "IntensityThresholdFractionMax" : .IntensityThresholdFractionMax = CSng(valueNode.InnerText)
                                                Case "IntensityThresholdAbsoluteMinimum" : .IntensityThresholdAbsoluteMinimum = CSng(valueNode.InnerText)

                                                Case "SICNoiseThresholdMode" : .SICBaselineNoiseOptions.BaselineNoiseMode = CType(valueNode.InnerText, MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
                                                Case "SICNoiseThresholdIntensity" : .SICBaselineNoiseOptions.BaselineNoiseLevelAbsolute = CSng(valueNode.InnerText)
                                                Case "SICNoiseFractionLowIntensityDataToAverage"
                                                    .SICBaselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage = CSng(valueNode.InnerText)
                                                Case "SICNoiseMinimumSignalToNoiseRatio" : .SICBaselineNoiseOptions.MinimumSignalToNoiseRatio = CSng(valueNode.InnerText)

                                                Case "MaxDistanceScansNoOverlap" : .MaxDistanceScansNoOverlap = CInt(valueNode.InnerText)
                                                Case "MaxAllowedUpwardSpikeFractionMax" : .MaxAllowedUpwardSpikeFractionMax = CSng(valueNode.InnerText)
                                                Case "InitialPeakWidthScansScaler" : .InitialPeakWidthScansScaler = CSng(valueNode.InnerText)
                                                Case "InitialPeakWidthScansMaximum" : .InitialPeakWidthScansMaximum = CInt(valueNode.InnerText)

                                                Case "FindPeaksOnSmoothedData" : .FindPeaksOnSmoothedData = CBool(valueNode.InnerText)
                                                Case "SmoothDataRegardlessOfMinimumPeakWidth" : .SmoothDataRegardlessOfMinimumPeakWidth = CBool(valueNode.InnerText)
                                                Case "UseButterworthSmooth" : .UseButterworthSmooth = CBool(valueNode.InnerText)
                                                Case "ButterworthSamplingFrequency" : .ButterworthSamplingFrequency = CSng(valueNode.InnerText)
                                                Case "ButterworthSamplingFrequencyDoubledForSIMData" : .ButterworthSamplingFrequencyDoubledForSIMData = CBool(valueNode.InnerText)

                                                Case "UseSavitzkyGolaySmooth" : .UseSavitzkyGolaySmooth = CBool(valueNode.InnerText)
                                                Case "SavitzkyGolayFilterOrder" : .SavitzkyGolayFilterOrder = CShort(valueNode.InnerText)

                                                Case "MassSpectraNoiseThresholdMode" : .MassSpectraNoiseThresholdOptions.BaselineNoiseMode = CType(valueNode.InnerText, MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes)
                                                Case "MassSpectraNoiseThresholdIntensity" : .MassSpectraNoiseThresholdOptions.BaselineNoiseLevelAbsolute = CSng(valueNode.InnerText)
                                                Case "MassSpectraNoiseFractionLowIntensityDataToAverage" : .MassSpectraNoiseThresholdOptions.TrimmedMeanFractionLowIntensityDataToAverage = CSng(valueNode.InnerText)
                                                Case "MassSpectraNoiseMinimumSignalToNoiseRatio" : .MassSpectraNoiseThresholdOptions.MinimumSignalToNoiseRatio = CSng(valueNode.InnerText)
                                            End Select
                                        End With
                                End Select
                            End With
                        Next valueNode

                        ' Read the BinningOptions and populate
                        matchingNodeList = rootElement.GetElementsByTagName("BinningOptions")
                        If matchingNodeList Is Nothing OrElse matchingNodeList.Count <> 1 Then Exit Try

                        For Each valueNode As Xml.XmlNode In matchingNodeList(0).ChildNodes
                            With binningOptionsCompare
                                Select Case valueNode.Name
                                    Case "BinStartX" : .StartX = CSng(valueNode.InnerText)
                                    Case "BinEndX" : .EndX = CSng(valueNode.InnerText)
                                    Case "BinSize" : .BinSize = CSng(valueNode.InnerText)
                                    Case "MaximumBinCount" : .MaximumBinCount = CInt(valueNode.InnerText)

                                    Case "IntensityPrecisionPercent" : .IntensityPrecisionPercent = CSng(valueNode.InnerText)
                                    Case "Normalize" : .Normalize = CBool(valueNode.InnerText)
                                    Case "SumAllIntensitiesForBin" : .SumAllIntensitiesForBin = CBool(valueNode.InnerText)
                                End Select
                            End With
                        Next valueNode

                        ' Read the CustomSICValues and populate

                        Dim customSICListCompare = New clsCustomSICList()

                        matchingNodeList = rootElement.GetElementsByTagName("CustomSICValues")
                        If matchingNodeList Is Nothing OrElse matchingNodeList.Count <> 1 Then
                            ' Custom values not defined; that's OK
                        Else
                            For Each valueNode As Xml.XmlNode In matchingNodeList(0).ChildNodes
                                With customSICListCompare
                                    Select Case valueNode.Name
                                        Case "MZList" : .RawTextMZList = valueNode.InnerText
                                        Case "MZToleranceDaList" : .RawTextMZToleranceDaList = valueNode.InnerText
                                        Case "ScanCenterList" : .RawTextScanOrAcqTimeCenterList = valueNode.InnerText
                                        Case "ScanToleranceList" : .RawTextScanOrAcqTimeToleranceList = valueNode.InnerText
                                        Case "ScanTolerance" : .ScanOrAcqTimeTolerance = CSng(valueNode.InnerText)
                                        Case "ScanType"
                                            .ScanToleranceType = masicOptions.GetScanToleranceTypeFromText(valueNode.InnerText)
                                    End Select
                                End With
                            Next valueNode
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
                                validExistingResultsFound = True
                            Else
                                validExistingResultsFound = False
                            End If
                        End With

                        If validExistingResultsFound Then
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

                                    validExistingResultsFound = True
                                Else
                                    validExistingResultsFound = False
                                End If
                            End With
                        End If

                        If validExistingResultsFound Then
                            ' Check if the Custom MZ options match
                            With customSICListCompare
                                If .RawTextMZList = masicOptions.CustomSICList.RawTextMZList AndAlso
                                   .RawTextMZToleranceDaList = masicOptions.CustomSICList.RawTextMZToleranceDaList AndAlso
                                   .RawTextScanOrAcqTimeCenterList = masicOptions.CustomSICList.RawTextScanOrAcqTimeCenterList AndAlso
                                   .RawTextScanOrAcqTimeToleranceList = masicOptions.CustomSICList.RawTextScanOrAcqTimeToleranceList AndAlso
                                   clsUtilities.ValuesMatch(.ScanOrAcqTimeTolerance, masicOptions.CustomSICList.ScanOrAcqTimeTolerance) AndAlso
                                   .ScanToleranceType = masicOptions.CustomSICList.ScanToleranceType Then

                                    validExistingResultsFound = True
                                Else
                                    validExistingResultsFound = False
                                End If
                            End With
                        End If

                        If validExistingResultsFound Then
                            ' All of the options match, make sure the other output files exist
                            validExistingResultsFound = False

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsFlatFile)
                            If Not File.Exists(filePathToCheck) Then Exit Try

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.SICStatsFlatFile)
                            If Not File.Exists(filePathToCheck) Then Exit Try

                            filePathToCheck = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.BPIFile)
                            If Not File.Exists(filePathToCheck) Then Exit Try

                            validExistingResultsFound = True
                        End If
                    End If
                End If
            Catch ex As Exception
                ReportError("There may be a programming error in CheckForExistingResults", ex)
                validExistingResultsFound = False
            End Try

            Return validExistingResultsFound

        End Function

        Public Shared Function ConstructOutputFilePath(
          inputFileName As String,
          outputDirectoryPath As String,
          eFileType As eOutputFileTypeConstants,
          Optional fragTypeNumber As Integer = 1) As String

            Dim outputFilePath As String

            outputFilePath = Path.Combine(outputDirectoryPath, Path.GetFileNameWithoutExtension(inputFileName))
            Select Case eFileType
                Case eOutputFileTypeConstants.XMLFile
                    outputFilePath &= "_SICs.xml"
                Case eOutputFileTypeConstants.ScanStatsFlatFile
                    outputFilePath &= "_ScanStats.txt"
                Case eOutputFileTypeConstants.ScanStatsExtendedFlatFile
                    outputFilePath &= "_ScanStatsEx.txt"
                Case eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile
                    outputFilePath &= "_ScanStatsConstant.txt"
                Case eOutputFileTypeConstants.SICStatsFlatFile
                    ' ReSharper disable once StringLiteralTypo
                    outputFilePath &= "_SICstats.txt"
                Case eOutputFileTypeConstants.BPIFile
                    outputFilePath &= "_BPI.txt"
                Case eOutputFileTypeConstants.FragBPIFile
                    outputFilePath &= "_Frag" & fragTypeNumber.ToString() & "_BPI.txt"
                Case eOutputFileTypeConstants.TICFile
                    outputFilePath &= "_TIC.txt"
                Case eOutputFileTypeConstants.ICRToolsBPIChromatogramByScan
                    outputFilePath &= "_BPI_Scan.tic"
                Case eOutputFileTypeConstants.ICRToolsBPIChromatogramByTime
                    outputFilePath &= "_BPI_Time.tic"
                Case eOutputFileTypeConstants.ICRToolsTICChromatogramByScan
                    outputFilePath &= "_TIC_Scan.tic"
                Case eOutputFileTypeConstants.ICRToolsFragTICChromatogramByScan
                    outputFilePath &= "_TIC_MSMS_Scan.tic"
                Case eOutputFileTypeConstants.DeconToolsMSChromatogramFile
                    outputFilePath &= "_MS_scans.csv"
                Case eOutputFileTypeConstants.DeconToolsMSMSChromatogramFile
                    outputFilePath &= "_MSMS_scans.csv"
                Case eOutputFileTypeConstants.PEKFile
                    outputFilePath &= ".pek"
                Case eOutputFileTypeConstants.HeaderGlossary
                    outputFilePath = Path.Combine(outputDirectoryPath, "Header_Glossary_Readme.txt")
                Case eOutputFileTypeConstants.DeconToolsIsosFile
                    outputFilePath &= "_isos.csv"
                Case eOutputFileTypeConstants.DeconToolsScansFile
                    outputFilePath &= "_scans.csv"
                Case eOutputFileTypeConstants.MSMethodFile
                    outputFilePath &= "_MSMethod"
                Case eOutputFileTypeConstants.MSTuneFile
                    outputFilePath &= "_MSTuneSettings"
                Case eOutputFileTypeConstants.ReporterIonsFile
                    outputFilePath &= "_ReporterIons.txt"
                Case eOutputFileTypeConstants.MRMSettingsFile
                    outputFilePath &= "_MRMSettings.txt"
                Case eOutputFileTypeConstants.MRMDatafile
                    outputFilePath &= "_MRMData.txt"
                Case eOutputFileTypeConstants.MRMCrosstabFile
                    outputFilePath &= "_MRMCrosstab.txt"
                Case eOutputFileTypeConstants.DatasetInfoFile
                    outputFilePath &= "_DatasetInfo.xml"
                Case eOutputFileTypeConstants.SICDataFile
                    outputFilePath &= "_SICdata.txt"
                Case Else
                    Throw New ArgumentOutOfRangeException(NameOf(eFileType), "Unknown Output File Type found in clsDataOutput.ConstructOutputFilePath")
            End Select

            Return outputFilePath

        End Function

        Public Function CreateDatasetInfoFile(
          inputFileName As String,
          outputDirectoryPath As String,
          scanTracking As clsScanTracking,
          datasetFileInfo As clsDatasetStatsSummarizer.udtDatasetFileInfoType) As Boolean

            Dim udtSampleInfo = New clsDatasetStatsSummarizer.udtSampleInfoType
            udtSampleInfo.Clear()

            Try
                Dim datasetName = Path.GetFileNameWithoutExtension(inputFileName)
                Dim datasetInfoFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.DatasetInfoFile)

                Dim datasetStatsSummarizer = New clsDatasetStatsSummarizer()

                Dim success = datasetStatsSummarizer.CreateDatasetInfoFile(
                  datasetName, datasetInfoFilePath,
                  scanTracking.ScanStats, datasetFileInfo, udtSampleInfo)

                If success Then
                    Return True
                End If

                ReportError("datasetStatsSummarizer.CreateDatasetInfoFile, error from DataStatsSummarizer: " + datasetStatsSummarizer.ErrorMessage,
                            New Exception("DataStatsSummarizer error " & datasetStatsSummarizer.ErrorMessage))

                Return False
            Catch ex As Exception
                ReportError("Error creating dataset info file", ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

        End Function

        Public Function GetHeadersForOutputFile(scanList As clsScanList, eOutputFileType As eOutputFileTypeConstants) As String
            Return GetHeadersForOutputFile(scanList, eOutputFileType, ControlChars.Tab)
        End Function

        Public Function GetHeadersForOutputFile(
          scanList As clsScanList, eOutputFileType As eOutputFileTypeConstants, cColDelimiter As Char) As String

            Dim headerNames As List(Of String)

            Select Case eOutputFileType
                Case eOutputFileTypeConstants.ScanStatsFlatFile
                    headerNames = New List(Of String) From {
                        "Dataset",
                        "ScanNumber",
                        "ScanTime",
                        "ScanType",
                        "TotalIonIntensity",
                        "BasePeakIntensity",
                        "BasePeakMZ",
                        "BasePeakSignalToNoiseRatio",
                        "IonCount",
                        "IonCountRaw",
                        "ScanTypeName"
                    }

                Case eOutputFileTypeConstants.ScanStatsExtendedFlatFile

                    If Not ExtendedStatsWriter.ExtendedHeaderNameCount > 0 Then

                        Dim nonConstantHeaderIDs As List(Of Integer) = Nothing

                        ' Lookup extended stats values that are constants for all scans
                        ' The following will also remove the constant header values from htExtendedHeaderInfo
                        ExtendedStatsWriter.ExtractConstantExtendedHeaderValues(nonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, cColDelimiter)

                        headerNames = ExtendedStatsWriter.ConstructExtendedStatsHeaders()
                    Else
                        headerNames = New List(Of String)
                    End If

                Case eOutputFileTypeConstants.SICStatsFlatFile
                    headerNames = New List(Of String) From {
                        "Dataset",
                        "ParentIonIndex",
                        "MZ",
                        "SurveyScanNumber",
                        "FragScanNumber",
                        "OptimalPeakApexScanNumber",
                        "PeakApexOverrideParentIonIndex",
                        "CustomSICPeak",
                        "PeakScanStart",
                        "PeakScanEnd",
                        "PeakScanMaxIntensity",
                        "PeakMaxIntensity",
                        "PeakSignalToNoiseRatio",
                        "FWHMInScans",
                        "PeakArea",
                        "ParentIonIntensity",
                        "PeakBaselineNoiseLevel",
                        "PeakBaselineNoiseStDev",
                        "PeakBaselinePointsUsed",
                        "StatMomentsArea",
                        "CenterOfMassScan",
                        "PeakStDev",
                        "PeakSkew",
                        "PeakKSStat",
                        "StatMomentsDataCountUsed",
                        "InterferenceScore"
                    }

                    If mOptions.IncludeScanTimesInSICStatsFile Then
                        headerNames.Add("SurveyScanTime")
                        headerNames.Add("FragScanTime")
                        headerNames.Add("OptimalPeakApexScanTime")
                    End If

                Case eOutputFileTypeConstants.MRMSettingsFile
                    headerNames = New List(Of String) From {
                        "Parent_Index",
                        "Parent_MZ",
                        "Daughter_MZ",
                        "MZ_Start",
                        "MZ_End",
                        "Scan_Count"
                    }

                Case eOutputFileTypeConstants.MRMDatafile
                    headerNames = New List(Of String) From {
                        "Scan",
                        "MRM_Parent_MZ",
                        "MRM_Daughter_MZ",
                        "MRM_Daughter_Intensity"
                    }

                Case Else
                    headerNames = New List(Of String) From {
                        "Unknown header column names"
                    }
            End Select

            Return String.Join(cColDelimiter, headerNames)
        End Function

        Public Function InitializeSICDetailsTextFile(
          inputFilePathFull As String,
          outputDirectoryPath As String) As Boolean

            Dim outputFilePath As String = String.Empty

            Try

                outputFilePath = ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, eOutputFileTypeConstants.SICDataFile)

                OutputFileHandles.SICDataFile = New StreamWriter(New FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

                ' Write the header line
                OutputFileHandles.SICDataFile.WriteLine("Dataset" & ControlChars.Tab &
                 "ParentIonIndex" & ControlChars.Tab &
                 "FragScanIndex" & ControlChars.Tab &
                 "ParentIonMZ" & ControlChars.Tab &
                 "Scan" & ControlChars.Tab &
                 "MZ" & ControlChars.Tab &
                 "Intensity")

            Catch ex As Exception
                ReportError("Error initializing the XML output file: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Sub OpenOutputFileHandles(
          inputFileName As String,
          outputDirectoryPath As String,
          writeHeaders As Boolean)

            Dim outputFilePath As String

            With OutputFileHandles

                ' Scan Stats file
                outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.ScanStatsFlatFile)
                .ScanStats = New StreamWriter(outputFilePath, False)
                If writeHeaders Then .ScanStats.WriteLine(GetHeadersForOutputFile(Nothing, eOutputFileTypeConstants.ScanStatsFlatFile))

                .MSMethodFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.MSMethodFile)
                .MSTuneFilePathBase = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.MSTuneFile)
            End With

        End Sub

        Public Function SaveHeaderGlossary(
          scanList As clsScanList,
          inputFileName As String,
          outputDirectoryPath As String) As Boolean

            Dim outputFilePath = "?UndefinedFile?"

            Try
                outputFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.HeaderGlossary)
                ReportMessage("Saving Header Glossary to " & Path.GetFileName(outputFilePath))

                Using writer = New StreamWriter(outputFilePath, False)

                    ' ScanStats
                    writer.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.ScanStatsFlatFile) & ":")
                    writer.WriteLine(GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsFlatFile))
                    writer.WriteLine()

                    ' SICStats
                    writer.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.SICStatsFlatFile) & ":")
                    writer.WriteLine(GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.SICStatsFlatFile))
                    writer.WriteLine()

                    ' ScanStatsExtended
                    Dim headers = GetHeadersForOutputFile(scanList, eOutputFileTypeConstants.ScanStatsExtendedFlatFile)
                    If Not String.IsNullOrWhiteSpace(headers) Then
                        writer.WriteLine(ConstructOutputFilePath(String.Empty, String.Empty, eOutputFileTypeConstants.ScanStatsExtendedFlatFile) & ":")
                        writer.WriteLine(headers)
                    End If

                End Using

            Catch ex As Exception
                ReportError("Error writing the Header Glossary to: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Function SaveSICDataToText(
          sicOptions As clsSICOptions,
          scanList As clsScanList,
          parentIonIndex As Integer,
          sicDetails As clsSICDetails) As Boolean


            Dim fragScanIndex As Integer
            Dim prefix As String

            Try

                If OutputFileHandles.SICDataFile Is Nothing Then
                    Return True
                End If

                ' Write the detailed SIC values for the given parent ion to the text file

                For fragScanIndex = 0 To scanList.ParentIons(parentIonIndex).FragScanIndexCount - 1

                    ' "Dataset  ParentIonIndex  FragScanIndex  ParentIonMZ
                    prefix = sicOptions.DatasetNumber.ToString() & ControlChars.Tab &
                       parentIonIndex.ToString() & ControlChars.Tab &
                       fragScanIndex.ToString() & ControlChars.Tab &
                       StringUtilities.DblToString(scanList.ParentIons(parentIonIndex).MZ, 4) & ControlChars.Tab

                    If sicDetails.SICDataCount = 0 Then
                        ' Nothing to write
                        OutputFileHandles.SICDataFile.WriteLine(prefix & "0" & ControlChars.Tab & "0" & ControlChars.Tab & "0")
                    Else
                        For Each dataPoint In sicDetails.SICData
                            OutputFileHandles.SICDataFile.WriteLine(prefix &
                                                                    dataPoint.ScanNumber & ControlChars.Tab &
                                                                    dataPoint.Mass & ControlChars.Tab &
                                                                    dataPoint.Intensity)
                        Next
                    End If

                Next

            Catch ex As Exception
                ReportError("Error writing to detailed SIC data text file", ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

    End Class

End Namespace
