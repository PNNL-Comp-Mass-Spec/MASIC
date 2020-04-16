using System.Collections.Generic;

namespace MASIC.DatasetStats
{
    public class DatasetSummaryStats
    {
        public double ElutionTimeMax { get; set; }
        public SummaryStatDetails MSStats { get; private set; }
        public SummaryStatDetails MSnStats { get; private set; }

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
        public Dictionary<string, int> ScanTypeStats { get; private set; }

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

        public void Clear()
        {
            ElutionTimeMax = 0;
            MSStats.Clear();
            MSnStats.Clear();
            ScanTypeStats.Clear();
        }
    }
}
