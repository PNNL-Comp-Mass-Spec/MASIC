Imports MASIC.clsMASIC

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
      intScanNumber As Integer,
      dblRetentionTime As Double,
      sicOptions As clsSICOptions) As Boolean

        ' Returns True if filtering is disabled, or if the ScanNumber is between the scan limits 
        ' and/or the retention time is between the time limits

        Dim blnInRange As Boolean

        blnInRange = True

        With sicOptions
            If .ScanRangeStart >= 0 AndAlso .ScanRangeEnd > .ScanRangeStart Then
                If intScanNumber < .ScanRangeStart OrElse intScanNumber > .ScanRangeEnd Then
                    blnInRange = False
                End If
            End If

            If blnInRange Then
                If .RTRangeStart >= 0 AndAlso .RTRangeEnd > .RTRangeStart Then
                    If dblRetentionTime < .RTRangeStart OrElse dblRetentionTime > .RTRangeEnd Then
                        blnInRange = False
                    End If
                End If
            End If
        End With

        Return blnInRange

    End Function

    Private Sub CompressSpectraData(
      objMSSpectrum As clsMSSpectrum,
      dblMSDataResolution As Double,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double)

        ' First, look for blocks of data points that consecutively have an intensity value of 0
        ' For each block of data found, reduce the data to only retain the first data point and last data point in the block
        '
        ' Next, look for data points in objMSSpectrum that are within dblMSDataResolution units of one another (m/z units)
        ' If found, combine into just one data point, keeping the largest intensity and the m/z value corresponding to the largest intensity

        Dim intIndex As Integer
        Dim intComparisonIndex As Integer
        Dim intTargetIndex As Integer

        Dim intCountCombined As Integer
        Dim dblBestMZ As Double

        Dim blnPointInIgnoreRange As Boolean

        With objMSSpectrum
            If .IonCount > 1 Then

                ' Look for blocks of data points that all have an intensity value of 0
                intTargetIndex = 0
                intIndex = 0

                Do While intIndex < .IonCount
                    If .IonsIntensity(intIndex) < Single.Epsilon Then
                        intCountCombined = 0
                        For intComparisonIndex = intIndex + 1 To .IonCount - 1
                            If .IonsIntensity(intComparisonIndex) < Single.Epsilon Then
                                intCountCombined += 1
                            Else
                                Exit For
                            End If
                        Next

                        If intCountCombined > 1 Then
                            ' Only keep the first and last data point in the block

                            .IonsMZ(intTargetIndex) = .IonsMZ(intIndex)
                            .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)

                            intTargetIndex += 1
                            .IonsMZ(intTargetIndex) = .IonsMZ(intIndex + intCountCombined)
                            .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex + intCountCombined)

                            intIndex += intCountCombined
                        Else
                            ' Keep this data point since a single zero
                            If intTargetIndex <> intIndex Then
                                .IonsMZ(intTargetIndex) = .IonsMZ(intIndex)
                                .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)
                            End If
                        End If
                    Else
                        ' Note: intTargetIndex will be the same as intIndex until the first time that data is combined (intCountCombined > 0)
                        ' After that, intTargetIndex will always be less than intIndex and we will thus always need to copy data
                        If intTargetIndex <> intIndex Then
                            .IonsMZ(intTargetIndex) = .IonsMZ(intIndex)
                            .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)
                        End If
                    End If

                    intIndex += 1
                    intTargetIndex += 1

                Loop

                ' Update .IonCount with the new data count
                .IonCount = intTargetIndex

                ' Step through the data, consolidating data within dblMSDataResolution
                ' Note that we're copying in place rather than making a new, duplicate array
                ' If the m/z value is between dblMZIgnoreRangeStart and dblMZIgnoreRangeEnd, then we will not compress the data

                intTargetIndex = 0
                intIndex = 0

                Do While intIndex < .IonCount
                    intCountCombined = 0
                    dblBestMZ = .IonsMZ(intIndex)

                    ' Only combine data if the first data point has a positive intensity value
                    If .IonsIntensity(intIndex) > 0 Then

                        blnPointInIgnoreRange = clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(intIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd)

                        If Not blnPointInIgnoreRange Then
                            For intComparisonIndex = intIndex + 1 To .IonCount - 1
                                If clsUtilities.CheckPointInMZIgnoreRange(.IonsMZ(intComparisonIndex), dblMZIgnoreRangeStart, dblMZIgnoreRangeEnd) Then
                                    ' Reached the ignore range; do not allow to be combined with the current data point
                                    Exit For
                                End If

                                If .IonsMZ(intComparisonIndex) - .IonsMZ(intIndex) < dblMSDataResolution Then
                                    If .IonsIntensity(intComparisonIndex) > .IonsIntensity(intIndex) Then
                                        .IonsIntensity(intIndex) = .IonsIntensity(intComparisonIndex)
                                        dblBestMZ = .IonsMZ(intComparisonIndex)
                                    End If
                                    intCountCombined += 1
                                Else
                                    Exit For
                                End If
                            Next
                        End If

                    End If

                    ' Note: intTargetIndex will be the same as intIndex until the first time that data is combined (intCountCombined > 0)
                    ' After that, intTargetIndex will always be less than intIndex and we will thus always need to copy data
                    If intTargetIndex <> intIndex OrElse intCountCombined > 0 Then
                        .IonsMZ(intTargetIndex) = dblBestMZ
                        .IonsIntensity(intTargetIndex) = .IonsIntensity(intIndex)

                        intIndex += intCountCombined
                    End If

                    intIndex += 1
                    intTargetIndex += 1
                Loop

                ' Update .IonCount with the new data count
                .IonCount = intTargetIndex
            End If
        End With

    End Sub

    Private Sub ComputeNoiseLevelForMassSpectrum(
      scanInfo As clsScanInfo,
      objMSSpectrum As clsMSSpectrum,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions)

        Const IGNORE_NON_POSITIVE_DATA = True

        MASICPeakFinder.clsMASICPeakFinder.InitializeBaselineNoiseStats(scanInfo.BaselineNoiseStats, 0, noiseThresholdOptions.BaselineNoiseMode)

        If noiseThresholdOptions.BaselineNoiseMode = MASICPeakFinder.clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold Then
            Dim noiseStats = scanInfo.BaselineNoiseStats

            noiseStats.NoiseLevel = noiseThresholdOptions.BaselineNoiseLevelAbsolute
            noiseStats.PointsUsed = 1

            scanInfo.BaselineNoiseStats = noiseStats
        Else
            If objMSSpectrum.IonCount > 0 Then
                mPeakFinder.ComputeTrimmedNoiseLevel(objMSSpectrum.IonsIntensity, 0, objMSSpectrum.IonCount - 1, noiseThresholdOptions, IGNORE_NON_POSITIVE_DATA, scanInfo.BaselineNoiseStats)
            End If
        End If

    End Sub

    Public Function ProcessAndStoreSpectrum(
      scanInfo As clsScanInfo,
      dataImportUtilities As DataInput.clsDataImport,
      objSpectraCache As clsSpectraCache,
      objMSSpectrum As clsMSSpectrum,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
      blnDiscardLowIntensityData As Boolean,
      blnCompressSpectraData As Boolean,
      dblMSDataResolution As Double,
      blnKeepRawSpectrum As Boolean) As Boolean


        Dim blnSuccess As Boolean
        Dim intMaxAllowableIonCount As Integer

        Dim strLastKnownLocation = "Start"

        Try
            blnSuccess = False

            ' Determine the noise threshold intensity for this spectrum
            strLastKnownLocation = "Call ComputeNoiseLevelForMassSpectrum"
            ComputeNoiseLevelForMassSpectrum(scanInfo, objMSSpectrum, noiseThresholdOptions)

            If blnKeepRawSpectrum Then

                ' Do not discard low intensity data for MRM scans
                If scanInfo.MRMScanType = ThermoRawFileReader.MRMScanTypeConstants.NotMRM Then
                    If blnDiscardLowIntensityData Then
                        ' Discard data below the noise level or below the minimum S/N level
                        ' If we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                        strLastKnownLocation = "Call DiscardDataBelowNoiseThreshold"
                        dataImportUtilities.DiscardDataBelowNoiseThreshold(objMSSpectrum,
                                                       scanInfo.BaselineNoiseStats.NoiseLevel,
                                                       mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                       mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                                                       noiseThresholdOptions)

                        scanInfo.IonCount = objMSSpectrum.IonCount
                    End If
                End If

                If blnCompressSpectraData Then
                    strLastKnownLocation = "Call CompressSpectraData"
                    ' Again, if we are searching for Reporter ions, then it is important to not discard any of the ions in the region of the reporter ion m/z values
                    CompressSpectraData(objMSSpectrum,
                                        dblMSDataResolution,
                                        mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                        mReporterIons.MZIntensityFilterIgnoreRangeEnd)
                End If

                intMaxAllowableIonCount = MAX_ALLOWABLE_ION_COUNT
                If objMSSpectrum.IonCount > intMaxAllowableIonCount Then
                    ' Do not keep more than 50,000 ions
                    strLastKnownLocation = "Call DiscardDataToLimitIonCount"
                    mSpectraFoundExceedingMaxIonCount += 1

                    ' Display a message at the console the first 10 times we encounter spectra with over intMaxAllowableIonCount ions
                    ' In addition, display a new message every time a new max value is encountered
                    If mSpectraFoundExceedingMaxIonCount <= 10 OrElse objMSSpectrum.IonCount > mMaxIonCountReported Then
                        Console.WriteLine()
                        Console.WriteLine("Note: Scan " & scanInfo.ScanNumber & " has " & objMSSpectrum.IonCount & " ions; " &
                                          "will only retain " & intMaxAllowableIonCount &
                                          " (trimmed " & mSpectraFoundExceedingMaxIonCount.ToString() & " spectra)")

                        mMaxIonCountReported = objMSSpectrum.IonCount
                    End If

                    dataImportUtilities.DiscardDataToLimitIonCount(
                        objMSSpectrum,
                        mReporterIons.MZIntensityFilterIgnoreRangeStart,
                        mReporterIons.MZIntensityFilterIgnoreRangeEnd,
                        intMaxAllowableIonCount)

                    scanInfo.IonCount = objMSSpectrum.IonCount
                End If

                strLastKnownLocation = "Call AddSpectrumToPool"
                blnSuccess = objSpectraCache.AddSpectrumToPool(objMSSpectrum, scanInfo.ScanNumber)
            Else
                blnSuccess = True
            End If

        Catch ex As Exception
            ReportError("ProcessAndStoreSpectrum", "Error in ProcessAndStoreSpectrum (LastKnownLocation: " & strLastKnownLocation & ")", ex, True, True, eMasicErrorCodes.InputFileDataReadError)
        End Try

        Return blnSuccess

    End Function

End Class
