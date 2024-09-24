using System.Collections.Generic;

namespace MASIC.Data
{
    /// <summary>
    /// Container for binned intensity values
    /// </summary>
    /// <remarks>Used when comparing spectra</remarks>
    public class BinnedData
    {
        // Ignore Spelling: MASIC

        /// <summary>
        /// X value of the first bin
        /// </summary>
        public float BinnedDataStartX { get; set; }

        /// <summary>
        /// Bin size
        /// </summary>
        public float BinSize { get; set; }

        /// <summary>
        /// Number of bins in BinnedIntensities
        /// </summary>
        public int BinCount => BinnedIntensities.Count;

        /// <summary>
        /// List of binned intensities; First bin starts at BinnedDataStartX
        /// </summary>
        public readonly List<float> BinnedIntensities;

        /// <summary>
        /// List of binned intensity offsets; First bin starts at BinnedDataStartX + BinSize/2
        /// </summary>
        public readonly List<float> BinnedIntensitiesOffset;

        /// <summary>
        /// Constructor
        /// </summary>
        public BinnedData()
        {
            BinnedIntensities = new List<float>();
            BinnedIntensitiesOffset = new List<float>();
        }

        /// <summary>
        /// Show the bin count, bin size, and start x
        /// </summary>
        public override string ToString()
        {
            return "BinCount: " + BinCount + ", BinSize: " + BinSize.ToString("0.0") + ", StartX: " + BinnedDataStartX.ToString("0.0");
        }
    }
}
