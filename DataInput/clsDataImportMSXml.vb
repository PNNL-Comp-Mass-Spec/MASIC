Imports MASIC.clsMASIC
Imports MASIC.DataOutput
Imports MASICPeakFinder
Imports MSDataFileReader
Imports PSI_Interface.CV
Imports PSI_Interface.MSData
Imports SpectraTypeClassifier
Imports ThermoRawFileReader

Namespace DataInput

    ''' <summary>
    ''' Import data from .mzXML, .mzData, or .mzML files
    ''' </summary>
    Public Class clsDataImportMSXml
        Inherits clsDataImport

#Region "Member variables"

        Private ReadOnly mCentroider As Centroider
        Private mWarnCount As Integer

        Private mMostRecentPrecursorScan As Integer

        Private ReadOnly mCentroidedPrecursorIonsMz As List(Of Double) = New List(Of Double)
        Private ReadOnly mCentroidedPrecursorIonsIntensity As List(Of Double) = New List(Of Double)

#End Region
        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="masicOptions"></param>
        ''' <param name="peakFinder"></param>
        ''' <param name="parentIonProcessor"></param>
        ''' <param name="scanTracking"></param>
        Public Sub New(
          masicOptions As clsMASICOptions,
          peakFinder As clsMASICPeakFinder,
          parentIonProcessor As clsParentIonProcessing,
          scanTracking As clsScanTracking)
            MyBase.New(masicOptions, peakFinder, parentIonProcessor, scanTracking)
            mCentroider = New Centroider()
        End Sub

        Private Function ComputeInterference(
          mzMLSpectrum As SimpleMzMLReader.SimpleSpectrum,
          scanInfo As clsScanInfo,
          precursorScanNumber As Integer) As Double

            If mzMLSpectrum Is Nothing Then
                Return 0
            End If

            If precursorScanNumber <> mCachedPrecursorScan Then

                If mMostRecentPrecursorScan <> precursorScanNumber Then
                    ReportWarning(String.Format(
                        "Most recent precursor scan is {0}, and not {1}; cannot compute interference for scan {2}",
                        mMostRecentPrecursorScan, precursorScanNumber, scanInfo.ScanNumber))
                    Return 0
                End If

                UpdateCachedPrecursorScan(mMostRecentPrecursorScan, mCentroidedPrecursorIonsMz, mCentroidedPrecursorIonsIntensity)
            End If

            Dim isolationWidth As Double = 0

            Dim chargeState = 0
            Dim chargeStateText = String.Empty

            ' This is only used if scanInfo.FragScanInfo.ParentIonMz is zero
            Dim monoMzText = String.Empty

            Dim isolationWidthText = String.Empty

            Dim isolationWindowTargetMzText = String.Empty
            Dim isolationWindowLowerOffsetText = String.Empty
            Dim isolationWindowUpperOffsetText = String.Empty

            If mzMLSpectrum.Precursors.Count > 0 AndAlso Not mzMLSpectrum.Precursors(0).IsolationWindow Is Nothing Then
                For Each cvParam In mzMLSpectrum.Precursors(0).IsolationWindow.CVParams
                    Select Case cvParam.TermInfo.Cvid
                        Case CV.CVID.MS_isolation_width_OBSOLETE
                            isolationWidthText = cvParam.Value

                        Case CV.CVID.MS_isolation_window_target_m_z
                            isolationWindowTargetMzText = cvParam.Value
                        Case CV.CVID.MS_isolation_window_lower_offset
                            isolationWindowLowerOffsetText = cvParam.Value
                        Case CV.CVID.MS_isolation_window_upper_offset
                            isolationWindowUpperOffsetText = cvParam.Value

                    End Select
                Next
            End If

            If mzMLSpectrum.Precursors.Count > 0 AndAlso
               Not mzMLSpectrum.Precursors(0).SelectedIons Is Nothing AndAlso
               mzMLSpectrum.Precursors(0).SelectedIons.Count > 0 Then

                For Each cvParam In mzMLSpectrum.Precursors(0).SelectedIons(0).CVParams
                    Select Case cvParam.TermInfo.Cvid
                        Case CV.CVID.MS_selected_ion_m_z,
                             CV.CVID.MS_selected_precursor_m_z
                            monoMzText = cvParam.Value

                        Case CV.CVID.MS_charge_state
                            chargeStateText = cvParam.Value

                    End Select
                Next

            End If

            If Not String.IsNullOrWhiteSpace(chargeStateText) Then
                If Not Integer.TryParse(chargeStateText, chargeState) Then
                    chargeState = 0
                End If
            End If

            Dim isolationWidthDefined = False

            If Not String.IsNullOrWhiteSpace(isolationWidthText) Then
                If Double.TryParse(isolationWidthText, isolationWidth) Then
                    isolationWidthDefined = True
                End If
            End If

            If Not isolationWidthDefined AndAlso Not String.IsNullOrWhiteSpace(isolationWindowTargetMzText) Then
                Dim isolationWindowTargetMz As Double
                Dim isolationWindowLowerOffset As Double
                Dim isolationWindowUpperOffset As Double

                If Double.TryParse(isolationWindowTargetMzText, isolationWindowTargetMz) AndAlso
                   Double.TryParse(isolationWindowLowerOffsetText, isolationWindowLowerOffset) AndAlso
                   Double.TryParse(isolationWindowUpperOffsetText, isolationWindowUpperOffset) Then
                    isolationWidth = isolationWindowLowerOffset + isolationWindowUpperOffset
                    isolationWidthDefined = True
                Else

                    WarnIsolationWidthNotFound(
                        scanInfo.ScanNumber,
                        String.Format("Could not determine the MS2 isolation width; unable to parse {0}",
                                      isolationWindowLowerOffsetText))
                End If
            Else
                WarnIsolationWidthNotFound(
                    scanInfo.ScanNumber,
                    String.Format("Could not determine the MS2 isolation width (CVParam '{0}' not found)",
                                  "isolation window target m/z"))
            End If

            If Not isolationWidthDefined Then
                Return 0
            End If

            Dim parentIonMz As Double

            If Math.Abs(scanInfo.FragScanInfo.ParentIonMz) > 0 Then
                parentIonMz = scanInfo.FragScanInfo.ParentIonMz
            Else
                ' The mzML reader could not determine the parent ion m/z value (this is highly unlikely)
                ' Use scan event "Monoisotopic M/Z" instead

                If String.IsNullOrWhiteSpace(monoMzText) Then

                    ReportWarning("Could not determine the parent ion m/z value via CV param 'selected ion m/z'" &
                                  "cannot compute interference for scan " & scanInfo.ScanNumber)
                    Return 0
                End If

                Dim mz As Double
                If Not Double.TryParse(monoMzText, mz) Then

                    OnWarningEvent(String.Format("Skipping scan {0} since 'selected ion m/z' was not a number:  {1}",
                                                 scanInfo.ScanNumber, monoMzText))
                    Return 0
                End If

                parentIonMz = mz
            End If

            If Math.Abs(parentIonMz) < Single.Epsilon Then
                ReportWarning("Parent ion m/z is 0; cannot compute interference for scan " & scanInfo.ScanNumber)
                Return 0
            End If

            Dim precursorInterference = ComputePrecursorInterference(
                scanInfo.ScanNumber,
                precursorScanNumber, parentIonMz, isolationWidth, chargeState)

            Return precursorInterference

        End Function

        Public Function ExtractScanInfoFromMzMLDataFile(
          filePath As String,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean

            Try
                Dim msXmlFileInfo = New FileInfo(filePath)

                Return ExtractScanInfoFromMzMLDataFile(msXmlFileInfo, scanList, spectraCache,
                                                       dataOutputHandler, keepRawSpectra, keepMSMSSpectra)

            Catch ex As Exception
                ReportError("Error in ExtractScanInfoFromMzMLDataFile", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Public Function ExtractScanInfoFromMzXMLDataFile(
          filePath As String,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean

            Dim xmlReader As clsMSDataFileReaderBaseClass

            Try
                xmlReader = New clsMzXMLFileReader()
                Return ExtractScanInfoFromMSXMLDataFile(filePath, xmlReader, scanList, spectraCache,
                                                        dataOutputHandler, keepRawSpectra, keepMSMSSpectra)

            Catch ex As Exception
                ReportError("Error in ExtractScanInfoFromMzXMLDataFile", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Public Function ExtractScanInfoFromMzDataFile(
          filePath As String,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean

            Dim xmlReader As clsMSDataFileReaderBaseClass

            Try
                xmlReader = New clsMzDataFileReader()
                Return ExtractScanInfoFromMSXMLDataFile(filePath, xmlReader, scanList, spectraCache,
                                                        dataOutputHandler,
                                                        keepRawSpectra, keepMSMSSpectra)

            Catch ex As Exception
                ReportError("Error in ExtractScanInfoFromMzDataFile", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Private Function ExtractScanInfoFromMSXMLDataFile(
          filePath As String,
          xmlReader As clsMSDataFileReaderBaseClass,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean

            ' Returns True if Success, False if failure
            ' Note: This function assumes filePath exists

            Dim success As Boolean

            Try
                Console.Write("Reading MSXml data file ")
                ReportMessage("Reading MSXml data file")

                UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(filePath))

                ' Obtain the full path to the file
                Dim msXmlFileInfo = New FileInfo(filePath)
                Dim inputFileFullPath = msXmlFileInfo.FullName

                Dim datasetID = mOptions.SICOptions.DatasetNumber

                Dim fileStatsSuccess = UpdateDatasetFileStats(msXmlFileInfo, datasetID)
                If Not fileStatsSuccess Then
                    Return False
                End If

                mDatasetFileInfo.ScanCount = 0

                ' Open a handle to the data file
                If Not xmlReader.OpenFile(inputFileFullPath) Then
                    ReportError("Error opening input data file: " & inputFileFullPath)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                InitOptions(scanList, keepRawSpectra, keepMSMSSpectra)

                UpdateProgress("Reading XML data" & ControlChars.NewLine & Path.GetFileName(filePath))
                ReportMessage("Reading XML data from " & filePath)

                Dim scanTimeMax As Double = 0

                While True
                    Dim spectrumInfo As clsSpectrumInfo = Nothing
                    Dim scanFound = xmlReader.ReadNextSpectrum(spectrumInfo)

                    If Not scanFound Then Exit While

                    mDatasetFileInfo.ScanCount += 1
                    scanTimeMax = spectrumInfo.RetentionTimeMin

                    Dim msSpectrum = New clsMSSpectrum(spectrumInfo.ScanNumber, spectrumInfo.MZList, spectrumInfo.IntensityList, spectrumInfo.DataCount)

                    Dim percentComplete = xmlReader.ProgressPercentComplete
                    Dim nullMzMLSpectrum As SimpleMzMLReader.SimpleSpectrum = Nothing

                    Dim extractSuccess = ExtractScanInfoCheckRange(msSpectrum, spectrumInfo, nullMzMLSpectrum,
                                                                   scanList, spectraCache, dataOutputHandler,
                                                                   percentComplete, mDatasetFileInfo.ScanCount)

                    If Not extractSuccess Then
                        Exit While
                    End If

                End While

                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanTimeMax)

                ' Shrink the memory usage of the scanList arrays
                success = FinalizeScanList(scanList, msXmlFileInfo)

            Catch ex As Exception
                ReportError("Error in ExtractScanInfoFromMSXMLDataFile", ex, eMasicErrorCodes.InputFileDataReadError)
                success = False
            End Try

            ' Close the handle to the data file
            If Not xmlReader Is Nothing Then
                Try
                    xmlReader.CloseFile()
                Catch ex As Exception
                    ' Ignore errors here
                End Try
            End If

            Return success

        End Function

        Private Function ExtractScanInfoFromMzMLDataFile(
          mzMLFile As FileInfo,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean


            Dim fileOpened = False
            Dim medianUtils = New clsMedianUtilities()

            Try
                Console.Write("Reading MSXml data file ")
                ReportMessage("Reading MSXml data file")

                UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & mzMLFile.Name)

                Dim datasetID = mOptions.SICOptions.DatasetNumber

                If Not mzMLFile.Exists Then
                    Return False
                End If

                mDatasetFileInfo.ScanCount = 0

                ' Open a handle to the data file
                Dim xmlReader = New SimpleMzMLReader(mzMLFile.FullName, False)
                fileOpened = True

                Dim fileStatsSuccess = UpdateDatasetFileStats(mzMLFile, datasetID, xmlReader)
                If Not fileStatsSuccess Then
                    Return False
                End If

                InitOptions(scanList, keepRawSpectra, keepMSMSSpectra)

                Dim thermoRawFile = False

                For Each cvParam In xmlReader.SourceFileParams.CVParams
                    Select Case cvParam.TermInfo.Cvid
                        Case CV.CVID.MS_Thermo_nativeID_format,
                             CV.CVID.MS_Thermo_nativeID_format__combined_spectra,
                             CV.CVID.MS_Thermo_RAW_format
                            thermoRawFile = True
                    End Select
                Next

                UpdateProgress("Reading XML data" & ControlChars.NewLine & mzMLFile.Name)
                ReportMessage("Reading XML data from " & mzMLFile.FullName)

                Dim scanTimeMax As Double = 0

                If xmlReader.NumSpectra > 0 Then
                    Dim iterator = xmlReader.ReadAllSpectra(True).GetEnumerator()

                    While iterator.MoveNext()

                        Dim mzMLSpectrum = iterator.Current

                        mDatasetFileInfo.ScanCount += 1

                        Dim mzList = mzMLSpectrum.Mzs.ToList()
                        Dim intensityList = mzMLSpectrum.Intensities.ToList()

                        Dim mzXmlSourceSpectrum = GetSpectrumInfoFromMzMLSpectrum(mzMLSpectrum, mzList, intensityList, thermoRawFile)
                        scanTimeMax = mzXmlSourceSpectrum.RetentionTimeMin

                        Dim msSpectrum = New clsMSSpectrum(mzXmlSourceSpectrum.ScanNumber, mzList, intensityList, mzList.Count)

                        Dim percentComplete = scanList.MasterScanOrderCount / CDbl(xmlReader.NumSpectra) * 100

                        Dim extractSuccess = ExtractScanInfoCheckRange(msSpectrum, mzXmlSourceSpectrum, mzMLSpectrum,
                                                                       scanList, spectraCache, dataOutputHandler,
                                                                       percentComplete, mDatasetFileInfo.ScanCount)

                        If Not extractSuccess Then
                            Exit While
                        End If

                    End While

                    mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanTimeMax)

                ElseIf xmlReader.NumSpectra = 0 AndAlso xmlReader.NumChromatograms > 0 Then
                    Dim iterator1 = xmlReader.ReadAllChromatograms(True).GetEnumerator()

                    ' Construct a list of the difference in time (in minutes) between adjacent data points in each chromatogram
                    Dim scanTimeDiffMedians = New List(Of Double)

                    ' Also keep track of elution times
                    ' Keys in this dictionary are elution times; values are the pseudo scan number mapped to each time (initially 0)
                    Dim elutionTimeToScanMap = New Dictionary(Of Double, Integer)

                    While iterator1.MoveNext()

                        Dim chromatogramItem = iterator1.Current

                        Dim isSRM As Boolean = IsSrmChromatogram(chromatogramItem)
                        If Not isSRM Then Continue While

                        ' Construct a list of the difference in time (in minutes) between adjacent data points in each chromatogram
                        Dim scanTimeDiffs = New List(Of Double)

                        Dim scanTimes = chromatogramItem.Times().ToList()

                        For i = 0 To scanTimes.Count - 1
                            If Not elutionTimeToScanMap.ContainsKey(scanTimes(i)) Then
                                elutionTimeToScanMap.Add(scanTimes(i), 0)
                            End If

                            If i > 0 Then
                                Dim adjacentTimeDiff = scanTimes(i) - scanTimes(i - 1)
                                If adjacentTimeDiff > 0 Then
                                    scanTimeDiffs.Add(adjacentTimeDiff)
                                End If
                            End If
                        Next

                        ' First, compute the median time diff in scanTimeDiffs
                        Dim medianScanTimeDiffThisChromatogram = medianUtils.Median(scanTimeDiffs)

                        scanTimeDiffMedians.Add(medianScanTimeDiffThisChromatogram)

                    End While

                    ' Construct a mapping between elution time and scan number
                    ' This is a bit of a challenge since chromatogram data only tracks elution time, and not scan number

                    ' First, compute the overall median time diff
                    Dim medianScanTimeDiff = medianUtils.Median(scanTimeDiffMedians)
                    If Math.Abs(medianScanTimeDiff) < 0.000001 Then
                        medianScanTimeDiff = 0.000001
                    End If

                    ' Populate a dictionary mapping elution time to scan number
                    For Each elutionTime In elutionTimeToScanMap.Keys.ToList()
                        Dim nearestPseudoScan = CInt(Math.Floor(elutionTime / medianScanTimeDiff)) + 1
                        elutionTimeToScanMap(elutionTime) = nearestPseudoScan
                    Next

                    ' Keys in this dictionary are scan numbers
                    ' Values are a dictionary tracking m/z values and intensities
                    Dim simulatedSpectraByScan = New Dictionary(Of Integer, Dictionary(Of Double, Double))

                    Dim xmlReader2 = New SimpleMzMLReader(mzMLFile.FullName, False)
                    Dim iterator2 = xmlReader2.ReadAllChromatograms(True).GetEnumerator()
                    While iterator2.MoveNext()

                        Dim chromatogramItem = iterator2.Current

                        Dim isSRM As Boolean = IsSrmChromatogram(chromatogramItem)
                        If Not isSRM Then Continue While

                        Dim scanTimes = chromatogramItem.Times().ToList()
                        Dim intensities = chromatogramItem.Intensities().ToList()

                        Dim scanToStore As Integer

                        For i = 0 To scanTimes.Count - 1
                            If elutionTimeToScanMap.TryGetValue(scanTimes(i), scanToStore) Then
                                ' Keys in this dictionary are m/z; values are intensity for the m/z
                                Dim mzListForScan As Dictionary(Of Double, Double) = Nothing
                                If Not simulatedSpectraByScan.TryGetValue(scanToStore, mzListForScan) Then
                                    mzListForScan = New Dictionary(Of Double, Double)
                                End If

                                ' mzListForScan.Add(currentMz, intensities(i))
                            End If
                        Next

                    End While


                    ' ToDo: Implement this
                    'Dim spectrumInfo = GetSpectrumInfoFromMzMLSpectrum(mzMLSpectrum)

                    'Dim msSpectrum = GetNewSpectrum(spectrumInfo.DataCount)

                    'mzList.CopyTo(msSpectrum.IonsMZ, 0)
                    'intensityList.CopyTo(msSpectrum.IonsIntensity, 0)


                    If xmlReader.NumChromatograms > 0 Then
                        Dim percentComplete = scanList.MasterScanOrderCount / CDbl(xmlReader.NumChromatograms) * 100
                    End If

                    'Dim extractSuccess = ExtractScanInfoCheckRange(msSpectrum, spectrumInfo, nullMzMLSpectrum,
                    '                                               scanList, spectraCache, dataOutputHandler,
                    '                                               percentComplete, mDatasetFileInfo.ScanCount)


                    'If Not extractSuccess Then
                    '    Exit While
                    'End If

                    'mDatasetFileInfo.ScanCount += 1
                    mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanTimeMax)
                End If

                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart.AddMinutes(scanTimeMax)

                ' Shrink the memory usage of the scanList arrays
                Dim finalizeSuccess = FinalizeScanList(scanList, mzMLFile)

                Return finalizeSuccess

            Catch ex As Exception
                If Not fileOpened Then
                    ReportError("Error opening input data file: " & mzMLFile.FullName)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                ReportError("Error in ExtractScanInfoFromMzMLDataFile", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Private Function ExtractScanInfoCheckRange(
          msSpectrum As clsMSSpectrum,
          spectrumInfo As clsSpectrumInfo,
          mzMLSpectrum As SimpleMzMLReader.SimpleSpectrum,
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          percentComplete As Double,
          scansRead As Integer) As Boolean

            Dim success As Boolean

            If mScanTracking.CheckScanInRange(spectrumInfo.ScanNumber, spectrumInfo.RetentionTimeMin, mOptions.SICOptions) Then
                success = ExtractScanInfoWork(scanList, spectraCache, dataOutputHandler,
                                              mOptions.SICOptions, msSpectrum, spectrumInfo, mzMLSpectrum)
            Else
                mScansOutOfRange += 1
                success = True
            End If

            UpdateProgress(CShort(Math.Round(percentComplete, 0)))

            UpdateCacheStats(spectraCache)

            If mOptions.AbortProcessing Then
                scanList.ProcessingIncomplete = True
                Return False
            End If

            If DateTime.UtcNow.Subtract(mLastLogTime).TotalSeconds >= 10 OrElse scansRead Mod 500 = 0 Then
                ReportMessage("Reading scan: " & scansRead.ToString())
                Console.Write(".")
                mLastLogTime = DateTime.UtcNow
            End If

            Return success

        End Function

        Private Function ExtractScanInfoWork(
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          sicOptions As clsSICOptions,
          msSpectrum As clsMSSpectrum,
          spectrumInfo As clsSpectrumInfo,
          mzMLSpectrum As SimpleMzMLReader.SimpleSpectrum) As Boolean

            Dim isMzXML As Boolean

            Dim mzXmlSourceSpectrum As clsSpectrumInfoMzXML = Nothing

            ' Note that both mzXML and mzML data is stored in spectrumInfo
            If TypeOf (spectrumInfo) Is clsSpectrumInfoMzXML Then
                mzXmlSourceSpectrum = CType(spectrumInfo, clsSpectrumInfoMzXML)
                isMzXML = True
            Else
                isMzXML = False
            End If

            Dim success As Boolean

            ' Determine if this was an MS/MS scan
            ' If yes, determine the scan number of the survey scan
            If spectrumInfo.MSLevel <= 1 Then
                ' Survey Scan

                success = ExtractSurveyScan(scanList, spectraCache, dataOutputHandler,
                                            spectrumInfo, msSpectrum, sicOptions,
                                            isMzXML, mzXmlSourceSpectrum)

            Else
                ' Fragmentation Scan
                success = ExtractFragmentationScan(scanList, spectraCache, dataOutputHandler,
                                                   spectrumInfo, msSpectrum, sicOptions,
                                                   isMzXML, mzXmlSourceSpectrum, mzMLSpectrum)

            End If

            Return success
        End Function

        ''' <summary>
        '''
        ''' </summary>
        ''' <param name="scanList"></param>
        ''' <param name="spectraCache"></param>
        ''' <param name="dataOutputHandler"></param>
        ''' <param name="spectrumInfo"></param>
        ''' <param name="msSpectrum">Tracks scan number, m/z values, and intensity values; msSpectrum.IonCount is the number of data points</param>
        ''' <param name="sicOptions"></param>
        ''' <param name="isMzXML"></param>
        ''' <param name="mzXmlSourceSpectrum"></param>
        ''' <returns></returns>
        Private Function ExtractSurveyScan(
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          spectrumInfo As clsSpectrumInfo,
          msSpectrum As clsMSSpectrum,
          sicOptions As clsSICOptions,
          isMzXML As Boolean,
          mzXmlSourceSpectrum As clsSpectrumInfoMzXML) As Boolean

            Dim scanInfo = New clsScanInfo() With {
                .ScanNumber = spectrumInfo.ScanNumber,
                .ScanTime = spectrumInfo.RetentionTimeMin,
                .ScanHeaderText = String.Empty,
                .ScanTypeName = "MS",    ' This may get updated via the call to UpdateMSXmlScanType()
                .BasePeakIonMZ = spectrumInfo.BasePeakMZ,
                .BasePeakIonIntensity = spectrumInfo.BasePeakIntensity,
                .TotalIonIntensity = spectrumInfo.TotalIonCurrent,
                .MinimumPositiveIntensity = 0,
                .ZoomScan = False,
                .SIMScan = False,
                .MRMScanType = MRMScanTypeConstants.NotMRM,
                .LowMass = spectrumInfo.mzRangeStart,
                .HighMass = spectrumInfo.mzRangeEnd
            }

            If Not mzXmlSourceSpectrum Is Nothing AndAlso Not String.IsNullOrWhiteSpace(mzXmlSourceSpectrum.FilterLine) Then
                scanInfo.IsFTMS = IsHighResolutionSpectrum(mzXmlSourceSpectrum.FilterLine)
            End If

            ' Survey scans typically lead to multiple parent ions; we do not record them here
            scanInfo.FragScanInfo.ParentIonInfoIndex = -1

            ' Determine the minimum positive intensity in this scan
            scanInfo.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonsIntensity, 0)

            scanList.SurveyScans.Add(scanInfo)

            UpdateMSXmlScanType(scanInfo, spectrumInfo.MSLevel, "MS", isMzXML, mzXmlSourceSpectrum)

            If Not scanInfo.ZoomScan Then
                mLastNonZoomSurveyScanIndex = scanList.SurveyScans.Count - 1
            End If

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, scanList.SurveyScans.Count - 1)
            mLastSurveyScanIndexInMasterSeqOrder = scanList.MasterScanOrderCount - 1

            ' ReSharper disable once NotAccessedVariable
            Dim msDataResolution As Double

            If mOptions.SICOptions.SICToleranceIsPPM Then
                ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
                ' However, if the lowest m/z value is < 100, then use 100 m/z
                If spectrumInfo.mzRangeStart < 100 Then
                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) / sicOptions.CompressToleranceDivisorForPPM
                Else
                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, spectrumInfo.mzRangeStart) / sicOptions.CompressToleranceDivisorForPPM
                End If
            Else
                msDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
            End If

            ' Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            StoreSpectrum(
                msSpectrum,
                scanInfo,
                spectraCache,
                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                sicOptions.CompressMSSpectraData,
                sicOptions.SimilarIonMZToleranceHalfWidth / sicOptions.CompressToleranceDivisorForDa,
                mKeepRawSpectra)

            If Not msSpectrum.IonsMZ Is Nothing AndAlso Not msSpectrum.IonsIntensity Is Nothing Then

                If mzXmlSourceSpectrum.Centroided Then
                    ' Data is already centroided
                    UpdateCachedPrecursorScanData(scanInfo.ScanNumber, msSpectrum)
                Else
                    ' Need to centroid the data
                    Dim sourceMzs As Double()
                    Dim sourceIntensities As Double()

                    ReDim sourceMzs(msSpectrum.IonCount - 1)
                    ReDim sourceIntensities(msSpectrum.IonCount - 1)

                    For i = 0 To msSpectrum.IonCount - 1
                        sourceMzs(i) = msSpectrum.IonsMZ(i)
                        sourceIntensities(i) = msSpectrum.IonsIntensity(i)
                    Next

                    Dim centroidedPrecursorIonsMz As Double() = Nothing
                    Dim centroidedPrecursorIonsIntensity As Double() = Nothing

                    Dim massResolution = mCentroider.EstimateResolution(1000, 0.5, scanInfo.IsFTMS)

                    Dim centroidSuccess = mCentroider.CentroidData(scanInfo, sourceMzs, sourceIntensities,
                                                                   massResolution, centroidedPrecursorIonsMz, centroidedPrecursorIonsIntensity)

                    If centroidSuccess Then
                        mMostRecentPrecursorScan = scanInfo.ScanNumber
                        mCentroidedPrecursorIonsMz.Clear()
                        mCentroidedPrecursorIonsIntensity.Clear()

                        For i = 0 To centroidedPrecursorIonsMz.Length - 1
                            mCentroidedPrecursorIonsMz.Add(centroidedPrecursorIonsMz(i))
                            mCentroidedPrecursorIonsIntensity.Add(centroidedPrecursorIonsIntensity(i))
                        Next

                    End If
                End If

            End If

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.SurveyScan, scanInfo, sicOptions.DatasetNumber)

            Return True

        End Function

        Private Function ExtractFragmentationScan(
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          spectrumInfo As clsSpectrumInfo,
          msSpectrum As clsMSSpectrum,
          sicOptions As clsSICOptions,
          isMzXML As Boolean,
          mzXmlSourceSpectrum As clsSpectrumInfoMzXML,
          mzMLSpectrum As SimpleMzMLReader.SimpleSpectrum) As Boolean

            Dim scanInfo = New clsScanInfo(spectrumInfo.ParentIonMZ) With {
                .ScanNumber = spectrumInfo.ScanNumber,
                .ScanTime = spectrumInfo.RetentionTimeMin,
                .ScanHeaderText = String.Empty,
                .ScanTypeName = "MSn",          ' This may get updated via the call to UpdateMSXmlScanType()
                .BasePeakIonMZ = spectrumInfo.BasePeakMZ,
                .BasePeakIonIntensity = spectrumInfo.BasePeakIntensity,
                .TotalIonIntensity = spectrumInfo.TotalIonCurrent,
                .MinimumPositiveIntensity = 0,
                .ZoomScan = False,
                .SIMScan = False,
                .MRMScanType = MRMScanTypeConstants.NotMRM
            }

            ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
            If mLastSurveyScanIndexInMasterSeqOrder < 0 Then
                ' We have not yet read a survey scan; store 1 for the fragmentation scan number
                scanInfo.FragScanInfo.FragScanNumber = 1
            Else
                scanInfo.FragScanInfo.FragScanNumber = (scanList.MasterScanOrderCount - 1) - mLastSurveyScanIndexInMasterSeqOrder
            End If

            scanInfo.FragScanInfo.MSLevel = spectrumInfo.MSLevel

            ' Determine the minimum positive intensity in this scan
            scanInfo.MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonsIntensity, 0)

            UpdateMSXmlScanType(scanInfo, spectrumInfo.MSLevel, "MSn", isMzXML, mzXmlSourceSpectrum)

            Dim eMRMScanType = scanInfo.MRMScanType
            If Not eMRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is an MRM scan
                scanList.MRMDataPresent = True

                Dim mrmScan = New ThermoRawFileReader.clsScanInfo(spectrumInfo.SpectrumID) With {
                    .FilterText = scanInfo.ScanHeaderText,
                    .MRMScanType = eMRMScanType,
                    .MRMInfo = New MRMInfo()
                }

                If Not String.IsNullOrEmpty(mrmScan.FilterText) Then
                    ' Parse out the MRM_QMS or SRM information for this scan
                    XRawFileIO.ExtractMRMMasses(mrmScan.FilterText, mrmScan.MRMScanType, mrmScan.MRMInfo)
                Else
                    ' .MZRangeStart and .MZRangeEnd should be equivalent, and they should define the m/z of the MRM transition

                    If spectrumInfo.mzRangeEnd - spectrumInfo.mzRangeStart >= 0.5 Then
                        ' The data is likely MRM and not SRM
                        ' We cannot currently handle data like this
                        ' (would need to examine the mass values  and find the clumps of data to infer the transitions present)
                        mWarnCount += 1
                        If mWarnCount <= 5 Then
                            ReportError("Warning: m/z range for SRM scan " & spectrumInfo.ScanNumber & " is " &
                                            (spectrumInfo.mzRangeEnd - spectrumInfo.mzRangeStart).ToString("0.0") &
                                            " m/z; this is likely a MRM scan, but MASIC doesn't support inferring the " &
                                            "MRM transition masses from the observed m/z values.  Results will likely not be meaningful")
                            If mWarnCount = 5 Then
                                ReportMessage("Additional m/z range warnings will not be shown")
                            End If
                        End If
                    End If

                    Dim mrmMassRange = New udtMRMMassRangeType With {
                            .StartMass = spectrumInfo.mzRangeStart,
                            .EndMass = spectrumInfo.mzRangeEnd
                        }
                    mrmMassRange.CentralMass = Math.Round(mrmMassRange.StartMass + (mrmMassRange.EndMass - mrmMassRange.StartMass) / 2, 6)
                    mrmScan.MRMInfo.MRMMassList.Add(mrmMassRange)

                End If

                scanInfo.MRMScanInfo = clsMRMProcessing.DuplicateMRMInfo(mrmScan.MRMInfo, spectrumInfo.ParentIonMZ)

                If scanList.SurveyScans.Count = 0 Then
                    ' Need to add a "fake" survey scan that we can map this parent ion to
                    mLastNonZoomSurveyScanIndex = scanList.AddFakeSurveyScan()
                End If
            Else
                scanInfo.MRMScanInfo.MRMMassCount = 0
            End If

            scanInfo.LowMass = spectrumInfo.mzRangeStart
            scanInfo.HighMass = spectrumInfo.mzRangeEnd
            scanInfo.IsFTMS = IsHighResolutionSpectrum(mzXmlSourceSpectrum.FilterLine)

            scanList.FragScans.Add(scanInfo)

            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1)

            ' Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
            StoreSpectrum(
                msSpectrum,
                scanInfo,
                spectraCache,
                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                sicOptions.CompressMSMSSpectraData,
                mOptions.BinningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa,
                mKeepRawSpectra AndAlso mKeepMSMSSpectra)

            SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, clsScanList.eScanTypeConstants.FragScan, scanInfo, sicOptions.DatasetNumber)

            If eMRMScanType = MRMScanTypeConstants.NotMRM Then
                ' This is not an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, mLastNonZoomSurveyScanIndex, spectrumInfo.ParentIonMZ,
                                                        scanList.FragScans.Count - 1, spectraCache, sicOptions)
            Else
                ' This is an MRM scan
                mParentIonProcessor.AddUpdateParentIons(scanList, mLastNonZoomSurveyScanIndex, spectrumInfo.ParentIonMZ,
                                                        scanInfo.MRMScanInfo, spectraCache, sicOptions)
            End If

            If mLastNonZoomSurveyScanIndex >= 0 Then
                Dim precursorScanNumber = scanList.SurveyScans(mLastNonZoomSurveyScanIndex).ScanNumber

                ' Compute the interference of the parent ion in the MS1 spectrum for this frag scan
                scanInfo.FragScanInfo.InterferenceScore = ComputeInterference(mzMLSpectrum, scanInfo, precursorScanNumber)
            End If

            Return True

        End Function

        Private Function IsHighResolutionSpectrum(filterString As String) As Boolean

            If filterString.IndexOf("FTMS", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If

            Return False
        End Function

        Private Function GetSpectrumInfoFromMzMLSpectrum(
          mzMLSpectrum As SimpleMzMLReader.SimpleSpectrum,
          mzList As IReadOnlyList(Of Double),
          intensityList As IReadOnlyList(Of Double),
          thermoRawFile As Boolean) As clsSpectrumInfoMzXML

            Dim mzXmlSourceSpectrum = New clsSpectrumInfoMzXML With {
                .SpectrumID = mzMLSpectrum.ScanNumber,
                .ScanNumber = mzMLSpectrum.ScanNumber,
                .RetentionTimeMin = clsUtilities.CSngSafe(mzMLSpectrum.ScanStartTime),
                .MSLevel = mzMLSpectrum.MsLevel,
                .TotalIonCurrent = mzMLSpectrum.TotalIonCurrent,
                .DataCount = mzList.Count
            }

            If mzXmlSourceSpectrum.DataCount > 0 Then
                Dim basePeakMz = mzList(0)
                Dim bpi = intensityList(0)
                Dim mzMin = basePeakMz
                Dim mzMax = basePeakMz

                For i = 1 To mzXmlSourceSpectrum.DataCount - 1
                    If intensityList(i) > bpi Then
                        basePeakMz = mzList(i)
                        bpi = intensityList(i)
                    End If

                    If mzList(i) < mzMin Then
                        mzMin = mzList(i)
                    ElseIf mzList(i) > mzMax Then
                        mzMax = mzList(i)
                    End If
                Next

                mzXmlSourceSpectrum.BasePeakMZ = basePeakMz
                mzXmlSourceSpectrum.BasePeakIntensity = clsUtilities.CSngSafe(bpi)

                mzXmlSourceSpectrum.mzRangeStart = clsUtilities.CSngSafe(mzMin)
                mzXmlSourceSpectrum.mzRangeEnd = clsUtilities.CSngSafe(mzMax)
            End If

            If mzXmlSourceSpectrum.MSLevel > 1 Then
                Dim firstPrecursor = mzMLSpectrum.Precursors(0)

                mzXmlSourceSpectrum.ParentIonMZ = firstPrecursor.IsolationWindow.TargetMz

                ' Verbose activation method description:
                ' Dim activationMethod = firstPrecursor.ActivationMethod

                Dim precursorParams = firstPrecursor.CVParams

                Dim activationMethods = New SortedSet(Of String)
                Dim supplementalMethods = New SortedSet(Of String)

                For Each cvParam In precursorParams
                    Select Case cvParam.TermInfo.Cvid
                        Case CV.CVID.MS_collision_induced_dissociation,
                             CV.CVID.MS_low_energy_collision_induced_dissociation,
                             CV.CVID.MS_in_source_collision_induced_dissociation,
                             CV.CVID.MS_trap_type_collision_induced_dissociation
                            activationMethods.Add("CID")

                        Case CV.CVID.MS_plasma_desorption
                            activationMethods.Add("PD")

                        Case CV.CVID.MS_post_source_decay
                            activationMethods.Add("PSD")

                        Case CV.CVID.MS_surface_induced_dissociation
                            activationMethods.Add("SID")

                        Case CV.CVID.MS_blackbody_infrared_radiative_dissociation
                            activationMethods.Add("BIRD")

                        Case CV.CVID.MS_electron_capture_dissociation
                            activationMethods.Add("ECD")

                        Case CV.CVID.MS_infrared_multiphoton_dissociation
                            ' ReSharper disable once StringLiteralTypo
                            activationMethods.Add("IRPD")

                        Case CV.CVID.MS_sustained_off_resonance_irradiation
                            activationMethods.Add("ORI")

                        Case CV.CVID.MS_beam_type_collision_induced_dissociation
                            activationMethods.Add("HCD")

                        Case CV.CVID.MS_photodissociation
                            ' ReSharper disable once StringLiteralTypo
                            activationMethods.Add("UVPD")

                        Case CV.CVID.MS_electron_transfer_dissociation
                            activationMethods.Add("ETD")

                        Case CV.CVID.MS_pulsed_q_dissociation
                            activationMethods.Add("PQD")

                        Case CV.CVID.MS_LIFT
                            activationMethods.Add("LIFT")

                        Case CV.CVID.MS_Electron_Transfer_Higher_Energy_Collision_Dissociation__EThcD_
                            activationMethods.Add("EThcD")

                        Case CV.CVID.MS_supplemental_beam_type_collision_induced_dissociation
                            supplementalMethods.Add("HCD")

                        Case CV.CVID.MS_supplemental_collision_induced_dissociation
                            supplementalMethods.Add("CID")

                    End Select

                Next cvParam

                If activationMethods.Contains("ETD") Then
                    If supplementalMethods.Contains("CID") Then
                        activationMethods.Remove("ETD")
                        activationMethods.Add("ETciD")
                    ElseIf supplementalMethods.Contains("HCD") Then
                        activationMethods.Remove("ETD")
                        activationMethods.Add("EThcD")
                    End If
                End If

                mzXmlSourceSpectrum.ActivationMethod = String.Join(","c, activationMethods)
            End If

            ' Store the "filter string" in .FilterLine

            Dim filterStrings = (From item In mzMLSpectrum.CVParams Where item.TermInfo.Cvid = CV.CVID.MS_filter_string).ToList()

            If filterStrings.Count > 0 Then
                Dim filterString = filterStrings.First().Value

                If thermoRawFile Then
                    mzXmlSourceSpectrum.FilterLine = XRawFileIO.MakeGenericFinniganScanFilter(filterString)
                    mzXmlSourceSpectrum.ScanType = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(filterString)
                Else
                    mzXmlSourceSpectrum.FilterLine = filterString
                End If
            End If

            If filterStrings.Count = 0 OrElse Not thermoRawFile Then
                Dim matchingParams = mzMLSpectrum.GetCVParamsChildOf(CV.CVID.MS_spectrum_type)
                If matchingParams.Count > 0 Then
                    mzXmlSourceSpectrum.ScanType = matchingParams.First().TermInfo.Name
                End If
            End If

            Dim centroidParams = (From item In mzMLSpectrum.CVParams Where item.TermInfo.Cvid = CV.CVID.MS_centroid_spectrum).ToList()
            mzXmlSourceSpectrum.Centroided = centroidParams.Count > 0

            Return mzXmlSourceSpectrum
        End Function

        Private Sub InitOptions(scanList As clsScanList,
                                keepRawSpectra As Boolean,
                                keepMSMSSpectra As Boolean)

            ' We won't know the total scan count until we have read all the data
            ' Thus, initially reserve space for 1000 scans

            scanList.Initialize(1000, 1000)

            InitBaseOptions(scanList, keepRawSpectra, keepMSMSSpectra)

            mLastSurveyScanIndexInMasterSeqOrder = -1

            mMostRecentPrecursorScan = -1

            mWarnCount = 0

        End Sub

        Private Function IsSrmChromatogram(chromatogramItem As SimpleMzMLReader.ParamData) As Boolean

            If chromatogramItem.CVParams.Count > 0 Then
                For Each item In chromatogramItem.CVParams
                    Select Case item.TermInfo.Cvid
                        Case CV.CVID.MS_total_ion_current_chromatogram
                            ' Skip this chromatogram
                            Return False
                        Case CV.CVID.MS_selected_ion_current_chromatogram,
                            CV.CVID.MS_selected_ion_monitoring_chromatogram,
                            CV.CVID.MS_selected_reaction_monitoring_chromatogram
                            Return True
                    End Select
                Next
            End If

            Return False
        End Function

        Private Sub StoreSpectrum(
          msSpectrum As clsMSSpectrum,
          scanInfo As clsScanInfo,
          spectraCache As clsSpectraCache,
          noiseThresholdOptions As clsBaselineNoiseOptions,
          discardLowIntensityData As Boolean,
          compressSpectraData As Boolean,
          msDataResolution As Double,
          keepRawSpectrum As Boolean)

            Try

                If msSpectrum.IonsMZ Is Nothing OrElse msSpectrum.IonsIntensity Is Nothing Then
                    scanInfo.IonCount = 0
                    scanInfo.IonCountRaw = 0
                Else
                    scanInfo.IonCount = msSpectrum.IonCount
                    scanInfo.IonCountRaw = scanInfo.IonCount
                End If

                If msSpectrum.ScanNumber <> scanInfo.ScanNumber Then
                    msSpectrum.ScanNumber = scanInfo.ScanNumber
                End If

                If scanInfo.IonCount > 0 Then
                    ' Confirm the total scan intensity stored in the mzXML file
                    Dim totalIonIntensity As Double = 0
                    For ionIndex = 0 To msSpectrum.IonCount - 1
                        totalIonIntensity += msSpectrum.IonsIntensity(ionIndex)
                    Next

                    If scanInfo.TotalIonIntensity < Single.Epsilon Then
                        scanInfo.TotalIonIntensity = totalIonIntensity
                    End If

                    mScanTracking.ProcessAndStoreSpectrum(
                        scanInfo, Me,
                        spectraCache, msSpectrum,
                        noiseThresholdOptions,
                        discardLowIntensityData,
                        compressSpectraData,
                        msDataResolution,
                        keepRawSpectrum)
                Else
                    scanInfo.TotalIonIntensity = 0
                End If

            Catch ex As Exception
                ReportError("Error in clsMasic->StoreSpectrum ", ex)
            End Try

        End Sub

        Private Sub UpdateCachedPrecursorScanData(scanNumber As Integer, msSpectrum As clsMSSpectrum)
            mMostRecentPrecursorScan = scanNumber
            mCentroidedPrecursorIonsMz.Clear()
            mCentroidedPrecursorIonsIntensity.Clear()

            For i = 0 To msSpectrum.IonCount - 1
                mCentroidedPrecursorIonsMz.Add(msSpectrum.IonsMZ(i))
                mCentroidedPrecursorIonsIntensity.Add(msSpectrum.IonsIntensity(i))
            Next

        End Sub

        <CLSCompliant(False)>
        Protected Overloads Function UpdateDatasetFileStats(
          rawFileInfo As FileInfo,
          datasetID As Integer,
          xmlReader As SimpleMzMLReader) As Boolean

            ' Read the file info from the file system
            Dim success = UpdateDatasetFileStats(rawFileInfo, datasetID)

            If Not success Then Return False

            If xmlReader.StartTimeStamp > DateTime.MinValue Then
                mDatasetFileInfo.AcqTimeStart = xmlReader.StartTimeStamp
                mDatasetFileInfo.AcqTimeEnd = mDatasetFileInfo.AcqTimeStart
            End If

            ' Note that .ScanCount and AcqTimeEnd will be updated by ExtractScanInfoFromMzMLDataFile

            Return True

        End Function

        Private Sub UpdateMSXmlScanType(
          scanInfo As clsScanInfo,
          msLevel As Integer,
          defaultScanType As String,
          isMzXML As Boolean,
          mzXmlSourceSpectrum As clsSpectrumInfoMzXML)

            If Not isMzXML Then
                ' Not a .mzXML file
                ' Use the defaults
                If String.IsNullOrWhiteSpace(scanInfo.ScanHeaderText) Then
                    If mzXmlSourceSpectrum Is Nothing OrElse String.IsNullOrWhiteSpace(mzXmlSourceSpectrum.FilterLine) Then
                        scanInfo.ScanHeaderText = String.Empty
                    Else
                        scanInfo.ScanHeaderText = mzXmlSourceSpectrum.FilterLine
                    End If
                End If

                scanInfo.ScanTypeName = defaultScanType
                Return
            End If

            ' Store the filter line text in .ScanHeaderText
            ' Only Thermo files processed with ReadW will have a FilterLine
            scanInfo.ScanHeaderText = mzXmlSourceSpectrum.FilterLine

            If Not String.IsNullOrEmpty(scanInfo.ScanHeaderText) Then
                ' This is a Thermo file; auto define .ScanTypeName using the FilterLine text
                scanInfo.ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.ScanHeaderText)

                ' Now populate .SIMScan, .MRMScanType and .ZoomScan
                Dim msLevelFromFilter As Integer
                Dim simScan As Boolean
                Dim mrmScanType As MRMScanTypeConstants
                Dim zoomScan As Boolean

                XRawFileIO.ValidateMSScan(scanInfo.ScanHeaderText, msLevelFromFilter, simScan, mrmScanType, zoomScan)

                scanInfo.SIMScan = simScan
                scanInfo.MRMScanType = mrmScanType
                scanInfo.ZoomScan = zoomScan

                Return

            End If

            scanInfo.ScanHeaderText = String.Empty
            scanInfo.ScanTypeName = mzXmlSourceSpectrum.ScanType

            If String.IsNullOrEmpty(scanInfo.ScanTypeName) Then
                scanInfo.ScanTypeName = defaultScanType
            Else
                ' Possibly update .ScanTypeName to match the values returned by XRawFileIO.GetScanTypeNameFromFinniganScanFilterText()
                Select Case scanInfo.ScanTypeName.ToLower()
                    Case clsSpectrumInfoMzXML.ScanTypeNames.Full.ToLower()
                        If msLevel <= 1 Then
                            scanInfo.ScanTypeName = "MS"
                        Else
                            scanInfo.ScanTypeName = "MSn"
                        End If

                    Case clsSpectrumInfoMzXML.ScanTypeNames.zoom.ToLower()
                        scanInfo.ScanTypeName = "Zoom-MS"

                    Case clsSpectrumInfoMzXML.ScanTypeNames.MRM.ToLower()
                        scanInfo.ScanTypeName = "MRM"
                        scanInfo.MRMScanType = MRMScanTypeConstants.SRM

                    Case clsSpectrumInfoMzXML.ScanTypeNames.SRM.ToLower()
                        scanInfo.ScanTypeName = "CID-SRM"
                        scanInfo.MRMScanType = MRMScanTypeConstants.SRM
                    Case Else
                        ' Leave .ScanTypeName unchanged
                End Select
            End If

            If Not String.IsNullOrWhiteSpace(mzXmlSourceSpectrum.ActivationMethod) Then
                ' Update ScanTypeName to include the activation method,
                ' For example, to be CID-MSn instead of simply MSn
                scanInfo.ScanTypeName = mzXmlSourceSpectrum.ActivationMethod & "-" & scanInfo.ScanTypeName

                If scanInfo.ScanTypeName = "HCD-MSn" Then
                    ' HCD spectra are always high res; auto-update things
                    scanInfo.ScanTypeName = "HCD-HMSn"
                End If

            End If

        End Sub

    End Class

End Namespace