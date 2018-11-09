Imports System.Runtime.InteropServices
Imports PRISM

Public Class clsDataAggregation
    Inherits EventNotifier

    ''' <summary>
    ''' When returnMax is false, determine the sum of the data within the search mass tolerance
    ''' When returnMaxis true, determine the maximum of the data within the search mass tolerance
    ''' </summary>
    ''' <param name="objMSSpectrum"></param>
    ''' <param name="searchMZ"></param>
    ''' <param name="searchToleranceHalfWidth"></param>
    ''' <param name="ionMatchCount"></param>
    ''' <param name="closestMZ"></param>
    ''' <param name="returnMax"></param>
    ''' <returns>The sum or maximum of the matching data; 0 if no matches</returns>
    ''' <remarks>
    ''' Note that this function performs a recursive search of objMSSpectrum.IonsMZ
    ''' It is therefore very efficient regardless of the number of data points in the spectrum
    ''' For sparse spectra, you can alternatively use FindMaxValueInMZRange
    ''' </remarks>
    Public Function AggregateIonsInRange(
      objMSSpectrum As clsMSSpectrum,
      searchMZ As Double,
      searchToleranceHalfWidth As Double,
      <Out> ByRef ionMatchCount As Integer,
      <Out> ByRef closestMZ As Double,
      returnMax As Boolean) As Single

        ionMatchCount = 0
        closestMZ = 0
        Dim ionSumOrMax As Single = 0

        Try


            Dim smallestDifference = Double.MaxValue

            If Not objMSSpectrum.IonsMZ Is Nothing AndAlso objMSSpectrum.IonCount > 0 Then
                Dim indexFirst, indexLast As Integer
                If SumIonsFindValueInRange(objMSSpectrum.IonsMZ, objMSSpectrum.IonCount, searchMZ, searchToleranceHalfWidth, indexFirst, indexLast) Then
                    For ionIndex = indexFirst To indexLast
                        If returnMax Then
                            ' Return max
                            If objMSSpectrum.IonsIntensity(ionIndex) > ionSumOrMax Then
                                ionSumOrMax = objMSSpectrum.IonsIntensity(ionIndex)
                            End If
                        Else
                            ' Return sum
                            ionSumOrMax += objMSSpectrum.IonsIntensity(ionIndex)
                        End If

                        Dim testDifference = Math.Abs(objMSSpectrum.IonsMZ(ionIndex) - searchMZ)
                        If testDifference < smallestDifference Then
                            smallestDifference = testDifference
                            closestMZ = objMSSpectrum.IonsMZ(ionIndex)
                        End If
                    Next
                    ionMatchCount = indexLast - indexFirst + 1
                End If
            End If

        Catch ex As Exception
            ionMatchCount = 0
        End Try

        Return ionSumOrMax

    End Function

    Public Function FindMaxValueInMZRange(
      objSpectraCache As clsSpectraCache,
      currentScan As clsScanInfo,
      mzStart As Double,
      mzEnd As Double,
      <Out> ByRef bestMatchMZ As Double,
      <Out> ByRef matchIntensity As Single) As Boolean

        ' Searches currentScan.IonsMZ for the maximum value between mzStart and mzEnd
        ' If a match is found, then updates bestMatchMZ to the m/z of the match, updates matchIntensity to its intensity,
        '  and returns True
        '
        ' Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
        '  and bad for spectra with > 10 data points
        ' As an alternative to this function, use AggregateIonsInRange

        Dim poolIndex As Integer

        bestMatchMZ = 0
        matchIntensity = 0

        Try
            If Not objSpectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, poolIndex) Then
                OnErrorEvent("Error uncaching scan " & currentScan.ScanNumber)
                Return False
            Else
                Dim success = FindMaxValueInMZRange(
                    objSpectraCache.SpectraPool(poolIndex).IonsMZ,
                    objSpectraCache.SpectraPool(poolIndex).IonsIntensity,
                    objSpectraCache.SpectraPool(poolIndex).IonCount,
                    mzStart, mzEnd,
                    bestMatchMZ, matchIntensity)

                Return success
            End If
        Catch ex As Exception
            OnErrorEvent("Error in FindMaxValueInMZRange (A): " & ex.Message, ex)
            Return False
        End Try

    End Function

    Private Function FindMaxValueInMZRange(
      mzList As IList(Of Double),
      intensityList As IList(Of Single),
      ionCount As Integer,
      mzStart As Double,
      mzEnd As Double,
      <Out> ByRef bestMatchMZ As Double,
      <Out> ByRef matchIntensity As Single) As Boolean

        ' Searches mzList for the maximum value between mzStart and mzEnd
        ' If a match is found, then updates bestMatchMZ to the m/z of the match, updates matchIntensity to its intensity,
        '  and returns True
        '
        ' Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
        '  and bad for spectra with > 10 data points
        ' As an alternative to this function, use AggregateIonsInRange

        Dim dataIndex As Integer
        Dim closestMatchIndex As Integer
        Dim highestIntensity As Single

        Try
            closestMatchIndex = -1
            highestIntensity = 0

            For dataIndex = 0 To ionCount - 1
                If mzList(dataIndex) >= mzStart AndAlso mzList(dataIndex) <= mzEnd Then
                    If closestMatchIndex < 0 Then
                        closestMatchIndex = dataIndex
                        highestIntensity = intensityList(dataIndex)
                    ElseIf intensityList(dataIndex) > highestIntensity Then
                        closestMatchIndex = dataIndex
                        highestIntensity = intensityList(dataIndex)
                    End If
                End If
            Next

        Catch ex As Exception
            OnErrorEvent("Error in FindMaxValueInMZRange (B): " & ex.Message, ex)
            closestMatchIndex = -1
        End Try

        If closestMatchIndex >= 0 Then
            bestMatchMZ = mzList(closestMatchIndex)
            matchIntensity = highestIntensity
            Return True
        Else
            bestMatchMZ = 0
            matchIntensity = 0
            Return False
        End If

    End Function


    Private Function SumIonsFindValueInRange(
      ByRef dataValues() As Double,
      dataCount As Integer,
      searchValue As Double,
      toleranceHalfWidth As Double,
      <Out> ByRef matchIndexStart As Integer,
      <Out> ByRef matchIndexEnd As Integer) As Boolean

        ' Searches dataValues for searchValue with a tolerance of +/-toleranceHalfWidth
        ' Returns True if a match is found; in addition, populates matchIndexStart and matchIndexEnd
        ' Otherwise, returns false

        Dim matchFound As Boolean

        matchIndexStart = 0
        matchIndexEnd = dataCount - 1

        If dataCount = 0 Then
            matchIndexEnd = -1
        ElseIf dataCount = 1 Then
            If Math.Abs(searchValue - dataValues(0)) > toleranceHalfWidth Then
                ' Only one data point, and it is not within tolerance
                matchIndexEnd = -1
            End If
        Else
            SumIonsBinarySearchRangeDbl(dataValues, searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        End If

        If matchIndexStart > matchIndexEnd Then
            matchIndexStart = -1
            matchIndexEnd = -1
            matchFound = False
        Else
            matchFound = True
        End If

        Return matchFound
    End Function

    Private Sub SumIonsBinarySearchRangeDbl(
      ByRef dataValues() As Double,
      searchValue As Double,
      toleranceHalfWidth As Double,
      ByRef matchIndexStart As Integer,
      ByRef matchIndexEnd As Integer)

        ' Recursive search function

        Dim indexMidpoint As Integer
        Dim leftDone As Boolean
        Dim rightDone As Boolean
        Dim leftIndex As Integer
        Dim rightIndex As Integer

        indexMidpoint = (matchIndexStart + matchIndexEnd) \ 2
        If indexMidpoint = matchIndexStart Then
            ' Min and Max are next to each other
            If Math.Abs(searchValue - dataValues(matchIndexStart)) > toleranceHalfWidth Then matchIndexStart = matchIndexEnd
            If Math.Abs(searchValue - dataValues(matchIndexEnd)) > toleranceHalfWidth Then matchIndexEnd = indexMidpoint
            Exit Sub
        End If

        If dataValues(indexMidpoint) > searchValue + toleranceHalfWidth Then
            ' Out of range on the right
            matchIndexEnd = indexMidpoint
            SumIonsBinarySearchRangeDbl(dataValues, searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        ElseIf dataValues(indexMidpoint) < searchValue - toleranceHalfWidth Then
            ' Out of range on the left
            matchIndexStart = indexMidpoint
            SumIonsBinarySearchRangeDbl(dataValues, searchValue, toleranceHalfWidth, matchIndexStart, matchIndexEnd)
        Else
            ' Inside range; figure out the borders
            leftIndex = indexMidpoint
            Do
                leftIndex = leftIndex - 1
                If leftIndex < matchIndexStart Then
                    leftDone = True
                Else
                    If Math.Abs(searchValue - dataValues(leftIndex)) > toleranceHalfWidth Then leftDone = True
                End If
            Loop While Not leftDone
            rightIndex = indexMidpoint

            Do
                rightIndex = rightIndex + 1
                If rightIndex > matchIndexEnd Then
                    rightDone = True
                Else
                    If Math.Abs(searchValue - dataValues(rightIndex)) > toleranceHalfWidth Then rightDone = True
                End If
            Loop While Not rightDone

            matchIndexStart = leftIndex + 1
            matchIndexEnd = rightIndex - 1
        End If

    End Sub

End Class
