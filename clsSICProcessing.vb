Imports MASIC.DataOutput

Public Class clsSICProcessing
    Inherits clsEventNotifier

#Region "Classwide variables"
    Private ReadOnly mMASICPeakFinder As MASICPeakFinder.clsMASICPeakFinder
    Private ReadOnly mMRMProcessor As clsMRMProcessing
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(peakFinder As MASICPeakFinder.clsMASICPeakFinder, mrmProcessor As clsMRMProcessing)
        mMASICPeakFinder = peakFinder
        mMRMProcessor = mrmProcessor
    End Sub

    Private Function CreateMZLookupList(
      masicOptions As clsMASICOptions,
      scanList As clsScanList,
      ByRef udtMZBinList() As clsDataObjects.udtMZBinListType,
      ByRef intParentIonIndices() As Integer,
      blnProcessSIMScans As Boolean,
      intSIMIndex As Integer) As Boolean

        Dim intParentIonIndex As Integer
        Dim intMZListCount As Integer

        Dim blnIncludeParentIon As Boolean

        intMZListCount = 0
        ReDim udtMZBinList(scanList.ParentIonInfoCount - 1)
        ReDim intParentIonIndices(scanList.ParentIonInfoCount - 1)

        Dim sicOptions = masicOptions.SICOptions

        For intParentIonIndex = 0 To scanList.ParentIonInfoCount - 1

            If scanList.ParentIons(intParentIonIndex).MRMDaughterMZ > 0 Then
                blnIncludeParentIon = False
            Else
                If masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                    ' Always include CustomSICPeak entries
                    blnIncludeParentIon = scanList.ParentIons(intParentIonIndex).CustomSICPeak
                Else
                    ' Use blnProcessingSIMScans and .SIMScan to decide whether or not to include the entry
                    With scanList.SurveyScans(scanList.ParentIons(intParentIonIndex).SurveyScanIndex)
                        If blnProcessSIMScans Then
                            If .SIMScan Then
                                If .SIMIndex = intSIMIndex Then
                                    blnIncludeParentIon = True
                                Else
                                    blnIncludeParentIon = False
                                End If
                            Else
                                blnIncludeParentIon = False
                            End If
                        Else
                            blnIncludeParentIon = Not .SIMScan
                        End If
                    End With
                End If
            End If

            If blnIncludeParentIon Then
                udtMZBinList(intMZListCount).MZ = scanList.ParentIons(intParentIonIndex).MZ
                If scanList.ParentIons(intParentIonIndex).CustomSICPeak Then
                    udtMZBinList(intMZListCount).MZTolerance = scanList.ParentIons(intParentIonIndex).CustomSICPeakMZToleranceDa
                    udtMZBinList(intMZListCount).MZToleranceIsPPM = False
                Else
                    udtMZBinList(intMZListCount).MZTolerance = sicOptions.SICTolerance
                    udtMZBinList(intMZListCount).MZToleranceIsPPM = sicOptions.SICToleranceIsPPM
                End If
                intParentIonIndices(intMZListCount) = intParentIonIndex
                intMZListCount += 1
            End If
        Next intParentIonIndex

        If intMZListCount > 0 Then
            If intMZListCount < scanList.ParentIonInfoCount Then
                ReDim Preserve udtMZBinList(intMZListCount - 1)
                ReDim Preserve intParentIonIndices(intMZListCount - 1)
            End If

            ' Sort udtMZBinList ascending and sort intParentIonIndices in parallel
            Array.Sort(udtMZBinList, intParentIonIndices, New clsMZBinListComparer())
            Return True
        Else
            Return False
        End If

    End Function

    Public Function CreateParentIonSICs(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      masicOptions As clsMASICOptions,
      dataOutputHandler As clsDataOutput,
      sicProcessor As clsSICProcessing,
      xmlResultsWriter As clsXMLResultsWriter) As Boolean

        Dim blnSuccess As Boolean
        Dim intParentIonIndex As Integer
        Dim intParentIonsProcessed As Integer

        If scanList.ParentIonInfoCount <= 0 Then
            ' No parent ions
            If masicOptions.SuppressNoParentIonsError Then
                Return True
            Else
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.NoParentIonsFoundInInputFile)
                Return False
            End If
        ElseIf scanList.SurveyScans.Count <= 0 Then
            ' No survey scans
            If masicOptions.SuppressNoParentIonsError Then
                Return True
            Else
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.NoSurveyScansFoundInInputFile)
                Return False
            End If
        End If

        Try
            intParentIonsProcessed = 0
            masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow

            UpdateProgress(0, "Creating SIC's for the parent ions")
            Console.Write("Creating SIC's for parent ions ")
            ReportMessage("Creating SIC's for parent ions")

            ' Create an array of m/z values in scanList.ParentIons, then sort by m/z
            ' Next, step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance

            Dim udtMZBinList() As clsDataObjects.udtMZBinListType = Nothing

            Dim intParentIonIndices() As Integer = Nothing
            Dim intSIMIndex As Integer
            Dim intSIMIndexMax As Integer

            ' First process the non SIM, non MRM scans
            ' If this file only has MRM scans, then CreateMZLookupList will return False
            If CreateMZLookupList(masicOptions, scanList, udtMZBinList, intParentIonIndices, False, 0) Then
                blnSuccess = ProcessMZList(scanList, objSpectraCache, masicOptions,
                                           dataOutputHandler, xmlResultsWriter,
                                           udtMZBinList, intParentIonIndices,
                                           False, 0, intParentIonsProcessed)
            End If

            If blnSuccess And Not masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                ' Now process the SIM scans (if any)
                ' First, see if any SIMScans are present and determine the maximum SIM Index
                intSIMIndexMax = -1
                For intParentIonIndex = 0 To scanList.ParentIonInfoCount - 1
                    With scanList.SurveyScans(scanList.ParentIons(intParentIonIndex).SurveyScanIndex)
                        If .SIMScan Then
                            If .SIMIndex > intSIMIndexMax Then
                                intSIMIndexMax = .SIMIndex
                            End If
                        End If
                    End With
                Next intParentIonIndex

                ' Now process each SIM Scan type
                For intSIMIndex = 0 To intSIMIndexMax
                    If CreateMZLookupList(masicOptions, scanList, udtMZBinList, intParentIonIndices, True, intSIMIndex) Then
                        blnSuccess = ProcessMZList(scanList, objSpectraCache, masicOptions,
                                                   dataOutputHandler, xmlResultsWriter,
                                                   udtMZBinList, intParentIonIndices,
                                                   True, intSIMIndex, intParentIonsProcessed)
                    End If
                Next intSIMIndex
            End If

            ' Lastly, process the MRM scans (if any)
            If scanList.MRMDataPresent Then
                blnSuccess = mMRMProcessor.ProcessMRMList(scanList, objSpectraCache, sicProcessor, xmlResultsWriter, mMASICPeakFinder, intParentIonsProcessed)
            End If

            Console.WriteLine()
            blnSuccess = True

        Catch ex As Exception
            ReportError("CreateParentIonSICs", "Error creating Parent Ion SICs", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function ExtractSICDetailsFromFullSIC(
      intMZIndexWork As Integer,
      ByRef udtMZSearchChunk() As clsDataObjects.udtMZSearchInfoType,
      intFullSICDataCount As Integer,
      ByRef intFullSICScanIndices(,) As Integer,
      ByRef sngFullSICIntensities(,) As Single,
      ByRef dblFullSICMasses(,) As Double,
      scanList As clsScanList,
      intScanIndexObservedInFullSIC As Integer,
      ByRef udtSICDetails As clsDataObjects.udtSICStatsDetailsType,
      ByRef udtSICPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType,
      masicOptions As clsMASICOptions,
      scanNumScanConverter As clsScanNumScanTimeConversion,
      blnCustomSICPeak As Boolean,
      sngCustomSICPeakScanOrAcqTimeTolerance As Single) As Boolean

        ' Minimum number of scans to extend left or right of the scan that meets the minimum intensity threshold requirement
        Const MINIMUM_NOISE_SCANS_TO_INCLUDE = 10

        Dim sngCustomSICScanToleranceMinutesHalfWidth As Single

        ' Pointers to entries in intFullSICScanIndices() and sngFullSICIntensities()
        Dim intScanIndexStart As Integer, intScanIndexEnd As Integer

        Dim sngMaximumIntensity As Single

        Dim sicOptions = masicOptions.SICOptions

        ' Initialize the peak
        udtSICPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

        ' Update .BaselineNoiseStats in udtSICPeak
        udtSICPeak.BaselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(
            intScanIndexObservedInFullSIC, udtMZSearchChunk(intMZIndexWork).BaselineNoiseStatSegments)

        ' Initialize the values for the maximum width of the SIC peak; these might get altered for custom SIC values
        Dim sngMaxSICPeakWidthMinutesBackward = sicOptions.MaxSICPeakWidthMinutesBackward
        Dim sngMaxSICPeakWidthMinutesForward = sicOptions.MaxSICPeakWidthMinutesForward

        ' Limit the data examined to a portion of intFullSICScanIndices() and intFullSICIntensities, populating udtSICDetails
        Try

            ' Initialize intCustomSICScanToleranceHalfWidth
            With masicOptions.CustomSICList
                If sngCustomSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    sngCustomSICPeakScanOrAcqTimeTolerance = .ScanOrAcqTimeTolerance
                End If

                If sngCustomSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    ' Use the entire SIC
                    ' Specify this by setting sngCustomSICScanToleranceMinutesHalfWidth to the maximum scan time in .MasterScanTimeList()
                    With scanList
                        If .MasterScanOrderCount > 0 Then
                            sngCustomSICScanToleranceMinutesHalfWidth = .MasterScanTimeList(.MasterScanOrderCount - 1)
                        Else
                            sngCustomSICScanToleranceMinutesHalfWidth = Single.MaxValue
                        End If
                    End With
                Else
                    If .ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.Relative AndAlso sngCustomSICPeakScanOrAcqTimeTolerance > 10 Then
                        ' Relative scan time should only range from 0 to 1; we'll allow values up to 10
                        sngCustomSICPeakScanOrAcqTimeTolerance = 10
                    End If

                    sngCustomSICScanToleranceMinutesHalfWidth = scanNumScanConverter.ScanOrAcqTimeToScanTime(
                        scanList, sngCustomSICPeakScanOrAcqTimeTolerance / 2, .ScanToleranceType, True)
                End If

                If blnCustomSICPeak Then
                    If sngMaxSICPeakWidthMinutesBackward < sngCustomSICScanToleranceMinutesHalfWidth Then
                        sngMaxSICPeakWidthMinutesBackward = sngCustomSICScanToleranceMinutesHalfWidth
                    End If

                    If sngMaxSICPeakWidthMinutesForward < sngCustomSICScanToleranceMinutesHalfWidth Then
                        sngMaxSICPeakWidthMinutesForward = sngCustomSICScanToleranceMinutesHalfWidth
                    End If
                End If
            End With

            ' Initially use just 3 survey scans, centered around intScanIndexObservedInFullSIC
            If intScanIndexObservedInFullSIC > 0 Then
                intScanIndexStart = intScanIndexObservedInFullSIC - 1
                intScanIndexEnd = intScanIndexObservedInFullSIC + 1
            Else
                intScanIndexStart = 0
                intScanIndexEnd = 1
                intScanIndexObservedInFullSIC = 0
            End If

            If intScanIndexEnd >= intFullSICDataCount Then intScanIndexEnd = intFullSICDataCount - 1

        Catch ex As Exception
            ReportError("ExtractSICDetailsFromFullSIC", "Error initializing SIC start/end indices", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
        End Try

        If intScanIndexEnd >= intScanIndexStart Then

            Dim intScanIndexMax As Integer

            Try
                ' Start by using the 3 survey scans centered around intScanIndexObservedInFullSIC
                sngMaximumIntensity = -1
                intScanIndexMax = -1
                For intScanIndex = intScanIndexStart To intScanIndexEnd
                    If sngFullSICIntensities(intMZIndexWork, intScanIndex) > sngMaximumIntensity Then
                        sngMaximumIntensity = sngFullSICIntensities(intMZIndexWork, intScanIndex)
                        intScanIndexMax = intScanIndex
                    End If
                Next intScanIndex
            Catch ex As Exception
                ReportError("ExtractSICDetailsFromFullSIC", "Error while creating initial SIC", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
            End Try

            ' Now extend the SIC, stepping left and right until a threshold is reached
            Dim blnLeftDone = False
            Dim blnRightDone = False

            ' The index of the first scan found to be below threshold (on the left)
            Dim intScanIndexBelowThresholdLeft = -1

            ' The index of the first scan found to be below threshold (on the right)
            Dim intScanIndexBelowThresholdRight = -1


            Do While (intScanIndexStart > 0 AndAlso Not blnLeftDone) OrElse (intScanIndexEnd < intFullSICDataCount - 1 AndAlso Not blnRightDone)
                Try
                    ' Extend the SIC to the left until the threshold is reached
                    If intScanIndexStart > 0 AndAlso Not blnLeftDone Then
                        If sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < udtSICPeak.BaselineNoiseStats.NoiseLevel Then
                            If intScanIndexBelowThresholdLeft < 0 Then
                                intScanIndexBelowThresholdLeft = intScanIndexStart
                            Else
                                If intScanIndexStart <= intScanIndexBelowThresholdLeft - MINIMUM_NOISE_SCANS_TO_INCLUDE Then
                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    ' Stop creating the SIC to the left
                                    blnLeftDone = True
                                End If
                            End If
                        Else
                            intScanIndexBelowThresholdLeft = -1
                        End If

                        Dim sngPeakWidthMinutesBackward = scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime -
                           scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexStart)).ScanTime

                        If blnLeftDone Then
                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the left of intScanIndexObservedInFullSIC and to the left of intScanIndexMax
                            If intScanIndexObservedInFullSIC - intScanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnLeftDone = False
                            If intScanIndexMax - intScanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnLeftDone = False

                            ' For custom SIC values, make sure the scan range has been satisfied
                            If blnLeftDone AndAlso blnCustomSICPeak Then
                                If sngPeakWidthMinutesBackward < sngCustomSICScanToleranceMinutesHalfWidth Then
                                    blnLeftDone = False
                                End If
                            End If
                        End If

                        If Not blnLeftDone Then
                            If intScanIndexStart = 0 Then
                                blnLeftDone = True
                            Else
                                intScanIndexStart -= 1
                                If sngFullSICIntensities(intMZIndexWork, intScanIndexStart) > sngMaximumIntensity Then
                                    sngMaximumIntensity = sngFullSICIntensities(intMZIndexWork, intScanIndexStart)
                                    intScanIndexMax = intScanIndexStart
                                End If
                            End If
                        End If

                        sngPeakWidthMinutesBackward = scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime -
                           scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexStart)).ScanTime

                        If sngPeakWidthMinutesBackward >= sngMaxSICPeakWidthMinutesBackward Then
                            blnLeftDone = True
                        End If

                    End If

                Catch ex As Exception
                    ReportError("ExtractSICDetailsFromFullSIC", "Error extending SIC to the left", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
                End Try

                Try
                    ' Extend the SIC to the right until the threshold is reached
                    If intScanIndexEnd < intFullSICDataCount - 1 AndAlso Not blnRightDone Then
                        If sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < udtSICPeak.BaselineNoiseStats.NoiseLevel Then
                            If intScanIndexBelowThresholdRight < 0 Then
                                intScanIndexBelowThresholdRight = intScanIndexEnd
                            Else
                                If intScanIndexEnd >= intScanIndexBelowThresholdRight + MINIMUM_NOISE_SCANS_TO_INCLUDE Then
                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    ' Stop creating the SIC to the right
                                    blnRightDone = True
                                End If
                            End If
                        Else
                            intScanIndexBelowThresholdRight = -1
                        End If

                        Dim sngPeakWidthMinutesForward = scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexEnd)).ScanTime -
                          scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime

                        If blnRightDone Then
                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the right of intScanIndexObservedInFullSIC and to the Rigth of intScanIndexMax
                            If intScanIndexEnd - intScanIndexObservedInFullSIC < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnRightDone = False
                            If intScanIndexEnd - intScanIndexMax < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then blnRightDone = False

                            ' For custom SIC values, make sure the scan range has been satisfied
                            If blnRightDone AndAlso blnCustomSICPeak Then
                                If sngPeakWidthMinutesForward < sngCustomSICScanToleranceMinutesHalfWidth Then
                                    blnRightDone = False
                                End If
                            End If
                        End If

                        If Not blnRightDone Then
                            If intScanIndexEnd = intFullSICDataCount - 1 Then
                                blnRightDone = True
                            Else
                                intScanIndexEnd += 1
                                If sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) > sngMaximumIntensity Then
                                    sngMaximumIntensity = sngFullSICIntensities(intMZIndexWork, intScanIndexEnd)
                                    intScanIndexMax = intScanIndexEnd
                                End If
                            End If
                        End If

                        sngPeakWidthMinutesForward = scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexEnd)).ScanTime -
                          scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndexObservedInFullSIC)).ScanTime

                        If sngPeakWidthMinutesForward >= sngMaxSICPeakWidthMinutesForward Then
                            blnRightDone = True
                        End If
                    End If

                Catch ex As Exception
                    ReportError("ExtractSICDetailsFromFullSIC", "Error extending SIC to the right", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
                End Try

            Loop    ' While Not LeftDone and Not RightDone

        End If

        ' Populate udtSICDetails with the data between intScanIndexStart and intScanIndexEnd
        If intScanIndexStart < 0 Then intScanIndexStart = 0
        If intScanIndexEnd >= intFullSICDataCount Then intScanIndexEnd = intFullSICDataCount - 1

        If intScanIndexEnd < intScanIndexStart Then
            ReportError("ExtractSICDetailsFromFullSIC", "Programming error: intScanIndexEnd < intScanIndexStart", Nothing, True, True, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
            intScanIndexEnd = intScanIndexStart
        End If

        Try

            ' Copy the scan index values from intFullSICScanIndices to .SICScanIndices()
            ' Copy the intensity values from sngFullSICIntensities() to .SICData()
            ' Copy the mz values from dblFullSICMasses() to .SICMasses()

            With udtSICDetails
                .SICDataCount = intScanIndexEnd - intScanIndexStart + 1
                .SICScanType = clsScanList.eScanTypeConstants.SurveyScan

                If .SICDataCount > .SICScanIndices.Length Then
                    ReDim .SICScanIndices(udtSICDetails.SICDataCount - 1)
                    ReDim .SICScanNumbers(udtSICDetails.SICDataCount - 1)
                    ReDim .SICData(udtSICDetails.SICDataCount - 1)
                    ReDim .SICMasses(udtSICDetails.SICDataCount - 1)
                End If

                udtSICPeak.IndexObserved = 0
                .SICDataCount = 0
                For intScanIndex = intScanIndexStart To intScanIndexEnd
                    If intFullSICScanIndices(intMZIndexWork, intScanIndex) >= 0 Then
                        .SICScanIndices(.SICDataCount) = intFullSICScanIndices(intMZIndexWork, intScanIndex)
                        .SICScanNumbers(.SICDataCount) = scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndex)).ScanNumber
                        .SICData(.SICDataCount) = sngFullSICIntensities(intMZIndexWork, intScanIndex)
                        .SICMasses(.SICDataCount) = dblFullSICMasses(intMZIndexWork, intScanIndex)

                        If intScanIndex = intScanIndexObservedInFullSIC Then
                            udtSICPeak.IndexObserved = .SICDataCount
                        End If
                        .SICDataCount += 1
                    Else
                        ' This shouldn't happen
                    End If
                Next intScanIndex
            End With

        Catch ex As Exception
            ReportError("ExtractSICDetailsFromFullSIC", "Error populating .SICScanIndices, .SICData, and .SICMasses", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
        End Try

        Return True

    End Function

    Private Function ProcessMZList(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      masicOptions As clsMASICOptions,
      dataOutputHandler As clsDataOutput,
      xmlResultsWriter As clsXMLResultsWriter,
      ByRef udtMZBinList() As clsDataObjects.udtMZBinListType,
      ByRef intParentIonIndices() As Integer,
      blnProcessSIMScans As Boolean,
      intSIMIndex As Integer,
      ByRef intParentIonsProcessed As Integer) As Boolean


        ' Step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance
        ' Note that udtMZBinList() and intParentIonIndices() are parallel arrays, with udtMZBinList() sorted on ascending m/z
        Const MAX_RAW_DATA_MEMORY_USAGE_MB = 50
        Const DATA_COUNT_MEMORY_RESERVE = 200

        Dim intMZIndex As Integer
        Dim intMZIndexWork As Integer
        Dim intMaxMZCountInChunk As Integer

        Dim intSurveyScanIndex As Integer
        Dim intParentIonIndexPointer As Integer
        Dim intDataIndex As Integer
        Dim intScanIndexObservedInFullSIC As Integer

        Dim intPoolIndex As Integer

        Dim sngIonSum As Single
        Dim dblClosestMZ As Double
        Dim intIonMatchCount As Integer

        Dim dblMZToleranceDa As Double

        ' Ranges from 0 to intMZSearchChunkCount-1
        Dim intMZSearchChunkCount As Integer
        Dim udtMZSearchChunk() As clsDataObjects.udtMZSearchInfoType

        ' The following are 2D arrays, ranging from 0 to intMZSearchChunkCount-1 in the first dimension and 0 to .SurveyScans.Count - 1 in the second dimension
        ' I could have included these in udtMZSearchChunk but memory management is more efficient if I use 2D arrays for this data
        Dim intFullSICScanIndices(,) As Integer     ' Pointer into .SurveyScans
        Dim sngFullSICIntensities(,) As Single
        Dim dblFullSICMasses(,) As Double
        Dim intFullSICDataCount() As Integer        ' Count of the number of valid entries in the second dimension of the above 3 arrays

        ' The following is a 1D array, containing the SIC intensities for a single m/z group
        Dim sngFullSICIntensities1D() As Single

        Dim udtSICPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType
        Dim udtSICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
        Dim udtSICPotentialAreaStatsInFullSIC As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

        ' Note: The arrays in this variable contain valid data from index 0 to .SICDataCount-1
        '       Do not assume that the amount of usable data is from index 0 to .SICData.Length -1, since these arrays are increased in length when needed, but never decreased in length (to reduce the number of times ReDim is called)
        Dim udtSICDetails As clsDataObjects.udtSICStatsDetailsType
        Dim udtSmoothedYData As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType
        Dim udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType

        Dim blnParentIonUpdated() As Boolean

        Dim blnUseScan As Boolean
        Dim blnStorePeakInParentIon As Boolean
        Dim blnLargestPeakFound As Boolean
        Dim blnSuccess As Boolean

        Const DebugParentIonIndexToFind = 3139
        Const DebugMZToFind As Single = 488.47

        Try
            ' Determine the maximum number of m/z values to process simultaneously
            ' Limit the total memory usage to ~50 MB
            ' Each m/z value will require 12 bytes per scan

            If scanList.SurveyScans.Count > 0 Then
                intMaxMZCountInChunk = CInt((MAX_RAW_DATA_MEMORY_USAGE_MB * 1024 * 1024) / (scanList.SurveyScans.Count * 12))
            Else
                intMaxMZCountInChunk = 1
            End If

            If intMaxMZCountInChunk > udtMZBinList.Length Then
                intMaxMZCountInChunk = udtMZBinList.Length
            End If
            If intMaxMZCountInChunk < 1 Then intMaxMZCountInChunk = 1

            ' Reserve room in dblSearchMZs
            ReDim udtMZSearchChunk(intMaxMZCountInChunk - 1)

            ' Reserve room in intFullSICScanIndices for at most intMaxMZCountInChunk values and .SurveyScans.Count scans
            ReDim intFullSICDataCount(intMaxMZCountInChunk - 1)
            ReDim intFullSICScanIndices(intMaxMZCountInChunk - 1, scanList.SurveyScans.Count - 1)
            ReDim sngFullSICIntensities(intMaxMZCountInChunk - 1, scanList.SurveyScans.Count - 1)
            ReDim dblFullSICMasses(intMaxMZCountInChunk - 1, scanList.SurveyScans.Count - 1)

            ReDim sngFullSICIntensities1D(scanList.SurveyScans.Count - 1)

            ' Pre-reserve space in the arrays in udtSICDetails
            With udtSICDetails
                .SICDataCount = 0
                .SICScanType = clsScanList.eScanTypeConstants.SurveyScan

                ReDim .SICScanIndices(DATA_COUNT_MEMORY_RESERVE)
                ReDim .SICScanNumbers(DATA_COUNT_MEMORY_RESERVE)
                ReDim .SICData(DATA_COUNT_MEMORY_RESERVE)
                ReDim .SICMasses(DATA_COUNT_MEMORY_RESERVE)

            End With

            ' Reserve room in udtSmoothedYData and udtSmoothedYDataSubset
            With udtSmoothedYData
                .DataCount = 0
                ReDim .Data(DATA_COUNT_MEMORY_RESERVE)
            End With

            With udtSmoothedYDataSubset
                .DataCount = 0
                ReDim .Data(DATA_COUNT_MEMORY_RESERVE)
            End With

            ' Reserve room in blnParentIonUpdated
            ReDim blnParentIonUpdated(intParentIonIndices.Length - 1)

        Catch ex As Exception
            ReportError("ProcessMZList", "Error reserving memory for the m/z chunks", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
            Return False
        End Try

        Try

            Dim dataAggregation = New clsDataAggregation()
            RegisterEvents(dataAggregation)

            Dim scanNumScanConverter As New clsScanNumScanTimeConversion()
            RegisterEvents(scanNumScanConverter)

            intMZSearchChunkCount = 0
            intMZIndex = 0
            Do While intMZIndex < udtMZBinList.Length

                '---------------------------------------------------------
                ' Find the next group of m/z values to use, starting with intMZIndex
                '---------------------------------------------------------
                With udtMZSearchChunk(intMZSearchChunkCount)
                    ' Initially set the MZIndexStart to intMZIndex
                    .MZIndexStart = intMZIndex


                    ' Look for adjacent m/z values within udtMZBinList(.MZIndexStart).MZToleranceDa / 2 
                    '  of the m/z value that starts this group
                    ' Only group m/z values with the same udtMZBinList().MZTolerance and udtMZBinList().MZToleranceIsPPM values
                    .MZTolerance = udtMZBinList(.MZIndexStart).MZTolerance
                    .MZToleranceIsPPM = udtMZBinList(.MZIndexStart).MZToleranceIsPPM

                    If .MZToleranceIsPPM Then
                        dblMZToleranceDa = clsUtilities.PPMToMass(.MZTolerance, udtMZBinList(.MZIndexStart).MZ)
                    Else
                        dblMZToleranceDa = .MZTolerance
                    End If

                    Do While intMZIndex < udtMZBinList.Length - 2 AndAlso
                     Math.Abs(udtMZBinList(intMZIndex + 1).MZTolerance - .MZTolerance) < Double.Epsilon AndAlso
                     udtMZBinList(intMZIndex + 1).MZToleranceIsPPM = .MZToleranceIsPPM AndAlso
                     udtMZBinList(intMZIndex + 1).MZ - udtMZBinList(.MZIndexStart).MZ <= dblMZToleranceDa / 2
                        intMZIndex += 1
                    Loop
                    .MZIndexEnd = intMZIndex

                    If .MZIndexEnd = .MZIndexStart Then
                        .MZIndexMidpoint = .MZIndexEnd
                        .SearchMZ = udtMZBinList(.MZIndexStart).MZ
                    Else
                        ' Determine the median m/z of the members in the m/z group
                        If (.MZIndexEnd - .MZIndexStart) Mod 2 = 0 Then
                            ' Odd number of points; use the m/z value of the midpoint
                            .MZIndexMidpoint = .MZIndexStart + CInt((.MZIndexEnd - .MZIndexStart) / 2)
                            .SearchMZ = udtMZBinList(.MZIndexMidpoint).MZ
                        Else
                            ' Even number of points; average the values on either side of (.mzIndexEnd - .mzIndexStart / 2)
                            .MZIndexMidpoint = .MZIndexStart + CInt(Math.Floor((.MZIndexEnd - .MZIndexStart) / 2))
                            .SearchMZ = (udtMZBinList(.MZIndexMidpoint).MZ + udtMZBinList(.MZIndexMidpoint + 1).MZ) / 2
                        End If
                    End If

                End With
                intMZSearchChunkCount += 1

                If intMZSearchChunkCount >= intMaxMZCountInChunk OrElse intMZIndex = udtMZBinList.Length - 1 Then
                    '---------------------------------------------------------
                    ' Reached intMaxMZCountInChunk m/z value
                    ' Process all of the m/z values in udtMZSearchChunk
                    '---------------------------------------------------------

                    ' Initialize .MaximumIntensity and .ScanIndexMax
                    ' Additionally, reset intFullSICDataCount() and, for safety, set intFullSICScanIndices() to -1
                    For intMZIndexWork = 0 To intMZSearchChunkCount - 1
                        With udtMZSearchChunk(intMZIndexWork)
                            .MaximumIntensity = 0
                            .ScanIndexMax = 0
                        End With

                        intFullSICDataCount(intMZIndexWork) = 0
                        For intSurveyScanIndex = 0 To scanList.SurveyScans.Count - 1
                            intFullSICScanIndices(intMZIndexWork, intSurveyScanIndex) = -1
                        Next intSurveyScanIndex
                    Next intMZIndexWork

                    '---------------------------------------------------------
                    ' Step through scanList to obtain the scan numbers and intensity data for each .SearchMZ in udtMZSearchChunk
                    ' We're stepping scan by scan since the process of loading a scan from disk is slower than the process of searching for each m/z in the scan
                    '---------------------------------------------------------
                    For intSurveyScanIndex = 0 To scanList.SurveyScans.Count - 1
                        If blnProcessSIMScans Then
                            If scanList.SurveyScans(intSurveyScanIndex).SIMScan AndAlso
                               scanList.SurveyScans(intSurveyScanIndex).SIMIndex = intSIMIndex Then
                                blnUseScan = True
                            Else
                                blnUseScan = False
                            End If
                        Else
                            blnUseScan = Not scanList.SurveyScans(intSurveyScanIndex).SIMScan

                            If scanList.SurveyScans(intSurveyScanIndex).ZoomScan Then
                                blnUseScan = False
                            End If
                        End If

                        If blnUseScan Then
                            If Not objSpectraCache.ValidateSpectrumInPool(scanList.SurveyScans(intSurveyScanIndex).ScanNumber, intPoolIndex) Then
                                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum)
                                Return False
                            End If

                            For intMZIndexWork = 0 To intMZSearchChunkCount - 1
                                With udtMZSearchChunk(intMZIndexWork)
                                    If .MZToleranceIsPPM Then
                                        dblMZToleranceDa = clsUtilities.PPMToMass(.MZTolerance, .SearchMZ)
                                    Else
                                        dblMZToleranceDa = .MZTolerance
                                    End If

                                    sngIonSum = dataAggregation.AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex), .SearchMZ, dblMZToleranceDa, intIonMatchCount, dblClosestMZ, False)

                                    intDataIndex = intFullSICDataCount(intMZIndexWork)
                                    intFullSICScanIndices(intMZIndexWork, intDataIndex) = intSurveyScanIndex
                                    sngFullSICIntensities(intMZIndexWork, intDataIndex) = sngIonSum

                                    If sngIonSum < Single.Epsilon AndAlso masicOptions.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData Then
                                        sngFullSICIntensities(intMZIndexWork, intDataIndex) = scanList.SurveyScans(intSurveyScanIndex).MinimumPositiveIntensity
                                    End If

                                    dblFullSICMasses(intMZIndexWork, intDataIndex) = dblClosestMZ
                                    If sngIonSum > .MaximumIntensity Then
                                        .MaximumIntensity = sngIonSum
                                        .ScanIndexMax = intDataIndex
                                    End If

                                    intFullSICDataCount(intMZIndexWork) += 1
                                End With
                            Next intMZIndexWork
                        End If

                        If intSurveyScanIndex Mod 100 = 0 Then
                            UpdateProgress("Loading raw SIC data: " & intSurveyScanIndex & " / " & scanList.SurveyScans.Count)
                            If masicOptions.AbortProcessing Then
                                scanList.ProcessingIncomplete = True
                                Exit Do
                            End If
                        End If
                    Next intSurveyScanIndex

                    UpdateProgress("Creating SIC's for the parent ions")

                    If masicOptions.AbortProcessing Then
                        scanList.ProcessingIncomplete = True
                        Exit Do
                    End If

                    '---------------------------------------------------------
                    ' Compute the noise level in sngFullSICIntensities() for each m/z in udtMZSearchChunk
                    ' Also, find the peaks for each m/z in udtMZSearchChunk and retain the largest peak found
                    '---------------------------------------------------------
                    For intMZIndexWork = 0 To intMZSearchChunkCount - 1

                        ' Use this for debugging
                        If Math.Abs(udtMZSearchChunk(intMZIndexWork).SearchMZ - DebugMZToFind) < 0.1 Then
                            intParentIonIndexPointer = udtMZSearchChunk(intMZIndexWork).MZIndexStart
                        End If

                        ' Copy the data for this m/z into sngFullSICIntensities1D()
                        For intDataIndex = 0 To intFullSICDataCount(intMZIndexWork) - 1
                            sngFullSICIntensities1D(intDataIndex) = sngFullSICIntensities(intMZIndexWork, intDataIndex)
                        Next intDataIndex

                        ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
                        blnSuccess = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(sngFullSICIntensities1D, 0, intFullSICDataCount(intMZIndexWork) - 1, masicOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions, udtMZSearchChunk(intMZIndexWork).BaselineNoiseStatSegments)

                        If Not blnSuccess Then
                            SetLocalErrorCode(clsMASIC.eMasicErrorCodes.FindSICPeaksError, True)
                            Exit Try
                        End If

                        ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
                        mMASICPeakFinder.FindPotentialPeakArea(intFullSICDataCount(intMZIndexWork), sngFullSICIntensities1D, udtSICPotentialAreaStatsInFullSIC, masicOptions.SICOptions.SICPeakFinderOptions)

                        ' Clear udtSICPotentialAreaStatsForPeak
                        udtSICPotentialAreaStatsForPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

                        intScanIndexObservedInFullSIC = udtMZSearchChunk(intMZIndexWork).ScanIndexMax

                        ' Populate udtSICDetails using the data centered around the highest intensity in intFullSICIntensities
                        ' Note that this function will update udtSICPeak.IndexObserved
                        blnSuccess = ExtractSICDetailsFromFullSIC(
                            intMZIndexWork, udtMZSearchChunk,
                            intFullSICDataCount(intMZIndexWork), intFullSICScanIndices, sngFullSICIntensities, dblFullSICMasses,
                            scanList, intScanIndexObservedInFullSIC,
                            udtSICDetails, udtSICPeak,
                            masicOptions, scanNumScanConverter, False, 0)

                        ' Find the largest peak in the SIC for this m/z
                        blnLargestPeakFound = mMASICPeakFinder.FindSICPeakAndArea(
                           udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData,
                           udtSICPotentialAreaStatsForPeak, udtSICPeak,
                           udtSmoothedYDataSubset, masicOptions.SICOptions.SICPeakFinderOptions,
                           udtSICPotentialAreaStatsInFullSIC,
                           True, scanList.SIMDataPresent, False)

                        If blnLargestPeakFound Then
                            '--------------------------------------------------------
                            ' Step through the parent ions and see if .SurveyScanIndex is contained in udtSICPeak
                            ' If it is, then assign the stats of the largest peak to the given parent ion
                            '--------------------------------------------------------
                            For intParentIonIndexPointer = udtMZSearchChunk(intMZIndexWork).MZIndexStart To udtMZSearchChunk(intMZIndexWork).MZIndexEnd
                                ' Use this for debugging
                                If intParentIonIndices(intParentIonIndexPointer) = DebugParentIonIndexToFind Then
                                    intScanIndexObservedInFullSIC = -1
                                End If

                                blnStorePeakInParentIon = False
                                If scanList.ParentIons(intParentIonIndices(intParentIonIndexPointer)).CustomSICPeak Then Continue For

                                ' Assign the stats of the largest peak to each parent ion with .SurveyScanIndex contained in the peak
                                With scanList.ParentIons(intParentIonIndices(intParentIonIndexPointer))
                                    If .SurveyScanIndex >= udtSICDetails.SICScanIndices(udtSICPeak.IndexBaseLeft) AndAlso
                                       .SurveyScanIndex <= udtSICDetails.SICScanIndices(udtSICPeak.IndexBaseRight) Then

                                        blnStorePeakInParentIon = True
                                    End If
                                End With


                                If blnStorePeakInParentIon Then
                                    blnSuccess = StorePeakInParentIon(scanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, udtSICPotentialAreaStatsForPeak, udtSICPeak, True)

                                    ' Possibly save the stats for this SIC to the SICData file
                                    dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                                                                        intParentIonIndices(intParentIonIndexPointer), udtSICDetails)

                                    ' Save the stats for this SIC to the XML file
                                    xmlResultsWriter.SaveDataToXML(scanList,
                                                                   intParentIonIndices(intParentIonIndexPointer), udtSICDetails,
                                                                   udtSmoothedYDataSubset, dataOutputHandler)

                                    blnParentIonUpdated(intParentIonIndexPointer) = True
                                    intParentIonsProcessed += 1

                                End If


                            Next intParentIonIndexPointer
                        End If

                        '--------------------------------------------------------
                        ' Now step through the parent ions and process those that were not updated using udtSICPeak
                        ' For each, search for the closest peak in sngSICIntensity
                        '--------------------------------------------------------
                        For intParentIonIndexPointer = udtMZSearchChunk(intMZIndexWork).MZIndexStart To udtMZSearchChunk(intMZIndexWork).MZIndexEnd

                            If Not blnParentIonUpdated(intParentIonIndexPointer) Then
                                If intParentIonIndices(intParentIonIndexPointer) = DebugParentIonIndexToFind Then
                                    intScanIndexObservedInFullSIC = -1
                                End If

                                With scanList.ParentIons(intParentIonIndices(intParentIonIndexPointer))
                                    ' Clear udtSICPotentialAreaStatsForPeak
                                    .SICStats.SICPotentialAreaStatsForPeak = New MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType

                                    ' Record the index in the Full SIC that the parent ion mass was first observed
                                    ' Search for .SurveyScanIndex in intFullSICScanIndices
                                    intScanIndexObservedInFullSIC = -1
                                    For intDataIndex = 0 To intFullSICDataCount(intMZIndexWork) - 1
                                        If intFullSICScanIndices(intMZIndexWork, intDataIndex) >= .SurveyScanIndex Then
                                            intScanIndexObservedInFullSIC = intDataIndex
                                            Exit For
                                        End If
                                    Next intDataIndex

                                    If intScanIndexObservedInFullSIC = -1 Then
                                        ' Match wasn't found; this is unexpected
                                        ReportError("ProcessMZList", "Programming error: survey scan index not found in intFullSICScanIndices()", Nothing, True, True, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                                        intScanIndexObservedInFullSIC = 0
                                    End If

                                    ' Populate udtSICDetails using the data centered around intScanIndexObservedInFullSIC
                                    ' Note that this function will update udtSICPeak.IndexObserved
                                    blnSuccess = ExtractSICDetailsFromFullSIC(
                                        intMZIndexWork, udtMZSearchChunk,
                                        intFullSICDataCount(intMZIndexWork), intFullSICScanIndices, sngFullSICIntensities, dblFullSICMasses,
                                        scanList, intScanIndexObservedInFullSIC,
                                        udtSICDetails, .SICStats.Peak,
                                        masicOptions, scanNumScanConverter,
                                        .CustomSICPeak, .CustomSICPeakScanOrAcqTimeTolerance)

                                    blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(
                                     udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData,
                                     .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak,
                                     udtSmoothedYDataSubset, masicOptions.SICOptions.SICPeakFinderOptions,
                                     udtSICPotentialAreaStatsInFullSIC,
                                     Not .CustomSICPeak, scanList.SIMDataPresent, False)


                                    blnSuccess = StorePeakInParentIon(scanList, intParentIonIndices(intParentIonIndexPointer), udtSICDetails, .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak, blnSuccess)
                                End With

                                ' Possibly save the stats for this SIC to the SICData file
                                dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                                                                    intParentIonIndices(intParentIonIndexPointer), udtSICDetails)

                                ' Save the stats for this SIC to the XML file
                                xmlResultsWriter.SaveDataToXML(scanList,
                                                               intParentIonIndices(intParentIonIndexPointer), udtSICDetails,
                                                               udtSmoothedYDataSubset, dataOutputHandler)

                                blnParentIonUpdated(intParentIonIndexPointer) = True
                                intParentIonsProcessed += 1

                            End If
                        Next intParentIonIndexPointer


                        '---------------------------------------------------------
                        ' Update progress
                        '---------------------------------------------------------
                        Try

                            If scanList.ParentIonInfoCount > 1 Then
                                UpdateProgress(CShort(intParentIonsProcessed / (scanList.ParentIonInfoCount - 1) * 100))
                            Else
                                UpdateProgress(0)
                            End If

                            UpdateCacheStats(objSpectraCache)
                            If masicOptions.AbortProcessing Then
                                scanList.ProcessingIncomplete = True
                                Exit For
                            End If

                            If intParentIonsProcessed Mod 100 = 0 Then
                                If DateTime.UtcNow.Subtract(masicOptions.LastParentIonProcessingLogTime).TotalSeconds >= 10 OrElse intParentIonsProcessed Mod 500 = 0 Then
                                    ReportMessage("Parent Ions Processed: " & intParentIonsProcessed.ToString)
                                    Console.Write(".")
                                    masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow
                                End If
                            End If

                        Catch ex As Exception
                            ReportError("ProcessMZList", "Error updating progress", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
                        End Try

                    Next intMZIndexWork

                    ' Reset intMZSearchChunkCount to 0
                    intMZSearchChunkCount = 0
                End If

                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Exit Do
                End If

                intMZIndex += 1
            Loop

            blnSuccess = True
        Catch ex As Exception
            ReportError("ProcessMZList", "Error processing the m/z chunks to create the SIC data", ex, True, True, clsMASIC.eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try


        Return blnSuccess

    End Function

    Public Function StorePeakInParentIon(
      scanList As clsScanList,
      intParentIonIndex As Integer,
      ByRef udtSICDetails As clsDataObjects.udtSICStatsDetailsType,
      ByRef udtSICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType,
      ByRef udtSICPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType,
      blnPeakIsValid As Boolean) As Boolean


        Dim intDataIndex As Integer
        Dim intScanIndexObserved As Integer
        Dim intFragScanNumber As Integer

        Dim blnProcessingMRMPeak As Boolean
        Dim blnSuccess As Boolean

        Try

            With scanList
                If udtSICDetails.SICData Is Nothing OrElse udtSICDetails.SICDataCount = 0 Then
                    ' Either .SICData is nothing or no SIC data exists
                    ' Cannot find peaks for this parent ion
                    With .ParentIons(intParentIonIndex).SICStats
                        With .Peak
                            .IndexObserved = 0
                            .IndexBaseLeft = .IndexObserved
                            .IndexBaseRight = .IndexObserved
                            .IndexMax = .IndexObserved
                        End With
                    End With
                Else
                    With .ParentIons(intParentIonIndex)
                        intScanIndexObserved = .SurveyScanIndex
                        If intScanIndexObserved < 0 Then intScanIndexObserved = 0

                        If .MRMDaughterMZ > 0 Then
                            blnProcessingMRMPeak = True
                        Else
                            blnProcessingMRMPeak = False
                        End If

                        With .SICStats

                            .SICPotentialAreaStatsForPeak = udtSICPotentialAreaStatsForPeak
                            .Peak = udtSICPeak

                            .ScanTypeForPeakIndices = udtSICDetails.SICScanType
                            If blnProcessingMRMPeak Then
                                If .ScanTypeForPeakIndices <> clsScanList.eScanTypeConstants.FragScan Then
                                    ' ScanType is not FragScan; this is unexpected
                                    ReportError("StorePeakInParentIon", "Programming error: udtSICDetails.SICScanType is not FragScan even though we're processing an MRM peak", Nothing, True, True, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                                    .ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan
                                End If
                            End If

                            If blnProcessingMRMPeak Then
                                .Peak.IndexObserved = 0
                            Else
                                ' Record the index (of data in .SICData) that the parent ion mass was first observed
                                ' This is not necessarily the same as udtSICPeak.IndexObserved, so we need to search for it here

                                ' Search for intScanIndexObserved in udtSICDetails.SICScanIndices()
                                .Peak.IndexObserved = -1
                                For intDataIndex = 0 To udtSICDetails.SICDataCount - 1
                                    If udtSICDetails.SICScanIndices(intDataIndex) = intScanIndexObserved Then
                                        .Peak.IndexObserved = intDataIndex
                                        Exit For
                                    End If
                                Next intDataIndex

                                If .Peak.IndexObserved = -1 Then
                                    ' Match wasn't found; this is unexpected
                                    ReportError("StorePeakInParentIon", "Programming error: survey scan index not found in udtSICDetails.SICScanIndices", Nothing, True, True, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                                    .Peak.IndexObserved = 0
                                End If
                            End If

                            If scanList.FragScans.Count > 0 AndAlso scanList.ParentIons(intParentIonIndex).FragScanIndices(0) < scanList.FragScans.Count Then
                                ' Record the fragmentation scan number
                                intFragScanNumber = scanList.FragScans(scanList.ParentIons(intParentIonIndex).FragScanIndices(0)).ScanNumber
                            Else
                                ' Use the parent scan number as the fragmentation scan number
                                ' This is OK based on how mMASICPeakFinder.ComputeParentIonIntensity() uses intFragScanNumber
                                intFragScanNumber = scanList.SurveyScans(scanList.ParentIons(intParentIonIndex).SurveyScanIndex).ScanNumber
                            End If

                            If blnProcessingMRMPeak Then
                                udtSICPeak.ParentIonIntensity = 0
                            Else
                                ' Determine the value for .ParentIonIntensity
                                blnSuccess = mMASICPeakFinder.ComputeParentIonIntensity(udtSICDetails.SICDataCount, udtSICDetails.SICScanNumbers, udtSICDetails.SICData, .Peak, intFragScanNumber)
                            End If

                            If blnPeakIsValid Then
                                ' Record the survey scan indices of the peak max, start, and end
                                ' Note that .ScanTypeForPeakIndices was set earlier in this function
                                .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
                                .PeakScanIndexStart = udtSICDetails.SICScanIndices(.Peak.IndexBaseLeft)
                                .PeakScanIndexEnd = udtSICDetails.SICScanIndices(.Peak.IndexBaseRight)
                            Else
                                ' No peak found
                                .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
                                .PeakScanIndexStart = .PeakScanIndexMax
                                .PeakScanIndexEnd = .PeakScanIndexMax

                                With .Peak
                                    .MaxIntensityValue = udtSICDetails.SICData(.IndexMax)
                                    .IndexBaseLeft = .IndexMax
                                    .IndexBaseRight = .IndexMax
                                    .FWHMScanWidth = 1
                                    ' Assign the intensity of the peak at the observed maximum to the area
                                    .Area = .MaxIntensityValue

                                    .SignalToNoiseRatio = MASICPeakFinder.clsMASICPeakFinder.ComputeSignalToNoise(.MaxIntensityValue, .BaselineNoiseStats.NoiseLevel)
                                End With
                            End If
                        End With
                    End With

                    ' Update .OptimalPeakApexScanNumber
                    ' Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
                    With .ParentIons(intParentIonIndex)
                        If blnProcessingMRMPeak Then
                            .OptimalPeakApexScanNumber = scanList.FragScans(udtSICDetails.SICScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
                        Else
                            .OptimalPeakApexScanNumber = scanList.SurveyScans(udtSICDetails.SICScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
                        End If
                    End With

                End If

            End With

            blnSuccess = True

        Catch ex As Exception

            ReportError("StorePeakInParentIon", "Error finding SIC peaks and their areas", ex, True, False, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
            blnSuccess = False

        End Try

        Return blnSuccess

    End Function

    Protected Class clsMZBinListComparer
        Implements IComparer

        Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
            Dim udtMZBinListA As clsDataObjects.udtMZBinListType
            Dim udtMZBinListB As clsDataObjects.udtMZBinListType

            udtMZBinListA = DirectCast(x, clsDataObjects.udtMZBinListType)
            udtMZBinListB = DirectCast(y, clsDataObjects.udtMZBinListType)

            If udtMZBinListA.MZ > udtMZBinListB.MZ Then
                Return 1
            ElseIf udtMZBinListA.MZ < udtMZBinListB.MZ Then
                Return -1
            Else
                Return 0
            End If
        End Function
    End Class

End Class
