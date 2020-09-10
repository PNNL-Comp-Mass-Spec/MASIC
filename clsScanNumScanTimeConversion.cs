using System;
using MASICPeakFinder;
using PRISM;

namespace MASIC
{
    public class clsScanNumScanTimeConversion : EventNotifier
    {
        /// <summary>
        /// Find the index of the scan closest to scanOrAcqTime (searching both Survey and Frag Scans using the MasterScanList)
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="scanOrAcqTime">can be absolute, relative, or AcquisitionTime</param>
        /// <param name="scanType">Specifies what type of value scanOrAcqTime is; 0=absolute, 1=relative, 2=acquisition time (aka elution time)</param>
        /// <returns>The index of the scan closest to scanOrAcqTime, or 0 if an error</returns>
        /// <remarks></remarks>
        private int FindNearestScanNumIndex(
            clsScanList scanList,
            float scanOrAcqTime,
            clsCustomSICList.eCustomSICScanTypeConstants scanType)
        {
            try
            {
                int scanIndexMatch;

                if (scanType == clsCustomSICList.eCustomSICScanTypeConstants.Absolute || scanType == clsCustomSICList.eCustomSICScanTypeConstants.Relative)
                {
                    var absoluteScanNumber = ScanOrAcqTimeToAbsolute(scanList, scanOrAcqTime, scanType, false);
                    scanIndexMatch = clsBinarySearch.BinarySearchFindNearest(
                        scanList.MasterScanNumList,
                        absoluteScanNumber,
                        clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint);
                }
                else
                {
                    // scanType = eCustomSICScanTypeConstants.AcquisitionTime
                    // Find the closest match in scanList.MasterScanTimeList
                    scanIndexMatch = clsBinarySearch.BinarySearchFindNearest(
                        scanList.MasterScanTimeList,
                        scanOrAcqTime,
                        clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint);
                }

                return scanIndexMatch;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in FindNearestScanNumIndex", ex);
                return 0;
            }
        }

        /// <summary>
        /// Finds the index of the survey scan closest to scanOrAcqTime
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="scanOrAcqTime">Scan or acquisition time</param>
        /// <param name="scanType">Type for scanOrAcqTime; should be Absolute, Relative, or AcquisitionTime</param>
        /// <returns></returns>
        public int FindNearestSurveyScanIndex(
            clsScanList scanList,
            float scanOrAcqTime,
            clsCustomSICList.eCustomSICScanTypeConstants scanType)
        {
            try
            {
                var surveyScanIndexMatch = -1;
                var scanNumberToFind = ScanOrAcqTimeToAbsolute(scanList, scanOrAcqTime, scanType, false);

                for (var index = 0; index < scanList.SurveyScans.Count; index++)
                {
                    if (scanList.SurveyScans[index].ScanNumber >= scanNumberToFind)
                    {
                        surveyScanIndexMatch = index;
                        if (scanList.SurveyScans[index].ScanNumber != scanNumberToFind && index < scanList.SurveyScans.Count - 1)
                        {
                            // Didn't find an exact match; determine which survey scan is closer
                            if (Math.Abs(scanList.SurveyScans[index + 1].ScanNumber - scanNumberToFind) <
                                Math.Abs(scanList.SurveyScans[index].ScanNumber - scanNumberToFind))
                            {
                                surveyScanIndexMatch++;
                            }
                        }

                        break;
                    }
                }

                if (surveyScanIndexMatch < 0)
                {
                    // Match not found; return either the first or the last survey scan
                    if (scanList.SurveyScans.Count > 0)
                    {
                        surveyScanIndexMatch = scanList.SurveyScans.Count - 1;
                    }
                    else
                    {
                        surveyScanIndexMatch = 0;
                    }
                }

                return surveyScanIndexMatch;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in FindNearestSurveyScanIndex", ex);
                return 0;
            }
        }

        /// <summary>
        /// Converts a scan number or acquisition time to an actual scan number
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="scanOrAcqTime">Value to convert</param>
        /// <param name="scanType">Type of the value to convert; 0=Absolute, 1=Relative, 2=Acquisition Time (aka elution time)</param>
        /// <param name="convertingRangeOrTolerance">True when converting a range</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public int ScanOrAcqTimeToAbsolute(
            clsScanList scanList,
            float scanOrAcqTime,
            clsCustomSICList.eCustomSICScanTypeConstants scanType,
            bool convertingRangeOrTolerance)
        {
            try
            {
                var absoluteScanNumber = 0;

                switch (scanType)
                {
                    case clsCustomSICList.eCustomSICScanTypeConstants.Absolute:
                        // scanOrAcqTime is an absolute scan number (or range of scan numbers)
                        // No conversion needed; simply return the value
                        absoluteScanNumber = (int)Math.Round(scanOrAcqTime);
                        break;

                    case clsCustomSICList.eCustomSICScanTypeConstants.Relative:
                        // scanOrAcqTime is a fraction of the total number of scans (for example, 0.5)

                        // Use the total range of scan numbers
                        if (scanList.MasterScanOrderCount > 0)
                        {
                            var totalScanRange = scanList.MasterScanNumList[scanList.MasterScanOrderCount - 1] - scanList.MasterScanNumList[0];

                            absoluteScanNumber = (int)Math.Round(scanOrAcqTime * totalScanRange + scanList.MasterScanNumList[0]);
                        }
                        else
                        {
                            absoluteScanNumber = 0;
                        }

                        break;

                    case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime:
                        // scanOrAcqTime is an elution time value
                        // If convertingRangeOrTolerance = False, then look for the scan that is nearest to scanOrAcqTime
                        // If convertingRangeOrTolerance = True, then Convert scanOrAcqTime to a relative scan range and then
                        // call this function again with that relative time

                        if (convertingRangeOrTolerance)
                        {
                            var totalRunTime = scanList.MasterScanTimeList[scanList.MasterScanOrderCount - 1] - scanList.MasterScanTimeList[0];
                            if (totalRunTime < 0.1)
                            {
                                totalRunTime = 1;
                            }

                            var relativeTime = scanOrAcqTime / totalRunTime;

                            absoluteScanNumber = ScanOrAcqTimeToAbsolute(scanList, relativeTime, clsCustomSICList.eCustomSICScanTypeConstants.Relative, true);
                        }
                        else
                        {
                            var masterScanIndex = FindNearestScanNumIndex(scanList, scanOrAcqTime, scanType);
                            if (masterScanIndex >= 0 && scanList.MasterScanOrderCount > 0)
                            {
                                absoluteScanNumber = scanList.MasterScanNumList[masterScanIndex];
                            }
                        }

                        break;

                    default:
                        // Unknown type; assume absolute scan number
                        absoluteScanNumber = (int)Math.Round(scanOrAcqTime);
                        break;
                }

                return absoluteScanNumber;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in clsMasic->ScanOrAcqTimeToAbsolute", ex);
                return 0;
            }
        }

        /// <summary>
        /// Convert a relative scan or acquisition time to elution time
        /// If scanType is eCustomSICScanTypeConstants.AcquisitionTime, no conversion is required
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="scanOrAcqTime"></param>
        /// <param name="scanType"></param>
        /// <param name="convertingRangeOrTolerance"></param>
        /// <returns></returns>
        public float ScanOrAcqTimeToScanTime(
            clsScanList scanList,
            float scanOrAcqTime,
            clsCustomSICList.eCustomSICScanTypeConstants scanType,
            bool convertingRangeOrTolerance)
        {
            try
            {
                switch (scanType)
                {
                    case clsCustomSICList.eCustomSICScanTypeConstants.Absolute:
                        // scanOrAcqTime is an absolute scan number (or range of scan numbers)

                        // If convertingRangeOrTolerance = False, look for the scan that is nearest to scanOrAcqTime

                        // If convertingRangeOrTolerance = True, Convert scanOrAcqTime to a relative scan range,
                        // then call this function again with that relative time

                        if (convertingRangeOrTolerance)
                        {
                            var totalScans = scanList.MasterScanNumList[scanList.MasterScanOrderCount - 1] - scanList.MasterScanNumList[0];
                            if (totalScans < 1)
                            {
                                totalScans = 1;
                            }

                            var relativeTime = scanOrAcqTime / totalScans;

                            return ScanOrAcqTimeToScanTime(scanList, relativeTime, clsCustomSICList.eCustomSICScanTypeConstants.Relative, true);
                        }
                        else
                        {
                            var masterScanIndex = FindNearestScanNumIndex(scanList, scanOrAcqTime, scanType);
                            if (masterScanIndex >= 0 && scanList.MasterScanOrderCount > 0)
                            {
                                return scanList.MasterScanTimeList[masterScanIndex];
                            }
                        }

                        break;

                    case clsCustomSICList.eCustomSICScanTypeConstants.Relative:
                        // scanOrAcqTime is a fraction of the total number of scans (for example, 0.5)

                        // Use the total range of scan times
                        if (scanList.MasterScanOrderCount > 0)
                        {
                            var totalRunTime = scanList.MasterScanTimeList[scanList.MasterScanOrderCount - 1] - scanList.MasterScanTimeList[0];

                            return scanOrAcqTime * totalRunTime + scanList.MasterScanTimeList[0];
                        }
                        else
                        {
                            return 0;
                        }

                    case clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime:
                        // scanOrAcqTime is an elution time value (or elution time range)
                        // No conversion needed; simply return the value
                        return scanOrAcqTime;

                    default:
                        // Unknown type; assume already a scan time
                        return scanOrAcqTime;
                }

                return 0;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in clsMasic->ScanOrAcqTimeToScanTime", ex);
                return 0;
            }
        }
    }
}
