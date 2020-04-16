using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PRISM;

namespace MASIC
{
    public class clsCustomSICList : EventNotifier
    {
        #region // TODO
        public const string CUSTOM_SIC_TYPE_ABSOLUTE = "Absolute";
        public const string CUSTOM_SIC_TYPE_RELATIVE = "Relative";
        public const string CUSTOM_SIC_TYPE_ACQUISITION_TIME = "AcquisitionTime";

        public enum eCustomSICScanTypeConstants
        {
            Absolute = 0,            // Absolute scan number
            Relative = 1,            // Relative scan number (ranging from 0 to 1, where 0 is the first scan and 1 is the last scan)
            AcquisitionTime = 2,     // The scan's acquisition time (aka elution time if using liquid chromatography)
            Undefined = 3
        }

        #endregion
        #region // TODO
        public string CustomSICListFileName
        {
            get
            {
                if (mCustomSICListFileName == null)
                    return string.Empty;
                return mCustomSICListFileName;
            }

            set
            {
                if (value == null)
                {
                    mCustomSICListFileName = string.Empty;
                }
                else
                {
                    mCustomSICListFileName = value.Trim();
                }
            }
        }

        public eCustomSICScanTypeConstants ScanToleranceType { get; set; }

        /// <summary>
        /// This is an Integer if ScanToleranceType = eCustomSICScanTypeConstants.Absolute
        /// It is a Single if ScanToleranceType = .Relative or ScanToleranceType = .AcquisitionTime
        /// Set to 0 to search the entire file for the given mass
        /// </summary>
        public float ScanOrAcqTimeTolerance { get; set; }
        public List<clsCustomMZSearchSpec> CustomMZSearchValues { get; private set; }

        /// <summary>
        /// When True, then will only search for the m/z values listed in the custom m/z list
        /// </summary>
        public bool LimitSearchToCustomMZList { get; set; }
        public string RawTextMZList { get; set; }
        public string RawTextMZToleranceDaList { get; set; }
        public string RawTextScanOrAcqTimeCenterList { get; set; }
        public string RawTextScanOrAcqTimeToleranceList { get; set; }

        #endregion
        #region // TODO
        private string mCustomSICListFileName;
        #endregion
        /// <summary>
        /// Constructor
        /// </summary>
        public clsCustomSICList()
        {
            CustomMZSearchValues = new List<clsCustomMZSearchSpec>();
        }

        public void AddCustomSICValues(clsScanList scanList, double defaultSICTolerance, bool sicToleranceIsPPM, float defaultScanOrAcqTimeTolerance)
        {
            int scanOrAcqTimeSumCount = 0;
            float scanOrAcqTimeSumForAveraging = 0;
            try
            {
                if (CustomMZSearchValues.Count == 0)
                {
                    return;
                }

                var scanNumScanConverter = new clsScanNumScanTimeConversion();
                RegisterEvents(scanNumScanConverter);
                foreach (var customMzSearchValue in CustomMZSearchValues)
                {
                    // Add a new parent ion entry to .ParentIons() for this custom MZ value
                    var currentParentIon = new clsParentIonInfo(customMzSearchValue.MZ);
                    if (customMzSearchValue.ScanOrAcqTimeCenter < float.Epsilon)
                    {
                        // Set the SurveyScanIndex to the center of the analysis
                        currentParentIon.SurveyScanIndex = scanNumScanConverter.FindNearestSurveyScanIndex(scanList, 0.5F, eCustomSICScanTypeConstants.Relative);
                    }
                    else
                    {
                        currentParentIon.SurveyScanIndex = scanNumScanConverter.FindNearestSurveyScanIndex(scanList, customMzSearchValue.ScanOrAcqTimeCenter, ScanToleranceType);
                    }

                    // Find the next MS2 scan that occurs after the survey scan (parent scan)
                    int surveyScanNumberAbsolute = 0;
                    if (currentParentIon.SurveyScanIndex < scanList.SurveyScans.Count)
                    {
                        surveyScanNumberAbsolute = scanList.SurveyScans[currentParentIon.SurveyScanIndex].ScanNumber + 1;
                    }

                    if (scanList.MasterScanOrderCount == 0)
                    {
                        currentParentIon.FragScanIndices.Add(0);
                    }
                    else
                    {
                        int fragScanIndexMatch = clsBinarySearch.BinarySearchFindNearest(scanList.MasterScanNumList, surveyScanNumberAbsolute, scanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint);
                        while (fragScanIndexMatch < scanList.MasterScanOrderCount && scanList.MasterScanOrder[fragScanIndexMatch].ScanType == clsScanList.eScanTypeConstants.SurveyScan)
                            fragScanIndexMatch += 1;
                        if (fragScanIndexMatch == scanList.MasterScanOrderCount)
                        {
                            // Did not find the next frag scan; find the previous frag scan
                            fragScanIndexMatch -= 1;
                            while (fragScanIndexMatch > 0 && scanList.MasterScanOrder[fragScanIndexMatch].ScanType == clsScanList.eScanTypeConstants.SurveyScan)
                                fragScanIndexMatch -= 1;
                            if (fragScanIndexMatch < 0)
                                fragScanIndexMatch = 0;
                        }

                        // This is a custom SIC-based parent ion
                        // Prior to August 2014, we set .FragScanIndices(0) = 0, which made it appear that the fragmentation scan was the first MS2 spectrum in the dataset for all custom SICs
                        // This caused undesirable display results in MASIC browser, so we now set it to the next MS2 scan that occurs after the survey scan (parent scan)
                        if (scanList.MasterScanOrder[fragScanIndexMatch].ScanType == clsScanList.eScanTypeConstants.FragScan)
                        {
                            currentParentIon.FragScanIndices.Add(scanList.MasterScanOrder[fragScanIndexMatch].ScanIndexPointer);
                        }
                        else
                        {
                            currentParentIon.FragScanIndices.Add(0);
                        }
                    }

                    currentParentIon.CustomSICPeak = true;
                    currentParentIon.CustomSICPeakComment = customMzSearchValue.Comment;
                    currentParentIon.CustomSICPeakMZToleranceDa = customMzSearchValue.MZToleranceDa;
                    currentParentIon.CustomSICPeakScanOrAcqTimeTolerance = customMzSearchValue.ScanOrAcqTimeTolerance;
                    if (currentParentIon.CustomSICPeakMZToleranceDa < double.Epsilon)
                    {
                        if (sicToleranceIsPPM)
                        {
                            currentParentIon.CustomSICPeakMZToleranceDa = clsUtilities.PPMToMass(defaultSICTolerance, currentParentIon.MZ);
                        }
                        else
                        {
                            currentParentIon.CustomSICPeakMZToleranceDa = defaultSICTolerance;
                        }
                    }

                    if (currentParentIon.CustomSICPeakScanOrAcqTimeTolerance < float.Epsilon)
                    {
                        currentParentIon.CustomSICPeakScanOrAcqTimeTolerance = defaultScanOrAcqTimeTolerance;
                    }
                    else
                    {
                        scanOrAcqTimeSumForAveraging += currentParentIon.CustomSICPeakScanOrAcqTimeTolerance;
                        scanOrAcqTimeSumCount += 1;
                    }

                    if (currentParentIon.SurveyScanIndex < scanList.SurveyScans.Count)
                    {
                        currentParentIon.OptimalPeakApexScanNumber = scanList.SurveyScans[currentParentIon.SurveyScanIndex].ScanNumber;
                    }
                    else
                    {
                        currentParentIon.OptimalPeakApexScanNumber = 1;
                    }

                    currentParentIon.PeakApexOverrideParentIonIndex = -1;
                    scanList.ParentIons.Add(currentParentIon);
                }

                if (scanOrAcqTimeSumCount == CustomMZSearchValues.Count && scanOrAcqTimeSumForAveraging > 0)
                {
                    // All of the entries had a custom scan or acq time tolerance defined
                    // Update mScanOrAcqTimeTolerance to the average of the values
                    ScanOrAcqTimeTolerance = Conversions.ToSingle(Math.Round(scanOrAcqTimeSumForAveraging / scanOrAcqTimeSumCount, 4));
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in AddCustomSICValues", ex);
            }
        }

        public void AddMzSearchTarget(clsCustomMZSearchSpec mzSearchSpec)
        {
            if (CustomMZSearchValues.Count > 0)
            {
                RawTextMZList += Conversions.ToString(',');
                RawTextMZToleranceDaList += Conversions.ToString(',');
                RawTextScanOrAcqTimeCenterList += Conversions.ToString(',');
                RawTextScanOrAcqTimeToleranceList += Conversions.ToString(',');
            }

            RawTextMZList += mzSearchSpec.MZ.ToString();
            RawTextMZToleranceDaList += mzSearchSpec.MZToleranceDa.ToString();
            RawTextScanOrAcqTimeCenterList += mzSearchSpec.ScanOrAcqTimeCenter.ToString();
            RawTextScanOrAcqTimeToleranceList += mzSearchSpec.ScanOrAcqTimeTolerance.ToString();
            CustomMZSearchValues.Add(mzSearchSpec);
        }

        public bool ParseCustomSICList(string mzList, string mzToleranceDaList, string scanCenterList, string scanToleranceList, string scanCommentList)
        {
            var delimiters = new char[] { ',', ControlChars.Tab };

            // Trim any trailing tab characters
            mzList = mzList.TrimEnd(ControlChars.Tab);
            mzToleranceDaList = mzToleranceDaList.TrimEnd(ControlChars.Tab);
            scanCenterList = scanCenterList.TrimEnd(ControlChars.Tab);
            scanCommentList = scanCommentList.TrimEnd(delimiters);
            var lstMZs = mzList.Split(delimiters).ToList();
            var lstMZToleranceDa = mzToleranceDaList.Split(delimiters).ToList();
            var lstScanCenters = scanCenterList.Split(delimiters).ToList();
            var lstScanTolerances = scanToleranceList.Split(delimiters).ToList();
            List<string> lstScanComments;
            if (scanCommentList.Length > 0)
            {
                lstScanComments = scanCommentList.Split(delimiters).ToList();
            }
            else
            {
                lstScanComments = new List<string>();
            }

            ResetMzSearchValues();
            if (lstMZs.Count <= 0)
            {
                // Nothing to parse; return true
                return true;
            }

            for (int index = 0; index <= lstMZs.Count - 1; index++)
            {
                double targetMz;
                if (!double.TryParse(lstMZs[index], out targetMz))
                {
                    continue;
                }

                var mzSearchSpec = new clsCustomMZSearchSpec(targetMz)
                {
                    MZToleranceDa = 0,
                    ScanOrAcqTimeCenter = 0,                 // Set to 0 to indicate that the entire file should be searched
                    ScanOrAcqTimeTolerance = 0
                };
                if (lstScanCenters.Count > index)
                {
                    if (clsUtilities.IsNumber(lstScanCenters[index]))
                    {
                        if (ScanToleranceType == eCustomSICScanTypeConstants.Absolute)
                        {
                            mzSearchSpec.ScanOrAcqTimeCenter = Conversions.ToInteger(lstScanCenters[index]);
                        }
                        else
                        {
                            // Includes .Relative and .AcquisitionTime
                            mzSearchSpec.ScanOrAcqTimeCenter = Conversions.ToSingle(lstScanCenters[index]);
                        }
                    }
                }

                if (lstScanTolerances.Count > index)
                {
                    if (clsUtilities.IsNumber(lstScanTolerances[index]))
                    {
                        if (ScanToleranceType == eCustomSICScanTypeConstants.Absolute)
                        {
                            mzSearchSpec.ScanOrAcqTimeTolerance = Conversions.ToInteger(lstScanTolerances[index]);
                        }
                        else
                        {
                            // Includes .Relative and .AcquisitionTime
                            mzSearchSpec.ScanOrAcqTimeTolerance = Conversions.ToSingle(lstScanTolerances[index]);
                        }
                    }
                }

                if (lstMZToleranceDa.Count > index)
                {
                    if (clsUtilities.IsNumber(lstMZToleranceDa[index]))
                    {
                        mzSearchSpec.MZToleranceDa = Conversions.ToDouble(lstMZToleranceDa[index]);
                    }
                }

                if (lstScanComments.Count > index)
                {
                    mzSearchSpec.Comment = lstScanComments[index];
                }
                else
                {
                    mzSearchSpec.Comment = string.Empty;
                }

                AddMzSearchTarget(mzSearchSpec);
            }

            return true;
        }

        public void Reset()
        {
            ScanToleranceType = eCustomSICScanTypeConstants.Absolute;
            ScanOrAcqTimeTolerance = 1000;
            ResetMzSearchValues();
        }

        public void ResetMzSearchValues()
        {
            CustomMZSearchValues.Clear();
            RawTextMZList = string.Empty;
            RawTextMZToleranceDaList = string.Empty;
            RawTextScanOrAcqTimeCenterList = string.Empty;
            RawTextScanOrAcqTimeToleranceList = string.Empty;
        }

        [Obsolete("Use SetCustomSICListValues that takes List(Of clsCustomMZSearchSpec)")]
        public bool SetCustomSICListValues(eCustomSICScanTypeConstants eScanType, double mzToleranceDa, float scanOrAcqTimeToleranceValue, double[] mzList, double[] mzToleranceList, float[] scanOrAcqTimeCenterList, float[] scanOrAcqTimeToleranceList, string[] scanComments)
        {
            // Returns True if success

            int index;
            if (mzToleranceList.Length > 0 && mzToleranceList.Length != mzList.Length)
            {
                // Invalid Custom SIC comment list; number of entries doesn't match
                return false;
            }
            else if (scanOrAcqTimeCenterList.Length > 0 && scanOrAcqTimeCenterList.Length != mzList.Length)
            {
                // Invalid Custom SIC scan center list; number of entries doesn't match
                return false;
            }
            else if (scanOrAcqTimeToleranceList.Length > 0 && scanOrAcqTimeToleranceList.Length != mzList.Length)
            {
                // Invalid Custom SIC scan center list; number of entries doesn't match
                return false;
            }
            else if (scanComments.Length > 0 && scanComments.Length != mzList.Length)
            {
                // Invalid Custom SIC comment list; number of entries doesn't match
                return false;
            }

            ResetMzSearchValues();
            ScanToleranceType = eScanType;

            // This value is used if scanOrAcqTimeToleranceList is blank or for any entries in scanOrAcqTimeToleranceList() that are zero
            ScanOrAcqTimeTolerance = scanOrAcqTimeToleranceValue;
            if (mzList.Length == 0)
            {
                return true;
            }

            for (index = 0; index <= mzList.Length - 1; index++)
            {
                var mzSearchSpec = new clsCustomMZSearchSpec(mzList[index]);
                if (mzToleranceList.Length > index && mzToleranceList[index] > 0)
                {
                    mzSearchSpec.MZToleranceDa = mzToleranceList[index];
                }
                else
                {
                    mzSearchSpec.MZToleranceDa = mzToleranceDa;
                }

                if (scanOrAcqTimeCenterList.Length > index)
                {
                    mzSearchSpec.ScanOrAcqTimeCenter = scanOrAcqTimeCenterList[index];
                }
                else
                {
                    mzSearchSpec.ScanOrAcqTimeCenter = 0;
                }         // Set to 0 to indicate that the entire file should be searched

                if (scanOrAcqTimeToleranceList.Length > index && scanOrAcqTimeToleranceList[index] > 0)
                {
                    mzSearchSpec.ScanOrAcqTimeTolerance = scanOrAcqTimeToleranceList[index];
                }
                else
                {
                    mzSearchSpec.ScanOrAcqTimeTolerance = scanOrAcqTimeToleranceValue;
                }

                if (scanComments.Length > 0 && scanComments.Length > index)
                {
                    mzSearchSpec.Comment = scanComments[index];
                }
                else
                {
                    mzSearchSpec.Comment = string.Empty;
                }

                AddMzSearchTarget(mzSearchSpec);
            }

            ValidateCustomSICList();
            return true;
        }

        public bool SetCustomSICListValues(eCustomSICScanTypeConstants eScanType, float scanOrAcqTimeToleranceValue, List<clsCustomMZSearchSpec> mzSearchSpecs)
        {
            // Returns True if success

            ResetMzSearchValues();
            ScanToleranceType = eScanType;

            // This value is used if scanOrAcqTimeToleranceList is blank or for any entries in scanOrAcqTimeToleranceList() that are zero
            ScanOrAcqTimeTolerance = scanOrAcqTimeToleranceValue;
            if (mzSearchSpecs.Count == 0)
            {
                return true;
            }

            foreach (var mzSearchSpec in mzSearchSpecs)
                AddMzSearchTarget(mzSearchSpec);
            ValidateCustomSICList();
            return true;
        }

        public void ValidateCustomSICList()
        {
            if (CustomMZSearchValues == null || CustomMZSearchValues.Count == 0)
            {
                return;
            }

            // Check whether all of the values are between 0 and 1
            // If they are, then auto-switch .ScanToleranceType to "Relative"

            int countBetweenZeroAndOne = 0;
            int countOverOne = 0;
            foreach (var customMzValue in CustomMZSearchValues)
            {
                if (customMzValue.ScanOrAcqTimeCenter > 1)
                {
                    countOverOne += 1;
                }
                else
                {
                    countBetweenZeroAndOne += 1;
                }
            }

            if (countOverOne == 0 & countBetweenZeroAndOne > 0)
            {
                if (ScanToleranceType == eCustomSICScanTypeConstants.Absolute)
                {
                    // No values were greater than 1 but at least one value is between 0 and 1
                    // Change the ScanToleranceType mode from Absolute to Relative
                    ScanToleranceType = eCustomSICScanTypeConstants.Relative;
                }
            }

            if (countOverOne > 0 & countBetweenZeroAndOne == 0)
            {
                if (ScanToleranceType == eCustomSICScanTypeConstants.Relative)
                {
                    // The ScanOrAcqTimeCenter values cannot be relative
                    // Change the ScanToleranceType mode from Relative to Absolute
                    ScanToleranceType = eCustomSICScanTypeConstants.Absolute;
                }
            }
        }

        public override string ToString()
        {
            if (CustomMZSearchValues == null || CustomMZSearchValues.Count == 0)
            {
                return "0 custom m/z search values";
            }
            else
            {
                return CustomMZSearchValues.Count + " custom m/z search values";
            }
        }
    }
}