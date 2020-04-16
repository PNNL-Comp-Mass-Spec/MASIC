using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;

namespace MASIC
{
    public class clsDatabaseAccess : clsMasicEventNotifier
    {

        #region // TODO
        // frmMain uses these constants

        // ReSharper disable UnusedMember.Global

        public const string DATABASE_CONNECTION_STRING_DEFAULT = "Data Source=gigasax;Initial Catalog=DMS5;User=DMSReader;Password=dms4fun";
        public const string DATABASE_DATASET_INFO_QUERY_DEFAULT = "Select Dataset, ID FROM V_Dataset_Export";

        // ReSharper restore UnusedMember.Global

        #endregion
        #region // TODO
        private readonly clsMASICOptions mOptions;
        #endregion
        /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="masicOptions"></param>
        public clsDatabaseAccess(clsMASICOptions masicOptions)
        {
            mOptions = masicOptions;
        }

        /// <summary>
    /// Lookup the dataset ID given the dataset name
    /// First contacts the database using the specified connection string and query
    /// If not found, looks for the dataset name in the file specified by mDatasetLookupFilePath
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="datasetLookupFilePath"></param>
    /// <param name="defaultDatasetID"></param>
    /// <returns></returns>
        public int LookupDatasetID(string inputFilePath, string datasetLookupFilePath, int defaultDatasetID)
        {
            string datasetName = Path.GetFileNameWithoutExtension(inputFilePath);
            int newDatasetID;

            // ReSharper disable once CommentTypo
            // Data Source=gigasax;Initial Catalog=DMS5;User=DMSReader;Password=...
            if (!string.IsNullOrWhiteSpace(mOptions.DatabaseConnectionString))
            {
                bool datasetFoundInDB = GetDatasetIDFromDatabase(mOptions, datasetName, out newDatasetID);
                if (datasetFoundInDB)
                {
                    return newDatasetID;
                }
            }

            if (!string.IsNullOrWhiteSpace(datasetLookupFilePath))
            {
                bool datasetFoundInFile = GetDatasetIDFromFile(datasetLookupFilePath, datasetName, out newDatasetID);
                if (datasetFoundInFile)
                {
                    return newDatasetID;
                }
            }

            return defaultDatasetID;
        }

        /// <summary>
    /// Attempt to lookup the Dataset ID in the database
    /// </summary>
    /// <param name="masicOptions"></param>
    /// <param name="datasetName"></param>
    /// <returns></returns>
        private bool GetDatasetIDFromDatabase(clsMASICOptions masicOptions, string datasetName, out int newDatasetID)
        {
            string avoidErrorMessage = "To avoid seeing this message in the future, clear the 'SQL Server Connection String' and " + "'Dataset Info Query SQL' entries on the Advanced tab and save a new settings file. " + "Alternatively, edit a MASIC parameter file to remove the text after the equals sign " + "for parameters ConnectionString and DatasetInfoQuerySql.";
            newDatasetID = 0;
            try
            {
                var dbTools = PRISMDatabaseUtils.DbToolsFactory.GetDBTools(masicOptions.DatabaseConnectionString);
                bool queryingSingleDataset = false;
                for (int iteration = 1; iteration <= 2; iteration++)
                {
                    string sqlQuery = masicOptions.DatasetInfoQuerySql;
                    if (string.IsNullOrEmpty(sqlQuery))
                    {
                        sqlQuery = "Select Dataset, ID FROM V_Dataset_Export";
                    }

                    if (sqlQuery.StartsWith("SELECT Dataset", StringComparison.OrdinalIgnoreCase))
                    {
                        // Add a where clause to the query
                        if (iteration == 1)
                        {
                            sqlQuery += " WHERE Dataset = '" + datasetName + "'";
                            queryingSingleDataset = true;
                        }
                        else
                        {
                            sqlQuery += " WHERE Dataset Like '" + datasetName + "%'";
                        }
                    }

                    List<List<string>> lstResults = null;
                    var success = dbTools.GetQueryResults(sqlQuery, out lstResults);
                    if (success)
                    {

                        // Find the row in the lstResults that matches fileNameCompare
                        foreach (var datasetItem in lstResults)
                        {
                            if (string.Equals(datasetItem[0], datasetName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Match found
                                if (int.TryParse(datasetItem[1], out newDatasetID))
                                {
                                    return true;
                                }
                                else
                                {
                                    ReportError("Error converting Dataset ID '" + datasetItem[1] + "' to an integer", clsMASIC.eMasicErrorCodes.InvalidDatasetID);
                                }

                                break;
                            }
                        }

                        if (lstResults.Count > 0)
                        {
                            try
                            {
                                if (queryingSingleDataset || lstResults.First()[0].StartsWith(datasetName))
                                {
                                    if (int.TryParse(lstResults.First()[1], out newDatasetID))
                                    {
                                        return true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Ignore errors here
                            }
                        }
                    }
                }

                return false;
            }
            catch (NullReferenceException ex2)
            {
                ReportError("Error connecting to database: " + masicOptions.DatabaseConnectionString + ControlChars.NewLine + avoidErrorMessage, clsMASIC.eMasicErrorCodes.InvalidDatasetID);
                return false;
            }
            catch (Exception ex)
            {
                ReportError("Error connecting to database: " + masicOptions.DatabaseConnectionString + ControlChars.NewLine + avoidErrorMessage, ex, clsMASIC.eMasicErrorCodes.InvalidDatasetID);
                return false;
            }
        }

        /// <summary>
    /// Lookup the dataset ID in the dataset lookup file
    /// This is a comma, space, or tab delimited file with two columns: Dataset Name and Dataset ID
    /// </summary>
    /// <param name="datasetLookupFilePath"></param>
    /// <param name="datasetName"></param>
    /// <param name="newDatasetId"></param>
    /// <returns></returns>
        private bool GetDatasetIDFromFile(string datasetLookupFilePath, string datasetName, out int newDatasetId)
        {
            var delimiterList = new char[] { ' ', ',', ControlChars.Tab };
            newDatasetId = 0;
            try
            {
                using (var reader = new StreamReader(datasetLookupFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string dataLine = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(dataLine))
                        {
                            continue;
                        }

                        if (dataLine.Length < datasetName.Length)
                        {
                            continue;
                        }

                        var dataValues = dataLine.Split(delimiterList);
                        if (dataValues.Length < 2)
                        {
                            continue;
                        }

                        if (!string.Equals(dataValues[0], datasetName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (int.TryParse(dataValues[1], out newDatasetId))
                        {
                            return true;
                        }
                        else
                        {
                            ReportError("Error converting Dataset ID '" + dataValues[1] + "' to an integer", clsMASIC.eMasicErrorCodes.InvalidDatasetID);
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ReportError("Error reading the dataset lookup file", ex, clsMASIC.eMasicErrorCodes.InvalidDatasetLookupFilePath);
                return false;
            }
        }
    }
}