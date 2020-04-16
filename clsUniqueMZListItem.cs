using System.Collections.Generic;

namespace MASIC
{
    public class clsUniqueMZListItem
    {
        /// <summary>
        /// Average m/z
        /// </summary>
        public double MZAvg { get; set; }

        /// <summary>
        /// Highest intensity value of the similar parent ions
        /// </summary>
        public double MaxIntensity { get; set; }

        /// <summary>
        /// Largest peak intensity value of the similar parent ions
        /// </summary>
        public double MaxPeakArea { get; set; }

        /// <summary>
        /// Scan number of the parent ion with the highest intensity
        /// </summary>
        public int ScanNumberMaxIntensity { get; set; }

        /// <summary>
        /// Elution time of the parent ion with the highest intensity
        /// </summary>
        public float ScanTimeMaxIntensity { get; set; }

        /// <summary>
        /// Pointer to an entry in scanList.ParentIons
        /// </summary>
        public int ParentIonIndexMaxIntensity { get; set; }

        /// <summary>
        /// Pointer to an entry in scanList.ParentIons
        /// </summary>
        public int ParentIonIndexMaxPeakArea { get; set; }

        /// <summary>
        /// Number of items in MatchIndices
        /// </summary>
        /// <returns></returns>
        public int MatchCount => MatchIndices.Count;

        /// <summary>
        /// Pointers to entries in scanList.ParentIons
        /// </summary>
        public readonly List<int> MatchIndices;

        /// <summary>
        /// Constructor
        /// </summary>
        public clsUniqueMZListItem()
        {
            MatchIndices = new List<int>();
        }

        public override string ToString()
        {
            return "m/z avg: " + MZAvg + ", MatchCount: " + MatchIndices.Count;
        }
    }
}