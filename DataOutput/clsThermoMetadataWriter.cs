using System;
using System.IO;
using Microsoft.VisualBasic;
using ThermoRawFileReader;

namespace MASIC.DataOutput
{
    public class clsThermoMetadataWriter : clsMasicEventNotifier
    {
        public bool SaveMSMethodFile(XRawFileIO objXcaliburAccessor, clsDataOutput dataOutputHandler)
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
                        var withBlock = objXcaliburAccessor.FileInfo;
                        writer.WriteLine("Instrument model: " + withBlock.InstModel);
                        writer.WriteLine("Instrument name: " + withBlock.InstName);
                        writer.WriteLine("Instrument description: " + withBlock.InstrumentDescription);
                        writer.WriteLine("Instrument serial number: " + withBlock.InstSerialNumber);
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

        public bool SaveMSTuneFile(XRawFileIO objXcaliburAccessor, clsDataOutput dataOutputHandler)
        {
            const char cColDelimiter = ControlChars.Tab;
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
                        writer.WriteLine("Category" + cColDelimiter + "Name" + cColDelimiter + "Value");
                        var withBlock = objXcaliburAccessor.FileInfo.TuneMethods[index];
                        foreach (udtTuneMethodSetting setting in withBlock.Settings)
                            writer.WriteLine(setting.Category + cColDelimiter + setting.Name + cColDelimiter + setting.Value);
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