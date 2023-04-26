using System;
using System.Collections.Generic;
using ThermoRawFileReader;

namespace MASIC.Data
{
    /// <summary>
    /// Used to track all spectra (scans) in the instrument data file
    /// </summary>
    public class ScanList : MasicEventNotifier
    {
        /// <summary>
        /// Scan types
        /// </summary>
        public enum ScanTypeConstants
        {
            /// <summary>
            /// Survey scan
            /// </summary>
            SurveyScan = 0,

            /// <summary>
            /// Fragmentation scan
            /// </summary>
            FragScan = 1
        }

        /// <summary>
        /// Scan order pointer container
        /// </summary>
        public struct ScanOrderPointerType
        {
            /// <summary>
            /// Scan type
            /// </summary>
            public ScanTypeConstants ScanType;

            /// <summary>
            /// Scan index pointer
            /// </summary>
            /// <remarks>
            /// Pointer to entry in .SurveyScans() or .FragScans()
            /// </remarks>
            public int ScanIndexPointer;

            /// <summary>
            /// Show the scan index value and scan type
            /// </summary>
            public override string ToString()
            {
                return ScanIndexPointer + ": " + ScanType;
            }
        }

        // ReSharper disable once CommentTypo

        // Note: We're keeping the Survey Scans separate from the Fragmentation Scans to make the creation of the
        // survey scan based SIC's easier (and faster)
        // The MasterScanOrder array allows us to step through the data scan-by-scan, using both SurveyScans and FragScans

        /// <summary>
        /// List of survey scans, the order is the same as in the original data file, and thus is by increasing scan number
        /// </summary>
        public readonly List<ScanInfo> SurveyScans;

        /// <summary>
        /// List of fragmentation scans, the order is the same as in the original data file, and thus is by increasing scan number
        /// </summary>
        public readonly List<ScanInfo> FragScans;

        /// <summary>
        /// List holding pointers to either the SurveyScans or FragScans lists, in order of scan number
        /// </summary>
        public readonly List<ScanOrderPointerType> MasterScanOrder;

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
        public readonly List<ParentIonInfo> ParentIons;

        /// <summary>
        /// Number of items in MasterScanOrder
        /// </summary>
        public int MasterScanOrderCount => MasterScanOrder.Count;

        /// <summary>
        /// Set to true if the user cancels any of the processing steps
        /// </summary>
        public bool ProcessingIncomplete { get; set; }

        /// <summary>
        /// Will be true if SIM data is present
        /// </summary>
        public bool SIMDataPresent { get; set; }

        /// <summary>
        /// Will be true if MRM data is present
        /// </summary>
        public bool MRMDataPresent { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ScanList()
        {
            SurveyScans = new List<ScanInfo>(8);
            FragScans = new List<ScanInfo>(8);

            MasterScanOrder = new List<ScanOrderPointerType>(8);
            MasterScanNumList = new List<int>(8);
            MasterScanTimeList = new List<float>(8);

            ParentIons = new List<ParentIonInfo>(8);
        }

        /// <summary>
        /// Adds a fake (placeholder) survey scan with scan number 0 and scan time 0
        /// </summary>
        /// <returns>The index in SurveyScans() at which the new scan was added</returns>
        public int AddFakeSurveyScan()
        {
            const int scanNumber = 0;
            const float scanTime = 0;

            return AddFakeSurveyScan(scanNumber, scanTime);
        }

        /// <summary>
        /// Adds a fake (placeholder) survey scan with the given scan number and scan time
        /// </summary>
        /// <param name="scanNumber"></param>
        /// <param name="scanTime"></param>
        /// <returns>The index in SurveyScans() at which the new scan was added</returns>
        private int AddFakeSurveyScan(
            int scanNumber,
            float scanTime)
        {
            var surveyScan = GetFakeSurveyScan(scanNumber, scanTime);

            var surveyScanIndex = SurveyScans.Count;

            SurveyScans.Add(surveyScan);

            AddMasterScanEntry(ScanTypeConstants.SurveyScan, surveyScanIndex);

            return surveyScanIndex;
        }

        /// <summary>
        /// Adds a new entry to .MasterScanOrder using an existing entry in SurveyScans() or FragScans()
        /// </summary>
        /// <param name="scanType"></param>
        /// <param name="scanIndex"></param>
        public void AddMasterScanEntry(ScanTypeConstants scanType, int scanIndex)
        {
            if (scanType == ScanTypeConstants.SurveyScan)
            {
                if (SurveyScans.Count > 0 && scanIndex < SurveyScans.Count)
                {
                    AddMasterScanEntry(scanType, scanIndex, SurveyScans[scanIndex].ScanNumber, SurveyScans[scanIndex].ScanTime);
                }
                else
                {
                    // This code shouldn't normally be reached
                    ReportMessage($"Error in AddMasterScanEntry for ScanType {scanType}, Survey ScanIndex {scanIndex}: index is out of range");
                    AddMasterScanEntry(scanType, scanIndex, 0, 0);
                }
            }
            else if (scanType == ScanTypeConstants.FragScan)
            {
                if (FragScans.Count > 0 && scanIndex < FragScans.Count)
                {
                    AddMasterScanEntry(scanType, scanIndex, FragScans[scanIndex].ScanNumber, FragScans[scanIndex].ScanTime);
                }
                else
                {
                    // This code shouldn't normally be reached
                    AddMasterScanEntry(scanType, scanIndex, 0, 0);
                    ReportMessage($"Error in AddMasterScanEntry for ScanType {scanType}, Fragmentation ScanIndex {scanIndex}: index is out of range");
                }
            }
            else
            {
                // Unknown type; cannot add
                ReportError("Programming error: unknown value for scanType: " + scanType);
            }
        }

        /// <summary>
        /// Adds a new entry to .MasterScanOrder, .MasterScanNumList and, .MasterScanTimeList
        /// </summary>
        /// <param name="scanType"></param>
        /// <param name="scanIndex"></param>
        /// <param name="scanNumber"></param>
        /// <param name="scanTime"></param>
        public void AddMasterScanEntry(
            ScanTypeConstants scanType,
            int scanIndex,
            int scanNumber,
            float scanTime)
        {
            var newScanEntry = new ScanOrderPointerType
            {
                ScanType = scanType,
                ScanIndexPointer = scanIndex
            };

            MasterScanOrder.Add(newScanEntry);

            MasterScanNumList.Add(scanNumber);
            MasterScanTimeList.Add(scanTime);
        }

        /// <summary>
        /// Resizes the lists to a set capacity (unless there are existing contents larger than <paramref name="scanCount"/>)
        /// </summary>
        /// <param name="scanCount"></param>
        /// <param name="surveyScanCount"></param>
        public void ReserveListCapacity(int scanCount, int surveyScanCount = 0)
        {
            MasterScanOrder.Capacity = Math.Max(MasterScanOrder.Count, scanCount);
            MasterScanNumList.Capacity = Math.Max(MasterScanNumList.Count, scanCount);
            MasterScanTimeList.Capacity = Math.Max(MasterScanTimeList.Count, scanCount);

            if (surveyScanCount > 0)
            {
                SurveyScans.Capacity = Math.Max(SurveyScans.Count, surveyScanCount);
                FragScans.Capacity = Math.Max(FragScans.Count, scanCount - surveyScanCount);
                ParentIons.Capacity = Math.Max(ParentIons.Count, scanCount - surveyScanCount);
            }
            else
            {
                // Unknown: start each off at 1/10th of scan count
                SurveyScans.Capacity = Math.Max(SurveyScans.Count, scanCount / 10);
                FragScans.Capacity = Math.Max(FragScans.Count, scanCount / 10);
                ParentIons.Capacity = Math.Max(ParentIons.Count, scanCount / 10);
            }
        }

        /// <summary>
        /// Reduces unnecessary memory overhead by resizing some lists to their contents.
        /// </summary>
        public void SetListCapacityToCount()
        {
            MasterScanOrder.Capacity = MasterScanOrder.Count;
            MasterScanNumList.Capacity = MasterScanNumList.Count;
            MasterScanTimeList.Capacity = MasterScanTimeList.Count;
            SurveyScans.Capacity = SurveyScans.Count;
            FragScans.Capacity = FragScans.Count;
            ParentIons.Capacity = ParentIons.Count;
        }

        private ScanInfo GetFakeSurveyScan(int scanNumber, float scanTime)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var surveyScan = new ScanInfo
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
                IsDIA = false,
                IsFTMS = false
            };

            // Survey scans typically lead to multiple parent ions; we do not record them here
            surveyScan.FragScanInfo.ParentIonInfoIndex = -1;

            // Store the collision mode and possibly the scan filter text
            surveyScan.FragScanInfo.CollisionMode = string.Empty;

            return surveyScan;
        }

        /// <summary>
        /// Clear all stored data
        /// </summary>
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
