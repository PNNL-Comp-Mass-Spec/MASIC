using System;
using System.Collections.Generic;
using System.IO;
using MASIC.Options;

namespace MASIC
{
    /// <summary>
    /// Utilizes a spectrum pool to store mass spectra
    /// </summary>
    public class clsSpectraCache : clsMasicEventNotifier, IDisposable
    {
        // Ignore Spelling: uncache, uncached

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cacheOptions"></param>
        public clsSpectraCache(SpectrumCacheOptions cacheOptions)
        {
            mCacheOptions = cacheOptions;
            InitializeVariables();
        }

        public void Dispose()
        {
            ClosePageFile();
            DeleteSpectrumCacheFiles();
        }

        private const string SPECTRUM_CACHE_FILE_PREFIX = "$SpecCache";
        private const string SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR = "_Temp";

        private const int SPECTRUM_CACHE_MAX_FILE_AGE_HOURS = 12;

        private enum eCacheStateConstants
        {
            /// <summary>
            /// No data present
            /// </summary>
            UnusedSlot = 0,

            /// <summary>
            /// In memory, but never cached
            /// </summary>
            NeverCached = 1,

            /// <summary>
            /// Loaded from cache, and in memory; or, loaded using XRaw; safe to purge without caching
            /// </summary>
            LoadedFromCache = 2
        }

        /// <summary>
        /// Pool (collection) of currently loaded spectra
        /// </summary>
        private IScanMemoryCache spectraPool;

        private readonly SpectrumCacheOptions mCacheOptions;

        private BinaryReader mPageFileReader;
        private BinaryWriter mPageFileWriter;

        private bool mDirectoryPathValidated;

        private int mMaximumPoolLength;

        /// <summary>
        /// Records the byte offset of the data in the page file for a given scan number
        /// </summary>
        private Dictionary<int, long> mSpectrumByteOffset;

        /// <summary>
        /// Number of cache events
        /// </summary>
        public int CacheEventCount { get; private set; }

        /// <summary>
        /// Number of times the spectrum was found in the in-memory spectrum pool
        /// </summary>
        public int SpectraPoolHitEventCount { get; private set; }

        /// <summary>
        /// Spectrum cache directory path
        /// </summary>
        public string CacheDirectoryPath
        {
            get => mCacheOptions.DirectoryPath;
            set => mCacheOptions.DirectoryPath = value;
        }

        /// <summary>
        /// Maximum memory that the cache is allowed to use
        /// </summary>
        [Obsolete("Legacy parameter; no longer used")]
        public float CacheMaximumMemoryUsageMB
        {
            get => mCacheOptions.MaximumMemoryUsageMB;
            set => mCacheOptions.MaximumMemoryUsageMB = value;
        }

        /// <summary>
        /// Minimum memory that the cache will use
        /// </summary>
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

        /// <summary>
        /// Number of spectra to keep in the in-memory cache
        /// </summary>
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

        /// <summary>
        /// When True, disk caching is disabled
        /// </summary>
        public bool DiskCachingAlwaysDisabled
        {
            get => mCacheOptions.DiskCachingAlwaysDisabled;
            set => mCacheOptions.DiskCachingAlwaysDisabled = value;
        }

        /// <summary>
        /// Number of times a spectrum was loaded from disk and cached in the SpectraPool
        /// </summary>
        public int UnCacheEventCount { get; private set; }

        /// <summary>
        /// The number of spectra we expect to read, updated to the number cached (to disk)
        /// </summary>
        public int SpectrumCount { get; set; }

        /// <summary>
        /// Adds spectrum to the spectrum pool
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="scanNumber"></param>
        /// <returns>Index of the spectrum in the pool in targetPoolIndex</returns>
        public bool AddSpectrumToPool(
            clsMSSpectrum spectrum,
            int scanNumber)
        {
            try
            {
                if (SpectrumCount > CacheSpectraToRetainInMemory + 5 &&
                    !DiskCachingAlwaysDisabled &&
                    ValidatePageFileIO(true))
                {
                    // Store all of the spectra in one large file
                    CacheSpectrumWork(spectrum);

                    CacheEventCount++;
                    return true;
                }

                if (spectraPool.GetItem(scanNumber, out var cacheItem))
                {
                    // Replace the spectrum data with spectrum
                    cacheItem.Scan.ReplaceData(spectrum, scanNumber);
                    cacheItem.CacheState = eCacheStateConstants.NeverCached;
                }
                else
                {
                    // Need to add the spectrum
                    AddItemToSpectraPool(new ScanMemoryCacheItem(spectrum, eCacheStateConstants.NeverCached));
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                return false;
            }
        }

        private void CacheSpectrumWork(clsMSSpectrum spectrumToCache)
        {
            const int MAX_RETRIES = 3;

            // See if the given spectrum is already present in the page file
            var scanNumber = spectrumToCache.ScanNumber;
            if (mSpectrumByteOffset.ContainsKey(scanNumber))
            {
                // Page file already contains the given scan;
                // re-cache the item. for some reason we have updated peaks.
                mSpectrumByteOffset.Remove(scanNumber);

                // Data not changed; do not re-write
                //return;
            }

            var initialOffset = mPageFileWriter.BaseStream.Position;

            // Write the spectrum to the page file
            // Record the current offset in the hash table
            mSpectrumByteOffset.Add(scanNumber, mPageFileWriter.BaseStream.Position);
            if (mSpectrumByteOffset.Count > SpectrumCount)
                SpectrumCount = mSpectrumByteOffset.Count;

            var retryCount = MAX_RETRIES;
            while (true)
            {
                try
                {
                    // Write the scan number
                    mPageFileWriter.Write(scanNumber);

                    // Write the ion count
                    mPageFileWriter.Write(spectrumToCache.IonCount);

                    // Write the m/z values
                    for (var index = 0; index < spectrumToCache.IonCount; index++)
                    {
                        mPageFileWriter.Write(spectrumToCache.IonsMZ[index]);
                    }

                    // Write the intensity values
                    for (var index = 0; index < spectrumToCache.IonCount; index++)
                    {
                        mPageFileWriter.Write(spectrumToCache.IonsIntensity[index]);
                    }

                    // Write four blank bytes (not really necessary, but adds a little padding between spectra)
                    mPageFileWriter.Write(0);
                    break;
                }
                catch (Exception ex)
                {
                    retryCount--;
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

        /// <summary>
        /// Close the page file
        /// </summary>
        public void ClosePageFile()
        {
            try
            {
                if (mPageFileReader != null)
                {
                    mPageFileReader.Close();
                    mPageFileReader = null;
                }

                if (mPageFileWriter != null)
                {
                    mPageFileWriter.Close();
                    mPageFileWriter = null;
                }
            }
            catch (Exception)
            {
                // Ignore errors here
            }

            mSpectrumByteOffset = new Dictionary<int, long>();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Threading.Thread.Sleep(500);
        }

        /// <summary>
        /// Constructs the full path for the given spectrum file
        /// </summary>
        /// <returns>The file path, or an empty string if unable to validate the spectrum cache directory</returns>
        private string ConstructCachedSpectrumPath()
        {
            if (!ValidateCachedSpectrumDirectory())
            {
                return string.Empty;
            }

            var randomGenerator = new Random();

            // Create the cache file name, using both a timestamp and a random number between 1 and 9999
            var baseName = SPECTRUM_CACHE_FILE_PREFIX +
                                 DateTime.UtcNow.Hour + DateTime.UtcNow.Minute + DateTime.UtcNow.Second + DateTime.UtcNow.Millisecond +
                                 randomGenerator.Next(1, 9999);

            var fileName = baseName + SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR + ".bin";

            return Path.Combine(mCacheOptions.DirectoryPath, fileName);
        }

        /// <summary>
        /// Looks for and deletes the spectrum cache files created by this instance of MASIC
        /// Additionally, looks for and deletes spectrum cache files with modification dates more than SPECTRUM_CACHE_MAX_FILE_AGE_HOURS from the present
        /// </summary>
        public void DeleteSpectrumCacheFiles()
        {
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

                var basePath = filePathMatch.Substring(0, charIndex);
                var cacheFiles = Directory.GetFiles(mCacheOptions.DirectoryPath, Path.GetFileName(basePath) + "*");

                foreach (var cacheFile in cacheFiles)
                {
                    File.Delete(cacheFile);
                }
            }
            catch (Exception ex)
            {
                // Report this error, but otherwise ignore it
                ReportError("Error deleting cached spectrum files for this task", ex);
            }

            // Now look for old spectrum cache files
            try
            {
                const string filePathMatch = SPECTRUM_CACHE_FILE_PREFIX + "*" + SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR + "*";

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

        /// <summary>
        /// Checks the spectraPool for available capacity, caching the oldest item if full
        /// </summary>
        private void AddItemToSpectraPool(ScanMemoryCacheItem itemToAdd)
        {
            // Disk caching disabled: expand the size of the in-memory cache
            if (spectraPool.Count >= mMaximumPoolLength &&
                mCacheOptions.DiskCachingAlwaysDisabled)
            {
                // The pool is full, but disk caching is disabled, so we have to expand the pool
                var newPoolLength = mMaximumPoolLength + 500;

                var currentPoolLength = Math.Min(mMaximumPoolLength, spectraPool.Capacity);
                mMaximumPoolLength = newPoolLength;

                if (newPoolLength > currentPoolLength)
                {
                    spectraPool.Capacity = mMaximumPoolLength;
                }
            }

            // Need to cache the spectrum stored at mNextAvailablePoolIndex
            // Removes the oldest spectrum from spectraPool
            if (spectraPool.AddNew(itemToAdd, out var cacheItem))
            {
                // An item was removed from the spectraPool. Write it to the disk cache if needed.
                if (cacheItem.CacheState == eCacheStateConstants.LoadedFromCache)
                {
                    // Already cached previously, simply reset the slot
                }
                else if (cacheItem.CacheState == eCacheStateConstants.NeverCached &&
                         ValidatePageFileIO(true))
                {
                    // Store all of the spectra in one large file
                    CacheSpectrumWork(cacheItem.Scan);
                }

                if (cacheItem.CacheState != eCacheStateConstants.UnusedSlot)
                {
                    // Reset .ScanNumber, .IonCount, and .CacheState
                    cacheItem.Scan.Clear(0);
                    cacheItem.CacheState = eCacheStateConstants.UnusedSlot;

                    CacheEventCount++;
                }
            }
        }

        /// <summary>
        /// Configures the spectra cache after all options have been set.
        /// </summary>
        public void InitializeSpectraPool()
        {
            mMaximumPoolLength = mCacheOptions.SpectraToRetainInMemory;
            if (mMaximumPoolLength < 1)
                mMaximumPoolLength = 1;

            CacheEventCount = 0;
            UnCacheEventCount = 0;
            SpectraPoolHitEventCount = 0;

            mDirectoryPathValidated = false;

            ClosePageFile();

            if (spectraPool == null || spectraPool.Capacity < mMaximumPoolLength)
            {
                if (mCacheOptions.DiskCachingAlwaysDisabled)
                {
                    spectraPool = new MemoryCacheArray(mMaximumPoolLength);
                }
                else
                {
                    spectraPool = new MemoryCacheLRU(mMaximumPoolLength);
                }
            }
            else
            {
                spectraPool.Clear();
            }
        }

        private void InitializeVariables()
        {
            mCacheOptions.Reset();

            InitializeSpectraPool();
        }

        /// <summary>
        /// Get default cache options
        /// </summary>
        /// <returns></returns>
        public static SpectrumCacheOptions GetDefaultCacheOptions()
        {
            var cacheOptions = new SpectrumCacheOptions
            {
                DiskCachingAlwaysDisabled = false,
                DirectoryPath = Path.GetTempPath(),
                SpectraToRetainInMemory = 1000
            };

            return cacheOptions;
        }

        /// <summary>
        /// Load the spectrum from disk and cache in SpectraPool
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="msSpectrum">Output: spectrum for scan number</param>
        /// <returns>True if successfully uncached, false if an error</returns>
        private bool UnCacheSpectrum(int scanNumber, out clsMSSpectrum msSpectrum)
        {
            // Make sure we have a valid object
            var cacheItem = new ScanMemoryCacheItem(new clsMSSpectrum(scanNumber), eCacheStateConstants.LoadedFromCache);

            msSpectrum = cacheItem.Scan;

            // Uncache the spectrum from disk
            if (!UnCacheSpectrumWork(scanNumber, msSpectrum))
            {
                // Scan not found; use a blank mass spectrum
                // Its cache state will be set to LoadedFromCache, which is OK, since we don't need to cache it to disk
                msSpectrum.Clear(scanNumber);
            }

            cacheItem.CacheState = eCacheStateConstants.LoadedFromCache;
            AddItemToSpectraPool(cacheItem);

            return true;
        }

        /// <summary>
        /// Load the spectrum from disk and cache in SpectraPool
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="msSpectrum"><see cref="clsMSSpectrum"/> object to store data into; supplying 'null' is an exception.</param>
        /// <returns>True if successfully uncached, false if an error</returns>
        private bool UnCacheSpectrumWork(int scanNumber, clsMSSpectrum msSpectrum)
        {
            var success = false;
            msSpectrum.Clear();

            // All of the spectra are stored in one large file
            // Lookup the byte offset for the given spectrum
            if (ValidatePageFileIO(false) &&
                mSpectrumByteOffset.ContainsKey(scanNumber))
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

                msSpectrum.Clear(scanNumber, ionCount);

                // Optimization: byte read, Buffer.BlockCopy, and AddRange can be very efficient, and therefore faster than ReadDouble() and Add.
                // It may require more memory, but it is all very short term, and should be removed by a level 1 garbage collection
                var byteCount = ionCount * 8;
                var byteBuffer = new byte[byteCount];
                var dblBuffer = new double[ionCount];
                mPageFileReader.Read(byteBuffer, 0, byteCount);
                Buffer.BlockCopy(byteBuffer, 0, dblBuffer, 0, byteCount);
                msSpectrum.IonsMZ.AddRange(dblBuffer);

                mPageFileReader.Read(byteBuffer, 0, byteCount);
                Buffer.BlockCopy(byteBuffer, 0, dblBuffer, 0, byteCount);
                msSpectrum.IonsIntensity.AddRange(dblBuffer);

                //for (var index = 0; index < ionCount; index++)
                //    msSpectrum.IonsMZ.Add(mPageFileReader.ReadDouble());
                //
                //for (var index = 0; index < ionCount; index++)
                //    msSpectrum.IonsIntensity.Add(mPageFileReader.ReadDouble());

                UnCacheEventCount++;
                success = true;
            }

            return success;
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
                    ReportError("Error creating spectrum cache directory: " + ex.Message);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates that we can read and write from a Page file
        /// Opens the page file reader and writer if not yet opened
        /// </summary>
        /// <param name="createIfUninitialized"></param>
        /// <returns></returns>
        private bool ValidatePageFileIO(bool createIfUninitialized)
        {
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
                {
                    mPageFileWriter.Write(byte.MinValue);
                }

                mPageFileWriter.Flush();

                // Initialize the binary reader
                mPageFileReader = new BinaryReader(new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192));

                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Get a spectrum from the pool or cache, potentially without updating the pool
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="spectrum">The requested spectrum</param>
        /// <param name="canSkipPool">if true and the spectrum is not in the pool, it will be read from the disk cache without updating the pool.
        /// This should be true for any spectrum requests that are not likely to be repeated within the next <see cref="SpectrumCacheOptions.SpectraToRetainInMemory"/> requests.</param>
        /// <returns>True if the scan was found in the spectrum pool (or was successfully added to the pool)</returns>
        public bool GetSpectrum(int scanNumber, out clsMSSpectrum spectrum, bool canSkipPool = true)
        {
            try
            {
                if (spectraPool.GetItem(scanNumber, out var cacheItem))
                {
                    SpectraPoolHitEventCount++;
                    spectrum = cacheItem.Scan;
                    return true;
                }

                if (!canSkipPool)
                {
                    // Need to load the spectrum
                    var success = UnCacheSpectrum(scanNumber, out var cacheSpectrum);
                    spectrum = cacheSpectrum;
                    return success;
                }

                spectrum = new clsMSSpectrum(scanNumber);
                UnCacheSpectrumWork(scanNumber, spectrum);

                // Maintain functionality: return true, even if the spectrum was not in the cache file.
                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                spectrum = null;
                return false;
            }
        }

        /// <summary>
        /// Container to group needed information in the in-memory cache
        /// </summary>
        private class ScanMemoryCacheItem
        {
            /// <summary>
            /// Cache state
            /// </summary>
            public eCacheStateConstants CacheState { get; set; }

            /// <summary>
            /// Mass Spectrum
            /// </summary>
            public clsMSSpectrum Scan { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="scan"></param>
            /// <param name="cacheState"></param>
            public ScanMemoryCacheItem(clsMSSpectrum scan, eCacheStateConstants cacheState = eCacheStateConstants.NeverCached)
            {
                Scan = scan;
                CacheState = cacheState;
            }
        }

        /// <summary>
        /// Interface to allow choosing between two different in-memory cache implementations
        /// </summary>
        private interface IScanMemoryCache
        {
            /// <summary>
            /// Number of items in the cache
            /// </summary>
            int Count { get; }

            /// <summary>
            /// Limit of items in the cache. Set will throw an exception if new value is smaller than <see cref="Count"/>
            /// </summary>
            int Capacity { get; set; }

            /// <summary>
            /// Retrieve the item for the scan number
            /// </summary>
            /// <param name="scanNumber"></param>
            /// <param name="item"></param>
            /// <returns>true if item available in cache</returns>
            bool GetItem(int scanNumber, out ScanMemoryCacheItem item);

            /// <summary>
            /// Adds an item to the cache. Will not add duplicates. Will remove (and return) the oldest item if necessary.
            /// </summary>
            /// <param name="newItem">Item to add to the cache</param>
            /// <param name="removedItem">Item removed from the cache, or default if remove not needed</param>
            /// <returns>true if <paramref name="removedItem"/> is an item removed from the cache</returns>
            bool AddNew(ScanMemoryCacheItem newItem, out ScanMemoryCacheItem removedItem);

            /// <summary>
            /// Clear all contents
            /// </summary>
            void Clear();
        }

        /// <summary>
        /// Basic in-memory cache option. Less memory than an LRU implementation.
        /// </summary>
        private class MemoryCacheArray : IScanMemoryCache
        {
            private readonly List<ScanMemoryCacheItem> mMemoryCache;
            private readonly Dictionary<int, int> mScanNumberToIndexMap;
            private int mCapacity;
            private int mLastIndex;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="initialCapacity"></param>
            public MemoryCacheArray(int initialCapacity)
            {
                mCapacity = initialCapacity;
                mMemoryCache = new List<ScanMemoryCacheItem>(initialCapacity);
                mScanNumberToIndexMap = new Dictionary<int, int>(initialCapacity);
                mLastIndex = -1;
                Count = 0;
            }

            /// <summary>
            /// Number of spectra in the cache
            /// </summary>
            public int Count { get; private set; }

            /// <summary>
            /// Spectrum cache capacity
            /// </summary>
            public int Capacity
            {
                get => mCapacity;
                set
                {
                    if (value < mMemoryCache.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "capacity was less than the current size.");
                    }

                    mCapacity = value;
                    mMemoryCache.Capacity = value;
                }
            }

            /// <summary>
            /// Get the spectrum for the given scan
            /// </summary>
            /// <param name="scanNumber"></param>
            /// <param name="item"></param>
            /// <returns></returns>
            public bool GetItem(int scanNumber, out ScanMemoryCacheItem item)
            {
                if (!mScanNumberToIndexMap.TryGetValue(scanNumber, out var index))
                {
                    item = null;
                    return false;
                }

                item = mMemoryCache[index];
                return true;
            }

            /// <summary>
            /// Add a new spectrum (and remove the oldest one if the cache is at capacity)
            /// </summary>
            /// <param name="newItem"></param>
            /// <param name="removedItem"></param>
            /// <returns></returns>
            public bool AddNew(ScanMemoryCacheItem newItem, out ScanMemoryCacheItem removedItem)
            {
                var itemRemoved = RemoveOldestItem(out removedItem);
                Add(newItem);

                return itemRemoved;
            }

            /// <summary>
            /// Add an item to the cache.
            /// </summary>
            /// <param name="newItem"></param>
            /// <returns>true if the item could be added, false otherwise (like if the cache is already full)</returns>
            private bool Add(ScanMemoryCacheItem newItem)
            {
                if (Count == mCapacity)
                {
                    return false;
                }

                if (mScanNumberToIndexMap.ContainsKey(newItem.Scan.ScanNumber))
                {
                    return true;
                }

                if (Count >= mCapacity)
                {
                    mLastIndex++;
                    if (mLastIndex >= Count)
                        mLastIndex = 0;

                    //cache.Insert(mLastIndex, newItem);
                    mMemoryCache[mLastIndex] = newItem;
                }
                else
                {
                    mLastIndex = mMemoryCache.Count;
                    mMemoryCache.Add(newItem);
                }

                mScanNumberToIndexMap.Add(newItem.Scan.ScanNumber, mLastIndex);
                Count++;
                return true;
            }

            /// <summary>
            /// Remove the oldest item from the cache, but only if it is full
            /// </summary>
            /// <param name="oldItem">oldest item in the cache</param>
            /// <returns>true if item was removed, false otherwise</returns>
            private bool RemoveOldestItem(out ScanMemoryCacheItem oldItem)
            {
                if (Count < mCapacity)
                {
                    oldItem = null;
                    return false;
                }

                if (mLastIndex == Count - 1)
                {
                    oldItem = mMemoryCache[0];
                }
                else
                {
                    oldItem = mMemoryCache[mLastIndex + 1];
                }

                mScanNumberToIndexMap.Remove(oldItem.Scan.ScanNumber);
                Count--;
                return true;
            }

            /// <summary>
            /// Clear the cache
            /// </summary>
            public void Clear()
            {
                mMemoryCache.Clear();
                mScanNumberToIndexMap.Clear();
                mLastIndex = -1;
            }

            /// <summary>
            /// Report the number of cached spectra, along with the cache capacity
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return string.Format(
                    "In-memory spectrum cache with {0} spectra cached (capacity {1})",
                    mMemoryCache.Count, mCapacity);
            }
        }

        /// <summary>
        /// LRU (Least-Recently-Used) cache implementation
        /// </summary>
        private class MemoryCacheLRU : IScanMemoryCache
        {
            private readonly LinkedList<ScanMemoryCacheItem> mMemoryCache;
            private readonly Dictionary<int, LinkedListNode<ScanMemoryCacheItem>> mScanNumberToNodeMap;
            private int mCapacity;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="initialCapacity"></param>
            public MemoryCacheLRU(int initialCapacity)
            {
                mCapacity = initialCapacity;
                mMemoryCache = new LinkedList<ScanMemoryCacheItem>();
                mScanNumberToNodeMap = new Dictionary<int, LinkedListNode<ScanMemoryCacheItem>>(initialCapacity);
            }

            /// <summary>
            /// Number of spectra in the cache
            /// </summary>
            public int Count => mMemoryCache.Count;

            /// <summary>
            /// Spectrum cache capacity
            /// </summary>
            public int Capacity
            {
                get => mCapacity;
                set
                {
                    if (value < mMemoryCache.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "capacity was less than the current size.");
                    }

                    mCapacity = value;
                }
            }

            /// <summary>
            /// Get the spectrum for the given scan
            /// </summary>
            /// <param name="scanNumber"></param>
            /// <param name="item"></param>
            /// <returns></returns>
            public bool GetItem(int scanNumber, out ScanMemoryCacheItem item)
            {
                if (!mScanNumberToNodeMap.TryGetValue(scanNumber, out var node))
                {
                    item = null;
                    return false;
                }

                item = node.Value;

                // LRU management
                mMemoryCache.Remove(node); // O(1)
                mMemoryCache.AddLast(node); // O(1)

                return true;
            }

            /// <summary>
            /// Add a new spectrum (and remove the oldest one if the cache is at capacity)
            /// </summary>
            /// <param name="newItem"></param>
            /// <param name="removedItem"></param>
            /// <returns></returns>
            public bool AddNew(ScanMemoryCacheItem newItem, out ScanMemoryCacheItem removedItem)
            {
                var itemRemoved = RemoveOldestItem(out removedItem);
                Add(newItem);

                return itemRemoved;
            }

            /// <summary>
            /// Add an item to the cache.
            /// </summary>
            /// <param name="newItem"></param>
            /// <returns>true if the item could be added, false otherwise (like if the cache is already full)</returns>
            private bool Add(ScanMemoryCacheItem newItem)
            {
                if (mMemoryCache.Count == mCapacity)
                {
                    return false;
                }

                if (mScanNumberToNodeMap.ContainsKey(newItem.Scan.ScanNumber))
                {
                    return true;
                }

                var node = mMemoryCache.AddLast(newItem);
                mScanNumberToNodeMap.Add(newItem.Scan.ScanNumber, node);
                return true;
            }

            /// <summary>
            /// Remove the oldest item from the cache, but only if it is full
            /// </summary>
            /// <param name="oldItem">oldest item in the cache</param>
            /// <returns>true if item was removed, false otherwise</returns>
            private bool RemoveOldestItem(out ScanMemoryCacheItem oldItem)
            {
                if (Count < mCapacity)
                {
                    oldItem = null;
                    return false;
                }

                var node = mMemoryCache.First;
                oldItem = node.Value;
                mMemoryCache.RemoveFirst();
                mScanNumberToNodeMap.Remove(node.Value.Scan.ScanNumber);

                return true;
            }

            /// <summary>
            /// Clear the cache
            /// </summary>
            public void Clear()
            {
                mMemoryCache.Clear();
                mScanNumberToNodeMap.Clear();
            }

            /// <summary>
            /// Report the number of cached spectra, along with the cache capacity
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return string.Format(
                    "In-memory least-recently-used spectrum cache with {0} spectra cached (capacity {1})",
                    mMemoryCache.Count, mCapacity);
            }
        }
    }
}
