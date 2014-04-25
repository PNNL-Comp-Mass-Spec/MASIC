Option Strict On

Imports System.IO
Imports System.Threading
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Collections.Generic

''' <summary>
''' This class contains functions used by both clsProcessFilesBaseClass and clsProcessFoldersBaseClass
''' </summary>
''' <remarks>
''' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
''' Created in October 2013
''' Last updated in November 2013
''' </remarks>
Public MustInherit Class clsProcessFilesOrFoldersBase

#Region "Constants and Enums"

	Protected Enum eMessageTypeConstants
		Normal = 0
		ErrorMsg = 1
		Warning = 2
	End Enum

#End Region

#Region "Classwide Variables"
	Protected mShowMessages As Boolean = True

	Protected mFileDate As String
	Protected mAbortProcessing As Boolean

	Protected mLogMessagesToFile As Boolean
	Protected mLogFileUsesDateStamp As Boolean = True
	Protected mLogFilePath As String
	Protected mLogFile As StreamWriter

	' This variable is updated when CleanupFilePaths() is called
	Protected mOutputFolderPath As String
	Protected mLogFolderPath As String			' If blank, then mOutputFolderPath will be used; if mOutputFolderPath is also blank, then the log is created in the same folder as the executing assembly

	Public Event ProgressReset()
	Public Event ProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single)	   ' PercentComplete ranges from 0 to 100, but can contain decimal percentage values
	Public Event ProgressComplete()

	Public Event ErrorEvent(ByVal strMessage As String)
	Public Event WarningEvent(ByVal strMessage As String)
	Public Event MessageEvent(ByVal strMessage As String)

	Protected mProgressStepDescription As String
	Protected mProgressPercentComplete As Single		' Ranges from 0 to 100, but can contain decimal percentage values

	''' <summary>
	''' Keys in this dictionary are the log type and message (separated by an underscore), values are the most recent time the string was logged
	''' </summary>
	''' <remarks></remarks>
	Protected mLogDataCache As Dictionary(Of String, DateTime)
	Protected mLogDataCacheStartTime As DateTime
	Protected Const MAX_LOGDATA_CACHE_SIZE As Integer = 100000

#End Region

#Region "Interface Functions"

	Public Property AbortProcessing() As Boolean
		Get
			Return mAbortProcessing
		End Get
		Set(ByVal Value As Boolean)
			mAbortProcessing = Value
		End Set
	End Property

	Public ReadOnly Property FileVersion() As String
		Get
			Return GetVersionForExecutingAssembly()
		End Get
	End Property

	Public ReadOnly Property FileDate() As String
		Get
			Return mFileDate
		End Get
	End Property

	Public Property LogFilePath() As String
		Get
			Return mLogFilePath
		End Get
		Set(ByVal value As String)
			If value Is Nothing Then value = String.Empty
			mLogFilePath = value
		End Set
	End Property

	Public Property LogFolderPath() As String
		Get
			Return mLogFolderPath
		End Get
		Set(ByVal value As String)
			mLogFolderPath = value
		End Set
	End Property

	Public Property LogMessagesToFile() As Boolean
		Get
			Return mLogMessagesToFile
		End Get
		Set(ByVal value As Boolean)
			mLogMessagesToFile = value
		End Set
	End Property

	Public Overridable ReadOnly Property ProgressStepDescription() As String
		Get
			Return mProgressStepDescription
		End Get
	End Property

	' ProgressPercentComplete ranges from 0 to 100, but can contain decimal percentage values
	Public ReadOnly Property ProgressPercentComplete() As Single
		Get
			Return CType(Math.Round(mProgressPercentComplete, 2), Single)
		End Get
	End Property

	Public Property ShowMessages() As Boolean
		Get
			Return mShowMessages
		End Get
		Set(ByVal Value As Boolean)
			mShowMessages = Value
		End Set
	End Property

#End Region

	''' <summary>
	''' Constructor
	''' </summary>
	''' <remarks></remarks>
	Public Sub New()
		mProgressStepDescription = String.Empty

		mOutputFolderPath = String.Empty
		mLogFolderPath = String.Empty
		mLogFilePath = String.Empty

		mLogDataCache = New Dictionary(Of String, DateTime)
		mLogDataCacheStartTime = DateTime.UtcNow
	End Sub


	Public Overridable Sub AbortProcessingNow()
		mAbortProcessing = True
	End Sub

	Protected MustOverride Sub CleanupPaths(ByRef strInputFileOrFolderPath As String, ByRef strOutputFolderPath As String)

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
	''' <param name="strApplicationName">Application name</param>
	''' <param name="strSettingsFileName">Settings file name</param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Shared Function CreateSettingsFileIfMissing(ByVal strApplicationName As String, ByVal strSettingsFileName As String) As Boolean
		Dim strSettingsFilePathLocal As String = GetSettingsFilePathLocal(strApplicationName, strSettingsFileName)

		Return CreateSettingsFileIfMissing(strSettingsFilePathLocal)

	End Function

	''' <summary>
	''' Verifies that the specified .XML settings file exists in the user's local settings folder
	''' </summary>
	''' <param name="strSettingsFilePathLocal">Full path to the local settings file, for example C:\Users\username\AppData\Roaming\AppName\SettingsFileName.xml</param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Shared Function CreateSettingsFileIfMissing(ByVal strSettingsFilePathLocal As String) As Boolean

		Try
			If Not File.Exists(strSettingsFilePathLocal) Then
				Dim fiMasterSettingsFile As FileInfo
				fiMasterSettingsFile = New FileInfo(Path.Combine(GetAppFolderPath(), Path.GetFileName(strSettingsFilePathLocal)))

				If fiMasterSettingsFile.Exists Then
					fiMasterSettingsFile.CopyTo(strSettingsFilePathLocal)
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
		Const intMaxWaitTimeMSec As Integer = 1000
		GarbageCollectNow(intMaxWaitTimeMSec)
	End Sub

	''' <summary>
	''' Perform garbage collection
	''' </summary>
	''' <param name="intMaxWaitTimeMSec"></param>
	''' <remarks></remarks>
	Public Shared Sub GarbageCollectNow(ByVal intMaxWaitTimeMSec As Integer)
		Const THREAD_SLEEP_TIME_MSEC As Integer = 100

		Dim intTotalThreadWaitTimeMsec As Integer
		If intMaxWaitTimeMSec < 100 Then intMaxWaitTimeMSec = 100
		If intMaxWaitTimeMSec > 5000 Then intMaxWaitTimeMSec = 5000

		Thread.Sleep(100)

		Try
			Dim gcThread As New Thread(AddressOf GarbageCollectWaitForGC)
			gcThread.Start()

			intTotalThreadWaitTimeMsec = 0
			While gcThread.IsAlive AndAlso intTotalThreadWaitTimeMsec < intMaxWaitTimeMSec
				Thread.Sleep(THREAD_SLEEP_TIME_MSEC)
				intTotalThreadWaitTimeMsec += THREAD_SLEEP_TIME_MSEC
			End While
			If gcThread.IsAlive Then gcThread.Abort()

		Catch ex As Exception
			' Ignore errors here
		End Try

	End Sub

	Protected Shared Sub GarbageCollectWaitForGC()
		Try
			GC.Collect()
			GC.WaitForPendingFinalizers()
		Catch
			' Ignore errors here
		End Try
	End Sub

	''' <summary>
	''' Returns the full path to the folder into which this application should read/write settings file information
	''' </summary>
	''' <param name="strAppName"></param>
	''' <returns></returns>
	''' <remarks>For example, C:\Users\username\AppData\Roaming\AppName</remarks>
	Public Shared Function GetAppDataFolderPath(ByVal strAppName As String) As String
		Dim strAppDataFolder As String

		If String.IsNullOrWhiteSpace(strAppName) Then
			strAppName = String.Empty
		End If

		Try
			strAppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), strAppName)
			If Not Directory.Exists(strAppDataFolder) Then
				Directory.CreateDirectory(strAppDataFolder)
			End If

		Catch ex As Exception
			' Error creating the folder, revert to using the system Temp folder
			strAppDataFolder = Path.GetTempPath()
		End Try

		Return strAppDataFolder

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
	''' <param name="strProgramDate"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Shared Function GetAppVersion(ByVal strProgramDate As String) As String
		Return Assembly.GetExecutingAssembly().GetName().Version.ToString() & " (" & strProgramDate & ")"
	End Function

	Public MustOverride Function GetErrorMessage() As String

	Private Function GetVersionForExecutingAssembly() As String

		Dim strVersion As String

		Try
			strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
		Catch ex As Exception
			strVersion = "??.??.??.??"
		End Try

		Return strVersion

	End Function

	''' <summary>
	''' Returns the full path to this application's local settings file
	''' </summary>
	''' <param name="strApplicationName"></param>
	''' <param name="strSettingsFileName"></param>
	''' <returns></returns>
	''' <remarks>For example, C:\Users\username\AppData\Roaming\AppName\SettingsFileName.xml</remarks>
	Public Shared Function GetSettingsFilePathLocal(ByVal strApplicationName As String, ByVal strSettingsFileName As String) As String
		Return Path.Combine(GetAppDataFolderPath(strApplicationName), strSettingsFileName)
	End Function

	Protected Sub HandleException(ByVal strBaseMessage As String, ByVal ex As Exception)
		If String.IsNullOrWhiteSpace(strBaseMessage) Then
			strBaseMessage = "Error"
		End If

		If ShowMessages Then
			' Note that ShowErrorMessage() will call LogMessage()
			ShowErrorMessage(strBaseMessage & ": " & ex.Message, True)
		Else
			LogMessage(strBaseMessage & ": " & ex.Message, eMessageTypeConstants.ErrorMsg)
			Throw New Exception(strBaseMessage, ex)
		End If

	End Sub

	Protected Sub LogMessage(ByVal strMessage As String)
		LogMessage(strMessage, eMessageTypeConstants.Normal)
	End Sub

	Protected Sub LogMessage(ByVal strMessage As String, ByVal eMessageType As eMessageTypeConstants)
		LogMessage(strMessage, eMessageType, intDuplicateHoldoffHours:=0)
	End Sub

	Protected Sub LogMessage(ByVal strMessage As String, ByVal eMessageType As eMessageTypeConstants, ByVal intDuplicateHoldoffHours As Integer)
		' Note that CleanupPaths() will update mOutputFolderPath, which is used here if mLogFolderPath is blank
		' Thus, be sure to call CleanupPaths (or update mLogFolderPath) before the first call to LogMessage

		Dim strMessageType As String
		Dim blnOpeningExistingFile As Boolean

		Select Case eMessageType
			Case eMessageTypeConstants.Normal
				strMessageType = "Normal"
			Case eMessageTypeConstants.ErrorMsg
				strMessageType = "Error"
			Case eMessageTypeConstants.Warning
				strMessageType = "Warning"
			Case Else
				strMessageType = "Unknown"
		End Select

		If mLogFile Is Nothing AndAlso mLogMessagesToFile Then
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
					If mLogFolderPath Is Nothing Then mLogFolderPath = String.Empty

					If String.IsNullOrWhiteSpace(mLogFolderPath) Then
						' Log folder is undefined; use mOutputFolderPath if it is defined
						If Not String.IsNullOrWhiteSpace(mOutputFolderPath) Then
							mLogFolderPath = String.Copy(mOutputFolderPath)
						End If
					End If

					If mLogFolderPath.Length > 0 Then
						' Create the log folder if it doesn't exist
						If Not Directory.Exists(mLogFolderPath) Then
							Directory.CreateDirectory(mLogFolderPath)
						End If
					End If
				Catch ex As Exception
					mLogFolderPath = String.Empty
				End Try

				If Not Path.IsPathRooted(mLogFilePath) AndAlso mLogFolderPath.Length > 0 Then
					mLogFilePath = Path.Combine(mLogFolderPath, mLogFilePath)
				End If

				blnOpeningExistingFile = File.Exists(mLogFilePath)

				If (blnOpeningExistingFile And mLogDataCache.Count = 0) Then
					UpdateLogDataCache(mLogFilePath, DateTime.UtcNow.AddHours(-intDuplicateHoldoffHours))
				End If

				mLogFile = New StreamWriter(New FileStream(mLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
				mLogFile.AutoFlush = True

				If Not blnOpeningExistingFile Then
					mLogFile.WriteLine("Date" & ControlChars.Tab & _
					 "Type" & ControlChars.Tab & _
					 "Message")
				End If

			Catch ex As Exception
				' Error creating the log file; set mLogMessagesToFile to false so we don't repeatedly try to create it
				mLogMessagesToFile = False
				HandleException("Error opening log file", ex)
				' Note: do not exit this function if an exception occurs
			End Try

		End If

		If Not mLogFile Is Nothing Then
			Dim blnWriteToLog As Boolean = True

			Dim strLogKey As String = strMessageType & "_" & strMessage
			Dim dtLastLogTime As DateTime
			Dim blnMessageCached As Boolean

			If mLogDataCache.TryGetValue(strLogKey, dtLastLogTime) Then
				blnMessageCached = True
			Else
				blnMessageCached = False
				dtLastLogTime = DateTime.UtcNow.AddHours(-(intDuplicateHoldoffHours + 1))
			End If

			If intDuplicateHoldoffHours > 0 AndAlso DateTime.UtcNow.Subtract(dtLastLogTime).TotalHours < intDuplicateHoldoffHours Then
				blnWriteToLog = False
			End If

			If blnWriteToLog Then

				mLogFile.WriteLine(
				  DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") & ControlChars.Tab & _
				  strMessageType & ControlChars.Tab & _
				  strMessage)

				If blnMessageCached Then
					mLogDataCache(strLogKey) = DateTime.UtcNow
				Else
					Try
						mLogDataCache.Add(strLogKey, DateTime.UtcNow)

						If mLogDataCache.Count > MAX_LOGDATA_CACHE_SIZE Then
							TrimLogDataCache()
						End If
					Catch ex As Exception
						' Ignore errors here
					End Try
				End If
			End If

		End If

		RaiseMessageEvent(strMessage, eMessageType)

	End Sub

	Private Sub RaiseMessageEvent(ByVal strMessage As String, ByVal eMessageType As eMessageTypeConstants)
		Static strLastMessage As String = String.Empty
		Static dtLastReportTime As DateTime

		If Not String.IsNullOrWhiteSpace(strMessage) Then
			If String.Equals(strMessage, strLastMessage) AndAlso _
			   DateTime.UtcNow.Subtract(dtLastReportTime).TotalSeconds < 0.5 Then
				' Duplicate message; do not raise any events
			Else
				dtLastReportTime = DateTime.UtcNow
				strLastMessage = String.Copy(strMessage)

				Select Case eMessageType
					Case eMessageTypeConstants.Normal
						RaiseEvent MessageEvent(strMessage)

					Case eMessageTypeConstants.Warning
						RaiseEvent WarningEvent(strMessage)

					Case eMessageTypeConstants.ErrorMsg
						RaiseEvent ErrorEvent(strMessage)

					Case Else
						RaiseEvent MessageEvent(strMessage)
				End Select
			End If
		End If

	End Sub

	Protected Sub ResetProgress()
		mProgressPercentComplete = 0
		RaiseEvent ProgressReset()
	End Sub

	Protected Sub ResetProgress(ByVal strProgressStepDescription As String)
		UpdateProgress(strProgressStepDescription, 0)
		RaiseEvent ProgressReset()
	End Sub

	Protected Sub ShowErrorMessage(ByVal strMessage As String)
		ShowErrorMessage(strMessage, blnAllowLogToFile:=True)
	End Sub

	Protected Sub ShowErrorMessage(ByVal strMessage As String, ByVal blnAllowLogToFile As Boolean)
		ShowErrorMessage(strMessage, blnAllowLogToFile, intDuplicateHoldoffHours:=0)
	End Sub

	Protected Sub ShowErrorMessage(ByVal strMessage As String, ByVal intDuplicateHoldoffHours As Integer)
		ShowErrorMessage(strMessage, blnAllowLogToFile:=True, intDuplicateHoldoffHours:=intDuplicateHoldoffHours)
	End Sub

	Protected Sub ShowErrorMessage(ByVal strMessage As String, ByVal blnAllowLogToFile As Boolean, ByVal intDuplicateHoldoffHours As Integer)
		Const strSeparator As String = "------------------------------------------------------------------------------"

		Console.WriteLine()
		Console.WriteLine(strSeparator)
		Console.WriteLine(strMessage)
		Console.WriteLine(strSeparator)
		Console.WriteLine()

		If blnAllowLogToFile Then
			' Note that LogMessage will call RaiseMessageEvent
			LogMessage(strMessage, eMessageTypeConstants.ErrorMsg, intDuplicateHoldoffHours)
		Else
			RaiseMessageEvent(strMessage, eMessageTypeConstants.ErrorMsg)
		End If

	End Sub

	Protected Sub ShowMessage(ByVal strMessage As String)
		ShowMessage(strMessage, blnAllowLogToFile:=True, blnPrecedeWithNewline:=False, intDuplicateHoldoffHours:=0)
	End Sub

	Protected Sub ShowMessage(ByVal strMessage As String, ByVal intDuplicateHoldoffHours As Integer)
		ShowMessage(strMessage, blnAllowLogToFile:=True, blnPrecedeWithNewline:=False, intDuplicateHoldoffHours:=intDuplicateHoldoffHours)
	End Sub

	Protected Sub ShowMessage(ByVal strMessage As String, ByVal blnAllowLogToFile As Boolean)
		ShowMessage(strMessage, blnAllowLogToFile, blnPrecedeWithNewline:=False, intDuplicateHoldoffHours:=0)
	End Sub

	Protected Sub ShowMessage(ByVal strMessage As String, ByVal blnAllowLogToFile As Boolean, ByVal blnPrecedeWithNewline As Boolean)
		ShowMessage(strMessage, blnAllowLogToFile, blnPrecedeWithNewline, intDuplicateHoldoffHours:=0)
	End Sub

								   
	Protected Sub ShowMessage(
	  ByVal strMessage As String, 
	  ByVal blnAllowLogToFile As Boolean, 
	  ByVal blnPrecedeWithNewline As Boolean, 
	  ByVal intDuplicateHoldoffHours As Integer)

		ShowMessage(strMessage, blnAllowLogToFile, blnPrecedeWithNewline, intDuplicateHoldoffHours, eMessageTypeConstants.Normal)
	End Sub

	Protected Sub ShowMessage(
	  ByVal strMessage As String, 
	  ByVal blnAllowLogToFile As Boolean, 
	  ByVal blnPrecedeWithNewline As Boolean, 
	  ByVal intDuplicateHoldoffHours As Integer,
	  ByVal eMessageType as eMessageTypeConstants)

		If blnPrecedeWithNewline Then
			Console.WriteLine()
		End If
		Console.WriteLine(strMessage)

		If blnAllowLogToFile Then
			' Note that LogMessage will call RaiseMessageEvent
			LogMessage(strMessage, eMessageType, intDuplicateHoldoffHours)
		Else
			RaiseMessageEvent(strMessage, eMessageType)
		End If

	End Sub
	
	Protected Sub ShowWarning(strMessage As String)
		ShowMessage(strMessage, blnAllowLogToFile := True, blnPrecedeWithNewline := False, intDuplicateHoldoffHours := 0, eMessageType := eMessageTypeConstants.Warning)
	End Sub
	
	Protected Sub ShowWarning(strMessage As String, intDuplicateHoldoffHours As Integer)
		ShowMessage(strMessage, blnAllowLogToFile := True, blnPrecedeWithNewline := False, intDuplicateHoldoffHours := intDuplicateHoldoffHours, eMessageType := eMessageTypeConstants.Warning)
	End Sub
	
	Protected Sub ShowWarning(strMessage As String, blnAllowLogToFile As Boolean)
		ShowMessage(strMessage, blnAllowLogToFile, blnPrecedeWithNewline := False, intDuplicateHoldoffHours := 0, eMessageType := eMessageTypeConstants.Warning)
	End Sub

	Private Sub TrimLogDataCache()

		If mLogDataCache.Count < MAX_LOGDATA_CACHE_SIZE Then Exit Sub

		Try
			' Remove entries from mLogDataCache so that the list count is 80% of MAX_LOGDATA_CACHE_SIZE

			' First construct a list of dates that we can sort to determine the datetime threshold for removal
			Dim lstDates As List(Of Date) = (From entry In mLogDataCache Select entry.Value).ToList()

			' Sort by date
			lstDates.Sort()

			Dim intThresholdIndex As Integer = CInt(Math.Floor(mLogDataCache.Count - MAX_LOGDATA_CACHE_SIZE * 0.8))
			If intThresholdIndex < 0 Then intThresholdIndex = 0

			Dim dtThreshold = lstDates(intThresholdIndex)

			' Construct a list of keys to be removed
			Dim lstKeys As List(Of String) = (From entry In mLogDataCache Where entry.Value <= dtThreshold Select entry.Key).ToList()

			' Remove each of the keys
			For Each strKey In lstKeys
				mLogDataCache.Remove(strKey)
			Next

		Catch ex As Exception
			' Ignore errors here
		End Try
	End Sub


	Private Sub UpdateLogDataCache(ByVal strLogFilePath As String, ByVal dtDateThresholdToStore As DateTime)
		Static dtLastErrorShown As DateTime = DateTime.UtcNow.AddSeconds(-60)

		Dim reParseLine As Regex = New Regex("^([^\t]+)\t([^\t]+)\t(.+)", RegexOptions.Compiled)

		Try
			mLogDataCache.Clear()
			mLogDataCacheStartTime = dtDateThresholdToStore

			Using srLogFile = New StreamReader(New FileStream(strLogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				While srLogFile.Peek > -1
					Dim strLineIn = srLogFile.ReadLine()
					Dim reMatch = reParseLine.Match(strLineIn)

					If reMatch.Success Then
						Dim dtLogTime As DateTime
						If DateTime.TryParse(reMatch.Groups(1).Value, dtLogTime) Then
							dtLogTime = dtLogTime.ToUniversalTime()
							If dtLogTime >= dtDateThresholdToStore Then
								Dim strKey As String = reMatch.Groups(2).Value & "_" & reMatch.Groups(3).Value

								Try
									If mLogDataCache.ContainsKey(strKey) Then
										mLogDataCache(strKey) = dtLogTime
									Else
										mLogDataCache.Add(strKey, dtLogTime)
									End If
								Catch ex As Exception
									' Ignore errors here
								End Try

							End If
						End If
					End If
				End While
			End Using

			If mLogDataCache.Count > MAX_LOGDATA_CACHE_SIZE Then
				TrimLogDataCache()
			End If

		Catch ex As Exception
			If DateTime.UtcNow.Subtract(dtLastErrorShown).TotalSeconds > 10 Then
				dtLastErrorShown = DateTime.UtcNow
				Console.WriteLine("Error caching the log file: " & ex.Message)
			End If

		End Try

	End Sub

	Protected Sub UpdateProgress(ByVal strProgressStepDescription As String)
		UpdateProgress(strProgressStepDescription, mProgressPercentComplete)
	End Sub

	Protected Sub UpdateProgress(ByVal sngPercentComplete As Single)
		UpdateProgress(ProgressStepDescription, sngPercentComplete)
	End Sub

	Protected Sub UpdateProgress(ByVal strProgressStepDescription As String, ByVal sngPercentComplete As Single)
		Dim blnDescriptionChanged = Not String.Equals(strProgressStepDescription, mProgressStepDescription)

		mProgressStepDescription = String.Copy(strProgressStepDescription)
		If sngPercentComplete < 0 Then
			sngPercentComplete = 0
		ElseIf sngPercentComplete > 100 Then
			sngPercentComplete = 100
		End If
		mProgressPercentComplete = sngPercentComplete

		If blnDescriptionChanged Then
			If mProgressPercentComplete < Single.Epsilon Then
				LogMessage(mProgressStepDescription.Replace(Environment.NewLine, "; "))
			Else
				LogMessage(mProgressStepDescription & " (" & mProgressPercentComplete.ToString("0.0") & "% complete)".Replace(Environment.NewLine, "; "))
			End If
		End If

		RaiseEvent ProgressChanged(ProgressStepDescription, ProgressPercentComplete)

	End Sub

	Protected Sub OperationComplete()
		RaiseEvent ProgressComplete()
	End Sub
End Class
