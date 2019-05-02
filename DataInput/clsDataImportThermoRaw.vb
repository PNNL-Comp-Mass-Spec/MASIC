Imports MASIC.clsMASIC
Imports ThermoRawFileReader

Namespace DataInput

    Public Class clsDataImportThermoRaw
        Inherits clsDataImport

        Private Const SCAN_EVENT_CHARGE_STATE = "Charge State"
        Private Const SCAN_EVENT_MONOISOTOPIC_MZ = "Monoisotopic M/Z"
        Private Const SCAN_EVENT_MS2_ISOLATION_WIDTH = "MS2 Isolation Width"

        Private Const ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW As Integer = 5

        Private Const PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW As Integer = 5

        Private ReadOnly mInterferenceCalculator As InterDetect.InterferenceCalculator

        Private ReadOnly mCachedPrecursorIons As List(Of InterDetect.Peak)
        Private mCachedPrecursorScan As Integer

        Private mIsolationWidthNotFoundCount As Integer
        Private mPrecursorNotFoundCount As Integer

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="masicOptions"></param>
        ''' <param name="peakFinder"></param>
        ''' <param name="parentIonProcessor"></param>
        ''' <param name="scanTracking"></param>
        Public Sub New(
          masicOptions As clsMASICOptions,
          peakFinder As MASICPeakFinder.clsMASICPeakFinder,
          parentIonProcessor As clsParentIonProcessing,
          scanTracking As clsScanTracking)
            MyBase.New(masicOptions, peakFinder, parentIonProcessor, scanTracking)

            mInterferenceCalculator = New InterDetect.InterferenceCalculator()

            AddHandler mInterferenceCalculator.StatusEvent, AddressOf OnStatusEvent
            AddHandler mInterferenceCalculator.ErrorEvent, AddressOf OnErrorEvent
            AddHandler mInterferenceCalculator.WarningEvent, AddressOf InterferenceWarningEventHandler

            mCachedPrecursorIons = New List(Of InterDetect.Peak)
            mCachedPrecursorScan = 0

            mIsolationWidthNotFoundCount = 0
            mPrecursorNotFoundCount = 0

        End Sub

        Private Function ComputeInterference(xcaliburAccessor As XRawFileIO, scanInfo As ThermoRawFileReader.clsScanInfo, precursorScanNumber As Integer) As Double

            If Math.Abs(scanInfo.ParentIonMZ) < Single.Epsilon Then
                ReportWarning("Parent ion m/z is 0; cannot compute interference for scan " & scanInfo.ScanNumber)
                Return 0
            End If

            If precursorScanNumber <> mCachedPrecursorScan Then

                Dim centroidedIonsMz As Double() = Nothing
                Dim centroidedIonsIntensity As Double() = Nothing

                Dim ionCount = xcaliburAccessor.GetScanData(precursorScanNumber, centroidedIonsMz, centroidedIonsIntensity, 0, True)

                mCachedPrecursorIons.Clear()
                For index = 0 To ionCount - 1
                    Dim newPeak = New InterDetect.Peak With {
                        .Mz = centroidedIonsMz(index),
                        .Abundance = centroidedIonsIntensity(index)
                    }

                    mCachedPrecursorIons.Add(newPeak)
                Next

                mCachedPrecursorScan = precursorScanNumber

            End If

            Dim chargeState As Integer
            Dim isolationWidth As Double

            Dim chargeStateText = String.Empty
            Dim isolationWidthText = String.Empty

            scanInfo.TryGetScanEvent(SCAN_EVENT_CHARGE_STATE, chargeStateText, True)
            If Not String.IsNullOrWhiteSpace(chargeStateText) Then
                If Not Integer.TryParse(chargeStateText, chargeState) Then
                    chargeState = 0
                End If
            End If

            If Not scanInfo.TryGetScanEvent(SCAN_EVENT_MS2_ISOLATION_WIDTH, isolationWidthText, True) Then
                If scanInfo.MRMScanType = MRMScanTypeConstants.SRM Then
                    ' SRM data files don't have the MS2 Isolation Width event
                    Return 0
                End If

                mIsolationWidthNotFoundCount += 1
                If mIsolationWidthNotFoundCount <= ISOLATION_WIDTH_NOT_FOUND_WARNINGS_TO_SHOW Then
                    ReportWarning("Could not determine the MS2 isolation width (" & SCAN_EVENT_MS2_ISOLATION_WIDTH & "); " &
                                  "cannot compute interference for scan " & scanInfo.ScanNumber)
                ElseIf mIsolationWidthNotFoundCount Mod 5000 = 0 Then
                    ReportWarning("Could not determine the MS2 isolation width (" & SCAN_EVENT_MS2_ISOLATION_WIDTH & "); " &
                                  "for " & mIsolationWidthNotFoundCount & " scans")
                End If
                Return 0
            End If

            If Not Double.TryParse(isolationWidthText, isolationWidth) Then
                ReportWarning("MS2 isolation width (" & SCAN_EVENT_MS2_ISOLATION_WIDTH & ") was non-numeric (" & isolationWidthText & "); " &
                              "cannot compute interference for scan " & scanInfo.ScanNumber)
                Return 0
            End If

            Dim parentIonMz As Double

            If scanInfo.ParentIonMZ > 0 Then
                parentIonMz = scanInfo.ParentIonMZ
            Else
                ' ThermoRawFileReader could not determine the parent ion m/z value (this is highly unlikely)
                ' Use scan event "Monoisotopic M/Z" instead
                Dim monoMzText = String.Empty
                If (Not scanInfo.TryGetScanEvent(SCAN_EVENT_MONOISOTOPIC_MZ, monoMzText, True)) Then

                    ReportWarning("Could not determine the parent ion m/z value (" & SCAN_EVENT_MONOISOTOPIC_MZ & "); " &
                                  "cannot compute interference for scan " & scanInfo.ScanNumber)
                    Return 0
                End If

                Dim mz As Double
                If (Not Double.TryParse(monoMzText, mz)) Then

                    OnWarningEvent(String.Format("Skipping scan {0} since scan event {1} was not a number: {2}",
                                                 scanInfo.ScanNumber, SCAN_EVENT_MONOISOTOPIC_MZ, monoMzText))
                    Return 0
                End If

                parentIonMz = mz
            End If

            Dim oPrecursorInfo = New InterDetect.PrecursorInfo(parentIonMz, isolationWidth, chargeState) With {
                .ScanNumber = precursorScanNumber
            }

            mInterferenceCalculator.Interference(oPrecursorInfo, mCachedPrecursorIons)

            Return oPrecursorInfo.Interference

        End Function

        Public Function ExtractScanInfoFromXcaliburDataFile(
          filePath As String,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean

            ' Returns True if Success, False if failure
            ' Note: This function assumes filePath exists

            ' Use Xraw to read the .Raw files
            Dim xcaliburAccessor = New XRawFileIO()
            RegisterEvents(xcaliburAccessor)

            Dim ioMode = "Xraw"

            mIsolationWidthNotFoundCount = 0
            mPrecursorNotFoundCount = 0

            ' Assume success for now
            Dim success = True

            Try
                Console.Write("Reading Xcalibur data file ")
                ReportMessage("Reading Xcalibur data file")

                UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(filePath))

                ' Obtain the full path to the file
                Dim rawFileInfo = New FileInfo(filePath)
                Dim inputFileFullPath = rawFileInfo.FullName

                xcaliburAccessor.LoadMSMethodInfo = mOptions.WriteMSMethodFile
                xcaliburAccessor.LoadMSTuneInfo = mOptions.WriteMSTuneFile

                ' Open a handle to the data file
                If Not xcaliburAccessor.OpenRawFile(inputFileFullPath) Then
                    ReportError("Error opening input data file: " & inputFileFullPath & " (xcaliburAccessor.OpenRawFile returned False)")
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                If xcaliburAccessor Is Nothing Then
                    ReportError("Error opening input data file: " & inputFileFullPath & " (xcaliburAccessor is Nothing)")
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                Dim datasetID = mOptions.SICOptions.DatasetNumber
                Dim sicOptions = mOptions.SICOptions

                success = UpdateDatasetFileStats(rawFileInfo, datasetID, xcaliburAccessor)

                Dim metadataWriter = New DataOutput.clsThermoMetadataWriter()
                RegisterEvents(metadataWriter)

                If mOptions.WriteMSMethodFile Then
                    metadataWriter.SaveMSMethodFile(xcaliburAccessor, dataOutputHandler)
                End If

                If mOptions.WriteMSTuneFile Then
                    metadataWriter.SaveMSTuneFile(xcaliburAccessor, dataOutputHandler)
                End If

                Dim scanCount = xcaliburAccessor.GetNumScans()

                If scanCount <= 0 Then
                    ' No scans found
                    ReportError("No scans found in the input file: " & filePath)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                Dim scanStart = xcaliburAccessor.FileInfo.ScanStart
                Dim scanEnd = xcaliburAccessor.FileInfo.ScanEnd

                With sicOptions
                    If .ScanRangeStart > 0 And .ScanRangeEnd = 0 Then
                        .ScanRangeEnd = Integer.MaxValue
                    End If

                    If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                        scanStart = Math.Max(scanStart, .ScanRangeStart)
                        scanEnd = Math.Min(scanEnd, .ScanRangeEnd)
                    End If
                End With

                UpdateProgress(String.Format("Reading Xcalibur data with {0} ({1:N0} scans){2}", ioMode, scanCount, ControlChars.NewLine & Path.GetFileName(filePath)))
                ReportMessage(String.Format("Reading Xcalibur data with {0}; Total scan count: {1:N0}", ioMode, scanCount))

                Dim lastLogTime = DateTime.UtcNow

                ' Pre-reserve memory for the maximum number of scans that might be loaded
                ' Re-dimming after loading each scan is extremely slow and uses additional memory
                scanList.Initialize(scanEnd - scanStart + 1, scanEnd - scanStart + 1)
                Dim lastNonZoomSurveyScanIndex = -1

                Dim htSIMScanMapping = New Dictionary(Of String, Integer)
                scanList.SIMDataPresent = False
                scanList.MRMDataPresent = False

                For scanNumber = scanStart To scanEnd

                    Dim thermoScanInfo As ThermoRawFileReader.clsScanInfo = Nothing

                    success = xcaliburAccessor.GetScanInfo(scanNumber, thermoScanInfo)

                    If Not success Then
                        ' GetScanInfo returned false
                        ReportWarning("xcaliburAccessor.GetScanInfo returned false for scan " & scanNumber.ToString() & "; aborting read")
                        Exit For
                    End If

                    If mScanTracking.CheckScanInRange(scanNumber, thermoScanInfo.RetentionTime, sicOptions) Then

                        If thermoScanInfo.ParentIonMZ > 0 AndAlso Math.Abs(mOptions.ParentIonDecoyMassDa) > 0 Then
                            thermoScanInfo.ParentIonMZ += mOptions.ParentIonDecoyMassDa
                        End If

                        ' Determine if this was an MS/MS scan
                        ' If yes, determine the scan number of the survey scan
                        If thermoScanInfo.MSLevel <= 1 Then
                            ' Survey Scan
                            success = ExtractXcaliburSurveyScan(xcaliburAccessor,
                               scanList, spectraCache, dataOutputHandler, sicOptions,
                               keepRawSpectra, thermoScanInfo, htSIMScanMapping,
                               lastNonZoomSurveyScanIndex, scanNumber)

                        Else

                            ' Fragmentation Scan
                            success = ExtractXcaliburFragmentationScan(xcaliburAccessor,
                               scanList, spectraCache, dataOutputHandler, sicOptions, mOptions.BinningOptions,
                               keepRawSpectra, keepMSMSSpectra, thermoScanInfo,
                               lastNonZoomSurveyScanIndex, scanNumber)

                        End If

                    End If

                    If scanCount > 0 Then
                        If scanNumber Mod 10 = 0 Then
                            UpdateProgress(CShort(scanNumber / scanCount * 100))
                        End If
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(spectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If scanNumber Mod 100 = 0 Then
                        If DateTime.UtcNow.Subtract(lastLogTime).TotalSeconds >= 10 OrElse scanNumber Mod 500 = 0 Then
                            ReportMessage(String.Format("Reading scan: {0:N0}", scanNumber))
                            Console.Write(".")
                            lastLogTime = DateTime.UtcNow
                        End If

                        ' Call the garbage collector every 100 spectra
                        GC.Collect()
                        GC.WaitForPendingFinalizers()
                        Threading.Thread.Sleep(50)
                    End If

                Next
                Console.WriteLine()

                ' Shrink the memory usage of the scanList arrays
                ReDim Preserve scanList.MasterScanOrder(scanList.MasterScanOrderCount - 1)
                ReDim Preserve scanList.MasterScanNumList(scanList.MasterScanOrderCount - 1)
                ReDim Preserve scanList.MasterScanTimeList(scanList.MasterScanOrderCount - 1)

                If mPrecursorNotFoundCount > PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW Then
                    Dim precursorMissingPct As Double
                    If scanList.FragScans.Count > 0 Then
                        precursorMissingPct = mPrecursorNotFoundCount / CDbl(scanList.FragScans.Count) * 100
                    End If

                    OnWarningEvent(String.Format("Could not determine the precursor for {0:F1}% of the MS2 spectra ({1} / {2} scans)",
                                                 precursorMissingPct, mPrecursorNotFoundCount, scanList.FragScans.Count))
                End If

            Catch ex As Exception
                Console.WriteLine(ex.StackTrace)
                ReportError("Error in ExtractScanInfoFromXcaliburDataFile", ex, eMasicErrorCodes.InputFileDataReadError)
            End Try

            ' Record the current memory usage (before we close the .Raw file)
            OnUpdateMemoryUsage()

            ' Close the handle to the data file
            xcaliburAccessor.CloseRawFile()

            Return success

        End Function

        Private Function ExtractXcaliburSurveyScan(
          xcaliburAccessor As XRawFileIO,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          sicOptions As clsSICOptions,
          keepRawSpectra As Boolean,
          thermoScanInfo As ThermoRawFileReader.clsScanInfo,
          htSIMScanMapping As IDictionary(Of String, Integer),
          ByRef lastNonZoomSurveyScanIndex As Integer,
          scanNumber As Integer) As Boolean

            Dim scanInfo = New clsScanInfo() With {
                .ScanNumber = scanNumber,
                .ScanTime = CSng(thermoScanInfo.RetentionTime),
                .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(thermoScanInfo.FilterText),
                .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(thermoScanInfo.FilterText),
                .BasePeakIonMZ = thermoScanInfo.BasePeakMZ,
                .BasePeakIonIntensity = thermoScanInfo.BasePeakIntensity,
                .TotalIonIntensity = thermoScanInfo.TotalIonCurrent,
                .MinimumPositiveIntensity = 0,        ' This will be determined in LoadSpectraForFinniganDataFile
                .ZoomScan = thermoScanInfo.ZoomScan,
                .SIMScan = thermoScanInfo.SIMScan,
                .MRMScanType = thermoScanInfo.MRMScanType,
                .LowMass = thermoScanInfo.LowMass,
                .HighMass = thermoScanInfo.HighMass,
                .IsFTMS = thermoScanInfo.IsFTMS
            }

            ' Survey scans typically lead to multiple parent ions; we do not record them here
            scanInfo.FragScanInfo.ParentIonInfoIndex = -1

            If Not scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is an MRM scan
                scanList.MRMDataPresent = True
            End If

            If scanInfo.SIMScan Then
                scanList.SIMDataPresent = True
                Dim simKey = scanInfo.LowMass & "_" & scanInfo.HighMass
                Dim simIndex As Integer

                If htSIMScanMapping.TryGetValue(simKey, simIndex) Then
                    scanInfo.SIMIndex = simIndex
                Else
                    scanInfo.SIMIndex = htSIMScanMapping.Count
                    htSIMScanMapping.Add(simKey, htSIMScanMapping.Count)
                End If
            End If

            ' Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.ScanEvents)

            ' Store the collision mode and possibly the scan filter text
            scanInfo.FragScanInfo.CollisionMode = thermoScanInfo.CollisionMode
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, thermoScanInfo.CollisionMode)
            If mOptions.WriteExtendedStatsIncludeScanFilterText Then
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, thermoScanInfo.FilterText)
            End If

            If mOptions.WriteExtendedStatsStatusLog Then
                ' Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList)
            End If

            scanList.SurveyScans.Add(scanInfo)

            If Not scanInfo.ZoomScan Then
                lastNonZoomSurveyScanIndex = scanList.SurveyScans.Count - 1
            End If

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, scanList.SurveyScans.Count - 1)

            Dim msDataResolution As Double

            If sicOptions.SICToleranceIsPPM Then
                ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
                ' However, if the lowest m/z value is < 100, then use 100 m/z
                If thermoScanInfo.LowMass < 100 Then
                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                        sicOptions.CompressToleranceDivisorForPPM
                Else
                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, thermoScanInfo.LowMass) /
                        sicOptions.CompressToleranceDivisorForPPM
                End If
            Else
                msDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
            End If

            ' Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            Dim success = LoadSpectraForFinniganDataFile(xcaliburAccessor, spectraCache, scanNumber, scanInfo, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD, sicOptions.CompressMSSpectraData, msDataResolution, keepRawSpectra)
            If Not success Then Return False

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.SurveyScan, scanInfo, sicOptions.DatasetNumber)

            Return True

        End Function

        Private Function ExtractXcaliburFragmentationScan(
          xcaliburAccessor As XRawFileIO,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          sicOptions As clsSICOptions,
          binningOptions As clsBinningOptions,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean,
          thermoScanInfo As ThermoRawFileReader.clsScanInfo,
          ByRef lastNonZoomSurveyScanIndex As Integer,
          scanNumber As Integer) As Boolean

            ' Note that MinimumPositiveIntensity will be determined in LoadSpectraForFinniganDataFile

            Dim scanInfo = New clsScanInfo() With {
                .ScanNumber = scanNumber,
                .ScanTime = CSng(thermoScanInfo.RetentionTime),
                .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(thermoScanInfo.FilterText),
                .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(thermoScanInfo.FilterText),
                .BasePeakIonMZ = thermoScanInfo.BasePeakMZ,
                .BasePeakIonIntensity = thermoScanInfo.BasePeakIntensity,
                .TotalIonIntensity = thermoScanInfo.TotalIonCurrent,
                .MinimumPositiveIntensity = 0,
                .ZoomScan = thermoScanInfo.ZoomScan,
                .SIMScan = thermoScanInfo.SIMScan,
                .MRMScanType = thermoScanInfo.MRMScanType
            }

            ' Typically .EventNumber is 1 for the parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.
            ' This resets for each new parent-ion scan
            scanInfo.FragScanInfo.FragScanNumber = thermoScanInfo.EventNumber - 1

            ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
            scanInfo.FragScanInfo.MSLevel = thermoScanInfo.MSLevel

            ' The .EventNumber value is sometimes wrong; need to check for this
            ' For example, if the dataset only has MS2 scans and no parent-ion scan, .EventNumber will be 2 for every MS2 scan
            If scanList.FragScans.Count > 0 Then
                Dim prevFragScan = scanList.FragScans(scanList.FragScans.Count - 1)
                If prevFragScan.ScanNumber = scanInfo.ScanNumber - 1 Then
                    If scanInfo.FragScanInfo.FragScanNumber <= prevFragScan.FragScanInfo.FragScanNumber Then
                        scanInfo.FragScanInfo.FragScanNumber = prevFragScan.FragScanInfo.FragScanNumber + 1
                    End If
                End If
            End If

            If Not scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is an MRM scan
                scanList.MRMDataPresent = True

                scanInfo.MRMScanInfo = clsMRMProcessing.DuplicateMRMInfo(thermoScanInfo.MRMInfo, thermoScanInfo.ParentIonMZ)

                If scanList.SurveyScans.Count = 0 Then
                    ' Need to add a "fake" survey scan that we can map this parent ion to
                    lastNonZoomSurveyScanIndex = scanList.AddFakeSurveyScan()
                End If
            Else
                scanInfo.MRMScanInfo.MRMMassCount = 0
            End If

            With scanInfo
                .LowMass = thermoScanInfo.LowMass
                .HighMass = thermoScanInfo.HighMass
                .IsFTMS = thermoScanInfo.IsFTMS
            End With

            ' Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.ScanEvents)

            ' Store the collision mode and possibly the scan filter text
            scanInfo.FragScanInfo.CollisionMode = thermoScanInfo.CollisionMode
            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, thermoScanInfo.CollisionMode)
            If mOptions.WriteExtendedStatsIncludeScanFilterText Then
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, thermoScanInfo.FilterText)
            End If

            If mOptions.WriteExtendedStatsStatusLog Then
                ' Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, thermoScanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList)
            End If

            scanList.FragScans.Add(scanInfo)

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1)

            ' Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            Dim msDataResolution = binningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa

            Dim success = LoadSpectraForFinniganDataFile(
              xcaliburAccessor,
              spectraCache,
              scanNumber,
              scanInfo,
              sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
              DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
              sicOptions.CompressMSMSSpectraData,
              msDataResolution,
              keepRawSpectra AndAlso keepMSMSSpectra)

            If Not success Then Return False

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.FragScan, scanInfo, sicOptions.DatasetNumber)

            If thermoScanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is not an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, lastNonZoomSurveyScanIndex, thermoScanInfo.ParentIonMZ, scanList.FragScans.Count - 1, spectraCache, sicOptions)
            Else
                ' This is an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, lastNonZoomSurveyScanIndex, thermoScanInfo.ParentIonMZ, scanInfo.MRMScanInfo, spectraCache, sicOptions)
            End If

            If lastNonZoomSurveyScanIndex >= 0 Then
                Dim precursorScanNumber = scanList.SurveyScans(lastNonZoomSurveyScanIndex).ScanNumber

                ' Compute the interference of the parent ion in the MS1 spectrum for this frag scan
                scanInfo.FragScanInfo.InterferenceScore = ComputeInterference(xcaliburAccessor, thermoScanInfo, precursorScanNumber)
            End If

            Return True

        End Function

        Private Function LoadSpectraForFinniganDataFile(
          xcaliburAccessor As XRawFileIO,
          spectraCache As clsSpectraCache,
          scanNumber As Integer,
          scanInfo As clsScanInfo,
          noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
          discardLowIntensityData As Boolean,
          compressSpectraData As Boolean,
          msDataResolution As Double,
          keepRawSpectrum As Boolean) As Boolean

            Dim mzList() As Double = Nothing
            Dim intensityList() As Double = Nothing

            Dim lastKnownLocation = "Start"

            Try

                ' Load the ions for this scan

                lastKnownLocation = "xcaliburAccessor.GetScanData for scan " & scanNumber

                ' Retrieve the m/z and intensity values for the given scan
                ' We retrieve the profile-mode data, since that's required for determining spectrum noise
                scanInfo.IonCountRaw = xcaliburAccessor.GetScanData(scanNumber, mzList, intensityList)

                If scanInfo.IonCountRaw > 0 Then
                    Dim ionCountVerified = VerifyDataSorted(scanNumber, scanInfo.IonCountRaw, mzList, intensityList)
                    If ionCountVerified <> scanInfo.IonCountRaw Then
                        scanInfo.IonCountRaw = ionCountVerified
                    End If
                End If

                scanInfo.IonCount = scanInfo.IonCountRaw

                Dim objMSSpectrum As New clsMSSpectrum() With {
                    .ScanNumber = scanNumber,
                    .IonCount = scanInfo.IonCountRaw
                }

                lastKnownLocation = "Resize IonsMz and IonsIntensity to length " & objMSSpectrum.IonCount

                ReDim objMSSpectrum.IonsMZ(objMSSpectrum.IonCount - 1)
                ReDim objMSSpectrum.IonsIntensity(objMSSpectrum.IonCount - 1)

                ' Copy the intensity data; and compute the total scan intensity
                Dim totalIonIntensity As Double = 0
                For ionIndex = 0 To scanInfo.IonCountRaw - 1
                    objMSSpectrum.IonsMZ(ionIndex) = mzList(ionIndex)
                    objMSSpectrum.IonsIntensity(ionIndex) = CSng(intensityList(ionIndex))
                    totalIonIntensity += intensityList(ionIndex)
                Next

                ' Determine the minimum positive intensity in this scan
                lastKnownLocation = "Call mMASICPeakFinder.FindMinimumPositiveValue"
                scanInfo.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(scanInfo.IonCountRaw, objMSSpectrum.IonsIntensity, 0)

                If objMSSpectrum.IonCount > 0 Then
                    If scanInfo.TotalIonIntensity < Single.Epsilon Then
                        scanInfo.TotalIonIntensity = totalIonIntensity
                    End If
                Else
                    scanInfo.TotalIonIntensity = 0
                End If

                Dim discardLowIntensityDataWork As Boolean
                Dim compressSpectraDataWork As Boolean

                If scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                    discardLowIntensityDataWork = discardLowIntensityData
                    compressSpectraDataWork = compressSpectraData
                Else
                    discardLowIntensityDataWork = False
                    compressSpectraDataWork = False
                End If

                lastKnownLocation = "Call ProcessAndStoreSpectrum"
                mScanTracking.ProcessAndStoreSpectrum(
                    scanInfo, Me,
                    spectraCache, objMSSpectrum,
                    noiseThresholdOptions,
                    discardLowIntensityDataWork,
                    compressSpectraDataWork,
                    msDataResolution,
                    keepRawSpectrum)

            Catch ex As Exception
                ReportError("Error in LoadSpectraForFinniganDataFile (LastKnownLocation: " & lastKnownLocation & ")", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

            Return True

        End Function

        Protected Overloads Function UpdateDatasetFileStats(
          rawFileInfo As FileInfo,
          datasetID As Integer,
          xcaliburAccessor As XRawFileIO) As Boolean

            Dim scanInfo = New ThermoRawFileReader.clsScanInfo(0)

            Dim scanEnd As Integer
            Dim success As Boolean

            ' Read the file info from the file system
            success = UpdateDatasetFileStats(rawFileInfo, datasetID)

            If Not success Then Return False

            ' Read the file info using the Xcalibur Accessor
            Try
                mDatasetFileInfo.AcqTimeStart = xcaliburAccessor.FileInfo.CreationDate
            Catch ex As Exception
                ' Read error
                Return False
            End Try

            Try
                ' Look up the end scan time then compute .AcqTimeEnd
                Dim scanEnd = xcaliburAccessor.FileInfo.ScanEnd
                xcaliburAccessor.GetScanInfo(scanEnd, scanInfo)

                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanInfo.RetentionTime)
                mDatasetFileInfo.ScanCount = xcaliburAccessor.GetNumScans()

            Catch ex As Exception
                ' Error; use default values
                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart
                mDatasetFileInfo.ScanCount = 0
            End Try

            Return True

        End Function

        Private Sub StoreExtendedHeaderInfo(
          dataOutputHandler As DataOutput.clsDataOutput,
          scanInfo As clsScanInfo,
          entryName As String,
          entryValue As String)

            If entryValue Is Nothing Then
                entryValue = String.Empty
            End If

            Dim statusEntries = New List(Of KeyValuePair(Of String, String)) From {
                New KeyValuePair(Of String, String)(entryName, entryValue)
            }

            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, statusEntries)

        End Sub

        Private Sub StoreExtendedHeaderInfo(
          dataOutputHandler As DataOutput.clsDataOutput,
          scanInfo As clsScanInfo,
          statusEntries As IReadOnlyCollection(Of KeyValuePair(Of String, String)))

            StoreExtendedHeaderInfo(dataOutputHandler, scanInfo, statusEntries, New SortedSet(Of String))
        End Sub

        Private Sub StoreExtendedHeaderInfo(
          dataOutputHandler As DataOutput.clsDataOutput,
          scanInfo As clsScanInfo,
          statusEntries As IReadOnlyCollection(Of KeyValuePair(Of String, String)),
          keyNameFilterList As IReadOnlyCollection(Of String))

            Dim filterItems As Boolean
            Dim saveItem As Boolean

            Try
                If (statusEntries Is Nothing) Then Exit Sub

                If Not keyNameFilterList Is Nothing AndAlso keyNameFilterList.Count > 0 Then
                    If keyNameFilterList.Any(Function(item) item.Length > 0) Then
                        filterItems = True
                    End If
                End If

                For Each statusEntry In statusEntries
                    If String.IsNullOrWhiteSpace(statusEntry.Key) Then
                        ' Empty entry name; do not add
                        Continue For
                    End If

                    If filterItems Then
                        saveItem = False

                        For Each item In keyNameFilterList
                            If statusEntry.Key.ToLower().Contains(item.ToLower()) Then
                                saveItem = True
                                Exit For
                            End If
                        Next
                    Else
                        saveItem = True
                    End If

                    If String.IsNullOrWhiteSpace(statusEntry.Key) OrElse statusEntry.Key = ChrW(1) Then
                        ' Name is null; skip it
                        saveItem = False
                    End If

                    If saveItem Then

                        Dim extendedHeaderID = dataOutputHandler.ExtendedStatsWriter.GetExtendedHeaderInfoIdByName(statusEntry.Key)

                        ' Add or update the value for extendedHeaderID
                        scanInfo.ExtendedHeaderInfo(extendedHeaderID) = statusEntry.Value.Trim()

                    End If

                Next

            Catch ex As Exception
                ' Ignore any errors here
            End Try

        End Sub

        ''' <summary>
        ''' Verify that data in mzList is sorted ascending
        ''' </summary>
        ''' <param name="scanNumber">Scan number</param>
        ''' <param name="ionCount">Expected length of mzList and intensityList</param>
        ''' <param name="mzList"></param>
        ''' <param name="intensityList"></param>
        ''' <returns>Number of data points in mzList</returns>
        Private Function VerifyDataSorted(scanNumber As Integer, ionCount As Integer, mzList As Double(), intensityList As Double()) As Integer

            If ionCount <> mzList.Length Then
                If ionCount = 0 Then
                    ReportWarning("Scan found with IonCount = 0, scan " & scanNumber)
                Else
                    ReportWarning(String.Format(
                                  "Scan found where IonCount <> mzList.Length, scan {0}: {1} vs. {2}",
                                  scanNumber, ionCount, mzList.Length))
                End If
                ionCount = Math.Min(ionCount, mzList.Length)
            End If

            Dim sortRequired = False

            For index = 1 To ionCount - 1
                ' Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z,
                ' we have observed a few cases in certain scans of certain datasets that points with
                ' similar m/z values are swapped and ths slightly out of order
                ' The following if statement checks for this
                If (mzList(index) < mzList(index - 1)) Then
                    sortRequired = True
                    Exit For
                End If
            Next

            If sortRequired Then
                Array.Sort(mzList, intensityList)
            End If

            Return ionCount

        End Function

        Private Sub InterferenceWarningEventHandler(message As String)
            If (message.StartsWith("Did not find the precursor for")) Then
                mPrecursorNotFoundCount += 1
                If mPrecursorNotFoundCount <= PRECURSOR_NOT_FOUND_WARNINGS_TO_SHOW Then
                    OnWarningEvent(message)
                End If
            Else
                OnWarningEvent(message)
            End If
        End Sub

    End Class

End Namespace