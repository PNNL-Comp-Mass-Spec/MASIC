Option Strict On

Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.InteropServices

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may Not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause

Public Class clsMASICPeakFinder
    Inherits PRISM.EventNotifier

#Region "Constants and Enums"
    Public PROGRAM_DATE As String = "October 1, 2018"

    Public Const MINIMUM_PEAK_WIDTH As Integer = 3                         ' Width in points

    Public Enum eNoiseThresholdModes
        AbsoluteThreshold = 0
        TrimmedMeanByAbundance = 1
        TrimmedMeanByCount = 2
        TrimmedMedianByAbundance = 3
        DualTrimmedMeanByAbundance = 4
        MeanOfDataInPeakVicinity = 5
    End Enum

    Public Enum eTTestConfidenceLevelConstants
        Conf80Pct = 0
        Conf90Pct = 1
        Conf95Pct = 2
        Conf98Pct = 3
        Conf99Pct = 4
        Conf99_5Pct = 5
        Conf99_8Pct = 6
        Conf99_9Pct = 7
    End Enum
#End Region

#Region "Classwide Variables"
    Private mStatusMessage As String

    ''' <summary>
    ''' TTest Significance Table.
    ''' Confidence Levels and critical values:
    '''  80%, 90%, 95%, 98%, 99%, 99.5%, 99.8%, 99.9%
    '''  1.886, 2.920, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598
    ''' </summary>
    Private ReadOnly TTestConfidenceLevels As Single() = New Single() {1.886, 2.92, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598}

#End Region

#Region "Properties"
    Public ReadOnly Property ProgramDate As String
        Get
            Return PROGRAM_DATE
        End Get
    End Property

    Public ReadOnly Property ProgramVersion As String
        Get
            Return GetVersionForExecutingAssembly()
        End Get
    End Property

    Public ReadOnly Property StatusMessage As String
        Get
            Return mStatusMessage
        End Get
    End Property
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        mStatusMessage = String.Empty
    End Sub

    ''' <summary>
    ''' Compute an updated peak area by adjusting for the baseline
    ''' </summary>
    ''' <param name="sicPeak"></param>
    ''' <param name="intSICPeakWidthFullScans"></param>
    ''' <param name="blnAllowNegativeValues"></param>
    ''' <returns>Adjusted peak area</returns>
    ''' <remarks>This method is used by MASIC Browser</remarks>
    Public Shared Function BaselineAdjustArea(
      sicPeak As clsSICStatsPeak, intSICPeakWidthFullScans As Integer, blnAllowNegativeValues As Boolean) As Single
        ' Note, compute intSICPeakWidthFullScans using:
        '  Width = SICScanNumbers(.Peak.IndexBaseRight) - SICScanNumbers(.Peak.IndexBaseLeft) + 1

        Return BaselineAdjustArea(sicPeak.Area, sicPeak.BaselineNoiseStats.NoiseLevel, sicPeak.FWHMScanWidth, intSICPeakWidthFullScans, blnAllowNegativeValues)

    End Function

    Public Shared Function BaselineAdjustArea(
      sngPeakArea As Single,
      sngBaselineNoiseLevel As Single,
      intSICPeakFWHMScans As Integer,
      intSICPeakWidthFullScans As Integer,
      blnAllowNegativeValues As Boolean) As Single

        Dim sngCorrectedArea As Single
        Dim intWidthToSubtract As Integer

        intWidthToSubtract = ComputeWidthAtBaseUsingFWHM(intSICPeakFWHMScans, intSICPeakWidthFullScans, 4)

        sngCorrectedArea = sngPeakArea - sngBaselineNoiseLevel * intWidthToSubtract
        If blnAllowNegativeValues OrElse sngCorrectedArea > 0 Then
            Return sngCorrectedArea
        Else
            Return 0
        End If
    End Function

    Public Shared Function BaselineAdjustIntensity(sicPeak As clsSICStatsPeak, blnAllowNegativeValues As Boolean) As Single
        Return BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, blnAllowNegativeValues)
    End Function

    Public Shared Function BaselineAdjustIntensity(
      sngRawIntensity As Single,
      sngBaselineNoiseLevel As Single,
      blnAllowNegativeValues As Boolean) As Single

        If blnAllowNegativeValues OrElse sngRawIntensity > sngBaselineNoiseLevel Then
            Return sngRawIntensity - sngBaselineNoiseLevel
        Else
            Return 0
        End If
    End Function

    Private Function ComputeAverageNoiseLevelCheckCounts(
      intValidDataCountA As Integer, intValidDataCountB As Integer,
      dblSumA As Double, dblSumB As Double,
      intMinimumCount As Integer,
      baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        Dim blnUseBothSides As Boolean
        Dim blnUseLeftData As Boolean
        Dim blnUseRightData As Boolean

        If intMinimumCount < 1 Then intMinimumCount = 1
        blnUseBothSides = False

        If intValidDataCountA >= intMinimumCount OrElse intValidDataCountB >= intMinimumCount Then

            If intValidDataCountA >= intMinimumCount AndAlso intValidDataCountB >= intMinimumCount Then
                ' Both meet the minimum count criterion
                ' Return an overall average
                blnUseBothSides = True
            ElseIf intValidDataCountA >= intMinimumCount Then
                blnUseLeftData = True
            Else
                blnUseRightData = True
            End If

            If blnUseBothSides Then
                baselineNoiseStats.NoiseLevel = CSng((dblSumA + dblSumB) / (intValidDataCountA + intValidDataCountB))
                baselineNoiseStats.NoiseStDev = 0      ' We'll compute noise StDev outside this function
                baselineNoiseStats.PointsUsed = intValidDataCountA + intValidDataCountB
            Else
                If blnUseLeftData Then
                    ' Use left data only
                    baselineNoiseStats.NoiseLevel = CSng(dblSumA / intValidDataCountA)
                    baselineNoiseStats.NoiseStDev = 0
                    baselineNoiseStats.PointsUsed = intValidDataCountA
                ElseIf blnUseRightData Then
                    ' Use right data only
                    baselineNoiseStats.NoiseLevel = CSng(dblSumB / intValidDataCountB)
                    baselineNoiseStats.NoiseStDev = 0
                    baselineNoiseStats.PointsUsed = intValidDataCountB
                Else
                    Throw New Exception("Logic error; This code should not be reached")
                End If
            End If
            Return True
        Else
            Return False
        End If

    End Function

    Private Function ComputeAverageNoiseLevelExcludingRegion(
      sicData As IList(Of clsSICDataPoint),
      intIndexStart As Integer, intIndexEnd As Integer,
      intExclusionIndexStart As Integer, intExclusionIndexEnd As Integer,
      baselineNoiseOptions As clsBaselineNoiseOptions,
      baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        ' Compute the average intensity level between intIndexStart and intExclusionIndexStart
        ' Also compute the average between intExclusionIndexEnd and intIndexEnd
        ' Use ComputeAverageNoiseLevelCheckCounts to determine whether both averages are used to determine
        '  the baseline noise level or whether just one of the averages is used

        Dim blnSuccess = False

        ' Examine the exclusion range.  If the exclusion range excludes all
        '  data to the left or right of the peak, then use a few data points anyway, even if this does include some of the peak
        If intExclusionIndexStart < intIndexStart + MINIMUM_PEAK_WIDTH Then
            intExclusionIndexStart = intIndexStart + MINIMUM_PEAK_WIDTH
            If intExclusionIndexStart >= intIndexEnd Then
                intExclusionIndexStart = intIndexEnd - 1
            End If
        End If

        If intExclusionIndexEnd > intIndexEnd - MINIMUM_PEAK_WIDTH Then
            intExclusionIndexEnd = intIndexEnd - MINIMUM_PEAK_WIDTH
            If intExclusionIndexEnd < 0 Then
                intExclusionIndexEnd = 0
            End If
        End If

        If intExclusionIndexStart >= intIndexStart AndAlso intExclusionIndexStart <= intIndexEnd AndAlso
           intExclusionIndexEnd >= intExclusionIndexStart AndAlso intExclusionIndexEnd <= intIndexEnd _
           Then

            Dim minimumPositiveValue = FindMinimumPositiveValue(sicData, 1)

            Dim intValidDataCountA = 0
            Dim dblSumA As Double = 0
            For intIndex = intIndexStart To intExclusionIndexStart
                dblSumA += Math.Max(minimumPositiveValue, sicData(intIndex).Intensity)
                intValidDataCountA += 1
            Next

            Dim intValidDataCountB = 0
            Dim dblSumB As Double = 0
            For intIndex = intExclusionIndexEnd To intIndexEnd
                dblSumB += Math.Max(minimumPositiveValue, sicData(intIndex).Intensity)
                intValidDataCountB += 1
            Next

            blnSuccess = ComputeAverageNoiseLevelCheckCounts(
              intValidDataCountA, intValidDataCountB,
              dblSumA, dblSumB,
              MINIMUM_PEAK_WIDTH, baselineNoiseStats)

            ' Assure that .NoiseLevel is at least as large as minimumPositiveValue
            If baselineNoiseStats.NoiseLevel < minimumPositiveValue Then
                baselineNoiseStats.NoiseLevel = minimumPositiveValue
            End If

            ' Populate .NoiseStDev
            intValidDataCountA = 0
            intValidDataCountB = 0
            dblSumA = 0
            dblSumB = 0
            If baselineNoiseStats.PointsUsed > 0 Then
                For intIndex = intIndexStart To intExclusionIndexStart
                    dblSumA += (Math.Max(minimumPositiveValue, sicData(intIndex).Intensity) - baselineNoiseStats.NoiseLevel) ^ 2
                    intValidDataCountA += 1
                Next

                For intIndex = intExclusionIndexEnd To intIndexEnd
                    dblSumB += (Math.Max(minimumPositiveValue, sicData(intIndex).Intensity) - baselineNoiseStats.NoiseLevel) ^ 2
                    intValidDataCountB += 1
                Next
            End If

            If intValidDataCountA + intValidDataCountB > 0 Then
                baselineNoiseStats.NoiseStDev = CSng(Math.Sqrt((dblSumA + dblSumB) / (intValidDataCountA + intValidDataCountB)))
            Else
                baselineNoiseStats.NoiseStDev = 0
            End If

        End If

        If Not blnSuccess Then
            Dim baselineNoiseOptionsOverride = baselineNoiseOptions.Clone()

            With baselineNoiseOptionsOverride
                .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
                .TrimmedMeanFractionLowIntensityDataToAverage = 0.33
            End With

            Dim intensities = (From item In sicData Select item.Intensity).ToArray()

            blnSuccess = ComputeTrimmedNoiseLevel(intensities, intIndexStart, intIndexEnd, baselineNoiseOptionsOverride, False, baselineNoiseStats)
        End If

        Return blnSuccess

    End Function

    ''' <summary>
    ''' Divide the data into the number of segments given by baselineNoiseOptions.DualTrimmedMeanMaximumSegments  (use 3 by default)
    ''' Call ComputeDualTrimmedNoiseLevel for each segment
    ''' Use a TTest to determine whether we need to define a custom noise threshold for each segment
    ''' </summary>
    ''' <param name="sngData"></param>
    ''' <param name="intIndexStart"></param>
    ''' <param name="intIndexEnd"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="noiseStatsSegments"></param>
    ''' <returns>True if success, False if error</returns>
    Public Function ComputeDualTrimmedNoiseLevelTTest(
      sngData() As Single, intIndexStart As Integer, intIndexEnd As Integer,
      baselineNoiseOptions As clsBaselineNoiseOptions,
      <Out> ByRef noiseStatsSegments As List(Of clsBaselineNoiseStatsSegment)) As Boolean

        noiseStatsSegments = New List(Of clsBaselineNoiseStatsSegment)

        Try

            Dim intSegmentCountLocal = CInt(baselineNoiseOptions.DualTrimmedMeanMaximumSegments)
            If intSegmentCountLocal = 0 Then intSegmentCountLocal = 3
            If intSegmentCountLocal < 1 Then intSegmentCountLocal = 1

            ' Initialize BaselineNoiseStats for each segment now, in case an error occurs
            For intIndex = 0 To intSegmentCountLocal - 1
                Dim baselineNoiseStats = InitializeBaselineNoiseStats(
                  baselineNoiseOptions.MinimumBaselineNoiseLevel,
                  eNoiseThresholdModes.DualTrimmedMeanByAbundance)

                noiseStatsSegments.Add(New clsBaselineNoiseStatsSegment(baselineNoiseStats))
            Next

            ' Determine the segment length
            Dim intSegmentLength = CInt(Math.Round((intIndexEnd - intIndexStart) / intSegmentCountLocal, 0))

            ' Initialize the first segment
            Dim firstSegment = noiseStatsSegments.First()
            firstSegment.SegmentIndexStart = intIndexStart
            If intSegmentCountLocal = 1 Then
                firstSegment.SegmentIndexEnd = intIndexEnd
            Else
                firstSegment.SegmentIndexEnd = firstSegment.SegmentIndexStart + intSegmentLength - 1
            End If

            ' Initialize the remaining segments
            For intIndex = 1 To intSegmentCountLocal - 1
                noiseStatsSegments(intIndex).SegmentIndexStart = noiseStatsSegments(intIndex - 1).SegmentIndexEnd + 1
                If intIndex = intSegmentCountLocal - 1 Then
                    noiseStatsSegments(intIndex).SegmentIndexEnd = intIndexEnd
                Else
                    noiseStatsSegments(intIndex).SegmentIndexEnd = noiseStatsSegments(intIndex).SegmentIndexStart + intSegmentLength - 1
                End If
            Next

            ' Call ComputeDualTrimmedNoiseLevel for each segment
            For intIndex = 0 To intSegmentCountLocal - 1
                Dim current = noiseStatsSegments(intIndex)

                ComputeDualTrimmedNoiseLevel(sngData, current.SegmentIndexStart, current.SegmentIndexEnd, baselineNoiseOptions, current.BaselineNoiseStats)

            Next

            ' Compare adjacent segments using a T-Test, starting with the final segment and working backward
            Dim eConfidenceLevel = eTTestConfidenceLevelConstants.Conf90Pct
            Dim intSegmentIndex = intSegmentCountLocal - 1

            Do While intSegmentIndex > 0
                Dim previous = noiseStatsSegments(intSegmentIndex - 1)
                Dim current = noiseStatsSegments(intSegmentIndex)

                Dim blnSignificantDifference As Boolean
                Dim dblTCalculated As Double

                blnSignificantDifference = TestSignificanceUsingTTest(
                    current.BaselineNoiseStats.NoiseLevel,
                    previous.BaselineNoiseStats.NoiseLevel,
                    current.BaselineNoiseStats.NoiseStDev,
                    previous.BaselineNoiseStats.NoiseStDev,
                    current.BaselineNoiseStats.PointsUsed,
                    previous.BaselineNoiseStats.PointsUsed,
                    eConfidenceLevel,
                    dblTCalculated)

                If blnSignificantDifference Then
                    ' Significant difference; leave the 2 segments intact
                Else
                    ' Not a significant difference; recompute the Baseline Noise stats using the two segments combined
                    previous.SegmentIndexEnd = current.SegmentIndexEnd
                    ComputeDualTrimmedNoiseLevel(sngData,
                                                 previous.SegmentIndexStart,
                                                 previous.SegmentIndexEnd,
                                                 baselineNoiseOptions,
                                                 previous.BaselineNoiseStats)

                    For intSegmentIndexCopy = intSegmentIndex To intSegmentCountLocal - 2
                        noiseStatsSegments(intSegmentIndexCopy) = noiseStatsSegments(intSegmentIndexCopy + 1)
                    Next
                    intSegmentCountLocal -= 1
                End If
                intSegmentIndex -= 1
            Loop

            While noiseStatsSegments.Count > intSegmentCountLocal
                noiseStatsSegments.RemoveAt(noiseStatsSegments.Count - 1)
            End While

        Catch ex As Exception
            Return False
        End Try

        Return True

    End Function

    ''' <summary>
    '''  Computes the average of all of the data in sngData()
    '''  Next, discards the data above and below baselineNoiseOptions.DualTrimmedMeanStdDevLimits of the mean
    '''  Finally, recomputes the average using the data that remains
    ''' </summary>
    ''' <param name="sngData"></param>
    ''' <param name="intIndexStart"></param>
    ''' <param name="intIndexEnd"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="baselineNoiseStats"></param>
    ''' <returns>True if success, False if error (or no data in sngData)</returns>
    ''' <remarks>
    ''' Replaces values of 0 with the minimum positive value in sngData()
    ''' You cannot use sngData.Length to determine the length of the array; use intIndexStart and intIndexEnd to find the limits
    ''' </remarks>
    Public Function ComputeDualTrimmedNoiseLevel(sngData() As Single, intIndexStart As Integer, intIndexEnd As Integer,
                                                 baselineNoiseOptions As clsBaselineNoiseOptions,
                                                 <Out> ByRef baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        ' Initialize udtBaselineNoiseStats
        baselineNoiseStats = InitializeBaselineNoiseStats(
          baselineNoiseOptions.MinimumBaselineNoiseLevel,
          eNoiseThresholdModes.DualTrimmedMeanByAbundance)

        If sngData Is Nothing OrElse intIndexEnd - intIndexStart < 0 Then
            Return False
        End If

        ' Copy the data into sngDataSorted
        Dim intDataSortedCount = intIndexEnd - intIndexStart + 1
        Dim sngDataSorted() As Single
        ReDim sngDataSorted(intDataSortedCount - 1)

        For intIndex = intIndexStart To intIndexEnd
            sngDataSorted(intIndex - intIndexStart) = sngData(intIndex)
        Next

        ' Sort the array
        Array.Sort(sngDataSorted)

        ' Look for the minimum positive value and replace all data in sngDataSorted with that value
        Dim minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(intDataSortedCount, sngDataSorted)

        ' Initialize the indices to use in sngDataSorted()
        Dim intDataSortedIndexStart = 0
        Dim intDataSortedIndexEnd = intDataSortedCount - 1

        ' Compute the average using the data in sngDataSorted between intDataSortedIndexStart and intDataSortedIndexEnd (i.e. all the data)
        Dim dblSum As Double = 0
        For intIndex = intDataSortedIndexStart To intDataSortedIndexEnd
            dblSum += sngDataSorted(intIndex)
        Next

        Dim intDataUsedCount = intDataSortedIndexEnd - intDataSortedIndexStart + 1
        Dim dblAverage = dblSum / intDataUsedCount
        Dim dblVariance As Double

        If intDataUsedCount > 1 Then
            ' Compute the variance (this is a sample variance, not a population variance)
            dblSum = 0
            For intIndex = intDataSortedIndexStart To intDataSortedIndexEnd
                dblSum += (sngDataSorted(intIndex) - dblAverage) ^ 2
            Next
            dblVariance = dblSum / (intDataUsedCount - 1)
        Else
            dblVariance = 0
        End If

        If baselineNoiseOptions.DualTrimmedMeanStdDevLimits < 1 Then
            baselineNoiseOptions.DualTrimmedMeanStdDevLimits = 1
        End If

        ' Note: Standard Deviation = sigma = SquareRoot(Variance)
        Dim dblIntensityThresholdMin = dblAverage - Math.Sqrt(dblVariance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits
        Dim dblIntensityThresholdMax = dblAverage + Math.Sqrt(dblVariance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits

        ' Recompute the average using only the data between dblIntensityThresholdMin and dblIntensityThresholdMax in sngDataSorted
        dblSum = 0
        Dim intSortedIndex = intDataSortedIndexStart
        Do While intSortedIndex <= intDataSortedIndexEnd
            If sngDataSorted(intSortedIndex) >= dblIntensityThresholdMin Then
                intDataSortedIndexStart = intSortedIndex
                Do While intSortedIndex <= intDataSortedIndexEnd
                    If sngDataSorted(intSortedIndex) <= dblIntensityThresholdMax Then
                        dblSum += sngDataSorted(intSortedIndex)
                    Else
                        intDataSortedIndexEnd = intSortedIndex - 1
                        Exit Do
                    End If
                    intSortedIndex += 1
                Loop
            End If
            intSortedIndex += 1
        Loop
        intDataUsedCount = intDataSortedIndexEnd - intDataSortedIndexStart + 1

        If intDataUsedCount > 0 Then
            baselineNoiseStats.NoiseLevel = CSng(dblSum / intDataUsedCount)

            ' Compute the variance (this is a sample variance, not a population variance)
            dblSum = 0
            For intIndex = intDataSortedIndexStart To intDataSortedIndexEnd
                dblSum += (sngDataSorted(intIndex) - baselineNoiseStats.NoiseLevel) ^ 2
            Next

            If intDataUsedCount > 1 Then
                baselineNoiseStats.NoiseStDev = CSng(Math.Sqrt(dblSum / (intDataUsedCount - 1)))
            Else
                baselineNoiseStats.NoiseStDev = 0
            End If
            baselineNoiseStats.PointsUsed = intDataUsedCount
        Else
            baselineNoiseStats.NoiseLevel = Math.Max(minimumPositiveValue, baselineNoiseOptions.MinimumBaselineNoiseLevel)
            baselineNoiseStats.NoiseStDev = 0
        End If

        ' Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel
        If baselineNoiseStats.NoiseLevel < baselineNoiseOptions.MinimumBaselineNoiseLevel AndAlso
           baselineNoiseOptions.MinimumBaselineNoiseLevel > 0 Then

            baselineNoiseStats.NoiseLevel = baselineNoiseOptions.MinimumBaselineNoiseLevel

            ' Set this to 0 since we have overridden .NoiseLevel
            baselineNoiseStats.NoiseStDev = 0
        End If

        Return True

    End Function

    Private Function ComputeFWHM(
      sicData As IList(Of clsSICDataPoint),
      sicPeak As clsSICStatsPeak,
      blnSubtractBaselineNoise As Boolean) As Integer

        ' Note: The calling function should have already populated udtSICPeak.MaxIntensityValue, plus .IndexMax, .IndexBaseLeft, and .IndexBaseRight
        ' If blnSubtractBaselineNoise is True, then this function also uses udtSICPeak.BaselineNoiseStats....
        ' Note: This function returns the FWHM value in units of scan number; it does not update the value stored in udtSICPeak
        ' This function does, however, update udtSICPeak.IndexMax if it is not between udtSICPeak.IndexBaseLeft and udtSICPeak.IndexBaseRight

        Const ALLOW_NEGATIVE_VALUES = False
        Dim sngFWHMScanStart, sngFWHMScanEnd As Single
        Dim intFWHMScans As Integer

        Dim intDataIndex As Integer
        Dim sngTargetIntensity As Single
        Dim sngMaximumIntensity As Single

        Dim sngY1, sngY2 As Single

        ' Determine the full width at half max (FWHM), in units of absolute scan number
        Try

            If sicPeak.IndexMax <= sicPeak.IndexBaseLeft OrElse sicPeak.IndexMax >= sicPeak.IndexBaseRight Then
                ' Find the index of the maximum (between .IndexBaseLeft and .IndexBaseRight)
                sngMaximumIntensity = 0
                If sicPeak.IndexMax < sicPeak.IndexBaseLeft OrElse sicPeak.IndexMax > sicPeak.IndexBaseRight Then
                    sicPeak.IndexMax = sicPeak.IndexBaseLeft
                End If

                For intDataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                    If sicData(intDataIndex).Intensity > sngMaximumIntensity Then
                        sicPeak.IndexMax = intDataIndex
                        sngMaximumIntensity = sicData(intDataIndex).Intensity
                    End If
                Next
            End If

            ' Look for the intensity halfway down the peak (correcting for baseline noise level if blnSubtractBaselineNoise = True)
            If blnSubtractBaselineNoise Then
                sngTargetIntensity = BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES) / 2

                If sngTargetIntensity <= 0 Then
                    ' The maximum intensity of the peak is below the baseline; do not correct for baseline noise level
                    sngTargetIntensity = sicPeak.MaxIntensityValue / 2
                    blnSubtractBaselineNoise = False
                End If
            Else
                sngTargetIntensity = sicPeak.MaxIntensityValue / 2
            End If

            If sngTargetIntensity > 0 Then

                ' Start the search at each peak edge to thus determine the largest FWHM value
                sngFWHMScanStart = -1
                For intDataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexMax - 1
                    If blnSubtractBaselineNoise Then
                        sngY1 = BaselineAdjustIntensity(sicData(intDataIndex).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                        sngY2 = BaselineAdjustIntensity(sicData(intDataIndex + 1).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                    Else
                        sngY1 = sicData(intDataIndex).Intensity
                        sngY2 = sicData(intDataIndex + 1).Intensity
                    End If

                    If sngY1 > sngTargetIntensity OrElse sngY2 > sngTargetIntensity Then
                        If sngY1 <= sngTargetIntensity AndAlso sngY2 >= sngTargetIntensity Then
                            InterpolateX(
                                sngFWHMScanStart,
                                sicData(intDataIndex).ScanNumber, sicData(intDataIndex + 1).ScanNumber,
                                sngY1, sngY2, sngTargetIntensity)
                        Else
                            ' sngTargetIntensity is not between sngY1 and sngY2; simply use intDataIndex
                            If intDataIndex = sicPeak.IndexBaseLeft Then
                                ' At the start of the peak; use the scan number halfway between .IndexBaseLeft and .IndexMax
                                sngFWHMScanStart = sicData(intDataIndex + CInt(Math.Round((sicPeak.IndexMax - sicPeak.IndexBaseLeft) / 2, 0))).ScanNumber
                            Else
                                ' This code will probably never be reached
                                sngFWHMScanStart = sicData(intDataIndex).ScanNumber
                            End If
                        End If
                        Exit For
                    End If
                Next
                If sngFWHMScanStart < 0 Then
                    If sicPeak.IndexMax > sicPeak.IndexBaseLeft Then
                        sngFWHMScanStart = sicData(sicPeak.IndexMax - 1).ScanNumber
                    Else
                        sngFWHMScanStart = sicData(sicPeak.IndexBaseLeft).ScanNumber
                    End If
                End If

                sngFWHMScanEnd = -1
                For intDataIndex = sicPeak.IndexBaseRight - 1 To sicPeak.IndexMax Step -1
                    If blnSubtractBaselineNoise Then
                        sngY1 = BaselineAdjustIntensity(sicData(intDataIndex).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                        sngY2 = BaselineAdjustIntensity(sicData(intDataIndex + 1).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                    Else
                        sngY1 = sicData(intDataIndex).Intensity
                        sngY2 = sicData(intDataIndex + 1).Intensity
                    End If

                    If sngY1 > sngTargetIntensity OrElse sngY2 > sngTargetIntensity Then
                        If sngY1 >= sngTargetIntensity AndAlso sngY2 <= sngTargetIntensity Then
                            InterpolateX(
                                sngFWHMScanEnd,
                                sicData(intDataIndex).ScanNumber, sicData(intDataIndex + 1).ScanNumber,
                                sngY1, sngY2, sngTargetIntensity)
                        Else
                            ' sngTargetIntensity is not between sngY1 and sngY2; simply use intDataIndex+1
                            If intDataIndex = sicPeak.IndexBaseRight - 1 Then
                                ' At the end of the peak; use the scan number halfway between .IndexBaseRight and .IndexMax
                                sngFWHMScanEnd = sicData(intDataIndex + 1 - CInt(Math.Round((sicPeak.IndexBaseRight - sicPeak.IndexMax) / 2, 0))).ScanNumber
                            Else
                                ' This code will probably never be reached
                                sngFWHMScanEnd = sicData(intDataIndex + 1).ScanNumber
                            End If
                        End If
                        Exit For
                    End If
                Next
                If sngFWHMScanEnd < 0 Then
                    If sicPeak.IndexMax < sicPeak.IndexBaseRight Then
                        sngFWHMScanEnd = sicData(sicPeak.IndexMax + 1).ScanNumber
                    Else
                        sngFWHMScanEnd = sicData(sicPeak.IndexBaseRight).ScanNumber
                    End If
                End If

                intFWHMScans = CInt(Math.Round(sngFWHMScanEnd - sngFWHMScanStart, 0))
                If intFWHMScans <= 0 Then intFWHMScans = 0
            Else
                ' Maximum intensity value is <= 0
                ' Set FWHM to 1
                intFWHMScans = 1
            End If

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->ComputeFWHM", "Error finding FWHM", ex, False)
            intFWHMScans = 0
        End Try

        Return intFWHMScans

    End Function

    Public Sub TestComputeKSStat()
        Dim ScanAtApex As Integer
        Dim FWHM As Single

        Dim intScanNumbers = New Integer() {0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40}
        Dim sngIntensities = New Single() {2, 5, 7, 10, 11, 18, 19, 15, 8, 4, 1}

        ScanAtApex = 20
        FWHM = 25

        Dim peakMean As Single
        Dim peakStDev As Double

        peakMean = ScanAtApex
        ' FWHM / 2.35482 = FWHM / (2 * Sqrt(2 * Ln(2)))
        peakStDev = FWHM / 2.35482
        'peakStDev = 28.8312

        ComputeKSStatistic(intScanNumbers.Length, intScanNumbers, sngIntensities, peakMean, peakStDev)

        ' ToDo: Update program to call ComputeKSStatistic

        ' ToDo: Update Statistical Moments computation to:
        '  a) Create baseline adjusted intensity values
        '  b) Remove the contiguous data from either end that is <= 0
        '  c) Step through the remaining data and interpolate across gaps with intensities of 0 (linear interpolation)
        '  d) Use this final data to compute the statistical moments and KS Statistic

        ' If less than 3 points remain with the above procedure, then use the 5 points centered around the peak maximum, non-baseline corrected data

    End Sub

    Private Function ComputeKSStatistic(
      intDataCount As Integer,
      intXDataIn As IList(Of Integer),
      sngYDataIn As IList(Of Single),
      peakMean As Single,
      peakStDev As Double) As Double

        Dim intScanOffset As Integer
        Dim intXData() As Integer
        Dim dblYData() As Double

        Dim dblYDataNormalized() As Double
        Dim dblXDataPDF() As Double
        Dim dblYDataEDF() As Double
        Dim dblXDataCDF() As Double

        Dim KS_gof As Double
        Dim dblCompare As Double

        Dim dblYDataSum As Double
        Dim intIndex As Integer

        ' Copy data from intXDataIn() to intXData, subtracting the value in intXDataIn(0) from each scan
        ReDim intXData(intDataCount - 1)
        ReDim dblYData(intDataCount - 1)

        intScanOffset = intXDataIn(0)
        For intIndex = 0 To intDataCount - 1
            intXData(intIndex) = intXDataIn(intIndex) - intScanOffset
            dblYData(intIndex) = sngYDataIn(intIndex)
        Next

        dblYDataSum = 0
        For intIndex = 0 To dblYData.Length - 1
            dblYDataSum += dblYData(intIndex)
        Next
        If Math.Abs(dblYDataSum) < Double.Epsilon Then dblYDataSum = 1

        ' Compute the Vector of normalized intensities = observed pdf
        ReDim dblYDataNormalized(dblYData.Length - 1)
        For intIndex = 0 To dblYData.Length - 1
            dblYDataNormalized(intIndex) = dblYData(intIndex) / dblYDataSum
        Next

        ' Estimate the empirical distribution function (EDF) using an accumulating sum
        dblYDataSum = 0
        ReDim dblYDataEDF(dblYDataNormalized.Length - 1)
        For intIndex = 0 To dblYDataNormalized.Length - 1
            dblYDataSum += dblYDataNormalized(intIndex)
            dblYDataEDF(intIndex) = dblYDataSum
        Next

        ' Compute the Vector of Normal PDF values evaluated at the X values in the peak window
        ReDim dblXDataPDF(intXData.Length - 1)
        For intIndex = 0 To intXData.Length - 1
            dblXDataPDF(intIndex) = (1 / (Math.Sqrt(2 * Math.PI) * peakStDev)) * Math.Exp((-1 / 2) *
                                    ((intXData(intIndex) - (peakMean - intScanOffset)) / peakStDev) ^ 2)
        Next

        Dim dblXDataPDFSum As Double
        dblXDataPDFSum = 0
        For intIndex = 0 To dblXDataPDF.Length - 1
            dblXDataPDFSum += dblXDataPDF(intIndex)
        Next

        ' Estimate the theoretical CDF using an accumulating sum
        ReDim dblXDataCDF(dblXDataPDF.Length - 1)
        dblYDataSum = 0
        For intIndex = 0 To dblXDataPDF.Length - 1
            dblYDataSum += dblXDataPDF(intIndex)
            dblXDataCDF(intIndex) = dblYDataSum / ((1 + (1 / intXData.Length)) * dblXDataPDFSum)
        Next

        ' Compute the maximum of the absolute differences between the YData EDF and XData CDF
        KS_gof = 0
        For intIndex = 0 To dblXDataCDF.Length - 1
            dblCompare = Math.Abs(dblYDataEDF(intIndex) - dblXDataCDF(intIndex))
            If dblCompare > KS_gof Then
                KS_gof = dblCompare
            End If
        Next

        Return Math.Sqrt(intXData.Length) * KS_gof   '  return modified KS statistic

    End Function

    ''' <summary>
    ''' Compute the noise level
    ''' </summary>
    ''' <param name="intDataCount"></param>
    ''' <param name="sngData"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="baselineNoiseStats"></param>
    ''' <returns>Returns True if success, false in an error</returns>
    ''' <remarks>Updates udtBaselineNoiseStats with the baseline noise level</remarks>
    Public Function ComputeNoiseLevelForSICData(intDataCount As Integer, sngData() As Single,
                                                baselineNoiseOptions As clsBaselineNoiseOptions,
                                                <Out> ByRef baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        Const IGNORE_NON_POSITIVE_DATA = False

        If baselineNoiseOptions.BaselineNoiseMode = eNoiseThresholdModes.AbsoluteThreshold Then
            baselineNoiseStats = InitializeBaselineNoiseStats(
                baselineNoiseOptions.BaselineNoiseLevelAbsolute,
                baselineNoiseOptions.BaselineNoiseMode)

            Return True
        End If

        If baselineNoiseOptions.BaselineNoiseMode = eNoiseThresholdModes.DualTrimmedMeanByAbundance Then
            Return ComputeDualTrimmedNoiseLevel(sngData, 0, intDataCount - 1, baselineNoiseOptions, baselineNoiseStats)
        Else
            Return ComputeTrimmedNoiseLevel(sngData, 0, intDataCount - 1, baselineNoiseOptions, IGNORE_NON_POSITIVE_DATA, baselineNoiseStats)
        End If
    End Function

    <Obsolete("Use the version that takes a List(Of clsSICDataPoint")>
    Public Function ComputeNoiseLevelInPeakVicinity(
      intDataCount As Integer, SICScanNumbers() As Integer, SICIntensities() As Single,
      sicPeak As clsSICStatsPeak,
      baselineNoiseOptions As clsBaselineNoiseOptions) As Boolean

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To intDataCount - 1
            sicData.Add(New clsSICDataPoint(SICScanNumbers(index), SICIntensities(index), 0))
        Next

        Return ComputeNoiseLevelInPeakVicinity(sicData, sicPeak, baselineNoiseOptions)

    End Function

    Public Function ComputeNoiseLevelInPeakVicinity(
      sicData As List(Of clsSICDataPoint),
      sicPeak As clsSICStatsPeak,
      baselineNoiseOptions As clsBaselineNoiseOptions) As Boolean

        Const NOISE_ESTIMATE_DATA_COUNT_MINIMUM = 5
        Const NOISE_ESTIMATE_DATA_COUNT_MAXIMUM = 100

        Dim intIndexStart As Integer
        Dim intIndexEnd As Integer

        Dim intIndexBaseLeft As Integer
        Dim intIndexBaseRight As Integer

        Dim intPeakWidthPoints As Integer
        Dim intPeakHalfWidthPoints As Integer

        Dim intPeakWidthBaseScans As Integer        ' Minimum of peak width at 4 sigma vs. intPeakWidthFullScans

        Dim blnSuccess As Boolean
        Dim blnShiftLeft As Boolean

        ' Initialize udtBaselineNoiseStats
        sicPeak.BaselineNoiseStats = InitializeBaselineNoiseStats(
          baselineNoiseOptions.MinimumBaselineNoiseLevel,
          eNoiseThresholdModes.MeanOfDataInPeakVicinity)

        ' Only use a portion of the data to compute the noise level
        ' The number of points to extend from the left and right is based on the width at 4 sigma; useful for tailing peaks
        ' Also, determine the peak start using the smaller of the width at 4 sigma vs. the observed peak width

        ' Estimate FWHM since it is sometimes not yet known when this function is called
        ' The reason it's not yet know is that the final FWHM value is computed using baseline corrected intensity data, but
        '  the whole purpose of this function is to compute the baseline level
        sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, False)
        intPeakWidthBaseScans = ComputeWidthAtBaseUsingFWHM(sicPeak, sicData, 4)
        intPeakWidthPoints = ConvertScanWidthToPoints(intPeakWidthBaseScans, sicPeak, sicData)

        intPeakHalfWidthPoints = CInt(Math.Round(intPeakWidthPoints / 1.5, 0))

        ' Make sure that intPeakHalfWidthPoints is at least NOISE_ESTIMATE_DATA_COUNT_MINIMUM
        If intPeakHalfWidthPoints < NOISE_ESTIMATE_DATA_COUNT_MINIMUM Then
            intPeakHalfWidthPoints = NOISE_ESTIMATE_DATA_COUNT_MINIMUM
        End If

        ' Copy the peak base indices
        intIndexBaseLeft = sicPeak.IndexBaseLeft
        intIndexBaseRight = sicPeak.IndexBaseRight

        ' Define IndexStart and IndexEnd, making sure that intPeakHalfWidthPoints is no larger than NOISE_ESTIMATE_DATA_COUNT_MAXIMUM
        intIndexStart = intIndexBaseLeft - Math.Min(intPeakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM)
        intIndexEnd = sicPeak.IndexBaseRight + Math.Min(intPeakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM)

        If intIndexStart < 0 Then intIndexStart = 0
        If intIndexEnd >= sicData.Count Then intIndexEnd = sicData.Count - 1

        ' Compare intIndexStart to udtSICPeak.PreviousPeakFWHMPointRight
        ' If it is less than .PreviousPeakFWHMPointRight, then update accordingly
        If intIndexStart < sicPeak.PreviousPeakFWHMPointRight AndAlso
           sicPeak.PreviousPeakFWHMPointRight < sicPeak.IndexMax Then
            ' Update intIndexStart to be at PreviousPeakFWHMPointRight
            intIndexStart = sicPeak.PreviousPeakFWHMPointRight

            If intIndexStart < 0 Then intIndexStart = 0

            ' If not enough points, then alternately shift intIndexStart to the left 1 point and
            '  intIndexBaseLeft to the right one point until we do have enough points
            blnShiftLeft = True
            Do While intIndexBaseLeft - intIndexStart + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM
                If blnShiftLeft Then
                    If intIndexStart > 0 Then intIndexStart -= 1
                Else
                    If intIndexBaseLeft < sicPeak.IndexMax Then intIndexBaseLeft += 1
                End If
                If intIndexStart <= 0 AndAlso intIndexBaseLeft >= sicPeak.IndexMax Then
                    Exit Do
                Else
                    blnShiftLeft = Not blnShiftLeft
                End If
            Loop
        End If

        ' Compare intIndexEnd to udtSICPeak.NextPeakFWHMPointLeft
        ' If it is greater than .NextPeakFWHMPointLeft, then update accordingly
        If intIndexEnd >= sicPeak.NextPeakFWHMPointLeft AndAlso
           sicPeak.NextPeakFWHMPointLeft > sicPeak.IndexMax Then
            intIndexEnd = sicPeak.NextPeakFWHMPointLeft

            If intIndexEnd >= sicData.Count Then intIndexEnd = sicData.Count - 1

            ' If not enough points, then alternately shift intIndexEnd to the right 1 point and
            '  intIndexBaseRight to the left one point until we do have enough points
            blnShiftLeft = False
            Do While intIndexEnd - intIndexBaseRight + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM
                If blnShiftLeft Then
                    If intIndexBaseRight > sicPeak.IndexMax Then intIndexBaseRight -= 1
                Else
                    If intIndexEnd < sicData.Count - 1 Then intIndexEnd += 1
                End If
                If intIndexBaseRight <= sicPeak.IndexMax AndAlso intIndexEnd >= sicData.Count - 1 Then
                    Exit Do
                Else
                    blnShiftLeft = Not blnShiftLeft
                End If
            Loop
        End If

        blnSuccess = ComputeAverageNoiseLevelExcludingRegion(
          sicData,
          intIndexStart, intIndexEnd,
          intIndexBaseLeft, intIndexBaseRight,
          baselineNoiseOptions, sicPeak.BaselineNoiseStats)

        ' Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel
        If sicPeak.BaselineNoiseStats.NoiseLevel < Math.Max(1, baselineNoiseOptions.MinimumBaselineNoiseLevel) Then
            Dim udtNoiseStats = sicPeak.BaselineNoiseStats

            udtNoiseStats.NoiseLevel = Math.Max(1, baselineNoiseOptions.MinimumBaselineNoiseLevel)

            ' Set this to 0 since we have overridden .NoiseLevel
            udtNoiseStats.NoiseStDev = 0

            sicPeak.BaselineNoiseStats = udtNoiseStats
        End If


        Return blnSuccess

    End Function

    ''' <summary>
    ''' Determine the value for udtSICPeak.ParentIonIntensity
    ''' The goal is to determine the intensity that the SIC data has in one scan prior to udtSICPeak.IndexObserved
    ''' This intensity value may be an interpolated value between two observed SIC values
    ''' </summary>
    ''' <param name="intDataCount"></param>
    ''' <param name="SICScanNumbers">List of scan numbers</param>
    ''' <param name="SICIntensities">List of intensities</param>
    ''' <param name="sicPeak"></param>
    ''' <param name="intFragScanNumber"></param>
    ''' <returns></returns>
    <Obsolete("Use the version that takes a List(Of clsSICDataPoint")>
    Public Function ComputeParentIonIntensity(
                                              intDataCount As Integer,
                                              SICScanNumbers() As Integer,
                                              SICIntensities() As Single,
                                              sicPeak As clsSICStatsPeak,
                                              intFragScanNumber As Integer) As Boolean

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To intDataCount - 1
            sicData.Add(New clsSICDataPoint(SICScanNumbers(index), SICIntensities(index), 0))
        Next

        Return ComputeParentIonIntensity(sicData, sicPeak, intFragScanNumber)
    End Function

    ''' <summary>
    ''' Determine the value for udtSICPeak.ParentIonIntensity
    ''' The goal is to determine the intensity that the SIC data has in one scan prior to udtSICPeak.IndexObserved
    ''' This intensity value may be an interpolated value between two observed SIC values
    ''' </summary>
    ''' <param name="sicData"></param>
    ''' <param name="sicPeak"></param>
    ''' <param name="intFragScanNumber"></param>
    ''' <returns></returns>
    Public Function ComputeParentIonIntensity(
      sicData As IList(Of clsSICDataPoint),
      sicPeak As clsSICStatsPeak,
      intFragScanNumber As Integer) As Boolean

        Dim intX1, intX2 As Integer
        Dim sngY1, sngY2 As Single

        Dim blnSuccess As Boolean

        Try
            ' Lookup the scan number and intensity of the SIC scan at udtSICPeak.IndexObserved
            intX1 = sicData(sicPeak.IndexObserved).ScanNumber
            sngY1 = sicData(sicPeak.IndexObserved).Intensity

            If intX1 = intFragScanNumber - 1 Then
                ' The fragmentation scan was the next scan after the SIC scan the data was observed in
                ' We can use sngY1 for .ParentIonIntensity
                sicPeak.ParentIonIntensity = sngY1
            ElseIf intX1 >= intFragScanNumber Then
                ' The fragmentation scan has the same scan number as the SIC scan just before it, or the SIC scan is greater than the fragmentation scan
                ' This shouldn't normally happen, but we'll account for the possibility anyway
                ' If the data file only has MS spectra and no MS/MS spectra, and if the parent ion is a custom M/Z value, then this code will be reached
                sicPeak.ParentIonIntensity = sngY1
            Else
                ' We need to perform some interpolation to determine .ParentIonIntensity
                ' Lookup the scan number and intensity of the next SIC scan
                If sicPeak.IndexObserved < sicData.Count - 1 Then
                    intX2 = sicData(sicPeak.IndexObserved + 1).ScanNumber
                    sngY2 = sicData(sicPeak.IndexObserved + 1).Intensity

                    blnSuccess = InterpolateY(sicPeak.ParentIonIntensity, intX1, intX2, sngY1, sngY2, intFragScanNumber - 1)
                    If Not blnSuccess Then
                        ' Interpolation failed; use sngY1
                        sicPeak.ParentIonIntensity = sngY1
                    End If
                Else
                    ' Cannot interpolate; we'll have to use sngY1 as .ParentIonIntensity
                    sicPeak.ParentIonIntensity = sngY1
                End If
            End If

            blnSuccess = True

        Catch ex As Exception
            ' Ignore errors here
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function ComputeSICPeakArea(sicData As IList(Of clsSICDataPoint), sicPeak As clsSICStatsPeak) As Boolean
        ' The calling function must populate udtSICPeak.IndexMax, udtSICPeak.IndexBaseLeft, and udtSICPeak.IndexBaseRight

        Dim intAreaDataCount As Integer
        Dim intAreaDataBaseIndex As Integer

        Dim intScanNumbers() As Integer
        Dim sngIntensities() As Single

        Dim sngIntensityThreshold As Single

        Dim intDataIndex As Integer

        Dim intScanDelta As Integer
        Dim intAvgScanInterval As Integer

        Dim intIndexPointer As Integer

        Try

            ' Compute the peak area

            ' Copy the matching data from the source arrays to intScanNumbers() and sngIntensities
            ' When copying, assure that the first and last points have an intensity of 0

            ' We're reserving extra space in case we need to prepend or append a minimum value
            ReDim intScanNumbers(sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2)
            ReDim sngIntensities(sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2)

            ' Define an intensity threshold of 5% of MaximumIntensity
            ' If the peak data is not flanked by points <= sngIntensityThreshold, then we'll add them
            sngIntensityThreshold = CSng(sicData(sicPeak.IndexMax).Intensity * 0.05)

            ' Estimate the average scan interval between each data point
            intAvgScanInterval = CInt(Math.Round(ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight), 0))

            If sicData(sicPeak.IndexBaseLeft).Intensity > sngIntensityThreshold Then
                ' Prepend an intensity data point of sngIntensityThreshold, with a scan number intAvgScanInterval less than the first scan number for the actual peak data
                intScanNumbers(0) = sicData(sicPeak.IndexBaseLeft).ScanNumber - intAvgScanInterval
                sngIntensities(0) = sngIntensityThreshold
                'sngIntensitiesSmoothed(0) = sngIntensityThreshold
                intAreaDataBaseIndex = 1
            Else
                intAreaDataBaseIndex = 0
            End If

            ' Populate intScanNumbers() and sngIntensities()
            For intDataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                intIndexPointer = intDataIndex - sicPeak.IndexBaseLeft + intAreaDataBaseIndex
                intScanNumbers(intIndexPointer) = sicData(intDataIndex).ScanNumber
                sngIntensities(intIndexPointer) = sicData(intDataIndex).Intensity
                'sngIntensitiesSmoothed(intIndexPointer) = udtSmoothedYDataSubset.Data(intDataIndex - udtSmoothedYDataSubset.DataStartIndex)
                'If sngIntensitiesSmoothed(intIndexPointer) < 0 Then sngIntensitiesSmoothed(intIndexPointer) = 0
            Next
            intAreaDataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1 + intAreaDataBaseIndex

            If sicData(sicPeak.IndexBaseRight).Intensity > sngIntensityThreshold Then
                ' Append an intensity data point of sngIntensityThreshold, with a scan number intAvgScanInterval more than the last scan number for the actual peak data
                intDataIndex = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + intAreaDataBaseIndex + 1
                intScanNumbers(intDataIndex) = sicData(sicPeak.IndexBaseRight).ScanNumber + intAvgScanInterval
                sngIntensities(intDataIndex) = sngIntensityThreshold
                intAreaDataCount += 1
                'sngIntensitiesSmoothed(intDataIndex) = sngIntensityThreshold
            End If

            ' Compute the area
            ' Note that we're using real data for this and not smoothed data
            ' Also note that we're using raw data for the peak area (not baseline corrected values)
            Dim peakArea As Single = 0
            For intDataIndex = 0 To intAreaDataCount - 2
                ' Use the Trapezoid area formula to compute the area slice to add to udtSICPeak.Area
                ' Area = 0.5 * DeltaX * (Y1 + Y2)
                intScanDelta = intScanNumbers(intDataIndex + 1) - intScanNumbers(intDataIndex)
                peakArea += CSng(0.5 * intScanDelta * (sngIntensities(intDataIndex) + sngIntensities(intDataIndex + 1)))
            Next

            If peakArea < 0 Then
                sicPeak.Area = 0
            Else
                sicPeak.Area = peakArea
            End If

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->ComputeSICPeakArea", "Error computing area", ex, False)
            Return False
        End Try

        Return True

    End Function

    Private Function ComputeAvgScanInterval(sicData As IList(Of clsSICDataPoint), intDataIndexStart As Integer, intDataIndexEnd As Integer) As Single

        Dim sngScansPerPoint As Single

        Try
            ' Estimate the average scan interval between each data point
            If intDataIndexEnd >= intDataIndexStart Then
                sngScansPerPoint = CSng((sicData(intDataIndexEnd).ScanNumber - sicData(intDataIndexStart).ScanNumber) / (intDataIndexEnd - intDataIndexStart + 1))
                If sngScansPerPoint < 1 Then sngScansPerPoint = 1
            Else
                sngScansPerPoint = 1
            End If
        Catch ex As Exception
            sngScansPerPoint = 1
        End Try

        Return sngScansPerPoint

    End Function

    Private Function ComputeStatisticalMomentsStats(
      sicData As IList(Of clsSICDataPoint),
      smoothedYDataSubset As clsSmoothedYDataSubset,
      sicPeak As clsSICStatsPeak) As Boolean

        ' The calling function must populate udtSICPeak.IndexMax, udtSICPeak.IndexBaseLeft, and udtSICPeak.IndexBaseRight
        ' Returns True if success; false if an error or less than 3 usable data points

        Const ALLOW_NEGATIVE_VALUES = False
        Const USE_SMOOTHED_DATA = True
        Const DEFAULT_MINIMUM_DATA_COUNT = 5

        ' Note that we're using baseline corrected intensity values for the statistical moments
        ' However, it is important that we use continuous, positive data for computing statistical moments

        Try
            ' Initialize to default values

            Dim statMomentsData = New clsStatisticalMoments() With {
                .Area = 0,
                .StDev = 0,
                .Skew = 0,
                .KSStat = 0,
                .DataCountUsed = 0
            }

            Try
                If sicPeak.IndexMax >= 0 AndAlso sicPeak.IndexMax < sicData.Count Then
                    statMomentsData.CenterOfMassScan = sicData(sicPeak.IndexMax).ScanNumber
                End If
            Catch ex As Exception
                ' Ignore errors here
            End Try

            sicPeak.StatisticalMoments = statMomentsData

            Dim intDataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1
            If intDataCount < 1 Then
                ' Do not continue if less than one point across the peak
                Return False
            End If

            ' When reserving memory for these arrays, include room to add a minimum value at the beginning and end of the data, if needed
            ' Also, reserve space for a minimum of 5 elements
            Dim intMinimumDataCount = DEFAULT_MINIMUM_DATA_COUNT
            If intMinimumDataCount > intDataCount Then
                intMinimumDataCount = 3
            End If

            Dim intScanNumbers() As Integer         ' Contains values from sicData(x).ScanNumber
            Dim sngIntensities() As Single          ' Contains values from sicData(x).Intensity subtracted by the baseline noise level; if the result is less than 0, then will contain 0

            ReDim intScanNumbers(Math.Max(intDataCount, intMinimumDataCount) + 1)
            ReDim sngIntensities(intScanNumbers.Length - 1)
            Dim blnUseRawDataAroundMaximum = False

            ' Populate intScanNumbers() and sngIntensities()
            ' Simultaneously, determine the maximum intensity
            Dim sngMaximumBaselineAdjustedIntensity As Single = 0
            Dim intIndexMaximumIntensity = 0

            If USE_SMOOTHED_DATA Then
                intDataCount = 0
                For intDataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                    Dim intSmoothedDataPointer = intDataIndex - smoothedYDataSubset.DataStartIndex
                    If intSmoothedDataPointer >= 0 AndAlso intSmoothedDataPointer < smoothedYDataSubset.DataCount Then
                        intScanNumbers(intDataCount) = sicData(intDataIndex).ScanNumber
                        sngIntensities(intDataCount) = BaselineAdjustIntensity(
                          smoothedYDataSubset.Data(intSmoothedDataPointer),
                          sicPeak.BaselineNoiseStats.NoiseLevel,
                          ALLOW_NEGATIVE_VALUES)

                        If sngIntensities(intDataCount) > sngMaximumBaselineAdjustedIntensity Then
                            sngMaximumBaselineAdjustedIntensity = sngIntensities(intDataCount)
                            intIndexMaximumIntensity = intDataCount
                        End If
                        intDataCount += 1
                    End If
                Next
            Else
                intDataCount = 0
                For intDataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                    intScanNumbers(intDataCount) = sicData(intDataIndex).ScanNumber
                    sngIntensities(intDataCount) = BaselineAdjustIntensity(
                      sicData(intDataIndex).Intensity,
                      sicPeak.BaselineNoiseStats.NoiseLevel,
                      ALLOW_NEGATIVE_VALUES)

                    If sngIntensities(intDataCount) > sngMaximumBaselineAdjustedIntensity Then
                        sngMaximumBaselineAdjustedIntensity = sngIntensities(intDataCount)
                        intIndexMaximumIntensity = intDataCount
                    End If
                    intDataCount += 1
                Next
            End If

            ' Define an intensity threshold of 10% of MaximumBaselineAdjustedIntensity
            Dim sngIntensityThreshold = CSng(sngMaximumBaselineAdjustedIntensity * 0.1)
            If sngIntensityThreshold < 1 Then sngIntensityThreshold = 1

            ' Step left from intIndexMaximumIntensity to find the first data point < sngIntensityThreshold
            ' Note that the final data will include one data point less than sngIntensityThreshold at the beginning and end of the data
            Dim intValidDataIndexLeft = intIndexMaximumIntensity
            Do While intValidDataIndexLeft > 0 AndAlso sngIntensities(intValidDataIndexLeft) >= sngIntensityThreshold
                intValidDataIndexLeft -= 1
            Loop

            ' Step right from intIndexMaximumIntensity to find the first data point < sngIntensityThreshold
            Dim intValidDataIndexRight = intIndexMaximumIntensity
            Do While intValidDataIndexRight < intDataCount - 1 AndAlso sngIntensities(intValidDataIndexRight) >= sngIntensityThreshold
                intValidDataIndexRight += 1
            Loop

            If intValidDataIndexLeft > 0 OrElse intValidDataIndexRight < intDataCount - 1 Then
                ' Shrink the arrays to only retain the data centered around intIndexMaximumIntensity and
                '  having and intensity >= sngIntensityThreshold, though one additional data point is retained at the beginning and end of the data
                For intDataIndex = intValidDataIndexLeft To intValidDataIndexRight
                    Dim intIndexPointer = intDataIndex - intValidDataIndexLeft
                    intScanNumbers(intIndexPointer) = intScanNumbers(intDataIndex)
                    sngIntensities(intIndexPointer) = sngIntensities(intDataIndex)
                Next
                intDataCount = intValidDataIndexRight - intValidDataIndexLeft + 1
            End If

            If intDataCount < intMinimumDataCount Then
                blnUseRawDataAroundMaximum = True
            Else
                ' Remove the contiguous data from the left that is < sngIntensityThreshold, retaining one point < sngIntensityThreshold
                ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                intValidDataIndexLeft = 0
                Do While intValidDataIndexLeft < intDataCount - 1 AndAlso sngIntensities(intValidDataIndexLeft + 1) < sngIntensityThreshold
                    intValidDataIndexLeft += 1
                Loop

                If intValidDataIndexLeft >= intDataCount - 1 Then
                    ' All of the data is <= sngIntensityThreshold
                    blnUseRawDataAroundMaximum = True
                Else
                    If intValidDataIndexLeft > 0 Then
                        ' Shrink the array to remove the values at the beginning that are < sngIntensityThreshold, retaining one point < sngIntensityThreshold
                        ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                        For intDataIndex = intValidDataIndexLeft To intDataCount - 1
                            Dim intIndexPointer = intDataIndex - intValidDataIndexLeft
                            intScanNumbers(intIndexPointer) = intScanNumbers(intDataIndex)
                            sngIntensities(intIndexPointer) = sngIntensities(intDataIndex)
                        Next
                        intDataCount -= intValidDataIndexLeft
                    End If

                    ' Remove the contiguous data from the right that is < sngIntensityThreshold, retaining one point < sngIntensityThreshold
                    ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                    intValidDataIndexRight = intDataCount - 1
                    Do While intValidDataIndexRight > 0 AndAlso sngIntensities(intValidDataIndexRight - 1) < sngIntensityThreshold
                        intValidDataIndexRight -= 1
                    Loop

                    If intValidDataIndexRight < intDataCount - 1 Then
                        ' Shrink the array to remove the values at the end that are < sngIntensityThreshold, retaining one point < sngIntensityThreshold
                        ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                        intDataCount = intValidDataIndexRight + 1
                    End If

                    ' Estimate the average scan interval between the data points in intScanNumbers
                    Dim intAvgScanInterval = CInt(Math.Round(ComputeAvgScanInterval(sicData, 0, intDataCount - 1), 0))

                    ' Make sure that sngIntensities(0) is <= sngIntensityThreshold
                    If sngIntensities(0) > sngIntensityThreshold Then
                        ' Prepend a data point with intensity sngIntensityThreshold and with a scan number 1 less than the first scan number in the valid data
                        For intDataIndex = intDataCount To 1 Step -1
                            intScanNumbers(intDataIndex) = intScanNumbers(intDataIndex - 1)
                            sngIntensities(intDataIndex) = sngIntensities(intDataIndex - 1)
                        Next
                        intScanNumbers(0) = intScanNumbers(1) - intAvgScanInterval
                        sngIntensities(0) = sngIntensityThreshold
                        intDataCount += 1
                    End If

                    ' Make sure that sngIntensities(intDataCount-1) is <= sngIntensityThreshold
                    If sngIntensities(intDataCount - 1) > sngIntensityThreshold Then
                        ' Append a data point with intensity sngIntensityThreshold and with a scan number 1 more than the last scan number in the valid data
                        intScanNumbers(intDataCount) = intScanNumbers(intDataCount - 1) + intAvgScanInterval
                        sngIntensities(intDataCount) = sngIntensityThreshold
                        intDataCount += 1
                    End If

                End If
            End If

            If blnUseRawDataAroundMaximum OrElse intDataCount < intMinimumDataCount Then
                ' Populate intScanNumbers() and sngIntensities() with the five data points centered around udtSICPeak.IndexMax
                If USE_SMOOTHED_DATA Then
                    intValidDataIndexLeft = sicPeak.IndexMax - CInt(Math.Floor(intMinimumDataCount / 2))
                    If intValidDataIndexLeft < 0 Then intValidDataIndexLeft = 0
                    intDataCount = 0
                    For intDataIndex = intValidDataIndexLeft To Math.Min(intValidDataIndexLeft + intMinimumDataCount - 1, sicData.Count - 1)
                        Dim intSmoothedDataPointer = intDataIndex - smoothedYDataSubset.DataStartIndex
                        If intSmoothedDataPointer >= 0 AndAlso intSmoothedDataPointer < smoothedYDataSubset.DataCount Then
                            If smoothedYDataSubset.Data(intSmoothedDataPointer) > 0 Then
                                intScanNumbers(intDataCount) = sicData(intDataIndex).ScanNumber
                                sngIntensities(intDataCount) = smoothedYDataSubset.Data(intSmoothedDataPointer)
                                intDataCount += 1
                            End If
                        End If
                    Next
                Else
                    intValidDataIndexLeft = sicPeak.IndexMax - CInt(Math.Floor(intMinimumDataCount / 2))
                    If intValidDataIndexLeft < 0 Then intValidDataIndexLeft = 0
                    intDataCount = 0
                    For intDataIndex = intValidDataIndexLeft To Math.Min(intValidDataIndexLeft + intMinimumDataCount - 1, sicData.Count - 1)
                        If sicData(intDataIndex).Intensity > 0 Then
                            intScanNumbers(intDataCount) = sicData(intDataIndex).ScanNumber
                            sngIntensities(intDataCount) = sicData(intDataIndex).Intensity
                            intDataCount += 1
                        End If
                    Next
                End If

                If intDataCount < 3 Then
                    ' We don't even have 3 positive values in the raw data; do not continue
                    Return False
                End If
            End If

            ' Step through sngIntensities and interpolate across gaps with intensities of 0
            ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
            Dim pointIndex = 1
            Do While pointIndex < intDataCount - 1
                If sngIntensities(pointIndex) <= 0 Then
                    ' Current point has an intensity of 0
                    ' Find the next positive point
                    intValidDataIndexLeft = pointIndex + 1
                    Do While intValidDataIndexLeft < intDataCount AndAlso sngIntensities(intValidDataIndexLeft) <= 0
                        intValidDataIndexLeft += 1
                    Loop

                    ' Interpolate between pointIndex-1 and intValidDataIndexLeft
                    For intIndexPointer = pointIndex To intValidDataIndexLeft - 1
                        Dim sngInterpolatedIntensity As Single

                        If InterpolateY(
                          sngInterpolatedIntensity,
                          intScanNumbers(pointIndex - 1), intScanNumbers(intValidDataIndexLeft),
                          sngIntensities(pointIndex - 1), sngIntensities(intValidDataIndexLeft),
                          intScanNumbers(intIndexPointer)) Then
                            sngIntensities(intIndexPointer) = sngInterpolatedIntensity
                        End If
                    Next
                    pointIndex = intValidDataIndexLeft + 1
                Else
                    pointIndex += 1
                End If
            Loop

            ' Compute the zeroth moment (m0)
            Dim dblArea As Double = 0
            For intDataIndex = 0 To intDataCount - 2
                ' Use the Trapezoid area formula to compute the area slice to add to dblArea
                ' Area = 0.5 * DeltaX * (Y1 + Y2)
                Dim intScanDelta = intScanNumbers(intDataIndex + 1) - intScanNumbers(intDataIndex)
                dblArea += 0.5 * intScanDelta * (sngIntensities(intDataIndex) + sngIntensities(intDataIndex + 1))
            Next

            ' For the first moment (m1), need to sum: intensity times scan number.
            ' For each of the moments, need to subtract intScanNumbers(0) from the scan numbers since
            ' statistical moments calculations are skewed if the first X value is not zero.
            ' When ScanDelta is > 1, then need to interpolate.

            Dim dblMoment1Sum = (intScanNumbers(0) - intScanNumbers(0)) * sngIntensities(0)
            For intDataIndex = 1 To intDataCount - 1
                dblMoment1Sum += (intScanNumbers(intDataIndex) - intScanNumbers(0)) * sngIntensities(intDataIndex)

                Dim intScanDelta = intScanNumbers(intDataIndex) - intScanNumbers(intDataIndex - 1)
                If intScanDelta > 1 Then
                    ' Data points are more than 1 scan apart; need to interpolate values
                    ' However, no need to interpolate if both intensity values are 0
                    If sngIntensities(intDataIndex - 1) > 0 OrElse sngIntensities(intDataIndex) > 0 Then
                        For intScanNumberInterpolate = intScanNumbers(intDataIndex - 1) + 1 To intScanNumbers(intDataIndex) - 1
                            ' Use InterpolateY() to fill in the scans between intDataIndex-1 and intDataIndex
                            Dim sngInterpolatedIntensity As Single
                            If InterpolateY(
                              sngInterpolatedIntensity,
                              intScanNumbers(intDataIndex - 1), intScanNumbers(intDataIndex),
                              sngIntensities(intDataIndex - 1), sngIntensities(intDataIndex),
                              intScanNumberInterpolate) Then

                                dblMoment1Sum += (intScanNumberInterpolate - intScanNumbers(0)) * sngInterpolatedIntensity
                            End If
                        Next
                    End If
                End If
            Next

            If dblArea <= 0 Then
                ' Cannot compute the center of mass; use the scan at .IndexMax instead
                Dim dblCenterOfMassDecimal As Double

                Dim intIndexPointer = sicPeak.IndexMax - sicPeak.IndexBaseLeft
                If intIndexPointer >= 0 AndAlso intIndexPointer < intScanNumbers.Length Then
                    dblCenterOfMassDecimal = intScanNumbers(intIndexPointer)
                End If

                statMomentsData.CenterOfMassScan = CInt(Math.Round(dblCenterOfMassDecimal, 0))
                statMomentsData.DataCountUsed = 1

            Else
                ' Area is positive; compute the center of mass

                Dim dblCenterOfMassDecimal = dblMoment1Sum / dblArea + intScanNumbers(0)

                statMomentsData.Area = CSng(Math.Min(Single.MaxValue, dblArea))
                statMomentsData.CenterOfMassScan = CInt(Math.Round(dblCenterOfMassDecimal, 0))
                statMomentsData.DataCountUsed = intDataCount

                ' For the second moment (m2), need to sum: (ScanNumber - m1)^2 * Intensity
                ' For the third moment (m3), need to sum: (ScanNumber - m1)^3 * Intensity
                ' When ScanDelta is > 1, then need to interpolate
                Dim dblMoment2Sum = ((intScanNumbers(0) - dblCenterOfMassDecimal) ^ 2) * sngIntensities(0)
                Dim dblMoment3Sum = ((intScanNumbers(0) - dblCenterOfMassDecimal) ^ 3) * sngIntensities(0)
                For intDataIndex = 1 To intDataCount - 1
                    dblMoment2Sum += ((intScanNumbers(intDataIndex) - dblCenterOfMassDecimal) ^ 2) * sngIntensities(intDataIndex)
                    dblMoment3Sum += ((intScanNumbers(intDataIndex) - dblCenterOfMassDecimal) ^ 3) * sngIntensities(intDataIndex)

                    Dim intScanDelta = intScanNumbers(intDataIndex) - intScanNumbers(intDataIndex - 1)
                    If intScanDelta > 1 Then
                        ' Data points are more than 1 scan apart; need to interpolate values
                        ' However, no need to interpolate if both intensity values are 0
                        If sngIntensities(intDataIndex - 1) > 0 OrElse sngIntensities(intDataIndex) > 0 Then
                            For intScanNumberInterpolate = intScanNumbers(intDataIndex - 1) + 1 To intScanNumbers(intDataIndex) - 1
                                ' Use InterpolateY() to fill in the scans between intDataIndex-1 and intDataIndex
                                Dim sngInterpolatedIntensity As Single
                                If InterpolateY(
                                  sngInterpolatedIntensity,
                                  intScanNumbers(intDataIndex - 1), intScanNumbers(intDataIndex),
                                  sngIntensities(intDataIndex - 1), sngIntensities(intDataIndex),
                                  intScanNumberInterpolate) Then

                                    dblMoment2Sum += ((intScanNumberInterpolate - dblCenterOfMassDecimal) ^ 2) * sngInterpolatedIntensity
                                    dblMoment3Sum += ((intScanNumberInterpolate - dblCenterOfMassDecimal) ^ 3) * sngInterpolatedIntensity
                                End If
                            Next
                        End If
                    End If
                Next

                statMomentsData.StDev = CSng(Math.Sqrt(dblMoment2Sum / dblArea))

                ' dblThirdMoment = dblMoment3Sum / dblArea
                ' skew = dblThirdMoment / sigma^3
                ' skew = (dblMoment3Sum / dblArea) / sigma^3
                If statMomentsData.StDev > 0 Then
                    statMomentsData.Skew = CSng((dblMoment3Sum / dblArea) / (statMomentsData.StDev ^ 3))
                    If Math.Abs(statMomentsData.Skew) < 0.0001 Then
                        statMomentsData.Skew = 0
                    End If
                Else
                    statMomentsData.Skew = 0
                End If

            End If

            Const blnUseStatMomentsStats = True
            Dim peakMean As Single
            Dim peakStDev As Double

            If blnUseStatMomentsStats Then
                peakMean = statMomentsData.CenterOfMassScan
                peakStDev = statMomentsData.StDev
            Else
                peakMean = sicData(sicPeak.IndexMax).ScanNumber
                ' FWHM / 2.35482 = FWHM / (2 * Sqrt(2 * Ln(2)))
                peakStDev = sicPeak.FWHMScanWidth / 2.35482
            End If
            statMomentsData.KSStat = CSng(ComputeKSStatistic(intDataCount, intScanNumbers, sngIntensities, peakMean, peakStDev))


        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->ComputeStatisticalMomentsStats", "Error computing statistical moments", ex, False)
            Return False
        End Try

        Return True

    End Function

    Public Shared Function ComputeSignalToNoise(sngSignal As Single, sngNoiseThresholdIntensity As Single) As Single

        If sngNoiseThresholdIntensity > 0 Then
            Return sngSignal / sngNoiseThresholdIntensity
        Else
            Return 0
        End If

    End Function

    ''' <summary>
    ''' Computes a trimmed mean or trimmed median using the low intensity data up to baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
    ''' Additionally, computes a full median using all data in sngData
    ''' If blnIgnoreNonPositiveData is True, then removes data from sngData() less than zero 0 and less than .MinimumBaselineNoiseLevel
    ''' </summary>
    ''' <param name="sngData"></param>
    ''' <param name="intIndexStart"></param>
    ''' <param name="intIndexEnd"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="blnIgnoreNonPositiveData"></param>
    ''' <param name="baselineNoiseStats"></param>
    ''' <returns>Returns True if success, False if error (or no data in sngData)</returns>
    ''' <remarks>
    ''' Replaces values of 0 with the minimum positive value in sngData()
    ''' You cannot use sngData.Length to determine the length of the array; use intDataCount
    ''' </remarks>
    Public Function ComputeTrimmedNoiseLevel(sngData() As Single, intIndexStart As Integer, intIndexEnd As Integer,
                                             baselineNoiseOptions As clsBaselineNoiseOptions,
                                             blnIgnoreNonPositiveData As Boolean,
                                             <Out> ByRef baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        Dim intDataSortedCount As Integer
        Dim sngDataSorted() As Single           ' Note: You cannot use sngDataSorted.Length to determine the length of the array; use intIndexStart and intIndexEnd to find the limits

        Dim dblIntensityThreshold As Double
        Dim dblSum As Double

        Dim intIndex As Integer
        Dim intValidDataCount As Integer

        Dim intCountSummed As Integer

        ' Initialize baselineNoiseStats
        baselineNoiseStats = InitializeBaselineNoiseStats(baselineNoiseOptions.MinimumBaselineNoiseLevel, baselineNoiseOptions.BaselineNoiseMode)

        If sngData Is Nothing OrElse intIndexEnd - intIndexStart < 0 Then
            Return False
        End If

        ' Copy the data into sngDataSorted
        intDataSortedCount = intIndexEnd - intIndexStart + 1
        ReDim sngDataSorted(intDataSortedCount - 1)

        If intIndexStart = 0 Then
            Array.Copy(sngData, sngDataSorted, intDataSortedCount)
        Else
            For intIndex = intIndexStart To intIndexEnd
                sngDataSorted(intIndex - intIndexStart) = sngData(intIndex)
            Next
        End If

        ' Sort the array
        Array.Sort(sngDataSorted)

        If blnIgnoreNonPositiveData Then
            ' Remove data with a value <= 0

            If sngDataSorted(0) <= 0 Then
                intValidDataCount = 0
                For intIndex = 0 To intDataSortedCount - 1
                    If sngDataSorted(intIndex) > 0 Then
                        sngDataSorted(intValidDataCount) = sngDataSorted(intIndex)
                        intValidDataCount += 1
                    End If
                Next

                If intValidDataCount < intDataSortedCount Then
                    intDataSortedCount = intValidDataCount
                End If

                ' Check for no data remaining
                If intDataSortedCount <= 0 Then
                    Return False
                End If
            End If
        End If

        ' Look for the minimum positive value and replace all data in sngDataSorted with that value
        Dim minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(intDataSortedCount, sngDataSorted)

        Select Case baselineNoiseOptions.BaselineNoiseMode
            Case eNoiseThresholdModes.TrimmedMeanByAbundance, eNoiseThresholdModes.TrimmedMeanByCount

                If baselineNoiseOptions.BaselineNoiseMode = eNoiseThresholdModes.TrimmedMeanByAbundance Then
                    ' Average the data that has intensity values less than
                    '  Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)
                    With baselineNoiseOptions
                        dblIntensityThreshold = sngDataSorted(0) + .TrimmedMeanFractionLowIntensityDataToAverage *
                                                (sngDataSorted(intDataSortedCount - 1) - sngDataSorted(0))
                    End With

                    ' Initialize intCountSummed to intDataSortedCount for now, in case all data is within the intensity threshold
                    intCountSummed = intDataSortedCount
                    dblSum = 0
                    For intIndex = 0 To intDataSortedCount - 1
                        If sngDataSorted(intIndex) <= dblIntensityThreshold Then
                            dblSum += sngDataSorted(intIndex)
                        Else
                            ' Update intCountSummed
                            intCountSummed = intIndex
                            Exit For
                        End If
                    Next
                    intIndexEnd = intCountSummed - 1
                Else
                    ' eNoiseThresholdModes.TrimmedMeanByCount
                    ' Find the index of the data point at intDataSortedCount * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage and
                    '  average the data from the start to that index
                    intIndexEnd = CInt(Math.Round((intDataSortedCount - 1) * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 0))

                    intCountSummed = intIndexEnd + 1
                    dblSum = 0
                    For intIndex = 0 To intIndexEnd
                        dblSum += sngDataSorted(intIndex)
                    Next
                End If

                If intCountSummed > 0 Then
                    ' Compute the average
                    ' Note that intCountSummed will be used below in the variance computation
                    baselineNoiseStats.NoiseLevel = CSng(dblSum / intCountSummed)
                    baselineNoiseStats.PointsUsed = intCountSummed

                    If intCountSummed > 1 Then
                        ' Compute the variance
                        dblSum = 0
                        For intIndex = 0 To intIndexEnd
                            dblSum += (sngDataSorted(intIndex) - baselineNoiseStats.NoiseLevel) ^ 2
                        Next
                        baselineNoiseStats.NoiseStDev = CSng(Math.Sqrt(dblSum / (intCountSummed - 1)))
                    Else
                        baselineNoiseStats.NoiseStDev = 0
                    End If
                Else
                    ' No data to average; define the noise level to be the minimum intensity
                    baselineNoiseStats.NoiseLevel = sngDataSorted(0)
                    baselineNoiseStats.NoiseStDev = 0
                    baselineNoiseStats.PointsUsed = 1

                End If

            Case eNoiseThresholdModes.TrimmedMedianByAbundance
                If baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage >= 1 Then
                    intIndexEnd = intDataSortedCount - 1
                Else
                    'Find the median of the data that has intensity values less than
                    '  Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)
                    With baselineNoiseOptions
                        dblIntensityThreshold = sngDataSorted(0) + .TrimmedMeanFractionLowIntensityDataToAverage *
                                                (sngDataSorted(intDataSortedCount - 1) - sngDataSorted(0))
                    End With

                    ' Find the first point with an intensity value <= dblIntensityThreshold
                    intIndexEnd = intDataSortedCount - 1
                    For intIndex = 1 To intDataSortedCount - 1
                        If sngDataSorted(intIndex) > dblIntensityThreshold Then
                            intIndexEnd = intIndex - 1
                            Exit For
                        End If
                    Next
                End If

                If intIndexEnd Mod 2 = 0 Then
                    ' Even value
                    baselineNoiseStats.NoiseLevel = sngDataSorted(CInt(intIndexEnd / 2))
                Else
                    ' Odd value; average the values on either side of intIndexEnd/2
                    intIndex = CInt((intIndexEnd - 1) / 2)
                    If intIndex < 0 Then intIndex = 0
                    dblSum = sngDataSorted(intIndex)

                    intIndex += 1
                    If intIndex = intDataSortedCount Then intIndex = intDataSortedCount - 1
                    dblSum += sngDataSorted(intIndex)

                    baselineNoiseStats.NoiseLevel = CSng(dblSum / 2.0)
                End If

                ' Compute the variance
                dblSum = 0
                For intIndex = 0 To intIndexEnd
                    dblSum += (sngDataSorted(intIndex) - baselineNoiseStats.NoiseLevel) ^ 2
                Next

                intCountSummed = intIndexEnd + 1
                If intCountSummed > 0 Then
                    baselineNoiseStats.NoiseStDev = CSng(Math.Sqrt(dblSum / (intCountSummed - 1)))
                Else
                    baselineNoiseStats.NoiseStDev = 0
                End If
                baselineNoiseStats.PointsUsed = intCountSummed
            Case Else
                ' Unknown mode
                LogErrors("clsMASICPeakFinder->ComputeTrimmedNoiseLevel",
                          "Unknown Noise Threshold Mode encountered: " & baselineNoiseOptions.BaselineNoiseMode.ToString,
                          Nothing, False)
                Return False
        End Select

        ' Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel
        If baselineNoiseStats.NoiseLevel < baselineNoiseOptions.MinimumBaselineNoiseLevel AndAlso
           baselineNoiseOptions.MinimumBaselineNoiseLevel > 0 Then

            baselineNoiseStats.NoiseLevel = baselineNoiseOptions.MinimumBaselineNoiseLevel

            ' Set this to 0 since we have overridden .NoiseLevel
            baselineNoiseStats.NoiseStDev = 0
        End If

        Return True

    End Function

    ''' <summary>
    ''' Computes the width of the peak (in scans) using the FWHM value in udtSICPeak
    ''' </summary>
    ''' <param name="sicPeak"></param>
    ''' <param name="sicData"></param>
    ''' <param name="sigmaValueForBase"></param>
    ''' <returns></returns>
    Private Shared Function ComputeWidthAtBaseUsingFWHM(
      sicPeak As clsSICStatsPeak,
      sicData As IList(Of clsSICDataPoint),
      sigmaValueForBase As Short) As Integer

        Dim intPeakWidthFullScans As Integer

        Try
            intPeakWidthFullScans = sicData(sicPeak.IndexBaseRight).ScanNumber - sicData(sicPeak.IndexBaseLeft).ScanNumber + 1
            Return ComputeWidthAtBaseUsingFWHM(sicPeak.FWHMScanWidth, intPeakWidthFullScans, sigmaValueForBase)
        Catch ex As Exception
            Return 0
        End Try

    End Function

    ''' <summary>
    '''  Computes the width of the peak (in scans) using the FWHM value
    ''' </summary>
    ''' <param name="intSICPeakFWHMScans"></param>
    ''' <param name="intSICPeakWidthFullScans"></param>
    ''' <param name="sigmaValueForBase"></param>
    ''' <returns></returns>
    ''' <remarks>Does not allow the width determined to be larger than intSICPeakWidthFullScans</remarks>
    Private Shared Function ComputeWidthAtBaseUsingFWHM(
      intSICPeakFWHMScans As Integer,
      intSICPeakWidthFullScans As Integer,
      Optional sigmaValueForBase As Short = 4) As Integer

        Dim intWidthAtBase As Integer
        Dim intSigmaBasedWidth As Integer

        If sigmaValueForBase < 4 Then sigmaValueForBase = 4

        If intSICPeakFWHMScans = 0 Then
            intWidthAtBase = intSICPeakWidthFullScans
        Else
            ' Compute the peak width
            ' Note: Sigma = FWHM / 2.35482 = FWHM / (2 * Sqrt(2 * Ln(2)))
            intSigmaBasedWidth = CInt(sigmaValueForBase * intSICPeakFWHMScans / 2.35482)

            If intSigmaBasedWidth <= 0 Then
                intWidthAtBase = intSICPeakWidthFullScans
            ElseIf intSICPeakWidthFullScans = 0 Then
                intWidthAtBase = intSigmaBasedWidth
            Else
                ' Compare the sigma-based peak width to intSICPeakWidthFullScans
                ' Assign the smaller of the two values to intWidthAtBase
                intWidthAtBase = Math.Min(intSigmaBasedWidth, intSICPeakWidthFullScans)
            End If
        End If

        Return intWidthAtBase

    End Function

    ''' <summary>
    ''' Convert from intPeakWidthFullScans to points; estimate number of scans per point to get this
    ''' </summary>
    ''' <param name="intPeakWidthBaseScans"></param>
    ''' <param name="sicPeak"></param>
    ''' <param name="sicData"></param>
    ''' <returns></returns>
    Private Function ConvertScanWidthToPoints(
      intPeakWidthBaseScans As Integer,
      sicPeak As clsSICStatsPeak,
      sicData As IList(Of clsSICDataPoint)) As Integer

        Dim sngScansPerPoint As Single

        sngScansPerPoint = ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight)
        Return CInt(Math.Round(intPeakWidthBaseScans / sngScansPerPoint, 0))

    End Function

    Public Function FindMinimumPositiveValue(sicData As IList(Of clsSICDataPoint), absoluteMinimumValue As Single) As Single

        Dim minimumPositiveValue = (From item In sicData Where item.Intensity > 0 Select item.Intensity).Min()

        If minimumPositiveValue < absoluteMinimumValue Then
            Return absoluteMinimumValue
        End If

        Return minimumPositiveValue
    End Function

    Public Function FindMinimumPositiveValue(intDataCount As Integer, sngData() As Single, absoluteMinimumValue As Single) As Single
        ' Note: Do not use sngData.Length to determine the length of the array; use intDataCount
        ' However, if intDataCount is > sngData.Length then sngData.Length-1 will be used for the maximum index to examine

        Dim intIndex As Integer
        Dim minimumPositiveValue As Single

        If intDataCount > sngData.Length Then
            intDataCount = sngData.Length
        End If

        ' Find the minimum positive value in sngData
        minimumPositiveValue = Single.MaxValue
        For intIndex = 0 To intDataCount - 1
            If sngData(intIndex) > 0 Then
                If sngData(intIndex) < minimumPositiveValue Then
                    minimumPositiveValue = sngData(intIndex)
                End If
            End If
        Next

        If minimumPositiveValue >= Single.MaxValue OrElse minimumPositiveValue < absoluteMinimumValue Then
            minimumPositiveValue = absoluteMinimumValue
        End If

        Return minimumPositiveValue

    End Function

    ''' <summary>
    ''' Find peaks in the scan/intensity data tracked by sicData
    ''' </summary>
    ''' <param name="sicData"></param>
    ''' <param name="intPeakIndexStart">Output</param>
    ''' <param name="intPeakIndexEnd">Output</param>
    ''' <param name="intPeakLocationIndex">Output</param>
    ''' <param name="intPreviousPeakFWHMPointRight">Output</param>
    ''' <param name="intNextPeakFWHMPointLeft">Output</param>
    ''' <param name="intShoulderCount">Output</param>
    ''' <param name="smoothedYDataSubset">Output</param>
    ''' <param name="simDataPresent"></param>
    ''' <param name="sicPeakFinderOptions"></param>
    ''' <param name="sngSICNoiseThresholdIntensity"></param>
    ''' <param name="dblMinimumPotentialPeakArea"></param>
    ''' <param name="returnClosestPeak">
    ''' When true, intPeakLocationIndex should be populated with the "best guess" location of the peak in the intScanNumbers() and sngIntensityData() arrays
    ''' The peak closest to intPeakLocationIndex will be the chosen peak, even if it is not the most intense peak found
    ''' </param>
    ''' <returns>Returns True if a valid peak is found in sngIntensityData(), otherwise false</returns>
    Private Function FindPeaks(
      sicData As IList(Of clsSICDataPoint),
      ByRef intPeakIndexStart As Integer,
      ByRef intPeakIndexEnd As Integer,
      ByRef intPeakLocationIndex As Integer,
      ByRef intPreviousPeakFWHMPointRight As Integer,
      ByRef intNextPeakFWHMPointLeft As Integer,
      ByRef intShoulderCount As Integer,
      <Out> ByRef smoothedYDataSubset As clsSmoothedYDataSubset,
      simDataPresent As Boolean,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      sngSICNoiseThresholdIntensity As Single,
      dblMinimumPotentialPeakArea As Double,
      returnClosestPeak As Boolean) As Boolean

        Const SMOOTHED_DATA_PADDING_COUNT = 2

        Dim blnValidPeakFound As Boolean

        smoothedYDataSubset = New clsSmoothedYDataSubset()

        Try
            Dim objPeakDetector As New clsPeakDetection()

            Dim peakData = New clsPeaksContainer() With {
                .SourceDataCount = sicData.Count
            }

            If peakData.SourceDataCount <= 1 Then
                ' Only 1 or fewer points in sngIntensityData()
                ' No point in looking for a "peak"
                intPeakIndexStart = 0
                intPeakIndexEnd = 0
                intPeakLocationIndex = 0

                Return False
            End If

            ' Try to find the peak using the Peak Detector class
            ' First need to populate .XData() and copy from sngIntensityData() to .YData()
            ' At the same time, find dblMaximumIntensity and dblMaximumPotentialPeakArea

            ' The peak finder class requires Arrays of type Double
            ' Copy the data from the source arrays into udtPeakData.XData() and udtPeakData.YData()
            ReDim peakData.XData(peakData.SourceDataCount - 1)
            ReDim peakData.YData(peakData.SourceDataCount - 1)

            Dim scanNumbers = (From item In sicData Select item.ScanNumber).ToArray()

            Dim dblMaximumIntensity As Double = sicData(0).Intensity
            Dim dblMaximumPotentialPeakArea As Double = 0
            Dim intIndexMaxIntensity = 0

            ' Initialize the intensity queue
            ' The queue is used to keep track of the most recent intensity values
            Dim queIntensityList As New Queue()

            Dim dblPotentialPeakArea As Double = 0
            Dim intDataPointCountAboveThreshold = 0

            For intIndex = 0 To peakData.SourceDataCount - 1
                peakData.XData(intIndex) = sicData(intIndex).ScanNumber
                peakData.YData(intIndex) = sicData(intIndex).Intensity
                If peakData.YData(intIndex) > dblMaximumIntensity Then
                    dblMaximumIntensity = peakData.YData(intIndex)
                    intIndexMaxIntensity = intIndex
                End If

                If sicData(intIndex).Intensity >= sngSICNoiseThresholdIntensity Then
                    ' Add this intensity to dblPotentialPeakArea
                    dblPotentialPeakArea += sicData(intIndex).Intensity
                    If queIntensityList.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum Then
                        ' Decrement dblPotentialPeakArea by the oldest item in the queue
                        dblPotentialPeakArea -= CDbl(queIntensityList.Dequeue())
                    End If
                    ' Add this intensity to the queue
                    queIntensityList.Enqueue(sicData(intIndex).Intensity)

                    If dblPotentialPeakArea > dblMaximumPotentialPeakArea Then
                        dblMaximumPotentialPeakArea = dblPotentialPeakArea
                    End If

                    intDataPointCountAboveThreshold += 1

                End If
            Next

            ' Determine the initial value for .PeakWidthPointsMinimum
            ' We will use dblMaximumIntensity and sngMinimumPeakIntensity to compute a S/N value to help pick .PeakWidthPointsMinimum

            ' Old: If sicPeakFinderOptions.SICNoiseThresholdIntensity < 1 Then sicPeakFinderOptions.SICNoiseThresholdIntensity = 1
            ' Old: dblAreaSignalToNoise = dblMaximumIntensity / sicPeakFinderOptions.SICNoiseThresholdIntensity

            If dblMinimumPotentialPeakArea < 1 Then dblMinimumPotentialPeakArea = 1
            Dim dblAreaSignalToNoise = dblMaximumPotentialPeakArea / dblMinimumPotentialPeakArea
            If dblAreaSignalToNoise < 1 Then dblAreaSignalToNoise = 1


            If Math.Abs(sicPeakFinderOptions.ButterworthSamplingFrequency) < Single.Epsilon Then
                sicPeakFinderOptions.ButterworthSamplingFrequency = 0.25
            End If

            peakData.PeakWidthPointsMinimum = CInt(sicPeakFinderOptions.InitialPeakWidthScansScaler *
                                                   Math.Log10(Math.Floor(dblAreaSignalToNoise)) * 10)

            ' Assure that .InitialPeakWidthScansMaximum is no greater than .InitialPeakWidthScansMaximum
            '  and no greater than intDataPointCountAboveThreshold/2 (rounded up)
            peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, sicPeakFinderOptions.InitialPeakWidthScansMaximum)
            peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, CInt(Math.Ceiling(intDataPointCountAboveThreshold / 2)))

            If peakData.PeakWidthPointsMinimum > peakData.SourceDataCount * 0.8 Then
                peakData.PeakWidthPointsMinimum = CInt(Math.Floor(peakData.SourceDataCount * 0.8))
            End If

            If peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH Then peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH

            ' Save the original value for intPeakLocationIndex
            peakData.OriginalPeakLocationIndex = intPeakLocationIndex
            peakData.MaxAllowedUpwardSpikeFractionMax = sicPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax

            Do
                Dim blnTestingMinimumPeakWidth As Boolean

                If peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH Then
                    blnTestingMinimumPeakWidth = True
                Else
                    blnTestingMinimumPeakWidth = False
                End If

                Try
                    blnValidPeakFound = FindPeaksWork(
                      objPeakDetector, scanNumbers, peakData,
                      simDataPresent, sicPeakFinderOptions,
                      blnTestingMinimumPeakWidth, returnClosestPeak)

                Catch ex As Exception
                    LogErrors("clsMASICPeakFinder->FindPeaks", "Error calling FindPeaksWork", ex, True)
                    blnValidPeakFound = False
                    Exit Do
                End Try

                If blnValidPeakFound Then

                    ' For each peak, see if several zero intensity values are in a row in the raw data
                    ' If found, then narrow the peak to leave just one zero intensity value
                    For intPeakIndexCompare = 0 To peakData.Peaks.Count - 1
                        Dim currentPeak = peakData.Peaks(intPeakIndexCompare)

                        Do While currentPeak.LeftEdge < sicData.Count - 1 AndAlso
                            currentPeak.LeftEdge < currentPeak.RightEdge
                            If Math.Abs(sicData(currentPeak.LeftEdge).Intensity) < Single.Epsilon AndAlso
                                Math.Abs(sicData(currentPeak.LeftEdge + 1).Intensity) < Single.Epsilon Then
                                currentPeak.LeftEdge += 1
                            Else
                                Exit Do
                            End If
                        Loop

                        Do While currentPeak.RightEdge > 0 AndAlso
                            currentPeak.RightEdge > currentPeak.LeftEdge
                            If Math.Abs(sicData(currentPeak.RightEdge).Intensity) < Single.Epsilon AndAlso
                                Math.Abs(sicData(currentPeak.RightEdge - 1).Intensity) < Single.Epsilon Then
                                currentPeak.RightEdge -= 1
                            Else
                                Exit Do
                            End If
                        Loop

                    Next

                    ' Update the stats for the "official" peak
                    Dim bestPeak = peakData.Peaks(peakData.BestPeakIndex)

                    intPeakLocationIndex = bestPeak.PeakLocation
                    intPeakIndexStart = bestPeak.LeftEdge
                    intPeakIndexEnd = bestPeak.RightEdge

                    ' Copy the smoothed Y data for the peak into udtSmoothedYDataSubset.Data()
                    ' Include some data to the left and right of the peak start and end
                    ' Additionally, be sure the smoothed data includes the data around the most intense data point in sngIntensityData
                    Dim smoothedYDataStartIndex = intPeakIndexStart - SMOOTHED_DATA_PADDING_COUNT
                    Dim smoothedYDataEndIndex = intPeakIndexEnd + SMOOTHED_DATA_PADDING_COUNT

                    ' Make sure the maximum intensity point is included (with padding on either side)
                    If intIndexMaxIntensity - SMOOTHED_DATA_PADDING_COUNT < smoothedYDataStartIndex Then
                        smoothedYDataStartIndex = intIndexMaxIntensity - SMOOTHED_DATA_PADDING_COUNT
                    End If

                    If intIndexMaxIntensity + SMOOTHED_DATA_PADDING_COUNT > smoothedYDataEndIndex Then
                        smoothedYDataEndIndex = intIndexMaxIntensity + SMOOTHED_DATA_PADDING_COUNT
                    End If

                    ' Make sure the indices aren't out of range
                    If smoothedYDataStartIndex < 0 Then
                        smoothedYDataStartIndex = 0
                    End If

                    If smoothedYDataEndIndex >= sicData.Count Then
                        smoothedYDataEndIndex = sicData.Count - 1
                    End If

                    ' Copy the smoothed data into smoothedYDataSubset
                    smoothedYDataSubset = New clsSmoothedYDataSubset(peakData.SmoothedYData, smoothedYDataStartIndex, smoothedYDataEndIndex)

                    ' Copy the peak location info into peakDataSaved since we're going to call FindPeaksWork again and the data will get overwritten
                    Dim peakDataSaved = peakData.Clone(True)

                    If peakData.PeakWidthPointsMinimum <> MINIMUM_PEAK_WIDTH Then
                        ' Determine the number of shoulder peaks for this peak
                        ' Use a minimum peak width of MINIMUM_PEAK_WIDTH and use a Max Allow Upward Spike Fraction of just 0.05 (= 5%)
                        peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH
                        If peakData.MaxAllowedUpwardSpikeFractionMax > 0.05 Then
                            peakData.MaxAllowedUpwardSpikeFractionMax = 0.05
                        End If
                        blnValidPeakFound = FindPeaksWork(
                          objPeakDetector, scanNumbers, peakData,
                          simDataPresent, sicPeakFinderOptions,
                          True, returnClosestPeak)

                        If blnValidPeakFound Then
                            intShoulderCount = 0

                            For Each peakItem In peakData.Peaks
                                If peakItem.PeakLocation >= intPeakIndexStart AndAlso peakItem.PeakLocation <= intPeakIndexEnd Then
                                    ' The peak at intIndex has a peak center between the "official" peak's boundaries
                                    ' Make sure it's not the same peak as the "official" peak
                                    If peakItem.PeakLocation <> intPeakLocationIndex Then
                                        ' Now see if the comparison peak's intensity is at least .IntensityThresholdFractionMax of the intensity of the "official" peak
                                        If sicData(peakItem.PeakLocation).Intensity >= sicPeakFinderOptions.IntensityThresholdFractionMax * sicData(intPeakLocationIndex).Intensity Then
                                            ' Yes, this is a shoulder peak
                                            intShoulderCount += 1
                                        End If
                                    End If
                                End If

                            Next
                        End If
                    Else
                        intShoulderCount = 0
                    End If

                    ' Make sure intPeakLocationIndex really is the point with the highest intensity (in the smoothed data)
                    dblMaximumIntensity = peakData.SmoothedYData(intPeakLocationIndex)
                    For intIndex = intPeakIndexStart To intPeakIndexEnd
                        If peakData.SmoothedYData(intIndex) > dblMaximumIntensity Then
                            ' A more intense data point was found; update intPeakLocationIndex
                            dblMaximumIntensity = peakData.SmoothedYData(intIndex)
                            intPeakLocationIndex = intIndex
                        End If
                    Next


                    Dim intComparisonPeakEdgeIndex As Integer
                    Dim sngTargetIntensity As Single
                    Dim intDataIndex As Integer

                    ' Populate intPreviousPeakFWHMPointRight and intNextPeakFWHMPointLeft
                    Dim sngAdjacentPeakIntensityThreshold = sicData(intPeakLocationIndex).Intensity / 3

                    ' Search through peakDataSaved to find the closest peak (with a significant intensity) to the left of this peak
                    ' Note that the peaks in peakDataSaved are not necessarily ordered by increasing index,
                    '  thus the need for an exhaustive search

                    Dim intSmallestIndexDifference = sicData.Count + 1
                    For intPeakIndexCompare = 0 To peakDataSaved.Peaks.Count - 1
                        Dim comparisonPeak = peakDataSaved.Peaks(intPeakIndexCompare)

                        If intPeakIndexCompare <> peakDataSaved.BestPeakIndex AndAlso
                           comparisonPeak.PeakLocation <= intPeakIndexStart Then
                            ' The peak is before intPeakIndexStart; is its intensity large enough?
                            If sicData(comparisonPeak.PeakLocation).Intensity >= sngAdjacentPeakIntensityThreshold Then
                                ' Yes, the intensity is large enough

                                ' Initialize intComparisonPeakEdgeIndex to the right edge of the adjacent peak
                                intComparisonPeakEdgeIndex = comparisonPeak.RightEdge

                                ' Find the first point in the adjacent peak that is at least 50% of the maximum in the adjacent peak
                                ' Store that point in intComparisonPeakEdgeIndex
                                sngTargetIntensity = sicData(comparisonPeak.PeakLocation).Intensity / 2
                                For intDataIndex = intComparisonPeakEdgeIndex To comparisonPeak.PeakLocation Step -1
                                    If sicData(intDataIndex).Intensity >= sngTargetIntensity Then
                                        intComparisonPeakEdgeIndex = intDataIndex
                                        Exit For
                                    End If
                                Next

                                ' Assure that intComparisonPeakEdgeIndex is less than intPeakIndexStart
                                If intComparisonPeakEdgeIndex >= intPeakIndexStart Then
                                    intComparisonPeakEdgeIndex = intPeakIndexStart - 1
                                    If intComparisonPeakEdgeIndex < 0 Then intComparisonPeakEdgeIndex = 0
                                End If

                                ' Possibly update intPreviousPeakFWHMPointRight
                                If intPeakIndexStart - intComparisonPeakEdgeIndex <= intSmallestIndexDifference Then
                                    intPreviousPeakFWHMPointRight = intComparisonPeakEdgeIndex
                                    intSmallestIndexDifference = intPeakIndexStart - intComparisonPeakEdgeIndex
                                End If
                            End If
                        End If
                    Next

                    ' Search through peakDataSaved to find the closest peak to the right of this peak
                    intSmallestIndexDifference = sicData.Count + 1
                    For intPeakIndexCompare = peakDataSaved.Peaks.Count - 1 To 0 Step -1
                        Dim comparisonPeak = peakDataSaved.Peaks(intPeakIndexCompare)

                        If intPeakIndexCompare <> peakDataSaved.BestPeakIndex AndAlso
                           comparisonPeak.PeakLocation >= intPeakIndexEnd Then

                            ' The peak is after intPeakIndexEnd; is its intensity large enough?
                            If sicData(comparisonPeak.PeakLocation).Intensity >= sngAdjacentPeakIntensityThreshold Then
                                ' Yes, the intensity is large enough

                                ' Initialize intComparisonPeakEdgeIndex to the left edge of the adjacent peak
                                intComparisonPeakEdgeIndex = comparisonPeak.LeftEdge

                                ' Find the first point in the adjacent peak that is at least 50% of the maximum in the adjacent peak
                                ' Store that point in intComparisonPeakEdgeIndex
                                sngTargetIntensity = sicData(comparisonPeak.PeakLocation).Intensity / 2
                                For intDataIndex = intComparisonPeakEdgeIndex To comparisonPeak.PeakLocation
                                    If sicData(intDataIndex).Intensity >= sngTargetIntensity Then
                                        intComparisonPeakEdgeIndex = intDataIndex
                                        Exit For
                                    End If
                                Next

                                ' Assure that intComparisonPeakEdgeIndex is greater than intPeakIndexEnd
                                If intPeakIndexEnd >= intComparisonPeakEdgeIndex Then
                                    intComparisonPeakEdgeIndex = intPeakIndexEnd + 1
                                    If intComparisonPeakEdgeIndex >= sicData.Count Then intComparisonPeakEdgeIndex = sicData.Count - 1
                                End If

                                ' Possibly update intNextPeakFWHMPointLeft
                                If intComparisonPeakEdgeIndex - intPeakIndexEnd <= intSmallestIndexDifference Then
                                    intNextPeakFWHMPointLeft = intComparisonPeakEdgeIndex
                                    intSmallestIndexDifference = intComparisonPeakEdgeIndex - intPeakIndexEnd
                                End If
                            End If
                        End If
                    Next


                Else
                    ' No peaks or no peaks containing .OriginalPeakLocationIndex
                    ' If peakData.PeakWidthPointsMinimum is greater than 3 and blnTestingMinimumPeakWidth = False, then decrement it by 50%
                    If peakData.PeakWidthPointsMinimum > MINIMUM_PEAK_WIDTH AndAlso Not blnTestingMinimumPeakWidth Then
                        peakData.PeakWidthPointsMinimum = CInt(Math.Floor(peakData.PeakWidthPointsMinimum / 2))
                        If peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH Then peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH
                    Else
                        intPeakLocationIndex = peakData.OriginalPeakLocationIndex
                        intPeakIndexStart = peakData.OriginalPeakLocationIndex
                        intPeakIndexEnd = peakData.OriginalPeakLocationIndex
                        intPreviousPeakFWHMPointRight = intPeakIndexStart
                        intNextPeakFWHMPointLeft = intPeakIndexEnd
                        blnValidPeakFound = True
                    End If
                End If
            Loop While Not blnValidPeakFound

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->FindPeaks", "Error in FindPeaks", ex, False)
            blnValidPeakFound = False
        End Try

        Return blnValidPeakFound

    End Function

    Private Function FindPeaksWork(
      objPeakDetector As clsPeakDetection,
      intScanNumbers As IList(Of Integer),
      peaksContainer As clsPeaksContainer,
      simDataPresent As Boolean,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      blnTestingMinimumPeakWidth As Boolean,
      returnClosestPeak As Boolean) As Boolean

        ' Returns True if a valid peak is found; otherwise, returns false
        ' When returnClosestPeak is True, then a valid peak is one that contains peaksContainer.OriginalPeakLocationIndex
        ' When returnClosestPeak is False, then stores the index of the most intense peak in peaksContainer.BestPeakIndex
        ' All of the identified peaks are returned in peaksContainer.Peaks(), regardless of whether they are valid or not

        Dim intFoundPeakIndex As Integer

        Dim sngPeakMaximum As Single

        Dim blnDataIsSmoothed As Boolean

        ' ReSharper disable once NotAccessedVariable
        Dim blnUsedSmoothedDataForPeakDetection As Boolean

        Dim blnValidPeakFound As Boolean
        Dim blnSuccess As Boolean

        Dim intPeakLocationIndex As Integer
        Dim intPeakIndexStart, intPeakIndexEnd As Integer

        Dim intStepOverIncreaseCount As Integer

        Dim strErrorMessage As String = String.Empty

        ' Smooth the Y data, and store in peaksContainer.SmoothedYData
        ' Note that if using a Butterworth filter, then we increase peaksContainer.PeakWidthPointsMinimum if too small, compared to 1/SamplingFrequency
        blnDataIsSmoothed = FindPeaksWorkSmoothData(
          peaksContainer, simDataPresent,
          sicPeakFinderOptions, peaksContainer.PeakWidthPointsMinimum,
          strErrorMessage)

        If sicPeakFinderOptions.FindPeaksOnSmoothedData AndAlso blnDataIsSmoothed Then
            peaksContainer.Peaks = objPeakDetector.DetectPeaks(
              peaksContainer.XData,
              peaksContainer.SmoothedYData,
              sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
              peaksContainer.PeakWidthPointsMinimum,
              CInt(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, True, True)

            blnUsedSmoothedDataForPeakDetection = True
        Else
            ' Look for the peaks, using peaksContainer.PeakWidthPointsMinimum as the minimum peak width
            peaksContainer.Peaks = objPeakDetector.DetectPeaks(
              peaksContainer.XData,
              peaksContainer.YData,
              sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
              peaksContainer.PeakWidthPointsMinimum,
              CInt(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, True, True)
            blnUsedSmoothedDataForPeakDetection = False
        End If


        If peaksContainer.Peaks.Count = -1 Then
            ' Fatal error occurred while finding peaks
            Return False
        End If

        If blnTestingMinimumPeakWidth Then
            If peaksContainer.Peaks.Count <= 0 Then
                ' No peaks were found; create a new peak list using the original peak location index as the peak center
                Dim newPeak = New clsPeakInfo(peaksContainer.OriginalPeakLocationIndex) With {
                    .LeftEdge = peaksContainer.OriginalPeakLocationIndex,
                    .RightEdge = peaksContainer.OriginalPeakLocationIndex
                }

                peaksContainer.Peaks.Add(newPeak)

            Else
                If returnClosestPeak Then
                    ' Make sure one of the peaks is within 1 of the original peak location
                    blnSuccess = False
                    For intFoundPeakIndex = 0 To peaksContainer.Peaks.Count - 1
                        If Math.Abs(peaksContainer.Peaks(intFoundPeakIndex).PeakLocation - peaksContainer.OriginalPeakLocationIndex) <= 1 Then
                            blnSuccess = True
                            Exit For
                        End If
                    Next

                    If Not blnSuccess Then
                        ' No match was found; add a new peak at peaksContainer.OriginalPeakLocationIndex

                        Dim newPeak = New clsPeakInfo(peaksContainer.OriginalPeakLocationIndex) With {
                            .LeftEdge = peaksContainer.OriginalPeakLocationIndex,
                            .RightEdge = peaksContainer.OriginalPeakLocationIndex,
                            .PeakArea = peaksContainer.YData(peaksContainer.OriginalPeakLocationIndex)
                        }

                        peaksContainer.Peaks.Add(newPeak)

                    End If
                End If
            End If
        End If

        If peaksContainer.Peaks.Count <= 0 Then
            ' No peaks were found
            blnValidPeakFound = False
        Else

            For Each peakItem In peaksContainer.Peaks

                peakItem.PeakIsValid = False

                ' Find the center and boundaries of this peak

                ' Copy from the PeakEdges arrays to the working variables
                intPeakLocationIndex = peakItem.PeakLocation
                intPeakIndexStart = peakItem.LeftEdge
                intPeakIndexEnd = peakItem.RightEdge

                ' Make sure intPeakLocationIndex is between intPeakIndexStart and intPeakIndexEnd
                If intPeakIndexStart > intPeakLocationIndex Then
                    LogErrors("clsMasicPeakFinder->FindPeaksWork",
                              "intPeakIndexStart is > intPeakLocationIndex; this is probably a programming error", Nothing, False)
                    intPeakIndexStart = intPeakLocationIndex
                End If

                If intPeakIndexEnd < intPeakLocationIndex Then
                    LogErrors("clsMasicPeakFinder->FindPeaksWork",
                              "intPeakIndexEnd is < intPeakLocationIndex; this is probably a programming error", Nothing, False)
                    intPeakIndexEnd = intPeakLocationIndex
                End If

                ' See if the peak boundaries (left and right edges) need to be narrowed or expanded
                ' Do this by stepping left or right while the intensity is decreasing.  If an increase is found, but the
                ' next point after the increasing point is less than the current point, then possibly keep stepping; the
                ' test for whether to keep stepping is that the next point away from the increasing point must be less
                ' than the current point.  If this is the case, replace the increasing point with the average of the
                ' current point and the point two points away
                '
                ' Use smoothed data for this step
                ' Determine the smoothing window based on peaksContainer.PeakWidthPointsMinimum
                ' If peaksContainer.PeakWidthPointsMinimum <= 4 then do not filter

                If Not blnDataIsSmoothed Then
                    ' Need to smooth the data now
                    blnDataIsSmoothed = FindPeaksWorkSmoothData(
                     peaksContainer, simDataPresent,
                     sicPeakFinderOptions, peaksContainer.PeakWidthPointsMinimum, strErrorMessage)
                End If

                ' First see if we need to narrow the peak by looking for decreasing intensities moving toward the peak center
                ' We'll use the unsmoothed data for this
                Do While intPeakIndexStart < intPeakLocationIndex - 1
                    If peaksContainer.YData(intPeakIndexStart) > peaksContainer.YData(intPeakIndexStart + 1) Then
                        ' OrElse (blnUsedSmoothedDataForPeakDetection AndAlso peaksContainer.SmoothedYData(intPeakIndexStart) < 0) Then
                        intPeakIndexStart += 1
                    Else
                        Exit Do
                    End If
                Loop

                Do While intPeakIndexEnd > intPeakLocationIndex + 1
                    If peaksContainer.YData(intPeakIndexEnd - 1) < peaksContainer.YData(intPeakIndexEnd) Then
                        ' OrElse (blnUsedSmoothedDataForPeakDetection AndAlso peaksContainer.SmoothedYData(intPeakIndexEnd) < 0) Then
                        intPeakIndexEnd -= 1
                    Else
                        Exit Do
                    End If
                Loop


                ' Now see if we need to expand the peak by looking for decreasing intensities moving away from the peak center,
                '  but allowing for small increases
                ' We'll use the smoothed data for this; if we encounter negative values in the smoothed data, we'll keep going until we reach the low point since huge peaks can cause some odd behavior with the Butterworth filter
                ' Keep track of the number of times we step over an increased value
                intStepOverIncreaseCount = 0
                Do While intPeakIndexStart > 0
                    'dblCurrentSlope = objPeakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, intPeakIndexStart, intPeakLocationIndex)

                    'If dblCurrentSlope > 0 AndAlso _
                    '   intPeakLocationIndex - intPeakIndexStart > 3 AndAlso _
                    '   peaksContainer.SmoothedYData(intPeakIndexStart - 1) < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * sngPeakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum) Then
                    '    ' We reached a low intensity data point and we're going downhill (i.e. the slope from this point to intPeakLocationIndex is positive)
                    '    ' Step once more and stop
                    '    intPeakIndexStart -= 1
                    '    Exit Do
                    If peaksContainer.SmoothedYData(intPeakIndexStart - 1) < peaksContainer.SmoothedYData(intPeakIndexStart) Then
                        ' The adjacent point is lower than the current point
                        intPeakIndexStart -= 1
                    ElseIf Math.Abs(peaksContainer.SmoothedYData(intPeakIndexStart - 1) -
                                    peaksContainer.SmoothedYData(intPeakIndexStart)) < Double.Epsilon Then
                        ' The adjacent point is equal to the current point
                        intPeakIndexStart -= 1
                    Else
                        ' The next point to the left is not lower; what about the point after it?
                        If intPeakIndexStart > 1 Then
                            If peaksContainer.SmoothedYData(intPeakIndexStart - 2) <= peaksContainer.SmoothedYData(intPeakIndexStart) Then
                                ' Only allow ignoring an upward spike if the delta from this point to the next is <= .MaxAllowedUpwardSpikeFractionMax of sngPeakMaximum
                                If peaksContainer.SmoothedYData(intPeakIndexStart - 1) - peaksContainer.SmoothedYData(intPeakIndexStart) >
                                   peaksContainer.MaxAllowedUpwardSpikeFractionMax * sngPeakMaximum Then
                                    Exit Do
                                End If

                                If blnDataIsSmoothed Then
                                    ' Only ignore an upward spike twice if the data is smoothed
                                    If intStepOverIncreaseCount >= 2 Then Exit Do
                                End If

                                intPeakIndexStart -= 1

                                intStepOverIncreaseCount += 1
                            Else
                                Exit Do
                            End If
                        Else
                            Exit Do
                        End If
                    End If
                Loop

                intStepOverIncreaseCount = 0
                Do While intPeakIndexEnd < peaksContainer.SourceDataCount - 1
                    'dblCurrentSlope = objPeakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, intPeakLocationIndex, intPeakIndexEnd)

                    'If dblCurrentSlope < 0 AndAlso _
                    '   intPeakIndexEnd - intPeakLocationIndex > 3 AndAlso _
                    '   peaksContainer.SmoothedYData(intPeakIndexEnd + 1) < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * sngPeakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum) Then
                    '    ' We reached a low intensity data point and we're going downhill (i.e. the slope from intPeakLocationIndex to this point is negative)
                    '    intPeakIndexEnd += 1
                    '    Exit Do
                    If peaksContainer.SmoothedYData(intPeakIndexEnd + 1) < peaksContainer.SmoothedYData(intPeakIndexEnd) Then
                        ' The adjacent point is lower than the current point
                        intPeakIndexEnd += 1
                    ElseIf Math.Abs(peaksContainer.SmoothedYData(intPeakIndexEnd + 1) -
                                    peaksContainer.SmoothedYData(intPeakIndexEnd)) < Double.Epsilon Then
                        ' The adjacent point is equal to the current point
                        intPeakIndexEnd += 1
                    Else
                        ' The next point to the right is not lower; what about the point after it?
                        If intPeakIndexEnd < peaksContainer.SourceDataCount - 2 Then
                            If peaksContainer.SmoothedYData(intPeakIndexEnd + 2) <= peaksContainer.SmoothedYData(intPeakIndexEnd) Then
                                ' Only allow ignoring an upward spike if the delta from this point to the next is <= .MaxAllowedUpwardSpikeFractionMax of sngPeakMaximum
                                If peaksContainer.SmoothedYData(intPeakIndexEnd + 1) - peaksContainer.SmoothedYData(intPeakIndexEnd) >
                                   peaksContainer.MaxAllowedUpwardSpikeFractionMax * sngPeakMaximum Then
                                    Exit Do
                                End If

                                If blnDataIsSmoothed Then
                                    ' Only ignore an upward spike twice if the data is smoothed
                                    If intStepOverIncreaseCount >= 2 Then Exit Do
                                End If

                                intPeakIndexEnd += 1

                                intStepOverIncreaseCount += 1
                            Else
                                Exit Do
                            End If
                        Else
                            Exit Do
                        End If
                    End If
                Loop

                peakItem.PeakIsValid = True
                If returnClosestPeak Then
                    ' If peaksContainer.OriginalPeakLocationIndex is not between intPeakIndexStart and intPeakIndexEnd, then check
                    '  if the scan number for peaksContainer.OriginalPeakLocationIndex is within .MaxDistanceScansNoOverlap scans of
                    '  either of the peak edges; if not, then mark the peak as invalid since it does not contain the
                    '  scan for the parent ion
                    If peaksContainer.OriginalPeakLocationIndex < intPeakIndexStart Then
                        If Math.Abs(intScanNumbers(peaksContainer.OriginalPeakLocationIndex) -
                                    intScanNumbers(intPeakIndexStart)) > sicPeakFinderOptions.MaxDistanceScansNoOverlap Then
                            peakItem.PeakIsValid = False
                        End If
                    ElseIf peaksContainer.OriginalPeakLocationIndex > intPeakIndexEnd Then
                        If Math.Abs(intScanNumbers(peaksContainer.OriginalPeakLocationIndex) -
                                    intScanNumbers(intPeakIndexEnd)) > sicPeakFinderOptions.MaxDistanceScansNoOverlap Then
                            peakItem.PeakIsValid = False
                        End If
                    End If
                End If

                ' Copy back from the working variables to the PeakEdges arrays
                peakItem.PeakLocation = intPeakLocationIndex
                peakItem.LeftEdge = intPeakIndexStart
                peakItem.RightEdge = intPeakIndexEnd

            Next

            ' Find the peak with the largest area that has peaksContainer.PeakIsValid = True
            peaksContainer.BestPeakIndex = -1
            peaksContainer.BestPeakArea = Single.MinValue
            For intFoundPeakIndex = 0 To peaksContainer.Peaks.Count - 1
                Dim currentPeak = peaksContainer.Peaks(intFoundPeakIndex)

                If currentPeak.PeakIsValid Then
                    If currentPeak.PeakArea > peaksContainer.BestPeakArea Then
                        peaksContainer.BestPeakIndex = intFoundPeakIndex
                        peaksContainer.BestPeakArea = CSng(Math.Min(currentPeak.PeakArea, Single.MaxValue))
                    End If
                End If
            Next

            If peaksContainer.BestPeakIndex >= 0 Then
                blnValidPeakFound = True
            Else
                blnValidPeakFound = False
            End If
        End If

        Return blnValidPeakFound

    End Function

    Private Function FindPeaksWorkSmoothData(
      peaksContainer As clsPeaksContainer,
      simDataPresent As Boolean,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      ByRef intPeakWidthPointsMinimum As Integer,
      ByRef strErrorMessage As String) As Boolean

        ' Returns True if the data was smoothed; false if not or an error
        ' The smoothed data is returned in udtPeakData.SmoothedYData

        Dim intFilterThirdWidth As Integer
        Dim blnSuccess As Boolean

        Dim sngButterWorthFrequency As Single

        Dim intPeakWidthPointsCompare As Integer

        Dim objFilter As New DataFilter.clsDataFilter

        ReDim peaksContainer.SmoothedYData(peaksContainer.SourceDataCount - 1)

        If (intPeakWidthPointsMinimum > 4 AndAlso (sicPeakFinderOptions.UseSavitzkyGolaySmooth OrElse sicPeakFinderOptions.UseButterworthSmooth)) OrElse
         sicPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth Then

            peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0)

            If sicPeakFinderOptions.UseButterworthSmooth Then
                ' Filter the data with a Butterworth filter (.UseButterworthSmooth takes precedence over .UseSavitzkyGolaySmooth)
                If simDataPresent AndAlso sicPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData Then
                    sngButterWorthFrequency = sicPeakFinderOptions.ButterworthSamplingFrequency * 2
                Else
                    sngButterWorthFrequency = sicPeakFinderOptions.ButterworthSamplingFrequency
                End If
                blnSuccess = objFilter.ButterworthFilter(
                  peaksContainer.SmoothedYData, 0,
                  peaksContainer.SourceDataCount - 1, sngButterWorthFrequency)
                If Not blnSuccess Then
                    LogErrors("clsMasicPeakFinder->FindPeaksWorkSmoothData",
                              "Error with the Butterworth filter" & strErrorMessage, Nothing, False)
                    Return False
                Else
                    ' Data was smoothed
                    ' Validate that intPeakWidthPointsMinimum is large enough
                    If sngButterWorthFrequency > 0 Then
                        intPeakWidthPointsCompare = CInt(Math.Round(1 / sngButterWorthFrequency, 0))
                        If intPeakWidthPointsMinimum < intPeakWidthPointsCompare Then
                            intPeakWidthPointsMinimum = intPeakWidthPointsCompare
                        End If
                    End If

                    Return True
                End If

            Else
                ' Filter the data with a Savitzky Golay filter
                intFilterThirdWidth = CInt(Math.Floor(peaksContainer.PeakWidthPointsMinimum / 3))
                If intFilterThirdWidth > 3 Then intFilterThirdWidth = 3

                ' Make sure intFilterThirdWidth is Odd
                If intFilterThirdWidth Mod 2 = 0 Then
                    intFilterThirdWidth -= 1
                End If

                ' Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                ' Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                blnSuccess = objFilter.SavitzkyGolayFilter(
                  peaksContainer.SmoothedYData, 0,
                  peaksContainer.SmoothedYData.Length - 1,
                  intFilterThirdWidth, intFilterThirdWidth,
                  sicPeakFinderOptions.SavitzkyGolayFilterOrder, True, strErrorMessage)

                If Not blnSuccess Then
                    LogErrors("clsMasicPeakFinder->FindPeaksWorkSmoothData",
                              "Error with the Savitzky-Golay filter: " & strErrorMessage, Nothing, False)
                    Return False
                Else
                    ' Data was smoothed
                    Return True
                End If
            End If
        Else
            ' Do not filter
            peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0)
            Return False
        End If

    End Function

    Public Sub FindPotentialPeakArea(
       intDataCount As Integer,
       SICIntensities() As Single,
       <Out> ByRef potentialAreaStats As clsSICPotentialAreaStats,
       sicPeakFinderOptions As clsSICPeakFinderOptions)

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To intDataCount - 1
            sicData.Add(New clsSICDataPoint(0, SICIntensities(index), 0))
        Next

        FindPotentialPeakArea(sicData, potentialAreaStats, sicPeakFinderOptions)
    End Sub

    Public Sub FindPotentialPeakArea(
      sicData As IList(Of clsSICDataPoint),
      <Out> ByRef potentialAreaStats As clsSICPotentialAreaStats,
      sicPeakFinderOptions As clsSICPeakFinderOptions)

        ' This function computes the potential peak area for a given SIC
        '  and stores in potentialAreaStats.MinimumPotentialPeakArea
        ' However, the summed intensity is not used if the number of points >= .SICBaselineNoiseOptions.MinimumBaselineNoiseLevel is less than Minimum_Peak_Width

        ' Note: You cannot use SICData.Length to determine the length of the array; use intDataCount

        Dim intIndex As Integer

        Dim minimumPositiveValue As Single
        Dim sngIntensityToUse As Single

        Dim dblOldestIntensity As Double
        Dim dblPotentialPeakArea, dblMinimumPotentialPeakArea As Double
        Dim intPeakCountBasisForMinimumPotentialArea As Integer
        Dim intValidPeakCount As Integer

        ' The queue is used to keep track of the most recent intensity values
        Dim queIntensityList As New Queue()

        dblMinimumPotentialPeakArea = Double.MaxValue
        intPeakCountBasisForMinimumPotentialArea = 0

        If sicData.Count > 0 Then

            queIntensityList.Clear()
            dblPotentialPeakArea = 0
            intValidPeakCount = 0

            ' Find the minimum intensity in SICData()
            minimumPositiveValue = FindMinimumPositiveValue(sicData, 1)

            For intIndex = 0 To sicData.Count - 1

                ' If this data point is > .MinimumBaselineNoiseLevel, then add this intensity to dblPotentialPeakArea
                '  and increment intValidPeakCount
                sngIntensityToUse = Math.Max(minimumPositiveValue, sicData(intIndex).Intensity)
                If sngIntensityToUse >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel Then
                    dblPotentialPeakArea += sngIntensityToUse
                    intValidPeakCount += 1
                End If

                If queIntensityList.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum Then
                    ' Decrement dblPotentialPeakArea by the oldest item in the queue
                    ' If that item is >= .MinimumBaselineNoiseLevel, then decrement intValidPeakCount too
                    dblOldestIntensity = CDbl(queIntensityList.Dequeue())

                    If dblOldestIntensity >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel AndAlso
                       dblOldestIntensity > 0 Then
                        dblPotentialPeakArea -= dblOldestIntensity
                        intValidPeakCount -= 1
                    End If
                End If
                ' Add this intensity to the queue
                queIntensityList.Enqueue(sngIntensityToUse)

                If dblPotentialPeakArea > 0 AndAlso intValidPeakCount >= MINIMUM_PEAK_WIDTH Then
                    If intValidPeakCount > intPeakCountBasisForMinimumPotentialArea Then
                        ' The non valid peak count value is larger than the one associated with the current
                        '  minimum potential peak area; update the minimum peak area to dblPotentialPeakArea
                        dblMinimumPotentialPeakArea = dblPotentialPeakArea
                        intPeakCountBasisForMinimumPotentialArea = intValidPeakCount
                    Else
                        If dblPotentialPeakArea < dblMinimumPotentialPeakArea AndAlso
                           intValidPeakCount = intPeakCountBasisForMinimumPotentialArea Then
                            dblMinimumPotentialPeakArea = dblPotentialPeakArea
                        End If
                    End If
                End If
            Next

        End If

        If dblMinimumPotentialPeakArea >= Double.MaxValue Then
            dblMinimumPotentialPeakArea = 1
        End If

        potentialAreaStats = New clsSICPotentialAreaStats() With {
            .MinimumPotentialPeakArea = dblMinimumPotentialPeakArea,
            .PeakCountBasisForMinimumPotentialArea = intPeakCountBasisForMinimumPotentialArea
        }

    End Sub

    ''' <summary>
    ''' Find SIC Peak and Area
    ''' </summary>
    ''' <param name="intDataCount"></param>
    ''' <param name="SICScanNumbers"></param>
    ''' <param name="SICIntensities"></param>
    ''' <param name="potentialAreaStatsForPeak"></param>
    ''' <param name="sicPeak"></param>
    ''' <param name="smoothedYDataSubset"></param>
    ''' <param name="sicPeakFinderOptions"></param>
    ''' <param name="potentialAreaStatsForRegion"></param>
    ''' <param name="returnClosestPeak"></param>
    ''' <param name="simDataPresent">True if Select Ion Monitoring data is present</param>
    ''' <param name="recomputeNoiseLevel"></param>
    <Obsolete("Use the version that takes a List(Of clsSICDataPoint")>
    Public Function FindSICPeakAndArea(
        intDataCount As Integer,
        SICScanNumbers() As Integer,
        SICIntensities() As Single,
        <Out> ByRef potentialAreaStatsForPeak As clsSICPotentialAreaStats,
        sicPeak As clsSICStatsPeak,
        <Out> ByRef smoothedYDataSubset As clsSmoothedYDataSubset,
        sicPeakFinderOptions As clsSICPeakFinderOptions,
        potentialAreaStatsForRegion As clsSICPotentialAreaStats,
        returnClosestPeak As Boolean,
        simDataPresent As Boolean,
        recomputeNoiseLevel As Boolean) As Boolean

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To intDataCount - 1
            sicData.Add(New clsSICDataPoint(SICScanNumbers(index), SICIntensities(index), 0))
        Next

        Return FindSICPeakAndArea(sicData,
                                  potentialAreaStatsForPeak, sicPeak,
                                  smoothedYDataSubset, sicPeakFinderOptions,
                                  potentialAreaStatsForRegion,
                                  returnClosestPeak, simDataPresent, recomputeNoiseLevel)
    End Function

    ''' <summary>
    ''' Find SIC Peak and Area
    ''' </summary>
    ''' <param name="sicData">Selected Ion Chromatogram data (scan, intensity, mass)</param>
    ''' <param name="potentialAreaStatsForPeak">Output: potential area stats for the identified peak</param>
    ''' <param name="sicPeak">Output: identified Peak</param>
    ''' <param name="smoothedYDataSubset"></param>
    ''' <param name="sicPeakFinderOptions"></param>
    ''' <param name="potentialAreaStatsForRegion"></param>
    ''' <param name="returnClosestPeak"></param>
    ''' <param name="simDataPresent">True if Select Ion Monitoring data is present</param>
    ''' <param name="recomputeNoiseLevel"></param>
    ''' <returns></returns>
    Public Function FindSICPeakAndArea(
      sicData As List(Of clsSICDataPoint),
      <Out> ByRef potentialAreaStatsForPeak As clsSICPotentialAreaStats,
      sicPeak As clsSICStatsPeak,
      <Out> ByRef smoothedYDataSubset As clsSmoothedYDataSubset,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      potentialAreaStatsForRegion As clsSICPotentialAreaStats,
      returnClosestPeak As Boolean,
      simDataPresent As Boolean,
      recomputeNoiseLevel As Boolean) As Boolean

        ' Note: The calling function should populate udtSICPeak.IndexObserved with the index in SICData() that the
        '       parent ion m/z was actually observed; this will be used as the default peak location if a peak cannot be found

        ' Set simDataPresent to True when there are large gaps in the survey scan numbers

        Dim intDataIndex As Integer
        Dim sngIntensityCompare As Single

        Dim blnSuccess As Boolean

        potentialAreaStatsForPeak = New clsSICPotentialAreaStats()
        smoothedYDataSubset = New clsSmoothedYDataSubset()

        Try
            ' Compute the potential peak area for this SIC
            FindPotentialPeakArea(sicData, potentialAreaStatsForPeak, sicPeakFinderOptions)

            ' See if the potential peak area for this SIC is lower than the values for the Region
            ' If so, then update the region values with this peak's values
            With potentialAreaStatsForPeak
                If .MinimumPotentialPeakArea > 1 AndAlso .PeakCountBasisForMinimumPotentialArea >= MINIMUM_PEAK_WIDTH Then
                    If .PeakCountBasisForMinimumPotentialArea > potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
                        potentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
                        potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
                    Else
                        If .MinimumPotentialPeakArea < potentialAreaStatsForRegion.MinimumPotentialPeakArea AndAlso
                           .PeakCountBasisForMinimumPotentialArea >= potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
                            potentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
                            potentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
                        End If
                    End If
                End If
            End With

            If sicData Is Nothing OrElse sicData.Count = 0 Then
                ' Either .SICData is nothing or no SIC data exists
                ' Cannot find peaks for this parent ion

                sicPeak.IndexObserved = 0
                sicPeak.IndexBaseLeft = sicPeak.IndexObserved
                sicPeak.IndexBaseRight = sicPeak.IndexObserved
                sicPeak.IndexMax = sicPeak.IndexObserved
                sicPeak.ParentIonIntensity = 0
                sicPeak.PreviousPeakFWHMPointRight = 0
                sicPeak.NextPeakFWHMPointLeft = 0

                smoothedYDataSubset = New clsSmoothedYDataSubset()
            Else


                ' Initialize the following to the entire range of the SICData
                sicPeak.IndexBaseLeft = 0
                sicPeak.IndexBaseRight = sicData.Count - 1

                ' Initialize .IndexMax to .IndexObserved (which should have been defined by the calling function)
                sicPeak.IndexMax = sicPeak.IndexObserved
                If sicPeak.IndexMax < 0 OrElse sicPeak.IndexMax >= sicData.Count Then
                    LogErrors("clsMasicPeakFinder->FindSICPeakAndArea", "Unexpected .IndexMax value", Nothing, False)
                    sicPeak.IndexMax = 0
                End If

                sicPeak.PreviousPeakFWHMPointRight = sicPeak.IndexBaseLeft
                sicPeak.NextPeakFWHMPointLeft = sicPeak.IndexBaseRight

                If recomputeNoiseLevel Then
                    Dim intensities = (From item In sicData Select item.Intensity).ToArray()

                    ' Compute the Noise Threshold for this SIC
                    ' This value is first computed using all data in the SIC; it is later updated
                    '  to be the minimum value of the average of the data to the immediate left and
                    '  immediate right of the peak identified in the SIC
                    blnSuccess = ComputeNoiseLevelForSICData(
                        sicData.Count, intensities,
                      sicPeakFinderOptions.SICBaselineNoiseOptions,
                      sicPeak.BaselineNoiseStats)
                End If


                ' Use a peak-finder algorithm to find the peak closest to .Peak.IndexMax
                ' Note that .Peak.IndexBaseLeft, .Peak.IndexBaseRight, and .Peak.IndexMax are passed ByRef and get updated by FindPeaks
                With sicPeak
                    blnSuccess = FindPeaks(sicData, .IndexBaseLeft, .IndexBaseRight, .IndexMax,
                      .PreviousPeakFWHMPointRight, .NextPeakFWHMPointLeft, .ShoulderCount,
                      smoothedYDataSubset, simDataPresent, sicPeakFinderOptions,
                                           sicPeak.BaselineNoiseStats.NoiseLevel,
                      potentialAreaStatsForRegion.MinimumPotentialPeakArea,
                      returnClosestPeak)
                End With

                If blnSuccess Then
                    ' Update the maximum peak intensity (required prior to call to ComputeNoiseLevelInPeakVicinity and call to ComputeSICPeakArea)
                    sicPeak.MaxIntensityValue = sicData(sicPeak.IndexMax).Intensity

                    If recomputeNoiseLevel Then
                        ' Update the value for potentialAreaStatsForPeak.SICNoiseThresholdIntensity based on the data around the peak
                        blnSuccess = ComputeNoiseLevelInPeakVicinity(
                          sicData, sicPeak,
                          sicPeakFinderOptions.SICBaselineNoiseOptions)
                    End If

                    '' ' Compute the trimmed median of the data in SICData (replacing non positive values with the minimum)
                    '' ' If the median is less than udtSICPeak.BaselineNoiseStats.NoiseLevel then update udtSICPeak.BaselineNoiseStats.NoiseLevel
                    ''udtNoiseOptionsOverride = sicPeakFinderOptions.SICBaselineNoiseOptions
                    ''With udtNoiseOptionsOverride
                    ''    .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
                    ''    .TrimmedMeanFractionLowIntensityDataToAverage = 0.75
                    ''End With
                    ''blnSuccess = ComputeNoiseLevelForSICData(sicData, udtNoiseOptionsOverride, udtNoiseStatsCompare)
                    ''With udtNoiseStatsCompare
                    ''    If .PointsUsed >= MINIMUM_NOISE_SCANS_REQUIRED Then
                    ''        ' Check whether the comparison noise level is less than the existing noise level times 0.75
                    ''        If .NoiseLevel < udtSICPeak.BaselineNoiseStats.NoiseLevel * 0.75 Then
                    ''            ' Yes, the comparison noise level is lower
                    ''            ' Use a T-Test to see if the comparison noise level is significantly different than the primary noise level
                    ''            If TestSignificanceUsingTTest(.NoiseLevel, udtSICPeak.BaselineNoiseStats.NoiseLevel, .NoiseStDev, udtSICPeak.BaselineNoiseStats.NoiseStDev, .PointsUsed, udtSICPeak.BaselineNoiseStats.PointsUsed, eTTestConfidenceLevelConstants.Conf95Pct, dblTCalculated) Then
                    ''                udtSICPeak.BaselineNoiseStats = udtNoiseStatsCompare
                    ''            End If
                    ''        End If
                    ''    End If
                    ''End With

                    ' If smoothing was enabled, then see if the smoothed value is larger than udtSICPeak.MaxIntensityValue
                    ' If it is, then use the smoothed value for udtSICPeak.MaxIntensityValue
                    If sicPeakFinderOptions.UseSavitzkyGolaySmooth OrElse sicPeakFinderOptions.UseButterworthSmooth Then
                        intDataIndex = sicPeak.IndexMax - smoothedYDataSubset.DataStartIndex
                        If intDataIndex >= 0 AndAlso Not smoothedYDataSubset.Data Is Nothing AndAlso
                           intDataIndex < smoothedYDataSubset.DataCount Then
                            ' Possibly use the intensity of the smoothed data as the peak intensity
                            sngIntensityCompare = smoothedYDataSubset.Data(intDataIndex)
                            If sngIntensityCompare > sicPeak.MaxIntensityValue Then
                                sicPeak.MaxIntensityValue = sngIntensityCompare
                            End If
                        End If
                    End If

                    ' Compute the signal to noise ratio for the peak
                    sicPeak.SignalToNoiseRatio = ComputeSignalToNoise(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel)

                    ' Compute the Full Width at Half Max (FWHM) value, this time subtracting the noise level from the baseline
                    sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, True)

                    ' Compute the Area (this function uses .FWHMScanWidth and therefore needs to be called after ComputeFWHM)
                    blnSuccess = ComputeSICPeakArea(sicData, sicPeak)

                    ' Compute the Statistical Moments values
                    ComputeStatisticalMomentsStats(sicData, smoothedYDataSubset, sicPeak)

                Else
                    ' No peak found

                    sicPeak.MaxIntensityValue = sicData(sicPeak.IndexMax).Intensity
                    sicPeak.IndexBaseLeft = sicPeak.IndexMax
                    sicPeak.IndexBaseRight = sicPeak.IndexMax
                    sicPeak.FWHMScanWidth = 1

                    ' Assign the intensity of the peak at the observed maximum to the area
                    sicPeak.Area = sicPeak.MaxIntensityValue

                    sicPeak.SignalToNoiseRatio = ComputeSignalToNoise(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel)

                End If

            End If

            blnSuccess = True

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->FindSICPeakAndArea", "Error finding SIC peaks and their areas", ex, False)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Public Shared Function GetDefaultNoiseThresholdOptions() As clsBaselineNoiseOptions
        Dim baselineNoiseOptions = New clsBaselineNoiseOptions()

        With baselineNoiseOptions
            .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
            .BaselineNoiseLevelAbsolute = 0
            .MinimumSignalToNoiseRatio = 0                      ' ToDo: Figure out how best to use this when > 0; for now, the SICNoiseMinimumSignalToNoiseRatio property ignores any attempts to set this value
            .MinimumBaselineNoiseLevel = 1
            .TrimmedMeanFractionLowIntensityDataToAverage = 0.75
            .DualTrimmedMeanStdDevLimits = 5
            .DualTrimmedMeanMaximumSegments = 3
        End With

        Return baselineNoiseOptions
    End Function

    Public Shared Function GetDefaultSICPeakFinderOptions() As clsSICPeakFinderOptions
        Dim sicPeakFinderOptions = New clsSICPeakFinderOptions()

        With sicPeakFinderOptions
            .IntensityThresholdFractionMax = 0.01           ' 1% of the peak maximum
            .IntensityThresholdAbsoluteMinimum = 0

            ' Set the default SIC Baseline noise threshold options
            .SICBaselineNoiseOptions = GetDefaultNoiseThresholdOptions()
            ' Customize a few values
            With .SICBaselineNoiseOptions
                .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
            End With

            .MaxDistanceScansNoOverlap = 0
            .MaxAllowedUpwardSpikeFractionMax = 0.2         ' 20%
            .InitialPeakWidthScansScaler = 1
            .InitialPeakWidthScansMaximum = 30

            .FindPeaksOnSmoothedData = True
            .SmoothDataRegardlessOfMinimumPeakWidth = True
            .UseButterworthSmooth = True                                ' If this is true, will ignore UseSavitzkyGolaySmooth
            .ButterworthSamplingFrequency = 0.25
            .ButterworthSamplingFrequencyDoubledForSIMData = True

            .UseSavitzkyGolaySmooth = True
            .SavitzkyGolayFilterOrder = 0                               ' Moving average filter if 0, Savitzky Golay filter if 2, 4, 6, etc.

            ' Set the default Mass Spectra noise threshold options
            .MassSpectraNoiseThresholdOptions = GetDefaultNoiseThresholdOptions()
            ' Customize a few values
            With .MassSpectraNoiseThresholdOptions
                .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
                .TrimmedMeanFractionLowIntensityDataToAverage = 0.5
                .MinimumSignalToNoiseRatio = 2
            End With
        End With

        Return sicPeakFinderOptions

    End Function

    Private Function GetVersionForExecutingAssembly() As String
        Dim strVersion As String

        Try
            strVersion = Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString()
        Catch ex As Exception
            strVersion = "??.??.??.??"
        End Try

        Return strVersion

    End Function

    Public Shared Function InitializeBaselineNoiseStats(
      sngMinimumBaselineNoiseLevel As Single,
      eNoiseThresholdMode As eNoiseThresholdModes) As clsBaselineNoiseStats

        Dim baselineNoiseStats = New clsBaselineNoiseStats() With {
            .NoiseLevel = sngMinimumBaselineNoiseLevel,
            .NoiseStDev = 0,
            .PointsUsed = 0,
            .NoiseThresholdModeUsed = eNoiseThresholdMode
        }

        Return baselineNoiseStats

    End Function

    <Obsolete("Use the version that returns udtBaselineNoiseStatsType")>
    Public Shared Sub InitializeBaselineNoiseStats(
      ByRef baselineNoiseStats As clsBaselineNoiseStats,
      sngMinimumBaselineNoiseLevel As Single,
      eNoiseThresholdMode As eNoiseThresholdModes)

        baselineNoiseStats = InitializeBaselineNoiseStats(sngMinimumBaselineNoiseLevel, eNoiseThresholdMode)

    End Sub

    ''' <summary>
    ''' Determines the X value that corresponds to sngTargetY by interpolating the line between (X1, Y1) and (X2, Y2)
    ''' </summary>
    ''' <param name="sngInterpolatedXValue"></param>
    ''' <param name="x1"></param>
    ''' <param name="x2"></param>
    ''' <param name="y1"></param>
    ''' <param name="y2"></param>
    ''' <param name="sngTargetY"></param>
    ''' <returns>Returns True on success, false on error</returns>
    Private Function InterpolateX(
      <Out> ByRef sngInterpolatedXValue As Single,
      x1 As Integer, x2 As Integer,
      y1 As Single, y2 As Single,
      sngTargetY As Single) As Boolean

        Dim sngDeltaY As Single
        Dim sngFraction As Single
        Dim intDeltaX As Integer
        Dim sngTargetX As Single

        sngDeltaY = y2 - y1                                 ' This is y-two minus y-one
        sngFraction = (sngTargetY - y1) / sngDeltaY
        intDeltaX = x2 - x1                                 ' This is x-two minus x-one

        sngTargetX = sngFraction * intDeltaX + x1

        If Math.Abs(sngTargetX - x1) >= 0 AndAlso Math.Abs(sngTargetX - x2) >= 0 Then
            sngInterpolatedXValue = sngTargetX
            Return True
        Else
            LogErrors("clsMasicPeakFinder->InterpolateX", "TargetX is not between X1 and X2; this shouldn't happen", Nothing, False)
            sngInterpolatedXValue = 0
            Return False
        End If

    End Function

    ''' <summary>
    ''' Determines the Y value that corresponds to sngXValToInterpolate by interpolating the line between (X1, Y1) and (X2, Y2)
    ''' </summary>
    ''' <param name="sngInterpolatedIntensity"></param>
    ''' <param name="X1"></param>
    ''' <param name="X2"></param>
    ''' <param name="Y1"></param>
    ''' <param name="Y2"></param>
    ''' <param name="sngXValToInterpolate"></param>
    ''' <returns></returns>
    Private Function InterpolateY(
      <Out> ByRef sngInterpolatedIntensity As Single,
      X1 As Integer, X2 As Integer,
      Y1 As Single, Y2 As Single,
      sngXValToInterpolate As Single) As Boolean

        Dim intScanDifference As Integer

        intScanDifference = X2 - X1
        If intScanDifference <> 0 Then
            sngInterpolatedIntensity = Y1 + (Y2 - Y1) * ((sngXValToInterpolate - X1) / intScanDifference)
            Return True
        Else
            ' sngXValToInterpolate is not between X1 and X2; cannot interpolate
            sngInterpolatedIntensity = 0
            Return False
        End If
    End Function

    Private Sub LogErrors(
      source As String,
      message As String,
      ex As Exception,
      Optional allowThrowingException As Boolean = True)

        Dim messageWithoutCRLF As String

        mStatusMessage = String.Copy(message)

        messageWithoutCRLF = mStatusMessage.Replace(ControlChars.NewLine, "; ")

        OnErrorEvent(source & ": " & messageWithoutCRLF, ex)

        If allowThrowingException Then
            Throw New Exception(mStatusMessage, ex)
        End If
    End Sub

    Public Function LookupNoiseStatsUsingSegments(
      intScanIndexObserved As Integer,
      noiseStatsSegments As List(Of clsBaselineNoiseStatsSegment)) As clsBaselineNoiseStats

        Dim intNoiseSegmentIndex As Integer
        Dim intIndexSegmentA As Integer
        Dim intIndexSegmentB As Integer

        Dim baselineNoiseStats As clsBaselineNoiseStats = Nothing
        Dim intSegmentMidPointA As Integer
        Dim intSegmentMidPointB As Integer
        Dim blnMatchFound As Boolean

        Dim dblFractionFromSegmentB As Double
        Dim dblFractionFromSegmentA As Double

        Try
            If noiseStatsSegments Is Nothing OrElse noiseStatsSegments.Count < 1 Then
                baselineNoiseStats = InitializeBaselineNoiseStats(
                  GetDefaultNoiseThresholdOptions().MinimumBaselineNoiseLevel,
                  eNoiseThresholdModes.DualTrimmedMeanByAbundance)

                Return baselineNoiseStats
            End If

            If noiseStatsSegments.Count <= 1 Then
                Return noiseStatsSegments.First().BaselineNoiseStats
            End If

            ' First, initialize to the first segment
            baselineNoiseStats = noiseStatsSegments.First().BaselineNoiseStats.Clone()

            ' Initialize intIndexSegmentA and intIndexSegmentB to 0, indicating no extrapolation needed
            intIndexSegmentA = 0
            intIndexSegmentB = 0
            blnMatchFound = False                ' Next, see if intScanIndexObserved matches any of the segments (provided more than one segment exists)
            For intNoiseSegmentIndex = 0 To noiseStatsSegments.Count - 1
                Dim current = noiseStatsSegments(intNoiseSegmentIndex)

                If intScanIndexObserved >= current.SegmentIndexStart AndAlso intScanIndexObserved <= current.SegmentIndexEnd Then
                    intSegmentMidPointA = current.SegmentIndexStart + CInt((current.SegmentIndexEnd - current.SegmentIndexStart) / 2)
                    blnMatchFound = True
                End If

                If blnMatchFound Then
                    baselineNoiseStats = current.BaselineNoiseStats.Clone()

                    If intScanIndexObserved < intSegmentMidPointA Then
                        If intNoiseSegmentIndex > 0 Then
                            ' Need to Interpolate using this segment and the next one
                            intIndexSegmentA = intNoiseSegmentIndex - 1
                            intIndexSegmentB = intNoiseSegmentIndex

                            ' Copy intSegmentMidPointA to intSegmentMidPointB since the current segment is actually segment B
                            ' Define intSegmentMidPointA
                            intSegmentMidPointB = intSegmentMidPointA
                            Dim previous = noiseStatsSegments(intNoiseSegmentIndex - 1)
                            intSegmentMidPointA = previous.SegmentIndexStart + CInt((previous.SegmentIndexEnd - previous.SegmentIndexStart) / 2)

                        Else
                            ' intScanIndexObserved occurs before the midpoint, but we're in the first segment; no need to Interpolate
                        End If
                    ElseIf intScanIndexObserved > intSegmentMidPointA Then
                        If intNoiseSegmentIndex < noiseStatsSegments.Count - 1 Then
                            ' Need to Interpolate using this segment and the one before it
                            intIndexSegmentA = intNoiseSegmentIndex
                            intIndexSegmentB = intNoiseSegmentIndex + 1

                            ' Note: intSegmentMidPointA is already defined since the current segment is segment A
                            ' Define intSegmentMidPointB
                            Dim nextSegment = noiseStatsSegments(intNoiseSegmentIndex + 1)

                            intSegmentMidPointB = nextSegment.SegmentIndexStart + CInt((nextSegment.SegmentIndexEnd - nextSegment.SegmentIndexStart) / 2)

                        Else
                            ' intScanIndexObserved occurs after the midpoint, but we're in the last segment; no need to Interpolate
                        End If
                    Else
                        ' intScanIndexObserved occurs at the midpoint; no need to Interpolate
                    End If

                    If intIndexSegmentA <> intIndexSegmentB Then
                        ' Interpolate between the two segments
                        dblFractionFromSegmentB = CDbl(intScanIndexObserved - intSegmentMidPointA) /
                                                  CDbl(intSegmentMidPointB - intSegmentMidPointA)
                        If dblFractionFromSegmentB < 0 Then
                            dblFractionFromSegmentB = 0
                        ElseIf dblFractionFromSegmentB > 1 Then
                            dblFractionFromSegmentB = 1
                        End If

                        dblFractionFromSegmentA = 1 - dblFractionFromSegmentB

                        ' Compute the weighted average values
                        Dim segmentA = noiseStatsSegments(intIndexSegmentA).BaselineNoiseStats
                        Dim segmentB = noiseStatsSegments(intIndexSegmentB).BaselineNoiseStats

                        baselineNoiseStats.NoiseLevel = CSng(segmentA.NoiseLevel * dblFractionFromSegmentA + segmentB.NoiseLevel * dblFractionFromSegmentB)
                        baselineNoiseStats.NoiseStDev = CSng(segmentA.NoiseStDev * dblFractionFromSegmentA + segmentB.NoiseStDev * dblFractionFromSegmentB)
                        baselineNoiseStats.PointsUsed = CInt(segmentA.PointsUsed * dblFractionFromSegmentA + segmentB.PointsUsed * dblFractionFromSegmentB)

                    End If
                    Exit For
                End If
            Next

        Catch ex As Exception
            ' Ignore Errors
        End Try

        Return baselineNoiseStats

    End Function

    ''' <summary>
    ''' Looks for the minimum positive value in sngDataSorted() and replaces all values of 0 in sngDataSorted() with minimumPositiveValue
    ''' </summary>
    ''' <param name="intDataCount"></param>
    ''' <param name="sngDataSorted"></param>
    ''' <returns>Minimum positive value</returns>
    ''' <remarks>Assumes data in sngDataSorted() is sorted ascending</remarks>
    Private Function ReplaceSortedDataWithMinimumPositiveValue(intDataCount As Integer, sngDataSorted As IList(Of Single)) As Single

        Dim minimumPositiveValue As Single
        Dim intIndex As Integer
        Dim intIndexFirstPositiveValue As Integer

        ' Find the minimum positive value in sngDataSorted
        ' Since it's sorted, we can stop at the first non-zero value

        intIndexFirstPositiveValue = -1
        minimumPositiveValue = 0
        For intIndex = 0 To intDataCount - 1
            If sngDataSorted(intIndex) > 0 Then
                intIndexFirstPositiveValue = intIndex
                minimumPositiveValue = sngDataSorted(intIndex)
                Exit For
            End If
        Next

        If minimumPositiveValue < 1 Then minimumPositiveValue = 1
        For intIndex = intIndexFirstPositiveValue To 0 Step -1
            sngDataSorted(intIndex) = minimumPositiveValue
        Next

        Return minimumPositiveValue

    End Function

    ''' <summary>
    ''' Uses the means and sigma values to compute the t-test value between the two populations to determine if they are statistically different
    ''' </summary>
    ''' <param name="dblMean1"></param>
    ''' <param name="dblMean2"></param>
    ''' <param name="dblStDev1"></param>
    ''' <param name="dblStDev2"></param>
    ''' <param name="intCount1"></param>
    ''' <param name="intCount2"></param>
    ''' <param name="eConfidenceLevel"></param>
    ''' <param name="TCalculated"></param>
    ''' <returns>True if the two populations are statistically different, based on the given significance threshold</returns>
    Private Function TestSignificanceUsingTTest(
      dblMean1 As Double, dblMean2 As Double,
      dblStDev1 As Double, dblStDev2 As Double,
      intCount1 As Integer, intCount2 As Integer,
      eConfidenceLevel As eTTestConfidenceLevelConstants,
      <Out> ByRef TCalculated As Double) As Boolean

        ' To use the t-test you must use sample variance values, not population variance values
        ' Note: Variance_Sample = Sum((x-mean)^2) / (count-1)
        ' Note: Sigma = SquareRoot(Variance_Sample)

        Dim SPooled As Double
        Dim intConfidenceLevelIndex As Integer

        If intCount1 + intCount2 <= 2 Then
            ' Cannot compute the T-Test
            TCalculated = 0
            Return False
        Else

            SPooled = Math.Sqrt(((dblStDev1 ^ 2) * (intCount1 - 1) + (dblStDev2 ^ 2) * (intCount2 - 1)) / (intCount1 + intCount2 - 2))
            TCalculated = ((dblMean1 - dblMean2) / SPooled) * Math.Sqrt(intCount1 * intCount2 / (intCount1 + intCount2))

            intConfidenceLevelIndex = eConfidenceLevel
            If intConfidenceLevelIndex < 0 Then
                intConfidenceLevelIndex = 0
            ElseIf intConfidenceLevelIndex >= TTestConfidenceLevels.Length Then
                intConfidenceLevelIndex = TTestConfidenceLevels.Length - 1
            End If

            If TCalculated >= TTestConfidenceLevels(intConfidenceLevelIndex) Then
                ' Differences are significant
                Return True
            Else
                ' Differences are not significant
                Return False
            End If
        End If

    End Function

End Class
