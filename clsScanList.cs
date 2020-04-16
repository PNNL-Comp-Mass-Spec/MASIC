using System.Collections.Generic;
using ThermoRawFileReader;

namespace MASIC
{

    /// <summary>
/// Used to track all spectra (scans) in the instrument data file
/// </summary>
    public class clsScanList : clsMasicEventNotifier
    {

        #region // TODO
        public enum eScanTypeConstants
        {
            SurveyScan = 0,
            FragScan = 1
        }

        #endregion
        #region // TODO
        public struct udtScanOrderPointerType
        {
            public eScanTypeConstants ScanType;
            public int ScanIndexPointer;                  // Pointer to entry into list clsScanList.SurveyScans or clsScanList.FragScans

            public override string ToString()
            {
                return ScanIndexPointer + ": " + ScanType.ToString();
            }
        }

        #endregion

        // Note: We're keeping the Survey Scans separate from the Fragmentation Scans to make the creation of the
        // survey scan based SIC's easier (and faster)
        // The MasterScanOrder array allows us to step through the data scan-by-scan, using both SurveyScans and FragScans

        /// <summary>
    /// List of survey scans, the order is the same as in the original data file, and thus is by increasing scan number
    /// </summary>
        public readonly List<clsScanInfo> SurveyScans;

        /// <summary>
    /// List of fragmentation scans, the order is the same as in the original data file, and thus is by increasing scan number
    /// </summary>
        public readonly List<clsScanInfo> FragScans;

        /// <summary>
    /// List holding pointers to either the SurveyScans or FragScans lists, in order of scan number
    /// </summary>
        public readonly List<udtScanOrderPointerType> MasterScanOrder;

        /// <summary>
    /// List of scan numbers, parallel to MasterScanOrder
    /// </summary>
        public readonly List<int> MasterScanNumList;

        /// <summary>
    /// List of scan times (elution timers), parallel to MasterScanOrder
    /// </summary>
        public readonly List<float> MasterScanTimeList;

        /// <summary>
    /// List of parent ions
    /// </summary>
        public readonly List<clsParentIonInfo> ParentIons;

        /// <summary>
    /// Number of items in MasterScanOrder
    /// </summary>
    /// <returns></returns>
        public int MasterScanOrderCount
        {
            get
            {
                return MasterScanOrder.Count;
            }
        }

        /// <summary>
    /// Set to true if the user cancels any of the processing steps
    /// </summary>
        public bool ProcessingIncomplete { get; set; }

        /// <summary>
    /// Will be true if SIM data is present
    /// </summary>
    /// <returns></returns>
        public bool SIMDataPresent { get; set; }

        /// <summary>
    /// Will be true if MRM data is present
    /// </summary>
    /// <returns></returns>
        public bool MRMDataPresent { get; set; }

        /// <summary>
    /// Constructor
    /// </summary>
        public clsScanList()
        {
            SurveyScans = new List<clsScanInfo>();
            FragScans = new List<clsScanInfo>();
            MasterScanOrder = new List<udtScanOrderPointerType>();
            MasterScanNumList = new List<int>();
            MasterScanTimeList = new List<float>();
            ParentIons = new List<clsParentIonInfo>();
        }

        public int AddFakeSurveyScan()
        {
            const int scanNumber = 0;
            const float scanTime = 0;
            return AddFakeSurveyScan(scanNumber, scanTime);
        }

        /// <summary>
    /// Adds a "fake" survey scan with the given scan number and scan time
    /// </summary>
    /// <param name="scanNumber"></param>
    /// <param name="scanTime"></param>
    /// <returns>The index in SurveyScans() at which the new scan was added</returns>
        private int AddFakeSurveyScan(int scanNumber, float scanTime)
        {
            var surveyScan = GetFakeSurveyScan(scanNumber, scanTime);
            int surveyScanIndex = SurveyScans.Count;
            SurveyScans.Add(surveyScan);
            AddMasterScanEntry(eScanTypeConstants.SurveyScan, surveyScanIndex);
            return surveyScanIndex;
        }

        public void AddMasterScanEntry(eScanTypeConstants eScanType, int scanIndex)
        {
            // Adds a new entry to .MasterScanOrder using an existing entry in SurveyScans() or FragScans()

            if (eScanType == eScanTypeConstants.SurveyScan)
            {
                if (SurveyScans.Count > 0 && scanIndex < SurveyScans.Count)
                {
                    AddMasterScanEntry(eScanType, scanIndex, SurveyScans[scanIndex].ScanNumber, SurveyScans[scanIndex].ScanTime);
                }
                else
                {
                    // This code shouldn't normally be reached
                    ReportMessage($"Error in AddMasterScanEntry for ScanType {eScanType}, Survey ScanIndex {scanIndex}: index is out of range");
                    AddMasterScanEntry(eScanType, scanIndex, 0, 0);
                }
            }
            else if (eScanType == eScanTypeConstants.FragScan)
            {
                if (FragScans.Count > 0 && scanIndex < FragScans.Count)
                {
                    AddMasterScanEntry(eScanType, scanIndex, FragScans[scanIndex].ScanNumber, FragScans[scanIndex].ScanTime);
                }
                else
                {
                    // This code shouldn't normally be reached
                    AddMasterScanEntry(eScanType, scanIndex, 0, 0);
                    ReportMessage($"Error in AddMasterScanEntry for ScanType {eScanType}, Frag ScanIndex {scanIndex}: index is out of range");
                }
            }
            else
            {
                // Unknown type; cannot add
                ReportError("Programming error: unknown value for eScanType: " + eScanType);
            }
        }

        public void AddMasterScanEntry(eScanTypeConstants eScanType, int scanIndex, int scanNumber, float scanTime)
        {
            var newScanEntry = new udtScanOrderPointerType()
            {
                ScanType = eScanType,
                ScanIndexPointer = scanIndex
            };
            MasterScanOrder.Add(newScanEntry);
            MasterScanNumList.Add(scanNumber);
            MasterScanTimeList.Add(scanTime);
        }

        private clsScanInfo GetFakeSurveyScan(int scanNumber, float scanTime)
        {
            var surveyScan = new clsScanInfo()
            {
                ScanNumber = scanNumber,
                ScanTime = scanTime,
                ScanHeaderText = "Full ms",
                ScanTypeName = "MS",
                BasePeakIonMZ = 0,
                BasePeakIonIntensity = 0,
                TotalIonIntensity = 0,
                ZoomScan = false,
                SIMScan = false,
                MRMScanType = MRMScanTypeConstants.NotMRM,
                LowMass = 0,
                HighMass = 0,
                IsFTMS = false
            };

            // Survey scans typically lead to multiple parent ions; we do not record them here
            surveyScan.FragScanInfo.ParentIonInfoIndex = -1;

            // Store the collision mode and possibly the scan filter text
            surveyScan.FragScanInfo.CollisionMode = string.Empty;
            return surveyScan;
        }

        public void Initialize()
        {
            SurveyScans.Clear();
            FragScans.Clear();
            MasterScanOrder.Clear();
            MasterScanNumList.Clear();
            MasterScanTimeList.Clear();
            ParentIons.Clear();
        }
    }
}