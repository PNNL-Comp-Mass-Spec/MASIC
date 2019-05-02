Imports MASIC.clsMASIC
Imports System.Runtime.InteropServices

Public Class clsParentIonProcessing
    Inherits clsMasicEventNotifier

#Region "Structures"
    Private Structure udtBinnedDataType
        Public BinnedDataStartX As Single
        Public BinSize As Single
        Public BinCount As Integer

        ''' <summary>
        ''' 0-based array, ranging from 0 to BinCount-1; First bin starts at BinnedDataStartX
        ''' </summary>
        Public BinnedIntensities() As Single

        ''' <summary>
        ''' 0-based array, ranging from 0 to BinCount-1; First bin starts at BinnedDataStartX + BinSize/2
        ''' </summary>
        Public BinnedIntensitiesOffset() As Single

        Public Overrides Function ToString() As String
            Return "BinCount: " & BinCount & ", BinSize: " & BinSize.ToString("0.0") & ", StartX: " & BinnedDataStartX.ToString("0.0")
        End Function
    End Structure

    Friend Structure udtFindSimilarIonsDataType
        Public MZPointerArray() As Integer
        Public IonInUseCount As Integer
        Public IonUsed() As Boolean
        Public UniqueMZListCount As Integer
        Public UniqueMZList() As udtUniqueMZListType

        Public Overrides Function ToString() As String
            Return "IonInUseCount: " & IonInUseCount
        End Function
    End Structure

    Friend Structure udtUniqueMZListType
        ''' <summary>
        ''' Average m/z
        ''' </summary>
        Public MZAvg As Double

        ''' <summary>
        ''' Highest intensity value of the similar parent ions
        ''' </summary>
        Public MaxIntensity As Double

        ''' <summary>
        ''' Largest peak intensity value of the similar parent ions
        ''' </summary>
        Public MaxPeakArea As Double

        ''' <summary>
        ''' Scan number of the parent ion with the highest intensity
        ''' </summary>
        Public ScanNumberMaxIntensity As Integer

        ''' <summary>
        ''' Elution time of the parent ion with the highest intensity
        ''' </summary>
        Public ScanTimeMaxIntensity As Single

        ''' <summary>
        ''' Pointer to an entry in .ParentIons()
        ''' </summary>
        Public ParentIonIndexMaxIntensity As Integer

        ''' <summary>
        ''' Pointer to an entry in .ParentIons()
        ''' </summary>
        Public ParentIonIndexMaxPeakArea As Integer

        Public MatchCount As Integer

        ''' <summary>
        ''' Pointer to an entry in .ParentIons()
        ''' </summary>
        Public MatchIndices() As Integer

        Public Overrides Function ToString() As String
            Return "m/z avg: " & MZAvg & ", MatchCount: " & MatchCount
        End Function
    End Structure

#End Region

#Region "Classwide Variables"
    Private ReadOnly mReporterIons As clsReporterIons
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="reporterIons"></param>
    Public Sub New(reporterIons As clsReporterIons)
        mReporterIons = reporterIons
    End Sub

    Public Sub AddUpdateParentIons(
      scanList As clsScanList,
      surveyScanIndex As Integer,
      parentIonMZ As Double,
      fragScanIndex As Integer,
      objSpectraCache As clsSpectraCache,
      sicOptions As clsSICOptions)

        AddUpdateParentIons(scanList, surveyScanIndex, parentIonMZ, 0, 0, fragScanIndex, objSpectraCache, sicOptions)
    End Sub

    Public Sub AddUpdateParentIons(
      scanList As clsScanList,
      surveyScanIndex As Integer,
      parentIonMZ As Double,
      mrmInfo As clsMRMScanInfo,
      objSpectraCache As clsSpectraCache,
      sicOptions As clsSICOptions)

        Dim mrmIndex As Integer
        Dim mrmDaughterMZ As Double
        Dim mrmToleranceHalfWidth As Double

        For mrmIndex = 0 To mrmInfo.MRMMassCount - 1
            mrmDaughterMZ = mrmInfo.MRMMassList(mrmIndex).CentralMass
            mrmToleranceHalfWidth = Math.Round((mrmInfo.MRMMassList(mrmIndex).EndMass - mrmInfo.MRMMassList(mrmIndex).StartMass) / 2, 6)
            If mrmToleranceHalfWidth < 0.001 Then
                mrmToleranceHalfWidth = 0.001
            End If

            AddUpdateParentIons(scanList, surveyScanIndex, parentIonMZ, mrmDaughterMZ, mrmToleranceHalfWidth, scanList.FragScans.Count - 1, objSpectraCache, sicOptions)
        Next

    End Sub

    Private Sub AddUpdateParentIons(
      scanList As clsScanList,
      surveyScanIndex As Integer,
      parentIonMZ As Double,
      mrmDaughterMZ As Double,
      mrmToleranceHalfWidth As Double,
      fragScanIndex As Integer,
      objSpectraCache As clsSpectraCache,
      sicOptions As clsSICOptions)

        Const MINIMUM_TOLERANCE_PPM = 0.01
        Const MINIMUM_TOLERANCE_DA = 0.0001

        ' Checks to see if the parent ion specified by surveyScanIndex and parentIonMZ exists in .ParentIons()
        ' If mrmDaughterMZ is > 0, then also considers that value when determining uniqueness
        '
        ' If the parent ion entry already exists, then adds an entry to .FragScanIndices()
        ' If it does not exist, then adds a new entry to .ParentIons()
        ' Note that typically fragScanIndex will equal scanList.FragScans.Count - 1

        ' If surveyScanIndex < 0 then the first scan(s) in the file occurred before we encountered a survey scan
        ' In this case, we cannot properly associate the fragmentation scan with a survey scan

        Dim parentIonIndex As Integer

        Dim parentIonTolerance As Double
        Dim parentIonMZMatch As Double

        If sicOptions.SICToleranceIsPPM Then
            parentIonTolerance = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForPPM
            If parentIonTolerance < MINIMUM_TOLERANCE_PPM Then
                parentIonTolerance = MINIMUM_TOLERANCE_PPM
            End If
        Else
            parentIonTolerance = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
            If parentIonTolerance < MINIMUM_TOLERANCE_DA Then
                parentIonTolerance = MINIMUM_TOLERANCE_DA
            End If
        End If

        ' See if an entry exists yet in .ParentIons for the parent ion for this fragmentation scan
        Dim matchFound = False

        If mrmDaughterMZ > 0 Then
            If sicOptions.SICToleranceIsPPM Then
                ' Force the tolerances to 0.01 m/z units
                parentIonTolerance = MINIMUM_TOLERANCE_PPM
            Else
                ' Force the tolerances to 0.01 m/z units
                parentIonTolerance = MINIMUM_TOLERANCE_DA
            End If
        End If

        If parentIonMZ > 0 Then

            Dim parentIonToleranceDa = GetParentIonToleranceDa(sicOptions, parentIonMZ, parentIonTolerance)

            For parentIonIndex = scanList.ParentIonInfoCount - 1 To 0 Step -1
                If scanList.ParentIons(parentIonIndex).SurveyScanIndex >= surveyScanIndex Then
                    If Math.Abs(scanList.ParentIons(parentIonIndex).MZ - parentIonMZ) <= parentIonToleranceDa Then
                        If mrmDaughterMZ < Double.Epsilon OrElse
                          Math.Abs(scanList.ParentIons(parentIonIndex).MRMDaughterMZ - mrmDaughterMZ) <= parentIonToleranceDa Then
                            matchFound = True
                            Exit For
                        End If
                    End If
                Else
                    Exit For
                End If
            Next
        End If


        If Not matchFound Then
            ' Add a new parent ion entry to .ParentIons(), but only if surveyScanIndex >= 0

            If surveyScanIndex >= 0 Then
                With scanList
                    If .ParentIonInfoCount >= .ParentIons.Length Then
                        ReDim Preserve .ParentIons(.ParentIonInfoCount + 100)
                        For index = .ParentIonInfoCount To .ParentIons.Length - 1
                            ReDim .ParentIons(index).FragScanIndices(0)
                        Next
                    End If

                    With .ParentIons(.ParentIonInfoCount)
                        .UpdateMz(parentIonMZ)

                        .SurveyScanIndex = surveyScanIndex

                        .FragScanIndices(0) = fragScanIndex
                        .FragScanIndexCount = 1
                        .CustomSICPeak = False

                        .MRMDaughterMZ = mrmDaughterMZ
                        .MRMToleranceHalfWidth = mrmToleranceHalfWidth
                    End With
                    .ParentIons(.ParentIonInfoCount).OptimalPeakApexScanNumber = .SurveyScans(surveyScanIndex).ScanNumber        ' Was: .FragScans(fragScanIndex).ScanNumber
                    .ParentIons(.ParentIonInfoCount).PeakApexOverrideParentIonIndex = -1
                    .FragScans(fragScanIndex).FragScanInfo.ParentIonInfoIndex = .ParentIonInfoCount

                    ' Look for .MZ in the survey scan, using a tolerance of parentIonTolerance
                    ' If found, then update the mass to the matched ion
                    ' This is done to determine the parent ion mass more precisely
                    If sicOptions.RefineReportedParentIonMZ Then
                        If FindClosestMZ(objSpectraCache, .SurveyScans, surveyScanIndex, parentIonMZ, parentIonTolerance, parentIonMZMatch) Then
                            .ParentIons(.ParentIonInfoCount).UpdateMz(parentIonMZMatch)
                        End If
                    End If

                    .ParentIonInfoCount += 1
                End With
            End If
        Else
            ' Add a new entry to .FragScanIndices() for the matching parent ion
            ' However, do not add a new entry if this is an MRM scan

            If mrmDaughterMZ < Double.Epsilon Then
                With scanList
                    With .ParentIons(parentIonIndex)
                        ReDim Preserve .FragScanIndices(.FragScanIndexCount)
                        .FragScanIndices(.FragScanIndexCount) = fragScanIndex
                        .FragScanIndexCount += 1
                    End With
                    .FragScans(fragScanIndex).FragScanInfo.ParentIonInfoIndex = parentIonIndex
                End With
            End If
        End If

    End Sub

    Private Sub AppendParentIonToUniqueMZEntry(
      scanList As clsScanList,
      parentIonIndex As Integer,
      ByRef udtMZListEntry As udtUniqueMZListType,
      searchMZOffset As Double)

        With scanList.ParentIons(parentIonIndex)
            If udtMZListEntry.MatchCount = 0 Then
                udtMZListEntry.MZAvg = .MZ - searchMZOffset
                udtMZListEntry.MatchCount = 1

                ' Note that .MatchIndices() was initialized in InitializeUniqueMZListMatchIndices()
                udtMZListEntry.MatchIndices(0) = parentIonIndex
            Else
                ' Update the average MZ: NewAvg = (OldAvg * OldCount + NewValue) / NewCount
                udtMZListEntry.MZAvg = (udtMZListEntry.MZAvg * udtMZListEntry.MatchCount + (.MZ - searchMZOffset)) / (udtMZListEntry.MatchCount + 1)

                ReDim Preserve udtMZListEntry.MatchIndices(udtMZListEntry.MatchCount)
                udtMZListEntry.MatchIndices(udtMZListEntry.MatchCount) = parentIonIndex
                udtMZListEntry.MatchCount += 1
            End If

            With .SICStats
                If .Peak.MaxIntensityValue > udtMZListEntry.MaxIntensity OrElse udtMZListEntry.MatchCount = 1 Then
                    udtMZListEntry.MaxIntensity = .Peak.MaxIntensityValue
                    If .ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan Then
                        udtMZListEntry.ScanNumberMaxIntensity = scanList.FragScans(.PeakScanIndexMax).ScanNumber
                        udtMZListEntry.ScanTimeMaxIntensity = scanList.FragScans(.PeakScanIndexMax).ScanTime
                    Else
                        udtMZListEntry.ScanNumberMaxIntensity = scanList.SurveyScans(.PeakScanIndexMax).ScanNumber
                        udtMZListEntry.ScanTimeMaxIntensity = scanList.SurveyScans(.PeakScanIndexMax).ScanTime
                    End If
                    udtMZListEntry.ParentIonIndexMaxIntensity = parentIonIndex
                End If

                If .Peak.Area > udtMZListEntry.MaxPeakArea OrElse udtMZListEntry.MatchCount = 1 Then
                    udtMZListEntry.MaxPeakArea = .Peak.Area
                    udtMZListEntry.ParentIonIndexMaxPeakArea = parentIonIndex
                End If
            End With

        End With

    End Sub

    Private Function CompareFragSpectraForParentIons(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      parentIonIndex1 As Integer,
      parentIonIndex2 As Integer,
      binningOptions As clsBinningOptions,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
      dataImportUtilities As DataInput.clsDataImport) As Single

        ' Compare the fragmentation spectra for the two parent ions
        ' Returns the highest similarity score (ranging from 0 to 1)
        ' Returns 0 if no similarity or no spectra to compare
        ' Returns -1 if an error

        Dim fragIndex1, fragIndex2 As Integer
        Dim fragSpectrumIndex1, fragSpectrumIndex2 As Integer
        Dim poolIndex1, poolIndex2 As Integer
        Dim similarityScore, highestSimilarityScore As Single

        Try
            If scanList.ParentIons(parentIonIndex1).CustomSICPeak OrElse scanList.ParentIons(parentIonIndex2).CustomSICPeak Then
                ' Custom SIC values do not have fragmentation spectra; nothing to compare
                highestSimilarityScore = 0
            ElseIf scanList.ParentIons(parentIonIndex1).MRMDaughterMZ > 0 OrElse scanList.ParentIons(parentIonIndex2).MRMDaughterMZ > 0 Then
                ' MRM Spectra should not be compared
                highestSimilarityScore = 0
            Else
                highestSimilarityScore = 0
                For fragIndex1 = 0 To scanList.ParentIons(parentIonIndex1).FragScanIndexCount - 1
                    fragSpectrumIndex1 = scanList.ParentIons(parentIonIndex1).FragScanIndices(fragIndex1)

                    If Not objSpectraCache.ValidateSpectrumInPool(scanList.FragScans(fragSpectrumIndex1).ScanNumber, poolIndex1) Then
                        SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                        Return -1
                    End If

                    If Not DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD Then
                        dataImportUtilities.DiscardDataBelowNoiseThreshold(objSpectraCache.SpectraPool(poolIndex1), scanList.FragScans(fragSpectrumIndex1).BaselineNoiseStats.NoiseLevel, 0, 0, noiseThresholdOptions)
                    End If

                    For fragIndex2 = 0 To scanList.ParentIons(parentIonIndex2).FragScanIndexCount - 1

                        fragSpectrumIndex2 = scanList.ParentIons(parentIonIndex2).FragScanIndices(fragIndex2)

                        If Not objSpectraCache.ValidateSpectrumInPool(scanList.FragScans(fragSpectrumIndex2).ScanNumber, poolIndex2) Then
                            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                            Return -1
                        End If

                        If Not DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD Then
                            dataImportUtilities.DiscardDataBelowNoiseThreshold(objSpectraCache.SpectraPool(poolIndex2), scanList.FragScans(fragSpectrumIndex2).BaselineNoiseStats.NoiseLevel, 0, 0, noiseThresholdOptions)
                        End If

                        similarityScore = CompareSpectra(objSpectraCache.SpectraPool(poolIndex1), objSpectraCache.SpectraPool(poolIndex2), binningOptions)

                        If similarityScore > highestSimilarityScore Then
                            highestSimilarityScore = similarityScore
                        End If
                    Next
                Next

            End If
        Catch ex As Exception
            ReportError("Error in CompareFragSpectraForParentIons", ex)
            Return -1
        End Try

        Return highestSimilarityScore

    End Function

    Private Function CompareSpectra(
      fragSpectrum1 As clsMSSpectrum,
      fragSpectrum2 As clsMSSpectrum,
      binningOptions As clsBinningOptions,
      Optional considerOffsetBinnedData As Boolean = True) As Single

        ' Compares the two spectra and returns a similarity score (ranging from 0 to 1)
        ' Perfect match is 1; no similarity is 0
        ' Note that both the standard binned data and the offset binned data are compared
        ' If considerOffsetBinnedData = True, then the larger of the two scores is returned
        '  similarity scores is returned
        '
        ' If an error, returns -1

        Dim udtBinnedSpectrum1 = New udtBinnedDataType
        Dim udtBinnedSpectrum2 = New udtBinnedDataType

        Dim success As Boolean

        Try

            Dim objCorrelate = New clsCorrelation(binningOptions)
            RegisterEvents(objCorrelate)

            Const eCorrelationMethod = clsCorrelation.cmCorrelationMethodConstants.Pearson

            ' Bin the data in the first spectrum
            success = CompareSpectraBinData(objCorrelate, fragSpectrum1, udtBinnedSpectrum1)
            If Not success Then Return -1

            ' Bin the data in the second spectrum
            success = CompareSpectraBinData(objCorrelate, fragSpectrum2, udtBinnedSpectrum2)
            If Not success Then Return -1

            ' Now compare the binned spectra
            ' Similarity will be 0 if either instance of BinnedIntensities has fewer than 5 data points
            Dim similarity1 = objCorrelate.Correlate(udtBinnedSpectrum1.BinnedIntensities, udtBinnedSpectrum2.BinnedIntensities, eCorrelationMethod)

            If Not considerOffsetBinnedData Then
                Return similarity1
            End If

            Dim similarity2 = objCorrelate.Correlate(udtBinnedSpectrum1.BinnedIntensitiesOffset, udtBinnedSpectrum2.BinnedIntensitiesOffset, eCorrelationMethod)
            Return Math.Max(similarity1, similarity2)

        Catch ex As Exception
            ReportError("CompareSpectra: " & ex.Message, ex)
            Return -1
        End Try

    End Function

    Private Function CompareSpectraBinData(
      objCorrelate As clsCorrelation,
      fragSpectrum As clsMSSpectrum,
      ByRef udtBinnedSpectrum As udtBinnedDataType) As Boolean

        Dim index As Integer
        Dim xData As Single()
        Dim yData As Single()

        ' Make a copy of the data, excluding any Reporter Ion data
        Dim filteredDataCount = 0
        ReDim xData(fragSpectrum.IonCount - 1)
        ReDim yData(fragSpectrum.IonCount - 1)

        For index = 0 To fragSpectrum.IonCount - 1
            If Not clsUtilities.CheckPointInMZIgnoreRange(fragSpectrum.IonsMZ(index),
                                                          mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                          mReporterIons.MZIntensityFilterIgnoreRangeEnd) Then
                xData(filteredDataCount) = CSng(fragSpectrum.IonsMZ(index))
                yData(filteredDataCount) = CSng(fragSpectrum.IonsIntensity(index))
                filteredDataCount += 1
            End If
        Next

        If filteredDataCount > 0 Then
            ReDim Preserve xData(filteredDataCount - 1)
            ReDim Preserve yData(filteredDataCount - 1)
        Else
            ReDim Preserve xData(0)
            ReDim Preserve yData(0)
        End If

        udtBinnedSpectrum.BinnedDataStartX = objCorrelate.BinStartX
        udtBinnedSpectrum.BinSize = objCorrelate.BinSize

        udtBinnedSpectrum.BinnedIntensities = Nothing
        udtBinnedSpectrum.BinnedIntensitiesOffset = Nothing

        ' Note that the data in xData and yData should have already been filtered to discard data points below the noise threshold intensity
        Dim success = objCorrelate.BinData(xData, yData, udtBinnedSpectrum.BinnedIntensities, udtBinnedSpectrum.BinnedIntensitiesOffset, udtBinnedSpectrum.BinCount)

        Return success

    End Function

    Private Function FindClosestMZ(
      objSpectraCache As clsSpectraCache,
      scanList As IList(Of clsScanInfo),
      spectrumIndex As Integer,
      searchMZ As Double,
      toleranceMZ As Double,
      <Out> ByRef bestMatchMZ As Double) As Boolean

        Dim poolIndex As Integer
        Dim success As Boolean

        bestMatchMZ = 0

        Try
            If scanList(spectrumIndex).IonCount = 0 And scanList(spectrumIndex).IonCountRaw = 0 Then
                ' No data in this spectrum
                success = False
            Else
                If Not objSpectraCache.ValidateSpectrumInPool(scanList(spectrumIndex).ScanNumber, poolIndex) Then
                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                    success = False
                Else
                    With objSpectraCache.SpectraPool(poolIndex)
                        success = FindClosestMZ(.IonsMZ, .IonCount, searchMZ, toleranceMZ, bestMatchMZ)
                    End With
                End If

            End If
        Catch ex As Exception
            ReportError("Error in FindClosestMZ", ex)
            success = False
        End Try

        Return success

    End Function

    Private Function FindClosestMZ(
      mzList As IList(Of Double),
      ionCount As Integer,
      searchMZ As Double,
      toleranceMZ As Double,
      <Out> ByRef bestMatchMZ As Double) As Boolean

        ' Searches mzList for the closest match to searchMZ within tolerance bestMatchMZ
        ' If a match is found, then updates bestMatchMZ to the m/z of the match and returns True

        Dim dataIndex As Integer
        Dim closestMatchIndex As Integer
        Dim massDifferenceAbs As Double
        Dim bestMassDifferenceAbs As Double

        Try
            closestMatchIndex = -1
            For dataIndex = 0 To ionCount - 1
                massDifferenceAbs = Math.Abs(mzList(dataIndex) - searchMZ)
                If massDifferenceAbs <= toleranceMZ Then
                    If closestMatchIndex < 0 OrElse massDifferenceAbs < bestMassDifferenceAbs Then
                        closestMatchIndex = dataIndex
                        bestMassDifferenceAbs = massDifferenceAbs
                    End If
                End If
            Next

        Catch ex As Exception
            ReportError("Error in FindClosestMZ", ex)
            closestMatchIndex = -1
        End Try

        If closestMatchIndex >= 0 Then
            bestMatchMZ = mzList(closestMatchIndex)
            Return True
        Else
            bestMatchMZ = 0
            Return False
        End If

    End Function

    Public Function FindSimilarParentIons(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      masicOptions As clsMASICOptions,
      dataImportUtilities As DataInput.clsDataImport,
      ByRef ionUpdateCount As Integer) As Boolean

        ' Look for parent ions that have similar m/z values and are nearby one another in time
        ' For the groups of similar ions, assign the scan number of the highest intensity parent ion to the other similar parent ions

        Dim success As Boolean

        Try
            ionUpdateCount = 0

            If scanList.ParentIonInfoCount <= 0 Then
                If masicOptions.SuppressNoParentIonsError Then
                    Return True
                Else
                    Return False
                End If
            End If

            Console.Write("Finding similar parent ions ")
            ReportMessage("Finding similar parent ions")
            UpdateProgress(0, "Finding similar parent ions")

            Dim findSimilarIonsDataCount As Integer
            Dim udtFindSimilarIonsData As udtFindSimilarIonsDataType

            ' Original m/z values, rounded to 2 decimal places
            Dim mzList() As Double
            Dim intensityPointerArray() As Integer
            Dim intensityList() As Double

            ' Populate udtFindSimilarIonsData.MZPointerArray and mzList, plus intensityPointerArray and intensityList()
            ReDim udtFindSimilarIonsData.MZPointerArray(scanList.ParentIonInfoCount - 1)
            ReDim udtFindSimilarIonsData.IonUsed(scanList.ParentIonInfoCount - 1)

            ReDim mzList(scanList.ParentIonInfoCount - 1)
            ReDim intensityPointerArray(scanList.ParentIonInfoCount - 1)
            ReDim intensityList(scanList.ParentIonInfoCount - 1)

            Dim parentIonIndex As Integer
            Dim ionInUseCountOriginal As Integer

            findSimilarIonsDataCount = 0
            For parentIonIndex = 0 To scanList.ParentIonInfoCount - 1

                Dim includeParentIon As Boolean

                If scanList.ParentIons(parentIonIndex).MRMDaughterMZ > 0 Then
                    includeParentIon = False
                Else
                    If masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                        includeParentIon = scanList.ParentIons(parentIonIndex).CustomSICPeak
                    Else
                        includeParentIon = True
                    End If
                End If

                If includeParentIon Then
                    udtFindSimilarIonsData.MZPointerArray(findSimilarIonsDataCount) = parentIonIndex
                    mzList(findSimilarIonsDataCount) = Math.Round(scanList.ParentIons(parentIonIndex).MZ, 2)

                    intensityPointerArray(findSimilarIonsDataCount) = parentIonIndex
                    intensityList(findSimilarIonsDataCount) = scanList.ParentIons(parentIonIndex).SICStats.Peak.MaxIntensityValue
                    findSimilarIonsDataCount += 1
                End If
            Next

            If udtFindSimilarIonsData.MZPointerArray.Length <> findSimilarIonsDataCount AndAlso findSimilarIonsDataCount > 0 Then
                ReDim Preserve udtFindSimilarIonsData.MZPointerArray(findSimilarIonsDataCount - 1)
                ReDim Preserve mzList(findSimilarIonsDataCount - 1)
                ReDim Preserve intensityPointerArray(findSimilarIonsDataCount - 1)
                ReDim Preserve intensityList(findSimilarIonsDataCount - 1)
            End If

            If findSimilarIonsDataCount = 0 Then
                If masicOptions.SuppressNoParentIonsError Then
                    Return True
                Else
                    If scanList.MRMDataPresent Then
                        Return True
                    Else
                        Return False
                    End If
                End If
            End If

            ReportMessage("FindSimilarParentIons: Sorting the mz arrays")

            ' Sort the MZ arrays
            Array.Sort(mzList, udtFindSimilarIonsData.MZPointerArray)

            ReportMessage("FindSimilarParentIons: Populate objSearchRange")

            ' Populate objSearchRange
            ' Set UsePointerIndexArray to false to prevent .FillWithData trying to sort mzList
            ' (the data was already sorted above)
            Dim objSearchRange = New clsSearchRange() With {
                .UsePointerIndexArray = False
            }

            success = objSearchRange.FillWithData(mzList)

            ReportMessage("FindSimilarParentIons: Sort the intensity arrays")

            ' Sort the Intensity arrays
            Array.Sort(intensityList, intensityPointerArray)

            ' Reverse the order of intensityPointerArray so that it is ordered from the most intense to the least intense ion
            ' Note: We don't really need to reverse intensityList since we're done using it, but
            ' it doesn't take long, it won't hurt, and it will keep intensityList sync'd with intensityPointerArray
            Array.Reverse(intensityPointerArray)
            Array.Reverse(intensityList)


            ' Initialize udtUniqueMZList
            ' Pre-reserve enough space for findSimilarIonsDataCount entries to avoid repeated use of Redim Preserve
            udtFindSimilarIonsData.UniqueMZListCount = 0
            ReDim udtFindSimilarIonsData.UniqueMZList(findSimilarIonsDataCount - 1)

            ' Initialize the .UniqueMZList().MatchIndices() arrays
            InitializeUniqueMZListMatchIndices(udtFindSimilarIonsData.UniqueMZList, 0, findSimilarIonsDataCount - 1)

            ReportMessage("FindSimilarParentIons: Look for similar parent ions by using m/z and scan")
            Dim lastLogTime = DateTime.UtcNow

            ' Look for similar parent ions by using m/z and scan
            ' Step through the ions by decreasing intensity
            parentIonIndex = 0
            Do
                Dim originalIndex = intensityPointerArray(parentIonIndex)
                If udtFindSimilarIonsData.IonUsed(originalIndex) Then
                    ' Parent ion was already used; move onto the next one
                    parentIonIndex += 1
                Else
                    With udtFindSimilarIonsData
                        If .UniqueMZListCount >= udtFindSimilarIonsData.UniqueMZList.Length Then
                            ReDim Preserve .UniqueMZList(.UniqueMZListCount + 100)

                            ' Initialize the .UniqueMZList().MatchIndices() arrays
                            InitializeUniqueMZListMatchIndices(udtFindSimilarIonsData.UniqueMZList, .UniqueMZListCount, .UniqueMZList.Length - 1)

                        End If
                        AppendParentIonToUniqueMZEntry(scanList, originalIndex, .UniqueMZList(.UniqueMZListCount), 0)
                        .UniqueMZListCount += 1

                        .IonUsed(originalIndex) = True
                        .IonInUseCount = 1
                    End With

                    ' Look for other parent ions with m/z values in tolerance (must be within mass tolerance and scan tolerance)
                    ' If new values are added, then repeat the search using the updated udtUniqueMZList().MZAvg value
                    Do
                        ionInUseCountOriginal = udtFindSimilarIonsData.IonInUseCount
                        Dim currentMZ = udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).MZAvg

                        If currentMZ <= 0 Then Continue Do

                        FindSimilarParentIonsWork(objSpectraCache, currentMZ, 0, originalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        ' Look for similar 1+ spaced m/z values
                        FindSimilarParentIonsWork(objSpectraCache, currentMZ, 1, originalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        FindSimilarParentIonsWork(objSpectraCache, currentMZ, -1, originalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        ' Look for similar 2+ spaced m/z values
                        FindSimilarParentIonsWork(objSpectraCache, currentMZ, 0.5, originalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        FindSimilarParentIonsWork(objSpectraCache, currentMZ, -0.5, originalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        Dim parentIonToleranceDa = GetParentIonToleranceDa(masicOptions.SICOptions, currentMZ)

                        If parentIonToleranceDa <= 0.25 AndAlso masicOptions.SICOptions.SimilarIonMZToleranceHalfWidth <= 0.15 Then
                            ' Also look for similar 3+ spaced m/z values
                            FindSimilarParentIonsWork(objSpectraCache, currentMZ, 0.666, originalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)

                            FindSimilarParentIonsWork(objSpectraCache, currentMZ, 0.333, originalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)

                            FindSimilarParentIonsWork(objSpectraCache, currentMZ, -0.333, originalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)

                            FindSimilarParentIonsWork(objSpectraCache, currentMZ, -0.666, originalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)
                        End If


                    Loop While udtFindSimilarIonsData.IonInUseCount > ionInUseCountOriginal

                    parentIonIndex += 1
                End If

                If findSimilarIonsDataCount > 1 Then
                    If parentIonIndex Mod 100 = 0 Then
                        UpdateProgress(CShort(parentIonIndex / (findSimilarIonsDataCount - 1) * 100))
                    End If
                Else
                    UpdateProgress(1)
                End If

                UpdateCacheStats(objSpectraCache)
                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Exit Do
                End If

                If parentIonIndex Mod 100 = 0 Then
                    If DateTime.UtcNow.Subtract(lastLogTime).TotalSeconds >= 10 OrElse parentIonIndex Mod 500 = 0 Then
                        ReportMessage("Parent Ion Index: " & parentIonIndex.ToString())
                        Console.Write(".")
                        lastLogTime = DateTime.UtcNow
                    End If
                End If

            Loop While parentIonIndex < findSimilarIonsDataCount

            Console.WriteLine()

            ' Shrink the .UniqueMZList array to the appropriate length
            ReDim Preserve udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1)

            ReportMessage("FindSimilarParentIons: Update the scan numbers for the unique ions")

            ' Update the optimal peak apex scan numbers for the unique ions
            ionUpdateCount = 0
            For uniqueMZIndex = 0 To udtFindSimilarIonsData.UniqueMZListCount - 1
                With udtFindSimilarIonsData.UniqueMZList(uniqueMZIndex)
                    For matchIndex = 0 To .MatchCount - 1
                        parentIonIndex = .MatchIndices(matchIndex)

                        If scanList.ParentIons(parentIonIndex).MZ > 0 Then
                            If scanList.ParentIons(parentIonIndex).OptimalPeakApexScanNumber <> .ScanNumberMaxIntensity Then
                                ionUpdateCount += 1
                                scanList.ParentIons(parentIonIndex).OptimalPeakApexScanNumber = .ScanNumberMaxIntensity
                                scanList.ParentIons(parentIonIndex).PeakApexOverrideParentIonIndex = .ParentIonIndexMaxIntensity
                            End If
                        End If
                    Next
                End With
            Next

            success = True

        Catch ex As Exception
            ReportError("Error in FindSimilarParentIons", ex, eMasicErrorCodes.FindSimilarParentIonsError)
            success = False
        End Try

        Return success
    End Function

    Private Sub FindSimilarParentIonsWork(
      objSpectraCache As clsSpectraCache,
      searchMZ As Double,
      searchMZOffset As Double,
      originalIndex As Integer,
      scanList As clsScanList,
      ByRef udtFindSimilarIonsData As udtFindSimilarIonsDataType,
      masicOptions As clsMASICOptions,
      dataImportUtilities As DataInput.clsDataImport,
      objSearchRange As clsSearchRange)

        Dim indexFirst As Integer
        Dim indexLast As Integer

        Dim sicOptions = masicOptions.SICOptions
        Dim binningOptions = masicOptions.BinningOptions

        If objSearchRange.FindValueRange(searchMZ + searchMZOffset, sicOptions.SimilarIonMZToleranceHalfWidth, indexFirst, indexLast) Then

            For matchIndex = indexFirst To indexLast
                ' See if the matches are unused and within the scan tolerance
                Dim matchOriginalIndex = udtFindSimilarIonsData.MZPointerArray(matchIndex)

                If udtFindSimilarIonsData.IonUsed(matchOriginalIndex) Then Continue For

                Dim timeDiff As Single

                With scanList.ParentIons(matchOriginalIndex)

                    If .SICStats.ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan Then
                        If scanList.FragScans(.SICStats.PeakScanIndexMax).ScanTime < Single.Epsilon AndAlso
                           udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity < Single.Epsilon Then
                            ' Both elution times are 0; instead of computing the difference in scan time, compute the difference in scan number, then convert to minutes assuming the acquisition rate is 1 Hz (which is obviously a big assumption)
                            timeDiff = CSng(Math.Abs(scanList.FragScans(.SICStats.PeakScanIndexMax).ScanNumber - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanNumberMaxIntensity) / 60.0)
                        Else
                            timeDiff = Math.Abs(scanList.FragScans(.SICStats.PeakScanIndexMax).ScanTime - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity)
                        End If
                    Else
                        If scanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanTime < Single.Epsilon AndAlso
                           udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity < Single.Epsilon Then
                            ' Both elution times are 0; instead of computing the difference in scan time, compute the difference in scan number, then convert to minutes assuming the acquisition rate is 1 Hz (which is obviously a big assumption)
                            timeDiff = CSng(Math.Abs(scanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanNumber - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanNumberMaxIntensity) / 60.0)
                        Else
                            timeDiff = Math.Abs(scanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanTime - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity)
                        End If
                    End If

                    If timeDiff <= sicOptions.SimilarIonToleranceHalfWidthMinutes Then
                        ' Match is within m/z and time difference; see if the fragmentation spectra patterns are similar

                        Dim similarityScore = CompareFragSpectraForParentIons(scanList, objSpectraCache, originalIndex, matchOriginalIndex, binningOptions, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, dataImportUtilities)

                        If similarityScore > sicOptions.SpectrumSimilarityMinimum Then
                            ' Yes, the spectra are similar
                            ' Add this parent ion to udtUniqueMZList(uniqueMZListCount - 1)
                            With udtFindSimilarIonsData
                                AppendParentIonToUniqueMZEntry(scanList, matchOriginalIndex, .UniqueMZList(.UniqueMZListCount - 1), searchMZOffset)
                            End With
                            udtFindSimilarIonsData.IonUsed(matchOriginalIndex) = True
                            udtFindSimilarIonsData.IonInUseCount += 1
                        End If
                    End If
                End With

            Next
        End If

    End Sub

    Public Shared Function GetParentIonToleranceDa(sicOptions As clsSICOptions, parentIonMZ As Double) As Double
        Return GetParentIonToleranceDa(sicOptions, parentIonMZ, sicOptions.SICTolerance)
    End Function

    Public Shared Function GetParentIonToleranceDa(sicOptions As clsSICOptions, parentIonMZ As Double, parentIonTolerance As Double) As Double
        If sicOptions.SICToleranceIsPPM Then
            Return clsUtilities.PPMToMass(parentIonTolerance, parentIonMZ)
        Else
            Return parentIonTolerance
        End If
    End Function

    Private Sub InitializeUniqueMZListMatchIndices(
      ByRef udtUniqueMZList() As udtUniqueMZListType,
      startIndex As Integer,
      endIndex As Integer)

        Dim index As Integer

        For index = startIndex To endIndex
            ReDim udtUniqueMZList(index).MatchIndices(0)
            udtUniqueMZList(index).MatchCount = 0
        Next

    End Sub

End Class
