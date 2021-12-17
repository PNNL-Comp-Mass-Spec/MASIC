using System;

namespace MASIC.Options
{
    /// <summary>
    /// Spectrum caching options
    /// </summary>
    public class SpectrumCacheOptions
    {
        /// <summary>
        /// If True, spectra will never be cached to disk and the spectra pool will consequently be increased as needed
        /// </summary>
        public bool DiskCachingAlwaysDisabled { get; set; }

        /// <summary>
        /// Path to the cache directory (can be relative or absolute, aka rooted); if empty, the user's AppData directory is used
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// Number of spectra to keep in the in-memory cache
        /// </summary>
        public int SpectraToRetainInMemory
        {
            get => mSpectraToRetainInMemory;
            set
            {
                if (value < 100)
                    value = 100;
                mSpectraToRetainInMemory = value;
            }
        }

        /// <summary>
        /// Required minimum free system memory size, in MB
        /// </summary>
        [Obsolete("Legacy parameter; no longer used")]
        public float MinimumFreeMemoryMB { get; set; }

        /// <summary>
        /// Allowed maximum free system memory size, in MB
        /// </summary>
        [Obsolete("Legacy parameter; no longer used")]
        public float MaximumMemoryUsageMB { get; set; }

        private int mSpectraToRetainInMemory = 1000;

        /// <summary>
        /// Reset the options
        /// </summary>
        public void Reset()
        {
            var defaultOptions = SpectraCache.GetDefaultCacheOptions();

            DiskCachingAlwaysDisabled = defaultOptions.DiskCachingAlwaysDisabled;
            DirectoryPath = defaultOptions.DirectoryPath;
            SpectraToRetainInMemory = defaultOptions.SpectraToRetainInMemory;
        }

        /// <summary>
        /// Show the maximum number of spectra to cache, and the cache directory
        /// </summary>
        public override string ToString()
        {
            return "Cache up to " + SpectraToRetainInMemory + " in directory " + DirectoryPath;
        }
    }
}
