Option Strict On

Imports System.IO


''' <summary>
''' This class can be used as a base class for classes that process a file or files, and create
''' new output files in an output folder.  Note that this class contains simple error codes that
''' can be set from any derived classes.  The derived classes can also set their own local error codes
''' </summary>
''' <remarks>
''' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
''' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
''' Started November 9, 2003
''' </remarks>
Public MustInherit Class clsProcessFilesBaseClass
	Inherits clsProcessFilesOrFoldersBase

	''' <summary>
	''' Constructor
	''' </summary>
	''' <remarks></remarks>
	Public Sub New()
		mFileDate = "October 17, 2013"
		mErrorCode = eProcessFilesErrorCodes.NoError		
	End Sub

#Region "Constants and Enums"
	Public Enum eProcessFilesErrorCodes
		NoError = 0
		InvalidInputFilePath = 1
		InvalidOutputFolderPath = 2
		ParameterFileNotFound = 4
		InvalidParameterFile = 8
		FilePathError = 16
		LocalizedError = 32
		UnspecifiedError = -1
	End Enum

	'' Copy the following to any derived classes
	''Public Enum eDerivedClassErrorCodes
	''    NoError = 0
	''    UnspecifiedError = -1
	''End Enum
#End Region

#Region "Classwide Variables"
	''Private mLocalErrorCode As eDerivedClassErrorCodes

	''Public ReadOnly Property LocalErrorCode() As eDerivedClassErrorCodes
	''    Get
	''        Return mLocalErrorCode
	''    End Get
	''End Property

	Private mErrorCode As eProcessFilesErrorCodes

	Protected mIgnoreErrorsWhenUsingWildcardMatching As Boolean


#End Region

#Region "Interface Functions"
	
	''' <summary>
	''' This option applies when processing files matched with a wildcard
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property IgnoreErrorsWhenUsingWildcardMatching As Boolean
		Get
			Return mIgnoreErrorsWhenUsingWildcardMatching
		End Get
		Set(value As Boolean)
			mIgnoreErrorsWhenUsingWildcardMatching = value
		End Set
	End Property

	Public ReadOnly Property ErrorCode() As eProcessFilesErrorCodes
		Get
			Return mErrorCode
		End Get
	End Property

#End Region

	Protected Overrides Sub CleanupPaths(ByRef strInputFileOrFolderPath As String, ByRef strOutputFolderPath As String)
		CleanupFilePaths(strInputFileOrFolderPath, strOutputFolderPath)
	End Sub

	Protected Function CleanupFilePaths(ByRef strInputFilePath As String, ByRef strOutputFolderPath As String) As Boolean
		' Returns True if success, False if failure

		Dim ioFileInfo As FileInfo
		Dim ioFolder As DirectoryInfo
		Dim blnSuccess As Boolean

		Try
			' Make sure strInputFilePath points to a valid file
			ioFileInfo = New FileInfo(strInputFilePath)

			If Not ioFileInfo.Exists Then
				If ShowMessages Then
					ShowErrorMessage("Input file not found: " & strInputFilePath)
				Else
					LogMessage("Input file not found: " & strInputFilePath, eMessageTypeConstants.ErrorMsg)
				End If

				mErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
				blnSuccess = False
			Else
				If String.IsNullOrWhiteSpace(strOutputFolderPath) Then
					' Define strOutputFolderPath based on strInputFilePath
					strOutputFolderPath = ioFileInfo.DirectoryName
				End If

				' Make sure strOutputFolderPath points to a folder
				ioFolder = New DirectoryInfo(strOutputFolderPath)

				If Not ioFolder.Exists() Then
					' strOutputFolderPath points to a non-existent folder; attempt to create it
					ioFolder.Create()
				End If

				mOutputFolderPath = String.Copy(ioFolder.FullName)

				blnSuccess = True
			End If

		Catch ex As Exception
			HandleException("Error cleaning up the file paths", ex)
			Return False
		End Try

		Return blnSuccess
	End Function

	Protected Function CleanupInputFilePath(ByRef strInputFilePath As String) As Boolean
		' Returns True if success, False if failure

		Dim ioFileInfo As FileInfo
		Dim blnSuccess As Boolean

		Try
			' Make sure strInputFilePath points to a valid file
			ioFileInfo = New FileInfo(strInputFilePath)

			If Not ioFileInfo.Exists Then
				If ShowMessages Then
					ShowErrorMessage("Input file not found: " & strInputFilePath)
				Else
					LogMessage("Input file not found: " & strInputFilePath, eMessageTypeConstants.ErrorMsg)
				End If

				mErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
				blnSuccess = False
			Else
				blnSuccess = True
			End If

		Catch ex As Exception
			HandleException("Error cleaning up the file paths", ex)
			Return False
		End Try

		Return blnSuccess
	End Function

	Protected Function GetBaseClassErrorMessage() As String
		' Returns String.Empty if no error

		Dim strErrorMessage As String

		Select Case ErrorCode
			Case eProcessFilesErrorCodes.NoError
				strErrorMessage = String.Empty
			Case eProcessFilesErrorCodes.InvalidInputFilePath
				strErrorMessage = "Invalid input file path"
			Case eProcessFilesErrorCodes.InvalidOutputFolderPath
				strErrorMessage = "Invalid output folder path"
			Case eProcessFilesErrorCodes.ParameterFileNotFound
				strErrorMessage = "Parameter file not found"
			Case eProcessFilesErrorCodes.InvalidParameterFile
				strErrorMessage = "Invalid parameter file"
			Case eProcessFilesErrorCodes.FilePathError
				strErrorMessage = "General file path error"
			Case eProcessFilesErrorCodes.LocalizedError
				strErrorMessage = "Localized error"
			Case eProcessFilesErrorCodes.UnspecifiedError
				strErrorMessage = "Unspecified error"
			Case Else
				' This shouldn't happen
				strErrorMessage = "Unknown error state"
		End Select

		Return strErrorMessage

	End Function

	Public Overridable Function GetDefaultExtensionsToParse() As String()
		Dim strExtensionsToParse(0) As String

		strExtensionsToParse(0) = ".*"

		Return strExtensionsToParse

	End Function

	Public Function ProcessFilesWildcard(ByVal strInputFolderPath As String) As Boolean
		Return ProcessFilesWildcard(strInputFolderPath, String.Empty, String.Empty)
	End Function

	Public Function ProcessFilesWildcard(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String) As Boolean
		Return ProcessFilesWildcard(strInputFilePath, strOutputFolderPath, String.Empty)
	End Function

	Public Function ProcessFilesWildcard(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String) As Boolean
		Return ProcessFilesWildcard(strInputFilePath, strOutputFolderPath, strParameterFilePath, True)
	End Function

	Public Function ProcessFilesWildcard(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String, ByVal blnResetErrorCode As Boolean) As Boolean
		' Returns True if success, False if failure

		Dim blnSuccess As Boolean
		Dim intMatchCount As Integer

		Dim strCleanPath As String
		Dim strInputFolderPath As String

		Dim ioFileInfo As FileInfo
		Dim ioFolderInfo As DirectoryInfo

		mAbortProcessing = False
		blnSuccess = True
		Try
			' Possibly reset the error code
			If blnResetErrorCode Then mErrorCode = eProcessFilesErrorCodes.NoError

			If Not String.IsNullOrWhiteSpace(strOutputFolderPath) Then
				' Update the cached output folder path
				mOutputFolderPath = String.Copy(strOutputFolderPath)
			End If

			' See if strInputFilePath contains a wildcard (* or ?)
			If Not strInputFilePath Is Nothing AndAlso (strInputFilePath.Contains("*") Or strInputFilePath.Contains("?")) Then
				' Obtain a list of the matching  files

				' Copy the path into strCleanPath and replace any * or ? characters with _
				strCleanPath = strInputFilePath.Replace("*", "_")
				strCleanPath = strCleanPath.Replace("?", "_")

				ioFileInfo = New FileInfo(strCleanPath)
				If ioFileInfo.Directory.Exists Then
					strInputFolderPath = ioFileInfo.DirectoryName
				Else
					' Use the directory that has the .exe file
					strInputFolderPath = GetAppFolderPath()
				End If

				ioFolderInfo = New DirectoryInfo(strInputFolderPath)

				' Remove any directory information from strInputFilePath
				strInputFilePath = Path.GetFileName(strInputFilePath)

				intMatchCount = 0
				For Each ioFileMatch As FileInfo In ioFolderInfo.GetFiles(strInputFilePath)
					intMatchCount += 1

					blnSuccess = ProcessFile(ioFileMatch.FullName, strOutputFolderPath, strParameterFilePath, blnResetErrorCode)

					If mAbortProcessing Then
						Exit For
					ElseIf Not blnSuccess AndAlso Not mIgnoreErrorsWhenUsingWildcardMatching Then
						Exit For
					End If
					If intMatchCount Mod 100 = 0 Then Console.Write(".")

				Next ioFileMatch

				If intMatchCount = 0 Then
					If mErrorCode = eProcessFilesErrorCodes.NoError Then
						If ShowMessages Then
							ShowErrorMessage("No match was found for the input file path: " & strInputFilePath)
						Else
							LogMessage("No match was found for the input file path: " & strInputFilePath, eMessageTypeConstants.ErrorMsg)
						End If
					End If
				Else
					Console.WriteLine()
				End If

			Else
				blnSuccess = ProcessFile(strInputFilePath, strOutputFolderPath, strParameterFilePath, blnResetErrorCode)
			End If

		Catch ex As Exception
			HandleException("Error in ProcessFilesWildcard", ex)
			Return False
		End Try

		Return blnSuccess

	End Function

	Public Function ProcessFile(ByVal strInputFilePath As String) As Boolean
		Return ProcessFile(strInputFilePath, String.Empty, String.Empty)
	End Function

	Public Function ProcessFile(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String) As Boolean
		Return ProcessFile(strInputFilePath, strOutputFolderPath, String.Empty)
	End Function

	Public Function ProcessFile(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String) As Boolean
		Return ProcessFile(strInputFilePath, strOutputFolderPath, strParameterFilePath, True)
	End Function

	' Main function for processing a single file
	Public MustOverride Function ProcessFile(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String, ByVal blnResetErrorCode As Boolean) As Boolean

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFolderPath As String) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFolderPath, String.Empty, String.Empty)
	End Function

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFilePathOrFolder, strOutputFolderName, String.Empty)
	End Function

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String, ByVal strParameterFilePath As String) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFilePathOrFolder, strOutputFolderName, String.Empty, False, strParameterFilePath)
	End Function

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String, ByVal strParameterFilePath As String, ByVal strExtensionsToParse() As String) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFilePathOrFolder, strOutputFolderName, String.Empty, False, strParameterFilePath, 0, strExtensionsToParse)
	End Function

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String, ByVal strOutputFolderAlternatePath As String, ByVal blnRecreateFolderHierarchyInAlternatePath As Boolean) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFilePathOrFolder, strOutputFolderName, strOutputFolderAlternatePath, blnRecreateFolderHierarchyInAlternatePath, String.Empty)
	End Function

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String, ByVal strOutputFolderAlternatePath As String, ByVal blnRecreateFolderHierarchyInAlternatePath As Boolean, ByVal strParameterFilePath As String) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFilePathOrFolder, strOutputFolderName, strOutputFolderAlternatePath, blnRecreateFolderHierarchyInAlternatePath, strParameterFilePath, 0)
	End Function

	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String, ByVal strOutputFolderAlternatePath As String, ByVal blnRecreateFolderHierarchyInAlternatePath As Boolean, ByVal strParameterFilePath As String, ByVal intRecurseFoldersMaxLevels As Integer) As Boolean
		Return ProcessFilesAndRecurseFolders(strInputFilePathOrFolder, strOutputFolderName, strOutputFolderAlternatePath, blnRecreateFolderHierarchyInAlternatePath, strParameterFilePath, intRecurseFoldersMaxLevels, GetDefaultExtensionsToParse())
	End Function

	' Main function for processing files in a folder (and subfolders)
	Public Function ProcessFilesAndRecurseFolders(ByVal strInputFilePathOrFolder As String, ByVal strOutputFolderName As String, ByVal strOutputFolderAlternatePath As String, ByVal blnRecreateFolderHierarchyInAlternatePath As Boolean, ByVal strParameterFilePath As String, ByVal intRecurseFoldersMaxLevels As Integer, ByVal strExtensionsToParse() As String) As Boolean
		' Calls ProcessFiles for all files in strInputFilePathOrFolder and below having an extension listed in strExtensionsToParse()
		' The extensions should be of the form ".TXT" or ".RAW" (i.e. a period then the extension)
		' If any of the extensions is "*" or ".*" then all files will be processed
		' If strInputFilePathOrFolder contains a filename with a wildcard (* or ?), then that information will be 
		'  used to filter the files that are processed
		' If intRecurseFoldersMaxLevels is <=0 then we recurse infinitely

		Dim strCleanPath As String
		Dim strInputFolderPath As String

		Dim ioFileInfo As FileInfo
		Dim ioFolderInfo As DirectoryInfo

		Dim blnSuccess As Boolean
		Dim intFileProcessCount, intFileProcessFailCount As Integer

		' Examine strInputFilePathOrFolder to see if it contains a filename; if not, assume it points to a folder
		' First, see if it contains a * or ?
		Try
			If Not strInputFilePathOrFolder Is Nothing AndAlso (strInputFilePathOrFolder.Contains("*") Or strInputFilePathOrFolder.Contains("?")) Then
				' Copy the path into strCleanPath and replace any * or ? characters with _
				strCleanPath = strInputFilePathOrFolder.Replace("*", "_")
				strCleanPath = strCleanPath.Replace("?", "_")

				ioFileInfo = New FileInfo(strCleanPath)
				If ioFileInfo.Directory.Exists Then
					strInputFolderPath = ioFileInfo.DirectoryName
				Else
					' Use the directory that has the .exe file
					strInputFolderPath = GetAppFolderPath()
				End If

				' Remove any directory information from strInputFilePath
				strInputFilePathOrFolder = Path.GetFileName(strInputFilePathOrFolder)

			Else
				ioFolderInfo = New DirectoryInfo(strInputFilePathOrFolder)
				If ioFolderInfo.Exists Then
					strInputFolderPath = ioFolderInfo.FullName
					strInputFilePathOrFolder = "*"
				Else
					If ioFolderInfo.Parent.Exists Then
						strInputFolderPath = ioFolderInfo.Parent.FullName
						strInputFilePathOrFolder = Path.GetFileName(strInputFilePathOrFolder)
					Else
						' Unable to determine the input folder path
						strInputFolderPath = String.Empty
					End If
				End If
			End If

			If Not String.IsNullOrWhiteSpace(strInputFolderPath) Then

				' Validate the output folder path
				If Not String.IsNullOrWhiteSpace(strOutputFolderAlternatePath) Then
					Try
						ioFolderInfo = New DirectoryInfo(strOutputFolderAlternatePath)
						If Not ioFolderInfo.Exists Then ioFolderInfo.Create()
					Catch ex As Exception
						mErrorCode = eProcessFilesErrorCodes.InvalidOutputFolderPath
						ShowErrorMessage("Error validating the alternate output folder path in ProcessFilesAndRecurseFolders:" & ex.Message)
						Return False
					End Try
				End If

				' Initialize some parameters
				mAbortProcessing = False
				intFileProcessCount = 0
				intFileProcessFailCount = 0

				' Call RecurseFoldersWork
				Const intRecursionLevel As Integer = 1
				blnSuccess = RecurseFoldersWork(strInputFolderPath, strInputFilePathOrFolder, strOutputFolderName, _
				  strParameterFilePath, strOutputFolderAlternatePath, _
				  blnRecreateFolderHierarchyInAlternatePath, strExtensionsToParse, _
				  intFileProcessCount, intFileProcessFailCount, _
				  intRecursionLevel, intRecurseFoldersMaxLevels)

			Else
				mErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
				Return False
			End If

		Catch ex As Exception
			HandleException("Error in ProcessFilesAndRecurseFolders", ex)
			Return False
		End Try

		Return blnSuccess

	End Function

	Private Function RecurseFoldersWork(ByVal strInputFolderPath As String, ByVal strFileNameMatch As String, ByVal strOutputFolderName As String, _
	   ByVal strParameterFilePath As String, ByVal strOutputFolderAlternatePath As String, _
	   ByVal blnRecreateFolderHierarchyInAlternatePath As Boolean, ByVal strExtensionsToParse() As String, _
	   ByRef intFileProcessCount As Integer, ByRef intFileProcessFailCount As Integer, _
	   ByVal intRecursionLevel As Integer, ByVal intRecurseFoldersMaxLevels As Integer) As Boolean
		' If intRecurseFoldersMaxLevels is <=0 then we recurse infinitely

		Dim ioInputFolderInfo As DirectoryInfo

		Dim intExtensionIndex As Integer
		Dim blnProcessAllExtensions As Boolean

		Dim strOutputFolderPathToUse As String
		Dim blnSuccess As Boolean

		Try
			ioInputFolderInfo = New DirectoryInfo(strInputFolderPath)
		Catch ex As Exception
			' Input folder path error
			HandleException("Error in RecurseFoldersWork", ex)
			mErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
			Return False
		End Try

		Try
			If Not String.IsNullOrWhiteSpace(strOutputFolderAlternatePath) Then
				If blnRecreateFolderHierarchyInAlternatePath Then
					strOutputFolderAlternatePath = Path.Combine(strOutputFolderAlternatePath, ioInputFolderInfo.Name)
				End If
				strOutputFolderPathToUse = Path.Combine(strOutputFolderAlternatePath, strOutputFolderName)
			Else
				strOutputFolderPathToUse = strOutputFolderName
			End If
		Catch ex As Exception
			' Output file path error
			HandleException("Error in RecurseFoldersWork", ex)
			mErrorCode = eProcessFilesErrorCodes.InvalidOutputFolderPath
			Return False
		End Try

		Try
			' Validate strExtensionsToParse()
			For intExtensionIndex = 0 To strExtensionsToParse.Length - 1
				If strExtensionsToParse(intExtensionIndex) Is Nothing Then
					strExtensionsToParse(intExtensionIndex) = String.Empty
				Else
					If Not strExtensionsToParse(intExtensionIndex).StartsWith(".") Then
						strExtensionsToParse(intExtensionIndex) = "." & strExtensionsToParse(intExtensionIndex)
					End If

					If strExtensionsToParse(intExtensionIndex) = ".*" Then
						blnProcessAllExtensions = True
						Exit For
					Else
						strExtensionsToParse(intExtensionIndex) = strExtensionsToParse(intExtensionIndex).ToUpper()
					End If
				End If
			Next intExtensionIndex
		Catch ex As Exception
			HandleException("Error in RecurseFoldersWork", ex)
			mErrorCode = eProcessFilesErrorCodes.UnspecifiedError
			Return False
		End Try

		Try
			If Not String.IsNullOrWhiteSpace(strOutputFolderPathToUse) Then
				' Update the cached output folder path
				mOutputFolderPath = String.Copy(strOutputFolderPathToUse)
			End If

			ShowMessage("Examining " & strInputFolderPath)

			' Process any matching files in this folder
			blnSuccess = True
			For Each ioFileMatch As FileInfo In ioInputFolderInfo.GetFiles(strFileNameMatch)

				For intExtensionIndex = 0 To strExtensionsToParse.Length - 1
					If blnProcessAllExtensions OrElse ioFileMatch.Extension.ToUpper() = strExtensionsToParse(intExtensionIndex) Then
						blnSuccess = ProcessFile(ioFileMatch.FullName, strOutputFolderPathToUse, strParameterFilePath, True)
						If Not blnSuccess Then
							intFileProcessFailCount += 1
							blnSuccess = True
						Else
							intFileProcessCount += 1
						End If
						Exit For
					End If

					If mAbortProcessing Then Exit For

				Next intExtensionIndex
			Next ioFileMatch
		Catch ex As Exception
			HandleException("Error in RecurseFoldersWork", ex)
			mErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
			Return False
		End Try

		If Not mAbortProcessing Then
			' If intRecurseFoldersMaxLevels is <=0 then we recurse infinitely
			'  otherwise, compare intRecursionLevel to intRecurseFoldersMaxLevels
			If intRecurseFoldersMaxLevels <= 0 OrElse intRecursionLevel <= intRecurseFoldersMaxLevels Then
				' Call this function for each of the subfolders of ioInputFolderInfo
				For Each ioSubFolderInfo As DirectoryInfo In ioInputFolderInfo.GetDirectories()
					blnSuccess = RecurseFoldersWork(ioSubFolderInfo.FullName, strFileNameMatch, strOutputFolderName, _
					  strParameterFilePath, strOutputFolderAlternatePath, _
					  blnRecreateFolderHierarchyInAlternatePath, strExtensionsToParse, _
					  intFileProcessCount, intFileProcessFailCount, _
					  intRecursionLevel + 1, intRecurseFoldersMaxLevels)

					If Not blnSuccess Then Exit For
				Next ioSubFolderInfo
			End If
		End If

		Return blnSuccess

	End Function

	Protected Sub SetBaseClassErrorCode(ByVal eNewErrorCode As eProcessFilesErrorCodes)
		mErrorCode = eNewErrorCode
	End Sub

	'' The following functions should be placed in any derived class
	'' Cannot define as MustOverride since it contains a customized enumerated type (eDerivedClassErrorCodes) in the function declaration

	''Private Sub SetLocalErrorCode(ByVal eNewErrorCode As eDerivedClassErrorCodes)
	''    SetLocalErrorCode(eNewErrorCode, False)
	''End Sub

	''Private Sub SetLocalErrorCode(ByVal eNewErrorCode As eDerivedClassErrorCodes, ByVal blnLeaveExistingErrorCodeUnchanged As Boolean)
	''    If blnLeaveExistingErrorCodeUnchanged AndAlso mLocalErrorCode <> eDerivedClassErrorCodes.NoError Then
	''        ' An error code is already defined; do not change it
	''    Else
	''        mLocalErrorCode = eNewErrorCode

	''        If eNewErrorCode = eDerivedClassErrorCodes.NoError Then
	''            If MyBase.ErrorCode = clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError Then
	''                MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError)
	''            End If
	''        Else
	''            MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.LocalizedError)
	''        End If
	''    End If

	''End Sub
End Class
