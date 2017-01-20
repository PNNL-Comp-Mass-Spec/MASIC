Public Class clsBaselineNoiseOptions
    ''' <summary>
    ''' Method to use to determine the baseline noise level
    ''' </summary>
    Public BaselineNoiseMode As clsMASICPeakFinder.eNoiseThresholdModes

    ''' <summary>
    ''' Explicitly defined noise intensity
    ''' </summary>
    ''' <remarks>Only used if .BaselineNoiseMode = eNoiseThresholdModes.AbsoluteThreshold; 50000 for SIC, 0 for MS/MS spectra</remarks>
    Public BaselineNoiseLevelAbsolute As Single

    ''' <summary>
    ''' Minimum signal/noise ratio
    ''' </summary>
    ''' <remarks>Typically 2 or 3 for spectra; 0 for SICs</remarks>
    Public MinimumSignalToNoiseRatio As Single

    ''' <summary>
    ''' If the noise threshold computed is less than this value, then this value is used to compute S/N
    ''' Additionally, this is used as the minimum intensity threshold when computing a trimmed noise level
    ''' </summary>
    Public MinimumBaselineNoiseLevel As Single

    ''' <summary>
    ''' Typically 0.75 for SICs, 0.5 for MS/MS spectra
    ''' Only used for eNoiseThresholdModes.TrimmedMeanByAbundance, .TrimmedMeanByCount, .TrimmedMedianByAbundance
    ''' </summary>
    Public TrimmedMeanFractionLowIntensityDataToAverage As Single

    ''' <summary>
    ''' Typically 5; distance from the mean in standard deviation units (SqrRt(Variance)) to discard data for computing the trimmed mean
    ''' </summary>
    Public DualTrimmedMeanStdDevLimits As Short

    ''' <summary>
    ''' Typically 3; set to 1 to disable segmentation
    ''' </summary>
    Public DualTrimmedMeanMaximumSegments As Short

    ''' <summary>
    ''' Return a new instance of clsBaselineNoiseOptions with copied options
    ''' </summary>
    ''' <returns></returns>
    Public Function Clone() As clsBaselineNoiseOptions
        Return DirectCast(Me.MemberwiseClone(), clsBaselineNoiseOptions)
    End Function

End Class
