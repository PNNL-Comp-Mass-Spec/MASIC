Public Class clsFragScanInfo
    ''' <summary>
    ''' Pointer to an entry in the ParentIons() array; -1 if undefined
    ''' </summary>
    Public Property ParentIonInfoIndex As Integer

    ''' <summary>
    ''' Parent ion m/z value
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ParentIonMz As Double

    ''' <summary>
    ''' The nth fragmentation scan after an MS1 scan
    ''' Computed as EventNumber - 1 since the first MS2 scan after a MS1 scan typically has EventNumber = 2
    ''' </summary>
    Public Property FragScanNumber As Integer

    ''' <summary>
    ''' 2 for MS/MS, 3 for MS/MS/MS
    ''' </summary>
    Public Property MSLevel As Integer

    ''' <summary>
    ''' Collision mode
    ''' </summary>
    Public Property CollisionMode As String

    ''' <summary>
    ''' Interference score: fraction of observed peaks that are from the precursor
    ''' Larger is better, with a max of 1 and minimum of 0
    ''' 1 means all peaks are from the precursor
    ''' </summary>
    Public Property InterferenceScore As Double

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(parentIonMzValue As Double)
        ' -1 means undefined; only used for fragmentation scans
        ParentIonInfoIndex = -1
        ParentIonMz = parentIonMzValue
        CollisionMode = String.Empty
    End Sub

    Public Overrides Function ToString() As String
        Return "Parent Ion " & ParentIonInfoIndex
    End Function
End Class
