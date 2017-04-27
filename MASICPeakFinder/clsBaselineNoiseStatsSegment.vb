Public Class clsBaselineNoiseStatsSegment

    Public Property BaselineNoiseStats As clsBaselineNoiseStats

    Public Property SegmentIndexStart As Integer

    Public Property SegmentIndexEnd As Integer

    <Obsolete("Use the constructor that takes an instance of clsBaselineNoiseStats")>
    Public Sub New()
        BaselineNoiseStats = New clsBaselineNoiseStats()
    End Sub

    Public Sub New(noiseStats As clsBaselineNoiseStats)
        BaselineNoiseStats = noiseStats
    End Sub
End Class
