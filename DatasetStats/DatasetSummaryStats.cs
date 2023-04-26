using System.Collections.Generic;

namespace MASIC.DatasetStats
{
    /// <summary>
    /// Container for dataset summary stats
    /// </summary>
    public class DatasetSummaryStats
    {
        /// <summary>
        /// Number of DIA spectra
        /// </summary>
        public int DIAScanCount { get; set; }

        /// <summary>
        /// Maximum elution time (retention time, in minutes
        /// </summary>
        public double ElutionTimeMax { get; set; }

        /// <summary>
        /// MS1 stats container
        /// </summary>
        public SummaryStatDetails MSStats { get; }

        /// <summary>
        /// MSn stats container
        /// </summary>
        public SummaryStatDetails MSnStats { get; }

        /// <summary>
        /// Keeps track of each ScanType in the dataset, along with the number of scans of this type
        /// </summary>
        /// <remarks>
        /// Examples
        /// FTMS + p NSI Full ms
        /// ITMS + c ESI Full ms
        /// ITMS + p ESI d Z ms
        /// ITMS + c ESI d Full ms2 @cid35.00
        /// </remarks>
        public Dictionary<string, int> ScanTypeStats { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DatasetSummaryStats()
        {
            MSStats = new SummaryStatDetails();
            MSnStats = new SummaryStatDetails();
            ScanTypeStats = new Dictionary<string, int>();
            Clear();
        }

        /// <summary>
        /// Reset data to defaults
        /// </summary>
        public void Clear()
        {
            DIAScanCount = 0;
            ElutionTimeMax = 0;
            MSStats.Clear();
            MSnStats.Clear();
            ScanTypeStats.Clear();
        }
    }
}
