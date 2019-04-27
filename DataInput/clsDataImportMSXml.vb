Imports MASIC.clsMASIC
Imports MASIC.DataOutput
Imports MSDataFileReader
Imports PSI_Interface
Imports ThermoRawFileReader

Namespace DataInput

    ''' <summary>
    ''' Import data from .mzXML, .mzData, or .mzML files
    ''' </summary>
    Public Class clsDataImportMSXml
        Inherits clsDataImport

#Region "Member variables"
        Private mKeepRawSpectra As Boolean
        Private mKeepMSMSSpectra As Boolean

        Private mLastSurveyScanIndex As Integer
        Private mLastSurveyScanIndexInMasterSeqOrder As Integer
        Private mLastNonZoomSurveyScanIndex As Integer

        Private mScansOutOfRange As Integer
        Private mWarnCount As Integer

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
          peakFinder As MASICPeakFinder.clsMASICPeakFinder,
          parentIonProcessor As clsParentIonProcessing,
          scanTracking As clsScanTracking)
            MyBase.New(masicOptions, peakFinder, parentIonProcessor, scanTracking)
        End Sub

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

        Private Function CSngSafe(value As Double) As Single
            If value > Single.MaxValue Then
                Return Single.MaxValue
            ElseIf value < Single.MinValue Then
                Return Single.MinValue
            Else
                Return CSng(value)
            End If
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

                While True
                    Dim spectrumInfo As clsSpectrumInfo = Nothing
                    Dim scanFound = xmlReader.ReadNextSpectrum(spectrumInfo)

                    If Not scanFound Then Exit While

                    mDatasetFileInfo.ScanCount += 1

                    Dim msSpectrum = GetNewSpectrum(spectrumInfo.DataCount)

                    spectrumInfo.MZList.CopyTo(msSpectrum.IonsMZ, 0)
                    spectrumInfo.IntensityList.CopyTo(msSpectrum.IonsIntensity, 0)

                    Dim percentComplete = xmlReader.ProgressPercentComplete
                    Dim extractSuccess = ExtractScanInfoCheckRange(scanList, msSpectrum, spectrumInfo, spectraCache, dataOutputHandler, percentComplete)

                    If Not extractSuccess Then
                        Exit While
                    End If

                End While

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

            Try
                Console.Write("Reading MSXml data file ")
                ReportMessage("Reading MSXml data file")

                UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & mzMLFile.Name)

                Dim datasetID = mOptions.SICOptions.DatasetNumber

                Dim fileStatsSuccess = UpdateDatasetFileStats(mzMLFile, datasetID)
                If Not fileStatsSuccess Then
                    Return False
                End If

                mDatasetFileInfo.ScanCount = 0

                ' Open a handle to the data file
                Dim xmlReader = New MSData.SimpleMzMLReader(mzMLFile.FullName, False)
                fileOpened = True

                InitOptions(scanList, keepRawSpectra, keepMSMSSpectra)

                UpdateProgress("Reading XML data" & ControlChars.NewLine & mzMLFile.Name)
                ReportMessage("Reading XML data from " & mzMLFile.FullName)

                Dim iterator = xmlReader.ReadAllSpectra(True).GetEnumerator()

                While iterator.MoveNext()

                    Dim mzMLSpectrum = iterator.Current

                    mDatasetFileInfo.ScanCount += 1

                    Dim spectrumInfo = GetSpectrumInfoFromMzMLSpectrum(mzMLSpectrum)

                    Dim msSpectrum = GetNewSpectrum(spectrumInfo.DataCount)

                    mzMLSpectrum.Mzs.CopyTo(msSpectrum.IonsMZ, 0)
                    For i = 0 To msSpectrum.IonCount - 1
                        msSpectrum.IonsIntensity(i) = CSngSafe(mzMLSpectrum.Intensities(i))
                    Next

                    Dim percentComplete = scanList.MasterScanOrderCount / CDbl(xmlReader.NumSpectra) * 100
                    Dim extractSuccess = ExtractScanInfoCheckRange(scanList, msSpectrum, spectrumInfo, spectraCache, dataOutputHandler, percentComplete)

                    If Not extractSuccess Then
                        Exit While
                    End If

                End While

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
          scanList As clsScanList,
          msSpectrum As clsMSSpectrum,
          spectrumInfo As clsSpectrumInfo,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          percentComplete As Double) As Boolean

            ' No Error
            If mScanTracking.CheckScanInRange(spectrumInfo.ScanNumber, spectrumInfo.RetentionTimeMin, mOptions.SICOptions) Then
                ExtractScanInfoWork(scanList, spectraCache, dataOutputHandler,
                                    mOptions.SICOptions, msSpectrum, spectrumInfo)
            Else
                mScansOutOfRange += 1
            End If

            UpdateProgress(CShort(Math.Round(percentComplete, 0)))

            UpdateCacheStats(spectraCache)

            If mOptions.AbortProcessing Then
                scanList.ProcessingIncomplete = True
                Return False
            End If

            If (scanList.MasterScanOrderCount - 1) Mod 100 = 0 Then
                ReportMessage("Reading scan index: " & (scanList.MasterScanOrderCount - 1).ToString())
                Console.Write(".")
            End If

            Return True

        End Function

        Private Sub ExtractScanInfoWork(
          scanList As clsScanList,
          spectraCache As clsSpectraCache,
          dataOutputHandler As clsDataOutput,
          sicOptions As clsSICOptions,
          msSpectrum As clsMSSpectrum,
          spectrumInfo As clsSpectrumInfo)

            Dim isMzXML As Boolean
            Dim eMRMScanType As MRMScanTypeConstants

            ' ReSharper disable once NotAccessedVariable
            Dim msDataResolution As Double

            Dim mzXmlSourceSpectrum As clsSpectrumInfoMzXML = Nothing

            If TypeOf (spectrumInfo) Is clsSpectrumInfoMzXML Then
                mzXmlSourceSpectrum = CType(spectrumInfo, clsSpectrumInfoMzXML)
                isMzXML = True
            Else
                isMzXML = False
            End If

            ' Determine if this was an MS/MS scan
            ' If yes, determine the scan number of the survey scan
            If spectrumInfo.MSLevel <= 1 Then
                ' Survey Scan

                Dim newSurveyScan = New clsScanInfo()
                With newSurveyScan
                    .ScanNumber = spectrumInfo.ScanNumber
                    .ScanTime = spectrumInfo.RetentionTimeMin

                    ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                    .ScanHeaderText = String.Empty

                    ' This may get updated via the call to UpdateMSXmlScanType()
                    .ScanTypeName = "MS"

                    .BasePeakIonMZ = spectrumInfo.BasePeakMZ
                    .BasePeakIonIntensity = spectrumInfo.BasePeakIntensity

                    ' Survey scans typically lead to multiple parent ions; we do not record them here
                    .FragScanInfo.ParentIonInfoIndex = -1
                    .TotalIonIntensity = CSngSafe(spectrumInfo.TotalIonCurrent)

                    ' Determine the minimum positive intensity in this scan
                    .MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonCount, msSpectrum.IonsIntensity, 0)

                    ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                    .ZoomScan = False
                    .SIMScan = False
                    .MRMScanType = MRMScanTypeConstants.NotMRM

                    .LowMass = spectrumInfo.mzRangeStart
                    .HighMass = spectrumInfo.mzRangeEnd
                    .IsFTMS = False

                End With

                scanList.SurveyScans.Add(newSurveyScan)

                UpdateMSXmlScanType(newSurveyScan, spectrumInfo.MSLevel, "MS", isMzXML, mzXmlSourceSpectrum)

                mLastSurveyScanIndex = scanList.SurveyScans.Count - 1

                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, mLastSurveyScanIndex)
                mLastSurveyScanIndexInMasterSeqOrder = scanList.MasterScanOrderCount - 1

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
                StoreMzXmlSpectrum(
                    msSpectrum,
                    newSurveyScan,
                    spectraCache,
                    sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                    DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                    sicOptions.CompressMSSpectraData,
                    sicOptions.SimilarIonMZToleranceHalfWidth / sicOptions.CompressToleranceDivisorForDa,
                    mKeepRawSpectra)

                SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats,
                                  clsScanList.eScanTypeConstants.SurveyScan, newSurveyScan, sicOptions.DatasetNumber)

            Else
                ' Fragmentation Scan

                Dim newFragScan = New clsScanInfo()
                With newFragScan
                    .ScanNumber = spectrumInfo.ScanNumber
                    .ScanTime = spectrumInfo.RetentionTimeMin

                    ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                    .ScanHeaderText = String.Empty

                    ' This may get updated via the call to UpdateMSXmlScanType()
                    .ScanTypeName = "MSn"

                    .BasePeakIonMZ = spectrumInfo.BasePeakMZ
                    .BasePeakIonIntensity = spectrumInfo.BasePeakIntensity

                    ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
                    .FragScanInfo.FragScanNumber = (scanList.MasterScanOrderCount - 1) - mLastSurveyScanIndexInMasterSeqOrder
                    .FragScanInfo.MSLevel = spectrumInfo.MSLevel

                    .TotalIonIntensity = CSngSafe(spectrumInfo.TotalIonCurrent)

                    ' Determine the minimum positive intensity in this scan
                    .MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonCount, msSpectrum.IonsIntensity, 0)

                    ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                    .ZoomScan = False
                    .SIMScan = False
                    .MRMScanType = MRMScanTypeConstants.NotMRM

                    .MRMScanInfo.MRMMassCount = 0

                End With

                UpdateMSXmlScanType(newFragScan, spectrumInfo.MSLevel, "MSn", isMzXML, mzXmlSourceSpectrum)

                eMRMScanType = newFragScan.MRMScanType
                If Not eMRMScanType = MRMScanTypeConstants.NotMRM Then
                    ' This is an MRM scan
                    scanList.MRMDataPresent = True

                    Dim scanInfo = New ThermoRawFileReader.clsScanInfo(spectrumInfo.SpectrumID)

                    With scanInfo
                        .FilterText = newFragScan.ScanHeaderText
                        .MRMScanType = eMRMScanType
                        .MRMInfo = New MRMInfo()

                        If Not String.IsNullOrEmpty(.FilterText) Then
                            ' Parse out the MRM_QMS or SRM information for this scan
                            XRawFileIO.ExtractMRMMasses(.FilterText, .MRMScanType, .MRMInfo)
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

                            Dim mRMMassRange As udtMRMMassRangeType
                            mRMMassRange = New udtMRMMassRangeType()
                            With mRMMassRange
                                .StartMass = spectrumInfo.mzRangeStart
                                .EndMass = spectrumInfo.mzRangeEnd
                                .CentralMass = Math.Round(.StartMass + (.EndMass - .StartMass) / 2, 6)
                            End With
                            .MRMInfo.MRMMassList.Add(mRMMassRange)

                        End If
                    End With

                    newFragScan.MRMScanInfo = clsMRMProcessing.DuplicateMRMInfo(scanInfo.MRMInfo, spectrumInfo.ParentIonMZ)

                    If scanList.SurveyScans.Count = 0 Then
                        ' Need to add a "fake" survey scan that we can map this parent ion to
                        mLastNonZoomSurveyScanIndex = scanList.AddFakeSurveyScan()
                    End If
                Else
                    newFragScan.MRMScanInfo.MRMMassCount = 0
                End If

                With newFragScan
                    .LowMass = spectrumInfo.mzRangeStart
                    .HighMass = spectrumInfo.mzRangeEnd
                    .IsFTMS = False
                End With

                scanList.FragScans.Add(newFragScan)

                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1)

                ' Note: Even if keepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
                StoreMzXmlSpectrum(
                    msSpectrum,
                    newFragScan,
                    spectraCache,
                    sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                    DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                    sicOptions.CompressMSMSSpectraData,
                    mOptions.BinningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa,
                    mKeepRawSpectra AndAlso mKeepMSMSSpectra)

                SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats,
                                  clsScanList.eScanTypeConstants.FragScan, newFragScan, sicOptions.DatasetNumber)

                If eMRMScanType = MRMScanTypeConstants.NotMRM Then
                    ' This is not an MRM scan
                    mParentIonProcessor.AddUpdateParentIons(scanList, mLastSurveyScanIndex, spectrumInfo.ParentIonMZ,
                                                            scanList.FragScans.Count - 1, spectraCache, sicOptions)
                Else
                    ' This is an MRM scan
                    mParentIonProcessor.AddUpdateParentIons(scanList, mLastNonZoomSurveyScanIndex, spectrumInfo.ParentIonMZ,
                                                            newFragScan.MRMScanInfo, spectraCache, sicOptions)
                End If

            End If
        End Sub

        Private Function FinalizeScanList(scanList As clsScanList, dataFile As FileSystemInfo) As Boolean

            ReDim Preserve scanList.MasterScanOrder(scanList.MasterScanOrderCount - 1)
            ReDim Preserve scanList.MasterScanNumList(scanList.MasterScanOrderCount - 1)
            ReDim Preserve scanList.MasterScanTimeList(scanList.MasterScanOrderCount - 1)

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

            Console.WriteLine()

            ' Record the current memory usage (before we close the .mzML or .mzML file)
            OnUpdateMemoryUsage()

            Return True

        End Function

        Private Function GetNewSpectrum(dataCount As Integer) As clsMSSpectrum

            Dim msSpectrum = New clsMSSpectrum()
            msSpectrum.IonCount = dataCount

            ReDim msSpectrum.IonsMZ(msSpectrum.IonCount - 1)
            ReDim msSpectrum.IonsIntensity(msSpectrum.IonCount - 1)

            Return msSpectrum

        End Function

        Private Function GetSpectrumInfoFromMzMLSpectrum(mzMLSpectrum As MSData.SimpleMzMLReader.SimpleSpectrum) As clsSpectrumInfoMzXML

            Dim spectrumInfo = New clsSpectrumInfoMzXML With {
                .SpectrumID = mzMLSpectrum.ScanNumber,
                .ScanNumber = mzMLSpectrum.ScanNumber,
                .RetentionTimeMin = CSngSafe(mzMLSpectrum.ElutionTime),
                .MSLevel = mzMLSpectrum.MsLevel,
                .TotalIonCurrent = mzMLSpectrum.TotalIonCurrent
            }

            spectrumInfo.DataCount = mzMLSpectrum.Mzs.Length

            If spectrumInfo.DataCount > 0 Then
                Dim basePeakMz = mzMLSpectrum.Mzs(0)
                Dim bpi = mzMLSpectrum.Intensities(0)
                Dim mzMin = basePeakMz
                Dim mzMax = basePeakMz

                For i = 0 To spectrumInfo.DataCount - 1
                    If mzMLSpectrum.Intensities(i) > bpi Then
                        basePeakMz = mzMLSpectrum.Mzs(i)
                        bpi = mzMLSpectrum.Intensities(i)
                    End If

                    If mzMLSpectrum.Mzs(i) < mzMin Then
                        mzMin = mzMLSpectrum.Mzs(i)
                    ElseIf mzMLSpectrum.Mzs(i) > mzMax Then
                        mzMax = mzMLSpectrum.Mzs(i)
                    End If
                Next

                spectrumInfo.BasePeakMZ = basePeakMz
                spectrumInfo.BasePeakIntensity = CSngSafe(bpi)

                spectrumInfo.mzRangeStart = CSngSafe(mzMin)
                spectrumInfo.mzRangeEnd = CSngSafe(mzMax)
            End If

            If spectrumInfo.MSLevel > 1 Then
                Dim productSpectrum = CType(mzMLSpectrum, MSData.SimpleMzMLReader.SimpleProductSpectrum)
                spectrumInfo.ParentIonMZ = productSpectrum.IsolationWindowTargetMz
                spectrumInfo.ActivationMethod = productSpectrum.ActivationMethod
            End If

            ' TODO: Store the "filter string" in .FilterLine
            ' spectrumInfo.FilterLine = mzMLSpectrum.GetCvParam("MS:1000512")

            ' If filter string is defined, ScanType can be empty
            spectrumInfo.ScanType = String.Empty

            Return spectrumInfo
        End Function

        Private Sub InitOptions(scanList As clsScanList,
                                keepRawSpectra As Boolean,
                                keepMSMSSpectra As Boolean)

            ' We won't know the total scan count until we have read all the data
            ' Thus, initially reserve space for 1000 scans

            scanList.Initialize(1000, 1000)
            mLastSurveyScanIndex = -1
            mLastSurveyScanIndexInMasterSeqOrder = -1
            mLastNonZoomSurveyScanIndex = -1

            mScansOutOfRange = 0

            scanList.SIMDataPresent = False
            scanList.MRMDataPresent = False

            mKeepRawSpectra = keepRawSpectra
            mKeepMSMSSpectra = keepMSMSSpectra

            mWarnCount = 0
        End Sub

        Private Sub StoreMzXmlSpectrum(
          objMSSpectrum As clsMSSpectrum,
          scanInfo As clsScanInfo,
          spectraCache As clsSpectraCache,
          noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
          discardLowIntensityData As Boolean,
          compressSpectraData As Boolean,
          msDataResolution As Double,
          keepRawSpectrum As Boolean)

            Try

                If objMSSpectrum.IonsMZ Is Nothing OrElse objMSSpectrum.IonsIntensity Is Nothing Then
                    scanInfo.IonCount = 0
                    scanInfo.IonCountRaw = 0
                Else
                    objMSSpectrum.IonCount = objMSSpectrum.IonsMZ.Length

                    scanInfo.IonCount = objMSSpectrum.IonCount
                    scanInfo.IonCountRaw = scanInfo.IonCount
                End If

                objMSSpectrum.ScanNumber = scanInfo.ScanNumber

                If scanInfo.IonCount > 0 Then
                    With scanInfo
                        ' Confirm the total scan intensity stored in the mzXML file
                        Dim totalIonIntensity As Single = 0
                        For ionIndex = 0 To objMSSpectrum.IonCount - 1
                            totalIonIntensity += objMSSpectrum.IonsIntensity(ionIndex)
                        Next

                        If .TotalIonIntensity < Single.Epsilon Then
                            .TotalIonIntensity = totalIonIntensity
                        End If

                    End With

                    mScanTracking.ProcessAndStoreSpectrum(
                        scanInfo, Me,
                        spectraCache, objMSSpectrum,
                        noiseThresholdOptions,
                        discardLowIntensityData,
                        compressSpectraData,
                        msDataResolution,
                        keepRawSpectrum)
                Else
                    scanInfo.TotalIonIntensity = 0
                End If

            Catch ex As Exception
                ReportError("Error in clsMasic->StoreMzXMLSpectrum ", ex)
            End Try

        End Sub

        Private Sub UpdateMSXmlScanType(
          scanInfo As clsScanInfo,
          msLevel As Integer,
          defaultScanType As String,
          isMzXML As Boolean,
          ByRef mzXmlSourceSpectrum As clsSpectrumInfoMzXML)

            If Not isMzXML Then
                ' Not a .mzXML file
                ' Use the defaults
                scanInfo.ScanHeaderText = String.Empty
                scanInfo.ScanTypeName = defaultScanType
                Return

            End If

            ' Store the filter line text in .ScanHeaderText
            ' Only Thermo files processed with ReadW will have a FilterLine
            scanInfo.ScanHeaderText = mzXmlSourceSpectrum.FilterLine

            If Not String.IsNullOrEmpty(scanInfo.ScanHeaderText) Then
                ' This is a Thermo file; auto define .ScanTypeName using the FilterLine text
                scanInfo.ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.ScanHeaderText)

                With scanInfo
                    ' Now populate .SIMScan, .MRMScanType and .ZoomScan
                    Dim msLevelFromFilter As Integer
                    XRawFileIO.ValidateMSScan(.ScanHeaderText, msLevelFromFilter, .SIMScan, .MRMScanType, .ZoomScan)
                End With
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