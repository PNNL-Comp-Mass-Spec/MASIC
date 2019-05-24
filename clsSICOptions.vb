Imports System.Runtime.InteropServices

Public Class clsSICOptions

#Region "Constants and Enums"

    Public Const DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA As Double = 5
    Public Const DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM As Double = 3

#End Region

#Region "Properties"

    ''' <summary>
    ''' Provided by the user at the command line or obtained from the database (if a connection string is defined)
    ''' 0 if unknown
    ''' </summary>
    Public Property DatasetNumber As Integer

    ''' <summary>
    ''' Defaults to 10 ppm
    ''' </summary>
    Public Property SICTolerance As Double

    ''' <summary>
    ''' When true, then SICTolerance is treated as a PPM value
    ''' </summary>
    Public Property SICToleranceIsPPM As Boolean

    Public Property SICToleranceDa As Double
        Get
            If SICToleranceIsPPM Then
                ' Return the Da tolerance value that will result for the given ppm tolerance at 1000 m/z
                Return clsParentIonProcessing.GetParentIonToleranceDa(Me, 1000)
            Else
                Return SICTolerance
            End If
        End Get
        Set
            SetSICTolerance(Value, False)
        End Set
    End Property
    ''' <summary>
    ''' If True, then will look through the m/z values in the parent ion spectrum data to find the closest match
    ''' (within SICToleranceDa / sicOptions.CompressToleranceDivisorForDa); will update the reported m/z value to the one found
    ''' </summary>
    Public Property RefineReportedParentIonMZ As Boolean

    ''' <summary>
    ''' If both ScanRangeStart >=0 and ScanRangeEnd > 0 then will only process data between those scan numbers
    ''' </summary>
    Public Property ScanRangeStart As Integer
    Public Property ScanRangeEnd As Integer

    ''' <summary>
    ''' If both RTRangeStart >=0 and RTRangeEnd > RTRangeStart then will only process data between those that scan range (in minutes)
    ''' </summary>
    Public Property RTRangeStart As Single
    Public Property RTRangeEnd As Single

    ''' <summary>
    ''' If true, then combines data points that have similar m/z values (within tolerance) when loading
    ''' Tolerance is sicOptions.SICToleranceDa / sicOptions.CompressToleranceDivisorForDa
    ''' (or divided by sicOptions.CompressToleranceDivisorForPPM if sicOptions.SICToleranceIsPPM=True)
    ''' </summary>
    Public Property CompressMSSpectraData As Boolean

    ''' <summary>
    ''' If true, then combines data points that have similar m/z values (within tolerance) when loading
    ''' Tolerance is binningOptions.BinSize / sicOptions.CompressToleranceDivisorForDa
    ''' </summary>
    Public Property CompressMSMSSpectraData As Boolean

    ''' <summary>
    ''' When compressing spectra, sicOptions.SICTolerance and binningOptions.BinSize will be divided by this value
    ''' to determine the resolution to compress the data to
    ''' </summary>
    Public Property CompressToleranceDivisorForDa As Double

    ''' <summary>
    ''' If sicOptions.SICToleranceIsPPM is True, then this divisor is used instead of CompressToleranceDivisorForDa
    ''' </summary>
    Public Property CompressToleranceDivisorForPPM As Double

    ' The SIC is extended left and right until:
    '      1) the SIC intensity falls below IntensityThresholdAbsoluteMinimum,
    '      2) the SIC intensity falls below the maximum value observed times IntensityThresholdFractionMax,
    '   or 3) the distance exceeds MaxSICPeakWidthMinutesBackward or MaxSICPeakWidthMinutesForward

    ''' <summary>
    ''' Defaults to 5
    ''' </summary>
    Public Property MaxSICPeakWidthMinutesBackward As Single
        Get
            Return mMaxSICPeakWidthMinutesBackward
        End Get
        Set
            If Value < 0 Or Value > 10000 Then Value = 5
            mMaxSICPeakWidthMinutesBackward = Value
        End Set
    End Property

    ''' <summary>
    ''' Defaults to 5
    ''' </summary>
    Public Property MaxSICPeakWidthMinutesForward As Single
        Get
            Return mMaxSICPeakWidthMinutesForward
        End Get
        Set
            If Value < 0 Or Value > 10000 Then Value = 5
            mMaxSICPeakWidthMinutesForward = Value
        End Set
    End Property
    Public Property SICPeakFinderOptions As MASICPeakFinder.clsSICPeakFinderOptions

    Public Property ReplaceSICZeroesWithMinimumPositiveValueFromMSData As Boolean

    Public Property SaveSmoothedData As Boolean

    ''' <summary>
    ''' m/z Tolerance for finding similar parent ions; full tolerance is +/- this value
    ''' </summary>
    ''' <remarks>Defaults to 0.1</remarks>
    Public Property SimilarIonMZToleranceHalfWidth As Single
        Get
            Return mSimilarIonMZToleranceHalfWidth
        End Get
        Set
            If Value < 0.001 Or Value > 100 Then Value = 0.1
            mSimilarIonMZToleranceHalfWidth = Value
        End Set
    End Property

    ''' <summary>
    ''' Time Tolerance (in minutes) for finding similar parent ions; full tolerance is +/- this value
    ''' </summary>
    ''' <remarks>Defaults to 5</remarks>
    Public Property SimilarIonToleranceHalfWidthMinutes As Single
        Get
            Return mSimilarIonToleranceHalfWidthMinutes
        End Get
        Set
            If Value < 0 Or Value > 100000 Then Value = 5
            mSimilarIonToleranceHalfWidthMinutes = Value
        End Set
    End Property

    ''' <summary>
    ''' Defaults to 0.8
    ''' </summary>
    Public Property SpectrumSimilarityMinimum As Single
        Get
            Return mSpectrumSimilarityMinimum
        End Get
        Set
            If Value < 0 Or Value > 1 Then Value = 0.8
            mSpectrumSimilarityMinimum = Value
        End Set
    End Property
#End Region

#Region "Classwide Variables"
    Private mMaxSICPeakWidthMinutesBackward As Single
    Private mMaxSICPeakWidthMinutesForward As Single
#End Region

    Public Function GetSICTolerance() As Double
        Dim toleranceIsPPM As Boolean
        Return GetSICTolerance(toleranceIsPPM)
    End Function

    Public Function GetSICTolerance(<Out> ByRef toleranceIsPPM As Boolean) As Double
        toleranceIsPPM = SICToleranceIsPPM
        Return SICTolerance
    End Function

    Public Sub Reset()
        SICTolerance = 10
        SICToleranceIsPPM = True

        ' Typically only useful when using a small value for .SICTolerance
        RefineReportedParentIonMZ = False

        ScanRangeStart = 0
        ScanRangeEnd = 0
        RTRangeStart = 0
        RTRangeEnd = 0

        CompressMSSpectraData = True
        CompressMSMSSpectraData = True

        CompressToleranceDivisorForDa = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA
        CompressToleranceDivisorForPPM = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM

        MaxSICPeakWidthMinutesBackward = 5
        MaxSICPeakWidthMinutesForward = 5

        ReplaceSICZeroesWithMinimumPositiveValueFromMSData = True

        SICPeakFinderOptions = MASICPeakFinder.clsMASICPeakFinder.GetDefaultSICPeakFinderOptions

        SaveSmoothedData = False

        ' Note: When using narrow SIC tolerances, be sure to SimilarIonMZToleranceHalfWidth to a smaller value
        ' However, with very small values, the SpectraCache file will be much larger
        ' The default for SimilarIonMZToleranceHalfWidth is 0.1
        ' Consider using 0.05 when using ppm-based SIC tolerances
        SimilarIonMZToleranceHalfWidth = 0.05

        ' SimilarIonScanToleranceHalfWidth = 100
        SimilarIonToleranceHalfWidthMinutes = 5
        SpectrumSimilarityMinimum = 0.8
    End Sub

    Public Sub SetSICTolerance(toleranceValue As Double, toleranceIsPPM As Boolean)
        SICToleranceIsPPM = toleranceIsPPM

        If SICToleranceIsPPM Then
            If toleranceValue < 0 Or toleranceValue > 1000000 Then toleranceValue = 100
        Else
            If toleranceValue < 0 Or toleranceValue > 10000 Then toleranceValue = 0.6
        End If
        SICTolerance = toleranceValue
    End Sub

    Public Sub ValidateSICOptions()

        If CompressToleranceDivisorForDa < 1 Then
            CompressToleranceDivisorForDa = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_DA
        End If

        If CompressToleranceDivisorForPPM < 1 Then
            CompressToleranceDivisorForPPM = DEFAULT_COMPRESS_TOLERANCE_DIVISOR_FOR_PPM
        End If

    End Sub

    Public Overrides Function ToString() As String
        If SICToleranceIsPPM Then
            Return "SIC Tolerance: " & SICTolerance.ToString("0.00") & " ppm"
        Else
            Return "SIC Tolerance: " & SICTolerance.ToString("0.0000") & " Da"
        End If
    End Function

#Region "Classwide variables"
    Private mSimilarIonMZToleranceHalfWidth As Single = 0.1
    Private mSimilarIonToleranceHalfWidthMinutes As Single = 5
    Private mSpectrumSimilarityMinimum As Single = 0.8

#End Region
End Class
