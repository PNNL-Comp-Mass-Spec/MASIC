Public Class clsSICDataPoint
    Public ReadOnly ScanIndex As Integer
    Public ReadOnly ScanNumber As Integer
    Public ReadOnly Intensity As Single
    Public ReadOnly Mass As Double

    Public Sub New(intScanNumber As Integer, sngIntensity As Single, dblMass As Double)
        Me.New(intScanNumber, sngIntensity, dblMass, 0)
    End Sub

    Public Sub New(intScanNumber As Integer, sngIntensity As Single, dblMass As Double, index As Integer)
        ScanNumber = intScanNumber
        Intensity = sngIntensity
        Mass = dblMass
        ScanIndex = index
    End Sub
End Class
