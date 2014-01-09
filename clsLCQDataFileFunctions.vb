Option Explicit On
Option Strict On

Imports ThermoRawFileReaderDLL.FinniganFileIO

Public Class ICR2LSFileIO
	Inherits FinniganFileReaderBaseClass

	' LCQ data export functions
	'
	' Functions written by Gordon Anderson for use in ICR-2LS
	' Ported from VB 6 to VB.NET by Matthew Monroe in October 2003

	' Last modified November 20, 2004

	' Declare statements for icr2ls32.dll
	' Note: Icr-2LS numbers scans from 0 to ScanCount-1

	' LCQ (Navigator) data file read routines...
	Private Declare Function LCQlocate Lib "icr2ls32.dll" (ByVal FileHandle As Int32, ByRef ScanNum As Int32, ByRef E As Int32, ByRef S As Int32, ByRef StartMZ As Single, ByRef StopMZ As Single, ByRef NumPoints As Int32) As Int32
	' Note that hArray is a pointer to a pointer, and thus cannot be (easily) called from VB.NET
	'Private Declare Function LCQload Lib "icr2ls32.dll" (ByVal hArray As Integer, ByVal Num As Int32, ByVal hLoadFile As Int32, ByVal Pos As Int32) As Int32
	Private Declare Function LCQcentroidNumPeaks Lib "icr2ls32.dll" (ByVal FileHandle As Int32, ByVal Scan As Int32) As Int32
	Private Declare Function LCQcentroidGetPeaks Lib "icr2ls32.dll" (ByVal FileHandle As Int32, ByVal Scan As Int32, ByRef X As Single, ByRef Y As Single) As Int32
	Private Declare Function LCQmzRange Lib "icr2ls32.dll" (ByVal hLoadFile As Int32, ByVal Scan As Int32, ByRef Min As Single, ByRef Max As Single) As Int32
	Private Declare Function LCQscanINFO Lib "icr2ls32.dll" (ByVal hLoadFile As Int32, ByVal Scan As Int32, ByRef Stype As Int32, ByRef Smz As Single, ByRef Pmz As Single) As Int32
	Private Declare Function LCQnumScans Lib "icr2ls32.dll" (ByVal hLoadFile As Int32) As Int32

	' Xcalibur (Typical) data file read routines...
	Private Declare Function isXcalibur Lib "icr2ls32.dll" (ByVal hLoadFile As Int32) As Int32
	Private Declare Function XcentroidNumPeaks Lib "icr2ls32.dll" (ByVal FileHandle As Int32, ByVal Scan As Int32) As Int32
	Private Declare Function XcentroidGetPeaks Lib "icr2ls32.dll" (ByVal FileHandle As Int32, ByVal Scan As Int32, ByRef X As Single, ByRef Y As Single) As Int32
	Private Declare Function XmzRange Lib "icr2ls32.dll" (ByVal hLoadFile As Int32, ByVal Scan As Int32, ByRef Min As Single, ByRef Max As Single) As Int32
	Private Declare Function Xlocate Lib "icr2ls32.dll" (ByVal FileHandle As Int32, ByVal ScanNum As Int32, ByRef StartMZ As Single, ByRef StopMZ As Single, ByRef NumPoints As Int32) As Int32
	Private Declare Function XscanINFO Lib "icr2ls32.dll" (ByVal hLoadFile As Int32, ByVal Scan As Int32, ByRef Stype As Int32, ByRef Smz As Single, ByRef Pmz As Single, ByRef RT As Single, ByRef Nl As Single) As Int32
	Private Declare Function XnumScans Lib "icr2ls32.dll" (ByVal hLoadFile As Int32) As Int32

	Private mDataFileStream As System.IO.FileStream
	Private mIsXcaliburFile As Boolean

	Public Overrides Function CheckFunctionality() As Boolean
		' Assume the Dll exists and is functional
		Return True
	End Function

	Public Overrides Sub CloseRawFile()
		Try
			If Not mDataFileStream Is Nothing Then
				mDataFileStream.Close()
			End If
		Catch ex As Exception
			' Ignore any errors
		Finally
			mDataFileStream = Nothing
			mCachedFileName = String.Empty
		End Try
	End Sub

	Protected Overrides Function FillFileInfo() As Boolean
		' Populates the mFileInfo structure
		' Function returns True if no error, False if an error
		' Most of the fields in mFileInfo are left blank, since the icr2ls32.dll has limited functionality for file info


		Dim objFile As System.IO.FileInfo

		Try
			If mDataFileStream.CanRead Then
				mIsXcaliburFile = IsXcaliburFile(mDataFileStream)

				With mFileInfo

					' Guess that the CreationDate is the modification date of the file

					Try
						objFile = New System.IO.FileInfo(mCachedFileName)
						If Not objFile Is Nothing Then
							.CreationDate = objFile.LastWriteTime
						End If
					Catch ex As Exception

					End Try

					.ScanStart = 1
					.ScanEnd = GetNumScans()
				End With
			Else
				Return False
			End If
		Catch ex As Exception
			Return False
		End Try

		Return True

	End Function

	Public Overrides Function GetNumScans() As Integer
		' Returns the number of scans, or -1 if an error

		Try
			If mDataFileStream.CanRead Then
				If mIsXcaliburFile Then
					Return XnumScans(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32)
				Else
					Return LCQnumScans(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32)
				End If
			Else
				Return -1
			End If
		Catch ex As Exception
			Return -1
		End Try

	End Function

	Public Overrides Function GetScanInfo(ByVal Scan As Integer, ByRef udtScanHeaderInfo As udtScanHeaderInfoType) As Boolean
		' Function returns True if no error, False if an error

		Dim intStatus As Integer
		Dim ScanType As Integer
		Dim sngBasePeakMZ As Single
		Dim sngParentIonMZ As Single
		Dim sngRT As Single
		Dim sngBasePeakIntensity As Single

		Dim intScanIndex As Integer

		Try
			If mDataFileStream.CanRead Then

				If Scan < mFileInfo.ScanStart Then
					Scan = mFileInfo.ScanStart
				ElseIf Scan > mFileInfo.ScanEnd Then
					Scan = mFileInfo.ScanEnd
				End If

				' Note: Icr-2LS numbers scans from 0 to ScanCount-1
				intScanIndex = Scan - 1

				If mIsXcaliburFile Then
					intStatus = XscanINFO(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, ScanType, sngBasePeakMZ, sngParentIonMZ, sngRT, sngBasePeakIntensity)
				Else
					intStatus = LCQscanINFO(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, ScanType, sngBasePeakMZ, sngParentIonMZ)
					sngRT = 0
					sngBasePeakIntensity = 0
				End If

				If intStatus = 0 Then
					' ScanType sometimes comes out as 65536, which should really be 0, or 65537, which should really be 1, etc.
					' Correct for this by And-ing ScanType with binary 111111, represented by 63 in Base-10
					' Note that ScanType will be -1 if not from a data directed analysis (MS then MS/MS)
					ScanType = ScanType And 63
					Debug.Assert(Math.Abs(ScanType) <= 63, "Scantype should be between 0 and 10; something is probably wrong (DirectFileIOViaICR2LS->GetScanInfo)")

					With udtScanHeaderInfo
						.EventNumber = ScanType + 1

						If .EventNumber <= 1 Then
							.MSLevel = 1
						Else
							.MSLevel = 2
						End If

						.BasePeakMZ = sngBasePeakMZ
						.BasePeakIntensity = sngBasePeakIntensity
						.ParentIonMZ = sngParentIonMZ
						.RetentionTime = sngRT

						' Set this to -1 for now; will get updated with call to .GetScanData()
						.NumPeaks = -1
					End With
				Else
					Return False
				End If

			Else
				Return False
			End If
		Catch ex As Exception
			Return False
		End Try

		Return True

	End Function

	Public Overloads Overrides Function GetScanData(ByVal Scan As Integer, ByRef dblMZList() As Double, ByRef dblIntensityList() As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType) As Integer
		' Return all data points by passing 0 for intMaxNumberOfPeaks
		Return GetScanData(Scan, dblMZList, dblIntensityList, udtScanHeaderInfo, 0)
	End Function

	Public Overloads Overrides Function GetScanData(ByVal Scan As Integer, ByRef dblMZList() As Double, ByRef dblIntensityList() As Double, ByRef udtScanHeaderInfo As udtScanHeaderInfoType, ByVal intMaxNumberOfPeaks As Integer) As Integer
		' Returns the number of data points, or -1 if an error
		' If intMaxNumberOfPeaks is <=0, then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned

		Dim intStatus As Integer
		Dim intScanIndex As Integer

		Dim sngMZList() As Single = Nothing
		Dim sngIntensityList() As Single = Nothing

		Try
			If mDataFileStream.CanRead Then

				If Scan < mFileInfo.ScanStart Then
					Scan = mFileInfo.ScanStart
				ElseIf Scan > mFileInfo.ScanEnd Then
					Scan = mFileInfo.ScanEnd
				End If

				' Note: Icr-2LS numbers scans from 0 to ScanCount-1
				intScanIndex = Scan - 1

				With udtScanHeaderInfo
					.IsCentroidScan = IsCentroid(Scan, udtScanHeaderInfo)

					If .IsCentroidScan Then
						.NumPeaks = XcentroidNumPeaks(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex)
						If .NumPeaks > 0 Then
							ReDim sngMZList(.NumPeaks - 1)
							ReDim sngIntensityList(.NumPeaks - 1)
							intStatus = XcentroidGetPeaks(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, sngMZList(0), sngIntensityList(0))
						End If
					Else
						.NumPeaks = LCQLoadRaw(mDataFileStream, intScanIndex, sngMZList, sngIntensityList)
					End If

					If .NumPeaks > 0 Then
						ReDim dblMZList(.NumPeaks - 1)
						ReDim dblIntensityList(.NumPeaks - 1)

						sngMZList.CopyTo(dblMZList, 0)
						sngIntensityList.CopyTo(dblIntensityList, 0)
					Else
						ReDim dblMZList(-1)
						ReDim dblIntensityList(-1)
					End If
				End With

			Else
				udtScanHeaderInfo.NumPeaks = -1
			End If

		Catch ex As Exception
			udtScanHeaderInfo.NumPeaks = -1
		End Try

		If udtScanHeaderInfo.NumPeaks < 0 Then
			udtScanHeaderInfo.NumPeaks = 0
			Return -1
		Else
			Return udtScanHeaderInfo.NumPeaks
		End If

	End Function

	Private Function IsCentroid(ByVal Scan As Integer, ByRef udtScanHeaderInfo As udtScanHeaderInfoType) As Boolean
		' Returns True if centroid (aka stick) data
		' Returns False if profile (aka continuum) data
		' Returns True if an error occurs or the file cannot be read
		'
		' Additionally, populates udtScanHeaderInfo.LowMass and udtScanHeaderInfo.HighMass

		Dim blnIsCentroid As Boolean

		Dim StartMZ As Single, StopMZ As Single
		Dim StartMZ2 As Single, StopMZ2 As Single
		Dim Pos As Integer, NumPoints As Integer

		Dim intScanIndex As Integer

		blnIsCentroid = True
		Try
			If mDataFileStream.CanRead Then
				If mIsXcaliburFile Then
					' Note: Icr-2LS numbers scans from 0 to ScanCount-1
					intScanIndex = Scan - 1

					Call Xlocate(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, StartMZ, StopMZ, NumPoints)
					Call XmzRange(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, StartMZ2, StopMZ2)
					If Math.Abs(StartMZ - StartMZ2) > 1.0 Or Math.Abs(StopMZ - StopMZ2) > 1.0 Then
						blnIsCentroid = True
					Else
						blnIsCentroid = False
					End If
					udtScanHeaderInfo.LowMass = StartMZ2
					udtScanHeaderInfo.HighMass = StopMZ2
				Else
					Pos = LCQlocate(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, 0, 0, 0, StartMZ, StopMZ, NumPoints)
					If (Pos < 0) Or (NumPoints <= 0) Then
						blnIsCentroid = True
					Else
						blnIsCentroid = False
					End If
					udtScanHeaderInfo.LowMass = StartMZ
					udtScanHeaderInfo.HighMass = StopMZ
				End If

			Else
				blnIsCentroid = True
				udtScanHeaderInfo.LowMass = 0
				udtScanHeaderInfo.HighMass = 0
			End If
		Catch ex As Exception
			blnIsCentroid = True
			udtScanHeaderInfo.LowMass = 0
			udtScanHeaderInfo.HighMass = 0
		End Try

		Return blnIsCentroid

	End Function

	Private Function IsXcaliburFile(ByVal mDataFileStream As System.IO.FileStream) As Boolean

		If isXcalibur(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32) <> 0 Then
			IsXcaliburFile = True
		Else
			IsXcaliburFile = False
		End If

	End Function

	Public Overrides Function OpenRawFile(ByVal FileName As String) As Boolean
		' Returns True if success, false if failure
		Dim blnSuccess As Boolean

		Try

			' Make sure any existing open files are closed
			CloseRawFile()

			mDataFileStream = System.IO.File.Open(FileName, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
			If Not mDataFileStream Is Nothing AndAlso mDataFileStream.CanRead Then
				mCachedFileName = FileName
				FillFileInfo()
				blnSuccess = True
			Else
				blnSuccess = False
			End If
		Catch ex As Exception
			blnSuccess = False
		Finally
			If Not blnSuccess Then
				mCachedFileName = String.Empty
			End If
		End Try

		Return blnSuccess
	End Function

	Private Function LCQLoadRaw(ByVal mDataFileStream As System.IO.FileStream, ByVal intScanIndex As Integer, ByRef sngMZList() As Single, ByRef sngIntensityList() As Single) As Integer
		' Returns the number of data points read into the sngMZList() and sngIntensityList() arrays
		' Returns 0 if no points read or -1 if an error

		Const BIT_MASK As Integer = 2147483647			' 2^31-1

		Dim intNumDataPoints As Integer
		Dim sngMZMin, sngMZMax, sngMZRange As Single

		Dim ProfileFileOffset As Integer

		Dim intStatus As Integer
		Dim intIndex As Integer

		Dim objBinaryReader As System.IO.BinaryReader

		Try
			If mIsXcaliburFile Then
				intStatus = XmzRange(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, sngMZMin, sngMZMax)
				If (intScanIndex < 0) Then
					ProfileFileOffset = Xlocate(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, 0, sngMZMin, sngMZMax, intNumDataPoints)
				Else
					ProfileFileOffset = Xlocate(mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, intScanIndex, sngMZMin, sngMZMax, intNumDataPoints)
				End If
			Else
				' We're not supporting Non-Xcalibur (aka Navigator) file types
				ProfileFileOffset = 0
			End If

			If ProfileFileOffset <= 0 Or intNumDataPoints <= 0 Then
				intNumDataPoints = -1
			Else
				ReDim sngMZList(intNumDataPoints - 1)
				ReDim sngIntensityList(intNumDataPoints - 1)

				' The following would be the method to call LCQLoad from icr2ls32.dll
				'intStatus = LCQload(sngIntensityList(0), ProfileNumPointsInScan, mDataFileStream.SafeFileHandle.DangerousGetHandle.ToInt32, ProfileFileOffset)

				' The sngMZList array ranges from sngMZMin to sngMZMax, evenly spaced
				sngMZRange = sngMZMax - sngMZMin

				' The intensity data is stored as 32 bit integers
				' We can read each integer in using the BinaryReader class
				' Each number must be And'd with 2147483647 to extract out the single precision intensity

				mDataFileStream.Seek(ProfileFileOffset, IO.SeekOrigin.Begin)
				objBinaryReader = New System.IO.BinaryReader(mDataFileStream)

				If intNumDataPoints = 1 Then
					sngMZList(0) = sngMZMin
					sngIntensityList(0) = objBinaryReader.ReadInt32() And BIT_MASK
				Else
					For intIndex = 0 To intNumDataPoints - 1
						sngMZList(intIndex) = sngMZMin + sngMZRange * intIndex / (intNumDataPoints - 1)
						sngIntensityList(intIndex) = objBinaryReader.ReadInt32() And BIT_MASK
					Next intIndex
				End If

			End If

		Catch ex As Exception
			intNumDataPoints = -1
		End Try

		Return intNumDataPoints

	End Function
End Class
