using System;
using System.IO;
using ThermoRawFileReader;

namespace MASIC.DataOutput
{
    public class clsThermoMetadataWriter : clsMasicEventNotifier
    {
        public bool SaveMSMethodFile(
            XRawFileIO objXcaliburAccessor,
            clsDataOutput dataOutputHandler)
        {
            int instMethodCount;
            string outputFilePath = "?UndefinedFile?";

            try
            {
                instMethodCount = objXcaliburAccessor.FileInfo.InstMethods.Count;
            }
            catch (Exception ex)
            {
                ReportError("Error looking up InstMethod length in objXcaliburAccessor.FileInfo", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            try
            {
                for (int index = 0; index <= instMethodCount - 1; index++)
                {
                    string methodNum;
                    if (index == 0 && objXcaliburAccessor.FileInfo.InstMethods.Count == 1)
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
                        var fileInfo = objXcaliburAccessor.FileInfo;
                        writer.WriteLine("Instrument model: " + fileInfo.InstModel);
                        writer.WriteLine("Instrument name: " + fileInfo.InstName);
                        writer.WriteLine("Instrument description: " + fileInfo.InstrumentDescription);
                        writer.WriteLine("Instrument serial number: " + fileInfo.InstSerialNumber);
                        writer.WriteLine();

                        writer.WriteLine(objXcaliburAccessor.FileInfo.InstMethods[index]);
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
            XRawFileIO objXcaliburAccessor,
            clsDataOutput dataOutputHandler)
        {
            const char TAB_DELIMITER = '\t';

            int tuneMethodCount;
            string outputFilePath = "?UndefinedFile?";
            try
            {
                tuneMethodCount = objXcaliburAccessor.FileInfo.TuneMethods.Count;
            }
            catch (Exception ex)
            {
                ReportError("Error looking up TuneMethods length in objXcaliburAccessor.FileInfo", ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }

            try
            {
                for (int index = 0; index <= tuneMethodCount - 1; index++)
                {
                    string tuneInfoNum;
                    if (index == 0 && objXcaliburAccessor.FileInfo.TuneMethods.Count == 1)
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

                        foreach (udtTuneMethodSetting setting in objXcaliburAccessor.FileInfo.TuneMethods[index].Settings)
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
