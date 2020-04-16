Imports ThermoRawFileReader

Public Class clsMRMScanInfo
    Public Property ParentIonMZ As Double
    ''' <summary>
    ''' List of mass ranges monitored by the first quadrupole
    ''' </summary>
    Public Property MRMMassCount As Integer

    ''' <summary>
    ''' Daughter m/z values monitored for this parent m/z
    ''' </summary>
    Public Property MRMMassList As List(Of udtMRMMassRangeType)

    ''' <summary>
    ''' Number of spectra that used these MRM search values
    ''' </summary>
    Public Property ScanCount As Integer

    Public Property ParentIonInfoIndex As Integer

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        MRMMassList = New List(Of udtMRMMassRangeType)
        ScanCount = 0
    End Sub
End Class
