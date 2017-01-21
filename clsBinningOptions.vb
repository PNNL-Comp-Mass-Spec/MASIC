Public Class clsBinningOptions

#Region "Properties"

    Public Property StartX As Single

    Public Property EndX As Single

    Public Property BinSize As Single
        Get
            Return mBinSize
        End Get
        Set(value As Single)
            If value <= 0 Then value = 1
            mBinSize = value
        End Set
    End Property
    Public Property IntensityPrecisionPercent As Single
        Get
            Return mIntensityPrecisionPercent
        End Get
        Set(value As Single)
            If value < 0 Or value > 100 Then value = 1
            mIntensityPrecisionPercent = value
        End Set
    End Property

    Public Property Normalize As Boolean

    ''' <summary>
    ''' Sum all of the intensities for binned ions of the same bin together
    ''' </summary>
    ''' <returns></returns>
    Public Property SumAllIntensitiesForBin As Boolean

    Public Property MaximumBinCount As Integer
        Get
            Return mMaximumBinCount
        End Get
        Set(value As Integer)
            If value < 2 Then value = 10
            If value > 1000000 Then value = 1000000
            mMaximumBinCount = value
        End Set
    End Property

#End Region

#Region "Classwide variables"
    Dim mBinSize As Single = 1
    Dim mIntensityPrecisionPercent As Single = 1
    Dim mMaximumBinCount As Integer = 100000
#End Region

    Public Sub Reset()
        Dim defaultOptions = clsCorrelation.GetDefaultBinningOptions()

        With defaultOptions
            StartX = .StartX
            EndX = .EndX
            BinSize = .BinSize
            IntensityPrecisionPercent = .IntensityPrecisionPercent
            Normalize = .Normalize
            SumAllIntensitiesForBin = .SumAllIntensitiesForBin
            MaximumBinCount = .MaximumBinCount
        End With
    End Sub
End Class
