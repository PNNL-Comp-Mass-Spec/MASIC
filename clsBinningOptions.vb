Public Class clsBinningOptions

    Public Property StartX As Single
    Public Property EndX As Single
    Public Property BinSize As Single

    Public Property IntensityPrecisionPercent As Single
    Public Property Normalize As Boolean
    Public Property SumAllIntensitiesForBin As Boolean                  ' Sum all of the intensities for binned ions of the same bin together
    Public Property MaximumBinCount As Integer

End Class
