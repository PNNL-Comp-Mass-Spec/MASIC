namespace MASIC
{
    /// <summary>
    /// Metadata for creating a selected ion chromatogram of a custom m/z value
    /// </summary>
    public class clsCustomMZSearchSpec
    {
        /// <summary>
        /// m/z to find
        /// </summary>
        public double MZ { get; set; }

        /// <summary>
        /// If 0, uses the global search tolerance defined
        /// </summary>
        public double MZToleranceDa { get; set; }

        /// <summary>
        /// This is an Integer if ScanType = CustomSICScanTypeConstants.Absolute
        /// It is a Single if ScanType = .Relative or ScanType = .AcquisitionTime
        /// </summary>
        public float ScanOrAcqTimeCenter { get; set; }

        /// <summary>
        /// This is an Integer if ScanType = CustomSICScanTypeConstants.Absolute
        /// It is a Single if ScanType = .Relative or ScanType = .AcquisitionTime
        /// </summary>
        /// <remarks>Set to 0 to search the entire file for the given mass</remarks>
        public float ScanOrAcqTimeTolerance { get; set; }

        /// <summary>
        /// Description of the custom m/z value
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetMz"></param>
        public clsCustomMZSearchSpec(double targetMz)
        {
            MZ = targetMz;
        }

        /// <summary>
        /// Show the m/z value and search tolerance
        /// </summary>
        public override string ToString()
        {
            return "m/z: " + MZ.ToString("0.0000") + " ±" + MZToleranceDa.ToString("0.0000");
        }
    }
}
