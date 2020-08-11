using System.IO;

namespace MASIC.Plots
{
    internal class PlotFileInfo
    {
        /// <summary>
        /// File description
        /// </summary>
        public string FileDescription { get; set;  }

        /// <summary>
        /// File info
        /// </summary>
        public FileInfo PlotFile { get; }

        /// <summary>
        /// Plot category
        /// </summary>
        public PlotContainerBase.PlotCategories PlotCategory { get; set; }

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
