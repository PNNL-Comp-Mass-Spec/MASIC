Public Class clsSICStatsPeak
    ''' <summary>
    ''' Index that the SIC peak officially starts; Pointer to entry in .SICData()
    ''' </summary>
    Public Property IndexBaseLeft As Integer

    ''' <summary>
    ''' Index that the SIC peak officially ends; Pointer to entry in .SICData()
    ''' </summary>
    Public Property IndexBaseRight As Integer

    ''' <summary>
    ''' Index of the maximum of the SIC peak; Pointer to entry in .SICData()
    ''' </summary>
    Public Property IndexMax As Integer

    ''' <summary>
    ''' Index that the SIC peak was first observed in by the instrument (and thus caused it to be chosen for fragmentation)
    ''' Pointer to entry in .SICData()
    ''' </summary>
    Public Property IndexObserved As Integer

    ''' <summary>
    ''' Intensity of the parent ion in the scan just prior to the scan in which the peptide was fragmented
    ''' If previous scan was not MS1, then interpolates between MS1 scans bracketing the MS2 scan
    ''' </summary>
    Public Property ParentIonIntensity As Double

    ''' <summary>
    ''' Index of the FWHM point in the previous closest peak in the SIC
    ''' Filtering to only include peaks with intensities >= BestPeak'sIntensity/3
    ''' </summary>
    Public Property PreviousPeakFWHMPointRight As Integer

    ''' <summary>
    ''' Index of the FWHM point in the next closest peak in the SIC; filtering to only include peaks with intensities >= BestPeak'sIntensity/3
    ''' </summary>
    Public Property NextPeakFWHMPointLeft As Integer

    Public Property FWHMScanWidth As Integer

    ''' <summary>
    ''' Maximum intensity of the SIC Peak -- not necessarily the maximum intensity in .SICData(); Not baseline corrected
    ''' </summary>
    Public Property MaxIntensityValue As Double

    ''' <summary>
    ''' Area of the SIC peak -- Equivalent to the zeroth statistical moment (m0); Not baseline corrected
    ''' </summary>
    Public Property Area As Double

    ''' <summary>
    ''' Number of small peaks that are contained by the peak
    ''' </summary>
    Public Property ShoulderCount As Integer

    Public Property SignalToNoiseRatio As Double

    Public Property BaselineNoiseStats As clsBaselineNoiseStats

    Public Property StatisticalMoments As clsStatisticalMoments

    Public Sub New()
        BaselineNoiseStats = New clsBaselineNoiseStats()
        StatisticalMoments = New clsStatisticalMoments()
    End Sub

    Public Function Clone() As clsSICStatsPeak
        Dim clonedPeak = New clsSICStatsPeak() With {
            .IndexBaseLeft = IndexBaseLeft,
            .IndexBaseRight = IndexBaseRight,
            .IndexMax = IndexMax,
            .IndexObserved = IndexObserved,
            .ParentIonIntensity = ParentIonIntensity,
            .PreviousPeakFWHMPointRight = PreviousPeakFWHMPointRight,
            .NextPeakFWHMPointLeft = NextPeakFWHMPointLeft,
            .FWHMScanWidth = FWHMScanWidth,
            .MaxIntensityValue = MaxIntensityValue,
            .Area = Area,
            .ShoulderCount = ShoulderCount,
            .SignalToNoiseRatio = SignalToNoiseRatio,
            .BaselineNoiseStats = BaselineNoiseStats.Clone(),
            .StatisticalMoments = StatisticalMoments.Clone()
        }

        Return clonedPeak

    End Function
End Class
