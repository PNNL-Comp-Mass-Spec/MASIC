namespace MASIC
{
    public class clsCustomMZSearchSpec
    {
        #region "Properties"
        public double MZ { get; set; }

        /// <summary>
        /// If 0, then uses the global search tolerance defined
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

        public string Comment { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetMz"></param>
        public clsCustomMZSearchSpec(double targetMz)
        {
            MZ = targetMz;
        }

        public override string ToString()
        {
            return "m/z: " + MZ.ToString("0.0000") + " ±" + MZToleranceDa.ToString("0.0000");
        }
    }
}
