Imports System.Runtime.InteropServices
Imports MASIC.DataOutput
Imports MASICPeakFinder

Public Class clsSICProcessing
    Inherits clsMasicEventNotifier

#Region "Classwide variables"
    Private ReadOnly mMASICPeakFinder As clsMASICPeakFinder
    Private ReadOnly mMRMProcessor As clsMRMProcessing
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(peakFinder As clsMASICPeakFinder, mrmProcessor As clsMRMProcessing)
        mMASICPeakFinder = peakFinder
        mMRMProcessor = mrmProcessor
    End Sub

    Private Function CreateMZLookupList(
      masicOptions As clsMASICOptions,
      scanList As clsScanList,
      blnProcessSIMScans As Boolean,
      intSIMIndex As Integer) As List(Of clsMzBinInfo)

        Dim intParentIonIndex As Integer

        Dim blnIncludeParentIon As Boolean

        Dim mzBinList = New List(Of clsMzBinInfo)(scanList.ParentIonInfoCount - 1)

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
                Dim newMzBin = New clsMzBinInfo() With {
                    .MZ = scanList.ParentIons(intParentIonIndex).MZ,
                    .ParentIonIndex = intParentIonIndex
                }

                If scanList.ParentIons(intParentIonIndex).CustomSICPeak Then
                    newMzBin.MZTolerance = scanList.ParentIons(intParentIonIndex).CustomSICPeakMZToleranceDa
                    newMzBin.MZToleranceIsPPM = False
                Else
                    newMzBin.MZTolerance = sicOptions.SICTolerance
                    newMzBin.MZToleranceIsPPM = sicOptions.SICToleranceIsPPM
                End If

                mzBinList.Add(newMzBin)

            End If
        Next

        ' Sort mzBinList by m/z
        Dim sortedMzBins = (From item In mzBinList Select item Order By item.MZ).ToList()

        Return sortedMzBins

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

            Dim intSIMIndex As Integer
            Dim intSIMIndexMax As Integer

            ' First process the non SIM, non MRM scans
            ' If this file only has MRM scans, then CreateMZLookupList will return False
            Dim mzBinList = CreateMZLookupList(masicOptions, scanList, False, 0)
            If mzBinList.Count > 0 Then

                blnSuccess = ProcessMZList(scanList, objSpectraCache, masicOptions,
                                           dataOutputHandler, xmlResultsWriter,
                                           mzBinList, False, 0, intParentIonsProcessed)
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
                Next

                ' Now process each SIM Scan type
                For intSIMIndex = 0 To intSIMIndexMax

                    Dim mzBinListSIM = CreateMZLookupList(masicOptions, scanList, True, intSIMIndex)

                    If mzBinListSIM.Count > 0 Then

                        ProcessMZList(scanList, objSpectraCache, masicOptions,
                                                   dataOutputHandler, xmlResultsWriter,
                                                   mzBinListSIM, True, intSIMIndex, intParentIonsProcessed)
                    End If
                Next
            End If

            ' Lastly, process the MRM scans (if any)
            If scanList.MRMDataPresent Then
                blnSuccess = mMRMProcessor.ProcessMRMList(scanList, objSpectraCache, sicProcessor, xmlResultsWriter, mMASICPeakFinder, intParentIonsProcessed)
            End If

            Console.WriteLine()
            blnSuccess = True

        Catch ex As Exception
            ReportError("Error creating Parent Ion SICs", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function ExtractSICDetailsFromFullSIC(
      intMZIndexWork As Integer,
      baselineNoiseStatSegments As List(Of clsBaselineNoiseStatsSegment),
      intFullSICDataCount As Integer,
      intFullSICScanIndices(,) As Integer,
      sngFullSICIntensities(,) As Single,
      dblFullSICMasses(,) As Double,
      scanList As clsScanList,
      intScanIndexObservedInFullSIC As Integer,
      sicDetails As clsSICDetails,
      <Out()> ByRef sicPeak As clsSICStatsPeak,
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

        Dim baselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(intScanIndexObservedInFullSIC, baselineNoiseStatSegments)

        ' Initialize the peak
        sicPeak = New clsSICStatsPeak() With {
            .BaselineNoiseStats = baselineNoiseStats
        }

        ' Initialize the values for the maximum width of the SIC peak; these might get altered for custom SIC values
        Dim maxSICPeakWidthMinutesBackward = sicOptions.MaxSICPeakWidthMinutesBackward
        Dim maxSICPeakWidthMinutesForward = sicOptions.MaxSICPeakWidthMinutesForward

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
                    If maxSICPeakWidthMinutesBackward < sngCustomSICScanToleranceMinutesHalfWidth Then
                        maxSICPeakWidthMinutesBackward = sngCustomSICScanToleranceMinutesHalfWidth
                    End If

                    If maxSICPeakWidthMinutesForward < sngCustomSICScanToleranceMinutesHalfWidth Then
                        maxSICPeakWidthMinutesForward = sngCustomSICScanToleranceMinutesHalfWidth
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
            ReportError("Error initializing SIC start/end indices", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
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
                Next
            Catch ex As Exception
                ReportError("Error while creating initial SIC", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
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
                           sngFullSICIntensities(intMZIndexWork, intScanIndexStart) < sicPeak.BaselineNoiseStats.NoiseLevel Then
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

                        If sngPeakWidthMinutesBackward >= maxSICPeakWidthMinutesBackward Then
                            blnLeftDone = True
                        End If

                    End If

                Catch ex As Exception
                    ReportError("Error extending SIC to the left", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
                End Try

                Try
                    ' Extend the SIC to the right until the threshold is reached
                    If intScanIndexEnd < intFullSICDataCount - 1 AndAlso Not blnRightDone Then
                        If sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * sngMaximumIntensity OrElse
                           sngFullSICIntensities(intMZIndexWork, intScanIndexEnd) < sicPeak.BaselineNoiseStats.NoiseLevel Then
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

                        If sngPeakWidthMinutesForward >= maxSICPeakWidthMinutesForward Then
                            blnRightDone = True
                        End If
                    End If

                Catch ex As Exception
                    ReportError("Error extending SIC to the right", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
                End Try

            Loop    ' While Not LeftDone and Not RightDone

        End If

        ' Populate udtSICDetails with the data between intScanIndexStart and intScanIndexEnd
        If intScanIndexStart < 0 Then intScanIndexStart = 0
        If intScanIndexEnd >= intFullSICDataCount Then intScanIndexEnd = intFullSICDataCount - 1

        If intScanIndexEnd < intScanIndexStart Then
            ReportError("Programming error: intScanIndexEnd < intScanIndexStart", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
            intScanIndexEnd = intScanIndexStart
        End If

        Try

            ' Copy the scan index values from intFullSICScanIndices to .SICScanIndices()
            ' Copy the intensity values from sngFullSICIntensities() to .SICData()
            ' Copy the mz values from dblFullSICMasses() to .SICMasses()

            With sicDetails
                .SICScanType = clsScanList.eScanTypeConstants.SurveyScan
                .SICData.Clear()

                sicPeak.IndexObserved = 0
                For intScanIndex = intScanIndexStart To intScanIndexEnd
                    If intFullSICScanIndices(intMZIndexWork, intScanIndex) >= 0 Then
                        .AddData(scanList.SurveyScans(intFullSICScanIndices(intMZIndexWork, intScanIndex)).ScanNumber,
                                 sngFullSICIntensities(intMZIndexWork, intScanIndex),
                                 dblFullSICMasses(intMZIndexWork, intScanIndex),
                                 intFullSICScanIndices(intMZIndexWork, intScanIndex))

                        If intScanIndex = intScanIndexObservedInFullSIC Then
                            sicPeak.IndexObserved = .SICDataCount - 1
                        End If
                    Else
                        ' This shouldn't happen
                    End If
                Next
            End With

        Catch ex As Exception
            ReportError("Error populating .SICScanIndices, .SICData, and .SICMasses", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
        End Try

        Return True

    End Function

    Private Function ProcessMZList(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      masicOptions As clsMASICOptions,
      dataOutputHandler As clsDataOutput,
      xmlResultsWriter As clsXMLResultsWriter,
      mzBinList As IReadOnlyList(Of clsMzBinInfo),
      blnProcessSIMScans As Boolean,
      intSIMIndex As Integer,
      ByRef intParentIonsProcessed As Integer) As Boolean

        ' Step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance
        ' Note that mzBinList and intParentIonIndices() are parallel arrays, with mzBinList() sorted on ascending m/z
        Const MAX_RAW_DATA_MEMORY_USAGE_MB = 50

        Dim intMaxMZCountInChunk As Integer

        Dim mzSearchChunks = New List(Of clsMzSearchInfo)

        Dim blnParentIonUpdated() As Boolean

        Try
            ' Determine the maximum number of m/z values to process simultaneously
            ' Limit the total memory usage to ~50 MB
            ' Each m/z value will require 12 bytes per scan

            If scanList.SurveyScans.Count > 0 Then
                intMaxMZCountInChunk = CInt((MAX_RAW_DATA_MEMORY_USAGE_MB * 1024 * 1024) / (scanList.SurveyScans.Count * 12))
            Else
                intMaxMZCountInChunk = 1
            End If

            If intMaxMZCountInChunk > mzBinList.Count Then
                intMaxMZCountInChunk = mzBinList.Count
            End If
            If intMaxMZCountInChunk < 1 Then intMaxMZCountInChunk = 1

            ' Reserve room in blnParentIonUpdated
            ReDim blnParentIonUpdated(mzBinList.Count - 1)

        Catch ex As Exception
            ReportError("Error reserving memory for the m/z chunks", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
            Return False
        End Try

        Try

            Dim dataAggregation = New clsDataAggregation()
            RegisterEvents(dataAggregation)

            Dim scanNumScanConverter As New clsScanNumScanTimeConversion()
            RegisterEvents(scanNumScanConverter)

            Dim parentIonIndices = (From item In mzBinList Select item.ParentIonIndex).ToList()

            Dim intMZIndex = 0
            Do While intMZIndex < mzBinList.Count

                '---------------------------------------------------------
                ' Find the next group of m/z values to use, starting with intMZIndex
                '---------------------------------------------------------

                ' Initially set the MZIndexStart to intMZIndex

                ' Look for adjacent m/z values within udtMZBinList(.MZIndexStart).MZToleranceDa / 2
                '  of the m/z value that starts this group
                ' Only group m/z values with the same udtMZBinList().MZTolerance and udtMZBinList().MZToleranceIsPPM values

                Dim mzSearchChunk = New clsMzSearchInfo() With {
                    .MZIndexStart = intMZIndex,
                    .MZTolerance = mzBinList(intMZIndex).MZTolerance,
                    .MZToleranceIsPPM = mzBinList(intMZIndex).MZToleranceIsPPM
                }

                Dim dblMZToleranceDa As Double
                If mzSearchChunk.MZToleranceIsPPM Then
                    dblMZToleranceDa = clsUtilities.PPMToMass(mzSearchChunk.MZTolerance, mzBinList(mzSearchChunk.MZIndexStart).MZ)
                Else
                    dblMZToleranceDa = mzSearchChunk.MZTolerance
                End If

                Do While intMZIndex < mzBinList.Count - 2 AndAlso
                     Math.Abs(mzBinList(intMZIndex + 1).MZTolerance - mzSearchChunk.MZTolerance) < Double.Epsilon AndAlso
                         mzBinList(intMZIndex + 1).MZToleranceIsPPM = mzSearchChunk.MZToleranceIsPPM AndAlso
                         mzBinList(intMZIndex + 1).MZ - mzBinList(mzSearchChunk.MZIndexStart).MZ <= dblMZToleranceDa / 2
                    intMZIndex += 1
                Loop
                mzSearchChunk.MZIndexEnd = intMZIndex

                If mzSearchChunk.MZIndexEnd = mzSearchChunk.MZIndexStart Then
                    mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexEnd
                    mzSearchChunk.SearchMZ = mzBinList(mzSearchChunk.MZIndexStart).MZ
                Else
                    ' Determine the median m/z of the members in the m/z group
                    If (mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) Mod 2 = 0 Then
                        ' Odd number of points; use the m/z value of the midpoint
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexStart + CInt((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) / 2)
                        mzSearchChunk.SearchMZ = mzBinList(mzSearchChunk.MZIndexMidpoint).MZ
                    Else
                        ' Even number of points; average the values on either side of (.mzIndexEnd - .mzIndexStart / 2)
                        mzSearchChunk.MZIndexMidpoint = mzSearchChunk.MZIndexStart + CInt(Math.Floor((mzSearchChunk.MZIndexEnd - mzSearchChunk.MZIndexStart) / 2))
                        mzSearchChunk.SearchMZ = (mzBinList(mzSearchChunk.MZIndexMidpoint).MZ + mzBinList(mzSearchChunk.MZIndexMidpoint + 1).MZ) / 2
                    End If
                End If

                mzSearchChunks.Add(mzSearchChunk)

                If mzSearchChunks.Count >= intMaxMZCountInChunk OrElse intMZIndex = mzBinList.Count - 1 Then

                    '---------------------------------------------------------
                    ' Reached intMaxMZCountInChunk m/z value
                    ' Process all of the m/z values in udtMZSearchChunk
                    '---------------------------------------------------------

                    Dim blnSuccess = ProcessMzSearchChunk(
                        masicOptions,
                        scanList,
                        dataAggregation, dataOutputHandler, xmlResultsWriter,
                        objSpectraCache, scanNumScanConverter,
                        mzSearchChunks,
                        parentIonIndices,
                        blnProcessSIMScans,
                        intSIMIndex,
                        blnParentIonUpdated,
                        intParentIonsProcessed)

                    If Not blnSuccess Then
                        Return False
                    End If

                    ' Clear mzSearchChunks
                    mzSearchChunks.Clear()

                End If

                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Exit Do
                End If

                intMZIndex += 1
            Loop

            Return True
        Catch ex As Exception
            ReportError("Error processing the m/z chunks to create the SIC data", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
            Return False
        End Try

    End Function

    Private Function ProcessMzSearchChunk(
      masicOptions As clsMASICOptions,
      scanList As clsScanList,
      dataAggregation As clsDataAggregation,
      dataOutputHandler As clsDataOutput,
      xmlResultsWriter As clsXMLResultsWriter,
      objSpectraCache As clsSpectraCache,
      scanNumScanConverter As clsScanNumScanTimeConversion,
      mzSearchChunk As IReadOnlyList(Of clsMzSearchInfo),
      parentIonIndices As IList(Of Integer),
      blnProcessSIMScans As Boolean,
      intSIMIndex As Integer,
      blnParentIonUpdated As IList(Of Boolean),
      ByRef intParentIonsProcessed As Integer) As Boolean

        ' The following are 2D arrays, ranging from 0 to intMZSearchChunkCount-1 in the first dimension and 0 to .SurveyScans.Count - 1 in the second dimension
        ' We could have included these in udtMZSearchChunk but memory management is more efficient if I use 2D arrays for this data
        Dim intFullSICScanIndices(,) As Integer     ' Pointer into .SurveyScans
        Dim sngFullSICIntensities(,) As Single
        Dim dblFullSICMasses(,) As Double
        Dim intFullSICDataCount() As Integer        ' Count of the number of valid entries in the second dimension of the above 3 arrays

        ' The following is a 1D array, containing the SIC intensities for a single m/z group
        Dim sngFullSICIntensities1D() As Single

        ' Reserve room in intFullSICScanIndices for at most intMaxMZCountInChunk values and .SurveyScans.Count scans
        ReDim intFullSICDataCount(mzSearchChunk.Count - 1)
        ReDim intFullSICScanIndices(mzSearchChunk.Count - 1, scanList.SurveyScans.Count - 1)
        ReDim sngFullSICIntensities(mzSearchChunk.Count - 1, scanList.SurveyScans.Count - 1)
        ReDim dblFullSICMasses(mzSearchChunk.Count - 1, scanList.SurveyScans.Count - 1)

        ReDim sngFullSICIntensities1D(scanList.SurveyScans.Count - 1)


        ' Initialize .MaximumIntensity and .ScanIndexMax
        ' Additionally, reset intFullSICDataCount() and, for safety, set intFullSICScanIndices() to -1
        For mzIndexWork = 0 To mzSearchChunk.Count - 1
            mzSearchChunk(mzIndexWork).ResetMaxIntensity()

            intFullSICDataCount(mzIndexWork) = 0
            For intSurveyScanIndex = 0 To scanList.SurveyScans.Count - 1
                intFullSICScanIndices(mzIndexWork, intSurveyScanIndex) = -1
            Next
        Next

        '---------------------------------------------------------
        ' Step through scanList to obtain the scan numbers and intensity data for each .SearchMZ in udtMZSearchChunk
        ' We're stepping scan by scan since the process of loading a scan from disk is slower than the process of searching for each m/z in the scan
        '---------------------------------------------------------
        For intSurveyScanIndex = 0 To scanList.SurveyScans.Count - 1

            Dim blnUseScan As Boolean

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

            If Not blnUseScan Then
                Continue For
            End If

            Dim intPoolIndex As Integer

            If Not objSpectraCache.ValidateSpectrumInPool(scanList.SurveyScans(intSurveyScanIndex).ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum)
                Return False
            End If

            For mzIndexWork = 0 To mzSearchChunk.Count - 1

                Dim current = mzSearchChunk(mzIndexWork)

                Dim dblMZToleranceDa As Double

                If current.MZToleranceIsPPM Then
                    dblMZToleranceDa = clsUtilities.PPMToMass(current.MZTolerance, current.SearchMZ)
                Else
                    dblMZToleranceDa = current.MZTolerance
                End If

                Dim intIonMatchCount As Integer
                Dim dblClosestMZ As Double

                Dim sngIonSum = dataAggregation.AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex),
                                                                         current.SearchMZ, dblMZToleranceDa,
                                                                         intIonMatchCount, dblClosestMZ, False)

                Dim intDataIndex = intFullSICDataCount(mzIndexWork)
                intFullSICScanIndices(mzIndexWork, intDataIndex) = intSurveyScanIndex
                sngFullSICIntensities(mzIndexWork, intDataIndex) = sngIonSum

                If sngIonSum < Single.Epsilon AndAlso masicOptions.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData Then
                    sngFullSICIntensities(mzIndexWork, intDataIndex) = scanList.SurveyScans(intSurveyScanIndex).MinimumPositiveIntensity
                End If

                dblFullSICMasses(mzIndexWork, intDataIndex) = dblClosestMZ
                If sngIonSum > current.MaximumIntensity Then
                    current.MaximumIntensity = sngIonSum
                    current.ScanIndexMax = intDataIndex
                End If

                intFullSICDataCount(mzIndexWork) += 1

            Next

            If intSurveyScanIndex Mod 100 = 0 Then
                UpdateProgress("Loading raw SIC data: " & intSurveyScanIndex & " / " & scanList.SurveyScans.Count)
                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Return False
                End If
            End If
        Next

        UpdateProgress("Creating SIC's for the parent ions")

        If masicOptions.AbortProcessing Then
            scanList.ProcessingIncomplete = True
            Return False
        End If

        Const DebugParentIonIndexToFind = 3139
        Const DebugMZToFind As Single = 488.47

        '---------------------------------------------------------
        ' Compute the noise level in sngFullSICIntensities() for each m/z in udtMZSearchChunk
        ' Also, find the peaks for each m/z in udtMZSearchChunk and retain the largest peak found
        '---------------------------------------------------------
        For intMZIndexWork = 0 To mzSearchChunk.Count - 1

            ' Use this for debugging
            If Math.Abs(mzSearchChunk(intMZIndexWork).SearchMZ - DebugMZToFind) < 0.1 Then
                Dim intParentIonIndexPointer = mzSearchChunk(intMZIndexWork).MZIndexStart
            End If

            ' Copy the data for this m/z into sngFullSICIntensities1D()
            For intDataIndex = 0 To intFullSICDataCount(intMZIndexWork) - 1
                sngFullSICIntensities1D(intDataIndex) = sngFullSICIntensities(intMZIndexWork, intDataIndex)
            Next

            ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
            Dim noiseStatSegments As List(Of clsBaselineNoiseStatsSegment) = Nothing

            Dim blnSuccess = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(
                sngFullSICIntensities1D, 0, intFullSICDataCount(intMZIndexWork) - 1,
                masicOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions,
                noiseStatSegments)

            If Not blnSuccess Then
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.FindSICPeaksError, True)
                Return False
            End If

            mzSearchChunk(intMZIndexWork).BaselineNoiseStatSegments = noiseStatSegments

            Dim potentialAreaStatsInFullSIC As clsSICPotentialAreaStats = Nothing

            ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
            mMASICPeakFinder.FindPotentialPeakArea(
                intFullSICDataCount(intMZIndexWork),
                sngFullSICIntensities1D,
                potentialAreaStatsInFullSIC,
                masicOptions.SICOptions.SICPeakFinderOptions)

            Dim potentialAreaStatsForPeak As clsSICPotentialAreaStats = Nothing

            Dim intScanIndexObservedInFullSIC = mzSearchChunk(intMZIndexWork).ScanIndexMax

            ' Initialize sicDetails
            Dim sicDetails = New clsSICDetails() With {
                .SICScanType = clsScanList.eScanTypeConstants.SurveyScan
            }

            Dim sicPeak As clsSICStatsPeak = Nothing

            ' Populate sicDetails using the data centered around the highest intensity in intFullSICIntensities
            ' Note that this function will update udtSICPeak.IndexObserved
            blnSuccess = ExtractSICDetailsFromFullSIC(
                intMZIndexWork, mzSearchChunk(intMZIndexWork).BaselineNoiseStatSegments,
                intFullSICDataCount(intMZIndexWork), intFullSICScanIndices, sngFullSICIntensities, dblFullSICMasses,
                scanList, intScanIndexObservedInFullSIC,
                sicDetails, sicPeak,
                masicOptions, scanNumScanConverter, False, 0)

            Dim smoothedYDataSubset As clsSmoothedYDataSubset = Nothing

            Dim mzIndexSICScanNumbers = sicDetails.SICScanNumbers
            Dim mzIndexSICIntensities = sicDetails.SICIntensities
            Dim mzIndexSICIndices = sicDetails.SICScanIndices

            ' Find the largest peak in the SIC for this m/z
            Dim blnLargestPeakFound = mMASICPeakFinder.FindSICPeakAndArea(
                sicDetails.SICDataCount, mzIndexSICScanNumbers, mzIndexSICIntensities,
                potentialAreaStatsForPeak, sicPeak,
                smoothedYDataSubset, masicOptions.SICOptions.SICPeakFinderOptions,
                potentialAreaStatsInFullSIC,
                True, scanList.SIMDataPresent, False)

            If blnLargestPeakFound Then
                '--------------------------------------------------------
                ' Step through the parent ions and see if .SurveyScanIndex is contained in udtSICPeak
                ' If it is, then assign the stats of the largest peak to the given parent ion
                '--------------------------------------------------------

                For intParentIonIndexPointer = mzSearchChunk(intMZIndexWork).MZIndexStart To mzSearchChunk(intMZIndexWork).MZIndexEnd
                    ' Use this for debugging
                    If parentIonIndices(intParentIonIndexPointer) = DebugParentIonIndexToFind Then
                        intScanIndexObservedInFullSIC = -1
                    End If

                    Dim blnStorePeakInParentIon = False
                    If scanList.ParentIons(parentIonIndices(intParentIonIndexPointer)).CustomSICPeak Then Continue For

                    ' Assign the stats of the largest peak to each parent ion with .SurveyScanIndex contained in the peak
                    With scanList.ParentIons(parentIonIndices(intParentIonIndexPointer))
                        If .SurveyScanIndex >= mzIndexSICIndices(sicPeak.IndexBaseLeft) AndAlso
                           .SurveyScanIndex <= mzIndexSICIndices(sicPeak.IndexBaseRight) Then

                            blnStorePeakInParentIon = True
                        End If
                    End With

                    If blnStorePeakInParentIon Then
                        blnSuccess = StorePeakInParentIon(scanList, parentIonIndices(intParentIonIndexPointer),
                                                          sicDetails, mzIndexSICScanNumbers, mzIndexSICIntensities, mzIndexSICIndices,
                                                          potentialAreaStatsForPeak, sicPeak, True)

                        ' Possibly save the stats for this SIC to the SICData file
                        dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                                                            parentIonIndices(intParentIonIndexPointer), sicDetails)

                        ' Save the stats for this SIC to the XML file
                        xmlResultsWriter.SaveDataToXML(scanList,
                                                       parentIonIndices(intParentIonIndexPointer), sicDetails,
                                                       smoothedYDataSubset, dataOutputHandler)

                        blnParentIonUpdated(intParentIonIndexPointer) = True
                        intParentIonsProcessed += 1

                    End If


                Next
            End If

            '--------------------------------------------------------
            ' Now step through the parent ions and process those that were not updated using udtSICPeak
            ' For each, search for the closest peak in sngSICIntensity
            '--------------------------------------------------------
            For intParentIonIndexPointer = mzSearchChunk(intMZIndexWork).MZIndexStart To mzSearchChunk(intMZIndexWork).MZIndexEnd

                If blnParentIonUpdated(intParentIonIndexPointer) Then Continue For

                If parentIonIndices(intParentIonIndexPointer) = DebugParentIonIndexToFind Then
                    intScanIndexObservedInFullSIC = -1
                End If

                Dim smoothedYDataSubsetInSearchChunk As clsSmoothedYDataSubset = Nothing

                With scanList.ParentIons(parentIonIndices(intParentIonIndexPointer))
                    ' Clear udtSICPotentialAreaStatsForPeak
                    .SICStats.SICPotentialAreaStatsForPeak = New clsSICPotentialAreaStats()

                    ' Record the index in the Full SIC that the parent ion mass was first observed
                    ' Search for .SurveyScanIndex in intFullSICScanIndices
                    intScanIndexObservedInFullSIC = -1
                    For intDataIndex = 0 To intFullSICDataCount(intMZIndexWork) - 1
                        If intFullSICScanIndices(intMZIndexWork, intDataIndex) >= .SurveyScanIndex Then
                            intScanIndexObservedInFullSIC = intDataIndex
                            Exit For
                        End If
                    Next

                    If intScanIndexObservedInFullSIC = -1 Then
                        ' Match wasn't found; this is unexpected
                        ReportError("Programming error: survey scan index not found in intFullSICScanIndices()", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                        intScanIndexObservedInFullSIC = 0
                    End If

                    ' Populate udtSICDetails using the data centered around intScanIndexObservedInFullSIC
                    ' Note that this function will update udtSICPeak.IndexObserved
                    blnSuccess = ExtractSICDetailsFromFullSIC(
                        intMZIndexWork, mzSearchChunk(intMZIndexWork).BaselineNoiseStatSegments,
                        intFullSICDataCount(intMZIndexWork), intFullSICScanIndices, sngFullSICIntensities, dblFullSICMasses,
                        scanList, intScanIndexObservedInFullSIC,
                        sicDetails, .SICStats.Peak,
                        masicOptions, scanNumScanConverter,
                        .CustomSICPeak, .CustomSICPeakScanOrAcqTimeTolerance)


                    Dim sicScanNumbers = sicDetails.SICScanNumbers
                    Dim sicIntensities = sicDetails.SICIntensities
                    Dim sicIndices = sicDetails.SICScanIndices

                    blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(
                        sicDetails.SICDataCount, sicScanNumbers, sicIntensities,
                        .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak,
                        smoothedYDataSubsetInSearchChunk, masicOptions.SICOptions.SICPeakFinderOptions,
                        potentialAreaStatsInFullSIC,
                        Not .CustomSICPeak, scanList.SIMDataPresent, False)


                    blnSuccess = StorePeakInParentIon(scanList, parentIonIndices(intParentIonIndexPointer),
                                                      sicDetails, sicScanNumbers, sicIntensities, sicIndices,
                                                      .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak, blnSuccess)
                End With

                ' Possibly save the stats for this SIC to the SICData file
                dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                                                    parentIonIndices(intParentIonIndexPointer), sicDetails)

                ' Save the stats for this SIC to the XML file
                xmlResultsWriter.SaveDataToXML(scanList,
                                               parentIonIndices(intParentIonIndexPointer), sicDetails,
                                               smoothedYDataSubsetInSearchChunk, dataOutputHandler)

                blnParentIonUpdated(intParentIonIndexPointer) = True
                intParentIonsProcessed += 1

            Next

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
                        ReportMessage("Parent Ions Processed: " & intParentIonsProcessed.ToString())
                        Console.Write(".")
                        masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow
                    End If
                End If

            Catch ex As Exception
                ReportError("Error updating progress", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
            End Try

        Next

        Return True

    End Function

    Public Function StorePeakInParentIon(
      scanList As clsScanList,
      intParentIonIndex As Integer,
      sicDetails As clsSICDetails,
      sicScanNumbers As Integer(),
      sicIntensities As Single(),
      sicScanIndices As Integer(),
      potentialAreaStatsForPeak As clsSICPotentialAreaStats,
      sicPeak As clsSICStatsPeak,
      blnPeakIsValid As Boolean) As Boolean


        Dim intDataIndex As Integer
        Dim intScanIndexObserved As Integer
        Dim intFragScanNumber As Integer

        Dim blnProcessingMRMPeak As Boolean
        Dim blnSuccess As Boolean

        Try

            Dim sicDataCount = sicDetails.SICDataCount
            If sicDataCount = 0 Then
                ' Either .SICData is nothing or no SIC data exists
                ' Cannot find peaks for this parent ion
                With scanList.ParentIons(intParentIonIndex).SICStats
                    With .Peak
                        .IndexObserved = 0
                        .IndexBaseLeft = .IndexObserved
                        .IndexBaseRight = .IndexObserved
                        .IndexMax = .IndexObserved
                    End With
                End With

                Return True
            End If

            If sicDataCount <> sicScanNumbers.Count OrElse
               sicDataCount <> sicIntensities.Count OrElse
               sicDataCount <> sicScanIndices.Count Then
                Throw New Exception("SIC Data arrays have conflicting lengths; likely a code bug")
            End If

            With scanList.ParentIons(intParentIonIndex)
                intScanIndexObserved = .SurveyScanIndex
                If intScanIndexObserved < 0 Then intScanIndexObserved = 0

                If .MRMDaughterMZ > 0 Then
                    blnProcessingMRMPeak = True
                Else
                    blnProcessingMRMPeak = False
                End If

                With .SICStats

                    .SICPotentialAreaStatsForPeak = potentialAreaStatsForPeak
                    .Peak = sicPeak

                    .ScanTypeForPeakIndices = sicDetails.SICScanType
                    If blnProcessingMRMPeak Then
                        If .ScanTypeForPeakIndices <> clsScanList.eScanTypeConstants.FragScan Then
                            ' ScanType is not FragScan; this is unexpected
                            ReportError("Programming error: udtSICDetails.SICScanType is not FragScan even though we're processing an MRM peak", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                            .ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan
                        End If
                    End If

                    If blnProcessingMRMPeak Then
                        .Peak.IndexObserved = 0
                    Else
                        ' Record the index (of data in .SICData) that the parent ion mass was first observed
                        ' This is not necessarily the same as udtSICPeak.IndexObserved, so we need to search for it here

                        ' Search for intScanIndexObserved in sicScanIndices()
                        .Peak.IndexObserved = -1
                        For intDataIndex = 0 To sicDetails.SICDataCount - 1
                            If sicScanIndices(intDataIndex) = intScanIndexObserved Then
                                .Peak.IndexObserved = intDataIndex
                                Exit For
                            End If
                        Next

                        If .Peak.IndexObserved = -1 Then
                            ' Match wasn't found; this is unexpected
                            ReportError("Programming error: survey scan index not found in sicScanIndices", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
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
                        sicPeak.ParentIonIntensity = 0
                    Else
                        ' Determine the value for .ParentIonIntensity
                        blnSuccess = mMASICPeakFinder.ComputeParentIonIntensity(
                            sicScanNumbers.Length,
                            sicScanNumbers,
                            sicIntensities,
                            .Peak,
                            intFragScanNumber)
                    End If

                    If blnPeakIsValid Then
                        ' Record the survey scan indices of the peak max, start, and end
                        ' Note that .ScanTypeForPeakIndices was set earlier in this function
                        .PeakScanIndexMax = sicScanIndices(.Peak.IndexMax)
                        .PeakScanIndexStart = sicScanIndices(.Peak.IndexBaseLeft)
                        .PeakScanIndexEnd = sicScanIndices(.Peak.IndexBaseRight)
                    Else
                        ' No peak found
                        .PeakScanIndexMax = sicScanIndices(.Peak.IndexMax)
                        .PeakScanIndexStart = .PeakScanIndexMax
                        .PeakScanIndexEnd = .PeakScanIndexMax

                        With .Peak
                            .MaxIntensityValue = sicIntensities(.IndexMax)
                            .IndexBaseLeft = .IndexMax
                            .IndexBaseRight = .IndexMax
                            .FWHMScanWidth = 1
                            ' Assign the intensity of the peak at the observed maximum to the area
                            .Area = .MaxIntensityValue

                            .SignalToNoiseRatio = clsMASICPeakFinder.ComputeSignalToNoise(.MaxIntensityValue, .BaselineNoiseStats.NoiseLevel)
                        End With
                    End If
                End With
            End With

            ' Update .OptimalPeakApexScanNumber
            ' Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
            With scanList.ParentIons(intParentIonIndex)
                If blnProcessingMRMPeak Then
                    .OptimalPeakApexScanNumber = scanList.FragScans(sicScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
                Else
                    .OptimalPeakApexScanNumber = scanList.SurveyScans(sicScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
                End If

            End With

            blnSuccess = True

        Catch ex As Exception

            ReportError("Error finding SIC peaks and their areas", ex, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
            blnSuccess = False

        End Try

        Return blnSuccess

    End Function

End Class
