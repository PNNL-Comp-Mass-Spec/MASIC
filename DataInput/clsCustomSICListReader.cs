using System;
using System.Collections.Generic;
using System.IO;

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

        private enum eCustomSICFileColumns
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

        #region "Classwide Variables"

        private readonly clsCustomSICList mCustomSICList;

        #endregion

        public static string GetCustomMZFileColumnHeaders(
                                                          string delimiter = ", ",
                                                          bool includeAndBeforeLastItem = true)
        {
            var headerNames = new List<string>()
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

        public bool LoadCustomSICListFromFile(string customSICValuesFileName)
        {
            var delimiterList = new char[] { '\t' };
            var forceAcquisitionTimeMode = false;

            try
            {
                var mzHeaderFound = false;
                var scanTimeHeaderFound = false;
                var timeToleranceHeaderFound = false;

                mCustomSICList.ResetMzSearchValues();

                // eColumnMapping will be initialized when the headers are read
                var eColumnMapping = new int[0];

                if (!File.Exists(customSICValuesFileName))
                {
                    // Custom SIC file not found
                    var errorMessage = "Custom MZ List file not found: " + customSICValuesFileName;
                    ReportError(errorMessage);
                    mCustomSICList.CustomMZSearchValues.Clear();
                    return false;
                }

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
                            delimiterList = new char[] { ',' };
                        }

                        var dataCols = dataLine.Split(delimiterList);

                        if (dataCols == null || dataCols.Length <= 0)
                            continue;

                        // This is the first non-blank line
                        linesRead += 1;

                        if (linesRead == 1)
                        {
                            // Initialize eColumnMapping, setting the value for each column to -1, indicating the column is not present
                            eColumnMapping = new int[dataCols.Length];
                            for (var colIndex = 0; colIndex <= eColumnMapping.Length - 1; colIndex++)
                                eColumnMapping[colIndex] = -1;

                            // The first row must be the header row; parse the values
                            for (var colIndex = 0; colIndex <= dataCols.Length - 1; colIndex++)
                            {
                                var colName = dataCols[colIndex];
                                if (colName.Equals(CUSTOM_SIC_COLUMN_MZ, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int)eCustomSICFileColumns.MZ;
                                    mzHeaderFound = true;
                                }
                                else if (colName.Equals(CUSTOM_SIC_COLUMN_MZ_TOLERANCE, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int)eCustomSICFileColumns.MZToleranceDa;
                                }
                                else if (colName.Equals(CUSTOM_SIC_COLUMN_SCAN_CENTER, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int)eCustomSICFileColumns.ScanCenter;
                                }
                                else if (colName.Equals(CUSTOM_SIC_COLUMN_SCAN_TOLERANCE, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int)eCustomSICFileColumns.ScanTolerance;
                                }
                                else if (colName.Equals(CUSTOM_SIC_COLUMN_SCAN_TIME, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int)eCustomSICFileColumns.ScanTime;
                                    scanTimeHeaderFound = true;
                                }
                                else if (colName.Equals(CUSTOM_SIC_COLUMN_TIME_TOLERANCE, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int)eCustomSICFileColumns.TimeTolerance;
                                    timeToleranceHeaderFound = true;
                                }
                                else if (colName.Equals(CUSTOM_SIC_COLUMN_COMMENT, StringComparison.OrdinalIgnoreCase))
                                {
                                    eColumnMapping[colIndex] = (int) eCustomSICFileColumns.Comment;
                                }
                                else
                                {
                                    // Unknown column name; ignore it
                                }
                            }

                            // Make sure that, at a minimum, the MZ column is present
                            if (!mzHeaderFound)
                            {
                                var errorMessage = "Custom M/Z List file " + customSICValuesFileName + "does not have a column header named " + CUSTOM_SIC_COLUMN_MZ + " in the first row; this header is required (valid column headers are: " + GetCustomMZFileColumnHeaders() + ")";
                                ReportError(errorMessage);

                                mCustomSICList.CustomMZSearchValues.Clear();
                                return false;
                            }

                            if (scanTimeHeaderFound && timeToleranceHeaderFound)
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


                        for (var colIndex = 0; colIndex <= dataCols.Length - 1; colIndex++)
                        {
                            if (colIndex >= eColumnMapping.Length)
                                break;
                            switch (eColumnMapping[colIndex])
                            {
                                case (int)eCustomSICFileColumns.MZ:
                                    if (!double.TryParse(dataCols[colIndex], out var mz))
                                    {
                                        throw new InvalidCastException("Non-numeric value for the MZ column in row " + (linesRead + 1) + ", column " + (colIndex + 1));
                                    }

                                    mzSearchSpec.MZ = mz;
                                    break;

                                case (int)eCustomSICFileColumns.MZToleranceDa:
                                    if (!double.TryParse(dataCols[colIndex], out var mzTolDa))
                                    {
                                        throw new InvalidCastException("Non-numeric value for the MZToleranceDa column in row " + (linesRead + 1) + ", column " + (colIndex + 1));
                                    }

                                    mzSearchSpec.MZToleranceDa = mzTolDa;
                                    break;

                                case (int)eCustomSICFileColumns.ScanCenter:
                                    // Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                    if (!forceAcquisitionTimeMode)
                                    {
                                        if (!float.TryParse(dataCols[colIndex], out var scanActTimeCenter))
                                        {
                                            throw new InvalidCastException("Non-numeric value for the ScanCenter column in row " + (linesRead + 1) + ", column " + (colIndex + 1));
                                        }

                                        mzSearchSpec.ScanOrAcqTimeCenter = scanActTimeCenter;
                                    }

                                    break;

                                case (int)eCustomSICFileColumns.ScanTolerance:
                                    // Do not use this value if both the ScanTime and the TimeTolerance columns were present
                                    if (!forceAcquisitionTimeMode)
                                    {
                                        if (mCustomSICList.ScanToleranceType == clsCustomSICList.eCustomSICScanTypeConstants.Absolute)
                                        {
                                            mzSearchSpec.ScanOrAcqTimeTolerance = int.Parse(dataCols[colIndex]);
                                        }
                                        else
                                        {
                                            // Includes .Relative and .AcquisitionTime
                                            if (!float.TryParse(dataCols[colIndex], out var scanAcqTimeTol))
                                            {
                                                throw new InvalidCastException("Non-numeric value for the ScanTolerance column in row " + (linesRead + 1) + ", column " + (colIndex + 1));
                                            }

                                            mzSearchSpec.ScanOrAcqTimeTolerance = scanAcqTimeTol;
                                        }
                                    }

                                    break;

                                case (int)eCustomSICFileColumns.ScanTime:
                                    // Only use this value if both the ScanTime and the TimeTolerance columns were present
                                    if (forceAcquisitionTimeMode)
                                    {
                                        if (!float.TryParse(dataCols[colIndex], out var scanActTimeCenter))
                                        {
                                            throw new InvalidCastException("Non-numeric value for the ScanTime column in row " + (linesRead + 1) + ", column " + (colIndex + 1));
                                        }

                                        mzSearchSpec.ScanOrAcqTimeCenter = scanActTimeCenter;
                                    }

                                    break;

                                case (int)eCustomSICFileColumns.TimeTolerance:
                                    // Only use this value if both the ScanTime and the TimeTolerance columns were present
                                    if (forceAcquisitionTimeMode)
                                    {
                                        if (!float.TryParse(dataCols[colIndex], out var scanAcqTimeTol))
                                        {
                                            throw new InvalidCastException("Non-numeric value for the TimeTolerance column in row " + (linesRead + 1) + ", column " + (colIndex + 1));
                                        }

                                        mzSearchSpec.ScanOrAcqTimeTolerance = scanAcqTimeTol;
                                    }

                                    break;

                                case (int)eCustomSICFileColumns.Comment:
                                    mzSearchSpec.Comment = string.Copy(dataCols[colIndex]);
                                    break;
                                default:
                                    // Unknown column code
                                    break;
                            }
                        }

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
    }
}
