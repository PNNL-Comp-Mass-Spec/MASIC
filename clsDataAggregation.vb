Imports System.Runtime.InteropServices

Public Class clsDataAggregation
    Inherits clsEventNotifier

    Private Function AggregateIonsInRange(
      objSpectraCache As clsSpectraCache,
      scanList As IList(Of clsScanInfo),
      intSpectrumIndex As Integer,
      dblSearchMZ As Double,
      dblSearchToleranceHalfWidth As Double,
      ByRef intIonMatchCount As Integer,
      ByRef dblClosestMZ As Double,
      blnReturnMax As Boolean) As Single


        Dim intPoolIndex As Integer
        Dim sngIonSumOrMax As Single

        Try
            intIonMatchCount = 0
            sngIonSumOrMax = 0

            If Not objSpectraCache.ValidateSpectrumInPool(scanList(intSpectrumIndex).ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum)
            Else
                sngIonSumOrMax = AggregateIonsInRange(objSpectraCache.SpectraPool(intPoolIndex),
                                                      dblSearchMZ, dblSearchToleranceHalfWidth,
                                                      intIonMatchCount, dblClosestMZ, blnReturnMax)
            End If
        Catch ex As Exception
            ReportError("AggregateIonsInRange_SpectraCache", "Error in AggregateIonsInRange", ex, True, False)
            intIonMatchCount = 0
        End Try

        Return sngIonSumOrMax

    End Function

    ''' <summary>
    ''' When blnReturnMax is false, determine the sum of the data within the search mass tolerance
    ''' When blnReturnMaxis true, determine the maximum of the data within the search mass tolerance
    ''' </summary>
    ''' <param name="objMSSpectrum"></param>
    ''' <param name="dblSearchMZ"></param>
    ''' <param name="dblSearchToleranceHalfWidth"></param>
    ''' <param name="intIonMatchCount"></param>
    ''' <param name="dblClosestMZ"></param>
    ''' <param name="blnReturnMax"></param>
    ''' <returns>The sum or maximum of the matching data; 0 if no matches</returns>
    ''' <remarks>
    ''' Note that this function performs a recursive search of objMSSpectrum.IonsMZ
    ''' It is therefore very efficient regardless of the number of data points in the spectrum
    ''' For sparse spectra, you can alternatively use FindMaxValueInMZRange
    ''' </remarks>
    Public Function AggregateIonsInRange(
      objMSSpectrum As clsMSSpectrum,
      dblSearchMZ As Double,
      dblSearchToleranceHalfWidth As Double,
      <Out()> ByRef intIonMatchCount As Integer,
      <Out()> ByRef dblClosestMZ As Double,
      blnReturnMax As Boolean) As Single

        intIonMatchCount = 0
        dblClosestMZ = 0
        Dim sngIonSumOrMax As Single = 0

        Try


            Dim dblSmallestDifference = Double.MaxValue

            If Not objMSSpectrum.IonsMZ Is Nothing AndAlso objMSSpectrum.IonCount > 0 Then
                Dim intIndexFirst, intIndexLast As Integer
                If SumIonsFindValueInRange(objMSSpectrum.IonsMZ, objMSSpectrum.IonCount, dblSearchMZ, dblSearchToleranceHalfWidth, intIndexFirst, intIndexLast) Then
                    For intIonIndex = intIndexFirst To intIndexLast
                        If blnReturnMax Then
                            ' Return max
                            If objMSSpectrum.IonsIntensity(intIonIndex) > sngIonSumOrMax Then
                                sngIonSumOrMax = objMSSpectrum.IonsIntensity(intIonIndex)
                            End If
                        Else
                            ' Return sum
                            sngIonSumOrMax += objMSSpectrum.IonsIntensity(intIonIndex)
                        End If

                        Dim dblTestDifference = Math.Abs(objMSSpectrum.IonsMZ(intIonIndex) - dblSearchMZ)
                        If dblTestDifference < dblSmallestDifference Then
                            dblSmallestDifference = dblTestDifference
                            dblClosestMZ = objMSSpectrum.IonsMZ(intIonIndex)
                        End If
                    Next intIonIndex
                    intIonMatchCount = intIndexLast - intIndexFirst + 1
                End If
            End If

        Catch ex As Exception
            intIonMatchCount = 0
        End Try

        Return sngIonSumOrMax

    End Function

    Public Function FindMaxValueInMZRange(
      objSpectraCache As clsSpectraCache,
      currentScan As clsScanInfo,
      dblMZStart As Double,
      dblMZEnd As Double,
      <Out()> ByRef dblBestMatchMZ As Double,
      <Out()> ByRef sngMatchIntensity As Single) As Boolean

        ' Searches currentScan.IonsMZ for the maximum value between dblMZStart and dblMZEnd
        ' If a match is found, then updates dblBestMatchMZ to the m/z of the match, updates sngMatchIntensity to its intensity,
        '  and returns True
        '
        ' Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
        '  and bad for spectra with > 10 data points
        ' As an alternative to this function, use AggregateIonsInRange

        Dim intPoolIndex As Integer
        Dim blnSuccess As Boolean
        dblBestMatchMZ = 0
        sngMatchIntensity = 0

        Try
            If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, intPoolIndex) Then
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum)
                blnSuccess = False
            Else
                With objSpectraCache.SpectraPool(intPoolIndex)
                    blnSuccess = FindMaxValueInMZRange(.IonsMZ, .IonsIntensity, .IonCount, dblMZStart, dblMZEnd, dblBestMatchMZ, sngMatchIntensity)
                End With
            End If
        Catch ex As Exception
            ReportError("FindMaxValueInMZRange", "Error in FindMaxValueInMZRange", ex, True, False)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

    Private Function FindMaxValueInMZRange(
      ByRef dblMZList() As Double,
      ByRef sngIntensityList() As Single,
      intIonCount As Integer,
      dblMZStart As Double,
      dblMZEnd As Double,
      <Out()> ByRef dblBestMatchMZ As Double,
      <Out()> ByRef sngMatchIntensity As Single) As Boolean

        ' Searches dblMZList for the maximum value between dblMZStart and dblMZEnd
        ' If a match is found, then updates dblBestMatchMZ to the m/z of the match, updates sngMatchIntensity to its intensity,
        '  and returns True
        '
        ' Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
        '  and bad for spectra with > 10 data points
        ' As an alternative to this function, use AggregateIonsInRange

        Dim intDataIndex As Integer
        Dim intClosestMatchIndex As Integer
        Dim sngHighestIntensity As Single

        Try
            intClosestMatchIndex = -1
            sngHighestIntensity = 0

            For intDataIndex = 0 To intIonCount - 1
                If dblMZList(intDataIndex) >= dblMZStart AndAlso dblMZList(intDataIndex) <= dblMZEnd Then
                    If intClosestMatchIndex < 0 Then
                        intClosestMatchIndex = intDataIndex
                        sngHighestIntensity = sngIntensityList(intDataIndex)
                    ElseIf sngIntensityList(intDataIndex) > sngHighestIntensity Then
                        intClosestMatchIndex = intDataIndex
                        sngHighestIntensity = sngIntensityList(intDataIndex)
                    End If
                End If
            Next intDataIndex

        Catch ex As Exception
            ReportError("FindMaxValueInMZRange", "Error in FindMaxValueInMZRange", ex, True, False)
            intClosestMatchIndex = -1
        End Try

        If intClosestMatchIndex >= 0 Then
            dblBestMatchMZ = dblMZList(intClosestMatchIndex)
            sngMatchIntensity = sngHighestIntensity
            Return True
        Else
            dblBestMatchMZ = 0
            sngMatchIntensity = 0
            Return False
        End If

    End Function


    Private Function SumIonsFindValueInRange(
      ByRef DataDouble() As Double,
      intDataCount As Integer,
      dblSearchValue As Double,
      dblToleranceHalfWidth As Double,
      <Out()> ByRef intMatchIndexStart As Integer,
      <Out()> ByRef intMatchIndexEnd As Integer) As Boolean

        ' Searches DataDouble for dblSearchValue with a tolerance of +/-dblToleranceHalfWidth
        ' Returns True if a match is found; in addition, populates intMatchIndexStart and intMatchIndexEnd
        ' Otherwise, returns false

        Dim blnMatchFound As Boolean

        intMatchIndexStart = 0
        intMatchIndexEnd = intDataCount - 1

        If intDataCount = 0 Then
            intMatchIndexEnd = -1
        ElseIf intDataCount = 1 Then
            If Math.Abs(dblSearchValue - DataDouble(0)) > dblToleranceHalfWidth Then
                ' Only one data point, and it is not within tolerance
                intMatchIndexEnd = -1
            End If
        Else
            SumIonsBinarySearchRangeDbl(DataDouble, dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        End If

        If intMatchIndexStart > intMatchIndexEnd Then
            intMatchIndexStart = -1
            intMatchIndexEnd = -1
            blnMatchFound = False
        Else
            blnMatchFound = True
        End If

        Return blnMatchFound
    End Function

    Private Sub SumIonsBinarySearchRangeDbl(
      ByRef DataDouble() As Double,
      dblSearchValue As Double,
      dblToleranceHalfWidth As Double,
      ByRef intMatchIndexStart As Integer,
      ByRef intMatchIndexEnd As Integer)

        ' Recursive search function

        Dim intIndexMidpoint As Integer
        Dim blnLeftDone As Boolean
        Dim blnRightDone As Boolean
        Dim intLeftIndex As Integer
        Dim intRightIndex As Integer

        intIndexMidpoint = (intMatchIndexStart + intMatchIndexEnd) \ 2
        If intIndexMidpoint = intMatchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(dblSearchValue - DataDouble(intMatchIndexStart)) > dblToleranceHalfWidth Then intMatchIndexStart = intMatchIndexEnd
            If Math.Abs(dblSearchValue - DataDouble(intMatchIndexEnd)) > dblToleranceHalfWidth Then intMatchIndexEnd = intIndexMidpoint
            Exit Sub
        End If

        If DataDouble(intIndexMidpoint) > dblSearchValue + dblToleranceHalfWidth Then
            ' Out of range on the right
            intMatchIndexEnd = intIndexMidpoint
            SumIonsBinarySearchRangeDbl(DataDouble, dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        ElseIf DataDouble(intIndexMidpoint) < dblSearchValue - dblToleranceHalfWidth Then
            ' Out of range on the left
            intMatchIndexStart = intIndexMidpoint
            SumIonsBinarySearchRangeDbl(DataDouble, dblSearchValue, dblToleranceHalfWidth, intMatchIndexStart, intMatchIndexEnd)
        Else
            ' Inside range; figure out the borders
            intLeftIndex = intIndexMidpoint
            Do
                intLeftIndex = intLeftIndex - 1
                If intLeftIndex < intMatchIndexStart Then
                    blnLeftDone = True
                Else
                    If Math.Abs(dblSearchValue - DataDouble(intLeftIndex)) > dblToleranceHalfWidth Then blnLeftDone = True
                End If
            Loop While Not blnLeftDone
            intRightIndex = intIndexMidpoint

            Do
                intRightIndex = intRightIndex + 1
                If intRightIndex > intMatchIndexEnd Then
                    blnRightDone = True
                Else
                    If Math.Abs(dblSearchValue - DataDouble(intRightIndex)) > dblToleranceHalfWidth Then blnRightDone = True
                End If
            Loop While Not blnRightDone

            intMatchIndexStart = intLeftIndex + 1
            intMatchIndexEnd = intRightIndex - 1
        End If

    End Sub

End Class
