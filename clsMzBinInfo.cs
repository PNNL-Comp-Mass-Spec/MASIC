Public Class clsMzBinInfo

    Public Property MZ As Double

    Public Property MZTolerance As Double

    Public Property MZToleranceIsPPM As Boolean

    Public Property ParentIonIndex As Integer

    Public Overrides Function ToString() As String
        If MZToleranceIsPPM Then
            Return "m/z: " & MZ.ToString("0.0") & ", MZTolerance: " & MZTolerance.ToString("0.0") & " ppm"
        Else
            Return "m/z: " & MZ.ToString("0.0") & ", MZTolerance: " & MZTolerance.ToString("0.000") & " Da"
        End If

    End Function

End Class
