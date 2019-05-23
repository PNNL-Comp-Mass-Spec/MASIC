Public Class clsUniqueMZListItem

    ''' <summary>
    ''' Average m/z
    ''' </summary>
    Public Property MZAvg As Double

    ''' <summary>
    ''' Highest intensity value of the similar parent ions
    ''' </summary>
    Public Property MaxIntensity As Double

    ''' <summary>
    ''' Largest peak intensity value of the similar parent ions
    ''' </summary>
    Public Property MaxPeakArea As Double

    ''' <summary>
    ''' Scan number of the parent ion with the highest intensity
    ''' </summary>
    Public Property ScanNumberMaxIntensity As Integer

    ''' <summary>
    ''' Elution time of the parent ion with the highest intensity
    ''' </summary>
    Public Property ScanTimeMaxIntensity As Single

    ''' <summary>
    ''' Pointer to an entry in scanList.ParentIons
    ''' </summary>
    Public Property ParentIonIndexMaxIntensity As Integer

    ''' <summary>
    ''' Pointer to an entry in scanList.ParentIons
    ''' </summary>
    Public Property ParentIonIndexMaxPeakArea As Integer

    ''' <summary>
    ''' Number of items in MatchIndices
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property MatchCount As Integer
        Get
            Return MatchIndices.Count
        End Get
    End Property

    ''' <summary>
    ''' Pointers to entries in scanList.ParentIons
    ''' </summary>
    Public ReadOnly MatchIndices As List(Of Integer)

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        MatchIndices = New List(Of Integer)
    End Sub

    Public Overrides Function ToString() As String
        Return "m/z avg: " & MZAvg & ", MatchCount: " & MatchIndices.Count
    End Function

End Class
