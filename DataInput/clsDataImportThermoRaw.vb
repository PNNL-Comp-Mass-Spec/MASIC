Imports System.Runtime.InteropServices
Imports ThermoRawFileReader

Public Class clsDataImportThermoRaw
    Inherits clsDataImport

    Public Sub New(masicOptions As clsMASICOptions, peakFinder As MASICPeakFinder.clsMASICPeakFinder, parentIonProcessor As clsParentIonProcessing)
        MyBase.New(masicOptions, peakFinder, parentIonProcessor)
    End Sub

    Public Function ExtractScanInfoFromXcaliburDataFile(
      strFilePath As String,
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      dataOutputHandler As clsDataOutput,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean) As Boolean

        ' Returns True if Success, False if failure
        ' Note: This function assumes strFilePath exists

        Dim ioFileInfo As FileInfo
        Dim strInputFileFullPath As String

        Dim intScanCount As Integer
        Dim intLastNonZoomSurveyScanIndex As Integer

        Dim intScanStart As Integer
        Dim intScanEnd As Integer
        Dim intScanNumber As Integer

        Dim blnSuccess As Boolean

        Dim strIOMode As String
        Dim dtLastLogTime As DateTime

        ' Use Xraw to read the .Raw files
        Dim xcaliburAccessor = New XRawFileIO()
        AddHandler xcaliburAccessor.ReportError, AddressOf mXcaliburAccessor_ReportError
        AddHandler xcaliburAccessor.ReportWarning, AddressOf mXcaliburAccessor_ReportWarning

        strIOMode = "Xraw"

        ' Assume success for now
        blnSuccess = True

        Try
            Console.Write("Reading Xcalibur data file ")
            ReportMessage("Reading Xcalibur data file")

            UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(strFilePath))

            ' Obtain the full path to the file
            ioFileInfo = New FileInfo(strFilePath)
            strInputFileFullPath = ioFileInfo.FullName

            xcaliburAccessor.LoadMSMethodInfo = mOptions.WriteMSMethodFile
            xcaliburAccessor.LoadMSTuneInfo = mOptions.WriteMSTuneFile

            ' Open a handle to the data file
            If Not xcaliburAccessor.OpenRawFile(strInputFileFullPath) Then
                ReportError("ExtractScanInfoFromXcaliburDataFile",
                            "Error opening input data file: " & strInputFileFullPath & " (xcaliburAccessor.OpenRawFile returned False)")
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            If xcaliburAccessor Is Nothing Then
                ReportError("ExtractScanInfoFromXcaliburDataFile",
                            "Error opening input data file: " & strInputFileFullPath & " (xcaliburAccessor is Nothing)")
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError)
                Return False
            End If

            Dim intDatasetID = mOptions.SICOptions.DatasetNumber
            Dim sicOptions = mOptions.SICOptions

            blnSuccess = UpdateDatasetFileStats(ioFileInfo, intDatasetID, xcaliburAccessor)

            If mOptions.WriteMSMethodFile Then
                SaveMSMethodFile(xcaliburAccessor, dataOutputHandler)
            End If

            If mOptionsWriteMSTuneFile Then
                SaveMSTuneFile(xcaliburAccessor, dataOutputHandler)
            End If

            intScanCount = xcaliburAccessor.GetNumScans()

            If intScanCount <= 0 Then
                ' No scans found
                ReportError("ExtractScanInfoFromXcaliburDataFile", "No scans found in the input file: " & strFilePath)
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.InputFileAccessError)
                Return False
            Else

                intScanStart = xcaliburAccessor.FileInfo.ScanStart
                intScanEnd = xcaliburAccessor.FileInfo.ScanEnd

                With sicOptions
                    If .ScanRangeStart > 0 And .ScanRangeEnd = 0 Then
                        .ScanRangeEnd = Integer.MaxValue
                    End If

                    If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                        intScanStart = Math.Max(intScanStart, .ScanRangeStart)
                        intScanEnd = Math.Min(intScanEnd, .ScanRangeEnd)
                    End If
                End With

                UpdateOverallProgress("Reading Xcalibur data with " & strIOMode & " (" & intScanCount.ToString & " scans)" & ControlChars.NewLine & Path.GetFileName(strFilePath))
                ReportMessage("Reading Xcalibur data with " & strIOMode & "; Total scan count: " & intScanCount.ToString)
                dtLastLogTime = DateTime.UtcNow

                ' Pre-reserve memory for the maximum number of scans that might be loaded
                ' Re-dimming after loading each scan is extremly slow and uses additional memory
                InitializeScanList(scanList, intScanEnd - intScanStart + 1, intScanEnd - intScanStart + 1)
                intLastNonZoomSurveyScanIndex = -1

                Dim htSIMScanMapping = New Dictionary(Of String, Integer)
                scanList.SIMDataPresent = False
                scanList.MRMDataPresent = False

                For intScanNumber = intScanStart To intScanEnd

                    Dim scanInfo As ThermoRawFileReader.clsScanInfo = Nothing

                    blnSuccess = xcaliburAccessor.GetScanInfo(intScanNumber, scanInfo)

                    If Not blnSuccess Then
                        ' GetScanInfo returned false
                        ReportMessage("xcaliburAccessor.GetScanInfo returned false for scan " & intScanNumber.ToString & "; aborting read", eMessageTypeConstants.Warning)
                        Exit For
                    End If

                    If CheckScanInRange(intScanNumber, scanInfo.RetentionTime, sicOptions) Then

                        If scanInfo.ParentIonMZ > 0 AndAlso Math.Abs(mParentIonDecoyMassDa) > 0 Then
                            scanInfo.ParentIonMZ += mParentIonDecoyMassDa
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
                            ReportMessage("Reading scan: " & intScanNumber.ToString)
                            Console.Write(".")
                            dtLastLogTime = DateTime.UtcNow
                        End If

                        ' Call the garbage collector every 100 spectra
                        GC.Collect()
                        GC.WaitForPendingFinalizers()
                        Threading.Thread.Sleep(50)
                    End If

                Next intScanNumber
                Console.WriteLine()

                ' Shrink the memory usage of the scanList arrays
                With scanList
                    ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)
                End With

            End If

        Catch ex As Exception
            Console.WriteLine(ex.StackTrace)
            ReportError("ExtractScanInfoFromXcaliburDataFile",
                        "Error in ExtractScanInfoFromXcaliburDataFile", ex, True, True, clsMASIC.eMasicErrorCodes.InputFileDataReadError)
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
      dataOutputHandler As clsDataOutput,
      sicOptions As clsSICOptions,
      blnKeepRawSpectra As Boolean,
      scanInfo As ThermoRawFileReader.clsScanInfo,
      htSIMScanMapping As Dictionary(Of String, Integer),
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
        StoreExtendedHeaderInfo(newSurveyScan, scanInfo.ScanEvents)

        ' Store the collision mode and possibly the scan filter text
        newSurveyScan.FragScanInfo.CollisionMode = scanInfo.CollisionMode
        StoreExtendedHeaderInfo(newSurveyScan, EXTENDED_STATS_HEADER_COLLISION_MODE, scanInfo.CollisionMode)
        If mOptions.WriteExtendedStatsIncludeScanFilterText Then
            StoreExtendedHeaderInfo(newSurveyScan, EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanInfo.FilterText)
        End If

        If mOptions.WriteExtendedStatsStatusLog Then
            ' Store the StatusLog values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(newSurveyScan, scanInfo.StatusLog, mStatusLogKeyNameFilterList)
        End If

        scanList.SurveyScans.Add(newSurveyScan)

        If Not newSurveyScan.ZoomScan Then
            intLastNonZoomSurveyScanIndex = scanList.SurveyScans.Count - 1
        End If

        AddMasterScanEntry(scanList, clsScanList.eScanTypeConstants.SurveyScan, scanList.SurveyScans.Count - 1)

        Dim dblMSDataResolution As Double

        If sicOptions.SICToleranceIsPPM Then
            ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
            ' However, if the lowest m/z value is < 100, then use 100 m/z
            If scanInfo.LowMass < 100 Then
                dblMSDataResolution = GetParentIonToleranceDa(sicOptions, 100) / sicOptions.CompressToleranceDivisorForPPM
            Else
                dblMSDataResolution = GetParentIonToleranceDa(sicOptions, scanInfo.LowMass) / sicOptions.CompressToleranceDivisorForPPM
            End If
        Else
            dblMSDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
        End If

        ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
        Dim blnSuccess = LoadSpectraForFinniganDataFile(xcaliburAccessor, objSpectraCache, intScanNumber, newSurveyScan, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD, sicOptions.CompressMSSpectraData, dblMSDataResolution, blnKeepRawSpectra)
        If Not blnSuccess Then Return False

        SaveScanStatEntry(outputFileHandles.ScanStats, clsScanList.eScanTypeConstants.SurveyScan, newSurveyScan, sicOptions.DatasetNumber)

        Return True

    End Function

    Private Function ExtractXcaliburFragmentationScan(
      xcaliburAccessor As XRawFileIO,
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      dataOutputHandler As clsDataOutput,
      sicOptions As clsSICOptions,
      binningOptions As clsBinningOptions,
      blnKeepRawSpectra As Boolean,
      blnKeepMSMSSpectra As Boolean,
      scanInfo As ThermoRawFileReader.clsScanInfo,
      ByRef intLastNonZoomSurveyScanIndex As Integer,
      intScanNumber As Integer) As Boolean

        Dim newFragScan = New clsScanInfo()

        With newFragScan
            .ScanNumber = intScanNumber
            .ScanTime = CSng(scanInfo.RetentionTime)

            .ScanHeaderText = XRawFileIO.MakeGenericFinniganScanFilter(scanInfo.FilterText)
            .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.FilterText)

            .BasePeakIonMZ = scanInfo.BasePeakMZ
            .BasePeakIonIntensity = Math.Min(CSng(scanInfo.BasePeakIntensity), Single.MaxValue)

            .FragScanInfo.FragScanNumber = scanInfo.EventNumber - 1                                      ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.

            ' The .EventNumber value is sometimes wrong; need to check for this
            If scanList.FragScans.Count > 0 Then
                Dim prevFragScan = scanList.FragScans(scanList.FragScans.Count - 1)
                If prevFragScan.ScanNumber = .ScanNumber - 1 Then
                    If .FragScanInfo.FragScanNumber <= prevFragScan.FragScanInfo.FragScanNumber Then
                        .FragScanInfo.FragScanNumber = prevFragScan.FragScanInfo.FragScanNumber + 1
                    End If
                End If
            End If

            .FragScanInfo.MSLevel = scanInfo.MSLevel

            .TotalIonIntensity = Math.Min(CSng(scanInfo.TotalIonCurrent), Single.MaxValue)

            ' This will be determined in LoadSpectraForFinniganDataFile
            .MinimumPositiveIntensity = 0

            .ZoomScan = scanInfo.ZoomScan
            .SIMScan = scanInfo.SIMScan
            .MRMScanType = scanInfo.MRMScanType
        End With

        If Not newFragScan.MRMScanType = MRMScanTypeConstants.NotMRM Then
            ' This is an MRM scan
            scanList.MRMDataPresent = True

            newFragScan.MRMScanInfo = DuplicateMRMInfo(scanInfo.MRMInfo, scanInfo.ParentIonMZ)

            If scanList.SurveyScans.Count = 0 Then
                ' Need to add a "fake" survey scan that we can map this parent ion to
                intLastNonZoomSurveyScanIndex = AddFakeSurveyScan(scanList)
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
        StoreExtendedHeaderInfo(newFragScan, scanInfo.ScanEvents)

        ' Store the collision mode and possibly the scan filter text
        newFragScan.FragScanInfo.CollisionMode = scanInfo.CollisionMode
        StoreExtendedHeaderInfo(newFragScan, EXTENDED_STATS_HEADER_COLLISION_MODE, scanInfo.CollisionMode)
        If mOptions.WriteExtendedStatsIncludeScanFilterText Then
            StoreExtendedHeaderInfo(newFragScan, EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, scanInfo.FilterText)
        End If

        If mOptions.WriteExtendedStatsStatusLog Then
            ' Store the StatusLog values in .ExtendedHeaderInfo
            StoreExtendedHeaderInfo(newFragScan, scanInfo.StatusLog, mStatusLogKeyNameFilterList)
        End If

        scanList.FragScans.Add(newFragScan)

        AddMasterScanEntry(scanList, clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1)

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


        Dim intIonIndex As Integer

        Dim dblTIC As Double

        Dim objMSSpectrum As New clsMSSpectrum()
        Dim dblIntensityList() As Double = Nothing

        Dim blnDiscardLowIntensityDataWork As Boolean
        Dim blnCompressSpectraDataWork As Boolean

        Dim strLastKnownLocation = "Start"

        Try

            ' Load the ions for this scan

            strLastKnownLocation = "objXcaliburAccessor.GetScanData for scan " & intScanNumber

            ' Start a new thread to load the data, in case MSFileReader encounters a corrupt scan

            objMSSpectrum.IonCount = objXcaliburAccessor.GetScanData(intScanNumber, objMSSpectrum.IonsMZ, dblIntensityList)

            scanInfo.IonCount = objMSSpectrum.IonCount
            scanInfo.IonCountRaw = scanInfo.IonCount

            If objMSSpectrum.IonCount > 0 Then
                If objMSSpectrum.IonCount <> objMSSpectrum.IonsMZ.Length Then
                    If objMSSpectrum.IonCount = 0 Then
                        Debug.WriteLine("LoadSpectraForFinniganDataFile: Survey Scan has IonCount = 0 -- Scan " & intScanNumber, "LoadSpectraForFinniganDataFile")
                    Else
                        Debug.WriteLine("LoadSpectraForFinniganDataFile: Survey Scan found where IonCount <> dblMZList.Length -- Scan " & intScanNumber, "LoadSpectraForFinniganDataFile")
                    End If
                End If

                Dim sortRequired = False

                For intIndex = 1 To objMSSpectrum.IonCount - 1
                    ' Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z, 
                    ' we have observed a few cases in certain scans of certain datasets that points with 
                    ' similar m/z values are swapped and ths slightly out of order
                    ' The following if statement checks for this
                    If (objMSSpectrum.IonsMZ(intIndex) < objMSSpectrum.IonsMZ(intIndex - 1)) Then
                        sortRequired = True
                        Exit For
                    End If
                Next intIndex

                If sortRequired Then
                    Array.Sort(objMSSpectrum.IonsMZ, dblIntensityList)
                End If

            Else
                objMSSpectrum.IonCount = 0
            End If

            With objMSSpectrum
                .ScanNumber = intScanNumber

                strLastKnownLocation = "Redim .IonsIntensity(" & .IonCount.ToString & " - 1)"
                ReDim .IonsIntensity(.IonCount - 1)

                ' Copy the intensity data; and compute the total scan intensity
                dblTIC = 0
                For intIonIndex = 0 To .IonCount - 1
                    .IonsIntensity(intIonIndex) = CSng(dblIntensityList(intIonIndex))
                    dblTIC += dblIntensityList(intIonIndex)
                Next intIonIndex
            End With

            ' Determine the minimum positive intensity in this scan
            strLastKnownLocation = "Call mMASICPeakFinder.FindMinimumPositiveValue"
            scanInfo.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

            If objMSSpectrum.IonCount > 0 Then
                If scanInfo.TotalIonIntensity < Single.Epsilon Then
                    scanInfo.TotalIonIntensity = CSng(Math.Min(dblTIC, Single.MaxValue))
                End If

                If scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM Then
                    blnDiscardLowIntensityDataWork = blnDiscardLowIntensityData
                    blnCompressSpectraDataWork = blnCompressSpectraData
                Else
                    blnDiscardLowIntensityDataWork = False
                    blnCompressSpectraDataWork = False
                End If

                strLastKnownLocation = "Call ProcessAndStoreSpectrum"
                mScanTracking.ProcessAndStoreSpectrum(scanInfo, objSpectraCache, objMSSpectrum, noiseThresholdOptions, blnDiscardLowIntensityDataWork, blnCompressSpectraDataWork, dblMSDataResolution, blnKeepRawSpectrum)
            Else
                scanInfo.TotalIonIntensity = 0
            End If

        Catch ex As Exception
            ReportError("LoadSpectraForFinniganDataFile", "Error in LoadSpectraForFinniganDataFile (LastKnownLocation: " & strLastKnownLocation & ")", ex, True, True, clsMASIC.eMasicErrorCodes.InputFileDataReadError)
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
      scanInfo As clsScanInfo,
      strEntryName As String,
      strEntryValue As String)


        If strEntryValue Is Nothing Then
            strEntryValue = String.Empty
        End If

        Dim statusEntries = New List(Of KeyValuePair(Of String, String))
        statusEntries.Add(New KeyValuePair(Of String, String)(strEntryName, strEntryValue))

        StoreExtendedHeaderInfo(scanInfo, statusEntries)

    End Sub

    Private Sub StoreExtendedHeaderInfo(
      scanInfo As clsScanInfo,
      statusEntries As List(Of KeyValuePair(Of String, String)))

        StoreExtendedHeaderInfo(scanInfo, statusEntries, New String() {})
    End Sub

    Private Sub StoreExtendedHeaderInfo(
      scanInfo As clsScanInfo,
      statusEntries As List(Of KeyValuePair(Of String, String)),
      ByRef strKeyNameFilterList() As String)

        Dim intIndex As Integer
        Dim intIDValue As Integer
        Dim intFilterIndex As Integer

        Dim blnFilterItems As Boolean
        Dim blnSaveItem As Boolean

        Try
            If (statusEntries Is Nothing) Then Exit Sub

            If Not strKeyNameFilterList Is Nothing AndAlso strKeyNameFilterList.Length > 0 Then
                For intIndex = 0 To strKeyNameFilterList.Length - 1
                    If strKeyNameFilterList(intIndex).Length > 0 Then
                        blnFilterItems = True
                        Exit For
                    End If
                Next
            End If

            For Each statusEntry In statusEntries
                If String.IsNullOrWhiteSpace(statusEntry.Key) Then
                    ' Empty entry name; do not add
                    Continue For
                End If

                If blnFilterItems Then
                    blnSaveItem = False
                    For intFilterIndex = 0 To strKeyNameFilterList.Length - 1
                        If statusEntry.Key.ToLower().Contains(strKeyNameFilterList(intFilterIndex).ToLower()) Then
                            blnSaveItem = True
                            Exit For
                        End If
                    Next intFilterIndex
                Else
                    blnSaveItem = True
                End If

                If blnSaveItem Then
                    If TryGetExtendedHeaderInfoValue(statusEntry.Key, intIDValue) Then
                        ' Match found
                    Else
                        intIDValue = mExtendedHeaderInfo.Count
                        mExtendedHeaderInfo.Add(New KeyValuePair(Of String, Integer)(statusEntry.Key, intIDValue))
                    End If

                    ' Add or update the value for intIDValue
                    scanInfo.ExtendedHeaderInfo(intIDValue) = statusEntry.Value

                End If

            Next

        Catch ex As Exception
            ' Ignore any errors here
        End Try

    End Sub

    Private Function TryGetExtendedHeaderInfoValue(keyName As String, <Out()> ByRef headerIndex As Integer) As Boolean

        Dim query = (From item In mExtendedHeaderInfo Where item.Key = keyName Select item.Value).ToList()
        headerIndex = 0

        If query.Count = 0 Then
            Return False
        End If

        headerIndex = query.First()
        Return True

    End Function

    Private Sub mXcaliburAccessor_ReportError(strMessage As String)
        Console.WriteLine(strMessage)
        ReportError("XcaliburAccessor", strMessage, Nothing, True, False, clsMASIC.eMasicErrorCodes.InputFileDataReadError)
    End Sub

    Private Sub mXcaliburAccessor_ReportWarning(strMessage As String)
        Console.WriteLine(strMessage)
        ReportError("XcaliburAccessor", strMessage, Nothing, False, False, clsMASIC.eMasicErrorCodes.InputFileDataReadError)
    End Sub

End Class
