Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports PRISM

' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 17, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may Not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause

Module modMain

    Public Const PROGRAM_DATE As String = "October 1, 2018"

    Private mInputFilePath As String

    Public Function Main() As Integer
        ' Returns 0 if no error, error code if an error

        Dim objParseCommandLine As New clsParseCommandLine()
        Dim proceed As Boolean

        mInputFilePath = String.Empty

        Try
            proceed = False
            If objParseCommandLine.ParseCommandLine Then
                If SetOptionsUsingCommandLineParameters(objParseCommandLine) Then proceed = True
            ElseIf Not objParseCommandLine.NeedToShowHelp Then
                proceed = True
            End If

            If objParseCommandLine.NeedToShowHelp OrElse Not proceed Then
                ShowProgramHelp()
                Return -1
            End If

            ShowGUI()

            Return 0

        Catch ex As Exception
            ShowErrorMessage("Error occurred in modMain->Main: " & Environment.NewLine & ex.Message)
            Return -1
        End Try

    End Function

    ''' <summary>
    ''' Returns the full path to the executing .Exe or .Dll
    ''' </summary>
    ''' <returns>File path</returns>
    ''' <remarks></remarks>
    Private Function GetAppPath() As String
        Return Assembly.GetExecutingAssembly().Location
    End Function

    ''' <summary>
    ''' Returns the .NET assembly version followed by the program date
    ''' </summary>
    ''' <param name="strProgramDate"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetAppVersion(strProgramDate As String) As String
        Return Assembly.GetExecutingAssembly().GetName().Version.ToString() & " (" & strProgramDate & ")"
    End Function


    Private Function SetOptionsUsingCommandLineParameters(objParseCommandLine As clsParseCommandLine) As Boolean
        ' Returns True if no problems; otherwise, returns false

        Dim value As String = String.Empty
        Dim validParameters = New List(Of String) From {"I"}

        Try
            ' Make sure no invalid parameters are present
            If objParseCommandLine.InvalidParametersPresent(validParameters) Then
                ShowErrorMessage("Invalid command line parameters",
                  (From item In objParseCommandLine.InvalidParameters(validParameters) Select "/" + item).ToList())
                Return False
            Else

                ' Query objParseCommandLine to see if various parameters are present
                With objParseCommandLine
                    If .RetrieveValueForParameter("I", value) Then
                        mInputFilePath = value
                    ElseIf .NonSwitchParameterCount > 0 Then
                        ' Treat the first non-switch parameter as the input file
                        mInputFilePath = .RetrieveNonSwitchParameter(0)
                    End If

                End With

                Return True
            End If

        Catch ex As Exception
            ShowErrorMessage("Error parsing the command line parameters: " & Environment.NewLine & ex.Message)
        End Try

        Return False

    End Function

    Private Sub ShowErrorMessage(message As String)
        ConsoleMsgUtils.ShowError(message)
    End Sub

    Private Sub ShowErrorMessage(title As String, errorMessages As IEnumerable(Of String))
        ConsoleMsgUtils.ShowErrors(title, errorMessages)
    End Sub

    Public Sub ShowGUI()

        Application.EnableVisualStyles()
        Application.DoEvents()

        Dim masicBrowser = New frmBrowser()

        If Not String.IsNullOrWhiteSpace(mInputFilePath) Then
            masicBrowser.FileToAutoLoad = mInputFilePath
        End If
        masicBrowser.ShowDialog()

    End Sub

    Private Sub ShowProgramHelp()

        Try

            Console.WriteLine("This program is used to visualize the results from MASIC")
            Console.WriteLine()

            Console.WriteLine("Program syntax:" & Environment.NewLine & Path.GetFileName(GetAppPath()))
            Console.WriteLine(" MasicResults_SICs.xml")
            Console.WriteLine()

            Console.WriteLine("The input file path is the MASIC results file to load")
            Console.WriteLine()

            Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2017")
            Console.WriteLine("Version: " & GetAppVersion(PROGRAM_DATE))

            Console.WriteLine()

            Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov")
            Console.WriteLine("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/")
            Console.WriteLine()

            ' Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
            Thread.Sleep(750)

        Catch ex As Exception
            ShowErrorMessage("Error displaying the program syntax: " & ex.Message)
        End Try

    End Sub


End Module
