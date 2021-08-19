namespace MASICPeakFinder
{
    /// <summary>
    /// Selected ion chromatogram data point
    /// </summary>
    public class clsSICDataPoint
    {
        /// <summary>
        /// Scan Index (pointer into .SurveyScans)
        /// </summary>
        public readonly int ScanIndex;

        /// <summary>
        /// Scan number
        /// </summary>
        public readonly int ScanNumber;

        /// <summary>
        /// Intensity
        /// </summary>
        public readonly double Intensity;

        /// <summary>
        /// Mass, as m/z
        /// </summary>
        public readonly double Mass;

        /// <summary>
        /// Constructor that stores 0 for the index
        /// </summary>
        /// <param name="intScanNumber"></param>
        /// <param name="dblIntensity"></param>
        /// <param name="dblMass"></param>
        public clsSICDataPoint(int intScanNumber, double dblIntensity, double dblMass)
            : this(intScanNumber, dblIntensity, dblMass, 0)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="intScanNumber"></param>
        /// <param name="dblIntensity"></param>
        /// <param name="dblMass"></param>
        /// <param name="index"></param>
        public clsSICDataPoint(int intScanNumber, double dblIntensity, double dblMass, int index)
        {
            ScanNumber = intScanNumber;
            Intensity = dblIntensity;
            Mass = dblMass;
            ScanIndex = index;
        }

        /// <summary>
        /// Show the intensity, m/z and scan number
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0:F0} at {1:F2} m/z in scan {2}", Intensity, Mass, ScanNumber);
        }
    }
}