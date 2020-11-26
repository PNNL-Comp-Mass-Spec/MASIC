namespace MASICPeakFinder
{
    /// <summary>
    /// Peak info container
    /// </summary>
    public class clsPeakInfo
    {
        /// <summary>
        /// Data index of the peak center
        /// </summary>
        public int PeakLocation { get; set; }

        /// <summary>
        /// Data index of the left edge
        /// </summary>
        public int LeftEdge { get; set; }

        /// <summary>
        /// Data index of the right edge
        /// </summary>
        public int RightEdge { get; set; }

        /// <summary>
        /// Peak area
        /// </summary>
        public double PeakArea { get; set; }

        /// <summary>
        /// True if the peak is valid
        /// </summary>
        public bool PeakIsValid { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="locationIndex">Index of this peak in the data arrays</param>
        public clsPeakInfo(int locationIndex)
        {
            PeakLocation = locationIndex;
        }

        /// <summary>
        /// Peak width (in points)
        /// </summary>
        public int PeakWidth => RightEdge - LeftEdge + 1;

        /// <summary>
        /// Duplicate an instance of this class
        /// </summary>
        public clsPeakInfo Clone()
        {
            return new clsPeakInfo(PeakLocation)
            {
                LeftEdge = LeftEdge,
                RightEdge = RightEdge,
                PeakArea = PeakArea,
                PeakIsValid = PeakIsValid
            };
        }

        /// <summary>
        /// Create a string describing this peak's location and area
        /// </summary>
        public override string ToString()
        {
            return string.Format("Center Index {0}, from {1} to {2}; Area {3:E1}", PeakLocation, LeftEdge, RightEdge, PeakArea);
        }
    }
}