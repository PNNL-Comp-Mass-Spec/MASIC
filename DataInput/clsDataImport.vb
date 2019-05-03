Imports MASIC.clsMASIC
Imports PRISM

Namespace DataInput

    Public MustInherit Class clsDataImport
        Inherits clsMasicEventNotifier

#Region "Constants and Enums"

        Public Const FINNIGAN_RAW_FILE_EXTENSION As String = ".RAW"

        Public Const MZ_ML_FILE_EXTENSION As String = ".MZML"

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

        Protected mDatasetFileInfo As clsDatasetStatsSummarizer.udtDatasetFileInfoType
#End Region

#Region "Properties"

        Public ReadOnly Property DatasetFileInfo As clsDatasetStatsSummarizer.udtDatasetFileInfoType
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

            mDatasetFileInfo = New clsDatasetStatsSummarizer.udtDatasetFileInfoType()

        End Sub

        Public Sub DiscardDataBelowNoiseThreshold(
          msSpectrum As clsMSSpectrum,
          noiseThresholdIntensity As Double,
          mzIgnoreRangeStart As Double,
          mzIgnoreRangeEnd As Double,
          noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions)

            Dim ionCountNew As Integer
            Dim ionIndex As Integer
            Dim pointPassesFilter As Boolean

            Try
                With msSpectrum
                    Select Case noiseThresholdOptions.BaselineNoiseMode
                        Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold
                            If noiseThresholdOptions.BaselineNoiseLevelAbsolute > 0 Then
                                ionCountNew = 0
                                For ionIndex = 0 To .IonCount - 1

                                    pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(ionIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                                    If Not pointPassesFilter Then
                                        ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                        If .IonsIntensity(ionIndex) >= noiseThresholdOptions.BaselineNoiseLevelAbsolute Then
                                            pointPassesFilter = True
                                        End If
                                    End If

                                    If pointPassesFilter Then
                                        .IonsMZ(ionCountNew) = .IonsMZ(ionIndex)
                                        .IonsIntensity(ionCountNew) = .IonsIntensity(ionIndex)
                                        ionCountNew += 1
                                    End If

                                Next
                            Else
                                ionCountNew = .IonCount
                            End If
                        Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance,
                      MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount,
                      MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                            If noiseThresholdOptions.MinimumSignalToNoiseRatio > 0 Then
                                ionCountNew = 0
                                For ionIndex = 0 To .IonCount - 1

                                    pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(ionIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                                    If Not pointPassesFilter Then
                                        ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                        If MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(.IonsIntensity(ionIndex), noiseThresholdIntensity) >= noiseThresholdOptions.MinimumSignalToNoiseRatio Then
                                            pointPassesFilter = True
                                        End If
                                    End If

                                    If pointPassesFilter Then
                                        .IonsMZ(ionCountNew) = .IonsMZ(ionIndex)
                                        .IonsIntensity(ionCountNew) = .IonsIntensity(ionIndex)
                                        ionCountNew += 1
                                    End If

                                Next
                            Else
                                ionCountNew = .IonCount
                            End If
                        Case Else
                            ReportError("Unknown BaselineNoiseMode encountered in DiscardDataBelowNoiseThreshold: " &
                                    noiseThresholdOptions.BaselineNoiseMode.ToString())
                    End Select

                    If ionCountNew < .IonCount Then
                        .IonCount = ionCountNew
                    End If
                End With
            Catch ex As Exception
                ReportError("Error discarding data below the noise threshold", ex, eMasicErrorCodes.UnspecifiedError)
            End Try

        End Sub

        Public Sub DiscardDataToLimitIonCount(
          msSpectrum As clsMSSpectrum,
          mzIgnoreRangeStart As Double,
          mzIgnoreRangeEnd As Double,
          maxIonCountToRetain As Integer)

            Dim ionCountNew As Integer
            Dim ionIndex As Integer
            Dim pointPassesFilter As Boolean

            ' When this is true, then will write a text file of the mass spectrum before and after it is filtered
            ' Used for debugging
            Dim writeDebugData As Boolean
            Dim writer As StreamWriter = Nothing

            Try

                With msSpectrum

                    If msSpectrum.IonCount > maxIonCountToRetain Then
                        Dim objFilterDataArray = New clsFilterDataArrayMaxCount() With {
                            .MaximumDataCountToLoad = maxIonCountToRetain,
                            .TotalIntensityPercentageFilterEnabled = False
                        }

                        writeDebugData = False
                        If writeDebugData Then
                            writer = New StreamWriter(New FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" & msSpectrum.ScanNumber.ToString() & "_BeforeFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                            writer.WriteLine("m/z" & ControlChars.Tab & "Intensity")
                        End If

                        ' Store the intensity values in objFilterDataArray
                        For ionIndex = 0 To .IonCount - 1
                            objFilterDataArray.AddDataPoint(.IonsIntensity(ionIndex), ionIndex)
                            If writeDebugData Then
                                writer.WriteLine(.IonsMZ(ionIndex) & ControlChars.Tab & .IonsIntensity(ionIndex))
                            End If
                        Next

                        If writeDebugData Then
                            writer.Close()
                        End If


                        ' Call .FilterData, which will determine which data points to keep
                        objFilterDataArray.FilterData()

                        ionCountNew = 0
                        For ionIndex = 0 To .IonCount - 1

                            pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(ionIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                            If Not pointPassesFilter Then
                                ' See if the point's intensity is negative
                                If objFilterDataArray.GetAbundanceByIndex(ionIndex) >= 0 Then
                                    pointPassesFilter = True
                                End If
                            End If

                            If pointPassesFilter Then
                                .IonsMZ(ionCountNew) = .IonsMZ(ionIndex)
                                .IonsIntensity(ionCountNew) = .IonsIntensity(ionIndex)
                                ionCountNew += 1
                            End If

                        Next
                    Else
                        ionCountNew = .IonCount
                    End If

                    If ionCountNew < .IonCount Then
                        .IonCount = ionCountNew
                    End If

                    If writeDebugData Then
                        Using postFilterWriter = New StreamWriter(New FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" & msSpectrum.ScanNumber.ToString() & "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                            postFilterWriter.WriteLine("m/z" & ControlChars.Tab & "Intensity")

                            ' Store the intensity values in objFilterDataArray
                            For ionIndex = 0 To .IonCount - 1
                                postFilterWriter.WriteLine(.IonsMZ(ionIndex) & ControlChars.Tab & .IonsIntensity(ionIndex))
                            Next
                        End Using
                    End If

                End With
            Catch ex As Exception
                ReportError("Error limiting the number of data points to " & maxIonCountToRetain, ex, eMasicErrorCodes.UnspecifiedError)
            End Try

        End Sub

        Public Shared Function GetDefaultExtensionsToParse() As IList(Of String)
            Dim extensionsToParse = New List(Of String) From {
                FINNIGAN_RAW_FILE_EXTENSION,
                MZ_XML_FILE_EXTENSION1,
                MZ_XML_FILE_EXTENSION2,
                MZ_DATA_FILE_EXTENSION1,
                MZ_DATA_FILE_EXTENSION2,
                AGILENT_MSMS_FILE_EXTENSION
            }

            Return extensionsToParse

        End Function

        Protected Sub SaveScanStatEntry(
          writer As StreamWriter,
          eScanType As clsScanList.eScanTypeConstants,
          currentScan As clsScanInfo,
          datasetNumber As Integer)

            Const cColDelimiter As Char = ControlChars.Tab

            Dim scanStatsEntry As New clsScanStatsEntry() With {
                .ScanNumber = currentScan.ScanNumber
            }

            If eScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                scanStatsEntry.ScanType = 1
                scanStatsEntry.ScanTypeName = String.Copy(currentScan.ScanTypeName)
            Else
                If currentScan.FragScanInfo.MSLevel <= 1 Then
                    ' This is a fragmentation scan, so it must have a scan type of at least 2
                    scanStatsEntry.ScanType = 2
                Else
                    ' .MSLevel is 2 or higher, record the actual MSLevel value
                    scanStatsEntry.ScanType = currentScan.FragScanInfo.MSLevel
                End If
                scanStatsEntry.ScanTypeName = String.Copy(currentScan.ScanTypeName)
            End If

            scanStatsEntry.ScanFilterText = currentScan.ScanHeaderText

            scanStatsEntry.ElutionTime = currentScan.ScanTime.ToString("0.0000")
            scanStatsEntry.TotalIonIntensity = StringUtilities.ValueToString(currentScan.TotalIonIntensity, 5)
            scanStatsEntry.BasePeakIntensity = StringUtilities.ValueToString(currentScan.BasePeakIonIntensity, 5)
            scanStatsEntry.BasePeakMZ = StringUtilities.DblToString(currentScan.BasePeakIonMZ, 4)

            ' Base peak signal to noise ratio
            scanStatsEntry.BasePeakSignalToNoiseRatio = StringUtilities.ValueToString(MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(currentScan.BasePeakIonIntensity, currentScan.BaselineNoiseStats.NoiseLevel), 4)

            scanStatsEntry.IonCount = currentScan.IonCount
            scanStatsEntry.IonCountRaw = currentScan.IonCountRaw

            mScanTracking.ScanStats.Add(scanStatsEntry)

            Dim dataColumns = New List(Of String) From {
                datasetNumber.ToString(),                   ' Dataset number
                scanStatsEntry.ScanNumber.ToString(),       ' Scan number
                scanStatsEntry.ElutionTime,                 ' Scan time (minutes)
                scanStatsEntry.ScanType.ToString(),         ' Scan type (1 for MS, 2 for MS2, etc.)
                scanStatsEntry.TotalIonIntensity,           ' Total ion intensity
                scanStatsEntry.BasePeakIntensity,           ' Base peak ion intensity
                scanStatsEntry.BasePeakMZ,                  ' Base peak ion m/z
                scanStatsEntry.BasePeakSignalToNoiseRatio,  ' Base peak signal to noise ratio
                scanStatsEntry.IonCount.ToString(),         ' Number of peaks (aka ions) in the spectrum
                scanStatsEntry.IonCountRaw.ToString(),      ' Number of peaks (aka ions) in the spectrum prior to any filtering
                scanStatsEntry.ScanTypeName                 ' Scan type name
                }

            writer.WriteLine(String.Join(cColDelimiter, dataColumns))

        End Sub

        Protected Function UpdateDatasetFileStats(
          dataFileInfo As FileInfo,
          datasetID As Integer) As Boolean

            Try
                If Not dataFileInfo.Exists Then Return False

                ' Record the file size and Dataset ID
                With mDatasetFileInfo
                    .FileSystemCreationTime = dataFileInfo.CreationTime
                    .FileSystemModificationTime = dataFileInfo.LastWriteTime

                    .AcqTimeStart = .FileSystemModificationTime
                    .AcqTimeEnd = .FileSystemModificationTime

                    .DatasetID = datasetID
                    .DatasetName = Path.GetFileNameWithoutExtension(dataFileInfo.Name)
                    .FileExtension = dataFileInfo.Extension
                    .FileSizeBytes = dataFileInfo.Length

                    .ScanCount = 0
                End With

            Catch ex As Exception
                Return False
            End Try

            Return True

        End Function

    End Class

End Namespace