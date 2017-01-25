Option Strict On

Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml

''' <summary>
''' This class is used to read or write settings in an Xml settings file
''' Based on a class from the DMS Analysis Manager software written by Dave Clark and Gary Kiebel (PNNL, Richland, WA)
''' Additional features added by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in October 2003
''' Copyright 2005, Battelle Memorial Institute
'''
''' Updated in October 2004 to truly be case-insensitive if IsCaseSensitive = False when calling LoadSettings()
''' Updated in August 2007 to remove the PRISM.Logging functionality and to include class XMLFileReader inside class XmlSettingsFileAccessor
''' Updated in December 2010 to rename objects from Ini to XML
''' </summary>
Public Class XmlSettingsFileAccessor

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        mCaseSensitive = False
        mSectionNames = New Dictionary(Of String, String)

        mCachedSection = New udtRecentSectionType
        With mCachedSection
            .SectionName = String.Empty
            .KeyNames = New Dictionary(Of String, String)
        End With
    End Sub

    Private Structure udtRecentSectionType
        ''' <summary>
        ''' Stores the section name whose keys are cached; the section name is capitalized identically to that actually present in the Xml file
        ''' </summary>
        Public SectionName As String

        ''' <summary>
        ''' Keys for this section
        ''' Keys in KeyNames are the lower case version of the name in the file if mCaseSensitive is true, or the actual version if mCaseSensitive is false
        ''' Values in KeyNames are the actual way the key name is capitalized in the Xml file
        ''' </summary>
        Public KeyNames As Dictionary(Of String, String)
    End Structure

    ' XML file reader
    ' Call LoadSettings to initialize, even if simply saving settings
    Private m_XMLFilePath As String = ""
    Private WithEvents m_XMLFileAccessor As XMLFileReader

    Private mCaseSensitive As Boolean

    ' When mCaseSensitive = False, then mSectionNames stores the mapping between lowercase section name and actual section name stored in file
    '   If section is present more than once in file, then only grabs the first occurence of the section
    ' When mCaseSensitive = True, then the mappings in mSectionNames are effectively not used
    Private ReadOnly mSectionNames As Dictionary(Of String, String)
    Private mCachedSection As udtRecentSectionType

    Public Event InformationMessage(msg As String)

    ''' <summary>
    ''' Loads the settings for the defined Xml Settings File.  Assumes names are not case sensitive
    ''' </summary>
    ''' <return>The function returns a boolean that shows if the file was successfully loaded.</return>
    Public Function LoadSettings() As Boolean
        Return LoadSettings(m_XMLFilePath, False)
    End Function

    ''' <summary>
    ''' Loads the settings for the defined Xml Settings File.   Assumes names are not case sensitive
    ''' </summary>
    ''' <param name="XmlSettingsFilePath">The path to the XML settings file.</param>
    ''' <return>True if the file was successfully loaded (or created)</return>
    ''' <remarks>The XML file will be created if it does not exist</remarks>
    Public Function LoadSettings(XmlSettingsFilePath As String) As Boolean
        Return LoadSettings(XmlSettingsFilePath, False)
    End Function

    ''' <summary>
    ''' Loads the settings for the defined Xml Settings File
    ''' </summary>
    ''' <param name="XmlSettingsFilePath">The path to the XML settings file.</param>
    ''' <param name="IsCaseSensitive">Case sensitive names if True.  Non-case sensitive if false.</param>
    ''' <return>True if the file was successfully loaded (or created)</return>
    ''' <remarks>The XML file will be created if it does not exist</remarks>
    Public Function LoadSettings(XmlSettingsFilePath As String, IsCaseSensitive As Boolean) As Boolean
        mCaseSensitive = IsCaseSensitive

        m_XMLFilePath = XmlSettingsFilePath

        ' Note: Always set IsCaseSensitive = True for XMLFileReader's constructor since this class handles 
        '       case sensitivity mapping internally
        m_XMLFileAccessor = New XMLFileReader(m_XMLFilePath, True)
        If m_XMLFileAccessor Is Nothing Then
            Return False
        ElseIf m_XMLFileAccessor.Initialized Then
            CacheSectionNames()
            Return True
        Else
            Return False
        End If

    End Function

    Public Function ManualParseXmlOrIniFile(strFilePath As String) As Boolean
        m_XMLFilePath = strFilePath

        ' Note: Always set IsCaseSensitive = True for XMLFileReader's constructor since this class handles 
        '       case sensitivity mapping internally
        m_XMLFileAccessor = New XMLFileReader(String.Empty, True)

        If m_XMLFileAccessor Is Nothing Then
            Return False
        ElseIf m_XMLFileAccessor.ManualParseXmlOrIniFile(strFilePath) Then
            If m_XMLFileAccessor.Initialized Then
                CacheSectionNames()
                Return True
            End If
        End If

        Return False

    End Function

    ''' <summary>
    ''' Saves the settings for the defined Xml Settings File.  Note that you must call LoadSettings to initialize the class prior to setting any values.
    ''' </summary>
    ''' <return>The function returns a boolean that shows if the file was successfully saved.</return>
    Public Function SaveSettings() As Boolean

        If m_XMLFileAccessor Is Nothing Then
            Return False
        ElseIf m_XMLFileAccessor.Initialized Then
            m_XMLFileAccessor.OutputFilename = m_XMLFilePath
            m_XMLFileAccessor.Save()
            Return True
        Else
            Return False
        End If

    End Function

    ''' <summary>
    ''' Checks if a section is present in the settings file.
    ''' </summary>
    ''' <param name="sectionName">The name of the section to look for.</param>
    ''' <return>The function returns a boolean that shows if the section is present.</return>
    Public Function SectionPresent(sectionName As String) As Boolean

        Dim strSections = m_XMLFileAccessor.AllSections

        For Each candidateSectionName In strSections
            If SetNameCase(candidateSectionName) = SetNameCase(sectionName) Then Return True
        Next

        Return False

    End Function

    Private Function CacheKeyNames(sectionName As String) As Boolean
        ' Looks up the Key Names for the given section, storing them in mCachedSection
        ' This is done so that this class will know the correct capitalization for the key names

        Dim strKeys As List(Of String)

        ' Lookup the correct capitalization for sectionName (only truly important if mCaseSensitive = False)
        Dim sectionNameInFile = GetCachedSectionName(sectionName)
        If String.IsNullOrWhiteSpace(sectionNameInFile) Then Return False

        Try
            ' Grab the keys for sectionName
            strKeys = m_XMLFileAccessor.AllKeysInSection(sectionNameInFile)
        Catch ex As Exception
            ' Invalid section name; do not update anything
            Return False
        End Try

        If strKeys Is Nothing Then
            Return False
        End If

        ' Update mCachedSection with the key names for the given section
        With mCachedSection
            .SectionName = sectionNameInFile
            .KeyNames.Clear()

            For Each keyName In strKeys
                ' Change the key name to lowercase if mCaseSensitive is true
                Dim strKeyNameToStore = SetNameCase(keyName)

                If Not .KeyNames.Keys.Contains(strKeyNameToStore) Then
                    .KeyNames.Add(strKeyNameToStore, keyName)
                Else
                    Console.WriteLine("Note: ignoring duplicate key in the XML file: " & keyName)
                End If

            Next
        End With

        Return True

    End Function

    Private Sub CacheSectionNames()
        ' Looks up the Section Names in the XML file
        ' This is done so that this class will know the correct capitalization for the section names

        Dim strSections = m_XMLFileAccessor.AllSections

        mSectionNames.Clear()

        For Each section In strSections
            Dim strSectionNameToStore = SetNameCase(section)

            If Not mSectionNames.ContainsKey(strSectionNameToStore) Then
                mSectionNames.Add(strSectionNameToStore, section)
            Else
                Console.WriteLine("Note: ignoring duplicate section in the XML file: " & section)
            End If

        Next

    End Sub

    Private Function GetCachedKeyName(sectionName As String, keyName As String) As String
        ' Looks up the correct capitalization for key keyName in section sectionName
        ' Returns String.Empty if not found

        Dim blnSuccess As Boolean
        Dim sectionNameInFile As String
        Dim keyNameToFind As String

        ' Lookup the correct capitalization for sectionName (only truly important if mCaseSensitive = False)
        sectionNameInFile = GetCachedSectionName(sectionName)
        If String.IsNullOrWhiteSpace(sectionNameInFile) Then Return String.Empty

        If mCachedSection.SectionName = sectionNameInFile Then
            blnSuccess = True
        Else
            ' Update the keys for sectionName
            blnSuccess = CacheKeyNames(sectionName)
        End If

        If blnSuccess Then
            With mCachedSection
                keyNameToFind = SetNameCase(keyName)
                If .KeyNames.ContainsKey(keyNameToFind) Then
                    Return .KeyNames(keyNameToFind)
                Else
                    Return String.Empty
                End If
            End With
        Else
            Return String.Empty
        End If
    End Function

    Private Function GetCachedSectionName(sectionName As String) As String
        ' Looks up the correct capitalization for sectionName
        ' Returns String.Empty if not found

        Dim sectionNameToFind = SetNameCase(sectionName)

        If mSectionNames.ContainsKey(sectionNameToFind) Then
            Return mSectionNames(sectionNameToFind)
        Else
            Return String.Empty
        End If

    End Function

    ''' <summary>
    ''' Return sectionName as is if mCaseSensitive is true, or return it lowercase if false
    ''' </summary>
    ''' <param name="sectionName">The name to be set.</param>
    ''' <return>The function returns a string.</return>
    Private Function SetNameCase(sectionName As String) As String
        If (mCaseSensitive) Then
            Return sectionName
        Else
            Return sectionName.ToLower()
        End If
    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns the name of the "value" attribute as a String.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As String, <Out()> Optional ByRef valueNotPresent As Boolean = False) As String
        Dim strResult As String = String.Empty
        Dim sectionNameInFile As String
        Dim keyNameInFile As String
        Dim blnValueFound As Boolean

        If mCaseSensitive Then
            strResult = m_XMLFileAccessor.GetXMLValue(sectionName, keyName)
            If Not strResult Is Nothing Then blnValueFound = True
        Else
            sectionNameInFile = GetCachedSectionName(sectionName)
            If sectionNameInFile.Length > 0 Then
                keyNameInFile = GetCachedKeyName(sectionName, keyName)
                If keyNameInFile.Length > 0 Then
                    strResult = m_XMLFileAccessor.GetXMLValue(sectionNameInFile, keyNameInFile)
                    If Not strResult Is Nothing Then blnValueFound = True
                End If
            End If
        End If

        If strResult Is Nothing OrElse Not blnValueFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            Return strResult
        End If
    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns boolean True if the "value" attribute is "true".  Otherwise, returns boolean False.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As Boolean, <Out()> Optional ByRef valueNotPresent As Boolean = False) As Boolean
        Dim strResult As String
        Dim blnNotFound = False

        strResult = Me.GetParam(sectionName, keyName, valueIfMissing.ToString, blnNotFound)
        If strResult Is Nothing OrElse blnNotFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            If strResult.ToLower = "true" Then
                Return True
            Else
                Return False
            End If
        End If
    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns the name of the "value" attribute as a Short.  If "value" is "true" returns -1.  If "value" is "false" returns 0.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As Short, <Out()> Optional ByRef valueNotPresent As Boolean = False) As Short
        Dim strResult As String
        Dim blnNotFound = False
        Dim intValue As Short

        strResult = Me.GetParam(sectionName, keyName, valueIfMissing.ToString, blnNotFound)
        If strResult Is Nothing OrElse blnNotFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            Try
                If Short.TryParse(strResult, intValue) Then
                    Return intValue
                ElseIf strResult.ToLower = "true" Then
                    Return -1
                ElseIf strResult.ToLower = "false" Then
                    Return 0
                Else
                    valueNotPresent = True
                    Return valueIfMissing
                End If
            Catch ex As Exception
                valueNotPresent = True
                Return valueIfMissing
            End Try
        End If

    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns the name of the "value" attribute as an Integer.  If "value" is "true" returns -1.  If "value" is "false" returns 0.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As Integer, <Out()> Optional ByRef valueNotPresent As Boolean = False) As Integer
        Dim strResult As String
        Dim blnNotFound = False
        Dim intValue As Integer

        strResult = Me.GetParam(sectionName, keyName, valueIfMissing.ToString, blnNotFound)
        If strResult Is Nothing OrElse blnNotFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            Try
                If Integer.TryParse(strResult, intValue) Then
                    Return intValue
                ElseIf strResult.ToLower = "true" Then
                    Return -1
                ElseIf strResult.ToLower = "false" Then
                    Return 0
                Else
                    valueNotPresent = True
                    Return valueIfMissing
                End If
            Catch ex As Exception
                valueNotPresent = True
                Return valueIfMissing
            End Try
        End If

    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns the name of the "value" attribute as a Long.  If "value" is "true" returns -1.  If "value" is "false" returns 0.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As Long, <Out()> Optional ByRef valueNotPresent As Boolean = False) As Int64
        Dim strResult As String
        Dim blnNotFound = False
        Dim intValue As Int64

        strResult = Me.GetParam(sectionName, keyName, valueIfMissing.ToString, blnNotFound)
        If strResult Is Nothing OrElse blnNotFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            Try
                If Int64.TryParse(strResult, intValue) Then
                    Return intValue
                ElseIf strResult.ToLower = "true" Then
                    Return -1
                ElseIf strResult.ToLower = "false" Then
                    Return 0
                Else
                    valueNotPresent = True
                    Return valueIfMissing
                End If
            Catch ex As Exception
                valueNotPresent = True
                Return valueIfMissing
            End Try
        End If

    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns the name of the "value" attribute as a Single.  If "value" is "true" returns -1.  If "value" is "false" returns 0.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As Single, <Out()> Optional ByRef valueNotPresent As Boolean = False) As Single
        Dim strResult As String
        Dim blnNotFound = False
        Dim sngValue As Single

        strResult = Me.GetParam(sectionName, keyName, valueIfMissing.ToString, blnNotFound)
        If strResult Is Nothing OrElse blnNotFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            Try
                If Single.TryParse(strResult, sngValue) Then
                    Return sngValue
                ElseIf strResult.ToLower = "true" Then
                    Return -1
                ElseIf strResult.ToLower = "false" Then
                    Return 0
                Else
                    valueNotPresent = True
                    Return valueIfMissing
                End If
            Catch ex As Exception
                valueNotPresent = True
                Return valueIfMissing
            End Try
        End If

    End Function

    ''' <summary>
    ''' Gets the name of the "value" attribute in section "sectionName".
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="valueIfMissing">Value to return if "sectionName" or "keyName" is missing.</param>
    ''' <param name="valueNotPresent">Set to True if "sectionName" or "keyName" is missing.  Returned ByRef.</param>
    ''' <return>The function returns the name of the "value" attribute as a Double.  If "value" is "true" returns -1.  If "value" is "false" returns 0.</return>
    Public Function GetParam(sectionName As String, keyName As String, valueIfMissing As Double, <Out()> Optional ByRef valueNotPresent As Boolean = False) As Double
        Dim strResult As String
        Dim blnNotFound = False
        Dim dblValue As Double

        strResult = Me.GetParam(sectionName, keyName, valueIfMissing.ToString, blnNotFound)
        If strResult Is Nothing OrElse blnNotFound Then
            valueNotPresent = True
            Return valueIfMissing
        Else
            valueNotPresent = False
            Try
                If Double.TryParse(strResult, dblValue) Then
                    Return dblValue
                ElseIf strResult.ToLower = "true" Then
                    Return -1
                ElseIf strResult.ToLower = "false" Then
                    Return 0
                Else
                    valueNotPresent = True
                    Return valueIfMissing
                End If
            Catch ex As Exception
                valueNotPresent = True
                Return valueIfMissing
            End Try
        End If

    End Function

    ''' <summary>
    ''' Legacy function name; calls SetXMLFilePath
    ''' </summary>
    <Obsolete("Use SetXMLFilePath")>
    Public Sub SetIniFilePath(XmlSettingsFilePath As String)
        SetXMLFilePath(XmlSettingsFilePath)
    End Sub

    ''' <summary>
    ''' Sets the path to the Xml Settings File.
    ''' </summary>
    ''' <param name="XmlSettingsFilePath">The path to the XML settings file.</param>
    Public Sub SetXMLFilePath(XmlSettingsFilePath As String)
        m_XMLFilePath = XmlSettingsFilePath
    End Sub

    ''' <summary>
    ''' Sets a new String value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As String) As Boolean
        Dim sectionNameInFile As String
        Dim keyNameInFile As String

        If Not mCaseSensitive Then
            sectionNameInFile = GetCachedSectionName(sectionName)
            If sectionNameInFile.Length > 0 Then
                keyNameInFile = GetCachedKeyName(sectionName, keyName)
                If keyNameInFile.Length > 0 Then
                    ' Section and Key are present; update them
                    Return m_XMLFileAccessor.SetXMLValue(sectionNameInFile, keyNameInFile, newValue)
                Else
                    ' Section is present, but the Key isn't; add teh key
                    Return m_XMLFileAccessor.SetXMLValue(sectionNameInFile, keyName, newValue)
                End If
            End If
        End If

        ' If we get here, then either mCaseSensitive = True or the section and key weren't found
        Return m_XMLFileAccessor.SetXMLValue(sectionName, keyName, newValue)

    End Function

    ''' <summary>
    ''' Sets a new Boolean value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As Boolean) As Boolean
        Return Me.SetParam(sectionName, keyName, CStr(newValue))
    End Function

    ''' <summary>
    ''' Sets a new Short value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As Short) As Boolean
        Return Me.SetParam(sectionName, keyName, CStr(newValue))
    End Function

    ''' <summary>
    ''' Sets a new Integer value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As Integer) As Boolean
        Return Me.SetParam(sectionName, keyName, CStr(newValue))
    End Function

    ''' <summary>
    ''' Sets a new Long value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As Long) As Boolean
        Return Me.SetParam(sectionName, keyName, CStr(newValue))
    End Function

    ''' <summary>
    ''' Sets a new Single value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As Single) As Boolean
        Return Me.SetParam(sectionName, keyName, CStr(newValue))
    End Function

    ''' <summary>
    ''' Sets a new Double value for the "value" attribute.
    ''' </summary>
    ''' <param name="sectionName">The name of the section.</param>
    ''' <param name="keyName">The name of the key.</param>
    ''' <param name="newValue">The new value for the "value".</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function SetParam(sectionName As String, keyName As String, newValue As Double) As Boolean
        Return Me.SetParam(sectionName, keyName, CStr(newValue))
    End Function

    ''' <summary>
    ''' Renames a section.
    ''' </summary>
    ''' <param name="sectionNameOld">The name of the old XML section name.</param>
    ''' <param name="sectionNameNew">The new name for the XML section.</param>
    ''' <return>The function returns a boolean that shows if the change was done.</return>
    Public Function RenameSection(sectionNameOld As String, sectionNameNew As String) As Boolean

        Dim strSectionName As String

        If Not mCaseSensitive Then
            strSectionName = GetCachedSectionName(sectionNameOld)
            If strSectionName.Length > 0 Then
                Return m_XMLFileAccessor.SetXMLSection(strSectionName, sectionNameNew)
            End If
        End If

        ' If we get here, then either mCaseSensitive = True or the section wasn't found using GetCachedSectionName
        Return m_XMLFileAccessor.SetXMLSection(sectionNameOld, sectionNameNew)

    End Function

    Private Sub FileAccessorInfoMessageEvent(msg As String) Handles m_XMLFileAccessor.InformationMessage
        RaiseEvent InformationMessage(msg)
    End Sub


    ''' <summary>
    ''' Tools to manipulates XML Settings files.
    ''' </summary>
    Protected Class XMLFileReader

        Enum XMLItemTypeEnum
            GetKeys = 0
            GetValues = 1
            GetKeysAndValues = 2
        End Enum

        Private m_XmlFilename As String
        Private m_XmlDoc As XmlDocument

        Private m_sections As List(Of String) = New List(Of String)
        Private m_CaseSensitive As Boolean = False
        Private m_SaveFilename As String
        Private m_initialized As Boolean = False

        Public NotifyOnEvent As Boolean
        Public NotifyOnException As Boolean

        Public Event InformationMessage(msg As String)

        ''' <summary>
        ''' Constructor: Initializes a new instance of the XMLFileReader (non case-sensitive)
        ''' </summary>
        ''' <param name="XmlFilename">The name of the XML file.</param>
        Public Sub New(XmlFilename As String)
            NotifyOnException = False
            InitXMLFileReader(XmlFilename, False)
        End Sub

        ''' <summary>
        ''' Constructor: Initializes a new instance of the XMLFileReader.
        ''' </summary>
        ''' <param name="XmlFilename">The name of the XML file.</param>
        ''' <param name="IsCaseSensitive">Case sensitive as boolean.</param>
        ''' <remarks>The XML file will be created if it does not exist</remarks>
        Public Sub New(XmlFilename As String, IsCaseSensitive As Boolean)
            NotifyOnException = True
            InitXMLFileReader(XmlFilename, IsCaseSensitive)
        End Sub

        ''' <summary>
        ''' This routine is called by each of the constructors to make the actual assignments.
        ''' </summary>
        ''' <remarks>The XML file will be created if it does not exist</remarks>
        Private Sub InitXMLFileReader(strXmlFilename As String, IsCaseSensitive As Boolean)
            m_CaseSensitive = IsCaseSensitive
            m_XmlDoc = New XmlDocument

            If String.IsNullOrEmpty(strXmlFilename) Then
                Return
            End If

            ' Try to load the file as an XML file
            Try
                If Not File.Exists(strXmlFilename) Then
                    ManualParseXmlOrIniFile(strXmlFilename)
                    Return
                End If

                m_XmlDoc.Load(strXmlFilename)
                UpdateSections()
                m_XmlFilename = strXmlFilename
                m_initialized = True

            Catch
                ' Exception occurred parsing XmlFilename 
                ' Manually parse the file line-by-line
                ManualParseXmlOrIniFile(strXmlFilename)
            End Try
        End Sub

        ''' <summary>
        ''' Legacy property; calls XmlFilename
        ''' </summary>
        Public ReadOnly Property IniFilename() As String
            Get
                Return XmlFilename()
            End Get
        End Property

        ''' <summary>
        ''' This routine returns the name of the ini file.
        ''' </summary>
        ''' <return>The function returns the name of ini file.</return>
        Public ReadOnly Property XmlFilename() As String
            Get
                If Not Initialized Then
                    Return String.Empty
                Else
                    Return (m_XmlFilename)
                End If
            End Get
        End Property

        ''' <summary>
        ''' Returns a boolean showing if the file was initialized or not.
        ''' </summary>
        Public ReadOnly Property Initialized() As Boolean
            Get
                Return m_initialized
            End Get
        End Property

        ''' <summary>
        ''' Returns a boolean showing if the name is case sensitive or not.
        ''' </summary>
        Public ReadOnly Property CaseSensitive() As Boolean
            Get
                Return m_CaseSensitive
            End Get
        End Property

        ''' <summary>
        ''' Return sectionName as is if CaseSensitive is true, or return it lowercase if false
        ''' </summary>
        ''' <param name="sectionName">The name to be set.</param>
        Private Function SetNameCase(sectionName As String) As String
            If (CaseSensitive) Then
                Return sectionName
            Else
                Return sectionName.ToLower()
            End If
        End Function

        ''' <summary>
        ''' Returns the root element of the XML document
        ''' </summary>
        Private Function GetRoot() As XmlElement
            Return m_XmlDoc.DocumentElement
        End Function

        ''' <summary>
        ''' Gets the last section.
        ''' </summary>
        ''' <return>The function returns the last section as System.Xml.XmlElement.</return>
        Private Function GetLastSection() As XmlElement
            If m_sections.Count = 0 Then
                Return GetRoot()
            Else
                Return GetSection(m_sections.Last())
            End If
        End Function

        ''' <summary>
        ''' Gets a section as System.Xml.XmlElement.
        ''' </summary>
        ''' <param name="sectionName">The name of a section.</param>
        ''' <return>The function returns a section as System.Xml.XmlElement.</return>
        Private Function GetSection(sectionName As String) As XmlElement
            If Not String.IsNullOrWhiteSpace(sectionName) Then
                sectionName = SetNameCase(sectionName)
                Return CType(m_XmlDoc.SelectSingleNode("//section[@name='" & sectionName & "']"), XmlElement)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets an item.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <return>The function returns a XML element.</return>
        Private Function GetItem(sectionName As String, keyName As String) As XmlElement
            Dim section As XmlElement
            If Not String.IsNullOrWhiteSpace(keyName) Then
                keyName = SetNameCase(keyName)
                section = GetSection(sectionName)
                If (Not section Is Nothing) Then
                    Return CType(section.SelectSingleNode("item[@key='" + keyName + "']"), XmlElement)
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Legacy function name; calls SetXMLSection
        ''' </summary>
        Public Function SetIniSection(oldSection As String, newSection As String) As Boolean
            Return SetXMLSection(oldSection, newSection)
        End Function

        ''' <summary>
        ''' Sets the ini section name.
        ''' </summary>
        ''' <param name="oldSection">The name of the old ini section name.</param>
        ''' <param name="newSection">The new name for the ini section.</param>
        ''' <return>The function returns a boolean that shows if the change was done.</return>
        Public Function SetXMLSection(oldSection As String, newSection As String) As Boolean
            Dim section As XmlElement
            If Not Initialized Then
                Throw New XMLFileReaderNotInitializedException
            End If
            If Not String.IsNullOrWhiteSpace(newSection) Then
                section = GetSection(oldSection)
                If (Not section Is Nothing) Then
                    section.SetAttribute("name", SetNameCase(newSection))
                    UpdateSections()
                    Return True
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Legacy function name; calls SetXMLValue
        ''' </summary>
        Public Function SetIniValue(sectionName As String, keyName As String, newValue As String) As Boolean
            Return SetXMLValue(sectionName, keyName, newValue)
        End Function

        ''' <summary>
        ''' Sets a new value for the "value" attribute.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <param name="newValue">The new value for the "value".</param>
        ''' <return>The function returns a boolean that shows if the change was done.</return>
        Public Function SetXMLValue(sectionName As String, keyName As String, newValue As String) As Boolean
            Dim item As XmlElement
            Dim section As XmlElement
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            section = GetSection(sectionName)
            If section Is Nothing Then
                If CreateSection(sectionName) Then
                    section = GetSection(sectionName)
                    ' exit if keyName is Nothing or blank
                    If (keyName Is Nothing) OrElse (keyName = "") Then
                        Return True
                    End If
                Else
                    ' can't create section
                    Return False
                End If
            End If
            If keyName Is Nothing Then
                ' Delete the section
                Return DeleteSection(sectionName)
            End If

            item = GetItem(sectionName, keyName)
            If Not item Is Nothing Then
                If newValue Is Nothing Then
                    ' delete this item
                    Return DeleteItem(sectionName, keyName)
                Else
                    ' add or update the value attribute
                    item.SetAttribute("value", newValue)
                    Return True
                End If
            Else
                ' try to create the item
                If (keyName <> "") AndAlso (Not newValue Is Nothing) Then
                    ' construct a new item (blank values are OK)
                    item = m_XmlDoc.CreateElement("item")
                    item.SetAttribute("key", SetNameCase(keyName))
                    item.SetAttribute("value", newValue)
                    section.AppendChild(item)
                    Return True
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' The function deletes a section in the file.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <return>The function returns a boolean that shows if the delete was completed.</return>
        Private Function DeleteSection(sectionName As String) As Boolean
            Dim section As XmlElement = GetSection(sectionName)
            If Not section Is Nothing Then
                section.ParentNode.RemoveChild(section)
                UpdateSections()
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' The function deletes a item in a specific section.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <return>The function returns a boolean that shows if the delete was completed.</return>
        Private Function DeleteItem(sectionName As String, keyName As String) As Boolean
            Dim item As XmlElement = GetItem(sectionName, keyName)
            If Not item Is Nothing Then
                item.ParentNode.RemoveChild(item)
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Legacy function name; calls SetXmlKey
        ''' </summary>
        Public Function SetIniKey(sectionName As String, keyName As String, newValue As String) As Boolean
            Return SetXmlKey(sectionName, keyName, newValue)
        End Function

        ''' <summary>
        ''' Sets a new value for the "key" attribute.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <param name="newValue">The new value for the "key".</param>
        ''' <return>The function returns a boolean that shows if the change was done.</return>
        Public Function SetXmlKey(sectionName As String, keyName As String, newValue As String) As Boolean
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Dim item As XmlElement = GetItem(sectionName, keyName)
            If Not item Is Nothing Then
                item.SetAttribute("key", SetNameCase(newValue))
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Legacy function name; calls GetXMLValue
        ''' </summary>
        Public Function GetIniValue(sectionName As String, keyName As String) As String
            Return GetXMLValue(sectionName, keyName)
        End Function

        ''' <summary>
        ''' Get the value for the given key in the given section
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <return>The function returns the name of the "value" attribute.</return>
        Public Function GetXMLValue(sectionName As String, keyName As String) As String
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Dim node As XmlNode = GetItem(sectionName, keyName)
            If Not node Is Nothing Then
                Return (node.Attributes.GetNamedItem("value").Value)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Legacy function name; calls GetXmlSectionComments
        ''' </summary>
        Public Function GetIniComments(sectionName As String) As List(Of String)
            Return GetXmlSectionComments(sectionName)
        End Function

        ''' <summary>
        ''' Gets the comments for a section name.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        '''<return>The function returns a string collection with comments</return>
        Public Function GetXmlSectionComments(sectionName As String) As List(Of String)
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Dim sc = New List(Of String)
            Dim target As XmlNode

            If sectionName Is Nothing Then
                target = m_XmlDoc.DocumentElement
            Else
                target = GetSection(sectionName)
            End If
            If Not target Is Nothing Then
                Dim nodes = target.SelectNodes("comment")
                If nodes.Count > 0 Then
                    For Each node As XmlNode In nodes
                        sc.Add(node.InnerText)
                    Next
                End If
            End If
            Return sc
        End Function

        ''' <summary>
        ''' Legacy function name; calls SetXMLComments
        ''' </summary>
        Public Function SetIniComments(sectionName As String, comments As List(Of String)) As Boolean
            Return SetXMLComments(sectionName, comments)
        End Function

        ''' <summary>
        ''' Sets a the comments for a section name.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="comments">A string collection.</param>
        '''<return>The function returns a Boolean that shows if the change was done.</return>
        Public Function SetXMLComments(sectionName As String, comments As List(Of String)) As Boolean
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Dim target As XmlNode

            If sectionName Is Nothing Then
                target = m_XmlDoc.DocumentElement
            Else
                target = GetSection(sectionName)
            End If

            If Not target Is Nothing Then
                Dim nodes = target.SelectNodes("comment")
                For Each node As XmlNode In nodes
                    target.RemoveChild(node)
                Next

                For Each s In comments
                    Dim node = m_XmlDoc.CreateElement("comment")
                    node.InnerText = s
                    Dim nodeLastComment = CType(target.SelectSingleNode("comment[last()]"), XmlElement)
                    If nodeLastComment Is Nothing Then
                        target.PrependChild(node)
                    Else
                        target.InsertAfter(node, nodeLastComment)
                    End If
                Next
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' The subroutine updades the sections.
        ''' </summary>
        Private Sub UpdateSections()
            m_sections = New List(Of String)
            For Each node As XmlElement In m_XmlDoc.SelectNodes("sections/section")
                m_sections.Add(node.GetAttribute("name"))
            Next
        End Sub

        ''' <summary>
        ''' The subroutine gets the sections.
        ''' </summary>
        ''' <return>The subroutine returns a strin collection of sections.</return>
        Public ReadOnly Property AllSections() As List(Of String)
            Get
                If Not Initialized Then
                    Return New List(Of String)
                Else
                    Return m_sections
                End If
            End Get
        End Property

        ''' <summary>
        ''' Gets a collection of items for a section name.
        ''' </summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="itemType">Item type.</param>
        ''' <return>The function returns a string colection of items in a section.</return>
        Private Function GetItemsInSection(sectionName As String, itemType As XMLItemTypeEnum) As List(Of String)

            Dim items = New List(Of String)
            Dim section As XmlNode = GetSection(sectionName)

            If section Is Nothing Then
                Return Nothing
            Else
                Dim nodes = section.SelectNodes("item")
                If nodes.Count > 0 Then
                    For Each node As XmlNode In nodes
                        Select Case itemType
                            Case XMLItemTypeEnum.GetKeys
                                items.Add(node.Attributes.GetNamedItem("key").Value)
                            Case XMLItemTypeEnum.GetValues
                                items.Add(node.Attributes.GetNamedItem("value").Value)
                            Case XMLItemTypeEnum.GetKeysAndValues
                                items.Add(node.Attributes.GetNamedItem("key").Value & "=" &
                                node.Attributes.GetNamedItem("value").Value)
                        End Select
                    Next
                End If
                Return items
            End If
        End Function

        ''' <summary>The funtions gets a collection of keys in a section.</summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <return>The function returns a string colection of all the keys in a section.</return>
        Public Function AllKeysInSection(sectionName As String) As List(Of String)
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Return GetItemsInSection(sectionName, XMLItemTypeEnum.GetKeys)
        End Function

        ''' <summary>The funtions gets a collection of values in a section.</summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <return>The function returns a string colection of all the values in a section.</return>
        Public Function AllValuesInSection(sectionName As String) As List(Of String)
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Return GetItemsInSection(sectionName, XMLItemTypeEnum.GetValues)
        End Function

        ''' <summary>The funtions gets a collection of items in a section.</summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <return>The function returns a string colection of all the items in a section.</return>
        Public Function AllItemsInSection(sectionName As String) As List(Of String)
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            Return (GetItemsInSection(sectionName, XMLItemTypeEnum.GetKeysAndValues))
        End Function

        ''' <summary>The funtions gets a custom attribute name.</summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <param name="attributeName">The name of the attribute.</param>
        ''' <return>The function returns a string.</return>
        Public Function GetCustomIniAttribute(sectionName As String, keyName As String, attributeName As String) As String

            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            If (Not attributeName Is Nothing) AndAlso (attributeName <> "") Then
                Dim node = GetItem(sectionName, keyName)
                If Not node Is Nothing Then
                    attributeName = SetNameCase(attributeName)
                    Return node.GetAttribute(attributeName)
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>The funtions sets a custom attribute name.</summary>
        ''' <param name="sectionName">The name of the section.</param>
        ''' <param name="keyName">The name of the key.</param>
        ''' <param name="attributeName">The name of the attribute.</param>
        ''' <param name="attributeValue">The value of the attribute.</param>
        ''' <return>The function returns a Boolean.</return>
        Public Function SetCustomIniAttribute(sectionName As String, keyName As String, attributeName As String, attributeValue As String) As Boolean

            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            If attributeName <> "" Then
                Dim node = GetItem(sectionName, keyName)
                If Not node Is Nothing Then
                    Try
                        If attributeValue Is Nothing Then
                            ' delete the attribute
                            node.RemoveAttribute(attributeName)
                            Return True
                        Else
                            attributeName = SetNameCase(attributeName)
                            node.SetAttribute(attributeName, attributeValue)
                            Return True
                        End If

                    Catch e As Exception
                        If NotifyOnException Then
                            Throw New Exception("Failed to create item.")
                        End If
                    End Try
                End If
                Return False
            End If
            Return False
        End Function

        ''' <summary>The funtions creates a section name.</summary>
        ''' <param name="sectionName">The name of the section to be created.</param>
        ''' <return>The function returns a Boolean.</return>
        Private Function CreateSection(sectionName As String) As Boolean
            If (Not sectionName Is Nothing) AndAlso (sectionName <> "") Then
                sectionName = SetNameCase(sectionName)
                Try
                    Dim node = m_XmlDoc.CreateElement("section")
                    Dim attribute = m_XmlDoc.CreateAttribute("name")
                    attribute.Value = SetNameCase(sectionName)
                    node.Attributes.SetNamedItem(attribute)
                    m_XmlDoc.DocumentElement.AppendChild(node)
                    m_sections.Add(attribute.Value)
                    Return True
                Catch e As Exception
                    If NotifyOnException Then
                        Throw New Exception("Failed to create item.")
                    End If
                    Return False
                End Try
            End If
            Return False
        End Function

        ''' <summary>
        ''' Manually read a XML or .INI settings file line-by-line, extracting out any settings in the expected format
        ''' </summary>
        ''' <param name="strFilePath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ManualParseXmlOrIniFile(strFilePath As String) As Boolean

            ' Create a new, blank XML document
            m_XmlDoc.LoadXml("<?xml version=""1.0"" encoding=""UTF-8""?><sections></sections>")

            Try
                Dim fi = New FileInfo(strFilePath)

                If (fi.Exists) Then
                    ' Read strFilePath line-by-line to see if it has any .Ini style settings
                    ' For example:
                    '   [SectionName]
                    '   Setting1=ValueA
                    '   Setting2=ValueB

                    ' Also look for XML-style entries
                    ' For example:
                    '   <section name="SectionName">
                    '     <item key="Setting1" value="ValueA" />
                    '   </section>

                    Using srInFile = New StreamReader(New FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))

                        Do While srInFile.Peek() > -1
                            Dim s = srInFile.ReadLine()

                            ' Try to manually parse this line
                            ParseLineManual(s, m_XmlDoc)
                        Loop

                        m_XmlFilename = strFilePath
                        m_initialized = True

                    End Using

                Else
                    ' File doesn't exist; create a new, blank .XML file
                    m_XmlFilename = strFilePath
                    m_XmlDoc.Save(m_XmlFilename)
                    m_initialized = True
                End If

                Return True

            Catch e As Exception
                If NotifyOnException Then
                    Throw New Exception("Failed to read XML file.")
                End If
            End Try

            Return False

        End Function

        ''' <summary>Manually parses a line to extract the settings information
        ''' Supports the traditional .Ini file format
        ''' Also supports the 'key="KeyName" value="Value"' method used in XML settings files
        ''' If success, then adds attributes to the doc object</summary>
        ''' <param name="strLine">The name of the string to be parse.</param>
        ''' <param name="doc">The name of the System.Xml.XmlDocument.</param>
        ''' <remarks>Returns True for blank lines</remarks>
        Private Sub ParseLineManual(strLine As String, doc As XmlDocument)
            Const SECTION_NAME_TAG = "<section name="
            Const KEY_TAG = "key="
            Const VALUE_TAG = "value="

            strLine = strLine.TrimStart()
            If String.IsNullOrWhiteSpace(strLine) Then
                Return
            End If

            Select Case (strLine.Substring(0, 1))
                Case "["
                    ' this is a section
                    ' trim the first and last characters
                    strLine = strLine.TrimStart("["c)
                    strLine = strLine.TrimEnd("]"c)
                    ' create a new section element
                    CreateSection(strLine)
                    Return
                Case ";"
                    ' new comment
                    Dim node = doc.CreateElement("comment")
                    node.InnerText = strLine.Substring(1)
                    GetLastSection().AppendChild(node)
                    Return
                Case Else
                    ' Look for typical XML settings file elements

                    Dim strKey As String = String.Empty
                    If ParseLineManualCheckTag(strLine, SECTION_NAME_TAG, strKey) Then
                        ' This is an XML-style section

                        ' Create a new section element
                        CreateSection(strKey)
                        Return
                    Else
                        Dim strValue As String = String.Empty
                        If ParseLineManualCheckTag(strLine, KEY_TAG, strKey) Then
                            ' This is an XML-style key

                            ParseLineManualCheckTag(strLine, VALUE_TAG, strValue)

                        Else
                            ' split the string on the "=" sign, if present
                            If (strLine.IndexOf("="c) > 0) Then
                                Dim parts = strLine.Split("="c)
                                strKey = parts(0).Trim()
                                strValue = parts(1).Trim()
                            Else
                                strKey = strLine
                                strValue = String.Empty
                            End If
                        End If

                        If String.IsNullOrEmpty(strKey) Then
                            strKey = String.Empty
                        End If

                        If String.IsNullOrEmpty(strValue) Then
                            strValue = String.Empty
                        End If

                        If String.IsNullOrWhiteSpace(strKey) Then
                            Return
                        Else

                            Dim blnAddSetting = True

                            Select Case strKey.ToLower().Trim()

                                Case "<sections>", "</section>", "</sections>"
                                    ' Do not add a new key
                                    If String.IsNullOrEmpty(strValue) Then
                                        blnAddSetting = False
                                    End If

                            End Select

                            If blnAddSetting Then
                                Dim node = doc.CreateElement("item")
                                Dim nodeAttribute = doc.CreateAttribute("key")
                                nodeAttribute.Value = SetNameCase(strKey)
                                node.Attributes.SetNamedItem(nodeAttribute)

                                nodeAttribute = doc.CreateAttribute("value")
                                nodeAttribute.Value = strValue
                                node.Attributes.SetNamedItem(nodeAttribute)

                                GetLastSection().AppendChild(node)

                            End If

                            Return
                        End If

                    End If

            End Select

        End Sub

        Private Function ParseLineManualCheckTag(
          strLine As String,
          strTagTofind As String,
          <Out()> ByRef strTagValue As String) As Boolean

            Dim intMatchIndex As Integer
            Dim intNextMatchIndex As Integer

            strTagValue = String.Empty

            intMatchIndex = strLine.ToLower().IndexOf(strTagTofind, StringComparison.Ordinal)

            If intMatchIndex >= 0 Then
                strTagValue = strLine.Substring(intMatchIndex + strTagTofind.Length)

                If strTagValue.StartsWith(""""c) Then
                    strTagValue = strTagValue.Substring(1)
                End If

                intNextMatchIndex = strTagValue.IndexOf(""""c)
                If intNextMatchIndex >= 0 Then
                    strTagValue = strTagValue.Substring(0, intNextMatchIndex)
                End If

                Return True
            Else
                Return False
            End If

        End Function

        ''' <summary>It Sets or Gets the output file name.</summary>
        Public Property OutputFilename() As String
            Get
                If Not Initialized Then
                    Return String.Empty
                Else
                    Return m_SaveFilename
                End If
            End Get
            Set(Value As String)
                Dim fi As FileInfo
                If Not Initialized Then Throw New XMLFileReaderNotInitializedException
                fi = New FileInfo(Value)
                If Not fi.Directory.Exists Then
                    If NotifyOnException Then
                        Throw New Exception("Invalid path for output file.")
                    End If
                Else
                    m_SaveFilename = Value
                End If
            End Set
        End Property

        ''' <summary>
        ''' It saves the data to the Xml output file.
        ''' </summary>
        Public Sub Save()
            If Not Initialized Then Throw New XMLFileReaderNotInitializedException
            If Not OutputFilename Is Nothing AndAlso Not m_XmlDoc Is Nothing Then
                Dim fi = New FileInfo(OutputFilename)
                If Not fi.Directory.Exists Then
                    If NotifyOnException Then
                        Throw New Exception("Invalid path.")
                    End If
                    Return
                End If
                If fi.Exists Then
                    fi.Delete()
                    m_XmlDoc.Save(OutputFilename)
                Else
                    m_XmlDoc.Save(OutputFilename)
                End If
                If NotifyOnEvent Then
                    RaiseEvent InformationMessage("File save complete.")
                End If
            Else
                If NotifyOnException Then
                    Throw New Exception("Not Output File name specified.")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Gets the System.Xml.XmlDocument.
        ''' </summary>
        Public ReadOnly Property XmlDoc() As XmlDocument
            Get
                If Not Initialized Then
                    Return New XmlDocument
                Else
                    Return m_XmlDoc
                End If
            End Get
        End Property

        ''' <summary>
        ''' Converts an XML document to a string.
        ''' </summary>
        ''' <return>It returns the XML document formatted as a string.</return>
        Public ReadOnly Property XML() As String
            Get
                If Not Initialized Then
                    Return String.Empty
                End If

                Dim sb = New StringBuilder
                Using sw = New StringWriter(sb)
                    Using xw = New XmlTextWriter(sw)
                        xw.Indentation = 3
                        xw.Formatting = Formatting.Indented
                        m_XmlDoc.WriteContentTo(xw)
                    End Using
                End Using

                Return sb.ToString()
            End Get
        End Property

    End Class

    Public Class XMLFileReaderNotInitializedException
        Inherits ApplicationException
        Public Overrides ReadOnly Property Message() As String
            Get
                Return "The XMLFileReader instance has not been properly initialized."
            End Get
        End Property
    End Class

End Class



