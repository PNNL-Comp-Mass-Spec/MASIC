Public Class clsSICPeakFinderOptions

    ''' <summary>
    ''' Intensity Threshold Fraction Max
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 0.01</remarks>
    Public Property IntensityThresholdFractionMax As Single

    ''' <summary>
    ''' Intensity Threshold Absolute Minimum
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 0</remarks>
    Public Property IntensityThresholdAbsoluteMinimum As Single

    Public Property SICBaselineNoiseOptions As clsBaselineNoiseOptions

    ''' <summary>
    ''' Maximum distance that the edge of an identified peak can be away from the scan number that the parent ion was observed in if the identified peak does not contain the parent ion
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 0</remarks>
    Public Property MaxDistanceScansNoOverlap As Integer

    ''' <summary>
    ''' Maximum fraction of the peak maximum that an upward spike can be to be included in the peak
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 0.20</remarks>
    Public Property MaxAllowedUpwardSpikeFractionMax As Single

    ''' <summary>
    ''' Multiplied by scaled S/N for the given spectrum to determine the initial minimum peak width (in scans) to try.  Scaled "S/N" = Math.Log10(Math.Floor("S/N")) * 10
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 0.5</remarks>
    Public Property InitialPeakWidthScansScaler As Single

    ''' <summary>
    ''' Maximum initial peak width to allow
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 30</remarks>
    Public Property InitialPeakWidthScansMaximum As Integer

    Public Property FindPeaksOnSmoothedData As Boolean

    Public Property SmoothDataRegardlessOfMinimumPeakWidth As Boolean

    ''' <summary>
    ''' Use Butterworth smoothing
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>UseButterworthSmooth takes precedence over UseSavitzkyGolaySmooth</remarks>
    Public Property UseButterworthSmooth As Boolean

    Public Property ButterworthSamplingFrequency As Single
    Public Property ButterworthSamplingFrequencyDoubledForSIMData As Boolean

    ''' <summary>
    ''' Use Savitzky Golay smoothing
    ''' </summary>
    ''' <returns></returns>
    Public Property UseSavitzkyGolaySmooth As Boolean

    ''' <summary>
    ''' Even number, 0 or greater; 0 means a moving average filter, 2 means a 2nd order Savitzky Golay filter
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Default: 0</remarks>
    Public Property SavitzkyGolayFilterOrder As Short

    Public Property MassSpectraNoiseThresholdOptions As clsBaselineNoiseOptions
End Class
