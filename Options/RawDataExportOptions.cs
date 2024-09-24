namespace MASIC.Options
{
    /// <summary>
    /// Raw data export options
    /// </summary>
    public class RawDataExportOptions
    {
        // Ignore Spelling: MASIC

        /// <summary>
        /// Raw data export file formats
        /// </summary>
        public enum ExportRawDataFileFormatConstants
        {
            /// <summary>
            /// ICR-2LS .pek file
            /// </summary>
            PEKFile = 0,

            /// <summary>
            /// DeconTools compatible _scans.csv and _isos.csv files
            /// </summary>
            CSVFile = 1
        }

        /// <summary>
        /// When true, export is enabled
        /// </summary>
        public bool ExportEnabled { get; set; }

        /// <summary>
        /// Raw data export file format
        /// </summary>
        public ExportRawDataFileFormatConstants FileFormat { get; set; }

        /// <summary>
        /// When true, include MS/MS spectra
        /// </summary>
        public bool IncludeMSMS { get; set; }

        /// <summary>
        /// When true, renumber the spectra to start at scan 1
        /// </summary>
        public bool RenumberScans { get; set; }

        /// <summary>
        /// Minimum S/N value to use to exclude data points by intensity
        /// </summary>
        public float MinimumSignalToNoiseRatio { get; set; }

        /// <summary>
        /// Maximum number of ions per scan to export
        /// </summary>
        public int MaxIonCountPerScan { get; set; }

        /// <summary>
        /// Absolute minimum intensity value
        /// </summary>
        public float IntensityMinimum { get; set; }

        /// <summary>
        /// Reset options to defaults
        /// </summary>
        public void Reset()
        {
            ExportEnabled = false;

            FileFormat = ExportRawDataFileFormatConstants.CSVFile;
            IncludeMSMS = false;
            RenumberScans = false;

            MinimumSignalToNoiseRatio = 1;
            MaxIonCountPerScan = 200;
            IntensityMinimum = 0;
        }

        /// <summary>
        /// Show the file format to use, or a message if ExportEnabled is false
        /// </summary>
        public override string ToString()
        {
            if (ExportEnabled)
            {
                return "Export raw data as " + FileFormat;
            }

            return "Raw data export is disabled";
        }
    }
}
