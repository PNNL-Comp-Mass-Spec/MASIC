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

    ' Last modified March 21, 2005

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

    Public Function ComputeSlope(dblXValsZeroBased() As Double, dblYValsZeroBased() As Double, intStartIndex As Integer, intEndIndex As Integer) As Double

        Const POLYNOMIAL_ORDER As Integer = 1

        Dim intSegmentCount As Integer
        Dim intIndex As Integer

        Dim dblSegmentX() As Double
        Dim dblSegmentY() As Double

        Dim dblCoefficients() As Double = Nothing

        If dblXValsZeroBased Is Nothing OrElse dblXValsZeroBased.Length = 0 Then Return 0

        intSegmentCount = intEndIndex - intStartIndex + 1

        ReDim dblSegmentX(intSegmentCount - 1)
        ReDim dblSegmentY(intSegmentCount - 1)

        ' Copy the desired segment of data from dblXVals to dblSegmentX and dblYVals to dblSegmentY
        For intIndex = intStartIndex To intEndIndex
            dblSegmentX(intIndex - intStartIndex) = dblXValsZeroBased(intIndex)
            dblSegmentY(intIndex - intStartIndex) = dblYValsZeroBased(intIndex)
        Next intIndex

        ' Compute the coefficients for the curve fit
        LeastSquaresFit(dblSegmentX, dblSegmentY, dblCoefficients, POLYNOMIAL_ORDER)

        Return dblCoefficients(1)

    End Function

    Public Function DetectPeaks(
      dblXValsZeroBased() As Double,
      dblYValsZeroBased() As Double,
      dblIntensityThresholdAbsoluteMinimum As Double,
      intPeakWidthPointsMinimum As Integer,
      Optional intPeakDetectIntensityThresholdPercentageOfMaximum As Integer = 0,
      Optional intPeakWidthInSigma As Integer = 4,
      Optional blnUseValleysForPeakWidth As Boolean = True,
      Optional blnMovePeakLocationToMaxIntensity As Boolean = True) As List(Of clsPeakInfo)

        ' Finds peaks in the parallel arrays dblXValsZeroBased() and dblYValsZeroBased()
        ' dblIntensityThreshold is the minimum absolute intensity allowable for a peak
        ' intPeakDetectIntensityThresholdPercentageOfMaximum allows one to specify a minimum intensity as a percentage of the maximum peak intensity
        ' Note that the maximum value of dblIntensityThreshold vs. MaxValue*intPeakDetectIntensityThresholdPercentageOfMaximum is used as the minimum
        ' For example, if dblIntensityThreshold = 10 and intPeakDetectIntensityThresholdPercentageOfMaximum =  5 (indicating 5%),
        '   then if the maximum of dblYValsZeroBased() is 50, then the minimum intensity of identified peaks is 10, and not 2.5
        '   However, if the maximum of dblYValsZeroBased() is 500, then the minimum intensity of identified peaks is 50, and not 10

        ' Returns the locations of the peaks in intPeakLocations() -- indices of the peak apexes in the source arrays
        ' Returns the left edges of the peaks (in points, not actual units) in intPeakEdgesLeft()       -- These values could be negative if blnUseValleysForPeakWidth = False
        ' Returns the right edges of the peaks in intPeakEdgesRight()                                   -- These values could be larger than intSourceDataCount-1 if blnUseValleysForPeakWidth = False
        ' Returns the areas of the peaks in dblPeakAreas()

        ' Note: Compute peak width using: intPeakWidthPoints = newPeak.RightEdge - newPeak.LeftEdge + 1

        ' The function returns the number of peaks found; if none are found, returns 0

        ' Uses the Magnitude-Concavity method, wherein a second order
        '   polynomial is fit to the points within the window, giving a_2*x^2 + a_1*x + a_0
        '   Given this, a_1 is the first derivative and a_2 is the second derivative
        ' From this, the first derivative gives the index of the peak apex
        ' The standard deviation (s) can be found using:
        '   s = sqrt(-h(t_r) / h''(t_r))
        '  where h(t_r) is the height of the peak at the peak center
        '  and h''(t_r) is the height of the second derivative of the peak
        ' In chromatography, the baseline peak intWidthInPoints = 4*dblSigma

        Dim intIndex, intIndexLast, intIndexFirst As Integer
        Dim intCompareIndex As Integer
        Dim intPeakHalfWidth As Integer

        Dim intLowIntensityPointCount As Integer

        Dim intSourceDataCount As Integer
        Dim intDataIndexCheck, intDataIndexCheckStart, intDataIndexCheckEnd As Integer

        Dim dblMaximumIntensity As Double, dblIntensityThreshold As Double
        Dim dblSigma As Double, intWidthInPoints As Integer
        Dim dblFirstDerivative() As Double, dblSecondDerivative() As Double

        Dim dblXValsForArea() As Double, dblYValsForArea() As Double
        Dim intThisPeakWidthInPoints As Integer
        Dim intThisPeakStartIndex As Integer, intThisPeakEndIndex As Integer
        Dim intAreaValsCopyIndex As Integer

        ' Initialize the list of detected peaks
        Dim detectedPeaks = New List(Of clsPeakInfo)

        Try


            intSourceDataCount = dblXValsZeroBased.Length
            If intSourceDataCount <= 0 Then Return detectedPeaks

            ' Reserve space for the first and second derivatives
            ReDim dblFirstDerivative(intSourceDataCount - 1)
            ReDim dblSecondDerivative(intSourceDataCount - 1)

            ' The mid point width is the minimum width divided by 2, rounded down
            intPeakHalfWidth = CInt(Math.Floor(intPeakWidthPointsMinimum / 2))

            ' Find the maximum intensity in the source data
            dblMaximumIntensity = 0
            For intIndex = 0 To intSourceDataCount - 1
                If dblYValsZeroBased(intIndex) > dblMaximumIntensity Then
                    dblMaximumIntensity = dblYValsZeroBased(intIndex)
                End If
            Next intIndex

            dblIntensityThreshold = dblMaximumIntensity * (intPeakDetectIntensityThresholdPercentageOfMaximum / 100.0)
            If dblIntensityThreshold < dblIntensityThresholdAbsoluteMinimum Then
                dblIntensityThreshold = dblIntensityThresholdAbsoluteMinimum
            End If

            ' Exit the function if none of the data is above the minimum threshold
            If dblMaximumIntensity < dblIntensityThreshold Then Return detectedPeaks

            ' Do the actual work
            FitSegments(dblXValsZeroBased, dblYValsZeroBased, intSourceDataCount, intPeakWidthPointsMinimum, intPeakHalfWidth, dblFirstDerivative, dblSecondDerivative)

            If intPeakWidthInSigma < 1 Then intPeakWidthInSigma = 1

            ' Examine the First Derivative function and look for zero crossings (in the downward direction)
            ' If looking for valleys, would look for zero crossings in the upward direction
            ' Only significant if intensity of point is above threshold
            If intPeakWidthPointsMinimum <= 0 Then intPeakWidthPointsMinimum = 1

            ' We'll start looking for peaks halfway into intPeakWidthPointsMinimum
            intIndexFirst = intPeakHalfWidth
            intIndexLast = intSourceDataCount - 1 - intPeakHalfWidth

            For intIndex = intIndexFirst To intIndexLast
                If dblFirstDerivative(intIndex) > 0 And dblFirstDerivative(intIndex + 1) < 0 Then
                    ' Possible peak
                    If dblYValsZeroBased(intIndex) >= dblIntensityThreshold Or dblYValsZeroBased(intIndex + 1) >= dblIntensityThreshold Then
                        ' Actual peak

                        Dim newPeak = New clsPeakInfo(intIndex)

                        If blnUseValleysForPeakWidth Then
                            ' Determine the peak width by looking for the adjacent valleys
                            ' If, while looking, we find intPeakWidthPointsMinimum / 2 points in a row with intensity values below dblIntensityThreshold, then
                            ' set the edge intPeakHalfWidth - 1 points closer to the peak maximum

                            If intIndex > 0 Then
                                newPeak.LeftEdge = 0
                                intLowIntensityPointCount = 0
                                For intCompareIndex = intIndex - 1 To 0 Step -1
                                    If dblFirstDerivative(intCompareIndex) <= 0 And dblFirstDerivative(intCompareIndex + 1) >= 0 Then
                                        ' Found a valley; this is the left edge
                                        newPeak.LeftEdge = intCompareIndex + 1
                                        Exit For
                                    ElseIf dblYValsZeroBased(intCompareIndex) < dblIntensityThreshold Then
                                        intLowIntensityPointCount += 1
                                        If intLowIntensityPointCount > intPeakHalfWidth Then
                                            newPeak.LeftEdge = intCompareIndex + (intPeakHalfWidth - 1)
                                            Exit For
                                        End If
                                    Else
                                        intLowIntensityPointCount = 0
                                    End If
                                Next intCompareIndex
                            Else
                                newPeak.LeftEdge = 0
                            End If

                            If intIndex < intSourceDataCount - 2 Then
                                newPeak.RightEdge = intSourceDataCount - 1
                                intLowIntensityPointCount = 0
                                For intCompareIndex = intIndex + 1 To intSourceDataCount - 2
                                    If dblFirstDerivative(intCompareIndex) <= 0 And dblFirstDerivative(intCompareIndex + 1) >= 0 Then
                                        ' Found a valley; this is the right edge
                                        newPeak.RightEdge = intCompareIndex
                                        Exit For
                                    ElseIf dblYValsZeroBased(intCompareIndex) < dblIntensityThreshold Then
                                        intLowIntensityPointCount += 1
                                        If intLowIntensityPointCount > intPeakHalfWidth Then
                                            newPeak.RightEdge = intCompareIndex - (intPeakHalfWidth - 1)
                                            Exit For
                                        End If
                                    Else
                                        intLowIntensityPointCount = 0
                                    End If
                                Next intCompareIndex
                            Else
                                newPeak.RightEdge = intSourceDataCount - 1
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

                            Try
                                ' If dblSecondDerivative(intIndex)) is tiny, the following division will fail
                                dblSigma = Math.Sqrt(Math.Abs(-dblYValsZeroBased(intIndex) / dblSecondDerivative(intIndex)))
                            Catch ex As Exception
                                dblSigma = 0
                            End Try
                            intWidthInPoints = CInt(Math.Ceiling(intPeakWidthInSigma * dblSigma))

                            If intWidthInPoints > 4 * intSourceDataCount Then
                                ' Predicted width is over 4 times the data count
                                ' Set it to be 4 times the data count
                                intWidthInPoints = intSourceDataCount * 4
                            End If

                            If intWidthInPoints < 2 Then intWidthInPoints = 2

                            ' If the peak width is odd, then center around intIndex
                            ' Otherwise, offset to the right of intIndex
                            If intWidthInPoints Mod 2 = 0 Then
                                ' Even number
                                newPeak.LeftEdge = intIndex - CInt(intWidthInPoints / 2)
                                newPeak.RightEdge = intIndex + CInt(intWidthInPoints / 2) - 1
                            Else
                                ' Odd number
                                newPeak.LeftEdge = intIndex - CInt((intWidthInPoints - 1) / 2)
                                newPeak.RightEdge = intIndex + CInt((intWidthInPoints - 1) / 2)
                            End If

                        End If

                        detectedPeaks.Add(newPeak)

                    End If
                End If
            Next intIndex


            ' Compute the peak areas
            For Each peakItem In detectedPeaks
                intThisPeakWidthInPoints = peakItem.RightEdge - peakItem.LeftEdge + 1

                If intThisPeakWidthInPoints > 0 Then
                    If intThisPeakWidthInPoints = 1 Then
                        ' I don't think this can happen
                        ' Just in case, we'll set the area equal to the peak intensity
                        peakItem.PeakArea = dblYValsZeroBased(peakItem.PeakLocation)
                    Else
                        ReDim dblXValsForArea(intThisPeakWidthInPoints - 1)
                        ReDim dblYValsForArea(intThisPeakWidthInPoints - 1)

                        intThisPeakStartIndex = peakItem.LeftEdge
                        intThisPeakEndIndex = peakItem.RightEdge

                        If intThisPeakStartIndex < 0 Then
                            ' This will happen if the width is too large, or if not all of the peak's data was included in the data arrays
                            intThisPeakStartIndex = 0
                        End If

                        If intThisPeakEndIndex >= intSourceDataCount Then
                            ' This will happen if the width is too large, or if not all of the peak's data was included in the data arrays
                            intThisPeakEndIndex = intSourceDataCount - 1
                        End If

                        For intAreaValsCopyIndex = intThisPeakStartIndex To intThisPeakEndIndex
                            dblXValsForArea(intAreaValsCopyIndex - intThisPeakStartIndex) = dblXValsZeroBased(intAreaValsCopyIndex)
                            dblYValsForArea(intAreaValsCopyIndex - intThisPeakStartIndex) = dblYValsZeroBased(intAreaValsCopyIndex)
                        Next intAreaValsCopyIndex

                        peakItem.PeakArea = FindArea(dblXValsForArea, dblYValsForArea, intThisPeakWidthInPoints)

                    End If
                Else
                    ' 0-width peak; this shouldn't happen
                    Console.WriteLine("Warning: 0-width peak; this shouldn't happen (clsPeakDetection->DetectPeaks)")
                    peakItem.PeakArea = 0
                End If
            Next

            If blnMovePeakLocationToMaxIntensity Then
                For Each peakItem In detectedPeaks
                    ' The peak finder often determines the peak center to be a few points away from the peak apex -- check for this
                    ' Define the maximum allowed peak apex shift to be 33% of intPeakWidthPointsMinimum
                    intDataIndexCheckStart = peakItem.PeakLocation - CInt(Math.Floor(intPeakWidthPointsMinimum / 3))
                    If intDataIndexCheckStart < 0 Then intDataIndexCheckStart = 0

                    intDataIndexCheckEnd = peakItem.PeakLocation + CInt(Math.Floor(intPeakWidthPointsMinimum / 3))
                    If intDataIndexCheckEnd > intSourceDataCount - 1 Then intDataIndexCheckEnd = intSourceDataCount - 1

                    dblMaximumIntensity = dblYValsZeroBased(peakItem.PeakLocation)
                    For intDataIndexCheck = intDataIndexCheckStart To intDataIndexCheckEnd
                        If dblYValsZeroBased(intDataIndexCheck) > dblMaximumIntensity Then
                            peakItem.PeakLocation = intDataIndexCheck
                            dblMaximumIntensity = dblYValsZeroBased(intDataIndexCheck)
                        End If
                    Next intDataIndexCheck

                    If peakItem.PeakLocation < peakItem.LeftEdge Then peakItem.LeftEdge = peakItem.PeakLocation
                    If peakItem.PeakLocation > peakItem.RightEdge Then peakItem.RightEdge = peakItem.PeakLocation
                Next
            End If

        Catch ex As Exception
            Console.WriteLine("Warning: Error in clsPeakDetection->DetectPeaks (or in a child function)" & vbCrLf & ex.Message)
        End Try

        Return detectedPeaks

    End Function

    Private Function FindArea(dblXVals() As Double, dblYVals() As Double, intArrayCount As Integer) As Double
        ' dblYVals() should be 0-based

        ' Finds the area under the curve, using trapezoidal integration

        Dim intIndex As Integer
        Dim dblArea As Double

        dblArea = 0
        For intIndex = 0 To intArrayCount - 2
            ' Area of a trapezoid (turned on its side) is:
            '   0.5 * d * (h1 + h2)
            ' where d is the distance between two points, and h1 and h2 are the intensities
            '   at the 2 points

            dblArea += 0.5 * Math.Abs(dblXVals(intIndex + 1) - dblXVals(intIndex)) * (dblYVals(intIndex) + dblYVals(intIndex + 1))
        Next intIndex

        Return dblArea

    End Function

    Private Sub FitSegments(dblXVals() As Double, dblYVals() As Double, intSourceDataCount As Integer, intPeakWidthPointsMinimum As Integer, intPeakWidthMidPoint As Integer, ByRef dblFirstDerivative() As Double, ByRef dblSecondDerivative() As Double)
        ' dblXVals() and dblYVals() are zero-based arrays

        Const POLYNOMIAL_ORDER As Integer = 2

        Dim dblSegmentX() As Double
        Dim dblSegmentY() As Double

        Dim dblCoefficients() As Double = Nothing

        Dim intSubIndex As Integer, intStartIndex As Integer
        Dim intMidPointIndex As Integer

        ' If POLYNOMIAL_ORDER < 2 Then POLYNOMIAL_ORDER = 2
        ' If POLYNOMIAL_ORDER > 9 Then POLYNOMIAL_ORDER = 9

        ReDim dblSegmentX(intPeakWidthPointsMinimum - 1)
        ReDim dblSegmentY(intPeakWidthPointsMinimum - 1)

        For intStartIndex = 0 To intSourceDataCount - intPeakWidthPointsMinimum - 1

            ' Copy the desired segment of data from dblXVals to dblSegmentX and dblYVals to dblSegmentY
            For intSubIndex = intStartIndex To intStartIndex + intPeakWidthPointsMinimum - 1
                dblSegmentX(intSubIndex - intStartIndex) = dblXVals(intSubIndex)
                dblSegmentY(intSubIndex - intStartIndex) = dblYVals(intSubIndex)
            Next intSubIndex

            ' Compute the coefficients for the curve fit
            LeastSquaresFit(dblSegmentX, dblSegmentY, dblCoefficients, POLYNOMIAL_ORDER)

            ' Compute the dblFirstDerivative at the midpoint
            intMidPointIndex = intStartIndex + intPeakWidthMidPoint
            dblFirstDerivative(intMidPointIndex) = 2 * dblCoefficients(2) * dblXVals(intMidPointIndex) + dblCoefficients(1)
            dblSecondDerivative(intMidPointIndex) = 2 * dblCoefficients(2)

        Next intStartIndex

    End Sub

#Region "LinearLeastSquaresFitting"

    Private Function LeastSquaresFit(dblXVals() As Double, dblYVals() As Double, <Out()> ByRef dblCoefficients() As Double, intPolynomialOrder As Integer) As Boolean

        ' Code from article "Fit for Purpose" written by Steven Abbot
        ' and published in the February 2003 issue of Hardcore Visual Basic.
        ' Code excerpted from the VB6 program FitIt
        ' URL: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnhcvb03/html/hcvb03b1.asp

        Dim udtEquationTerms() As udtLeastSquaresFitEquationTermType
        Dim intTerm As Integer
        Dim blnSuccess As Boolean

        ReDim udtEquationTerms(intPolynomialOrder)
        ReDim dblCoefficients(intPolynomialOrder)

        If dblXVals.Length < intPolynomialOrder + 1 Then
            ' Not enough data to fit a curve
            blnSuccess = False
        Else

            ' Define equation for "ax^0 + bx^1 + cx^2", which is the same as "a + bx + cx^2"
            For intTerm = 0 To intPolynomialOrder
                With udtEquationTerms(intTerm)
                    .Coefficient = 1                        ' a, b, c in the above equation
                    .Func = eTermFunctionConstants.X        ' X
                    .Power = intTerm                        ' Power that X is raised to
                    .Inverse = False                        ' Whether or not to inverse the entire term

                    .ParamResult = 0
                End With
            Next intTerm

            blnSuccess = LLSqFit(dblXVals, dblYVals, udtEquationTerms)
            For intTerm = 0 To intPolynomialOrder
                dblCoefficients(intTerm) = udtEquationTerms(intTerm).ParamResult
            Next intTerm
        End If

        Return blnSuccess

    End Function

    Private Function LLSqFit(DataX() As Double, DataY() As Double, ByRef udtEquationTerms() As udtLeastSquaresFitEquationTermType) As Boolean

        'Linear Least Squares Fit

        Dim i As Integer, j As Integer, k As Integer, L As Integer, m As Integer
        Dim ym As Double

        Dim Beta() As Double, CoVar(,) As Double, PFuncVal() As Double

        ReDim Beta(DataX.Length - 1)
        ReDim CoVar(udtEquationTerms.Length - 1, udtEquationTerms.Length - 1)
        ReDim PFuncVal(udtEquationTerms.Length - 1)

        For i = 0 To DataX.Length - 1
            GetLVals(DataX(i), udtEquationTerms, PFuncVal)
            ym = DataY(i)
            For L = 0 To udtEquationTerms.Length - 1
                For m = 0 To L
                    CoVar(L, m) += PFuncVal(L) * PFuncVal(m)
                Next m
                Beta(L) += ym * PFuncVal(L)
            Next L
        Next i
        For j = 1 To udtEquationTerms.Length - 1
            For k = 0 To j - 1
                CoVar(k, j) = CoVar(j, k)
            Next k
        Next j

        If GaussJordan(CoVar, udtEquationTerms, Beta) Then
            For L = 0 To udtEquationTerms.Length - 1
                udtEquationTerms(L).ParamResult = Beta(L)
            Next L

            Return True
        Else
            ' Error fitting; clear dblCoefficients
            For L = 0 To udtEquationTerms.Length - 1
                udtEquationTerms(L).ParamResult = 0
            Next L

            Return False
        End If

    End Function

    Private Sub GetLVals(X As Double, ByRef udtEquationTerms() As udtLeastSquaresFitEquationTermType, ByRef PFuncVal() As Double)
        ' Get values for Linear Least Squares
        ' udtEquationTerms() is a 0-based array defining the form of each term

        Dim i As Integer, v As Double

        ' Use the following for a 2nd order polynomial fit
        ''Define the formula via PFuncVal
        ''In this case NTerms=3 and y=a+bx+cx^2
        'PFuncVal(1) = 1
        'PFuncVal(2) = X
        'PFuncVal(3) = X ^ 2

        'f = "1,X,Log(X),Log10(X),Exp(X),Sin(X),Cos(X),Tan(X),ATAN(X)"
        For i = 0 To udtEquationTerms.Length - 1
            With udtEquationTerms(i)
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

    Private Function GaussJordan(ByRef A(,) As Double, ByRef udtEquationTerms() As udtLeastSquaresFitEquationTermType, ByRef b() As Double) As Boolean

        ' GaussJordan elimination for LLSq and LM solving
        ' Returns True if success, False if an error

        Dim indxc() As Integer, indxr() As Integer, ipiv() As Integer
        Dim n As Integer

        n = udtEquationTerms.Length

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

    'Private Sub LeastSquaresFitEolsDll(ByVal dblXVals() As Double, ByVal dblYVals() As Double, ByRef dblCoefficients() As Double, ByVal intPolynomialOrder As Integer)
    '    ' Uses the EoLeastSquaresFit function in the eols.dll file to compute a least squares fit on the portion of the data between intIndexStart and intIndexEnd
    '    ' intPolynomialOrder should be between 2 and 9
    '    ' dblXVals() should range from 0 to intDataCount-1

    '    Dim intReturnCode As Integer

    '    Try
    '        ReDim dblCoefficients(intPolynomialOrder)

    '        ' Note: For a 2nd order equation, dblCoefficients(0), (1), and (2) correspond to C0, C1, and C2 in the equation:
    '        '       y = C0 +  C1 x  +  C2 x^2
    '        intReturnCode = EoLeastSquaresFit(dblXVals, dblYVals, dblXVals.Length, intPolynomialOrder + 1, 0, dblCoefficients, EODOUBLE, 0, 0)
    '        Debug.Assert(intReturnCode = 1, "Call to EoLeastSquaresFit failed (clsPeakDetection->LeastSquaresFitEolsDll)")

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
