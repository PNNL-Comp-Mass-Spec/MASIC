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
      processSIMScans As Boolean,
      simIndex As Integer) As List(Of clsMzBinInfo)

        Dim parentIonIndex As Integer

        Dim includeParentIon As Boolean

        Dim mzBinList = New List(Of clsMzBinInfo)(scanList.ParentIons.Count - 1)

        Dim sicOptions = masicOptions.SICOptions

        For parentIonIndex = 0 To scanList.ParentIons.Count - 1

            If scanList.ParentIons(parentIonIndex).MRMDaughterMZ > 0 Then
                includeParentIon = False
            Else
                If masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                    ' Always include CustomSICPeak entries
                    includeParentIon = scanList.ParentIons(parentIonIndex).CustomSICPeak
                Else
                    ' Use processingSIMScans and .SIMScan to decide whether or not to include the entry
                    With scanList.SurveyScans(scanList.ParentIons(parentIonIndex).SurveyScanIndex)
                        If processSIMScans Then
                            If .SIMScan Then
                                If .SIMIndex = simIndex Then
                                    includeParentIon = True
                                Else
                                    includeParentIon = False
                                End If
                            Else
                                includeParentIon = False
                            End If
                        Else
                            includeParentIon = Not .SIMScan
                        End If
                    End With
                End If
            End If

            If includeParentIon Then
                Dim newMzBin = New clsMzBinInfo() With {
                    .MZ = scanList.ParentIons(parentIonIndex).MZ,
                    .ParentIonIndex = parentIonIndex
                }

                If scanList.ParentIons(parentIonIndex).CustomSICPeak Then
                    newMzBin.MZTolerance = scanList.ParentIons(parentIonIndex).CustomSICPeakMZToleranceDa
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

        Dim success As Boolean
        Dim parentIonIndex As Integer
        Dim parentIonsProcessed As Integer

        If scanList.ParentIons.Count <= 0 Then
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
            parentIonsProcessed = 0
            masicOptions.LastParentIonProcessingLogTime = DateTime.UtcNow

            UpdateProgress(0, "Creating SIC's for the parent ions")
            Console.Write("Creating SIC's for parent ions ")
            ReportMessage("Creating SIC's for parent ions")

            ' Create an array of m/z values in scanList.ParentIons, then sort by m/z
            ' Next, step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance

            Dim simIndex As Integer
            Dim simIndexMax As Integer

            ' First process the non SIM, non MRM scans
            ' If this file only has MRM scans, then CreateMZLookupList will return False
            Dim mzBinList = CreateMZLookupList(masicOptions, scanList, False, 0)
            If mzBinList.Count > 0 Then

                success = ProcessMZList(scanList, objSpectraCache, masicOptions,
                                        dataOutputHandler, xmlResultsWriter,
                                        mzBinList, False, 0, parentIonsProcessed)
            End If

            If success And Not masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                ' Now process the SIM scans (if any)
                ' First, see if any SIMScans are present and determine the maximum SIM Index
                simIndexMax = -1
                For parentIonIndex = 0 To scanList.ParentIons.Count - 1
                    With scanList.SurveyScans(scanList.ParentIons(parentIonIndex).SurveyScanIndex)
                        If .SIMScan Then
                            If .SIMIndex > simIndexMax Then
                                simIndexMax = .SIMIndex
                            End If
                        End If
                    End With
                Next

                ' Now process each SIM Scan type
                For simIndex = 0 To simIndexMax

                    Dim mzBinListSIM = CreateMZLookupList(masicOptions, scanList, True, simIndex)

                    If mzBinListSIM.Count > 0 Then

                        ProcessMZList(scanList, objSpectraCache, masicOptions,
                                                   dataOutputHandler, xmlResultsWriter,
                                                   mzBinListSIM, True, simIndex, parentIonsProcessed)
                    End If
                Next
            End If

            ' Lastly, process the MRM scans (if any)
            If scanList.MRMDataPresent Then
                mMRMProcessor.ProcessMRMList(scanList, objSpectraCache, sicProcessor, xmlResultsWriter, mMASICPeakFinder, parentIonsProcessed)
            End If

            Console.WriteLine()
            Return True

        Catch ex As Exception
            ReportError("Error creating Parent Ion SICs", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
            Return False
        End Try

    End Function

    Private Function ExtractSICDetailsFromFullSIC(
      mzIndexWork As Integer,
      baselineNoiseStatSegments As List(Of clsBaselineNoiseStatsSegment),
      fullSICDataCount As Integer,
      fullSICScanIndices(,) As Integer,
      fullSICIntensities(,) As Double,
      fullSICMasses(,) As Double,
      scanList As clsScanList,
      scanIndexObservedInFullSIC As Integer,
      sicDetails As clsSICDetails,
      <Out> ByRef sicPeak As clsSICStatsPeak,
      masicOptions As clsMASICOptions,
      scanNumScanConverter As clsScanNumScanTimeConversion,
      customSICPeak As Boolean,
      customSICPeakScanOrAcqTimeTolerance As Single) As Boolean

        ' Minimum number of scans to extend left or right of the scan that meets the minimum intensity threshold requirement
        Const MINIMUM_NOISE_SCANS_TO_INCLUDE = 10

        Dim customSICScanToleranceMinutesHalfWidth As Single

        ' Pointers to entries in fullSICScanIndices() and fullSICIntensities()
        Dim scanIndexStart As Integer, scanIndexEnd As Integer

        Dim maximumIntensity As Double

        Dim sicOptions = masicOptions.SICOptions

        Dim baselineNoiseStats = mMASICPeakFinder.LookupNoiseStatsUsingSegments(scanIndexObservedInFullSIC, baselineNoiseStatSegments)

        ' Initialize the peak
        sicPeak = New clsSICStatsPeak() With {
            .BaselineNoiseStats = baselineNoiseStats
        }

        ' Initialize the values for the maximum width of the SIC peak; these might get altered for custom SIC values
        Dim maxSICPeakWidthMinutesBackward = sicOptions.MaxSICPeakWidthMinutesBackward
        Dim maxSICPeakWidthMinutesForward = sicOptions.MaxSICPeakWidthMinutesForward

        ' Limit the data examined to a portion of fullSICScanIndices() and fullSICIntensities, populating udtSICDetails
        Try

            ' Initialize customSICScanToleranceHalfWidth
            With masicOptions.CustomSICList
                If customSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    customSICPeakScanOrAcqTimeTolerance = .ScanOrAcqTimeTolerance
                End If

                If customSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    ' Use the entire SIC
                    ' Specify this by setting customSICScanToleranceMinutesHalfWidth to the maximum scan time in .MasterScanTimeList()
                    With scanList
                        If .MasterScanOrderCount > 0 Then
                            customSICScanToleranceMinutesHalfWidth = .MasterScanTimeList(.MasterScanOrderCount - 1)
                        Else
                            customSICScanToleranceMinutesHalfWidth = Single.MaxValue
                        End If
                    End With
                Else
                    If .ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.Relative AndAlso customSICPeakScanOrAcqTimeTolerance > 10 Then
                        ' Relative scan time should only range from 0 to 1; we'll allow values up to 10
                        customSICPeakScanOrAcqTimeTolerance = 10
                    End If

                    customSICScanToleranceMinutesHalfWidth = scanNumScanConverter.ScanOrAcqTimeToScanTime(
                        scanList, customSICPeakScanOrAcqTimeTolerance / 2, .ScanToleranceType, True)
                End If

                If customSICPeak Then
                    If maxSICPeakWidthMinutesBackward < customSICScanToleranceMinutesHalfWidth Then
                        maxSICPeakWidthMinutesBackward = customSICScanToleranceMinutesHalfWidth
                    End If

                    If maxSICPeakWidthMinutesForward < customSICScanToleranceMinutesHalfWidth Then
                        maxSICPeakWidthMinutesForward = customSICScanToleranceMinutesHalfWidth
                    End If
                End If
            End With

            ' Initially use just 3 survey scans, centered around scanIndexObservedInFullSIC
            If scanIndexObservedInFullSIC > 0 Then
                scanIndexStart = scanIndexObservedInFullSIC - 1
                scanIndexEnd = scanIndexObservedInFullSIC + 1
            Else
                scanIndexStart = 0
                scanIndexEnd = 1
                scanIndexObservedInFullSIC = 0
            End If

            If scanIndexEnd >= fullSICDataCount Then scanIndexEnd = fullSICDataCount - 1

        Catch ex As Exception
            ReportError("Error initializing SIC start/end indices", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
        End Try

        If scanIndexEnd >= scanIndexStart Then

            Dim scanIndexMax As Integer

            Try
                ' Start by using the 3 survey scans centered around scanIndexObservedInFullSIC
                maximumIntensity = -1
                scanIndexMax = -1
                For scanIndex = scanIndexStart To scanIndexEnd
                    If fullSICIntensities(mzIndexWork, scanIndex) > maximumIntensity Then
                        maximumIntensity = fullSICIntensities(mzIndexWork, scanIndex)
                        scanIndexMax = scanIndex
                    End If
                Next
            Catch ex As Exception
                ReportError("Error while creating initial SIC", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
            End Try

            ' Now extend the SIC, stepping left and right until a threshold is reached
            Dim leftDone = False
            Dim rightDone = False

            ' The index of the first scan found to be below threshold (on the left)
            Dim scanIndexBelowThresholdLeft = -1

            ' The index of the first scan found to be below threshold (on the right)
            Dim scanIndexBelowThresholdRight = -1


            Do While (scanIndexStart > 0 AndAlso Not leftDone) OrElse (scanIndexEnd < fullSICDataCount - 1 AndAlso Not rightDone)
                Try
                    ' Extend the SIC to the left until the threshold is reached
                    If scanIndexStart > 0 AndAlso Not leftDone Then
                        If fullSICIntensities(mzIndexWork, scanIndexStart) < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           fullSICIntensities(mzIndexWork, scanIndexStart) < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * maximumIntensity OrElse
                           fullSICIntensities(mzIndexWork, scanIndexStart) < sicPeak.BaselineNoiseStats.NoiseLevel Then
                            If scanIndexBelowThresholdLeft < 0 Then
                                scanIndexBelowThresholdLeft = scanIndexStart
                            Else
                                If scanIndexStart <= scanIndexBelowThresholdLeft - MINIMUM_NOISE_SCANS_TO_INCLUDE Then
                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    ' Stop creating the SIC to the left
                                    leftDone = True
                                End If
                            End If
                        Else
                            scanIndexBelowThresholdLeft = -1
                        End If

                        Dim peakWidthMinutesBackward = scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexObservedInFullSIC)).ScanTime -
                           scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexStart)).ScanTime

                        If leftDone Then
                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the left of scanIndexObservedInFullSIC and to the left of scanIndexMax
                            If scanIndexObservedInFullSIC - scanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then leftDone = False
                            If scanIndexMax - scanIndexStart < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then leftDone = False

                            ' For custom SIC values, make sure the scan range has been satisfied
                            If leftDone AndAlso customSICPeak Then
                                If peakWidthMinutesBackward < customSICScanToleranceMinutesHalfWidth Then
                                    leftDone = False
                                End If
                            End If
                        End If

                        If Not leftDone Then
                            If scanIndexStart = 0 Then
                                leftDone = True
                            Else
                                scanIndexStart -= 1
                                If fullSICIntensities(mzIndexWork, scanIndexStart) > maximumIntensity Then
                                    maximumIntensity = fullSICIntensities(mzIndexWork, scanIndexStart)
                                    scanIndexMax = scanIndexStart
                                End If
                            End If
                        End If

                        peakWidthMinutesBackward = scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexObservedInFullSIC)).ScanTime -
                           scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexStart)).ScanTime

                        If peakWidthMinutesBackward >= maxSICPeakWidthMinutesBackward Then
                            leftDone = True
                        End If

                    End If

                Catch ex As Exception
                    ReportError("Error extending SIC to the left", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
                End Try

                Try
                    ' Extend the SIC to the right until the threshold is reached
                    If scanIndexEnd < fullSICDataCount - 1 AndAlso Not rightDone Then
                        If fullSICIntensities(mzIndexWork, scanIndexEnd) < sicOptions.SICPeakFinderOptions.IntensityThresholdAbsoluteMinimum OrElse
                           fullSICIntensities(mzIndexWork, scanIndexEnd) < sicOptions.SICPeakFinderOptions.IntensityThresholdFractionMax * maximumIntensity OrElse
                           fullSICIntensities(mzIndexWork, scanIndexEnd) < sicPeak.BaselineNoiseStats.NoiseLevel Then
                            If scanIndexBelowThresholdRight < 0 Then
                                scanIndexBelowThresholdRight = scanIndexEnd
                            Else
                                If scanIndexEnd >= scanIndexBelowThresholdRight + MINIMUM_NOISE_SCANS_TO_INCLUDE Then
                                    ' We have now processed MINIMUM_NOISE_SCANS_TO_INCLUDE+1 scans that are below the thresholds
                                    ' Stop creating the SIC to the right
                                    rightDone = True
                                End If
                            End If
                        Else
                            scanIndexBelowThresholdRight = -1
                        End If

                        Dim peakWidthMinutesForward = scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexEnd)).ScanTime -
                          scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexObservedInFullSIC)).ScanTime

                        If rightDone Then
                            ' Require a minimum distance of InitialPeakWidthScansMaximum data points to the right of scanIndexObservedInFullSIC and to the right of scanIndexMax
                            If scanIndexEnd - scanIndexObservedInFullSIC < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then rightDone = False
                            If scanIndexEnd - scanIndexMax < sicOptions.SICPeakFinderOptions.InitialPeakWidthScansMaximum Then rightDone = False

                            ' For custom SIC values, make sure the scan range has been satisfied
                            If rightDone AndAlso customSICPeak Then
                                If peakWidthMinutesForward < customSICScanToleranceMinutesHalfWidth Then
                                    rightDone = False
                                End If
                            End If
                        End If

                        If Not rightDone Then
                            If scanIndexEnd = fullSICDataCount - 1 Then
                                rightDone = True
                            Else
                                scanIndexEnd += 1
                                If fullSICIntensities(mzIndexWork, scanIndexEnd) > maximumIntensity Then
                                    maximumIntensity = fullSICIntensities(mzIndexWork, scanIndexEnd)
                                    scanIndexMax = scanIndexEnd
                                End If
                            End If
                        End If

                        peakWidthMinutesForward = scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexEnd)).ScanTime -
                          scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndexObservedInFullSIC)).ScanTime

                        If peakWidthMinutesForward >= maxSICPeakWidthMinutesForward Then
                            rightDone = True
                        End If
                    End If

                Catch ex As Exception
                    ReportError("Error extending SIC to the right", ex, clsMASIC.eMasicErrorCodes.CreateSICsError)
                End Try

            Loop    ' While Not LeftDone and Not RightDone

        End If

        ' Populate udtSICDetails with the data between scanIndexStart and scanIndexEnd
        If scanIndexStart < 0 Then scanIndexStart = 0
        If scanIndexEnd >= fullSICDataCount Then scanIndexEnd = fullSICDataCount - 1

        If scanIndexEnd < scanIndexStart Then
            ReportError("Programming error: scanIndexEnd < scanIndexStart", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
            scanIndexEnd = scanIndexStart
        End If

        Try

            ' Copy the scan index values from fullSICScanIndices to .SICScanIndices()
            ' Copy the intensity values from fullSICIntensities() to .SICData()
            ' Copy the mz values from fullSICMasses() to .SICMasses()

            With sicDetails
                .SICScanType = clsScanList.eScanTypeConstants.SurveyScan
                .SICData.Clear()

                sicPeak.IndexObserved = 0
                For scanIndex = scanIndexStart To scanIndexEnd
                    If fullSICScanIndices(mzIndexWork, scanIndex) >= 0 Then
                        .AddData(scanList.SurveyScans(fullSICScanIndices(mzIndexWork, scanIndex)).ScanNumber,
                                 fullSICIntensities(mzIndexWork, scanIndex),
                                 fullSICMasses(mzIndexWork, scanIndex),
                                 fullSICScanIndices(mzIndexWork, scanIndex))

                        If scanIndex = scanIndexObservedInFullSIC Then
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
      processSIMScans As Boolean,
      simIndex As Integer,
      ByRef parentIonsProcessed As Integer) As Boolean

        ' Step through the data in order of m/z, creating SICs for each grouping of m/z's within half of the SIC tolerance
        ' Note that mzBinList and parentIonIndices() are parallel arrays, with mzBinList() sorted on ascending m/z
        Const MAX_RAW_DATA_MEMORY_USAGE_MB = 50

        Dim maxMZCountInChunk As Integer

        Dim mzSearchChunks = New List(Of clsMzSearchInfo)

        Dim parentIonUpdated() As Boolean

        Try
            ' Determine the maximum number of m/z values to process simultaneously
            ' Limit the total memory usage to ~50 MB
            ' Each m/z value will require 12 bytes per scan

            If scanList.SurveyScans.Count > 0 Then
                maxMZCountInChunk = CInt((MAX_RAW_DATA_MEMORY_USAGE_MB * 1024 * 1024) / (scanList.SurveyScans.Count * 12))
            Else
                maxMZCountInChunk = 1
            End If

            If maxMZCountInChunk > mzBinList.Count Then
                maxMZCountInChunk = mzBinList.Count
            End If
            If maxMZCountInChunk < 1 Then maxMZCountInChunk = 1

            ' Reserve room in parentIonUpdated
            ReDim parentIonUpdated(mzBinList.Count - 1)

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

            Dim mzIndex = 0
            Do While mzIndex < mzBinList.Count

                '---------------------------------------------------------
                ' Find the next group of m/z values to use, starting with mzIndex
                '---------------------------------------------------------

                ' Initially set the MZIndexStart to mzIndex

                ' Look for adjacent m/z values within udtMZBinList(.MZIndexStart).MZToleranceDa / 2
                '  of the m/z value that starts this group
                ' Only group m/z values with the same udtMZBinList().MZTolerance and udtMZBinList().MZToleranceIsPPM values

                Dim mzSearchChunk = New clsMzSearchInfo() With {
                    .MZIndexStart = mzIndex,
                    .MZTolerance = mzBinList(mzIndex).MZTolerance,
                    .MZToleranceIsPPM = mzBinList(mzIndex).MZToleranceIsPPM
                }

                Dim mzToleranceDa As Double
                If mzSearchChunk.MZToleranceIsPPM Then
                    mzToleranceDa = clsUtilities.PPMToMass(mzSearchChunk.MZTolerance, mzBinList(mzSearchChunk.MZIndexStart).MZ)
                Else
                    mzToleranceDa = mzSearchChunk.MZTolerance
                End If

                Do While mzIndex < mzBinList.Count - 2 AndAlso
                     Math.Abs(mzBinList(mzIndex + 1).MZTolerance - mzSearchChunk.MZTolerance) < Double.Epsilon AndAlso
                         mzBinList(mzIndex + 1).MZToleranceIsPPM = mzSearchChunk.MZToleranceIsPPM AndAlso
                         mzBinList(mzIndex + 1).MZ - mzBinList(mzSearchChunk.MZIndexStart).MZ <= mzToleranceDa / 2
                    mzIndex += 1
                Loop
                mzSearchChunk.MZIndexEnd = mzIndex

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

                If mzSearchChunks.Count >= maxMZCountInChunk OrElse mzIndex = mzBinList.Count - 1 Then

                    '---------------------------------------------------------
                    ' Reached maxMZCountInChunk m/z value
                    ' Process all of the m/z values in udtMZSearchChunk
                    '---------------------------------------------------------

                    Dim success = ProcessMzSearchChunk(
                        masicOptions,
                        scanList,
                        dataAggregation, dataOutputHandler, xmlResultsWriter,
                        objSpectraCache, scanNumScanConverter,
                        mzSearchChunks,
                        parentIonIndices,
                        processSIMScans,
                        simIndex,
                        parentIonUpdated,
                        parentIonsProcessed)

                    If Not success Then
                        Return False
                    End If

                    ' Clear mzSearchChunks
                    mzSearchChunks.Clear()

                End If

                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Exit Do
                End If

                mzIndex += 1
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
      processSIMScans As Boolean,
      simIndex As Integer,
      parentIonUpdated As IList(Of Boolean),
      ByRef parentIonsProcessed As Integer) As Boolean

        ' The following are 2D arrays, ranging from 0 to mzSearchChunkCount-1 in the first dimension and 0 to .SurveyScans.Count - 1 in the second dimension
        ' We could have included these in udtMZSearchChunk but memory management is more efficient if I use 2D arrays for this data
        Dim fullSICScanIndices(,) As Integer     ' Pointer into .SurveyScans
        Dim fullSICIntensities(,) As Double
        Dim fullSICMasses(,) As Double
        Dim fullSICDataCount() As Integer        ' Count of the number of valid entries in the second dimension of the above 3 arrays

        ' The following is a 1D array, containing the SIC intensities for a single m/z group
        Dim fullSICIntensities1D() As Double

        ' Reserve room in fullSICScanIndices for at most maxMZCountInChunk values and .SurveyScans.Count scans
        ReDim fullSICDataCount(mzSearchChunk.Count - 1)
        ReDim fullSICScanIndices(mzSearchChunk.Count - 1, scanList.SurveyScans.Count - 1)
        ReDim fullSICIntensities(mzSearchChunk.Count - 1, scanList.SurveyScans.Count - 1)
        ReDim fullSICMasses(mzSearchChunk.Count - 1, scanList.SurveyScans.Count - 1)

        ReDim fullSICIntensities1D(scanList.SurveyScans.Count - 1)

        ' Initialize .MaximumIntensity and .ScanIndexMax
        ' Additionally, reset fullSICDataCount() and, for safety, set fullSICScanIndices() to -1
        For mzIndexWork = 0 To mzSearchChunk.Count - 1
            mzSearchChunk(mzIndexWork).ResetMaxIntensity()

            fullSICDataCount(mzIndexWork) = 0
            For surveyScanIndex = 0 To scanList.SurveyScans.Count - 1
                fullSICScanIndices(mzIndexWork, surveyScanIndex) = -1
            Next
        Next

        '---------------------------------------------------------
        ' Step through scanList to obtain the scan numbers and intensity data for each .SearchMZ in udtMZSearchChunk
        ' We're stepping scan by scan since the process of loading a scan from disk is slower than the process of searching for each m/z in the scan
        '---------------------------------------------------------
        For surveyScanIndex = 0 To scanList.SurveyScans.Count - 1

            Dim useScan As Boolean

            If processSIMScans Then
                If scanList.SurveyScans(surveyScanIndex).SIMScan AndAlso
                   scanList.SurveyScans(surveyScanIndex).SIMIndex = simIndex Then
                    useScan = True
                Else
                    useScan = False
                End If
            Else
                useScan = Not scanList.SurveyScans(surveyScanIndex).SIMScan

                If scanList.SurveyScans(surveyScanIndex).ZoomScan Then
                    useScan = False
                End If
            End If

            If Not useScan Then
                Continue For
            End If

            Dim poolIndex As Integer

            If Not objSpectraCache.ValidateSpectrumInPool(scanList.SurveyScans(surveyScanIndex).ScanNumber, poolIndex) Then
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum)
                Return False
            End If

            For mzIndexWork = 0 To mzSearchChunk.Count - 1

                Dim current = mzSearchChunk(mzIndexWork)

                Dim mzToleranceDa As Double

                If current.MZToleranceIsPPM Then
                    mzToleranceDa = clsUtilities.PPMToMass(current.MZTolerance, current.SearchMZ)
                Else
                    mzToleranceDa = current.MZTolerance
                End If

                Dim ionMatchCount As Integer
                Dim closestMZ As Double

                Dim ionSum = dataAggregation.AggregateIonsInRange(objSpectraCache.SpectraPool(poolIndex),
                                                                         current.SearchMZ, mzToleranceDa,
                                                                         ionMatchCount, closestMZ, False)

                Dim dataIndex = fullSICDataCount(mzIndexWork)
                fullSICScanIndices(mzIndexWork, dataIndex) = surveyScanIndex
                fullSICIntensities(mzIndexWork, dataIndex) = ionSum

                If ionSum < Single.Epsilon AndAlso masicOptions.SICOptions.ReplaceSICZeroesWithMinimumPositiveValueFromMSData Then
                    fullSICIntensities(mzIndexWork, dataIndex) = scanList.SurveyScans(surveyScanIndex).MinimumPositiveIntensity
                End If

                fullSICMasses(mzIndexWork, dataIndex) = closestMZ
                If ionSum > current.MaximumIntensity Then
                    current.MaximumIntensity = ionSum
                    current.ScanIndexMax = dataIndex
                End If

                fullSICDataCount(mzIndexWork) += 1

            Next

            If surveyScanIndex Mod 100 = 0 Then
                UpdateProgress("Loading raw SIC data: " & surveyScanIndex & " / " & scanList.SurveyScans.Count)
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
        ' Compute the noise level in fullSICIntensities() for each m/z in udtMZSearchChunk
        ' Also, find the peaks for each m/z in udtMZSearchChunk and retain the largest peak found
        '---------------------------------------------------------
        For mzIndexWork = 0 To mzSearchChunk.Count - 1

            ' Use this for debugging
            If Math.Abs(mzSearchChunk(mzIndexWork).SearchMZ - DebugMZToFind) < 0.1 Then
                Dim parentIonIndexPointer = mzSearchChunk(mzIndexWork).MZIndexStart
            End If

            ' Copy the data for this m/z into fullSICIntensities1D()
            For dataIndex = 0 To fullSICDataCount(mzIndexWork) - 1
                fullSICIntensities1D(dataIndex) = fullSICIntensities(mzIndexWork, dataIndex)
            Next

            ' Compute the noise level; the noise level may change with increasing index number if the background is increasing for a given m/z
            Dim noiseStatSegments As List(Of clsBaselineNoiseStatsSegment) = Nothing

            Dim success = mMASICPeakFinder.ComputeDualTrimmedNoiseLevelTTest(
                fullSICIntensities1D, 0, fullSICDataCount(mzIndexWork) - 1,
                masicOptions.SICOptions.SICPeakFinderOptions.SICBaselineNoiseOptions,
                noiseStatSegments)

            If Not success Then
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.FindSICPeaksError, True)
                Return False
            End If

            mzSearchChunk(mzIndexWork).BaselineNoiseStatSegments = noiseStatSegments

            Dim potentialAreaStatsInFullSIC As clsSICPotentialAreaStats = Nothing

            ' Compute the minimum potential peak area in the entire SIC, populating udtSICPotentialAreaStatsInFullSIC
            mMASICPeakFinder.FindPotentialPeakArea(
                fullSICDataCount(mzIndexWork),
                fullSICIntensities1D,
                potentialAreaStatsInFullSIC,
                masicOptions.SICOptions.SICPeakFinderOptions)

            Dim potentialAreaStatsForPeak As clsSICPotentialAreaStats = Nothing

            Dim scanIndexObservedInFullSIC = mzSearchChunk(mzIndexWork).ScanIndexMax

            ' Initialize sicDetails
            Dim sicDetails = New clsSICDetails() With {
                .SICScanType = clsScanList.eScanTypeConstants.SurveyScan
            }

            Dim sicPeak As clsSICStatsPeak = Nothing

            ' Populate sicDetails using the data centered around the highest intensity in fullSICIntensities
            ' Note that this function will update udtSICPeak.IndexObserved
            ExtractSICDetailsFromFullSIC(
                mzIndexWork, mzSearchChunk(mzIndexWork).BaselineNoiseStatSegments,
                fullSICDataCount(mzIndexWork), fullSICScanIndices, fullSICIntensities, fullSICMasses,
                scanList, scanIndexObservedInFullSIC,
                sicDetails, sicPeak,
                masicOptions, scanNumScanConverter, False, 0)

            Dim smoothedYDataSubset As clsSmoothedYDataSubset = Nothing

            ' Find the largest peak in the SIC for this m/z
            Dim largestPeakFound = mMASICPeakFinder.FindSICPeakAndArea(
                sicDetails.SICData,
                potentialAreaStatsForPeak, sicPeak,
                smoothedYDataSubset, masicOptions.SICOptions.SICPeakFinderOptions,
                potentialAreaStatsInFullSIC,
                True, scanList.SIMDataPresent, False)

            If largestPeakFound Then
                '--------------------------------------------------------
                ' Step through the parent ions and see if .SurveyScanIndex is contained in udtSICPeak
                ' If it is, then assign the stats of the largest peak to the given parent ion
                '--------------------------------------------------------

                Dim mzIndexSICIndices = sicDetails.SICScanIndices

                For parentIonIndexPointer = mzSearchChunk(mzIndexWork).MZIndexStart To mzSearchChunk(mzIndexWork).MZIndexEnd
                    ' Use this for debugging
                    If parentIonIndices(parentIonIndexPointer) = DebugParentIonIndexToFind Then
                        scanIndexObservedInFullSIC = -1
                    End If

                    Dim storePeak = False
                    If scanList.ParentIons(parentIonIndices(parentIonIndexPointer)).CustomSICPeak Then Continue For

                    ' Assign the stats of the largest peak to each parent ion with .SurveyScanIndex contained in the peak
                    With scanList.ParentIons(parentIonIndices(parentIonIndexPointer))
                        If .SurveyScanIndex >= mzIndexSICIndices(sicPeak.IndexBaseLeft) AndAlso
                           .SurveyScanIndex <= mzIndexSICIndices(sicPeak.IndexBaseRight) Then

                            storePeak = True
                        End If
                    End With

                    If storePeak Then
                        StorePeakInParentIon(scanList, parentIonIndices(parentIonIndexPointer),
                                                       sicDetails,
                                                       potentialAreaStatsForPeak, sicPeak, True)

                        ' Possibly save the stats for this SIC to the SICData file
                        dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                                                            parentIonIndices(parentIonIndexPointer), sicDetails)

                        ' Save the stats for this SIC to the XML file
                        xmlResultsWriter.SaveDataToXML(scanList,
                                                       parentIonIndices(parentIonIndexPointer), sicDetails,
                                                       smoothedYDataSubset, dataOutputHandler)

                        parentIonUpdated(parentIonIndexPointer) = True
                        parentIonsProcessed += 1

                    End If


                Next
            End If

            '--------------------------------------------------------
            ' Now step through the parent ions and process those that were not updated using udtSICPeak
            ' For each, search for the closest peak in sICIntensity
            '--------------------------------------------------------
            For parentIonIndexPointer = mzSearchChunk(mzIndexWork).MZIndexStart To mzSearchChunk(mzIndexWork).MZIndexEnd

                If parentIonUpdated(parentIonIndexPointer) Then Continue For

                If parentIonIndices(parentIonIndexPointer) = DebugParentIonIndexToFind Then
                    scanIndexObservedInFullSIC = -1
                End If

                Dim smoothedYDataSubsetInSearchChunk As clsSmoothedYDataSubset = Nothing

                With scanList.ParentIons(parentIonIndices(parentIonIndexPointer))
                    ' Clear udtSICPotentialAreaStatsForPeak
                    .SICStats.SICPotentialAreaStatsForPeak = New clsSICPotentialAreaStats()

                    ' Record the index in the Full SIC that the parent ion mass was first observed
                    ' Search for .SurveyScanIndex in fullSICScanIndices
                    scanIndexObservedInFullSIC = -1
                    For dataIndex = 0 To fullSICDataCount(mzIndexWork) - 1
                        If fullSICScanIndices(mzIndexWork, dataIndex) >= .SurveyScanIndex Then
                            scanIndexObservedInFullSIC = dataIndex
                            Exit For
                        End If
                    Next

                    If scanIndexObservedInFullSIC = -1 Then
                        ' Match wasn't found; this is unexpected
                        ReportError("Programming error: survey scan index not found in fullSICScanIndices()", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                        scanIndexObservedInFullSIC = 0
                    End If

                    ' Populate udtSICDetails using the data centered around scanIndexObservedInFullSIC
                    ' Note that this function will update udtSICPeak.IndexObserved
                    ExtractSICDetailsFromFullSIC(
                        mzIndexWork, mzSearchChunk(mzIndexWork).BaselineNoiseStatSegments,
                        fullSICDataCount(mzIndexWork), fullSICScanIndices, fullSICIntensities, fullSICMasses,
                        scanList, scanIndexObservedInFullSIC,
                        sicDetails, .SICStats.Peak,
                        masicOptions, scanNumScanConverter,
                        .CustomSICPeak, .CustomSICPeakScanOrAcqTimeTolerance)

                    Dim peakIsValid = mMASICPeakFinder.FindSICPeakAndArea(
                        sicDetails.SICData,
                        .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak,
                        smoothedYDataSubsetInSearchChunk, masicOptions.SICOptions.SICPeakFinderOptions,
                        potentialAreaStatsInFullSIC,
                        Not .CustomSICPeak, scanList.SIMDataPresent, False)


                    StorePeakInParentIon(scanList, parentIonIndices(parentIonIndexPointer),
                                                      sicDetails,
                                                      .SICStats.SICPotentialAreaStatsForPeak, .SICStats.Peak, peakIsValid)
                End With

                ' Possibly save the stats for this SIC to the SICData file
                dataOutputHandler.SaveSICDataToText(masicOptions.SICOptions, scanList,
                                                    parentIonIndices(parentIonIndexPointer), sicDetails)

                ' Save the stats for this SIC to the XML file
                xmlResultsWriter.SaveDataToXML(scanList,
                                               parentIonIndices(parentIonIndexPointer), sicDetails,
                                               smoothedYDataSubsetInSearchChunk, dataOutputHandler)

                parentIonUpdated(parentIonIndexPointer) = True
                parentIonsProcessed += 1

            Next

            '---------------------------------------------------------
            ' Update progress
            '---------------------------------------------------------
            Try

                If scanList.ParentIons.Count > 1 Then
                    UpdateProgress(CShort(parentIonsProcessed / (scanList.ParentIons.Count - 1) * 100))
                Else
                    UpdateProgress(0)
                End If

                UpdateCacheStats(objSpectraCache)
                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Exit For
                End If

                If parentIonsProcessed Mod 100 = 0 Then
                    If DateTime.UtcNow.Subtract(masicOptions.LastParentIonProcessingLogTime).TotalSeconds >= 10 OrElse parentIonsProcessed Mod 500 = 0 Then
                        ReportMessage("Parent Ions Processed: " & parentIonsProcessed.ToString())
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
      parentIonIndex As Integer,
      sicDetails As clsSICDetails,
      potentialAreaStatsForPeak As clsSICPotentialAreaStats,
      sicPeak As clsSICStatsPeak,
      peakIsValid As Boolean) As Boolean

        ' sicIntensities
        ' sicScanIndices is sicDetails.SICScanIndices



        Dim dataIndex As Integer
        Dim scanIndexObserved As Integer
        Dim fragScanNumber As Integer

        Dim processingMRMPeak As Boolean

        Try

            If sicDetails.SICDataCount = 0 Then
                ' Either .SICData is nothing or no SIC data exists
                ' Cannot find peaks for this parent ion
                With scanList.ParentIons(parentIonIndex).SICStats
                    With .Peak
                        .IndexObserved = 0
                        .IndexBaseLeft = .IndexObserved
                        .IndexBaseRight = .IndexObserved
                        .IndexMax = .IndexObserved
                    End With
                End With

                Return True
            End If

            Dim sicData = sicDetails.SICData

            With scanList.ParentIons(parentIonIndex)
                scanIndexObserved = .SurveyScanIndex
                If scanIndexObserved < 0 Then scanIndexObserved = 0

                If .MRMDaughterMZ > 0 Then
                    processingMRMPeak = True
                Else
                    processingMRMPeak = False
                End If

                With .SICStats

                    .SICPotentialAreaStatsForPeak = potentialAreaStatsForPeak
                    .Peak = sicPeak

                    .ScanTypeForPeakIndices = sicDetails.SICScanType
                    If processingMRMPeak Then
                        If .ScanTypeForPeakIndices <> clsScanList.eScanTypeConstants.FragScan Then
                            ' ScanType is not FragScan; this is unexpected
                            ReportError("Programming error: udtSICDetails.SICScanType is not FragScan even though we're processing an MRM peak", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                            .ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan
                        End If
                    End If

                    If processingMRMPeak Then
                        .Peak.IndexObserved = 0
                    Else
                        ' Record the index (of data in .SICData) that the parent ion mass was first observed
                        ' This is not necessarily the same as udtSICPeak.IndexObserved, so we need to search for it here

                        ' Search for scanIndexObserved in sicScanIndices()
                        .Peak.IndexObserved = -1
                        For dataIndex = 0 To sicDetails.SICDataCount - 1
                            If sicData(dataIndex).ScanIndex = scanIndexObserved Then
                                .Peak.IndexObserved = dataIndex
                                Exit For
                            End If
                        Next

                        If .Peak.IndexObserved = -1 Then
                            ' Match wasn't found; this is unexpected
                            ReportError("Programming error: survey scan index not found in sicScanIndices", clsMASIC.eMasicErrorCodes.FindSICPeaksError)
                            .Peak.IndexObserved = 0
                        End If
                    End If

                    If scanList.FragScans.Count > 0 AndAlso scanList.ParentIons(parentIonIndex).FragScanIndices(0) < scanList.FragScans.Count Then
                        ' Record the fragmentation scan number
                        fragScanNumber = scanList.FragScans(scanList.ParentIons(parentIonIndex).FragScanIndices(0)).ScanNumber
                    Else
                        ' Use the parent scan number as the fragmentation scan number
                        ' This is OK based on how mMASICPeakFinder.ComputeParentIonIntensity() uses fragScanNumber
                        fragScanNumber = scanList.SurveyScans(scanList.ParentIons(parentIonIndex).SurveyScanIndex).ScanNumber
                    End If

                    If processingMRMPeak Then
                        sicPeak.ParentIonIntensity = 0
                    Else
                        ' Determine the value for .ParentIonIntensity
                        mMASICPeakFinder.ComputeParentIonIntensity(
                            sicData,
                            .Peak,
                            fragScanNumber)
                    End If

                    If peakIsValid Then
                        ' Record the survey scan indices of the peak max, start, and end
                        ' Note that .ScanTypeForPeakIndices was set earlier in this function
                        .PeakScanIndexMax = sicData(.Peak.IndexMax).ScanIndex
                        .PeakScanIndexStart = sicData(.Peak.IndexBaseLeft).ScanIndex
                        .PeakScanIndexEnd = sicData(.Peak.IndexBaseRight).ScanIndex
                    Else
                        ' No peak found
                        .PeakScanIndexMax = sicData(.Peak.IndexMax).ScanIndex
                        .PeakScanIndexStart = .PeakScanIndexMax
                        .PeakScanIndexEnd = .PeakScanIndexMax

                        With .Peak
                            .MaxIntensityValue = sicData(.IndexMax).Intensity
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
            With scanList.ParentIons(parentIonIndex)
                Dim scanIndexPointer = sicData(.SICStats.Peak.IndexMax).ScanIndex
                If processingMRMPeak Then
                    .OptimalPeakApexScanNumber = scanList.FragScans(scanIndexPointer).ScanNumber
                Else
                    .OptimalPeakApexScanNumber = scanList.SurveyScans(scanIndexPointer).ScanNumber
                End If

            End With

            Return True

        Catch ex As Exception

            ReportError("Error finding SIC peaks and their areas", ex, clsMASIC.eMasicErrorCodes.FindSICPeaksError)
            Return False

        End Try

    End Function

End Class
