using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MASIC.Data;
using MASIC.Options;

namespace MASIC.DataOutput
{
    /// <summary>
    /// Extended scan stats writer
    /// </summary>
    public class ExtendedStatsWriter : MasicEventNotifier
    {
        // Ignore Spelling: frag, MASIC

        /// <summary>
        /// Collision mode column name
        /// </summary>
        public const string EXTENDED_STATS_HEADER_COLLISION_MODE = "Collision Mode";

        /// <summary>
        /// Scan filter text column name
        /// </summary>
        public const string EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT = "Scan Filter Text";

        /// <summary>
        /// Number of extended header names tracked by this class
        /// </summary>
        public int ExtendedHeaderNameCount => mExtendedHeaderNameMap.Count;

        /// <summary>
        /// Keys are strings of extended info names
        /// Values are the assigned ID value for the extended info name
        /// </summary>
        /// <remarks>The order of the values defines the appropriate output order for the names</remarks>
        private readonly List<KeyValuePair<string, int>> mExtendedHeaderNameMap;

        private readonly MASICOptions mOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExtendedStatsWriter(MASICOptions masicOptions)
        {
            mExtendedHeaderNameMap = new List<KeyValuePair<string, int>>();
            mOptions = masicOptions;
        }

        private IEnumerable<string> ConcatenateExtendedStats(
            IEnumerable<int> nonConstantHeaderIDs,
            int datasetID,
            int scanNumber,
            IReadOnlyDictionary<int, string> extendedHeaderInfo)
        {
            var dataValues = new List<string>(2 + mExtendedHeaderNameMap.Count)
            {
                datasetID.ToString(),
                scanNumber.ToString()
            };

            if (extendedHeaderInfo != null && nonConstantHeaderIDs != null)
            {
                foreach (var headerID in from item in nonConstantHeaderIDs orderby item select item)
                {
                    if (extendedHeaderInfo.TryGetValue(headerID, out var value))
                    {
                        if (double.TryParse(value, out var number))
                        {
                            if (Math.Abs(number) < float.Epsilon)
                                value = "0";
                        }
                        else
                        {
                            // ReSharper disable StringLiteralTypo
                            value = value switch
                            {
                                "ff" => "Off",
                                "n" => "On",
                                "eady" => "Ready",
                                "cquiring" => "Acquiring",
                                "oad" => "Load",
                                _ => value
                            };
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

        /// <summary>
        /// Construct the list of extended stat headers
        /// </summary>
        public List<string> ConstructExtendedStatsHeaders()
        {
            var cTrimChars = new[] { ':', ' ' };

            var headerNames = new List<string>(2)
            {
                "Dataset",
                "ScanNumber"
            };

            // Populate headerNames

            if (mExtendedHeaderNameMap.Count == 0)
            {
                return headerNames;
            }

            headerNames.Capacity = 2 + mExtendedHeaderNameMap.Count;

            var headerNamesByID = new Dictionary<int, string>();

            foreach (var item in mExtendedHeaderNameMap)
            {
                headerNamesByID.Add(item.Value, item.Key);
            }

            foreach (var headerItem in from item in headerNamesByID orderby item.Key select item.Value)
            {
                headerNames.Add(headerItem.TrimEnd(cTrimChars));
            }

            return headerNames;
        }

        private Dictionary<int, string> DeepCopyHeaderInfoDictionary(Dictionary<int, string> sourceTable)
        {
            var newTable = new Dictionary<int, string>();

            foreach (var item in sourceTable)
            {
                newTable.Add(item.Key, item.Value);
            }

            return newTable;
        }

        /// <summary>
        /// Looks through surveyScans and fragScans for ExtendedHeaderInfo values that are constant across all scans
        /// </summary>
        /// <remarks>mExtendedHeaderNameMap is updated so that constant header values are removed from it</remarks>
        /// <param name="nonConstantHeaderIDs">Output: the ID values of the header values that are not constant</param>
        /// <param name="surveyScans"></param>
        /// <param name="fragScans"></param>
        /// <param name="delimiter"></param>
        /// <returns>
        /// String that is a newline separated list of header values that are constant, tab delimited, and their constant values, also tab delimited
        /// Each line is in the form ParameterName_ColumnDelimiter_ParameterValue
        /// </returns>
        public string ExtractConstantExtendedHeaderValues(
            out List<int> nonConstantHeaderIDs,
            IList<ScanInfo> surveyScans,
            IList<ScanInfo> fragScans,
            char delimiter)
        {
            var cTrimChars = new[] { ':', ' ' };

            // Keys are ID values pointing to mExtendedHeaderNameMap (where the name is defined); values are the string or numeric values for the settings
            Dictionary<int, string> consolidatedValuesByID;

            var constantHeaderIDs = new List<int>();

            nonConstantHeaderIDs = new List<int>(mExtendedHeaderNameMap.Count);

            if (mExtendedHeaderNameMap.Count == 0)
            {
                return string.Empty;
            }

            // Initialize nonConstantHeaderIDs
            for (var i = 0; i < mExtendedHeaderNameMap.Count; i++)
            {
                nonConstantHeaderIDs.Add(i);
            }

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

            if (consolidatedValuesByID == null)
            {
                return string.Empty;
            }

            // Look for "Scan Filter Text" in mExtendedHeaderNameMap
            if (TryGetExtendedHeaderInfoValue(EXTENDED_STATS_HEADER_SCAN_FILTER_TEXT, out var scanFilterTextHeaderID))
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
                if (surveyScan.ExtendedHeaderInfo != null)
                {
                    foreach (var dataItem in surveyScan.ExtendedHeaderInfo)
                    {
                        if (consolidatedValuesByID.TryGetValue(dataItem.Key, out var value))
                        {
                            if (string.Equals(value, dataItem.Value))
                            {
                                // Value matches; nothing to do
                            }
                            else
                            {
                                // Value differs; remove the key from consolidatedValuesByID
                                consolidatedValuesByID.Remove(dataItem.Key);
                            }
                        }
                    }
                }
            }

            // Examine the values in .ExtendedHeaderInfo() in the fragmentation scans and compare them
            // to the values in consolidatedValuesByID, looking to see if they match
            foreach (var fragScan in fragScans)
            {
                if (fragScan.ExtendedHeaderInfo != null)
                {
                    foreach (var item in fragScan.ExtendedHeaderInfo)
                    {
                        if (consolidatedValuesByID.TryGetValue(item.Key, out var value))
                        {
                            if (string.Equals(value, item.Value))
                            {
                                // Value matches; nothing to do
                            }
                            else
                            {
                                // Value differs; remove key from consolidatedValuesByID
                                consolidatedValuesByID.Remove(item.Key);
                            }
                        }
                    }
                }
            }

            if (consolidatedValuesByID.Count == 0)
            {
                return string.Empty;
            }

            // Populate consolidatedValueList with the values in consolidatedValuesByID,
            // separating each header and value with a tab and separating each pair of values with a NewLine character

            // Need to first populate constantHeaderIDs with the ID values and sort the list so that the values are
            // stored in consolidatedValueList in the correct order

            var consolidatedValueList = new List<string>(30)
            {
                "Setting" + delimiter + "Value"
            };

            constantHeaderIDs.Capacity = consolidatedValuesByID.Count;

            foreach (var item in consolidatedValuesByID)
            {
                constantHeaderIDs.Add(item.Key);
            }

            var keysToRemove = new List<string>();

            foreach (var headerId in from item in constantHeaderIDs orderby item select item)
            {
                foreach (var item in mExtendedHeaderNameMap)
                {
                    if (item.Value == headerId)
                    {
                        consolidatedValueList.Add(item.Key.TrimEnd(cTrimChars) + delimiter + consolidatedValuesByID[headerId]);
                        keysToRemove.Add(item.Key);
                        break;
                    }
                }
            }

            // Remove the elements from mExtendedHeaderNameMap that were included in consolidatedValueList;
            // we couldn't remove these above since that would invalidate the iHeaderEnum enumerator

            foreach (var keyName in keysToRemove)
            {
                for (var headerIndex = 0; headerIndex < mExtendedHeaderNameMap.Count; headerIndex++)
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
            {
                nonConstantHeaderIDs.Add(item.Value);
            }

            return string.Join(Environment.NewLine, consolidatedValueList);
        }

        private ScanInfo GetScanByMasterScanIndex(ScanList scanList, int masterScanIndex)
        {
            var currentScan = new ScanInfo();

            if (scanList.MasterScanOrder == null)
                return currentScan;

            if (masterScanIndex < 0)
            {
                masterScanIndex = 0;
            }
            else if (masterScanIndex >= scanList.MasterScanOrderCount)
            {
                masterScanIndex = scanList.MasterScanOrderCount - 1;
            }

            switch (scanList.MasterScanOrder[masterScanIndex].ScanType)
            {
                case ScanList.ScanTypeConstants.SurveyScan:
                    // Survey scan
                    currentScan = scanList.SurveyScans[scanList.MasterScanOrder[masterScanIndex].ScanIndexPointer];
                    break;

                case ScanList.ScanTypeConstants.FragScan:
                    // Fragmentation Scan
                    currentScan = scanList.FragScans[scanList.MasterScanOrder[masterScanIndex].ScanIndexPointer];
                    break;

                default:
                    // Unknown scan type
                    break;
            }

            return currentScan;
        }

        /// <summary>
        /// Create the extended scan stats file
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <param name="includeHeaders"></param>
        /// <returns>True if successful, false if an error</returns>
        public bool SaveExtendedScanStatsFiles(
            ScanList scanList,
            string inputFileName,
            string outputDirectoryPath,
            bool includeHeaders)
        {
            // Writes out a flat file containing the extended scan stats

            var extendedNonConstantHeaderOutputFilePath = string.Empty;

            const char TAB_DELIMITER = '\t';

            try
            {
                UpdateProgress(0, "Saving extended scan stats to flat file");

                var extendedConstantHeaderOutputFilePath = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.ScanStatsExtendedConstantFlatFile);
                extendedNonConstantHeaderOutputFilePath = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.ScanStatsExtendedFlatFile);

                ReportMessage("Saving extended scan stats flat file to disk: " + Path.GetFileName(extendedNonConstantHeaderOutputFilePath));

                if (mExtendedHeaderNameMap.Count == 0)
                {
                    // No extended stats to write; exit the function
                    UpdateProgress(100);
                    return true;
                }

                // Lookup extended stats values that are constants for all scans
                // The following will also remove the constant header values from mExtendedHeaderNameMap
                var constantExtendedHeaderValues = ExtractConstantExtendedHeaderValues(
                    out var nonConstantHeaderIDs,
                    scanList.SurveyScans,
                    scanList.FragScans,
                    TAB_DELIMITER) ?? string.Empty;

                // Write the constant extended stats values to a text file

                // ReSharper disable once RemoveRedundantBraces
                using (var constantHeaderWriter = new StreamWriter(extendedConstantHeaderOutputFilePath, false))
                {
                    constantHeaderWriter.WriteLine(constantExtendedHeaderValues);
                }

                // Now open another output file for the non-constant extended stats
                using var writer = new StreamWriter(extendedNonConstantHeaderOutputFilePath, false);

                if (includeHeaders)
                {
                    var headerNames = ConstructExtendedStatsHeaders();
                    writer.WriteLine(string.Join(TAB_DELIMITER.ToString(), headerNames));
                }

                for (var scanIndex = 0; scanIndex < scanList.MasterScanOrderCount; scanIndex++)
                {
                    var currentScan = GetScanByMasterScanIndex(scanList, scanIndex);

                    var dataColumns = ConcatenateExtendedStats(nonConstantHeaderIDs, mOptions.SICOptions.DatasetID, currentScan.ScanNumber, currentScan.ExtendedHeaderInfo);
                    writer.WriteLine(string.Join(TAB_DELIMITER.ToString(), dataColumns));

                    if (scanIndex % 100 == 0)
                    {
                        UpdateProgress((short)(scanIndex / (double)(scanList.MasterScanOrderCount - 1) * 100));
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the Extended Scan Stats to: " + extendedNonConstantHeaderOutputFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }

            UpdateProgress(100);
            return true;
        }

        /// <summary>
        /// Get extended header info ID by extended header name
        /// </summary>
        /// <param name="keyName"></param>
        public int GetExtendedHeaderInfoIdByName(string keyName)
        {
            if (TryGetExtendedHeaderInfoValue(keyName, out var idValue))
            {
                // Match found
            }
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
            var query = (from item in mExtendedHeaderNameMap where (item.Key ?? "") == (keyName ?? "") select item.Value).ToList();
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
