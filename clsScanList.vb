''' <summary>
''' 
''' </summary>
Public Class clsScanList

    Public Enum eScanTypeConstants
        SurveyScan = 0
        FragScan = 1
    End Enum

    Public Structure udtScanOrderPointerType
        Public ScanType As eScanTypeConstants
        Public ScanIndexPointer As Integer                  ' Pointer to entry into udtScanList.SurveyScans() or udtScanList.FragScans()
    End Structure


    ' Note: We're keeping the Survey Scans separate from the Fragmentation Scans to make the creation of the
    '         survey scan based SIC's easier (and faster)
    '       The MasterScanOrder array allows us to step through the data scan-by-scan, using both SurveyScans and FragScans

    ''' <summary>
    ''' 0-based array, holding survey scans, the order is the same as in the original data file, and thus is by increasing scan number
    ''' </summary>
    Public SurveyScans() As clsScanInfo
    Public SurveyScanCount As Integer

    ''' <summary>
    ''' 0-based array, holding fragmentation scans, the order is the same as in the original data file, and thus is by increasing scan number
    ''' </summary>
    Public FragScans() As clsScanInfo
    Public FragScanCount As Integer

    ''' <summary>
    ''' 0-based array, holding pointers to either the SurveyScans() or FragScans() arrays, in order of scan number
    ''' </summary>
    Public MasterScanOrder() As udtScanOrderPointerType
    Public MasterScanOrderCount As Integer

    ''' <summary>
    ''' 0-based array; parallel to MasterScanOrder
    ''' </summary>
    Public MasterScanNumList() As Integer

    ''' <summary>
    ''' 0-based array; parallel to MasterScanOrder
    ''' </summary>
    Public MasterScanTimeList() As Single

    Public ParentIonInfoCount As Integer

    ''' <summary>
    ''' 0-based array, ranging from 0 to ParentIonInfoCount-1
    ''' </summary>
    Public ParentIons() As clsParentIonInfo

    ''' <summary>
    ''' Set to true if the user cancels any of the processing steps
    ''' </summary>
    Public ProcessingIncomplete As Boolean
    Public SIMDataPresent As Boolean
    Public MRMDataPresent As Boolean

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()

    End Sub
End Class
