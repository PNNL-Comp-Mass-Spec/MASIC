Option Strict On

' This class computes aggregate stats for a dataset
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started May 7, 2009
' Ported from clsMASICScanStatsParser to clsDatasetStatsSummarizer in February 2010
'
' E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
' Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov
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

Imports PNNLOmics.Utilities

Namespace DSSummarizer

	Public Class clsDatasetStatsSummarizer

#Region "Constants and Enums"
		Public Const SCANTYPE_STATS_SEPCHAR As String = "::###::"
		Public Const DATASET_INFO_FILE_SUFFIX As String = "_DatasetInfo.xml"
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
				FileSystemCreationTime = System.DateTime.MinValue
				FileSystemModificationTime = System.DateTime.MinValue
				DatasetID = 0
				DatasetName = String.Empty
				FileExtension = String.Empty
				AcqTimeStart = System.DateTime.MinValue
				AcqTimeEnd = System.DateTime.MinValue
				ScanCount = 0
				FileSizeBytes = 0
			End Sub
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
				If Not String.IsNullOrEmpty(SampleName) OrElse _
				   Not String.IsNullOrEmpty(Comment1) OrElse _
				   Not String.IsNullOrEmpty(Comment2) Then
					Return True
				Else
					Return False
				End If
			End Function
		End Structure

#End Region

#Region "Classwide Variables"
		Protected mFileDate As String
		Protected mDatasetStatsSummaryFileName As String
		Protected mErrorMessage As String = String.Empty

		Protected mDatasetScanStats As Generic.List(Of clsScanStatsEntry)
		Public DatasetFileInfo As udtDatasetFileInfoType
		Public SampleInfo As udtSampleInfoType

		Protected mDatasetSummaryStatsUpToDate As Boolean
		Protected mDatasetSummaryStats As clsDatasetSummaryStats

#End Region

#Region "Properties"

		Public Property DatasetStatsSummaryFileName() As String
			Get
				Return mDatasetStatsSummaryFileName
			End Get
			Set(ByVal value As String)
				If Not value Is Nothing Then
					mDatasetStatsSummaryFileName = value
				End If
			End Set
		End Property

		Public ReadOnly Property ErrorMessage() As String
			Get
				Return mErrorMessage
			End Get
		End Property

		Public ReadOnly Property FileDate() As String
			Get
				FileDate = mFileDate
			End Get
		End Property

#End Region

		Public Sub New()
			mFileDate = "November 29, 2010"
			InitializeLocalVariables()
		End Sub

		Public Sub AddDatasetScan(ByVal objScanStats As clsScanStatsEntry)

			mDatasetScanStats.Add(objScanStats)
			mDatasetSummaryStatsUpToDate = False

		End Sub

		Public Sub ClearCachedData()
			If mDatasetScanStats Is Nothing Then
				mDatasetScanStats = New Generic.List(Of clsScanStatsEntry)
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
		''' <param name="objSummaryStats">Stats output</param>
		''' <returns>>True if success, false if error</returns>
		''' <remarks></remarks>
		Public Function ComputeScanStatsSummary(ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		 ByRef objSummaryStats As clsDatasetSummaryStats) As Boolean

			Dim intScanStatsCount As Integer
			Dim objEntry As clsScanStatsEntry

			Dim strScanTypeKey As String

			Dim blnSuccess As Boolean = False

			Dim dblTICListMS() As Double
			Dim intTICListMSCount As Integer = 0

			Dim dblTICListMSn() As Double
			Dim intTICListMSnCount As Integer = 0

			Dim dblBPIListMS() As Double
			Dim intBPIListMSCount As Integer = 0

			Dim dblBPIListMSn() As Double
			Dim intBPIListMSnCount As Integer = 0

			Try

				If objScanStats Is Nothing Then
					mErrorMessage = "objScanStats is Nothing; unable to continue"
					Return False
				Else
					mErrorMessage = ""
				End If

				intScanStatsCount = objScanStats.Count

				' Initialize objSummaryStats
				If objSummaryStats Is Nothing Then
					objSummaryStats = New clsDatasetSummaryStats
				Else
					objSummaryStats.Clear()
				End If

				' Initialize the TIC and BPI List arrays
				ReDim dblTICListMS(intScanStatsCount - 1)
				ReDim dblBPIListMS(intScanStatsCount - 1)

				ReDim dblTICListMSn(intScanStatsCount - 1)
				ReDim dblBPIListMSn(intScanStatsCount - 1)

				For Each objEntry In objScanStats

					If objEntry.ScanType > 1 Then
						' MSn spectrum
						ComputeScanStatsUpdateDetails(objEntry, objSummaryStats.ElutionTimeMax, _
						   objSummaryStats.MSnStats, _
						   dblTICListMSn, intTICListMSnCount, _
						   dblBPIListMSn, intBPIListMSnCount)
					Else
						' MS spectrum
						ComputeScanStatsUpdateDetails(objEntry, objSummaryStats.ElutionTimeMax, _
						   objSummaryStats.MSStats, _
						   dblTICListMS, intTICListMSCount, _
						   dblBPIListMS, intBPIListMSCount)
					End If

					strScanTypeKey = objEntry.ScanTypeName & SCANTYPE_STATS_SEPCHAR & objEntry.ScanFilterText
					If objSummaryStats.objScanTypeStats.ContainsKey(strScanTypeKey) Then
						objSummaryStats.objScanTypeStats.Item(strScanTypeKey) += 1
					Else
						objSummaryStats.objScanTypeStats.Add(strScanTypeKey, 1)
					End If
				Next

				objSummaryStats.MSStats.TICMedian = ComputeMedian(dblTICListMS, intTICListMSCount)
				objSummaryStats.MSStats.BPIMedian = ComputeMedian(dblBPIListMS, intBPIListMSCount)

				objSummaryStats.MSnStats.TICMedian = ComputeMedian(dblTICListMSn, intTICListMSnCount)
				objSummaryStats.MSnStats.BPIMedian = ComputeMedian(dblBPIListMSn, intBPIListMSnCount)

				blnSuccess = True

			Catch ex As System.Exception
				mErrorMessage = "Error in ComputeScanStatsSummary: " & ex.Message
			End Try

			Return blnSuccess

		End Function

		Protected Sub ComputeScanStatsUpdateDetails(ByRef objScanStats As clsScanStatsEntry, _
		  ByRef dblElutionTimeMax As Double, _
		  ByRef udtSummaryStatDetails As clsDatasetSummaryStats.udtSummaryStatDetailsType, _
		  ByRef dblTICList() As Double, _
		  ByRef intTICListCount As Integer, _
		  ByRef dblBPIList() As Double, _
		  ByRef intBPIListCount As Integer)

			Dim dblElutionTime As Double
			Dim dblTIC As Double
			Dim dblBPI As Double

			If objScanStats.ElutionTime <> Nothing AndAlso objScanStats.ElutionTime.Length > 0 Then
				If Double.TryParse(objScanStats.ElutionTime, dblElutionTime) Then
					If dblElutionTime > dblElutionTimeMax Then
						dblElutionTimeMax = dblElutionTime
					End If
				End If
			End If

			If Double.TryParse(objScanStats.TotalIonIntensity, dblTIC) Then
				If dblTIC > udtSummaryStatDetails.TICMax Then
					udtSummaryStatDetails.TICMax = dblTIC
				End If

				dblTICList(intTICListCount) = dblTIC
				intTICListCount += 1
			End If

			If Double.TryParse(objScanStats.BasePeakIntensity, dblBPI) Then
				If dblBPI > udtSummaryStatDetails.BPIMax Then
					udtSummaryStatDetails.BPIMax = dblBPI
				End If

				dblBPIList(intBPIListCount) = dblBPI
				intBPIListCount += 1
			End If

			udtSummaryStatDetails.ScanCount += 1

		End Sub

		Protected Function ComputeMedian(ByRef dblList() As Double, ByVal intItemCount As Integer) As Double

			Dim intMidpointIndex As Integer
			Dim blnAverage As Boolean

			If dblList Is Nothing OrElse dblList.Length < 1 OrElse intItemCount < 1 Then
				' List is empty (or intItemCount = 0)
				Return 0
			ElseIf intItemCount <= 1 Then
				' Only 1 item; the median is the value
				Return dblList(0)
			Else
				' Sort dblList ascending, then find the midpoint
				Array.Sort(dblList, 0, intItemCount)

				If intItemCount Mod 2 = 0 Then
					' Even number
					intMidpointIndex = CInt(Math.Floor(intItemCount / 2)) - 1
					blnAverage = True
				Else
					' Odd number
					intMidpointIndex = CInt(Math.Floor(intItemCount / 2))
				End If

				If intMidpointIndex > intItemCount Then intMidpointIndex = intItemCount - 1
				If intMidpointIndex < 0 Then intMidpointIndex = 0

				If blnAverage Then
					' Even number of items
					' Return the average of the two middle points
					Return (dblList(intMidpointIndex) + dblList(intMidpointIndex + 1)) / 2
				Else
					' Odd number of items
					Return dblList(intMidpointIndex)
				End If

				Return dblList(intMidpointIndex)
			End If

		End Function

		''' <summary>
		''' Creates an XML file summarizing the data stored in this class (in mDatasetScanStats, Me.DatasetFileInfo, and Me.SampleInfo)
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="strDatasetInfoFilePath">File path to write the XML to</param>
		''' <returns>True if success; False if failure</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoFile(ByVal strDatasetName As String, _
		  ByVal strDatasetInfoFilePath As String) As Boolean

			Return CreateDatasetInfoFile(strDatasetName, strDatasetInfoFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
		End Function

		''' <summary>
		''' Creates an XML file summarizing the data in objScanStats and udtDatasetFileInfo
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="strDatasetInfoFilePath">File path to write the XML to</param>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <param name="udtSampleInfo">Sample Info</param>
		''' <returns>True if success; False if failure</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoFile(ByVal strDatasetName As String, _
		  ByVal strDatasetInfoFilePath As String, _
		  ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		  ByRef udtDatasetFileInfo As udtDatasetFileInfoType, _
		  ByRef udtSampleInfo As udtSampleInfoType) As Boolean

			Dim swOutFile As System.IO.StreamWriter

			Dim blnSuccess As Boolean

			Try
				If objScanStats Is Nothing Then
					mErrorMessage = "objScanStats is Nothing; unable to continue"
					Return False
				Else
					mErrorMessage = ""
				End If

				' If CreateDatasetInfoXML() used a StringBuilder to cache the XML data, then we would have to use System.Text.Encoding.Unicode
				' However, CreateDatasetInfoXML() now uses a MemoryStream, so we're able to use UTF8
				swOutFile = New System.IO.StreamWriter(New System.IO.FileStream(strDatasetInfoFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read), System.Text.Encoding.UTF8)

				swOutFile.WriteLine(CreateDatasetInfoXML(strDatasetName, objScanStats, udtDatasetFileInfo, udtSampleInfo))

				swOutFile.Close()

				blnSuccess = True

			Catch ex As System.Exception
				blnSuccess = False
				mErrorMessage = "Error in CreateDatasetInfoFile: " & ex.Message
			End Try

			Return blnSuccess

		End Function

		''' <summary>
		''' Creates XML summarizing the data stored in this class (in mDatasetScanStats, Me.DatasetFileInfo, and Me.SampleInfo)
		''' Auto-determines the dataset name using Me.DatasetFileInfo.DatasetName
		''' </summary>
		''' <returns>XML (as string)</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoXML() As String
			Return CreateDatasetInfoXML(Me.DatasetFileInfo.DatasetName, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
		End Function

		''' <summary>
		''' Creates XML summarizing the data stored in this class (in mDatasetScanStats, Me.DatasetFileInfo, and Me.SampleInfo)
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <returns>XML (as string)</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoXML(ByVal strDatasetName As String) As String
			Return CreateDatasetInfoXML(strDatasetName, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
		End Function


		''' <summary>
		''' Creates XML summarizing the data in objScanStats and udtDatasetFileInfo
		''' Auto-determines the dataset name using udtDatasetFileInfo.DatasetName
		''' </summary>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <returns>XML (as string)</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoXML(ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		 ByRef udtDatasetFileInfo As udtDatasetFileInfoType) As String

			Return CreateDatasetInfoXML(udtDatasetFileInfo.DatasetName, objScanStats, udtDatasetFileInfo)
		End Function

		''' <summary>
		''' Creates XML summarizing the data in objScanStats, udtDatasetFileInfo, and udtSampleInfo
		''' Auto-determines the dataset name using udtDatasetFileInfo.DatasetName
		''' </summary>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <param name="udtSampleInfo">Sample Info</param>
		''' <returns>XML (as string)</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoXML(ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		 ByRef udtDatasetFileInfo As udtDatasetFileInfoType, _
		 ByRef udtSampleInfo As udtSampleInfoType) As String

			Return CreateDatasetInfoXML(udtDatasetFileInfo.DatasetName, objScanStats, udtDatasetFileInfo, udtSampleInfo)
		End Function

		''' <summary>
		''' Creates XML summarizing the data in objScanStats and udtDatasetFileInfo
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <returns>XML (as string)</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoXML(ByVal strDatasetName As String, _
		 ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		 ByRef udtDatasetFileInfo As udtDatasetFileInfoType) As String

			Dim udtSampleInfo As udtSampleInfoType = New udtSampleInfoType
			udtSampleInfo.Clear()

			Return CreateDatasetInfoXML(strDatasetName, objScanStats, udtDatasetFileInfo, udtSampleInfo)
		End Function

		''' <summary>
		''' Creates XML summarizing the data in objScanStats and udtDatasetFileInfo
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <returns>XML (as string)</returns>
		''' <remarks></remarks>
		Public Function CreateDatasetInfoXML(ByVal strDatasetName As String, _
		 ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		 ByRef udtDatasetFileInfo As udtDatasetFileInfoType, _
		 ByRef udtSampleInfo As udtSampleInfoType) As String

			' Create a MemoryStream to hold the results
			Dim objMemStream As System.IO.MemoryStream
			Dim objXMLSettings As System.Xml.XmlWriterSettings

			Dim objDSInfo As System.Xml.XmlWriter
			Dim objEnum As Generic.Dictionary(Of String, Integer).Enumerator

			Dim objSummaryStats As DSSummarizer.clsDatasetSummaryStats

			Dim intIndexMatch As Integer
			Dim strScanType As String
			Dim strScanFilterText As String

			Try

				If objScanStats Is Nothing Then
					mErrorMessage = "objScanStats is Nothing; unable to continue"
					Return String.Empty
				Else
					mErrorMessage = ""
				End If

				If objScanStats Is mDatasetScanStats Then
					objSummaryStats = GetDatasetSummaryStats()
				Else
					objSummaryStats = New clsDatasetSummaryStats

					' Parse the data in objScanStats to compute the bulk values
					Me.ComputeScanStatsSummary(objScanStats, objSummaryStats)
				End If

				objXMLSettings = New System.Xml.XmlWriterSettings()

				With objXMLSettings
					.CheckCharacters = True
					.Indent = True
					.IndentChars = "  "
					.Encoding = System.Text.Encoding.UTF8

					' Do not close output automatically so that MemoryStream
					' can be read after the XmlWriter has been closed
					.CloseOutput = False
				End With

				' We could cache the text using a StringBuilder, like this:
				'
				' Dim sbDatasetInfo As New System.Text.StringBuilder
				' Dim objStringWriter As System.IO.StringWriter
				' objStringWriter = New System.IO.StringWriter(sbDatasetInfo)
				' objDSInfo = New System.Xml.XmlTextWriter(objStringWriter)
				' objDSInfo.Formatting = System.Xml.Formatting.Indented
				' objDSInfo.Indentation = 2

				' However, when you send the output to a StringBuilder it is always encoded as Unicode (UTF-16) 
				'  since this is the only character encoding used in the .NET Framework for String values, 
				'  and thus you'll see the attribute encoding="utf-16" in the opening XML declaration 
				' The alternative is to use a MemoryStream.  Here, the stream encoding is set by the XmlWriter 
				'  and so you see the attribute encoding="utf-8" in the opening XML declaration encoding 
				'  (since we used objXMLSettings.Encoding = System.Text.Encoding.UTF8)
				'
				objMemStream = New System.IO.MemoryStream()
				objDSInfo = System.Xml.XmlWriter.Create(objMemStream, objXMLSettings)

				objDSInfo.WriteStartDocument(True)

				'Write the beginning of the "Root" element.
				objDSInfo.WriteStartElement("DatasetInfo")

				objDSInfo.WriteElementString("Dataset", strDatasetName)

				objDSInfo.WriteStartElement("ScanTypes")

				objEnum = objSummaryStats.objScanTypeStats.GetEnumerator()
				Do While objEnum.MoveNext

					strScanType = objEnum.Current.Key
					intIndexMatch = strScanType.IndexOf(clsDatasetStatsSummarizer.SCANTYPE_STATS_SEPCHAR)

					If intIndexMatch >= 0 Then
						strScanFilterText = strScanType.Substring(intIndexMatch + clsDatasetStatsSummarizer.SCANTYPE_STATS_SEPCHAR.Length)
						If intIndexMatch > 0 Then
							strScanType = strScanType.Substring(0, intIndexMatch)
						Else
							strScanType = String.Empty
						End If
					Else
						strScanFilterText = String.Empty
					End If

					objDSInfo.WriteStartElement("ScanType")
					objDSInfo.WriteAttributeString("ScanCount", objEnum.Current.Value.ToString)
					objDSInfo.WriteAttributeString("ScanFilterText", FixNull(strScanFilterText))
					objDSInfo.WriteString(strScanType)
					objDSInfo.WriteEndElement()		' ScanType
				Loop

				objDSInfo.WriteEndElement()		  ' ScanTypes

				objDSInfo.WriteStartElement("AcquisitionInfo")

				objDSInfo.WriteElementString("ScanCount", (objSummaryStats.MSStats.ScanCount + objSummaryStats.MSnStats.ScanCount).ToString)
				objDSInfo.WriteElementString("ScanCountMS", objSummaryStats.MSStats.ScanCount.ToString)
				objDSInfo.WriteElementString("ScanCountMSn", objSummaryStats.MSnStats.ScanCount.ToString)
				objDSInfo.WriteElementString("Elution_Time_Max", objSummaryStats.ElutionTimeMax.ToString)

				objDSInfo.WriteElementString("AcqTimeMinutes", udtDatasetFileInfo.AcqTimeEnd.Subtract(udtDatasetFileInfo.AcqTimeStart).TotalMinutes.ToString("0.00"))
				objDSInfo.WriteElementString("StartTime", udtDatasetFileInfo.AcqTimeStart.ToString("yyyy-MM-dd hh:mm:ss tt"))
				objDSInfo.WriteElementString("EndTime", udtDatasetFileInfo.AcqTimeEnd.ToString("yyyy-MM-dd hh:mm:ss tt"))

				objDSInfo.WriteElementString("FileSizeBytes", udtDatasetFileInfo.FileSizeBytes.ToString)
				objDSInfo.WriteEndElement()		  ' AcquisitionInfo

				objDSInfo.WriteStartElement("TICInfo")
				objDSInfo.WriteElementString("TIC_Max_MS", MathUtilities.ValueToString(objSummaryStats.MSStats.TICMax, 5))
				objDSInfo.WriteElementString("TIC_Max_MSn", MathUtilities.ValueToString(objSummaryStats.MSnStats.TICMax, 5))
				objDSInfo.WriteElementString("BPI_Max_MS", MathUtilities.ValueToString(objSummaryStats.MSStats.BPIMax, 5))
				objDSInfo.WriteElementString("BPI_Max_MSn", MathUtilities.ValueToString(objSummaryStats.MSnStats.BPIMax, 5))
				objDSInfo.WriteElementString("TIC_Median_MS", MathUtilities.ValueToString(objSummaryStats.MSStats.TICMedian, 5))
				objDSInfo.WriteElementString("TIC_Median_MSn", MathUtilities.ValueToString(objSummaryStats.MSnStats.TICMedian, 5))
				objDSInfo.WriteElementString("BPI_Median_MS", MathUtilities.ValueToString(objSummaryStats.MSStats.BPIMedian, 5))
				objDSInfo.WriteElementString("BPI_Median_MSn", MathUtilities.ValueToString(objSummaryStats.MSnStats.BPIMedian, 5))
				objDSInfo.WriteEndElement()		  ' TICInfo

				' Only write the SampleInfo block if udtSampleInfo contains entries
				If udtSampleInfo.HasData() Then
					objDSInfo.WriteStartElement("SampleInfo")
					objDSInfo.WriteElementString("SampleName", FixNull(udtSampleInfo.SampleName))
					objDSInfo.WriteElementString("Comment1", FixNull(udtSampleInfo.Comment1))
					objDSInfo.WriteElementString("Comment2", FixNull(udtSampleInfo.Comment2))
					objDSInfo.WriteEndElement()		  ' SampleInfo
				End If


				objDSInfo.WriteEndElement()	 'End the "Root" element (DatasetInfo)
				objDSInfo.WriteEndDocument() 'End the document

				objDSInfo.Close()
				objDSInfo = Nothing

				' Now Rewind the memory stream and output as a string
				objMemStream.Position = 0
				Dim srStreamReader As System.IO.StreamReader
				srStreamReader = New System.IO.StreamReader(objMemStream)

				' Return the XML as text
				Return srStreamReader.ReadToEnd()

			Catch ex As System.Exception
				mErrorMessage = "Error in CreateDatasetInfoXML: " & ex.Message
			End Try

			' This code will only be reached if an exception occurs
			Return String.Empty

		End Function

		''' <summary>
		''' Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="strScanStatsFilePath">File path to write the text file to</param>
		''' <returns>True if success; False if failure</returns>
		''' <remarks></remarks>
		Public Function CreateScanStatsFile(ByVal strDatasetName As String, _
		  ByVal strScanStatsFilePath As String) As Boolean

			Return CreateScanStatsFile(strDatasetName, strScanStatsFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
		End Function

		''' <summary>
		''' Creates a tab-delimited text file with details on each scan tracked by this class (stored in mDatasetScanStats)
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="strScanStatsFilePath">File path to write the text file to</param>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <param name="udtSampleInfo">Sample Info</param>
		''' <returns>True if success; False if failure</returns>
		''' <remarks></remarks>
		Public Function CreateScanStatsFile(ByVal strDatasetName As String, _
		  ByVal strScanStatsFilePath As String, _
		  ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		  ByRef udtDatasetFileInfo As udtDatasetFileInfoType, _
		  ByRef udtSampleInfo As udtSampleInfoType) As Boolean


			Dim swOutFile As System.IO.StreamWriter
			Dim intDatasetNumber As Integer = 0
			Dim strLineOut As String

			Dim blnSuccess As Boolean

			Try
				If objScanStats Is Nothing Then
					mErrorMessage = "objScanStats is Nothing; unable to continue"
					Return False
				Else
					mErrorMessage = ""
				End If

				swOutFile = New System.IO.StreamWriter(New System.IO.FileStream(strScanStatsFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))

				swOutFile.WriteLine("Dataset" & ControlChars.Tab & "ScanNumber" & ControlChars.Tab & "ScanTime" & ControlChars.Tab & _
				 "ScanType" & ControlChars.Tab & "TotalIonIntensity" & ControlChars.Tab & "BasePeakIntensity" & ControlChars.Tab & _
				 "BasePeakMZ" & ControlChars.Tab & "BasePeakSignalToNoiseRatio" & ControlChars.Tab & _
				 "IonCount" & ControlChars.Tab & "IonCountRaw" & ControlChars.Tab & "ScanTypeName")


				For Each objScanStatsEntry As clsScanStatsEntry In objScanStats

					strLineOut = String.Empty
					strLineOut &= intDatasetNumber.ToString & ControlChars.Tab						' Dataset number
					strLineOut &= objScanStatsEntry.ScanNumber.ToString & ControlChars.Tab			' Scan number
					strLineOut &= objScanStatsEntry.ElutionTime & ControlChars.Tab					' Scan time (minutes)
					strLineOut &= objScanStatsEntry.ScanType.ToString & ControlChars.Tab			' Scan type (1 for MS, 2 for MS2, etc.)
					strLineOut &= objScanStatsEntry.TotalIonIntensity & ControlChars.Tab			' Total ion intensity
					strLineOut &= objScanStatsEntry.BasePeakIntensity & ControlChars.Tab			' Base peak ion intensity
					strLineOut &= objScanStatsEntry.BasePeakMZ & ControlChars.Tab					' Base peak ion m/z
					strLineOut &= objScanStatsEntry.BasePeakSignalToNoiseRatio & ControlChars.Tab	' Base peak signal to noise ratio
					strLineOut &= objScanStatsEntry.IonCount.ToString & ControlChars.Tab			' Number of peaks (aka ions) in the spectrum
					strLineOut &= objScanStatsEntry.IonCountRaw.ToString & ControlChars.Tab		' Number of peaks (aka ions) in the spectrum prior to any filtering
					strLineOut &= objScanStatsEntry.ScanTypeName								' Scan type name

					swOutFile.WriteLine(strLineOut)

				Next


				swOutFile.Close()

				blnSuccess = True

			Catch ex As System.Exception
				blnSuccess = False
				mErrorMessage = "Error in CreateScanStatsFile: " & ex.Message
			End Try

			Return blnSuccess

		End Function

		Protected Function FixNull(ByVal strText As String) As String
			If String.IsNullOrEmpty(strText) Then
				Return String.Empty
			Else
				Return strText
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

		''' <summary>
		''' Updates the scan type information for the specified scan number
		''' </summary>
		''' <param name="intScanNumber"></param>
		''' <param name="intScanType"></param>
		''' <param name="strScanTypeName"></param>
		''' <returns>True if the scan was found and updated; otherwise false</returns>
		''' <remarks></remarks>
		Public Function UpdateDatasetScanType(ByVal intScanNumber As Integer, _
		  ByVal intScanType As Integer, _
		  ByVal strScanTypeName As String) As Boolean

			Dim intIndex As Integer
			Dim blnMatchFound As Boolean

			' Look for scan intScanNumber in mDatasetScanStats
			For intIndex = 0 To mDatasetScanStats.Count - 1
				If mDatasetScanStats(intIndex).ScanNumber = intScanNumber Then
					mDatasetScanStats(intIndex).ScanType = intScanType
					mDatasetScanStats(intIndex).ScanTypeName = strScanTypeName
					mDatasetSummaryStatsUpToDate = False

					blnMatchFound = True
					Exit For
				End If
			Next

			Return blnMatchFound

		End Function

		''' <summary>
		''' Updates a tab-delimited text file, adding a new line summarizing the data stored in this class (in mDatasetScanStats and Me.DatasetFileInfo)
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="strDatasetInfoFilePath">File path to write the XML to</param>
		''' <returns>True if success; False if failure</returns>
		''' <remarks></remarks>
		Public Function UpdateDatasetStatsTextFile(ByVal strDatasetName As String, _
		 ByVal strDatasetInfoFilePath As String) As Boolean

			Return UpdateDatasetStatsTextFile(strDatasetName, strDatasetInfoFilePath, mDatasetScanStats, Me.DatasetFileInfo, Me.SampleInfo)
		End Function

		''' <summary>
		''' Updates a tab-delimited text file, adding a new line summarizing the data in objScanStats and udtDatasetFileInfo
		''' </summary>
		''' <param name="strDatasetName">Dataset Name</param>
		''' <param name="strDatasetStatsFilePath">Tab-delimited file to create/update</param>
		''' <param name="objScanStats">Scan stats to parse</param>
		''' <param name="udtDatasetFileInfo">Dataset Info</param>
		''' <param name="udtSampleInfo">Sample Info</param>
		''' <returns>True if success; False if failure</returns>
		''' <remarks></remarks>
		Public Function UpdateDatasetStatsTextFile(ByVal strDatasetName As String, _
		 ByVal strDatasetStatsFilePath As String, _
		 ByRef objScanStats As Generic.List(Of clsScanStatsEntry), _
		 ByRef udtDatasetFileInfo As udtDatasetFileInfoType, _
		 ByRef udtSampleInfo As udtSampleInfoType) As Boolean

			Dim swOutFile As System.IO.StreamWriter
			Dim blnWriteHeaders As Boolean

			Dim strLineOut As String

			Dim objSummaryStats As clsDatasetSummaryStats

			Dim blnSuccess As Boolean

			Try

				If objScanStats Is Nothing Then
					mErrorMessage = "objScanStats is Nothing; unable to continue"
					Return False
				Else
					mErrorMessage = ""
				End If

				If objScanStats Is mDatasetScanStats Then
					objSummaryStats = GetDatasetSummaryStats()
				Else
					objSummaryStats = New clsDatasetSummaryStats

					' Parse the data in objScanStats to compute the bulk values
					Me.ComputeScanStatsSummary(objScanStats, objSummaryStats)
				End If

				If Not System.IO.File.Exists(strDatasetStatsFilePath) Then
					blnWriteHeaders = True
				End If

				' Create or open the output file
				swOutFile = New System.IO.StreamWriter(New System.IO.FileStream(strDatasetStatsFilePath, IO.FileMode.Append, IO.FileAccess.Write, IO.FileShare.Read))

				If blnWriteHeaders Then
					' Write the header line
					strLineOut = "Dataset" & ControlChars.Tab & _
					 "ScanCount" & ControlChars.Tab & _
					 "ScanCountMS" & ControlChars.Tab & _
					 "ScanCountMSn" & ControlChars.Tab & _
					 "Elution_Time_Max" & ControlChars.Tab & _
					 "AcqTimeMinutes" & ControlChars.Tab & _
					 "StartTime" & ControlChars.Tab & _
					 "EndTime" & ControlChars.Tab & _
					 "FileSizeBytes" & ControlChars.Tab & _
					 "SampleName" & ControlChars.Tab & _
					 "Comment1" & ControlChars.Tab & _
					 "Comment2"

					swOutFile.WriteLine(strLineOut)
				End If

				strLineOut = strDatasetName & ControlChars.Tab & _
				 (objSummaryStats.MSStats.ScanCount + objSummaryStats.MSnStats.ScanCount).ToString & ControlChars.Tab & _
				 objSummaryStats.MSStats.ScanCount.ToString & ControlChars.Tab & _
				 objSummaryStats.MSnStats.ScanCount.ToString & ControlChars.Tab & _
				 objSummaryStats.ElutionTimeMax.ToString & ControlChars.Tab & _
				 udtDatasetFileInfo.AcqTimeEnd.Subtract(udtDatasetFileInfo.AcqTimeStart).TotalMinutes.ToString("0.00") & ControlChars.Tab & _
				 udtDatasetFileInfo.AcqTimeStart.ToString("yyyy-MM-dd hh:mm:ss tt") & ControlChars.Tab & _
				 udtDatasetFileInfo.AcqTimeEnd.ToString("yyyy-MM-dd hh:mm:ss tt") & ControlChars.Tab & _
				 udtDatasetFileInfo.FileSizeBytes.ToString & ControlChars.Tab & _
				 FixNull(udtSampleInfo.SampleName) & ControlChars.Tab & _
				 FixNull(udtSampleInfo.Comment1) & ControlChars.Tab & _
				 FixNull(udtSampleInfo.Comment2)

				swOutFile.WriteLine(strLineOut)

				swOutFile.Close()

				blnSuccess = True

			Catch ex As System.Exception
				blnSuccess = False
				mErrorMessage = "Error in UpdateDatasetStatsTextFile: " & ex.Message
			End Try

			Return blnSuccess

		End Function
	End Class

	Public Class clsScanStatsEntry
		Public ScanNumber As Integer
		Public ScanType As Integer				' 1 for MS, 2 for MS2, 3 for MS3

		Public ScanFilterText As String		   ' Example values: "FTMS + p NSI Full ms [400.00-2000.00]" or "ITMS + c ESI Full ms [300.00-2000.00]" or "ITMS + p ESI d Z ms [1108.00-1118.00]" or "ITMS + c ESI d Full ms2 342.90@cid35.00"
		Public ScanTypeName As String		   ' Example values: MS, HMS, Zoom, CID-MSn, or PQD-MSn

		' The following are strings to prevent the number formatting from changing
		Public ElutionTime As String
		Public TotalIonIntensity As String
		Public BasePeakIntensity As String
		Public BasePeakMZ As String
		Public BasePeakSignalToNoiseRatio As String

		Public IonCount As Integer
		Public IonCountRaw As Integer

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

		Public ElutionTimeMax As Double
		Public MSStats As udtSummaryStatDetailsType
		Public MSnStats As udtSummaryStatDetailsType

		' The following collection keeps track of each ScanType in the dataset, along with the number of scans of this type
		' Example scan types:  FTMS + p NSI Full ms" or "ITMS + c ESI Full ms" or "ITMS + p ESI d Z ms" or "ITMS + c ESI d Full ms2 @cid35.00"
		Public objScanTypeStats As Generic.Dictionary(Of String, Integer)

		Public Structure udtSummaryStatDetailsType
			Public ScanCount As Integer
			Public TICMax As Double
			Public BPIMax As Double
			Public TICMedian As Double
			Public BPIMedian As Double
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
				objScanTypeStats = New Generic.Dictionary(Of String, Integer)
			Else
				objScanTypeStats.Clear()
			End If

		End Sub

		Public Sub New()
			Me.Clear()
		End Sub

	End Class

End Namespace
