using System;
using System.IO;

namespace MASIC.DataOutput
{
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

        public System.Xml.XmlTextWriter XMLFileForSICs { get; set; }

        public string MSMethodFilePathBase { get; set; }

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
        /// <returns></returns>
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
