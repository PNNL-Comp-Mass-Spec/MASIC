using System.Collections.Generic;
using System.Linq;
using MASICPeakFinder;

namespace MASIC.Data
{
    /// <summary>
    /// Container for a selected ion chromatogram
    /// </summary>
    public class SICDetails
    {
        // Ignore Spelling: MASIC

        /// <summary>
        /// Indicates the type of scans that the SICScanIndices() array points to. Will normally be "SurveyScan", but for MRM data will be "FragScan"
        /// </summary>
        public ScanList.ScanTypeConstants SICScanType;

        /// <summary>
        /// Selected ion chromatogram data points
        /// </summary>
        public readonly List<SICDataPoint> SICData;

        /// <summary>
        /// Number of values in SICData
        /// </summary>
        public int SICDataCount => SICData.Count;

        /// <summary>
        /// Intensity values stored in SICData
        /// </summary>
        public double[] SICIntensities => (from item in SICData select item.Intensity).ToArray();

        /// <summary>
        /// Intensity values stored in SICData, converted to floats
        /// </summary>
        public float[] SICIntensitiesAsFloat => (from item in SICData select (float)item.Intensity).ToArray();

        /// <summary>
        /// m/z values stored in SICData, converted to floats
        /// </summary>
        public float[] SICMassesAsFloat => (from item in SICData select (float)item.Mass).ToArray();

        /// <summary>
        /// m/z values stored in SICData
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public double[] SICMasses => (from item in SICData select item.Mass).ToArray();

        /// <summary>
        /// Scan numbers of the data stored in SICData
        /// </summary>
        public int[] SICScanNumbers => (from item in SICData select item.ScanNumber).ToArray();

        /// <summary>
        /// Scan index values of the data stored in SICData
        /// </summary>
        public int[] SICScanIndices => (from item in SICData select item.ScanIndex).ToArray();

        /// <summary>
        /// Constructor
        /// </summary>
        public SICDetails()
        {
            SICData = new List<SICDataPoint>(16);
        }

        /// <summary>
        /// Append a data point to SICData
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="intensity"></param>
        /// <param name="mass"></param>
        /// <param name="scanIndex"></param>
        public void AddData(int scanNumber, double intensity, double mass, int scanIndex)
        {
            var dataPoint = new SICDataPoint(scanNumber, intensity, mass, scanIndex);
            SICData.Add(dataPoint);
        }

        /// <summary>
        /// Clear all stored data, including SICScanType
        /// </summary>
        public void Reset()
        {
            SICData.Clear();
            SICScanType = ScanList.ScanTypeConstants.SurveyScan;
        }

        /// <summary>
        /// Show the data count in SICData
        /// </summary>
        public override string ToString()
        {
            return "SICDataCount: " + SICData.Count;
        }
    }
}
