Imports ThermoRawFileReader

Public Class clsScanInfo

    ''' <summary>
    ''' Ranges from 1 to the number of scans in the datafile
    ''' </summary>
    Public ScanNumber As Integer

    ''' <summary>
    ''' Retention (elution) Time (in minutes)
    ''' </summary>
    Public ScanTime As Single

    ''' <summary>
    ''' String description of the scan mode for the given scan; only used for Finnigan .Raw files
    ''' </summary>
    ''' <remarks>Typical values are: FTMS + p NSI Full ms, ITMS + c ESI Full ms, 
    ''' ITMS + p ESI d Z ms, ITMS + c NSI d Full ms2, ITMS + c NSI d Full ms2, 
    ''' ITMS + c NSI d Full ms2, FTMS + c NSI d Full ms2, ITMS + c NSI d Full ms3</remarks>
    Public ScanHeaderText As String

    ''' <summary>
    ''' Scan type name
    ''' </summary>
    ''' <remarks>Typical values: MS, HMS, Zoom, CID-MSn, or PQD-MSn</remarks>
    Public ScanTypeName As String
    ' 
    ''' <summary>
    ''' mz of the most intense ion in this scan
    ''' </summary>
    Public BasePeakIonMZ As Double

    ''' <summary>
    ''' intensity of the most intense ion in this scan
    ''' </summary>
    Public BasePeakIonIntensity As Single

    ''' <summary>
    ''' intensity of all of the ions for this scan
    ''' </summary>
    Public TotalIonIntensity As Single

    ''' <summary>
    ''' minimum intensity > 0 in this scan
    ''' </summary>
    Public MinimumPositiveIntensity As Single

    ''' <summary>
    ''' True if the scan is a Zoom scan
    ''' </summary>
    Public ZoomScan As Boolean

    ''' <summary>
    ''' True if the scan was a SIM scan
    ''' </summary>
    Public SIMScan As Boolean

    Public MRMScanType As MRMScanTypeConstants

    ''' <summary>
    ''' For SIM scans, allows one to quickly find all of the SIM scans with the same mass range, since they'll all have the same SIMIndex
    ''' </summary>
    Public SIMIndex As Integer

    ''' <summary>
    ''' Useful for SIMScans to find similar SIM scans
    ''' </summary>
    Public LowMass As Double

    ''' <summary>
    ''' Useful for SIMScans to find similar SIM scans
    ''' </summary>
    Public HighMass As Double

    ''' <summary>
    ''' Information specific to fragmentation scans
    ''' </summary>
    Public ReadOnly FragScanInfo As clsFragScanInfo

    ''' <summary>
    ''' Information specific to MRM/SRM scans
    ''' </summary>
    Public MRMScanInfo As clsMRMScanInfo

    ''' <summary>
    ''' Keys are ID values pointing to mExtendedHeaderInfo (where the name is defined); values are the string or numeric values for the settings
    ''' </summary>
    Public ExtendedHeaderInfo As Dictionary(Of Integer, String)

    ''' <summary>
    ''' Note: the mass spectral data for this scan is tracked by a clsSpectraCache object
    ''' </summary>
    Public IonCount As Integer

    Public IonCountRaw As Integer

    Public BaselineNoiseStats As MASICPeakFinder.clsMASICPeakFinder.udtBaselineNoiseStatsType

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        MRMScanType = MRMScanTypeConstants.NotMRM

        FragScanInfo = New clsFragScanInfo()
        MRMScanInfo = New clsMRMScanInfo()

        ExtendedHeaderInfo = New Dictionary(Of Integer, String)
    End Sub
End Class
