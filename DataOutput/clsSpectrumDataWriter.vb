Public Class clsSpectrumDataWriter
    Inherits clsEventNotifier

    Public Function ExportRawDataToDisk(
      scanList As clsScanList,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      strOutputFolderPath As String) As Boolean

        Dim strOutputFilePath = "??"
        Dim strOutputFilePath2 As String

        Dim srDataOutfile As StreamWriter
        Dim srScanInfoOutFile As StreamWriter = Nothing

        Dim intMasterOrderIndex As Integer
        Dim intScanPointer As Integer

        Dim udtRawDataExportOptions As udtRawDataExportOptionsType
        Dim intSpectrumExportCount As Integer

        Dim blnSuccess As Boolean

        Try

            udtRawDataExportOptions = mRawDataExportOptions

            Select Case udtRawDataExportOptions.FileFormat
                Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile
                    strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.PEKFile)
                    srDataOutfile = New StreamWriter(strOutputFilePath)
                Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
                    strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsIsosFile)
                    strOutputFilePath2 = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsScansFile)

                    srDataOutfile = New StreamWriter(strOutputFilePath)
                    srScanInfoOutFile = New StreamWriter(strOutputFilePath2)

                    ' Write the file headers
                    WriteDecon2LSIsosFileHeaders(srDataOutfile)
                    WriteDecon2LSScanFileHeaders(srScanInfoOutFile)

                Case Else
                    ' Unknown format
                    mStatusMessage = "Unknown raw data file format: " & udtRawDataExportOptions.FileFormat.ToString
                    ShowErrorMessage(mStatusMessage)

                    If MyBase.ShowMessages Then
                        Windows.Forms.MessageBox.Show(mStatusMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Else
                        Throw New Exception(mStatusMessage)
                    End If
                    blnSuccess = False
                    Exit Try
            End Select

            intSpectrumExportCount = 0

            If Not udtRawDataExportOptions.IncludeMSMS AndAlso udtRawDataExportOptions.RenumberScans Then
                udtRawDataExportOptions.RenumberScans = True
            Else
                udtRawDataExportOptions.RenumberScans = False
            End If

            SetSubtaskProcessingStepPct(0, "Exporting raw data")

            For intMasterOrderIndex = 0 To scanList.MasterScanOrderCount - 1
                intScanPointer = scanList.MasterScanOrder(intMasterOrderIndex).ScanIndexPointer
                If scanList.MasterScanOrder(intMasterOrderIndex).ScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                    SaveRawDatatoDiskWork(srDataOutfile, srScanInfoOutFile, scanList.SurveyScans(intScanPointer), objSpectraCache, strInputFileName, False, intSpectrumExportCount, udtRawDataExportOptions)
                Else
                    If udtRawDataExportOptions.IncludeMSMS OrElse
                      Not scanList.FragScans(intScanPointer).MRMScanType = MRMScanTypeConstants.NotMRM Then
                        ' Either we're writing out MS/MS data or this is an MRM scan
                        SaveRawDatatoDiskWork(srDataOutfile, srScanInfoOutFile, scanList.FragScans(intScanPointer), objSpectraCache, strInputFileName, True, intSpectrumExportCount, udtRawDataExportOptions)
                    End If
                End If

                If scanList.MasterScanOrderCount > 1 Then
                    SetSubtaskProcessingStepPct(CShort(intMasterOrderIndex / (scanList.MasterScanOrderCount - 1) * 100))
                Else
                    SetSubtaskProcessingStepPct(0)
                End If

                UpdateCacheStats(objSpectraCache)

                If masicoptions.AbortProcessing Then
                    Exit For
                End If
            Next intMasterOrderIndex

            If Not srDataOutfile Is Nothing Then srDataOutfile.Close()
            If Not srScanInfoOutFile Is Nothing Then srScanInfoOutFile.Close()

            blnSuccess = True

        Catch ex As Exception
            LogErrors("ExportRawDataToDisk", "Error writing the raw spectra data to" & GetFilePathPrefixChar() & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
            blnSuccess = False
        End Try

        Return blnSuccess
    End Function


    Private Sub SaveCSVFilesToDiskWork(
      srDataOutFile As StreamWriter,
      srScanInfoOutfile As StreamWriter,
      currentScan As clsScanInfo,
      objSpectraCache As clsSpectraCache,
      blnFragmentationScan As Boolean,
      ByRef intSpectrumExportCount As Integer,
      udtRawDataExportOptions As udtRawDataExportOptionsType)

        Dim intIonIndex As Integer
        Dim sngIntensities() As Single
        Dim intPointerArray() As Integer

        Dim intScanNumber As Integer
        Dim intMSLevel As Integer
        Dim intPoolIndex As Integer

        Dim intStartIndex As Integer, intExportCount As Integer
        Dim sngMinimumIntensityCurrentScan As Single

        Dim intNumIsotopicSignatures As Integer
        Dim intNumPeaks As Integer
        Dim sngBaselineNoiseLevel As Single

        Dim intCharge As Integer
        Dim sngFit As Single
        Dim dblMass As Double
        Dim sngFWHM As Single
        Dim sngSignalToNoise As Single
        Dim sngMonoisotopicAbu As Single
        Dim sngMonoPlus2Abu As Single

        intExportCount = 0

        If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        intSpectrumExportCount += 1

        With currentScan
            ' First, write an entry to the "_scans.csv" file

            If udtRawDataExportOptions.RenumberScans Then
                intScanNumber = intSpectrumExportCount
            Else
                intScanNumber = .ScanNumber
            End If

            If blnFragmentationScan Then
                intMSLevel = .FragScanInfo.MSLevel
            Else
                intMSLevel = 1
            End If

            intNumIsotopicSignatures = 0
            intNumPeaks = objSpectraCache.SpectraPool(intPoolIndex).IonCount

            sngBaselineNoiseLevel = .BaselineNoiseStats.NoiseLevel
            If sngBaselineNoiseLevel < 1 Then sngBaselineNoiseLevel = 1

            ' Old Column Order:
            ''strLineOut = intScanNumber.ToString & "," &
            ''             .ScanTime.ToString("0.0000") &
            ''             intMSLevel & "," &
            ''             intNumIsotopicSignatures & "," &
            ''             intNumPeaks & "," &
            ''             .TotalIonIntensity.ToString & "," &
            ''             .BasePeakIonMZ & "," &
            ''             .BasePeakIonIntensity & "," &
            ''             sngTimeDomainIntensity & "," &
            ''             sngPeakIntensityThreshold & "," &
            ''             sngPeptideIntensityThreshold

            WriteDecon2LSScanFileEntry(srScanInfoOutfile, currentScan, intScanNumber, intMSLevel, intNumPeaks, intNumIsotopicSignatures)
        End With


        With objSpectraCache.SpectraPool(intPoolIndex)
            ' Now write an entry to the "_isos.csv" file

            If .IonCount > 0 Then
                ' Populate sngIntensities and intPointerArray()
                ReDim sngIntensities(.IonCount - 1)
                ReDim intPointerArray(.IonCount - 1)
                For intIonIndex = 0 To .IonCount - 1
                    sngIntensities(intIonIndex) = .IonsIntensity(intIonIndex)
                    intPointerArray(intIonIndex) = intIonIndex
                Next intIonIndex

                ' Sort intPointerArray() based on the intensities in sngIntensities
                Array.Sort(sngIntensities, intPointerArray)

                If udtRawDataExportOptions.MaxIonCountPerScan > 0 Then
                    ' Possibly limit the number of ions to intMaxIonCount
                    intStartIndex = .IonCount - udtRawDataExportOptions.MaxIonCountPerScan
                    If intStartIndex < 0 Then intStartIndex = 0
                Else
                    intStartIndex = 0
                End If

                ' Define the minimum data point intensity value
                sngMinimumIntensityCurrentScan = .IonsIntensity(intPointerArray(intStartIndex))

                ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, udtRawDataExportOptions.IntensityMinimum)

                ' If udtRawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update sngMinimumIntensityCurrentScan
                If udtRawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                    sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * udtRawDataExportOptions.MinimumSignalToNoiseRatio)
                End If

                intExportCount = 0
                For intIonIndex = 0 To .IonCount - 1
                    If .IonsIntensity(intIonIndex) >= sngMinimumIntensityCurrentScan Then

                        intCharge = 1
                        sngFit = 0
                        dblMass = ConvoluteMass(.IonsMZ(intIonIndex), 1, 0)
                        sngFWHM = 0
                        sngSignalToNoise = .IonsIntensity(intIonIndex) / sngBaselineNoiseLevel
                        sngMonoisotopicAbu = -10
                        sngMonoPlus2Abu = -10

                        WriteDecon2LSIsosFileEntry(
                          srDataOutFile, intScanNumber, intCharge,
                          .IonsIntensity(intIonIndex), .IonsMZ(intIonIndex),
                          sngFit, dblMass, dblMass, dblMass,
                          sngFWHM, sngSignalToNoise, sngMonoisotopicAbu, sngMonoPlus2Abu)

                        intExportCount += 1
                    End If
                Next intIonIndex
            End If

        End With

    End Sub


    Private Sub SavePEKFileToDiskWork(
      srOutFile As StreamWriter,
      currentScan As clsScanInfo,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      blnFragmentationScan As Boolean,
      ByRef intSpectrumExportCount As Integer,
      udtRawDataExportOptions As udtRawDataExportOptionsType)

        Dim strLineOut As String
        Dim intIonIndex As Integer
        Dim sngIntensities() As Single
        Dim intPointerArray() As Integer

        Dim intScanNumber As Integer
        Dim intPoolIndex As Integer

        Dim intStartIndex As Integer, intExportCount As Integer
        Dim sngMinimumIntensityCurrentScan As Single

        intExportCount = 0

        If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
            SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
            Exit Sub
        End If

        intSpectrumExportCount += 1

        With currentScan

            srOutFile.WriteLine("Time domain signal level:" & ControlChars.Tab & .BasePeakIonIntensity.ToString)          ' Store the base peak ion intensity as the time domain signal level value

            srOutFile.WriteLine("MASIC " & MyBase.FileVersion)                     ' Software version
            strLineOut = "MS/MS-based PEK file"
            If udtRawDataExportOptions.IncludeMSMS Then
                strLineOut &= " (includes both survey scans and fragmentation spectra)"
            Else
                strLineOut &= " (includes only survey scans)"
            End If
            srOutFile.WriteLine(strLineOut)

            If udtRawDataExportOptions.RenumberScans Then
                intScanNumber = intSpectrumExportCount
            Else
                intScanNumber = .ScanNumber
            End If
            strLineOut = "Filename: " & strInputFileName & "." & intScanNumber.ToString("00000")
            srOutFile.WriteLine(strLineOut)

            If blnFragmentationScan Then
                srOutFile.WriteLine("ScanType: Fragmentation Scan")
            Else
                srOutFile.WriteLine("ScanType: Survey Scan")
            End If

            srOutFile.WriteLine("Charge state mass transform results:")
            srOutFile.WriteLine("First CS,    Number of CS,   Abundance,   Mass,   Standard deviation")
        End With

        With objSpectraCache.SpectraPool(intPoolIndex)

            If .IonCount > 0 Then
                ' Populate sngIntensities and intPointerArray()
                ReDim sngIntensities(.IonCount - 1)
                ReDim intPointerArray(.IonCount - 1)
                For intIonIndex = 0 To .IonCount - 1
                    sngIntensities(intIonIndex) = .IonsIntensity(intIonIndex)
                    intPointerArray(intIonIndex) = intIonIndex
                Next intIonIndex

                ' Sort intPointerArray() based on the intensities in sngIntensities
                Array.Sort(sngIntensities, intPointerArray)

                If udtRawDataExportOptions.MaxIonCountPerScan > 0 Then
                    ' Possibly limit the number of ions to intMaxIonCount
                    intStartIndex = .IonCount - udtRawDataExportOptions.MaxIonCountPerScan
                    If intStartIndex < 0 Then intStartIndex = 0
                Else
                    intStartIndex = 0
                End If


                ' Define the minimum data point intensity value
                sngMinimumIntensityCurrentScan = .IonsIntensity(intPointerArray(intStartIndex))

                ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, udtRawDataExportOptions.IntensityMinimum)

                ' If udtRawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update sngMinimumIntensityCurrentScan
                If udtRawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                    sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * udtRawDataExportOptions.MinimumSignalToNoiseRatio)
                End If

                intExportCount = 0
                For intIonIndex = 0 To .IonCount - 1
                    If .IonsIntensity(intIonIndex) >= sngMinimumIntensityCurrentScan Then
                        strLineOut = "1" & ControlChars.Tab & "1" & ControlChars.Tab & .IonsIntensity(intIonIndex) & ControlChars.Tab & .IonsMZ(intIonIndex) & ControlChars.Tab & "0"
                        srOutFile.WriteLine(strLineOut)
                        intExportCount += 1
                    End If
                Next intIonIndex
            End If

            srOutFile.WriteLine("Number of peaks in spectrum = " & .IonCount.ToString)
            srOutFile.WriteLine("Number of isotopic distributions detected = " & intExportCount.ToString)
            srOutFile.WriteLine()

        End With

    End Sub

    Private Sub SaveRawDatatoDiskWork(
      srDataOutFile As StreamWriter,
      srScanInfoOutfile As StreamWriter,
      currentScan As clsScanInfo,
      objSpectraCache As clsSpectraCache,
      strInputFileName As String,
      blnFragmentationScan As Boolean,
      ByRef intSpectrumExportCount As Integer,
      udtRawDataExportOptions As udtRawDataExportOptionsType)

        Select Case udtRawDataExportOptions.FileFormat
            Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile
                SavePEKFileToDiskWork(srDataOutFile, currentScan, objSpectraCache, strInputFileName, blnFragmentationScan, intSpectrumExportCount, udtRawDataExportOptions)
            Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
                SaveCSVFilesToDiskWork(srDataOutFile, srScanInfoOutfile, currentScan, objSpectraCache, blnFragmentationScan, intSpectrumExportCount, udtRawDataExportOptions)
            Case Else
                ' Unknown format
                ' This code should never be reached
        End Select
    End Sub

End Class
