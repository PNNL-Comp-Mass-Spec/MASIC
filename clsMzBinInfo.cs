namespace MASIC
{
    public class clsMzBinInfo
    {
        public double MZ { get; set; }
        public double MZTolerance { get; set; }
        public bool MZToleranceIsPPM { get; set; }
        public int ParentIonIndex { get; set; }

        public override string ToString()
        {
            if (MZToleranceIsPPM)
            {
                return "m/z: " + MZ.ToString("0.0") + ", MZTolerance: " + MZTolerance.ToString("0.0") + " ppm";
            }
            else
            {
                return "m/z: " + MZ.ToString("0.0") + ", MZTolerance: " + MZTolerance.ToString("0.000") + " Da";
            }
        }
    }
}
