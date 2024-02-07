using System;
using System.Collections.Generic;
using System.IO;
using MASIC.Data;
using MASIC.DataInput;
using MASIC.Options;

namespace MASIC
{
    /// <summary>
    /// Utilizes a spectrum pool to store mass spectra
    /// </summary>
    public class SpectraCache : MasicEventNotifier, IDisposable
    {
        // Ignore Spelling: uncache, uncached

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cacheOptions"></param>
        /// <param name="instrumentDataFile"></param>
        public SpectraCache(SpectrumCacheOptions cacheOptions, FileInfo instrumentDataFile)
        {
            mCacheOptions = cacheOptions ?? new SpectrumCacheOptions();

            mDirectorySpaceTools = new DirectorySpaceTools();
            RegisterEvents(mDirectorySpaceTools);

            mInstrumentDataFile = instrumentDataFile;

            InitializeSpectraPool();
        }

        /// <summary>
        /// Close the page file and delete spectrum cache files
        /// </summary>
        public void Dispose()
        {
            ClosePageFile();
            DeleteSpectrumCacheFiles();
        }

        private const string SPECTRUM_CACHE_FILE_PREFIX = "$SpecCache";

        private const string SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR = "_Temp";

        private const int SPECTRUM_CACHE_MAX_FILE_AGE_HOURS = 12;

        private enum CacheStateConstants
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
        private IScanMemoryCache mSpectraPool;

        private readonly SpectrumCacheOptions mCacheOptions;

        private readonly DirectorySpaceTools mDirectorySpaceTools;

        private readonly FileInfo mInstrumentDataFile;

        private BinaryReader mPageFileReader;

        private BinaryWriter mPageFileWriter;

        private bool mDirectoryPathValidated;

        private int mMaximumPoolLength;

        /// <summary>
        /// Records the byte offset of the data in the page file for a given scan number
        /// </summary>
        private Dictionary<int, long> mSpectrumByteOffset;

        /// <summary>
        /// Spectrum cache directory path
        /// </summary>
        public string CacheDirectoryPath
        {
            get => mCacheOptions.DirectoryPath;
            set => mCacheOptions.DirectoryPath = value;
        }

        /// <summary>
        /// Number of cache events
        /// </summary>
        public int CacheEventCount { get; private set; }

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
        /// This will be set to true if unable to create the spectrum cache file
        /// </summary>
        public bool PageFileInitializationFailed { get; private set; }

        /// <summary>
        /// Number of times the spectrum was found in the in-memory spectrum pool
        /// </summary>
        public int SpectraPoolHitEventCount { get; private set; }

        /// <summary>
        /// The number of spectra we expect to read, updated to the number cached (to disk)
        /// </summary>
        public int SpectrumCount { get; set; }

        /// <summary>
        /// Number of times a spectrum was loaded from disk and cached in the SpectraPool
        /// </summary>
        public int UnCacheEventCount { get; private set; }

        /// <summary>
        /// Add the directory to the list if its drive letter (or network share) does not match any of the entries in the list
        /// </summary>
        /// <param name="directoryList"></param>
        /// <param name="comparisonDirectory"></param>
        private void AddDirectoryIfNewDrive(List<DirectoryInfo> directoryList, DirectoryInfo comparisonDirectory)
        {
            try
            {
                var comparisonRootPath = Path.GetPathRoot(comparisonDirectory.FullName);

                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var directory in directoryList)
                {
                    var pathRoot = Path.GetPathRoot(directory.FullName);

                    if (pathRoot.Equals(comparisonRootPath, StringComparison.OrdinalIgnoreCase))
                        return;
                }

                directoryList.Add(comparisonDirectory);
            }
            catch (Exception ex)
            {
                ReportError("Error comparing directory root path to directories in list", ex);
            }
        }

        /// <summary>
        /// Checks the spectraPool for available capacity, caching the oldest item if full
        /// </summary>
        private void AddItemToSpectraPool(ScanMemoryCacheItem itemToAdd)
        {
            // Disk caching disabled: expand the size of the in-memory cache
            if (mSpectraPool.Count >= mMaximumPoolLength &&
                mCacheOptions.DiskCachingAlwaysDisabled)
            {
                // The pool is full, but disk caching is disabled, so we have to expand the pool
                var newPoolLength = mMaximumPoolLength + 500;

                var currentPoolLength = Math.Min(mMaximumPoolLength, mSpectraPool.Capacity);
                mMaximumPoolLength = newPoolLength;

                if (newPoolLength > currentPoolLength)
                {
                    mSpectraPool.Capacity = mMaximumPoolLength;
                }
            }

            // Need to cache the spectrum stored at mNextAvailablePoolIndex
            // Removes the oldest spectrum from spectraPool
            if (mSpectraPool.AddNew(itemToAdd, out var cacheItem))
            {
                // An item was removed from the spectraPool. Write it to the disk cache if needed.
                if (cacheItem.CacheState == CacheStateConstants.LoadedFromCache)
                {
                    // Already cached previously, simply reset the slot
                }
                else if (cacheItem.CacheState == CacheStateConstants.NeverCached &&
                         ValidatePageFileIO(true))
                {
                    // Store all the spectra in one large file
                    CacheSpectrumWork(cacheItem.Scan);
                }

                if (cacheItem.CacheState != CacheStateConstants.UnusedSlot)
                {
                    // Reset .ScanNumber, .IonCount, and .CacheState
                    cacheItem.Scan.Clear(0);
                    cacheItem.CacheState = CacheStateConstants.UnusedSlot;

                    CacheEventCount++;
                }
            }
        }

        /// <summary>
        /// Adds spectrum to the spectrum pool
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="scanNumber"></param>
        /// <returns>Index of the spectrum in the pool in targetPoolIndex</returns>
        public bool AddSpectrumToPool(
            MSSpectrum spectrum,
            int scanNumber)
        {
            try
            {
                if (SpectrumCount > CacheSpectraToRetainInMemory + 5 &&
                    !DiskCachingAlwaysDisabled &&
                    ValidatePageFileIO(true))
                {
                    // Store all the spectra in one large file
                    CacheSpectrumWork(spectrum);

                    CacheEventCount++;
                    return true;
                }

                if (PageFileInitializationFailed)
                    return false;

                if (mSpectraPool.GetItem(scanNumber, out var cacheItem))
                {
                    // Replace the spectrum data with spectrum
                    cacheItem.Scan.ReplaceData(spectrum, scanNumber);
                    cacheItem.CacheState = CacheStateConstants.NeverCached;
                }
                else
                {
                    // Need to add the spectrum
                    AddItemToSpectraPool(new ScanMemoryCacheItem(spectrum, CacheStateConstants.NeverCached));
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex.Message, ex);
                return false;
            }
        }

        private void CacheSpectrumWork(MSSpectrum spectrumToCache)
        {
            const int MAX_RETRIES = 3;

            // See if the given spectrum is already present in the page file
            var scanNumber = spectrumToCache.ScanNumber;

            if (mSpectrumByteOffset.ContainsKey(scanNumber))
            {
                // Page file already contains the given scan;
                // re-cache the item. for some reason we have updated peaks.
                mSpectrumByteOffset.Remove(scanNumber);
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
        private string ConstructCachedSpectrumPath(bool ignoreInstrumentDataFileSize = false)
        {
            if (!ValidateCachedSpectrumDirectory(ignoreInstrumentDataFileSize))
            {
                return string.Empty;
            }

            var randomNumber = GetRandom(1, 9999);

            var workingDirectory = new DirectoryInfo(".");

            // Create the cache file name, using both a timestamp and a random number between 1 and 9999
            var baseName = string.Format("{0}_{1}{2}{3}{4}{5}_{6}",
                SPECTRUM_CACHE_FILE_PREFIX,
                DateTime.UtcNow.Hour ,DateTime.UtcNow.Minute ,DateTime.UtcNow.Second ,DateTime.UtcNow.Millisecond,
                randomNumber,
                workingDirectory.Name);

            var fileName = baseName + SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR + ".bin";

            return Path.Combine(mCacheOptions.DirectoryPath, fileName);
        }

        /// <summary>
        /// Looks for and deletes the spectrum cache files created by this instance of MASIC
        /// Additionally, looks for and deletes spectrum cache files with modification dates more than SPECTRUM_CACHE_MAX_FILE_AGE_HOURS from the present
        /// </summary>
        public void DeleteSpectrumCacheFiles()
        {
            var dateTimeThreshold = DateTime.UtcNow.Subtract(new TimeSpan(SPECTRUM_CACHE_MAX_FILE_AGE_HOURS, 0, 0));

            try
            {
                // Delete the cached files for this instance of clsMasic
                if (!string.IsNullOrWhiteSpace(mCacheOptions.CacheFilePath))
                {
                    var charIndex = mCacheOptions.CacheFilePath.IndexOf(SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR, StringComparison.Ordinal);

                    if (charIndex < 0)
                    {
                        ReportError("charIndex was less than 0; this is unexpected in DeleteSpectrumCacheFiles");
                        return;
                    }

                    var basePath = mCacheOptions.CacheFilePath.Substring(0, charIndex);

                    foreach (var cacheFile in Directory.GetFiles(mCacheOptions.DirectoryPath, Path.GetFileName(basePath) + "*"))
                    {
                        File.Delete(cacheFile);
                    }
                }
            }
            catch (Exception ex)
            {
                // Report this error, but otherwise ignore it
                ReportWarning("Error deleting cached spectrum files for this task: " + ex.Message);
            }

            // Now look for old spectrum cache files
            try
            {
                const string filePathMatch = SPECTRUM_CACHE_FILE_PREFIX + "*" + SPECTRUM_CACHE_FILE_BASENAME_TERMINATOR + "*";

                var cacheFilePath = ConstructCachedSpectrumPath(true);

                if (string.IsNullOrWhiteSpace(cacheFilePath))
                    return;

                var spectrumFile = new FileInfo(cacheFilePath);

                if (spectrumFile.Directory == null)
                {
                    ReportWarning("Unable to determine the spectrum cache directory path in DeleteSpectrumCacheFiles; this is unexpected");
                    return;
                }

                foreach (var candidateFile in spectrumFile.Directory.GetFiles(filePathMatch))
                {
                    if (candidateFile.LastWriteTimeUtc < dateTimeThreshold)
                    {
                        candidateFile.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportWarning("Error deleting old cached spectrum files: " + ex.Message);
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
        public bool GetSpectrum(int scanNumber, out MSSpectrum spectrum, bool canSkipPool = true)
        {
            try
            {
                if (mSpectraPool.GetItem(scanNumber, out var cacheItem))
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

                spectrum = new MSSpectrum(scanNumber);
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

            PageFileInitializationFailed = false;

            ClosePageFile();

            if (mSpectraPool == null || mSpectraPool.Capacity < mMaximumPoolLength)
            {
                if (mCacheOptions.DiskCachingAlwaysDisabled)
                {
                    mSpectraPool = new MemoryCacheArray(mMaximumPoolLength);
                }
                else
                {
                    mSpectraPool = new MemoryCacheLRU(mMaximumPoolLength);
                }
            }
            else
            {
                mSpectraPool.Clear();
            }
        }

        private static int GetRandom(int minValue, int maxValue)
        {
            // Use the hash code of the full path of the working directory as the seed for the random number generator
            var workingDirectory = new DirectoryInfo(".");

            var rand = new Random(workingDirectory.FullName.GetHashCode());

            return rand.Next(minValue, maxValue);
        }

        /// <summary>
        /// Load the spectrum from disk and cache in SpectraPool
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="msSpectrum">Output: spectrum for scan number</param>
        /// <returns>True if successfully uncached, false if an error</returns>
        private bool UnCacheSpectrum(int scanNumber, out MSSpectrum msSpectrum)
        {
            // Make sure we have a valid object
            var cacheItem = new ScanMemoryCacheItem(new MSSpectrum(scanNumber), CacheStateConstants.LoadedFromCache);

            msSpectrum = cacheItem.Scan;

            // Uncache the spectrum from disk
            if (!UnCacheSpectrumWork(scanNumber, msSpectrum))
            {
                // Scan not found; use a blank mass spectrum
                // Its cache state will be set to LoadedFromCache, which is OK, since we don't need to cache it to disk
                msSpectrum.Clear(scanNumber);
            }

            cacheItem.CacheState = CacheStateConstants.LoadedFromCache;
            AddItemToSpectraPool(cacheItem);

            return true;
        }

        /// <summary>
        /// Load the spectrum from disk and cache in SpectraPool
        /// </summary>
        /// <param name="scanNumber">Scan number to load</param>
        /// <param name="msSpectrum"><see cref="MSSpectrum"/> object to store data into; supplying 'null' is an exception.</param>
        /// <returns>True if successfully uncached, false if an error</returns>
        private bool UnCacheSpectrumWork(int scanNumber, MSSpectrum msSpectrum)
        {
            var success = false;
            msSpectrum.Clear();

            // All the spectra are stored in one large file
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

                // ReSharper disable once MustUseReturnValue
                mPageFileReader.Read(byteBuffer, 0, byteCount);
                Buffer.BlockCopy(byteBuffer, 0, dblBuffer, 0, byteCount);
                msSpectrum.IonsMZ.AddRange(dblBuffer);

                // ReSharper disable once MustUseReturnValue
                mPageFileReader.Read(byteBuffer, 0, byteCount);
                Buffer.BlockCopy(byteBuffer, 0, dblBuffer, 0, byteCount);
                msSpectrum.IonsIntensity.AddRange(dblBuffer);

                UnCacheEventCount++;
                success = true;
            }

            return success;
        }

        private bool ValidateCachedSpectrumDirectory(bool ignoreInstrumentDataFileSize)
        {
            if (string.IsNullOrWhiteSpace(mCacheOptions.DirectoryPath))
            {
                // Need to define the spectrum caching directory path
                mCacheOptions.DirectoryPath = Path.GetTempPath();
                mDirectoryPathValidated = false;
            }

            if (mDirectoryPathValidated)
                return true;

            try
            {
                var directoriesToCheck = new List<DirectoryInfo>();

                if (!Path.IsPathRooted(mCacheOptions.DirectoryPath))
                {
                    mCacheOptions.DirectoryPath = Path.Combine(Path.GetTempPath(), mCacheOptions.DirectoryPath);
                }

                directoriesToCheck.Add(new DirectoryInfo(mCacheOptions.DirectoryPath));

                // Assume that the cache file will be roughly the same size as the instrument data file
                int minFreeSpaceMB;

                if (ignoreInstrumentDataFileSize)
                {
                    minFreeSpaceMB = 0;
                }
                else if (mInstrumentDataFile == null)
                {
                    minFreeSpaceMB = 500;
                }
                else
                {
                    minFreeSpaceMB = Math.Max(500, (int)Math.Round(DirectorySpaceTools.BytesToMB(mInstrumentDataFile.Length)));

                    AddDirectoryIfNewDrive(directoriesToCheck, mInstrumentDataFile.Directory);
                }

                AddDirectoryIfNewDrive(directoriesToCheck, new DirectoryInfo("."));

                var warningMessages = new List<string>();

                foreach (var cacheDirectory in directoriesToCheck)
                {
                    if (!cacheDirectory.Exists)
                    {
                        cacheDirectory.Create();
                        cacheDirectory.Refresh();
                    }

                    var validFreeSpace = mDirectorySpaceTools.ValidateFreeDiskSpace(
                        "Spectrum cache directory on the " + Path.GetPathRoot(cacheDirectory.FullName).TrimEnd('\\'),
                        cacheDirectory.FullName,
                        minFreeSpaceMB,
                        false,
                        out var errorMessage);

                    if (validFreeSpace)
                    {
                        mCacheOptions.DirectoryPath = cacheDirectory.FullName;
                        mDirectoryPathValidated = true;
                        return true;
                    }

                    warningMessages.Add(errorMessage);
                }

                OnErrorEvent("Unable to find a drive with sufficient free space to store the spectrum cache file");

                foreach (var message in warningMessages)
                {
                    OnWarningEvent(message);
                }

                return false;
            }
            catch (Exception ex)
            {
                // Error defining .DirectoryPath
                ReportError("Error validating spectrum cache directory: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validates that we can read and write from a Page file
        /// Opens the page file reader and writer if not yet opened
        /// </summary>
        /// <param name="createIfUninitialized"></param>
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

                if (string.IsNullOrWhiteSpace(cacheFilePath))
                {
                    PageFileInitializationFailed = true;
                    return false;
                }

                // Initialize the binary writer and create the file
                // The initialization process sometimes fails with error "The process cannot access the file '...' because it is being used by another process"
                // Use a while loop to try up to 3 times to initialize the writer

                var iteration = 0;

                while (true)
                {
                    iteration++;
                    bool success;
                    string exceptionMessage;

                    try
                    {
                        mPageFileWriter = new BinaryWriter(new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));
                        exceptionMessage = string.Empty;
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        ReportWarning(string.Format(
                            "Error initializing the binary writer in ValidatePageFileIO (iteration {0}): {1}",
                            iteration, ex.Message));

                        exceptionMessage = ex.Message;
                        success = false;
                    }

                    if (success)
                        break;

                    if (iteration < 3)
                    {
                        // Sleep between 250 and 1000 milliseconds
                        var sleepDelayMsec = GetRandom(250, 1000);
                        System.Threading.Thread.Sleep(sleepDelayMsec);

                        continue;
                    }

                    ReportError(string.Format("Error initializing the binary writer in ValidatePageFileIO: {0}", exceptionMessage));

                    return false;
                }

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

                mCacheOptions.CacheFilePath = cacheFilePath;

                return true;
            }
            catch (Exception ex)
            {
                PageFileInitializationFailed = true;
                ReportError("Error in ValidatePageFileIO: " + ex.Message, ex);
                ReportWarning(PRISM.StackTraceFormatter.GetExceptionStackTrace(ex));
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
            public CacheStateConstants CacheState { get; set; }

            /// <summary>
            /// Mass Spectrum
            /// </summary>
            public MSSpectrum Scan { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="scan"></param>
            /// <param name="cacheState"></param>
            public ScanMemoryCacheItem(MSSpectrum scan, CacheStateConstants cacheState = CacheStateConstants.NeverCached)
            {
                Scan = scan;
                CacheState = cacheState;
            }

            public override string ToString()
            {
                return string.Format("Scan {0}, {1}", Scan.ScanNumber, CacheState);
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
            /// <returns>True if item available in cache</returns>
            bool GetItem(int scanNumber, out ScanMemoryCacheItem item);

            /// <summary>
            /// Adds an item to the cache. Will not add duplicates. Will remove (and return) the oldest item if necessary.
            /// </summary>
            /// <param name="newItem">Item to add to the cache</param>
            /// <param name="removedItem">Item removed from the cache, or default if remove not needed</param>
            /// <returns>True if <paramref name="removedItem"/> is an item removed from the cache</returns>
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
            /// <returns>True if the item could be added, false otherwise (like if the cache is already full)</returns>
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
            /// <returns>True if item was removed, false otherwise</returns>
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
            /// <returns>True if the item could be added, false otherwise (like if the cache is already full)</returns>
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
            /// <returns>True if item was removed, false otherwise</returns>
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
            public override string ToString()
            {
                return string.Format(
                    "In-memory least-recently-used spectrum cache with {0} spectra cached (capacity {1})",
                    mMemoryCache.Count, mCapacity);
            }
        }
    }
}
