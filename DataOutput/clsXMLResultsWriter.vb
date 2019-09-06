Imports System.Xml
Imports MASIC.clsMASIC
Imports MASIC.DatasetStats
Imports PRISM

Namespace DataOutput

    Public Class clsXMLResultsWriter
        Inherits clsMasicEventNotifier

#Region "Classwide variables"
        Private ReadOnly mOptions As clsMASICOptions
#End Region

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="masicOptions"></param>
        Public Sub New(masicOptions As clsMASICOptions)
            mOptions = masicOptions
        End Sub

        ''' <summary>
        ''' Examines the values in toleranceList
        ''' If all empty and/or all 0, returns an empty string
        ''' </summary>
        ''' <param name="toleranceList">Comma separated list of values</param>
        ''' <returns></returns>
        Private Function CheckForEmptyToleranceList(toleranceList As String) As String

            Dim toleranceValues = toleranceList.Split(","c)
            Dim valuesDefined = False

            For Each value In toleranceValues
                If String.IsNullOrWhiteSpace(value) Then
                    Continue For
                End If

                If value.Trim() = "0" Then
                    Continue For
                End If

                Dim parsedValue As Double
                If Double.TryParse(value, parsedValue) Then
                    If Math.Abs(parsedValue) < Double.Epsilon Then
                        Continue For
                    End If
                End If

                valuesDefined = True
            Next

            If valuesDefined Then
                Return toleranceList
            Else
                Return String.Empty
            End If

        End Function

        Public Function SaveDataToXML(
          scanList As clsScanList,
          parentIonIndex As Integer,
          sicDetails As clsSICDetails,
          smoothedYDataSubset As MASICPeakFinder.clsSmoothedYDataSubset,
          dataOutputHandler As clsDataOutput) As Boolean

            ' Numbers between 0 and 255 that specify the distance (in scans) between each of the data points in SICData(); the first scan number is given by SICScanIndices(0)
            Dim SICDataScanIntervals() As Byte

            Dim lastGoodLoc = "Start"
            Dim intensityDataListWritten As Boolean
            Dim massDataList As Boolean

            Try
                ' Populate udtSICStats.SICDataScanIntervals with the scan intervals between each of the data points

                If sicDetails.SICDataCount = 0 Then
                    ReDim SICDataScanIntervals(0)
                Else
                    ReDim SICDataScanIntervals(sicDetails.SICDataCount - 1)
                    Dim sicScanNumbers = sicDetails.SICScanNumbers

                    For scanIndex = 1 To sicDetails.SICDataCount - 1
                        Dim scanDelta = sicScanNumbers(scanIndex) - sicScanNumbers(scanIndex - 1)
                        ' When storing in SICDataScanIntervals, make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
                        ' However, for MRM data, field size can be much larger
                        SICDataScanIntervals(scanIndex) = CByte(Math.Min(Byte.MaxValue, scanDelta))
                    Next
                End If

                Dim writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs
                If writer Is Nothing Then Return False

                ' Initialize the StringBuilder objects
                Dim sbIntensityDataList = New Text.StringBuilder()
                Dim sbMassDataList = New Text.StringBuilder()
                Dim sbPeakYDataSmoothed = New Text.StringBuilder()

                Dim sicScanIndices = sicDetails.SICScanIndices

                ' Write the SIC's and computed peak stats and areas to the XML file for the given parent ion
                For fragScanIndex = 0 To scanList.ParentIons(parentIonIndex).FragScanIndices.Count - 1
                    lastGoodLoc = "fragScanIndex=" & fragScanIndex.ToString

                    writer.WriteStartElement("ParentIon")
                    writer.WriteAttributeString("Index", parentIonIndex.ToString())             ' Parent ion Index
                    writer.WriteAttributeString("FragScanIndex", fragScanIndex.ToString())      ' Frag Scan Index

                    lastGoodLoc = "currentParentIon = scanList.ParentIons(parentIonIndex)"
                    Dim currentParentIon = scanList.ParentIons(parentIonIndex)

                    writer.WriteElementString("MZ", StringUtilities.DblToString(currentParentIon.MZ, 4))

                    If currentParentIon.SurveyScanIndex >= 0 AndAlso currentParentIon.SurveyScanIndex < scanList.SurveyScans.Count Then
                        writer.WriteElementString("SurveyScanNumber", scanList.SurveyScans(currentParentIon.SurveyScanIndex).ScanNumber.ToString())
                    Else
                        writer.WriteElementString("SurveyScanNumber", "-1")
                    End If

                    lastGoodLoc = "Write FragScanNumber"

                    Dim interferenceScore As Double

                    If fragScanIndex < scanList.FragScans.Count Then
                        Dim currentFragScan = scanList.FragScans(currentParentIon.FragScanIndices(fragScanIndex))
                        writer.WriteElementString("FragScanNumber", currentFragScan.ScanNumber.ToString())
                        writer.WriteElementString("FragScanTime", currentFragScan.ScanTime.ToString())
                        interferenceScore = currentFragScan.FragScanInfo.InterferenceScore
                    Else
                        ' Fragmentation scan does not exist
                        writer.WriteElementString("FragScanNumber", "0")
                        writer.WriteElementString("FragScanTime", "0")
                        interferenceScore = 0
                    End If

                    writer.WriteElementString("OptimalPeakApexScanNumber", currentParentIon.OptimalPeakApexScanNumber.ToString())
                    writer.WriteElementString("PeakApexOverrideParentIonIndex", currentParentIon.PeakApexOverrideParentIonIndex.ToString())
                    writer.WriteElementString("CustomSICPeak", currentParentIon.CustomSICPeak.ToString())

                    If currentParentIon.CustomSICPeak Then
                        writer.WriteElementString("CustomSICPeakComment", currentParentIon.CustomSICPeakComment)
                        writer.WriteElementString("CustomSICPeakMZToleranceDa", currentParentIon.CustomSICPeakMZToleranceDa.ToString())
                        writer.WriteElementString("CustomSICPeakScanTolerance", currentParentIon.CustomSICPeakScanOrAcqTimeTolerance.ToString())
                        writer.WriteElementString("CustomSICPeakScanToleranceType", mOptions.CustomSICList.ScanToleranceType.ToString())
                    End If

                    lastGoodLoc = "sicStatsPeak = currentParentIon.SICStats.Peak"
                    Dim sicStatsPeak = currentParentIon.SICStats.Peak

                    If sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan Then
                        writer.WriteElementString("SICScanType", "FragScan")
                        writer.WriteElementString("PeakScanStart", scanList.FragScans(sicScanIndices(sicStatsPeak.IndexBaseLeft)).ScanNumber.ToString())
                        writer.WriteElementString("PeakScanEnd", scanList.FragScans(sicScanIndices(sicStatsPeak.IndexBaseRight)).ScanNumber.ToString())
                        writer.WriteElementString("PeakScanMaxIntensity", scanList.FragScans(sicScanIndices(sicStatsPeak.IndexMax)).ScanNumber.ToString())
                    Else
                        writer.WriteElementString("SICScanType", "SurveyScan")
                        writer.WriteElementString("PeakScanStart", scanList.SurveyScans(sicScanIndices(sicStatsPeak.IndexBaseLeft)).ScanNumber.ToString())
                        writer.WriteElementString("PeakScanEnd", scanList.SurveyScans(sicScanIndices(sicStatsPeak.IndexBaseRight)).ScanNumber.ToString())
                        writer.WriteElementString("PeakScanMaxIntensity", scanList.SurveyScans(sicScanIndices(sicStatsPeak.IndexMax)).ScanNumber.ToString())
                    End If

                    writer.WriteElementString("PeakIntensity", StringUtilities.ValueToString(sicStatsPeak.MaxIntensityValue, 5))
                    writer.WriteElementString("PeakSignalToNoiseRatio", StringUtilities.ValueToString(sicStatsPeak.SignalToNoiseRatio, 4))
                    writer.WriteElementString("FWHMInScans", sicStatsPeak.FWHMScanWidth.ToString())
                    writer.WriteElementString("PeakArea", StringUtilities.ValueToString(sicStatsPeak.Area, 5))
                    writer.WriteElementString("ShoulderCount", sicStatsPeak.ShoulderCount.ToString())

                    writer.WriteElementString("ParentIonIntensity", StringUtilities.ValueToString(sicStatsPeak.ParentIonIntensity, 5))

                    Dim noiseStats = sicStatsPeak.BaselineNoiseStats
                    writer.WriteElementString("PeakBaselineNoiseLevel", StringUtilities.ValueToString(noiseStats.NoiseLevel, 5))
                    writer.WriteElementString("PeakBaselineNoiseStDev", StringUtilities.ValueToString(noiseStats.NoiseStDev, 3))
                    writer.WriteElementString("PeakBaselinePointsUsed", noiseStats.PointsUsed.ToString())
                    writer.WriteElementString("NoiseThresholdModeUsed", CInt(noiseStats.NoiseThresholdModeUsed).ToString())

                    Dim statMoments = sicStatsPeak.StatisticalMoments

                    writer.WriteElementString("StatMomentsArea", StringUtilities.ValueToString(statMoments.Area, 5))
                    writer.WriteElementString("CenterOfMassScan", statMoments.CenterOfMassScan.ToString())
                    writer.WriteElementString("PeakStDev", StringUtilities.ValueToString(statMoments.StDev, 3))
                    writer.WriteElementString("PeakSkew", StringUtilities.ValueToString(statMoments.Skew, 4))
                    writer.WriteElementString("PeakKSStat", StringUtilities.ValueToString(statMoments.KSStat, 4))
                    writer.WriteElementString("StatMomentsDataCountUsed", statMoments.DataCountUsed.ToString())

                    writer.WriteElementString("InterferenceScore", StringUtilities.ValueToString(interferenceScore, 4))

                    If sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan Then
                        writer.WriteElementString("SICScanStart", scanList.FragScans(sicScanIndices(0)).ScanNumber.ToString())
                    Else
                        writer.WriteElementString("SICScanStart", scanList.SurveyScans(sicScanIndices(0)).ScanNumber.ToString())
                    End If

                    If mOptions.UseBase64DataEncoding Then
                        ' Save scan interval list as base-64 encoded strings
                        lastGoodLoc = "Call SaveDataToXMLEncodeArray with SICScanIntervals"
                        SaveDataToXMLEncodeArray(writer, "SICScanIntervals", SICDataScanIntervals)
                    Else
                        ' Save scan interval list as long list of numbers
                        ' There are no tab delimiters, since we require that all
                        '  of the SICDataScanInterval values be <= 61
                        '   If the interval is <=9, then the interval is stored as a number
                        '   For intervals between 10 and 35, uses letters A to Z
                        '   For intervals between 36 and 61, uses letters A to Z

                        lastGoodLoc = "Populate scanIntervalList"
                        Dim scanIntervalList = String.Empty
                        If Not SICDataScanIntervals Is Nothing Then
                            For scanIntervalIndex = 0 To sicDetails.SICDataCount - 1
                                If SICDataScanIntervals(scanIntervalIndex) <= 9 Then
                                    scanIntervalList &= SICDataScanIntervals(scanIntervalIndex)
                                ElseIf SICDataScanIntervals(scanIntervalIndex) <= 35 Then
                                    scanIntervalList &= Chr(SICDataScanIntervals(scanIntervalIndex) + 55)     ' 55 = -10 + 65
                                ElseIf SICDataScanIntervals(scanIntervalIndex) <= 61 Then
                                    scanIntervalList &= Chr(SICDataScanIntervals(scanIntervalIndex) + 61)     ' 61 = -36 + 97
                                Else
                                    scanIntervalList &= "z"
                                End If
                            Next
                        End If
                        writer.WriteElementString("SICScanIntervals", scanIntervalList)
                    End If

                    lastGoodLoc = "Write SICPeakIndexStart"
                    writer.WriteElementString("SICPeakIndexStart", currentParentIon.SICStats.Peak.IndexBaseLeft.ToString())
                    writer.WriteElementString("SICPeakIndexEnd", currentParentIon.SICStats.Peak.IndexBaseRight.ToString())
                    writer.WriteElementString("SICDataCount", sicDetails.SICDataCount.ToString())

                    If mOptions.SICOptions.SaveSmoothedData Then
                        writer.WriteElementString("SICSmoothedYDataIndexStart", smoothedYDataSubset.DataStartIndex.ToString())
                    End If

                    If mOptions.UseBase64DataEncoding Then

                        ' Save intensity and mass data lists as base-64 encoded strings
                        ' Note that these field names are purposely different than the DataList names used below for comma separated lists
                        lastGoodLoc = "Call SaveDataToXMLEncodeArray with SICIntensityData"
                        SaveDataToXMLEncodeArray(writer, "SICIntensityData", sicDetails.SICIntensitiesAsFloat)

                        lastGoodLoc = "Call SaveDataToXMLEncodeArray with SICMassData"
                        SaveDataToXMLEncodeArray(writer, "SICMassData", sicDetails.SICMassesAsFloat)

                        If mOptions.SICOptions.SaveSmoothedData Then
                            ' Need to copy the data into an array with the correct number of elements
                            Dim dataArray As Single()
                            ReDim dataArray(smoothedYDataSubset.DataCount - 1)
                            Array.Copy(smoothedYDataSubset.Data, dataArray, smoothedYDataSubset.DataCount)

                            SaveDataToXMLEncodeArray(writer, "SICSmoothedYData", dataArray)
                        End If
                    Else
                        ' Save intensity and mass data lists as tab-delimited text list

                        intensityDataListWritten = False
                        massDataList = False

                        Try
                            lastGoodLoc = "Populate sbIntensityDataList"
                            sbIntensityDataList.Length = 0
                            sbMassDataList.Length = 0

                            If sicDetails.SICDataCount > 0 Then
                                For Each dataPoint In sicDetails.SICData
                                    If dataPoint.Intensity > 0 Then
                                        sbIntensityDataList.Append(StringUtilities.DblToString(dataPoint.Intensity, 1) & ",")
                                    Else
                                        sbIntensityDataList.Append(","c)     ' Do not output any number if the intensity is 0
                                    End If

                                    If dataPoint.Mass > 0 Then
                                        sbMassDataList.Append(StringUtilities.DblToString(dataPoint.Mass, 3) & ",")
                                    Else
                                        sbMassDataList.Append(","c)     ' Do not output any number if the mass is 0
                                    End If
                                Next

                                ' Trim the trailing comma
                                If sbIntensityDataList.Chars(sbIntensityDataList.Length - 1) = ","c Then
                                    sbIntensityDataList.Length -= 1
                                    sbMassDataList.Length -= 1
                                End If

                            End If

                            writer.WriteElementString("IntensityDataList", sbIntensityDataList.ToString())
                            intensityDataListWritten = True

                            writer.WriteElementString("MassDataList", sbMassDataList.ToString())
                            massDataList = True

                        Catch ex As OutOfMemoryException
                            ' Ignore the exception if this is an Out of Memory exception

                            If Not intensityDataListWritten Then
                                writer.WriteElementString("IntensityDataList", String.Empty)
                            End If

                            If Not massDataList Then
                                writer.WriteElementString("MassDataList", String.Empty)
                            End If

                        End Try

                        If mOptions.SICOptions.SaveSmoothedData Then
                            Try
                                lastGoodLoc = "Populate sbPeakYDataSmoothed"
                                sbPeakYDataSmoothed.Length = 0

                                If Not smoothedYDataSubset.Data Is Nothing AndAlso smoothedYDataSubset.DataCount > 0 Then
                                    For index = 0 To smoothedYDataSubset.DataCount - 1
                                        sbPeakYDataSmoothed.Append(Math.Round(smoothedYDataSubset.Data(index)).ToString() & ",")
                                    Next

                                    ' Trim the trailing comma
                                    sbPeakYDataSmoothed.Length -= 1
                                End If

                                writer.WriteElementString("SmoothedYDataList", sbPeakYDataSmoothed.ToString())

                            Catch ex As OutOfMemoryException
                                ' Ignore the exception if this is an Out of Memory exception
                                writer.WriteElementString("SmoothedYDataList", String.Empty)
                            End Try

                        End If

                    End If

                    writer.WriteEndElement()
                Next

            Catch ex As Exception
                ReportError("Error writing the XML data to the output file; Last good location: " & lastGoodLoc, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Private Sub SaveDataToXMLEncodeArray(
          writer As XmlWriter,
          elementName As String,
          dataArray() As Byte)

            Dim precisionBits As Integer
            Dim dataTypeName As String = String.Empty

            Dim encodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, precisionBits, dataTypeName)

            writer.WriteStartElement(elementName)
            writer.WriteAttributeString("precision", precisionBits.ToString())        ' Store the precision, in bits
            writer.WriteAttributeString("type", dataTypeName)
            writer.WriteString(encodedValues)
            writer.WriteEndElement()

        End Sub

        Private Sub SaveDataToXMLEncodeArray(
          writer As XmlWriter,
          elementName As String,
          dataArray() As Single)

            Dim precisionBits As Integer
            Dim dataTypeName As String = String.Empty

            Dim encodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, precisionBits, dataTypeName)

            writer.WriteStartElement(elementName)
            writer.WriteAttributeString("precision", precisionBits.ToString())        ' Store the precision, in bits
            writer.WriteAttributeString("type", dataTypeName)
            writer.WriteString(encodedValues)
            writer.WriteEndElement()

        End Sub

        Public Function XMLOutputFileFinalize(
           dataOutputHandler As clsDataOutput,
           scanList As clsScanList,
           spectraCache As clsSpectraCache,
           processingStats As clsProcessingStats,
           processingTimeSec As Single) As Boolean


            Dim writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs
            If writer Is Nothing Then Return False

            Try
                writer.WriteStartElement("ProcessingStats")
                With spectraCache
                    writer.WriteElementString("CacheEventCount", .CacheEventCount.ToString())
                    writer.WriteElementString("UnCacheEventCount", .UnCacheEventCount.ToString())
                End With

                With processingStats
                    writer.WriteElementString("PeakMemoryUsageMB", StringUtilities.DblToString(.PeakMemoryUsageMB, 2))
                    Dim effectiveSeconds = processingTimeSec - .TotalProcessingTimeAtStart
                    writer.WriteElementString("TotalProcessingTimeSeconds", StringUtilities.DblToString(effectiveSeconds, 2))
                End With
                writer.WriteEndElement()

                If scanList.ProcessingIncomplete Then
                    writer.WriteElementString("ProcessingComplete", "False")
                Else
                    writer.WriteElementString("ProcessingComplete", "True")
                End If

                writer.WriteEndElement()     ' Close out the <SICData> element
                writer.WriteEndDocument()
                writer.Close()

            Catch ex As Exception
                ReportError("Error finalizing the XML output file", ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Function XMLOutputFileInitialize(
          inputFilePathFull As String,
          outputDirectoryPath As String,
          dataOutputHandler As clsDataOutput,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          sicOptions As clsSICOptions,
          binningOptions As clsBinningOptions) As Boolean

            Dim xmlOutputFilePath = String.Empty

            Try

                xmlOutputFilePath = clsDataOutput.ConstructOutputFilePath(inputFilePathFull, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.XMLFile)

                dataOutputHandler.OutputFileHandles.XMLFileForSICs = New Xml.XmlTextWriter(xmlOutputFilePath, Text.Encoding.UTF8)
                Dim writer = dataOutputHandler.OutputFileHandles.XMLFileForSICs
                writer.Formatting = Xml.Formatting.Indented
                writer.Indentation = 1

                writer.WriteStartDocument(True)
                writer.WriteStartElement("SICData")

                writer.WriteStartElement("ProcessingSummary")
                writer.WriteElementString("DatasetID", sicOptions.DatasetID.ToString())
                writer.WriteElementString("SourceFilePath", inputFilePathFull)

                Dim lastModTimeText As String
                Dim fileSizeBytes As String

                Try
                    Dim inputFileInfo = New FileInfo(inputFilePathFull)
                    Dim lastModTime = inputFileInfo.LastWriteTime()
                    lastModTimeText = lastModTime.ToShortDateString() & " " & lastModTime.ToShortTimeString()
                    fileSizeBytes = inputFileInfo.Length.ToString()
                Catch ex As Exception
                    lastModTimeText = String.Empty
                    fileSizeBytes = "0"
                End Try

                writer.WriteElementString("SourceFileDateTime", lastModTimeText)
                writer.WriteElementString("SourceFileSizeBytes", fileSizeBytes)

                writer.WriteElementString("MASICProcessingDate", DateTime.Now.ToString(clsDatasetStatsSummarizer.DATE_TIME_FORMAT_STRING))
                writer.WriteElementString("MASICVersion", mOptions.MASICVersion)
                writer.WriteElementString("MASICPeakFinderDllVersion", mOptions.PeakFinderVersion)
                writer.WriteElementString("ScanCountTotal", scanList.MasterScanOrderCount.ToString())
                writer.WriteElementString("SurveyScanCount", scanList.SurveyScans.Count.ToString())
                writer.WriteElementString("FragScanCount", scanList.FragScans.Count.ToString())
                writer.WriteElementString("SkipMSMSProcessing", mOptions.SkipMSMSProcessing.ToString())

                writer.WriteElementString("ParentIonDecoyMassDa", mOptions.ParentIonDecoyMassDa.ToString("0.0000"))

                writer.WriteEndElement()

                writer.WriteStartElement("MemoryOptions")
                With spectraCache

                    writer.WriteElementString("CacheAlwaysDisabled", .DiskCachingAlwaysDisabled.ToString())
                    writer.WriteElementString("CacheSpectraToRetainInMemory", .CacheSpectraToRetainInMemory.ToString())

                End With
                writer.WriteEndElement()


                writer.WriteStartElement("SICOptions")
                With sicOptions
                    ' SIC Options

                    ' "SICToleranceDa" is a legacy parameter; If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                    writer.WriteElementString("SICToleranceDa", clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 1000).ToString("0.0000"))

                    writer.WriteElementString("SICTolerance", .SICTolerance.ToString("0.0000"))
                    writer.WriteElementString("SICToleranceIsPPM", .SICToleranceIsPPM.ToString())

                    writer.WriteElementString("RefineReportedParentIonMZ", .RefineReportedParentIonMZ.ToString())

                    writer.WriteElementString("ScanRangeStart", .ScanRangeStart.ToString())
                    writer.WriteElementString("ScanRangeEnd", .ScanRangeEnd.ToString())
                    writer.WriteElementString("RTRangeStart", .RTRangeStart.ToString())
                    writer.WriteElementString("RTRangeEnd", .RTRangeEnd.ToString())

                    writer.WriteElementString("CompressMSSpectraData", .CompressMSSpectraData.ToString())
                    writer.WriteElementString("CompressMSMSSpectraData", .CompressMSMSSpectraData.ToString())

                    writer.WriteElementString("CompressToleranceDivisorForDa", .CompressToleranceDivisorForDa.ToString("0.0"))
                    writer.WriteElementString("CompressToleranceDivisorForPPM", .CompressToleranceDivisorForPPM.ToString("0.0"))

                    writer.WriteElementString("MaxSICPeakWidthMinutesBackward", .MaxSICPeakWidthMinutesBackward.ToString())
                    writer.WriteElementString("MaxSICPeakWidthMinutesForward", .MaxSICPeakWidthMinutesForward.ToString())

                    With .SICPeakFinderOptions
                        writer.WriteElementString("IntensityThresholdFractionMax", .IntensityThresholdFractionMax.ToString())
                        writer.WriteElementString("IntensityThresholdAbsoluteMinimum", .IntensityThresholdAbsoluteMinimum.ToString())

                        ' Peak Finding Options
                        With .SICBaselineNoiseOptions
                            writer.WriteElementString("SICNoiseThresholdMode", .BaselineNoiseMode.ToString())
                            writer.WriteElementString("SICNoiseThresholdIntensity", .BaselineNoiseLevelAbsolute.ToString())
                            writer.WriteElementString("SICNoiseFractionLowIntensityDataToAverage", .TrimmedMeanFractionLowIntensityDataToAverage.ToString())
                            writer.WriteElementString("SICNoiseMinimumSignalToNoiseRatio", .MinimumSignalToNoiseRatio.ToString())
                        End With

                        writer.WriteElementString("MaxDistanceScansNoOverlap", .MaxDistanceScansNoOverlap.ToString())
                        writer.WriteElementString("MaxAllowedUpwardSpikeFractionMax", .MaxAllowedUpwardSpikeFractionMax.ToString())
                        writer.WriteElementString("InitialPeakWidthScansScaler", .InitialPeakWidthScansScaler.ToString())
                        writer.WriteElementString("InitialPeakWidthScansMaximum", .InitialPeakWidthScansMaximum.ToString())

                        writer.WriteElementString("FindPeaksOnSmoothedData", .FindPeaksOnSmoothedData.ToString())
                        writer.WriteElementString("SmoothDataRegardlessOfMinimumPeakWidth", .SmoothDataRegardlessOfMinimumPeakWidth.ToString())
                        writer.WriteElementString("UseButterworthSmooth", .UseButterworthSmooth.ToString())
                        writer.WriteElementString("ButterworthSamplingFrequency", .ButterworthSamplingFrequency.ToString())
                        writer.WriteElementString("ButterworthSamplingFrequencyDoubledForSIMData", .ButterworthSamplingFrequencyDoubledForSIMData.ToString())

                        writer.WriteElementString("UseSavitzkyGolaySmooth", .UseSavitzkyGolaySmooth.ToString())
                        writer.WriteElementString("SavitzkyGolayFilterOrder", .SavitzkyGolayFilterOrder.ToString())

                        With .MassSpectraNoiseThresholdOptions
                            writer.WriteElementString("MassSpectraNoiseThresholdMode", .BaselineNoiseMode.ToString())
                            writer.WriteElementString("MassSpectraNoiseThresholdIntensity", .BaselineNoiseLevelAbsolute.ToString())
                            writer.WriteElementString("MassSpectraNoiseFractionLowIntensityDataToAverage", .TrimmedMeanFractionLowIntensityDataToAverage.ToString())
                            writer.WriteElementString("MassSpectraNoiseMinimumSignalToNoiseRatio", .MinimumSignalToNoiseRatio.ToString())
                        End With
                    End With

                    writer.WriteElementString("ReplaceSICZeroesWithMinimumPositiveValueFromMSData", .ReplaceSICZeroesWithMinimumPositiveValueFromMSData.ToString())
                    writer.WriteElementString("SaveSmoothedData", .SaveSmoothedData.ToString())

                    ' Similarity options
                    writer.WriteElementString("SimilarIonMZToleranceHalfWidth", .SimilarIonMZToleranceHalfWidth.ToString())
                    writer.WriteElementString("SimilarIonToleranceHalfWidthMinutes", .SimilarIonToleranceHalfWidthMinutes.ToString())
                    writer.WriteElementString("SpectrumSimilarityMinimum", .SpectrumSimilarityMinimum.ToString())
                End With
                writer.WriteEndElement()

                writer.WriteStartElement("BinningOptions")
                With binningOptions
                    writer.WriteElementString("BinStartX", .StartX.ToString())
                    writer.WriteElementString("BinEndX", .EndX.ToString())
                    writer.WriteElementString("BinSize", .BinSize.ToString())
                    writer.WriteElementString("MaximumBinCount", .MaximumBinCount.ToString())

                    writer.WriteElementString("IntensityPrecisionPercent", .IntensityPrecisionPercent.ToString())
                    writer.WriteElementString("Normalize", .Normalize.ToString())
                    writer.WriteElementString("SumAllIntensitiesForBin", .SumAllIntensitiesForBin.ToString())

                End With
                writer.WriteEndElement()

                writer.WriteStartElement("CustomSICValues")
                With mOptions.CustomSICList
                    writer.WriteElementString("MZList", .RawTextMZList)
                    writer.WriteElementString("MZToleranceDaList", CheckForEmptyToleranceList(.RawTextMZToleranceDaList))
                    writer.WriteElementString("ScanCenterList", .RawTextScanOrAcqTimeCenterList)
                    writer.WriteElementString("ScanToleranceList", CheckForEmptyToleranceList(.RawTextScanOrAcqTimeToleranceList))
                    writer.WriteElementString("ScanTolerance", .ScanOrAcqTimeTolerance.ToString())
                    writer.WriteElementString("ScanType", .ScanToleranceType.ToString())
                    writer.WriteElementString("LimitSearchToCustomMZList", .LimitSearchToCustomMZList.ToString())
                End With
                writer.WriteEndElement()


            Catch ex As Exception
                ReportError("Error initializing the XML output file: " & xmlOutputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Private Sub XmlOutputFileReplaceSetting(
          writer As TextWriter,
          lineIn As String,
          xmlElementName As String,
          newValueToSave As Integer)

            ' xmlElementName should be the properly capitalized element name and should not start with "<"

            Dim work As String
            Dim charIndex As Integer
            Dim currentValue As Integer

            ' Need to add two since xmlElementName doesn't include "<" at the beginning
            work = lineIn.Trim.ToLower().Substring(xmlElementName.Length + 2)

            ' Look for the "<" after the number
            charIndex = work.IndexOf("<", StringComparison.Ordinal)
            If charIndex > 0 Then
                ' Isolate the number
                work = work.Substring(0, charIndex)
                If clsUtilities.IsNumber(work) Then
                    currentValue = CInt(work)

                    If newValueToSave <> currentValue Then
                        lineIn = "  <" & xmlElementName & ">"
                        lineIn &= newValueToSave.ToString
                        lineIn &= "</" & xmlElementName & ">"

                    End If
                End If
            End If

            writer.WriteLine(lineIn)

        End Sub

        Public Function XmlOutputFileUpdateEntries(
          scanList As clsScanList,
          inputFileName As String,
          outputDirectoryPath As String) As Boolean

            ' ReSharper disable once StringLiteralTypo
            Const PARENT_ION_TAG_START_LCASE = "<parention"     ' Note: this needs to be lowercase
            Const INDEX_ATTRIBUTE_LCASE = "index="              ' Note: this needs to be lowercase

            Const OPTIMAL_PEAK_APEX_TAG_NAME = "OptimalPeakApexScanNumber"
            Const PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME = "PeakApexOverrideParentIonIndex"

            Dim xmlReadFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.XMLFile)

            Dim xmlOutputFilePath = Path.Combine(outputDirectoryPath, "__temp__MASICOutputFile.xml")

            Try
                ' Wait 2 seconds before reopening the file, to make sure the handle is closed
                Threading.Thread.Sleep(2000)

                If Not File.Exists(xmlReadFilePath) Then
                    ' XML file not found, exit the function
                    Return True
                End If

                Using reader = New StreamReader(xmlReadFilePath),
                  writer = New StreamWriter(xmlOutputFilePath, False)

                    UpdateProgress(0, "Updating XML file with optimal peak apex values")

                    Dim parentIonIndex = -1
                    Dim parentIonsProcessed = 0
                    Do While Not reader.EndOfStream
                        Dim dataLine = reader.ReadLine()
                        If dataLine Is Nothing Then Continue Do

                        Dim dataLineLCase = dataLine.Trim().ToLower()

                        If dataLineLCase.StartsWith(PARENT_ION_TAG_START_LCASE) Then
                            Dim charIndex = dataLineLCase.IndexOf(INDEX_ATTRIBUTE_LCASE, StringComparison.CurrentCultureIgnoreCase)
                            If charIndex > 0 Then
                                Dim work = dataLineLCase.Substring(charIndex + INDEX_ATTRIBUTE_LCASE.Length + 1)
                                charIndex = work.IndexOf(ControlChars.Quote)
                                If charIndex > 0 Then
                                    work = work.Substring(0, charIndex)
                                    If clsUtilities.IsNumber(work) Then
                                        parentIonIndex = CInt(work)
                                        parentIonsProcessed += 1

                                        ' Update progress
                                        If scanList.ParentIons.Count > 1 Then
                                            If parentIonsProcessed Mod 100 = 0 Then
                                                UpdateProgress(CShort(parentIonsProcessed / (scanList.ParentIons.Count - 1) * 100))
                                            End If
                                        Else
                                            UpdateProgress(0)
                                        End If

                                        If mOptions.AbortProcessing Then
                                            scanList.ProcessingIncomplete = True
                                            Exit Do
                                        End If

                                    End If
                                End If
                            End If

                            writer.WriteLine(dataLine)

                        ElseIf dataLineLCase.StartsWith("<" & OPTIMAL_PEAK_APEX_TAG_NAME.ToLower) AndAlso parentIonIndex >= 0 Then
                            If parentIonIndex < scanList.ParentIons.Count Then
                                XmlOutputFileReplaceSetting(writer, dataLine, OPTIMAL_PEAK_APEX_TAG_NAME, scanList.ParentIons(parentIonIndex).OptimalPeakApexScanNumber)
                            End If
                        ElseIf dataLineLCase.StartsWith("<" & PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME.ToLower) AndAlso parentIonIndex >= 0 Then
                            If parentIonIndex < scanList.ParentIons.Count Then
                                XmlOutputFileReplaceSetting(writer, dataLine, PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME, scanList.ParentIons(parentIonIndex).PeakApexOverrideParentIonIndex)
                            End If
                        Else
                            writer.WriteLine(dataLine)
                        End If

                    Loop

                End Using

                Try
                    ' Wait 2 seconds, then delete the original file and rename the temp one to the original one
                    Threading.Thread.Sleep(2000)

                    If File.Exists(xmlOutputFilePath) Then
                        If File.Exists(xmlReadFilePath) Then
                            File.Delete(xmlReadFilePath)
                            Threading.Thread.Sleep(500)
                        End If

                        File.Move(xmlOutputFilePath, xmlReadFilePath)
                    End If

                Catch ex As Exception
                    ReportError("Error renaming XML output file from temp name to: " & xmlReadFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                    Return False
                End Try

                UpdateProgress(100)

#If GUI Then
                System.Windows.Forms.Application.DoEvents()
#End If
            Catch ex As Exception
                ReportError("Error updating the XML output file: " & xmlReadFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

    End Class

End Namespace