Imports MASIC
Imports NUnit.Framework

Public Class DatabaseTests

    Private mOptions As clsMASICOptions

    Private mDBAccessor As clsDatabaseAccess

    <OneTimeSetUp>
    Public Sub Setup()
        Dim oMasic = New clsMASIC()

        Dim oMASICPeakFinder = New MASICPeakFinder.clsMASICPeakFinder()

        mOptions = New clsMASICOptions(oMasic.FileVersion(), oMASICPeakFinder.ProgramVersion)

        mOptions.DatabaseConnectionString = "Data Source=gigasax;Initial Catalog=DMS5;Integrated Security=true"

        mDBAccessor = New clsDatabaseAccess(mOptions)


    End Sub

    <Test>
    <TestCase("FakeNonexistentDataset.raw", 1)>
    <TestCase("c:\Temp\FakeNonexistentDataset.raw", 1)>
    <TestCase("QC_Shew_16_01_R1_23Mar17_Pippin_16-11-03", 571774)>
    <TestCase("QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)>
    <TestCase("\\Proto-x\Share\QC_Shew_16_01-500ng_3b_4Apr16_Falcon_16-01-09", 482564)>
    <TestCase("nBSA_Supernatant_1_21Jul09", 155993)>
    Public Sub TestDatasetLookup(datasetName As String, expectedDatasetID As Integer)

        Const strDatasetLookupFilePath = ""

        Dim datasetID = mDBAccessor.LookupDatasetNumber(datasetName, strDatasetLookupFilePath, 1)

        Console.WriteLine("Data file " & datasetName & " is dataset ID " & datasetID)

        Assert.AreEqual(expectedDatasetID, datasetID, "DatasetID Mismatch")

    End Sub

End Class