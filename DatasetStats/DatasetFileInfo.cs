using System;

namespace MASIC.DatasetStats
{
    /// <summary>
    /// Dataset file info, including dataset ID, scan count, acquisition start time, etc.
    /// </summary>
    public class DatasetFileInfo
    {
        // Ignore Spelling: acq, MASIC

        /// <summary>
        /// File creation time (local time)
        /// </summary>
        public DateTime FileSystemCreationTime { get; set; }

        /// <summary>
        /// File modification time (local time)
        /// </summary>
        public DateTime FileSystemModificationTime { get; set; }

        /// <summary>
        /// Dataset ID
        /// </summary>
        public int DatasetID { get; set; }

        /// <summary>
        /// Dataset name
        /// </summary>
        public string DatasetName { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// Acquisition start time
        /// </summary>
        public DateTime AcqTimeStart { get; set; }

        /// <summary>
        /// Acquisition end time
        /// </summary>
        public DateTime AcqTimeEnd { get; set; }

        /// <summary>
        /// Scan count (spectrum count)
        /// </summary>
        public int ScanCount { get; set; }

        /// <summary>
        /// Size of the file, in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DatasetFileInfo()
        {
            Clear();
        }

        /// <summary>
        /// Clear all values
        /// </summary>
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

        /// <summary>
        /// Show the dataset name and scan count
        /// </summary>
        public override string ToString()
        {
            return string.Format("Dataset {0}, ScanCount={1}", DatasetName, ScanCount);
        }
    }
}
