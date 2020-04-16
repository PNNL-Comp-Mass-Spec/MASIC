Namespace DatasetStats
    Public Class DatasetSummaryStats

        Public Property ElutionTimeMax As Double

        Public ReadOnly Property MSStats As SummaryStatDetails

        Public ReadOnly Property MSnStats As SummaryStatDetails

        ''' <summary>
        ''' Keeps track of each ScanType in the dataset, along with the number of scans of this type
        ''' </summary>
        ''' <remarks>
        ''' Examples
        '''   FTMS + p NSI Full ms
        '''   ITMS + c ESI Full ms
        '''   ITMS + p ESI d Z ms
        '''   ITMS + c ESI d Full ms2 @cid35.00
        ''' </remarks>
        Public ReadOnly Property ScanTypeStats As Dictionary(Of String, Integer)

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Sub New()
            MSStats = New SummaryStatDetails()
            MSnStats = New SummaryStatDetails()
            ScanTypeStats = New Dictionary(Of String, Integer)()
            Clear()
        End Sub

        Public Sub Clear()
            ElutionTimeMax = 0
            MSStats.Clear()
            MSnStats.Clear()
            ScanTypeStats.Clear()
        End Sub

    End Class
End Namespace
