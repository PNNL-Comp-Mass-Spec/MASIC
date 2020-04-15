Imports MASICPeakFinder

Public Class clsParentIonStats
    Public Enum eScanTypeConstants
        SurveyScan = 0
        FragScan = 1
    End Enum

    Public Index As Integer

    Public MZ As Double

    ''' <summary>
    ''' Scan number of the survey scan
    ''' </summary>
    Public SurveyScanNumber As Integer

    ''' <summary>
    ''' Scan number of the fragmentation scan
    ''' </summary>
    Public FragScanObserved As Integer
    Public FragScanTime As Single

    ''' <summary>
    ''' Optimal peak apex scan number (if parent ion was combined with another parent ion due to similar m/z)
    ''' </summary>
    Public Property OptimalPeakApexScanNumber As Integer

    Public Property OptimalPeakApexTime As Single
    Public Property CustomSICPeak As Boolean
    Public Property CustomSICPeakComment As String

    Public Property SICScanType As eScanTypeConstants

    Public Property SICData As List(Of clsSICDataPoint)

    ' Maximum intensity in SICData
    Public Property SICIntensityMax As Double

    ' Contains the smoothed SIC data plus the details on the peak identified in the SIC (including its baseline noise stats)
    Public Property SICStats As clsSICStats

    ' List of scan numbers at which this m/z was chosen for fragmentation; the range of scans checked will be from SICScans(0) to SICScans(DataCount)
    Public Property SimilarFragScans As List(Of clsSICDataPoint)

    Public Sub New()
        SICData = New List(Of clsSICDataPoint)
        SICStats = New clsSICStats()
        SimilarFragScans = New List(Of clsSICDataPoint)
    End Sub
End Class
