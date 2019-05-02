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
          filePath As String,
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          dataOutputHandler As DataOutput.clsDataOutput,
          keepRawSpectra As Boolean,
          keepMSMSSpectra As Boolean) As Boolean

            ' Returns True if Success, False if failure
            ' Note: This function assumes filePath exists
            '
            ' This function can be used to read a pair of MGF and NetCDF files that contain MS/MS and MS-only parent ion scans, respectively
            ' Typically, this will apply to LC-MS/MS analyses acquired using an Agilent mass spectrometer running DataAnalysis software
            ' filePath can contain the path to the MGF or to the CDF file; the extension will be removed in order to determine the base file name,
            '  then the two files will be looked for separately

            Dim scanTime As Double

            Dim objCDFReader As New NetCDFReader.clsMSNetCdf()
            Dim objMGFReader As New MSDataFileReader.clsMGFFileReader()

            Try
                Console.Write("Reading CDF/MGF data files ")
                ReportMessage("Reading CDF/MGF data files")

                UpdateProgress(0, "Opening data file: " & ControlChars.NewLine & Path.GetFileName(filePath))

                ' Obtain the full path to the file
                Dim mgfFileInfo = New FileInfo(filePath)
                Dim mgfInputFilePathFull = mgfFileInfo.FullName

                ' Make sure the extension for mgfInputFilePathFull is .MGF
                mgfInputFilePathFull = Path.ChangeExtension(mgfInputFilePathFull, AGILENT_MSMS_FILE_EXTENSION)
                Dim cdfInputFilePathFull = Path.ChangeExtension(mgfInputFilePathFull, AGILENT_MS_FILE_EXTENSION)

                Dim datasetID = mOptions.SICOptions.DatasetNumber
                Dim sicOptions = mOptions.SICOptions

                Dim success = UpdateDatasetFileStats(mgfFileInfo, datasetID)
                mDatasetFileInfo.ScanCount = 0

                ' Open a handle to each data file
                If Not objCDFReader.OpenMSCdfFile(cdfInputFilePathFull) Then
                    ReportError("Error opening input data file: " & cdfInputFilePathFull)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                If Not objMGFReader.OpenFile(mgfInputFilePathFull) Then
                    ReportError("Error opening input data file: " & mgfInputFilePathFull)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                Dim msScanCount = objCDFReader.GetScanCount()
                mDatasetFileInfo.ScanCount = msScanCount

                If msScanCount <= 0 Then
                    ' No scans found
                    ReportError("No scans found in the input file: " & cdfInputFilePathFull)
                    SetLocalErrorCode(eMasicErrorCodes.InputFileAccessError)
                    Return False
                End If

                ' Reserve memory for all of the Survey Scan data
                scanList.Initialize(msScanCount, 0)

                UpdateProgress("Reading CDF/MGF data (" & msScanCount.ToString() & " scans)" & ControlChars.NewLine & Path.GetFileName(filePath))
                ReportMessage("Reading CDF/MGF data; Total MS scan count: " & msScanCount.ToString())

                ' Read all of the Survey scans from the CDF file
                ' CDF files created by the Agilent XCT list the first scan number as 0; use scanNumberCorrection to correct for this
                Dim scanNumberCorrection = 0
                For msScanIndex = 0 To msScanCount - 1
                    Dim scanNumber As Integer
                    Dim scanTotalIntensity, massMin, massMax As Double

                    success = objCDFReader.GetScanInfo(msScanIndex, scanNumber, scanTotalIntensity, scanTime, massMin, massMax)

                    If msScanIndex = 0 AndAlso scanNumber = 0 Then
                        scanNumberCorrection = 1
                    End If

                    If Not success Then
                        ' Error reading CDF file
                        ReportError("Error obtaining data from CDF file: " & cdfInputFilePathFull)
                        SetLocalErrorCode(eMasicErrorCodes.InputFileDataReadError)
                        Return False
                    End If

                    If scanNumberCorrection > 0 Then scanNumber += scanNumberCorrection
                    Dim msSpectrum As New clsMSSpectrum()

                    If mScanTracking.CheckScanInRange(scanNumber, scanTime, sicOptions) Then



                        Dim newSurveyScan = New clsScanInfo()
                        With newSurveyScan
                            .ScanNumber = scanNumber
                            If mOptions.CDFTimeInSeconds Then
                                .ScanTime = CSng(scanTime / 60)
                            Else
                                .ScanTime = CSng(scanTime)
                            End If

                            ' Copy the Total Scan Intensity to .TotalIonIntensity
                            .TotalIonIntensity = CSng(scanTotalIntensity)

                            ' Survey scans typically lead to multiple parent ions; we do not record them here
                            .FragScanInfo.ParentIonInfoIndex = -1

                            .ScanHeaderText = String.Empty
                            .ScanTypeName = "MS"
                        End With

                        scanList.SurveyScans.Add(newSurveyScan)

                        Dim mzData() As Double = Nothing

                        success = objCDFReader.GetMassSpectrum(msScanIndex, mzData,
                                                              msSpectrum.IonsIntensity,
                                                              msSpectrum.IonCount)

                        If success AndAlso msSpectrum.IonCount > 0 Then
                            msSpectrum.ScanNumber = newSurveyScan.ScanNumber

                            With msSpectrum
                                ReDim .IonsMZ(.IonCount - 1)
                                mzData.CopyTo(.IonsMZ, 0)

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

                            Dim mzMin As Double, mzMax As Double
                            Dim msDataResolution As Double

                            With newSurveyScan
                                .IonCount = msSpectrum.IonCount
                                .IonCountRaw = .IonCount

                                ' Find the base peak ion mass and intensity
                                .BasePeakIonMZ = FindBasePeakIon(msSpectrum.IonsMZ,
                                                             msSpectrum.IonsIntensity,
                                                             msSpectrum.IonCount, .BasePeakIonIntensity,
                                                             mzMin, mzMax)

                                ' Determine the minimum positive intensity in this scan
                                .MinimumPositiveIntensity =
                                mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonCount,
                                                                          msSpectrum.IonsIntensity, 0)
                            End With

                            If sicOptions.SICToleranceIsPPM Then
                                ' Define MSDataResolution based on the tolerance value that will be used at the lowest m/z in this spectrum, divided by COMPRESS_TOLERANCE_DIVISOR
                                ' However, if the lowest m/z value is < 100, then use 100 m/z
                                If mzMin < 100 Then
                                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, 100) /
                                        sicOptions.CompressToleranceDivisorForPPM
                                Else
                                    msDataResolution = clsParentIonProcessing.GetParentIonToleranceDa(sicOptions, mzMin) /
                                        sicOptions.CompressToleranceDivisorForPPM
                                End If
                            Else
                                msDataResolution = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
                            End If

                            mScanTracking.ProcessAndStoreSpectrum(
                                newSurveyScan, Me,
                                objSpectraCache, msSpectrum,
                                sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                                DISCARD_LOW_INTENSITY_MS_DATA_ON_LOAD,
                                sicOptions.CompressMSSpectraData,
                                msDataResolution,
                                keepRawSpectra)

                        Else
                            With newSurveyScan
                                .IonCount = 0
                                .IonCountRaw = 0
                            End With
                        End If

                        ' Note: Since we're reading all of the Survey Scan data, we cannot update .MasterScanOrder() at this time

                    End If

                    ' Note: We need to take msScanCount * 2 since we have to read two different files
                    If msScanCount > 1 Then
                        UpdateProgress(CShort(msScanIndex / (msScanCount * 2 - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)
                    If mOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit For
                    End If

                    If msScanIndex Mod 100 = 0 Then
                        ReportMessage("Reading MS scan index: " & msScanIndex.ToString())
                        Console.Write(".")
                    End If

                Next

                ' Record the current memory usage (before we close the .CDF file)
                OnUpdateMemoryUsage()

                objCDFReader.CloseMSCdfFile()

                ' We loaded all of the survey scan data above
                ' We can now initialize .MasterScanOrder()
                Dim lastSurveyScanIndex = 0
                scanList.MasterScanOrderCount = 0
                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, lastSurveyScanIndex)

                Dim surveyScansRecorded = New SortedSet(Of Integer) From {
                    lastSurveyScanIndex
                }

                ' Reset scanNumberCorrection; we might also apply it to MS/MS data
                scanNumberCorrection = 0

                ' Now read the MS/MS data from the MGF file
                Do
                    Dim spectrumInfo As MSDataFileReader.clsSpectrumInfo = Nothing
                    Dim fragScanFound = objMGFReader.ReadNextSpectrum(spectrumInfo)
                    If Not fragScanFound Then Exit Do

                    mDatasetFileInfo.ScanCount += 1

                    If spectrumInfo.ScanNumber < scanList.SurveyScans(lastSurveyScanIndex).ScanNumber Then
                        ' The scan number for the current MS/MS spectrum is less than the last survey scan index scan number
                        ' This can happen, due to oddities with combining scans when creating the .MGF file
                        ' Need to decrement lastSurveyScanIndex until we find the appropriate survey scan
                        Do
                            lastSurveyScanIndex -= 1
                            If lastSurveyScanIndex = 0 Then Exit Do
                        Loop While spectrumInfo.ScanNumber < scanList.SurveyScans(lastSurveyScanIndex).ScanNumber

                    End If

                    If scanNumberCorrection = 0 Then
                        ' See if udtSpectrumHeaderInfo.ScanNumberStart is equivalent to one of the survey scan numbers, yielding conflicting scan numbers
                        ' If it is, then there is an indexing error in the .MGF file; this error was present in .MGF files generated with
                        '  an older version of Agilent Chemstation.  These files typically have lines like ###MSMS: #13-29 instead of ###MSMS: #13/29/
                        ' If this indexing error is found, then we'll set scanNumberCorrection = 1 and apply it to all subsequent MS/MS scans;
                        '  we'll also need to correct prior MS/MS scans
                        For surveyScanIndex = lastSurveyScanIndex To scanList.SurveyScans.Count - 1
                            If scanList.SurveyScans(surveyScanIndex).ScanNumber = spectrumInfo.ScanNumber Then
                                ' Conflicting scan numbers were found
                                scanNumberCorrection = 1

                                ' Need to update prior MS/MS scans
                                For Each fragScan In scanList.FragScans

                                    fragScan.ScanNumber += scanNumberCorrection
                                    Dim scanTimeInterpolated = InterpolateRTandFragScanNumber(
                                    scanList.SurveyScans, 0, fragScan.ScanNumber, fragScan.FragScanInfo.FragScanNumber)

                                    fragScan.ScanTime = CSng(scanTimeInterpolated)

                                Next
                                Exit For
                            ElseIf scanList.SurveyScans(surveyScanIndex).ScanNumber > spectrumInfo.ScanNumber Then
                                Exit For
                            End If
                        Next
                    End If

                    If scanNumberCorrection > 0 Then
                        spectrumInfo.ScanNumber += scanNumberCorrection
                        spectrumInfo.ScanNumberEnd += scanNumberCorrection
                    End If

                    Dim fragScanIteration As Integer

                    scanTime = InterpolateRTandFragScanNumber(
                    scanList.SurveyScans, lastSurveyScanIndex, spectrumInfo.ScanNumber, fragScanIteration)

                    ' Make sure this fragmentation scan isn't present yet in scanList.FragScans
                    ' This can occur in Agilent .MGF files if the scan is listed both singly and grouped with other MS/MS scans
                    Dim validFragScan = True
                    For Each fragScan In scanList.FragScans

                        If fragScan.ScanNumber = spectrumInfo.ScanNumber Then
                            ' Duplicate found
                            validFragScan = False
                            Exit For
                        End If
                    Next

                    If Not (validFragScan AndAlso mScanTracking.CheckScanInRange(spectrumInfo.ScanNumber, scanTime, sicOptions)) Then
                        Continue Do
                    End If

                    ' See if lastSurveyScanIndex needs to be updated
                    ' At the same time, populate .MasterScanOrder
                    Do While lastSurveyScanIndex < scanList.SurveyScans.Count - 1 AndAlso
                             spectrumInfo.ScanNumber > scanList.SurveyScans(lastSurveyScanIndex + 1).ScanNumber

                        lastSurveyScanIndex += 1

                        ' Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                        If Not surveyScansRecorded.Contains(lastSurveyScanIndex) Then
                            surveyScansRecorded.Add(lastSurveyScanIndex)

                            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan,
                                           lastSurveyScanIndex)
                        End If
                    Loop

                    scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count,
                                   spectrumInfo.ScanNumber, CSng(scanTime))

                    Dim newFragScan = New clsScanInfo()
                    With newFragScan
                        .ScanNumber = spectrumInfo.ScanNumber
                        .ScanTime = CSng(scanTime)
                        .FragScanInfo.FragScanNumber = fragScanIteration
                        .FragScanInfo.MSLevel = 2
                        .MRMScanInfo.MRMMassCount = 0

                        .ScanHeaderText = String.Empty
                        .ScanTypeName = "MSn"
                    End With
                    scanList.FragScans.Add(newFragScan)

                    Dim msSpectrum As New clsMSSpectrum() With {
                        .IonCount = spectrumInfo.DataCount
                    }

                    If msSpectrum.IonCount > 0 Then
                        msSpectrum.ScanNumber = newFragScan.ScanNumber
                        With msSpectrum
                            ReDim .IonsMZ(.IonCount - 1)
                            ReDim .IonsIntensity(.IonCount - 1)

                            spectrumInfo.MZList.CopyTo(.IonsMZ, 0)

                            ' Copy one item at a time since spectrumInfo.IntensityList is a float but msSpectrum.IonsIntensity is a double
                            For i = 0 To spectrumInfo.IntensityList.Count - 1
                                .IonsIntensity(i) = spectrumInfo.IntensityList(i)
                            Next

                        End With

                        With newFragScan
                            .IonCount = msSpectrum.IonCount
                            .IonCountRaw = .IonCount

                            Dim mzMin As Double, mzMax As Double

                            ' Find the base peak ion mass and intensity
                            .BasePeakIonMZ = FindBasePeakIon(msSpectrum.IonsMZ, msSpectrum.IonsIntensity,
                                                         msSpectrum.IonCount, .BasePeakIonIntensity,
                                                         mzMin, mzMax)

                            ' Compute the total scan intensity
                            .TotalIonIntensity = 0
                            For ionIndex = 0 To .IonCount - 1
                                .TotalIonIntensity += msSpectrum.IonsIntensity(ionIndex)
                            Next

                            ' Determine the minimum positive intensity in this scan
                            .MinimumPositiveIntensity =
                            mPeakFinder.FindMinimumPositiveValue(msSpectrum.IonCount,
                                                                      msSpectrum.IonsIntensity, 0)

                        End With

                        Dim msDataResolution = mOptions.BinningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa
                        Dim keepRawSpectrum = keepRawSpectra AndAlso keepMSMSSpectra

                        mScanTracking.ProcessAndStoreSpectrum(
                            newFragScan, Me,
                            objSpectraCache, msSpectrum,
                            sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions,
                            DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD,
                            sicOptions.CompressMSMSSpectraData,
                            msDataResolution,
                            keepRawSpectrum)

                    Else
                        With newFragScan
                            .IonCount = 0
                            .IonCountRaw = 0
                            .TotalIonIntensity = 0
                        End With
                    End If

                    mParentIonProcessor.AddUpdateParentIons(scanList, lastSurveyScanIndex, spectrumInfo.ParentIonMZ,
                                                            scanList.FragScans.Count - 1, objSpectraCache, sicOptions)

                    ' Note: We need to take msScanCount * 2, in addition to adding msScanCount to lastSurveyScanIndex, since we have to read two different files
                    If msScanCount > 1 Then
                        UpdateProgress(CShort((lastSurveyScanIndex + msScanCount) / (msScanCount * 2 - 1) * 100))
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

                ' See if lastSurveyScanIndex needs to be updated
                ' At the same time, populate .MasterScanOrder
                Do While lastSurveyScanIndex < scanList.SurveyScans.Count - 1

                    lastSurveyScanIndex += 1

                    ' Note that scanTime is the scan time of the most recent survey scan processed in the above Do loop, so it's not accurate
                    If mScanTracking.CheckScanInRange(scanList.SurveyScans(lastSurveyScanIndex).ScanNumber, scanTime, sicOptions) Then

                        ' Add the given SurveyScan to .MasterScanOrder, though only if it hasn't yet been added
                        If Not surveyScansRecorded.Contains(lastSurveyScanIndex) Then
                            surveyScansRecorded.Add(lastSurveyScanIndex)

                            scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, lastSurveyScanIndex)
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
                For scanIndex = 0 To scanList.MasterScanOrderCount - 1

                    Dim eScanType = scanList.MasterScanOrder(scanIndex).ScanType
                    Dim currentScan As clsScanInfo

                    If eScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                        ' Survey scan
                        currentScan = scanList.SurveyScans(scanList.MasterScanOrder(scanIndex).ScanIndexPointer)
                    Else
                        ' Frag Scan
                        currentScan = scanList.FragScans(scanList.MasterScanOrder(scanIndex).ScanIndexPointer)
                    End If

                    SaveScanStatEntry(dataOutputHandler.OutputFileHandles.ScanStats, eScanType, currentScan, sicOptions.DatasetNumber)
                Next

                Console.WriteLine()

                Return success
            Catch ex As Exception
                ReportError("Error in ExtractScanInfoFromMGFandCDF", ex, eMasicErrorCodes.InputFileDataReadError)
                Return False
            End Try

        End Function

        Private Function FindBasePeakIon(
          ByRef mzList() As Double,
          ByRef ionIntensity() As Double,
          ionCount As Integer,
          ByRef basePeakIonIntensity As Double,
          ByRef mzMin As Double,
          ByRef mzMax As Double) As Double

            ' Finds the base peak ion
            ' Also determines the minimum and maximum m/z values in mzList
            Dim basePeakIndex As Integer
            Dim dataIndex As Integer

            Try
                mzMin = mzList(0)
                mzMax = mzList(0)

                basePeakIndex = 0
                For dataIndex = 0 To ionCount - 1
                    If ionIntensity(dataIndex) > ionIntensity(basePeakIndex) Then
                        basePeakIndex = dataIndex
                    End If

                    If mzList(dataIndex) < mzMin Then
                        mzMin = mzList(dataIndex)
                    End If

                    If mzList(dataIndex) > mzMax Then
                        mzMax = mzList(dataIndex)
                    End If

                Next

                basePeakIonIntensity = ionIntensity(basePeakIndex)
                Return mzList(basePeakIndex)

            Catch ex As Exception
                ReportError("Error in FindBasePeakIon", ex)
                basePeakIonIntensity = 0
                Return 0
            End Try

        End Function

        ''' <summary>
        ''' Examine the scan numbers in surveyScans, starting at lastSurveyScanIndex, to find the survey scans on either side of fragScanNumber
        ''' Interpolate the retention time that corresponds to fragScanNumber
        ''' Determine fragScanNumber, which is generally 1, 2, or 3, indicating if this is the 1st, 2nd, or 3rd MS/MS scan after the survey scan
        ''' </summary>
        ''' <param name="surveyScans"></param>
        ''' <param name="lastSurveyScanIndex"></param>
        ''' <param name="fragScanNumber"></param>
        ''' <param name="fragScanIteration"></param>
        ''' <returns>Closest elution time</returns>
        Private Function InterpolateRTandFragScanNumber(
          surveyScans As IList(Of clsScanInfo),
          lastSurveyScanIndex As Integer,
          fragScanNumber As Integer,
          <Out> ByRef fragScanIteration As Integer) As Single

            Dim elutionTime As Single

            fragScanIteration = 1

            Try

                ' Decrement lastSurveyScanIndex if the corresponding SurveyScan's scan number is larger than fragScanNumber
                Do While lastSurveyScanIndex > 0 AndAlso surveyScans(lastSurveyScanIndex).ScanNumber > fragScanNumber
                    ' This code will generally not be reached, provided the calling function passed the correct lastSurveyScanIndex value to this function
                    lastSurveyScanIndex -= 1
                Loop

                ' Increment lastSurveyScanIndex if the next SurveyScan's scan number is smaller than fragScanNumber
                Do While lastSurveyScanIndex < surveyScans.Count - 1 AndAlso surveyScans(lastSurveyScanIndex + 1).ScanNumber < fragScanNumber
                    ' This code will generally not be reached, provided the calling function passed the correct lastSurveyScanIndex value to this function
                    lastSurveyScanIndex += 1
                Loop

                If lastSurveyScanIndex >= surveyScans.Count - 1 Then
                    ' Cannot easily interpolate since FragScanNumber is greater than the last survey scan number
                    If surveyScans.Count > 0 Then
                        If surveyScans.Count >= 2 Then
                            ' Use the scan numbers of the last 2 survey scans to extrapolate the scan number for this fragmentation scan

                            lastSurveyScanIndex = surveyScans.Count - 1
                            With surveyScans(lastSurveyScanIndex)
                                Dim scanDiff = .ScanNumber - surveyScans(lastSurveyScanIndex - 1).ScanNumber
                                Dim prevScanElutionTime = surveyScans(lastSurveyScanIndex - 1).ScanTime

                                ' Compute fragScanIteration
                                fragScanIteration = fragScanNumber - .ScanNumber

                                If scanDiff > 0 AndAlso fragScanIteration > 0 Then
                                    elutionTime = CSng(.ScanTime + (fragScanIteration / scanDiff * (.ScanTime - prevScanElutionTime)))
                                Else
                                    ' Adjacent survey scans have the same scan number
                                    ' This shouldn't happen
                                    elutionTime = surveyScans(lastSurveyScanIndex).ScanTime
                                End If

                                If fragScanIteration < 1 Then fragScanIteration = 1

                            End With
                        Else
                            ' Use the scan time of the highest survey scan in memory
                            elutionTime = surveyScans(surveyScans.Count - 1).ScanTime
                        End If
                    Else
                        elutionTime = 0
                    End If
                Else
                    ' Interpolate retention time
                    With surveyScans(lastSurveyScanIndex)
                        Dim scanDiff = surveyScans(lastSurveyScanIndex + 1).ScanNumber - .ScanNumber
                        Dim nextScanElutionTime = surveyScans(lastSurveyScanIndex + 1).ScanTime

                        ' Compute fragScanIteration
                        fragScanIteration = fragScanNumber - .ScanNumber

                        If scanDiff > 0 AndAlso fragScanIteration > 0 Then
                            elutionTime = CSng(.ScanTime + (fragScanIteration / scanDiff * (nextScanElutionTime - .ScanTime)))
                        Else
                            ' Adjacent survey scans have the same scan number
                            ' This shouldn't happen
                            elutionTime = .ScanTime
                        End If

                        If fragScanIteration < 1 Then fragScanIteration = 1

                    End With

                End If

            Catch ex As Exception
                ' Ignore any errors that occur in this function
                ReportError("Error in InterpolateRTandFragScanNumber", ex)
            End Try

            Return elutionTime

        End Function


        Private Sub ValidateMasterScanOrderSorting(scanList As clsScanList)
            ' Validate that .MasterScanOrder() really is sorted by scan number
            ' Cannot use an IComparer because .MasterScanOrder points into other arrays

            Dim masterScanOrderIndices() As Integer
            Dim udtMasterScanOrderListCopy() As clsScanList.udtScanOrderPointerType
            Dim masterScanTimeListCopy() As Single

            Dim listWasSorted As Boolean

            With scanList

                ReDim masterScanOrderIndices(.MasterScanOrderCount - 1)

                For index = 0 To .MasterScanOrderCount - 1
                    masterScanOrderIndices(index) = index
                Next

                ' Sort .MasterScanNumList ascending, sorting the scan order indices array in parallel
                Array.Sort(.MasterScanNumList, masterScanOrderIndices)

                ' Check whether we need to re-populate the lists
                listWasSorted = False
                For index = 1 To .MasterScanOrderCount - 1
                    If masterScanOrderIndices(index) < masterScanOrderIndices(index - 1) Then
                        listWasSorted = True
                    End If
                Next

                If listWasSorted Then
                    ' Reorder .MasterScanOrder
                    ReDim udtMasterScanOrderListCopy(.MasterScanOrder.Length - 1)
                    ReDim masterScanTimeListCopy(.MasterScanOrder.Length - 1)

                    Array.Copy(.MasterScanOrder, udtMasterScanOrderListCopy, .MasterScanOrderCount)
                    Array.Copy(.MasterScanTimeList, masterScanTimeListCopy, .MasterScanOrderCount)

                    For index = 0 To .MasterScanOrderCount - 1
                        .MasterScanOrder(index) = udtMasterScanOrderListCopy(masterScanOrderIndices(index))
                        .MasterScanTimeList(index) = masterScanTimeListCopy(masterScanOrderIndices(index))
                    Next
                End If


            End With
        End Sub

    End Class

End Namespace