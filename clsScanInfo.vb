Imports ThermoRawFileReader

Public Class clsScanInfo

    ''' <summary>
    ''' Ranges from 1 to the number of scans in the datafile
    ''' </summary>
    Public Property ScanNumber As Integer

    ''' <summary>
    ''' Retention (elution) Time (in minutes)
    ''' </summary>
    Public Property ScanTime As Single

    ''' <summary>
    ''' String description of the scan mode for the given scan; only used for Finnigan .Raw files
    ''' </summary>
    ''' <remarks>Typical values are: FTMS + p NSI Full ms, ITMS + c ESI Full ms, 
    ''' ITMS + p ESI d Z ms, ITMS + c NSI d Full ms2, ITMS + c NSI d Full ms2, 
    ''' ITMS + c NSI d Full ms2, FTMS + c NSI d Full ms2, ITMS + c NSI d Full ms3</remarks>
    Public Property ScanHeaderText As String

    ''' <summary>
    ''' Scan type name
    ''' </summary>
    ''' <remarks>Typical values: MS, HMS, Zoom, CID-MSn, or PQD-MSn</remarks>
    Public Property ScanTypeName As String
    ' 
    ''' <summary>
    ''' mz of the most intense ion in this scan
    ''' </summary>
    Public Property BasePeakIonMZ As Double

    ''' <summary>
    ''' intensity of the most intense ion in this scan
    ''' </summary>
    Public Property BasePeakIonIntensity As Single

    ''' <summary>
    ''' intensity of all of the ions for this scan
    ''' </summary>
    Public Property TotalIonIntensity As Single

    ''' <summary>
    ''' minimum intensity > 0 in this scan
    ''' </summary>
    Public Property MinimumPositiveIntensity As Single

    ''' <summary>
    ''' True if the scan is a Zoom scan
    ''' </summary>
    Public Property ZoomScan As Boolean

    ''' <summary>
    ''' True if the scan was a SIM scan
    ''' </summary>
    Public Property SIMScan As Boolean

    Public Property MRMScanType As MRMScanTypeConstants

    ''' <summary>
    ''' For SIM scans, allows one to quickly find all of the SIM scans with the same mass range, since they'll all have the same SIMIndex
    ''' </summary>
    Public Property SIMIndex As Integer

    ''' <summary>
    ''' Useful for SIMScans to find similar SIM scans
    ''' </summary>
    Public Property LowMass As Double

    ''' <summary>
    ''' Useful for SIMScans to find similar SIM scans
    ''' </summary>
    Public Property HighMass As Double

    ''' <summary>
    ''' True if the scan was collected in the FT cell of a Thermo instrument
    ''' </summary>
    ''' <returns></returns>
    Public Property IsFTMS As Boolean

    ''' <summary>
    ''' Information specific to fragmentation scans
    ''' </summary>
    Public ReadOnly Property FragScanInfo As clsFragScanInfo

    ''' <summary>
    ''' Information specific to MRM/SRM scans
    ''' </summary>
    Public Property MRMScanInfo As clsMRMScanInfo

    ''' <summary>
    ''' Keys are ID values pointing to mExtendedHeaderNameMap (where the name is defined); values are the string or numeric values for the settings
    ''' </summary>
    Public ReadOnly Property ExtendedHeaderInfo As Dictionary(Of Integer, String)

    ''' <summary>
    ''' Note: the mass spectral data for this scan is tracked by a clsSpectraCache object
    ''' </summary>
    Public Property IonCount As Integer

    Public Property IonCountRaw As Integer

    Public Property BaselineNoiseStats As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseStatsType

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        MRMScanType = MRMScanTypeConstants.NotMRM

        FragScanInfo = New clsFragScanInfo()
        MRMScanInfo = New clsMRMScanInfo()

        ExtendedHeaderInfo = New Dictionary(Of Integer, String)
    End Sub

    Public Overrides Function ToString() As String
        Return "Scan " & ScanNumber & ", " & ScanTypeName
    End Function
End Class
