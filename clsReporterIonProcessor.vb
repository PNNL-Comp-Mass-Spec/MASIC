Imports MASIC.clsMASIC
Imports MASIC.DataOutput
Imports ThermoRawFileReader

Public Class clsReporterIonProcessor
    Inherits clsEventNotifier

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
    ''' <param name="objSpectraCache"></param>
    ''' <param name="strInputFilePathFull">Full path to the input file</param>
    ''' <param name="strOutputFolderPath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function FindReporterIons(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      strInputFilePathFull As String,
      strOutputFolderPath As String) As Boolean

        Const cColDelimiter As Char = ControlChars.Tab

        Dim outputFilePath = "??"

        Try

            ' Use Xraw to read the .Raw files
            Dim xcaliburAccessor = New XRawFileIO()
            Dim includeFtmsColumns = False

            AddHandler xcaliburAccessor.ReportError, AddressOf mXcaliburAccessor_ReportError
            AddHandler xcaliburAccessor.ReportWarning, AddressOf mXcaliburAccessor_ReportWarning

            If strInputFilePathFull.ToUpper().EndsWith(DataInput.clsDataImport.FINNIGAN_RAW_FILE_EXTENSION.ToUpper()) Then

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
                    xcaliburAccessor.OpenRawFile(strInputFilePathFull)
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
                Path.GetFileName(strInputFilePathFull),
                strOutputFolderPath,
                clsDataOutput.eOutputFileTypeConstants.ReporterIonsFile)

            Using srOutFile = New StreamWriter(outputFilePath)

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

                Dim blnSaveUncorrectedIntensities As Boolean =
                    mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection AndAlso mOptions.ReporterIons.ReporterIonSaveUncorrectedIntensities

                Dim dataAggregation = New clsDataAggregation()
                RegisterEvents(dataAggregation)

                For Each reporterIon In reporterIons

                    If Not reporterIon.ContaminantIon OrElse blnSaveUncorrectedIntensities Then
                        ' Contruct the reporter ion intensity header
                        ' We skip contaminant ions, unless blnSaveUncorrectedIntensities is True, then we include them

                        Dim mzValue As String
                        If (mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ) Then
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
                            ' Error updating the sortedset; 
                            ' this shouldn't happen based on the .ContainsKey test above
                        End Try

                        ' Append the reporter ion intensity title to the headers
                        headerColumns.Add("Ion_" & mzValue)

                        ' This string will only be included in the header line if mOptions.ReporterIons.ReporterIonSaveObservedMasses is true
                        obsMZHeaders.Add("Ion_" & mzValue & "_ObsMZ")

                        ' This string will be included in the header line if blnSaveUncorrectedIntensities is true
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

                If blnSaveUncorrectedIntensities Then
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
                srOutFile.WriteLine(String.Join(cColDelimiter, headerColumns))

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
                        objSpectraCache,
                        scanList.FragScans(scanPointer),
                        srOutFile,
                        reporterIons,
                        cColDelimiter,
                        blnSaveUncorrectedIntensities,
                        mOptions.ReporterIons.ReporterIonSaveObservedMasses)

                    If scanList.MasterScanOrderCount > 1 Then
                        UpdateProgress(CShort(masterOrderIndex / (scanList.MasterScanOrderCount - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)
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
            ReportError("FindReporterIons", "Error writing the reporter ions to: " & outputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Looks for the reporter ion m/z values, +/- a tolerance
    ''' Calls AggregateIonsInRange with blnReturnMax = True, meaning we're reporting the maximum ion abundance for each reporter ion m/z
    ''' </summary>
    ''' <param name="xcaliburAccessor"></param>
    ''' <param name="dataAggregation"></param>
    ''' <param name="includeFtmsColumns"></param>
    ''' <param name="sicOptions"></param>
    ''' <param name="scanList"></param>
    ''' <param name="objSpectraCache"></param>
    ''' <param name="currentScan"></param>
    ''' <param name="srOutFile"></param>
    ''' <param name="reporterIons"></param>
    ''' <param name="cColDelimiter"></param>
    ''' <param name="blnSaveUncorrectedIntensities"></param>
    ''' <param name="saveObservedMasses"></param>
    ''' <remarks></remarks>
    Private Sub FindReporterIonsWork(
      xcaliburAccessor As XRawFileIO,
      dataAggregation As clsDataAggregation,
      includeFtmsColumns As Boolean,
      sicOptions As clsSICOptions,
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      currentScan As clsScanInfo,
      srOutFile As StreamWriter,
      reporterIons As clsReporterIonInfo(),
      cColDelimiter As Char,
      blnSaveUncorrectedIntensities As Boolean,
      saveObservedMasses As Boolean)

        Const USE_MAX_ABUNDANCE_IN_WINDOW = True

        Static objITraqIntensityCorrector As New clsITraqIntensityCorrection(
            clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes,
            clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex.ABSciex)

        Dim sngReporterIntensities() As Single
        Dim sngReporterIntensitiesCorrected() As Single
        Dim dblClosestMZ() As Double

        ' The following will be a value between 0 and 100
        ' Using Absolute Value of percent change to avoid averaging both negative and positive values
        Dim dblParentIonMZ As Double

        If currentScan.FragScanInfo.ParentIonInfoIndex >= 0 AndAlso currentScan.FragScanInfo.ParentIonInfoIndex < scanList.ParentIons.Count Then
            dblParentIonMZ = scanList.ParentIons(currentScan.FragScanInfo.ParentIonInfoIndex).MZ
        Else
            dblParentIonMZ = 0
        End If

        Dim intPoolIndex As Integer
        If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        ' Initialize the arrays used to track the observed reporter ion values
        ReDim sngReporterIntensities(reporterIons.Length - 1)
        ReDim sngReporterIntensitiesCorrected(reporterIons.Length - 1)
        ReDim dblClosestMZ(reporterIons.Length - 1)

        ' Initialize the output variables
        Dim dataColumns = New List(Of String)
        dataColumns.Add(sicOptions.DatasetNumber.ToString())
        dataColumns.Add(currentScan.ScanNumber.ToString())
        dataColumns.Add(currentScan.FragScanInfo.CollisionMode)
        dataColumns.Add(Math.Round(dblParentIonMZ, 2).ToString())
        dataColumns.Add(Math.Round(currentScan.BasePeakIonIntensity, 2).ToString())
        dataColumns.Add(Math.Round(currentScan.BasePeakIonMZ, 4).ToString())

        Dim reporterIntensityList = New List(Of String)
        Dim obsMZList = New List(Of String)
        Dim uncorrectedIntensityList = New List(Of String)

        Dim ftmsSignalToNoise = New List(Of String)
        Dim ftmsResolution = New List(Of String)
        ' Dim ftmsLabelDataMz = New List(Of String)

        Dim sngReporterIntensityMax As Single = 0

        ' Find the reporter ion intensities
        ' Also keep track of the closest m/z for each reporter ion
        ' Note that we're using the maximum intensity in the range (not the sum)
        For intReporterIonIndex = 0 To reporterIons.Length - 1

            Dim intIonmatchcount As Integer

            With reporterIons(intReporterIonIndex)
                ' Search for the reporter ion MZ in this mass spectrum
                sngReporterIntensities(intReporterIonIndex) = dataAggregation.AggregateIonsInRange(
                    objSpectraCache.SpectraPool(intPoolIndex),
                    .MZ,
                    .MZToleranceDa,
                    intIonmatchcount,
                    dblClosestMZ(intReporterIonIndex),
                    USE_MAX_ABUNDANCE_IN_WINDOW)

                .SignalToNoise = 0
                .Resolution = 0
                .LabelDataMZ = 0
            End With
        Next intReporterIonIndex

        If includeFtmsColumns AndAlso currentScan.IsFTMS Then

            ' Retrieve the label data for this spectrum

            Dim ftLabelData As udtFTLabelInfoType() = Nothing
            xcaliburAccessor.GetScanLabelData(currentScan.ScanNumber, ftLabelData)

            ' Find each reporter ion in ftLabelData

            For intReporterIonIndex = 0 To reporterIons.Length - 1
                Dim mzToFind = reporterIons(intReporterIonIndex).MZ
                Dim mzToleranceDa = reporterIons(intReporterIonIndex).MZToleranceDa
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
                    reporterIons(intReporterIonIndex).SignalToNoise = udtBestMatch.SignalToNoise
                    reporterIons(intReporterIonIndex).Resolution = udtBestMatch.Resolution
                    reporterIons(intReporterIonIndex).LabelDataMZ = udtBestMatch.Mass
                End If

            Next
        End If

        ' Populate sngReporterIntensitiesCorrected with the data in sngReporterIntensities
        Array.Copy(sngReporterIntensities, sngReporterIntensitiesCorrected, sngReporterIntensities.Length)

        If mOptions.ReporterIons.ReporterIonApplyAbundanceCorrection Then

            If mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ OrElse
               mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes OrElse
               mOptions.ReporterIons.ReporterIonMassMode = clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes Then

                ' Correct the reporter ion intensities using the ITraq Intensity Corrector class

                If objITraqIntensityCorrector.ITraqMode <> mOptions.ReporterIons.ReporterIonMassMode OrElse
                   objITraqIntensityCorrector.ITraq4PlexCorrectionFactorType <> mOptions.ReporterIons.ReporterIonITraq4PlexCorrectionFactorType Then
                    objITraqIntensityCorrector.UpdateITraqMode(
                        mOptions.ReporterIons.ReporterIonMassMode,
                        mOptions.ReporterIons.ReporterIonITraq4PlexCorrectionFactorType)
                End If

                ' Make sure at least one of two of the points in sngReporterIntensitiesCorrected() is non-zero
                ' If not, then no correction can be applied
                Dim intPositiveCount = 0
                For intReporterIonIndex = 0 To reporterIons.Length - 1
                    If sngReporterIntensitiesCorrected(intReporterIonIndex) > 0 Then
                        intPositiveCount += 1
                    End If
                Next

                If intPositiveCount >= 2 Then
                    objITraqIntensityCorrector.ApplyCorrection(sngReporterIntensitiesCorrected)
                End If

            End If

        End If

        ' Now construct the string of intensity values, delimited by cColDelimiter
        ' Will also compute the percent change in intensities

        ' Initialize the variables used to compute the weighted average percent change
        Dim dblPctChangeSum As Double = 0
        Dim dblOriginalIntensitySum As Double = 0

        For intReporterIonIndex = 0 To reporterIons.Length - 1

            If Not reporterIons(intReporterIonIndex).ContaminantIon Then
                ' Update the PctChange variables and the IntensityMax variable only if this is not a Contaminant Ion

                dblOriginalIntensitySum += sngReporterIntensities(intReporterIonIndex)

                If sngReporterIntensities(intReporterIonIndex) > 0 Then
                    ' Compute the percent change, then update dblPctChangeSum
                    Dim dblPctChange =
                        (sngReporterIntensitiesCorrected(intReporterIonIndex) - sngReporterIntensities(intReporterIonIndex)) /
                        sngReporterIntensities(intReporterIonIndex)

                    ' Using Absolute Value here to prevent negative changes from cancelling out positive changes
                    dblPctChangeSum += Math.Abs(dblPctChange * sngReporterIntensities(intReporterIonIndex))
                End If

                If sngReporterIntensitiesCorrected(intReporterIonIndex) > sngReporterIntensityMax Then
                    sngReporterIntensityMax = sngReporterIntensitiesCorrected(intReporterIonIndex)
                End If
            End If

            If Not reporterIons(intReporterIonIndex).ContaminantIon OrElse blnSaveUncorrectedIntensities Then
                ' Append the reporter ion intensity to strReporterIntensityList
                ' We skip contaminant ions, unless blnSaveUncorrectedIntensities is True, then we include them

                reporterIntensityList.Add(Math.Round(sngReporterIntensitiesCorrected(intReporterIonIndex), 2).ToString())

                If saveObservedMasses Then
                    ' Append the observed reporter mass value to strObsMZList
                    obsMZList.Add(Math.Round(dblClosestMZ(intReporterIonIndex), 3).ToString())
                End If

                If blnSaveUncorrectedIntensities Then
                    ' Append the original, uncorrected intensity value
                    uncorrectedIntensityList.Add(Math.Round(sngReporterIntensities(intReporterIonIndex), 2).ToString())
                End If

                If includeFtmsColumns Then
                    If Math.Abs(reporterIons(intReporterIonIndex).SignalToNoise) < Single.Epsilon AndAlso
                       Math.Abs(reporterIons(intReporterIonIndex).Resolution) < Single.Epsilon AndAlso
                       Math.Abs(reporterIons(intReporterIonIndex).LabelDataMZ) < Single.Epsilon Then
                        ' A match was not found in the label data; display blanks (not zeroes)
                        ftmsSignalToNoise.Add("")
                        ftmsResolution.Add("")
                        ' ftmsLabelDataMz.Add("")
                    Else
                        ftmsSignalToNoise.Add(Math.Round(reporterIons(intReporterIonIndex).SignalToNoise, 2).ToString())
                        ftmsResolution.Add(Math.Round(reporterIons(intReporterIonIndex).Resolution, 2).ToString())
                        ' ftmsLabelDataMz.Add(Math.Round(reporterIons(intReporterIonIndex).LabelDataMZ, 4).ToString())
                    End If

                End If

            End If

        Next

        ' Compute the weighted average percent intensity correction value
        Dim sngWeightedAvgPctIntensityCorrection As Single
        If dblOriginalIntensitySum > 0 Then
            sngWeightedAvgPctIntensityCorrection = CSng(dblPctChangeSum / dblOriginalIntensitySum * 100)
        Else
            sngWeightedAvgPctIntensityCorrection = 0
        End If

        ' Append the maximum reporter ion intensity then the individual reporter ion intensities
        dataColumns.Add(Math.Round(sngReporterIntensityMax, 2).ToString())
        dataColumns.AddRange(reporterIntensityList)

        ' Append the weighted average percent intensity correction
        If sngWeightedAvgPctIntensityCorrection < Single.Epsilon Then
            dataColumns.Add("0")
        Else
            dataColumns.Add(Math.Round(sngWeightedAvgPctIntensityCorrection, 1).ToString())
        End If

        If saveObservedMasses Then
            dataColumns.AddRange(obsMZList)
        End If

        If blnSaveUncorrectedIntensities Then
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

        srOutFile.WriteLine(String.Join(cColDelimiter, dataColumns))

    End Sub

    Private Sub mXcaliburAccessor_ReportError(strMessage As String)
        Console.WriteLine(strMessage)
        ReportError("XcaliburAccessor", strMessage, Nothing, True, False, eMasicErrorCodes.InputFileDataReadError)
    End Sub

    Private Sub mXcaliburAccessor_ReportWarning(strMessage As String)
        Console.WriteLine(strMessage)
        ReportError("XcaliburAccessor", strMessage, Nothing, False, False, eMasicErrorCodes.InputFileDataReadError)
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
