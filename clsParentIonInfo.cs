Public Class clsParentIonInfo

    Private mParentIonMz As Double

    ''' <summary>
    ''' Parent ion m/z value
    ''' </summary>
    Public ReadOnly Property MZ As Double
        Get
            Return mParentIonMz
        End Get
    End Property

    ''' <summary>
    ''' Survey scan that this parent ion was observed in; Pointer to entry in .SurveyScans(); For custom SIC values, this is the closest survey scan to .ScanCenter
    ''' </summary>
    Public Property SurveyScanIndex As Integer

    ''' <summary>
    ''' Scan number of the peak apex for this parent ion; originally the scan number of the first fragmentation spectrum; later updated to the scan number of the SIC data Peak apex; possibly updated later in FindSimilarParentIons()
    ''' </summary>
    Public Property OptimalPeakApexScanNumber As Integer

    ''' <summary>
    ''' If OptimalPeakApexScanNumber is inherited from another parent ion, then this is set to that parent ion's index; otherwise, this is -1
    ''' </summary>
    Public Property PeakApexOverrideParentIonIndex As Integer

    ''' <summary>
    ''' Pointers to entries in .FragScans(); for custom SIC values, points to the next MS2 scan that occurs after the ScanCenter search value
    ''' </summary>
    Public ReadOnly FragScanIndices As List(Of Integer)

    Public Property SICStats As clsSICStats

    ''' <summary>
    ''' True if this is a custom SIC-based parent ion
    ''' </summary>
    Public Property CustomSICPeak As Boolean

    ''' <summary>
    ''' Only applies to custom SIC-based parent ions
    ''' </summary>
    Public Property CustomSICPeakComment As String

    ''' <summary>
    ''' Only applies to custom SIC-based parent ions
    ''' </summary>
    Public Property CustomSICPeakMZToleranceDa As Double

    ''' <summary>
    ''' Only applies to custom SIC-based parent ions
    ''' </summary>
    Public Property CustomSICPeakScanOrAcqTimeTolerance As Single

    ''' <summary>
    ''' Only applicable to MRM scans
    ''' </summary>
    Public Property MRMDaughterMZ As Double

    ''' <summary>
    ''' Only applicable to MRM scans
    ''' </summary>
    Public Property MRMToleranceHalfWidth As Double

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="parentIonMz">Parent ion m/z value</param>
    Public Sub New(parentIonMz As Double)
        FragScanIndices = New List(Of Integer)
        mParentIonMz = parentIonMz
        SICStats = New clsSICStats()
    End Sub

    Public Sub UpdateMz(parentIonMz As Double)
        mParentIonMz = parentIonMz
    End Sub

    Public Overrides Function ToString() As String
        If CustomSICPeak Then
            Return "m/z " & MZ.ToString("0.00") & " (Custom SIC peak)"
        Else
            Return "m/z " & MZ.ToString("0.00")
        End If

    End Function
End Class
