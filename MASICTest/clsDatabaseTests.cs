using System;
using MASIC;
using NUnit.Framework;

namespace MASICTest
{
    [TestFixture]
    public class DatabaseTests
    {
        private clsMASIC mMasic;
        private MASICPeakFinder.clsMASICPeakFinder mMASICPeakFinder;

        [OneTimeSetUp()]
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

            var connectionString = GetConnectionString("Gigasax", "DMS5", user, password);

            var options = new clsMASICOptions(mMasic.FileVersion, mMASICPeakFinder.ProgramVersion)
            {
                DatabaseConnectionString = connectionString
            };

            var dbAccessor = new clsDatabaseAccess(options);

            var datasetID = dbAccessor.LookupDatasetNumber(datasetName, strDatasetLookupFilePath, 1);

            Console.WriteLine("Data file " + datasetName + " is dataset ID " + datasetID);

            Assert.AreEqual(expectedDatasetID, datasetID, "DatasetID Mismatch");

        }

        private static string GetConnectionString(string server, string database, string user = "Integrated", string password = "")
        {
            if (string.Equals(user, "Integrated", StringComparison.OrdinalIgnoreCase))
                return string.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;", server, database);

            return string.Format("Data Source={0};Initial Catalog={1};User={2};Password={3};", server, database, user, password);
        }

    }
}
