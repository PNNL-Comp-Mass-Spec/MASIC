using System.Collections.Generic;

namespace MASIC
{
    /// <summary>
    /// Container for a single parent ion
    /// </summary>
    public class clsParentIonInfo
    {
        /// <summary>
        /// Parent ion m/z value
        /// </summary>
        public double MZ { get; private set; }

        /// <summary>
        /// Survey scan that this parent ion was observed in; Pointer to entry in .SurveyScans()
        /// For custom SIC values, this is the closest survey scan to .ScanCenter
        /// </summary>
        public int SurveyScanIndex { get; set; }

        /// <summary>
        /// Scan number of the peak apex for this parent ion; originally the scan number of the first fragmentation spectrum; later updated to the scan number of the SIC data Peak apex; possibly updated later in FindSimilarParentIons()
        /// </summary>
        public int OptimalPeakApexScanNumber { get; set; }

        /// <summary>
        /// If OptimalPeakApexScanNumber is inherited from another parent ion, this is set to that parent ion's index; otherwise, this is -1
        /// </summary>
        public int PeakApexOverrideParentIonIndex { get; set; }

        /// <summary>
        /// Pointers to entries in .FragScans(); for custom SIC values, points to the next MS2 scan that occurs after the ScanCenter search value
        /// </summary>
        public readonly List<int> FragScanIndices;

        /// <summary>
        /// SIC Stats for this parent ion
        /// </summary>
        public clsSICStats SICStats { get; set; }

        /// <summary>
        /// True if this is a custom SIC-based parent ion
        /// </summary>
        public bool CustomSICPeak { get; set; }

        /// <summary>
        /// Only applies to custom SIC-based parent ions
        /// </summary>
        public string CustomSICPeakComment { get; set; }

        /// <summary>
        /// Only applies to custom SIC-based parent ions
        /// </summary>
        public double CustomSICPeakMZToleranceDa { get; set; }

        /// <summary>
        /// Only applies to custom SIC-based parent ions
        /// </summary>
        public float CustomSICPeakScanOrAcqTimeTolerance { get; set; }

        /// <summary>
        /// Only applicable to MRM scans
        /// </summary>
        public double MRMDaughterMZ { get; set; }

        /// <summary>
        /// Only applicable to MRM scans
        /// </summary>
        public double MRMToleranceHalfWidth { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parentIonMz">Parent ion m/z value</param>
        public clsParentIonInfo(double parentIonMz)
        {
            FragScanIndices = new List<int>();
            MZ = parentIonMz;
            SICStats = new clsSICStats();
        }

        /// <summary>
        /// Update the parent ion m/z value
        /// </summary>
        /// <param name="parentIonMz"></param>
        public void UpdateMz(double parentIonMz)
        {
            MZ = parentIonMz;
        }

        /// <summary>
        /// Show the parent ion m/z value
        /// </summary>
        public override string ToString()
        {
            if (CustomSICPeak)
            {
                return "m/z " + MZ.ToString("0.00") + " (Custom SIC peak)";
            }

            return "m/z " + MZ.ToString("0.00");
        }
    }
}
