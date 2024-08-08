using System;
using System.IO;
using System.Linq;
using MASIC.Options;
using PRISMDatabaseUtils;

namespace MASIC
{
    /// <summary>
    /// Class for obtaining data from the DMS database
    /// </summary>
    public class DatabaseAccess : MasicEventNotifier
    {
        // ReSharper disable UnusedMember.Global

        /// <summary>
        /// DMS connection string
        /// </summary>
        /// <remarks>
        /// The GUI uses this (see frmMain)
        /// </remarks>
        public const string DATABASE_CONNECTION_STRING_DEFAULT = "Host=prismdb2.emsl.pnl.gov;Port=5432;Database=dms;UserId=svc-dms";

        /// <summary>
        /// Query for obtaining dataset names and IDs
        /// </summary>
        /// <remarks>
        /// The GUI uses this (see frmMain)
        /// </remarks>
        public const string DATABASE_DATASET_INFO_QUERY_DEFAULT = "Select Dataset, ID FROM V_Dataset_Export";

        // ReSharper restore UnusedMember.Global

        private readonly MASICOptions mOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masicOptions"></param>
        public DatabaseAccess(MASICOptions masicOptions)
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
        public int LookupDatasetID(
            string inputFilePath,
            string datasetLookupFilePath,
            int defaultDatasetID)
        {
            var datasetName = Path.GetFileNameWithoutExtension(inputFilePath);
            int newDatasetID;

            // ReSharper disable once CommentTypo
            // SQL Server: Data Source=gigasax;Initial Catalog=DMS5;User=DMSReader;Password=...
            // PostgreSQL: Host=prismdb2.emsl.pnl.gov;Port=5432;Database=dms;UserId=svc-dms
            if (!string.IsNullOrWhiteSpace(mOptions.DatabaseConnectionString))
            {
                var datasetFoundInDB = GetDatasetIDFromDatabase(mOptions, datasetName, out newDatasetID);

                if (datasetFoundInDB)
                {
                    return newDatasetID;
                }
            }

            if (!string.IsNullOrWhiteSpace(datasetLookupFilePath))
            {
                var datasetFoundInFile = GetDatasetIDFromFile(datasetLookupFilePath, datasetName, out newDatasetID);

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
        /// <param name="newDatasetID"></param>
        /// <returns>Dataset ID</returns>
        private bool GetDatasetIDFromDatabase(MASICOptions masicOptions, string datasetName, out int newDatasetID)
        {
            const string avoidErrorMessage =
                "To avoid seeing this message in the future, clear the 'SQL Server Connection String' field " +
                "on the Advanced tab and save a new settings file. Alternatively, edit a MASIC parameter file " +
                "to have <item key=\"ConnectionString\" value=\"\" /> ";

            newDatasetID = 0;

            try
            {
                var applicationName = string.Format("MASIC_{0}", datasetName);
                var connectionStringToUse = DbToolsFactory.AddApplicationNameToConnectionString(masicOptions.DatabaseConnectionString, applicationName);

                var dbTools = DbToolsFactory.GetDBTools(connectionStringToUse);

                var queryingSingleDataset = false;

                for (var iteration = 1; iteration <= 2; iteration++)
                {
                    var sqlQuery = masicOptions.DatasetInfoQuerySql;

                    if (string.IsNullOrEmpty(sqlQuery))
                    {
                        sqlQuery = "SELECT dataset, id FROM V_Dataset_Export";
                    }

                    if (sqlQuery.StartsWith("SELECT dataset", StringComparison.OrdinalIgnoreCase))
                    {
                        // Add a where clause to the query
                        if (iteration == 1)
                        {
                            sqlQuery += " WHERE dataset = '" + datasetName + "'";
                            queryingSingleDataset = true;
                        }
                        else
                        {
                            sqlQuery += " WHERE dataset LIKE '" + datasetName + "%'";
                        }
                    }

                    var success = dbTools.GetQueryResults(sqlQuery, out var results);

                    if (success)
                    {
                        // Find the row in the lstResults that matches fileNameCompare
                        foreach (var datasetItem in results)
                        {
                            if (string.Equals(datasetItem[0], datasetName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Match found
                                if (int.TryParse(datasetItem[1], out newDatasetID))
                                {
                                    return true;
                                }

                                ReportError("Error converting Dataset ID '" + datasetItem[1] + "' to an integer", clsMASIC.MasicErrorCodes.InvalidDatasetID);

                                break;
                            }
                        }

                        if (results.Count > 0)
                        {
                            try
                            {
                                if (queryingSingleDataset || results.First()[0].StartsWith(datasetName))
                                {
                                    if (int.TryParse(results.First()[1], out newDatasetID))
                                    {
                                        return true;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                // Ignore errors here
                            }
                        }
                    }
                }

                return false;
            }
            catch (NullReferenceException)
            {
                ReportError("Error connecting to database: " + masicOptions.DatabaseConnectionString + Environment.NewLine + avoidErrorMessage, clsMASIC.MasicErrorCodes.InvalidDatasetID);
                return false;
            }
            catch (Exception ex)
            {
                ReportError("Error connecting to database: " + masicOptions.DatabaseConnectionString + Environment.NewLine + avoidErrorMessage, ex, clsMASIC.MasicErrorCodes.InvalidDatasetID);
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
        private bool GetDatasetIDFromFile(string datasetLookupFilePath, string datasetName, out int newDatasetId)
        {
            var delimiterList = new[] { ' ', ',', '\t' };

            newDatasetId = 0;

            try
            {
                using var reader = new StreamReader(datasetLookupFilePath);

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();

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

                    ReportError("Error converting Dataset ID '" + dataValues[1] + "' to an integer", clsMASIC.MasicErrorCodes.InvalidDatasetID);
                }

                return false;
            }
            catch (Exception ex)
            {
                ReportError("Error reading the dataset lookup file", ex, clsMASIC.MasicErrorCodes.InvalidDatasetLookupFilePath);
                return false;
            }
        }
    }
}
