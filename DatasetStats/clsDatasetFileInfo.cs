using System;

namespace MASIC.DatasetStats
{
    public class DatasetFileInfo
    {
        public DateTime FileSystemCreationTime { get; set; }
        public DateTime FileSystemModificationTime { get; set; }
        public int DatasetID { get; set; }
        public string DatasetName { get; set; }
        public string FileExtension { get; set; }
        public DateTime AcqTimeStart { get; set; }
        public DateTime AcqTimeEnd { get; set; }
        public int ScanCount { get; set; }
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DatasetFileInfo()
        {
            Clear();
        }

        public void Clear()
        {
            FileSystemCreationTime = DateTime.MinValue;
            FileSystemModificationTime = DateTime.MinValue;
            DatasetID = 0;
            DatasetName = string.Empty;
            FileExtension = string.Empty;
            AcqTimeStart = DateTime.MinValue;
            AcqTimeEnd = DateTime.MinValue;
            ScanCount = 0;
            FileSizeBytes = 0;
        }

        public override string ToString()
        {
            return string.Format("Dataset {0}, ScanCount={1}", DatasetName, ScanCount);
        }
    }
}
