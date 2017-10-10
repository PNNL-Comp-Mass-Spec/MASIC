Imports PRISM
Imports ThermoRawFileReader

Public Class clsUnused
    Inherits clsMasicEventNotifier

    ''Private Sub FindMinimumPotentialPeakAreaInRegion(scanList As clsScanList, intParentIonIndexStart As Integer, intParentIonIndexEnd As Integer, ByRef udtSICPotentialAreaStatsForRegion As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType)
    ''    ' This function finds the minimum potential peak area in the parent ions between
    ''    '  intParentIonIndexStart and intParentIonIndexEnd
    ''    ' However, the summed intensity is not used if the number of points >= .SICNoiseThresholdIntensity is less than MASICPeakFinder.clsMASICPeakFinder.MINIMUM_PEAK_WIDTH

    ''    Dim intParentIonIndex As Integer
    ''    Dim intIndex As Integer

    ''    With udtSICPotentialAreaStatsForRegion
    ''        .MinimumPotentialPeakArea = Double.MaxValue
    ''        .PeakCountBasisForMinimumPotentialArea = 0
    ''    End With

    ''    For intParentIonIndex = intParentIonIndexStart To intParentIonIndexEnd
    ''        With scanList.ParentIons(intParentIonIndex).SICStats.SICPotentialAreaStatsForPeak

    ''            If .MinimumPotentialPeakArea > 0 AndAlso .PeakCountBasisForMinimumPotentialArea >= MASICPeakFinder.clsMASICPeakFinder.MINIMUM_PEAK_WIDTH Then
    ''                If .PeakCountBasisForMinimumPotentialArea > udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
    ''                    ' The non valid peak count value is larger than the one associated with the current
    ''                    '  minimum potential peak area; update the minimum peak area to dblPotentialPeakArea
    ''                    udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
    ''                    udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
    ''                Else
    ''                    If .MinimumPotentialPeakArea < udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea AndAlso
    ''                       .PeakCountBasisForMinimumPotentialArea >= udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea Then
    ''                        udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = .MinimumPotentialPeakArea
    ''                        udtSICPotentialAreaStatsForRegion.PeakCountBasisForMinimumPotentialArea = .PeakCountBasisForMinimumPotentialArea
    ''                    End If
    ''                End If
    ''            End If

    ''        End With
    ''    Next

    ''    If udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = Double.MaxValue Then
    ''        udtSICPotentialAreaStatsForRegion.MinimumPotentialPeakArea = 1
    ''    End If

    ''End Sub

    ''Private Function FindSICPeakAndAreaForParentIon(scanList As clsScanList, intParentIonIndex As Integer, ByRef udtSICDetails As clsSICStatsDetails, ByRef udtSmoothedYDataSubset As MASICPeakFinder.clsMASICPeakFinder.udtSmoothedYDataSubsetType, sicOptions As clsSICOptions) As Boolean

    ''    Const RECOMPUTE_NOISE_LEVEL As Boolean = True

    ''    Dim intSurveyScanIndex As Integer
    ''    Dim intDataIndex As Integer
    ''    Dim intIndexPointer As Integer
    ''    Dim intParentIonIndexStart As Integer

    ''    Dim intScanIndexObserved As Integer
    ''    Dim intScanDelta As Integer

    ''    Dim intFragScanNumber As Integer

    ''    Dim intAreaDataCount As Integer
    ''    Dim intAreaDataBaseIndex As Integer

    ''    Dim sngFWHMScanStart, sngFWHMScanEnd As Single

    ''    Dim udtSICPotentialAreaStatsForRegion As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
    ''    Dim sngMaxIntensityValueRawData As Single

    ''    Dim intSICScanNumbers() As Integer
    ''    Dim sngSICIntensities() As Single

    ''    Dim blnCustomSICPeak As Boolean
    ''    Dim blnSuccess As Boolean

    ''    Try

    ''        ' Determine the minimum potential peak area in the last 500 scans
    ''        intParentIonIndexStart = intParentIonIndex - 500
    ''        If intParentIonIndexStart < 0 Then intParentIonIndexStart = 0
    ''        FindMinimumPotentialPeakAreaInRegion(scanList, intParentIonIndexStart, intParentIonIndex, udtSICPotentialAreaStatsForRegion)

    ''        With scanList
    ''            With .ParentIons(intParentIonIndex)
    ''                intScanIndexObserved = .SurveyScanIndex
    ''                If intScanIndexObserved < 0 Then intScanIndexObserved = 0
    ''                blnCustomSICPeak = .CustomSICPeak
    ''            End With

    ''            If udtSICDetails.SICData Is Nothing OrElse udtSICDetails.SICDataCount = 0 Then
    ''                ' Either .SICData is nothing or no SIC data exists
    ''                ' Cannot find peaks for this parent ion
    ''                With .ParentIons(intParentIonIndex).SICStats
    ''                    With .Peak
    ''                        .IndexObserved = 0
    ''                        .IndexBaseLeft = .IndexObserved
    ''                        .IndexBaseRight = .IndexObserved
    ''                        .IndexMax = .IndexObserved
    ''                    End With
    ''                End With
    ''            Else
    ''                With .ParentIons(intParentIonIndex).SICStats
    ''                    ' Record the index (of data in .SICData) that the parent ion mass was first observed

    ''                    .Peak.ScanTypeForPeakIndices = eScanTypeConstants.SurveyScan

    ''                    ' Search for intScanIndexObserved in udtSICDetails.SICScanIndices()
    ''                    .Peak.IndexObserved = -1
    ''                    For intSurveyScanIndex = 0 To udtSICDetails.SICDataCount - 1
    ''                        If udtSICDetails.SICScanIndices(intSurveyScanIndex) = intScanIndexObserved Then
    ''                            .Peak.IndexObserved = intSurveyScanIndex
    ''                            Exit For
    ''                        End If
    ''                    Next

    ''                    If .Peak.IndexObserved = -1 Then
    ''                        ' Match wasn't found; this is unexpected
    ''                        ReportError("Programming error: survey scan index not found", eMasicErrorCodes.FindSICPeaksError)
    ''                        .Peak.IndexObserved = 0
    ''                    End If

    ''                    ' Populate intSICScanNumbers() with the scan numbers that the SICData corresponds to
    ''                    ' At the same time, populate udtSICStats.SICDataScanIntervals with the scan intervals between each of the data points

    ''                    If udtSICDetails.SICDataCount > udtSICDetails.SICDataScanIntervals.Length Then
    ''                        ReDim udtSICDetails.SICDataScanIntervals(udtSICDetails.SICDataCount - 1)
    ''                    End If

    ''                    ReDim intSICScanNumbers(udtSICDetails.SICDataCount - 1)
    ''                    For intSurveyScanIndex = 0 To udtSICDetails.SICDataCount - 1
    ''                        intSICScanNumbers(intSurveyScanIndex) = scanList.SurveyScans(udtSICDetails.SICScanIndices(intSurveyScanIndex)).ScanNumber
    ''                        If intSurveyScanIndex > 0 Then
    ''                            intScanDelta = intSICScanNumbers(intSurveyScanIndex) - intSICScanNumbers(intSurveyScanIndex - 1)
    ''                            udtSICDetails.SICDataScanIntervals(intSurveyScanIndex) = CByte(Math.Min(Byte.MaxValue, intScanDelta))        ' Make sure the Scan Interval is, at most, 255; it will typically be 1 or 4
    ''                        End If
    ''                    Next

    ''                    ' Record the fragmentation scan number
    ''                    intFragScanNumber = scanList.FragScans(scanList.ParentIons(intParentIonIndex).FragScanIndices(0)).ScanNumber

    ''                    ' Determine the value for .ParentIonIntensity
    ''                    blnSuccess = mMASICPeakFinder.ComputeParentIonIntensity(udtSICDetails.SICDataCount, intSICScanNumbers, udtSICDetails.SICData, .Peak, intFragScanNumber)

    ''                    blnSuccess = mMASICPeakFinder.FindSICPeakAndArea(udtSICDetails.SICDataCount, intSICScanNumbers, udtSICDetails.SICData, .SICPotentialAreaStatsForPeak, .Peak,
    ''                                                                     udtSmoothedYDataSubset, sicOptions.SICPeakFinderOptions,
    ''                                                                     udtSICPotentialAreaStatsForRegion,
    ''                                                                     Not blnCustomSICPeak, scanList.SIMDataPresent, RECOMPUTE_NOISE_LEVEL)

    ''                    If blnSuccess Then
    ''                        ' Record the survey scan indices of the peak max, start, and end
    ''                        ' Note that .ScanTypeForPeakIndices was set earlier in this function
    ''                        .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
    ''                        .PeakScanIndexStart = udtSICDetails.SICScanIndices(.Peak.IndexBaseLeft)
    ''                        .PeakScanIndexEnd = udtSICDetails.SICScanIndices(.Peak.IndexBaseRight)
    ''                    Else
    ''                        ' No peak found
    ''                        .PeakScanIndexMax = udtSICDetails.SICScanIndices(.Peak.IndexMax)
    ''                        .PeakScanIndexStart = .PeakScanIndexMax
    ''                        .PeakScanIndexEnd = .PeakScanIndexMax

    ''                        With .Peak
    ''                            .MaxIntensityValue = udtSICDetails.SICData(.IndexMax)
    ''                            .IndexBaseLeft = .IndexMax
    ''                            .IndexBaseRight = .IndexMax
    ''                            .FWHMScanWidth = 1
    ''                            ' Assign the intensity of the peak at the observed maximum to the area
    ''                            .Area = .MaxIntensityValue

    ''                            .SignalToNoiseRatio = mMASICPeakFinder.ComputeSignalToNoise(.MaxIntensityValue, .BaselineNoiseStats.NoiseLevel)
    ''                        End With
    ''                    End If
    ''                End With

    ''                ' Update .OptimalPeakApexScanNumber
    ''                ' Note that a valid peak will typically have .IndexBaseLeft or .IndexBaseRight different from .IndexMax
    ''                With .ParentIons(intParentIonIndex)
    ''                    .OptimalPeakApexScanNumber = scanList.SurveyScans(udtSICDetails.SICScanIndices(.SICStats.Peak.IndexMax)).ScanNumber
    ''                End With

    ''            End If

    ''        End With

    ''        blnSuccess = True

    ''    Catch ex As Exception
    ''        ReportError("Error finding SIC peaks and their areas", ex, eMasicErrorCodes.FindSICPeaksError)
    ''        blnSuccess = False
    ''    End Try

    ''    Return blnSuccess

    ''End Function

    Private Function GetNextSurveyScanIndex(surveyScans As IList(Of clsScanInfo), intSurveyScanIndex As Integer) As Integer
        ' Returns the next adjacent survey scan index
        ' If the given survey scan is not a SIM scan, then simply returns the next index
        ' If the given survey scan is a SIM scan, then returns the next survey scan with the same .SIMIndex
        ' If no appropriate next survey scan index is found, then returns intSurveyScanIndex instead

        Dim intNextSurveyScanIndex As Integer
        Dim intSIMIndex As Integer

        Try
            If intSurveyScanIndex < surveyScans.Count - 1 AndAlso intSurveyScanIndex >= 0 Then
                If surveyScans(intSurveyScanIndex).SIMScan Then
                    intSIMIndex = surveyScans(intSurveyScanIndex).SIMIndex

                    intNextSurveyScanIndex = intSurveyScanIndex + 1
                    Do While intNextSurveyScanIndex < surveyScans.Count
                        If surveyScans(intNextSurveyScanIndex).SIMIndex = intSIMIndex Then
                            Exit Do
                        Else
                            intNextSurveyScanIndex += 1
                        End If
                    Loop

                    If intNextSurveyScanIndex = surveyScans.Count Then
                        ' Match was not found
                        intNextSurveyScanIndex = intSurveyScanIndex
                    End If

                    Return intNextSurveyScanIndex
                Else
                    ' Not a SIM Scan
                    Return intSurveyScanIndex + 1
                End If
            Else
                ' intSurveyScanIndex is the final survey scan or is less than 0
                Return intSurveyScanIndex
            End If
        Catch ex As Exception
            ' Error occurred; simply return intSurveyScanIndex
            Return intSurveyScanIndex
        End Try

    End Function

    Private Function GetPreviousSurveyScanIndex(surveyScans As IList(Of clsScanInfo), intSurveyScanIndex As Integer) As Integer
        ' Returns the previous adjacent survey scan index
        ' If the given survey scan is not a SIM scan, then simply returns the previous index
        ' If the given survey scan is a SIM scan, then returns the previous survey scan with the same .SIMIndex
        ' If no appropriate next survey scan index is found, then returns intSurveyScanIndex instead

        Dim intPreviousSurveyScanIndex As Integer
        Dim intSIMIndex As Integer

        Try
            If intSurveyScanIndex > 0 AndAlso intSurveyScanIndex < surveyScans.Count Then
                If surveyScans(intSurveyScanIndex).SIMScan Then
                    intSIMIndex = surveyScans(intSurveyScanIndex).SIMIndex

                    intPreviousSurveyScanIndex = intSurveyScanIndex - 1
                    Do While intPreviousSurveyScanIndex >= 0
                        If surveyScans(intPreviousSurveyScanIndex).SIMIndex = intSIMIndex Then
                            Exit Do
                        Else
                            intPreviousSurveyScanIndex -= 1
                        End If
                    Loop

                    If intPreviousSurveyScanIndex < 0 Then
                        ' Match was not found
                        intPreviousSurveyScanIndex = intSurveyScanIndex
                    End If

                    Return intPreviousSurveyScanIndex
                Else
                    ' Not a SIM Scan
                    Return intSurveyScanIndex - 1
                End If
            Else
                ' intSurveyScanIndex is the first survey scan or is less than 0
                Return intSurveyScanIndex
            End If
        Catch ex As Exception
            ' Error occurred; simply return intSurveyScanIndex
            Return intSurveyScanIndex
        End Try

    End Function

    Private Function GetScanTypeName(eScanType As clsScanList.eScanTypeConstants) As String
        Select Case eScanType
            Case clsScanList.eScanTypeConstants.SurveyScan
                Return "survey scan"
            Case clsScanList.eScanTypeConstants.FragScan
                Return "frag scan"
            Case Else
                Return "unknown scan type"
        End Select
    End Function

    Private Function InterpolateX(
      ByRef sngInterpolatedXValue As Single,
      X1 As Integer,
      X2 As Integer,
      Y1 As Single,
      Y2 As Single,
      sngTargetY As Single) As Boolean

        ' Checks if Y1 or Y2 is less than sngTargetY
        ' If it is, then determines the X value that corresponds to sngTargetY by interpolating the line between (X1, Y1) and (X2, Y2)
        '
        ' Returns True if a match is found; otherwise, returns false

        Dim sngDeltaY As Single
        Dim sngFraction As Single
        Dim intDeltaX As Integer
        Dim sngTargetX As Single

        If Y1 < sngTargetY OrElse Y2 < sngTargetY Then
            If Y1 < sngTargetY AndAlso Y2 < sngTargetY Then
                ' Both of the Y values are less than sngTargetY
                ' We cannot interpolate
                ReportError("This code should normally not be reached (clsMasic->InterpolateX)")
                Return False
            Else
                sngDeltaY = Y2 - Y1                                 ' Yes, this is y-two minus y-one
                sngFraction = (sngTargetY - Y1) / sngDeltaY
                intDeltaX = X2 - X1                                 ' Yes, this is x-two minus x-one

                sngTargetX = sngFraction * intDeltaX + X1

                If Math.Abs(sngTargetX - X1) >= 0 AndAlso Math.Abs(sngTargetX - X2) >= 0 Then
                    sngInterpolatedXValue = sngTargetX
                    Return True
                Else
                    ReportError("TargetX is not between X1 and X2; this shouldn't happen (clsMasic->InterpolateX)")
                    Return False
                End If

            End If
        Else
            Return False
        End If

    End Function

    Private Function LookupRTByScanNumber(
      scanList As IList(Of clsScanInfo),
      intScanListCount As Integer,
      intScanListArray() As Integer,
      intScanNumberToFind As Integer) As Single

        ' intScanListArray() must be populated with the scan numbers in scanList() before calling this function

        Dim intScanIndex As Integer
        Dim intMatchIndex As Integer

        Try
            intMatchIndex = Array.IndexOf(intScanListArray, intScanNumberToFind)

            If intMatchIndex < 0 Then
                ' Need to find the closest scan with this scan number
                intMatchIndex = 0
                For intScanIndex = 0 To intScanListCount - 1
                    If scanList(intScanIndex).ScanNumber <= intScanNumberToFind Then intMatchIndex = intScanIndex
                Next
            End If

            Return scanList(intMatchIndex).ScanTime
        Catch ex As Exception
            ' Ignore any errors that occur in this function
            ReportError("Error in LookupRTByScanNumber", ex)
            Return 0
        End Try

    End Function

    Private Sub SaveBPIWork(
          scanList As IList(Of clsScanInfo),
          intScanCount As Integer,
          strOutputFilePath As String,
          blnSaveTIC As Boolean,
          cColDelimiter As Char)

        Dim srOutFile As StreamWriter
        Dim intScanIndex As Integer

        srOutFile = New StreamWriter(strOutputFilePath)

        If blnSaveTIC Then
            srOutFile.WriteLine("Time" & cColDelimiter & "TotalIonIntensity")
        Else
            srOutFile.WriteLine("Time" & cColDelimiter & "BasePeakIntensity" & cColDelimiter & "m/z")
        End If

        For intScanIndex = 0 To intScanCount - 1
            With scanList(intScanIndex)
                If blnSaveTIC Then
                    srOutFile.WriteLine(StringUtilities.DblToString(.ScanTime, 5) & cColDelimiter &
                                        StringUtilities.DblToString(.TotalIonIntensity, 2))
                Else
                    srOutFile.WriteLine(StringUtilities.DblToString(.ScanTime, 5) & cColDelimiter &
                                        StringUtilities.DblToString(.BasePeakIonIntensity, 2) & cColDelimiter &
                                        StringUtilities.DblToString(.BasePeakIonMZ, 4))
                End If

            End With
        Next

        srOutFile.Close()

    End Sub

    Private Function ValidateXRawAccessor() As Boolean

        Static blnValidated As Boolean
        Static blnValidationSaved As Boolean

        If blnValidated Then
            Return blnValidationSaved
        End If

        Try
            Dim objXRawAccess As New XRawFileIO()
            Dim isValid = objXRawAccess.IsMSFileReaderInstalled()

            If isValid Then
                blnValidationSaved = True
                Return True
            Else
                ReportError("MSFileReader was not found; Thermo .raw files cannot be read.  Download the MSFileReader installer " &
                            "by creating an account at https://thermo.flexnetoperations.com/control/thmo/login , " &
                            "then logging in and choosing 'Utility Software'")
                Return False
            End If

        Catch ex As Exception
            blnValidationSaved = False
            Return False
        End Try

    End Function

End Class
