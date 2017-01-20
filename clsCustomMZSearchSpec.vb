Public Class clsCustomMZSearchSpec

#Region "Properties"
    Public Property MZ As Double

    ''' <summary>
    ''' If 0, then uses the global search tolerance defined
    ''' </summary>
    Public Property MZToleranceDa As Double

    ''' <summary>
    ''' This is an Integer if ScanType = eCustomSICScanTypeConstants.Absolute
    ''' It is a Single if ScanType = .Relative or ScanType = .AcquisitionTime
    ''' </summary>
    Public Property ScanOrAcqTimeCenter As Single

    ''' <summary>
    ''' This is an Integer if ScanType = eCustomSICScanTypeConstants.Absolute
    ''' It is a Single if ScanType = .Relative or ScanType = .AcquisitionTime
    ''' </summary>
    ''' <remarks>Set to 0 to search the entire file for the given mass</remarks>
    Public Property ScanOrAcqTimeTolerance As Single

    Public Property Comment As String

#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <param name="targetMz"></param>
    Public Sub New(targetMz As Double)
        MZ = targetMz
    End Sub

    Public Overrides Function ToString() As String
        Return "m/z: " & MZ.ToString("0.0000") & " ±" & MZToleranceDa.ToString("0.0000")
    End Function

End Class
