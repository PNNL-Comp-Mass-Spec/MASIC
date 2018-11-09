Option Strict On

' This class computes aggregate stats for a dataset
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started May 7, 2009
' Ported from clsMASICScanStatsParser to clsDatasetStatsSummarizer in February 2010
'
' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may Not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause

Imports System.Text
Imports System.Xml
Imports PRISM
Imports SpectraTypeClassifier

Public Class clsDatasetStatsSummarizer
    Inherits EventNotifier

#Region "Constants and Enums"
    Public Const SCANTYPE_STATS_SEPCHAR As String = "::###::"

    ' ReSharper disable once UnusedMember.Global
    Public Const DATASET_INFO_FILE_SUFFIX As String = "_DatasetInfo.xml"

    ' ReSharper disable once UnusedMember.Global
    Public Const DEFAULT_DATASET_STATS_FILENAME As String = "MSFileInfo_DatasetStats.txt"
#End Region

#Region "Structures"

    Public Structure udtDatasetFileInfoType
        Public FileSystemCreationTime As DateTime
        Public FileSystemModificationTime As DateTime
        Public DatasetID As Integer
        Public DatasetName As String
        Public FileExtension As String
        Public AcqTimeStart As DateTime
        Public AcqTimeEnd As DateTime
        Public ScanCount As Integer
        Public FileSizeBytes As Long

        Public Sub Clear()
            FileSystemCreationTime = DateTime.MinValue
            FileSystemModificationTime = DateTime.MinValue
            DatasetID = 0
            DatasetName = String.Empty
            FileExtension = String.Empty
            AcqTimeStart = DateTime.MinValue
            AcqTimeEnd = DateTime.MinValue
            ScanCount = 0
            FileSizeBytes = 0
        End Sub

        Public Overrides Function ToString() As String
            Return "Dataset " & DatasetName & ", ScanCount=" & ScanCount
        End Function
    End Structure

    Public Structure udtSampleInfoType
        Public SampleName As String
        Public Comment1 As String
        Public Comment2 As String

        Public Sub Clear()
            SampleName = String.Empty
            Comment1 = String.Empty
            Comment2 = String.Empty
        End Sub

        Public Function HasData() As Boolean
            If Not String.IsNullOrEmpty(SampleName) OrElse
               Not String.IsNullOrEmpty(Comment1) OrElse
               Not String.IsNullOrEmpty(Comment2) Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Overrides Function ToString() As String
            Return SampleName
        End Function
    End Structure

#End Region

#Region "Classwide Variables"
    Private ReadOnly mFileDate As String
    Private mDatasetStatsSummaryFileName As String
    Private mErrorMessage As String = String.Empty

    Private mDatasetScanStats As List(Of clsScanStatsEntry)
    Public DatasetFileInfo As udtDatasetFileInfoType
    Public SampleInfo As udtSampleInfoType

    Private mDatasetSummaryStatsUpToDate As Boolean
    Private mDatasetSummaryStats As clsDatasetSummaryStats

    Private ReadOnly mMedianUtils As clsMedianUtilities

#End Region

#Region "Properties"

    ' ReSharper disable once UnusedMember.Global
    Public Property DatasetStatsSummaryFileName As String
        Get
            Return mDatasetStatsSummaryFileName
        End Get
        Set
            If Not Value Is Nothing Then
                mDatasetStatsSummaryFileName = Value
            End If
        End Set
    End Property

    Public ReadOnly Property ErrorMessage As String
        Get
            Return mErrorMessage
        End Get
    End Property

    Public ReadOnly Property FileDate As String
        Get
            FileDate = mFileDate
        End Get
    End Property

#End Region

    Public Sub New()
        mFileDate = "October 16, 2018"

        mMedianUtils = New clsMedianUtilities()

        InitializeLocalVariables()
    End Sub

    ' ReSharper disable once UnusedMember.Global
    Public Sub AddDatasetScan(objScanStats As clsScanStatsEntry)

        mDatasetScanStats.Add(objScanStats)
        mDatasetSummaryStatsUpToDate = False

    End Sub

    Public Sub ClearCachedData()
        If mDatasetScanStats Is Nothing Then
            mDatasetScanStats = New List(Of clsScanStatsEntry)
        Else
            mDatasetScanStats.Clear()
        End If

        If mDatasetSummaryStats Is Nothing Then
            mDatasetSummaryStats = New clsDatasetSummaryStats
        Else
            mDatasetSummaryStats.Clear()
        End If

        Me.DatasetFileInfo.Clear()
        Me.SampleInfo.Clear()

        mDatasetSummaryStatsUpToDate = False

    End Sub

    ''' <summary>
    ''' Summarizes the scan info in objScanStats()
    ''' </summary>
    ''' <param name="objScanStats">ScanStats data to parse</param>
    ''' <param name="objSummaryStats">Stats output (initialized if nothing)</param>
    ''' <returns>>True if success, false if error</returns>
    ''' <remarks></remarks>
    Public Function ComputeScanStatsSummary(objScanStats As List(Of clsScanStatsEntry),
                                            ByRef objSummaryStats As clsDatasetSummaryStats) As Boolean

        Dim objEntry As clsScanStatsEntry

        Dim scanTypeKey As String

        Dim ticListMS = New List(Of Double)

        Dim ticListMSn = New List(Of Double)

        Dim bpiListMS = New List(Of Double)

        Dim bpiListMSn = New List(Of Double)

        Try

            If objScanStats Is Nothing Then
                ReportError("objScanStats is Nothing; unable to continue in ComputeScanStatsSummary")
                Return False
            Else
                mErrorMessage = ""
            End If

            ' Initialize objSummaryStats
            If objSummaryStats Is Nothing Then
                objSummaryStats = New clsDatasetSummaryStats
            Else
                objSummaryStats.Clear()
            End If

            For Each objEntry In objScanStats

                If objEntry.ScanType > 1 Then
                    ' MSn spectrum
                    ComputeScanStatsUpdateDetails(objEntry,
                                                  objSummaryStats.ElutionTimeMax,
                                                  objSummaryStats.MSnStats,
                                                  ticListMSn,
                                                  bpiListMSn)
                Else
                    ' MS spectrum
                    ComputeScanStatsUpdateDetails(objEntry,
                                                  objSummaryStats.ElutionTimeMax,
                                                  objSummaryStats.MSStats,
                                                  ticListMS,
                                                  bpiListMS)
                End If

                scanTypeKey = objEntry.ScanTypeName & SCANTYPE_STATS_SEPCHAR & objEntry.ScanFilterText
                If objSummaryStats.objScanTypeStats.ContainsKey(scanTypeKey) Then
                    objSummaryStats.objScanTypeStats.Item(scanTypeKey) += 1
                Else
                    objSummaryStats.objScanTypeStats.Add(scanTypeKey, 1)
                End If
            Next

            objSummaryStats.MSStats.TICMedian = mMedianUtils.Median(ticListMS)
            objSummaryStats.MSStats.BPIMedian = mMedianUtils.Median(bpiListMS)

            objSummaryStats.MSnStats.TICMedian = mMedianUtils.Median(ticListMSn)
            objSummaryStats.MSnStats.BPIMedian = mMedianUtils.Median(bpiListMSn)

            Return True

        Catch ex As Exception
            ReportError("Error in ComputeScanStatsSummary: " & ex.Message)
            Return False
        End Try

    End Function

    Private Sub ComputeScanStatsUpdateDetails(
                                              ByRef objScanStats As clsScanStatsEntry,
                                              ByRef elutionTimeMax As Double,
                                              ByRef udtSummaryStatDetails As clsDatasetSummaryStats.udtSummaryStatDetailsType,
                                              ByRef ticList As List(Of Double),
                                              ByRef bpiList As List(Of Double))

        Dim elutionTime As Double
        Dim totalIonCurrent As Double
        Dim basePeakIntensity As Double

        If objScanStats.ElutionTime <> Nothing AndAlso objScanStats.ElutionTime.Length > 0 Then
            If Double.TryParse(objScanStats.ElutionTime, elutionTime) Then
                If elutionTime > elutionTimeMax Then
                    elutionTimeMax = elutionTime
                End If
            End If
        End If

        If Double.TryParse(objScanStats.TotalIonIntensity, totalIonCurrent) Then
            If totalIonCurrent > udtSummaryStatDetails.TICMax Then
                udtSummaryStatDetails.TICMax = totalIonCurrent
            End If

            ticList.Add(totalIonCurrent)
        End If

        If Double.TryParse(objScanStats.BasePeakIntensity, basePeakIntensity) Then
            If basePeakIntensity > udtSummaryStatDetails.BPIMax Then
                udtSummaryStatDetails.BPIMax = basePeakIntensity
            End If

            bpiList.Add(basePeakIntensity)
        End If

        udtSummaryStatDetails.ScanCount += 1

    End Sub

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates an XML file summarizing the data stored in this class (in mDatasetScanStats, Me.DatasetFileInfo, and Me.SampleInfo)
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="datasetInfoFilePath">File path to write the XML to</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoFile(
                                          datasetName As String,
                                          datasetInfoFilePath As String) As Boolean

        Return CreateDatasetInfoFile(datasetName, datasetInfoFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ''' <summary>
    ''' Creates an XML file summarizing the data in objScanStats and udtDatasetFileInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="datasetInfoFilePath">File path to write the XML to</param>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <param name="udtSampleInfo">Sample Info</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoFile(
                                          datasetName As String,
                                          datasetInfoFilePath As String,
                                          ByRef objScanStats As List(Of clsScanStatsEntry),
                                          ByRef udtDatasetFileInfo As udtDatasetFileInfoType,
                                          ByRef udtSampleInfo As udtSampleInfoType) As Boolean

        Dim success As Boolean

        Try
            If objScanStats Is Nothing Then
                ReportError("objScanStats is Nothing; unable to continue in CreateDatasetInfoFile")
                Return False
            Else
                mErrorMessage = ""
            End If

            ' If CreateDatasetInfoXML() used a StringBuilder to cache the XML data, then we would have to use System.Encoding.Unicode
            ' However, CreateDatasetInfoXML() now uses a MemoryStream, so we're able to use UTF8
            Using writer = New StreamWriter(New FileStream(datasetInfoFilePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8)

                writer.WriteLine(CreateDatasetInfoXML(datasetName, objScanStats, udtDatasetFileInfo, udtSampleInfo))

            End Using

            success = True

        Catch ex As Exception
            ReportError("Error in CreateDatasetInfoFile: " & ex.Message)
            success = False
        End Try

        Return success

    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates XML summarizing the data stored in this class (in mDatasetScanStats, Me.DatasetFileInfo, and Me.SampleInfo)
    ''' Auto-determines the dataset name using Me.DatasetFileInfo.DatasetName
    ''' </summary>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML() As String
        Return CreateDatasetInfoXML(Me.DatasetFileInfo.DatasetName, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates XML summarizing the data stored in this class (in mDatasetScanStats, Me.DatasetFileInfo, and Me.SampleInfo)
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(datasetName As String) As String
        Return CreateDatasetInfoXML(datasetName, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates XML summarizing the data in objScanStats and udtDatasetFileInfo
    ''' Auto-determines the dataset name using udtDatasetFileInfo.DatasetName
    ''' </summary>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
                                         ByRef objScanStats As List(Of clsScanStatsEntry),
                                         ByRef udtDatasetFileInfo As udtDatasetFileInfoType) As String

        Return CreateDatasetInfoXML(udtDatasetFileInfo.DatasetName, objScanStats, udtDatasetFileInfo)
    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates XML summarizing the data in objScanStats, udtDatasetFileInfo, and udtSampleInfo
    ''' Auto-determines the dataset name using udtDatasetFileInfo.DatasetName
    ''' </summary>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <param name="udtSampleInfo">Sample Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
                                         ByRef objScanStats As List(Of clsScanStatsEntry),
                                         ByRef udtDatasetFileInfo As udtDatasetFileInfoType,
                                         ByRef udtSampleInfo As udtSampleInfoType) As String

        Return CreateDatasetInfoXML(udtDatasetFileInfo.DatasetName, objScanStats, udtDatasetFileInfo, udtSampleInfo)
    End Function

    ''' <summary>
    ''' Creates XML summarizing the data in objScanStats and udtDatasetFileInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
                                         datasetName As String,
                                         ByRef objScanStats As List(Of clsScanStatsEntry),
                                         ByRef udtDatasetFileInfo As udtDatasetFileInfoType) As String

        Dim udtSampleInfo = New udtSampleInfoType()
        udtSampleInfo.Clear()

        Return CreateDatasetInfoXML(datasetName, objScanStats, udtDatasetFileInfo, udtSampleInfo)
    End Function

    ''' <summary>
    ''' Creates XML summarizing the data in objScanStats and udtDatasetFileInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
                                         datasetName As String,
                                         objScanStats As List(Of clsScanStatsEntry),
                                         ByRef udtDatasetFileInfo As udtDatasetFileInfoType,
                                         ByRef udtSampleInfo As udtSampleInfoType) As String

        Try

            If objScanStats Is Nothing Then
                ReportError("objScanStats is Nothing; unable to continue in CreateDatasetInfoXML")
                Return String.Empty
            Else
                mErrorMessage = ""
            End If

            Dim objSummaryStats As clsDatasetSummaryStats

            If objScanStats Is mDatasetScanStats Then
                objSummaryStats = GetDatasetSummaryStats()
            Else
                objSummaryStats = New clsDatasetSummaryStats()

                ' Parse the data in objScanStats to compute the bulk values
                Me.ComputeScanStatsSummary(objScanStats, objSummaryStats)
            End If

            Dim objXMLSettings = New XmlWriterSettings()

            With objXMLSettings
                .CheckCharacters = True
                .Indent = True
                .IndentChars = "  "
                .Encoding = Encoding.UTF8

                ' Do not close output automatically so that MemoryStream
                ' can be read after the XmlWriter has been closed
                .CloseOutput = False
            End With

            ' We could cache the text using a StringBuilder, like this:
            '
            ' Dim sbDatasetInfo As New System.StringBuilder
            ' Dim objStringWriter As = StringWriter
            ' objStringWriter = New StringWriter(sbDatasetInfo)
            ' objDSInfo = New System.Xml.XmlTextWriter(objStringWriter)
            ' objDSInfo.Formatting = System.Xml.Formatting.Indented
            ' objDSInfo.Indentation = 2

            ' However, when you send the output to a StringBuilder it is always encoded as Unicode (UTF-16)
            '  since this is the only character encoding used in the .NET Framework for String values,
            '  and thus you'll see the attribute encoding="utf-16" in the opening XML declaration
            ' The alternative is to use a MemoryStream.  Here, the stream encoding is set by the XmlWriter
            '  and so you see the attribute encoding="utf-8" in the opening XML declaration encoding
            '  (since we used objXMLSettings.Encoding = System.Encoding.UTF8)
            '
            Dim objMemStream = New MemoryStream()
            Dim objDSInfo = XmlWriter.Create(objMemStream, objXMLSettings)

            objDSInfo.WriteStartDocument(True)

            'Write the beginning of the "Root" element.
            objDSInfo.WriteStartElement("DatasetInfo")

            objDSInfo.WriteElementString("Dataset", datasetName)

            objDSInfo.WriteStartElement("ScanTypes")

            Dim objEnum = objSummaryStats.objScanTypeStats.GetEnumerator()
            Do While objEnum.MoveNext

                Dim scanType = objEnum.Current.Key
                Dim indexMatch = scanType.IndexOf(SCANTYPE_STATS_SEPCHAR, StringComparison.Ordinal)
                Dim scanFilterText As String

                If indexMatch >= 0 Then
                    scanFilterText = scanType.Substring(indexMatch + SCANTYPE_STATS_SEPCHAR.Length)
                    If indexMatch > 0 Then
                        scanType = scanType.Substring(0, indexMatch)
                    Else
                        scanType = String.Empty
                    End If
                Else
                    scanFilterText = String.Empty
                End If

                objDSInfo.WriteStartElement("ScanType")
                objDSInfo.WriteAttributeString("ScanCount", objEnum.Current.Value.ToString())
                objDSInfo.WriteAttributeString("ScanFilterText", FixNull(scanFilterText))
                objDSInfo.WriteString(scanType)
                objDSInfo.WriteEndElement()     ' ScanType
            Loop

            objDSInfo.WriteEndElement()       ' ScanTypes

            objDSInfo.WriteStartElement("AcquisitionInfo")

            Dim scanCountTotal = objSummaryStats.MSStats.ScanCount + objSummaryStats.MSnStats.ScanCount
            If scanCountTotal = 0 And udtDatasetFileInfo.ScanCount > 0 Then
                scanCountTotal = udtDatasetFileInfo.ScanCount
            End If

            objDSInfo.WriteElementString("ScanCount", scanCountTotal.ToString())

            objDSInfo.WriteElementString("ScanCountMS", objSummaryStats.MSStats.ScanCount.ToString())
            objDSInfo.WriteElementString("ScanCountMSn", objSummaryStats.MSnStats.ScanCount.ToString())
            objDSInfo.WriteElementString("Elution_Time_Max", objSummaryStats.ElutionTimeMax.ToString())

            objDSInfo.WriteElementString("AcqTimeMinutes", udtDatasetFileInfo.AcqTimeEnd.Subtract(udtDatasetFileInfo.AcqTimeStart).TotalMinutes.ToString("0.00"))
            objDSInfo.WriteElementString("StartTime", udtDatasetFileInfo.AcqTimeStart.ToString("yyyy-MM-dd hh:mm:ss tt"))
            objDSInfo.WriteElementString("EndTime", udtDatasetFileInfo.AcqTimeEnd.ToString("yyyy-MM-dd hh:mm:ss tt"))

            objDSInfo.WriteElementString("FileSizeBytes", udtDatasetFileInfo.FileSizeBytes.ToString())

            objDSInfo.WriteEndElement()       ' AcquisitionInfo

            objDSInfo.WriteStartElement("TICInfo")
            objDSInfo.WriteElementString("TIC_Max_MS", StringUtilities.ValueToString(objSummaryStats.MSStats.TICMax, 5))
            objDSInfo.WriteElementString("TIC_Max_MSn", StringUtilities.ValueToString(objSummaryStats.MSnStats.TICMax, 5))
            objDSInfo.WriteElementString("BPI_Max_MS", StringUtilities.ValueToString(objSummaryStats.MSStats.BPIMax, 5))
            objDSInfo.WriteElementString("BPI_Max_MSn", StringUtilities.ValueToString(objSummaryStats.MSnStats.BPIMax, 5))
            objDSInfo.WriteElementString("TIC_Median_MS", StringUtilities.ValueToString(objSummaryStats.MSStats.TICMedian, 5))
            objDSInfo.WriteElementString("TIC_Median_MSn", StringUtilities.ValueToString(objSummaryStats.MSnStats.TICMedian, 5))
            objDSInfo.WriteElementString("BPI_Median_MS", StringUtilities.ValueToString(objSummaryStats.MSStats.BPIMedian, 5))
            objDSInfo.WriteElementString("BPI_Median_MSn", StringUtilities.ValueToString(objSummaryStats.MSnStats.BPIMedian, 5))
            objDSInfo.WriteEndElement()       ' TICInfo

            ' Only write the SampleInfo block if udtSampleInfo contains entries
            If udtSampleInfo.HasData() Then
                objDSInfo.WriteStartElement("SampleInfo")
                objDSInfo.WriteElementString("SampleName", FixNull(udtSampleInfo.SampleName))
                objDSInfo.WriteElementString("Comment1", FixNull(udtSampleInfo.Comment1))
                objDSInfo.WriteElementString("Comment2", FixNull(udtSampleInfo.Comment2))
                objDSInfo.WriteEndElement()       ' SampleInfo
            End If


            objDSInfo.WriteEndElement()  'End the "Root" element (DatasetInfo)
            objDSInfo.WriteEndDocument() 'End the document

            objDSInfo.Close()

            ' Now Rewind the memory stream and output as a string
            objMemStream.Position = 0
            Dim reader = New StreamReader(objMemStream)

            ' Return the XML as text
            Return reader.ReadToEnd()

        Catch ex As Exception
            ReportError("Error in CreateDatasetInfoXML: " & ex.Message)
        End Try

        ' This code will only be reached if an exception occurs
        Return String.Empty

    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="scanStatsFilePath">File path to write the text file to</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function CreateScanStatsFile(
                                        datasetName As String,
                                        scanStatsFilePath As String) As Boolean

        Return CreateScanStatsFile(datasetName, scanStatsFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ''' <summary>
    ''' Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="scanStatsFilePath">File path to write the text file to</param>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <param name="udtSampleInfo">Sample Info</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function CreateScanStatsFile(
                                        datasetName As String,
                                        scanStatsFilePath As String,
                                        objScanStats As List(Of clsScanStatsEntry),
                                        ByRef udtDatasetFileInfo As udtDatasetFileInfoType,
                                        ByRef udtSampleInfo As udtSampleInfoType) As Boolean


        Dim datasetID = 0

        Try
            If objScanStats Is Nothing Then
                ReportError("objScanStats is Nothing; unable to continue in CreateScanStatsFile")
                Return False
            Else
                mErrorMessage = ""
            End If

            Using writer = New StreamWriter(New FileStream(scanStatsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

                ' Write the headers
                Dim headerNames = New List(Of String) From {
                    "Dataset",
                    "ScanNumber",
                    "ScanTime",
                    "ScanType",
                    "TotalIonIntensity",
                    "BasePeakIntensity",
                    "BasePeakMZ",
                    "BasePeakSignalToNoiseRatio",
                    "IonCount",
                    "IonCountRaw",
                    "ScanTypeName"
                }

                writer.WriteLine(String.Join(ControlChars.Tab, headerNames))

                Dim dataColumns As New List(Of String)

                For Each objScanStatsEntry As clsScanStatsEntry In objScanStats

                    dataColumns.Clear()
                    dataColumns.Add(datasetID.ToString())                           ' Dataset number (aka Dataset ID)
                    dataColumns.Add(objScanStatsEntry.ScanNumber.ToString())        ' Scan number
                    dataColumns.Add(objScanStatsEntry.ElutionTime)                  ' Scan time (minutes)
                    dataColumns.Add(objScanStatsEntry.ScanType.ToString())          ' Scan type (1 for MS, 2 for MS2, etc.)
                    dataColumns.Add(objScanStatsEntry.TotalIonIntensity)            ' Total ion intensity
                    dataColumns.Add(objScanStatsEntry.BasePeakIntensity)            ' Base peak ion intensity
                    dataColumns.Add(objScanStatsEntry.BasePeakMZ)                   ' Base peak ion m/z
                    dataColumns.Add(objScanStatsEntry.BasePeakSignalToNoiseRatio)   ' Base peak signal to noise ratio
                    dataColumns.Add(objScanStatsEntry.IonCount.ToString())          ' Number of peaks (aka ions) in the spectrum
                    dataColumns.Add(objScanStatsEntry.IonCountRaw.ToString())       ' Number of peaks (aka ions) in the spectrum prior to any filtering
                    dataColumns.Add(objScanStatsEntry.ScanTypeName)                 ' Scan type name

                    writer.WriteLine(String.Join(ControlChars.Tab, dataColumns))

                Next

            End Using

            Return True

        Catch ex As Exception
            ReportError("Error in CreateScanStatsFile: " & ex.Message)
            Return False
        End Try

    End Function

    Private Function FixNull(item As String) As String
        If String.IsNullOrEmpty(item) Then
            Return String.Empty
        Else
            Return item
        End If
    End Function

    Public Function GetDatasetSummaryStats() As clsDatasetSummaryStats

        If Not mDatasetSummaryStatsUpToDate Then
            ComputeScanStatsSummary(mDatasetScanStats, mDatasetSummaryStats)
            mDatasetSummaryStatsUpToDate = True
        End If

        Return mDatasetSummaryStats

    End Function

    Private Sub InitializeLocalVariables()
        mErrorMessage = String.Empty

        ClearCachedData()
    End Sub

    Private Sub ReportError(message As String, Optional ex As Exception = Nothing)
        mErrorMessage = String.Copy(message)
        OnErrorEvent(mErrorMessage, ex)
    End Sub

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Updates the scan type information for the specified scan number
    ''' </summary>
    ''' <param name="scanNumber"></param>
    ''' <param name="scanType"></param>
    ''' <param name="scanTypeName"></param>
    ''' <returns>True if the scan was found and updated; otherwise false</returns>
    ''' <remarks></remarks>
    Public Function UpdateDatasetScanType(
                                          scanNumber As Integer,
                                          scanType As Integer,
                                          scanTypeName As String) As Boolean

        Dim index As Integer
        Dim matchFound As Boolean

        ' Look for scan scanNumber in mDatasetScanStats
        For index = 0 To mDatasetScanStats.Count - 1
            If mDatasetScanStats(index).ScanNumber = scanNumber Then
                mDatasetScanStats(index).ScanType = scanType
                mDatasetScanStats(index).ScanTypeName = scanTypeName
                mDatasetSummaryStatsUpToDate = False

                matchFound = True
                Exit For
            End If
        Next

        Return matchFound

    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Updates a tab-delimited text file, adding a new line summarizing the data stored in this class (in mDatasetScanStats and Me.DatasetFileInfo)
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="datasetInfoFilePath">File path to write the XML to</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function UpdateDatasetStatsTextFile(
                                               datasetName As String,
                                               datasetInfoFilePath As String) As Boolean

        Return UpdateDatasetStatsTextFile(datasetName, datasetInfoFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ''' <summary>
    ''' Updates a tab-delimited text file, adding a new line summarizing the data in objScanStats and udtDatasetFileInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="datasetStatsFilePath">Tab-delimited file to create/update</param>
    ''' <param name="objScanStats">Scan stats to parse</param>
    ''' <param name="udtDatasetFileInfo">Dataset Info</param>
    ''' <param name="udtSampleInfo">Sample Info</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function UpdateDatasetStatsTextFile(
                                               datasetName As String,
                                               datasetStatsFilePath As String,
                                               objScanStats As List(Of clsScanStatsEntry),
                                               ByRef udtDatasetFileInfo As udtDatasetFileInfoType,
                                               ByRef udtSampleInfo As udtSampleInfoType) As Boolean

        Dim writeHeaders As Boolean

        Dim objSummaryStats As clsDatasetSummaryStats

        Try

            If objScanStats Is Nothing Then
                ReportError("objScanStats is Nothing; unable to continue in UpdateDatasetStatsTextFile")
                Return False
            Else
                mErrorMessage = ""
            End If

            If objScanStats Is mDatasetScanStats Then
                objSummaryStats = GetDatasetSummaryStats()
            Else
                objSummaryStats = New clsDatasetSummaryStats()

                ' Parse the data in objScanStats to compute the bulk values
                Me.ComputeScanStatsSummary(objScanStats, objSummaryStats)
            End If

            If Not File.Exists(datasetStatsFilePath) Then
                writeHeaders = True
            End If

            ' Create or open the output file
            Using writer = New StreamWriter(New FileStream(datasetStatsFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))

                If writeHeaders Then
                    ' Write the header line
                    Dim headerNames = New List(Of String) From {
                        "Dataset",
                         "ScanCount",
                         "ScanCountMS",
                         "ScanCountMSn",
                         "Elution_Time_Max",
                         "AcqTimeMinutes",
                         "StartTime",
                         "EndTime",
                         "FileSizeBytes",
                         "SampleName",
                         "Comment1",
                         "Comment2"
                    }

                    writer.WriteLine(String.Join(ControlChars.Tab, headerNames))
                End If

                Dim dataColumns = New List(Of String) From {
                    datasetName,
                    (objSummaryStats.MSStats.ScanCount + objSummaryStats.MSnStats.ScanCount).ToString(),
                    objSummaryStats.MSStats.ScanCount.ToString(),
                    objSummaryStats.MSnStats.ScanCount.ToString(),
                    objSummaryStats.ElutionTimeMax.ToString(),
                    udtDatasetFileInfo.AcqTimeEnd.Subtract(udtDatasetFileInfo.AcqTimeStart).TotalMinutes.ToString("0.00"),
                    udtDatasetFileInfo.AcqTimeStart.ToString("yyyy-MM-dd hh:mm:ss tt"),
                    udtDatasetFileInfo.AcqTimeEnd.ToString("yyyy-MM-dd hh:mm:ss tt"),
                    udtDatasetFileInfo.FileSizeBytes.ToString(),
                    FixNull(udtSampleInfo.SampleName),
                    FixNull(udtSampleInfo.Comment1),
                    FixNull(udtSampleInfo.Comment2)
                }

                writer.WriteLine(String.Join(ControlChars.Tab, dataColumns))

            End Using

            Return True

        Catch ex As Exception
            ReportError("Error in UpdateDatasetStatsTextFile: " & ex.Message, ex)
            Return False
        End Try

    End Function

End Class

Public Class clsScanStatsEntry
    Public Property ScanNumber As Integer

    ''' <summary>
    ''' 1 for MS, 2 for MS2, 3 for MS3
    ''' </summary>
    ''' <returns></returns>
    Public Property ScanType As Integer

    ''' <summary>
    ''' Scan filter
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>
    ''' Examples:
    ''' FTMS + p NSI Full ms [400.00-2000.00]
    ''' ITMS + c ESI Full ms [300.00-2000.00]
    ''' ITMS + p ESI d Z ms [1108.00-1118.00]
    ''' ITMS + c ESI d Full ms2 342.90@cid35.00
    ''' </remarks>
    Public Property ScanFilterText As String

    ''' <summary>
    ''' Scan type name
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>
    ''' Examples:
    ''' MS, HMS, Zoom, CID-MSn, or PQD-MSn
    ''' </remarks>
    Public Property ScanTypeName As String

    ' The following are strings to prevent the number formatting from changing
    Public Property ElutionTime As String
    Public Property TotalIonIntensity As String
    Public Property BasePeakIntensity As String
    Public Property BasePeakMZ As String
    Public Property BasePeakSignalToNoiseRatio As String

    Public Property IonCount As Integer
    Public Property IonCountRaw As Integer

    Public Sub Clear()
        ScanNumber = 0
        ScanType = 0

        ScanFilterText = String.Empty
        ScanTypeName = String.Empty

        ElutionTime = "0"
        TotalIonIntensity = "0"
        BasePeakIntensity = "0"
        BasePeakMZ = "0"
        BasePeakSignalToNoiseRatio = "0"

        IonCount = 0
        IonCountRaw = 0
    End Sub

    Public Sub New()
        Me.Clear()
    End Sub
End Class

Public Class clsDatasetSummaryStats

    Public Property ElutionTimeMax As Double
    Public MSStats As udtSummaryStatDetailsType
    Public MSnStats As udtSummaryStatDetailsType

    ' The following collection keeps track of each ScanType in the dataset, along with the number of scans of this type
    ' Example scan types:  FTMS + p NSI Full ms" or "ITMS + c ESI Full ms" or "ITMS + p ESI d Z ms" or "ITMS + c ESI d Full ms2 @cid35.00"
    Public objScanTypeStats As Dictionary(Of String, Integer)

    Public Structure udtSummaryStatDetailsType
        Public ScanCount As Integer
        Public TICMax As Double
        Public BPIMax As Double
        Public TICMedian As Double
        Public BPIMedian As Double

        Public Overrides Function ToString() As String
            Return "ScanCount: " & ScanCount
        End Function
    End Structure

    Public Sub Clear()

        ElutionTimeMax = 0

        With MSStats
            .ScanCount = 0
            .TICMax = 0
            .BPIMax = 0
            .TICMedian = 0
            .BPIMedian = 0
        End With

        With MSnStats
            .ScanCount = 0
            .TICMax = 0
            .BPIMax = 0
            .TICMedian = 0
            .BPIMedian = 0
        End With

        If objScanTypeStats Is Nothing Then
            objScanTypeStats = New Dictionary(Of String, Integer)
        Else
            objScanTypeStats.Clear()
        End If

    End Sub

    Public Sub New()
        Me.Clear()
    End Sub

End Class