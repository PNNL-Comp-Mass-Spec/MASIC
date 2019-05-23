Imports ThermoRawFileReader

''' <summary>
''' Used to track all spectra (scans) in the instrument data file
''' </summary>
Public Class clsScanList
    Inherits clsMasicEventNotifier

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
    ''' List of survey scans, the order is the same as in the original data file, and thus is by increasing scan number
    ''' </summary>
    Public ReadOnly SurveyScans As List(Of clsScanInfo)

    ''' <summary>
    ''' List of fragmentation scans, the order is the same as in the original data file, and thus is by increasing scan number
    ''' </summary>
    Public ReadOnly FragScans As List(Of clsScanInfo)

    ''' <summary>
    ''' List holding pointers to either the SurveyScans or FragScans lists, in order of scan number
    ''' </summary>
    Public ReadOnly MasterScanOrder As List(Of udtScanOrderPointerType)

    ''' <summary>
    ''' List of scan numbers, parallel to MasterScanOrder
    ''' </summary>
    Public ReadOnly MasterScanNumList As List(Of Integer)

    ''' <summary>
    ''' List of scan times (elution timers), parallel to MasterScanOrder
    ''' </summary>
    Public ReadOnly MasterScanTimeList As List(Of Single)

    ''' <summary>
    ''' List of parent ions
    ''' </summary>
    Public ReadOnly ParentIons As List(Of clsParentIonInfo)

    ''' <summary>
    ''' Number of items in MasterScanOrder
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property MasterScanOrderCount As Integer
        Get
            Return MasterScanOrder.Count
        End Get
    End Property

    ''' <summary>
    ''' Set to true if the user cancels any of the processing steps
    ''' </summary>
    Public Property ProcessingIncomplete As Boolean

    ''' <summary>
    ''' Will be true if SIM data is present
    ''' </summary>
    ''' <returns></returns>
    Public Property SIMDataPresent As Boolean

    ''' <summary>
    ''' Will be true if MRM data is present
    ''' </summary>
    ''' <returns></returns>
    Public Property MRMDataPresent As Boolean

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        SurveyScans = New List(Of clsScanInfo)
        FragScans = New List(Of clsScanInfo)

        MasterScanOrder = New List(Of udtScanOrderPointerType)
        MasterScanNumList = New List(Of Integer)
        MasterScanTimeList = New List(Of Single)

        ParentIons = New List(Of clsParentIonInfo)
    End Sub

    Public Function AddFakeSurveyScan() As Integer
        Const scanNumber = 0
        Const scanTime As Single = 0

        Return AddFakeSurveyScan(scanNumber, scanTime)
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

        Dim surveyScanIndex = SurveyScans.Count

        SurveyScans.Add(surveyScan)

        AddMasterScanEntry(eScanTypeConstants.SurveyScan, surveyScanIndex)

        Return surveyScanIndex
    End Function

    Public Sub AddMasterScanEntry(eScanType As eScanTypeConstants, scanIndex As Integer)
        ' Adds a new entry to .MasterScanOrder using an existing entry in SurveyScans() or FragScans()

        If eScanType = eScanTypeConstants.SurveyScan Then
            If SurveyScans.Count > 0 AndAlso scanIndex < SurveyScans.Count Then
                AddMasterScanEntry(eScanType, scanIndex, SurveyScans(scanIndex).ScanNumber, SurveyScans(scanIndex).ScanTime)
            Else
                ' This code shouldn't normally be reached
                ReportMessage($"Error in AddMasterScanEntry for ScanType {eScanType}, Survey ScanIndex {scanIndex}: index is out of range")
                AddMasterScanEntry(eScanType, scanIndex, 0, 0)
            End If

        ElseIf eScanType = eScanTypeConstants.FragScan Then
            If FragScans.Count > 0 AndAlso scanIndex < FragScans.Count Then
                AddMasterScanEntry(eScanType, scanIndex, FragScans(scanIndex).ScanNumber, FragScans(scanIndex).ScanTime)
            Else
                ' This code shouldn't normally be reached
                AddMasterScanEntry(eScanType, scanIndex, 0, 0)
                ReportMessage($"Error in AddMasterScanEntry for ScanType {eScanType}, Frag ScanIndex {scanIndex}: index is out of range")
            End If

        Else
            ' Unknown type; cannot add
            ReportError("Programming error: unknown value for eScanType: " & eScanType)
        End If

    End Sub

    Public Sub AddMasterScanEntry(
       eScanType As eScanTypeConstants,
       scanIndex As Integer,
       scanNumber As Integer,
       scanTime As Single)

        Dim newScanEntry = New udtScanOrderPointerType With {
            .ScanType = eScanType,
            .ScanIndexPointer = scanIndex
        }

        MasterScanOrder.Add(newScanEntry)

        MasterScanNumList.Add(scanNumber)
        MasterScanTimeList.Add(scanTime)

    End Sub

    Private Function GetFakeSurveyScan(scanNumber As Integer, scanTime As Single) As clsScanInfo
        Dim surveyScan = New clsScanInfo() With {
            .ScanNumber = scanNumber,
            .ScanTime = scanTime,
            .ScanHeaderText = "Full ms",
            .ScanTypeName = "MS",
            .BasePeakIonMZ = 0,
            .BasePeakIonIntensity = 0,
            .TotalIonIntensity = 0,
            .ZoomScan = False,
            .SIMScan = False,
            .MRMScanType = MRMScanTypeConstants.NotMRM,
            .LowMass = 0,
            .HighMass = 0,
            .IsFTMS = False
        }

        ' Survey scans typically lead to multiple parent ions; we do not record them here
        surveyScan.FragScanInfo.ParentIonInfoIndex = -1

        ' Store the collision mode and possibly the scan filter text
        surveyScan.FragScanInfo.CollisionMode = String.Empty

        Return surveyScan

    End Function

    Public Sub Initialize()

        SurveyScans.Clear()

        FragScans.Clear()

        MasterScanOrder.Clear()
        MasterScanNumList.Clear()
        MasterScanTimeList.Clear()

        ParentIons.Clear()

    End Sub


End Class
