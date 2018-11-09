Imports PRISM

Public Class clsScanNumScanTimeConversion
    Inherits EventNotifier

    ''' <summary>
    ''' Returns the index of the scan closest to scanOrAcqTime (searching both Survey and Frag Scans using the MasterScanList)
    ''' </summary>
    ''' <param name="scanList"></param>
    ''' <param name="scanOrAcqTime">can be absolute, relative, or AcquisitionTime</param>
    ''' <param name="eScanType">Specifies what type of value scanOrAcqTime is; 0=absolute, 1=relative, 2=acquisition time (aka elution time)</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function FindNearestScanNumIndex(
      scanList As clsScanList,
      scanOrAcqTime As Single,
      eScanType As clsCustomSICList.eCustomSICScanTypeConstants) As Integer

        Try
            Dim scanIndexMatch As Integer

            If eScanType = clsCustomSICList.eCustomSICScanTypeConstants.Absolute Or eScanType = clsCustomSICList.eCustomSICScanTypeConstants.Relative Then
                Dim absoluteScanNumber = ScanOrAcqTimeToAbsolute(scanList, scanOrAcqTime, eScanType, False)
                scanIndexMatch = clsBinarySearch.BinarySearchFindNearest(scanList.MasterScanNumList, absoluteScanNumber, scanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)
            Else
                ' eScanType = eCustomSICScanTypeConstants.AcquisitionTime
                ' Find the closest match in scanList.MasterScanTimeList
                scanIndexMatch = clsBinarySearch.BinarySearchFindNearest(scanList.MasterScanTimeList, scanOrAcqTime, scanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)
            End If

            Return scanIndexMatch

        Catch ex As Exception
            OnErrorEvent("Error in FindNearestScanNumIndex", ex)
            Return 0
        End Try

    End Function

    Public Function FindNearestSurveyScanIndex(
      scanList As clsScanList,
      scanOrAcqTime As Single,
      eScanType As clsCustomSICList.eCustomSICScanTypeConstants) As Integer

        ' Finds the index of the survey scan closest to scanOrAcqTime
        ' Note that scanOrAcqTime can be absolute, relative, or AcquisitionTime; eScanType specifies which it is

        Try
            Dim surveyScanIndexMatch = -1
            Dim scanNumberToFind = ScanOrAcqTimeToAbsolute(scanList, scanOrAcqTime, eScanType, False)

            For index = 0 To scanList.SurveyScans.Count - 1
                If scanList.SurveyScans(index).ScanNumber >= scanNumberToFind Then
                    surveyScanIndexMatch = index
                    If scanList.SurveyScans(index).ScanNumber <> scanNumberToFind AndAlso index < scanList.SurveyScans.Count - 1 Then
                        ' Didn't find an exact match; determine which survey scan is closer
                        If Math.Abs(scanList.SurveyScans(index + 1).ScanNumber - scanNumberToFind) <
                           Math.Abs(scanList.SurveyScans(index).ScanNumber - scanNumberToFind) Then
                            surveyScanIndexMatch += 1
                        End If
                    End If
                    Exit For
                End If
            Next

            If surveyScanIndexMatch < 0 Then
                ' Match not found; return either the first or the last survey scan
                If scanList.SurveyScans.Count > 0 Then
                    surveyScanIndexMatch = scanList.SurveyScans.Count - 1
                Else
                    surveyScanIndexMatch = 0
                End If
            End If

            Return surveyScanIndexMatch
        Catch ex As Exception
            OnErrorEvent("Error in FindNearestSurveyScanIndex", ex)
            Return 0
        End Try

    End Function

    ''' <summary>
    ''' Converts a scan number of acquisition time to an actual scan number
    ''' </summary>
    ''' <param name="scanList"></param>
    ''' <param name="scanOrAcqTime">Value to convert</param>
    ''' <param name="eScanType">Type of the value to convert; 0=Absolute, 1=Relative, 2=Acquisition Time (aka elution time)</param>
    ''' <param name="convertingRangeOrTolerance">True when converting a range</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ScanOrAcqTimeToAbsolute(
      scanList As clsScanList,
      scanOrAcqTime As Single,
      eScanType As clsCustomSICList.eCustomSICScanTypeConstants,
      convertingRangeOrTolerance As Boolean) As Integer

        Try
            Dim absoluteScanNumber As Integer

            Select Case eScanType
                Case clsCustomSICList.eCustomSICScanTypeConstants.Absolute
                    ' scanOrAcqTime is an absolute scan number (or range of scan numbers)
                    ' No conversion needed; simply return the value
                    absoluteScanNumber = CInt(scanOrAcqTime)

                Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                    ' scanOrAcqTime is a fraction of the total number of scans (for example, 0.5)

                    ' Use the total range of scan numbers
                    With scanList
                        If .MasterScanOrderCount > 0 Then
                            Dim totalScanRange = .MasterScanNumList(.MasterScanOrderCount - 1) - .MasterScanNumList(0)

                            absoluteScanNumber = CInt(scanOrAcqTime * totalScanRange + .MasterScanNumList(0))
                        Else
                            absoluteScanNumber = 0
                        End If
                    End With

                Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                    ' scanOrAcqTime is an elution time value
                    ' If convertingRangeOrTolerance = False, then look for the scan that is nearest to scanOrAcqTime
                    ' If convertingRangeOrTolerance = True, then Convert scanOrAcqTime to a relative scan range and then
                    '   call this function again with that relative time

                    If convertingRangeOrTolerance Then

                        Dim totalRunTime = scanList.MasterScanTimeList(scanList.MasterScanOrderCount - 1) - scanList.MasterScanTimeList(0)
                        If totalRunTime < 0.1 Then
                            totalRunTime = 1
                        End If

                        Dim relativeTime = scanOrAcqTime / totalRunTime

                        absoluteScanNumber = ScanOrAcqTimeToAbsolute(scanList, relativeTime, clsCustomSICList.eCustomSICScanTypeConstants.Relative, True)
                    Else
                        Dim masterScanIndex = FindNearestScanNumIndex(scanList, scanOrAcqTime, eScanType)
                        If masterScanIndex >= 0 AndAlso scanList.MasterScanOrderCount > 0 Then
                            absoluteScanNumber = scanList.MasterScanNumList(masterScanIndex)
                        End If
                    End If


                Case Else
                    ' Unknown type; assume absolute scan number
                    absoluteScanNumber = CInt(scanOrAcqTime)
            End Select


            Return absoluteScanNumber
        Catch ex As Exception
            OnErrorEvent("Error in clsMasic->ScanOrAcqTimeToAbsolute", ex)
            Return 0
        End Try

    End Function

    Public Function ScanOrAcqTimeToScanTime(
      scanList As clsScanList,
      scanOrAcqTime As Single,
      eScanType As clsCustomSICList.eCustomSICScanTypeConstants,
      convertingRangeOrTolerance As Boolean) As Single

        Try
            Dim computedScanTime As Single

            Select Case eScanType
                Case clsCustomSICList.eCustomSICScanTypeConstants.Absolute
                    ' scanOrAcqTime is an absolute scan number (or range of scan numbers)

                    ' If convertingRangeOrTolerance = False, then look for the scan that is nearest to scanOrAcqTime
                    ' If convertingRangeOrTolerance = True, then Convert scanOrAcqTime to a relative scan range and then
                    '   call this function again with that relative time

                    If convertingRangeOrTolerance Then
                        Dim totalScans As Integer
                        totalScans = scanList.MasterScanNumList(scanList.MasterScanOrderCount - 1) - scanList.MasterScanNumList(0)
                        If totalScans < 1 Then
                            totalScans = 1
                        End If

                        Dim relativeTime = scanOrAcqTime / totalScans

                        computedScanTime = ScanOrAcqTimeToScanTime(scanList, relativeTime, clsCustomSICList.eCustomSICScanTypeConstants.Relative, True)
                    Else
                        Dim masterScanIndex = FindNearestScanNumIndex(scanList, scanOrAcqTime, eScanType)
                        If masterScanIndex >= 0 AndAlso scanList.MasterScanOrderCount > 0 Then
                            computedScanTime = scanList.MasterScanTimeList(masterScanIndex)
                        End If
                    End If

                Case clsCustomSICList.eCustomSICScanTypeConstants.Relative
                    ' scanOrAcqTime is a fraction of the total number of scans (for example, 0.5)

                    ' Use the total range of scan times
                    With scanList
                        If .MasterScanOrderCount > 0 Then
                            Dim totalRunTime = .MasterScanTimeList(.MasterScanOrderCount - 1) - .MasterScanTimeList(0)

                            computedScanTime = CSng(scanOrAcqTime * totalRunTime + .MasterScanTimeList(0))
                        Else
                            computedScanTime = 0
                        End If
                    End With

                Case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime
                    ' scanOrAcqTime is an elution time value (or elution time range)
                    ' No conversion needed; simply return the value
                    computedScanTime = scanOrAcqTime

                Case Else
                    ' Unknown type; assume already a scan time
                    computedScanTime = scanOrAcqTime
            End Select

            Return computedScanTime
        Catch ex As Exception
            OnErrorEvent("Error in clsMasic->ScanOrAcqTimeToScanTime", ex)
            Return 0
        End Try

    End Function

End Class
