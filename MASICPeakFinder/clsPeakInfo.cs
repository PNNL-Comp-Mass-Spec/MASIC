namespace MASICPeakFinder
{
    public class clsPeakInfo
    {
        public int PeakLocation { get; set; }
        public int LeftEdge { get; set; }
        public int RightEdge { get; set; }
        public double PeakArea { get; set; }
        public bool PeakIsValid { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="intPeakLocation">Index of this peak in the data arrays</param>
        public clsPeakInfo(int intPeakLocation)
        {
            PeakLocation = intPeakLocation;
        }

        public clsPeakInfo Clone()
        {
            var newPeak = new clsPeakInfo(PeakLocation)
            {
                LeftEdge = LeftEdge,
                RightEdge = RightEdge,
                PeakArea = PeakArea,
                PeakIsValid = PeakIsValid
            };

            return newPeak;
        }

        public override string ToString()
        {
            return string.Format("Peak Index {0}, Area {1:E1}", PeakLocation, PeakArea);
        }
    }
}