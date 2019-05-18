Option Strict On

Imports PRISM

''' <summary>
''' Used to track the MZ and Intensity values of a given mass spectrum
''' </summary>
Public Class clsMSSpectrum

    Private mScanNumber As Integer

    ''' <summary>
    ''' Scan number
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>0 if not in use</remarks>
    Public Property ScanNumber As Integer
        Get
            Return mScanNumber
        End Get
        Private Set
            mScanNumber = Value
        End Set
    End Property

    Public ReadOnly Property IonCount As Integer
        Get
            Return IonsMZ.Count
        End Get
    End Property

    ''' <summary>
    ''' List of m/z's
    ''' </summary>
    Public ReadOnly IonsMZ As List(Of Double)

    ''' <summary>
    ''' List of intensities
    ''' </summary>
    Public ReadOnly IonsIntensity As List(Of Double)

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(intScanNumber As Integer)
        ScanNumber = intScanNumber

        IonsMZ = New List(Of Double)
        IonsIntensity = New List(Of Double)
    End Sub

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(intScanNumber As Integer, mzList As IList(Of Double), intensityList As IList(Of Single), dataCount As Integer)
        Me.New(intScanNumber)

        For i = 0 To dataCount - 1
            IonsMZ.Add(mzList(i))
            IonsIntensity.Add(intensityList(i))
        Next
    End Sub

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New(intScanNumber As Integer, mzList As IList(Of Double), intensityList As IList(Of Double), dataCount As Integer)
        Me.New(intScanNumber)

        For i = 0 To dataCount - 1
            IonsMZ.Add(mzList(i))
            IonsIntensity.Add(intensityList(i))
        Next
    End Sub

    ''' <summary>
    ''' Clear the mz and intensity values (but leave the scan number unchanged)
    ''' </summary>
    Public Sub Clear()
        IonsMZ.Clear()
        IonsIntensity.Clear()
    End Sub

    ''' <summary>
    ''' Clear the mz and intensity values, and update the scan number
    ''' </summary>
    Public Sub Clear(newScanNumber As Integer)
        IonsMZ.Clear()
        IonsIntensity.Clear()
        ScanNumber = newScanNumber
    End Sub

    Public Function Clone() As clsMSSpectrum
        Return Copy(Me)
    End Function

    Public Function Copy(objSource As clsMSSpectrum) As clsMSSpectrum
        Dim newSpectrum = New clsMSSpectrum(objSource.ScanNumber, objSource.IonsMZ, objSource.IonsIntensity, objSource.IonsMZ.Count)
        Return newSpectrum
    End Function

    Public Sub ReplaceData(spectrum As clsMSSpectrum, scanNumberOverride As Integer)

        ScanNumber = spectrum.ScanNumber
        If ScanNumber <> scanNumberOverride Then
            ScanNumber = scanNumberOverride
        End If

        IonsMZ.Clear()
        IonsMZ.AddRange(spectrum.IonsMZ)

        IonsIntensity.Clear()
        IonsIntensity.AddRange(spectrum.IonsIntensity)

    End Sub

    Public Sub ShrinkArrays(ionCountNew As Integer)
        If ionCountNew > IonsMZ.Count Then
            Throw New Exception("ShrinkArrays should only be called with a length less than or equal to the current length")
        End If

        Dim countToRemove = IonsMZ.Count - ionCountNew
        If countToRemove = 0 Then Exit Sub

        IonsMZ.RemoveRange(ionCountNew, countToRemove)
        IonsIntensity.RemoveRange(ionCountNew, countToRemove)
    End Sub

    Public Sub UpdateScanNumber(newScanNumber As Integer)
        ScanNumber = newScanNumber
    End Sub
End Class
