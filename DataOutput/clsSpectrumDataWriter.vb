Imports MASIC.clsMASIC

Namespace DataOutput

    Public Class clsSpectrumDataWriter
        Inherits clsMasicEventNotifier

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
          inputFileName As String,
          outputDirectoryPath As String) As Boolean

            Dim outputFilePath = "??"

            Try
                Dim dataWriter As StreamWriter
                Dim scanInfoWriter As StreamWriter

                Select Case mOptions.RawDataExportOptions.FileFormat
                    Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile
                        outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.PEKFile)
                        dataWriter = New StreamWriter(outputFilePath)
                        scanInfoWriter = Nothing

                    Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
                        outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.DeconToolsIsosFile)

                        Dim outputFilePath2 = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.DeconToolsScansFile)

                        dataWriter = New StreamWriter(outputFilePath)
                        scanInfoWriter = New StreamWriter(outputFilePath2)

                        ' Write the file headers
                        mBPIWriter.WriteDecon2LSIsosFileHeaders(dataWriter)
                        mBPIWriter.WriteDecon2LSScanFileHeaders(scanInfoWriter)

                    Case Else
                        ' Unknown format
                        ReportError("Unknown raw data file format: " & mOptions.RawDataExportOptions.FileFormat.ToString())
                        Return False
                End Select

                Dim spectrumExportCount = 0

                If Not mOptions.RawDataExportOptions.IncludeMSMS AndAlso mOptions.RawDataExportOptions.RenumberScans Then
                    mOptions.RawDataExportOptions.RenumberScans = True
                Else
                    mOptions.RawDataExportOptions.RenumberScans = False
                End If

                UpdateProgress(0, "Exporting raw data")

                For masterOrderIndex = 0 To scanList.MasterScanOrderCount - 1
                    Dim scanPointer = scanList.MasterScanOrder(masterOrderIndex).ScanIndexPointer
                    If scanList.MasterScanOrder(masterOrderIndex).ScanType = clsScanList.eScanTypeConstants.SurveyScan Then
                        SaveRawDataToDiskWork(dataWriter, scanInfoWriter, scanList.SurveyScans(scanPointer), objSpectraCache, inputFileName, False, spectrumExportCount)
                    Else
                        If mOptions.RawDataExportOptions.IncludeMSMS OrElse
                          Not scanList.FragScans(scanPointer).MRMScanType = ThermoRawFileReader.MRMScanTypeConstants.NotMRM Then
                            ' Either we're writing out MS/MS data or this is an MRM scan
                            SaveRawDataToDiskWork(dataWriter, scanInfoWriter, scanList.FragScans(scanPointer), objSpectraCache, inputFileName, True, spectrumExportCount)
                        End If
                    End If

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

                If Not dataWriter Is Nothing Then dataWriter.Close()
                If Not scanInfoWriter Is Nothing Then scanInfoWriter.Close()

                Return True

            Catch ex As Exception
                ReportError("Error writing the raw spectra data to: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

        End Function


        Private Sub SaveCSVFilesToDiskWork(
          dataWriter As StreamWriter,
          scanInfoWriter As StreamWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache,
          fragmentationScan As Boolean,
          ByRef spectrumExportCount As Integer)

            Dim poolIndex As Integer
            Dim scanNumber As Integer
            Dim baselineNoiseLevel As Double

            If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, poolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                Exit Sub
            End If

            spectrumExportCount += 1

            With currentScan
                ' First, write an entry to the "_scans.csv" file

                If mOptions.RawDataExportOptions.RenumberScans Then
                    scanNumber = spectrumExportCount
                Else
                    scanNumber = .ScanNumber
                End If

                Dim msLevel As Integer
                If fragmentationScan Then
                    msLevel = .FragScanInfo.MSLevel
                Else
                    msLevel = 1
                End If

                Dim numIsotopicSignatures = 0
                Dim numPeaks = objSpectraCache.SpectraPool(poolIndex).IonCount

                baselineNoiseLevel = .BaselineNoiseStats.NoiseLevel
                If baselineNoiseLevel < 1 Then baselineNoiseLevel = 1

                ' Old Column Order:
                ''lineOut = scanNumber.ToString() & "," &
                ''             .ScanTime.ToString("0.0000") &
                ''             msLevel & "," &
                ''             numIsotopicSignatures & "," &
                ''             numPeaks & "," &
                ''             .TotalIonIntensity.ToString() & "," &
                ''             .BasePeakIonMZ & "," &
                ''             .BasePeakIonIntensity & "," &
                ''             timeDomainIntensity & "," &
                ''             peakIntensityThreshold & "," &
                ''             peptideIntensityThreshold

                mBPIWriter.WriteDecon2LSScanFileEntry(scanInfoWriter, currentScan, scanNumber, msLevel, numPeaks, numIsotopicSignatures)
            End With


            With objSpectraCache.SpectraPool(poolIndex)
                ' Now write an entry to the "_isos.csv" file

                If .IonCount > 0 Then
                    ' Populate intensities and pointerArray()

                    Dim intensities() As Double
                    Dim pointerArray() As Integer

                    ReDim intensities(.IonCount - 1)
                    ReDim pointerArray(.IonCount - 1)
                    For ionIndex = 0 To .IonCount - 1
                        intensities(ionIndex) = .IonsIntensity(ionIndex)
                        pointerArray(ionIndex) = ionIndex
                    Next

                    ' Sort pointerArray() based on the intensities in intensities
                    Array.Sort(intensities, pointerArray)

                    Dim startIndex As Integer
                    If mOptions.RawDataExportOptions.MaxIonCountPerScan > 0 Then
                        ' Possibly limit the number of ions to maxIonCount
                        startIndex = .IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan
                        If startIndex < 0 Then startIndex = 0
                    Else
                        startIndex = 0
                    End If

                    ' Define the minimum data point intensity value
                    Dim minimumIntensityCurrentScan = .IonsIntensity(pointerArray(startIndex))

                    ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                    minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum)

                    ' If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update minimumIntensityCurrentScan
                    If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                        minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio)
                    End If

                    For ionIndex = 0 To .IonCount - 1
                        If .IonsIntensity(ionIndex) >= minimumIntensityCurrentScan Then

                            Dim charge = 1
                            Dim isoFit = 0
                            Dim mass = clsUtilities.ConvoluteMass(.IonsMZ(ionIndex), 1, 0)
                            Dim peakFWHM = 0
                            Dim signalToNoise = .IonsIntensity(ionIndex) / baselineNoiseLevel
                            Dim monoisotopicAbu = -10
                            Dim monoPlus2Abu = -10

                            mBPIWriter.WriteDecon2LSIsosFileEntry(
                              dataWriter, scanNumber, charge,
                              .IonsIntensity(ionIndex), .IonsMZ(ionIndex),
                              isoFit, mass, mass, mass,
                              peakFWHM, signalToNoise, monoisotopicAbu, monoPlus2Abu)

                        End If
                    Next
                End If

            End With

        End Sub

        Private Sub SavePEKFileToDiskWork(
          writer As TextWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache,
          inputFileName As String,
          fragmentationScan As Boolean,
          ByRef spectrumExportCount As Integer)

            Dim poolIndex As Integer
            Dim exportCount = 0

            If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, poolIndex) Then
                SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                Exit Sub
            End If

            spectrumExportCount += 1

            With currentScan

                writer.WriteLine("Time domain signal level:" & ControlChars.Tab & .BasePeakIonIntensity.ToString())          ' Store the base peak ion intensity as the time domain signal level value

                writer.WriteLine("MASIC " & mOptions.MASICVersion)                     ' Software version
                Dim dataLine = "MS/MS-based PEK file"
                If mOptions.RawDataExportOptions.IncludeMSMS Then
                    dataLine &= " (includes both survey scans and fragmentation spectra)"
                Else
                    dataLine &= " (includes only survey scans)"
                End If
                writer.WriteLine(dataLine)

                Dim scanNumber As Integer
                If mOptions.RawDataExportOptions.RenumberScans Then
                    scanNumber = spectrumExportCount
                Else
                    scanNumber = .ScanNumber
                End If
                dataLine = "Filename: " & inputFileName & "." & scanNumber.ToString("00000")
                writer.WriteLine(dataLine)

                If fragmentationScan Then
                    writer.WriteLine("ScanType: Fragmentation Scan")
                Else
                    writer.WriteLine("ScanType: Survey Scan")
                End If

                writer.WriteLine("Charge state mass transform results:")
                writer.WriteLine("First CS,    Number of CS,   Abundance,   Mass,   Standard deviation")
            End With

            With objSpectraCache.SpectraPool(poolIndex)

                If .IonCount > 0 Then
                    ' Populate intensities and pointerArray()
                    Dim intensities() As Double
                    Dim pointerArray() As Integer

                    ReDim intensities(.IonCount - 1)
                    ReDim pointerArray(.IonCount - 1)
                    For ionIndex = 0 To .IonCount - 1
                        intensities(ionIndex) = .IonsIntensity(ionIndex)
                        pointerArray(ionIndex) = ionIndex
                    Next

                    ' Sort pointerArray() based on the intensities in intensities
                    Array.Sort(intensities, pointerArray)

                    Dim startIndex As Integer

                    If mOptions.RawDataExportOptions.MaxIonCountPerScan > 0 Then
                        ' Possibly limit the number of ions to maxIonCount
                        startIndex = .IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan
                        If startIndex < 0 Then startIndex = 0
                    Else
                        startIndex = 0
                    End If

                    ' Define the minimum data point intensity value
                    Dim minimumIntensityCurrentScan = .IonsIntensity(pointerArray(startIndex))

                    ' Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                    minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum)

                    ' If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update minimumIntensityCurrentScan
                    If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0 Then
                        minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio)
                    End If

                    exportCount = 0
                    For ionIndex = 0 To .IonCount - 1
                        If .IonsIntensity(ionIndex) >= minimumIntensityCurrentScan Then
                            Dim dataLine =
                                "1" & ControlChars.Tab &
                                "1" & ControlChars.Tab &
                                .IonsIntensity(ionIndex) & ControlChars.Tab &
                                .IonsMZ(ionIndex) & ControlChars.Tab &
                                "0"

                            writer.WriteLine(dataLine)
                            exportCount += 1
                        End If
                    Next
                End If

                writer.WriteLine("Number of peaks in spectrum = " & .IonCount.ToString())
                writer.WriteLine("Number of isotopic distributions detected = " & exportCount.ToString())
                writer.WriteLine()

            End With

        End Sub

        Private Sub SaveRawDataToDiskWork(
          dataWriter As StreamWriter,
          scanInfoWriter As StreamWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache,
          inputFileName As String,
          fragmentationScan As Boolean,
          ByRef spectrumExportCount As Integer)

            Select Case mOptions.RawDataExportOptions.FileFormat
                Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile
                    SavePEKFileToDiskWork(dataWriter, currentScan, objSpectraCache, inputFileName, fragmentationScan, spectrumExportCount)
                Case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile
                    SaveCSVFilesToDiskWork(dataWriter, scanInfoWriter, currentScan, objSpectraCache, fragmentationScan, spectrumExportCount)
                Case Else
                    ' Unknown format
                    ' This code should never be reached
            End Select
        End Sub

    End Class

End Namespace
