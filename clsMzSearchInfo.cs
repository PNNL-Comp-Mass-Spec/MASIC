Public Class clsMzSearchInfo
    Public Property SearchMZ As Double

    Public Property MZIndexStart As Integer
    Public Property MZIndexEnd As Integer
    Public Property MZIndexMidpoint As Integer

    Public Property MZTolerance As Double
    Public Property MZToleranceIsPPM As Boolean

    Public Property MaximumIntensity As Double
    Public Property ScanIndexMax As Integer

    Public Property BaselineNoiseStatSegments As List(Of MASICPeakFinder.clsBaselineNoiseStatsSegment)

    Public Overrides Function ToString() As String
        Return "m/z: " & SearchMZ.ToString("0.0") & ", Intensity: " & MaximumIntensity.ToString("0.0")
    End Function

    ''' <summary>
    ''' Reset MaximumIntensity and ScanIndexMax to defaults
    ''' </summary>
    Public Sub ResetMaxIntensity()
        MaximumIntensity = 0
        ScanIndexMax = 0
    End Sub
End Class
