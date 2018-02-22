Option Strict On

Imports System.Runtime.InteropServices

''' <summary>
''' Utilizes a spectrum pool to store mass spectra
''' </summary>
Public Class clsSpectraCache
    Inherits clsMasicEventNotifier
    Implements IDisposable

    Public Sub New(cacheOptions As clsSpectrumCacheOptions)
        mCacheOptions = cacheOptions
        InitializeVariables()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ClosePageFile()
        DeleteSpectrumCacheFiles()
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

    Public ReadOnly Property CacheEventCount As Integer
        Get
            Return mCacheEventCount
        End Get
    End Property

    Public ReadOnly Property CacheFileNameBase As String
        Get
            Return mCacheFileNameBase
        End Get
    End Property

    Public Property CacheFolderPath As String
        Get
            Return mCacheOptions.FolderPath
        End Get
        Set
            mCacheOptions.FolderPath = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheMaximumMemoryUsageMB As Single
        Get
            Return mCacheOptions.MaximumMemoryUsageMB
        End Get
        Set
            mCacheOptions.MaximumMemoryUsageMB = Value
        End Set
    End Property

    <Obsolete("Legacy parameter; no longer used")>
    Public Property CacheMinimumFreeMemoryMB As Single
        Get
            Return mCacheOptions.MinimumFreeMemoryMB
        End Get
        Set
            If mCacheOptions.MinimumFreeMemoryMB < 10 Then
                mCacheOptions.MinimumFreeMemoryMB = 10
            End If
            mCacheOptions.MinimumFreeMemoryMB = Value
        End Set
    End Property

    Public Property CacheSpectraToRetainInMemory As Integer
        Get
            Return mCacheOptions.SpectraToRetainInMemory
        End Get
        Set
            If Value < 100 Then Value = 100
            mCacheOptions.SpectraToRetainInMemory = Value
        End Set
    End Property

    Public Property DiskCachingAlwaysDisabled As Boolean
        Get
            Return mCacheOptions.DiskCachingAlwaysDisabled
        End Get
        Set
            mCacheOptions.DiskCachingAlwaysDisabled = Value
        End Set
    End Property

    Public ReadOnly Property UnCacheEventCount As Integer
        Get
            Return mUnCacheEventCount
        End Get
    End Property

    Public Function AddSpectrumToPool(
       spectrum As clsMSSpectrum,
       scanNumber As Integer) As Boolean

        ' Adds objMSSpectrum to the spectrum pool
        ' Returns the index of the spectrum in the pool in targetPoolIndex

        Try
            Dim targetPoolIndex As Integer

            If mSpectrumIndexInPool.Contains(scanNumber) Then
                ' Replace the spectrum data with objMSSpectrum
                targetPoolIndex = CInt(mSpectrumIndexInPool(scanNumber))
            Else
                ' Need to add the spectrum
                targetPoolIndex = GetNextAvailablePoolIndex()
                mSpectrumIndexInPool.Add(scanNumber, targetPoolIndex)
            End If

            ValidateMemoryAllocation(SpectraPool(targetPoolIndex), spectrum.IonCount)

            With SpectraPool(targetPoolIndex)
                .ScanNumber = spectrum.ScanNumber
                If .ScanNumber <> scanNumber Then
                    .ScanNumber = scanNumber
                End If

                .IonCount = spectrum.IonCount
            End With

            Array.Copy(spectrum.IonsMZ, SpectraPool(targetPoolIndex).IonsMZ, spectrum.IonCount)
            Array.Copy(spectrum.IonsIntensity, SpectraPool(targetPoolIndex).IonsIntensity, spectrum.IonCount)

            SpectraPoolInfo(targetPoolIndex).CacheState = eCacheStateConstants.NeverCached

            Return True
        Catch ex As Exception
            ReportError(ex.Message, ex)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Cache the spectrum at the given pool index
    ''' </summary>
    ''' <param name="poolIndexToCache"></param>
    ''' <return>
    ''' True if already cached or if successfully cached
    ''' False if an error
    ''' </return>
    Private Sub CacheSpectrum(poolIndexToCache As Integer)
        Const MAX_RETRIES = 3

        If SpectraPoolInfo(poolIndexToCache).CacheState = eCacheStateConstants.UnusedSlot Then
            ' Nothing to do; slot is already empty
        Else
            If SpectraPoolInfo(poolIndexToCache).CacheState = eCacheStateConstants.LoadedFromCache Then
                ' Already cached previously, simply reset the slot
            Else

                ' Store all of the spectra in one large file

                If ValidatePageFileIO(True) Then
                    ' See if the given spectrum is already present in the page file
                    Dim scanNumber = SpectraPool(poolIndexToCache).ScanNumber
                    If mSpectrumByteOffset.Contains(scanNumber) Then
                        ' Page file already contains the given scan; do not re-write
                    Else
                        Dim initialOffset = mPageFileWriter.BaseStream.Position

                        ' Write the spectrum to the page file
                        ' Record the current offset in the hashtable
                        mSpectrumByteOffset.Add(scanNumber, mPageFileWriter.BaseStream.Position)

                        Dim retryCount = MAX_RETRIES
                        Do While retryCount >= 0
                            Try
                                With SpectraPool(poolIndexToCache)
                                    ' Write the scan number
                                    mPageFileWriter.Write(scanNumber)

                                    ' Write the ion count
                                    mPageFileWriter.Write(.IonCount)

                                    ' Write the m/z values
                                    For index = 0 To .IonCount - 1
                                        mPageFileWriter.Write(.IonsMZ(index))
                                    Next

                                    ' Write the intensity values
                                    For index = 0 To .IonCount - 1
                                        mPageFileWriter.Write(.IonsIntensity(index))
                                    Next
                                End With

                                ' Write four blank bytes (not really necessary, but adds a little padding between spectra)
                                mPageFileWriter.Write(0)

                                Exit Do

                            Catch ex As Exception
                                retryCount -= 1
                                Dim message = String.Format("Error caching scan {0}: {1}", scanNumber, ex.Message)
                                If retryCount >= 0 Then
                                    OnWarningEvent(message)

                                    ' Wait 2, 4, or 8 seconds, then try again
                                    Dim sleepSeconds = Math.Pow(2, MAX_RETRIES - retryCount)
                                    Threading.Thread.Sleep(CInt(sleepSeconds * 1000))

                                    mPageFileWriter.BaseStream.Seek(initialOffset, SeekOrigin.Begin)
                                Else
                                    Throw New Exception(message, ex)
                                End If
                            End Try
                        Loop

                    End If
                End If

            End If

            ' Remove the spectrum from mSpectrumIndexInPool
            mSpectrumIndexInPool.Remove(SpectraPool(poolIndexToCache).ScanNumber)

            ' Reset .ScanNumber, .IonCount, and .CacheState
            With SpectraPool(poolIndexToCache)
                .ScanNumber = 0
                .IonCount = 0
            End With

            SpectraPoolInfo(poolIndexToCache).CacheState = eCacheStateConstants.UnusedSlot

            mCacheEventCount += 1

        End If

        Return
    End Sub

    Public Sub ClosePageFile()


        Try
            Dim garbageCollect As Boolean

            If Not mPageFileReader Is Nothing Then
                mPageFileReader.Close()
                mPageFileReader = Nothing
                garbageCollect = True
            End If

            If Not mPageFileWriter Is Nothing Then
                mPageFileWriter.Close()
                mPageFileWriter = Nothing
                garbageCollect = True
            End If

            If garbageCollect Then
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

        If Not ValidateCachedSpectrumFolder() Then
            Return String.Empty
        End If

        If String.IsNullOrWhiteSpace(mCacheFileNameBase) Then
            Dim objRand As New Random()

            ' Create the cache file name, using both a timestamp and a random number between 1 and 9999
            mCacheFileNameBase = SPECTRUM_CACHE_FILE_PREFIX & DateTime.UtcNow.Hour & DateTime.UtcNow.Minute & DateTime.UtcNow.Second & DateTime.UtcNow.Millisecond & objRand.Next(1, 9999)
        End If

        Dim fileName = mCacheFileNameBase & SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR & ".bin"

        Return Path.Combine(mCacheOptions.FolderPath, fileName)

    End Function

    Public Sub DeleteSpectrumCacheFiles()
        ' Looks for and deletes the spectrum cache files created by this instance of MASIC
        ' Additionally, looks for and deletes spectrum cache files with modification dates more than SPECTRUM_CACHE_MAX_FILE_AGE_HOURS from the present

        Dim dtFileDateTolerance = DateTime.UtcNow.Subtract(New TimeSpan(SPECTRUM_CACHE_MAX_FILE_AGE_HOURS, 0, 0))

        Try
            ' Delete the cached files for this instance of clsMasic
            Dim filePathMatch = ConstructCachedSpectrumPath()

            Dim charIndex = filePathMatch.IndexOf(SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR, StringComparison.Ordinal)
            If charIndex < 0 Then
                ReportError("charIndex was less than 0; this is unexpected in DeleteSpectrumCacheFiles")
                Return
            End If

            filePathMatch = filePathMatch.Substring(0, charIndex)
            Dim files = Directory.GetFiles(mCacheOptions.FolderPath, Path.GetFileName(filePathMatch) & "*")

            For index = 0 To files.Length - 1
                File.Delete(files(index))
            Next

        Catch ex As Exception
            ' Ignore errors here
            ReportError("Error deleting cached spectrum files for this task", ex)
        End Try

        ' Now look for old spectrum cache files
        Try
            Dim filePathMatch = SPECTRUM_CACHE_FILE_PREFIX & "*" & SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR & "*"

            Dim objFolder = New DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(ConstructCachedSpectrumPath())))

            If Not objFolder Is Nothing Then
                For Each objFile In objFolder.GetFiles(filePathMatch)
                    If objFile.LastWriteTimeUtc < dtFileDateTolerance Then
                        objFile.Delete()
                    End If
                Next objFile
            End If
        Catch ex As Exception
            ReportError("Error deleting old cached spectrum files", ex)
        End Try

    End Sub

    Private Sub ExpandSpectraPool(newPoolLength As Integer)

        Dim currentPoolLength = Math.Min(mMaximumPoolLength, SpectraPool.Length)
        mMaximumPoolLength = newPoolLength

        If newPoolLength > currentPoolLength Then
            ReDim Preserve SpectraPool(mMaximumPoolLength - 1)
            ReDim Preserve SpectraPoolInfo(mMaximumPoolLength - 1)

            For index = currentPoolLength To mMaximumPoolLength - 1
                SpectraPool(index) = New clsMSSpectrum
                SpectraPoolInfo(index).CacheState = eCacheStateConstants.UnusedSlot
            Next
        End If

    End Sub

    Private Function GetNextAvailablePoolIndex() As Integer
        Dim nextPoolIndex As Integer

        ' Need to cache the spectrum stored at mNextAvailablePoolIndex
        CacheSpectrum(mNextAvailablePoolIndex)

        nextPoolIndex = mNextAvailablePoolIndex

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

        Return nextPoolIndex

    End Function

    Public Sub InitializeSpectraPool()

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
        For index = 0 To SpectraPool.Length - 1
            SpectraPool(index) = New clsMSSpectrum
            SpectraPoolInfo(index).CacheState = eCacheStateConstants.UnusedSlot
        Next

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
    ''' <param name="scanNumber">Scan number to load</param>
    ''' <param name="targetPoolIndex">Output: index in the array that contains the given spectrum</param>
    ''' <returns>True if successfully uncached, false if an error</returns>
    Private Function UnCacheSpectrum(scanNumber As Integer, <Out> ByRef targetPoolIndex As Integer) As Boolean

        Dim success As Boolean
        targetPoolIndex = GetNextAvailablePoolIndex()

        ' Uncache the spectrum from disk
        Dim returnBlankSpectrum = False

        ' All of the spectra are stored in one large file
        If ValidatePageFileIO(False) Then
            ' Lookup the byte offset for the given spectrum

            If mSpectrumByteOffset.Contains(scanNumber) Then
                Dim lngByteOffset = CType(mSpectrumByteOffset.Item(scanNumber), Long)

                ' Make sure all previous spectra are flushed to disk
                mPageFileWriter.Flush()

                ' Read the spectrum from the page file
                mPageFileReader.BaseStream.Seek(lngByteOffset, SeekOrigin.Begin)

                Dim scanNumberInCacheFile = mPageFileReader.ReadInt32()
                Dim ionCount = mPageFileReader.ReadInt32()
                ValidateMemoryAllocation(SpectraPool(targetPoolIndex), ionCount)

                With SpectraPool(targetPoolIndex)
                    .ScanNumber = scanNumber

                    If (scanNumberInCacheFile <> .ScanNumber) Then
                        ReportWarning("Scan number In cache file doesn't agree with expected scan number in UnCacheSpectrum")
                    End If

                    .IonCount = ionCount
                    For index = 0 To .IonCount - 1
                        .IonsMZ(index) = mPageFileReader.ReadDouble()
                    Next

                    For index = 0 To .IonCount - 1
                        .IonsIntensity(index) = mPageFileReader.ReadSingle()
                    Next

                End With

                success = True

            Else
                returnBlankSpectrum = True
            End If
        Else
            returnBlankSpectrum = True
        End If

        If returnBlankSpectrum Then
            ' Scan not found; create a new, blank mass spectrum
            ' Its cache state will be set to LoadedFromCache, which is ok, since we don't need to cache it to disk
            If SpectraPool(targetPoolIndex) Is Nothing Then
                SpectraPool(targetPoolIndex) = New clsMSSpectrum()
            End If

            With SpectraPool(targetPoolIndex)
                .ScanNumber = scanNumber
                .IonCount = 0
            End With
            success = True
        End If

        If Not success Then
            Return False
        End If

        SpectraPoolInfo(targetPoolIndex).CacheState = eCacheStateConstants.LoadedFromCache

        If mSpectrumIndexInPool.Contains(scanNumber) Then
            mSpectrumIndexInPool.Item(scanNumber) = targetPoolIndex
        Else
            mSpectrumIndexInPool.Add(scanNumber, targetPoolIndex)
        End If

        If Not returnBlankSpectrum Then mUnCacheEventCount += 1

        Return True

    End Function

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
                        ReportError("Error creating spectrum cache folder: " & mCacheOptions.FolderPath)
                        Return False
                    End If
                End If

                mFolderPathValidated = True
                Return True

            Catch ex As Exception
                ' Error defining .FolderPath
                ReportError("Error creating spectrum cache folder")
                Return False
            End Try
        Else
            Return True
        End If

    End Function

    Private Sub ValidateMemoryAllocation(spectrum As clsMSSpectrum, ionCount As Integer)

        With spectrum
            If .IonsMZ.Length < ionCount Then
                ' Find the next largest multiple of DEFAULT_SPECTRUM_ION_COUNT that is larger than ionCount

                Dim allocationAmount = .IonsMZ.Length
                Do While allocationAmount < ionCount
                    allocationAmount += DEFAULT_SPECTRUM_ION_COUNT
                Loop

                ReDim .IonsMZ(allocationAmount - 1)
                ReDim .IonsIntensity(allocationAmount - 1)
            End If
        End With
    End Sub

    Private Function ValidatePageFileIO(createIfUninitialized As Boolean) As Boolean
        ' Validates that we can read and write from a Page file
        ' Opens the page file reader and writer if not yet opened

        If mPageFileReader IsNot Nothing Then
            Return True
        End If

        If Not createIfUninitialized Then
            Return False
        End If

        Try
            ' Construct the page file path
            Dim cacheFilePath = ConstructCachedSpectrumPath()

            ' Initialize the binary writer and create the file
            mPageFileWriter = New BinaryWriter(New FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))

            ' Write a header line
            mPageFileWriter.Write(
                "MASIC Spectrum Cache Page File.  Created " & DateTime.Now.ToLongDateString() & " " &
                DateTime.Now.ToLongTimeString())

            ' Add 64 bytes of white space
            For index = 0 To 63
                mPageFileWriter.Write(Byte.MinValue)
            Next
            mPageFileWriter.Flush()

            ' Initialize the binary reader
            mPageFileReader = New BinaryReader(New FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))

            Return True

        Catch ex As Exception
            ReportError(ex.Message, ex)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Make sure the spectrum given by scanNumber is present in FragScanSpectra
    ''' When doing this, update the Pool Access History with this scan number to assure it doesn't get purged from the pool anytime soon
    ''' </summary>
    ''' <param name="scanNumber">Scan number to load</param>
    ''' <param name="poolIndex">Output: index in the array that contains the given spectrum; -1 if no match</param>
    ''' <returns>True if the scan was found in the spectrum pool (or was successfully added to the pool)</returns>
    Public Function ValidateSpectrumInPool(scanNumber As Integer, <Out> ByRef poolIndex As Integer) As Boolean

        Try
            If mSpectrumIndexInPool.Contains(scanNumber) Then
                poolIndex = CInt(mSpectrumIndexInPool(scanNumber))
                Return True
            End If

            ' Need to load the spectrum
            Dim success = UnCacheSpectrum(scanNumber, poolIndex)
            Return success
        Catch ex As Exception
            ReportError(ex.Message, ex)
            poolIndex = -1
            Return False
        End Try

    End Function

End Class
