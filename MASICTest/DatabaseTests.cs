using System;
using MASIC;
using MASIC.Options;
using NUnit.Framework;
using PRISMDatabaseUtils;

namespace MASICTest
{
    [TestFixture]
    public class DatabaseTests
    {
        private clsMASIC mMasic;
        private MASICPeakFinder.clsMASICPeakFinder mMASICPeakFinder;

        [OneTimeSetUp]
        public void Setup()
        {
            mMasic = new clsMASIC();

            mMASICPeakFinder = new MASICPeakFinder.clsMASICPeakFinder();
        }

        [Test]
        [TestCase("FakeNonexistentDataset.raw", 1)]
        [TestCase(@"c:\Temp\FakeNonexistentDataset.raw", 1)]
        [TestCase("QC_Shew_16_01_R1_23Mar17_Pippin_16-11-03", 571774)]
        [TestCase("QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)]
        [TestCase(@"\\Proto-x\Share\QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)]
        [TestCase("nBSA_Supernatant_1_21Jul09", 155993)]
        [Category("DatabaseIntegrated")]
        public void TestDatasetLookupIntegrated(string datasetName, int expectedDatasetID)
        {
            TestDatasetLookup(datasetName, expectedDatasetID, "Integrated", "");
        }

        [Test]
        [TestCase("FakeNonexistentDataset.raw", 1)]
        [TestCase(@"c:\Temp\FakeNonexistentDataset.raw", 1)]
        [TestCase("QC_Shew_16_01_R1_23Mar17_Pippin_16-11-03", 571774)]
        [TestCase("QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)]
        [TestCase(@"\\Proto-x\Share\QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)]
        [TestCase("nBSA_Supernatant_1_21Jul09", 155993)]
        [Category("DatabaseNamedUser")]
        public void TestDatasetLookupNamedUser(string datasetName, int expectedDatasetID)
        {
            TestDatasetLookup(datasetName, expectedDatasetID, "dmsreader", "dms4fun");
        }

        private void TestDatasetLookup(string datasetName, int expectedDatasetID, string user, string password)
        {
            const string strDatasetLookupFilePath = "";

            var connectionString = GetConnectionString("prismdb2.emsl.pnl.gov", "dms", true, user, password);

            var options = new MASICOptions(mMasic.FileVersion, mMASICPeakFinder.ProgramVersion)
            {
                DatabaseConnectionString = connectionString
            };

            var dbAccessor = new DatabaseAccess(options);

            var datasetID = dbAccessor.LookupDatasetID(datasetName, strDatasetLookupFilePath, 1);

            Console.WriteLine("Data file " + datasetName + " is dataset ID " + datasetID);

            Assert.AreEqual(expectedDatasetID, datasetID, "DatasetID Mismatch");
        }

        private static string GetConnectionString(string server, string database, bool isPostgres, string user = "Integrated", string password = "")
        {
            var dbType = isPostgres ? DbServerTypes.PostgreSQL : DbServerTypes.MSSQLServer;

            var useIntegratedSecurity = string.Equals(user, "Integrated", StringComparison.OrdinalIgnoreCase);

            return DbToolsFactory.GetConnectionString(
                dbType, server, database, user, password,
                "MASIC_DatabaseTests", useIntegratedSecurity);
        }
    }
}
