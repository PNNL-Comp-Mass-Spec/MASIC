using System;

namespace MASIC
{
    public class clsProcessingStats
    {
        public float PeakMemoryUsageMB { get; set; }
        public float TotalProcessingTimeAtStart { get; set; }
        public int CacheEventCount { get; set; }
        public int UnCacheEventCount { get; set; }

        public DateTime FileLoadStartTime { get; set; }
        public DateTime FileLoadEndTime { get; set; }

        public DateTime ProcessingStartTime { get; set; }
        public DateTime ProcessingEndTime { get; set; }

        public float MemoryUsageMBAtStart { get; set; }
        public float MemoryUsageMBDuringLoad { get; set; }
        public float MemoryUsageMBAtEnd { get; set; }

        public override string ToString()
        {
            return "PeakMemoryUsageMB: " + PeakMemoryUsageMB.ToString("0.0") + ", " +
                "CacheEventCount: " + CacheEventCount + ", " +
                "UnCacheEventCount: " + UnCacheEventCount;
        }
    }
}
