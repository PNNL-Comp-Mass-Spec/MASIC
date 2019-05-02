Public Class clsSICDataPoint
    Public ReadOnly ScanIndex As Integer
    Public ReadOnly ScanNumber As Integer
    Public ReadOnly Intensity As Double
    Public ReadOnly Mass As Double

    Public Sub New(intScanNumber As Integer, dblIntensity As Double, dblMass As Double)
        Me.New(intScanNumber, dblIntensity, dblMass, 0)
    End Sub

    Public Sub New(intScanNumber As Integer, dblIntensity As Double, dblMass As Double, index As Integer)
        ScanNumber = intScanNumber
        Intensity = dblIntensity
        Mass = dblMass
        ScanIndex = index
    End Sub

    Public Overrides Function ToString() As String
        Return String.Format("{0:F0} at {1:F2} m/z in scan {2}", Intensity, Mass, ScanNumber)
    End Function

End Class
