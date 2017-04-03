Imports MASIC
Imports MASIC.clsCustomSICList
Imports NUnit.Framework
Imports PNNLOmics.Utilities
Imports ThermoRawFileReader

Public Class clsTests
    Inherits clsEventNotifier

    <Test>
    Public Sub TestScanConversions()
        Const MZ_MINIMUM As Double = 100
        Const INTENSITY_MINIMUM As Double = 10000
        Const SCAN_TIME_SCALAR As Double = 10.0

        Dim scanList = New clsScanList()
        Dim oRand = New Random()

        Dim intLastSurveyScanIndex As Integer = -1
        Dim intLastSurveyScanIndexInMasterSeqOrder = -1

        ' Populate scanList with example scan data
        For scanNumber = 1 To 1750

            If scanNumber Mod 10 = 0 Then
                ' Add a survey scan
                Dim newSurveyScan = New MASIC.clsScanInfo()
                With newSurveyScan
                    .ScanNumber = scanNumber
                    .ScanTime = scanNumber / SCAN_TIME_SCALAR

                    ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                    .ScanHeaderText = String.Empty
                    .ScanTypeName = "MS"

                    .BasePeakIonMZ = MZ_MINIMUM + oRand.NextDouble() * 1000
                    .BasePeakIonIntensity = INTENSITY_MINIMUM + oRand.NextDouble() * 1000

                    ' Survey scans typically lead to multiple parent ions; we do not record them here
                    .FragScanInfo.ParentIonInfoIndex = -1
                    .TotalIonIntensity = .BasePeakIonIntensity * (0.25 + oRand.NextDouble() * 5)

                    ' Determine the minimum positive intensity in this scan
                    .MinimumPositiveIntensity = INTENSITY_MINIMUM

                    ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                    .ZoomScan = False
                    .SIMScan = False
                    .MRMScanType = MRMScanTypeConstants.NotMRM

                    .LowMass = MZ_MINIMUM
                    .HighMass = Math.Max(.BasePeakIonMZ * 1.1, MZ_MINIMUM * 10)
                    .IsFTMS = False

                End With

                scanList.SurveyScans.Add(newSurveyScan)

                With scanList
                    intLastSurveyScanIndex = .SurveyScans.Count - 1

                    scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.SurveyScan, intLastSurveyScanIndex)
                    intLastSurveyScanIndexInMasterSeqOrder = .MasterScanOrderCount - 1
                End With
            Else

                Dim newFragScan = New MASIC.clsScanInfo()
                With newFragScan
                    .ScanNumber = scanNumber
                    .ScanTime = scanNumber / SCAN_TIME_SCALAR

                    ' If this is a mzXML file that was processed with ReadW, then .ScanHeaderText and .ScanTypeName will get updated by UpdateMSXMLScanType
                    .ScanHeaderText = String.Empty
                    .ScanTypeName = "MSn"

                    .BasePeakIonMZ = MZ_MINIMUM + oRand.NextDouble() * 1000
                    .BasePeakIonIntensity = INTENSITY_MINIMUM + oRand.NextDouble() * 1000

                    ' 1 for the first MS/MS scan after the survey scan, 2 for the second one, etc.
                    .FragScanInfo.FragScanNumber = (scanList.MasterScanOrderCount - 1) - intLastSurveyScanIndexInMasterSeqOrder
                    .FragScanInfo.MSLevel = 2

                    .TotalIonIntensity = .BasePeakIonIntensity * (0.25 + oRand.NextDouble() * 2)

                    ' Determine the minimum positive intensity in this scan
                    .MinimumPositiveIntensity = INTENSITY_MINIMUM

                    ' If this is a mzXML file that was processed with ReadW, then these values will get updated by UpdateMSXMLScanType
                    .ZoomScan = False
                    .SIMScan = False
                    .MRMScanType = MRMScanTypeConstants.NotMRM

                    .MRMScanInfo.MRMMassCount = 0

                End With

                newFragScan.MRMScanInfo.MRMMassCount = 0

                With newFragScan
                    .LowMass = MZ_MINIMUM
                    .HighMass = Math.Max(.BasePeakIonMZ * 1.1, MZ_MINIMUM * 10)
                    .IsFTMS = False
                End With

                scanList.FragScans.Add(newFragScan)
                scanList.AddMasterScanEntry(clsScanList.eScanTypeConstants.FragScan, scanList.FragScans.Count - 1)
            End If
        Next


        Dim scanNumScanConverter As New clsScanNumScanTimeConversion()
        RegisterEvents(scanNumScanConverter)

        ' Convert absolute values
        ' Scan 500, relative scan 0.5, and the scan at 30 minutes
        TestScanConversionToAbsolute(scanList, scanNumScanConverter,
                               New KeyValuePair(Of Integer, Integer)(500, 500),
                               New KeyValuePair(Of Single, Single)(0.5, 876),
                               New KeyValuePair(Of Single, Single)(30, 300))

        TestScanConversionToTime(scanList, scanNumScanConverter,
                               New KeyValuePair(Of Integer, Integer)(500, 50),
                               New KeyValuePair(Of Single, Single)(0.5, 87.55),
                               New KeyValuePair(Of Single, Single)(30, 30))

        ' Convert ranges
        ' 50 scans wide, 10% of the run, and 5 minutes
        TestScanConversionToAbsolute(scanList, scanNumScanConverter,
                               New KeyValuePair(Of Integer, Integer)(50, 50),
                               New KeyValuePair(Of Single, Single)(0.1, 176),
                               New KeyValuePair(Of Single, Single)(5, 50))

        TestScanConversionToTime(scanList, scanNumScanConverter,
                               New KeyValuePair(Of Integer, Integer)(50, 5),
                               New KeyValuePair(Of Single, Single)(0.1, 17.59),
                               New KeyValuePair(Of Single, Single)(5, 5))


    End Sub

    ''' <summary>
    ''' Test ScanOrAcqTimeToAbsolute
    ''' </summary>
    ''' <param name="scanList"></param>
    ''' <param name="scanNumScanConverter"></param>
    ''' <param name="scanNumber">Absolute scan number</param>
    ''' <param name="relativeTime">Relative scan (value between 0 and 1)</param>
    ''' <param name="scanTime">Scan time</param>
    Public Sub TestScanConversionToAbsolute(
      scanList As clsScanList,
      scanNumScanConverter As clsScanNumScanTimeConversion,
      scanNumber As KeyValuePair(Of Integer, Integer),
      relativeTime As KeyValuePair(Of Single, Single),
      scanTime As KeyValuePair(Of Single, Single))

        Dim sngResult As Single

        Try

            ' Find the scan number corresponding to each of these values
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, scanNumber.Key, eCustomSICScanTypeConstants.Absolute, False)
            Console.WriteLine(scanNumber.Key & " -> " & sngResult)
            Assert.AreEqual(scanNumber.Value, sngResult, 0.00001)

            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, relativeTime.Key, eCustomSICScanTypeConstants.Relative, False)
            Console.WriteLine(relativeTime.Key & " -> " & sngResult)
            Assert.AreEqual(relativeTime.Value, sngResult, 0.00001)

            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, scanTime.Key, eCustomSICScanTypeConstants.AcquisitionTime, False)
            Console.WriteLine(scanTime.Key & " -> " & sngResult)
            Assert.AreEqual(scanTime.Value, sngResult, 0.00001)

            Console.WriteLine()

        Catch ex As Exception
            Console.WriteLine("Error caught: " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Test ScanOrAcqTimeToAbsolute
    ''' </summary>
    ''' <param name="scanList"></param>
    ''' <param name="scanNumScanConverter"></param>
    ''' <param name="scanNumber">Absolute scan number</param>
    ''' <param name="relativeTime">Relative scan (value between 0 and 1)</param>
    ''' <param name="scanTime">Scan time</param>
    Public Sub TestScanConversionToTime(
      scanList As clsScanList,
      scanNumScanConverter As clsScanNumScanTimeConversion,
      scanNumber As KeyValuePair(Of Integer, Integer),
      relativeTime As KeyValuePair(Of Single, Single),
      scanTime As KeyValuePair(Of Single, Single))

        Dim sngResult As Single

        Try

            ' Find the scan time corresponding to each of these values
            sngResult = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, scanNumber.Key, eCustomSICScanTypeConstants.Absolute, False)
            Console.WriteLine(scanNumber.Key & " -> " & sngResult & " minutes")
            Assert.AreEqual(scanNumber.Value, sngResult, 0.00001)

            sngResult = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, relativeTime.Key, eCustomSICScanTypeConstants.Relative, False)
            Console.WriteLine(relativeTime.Key & " -> " & sngResult & " minutes")
            Assert.AreEqual(relativeTime.Value, sngResult, 0.00001)

            sngResult = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, scanTime.Key, eCustomSICScanTypeConstants.AcquisitionTime, False)
            Console.WriteLine(scanTime.Key & " -> " & sngResult & " minutes")
            Assert.AreEqual(scanTime.Value, sngResult, 0.00001)

            Console.WriteLine()

        Catch ex As Exception
            Console.WriteLine("Error caught: " & ex.Message)
        End Try

    End Sub

    <TestCase(1.2301, "1.23", 3, 100000)>
    <TestCase(1.2, "1.2", 3, 100000)>
    <TestCase(1.003, "1", 3, 100000)>
    <TestCase(999.995, "999.995", 9, 100000)>
    <TestCase(999.995, "999.995", 8, 100000)>
    <TestCase(999.995, "999.995", 7, 100000)>
    <TestCase(999.995, "999.995", 6, 100000)>
    <TestCase(999.995, "1000", 5, 100000)>
    <TestCase(999.995, "1000", 4, 100000)>
    <TestCase(1000.995, "1001", 3, 100000)>
    <TestCase(1000.995, "1001", 2, 100000)>
    <TestCase(1.003, "1.003", 5, 0)>
    <TestCase(1.23123, "1.2312", 5, 0)>
    <TestCase(12.3123, "12.312", 5, 0)>
    <TestCase(123.123, "123.12", 5, 0)>
    <TestCase(1231.23, "1231.2", 5, 0)>
    <TestCase(12312.3, "12312", 5, 0)>
    <TestCase(123123, "123123", 5, 0)>
    <TestCase(1231234, "1.2312E+06", 5, 0)>
    <TestCase(12312345, "1.2312E+07", 5, 0)>
    <TestCase(123123456, "1.2312E+08", 5, 0)>
    Public Sub TestValueToString(valueToConvert As Double, expectedResult As String, digitsOfPrecision As Integer, scientificNotationThreshold As Integer)

        Dim result As String
        If scientificNotationThreshold > 0 Then
            result = StringUtilities.ValueToString(valueToConvert, digitsOfPrecision, scientificNotationThreshold)
        Else
            result = StringUtilities.ValueToString(valueToConvert, digitsOfPrecision)
        End If

        Console.WriteLine(String.Format("{0,-12} -> {1,-12}", valueToConvert, result))

    End Sub

End Class
