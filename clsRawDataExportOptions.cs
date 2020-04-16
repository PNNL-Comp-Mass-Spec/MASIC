
namespace MASIC
{
    public class clsRawDataExportOptions
    {

        /* TODO ERROR: Skipped RegionDirectiveTrivia */
        public enum eExportRawDataFileFormatConstants
        {
            PEKFile = 0,
            CSVFile = 1
        }
        /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        public bool ExportEnabled { get; set; }
        public eExportRawDataFileFormatConstants FileFormat { get; set; }
        public bool IncludeMSMS { get; set; }
        public bool RenumberScans { get; set; }
        public float MinimumSignalToNoiseRatio { get; set; }
        public int MaxIonCountPerScan { get; set; }
        public float IntensityMinimum { get; set; }

        public void Reset()
        {
            ExportEnabled = false;
            FileFormat = eExportRawDataFileFormatConstants.CSVFile;
            IncludeMSMS = false;
            RenumberScans = false;
            MinimumSignalToNoiseRatio = 1;
            MaxIonCountPerScan = 200;
            IntensityMinimum = 0;
        }

        public override string ToString()
        {
            if (ExportEnabled)
            {
                return "Export raw data as " + FileFormat.ToString();
            }
            else
            {
                return "Raw data export is disabled";
            }
        }
    }
}