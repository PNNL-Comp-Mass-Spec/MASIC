Imports MASIC.clsMASIC
Imports ThermoRawFileReader

Namespace DataInput

    Public Class clsDataImportMSXml
        Inherits clsDataImport

        Public Sub New(
          masicOptions As clsMASICOptions,
          peakFinder As MASICPeakFinder.clsMASICPeakFinder,
          parentIonProcessor As clsParentIonProcessing,
          scanTracking As clsScanTracking)
            MyBase.New(masicOptions, peakFinder, parentIonProcessor, scanTracking)
        End Sub

        Public Function ExtractScanInfoFromMZXMLDataFile(
          strFilePath As String,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          blnKeepRawSpectra As Boolean,
          blnKeepMSMSSpectra As Boolean) As Boolean

            Dim objXMLReader As MSDataFileReader.clsMSDataFileReaderBaseClass

            Try
                objXMLReader = New MSDataFileReader.clsMzXMLFileReader
                Return ExtractScanInfoFromMSXMLDataFile(strFilePath, objXMLReader, scanList, objSpectraCache,
                                                    dataOutputHandler,
                                                    mStatusMessage, blnKeepRawSpectra, blnKeepMSMSSpectra)

            Catch ex As Exception
                ReportError("ExtractScanInfoFromMZXMLDataFile", "Error in ExtractScanInfoFromMZXMLDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Public Function ExtractScanInfoFromMZDataFile(
          strFilePath As String,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          blnKeepRawSpectra As Boolean,
          blnKeepMSMSSpectra As Boolean) As Boolean

            Dim objXMLReader As MSDataFileReader.clsMSDataFileReaderBaseClass

            Try
                objXMLReader = New MSDataFileReader.clsMzDataFileReader
                Return ExtractScanInfoFromMSXMLDataFile(strFilePath, objXMLReader, scanList, objSpectraCache,
                                                    dataOutputHandler,
                                                    blnKeepRawSpectra, blnKeepMSMSSpectra)

            Catch ex As Exception
                ReportError("ExtractScanInfoFromMZDataFile", "Error in ExtractScanInfoFromMZDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Private Function ExtractScanInfoFromMSXMLDataFile(
          strFilePath As String,
          objXMLReader As MSDataFileReader.clsMSDataFileReaderBaseClass,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          blnKeepRawSpectra As Boolean,
          blnKeepMSMSSpectra As Boolean) As Boolean

            ' Returns True if Success, False if failure
            ' Note: This function assumes strFilePath exists

            Dim ioFileInfo As FileInfo
            Dim strInputFileFullPath As String

            Dim intLastSurveyScanIndex As Integer
            Dim intLastSurveyScanIndexInMasterSeqOrder As Integer
            Dim intLastNonZoomSurveyScanIndex As Integer
            Dim intWarnCount = 0

            Dim eMRMScanType As MRMScanTypeConstants

            Dim objSpectrumInfo As MSDataFileReader.clsSpectrumInfo = Nothing
            Dim objMZXmlSpectrumInfo As MSDataFileReader.clsSpectrumInfoMzXML = Nothing

            Dim objMSSpectrum As New clsMSSpectrum()

            ' ReSharper disable once NotAccessedVariable
            Dim dblMSDataResolution As Double

            Dim blnScanFound As Boolean
            Dim blnSuccess As Boolean
            Dim blnIsMzXML As Boolean

            Try
                Console.Write("Reading MSXml data file ")
                ReportMessage("Reading MSXml data file")

                UpdateProgress(0, "Opening data file:" & ControlChars.NewLine & Path.GetFileName(strFilePath))

                ' Obtain the full path to the file
                ioFileInfo = New FileInfo(strFilePath)
                strInputFileFullPath = ioFileInfo.FullName

                Dim intDatasetID = mOptions.SICOptions.DatasetNumber
                Dim sicOptions = mOptions.SICOptions

                blnSuccess = UpdateDatasetFileStats(ioFileInfo, intDatasetID)
                mDatasetFileInfo.ScanCount = 0

                ' Open a handle to the data file
                If Not objXMLReader.OpenFile(strInputFileFullPath) Then
                    ReportError("ExtractScanInfoFromMSXMLDataFile", "Error opening input data file: " & strInputFileFullPath)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                ' We won't know the total scan count until we have read all the data
                ' Thus, initially reserve space for 1000 scans

                scanList.Initialize(1000, 1000)
                intLastSurveyScanIndex = -1
                intLastSurveyScanIndexInMasterSeqOrder = -1
                intLastNonZoomSurveyScanIndex = -1

                scanList.SIMDataPresent = False
                scanList.MRMDataPresent = False

                UpdateProgress("Reading XML data" & ControlChars.NewLine & Path.GetFileName(strFilePath))
                ReportMessage("Reading XML data from " & strFilePath)

                Do
                    blnScanFound = objXMLReader.ReadNextSpectrum(objSpectrumInfo)

                    If Not blnScanFound Then Continue Do

                    mDatasetFileInfo.ScanCount += 1

                    With objMSSpectrum
                        .IonCount = objSpectrumInfo.DataCount

                        ReDim .IonsMZ(.IonCount - 1)
                        ReDim .IonsIntensity(.IonCount - 1)

                        objSpectrumInfo.MZList.CopyTo(.IonsMZ, 0)
                        objSpectrumInfo.IntensityList.CopyTo(.IonsIntensity, 0)
                    End With

                    ' No Error
                    If mScanTracking.CheckScanInRange(objSpectrumInfo.ScanNumber, objSpectrumInfo.RetentionTimeMin, mOptions.SICOptions) Then

                        If TypeOf (objSpectrumInfo) Is MSDataFileReader.clsSpectrumInfoMzXML Then
                            objMZXmlSpectrumInfo = CType(objSpectrumInfo, MSDataFileReader.clsSpectrumInfoMzXML)
                            blnIsMzXML = True
                        Else
                            blnIsMzXML = False
                        End If

                        ' Determine if this was an MS/MS scan
                        ' If yes, determine the scan number of the survey scan
                        If objSpectrumInfo.MSLevel <= 1 Then
                            ' Survey Scan

                            Dim newSurveyScan = New clsScanInfo()
                            With newSurveyScan
                                .ScanNumber = objSpectrumInfo.ScanNumber
                                .ScanTime = objSpectrumInfo.RetentionTimeMin

                                ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                                .ScanHeaderText = String.Empty
                                .ScanTypeName = "MS"                ' This may get updated via the call to UpdateMSXmlScanType()

                                .BasePeakIonMZ = objSpectrumInfo.BasePeakMZ
                                .BasePeakIonIntensity = objSpectrumInfo.BasePeakIntensity

                                .FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here
                                .TotalIonIntensity = CSng(Math.Min(objSpectrumInfo.TotalIonCurrent, Single.MaxValue))

                                ' Determine the minimum positive intensity in this scan
                                .MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

                                ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                                .ZoomScan = False
                                .SIMScan = False
                                .MRMScanType = MRMScanTypeConstants.NotMRM

                                .LowMass = objSpectrumInfo.mzRangeStart
                                .HighMass = objSpectrumInfo.mzRangeEnd
                                .IsFTMS = False

                            End With

                            scanList.SurveyScans.Add(newSurveyScan)

                            UpdateMSXmlScanType(newSurveyScan, objSpectrumInfo.MSLevel, "MS", blnIsMzXML, objMZXmlSpectrumInfo)

                            With scanList
                                intLastSurveyScanIndex = .SurveyScans.Count - 1

                                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)
                                intLastSurveyScanIndexInMasterSeqOrder = .MasterScanOrderCount - 1

                                If mOptions.SICOptions.SICToleranceIsPPM Then
                                    ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by sicOptions.CompressToleranceDivisorForPPM
                                    ' However, if the lowest m/z value is < 100, then use 100 m/z
                                    If objSpectrumInfo.mzRangeStart < 100 Then
                                        dblMSDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) / sicOptions.CompressToleranceDivisorForPPM
                                    Else
                                        dblMSDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, objSpectrumInfo.mzRangeStart) / sicOptions.CompressToleranceDivisorForPPM
                                    End If
                                Else
                                    dblMSDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
                                End If


                                ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
                                StoreMzXmlSpectrum(
                                objMSSpectrum,
                                newSurveyScan,
                                objSpectraCache,
                                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                                sicOptions.CompressMSSpectraData,
                                sicOptions.SimilarIonMZToleranceHalfWidth / sicOptions.CompressToleranceDivisorForDa,
                                blnKeepRawSpectra)

                                SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats,
                                              clsScanList.eScanTypeConstants.SurveyScan, newSurveyScan, sicOptions.DatasetNumber)

                            End With
                        Else
                            ' Fragmentation Scan

                            Dim newFragScan = New clsScanInfo()
                            With newFragScan
                                .ScanNumber = objSpectrumInfo.ScanNumber
                                .ScanTime = objSpectrumInfo.RetentionTimeMin

                                ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                                .ScanHeaderText = String.Empty
                                .ScanTypeName = "MSn"               ' This may get updated via the call to UpdateMSXmlScanType()

                                .BasePeakIonMZ = objSpectrumInfo.BasePeakMZ
                                .BasePeakIonIntensity = objSpectrumInfo.BasePeakIntensity

                                .FragScanInfo.FragScanNumber = (scanList.MasterScanOrderCount - 1) - intLastSurveyScanIndexInMasterSeqOrder      ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
                                .FragScanInfo.MSLevel = objSpectrumInfo.MSLevel

                                .TotalIonIntensity = CSng(Math.Min(objSpectrumInfo.TotalIonCurrent, Single.MaxValue))

                                ' Determine the minimum positive intensity in this scan
                                .MinimumPositiveIntensity = mPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount, objMSSpectrum.IonsIntensity, 0)

                                ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                                .ZoomScan = False
                                .SIMScan = False
                                .MRMScanType = MRMScanTypeConstants.NotMRM

                                .MRMScanInfo.MRMMassCount = 0

                            End With

                            UpdateMSXmlScanType(newFragScan, objSpectrumInfo.MSLevel, "MSn", blnIsMzXML, objMZXmlSpectrumInfo)

                            eMRMScanType = newFragScan.MRMScanType
                            If Not eMRMScanType = MRMScanTypeConstants.NotMRM Then
                                ' This is an MRM scan
                                scanList.MRMDataPresent = True

                                Dim scanInfo = New ThermoRawFileReader.clsScanInfo(objSpectrumInfo.SpectrumID)

                                With scanInfo
                                    .FilterText = newFragScan.ScanHeaderText
                                    .MRMScanType = eMRMScanType
                                    .MRMInfo = New MRMInfo()

                                    If Not String.IsNullOrEmpty(.FilterText) Then
                                        ' Parse out the MRM_QMS or SRM information for this scan
                                        XRawFileIO.ExtractMRMMasses(.FilterText, .MRMScanType, .MRMInfo)
                                    Else
                                        ' .MZRangeStart and .MZRangeEnd should be equivalent, and they should define the m/z of the MRM transition

                                        If objSpectrumInfo.mzRangeEnd - objSpectrumInfo.mzRangeStart >= 0.5 Then
                                            ' The data is likely MRM and not SRM
                                            ' We cannot currently handle data like this (would need to examine the mass values and find the clumps of data to infer the transitions present
                                            intWarnCount += 1
                                            If intWarnCount <= 5 Then
                                                ReportError("ExtractScanInfoFromMSXMLDataFile",
                                                        "Warning: m/z range for SRM scan " & objSpectrumInfo.ScanNumber & " is " &
                                                        (objSpectrumInfo.mzRangeEnd - objSpectrumInfo.mzRangeStart).ToString("0.0") &
                                                        " m/z; this is likely a MRM scan, but MASIC doesn't support inferring the " &
                                                        "MRM transition masses from the observed m/z values.  Results will likely not be meaningful")
                                                If intWarnCount = 5 Then
                                                    ReportMessage("Additional m/z range warnings will not be shown")
                                                End If
                                            End If
                                        End If

                                        Dim mRMMassRange As udtMRMMassRangeType
                                        mRMMassRange = New udtMRMMassRangeType()
                                        With mRMMassRange
                                            .StartMass = objSpectrumInfo.mzRangeStart
                                            .EndMass = objSpectrumInfo.mzRangeEnd
                                            .CentralMass = Math.Round(.StartMass + (.EndMass - .StartMass) / 2, 6)
                                        End With
                                        .MRMInfo.MRMMassList.Add(mRMMassRange)

                                    End If
                                End With

                                newFragScan.MRMScanInfo = clsMRMProcessing.DuplicateMRMInfo(scanInfo.MRMInfo, objSpectrumInfo.ParentIonMZ)

                                If scanList.SurveyScans.Count = 0 Then
                                    ' Need to add a "fake" survey scan that we can map this parent ion to
                                    intLastNonZoomSurveyScanIndex = scanList.AddFakeSurveyScan()
                                End If
                            Else
                                newFragScan.MRMScanInfo.MRMMassCount = 0
                            End If

                            With newFragScan
                                .LowMass = objSpectrumInfo.mzRangeStart
                                .HighMass = objSpectrumInfo.mzRangeEnd
                                .IsFTMS = False
                            End With

                            scanList.FragScans.Add(newFragScan)
                            With scanList

                                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, .FragScans.Count - 1)

                                ' Note: Even if blnKeepRawSpectra = False, we still need to load the raw data so that we can compute the noise level for the spectrum
                                StoreMzXmlSpectrum(
                                objMSSpectrum,
                                newFragScan,
                                objSpectraCache,
                                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                clsMASIC.DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                                sicOptions.CompressMSMSSpectraData,
                                mOptions.BinningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa,
                                blnKeepRawSpectra And blnKeepMSMSSpectra)

                                SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats,
                                              clsScanList.eScanTypeConstants.FragScan, newFragScan, sicOptions.DatasetNumber)

                            End With

                            If eMRMScanType = MRMScanTypeConstants.NotMRM Then
                                ' This is not an MRM scan
                                AddUpdateParentIons(scanList, intLastSurveyScanIndex, objSpectrumInfo.ParentIonMZ, scanList.FragScans.Count - 1, objSpectraCache, sicOptions)
                            Else
                                ' This is an MRM scan
                                AddUpdateParentIons(scanList, intLastNonZoomSurveyScanIndex, objSpectrumInfo.ParentIonMZ, newFragScan.MRMScanInfo, objSpectraCache, sicOptions)
                            End If

                        End If

                    End If

                    UpdateProgress(CShort(Math.Round(objXMLReader.ProgressPercentComplete, 0)))

                    UpdateCacheStats(objSpectraCache)

                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit Do
                    End If

                    If (scanList.MasterScanOrderCount - 1) Mod 100 = 0 Then
                        ReportMessage("Reading scan index: " & (scanList.MasterScanOrderCount - 1).ToString)
                        Console.Write(".")
                    End If


                Loop While blnScanFound

                ' Shrink the memory usage of the scanList arrays
                With scanList
                    ReDim Preserve .MasterScanOrder(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanNumList(.MasterScanOrderCount - 1)
                    ReDim Preserve .MasterScanTimeList(.MasterScanOrderCount - 1)
                End With

                If scanList.MasterScanOrderCount <= 0 Then
                    ' No scans found
                    ReportError("ExtractScanInfoFromMSXMLDataFile", "No scans found in the input file: " & strFilePath)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                blnSuccess = True

                Console.WriteLine()
            Catch ex As Exception
                ReportError("ExtractScanInfoFromMSXMLDataFile", "Error in ExtractScanInfoFromMSXMLDataFile", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            End Try

            ' Record the current memory usage (before we close the .mzXML file)
            OnUpdateMemoryUsage()

            ' Close the handle to the data file
            If Not objXMLReader Is Nothing Then
                Try
                    objXMLReader.CloseFile()
                    objXMLReader = Nothing
                Catch ex As Exception
                    ' Ignore errors here
                End Try
            End If

            Return blnSuccess

        End Function

        Private Sub StoreMzXmlSpectrum(
          objMSSpectrum As clsMSSpectrum,
          scanInfo As clsScanInfo,
          objSpectraCache As clsSpectraCache,
          noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
          blnDiscardLowIntensityData As Boolean,
          blnCompressSpectraData As Boolean,
          dblMSDataResolution As Double,
          blnKeepRawSpectrum As Boolean)

            Dim intIonIndex As Integer
            Dim sngTotalIonIntensity As Single

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
                        sngTotalIonIntensity = 0
                        For intIonIndex = 0 To objMSSpectrum.IonCount - 1
                            sngTotalIonIntensity += objMSSpectrum.IonsIntensity(intIonIndex)
                        Next intIonIndex

                        If .TotalIonIntensity < Single.Epsilon Then
                            .TotalIonIntensity = sngTotalIonIntensity
                        End If

                    End With

                    mScanTracking.ProcessAndStoreSpectrum(scanInfo, objSpectraCache, objMSSpectrum, noiseThresholdOptions, blnDiscardLowIntensityData, blnCompressSpectraData, dblMSDataResolution, blnKeepRawSpectrum)
                Else
                    scanInfo.TotalIonIntensity = 0
                End If

            Catch ex As Exception
                ReportError("StoreMzXMLSpectrum", "Error in clsMasic->StoreMzXMLSpectrum ", ex, True, True)
            End Try

        End Sub

        Private Sub UpdateMSXmlScanType(
          scanInfo As clsScanInfo,
          intMSLevel As Integer,
          strDefaultScanType As String,
          blnIsMzXML As Boolean,
          ByRef objMZXmlSpectrumInfo As MSDataFileReader.clsSpectrumInfoMzXML)

            Dim intMSLevelFromFilter As Integer

            With scanInfo
                If blnIsMzXML Then
                    ' Store the filter line text in .ScanHeaderText
                    ' Only Thermo files processed with ReadW will have a FilterLine

                    .ScanHeaderText = objMZXmlSpectrumInfo.FilterLine

                    If Not String.IsNullOrEmpty(.ScanHeaderText) Then
                        ' This is a Thermo file; auto define .ScanTypeName using the FilterLine text
                        .ScanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(.ScanHeaderText)

                        ' Now populate .SIMScan, .MRMScanType and .ZoomScan
                        Dim blnValidScan = XRawFileIO.ValidateMSScan(.ScanHeaderText, intMSLevelFromFilter, .SIMScan, .MRMScanType, .ZoomScan)

                    Else
                        .ScanHeaderText = String.Empty
                        .ScanTypeName = objMZXmlSpectrumInfo.ScanType

                        If String.IsNullOrEmpty(.ScanTypeName) Then
                            .ScanTypeName = strDefaultScanType
                        Else
                            ' Possibly update .ScanTypeName to match the values returned by XRawFileIO.GetScanTypeNameFromFinniganScanFilterText()
                            Select Case .ScanTypeName.ToLower
                                Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.Full.ToLower
                                    If intMSLevel <= 1 Then
                                        .ScanTypeName = "MS"
                                    Else
                                        .ScanTypeName = "MSn"
                                    End If

                                Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.zoom.ToLower
                                    .ScanTypeName = "Zoom-MS"

                                Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.MRM.ToLower
                                    .ScanTypeName = "MRM"
                                    .MRMScanType = MRMScanTypeConstants.SRM

                                Case MSDataFileReader.clsSpectrumInfoMzXML.ScanTypeNames.SRM.ToLower
                                    .ScanTypeName = "CID-SRM"
                                    .MRMScanType = MRMScanTypeConstants.SRM
                                Case Else
                                    ' Leave .ScanTypeName unchanged
                            End Select
                        End If

                        If Not String.IsNullOrWhiteSpace(objMZXmlSpectrumInfo.ActivationMethod) Then
                            ' Update ScanTypeName to include the activation method, 
                            ' For example, to be CID-MSn instead of simply MSn
                            .ScanTypeName = objMZXmlSpectrumInfo.ActivationMethod & "-" & .ScanTypeName

                            If .ScanTypeName = "HCD-MSn" Then
                                ' HCD spectra are always high res; auto-update things
                                .ScanTypeName = "HCD-HMSn"
                            End If

                        End If
                    End If

                Else
                    ' Not a .mzXML file
                    ' Use the defaults
                    .ScanHeaderText = String.Empty
                    .ScanTypeName = strDefaultScanType
                End If
            End With

        End Sub

    End Class

End Namespace