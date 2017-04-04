Imports MASIC.clsMASIC

Namespace DataOutput

    Public Class clsSpectrumDataWriter
        Inherits clsEventNotifier

#Region "Classwide variables"
        Private ReadOnly mBPIWriter As clsBPIWriter
        Private ReadOnly mOptions As clsMASICOptions
#End Region

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New(bpiWriter As clsBPIWriter, masicOptions As clsMASICOptions)
            mBPIWriter = bpiWriter
            mOptions = masicOptions
        End Sub

        Public Function ExportRawDataToDisk(
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          strInputFileName As String,
          strOutputFolderPath As String) As Boolean

            Dim strOutputFilePath = "??"

            Try
                Dim srDataOutfile As StreamWriter
                Dim srScanInfoOutFile As StreamWriter

                Select Case mOptions.RawDataExportOptions.FileFormat
                    Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile
                        strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.PEKFile)
                        srDataOutfile = New StreamWriter(strOutputFilePath)
                        srScanInfoOutFile = Nothing

                    Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
                        strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.DeconToolsIsosFile)

                        Dim strOutputFilePath2 = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, clsDataOutput.eOutputFileTypeConstants.DeconToolsScansFile)

                        srDataOutfile = New StreamWriter(strOutputFilePath)
                        srScanInfoOutFile = New StreamWriter(strOutputFilePath2)

                        ' Write the file headers
                        mBPIWriter.WriteDecon2LSIsosFileHeaders(srDataOutfile)
                        mBPIWriter.WriteDecon2LSScanFileHeaders(srScanInfoOutFile)

                    Case Else
                        ' Unknown format
                        ReportError("ExportRawDataToDisk", "Unknown raw data file format: " & mOptions.RawDataExportOptions.FileFormat.ToString())
                        Return False
                End Select

                Dim intSpectrumExportCount = 0

                If Not mOptions.RawDataExportOptions.IncludeMSMS AndAlso mOptions.RawDataExportOptions.RenumberScans Then
                    mOptions.RawDataExportOptions.RenumberScans = True
                Else
                    mOptions.RawDataExportOptions.RenumberScans = False
                End If

                UpdateProgress(0, "Exporting raw data")

                For intMasterOrderIndex = 0 To scanList.MasterScanOrderCount - 1
                    Dim intScanPointer = scanList.MasterScanOrder(intMasterOrderIndex).ScanIndexPointer
                    If scanList.MasterScanOrder(intMasterOrderIndex).ScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                        SaveRawDatatoDiskWork(srDataOutfile, srScanInfoOutFile, scanList.SurveyScans(intScanPointer), objSpectraCache, strInputFileName, False, intSpectrumExportCount)
                    Else
                        If mOptions.RawDataExportOptions.IncludeMSMS OrElse
                          Not scanList.FragScans(intScanPointer).MRMScanType = ThermoRawFileReader.MRMScanTypeConstants.NotMRM Then
                            ' Either we're writing out MS/MS data or this is an MRM scan
                            SaveRawDatatoDiskWork(srDataOutfile, srScanInfoOutFile, scanList.FragScans(intScanPointer), objSpectraCache, strInputFileName, True, intSpectrumExportCount)
                        End If
                    End If

                    If scanList.MasterScanOrderCount > 1 Then
                        UpdateProgress(CShort(intMasterOrderIndex / (scanList.MasterScanOrderCount - 1) * 100))
                    Else
                        UpdateProgress(0)
                    End If

                    UpdateCacheStats(objSpectraCache)

                    If mOptions.AbortProcessing Then
                        Exit For
                    End If
                Next

                If Not srDataOutfile Is Nothing Then srDataOutfile.Close()
                If Not srScanInfoOutFile Is Nothing Then srScanInfoOutFile.Close()

                Return True

            Catch ex As Exception
                ReportError("ExportRawDataToDisk", "Error writing the raw spectra data to: " & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

        End Function


        Private Sub SaveCSVFilesToDiskWork(
          srDataOutFile As StreamWriter,
          srScanInfoOutfile As StreamWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache,
          blnFragmentationScan As Boolean,
          ByRef intSpectrumExportCount As Integer)

            Dim intPoolIndex As Integer
            Dim intScanNumber As Integer
            Dim sngBaselineNoiseLevel As Single

            If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                Exit Sub
            End If

            intSpectrumExportCount += 1

            With currentScan
                ' First, write an entry to the "_scans.csv" file

                If mOptions.RawDataExportOptions.RenumberScans Then
                    intScanNumber = intSpectrumExportCount
                Else
                    intScanNumber = .ScanNumber
                End If

                Dim intMSLevel As Integer
                If blnFragmentationScan Then
                    intMSLevel = .FragScanInfo.MSLevel
                Else
                    intMSLevel = 1
                End If

                Dim intNumIsotopicSignatures = 0
                Dim intNumPeaks = objSpectraCache.SpectraPool(intPoolIndex).IonCount

                sngBaselineNoiseLevel = .BaselineNoiseStats.NoiseLevel
                If sngBaselineNoiseLevel < 1 Then sngBaselineNoiseLevel = 1

                ' Old Column Order:
                ''strLineOut = intScanNumber.ToString() & "," &
                ''             .ScanTime.ToString("0.0000") &
                ''             intMSLevel & "," &
                ''             intNumIsotopicSignatures & "," &
                ''             intNumPeaks & "," &
                ''             .TotalIonIntensity.ToString() & "," &
                ''             .BasePeakIonMZ & "," &
                ''             .BasePeakIonIntensity & "," &
                ''             sngTimeDomainIntensity & "," &
                ''             sngPeakIntensityThreshold & "," &
                ''             sngPeptideIntensityThreshold

                mBPIWriter.WriteDecon2LSScanFileEntry(srScanInfoOutfile, currentScan, intScanNumber, intMSLevel, intNumPeaks, intNumIsotopicSignatures)
            End With


            With objSpectraCache.SpectraPool(intPoolIndex)
                ' Now write an entry to the "_isos.csv" file

                If .IonCount > 0 Then
                    ' Populate sngIntensities and intPointerArray()

                    Dim sngIntensities() As Single
                    Dim intPointerArray() As Integer

                    ReDim sngIntensities(.IonCount - 1)
                    ReDim intPointerArray(.IonCount - 1)
                    For intIonIndex = 0 To .IonCount - 1
                        sngIntensities(intIonIndex) = .IonsIntensity(intIonIndex)
                        intPointerArray(intIonIndex) = intIonIndex
                    Next

                    ' Sort intPointerArray() based on the intensities in sngIntensities
                    Array.Sort(sngIntensities, intPointerArray)

                    Dim intStartIndex As Integer
                    If mOptions.RawDataExportOptions.MaxIonCountPerScan > 0 Then
                        ' Possibly limit the number of ions to intMaxIonCount
                        intStartIndex = .IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan
                        If intStartIndex < 0 Then intStartIndex = 0
                    Else
                        intStartIndex = 0
                    End If

                    ' Define the minimum data point intensity value
                    Dim sngMinimumIntensityCurrentScan = .IonsIntensity(intPointerArray(intStartIndex))

                    ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                    sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum)

                    ' If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update sngMinimumIntensityCurrentScan
                    If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                        sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio)
                    End If

                    For intIonIndex = 0 To .IonCount - 1
                        If .IonsIntensity(intIonIndex) >= sngMinimumIntensityCurrentScan Then

                            Dim intCharge = 1
                            Dim sngFit = 0
                            Dim dblMass = clsUtilities.ConvoluteMass(.IonsMZ(intIonIndex), 1, 0)
                            Dim sngFWHM = 0
                            Dim sngSignalToNoise = .IonsIntensity(intIonIndex) / sngBaselineNoiseLevel
                            Dim sngMonoisotopicAbu = -10
                            Dim sngMonoPlus2Abu = -10

                            mBPIWriter.WriteDecon2LSIsosFileEntry(
                              srDataOutFile, intScanNumber, intCharge,
                              .IonsIntensity(intIonIndex), .IonsMZ(intIonIndex),
                              sngFit, dblMass, dblMass, dblMass,
                              sngFWHM, sngSignalToNoise, sngMonoisotopicAbu, sngMonoPlus2Abu)

                        End If
                    Next
                End If

            End With

        End Sub

        Private Sub SavePEKFileToDiskWork(
          srOutFile As StreamWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache,
          strInputFileName As String,
          blnFragmentationScan As Boolean,
          ByRef intSpectrumExportCount As Integer)

            Dim intPoolIndex As Integer
            Dim intExportCount = 0

            If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                Exit Sub
            End If

            intSpectrumExportCount += 1

            With currentScan

                srOutFile.WriteLine("Time domain signal level:" & ControlChars.Tab & .BasePeakIonIntensity.ToString())          ' Store the base peak ion intensity as the time domain signal level value

                srOutFile.WriteLine("MASIC " & mOptions.MASICVersion)                     ' Software version
                Dim strLineOut = "MS/MS-based PEK file"
                If mOptions.RawDataExportOptions.IncludeMSMS Then
                    strLineOut &= " (includes both survey scans and fragmentation spectra)"
                Else
                    strLineOut &= " (includes only survey scans)"
                End If
                srOutFile.WriteLine(strLineOut)

                Dim intScanNumber As Integer
                If mOptions.RawDataExportOptions.RenumberScans Then
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
                    Dim sngIntensities() As Single
                    Dim intPointerArray() As Integer

                    ReDim sngIntensities(.IonCount - 1)
                    ReDim intPointerArray(.IonCount - 1)
                    For intIonIndex = 0 To .IonCount - 1
                        sngIntensities(intIonIndex) = .IonsIntensity(intIonIndex)
                        intPointerArray(intIonIndex) = intIonIndex
                    Next

                    ' Sort intPointerArray() based on the intensities in sngIntensities
                    Array.Sort(sngIntensities, intPointerArray)

                    Dim intStartIndex As Integer

                    If mOptions.RawDataExportOptions.MaxIonCountPerScan > 0 Then
                        ' Possibly limit the number of ions to intMaxIonCount
                        intStartIndex = .IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan
                        If intStartIndex < 0 Then intStartIndex = 0
                    Else
                        intStartIndex = 0
                    End If

                    ' Define the minimum data point intensity value
                    Dim sngMinimumIntensityCurrentScan = .IonsIntensity(intPointerArray(intStartIndex))

                    ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                    sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum)

                    ' If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update sngMinimumIntensityCurrentScan
                    If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                        sngMinimumIntensityCurrentScan = Math.Max(sngMinimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio)
                    End If

                    intExportCount = 0
                    For intIonIndex = 0 To .IonCount - 1
                        If .IonsIntensity(intIonIndex) >= sngMinimumIntensityCurrentScan Then
                            Dim strLineOut =
                                "1" & ControlChars.Tab &
                                "1" & ControlChars.Tab &
                                .IonsIntensity(intIonIndex) & ControlChars.Tab &
                                .IonsMZ(intIonIndex) & ControlChars.Tab &
                                "0"

                            srOutFile.WriteLine(strLineOut)
                            intExportCount += 1
                        End If
                    Next
                End If

                srOutFile.WriteLine("Number of peaks in spectrum = " & .IonCount.ToString())
                srOutFile.WriteLine("Number of isotopic distributions detected = " & intExportCount.ToString())
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
          ByRef intSpectrumExportCount As Integer)

            Select Case mOptions.RawDataExportOptions.FileFormat
                Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile
                    SavePEKFileToDiskWork(srDataOutFile, currentScan, objSpectraCache, strInputFileName, blnFragmentationScan, intSpectrumExportCount)
                Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
                    SaveCSVFilesToDiskWork(srDataOutFile, srScanInfoOutfile, currentScan, objSpectraCache, blnFragmentationScan, intSpectrumExportCount)
                Case Else
                    ' Unknown format
                    ' This code should never be reached
            End Select
        End Sub

    End Class

End Namespace
