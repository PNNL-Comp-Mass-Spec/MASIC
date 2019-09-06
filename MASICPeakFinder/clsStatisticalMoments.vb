Public Class clsStatisticalMoments

    ''' <summary>
    ''' Area; Zeroth central moment (m0)
    ''' Using baseline-corrected intensities unless all of the data is below the baseline;
    ''' if that's the case, then using the 3 points surrounding the peak apex
    ''' </summary>
    Public Property Area As Double

    ''' <summary>
    ''' Center of Mass of the peak; First central moment (m1); reported as an absolute scan number
    ''' </summary>
    Public Property CenterOfMassScan As Integer

    ''' <summary>
    ''' Standard Deviation; Sqrt(Variance) where Variance is the second central moment (m2)
    ''' </summary>
    Public Property StDev As Double

    ''' <summary>
    ''' Computed using the third central moment via m3 / sigma^3 where m3 is the third central moment and sigma^3 = (Sqrt(m2))^3
    ''' </summary>
    Public Property Skew As Double

    ''' <summary>
    ''' The Kolmogorov-Smirnov Goodness-of-Fit value (not officially a statistical moment, but we'll put it here anyway)
    ''' </summary>
    Public Property KSStat As Double

    ''' <summary>
    ''' Data count used
    ''' </summary>
    Public Property DataCountUsed As Integer

    Public Function Clone() As clsStatisticalMoments
        Dim clonedStats = New clsStatisticalMoments() With {
            .Area = Area,
            .CenterOfMassScan = CenterOfMassScan,
            .StDev = StDev,
            .Skew = Skew,
            .KSStat = KSStat,
            .DataCountUsed = DataCountUsed
        }

        Return clonedStats

    End Function
End Class
