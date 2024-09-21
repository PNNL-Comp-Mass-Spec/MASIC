using System.Collections.Generic;

namespace MASIC.Data
{
    /// <summary>
    /// Container for reporter ion data associated with a scan
    /// </summary>
    internal class ReporterIonStats
    {
        // Ignore Spelling: MASIC

        public List<string> DataColumns { get; }

        public int MSLevel { get; set; }

        public double ParentIonMz { get; set; }

        public int ParentScan { get; set; }

        public int ReporterIonIntensityStartIndex { get; set; }

        public int ReporterIonIntensityEndIndex { get; set; }

        public int ScanNumber { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scanNumber">Scan number</param>
        public ReporterIonStats(int scanNumber)
        {
            DataColumns = new List<string>();
            ScanNumber = scanNumber;
        }
    }
}
