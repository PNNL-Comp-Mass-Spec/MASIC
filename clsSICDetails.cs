using System.Collections.Generic;
using System.Data;
using System.Linq;
using MASICPeakFinder;
using Microsoft.VisualBasic.CompilerServices;

namespace MASIC
{
    public class clsSICDetails
    {
        /// <summary>
        /// Indicates the type of scans that the SICScanIndices() array points to. Will normally be "SurveyScan", but for MRM data will be "FragScan"
        /// </summary>
        public clsScanList.eScanTypeConstants SICScanType;
        public readonly List<clsSICDataPoint> SICData;

        public int SICDataCount => SICData.Count;

        public double[] SICIntensities => (from item in SICData select item.Intensity).ToArray();

        public float[] SICIntensitiesAsFloat => (from item in SICData select Conversions.ToSingle(item.Intensity)).ToArray();

        public float[] SICMassesAsFloat => (from item in SICData select Conversions.ToSingle(item.Mass)).ToArray();

        public double[] SICMasses => (from item in SICData select item.Mass).ToArray();

        public int[] SICScanNumbers => (from item in SICData select item.ScanNumber).ToArray();

        public int[] SICScanIndices => (from item in SICData select item.ScanIndex).ToArray();

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSICDetails()
        {
            SICData = new List<clsSICDataPoint>();
        }

        public void AddData(int scanNumber, double intensity, double mass, int scanIndex)
        {
            var dataPoint = new clsSICDataPoint(scanNumber, intensity, mass, scanIndex);
            SICData.Add(dataPoint);
        }

        public void Reset()
        {
            SICData.Clear();
            SICScanType = clsScanList.eScanTypeConstants.SurveyScan;
        }

        public override string ToString()
        {
            return "SICDataCount: " + SICData.Count;
        }
    }
}
