Public Class clsReporterIons

#Region "Constants and Enums"
    Public Const REPORTER_ION_TOLERANCE_DA_DEFAULT As Double = 0.5
    Public Const REPORTER_ION_TOLERANCE_DA_MINIMUM As Double = 0.001

    Public Const REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES As Double = 0.015

    Public Enum eReporterIonMassModeConstants
        CustomOrNone = 0
        ITraqFourMZ = 1
        ITraqETDThreeMZ = 2
        TMTTwoMZ = 3
        TMTSixMZ = 4
        ITraqEightMZHighRes = 5     ' This version of 8-plex iTraq should be used when the reporter ion search tolerance is +/-0.03 Da or smaller
        ITraqEightMZLowRes = 6      ' This version of 8-plex iTraq will account for immonium loss from phenylalanine
        PCGalnaz = 7
        HemeCFragment = 8
        LycAcetFragment = 9
        TMTTenMZ = 10               ' Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
        OGlcNAc = 11
        FrackingAmine20160217 = 12
        FSFACustomCarbonyl = 13
        FSFACustomCarboxylic = 14
        FSFACustomHydroxyl = 15
    End Enum

#End Region

#Region "Classwide Variables"

    Private mReporterIonToleranceDaDefault As Double

    Private mReporterIonMassMode As eReporterIonMassModeConstants

#End Region

#Region "Properties"

    ''' <summary>
    ''' When ReporterIonStatsEnabled = True, MZIntensityFilterIgnoreRangeStart and MZIntensityFilterIgnoreRangeEnd 
    ''' will be populated with the m/z range of the reporter ions being processed
    ''' </summary>
    Public Property MZIntensityFilterIgnoreRangeStart As Double

    ''' <summary>
    ''' When ReporterIonStatsEnabled = True, MZIntensityFilterIgnoreRangeStart and MZIntensityFilterIgnoreRangeEnd 
    ''' will be populated with the m/z range of the reporter ions being processed
    ''' </summary>
    Public Property MZIntensityFilterIgnoreRangeEnd As Double

    Public ReadOnly Property ReporterIonList As List(Of clsReporterIonInfo)

    Public Property ReporterIonStatsEnabled() As Boolean

    Public Property ReporterIonApplyAbundanceCorrection() As Boolean

    Public Property ReporterIonITraq4PlexCorrectionFactorType() As clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex

    ''' <summary>
    ''' This is ignored if mReporterIonApplyAbundanceCorrection is False
    ''' </summary>
    Public Property ReporterIonSaveUncorrectedIntensities() As Boolean

    Public Property ReporterIonMassMode() As eReporterIonMassModeConstants
        Get
            Return mReporterIonMassMode
        End Get
        Set(Value As eReporterIonMassModeConstants)
            SetReporterIonMassMode(Value)
        End Set
    End Property

    ''' <summary>
    ''' When true, observed m/z values of the reporter ions will be included in the _ReporterIons.txt file
    ''' </summary>
    Public Property ReporterIonSaveObservedMasses() As Boolean

    Public Property ReporterIonToleranceDaDefault() As Double
        Get
            If mReporterIonToleranceDaDefault < Double.Epsilon Then mReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT
            Return mReporterIonToleranceDaDefault
        End Get
        Set(Value As Double)
            If Value < Double.Epsilon Then Value = REPORTER_ION_TOLERANCE_DA_DEFAULT
            mReporterIonToleranceDaDefault = Value
        End Set
    End Property

#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        ReporterIonList = New List(Of clsReporterIonInfo)
        InitializeReporterIonInfo()
    End Sub

    Public Shared Function GetDefaultReporterIons(eReporterIonMassMode As eReporterIonMassModeConstants) As List(Of clsReporterIonInfo)
        If eReporterIonMassMode = eReporterIonMassModeConstants.ITraqEightMZHighRes Then
            Return GetDefaultReporterIons(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES)
        Else
            Return GetDefaultReporterIons(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT)
        End If
    End Function

    Public Shared Function GetDefaultReporterIons(
      eReporterIonMassMode As eReporterIonMassModeConstants,
      dblMZToleranceDa As Double) As List(Of clsReporterIonInfo)

        Dim reporterIons = New List(Of clsReporterIonInfo)

        Select Case eReporterIonMassMode
            Case eReporterIonMassModeConstants.ITraqFourMZ
                ' ITRAQ
                reporterIons.Add(New clsReporterIonInfo(114.1112))
                reporterIons.Add(New clsReporterIonInfo(115.1083))
                reporterIons.Add(New clsReporterIonInfo(116.1116))
                reporterIons.Add(New clsReporterIonInfo(117.115))

            Case eReporterIonMassModeConstants.ITraqETDThreeMZ
                ' ITRAQ ETD tags
                reporterIons.Add(New clsReporterIonInfo(101.107))
                reporterIons.Add(New clsReporterIonInfo(102.104))
                reporterIons.Add(New clsReporterIonInfo(104.1107))

            Case eReporterIonMassModeConstants.TMTTwoMZ
                ' TMT duplex Isobaric tags (from Thermo)               
                reporterIons.Add(New clsReporterIonInfo(126.1283))
                reporterIons.Add(New clsReporterIonInfo(127.1316))

            Case eReporterIonMassModeConstants.TMTSixMZ
                ' TMT sixplex Isobaric tags (from Thermo)
                ' These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                '                                                           ' Old values:
                reporterIons.Add(New clsReporterIonInfo(126.127725))        ' 126.1283
                reporterIons.Add(New clsReporterIonInfo(127.12476))         ' 127.1316
                reporterIons.Add(New clsReporterIonInfo(128.134433))        ' 128.135
                reporterIons.Add(New clsReporterIonInfo(129.131468))        ' 129.1383
                reporterIons.Add(New clsReporterIonInfo(130.141141))        ' 130.1417
                reporterIons.Add(New clsReporterIonInfo(131.138176))        ' 131.1387

            Case eReporterIonMassModeConstants.TMTTenMZ
                ' TMT 10-plex Isobaric tags (from Thermo)
                ' These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                ' Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                reporterIons.Add(New clsReporterIonInfo(126.127725))
                reporterIons.Add(New clsReporterIonInfo(127.12476))
                reporterIons.Add(New clsReporterIonInfo(127.131079))
                reporterIons.Add(New clsReporterIonInfo(128.128114))
                reporterIons.Add(New clsReporterIonInfo(128.134433))
                reporterIons.Add(New clsReporterIonInfo(129.131468))
                reporterIons.Add(New clsReporterIonInfo(129.137787))
                reporterIons.Add(New clsReporterIonInfo(130.134822))
                reporterIons.Add(New clsReporterIonInfo(130.141141))
                reporterIons.Add(New clsReporterIonInfo(131.138176))

            Case eReporterIonMassModeConstants.ITraqEightMZHighRes

                ' ITRAQ eight-plex Isobaric tags, Low-Res MS/MS
                reporterIons.Add(New clsReporterIonInfo(113.107873))
                reporterIons.Add(New clsReporterIonInfo(114.111228))
                reporterIons.Add(New clsReporterIonInfo(115.108263))
                reporterIons.Add(New clsReporterIonInfo(116.111618))
                reporterIons.Add(New clsReporterIonInfo(117.114973))
                reporterIons.Add(New clsReporterIonInfo(118.112008))
                reporterIons.Add(New clsReporterIonInfo(119.115363))
                reporterIons.Add(New clsReporterIonInfo(121.122072))


            Case eReporterIonMassModeConstants.ITraqEightMZLowRes

                ' ITRAQ eight-plex Isobaric tags, Low-Res MS/MS               
                reporterIons.Add(New clsReporterIonInfo(113.107873))
                reporterIons.Add(New clsReporterIonInfo(114.111228))
                reporterIons.Add(New clsReporterIonInfo(115.108263))
                reporterIons.Add(New clsReporterIonInfo(116.111618))
                reporterIons.Add(New clsReporterIonInfo(117.114973))
                reporterIons.Add(New clsReporterIonInfo(118.112008))
                reporterIons.Add(New clsReporterIonInfo(119.115363))

                ' This corresponds to immonium ion loss from Phenylalanine (147.06841 - 26.9871 since Immonium is CO minus H)
                reporterIons.Add(New clsReporterIonInfo(120.08131, True))

                reporterIons.Add(New clsReporterIonInfo(121.122072))

            Case eReporterIonMassModeConstants.PCGalnaz

                ' Custom reporter ions for Josh Alfaro               
                reporterIons.Add(New clsReporterIonInfo(204.0871934))     ' C8H14NO5
                reporterIons.Add(New clsReporterIonInfo(300.130787))      ' C11H18N5O5
                reporterIons.Add(New clsReporterIonInfo(503.2101566))     ' C19H31N6O10

            Case eReporterIonMassModeConstants.HemeCFragment

                ' Custom reporter ions for Eric Merkley               
                reporterIons.Add(New clsReporterIonInfo(616.1767))
                reporterIons.Add(New clsReporterIonInfo(617.1845))

            Case eReporterIonMassModeConstants.LycAcetFragment

                ' Custom reporter ions for Ernesto Nakayasu               
                reporterIons.Add(New clsReporterIonInfo(126.09134))
                reporterIons.Add(New clsReporterIonInfo(127.094695))

            Case eReporterIonMassModeConstants.OGlcNAc
                ' O-GlcNAc               
                reporterIons.Add(New clsReporterIonInfo(204.0872))
                reporterIons.Add(New clsReporterIonInfo(300.13079))
                reporterIons.Add(New clsReporterIonInfo(503.21017))

            Case eReporterIonMassModeConstants.FrackingAmine20160217
                ' Product ions associated with FrackingFluid_amine_1_02172016               
                reporterIons.Add(New clsReporterIonInfo(157.089))
                reporterIons.Add(New clsReporterIonInfo(170.097))
                reporterIons.Add(New clsReporterIonInfo(234.059))

            Case eReporterIonMassModeConstants.FSFACustomCarbonyl
                ' Custom product ions from Chengdong Xu               
                reporterIons.Add(New clsReporterIonInfo(171.104))
                reporterIons.Add(New clsReporterIonInfo(236.074))
                reporterIons.Add(New clsReporterIonInfo(257.088))

            Case eReporterIonMassModeConstants.FSFACustomCarboxylic
                ' Custom product ions from Chengdong Xu               
                reporterIons.Add(New clsReporterIonInfo(171.104))
                reporterIons.Add(New clsReporterIonInfo(234.058))
                reporterIons.Add(New clsReporterIonInfo(336.174))

            Case eReporterIonMassModeConstants.FSFACustomHydroxyl
                ' Custom product ions from Chengdong Xu               
                reporterIons.Add(New clsReporterIonInfo(151.063))
                reporterIons.Add(New clsReporterIonInfo(166.087))

            Case Else
                ' Includes eReporterIonMassModeConstants.CustomOrNone
                reporterIons.Clear()
        End Select

        For Each reporterIon In reporterIons
            reporterIon.MZToleranceDa = dblMZToleranceDa
        Next

        Return reporterIons

    End Function

    Public Shared Function GetReporterIonModeDescription(eReporterIonMode As eReporterIonMassModeConstants) As String

        Select Case eReporterIonMode
            Case eReporterIonMassModeConstants.CustomOrNone
                Return "Custom/None"
            Case eReporterIonMassModeConstants.ITraqFourMZ
                Return "4-plex iTraq"
            Case eReporterIonMassModeConstants.ITraqETDThreeMZ
                Return "3-plex ETD iTraq"
            Case eReporterIonMassModeConstants.TMTTwoMZ
                Return "2-plex TMT"
            Case eReporterIonMassModeConstants.TMTSixMZ
                Return "6-plex TMT"
            Case eReporterIonMassModeConstants.TMTTenMZ
                Return "10-plex TMT"
            Case eReporterIonMassModeConstants.ITraqEightMZHighRes
                Return "8-plex iTraq (High Res MS/MS)"
            Case eReporterIonMassModeConstants.ITraqEightMZLowRes
                Return "8-plex iTraq (Low Res MS/MS)"
            Case eReporterIonMassModeConstants.PCGalnaz
                Return "PCGalnaz (300.13 m/z and 503.21 m/z)"
            Case eReporterIonMassModeConstants.HemeCFragment
                Return "Heme C (616.18 m/z and 616.19 m/z)"
            Case eReporterIonMassModeConstants.LycAcetFragment
                Return "Lys Acet (126.091 m/z and 127.095 m/z)"
            Case eReporterIonMassModeConstants.OGlcNAc
                Return "O-GlcNAc (204.087, 300.13, and 503.21 m/z)"
            Case eReporterIonMassModeConstants.FrackingAmine20160217
                Return "Fracking Amine 20160217 (157.089, 170.097, and 234.059 m/z)"
            Case eReporterIonMassModeConstants.FSFACustomCarbonyl
                Return "FSFA Custom Carbonyl (171.104, 236.074, 157.088 m/z)"
            Case eReporterIonMassModeConstants.FSFACustomCarboxylic
                Return "FSFA Custom Carboxylic (171.104, 234.058, 336.174 m/z)"
            Case eReporterIonMassModeConstants.FSFACustomHydroxyl
                Return "FSFA Custom Hydroxyl (151.063 and 166.087 m/z)"
            Case Else
                Return "Unknown mode"
        End Select

    End Function

    Private Sub InitializeReporterIonInfo()
        ReporterIonList.Clear()

        SetReporterIonMassMode(eReporterIonMassModeConstants.CustomOrNone)

        Me.ReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT
        Me.ReporterIonApplyAbundanceCorrection = True
        Me.ReporterIonITraq4PlexCorrectionFactorType = clsITraqIntensityCorrection.eCorrectionFactorsiTRAQ4Plex.ABSciex

        Me.ReporterIonSaveObservedMasses = False
        Me.ReporterIonSaveUncorrectedIntensities = False

    End Sub

    Public Sub SetReporterIonMassMode(eReporterIonMassMode As eReporterIonMassModeConstants)
        If eReporterIonMassMode = eReporterIonMassModeConstants.ITraqEightMZHighRes Then
            SetReporterIonMassMode(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES)
        Else
            SetReporterIonMassMode(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT)
        End If
    End Sub

    Public Sub SetReporterIonMassMode(
      eReporterIonMassMode As eReporterIonMassModeConstants,
      dblMZToleranceDa As Double)

        ' Note: If eReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone then nothing is changed

        If eReporterIonMassMode <> eReporterIonMassModeConstants.CustomOrNone Then
            Me.ReporterIonToleranceDaDefault = dblMZToleranceDa

            Dim reporterIonInfo = GetDefaultReporterIons(eReporterIonMassMode, dblMZToleranceDa)

            SetReporterIons(reporterIonInfo, False)
            mReporterIonMassMode = eReporterIonMassMode
        End If

    End Sub

    Public Sub SetReporterIons(
      reporterIons As List(Of clsReporterIonInfo),
      blnCustomReporterIons As Boolean)

        ReporterIonList.Clear()
        If reporterIons Is Nothing OrElse reporterIons.Count = 0 Then
            Return
        End If

        For Each reporterIon In reporterIons
            If reporterIon.MZToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM Then
                reporterIon.MZToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM
            End If
            ReporterIonList.Add(reporterIon)
        Next

        If blnCustomReporterIons Then
            mReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone
        End If

    End Sub

    Public Sub SetReporterIons(
      dblReporterIonMZList() As Double)
        SetReporterIons(dblReporterIonMZList, REPORTER_ION_TOLERANCE_DA_DEFAULT)
    End Sub

    Public Sub SetReporterIons(
      dblReporterIonMZList() As Double,
      dblMZToleranceDa As Double)
        SetReporterIons(dblReporterIonMZList, dblMZToleranceDa, True)
    End Sub

    Public Sub SetReporterIons(
      dblReporterIonMZList() As Double,
      dblMZToleranceDa As Double,
      blnCustomReporterIons As Boolean)

        ' dblMZToleranceDa is the search tolerance (half width)

        If dblMZToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM Then
            dblMZToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM
        End If

        ReporterIonList.Clear()
        If dblReporterIonMZList Is Nothing OrElse dblReporterIonMZList.Length = 0 Then
            mReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone
            Return
        End If

        For Each reporterIonMZ In dblReporterIonMZList
            Dim newReporterIon = New clsReporterIonInfo(reporterIonMZ)
            newReporterIon.MZToleranceDa = dblMZToleranceDa

            ReporterIonList.Add(newReporterIon)
        Next

        If blnCustomReporterIons Then
            mReporterIonMassMode = eReporterIonMassModeConstants.CustomOrNone
        End If
    End Sub

    Public Sub UpdateMZIntensityFilterIgnoreRange()
        ' Look at the m/z values in ReporterIonList to determine the minimum and maximum m/z values
        ' Update MZIntensityFilterIgnoreRangeStart and MZIntensityFilterIgnoreRangeEnd to be
        '  2x .MZToleranceDa away from the minimum and maximum

        If ReporterIonStatsEnabled AndAlso ReporterIonList.Count > 0 Then
            MZIntensityFilterIgnoreRangeStart = ReporterIonList(0).MZ - ReporterIonList(0).MZToleranceDa * 2
            MZIntensityFilterIgnoreRangeEnd = ReporterIonList(0).MZ + ReporterIonList(0).MZToleranceDa * 2

            For Each reporterIon In ReporterIonList
                Dim dblMzStart = reporterIon.MZ - reporterIon.MZToleranceDa * 2
                MZIntensityFilterIgnoreRangeStart = Math.Min(MZIntensityFilterIgnoreRangeStart, dblMzStart)

                Dim dblMzEnd = reporterIon.MZ + reporterIon.MZToleranceDa * 2
                MZIntensityFilterIgnoreRangeEnd = Math.Max(MZIntensityFilterIgnoreRangeEnd, dblMzEnd)
            Next
        Else
            MZIntensityFilterIgnoreRangeStart = 0
            MZIntensityFilterIgnoreRangeEnd = 0
        End If

    End Sub

End Class
