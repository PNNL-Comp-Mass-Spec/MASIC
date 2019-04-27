Option Explicit On
Option Strict On

Imports System.Runtime.InteropServices
Imports PRISM

''' <summary>
''' This class can be used to correlate two lists of numbers (typically mass spectra) to determine their similarity
''' The lists of numbers must have the same number of values
''' Use the BinData function to bin a list of X,Y pairs into bins ranging from .BinStartX to .BinEndX
'''
''' These functions were originally written in VB6 and required the use of a C DLL
''' They have since been ported to VB.NET
''' </summary>
Public Class clsCorrelation
    Inherits EventNotifier

#Region "Classwide variables"

    ''' <summary>
    ''' Coefficients used by GammaLn
    ''' </summary>
    Private ReadOnly mCoefficients As Double()

#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        Me.New(GetDefaultBinningOptions())
    End Sub

    Public Sub New(binningOptions As clsBinningOptions)
        mBinningOptions = binningOptions
        mNoiseThresholdIntensity = 0

        mCoefficients = New Double() {76.180091729471457, -86.505320329416776,
                                      24.014098240830911, -1.231739572450155,
                                      0.001208650973866179, -0.000005395239384953}

    End Sub

#Region "Constants and Structures"
    Private Const MIN_NON_ZERO_ION_COUNT As Integer = 5

    Public Enum cmCorrelationMethodConstants
        Pearson = 0
        Spearman = 1
        Kendall = 2
    End Enum

#End Region

#Region "Local Member Variables"
    Private mBinningOptions As clsBinningOptions
    Private mNoiseThresholdIntensity As Single
#End Region

#Region "Property Interface Functions"

    Public Property BinStartX As Single
        Get
            Return mBinningOptions.StartX
        End Get
        Set
            mBinningOptions.StartX = Value
        End Set
    End Property

    Public Property BinEndX As Single
        Get
            Return mBinningOptions.EndX
        End Get
        Set
            mBinningOptions.EndX = Value
        End Set
    End Property

    Public Property BinSize As Single
        Get
            Return mBinningOptions.BinSize
        End Get
        Set
            If Value <= 0 Then Value = 1
            mBinningOptions.BinSize = Value
        End Set
    End Property

    Public Property BinnedDataIntensityPrecisionPercent As Single
        Get
            Return mBinningOptions.IntensityPrecisionPercent
        End Get
        Set
            If Value < 0 Or Value > 100 Then Value = 1
            mBinningOptions.IntensityPrecisionPercent = Value
        End Set
    End Property

    Public Property NoiseThresholdIntensity As Single
        Get
            Return mNoiseThresholdIntensity
        End Get
        Set
            mNoiseThresholdIntensity = Value
        End Set
    End Property

    Public Property NormalizeBinnedData As Boolean
        Get
            Return mBinningOptions.Normalize
        End Get
        Set
            mBinningOptions.Normalize = Value
        End Set
    End Property

    Public Property SumAllIntensitiesForBin As Boolean
        Get
            Return mBinningOptions.SumAllIntensitiesForBin
        End Get
        Set
            mBinningOptions.SumAllIntensitiesForBin = Value
        End Set
    End Property

    Public Property MaximumBinCount As Integer
        Get
            Return mBinningOptions.MaximumBinCount
        End Get
        Set
            If Value < 2 Then Value = 10
            If Value > 1000000 Then Value = 1000000
            mBinningOptions.MaximumBinCount = Value
        End Set
    End Property
#End Region

    Private Function BetaCF(a As Double, b As Double, x As Double) As Double

        Dim MAXIT = 100
        Dim EPS = 0.0000003
        Dim FPMIN = 1.0E-30

        Dim m, m2 As Integer
        Dim aa, c, d, del, h, qab, qam, qap As Double

        qab = a + b
        qap = a + 1.0
        qam = a - 1.0
        c = 1.0
        d = 1.0 - qab * x / qap
        If (Math.Abs(d) < FPMIN) Then d = FPMIN
        d = 1.0 / d
        h = d
        For m = 1 To MAXIT
            m2 = 2 * m
            aa = m * (b - m) * x / ((qam + m2) * (a + m2))
            d = 1.0 + aa * d
            If (Math.Abs(d) < FPMIN) Then d = FPMIN
            c = 1.0 + aa / c
            If (Math.Abs(c) < FPMIN) Then c = FPMIN
            d = 1.0 / d
            h *= d * c
            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2))
            d = 1.0 + aa * d
            If (Math.Abs(d) < FPMIN) Then d = FPMIN
            c = 1.0 + aa / c
            If (Math.Abs(c) < FPMIN) Then c = FPMIN
            d = 1.0 / d
            del = d * c
            h *= del
            If (Math.Abs(del - 1.0) < EPS) Then Exit For
        Next m

        If (m > MAXIT) Then
            Throw New Exception("a or b too big, or MAXIT too small in clsCorrelation->BetaCF")
            Return 0
        Else
            Return h
        End If

    End Function

    Private Function BetaI(a As Double, b As Double, x As Double) As Double

        Dim bt As Double

        If (x < 0.0 Or x > 1.0) Then
            Throw New Exception("Bad x in routine clsCorrelation->BetaI; should be between 0 and 1")
        Else

            If (Math.Abs(x) < Double.Epsilon Or Math.Abs(x - 1.0) < Double.Epsilon) Then
                bt = 0.0
            Else
                bt = Math.Exp(GammaLn(a + b) - GammaLn(a) - GammaLn(b) + a * Math.Log(x) + b * Math.Log(1.0 - x))
            End If

            If (x < (a + 1.0) / (a + b + 2.0)) Then
                Return bt * BetaCF(a, b, x) / a
            Else
                Return 1.0 - bt * BetaCF(b, a, 1.0 - x) / b
            End If
        End If

    End Function

    Public Function BinData(
      xData() As Single,
      yData() As Single,
      <Out> ByRef binnedYData() As Single,
      <Out> ByRef binnedOffsetYData() As Single,
      <Out> ByRef binCount As Integer) As Boolean

        ' Bins the data in xData() according to startBinXValue and binSize
        ' Returns the binned data in binnedYData() and binnedOffsetYData()
        ' The difference between the two is that the StartX value is offset by 50% of the bin size when populating binnedOffsetYData()
        '
        ' Returns True if successful, false otherwise

        Dim dataCount As Integer

        Dim bin2Offset As Single

        Try
            dataCount = xData.Length
            If dataCount <= 0 Then
                ReDim binnedYData(-1)
                ReDim binnedOffsetYData(-1)
                binCount = 0
                Return False
            End If

            With mBinningOptions
                If .BinSize <= 0 Then .BinSize = 1
                If .StartX >= .EndX Then .EndX = .StartX + .BinSize * 10

                binCount = CInt((.EndX - .StartX) / .BinSize - 1)
                If binCount < 1 Then binCount = 1

                If binCount > .MaximumBinCount Then
                    .BinSize = (.EndX - .StartX) / .MaximumBinCount
                    binCount = CInt((.EndX - .StartX) / .BinSize - 1)
                End If
                bin2Offset = .BinSize / 2

                ReDim binnedYData(binCount - 1)
                ReDim binnedOffsetYData(binCount - 1)

            End With

            ' Fill binnedYData()
            BinDataWork(xData, yData, dataCount, binnedYData, binCount, mBinningOptions, 0)

            ' Fill binnedOffsetYData(), using a StartX of startBinXValue + bin2Offset
            BinDataWork(xData, yData, dataCount, binnedOffsetYData, binCount, mBinningOptions, bin2Offset)

        Catch ex As Exception
            OnErrorEvent("BinData: " & ex.Message, ex)
            ReDim binnedYData(-1)
            ReDim binnedOffsetYData(-1)
            binCount = 0
            Return False
        End Try

        Return True

    End Function

    Private Sub BinDataWork(xData As IList(Of Single), yData As IList(Of Single), dataCount As Integer,
                            binnedYData As IList(Of Single), binCount As Integer, binningOptions As clsBinningOptions, offset As Single)

        Dim index As Integer
        Dim binNumber As Integer

        Dim maximumIntensity As Single
        Dim intensityQuantizationValue As Single

        Try

            maximumIntensity = Single.MinValue
            For index = 0 To dataCount - 1
                If yData(index) >= mNoiseThresholdIntensity Then
                    binNumber = ValueToBinNumber(xData(index), binningOptions.StartX + offset, binningOptions.BinSize)
                    If binNumber >= 0 And binNumber < binCount Then
                        If binningOptions.SumAllIntensitiesForBin Then
                            ' Add this ion's intensity to the bin intensity
                            binnedYData(binNumber) += yData(index)
                        Else
                            ' Only change the bin's intensity if this ion's intensity is larger than the bin's intensity
                            ' If it is, then set the bin intensity to equal the ion's intensity
                            If yData(index) > binnedYData(binNumber) Then
                                binnedYData(binNumber) = yData(index)
                            End If
                        End If
                        If binnedYData(binNumber) > maximumIntensity Then
                            maximumIntensity = binnedYData(binNumber)
                        End If
                    Else
                        ' Bin is Out of range; ignore the value
                    End If
                End If
            Next

            If Not maximumIntensity > Single.MinValue Then maximumIntensity = 0

            If binningOptions.IntensityPrecisionPercent > 0 Then
                ' Quantize the intensities to .IntensityPrecisionPercent of maximumIntensity
                intensityQuantizationValue = binningOptions.IntensityPrecisionPercent / 100 * maximumIntensity
                If intensityQuantizationValue <= 0 Then intensityQuantizationValue = 1
                If intensityQuantizationValue > 1 Then intensityQuantizationValue = CSng(Math.Round(intensityQuantizationValue, 0))

                For index = 0 To binCount - 1
                    If Math.Abs(binnedYData(index)) > Single.Epsilon Then
                        binnedYData(index) = CSng(Math.Round(binnedYData(index) / intensityQuantizationValue, 0)) * intensityQuantizationValue
                    End If
                Next

            End If

            If binningOptions.Normalize And maximumIntensity > 0 Then
                For index = 0 To binCount - 1
                    If Math.Abs(binnedYData(index)) > Single.Epsilon Then
                        binnedYData(index) /= maximumIntensity * 100
                    End If
                Next
            End If

        Catch ex As Exception
            OnErrorEvent("BinDataWork: " & ex.Message, ex)
        End Try

    End Sub

    Public Function Correlate(dataList1() As Single, dataList2() As Single, eCorrelationMethod As cmCorrelationMethodConstants) As Single
        ' Finds the correlation value between the two lists of data
        ' The lists must have the same number of data points
        ' If they have fewer than MIN_NON_ZERO_ION_COUNT non-zero values, then the correlation value returned will be 0
        '
        ' Returns correlation value (0 to 1)
        ' If an error, returns -1
        '
        ' Note: If necessary, use the BinData function before calling this function to bin the data

        Dim index As Integer
        Dim dataCount As Integer
        Dim nonZeroDataCount As Integer

        Dim RValue As Single, ProbOfSignificance As Single, FishersZ As Single
        Dim DiffInRanks As Single, ZD As Single, RS As Single, ProbRS As Single
        Dim KendallsTau As Single, Z As Single

        ''        Dim dataList1Test() As Single = New Single() {1, 2, 2, 8, 9, 0, 0, 3, 9, 0, 5, 6}
        ''        Dim dataList2Test() As Single = New Single() {2, 3, 7, 7, 11, 1, 3, 2, 13, 0, 4, 10}

        Try

            RValue = 0
            RS = 0
            KendallsTau = 0

            dataCount = dataList1.Length
            If dataList2.Length <> dataList1.Length Then
                Return -1
            End If

            ' Determine the number of non-zero data points in the two spectra
            nonZeroDataCount = 0
            For index = 0 To dataCount - 1
                If dataList1(index) > 0 Then nonZeroDataCount += 1
            Next
            If nonZeroDataCount < MIN_NON_ZERO_ION_COUNT Then Return 0

            nonZeroDataCount = 0
            For index = 0 To dataCount - 1
                If dataList2(index) > 0 Then nonZeroDataCount += 1
            Next
            If nonZeroDataCount < MIN_NON_ZERO_ION_COUNT Then Return 0


            Select Case eCorrelationMethod
                Case cmCorrelationMethodConstants.Pearson
                    CorrelatePearson(dataList1, dataList2, RValue, ProbOfSignificance, FishersZ)
                    Return RValue
                Case cmCorrelationMethodConstants.Spearman
                    CorrelateSpearman(dataList1, dataList2, DiffInRanks, ZD, ProbOfSignificance, RS, ProbRS)
                    Return RS
                Case cmCorrelationMethodConstants.Kendall
                    CorrelateKendall(dataList1, dataList2, KendallsTau, Z, ProbOfSignificance)
                    Return KendallsTau
                Case Else
                    Return -1
            End Select

        Catch ex As Exception
            OnErrorEvent("Correlate: " & ex.Message, ex)
            Return -1
        End Try

    End Function

    Private Sub CorrelatePearson(
      dataList1 As IList(Of Single), dataList2 As IList(Of Single),
      <Out> ByRef RValue As Single,
      <Out> ByRef ProbOfSignificance As Single,
      <Out> ByRef FishersZ As Single)

        ' Performs a Pearson correlation (aka linear correlation) of the two lists
        ' The lists must have the same number of data points in each and should be 0-based arrays
        '
        ' Code from Numerical Recipes in C

        '  TINY is used to "regularize" the unusual case of complete correlation
        Dim TINY = 1.0E-20

        ' Given two arrays x[1..n] and y[1..n], this routine computes their correlation coefficient
        ' r (returned as r), the significance level at which the null hypothesis of zero correlation is
        ' disproved (prob whose small value indicates a significant correlation), and Fisher's z (returned
        ' as z), whose value can be used in further statistical tests as described above.

        Dim n As Integer
        Dim j As Integer
        Dim yt, xt, t, df As Double
        Dim syy = 0.0
        Dim sxy = 0.0
        Dim sxx = 0.0
        Dim ay = 0.0
        Dim ax = 0.0

        RValue = 0
        ProbOfSignificance = 0
        FishersZ = 0

        n = dataList1.Count
        If n <> dataList2.Count Then
            Throw New Exception("dataList1 and dataList2 must be arrays of the same length")
            n = 0
        End If
        If n <= 0 Then Exit Sub

        ' Find the means
        For j = 0 To n - 1
            ax += dataList1(j)
            ay += dataList2(j)
        Next j
        ax /= n
        ay /= n

        ' Compute the correlation coefficient
        For j = 0 To n - 1
            xt = dataList1(j) - ax
            yt = dataList2(j) - ay
            sxx += xt * xt
            syy += yt * yt
            sxy += xt * yt
        Next j

        RValue = CSng(sxy / (Math.Sqrt(sxx * syy) + TINY))

        ' Fisher's z transformation
        FishersZ = CSng(0.5 * Math.Log((1.0 + RValue + TINY) / (1.0 - RValue + TINY)))
        df = n - 2

        t = RValue * Math.Sqrt(df / ((1.0 - RValue + TINY) * (1.0 + RValue + TINY)))

        ' Student's t probability
        ProbOfSignificance = CSng(BetaI(0.5 * df, 0.5, df / (df + t * t)))

    End Sub

    Private Sub CorrelateKendall(
      dataList1 As IList(Of Single),
      dataList2 As IList(Of Single),
      <Out> ByRef KendallsTau As Single,
      <Out> ByRef Z As Single,
      <Out> ByRef ProbOfSignificance As Single)

        ' Performs a Kendall correlation (aka linear correlation) of the two lists
        ' The lists must have the same number of data points in each and should be 0-based arrays
        '
        ' Code from Numerical Recipes in C

        ' Given data arrays data1[1..n] and data2[1..n], this program returns Kendall's tau as tau,
        ' its number of standard deviations from zero as z, and its two-sided significance level as prob.
        ' Small values of prob indicate a significant correlation (tau positive) or anti correlation (tau
        ' negative).

        Dim n As Integer
        Dim n2 As Long = 0
        Dim n1 As Long = 0
        Dim k, j As Integer
        Dim intIS = 0

        Dim svar, aa, a2, a1 As Double

        KendallsTau = 0
        Z = 0
        ProbOfSignificance = 0

        n = dataList1.Count
        If n <> dataList2.Count Then
            Throw New Exception("dataList1 and dataList2 must be arrays of the same length")
            n = 0
        End If
        If n <= 0 Then Exit Sub

        For j = 0 To n - 2
            For k = j + 1 To n - 1
                a1 = dataList1(j) - dataList1(k)
                a2 = dataList2(j) - dataList2(k)
                aa = a1 * a2
                If Math.Abs(aa) > Double.Epsilon Then
                    n1 += 1
                    n2 += 1
                    If aa > 0 Then
                        intIS += 1
                    Else
                        intIS -= 1
                    End If
                Else
                    If Math.Abs(a1) > Double.Epsilon Then n1 += 1
                    If Math.Abs(a2) > Double.Epsilon Then n2 += 1
                End If
            Next k
        Next j

        KendallsTau = CSng(intIS / (Math.Sqrt(n1) * Math.Sqrt(n2)))

        svar = (4.0 * n + 10.0) / (9.0 * n * (n - 1.0))
        Z = CSng(KendallsTau / Math.Sqrt(svar))
        ProbOfSignificance = CSng(ErfCC(Math.Abs(Z) / 1.4142136))

    End Sub

    Private Sub CorrelateSpearman(
      dataList1() As Single,
      dataList2() As Single,
      <Out> ByRef DiffInRanks As Single,
      <Out> ByRef ZD As Single,
      <Out> ByRef ProbOfSignificance As Single,
      <Out> ByRef RS As Single,
      <Out> ByRef ProbRS As Single)

        ' Performs a Spearman correlation of the two lists
        ' The lists must have the same number of data points in each and should be 0-based arrays
        '
        ' Code from Numerical Recipes in C

        ' Note: dataList1 and dataList2 are re-ordered by this function; thus, they are passed ByVal

        ' Given two data arrays, data1[0..n-1] and data2[0..n-1], this routine returns their sum-squared
        ' difference of ranks as D, the number of standard deviations by which D deviates from its null hypothesis
        ' expected value as zd, the two-sided significance level of this deviation as probd,
        ' Spearman's rank correlation rs as rs, and the two-sided significance level of its deviation from
        ' zero as probrs. The external routine CRank is used.  A small value of either probd or probrs indicates
        ' a significant correlation (rs positive) or anti correlation (rs negative).

        Dim n As Integer
        Dim j As Integer

        Dim sg, sf As Single
        Dim vard, t, fac, en3n, en, df, AvgD As Double
        Dim DiffInRanksWork As Double

        DiffInRanks = 0
        ZD = 0
        ProbOfSignificance = 0
        RS = 0
        ProbRS = 0

        n = dataList1.Length
        If n <> dataList2.Length Then
            Throw New Exception("dataList1 and dataList2 must be arrays of the same length")
            n = 0
        End If
        If n <= 0 Then Exit Sub

        ' Sort dataList1, sorting dataList2 parallel to it
        Array.Sort(dataList1, dataList2)
        CRank(n, dataList1, sf)

        ' Sort dataList2, sorting dataList1 parallel to it
        Array.Sort(dataList2, dataList1)
        CRank(n, dataList2, sg)

        DiffInRanksWork = 0.0
        For j = 0 To n - 1
            DiffInRanksWork += SquareNum(dataList1(j) - dataList2(j))
        Next j
        DiffInRanks = CSng(DiffInRanksWork)

        en = n

        en3n = en * en * en - en
        AvgD = en3n / 6.0 - (sf + sg) / 12.0
        fac = (1.0 - sf / en3n) * (1.0 - sg / en3n)
        vard = ((en - 1.0) * en * en * SquareNum(en + 1.0) / 36.0) * fac
        ZD = CSng((DiffInRanks - AvgD) / Math.Sqrt(vard))

        ProbOfSignificance = CSng(ErfCC(Math.Abs(ZD) / 1.4142136))
        RS = CSng((1.0 - (6.0 / en3n) * (DiffInRanks + (sf + sg) / 12.0)) / Math.Sqrt(fac))

        fac = (RS + 1.0) * (1.0 - RS)

        If (fac > 0.0) Then
            t = RS * Math.Sqrt((en - 2.0) / fac)
            df = en - 2.0
            ProbRS = CSng(BetaI(0.5 * df, 0.5, df / (df + t * t)))
        Else
            ProbRS = 0.0
        End If

    End Sub

    Private Sub CRank(n As Integer, w As IList(Of Single), <Out> ByRef s As Single)

        ' Given a zero-based sorted array w(0..n-1), replaces the elements by their rank (1 .. n), including mid-ranking of ties,
        ' and returns as s the sum of f^3 - f, where f is the number of elements in each tie.

        Dim j As Integer
        Dim ji, jt As Integer
        Dim t, rank As Single

        s = 0
        j = 0
        Do While j < n - 1
            If Math.Abs(w(j + 1) - w(j)) > Single.Epsilon Then
                w(j) = j + 1            ' Rank = j + 1
                j += 1
            Else
                jt = j + 1
                Do While jt < n AndAlso Math.Abs(w(jt) - w(j)) < Single.Epsilon
                    jt += 1
                Loop
                rank = 0.5! * (j + jt - 1) + 1

                For ji = j To jt - 1
                    w(ji) = rank
                Next ji

                t = jt - j
                s += t * t * t - t          ' t^3 - t
                j = jt
            End If
        Loop

        If j = n - 1 Then
            w(n - 1) = n
        End If

    End Sub

    Private Function ErfCC(x As Double) As Double

        Dim t, z, ans As Double

        z = Math.Abs(x)
        t = 1.0 / (1.0 + 0.5 * z)

        ans = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                             t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                             t * (-0.82215223 + t * 0.17087277)))))))))

        If x >= 0.0 Then
            Return ans
        Else
            Return 2.0 - ans
        End If

    End Function

    ''' <summary>
    ''' Computes the natural logarithm of the Gamma Function
    ''' </summary>
    ''' <param name="xx"></param>
    ''' <returns></returns>
    Private Function GammaLn(xx As Double) As Double
        Dim x, y, tmp, ser As Double
        Dim j As Integer

        x = xx
        y = x

        tmp = x + 5.5
        tmp -= (x + 0.5) * Math.Log(tmp)
        ser = 1.0000000001900149

        For j = 0 To 5
            y += 1
            ser += mCoefficients(j) / y
        Next j

        Return -tmp + Math.Log(2.5066282746310007 * ser / x)

    End Function

    Public Shared Function GetDefaultBinningOptions() As clsBinningOptions
        Dim binningOptions = New clsBinningOptions()

        With binningOptions
            .StartX = 50
            .EndX = 2000
            .BinSize = 1
            .IntensityPrecisionPercent = 1
            .Normalize = False
            .SumAllIntensitiesForBin = True                     ' Sum all of the intensities for binned ions of the same bin together
            .MaximumBinCount = 100000
        End With

        Return binningOptions
    End Function

    <Obsolete("Use GetDefaultBinningOptions, which returns an instance of clsBinningOptions")>
    Public Shared Sub InitializeBinningOptions(<Out> ByRef binningOptions As clsBinningOptions)
        binningOptions = GetDefaultBinningOptions()
    End Sub

    Public Sub SetBinningOptions(binningOptions As clsBinningOptions)
        mBinningOptions = binningOptions
    End Sub

    Private Function SquareNum(value As Double) As Double

        If Math.Abs(value) < Double.Epsilon Then
            Return 0
        Else
            Return value * value
        End If

    End Function

    Private Function ValueToBinNumber(thisValue As Single, startValue As Single, histogramBinSize As Single) As Integer

        ' First subtract StartValue from ThisValue
        ' For example, if StartValue is 500 and ThisValue is 500.28, then WorkingValue = 0.28
        ' Or, if StartValue is 500 and ThisValue is 530.83, then WorkingValue = 30.83
        Dim workingValue = thisValue - startValue

        ' Now, dividing WorkingValue by BinSize and rounding to nearest integer
        '  actually gives the bin
        ' For example, given WorkingValue = 0.28 and BinSize = 0.1, Bin = CInt(Round(2.8,0)) = 3
        ' Or, given WorkingValue = 30.83 and BinSize = 0.1, Bin = CInt(Round(308.3,0)) = 308
        ValueToBinNumber = CInt(Math.Round(WorkingValue / histogramBinSize, 0))

    End Function

End Class
