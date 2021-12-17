using System.Collections.Generic;
using ThermoRawFileReader;

namespace MASIC
{
    /// <summary>
    /// Container for MRM scan info
    /// </summary>
    public class clsMRMScanInfo
    {
        /// <summary>
        /// Parent ion m/z
        /// </summary>
        public double ParentIonMZ { get; set; }

        /// <summary>
        /// List of mass ranges monitored by the first quadrupole
        /// </summary>
        public int MRMMassCount { get; set; }

        /// <summary>
        /// Daughter m/z values monitored for this parent m/z
        /// </summary>
        public List<MRMMassRangeType> MRMMassList { get; set; }

        /// <summary>
        /// Number of spectra that used these MRM search values
        /// </summary>
        public int ScanCount { get; set; }

        /// <summary>
        /// Parent ion index
        /// </summary>
        public int ParentIonInfoIndex { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsMRMScanInfo()
        {
            MRMMassList = new List<MRMMassRangeType>();
            ScanCount = 0;
        }
    }
}
