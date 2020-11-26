using System;
using System.Collections.Generic;
using System.IO;
using PRISMDatabaseUtils;

namespace MASIC.DataInput
{
    public class clsCustomSICListReader : clsMasicEventNotifier
    {
        #region "Constants and Enums"

        public const string CUSTOM_SIC_COLUMN_MZ = "MZ";
        public const string CUSTOM_SIC_COLUMN_MZ_TOLERANCE = "MZToleranceDa";
        public const string CUSTOM_SIC_COLUMN_SCAN_CENTER = "ScanCenter";
        public const string CUSTOM_SIC_COLUMN_SCAN_TOLERANCE = "ScanTolerance";
        public const string CUSTOM_SIC_COLUMN_SCAN_TIME = "ScanTime";
        public const string CUSTOM_SIC_COLUMN_TIME_TOLERANCE = "TimeTolerance";
        public const string CUSTOM_SIC_COLUMN_COMMENT = "Comment";

        private enum CustomSICFileColumns
        {
            MZ = 0,
            MZToleranceDa = 1,
            ScanCenter = 2,              // Absolute scan or Relative Scan, or Acquisition Time
            ScanTolerance = 3,           // Absolute scan or Relative Scan, or Acquisition Time
            ScanTime = 4,                // Only used for acquisition Time
            TimeTolerance = 5,           // Only used for acquisition Time
            Comment = 6
        }

        #endregion

        #region "Class wide Variables"

        private readonly clsCustomSICList mCustomSICList;

        #endregion

        /// <summary>
        /// Get the default header names for a custom SIC list file
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="includeAndBeforeLastItem"></param>
        /// <returns></returns>
        public static string GetCustomMZFileColumnHeaders(
            string delimiter = ", ",
            bool includeAndBeforeLastItem = true)
        {
            var headerNames = new List<string>(8)
            {
                CUSTOM_SIC_COLUMN_MZ,
                CUSTOM_SIC_COLUMN_MZ_TOLERANCE,
                CUSTOM_SIC_COLUMN_SCAN_CENTER,
                CUSTOM_SIC_COLUMN_SCAN_TOLERANCE,
                CUSTOM_SIC_COLUMN_SCAN_TIME,
                CUSTOM_SIC_COLUMN_TIME_TOLERANCE
            };

            if (includeAndBeforeLastItem)
            {
                headerNames.Add("and " + CUSTOM_SIC_COLUMN_COMMENT);
            }
            else
            {
                headerNames.Add(CUSTOM_SIC_COLUMN_COMMENT);
            }

            return string.Join(delimiter, headerNames);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsCustomSICListReader(clsCustomSICList customSicList)
        {
            mCustomSICList = customSicList;
        }

        /// <summary>
        /// Load a custom SIC list file
        /// </summary>
        /// <param name="customSICValuesFileName"></param>
        /// <returns></returns>
        public bool LoadCustomSICListFromFile(string customSICValuesFileName)
        {
            var delimiterList = new[] { '\t' };
            var forceAcquisitionTimeMode = false;

            try
            {
                mCustomSICList.ResetMzSearchValues();

                // Keys in this dictionary are column identifier
                // Values are the index of this column in the tab-delimited text file (-1 if not present)
                var columnMap = new Dictionary<CustomSICFileColumns, int>();

                var columnNamesByIdentifier = new Dictionary<CustomSICFileColumns, SortedSet<string>>();
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.MZ, CUSTOM_SIC_COLUMN_MZ);
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.MZToleranceDa, CUSTOM_SIC_COLUMN_MZ_TOLERANCE);
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.ScanCenter, CUSTOM_SIC_COLUMN_SCAN_CENTER);
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.ScanTolerance, CUSTOM_SIC_COLUMN_SCAN_TOLERANCE);
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.ScanTime, CUSTOM_SIC_COLUMN_SCAN_TIME);
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.TimeTolerance, CUSTOM_SIC_COLUMN_TIME_TOLERANCE);
                DataTableUtils.AddColumnNamesForIdentifier(columnNamesByIdentifier, CustomSICFileColumns.Comment, CUSTOM_SIC_COLUMN_COMMENT);

                if (!File.Exists(customSICValuesFileName))
                {
                    // Custom SIC file not found
                    var errorMessage = "Custom MZ List file not found: " + customSICValuesFileName;
                    ReportError(errorMessage);
                    mCustomSICList.CustomMZSearchValues.Clear();
                    return false;
                }

                // Do not throw exceptions when a column is not present
                // GetColumnValue will simply return the default value
                DataTableUtils.GetColumnValueThrowExceptions = false;

                using (var reader = new StreamReader(new FileStream(customSICValuesFileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var linesRead = 0;
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;

                        if (linesRead == 0 && !dataLine.Contains("\t"))
                        {
                            // Split on commas instead of tab characters
                            delimiterList = new[] { ',' };
                        }

                        var dataCols = dataLine.Split(delimiterList);

                        if (dataCols.Length == 0)
                            continue;

                        linesRead++;

                        if (linesRead == 1)
                        {
                            // This is the first non-blank line; parse the headers

                            DataTableUtils.GetColumnMappingFromHeaderLine(columnMap, dataLine, columnNamesByIdentifier);

                            // Make sure that, at a minimum, the MZ column is present
                            if (DataTableUtils.GetColumnIndex(columnMap, CustomSICFileColumns.MZ) < 0 )
                            {
                                var errorMessage = "Custom M/Z List file " + customSICValuesFileName + "does not have a column header named " + CUSTOM_SIC_COLUMN_MZ + " in the first row; this header is required (valid column headers are: " + GetCustomMZFileColumnHeaders() + ")";
                                ReportError(errorMessage);

                                mCustomSICList.CustomMZSearchValues.Clear();
                                return false;
                            }

                            if (DataTableUtils.GetColumnIndex(columnMap, CustomSICFileColumns.ScanTime) >= 0 &&
                                DataTableUtils.GetColumnIndex(columnMap, CustomSICFileColumns.TimeTolerance) >= 0)
                            {
                                forceAcquisitionTimeMode = true;
                                mCustomSICList.ScanToleranceType = clsCustomSICList.eCustomSICScanTypeConstants.AcquisitionTime;
                            }
                            else
                            {
                                forceAcquisitionTimeMode = false;
                            }

                            continue;
                        }

                        // Parse this line's data if dataCols(0) is numeric
                        if (!clsUtilities.IsNumber(dataCols[0]))
                        {
                            continue;
                        }

                        var mzSearchSpec = new clsCustomMZSearchSpec(0)
                        {
                            MZToleranceDa = 0,
                            ScanOrAcqTimeCenter = 0,
                            ScanOrAcqTimeTolerance = 0
                        };

                        var mzTarget = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.MZ, string.Empty, out var mzValuePresent);
                        if (mzValuePresent)
                        {
                            mzSearchSpec.MZ = ParseDouble(mzTarget, "MZ", linesRead);
                        }

                        var mzToleranceDa = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.MZToleranceDa, string.Empty, out var mzTolerancePresent);
                        if (mzTolerancePresent)
                        {
                            mzSearchSpec.MZToleranceDa = ParseDouble(mzToleranceDa, "MZToleranceDa", linesRead);
                        }

                        var scanCenter = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.ScanCenter, string.Empty, out var scanCenterPresent);
                        if (scanCenterPresent)
                        {
                            // Do not use this value if both the ScanTime and the TimeTolerance columns were present
                            if (!forceAcquisitionTimeMode)
                            {
                                mzSearchSpec.ScanOrAcqTimeCenter = (float)ParseDouble(scanCenter, "ScanCenter", linesRead);
                            }
                        }

                        var scanTolerance = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.ScanTolerance, string.Empty, out var scanTolPresent);
                        if (scanTolPresent)
                        {
                            // Do not use this value if both the ScanTime and the TimeTolerance columns were present
                            if (!forceAcquisitionTimeMode)
                            {
                                if (mCustomSICList.ScanToleranceType == clsCustomSICList.eCustomSICScanTypeConstants.Absolute)
                                {
                                    mzSearchSpec.ScanOrAcqTimeTolerance = int.Parse(scanTolerance);
                                }
                                else
                                {
                                    // Includes .Relative and .AcquisitionTime
                                    mzSearchSpec.ScanOrAcqTimeTolerance = (float)ParseDouble(scanTolerance, "ScanTolerance", linesRead);
                                }
                            }
                        }

                        var scanTime = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.ScanTime, string.Empty, out var scanTimePresent);
                        if (scanTimePresent)
                        {
                            // Only use this value if both the ScanTime and the TimeTolerance columns were present
                            if (forceAcquisitionTimeMode)
                            {
                                mzSearchSpec.ScanOrAcqTimeCenter = (float)ParseDouble(scanTime, "ScanTime", linesRead);
                            }
                        }

                        var timeTolerance = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.TimeTolerance, string.Empty, out var timeTolerancePresent);
                        if (timeTolerancePresent)
                        {
                            // Only use this value if both the ScanTime and the TimeTolerance columns were present
                            if (forceAcquisitionTimeMode)
                            {
                                mzSearchSpec.ScanOrAcqTimeTolerance = (float)ParseDouble(timeTolerance, "TimeTolerance", linesRead);
                            }
                        }

                        mzSearchSpec.Comment = DataTableUtils.GetColumnValue(dataCols, columnMap, CustomSICFileColumns.Comment);

                        mCustomSICList.AddMzSearchTarget(mzSearchSpec);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error in LoadCustomSICListFromFile", ex, clsMASIC.eMasicErrorCodes.InvalidCustomSICValues);
                mCustomSICList.CustomMZSearchValues.Clear();
                return false;
            }

            if (!forceAcquisitionTimeMode)
            {
                mCustomSICList.ValidateCustomSICList();
            }

            return true;
        }

        /// <summary>
        /// Parse a double value from a string
        /// </summary>
        /// <param name="valueText"></param>
        /// <param name="columnName"></param>
        /// <param name="rowNumber"></param>
        /// <returns></returns>
        /// <remarks>Raises an exception if unable to parse</remarks>
        private double ParseDouble(string valueText, string columnName, int rowNumber)
        {
            if (!double.TryParse(valueText, out var value))
            {
                throw new InvalidCastException(string.Format(
                    "Non-numeric value for the {0} column in the custom SIC list file, line {1}: {2}",
                    columnName, rowNumber, valueText));
            }

            return value;
        }
    }
}
