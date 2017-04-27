Imports System.Runtime.InteropServices
Imports MASIC.clsMASIC

Namespace DataInput

    Public Class clsDataImportMGFandCDF
        Inherits clsDataImport

        Public Sub New(
          masicOptions As clsMASICOptions,
          peakFinder As MASICPeakFinder.clsMASICPeakFinder,
          parentIonProcessor As clsParentIonProcessing,
          scanTracking As clsScanTracking)
            MyBase.New(masicOptions, peakFinder, parentIonProcessor, scanTracking)
        End Sub

        Public Function ExtractScanInfoFromMGFandCDF(
          strFilePath As String,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          blnKeepRawSpectra As Boolean,
          blnKeepMSMSSpectra As Boolean) As Boolean

            ' Returns True if Success, False if failure
            ' Note: This function assumes strFilePath exists
            '
            ' This function can be used to read a pair of MGF and NetCDF files that contain MS/MS and MS-only parent ion scans, respectively
            ' Typically, this will apply to LC-MS/MS analyses acquired using an Agilent mass spectrometer running DataAnalysis software
            ' strFilePath can contain the path to the MGF or to the CDF file; the extension will be removed in order to determine the base file name,
            '  then the two files will be looked for separately

            Dim dblScanTime As Double

            Dim objCDFReader As New NetCDFReader.clsMSNetCdf()
            Dim objMGFReader As New MSDataFileReader.clsMGFFileReader()

            Try
                Console.Write("Reading CDF/MGF data files ")
                ReportMessage("Reading CDF/MGF data files")

                UpdateProgress(0, "Opening data file: " & ControlChars.NewLine & Path.GetFileName(strFilePath))

                ' Obtain the full path to the file
                Dim ioFileInfo = New FileInfo(strFilePath)
                Dim strMGFInputFilePathFull = ioFileInfo.FullName

                ' Make sure the extension for strMGFInputFilePathFull is .MGF
                strMGFInputFilePathFull = Path.ChangeExtension(strMGFInputFilePathFull, AGILENT_MSMS_FILE_EXTENSION)
                Dim strCDFInputFilePathFull = Path.ChangeExtension(strMGFInputFilePathFull, AGILENT_MS_FILE_EXTENSION)

                Dim intDatasetID = mOptions.SICOptions.DatasetNumber
                Dim sicOptions = mOptions.SICOptions

                Dim blnSuccess = UpdateDatasetFileStats(ioFileInfo, intDatasetID)
                mDatasetFileInfo.ScanCount = 0

                ' Open a handle to each data file
                If Not objCDFReader.OpenMSCdfFile(strCDFInputFilePathFull) Then
                    ReportError("Error opening input data file: " & strCDFInputFilePathFull)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                If Not objMGFReader.OpenFile(strMGFInputFilePathFull) Then
                    ReportError("Error opening input data file: " & strMGFInputFilePathFull)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                Dim intMsScanCount = objCDFReader.GetScanCount()
                mDatasetFileInfo.ScanCount = intMsScanCount

                If intMsScanCount <= 0 Then
                    ' No scans found
                    ReportError("No scans found in the input file: " & strCDFInputFilePathFull)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                ' Reserve memory for all of the the Survey Scan data
                scanList.Initialize(intMsScanCount, 0)

                UpdateProgress("Reading CDF/MGF data (" & intMsScanCount.ToString() & " scans)" & ControlChars.NewLine & Path.GetFileName(strFilePath))
                ReportMessage("Reading CDF/MGF data; Total MS scan count: " & intMsScanCount.ToString())

                ' Read all of the Survey scans from the CDF file
                ' CDF files created by the Agilent XCT list the first scan number as 0; use intScanNumberCorrection to correct for this
                Dim intScanNumberCorrection = 0
                For intMsScanIndex = 0 To intMsScanCount - 1
                    Dim intScanNumber As Integer
                    Dim dblScanTotalIntensity, dblMassMin, dblMassMax As Double

                    blnSuccess = objCDFReader.GetScanInfo(intMsScanIndex, intScanNumber, dblScanTotalIntensity, dblScanTime, dblMassMin, dblMassMax)

                    If intMsScanIndex = 0 AndAlso intScanNumber = 0 Then
                        intScanNumberCorrection = 1
                    End If

                    If Not blnSuccess Then
                        ' Error reading CDF file
                        ReportError("Error obtaining data from CDF file: " & strCDFInputFilePathFull)
                        SetLocalErrorCode(eMasicErrorCodes.InputFileDataReadError)
                        Return False
                    End If

                    If intScanNumberCorrection > 0 Then intScanNumber += intScanNumberCorrection
                    Dim objMSSpectrum As New clsMSSpectrum()

                    If mScanTracking.CheckScanInRange(intScanNumber, dblScanTime, sicOptions) Then



                        Dim newSurveyScan = New clsScanInfo()
                        With newSurveyScan
                            .ScanNumber = intScanNumber
                            If mOptions.CDFTimeInSeconds Then
                                .ScanTime = CSng(dblScanTime / 60)
                            Else
                                .ScanTime = CSng(dblScanTime)
                            End If

                            ' Copy the Total Scan Intensity to .TotalIonIntensity
                            .TotalIonIntensity = CSng(dblScanTotalIntensity)

                            ' Survey scans typically lead to multiple parent ions; we do not record them here
                            .FragScanInfo.ParentIonInfoIndex = -1

                            .ScanHeaderText = String.Empty
                            .ScanTypeName = "MS"
                        End With

                        scanList.SurveyScans.Add(newSurveyScan)

                        Dim sngMZ() As Single = Nothing

                        blnSuccess = objCDFReader.GetMassSpectrum(intMsScanIndex, sngMZ,
                                                              objMSSpectrum.IonsIntensity,
                                                              objMSSpectrum.IonCount)

                        If blnSuccess AndAlso objMSSpectrum.IonCount > 0 Then
                            objMSSpectrum.ScanNumber = newSurveyScan.ScanNumber

                            With objMSSpectrum
                                ReDim .IonsMZ(.IonCount - 1)
                                sngMZ.CopyTo(.IonsMZ, 0)

                                If .IonsMZ.GetLength(0) < .IonCount Then
                                    ' Error with objCDFReader
                                    ReportError("objCDFReader returned an array of data that was shorter than expected")
                                    .IonCount = .IonsMZ.GetLength(0)
                                End If

                                If .IonsIntensity.GetLength(0) < .IonCount Then
                                    ' Error with objCDFReader
                                    ReportError("objCDFReader returned an array of data that was shorter than expected")
                                    .IonCount = .IonsIntensity.GetLength(0)
                                End If

                            End With

                            Dim dblMZMin As Double, dblMZMax As Double
                            Dim dblMSDataResolution As Double

                            With newSurveyScan
                                .IonCount = objMSSpectrum.IonCount
                                .IonCountRaw = .IonCount

                                ' Find the base peak ion mass and intensity
                                .BasePeakIonMZ = FindBasePeakIon(objMSSpectrum.IonsMZ,
                                                             objMSSpectrum.IonsIntensity,
                                                             objMSSpectrum.IonCount, .BasePeakIonIntensity,
                                                             dblMZMin, dblMZMax)

                                ' Determine the minimum positive intensity in this scan
                                .MinimumPositiveIntensity =
                                mPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount,
                                                                          objMSSpectrum.IonsIntensity, 0)
                            End With

                            If sicOptions.SICToleranceIsPPM Then
                                ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by COMPRESS_TOLERANCE_DIVISOR
                                ' However, if the lowest m/z value is < 100, then use 100 m/z
                                If dblMZMin < 100 Then
                                    dblMSDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                                        sicOptions.CompressToleranceDivisorForPPM
                                Else
                                    dblMSDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, dblMZMin) /
                                        sicOptions.CompressToleranceDivisorForPPM
                                End If
                            Else
                                dblMSDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
                            End If

                            mScanTracking.ProcessAndStoreSpectrum(
                                newSurveyScan, Me,
                                objSpectraCache, objMSSpectrum,
                                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                                sicOptions.CompressMSSpectraData,
                                dblMSDataResolution,
                                blnKeepRawSpectra)

                        Else
                            With newSurveyScan
                                .IonCount = 0
                                .IonCountRaw = 0
                            End With
                        End If

                        ' Note: Since we're reading all of the Survey Scan data, we cannot update .MasterScanOrder() at this time

                    End If

                    ' Note: We need to take intMsScanCount * 2 since we have to read two different files
                    If intMsScanCount > 1 Then
                        UpdateProgress(CShort(intMsScanIndex / (intMsScanCount * 2 - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If intMsScanIndex Mod 100 = 0 Then
                        ReportMessage("Reading MS scan index: " & intMsScanIndex.ToString())
                        Console.Write(".")
                    End If

                Next

                ' Record the current memory usage (before we close the .CDF file)
                OnUpdateMemoryUsage()

                objCDFReader.CloseMSCdfFile()

                ' We loaded all of the survey scan data above
                ' We can now initialize .MasterScanOrder()
                Dim intLastSurveyScanIndex = 0
                scanList.MasterScanOrderCount = 0
                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)

                Dim surveyScansRecorded = New SortedSet(Of Integer) From {
                    intLastSurveyScanIndex
                }

                ' Reset intScanNumberCorrection; we might also apply it to MS/MS data
                intScanNumberCorrection = 0

                ' Now read the MS/MS data from the MGF file
                Do
                    Dim objSpectrumInfo As MSDataFileReader.clsSpectrumInfo = Nothing
                    Dim blnFragScanFound = objMGFReader.ReadNextSpectrum(objSpectrumInfo)
                    If Not blnFragScanFound Then Exit Do

                    mDatasetFileInfo.ScanCount += 1

                    If objSpectrumInfo.ScanNumber < scanList.SurveyScans(intLastSurveyScanIndex).ScanNumber Then
                        ' The scan number for the current MS/MS spectrum is less than the last survey scan index scan number
                        ' This can happen, due to oddities with combining scans when creating the .MGF file
                        ' Need to decrement intLastSurveyScanIndex until we find the appropriate survey scan
                        Do
                            intLastSurveyScanIndex -= 1
                            If intLastSurveyScanIndex = 0 Then Exit Do
                        Loop While objSpectrumInfo.ScanNumber < scanList.SurveyScans(intLastSurveyScanIndex).ScanNumber

                    End If

                    If intScanNumberCorrection = 0 Then
                        ' See if udtSpectrumHeaderInfo.ScanNumberStart is equivalent to one of the survey scan numbers, yielding conflicting scan numbers
                        ' If it is, then there is an indexing error in the .MGF file; this error was present in .MGF files generated with
                        '  an older version of Agilent Chemstation.  These files typically have lines like ###MSMS: #13-29 instead of ###MSMS: #13/29/
                        ' If this indexing error is found, then we'll set intScanNumberCorrection = 1 and apply it to all subsequent MS/MS scans;
                        '  we'll also need to correct prior MS/MS scans
                        For intSurveyScanIndex = intLastSurveyScanIndex To scanList.SurveyScans.Count - 1
                            If scanList.SurveyScans(intSurveyScanIndex).ScanNumber = objSpectrumInfo.ScanNumber Then
                                ' Conflicting scan numbers were found
                                intScanNumberCorrection = 1

                                ' Need to update prior MS/MS scans
                                For Each fragScan In scanList.FragScans

                                    fragScan.ScanNumber += intScanNumberCorrection
                                    Dim dblScanTimeInterpolated = InterpolateRTandFragScanNumber(
                                    scanList.SurveyScans, 0, fragScan.ScanNumber, fragScan.FragScanInfo.FragScanNumber)

                                    fragScan.ScanTime = CSng(dblScanTimeInterpolated)

                                Next
                                Exit For
                            ElseIf scanList.SurveyScans(intSurveyScanIndex).ScanNumber > objSpectrumInfo.ScanNumber Then
                                Exit For
                            End If
                        Next
                    End If

                    If intScanNumberCorrection > 0 Then
                        objSpectrumInfo.ScanNumber += intScanNumberCorrection
                        objSpectrumInfo.ScanNumberEnd += intScanNumberCorrection
                    End If

                    Dim intFragScanIteration As Integer

                    dblScanTime = InterpolateRTandFragScanNumber(
                    scanList.SurveyScans, intLastSurveyScanIndex, objSpectrumInfo.ScanNumber, intFragScanIteration)

                    ' Make sure this fragmentation scan isn't present yet in scanList.FragScans
                    ' This can occur in Agilent .MGF files if the scan is listed both singly and grouped with other MS/MS scans
                    Dim blnValidFragScan = True
                    For Each fragScan In scanList.FragScans

                        If fragScan.ScanNumber = objSpectrumInfo.ScanNumber Then
                            ' Duplicate found
                            blnValidFragScan = False
                            Exit For
                        End If
                    Next

                    If Not (blnValidFragScan AndAlso mScanTracking.CheckScanInRange(objSpectrumInfo.ScanNumber, dblScanTime, sicOptions)) Then
                        Continue Do
                    End If



                    ' See if intLastSurveyScanIndex needs to be updated
                    ' At the same time, populate .MasterScanOrder
                    Do While intLastSurveyScanIndex < scanList.SurveyScans.Count - 1 AndAlso
                             objSpectrumInfo.ScanNumber > scanList.SurveyScans(intLastSurveyScanIndex + 1).ScanNumber

                        intLastSurveyScanIndex += 1

                        ' Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                        If Not surveyScansRecorded.Contains(intLastSurveyScanIndex) Then
                            surveyScansRecorded.Add(intLastSurveyScanIndex)

                            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan,
                                           intLastSurveyScanIndex)
                        End If
                    Loop

                    scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count,
                                   objSpectrumInfo.ScanNumber, CSng(dblScanTime))

                    Dim newFragScan = New clsScanInfo()
                    With newFragScan
                        .ScanNumber = objSpectrumInfo.ScanNumber
                        .ScanTime = CSng(dblScanTime)
                        .FragScanInfo.FragScanNumber = intFragScanIteration
                        .FragScanInfo.MSLevel = 2
                        .MRMScanInfo.MRMMassCount = 0

                        .ScanHeaderText = String.Empty
                        .ScanTypeName = "MSn"
                    End With
                    scanList.FragScans.Add(newFragScan)

                    Dim objMSSpectrum As New clsMSSpectrum() With {
                        .IonCount = objSpectrumInfo.DataCount
                    }

                    If objMSSpectrum.IonCount > 0 Then
                        objMSSpectrum.ScanNumber = newFragScan.ScanNumber
                        With objMSSpectrum
                            ReDim .IonsMZ(.IonCount - 1)
                            ReDim .IonsIntensity(.IonCount - 1)

                            objSpectrumInfo.MZList.CopyTo(.IonsMZ, 0)
                            objSpectrumInfo.IntensityList.CopyTo(.IonsMZ, 0)
                        End With

                        With newFragScan
                            .IonCount = objMSSpectrum.IonCount
                            .IonCountRaw = .IonCount

                            Dim dblMZMin As Double, dblMZMax As Double

                            ' Find the base peak ion mass and intensity
                            .BasePeakIonMZ = FindBasePeakIon(objMSSpectrum.IonsMZ, objMSSpectrum.IonsIntensity,
                                                         objMSSpectrum.IonCount, .BasePeakIonIntensity,
                                                         dblMZMin, dblMZMax)

                            ' Compute the total scan intensity
                            .TotalIonIntensity = 0
                            For intIonIndex = 0 To .IonCount - 1
                                .TotalIonIntensity += objMSSpectrum.IonsIntensity(intIonIndex)
                            Next

                            ' Determine the minimum positive intensity in this scan
                            .MinimumPositiveIntensity =
                            mPeakFinder.FindMinimumPositiveValue(objMSSpectrum.IonCount,
                                                                      objMSSpectrum.IonsIntensity, 0)

                        End With

                        Dim dblMSDataResolution = mOptions.BinningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa
                        Dim blnKeepRawSpectrum = blnKeepRawSpectra And blnKeepMSMSSpectra

                        mScanTracking.ProcessAndStoreSpectrum(
                            newFragScan, Me,
                            objSpectraCache, objMSSpectrum,
                            sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                            DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                            sicOptions.CompressMSMSSpectraData,
                            dblMSDataResolution,
                            blnKeepRawSpectrum)

                    Else
                        With newFragScan
                            .IonCount = 0
                            .IonCountRaw = 0
                            .TotalIonIntensity = 0
                        End With
                    End If

                    mParentIonProcessor.AddUpdateParentIons(scanList, intLastSurveyScanIndex, objSpectrumInfo.ParentIonMZ,
                                                            scanList.FragScans.Count - 1, objSpectraCache, sicOptions)

                    ' Note: We need to take intMsScanCount * 2, in addition to adding intMsScanCount to intLastSurveyScanIndex, since we have to read two different files
                    If intMsScanCount > 1 Then
                        UpdateProgress(CShort((intLastSurveyScanIndex + intMsScanCount) / (intMsScanCount * 2 - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit Do
                    End If

                    If scanList.FragScans.Count Mod 100 = 0 Then
                        ReportMessage("Reading MSMS scan index: " & scanList.FragScans.Count)
                        Console.Write(".")
                    End If

                Loop

                ' Record the current memory usage (before we close the .MGF file)
                OnUpdateMemoryUsage()

                objMGFReader.CloseFile()

                ' Check for any other survey scans that need to be added to MasterScanOrder

                ' See if intLastSurveyScanIndex needs to be updated
                ' At the same time, populate .MasterScanOrder
                Do While intLastSurveyScanIndex < scanList.SurveyScans.Count - 1

                    intLastSurveyScanIndex += 1

                    ' Note that dblScanTime is the scan time of the most recent survey scan processed in the above Do loop, so it's not accurate
                    If mScanTracking.CheckScanInRange(scanList.SurveyScans(intLastSurveyScanIndex).ScanNumber, dblScanTime, sicOptions) Then

                        ' Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                        If Not surveyScansRecorded.Contains(intLastSurveyScanIndex) Then
                            surveyScansRecorded.Add(intLastSurveyScanIndex)

                            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)
                        End If
                    End If
                Loop

                ' Shrink the memory usage of the scanList arrays
                ReDim Preserve scanList.MasterScanOrder(scanList.MasterScanOrderCount - 1)
                ReDim Preserve scanList.MasterScanNumList(scanList.MasterScanOrderCount - 1)
                ReDim Preserve scanList.MasterScanTimeList(scanList.MasterScanOrderCount - 1)

                ' Make sure that MasterScanOrder really is sorted by scan number
                ValidateMasterScanOrderSorting(scanList)

                ' Now that all of the data has been read, write out to the scan stats file, in order of scan number
                For intScanIndex = 0 To scanList.MasterScanOrderCount - 1

                    Dim eScanType = scanList.MasterScanOrder(intScanIndex).ScanType
                    Dim currentScan As clsScanInfo

                    If eScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                        ' Survey scan
                        currentScan = scanList.SurveyScans(scanList.MasterScanOrder(intScanIndex).ScanIndexPointer)
                    Else
                        ' Frag Scan
                        currentScan = scanList.FragScans(scanList.MasterScanOrder(intScanIndex).ScanIndexPointer)
                    End If

                    SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, eScanType, currentScan, sicOptions.DatasetNumber)
                Next

                Console.WriteLine()

                Return blnSuccess
            Catch ex As Exception
                ReportError("Error in ExtractScanInfoFromMGFandCDF", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Private Function FindBasePeakIon(
          ByRef dblMZList() As Double,
          ByRef sngIonIntensity() As Single,
          intIonCount As Integer,
          ByRef sngBasePeakIonIntensity As Single,
          ByRef dblMZMin As Double,
          ByRef dblMZMax As Double) As Double

            ' Finds the base peak ion
            ' Also determines the minimum and maximum m/z values in dblMZList
            Dim intBasePeakIndex As Integer
            Dim intDataIndex As Integer

            Try
                dblMZMin = dblMZList(0)
                dblMZMax = dblMZList(0)

                intBasePeakIndex = 0
                For intDataIndex = 0 To intIonCount - 1
                    If sngIonIntensity(intDataIndex) > sngIonIntensity(intBasePeakIndex) Then
                        intBasePeakIndex = intDataIndex
                    End If

                    If dblMZList(intDataIndex) < dblMZMin Then
                        dblMZMin = dblMZList(intDataIndex)
                    End If

                    If dblMZList(intDataIndex) > dblMZMax Then
                        dblMZMax = dblMZList(intDataIndex)
                    End If

                Next

                sngBasePeakIonIntensity = sngIonIntensity(intBasePeakIndex)
                Return dblMZList(intBasePeakIndex)

            Catch ex As Exception
                ReportError("Error in FindBasePeakIon", ex)
                sngBasePeakIonIntensity = 0
                Return 0
            End Try

        End Function


        Private Function InterpolateRTandFragScanNumber(
          surveyScans As IList(Of clsScanInfo),
          intLastSurveyScanIndex As Integer,
          intFragScanNumber As Integer,
          <Out()> ByRef intFragScanIteration As Integer) As Single

            ' Examine the scan numbers in surveyScans, starting at intLastSurveyScanIndex, to find the survey scans on either side of intFragScanNumber
            ' Interpolate the retention time that corresponds to intFragScanNumber
            ' Determine intFragScanNumber, which is generally 1, 2, or 3, indicating if this is the 1st, 2nd, or 3rd MS/MS scan after the survey scan

            Dim sngRT As Single

            intFragScanIteration = 1

            Try

                ' Decrement intLastSurveyScanIndex if the corresponding SurveyScan's scan number is larger than intFragScanNumber
                Do While intLastSurveyScanIndex > 0 AndAlso surveyScans(intLastSurveyScanIndex).ScanNumber > intFragScanNumber
                    ' This code will generally not be reached, provided the calling function passed the correct intLastSurveyScanIndex value to this function
                    intLastSurveyScanIndex -= 1
                Loop

                ' Increment intLastSurveyScanIndex if the next SurveyScan's scan number is smaller than intFragScanNumber
                Do While intLastSurveyScanIndex < surveyScans.Count - 1 AndAlso surveyScans(intLastSurveyScanIndex + 1).ScanNumber < intFragScanNumber
                    ' This code will generally not be reached, provided the calling function passed the correct intLastSurveyScanIndex value to this function
                    intLastSurveyScanIndex += 1
                Loop

                If intLastSurveyScanIndex >= surveyScans.Count - 1 Then
                    ' Cannot easily interpolate since FragScanNumber is greater than the last survey scan number
                    If surveyScans.Count > 0 Then
                        If surveyScans.Count >= 2 Then
                            ' Use the scan numbers of the last 2 survey scans to extrapolate the scan number for this fragmentation scan

                            intLastSurveyScanIndex = surveyScans.Count - 1
                            With surveyScans(intLastSurveyScanIndex)
                                Dim intScanDiff = .ScanNumber - surveyScans(intLastSurveyScanIndex - 1).ScanNumber
                                Dim sngPrevScanRT = surveyScans(intLastSurveyScanIndex - 1).ScanTime

                                ' Compute intFragScanIteration
                                intFragScanIteration = intFragScanNumber - .ScanNumber

                                If intScanDiff > 0 AndAlso intFragScanIteration > 0 Then
                                    sngRT = CSng(.ScanTime + (intFragScanIteration / intScanDiff * (.ScanTime - sngPrevScanRT)))
                                Else
                                    ' Adjacent survey scans have the same scan number
                                    ' This shouldn't happen
                                    sngRT = surveyScans(intLastSurveyScanIndex).ScanTime
                                End If

                                If intFragScanIteration < 1 Then intFragScanIteration = 1

                            End With
                        Else
                            ' Use the scan time of the highest survey scan in memory
                            sngRT = surveyScans(surveyScans.Count - 1).ScanTime
                        End If
                    Else
                        sngRT = 0
                    End If
                Else
                    ' Interpolate retention time
                    With surveyScans(intLastSurveyScanIndex)
                        Dim intScanDiff = surveyScans(intLastSurveyScanIndex + 1).ScanNumber - .ScanNumber
                        Dim sngNextScanRT = surveyScans(intLastSurveyScanIndex + 1).ScanTime

                        ' Compute intFragScanIteration
                        intFragScanIteration = intFragScanNumber - .ScanNumber

                        If intScanDiff > 0 AndAlso intFragScanIteration > 0 Then
                            sngRT = CSng(.ScanTime + (intFragScanIteration / intScanDiff * (sngNextScanRT - .ScanTime)))
                        Else
                            ' Adjacent survey scans have the same scan number
                            ' This shouldn't happen
                            sngRT = .ScanTime
                        End If

                        If intFragScanIteration < 1 Then intFragScanIteration = 1

                    End With

                End If

            Catch ex As Exception
                ' Ignore any errors that occur in this function
                ReportError("Error in InterpolateRTandFragScanNumber", ex)
            End Try

            Return sngRT

        End Function


        Private Sub ValidateMasterScanOrderSorting(scanList As clsScanList)
            ' Validate that .MasterScanOrder() really is sorted by scan number
            ' Cannot use an IComparer because .MasterScanOrder points into other arrays

            Dim intMasterScanOrderIndices() As Integer
            Dim udtMasterScanOrderListCopy() As clsScanList.udtScanOrderPointerType
            Dim sngMasterScanTimeListCopy() As Single

            Dim intIndex As Integer

            Dim blnListWasSorted As Boolean

            With scanList

                ReDim intMasterScanOrderIndices(.MasterScanOrderCount - 1)

                For intIndex = 0 To .MasterScanOrderCount - 1
                    intMasterScanOrderIndices(intIndex) = intIndex
                Next

                ' Sort .MasterScanNumList ascending, sorting the scan order indices array in parallel
                Array.Sort(.MasterScanNumList, intMasterScanOrderIndices)

                ' Check whether we need to re-populate the lists
                blnListWasSorted = False
                For intIndex = 1 To .MasterScanOrderCount - 1
                    If intMasterScanOrderIndices(intIndex) < intMasterScanOrderIndices(intIndex - 1) Then
                        blnListWasSorted = True
                    End If
                Next

                If blnListWasSorted Then
                    ' Reorder .MasterScanOrder
                    ReDim udtMasterScanOrderListCopy(.MasterScanOrder.Length - 1)
                    ReDim sngMasterScanTimeListCopy(.MasterScanOrder.Length - 1)

                    Array.Copy(.MasterScanOrder, udtMasterScanOrderListCopy, .MasterScanOrderCount)
                    Array.Copy(.MasterScanTimeList, sngMasterScanTimeListCopy, .MasterScanOrderCount)

                    For intIndex = 0 To .MasterScanOrderCount - 1
                        .MasterScanOrder(intIndex) = udtMasterScanOrderListCopy(intMasterScanOrderIndices(intIndex))
                        .MasterScanTimeList(intIndex) = sngMasterScanTimeListCopy(intMasterScanOrderIndices(intIndex))
                    Next
                End If


            End With
        End Sub

    End Class

End Namespace