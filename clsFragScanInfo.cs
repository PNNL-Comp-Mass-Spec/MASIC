
namespace MASIC
{
    public class clsFragScanInfo
    {
        /// <summary>
    /// Pointer to an entry in the ParentIons() array; -1 if undefined
    /// </summary>
        public int ParentIonInfoIndex { get; set; }

        /// <summary>
    /// Parent ion m/z value
    /// </summary>
    /// <returns></returns>
        public double ParentIonMz { get; private set; }

        /// <summary>
    /// The nth fragmentation scan after an MS1 scan
    /// Computed as EventNumber - 1 since the first MS2 scan after a MS1 scan typically has EventNumber = 2
    /// </summary>
        public int FragScanNumber { get; set; }

        /// <summary>
    /// 2 for MS/MS, 3 for MS/MS/MS
    /// </summary>
        public int MSLevel { get; set; }

        /// <summary>
    /// Collision mode
    /// </summary>
        public string CollisionMode { get; set; }

        /// <summary>
    /// Interference score: fraction of observed peaks that are from the precursor
    /// Larger is better, with a max of 1 and minimum of 0
    /// 1 means all peaks are from the precursor
    /// </summary>
        public double InterferenceScore { get; set; }

        /// <summary>
    /// Constructor
    /// </summary>
        public clsFragScanInfo(double parentIonMzValue)
        {
            // -1 means undefined; only used for fragmentation scans
            ParentIonInfoIndex = -1;
            ParentIonMz = parentIonMzValue;
            CollisionMode = string.Empty;
        }

        public override string ToString()
        {
            return "Parent Ion " + ParentIonInfoIndex;
        }
    }
}