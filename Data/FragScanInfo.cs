using System.Collections.Generic;

namespace MASIC.Data
{
    /// <summary>
    /// Information about a single fragmentation scan (spectrum)
    /// </summary>
    public class FragScanInfo
    {
        // Ignore Spelling: frag, MASIC

        /// <summary>
        /// Collision mode
        /// </summary>
        public string CollisionMode { get; set; }

        /// <summary>
        /// The nth fragmentation scan after an MS1 scan
        /// Computed as EventNumber - 1 since the first MS2 scan after a MS1 scan typically has EventNumber = 2
        /// </summary>
        public int FragScanNumber { get; set; }

        /// <summary>
        /// Interference score: fraction of observed peaks that are from the precursor
        /// Larger is better, with a max of 1 and minimum of 0
        /// 1 means all peaks are from the precursor
        /// </summary>
        public double InterferenceScore { get; set; }

        /// <summary>
        /// 2 for MS/MS, 3 for MS/MS/MS
        /// </summary>
        public int MSLevel { get; set; }

        /// <summary>
        /// Pointer to an entry in the ParentIons() array; -1 if undefined
        /// </summary>
        public int ParentIonInfoIndex { get; set; }

        /// <summary>
        /// Parent ion m/z value
        /// </summary>
        public double ParentIonMz { get; }

        /// <summary>
        /// List of parent ion m/z values
        /// </summary>
        /// <remarks>
        /// For MS2 spectra, will only have ParentIonMz
        /// For MS3 spectra, will list each isolated parent ion, starting with the MS2 scan
        /// </remarks>
        public List<double> ParentIons { get; }

        /// <summary>
        /// For MS2 scans, the scan number of the previous MS1 scan
        /// For MS3 scans, the scan number of the MS2 scan that this MS3 scan is related to
        /// </summary>
        public int ParentScan { get; }

        /// <summary>
        /// Constructor for MS2 scans
        /// </summary>
        public FragScanInfo(int parentScan, double parentIonMzValue)
        {
            CollisionMode = string.Empty;
            ParentScan = parentScan;

            // -1 means undefined; only used for fragmentation scans
            ParentIonInfoIndex = -1;
            ParentIonMz = parentIonMzValue;
            ParentIons = new List<double> { ParentIonMz };
        }

        /// <summary>
        /// Constructor for MS3 scans
        /// </summary>
        public FragScanInfo(int parentScan, IReadOnlyList<double> parentIons)
        {
            CollisionMode = string.Empty;
            ParentScan = parentScan;

            // -1 means undefined; only used for fragmentation scans
            ParentIonInfoIndex = -1;

            ParentIons = new List<double>();

            if (parentIons.Count == 0)
                return;

            ParentIonMz = parentIons[0];
            ParentIons.AddRange(parentIons);
        }

        /// <summary>
        /// Show the parent ion index
        /// </summary>
        public override string ToString()
        {
            return "Parent Ion " + ParentIonInfoIndex;
        }
    }
}
