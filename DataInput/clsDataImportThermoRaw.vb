Imports MASIC.clsMASIC
Imports ThermoRawFileReader

Namespace DataInput

    Public Class clsDataImportThermoRaw
        Inherits clsDataImport

        Private Const SCAN_EVENT_CHARGE_STATE = "Charge State"
        Private Const SCAN_EVENT_MONOISOTOPIC_MZ = "Monoisotopic M/Z"
        Private Const SCAN_EVENT_MS2_ISOLATION_WIDTH = "MS2 Isolation Width"

        Private ReadOnly mInterferenceCalculator As InterferenceCalculator

        Private ReadOnly mCachedPrecursorIons As List(Of InterDetect.Peak)
        Private mCachedPrecursorScan As Integer

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

            mInterferenceCalculator = New InterferenceCalculator()
            RegisterEvents(mInterferenceCalculator)

            mCachedPrecursorIons = New List(Of InterDetect.Peak)
            mCachedPrecursorScan = 0

        End Sub

        Private Function ComputeInterference(xcaliburAccessor As XRawFileIO, scanInfo As ThermoRawFileReader.clsScanInfo, precursorScanNumber As Integer) As Double

            If Math.Abs(scanInfo.ParentIonMZ) < Single.Epsilon Then
                ReportWarning("Parent ion m/z is 0; cannot compute inteference for scan " & scanInfo.ScanNumber)
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

            Dim chargeStateText = ""
            Dim isolationWidthText = ""

            scanInfo.TryGetScanEvent(SCAN_EVENT_CHARGE_STATE, chargeStateText, True)
            If Not String.IsNullOrWhiteSpace(chargeStateText) Then
                If Not Integer.TryParse(chargeStateText, chargeState) Then
                    chargeState = 0
                End If
            End If

            If Not scanInfo.TryGetScanEvent(SCAN_EVENT_MS2_ISOLATION_WIDTH, isolationWidthText, True) Then
                ReportWarning("Could not determine the MS2 isolation width (" & SCAN_EVENT_MS2_ISOLATION_WIDTH & "); " &
                              "cannot compute inteference for scan " & scanInfo.ScanNumber)
                Return 0
            End If

            If Not Double.TryParse(isolationWidthText, isolationWidth) Then
                ReportWarning("MS2 isolation width (" & SCAN_EVENT_MS2_ISOLATION_WIDTH & ") was non-numeric (" & isolationWidthText & "); " &
                              "cannot compute inteference for scan " & scanInfo.ScanNumber)
                Return 0
            End If

            Dim parentIonMz As Double

            If scanInfo.ParentIonMZ > 0 Then
                parentIonMz = scanInfo.ParentIonMZ
            Else
                ' ThermoRawFileReader could not determine the parent ion m/z value (this is highly unlikely)
                ' Use scan event "Monoisotopic M/Z" instead
                Dim monoMzText = ""
                If (Not scanInfo.TryGetScanEvent(SCAN_EVENT_MONOISOTOPIC_MZ, monoMzText, True)) Then

                    ReportWarning("Could not determine the parent ion m/z value (" & SCAN_EVENT_MONOISOTOPIC_MZ & "); " &
                                  "cannot compute inteference for scan " & scanInfo.ScanNumber)
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

            Dim oPrecursorInfo = New PrecursorInfo(parentIonMz, isolationWidth, chargeState) With {
                .ScanNumber = precursorScanNumber
            }

            mInterferenceCalculator.Interference(oPrecursorInfo, mCachedPrecursorIons)

            Return oPrecursorInfo.Interference

        End Function

        Public Function ExtractScanInfoFromXcaliburDataFile(
          strFilePath As String,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          blnKeepRawSpectra As Boolean,
          blnKeepMSMSSpectra As Boolean) As Boolean

            ' Returns True if Success, False if failure
            ' Note: This function assumes strFilePath exists

            ' Use Xraw to read the .Raw files
            Dim xcaliburAccessor = New XRawFileIO()
            AddHandler xcaliburAccessor.ReportError, AddressOf mXcaliburAccessor_ReportError
            AddHandler xcaliburAccessor.ReportWarning, AddressOf mXcaliburAccessor_ReportWarning

            Dim strIOMode = "Xraw"

            ' Assume success for now
            Dim blnSuccess = True

            Try
                Console.Write("Reading Xcalibur data file ")
                ReportMessage("Reading Xcalibur data file")

                UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(strFilePath))

                ' Obtain the full path to the file
                Dim ioFileInfo = New FileInfo(strFilePath)
                Dim strInputFileFullPath = ioFileInfo.FullName

                xcaliburAccessor.LoadMSMethodInfo = mOptions.WriteMSMethodFile
                xcaliburAccessor.LoadMSTuneInfo = mOptions.WriteMSTuneFile

                ' Open a handle to the data file
                If Not xcaliburAccessor.OpenRawFile(strInputFileFullPath) Then
                    ReportError("ExtractScanInfoFromXcaliburDataFile",
                            "Error opening input data file: " & strInputFileFullPath & " (xcaliburAccessor.OpenRawFile returned False)")
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                If xcaliburAccessor Is Nothing Then
                    ReportError("ExtractScanInfoFromXcaliburDataFile",
                            "Error opening input data file: " & strInputFileFullPath & " (xcaliburAccessor is Nothing)")
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                Dim intDatasetID = mOptions.SICOptions.DatasetNumber
                Dim sicOptions = mOptions.SICOptions

                blnSuccess = UpdateDatasetFileStats(ioFileInfo, intDatasetID, xcaliburAccessor)

                Dim metadataWriter = New DataOutput.clsThermoMetadataWriter()
                RegisterEvents(metadataWriter)

                If mOptions.WriteMSMethodFile Then
                    metadataWriter.SaveMSMethodFile(xcaliburAccessor, dataOutputHandler)
                End If

                If mOptions.WriteMSTuneFile Then
                    metadataWriter.SaveMSTuneFile(xcaliburAccessor, dataOutputHandler)
                End If

                Dim intScanCount = xcaliburAccessor.GetNumScans()

                If intScanCount <= 0 Then
                    ' No scans found
                    ReportError("ExtractScanInfoFromXcaliburDataFile", "No scans found in the input file: " & strFilePath)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                Dim intScanStart = xcaliburAccessor.FileInfo.ScanStart
                Dim intScanEnd = xcaliburAccessor.FileInfo.ScanEnd

                With sicOptions
                    If .ScanRangeStart > 0 And .ScanRangeEnd = 0 Then
                        .ScanRangeEnd = Integer.MaxValue
                    End If

                    If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                        intScanStart = Math.Max(intScanStart, .ScanRangeStart)
                        intScanEnd = Math.Min(intScanEnd, .ScanRangeEnd)
                    End If
                End With

                UpdateProgress("Reading Xcalibur data with " & strIOMode & " (" & intScanCount.ToString() & " scans)" & ControlChars.NewLine & Path.GetFileName(strFilePath))
                ReportMessage("Reading Xcalibur data with " & strIOMode & "; Total scan count: " & intScanCount.ToString())
                Dim dtLastLogTime = DateTime.UtcNow

                ' Pre-reserve memory for the maximum number of scans that might be loaded
                ' Re-dimming after loading each scan is extremly slow and uses additional memory
                scanList.Initialize(intScanEnd - intScanStart + 1, intScanEnd - intScanStart + 1)
                Dim intLastNonZoomSurveyScanIndex = -1

                Dim htSIMScanMapping = New Dictionary(Of String, Integer)
                scanList.SIMDataPresent = False
                scanList.MRMDataPresent = False

                For intScanNumber = intScanStart To intScanEnd

                    Dim scanInfo As ThermoRawFileReader.clsScanInfo = Nothing

                    blnSuccess = xcaliburAccessor.GetScanInfo(intScanNumber, scanInfo)

                    If Not blnSuccess Then
                        ' GetScanInfo returned false
                        ReportWarning("ExtractScanInfoFromXcaliburDataFile",
                                  "xcaliburAccessor.GetScanInfo returned false for scan " & intScanNumber.ToString() & "; aborting read")
                        Exit For
                    End If

                    If mScanTracking.CheckScanInRange(intScanNumber, scanInfo.RetentionTime, sicOptions) Then

                        If scanInfo.ParentIonMZ > 0 AndAlso Math.Abs(mOptions.ParentIonDecoyMassDa) > 0 Then
                            scanInfo.ParentIonMZ += mOptions.ParentIonDecoyMassDa
                        End If

                        ' Determine if this was an MS/MS scan
                        ' If yes, determine the scan number of the survey scan
                        If scanInfo.MSLevel <= 1 Then
                            ' Survey Scan
                            blnSuccess = ExtractXcaliburSurveyScan(xcaliburAccessor,
                               scanList, objSpectraCache, dataOutputHandler, sicOptions,
                               blnKeepRawSpectra, scanInfo, htSIMScanMapping,
                               intLastNonZoomSurveyScanIndex, intScanNumber)

                        Else

                            ' Fragmentation Scan
                            blnSuccess = ExtractXcaliburFragmentationScan(xcaliburAccessor,
                               scanList, objSpectraCache, dataOutputHandler, sicOptions, mOptions.BinningOptions,
                               blnKeepRawSpectra, blnKeepMSMSSpectra, scanInfo,
                               intLastNonZoomSurveyScanIndex, intScanNumber)

                        End If

                    End If

                    If intScanCount > 0 Then
                        If intScanNumber Mod 10 = 0 Then
                            UpdateProgress(CShort(intScanNumber / intScanCount * 100))
                        End If
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If intScanNumber Mod 100 = 0 Then
                        If DateTime.UtcNow.Subtract(dtLastLogTime).TotalSeconds >= 10 OrElse intScanNumber Mod 500 = 0 Then
                            ReportMessage("Reading scan: " & intScanNumber.ToString())
                            Console.Write(".")
                            dtLastLogTime = DateTime.UtcNow
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

            Catch ex As Exception
                Console.WriteLine(ex.StackTrace)
                ReportError("ExtractScanInfoFromXcaliburDataFile",
                        "Error in ExtractScanInfoFromXcaliburDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            End Try

            ' Record the current memory usage (before we close the .Raw file)
            OnUpdateMemoryUsage()

            ' Close the handle to the data file
            xcaliburAccessor.CloseRawFile()

            Return blnSuccess

        End Function

        Private Function ExtractXcaliburSurveyScan(
          xcaliburAccessor As XRawFileIO,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          sicOptions As clsSICOptions,
          blnKeepRawSpectra As Boolean,
          scanInfo As ThermoRawFileReader.clsScanInfo,
          htSIMScanMapping As IDictionary(Of String, Integer),
          ByRef intLastNonZoomSurveyScanIndex As Integer,
          intScanNumber As Integer) As Boolean

            Dim newSurveyScan = New clsScanInfo()
            With newSurveyScan
                .ScanNumber = intScanNumber
                .ScanTime = CSng(scanInfo.RetentionTime)

                .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(scanInfo.FilterText)
                .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.FilterText)

                .BasePeakIonMZ = scanInfo.BasePeakMZ
                .BasePeakIonIntensity = Math.Min(CSng(scanInfo.BasePeakIntensity), Single.MaxValue)

                .FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here
                .TotalIonIntensity = Math.Min(CSng(scanInfo.TotalIonCurrent), Single.MaxValue)

                ' This will be determined in LoadSpectraForFinniganDataFile
                .MinimumPositiveIntensity = 0

                .ZoomScan = scanInfo.ZoomScan
                .SIMScan = scanInfo.SIMScan
                .MRMScanType = scanInfo.MRMScanType

                If Not .MRMScanType = MRMScanTypeConstants.NotMRM Then
                    ' This is an MRM scan
                    scanList.MRMDataPresent = True
                End If

                .LowMass = scanInfo.LowMass
                .HighMass = scanInfo.HighMass
                .IsFTMS = scanInfo.IsFTMS

                If .SIMScan Then
                    scanList.SIMDataPresent = True
                    Dim strSIMKey = .LowMass & "_" & .HighMass
                    Dim simIndex As Integer

                    If htSIMScanMapping.TryGetValue(strSIMKey, simIndex) Then
                        .SIMIndex = simIndex
                    Else
                        .SIMIndex = htSIMScanMapping.Count
                        htSIMScanMapping.Add(strSIMKey, htSIMScanMapping.Count)
                    End If
                End If
            End With

            ' Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, newSurveyScan, scanInfo.ScanEvents)

            ' Store the collision mode and possibly the scan filter text
            newSurveyScan.FragScanInfo.CollisionMode = scanInfo.CollisionMode
            StoreExtendedHeaderInfo(dataOutputHandler, newSurveyScan, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, scanInfo.CollisionMode)
            If mOptions.WriteExtendedStatsIncludeScanFilterText Then
                StoreExtendedHeaderInfo(dataOutputHandler, newSurveyScan, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanInfo.FilterText)
            End If

            If mOptions.WriteExtendedStatsStatusLog Then
                ' Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, newSurveyScan, scanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList)
            End If

            scanList.SurveyScans.Add(newSurveyScan)

            If Not newSurveyScan.ZoomScan Then
                intLastNonZoomSurveyScanIndex = scanList.SurveyScans.Count - 1
            End If

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, scanList.SurveyScans.Count - 1)

            Dim dblMSDataResolution As Double

            If sicOptions.SICToleranceIsPPM Then
                ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
                ' However, if the lowest m/z value is < 100, then use 100 m/z
                If scanInfo.LowMass < 100 Then
                    dblMSDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                        sicOptions.CompressToleranceDivisorForPPM
                Else
                    dblMSDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, scanInfo.LowMass) /
                        sicOptions.CompressToleranceDivisorForPPM
                End If
            Else
                dblMSDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
            End If

            ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            Dim blnSuccess = LoadSpectraForFinniganDataFile(xcaliburAccessor, objSpectraCache, intScanNumber, newSurveyScan, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD, sicOptions.CompressMSSpectraData, dblMSDataResolution, blnKeepRawSpectra)
            If Not blnSuccess Then Return False

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.SurveyScan, newSurveyScan, sicOptions.DatasetNumber)

            Return True

        End Function

        Private Function ExtractXcaliburFragmentationScan(
          xcaliburAccessor As XRawFileIO,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          sicOptions As clsSICOptions,
          binningOptions As clsBinningOptions,
          blnKeepRawSpectra As Boolean,
          blnKeepMSMSSpectra As Boolean,
          scanInfo As ThermoRawFileReader.clsScanInfo,
          ByRef intLastNonZoomSurveyScanIndex As Integer,
          intScanNumber As Integer) As Boolean

            ' Note that MinimumPositiveIntensity will be determined in LoadSpectraForFinniganDataFile

            Dim newFragScan = New clsScanInfo() With {
                .ScanNumber = intScanNumber,
                .ScanTime = CSng(scanInfo.RetentionTime),
                .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(scanInfo.FilterText),
                .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.FilterText),
                .BasePeakIonMZ = scanInfo.BasePeakMZ,
                .BasePeakIonIntensity = Math.Min(CSng(scanInfo.BasePeakIntensity), Single.MaxValue),
                .TotalIonIntensity = Math.Min(CSng(scanInfo.TotalIonCurrent), Single.MaxValue),
                .MinimumPositiveIntensity = 0,
                .ZoomScan = scanInfo.ZoomScan,
                .SIMScan = scanInfo.SIMScan,
                .MRMScanType = scanInfo.MRMScanType
            }

            ' Typically .EventNumber is 1 for the parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.
            ' This resets for each new parent-ion scan
            newFragScan.FragScanInfo.FragScanNumber = scanInfo.EventNumber - 1

            ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
            newFragScan.FragScanInfo.MSLevel = scanInfo.MSLevel

            ' The .EventNumber value is sometimes wrong; need to check for this
            ' For example, if the dataset only has MS2 scans and no parent-ion scan, .EventNumber will be 2 for every MS2 scan
            If scanList.FragScans.Count > 0 Then
                Dim prevFragScan = scanList.FragScans(scanList.FragScans.Count - 1)
                If prevFragScan.ScanNumber = newFragScan.ScanNumber - 1 Then
                    If newFragScan.FragScanInfo.FragScanNumber <= prevFragScan.FragScanInfo.FragScanNumber Then
                        newFragScan.FragScanInfo.FragScanNumber = prevFragScan.FragScanInfo.FragScanNumber + 1
                    End If
                End If
            End If

            If Not newFragScan.MRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is an MRM scan
                scanList.MRMDataPresent = True

                newFragScan.MRMScanInfo = clsMRMProcessing.DuplicateMRMInfo(scanInfo.MRMInfo, scanInfo.ParentIonMZ)

                If scanList.SurveyScans.Count = 0 Then
                    ' Need to add a "fake" survey scan that we can map this parent ion to
                    intLastNonZoomSurveyScanIndex = scanList.AddFakeSurveyScan()
                End If
            Else
                newFragScan.MRMScanInfo.MRMMassCount = 0
            End If

            With newFragScan
                .LowMass = scanInfo.LowMass
                .HighMass = scanInfo.HighMass
                .IsFTMS = scanInfo.IsFTMS
            End With

            ' Store the ScanEvent values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(dataOutputHandler, newFragScan, scanInfo.ScanEvents)

            ' Store the collision mode and possibly the scan filter text
            newFragScan.FragScanInfo.CollisionMode = scanInfo.CollisionMode
            StoreExtendedHeaderInfo(dataOutputHandler, newFragScan, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_COLLISION_MODE, scanInfo.CollisionMode)
            If mOptions.WriteExtendedStatsIncludeScanFilterText Then
                StoreExtendedHeaderInfo(dataOutputHandler, newFragScan, DataOutput.clsExtendedStatsWriter.EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanInfo.FilterText)
            End If

            If mOptions.WriteExtendedStatsStatusLog Then
                ' Store the StatusLog values in .ExtendedHeaderInfo
                StoreExtendedHeaderInfo(dataOutputHandler, newFragScan, scanInfo.StatusLog, mOptions.StatusLogKeyNameFilterList)
            End If

            scanList.FragScans.Add(newFragScan)

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1)

            ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            Dim dblMSDataResolution = binningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa

            Dim blnSuccess = LoadSpectraForFinniganDataFile(
              xcaliburAccessor,
              objSpectraCache,
              intScanNumber,
              newFragScan,
              sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
              DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
              sicOptions.CompressMSMSSpectraData,
              dblMSDataResolution,
              blnKeepRawSpectra And blnKeepMSMSSpectra)

            If Not blnSuccess Then Return False

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.FragScan, newFragScan, sicOptions.DatasetNumber)

            If scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is not an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, intLastNonZoomSurveyScanIndex, scanInfo.ParentIonMZ, scanList.FragScans.Count - 1, objSpectraCache, sicOptions)
            Else
                ' This is an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, intLastNonZoomSurveyScanIndex, scanInfo.ParentIonMZ, newFragScan.MRMScanInfo, objSpectraCache, sicOptions)
            End If

            If intLastNonZoomSurveyScanIndex >= 0 Then
                Dim precursorScanNumber = scanList.SurveyScans(intLastNonZoomSurveyScanIndex).ScanNumber

                ' Compute the interference of the parent ion in the MS1 spectrum for this frag scan
                newFragScan.FragScanInfo.InteferenceScore = ComputeInterference(xcaliburAccessor, scanInfo, precursorScanNumber)
            End If

            Return True

        End Function

        Private Function LoadSpectraForFinniganDataFile(
          objXcaliburAccessor As XRawFileIO,
          objSpectraCache As clsSpectraCache,
          intScanNumber As Integer,
          scanInfo As clsScanInfo,
          noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
          blnDiscardLowIntensityData As Boolean,
          blnCompressSpectraData As Boolean,
          dblMSDataResolution As Double,
          blnKeepRawSpectrum As Boolean) As Boolean

            Dim dblMzList() As Double = Nothing
            Dim dblIntensityList() As Double = Nothing

            Dim strLastKnownLocation = "Start"

            Try

                ' Load the ions for this scan

                strLastKnownLocation = "objXcaliburAccessor.GetScanData for scan " & intScanNumber

                ' Retrieve the m/z and intensity values for the given scan
                ' We retrieve the profile-mode data, since that's required for determing spectrum noise
                scanInfo.IonCountRaw = objXcaliburAccessor.GetScanData(intScanNumber, dblMzList, dblIntensityList)

                If scanInfo.IonCountRaw > 0 Then
                    Dim ionCountVerified = VerifyDataSorted(intScanNumber, scanInfo.IonCountRaw, dblMzList, dblIntensityList)
                    If ionCountVerified <> scanInfo.IonCountRaw Then
                        scanInfo.IonCountRaw = ionCountVerified
                    End If
                End If

                Dim objMSSpectrum As New clsMSSpectrum() With {
                    .ScanNumber = intScanNumber,
                    .IonCount = scanInfo.IonCountRaw
                }

                If objMSSpectrum.IonCount = 0 Then
                    Return False
                End If

                strLastKnownLocation = "Redim IonsMz and IonsIntensity to length " & objMSSpectrum.IonCount
                ReDim objMSSpectrum.IonsMZ(objMSSpectrum.IonCount - 1)
                ReDim objMSSpectrum.IonsIntensity(objMSSpectrum.IonCount - 1)

                ' Copy the intensity data; and compute the total scan intensity
                Dim dblTIC As Double = 0
                For intIonIndex = 0 To scanInfo.IonCountRaw - 1
                    objMSSpectrum.IonsMZ(intIonIndex) = dblMzList(intIonIndex)
                    objMSSpectrum.IonsIntensity(intIonIndex) = CSng(dblIntensityList(intIonIndex))
                    dblTIC += dblIntensityList(intIonIndex)
                Next

                ' Determine the minimum positive intensity in this scan
                strLastKnownLocation = "Call mMASICPeakFinder.FindMinimumPositiveValue"
                scanInfo.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(scanInfo.IonCountRaw, objMSSpectrum.IonsIntensity, 0)

                If objMSSpectrum.IonCount > 0 Then
                    If scanInfo.TotalIonIntensity < Single.Epsilon Then
                        scanInfo.TotalIonIntensity = CSng(Math.Min(dblTIC, Single.MaxValue))
                    End If

                    Dim blnDiscardLowIntensityDataWork As Boolean
                    Dim blnCompressSpectraDataWork As Boolean

                    If scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                        blnDiscardLowIntensityDataWork = blnDiscardLowIntensityData
                        blnCompressSpectraDataWork = blnCompressSpectraData
                    Else
                        blnDiscardLowIntensityDataWork = False
                        blnCompressSpectraDataWork = False
                    End If

                    strLastKnownLocation = "Call ProcessAndStoreSpectrum"
                    mScanTracking.ProcessAndStoreSpectrum(
                        scanInfo, Me,
                        objSpectraCache, objMSSpectrum,
                        noiseThresholdOptions,
                        blnDiscardLowIntensityDataWork,
                        blnCompressSpectraDataWork,
                        dblMSDataResolution,
                        blnKeepRawSpectrum)
                Else
                    scanInfo.TotalIonIntensity = 0
                End If

            Catch ex As Exception
                ReportError("LoadSpectraForFinniganDataFile", "Error in LoadSpectraForFinniganDataFile (LastKnownLocation: " & strLastKnownLocation & ")", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

            Return True

        End Function

        Protected Overloads Function UpdateDatasetFileStats(
          ioFileInfo As FileInfo,
          intDatasetID As Integer,
          ByRef objXcaliburAccessor As XRawFileIO) As Boolean

            Dim scanInfo = New ThermoRawFileReader.clsScanInfo(0)

            Dim intScanEnd As Integer
            Dim blnSuccess As Boolean

            ' Read the file info from the file system
            blnSuccess = MyBase.UpdateDatasetFileStats(ioFileInfo, intDatasetID)

            If Not blnSuccess Then Return False

            ' Read the file info using the Xcalibur Accessor
            Try
                mDatasetFileInfo.AcqTimeStart = objXcaliburAccessor.FileInfo.CreationDate
            Catch ex As Exception
                ' Read error
                blnSuccess = False
            End Try

            If blnSuccess Then
                Try
                    ' Look up the end scan time then compute .AcqTimeEnd
                    intScanEnd = objXcaliburAccessor.FileInfo.ScanEnd
                    objXcaliburAccessor.GetScanInfo(intScanEnd, scanInfo)

                    With mDatasetFileInfo
                        .AcqTimeEnd = .AcqTimeStart.AddMinutes(scanInfo.RetentionTime)
                        .ScanCount = objXcaliburAccessor.GetNumScans()
                    End With

                Catch ex As Exception
                    ' Error; use default values
                    With mDatasetFileInfo
                        .AcqTimeEnd = .AcqTimeStart
                        .ScanCount = 0
                    End With
                End Try
            End If

            Return blnSuccess

        End Function

        Private Sub StoreExtendedHeaderInfo(
          dataOutputHandler As DataOutput.clsDataOutput,
          scanInfo As clsScanInfo,
          strEntryName As String,
          strEntryValue As String)

            If strEntryValue Is Nothing Then
                strEntryValue = String.Empty
            End If

            Dim statusEntries = New List(Of KeyValuePair(Of String, String)) From {
                New KeyValuePair(Of String, String)(strEntryName, strEntryValue)
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

            Dim blnFilterItems As Boolean
            Dim blnSaveItem As Boolean

            Try
                If (statusEntries Is Nothing) Then Exit Sub

                If Not keyNameFilterList Is Nothing AndAlso keyNameFilterList.Count > 0 Then
                    If keyNameFilterList.Any(Function(item) item.Length > 0) Then
                        blnFilterItems = True
                    End If
                End If

                For Each statusEntry In statusEntries
                    If String.IsNullOrWhiteSpace(statusEntry.Key) Then
                        ' Empty entry name; do not add
                        Continue For
                    End If

                    If blnFilterItems Then
                        blnSaveItem = False

                        For Each item In keyNameFilterList
                            If statusEntry.Key.ToLower().Contains(item.ToLower()) Then
                                blnSaveItem = True
                                Exit For
                            End If
                        Next
                    Else
                        blnSaveItem = True
                    End If

                    If blnSaveItem Then
                        Dim extendedHeaderID = dataOutputHandler.ExtendedStatsWriter.GetExtendedHeaderInfoIdByName(statusEntry.Key)

                        ' Add or update the value for intIDValue
                        scanInfo.ExtendedHeaderInfo(extendedHeaderID) = statusEntry.Value

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

            For intIndex = 1 To ionCount - 1
                ' Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z,
                ' we have observed a few cases in certain scans of certain datasets that points with
                ' similar m/z values are swapped and ths slightly out of order
                ' The following if statement checks for this
                If (mzList(intIndex) < mzList(intIndex - 1)) Then
                    sortRequired = True
                    Exit For
                End If
            Next

            If sortRequired Then
                Array.Sort(mzList, intensityList)
            End If

            Return ionCount

        End Function

        Private Sub mXcaliburAccessor_ReportError(strMessage As String)
            Console.WriteLine(strMessage)
            ReportError("XcaliburAccessor", strMessage, Nothing, True, False, eMasicErrorCodes.InputFileDataReadError)
        End Sub

        Private Sub mXcaliburAccessor_ReportWarning(strMessage As String)
            Console.WriteLine(strMessage)
            ReportError("XcaliburAccessor", strMessage, Nothing, False, False, eMasicErrorCodes.InputFileDataReadError)
        End Sub

    End Class

End Namespace