using System.IO;

namespace MASIC.Plots
{
    internal class PlotFileInfo
    {
        public string FileDescription { get; set;  }

        public FileInfo PlotFile { get; }

        public PlotContainerBase.PlotTypes PlotType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="plotFile"></param>
        public PlotFileInfo(FileInfo plotFile)
        {
            PlotFile = plotFile;
        }
    }
}
