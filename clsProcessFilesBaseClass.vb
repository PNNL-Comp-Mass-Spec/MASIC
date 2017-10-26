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
        mFileDate = "October 25, 2017"
        ErrorCode = eProcessFilesErrorCodes.NoError
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


#End Region

#Region "Interface Functions"

    ''' <summary>
    ''' This option applies when processing files matched with a wildcard
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property IgnoreErrorsWhenUsingWildcardMatching As Boolean

    ''' <summary>
    ''' Error code reflecting processing outcome
    ''' </summary>
    ''' <returns></returns>
    Public Property ErrorCode As eProcessFilesErrorCodes

#End Region

    Protected Overrides Sub CleanupPaths(ByRef inputFileOrFolderPath As String, ByRef outputFolderPath As String)
        CleanupFilePaths(inputFileOrFolderPath, outputFolderPath)
    End Sub

    Protected Function CleanupFilePaths(ByRef inputFilePath As String, ByRef outputFolderPath As String) As Boolean
        ' Returns True if success, False if failure

        Try
            ' Make sure inputFilePath points to a valid file
            Dim ioFileInfo = New FileInfo(inputFilePath)

            If Not ioFileInfo.Exists Then
                If ShowMessages Then
                    ShowErrorMessage("Input file not found: " & inputFilePath)
                Else
                    LogMessage("Input file not found: " & inputFilePath, eMessageTypeConstants.ErrorMsg)
                End If

                ErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
                Return False
            End If

            If String.IsNullOrWhiteSpace(outputFolderPath) Then
                ' Define outputFolderPath based on inputFilePath
                outputFolderPath = ioFileInfo.DirectoryName
            End If

            ' Make sure outputFolderPath points to a folder
            Dim outputFolder = New DirectoryInfo(outputFolderPath)

            If Not outputFolder.Exists() Then
                ' outputFolderPath points to a non-existent folder; attempt to create it
                outputFolder.Create()
            End If

            mOutputFolderPath = String.Copy(outputFolder.FullName)

            Return True


        Catch ex As Exception
            HandleException("Error cleaning up the file paths", ex)
            Return False
        End Try

    End Function

    Protected Function CleanupInputFilePath(ByRef inputFilePath As String) As Boolean
        ' Returns True if success, False if failure

        Try
            ' Make sure inputFilePath points to a valid file
            Dim inputFile = New FileInfo(inputFilePath)

            If Not inputFile.Exists Then
                If ShowMessages Then
                    ShowErrorMessage("Input file not found: " & inputFilePath)
                Else
                    LogMessage("Input file not found: " & inputFilePath, eMessageTypeConstants.ErrorMsg)
                End If

                ErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
                Return False
            Else
                Return True
            End If

        Catch ex As Exception
            HandleException("Error cleaning up the file paths", ex)
            Return False
        End Try

    End Function

    Protected Function GetBaseClassErrorMessage() As String
        ' Returns String.Empty if no error

        Dim errorMessage As String

        Select Case ErrorCode
            Case eProcessFilesErrorCodes.NoError
                errorMessage = String.Empty
            Case eProcessFilesErrorCodes.InvalidInputFilePath
                errorMessage = "Invalid input file path"
            Case eProcessFilesErrorCodes.InvalidOutputFolderPath
                errorMessage = "Invalid output folder path"
            Case eProcessFilesErrorCodes.ParameterFileNotFound
                errorMessage = "Parameter file not found"
            Case eProcessFilesErrorCodes.InvalidParameterFile
                errorMessage = "Invalid parameter file"
            Case eProcessFilesErrorCodes.FilePathError
                errorMessage = "General file path error"
            Case eProcessFilesErrorCodes.LocalizedError
                errorMessage = "Localized error"
            Case eProcessFilesErrorCodes.UnspecifiedError
                errorMessage = "Unspecified error"
            Case Else
                ' This shouldn't happen
                errorMessage = "Unknown error state"
        End Select

        Return errorMessage

    End Function

    Public Overridable Function GetDefaultExtensionsToParse() As String()
        Dim extensionsToParse(0) As String

        extensionsToParse(0) = ".*"

        Return extensionsToParse

    End Function

    Public Function ProcessFilesWildcard(inputFolderPath As String) As Boolean
        Return ProcessFilesWildcard(inputFolderPath, String.Empty, String.Empty)
    End Function

    Public Function ProcessFilesWildcard(inputFilePath As String, outputFolderPath As String) As Boolean
        Return ProcessFilesWildcard(inputFilePath, outputFolderPath, String.Empty)
    End Function

    Public Function ProcessFilesWildcard(inputFilePath As String, outputFolderPath As String, parameterFilePath As String) As Boolean
        Return ProcessFilesWildcard(inputFilePath, outputFolderPath, parameterFilePath, True)
    End Function

    Public Function ProcessFilesWildcard(inputFilePath As String, outputFolderPath As String, parameterFilePath As String, resetErrorCode As Boolean) As Boolean
        ' Returns True if success, False if failure

        AbortProcessing = False
        Dim success = True
        Try
            ' Possibly reset the error code
            If resetErrorCode Then ErrorCode = eProcessFilesErrorCodes.NoError

            If Not String.IsNullOrWhiteSpace(outputFolderPath) Then
                ' Update the cached output folder path
                mOutputFolderPath = String.Copy(outputFolderPath)
            End If

            ' See if inputFilePath contains a wildcard (* or ?)
            If Not inputFilePath Is Nothing AndAlso (inputFilePath.Contains("*") Or inputFilePath.Contains("?")) Then
                ' Obtain a list of the matching  files

                ' Copy the path into cleanPath and replace any * or ? characters with _
                Dim cleanPath = inputFilePath.Replace("*", "_")
                cleanPath = cleanPath.Replace("?", "_")

                Dim ioFileInfo = New FileInfo(cleanPath)
                Dim inputFolderPath As String
                If ioFileInfo.Directory.Exists Then
                    inputFolderPath = ioFileInfo.DirectoryName
                Else
                    ' Use the directory that has the .exe file
                    inputFolderPath = GetAppFolderPath()
                End If

                Dim ioFolderInfo = New DirectoryInfo(inputFolderPath)

                ' Remove any directory information from inputFilePath
                inputFilePath = Path.GetFileName(inputFilePath)

                Dim matchCount = 0
                For Each ioFileMatch As FileInfo In ioFolderInfo.GetFiles(inputFilePath)
                    matchCount += 1

                    success = ProcessFile(ioFileMatch.FullName, outputFolderPath, parameterFilePath, resetErrorCode)

                    If AbortProcessing Then
                        Exit For
                    ElseIf Not success AndAlso Not IgnoreErrorsWhenUsingWildcardMatching Then
                        Exit For
                    End If
                    If matchCount Mod 100 = 0 Then Console.Write(".")

                Next ioFileMatch

                If matchCount = 0 Then
                    If ErrorCode = eProcessFilesErrorCodes.NoError Then
                        If ShowMessages Then
                            ShowErrorMessage("No match was found for the input file path: " & inputFilePath)
                        Else
                            LogMessage("No match was found for the input file path: " & inputFilePath, eMessageTypeConstants.ErrorMsg)
                        End If
                    End If
                Else
                    Console.WriteLine()
                End If

            Else
                success = ProcessFile(inputFilePath, outputFolderPath, parameterFilePath, resetErrorCode)
            End If

        Catch ex As Exception
            HandleException("Error in ProcessFilesWildcard", ex)
            Return False
        End Try

        Return success

    End Function

    Public Function ProcessFile(inputFilePath As String) As Boolean
        Return ProcessFile(inputFilePath, String.Empty, String.Empty)
    End Function

    Public Function ProcessFile(inputFilePath As String, outputFolderPath As String) As Boolean
        Return ProcessFile(inputFilePath, outputFolderPath, String.Empty)
    End Function

    Public Function ProcessFile(inputFilePath As String, outputFolderPath As String, parameterFilePath As String) As Boolean
        Return ProcessFile(inputFilePath, outputFolderPath, parameterFilePath, True)
    End Function

    ' Main function for processing a single file
    Public MustOverride Function ProcessFile(inputFilePath As String, outputFolderPath As String, parameterFilePath As String, resetErrorCode As Boolean) As Boolean

    Public Function ProcessFilesAndRecurseFolders(inputFolderPath As String) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFolderPath, String.Empty, String.Empty)
    End Function

    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFilePathOrFolder, outputFolderName, String.Empty)
    End Function

    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String, parameterFilePath As String) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFilePathOrFolder, outputFolderName, String.Empty, False, parameterFilePath)
    End Function

    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String, parameterFilePath As String, extensionsToParse() As String) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFilePathOrFolder, outputFolderName, String.Empty, False, parameterFilePath, 0, extensionsToParse)
    End Function

    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String, outputFolderAlternatePath As String, recreateFolderHierarchyInAlternatePath As Boolean) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFilePathOrFolder, outputFolderName, outputFolderAlternatePath, recreateFolderHierarchyInAlternatePath, String.Empty)
    End Function

    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String, outputFolderAlternatePath As String, recreateFolderHierarchyInAlternatePath As Boolean, parameterFilePath As String) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFilePathOrFolder, outputFolderName, outputFolderAlternatePath, recreateFolderHierarchyInAlternatePath, parameterFilePath, 0)
    End Function

    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String, outputFolderAlternatePath As String, recreateFolderHierarchyInAlternatePath As Boolean, parameterFilePath As String, recurseFoldersMaxLevels As Integer) As Boolean
        Return ProcessFilesAndRecurseFolders(inputFilePathOrFolder, outputFolderName, outputFolderAlternatePath, recreateFolderHierarchyInAlternatePath, parameterFilePath, recurseFoldersMaxLevels, GetDefaultExtensionsToParse())
    End Function

    ' Main function for processing files in a folder (and subfolders)
    Public Function ProcessFilesAndRecurseFolders(inputFilePathOrFolder As String, outputFolderName As String, outputFolderAlternatePath As String, recreateFolderHierarchyInAlternatePath As Boolean, parameterFilePath As String, recurseFoldersMaxLevels As Integer, extensionsToParse() As String) As Boolean
        ' Calls ProcessFiles for all files in inputFilePathOrFolder and below having an extension listed in extensionsToParse()
        ' The extensions should be of the form ".TXT" or ".RAW" (i.e. a period then the extension)
        ' If any of the extensions is "*" or ".*" then all files will be processed
        ' If inputFilePathOrFolder contains a filename with a wildcard (* or ?), then that information will be
        '  used to filter the files that are processed
        ' If recurseFoldersMaxLevels is <=0 then we recurse infinitely

        ' Examine inputFilePathOrFolder to see if it contains a filename; if not, assume it points to a folder
        ' First, see if it contains a * or ?
        Try
            Dim inputFolderPath As String

            If String.IsNullOrWhiteSpace(inputFilePathOrFolder) Then
                inputFolderPath = String.Empty
            ElseIf inputFilePathOrFolder.Contains("*") OrElse inputFilePathOrFolder.Contains("?") Then
                ' Copy the path into cleanPath and replace any * or ? characters with _
                Dim cleanPath = inputFilePathOrFolder.Replace("*", "_")
                cleanPath = cleanPath.Replace("?", "_")

                Dim ioFileInfo = New FileInfo(cleanPath)
                If ioFileInfo.Directory.Exists Then
                    inputFolderPath = ioFileInfo.DirectoryName
                Else
                    ' Use the directory that has the .exe file
                    inputFolderPath = GetAppFolderPath()
                End If

                ' Remove any directory information from inputFilePath
                inputFilePathOrFolder = Path.GetFileName(inputFilePathOrFolder)

            Else
                Dim inputfolder = New DirectoryInfo(inputFilePathOrFolder)
                If inputfolder.Exists Then
                    inputFolderPath = inputfolder.FullName
                    inputFilePathOrFolder = "*"
                Else
                    If Not inputfolder.Parent Is Nothing AndAlso inputfolder.Parent.Exists Then
                        inputFolderPath = inputfolder.Parent.FullName
                        inputFilePathOrFolder = Path.GetFileName(inputFilePathOrFolder)
                    Else
                        ' Unable to determine the input folder path
                        inputFolderPath = String.Empty
                    End If
                End If
            End If

            If String.IsNullOrWhiteSpace(inputFolderPath) Then
                ErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
                Return False
            End If

            ' Validate the output folder path
            If Not String.IsNullOrWhiteSpace(outputFolderAlternatePath) Then
                Try
                    Dim ioFolderInfo = New DirectoryInfo(outputFolderAlternatePath)
                    If Not ioFolderInfo.Exists Then ioFolderInfo.Create()
                Catch ex As Exception
                    ErrorCode = eProcessFilesErrorCodes.InvalidOutputFolderPath
                    ShowErrorMessage(
                        "Error validating the alternate output folder path in ProcessFilesAndRecurseFolders:" &
                        ex.Message)
                    Return False
                End Try
            End If

            ' Initialize some parameters
            AbortProcessing = False
            Dim fileProcessCount = 0
            Dim fileProcessFailCount = 0

            ' Call RecurseFoldersWork
            Const recursionLevel = 1
            Dim success = RecurseFoldersWork(inputFolderPath, inputFilePathOrFolder, outputFolderName,
                                             parameterFilePath, outputFolderAlternatePath,
                                             recreateFolderHierarchyInAlternatePath, extensionsToParse,
                                             fileProcessCount, fileProcessFailCount, recursionLevel,
                                             recurseFoldersMaxLevels)

            Return success


        Catch ex As Exception
            HandleException("Error in ProcessFilesAndRecurseFolders", ex)
            Return False
        End Try

    End Function

    Private Function RecurseFoldersWork(inputFolderPath As String, fileNameMatch As String, outputFolderName As String,
       parameterFilePath As String, outputFolderAlternatePath As String,
       recreateFolderHierarchyInAlternatePath As Boolean, extensionsToParse As IList(Of String),
       ByRef fileProcessCount As Integer, ByRef fileProcessFailCount As Integer,
       recursionLevel As Integer, recurseFoldersMaxLevels As Integer) As Boolean
        ' If recurseFoldersMaxLevels is <=0 then we recurse infinitely

        Dim ioInputFolderInfo As DirectoryInfo

        Dim extensionIndex As Integer
        Dim processAllExtensions As Boolean

        Dim outputFolderPathToUse As String
        Dim success As Boolean

        Try
            ioInputFolderInfo = New DirectoryInfo(inputFolderPath)
        Catch ex As Exception
            ' Input folder path error
            HandleException("Error in RecurseFoldersWork", ex)
            ErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
            Return False
        End Try

        Try
            If Not String.IsNullOrWhiteSpace(outputFolderAlternatePath) Then
                If recreateFolderHierarchyInAlternatePath Then
                    outputFolderAlternatePath = Path.Combine(outputFolderAlternatePath, ioInputFolderInfo.Name)
                End If
                outputFolderPathToUse = Path.Combine(outputFolderAlternatePath, outputFolderName)
            Else
                outputFolderPathToUse = outputFolderName
            End If
        Catch ex As Exception
            ' Output file path error
            HandleException("Error in RecurseFoldersWork", ex)
            ErrorCode = eProcessFilesErrorCodes.InvalidOutputFolderPath
            Return False
        End Try

        Try
            ' Validate extensionsToParse()
            For extensionIndex = 0 To extensionsToParse.Count - 1
                If extensionsToParse(extensionIndex) Is Nothing Then
                    extensionsToParse(extensionIndex) = String.Empty
                Else
                    If Not extensionsToParse(extensionIndex).StartsWith(".") Then
                        extensionsToParse(extensionIndex) = "." & extensionsToParse(extensionIndex)
                    End If

                    If extensionsToParse(extensionIndex) = ".*" Then
                        processAllExtensions = True
                        Exit For
                    Else
                        extensionsToParse(extensionIndex) = extensionsToParse(extensionIndex).ToUpper()
                    End If
                End If
            Next extensionIndex
        Catch ex As Exception
            HandleException("Error in RecurseFoldersWork", ex)
            ErrorCode = eProcessFilesErrorCodes.UnspecifiedError
            Return False
        End Try

        Try
            If Not String.IsNullOrWhiteSpace(outputFolderPathToUse) Then
                ' Update the cached output folder path
                mOutputFolderPath = String.Copy(outputFolderPathToUse)
            End If

            ShowMessage("Examining " & inputFolderPath)

            ' Process any matching files in this folder
            success = True
            For Each ioFileMatch As FileInfo In ioInputFolderInfo.GetFiles(fileNameMatch)

                For extensionIndex = 0 To extensionsToParse.Count - 1
                    If processAllExtensions OrElse ioFileMatch.Extension.ToUpper() = extensionsToParse(extensionIndex) Then
                        success = ProcessFile(ioFileMatch.FullName, outputFolderPathToUse, parameterFilePath, True)
                        If Not success Then
                            fileProcessFailCount += 1
                            success = True
                        Else
                            fileProcessCount += 1
                        End If
                        Exit For
                    End If

                    If AbortProcessing Then Exit For

                Next extensionIndex
            Next ioFileMatch
        Catch ex As Exception
            HandleException("Error in RecurseFoldersWork", ex)
            ErrorCode = eProcessFilesErrorCodes.InvalidInputFilePath
            Return False
        End Try

        If Not AbortProcessing Then
            ' If recurseFoldersMaxLevels is <=0 then we recurse infinitely
            '  otherwise, compare recursionLevel to recurseFoldersMaxLevels
            If recurseFoldersMaxLevels <= 0 OrElse recursionLevel <= recurseFoldersMaxLevels Then
                ' Call this function for each of the subfolders of ioInputFolderInfo
                For Each ioSubFolderInfo As DirectoryInfo In ioInputFolderInfo.GetDirectories()
                    success = RecurseFoldersWork(ioSubFolderInfo.FullName, fileNameMatch, outputFolderName,
                      parameterFilePath, outputFolderAlternatePath,
                      recreateFolderHierarchyInAlternatePath, extensionsToParse,
                      fileProcessCount, fileProcessFailCount,
                      recursionLevel + 1, recurseFoldersMaxLevels)

                    If Not success Then Exit For
                Next ioSubFolderInfo
            End If
        End If

        Return success

    End Function

    Protected Sub SetBaseClassErrorCode(eNewErrorCode As eProcessFilesErrorCodes)
        ErrorCode = eNewErrorCode
    End Sub

    '' The following functions should be placed in any derived class
    '' Cannot define as MustOverride since it contains a customized enumerated type (eDerivedClassErrorCodes) in the function declaration

    ''Private Sub SetLocalErrorCode(eNewErrorCode As eDerivedClassErrorCodes)
    ''    SetLocalErrorCode(eNewErrorCode, False)
    ''End Sub

    ''Private Sub SetLocalErrorCode(eNewErrorCode As eDerivedClassErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
    ''    If leaveExistingErrorCodeUnchanged AndAlso mLocalErrorCode <> eDerivedClassErrorCodes.NoError Then
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
