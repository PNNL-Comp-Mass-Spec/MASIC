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
        Public MaxIntensity As Single

        ''' <summary>
        ''' Largest peak intensity value of the similar parent ions
        ''' </summary>
        Public MaxPeakArea As Single

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
      intSurveyScanIndex As Integer,
      dblParentIonMZ As Double,
      intFragScanIndex As Integer,
      objSpectraCache As clsSpectraCache,
      sicOptions As clsSICOptions)

        AddUpdateParentIons(scanList, intSurveyScanIndex, dblParentIonMZ, 0, 0, intFragScanIndex, objSpectraCache, sicOptions)
    End Sub

    Public Sub AddUpdateParentIons(
      scanList As clsScanList,
      intSurveyScanIndex As Integer,
      dblParentIonMZ As Double,
      mrmInfo As clsMRMScanInfo,
      objSpectraCache As clsSpectraCache,
      sicOptions As clsSICOptions)

        Dim intMRMIndex As Integer
        Dim dblMRMDaughterMZ As Double
        Dim dblMRMToleranceHalfWidth As Double

        For intMRMIndex = 0 To mrmInfo.MRMMassCount - 1
            dblMRMDaughterMZ = mrmInfo.MRMMassList(intMRMIndex).CentralMass
            dblMRMToleranceHalfWidth = Math.Round((mrmInfo.MRMMassList(intMRMIndex).EndMass - mrmInfo.MRMMassList(intMRMIndex).StartMass) / 2, 6)
            If dblMRMToleranceHalfWidth < 0.001 Then
                dblMRMToleranceHalfWidth = 0.001
            End If

            AddUpdateParentIons(scanList, intSurveyScanIndex, dblParentIonMZ, dblMRMDaughterMZ, dblMRMToleranceHalfWidth, scanList.FragScans.Count - 1, objSpectraCache, sicOptions)
        Next

    End Sub

    Private Sub AddUpdateParentIons(
      scanList As clsScanList,
      intSurveyScanIndex As Integer,
      dblParentIonMZ As Double,
      dblMRMDaughterMZ As Double,
      dblMRMToleranceHalfWidth As Double,
      intFragScanIndex As Integer,
      objSpectraCache As clsSpectraCache,
      sicOptions As clsSICOptions)

        Const MINIMUM_TOLERANCE_PPM = 0.01
        Const MINIMUM_TOLERANCE_DA = 0.0001

        ' Checks to see if the parent ion specified by intSurveyScanIndex and dblParentIonMZ exists in .ParentIons()
        ' If dblMRMDaughterMZ is > 0, then also considers that value when determining uniqueness
        '
        ' If the parent ion entry already exists, then adds an entry to .FragScanIndices()
        ' If it does not exist, then adds a new entry to .ParentIons()
        ' Note that typically intFragScanIndex will equal scanList.FragScans.Count - 1

        ' If intSurveyScanIndex < 0 then the first scan(s) in the file occurred before we encountered a survey scan
        ' In this case, we cannot properly associate the fragmentation scan with a survey scan

        Dim intParentIonIndex As Integer

        Dim dblParentIonTolerance As Double
        Dim dblParentIonMZMatch As Double

        If sicOptions.SICToleranceIsPPM Then
            dblParentIonTolerance = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForPPM
            If dblParentIonTolerance < MINIMUM_TOLERANCE_PPM Then
                dblParentIonTolerance = MINIMUM_TOLERANCE_PPM
            End If
        Else
            dblParentIonTolerance = sicOptions.SICTolerance / sicOptions.CompressToleranceDivisorForDa
            If dblParentIonTolerance < MINIMUM_TOLERANCE_DA Then
                dblParentIonTolerance = MINIMUM_TOLERANCE_DA
            End If
        End If

        ' See if an entry exists yet in .ParentIons for the parent ion for this fragmentation scan
        Dim blnMatchFound = False

        If dblMRMDaughterMZ > 0 Then
            If sicOptions.SICToleranceIsPPM Then
                ' Force the tolerances to 0.01 m/z units
                dblParentIonTolerance = MINIMUM_TOLERANCE_PPM
            Else
                ' Force the tolerances to 0.01 m/z units
                dblParentIonTolerance = MINIMUM_TOLERANCE_DA
            End If
        End If

        If dblParentIonMZ > 0 Then

            Dim dblParentIonToleranceDa = GetParentIonToleranceDa(sicOptions, dblParentIonMZ, dblParentIonTolerance)

            For intParentIonIndex = scanList.ParentIonInfoCount - 1 To 0 Step -1
                If scanList.ParentIons(intParentIonIndex).SurveyScanIndex >= intSurveyScanIndex Then
                    If Math.Abs(scanList.ParentIons(intParentIonIndex).MZ - dblParentIonMZ) <= dblParentIonToleranceDa Then
                        If dblMRMDaughterMZ < Double.Epsilon OrElse
                          Math.Abs(scanList.ParentIons(intParentIonIndex).MRMDaughterMZ - dblMRMDaughterMZ) <= dblParentIonToleranceDa Then
                            blnMatchFound = True
                            Exit For
                        End If
                    End If
                Else
                    Exit For
                End If
            Next
        End If


        If Not blnMatchFound Then
            ' Add a new parent ion entry to .ParentIons(), but only if intSurveyScanIndex >= 0

            If intSurveyScanIndex >= 0 Then
                With scanList
                    If .ParentIonInfoCount >= .ParentIons.Length Then
                        ReDim Preserve .ParentIons(.ParentIonInfoCount + 100)
                        For intIndex = .ParentIonInfoCount To .ParentIons.Length - 1
                            ReDim .ParentIons(intIndex).FragScanIndices(0)
                        Next
                    End If

                    With .ParentIons(.ParentIonInfoCount)
                        .UpdateMz(dblParentIonMZ)

                        .SurveyScanIndex = intSurveyScanIndex

                        .FragScanIndices(0) = intFragScanIndex
                        .FragScanIndexCount = 1
                        .CustomSICPeak = False

                        .MRMDaughterMZ = dblMRMDaughterMZ
                        .MRMToleranceHalfWidth = dblMRMToleranceHalfWidth
                    End With
                    .ParentIons(.ParentIonInfoCount).OptimalPeakApexScanNumber = .SurveyScans(intSurveyScanIndex).ScanNumber        ' Was: .FragScans(intFragScanIndex).ScanNumber
                    .ParentIons(.ParentIonInfoCount).PeakApexOverrideParentIonIndex = -1
                    .FragScans(intFragScanIndex).FragScanInfo.ParentIonInfoIndex = .ParentIonInfoCount

                    ' Look for .MZ in the survey scan, using a tolerance of dblParentIonTolerance
                    ' If found, then update the mass to the matched ion
                    ' This is done to determine the parent ion mass more precisely
                    If sicOptions.RefineReportedParentIonMZ Then
                        If FindClosestMZ(objSpectraCache, .SurveyScans, intSurveyScanIndex, dblParentIonMZ, dblParentIonTolerance, dblParentIonMZMatch) Then
                            .ParentIons(.ParentIonInfoCount).UpdateMz(dblParentIonMZMatch)
                        End If
                    End If

                    .ParentIonInfoCount += 1
                End With
            End If
        Else
            ' Add a new entry to .FragScanIndices() for the matching parent ion
            ' However, do not add a new entry if this is an MRM scan

            If dblMRMDaughterMZ < Double.Epsilon Then
                With scanList
                    With .ParentIons(intParentIonIndex)
                        ReDim Preserve .FragScanIndices(.FragScanIndexCount)
                        .FragScanIndices(.FragScanIndexCount) = intFragScanIndex
                        .FragScanIndexCount += 1
                    End With
                    .FragScans(intFragScanIndex).FragScanInfo.ParentIonInfoIndex = intParentIonIndex
                End With
            End If
        End If

    End Sub

    Private Sub AppendParentIonToUniqueMZEntry(
      scanList As clsScanList,
      intParentIonIndex As Integer,
      ByRef udtMZListEntry As udtUniqueMZListType,
      dblSearchMZOffset As Double)

        With scanList.ParentIons(intParentIonIndex)
            If udtMZListEntry.MatchCount = 0 Then
                udtMZListEntry.MZAvg = .MZ - dblSearchMZOffset
                udtMZListEntry.MatchCount = 1

                ' Note that .MatchIndices() was initialized in InitializeUniqueMZListMatchIndices()
                udtMZListEntry.MatchIndices(0) = intParentIonIndex
            Else
                ' Update the average MZ: NewAvg = (OldAvg * OldCount + NewValue) / NewCount
                udtMZListEntry.MZAvg = (udtMZListEntry.MZAvg * udtMZListEntry.MatchCount + (.MZ - dblSearchMZOffset)) / (udtMZListEntry.MatchCount + 1)

                ReDim Preserve udtMZListEntry.MatchIndices(udtMZListEntry.MatchCount)
                udtMZListEntry.MatchIndices(udtMZListEntry.MatchCount) = intParentIonIndex
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
                    udtMZListEntry.ParentIonIndexMaxIntensity = intParentIonIndex
                End If

                If .Peak.Area > udtMZListEntry.MaxPeakArea OrElse udtMZListEntry.MatchCount = 1 Then
                    udtMZListEntry.MaxPeakArea = .Peak.Area
                    udtMZListEntry.ParentIonIndexMaxPeakArea = intParentIonIndex
                End If
            End With

        End With

    End Sub

    Private Function CompareFragSpectraForParentIons(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      intParentIonIndex1 As Integer,
      intParentIonIndex2 As Integer,
      binningOptions As clsBinningOptions,
      noiseThresholdOptions As MASICPeakFinder.clsBaselineNoiseOptions,
      dataImportUtilities As DataInput.clsDataImport) As Single

        ' Compare the fragmentation spectra for the two parent ions
        ' Returns the highest similarity score (ranging from 0 to 1)
        ' Returns 0 if no similarity or no spectra to compare
        ' Returns -1 if an error

        Dim intFragIndex1, intFragIndex2 As Integer
        Dim intFragSpectrumIndex1, intFragSpectrumIndex2 As Integer
        Dim intPoolIndex1, intPoolIndex2 As Integer
        Dim sngSimilarityScore, sngHighestSimilarityScore As Single

        Try
            If scanList.ParentIons(intParentIonIndex1).CustomSICPeak OrElse scanList.ParentIons(intParentIonIndex2).CustomSICPeak Then
                ' Custom SIC values do not have fragmentation spectra; nothing to compare
                sngHighestSimilarityScore = 0
            ElseIf scanList.ParentIons(intParentIonIndex1).MRMDaughterMZ > 0 OrElse scanList.ParentIons(intParentIonIndex2).MRMDaughterMZ > 0 Then
                ' MRM Spectra should not be compared
                sngHighestSimilarityScore = 0
            Else
                sngHighestSimilarityScore = 0
                For intFragIndex1 = 0 To scanList.ParentIons(intParentIonIndex1).FragScanIndexCount - 1
                    intFragSpectrumIndex1 = scanList.ParentIons(intParentIonIndex1).FragScanIndices(intFragIndex1)

                    If Not objSpectraCache.ValidateSpectrumInPool(scanList.FragScans(intFragSpectrumIndex1).ScanNumber, intPoolIndex1) Then
                        SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                        Return -1
                    End If

                    If Not DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD Then
                        dataImportUtilities.DiscardDataBelowNoiseThreshold(objSpectraCache.SpectraPool(intPoolIndex1), scanList.FragScans(intFragSpectrumIndex1).BaselineNoiseStats.NoiseLevel, 0, 0, noiseThresholdOptions)
                    End If

                    For intFragIndex2 = 0 To scanList.ParentIons(intParentIonIndex2).FragScanIndexCount - 1

                        intFragSpectrumIndex2 = scanList.ParentIons(intParentIonIndex2).FragScanIndices(intFragIndex2)

                        If Not objSpectraCache.ValidateSpectrumInPool(scanList.FragScans(intFragSpectrumIndex2).ScanNumber, intPoolIndex2) Then
                            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                            Return -1
                        End If

                        If Not DISCARD_LOW_INTENSITY_MSMS_DATA_ON_LOAD Then
                            dataImportUtilities.DiscardDataBelowNoiseThreshold(objSpectraCache.SpectraPool(intPoolIndex2), scanList.FragScans(intFragSpectrumIndex2).BaselineNoiseStats.NoiseLevel, 0, 0, noiseThresholdOptions)
                        End If

                        sngSimilarityScore = CompareSpectra(objSpectraCache.SpectraPool(intPoolIndex1), objSpectraCache.SpectraPool(intPoolIndex2), binningOptions)

                        If sngSimilarityScore > sngHighestSimilarityScore Then
                            sngHighestSimilarityScore = sngSimilarityScore
                        End If
                    Next
                Next

            End If
        Catch ex As Exception
            ReportError("Error in CompareFragSpectraForParentIons", ex)
            Return -1
        End Try

        Return sngHighestSimilarityScore

    End Function

    Private Function CompareSpectra(
      fragSpectrum1 As clsMSSpectrum,
      fragSpectrum2 As clsMSSpectrum,
      binningOptions As clsBinningOptions,
      Optional blnConsiderOffsetBinnedData As Boolean = True) As Single

        ' Compares the two spectra and returns a similarity score (ranging from 0 to 1)
        ' Perfect match is 1; no similarity is 0
        ' Note that both the standard binned data and the offset binned data are compared
        ' If blnConsiderOffsetBinnedData = True, then the larger of the two scores is returned
        '  similarity scores is returned
        '
        ' If an error, returns -1

        Dim udtBinnedSpectrum1 = New udtBinnedDataType
        Dim udtBinnedSpectrum2 = New udtBinnedDataType

        Dim blnSuccess As Boolean

        Try

            Dim objCorrelate = New clsCorrelation(binningOptions)
            RegisterEvents(objCorrelate)

            Const eCorrelationMethod = clsCorrelation.cmCorrelationMethodConstants.Pearson

            ' Bin the data in the first spectrum
            blnSuccess = CompareSpectraBinData(objCorrelate, fragSpectrum1, udtBinnedSpectrum1)
            If Not blnSuccess Then Return -1

            ' Bin the data in the second spectrum
            blnSuccess = CompareSpectraBinData(objCorrelate, fragSpectrum2, udtBinnedSpectrum2)
            If Not blnSuccess Then Return -1

            ' Now compare the binned spectra
            ' Similarity will be 0 if either intance of BinnedIntensities has fewer than 5 data points
            Dim sngSimilarity1 = objCorrelate.Correlate(udtBinnedSpectrum1.BinnedIntensities, udtBinnedSpectrum2.BinnedIntensities, eCorrelationMethod)

            If Not blnConsiderOffsetBinnedData Then
                Return sngSimilarity1
            End If

            Dim sngSimilarity2 = objCorrelate.Correlate(udtBinnedSpectrum1.BinnedIntensitiesOffset, udtBinnedSpectrum2.BinnedIntensitiesOffset, eCorrelationMethod)
            Return Math.Max(sngSimilarity1, sngSimilarity2)

        Catch ex As Exception
            ReportError("CompareSectra: " & ex.Message, ex)
            Return -1
        End Try

    End Function

    Private Function CompareSpectraBinData(
      objCorrelate As clsCorrelation,
      fragSpectrum As clsMSSpectrum,
      ByRef udtBinnedSpectrum As udtBinnedDataType) As Boolean

        Dim intIndex As Integer
        Dim sngXData As Single()
        Dim sngYData As Single()


        ' Make a copy of the data, excluding any Reporter Ion data
        Dim filteredDataCount = 0
        ReDim sngXData(fragSpectrum.IonCount - 1)
        ReDim sngYData(fragSpectrum.IonCount - 1)

        For intIndex = 0 To fragSpectrum.IonCount - 1
            If Not clsUtilities.CheckPointInMZIgnoreRange(fragSpectrum.IonsMZ(intIndex),
                                                          mReporterIons.MZIntensityFilterIgnoreRangeStart,
                                                          mReporterIons.MZIntensityFilterIgnoreRangeEnd) Then
                sngXData(filteredDataCount) = CSng(fragSpectrum.IonsMZ(intIndex))
                sngYData(filteredDataCount) = fragSpectrum.IonsIntensity(intIndex)
                filteredDataCount += 1
            End If
        Next

        If filteredDataCount > 0 Then
            ReDim Preserve sngXData(filteredDataCount - 1)
            ReDim Preserve sngYData(filteredDataCount - 1)
        Else
            ReDim Preserve sngXData(0)
            ReDim Preserve sngYData(0)
        End If

        udtBinnedSpectrum.BinnedDataStartX = objCorrelate.BinStartX
        udtBinnedSpectrum.BinSize = objCorrelate.BinSize

        udtBinnedSpectrum.BinnedIntensities = Nothing
        udtBinnedSpectrum.BinnedIntensitiesOffset = Nothing

        ' Note that the data in sngXData and sngYData should have already been filtered to discard data points below the noise threshold intensity
        Dim blnSuccess = objCorrelate.BinData(sngXData, sngYData, udtBinnedSpectrum.BinnedIntensities, udtBinnedSpectrum.BinnedIntensitiesOffset, udtBinnedSpectrum.BinCount)

        Return blnSuccess

    End Function

    Private Function FindClosestMZ(
      objSpectraCache As clsSpectraCache,
      scanList As IList(Of clsScanInfo),
      intSpectrumIndex As Integer,
      dblSearchMZ As Double,
      dblToleranceMZ As Double,
      <Out()> ByRef dblBestMatchMZ As Double) As Boolean

        Dim intPoolIndex As Integer
        Dim blnSuccess As Boolean

        dblBestMatchMZ = 0

        Try
            If scanList(intSpectrumIndex).IonCount = 0 And scanList(intSpectrumIndex).IonCountRaw = 0 Then
                ' No data in this spectrum
                blnSuccess = False
            Else
                If Not objSpectraCache.ValidateSpectrumInPool(scanList(intSpectrumIndex).ScanNumber, intPoolIndex) Then
                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                    blnSuccess = False
                Else
                    With objSpectraCache.SpectraPool(intPoolIndex)
                        blnSuccess = FindClosestMZ(.IonsMZ, .IonCount, dblSearchMZ, dblToleranceMZ, dblBestMatchMZ)
                    End With
                End If

            End If
        Catch ex As Exception
            ReportError("Error in FindClosestMZ", ex)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function FindClosestMZ(
      dblMZList() As Double,
      intIonCount As Integer,
      dblSearchMZ As Double,
      dblToleranceMZ As Double,
      <Out()> ByRef dblBestMatchMZ As Double) As Boolean

        ' Searches dblMZList for the closest match to dblSearchMZ within tolerance dblBestMatchMZ
        ' If a match is found, then updates dblBestMatchMZ to the m/z of the match and returns True

        Dim intDataIndex As Integer
        Dim intClosestMatchIndex As Integer
        Dim dblMassDifferenceAbs As Double
        Dim dblBestMassDifferenceAbs As Double

        Try
            intClosestMatchIndex = -1
            For intDataIndex = 0 To intIonCount - 1
                dblMassDifferenceAbs = Math.Abs(dblMZList(intDataIndex) - dblSearchMZ)
                If dblMassDifferenceAbs <= dblToleranceMZ Then
                    If intClosestMatchIndex < 0 OrElse dblMassDifferenceAbs < dblBestMassDifferenceAbs Then
                        intClosestMatchIndex = intDataIndex
                        dblBestMassDifferenceAbs = dblMassDifferenceAbs
                    End If
                End If
            Next

        Catch ex As Exception
            ReportError("Error in FindClosestMZ", ex)
            intClosestMatchIndex = -1
        End Try

        If intClosestMatchIndex >= 0 Then
            dblBestMatchMZ = dblMZList(intClosestMatchIndex)
            Return True
        Else
            dblBestMatchMZ = 0
            Return False
        End If

    End Function

    Public Function FindSimilarParentIons(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      masicOptions As clsMASICOptions,
      dataImportUtilities As DataInput.clsDataImport,
      ByRef intIonUpdateCount As Integer) As Boolean

        ' Look for parent ions that have similar m/z values and are nearby one another in time
        ' For the groups of similar ions, assign the scan number of the highest intensity parent ion to the other similar parent ions

        Dim blnSuccess As Boolean

        Try
            intIonUpdateCount = 0

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

            Dim intFindSimilarIonsDataCount As Integer
            Dim udtFindSimilarIonsData As udtFindSimilarIonsDataType

            ' Original m/z values, rounded to 2 decimal places
            Dim dblMZList() As Double
            Dim intIntensityPointerArray() As Integer
            Dim sngIntensityList() As Single

            ' Populate udtFindSimilarIonsData.MZPointerArray and dblMZList, plus intIntensityPointerArray and sngIntensityList()
            ReDim udtFindSimilarIonsData.MZPointerArray(scanList.ParentIonInfoCount - 1)
            ReDim udtFindSimilarIonsData.IonUsed(scanList.ParentIonInfoCount - 1)

            ReDim dblMZList(scanList.ParentIonInfoCount - 1)
            ReDim intIntensityPointerArray(scanList.ParentIonInfoCount - 1)
            ReDim sngIntensityList(scanList.ParentIonInfoCount - 1)

            Dim intParentIonIndex As Integer
            Dim intIonInUseCountOriginal As Integer

            intFindSimilarIonsDataCount = 0
            For intParentIonIndex = 0 To scanList.ParentIonInfoCount - 1

                Dim blnIncludeParentIon As Boolean

                If scanList.ParentIons(intParentIonIndex).MRMDaughterMZ > 0 Then
                    blnIncludeParentIon = False
                Else
                    If masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                        blnIncludeParentIon = scanList.ParentIons(intParentIonIndex).CustomSICPeak
                    Else
                        blnIncludeParentIon = True
                    End If
                End If

                If blnIncludeParentIon Then
                    udtFindSimilarIonsData.MZPointerArray(intFindSimilarIonsDataCount) = intParentIonIndex
                    dblMZList(intFindSimilarIonsDataCount) = Math.Round(scanList.ParentIons(intParentIonIndex).MZ, 2)

                    intIntensityPointerArray(intFindSimilarIonsDataCount) = intParentIonIndex
                    sngIntensityList(intFindSimilarIonsDataCount) = scanList.ParentIons(intParentIonIndex).SICStats.Peak.MaxIntensityValue
                    intFindSimilarIonsDataCount += 1
                End If
            Next

            If udtFindSimilarIonsData.MZPointerArray.Length <> intFindSimilarIonsDataCount AndAlso intFindSimilarIonsDataCount > 0 Then
                ReDim Preserve udtFindSimilarIonsData.MZPointerArray(intFindSimilarIonsDataCount - 1)
                ReDim Preserve dblMZList(intFindSimilarIonsDataCount - 1)
                ReDim Preserve intIntensityPointerArray(intFindSimilarIonsDataCount - 1)
                ReDim Preserve sngIntensityList(intFindSimilarIonsDataCount - 1)
            End If

            If intFindSimilarIonsDataCount = 0 Then
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
            Array.Sort(dblMZList, udtFindSimilarIonsData.MZPointerArray)

            ReportMessage("FindSimilarParentIons: Populate objSearchRange")

            ' Populate objSearchRange
            Dim objSearchRange = New clsSearchRange()

            ' Set to false to prevent sorting the input array when calling .FillWithData (saves memory)
            ' Array was already above
            objSearchRange.UsePointerIndexArray = False

            blnSuccess = objSearchRange.FillWithData(dblMZList)

            ReportMessage("FindSimilarParentIons: Sort the intensity arrays")

            ' Sort the Intensity arrays
            Array.Sort(sngIntensityList, intIntensityPointerArray)

            ' Reverse the order of intIntensityPointerArray so that it is ordered from the most intense to the least intense ion
            ' Note: We don't really need to reverse sngIntensityList since we're done using it, but
            ' it doesn't take long, it won't hurt, and it will keep sngIntensityList sync'd with intIntensityPointerArray
            Array.Reverse(intIntensityPointerArray)
            Array.Reverse(sngIntensityList)


            ' Initialize udtUniqueMZList
            ' Pre-reserve enough space for intFindSimilarIonsDataCount entries to avoid repeated use of Redim Preserve
            udtFindSimilarIonsData.UniqueMZListCount = 0
            ReDim udtFindSimilarIonsData.UniqueMZList(intFindSimilarIonsDataCount - 1)

            ' Initialize the .UniqueMZList().MatchIndices() arrays
            InitializeUniqueMZListMatchIndices(udtFindSimilarIonsData.UniqueMZList, 0, intFindSimilarIonsDataCount - 1)

            ReportMessage("FindSimilarParentIons: Look for similar parent ions by using m/z and scan")
            Dim dtLastLogTime = DateTime.UtcNow

            ' Look for similar parent ions by using m/z and scan
            ' Step through the ions by decreasing intensity
            intParentIonIndex = 0
            Do
                Dim intOriginalIndex = intIntensityPointerArray(intParentIonIndex)
                If udtFindSimilarIonsData.IonUsed(intOriginalIndex) Then
                    ' Parent ion was already used; move onto the next one
                    intParentIonIndex += 1
                Else
                    With udtFindSimilarIonsData
                        If .UniqueMZListCount >= udtFindSimilarIonsData.UniqueMZList.Length Then
                            ReDim Preserve .UniqueMZList(.UniqueMZListCount + 100)

                            ' Initialize the .UniqueMZList().MatchIndices() arrays
                            InitializeUniqueMZListMatchIndices(udtFindSimilarIonsData.UniqueMZList, .UniqueMZListCount, .UniqueMZList.Length - 1)

                        End If
                        AppendParentIonToUniqueMZEntry(scanList, intOriginalIndex, .UniqueMZList(.UniqueMZListCount), 0)
                        .UniqueMZListCount += 1

                        .IonUsed(intOriginalIndex) = True
                        .IonInUseCount = 1
                    End With

                    ' Look for other parent ions with m/z values in tolerance (must be within mass tolerance and scan tolerance)
                    ' If new values are added, then repeat the search using the updated udtUniqueMZList().MZAvg value
                    Do
                        intIonInUseCountOriginal = udtFindSimilarIonsData.IonInUseCount
                        Dim dblCurrentMZ = udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).MZAvg

                        If dblCurrentMZ <= 0 Then Continue Do

                        FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0, intOriginalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        ' Look for similar 1+ spaced m/z values
                        FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 1, intOriginalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -1, intOriginalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        ' Look for similar 2+ spaced m/z values
                        FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0.5, intOriginalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -0.5, intOriginalIndex,
                                                  scanList, udtFindSimilarIonsData,
                                                  masicOptions, dataImportUtilities, objSearchRange)

                        Dim parentIonToleranceDa = GetParentIonToleranceDa(masicOptions.SICOptions, dblCurrentMZ)

                        If parentIonToleranceDa <= 0.25 AndAlso masicOptions.SICOptions.SimilarIonMZToleranceHalfWidth <= 0.15 Then
                            ' Also look for similar 3+ spaced m/z values
                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0.666, intOriginalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)

                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, 0.333, intOriginalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)

                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -0.333, intOriginalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)

                            FindSimilarParentIonsWork(objSpectraCache, dblCurrentMZ, -0.666, intOriginalIndex,
                                                      scanList, udtFindSimilarIonsData,
                                                      masicOptions, dataImportUtilities, objSearchRange)
                        End If


                    Loop While udtFindSimilarIonsData.IonInUseCount > intIonInUseCountOriginal

                    intParentIonIndex += 1
                End If

                If intFindSimilarIonsDataCount > 1 Then
                    If intParentIonIndex Mod 100 = 0 Then
                        UpdateProgress(CShort(intParentIonIndex / (intFindSimilarIonsDataCount - 1) * 100))
                    End If
                Else
                    UpdateProgress(1)
                End If

                UpdateCacheStats(objSpectraCache)
                If masicOptions.AbortProcessing Then
                    scanList.ProcessingIncomplete = True
                    Exit Do
                End If

                If intParentIonIndex Mod 100 = 0 Then
                    If DateTime.UtcNow.Subtract(dtLastLogTime).TotalSeconds >= 10 OrElse intParentIonIndex Mod 500 = 0 Then
                        ReportMessage("Parent Ion Index: " & intParentIonIndex.ToString())
                        Console.Write(".")
                        dtLastLogTime = DateTime.UtcNow
                    End If
                End If

            Loop While intParentIonIndex < intFindSimilarIonsDataCount

            Console.WriteLine()

            ' Shrink the .UniqueMZList array to the appropriate length
            ReDim Preserve udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1)

            ReportMessage("FindSimilarParentIons: Update the scan numbers for the unique ions")

            ' Update the optimal peak apex scan numbers for the unique ions
            intIonUpdateCount = 0
            For intUniqueMZIndex = 0 To udtFindSimilarIonsData.UniqueMZListCount - 1
                With udtFindSimilarIonsData.UniqueMZList(intUniqueMZIndex)
                    For intMatchIndex = 0 To .MatchCount - 1
                        intParentIonIndex = .MatchIndices(intMatchIndex)

                        If scanList.ParentIons(intParentIonIndex).MZ > 0 Then
                            If scanList.ParentIons(intParentIonIndex).OptimalPeakApexScanNumber <> .ScanNumberMaxIntensity Then
                                intIonUpdateCount += 1
                                scanList.ParentIons(intParentIonIndex).OptimalPeakApexScanNumber = .ScanNumberMaxIntensity
                                scanList.ParentIons(intParentIonIndex).PeakApexOverrideParentIonIndex = .ParentIonIndexMaxIntensity
                            End If
                        End If
                    Next
                End With
            Next

            blnSuccess = True

        Catch ex As Exception
            ReportError("Error in FindSimilarParentIons", ex, eMasicErrorCodes.FindSimilarParentIonsError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function

    Private Sub FindSimilarParentIonsWork(
      objSpectraCache As clsSpectraCache,
      dblSearchMZ As Double,
      dblSearchMZOffset As Double,
      intOriginalIndex As Integer,
      scanList As clsScanList,
      ByRef udtFindSimilarIonsData As udtFindSimilarIonsDataType,
      masicOptions As clsMASICOptions,
      dataImportUtilities As DataInput.clsDataImport,
      objSearchRange As clsSearchRange)

        Dim intIndexFirst As Integer
        Dim intIndexLast As Integer

        Dim sicOptions = masicOptions.SICOptions
        Dim binningOptions = masicOptions.BinningOptions

        If objSearchRange.FindValueRange(dblSearchMZ + dblSearchMZOffset, sicOptions.SimilarIonMZToleranceHalfWidth, intIndexFirst, intIndexLast) Then

            For intMatchIndex = intIndexFirst To intIndexLast
                ' See if the matches are unused and within the scan tolerance
                Dim intMatchOriginalIndex = udtFindSimilarIonsData.MZPointerArray(intMatchIndex)

                If udtFindSimilarIonsData.IonUsed(intMatchOriginalIndex) Then Continue For

                Dim sngTimeDiff As Single

                With scanList.ParentIons(intMatchOriginalIndex)

                    If .SICStats.ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan Then
                        If scanList.FragScans(.SICStats.PeakScanIndexMax).ScanTime < Single.Epsilon AndAlso
                           udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity < Single.Epsilon Then
                            ' Both elution times are 0; instead of computing the difference in scan time, compute the difference in scan number, then convert to minutes assuming the acquisition rate is 1 Hz (which is obviously a big assumption)
                            sngTimeDiff = CSng(Math.Abs(scanList.FragScans(.SICStats.PeakScanIndexMax).ScanNumber - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanNumberMaxIntensity) / 60.0)
                        Else
                            sngTimeDiff = Math.Abs(scanList.FragScans(.SICStats.PeakScanIndexMax).ScanTime - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity)
                        End If
                    Else
                        If scanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanTime < Single.Epsilon AndAlso
                           udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity < Single.Epsilon Then
                            ' Both elution times are 0; instead of computing the difference in scan time, compute the difference in scan number, then convert to minutes assuming the acquisition rate is 1 Hz (which is obviously a big assumption)
                            sngTimeDiff = CSng(Math.Abs(scanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanNumber - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanNumberMaxIntensity) / 60.0)
                        Else
                            sngTimeDiff = Math.Abs(scanList.SurveyScans(.SICStats.PeakScanIndexMax).ScanTime - udtFindSimilarIonsData.UniqueMZList(udtFindSimilarIonsData.UniqueMZListCount - 1).ScanTimeMaxIntensity)
                        End If
                    End If

                    If sngTimeDiff <= sicOptions.SimilarIonToleranceHalfWidthMinutes Then
                        ' Match is within m/z and time difference; see if the fragmentation spectra patterns are similar

                        Dim similarityScore = CompareFragSpectraForParentIons(scanList, objSpectraCache, intOriginalIndex, intMatchOriginalIndex, binningOptions, sicOptions.SICPeakFinderOptions.MassSpectraNoiseThresholdOptions, dataImportUtilities)

                        If similarityScore > sicOptions.SpectrumSimilarityMinimum Then
                            ' Yes, the spectra are similar
                            ' Add this parent ion to udtUniqueMZList(intUniqueMZListCount - 1)
                            With udtFindSimilarIonsData
                                AppendParentIonToUniqueMZEntry(scanList, intMatchOriginalIndex, .UniqueMZList(.UniqueMZListCount - 1), dblSearchMZOffset)
                            End With
                            udtFindSimilarIonsData.IonUsed(intMatchOriginalIndex) = True
                            udtFindSimilarIonsData.IonInUseCount += 1
                        End If
                    End If
                End With

            Next
        End If

    End Sub

    Public Shared Function GetParentIonToleranceDa(sicOptions As clsSICOptions, dblParentIonMZ As Double) As Double
        Return GetParentIonToleranceDa(sicOptions, dblParentIonMZ, sicOptions.SICTolerance)
    End Function

    Public Shared Function GetParentIonToleranceDa(sicOptions As clsSICOptions, dblParentIonMZ As Double, dblParentIonTolerance As Double) As Double
        If sicOptions.SICToleranceIsPPM Then
            Return clsUtilities.PPMToMass(dblParentIonTolerance, dblParentIonMZ)
        Else
            Return dblParentIonTolerance
        End If
    End Function

    Private Sub InitializeUniqueMZListMatchIndices(
      ByRef udtUniqueMZList() As udtUniqueMZListType,
      intStartIndex As Integer,
      intEndIndex As Integer)

        Dim intIndex As Integer

        For intIndex = intStartIndex To intEndIndex
            ReDim udtUniqueMZList(intIndex).MatchIndices(0)
            udtUniqueMZList(intIndex).MatchCount = 0
        Next

    End Sub

End Class
