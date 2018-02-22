Option Strict On

' This class corrects the intensities of iTraq data, based on the expected overlapping isotopic distributions
' It supports 4-plex and 8-plex iTraq
'
' The isotopic distribution weights were provided by Feng Yang (and originally came from the iTraq manufacturer)
'
' There are two options for the iTRAQ 4-plex weights:
'   eCorrectionFactorsiTRAQ4Plex.ABSciex
'   eCorrectionFactorsiTRAQ4Plex.BroadInstitute

Public Class clsITraqIntensityCorrection

#Region "Constants and Enums"

    Private Const FOUR_PLEX_MATRIX_LENGTH As Integer = 4
    Private Const EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH As Integer = 8
    Private Const EIGHT_PLEX_LOW_RES_MATRIX_LENGTH As Integer = 9

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

    '<Obsolete("Use ReporterIonMode")>
    'Public ReadOnly Property ITraqMode As clsReporterIons.eReporterIonMassModeConstants
    '    Get
    '        Return mReporterIonMode
    '    End Get
    'End Property

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
    ''' <param name="eITraqMode">iTRAQ mode</param>
    ''' <remarks></remarks>
    Public Sub New(eITraqMode As clsReporterIons.eReporterIonMassModeConstants)
        Me.New(eITraqMode, eCorrectionFactorsiTRAQ4Plex.ABSciex)
    End Sub

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="eITraqMode">iTRAQ mode</param>
    ''' <param name="iTraqCorrectionFactorType">Correction factor type for 4-plex iTRAQ</param>
    ''' <remarks>The iTraqCorrectionFactorType parameter is only used if eITraqMode is ITraqFourMZ</remarks>
    Public Sub New(eITraqMode As clsReporterIons.eReporterIonMassModeConstants, iTraqCorrectionFactorType As eCorrectionFactorsiTRAQ4Plex)
        mReporterIonMode = eITraqMode
        mITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType

        mMatrixUtility = New MatrixDecompositionUtility.LUDecomposition()

        InitializeCoefficients(False)
    End Sub

    Public Sub UpdateITraqMode(eITraqMode As clsReporterIons.eReporterIonMassModeConstants)
        UpdateITraqMode(eITraqMode, mITraq4PlexCorrectionFactorType)
    End Sub

    Public Sub UpdateITraqMode(eITraqMode As clsReporterIons.eReporterIonMassModeConstants, iTraqCorrectionFactorType As eCorrectionFactorsiTRAQ4Plex)
        If mReporterIonMode <> eITraqMode OrElse mITraq4PlexCorrectionFactorType <> iTraqCorrectionFactorType Then
            mReporterIonMode = eITraqMode
            mITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType
            InitializeCoefficients(False)
        End If
    End Sub

    Public Function ApplyCorrection(ByRef sngReporterIonIntensites() As Single) As Boolean

        Dim dblOriginalIntensities() As Double
        Dim intDataCount As Integer = reporterIonIntensites.Count - 1

        ReDim dblOriginalIntensities(intDataCount)
        For intIndex = 0 To intDataCount
            dblOriginalIntensities(intIndex) = reporterIonIntensites(intIndex)
        Next

        If ApplyCorrection(dblOriginalIntensities, debugShowIntensities) Then
            For intIndex = 0 To intDataCount
                reporterIonIntensites(intIndex) = CSng(dblOriginalIntensities(intIndex))
            Next
            Return True
        Else
            Return False
        End If

    End Function

    Public Function ApplyCorrection(dblReporterIonIntensites() As Double) As Boolean

        Dim matrixSize = GetMatrixLength(mReporterIonMode)
        Dim iTraqMode = clsReporterIons.GetReporterIonModeDescription(mReporterIonMode)

        If reporterIonIntensites.Length <> matrixSize Then
            Throw New InvalidOperationException("Length of ReporterIonIntensites array must be " & matrixSize.ToString() &
                                                " when using the " & iTraqMode & " mode")
        End If

        Dim correctedIntensities = mMatrixUtility.ProcessData(mCoeffs, matrixSize, reporterIonIntensites)

        Dim maxIntensity As Double
        For index = 0 To matrixSize - 1
            maxIntensity = Math.Max(maxIntensity, reporterIonIntensites(index))
        Next

        If debugShowIntensities Then
            Console.WriteLine()
            Console.WriteLine("{0,-8} {1,-10} {2,-12}  {3}", "Index", "Intensity", "NewIntensity", "% Change")
        End If

        ' Now update dblReporterIonIntensites
        For index = 0 To matrixSize - 1
            If reporterIonIntensites(index) > 0 Then
                Else
                    dblReporterIonIntensites(intIndex) = dblCorrectedIntensities(intIndex)
                End If
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
            Case Else
                Throw New ArgumentOutOfRangeException("Invalid value for eReporterIonMode in GetMatrixLength: " & eReporterIonMode.ToString())
        End Select
    End Function

    ''' <summary>
    ''' Initialize the coefficients
    ''' </summary>
    ''' <param name="debugShowMatrixTable">When true, show a table of the coefficients at the console</param>
    Private Sub InitializeCoefficients(debugShowMatrixTable As Boolean)

        ' iTraq labels
        Dim udtIsoPct113 As udtIsotopeContributionType
        Dim udtIsoPct114 As udtIsotopeContributionType
        Dim udtIsoPct115 As udtIsotopeContributionType
        Dim udtIsoPct116 As udtIsotopeContributionType
        Dim udtIsoPct117 As udtIsotopeContributionType
        Dim udtIsoPct118 As udtIsotopeContributionType
        Dim udtIsoPct119 As udtIsotopeContributionType
        Dim udtIsoPct120 As udtIsotopeContributionType
        Dim udtIsoPct121 As udtIsotopeContributionType


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

                Else

                    ' 4-plex ITraq, isotope contribution table
                    ' Source percentages provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)

                    udtIsoPct114 = DefineIsotopeContribution(0, 0, 95.5, 4.5, 0)
                    udtIsoPct115 = DefineIsotopeContribution(0, 0.9, 94.6, 4.5, 0)
                    udtIsoPct116 = DefineIsotopeContribution(0, 0.9, 95.7, 3.4, 0)
                    udtIsoPct117 = DefineIsotopeContribution(0, 1.4, 98.6, 0, 0)

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


                ReDim mCoeffs(intMatrixSize - 1, intMatrixSize - 1)

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

                mCoeffs(6, 4) = udtIsoPct117.Plus2
                mCoeffs(6, 5) = udtIsoPct118.Plus1
                mCoeffs(6, 6) = udtIsoPct119.Zero
                mCoeffs(6, 7) = udtIsoPct121.Minus2

                mCoeffs(7, 5) = 0           ' udtIsoPct118.Plus2
                mCoeffs(7, 7) = udtIsoPct121.Zero

            Case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes

                ' 8-plex ITraq, isotope contribution table for Low Res MS/MS
                ' Source percentages come from page 664 in:
                '  Vaudel, M., Sickmann, A., and L. Martens. "Peptide and protein quantification: A map of the minefield",
                '  Proteomics 2010, 10, 650-670.

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

                Console.Write("  " & i.ToString() & "  ")
                For j = 0 To maxIndex
                    If Math.Abs(mCoeffs(i, j)) < Single.Epsilon Then
                        Console.Write("   0    ")
                    Else
                        Console.Write(" " & mCoeffs(i, j).ToString("0.0000") & " ")
                    End If

                Next
                Console.WriteLine()
            Next
        End If

    End Sub

    ''' <summary>
    ''' Given a set of isotope correction values
    ''' </summary>
    ''' <param name="Minus2"></param>
    ''' <param name="Minus1"></param>
    ''' <param name="Zero"></param>
    ''' <param name="Plus1"></param>
    ''' <param name="Plus2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function DefineIsotopeContribution(Minus2 As Single,
                                               Minus1 As Single,
                                               Zero As Single,
                                               Plus1 As Single,
                                               Plus2 As Single) As udtIsotopeContributionType

        Dim udtIsotopePct As udtIsotopeContributionType

        With udtIsotopePct
            .Minus2 = Minus2
            .Minus1 = Minus1
            .Zero = Zero
            .Plus1 = Plus1
            .Plus2 = Plus2
        End With

        Return udtIsotopePct

    End Function

End Class
