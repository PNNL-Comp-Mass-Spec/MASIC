
Namespace DatasetStats
    Public Class ScanStatsEntry

        ''' <summary>
        ''' Scan number
        ''' </summary>
        Public Property ScanNumber As Integer

        ''' <summary>
        ''' Scan Type (aka MSLevel)
        ''' </summary>
        ''' <remarks>1 for MS, 2 for MS2, 3 for MS3</remarks>
        Public Property ScanType As Integer

        ''' <summary>
        ''' Scan filter text
        ''' </summary>
        ''' <remarks>
        ''' Examples:
        '''   FTMS + p NSI Full ms [400.00-2000.00]
        '''   ITMS + c ESI Full ms [300.00-2000.00]
        '''   ITMS + p ESI d Z ms [1108.00-1118.00]
        '''   ITMS + c ESI d Full ms2 342.90@cid35.00
        ''' </remarks>
        Public Property ScanFilterText As String

        ''' <summary>
        ''' Scan type name
        ''' </summary>
        ''' <remarks>
        ''' Examples:
        '''   MS, HMS, Zoom, CID-MSn, or PQD-MSn
        ''' </remarks>
        Public Property ScanTypeName As String

        ' The following are strings to prevent the number formatting from changing

        ''' <summary>
        ''' Elution time, in minutes
        ''' </summary>
        Public Property ElutionTime As String

        ''' <summary>
        ''' Drift time, in milliseconds
        ''' </summary>
        Public Property DriftTimeMsec As String

        ''' <summary>
        ''' Total ion intensity
        ''' </summary>
        Public Property TotalIonIntensity As String

        ''' <summary>
        ''' Base peak ion intensity
        ''' </summary>
        Public Property BasePeakIntensity As String

        ''' <summary>
        ''' Base peak m/z
        ''' </summary>
        Public Property BasePeakMZ As String

        ''' <summary>
        ''' Signal to noise ratio (S/N)
        ''' </summary>
        Public Property BasePeakSignalToNoiseRatio As String

        ''' <summary>
        ''' Ion count
        ''' </summary>
        Public Property IonCount As Integer

        ''' <summary>
        ''' Ion count before centroiding
        ''' </summary>
        Public Property IonCountRaw As Integer

        ''' <summary>
        ''' Smallest m/z value in the scan
        ''' </summary>
        Public Property MzMin As Double

        ''' <summary>
        ''' Largest m/z value in the scan
        ''' </summary>
        Public Property MzMax As Double

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New()
            Clear()
        End Sub

        ''' <summary>
        ''' Clear all values
        ''' </summary>
        Public Sub Clear()
            ScanNumber = 0
            ScanType = 0

            ScanFilterText = String.Empty
            ScanTypeName = String.Empty

            ElutionTime = "0"
            DriftTimeMsec = "0"
            TotalIonIntensity = "0"
            BasePeakIntensity = "0"
            BasePeakMZ = "0"
            BasePeakSignalToNoiseRatio = "0"

            IonCount = 0
            IonCountRaw = 0

            MzMin = 0
            MzMax = 0
        End Sub

    End Class
End Namespace