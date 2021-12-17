namespace MASICPeakFinder
{
    /// <summary>
    /// Selected ion chromatogram data point
    /// </summary>
    public class SICDataPoint
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
        /// <param name="scanNumber"></param>
        /// <param name="intensity"></param>
        /// <param name="mass"></param>
        public SICDataPoint(int scanNumber, double intensity, double mass)
            : this(scanNumber, intensity, mass, 0)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="intensity"></param>
        /// <param name="mass"></param>
        /// <param name="index"></param>
        public SICDataPoint(int scanNumber, double intensity, double mass, int index)
        {
            ScanNumber = scanNumber;
            Intensity = intensity;
            Mass = mass;
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