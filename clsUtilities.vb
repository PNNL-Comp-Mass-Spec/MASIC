
Public Class clsUtilities

#Region "Constants and Enums"
    ' Const CHARGE_CARRIER_MASS_AVG As Double = 1.00739

    Public Const CHARGE_CARRIER_MASS_MONOISOTOPIC As Double = 1.00727649

#End Region

    Public Shared Function CheckPointInMZIgnoreRange(
      mz As Double,
      mzIgnoreRangeStart As Double,
      mzIgnoreRangeEnd As Double) As Boolean

        If mzIgnoreRangeStart > 0 OrElse mzIgnoreRangeEnd > 0 Then
            If mz <= mzIgnoreRangeEnd AndAlso mz >= mzIgnoreRangeStart Then
                ' The m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If

    End Function

    Public Shared Function ConvoluteMass(
      massMZ As Double,
      currentCharge As Short,
      Optional desiredCharge As Short = 1,
      Optional chargeCarrierMass As Double = 0) As Double

        ' Converts massMZ to the MZ that would appear at the given desiredCharge
        ' To return the neutral mass, set desiredCharge to 0

        ' If chargeCarrierMass is 0, uses CHARGE_CARRIER_MASS_MONOISOTOPIC

        Dim newMZ As Double

        If Math.Abs(chargeCarrierMass) < Double.Epsilon Then chargeCarrierMass = CHARGE_CARRIER_MASS_MONOISOTOPIC

        If currentCharge = desiredCharge Then
            newMZ = massMZ
        Else
            If currentCharge = 1 Then
                newMZ = massMZ
            ElseIf currentCharge > 1 Then
                ' Convert massMZ to M+H
                newMZ = (massMZ * currentCharge) - chargeCarrierMass * (currentCharge - 1)
            ElseIf currentCharge = 0 Then
                ' Convert massMZ (which is neutral) to M+H and store in newMZ
                newMZ = massMZ + chargeCarrierMass
            Else
                ' Negative charges are not supported; return 0
                Return 0
            End If

            If desiredCharge > 1 Then
                newMZ = (newMZ + chargeCarrierMass * (desiredCharge - 1)) / desiredCharge
            ElseIf desiredCharge = 1 Then
                ' Return M+H, which is currently stored in newMZ
            ElseIf desiredCharge = 0 Then
                ' Return the neutral mass
                newMZ -= chargeCarrierMass
            Else
                ' Negative charges are not supported; return 0
                newMZ = 0
            End If
        End If

        Return newMZ

    End Function

    Public Shared Function CSngSafe(value As Double) As Single
        If value > Single.MaxValue Then Return Single.MaxValue
        If value < Single.MinValue Then Return Single.MinValue
        Return CSng(value)
    End Function

    Public Shared Function IsNumber(value As String) As Boolean
        Try
            Return Double.TryParse(value, 0)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Shared Function ValuesMatch(value1 As Single, value2 As Single) As Boolean
        Return ValuesMatch(value1, value2, -1)
    End Function

    Public Shared Function ValuesMatch(value1 As Single, value2 As Single, digitsOfPrecision As Integer) As Boolean

        If digitsOfPrecision < 0 Then
            If Math.Abs(value1 - value2) < Single.Epsilon Then
                Return True
            End If
        Else
            If Math.Abs(Math.Round(value1, digitsOfPrecision) - Math.Round(value2, digitsOfPrecision)) < Single.Epsilon Then
                Return True
            End If

        End If

        Return False

    End Function

    Public Shared Function ValuesMatch(value1 As Double, value2 As Double) As Boolean
        Return ValuesMatch(value1, value2, -1)
    End Function

    Public Shared Function ValuesMatch(value1 As Double, value2 As Double, digitsOfPrecision As Integer) As Boolean
        If digitsOfPrecision < 0 Then
            If Math.Abs(value1 - value2) < Double.Epsilon Then
                Return True
            End If
        Else
            If Math.Abs(Math.Round(value1, digitsOfPrecision) - Math.Round(value2, digitsOfPrecision)) < Double.Epsilon Then
                Return True
            End If

        End If

        Return False
    End Function

#Region "PPMToMassConversion"
    Public Shared Function MassToPPM(massToConvert As Double, currentMZ As Double) As Double
        ' Converts massToConvert to ppm, based on the value of currentMZ

        Return massToConvert * 1000000.0 / currentMZ
    End Function

    Public Shared Function PPMToMass(ppmToConvert As Double, currentMZ As Double) As Double
        ' Converts ppmToConvert to a mass value, which is dependent on currentMZ

        Return ppmToConvert / 1000000.0 * currentMZ
    End Function
#End Region

End Class
