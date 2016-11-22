Imports ThermoRawFileReader

Public Class clsMRMScanInfo
    Public ParentIonMZ As Double
    ''' <summary>
    ''' List of mass ranges monitored by the first quadrupole
    ''' </summary>
    Public MRMMassCount As Integer

    ''' <summary>
    ''' Daughter m/z values monitored for this parent m/z
    ''' </summary>
    Public MRMMassList As List(Of udtMRMMassRangeType)

    ''' <summary>
    ''' Number of spectra that used these MRM search values
    ''' </summary>
    Public ScanCount As Integer

    Public ParentIonInfoIndex As Integer

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        MRMMassList = New List(Of udtMRMMassRangeType)
    End Sub
End Class
