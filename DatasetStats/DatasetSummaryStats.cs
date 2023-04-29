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
        /// Keys are of the form "ScanTypeName::###::ScanFilterText"
        /// Values are the number of scans with the given scan type and scan filter
        /// Example keys:
        ///   HMS::###::FTMS + p NSI Full ms
        ///   HMSn::###::FTMS + p NSI d Full ms2 0@hcd25.00
        ///   MS::###::ITMS + c ESI Full ms
        ///   MSn::###::ITMS + p ESI d Z ms
        ///   MSn::###::ITMS + c ESI d Full ms2 @cid35.00
        /// </remarks>
        public Dictionary<string, int> ScanTypeStats { get; }

        /// <summary>
        /// Keeps track of each ScanType in the dataset, along with the isolation window width(s) for the scan type
        /// </summary>
        /// <remarks>
        /// Keys are of the form "ScanTypeName::###::ScanFilterText"
        /// Values are a sorted set of isolation window widths (typically 0 or -1 for MS1 spectra)
        /// Example keys:
        ///   HMS::###::FTMS + p NSI Full ms
        ///   HMSn::###::FTMS + p NSI d Full ms2 0@hcd25.00
        ///   MS::###::ITMS + c ESI Full ms
        ///   MSn::###::ITMS + p ESI d Z ms
        ///   MSn::###::ITMS + c ESI d Full ms2 @cid35.00
        /// </remarks>
        public Dictionary<string, SortedSet<double>> ScanTypeWindowWidths { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DatasetSummaryStats()
        {
            MSStats = new SummaryStatDetails();
            MSnStats = new SummaryStatDetails();
            ScanTypeStats = new Dictionary<string, int>();
            ScanTypeWindowWidths = new Dictionary<string, SortedSet<double>>();
            Clear();
        }

        /// <summary>
        /// Reset stats
        /// </summary>
        public void Clear()
        {
            DIAScanCount = 0;
            ElutionTimeMax = 0;
            MSStats.Clear();
            MSnStats.Clear();
            ScanTypeStats.Clear();
            ScanTypeWindowWidths.Clear();
        }
    }
}
