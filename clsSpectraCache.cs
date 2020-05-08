using System;
using System.Collections.Generic;
using System.IO;

namespace MASIC
{
    /// <summary>
    /// Utilizes a spectrum pool to store mass spectra
    /// </summary>
    public class clsSpectraCache : clsMasicEventNotifier, IDisposable
    {
        public clsSpectraCache(clsSpectrumCacheOptions cacheOptions)
        {
            mCacheOptions = cacheOptions;
            InitializeVariables();
        }

        public void Dispose()
        {
            ClosePageFile();
            DeleteSpectrumCacheFiles();
        }

        #region "Constants and Enums"
        private const string SPECTRUM_CACHE_FILE_PREFIX = "$SpecCache";
        private const string SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR = "_Temp";

        private const int SPECTRUM_CACHE_MAX_FILE_AGE_HOURS = 12;

        private enum eCacheStateConstants
        {
            UnusedSlot = 0,              // No data present
            NeverCached = 1,             // In memory, but never cached
            LoadedFromCache = 2,         // Loaded from cache, and in memory; or, loaded using XRaw; safe to purge without caching
        }

        //private enum eCacheRequestStateConstants
        //{
        //    NoRequest = 0,               // Undefined
        //    SafeToCache = 1,             // In memory, but safe to cache or purge
        //    RequestUncache = 2,          // Not in memory, need to uncache
        //    RequestUncacheAndLock = 3,   // Not in memory, need to uncache and lock in memory
        //    LockedInMemory = 4,          // In memory and should not cache
        //}

        //private enum eCacheCommandConstants
        //{
        //    CacheAllSpectraOutOfRange = 0,
        //    CacheSurveyScansOutOfRange = 1,
        //    CacheFragScansOutOfRange = 2,
        //    CacheAllSpectraOutOfRangeDoNotUncache = 3,
        //    ValidateSurveyScanUncached = 4,
        //    ValidateFragScanUncached = 5,
        //    UnlockAllSpectra = 6
        //}

        #endregion

        #region "Structures"

        private struct udtSpectraPoolInfoType
        {
            public eCacheStateConstants CacheState;
            // Public LockInMemory As Boolean
        }

        #endregion

        #region "Classwide Variables"
        public clsMSSpectrum[] SpectraPool;                   // Pool (collection) of currently loaded spectra; 0-based array
        private udtSpectraPoolInfoType[] SpectraPoolInfo;     // Parallel with SpectraPool(), but not publicly visible

        private readonly clsSpectrumCacheOptions mCacheOptions;

        private BinaryReader mPageFileReader;
        private BinaryWriter mPageFileWriter;

        private bool mDirectoryPathValidated;

        // Base filename for this instance of clsMasic, includes a timestamp to allow multiple instances to write to the same cache directory
        private string mCacheFileNameBase;

        private int mCacheEventCount;
        private int mUnCacheEventCount;

        private int mMaximumPoolLength;
        private int mNextAvailablePoolIndex;

        private Dictionary<int, int> mSpectrumIndexInPool;
        private Dictionary<int, long> mSpectrumByteOffset;         // Records the byte offset of the data in the page file for a given scan number

        #endregion

        public int CacheEventCount => mCacheEventCount;

        [Obsolete("Legacy parameter; no longer used")]
        public string CacheFileNameBase => mCacheFileNameBase;

        public string CacheDirectoryPath
        {
            get => mCacheOptions.DirectoryPath;
            set => mCacheOptions.DirectoryPath = value;
        }

        [Obsolete("Legacy parameter; no longer used")]
        public float CacheMaximumMemoryUsageMB
        {
            get => mCacheOptions.MaximumMemoryUsageMB;
            set => mCacheOptions.MaximumMemoryUsageMB = value;
        }

        [Obsolete("Legacy parameter; no longer used")]
        public float CacheMinimumFreeMemoryMB
        {
            get => mCacheOptions.MinimumFreeMemoryMB;
            set
            {
                if (mCacheOptions.MinimumFreeMemoryMB < 10)
                {
                    mCacheOptions.MinimumFreeMemoryMB = 10;
                }

                mCacheOptions.MinimumFreeMemoryMB = value;
            }
        }

        public int CacheSpectraToRetainInMemory
        {
            get => mCacheOptions.SpectraToRetainInMemory;
            set
            {
                if (value < 100)
                    value = 100;
                mCacheOptions.SpectraToRetainInMemory = value;
            }
        }

        public bool DiskCachingAlwaysDisabled
        {
            get => mCacheOptions.DiskCachingAlwaysDisabled;
            set => mCacheOptions.DiskCachingAlwaysDisabled = value;
        }

        public int UnCacheEventCount => mUnCacheEventCount;

        public bool AddSpectrumToPool(
            clsMSSpectrum spectrum,
            int scanNumber)
        {
            // Adds spectrum to the spectrum pool
            // Returns the index of the spectrum in the pool in targetPoolIndex

            try
            {
                int targetPoolIndex;

                if (mSpectrumIndexInPool.ContainsKey(scanNumber))
                {
                    // Replace the spectrum data with objMSSpectrum
                    targetPoolIndex = mSpectrumIndexInPool[scanNumber];
                }
                else
                {
                    // Need to add the spectrum
                    targetPoolIndex = GetNextAvailablePoolIndex();
                    mSpectrumIndexInPool.Add(scanNumber, targetPoolIndex);
                }

                SpectraPool[targetPoolIndex].ReplaceData(spectrum, scanNumber);

                SpectraPoolInfo[targetPoolIndex].CacheState = eCacheStateConstants.NeverCached;

                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Cache the spectrum at the given pool index
        /// </summary>
        /// <param name="poolIndexToCache"></param>
        /// <return>
        /// True if already cached or if successfully cached
        /// False if an error
        /// </return>
        private void CacheSpectrum(int poolIndexToCache)
        {
            if (SpectraPoolInfo[poolIndexToCache].CacheState == eCacheStateConstants.UnusedSlot)
            {
                // Nothing to do; slot is already empty
                return;
            }

            if (SpectraPoolInfo[poolIndexToCache].CacheState == eCacheStateConstants.LoadedFromCache)
            {
                // Already cached previously, simply reset the slot
            }
            else if (ValidatePageFileIO(true))
            {
                // Store all of the spectra in one large file
                CacheSpectrumWork(poolIndexToCache);
            }

            // Remove the spectrum from mSpectrumIndexInPool
            mSpectrumIndexInPool.Remove(SpectraPool[poolIndexToCache].ScanNumber);

            // Reset .ScanNumber, .IonCount, and .CacheState
            SpectraPool[poolIndexToCache].Clear(0);

            SpectraPoolInfo[poolIndexToCache].CacheState = eCacheStateConstants.UnusedSlot;

            mCacheEventCount += 1;
        }

        private void CacheSpectrumWork(int poolIndexToCache)
        {
            const int MAX_RETRIES = 3;

            // See if the given spectrum is already present in the page file
            var scanNumber = SpectraPool[poolIndexToCache].ScanNumber;
            if (mSpectrumByteOffset.ContainsKey(scanNumber))
            {
                // Page file already contains the given scan; do not re-write
                return;
            }

            var initialOffset = mPageFileWriter.BaseStream.Position;

            // Write the spectrum to the page file
            // Record the current offset in the hashtable
            mSpectrumByteOffset.Add(scanNumber, mPageFileWriter.BaseStream.Position);

            var retryCount = MAX_RETRIES;
            while (true)
            {
                try
                {
                    var spectraPool = SpectraPool[poolIndexToCache];
                    // Write the scan number
                    mPageFileWriter.Write(scanNumber);

                    // Write the ion count
                    mPageFileWriter.Write(spectraPool.IonCount);

                    // Write the m/z values
                    for (var index = 0; index < spectraPool.IonCount; index++)
                        mPageFileWriter.Write(spectraPool.IonsMZ[index]);

                    // Write the intensity values
                    for (var index = 0; index < spectraPool.IonCount; index++)
                        mPageFileWriter.Write(spectraPool.IonsIntensity[index]);

                    // Write four blank bytes (not really necessary, but adds a little padding between spectra)
                    mPageFileWriter.Write(0);
                    break;
                }
                catch (Exception ex)
                {
                    retryCount -= 1;
                    var message = string.Format("Error caching scan {0}: {1}", scanNumber, ex.Message);
                    if (retryCount >= 0)
                    {
                        OnWarningEvent(message);

                        // Wait 2, 4, or 8 seconds, then try again
                        var sleepSeconds = Math.Pow(2, MAX_RETRIES - retryCount);
                        System.Threading.Thread.Sleep((int)(sleepSeconds * 1000));

                        mPageFileWriter.BaseStream.Seek(initialOffset, SeekOrigin.Begin);
                    }
                    else
                    {
                        throw new Exception(message, ex);
                    }
                }
            }
        }

        public void ClosePageFile()
        {
            try
            {
                var garbageCollect = false;

                if (mPageFileReader != null)
                {
                    mPageFileReader.Close();
                    mPageFileReader = null;
                    garbageCollect = true;
                }

                if (mPageFileWriter != null)
                {
                    mPageFileWriter.Close();
                    mPageFileWriter = null;
                    garbageCollect = true;
                }

                if (garbageCollect)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                // Ignore errors here
            }

            if (mSpectrumByteOffset == null)
            {
                mSpectrumByteOffset = new Dictionary<int, long>();
            }
            else
            {
                mSpectrumByteOffset.Clear();
            }
        }

        private string ConstructCachedSpectrumPath()
        {
            // Constructs the full path for the given spectrum file
            // Returns String.empty if unable to validate the cached spectrum directory

            if (!ValidateCachedSpectrumDirectory())
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(mCacheFileNameBase))
            {
                var objRand = new Random();

                // Create the cache file name, using both a timestamp and a random number between 1 and 9999
                mCacheFileNameBase = SPECTRUM_CACHE_FILE_PREFIX + DateTime.UtcNow.Hour + DateTime.UtcNow.Minute + DateTime.UtcNow.Second + DateTime.UtcNow.Millisecond + objRand.Next(1, 9999);
            }

            var fileName = mCacheFileNameBase + SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR + ".bin";

            return Path.Combine(mCacheOptions.DirectoryPath, fileName);
        }

        public void DeleteSpectrumCacheFiles()
        {
            // Looks for and deletes the spectrum cache files created by this instance of MASIC
            // Additionally, looks for and deletes spectrum cache files with modification dates more than SPECTRUM_CACHE_MAX_FILE_AGE_HOURS from the present

            var fileDateTolerance = DateTime.UtcNow.Subtract(new TimeSpan(SPECTRUM_CACHE_MAX_FILE_AGE_HOURS, 0, 0));
            try
            {
                // Delete the cached files for this instance of clsMasic
                var filePathMatch = ConstructCachedSpectrumPath();

                var charIndex = filePathMatch.IndexOf(SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR, StringComparison.Ordinal);
                if (charIndex < 0)
                {
                    ReportError("charIndex was less than 0; this is unexpected in DeleteSpectrumCacheFiles");
                    return;
                }

                filePathMatch = filePathMatch.Substring(0, charIndex);
                var files = Directory.GetFiles(mCacheOptions.DirectoryPath, Path.GetFileName(filePathMatch) + "*");

                for (var index = 0; index < files.Length; index++)
                    File.Delete(files[index]);
            }
            catch (Exception ex)
            {
                // Ignore errors here
                ReportError("Error deleting cached spectrum files for this task", ex);
            }

            // Now look for old spectrum cache files
            try
            {
                var filePathMatch = SPECTRUM_CACHE_FILE_PREFIX + "*" + SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR + "*";

                var spectrumFile = new FileInfo(Path.GetFullPath(ConstructCachedSpectrumPath()));
                if (spectrumFile.Directory == null)
                {
                    ReportWarning("Unable to determine the spectrum cache directory path in DeleteSpectrumCacheFiles; this is unexpected");
                    return;
                }

                foreach (var candidateFile in spectrumFile.Directory.GetFiles(filePathMatch))
                {
                    if (candidateFile.LastWriteTimeUtc < fileDateTolerance)
                    {
                        candidateFile.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error deleting old cached spectrum files", ex);
            }
        }

        private void ExpandSpectraPool(int newPoolLength)
        {
            var currentPoolLength = Math.Min(mMaximumPoolLength, SpectraPool.Length);
            mMaximumPoolLength = newPoolLength;

            if (newPoolLength > currentPoolLength)
            {
                var oldSpectraPool = SpectraPool;
                SpectraPool = new clsMSSpectrum[mMaximumPoolLength];
                Array.Copy(oldSpectraPool, SpectraPool, Math.Min(mMaximumPoolLength, oldSpectraPool.Length));
                var oldSpectraPoolInfo = SpectraPoolInfo;
                SpectraPoolInfo = new udtSpectraPoolInfoType[mMaximumPoolLength];
                Array.Copy(oldSpectraPoolInfo, SpectraPoolInfo, Math.Min(mMaximumPoolLength, oldSpectraPoolInfo.Length));

                for (var index = currentPoolLength; index < mMaximumPoolLength; index++)
                {
                    SpectraPool[index] = new clsMSSpectrum(0);
                    SpectraPoolInfo[index].CacheState = eCacheStateConstants.UnusedSlot;
                }
            }
        }

        private int GetNextAvailablePoolIndex()
        {
            // Need to cache the spectrum stored at mNextAvailablePoolIndex
            CacheSpectrum(mNextAvailablePoolIndex);

            var nextPoolIndex = mNextAvailablePoolIndex;

            mNextAvailablePoolIndex += 1;
            if (mNextAvailablePoolIndex >= mMaximumPoolLength)
            {
                if (mCacheOptions.DiskCachingAlwaysDisabled)
                {
                    // The pool is full, but disk caching is disabled, so we have to expand the pool
                    ExpandSpectraPool(mMaximumPoolLength + 500);
                }
                else
                {
                    mNextAvailablePoolIndex = 0;

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(50);
                }
            }

            return nextPoolIndex;
        }

        public void InitializeSpectraPool()
        {
            mMaximumPoolLength = mCacheOptions.SpectraToRetainInMemory;
            if (mMaximumPoolLength < 1)
                mMaximumPoolLength = 1;

            mNextAvailablePoolIndex = 0;

            mCacheEventCount = 0;
            mUnCacheEventCount = 0;

            mDirectoryPathValidated = false;
            mCacheFileNameBase = string.Empty;

            ClosePageFile();

            if (mSpectrumIndexInPool == null)
            {
                mSpectrumIndexInPool = new Dictionary<int, int>();
            }
            else
            {
                mSpectrumIndexInPool.Clear();
            }

            //if (mPoolAccessHistory == null)
            //    mPoolAccessHistory = new Hashtable();
            //else
            //    mPoolAccessHistory.Clear();

            if (SpectraPool == null)
            {
                SpectraPool = new clsMSSpectrum[mMaximumPoolLength];
                SpectraPoolInfo = new udtSpectraPoolInfoType[mMaximumPoolLength];
            }
            else if (SpectraPool.Length < mMaximumPoolLength)
            {
                SpectraPool = new clsMSSpectrum[mMaximumPoolLength];
                SpectraPoolInfo = new udtSpectraPoolInfoType[mMaximumPoolLength];
            }

            // Note: Resetting spectra all the way to SpectraPool.Length, even if SpectraPool.Length is > mMaximumPoolLength
            for (var index = 0; index < SpectraPool.Length; index++)
            {
                SpectraPool[index] = new clsMSSpectrum(0);
                SpectraPoolInfo[index].CacheState = eCacheStateConstants.UnusedSlot;
            }
        }

        private void InitializeVariables()
        {
            mCacheOptions.Reset();

            InitializeSpectraPool();
        }

        public static clsSpectrumCacheOptions GetDefaultCacheOptions()
        {
            var udtCacheOptions = new clsSpectrumCacheOptions
            {
                DiskCachingAlwaysDisabled = false,
                DirectoryPath = Path.GetTempPath(),
                SpectraToRetainInMemory = 1000
            };

            return udtCacheOptions;
        }

        [Obsolete("Use GetDefaultCacheOptions, which returns a new instance of clsSpectrumCacheOptions")]
        // ReSharper disable once RedundantAssignment
        public static void ResetCacheOptions(ref clsSpectrumCacheOptions udtCacheOptions)
        {
            udtCacheOptions = GetDefaultCacheOptions();
        }

        /// <summary>
        /// Load the spectrum from disk and cache in SpectraPool
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="targetPoolIndex">Output: index in the array that contains the given spectrum</param>
        /// <returns>True if successfully uncached, false if an error</returns>
        private bool UnCacheSpectrum(int scanNumber, out int targetPoolIndex)
        {
            var success = false;
            targetPoolIndex = GetNextAvailablePoolIndex();

            // Uncache the spectrum from disk
            var returnBlankSpectrum = false;

            // All of the spectra are stored in one large file
            if (ValidatePageFileIO(false))
            {
                // Lookup the byte offset for the given spectrum

                if (mSpectrumByteOffset.ContainsKey(scanNumber))
                {
                    var byteOffset = mSpectrumByteOffset[scanNumber];

                    // Make sure all previous spectra are flushed to disk
                    mPageFileWriter.Flush();

                    // Read the spectrum from the page file
                    mPageFileReader.BaseStream.Seek(byteOffset, SeekOrigin.Begin);

                    var scanNumberInCacheFile = mPageFileReader.ReadInt32();
                    var ionCount = mPageFileReader.ReadInt32();

                    if (scanNumberInCacheFile != scanNumber)
                    {
                        ReportWarning("Scan number In cache file doesn't agree with expected scan number in UnCacheSpectrum");
                    }

                    var msSpectrum = SpectraPool[targetPoolIndex];

                    msSpectrum.Clear(scanNumber, ionCount);

                    for (var index = 0; index < ionCount; index++)
                        msSpectrum.IonsMZ.Add(mPageFileReader.ReadDouble());

                    for (var index = 0; index < ionCount; index++)
                        msSpectrum.IonsIntensity.Add(mPageFileReader.ReadDouble());

                    success = true;
                }
                else
                {
                    returnBlankSpectrum = true;
                }
            }
            else
            {
                returnBlankSpectrum = true;
            }

            if (returnBlankSpectrum)
            {
                // Scan not found; create a new, blank mass spectrum
                // Its cache state will be set to LoadedFromCache, which is ok, since we don't need to cache it to disk

                // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
                if (SpectraPool[targetPoolIndex] == null)
                {
                    SpectraPool[targetPoolIndex] = new clsMSSpectrum(scanNumber);
                }

                SpectraPool[targetPoolIndex].Clear();

                success = true;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!success)
            {
                return false;
            }

            SpectraPoolInfo[targetPoolIndex].CacheState = eCacheStateConstants.LoadedFromCache;

            if (mSpectrumIndexInPool.ContainsKey(scanNumber))
            {
                mSpectrumIndexInPool[scanNumber] = targetPoolIndex;
            }
            else
            {
                mSpectrumIndexInPool.Add(scanNumber, targetPoolIndex);
            }

            if (!returnBlankSpectrum)
                mUnCacheEventCount += 1;

            return true;
        }

        private bool ValidateCachedSpectrumDirectory()
        {
            if (string.IsNullOrWhiteSpace(mCacheOptions.DirectoryPath))
            {
                // Need to define the spectrum caching directory path
                mCacheOptions.DirectoryPath = Path.GetTempPath();
                mDirectoryPathValidated = false;
            }

            if (!mDirectoryPathValidated)
            {
                try
                {
                    if (!Path.IsPathRooted(mCacheOptions.DirectoryPath))
                    {
                        mCacheOptions.DirectoryPath = Path.Combine(Path.GetTempPath(), mCacheOptions.DirectoryPath);
                    }

                    if (!Directory.Exists(mCacheOptions.DirectoryPath))
                    {
                        Directory.CreateDirectory(mCacheOptions.DirectoryPath);

                        if (!Directory.Exists(mCacheOptions.DirectoryPath))
                        {
                            ReportError("Error creating spectrum cache directory: " + mCacheOptions.DirectoryPath);
                            return false;
                        }
                    }

                    mDirectoryPathValidated = true;
                    return true;
                }
                catch (Exception ex)
                {
                    // Error defining .DirectoryPath
                    ReportError("Error creating spectrum cache directory");
                    return false;
                }
            }

            return true;
        }

        private bool ValidatePageFileIO(bool createIfUninitialized)
        {
            // Validates that we can read and write from a Page file
            // Opens the page file reader and writer if not yet opened

            if (mPageFileReader != null)
            {
                return true;
            }

            if (!createIfUninitialized)
            {
                return false;
            }

            try
            {
                // Construct the page file path
                var cacheFilePath = ConstructCachedSpectrumPath();

                // Initialize the binary writer and create the file
                mPageFileWriter = new BinaryWriter(new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

                // Write a header line
                mPageFileWriter.Write(
                    "MASIC Spectrum Cache Page File.  Created " + DateTime.Now.ToLongDateString() + " " +
                    DateTime.Now.ToLongTimeString());

                // Add 64 bytes of white space
                for (var index = 0; index <= 63; index++)
                    mPageFileWriter.Write(byte.MinValue);

                mPageFileWriter.Flush();

                // Initialize the binary reader
                mPageFileReader = new BinaryReader(new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Make sure the spectrum given by scanNumber is present in FragScanSpectra
        /// When doing this, update the Pool Access History with this scan number to assure it doesn't get purged from the pool anytime soon
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="poolIndex">Output: index in the array that contains the given spectrum; -1 if no match</param>
        /// <returns>True if the scan was found in the spectrum pool (or was successfully added to the pool)</returns>
        public bool ValidateSpectrumInPool(int scanNumber, out int poolIndex)
        {
            try
            {
                if (mSpectrumIndexInPool.ContainsKey(scanNumber))
                {
                    poolIndex = mSpectrumIndexInPool[scanNumber];
                    return true;
                }

                // Need to load the spectrum
                var success = UnCacheSpectrum(scanNumber, out poolIndex);
                return success;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                poolIndex = -1;
                return false;
            }
        }
    }
}
