Imports MASIC.clsMASIC
Imports MASIC.DatasetStats
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

        Private Const ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW As Integer = 5

        Protected Const PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW As Integer = 5

#End Region

#Region "Classwide Variables"
        Protected ReadOnly mOptions As clsMASICOptions

        Protected ReadOnly mParentIonProcessor As clsParentIonProcessing

        Protected ReadOnly mPeakFinder As MASICPeakFinder.clsMASICPeakFinder

        Protected ReadOnly mScanTracking As clsScanTracking

        Protected mDatasetFileInfo As DatasetFileInfo

        Protected mKeepRawSpectra As Boolean
        Protected mKeepMSMSSpectra As Boolean

        Protected mLastSurveyScanIndexInMasterSeqOrder As Integer
        Protected mLastNonZoomSurveyScanIndex As Integer
        Protected mLastLogTime As DateTime

        Private ReadOnly mInterferenceCalculator As InterDetect.InterferenceCalculator

        Private ReadOnly mCachedPrecursorIons As List(Of InterDetect.Peak)
        Protected mCachedPrecursorScan As Integer

        Private mIsolationWidthNotFoundCount As Integer
        Private mPrecursorNotFoundCount As Integer
        Private mNextPrecursorNotFoundCountThreshold As Integer

        Protected mScansOutOfRange As Integer

#End Region

#Region "Properties"

        Public ReadOnly Property DatasetFileInfo As DatasetFileInfo
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

            mDatasetFileInfo = New DatasetFileInfo()

            mInterferenceCalculator = New InterDetect.InterferenceCalculator()

            AddHandler mInterferenceCalculator.StatusEvent, AddressOf OnStatusEvent
            AddHandler mInterferenceCalculator.ErrorEvent, AddressOf OnErrorEvent
            AddHandler mInterferenceCalculator.WarningEvent, AddressOf InterferenceWarningEventHandler

            mCachedPrecursorIons = New List(Of InterDetect.Peak)
            mCachedPrecursorScan = 0

            mIsolationWidthNotFoundCount = 0
            mPrecursorNotFoundCount = 0

        End Sub

        ''' <summary>
        ''' Compute the interference in the region centered around parentIonMz
        ''' Before calling this method, call UpdateCachedPrecursorScan to store the m/z and intensity values of the precursor spectrum
        ''' </summary>
        ''' <param name="fragScanNumber">Used for reporting purposes</param>
        ''' <param name="precursorScanNumber">Used for reporting purposes</param>
        ''' <param name="parentIonMz"></param>
        ''' <param name="isolationWidth"></param>
        ''' <param name="chargeState"></param>
        ''' <returns>
        ''' Interference score: fraction of observed peaks that are from the precursor
        ''' Larger is better, with a max of 1 and minimum of 0
        ''' 1 means all peaks are from the precursor
        ''' </returns>
        Protected Function ComputePrecursorInterference(
          fragScanNumber As Integer,
          precursorScanNumber As Integer,
          parentIonMz As Double,
          isolationWidth As Double,
          chargeState As Integer) As Double

            Dim precursorInfo = New InterDetect.PrecursorIntense(parentIonMz, isolationWidth, chargeState) With {
                .PrecursorScanNumber = precursorScanNumber,
                .ScanNumber = fragScanNumber
            }

            mInterferenceCalculator.Interference(precursorInfo, mCachedPrecursorIons)

            Return precursorInfo.Interference

        End Function

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
                Select Case noiseThresholdOptions.BaselineNoiseMode
                    Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold
                        If noiseThresholdOptions.BaselineNoiseLevelAbsolute > 0 Then
                            ionCountNew = 0
                            For ionIndex = 0 To msSpectrum.IonCount - 1

                                pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ(ionIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                                If Not pointPassesFilter Then
                                    ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                    If msSpectrum.IonsIntensity(ionIndex) >= noiseThresholdOptions.BaselineNoiseLevelAbsolute Then
                                        pointPassesFilter = True
                                    End If
                                End If

                                If pointPassesFilter Then
                                    msSpectrum.IonsMZ(ionCountNew) = msSpectrum.IonsMZ(ionIndex)
                                    msSpectrum.IonsIntensity(ionCountNew) = msSpectrum.IonsIntensity(ionIndex)
                                    ionCountNew += 1
                                End If

                            Next
                        Else
                            ionCountNew = msSpectrum.IonCount
                        End If
                    Case MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByAbundance,
                  MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMeanByCount,
                  MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.TrimmedMedianByAbundance
                        If noiseThresholdOptions.MinimumSignalToNoiseRatio > 0 Then
                            ionCountNew = 0
                            For ionIndex = 0 To msSpectrum.IonCount - 1

                                pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ(ionIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                                If Not pointPassesFilter Then
                                    ' Check the point's intensity against .BaselineNoiseLevelAbsolute
                                    If MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(msSpectrum.IonsIntensity(ionIndex), noiseThresholdIntensity) >= noiseThresholdOptions.MinimumSignalToNoiseRatio Then
                                        pointPassesFilter = True
                                    End If
                                End If

                                If pointPassesFilter Then
                                    msSpectrum.IonsMZ(ionCountNew) = msSpectrum.IonsMZ(ionIndex)
                                    msSpectrum.IonsIntensity(ionCountNew) = msSpectrum.IonsIntensity(ionIndex)
                                    ionCountNew += 1
                                End If

                            Next
                        Else
                            ionCountNew = msSpectrum.IonCount
                        End If
                    Case Else
                        ReportError("Unknown BaselineNoiseMode encountered in DiscardDataBelowNoiseThreshold: " &
                                noiseThresholdOptions.BaselineNoiseMode.ToString())
                End Select

                If ionCountNew < msSpectrum.IonCount Then
                    msSpectrum.ShrinkArrays(ionCountNew)
                End If
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
                    For ionIndex = 0 To msSpectrum.IonCount - 1
                        objFilterDataArray.AddDataPoint(msSpectrum.IonsIntensity(ionIndex), ionIndex)
                        If writeDebugData Then
                            writer.WriteLine(msSpectrum.IonsMZ(ionIndex) & ControlChars.Tab & msSpectrum.IonsIntensity(ionIndex))
                        End If
                    Next

                    If writeDebugData Then
                        writer.Close()
                    End If


                    ' Call .FilterData, which will determine which data points to keep
                    objFilterDataArray.FilterData()

                    ionCountNew = 0
                    For ionIndex = 0 To msSpectrum.IonCount - 1

                        pointPassesFilter = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ(ionIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                        If Not pointPassesFilter Then
                            ' See if the point's intensity is negative
                            If objFilterDataArray.GetAbundanceByIndex(ionIndex) >= 0 Then
                                pointPassesFilter = True
                            End If
                        End If

                        If pointPassesFilter Then
                            msSpectrum.IonsMZ(ionCountNew) = msSpectrum.IonsMZ(ionIndex)
                            msSpectrum.IonsIntensity(ionCountNew) = msSpectrum.IonsIntensity(ionIndex)
                            ionCountNew += 1
                        End If

                    Next
                Else
                    ionCountNew = msSpectrum.IonCount
                End If

                If ionCountNew < msSpectrum.IonCount Then
                    msSpectrum.ShrinkArrays(ionCountNew)
                End If

                If writeDebugData Then
                    Using postFilterWriter = New StreamWriter(New FileStream(Path.Combine(mOptions.OutputDirectoryPath, "DataDump_" & msSpectrum.ScanNumber.ToString() & "_PostFilter.txt"), FileMode.Create, FileAccess.Write, FileShare.Read))
                        postFilterWriter.WriteLine("m/z" & ControlChars.Tab & "Intensity")

                        ' Store the intensity values in objFilterDataArray
                        For ionIndex = 0 To msSpectrum.IonCount - 1
                            postFilterWriter.WriteLine(msSpectrum.IonsMZ(ionIndex) & ControlChars.Tab &
                                                       msSpectrum.IonsIntensity(ionIndex))
                        Next
                    End Using
                End If

            Catch ex As Exception
                ReportError("Error limiting the number of data points to " & maxIonCountToRetain, ex, eMasicErrorCodes.UnspecifiedError)
            End Try

        End Sub

        Protected Function FinalizeScanList(scanList As clsScanList, dataFile As FileSystemInfo) As Boolean

            If scanList.MasterScanOrderCount <= 0 Then
                ' No scans found
                If mScansOutOfRange > 0 Then
                    ReportWarning("None of the spectra in the input file was within the specified scan number and/or scan time range: " & dataFile.FullName)
                    SetLocalErrorCode(eMasicErrorCodes.NoParentIonsFoundInInputFile)
                Else
                    ReportError("No scans found in the input file: " & dataFile.FullName)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                End If

                Return False
            End If

            If mPrecursorNotFoundCount > PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW Then
                Dim precursorMissingPct As Double
                If scanList.FragScans.Count > 0 Then
                    precursorMissingPct = mPrecursorNotFoundCount / CDbl(scanList.FragScans.Count) * 100
                End If

                OnWarningEvent(String.Format("Could not determine the precursor for {0:F1}% of the MS2 spectra ({1} / {2} scans). " &
                                             "These scans will have an Interference Score of 1 (to avoid accidentally filtering out low intensity results).",
                                             precursorMissingPct, mPrecursorNotFoundCount, scanList.FragScans.Count))
            End If

            ' Record the current memory usage
            OnUpdateMemoryUsage()

            Return True

        End Function

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

        Protected Sub InitBaseOptions(scanList As clsScanList, keepRawSpectra As Boolean, keepMSMSSpectra As Boolean)

            mLastNonZoomSurveyScanIndex = -1
            mScansOutOfRange = 0

            scanList.SIMDataPresent = False
            scanList.MRMDataPresent = False

            mKeepRawSpectra = keepRawSpectra
            mKeepMSMSSpectra = keepMSMSSpectra

            mLastLogTime = DateTime.UtcNow

        End Sub

        Protected Sub SaveScanStatEntry(
          writer As StreamWriter,
          eScanType As clsScanList.eScanTypeConstants,
          currentScan As clsScanInfo,
          datasetID As Integer)

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
                    datasetID.ToString(),                       ' Dataset ID
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

        Protected Sub UpdateCachedPrecursorScan(
          precursorScanNumber As Integer,
          centroidedIonsMz As Double(),
          centroidedIonsIntensity As Double(),
          ionCount As Integer)

            Dim mzList As New List(Of Double)
            Dim intensityList As New List(Of Double)

            For i = 0 To ionCount - 1
                mzList.Add(centroidedIonsMz(i))
                intensityList.Add(centroidedIonsIntensity(i))
            Next

            UpdateCachedPrecursorScan(precursorScanNumber, mzList, intensityList)

        End Sub

        Protected Sub UpdateCachedPrecursorScan(
          precursorScanNumber As Integer,
          centroidedIonsMz As List(Of Double),
          centroidedIonsIntensity As List(Of Double))

            mCachedPrecursorIons.Clear()

            Dim ionCount = centroidedIonsMz.Count

            For index = 0 To ionCount - 1
                Dim newPeak = New InterDetect.Peak With {
                    .Mz = centroidedIonsMz(index),
                    .Abundance = centroidedIonsIntensity(index)
                }

                mCachedPrecursorIons.Add(newPeak)
            Next

            mCachedPrecursorScan = precursorScanNumber
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

        Protected Sub WarnIsolationWidthNotFound(scanNumber As Integer, warningMessage As String)
            mIsolationWidthNotFoundCount += 1

            If mIsolationWidthNotFoundCount <= ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW Then
                ReportWarning(warningMessage & "; " & "cannot compute interference for scan " & scanNumber)
            ElseIf mIsolationWidthNotFoundCount Mod 5000 = 0 Then
                ReportWarning("Could not determine the MS2 isolation width for " & mIsolationWidthNotFoundCount & " scans")
            End If

        End Sub

        Private Sub InterferenceWarningEventHandler(message As String)
            If message.StartsWith("Did not find the precursor for") Then
                ' The precursor ion was not found in the centroided MS1 spectrum; this happens sometimes
                mPrecursorNotFoundCount += 1
                If mPrecursorNotFoundCount <= PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW OrElse mPrecursorNotFoundCount > mNextPrecursorNotFoundCountThreshold Then
                    OnWarningEvent(message)

                    If mPrecursorNotFoundCount > PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW Then
                        mNextPrecursorNotFoundCountThreshold *= 2
                    End If

                End If
                Else
                OnWarningEvent(message)
            End If
        End Sub
    End Class

End Namespace