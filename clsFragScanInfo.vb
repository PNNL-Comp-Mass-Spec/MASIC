Public Class clsFragScanInfo
    ''' <summary>
    ''' Pointer to an entry in the ParentIons() array; -1 if undefined
    ''' </summary>
    Public ParentIonInfoIndex As Integer

    ''' <summary>
    ''' Usually 1, 2, or 3
    ''' </summary>
    Public FragScanNumber As Integer

    ''' <summary>
    ''' 2 for MS/MS, 3 for MS/MS/MS
    ''' </summary>
    Public MSLevel As Integer

    ''' <summary>
    ''' Collision mode
    ''' </summary>
    Public CollisionMode As String

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        ParentIonInfoIndex = -1
    End Sub
End Class
