Imports ThermoRawFileReader

''' <summary>
''' 
''' </summary>
Public Class clsScanList
    Inherits clsEventNotifier

#Region "Constants and Enums"
    Public Enum eScanTypeConstants
        SurveyScan = 0
        FragScan = 1
    End Enum

#End Region

#Region "Structures"

    Public Structure udtScanOrderPointerType
        Public ScanType As eScanTypeConstants
        Public ScanIndexPointer As Integer                  ' Pointer to entry into list clsScanList.SurveyScans or clsScanList.FragScans

        Public Overrides Function ToString() As String
            Return ScanIndexPointer & ": " & ScanType.ToString()
        End Function
    End Structure

#End Region


    ' Note: We're keeping the Survey Scans separate from the Fragmentation Scans to make the creation of the
    '         survey scan based SIC's easier (and faster)
    '       The MasterScanOrder array allows us to step through the data scan-by-scan, using both SurveyScans and FragScans

    ''' <summary>
    ''' 0-based array, holding survey scans, the order is the same as in the original data file, and thus is by increasing scan number
    ''' </summary>
    Public SurveyScans As List(Of clsScanInfo)

    ''' <summary>
    ''' 0-based array, holding fragmentation scans, the order is the same as in the original data file, and thus is by increasing scan number
    ''' </summary>
    Public FragScans As List(Of clsScanInfo)

    ''' <summary>
    ''' 0-based array, holding pointers to either the SurveyScans or FragScans lists, in order of scan number
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
        SurveyScans = New List(Of clsScanInfo)
        FragScans = New List(Of clsScanInfo)
    End Sub

    Public Function AddFakeSurveyScan() As Integer
        Const intScanNumber = 0
        Const sngScanTime As Single = 0

        Return AddFakeSurveyScan(intScanNumber, sngScanTime)
    End Function

    ''' <summary>
    ''' Adds a "fake" survey scan with the given scan number and scan time
    ''' </summary>
    ''' <param name="scanNumber"></param>
    ''' <param name="scanTime"></param>
    ''' <returns>The index in SurveyScans() at which the new scan was added</returns>
    Private Function AddFakeSurveyScan(
      scanNumber As Integer,
      scanTime As Single) As Integer

        Dim surveyScan = GetFakeSurveyScan(scanNumber, scanTime)

        Dim intSurveyScanIndex = SurveyScans.Count

        SurveyScans.Add(surveyScan)

        AddMasterScanEntry(eScanTypeConstants.SurveyScan, intSurveyScanIndex)

        Return intSurveyScanIndex
    End Function

    Public Sub AddMasterScanEntry(eScanType As eScanTypeConstants, intScanIndex As Integer)
        ' Adds a new entry to .MasterScanOrder using an existing entry in SurveyScans() or FragScans()

        If eScanType = eScanTypeConstants.SurveyScan Then
            If SurveyScans.Count > 0 AndAlso intScanIndex < SurveyScans.Count Then
                AddMasterScanEntry(eScanType, intScanIndex, SurveyScans(intScanIndex).ScanNumber, SurveyScans(intScanIndex).ScanTime)
            Else
                ' This code shouldn't normally be reached
                ReportMessage($"Error in AddMasterScanEntry for ScanType {eScanType}, Survey ScanIndex {intScanIndex}: index is out of range")
                AddMasterScanEntry(eScanType, intScanIndex, 0, 0)
            End If

        ElseIf eScanType = eScanTypeConstants.FragScan Then
            If FragScans.Count > 0 AndAlso intScanIndex < FragScans.Count Then
                AddMasterScanEntry(eScanType, intScanIndex, FragScans(intScanIndex).ScanNumber, FragScans(intScanIndex).ScanTime)
            Else
                ' This code shouldn't normally be reached
                AddMasterScanEntry(eScanType, intScanIndex, 0, 0)
                ReportMessage($"Error in AddMasterScanEntry for ScanType {eScanType}, Frag ScanIndex {intScanIndex}: index is out of range")
            End If

        Else
            ' Unknown type; cannot add
            ReportError("AddMasterScanEntry", "Programming error: unknown value for eScanType: " & eScanType, Nothing, True, False)
        End If

    End Sub

    Public Sub AddMasterScanEntry(
       eScanType As eScanTypeConstants,
       intScanIndex As Integer,
       intScanNumber As Integer,
       sngScanTime As Single)

        Dim initialScanCount = MasterScanOrderCount

        If MasterScanOrder Is Nothing Then
            ReDim MasterScanOrder(99)
            ReDim MasterScanNumList(99)
            ReDim MasterScanTimeList(99)
        ElseIf initialScanCount >= MasterScanOrder.Length Then
            ReDim Preserve MasterScanOrder(initialScanCount + 100)
            ReDim Preserve MasterScanNumList(MasterScanOrder.Length - 1)
            ReDim Preserve MasterScanTimeList(MasterScanOrder.Length - 1)
        End If

        MasterScanOrder(initialScanCount).ScanType = eScanType
        MasterScanOrder(initialScanCount).ScanIndexPointer = intScanIndex

        MasterScanNumList(initialScanCount) = intScanNumber
        MasterScanTimeList(initialScanCount) = sngScanTime

        MasterScanOrderCount += 1

    End Sub

    Private Function GetFakeSurveyScan(scanNumber As Integer, scanTime As Single) As clsScanInfo

        Dim surveyScan = New clsScanInfo()
        surveyScan.ScanNumber = scanNumber
        surveyScan.ScanTime = scanTime
        surveyScan.ScanHeaderText = "Full ms"
        surveyScan.ScanTypeName = "MS"

        surveyScan.BasePeakIonMZ = 0
        surveyScan.BasePeakIonIntensity = 0
        surveyScan.FragScanInfo.ParentIonInfoIndex = -1                        ' Survey scans typically lead to multiple parent ions; we do not record them here
        surveyScan.TotalIonIntensity = 0

        surveyScan.ZoomScan = False
        surveyScan.SIMScan = False
        surveyScan.MRMScanType = MRMScanTypeConstants.NotMRM

        surveyScan.LowMass = 0
        surveyScan.HighMass = 0
        surveyScan.IsFTMS = False

        ' Store the collision mode and possibly the scan filter text
        surveyScan.FragScanInfo.CollisionMode = String.Empty

        Return surveyScan

    End Function

    Public Sub Initialize(
      intSurveyScanCountToAllocate As Integer,
      intFragScanCountToAllocate As Integer)

        Dim intMasterScanOrderCountToAllocate As Integer
        Dim intIndex As Integer

        If intSurveyScanCountToAllocate < 1 Then intSurveyScanCountToAllocate = 1
        If intFragScanCountToAllocate < 1 Then intFragScanCountToAllocate = 1

        intMasterScanOrderCountToAllocate = intSurveyScanCountToAllocate + intFragScanCountToAllocate

        SurveyScans.Clear()

        FragScans.Clear()

        MasterScanOrderCount = 0
        ReDim MasterScanOrder(intMasterScanOrderCountToAllocate - 1)
        ReDim MasterScanNumList(intMasterScanOrderCountToAllocate - 1)
        ReDim MasterScanTimeList(intMasterScanOrderCountToAllocate - 1)

        ParentIonInfoCount = 0
        ReDim ParentIons(intFragScanCountToAllocate - 1)
        For intIndex = 0 To intFragScanCountToAllocate - 1
            ParentIons(intIndex) = New clsParentIonInfo()
        Next intIndex

    End Sub


End Class
