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
' Licensed under the 2-Clause BSD License; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause

Public Class clsMASICPeakFinder
    Inherits PRISM.EventNotifier

#Region "Constants and Enums"
    Public PROGRAM_DATE As String = "May 23, 2019"

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
        ' ReSharper disable UnusedMember.Global
        Conf80Pct = 0
        Conf90Pct = 1
        Conf95Pct = 2
        Conf98Pct = 3
        Conf99Pct = 4
        Conf99_5Pct = 5
        Conf99_8Pct = 6
        Conf99_9Pct = 7
        ' ReSharper restore UnusedMember.Global
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
    Private ReadOnly TTestConfidenceLevels As Double() = New Double() {1.886, 2.92, 4.303, 6.965, 9.925, 14.089, 22.327, 31.598}

#End Region

#Region "Properties"
    ' ReSharper disable once UnusedMember.Global
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

    ' ReSharper disable once UnusedMember.Global
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

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Compute an updated peak area by adjusting for the baseline
    ''' </summary>
    ''' <param name="sicPeak"></param>
    ''' <param name="sicPeakWidthFullScans"></param>
    ''' <param name="allowNegativeValues"></param>
    ''' <returns>Adjusted peak area</returns>
    ''' <remarks>This method is used by MASIC Browser</remarks>
    Public Shared Function BaselineAdjustArea(
      sicPeak As clsSICStatsPeak, sicPeakWidthFullScans As Integer, allowNegativeValues As Boolean) As Double
        ' Note, compute sicPeakWidthFullScans using:
        '  Width = sicScanNumbers(.Peak.IndexBaseRight) - sicScanNumbers(.Peak.IndexBaseLeft) + 1

        Return BaselineAdjustArea(sicPeak.Area, sicPeak.BaselineNoiseStats.NoiseLevel, sicPeak.FWHMScanWidth, sicPeakWidthFullScans, allowNegativeValues)

    End Function

    Public Shared Function BaselineAdjustArea(
      peakArea As Double,
      baselineNoiseLevel As Double,
      sicPeakFWHMScans As Integer,
      sicPeakWidthFullScans As Integer,
      allowNegativeValues As Boolean) As Double

        Dim widthToSubtract = ComputeWidthAtBaseUsingFWHM(sicPeakFWHMScans, sicPeakWidthFullScans, 4)

        Dim correctedArea = peakArea - baselineNoiseLevel * widthToSubtract
        If allowNegativeValues OrElse correctedArea > 0 Then
            Return correctedArea
        Else
            Return 0
        End If
    End Function

    ' ReSharper disable once UnusedMember.Global
    Public Shared Function BaselineAdjustIntensity(sicPeak As clsSICStatsPeak, allowNegativeValues As Boolean) As Double
        Return BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, allowNegativeValues)
    End Function

    Public Shared Function BaselineAdjustIntensity(
      rawIntensity As Double,
      baselineNoiseLevel As Double,
      allowNegativeValues As Boolean) As Double

        If allowNegativeValues OrElse rawIntensity > baselineNoiseLevel Then
            Return rawIntensity - baselineNoiseLevel
        Else
            Return 0
        End If
    End Function

    Private Function ComputeAverageNoiseLevelCheckCounts(
      validDataCountA As Integer, validDataCountB As Integer,
      sumA As Double, sumB As Double,
      minimumCount As Integer,
      baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        Dim useLeftData As Boolean
        Dim useRightData As Boolean

        If minimumCount < 1 Then minimumCount = 1
        Dim useBothSides = False

        If validDataCountA >= minimumCount OrElse validDataCountB >= minimumCount Then

            If validDataCountA >= minimumCount AndAlso validDataCountB >= minimumCount Then
                ' Both meet the minimum count criterion
                ' Return an overall average
                useBothSides = True
            ElseIf validDataCountA >= minimumCount Then
                useLeftData = True
            Else
                useRightData = True
            End If

            If useBothSides Then
                baselineNoiseStats.NoiseLevel = (sumA + sumB) / (validDataCountA + validDataCountB)
                baselineNoiseStats.NoiseStDev = 0      ' We'll compute noise StDev outside this function
                baselineNoiseStats.PointsUsed = validDataCountA + validDataCountB
            Else
                If useLeftData Then
                    ' Use left data only
                    baselineNoiseStats.NoiseLevel = sumA / validDataCountA
                    baselineNoiseStats.NoiseStDev = 0
                    baselineNoiseStats.PointsUsed = validDataCountA
                ElseIf useRightData Then
                    ' Use right data only
                    baselineNoiseStats.NoiseLevel = sumB / validDataCountB
                    baselineNoiseStats.NoiseStDev = 0
                    baselineNoiseStats.PointsUsed = validDataCountB
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
      indexStart As Integer, indexEnd As Integer,
      exclusionIndexStart As Integer, exclusionIndexEnd As Integer,
      baselineNoiseOptions As clsBaselineNoiseOptions,
      baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        ' Compute the average intensity level between indexStart and exclusionIndexStart
        ' Also compute the average between exclusionIndexEnd and indexEnd
        ' Use ComputeAverageNoiseLevelCheckCounts to determine whether both averages are used to determine
        '  the baseline noise level or whether just one of the averages is used

        Dim success = False

        ' Examine the exclusion range.  If the exclusion range excludes all
        '  data to the left or right of the peak, then use a few data points anyway, even if this does include some of the peak
        If exclusionIndexStart < indexStart + MINIMUM_PEAK_WIDTH Then
            exclusionIndexStart = indexStart + MINIMUM_PEAK_WIDTH
            If exclusionIndexStart >= indexEnd Then
                exclusionIndexStart = indexEnd - 1
            End If
        End If

        If exclusionIndexEnd > indexEnd - MINIMUM_PEAK_WIDTH Then
            exclusionIndexEnd = indexEnd - MINIMUM_PEAK_WIDTH
            If exclusionIndexEnd < 0 Then
                exclusionIndexEnd = 0
            End If
        End If

        If exclusionIndexStart >= indexStart AndAlso exclusionIndexStart <= indexEnd AndAlso
           exclusionIndexEnd >= exclusionIndexStart AndAlso exclusionIndexEnd <= indexEnd _
           Then

            Dim minimumPositiveValue = FindMinimumPositiveValue(sicData, 1)

            Dim validDataCountA = 0
            Dim sumA As Double = 0
            For i = indexStart To exclusionIndexStart
                sumA += Math.Max(minimumPositiveValue, sicData(i).Intensity)
                validDataCountA += 1
            Next

            Dim validDataCountB = 0
            Dim sumB As Double = 0
            For i = exclusionIndexEnd To indexEnd
                sumB += Math.Max(minimumPositiveValue, sicData(i).Intensity)
                validDataCountB += 1
            Next

            success = ComputeAverageNoiseLevelCheckCounts(
              validDataCountA, validDataCountB,
              sumA, sumB,
              MINIMUM_PEAK_WIDTH, baselineNoiseStats)

            ' Assure that .NoiseLevel is at least as large as minimumPositiveValue
            If baselineNoiseStats.NoiseLevel < minimumPositiveValue Then
                baselineNoiseStats.NoiseLevel = minimumPositiveValue
            End If

            ' Populate .NoiseStDev
            validDataCountA = 0
            validDataCountB = 0
            sumA = 0
            sumB = 0
            If baselineNoiseStats.PointsUsed > 0 Then
                For i = indexStart To exclusionIndexStart
                    sumA += (Math.Max(minimumPositiveValue, sicData(i).Intensity) - baselineNoiseStats.NoiseLevel) ^ 2
                    validDataCountA += 1
                Next

                For i = exclusionIndexEnd To indexEnd
                    sumB += (Math.Max(minimumPositiveValue, sicData(i).Intensity) - baselineNoiseStats.NoiseLevel) ^ 2
                    validDataCountB += 1
                Next
            End If

            If validDataCountA + validDataCountB > 0 Then
                baselineNoiseStats.NoiseStDev = Math.Sqrt((sumA + sumB) / (validDataCountA + validDataCountB))
            Else
                baselineNoiseStats.NoiseStDev = 0
            End If

        End If

        If Not success Then
            Dim baselineNoiseOptionsOverride = baselineNoiseOptions.Clone()

            With baselineNoiseOptionsOverride
                .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
                .TrimmedMeanFractionLowIntensityDataToAverage = 0.33
            End With

            Dim intensities = (From item In sicData Select item.Intensity).ToArray()

            success = ComputeTrimmedNoiseLevel(intensities, indexStart, indexEnd, baselineNoiseOptionsOverride, False, baselineNoiseStats)
        End If

        Return success

    End Function

    ''' <summary>
    ''' Divide the data into the number of segments given by baselineNoiseOptions.DualTrimmedMeanMaximumSegments  (use 3 by default)
    ''' Call ComputeDualTrimmedNoiseLevel for each segment
    ''' Use a TTest to determine whether we need to define a custom noise threshold for each segment
    ''' </summary>
    ''' <param name="dataList"></param>
    ''' <param name="indexStart"></param>
    ''' <param name="indexEnd"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="noiseStatsSegments"></param>
    ''' <returns>True if success, False if error</returns>
    Public Function ComputeDualTrimmedNoiseLevelTTest(
      dataList As IReadOnlyList(Of Double), indexStart As Integer, indexEnd As Integer,
      baselineNoiseOptions As clsBaselineNoiseOptions,
      <Out> ByRef noiseStatsSegments As List(Of clsBaselineNoiseStatsSegment)) As Boolean

        noiseStatsSegments = New List(Of clsBaselineNoiseStatsSegment)

        Try

            Dim segmentCountLocal = CInt(baselineNoiseOptions.DualTrimmedMeanMaximumSegments)
            If segmentCountLocal = 0 Then segmentCountLocal = 3
            If segmentCountLocal < 1 Then segmentCountLocal = 1

            ' Initialize BaselineNoiseStats for each segment now, in case an error occurs
            For i = 0 To segmentCountLocal - 1
                Dim baselineNoiseStats = InitializeBaselineNoiseStats(
                  baselineNoiseOptions.MinimumBaselineNoiseLevel,
                  eNoiseThresholdModes.DualTrimmedMeanByAbundance)

                noiseStatsSegments.Add(New clsBaselineNoiseStatsSegment(baselineNoiseStats))
            Next

            ' Determine the segment length
            Dim segmentLength = CInt(Math.Round((indexEnd - indexStart) / segmentCountLocal, 0))

            ' Initialize the first segment
            Dim firstSegment = noiseStatsSegments.First()
            firstSegment.SegmentIndexStart = indexStart
            If segmentCountLocal = 1 Then
                firstSegment.SegmentIndexEnd = indexEnd
            Else
                firstSegment.SegmentIndexEnd = firstSegment.SegmentIndexStart + segmentLength - 1
            End If

            ' Initialize the remaining segments
            For i = 1 To segmentCountLocal - 1
                noiseStatsSegments(i).SegmentIndexStart = noiseStatsSegments(i - 1).SegmentIndexEnd + 1
                If i = segmentCountLocal - 1 Then
                    noiseStatsSegments(i).SegmentIndexEnd = indexEnd
                Else
                    noiseStatsSegments(i).SegmentIndexEnd = noiseStatsSegments(i).SegmentIndexStart + segmentLength - 1
                End If
            Next

            ' Call ComputeDualTrimmedNoiseLevel for each segment
            For i = 0 To segmentCountLocal - 1
                Dim current = noiseStatsSegments(i)

                ComputeDualTrimmedNoiseLevel(dataList, current.SegmentIndexStart, current.SegmentIndexEnd, baselineNoiseOptions, current.BaselineNoiseStats)

            Next

            ' Compare adjacent segments using a T-Test, starting with the final segment and working backward
            Dim confidenceLevel = eTTestConfidenceLevelConstants.Conf90Pct
            Dim segmentIndex = segmentCountLocal - 1

            Do While segmentIndex > 0
                Dim previous = noiseStatsSegments(segmentIndex - 1)
                Dim current = noiseStatsSegments(segmentIndex)

                Dim significantDifference As Boolean
                Dim tCalculated As Double

                significantDifference = TestSignificanceUsingTTest(
                    current.BaselineNoiseStats.NoiseLevel,
                    previous.BaselineNoiseStats.NoiseLevel,
                    current.BaselineNoiseStats.NoiseStDev,
                    previous.BaselineNoiseStats.NoiseStDev,
                    current.BaselineNoiseStats.PointsUsed,
                    previous.BaselineNoiseStats.PointsUsed,
                    confidenceLevel,
                    tCalculated)

                If significantDifference Then
                    ' Significant difference; leave the 2 segments intact
                Else
                    ' Not a significant difference; recompute the Baseline Noise stats using the two segments combined
                    previous.SegmentIndexEnd = current.SegmentIndexEnd
                    ComputeDualTrimmedNoiseLevel(dataList,
                                                 previous.SegmentIndexStart,
                                                 previous.SegmentIndexEnd,
                                                 baselineNoiseOptions,
                                                 previous.BaselineNoiseStats)

                    For segmentIndexCopy = segmentIndex To segmentCountLocal - 2
                        noiseStatsSegments(segmentIndexCopy) = noiseStatsSegments(segmentIndexCopy + 1)
                    Next
                    segmentCountLocal -= 1
                End If
                segmentIndex -= 1
            Loop

            While noiseStatsSegments.Count > segmentCountLocal
                noiseStatsSegments.RemoveAt(noiseStatsSegments.Count - 1)
            End While

        Catch ex As Exception
            Return False
        End Try

        Return True

    End Function

    ''' <summary>
    '''  Computes the average of all of the data in dataList()
    '''  Next, discards the data above and below baselineNoiseOptions.DualTrimmedMeanStdDevLimits of the mean
    '''  Finally, recomputes the average using the data that remains
    ''' </summary>
    ''' <param name="dataList"></param>
    ''' <param name="indexStart"></param>
    ''' <param name="indexEnd"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="baselineNoiseStats"></param>
    ''' <returns>True if success, False if error (or no data in dataList)</returns>
    ''' <remarks>
    ''' Replaces values of 0 with the minimum positive value in dataList()
    ''' You cannot use dataList.Length to determine the length of the array; use indexStart and indexEnd to find the limits
    ''' </remarks>
    Public Function ComputeDualTrimmedNoiseLevel(dataList As IReadOnlyList(Of Double), indexStart As Integer, indexEnd As Integer,
                                                 baselineNoiseOptions As clsBaselineNoiseOptions,
                                                 <Out> ByRef baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        ' Initialize baselineNoiseStats
        baselineNoiseStats = InitializeBaselineNoiseStats(
          baselineNoiseOptions.MinimumBaselineNoiseLevel,
          eNoiseThresholdModes.DualTrimmedMeanByAbundance)

        If dataList Is Nothing OrElse indexEnd - indexStart < 0 Then
            Return False
        End If

        ' Copy the data into dataListSorted
        Dim dataSortedCount = indexEnd - indexStart + 1
        Dim dataListSorted() As Double
        ReDim dataListSorted(dataSortedCount - 1)

        For i = indexStart To indexEnd
            dataListSorted(i - indexStart) = dataList(i)
        Next

        ' Sort the array
        Array.Sort(dataListSorted)

        ' Look for the minimum positive value and replace all data in dataListSorted with that value
        Dim minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, dataListSorted)

        ' Initialize the indices to use in dataListSorted()
        Dim dataSortedIndexStart = 0
        Dim dataSortedIndexEnd = dataSortedCount - 1

        ' Compute the average using the data in dataListSorted between dataSortedIndexStart and dataSortedIndexEnd (i.e. all the data)
        Dim sum As Double = 0
        For i = dataSortedIndexStart To dataSortedIndexEnd
            sum += dataListSorted(i)
        Next

        Dim dataUsedCount = dataSortedIndexEnd - dataSortedIndexStart + 1
        Dim average = sum / dataUsedCount
        Dim variance As Double

        If dataUsedCount > 1 Then
            ' Compute the variance (this is a sample variance, not a population variance)
            sum = 0
            For i = dataSortedIndexStart To dataSortedIndexEnd
                sum += (dataListSorted(i) - average) ^ 2
            Next
            variance = sum / (dataUsedCount - 1)
        Else
            variance = 0
        End If

        If baselineNoiseOptions.DualTrimmedMeanStdDevLimits < 1 Then
            baselineNoiseOptions.DualTrimmedMeanStdDevLimits = 1
        End If

        ' Note: Standard Deviation = sigma = SquareRoot(Variance)
        Dim intensityThresholdMin = average - Math.Sqrt(variance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits
        Dim intensityThresholdMax = average + Math.Sqrt(variance) * baselineNoiseOptions.DualTrimmedMeanStdDevLimits

        ' Recompute the average using only the data between intensityThresholdMin and intensityThresholdMax in dataListSorted
        sum = 0
        Dim sortedIndex = dataSortedIndexStart
        Do While sortedIndex <= dataSortedIndexEnd
            If dataListSorted(sortedIndex) >= intensityThresholdMin Then
                dataSortedIndexStart = sortedIndex
                Do While sortedIndex <= dataSortedIndexEnd
                    If dataListSorted(sortedIndex) <= intensityThresholdMax Then
                        sum += dataListSorted(sortedIndex)
                    Else
                        dataSortedIndexEnd = sortedIndex - 1
                        Exit Do
                    End If
                    sortedIndex += 1
                Loop
            End If
            sortedIndex += 1
        Loop
        dataUsedCount = dataSortedIndexEnd - dataSortedIndexStart + 1

        If dataUsedCount > 0 Then
            baselineNoiseStats.NoiseLevel = sum / dataUsedCount

            ' Compute the variance (this is a sample variance, not a population variance)
            sum = 0
            For i = dataSortedIndexStart To dataSortedIndexEnd
                sum += (dataListSorted(i) - baselineNoiseStats.NoiseLevel) ^ 2
            Next

            If dataUsedCount > 1 Then
                baselineNoiseStats.NoiseStDev = Math.Sqrt(sum / (dataUsedCount - 1))
            Else
                baselineNoiseStats.NoiseStDev = 0
            End If
            baselineNoiseStats.PointsUsed = dataUsedCount
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
      subtractBaselineNoise As Boolean) As Integer

        ' Note: The calling function should have already populated sicPeak.MaxIntensityValue, plus .IndexMax, .IndexBaseLeft, and .IndexBaseRight
        ' If subtractBaselineNoise is True, then this function also uses sicPeak.BaselineNoiseStats....
        ' Note: This function returns the FWHM value in units of scan number; it does not update the value stored in sicPeak
        ' This function does, however, update sicPeak.IndexMax if it is not between sicPeak.IndexBaseLeft and sicPeak.IndexBaseRight

        Const ALLOW_NEGATIVE_VALUES = False
        Dim fwhmScanStart, fwhmScanEnd As Double
        Dim fwhmScans As Integer

        Dim targetIntensity As Double
        Dim maximumIntensity As Double

        Dim y1, y2 As Double

        ' Determine the full width at half max (fwhm), in units of absolute scan number
        Try

            If sicPeak.IndexMax <= sicPeak.IndexBaseLeft OrElse sicPeak.IndexMax >= sicPeak.IndexBaseRight Then
                ' Find the index of the maximum (between .IndexBaseLeft and .IndexBaseRight)
                maximumIntensity = 0
                If sicPeak.IndexMax < sicPeak.IndexBaseLeft OrElse sicPeak.IndexMax > sicPeak.IndexBaseRight Then
                    sicPeak.IndexMax = sicPeak.IndexBaseLeft
                End If

                For dataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                    If sicData(dataIndex).Intensity > maximumIntensity Then
                        sicPeak.IndexMax = dataIndex
                        maximumIntensity = sicData(dataIndex).Intensity
                    End If
                Next
            End If

            ' Look for the intensity halfway down the peak (correcting for baseline noise level if subtractBaselineNoise = True)
            If subtractBaselineNoise Then
                targetIntensity = BaselineAdjustIntensity(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES) / 2

                If targetIntensity <= 0 Then
                    ' The maximum intensity of the peak is below the baseline; do not correct for baseline noise level
                    targetIntensity = sicPeak.MaxIntensityValue / 2
                    subtractBaselineNoise = False
                End If
            Else
                targetIntensity = sicPeak.MaxIntensityValue / 2
            End If

            If targetIntensity > 0 Then

                ' Start the search at each peak edge to thus determine the largest fwhm value
                fwhmScanStart = -1
                For dataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexMax - 1
                    If subtractBaselineNoise Then
                        y1 = BaselineAdjustIntensity(sicData(dataIndex).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                        y2 = BaselineAdjustIntensity(sicData(dataIndex + 1).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                    Else
                        y1 = sicData(dataIndex).Intensity
                        y2 = sicData(dataIndex + 1).Intensity
                    End If

                    If y1 > targetIntensity OrElse y2 > targetIntensity Then
                        If y1 <= targetIntensity AndAlso y2 >= targetIntensity Then
                            InterpolateX(
                                fwhmScanStart,
                                sicData(dataIndex).ScanNumber, sicData(dataIndex + 1).ScanNumber,
                                y1, y2, targetIntensity)
                        Else
                            ' targetIntensity is not between y1 and y2; simply use dataIndex
                            If dataIndex = sicPeak.IndexBaseLeft Then
                                ' At the start of the peak; use the scan number halfway between .IndexBaseLeft and .IndexMax
                                fwhmScanStart = sicData(dataIndex + CInt(Math.Round((sicPeak.IndexMax - sicPeak.IndexBaseLeft) / 2, 0))).ScanNumber
                            Else
                                ' This code will probably never be reached
                                fwhmScanStart = sicData(dataIndex).ScanNumber
                            End If
                        End If
                        Exit For
                    End If
                Next
                If fwhmScanStart < 0 Then
                    If sicPeak.IndexMax > sicPeak.IndexBaseLeft Then
                        fwhmScanStart = sicData(sicPeak.IndexMax - 1).ScanNumber
                    Else
                        fwhmScanStart = sicData(sicPeak.IndexBaseLeft).ScanNumber
                    End If
                End If

                fwhmScanEnd = -1
                For dataIndex = sicPeak.IndexBaseRight - 1 To sicPeak.IndexMax Step -1
                    If subtractBaselineNoise Then
                        y1 = BaselineAdjustIntensity(sicData(dataIndex).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                        y2 = BaselineAdjustIntensity(sicData(dataIndex + 1).Intensity, sicPeak.BaselineNoiseStats.NoiseLevel, ALLOW_NEGATIVE_VALUES)
                    Else
                        y1 = sicData(dataIndex).Intensity
                        y2 = sicData(dataIndex + 1).Intensity
                    End If

                    If y1 > targetIntensity OrElse y2 > targetIntensity Then
                        If y1 >= targetIntensity AndAlso y2 <= targetIntensity Then
                            InterpolateX(
                                fwhmScanEnd,
                                sicData(dataIndex).ScanNumber, sicData(dataIndex + 1).ScanNumber,
                                y1, y2, targetIntensity)
                        Else
                            ' targetIntensity is not between y1 and y2; simply use dataIndex+1
                            If dataIndex = sicPeak.IndexBaseRight - 1 Then
                                ' At the end of the peak; use the scan number halfway between .IndexBaseRight and .IndexMax
                                fwhmScanEnd = sicData(dataIndex + 1 - CInt(Math.Round((sicPeak.IndexBaseRight - sicPeak.IndexMax) / 2, 0))).ScanNumber
                            Else
                                ' This code will probably never be reached
                                fwhmScanEnd = sicData(dataIndex + 1).ScanNumber
                            End If
                        End If
                        Exit For
                    End If
                Next
                If fwhmScanEnd < 0 Then
                    If sicPeak.IndexMax < sicPeak.IndexBaseRight Then
                        fwhmScanEnd = sicData(sicPeak.IndexMax + 1).ScanNumber
                    Else
                        fwhmScanEnd = sicData(sicPeak.IndexBaseRight).ScanNumber
                    End If
                End If

                fwhmScans = CInt(Math.Round(fwhmScanEnd - fwhmScanStart, 0))
                If fwhmScans <= 0 Then fwhmScans = 0
            Else
                ' Maximum intensity value is <= 0
                ' Set fwhm to 1
                fwhmScans = 1
            End If

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->ComputeFWHM", "Error finding fwhm", ex, False)
            fwhmScans = 0
        End Try

        Return fwhmScans

    End Function

    ' ReSharper disable once UnusedMember.Global
    Public Sub TestComputeKSStat()
        Dim scanNumbers = New Integer() {0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40}
        Dim intensities = New Double() {2, 5, 7, 10, 11, 18, 19, 15, 8, 4, 1}

        Dim scanAtApex = 20
        Dim fwhm As Double = 25

        Dim peakMean As Double = scanAtApex
        ' fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * Ln(2)))

        Dim peakStDev = fwhm / 2.35482
        'peakStDev = 28.8312

        ComputeKSStatistic(scanNumbers.Length, scanNumbers, intensities, peakMean, peakStDev)

        ' ToDo: Update program to call ComputeKSStatistic

        ' ToDo: Update Statistical Moments computation to:
        '  a) Create baseline adjusted intensity values
        '  b) Remove the contiguous data from either end that is <= 0
        '  c) Step through the remaining data and interpolate across gaps with intensities of 0 (linear interpolation)
        '  d) Use this final data to compute the statistical moments and KS Statistic

        ' If less than 3 points remain with the above procedure, then use the 5 points centered around the peak maximum, non-baseline corrected data

    End Sub

    Private Function ComputeKSStatistic(
      dataCount As Integer,
      xDataIn As IList(Of Integer),
      yDataIn As IList(Of Double),
      peakMean As Double,
      peakStDev As Double) As Double

        Dim xData() As Integer
        Dim yData() As Double

        Dim yDataNormalized() As Double
        Dim xDataPDF() As Double
        Dim yDataEDF() As Double
        Dim xDataCDF() As Double

        Dim yDataSum As Double

        ' Copy data from xDataIn() to xData, subtracting the value in xDataIn(0) from each scan
        ReDim xData(dataCount - 1)
        ReDim yData(dataCount - 1)

        Dim scanOffset = xDataIn(0)
        For i = 0 To dataCount - 1
            xData(i) = xDataIn(i) - scanOffset
            yData(i) = yDataIn(i)
        Next

        yDataSum = 0
        For i = 0 To yData.Length - 1
            yDataSum += yData(i)
        Next
        If Math.Abs(yDataSum) < Double.Epsilon Then yDataSum = 1

        ' Compute the Vector of normalized intensities = observed pdf
        ReDim yDataNormalized(yData.Length - 1)
        For i = 0 To yData.Length - 1
            yDataNormalized(i) = yData(i) / yDataSum
        Next

        ' Estimate the empirical distribution function (EDF) using an accumulating sum
        yDataSum = 0
        ReDim yDataEDF(yDataNormalized.Length - 1)
        For i = 0 To yDataNormalized.Length - 1
            yDataSum += yDataNormalized(i)
            yDataEDF(i) = yDataSum
        Next

        ' Compute the Vector of Normal PDF values evaluated at the X values in the peak window
        ReDim xDataPDF(xData.Length - 1)
        For i = 0 To xData.Length - 1
            xDataPDF(i) = (1 / (Math.Sqrt(2 * Math.PI) * peakStDev)) * Math.Exp((-1 / 2) *
                                    ((xData(i) - (peakMean - scanOffset)) / peakStDev) ^ 2)
        Next

        Dim xDataPDFSum As Double = 0
        For i = 0 To xDataPDF.Length - 1
            xDataPDFSum += xDataPDF(i)
        Next

        ' Estimate the theoretical CDF using an accumulating sum
        ReDim xDataCDF(xDataPDF.Length - 1)
        yDataSum = 0
        For i = 0 To xDataPDF.Length - 1
            yDataSum += xDataPDF(i)
            xDataCDF(i) = yDataSum / ((1 + (1 / xData.Length)) * xDataPDFSum)
        Next

        ' Compute the maximum of the absolute differences between the YData EDF and XData CDF
        Dim KS_gof As Double = 0
        For i = 0 To xDataCDF.Length - 1
            Dim compareVal = Math.Abs(yDataEDF(i) - xDataCDF(i))
            If compareVal > KS_gof Then
                KS_gof = compareVal
            End If
        Next

        Return Math.Sqrt(xData.Length) * KS_gof   '  return modified KS statistic

    End Function

    ''' <summary>
    ''' Compute the noise level
    ''' </summary>
    ''' <param name="dataCount"></param>
    ''' <param name="dataList"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="baselineNoiseStats"></param>
    ''' <returns>Returns True if success, false in an error</returns>
    ''' <remarks>Updates baselineNoiseStats with the baseline noise level</remarks>
    Public Function ComputeNoiseLevelForSICData(dataCount As Integer, dataList As IReadOnlyList(Of Double),
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
            Return ComputeDualTrimmedNoiseLevel(dataList, 0, dataCount - 1, baselineNoiseOptions, baselineNoiseStats)
        Else
            Return ComputeTrimmedNoiseLevel(dataList, 0, dataCount - 1, baselineNoiseOptions, IGNORE_NON_POSITIVE_DATA, baselineNoiseStats)
        End If
    End Function

    <Obsolete("Use the version that takes a List(Of clsSICDataPoint")>
    Public Function ComputeNoiseLevelInPeakVicinity(
      dataCount As Integer, sicScanNumbers() As Integer, sicIntensities() As Double,
      sicPeak As clsSICStatsPeak,
      baselineNoiseOptions As clsBaselineNoiseOptions) As Boolean

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To dataCount - 1
            sicData.Add(New clsSICDataPoint(sicScanNumbers(index), sicIntensities(index), 0))
        Next

        Return ComputeNoiseLevelInPeakVicinity(sicData, sicPeak, baselineNoiseOptions)

    End Function

    Public Function ComputeNoiseLevelInPeakVicinity(
      sicData As List(Of clsSICDataPoint),
      sicPeak As clsSICStatsPeak,
      baselineNoiseOptions As clsBaselineNoiseOptions) As Boolean

        Const NOISE_ESTIMATE_DATA_COUNT_MINIMUM = 5
        Const NOISE_ESTIMATE_DATA_COUNT_MAXIMUM = 100

        Dim success As Boolean

        ' Initialize baselineNoiseStats
        sicPeak.BaselineNoiseStats = InitializeBaselineNoiseStats(
          baselineNoiseOptions.MinimumBaselineNoiseLevel,
          eNoiseThresholdModes.MeanOfDataInPeakVicinity)

        ' Only use a portion of the data to compute the noise level
        ' The number of points to extend from the left and right is based on the width at 4 sigma; useful for tailing peaks
        ' Also, determine the peak start using the smaller of the width at 4 sigma vs. the observed peak width

        ' Estimate fwhm since it is sometimes not yet known when this function is called
        ' The reason it's not yet know is that the final fwhm value is computed using baseline corrected intensity data, but
        '  the whole purpose of this function is to compute the baseline level
        sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, False)

        ' Minimum of peak width at 4 sigma vs. peakWidthFullScans
        Dim peakWidthBaseScans = ComputeWidthAtBaseUsingFWHM(sicPeak, sicData, 4)
        Dim peakWidthPoints = ConvertScanWidthToPoints(peakWidthBaseScans, sicPeak, sicData)

        Dim peakHalfWidthPoints = CInt(Math.Round(peakWidthPoints / 1.5, 0))

        ' Make sure that peakHalfWidthPoints is at least NOISE_ESTIMATE_DATA_COUNT_MINIMUM
        If peakHalfWidthPoints < NOISE_ESTIMATE_DATA_COUNT_MINIMUM Then
            peakHalfWidthPoints = NOISE_ESTIMATE_DATA_COUNT_MINIMUM
        End If

        ' Copy the peak base indices
        Dim indexBaseLeft = sicPeak.IndexBaseLeft
        Dim indexBaseRight = sicPeak.IndexBaseRight

        ' Define IndexStart and IndexEnd, making sure that peakHalfWidthPoints is no larger than NOISE_ESTIMATE_DATA_COUNT_MAXIMUM
        Dim indexStart = indexBaseLeft - Math.Min(peakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM)
        Dim indexEnd = sicPeak.IndexBaseRight + Math.Min(peakHalfWidthPoints, NOISE_ESTIMATE_DATA_COUNT_MAXIMUM)

        If indexStart < 0 Then indexStart = 0
        If indexEnd >= sicData.Count Then indexEnd = sicData.Count - 1

        ' Compare indexStart to sicPeak.PreviousPeakFWHMPointRight
        ' If it is less than .PreviousPeakFWHMPointRight, then update accordingly
        If indexStart < sicPeak.PreviousPeakFWHMPointRight AndAlso
           sicPeak.PreviousPeakFWHMPointRight < sicPeak.IndexMax Then
            ' Update indexStart to be at PreviousPeakFWHMPointRight
            indexStart = sicPeak.PreviousPeakFWHMPointRight

            If indexStart < 0 Then indexStart = 0

            ' If not enough points, then alternately shift indexStart to the left 1 point and
            '  indexBaseLeft to the right one point until we do have enough points
            Dim shiftLeft = True
            Do While indexBaseLeft - indexStart + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM
                If shiftLeft Then
                    If indexStart > 0 Then indexStart -= 1
                Else
                    If indexBaseLeft < sicPeak.IndexMax Then indexBaseLeft += 1
                End If
                If indexStart <= 0 AndAlso indexBaseLeft >= sicPeak.IndexMax Then
                    Exit Do
                Else
                    shiftLeft = Not shiftLeft
                End If
            Loop
        End If

        ' Compare indexEnd to sicPeak.NextPeakFWHMPointLeft
        ' If it is greater than .NextPeakFWHMPointLeft, then update accordingly
        If indexEnd >= sicPeak.NextPeakFWHMPointLeft AndAlso
           sicPeak.NextPeakFWHMPointLeft > sicPeak.IndexMax Then
            indexEnd = sicPeak.NextPeakFWHMPointLeft

            If indexEnd >= sicData.Count Then indexEnd = sicData.Count - 1

            ' If not enough points, then alternately shift indexEnd to the right 1 point and
            '  indexBaseRight to the left one point until we do have enough points
            Dim shiftLeft = False
            Do While indexEnd - indexBaseRight + 1 < NOISE_ESTIMATE_DATA_COUNT_MINIMUM
                If shiftLeft Then
                    If indexBaseRight > sicPeak.IndexMax Then indexBaseRight -= 1
                Else
                    If indexEnd < sicData.Count - 1 Then indexEnd += 1
                End If
                If indexBaseRight <= sicPeak.IndexMax AndAlso indexEnd >= sicData.Count - 1 Then
                    Exit Do
                Else
                    shiftLeft = Not shiftLeft
                End If
            Loop
        End If

        success = ComputeAverageNoiseLevelExcludingRegion(
          sicData,
          indexStart, indexEnd,
          indexBaseLeft, indexBaseRight,
          baselineNoiseOptions, sicPeak.BaselineNoiseStats)

        ' Assure that .NoiseLevel is >= .MinimumBaselineNoiseLevel
        If sicPeak.BaselineNoiseStats.NoiseLevel < Math.Max(1, baselineNoiseOptions.MinimumBaselineNoiseLevel) Then
            Dim noiseStats = sicPeak.BaselineNoiseStats

            noiseStats.NoiseLevel = Math.Max(1, baselineNoiseOptions.MinimumBaselineNoiseLevel)

            ' Set this to 0 since we have overridden .NoiseLevel
            noiseStats.NoiseStDev = 0

            sicPeak.BaselineNoiseStats = noiseStats
        End If


        Return success

    End Function

    ''' <summary>
    ''' Determine the value for sicPeak.ParentIonIntensity
    ''' The goal is to determine the intensity that the SIC data has in one scan prior to sicPeak.IndexObserved
    ''' This intensity value may be an interpolated value between two observed SIC values
    ''' </summary>
    ''' <param name="dataCount"></param>
    ''' <param name="sicScanNumbers">List of scan numbers</param>
    ''' <param name="sicIntensities">List of intensities</param>
    ''' <param name="sicPeak"></param>
    ''' <param name="fragScanNumber"></param>
    ''' <returns></returns>
    <Obsolete("Use the version that takes a List(Of clsSICDataPoint")>
    Public Function ComputeParentIonIntensity(
      dataCount As Integer,
      sicScanNumbers() As Integer,
      sicIntensities() As Double,
      sicPeak As clsSICStatsPeak,
      fragScanNumber As Integer) As Boolean

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To dataCount - 1
            sicData.Add(New clsSICDataPoint(sicScanNumbers(index), sicIntensities(index), 0))
        Next

        Return ComputeParentIonIntensity(sicData, sicPeak, fragScanNumber)
    End Function

    ''' <summary>
    ''' Determine the value for sicPeak.ParentIonIntensity
    ''' The goal is to determine the intensity that the SIC data has in one scan prior to sicPeak.IndexObserved
    ''' This intensity value may be an interpolated value between two observed SIC values
    ''' </summary>
    ''' <param name="sicData"></param>
    ''' <param name="sicPeak"></param>
    ''' <param name="fragScanNumber"></param>
    ''' <returns></returns>
    Public Function ComputeParentIonIntensity(
      sicData As IList(Of clsSICDataPoint),
      sicPeak As clsSICStatsPeak,
      fragScanNumber As Integer) As Boolean

        Dim success As Boolean

        Try
            ' Lookup the scan number and intensity of the SIC scan at sicPeak.IndexObserved
            Dim x1 = sicData(sicPeak.IndexObserved).ScanNumber
            Dim y1 = sicData(sicPeak.IndexObserved).Intensity

            If x1 = fragScanNumber - 1 Then
                ' The fragmentation scan was the next scan after the SIC scan the data was observed in
                ' We can use y1 for .ParentIonIntensity
                sicPeak.ParentIonIntensity = y1
            ElseIf x1 >= fragScanNumber Then
                ' The fragmentation scan has the same scan number as the SIC scan just before it, or the SIC scan is greater than the fragmentation scan
                ' This shouldn't normally happen, but we'll account for the possibility anyway
                ' If the data file only has MS spectra and no MS/MS spectra, and if the parent ion is a custom M/Z value, then this code will be reached
                sicPeak.ParentIonIntensity = y1
            Else
                ' We need to perform some interpolation to determine .ParentIonIntensity
                ' Lookup the scan number and intensity of the next SIC scan
                If sicPeak.IndexObserved < sicData.Count - 1 Then
                    Dim x2 = sicData(sicPeak.IndexObserved + 1).ScanNumber
                    Dim y2 = sicData(sicPeak.IndexObserved + 1).Intensity
                    Dim interpolatedIntensity As Double

                    success = InterpolateY(interpolatedIntensity, x1, x2, y1, y2, fragScanNumber - 1)

                    If success Then
                        sicPeak.ParentIonIntensity = interpolatedIntensity
                    Else
                        ' Interpolation failed; use y1
                        sicPeak.ParentIonIntensity = y1
                    End If
                Else
                    ' Cannot interpolate; we'll have to use y1 as .ParentIonIntensity
                    sicPeak.ParentIonIntensity = y1
                End If
            End If

            success = True

        Catch ex As Exception
            ' Ignore errors here
            success = False
        End Try

        Return success

    End Function

    Private Function ComputeSICPeakArea(sicData As IList(Of clsSICDataPoint), sicPeak As clsSICStatsPeak) As Boolean
        ' The calling function must populate sicPeak.IndexMax, sicPeak.IndexBaseLeft, and sicPeak.IndexBaseRight

        Dim scanNumbers() As Integer
        Dim intensities() As Double

        Try

            ' Compute the peak area

            ' Copy the matching data from the source arrays to scanNumbers() and intensities
            ' When copying, assure that the first and last points have an intensity of 0

            ' We're reserving extra space in case we need to prepend or append a minimum value
            ReDim scanNumbers(sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2)
            ReDim intensities(sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 2)

            ' Define an intensity threshold of 5% of MaximumIntensity
            ' If the peak data is not flanked by points <= intensityThreshold, then we'll add them
            Dim intensityThreshold = sicData(sicPeak.IndexMax).Intensity * 0.05

            ' Estimate the average scan interval between each data point
            Dim avgScanInterval = CInt(Math.Round(ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight), 0))

            Dim areaDataBaseIndex As Integer
            If sicData(sicPeak.IndexBaseLeft).Intensity > intensityThreshold Then
                ' Prepend an intensity data point of intensityThreshold, with a scan number avgScanInterval less than the first scan number for the actual peak data
                scanNumbers(0) = sicData(sicPeak.IndexBaseLeft).ScanNumber - avgScanInterval
                intensities(0) = intensityThreshold
                'intensitiesSmoothed(0) = intensityThreshold
                areaDataBaseIndex = 1
            Else
                areaDataBaseIndex = 0
            End If

            ' Populate scanNumbers() and intensities()
            For dataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                Dim indexPointer = dataIndex - sicPeak.IndexBaseLeft + areaDataBaseIndex
                scanNumbers(indexPointer) = sicData(dataIndex).ScanNumber
                intensities(indexPointer) = sicData(dataIndex).Intensity
                'intensitiesSmoothed(indexPointer) = smoothedYDataSubset.Data(dataIndex - smoothedYDataSubset.DataStartIndex)
                'If intensitiesSmoothed(indexPointer) < 0 Then intensitiesSmoothed(indexPointer) = 0
            Next

            Dim areaDataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1 + areaDataBaseIndex

            If sicData(sicPeak.IndexBaseRight).Intensity > intensityThreshold Then
                ' Append an intensity data point of intensityThreshold, with a scan number avgScanInterval more than the last scan number for the actual peak data
                Dim dataIndex = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + areaDataBaseIndex + 1
                scanNumbers(dataIndex) = sicData(sicPeak.IndexBaseRight).ScanNumber + avgScanInterval
                intensities(dataIndex) = intensityThreshold
                areaDataCount += 1
                'intensitiesSmoothed(dataIndex) = intensityThreshold
            End If

            ' Compute the area
            ' Note that we're using real data for this and not smoothed data
            ' Also note that we're using raw data for the peak area (not baseline corrected values)
            Dim peakArea As Double = 0
            For dataIndex = 0 To areaDataCount - 2
                ' Use the Trapezoid area formula to compute the area slice to add to sicPeak.Area
                ' Area = 0.5 * DeltaX * (Y1 + Y2)
                Dim scanDelta = scanNumbers(dataIndex + 1) - scanNumbers(dataIndex)
                peakArea += 0.5 * scanDelta * (intensities(dataIndex) + intensities(dataIndex + 1))
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

    Private Function ComputeAvgScanInterval(sicData As IList(Of clsSICDataPoint), dataIndexStart As Integer, dataIndexEnd As Integer) As Double

        Dim scansPerPoint As Double

        Try
            ' Estimate the average scan interval between each data point
            If dataIndexEnd >= dataIndexStart Then
                scansPerPoint = (sicData(dataIndexEnd).ScanNumber - sicData(dataIndexStart).ScanNumber) / (dataIndexEnd - dataIndexStart + 1)
                If scansPerPoint < 1 Then scansPerPoint = 1
            Else
                scansPerPoint = 1
            End If
        Catch ex As Exception
            scansPerPoint = 1
        End Try

        Return scansPerPoint

    End Function

    Private Function ComputeStatisticalMomentsStats(
      sicData As IList(Of clsSICDataPoint),
      smoothedYDataSubset As clsSmoothedYDataSubset,
      sicPeak As clsSICStatsPeak) As Boolean

        ' The calling function must populate sicPeak.IndexMax, sicPeak.IndexBaseLeft, and sicPeak.IndexBaseRight
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

            Dim dataCount = sicPeak.IndexBaseRight - sicPeak.IndexBaseLeft + 1
            If dataCount < 1 Then
                ' Do not continue if less than one point across the peak
                Return False
            End If

            ' When reserving memory for these arrays, include room to add a minimum value at the beginning and end of the data, if needed
            ' Also, reserve space for a minimum of 5 elements
            Dim minimumDataCount = DEFAULT_MINIMUM_DATA_COUNT
            If minimumDataCount > dataCount Then
                minimumDataCount = 3
            End If

            Dim scanNumbers() As Integer         ' Contains values from sicData(x).ScanNumber
            Dim intensities() As Double          ' Contains values from sicData(x).Intensity subtracted by the baseline noise level; if the result is less than 0, then will contain 0

            ReDim scanNumbers(Math.Max(dataCount, minimumDataCount) + 1)
            ReDim intensities(scanNumbers.Length - 1)
            Dim useRawDataAroundMaximum = False

            ' Populate scanNumbers() and intensities()
            ' Simultaneously, determine the maximum intensity
            Dim maximumBaselineAdjustedIntensity As Double = 0
            Dim indexMaximumIntensity = 0

            If USE_SMOOTHED_DATA Then
                dataCount = 0
                For dataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                    Dim smoothedDataPointer = dataIndex - smoothedYDataSubset.DataStartIndex
                    If smoothedDataPointer >= 0 AndAlso smoothedDataPointer < smoothedYDataSubset.DataCount Then
                        scanNumbers(dataCount) = sicData(dataIndex).ScanNumber
                        intensities(dataCount) = BaselineAdjustIntensity(
                          smoothedYDataSubset.Data(smoothedDataPointer),
                          sicPeak.BaselineNoiseStats.NoiseLevel,
                          ALLOW_NEGATIVE_VALUES)

                        If intensities(dataCount) > maximumBaselineAdjustedIntensity Then
                            maximumBaselineAdjustedIntensity = intensities(dataCount)
                            indexMaximumIntensity = dataCount
                        End If
                        dataCount += 1
                    End If
                Next
            Else
                dataCount = 0
                For dataIndex = sicPeak.IndexBaseLeft To sicPeak.IndexBaseRight
                    scanNumbers(dataCount) = sicData(dataIndex).ScanNumber
                    intensities(dataCount) = BaselineAdjustIntensity(
                      sicData(dataIndex).Intensity,
                      sicPeak.BaselineNoiseStats.NoiseLevel,
                      ALLOW_NEGATIVE_VALUES)

                    If intensities(dataCount) > maximumBaselineAdjustedIntensity Then
                        maximumBaselineAdjustedIntensity = intensities(dataCount)
                        indexMaximumIntensity = dataCount
                    End If
                    dataCount += 1
                Next
            End If

            ' Define an intensity threshold of 10% of MaximumBaselineAdjustedIntensity
            Dim intensityThreshold = maximumBaselineAdjustedIntensity * 0.1
            If intensityThreshold < 1 Then intensityThreshold = 1

            ' Step left from indexMaximumIntensity to find the first data point < intensityThreshold
            ' Note that the final data will include one data point less than intensityThreshold at the beginning and end of the data
            Dim validDataIndexLeft = indexMaximumIntensity
            Do While validDataIndexLeft > 0 AndAlso intensities(validDataIndexLeft) >= intensityThreshold
                validDataIndexLeft -= 1
            Loop

            ' Step right from indexMaximumIntensity to find the first data point < intensityThreshold
            Dim validDataIndexRight = indexMaximumIntensity
            Do While validDataIndexRight < dataCount - 1 AndAlso intensities(validDataIndexRight) >= intensityThreshold
                validDataIndexRight += 1
            Loop

            If validDataIndexLeft > 0 OrElse validDataIndexRight < dataCount - 1 Then
                ' Shrink the arrays to only retain the data centered around indexMaximumIntensity and
                '  having and intensity >= intensityThreshold, though one additional data point is retained at the beginning and end of the data
                For dataIndex = validDataIndexLeft To validDataIndexRight
                    Dim indexPointer = dataIndex - validDataIndexLeft
                    scanNumbers(indexPointer) = scanNumbers(dataIndex)
                    intensities(indexPointer) = intensities(dataIndex)
                Next
                dataCount = validDataIndexRight - validDataIndexLeft + 1
            End If

            If dataCount < minimumDataCount Then
                useRawDataAroundMaximum = True
            Else
                ' Remove the contiguous data from the left that is < intensityThreshold, retaining one point < intensityThreshold
                ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                validDataIndexLeft = 0
                Do While validDataIndexLeft < dataCount - 1 AndAlso intensities(validDataIndexLeft + 1) < intensityThreshold
                    validDataIndexLeft += 1
                Loop

                If validDataIndexLeft >= dataCount - 1 Then
                    ' All of the data is <= intensityThreshold
                    useRawDataAroundMaximum = True
                Else
                    If validDataIndexLeft > 0 Then
                        ' Shrink the array to remove the values at the beginning that are < intensityThreshold, retaining one point < intensityThreshold
                        ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                        For dataIndex = validDataIndexLeft To dataCount - 1
                            Dim indexPointer = dataIndex - validDataIndexLeft
                            scanNumbers(indexPointer) = scanNumbers(dataIndex)
                            intensities(indexPointer) = intensities(dataIndex)
                        Next
                        dataCount -= validDataIndexLeft
                    End If

                    ' Remove the contiguous data from the right that is < intensityThreshold, retaining one point < intensityThreshold
                    ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
                    validDataIndexRight = dataCount - 1
                    Do While validDataIndexRight > 0 AndAlso intensities(validDataIndexRight - 1) < intensityThreshold
                        validDataIndexRight -= 1
                    Loop

                    If validDataIndexRight < dataCount - 1 Then
                        ' Shrink the array to remove the values at the end that are < intensityThreshold, retaining one point < intensityThreshold
                        ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this code will typically never be reached
                        dataCount = validDataIndexRight + 1
                    End If

                    ' Estimate the average scan interval between the data points in scanNumbers
                    Dim avgScanInterval = CInt(Math.Round(ComputeAvgScanInterval(sicData, 0, dataCount - 1), 0))

                    ' Make sure that intensities(0) is <= intensityThreshold
                    If intensities(0) > intensityThreshold Then
                        ' Prepend a data point with intensity intensityThreshold and with a scan number 1 less than the first scan number in the valid data
                        For dataIndex = dataCount To 1 Step -1
                            scanNumbers(dataIndex) = scanNumbers(dataIndex - 1)
                            intensities(dataIndex) = intensities(dataIndex - 1)
                        Next
                        scanNumbers(0) = scanNumbers(1) - avgScanInterval
                        intensities(0) = intensityThreshold
                        dataCount += 1
                    End If

                    ' Make sure that intensities(dataCount-1) is <= intensityThreshold
                    If intensities(dataCount - 1) > intensityThreshold Then
                        ' Append a data point with intensity intensityThreshold and with a scan number 1 more than the last scan number in the valid data
                        scanNumbers(dataCount) = scanNumbers(dataCount - 1) + avgScanInterval
                        intensities(dataCount) = intensityThreshold
                        dataCount += 1
                    End If

                End If
            End If

            If useRawDataAroundMaximum OrElse dataCount < minimumDataCount Then
                ' Populate scanNumbers() and intensities() with the five data points centered around sicPeak.IndexMax
                If USE_SMOOTHED_DATA Then
                    validDataIndexLeft = sicPeak.IndexMax - CInt(Math.Floor(minimumDataCount / 2))
                    If validDataIndexLeft < 0 Then validDataIndexLeft = 0
                    dataCount = 0
                    For dataIndex = validDataIndexLeft To Math.Min(validDataIndexLeft + minimumDataCount - 1, sicData.Count - 1)
                        Dim smoothedDataPointer = dataIndex - smoothedYDataSubset.DataStartIndex
                        If smoothedDataPointer >= 0 AndAlso smoothedDataPointer < smoothedYDataSubset.DataCount Then
                            If smoothedYDataSubset.Data(smoothedDataPointer) > 0 Then
                                scanNumbers(dataCount) = sicData(dataIndex).ScanNumber
                                intensities(dataCount) = smoothedYDataSubset.Data(smoothedDataPointer)
                                dataCount += 1
                            End If
                        End If
                    Next
                Else
                    validDataIndexLeft = sicPeak.IndexMax - CInt(Math.Floor(minimumDataCount / 2))
                    If validDataIndexLeft < 0 Then validDataIndexLeft = 0
                    dataCount = 0
                    For dataIndex = validDataIndexLeft To Math.Min(validDataIndexLeft + minimumDataCount - 1, sicData.Count - 1)
                        If sicData(dataIndex).Intensity > 0 Then
                            scanNumbers(dataCount) = sicData(dataIndex).ScanNumber
                            intensities(dataCount) = sicData(dataIndex).Intensity
                            dataCount += 1
                        End If
                    Next
                End If

                If dataCount < 3 Then
                    ' We don't even have 3 positive values in the raw data; do not continue
                    Return False
                End If
            End If

            ' Step through intensities and interpolate across gaps with intensities of 0
            ' Due to the algorithm used to find the contiguous data centered around the peak maximum, this will typically have no effect
            Dim pointIndex = 1
            Do While pointIndex < dataCount - 1
                If intensities(pointIndex) <= 0 Then
                    ' Current point has an intensity of 0
                    ' Find the next positive point
                    validDataIndexLeft = pointIndex + 1
                    Do While validDataIndexLeft < dataCount AndAlso intensities(validDataIndexLeft) <= 0
                        validDataIndexLeft += 1
                    Loop

                    ' Interpolate between pointIndex-1 and validDataIndexLeft
                    For indexPointer = pointIndex To validDataIndexLeft - 1
                        Dim interpolatedIntensity As Double

                        If InterpolateY(
                          interpolatedIntensity,
                          scanNumbers(pointIndex - 1), scanNumbers(validDataIndexLeft),
                          intensities(pointIndex - 1), intensities(validDataIndexLeft),
                          scanNumbers(indexPointer)) Then
                            intensities(indexPointer) = interpolatedIntensity
                        End If
                    Next
                    pointIndex = validDataIndexLeft + 1
                Else
                    pointIndex += 1
                End If
            Loop

            ' Compute the zeroth moment (m0)
            Dim peakArea As Double = 0
            For dataIndex = 0 To dataCount - 2
                ' Use the Trapezoid area formula to compute the area slice to add to peakArea
                ' Area = 0.5 * DeltaX * (Y1 + Y2)
                Dim scanDelta = scanNumbers(dataIndex + 1) - scanNumbers(dataIndex)
                peakArea += 0.5 * scanDelta * (intensities(dataIndex) + intensities(dataIndex + 1))
            Next

            ' For the first moment (m1), need to sum: intensity times scan number.
            ' For each of the moments, need to subtract scanNumbers(0) from the scan numbers since
            ' statistical moments calculations are skewed if the first X value is not zero.
            ' When ScanDelta is > 1, then need to interpolate.

            Dim moment1Sum = (scanNumbers(0) - scanNumbers(0)) * intensities(0)
            For dataIndex = 1 To dataCount - 1
                moment1Sum += (scanNumbers(dataIndex) - scanNumbers(0)) * intensities(dataIndex)

                Dim scanDelta = scanNumbers(dataIndex) - scanNumbers(dataIndex - 1)
                If scanDelta > 1 Then
                    ' Data points are more than 1 scan apart; need to interpolate values
                    ' However, no need to interpolate if both intensity values are 0
                    If intensities(dataIndex - 1) > 0 OrElse intensities(dataIndex) > 0 Then
                        For scanNumberInterpolate = scanNumbers(dataIndex - 1) + 1 To scanNumbers(dataIndex) - 1
                            ' Use InterpolateY() to fill in the scans between dataIndex-1 and dataIndex
                            Dim interpolatedIntensity As Double
                            If InterpolateY(
                              interpolatedIntensity,
                              scanNumbers(dataIndex - 1), scanNumbers(dataIndex),
                              intensities(dataIndex - 1), intensities(dataIndex),
                              scanNumberInterpolate) Then

                                moment1Sum += (scanNumberInterpolate - scanNumbers(0)) * interpolatedIntensity
                            End If
                        Next
                    End If
                End If
            Next

            If peakArea <= 0 Then
                ' Cannot compute the center of mass; use the scan at .IndexMax instead
                Dim centerOfMassDecimal As Double

                Dim indexPointer = sicPeak.IndexMax - sicPeak.IndexBaseLeft
                If indexPointer >= 0 AndAlso indexPointer < scanNumbers.Length Then
                    centerOfMassDecimal = scanNumbers(indexPointer)
                End If

                statMomentsData.CenterOfMassScan = CInt(Math.Round(centerOfMassDecimal, 0))
                statMomentsData.DataCountUsed = 1

            Else
                ' Area is positive; compute the center of mass

                Dim centerOfMassDecimal = moment1Sum / peakArea + scanNumbers(0)

                statMomentsData.Area = Math.Min(Double.MaxValue, peakArea)
                statMomentsData.CenterOfMassScan = CInt(Math.Round(centerOfMassDecimal, 0))
                statMomentsData.DataCountUsed = dataCount

                ' For the second moment (m2), need to sum: (ScanNumber - m1)^2 * Intensity
                ' For the third moment (m3), need to sum: (ScanNumber - m1)^3 * Intensity
                ' When ScanDelta is > 1, then need to interpolate
                Dim moment2Sum = ((scanNumbers(0) - centerOfMassDecimal) ^ 2) * intensities(0)
                Dim moment3Sum = ((scanNumbers(0) - centerOfMassDecimal) ^ 3) * intensities(0)
                For dataIndex = 1 To dataCount - 1
                    moment2Sum += ((scanNumbers(dataIndex) - centerOfMassDecimal) ^ 2) * intensities(dataIndex)
                    moment3Sum += ((scanNumbers(dataIndex) - centerOfMassDecimal) ^ 3) * intensities(dataIndex)

                    Dim scanDelta = scanNumbers(dataIndex) - scanNumbers(dataIndex - 1)
                    If scanDelta > 1 Then
                        ' Data points are more than 1 scan apart; need to interpolate values
                        ' However, no need to interpolate if both intensity values are 0
                        If intensities(dataIndex - 1) > 0 OrElse intensities(dataIndex) > 0 Then
                            For scanNumberInterpolate = scanNumbers(dataIndex - 1) + 1 To scanNumbers(dataIndex) - 1
                                ' Use InterpolateY() to fill in the scans between dataIndex-1 and dataIndex
                                Dim interpolatedIntensity As Double
                                If InterpolateY(
                                  interpolatedIntensity,
                                  scanNumbers(dataIndex - 1), scanNumbers(dataIndex),
                                  intensities(dataIndex - 1), intensities(dataIndex),
                                  scanNumberInterpolate) Then

                                    moment2Sum += ((scanNumberInterpolate - centerOfMassDecimal) ^ 2) * interpolatedIntensity
                                    moment3Sum += ((scanNumberInterpolate - centerOfMassDecimal) ^ 3) * interpolatedIntensity
                                End If
                            Next
                        End If
                    End If
                Next

                statMomentsData.StDev = Math.Sqrt(moment2Sum / peakArea)

                ' thirdMoment = moment3Sum / peakArea
                ' skew = thirdMoment / sigma^3
                ' skew = (moment3Sum / peakArea) / sigma^3
                If statMomentsData.StDev > 0 Then
                    statMomentsData.Skew = (moment3Sum / peakArea) / (statMomentsData.StDev ^ 3)
                    If Math.Abs(statMomentsData.Skew) < 0.0001 Then
                        statMomentsData.Skew = 0
                    End If
                Else
                    statMomentsData.Skew = 0
                End If

            End If

            Const useStatMomentsStats = True
            Dim peakMean As Double
            Dim peakStDev As Double

            If useStatMomentsStats Then
                peakMean = statMomentsData.CenterOfMassScan
                peakStDev = statMomentsData.StDev
            Else
                peakMean = sicData(sicPeak.IndexMax).ScanNumber
                ' fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * Ln(2)))
                peakStDev = sicPeak.FWHMScanWidth / 2.35482
            End If
            statMomentsData.KSStat = ComputeKSStatistic(dataCount, scanNumbers, intensities, peakMean, peakStDev)


        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->ComputeStatisticalMomentsStats", "Error computing statistical moments", ex, False)
            Return False
        End Try

        Return True

    End Function

    Public Shared Function ComputeSignalToNoise(signal As Double, noiseThresholdIntensity As Double) As Double

        If noiseThresholdIntensity > 0 Then
            Return signal / noiseThresholdIntensity
        Else
            Return 0
        End If

    End Function

    ''' <summary>
    ''' Computes a trimmed mean or trimmed median using the low intensity data up to baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage
    ''' Additionally, computes a full median using all data in dataList
    ''' If ignoreNonPositiveData is True, then removes data from dataList() less than zero 0 and less than .MinimumBaselineNoiseLevel
    ''' </summary>
    ''' <param name="dataList"></param>
    ''' <param name="indexStart"></param>
    ''' <param name="indexEnd"></param>
    ''' <param name="baselineNoiseOptions"></param>
    ''' <param name="ignoreNonPositiveData"></param>
    ''' <param name="baselineNoiseStats"></param>
    ''' <returns>Returns True if success, False if error (or no data in dataList)</returns>
    ''' <remarks>
    ''' Replaces values of 0 with the minimum positive value in dataList()
    ''' You cannot use dataList.Length to determine the length of the array; use dataCount
    ''' </remarks>
    Public Function ComputeTrimmedNoiseLevel(
      dataList As IReadOnlyList(Of Double), indexStart As Integer, indexEnd As Integer,
      baselineNoiseOptions As clsBaselineNoiseOptions,
      ignoreNonPositiveData As Boolean,
      <Out> ByRef baselineNoiseStats As clsBaselineNoiseStats) As Boolean

        Dim dataListSorted() As Double           ' Note: You cannot use dataListSorted.Length to determine the length of the array; use indexStart and indexEnd to find the limits

        ' Initialize baselineNoiseStats
        baselineNoiseStats = InitializeBaselineNoiseStats(baselineNoiseOptions.MinimumBaselineNoiseLevel, baselineNoiseOptions.BaselineNoiseMode)

        If dataList Is Nothing OrElse indexEnd - indexStart < 0 Then
            Return False
        End If

        ' Copy the data into dataListSorted
        Dim dataSortedCount = indexEnd - indexStart + 1
        ReDim dataListSorted(dataSortedCount - 1)

        For i = indexStart To indexEnd
            dataListSorted(i - indexStart) = dataList(i)
        Next

        ' Sort the array
        Array.Sort(dataListSorted)

        If ignoreNonPositiveData Then
            ' Remove data with a value <= 0

            If dataListSorted(0) <= 0 Then
                Dim validDataCount = 0
                For i = 0 To dataSortedCount - 1
                    If dataListSorted(i) > 0 Then
                        dataListSorted(validDataCount) = dataListSorted(i)
                        validDataCount += 1
                    End If
                Next

                If validDataCount < dataSortedCount Then
                    dataSortedCount = validDataCount
                End If

                ' Check for no data remaining
                If dataSortedCount <= 0 Then
                    Return False
                End If
            End If
        End If

        ' Look for the minimum positive value and replace all data in dataListSorted with that value
        Dim minimumPositiveValue = ReplaceSortedDataWithMinimumPositiveValue(dataSortedCount, dataListSorted)

        Select Case baselineNoiseOptions.BaselineNoiseMode
            Case eNoiseThresholdModes.TrimmedMeanByAbundance, eNoiseThresholdModes.TrimmedMeanByCount

                Dim countSummed As Integer
                Dim sum As Double

                If baselineNoiseOptions.BaselineNoiseMode = eNoiseThresholdModes.TrimmedMeanByAbundance Then
                    ' Average the data that has intensity values less than
                    '  Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)

                    Dim intensityThreshold As Double = dataListSorted(0) +
                                                       baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                                                       (dataListSorted(dataSortedCount - 1) - dataListSorted(0))

                    ' Initialize countSummed to dataSortedCount for now, in case all data is within the intensity threshold
                    countSummed = dataSortedCount
                    sum = 0
                    For i = 0 To dataSortedCount - 1
                        If dataListSorted(i) <= intensityThreshold Then
                            sum += dataListSorted(i)
                        Else
                            ' Update countSummed
                            countSummed = i
                            Exit For
                        End If
                    Next
                    indexEnd = countSummed - 1
                Else
                    ' eNoiseThresholdModes.TrimmedMeanByCount
                    ' Find the index of the data point at dataSortedCount * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage and
                    '  average the data from the start to that index
                    indexEnd = CInt(Math.Round((dataSortedCount - 1) * baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage, 0))

                    countSummed = indexEnd + 1
                    sum = 0
                    For i = 0 To indexEnd
                        sum += dataListSorted(i)
                    Next
                End If

                If countSummed > 0 Then
                    ' Compute the average
                    ' Note that countSummed will be used below in the variance computation
                    baselineNoiseStats.NoiseLevel = sum / CDbl(countSummed)
                    baselineNoiseStats.PointsUsed = countSummed

                    If countSummed > 1 Then
                        ' Compute the variance
                        sum = 0
                        For i = 0 To indexEnd
                            sum += (dataListSorted(i) - baselineNoiseStats.NoiseLevel) ^ 2
                        Next
                        baselineNoiseStats.NoiseStDev = Math.Sqrt(sum / CDbl(countSummed - 1))
                    Else
                        baselineNoiseStats.NoiseStDev = 0
                    End If
                Else
                    ' No data to average; define the noise level to be the minimum intensity
                    baselineNoiseStats.NoiseLevel = dataListSorted(0)
                    baselineNoiseStats.NoiseStDev = 0
                    baselineNoiseStats.PointsUsed = 1

                End If

            Case eNoiseThresholdModes.TrimmedMedianByAbundance
                If baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage >= 1 Then
                    indexEnd = dataSortedCount - 1
                Else
                    'Find the median of the data that has intensity values less than
                    '  Minimum + baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage * (Maximum - Minimum)
                    Dim intensityThreshold As Double = dataListSorted(0) +
                                                       baselineNoiseOptions.TrimmedMeanFractionLowIntensityDataToAverage *
                                                       (dataListSorted(dataSortedCount - 1) - dataListSorted(0))

                    ' Find the first point with an intensity value <= intensityThreshold
                    indexEnd = dataSortedCount - 1
                    For i = 1 To dataSortedCount - 1
                        If dataListSorted(i) > intensityThreshold Then
                            indexEnd = i - 1
                            Exit For
                        End If
                    Next
                End If

                If indexEnd Mod 2 = 0 Then
                    ' Even value
                    baselineNoiseStats.NoiseLevel = dataListSorted(CInt(indexEnd / 2))
                Else
                    ' Odd value; average the values on either side of indexEnd/2
                    Dim i = CInt((indexEnd - 1) / 2)
                    If i < 0 Then i = 0
                    Dim sum As Double = dataListSorted(i)

                    i += 1
                    If i = dataSortedCount Then i = dataSortedCount - 1
                    sum += dataListSorted(i)

                    baselineNoiseStats.NoiseLevel = sum / 2.0
                End If

                ' Compute the variance
                Dim varianceSum As Double = 0
                For i = 0 To indexEnd
                    varianceSum += (dataListSorted(i) - baselineNoiseStats.NoiseLevel) ^ 2
                Next

                Dim countSummed = indexEnd + 1
                If countSummed > 0 Then
                    baselineNoiseStats.NoiseStDev = Math.Sqrt(varianceSum / CDbl(countSummed - 1))
                Else
                    baselineNoiseStats.NoiseStDev = 0
                End If
                baselineNoiseStats.PointsUsed = countSummed
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
    ''' Computes the width of the peak (in scans) using the fwhm value in sicPeak
    ''' </summary>
    ''' <param name="sicPeak"></param>
    ''' <param name="sicData"></param>
    ''' <param name="sigmaValueForBase"></param>
    ''' <returns></returns>
    Private Shared Function ComputeWidthAtBaseUsingFWHM(
      sicPeak As clsSICStatsPeak,
      sicData As IList(Of clsSICDataPoint),
      sigmaValueForBase As Short) As Integer

        Dim peakWidthFullScans As Integer

        Try
            peakWidthFullScans = sicData(sicPeak.IndexBaseRight).ScanNumber - sicData(sicPeak.IndexBaseLeft).ScanNumber + 1
            Return ComputeWidthAtBaseUsingFWHM(sicPeak.FWHMScanWidth, peakWidthFullScans, sigmaValueForBase)
        Catch ex As Exception
            Return 0
        End Try

    End Function

    ''' <summary>
    '''  Computes the width of the peak (in scans) using the fwhm value
    ''' </summary>
    ''' <param name="sicPeakFWHMScans"></param>
    ''' <param name="sicPeakWidthFullScans"></param>
    ''' <param name="sigmaValueForBase"></param>
    ''' <returns></returns>
    ''' <remarks>Does not allow the width determined to be larger than sicPeakWidthFullScans</remarks>
    Private Shared Function ComputeWidthAtBaseUsingFWHM(
      sicPeakFWHMScans As Integer,
      sicPeakWidthFullScans As Integer,
      Optional sigmaValueForBase As Short = 4) As Integer

        Dim widthAtBase As Integer
        Dim sigmaBasedWidth As Integer

        If sigmaValueForBase < 4 Then sigmaValueForBase = 4

        If sicPeakFWHMScans = 0 Then
            widthAtBase = sicPeakWidthFullScans
        Else
            ' Compute the peak width
            ' Note: Sigma = fwhm / 2.35482 = fwhm / (2 * Sqrt(2 * Ln(2)))
            sigmaBasedWidth = CInt(sigmaValueForBase * sicPeakFWHMScans / 2.35482)

            If sigmaBasedWidth <= 0 Then
                widthAtBase = sicPeakWidthFullScans
            ElseIf sicPeakWidthFullScans = 0 Then
                widthAtBase = sigmaBasedWidth
            Else
                ' Compare the sigma-based peak width to sicPeakWidthFullScans
                ' Assign the smaller of the two values to widthAtBase
                widthAtBase = Math.Min(sigmaBasedWidth, sicPeakWidthFullScans)
            End If
        End If

        Return widthAtBase

    End Function

    ''' <summary>
    ''' Convert from peakWidthFullScans to points; estimate number of scans per point to get this
    ''' </summary>
    ''' <param name="peakWidthBaseScans"></param>
    ''' <param name="sicPeak"></param>
    ''' <param name="sicData"></param>
    ''' <returns></returns>
    Private Function ConvertScanWidthToPoints(
      peakWidthBaseScans As Integer,
      sicPeak As clsSICStatsPeak,
      sicData As IList(Of clsSICDataPoint)) As Integer

        Dim scansPerPoint = ComputeAvgScanInterval(sicData, sicPeak.IndexBaseLeft, sicPeak.IndexBaseRight)
        Return CInt(Math.Round(peakWidthBaseScans / scansPerPoint, 0))

    End Function

    ''' <summary>
    ''' Determine the minimum positive value in the list, or absoluteMinimumValue if the list is empty
    ''' </summary>
    ''' <param name="sicData"></param>
    ''' <param name="absoluteMinimumValue"></param>
    ''' <returns></returns>
    Public Function FindMinimumPositiveValue(sicData As IList(Of clsSICDataPoint), absoluteMinimumValue As Double) As Double

        Dim minimumPositiveValue = (From item In sicData Where item.Intensity > 0 Select item.Intensity).DefaultIfEmpty(absoluteMinimumValue).Min()
        If minimumPositiveValue < absoluteMinimumValue Then
            Return absoluteMinimumValue
        End If

        Return minimumPositiveValue

    End Function

    ''' <summary>
    ''' Determine the minimum positive value in the list, or absoluteMinimumValue if the list is empty
    ''' </summary>
    ''' <param name="dataList"></param>
    ''' <param name="absoluteMinimumValue"></param>
    ''' <returns></returns>
    Public Function FindMinimumPositiveValue(dataList As IList(Of Double), absoluteMinimumValue As Double) As Double

        Dim minimumPositiveValue = (From item In dataList Where item > 0 Select item).DefaultIfEmpty(absoluteMinimumValue).Min()
        If minimumPositiveValue < absoluteMinimumValue Then
            Return absoluteMinimumValue
        End If

        Return minimumPositiveValue

    End Function

    ''' <summary>
    ''' Determine the minimum positive value in the list, examining the first dataCount items
    ''' </summary>
    ''' <param name="dataCount"></param>
    ''' <param name="dataList"></param>
    ''' <param name="absoluteMinimumValue"></param>
    ''' <returns></returns>
    ''' <remarks>
    ''' Does not use dataList.Length to determine the length of the list; uses dataCount
    ''' However, if dataCount is > dataList.Length, dataList.Length-1 will be used for the maximum index to examine
    ''' </remarks>
    Public Function FindMinimumPositiveValue(dataCount As Integer, dataList As IReadOnlyList(Of Double), absoluteMinimumValue As Double) As Double

        If dataCount > dataList.Count Then
            dataCount = dataList.Count
        End If

        Dim minimumPositiveValue = (From item In dataList.Take(dataCount) Where item > 0 Select item).DefaultIfEmpty(absoluteMinimumValue).Min()
        If minimumPositiveValue < absoluteMinimumValue Then
            Return absoluteMinimumValue
        End If

        Return minimumPositiveValue

    End Function

    ''' <summary>
    ''' Find peaks in the scan/intensity data tracked by sicData
    ''' </summary>
    ''' <param name="sicData"></param>
    ''' <param name="peakIndexStart">Output</param>
    ''' <param name="peakIndexEnd">Output</param>
    ''' <param name="peakLocationIndex">Output</param>
    ''' <param name="previousPeakFWHMPointRight">Output</param>
    ''' <param name="nextPeakFWHMPointLeft">Output</param>
    ''' <param name="shoulderCount">Output</param>
    ''' <param name="smoothedYDataSubset">Output</param>
    ''' <param name="simDataPresent"></param>
    ''' <param name="sicPeakFinderOptions"></param>
    ''' <param name="sicNoiseThresholdIntensity"></param>
    ''' <param name="minimumPotentialPeakArea"></param>
    ''' <param name="returnClosestPeak">
    ''' When true, peakLocationIndex should be populated with the "best guess" location of the peak in the scanNumbers() and intensityData() arrays
    ''' The peak closest to peakLocationIndex will be the chosen peak, even if it is not the most intense peak found
    ''' </param>
    ''' <returns>Returns True if a valid peak is found in intensityData(), otherwise false</returns>
    Private Function FindPeaks(
      sicData As IList(Of clsSICDataPoint),
      ByRef peakIndexStart As Integer,
      ByRef peakIndexEnd As Integer,
      ByRef peakLocationIndex As Integer,
      ByRef previousPeakFWHMPointRight As Integer,
      ByRef nextPeakFWHMPointLeft As Integer,
      ByRef shoulderCount As Integer,
      <Out> ByRef smoothedYDataSubset As clsSmoothedYDataSubset,
      simDataPresent As Boolean,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      sicNoiseThresholdIntensity As Double,
      minimumPotentialPeakArea As Double,
      returnClosestPeak As Boolean) As Boolean

        Const SMOOTHED_DATA_PADDING_COUNT = 2

        Dim validPeakFound As Boolean

        smoothedYDataSubset = New clsSmoothedYDataSubset()

        Try
            Dim peakDetector As New clsPeakDetection()

            Dim peakData = New clsPeaksContainer() With {
                .SourceDataCount = sicData.Count
            }

            If peakData.SourceDataCount <= 1 Then
                ' Only 1 or fewer points in intensityData()
                ' No point in looking for a "peak"
                peakIndexStart = 0
                peakIndexEnd = 0
                peakLocationIndex = 0

                Return False
            End If

            ' Try to find the peak using the Peak Detector class
            ' First need to populate .XData() and copy from intensityData() to .YData()
            ' At the same time, find maximumIntensity and maximumPotentialPeakArea

            ' The peak finder class requires Arrays of type Double
            ' Copy the data from the source arrays into peakData.XData() and peakData.YData()
            ReDim peakData.XData(peakData.SourceDataCount - 1)
            ReDim peakData.YData(peakData.SourceDataCount - 1)

            Dim scanNumbers = (From item In sicData Select item.ScanNumber).ToArray()

            Dim maximumIntensity As Double = sicData(0).Intensity
            Dim maximumPotentialPeakArea As Double = 0
            Dim indexMaxIntensity = 0

            ' Initialize the intensity queue
            ' The queue is used to keep track of the most recent intensity values
            Dim intensityQueue As New Queue()

            Dim potentialPeakArea As Double = 0
            Dim dataPointCountAboveThreshold = 0

            For i = 0 To peakData.SourceDataCount - 1
                peakData.XData(i) = sicData(i).ScanNumber
                peakData.YData(i) = sicData(i).Intensity
                If peakData.YData(i) > maximumIntensity Then
                    maximumIntensity = peakData.YData(i)
                    indexMaxIntensity = i
                End If

                If sicData(i).Intensity >= sicNoiseThresholdIntensity Then
                    ' Add this intensity to potentialPeakArea
                    potentialPeakArea += sicData(i).Intensity
                    If intensityQueue.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum Then
                        ' Decrement potentialPeakArea by the oldest item in the queue
                        potentialPeakArea -= CDbl(intensityQueue.Dequeue())
                    End If
                    ' Add this intensity to the queue
                    intensityQueue.Enqueue(sicData(i).Intensity)

                    If potentialPeakArea > maximumPotentialPeakArea Then
                        maximumPotentialPeakArea = potentialPeakArea
                    End If

                    dataPointCountAboveThreshold += 1

                End If
            Next

            ' Determine the initial value for .PeakWidthPointsMinimum
            ' We will use maximumIntensity and minimumPeakIntensity to compute a S/N value to help pick .PeakWidthPointsMinimum

            ' Old: If sicPeakFinderOptions.SICNoiseThresholdIntensity < 1 Then sicPeakFinderOptions.SICNoiseThresholdIntensity = 1
            ' Old: peakAreaSignalToNoise = maximumIntensity / sicPeakFinderOptions.SICNoiseThresholdIntensity

            If minimumPotentialPeakArea < 1 Then minimumPotentialPeakArea = 1
            Dim peakAreaSignalToNoise = maximumPotentialPeakArea / minimumPotentialPeakArea
            If peakAreaSignalToNoise < 1 Then peakAreaSignalToNoise = 1


            If Math.Abs(sicPeakFinderOptions.ButterworthSamplingFrequency) < Double.Epsilon Then
                sicPeakFinderOptions.ButterworthSamplingFrequency = 0.25
            End If

            peakData.PeakWidthPointsMinimum = CInt(sicPeakFinderOptions.InitialPeakWidthScansScaler *
                                                   Math.Log10(Math.Floor(peakAreaSignalToNoise)) * 10)

            ' Assure that .InitialPeakWidthScansMaximum is no greater than .InitialPeakWidthScansMaximum
            '  and no greater than dataPointCountAboveThreshold/2 (rounded up)
            peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, sicPeakFinderOptions.InitialPeakWidthScansMaximum)
            peakData.PeakWidthPointsMinimum = Math.Min(peakData.PeakWidthPointsMinimum, CInt(Math.Ceiling(dataPointCountAboveThreshold / 2)))

            If peakData.PeakWidthPointsMinimum > peakData.SourceDataCount * 0.8 Then
                peakData.PeakWidthPointsMinimum = CInt(Math.Floor(peakData.SourceDataCount * 0.8))
            End If

            If peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH Then peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH

            ' Save the original value for peakLocationIndex
            peakData.OriginalPeakLocationIndex = peakLocationIndex
            peakData.MaxAllowedUpwardSpikeFractionMax = sicPeakFinderOptions.MaxAllowedUpwardSpikeFractionMax

            Do
                Dim testingMinimumPeakWidth As Boolean

                If peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH Then
                    testingMinimumPeakWidth = True
                Else
                    testingMinimumPeakWidth = False
                End If

                Try
                    validPeakFound = FindPeaksWork(
                      peakDetector, scanNumbers, peakData,
                      simDataPresent, sicPeakFinderOptions,
                      testingMinimumPeakWidth, returnClosestPeak)

                Catch ex As Exception
                    LogErrors("clsMASICPeakFinder->FindPeaks", "Error calling FindPeaksWork", ex, True)
                    validPeakFound = False
                    Exit Do
                End Try

                If validPeakFound Then

                    ' For each peak, see if several zero intensity values are in a row in the raw data
                    ' If found, then narrow the peak to leave just one zero intensity value
                    For peakIndexCompare = 0 To peakData.Peaks.Count - 1
                        Dim currentPeak = peakData.Peaks(peakIndexCompare)

                        Do While currentPeak.LeftEdge < sicData.Count - 1 AndAlso
                            currentPeak.LeftEdge < currentPeak.RightEdge
                            If Math.Abs(sicData(currentPeak.LeftEdge).Intensity) < Double.Epsilon AndAlso
                                Math.Abs(sicData(currentPeak.LeftEdge + 1).Intensity) < Double.Epsilon Then
                                currentPeak.LeftEdge += 1
                            Else
                                Exit Do
                            End If
                        Loop

                        Do While currentPeak.RightEdge > 0 AndAlso
                            currentPeak.RightEdge > currentPeak.LeftEdge
                            If Math.Abs(sicData(currentPeak.RightEdge).Intensity) < Double.Epsilon AndAlso
                                Math.Abs(sicData(currentPeak.RightEdge - 1).Intensity) < Double.Epsilon Then
                                currentPeak.RightEdge -= 1
                            Else
                                Exit Do
                            End If
                        Loop

                    Next

                    ' Update the stats for the "official" peak
                    Dim bestPeak = peakData.Peaks(peakData.BestPeakIndex)

                    peakLocationIndex = bestPeak.PeakLocation
                    peakIndexStart = bestPeak.LeftEdge
                    peakIndexEnd = bestPeak.RightEdge

                    ' Copy the smoothed Y data for the peak into smoothedYDataSubset.Data()
                    ' Include some data to the left and right of the peak start and end
                    ' Additionally, be sure the smoothed data includes the data around the most intense data point in intensityData
                    Dim smoothedYDataStartIndex = peakIndexStart - SMOOTHED_DATA_PADDING_COUNT
                    Dim smoothedYDataEndIndex = peakIndexEnd + SMOOTHED_DATA_PADDING_COUNT

                    ' Make sure the maximum intensity point is included (with padding on either side)
                    If indexMaxIntensity - SMOOTHED_DATA_PADDING_COUNT < smoothedYDataStartIndex Then
                        smoothedYDataStartIndex = indexMaxIntensity - SMOOTHED_DATA_PADDING_COUNT
                    End If

                    If indexMaxIntensity + SMOOTHED_DATA_PADDING_COUNT > smoothedYDataEndIndex Then
                        smoothedYDataEndIndex = indexMaxIntensity + SMOOTHED_DATA_PADDING_COUNT
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
                        validPeakFound = FindPeaksWork(
                          peakDetector, scanNumbers, peakData,
                          simDataPresent, sicPeakFinderOptions,
                          True, returnClosestPeak)

                        If validPeakFound Then
                            shoulderCount = 0

                            For Each peakItem In peakData.Peaks
                                If peakItem.PeakLocation >= peakIndexStart AndAlso peakItem.PeakLocation <= peakIndexEnd Then
                                    ' The peak at i has a peak center between the "official" peak's boundaries
                                    ' Make sure it's not the same peak as the "official" peak
                                    If peakItem.PeakLocation <> peakLocationIndex Then
                                        ' Now see if the comparison peak's intensity is at least .IntensityThresholdFractionMax of the intensity of the "official" peak
                                        If sicData(peakItem.PeakLocation).Intensity >= sicPeakFinderOptions.IntensityThresholdFractionMax * sicData(peakLocationIndex).Intensity Then
                                            ' Yes, this is a shoulder peak
                                            shoulderCount += 1
                                        End If
                                    End If
                                End If

                            Next
                        End If
                    Else
                        shoulderCount = 0
                    End If

                    ' Make sure peakLocationIndex really is the point with the highest intensity (in the smoothed data)
                    maximumIntensity = peakData.SmoothedYData(peakLocationIndex)
                    For i = peakIndexStart To peakIndexEnd
                        If peakData.SmoothedYData(i) > maximumIntensity Then
                            ' A more intense data point was found; update peakLocationIndex
                            maximumIntensity = peakData.SmoothedYData(i)
                            peakLocationIndex = i
                        End If
                    Next


                    Dim comparisonPeakEdgeIndex As Integer
                    Dim targetIntensity As Double
                    Dim dataIndex As Integer

                    ' Populate previousPeakFWHMPointRight and nextPeakFWHMPointLeft
                    Dim adjacentPeakIntensityThreshold = sicData(peakLocationIndex).Intensity / 3

                    ' Search through peakDataSaved to find the closest peak (with a significant intensity) to the left of this peak
                    ' Note that the peaks in peakDataSaved are not necessarily ordered by increasing index,
                    '  thus the need for an exhaustive search

                    Dim smallestIndexDifference = sicData.Count + 1
                    For peakIndexCompare = 0 To peakDataSaved.Peaks.Count - 1
                        Dim comparisonPeak = peakDataSaved.Peaks(peakIndexCompare)

                        If peakIndexCompare <> peakDataSaved.BestPeakIndex AndAlso
                           comparisonPeak.PeakLocation <= peakIndexStart Then
                            ' The peak is before peakIndexStart; is its intensity large enough?
                            If sicData(comparisonPeak.PeakLocation).Intensity >= adjacentPeakIntensityThreshold Then
                                ' Yes, the intensity is large enough

                                ' Initialize comparisonPeakEdgeIndex to the right edge of the adjacent peak
                                comparisonPeakEdgeIndex = comparisonPeak.RightEdge

                                ' Find the first point in the adjacent peak that is at least 50% of the maximum in the adjacent peak
                                ' Store that point in comparisonPeakEdgeIndex
                                targetIntensity = sicData(comparisonPeak.PeakLocation).Intensity / 2
                                For dataIndex = comparisonPeakEdgeIndex To comparisonPeak.PeakLocation Step -1
                                    If sicData(dataIndex).Intensity >= targetIntensity Then
                                        comparisonPeakEdgeIndex = dataIndex
                                        Exit For
                                    End If
                                Next

                                ' Assure that comparisonPeakEdgeIndex is less than peakIndexStart
                                If comparisonPeakEdgeIndex >= peakIndexStart Then
                                    comparisonPeakEdgeIndex = peakIndexStart - 1
                                    If comparisonPeakEdgeIndex < 0 Then comparisonPeakEdgeIndex = 0
                                End If

                                ' Possibly update previousPeakFWHMPointRight
                                If peakIndexStart - comparisonPeakEdgeIndex <= smallestIndexDifference Then
                                    previousPeakFWHMPointRight = comparisonPeakEdgeIndex
                                    smallestIndexDifference = peakIndexStart - comparisonPeakEdgeIndex
                                End If
                            End If
                        End If
                    Next

                    ' Search through peakDataSaved to find the closest peak to the right of this peak
                    smallestIndexDifference = sicData.Count + 1
                    For peakIndexCompare = peakDataSaved.Peaks.Count - 1 To 0 Step -1
                        Dim comparisonPeak = peakDataSaved.Peaks(peakIndexCompare)

                        If peakIndexCompare <> peakDataSaved.BestPeakIndex AndAlso
                           comparisonPeak.PeakLocation >= peakIndexEnd Then

                            ' The peak is after peakIndexEnd; is its intensity large enough?
                            If sicData(comparisonPeak.PeakLocation).Intensity >= adjacentPeakIntensityThreshold Then
                                ' Yes, the intensity is large enough

                                ' Initialize comparisonPeakEdgeIndex to the left edge of the adjacent peak
                                comparisonPeakEdgeIndex = comparisonPeak.LeftEdge

                                ' Find the first point in the adjacent peak that is at least 50% of the maximum in the adjacent peak
                                ' Store that point in comparisonPeakEdgeIndex
                                targetIntensity = sicData(comparisonPeak.PeakLocation).Intensity / 2
                                For dataIndex = comparisonPeakEdgeIndex To comparisonPeak.PeakLocation
                                    If sicData(dataIndex).Intensity >= targetIntensity Then
                                        comparisonPeakEdgeIndex = dataIndex
                                        Exit For
                                    End If
                                Next

                                ' Assure that comparisonPeakEdgeIndex is greater than peakIndexEnd
                                If peakIndexEnd >= comparisonPeakEdgeIndex Then
                                    comparisonPeakEdgeIndex = peakIndexEnd + 1
                                    If comparisonPeakEdgeIndex >= sicData.Count Then comparisonPeakEdgeIndex = sicData.Count - 1
                                End If

                                ' Possibly update nextPeakFWHMPointLeft
                                If comparisonPeakEdgeIndex - peakIndexEnd <= smallestIndexDifference Then
                                    nextPeakFWHMPointLeft = comparisonPeakEdgeIndex
                                    smallestIndexDifference = comparisonPeakEdgeIndex - peakIndexEnd
                                End If
                            End If
                        End If
                    Next


                Else
                    ' No peaks or no peaks containing .OriginalPeakLocationIndex
                    ' If peakData.PeakWidthPointsMinimum is greater than 3 and testingMinimumPeakWidth = False, then decrement it by 50%
                    If peakData.PeakWidthPointsMinimum > MINIMUM_PEAK_WIDTH AndAlso Not testingMinimumPeakWidth Then
                        peakData.PeakWidthPointsMinimum = CInt(Math.Floor(peakData.PeakWidthPointsMinimum / 2))
                        If peakData.PeakWidthPointsMinimum < MINIMUM_PEAK_WIDTH Then peakData.PeakWidthPointsMinimum = MINIMUM_PEAK_WIDTH
                    Else
                        peakLocationIndex = peakData.OriginalPeakLocationIndex
                        peakIndexStart = peakData.OriginalPeakLocationIndex
                        peakIndexEnd = peakData.OriginalPeakLocationIndex
                        previousPeakFWHMPointRight = peakIndexStart
                        nextPeakFWHMPointLeft = peakIndexEnd
                        validPeakFound = True
                    End If
                End If
            Loop While Not validPeakFound

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->FindPeaks", "Error in FindPeaks", ex, False)
            validPeakFound = False
        End Try

        Return validPeakFound

    End Function

    ''' <summary>
    ''' Find peaks
    ''' </summary>
    ''' <param name="peakDetector">peak detector object</param>
    ''' <param name="scanNumbers">Scan numbers of the data tracked by peaksContainer</param>
    ''' <param name="peaksContainer">Container object with XData, YData, SmoothedData, found Peaks, and various tracking properties</param>
    ''' <param name="simDataPresent">
    ''' Set to true if processing selected ion monitoring data (or if there are huge gaps in the scan numbers).
    ''' When true, and if sicPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData is true, uses a larger Butterworth filter sampling frequency
    ''' </param>
    ''' <param name="sicPeakFinderOptions">Peak finder options</param>
    ''' <param name="testingMinimumPeakWidth">When true, assure that at least one peak is returned</param>
    ''' <param name="returnClosestPeak">
    ''' When true, a valid peak is one that contains peaksContainer.OriginalPeakLocationIndex
    ''' When false, stores the index of the most intense peak in peaksContainer.BestPeakIndex
    ''' </param>
    ''' <returns>True if a valid peak is found; otherwise, returns false</returns>
    ''' <remarks>All of the identified peaks are returned in peaksContainer.Peaks(), regardless of whether they are valid or not</remarks>
    Private Function FindPeaksWork(
      peakDetector As clsPeakDetection,
      scanNumbers As IList(Of Integer),
      peaksContainer As clsPeaksContainer,
      simDataPresent As Boolean,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      testingMinimumPeakWidth As Boolean,
      returnClosestPeak As Boolean) As Boolean


        Dim errorMessage As String = String.Empty

        ' Smooth the Y data, and store in peaksContainer.SmoothedYData
        ' Note that if using a Butterworth filter, then we increase peaksContainer.PeakWidthPointsMinimum if too small, compared to 1/SamplingFrequency
        Dim dataIsSmoothed = FindPeaksWorkSmoothData(
          peaksContainer, simDataPresent,
          sicPeakFinderOptions, peaksContainer.PeakWidthPointsMinimum,
          errorMessage)

        If sicPeakFinderOptions.FindPeaksOnSmoothedData AndAlso dataIsSmoothed Then
            peaksContainer.Peaks = peakDetector.DetectPeaks(
              peaksContainer.XData,
              peaksContainer.SmoothedYData,
              sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
              peaksContainer.PeakWidthPointsMinimum,
              CInt(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, True, True)

            ' usedSmoothedDataForPeakDetection = True
        Else
            ' Look for the peaks, using peaksContainer.PeakWidthPointsMinimum as the minimum peak width
            peaksContainer.Peaks = peakDetector.DetectPeaks(
              peaksContainer.XData,
              peaksContainer.YData,
              sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum,
              peaksContainer.PeakWidthPointsMinimum,
              CInt(sicPeakFinderOptions.IntensityThresholdFractionMax * 100), 2, True, True)
            ' usedSmoothedDataForPeakDetection = False
        End If

        If peaksContainer.Peaks.Count = -1 Then
            ' Fatal error occurred while finding peaks
            Return False
        End If

        Dim peakMaximum = peaksContainer.YData.Max()

        If testingMinimumPeakWidth Then
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
                    Dim success = False
                    For foundPeakIndex = 0 To peaksContainer.Peaks.Count - 1
                        If Math.Abs(peaksContainer.Peaks(foundPeakIndex).PeakLocation - peaksContainer.OriginalPeakLocationIndex) <= 1 Then
                            success = True
                            Exit For
                        End If
                    Next

                    If Not success Then
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
            return False
        end if

        For Each peakItem In peaksContainer.Peaks

            peakItem.PeakIsValid = False

            ' Find the center and boundaries of this peak

            ' Copy from the PeakEdges arrays to the working variables
            Dim peakLocationIndex = peakItem.PeakLocation
            Dim peakIndexStart = peakItem.LeftEdge
            Dim peakIndexEnd = peakItem.RightEdge

            ' Make sure peakLocationIndex is between peakIndexStart and peakIndexEnd
            If peakIndexStart > peakLocationIndex Then
                LogErrors("clsMasicPeakFinder->FindPeaksWork",
                          "peakIndexStart is > peakLocationIndex; this is probably a programming error", Nothing, False)
                peakIndexStart = peakLocationIndex
            End If

            If peakIndexEnd < peakLocationIndex Then
                LogErrors("clsMasicPeakFinder->FindPeaksWork",
                          "peakIndexEnd is < peakLocationIndex; this is probably a programming error", Nothing, False)
                peakIndexEnd = peakLocationIndex
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

            If Not dataIsSmoothed Then
                ' Need to smooth the data now
                dataIsSmoothed = FindPeaksWorkSmoothData(
                 peaksContainer, simDataPresent,
                 sicPeakFinderOptions, peaksContainer.PeakWidthPointsMinimum, errorMessage)
            End If

            ' First see if we need to narrow the peak by looking for decreasing intensities moving toward the peak center
            ' We'll use the unsmoothed data for this
            Do While peakIndexStart < peakLocationIndex - 1
                If peaksContainer.YData(peakIndexStart) > peaksContainer.YData(peakIndexStart + 1) Then
                    ' OrElse (usedSmoothedDataForPeakDetection AndAlso peaksContainer.SmoothedYData(peakIndexStart) < 0) Then
                    peakIndexStart += 1
                Else
                    Exit Do
                End If
            Loop

            Do While peakIndexEnd > peakLocationIndex + 1
                If peaksContainer.YData(peakIndexEnd - 1) < peaksContainer.YData(peakIndexEnd) Then
                    ' OrElse (usedSmoothedDataForPeakDetection AndAlso peaksContainer.SmoothedYData(peakIndexEnd) < 0) Then
                    peakIndexEnd -= 1
                Else
                    Exit Do
                End If
            Loop

            ' Now see if we need to expand the peak by looking for decreasing intensities moving away from the peak center,
            '  but allowing for small increases
            ' We'll use the smoothed data for this; if we encounter negative values in the smoothed data, we'll keep going until we reach the low point since huge peaks can cause some odd behavior with the Butterworth filter
            ' Keep track of the number of times we step over an increased value
            Dim stepOverIncreaseCount = 0
            Do While peakIndexStart > 0
                'currentSlope = peakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, peakIndexStart, peakLocationIndex)

                'If currentSlope > 0 AndAlso _
                '   peakLocationIndex - peakIndexStart > 3 AndAlso _
                '   peaksContainer.SmoothedYData(peakIndexStart - 1) < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * peakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum) Then
                '    ' We reached a low intensity data point and we're going downhill (i.e. the slope from this point to peakLocationIndex is positive)
                '    ' Step once more and stop
                '    peakIndexStart -= 1
                '    Exit Do

                If peaksContainer.SmoothedYData(peakIndexStart - 1) < peaksContainer.SmoothedYData(peakIndexStart) Then
                    ' The adjacent point is lower than the current point
                    peakIndexStart -= 1
                ElseIf Math.Abs(peaksContainer.SmoothedYData(peakIndexStart - 1) -
                                peaksContainer.SmoothedYData(peakIndexStart)) < Double.Epsilon Then
                    ' The adjacent point is equal to the current point
                    peakIndexStart -= 1
                Else
                    ' The next point to the left is not lower; what about the point after it?
                    If peakIndexStart > 1 Then
                        If peaksContainer.SmoothedYData(peakIndexStart - 2) <= peaksContainer.SmoothedYData(peakIndexStart) Then
                            ' Only allow ignoring an upward spike if the delta from this point to the next is <= .MaxAllowedUpwardSpikeFractionMax of peakMaximum
                            If peaksContainer.SmoothedYData(peakIndexStart - 1) - peaksContainer.SmoothedYData(peakIndexStart) >
                               peaksContainer.MaxAllowedUpwardSpikeFractionMax * peakMaximum Then
                                Exit Do
                            End If

                            If dataIsSmoothed Then
                                ' Only ignore an upward spike twice if the data is smoothed
                                If stepOverIncreaseCount >= 2 Then Exit Do
                            End If

                            peakIndexStart -= 1

                            stepOverIncreaseCount += 1
                        Else
                            Exit Do
                        End If
                    Else
                        Exit Do
                    End If
                End If
            Loop

            stepOverIncreaseCount = 0
            Do While peakIndexEnd < peaksContainer.SourceDataCount - 1
                'currentSlope = peakDetector.ComputeSlope(peaksContainer.XData, peaksContainer.SmoothedYData, peakLocationIndex, peakIndexEnd)

                'If currentSlope < 0 AndAlso _
                '   peakIndexEnd - peakLocationIndex > 3 AndAlso _
                '   peaksContainer.SmoothedYData(peakIndexEnd + 1) < Math.Max(sicPeakFinderOptions.IntensityThresholdFractionMax * peakMaximum, sicPeakFinderOptions.IntensityThresholdAbsoluteMinimum) Then
                '    ' We reached a low intensity data point and we're going downhill (i.e. the slope from peakLocationIndex to this point is negative)
                '    peakIndexEnd += 1
                '    Exit Do

                If peaksContainer.SmoothedYData(peakIndexEnd + 1) < peaksContainer.SmoothedYData(peakIndexEnd) Then
                    ' The adjacent point is lower than the current point
                    peakIndexEnd += 1
                ElseIf Math.Abs(peaksContainer.SmoothedYData(peakIndexEnd + 1) -
                                peaksContainer.SmoothedYData(peakIndexEnd)) < Double.Epsilon Then
                    ' The adjacent point is equal to the current point
                    peakIndexEnd += 1
                Else
                    ' The next point to the right is not lower; what about the point after it?
                    If peakIndexEnd < peaksContainer.SourceDataCount - 2 Then
                        If peaksContainer.SmoothedYData(peakIndexEnd + 2) <= peaksContainer.SmoothedYData(peakIndexEnd) Then
                            ' Only allow ignoring an upward spike if the delta from this point to the next is <= .MaxAllowedUpwardSpikeFractionMax of peakMaximum
                            If peaksContainer.SmoothedYData(peakIndexEnd + 1) - peaksContainer.SmoothedYData(peakIndexEnd) >
                               peaksContainer.MaxAllowedUpwardSpikeFractionMax * peakMaximum Then
                                Exit Do
                            End If

                            If dataIsSmoothed Then
                                ' Only ignore an upward spike twice if the data is smoothed
                                If stepOverIncreaseCount >= 2 Then Exit Do
                            End If

                            peakIndexEnd += 1

                            stepOverIncreaseCount += 1
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
                ' If peaksContainer.OriginalPeakLocationIndex is not between peakIndexStart and peakIndexEnd, then check
                '  if the scan number for peaksContainer.OriginalPeakLocationIndex is within .MaxDistanceScansNoOverlap scans of
                '  either of the peak edges; if not, then mark the peak as invalid since it does not contain the
                '  scan for the parent ion
                If peaksContainer.OriginalPeakLocationIndex < peakIndexStart Then
                    If Math.Abs(scanNumbers(peaksContainer.OriginalPeakLocationIndex) -
                                scanNumbers(peakIndexStart)) > sicPeakFinderOptions.MaxDistanceScansNoOverlap Then
                        peakItem.PeakIsValid = False
                    End If
                ElseIf peaksContainer.OriginalPeakLocationIndex > peakIndexEnd Then
                    If Math.Abs(scanNumbers(peaksContainer.OriginalPeakLocationIndex) -
                                scanNumbers(peakIndexEnd)) > sicPeakFinderOptions.MaxDistanceScansNoOverlap Then
                        peakItem.PeakIsValid = False
                    End If
                End If
            End If

            ' Copy back from the working variables to the PeakEdges arrays
            peakItem.PeakLocation = peakLocationIndex
            peakItem.LeftEdge = peakIndexStart
            peakItem.RightEdge = peakIndexEnd

        Next

        ' Find the peak with the largest area that has peaksContainer.PeakIsValid = True
        peaksContainer.BestPeakIndex = -1
        peaksContainer.BestPeakArea = Double.MinValue
        For foundPeakIndex = 0 To peaksContainer.Peaks.Count - 1
            Dim currentPeak = peaksContainer.Peaks(foundPeakIndex)

            If currentPeak.PeakIsValid Then
                If currentPeak.PeakArea > peaksContainer.BestPeakArea Then
                    peaksContainer.BestPeakIndex = foundPeakIndex
                    peaksContainer.BestPeakArea = Math.Min(currentPeak.PeakArea, Double.MaxValue)
                End If
            End If
        Next

        If peaksContainer.BestPeakIndex >= 0 Then
            Return true
        Else
            return false
        End If

    End Function

    Private Function FindPeaksWorkSmoothData(
      peaksContainer As clsPeaksContainer,
      simDataPresent As Boolean,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      ByRef peakWidthPointsMinimum As Integer,
      ByRef errorMessage As String) As Boolean

        ' Returns True if the data was smoothed; false if not or an error
        ' The smoothed data is returned in peakData.SmoothedYData

        Dim filterThirdWidth As Integer
        Dim success As Boolean

        Dim butterWorthFrequency As Double

        Dim peakWidthPointsCompare As Integer

        Dim filter As New DataFilter.DataFilter()

        ReDim peaksContainer.SmoothedYData(peaksContainer.SourceDataCount - 1)

        If (peakWidthPointsMinimum > 4 AndAlso (sicPeakFinderOptions.UseSavitzkyGolaySmooth OrElse sicPeakFinderOptions.UseButterworthSmooth)) OrElse
         sicPeakFinderOptions.SmoothDataRegardlessOfMinimumPeakWidth Then

            peaksContainer.YData.CopyTo(peaksContainer.SmoothedYData, 0)

            If sicPeakFinderOptions.UseButterworthSmooth Then
                ' Filter the data with a Butterworth filter (.UseButterworthSmooth takes precedence over .UseSavitzkyGolaySmooth)
                If simDataPresent AndAlso sicPeakFinderOptions.ButterworthSamplingFrequencyDoubledForSIMData Then
                    butterWorthFrequency = sicPeakFinderOptions.ButterworthSamplingFrequency * 2
                Else
                    butterWorthFrequency = sicPeakFinderOptions.ButterworthSamplingFrequency
                End If
                success = filter.ButterworthFilter(
                  peaksContainer.SmoothedYData, 0,
                  peaksContainer.SourceDataCount - 1, butterWorthFrequency)
                If Not success Then
                    LogErrors("clsMasicPeakFinder->FindPeaksWorkSmoothData",
                              "Error with the Butterworth filter" & errorMessage, Nothing, False)
                    Return False
                Else
                    ' Data was smoothed
                    ' Validate that peakWidthPointsMinimum is large enough
                    If butterWorthFrequency > 0 Then
                        peakWidthPointsCompare = CInt(Math.Round(1 / butterWorthFrequency, 0))
                        If peakWidthPointsMinimum < peakWidthPointsCompare Then
                            peakWidthPointsMinimum = peakWidthPointsCompare
                        End If
                    End If

                    Return True
                End If

            Else
                ' Filter the data with a Savitzky Golay filter
                filterThirdWidth = CInt(Math.Floor(peaksContainer.PeakWidthPointsMinimum / 3))
                If filterThirdWidth > 3 Then filterThirdWidth = 3

                ' Make sure filterThirdWidth is Odd
                If filterThirdWidth Mod 2 = 0 Then
                    filterThirdWidth -= 1
                End If

                ' Note that the SavitzkyGolayFilter doesn't work right for PolynomialDegree values greater than 0
                ' Also note that a PolynomialDegree value of 0 results in the equivalent of a moving average filter
                success = filter.SavitzkyGolayFilter(
                  peaksContainer.SmoothedYData, 0,
                  peaksContainer.SmoothedYData.Length - 1,
                  filterThirdWidth, filterThirdWidth,
                  sicPeakFinderOptions.SavitzkyGolayFilterOrder, errorMessage, True)

                If Not success Then
                    LogErrors("clsMasicPeakFinder->FindPeaksWorkSmoothData",
                              "Error with the Savitzky-Golay filter: " & errorMessage, Nothing, False)
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
      dataCount As Integer,
      sicIntensities As IReadOnlyList(Of Double),
      <Out> ByRef potentialAreaStats As clsSICPotentialAreaStats,
      sicPeakFinderOptions As clsSICPeakFinderOptions)

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To dataCount - 1
            sicData.Add(New clsSICDataPoint(0, sicIntensities(index), 0))
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

        ' Note: You cannot use SICData.Length to determine the length of the array; use dataCount

        Dim minimumPositiveValue As Double
        Dim intensityToUse As Double

        Dim oldestIntensity As Double
        Dim potentialPeakArea, minimumPotentialPeakArea As Double
        Dim peakCountBasisForMinimumPotentialArea As Integer
        Dim validPeakCount As Integer

        ' The queue is used to keep track of the most recent intensity values
        Dim intensityQueue As New Queue()

        minimumPotentialPeakArea = Double.MaxValue
        peakCountBasisForMinimumPotentialArea = 0

        If sicData.Count > 0 Then

            intensityQueue.Clear()
            potentialPeakArea = 0
            validPeakCount = 0

            ' Find the minimum intensity in SICData()
            minimumPositiveValue = FindMinimumPositiveValue(sicData, 1)

            For i = 0 To sicData.Count - 1

                ' If this data point is > .MinimumBaselineNoiseLevel, then add this intensity to potentialPeakArea
                '  and increment validPeakCount
                intensityToUse = Math.Max(minimumPositiveValue, sicData(i).Intensity)
                If intensityToUse >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel Then
                    potentialPeakArea += intensityToUse
                    validPeakCount += 1
                End If

                If intensityQueue.Count >= sicPeakFinderOptions.InitialPeakWidthScansMaximum Then
                    ' Decrement potentialPeakArea by the oldest item in the queue
                    ' If that item is >= .MinimumBaselineNoiseLevel, then decrement validPeakCount too
                    oldestIntensity = CDbl(intensityQueue.Dequeue())

                    If oldestIntensity >= sicPeakFinderOptions.SICBaselineNoiseOptions.MinimumBaselineNoiseLevel AndAlso
                       oldestIntensity > 0 Then
                        potentialPeakArea -= oldestIntensity
                        validPeakCount -= 1
                    End If
                End If
                ' Add this intensity to the queue
                intensityQueue.Enqueue(intensityToUse)

                If potentialPeakArea > 0 AndAlso validPeakCount >= MINIMUM_PEAK_WIDTH Then
                    If validPeakCount > peakCountBasisForMinimumPotentialArea Then
                        ' The non valid peak count value is larger than the one associated with the current
                        '  minimum potential peak area; update the minimum peak area to potentialPeakArea
                        minimumPotentialPeakArea = potentialPeakArea
                        peakCountBasisForMinimumPotentialArea = validPeakCount
                    Else
                        If potentialPeakArea < minimumPotentialPeakArea AndAlso
                           validPeakCount = peakCountBasisForMinimumPotentialArea Then
                            minimumPotentialPeakArea = potentialPeakArea
                        End If
                    End If
                End If
            Next

        End If

        If minimumPotentialPeakArea >= Double.MaxValue Then
            minimumPotentialPeakArea = 1
        End If

        potentialAreaStats = New clsSICPotentialAreaStats() With {
            .MinimumPotentialPeakArea = minimumPotentialPeakArea,
            .PeakCountBasisForMinimumPotentialArea = peakCountBasisForMinimumPotentialArea
        }

    End Sub

    ''' <summary>
    ''' Find SIC Peak and Area
    ''' </summary>
    ''' <param name="dataCount"></param>
    ''' <param name="sicScanNumbers"></param>
    ''' <param name="sicIntensities"></param>
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
      dataCount As Integer,
      sicScanNumbers() As Integer,
      sicIntensities() As Double,
      <Out> ByRef potentialAreaStatsForPeak As clsSICPotentialAreaStats,
      sicPeak As clsSICStatsPeak,
      <Out> ByRef smoothedYDataSubset As clsSmoothedYDataSubset,
      sicPeakFinderOptions As clsSICPeakFinderOptions,
      potentialAreaStatsForRegion As clsSICPotentialAreaStats,
      returnClosestPeak As Boolean,
      simDataPresent As Boolean,
      recomputeNoiseLevel As Boolean) As Boolean

        Dim sicData = New List(Of clsSICDataPoint)

        For index = 0 To dataCount - 1
            sicData.Add(New clsSICDataPoint(sicScanNumbers(index), sicIntensities(index), 0))
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

        ' Note: The calling function should populate sicPeak.IndexObserved with the index in SICData() that the
        '       parent ion m/z was actually observed; this will be used as the default peak location if a peak cannot be found

        ' Set simDataPresent to True when there are large gaps in the survey scan numbers

        Dim dataIndex As Integer
        Dim intensityCompare As Double

        Dim success As Boolean

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
                    success = ComputeNoiseLevelForSICData(
                        sicData.Count, intensities,
                      sicPeakFinderOptions.SICBaselineNoiseOptions,
                      sicPeak.BaselineNoiseStats)
                End If


                ' Use a peak-finder algorithm to find the peak closest to .Peak.IndexMax
                ' Note that .Peak.IndexBaseLeft, .Peak.IndexBaseRight, and .Peak.IndexMax are passed ByRef and get updated by FindPeaks
                With sicPeak
                    success = FindPeaks(sicData, .IndexBaseLeft, .IndexBaseRight, .IndexMax,
                      .PreviousPeakFWHMPointRight, .NextPeakFWHMPointLeft, .ShoulderCount,
                      smoothedYDataSubset, simDataPresent, sicPeakFinderOptions,
                                           sicPeak.BaselineNoiseStats.NoiseLevel,
                      potentialAreaStatsForRegion.MinimumPotentialPeakArea,
                      returnClosestPeak)
                End With

                If success Then
                    ' Update the maximum peak intensity (required prior to call to ComputeNoiseLevelInPeakVicinity and call to ComputeSICPeakArea)
                    sicPeak.MaxIntensityValue = sicData(sicPeak.IndexMax).Intensity

                    If recomputeNoiseLevel Then
                        ' Update the value for potentialAreaStatsForPeak.SICNoiseThresholdIntensity based on the data around the peak
                        success = ComputeNoiseLevelInPeakVicinity(
                          sicData, sicPeak,
                          sicPeakFinderOptions.SICBaselineNoiseOptions)
                    End If

                    '' ' Compute the trimmed median of the data in SICData (replacing non positive values with the minimum)
                    '' ' If the median is less than sicPeak.BaselineNoiseStats.NoiseLevel then update sicPeak.BaselineNoiseStats.NoiseLevel
                    ''noiseOptionsOverride = sicPeakFinderOptions.SICBaselineNoiseOptions
                    ''With noiseOptionsOverride
                    ''    .BaselineNoiseMode = eNoiseThresholdModes.TrimmedMedianByAbundance
                    ''    .TrimmedMeanFractionLowIntensityDataToAverage = 0.75
                    ''End With
                    ''success = ComputeNoiseLevelForSICData(sicData, noiseOptionsOverride, noiseStatsCompare)
                    ''With noiseStatsCompare
                    ''    If .PointsUsed >= MINIMUM_NOISE_SCANS_REQUIRED Then
                    ''        ' Check whether the comparison noise level is less than the existing noise level times 0.75
                    ''        If .NoiseLevel < sicPeak.BaselineNoiseStats.NoiseLevel * 0.75 Then
                    ''            ' Yes, the comparison noise level is lower
                    ''            ' Use a T-Test to see if the comparison noise level is significantly different than the primary noise level
                    ''            If TestSignificanceUsingTTest(.NoiseLevel, sicPeak.BaselineNoiseStats.NoiseLevel, .NoiseStDev, sicPeak.BaselineNoiseStats.NoiseStDev, .PointsUsed, sicPeak.BaselineNoiseStats.PointsUsed, eTTestConfidenceLevelConstants.Conf95Pct, tCalculated) Then
                    ''                sicPeak.BaselineNoiseStats = noiseStatsCompare
                    ''            End If
                    ''        End If
                    ''    End If
                    ''End With

                    ' If smoothing was enabled, then see if the smoothed value is larger than sicPeak.MaxIntensityValue
                    ' If it is, then use the smoothed value for sicPeak.MaxIntensityValue
                    If sicPeakFinderOptions.UseSavitzkyGolaySmooth OrElse sicPeakFinderOptions.UseButterworthSmooth Then
                        dataIndex = sicPeak.IndexMax - smoothedYDataSubset.DataStartIndex
                        If dataIndex >= 0 AndAlso Not smoothedYDataSubset.Data Is Nothing AndAlso
                           dataIndex < smoothedYDataSubset.DataCount Then
                            ' Possibly use the intensity of the smoothed data as the peak intensity
                            intensityCompare = smoothedYDataSubset.Data(dataIndex)
                            If intensityCompare > sicPeak.MaxIntensityValue Then
                                sicPeak.MaxIntensityValue = intensityCompare
                            End If
                        End If
                    End If

                    ' Compute the signal to noise ratio for the peak
                    sicPeak.SignalToNoiseRatio = ComputeSignalToNoise(sicPeak.MaxIntensityValue, sicPeak.BaselineNoiseStats.NoiseLevel)

                    ' Compute the Full Width at Half Max (fwhm) value, this time subtracting the noise level from the baseline
                    sicPeak.FWHMScanWidth = ComputeFWHM(sicData, sicPeak, True)

                    ' Compute the Area (this function uses .FWHMScanWidth and therefore needs to be called after ComputeFWHM)
                    success = ComputeSICPeakArea(sicData, sicPeak)

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

            Return True

        Catch ex As Exception
            LogErrors("clsMASICPeakFinder->FindSICPeakAndArea", "Error finding SIC peaks and their areas", ex, False)
            Return False
        End Try

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

        Try
            Dim versionInfo = Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString()
            Return versionInfo
        Catch ex As Exception
            Return "??.??.??.??"
        End Try

    End Function

    Public Shared Function InitializeBaselineNoiseStats(
      minimumBaselineNoiseLevel As Double,
      noiseThresholdMode As eNoiseThresholdModes) As clsBaselineNoiseStats

        Dim baselineNoiseStats = New clsBaselineNoiseStats() With {
            .NoiseLevel = minimumBaselineNoiseLevel,
            .NoiseStDev = 0,
            .PointsUsed = 0,
            .NoiseThresholdModeUsed = noiseThresholdMode
        }

        Return baselineNoiseStats

    End Function

    <Obsolete("Use the version that returns baselineNoiseStatsType")>
    Public Shared Sub InitializeBaselineNoiseStats(
      ByRef baselineNoiseStats As clsBaselineNoiseStats,
      minimumBaselineNoiseLevel As Double,
      noiseThresholdMode As eNoiseThresholdModes)

        baselineNoiseStats = InitializeBaselineNoiseStats(minimumBaselineNoiseLevel, noiseThresholdMode)

    End Sub

    ''' <summary>
    ''' Determines the X value that corresponds to targetY by interpolating the line between (X1, Y1) and (X2, Y2)
    ''' </summary>
    ''' <param name="interpolatedXValue"></param>
    ''' <param name="x1"></param>
    ''' <param name="x2"></param>
    ''' <param name="y1"></param>
    ''' <param name="y2"></param>
    ''' <param name="targetY"></param>
    ''' <returns>Returns True on success, false on error</returns>
    Private Function InterpolateX(
      <Out> ByRef interpolatedXValue As Double,
      x1 As Integer, x2 As Integer,
      y1 As Double, y2 As Double,
      targetY As Double) As Boolean

        Dim deltaY = y2 - y1                                 ' This is y-two minus y-one
        Dim ratio = (targetY - y1) / deltaY
        Dim deltaX = x2 - x1                                 ' This is x-two minus x-one

        Dim targetX = ratio * deltaX + x1

        If Math.Abs(targetX - x1) >= 0 AndAlso Math.Abs(targetX - x2) >= 0 Then
            interpolatedXValue = targetX
            Return True
        Else
            LogErrors("clsMasicPeakFinder->InterpolateX", "TargetX is not between X1 and X2; this shouldn't happen", Nothing, False)
            interpolatedXValue = 0
            Return False
        End If

    End Function

    ''' <summary>
    ''' Determines the Y value that corresponds to xValToInterpolate by interpolating the line between (X1, Y1) and (X2, Y2)
    ''' </summary>
    ''' <param name="interpolatedIntensity"></param>
    ''' <param name="X1"></param>
    ''' <param name="X2"></param>
    ''' <param name="Y1"></param>
    ''' <param name="Y2"></param>
    ''' <param name="xValToInterpolate"></param>
    ''' <returns></returns>
    Private Function InterpolateY(
      <Out> ByRef interpolatedIntensity As Double,
      X1 As Integer, X2 As Integer,
      Y1 As Double, Y2 As Double,
      xValToInterpolate As Double) As Boolean

        Dim scanDifference = X2 - X1
        If scanDifference <> 0 Then
            interpolatedIntensity = Y1 + (Y2 - Y1) * ((xValToInterpolate - X1) / scanDifference)
            Return True
        Else
            ' xValToInterpolate is not between X1 and X2; cannot interpolate
            interpolatedIntensity = 0
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
      scanIndexObserved As Integer,
      noiseStatsSegments As List(Of clsBaselineNoiseStatsSegment)) As clsBaselineNoiseStats

        Dim noiseSegmentIndex As Integer
        Dim indexSegmentA As Integer
        Dim indexSegmentB As Integer

        Dim baselineNoiseStats As clsBaselineNoiseStats = Nothing
        Dim segmentMidPointA As Integer
        Dim segmentMidPointB As Integer
        Dim matchFound As Boolean

        Dim fractionFromSegmentB As Double
        Dim fractionFromSegmentA As Double

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

            ' Initialize indexSegmentA and indexSegmentB to 0, indicating no extrapolation needed
            indexSegmentA = 0
            indexSegmentB = 0
            matchFound = False                ' Next, see if scanIndexObserved matches any of the segments (provided more than one segment exists)
            For noiseSegmentIndex = 0 To noiseStatsSegments.Count - 1
                Dim current = noiseStatsSegments(noiseSegmentIndex)

                If scanIndexObserved >= current.SegmentIndexStart AndAlso scanIndexObserved <= current.SegmentIndexEnd Then
                    segmentMidPointA = current.SegmentIndexStart + CInt((current.SegmentIndexEnd - current.SegmentIndexStart) / 2)
                    matchFound = True
                End If

                If matchFound Then
                    baselineNoiseStats = current.BaselineNoiseStats.Clone()

                    If scanIndexObserved < segmentMidPointA Then
                        If noiseSegmentIndex > 0 Then
                            ' Need to Interpolate using this segment and the next one
                            indexSegmentA = noiseSegmentIndex - 1
                            indexSegmentB = noiseSegmentIndex

                            ' Copy segmentMidPointA to segmentMidPointB since the current segment is actually segment B
                            ' Define segmentMidPointA
                            segmentMidPointB = segmentMidPointA
                            Dim previous = noiseStatsSegments(noiseSegmentIndex - 1)
                            segmentMidPointA = previous.SegmentIndexStart + CInt((previous.SegmentIndexEnd - previous.SegmentIndexStart) / 2)

                        Else
                            ' scanIndexObserved occurs before the midpoint, but we're in the first segment; no need to Interpolate
                        End If
                    ElseIf scanIndexObserved > segmentMidPointA Then
                        If noiseSegmentIndex < noiseStatsSegments.Count - 1 Then
                            ' Need to Interpolate using this segment and the one before it
                            indexSegmentA = noiseSegmentIndex
                            indexSegmentB = noiseSegmentIndex + 1

                            ' Note: segmentMidPointA is already defined since the current segment is segment A
                            ' Define segmentMidPointB
                            Dim nextSegment = noiseStatsSegments(noiseSegmentIndex + 1)

                            segmentMidPointB = nextSegment.SegmentIndexStart + CInt((nextSegment.SegmentIndexEnd - nextSegment.SegmentIndexStart) / 2)

                        Else
                            ' scanIndexObserved occurs after the midpoint, but we're in the last segment; no need to Interpolate
                        End If
                    Else
                        ' scanIndexObserved occurs at the midpoint; no need to Interpolate
                    End If

                    If indexSegmentA <> indexSegmentB Then
                        ' Interpolate between the two segments
                        fractionFromSegmentB = CDbl(scanIndexObserved - segmentMidPointA) /
                                                  CDbl(segmentMidPointB - segmentMidPointA)
                        If fractionFromSegmentB < 0 Then
                            fractionFromSegmentB = 0
                        ElseIf fractionFromSegmentB > 1 Then
                            fractionFromSegmentB = 1
                        End If

                        fractionFromSegmentA = 1 - fractionFromSegmentB

                        ' Compute the weighted average values
                        Dim segmentA = noiseStatsSegments(indexSegmentA).BaselineNoiseStats
                        Dim segmentB = noiseStatsSegments(indexSegmentB).BaselineNoiseStats

                        baselineNoiseStats.NoiseLevel = segmentA.NoiseLevel * fractionFromSegmentA + segmentB.NoiseLevel * fractionFromSegmentB
                        baselineNoiseStats.NoiseStDev = segmentA.NoiseStDev * fractionFromSegmentA + segmentB.NoiseStDev * fractionFromSegmentB
                        baselineNoiseStats.PointsUsed = CInt(segmentA.PointsUsed * fractionFromSegmentA + segmentB.PointsUsed * fractionFromSegmentB)

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
    ''' Looks for the minimum positive value in dataListSorted() and replaces all values of 0 in dataListSorted() with minimumPositiveValue
    ''' </summary>
    ''' <param name="dataCount"></param>
    ''' <param name="dataListSorted"></param>
    ''' <returns>Minimum positive value</returns>
    ''' <remarks>Assumes data in dataListSorted() is sorted ascending</remarks>
    Private Function ReplaceSortedDataWithMinimumPositiveValue(dataCount As Integer, dataListSorted As IList(Of Double)) As Double

        Dim minimumPositiveValue As Double
        Dim indexFirstPositiveValue As Integer

        ' Find the minimum positive value in dataListSorted
        ' Since it's sorted, we can stop at the first non-zero value

        indexFirstPositiveValue = -1
        minimumPositiveValue = 0
        For i = 0 To dataCount - 1
            If dataListSorted(i) > 0 Then
                indexFirstPositiveValue = i
                minimumPositiveValue = dataListSorted(i)
                Exit For
            End If
        Next

        If minimumPositiveValue < 1 Then minimumPositiveValue = 1
        For i = indexFirstPositiveValue To 0 Step -1
            dataListSorted(i) = minimumPositiveValue
        Next

        Return minimumPositiveValue

    End Function

    ''' <summary>
    ''' Uses the means and sigma values to compute the t-test value between the two populations to determine if they are statistically different
    ''' </summary>
    ''' <param name="mean1"></param>
    ''' <param name="mean2"></param>
    ''' <param name="stDev1"></param>
    ''' <param name="stDev2"></param>
    ''' <param name="dataCount1"></param>
    ''' <param name="dataCount2"></param>
    ''' <param name="confidenceLevel"></param>
    ''' <param name="tCalculated"></param>
    ''' <returns>True if the two populations are statistically different, based on the given significance threshold</returns>
    Private Function TestSignificanceUsingTTest(
      mean1 As Double, mean2 As Double,
      stDev1 As Double, stDev2 As Double,
      dataCount1 As Integer, dataCount2 As Integer,
      confidenceLevel As eTTestConfidenceLevelConstants,
      <Out> ByRef tCalculated As Double) As Boolean

        ' To use the t-test you must use sample variance values, not population variance values
        ' Note: Variance_Sample = Sum((x-mean)^2) / (count-1)
        ' Note: Sigma = SquareRoot(Variance_Sample)

        Dim sPooled As Double
        Dim confidenceLevelIndex As Integer

        If dataCount1 + dataCount2 <= 2 Then
            ' Cannot compute the T-Test
            tCalculated = 0
            Return False
        Else

            sPooled = Math.Sqrt(((stDev1 ^ 2) * (dataCount1 - 1) + (stDev2 ^ 2) * (dataCount2 - 1)) / (dataCount1 + dataCount2 - 2))
            tCalculated = ((mean1 - mean2) / sPooled) * Math.Sqrt(dataCount1 * dataCount2 / (dataCount1 + dataCount2))

            confidenceLevelIndex = confidenceLevel
            If confidenceLevelIndex < 0 Then
                confidenceLevelIndex = 0
            ElseIf confidenceLevelIndex >= TTestConfidenceLevels.Length Then
                confidenceLevelIndex = TTestConfidenceLevels.Length - 1
            End If

            If tCalculated >= TTestConfidenceLevels(confidenceLevelIndex) Then
                ' Differences are significant
                Return True
            Else
                ' Differences are not significant
                Return False
            End If
        End If

    End Function

End Class
