using System;

namespace MASIC.Options
{
    public class SpectrumCacheOptions
    {
        #region "Properties"

        /// <summary>
        /// If True, then spectra will never be cached to disk and the spectra pool will consequently be increased as needed
        /// </summary>
        public bool DiskCachingAlwaysDisabled { get; set; }

        /// <summary>
        /// Path to the cache directory (can be relative or absolute, aka rooted); if empty, then the user's AppData directory is used
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

        [Obsolete("Legacy parameter; no longer used")]
        public float MinimumFreeMemoryMB { get; set; }

        [Obsolete("Legacy parameter; no longer used")]
        public float MaximumMemoryUsageMB { get; set; }

        #endregion

        #region "Classwide variables"

        private int mSpectraToRetainInMemory = 1000;

        #endregion

        /// <summary>
        /// Reset the options
        /// </summary>
        public void Reset()
        {
            var defaultOptions = clsSpectraCache.GetDefaultCacheOptions();

            DiskCachingAlwaysDisabled = defaultOptions.DiskCachingAlwaysDisabled;
            DirectoryPath = defaultOptions.DirectoryPath;
            SpectraToRetainInMemory = defaultOptions.SpectraToRetainInMemory;
        }

        /// <summary>
        /// Show the maximum number of spectra to cache, and the cache directory
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Cache up to " + SpectraToRetainInMemory + " in directory " + DirectoryPath;
        }
    }
}
