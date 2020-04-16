using System.Collections.Generic;

namespace MASIC
{
    public class clsBinnedData
    {
        public float BinnedDataStartX { get; set; }
        public float BinSize { get; set; }

        /// <summary>
        /// Number of bins in BinnedIntensities
        /// </summary>
        /// <returns></returns>
        public int BinCount
        {
            get
            {
                return BinnedIntensities.Count;
            }
        }

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
        public clsBinnedData()
        {
            BinnedIntensities = new List<float>();
            BinnedIntensitiesOffset = new List<float>();
        }

        public override string ToString()
        {
            return "BinCount: " + BinCount + ", BinSize: " + BinSize.ToString("0.0") + ", StartX: " + BinnedDataStartX.ToString("0.0");
        }
    }
}