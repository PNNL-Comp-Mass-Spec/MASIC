using System.Collections.Generic;
using ThermoRawFileReader;

namespace MASIC.Data
{
    /// <summary>
    /// Container for scan metadata
    /// </summary>
    public class ScanInfo
    {
        /// <summary>
        /// Ranges from 1 to the number of scans in the datafile
        /// </summary>
        public int ScanNumber { get; set; }

        /// <summary>
        /// Retention (elution) Time (in minutes)
        /// </summary>
        public float ScanTime { get; set; }

        /// <summary>
        /// String description of the scan mode for the given scan; only used for Thermo .Raw files
        /// </summary>
        /// <remarks>Typical values are: FTMS + p NSI Full ms, ITMS + c ESI Full ms,
        /// ITMS + p ESI d Z ms, ITMS + c NSI d Full ms2, ITMS + c NSI d Full ms2,
        /// ITMS + c NSI d Full ms2, FTMS + c NSI d Full ms2, ITMS + c NSI d Full ms3</remarks>
        public string ScanHeaderText { get; set; }

        /// <summary>
        /// Scan type name
        /// </summary>
        /// <remarks>Typical values: MS, HMS, Zoom, CID-MSn, or PQD-MSn</remarks>
        public string ScanTypeName { get; set; }

        /// <summary>
        /// m/z of the most intense ion in this scan
        /// </summary>
        public double BasePeakIonMZ { get; set; }

        /// <summary>
        /// Intensity of the most intense ion in this scan
        /// </summary>
        public double BasePeakIonIntensity { get; set; }

        /// <summary>
        /// Intensity of all of the ions for this scan
        /// </summary>
        public double TotalIonIntensity { get; set; }

        /// <summary>
        /// Minimum intensity > 0 in this scan (using profile-mode data)
        /// </summary>
        public double MinimumPositiveIntensity { get; set; }

        /// <summary>
        /// True if the scan is a Zoom scan
        /// </summary>
        public bool ZoomScan { get; set; }

        /// <summary>
        /// True if the scan was a SIM scan
        /// </summary>
        public bool SIMScan { get; set; }

        /// <summary>
        /// MRM scan type
        /// </summary>
        public MRMScanTypeConstants MRMScanType { get; set; }

        /// <summary>
        /// For SIM scans, allows one to quickly find all of the SIM scans with the same mass range, since they'll all have the same SIMIndex
        /// </summary>
        public int SIMIndex { get; set; }

        /// <summary>
        /// Useful for SIMScans to find similar SIM scans
        /// </summary>
        public double LowMass { get; set; }

        /// <summary>
        /// Useful for SIMScans to find similar SIM scans
        /// </summary>
        public double HighMass { get; set; }

        /// <summary>
        /// True if the scan was collected in the FT cell of a Thermo instrument
        /// </summary>
        public bool IsFTMS { get; set; }

        /// <summary>
        /// Information specific to fragmentation scans
        /// </summary>
        public FragScanInfo FragScanInfo { get; }

        /// <summary>
        /// Information specific to MRM/SRM scans
        /// </summary>
        public MRMScanInfo MRMScanInfo { get; set; }

        /// <summary>
        /// Keys are ID values pointing to mExtendedHeaderNameMap (where the name is defined); values are the string or numeric values for the settings
        /// </summary>
        public Dictionary<int, string> ExtendedHeaderInfo { get; }

        /// <summary>
        /// Number of ions that remain after filtering / condensing the data loaded via GetScanData
        /// The mass spectral data for this scan is tracked by a clsSpectraCache object
        /// </summary>
        public int IonCount { get; set; }

        /// <summary>
        /// Number of data points loaded via GetScanData
        /// (typically we load profile mode data)
        /// </summary>
        public int IonCountRaw { get; set; }

        /// <summary>
        /// Baseline noise stats
        /// </summary>
        public MASICPeakFinder.BaselineNoiseStats BaselineNoiseStats { get; set; }

        /// <summary>
        /// Constructor for a MS1 scan
        /// </summary>
        public ScanInfo()
            : this(0)
        {
        }

        /// <summary>
        /// Constructor for a MS2 scan
        /// </summary>
        public ScanInfo(double fragScanParentIonMz)
        {
            MRMScanType = MRMScanTypeConstants.NotMRM;

            FragScanInfo = new FragScanInfo(fragScanParentIonMz);
            MRMScanInfo = new MRMScanInfo();

            ExtendedHeaderInfo = new Dictionary<int, string>();
        }

        /// <summary>
        /// Show the scan number and scan type
        /// </summary>
        public override string ToString()
        {
            return "Scan " + ScanNumber + ", " + ScanTypeName;
        }
    }
}
