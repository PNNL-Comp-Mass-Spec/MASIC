Option Strict On

' This class can be used to parse the text following the program name when a
'  program is started from the command line
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started November 8, 2003

' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
'
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute,
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the
' Department of Energy (DOE).  All rights in the computer software are reserved
' by DOE on behalf of the United States Government and the Contractor as
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS
' SOFTWARE.  This notice including this sentence must appear on any copies of
' this computer software.

'
' Last modified March 28, 2017

Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic

Public Class clsParseCommandLine

    ''' <summary>
    ''' Default switch char
    ''' </summary>
    Public Const DEFAULT_SWITCH_CHAR As Char = "/"c

    ''' <summary>
    ''' Alternate switch char
    ''' </summary>
    Public Const ALTERNATE_SWITCH_CHAR As Char = "-"c

    ''' <summary>
    ''' Default character between the switch name and a value to associate with the parameter
    ''' </summary>
    Public Const DEFAULT_SWITCH_PARAM_CHAR As Char = ":"c

    Private ReadOnly mSwitches As New Dictionary(Of String, String)
    Private ReadOnly mNonSwitchParameters As New List(Of String)

    Private mShowHelp As Boolean = False

    ''' <summary>
    ''' If true, we need to show the syntax to the user due to a switch error, invalid switch, or the presence of /? or /help
    ''' </summary>
    Public ReadOnly Property NeedToShowHelp() As Boolean
        Get
            Return mShowHelp
        End Get
    End Property

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Number of switches
    ''' </summary>
    Public ReadOnly Property ParameterCount() As Integer
        Get
            Return mSwitches.Count
        End Get
    End Property

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Number of parameters that are not preceded by a switch
    ''' </summary>
    Public ReadOnly Property NonSwitchParameterCount() As Integer
        Get
            Return mNonSwitchParameters.Count
        End Get
    End Property

    ''' <summary>
    ''' Set to true to see extra debug information
    ''' </summary>
    Public ReadOnly Property DebugMode() As Boolean

    Public Sub New(Optional blnDebugMode As Boolean = False)
        DebugMode = blnDebugMode
    End Sub

    ''' <summary>
    ''' Compares the parameter names in objParameterList with the parameters at the command line
    ''' </summary>
    ''' <param name="parameterList">Parameter list</param>
    ''' <returns>True if any of the parameters are not present in parameterList()</returns>
    Public Function InvalidParametersPresent(parameterList As List(Of String)) As Boolean
        Const caseSensitive = False
        Return InvalidParametersPresent(parameterList, caseSensitive)
    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Compares the parameter names in parameterList with the parameters at the command line
    ''' </summary>
    ''' <param name="parameterList">Parameter list</param>
    ''' <returns>True if any of the parameters are not present in parameterList()</returns>
    Public Function InvalidParametersPresent(parameterList() As String) As Boolean
        Const caseSensitive = False
        Return InvalidParametersPresent(parameterList, caseSensitive)
    End Function

    ''' <summary>
    ''' Compares the parameter names in parameterList with the parameters at the command line
    ''' </summary>
    ''' <param name="parameterList">Parameter list</param>
    ''' <param name="caseSensitive">True to perform case-sensitive matching of the parameter name</param>
    ''' <returns>True if any of the parameters are not present in parameterList()</returns>
    Public Function InvalidParametersPresent(parameterList() As String, caseSensitive As Boolean) As Boolean
        If InvalidParameters(parameterList.ToList(), caseSensitive).Count > 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Validate that the user-provided parameters are in the validParameters list
    ''' </summary>
    ''' <param name="validParameters"></param>
    ''' <param name="caseSensitive"></param>
    ''' <returns></returns>
    Public Function InvalidParametersPresent(validParameters As List(Of String), caseSensitive As Boolean) As Boolean

        If InvalidParameters(validParameters, caseSensitive).Count > 0 Then
            Return True
        Else
            Return False
        End If

    End Function

    ''' <summary>
    ''' Retrieve a list of the user-provided parameters that are not in validParameters
    ''' </summary>
    ''' <param name="validParameters"></param>
    ''' <returns></returns>
    Public Function InvalidParameters(validParameters As List(Of String)) As List(Of String)
        Const caseSensitive = False
        Return InvalidParameters(validParameters, caseSensitive)
    End Function

    ''' <summary>
    ''' Retrieve a list of the user-provided parameters that are not in validParameters
    ''' </summary>
    ''' <param name="validParameters"></param>
    ''' <param name="caseSensitive"></param>
    ''' <returns></returns>
    Public Function InvalidParameters(validParameters As List(Of String), caseSensitive As Boolean) As List(Of String)
        Dim invalidParams = New List(Of String)

        Try

            ' Find items in mSwitches whose keys are not in validParameters)
            For Each item As KeyValuePair(Of String, String) In mSwitches

                Dim itemKey As String = item.Key
                Dim intMatchCount As Integer

                If caseSensitive Then
                    intMatchCount = (From validItem In validParameters Where validItem = itemKey).Count
                Else
                    intMatchCount = (From validItem In validParameters Where String.Equals(validItem, itemKey, StringComparison.InvariantCultureIgnoreCase)).Count
                End If

                If intMatchCount = 0 Then
                    invalidParams.Add(item.Key)
                End If
            Next

        Catch ex As Exception
            Throw New Exception("Error in InvalidParameters", ex)
        End Try

        Return invalidParams

    End Function

    ''' <summary>
    ''' Look for parameter on the command line
    ''' </summary>
    ''' <param name="paramName">Parameter name</param>
    ''' <returns>True if present, otherwise false</returns>
    ''' <remarks>Does not work for /? or /help -- for those, use .NeedToShowHelp</remarks>
    Public Function IsParameterPresent(paramName As String) As Boolean
        Dim paramValue As String = String.Empty
        Const caseSensitive = False
        Return RetrieveValueForParameter(paramName, paramValue, caseSensitive)
    End Function

    ''' <summary>
    ''' Parse the parameters and switches at the command line; uses / for the switch character and : for the switch parameter character
    ''' </summary>
    ''' <returns>Returns True if any command line parameters were found; otherwise false</returns>
    ''' <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
    Public Function ParseCommandLine() As Boolean
        Return ParseCommandLine(DEFAULT_SWITCH_CHAR, DEFAULT_SWITCH_PARAM_CHAR)
    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Parse the parameters and switches at the command line; uses : for the switch parameter character
    ''' </summary>
    ''' <returns>Returns True if any command line parameters were found; otherwise false</returns>
    ''' <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
    Public Function ParseCommandLine(switchStartChar As Char) As Boolean
        Return ParseCommandLine(switchStartChar, DEFAULT_SWITCH_PARAM_CHAR)
    End Function

    ''' <summary>
    ''' Parse the parameters and switches at the command line
    ''' </summary>
    ''' <param name="switchStartChar"></param>
    ''' <param name="switchParameterChar"></param>
    ''' <returns>Returns True if any command line parameters were found; otherwise false</returns>
    ''' <remarks>If /? or /help is found, then returns False and sets mShowHelp to True</remarks>
    Public Function ParseCommandLine(switchStartChar As Char, switchParameterChar As Char) As Boolean
        ' Returns True if any command line parameters were found
        ' Otherwise, returns false
        '
        ' If /? or /help is found, then returns False and sets mShowHelp to True

        Dim strCmdLine As String

        mSwitches.Clear()
        mNonSwitchParameters.Clear()

        Try
            Try
                ' .CommandLine() returns the full command line
                strCmdLine = Environment.CommandLine()

                ' .GetCommandLineArgs splits the command line at spaces, though it keeps text between double quotes together
                ' Note that .NET will strip out the starting and ending double quote if the user provides a parameter like this:
                ' MyProgram.exe "C:\Program Files\FileToProcess"
                '
                ' In this case, paramList(1) will not have a double quote at the start but it will have a double quote at the end:
                '  paramList(1) = C:\Program Files\FileToProcess"

                ' One very odd feature of Environment.GetCommandLineArgs() is that if the command line looks like this:
                '    MyProgram.exe "D:\My Folder\Subfolder\" /O:D:\OutputFolder
                ' Then paramList will have:
                '    paramList(1) = D:\My Folder\Subfolder" /O:D:\OutputFolder
                '
                ' To avoid this problem instead specify the command line as:
                '    MyProgram.exe "D:\My Folder\Subfolder" /O:D:\OutputFolder
                ' which gives:
                '    paramList(1) = D:\My Folder\Subfolder
                '    paramList(2) = /O:D:\OutputFolder
                '
                ' Due to the idiosyncrasies of .GetCommandLineArgs, we will instead use SplitCommandLineParams to do the splitting
                ' paramList = Environment.GetCommandLineArgs()

            Catch ex As Exception
                ' In .NET 1.x, programs would fail if called from a network share
                ' This appears to be fixed in .NET 2.0 and above
                ' If an exception does occur here, we'll show the error message at the console, then sleep for 2 seconds

                Console.WriteLine("------------------------------------------------------------------------------")
                Console.WriteLine("This program cannot be run from a network share.  Please map a drive to the")
                Console.WriteLine(" network share you are currently accessing or copy the program files and")
                Console.WriteLine(" required DLL's to your local computer.")
                Console.WriteLine(" Exception: " & ex.Message)
                Console.WriteLine("------------------------------------------------------------------------------")

                PauseAtConsole(5000, 1000)

                mShowHelp = True
                Return False
            End Try

            If DebugMode Then
                Console.WriteLine()
                Console.WriteLine("Debugging command line parsing")
                Console.WriteLine()
            End If

            Dim paramList = SplitCommandLineParams(strCmdLine)

            If DebugMode Then
                Console.WriteLine()
            End If

            If String.IsNullOrWhiteSpace(strCmdLine) Then
                Return False
            ElseIf strCmdLine.IndexOf(switchStartChar & "?", StringComparison.Ordinal) > 0 OrElse strCmdLine.ToLower().IndexOf(switchStartChar & "help", StringComparison.Ordinal) > 0 Then
                mShowHelp = True
                Return False
            End If

            ' Parse the command line
            ' Note that paramList(0) is the path to the Executable for the calling program
            For intIndex = 1 To paramList.Length - 1

                If paramList(intIndex).Length > 0 Then
                    Dim paramName = paramList(intIndex).TrimStart(" "c)
                    Dim paramValue = String.Empty
                    Dim isSwitchParam As Boolean

                    If paramName.StartsWith(switchStartChar) Then
                        isSwitchParam = True
                    ElseIf paramName.StartsWith(ALTERNATE_SWITCH_CHAR) OrElse paramName.StartsWith(DEFAULT_SWITCH_CHAR) Then
                        isSwitchParam = True
                    Else
                        ' Parameter doesn't start with switchStartChar or / or -
                        isSwitchParam = False
                    End If

                    If isSwitchParam Then
                        ' Look for switchParameterChar in paramList(intIndex)
                        Dim charIndex = paramList(intIndex).IndexOf(switchParameterChar)

                        If charIndex >= 0 Then
                            ' Parameter is of the form /I:MyParam or /I:"My Parameter" or -I:"My Parameter" or /MyParam:Setting
                            paramValue = paramName.Substring(charIndex + 1).Trim()

                            ' Remove any starting and ending quotation marks
                            paramValue = paramValue.Trim(""""c)

                            paramName = paramName.Substring(0, charIndex)
                        Else
                            ' Parameter is of the form /S or -S
                        End If

                        ' Remove the switch character from paramName
                        paramName = paramName.Substring(1).Trim()

                        If DebugMode Then
                            Console.WriteLine("SwitchParam: " & paramName & "=" & paramValue)
                        End If

                        ' Note: .Item() will add paramName if it doesn't exist (which is normally the case)
                        mSwitches.Item(paramName) = paramValue
                    Else
                        ' Non-switch parameter since switchParameterChar was not found and does not start with switchStartChar

                        ' Remove any starting and ending quotation marks
                        paramName = paramName.Trim(""""c)

                        If DebugMode Then
                            Console.WriteLine("NonSwitchParam " & mNonSwitchParameters.Count & ": " & paramName)
                        End If

                        mNonSwitchParameters.Add(paramName)
                    End If

                End If
            Next

        Catch ex As Exception
            Throw New Exception("Error in ParseCommandLine", ex)
        End Try

        If DebugMode Then
            Console.WriteLine()
            Console.WriteLine("Switch Count = " & mSwitches.Count)
            Console.WriteLine("NonSwitch Count = " & mNonSwitchParameters.Count)
            Console.WriteLine()
        End If

        If mSwitches.Count + mNonSwitchParameters.Count > 0 Then
            Return True
        Else
            Return False
        End If

    End Function

    ''' <summary>
    ''' Pause the program for the specified number of milliseconds, displaying a period at a set interval while paused
    ''' </summary>
    ''' <param name="millisecondsToPause">Milliseconds to pause; default 5 seconds</param>
    ''' <param name="millisecondsBetweenDots">Seconds between each period; default 1 second</param>
    Public Shared Sub PauseAtConsole(millisecondsToPause As Integer, millisecondsBetweenDots As Integer)

        Dim totalIterations As Integer

        Console.WriteLine()
        Console.Write("Continuing in " & (millisecondsToPause / 1000.0).ToString("0") & " seconds ")

        Try
            If millisecondsBetweenDots = 0 Then millisecondsBetweenDots = millisecondsToPause

            totalIterations = CInt(Math.Round(millisecondsToPause / CDbl(millisecondsBetweenDots), 0))
        Catch ex As Exception
            totalIterations = 1
        End Try

        Dim iteration = 0
        Do
            Console.Write("."c)

            Threading.Thread.Sleep(millisecondsBetweenDots)

            iteration += 1
        Loop While iteration < totalIterations

        Console.WriteLine()

    End Sub

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Returns the value of the non-switch parameter at the given index
    ''' </summary>
    ''' <param name="parameterIndex">Parameter index</param>
    ''' <returns>The value of the parameter at the given index; empty string if no value or invalid index</returns>
    Public Function RetrieveNonSwitchParameter(parameterIndex As Integer) As String
        Dim paramValue As String = String.Empty

        If parameterIndex < mNonSwitchParameters.Count Then
            paramValue = mNonSwitchParameters(parameterIndex)
        End If

        If paramValue Is Nothing Then
            Return String.Empty
        End If

        Return paramValue

    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Returns the parameter at the given index
    ''' </summary>
    ''' <param name="parameterIndex">Parameter index</param>
    ''' <param name="paramName">Parameter name (output)</param>
    ''' <param name="paramValue">Value associated with the parameter; empty string if no value (output)</param>
    ''' <returns></returns>
    Public Function RetrieveParameter(parameterIndex As Integer, <Out()> ByRef paramName As String, <Out()> ByRef paramValue As String) As Boolean

        Try
            paramName = String.Empty
            paramValue = String.Empty

            If parameterIndex < mSwitches.Count Then
                Dim iEnum As Dictionary(Of String, String).Enumerator = mSwitches.GetEnumerator()

                Dim switchIndex = 0
                Do While iEnum.MoveNext()
                    If switchIndex = parameterIndex Then
                        paramName = iEnum.Current.Key
                        paramValue = iEnum.Current.Value
                        Return True
                    End If
                    switchIndex += 1
                Loop
            Else
                Return False
            End If
        Catch ex As Exception
            Throw New Exception("Error in RetrieveParameter", ex)
        End Try

        Return False

    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Look for parameter on the command line and returns its value in paramValue
    ''' </summary>
    ''' <param name="paramName">Parameter name</param>
    ''' <param name="paramValue">Value associated with the parameter; empty string if no value (output)</param>
    ''' <returns>True if present, otherwise false</returns>
    Public Function RetrieveValueForParameter(paramName As String, <Out()> ByRef paramValue As String) As Boolean
        Return RetrieveValueForParameter(paramName, paramValue, False)
    End Function

    ''' <summary>
    ''' Look for parameter on the command line and returns its value in paramValue
    ''' </summary>
    ''' <param name="paramName">Parameter name</param>
    ''' <param name="paramValue">Value associated with the parameter; empty string if no value (output)</param>
    ''' <param name="caseSensitive">True to perform case-sensitive matching of the parameter name</param>
    ''' <returns>True if present, otherwise false</returns>
    Public Function RetrieveValueForParameter(paramName As String, <Out()> ByRef paramValue As String, caseSensitive As Boolean) As Boolean

        Try
            paramValue = String.Empty

            If caseSensitive Then
                If mSwitches.ContainsKey(paramName) Then
                    paramValue = CStr(mSwitches(paramName))
                    Return True
                Else
                    Return False
                End If
            Else
                Dim query = (From item In mSwitches Where String.Equals(item.Key, paramName, StringComparison.InvariantCultureIgnoreCase) Select item).ToList()

                If query.Count = 0 Then
                    Return False
                End If

                paramValue = query.FirstOrDefault.Value
                Return True

            End If
        Catch ex As Exception
            Throw New Exception("Error in RetrieveValueForParameter", ex)
        End Try

    End Function

    Private Function SplitCommandLineParams(strCmdLine As String) As String()
        Dim paramList As New List(Of String)

        Dim indexStart = 0
        Dim indexEnd = 0

        Try
            If Not String.IsNullOrEmpty(strCmdLine) Then

                ' Make sure the command line doesn't have any carriage return or linefeed characters
                strCmdLine = strCmdLine.Replace(ControlChars.CrLf, " ")
                strCmdLine = strCmdLine.Replace(ControlChars.Cr, " ")
                strCmdLine = strCmdLine.Replace(ControlChars.Lf, " ")

                Dim insideDoubleQuotes = False

                Do While indexStart < strCmdLine.Length
                    ' Step through the characters to find the next space
                    ' However, if we find a double quote, then stop checking for spaces

                    If strCmdLine.Chars(indexEnd) = """"c Then
                        insideDoubleQuotes = Not insideDoubleQuotes
                    End If

                    If Not insideDoubleQuotes OrElse indexEnd = strCmdLine.Length - 1 Then
                        If strCmdLine.Chars(indexEnd) = " "c OrElse indexEnd = strCmdLine.Length - 1 Then
                            ' Found the end of a parameter
                            Dim paramName = strCmdLine.Substring(indexStart, indexEnd - indexStart + 1).TrimEnd(" "c)

                            If paramName.StartsWith(""""c) Then
                                paramName = paramName.Substring(1)
                            End If

                            If paramName.EndsWith(""""c) Then
                                paramName = paramName.Substring(0, paramName.Length - 1)
                            End If

                            If Not String.IsNullOrEmpty(paramName) Then
                                If DebugMode Then
                                    Console.WriteLine("Param " & paramList.Count & ": " & paramName)
                                End If
                                paramList.Add(paramName)
                            End If

                            indexStart = indexEnd + 1
                        End If
                    End If

                    indexEnd += 1
                Loop

            End If

        Catch ex As Exception
            Throw New Exception("Error in SplitCommandLineParams", ex)
        End Try

        Return paramList.ToArray()

    End Function
End Class
