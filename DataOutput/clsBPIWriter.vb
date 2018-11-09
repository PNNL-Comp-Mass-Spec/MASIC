Imports MASIC.clsMASIC
Imports MASIC.DataOutput.clsDataOutput

Namespace DataOutput

    Public Class clsBPIWriter
        Inherits clsMasicEventNotifier

        Public Function SaveBPIs(
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          inputFilePathFull As String,
          outputDirectoryPath As String) As Boolean

            ' This function creates an ICR-2LS compatible .TIC file (using only the MS1 scans), plus
            ' two Decon2LS compatible .CSV files (one for the MS1 scans and one for the MS2, MS3, etc. scans)

            ' Note: Note that SaveExtendedScanStatsFiles() creates a tab-delimited text file with the BPI and TIC information for each scan

            Dim currentFilePath = "_MS_scans.csv"

            Try
                Dim bpiStepCount = 3

                UpdateProgress(0, "Saving chromatograms to disk")
                Dim stepsCompleted = 0

                Dim inputFileName = Path.GetFileName(inputFilePathFull)

                ' Disabled in April 2015 since not used
                '' First, write a true TIC file (in ICR-2LS format)
                'outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.ICRToolsTICChromatogramByScan)
                'LogMessage("Saving ICR Tools TIC to " & Path.GetFileName(outputFilePath))

                'SaveICRToolsChromatogramByScan(scanList.SurveyScans, scanList.SurveyScans.Count, outputFilePath, False, True, inputFilePathFull)

                stepsCompleted += 1
                UpdateProgress(CShort(stepsCompleted / bpiStepCount * 100))


                ' Second, write an MS-based _scans.csv file (readable with Decon2LS)
                Dim msScansFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.DeconToolsMSChromatogramFile)
                currentFilePath = String.Copy(msScansFilePath)

                ReportMessage("Saving Decon2LS MS Chromatogram File to " & Path.GetFileName(msScansFilePath))

                SaveDecon2LSChromatogram(scanList.SurveyScans, objSpectraCache, msScansFilePath)

                stepsCompleted += 1
                UpdateProgress(CShort(stepsCompleted / bpiStepCount * 100))


                ' Third, write an MSMS-based _scans.csv file (readable with Decon2LS)
                Dim msmsScansFilePath = ConstructOutputFilePath(inputFileName, outputDirectoryPath, eOutputFileTypeConstants.DeconToolsMSMSChromatogramFile)
                currentFilePath = String.Copy(msmsScansFilePath)

                ReportMessage("Saving Decon2LS MSMS Chromatogram File to " & Path.GetFileName(msmsScansFilePath))

                SaveDecon2LSChromatogram(scanList.FragScans, objSpectraCache, msmsScansFilePath)

                UpdateProgress(100)
                Return True

            Catch ex As Exception
                ReportError("Error writing the BPI to: " & currentFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

        End Function

        Private Sub SaveDecon2LSChromatogram(
          scanList As IEnumerable(Of clsScanInfo),
          objSpectraCache As clsSpectraCache,
          outputFilePath As String)

            Dim scansWritten = 0
            Dim lastStatus = DateTime.UtcNow

            Using writer = New StreamWriter(outputFilePath)

                ' Write the file headers
                WriteDecon2LSScanFileHeaders(writer)

                ' Step through the scans and write each one
                For Each scanItem In scanList
                    WriteDecon2LSScanFileEntry(writer, scanItem, objSpectraCache)

                    If scansWritten Mod 250 = 0 Then
                        UpdateCacheStats(objSpectraCache)

                        If DateTime.UtcNow.Subtract(lastStatus).TotalSeconds >= 30 Then
                            lastStatus = DateTime.UtcNow
                            ReportMessage(String.Format("  {0} / {1} scans processed", scansWritten, scanList.Count))
                        End If
                    End If

                    scansWritten += 1
                Next

            End Using

        End Sub

        <Obsolete("No longer used")>
        Private Sub SaveICRToolsChromatogramByScan(
          masicOptions As clsMASICOptions,
          scanList As IList(Of clsScanInfo),
          scanCount As Integer,
          outputFilePath As String,
          saveElutionTimeInsteadOfScan As Boolean,
          saveTICInsteadOfBPI As Boolean,
          inputFilePathFull As String)

            Using writer = New StreamWriter(outputFilePath)

                ' ReSharper disable StringLiteralTypo

                ' Write the Header text
                writer.WriteLine("ICR-2LS Data File (GA Anderson & JE Bruce); output from MASIC by Matthew E Monroe")
                writer.WriteLine("Version " & masicOptions.MASICVersion)
                writer.WriteLine("FileName:")
                If saveTICInsteadOfBPI Then
                    writer.WriteLine("title:" & Path.GetFileName(inputFilePathFull) & " TIC")
                    writer.WriteLine("Ytitle:Amplitude (TIC)")
                Else
                    writer.WriteLine("title:" & Path.GetFileName(inputFilePathFull) & " BPI")
                    writer.WriteLine("Ytitle:Amplitude (BPI)")
                End If

                If saveElutionTimeInsteadOfScan Then
                    writer.WriteLine("Xtitle:Time (Minutes)")
                Else
                    writer.WriteLine("Xtitle:Scan #")
                End If

                writer.WriteLine("Comment:")
                writer.WriteLine("LCQfilename: " & inputFilePathFull)
                writer.WriteLine()
                writer.WriteLine("CommentEnd")
                writer.WriteLine("FileType: 5 ")
                writer.WriteLine(" ValidTypes:1=Time,2=Freq,3=Mass;4=TimeSeriesWithCalibrationFn;5=XYPairs")
                writer.WriteLine("DataType: 3 ")
                writer.WriteLine(" ValidTypes:1=Integer,no header,2=Floats,Sun Extrel,3=Floats with header,4=Excite waveform")
                writer.WriteLine("Appodization: 0")
                writer.WriteLine(" ValidFunctions:0=Square,1=Parzen,2=Hanning,3=Welch")
                writer.WriteLine("ZeroFills: 0 ")

                ' Since we're using XY pairs, the buffer length needs to be two times scanCount
                Dim bufferLength = scanCount * 2
                If bufferLength < 1 Then bufferLength = 1

                writer.WriteLine("NumberOfSamples: " & bufferLength.ToString() & " ")
                writer.WriteLine("SampleRate: 1 ")
                writer.WriteLine("LowMassFreq: 0 ")
                writer.WriteLine("FreqShift: 0 ")
                writer.WriteLine("NumberSegments: 0 ")
                writer.WriteLine("MaxPoint: 0 ")
                writer.WriteLine("CalType: 0 ")
                writer.WriteLine("CalA: 108205311.2284 ")
                writer.WriteLine("CalB:-1767155067.018 ")
                writer.WriteLine("CalC: 29669467490280 ")
                writer.WriteLine("Intensity: 0 ")
                writer.WriteLine("CurrentXmin: 0 ")
                If scanCount > 0 Then
                    If saveElutionTimeInsteadOfScan Then
                        writer.WriteLine("CurrentXmax: " & scanList(scanCount - 1).ScanTime.ToString() & " ")
                    Else
                        writer.WriteLine("CurrentXmax: " & scanList(scanCount - 1).ScanNumber.ToString() & " ")
                    End If
                Else
                    writer.WriteLine("CurrentXmax: 0")
                End If

                writer.WriteLine("Tags:")
                writer.WriteLine("TagsEnd")
                writer.WriteLine("End")

                ' ReSharper restore StringLiteralTypo

            End Using

            ' Wait 500 msec, then re-open the file using Binary IO
            Threading.Thread.Sleep(500)

            Using writer = New BinaryWriter(New FileStream(outputFilePath, FileMode.Append))

                ' Write an Escape character (Byte 1B)
                writer.Write(CByte(27))

                For scanIndex = 0 To scanCount - 1
                    With scanList(scanIndex)
                        ' Note: Using CSng to assure that we write out single precision numbers
                        ' .TotalIonIntensity and .BasePeakIonIntensity are actually already singles

                        If saveElutionTimeInsteadOfScan Then
                            writer.Write(CSng(.ScanTime))
                        Else
                            writer.Write(CSng(.ScanNumber))
                        End If

                        If saveTICInsteadOfBPI Then
                            writer.Write(CSng(.TotalIonIntensity))
                        Else
                            writer.Write(CSng(.BasePeakIonIntensity))
                        End If

                    End With
                Next

            End Using

        End Sub

        Public Sub WriteDecon2LSIsosFileHeaders(srOutFile As StreamWriter)
            srOutFile.WriteLine("scan_num,charge,abundance,mz,fit,average_mw,monoisotopic_mw,mostabundant_mw,fwhm,signal_noise,mono_abundance,mono_plus2_abundance")
        End Sub

        Public Sub WriteDecon2LSIsosFileEntry(
          srIsosOutFile As StreamWriter,
          intScanNumber As Integer,
          intCharge As Integer,
          sngIntensity As Single,
          dblIonMZ As Double,
          sngFit As Single,
          dblAverageMass As Double,
          dblMonoisotopicMass As Double,
          dblMostAbundanctMass As Double,
          sngFWHM As Single,
          sngSignalToNoise As Single,
          sngMonoisotopicAbu As Single,
          sngMonoPlus2Abu As Single)

            Dim strLineOut As String

            strLineOut = intScanNumber & "," &
             intCharge & "," &
             sngIntensity & "," &
             dblIonMZ.ToString("0.00000") & "," &
             sngFit & "," &
             dblAverageMass.ToString("0.00000") & "," &
             dblMonoisotopicMass.ToString("0.00000") & "," &
             dblMostAbundanctMass.ToString("0.00000") & "," &
             sngFWHM & "," &
             sngSignalToNoise & "," &
             sngMonoisotopicAbu & "," &
             sngMonoPlus2Abu

            srIsosOutFile.WriteLine(strLineOut)

        End Sub

        Public Sub WriteDecon2LSScanFileHeaders(srOutFile As StreamWriter)
            srOutFile.WriteLine("scan_num,scan_time,type,bpi,bpi_mz,tic,num_peaks,num_deisotoped")

            ' Old Headers:      "scan_num,time,type,num_isotopic_signatures,num_peaks,tic,bpi_mz,bpi,time_domain_signal,peak_intensity_threshold,peptide_intensity_threshold")

        End Sub

        Private Sub WriteDecon2LSScanFileEntry(
          writer As StreamWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache)

            Dim numPeaks As Integer

            If objSpectraCache Is Nothing Then
                numPeaks = 0
            Else
                Dim poolIndex As Integer
                If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, poolIndex) Then
                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                    Exit Sub
                End If
                numPeaks = objSpectraCache.SpectraPool(poolIndex).IonCount
            End If

            Dim scanNumber = currentScan.ScanNumber

            Dim msLevel = currentScan.FragScanInfo.MSLevel
            If msLevel < 1 Then msLevel = 1

            Dim numIsotopicSignatures = 0

            WriteDecon2LSScanFileEntry(writer, currentScan, scanNumber, msLevel, numPeaks, numIsotopicSignatures)

        End Sub

        Public Sub WriteDecon2LSScanFileEntry(
          srScanInfoOutFile As StreamWriter,
          currentScan As clsScanInfo,
          intScanNumber As Integer,
          intMSLevel As Integer,
          intNumPeaks As Integer,
          intNumIsotopicSignatures As Integer)

            Dim strLineOut As String

            With currentScan
                strLineOut = intScanNumber.ToString() & "," &
                 .ScanTime.ToString("0.0000") & "," &
                 intMSLevel & "," &
                 .BasePeakIonIntensity & "," &
                 .BasePeakIonMZ.ToString("0.00000") & "," &
                 .TotalIonIntensity.ToString() & "," &
                 intNumPeaks & "," &
                 intNumIsotopicSignatures
            End With

            srScanInfoOutFile.WriteLine(strLineOut)

        End Sub

    End Class

End Namespace
