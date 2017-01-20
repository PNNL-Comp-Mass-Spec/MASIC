
Public Class clsUtilities

#Region "Constants and Enums"
    Public Const CHARGE_CARRIER_MASS_MONOISO As Double = 1.00727649
#End Region

    Public Shared Function CheckPointInMZIgnoreRange(
      dblMZ As Double,
      dblMZIgnoreRangeStart As Double,
      dblMZIgnoreRangeEnd As Double) As Boolean

        If dblMZIgnoreRangeStart > 0 OrElse dblMZIgnoreRangeEnd > 0 Then
            If dblMZ <= dblMZIgnoreRangeEnd AndAlso dblMZ >= dblMZIgnoreRangeStart Then
                ' The m/z value is between dblMZIgnoreRangeStart and dblMZIgnoreRangeEnd
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If

    End Function


    Public Shared Function ConvoluteMass(
      dblMassMZ As Double,
      intCurrentCharge As Short,
      Optional intDesiredCharge As Short = 1,
      Optional dblChargeCarrierMass As Double = 0) As Double

        ' Converts dblMassMZ to the MZ that would appear at the given intDesiredCharge
        ' To return the neutral mass, set intDesiredCharge to 0

        ' If dblChargeCarrierMass is 0, then uses CHARGE_CARRIER_MASS_MONOISO
        'Const CHARGE_CARRIER_MASS_AVG As Double = 1.00739
        'Const CHARGE_CARRIER_MASS_MONOISO As Double = 1.00727649

        Dim dblNewMZ As Double

        If Math.Abs(dblChargeCarrierMass) < Double.Epsilon Then dblChargeCarrierMass = CHARGE_CARRIER_MASS_MONOISO

        If intCurrentCharge = intDesiredCharge Then
            dblNewMZ = dblMassMZ
        Else
            If intCurrentCharge = 1 Then
                dblNewMZ = dblMassMZ
            ElseIf intCurrentCharge > 1 Then
                ' Convert dblMassMZ to M+H
                dblNewMZ = (dblMassMZ * intCurrentCharge) - dblChargeCarrierMass * (intCurrentCharge - 1)
            ElseIf intCurrentCharge = 0 Then
                ' Convert dblMassMZ (which is neutral) to M+H and store in dblNewMZ
                dblNewMZ = dblMassMZ + dblChargeCarrierMass
            Else
                ' Negative charges are not supported; return 0
                Return 0
            End If

            If intDesiredCharge > 1 Then
                dblNewMZ = (dblNewMZ + dblChargeCarrierMass * (intDesiredCharge - 1)) / intDesiredCharge
            ElseIf intDesiredCharge = 1 Then
                ' Return M+H, which is currently stored in dblNewMZ
            ElseIf intDesiredCharge = 0 Then
                ' Return the neutral mass
                dblNewMZ -= dblChargeCarrierMass
            Else
                ' Negative charges are not supported; return 0
                dblNewMZ = 0
            End If
        End If

        Return dblNewMZ

    End Function

    Public Shared Function IsNumber(strValue As String) As Boolean
        Try
            Return Double.TryParse(strValue, 0)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Shared Function ValuesMatch(sngValue1 As Single, sngValue2 As Single) As Boolean
        Return ValuesMatch(sngValue1, sngValue2, -1)
    End Function

    Public Shared Function ValuesMatch(sngValue1 As Single, sngValue2 As Single, digitsOfPrecision As Integer) As Boolean

        If digitsOfPrecision < 0 Then
            If Math.Abs(sngValue1 - sngValue2) < Single.Epsilon Then
                Return True
            End If
        Else
            If Math.Abs(Math.Round(sngValue1, digitsOfPrecision) - Math.Round(sngValue2, digitsOfPrecision)) < Single.Epsilon Then
                Return True
            End If

        End If

        Return False

    End Function

    Public Shared Function ValuesMatch(dblValue1 As Double, dblValue2 As Double, digitsOfPrecision As Integer) As Boolean
        If digitsOfPrecision < 0 Then
            If Math.Abs(dblValue1 - dblValue2) < Double.Epsilon Then
                Return True
            End If
        Else
            If Math.Abs(Math.Round(dblValue1, digitsOfPrecision) - Math.Round(dblValue2, digitsOfPrecision)) < Double.Epsilon Then
                Return True
            End If

        End If

        Return False
    End Function

#Region "PPMToMassConversion"
    Public Shared Function MassToPPM(dblMassToConvert As Double, dblCurrentMZ As Double) As Double
        ' Converts dblMassToConvert to ppm, based on the value of dblCurrentMZ

        Return dblMassToConvert * 1000000.0 / dblCurrentMZ
    End Function

    Public Shared Function PPMToMass(dblPPMToConvert As Double, dblCurrentMZ As Double) As Double
        ' Converts dblPPMToConvert to a mass value, which is dependent on dblCurrentMZ

        Return dblPPMToConvert / 1000000.0 * dblCurrentMZ
    End Function
#End Region

End Class
