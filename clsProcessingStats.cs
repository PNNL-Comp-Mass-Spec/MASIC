using System;

namespace MASIC
{
    /// <summary>
    /// Container for tracking processing stats
    /// </summary>
    public class clsProcessingStats
    {

        /// <summary>
        /// Peak memory usage, in MB
        /// </summary>
        public float PeakMemoryUsageMB { get; set; }

        /// <summary>
        /// Total processor time at the start of processing, in seconds
        /// </summary>
        public float TotalProcessingTimeAtStart { get; set; }

        /// <summary>
        /// Spectrum cache event count
        /// </summary>
        public int CacheEventCount { get; set; }

        /// <summary>
        /// Spectrum uncache event count
        /// </summary>
        public int UnCacheEventCount { get; set; }

        /// <summary>
        /// Spectra pool hit event count
        /// </summary>
        public int SpectraPoolHitEventCount { get; set; }

        /// <summary>
        /// Time when mass spec data loading starts
        /// </summary>
        public DateTime FileLoadStartTime { get; set; }

        /// <summary>
        /// Time when mass spec data loading has finished
        /// </summary>
        public DateTime FileLoadEndTime { get; set; }

        /// <summary>
        /// Time when data processing starts
        /// </summary>
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>
        /// Time when data processing ends
        /// </summary>
        public DateTime ProcessingEndTime { get; set; }

        /// <summary>
        /// MASIC memory usage at the start, in MB
        /// </summary>
        public float MemoryUsageMBAtStart { get; set; }

        /// <summary>
        /// Peak MASIC memory usage while loading data
        /// </summary>
        public float MemoryUsageMBDuringLoad { get; set; }

        /// <summary>
        /// MASIC memory usage at the end, in MB
        /// </summary>
        public float MemoryUsageMBAtEnd { get; set; }

        /// <summary>
        /// Show peak memory usage, cache event count, and uncache event count
        /// </summary>
        public override string ToString()
        {
            return "PeakMemoryUsageMB: " + PeakMemoryUsageMB.ToString("0.0") + ", " +
                "CacheEventCount: " + CacheEventCount + ", " +
                "UnCacheEventCount: " + UnCacheEventCount;
        }
    }
}
