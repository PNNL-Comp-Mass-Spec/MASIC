Public Class clsReporterIonInfo

    Public MZ As Double

    Public MZToleranceDa As Double

    ''' <summary>
    ''' Should be False for Reporter Ions and True for other ions, e.g. immonium loss from phenylalanine
    ''' </summary>
    Public ContaminantIon As Boolean

    ''' <summary>
    ''' Signal/Noise ratio; only populated for FTMS MS2 spectra on Thermo instruments
    ''' </summary>
    Public SignalToNoise As Double

    ''' <summary>
    ''' Resolution; only populated for FTMS MS2 spectra on Thermo instruments
    ''' </summary>
    Public Resolution As Double

    ''' <summary>
    ''' m/z value for which the resolution and signal/noise value was computed
    ''' Only populated for FTMS MS2 spectra on Thermo instruments
    ''' </summary>
    Public LabelDataMZ As Double

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(ionMZ As Double)
        MZ = ionMZ
    End Sub

    Public Sub New(ionMZ As Double, isContaminantIon As Boolean)
        MZ = ionMZ
        ContaminantIon = isContaminantIon
    End Sub

    Public Overrides Function ToString() As String
        Return "m/z: " & MZ.ToString("0.0000") & " ±" & MZToleranceDa.ToString("0.0000")
    End Function

End Class
