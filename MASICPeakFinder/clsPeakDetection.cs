Option Explicit On
Option Strict On

Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Friend Class clsPeakDetection

    ' Peak detection routines
    ' Written by Matthew Monroe in roughly 2001 at UNC (Chapel Hill, NC)
    ' Kevin Lan provided the concept of Magnitude Concavity fitting
    ' Ported from LabView code to VB 6 in June 2003 at PNNL (Richland, WA)
    ' Ported from VB 6 to VB.NET in October 2003
    ' Switched from using the eols.dll least squares fitting routine to using a local function

    ' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
    ' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

    Private Enum eTermFunctionConstants
        One = 0
        X = 1
        LogX = 2
        Log10X = 3
        ExpX = 4
        SinX = 5
        CosX = 6
        TanX = 7
        ATanX = 8
    End Enum

    Private Structure udtLeastSquaresFitEquationTermType
        Public Func As eTermFunctionConstants
        Public Power As Double
        Public Coefficient As Double
        Public Inverse As Boolean

        Public ParamResult As Double        ' Stores the coefficient determined for the fit
    End Structure

    ''Private mEolsDllNotFound As Boolean

    Public Function ComputeSlope(xValuesZeroBased() As Double, yValuesZeroBased() As Double, startIndex As Integer, endIndex As Integer) As Double

        Const POLYNOMIAL_ORDER = 1

        Dim segmentX() As Double
        Dim segmentY() As Double

        Dim coefficients() As Double = Nothing

        If xValuesZeroBased Is Nothing OrElse xValuesZeroBased.Length = 0 Then Return 0

        Dim segmentCount = endIndex - startIndex + 1

        ReDim segmentX(segmentCount - 1)
        ReDim segmentY(segmentCount - 1)

        ' Copy the desired segment of data from xValues to segmentX and yValues to segmentY
        For index = startIndex To endIndex
            segmentX(index - startIndex) = xValuesZeroBased(index)
            segmentY(index - startIndex) = yValuesZeroBased(index)
        Next

        ' Compute the coefficients for the curve fit
        LeastSquaresFit(segmentX, segmentY, coefficients, POLYNOMIAL_ORDER)

        Return coefficients(1)

    End Function

    Public Function DetectPeaks(
      xValuesZeroBased() As Double,
      yValuesZeroBased() As Double,
      intensityThresholdAbsoluteMinimum As Double,
      peakWidthPointsMinimum As Integer,
      Optional peakDetectIntensityThresholdPercentageOfMaximum As Integer = 0,
      Optional peakWidthInSigma As Integer = 4,
      Optional useValleysForPeakWidth As Boolean = True,
      Optional movePeakLocationToMaxIntensity As Boolean = True) As List(Of clsPeakInfo)

        ' Finds peaks in the parallel arrays xValuesZeroBased() and yValuesZeroBased()
        ' intensityThreshold is the minimum absolute intensity allowable for a peak
        ' peakDetectIntensityThresholdPercentageOfMaximum allows one to specify a minimum intensity as a percentage of the maximum peak intensity
        ' Note that the maximum value of intensityThreshold vs. MaxValue*peakDetectIntensityThresholdPercentageOfMaximum is used as the minimum
        ' For example, if intensityThreshold = 10 and peakDetectIntensityThresholdPercentageOfMaximum =  5 (indicating 5%),
        '   then if the maximum of yValuesZeroBased() is 50, then the minimum intensity of identified peaks is 10, and not 2.5
        '   However, if the maximum of yValuesZeroBased() is 500, then the minimum intensity of identified peaks is 50, and not 10

        ' Returns the locations of the peaks in peakLocations() -- indices of the peak apexes in the source arrays
        ' Returns the left edges of the peaks (in points, not actual units) in peakEdgesLeft()       -- These values could be negative if useValleysForPeakWidth = False
        ' Returns the right edges of the peaks in peakEdgesRight()                                   -- These values could be larger than sourceDataCount-1 if useValleysForPeakWidth = False
        ' Returns the areas of the peaks in peakAreas()

        ' Note: Compute peak width using: peakWidthPoints = newPeak.RightEdge - newPeak.LeftEdge + 1

        ' The function returns the number of peaks found; if none are found, returns 0

        ' Uses the Magnitude-Concavity method, wherein a second order
        '   polynomial is fit to the points within the window, giving a_2*x^2 + a_1*x + a_0
        '   Given this, a_1 is the first derivative and a_2 is the second derivative
        ' From this, the first derivative gives the index of the peak apex
        ' The standard deviation (s) can be found using:
        '   s = sqrt(-h(t_r) / h''(t_r))
        '  where h(t_r) is the height of the peak at the peak center
        '  and h''(t_r) is the height of the second derivative of the peak
        ' In chromatography, the baseline peak widthInPoints = 4*sigma

        Dim firstDerivative() As Double, secondDerivative() As Double

        Dim xValuesForArea() As Double, yValuesForArea() As Double

        ' Initialize the list of detected peaks
        Dim detectedPeaks = New List(Of clsPeakInfo)

        Try


            Dim sourceDataCount = xValuesZeroBased.Length
            If sourceDataCount <= 0 Then Return detectedPeaks

            ' Reserve space for the first and second derivatives
            ReDim firstDerivative(sourceDataCount - 1)
            ReDim secondDerivative(sourceDataCount - 1)

            ' The mid point width is the minimum width divided by 2, rounded down
            Dim peakHalfWidth = CInt(Math.Floor(peakWidthPointsMinimum / 2))

            ' Find the maximum intensity in the source data
            Dim maximumIntensity As Double = 0
            For index = 0 To sourceDataCount - 1
                If yValuesZeroBased(index) > maximumIntensity Then
                    maximumIntensity = yValuesZeroBased(index)
                End If
            Next

            Dim intensityThreshold = maximumIntensity * (peakDetectIntensityThresholdPercentageOfMaximum / 100.0)
            If intensityThreshold < intensityThresholdAbsoluteMinimum Then
                intensityThreshold = intensityThresholdAbsoluteMinimum
            End If

            ' Exit the function if none of the data is above the minimum threshold
            If maximumIntensity < intensityThreshold Then Return detectedPeaks

            ' Do the actual work
            FitSegments(xValuesZeroBased, yValuesZeroBased, sourceDataCount, peakWidthPointsMinimum, peakHalfWidth, firstDerivative, secondDerivative)

            If peakWidthInSigma < 1 Then peakWidthInSigma = 1

            ' Examine the First Derivative function and look for zero crossings (in the downward direction)
            ' If looking for valleys, would look for zero crossings in the upward direction
            ' Only significant if intensity of point is above threshold
            If peakWidthPointsMinimum <= 0 Then peakWidthPointsMinimum = 1

            ' We'll start looking for peaks halfway into peakWidthPointsMinimum
            Dim indexFirst = peakHalfWidth
            Dim indexLast = sourceDataCount - 1 - peakHalfWidth

            For index = indexFirst To indexLast
                If firstDerivative(index) > 0 And firstDerivative(index + 1) < 0 Then
                    ' Possible peak
                    If yValuesZeroBased(index) >= intensityThreshold Or yValuesZeroBased(index + 1) >= intensityThreshold Then
                        ' Actual peak

                        Dim newPeak = New clsPeakInfo(index)

                        If useValleysForPeakWidth Then
                            ' Determine the peak width by looking for the adjacent valleys
                            ' If, while looking, we find peakWidthPointsMinimum / 2 points in a row with intensity values below intensityThreshold, then
                            ' set the edge peakHalfWidth - 1 points closer to the peak maximum

                            If index > 0 Then
                                newPeak.LeftEdge = 0
                                Dim lowIntensityPointCount = 0
                                For compareIndex = index - 1 To 0 Step -1
                                    If firstDerivative(compareIndex) <= 0 And firstDerivative(compareIndex + 1) >= 0 Then
                                        ' Found a valley; this is the left edge
                                        newPeak.LeftEdge = compareIndex + 1
                                        Exit For
                                    ElseIf yValuesZeroBased(compareIndex) < intensityThreshold Then
                                        lowIntensityPointCount += 1
                                        If lowIntensityPointCount > peakHalfWidth Then
                                            newPeak.LeftEdge = compareIndex + (peakHalfWidth - 1)
                                            Exit For
                                        End If
                                    Else
                                        lowIntensityPointCount = 0
                                    End If
                                Next
                            Else
                                newPeak.LeftEdge = 0
                            End If

                            If index < sourceDataCount - 2 Then
                                newPeak.RightEdge = sourceDataCount - 1
                                Dim lowIntensityPointCount = 0
                                For compareIndex = index + 1 To sourceDataCount - 2
                                    If firstDerivative(compareIndex) <= 0 And firstDerivative(compareIndex + 1) >= 0 Then
                                        ' Found a valley; this is the right edge
                                        newPeak.RightEdge = compareIndex
                                        Exit For
                                    ElseIf yValuesZeroBased(compareIndex) < intensityThreshold Then
                                        lowIntensityPointCount += 1
                                        If lowIntensityPointCount > peakHalfWidth Then
                                            newPeak.RightEdge = compareIndex - (peakHalfWidth - 1)
                                            Exit For
                                        End If
                                    Else
                                        lowIntensityPointCount = 0
                                    End If
                                Next
                            Else
                                newPeak.RightEdge = sourceDataCount - 1
                            End If

                            If newPeak.LeftEdge > newPeak.PeakLocation Then
                                Console.WriteLine("Warning: Left edge is > peak center; this is unexpected (clsPeakDetection->DetectPeaks)")
                                newPeak.LeftEdge = newPeak.PeakLocation
                            End If

                            If newPeak.RightEdge < newPeak.PeakLocation Then
                                Console.WriteLine("Warning: Right edge is < peak center; this is unexpected (clsPeakDetection->DetectPeaks)")
                                newPeak.RightEdge = newPeak.PeakLocation
                            End If

                        Else
                            ' Examine the Second Derivative to determine peak Width (in points)

                            Dim sigma as Double

                            Try
                                ' If secondDerivative(index)) is tiny, the following division will fail
                                sigma = Math.Sqrt(Math.Abs(-yValuesZeroBased(index) / secondDerivative(index)))
                            Catch ex As Exception
                                sigma = 0
                            End Try
                            Dim widthInPoints = CInt(Math.Ceiling(peakWidthInSigma * sigma))

                            If widthInPoints > 4 * sourceDataCount Then
                                ' Predicted width is over 4 times the data count
                                ' Set it to be 4 times the data count
                                widthInPoints = sourceDataCount * 4
                            End If

                            If widthInPoints < 2 Then widthInPoints = 2

                            ' If the peak width is odd, then center around index
                            ' Otherwise, offset to the right of index
                            If widthInPoints Mod 2 = 0 Then
                                ' Even number
                                newPeak.LeftEdge = index - CInt(widthInPoints / 2)
                                newPeak.RightEdge = index + CInt(widthInPoints / 2) - 1
                            Else
                                ' Odd number
                                newPeak.LeftEdge = index - CInt((widthInPoints - 1) / 2)
                                newPeak.RightEdge = index + CInt((widthInPoints - 1) / 2)
                            End If

                        End If

                        detectedPeaks.Add(newPeak)

                    End If
                End If
            Next

            ' Compute the peak areas
            For Each peakItem In detectedPeaks
                Dim thisPeakWidthInPoints = peakItem.RightEdge - peakItem.LeftEdge + 1

                If thisPeakWidthInPoints > 0 Then
                    If thisPeakWidthInPoints = 1 Then
                        ' I don't think this can happen
                        ' Just in case, we'll set the area equal to the peak intensity
                        peakItem.PeakArea = yValuesZeroBased(peakItem.PeakLocation)
                    Else
                        ReDim xValuesForArea(thisPeakWidthInPoints - 1)
                        ReDim yValuesForArea(thisPeakWidthInPoints - 1)

                        Dim thisPeakStartIndex = peakItem.LeftEdge
                        Dim thisPeakEndIndex = peakItem.RightEdge

                        If thisPeakStartIndex < 0 Then
                            ' This will happen if the width is too large, or if not all of the peak's data was included in the data arrays
                            thisPeakStartIndex = 0
                        End If

                        If thisPeakEndIndex >= sourceDataCount Then
                            ' This will happen if the width is too large, or if not all of the peak's data was included in the data arrays
                            thisPeakEndIndex = sourceDataCount - 1
                        End If

                        For areaValsCopyIndex = thisPeakStartIndex To thisPeakEndIndex
                            xValuesForArea(areaValsCopyIndex - thisPeakStartIndex) = xValuesZeroBased(areaValsCopyIndex)
                            yValuesForArea(areaValsCopyIndex - thisPeakStartIndex) = yValuesZeroBased(areaValsCopyIndex)
                        Next

                        peakItem.PeakArea = FindArea(xValuesForArea, yValuesForArea, thisPeakWidthInPoints)

                    End If
                Else
                    ' 0-width peak; this shouldn't happen
                    Console.WriteLine("Warning: 0-width peak; this shouldn't happen (clsPeakDetection->DetectPeaks)")
                    peakItem.PeakArea = 0
                End If
            Next

            If movePeakLocationToMaxIntensity Then
                For Each peakItem In detectedPeaks
                    ' The peak finder often determines the peak center to be a few points away from the peak apex -- check for this
                    ' Define the maximum allowed peak apex shift to be 33% of peakWidthPointsMinimum
                    Dim dataIndexCheckStart = peakItem.PeakLocation - CInt(Math.Floor(peakWidthPointsMinimum / 3))
                    If dataIndexCheckStart < 0 Then dataIndexCheckStart = 0

                    Dim dataIndexCheckEnd = peakItem.PeakLocation + CInt(Math.Floor(peakWidthPointsMinimum / 3))
                    If dataIndexCheckEnd > sourceDataCount - 1 Then dataIndexCheckEnd = sourceDataCount - 1

                    maximumIntensity = yValuesZeroBased(peakItem.PeakLocation)
                    For dataIndexCheck = dataIndexCheckStart To dataIndexCheckEnd
                        If yValuesZeroBased(dataIndexCheck) > maximumIntensity Then
                            peakItem.PeakLocation = dataIndexCheck
                            maximumIntensity = yValuesZeroBased(dataIndexCheck)
                        End If
                    Next

                    If peakItem.PeakLocation < peakItem.LeftEdge Then peakItem.LeftEdge = peakItem.PeakLocation
                    If peakItem.PeakLocation > peakItem.RightEdge Then peakItem.RightEdge = peakItem.PeakLocation
                Next
            End If

        Catch ex As Exception
            Console.WriteLine("Warning: Error in clsPeakDetection->DetectPeaks (or in a child function)" & vbCrLf & ex.Message)
        End Try

        Return detectedPeaks

    End Function

    Private Function FindArea(xValues As IList(Of Double), yValues As IList(Of Double), arrayCount As Integer) As Double
        ' yValues() should be 0-based

        ' Finds the area under the curve, using trapezoidal integration

        Dim area As Double = 0

        For index = 0 To arrayCount - 2
            ' Area of a trapezoid (turned on its side) is:
            '   0.5 * d * (h1 + h2)
            ' where d is the distance between two points, and h1 and h2 are the intensities
            '   at the 2 points

            area += 0.5 * Math.Abs(xValues(index + 1) - xValues(index)) * (yValues(index) + yValues(index + 1))
        Next

        Return area

    End Function

    Private Sub FitSegments(xValues As IList(Of Double), yValues As IList(Of Double), sourceDataCount As Integer, peakWidthPointsMinimum As Integer, peakWidthMidPoint As Integer, ByRef firstDerivative() As Double, ByRef secondDerivative() As Double)
        ' xValues() and yValues() are zero-based arrays

        Const POLYNOMIAL_ORDER = 2

        Dim segmentX() As Double
        Dim segmentY() As Double

        Dim coefficients() As Double = Nothing

        ' If POLYNOMIAL_ORDER < 2 Then POLYNOMIAL_ORDER = 2
        ' If POLYNOMIAL_ORDER > 9 Then POLYNOMIAL_ORDER = 9

        ReDim segmentX(peakWidthPointsMinimum - 1)
        ReDim segmentY(peakWidthPointsMinimum - 1)

        For startIndex = 0 To sourceDataCount - peakWidthPointsMinimum - 1

            ' Copy the desired segment of data from xValues to segmentX and yValues to segmentY
            For subIndex = startIndex To startIndex + peakWidthPointsMinimum - 1
                segmentX(subIndex - startIndex) = xValues(subIndex)
                segmentY(subIndex - startIndex) = yValues(subIndex)
            Next

            ' Compute the coefficients for the curve fit
            LeastSquaresFit(segmentX, segmentY, coefficients, POLYNOMIAL_ORDER)

            ' Compute the firstDerivative at the midpoint
            Dim midPointIndex = startIndex + peakWidthMidPoint
            firstDerivative(midPointIndex) = 2 * coefficients(2) * xValues(midPointIndex) + coefficients(1)
            secondDerivative(midPointIndex) = 2 * coefficients(2)

        Next

    End Sub

#Region "LinearLeastSquaresFitting"

    Private Function LeastSquaresFit(xValues As IList(Of Double), yValues As IList(Of Double), <Out> ByRef coefficients() As Double, polynomialOrder As Integer) As Boolean

        ' Code from article "Fit for Purpose" written by Steven Abbot
        ' and published in the February 2003 issue of Hardcore Visual Basic.
        ' Code excerpted from the VB6 program FitIt
        ' URL: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnhcvb03/html/hcvb03b1.asp

        Dim equationTerms() As udtLeastSquaresFitEquationTermType
'        Dim term As Integer
'        Dim success As Boolean

        ReDim equationTerms(polynomialOrder)
        ReDim coefficients(polynomialOrder)

        If xValues.Count < polynomialOrder + 1 Then
            ' Not enough data to fit a curve
            Return false
        End If

        ' Define equation for "ax^0 + bx^1 + cx^2", which is the same as "a + bx + cx^2"
        For term = 0 To polynomialOrder
            With equationTerms(term)
                .Coefficient = 1                        ' a, b, c in the above equation
                .Func = eTermFunctionConstants.X        ' X
                .Power = term                        ' Power that X is raised to
                .Inverse = False                        ' Whether or not to inverse the entire term

                .ParamResult = 0
            End With
        Next

        Dim success = LLSqFit(xValues, yValues, equationTerms)
        For term = 0 To polynomialOrder
            coefficients(term) = equationTerms(term).ParamResult
        Next

        Return success

    End Function

    Private Function LLSqFit(xValues As IList(Of Double), yValues As IList(Of Double), ByRef equationTerms() As udtLeastSquaresFitEquationTermType) As Boolean

        'Linear Least Squares Fit

        Dim i As Integer, j As Integer, k As Integer, L As Integer, m As Integer
        Dim ym As Double

        Dim Beta() As Double, CoVar(,) As Double, PFuncVal() As Double

        ReDim Beta(xValues.Count - 1)
        ReDim CoVar(equationTerms.Length - 1, equationTerms.Length - 1)
        ReDim PFuncVal(equationTerms.Length - 1)

        For i = 0 To xValues.Count - 1
            GetLVals(xValues(i), equationTerms, PFuncVal)
            ym = yValues(i)
            For L = 0 To equationTerms.Length - 1
                For m = 0 To L
                    CoVar(L, m) += PFuncVal(L) * PFuncVal(m)
                Next m
                Beta(L) += ym * PFuncVal(L)
            Next L
        Next i
        For j = 1 To equationTerms.Length - 1
            For k = 0 To j - 1
                CoVar(k, j) = CoVar(j, k)
            Next k
        Next j

        If GaussJordan(CoVar, equationTerms, Beta) Then
            For L = 0 To equationTerms.Length - 1
                equationTerms(L).ParamResult = Beta(L)
            Next L

            Return True
        Else
            ' Error fitting; clear coefficients
            For L = 0 To equationTerms.Length - 1
                equationTerms(L).ParamResult = 0
            Next L

            Return False
        End If

    End Function

    Private Sub GetLVals(X As Double, ByRef equationTerms() As udtLeastSquaresFitEquationTermType, ByRef PFuncVal() As Double)
        ' Get values for Linear Least Squares
        ' equationTerms() is a 0-based array defining the form of each term

        Dim i As Integer, v As Double

        ' Use the following for a 2nd order polynomial fit
        ''Define the formula via PFuncVal
        ''In this case NTerms=3 and y=a+bx+cx^2
        'PFuncVal(1) = 1
        'PFuncVal(2) = X
        'PFuncVal(3) = X ^ 2

        'f = "1,X,Log(X),Log10(X),Exp(X),Sin(X),Cos(X),Tan(X),ATAN(X)"
        For i = 0 To equationTerms.Length - 1
            With equationTerms(i)
                Select Case .Func
                    Case eTermFunctionConstants.One
                        v = 1
                    Case eTermFunctionConstants.X
                        v = X ^ .Power
                    Case eTermFunctionConstants.LogX
                        If .Coefficient * X <= 0 Then
                            v = 0
                        Else
                            v = Math.Log(.Coefficient * X) ^ .Power
                        End If
                    Case eTermFunctionConstants.Log10X
                        If .Coefficient * X <= 0 Then
                            v = 0
                        Else
                            v = Math.Log10(.Coefficient * X) ^ .Power
                        End If
                    Case eTermFunctionConstants.ExpX
                        v = Math.Exp(.Coefficient * X) ^ .Power
                    Case eTermFunctionConstants.SinX
                        v = Math.Sin(.Coefficient * X) ^ .Power
                    Case eTermFunctionConstants.CosX
                        v = Math.Cos(.Coefficient * X) ^ .Power
                    Case eTermFunctionConstants.TanX
                        v = Math.Tan(.Coefficient * X) ^ .Power
                    Case eTermFunctionConstants.ATanX
                        v = Math.Atan(.Coefficient * X) ^ .Power
                End Select

                If .Inverse Then
                    If Math.Abs(v) < Double.Epsilon Then
                        PFuncVal(i) = 0
                    Else 'NOT V...
                        PFuncVal(i) = 1 / v
                    End If
                Else 'INV(I) = FALSE
                    PFuncVal(i) = v
                End If

            End With
        Next i

    End Sub

    Private Function GaussJordan(ByRef A(,) As Double, ByRef equationTerms() As udtLeastSquaresFitEquationTermType, ByRef b() As Double) As Boolean

        ' GaussJordan elimination for LLSq and LM solving
        ' Returns True if success, False if an error

        Dim indxc() As Integer, indxr() As Integer, ipiv() As Integer
        Dim n As Integer

        n = equationTerms.Length

        ReDim indxc(n - 1)
        ReDim indxr(n - 1)
        ReDim ipiv(n - 1)

        Dim i As Integer, icol As Integer, irow As Integer, j As Integer, k As Integer, L As Integer, ll As Integer
        Dim Big As Double, Dum As Double, PivInv As Double

        Try
            For i = 0 To n - 1
                Big = 0
                For j = 0 To n - 1
                    If ipiv(j) <> 1 Then
                        For k = 0 To n - 1
                            If ipiv(k) = 0 Then
                                If Math.Abs(A(j, k)) >= Big Then
                                    Big = Math.Abs(A(j, k))
                                    irow = j
                                    icol = k
                                End If
                            End If
                        Next k
                    End If
                Next j

                ipiv(icol) += 1
                If irow <> icol Then
                    For L = 0 To n - 1
                        Dum = A(irow, L)
                        A(irow, L) = A(icol, L)
                        A(icol, L) = Dum
                    Next L
                    Dum = b(irow)
                    b(irow) = b(icol)
                    b(icol) = Dum
                End If

                indxr(i) = irow
                indxc(i) = icol
                If Math.Abs(A(icol, icol)) < Double.Epsilon Then
                    ' Error, the matrix was singular
                    Return False
                End If

                PivInv = 1 / A(icol, icol)
                A(icol, icol) = 1
                For L = 0 To n - 1
                    A(icol, L) *= PivInv
                Next L

                b(icol) *= PivInv
                For ll = 0 To n - 1
                    If ll <> icol Then
                        Dum = A(ll, icol)
                        A(ll, icol) = 0
                        For L = 0 To n - 1
                            A(ll, L) -= A(icol, L) * Dum
                        Next L
                        b(ll) -= b(icol) * Dum
                    End If
                Next ll
            Next i

            For L = n - 1 To 0 Step -1
                If indxr(L) <> indxc(L) Then
                    For k = 0 To n - 1
                        Dum = A(k, indxr(L))
                        A(k, indxr(L)) = A(k, indxc(L))
                        A(k, indxc(L)) = Dum
                    Next k
                End If
            Next L

        Catch ex As Exception
            Return False
        End Try

        Return True

    End Function
#End Region

#Region "EolsDll_LeastSquaresFitting"

    'Private Const EODOUBLE As Integer = 1
    'Private Const EOFLOAT As Integer = 2
    'Private Const EOLONG As Integer = 3
    'Private Const EOSHORT As Integer = 4

    'Private Declare Function EoLeastSquaresFit Lib "eols.dll" _
    '    (ByVal XData() As Double, _
    '    ByVal YData() As Double, _
    '    ByVal iNDataPoints As Integer, _
    '    ByVal iNCoefficients As Integer, _
    '    ByVal fnUserEquation As Integer, _
    '    ByVal Coef() As Double, _
    '    ByVal iDataType As Integer, _
    '    ByVal iSaveStateFlag As Integer, _
    '    ByRef handle As Integer) As Integer

    ''The following is only needed when using eolsrt.dll; used for real-time least squares fitting, utilizing data buffering
    ''Private Declare Sub EoLeastSquaresFitClose Lib "eolsrt.dll" (ByRef handle As Long)

    'Private Sub LeastSquaresFitEolsDll(ByVal xValues() As Double, ByVal yValues() As Double, ByRef coefficients() As Double, ByVal polynomialOrder As Integer)
    '    ' Uses the EoLeastSquaresFit function in the eols.dll file to compute a least squares fit on the portion of the data between indexStart and indexEnd
    '    ' polynomialOrder should be between 2 and 9
    '    ' xValues() should range from 0 to dataCount-1

    '    Dim returnCode As Integer

    '    Try
    '        ReDim coefficients(polynomialOrder)

    '        ' Note: For a 2nd order equation, coefficients(0), (1), and (2) correspond to C0, C1, and C2 in the equation:
    '        '       y = C0 +  C1 x  +  C2 x^2
    '        returnCode = EoLeastSquaresFit(xValues, yValues, xValues.Length, polynomialOrder + 1, 0, coefficients, EODOUBLE, 0, 0)
    '        Debug.Assert(returnCode = 1, "Call to EoLeastSquaresFit failed (clsPeakDetection->LeastSquaresFitEolsDll)")

    '    Catch ex As Exception
    '        If Err.Number = 52 Or Err.Number = 53 Then
    '            If Not mEolsDllNotFound Then
    '                mEolsDllNotFound = True

    '                ' Inform the user that we couldn't find eols.dll
    '                MsgBox("Could not find the eols.dll file; please assure it is located in the application folder.")
    '            End If
    '        Else
    '            Debug.Assert(False, "Error in clsPeakDetection->LeastSquaresFitEolsDll: " & ex.Message)
    '        End If

    '    End Try

    'End Sub

#End Region


End Class
