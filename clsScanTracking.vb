Imports MASIC.clsMASIC
Imports MASICPeakFinder

Public Class clsScanTracking
    Inherits clsEventNotifier

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
    ''' <param name="peakfinder"></param>
    Public Sub New(reporterIons As clsReporterIons, peakfinder As MASICPeakFinder.clsMASICPeakFinder)
        mReporterIons = reporterIons
        mPeakFinder = peakfinder

        ScanStats = New List(Of clsScanStatsEntry)
    End Sub

    Public Function CheckScanInRange(
      scanNumber As Integer,
      retentionTime As Double,
      sicOptions As clsSICOptions) As Boolean

        ' Returns True if filtering is disabled, or if the ScanNumber is between the scan limits
        ' and/or the retention time is between the time limits

        Dim inRange = True

        With sicOptions
            If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                If scanNumber < .ScanRangeStart OrElse scanNumber > .ScanRangeEnd Then
                    inRange = False
                End If
            End If

            If inRange Then
                If .RTRangeStart >= 0 AndAlso .RTRangeEnd > .RTRangeStart Then
                    If retentionTime < .RTRangeStart OrElse retentionTime > .RTRangeEnd Then
                        inRange = False
                    End If
                End If
            End If
        End With

        Return inRange

    End Function

    Private Sub CompressSpectraData(
      objMSSpectrum As clsMSSpectrum,
      msDataResolution As Double,
      mzIgnoreRangeStart As Double,
      mzIgnoreRangeEnd As Double)

        ' First, look for blocks of data points that consecutively have an intensity value of 0
        ' For each block of data found, reduce the data to only retain the first data point and last data point in the block
        '
        ' Next, look for data points in objMSSpectrum that are within msDataResolution units of one another (m/z units)
        ' If found, combine into just one data point, keeping the largest intensity and the m/z value corresponding to the largest intensity

        If objMSSpectrum.IonCount <= 1 Then
            Return
        End If

        ' Look for blocks of data points that all have an intensity value of 0
        Dim targetIndex = 0
        Dim index = 0

        Do While index < objMSSpectrum.IonCount
            If objMSSpectrum.IonsIntensity(index) < Single.Epsilon Then
                Dim countCombined = 0
                For intComparisonIndex = index + 1 To objMSSpectrum.IonCount - 1
                    If objMSSpectrum.IonsIntensity(intComparisonIndex) < Single.Epsilon Then
                        countCombined += 1
                    Else
                        Exit For
                    End If
                Next

                If countCombined > 1 Then
                    ' Only keep the first and last data point in the block

                    objMSSpectrum.IonsMZ(targetIndex) = objMSSpectrum.IonsMZ(index)
                    objMSSpectrum.IonsIntensity(targetIndex) = objMSSpectrum.IonsIntensity(index)

                    targetIndex += 1
                    objMSSpectrum.IonsMZ(targetIndex) = objMSSpectrum.IonsMZ(index + countCombined)
                    objMSSpectrum.IonsIntensity(targetIndex) = objMSSpectrum.IonsIntensity(index + countCombined)

                    index += countCombined
                Else
                    ' Keep this data point since a single zero
                    If targetIndex <> index Then
                        objMSSpectrum.IonsMZ(targetIndex) = objMSSpectrum.IonsMZ(index)
                        objMSSpectrum.IonsIntensity(targetIndex) = objMSSpectrum.IonsIntensity(index)
                    End If
                End If
            Else
                ' Note: targetIndex will be the same as index until the first time that data is combined (countCombined > 0)
                ' After that, targetIndex will always be less than index and we will thus always need to copy data
                If targetIndex <> index Then
                    objMSSpectrum.IonsMZ(targetIndex) = objMSSpectrum.IonsMZ(index)
                    objMSSpectrum.IonsIntensity(targetIndex) = objMSSpectrum.IonsIntensity(index)
                End If
            End If

            index += 1
            targetIndex += 1

        Loop

        ' Update .IonCount with the new data count
        objMSSpectrum.IonCount = targetIndex

        ' Step through the data, consolidating data within msDataResolution
        ' Note that we're copying in place rather than making a new, duplicate array
        ' If the m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd, then we will not compress the data

        targetIndex = 0
        index = 0

        Do While index < objMSSpectrum.IonCount
            Dim countCombined = 0
            Dim bestMz = objMSSpectrum.IonsMZ(index)

            ' Only combine data if the first data point has a positive intensity value
            If objMSSpectrum.IonsIntensity(index) > 0 Then

                Dim pointInIgnoreRange = clsUtilities.CheckPointInMZIgnoreRange(objMSSpectrum.IonsMZ(index), mzIgnoreRangeStart, mzIgnoreRangeEnd)

                If Not pointInIgnoreRange Then
                    For intComparisonIndex = index + 1 To objMSSpectrum.IonCount - 1
                        If clsUtilities.CheckPointInMZIgnoreRange(objMSSpectrum.IonsMZ(intComparisonIndex), mzIgnoreRangeStart, mzIgnoreRangeEnd) Then
                            ' Reached the ignore range; do not allow to be combined with the current data point
                            Exit For
                        End If

                        If objMSSpectrum.IonsMZ(intComparisonIndex) - objMSSpectrum.IonsMZ(index) < msDataResolution Then
                            If objMSSpectrum.IonsIntensity(intComparisonIndex) > objMSSpectrum.IonsIntensity(index) Then
                                objMSSpectrum.IonsIntensity(index) = objMSSpectrum.IonsIntensity(intComparisonIndex)
                                bestMz = objMSSpectrum.IonsMZ(intComparisonIndex)
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
                objMSSpectrum.IonsMZ(targetIndex) = bestMz
                objMSSpectrum.IonsIntensity(targetIndex) = objMSSpectrum.IonsIntensity(index)

                index += countCombined
            End If

            index += 1
            targetIndex += 1
        Loop

        ' Update .IonCount with the new data count
        objMSSpectrum.IonCount = targetIndex

    End Sub

    Private Sub ComputeNoiseLevelForMassSpectrum(
      scanInfo As clsScanInfo,
      objMSSpectrum As clsMSSpectrum,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions)

        Const IGNORE_NON_POSITIVE_DATA = True

        scanInfo.BaselineNoiseStats = clsMASICPeakFinder.InitializeBaselineNoiseStats(0, noiseThresholdOptions.BaselineNoiseMode)

        If noiseThresholdOptions.BaselineNoiseMode = clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold Then
            scanInfo.BaselineNoiseStats.NoiseLevel = noiseThresholdOptions.BaselineNoiseLevelAbsolute
            scanInfo.BaselineNoiseStats.PointsUsed = 1
        Else
            If objMSSpectrum.IonCount > 0 Then
                Dim newBaselineNoiseStats As clsBaselineNoiseStats = Nothing

                mPeakFinder.ComputeTrimmedNoiseLevel(
                    objMSSpectrum.IonsIntensity, 0, objMSSpectrum.IonCount - 1,
                    noiseThresholdOptions, IGNORE_NON_POSITIVE_DATA,
                    newBaselineNoiseStats)

                scanInfo.BaselineNoiseStats = newBaselineNoiseStats
            End If
        End If

    End Sub

    Public Function ProcessAndStoreSpectrum(
      scanInfo As clsScanInfo,
      dataImportUtilities As DataInput.clsDataImport,
      objSpectraCache As clsSpectraCache,
      objMSSpectrum As clsMSSpectrum,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
      discardLowIntensityData As Boolean,
      compressData As Boolean,
      msDataResolution As Double,
      keepRawSpectrum As Boolean) As Boolean

        Dim lastKnownLocation = "Start"

        Try

            ' Determine the noise threshold intensity for this spectrum
            lastKnownLocation = "Call ComputeNoiseLevelForMassSpectrum"
            ComputeNoiseLevelForMassSpectrum(scanInfo, objMSSpectrum, noiseThresholdOptions)

            If Not keepRawSpectrum Then
                Return True
            End If

            ' Discard low intensity data, but not for MRM scans
            If discardLowIntensityData AndAlso scanInfo.MRMScanType = ThermoRawFileReader.MRMScanTypeConstants.NotMRM Then
                ' Discard data below the noise level or below the minimum S/N level
                ' If we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                lastKnownLocation = "Call DiscardDataBelowNoiseThreshold"
                dataImportUtilities.DiscardDataBelowNoiseThreshold(objMSSpectrum,
                                                                   scanInfo.BaselineNoiseStats.NoiseLevel,
                                                                   mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                                   mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                                                                   noiseThresholdOptions)

                scanInfo.IonCount = objMSSpectrum.IonCount
            End If

            If compressData Then
                lastKnownLocation = "Call CompressSpectraData"
                ' Again, if we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                CompressSpectraData(objMSSpectrum, msDataResolution,
                                    mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                    mReporterIons.MZIntensityFilterIgnoreRangeEnd)
            End If

            If objMSSpectrum.IonCount > MAX_ALLOWABLE_ION_COUNT Then
                ' Do not keep more than 50,000 ions
                lastKnownLocation = "Call DiscardDataToLimitIonCount"
                mSpectraFoundExceedingMaxIonCount += 1

                ' Display a message at the console the first 10 times we encounter spectra with over MAX_ALLOWABLE_ION_COUNT ions
                ' In addition, display a new message every time a new max value is encountered
                If mSpectraFoundExceedingMaxIonCount <= 10 OrElse objMSSpectrum.IonCount > mMaxIonCountReported Then
                    Console.WriteLine()
                    Console.WriteLine(
                        "Note: Scan " & scanInfo.ScanNumber & " has " & objMSSpectrum.IonCount & " ions; " &
                        "will only retain " & MAX_ALLOWABLE_ION_COUNT & " (trimmed " &
                        mSpectraFoundExceedingMaxIonCount.ToString() & " spectra)")

                    mMaxIonCountReported = objMSSpectrum.IonCount
                End If

                dataImportUtilities.DiscardDataToLimitIonCount(objMSSpectrum,
                                                               mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                               mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                                                               MAX_ALLOWABLE_ION_COUNT)

                scanInfo.IonCount = objMSSpectrum.IonCount
            End If

            lastKnownLocation = "Call AddSpectrumToPool"
            Dim success = objSpectraCache.AddSpectrumToPool(objMSSpectrum, scanInfo.ScanNumber)

            Return success

        Catch ex As Exception
            ReportError("ProcessAndStoreSpectrum", "Error in ProcessAndStoreSpectrum (LastKnownLocation: " & lastKnownLocation & ")", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
            Return False
        End Try

    End Function

End Class
