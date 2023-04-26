namespace MASIC.DatasetStats
{
    /// <summary>
    /// Information about a given scan (spectrum)
    /// </summary>
    public class ScanStatsEntry
    {
        // Ignore Spelling: centroiding

        /// <summary>
        /// Scan number
        /// </summary>
        public int ScanNumber { get; set; }

        /// <summary>
        /// Scan Type (aka MSLevel)
        /// </summary>
        /// <remarks>1 for MS, 2 for MS2, 3 for MS3</remarks>
        public int ScanType { get; set; }

        /// <summary>
        /// Scan filter text (for Thermo files, generic scan filter text, created using XRawFileIO.MakeGenericThermoScanFilter)
        /// </summary>
        /// <remarks>
        /// Examples:
        ///   FTMS + p NSI Full ms [400.00-2000.00]
        ///   ITMS + c ESI Full ms [300.00-2000.00]
        ///   ITMS + p ESI d Z ms [1108.00-1118.00]
        ///   ITMS + c ESI d Full ms2 342.90@cid35.00
        /// </remarks>
        public string ScanFilterText { get; set; }

        /// <summary>
        /// Scan type name
        /// </summary>
        /// <remarks>
        /// Examples:
        ///   MS, HMS, Zoom, CID-MSn, or PQD-MSn
        /// </remarks>
        public string ScanTypeName { get; set; }

        // The following are strings to prevent the number formatting from changing

        /// <summary>
        /// Elution time, in minutes
        /// </summary>
        public string ElutionTime { get; set; }

        /// <summary>
        /// Drift time, in milliseconds
        /// </summary>
        public string DriftTimeMsec { get; set; }

        /// <summary>
        /// Total ion intensity
        /// </summary>
        public string TotalIonIntensity { get; set; }

        /// <summary>
        /// Base peak ion intensity
        /// </summary>
        public string BasePeakIntensity { get; set; }

        /// <summary>
        /// Base peak m/z
        /// </summary>
        public string BasePeakMZ { get; set; }

        /// <summary>
        /// Signal to noise ratio (S/N)
        /// </summary>
        public string BasePeakSignalToNoiseRatio { get; set; }

        /// <summary>
        /// Ion count
        /// </summary>
        public int IonCount { get; set; }

        /// <summary>
        /// Ion count before centroiding
        /// </summary>
        public int IonCountRaw { get; set; }

        /// <summary>
        /// True if this is a Data Independent Acquisition (DIA) scan
        /// </summary>
        public bool IsDIA { get; set; }

        /// <summary>
        /// Smallest m/z value in the scan
        /// </summary>
        public double MzMin { get; set; }

        /// <summary>
        /// Largest m/z value in the scan
        /// </summary>
        public double MzMax { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ScanStatsEntry()
        {
            Clear();
        }

        /// <summary>
        /// Clear all values
        /// </summary>
        public void Clear()
        {
            ScanNumber = 0;
            ScanType = 0;

            ScanFilterText = string.Empty;
            ScanTypeName = string.Empty;

            ElutionTime = "0";
            DriftTimeMsec = "0";
            TotalIonIntensity = "0";
            BasePeakIntensity = "0";
            BasePeakMZ = "0";
            BasePeakSignalToNoiseRatio = "0";

            IonCount = 0;
            IonCountRaw = 0;

            MzMin = 0;
            MzMax = 0;
        }

        /// <summary>
        /// Show the scan number and scan filter
        /// </summary>
        public override string ToString()
        {
            return string.Format("Scan {0}: {1}", ScanNumber, ScanFilterText);
        }
    }
}
