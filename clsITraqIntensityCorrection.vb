Option Strict On

' This class corrects the intensities of iTraq or TMT data, based on the expected overlapping isotopic distributions
' It supports 4-plex and 8-plex iTraq
' It also supports TMT10 and TMT11
'
' The isotopic distribution weights are provided by the iTraq or TMT manufacturer
'
' There are two options for the iTRAQ 4-plex weights:
'   eCorrectionFactorsiTRAQ4Plex.ABSciex
'   eCorrectionFactorsiTRAQ4Plex.BroadInstitute

Public Class clsITraqIntensityCorrection

#Region "Constants and Enums"

    Private Const FOUR_PLEX_MATRIX_LENGTH As Integer = 4
    Private Const EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH As Integer = 8
    Private Const EIGHT_PLEX_LOW_RES_MATRIX_LENGTH As Integer = 9
    Private Const TEN_PLEX_TMT_MATRIX_LENGTH As Integer = 10
    Private Const ELEVEN_PLEX_TMT_MATRIX_LENGTH As Integer = 11

    Public Enum eCorrectionFactorsiTRAQ4Plex
        ABSciex = 0
        BroadInstitute = 1          ' Provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)
    End Enum
#End Region

#Region "Structures"
    Private Structure udtIsotopeContributionType
        Public Minus2 As Single
        Public Minus1 As Single
        Public Zero As Single
        Public Plus1 As Single
        Public Plus2 As Single
    End Structure
#End Region

#Region "Classwide Variables"
    Private mReporterIonMode As clsReporterIons.eReporterIonMassModeConstants

    Private mITraq4PlexCorrectionFactorType As eCorrectionFactorsiTRAQ4Plex

    ' Matrix of coefficients, derived from the isotope contribution table
    Private mCoeffs(,) As Double

    Private ReadOnly mMatrixUtility As MatrixDecompositionUtility.LUDecomposition

#End Region

#Region "Properties"

    Public ReadOnly Property ReporterIonMode As clsReporterIons.eReporterIonMassModeConstants
        Get
            Return mReporterIonMode
        End Get
    End Property

    Public ReadOnly Property ITraq4PlexCorrectionFactorType As eCorrectionFactorsiTRAQ4Plex
        Get
            Return mITraq4PlexCorrectionFactorType
        End Get
    End Property
#End Region

    ''' <summary>
    ''' Constructor; assumes iTraqCorrectionFactorType = eCorrectionFactorsiTRAQ4Plex.ABSciex
    ''' </summary>
    ''' <param name="eReporterIonMode">iTRAQ or TMT mode</param>
    ''' <remarks></remarks>
    Public Sub New(eReporterIonMode As clsReporterIons.eReporterIonMassModeConstants)
        Me.New(eReporterIonMode, eCorrectionFactorsiTRAQ4Plex.ABSciex)
    End Sub

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="eReporterIonMode">iTRAQ or TMT mode</param>
    ''' <param name="iTraqCorrectionFactorType">Correction factor type for 4-plex iTRAQ</param>
    ''' <remarks>The iTraqCorrectionFactorType parameter is only used if eReporterIonMode is ITraqFourMZ</remarks>
    Public Sub New(eReporterIonMode As clsReporterIons.eReporterIonMassModeConstants, iTraqCorrectionFactorType As eCorrectionFactorsiTRAQ4Plex)
        mReporterIonMode = eReporterIonMode
        mITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType

        mMatrixUtility = New MatrixDecompositionUtility.LUDecomposition()

        If mReporterIonMode = clsReporterIons.eReporterIonMassModeConstants.CustomOrNone Then
            Return
        End If

        InitializeCoefficients(False)
    End Sub

    ''' <summary>
    ''' Change the reporter ion mode
    ''' </summary>
    ''' <param name="eReporterIonMode"></param>
    Public Sub UpdateReporterIonMode(eReporterIonMode As clsReporterIons.eReporterIonMassModeConstants)
        UpdateReporterIonMode(eReporterIonMode, mITraq4PlexCorrectionFactorType)
    End Sub

    ''' <summary>
    ''' Change the reporter ion mode
    ''' </summary>
    ''' <param name="eReporterIonMode"></param>
    ''' <param name="iTraqCorrectionFactorType"></param>
    Public Sub UpdateReporterIonMode(eReporterIonMode As clsReporterIons.eReporterIonMassModeConstants, iTraqCorrectionFactorType As eCorrectionFactorsiTRAQ4Plex)
        If mReporterIonMode <> eReporterIonMode OrElse mITraq4PlexCorrectionFactorType <> iTraqCorrectionFactorType Then
            mReporterIonMode = eReporterIonMode
            mITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType
            InitializeCoefficients(True)
        End If
    End Sub

    ''' <summary>
    ''' Apply the correction factors to the reporter ions
    ''' </summary>
    ''' <param name="reporterIonIntensities"></param>
    ''' <param name="debugShowIntensities">When true, show the old and new reporter ion intensities at the console</param>
    ''' <returns></returns>
    Public Function ApplyCorrection(ByRef reporterIonIntensities() As Single, Optional debugShowIntensities As Boolean = False) As Boolean

        Dim originalIntensities() As Double
        Dim dataCount As Integer = reporterIonIntensities.Count - 1

        ReDim originalIntensities(dataCount)
        For index = 0 To dataCount
            originalIntensities(index) = reporterIonIntensities(index)
        Next

        If ApplyCorrection(originalIntensities, debugShowIntensities) Then
            For index = 0 To dataCount
                reporterIonIntensities(index) = CSng(originalIntensities(index))
            Next
            Return True
        Else
            Return False
        End If

    End Function

    ''' <summary>
    ''' Apply the correction factors to the reporter ions
    ''' </summary>
    ''' <param name="reporterIonIntensities"></param>
    ''' <param name="debugShowIntensities">When true, show the old and new reporter ion intensities at the console</param>
    ''' <returns></returns>
    Public Function ApplyCorrection(reporterIonIntensities() As Double, Optional debugShowIntensities As Boolean = False) As Boolean

        Dim matrixSize = GetMatrixLength(mReporterIonMode)
        Dim eReporterIonMode = clsReporterIons.GetReporterIonModeDescription(mReporterIonMode)

        If reporterIonIntensities.Length <> matrixSize Then
            Throw New InvalidOperationException("Length of ReporterIonIntensities array must be " & matrixSize.ToString() &
                                                " when using the " & eReporterIonMode & " mode")
        End If

        Dim correctedIntensities = mMatrixUtility.ProcessData(mCoeffs, matrixSize, reporterIonIntensities)

        Dim maxIntensity As Double
        For index = 0 To matrixSize - 1
            maxIntensity = Math.Max(maxIntensity, reporterIonIntensities(index))
        Next

        If debugShowIntensities Then
            Console.WriteLine()
            Console.WriteLine("{0,-8} {1,-10} {2,-12}  {3}", "Index", "Intensity", "NewIntensity", "% Change")
        End If

        ' Now update reporterIonIntensities
        For index = 0 To matrixSize - 1
            If reporterIonIntensities(index) > 0 Then
                Dim newIntensity As Double
                If correctedIntensities(index) < 0 Then
                    newIntensity = 0
                Else
                    newIntensity = correctedIntensities(index)
                End If

                If debugShowIntensities Then
                    ' Compute percent change vs. the maximum reporter ion intensity
                    Dim percentChange = (newIntensity - reporterIonIntensities(index)) / maxIntensity * 100
                    Dim percentChangeRounded = CInt(Math.Round(percentChange, 0))

                    Dim visualPercentChange As String
                    If percentChangeRounded > 0 Then
                        visualPercentChange = New String("+"c, percentChangeRounded)
                    ElseIf percentChangeRounded < 0 Then
                        visualPercentChange = New String("-"c, -percentChangeRounded)
                    Else
                        visualPercentChange = ""
                    End If

                    Console.WriteLine("{0,-8} {1,-10:0.0} {2,-12:0.0}{3,7:0.0}%   {4}", index, reporterIonIntensities(index), newIntensity, percentChange, visualPercentChange)
                End If

                reporterIonIntensities(index) = newIntensity
            End If
        Next

        Return True

    End Function

    Private Function GetMatrixLength(eReporterIonMode As clsReporterIons.eReporterIonMassModeConstants) As Integer
        Select Case eReporterIonMode
            Case clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ
                Return FOUR_PLEX_MATRIX_LENGTH
            Case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes
                Return EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH
            Case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes
                Return EIGHT_PLEX_LOW_RES_MATRIX_LENGTH
            Case clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ
                Return TEN_PLEX_TMT_MATRIX_LENGTH
            Case clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ
                Return ELEVEN_PLEX_TMT_MATRIX_LENGTH
            Case Else
                Throw New ArgumentOutOfRangeException("Invalid value for eReporterIonMode in GetMatrixLength: " & eReporterIonMode.ToString())
        End Select
    End Function

    ''' <summary>
    ''' Initialize the coefficients
    ''' </summary>
    ''' <param name="debugShowMatrixTable">When true, show a table of the coefficients at the console</param>
    Private Sub InitializeCoefficients(debugShowMatrixTable As Boolean)

        ' iTraq reporter ions
        Dim udtIsoPct113 As udtIsotopeContributionType
        Dim udtIsoPct114 As udtIsotopeContributionType
        Dim udtIsoPct115 As udtIsotopeContributionType
        Dim udtIsoPct116 As udtIsotopeContributionType
        Dim udtIsoPct117 As udtIsotopeContributionType
        Dim udtIsoPct118 As udtIsotopeContributionType
        Dim udtIsoPct119 As udtIsotopeContributionType
        Dim udtIsoPct120 As udtIsotopeContributionType
        Dim udtIsoPct121 As udtIsotopeContributionType

        ' TMT reporter ions
        Dim udtIsoPct126 As udtIsotopeContributionType
        Dim udtIsoPct127N As udtIsotopeContributionType
        Dim udtIsoPct127C As udtIsotopeContributionType
        Dim udtIsoPct128N As udtIsotopeContributionType
        Dim udtIsoPct128C As udtIsotopeContributionType
        Dim udtIsoPct129N As udtIsotopeContributionType
        Dim udtIsoPct129C As udtIsotopeContributionType
        Dim udtIsoPct130N As udtIsotopeContributionType
        Dim udtIsoPct130C As udtIsotopeContributionType
        Dim udtIsoPct131N As udtIsotopeContributionType
        Dim udtIsoPct131C As udtIsotopeContributionType

        Dim matrixSize = GetMatrixLength(mReporterIonMode)
        Dim maxIndex = matrixSize - 1

        Select Case mReporterIonMode
            Case clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ

                If mITraq4PlexCorrectionFactorType = eCorrectionFactorsiTRAQ4Plex.ABSciex Then
                    ' 4-plex ITraq, isotope contribution table
                    ' Source percentages provided by Applied Biosystems

                    udtIsoPct114 = DefineIsotopeContribution(0, 1, 92.9, 5.9, 0.2)
                    udtIsoPct115 = DefineIsotopeContribution(0, 2, 92.3, 5.6, 0.1)
                    udtIsoPct116 = DefineIsotopeContribution(0, 3, 92.4, 4.5, 0.1)
                    udtIsoPct117 = DefineIsotopeContribution(0.1, 4, 92.3, 3.5, 0.1)

                ElseIf mITraq4PlexCorrectionFactorType = eCorrectionFactorsiTRAQ4Plex.BroadInstitute Then

                    ' 4-plex ITraq, isotope contribution table
                    ' Source percentages provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)

                    udtIsoPct114 = DefineIsotopeContribution(0, 0, 95.5, 4.5, 0)
                    udtIsoPct115 = DefineIsotopeContribution(0, 0.9, 94.6, 4.5, 0)
                    udtIsoPct116 = DefineIsotopeContribution(0, 0.9, 95.7, 3.4, 0)
                    udtIsoPct117 = DefineIsotopeContribution(0, 1.4, 98.6, 0, 0)
                Else
                    Throw New ArgumentOutOfRangeException(NameOf(mITraq4PlexCorrectionFactorType), "Unrecognized value for the iTRAQ 4 plex correction type")
                End If

                ' Goal is to generate either of these two matrices (depending on mITraq4PlexCorrectionFactorType):
                '        0      1      2      3
                '      -----  -----  -----  -----
                '  0   0.929  0.020    0      0
                '  1   0.059  0.923  0.030  0.001
                '  2   0.002  0.056  0.924  0.040
                '  3     0    0.001  0.045  0.923

                '        0      1      2      3
                '      -----  -----  -----  -----
                '  0   0.955  0.009    0      0
                '  1   0.045  0.946  0.009    0
                '  2     0    0.045  0.957  0.014
                '  3     0      0    0.034  0.986


                ReDim mCoeffs(maxIndex, maxIndex)

                mCoeffs(0, 0) = udtIsoPct114.Zero
                mCoeffs(0, 1) = udtIsoPct115.Minus1
                mCoeffs(0, 2) = udtIsoPct116.Minus2

                mCoeffs(1, 0) = udtIsoPct114.Plus1
                mCoeffs(1, 1) = udtIsoPct115.Zero
                mCoeffs(1, 2) = udtIsoPct116.Minus1
                mCoeffs(1, 3) = udtIsoPct117.Minus2

                mCoeffs(2, 0) = udtIsoPct114.Plus2
                mCoeffs(2, 2) = udtIsoPct116.Zero
                mCoeffs(2, 1) = udtIsoPct115.Plus1
                mCoeffs(2, 3) = udtIsoPct117.Minus1

                mCoeffs(3, 1) = udtIsoPct115.Plus2
                mCoeffs(3, 2) = udtIsoPct116.Plus1
                mCoeffs(3, 3) = udtIsoPct117.Zero

            Case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes

                ' 8-plex ITraq, isotope contribution table for High Res MS/MS
                ' Source percentages provided by Applied Biosystems
                ' Note there is a 2 Da jump between 119 and 121, which is why 7.44 and 0.87 are not included in mCoeffs()

                udtIsoPct113 = DefineIsotopeContribution(0, 0, 92.89, 6.89, 0.22)
                udtIsoPct114 = DefineIsotopeContribution(0, 0.94, 93.01, 5.9, 0.16)
                udtIsoPct115 = DefineIsotopeContribution(0, 1.88, 93.12, 4.9, 0.1)
                udtIsoPct116 = DefineIsotopeContribution(0, 2.82, 93.21, 3.9, 0.07)
                udtIsoPct117 = DefineIsotopeContribution(0.06, 3.77, 93.29, 2.88, 0)
                udtIsoPct118 = DefineIsotopeContribution(0.09, 4.71, 93.32, 1.88, 0)
                udtIsoPct119 = DefineIsotopeContribution(0.14, 5.66, 93.34, 0.87, 0)
                udtIsoPct121 = DefineIsotopeContribution(0.27, 7.44, 92.11, 0.18, 0)

                ' Goal is to generate this matrix:
                '        0       1       2       3       4       5       6       7
                '      ------  ------  ------  ------  ------  ------  ------  ------
                '  0   0.9289  0.0094    0       0       0       0       0       0
                '  1   0.0689  0.9301  0.0188    0       0       0       0       0
                '  2   0.0022  0.0590  0.9312  0.0282  0.0006    0       0       0
                '  3     0     0.0016  0.0490  0.9321  0.0377  0.0009    0       0
                '  4     0       0     0.0010  0.0390  0.9329  0.0471  0.0014    0
                '  5     0       0       0     0.0007  0.0288  0.9332  0.0566    0
                '  6     0       0       0       0       0     0.0188  0.9334  0.0027
                '  7     0       0       0       0       0       0       0     0.9211


                ReDim mCoeffs(maxIndex, maxIndex)

                mCoeffs(0, 0) = udtIsoPct113.Zero
                mCoeffs(0, 1) = udtIsoPct114.Minus1
                mCoeffs(0, 2) = udtIsoPct115.Minus2

                mCoeffs(1, 0) = udtIsoPct113.Plus1
                mCoeffs(1, 1) = udtIsoPct114.Zero
                mCoeffs(1, 2) = udtIsoPct115.Minus1
                mCoeffs(1, 3) = udtIsoPct116.Minus2

                mCoeffs(2, 0) = udtIsoPct113.Plus2
                mCoeffs(2, 1) = udtIsoPct114.Plus1
                mCoeffs(2, 2) = udtIsoPct115.Zero
                mCoeffs(2, 3) = udtIsoPct116.Minus1
                mCoeffs(2, 4) = udtIsoPct117.Minus2

                mCoeffs(3, 1) = udtIsoPct114.Plus2
                mCoeffs(3, 2) = udtIsoPct115.Plus1
                mCoeffs(3, 3) = udtIsoPct116.Zero
                mCoeffs(3, 4) = udtIsoPct117.Minus1
                mCoeffs(3, 5) = udtIsoPct118.Minus2

                mCoeffs(4, 2) = udtIsoPct115.Plus2
                mCoeffs(4, 3) = udtIsoPct116.Plus1
                mCoeffs(4, 4) = udtIsoPct117.Zero
                mCoeffs(4, 5) = udtIsoPct118.Minus1
                mCoeffs(4, 6) = udtIsoPct119.Minus2

                mCoeffs(5, 3) = udtIsoPct116.Plus2
                mCoeffs(5, 4) = udtIsoPct117.Plus1
                mCoeffs(5, 5) = udtIsoPct118.Zero
                mCoeffs(5, 6) = udtIsoPct119.Minus1
                mCoeffs(5, 7) = 0

                mCoeffs(6, 4) = udtIsoPct117.Plus2
                mCoeffs(6, 5) = udtIsoPct118.Plus1
                mCoeffs(6, 6) = udtIsoPct119.Zero
                mCoeffs(6, 7) = udtIsoPct121.Minus2

                mCoeffs(7, 5) = 0           ' udtIsoPct118.Plus2
                mCoeffs(7, 7) = udtIsoPct121.Zero

            Case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes

                ' 8-plex ITraq, isotope contribution table for Low Res MS/MS

                ' ReSharper disable CommentTypo

                ' Source percentages come from page 664 in:
                '  Vaudel, M., Sickmann, A., and L. Martens. "Peptide and protein quantification: A map of the minefield",
                '  Proteomics 2010, 10, 650-670.

                ' ReSharper restore CommentTypo

                udtIsoPct113 = DefineIsotopeContribution(0, 0, 92.89, 6.89, 0.22)
                udtIsoPct114 = DefineIsotopeContribution(0, 0.94, 93.01, 5.9, 0.16)
                udtIsoPct115 = DefineIsotopeContribution(0, 1.88, 93.12, 4.9, 0.1)
                udtIsoPct116 = DefineIsotopeContribution(0, 2.82, 93.21, 3.9, 0.07)
                udtIsoPct117 = DefineIsotopeContribution(0.06, 3.77, 93.29, 2.88, 0)
                udtIsoPct118 = DefineIsotopeContribution(0.09, 4.71, 93.32, 1.88, 0)
                udtIsoPct119 = DefineIsotopeContribution(0.14, 5.66, 93.34, 0.87, 0)
                udtIsoPct120 = DefineIsotopeContribution(0, 0, 91.01, 8.62, 0)
                udtIsoPct121 = DefineIsotopeContribution(0.27, 7.44, 92.11, 0.18, 0)

                ' Goal is to generate this expanded matrix, which takes Phenylalanine into account
                '        0       1       2       3       4       5       6       7      8
                '      ------  ------  ------  ------  ------  ------  ------  ------  ------
                '  0   0.9289  0.0094    0       0       0       0       0       0       0
                '  1   0.0689  0.9301  0.0188    0       0       0       0       0       0
                '  2   0.0022  0.0590  0.9312  0.0282  0.0006    0       0       0       0
                '  3     0     0.0016  0.0490  0.9321  0.0377  0.0009    0       0       0
                '  4     0       0     0.0010  0.0390  0.9329  0.0471  0.0014    0       0
                '  5     0       0       0     0.0007  0.0288  0.9332  0.0566    0       0
                '  6     0       0       0       0       0     0.0188  0.9334    0     0.0027
                '  7     0       0       0       0       0       0     0.8700  0.9101  0.0744
                '  8     0       0       0       0       0       0       0     0.0862  0.9211

                ReDim mCoeffs(maxIndex, maxIndex)

                mCoeffs(0, 0) = udtIsoPct113.Zero
                mCoeffs(0, 1) = udtIsoPct114.Minus1
                mCoeffs(0, 2) = udtIsoPct115.Minus2

                mCoeffs(1, 0) = udtIsoPct113.Plus1
                mCoeffs(1, 1) = udtIsoPct114.Zero
                mCoeffs(1, 2) = udtIsoPct115.Minus1
                mCoeffs(1, 3) = udtIsoPct116.Minus2

                mCoeffs(2, 0) = udtIsoPct113.Plus2
                mCoeffs(2, 1) = udtIsoPct114.Plus1
                mCoeffs(2, 2) = udtIsoPct115.Zero
                mCoeffs(2, 3) = udtIsoPct116.Minus1
                mCoeffs(2, 4) = udtIsoPct117.Minus2

                mCoeffs(3, 1) = udtIsoPct114.Plus2
                mCoeffs(3, 2) = udtIsoPct115.Plus1
                mCoeffs(3, 3) = udtIsoPct116.Zero
                mCoeffs(3, 4) = udtIsoPct117.Minus1
                mCoeffs(3, 5) = udtIsoPct118.Minus2

                mCoeffs(4, 2) = udtIsoPct115.Plus2
                mCoeffs(4, 3) = udtIsoPct116.Plus1
                mCoeffs(4, 4) = udtIsoPct117.Zero
                mCoeffs(4, 5) = udtIsoPct118.Minus1
                mCoeffs(4, 6) = udtIsoPct119.Minus2

                mCoeffs(5, 3) = udtIsoPct116.Plus2
                mCoeffs(5, 4) = udtIsoPct117.Plus1
                mCoeffs(5, 5) = udtIsoPct118.Zero
                mCoeffs(5, 6) = udtIsoPct119.Minus1
                mCoeffs(5, 7) = 0

                mCoeffs(6, 4) = udtIsoPct117.Plus2
                mCoeffs(6, 5) = udtIsoPct118.Plus1
                mCoeffs(6, 6) = udtIsoPct119.Zero
                mCoeffs(6, 7) = 0
                mCoeffs(6, 8) = udtIsoPct121.Minus2

                mCoeffs(7, 5) = 0
                mCoeffs(7, 6) = udtIsoPct119.Plus1
                mCoeffs(7, 7) = udtIsoPct120.Zero
                mCoeffs(7, 8) = udtIsoPct121.Minus1

                mCoeffs(8, 6) = udtIsoPct119.Plus2
                mCoeffs(8, 7) = udtIsoPct120.Plus1
                mCoeffs(8, 8) = udtIsoPct121.Zero

            Case clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ, clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ

                ' 10-plex TMT and 11-plex TMT, isotope contribution table for High Res MS/MS
                ' Source percentages provided by Thermo

                ' TMT10plex lot RG234623
                ' TMT11plex lot SD250515
                'udtIsoPct126 = DefineIsotopeContribution(0, 0, 95.1, 4.9, 0)
                'udtIsoPct127N = DefineIsotopeContribution(0, 0.2, 94, 5.8, 0)
                'udtIsoPct127C = DefineIsotopeContribution(0, 0.3, 94.9, 4.8, 0)
                'udtIsoPct128N = DefineIsotopeContribution(0, 0.3, 96.1, 3.6, 0)
                'udtIsoPct128C = DefineIsotopeContribution(0, 0.6, 95.5, 3.9, 0)
                'udtIsoPct129N = DefineIsotopeContribution(0, 0.8, 96.2, 3, 0)
                'udtIsoPct129C = DefineIsotopeContribution(0, 1.3, 95.8, 2.9, 0)
                'udtIsoPct130N = DefineIsotopeContribution(0, 1.4, 93, 2.3, 3.3)
                'udtIsoPct130C = DefineIsotopeContribution(0, 1.7, 96.1, 2.2, 0)
                'udtIsoPct131N = DefineIsotopeContribution(0.2, 2, 95.6, 2.2, 0)
                'udtIsoPct131C = DefineIsotopeContribution(0, 2.6, 94.5, 2.9, 0)


                ' TMT10plex lot A37725
                ' TMT11plex lot TB265130
                udtIsoPct126 = DefineIsotopeContribution(0, 0, 92.081, 7.551, 0.368)
                udtIsoPct127N = DefineIsotopeContribution(0, 0.093, 92.593, 7.315, 0)
                udtIsoPct127C = DefineIsotopeContribution(0, 0.468, 93.633, 5.899, 0)
                udtIsoPct128N = DefineIsotopeContribution(0, 0.658, 93.985, 5.357, 0)
                udtIsoPct128C = DefineIsotopeContribution(0.186, 1.484, 92.764, 5.566, 0)
                udtIsoPct129N = DefineIsotopeContribution(0, 2.326, 93.023, 4.651, 0)
                udtIsoPct129C = DefineIsotopeContribution(0, 2.158, 93.809, 4.034, 0)
                udtIsoPct130N = DefineIsotopeContribution(0, 2.533, 93.809, 3.659, 0)
                udtIsoPct130C = DefineIsotopeContribution(0, 1.628, 95.785, 2.586, 0)
                udtIsoPct131N = DefineIsotopeContribution(0, 3.625, 92.937, 3.439, 0)
                udtIsoPct131C = DefineIsotopeContribution(0, 3.471, 93.809, 2.72, 0)

                ' Goal is to generate this matrix (10-plex will not have the final row or final column)
                '        0       1       2       3       4       5       6       7       8       9      10
                '      ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------
                '  0   0.9208    0     0.0047    0     0.0019    0       0       0       0       0       0
                '  1     0     0.9259    0     0.0066    0       0       0       0       0       0       0
                '  2   0.0755    0     0.9363    0     0.0148    0       0       0       0       0       0
                '  3     0     0.0732    0     0.9399    0     0.0233    0       0       0       0       0
                '  4   0.0037    0     0.0590    0     0.9276    0     0.0216    0       0       0       0
                '  5     0       0       0     0.0536    0     0.9302    0     0.0253    0       0       0
                '  6     0       0       0       0     0.0557    0     0.9381    0     0.0163    0       0
                '  7     0       0       0       0       0     0.0465    0     0.9381    0     0.0363    0
                '  8     0       0       0       0       0       0     0.0403    0     0.9579    0     0.0347
                '  9     0       0       0       0       0       0       0     0.0366    0     0.9294    0
                ' 10     0       0       0       0       0       0       0       0     0.0259    0     0.9381

                ReDim mCoeffs(maxIndex, maxIndex)

                mCoeffs(0, 0) = udtIsoPct126.Zero
                mCoeffs(0, 1) = 0
                mCoeffs(0, 2) = udtIsoPct127C.Minus1
                mCoeffs(0, 3) = 0
                mCoeffs(0, 4) = udtIsoPct128C.Minus2

                mCoeffs(1, 0) = 0
                mCoeffs(1, 1) = udtIsoPct127N.Zero
                mCoeffs(1, 2) = 0
                mCoeffs(1, 3) = udtIsoPct128N.Minus1
                mCoeffs(1, 4) = 0
                mCoeffs(1, 5) = udtIsoPct129N.Minus2

                mCoeffs(2, 0) = udtIsoPct126.Plus1
                mCoeffs(2, 1) = 0
                mCoeffs(2, 2) = udtIsoPct127C.Zero
                mCoeffs(2, 3) = 0
                mCoeffs(2, 4) = udtIsoPct128C.Minus1
                mCoeffs(2, 5) = 0
                mCoeffs(2, 6) = udtIsoPct129C.Minus2

                mCoeffs(3, 0) = 0
                mCoeffs(3, 1) = udtIsoPct127N.Plus1
                mCoeffs(3, 2) = 0
                mCoeffs(3, 3) = udtIsoPct128N.Zero
                mCoeffs(3, 4) = 0
                mCoeffs(3, 5) = udtIsoPct129N.Minus1
                mCoeffs(3, 6) = 0
                mCoeffs(3, 7) = udtIsoPct130N.Minus2

                mCoeffs(4, 0) = udtIsoPct126.Plus2
                mCoeffs(4, 1) = 0
                mCoeffs(4, 2) = udtIsoPct127C.Plus1
                mCoeffs(4, 3) = 0
                mCoeffs(4, 4) = udtIsoPct128C.Zero
                mCoeffs(4, 5) = 0
                mCoeffs(4, 6) = udtIsoPct129C.Minus1
                mCoeffs(4, 7) = 0
                mCoeffs(4, 8) = udtIsoPct130C.Minus2

                mCoeffs(5, 1) = udtIsoPct127N.Plus2
                mCoeffs(5, 2) = 0
                mCoeffs(5, 3) = udtIsoPct128N.Plus1
                mCoeffs(5, 4) = 0
                mCoeffs(5, 5) = udtIsoPct129N.Zero
                mCoeffs(5, 6) = 0
                mCoeffs(5, 7) = udtIsoPct130N.Minus1
                mCoeffs(5, 8) = 0
                mCoeffs(5, 9) = udtIsoPct131N.Minus2

                mCoeffs(6, 2) = udtIsoPct127C.Plus2
                mCoeffs(6, 3) = 0
                mCoeffs(6, 4) = udtIsoPct128C.Plus1
                mCoeffs(6, 5) = 0
                mCoeffs(6, 6) = udtIsoPct129C.Zero
                mCoeffs(6, 7) = 0
                mCoeffs(6, 8) = udtIsoPct130C.Minus1

                mCoeffs(7, 3) = udtIsoPct128N.Plus2
                mCoeffs(7, 4) = 0
                mCoeffs(7, 5) = udtIsoPct129N.Plus1
                mCoeffs(7, 6) = 0
                mCoeffs(7, 7) = udtIsoPct130N.Zero
                mCoeffs(7, 8) = 0
                mCoeffs(7, 9) = udtIsoPct131N.Minus1

                mCoeffs(8, 4) = udtIsoPct128C.Plus2
                mCoeffs(8, 5) = 0
                mCoeffs(8, 6) = udtIsoPct129C.Plus1
                mCoeffs(8, 7) = 0
                mCoeffs(8, 8) = udtIsoPct130C.Zero
                mCoeffs(8, 9) = 0
                If maxIndex >= 10 Then
                    mCoeffs(8, 10) = udtIsoPct131C.Minus1
                End If

                mCoeffs(9, 5) = udtIsoPct129N.Plus2
                mCoeffs(9, 6) = 0
                mCoeffs(9, 7) = udtIsoPct130N.Plus1
                mCoeffs(9, 8) = 0
                mCoeffs(9, 9) = udtIsoPct131N.Zero

                If maxIndex >= 10 Then
                    mCoeffs(10, 6) = udtIsoPct129C.Plus2
                    mCoeffs(10, 7) = 0
                    mCoeffs(10, 8) = udtIsoPct130C.Plus1
                    mCoeffs(10, 9) = 0
                    mCoeffs(10, 10) = udtIsoPct131C.Zero
                End If

            Case Else
                Throw New Exception("Invalid reporter ion mode in IntensityCorrection.InitializeCoefficients")
        End Select

        ' Now divide all of the weights by 100
        For i = 0 To maxIndex
            For j = 0 To maxIndex
                mCoeffs(i, j) /= 100.0
            Next j
        Next i

        If debugShowMatrixTable Then
            ' Print out the matrix
            Console.WriteLine()
            Console.WriteLine()
            Console.WriteLine("Reporter Ion Correction Matrix; mode = " & mReporterIonMode.ToString())
            For i = 0 To maxIndex
                If i = 0 Then
                    ' Header line
                    Console.Write("     ")
                    For j = 0 To maxIndex
                        Console.Write("   " & j.ToString() & "    ")
                    Next
                    Console.WriteLine()

                    Console.Write("     ")
                    For k = 0 To maxIndex
                        Console.Write(" ------ ")
                    Next
                    Console.WriteLine()

                End If

                Dim indexSpacer As String
                If i < 10 Then indexSpacer = "  " Else indexSpacer = " "

                Console.Write("  " & i.ToString() & indexSpacer)
                For j = 0 To maxIndex
                    If Math.Abs(mCoeffs(i, j)) < Single.Epsilon Then
                        Console.Write("   0    ")
                    Else
                        Console.Write(" " & mCoeffs(i, j).ToString("0.0000") & " ")
                    End If

                Next
                Console.WriteLine()
            Next
            Console.WriteLine()
        End If

    End Sub

    ''' <summary>
    ''' Given a set of isotope correction values
    ''' </summary>
    ''' <param name="minus2">Value between 0 and 100, but typically close to 0</param>
    ''' <param name="minus1">Value between 0 and 100, but typically close to 0</param>
    ''' <param name="zero">Value between 0 and 100, but typically close to 98</param>
    ''' <param name="plus1">Value between 0 and 100, but typically close to 0</param>
    ''' <param name="plus2">Value between 0 and 100, but typically close to 0</param>
    ''' <returns></returns>
    ''' <remarks>The values should sum to 100; however, if zero (aka the Monoisotopic Peak) is 0, its value will be auto-computed</remarks>
    Private Function DefineIsotopeContribution(minus2 As Single,
                                               minus1 As Single,
                                               zero As Single,
                                               plus1 As Single,
                                               plus2 As Single) As udtIsotopeContributionType

        Dim udtIsotopePct As udtIsotopeContributionType

        If Math.Abs(zero) < Single.Epsilon Or zero < 0 Then
            zero = 100 - minus2 - minus1 - plus1 - plus2
        End If

        Dim sum = minus2 + minus1 + zero + plus1 + plus2
        If Math.Abs(100 - sum) > 0.05 Then
            Throw New Exception(String.Format("Parameters for DefineIsotopeContribution should add up to 100; current sum is {0:F1}", sum))
        End If

        With udtIsotopePct
            .Minus2 = minus2
            .Minus1 = minus1
            .Zero = zero
            .Plus1 = plus1
            .Plus2 = plus2
        End With

        Return udtIsotopePct

    End Function

End Class
