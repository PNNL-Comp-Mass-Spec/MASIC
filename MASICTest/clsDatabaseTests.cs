using System;
using MASIC;
using NUnit.Framework;

namespace MASICTest
{
    [TestFixture]
    public class DatabaseTests
    {

        private clsMASICOptions mOptions;

        private clsDatabaseAccess mDBAccessor;

        [OneTimeSetUp()]
        public void Setup()
        {
            var oMasic = new clsMASIC();

            var oMASICPeakFinder = new MASICPeakFinder.clsMASICPeakFinder();

            mOptions = new clsMASICOptions(oMasic.FileVersion, oMASICPeakFinder.ProgramVersion)
            {
                DatabaseConnectionString = "Data Source=gigasax;Initial Catalog=DMS5;Integrated Security=true"
            };

            mDBAccessor = new clsDatabaseAccess(mOptions);

        }

        [Test()]
        [TestCase("FakeNonexistentDataset.raw", 1)]
        [TestCase(@"c:\Temp\FakeNonexistentDataset.raw", 1)]
        [TestCase("QC_Shew_16_01_R1_23Mar17_Pippin_16-11-03", 571774)]
        [TestCase("QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)]
        [TestCase(@"\\Proto-x\Share\QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)]
        [TestCase("nBSA_Supernatant_1_21Jul09", 155993)]
        public void TestDatasetLookup(string datasetName, int expectedDatasetID)
        {
            const string strDatasetLookupFilePath = "";

            var datasetID = mDBAccessor.LookupDatasetNumber(datasetName, strDatasetLookupFilePath, 1);

            Console.WriteLine("Data file " + datasetName + " is dataset ID " + datasetID);

            Assert.AreEqual(expectedDatasetID, datasetID, "DatasetID Mismatch");

        }

    }
}
