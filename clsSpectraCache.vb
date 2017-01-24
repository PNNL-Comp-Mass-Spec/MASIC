Option Strict On

Imports System.Runtime.InteropServices

''' <summary>
''' Utilizes a spectrum pool to store mass spectra
''' </summary>
Public Class clsSpectraCache
    Inherits clsEventNotifier

    Public Sub New(cacheOptions As clsSpectrumCacheOptions)
        mCacheOptions = cacheOptions
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

    Private Structure udtSpectraPoolInfoType
        Public CacheState As eCacheStateConstants
        'Public LockInMemory As Boolean
    End Structure

#End Region

#Region "Classwide Variables"
    Public SpectraPool() As clsMSSpectrum                   ' Pool (collection) of currently loaded spectra; 0-based array
    Private SpectraPoolInfo() As udtSpectraPoolInfoType     ' Parallel with SpectraPool(), but not publicly visible

    Private ReadOnly mCacheOptions As clsSpectrumCacheOptions

    Private mPageFileReader As BinaryReader
    Private mPageFileWriter As BinaryWriter

    Private mFolderPathValidated As Boolean

    ' Base filename for this instance of clsMasic, includes a timestamp to allow multiple instances to write to the same cache folder
    Private mCacheFileNameBase As String

    Private mCacheEventCount As Integer
    Private mUnCacheEventCount As Integer

    Private mMaximumPoolLength As Integer
    Private mNextAvailablePoolIndex As Integer

    Private mSpectrumIndexInPool As Hashtable
    Private mSpectrumByteOffset As Hashtable         ' Records the byte offset of the data in the page file for a given scan number

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
        Set(Value As String)
            mCacheOptions.FolderPath = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheMaximumMemoryUsageMB() As Single
        Get
            Return mCacheOptions.MaximumMemoryUsageMB
        End Get
        Set(Value As Single)
            mCacheOptions.MaximumMemoryUsageMB = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheMinimumFreeMemoryMB() As Single
        Get
            Return mCacheOptions.MinimumFreeMemoryMB
        End Get
        Set(Value As Single)
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
        Set(Value As Integer)
            If Value < 100 Then Value = 100
            mCacheOptions.SpectraToRetainInMemory = Value
        End Set
    End Property

    Public Property DiskCachingAlwaysDisabled() As Boolean
        Get
            Return mCacheOptions.DiskCachingAlwaysDisabled
        End Get
        Set(Value As Boolean)
            mCacheOptions.DiskCachingAlwaysDisabled = Value
        End Set
    End Property

    Public Property ShowMessages As Boolean

    Public ReadOnly Property UnCacheEventCount() As Integer
        Get
            Return mUnCacheEventCount
        End Get
    End Property

    Public Function AddSpectrumToPool(
       objMSSpectrum As clsMSSpectrum,
       intScanNumber As Integer,
       ByRef intTargetPoolIndex As Integer) As Boolean

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
            ReportError("AddSpectrumToPool", ex.Message, ex, True, True)
        End Try

        Return blnSuccess
    End Function

    Private Sub CacheSpectrum(intPoolIndexToCache As Integer)
        ' Returns True if already cached or if successfully cached
        ' Returns False if an error

        Dim intIndex As Integer
        Dim intScanNumber As Integer

        If SpectraPoolInfo(intPoolIndexToCache).CacheState = eCacheStateConstants.UnusedSlot Then
            ' Nothing to do; slot is already empty
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
                        mPageFileWriter.Write(0)

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

        End If

        Return
    End Sub

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
                Threading.Thread.Sleep(500)
            End If

        Catch ex As Exception
            ' Ignore errors here
        End Try

        If mSpectrumByteOffset Is Nothing Then
            mSpectrumByteOffset = New Hashtable
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
            mCacheFileNameBase = SPECTRUM_CACHE_FILE_PREFIX & DateTime.UtcNow.Hour & DateTime.UtcNow.Minute & DateTime.UtcNow.Second & DateTime.UtcNow.Millisecond & objRand.Next(1, 9999)
        End If

        strFileName = mCacheFileNameBase & SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR & ".bin"

        Return Path.Combine(mCacheOptions.FolderPath, strFileName)

    End Function

    Public Sub DeleteSpectrumCacheFiles()
        ' Looks for and deletes the spectrum cache files created by this instance of MASIC
        ' Additionally, looks for and deletes spectrum cache files with modification dates more than SPECTRUM_CACHE_MAX_FILE_AGE_HOURS from the present

        Dim dtFileDateTolerance As DateTime
        Dim strFilePathMatch As String
        Dim intCharIndex As Integer
        Dim intIndex As Integer

        Dim strFiles() As String

        Dim objFolder As DirectoryInfo
        Dim objFile As FileInfo

        dtFileDateTolerance = DateTime.UtcNow.Subtract(New TimeSpan(SPECTRUM_CACHE_MAX_FILE_AGE_HOURS, 0, 0))

        Try
            ' Delete the cached files for this instance of clsMasic
            strFilePathMatch = ConstructCachedSpectrumPath()

            intCharIndex = strFilePathMatch.IndexOf(SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR, StringComparison.Ordinal)
            If intCharIndex < 0 Then
                ReportError("DeleteSpectrumCacheFiles", "intCharIndex was less than 0; this is unexpected in DeleteSpectrumCacheFiles")
                Return
            End If

            strFilePathMatch = strFilePathMatch.Substring(0, intCharIndex)
            strFiles = Directory.GetFiles(mCacheOptions.FolderPath, Path.GetFileName(strFilePathMatch) & "*")

            For intIndex = 0 To strFiles.Length - 1
                File.Delete(strFiles(intIndex))
            Next intIndex

        Catch ex As Exception
            ' Ignore errors here
            ReportError("DeleteSpectrumCacheFiles", "Error deleting cached spectrum files for this task", ex, True, False)
        End Try

        ' Now look for old spectrum cache files
        Try
            strFilePathMatch = SPECTRUM_CACHE_FILE_PREFIX & "*" & SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR & "*"

            objFolder = New DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(ConstructCachedSpectrumPath())))

            If Not objFolder Is Nothing Then
                For Each objFile In objFolder.GetFiles(strFilePathMatch)
                    If objFile.LastWriteTimeUtc < dtFileDateTolerance Then
                        objFile.Delete()
                    End If
                Next objFile
            End If
        Catch ex As Exception
            ReportError("DeleteSpectrumCacheFiles", "Error deleting old cached spectrum files", ex, True, False)
        End Try

    End Sub

    Private Sub ExpandSpectraPool(intNewPoolLength As Integer)
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
                Threading.Thread.Sleep(50)
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
            mSpectrumIndexInPool = New Hashtable
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
        mCacheOptions.Reset()

        InitializeSpectraPool()
    End Sub

    Public Shared Function GetDefaultCacheOptions() As clsSpectrumCacheOptions
        Dim udtCacheOptions = New clsSpectrumCacheOptions()

        With udtCacheOptions
            .DiskCachingAlwaysDisabled = False
            .FolderPath = Path.GetTempPath()
            .SpectraToRetainInMemory = 1000
        End With

        Return udtCacheOptions
    End Function

    <Obsolete("Use GetDefaultCacheOptions, which returns a new instance of clsSpectrumCacheOptions")>
    Public Shared Sub ResetCacheOptions(ByRef udtCacheOptions As clsSpectrumCacheOptions)
        udtCacheOptions = GetDefaultCacheOptions()
    End Sub

    ''' <summary>
    ''' Load the spectrum from disk and cache in SpectraPool
    ''' </summary>
    ''' <param name="intScanNumber">Scan number to load</param>
    ''' <param name="intTargetPoolIndex">Output: index in the array that contains the given spectrum</param>
    ''' <returns>True if successfully uncached, false if an error</returns>
    Private Function UnCacheSpectrum(intScanNumber As Integer, <Out()> ByRef intTargetPoolIndex As Integer) As Boolean

        Dim blnSuccess As Boolean
        Dim blnReturnBlankSpectrum As Boolean

        Dim intIndex As Integer
        Dim intIonCount As Integer
        Dim intScanNumberInCacheFile As Integer

        Dim lngByteOffset As Int64

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
                mPageFileReader.BaseStream.Seek(lngByteOffset, SeekOrigin.Begin)

                intScanNumberInCacheFile = mPageFileReader.ReadInt32()
                intIonCount = mPageFileReader.ReadInt32()
                ValidateMemoryAllocation(SpectraPool(intTargetPoolIndex), intIonCount)

                With SpectraPool(intTargetPoolIndex)
                    .ScanNumber = intScanNumber

                    If (intScanNumberInCacheFile <> .ScanNumber) Then
                        ReportWarning("UncacheSpectrum", "Scan number In cache file doesn't agree with expected scan number in UnCacheSpectrum")
                    End If

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
                SpectraPool(intTargetPoolIndex) = New clsMSSpectrum()
            End If

            With SpectraPool(intTargetPoolIndex)
                .ScanNumber = intScanNumber
                .IonCount = 0
            End With
            blnSuccess = True
        End If

        If Not blnSuccess Then Return False

        SpectraPoolInfo(intTargetPoolIndex).CacheState = eCacheStateConstants.LoadedFromCache

        If mSpectrumIndexInPool.Contains(intScanNumber) Then
            mSpectrumIndexInPool.Item(intScanNumber) = intTargetPoolIndex
        Else
            mSpectrumIndexInPool.Add(intScanNumber, intTargetPoolIndex)
        End If

        If Not blnReturnBlankSpectrum Then mUnCacheEventCount += 1

        Return True

    End Function

    ''Private Function UpdatePoolAccessHistory(intScanNumber As Integer) As Boolean
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
            mCacheOptions.FolderPath = Path.GetTempPath()
            mFolderPathValidated = False
        End If

        If Not mFolderPathValidated Then
            Try
                If Not Path.IsPathRooted(mCacheOptions.FolderPath) Then
                    mCacheOptions.FolderPath = Path.Combine(Path.GetTempPath(), mCacheOptions.FolderPath)
                End If

                If Not Directory.Exists(mCacheOptions.FolderPath) Then
                    Directory.CreateDirectory(mCacheOptions.FolderPath)

                    If Not Directory.Exists(mCacheOptions.FolderPath) Then
                        ReportError("ValidateCachedSpectrumFolder", "Error creating spectrum cache folder: " & mCacheOptions.FolderPath, Nothing, True, False)
                        Return False
                    End If
                End If

                mFolderPathValidated = True
                Return True

            Catch ex As Exception
                ' Error defining .FolderPath
                ReportError("ValidateCachedSpectrumFolder", "Error creating spectrum cache folder", ex, True, False)
                Return False
            End Try
        Else
            Return True
        End If

    End Function

    Private Sub ValidateMemoryAllocation(objMSSpectrum As clsMSSpectrum, intIonCount As Integer)

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

    Private Function ValidatePageFileIO(blnCreateIfUninitialized As Boolean) As Boolean
        ' Validates that we can read and write from a Page file
        ' Opens the page file reader and writer if not yet opened

        Dim blnValid As Boolean

        If mPageFileReader Is Nothing Then
            If blnCreateIfUninitialized Then
                Dim strCacheFilePath As String
                Dim fsWrite As FileStream
                Dim fsRead As FileStream

                Try
                    ' Construct the page file path
                    strCacheFilePath = ConstructCachedSpectrumPath()

                    ' Initialize the binary writer and create the file
                    fsWrite = New FileStream(strCacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)
                    mPageFileWriter = New BinaryWriter(fsWrite)

                    ' Write a header line
                    mPageFileWriter.Write("MASIC Spectrum Cache Page File.  Created " & DateTime.Now.ToLongDateString() & " " & DateTime.Now.ToLongTimeString())

                    ' Add 64 bytes of white space                    
                    ' ReSharper disable once RedundantAssignment
                    For intIndex = 0 To 63
                        mPageFileWriter.Write(Byte.MinValue)
                    Next intIndex
                    mPageFileWriter.Flush()

                    ' Initialize the binary reader
                    fsRead = New FileStream(strCacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                    mPageFileReader = New BinaryReader(fsRead)

                    blnValid = True
                Catch ex As Exception
                    ReportError("ValidatePageFileIO", ex.Message, ex, True, True)
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

    ''' <summary>
    ''' Make sure the spectrum given by intScanNumber is present in FragScanSpectra
    ''' When doing this, update the Pool Access History with this scan number to assure it doesn't get purged from the pool anytime soon
    ''' </summary>
    ''' <param name="intScanNumber">Scan number to load</param>
    ''' <param name="intPoolIndex">Output: index in the array that contains the given spectrum; -1 if no match</param>
    ''' <returns>True if the scan was found in the spectrum pool (or was successfully added to the pool)</returns>
    Public Function ValidateSpectrumInPool(intScanNumber As Integer, <Out()> ByRef intPoolIndex As Integer) As Boolean

        Dim blnSuccess As Boolean

        Try
            If mSpectrumIndexInPool.Contains(intScanNumber) Then
                intPoolIndex = CInt(mSpectrumIndexInPool(intScanNumber))
                Return True
            End If

            ' Need to load the spectrum
            blnSuccess = UnCacheSpectrum(intScanNumber, intPoolIndex)
            Return blnSuccess
        Catch ex As Exception
            ReportError("ValidateSpectrumInPool", ex.Message, ex, True, True)
            intPoolIndex = -1
            Return False
        End Try

    End Function

End Class
