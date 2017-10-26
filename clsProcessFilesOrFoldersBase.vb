Option Strict On

Imports System.IO
Imports System.Threading
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports PRISM

''' <summary>
''' This class contains functions used by both clsProcessFilesBaseClass and clsProcessFoldersBaseClass
''' </summary>
''' <remarks>
''' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
''' Created in October 2013
''' </remarks>
Public MustInherit Class clsProcessFilesOrFoldersBase
    Inherits clsEventNotifier

#Region "Constants and Enums"

    Protected Enum eMessageTypeConstants
        Normal = 0
        ErrorMsg = 1
        Warning = 2
    End Enum

#End Region

#Region "Classwide Variables"

    Protected mFileDate As String

    Protected mLogFileUsesDateStamp As Boolean = True
    Protected mLogFilePath As String
    Protected mLogFile As StreamWriter

    ' This variable is updated when CleanupFilePaths() is called
    Protected mOutputFolderPath As String

    Private mLastMessage As String = String.Empty
    Private mLastReportTime As DateTime = DateTime.UtcNow
    Private mLastErrorShown As DateTime = DateTime.MinValue

    Public Event ProgressReset()
    Public Event ProgressComplete()

    Protected mProgressStepDescription As String

    ''' <summary>
    ''' Percent complete, value between 0 and 100, but can contain decimal percentage values
    ''' </summary>
    Protected mProgressPercentComplete As Single

    ''' <summary>
    ''' Keys in this dictionary are the log type and message (separated by an underscore), values are the most recent time the string was logged
    ''' </summary>
    ''' <remarks></remarks>
    Private ReadOnly mLogDataCache As Dictionary(Of String, DateTime)

    Private Const MAX_LOGDATA_CACHE_SIZE As Integer = 100000

#End Region

#Region "Interface Functions"

    Public Property AbortProcessing As Boolean

    Public ReadOnly Property FileVersion As String
        Get
            Return GetVersionForExecutingAssembly()
        End Get
    End Property

    Public ReadOnly Property FileDate As String
        Get
            Return mFileDate
        End Get
    End Property

    Public Property LogFilePath As String
        Get
            Return mLogFilePath
        End Get
        Set
            If Value Is Nothing Then Value = String.Empty
            mLogFilePath = Value
        End Set
    End Property

    ''' <summary>
    ''' Log folder path (ignored if LogFilePath is rooted)
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>
    ''' If blank, mOutputFolderPath will be used; if mOutputFolderPath is also blank, the log is created in the same folder as the executing assembly
    ''' </remarks>
    Public Property LogFolderPath As String

    Public Property LogMessagesToFile As Boolean

    Public Overridable ReadOnly Property ProgressStepDescription As String
        Get
            Return mProgressStepDescription
        End Get
    End Property

    ''' <summary>
    ''' Percent complete, value between 0 and 100, but can contain decimal percentage values
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ProgressPercentComplete As Single
        Get
            Return CType(Math.Round(mProgressPercentComplete, 2), Single)
        End Get
    End Property

    Public Property ShowMessages As Boolean = True

#End Region

    ''' <summary>
    ''' Constructor
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub New()
        mProgressStepDescription = String.Empty

        mOutputFolderPath = String.Empty
        LogFolderPath = String.Empty
        mLogFilePath = String.Empty

        mLogDataCache = New Dictionary(Of String, DateTime)
    End Sub

    Public Overridable Sub AbortProcessingNow()
        AbortProcessing = True
    End Sub

    Protected MustOverride Sub CleanupPaths(ByRef inputFileOrFolderPath As String, ByRef outputFolderPath As String)

    Public Sub CloseLogFileNow()
        If Not mLogFile Is Nothing Then
            mLogFile.Close()
            mLogFile = Nothing

            GarbageCollectNow()
            Thread.Sleep(100)
        End If
    End Sub

    ''' <summary>
    ''' Verifies that the specified .XML settings file exists in the user's local settings folder
    ''' </summary>
    ''' <param name="applicationName">Application name</param>
    ''' <param name="settingsFileName">Settings file name</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function CreateSettingsFileIfMissing(applicationName As String, settingsFileName As String) As Boolean
        Dim settingsFilePathLocal = GetSettingsFilePathLocal(applicationName, settingsFileName)

        Return CreateSettingsFileIfMissing(settingsFilePathLocal)

    End Function

    ''' <summary>
    ''' Verifies that the specified .XML settings file exists in the user's local settings folder
    ''' </summary>
    ''' <param name="settingsFilePathLocal">Full path to the local settings file, for example C:\Users\username\AppData\Roaming\AppName\SettingsFileName.xml</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function CreateSettingsFileIfMissing(settingsFilePathLocal As String) As Boolean

        Try
            If Not File.Exists(settingsFilePathLocal) Then
                Dim fiMasterSettingsFile As FileInfo
                fiMasterSettingsFile = New FileInfo(Path.Combine(GetAppFolderPath(), Path.GetFileName(settingsFilePathLocal)))

                If fiMasterSettingsFile.Exists Then
                    fiMasterSettingsFile.CopyTo(settingsFilePathLocal)
                End If
            End If

        Catch ex As Exception
            ' Ignore errors, but return false
            Return False
        End Try

        Return True

    End Function

    ''' <summary>
    ''' Perform garbage collection
    ''' </summary>
    ''' <remarks></remarks>
    Public Shared Sub GarbageCollectNow()
        Const maxWaitTimeMSec = 1000
        GarbageCollectNow(maxWaitTimeMSec)
    End Sub

    ''' <summary>
    ''' Perform garbage collection
    ''' </summary>
    ''' <param name="maxWaitTimeMSec"></param>
    ''' <remarks></remarks>
    Public Shared Sub GarbageCollectNow(maxWaitTimeMSec As Integer)
        Const THREAD_SLEEP_TIME_MSEC = 100

        If maxWaitTimeMSec < 100 Then maxWaitTimeMSec = 100
        If maxWaitTimeMSec > 5000 Then maxWaitTimeMSec = 5000

        Thread.Sleep(100)

        Try
            Dim gcThread As New Thread(AddressOf GarbageCollectWaitForGC)
            gcThread.Start()

            Dim totalThreadWaitTimeMsec = 0
            While gcThread.IsAlive AndAlso totalThreadWaitTimeMsec < maxWaitTimeMSec
                Thread.Sleep(THREAD_SLEEP_TIME_MSEC)
                totalThreadWaitTimeMsec += THREAD_SLEEP_TIME_MSEC
            End While
            If gcThread.IsAlive Then gcThread.Abort()

        Catch ex As Exception
            ' Ignore errors here
        End Try

    End Sub

    Private Shared Sub GarbageCollectWaitForGC()
        clsProgRunner.GarbageCollectNow()
    End Sub

    ''' <summary>
    ''' Returns the full path to the folder into which this application should read/write settings file information
    ''' </summary>
    ''' <param name="appName"></param>
    ''' <returns></returns>
    ''' <remarks>For example, C:\Users\username\AppData\Roaming\AppName</remarks>
    Public Shared Function GetAppDataFolderPath(appName As String) As String
        Dim appDataFolder As String

        If String.IsNullOrWhiteSpace(appName) Then
            appName = String.Empty
        End If

        Try
            appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName)
            If Not Directory.Exists(appDataFolder) Then
                Directory.CreateDirectory(appDataFolder)
            End If

        Catch ex As Exception
            ' Error creating the folder, revert to using the system Temp folder
            appDataFolder = Path.GetTempPath()
        End Try

        Return appDataFolder

    End Function

    ''' <summary>
    ''' Returns the full path to the folder that contains the currently executing .Exe or .Dll
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetAppFolderPath() As String
        ' Could use Application.StartupPath, but .GetExecutingAssembly is better
        Return Path.GetDirectoryName(GetAppPath())
    End Function

    ''' <summary>
    ''' Returns the full path to the executing .Exe or .Dll
    ''' </summary>
    ''' <returns>File path</returns>
    ''' <remarks></remarks>
    Public Shared Function GetAppPath() As String
        Return Assembly.GetExecutingAssembly().Location
    End Function

    ''' <summary>
    ''' Returns the .NET assembly version followed by the program date
    ''' </summary>
    ''' <param name="programDate"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetAppVersion(programDate As String) As String
        Return Assembly.GetExecutingAssembly().GetName().Version.ToString() & " (" & programDate & ")"
    End Function

    Public MustOverride Function GetErrorMessage() As String

    Private Function GetVersionForExecutingAssembly() As String

        Dim version As String

        Try
            version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        Catch ex As Exception
            version = "??.??.??.??"
        End Try

        Return version

    End Function

    ''' <summary>
    ''' Returns the full path to this application's local settings file
    ''' </summary>
    ''' <param name="applicationName"></param>
    ''' <param name="settingsFileName"></param>
    ''' <returns></returns>
    ''' <remarks>For example, C:\Users\username\AppData\Roaming\AppName\SettingsFileName.xml</remarks>
    Public Shared Function GetSettingsFilePathLocal(applicationName As String, settingsFileName As String) As String
        Return Path.Combine(GetAppDataFolderPath(applicationName), settingsFileName)
    End Function

    Protected Sub HandleException(baseMessage As String, ex As Exception)
        If String.IsNullOrWhiteSpace(baseMessage) Then
            baseMessage = "Error"
        End If

        If ShowMessages Then
            ' Note that ShowErrorMessage() will call LogMessage()
            ShowErrorMessage(baseMessage & ": " & ex.Message)
        Else
            LogMessage(baseMessage & ": " & ex.Message, eMessageTypeConstants.ErrorMsg)
            Throw New Exception(baseMessage, ex)
        End If

    End Sub

    Private Sub InitializeLogFile(duplicateHoldoffHours As Integer)
        Try
            If String.IsNullOrWhiteSpace(mLogFilePath) Then
                ' Auto-name the log file
                mLogFilePath = Path.GetFileNameWithoutExtension(GetAppPath())
                mLogFilePath &= "_log"

                If mLogFileUsesDateStamp Then
                    mLogFilePath &= "_" & DateTime.Now.ToString("yyyy-MM-dd") & ".txt"
                Else
                    mLogFilePath &= ".txt"
                End If

            End If

            Try
                If LogFolderPath Is Nothing Then LogFolderPath = String.Empty

                If String.IsNullOrWhiteSpace(LogFolderPath) Then
                    ' Log folder is undefined; use mOutputFolderPath if it is defined
                    If Not String.IsNullOrWhiteSpace(mOutputFolderPath) Then
                        LogFolderPath = String.Copy(mOutputFolderPath)
                    End If
                End If

                If LogFolderPath.Length > 0 Then
                    ' Create the log folder if it doesn't exist
                    If Not Directory.Exists(LogFolderPath) Then
                        Directory.CreateDirectory(LogFolderPath)
                    End If
                End If
            Catch ex As Exception
                LogFolderPath = String.Empty
            End Try

            If Not Path.IsPathRooted(mLogFilePath) AndAlso LogFolderPath.Length > 0 Then
                mLogFilePath = Path.Combine(LogFolderPath, mLogFilePath)
            End If

            Dim openingExistingFile = File.Exists(mLogFilePath)

            If (openingExistingFile And mLogDataCache.Count = 0) Then
                UpdateLogDataCache(DateTime.UtcNow.AddHours(-duplicateHoldoffHours))
            End If

            mLogFile = New StreamWriter(New FileStream(mLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read)) With {
                .AutoFlush = True
            }

            If Not openingExistingFile Then
                mLogFile.WriteLine("Date" & ControlChars.Tab &
                 "Type" & ControlChars.Tab &
                 "Message")
            End If

        Catch ex As Exception
            ' Error creating the log file; set mLogMessagesToFile to false so we don't repeatedly try to create it
            LogMessagesToFile = False
            HandleException("Error opening log file", ex)
            ' Note: do not exit this function if an exception occurs
        End Try

    End Sub

    ''' <summary>
    ''' Log a message then raise a Status, Warning, or Error event
    ''' </summary>
    ''' <param name="message"></param>
    ''' <param name="eMessageType"></param>
    ''' <param name="duplicateHoldoffHours"></param>
    ''' <remarks>
    ''' Note that CleanupPaths() will update mOutputFolderPath, which is used here if mLogFolderPath is blank
    ''' Thus, be sure to call CleanupPaths (or update mLogFolderPath) before the first call to LogMessage
    ''' </remarks>
    Protected Sub LogMessage(
      message As String,
      Optional eMessageType As eMessageTypeConstants = eMessageTypeConstants.Normal,
      Optional duplicateHoldoffHours As Integer = 0)

        If mLogFile Is Nothing AndAlso LogMessagesToFile Then
            InitializeLogFile(duplicateHoldoffHours)
        End If

        If Not mLogFile Is Nothing Then
            WriteToLogFile(message, eMessageType, duplicateHoldoffHours)
        End If

        RaiseMessageEvent(message, eMessageType)

    End Sub

    Private Sub RaiseMessageEvent(message As String, eMessageType As eMessageTypeConstants)

        If String.IsNullOrWhiteSpace(message) Then
            Exit Sub
        End If

        If String.Equals(message, mLastMessage) AndAlso
               DateTime.UtcNow.Subtract(mLastReportTime).TotalSeconds < 0.5 Then
            ' Duplicate message; do not raise any events
        Else
            mLastReportTime = DateTime.UtcNow
            mLastMessage = String.Copy(message)

            Select Case eMessageType
                Case eMessageTypeConstants.Normal
                    OnStatusEvent(message)

                Case eMessageTypeConstants.Warning
                    OnWarningEvent(message)

                Case eMessageTypeConstants.ErrorMsg
                    OnErrorEvent(message)

                Case Else
                    OnStatusEvent(message)
            End Select
        End If

    End Sub

    Protected Sub ResetProgress()
        mProgressPercentComplete = 0
        RaiseEvent ProgressReset()
    End Sub

    Protected Sub ResetProgress(description As String)
        UpdateProgress(description, 0)
        RaiseEvent ProgressReset()
    End Sub

    Protected Sub ShowErrorMessage(message As String, duplicateHoldoffHours As Integer)
        ShowErrorMessage(message, allowLogToFile:=True, duplicateHoldoffHours:=duplicateHoldoffHours)
    End Sub

    Protected Sub ShowErrorMessage(message As String, Optional allowLogToFile As Boolean = True, Optional duplicateHoldoffHours As Integer = 0)

        If allowLogToFile Then
            ' Note that LogMessage will call RaiseMessageEvent
            LogMessage(message, eMessageTypeConstants.ErrorMsg, duplicateHoldoffHours)
        Else
            RaiseMessageEvent(message, eMessageTypeConstants.ErrorMsg)
        End If

    End Sub

    Protected Sub ShowMessage(message As String, duplicateHoldoffHours As Integer)
        ShowMessage(message, allowLogToFile:=True, duplicateHoldoffHours:=duplicateHoldoffHours)
    End Sub

    Protected Sub ShowMessage(
      message As String,
      Optional allowLogToFile As Boolean = True,
      Optional duplicateHoldoffHours As Integer = 0,
      Optional eMessageType As eMessageTypeConstants = eMessageTypeConstants.Normal)

        If allowLogToFile Then
            ' Note that LogMessage will call RaiseMessageEvent
            LogMessage(message, eMessageType, duplicateHoldoffHours)
        Else
            RaiseMessageEvent(message, eMessageType)
        End If

    End Sub

    Protected Sub ShowWarning(message As String, Optional duplicateHoldoffHours As Integer = 0)
        ShowMessage(message, allowLogToFile:=True, duplicateHoldoffHours:=duplicateHoldoffHours, eMessageType:=eMessageTypeConstants.Warning)
    End Sub

    Protected Sub ShowWarning(message As String, allowLogToFile As Boolean)
        ShowMessage(message, allowLogToFile, duplicateHoldoffHours:=0, eMessageType:=eMessageTypeConstants.Warning)
    End Sub

    Private Sub TrimLogDataCache()

        If mLogDataCache.Count < MAX_LOGDATA_CACHE_SIZE Then Exit Sub

        Try
            ' Remove entries from mLogDataCache so that the list count is 80% of MAX_LOGDATA_CACHE_SIZE

            ' First construct a list of dates that we can sort to determine the datetime threshold for removal
            Dim lstDates As List(Of Date) = (From entry In mLogDataCache Select entry.Value).ToList()

            ' Sort by date
            lstDates.Sort()

            Dim thresholdIndex = CInt(Math.Floor(mLogDataCache.Count - MAX_LOGDATA_CACHE_SIZE * 0.8))
            If thresholdIndex < 0 Then thresholdIndex = 0

            Dim threshold = lstDates(thresholdIndex)

            ' Construct a list of keys to be removed
            Dim lstKeys As List(Of String) = (From entry In mLogDataCache Where entry.Value <= threshold Select entry.Key).ToList()

            ' Remove each of the keys
            For Each key In lstKeys
                mLogDataCache.Remove(key)
            Next

        Catch ex As Exception
            ' Ignore errors here
        End Try
    End Sub

    Private Sub UpdateLogDataCache(dateThresholdToStore As DateTime)

        Dim reParseLine = New Regex("^([^\t]+)\t([^\t]+)\t(.+)", RegexOptions.Compiled)

        Try
            mLogDataCache.Clear()

            Using srLogFile = New StreamReader(New FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                While Not srLogFile.EndOfStream
                    Dim lineIn = srLogFile.ReadLine()
                    If String.IsNullOrEmpty(lineIn) Then
                        Continue While
                    End If

                    Dim reMatch = reParseLine.Match(lineIn)

                    If Not reMatch.Success Then
                        Continue While
                    End If

                    Dim logTime As DateTime
                    If DateTime.TryParse(reMatch.Groups(1).Value, logTime) Then
                        logTime = logTime.ToUniversalTime()
                        If logTime >= dateThresholdToStore Then
                            Dim key As String = reMatch.Groups(2).Value & "_" & reMatch.Groups(3).Value

                            Try
                                If mLogDataCache.ContainsKey(key) Then
                                    mLogDataCache(key) = logTime
                                Else
                                    mLogDataCache.Add(key, logTime)
                                End If
                            Catch ex As Exception
                                ' Ignore errors here
                            End Try

                        End If
                    End If

                End While
            End Using

            If mLogDataCache.Count > MAX_LOGDATA_CACHE_SIZE Then
                TrimLogDataCache()
            End If

        Catch ex As Exception
            If DateTime.UtcNow.Subtract(mLastErrorShown).TotalSeconds > 10 Then
                mLastErrorShown = DateTime.UtcNow
                Console.WriteLine("Error caching the log file: " & ex.Message)
            End If

        End Try

    End Sub

    Protected Sub UpdateProgress(description As String)
        UpdateProgress(description, mProgressPercentComplete)
    End Sub

    Protected Sub UpdateProgress(sngPercentComplete As Single)
        UpdateProgress(ProgressStepDescription, sngPercentComplete)
    End Sub

    Protected Sub UpdateProgress(description As String, sngPercentComplete As Single)
        Dim descriptionChanged = Not String.Equals(description, mProgressStepDescription)

        mProgressStepDescription = String.Copy(description)
        If sngPercentComplete < 0 Then
            sngPercentComplete = 0
        ElseIf sngPercentComplete > 100 Then
            sngPercentComplete = 100
        End If
        mProgressPercentComplete = sngPercentComplete

        If descriptionChanged Then
            If mProgressPercentComplete < Single.Epsilon Then
                LogMessage(mProgressStepDescription.Replace(Environment.NewLine, "; "))
            Else
                LogMessage(mProgressStepDescription & " (" & mProgressPercentComplete.ToString("0.0") & "% complete)".Replace(Environment.NewLine, "; "))
            End If
        End If

        OnProgressUpdate(mProgressStepDescription, mProgressPercentComplete)

    End Sub

    Private Sub WriteToLogFile(message As String, eMessageType As eMessageTypeConstants, duplicateHoldoffHours As Integer)

        Dim messageType As String

        Select Case eMessageType
            Case eMessageTypeConstants.Normal
                messageType = "Normal"
            Case eMessageTypeConstants.ErrorMsg
                messageType = "Error"
            Case eMessageTypeConstants.Warning
                messageType = "Warning"
            Case Else
                messageType = "Unknown"
        End Select

        Dim writeToLog = True

        Dim logKey As String = messageType & "_" & message
        Dim lastLogTime As DateTime
        Dim messageCached As Boolean

        If mLogDataCache.TryGetValue(logKey, lastLogTime) Then
            messageCached = True
        Else
            messageCached = False
            lastLogTime = DateTime.UtcNow.AddHours(-(duplicateHoldoffHours + 1))
        End If

        If duplicateHoldoffHours > 0 AndAlso DateTime.UtcNow.Subtract(lastLogTime).TotalHours < duplicateHoldoffHours Then
            writeToLog = False
        End If

        If Not writeToLog Then
            Exit Sub
        End If

        mLogFile.WriteLine(
            DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") & ControlChars.Tab &
            messageType & ControlChars.Tab &
            message)

        If messageCached Then
            mLogDataCache(logKey) = DateTime.UtcNow
        Else
            Try
                mLogDataCache.Add(logKey, DateTime.UtcNow)

                If mLogDataCache.Count > MAX_LOGDATA_CACHE_SIZE Then
                    TrimLogDataCache()
                End If
            Catch ex As Exception
                ' Ignore errors here
            End Try
        End If

    End Sub

    Protected Sub OperationComplete()
        RaiseEvent ProgressComplete()
    End Sub
End Class
