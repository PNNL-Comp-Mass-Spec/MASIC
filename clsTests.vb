Imports MASIC.clsCustomSICList

Public Class clsTests
    Inherits clsEventNotifier

    Public Sub TestScanConversions(scanList As clsScanList)

        Dim intScanNumber As Integer
        Dim sngRelativeTime As Single
        Dim sngScanTime As Single

        Dim sngResult As Single

        Dim scanNumScanConverter As New clsScanNumScanTimeConversion()
        RegisterEvents(scanNumScanConverter)

        Try
            ' Convert absolute values
            intScanNumber = 500         ' Scan 500
            sngRelativeTime = 0.5       ' Relative scan 0.5
            sngScanTime = 30            ' The scan at 30 minutes

            ' Find the scan number corresponding to each of these values
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, False)
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, False)
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, False)


            ' Convert ranges
            intScanNumber = 50          ' 50 scans wide
            sngRelativeTime = 0.1       ' 10% of the run
            sngScanTime = 5             ' 5 minutes

            ' Convert each of these ranges to a scan time range in minutes
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, True)
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, True)
            sngResult = scanNumScanConverter.ScanOrAcqTimeToAbsolute(scanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, True)



            ' Convert absolute values
            intScanNumber = 500         ' Scan 500
            sngRelativeTime = 0.5       ' Relative scan 0.5
            sngScanTime = 30            ' The scan at 30 minutes

            ' Find the scan number corresponding to each of these values
            sngResult = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, False)
            sngResult = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, False)
            sngResult = scanNumScanConverter.ScanOrAcqTimeToScanTime(scanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, False)


            ' Convert ranges
            intScanNumber = 50          ' 50 scans wide
            sngRelativeTime = 0.1       ' 10% of the run
            sngScanTime = 5             ' 5 minutes

            ' Convert each of these ranges to a scan time range in minutes
            sngResult = ScanOrAcqTimeToScanTime(scanList, intScanNumber, eCustomSICScanTypeConstants.Absolute, True)
            sngResult = ScanOrAcqTimeToScanTime(scanList, sngRelativeTime, eCustomSICScanTypeConstants.Relative, True)
            sngResult = ScanOrAcqTimeToScanTime(scanList, sngScanTime, eCustomSICScanTypeConstants.AcquisitionTime, True)


        Catch ex As Exception
            Console.WriteLine("Error caught: " & ex.Message)
        End Try

    End Sub

    Public Shared Sub TestValueToString()

        Const intDigitsOfPrecision = 5

        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1.2301, 3, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1.2, 3, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1.003, 3, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(999.995, 9, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(999.995, 8, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(999.995, 7, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(999.995, 6, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(999.995, 5, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(999.995, 4, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1000.995, 3, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1000.995, 2, 100000))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1.003, 5))

        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1.23123, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(12.3123, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(123.123, intDigitsOfPrecision))

        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1231.23, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(12312.3, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(123123, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(1231234, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(12312345, intDigitsOfPrecision))
        Console.WriteLine(PNNLOmics.Utilities.StringUtilities.ValueToString(123123456, intDigitsOfPrecision))

    End Sub
End Class
