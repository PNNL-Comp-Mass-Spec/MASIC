Imports System.Collections.Generic

Public Class clsPeaksContainer

    Public Property OriginalPeakLocationIndex As Integer

    Public Property SourceDataCount As Integer
    Public XData() As Double
    Public YData() As Double
    Public SmoothedYData() As Double

    Public Property Peaks As List(Of clsPeakInfo)

    Public Property PeakWidthPointsMinimum As Integer
    Public Property MaxAllowedUpwardSpikeFractionMax As Single
    Public Property BestPeakIndex As Integer
    Public Property BestPeakArea As Single

    Public Sub New()
        Peaks = New List(Of clsPeakInfo)
    End Sub

    Public Function Clone(Optional skipSourceData As Boolean = False) As clsPeaksContainer
        Dim clonedContainer = New clsPeaksContainer() With {
            .OriginalPeakLocationIndex = Me.OriginalPeakLocationIndex,
            .SourceDataCount = Me.SourceDataCount,
            .PeakWidthPointsMinimum = Me.PeakWidthPointsMinimum,
            .MaxAllowedUpwardSpikeFractionMax = Me.MaxAllowedUpwardSpikeFractionMax,
            .BestPeakIndex = Me.BestPeakIndex,
            .BestPeakArea = Me.BestPeakArea
        }

        If skipSourceData OrElse Me.SourceDataCount <= 0 Then
            clonedContainer.SourceDataCount = 0

            ReDim clonedContainer.XData(-1)
            ReDim clonedContainer.YData(-1)
            ReDim clonedContainer.SmoothedYData(-1)

        Else
            ReDim clonedContainer.XData(Me.SourceDataCount)
            ReDim clonedContainer.YData(Me.SourceDataCount)
            ReDim clonedContainer.SmoothedYData(Me.SourceDataCount)

            Me.XData.CopyTo(clonedContainer.XData, 0)
            Me.YData.CopyTo(clonedContainer.YData, 0)
            Me.SmoothedYData.CopyTo(clonedContainer.SmoothedYData, 0)
        End If

        For Each sourcePeak In Me.Peaks
            clonedContainer.Peaks.Add(sourcePeak.Clone())
        Next

        Return clonedContainer

    End Function
End Class
