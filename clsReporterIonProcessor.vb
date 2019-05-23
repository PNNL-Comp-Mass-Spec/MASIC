Imports MASIC.clsMASIC
Imports MASIC.DataOutput
Imports PRISM
Imports ThermoRawFileReader

Public Class clsReporterIonProcessor
    Inherits clsMasicEventNotifier

#Region "Classwide variables"
    Private ReadOnly mOptions As clsMASICOptions
#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="masicOptions"></param>
    Public Sub New(masicOptions As clsMASICOptions)
        mOptions = masicOptions
    End Sub

    ''' <summary>
    ''' Looks for the reporter ion peaks using FindReporterIonsWork
    ''' </summary>
    ''' <param name="scanList"></param>
    ''' <param name="spectraCache"></param>
    ''' <param name="inputFilePathFull">Full path to the input file</param>
    ''' <param name="outputDirectoryPath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function FindReporterIons(
      scanList As clsScanList,
      spectraCache As clsSpectraCache,
      inputFilePathFull As String,
      outputDirectoryPath As String) As Boolean

        Const cColDelimiter As Char = ControlChars.Tab

        Dim outputFilePath = "??"

        Try

            ' Use Xraw to read the .Raw files
            Dim xcaliburAccessor = New XRawFileIO()
            RegisterEvents(xcaliburAccessor)

            Dim includeFtmsColumns = False

            If inputFilePathFull.ToUpper().EndsWith(DataInput.clsDataImport.FINNIGAN_RAW_FILE_EXTENSION.ToUpper()) Then

                ' Processing a thermo .Raw file
                ' Check whether any of the frag scans has IsFTMS true
                For masterOrderIndex = 0 To scanList.MasterScanOrderCount - 1
                    Dim scanPointer = scanList.MasterScanOrder(masterOrderIndex).ScanIndexPointer
                    If scanList.MasterScanOrder(masterOrderIndex).ScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                        ' Skip survey scans
                        Continue For
                    End If

                    If scanList.FragScans(scanPointer).IsFTMS Then
                        includeFtmsColumns = True
                        Exit For
                    End If
                Next

                If includeFtmsColumns Then
                    xcaliburAccessor.OpenRawFile(inputFilePathFull)
                End If

            End If

            If mOptions.ReporterIons.ReporterIonList.Count = 0 Then
                ' No reporter ions defined; default to ITraq
                mOptions.ReporterIons.SetReporterIonMassMode(clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ)
            End If

            ' Populate array reporterIons, which we will sort by m/z
            Dim reporterIons As clsReporterIonInfo()
            ReDim reporterIons(mOptions.ReporterIons.ReporterIonList.Count - 1)

            Dim reporterIonIndex = 0
            For Each reporterIon In mOptions.ReporterIons.ReporterIonList
                reporterIons(reporterIonIndex) = reporterIon
                reporterIonIndex += 1
            Next

            Array.Sort(reporterIons, New clsReportIonInfoComparer())

            outputFilePath = clsDataOutput.ConstructOutputFilePath(
                Path.GetFileName(inputFilePathFull),
                outputDirectoryPath,
                clsDataOutput.eOutputFileTypeConstants.ReporterIonsFile)

            Using writer = New StreamWriter(outputFilePath)

                ' Write the file headers
                Dim reporterIonMZsUnique = New SortedSet(Of String)
                Dim headerColumns = New List(Of String) From {
                    "Dataset",
                    "ScanNumber",
                    "Collision Mode",
                    "ParentIonMZ",
                    "BasePeakIntensity",
                    "BasePeakMZ",
                    "ReporterIonIntensityMax"
                }

                Dim obsMZHeaders = New List(Of String)
                Dim uncorrectedIntensityHeaders = New List(Of String)
                Dim ftmsSignalToNoise = New List(Of String)
                Dim ftmsResolution = New List(Of String)
                ' Dim ftmsLabelDataMz = New List(Of String)

                Dim saveUncorrectedIntensities As Boolean =
                    mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection AndAlso mOptions.ReporterIons.ReporterIonSaveUncorrectedIntensities

                Dim dataAggregation = New clsDataAggregation()
                RegisterEvents(dataAggregation)

                For Each reporterIon In reporterIons

                    If Not reporterIon.ContaminantIon OrElse saveUncorrectedIntensities Then
                        ' Construct the reporter ion intensity header
                        ' We skip contaminant ions, unless saveUncorrectedIntensities is True, then we include them

                        Dim mzValue As String
                        If (mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ OrElse
                            mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ) Then
                            mzValue = reporterIon.MZ.ToString("#0.000")
                        Else
                            mzValue = CInt(reporterIon.MZ).ToString()
                        End If

                        If reporterIonMZsUnique.Contains(mzValue) Then
                            ' Uniquify the m/z value
                            mzValue &= "_" & reporterIonIndex.ToString()
                        End If

                        Try
                            reporterIonMZsUnique.Add(mzValue)
                        Catch ex As Exception
                            ' Error updating the SortedSet;
                            ' this shouldn't happen based on the .ContainsKey test above
                        End Try

                        ' Append the reporter ion intensity title to the headers
                        headerColumns.Add("Ion_" & mzValue)

                        ' This string will only be included in the header line if mOptions.ReporterIons.ReporterIonSaveObservedMasses is true
                        obsMZHeaders.Add("Ion_" & mzValue & "_ObsMZ")

                        ' This string will be included in the header line if saveUncorrectedIntensities is true
                        uncorrectedIntensityHeaders.Add("Ion_" & mzValue & "_OriginalIntensity")

                        ' This string will be included in the header line if includeFtmsColumns is true
                        ftmsSignalToNoise.Add("Ion_" & mzValue & "_SignalToNoise")
                        ftmsResolution.Add("Ion_" & mzValue & "_Resolution")

                        ' Uncomment to include the label data m/z value in the _ReporterIons.txt file
                        '' This string will only be included in the header line if mOptions.ReporterIons.ReporterIonSaveObservedMasses is true
                        'ftmsLabelDataMz.Add("Ion_" & mzValue & "_LabelDataMZ")
                    End If

                Next

                headerColumns.Add("Weighted Avg Pct Intensity Correction")

                If mOptions.ReporterIons.ReporterIonSaveObservedMasses Then
                    headerColumns.AddRange(obsMZHeaders)
                End If

                If saveUncorrectedIntensities Then
                    headerColumns.AddRange(uncorrectedIntensityHeaders)
                End If

                If includeFtmsColumns Then
                    headerColumns.AddRange(ftmsSignalToNoise)
                    headerColumns.AddRange(ftmsResolution)
                    ' Uncomment to include the label data m/z value in the _ReporterIons.txt file
                    'If mOptions.ReporterIons.ReporterIonSaveObservedMasses Then
                    '    headerColumns.AddRange(ftmsLabelDataMz)
                    'End If
                End If

                ' Write the headers to the output file, separated by tabs
                writer.WriteLine(String.Join(cColDelimiter, headerColumns))

                UpdateProgress(0, "Searching for reporter ions")

                For masterOrderIndex = 0 To scanList.MasterScanOrderCount - 1
                    Dim scanPointer = scanList.MasterScanOrder(masterOrderIndex).ScanIndexPointer
                    If scanList.MasterScanOrder(masterOrderIndex).ScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                        ' Skip Survey Scans
                        Continue For
                    End If

                    FindReporterIonsWork(
                        xcaliburAccessor,
                        dataAggregation,
                        includeFtmsColumns,
                        mOptions.SICOptions,
                        scanList,
                        spectraCache,
                        scanList.FragScans(scanPointer),
                        writer,
                        reporterIons,
                        cColDelimiter,
                        saveUncorrectedIntensities,
                        mOptions.ReporterIons.ReporterIonSaveObservedMasses)

                    If scanList.MasterScanOrderCount > 1 Then
                        UpdateProgress(CShort(masterOrderIndex / (scanList.MasterScanOrderCount - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(spectraCache)
                    If mOptions.AbortProcessing Then
                        Exit For
                    End If

                Next

            End Using

            If includeFtmsColumns Then
                ' Close the handle to the data file
                xcaliburAccessor.CloseRawFile()
            End If

            Return True

        Catch ex As Exception
            ReportError("Error writing the reporter ions to: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Looks for the reporter ion m/z values, +/- a tolerance
    ''' Calls AggregateIonsInRange with returnMax = True, meaning we're reporting the maximum ion abundance for each reporter ion m/z
    ''' </summary>
    ''' <param name="xcaliburAccessor"></param>
    ''' <param name="dataAggregation"></param>
    ''' <param name="includeFtmsColumns"></param>
    ''' <param name="sicOptions"></param>
    ''' <param name="scanList"></param>
    ''' <param name="spectraCache"></param>
    ''' <param name="currentScan"></param>
    ''' <param name="writer"></param>
    ''' <param name="reporterIons"></param>
    ''' <param name="cColDelimiter"></param>
    ''' <param name="saveUncorrectedIntensities"></param>
    ''' <param name="saveObservedMasses"></param>
    ''' <remarks></remarks>
    Private Sub FindReporterIonsWork(
      xcaliburAccessor As XRawFileIO,
      dataAggregation As clsDataAggregation,
      includeFtmsColumns As Boolean,
      sicOptions As clsSICOptions,
      scanList As clsScanList,
      spectraCache As clsSpectraCache,
      currentScan As clsScanInfo,
      writer As TextWriter,
      reporterIons As IList(Of clsReporterIonInfo),
      cColDelimiter As Char,
      saveUncorrectedIntensities As Boolean,
      saveObservedMasses As Boolean)

        Const USE_MAX_ABUNDANCE_IN_WINDOW = True

        Static intensityCorrector As New clsITraqIntensityCorrection(
            clsReporterIons.eReporterIonMassModeConstants.CustomOrNone,
            clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex.ABSciex)

        Dim reporterIntensities() As Double
        Dim reporterIntensitiesCorrected() As Double
        Dim closestMZ() As Double

        ' The following will be a value between 0 and 100
        ' Using Absolute Value of percent change to avoid averaging both negative and positive values
        Dim parentIonMZ As Double

        If currentScan.FragScanInfo.ParentIonInfoIndex >= 0 AndAlso currentScan.FragScanInfo.ParentIonInfoIndex < scanList.ParentIons.Count Then
            parentIonMZ = scanList.ParentIons(currentScan.FragScanInfo.ParentIonInfoIndex).MZ
        Else
            parentIonMZ = 0
        End If

        Dim poolIndex As Integer
        If Not spectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, poolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        ' Initialize the arrays used to track the observed reporter ion values
        ReDim reporterIntensities(reporterIons.Count - 1)
        ReDim reporterIntensitiesCorrected(reporterIons.Count - 1)
        ReDim closestMZ(reporterIons.Count - 1)

        ' Initialize the output variables
        Dim dataColumns = New List(Of String) From {
            sicOptions.DatasetNumber.ToString(),
            currentScan.ScanNumber.ToString(),
            currentScan.FragScanInfo.CollisionMode,
            StringUtilities.DblToString(parentIonMZ, 2),
            StringUtilities.DblToString(currentScan.BasePeakIonIntensity, 2),
            StringUtilities.DblToString(currentScan.BasePeakIonMZ, 4)
        }

        Dim reporterIntensityList = New List(Of String)
        Dim obsMZList = New List(Of String)
        Dim uncorrectedIntensityList = New List(Of String)

        Dim ftmsSignalToNoise = New List(Of String)
        Dim ftmsResolution = New List(Of String)
        ' Dim ftmsLabelDataMz = New List(Of String)

        Dim reporterIntensityMax As Double = 0

        ' Find the reporter ion intensities
        ' Also keep track of the closest m/z for each reporter ion
        ' Note that we're using the maximum intensity in the range (not the sum)
        For reporterIonIndex = 0 To reporterIons.Count - 1

            Dim ionMatchCount As Integer

            With reporterIons(reporterIonIndex)
                ' Search for the reporter ion MZ in this mass spectrum
                reporterIntensities(reporterIonIndex) = dataAggregation.AggregateIonsInRange(
                    spectraCache.SpectraPool(poolIndex),
                    .MZ,
                    .MZToleranceDa,
                    ionMatchCount,
                    closestMZ(reporterIonIndex),
                    USE_MAX_ABUNDANCE_IN_WINDOW)

                .SignalToNoise = 0
                .Resolution = 0
                .LabelDataMZ = 0
            End With
        Next

        If includeFtmsColumns AndAlso currentScan.IsFTMS Then

            ' Retrieve the label data for this spectrum

            Dim ftLabelData As udtFTLabelInfoType() = Nothing
            xcaliburAccessor.GetScanLabelData(currentScan.ScanNumber, ftLabelData)

            ' Find each reporter ion in ftLabelData

            For reporterIonIndex = 0 To reporterIons.Count - 1
                Dim mzToFind = reporterIons(reporterIonIndex).MZ
                Dim mzToleranceDa = reporterIons(reporterIonIndex).MZToleranceDa
                Dim highestIntensity = 0.0
                Dim udtBestMatch = New udtFTLabelInfoType()
                Dim matchFound = False

                For Each labelItem In ftLabelData

                    ' Compare labelItem.Mass (which is m/z of the ion in labelItem) to the m/z of the current reporter ion
                    If Math.Abs(mzToFind - labelItem.Mass) > mzToleranceDa Then
                        Continue For
                    End If

                    ' m/z is within range
                    If labelItem.Intensity > highestIntensity Then
                        udtBestMatch = labelItem
                        highestIntensity = labelItem.Intensity
                        matchFound = True
                    End If

                Next

                If matchFound Then
                    reporterIons(reporterIonIndex).SignalToNoise = udtBestMatch.SignalToNoise
                    reporterIons(reporterIonIndex).Resolution = udtBestMatch.Resolution
                    reporterIons(reporterIonIndex).LabelDataMZ = udtBestMatch.Mass
                End If

            Next
        End If

        ' Populate reporterIntensitiesCorrected with the data in reporterIntensities
        Array.Copy(reporterIntensities, reporterIntensitiesCorrected, reporterIntensities.Length)

        If mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection Then

            If mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ OrElse
               mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes OrElse
               mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes OrElse
               mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ OrElse
               mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ Then

                ' Correct the reporter ion intensities using the Reporter Ion Intensity Corrector class

                If intensityCorrector.ReporterIonMode <> mOptions.ReporterIons.ReporterIonMassMode OrElse
                   intensityCorrector.ITraq4PlexCorrectionFactorType <> mOptions.ReporterIons.ReporterIonITraq4PlexCorrectionFactorType Then
                    intensityCorrector.UpdateReporterIonMode(
                        mOptions.ReporterIons.ReporterIonMassMode,
                        mOptions.ReporterIons.ReporterIonITraq4PlexCorrectionFactorType)
                End If

                ' Count the number of non-zero data points in reporterIntensitiesCorrected()
                Dim positiveCount = 0
                For reporterIonIndex = 0 To reporterIons.Count - 1
                    If reporterIntensitiesCorrected(reporterIonIndex) > 0 Then
                        positiveCount += 1
                    End If
                Next

                ' Apply the correction if 2 or more points are non-zero
                If positiveCount >= 2 Then
                    intensityCorrector.ApplyCorrection(reporterIntensitiesCorrected)
                End If

            End If

        End If

        ' Now construct the string of intensity values, delimited by cColDelimiter
        ' Will also compute the percent change in intensities

        ' Initialize the variables used to compute the weighted average percent change
        Dim pctChangeSum As Double = 0
        Dim originalIntensitySum As Double = 0

        For reporterIonIndex = 0 To reporterIons.Count - 1

            If Not reporterIons(reporterIonIndex).ContaminantIon Then
                ' Update the PctChange variables and the IntensityMax variable only if this is not a Contaminant Ion

                originalIntensitySum += reporterIntensities(reporterIonIndex)

                If reporterIntensities(reporterIonIndex) > 0 Then
                    ' Compute the percent change, then update pctChangeSum
                    Dim pctChange =
                        (reporterIntensitiesCorrected(reporterIonIndex) - reporterIntensities(reporterIonIndex)) /
                        reporterIntensities(reporterIonIndex)

                    ' Using Absolute Value here to prevent negative changes from cancelling out positive changes
                    pctChangeSum += Math.Abs(pctChange * reporterIntensities(reporterIonIndex))
                End If

                If reporterIntensitiesCorrected(reporterIonIndex) > reporterIntensityMax Then
                    reporterIntensityMax = reporterIntensitiesCorrected(reporterIonIndex)
                End If
            End If

            If Not reporterIons(reporterIonIndex).ContaminantIon OrElse saveUncorrectedIntensities Then
                ' Append the reporter ion intensity to reporterIntensityList
                ' We skip contaminant ions, unless saveUncorrectedIntensities is True, then we include them

                reporterIntensityList.Add(StringUtilities.DblToString(reporterIntensitiesCorrected(reporterIonIndex), 2))

                If saveObservedMasses Then
                    ' Append the observed reporter mass value to obsMZList
                    obsMZList.Add(StringUtilities.DblToString(closestMZ(reporterIonIndex), 3))
                End If

                If saveUncorrectedIntensities Then
                    ' Append the original, uncorrected intensity value
                    uncorrectedIntensityList.Add(StringUtilities.DblToString(reporterIntensities(reporterIonIndex), 2))
                End If

                If includeFtmsColumns Then
                    If Math.Abs(reporterIons(reporterIonIndex).SignalToNoise) < Single.Epsilon AndAlso
                       Math.Abs(reporterIons(reporterIonIndex).Resolution) < Single.Epsilon AndAlso
                       Math.Abs(reporterIons(reporterIonIndex).LabelDataMZ) < Single.Epsilon Then
                        ' A match was not found in the label data; display blanks (not zeroes)
                        ftmsSignalToNoise.Add(String.Empty)
                        ftmsResolution.Add(String.Empty)
                        ' ftmsLabelDataMz.Add(String.Empty)
                    Else
                        ftmsSignalToNoise.Add(StringUtilities.DblToString(reporterIons(reporterIonIndex).SignalToNoise, 2))
                        ftmsResolution.Add(StringUtilities.DblToString(reporterIons(reporterIonIndex).Resolution, 2))
                        ' ftmsLabelDataMz.Add(StringUtilities.DblToString(reporterIons(reporterIonIndex).LabelDataMZ, 4))
                    End If

                End If

            End If

        Next

        ' Compute the weighted average percent intensity correction value
        Dim weightedAvgPctIntensityCorrection As Single
        If originalIntensitySum > 0 Then
            weightedAvgPctIntensityCorrection = CSng(pctChangeSum / originalIntensitySum * 100)
        Else
            weightedAvgPctIntensityCorrection = 0
        End If

        ' Append the maximum reporter ion intensity then the individual reporter ion intensities
        dataColumns.Add(StringUtilities.DblToString(reporterIntensityMax, 2))
        dataColumns.AddRange(reporterIntensityList)

        ' Append the weighted average percent intensity correction
        If weightedAvgPctIntensityCorrection < Single.Epsilon Then
            dataColumns.Add("0")
        Else
            dataColumns.Add(StringUtilities.DblToString(weightedAvgPctIntensityCorrection, 1))
        End If

        If saveObservedMasses Then
            dataColumns.AddRange(obsMZList)
        End If

        If saveUncorrectedIntensities Then
            dataColumns.AddRange(uncorrectedIntensityList)
        End If

        If includeFtmsColumns Then
            dataColumns.AddRange(ftmsSignalToNoise)
            dataColumns.AddRange(ftmsResolution)

            ' Uncomment to include the label data m/z value in the _ReporterIons.txt file
            'If saveObservedMasses Then
            '    dataColumns.AddRange(ftmsLabelDataMz)
            'End If
        End If

        writer.WriteLine(String.Join(cColDelimiter, dataColumns))

    End Sub

    Protected Class clsReportIonInfoComparer
        Implements IComparer

        Public Function Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
            Dim reporterIonInfoA As clsReporterIonInfo
            Dim reporterIonInfoB As clsReporterIonInfo

            reporterIonInfoA = DirectCast(x, clsReporterIonInfo)
            reporterIonInfoB = DirectCast(y, clsReporterIonInfo)

            If reporterIonInfoA.MZ > reporterIonInfoB.MZ Then
                Return 1
            ElseIf reporterIonInfoA.MZ < reporterIonInfoB.MZ Then
                Return -1
            Else
                Return 0
            End If
        End Function
    End Class


End Class
