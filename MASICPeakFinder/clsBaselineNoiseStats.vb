Public Class clsBaselineNoiseStats

    ''' <summary>
    ''' Typically the average of the data being sampled to determine the baseline noise estimate
    ''' </summary>
    Public Property NoiseLevel As Single

    ''' <summary>
    ''' Standard Deviation of the data used to compute the baseline estimate
    ''' </summary>
    Public Property NoiseStDev As Single

    Public Property PointsUsed As Integer

    Public Property NoiseThresholdModeUsed As clsMASICPeakFinder.eNoiseThresholdModes

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
