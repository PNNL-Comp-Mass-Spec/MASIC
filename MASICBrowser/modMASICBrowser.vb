Module modMASICBrowswer

    'Private objSpectrum As CWSpectrumDLL.SpectrumClass

    Public Sub Main()

        Dim objBrowserForm As frmBrowser

        Try
            'objSpectrum = New CWSpectrumDLL.SpectrumClass
            'objSpectrum.ShowSpectrum()

            objBrowserForm = New frmBrowser
            objBrowserForm.ShowDialog()

        Catch ex As Exception
            MsgBox("Unable to initialize the CW Spectrum DLL.  Ending program." & vbCrLf & ex.Message, MsgBoxStyle.Exclamation Or MsgBoxStyle.OKOnly, "Missing DLL")
        End Try

    End Sub

    Public Sub ShowSICSpectrum()
        'objSpectrum.ShowSpectrum()
    End Sub
End Module
