Imports MASIC.clsMASIC
Imports MASICPeakFinder

Public Class clsScanTracking
    Inherits clsMasicEventNotifier

#Region "Constants and Enums"

    ' Absolute maximum number of ions that will be tracked for a mass spectrum
    Private Const MAX_ALLOWABLE_ION_COUNT As Integer = 50000

#End Region

#Region "Properties"
    Public ReadOnly Property ScanStats As List(Of clsScanStatsEntry)
#End Region

#Region "Classwide variables"
    Private ReadOnly mReporterIons As clsReporterIons
    Private ReadOnly mPeakFinder As MASICPeakFinder.clsMASICPeakFinder

    Private mSpectraFoundExceedingMaxIonCount As Integer = 0
    Private mMaxIonCountReported As Integer = 0

#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="reporterIons"></param>
    ''' <param name="peakFinder"></param>
    Public Sub New(reporterIons As clsReporterIons, peakFinder As MASICPeakFinder.clsMASICPeakFinder)
        mReporterIons = reporterIons
        mPeakFinder = peakFinder

        ScanStats = New List(Of clsScanStatsEntry)
    End Sub

    ''' <summary>
    ''' Check whether the scan number is within the range specified by sicOptions
    ''' </summary>
    ''' <param name="scanNumber"></param>
    ''' <param name="sicOptions"></param>
    ''' <returns>True if filtering is disabled, or if scanNumber is within the limits</returns>
    Public Function CheckScanInRange(
      scanNumber As Integer,
      sicOptions As clsSICOptions) As Boolean

        If sicOptions.ScanRangeStart >= 0 AndAlso sicOptions.ScanRangeEnd > sicOptions.ScanRangeStart Then
            If scanNumber < sicOptions.ScanRangeStart OrElse scanNumber > sicOptions.ScanRangeEnd Then
                Return False
            End If
        End If

        Return True

    End Function


    ''' <summary>
    ''' Check whether the scan number and elution time are within the ranges specified by sicOptions
    ''' </summary>
    ''' <param name="scanNumber"></param>
    ''' <param name="elutionTime"></param>
    ''' <param name="sicOptions"></param>
    ''' <returns>True if filtering is disabled, or if scanNumber and elutionTime are within the limits</returns>
    Public Function CheckScanInRange(
      scanNumber As Integer,
      elutionTime As Double,
      sicOptions As clsSICOptions) As Boolean

        If Not CheckScanInRange(scanNumber, sicOptions) Then
            Return False
        End If

        If sicOptions.RTRangeStart >= 0 AndAlso sicOptions.RTRangeEnd > sicOptions.RTRangeStart Then
            If elutionTime < sicOptions.RTRangeStart OrElse elutionTime > sicOptions.RTRangeEnd Then
                Return False
            End If
        End If

        Return True

    End Function

    Private Sub CompressSpectraData(
      msSpectrum As clsMSSpectrum,
      msDataResolution As Double,
      mzIgnoreRangeStart As Double,
      mzIgnoreRangeEnd As Double)

        ' First, look for blocks of data points that consecutively have an intensity value of 0
        ' For each block of data found, reduce the data to only retain the first data point and last data point in the block
        '
        ' Next, look for data points in msSpectrum that are within msDataResolution units of one another (m/z units)
        ' If found, combine into just one data point, keeping the largest intensity and the m/z value corresponding to the largest intensity

        If msSpectrum.IonCount <= 1 Then
            Return
        End If

        ' Look for blocks of data points that all have an intensity value of 0
        Dim targetIndex = 0
        Dim index = 0

        Do While index < msSpectrum.IonCount
            If msSpectrum.IonsIntensity(index) < Single.Epsilon Then
                Dim countCombined = 0
                For comparisonIndex = index + 1 To msSpectrum.IonCount - 1
                    If msSpectrum.IonsIntensity(comparisonIndex) < Single.Epsilon Then
                        countCombined += 1
                    Else
                        Exit For
                    End If
                Next

                If countCombined > 1 Then
                    ' Only keep the first and last data point in the block

                    msSpectrum.IonsMZ(targetIndex) = msSpectrum.IonsMZ(index)
                    msSpectrum.IonsIntensity(targetIndex) = msSpectrum.IonsIntensity(index)

                    targetIndex += 1
                    msSpectrum.IonsMZ(targetIndex) = msSpectrum.IonsMZ(index + countCombined)
                    msSpectrum.IonsIntensity(targetIndex) = msSpectrum.IonsIntensity(index + countCombined)

                    index += countCombined
                Else
                    ' Keep this data point since a single zero
                    If targetIndex <> index Then
                        msSpectrum.IonsMZ(targetIndex) = msSpectrum.IonsMZ(index)
                        msSpectrum.IonsIntensity(targetIndex) = msSpectrum.IonsIntensity(index)
                    End If
                End If
            Else
                ' Note: targetIndex will be the same as index until the first time that data is combined (countCombined > 0)
                ' After that, targetIndex will always be less than index and we will thus always need to copy data
                If targetIndex <> index Then
                    msSpectrum.IonsMZ(targetIndex) = msSpectrum.IonsMZ(index)
                    msSpectrum.IonsIntensity(targetIndex) = msSpectrum.IonsIntensity(index)
                End If
            End If

            index += 1
            targetIndex += 1

        Loop

        ' Update .IonCount with the new data count
        msSpectrum.ShrinkArrays(targetIndex)

        ' Step through the data, consolidating data within msDataResolution
        ' Note that we're copying in place rather than making a new, duplicate array
        ' If the m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd, then we will not compress the data

        targetIndex = 0
        index = 0

        Do While index < msSpectrum.IonCount
            Dim countCombined = 0
            Dim bestMz = msSpectrum.IonsMZ(index)

            ' Only combine data if the first data point has a positive intensity value
            If msSpectrum.IonsIntensity(index) > 0 Then

                Dim pointInIgnoreRange = clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ(index), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                If Not pointInIgnoreRange Then
                    For comparisonIndex = index + 1 To msSpectrum.IonCount - 1
                        If clsUtilities.CheckPointInMZIgnoreRange(msSpectrum.IonsMZ(comparisonIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd) Then
                            ' Reached the ignore range; do not allow to be combined with the current data point
                            Exit For
                        End If

                        If msSpectrum.IonsMZ(comparisonIndex) - msSpectrum.IonsMZ(index) < msDataResolution Then
                            If msSpectrum.IonsIntensity(comparisonIndex) > msSpectrum.IonsIntensity(index) Then
                                msSpectrum.IonsIntensity(index) = msSpectrum.IonsIntensity(comparisonIndex)
                                bestMz = msSpectrum.IonsMZ(comparisonIndex)
                            End If
                            countCombined += 1
                        Else
                            Exit For
                        End If
                    Next
                End If

            End If

            ' Note: targetIndex will be the same as index until the first time that data is combined (countCombined > 0)
            ' After that, targetIndex will always be less than index and we will thus always need to copy data
            If targetIndex <> index OrElse countCombined > 0 Then
                msSpectrum.IonsMZ(targetIndex) = bestMz
                msSpectrum.IonsIntensity(targetIndex) = msSpectrum.IonsIntensity(index)

                index += countCombined
            End If

            index += 1
            targetIndex += 1
        Loop

        ' Update .IonCount with the new data count
        msSpectrum.ShrinkArrays(targetIndex)

    End Sub

    Private Sub ComputeNoiseLevelForMassSpectrum(
      scanInfo As clsScanInfo,
      msSpectrum As clsMSSpectrum,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions)

        Const IGNORE_NON_POSITIVE_DATA = True

        scanInfo.BaselineNoiseStats = clsMASICPeakFinder.InitializeBaselineNoiseStats(0, noiseThresholdOptions.BaselineNoiseMode)

        If noiseThresholdOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold Then
            scanInfo.BaselineNoiseStats.NoiseLevel = noiseThresholdOptions.BaselineNoiseLevelAbsolute
            scanInfo.BaselineNoiseStats.PointsUsed = 1
        Else
            If msSpectrum.IonCount > 0 Then
                Dim newBaselineNoiseStats As clsBaselineNoiseStats = Nothing

                mPeakFinder.ComputeTrimmedNoiseLevel(
                    msSpectrum.IonsIntensity, 0, msSpectrum.IonCount - 1,
                    noiseThresholdOptions, IGNORE_NON_POSITIVE_DATA,
                    newBaselineNoiseStats)

                scanInfo.BaselineNoiseStats = newBaselineNoiseStats
            End If
        End If

    End Sub

    Public Function ProcessAndStoreSpectrum(
      scanInfo As clsScanInfo,
      dataImportUtilities As DataInput.clsDataImport,
      spectraCache As clsSpectraCache,
      msSpectrum As clsMSSpectrum,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
      discardLowIntensityData As Boolean,
      compressData As Boolean,
      msDataResolution As Double,
      keepRawSpectrum As Boolean) As Boolean

        Dim lastKnownLocation = "Start"

        Try

            ' Determine the noise threshold intensity for this spectrum
            ' Stored in scanInfo.BaselineNoiseStats
            lastKnownLocation = "Call ComputeNoiseLevelForMassSpectrum"
            ComputeNoiseLevelForMassSpectrum(scanInfo, msSpectrum, noiseThresholdOptions)

            If Not keepRawSpectrum Then
                Return True
            End If

            ' Discard low intensity data, but not for MRM scans
            If discardLowIntensityData AndAlso scanInfo.MRMScanType = ThermoRawFileReader.MRMScanTypeConstants.NotMRM Then
                ' Discard data below the noise level or below the minimum S/N level
                ' If we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                lastKnownLocation = "Call DiscardDataBelowNoiseThreshold"
                dataImportUtilities.DiscardDataBelowNoiseThreshold(msSpectrum,
                                                                   scanInfo.BaselineNoiseStats.NoiseLevel,
                                                                   mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                                   mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                                                                   noiseThresholdOptions)

                scanInfo.IonCount = msSpectrum.IonCount
            End If

            If compressData Then
                lastKnownLocation = "Call CompressSpectraData"
                ' Again, if we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                CompressSpectraData(msSpectrum, msDataResolution,
                                    mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                    mReporterIons.MZIntensityFilterIgnoreRangeEnd)
            End If

            If msSpectrum.IonCount > MAX_ALLOWABLE_ION_COUNT Then
                ' Do not keep more than 50,000 ions
                lastKnownLocation = "Call DiscardDataToLimitIonCount"
                mSpectraFoundExceedingMaxIonCount += 1

                ' Display a message at the console the first 10 times we encounter spectra with over MAX_ALLOWABLE_ION_COUNT ions
                ' In addition, display a new message every time a new max value is encountered
                If mSpectraFoundExceedingMaxIonCount <= 10 OrElse msSpectrum.IonCount > mMaxIonCountReported Then
                    Console.WriteLine()
                    Console.WriteLine(
                        "Note: Scan " & scanInfo.ScanNumber & " has " & msSpectrum.IonCount & " ions; " &
                        "will only retain " & MAX_ALLOWABLE_ION_COUNT & " (trimmed " &
                        mSpectraFoundExceedingMaxIonCount.ToString() & " spectra)")

                    mMaxIonCountReported = msSpectrum.IonCount
                End If

                dataImportUtilities.DiscardDataToLimitIonCount(msSpectrum,
                                                               mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                               mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                                                               MAX_ALLOWABLE_ION_COUNT)

                scanInfo.IonCount = msSpectrum.IonCount
            End If

            lastKnownLocation = "Call AddSpectrumToPool"
            Dim success = spectraCache.AddSpectrumToPool(msSpectrum, scanInfo.ScanNumber)

            Return success

        Catch ex As Exception
            ReportError("Error in ProcessAndStoreSpectrum (LastKnownLocation: " & lastKnownLocation & ")", ex, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

    End Function

End Class
