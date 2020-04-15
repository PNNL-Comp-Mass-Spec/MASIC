namespace MASICPeakFinder
{
    public class clsSICDataPoint
    {
        public readonly int ScanIndex;
        public readonly int ScanNumber;
        public readonly double Intensity;
        public readonly double Mass;

        public clsSICDataPoint(int intScanNumber, double dblIntensity, double dblMass)
            : this(intScanNumber, dblIntensity, dblMass, 0)
        {
        }

        public clsSICDataPoint(int intScanNumber, double dblIntensity, double dblMass, int index)
        {
            ScanNumber = intScanNumber;
            Intensity = dblIntensity;
            Mass = dblMass;
            ScanIndex = index;
        }

        public override string ToString()
        {
            return string.Format("{0:F0} at {1:F2} m/z in scan {2}", Intensity, Mass, ScanNumber);
        }
    }
}