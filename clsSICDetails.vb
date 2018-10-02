Imports MASICPeakFinder

Public Class clsSICDetails

    ''' <summary>
    ''' Indicates the type of scans that the SICScanIndices() array points to. Will normally be "SurveyScan", but for MRM data will be "FragScan"
    ''' </summary>
    Public SICScanType As clsScanList.eScanTypeConstants

    Public ReadOnly SICData As List(Of clsSICDataPoint)

    Public ReadOnly Property SICDataCount As Integer
        Get
            Return SICData.Count
        End Get
    End Property

    Public ReadOnly Property SICIntensities As Single()
        Get
            Return (From item In SICData Select item.Intensity).ToArray()
        End Get
    End Property

    Public ReadOnly Property SICMassesAsFloat As Single()
        Get
            Return (From item In SICData Select CSng(item.Mass)).ToArray()
        End Get
    End Property

    Public ReadOnly Property SICMasses As Double()
        Get
            Return (From item In SICData Select item.Mass).ToArray()
        End Get
    End Property

    Public ReadOnly Property SICScanNumbers As Integer()
        Get
            Return (From item In SICData Select item.ScanNumber).ToArray()
        End Get
    End Property

    Public ReadOnly Property SICScanIndices As Integer()
        Get
            Return (From item In SICData Select item.ScanIndex).ToArray()
        End Get
    End Property

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        SICData = New List(Of clsSICDataPoint)
    End Sub

    Public Sub AddData(scanNumber As Integer, intensity As Single, mass As Double, scanIndex As Integer)
        Dim dataPoint = New clsSICDataPoint(scanNumber, intensity, mass, scanIndex)
        SICData.Add(dataPoint)
    End Sub

    Public Sub Reset()
        SICData.Clear()
        SICScanType = clsScanList.eScanTypeConstants.SurveyScan
    End Sub

    Public Overrides Function ToString() As String
        Return "SICDataCount: " & SICData.Count
    End Function

End Class
