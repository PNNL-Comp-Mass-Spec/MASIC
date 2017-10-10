Option Strict On

Imports System.Threading
Imports PRISM
Imports ProgressFormNET

' See clsMASIC for a program description
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started October 11, 2003
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://omics.pnl.gov/ or http://www.sysbio.org/resources/staff/ or http://panomics.pnnl.gov/
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

Public Module modMain

    Public Const PROGRAM_DATE As String = "October 10, 2017"

    Private mInputFilePath As String
    Private mOutputFolderPath As String             ' Optional
    Private mParameterFilePath As String            ' Optional
    Private mOutputFolderAlternatePath As String                ' Optional
    Private mRecreateFolderHierarchyInAlternatePath As Boolean  ' Optional

    Private mDatasetLookupFilePath As String        ' Optional
    Private mDatasetNumber As Integer

    Private mRecurseFolders As Boolean
    Private mRecurseFoldersMaxLevels As Integer

    Private mLogMessagesToFile As Boolean
    Private mLogFilePath As String = String.Empty
    Private mLogFolderPath As String = String.Empty

    Private mMASICStatusFilename As String = String.Empty
    Private mQuietMode As Boolean

    Private mMASIC As clsMASIC
    Private mProgressForm As frmProgress

    Private mLastProgressReportTime As DateTime
    Private mLastProgressReportValue As Integer

    ''Private Sub InitializeTraceLogFile(strUserDefinedLogFilePath As String)
    ''    Dim strTraceFilePath As String

    ''    Static blnTraceFileEnabled As Boolean

    ''    If Not blnTraceFileEnabled Then
    ''        Try
    ''            If strUserDefinedLogFilePath Is Nothing Then strUserDefinedLogFilePath = String.Empty

    ''            If strUserDefinedLogFilePath.Length = 0 Then
    ''                strTraceFilePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    ''                strTraceFilePath = Path.Combine(strTraceFilePath, "MASIC_Log_" & System.DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") & ".txt")
    ''            Else
    ''                strTraceFilePath = String.Copy(strUserDefinedLogFilePath)
    ''            End If

    ''            Trace.Listeners.Add(New TextWriterTraceListener(strTraceFilePath))
    ''            Trace.AutoFlush = True

    ''            blnTraceFileEnabled = True
    ''        Catch ex As Exception
    ''            If strUserDefinedLogFilePath.Length > 0 Then
    ''                ' Error using the user-defined path
    ''                ' Call this function using a blank string so that a default file is created
    ''                InitializeTraceLogFile(String.Empty)
    ''            End If

    ''        End Try

    ''    End If

    ''End Sub

    Public Function Main() As Integer
        ' Returns 0 if no error, error code if an error

        Dim objParseCommandLine As New clsParseCommandLine
        Dim proceed As Boolean

        mInputFilePath = String.Empty
        mOutputFolderPath = String.Empty
        mParameterFilePath = String.Empty

        mRecurseFolders = False
        mRecurseFoldersMaxLevels = 0

        mQuietMode = False
        mLogMessagesToFile = False
        mLogFilePath = String.Empty

        Try
            proceed = False
            If objParseCommandLine.ParseCommandLine Then
                If SetOptionsUsingCommandLineParameters(objParseCommandLine) Then proceed = True
            End If

            If (objParseCommandLine.ParameterCount + objParseCommandLine.NonSwitchParameterCount = 0) And Not objParseCommandLine.NeedToShowHelp Then
                ShowGUI()
                Return 0
            End If

            If Not proceed OrElse objParseCommandLine.NeedToShowHelp OrElse mInputFilePath.Length = 0 Then
                ShowProgramHelp()
                Return -1
            End If

            mMASIC = New clsMASIC()
            RegisterEvents(mMASIC)

            mMASIC.Options.DatasetLookupFilePath = mDatasetLookupFilePath
            mMASIC.Options.SICOptions.DatasetNumber = mDatasetNumber

            If Not mMASICStatusFilename Is Nothing AndAlso mMASICStatusFilename.Length > 0 Then
                mMASIC.Options.MASICStatusFilename = mMASICStatusFilename
            End If

            mMASIC.ShowMessages = Not mQuietMode
            mMASIC.LogMessagesToFile = mLogMessagesToFile
            mMASIC.LogFilePath = mLogFilePath
            mMASIC.LogFolderPath = mLogFolderPath

            If mMASIC.ShowMessages Then
                mProgressForm = New frmProgress

                mProgressForm.InitializeProgressForm("Parsing " & Path.GetFileName(mInputFilePath), 0, 100, False, True)
                mProgressForm.InitializeSubtask("", 0, 100, False)
                mProgressForm.ResetKeyPressAbortProcess()
                mProgressForm.Show()
                Application.DoEvents()

            End If

            Dim returnCode As Integer

            If mRecurseFolders Then
                If mMASIC.ProcessFilesAndRecurseFolders(mInputFilePath, mOutputFolderPath, mOutputFolderAlternatePath, mRecreateFolderHierarchyInAlternatePath, mParameterFilePath, mRecurseFoldersMaxLevels) Then
                    returnCode = 0
                Else
                    returnCode = mMASIC.ErrorCode
                End If
            Else
                If mMASIC.ProcessFilesWildcard(mInputFilePath, mOutputFolderPath, mParameterFilePath) Then
                    returnCode = 0
                Else
                    returnCode = mMASIC.ErrorCode
                    If returnCode <> 0 AndAlso Not mQuietMode Then
                        Console.WriteLine("Error while processing: " & mMASIC.GetErrorMessage())
                    End If
                End If
            End If

            Return returnCode

        Catch ex As Exception
            ShowErrorMessage("Error occurred in modMain->Main: " & Environment.NewLine & ex.Message)
            Return -1
        Finally
            If Not mProgressForm Is Nothing Then
                mProgressForm.HideForm()
                mProgressForm = Nothing
            End If
        End Try

    End Function

    Private Sub DisplayProgressPercent(percentComplete As Integer, addCarriageReturn As Boolean)
        If addCarriageReturn Then
            Console.WriteLine()
        End If
        If percentComplete > 100 Then percentComplete = 100
        Console.Write("Processing: " & percentComplete.ToString() & "% ")
        If addCarriageReturn Then
            Console.WriteLine()
        End If
    End Sub

    Private Function GetAppVersion() As String
        Return clsProcessFilesBaseClass.GetAppVersion(PROGRAM_DATE)
    End Function

    Private Sub RegisterEvents(oClass As clsMASIC)
        'AddHandler oClass.MessageEvent, AddressOf MessageEventHandler
        'AddHandler oClass.ErrorEvent, AddressOf ErrorEventHandler
        'AddHandler oClass.WarningEvent, AddressOf WarningEventHandler

        AddHandler oClass.ProgressChanged, AddressOf ProgressChangedHandler
        AddHandler oClass.ProgressResetKeypressAbort, AddressOf ProgressResetKeypressAbortHandler
        AddHandler oClass.ProgressSubtaskChanged, AddressOf ProgressSubtaskChangedHandler
    End Sub

    Private Function SetOptionsUsingCommandLineParameters(objParseCommandLine As clsParseCommandLine) As Boolean
        ' Returns True if no problems; otherwise, returns false

        Dim value As String = String.Empty
        Dim lstValidParameters = New List(Of String) From {"I", "O", "P", "D", "S", "A", "R", "L", "SF", "LogFolder", "Q"}
        Dim intValue As Integer

        Try
            ' Make sure no invalid parameters are present
            If objParseCommandLine.InvalidParametersPresent(lstValidParameters) Then
                ShowErrorMessage("Invalid commmand line parameters",
                  (From item In objParseCommandLine.InvalidParameters(lstValidParameters) Select "/" + item).ToList())
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

                    If .RetrieveValueForParameter("O", value) Then mOutputFolderPath = value
                    If .RetrieveValueForParameter("P", value) Then mParameterFilePath = value
                    If .RetrieveValueForParameter("D", value) Then
                        If Not IsNumeric(value) AndAlso Not value Is Nothing Then
                            ' Assume the user specified a dataset number lookup file (comma or tab delimited file specifying the dataset number for each input file)
                            mDatasetLookupFilePath = value
                            mDatasetNumber = 0
                        Else
                            mDatasetNumber = CInt(value)
                        End If
                    End If

                    If .RetrieveValueForParameter("S", value) Then
                        mRecurseFolders = True
                        If Integer.TryParse(value, intValue) Then
                            mRecurseFoldersMaxLevels = intValue
                        End If
                    End If
                    If .RetrieveValueForParameter("A", value) Then mOutputFolderAlternatePath = value
                    If .IsParameterPresent("R") Then mRecreateFolderHierarchyInAlternatePath = True

                    If .RetrieveValueForParameter("L", value) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(value) Then
                            mLogFilePath = value.Trim(""""c)
                        End If
                    End If

                    If .RetrieveValueForParameter("SF", value) Then
                        mMASICStatusFilename = value
                    End If

                    If .RetrieveValueForParameter("LogFolder", value) Then
                        mLogMessagesToFile = True
                        If Not String.IsNullOrEmpty(value) Then
                            mLogFolderPath = value
                        End If
                    End If
                    If .IsParameterPresent("Q") Then mQuietMode = True
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
        Dim objFormMain As frmMain

        Application.EnableVisualStyles()
        Application.DoEvents()

        objFormMain = New frmMain

        ' The following call is needed because the .ShowDialog() call is inexplicably increasing the size of the form
        objFormMain.SetHeightAdjustForce(objFormMain.Height)

        objFormMain.ShowDialog()

    End Sub

    Private Sub ShowProgramHelp()

        Try

            Console.WriteLine("This program will read a Finnigan LCQ .RAW file or Agilent LC/MSD .CDF/.MGF file combo and create a selected ion chromatogram (SIC) for each parent ion.")
            Console.WriteLine()

            Console.WriteLine("Program syntax:" & Environment.NewLine & Path.GetFileName(clsProcessFilesBaseClass.GetAppPath()))
            Console.WriteLine(" /I:InputFilePath.raw [/O:OutputFolderPath]")
            Console.WriteLine(" [/P:ParamFilePath] [/D:DatasetNumber or DatasetLookupFilePath] ")
            Console.WriteLine(" [/S:[MaxLevel]] [/A:AlternateOutputFolderPath] [/R]")
            Console.WriteLine(" [/L:[LogFilePath]] [/LogFolder:LogFolderPath] [/SF:StatusFileName] [/Q]")
            Console.WriteLine()

            Console.WriteLine("The input file path can contain the wildcard character *")
            Console.WriteLine("The output folder name is optional.  If omitted, the output files will be created in the same folder as the input file.  If included, then a subfolder is created with the name OutputFolderName.")
            Console.WriteLine("The param file switch is optional.  If supplied, it should point to a valid MASIC XML parameter file.  If omitted, defaults are used.")
            Console.WriteLine()
            Console.WriteLine("The /D switch can be used to specify the dataset number of the input file; if omitted, 0 will be used")
            Console.WriteLine("Alternatively, a lookup file can be specified with the /D switch (useful if processing multiple files using * or /S)")
            Console.WriteLine()
            Console.WriteLine("Use /S to process all valid files in the input folder and subfolders. Include a number after /S (like /S:2) to limit the level of subfolders to examine.")
            Console.WriteLine("When using /S, you can redirect the output of the results using /A.")
            Console.WriteLine("When using /S, you can use /R to re-create the input folder hierarchy in the alternate output folder (if defined).")
            Console.WriteLine()
            Console.WriteLine("Use /L to specify that a log file should be created.  Use /L:LogFilePath to specify the name (or full path) for the log file.")
            Console.WriteLine("Use /SF to specify the name to use for the Masic Status file (default is " & clsMASICOptions.DEFAULT_MASIC_STATUS_FILE_NAME & ").")
            Console.WriteLine("The optional /Q switch will suppress all error messages.")
            Console.WriteLine()

            Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003")
            Console.WriteLine("Version: " & GetAppVersion())

            Console.WriteLine()

            Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com")
            Console.WriteLine("Website: http://omics.pnl.gov/ or http://panomics.pnnl.gov/")
            Console.WriteLine()

            ' Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
            Thread.Sleep(750)

        Catch ex As Exception
            ShowErrorMessage("Error displaying the program syntax: " & ex.Message)
        End Try

    End Sub

    Private Sub ProgressChangedHandler(taskDescription As String, percentComplete As Single)
        Const PERCENT_REPORT_INTERVAL = 25
        Const PROGRESS_DOT_INTERVAL_MSEC = 250

        If Not mProgressForm Is Nothing Then
            mProgressForm.UpdateCurrentTask(mMASIC.ProgressStepDescription)
            mProgressForm.UpdateProgressBar(percentComplete)
            If mProgressForm.KeyPressAbortProcess Then
                mMASIC.AbortProcessingNow()
            End If
            Application.DoEvents()

        Else

            If percentComplete >= mLastProgressReportValue Then
                If mLastProgressReportValue > 0 Then
                    Console.WriteLine()
                End If
                DisplayProgressPercent(mLastProgressReportValue, False)
                mLastProgressReportValue += PERCENT_REPORT_INTERVAL
                mLastProgressReportTime = DateTime.UtcNow
            Else
                If DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC Then
                    mLastProgressReportTime = DateTime.UtcNow
                    Console.Write(".")
                End If
            End If
        End If

    End Sub

    Private Sub ProgressResetKeypressAbortHandler()
        If Not mProgressForm Is Nothing Then
            mProgressForm.ResetKeyPressAbortProcess()
        End If
    End Sub

    Private Sub ProgressSubtaskChangedHandler()
        If Not mProgressForm Is Nothing Then
            mProgressForm.UpdateCurrentSubTask(mMASIC.SubtaskDescription)
            mProgressForm.UpdateSubtaskProgressBar(mMASIC.SubtaskProgressPercentComplete)
            If mProgressForm.KeyPressAbortProcess Then
                mMASIC.AbortProcessingNow()
            End If
            Application.DoEvents()
        End If
    End Sub

End Module
