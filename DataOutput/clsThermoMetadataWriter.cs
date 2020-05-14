using System;
using System.IO;
using ThermoRawFileReader;

namespace MASIC.DataOutput
{
    public class clsThermoMetadataWriter : clsMasicEventNotifier
    {
        public bool SaveMSMethodFile(
            XRawFileIO rawFileReader,
            clsDataOutput dataOutputHandler)
        {
            int instMethodCount;
            var outputFilePath = "?UndefinedFile?";

            try
            {
                instMethodCount = rawFileReader.FileInfo.InstMethods.Count;
            }
            catch (Exception ex)
            {
                ReportError("Error looking up InstMethod length in rawFileReader.FileInfo", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            try
            {
                for (var index = 0; index < instMethodCount; index++)
                {
                    string methodNum;
                    if (index == 0 && rawFileReader.FileInfo.InstMethods.Count == 1)
                    {
                        methodNum = string.Empty;
                    }
                    else
                    {
                        methodNum = (index + 1).ToString().Trim();
                    }

                    outputFilePath = dataOutputHandler.OutputFileHandles.MSMethodFilePathBase + methodNum + ".txt";

                    using (var writer = new StreamWriter(outputFilePath, false))
                    {
                        var fileInfo = rawFileReader.FileInfo;
                        writer.WriteLine("Instrument model: " + fileInfo.InstModel);
                        writer.WriteLine("Instrument name: " + fileInfo.InstName);
                        writer.WriteLine("Instrument description: " + fileInfo.InstrumentDescription);
                        writer.WriteLine("Instrument serial number: " + fileInfo.InstSerialNumber);
                        writer.WriteLine();

                        writer.WriteLine(rawFileReader.FileInfo.InstMethods[index]);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the MS Method to: " + outputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }

        public bool SaveMSTuneFile(
            XRawFileIO rawFileReader,
            clsDataOutput dataOutputHandler)
        {
            const char TAB_DELIMITER = '\t';

            int tuneMethodCount;
            var outputFilePath = "?UndefinedFile?";
            try
            {
                tuneMethodCount = rawFileReader.FileInfo.TuneMethods.Count;
            }
            catch (Exception ex)
            {
                ReportError("Error looking up TuneMethods length in rawFileReader.FileInfo", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            try
            {
                for (var index = 0; index < tuneMethodCount; index++)
                {
                    string tuneInfoNum;
                    if (index == 0 && rawFileReader.FileInfo.TuneMethods.Count == 1)
                    {
                        tuneInfoNum = string.Empty;
                    }
                    else
                    {
                        tuneInfoNum = (index + 1).ToString().Trim();
                    }

                    outputFilePath = dataOutputHandler.OutputFileHandles.MSTuneFilePathBase + tuneInfoNum + ".txt";

                    using (var writer = new StreamWriter(outputFilePath, false))
                    {
                        writer.WriteLine("Category" + TAB_DELIMITER + "Name" + TAB_DELIMITER + "Value");

                        foreach (var setting in rawFileReader.FileInfo.TuneMethods[index].Settings)
                            writer.WriteLine(setting.Category + TAB_DELIMITER + setting.Name + TAB_DELIMITER + setting.Value);
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError("Error writing the MS Tune Settings to: " + outputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            return true;
        }
    }
}
