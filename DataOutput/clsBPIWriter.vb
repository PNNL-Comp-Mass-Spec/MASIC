Imports MASIC.clsMASIC
Imports MASIC.DataOutput.clsDataOutput

Namespace DataOutput

    Public Class clsBPIWriter
        Inherits clsEventNotifier

        Public Function SaveBPIs(
          scanList As clsScanList,
          objSpectraCache As clsSpectraCache,
          strInputFilePathFull As String,
          strOutputFolderPath As String) As Boolean

            ' This function creates an ICR-2LS compatible .TIC file (using only the MS1 scans), plus
            ' two Decon2LS compatible .CSV files (one for the MS1 scans and one for the MS2, MS3, etc. scans)

            ' Note: Note that SaveExtendedScanStatsFiles() creates a tab-delimited text file with the BPI and TIC information for each scan

            Dim intBPIStepCount As Integer
            Dim intStepsCompleted As Integer

            Dim strInputFileName As String
            Dim strOutputFilePath As String = String.Empty

            Dim blnSuccess As Boolean

            Try
                intBPIStepCount = 3

                UpdateProgress(0, "Saving chromatograms to disk")
                intStepsCompleted = 0

                strInputFileName = Path.GetFileName(strInputFilePathFull)

                ' Disabled in April 2015 since not used
                '' First, write a true TIC file (in ICR-2LS format)
                'strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.ICRToolsTICChromatogramByScan)
                'LogMessage("Saving ICR Tools TIC to " & Path.GetFileName(strOutputFilePath))

                'SaveICRToolsChromatogramByScan(scanList.SurveyScans, scanList.SurveyScans.Count, strOutputFilePath, False, True, strInputFilePathFull)

                intStepsCompleted += 1
                UpdateProgress(CShort(intStepsCompleted / intBPIStepCount * 100))


                ' Second, write an MS-based _scans.csv file (readable with Decon2LS)
                strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsMSChromatogramFile)
                ReportMessage("Saving Decon2LS MS Chromatogram File to " & Path.GetFileName(strOutputFilePath))

                SaveDecon2LSChromatogram(scanList.SurveyScans, objSpectraCache, strOutputFilePath)

                intStepsCompleted += 1
                UpdateProgress(CShort(intStepsCompleted / intBPIStepCount * 100))


                ' Third, write an MSMS-based _scans.csv file (readable with Decon2LS)
                strOutputFilePath = clsDataOutput.ConstructOutputFilePath(strInputFileName, strOutputFolderPath, eOutputFileTypeConstants.DeconToolsMSMSChromatogramFile)
                ReportMessage("Saving Decon2LS MSMS Chromatogram File to " & Path.GetFileName(strOutputFilePath))

                SaveDecon2LSChromatogram(scanList.FragScans, objSpectraCache, strOutputFilePath)

                UpdateProgress(100)
                blnSuccess = True

            Catch ex As Exception
                ReportError("SaveBPIs", "Error writing the BPI to: " & strOutputFilePath, ex, True, True, eMasicErrorCodes.OutputFileWriteError)
                blnSuccess = False
            End Try

            Return blnSuccess
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
                        srOutFile.WriteLine(Math.Round(.ScanTime, 5).ToString() & cColDelimiter &
                          Math.Round(.TotalIonIntensity, 2).ToString)
                    Else
                        srOutFile.WriteLine(Math.Round(.ScanTime, 5).ToString() & cColDelimiter &
                          Math.Round(.BasePeakIonIntensity, 2).ToString() & cColDelimiter &
                          Math.Round(.BasePeakIonMZ, 4).ToString)
                    End If

                End With
            Next intScanIndex

            srOutFile.Close()

        End Sub

        Private Sub SaveDecon2LSChromatogram(
          scanList As IList(Of clsScanInfo),
          objSpectraCache As clsSpectraCache,
          strOutputFilePath As String)

            Dim srScanInfoOutfile As StreamWriter
            Dim scansWritten As Integer = 0

            srScanInfoOutfile = New StreamWriter(strOutputFilePath)

            ' Write the file headers
            WriteDecon2LSScanFileHeaders(srScanInfoOutfile)

            ' Step through the scans and write each one
            For Each scanItem In scanList
                WriteDecon2LSScanFileEntry(srScanInfoOutfile, scanItem, objSpectraCache)

                If scansWritten Mod 250 = 0 Then
                    UpdateCacheStats(objSpectraCache)
                End If
                scansWritten += 1
            Next

            srScanInfoOutfile.Close()

        End Sub

        <Obsolete("No longer used")>
        Private Sub SaveICRToolsChromatogramByScan(
          masicOptions As clsMASICOptions,
          scanList As IList(Of clsScanInfo),
          intScanCount As Integer,
          strOutputFilePath As String,
          blnSaveElutionTimeInsteadOfScan As Boolean,
          blnSaveTICInsteadOfBPI As Boolean,
          strInputFilePathFull As String)

            Dim srOutFile As StreamWriter
            Dim fsBinaryOutStream As FileStream
            Dim srBinaryOut As BinaryWriter

            Dim intBufferLength As Integer
            Dim intScanIndex As Integer

            srOutFile = New StreamWriter(strOutputFilePath)

            ' Write the Header text
            srOutFile.WriteLine("ICR-2LS Data File (GA Anderson & JE Bruce); output from MASIC by Matthew E Monroe")
            srOutFile.WriteLine("Version " & masicOptions.MASICVersion)
            srOutFile.WriteLine("FileName:")
            If blnSaveTICInsteadOfBPI Then
                srOutFile.WriteLine("title:" & Path.GetFileName(strInputFilePathFull) & " TIC")
                srOutFile.WriteLine("Ytitle:Amplitude (TIC)")
            Else
                srOutFile.WriteLine("title:" & Path.GetFileName(strInputFilePathFull) & " BPI")
                srOutFile.WriteLine("Ytitle:Amplitude (BPI)")
            End If

            If blnSaveElutionTimeInsteadOfScan Then
                srOutFile.WriteLine("Xtitle:Time (Minutes)")
            Else
                srOutFile.WriteLine("Xtitle:Scan #")
            End If

            srOutFile.WriteLine("Comment:")
            srOutFile.WriteLine("LCQfilename: " & strInputFilePathFull)
            srOutFile.WriteLine()
            srOutFile.WriteLine("CommentEnd")
            srOutFile.WriteLine("FileType: 5 ")
            srOutFile.WriteLine(" ValidTypes:1=Time,2=Freq,3=Mass;4=TimeSeriesWithCalibrationFn;5=XYPairs")
            srOutFile.WriteLine("DataType: 3 ")
            srOutFile.WriteLine(" ValidTypes:1=Integer,no header,2=Floats,Sun Extrel,3=Floats with header,4=Excite waveform")
            srOutFile.WriteLine("Appodization: 0")
            srOutFile.WriteLine(" ValidFunctions:0=Square,1=Parzen,2=Hanning,3=Welch")
            srOutFile.WriteLine("ZeroFills: 0 ")

            ' Since we're using XY pairs, the buffer length needs to be two times intScanCount
            intBufferLength = intScanCount * 2
            If intBufferLength < 1 Then intBufferLength = 1

            srOutFile.WriteLine("NumberOfSamples: " & intBufferLength.ToString() & " ")
            srOutFile.WriteLine("SampleRate: 1 ")
            srOutFile.WriteLine("LowMassFreq: 0 ")
            srOutFile.WriteLine("FreqShift: 0 ")
            srOutFile.WriteLine("NumberSegments: 0 ")
            srOutFile.WriteLine("MaxPoint: 0 ")
            srOutFile.WriteLine("CalType: 0 ")
            srOutFile.WriteLine("CalA: 108205311.2284 ")
            srOutFile.WriteLine("CalB:-1767155067.018 ")
            srOutFile.WriteLine("CalC: 29669467490280 ")
            srOutFile.WriteLine("Intensity: 0 ")
            srOutFile.WriteLine("CurrentXmin: 0 ")
            If intScanCount > 0 Then
                If blnSaveElutionTimeInsteadOfScan Then
                    srOutFile.WriteLine("CurrentXmax: " & scanList(intScanCount - 1).ScanTime.ToString() & " ")
                Else
                    srOutFile.WriteLine("CurrentXmax: " & scanList(intScanCount - 1).ScanNumber.ToString() & " ")
                End If
            Else
                srOutFile.WriteLine("CurrentXmax: 0")
            End If

            srOutFile.WriteLine("Tags:")
            srOutFile.WriteLine("TagsEnd")
            srOutFile.WriteLine("End")

            srOutFile.Close()

            ' Wait 500 msec, then re-open the file using Binary IO
            Threading.Thread.Sleep(500)

            fsBinaryOutStream = New FileStream(strOutputFilePath, FileMode.Append)
            srBinaryOut = New BinaryWriter(fsBinaryOutStream)


            ' Write an Escape character (Byte 1B)
            srBinaryOut.Write(CByte(27))

            For intScanIndex = 0 To intScanCount - 1
                With scanList(intScanIndex)
                    ' Note: Using CSng to assure that we write out single precision numbers
                    ' .TotalIonIntensity and .BasePeakIonIntensity are actually already singles

                    If blnSaveElutionTimeInsteadOfScan Then
                        srBinaryOut.Write(CSng(.ScanTime))
                    Else
                        srBinaryOut.Write(CSng(.ScanNumber))
                    End If

                    If blnSaveTICInsteadOfBPI Then
                        srBinaryOut.Write(CSng(.TotalIonIntensity))
                    Else
                        srBinaryOut.Write(CSng(.BasePeakIonIntensity))
                    End If

                End With
            Next intScanIndex

            srBinaryOut.Close()

        End Sub

        Private Sub WriteDecon2LSIsosFileHeaders(srOutFile As StreamWriter)
            srOutFile.WriteLine("scan_num,charge,abundance,mz,fit,average_mw,monoisotopic_mw,mostabundant_mw,fwhm,signal_noise,mono_abundance,mono_plus2_abundance")
        End Sub

        Private Sub WriteDecon2LSIsosFileEntry(
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

        Private Sub WriteDecon2LSScanFileHeaders(srOutFile As StreamWriter)
            srOutFile.WriteLine("scan_num,scan_time,type,bpi,bpi_mz,tic,num_peaks,num_deisotoped")

            ' Old Headers:      "scan_num,time,type,num_isotopic_signatures,num_peaks,tic,bpi_mz,bpi,time_domain_signal,peak_intensity_threshold,peptide_intensity_threshold")

        End Sub

        Private Sub WriteDecon2LSScanFileEntry(
          srScanInfoOutFile As StreamWriter,
          currentScan As clsScanInfo,
          objSpectraCache As clsSpectraCache)

            Dim intPoolIndex As Integer
            Dim intNumPeaks As Integer

            Dim intScanNumber As Integer
            Dim intMSLevel As Integer
            Dim intNumIsotopicSignatures As Integer

            If objSpectraCache Is Nothing Then
                intNumPeaks = 0
            Else
                If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
                    SetLocalErrorCode(eMasicErrorCodes.ErrorUncachingSpectrum)
                    Exit Sub
                End If
                intNumPeaks = objSpectraCache.SpectraPool(intPoolIndex).IonCount
            End If

            intScanNumber = currentScan.ScanNumber

            intMSLevel = currentScan.FragScanInfo.MSLevel
            If intMSLevel < 1 Then intMSLevel = 1

            intNumIsotopicSignatures = 0

            WriteDecon2LSScanFileEntry(srScanInfoOutFile, currentScan, intScanNumber, intMSLevel, intNumPeaks, intNumIsotopicSignatures)

        End Sub

        Private Sub WriteDecon2LSScanFileEntry(
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
