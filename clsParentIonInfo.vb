Public Class clsParentIonInfo

    Public Structure udtSICStatsType
        Public Peak As MASICPeakFinder.clsMASICPeakFinder.udtSICStatsPeakType

        Public ScanTypeForPeakIndices As clsScanList.eScanTypeConstants
        Public PeakScanIndexStart As Integer              ' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        Public PeakScanIndexEnd As Integer                ' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum
        Public PeakScanIndexMax As Integer                ' Pointer to entry in .SurveyScans() or .FragScans() indicating the survey scan that contains the peak maximum

        Public SICPotentialAreaStatsForPeak As MASICPeakFinder.clsMASICPeakFinder.udtSICPotentialAreaStatsType
    End Structure


    ''' <summary>
    ''' m/z value
    ''' </summary>
    Public MZ As Double

    ''' <summary>
    ''' Survey scan that this parent ion was observed in; Pointer to entry in .SurveyScans(); For custom SIC values, this is the closest survey scan to .ScanCenter
    ''' </summary>
    Public SurveyScanIndex As Integer

    ''' <summary>
    ''' Scan number of the peak apex for this parent ion; originally the scan number of the first fragmentation spectrum; later updated to the scan number of the SIC data Peak apex; possibly updated later in FindSimilarParentIons()
    ''' </summary>
    Public OptimalPeakApexScanNumber As Integer

    ''' <summary>
    ''' If OptimalPeakApexScanNumber is inherited from another parent ion, then this is set to that parent ion's index; otherwise, this is -1
    ''' </summary>
    Public PeakApexOverrideParentIonIndex As Integer

    ''' <summary>
    ''' Number of fragmentation scans attributable to this parent ion; normally just 1; for custom SIC values, there are no associated fragmentation scans, but we still set this value to 1
    ''' </summary>
    Public FragScanIndexCount As Integer

    ''' <summary>
    ''' Pointers to entries in .FragScans(); for custom SIC values, points to the next MS2 scan that occurs after the ScanCenter search value
    ''' </summary>
    Public FragScanIndices() As Integer

    Public SICStats As udtSICStatsType

    ''' <summary>
    ''' True if this is a custom SIC-based parent ion
    ''' </summary>
    Public CustomSICPeak As Boolean

    ''' <summary>
    ''' Only applies to custom SIC-based parent ions
    ''' </summary>
    Public CustomSICPeakComment As String

    ''' <summary>
    ''' Only applies to custom SIC-based parent ions
    ''' </summary>
    Public CustomSICPeakMZToleranceDa As Double

    ''' <summary>
    ''' Only applies to custom SIC-based parent ions
    ''' </summary>
    Public CustomSICPeakScanOrAcqTimeTolerance As Single

    ''' <summary>
    ''' Only applicable to MRM scans
    ''' </summary>
    Public MRMDaughterMZ As Double

    ''' <summary>
    ''' Only applicable to MRM scans
    ''' </summary>
    Public MRMToleranceHalfWidth As Double

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        ReDim FragScanIndices(0)
    End Sub
End Class
