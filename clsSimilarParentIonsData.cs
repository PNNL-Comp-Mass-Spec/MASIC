using System.Collections.Generic;

namespace MASIC
{
    /// <summary>
    /// Container for tracking similar parent ions
    /// </summary>
    public class clsSimilarParentIonsData
    {
        /// <summary>
        /// Pointer array of index values in the original data
        /// </summary>
        public int[] MZPointerArray { get; set; }

        /// <summary>
        /// Number of parent ions with true in IonUsed[]
        /// </summary>
        public int IonInUseCount { get; set; }

        /// <summary>
        /// Flags for whether a parent ion has already been grouped with another parent ion
        /// </summary>
        public bool[] IonUsed { get; }

        /// <summary>
        /// List of unique m/z values
        /// </summary>
        public List<clsUniqueMZListItem> UniqueMZList { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSimilarParentIonsData(int parentIonCount)
        {
            MZPointerArray = new int[parentIonCount];
            IonUsed = new bool[parentIonCount];

            UniqueMZList = new List<clsUniqueMZListItem>();
        }

        /// <summary>
        /// Show the value of IonInUseCount
        /// </summary>
        public override string ToString()
        {
            return "IonInUseCount: " + IonInUseCount;
        }
    }
}
