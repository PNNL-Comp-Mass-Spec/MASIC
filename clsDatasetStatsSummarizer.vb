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
' Licensed under the 2-Clause BSD License; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause

Imports System.Text
Imports System.Xml
Imports MASIC.DatasetStats
Imports PRISM
Imports SpectraTypeClassifier

Public Class clsDatasetStatsSummarizer
    Inherits EventNotifier

#Region "Constants and Enums"
    Public Const SCAN_TYPE_STATS_SEP_CHAR As String = "::###::"

    ' ReSharper disable once UnusedMember.Global
    Public Const DATASET_INFO_FILE_SUFFIX As String = "_DatasetInfo.xml"

    ' ReSharper disable once UnusedMember.Global
    Public Const DEFAULT_DATASET_STATS_FILENAME As String = "MSFileInfo_DatasetStats.txt"


#End Region

#Region "Classwide Variables"
    Private ReadOnly mFileDate As String
    Private mDatasetStatsSummaryFileName As String
    Private mErrorMessage As String = String.Empty

    Private ReadOnly mDatasetScanStats As List(Of clsScanStatsEntry)

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

    ''' <summary>
    ''' Dataset file info
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property DatasetFileInfo As DatasetFileInfo
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

    ''' <summary>
    ''' Sample info
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property SampleInfo As SampleInfo

#End Region

    Public Sub New()
        mFileDate = "October 16, 2018"

        mMedianUtils = New clsMedianUtilities()

        mDatasetScanStats = New List(Of clsScanStatsEntry)
        mDatasetSummaryStats = New clsDatasetSummaryStats()

        mDatasetSummaryStatsUpToDate = False

        DatasetFileInfo = New DatasetFileInfo()
        SampleInfo = New SampleInfo()

        ClearCachedData()
    End Sub

    ' ReSharper disable once UnusedMember.Global
    '''<summary>
    ''' Add a New scan
    ''' </summary>
    ''' <param name="scanStats"></param>
    Public Sub AddDatasetScan(scanStats As clsScanStatsEntry)

        mDatasetScanStats.Add(scanStats)
        mDatasetSummaryStatsUpToDate = False

    End Sub

    Public Sub ClearCachedData()
        mDatasetScanStats.Clear()
        mDatasetSummaryStats.Clear()

        DatasetFileInfo.Clear()
        SampleInfo.Clear()

        mDatasetSummaryStatsUpToDate = False

    End Sub

    ''' <summary>
    ''' Summarizes the scan info in scanStats()
    ''' </summary>
    ''' <param name="scanStats">ScanStats data to parse</param>
    ''' <param name="summaryStats">Stats output (initialized if nothing)</param>
    ''' <returns>>True if success, false if error</returns>
    ''' <remarks></remarks>
        Dim objEntry As clsScanStatsEntry

        Dim scanTypeKey As String

        Dim ticListMS = New List(Of Double)

        Dim ticListMSn = New List(Of Double)

        Dim bpiListMS = New List(Of Double)
    Public Function ComputeScanStatsSummary(scanStats As List(Of clsScanStatsEntry), <Out> ByRef summaryStats As clsDatasetSummaryStats) As Boolean

        Dim bpiListMSn = New List(Of Double)

        Try

            If scanStats Is Nothing Then
                ReportError("scanStats is Nothing; unable to continue in ComputeScanStatsSummary")
                Return False
            Else
                mErrorMessage = String.Empty
            End If



            For Each statEntry In scanStats

                If statEntry.ScanType > 1 Then
                    ' MSn spectrum
                    ComputeScanStatsUpdateDetails(statEntry,
                                                  summaryStats.ElutionTimeMax,
                                                  summaryStats.MSnStats,
                                                  ticListMSn,
                                                  bpiListMSn)
                Else
                    ' MS spectrum
                    ComputeScanStatsUpdateDetails(statEntry,
                                                  summaryStats.ElutionTimeMax,
                                                  summaryStats.MSStats,
                                                  ticListMS,
                                                  bpiListMS)
                End If

                Dim scanTypeKey = statEntry.ScanTypeName & SCAN_TYPE_STATS_SEP_CHAR & statEntry.ScanFilterText
                If summaryStats.ScanTypeStats.ContainsKey(scanTypeKey) Then
                    summaryStats.ScanTypeStats.Item(scanTypeKey) += 1
                Else
                    summaryStats.ScanTypeStats.Add(scanTypeKey, 1)
                End If
            Next

            summaryStats.MSStats.TICMedian = mMedianUtils.Median(ticListMS)
            summaryStats.MSStats.BPIMedian = mMedianUtils.Median(bpiListMS)

            summaryStats.MSnStats.TICMedian = mMedianUtils.Median(ticListMSn)
            summaryStats.MSnStats.BPIMedian = mMedianUtils.Median(bpiListMSn)

            Return True

        Catch ex As Exception
            ReportError("Error in ComputeScanStatsSummary: " & ex.Message)
            Return False
        End Try

    End Function

    Private Sub ComputeScanStatsUpdateDetails(
        scanStats As clsScanStatsEntry,
        ByRef elutionTimeMax As Double,
        ByRef udtSummaryStatDetails As clsDatasetSummaryStats.udtSummaryStatDetailsType,
        ticList As ICollection(Of Double),
        bpiList As ICollection(Of Double))

        Dim elutionTime As Double
        Dim totalIonCurrent As Double
        Dim basePeakIntensity As Double

        If Not String.IsNullOrWhiteSpace(scanStats.ElutionTime) Then
            If Double.TryParse(scanStats.ElutionTime, elutionTime) Then
                If elutionTime > elutionTimeMax Then
                    elutionTimeMax = elutionTime
                End If
            End If
        End If

        If Double.TryParse(scanStats.TotalIonIntensity, totalIonCurrent) Then
            If totalIonCurrent > udtSummaryStatDetails.TICMax Then
                udtSummaryStatDetails.TICMax = totalIonCurrent
            End If

            ticList.Add(totalIonCurrent)
        End If

        If Double.TryParse(scanStats.BasePeakIntensity, basePeakIntensity) Then
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
    Public Function CreateDatasetInfoFile(datasetName As String, datasetInfoFilePath As String) As Boolean
        Return CreateDatasetInfoFile(datasetName, datasetInfoFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ''' <summary>
    ''' Creates an XML file summarizing the data in scanStats and datasetInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="datasetInfoFilePath">File path to write the XML to</param>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <param name="oSampleInfo">Sample Info</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoFile(
        datasetName As String,
        datasetInfoFilePath As String,
        scanStats As List(Of clsScanStatsEntry),
        datasetInfo As DatasetFileInfo,
        oSampleInfo As SampleInfo) As Boolean

        Dim success As Boolean

        Try
            If scanStats Is Nothing Then
                ReportError("scanStats is Nothing; unable to continue in CreateDatasetInfoFile")
                Return False
            End If

            mErrorMessage = String.Empty

            ' If CreateDatasetInfoXML() used a StringBuilder to cache the XML data, then we would have to use System.Encoding.Unicode
            ' However, CreateDatasetInfoXML() now uses a MemoryStream, so we're able to use UTF8
            Using writer = New StreamWriter(New FileStream(datasetInfoFilePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8)

                writer.WriteLine(CreateDatasetInfoXML(datasetName, scanStats, datasetInfo, oSampleInfo))

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
    ''' Creates XML summarizing the data in scanStats and datasetInfo
    ''' Auto-determines the dataset name using datasetInfo.DatasetName
    ''' </summary>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(scanStats As List(Of clsScanStatsEntry), datasetInfo As DatasetFileInfo) As String
        Return CreateDatasetInfoXML(datasetInfo.DatasetName, scanStats, datasetInfo)
    End Function

    ' ReSharper disable once UnusedMember.Global
    ''' <summary>
    ''' Creates XML summarizing the data in scanStats, datasetInfo, and oSampleInfo
    ''' Auto-determines the dataset name using datasetInfo.DatasetName
    ''' </summary>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <param name="oSampleInfo">Sample Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
        scanStats As List(Of clsScanStatsEntry),
        datasetInfo As DatasetFileInfo,
        oSampleInfo As SampleInfo) As String

        Return CreateDatasetInfoXML(datasetInfo.DatasetName, scanStats, datasetInfo, oSampleInfo)
    End Function

    ''' <summary>
    ''' Creates XML summarizing the data in scanStats and datasetInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
        datasetName As String,
        scanStats As List(Of clsScanStatsEntry),
        datasetInfo As DatasetFileInfo) As String

        Return CreateDatasetInfoXML(datasetName, scanStats, datasetInfo, New SampleInfo())
    End Function

    ''' <summary>
    ''' Creates XML summarizing the data in scanStats and datasetInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <returns>XML (as string)</returns>
    ''' <remarks></remarks>
    Public Function CreateDatasetInfoXML(
        datasetName As String,
        scanStats As List(Of clsScanStatsEntry),
        datasetInfo As DatasetFileInfo,
        oSampleInfo As SampleInfo) As String

        Try

            If scanStats Is Nothing Then
                ReportError("scanStats is Nothing; unable to continue in CreateDatasetInfoXML")
                Return String.Empty
            End If

            mErrorMessage = String.Empty

            Dim summaryStats As clsDatasetSummaryStats

            If scanStats Is mDatasetScanStats Then
                summaryStats = GetDatasetSummaryStats()
            Else
                summaryStats = New clsDatasetSummaryStats()

                ' Parse the data in scanStats to compute the bulk values
                Me.ComputeScanStatsSummary(scanStats, summaryStats)
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
            ' writer = New System.Xml.XmlTextWriter(objStringWriter)
            ' writer.Formatting = System.Xml.Formatting.Indented
            ' writer.Indentation = 2

            ' However, when you send the output to a StringBuilder it is always encoded as Unicode (UTF-16)
            '  since this is the only character encoding used in the .NET Framework for String values,
            '  and thus you'll see the attribute encoding="utf-16" in the opening XML declaration
            ' The alternative is to use a MemoryStream.  Here, the stream encoding is set by the XmlWriter
            '  and so you see the attribute encoding="utf-8" in the opening XML declaration encoding
            '  (since we used objXMLSettings.Encoding = System.Encoding.UTF8)
            '
            Dim objMemStream = New MemoryStream()
            Dim writer = XmlWriter.Create(objMemStream, objXMLSettings)

            writer.WriteStartDocument(True)

            'Write the beginning of the "Root" element.
            writer.WriteStartElement("DatasetInfo")

            writer.WriteElementString("Dataset", datasetName)

            writer.WriteStartElement("ScanTypes")

            For Each scanTypeEntry In summaryStats.ScanTypeStats

                Dim scanType = scanTypeEntry.Key
                Dim indexMatch = scanType.IndexOf(SCAN_TYPE_STATS_SEP_CHAR, StringComparison.Ordinal)
                Dim scanFilterText As String

                If indexMatch >= 0 Then
                    scanFilterText = scanType.Substring(indexMatch + SCAN_TYPE_STATS_SEP_CHAR.Length)
                    If indexMatch > 0 Then
                        scanType = scanType.Substring(0, indexMatch)
                    Else
                        scanType = String.Empty
                    End If
                Else
                    scanFilterText = String.Empty
                End If

                writer.WriteStartElement("ScanType")
                writer.WriteAttributeString("ScanCount", scanTypeEntry.Value.ToString())
                writer.WriteAttributeString("ScanFilterText", FixNull(scanFilterText))
                writer.WriteString(scanType)
                writer.WriteEndElement()     ' ScanType
            Next

            writer.WriteEndElement()       ' ScanTypes

            writer.WriteStartElement("AcquisitionInfo")

            Dim scanCountTotal = summaryStats.MSStats.ScanCount + summaryStats.MSnStats.ScanCount
            If scanCountTotal = 0 And datasetInfo.ScanCount > 0 Then
                scanCountTotal = datasetInfo.ScanCount
            End If

            writer.WriteElementString("ScanCount", scanCountTotal.ToString())

            writer.WriteElementString("ScanCountMS", summaryStats.MSStats.ScanCount.ToString())
            writer.WriteElementString("ScanCountMSn", summaryStats.MSnStats.ScanCount.ToString())
            writer.WriteElementString("Elution_Time_Max", summaryStats.ElutionTimeMax.ToString())

            writer.WriteElementString("AcqTimeMinutes", datasetInfo.AcqTimeEnd.Subtract(datasetInfo.AcqTimeStart).TotalMinutes.ToString("0.00"))
            writer.WriteElementString("StartTime", datasetInfo.AcqTimeStart.ToString(DATE_TIME_FORMAT_STRING))
            writer.WriteElementString("EndTime", datasetInfo.AcqTimeEnd.ToString(DATE_TIME_FORMAT_STRING))

            writer.WriteElementString("FileSizeBytes", datasetInfo.FileSizeBytes.ToString())

            writer.WriteEndElement()       ' AcquisitionInfo

            writer.WriteStartElement("TICInfo")
            writer.WriteElementString("TIC_Max_MS", StringUtilities.ValueToString(summaryStats.MSStats.TICMax, 5))
            writer.WriteElementString("TIC_Max_MSn", StringUtilities.ValueToString(summaryStats.MSnStats.TICMax, 5))
            writer.WriteElementString("BPI_Max_MS", StringUtilities.ValueToString(summaryStats.MSStats.BPIMax, 5))
            writer.WriteElementString("BPI_Max_MSn", StringUtilities.ValueToString(summaryStats.MSnStats.BPIMax, 5))
            writer.WriteElementString("TIC_Median_MS", StringUtilities.ValueToString(summaryStats.MSStats.TICMedian, 5))
            writer.WriteElementString("TIC_Median_MSn", StringUtilities.ValueToString(summaryStats.MSnStats.TICMedian, 5))
            writer.WriteElementString("BPI_Median_MS", StringUtilities.ValueToString(summaryStats.MSStats.BPIMedian, 5))
            writer.WriteElementString("BPI_Median_MSn", StringUtilities.ValueToString(summaryStats.MSnStats.BPIMedian, 5))
            writer.WriteEndElement()       ' TICInfo

            ' Only write the oSampleInfo block if oSampleInfo contains entries
            If oSampleInfo.HasData() Then
                writer.WriteStartElement("SampleInfo")
                writer.WriteElementString("SampleName", FixNull(oSampleInfo.SampleName))
                writer.WriteElementString("Comment1", FixNull(oSampleInfo.Comment1))
                writer.WriteElementString("Comment2", FixNull(oSampleInfo.Comment2))
                writer.WriteEndElement()       ' SampleInfo
            End If


            writer.WriteEndElement()  'End the "Root" element (DatasetInfo)
            writer.WriteEndDocument() 'End the document

            writer.Close()

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
    Public Function CreateScanStatsFile(datasetName As String, scanStatsFilePath As String) As Boolean
        Return CreateScanStatsFile(datasetName, scanStatsFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ''' <summary>
    ''' Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="scanStatsFilePath">File path to write the text file to</param>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <param name="oSampleInfo">Sample Info</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function CreateScanStatsFile(
        datasetName As String,
        scanStatsFilePath As String,
        scanStats As List(Of clsScanStatsEntry),
        datasetInfo As DatasetFileInfo,
        oSampleInfo As SampleInfo) As Boolean


        Dim datasetID = 0

        Try
            If scanStats Is Nothing Then
                ReportError("scanStats is Nothing; unable to continue in CreateScanStatsFile")
                Return False
            End If

            mErrorMessage = String.Empty

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

                Dim dataValues As New List(Of String)

                For Each scanStatsEntry As clsScanStatsEntry In scanStats

                    dataValues.Clear()

                    ' Dataset ID
                    dataValues.Add(datasetID.ToString())

                    ' Scan number
                    dataValues.Add(scanStatsEntry.ScanNumber.ToString())

                    ' Scan time (minutes)
                    dataValues.Add(scanStatsEntry.ElutionTime)

                    ' Scan type (1 for MS, 2 for MS2, etc.)
                    dataValues.Add(scanStatsEntry.ScanType.ToString())

                    ' Total ion intensity
                    dataValues.Add(scanStatsEntry.TotalIonIntensity)

                    ' Base peak ion intensity
                    dataValues.Add(scanStatsEntry.BasePeakIntensity)

                    ' Base peak ion m/z
                    dataValues.Add(scanStatsEntry.BasePeakMZ)

                    ' Base peak signal to noise ratio
                    dataValues.Add(scanStatsEntry.BasePeakSignalToNoiseRatio)

                    ' Number of peaks (aka ions) in the spectrum
                    dataValues.Add(scanStatsEntry.IonCount.ToString())

                    ' Number of peaks (aka ions) in the spectrum prior to any filtering
                    dataValues.Add(scanStatsEntry.IonCountRaw.ToString())

                    ' Scan type name
                    dataValues.Add(scanStatsEntry.ScanTypeName)

                    writer.WriteLine(String.Join(ControlChars.Tab, dataValues))

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
    Public Function UpdateDatasetScanType(scanNumber As Integer, scanType As Integer, scanTypeName As String) As Boolean

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
    Public Function UpdateDatasetStatsTextFile(datasetName As String, datasetInfoFilePath As String) As Boolean
        Return UpdateDatasetStatsTextFile(datasetName, datasetInfoFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
    End Function

    ''' <summary>
    ''' Updates a tab-delimited text file, adding a new line summarizing the data in scanStats and datasetInfo
    ''' </summary>
    ''' <param name="datasetName">Dataset Name</param>
    ''' <param name="datasetStatsFilePath">Tab-delimited file to create/update</param>
    ''' <param name="scanStats">Scan stats to parse</param>
    ''' <param name="datasetInfo">Dataset Info</param>
    ''' <param name="oSampleInfo">Sample Info</param>
    ''' <returns>True if success; False if failure</returns>
    ''' <remarks></remarks>
    Public Function UpdateDatasetStatsTextFile(
        datasetName As String,
        datasetStatsFilePath As String,
        scanStats As List(Of clsScanStatsEntry),
        datasetInfo As DatasetFileInfo,
        oSampleInfo As SampleInfo) As Boolean

        Dim writeHeaders As Boolean

        Dim summaryStats As clsDatasetSummaryStats

        Try

            If scanStats Is Nothing Then
                ReportError("scanStats is Nothing; unable to continue in UpdateDatasetStatsTextFile")
                Return False
            End If

            mErrorMessage = String.Empty

            If scanStats Is mDatasetScanStats Then
                summaryStats = GetDatasetSummaryStats()
            Else
                summaryStats = New clsDatasetSummaryStats()

                ' Parse the data in scanStats to compute the bulk values
                Dim summarySuccess = Me.ComputeScanStatsSummary(scanStats, summaryStats)
                If Not summarySuccess Then
                    ReportError("ComputeScanStatsSummary returned false; unable to continue in UpdateDatasetStatsTextFile")
                    Return False
                End If
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

                Dim dataValues = New List(Of String) From {
                        datasetName,
                        (summaryStats.MSStats.ScanCount + summaryStats.MSnStats.ScanCount).ToString(),
                        summaryStats.MSStats.ScanCount.ToString(),
                        summaryStats.MSnStats.ScanCount.ToString(),
                        summaryStats.ElutionTimeMax.ToString("0.00"),
                        datasetInfo.AcqTimeEnd.Subtract(datasetInfo.AcqTimeStart).TotalMinutes.ToString("0.00"),
                        datasetInfo.AcqTimeStart.ToString(DATE_TIME_FORMAT_STRING),
                        datasetInfo.AcqTimeEnd.ToString(DATE_TIME_FORMAT_STRING),
                        datasetInfo.FileSizeBytes.ToString(),
                        FixNull(oSampleInfo.SampleName),
                        FixNull(oSampleInfo.Comment1),
                        FixNull(oSampleInfo.Comment2)
                }

                writer.WriteLine(String.Join(ControlChars.Tab, dataValues))

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

    ''' <summary>
    ''' Keeps track of each ScanType in the dataset, along with the number of scans of this type
    ''' </summary>
    ''' <remarks>
    ''' Examples:
    '''   FTMS + p NSI Full ms
    '''   ITMS + c ESI Full ms
    '''   ITMS + p ESI d Z ms
    '''   ITMS + c ESI d Full ms2 @cid35.00
    ''' </remarks>
    Public ReadOnly ScanTypeStats As Dictionary(Of String, Integer)

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

        MSStats.ScanCount = 0
        MSStats.TICMax = 0
        MSStats.BPIMax = 0
        MSStats.TICMedian = 0
        MSStats.BPIMedian = 0

        MSnStats.ScanCount = 0
        MSnStats.TICMax = 0
        MSnStats.BPIMax = 0
        MSnStats.TICMedian = 0
        MSnStats.BPIMedian = 0

        ScanTypeStats.Clear()

    End Sub

    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        ScanTypeStats = New Dictionary(Of String, Integer)
        Me.Clear()
    End Sub

End Class