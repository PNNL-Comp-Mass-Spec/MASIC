using System;
using System.IO;

namespace MASIC.DataOutput
{
    /// <summary>
    /// Container for output file handles
    /// </summary>
    public class clsOutputFileHandles : clsMasicEventNotifier
    {
        /// <summary>
        /// Scan stats file
        /// </summary>
        public StreamWriter ScanStats { get; set; }

        // ReSharper disable CommentTypo

        /// <summary>
        /// SIC details file
        /// </summary>
        /// <remarks>This is different than _SICstats.txt file</remarks>
        public StreamWriter SICDataFile { get; set; }

        // ReSharper restore CommentTypo

        /// <summary>
        /// XML Results file
        /// </summary>
        public System.Xml.XmlTextWriter XMLFileForSICs { get; set; }

        /// <summary>
        /// MS method file base path
        /// </summary>
        public string MSMethodFilePathBase { get; set; }

        /// <summary>
        /// MS tune file base path
        /// </summary>
        public string MSTuneFilePathBase { get; set; }

        /// <summary>
        /// Close the _ScanStats.txt file
        /// </summary>
        public void CloseScanStats()
        {
            if (ScanStats != null)
            {
                ScanStats.Close();
                ScanStats = null;
            }
        }

        /// <summary>
        /// Close all files
        /// </summary>
        public bool CloseAll()
        {
            try
            {
                CloseScanStats();
                if (SICDataFile != null)
                {
                    SICDataFile.Close();
                    SICDataFile = null;
                }

                if (XMLFileForSICs != null)
                {
                    XMLFileForSICs.Close();
                    XMLFileForSICs = null;
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error in CloseOutputFileHandles", ex);
                return false;
            }
        }
    }
}
