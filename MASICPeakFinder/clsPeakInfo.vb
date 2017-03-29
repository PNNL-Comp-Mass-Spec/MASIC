Public Class clsPeakInfo
    Public Property PeakLocation As Integer
    Public Property LeftEdge As Integer
    Public Property RightEdge As Integer
    Public Property PeakArea As Double
    Public Property PeakIsValid As Boolean

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="intPeakLocation">Index of this peak in the data arrays</param>
    Public Sub New(intPeakLocation As Integer)
        PeakLocation = intPeakLocation
    End Sub

    Public Function Clone() As clsPeakInfo

        Dim newPeak = New clsPeakInfo(PeakLocation) With {
                .LeftEdge = LeftEdge,
                .RightEdge = RightEdge,
                .PeakArea = PeakArea,
                .PeakIsValid = PeakIsValid
            }

        Return newPeak

    End Function
End Class
