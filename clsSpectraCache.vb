Option Strict On

Public Class clsSpectraCache

    ' Utilizes a spectrum pool to store mass spectra

    ' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    ' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
    ' Started March 20, 2005
    '
    ' Last modified March 21, 2005

    Public Sub New()
        InitializeVariables()
    End Sub

    Protected Overrides Sub Finalize()
        ClosePageFile()
        DeleteSpectrumCacheFiles()
        MyBase.Finalize()
    End Sub

#Region "Constants and Enums"
    Private Const SPECTRUM_CACHE_FILE_PREFIX As String = "$SpecCache"
    Private Const SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR As String = "_Temp"

    Private Const SPECTRUM_CACHE_MAX_FILE_AGE_HOURS As Integer = 12
    Private Const DEFAULT_SPECTRUM_ION_COUNT As Integer = 500

    Private Enum eCacheStateConstants
        UnusedSlot = 0              ' No data present
        NeverCached = 1             ' In memory, but never cached
        LoadedFromCache = 2         ' Loaded from cache, and in memory; or, loaded using XRaw; safe to purge without caching
    End Enum

    ''Private Enum eCacheRequestStateConstants
    ''    NoRequest = 0               ' Undefined
    ''    SafeToCache = 1             ' In memory, but safe to cache or purge
    ''    RequestUncache = 2          ' Not in memory, need to uncache
    ''    RequestUncacheAndLock = 3   ' Not in memory, need to uncache and lock in memory
    ''    LockedInMemory = 4          ' In memory and should not cache
    ''End Enum

    ''Private Enum eCacheCommandConstants
    ''    CacheAllSpectraOutOfRange = 0
    ''    CacheSurveyScansOutOfRange = 1
    ''    CacheFragScansOutOfRange = 2
    ''    CacheAllSpectraOutOfRangeDoNotUncache = 3
    ''    ValidateSurveyScanUncached = 4
    ''    ValidateFragScanUncached = 5
    ''    UnlockAllSpectra = 6
    ''End Enum

#End Region

#Region "Structures"
    ''Public Structure udtMSSpectrumType
    ''    Public ScanNumber As Integer                        ' 0 if not in use
    ''    Public IonCount As Integer
    ''    Public IonsMZ() As Double                           ' 0-based array, ranging from 0 to IonCount-1; note that IonsMZ.Length could be > IonCount, so do not use .Length to determine the data count
    ''    Public IonsIntensity() As Single                    ' 0-based array, ranging from 0 to IonCount-1; note that IonsIntensity.Length could be > IonCount, so do not use .Length to determine the data count
    ''End Structure

    Public Structure udtSpectrumCacheOptionsType
        Public DiskCachingAlwaysDisabled As Boolean             ' If True, then spectra will never be cached to disk and the spectra pool will consequently be increased as needed
		Public FolderPath As String								' Path to the cache folder (can be relative or absolute, aka rooted); if empty, then the user's AppData folder is used
        Public SpectraToRetainInMemory As Integer
        Public MinimumFreeMemoryMB As Single                    ' Legacy parameter; no longer used
        Public MaximumMemoryUsageMB As Single                   ' Legacy parameter; no longer used
    End Structure

    Private Structure udtSpectraPoolInfoType
        Public CacheState As eCacheStateConstants
        'Public LockInMemory As Boolean
    End Structure

#End Region

#Region "Classwide Variables"
    Public SpectraPool() As clsMSSpectrum                   ' Pool (collection) of currently loaded spectra; 0-based array
    Private SpectraPoolInfo() As udtSpectraPoolInfoType     ' Parallel with SpectraPool(), but not publicly visible

    Private mCacheOptions As udtSpectrumCacheOptionsType

    Private mPageFileReader As System.IO.BinaryReader
    Private mPageFileWriter As System.IO.BinaryWriter

    Private mFolderPathValidated As Boolean
    Private mCacheFileNameBase As String                    ' Base filename for this instance of clsMasic, includes a timestamp to allow multiple instances to write to the same cache folder
    Private mCacheEventCount As Integer
    Private mUnCacheEventCount As Integer

    Private mMaximumPoolLength As Integer
    Private mNextAvailablePoolIndex As Integer

    ''Private mAccessIterator As System.Int64

    Private mSpectrumIndexInPool As System.Collections.Hashtable
    ''Private mPoolAccessHistory As System.Collections.Hashtable        ' Assigns a unique long integer (access iterator) to each scan present in SpectraPool(); look for the item with the smallest access iterator value to determine the best candidate for purging (though LockInMemory cannot be true)
    Private mSpectrumByteOffset As System.Collections.Hashtable         ' Records the byte offset of the data in the page file for a given scan number

    Private mShowMessages As Boolean

#End Region

    Public ReadOnly Property CacheEventCount() As Integer
        Get
            Return mCacheEventCount
        End Get
    End Property

    Public ReadOnly Property CacheFileNameBase() As String
        Get
            Return mCacheFileNameBase
        End Get
    End Property

    Public Property CacheFolderPath() As String
        Get
            Return mCacheOptions.FolderPath
        End Get
        Set(ByVal Value As String)
            mCacheOptions.FolderPath = Value
        End Set
    End Property

    Public Property CacheMaximumMemoryUsageMB() As Single
        Get
            Return mCacheOptions.MaximumMemoryUsageMB
        End Get
        Set(ByVal Value As Single)
            mCacheOptions.MaximumMemoryUsageMB = Value
        End Set
    End Property

    Public Property CacheMinimumFreeMemoryMB() As Single
        Get
            Return mCacheOptions.MinimumFreeMemoryMB
        End Get
        Set(ByVal Value As Single)
            If mCacheOptions.MinimumFreeMemoryMB < 10 Then
                mCacheOptions.MinimumFreeMemoryMB = 10
            End If
            mCacheOptions.MinimumFreeMemoryMB = Value
        End Set
    End Property

    Public Property CacheSpectraToRetainInMemory() As Integer
        Get
            Return mCacheOptions.SpectraToRetainInMemory
        End Get
        Set(ByVal Value As Integer)
            If Value < 100 Then Value = 100
            mCacheOptions.SpectraToRetainInMemory = Value
        End Set
    End Property

    Public Property DiskCachingAlwaysDisabled() As Boolean
        Get
            Return mCacheOptions.DiskCachingAlwaysDisabled
        End Get
        Set(ByVal Value As Boolean)
            mCacheOptions.DiskCachingAlwaysDisabled = Value
        End Set
    End Property

    Public Property ShowMessages() As Boolean
        Get
            Return mShowMessages
        End Get
        Set(ByVal Value As Boolean)
            mShowMessages = Value
        End Set
    End Property

    Public ReadOnly Property UnCacheEventCount() As Integer
        Get
            Return mUnCacheEventCount
        End Get
    End Property

    Public Function AddSpectrumToPool(ByRef objMSSpectrum As clsMSSpectrum, ByVal intScanNumber As Integer, ByRef intTargetPoolIndex As Integer) As Boolean
        ' Adds objMSSpectrum to the spectrum pool
        ' Returns the index of the spectrum in the pool in intTargetPoolIndex

        Dim blnSuccess As Boolean

        Try
            If mSpectrumIndexInPool.Contains(intScanNumber) Then
                ' Replace the spectrum data with objMSSpectrum
                intTargetPoolIndex = CInt(mSpectrumIndexInPool(intScanNumber))
            Else
                ' Need to add the spectrum
                intTargetPoolIndex = GetNextAvailablePoolIndex()
                mSpectrumIndexInPool.Add(intScanNumber, intTargetPoolIndex)
            End If

            ValidateMemoryAllocation(SpectraPool(intTargetPoolIndex), objMSSpectrum.IonCount)

            With SpectraPool(intTargetPoolIndex)
                .ScanNumber = objMSSpectrum.ScanNumber
                If .ScanNumber <> intScanNumber Then
                    .ScanNumber = intScanNumber
                End If

                .IonCount = objMSSpectrum.IonCount
            End With

            Array.Copy(objMSSpectrum.IonsMZ, SpectraPool(intTargetPoolIndex).IonsMZ, objMSSpectrum.IonCount)
            Array.Copy(objMSSpectrum.IonsIntensity, SpectraPool(intTargetPoolIndex).IonsIntensity, objMSSpectrum.IonCount)

            SpectraPoolInfo(intTargetPoolIndex).CacheState = eCacheStateConstants.NeverCached

            blnSuccess = True
        Catch ex As Exception
            LogErrors("AddSpectrumToPool", ex.Message, ex, True, True)
        End Try

        Return blnSuccess
    End Function

    Private Function CacheSpectrum(ByVal intPoolIndexToCache As Integer) As Boolean
        ' Returns True if already cached or if successfully cached
        ' Returns False if an error

        Dim blnSuccess As Boolean

        Dim intIndex As Integer
        Dim intScanNumber As Integer

        If SpectraPoolInfo(intPoolIndexToCache).CacheState = eCacheStateConstants.UnusedSlot Then
            ' Nothing to do; slot is already empty
            blnSuccess = True
        Else
            If SpectraPoolInfo(intPoolIndexToCache).CacheState = eCacheStateConstants.LoadedFromCache Then
                ' Already cached previously, simply reset the slot
            Else

                ' Store all of the spectra in one large file

                If ValidatePageFileIO(True) Then
                    ' See if the given spectrum is already present in the page file
                    intScanNumber = SpectraPool(intPoolIndexToCache).ScanNumber
                    If mSpectrumByteOffset.Contains(intScanNumber) Then
                        ' Page file already contains the given scan; do not re-write
                    Else
                        ' Write the spectrum to the page file
                        ' Record the current offset in the hashtable
                        mSpectrumByteOffset.Add(intScanNumber, mPageFileWriter.BaseStream.Position)

                        With SpectraPool(intPoolIndexToCache)
                            ' Write the scan number
                            mPageFileWriter.Write(intScanNumber)

                            ' Write the ion count
                            mPageFileWriter.Write(.IonCount)

                            ' Write the m/z values
                            For intIndex = 0 To .IonCount - 1
                                mPageFileWriter.Write(.IonsMZ(intIndex))
                            Next intIndex

                            ' Write the intensity values
                            For intIndex = 0 To .IonCount - 1
                                mPageFileWriter.Write(.IonsIntensity(intIndex))
                            Next intIndex
                        End With

                        ' Write four blank bytes (not really necessary, but adds a little padding between spectra)
                        mPageFileWriter.Write(CType(0, Int32))

                    End If
                End If

            End If

            ' Remove intScanNumber from mSpectrumIndexInPool
            mSpectrumIndexInPool.Remove(SpectraPool(intPoolIndexToCache).ScanNumber)

            ' Reset .ScanNumber, .IonCount, and .CacheState
            With SpectraPool(intPoolIndexToCache)
                .ScanNumber = 0
                .IonCount = 0
            End With

            SpectraPoolInfo(intPoolIndexToCache).CacheState = eCacheStateConstants.UnusedSlot

            mCacheEventCount += 1
            blnSuccess = True

        End If

        Return blnSuccess

    End Function

    Public Sub ClosePageFile()

        Dim blnGarbageCollect As Boolean

        Try
            If Not mPageFileReader Is Nothing Then
                mPageFileReader.Close()
                mPageFileReader = Nothing
                blnGarbageCollect = True
            End If

            If Not mPageFileWriter Is Nothing Then
                mPageFileWriter.Close()
                mPageFileWriter = Nothing
                blnGarbageCollect = True
            End If

            If blnGarbageCollect Then
                GC.Collect()
                GC.WaitForPendingFinalizers()
                System.Threading.Thread.Sleep(500)
            End If

        Catch ex As Exception
            ' Ignore errors here
        End Try

        If mSpectrumByteOffset Is Nothing Then
            mSpectrumByteOffset = New System.Collections.Hashtable
        Else
            mSpectrumByteOffset.Clear()
        End If

    End Sub

    Private Function ConstructCachedSpectrumPath() As String
        ' Constructs the full path for the given spectrum file
        ' Returns String.empty if unable to validate the cached spectrum folder

        Dim strFileName As String

        If Not ValidateCachedSpectrumFolder() Then
            Return String.Empty
        End If

		If String.IsNullOrWhiteSpace(mCacheFileNameBase) Then
			Dim objRand As New Random()

			' Create the cache file name, using both a timestamp and a random number between 1 and 9999
			mCacheFileNameBase = SPECTRUM_CACHE_FILE_PREFIX & System.DateTime.UtcNow.Hour.ToString & System.DateTime.UtcNow.Minute.ToString & System.DateTime.UtcNow.Second.ToString & System.DateTime.UtcNow.Millisecond.ToString & objRand.Next(1, 9999).ToString
		End If

        strFileName = mCacheFileNameBase & SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR & ".bin"

        Return System.IO.Path.Combine(mCacheOptions.FolderPath, strFileName)

    End Function

    Public Sub DeleteSpectrumCacheFiles()
        ' Looks for and deletes the spectrum cache files created by this instance of MASIC
        ' Additionally, looks for and deletes spectrum cache files with modification dates more than SPECTRUM_CACHE_MAX_FILE_AGE_HOURS from the present

        Dim dtFileDateTolerance As DateTime
        Dim strFilePathMatch As String
        Dim intCharIndex As Integer
        Dim intIndex As Integer

        Dim strFiles() As String

        Dim objFolder As System.IO.DirectoryInfo
        Dim objFile As System.IO.FileInfo

        dtFileDateTolerance = System.DateTime.UtcNow.Subtract(New System.TimeSpan(SPECTRUM_CACHE_MAX_FILE_AGE_HOURS, 0, 0))

        Try
            ' Delete the cached files for this instance of clsMasic
            strFilePathMatch = ConstructCachedSpectrumPath()

            intCharIndex = strFilePathMatch.IndexOf(SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR)
            If intCharIndex < 0 Then
                Debug.Assert(False, "intCharIndex was less than 0; this is unexpected in DeleteSpectrumCacheFiles")
                Return
            End If

            strFilePathMatch = strFilePathMatch.Substring(0, intCharIndex)
            strFiles = System.IO.Directory.GetFiles(mCacheOptions.FolderPath, System.IO.Path.GetFileName(strFilePathMatch) & "*")

            For intIndex = 0 To strFiles.Length - 1
                System.IO.File.Delete(strFiles(intIndex))
            Next intIndex

        Catch ex As Exception
            ' Ignore errors here
            LogErrors("DeleteSpectrumCacheFiles", "Error deleting cached spectrum files for this task", ex, True, False, True)
        End Try

        ' Now look for old spectrum cache files
        Try
            strFilePathMatch = SPECTRUM_CACHE_FILE_PREFIX & "*" & SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR & "*"

            objFolder = New System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(ConstructCachedSpectrumPath())))

            If Not objFolder Is Nothing Then
                For Each objFile In objFolder.GetFiles(strFilePathMatch)
                    If objFile.LastWriteTimeUtc < dtFileDateTolerance Then
                        objFile.Delete()
                    End If
                Next objFile
            End If
        Catch ex As Exception
            LogErrors("DeleteSpectrumCacheFiles", "Error deleting old cached spectrum files", ex, True, False, True)
        End Try

    End Sub

    Private Sub ExpandSpectraPool(ByVal intNewPoolLength As Integer)
        Dim intCurrentPoolLength As Integer
        Dim intIndex As Integer

        intCurrentPoolLength = Math.Min(mMaximumPoolLength, SpectraPool.Length)
        mMaximumPoolLength = intNewPoolLength

        If intNewPoolLength > intCurrentPoolLength Then
            ReDim Preserve SpectraPool(mMaximumPoolLength - 1)
            ReDim Preserve SpectraPoolInfo(mMaximumPoolLength - 1)
            
            For intIndex = intCurrentPoolLength To mMaximumPoolLength - 1
                SpectraPool(intIndex) = New clsMSSpectrum
                SpectraPoolInfo(intIndex).CacheState = eCacheStateConstants.UnusedSlot
            Next intIndex
        End If

    End Sub

    Private Function GetNextAvailablePoolIndex() As Integer
        Dim intNextPoolIndex As Integer

        ' Need to cache the spectrum stored at mNextAvailablePoolIndex
        CacheSpectrum(mNextAvailablePoolIndex)

        intNextPoolIndex = mNextAvailablePoolIndex

        mNextAvailablePoolIndex += 1
        If mNextAvailablePoolIndex >= mMaximumPoolLength Then
            If mCacheOptions.DiskCachingAlwaysDisabled Then
                ' The pool is full, but disk caching is disabled, so we have to expand the pool
                ExpandSpectraPool(mMaximumPoolLength + 500)
            Else
                mNextAvailablePoolIndex = 0

                GC.Collect()
                GC.WaitForPendingFinalizers()
                System.Threading.Thread.Sleep(50)
            End If
        End If

        Return intNextPoolIndex

    End Function

    Public Sub InitializeSpectraPool()
        Dim intIndex As Integer

        mMaximumPoolLength = mCacheOptions.SpectraToRetainInMemory
        If mMaximumPoolLength < 1 Then mMaximumPoolLength = 1

        mNextAvailablePoolIndex = 0

        mCacheEventCount = 0
        mUnCacheEventCount = 0

        mFolderPathValidated = False
        mCacheFileNameBase = String.Empty

        ClosePageFile()

        If mSpectrumIndexInPool Is Nothing Then
            mSpectrumIndexInPool = New System.Collections.Hashtable
        Else
            mSpectrumIndexInPool.Clear()
        End If

        ''If mPoolAccessHistory Is Nothing Then
        ''    mPoolAccessHistory = New System.Collections.Hashtable
        ''Else
        ''    mPoolAccessHistory.Clear()
        ''End If

        If SpectraPool Is Nothing Then
            ReDim SpectraPool(mMaximumPoolLength - 1)
            ReDim SpectraPoolInfo(mMaximumPoolLength - 1)
        Else
            If SpectraPool.Length < mMaximumPoolLength Then
                ReDim SpectraPool(mMaximumPoolLength - 1)
                ReDim SpectraPoolInfo(mMaximumPoolLength - 1)
            End If
        End If

        ' Note: Resetting spectra all the way to SpectraPool.Length, even if SpectraPool.Length is > mMaximumPoolLength
        For intIndex = 0 To SpectraPool.Length - 1
            SpectraPool(intIndex) = New clsMSSpectrum
            SpectraPoolInfo(intIndex).CacheState = eCacheStateConstants.UnusedSlot
        Next intIndex

    End Sub

    Private Sub InitializeVariables()
        ResetCacheOptions(mCacheOptions)

        InitializeSpectraPool()
    End Sub

    ' Loads spectral data into a spectrum pool entry

    Private Sub LogErrors(ByVal strSource As String, ByVal strMessage As String, ByVal ex As Exception, Optional ByVal blnAllowInformUser As Boolean = True, Optional ByVal blnAllowThrowingException As Boolean = True, Optional ByVal blnLogLocalOnly As Boolean = True)
        Dim strMessageWithoutCRLF As String

        strMessageWithoutCRLF = strMessage.Replace(ControlChars.NewLine, "; ")

        If ex Is Nothing Then
            ex = New System.Exception("Error")
        Else
            If Not ex.Message Is Nothing AndAlso ex.Message.Length > 0 Then
                strMessageWithoutCRLF &= "; " & ex.Message
            End If
        End If

        Trace.WriteLine(System.DateTime.Now.ToLongTimeString & "; " & strMessageWithoutCRLF, strSource)
        Console.WriteLine(System.DateTime.Now.ToLongTimeString & "; " & strMessageWithoutCRLF, strSource)

        If mShowMessages AndAlso blnAllowInformUser Then
            Windows.Forms.MessageBox.Show(strMessage & ControlChars.NewLine & ex.Message, "Error", Windows.Forms.MessageBoxButtons.OK, Windows.Forms.MessageBoxIcon.Exclamation)
        ElseIf blnAllowThrowingException Then
            Throw New System.Exception(strMessage, ex)
        End If
    End Sub

    Public Shared Sub ResetCacheOptions(ByRef udtCacheOptions As udtSpectrumCacheOptionsType)
        With udtCacheOptions
            .DiskCachingAlwaysDisabled = False
			.FolderPath = System.IO.Path.GetTempPath()
            .SpectraToRetainInMemory = 5000
            .MinimumFreeMemoryMB = 250
            .MaximumMemoryUsageMB = 3000             ' Spectrum caching to disk will be enabled if the memory usage rises over this value
        End With
    End Sub

    Private Function UnCacheSpectrum(ByVal intScanNumber As Integer, ByRef intTargetPoolIndex As Integer) As Boolean
        ' Returns True if successfully uncached
        ' Returns False if an error
        ' intTargetPoolIndex will contain the index of the spectrum in the Spectrum Pool 

        Dim blnSuccess As Boolean
        Dim blnReturnBlankSpectrum As Boolean

        Dim intIndex As Integer
        Dim intIonCount As Integer
        Dim intScanNumberInCacheFile As Integer

        Dim lngByteOffset As System.Int64

        intTargetPoolIndex = GetNextAvailablePoolIndex()

        ' Uncache the spectrum from disk
        blnReturnBlankSpectrum = False

        ' All of the spectra are stored in one large file
        If ValidatePageFileIO(False) Then
            ' Lookup the byte offset for the given spectrum 

            If mSpectrumByteOffset.Contains(intScanNumber) Then
                lngByteOffset = CType(mSpectrumByteOffset.Item(intScanNumber), Long)

                ' Make sure all previous spectra are flushed to disk
                mPageFileWriter.Flush()

                ' Read the spectrum from the page file
                mPageFileReader.BaseStream.Seek(lngByteOffset, IO.SeekOrigin.Begin)

                intScanNumberInCacheFile = mPageFileReader.ReadInt32()
                intIonCount = mPageFileReader.ReadInt32()
                ValidateMemoryAllocation(SpectraPool(intTargetPoolIndex), intIonCount)

                With SpectraPool(intTargetPoolIndex)
                    .ScanNumber = intScanNumber

                    Debug.Assert(intScanNumberInCacheFile = .ScanNumber, "Error: scan number in cache file doesn't agree with expected scan number in UnCacheSpectrum")

                    .IonCount = intIonCount
                    For intIndex = 0 To .IonCount - 1
                        .IonsMZ(intIndex) = mPageFileReader.ReadDouble()
                    Next intIndex

                    For intIndex = 0 To .IonCount - 1
                        .IonsIntensity(intIndex) = mPageFileReader.ReadSingle()
                    Next intIndex

                End With

                blnSuccess = True

            Else
                blnReturnBlankSpectrum = True
            End If
        Else
            blnReturnBlankSpectrum = True
        End If

        If blnReturnBlankSpectrum Then
            ' Scan not found; create a new, blank mass spectrum
            ' Its cache state will be set to LoadedFromCache, which is ok, since we don't need to cache it to disk
            If SpectraPool(intTargetPoolIndex) Is Nothing Then
                SpectraPool(intTargetPoolIndex) = New clsMSSpectrum
            End If

            With SpectraPool(intTargetPoolIndex)
                .ScanNumber = intScanNumber
                .IonCount = 0
            End With
            blnSuccess = True
        End If

        If blnSuccess Then
            SpectraPoolInfo(intTargetPoolIndex).CacheState = eCacheStateConstants.LoadedFromCache

            If mSpectrumIndexInPool.Contains(intScanNumber) Then
                mSpectrumIndexInPool.Item(intScanNumber) = intTargetPoolIndex
            Else
                mSpectrumIndexInPool.Add(intScanNumber, intTargetPoolIndex)
            End If

            If Not blnReturnBlankSpectrum Then mUnCacheEventCount += 1
        End If

        Return blnSuccess

    End Function

    ''Private Function UpdatePoolAccessHistory(ByVal intScanNumber As Integer) As Boolean
    ''    ' Returns True if the scan is present in mPoolAccessHistory, otherwise, returns false

    ''    If mPoolAccessHistory.Contains(intScanNumber) Then
    ''        mAccessIterator += 1
    ''        mPoolAccessHistory.Item(intScanNumber) = mAccessIterator
    ''        Return True
    ''    Else
    ''        Return False
    ''    End If
    ''End Function

    Private Function ValidateCachedSpectrumFolder() As Boolean

		If String.IsNullOrWhiteSpace(mCacheOptions.FolderPath) Then
			' Need to define the spectrum caching folder path
			mCacheOptions.FolderPath = System.IO.Path.GetTempPath()
			mFolderPathValidated = False
		End If

        If Not mFolderPathValidated Then
            Try
                If Not System.IO.Path.IsPathRooted(mCacheOptions.FolderPath) Then
					mCacheOptions.FolderPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), mCacheOptions.FolderPath)
                End If

                If Not System.IO.Directory.Exists(mCacheOptions.FolderPath) Then
                    System.IO.Directory.CreateDirectory(mCacheOptions.FolderPath)

                    If Not System.IO.Directory.Exists(mCacheOptions.FolderPath) Then
                        LogErrors("ValidateCachedSpectrumFolder", "Error creating spectrum cache folder: " & mCacheOptions.FolderPath, Nothing, True, False, False)
                        Return False
                    End If
                End If

                mFolderPathValidated = True
                Return True

            Catch ex As Exception
                ' Error defining .FolderPath
                LogErrors("ValidateCachedSpectrumFolder", "Error creating spectrum cache folder", ex, True, False, False)
                Return False
            End Try
        Else
            Return True
        End If

    End Function

    Private Sub ValidateMemoryAllocation(ByRef objMSSpectrum As clsMSSpectrum, ByVal intIonCount As Integer)

        Dim intAllocationAmount As Integer

        With objMSSpectrum
            If .IonsMZ.Length < intIonCount Then
                ' Find the next largest multiple of DEFAULT_SPECTRUM_ION_COUNT that is larger than intIonCount

                intAllocationAmount = .IonsMZ.Length
                Do While intAllocationAmount < intIonCount
                    intAllocationAmount += DEFAULT_SPECTRUM_ION_COUNT
                Loop

                ReDim .IonsMZ(intAllocationAmount - 1)
                ReDim .IonsIntensity(intAllocationAmount - 1)
            End If
        End With
    End Sub

    Private Function ValidatePageFileIO(ByVal blnCreateIfUninitialized As Boolean) As Boolean
        ' Validates that we can read and write from a Page file
        ' Opens the page file reader and writer if not yet opened

        Dim blnValid As Boolean

        If mPageFileReader Is Nothing Then
            If blnCreateIfUninitialized Then
                Dim strCacheFilePath As String
                Dim fsWrite As System.IO.FileStream
                Dim fsRead As System.io.FileStream

                Dim intIndex As Integer

                Try
                    ' Construct the page file path
                    strCacheFilePath = ConstructCachedSpectrumPath()

                    ' Initialize the binary writer and create the file
                    fsWrite = New System.IO.FileStream(strCacheFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read)
                    mPageFileWriter = New System.IO.BinaryWriter(fsWrite)

                    ' Write a header line
                    mPageFileWriter.Write("MASIC Spectrum Cache Page File.  Created " & System.DateTime.Now.ToLongDateString & " " & System.DateTime.Now.ToLongTimeString)

                    ' Add 64 bytes of white space
                    For intIndex = 0 To 63
                        mPageFileWriter.Write(Byte.MinValue)
                    Next intIndex
                    mPageFileWriter.Flush()

                    ' Initialize the binary reader
                    fsRead = New System.IO.FileStream(strCacheFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                    mPageFileReader = New System.IO.BinaryReader(fsRead)

                    blnValid = True
                Catch ex As Exception
                    LogErrors("ValidatePageFileIO", ex.Message, ex, True, True, True)
                    blnValid = False
                End Try
            Else
                blnValid = False
            End If
        Else
            blnValid = True
        End If

        Return blnValid

    End Function

    Public Function ValidateSpectrumInPool(ByVal intScanNumber As Integer, ByRef intPoolIndex As Integer) As Boolean

        ' Make sure the spectrum given by intScanNumber is present in FragScanSpectra
        ' When doing this, update the Pool Access History with this scan number to assure it doesn't get purged from the pool anytime soon
        ' Update intPoolIndex to hold the index in the array that contains the given spectrum

        Dim blnSuccess As Boolean

        Try
            If mSpectrumIndexInPool.Contains(intScanNumber) Then
                intPoolIndex = CInt(mSpectrumIndexInPool(intScanNumber))
                blnSuccess = True
            Else
                ' Need to load the spectrum
                blnSuccess = UnCacheSpectrum(intScanNumber, intPoolIndex)
            End If
        Catch ex As Exception
            LogErrors("ValidateSpectrumInPool", ex.Message, ex, True, True)
            blnSuccess = False
        End Try

        Return blnSuccess

    End Function

End Class
