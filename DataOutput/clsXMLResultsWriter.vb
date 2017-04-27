Imports MASIC.clsMASIC
Imports PNNLOmics.Utilities

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

                Dim dblValue As Double
                If Double.TryParse(value, dblValue) Then
                    If Math.Abs(dblValue) < Double.Epsilon Then
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
          intParentIonIndex As Integer,
          sicDetails As clsSICDetails,
          smoothedYDataSubset As MASICPeakFinder.clsSmoothedYDataSubset,
          dataOutputHandler As clsDataOutput) As Boolean

            ' Numbers between 0 and 255 that specify the distance (in scans) between each of the data points in SICData(); the first scan number is given by SICScanIndices(0)
            Dim SICDataScanIntervals() As Byte

            Dim strLastGoodLoc = "Start"
            Dim blnIntensityDataListWritten As Boolean
            Dim blnMassDataList As Boolean

            Try
                ' Populate udtSICStats.SICDataScanIntervals with the scan intervals between each of the data points

                If sicDetails.SICDataCount = 0 Then
                    ReDim SICDataScanIntervals(0)
                Else
                    ReDim SICDataScanIntervals(sicDetails.SICDataCount - 1)
                    Dim sicScanNumbers = sicDetails.SICScanNumbers

                    For intScanIndex = 1 To sicDetails.SICDataCount - 1
                        Dim intScanDelta = sicScanNumbers(intScanIndex) - sicScanNumbers(intScanIndex - 1)
                        ' When storing in SICDataScanIntervals, make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
                        ' However, for MRM data, field size can be much larger
                        SICDataScanIntervals(intScanIndex) = CByte(Math.Min(Byte.MaxValue, intScanDelta))
                    Next
                End If


                Dim objXMLOut = dataOutputHandler.OutputFileHandles.XMLFileForSICs
                If objXMLOut Is Nothing Then Return False

                ' Initialize the StringBuilder objects
                Dim sbIntensityDataList = New Text.StringBuilder()
                Dim sbMassDataList = New Text.StringBuilder()
                Dim sbPeakYDataSmoothed = New Text.StringBuilder()

                Dim sicScanIndices = sicDetails.SICScanIndices

                ' Write the SIC's and computed peak stats and areas to the XML file for the given parent ion
                For intFragScanIndex = 0 To scanList.ParentIons(intParentIonIndex).FragScanIndexCount - 1
                    strLastGoodLoc = "intFragScanIndex=" & intFragScanIndex.ToString

                    objXMLOut.WriteStartElement("ParentIon")
                    objXMLOut.WriteAttributeString("Index", intParentIonIndex.ToString())             ' Parent ion Index
                    objXMLOut.WriteAttributeString("FragScanIndex", intFragScanIndex.ToString())      ' Frag Scan Index

                    strLastGoodLoc = "With scanList.ParentIons(intParentIonIndex)"
                    With scanList.ParentIons(intParentIonIndex)
                        objXMLOut.WriteElementString("MZ", Math.Round(.MZ, 4).ToString())

                        If .SurveyScanIndex >= 0 AndAlso .SurveyScanIndex < scanList.SurveyScans.Count Then
                            objXMLOut.WriteElementString("SurveyScanNumber", scanList.SurveyScans(.SurveyScanIndex).ScanNumber.ToString())
                        Else
                            objXMLOut.WriteElementString("SurveyScanNumber", "-1")
                        End If

                        strLastGoodLoc = "Write FragScanNumber"

                        Dim interferenceScore As Double

                        If intFragScanIndex < scanList.FragScans.Count Then
                            objXMLOut.WriteElementString("FragScanNumber", scanList.FragScans(.FragScanIndices(intFragScanIndex)).ScanNumber.ToString())
                            objXMLOut.WriteElementString("FragScanTime", scanList.FragScans(.FragScanIndices(intFragScanIndex)).ScanTime.ToString())
                            interferenceScore = scanList.FragScans(.FragScanIndices(intFragScanIndex)).FragScanInfo.InteferenceScore
                        Else
                            ' Fragmentation scan does not exist
                            objXMLOut.WriteElementString("FragScanNumber", "0")
                            objXMLOut.WriteElementString("FragScanTime", "0")
                            interferenceScore = 0
                        End If

                        objXMLOut.WriteElementString("OptimalPeakApexScanNumber", .OptimalPeakApexScanNumber.ToString())
                        objXMLOut.WriteElementString("PeakApexOverrideParentIonIndex", .PeakApexOverrideParentIonIndex.ToString())
                        objXMLOut.WriteElementString("CustomSICPeak", .CustomSICPeak.ToString())

                        If .CustomSICPeak Then
                            objXMLOut.WriteElementString("CustomSICPeakComment", .CustomSICPeakComment)
                            objXMLOut.WriteElementString("CustomSICPeakMZToleranceDa", .CustomSICPeakMZToleranceDa.ToString())
                            objXMLOut.WriteElementString("CustomSICPeakScanTolerance", .CustomSICPeakScanOrAcqTimeTolerance.ToString())
                            objXMLOut.WriteElementString("CustomSICPeakScanToleranceType", mOptions.CustomSICList.ScanToleranceType.ToString())
                        End If

                        strLastGoodLoc = "With .SICStats"
                        With .SICStats
                            With .Peak
                                If sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan Then
                                    objXMLOut.WriteElementString("SICScanType", "FragScan")
                                    objXMLOut.WriteElementString("PeakScanStart", scanList.FragScans(sicScanIndices(.IndexBaseLeft)).ScanNumber.ToString())
                                    objXMLOut.WriteElementString("PeakScanEnd", scanList.FragScans(sicScanIndices(.IndexBaseRight)).ScanNumber.ToString())
                                    objXMLOut.WriteElementString("PeakScanMaxIntensity", scanList.FragScans(sicScanIndices(.IndexMax)).ScanNumber.ToString())
                                Else
                                    objXMLOut.WriteElementString("SICScanType", "SurveyScan")
                                    objXMLOut.WriteElementString("PeakScanStart", scanList.SurveyScans(sicScanIndices(.IndexBaseLeft)).ScanNumber.ToString())
                                    objXMLOut.WriteElementString("PeakScanEnd", scanList.SurveyScans(sicScanIndices(.IndexBaseRight)).ScanNumber.ToString())
                                    objXMLOut.WriteElementString("PeakScanMaxIntensity", scanList.SurveyScans(sicScanIndices(.IndexMax)).ScanNumber.ToString())
                                End If

                                objXMLOut.WriteElementString("PeakIntensity", StringUtilities.ValueToString(.MaxIntensityValue, 5))
                                objXMLOut.WriteElementString("PeakSignalToNoiseRatio", StringUtilities.ValueToString(.SignalToNoiseRatio, 4))
                                objXMLOut.WriteElementString("FWHMInScans", .FWHMScanWidth.ToString())
                                objXMLOut.WriteElementString("PeakArea", StringUtilities.ValueToString(.Area, 5))
                                objXMLOut.WriteElementString("ShoulderCount", .ShoulderCount.ToString())

                                objXMLOut.WriteElementString("ParentIonIntensity", StringUtilities.ValueToString(.ParentIonIntensity, 5))

                                With .BaselineNoiseStats
                                    objXMLOut.WriteElementString("PeakBaselineNoiseLevel", StringUtilities.ValueToString(.NoiseLevel, 5))
                                    objXMLOut.WriteElementString("PeakBaselineNoiseStDev", StringUtilities.ValueToString(.NoiseStDev, 3))
                                    objXMLOut.WriteElementString("PeakBaselinePointsUsed", .PointsUsed.ToString())
                                    objXMLOut.WriteElementString("NoiseThresholdModeUsed", CInt(.NoiseThresholdModeUsed).ToString())
                                End With

                                With .StatisticalMoments
                                    objXMLOut.WriteElementString("StatMomentsArea", StringUtilities.ValueToString(.Area, 5))
                                    objXMLOut.WriteElementString("CenterOfMassScan", .CenterOfMassScan.ToString())
                                    objXMLOut.WriteElementString("PeakStDev", StringUtilities.ValueToString(.StDev, 3))
                                    objXMLOut.WriteElementString("PeakSkew", StringUtilities.ValueToString(.Skew, 4))
                                    objXMLOut.WriteElementString("PeakKSStat", StringUtilities.ValueToString(.KSStat, 4))
                                    objXMLOut.WriteElementString("StatMomentsDataCountUsed", .DataCountUsed.ToString())
                                End With

                                objXMLOut.WriteElementString("InterferenceScore", StringUtilities.ValueToString(interferenceScore, 4))
                            End With

                            If sicDetails.SICScanType = clsScanList.eScanTypeConstants.FragScan Then
                                objXMLOut.WriteElementString("SICScanStart", scanList.FragScans(sicScanIndices(0)).ScanNumber.ToString())
                            Else
                                objXMLOut.WriteElementString("SICScanStart", scanList.SurveyScans(sicScanIndices(0)).ScanNumber.ToString())
                            End If

                            If mOptions.UseBase64DataEncoding Then
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
                                Dim strScanIntervalList = String.Empty
                                If Not SICDataScanIntervals Is Nothing Then
                                    For intScanIntervalIndex = 0 To sicDetails.SICDataCount - 1
                                        If SICDataScanIntervals(intScanIntervalIndex) <= 9 Then
                                            strScanIntervalList &= SICDataScanIntervals(intScanIntervalIndex)
                                        ElseIf SICDataScanIntervals(intScanIntervalIndex) <= 35 Then
                                            strScanIntervalList &= Chr(SICDataScanIntervals(intScanIntervalIndex) + 55)     ' 55 = -10 + 65
                                        ElseIf SICDataScanIntervals(intScanIntervalIndex) <= 61 Then
                                            strScanIntervalList &= Chr(SICDataScanIntervals(intScanIntervalIndex) + 61)     ' 61 = -36 + 97
                                        Else
                                            strScanIntervalList &= "z"
                                        End If
                                    Next
                                End If
                                objXMLOut.WriteElementString("SICScanIntervals", strScanIntervalList)
                            End If

                            strLastGoodLoc = "Write SICPeakIndexStart"
                            objXMLOut.WriteElementString("SICPeakIndexStart", .Peak.IndexBaseLeft.ToString())
                            objXMLOut.WriteElementString("SICPeakIndexEnd", .Peak.IndexBaseRight.ToString())
                            objXMLOut.WriteElementString("SICDataCount", sicDetails.SICDataCount.ToString())

                            If mOptions.SICOptions.SaveSmoothedData Then
                                objXMLOut.WriteElementString("SICSmoothedYDataIndexStart", smoothedYDataSubset.DataStartIndex.ToString())
                            End If

                            If mOptions.UseBase64DataEncoding Then

                                ' Save intensity and mass data lists as base-64 encoded strings
                                ' Note that these field names are purposely different than the DataList names used below for comma separated lists
                                strLastGoodLoc = "Call SaveDataToXMLEncodeArray with SICIntensityData"
                                SaveDataToXMLEncodeArray(objXMLOut, "SICIntensityData", sicDetails.SICIntensities)

                                strLastGoodLoc = "Call SaveDataToXMLEncodeArray with SICMassData"
                                SaveDataToXMLEncodeArray(objXMLOut, "SICMassData", sicDetails.SICMassesAsFloat)

                                If mOptions.SICOptions.SaveSmoothedData Then
                                    ' Need to copy the data into an array with the correct number of elements
                                    Dim dataArray As Single()
                                    ReDim dataArray(smoothedYDataSubset.DataCount - 1)
                                    Array.Copy(smoothedYDataSubset.Data, dataArray, smoothedYDataSubset.DataCount)

                                    SaveDataToXMLEncodeArray(objXMLOut, "SICSmoothedYData", dataArray)
                                End If
                            Else
                                ' Save intensity and mass data lists as tab-delimited text list

                                blnIntensityDataListWritten = False
                                blnMassDataList = False

                                Try
                                    strLastGoodLoc = "Populate sbIntensityDataList"
                                    sbIntensityDataList.Length = 0
                                    sbMassDataList.Length = 0

                                    If sicDetails.SICDataCount > 0 Then
                                        For Each dataPoint In sicDetails.SICData
                                            If dataPoint.Intensity > 0 Then
                                                sbIntensityDataList.Append(Math.Round(dataPoint.Intensity, 1).ToString() & ",")
                                            Else
                                                sbIntensityDataList.Append(","c)     ' Do not output any number if the intensity is 0
                                            End If

                                            If dataPoint.Mass > 0 Then
                                                sbMassDataList.Append(Math.Round(dataPoint.Mass, 3).ToString() & ",")
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

                                    objXMLOut.WriteElementString("IntensityDataList", sbIntensityDataList.ToString())
                                    blnIntensityDataListWritten = True

                                    objXMLOut.WriteElementString("MassDataList", sbMassDataList.ToString())
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

                                If mOptions.SICOptions.SaveSmoothedData Then
                                    Try
                                        strLastGoodLoc = "Populate sbPeakYDataSmoothed"
                                        sbPeakYDataSmoothed.Length = 0

                                        If Not smoothedYDataSubset.Data Is Nothing AndAlso smoothedYDataSubset.DataCount > 0 Then
                                            For intIndex = 0 To smoothedYDataSubset.DataCount - 1
                                                sbPeakYDataSmoothed.Append(Math.Round(smoothedYDataSubset.Data(intIndex)).ToString() & ",")
                                            Next

                                            ' Trim the trailing comma
                                            sbPeakYDataSmoothed.Length -= 1
                                        End If

                                        objXMLOut.WriteElementString("SmoothedYDataList", sbPeakYDataSmoothed.ToString())

                                    Catch ex As OutOfMemoryException
                                        ' Ignore the exception if this is an Out of Memory exception
                                        objXMLOut.WriteElementString("SmoothedYDataList", "")
                                    End Try

                                End If

                            End If

                        End With
                    End With
                    objXMLOut.WriteEndElement()
                Next

            Catch ex As Exception
                ReportError("Error writing the XML data to the output file; Last good location: " & strLastGoodLoc, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Private Sub SaveDataToXMLEncodeArray(
          objXMLOut As Xml.XmlTextWriter,
          strElementName As String,
          dataArray() As Byte)

            Dim intPrecisionBits As Integer
            Dim strDataTypeName As String = String.Empty

            Dim strEncodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, intPrecisionBits, strDataTypeName)

            With objXMLOut
                .WriteStartElement(strElementName)
                .WriteAttributeString("precision", intPrecisionBits.ToString())        ' Store the precision, in bits
                .WriteAttributeString("type", strDataTypeName)
                .WriteString(strEncodedValues)
                .WriteEndElement()
            End With

        End Sub

        Private Sub SaveDataToXMLEncodeArray(
          objXMLOut As Xml.XmlTextWriter,
          strElementName As String,
          dataArray() As Single)

            Dim intPrecisionBits As Integer
            Dim strDataTypeName As String = String.Empty

            Dim strEncodedValues = MSDataFileReader.clsBase64EncodeDecode.EncodeNumericArray(dataArray, intPrecisionBits, strDataTypeName)

            With objXMLOut
                .WriteStartElement(strElementName)
                .WriteAttributeString("precision", intPrecisionBits.ToString())        ' Store the precision, in bits
                .WriteAttributeString("type", strDataTypeName)
                .WriteString(strEncodedValues)
                .WriteEndElement()
            End With

        End Sub

        Public Function XMLOutputFileFinalize(
           dataOutputHandler As clsDataOutput,
           scanList As clsScanList,
           objSpectraCache As clsSpectraCache,
           processingStats As clsProcessingStats,
           processingTimeSec As Single) As Boolean


            Dim objXMLOut = dataOutputHandler.OutputFileHandles.XMLFileForSICs
            If objXMLOut Is Nothing Then Return False

            Try
                objXMLOut.WriteStartElement("ProcessingStats")
                With objSpectraCache
                    objXMLOut.WriteElementString("CacheEventCount", .CacheEventCount.ToString())
                    objXMLOut.WriteElementString("UnCacheEventCount", .UnCacheEventCount.ToString())
                End With

                With processingStats
                    objXMLOut.WriteElementString("PeakMemoryUsageMB", Math.Round(.PeakMemoryUsageMB, 2).ToString())
                    objXMLOut.WriteElementString("TotalProcessingTimeSeconds", Math.Round(processingTimeSec - .TotalProcessingTimeAtStart, 2).ToString())
                End With
                objXMLOut.WriteEndElement()

                If scanList.ProcessingIncomplete Then
                    objXMLOut.WriteElementString("ProcessingComplete", "False")
                Else
                    objXMLOut.WriteElementString("ProcessingComplete", "True")
                End If

                objXMLOut.WriteEndElement()     ' Close out the <SICData> element
                objXMLOut.WriteEndDocument()
                objXMLOut.Close()

            Catch ex As Exception
                ReportError("Error finalizing the XML output file", ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Public Function XMLOutputFileInitialize(
          strInputFilePathFull As String,
          strOutputFolderPath As String,
          dataOutputHandler As clsDataOutput,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          sicOptions As clsSICOptions,
          binningOptions As clsBinningOptions) As Boolean

            Dim strXMLOutputFilePath = String.Empty

            Try

                strXMLOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFilePathFull, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.XMLFile)

                dataOutputHandler.OutputFileHandles.XMLFileForSICs = New Xml.XmlTextWriter(strXMLOutputFilePath, Text.Encoding.UTF8)
                Dim objXMLOut = dataOutputHandler.OutputFileHandles.XMLFileForSICs

                With objXMLOut
                    .Formatting = Xml.Formatting.Indented
                    .Indentation = 1
                End With

                objXMLOut.WriteStartDocument(True)
                objXMLOut.WriteStartElement("SICData")

                objXMLOut.WriteStartElement("ProcessingSummary")
                objXMLOut.WriteElementString("DatasetNumber", sicOptions.DatasetNumber.ToString())
                objXMLOut.WriteElementString("SourceFilePath", strInputFilePathFull)

                Dim strLastModTime As String
                Dim strFileSizeBytes As String

                Try
                    Dim ioFileInfo = New FileInfo(strInputFilePathFull)
                    Dim dtLastModTime = ioFileInfo.LastWriteTime()
                    strLastModTime = dtLastModTime.ToShortDateString() & " " & dtLastModTime.ToShortTimeString()
                    strFileSizeBytes = ioFileInfo.Length.ToString()
                Catch ex As Exception
                    strLastModTime = String.Empty
                    strFileSizeBytes = "0"
                End Try

                objXMLOut.WriteElementString("SourceFileDateTime", strLastModTime)
                objXMLOut.WriteElementString("SourceFileSizeBytes", strFileSizeBytes)

                objXMLOut.WriteElementString("MASICProcessingDate", DateTime.Now.ToShortDateString() & " " & DateTime.Now.ToLongTimeString())
                objXMLOut.WriteElementString("MASICVersion", mOptions.MASICVersion)
                objXMLOut.WriteElementString("MASICPeakFinderDllVersion", mOptions.PeakFinderVersion)
                objXMLOut.WriteElementString("ScanCountTotal", scanList.MasterScanOrderCount.ToString())
                objXMLOut.WriteElementString("SurveyScanCount", scanList.SurveyScans.Count.ToString())
                objXMLOut.WriteElementString("FragScanCount", scanList.FragScans.Count.ToString())
                objXMLOut.WriteElementString("SkipMSMSProcessing", mOptions.SkipMSMSProcessing.ToString())

                objXMLOut.WriteElementString("ParentIonDecoyMassDa", mOptions.ParentIonDecoyMassDa.ToString("0.0000"))

                objXMLOut.WriteEndElement()

                objXMLOut.WriteStartElement("MemoryOptions")
                With objSpectraCache

                    objXMLOut.WriteElementString("CacheAlwaysDisabled", .DiskCachingAlwaysDisabled.ToString())
                    objXMLOut.WriteElementString("CacheSpectraToRetainInMemory", .CacheSpectraToRetainInMemory.ToString())

                End With
                objXMLOut.WriteEndElement()


                objXMLOut.WriteStartElement("SICOptions")
                With sicOptions
                    ' SIC Options

                    ' "SICToleranceDa" is a legacy parameter; If the SIC tolerance is in PPM, then "SICToleranceDa" is the Da tolerance at 1000 m/z
                    objXMLOut.WriteElementString("SICToleranceDa", clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 1000).ToString("0.0000"))

                    objXMLOut.WriteElementString("SICTolerance", .SICTolerance.ToString("0.0000"))
                    objXMLOut.WriteElementString("SICToleranceIsPPM", .SICToleranceIsPPM.ToString())

                    objXMLOut.WriteElementString("RefineReportedParentIonMZ", .RefineReportedParentIonMZ.ToString())

                    objXMLOut.WriteElementString("ScanRangeStart", .ScanRangeStart.ToString())
                    objXMLOut.WriteElementString("ScanRangeEnd", .ScanRangeEnd.ToString())
                    objXMLOut.WriteElementString("RTRangeStart", .RTRangeStart.ToString())
                    objXMLOut.WriteElementString("RTRangeEnd", .RTRangeEnd.ToString())

                    objXMLOut.WriteElementString("CompressMSSpectraData", .CompressMSSpectraData.ToString())
                    objXMLOut.WriteElementString("CompressMSMSSpectraData", .CompressMSMSSpectraData.ToString())

                    objXMLOut.WriteElementString("CompressToleranceDivisorForDa", .CompressToleranceDivisorForDa.ToString("0.0"))
                    objXMLOut.WriteElementString("CompressToleranceDivisorForPPM", .CompressToleranceDivisorForPPM.ToString("0.0"))

                    objXMLOut.WriteElementString("MaxSICPeakWidthMinutesBackward", .MaxSICPeakWidthMinutesBackward.ToString())
                    objXMLOut.WriteElementString("MaxSICPeakWidthMinutesForward", .MaxSICPeakWidthMinutesForward.ToString())

                    With .SICPeakFinderOptions
                        objXMLOut.WriteElementString("IntensityThresholdFractionMax", .IntensityThresholdFractionMax.ToString())
                        objXMLOut.WriteElementString("IntensityThresholdAbsoluteMinimum", .IntensityThresholdAbsoluteMinimum.ToString())

                        ' Peak Finding Options
                        With .SICBaselineNoiseOptions
                            objXMLOut.WriteElementString("SICNoiseThresholdMode", .BaselineNoiseMode.ToString())
                            objXMLOut.WriteElementString("SICNoiseThresholdIntensity", .BaselineNoiseLevelAbsolute.ToString())
                            objXMLOut.WriteElementString("SICNoiseFractionLowIntensityDataToAverage", .TrimmedMeanFractionLowIntensityDataToAverage.ToString())
                            objXMLOut.WriteElementString("SICNoiseMinimumSignalToNoiseRatio", .MinimumSignalToNoiseRatio.ToString())
                        End With

                        objXMLOut.WriteElementString("MaxDistanceScansNoOverlap", .MaxDistanceScansNoOverlap.ToString())
                        objXMLOut.WriteElementString("MaxAllowedUpwardSpikeFractionMax", .MaxAllowedUpwardSpikeFractionMax.ToString())
                        objXMLOut.WriteElementString("InitialPeakWidthScansScaler", .InitialPeakWidthScansScaler.ToString())
                        objXMLOut.WriteElementString("InitialPeakWidthScansMaximum", .InitialPeakWidthScansMaximum.ToString())

                        objXMLOut.WriteElementString("FindPeaksOnSmoothedData", .FindPeaksOnSmoothedData.ToString())
                        objXMLOut.WriteElementString("SmoothDataRegardlessOfMinimumPeakWidth", .SmoothDataRegardlessOfMinimumPeakWidth.ToString())
                        objXMLOut.WriteElementString("UseButterworthSmooth", .UseButterworthSmooth.ToString())
                        objXMLOut.WriteElementString("ButterworthSamplingFrequency", .ButterworthSamplingFrequency.ToString())
                        objXMLOut.WriteElementString("ButterworthSamplingFrequencyDoubledForSIMData", .ButterworthSamplingFrequencyDoubledForSIMData.ToString())

                        objXMLOut.WriteElementString("UseSavitzkyGolaySmooth", .UseSavitzkyGolaySmooth.ToString())
                        objXMLOut.WriteElementString("SavitzkyGolayFilterOrder", .SavitzkyGolayFilterOrder.ToString())

                        With .MassSpectraNoiseThresholdOptions
                            objXMLOut.WriteElementString("MassSpectraNoiseThresholdMode", .BaselineNoiseMode.ToString())
                            objXMLOut.WriteElementString("MassSpectraNoiseThresholdIntensity", .BaselineNoiseLevelAbsolute.ToString())
                            objXMLOut.WriteElementString("MassSpectraNoiseFractionLowIntensityDataToAverage", .TrimmedMeanFractionLowIntensityDataToAverage.ToString())
                            objXMLOut.WriteElementString("MassSpectraNoiseMinimumSignalToNoiseRatio", .MinimumSignalToNoiseRatio.ToString())
                        End With
                    End With

                    objXMLOut.WriteElementString("ReplaceSICZeroesWithMinimumPositiveValueFromMSData", .ReplaceSICZeroesWithMinimumPositiveValueFromMSData.ToString())

                    objXMLOut.WriteElementString("SaveSmoothedData", .SaveSmoothedData.ToString())

                    ' Similarity options
                    objXMLOut.WriteElementString("SimilarIonMZToleranceHalfWidth", .SimilarIonMZToleranceHalfWidth.ToString())
                    objXMLOut.WriteElementString("SimilarIonToleranceHalfWidthMinutes", .SimilarIonToleranceHalfWidthMinutes.ToString())
                    objXMLOut.WriteElementString("SpectrumSimilarityMinimum", .SpectrumSimilarityMinimum.ToString())
                End With
                objXMLOut.WriteEndElement()

                objXMLOut.WriteStartElement("BinningOptions")
                With binningOptions
                    objXMLOut.WriteElementString("BinStartX", .StartX.ToString())
                    objXMLOut.WriteElementString("BinEndX", .EndX.ToString())
                    objXMLOut.WriteElementString("BinSize", .BinSize.ToString())
                    objXMLOut.WriteElementString("MaximumBinCount", .MaximumBinCount.ToString())

                    objXMLOut.WriteElementString("IntensityPrecisionPercent", .IntensityPrecisionPercent.ToString())
                    objXMLOut.WriteElementString("Normalize", .Normalize.ToString())
                    objXMLOut.WriteElementString("SumAllIntensitiesForBin", .SumAllIntensitiesForBin.ToString())

                End With
                objXMLOut.WriteEndElement()

                objXMLOut.WriteStartElement("CustomSICValues")
                With mOptions.CustomSICList
                    objXMLOut.WriteElementString("MZList", .RawTextMZList)
                    objXMLOut.WriteElementString("MZToleranceDaList", CheckForEmptyToleranceList(.RawTextMZToleranceDaList))
                    objXMLOut.WriteElementString("ScanCenterList", .RawTextScanOrAcqTimeCenterList)
                    objXMLOut.WriteElementString("ScanToleranceList", CheckForEmptyToleranceList(.RawTextScanOrAcqTimeToleranceList))
                    objXMLOut.WriteElementString("ScanTolerance", .ScanOrAcqTimeTolerance.ToString())
                    objXMLOut.WriteElementString("ScanType", .ScanToleranceType.ToString())
                    objXMLOut.WriteElementString("LimitSearchToCustomMZList", .LimitSearchToCustomMZList.ToString())
                End With
                objXMLOut.WriteEndElement()


            Catch ex As Exception
                ReportError("Error initializing the XML output file: " & strXMLOutputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
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
                If clsUtilities.IsNumber(strWork) Then
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

        Public Function XmlOutputFileUpdateEntries(
          scanList As clsScanList,
          strInputFileName As String,
          strOutputFolderPath As String) As Boolean


            Const PARENT_ION_TAG_START_LCASE = "<parention"     ' Note: this needs to be lowercase
            Const INDEX_ATTRIBUTE_LCASE = "index="              ' Note: this needs to be lowercase

            Const OPTIMAL_PEAK_APEX_TAG_NAME = "OptimalPeakApexScanNumber"
            Const PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME = "PeakApexOverrideParentIonIndex"

            Dim strXMLReadFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.XMLFile)

            Dim strXMLOutputFilePath = Path.Combine(strOutputFolderPath, "__temp__MASICOutputFile.xml")

            Try
                ' Wait 2 seconds before reopening the file, to make sure the handle is closed
                Threading.Thread.Sleep(2000)

                If Not File.Exists(strXMLReadFilePath) Then
                    ' XML file not found, exit the function
                    Return True
                End If

                Using srInFile = New StreamReader(strXMLReadFilePath),
                  srOutFile = New StreamWriter(strXMLOutputFilePath, False)

                    UpdateProgress(0, "Updating XML file with optimal peak apex values")

                    Dim intParentIonIndex = -1
                    Dim intParentIonsProcessed = 0
                    Do While Not srInFile.EndOfStream
                        Dim strLineIn = srInFile.ReadLine()
                        If strLineIn Is Nothing Then Continue Do

                        Dim strLineInTrimmedAndLower = strLineIn.Trim.ToLower()

                        If strLineInTrimmedAndLower.StartsWith(PARENT_ION_TAG_START_LCASE) Then
                            Dim intCharIndex = strLineInTrimmedAndLower.IndexOf(INDEX_ATTRIBUTE_LCASE, StringComparison.CurrentCultureIgnoreCase)
                            If intCharIndex > 0 Then
                                Dim strWork = strLineInTrimmedAndLower.Substring(intCharIndex + INDEX_ATTRIBUTE_LCASE.Length + 1)
                                intCharIndex = strWork.IndexOf(ControlChars.Quote)
                                If intCharIndex > 0 Then
                                    strWork = strWork.Substring(0, intCharIndex)
                                    If clsUtilities.IsNumber(strWork) Then
                                        intParentIonIndex = CInt(strWork)
                                        intParentIonsProcessed += 1

                                        ' Update progress
                                        If scanList.ParentIonInfoCount > 1 Then
                                            If intParentIonsProcessed Mod 100 = 0 Then
                                                UpdateProgress(CShort(intParentIonsProcessed / (scanList.ParentIonInfoCount - 1) * 100))
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

                            srOutFile.WriteLine(strLineIn)

                        ElseIf strLineInTrimmedAndLower.StartsWith("<" & OPTIMAL_PEAK_APEX_TAG_NAME.ToLower) AndAlso intParentIonIndex >= 0 Then
                            If intParentIonIndex < scanList.ParentIonInfoCount Then
                                XmlOutputFileReplaceSetting(srOutFile, strLineIn, OPTIMAL_PEAK_APEX_TAG_NAME, scanList.ParentIons(intParentIonIndex).OptimalPeakApexScanNumber)
                            End If
                        ElseIf strLineInTrimmedAndLower.StartsWith("<" & PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME.ToLower) AndAlso intParentIonIndex >= 0 Then
                            If intParentIonIndex < scanList.ParentIonInfoCount Then
                                XmlOutputFileReplaceSetting(srOutFile, strLineIn, PEAK_APEX_OVERRIDE_PARENT_ION_TAG_NAME, scanList.ParentIons(intParentIonIndex).PeakApexOverrideParentIonIndex)
                            End If
                        Else
                            srOutFile.WriteLine(strLineIn)
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
                    ReportError("Error renaming XML output file from temp name to: " & strXMLReadFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                    Return False
                End Try

                UpdateProgress(100)
                System.Windows.Forms.Application.DoEvents()

            Catch ex As Exception
                ReportError("Error updating the XML output file: " & strXMLReadFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

    End Class

End Namespace