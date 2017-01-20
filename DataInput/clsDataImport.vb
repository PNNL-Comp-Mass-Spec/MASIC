Imports MASIC.clsMASIC
Imports PNNLOmics.Utilities

Namespace DataInput

    Public MustInherit Class clsDataImport
        Inherits clsEventNotifier

#Region "Constants and Enums"

        Public Const FINNIGAN_RAW_FILE_EXTENSION As String = ".RAW"
        Public Const MZ_XML_FILE_EXTENSION1 As String = ".MZXML"
        Public Const MZ_XML_FILE_EXTENSION2 As String = "MZXML.XML"
        Public Const MZ_DATA_FILE_EXTENSION1 As String = ".MZDATA"
        Public Const MZ_DATA_FILE_EXTENSION2 As String = "MZDATA.XML"
        Public Const AGILENT_MSMS_FILE_EXTENSION As String = ".MGF"    ' Agilent files must have been exported to a .MGF and .CDF file pair prior to using MASIC
        Public Const AGILENT_MS_FILE_EXTENSION As String = ".CDF"

#End Region

#Region "Classwide Variables"
        Protected ReadOnly mOptions As clsMASICOptions

        Protected ReadOnly mParentIonProcessor As clsParentIonProcessing

        Protected ReadOnly mPeakFinder As MASICPeakFinder.clsMASICPeakFinder

        Protected ReadOnly mScanTracking As clsScanTracking

        Protected mDatasetFileInfo As DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType
#End Region

#Region "Properties"

        Public ReadOnly Property DatasetFileInfo As DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType
            Get
                Return mDatasetFileInfo
            End Get
        End Property

#End Region

#Region "Events"

        ''' <summary>
        ''' This event is used to signify to the calling class that it should update the status of the available memory usage
        ''' </summary>
        Public Event UpdateMemoryUsageEvent()

        Protected Sub OnUpdateMemoryUsage()
            RaiseEvent UpdateMemoryUsageEvent()
        End Sub

#End Region

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="masicOptions"></param>
        ''' <param name="peakFinder"></param>
        ''' <param name="parentIonProcessor"></param>
        Public Sub New(
      masicOptions As clsMASICOptions,
      peakFinder As MASICPeakFinder.clsMASICPeakFinder,
      parentIonProcessor As clsParentIonProcessing,
      scanTracking As clsScanTracking)

            mOptions = masicOptions
            mPeakFinder = peakFinder
            mParentIonProcessor = parentIonProcessor
            mScanTracking = scanTracking

            mDatasetFileInfo = New DSSummarizer.clsDatasetStatsSummarizer.udtDatasetFileInfoType()

        End Sub

        Public Sub DiscardDataBelowNoiseThreshold(
      objMSSpectrum As clsMSSpectrum,
      sngNoiseThresholdIntensity As Single,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions)

            Dim intIonCountNew As Integer
            Dim intIonIndex As Integer
            Dim blnPointPassesFilter As Boolean

            Try
                With objMSSpectrum
                    Select Case noiseThresholdOptions.BaselineNoiseMode
                        Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold
                            If noiseThresholdOptions.BaselineNoiseLevelAbsolute > 0 Then
                                intIonCountNew = 0
                                For intIonIndex = 0 To .IonCount - 1

                                    blnPointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(intIonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                                    If Not blnPointPassesFilter Then
                                        ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                        If .IonsIntensity(intIonIndex) >= noiseThresholdOptions.BaselineNoiseLevelAbsolute Then
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
                            If noiseThresholdOptions.MinimumSignalToNoiseRatio > 0 Then
                                intIonCountNew = 0
                                For intIonIndex = 0 To .IonCount - 1

                                    blnPointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(intIonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                                    If Not blnPointPassesFilter Then
                                        ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                        If MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(.IonsIntensity(intIonIndex), sngNoiseThresholdIntensity) >= noiseThresholdOptions.MinimumSignalToNoiseRatio Then
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
                            ReportError("DiscardDataBelowNoiseThreshold", "Unknown BaselineNoiseMode encountered in DiscardDataBelowNoiseThreshold: " &
                                    noiseThresholdOptions.BaselineNoiseMode.ToString)
                    End Select

                    If intIonCountNew < .IonCount Then
                        .IonCount = intIonCountNew
                    End If
                End With
            Catch ex As Exception
                ReportError("DiscardDataBelowNoiseThreshold", "Error discarding data below the noise threshold", ex, True, True, eMasicErrorCodes.UnspecifiedError)
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
                        objFilterDataArray = New clsFilterDataArrayMaxCount()

                        objFilterDataArray.MaximumDataCountToLoad = intMaxIonCountToRetain
                        objFilterDataArray.TotalIntensityPercentageFilterEnabled = False

                        blnWriteDebugData = False
                        If blnWriteDebugData Then
                            srOutFile = New StreamWriter(New FileStream(Path.Combine(mOptions.OutputFolderPath, "DataDump_" & objMSSpectrum.ScanNumber.ToString() & "_BeforeFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
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

                            blnPointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(intIonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

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
                        srOutFile = New StreamWriter(New FileStream(Path.Combine(mOptions.OutputFolderPath, "DataDump_" & objMSSpectrum.ScanNumber.ToString() & "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                        srOutFile.WriteLine("m/z" & ControlChars.Tab & "Intensity")

                        ' Store the intensity values in objFilterDataArray
                        For intIonIndex = 0 To .IonCount - 1
                            srOutFile.WriteLine(.IonsMZ(intIonIndex) & ControlChars.Tab & .IonsIntensity(intIonIndex))
                        Next
                        srOutFile.Close()
                    End If

                End With
            Catch ex As Exception
                ReportError("DiscardDataToLimitIonCount", "Error limiting the number of data points to " & intMaxIonCountToRetain.ToString, ex, True, True, eMasicErrorCodes.UnspecifiedError)
            End Try

        End Sub

        Public Shared Function GetDefaultExtensionsToParse() As String()
            Dim strExtensionsToParse(5) As String

            strExtensionsToParse(0) = FINNIGAN_RAW_FILE_EXTENSION
            strExtensionsToParse(1) = MZ_XML_FILE_EXTENSION1
            strExtensionsToParse(2) = MZ_XML_FILE_EXTENSION2
            strExtensionsToParse(3) = MZ_DATA_FILE_EXTENSION1
            strExtensionsToParse(4) = MZ_DATA_FILE_EXTENSION2
            strExtensionsToParse(5) = AGILENT_MSMS_FILE_EXTENSION

            Return strExtensionsToParse

        End Function

        Protected Sub SaveScanStatEntry(
          swOutFile As StreamWriter,
          eScanType As clsScanList.eScanTypeConstants,
          currentScan As clsScanInfo,
          intDatasetNumber As Integer)

            Const cColDelimiter As Char = ControlChars.Tab

            Dim objScanStatsEntry As New DSSummarizer.clsScanStatsEntry

            objScanStatsEntry.ScanNumber = currentScan.ScanNumber

            If eScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                objScanStatsEntry.ScanType = 1
                objScanStatsEntry.ScanTypeName = String.Copy(currentScan.ScanTypeName)
            Else
                If currentScan.FragScanInfo.MSLevel <= 1 Then
                    ' This is a fragmentation scan, so it must have a scan type of at least 2
                    objScanStatsEntry.ScanType = 2
                Else
                    ' .MSLevel is 2 or higher, record the actual MSLevel value
                    objScanStatsEntry.ScanType = currentScan.FragScanInfo.MSLevel
                End If
                objScanStatsEntry.ScanTypeName = String.Copy(currentScan.ScanTypeName)
            End If

            objScanStatsEntry.ScanFilterText = currentScan.ScanHeaderText

            objScanStatsEntry.ElutionTime = currentScan.ScanTime.ToString("0.0000")
            objScanStatsEntry.TotalIonIntensity = StringUtilities.ValueToString(currentScan.TotalIonIntensity, 5)
            objScanStatsEntry.BasePeakIntensity = StringUtilities.ValueToString(currentScan.BasePeakIonIntensity, 5)
            objScanStatsEntry.BasePeakMZ = Math.Round(currentScan.BasePeakIonMZ, 4).ToString

            ' Base peak signal to noise ratio
            objScanStatsEntry.BasePeakSignalToNoiseRatio = StringUtilities.ValueToString(MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(currentScan.BasePeakIonIntensity, currentScan.BaselineNoiseStats.NoiseLevel), 4)

            objScanStatsEntry.IonCount = currentScan.IonCount
            objScanStatsEntry.IonCountRaw = currentScan.IonCountRaw

            mScanTracking.ScanStats.Add(objScanStatsEntry)

            swOutFile.WriteLine(
          intDatasetNumber.ToString() & cColDelimiter &                    ' Dataset number
          objScanStatsEntry.ScanNumber.ToString() & cColDelimiter &        ' Scan number
          objScanStatsEntry.ElutionTime & cColDelimiter &                ' Scan time (minutes)
          objScanStatsEntry.ScanType.ToString() & cColDelimiter &          ' Scan type (1 for MS, 2 for MS2, etc.)
          objScanStatsEntry.TotalIonIntensity & cColDelimiter &          ' Total ion intensity
          objScanStatsEntry.BasePeakIntensity & cColDelimiter &          ' Base peak ion intensity
          objScanStatsEntry.BasePeakMZ & cColDelimiter &                 ' Base peak ion m/z
          objScanStatsEntry.BasePeakSignalToNoiseRatio & cColDelimiter & ' Base peak signal to noise ratio
          objScanStatsEntry.IonCount.ToString() & cColDelimiter &          ' Number of peaks (aka ions) in the spectrum
          objScanStatsEntry.IonCountRaw.ToString() & cColDelimiter &       ' Number of peaks (aka ions) in the spectrum prior to any filtering
          objScanStatsEntry.ScanTypeName)                                ' Scan type name

        End Sub

        Protected Function UpdateDatasetFileStats(
          ioFileInfo As FileInfo,
          intDatasetID As Integer) As Boolean

            Try
                If Not ioFileInfo.Exists Then Return False

                ' Record the file size and Dataset ID
                With mDatasetFileInfo
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

    End Class

End Namespace