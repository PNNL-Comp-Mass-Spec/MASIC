Public Class clsBaselineNoiseStats

    ''' <summary>
    ''' Typically the average of the data being sampled to determine the baseline noise estimate
    ''' </summary>
    Public Property NoiseLevel As Double

    ''' <summary>
    ''' Standard Deviation of the data used to compute the baseline estimate
    ''' </summary>
    Public Property NoiseStDev As Double

    Public Property PointsUsed As Integer

    Public Property NoiseThresholdModeUsed As clsMASICPeakFinder.eNoiseThresholdModes

    Public Sub New()
        NoiseThresholdModeUsed = clsMASICPeakFinder.eNoiseThresholdModes.AbsoluteThreshold
    End Sub

    Public Function Clone() As clsBaselineNoiseStats
        Dim clonedStats = New clsBaselineNoiseStats() With {
                .NoiseLevel = NoiseLevel,
                .NoiseStDev = NoiseStDev,
                .PointsUsed = PointsUsed,
                .NoiseThresholdModeUsed = NoiseThresholdModeUsed
            }

        Return clonedStats

    End Function
End Class
