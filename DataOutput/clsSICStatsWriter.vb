Imports System.Runtime.InteropServices
Imports MASIC.clsMASIC
Imports PRISM

Namespace DataOutput
    Public Class clsSICStatsWriter
        Inherits clsMasicEventNotifier

        Private Function GetFakeParentIonForFragScan(scanList As clsScanList, fragScanIndex As Integer) As clsParentIonInfo

            Dim currentFragScan = scanList.FragScans(fragScanIndex)

            Dim newParentIon = New clsParentIonInfo(currentFragScan.BasePeakIonMZ) With {
                .SurveyScanIndex = 0
            }

            ' Find the previous MS1 scan that occurs before the frag scan
            Dim surveyScanNumberAbsolute = currentFragScan.ScanNumber - 1

            newParentIon.FragScanIndices.Add(fragScanIndex)

            If scanList.MasterScanOrderCount > 0 Then
                Dim surveyScanIndexMatch = clsBinarySearch.BinarySearchFindNearest(scanList.MasterScanNumList, surveyScanNumberAbsolute, scanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)

                While surveyScanIndexMatch >= 0 AndAlso scanList.MasterScanOrder(surveyScanIndexMatch).ScanType = clsScanList.eScanTypeConstants.FragScan
                    surveyScanIndexMatch -= 1
                End While

                If surveyScanIndexMatch < 0 Then
                    ' Did not find the previous survey scan; find the next survey scan
                    surveyScanIndexMatch += 1
                    While surveyScanIndexMatch < scanList.MasterScanOrderCount AndAlso scanList.MasterScanOrder(surveyScanIndexMatch).ScanType = clsScanList.eScanTypeConstants.FragScan
                        surveyScanIndexMatch += 1
                    End While

                    If surveyScanIndexMatch >= scanList.MasterScanOrderCount Then
                        surveyScanIndexMatch = 0
                    End If
                End If

                newParentIon.SurveyScanIndex = scanList.MasterScanOrder(surveyScanIndexMatch).ScanIndexPointer
            End If

            If newParentIon.SurveyScanIndex < scanList.SurveyScans.Count Then
                newParentIon.OptimalPeakApexScanNumber = scanList.SurveyScans(newParentIon.SurveyScanIndex).ScanNumber
            Else
                newParentIon.OptimalPeakApexScanNumber = surveyScanNumberAbsolute
            End If

            newParentIon.PeakApexOverrideParentIonIndex = -1

            newParentIon.SICStats.ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan
            newParentIon.SICStats.PeakScanIndexStart = fragScanIndex
            newParentIon.SICStats.PeakScanIndexEnd = fragScanIndex
            newParentIon.SICStats.PeakScanIndexMax = fragScanIndex

            With newParentIon.SICStats.Peak
                .MaxIntensityValue = currentFragScan.BasePeakIonIntensity
                .SignalToNoiseRatio = 1
                .FWHMScanWidth = 1
                .Area = currentFragScan.BasePeakIonIntensity
                .ParentIonIntensity = currentFragScan.BasePeakIonIntensity
            End With

            Return newParentIon

        End Function

        Private Sub PopulateScanListPointerArray(
          surveyScans As IList(Of clsScanInfo),
          surveyScanCount As Integer,
          <Out> ByRef scanListArray() As Integer)

            Dim index As Integer

            If surveyScanCount > 0 Then
                ReDim scanListArray(surveyScanCount - 1)

                For index = 0 To surveyScanCount - 1
                    scanListArray(index) = surveyScans(index).ScanNumber
                Next
            Else
                ReDim scanListArray(0)
            End If

        End Sub

        Public Function SaveSICStatsFlatFile(
          scanList As clsScanList,
          inputFileName As String,
          outputDirectoryPath As String,
          masicOptions As clsMASICOptions,
          dataOutputHandler As clsDataOutput) As Boolean

            ' Writes out a flat file containing identified peaks and statistics

            Dim outputFilePath As String = String.Empty

            Const cColDelimiter As Char = ControlChars.Tab

            Dim scanListArray() As Integer = Nothing

            ' Populate scanListArray with the scan numbers in scanList.SurveyScans
            PopulateScanListPointerArray(scanList.SurveyScans, scanList.SurveyScans.Count, scanListArray)

            Try

                UpdateProgress(0, "Saving SIC data to flat file")

                outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.SICStatsFlatFile)
                ReportMessage("Saving SIC flat file to disk: " & Path.GetFileName(outputFilePath))

                Using writer = New StreamWriter(outputFilePath, False)

                    ' Write the SIC stats to the output file
                    ' The file is tab delimited

                    Dim includeScanTimesInSICStatsFile = masicOptions.IncludeScanTimesInSICStatsFile

                    If masicOptions.IncludeHeadersInExportFile Then
                        writer.WriteLine(dataOutputHandler.GetHeadersForOutputFile(scanList, clsDataOutput.eOutputFileTypeConstants.SICStatsFlatFile, cColDelimiter))
                    End If

                    If scanList.SurveyScans.Count = 0 AndAlso scanList.ParentIons.Count = 0 Then
                        ' Write out fake values to the _SICStats.txt file so that downstream software can still access some of the information
                        For fragScanIndex = 0 To scanList.FragScans.Count - 1

                            Dim fakeParentIon = GetFakeParentIonForFragScan(scanList, fragScanIndex)
                            Dim parentIonIndex = 0

                            Dim surveyScanNumber As Integer
                            Dim surveyScanTime As Single

                            WriteSICStatsFlatFileEntry(writer, cColDelimiter, masicOptions.SICOptions, scanList,
                                                   fakeParentIon, parentIonIndex, surveyScanNumber, surveyScanTime,
                                                   0, includeScanTimesInSICStatsFile)
                        Next
                    Else

                        For parentIonIndex = 0 To scanList.ParentIons.Count - 1
                            Dim includeParentIon As Boolean

                            If masicOptions.CustomSICList.LimitSearchToCustomMZList Then
                                includeParentIon = scanList.ParentIons(parentIonIndex).CustomSICPeak
                            Else
                                includeParentIon = True
                            End If

                            If includeParentIon Then
                                For fragScanIndex = 0 To scanList.ParentIons(parentIonIndex).FragScanIndices.Count - 1

                                    Dim parentIon = scanList.ParentIons(parentIonIndex)
                                    Dim surveyScanNumber As Integer
                                    Dim surveyScanTime As Single

                                    If parentIon.SurveyScanIndex >= 0 AndAlso parentIon.SurveyScanIndex < scanList.SurveyScans.Count Then
                                        surveyScanNumber = scanList.SurveyScans(parentIon.SurveyScanIndex).ScanNumber
                                        surveyScanTime = scanList.SurveyScans(parentIon.SurveyScanIndex).ScanTime
                                    Else
                                        surveyScanNumber = -1
                                        surveyScanTime = 0
                                    End If

                                    WriteSICStatsFlatFileEntry(writer, cColDelimiter, masicOptions.SICOptions, scanList,
                                                           parentIon, parentIonIndex, surveyScanNumber, surveyScanTime,
                                                           fragScanIndex, includeScanTimesInSICStatsFile)

                                Next
                            End If

                            If scanList.ParentIons.Count > 1 Then
                                If parentIonIndex Mod 100 = 0 Then
                                    UpdateProgress(CShort(parentIonIndex / (scanList.ParentIons.Count - 1) * 100))
                                End If
                            Else
                                UpdateProgress(1)
                            End If
                            If masicOptions.AbortProcessing Then
                                scanList.ProcessingIncomplete = True
                                Exit For
                            End If
                        Next

                    End If

                End Using

            Catch ex As Exception
                Console.WriteLine(ex.StackTrace)
                ReportError("Error writing the Peak Stats to: " & outputFilePath, ex, eMasicErrorCodes.OutputFileWriteError)
                Return False
            End Try

            Return True

        End Function

        Private Function ScanNumberToScanTime(
          scanList As clsScanList,
          scanNumber As Integer) As Single

            Dim surveyScanMatches = (From item In scanList.SurveyScans Where item.ScanNumber = scanNumber Select item).ToList()

            If surveyScanMatches.Count > 0 Then
                Return surveyScanMatches.First.ScanTime
            End If

            Dim fragScanMatches = (From item In scanList.FragScans Where item.ScanNumber = scanNumber Select item).ToList()
            If fragScanMatches.Count > 0 Then
                Return fragScanMatches.First.ScanTime
            End If

            Return 0

        End Function

        Private Sub WriteSICStatsFlatFileEntry(
          sicStatsWriter As TextWriter,
          cColDelimiter As Char,
          sicOptions As clsSICOptions,
          scanList As clsScanList,
          parentIon As clsParentIonInfo,
          parentIonIndex As Integer,
          surveyScanNumber As Integer,
          surveyScanTime As Single,
          fragScanIndex As Integer,
          includeScanTimesInSICStatsFile As Boolean)

            Dim dataValues = New List(Of String)

            Dim fragScanTime As Single = 0
            Dim optimalPeakApexScanTime As Single = 0

            dataValues.Add(sicOptions.DatasetNumber.ToString())             ' Dataset number
            dataValues.Add(parentIonIndex.ToString())                       ' Parent Ion Index

            dataValues.Add(StringUtilities.DblToString(parentIon.MZ, 4))    ' MZ

            dataValues.Add(surveyScanNumber.ToString())                     ' Survey scan number

            Dim interferenceScore As Double

            If fragScanIndex < scanList.FragScans.Count Then
                dataValues.Add(scanList.FragScans(parentIon.FragScanIndices(fragScanIndex)).ScanNumber.ToString())  ' Fragmentation scan number
                interferenceScore = scanList.FragScans(parentIon.FragScanIndices(fragScanIndex)).FragScanInfo.InterferenceScore
            Else
                dataValues.Add("0")    ' Fragmentation scan does not exist
                interferenceScore = 0
            End If

            dataValues.Add(parentIon.OptimalPeakApexScanNumber.ToString())                ' Optimal peak apex scan number

            If includeScanTimesInSICStatsFile Then
                If fragScanIndex < scanList.FragScans.Count Then
                    fragScanTime = scanList.FragScans(parentIon.FragScanIndices(fragScanIndex)).ScanTime
                Else
                    fragScanTime = 0                ' Fragmentation scan does not exist
                End If

                optimalPeakApexScanTime = ScanNumberToScanTime(scanList, parentIon.OptimalPeakApexScanNumber)
            End If

            dataValues.Add(parentIon.PeakApexOverrideParentIonIndex.ToString())           ' Parent Ion Index that supplied the optimal peak apex scan number
            If parentIon.CustomSICPeak Then
                dataValues.Add("1")   ' Custom SIC peak, record 1
            Else
                dataValues.Add("0")   ' Not a Custom SIC peak, record 0
            End If

            With parentIon.SICStats
                If .ScanTypeForPeakIndices = clsScanList.eScanTypeConstants.FragScan Then
                    dataValues.Add(scanList.FragScans(.PeakScanIndexStart).ScanNumber.ToString())    ' Peak Scan Start
                    dataValues.Add(scanList.FragScans(.PeakScanIndexEnd).ScanNumber.ToString())      ' Peak Scan End
                    dataValues.Add(scanList.FragScans(.PeakScanIndexMax).ScanNumber.ToString())      ' Peak Scan Max Intensity
                Else
                    dataValues.Add(scanList.SurveyScans(.PeakScanIndexStart).ScanNumber.ToString())  ' Peak Scan Start
                    dataValues.Add(scanList.SurveyScans(.PeakScanIndexEnd).ScanNumber.ToString())    ' Peak Scan End
                    dataValues.Add(scanList.SurveyScans(.PeakScanIndexMax).ScanNumber.ToString())    ' Peak Scan Max Intensity
                End If

                With .Peak
                    dataValues.Add(StringUtilities.ValueToString(.MaxIntensityValue, 5))          ' Peak Intensity
                    dataValues.Add(StringUtilities.ValueToString(.SignalToNoiseRatio, 4))         ' Peak signal to noise ratio
                    dataValues.Add(.FWHMScanWidth.ToString())                                     ' Full width at half max (in scans)
                    dataValues.Add(StringUtilities.ValueToString(.Area, 5))                       ' Peak area

                    dataValues.Add(StringUtilities.ValueToString(.ParentIonIntensity, 5))         ' Intensity of the parent ion (just before the fragmentation scan)
                    With .BaselineNoiseStats
                        dataValues.Add(StringUtilities.ValueToString(.NoiseLevel, 5))
                        dataValues.Add(StringUtilities.ValueToString(.NoiseStDev, 3))
                        dataValues.Add(.PointsUsed.ToString())
                    End With

                    With .StatisticalMoments
                        dataValues.Add(StringUtilities.ValueToString(.Area, 5))
                        dataValues.Add(.CenterOfMassScan.ToString())
                        dataValues.Add(StringUtilities.ValueToString(.StDev, 3))
                        dataValues.Add(StringUtilities.ValueToString(.Skew, 4))
                        dataValues.Add(StringUtilities.ValueToString(.KSStat, 4))
                        dataValues.Add(.DataCountUsed.ToString())
                    End With

                End With
            End With

            dataValues.Add(StringUtilities.ValueToString(interferenceScore, 4))     ' Interference Score

            If includeScanTimesInSICStatsFile Then
                dataValues.Add(StringUtilities.DblToString(surveyScanTime, 5))         ' SurveyScanTime
                dataValues.Add(StringUtilities.DblToString(fragScanTime, 5))              ' FragScanTime
                dataValues.Add(StringUtilities.DblToString(optimalPeakApexScanTime, 5))   ' OptimalPeakApexScanTime
            End If

            sicStatsWriter.WriteLine(String.Join(cColDelimiter, dataValues))

        End Sub


    End Class

End Namespace
