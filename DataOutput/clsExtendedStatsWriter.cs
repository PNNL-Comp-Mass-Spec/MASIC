using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace MASIC.DataOutput
{
    public class clsExtendedStatsWriter : clsMasicEventNotifier
    {
        #region // TODO
        public const string EXTENDED_STATS_HEADER_COLLISION_MODE = "Collision Mode";
        public const string EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT = "Scan Filter Text";
        #endregion
        #region // TODO
        public int ExtendedHeaderNameCount
        {
            get
            {
                return mExtendedHeaderNameMap.Count;
            }
        }

        #endregion
        #region // TODO
        /// <summary>
        /// Keys are strings of extended info names
        /// Values are the assigned ID value for the extended info name
        /// </summary>
        /// <remarks>The order of the values defines the appropriate output order for the names</remarks>
        private readonly List<KeyValuePair<string, int>> mExtendedHeaderNameMap;
        private readonly clsMASICOptions mOptions;
        #endregion
        /// <summary>
        /// Constructor
        /// </summary>
        public clsExtendedStatsWriter(clsMASICOptions masicOptions)
        {
            mExtendedHeaderNameMap = new List<KeyValuePair<string, int>>();
            mOptions = masicOptions;
        }

        private IEnumerable<string> ConcatenateExtendedStats(IEnumerable<int> nonConstantHeaderIDs, int datasetID, int scanNumber, IReadOnlyDictionary<int, string> extendedHeaderInfo)
        {
            var dataValues = new List<string>() { datasetID.ToString(), scanNumber.ToString() };
            if (extendedHeaderInfo is object && nonConstantHeaderIDs is object)
            {
                foreach (var headerID in from item in nonConstantHeaderIDs
                                         orderby item
                                         select item)
                {
                    string value = null;
                    if (extendedHeaderInfo.TryGetValue(headerID, out value))
                    {
                        if (clsUtilities.IsNumber(value))
                        {
                            if (Math.Abs(Conversion.Val(value)) < float.Epsilon)
                                value = "0";
                        }
                        else
                        {
                            // ReSharper disable StringLiteralTypo
                            var switchExpr = value;
                            switch (switchExpr)
                            {
                                case "ff":
                                    {
                                        value = "Off";
                                        break;
                                    }

                                case "n":
                                    {
                                        value = "On";
                                        break;
                                    }

                                case "eady":
                                    {
                                        value = "Ready";
                                        break;
                                    }

                                case "cquiring":
                                    {
                                        value = "Acquiring";
                                        break;
                                    }

                                case "oad":
                                    {
                                        value = "Load";
                                        break;
                                    }
                            }
                            // ReSharper restore StringLiteralTypo
                        }

                        dataValues.Add(value);
                    }
                    else
                    {
                        dataValues.Add("0");
                    }
                }
            }

            return dataValues;
        }

        public List<string> ConstructExtendedStatsHeaders()
        {
            var cTrimChars = new char[] { ':', ' ' };
            var headerNames = new List<string>() { "Dataset", "ScanNumber" };

            // Populate headerNames

            if (mExtendedHeaderNameMap.Count <= 0)
            {
                return headerNames;
            }

            var headerNamesByID = new Dictionary<int, string>();
            foreach (var item in mExtendedHeaderNameMap)
                headerNamesByID.Add(item.Value, item.Key);
            foreach (var headerItem in from item in headerNamesByID
                                       orderby item.Key
                                       select item.Value)
                headerNames.Add(headerItem.TrimEnd(cTrimChars));
            return headerNames;
        }

        private Dictionary<int, string> DeepCopyHeaderInfoDictionary(Dictionary<int, string> sourceTable)
        {
            var newTable = new Dictionary<int, string>();
            foreach (var item in sourceTable)
                newTable.Add(item.Key, item.Value);
            return newTable;
        }

        /// <summary>
        /// Looks through surveyScans and fragScans for ExtendedHeaderInfo values that are constant across all scans
        /// </summary>
        /// <param name="nonConstantHeaderIDs">Output: the ID values of the header values that are not constant</param>
        /// <param name="surveyScans"></param>
        /// <param name="fragScans"></param>
        /// <param name="cColDelimiter"></param>
        /// <returns>
        /// String that is a newline separated list of header values that are constant, tab delimited, and their constant values, also tab delimited
        /// Each line is in the form ParameterName_ColumnDelimiter_ParameterValue
        /// </returns>
        /// <remarks>mExtendedHeaderNameMap is updated so that constant header values are removed from it</remarks>
        public string ExtractConstantExtendedHeaderValues(out List<int> nonConstantHeaderIDs, IList<clsScanInfo> surveyScans, IList<clsScanInfo> fragScans, char cColDelimiter)
        {
            var cTrimChars = new char[] { ':', ' ' };
            string value = string.Empty;

            // Keys are ID values pointing to mExtendedHeaderNameMap (where the name is defined); values are the string or numeric values for the settings
            Dictionary<int, string> consolidatedValuesByID;
            var constantHeaderIDs = new List<int>();
            int scanFilterTextHeaderID;
            nonConstantHeaderIDs = new List<int>();
            if (mExtendedHeaderNameMap.Count == 0)
            {
                return string.Empty;
            }

            // Initialize nonConstantHeaderIDs
            for (int i = 0; i <= mExtendedHeaderNameMap.Count - 1; i++)
                nonConstantHeaderIDs.Add(i);
            if (!mOptions.ConsolidateConstantExtendedHeaderValues)
            {
                // Do not try to consolidate anything
                return string.Empty;
            }

            if (surveyScans.Count > 0)
            {
                consolidatedValuesByID = DeepCopyHeaderInfoDictionary(surveyScans[0].ExtendedHeaderInfo);
            }
            else if (fragScans.Count > 0)
            {
                consolidatedValuesByID = DeepCopyHeaderInfoDictionary(fragScans[0].ExtendedHeaderInfo);
            }
            else
            {
                return string.Empty;
            }

            if (consolidatedValuesByID is null)
            {
                return string.Empty;
            }

            // Look for "Scan Filter Text" in mExtendedHeaderNameMap
            if (TryGetExtendedHeaderInfoValue(EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, out scanFilterTextHeaderID))
            {
                // Match found

                // Now look for and remove the HeaderID value from consolidatedValuesByID to prevent the scan filter text from being included in the consolidated values file
                if (consolidatedValuesByID.ContainsKey(scanFilterTextHeaderID))
                {
                    consolidatedValuesByID.Remove(scanFilterTextHeaderID);
                }
            }

            // Examine the values in .ExtendedHeaderInfo() in the survey scans and compare them
            // to the values in consolidatedValuesByID, looking to see if they match
            foreach (var surveyScan in surveyScans)
            {
                if (surveyScan.ExtendedHeaderInfo is object)
                {
                    foreach (var dataItem in surveyScan.ExtendedHeaderInfo)
                    {
                        if (consolidatedValuesByID.TryGetValue(dataItem.Key, out value))
                        {
                            if (string.Equals(value, dataItem.Value))
                            {
                            }
                            // Value matches; nothing to do
                            else
                            {
                                // Value differs; remove the key from consolidatedValuesByID
                                consolidatedValuesByID.Remove(dataItem.Key);
                            }
                        }
                    }
                }
            }

            // Examine the values in .ExtendedHeaderInfo() in the frag scans and compare them
            // to the values in consolidatedValuesByID, looking to see if they match
            foreach (var fragScan in fragScans)
            {
                if (fragScan.ExtendedHeaderInfo is object)
                {
                    foreach (var item in fragScan.ExtendedHeaderInfo)
                    {
                        if (consolidatedValuesByID.TryGetValue(item.Key, out value))
                        {
                            if (string.Equals(value, item.Value))
                            {
                            }
                            // Value matches; nothing to do
                            else
                            {
                                // Value differs; remove key from consolidatedValuesByID
                                consolidatedValuesByID.Remove(item.Key);
                            }
                        }
                    }
                }
            }

            if (consolidatedValuesByID is null || consolidatedValuesByID.Count == 0)
            {
                return string.Empty;
            }

            // Populate consolidatedValueList with the values in consolidatedValuesByID,
            // separating each header and value with a tab and separating each pair of values with a NewLine character

            // Need to first populate constantHeaderIDs with the ID values and sort the list so that the values are
            // stored in consolidatedValueList in the correct order

            var consolidatedValueList = new List<string>() { "Setting" + cColDelimiter + "Value" };
            foreach (var item in consolidatedValuesByID)
                constantHeaderIDs.Add(item.Key);
            var keysToRemove = new List<string>();
            foreach (var headerId in from item in constantHeaderIDs
                                     orderby item
                                     select item)
            {
                foreach (var item in mExtendedHeaderNameMap)
                {
                    if (item.Value == headerId)
                    {
                        consolidatedValueList.Add(item.Key.TrimEnd(cTrimChars) + cColDelimiter + consolidatedValuesByID[headerId]);
                        keysToRemove.Add(item.Key);
                        break;
                    }
                }
            }

            // Remove the elements from mExtendedHeaderNameMap that were included in consolidatedValueList;
            // we couldn't remove these above since that would invalidate the iHeaderEnum enumerator

            foreach (var keyName in keysToRemove)
            {
                for (int headerIndex = 0; headerIndex <= mExtendedHeaderNameMap.Count - 1; headerIndex++)
                {
                    if ((mExtendedHeaderNameMap[headerIndex].Key ?? "") == (keyName ?? ""))
                    {
                        mExtendedHeaderNameMap.RemoveAt(headerIndex);
                        break;
                    }
                }
            }

            nonConstantHeaderIDs.Clear();

            // Populate nonConstantHeaderIDs with the ID values in mExtendedHeaderNameMap
            foreach (var item in mExtendedHeaderNameMap)
                nonConstantHeaderIDs.Add(item.Value);
            return string.Join(ControlChars.NewLine, consolidatedValueList);
        }

        private clsScanInfo GetScanByMasterScanIndex(clsScanList scanList, int masterScanIndex)
        {
            var currentScan = new clsScanInfo();
            if (scanList.MasterScanOrder is object)
            {
                if (masterScanIndex < 0)
                {
                    masterScanIndex = 0;
                }
                else if (masterScanIndex >= scanList.MasterScanOrderCount)
                {
                    masterScanIndex = scanList.MasterScanOrderCount - 1;
                }

                var switchExpr = scanList.MasterScanOrder[masterScanIndex].ScanType;
                switch (switchExpr)
                {
                    case clsScanList.eScanTypeConstants.SurveyScan:
                        {
                            // Survey scan
                            currentScan = scanList.SurveyScans[scanList.MasterScanOrder[masterScanIndex].ScanIndexPointer];
                            break;
                        }

                    case clsScanList.eScanTypeConstants.FragScan:
                        {
                            // Frag Scan
                            currentScan = scanList.FragScans[scanList.MasterScanOrder[masterScanIndex].ScanIndexPointer];
                            break;
                        }

                    default:
                        {
                            break;
                        }
                        // Unknown scan type
                }
            }

            return currentScan;
        }

        public bool SaveExtendedScanStatsFiles(clsScanList scanList, string inputFileName, string outputDirectoryPath, bool includeHeaders)
        {
            // Writes out a flat file containing the extended scan stats

            string extendedConstantHeaderOutputFilePath;
            string extendedNonConstantHeaderOutputFilePath = string.Empty;
            const char cColDelimiter = ControlChars.Tab;
            List<int> nonConstantHeaderIDs = null;
            try
            {
                UpdateProgress(0, "Saving extended scan stats to flat file");
                extendedConstantHeaderOutputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.ScanStatsExtendedConstantFlatFile);
                extendedNonConstantHeaderOutputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.ScanStatsExtendedFlatFile);
                ReportMessage("Saving extended scan stats flat file to disk: " + Path.GetFileName(extendedNonConstantHeaderOutputFilePath));
                if (mExtendedHeaderNameMap.Count == 0)
                {
                    // No extended stats to write; exit the function
                    break;
                }

                // Lookup extended stats values that are constants for all scans
                // The following will also remove the constant header values from mExtendedHeaderNameMap
                string constantExtendedHeaderValues = ExtractConstantExtendedHeaderValues(out nonConstantHeaderIDs, scanList.SurveyScans, scanList.FragScans, cColDelimiter);
                if (constantExtendedHeaderValues is null)
                    constantExtendedHeaderValues = string.Empty;

                // Write the constant extended stats values to a text file
                using (var writer = new StreamWriter(extendedConstantHeaderOutputFilePath, false))
                {
                    writer.WriteLine(constantExtendedHeaderValues);
                }

                // Now open another output file for the non-constant extended stats
                using (var writer = new StreamWriter(extendedNonConstantHeaderOutputFilePath, false))
                {
                    if (includeHeaders)
                    {
                        var headerNames = ConstructExtendedStatsHeaders();
                        writer.WriteLine(string.Join(Conversions.ToString(cColDelimiter), headerNames));
                    }

                    for (int scanIndex = 0; scanIndex <= scanList.MasterScanOrderCount - 1; scanIndex++)
                    {
                        var currentScan = GetScanByMasterScanIndex(scanList, scanIndex);
                        var dataColumns = ConcatenateExtendedStats(nonConstantHeaderIDs, mOptions.SICOptions.DatasetID, currentScan.ScanNumber, currentScan.ExtendedHeaderInfo);
                        writer.WriteLine(string.Join(Conversions.ToString(cColDelimiter), dataColumns));
                        if (scanIndex % 100 == 0)
                        {
                            UpdateProgress(Conversions.ToShort(scanIndex / (double)(scanList.MasterScanOrderCount - 1) * 100));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the Extended Scan Stats to: " + extendedNonConstantHeaderOutputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            UpdateProgress(100);
            return true;
        }

        public int GetExtendedHeaderInfoIdByName(string keyName)
        {
            int idValue;
            if (TryGetExtendedHeaderInfoValue(keyName, out idValue))
            {
            }
            // Match found
            else
            {
                // Match not found; add it
                idValue = mExtendedHeaderNameMap.Count;
                mExtendedHeaderNameMap.Add(new KeyValuePair<string, int>(keyName, idValue));
            }

            return idValue;
        }

        private bool TryGetExtendedHeaderInfoValue(string keyName, out int headerIndex)
        {
            var query = (from item in mExtendedHeaderNameMap
                         where (item.Key ?? "") == (keyName ?? "")
                         select item.Value).ToList();
            headerIndex = 0;
            if (query.Count == 0)
            {
                return false;
            }

            headerIndex = query.First();
            return true;
        }
    }
}