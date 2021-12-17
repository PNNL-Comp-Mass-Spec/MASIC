using System;
using System.IO;
using MASIC.Data;
using MASIC.Options;

namespace MASIC.DataOutput
{
    /// <summary>
    /// Spectrum data writer
    /// </summary>
    public class SpectrumDataWriter : MasicEventNotifier
    {
        private readonly BPIWriter mBPIWriter;
        private readonly MASICOptions mOptions;

        /// <summary>
        /// Constructor
        /// </summary>
        public SpectrumDataWriter(BPIWriter bpiWriter, MASICOptions masicOptions)
        {
            mBPIWriter = bpiWriter;
            mOptions = masicOptions;
        }

        /// <summary>
        /// Export raw data to a .pek file or .csv file
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="inputFileName"></param>
        /// <param name="outputDirectoryPath"></param>
        public bool ExportRawDataToDisk(
            ScanList scanList,
            SpectraCache spectraCache,
            string inputFileName,
            string outputDirectoryPath)
        {
            var outputFilePath = "??";

            try
            {
                StreamWriter dataWriter;
                StreamWriter scanInfoWriter;

                switch (mOptions.RawDataExportOptions.FileFormat)
                {
                    case RawDataExportOptions.ExportRawDataFileFormatConstants.PEKFile:
                        outputFilePath = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.PEKFile);
                        dataWriter = new StreamWriter(outputFilePath);
                        scanInfoWriter = null;
                        break;
                    case RawDataExportOptions.ExportRawDataFileFormatConstants.CSVFile:
                        outputFilePath = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.DeconToolsIsosFile);

                        var outputFilePath2 = DataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, DataOutput.OutputFileTypeConstants.DeconToolsScansFile);

                        dataWriter = new StreamWriter(outputFilePath);
                        scanInfoWriter = new StreamWriter(outputFilePath2);

                        // Write the file headers
                        mBPIWriter.WriteDecon2LSIsosFileHeaders(dataWriter);
                        mBPIWriter.WriteDecon2LSScanFileHeaders(scanInfoWriter);
                        break;

                    default:
                        // Unknown format
                        ReportError("Unknown raw data file format: " + mOptions.RawDataExportOptions.FileFormat);
                        return false;
                }

                var spectrumExportCount = 0;

                mOptions.RawDataExportOptions.RenumberScans = !mOptions.RawDataExportOptions.IncludeMSMS && mOptions.RawDataExportOptions.RenumberScans;

                UpdateProgress(0, "Exporting raw data");

                for (var masterOrderIndex = 0; masterOrderIndex < scanList.MasterScanOrderCount; masterOrderIndex++)
                {
                    var scanPointer = scanList.MasterScanOrder[masterOrderIndex].ScanIndexPointer;
                    if (scanList.MasterScanOrder[masterOrderIndex].ScanType == ScanList.ScanTypeConstants.SurveyScan)
                    {
                        SaveRawDataToDiskWork(dataWriter, scanInfoWriter, scanList.SurveyScans[scanPointer], spectraCache, inputFileName, false, ref spectrumExportCount);
                    }
                    else if (mOptions.RawDataExportOptions.IncludeMSMS ||
                        scanList.FragScans[scanPointer].MRMScanType != ThermoRawFileReader.MRMScanTypeConstants.NotMRM)
                    {
                        // Either we're writing out MS/MS data or this is an MRM scan
                        SaveRawDataToDiskWork(dataWriter, scanInfoWriter, scanList.FragScans[scanPointer], spectraCache, inputFileName, true, ref spectrumExportCount);
                    }

                    if (scanList.MasterScanOrderCount > 1)
                    {
                        UpdateProgress((short)(masterOrderIndex / (double)(scanList.MasterScanOrderCount - 1) * 100));
                    }
                    else
                    {
                        UpdateProgress(0);
                    }

                    UpdateCacheStats(spectraCache);

                    if (mOptions.AbortProcessing)
                    {
                        break;
                    }
                }

                dataWriter.Close();
                scanInfoWriter?.Close();

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error writing the raw spectra data to: " + outputFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        private void SaveCSVFilesToDiskWork(
            StreamWriter dataWriter,
            StreamWriter scanInfoWriter,
            ScanInfo currentScan,
            SpectraCache spectraCache,
            bool fragmentationScan,
            ref int spectrumExportCount)
        {
            int scanNumber;

            if (!spectraCache.GetSpectrum(currentScan.ScanNumber, out var spectrum, true))
            {
                SetLocalErrorCode(clsMASIC.MasicErrorCodes.ErrorUncachingSpectrum);
                return;
            }

            spectrumExportCount++;

            // First, write an entry to the "_scans.csv" file

            if (mOptions.RawDataExportOptions.RenumberScans)
            {
                scanNumber = spectrumExportCount;
            }
            else
            {
                scanNumber = currentScan.ScanNumber;
            }

            int msLevel;
            if (fragmentationScan)
            {
                msLevel = currentScan.FragScanInfo.MSLevel;
            }
            else
            {
                msLevel = 1;
            }

            const int numIsotopicSignatures = 0;
            var numPeaks = spectrum.IonCount;

            var baselineNoiseLevel = currentScan.BaselineNoiseStats.NoiseLevel;
            if (baselineNoiseLevel < 1)
                baselineNoiseLevel = 1;

            mBPIWriter.WriteDecon2LSScanFileEntry(scanInfoWriter, currentScan, scanNumber, msLevel, numPeaks, numIsotopicSignatures);

            // Now write an entry to the "_isos.csv" file

            if (spectrum.IonCount > 0)
            {
                // Populate intensities and pointerArray()

                var intensities = new double[spectrum.IonCount];
                var pointerArray = new int[spectrum.IonCount];
                for (var ionIndex = 0; ionIndex < spectrum.IonCount; ionIndex++)
                {
                    intensities[ionIndex] = spectrum.IonsIntensity[ionIndex];
                    pointerArray[ionIndex] = ionIndex;
                }

                // Sort pointerArray() based on the intensities in intensities
                Array.Sort(intensities, pointerArray);

                int startIndex;
                if (mOptions.RawDataExportOptions.MaxIonCountPerScan > 0)
                {
                    // Possibly limit the number of ions to maxIonCount
                    startIndex = spectrum.IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan;
                    if (startIndex < 0)
                        startIndex = 0;
                }
                else
                {
                    startIndex = 0;
                }

                // Define the minimum data point intensity value
                var minimumIntensityCurrentScan = spectrum.IonsIntensity[pointerArray[startIndex]];

                // Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum);

                // If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, possibly update minimumIntensityCurrentScan
                if (mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0)
                {
                    minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio);
                }

                for (var ionIndex = 0; ionIndex < spectrum.IonCount; ionIndex++)
                {
                    if (spectrum.IonsIntensity[ionIndex] >= minimumIntensityCurrentScan)
                    {
                        const int charge = 1;
                        const int isoFit = 0;
                        var mass = Utilities.ConvoluteMass(spectrum.IonsMZ[ionIndex], 1, 0);
                        const int peakFWHM = 0;
                        var signalToNoise = spectrum.IonsIntensity[ionIndex] / baselineNoiseLevel;
                        const int monoisotopicAbu = -10;
                        const int monoPlus2Abu = -10;

                        mBPIWriter.WriteDecon2LSIsosFileEntry(
                            dataWriter, scanNumber, charge,
                            spectrum.IonsIntensity[ionIndex], spectrum.IonsMZ[ionIndex],
                            isoFit, mass, mass, mass,
                            peakFWHM, signalToNoise, monoisotopicAbu, monoPlus2Abu);
                    }
                }
            }
        }

        private void SavePEKFileToDiskWork(
            TextWriter writer,
            ScanInfo currentScan,
            SpectraCache spectraCache,
            string inputFileName,
            bool fragmentationScan,
            ref int spectrumExportCount)
        {
            var exportCount = 0;

            if (!spectraCache.GetSpectrum(currentScan.ScanNumber, out var spectrum, true))
            {
                SetLocalErrorCode(clsMASIC.MasicErrorCodes.ErrorUncachingSpectrum);
                return;
            }

            spectrumExportCount++;

            // Store the base peak ion intensity as the time domain signal level value
            writer.WriteLine("{0}\t{1:0.000}", "Time domain signal level:", currentScan.BasePeakIonIntensity);

            writer.WriteLine("MASIC " + mOptions.MASICVersion);                     // Software version
            var pekFileInfoLine = "MS/MS-based PEK file";
            if (mOptions.RawDataExportOptions.IncludeMSMS)
            {
                pekFileInfoLine += " (includes both survey scans and fragmentation spectra)";
            }
            else
            {
                pekFileInfoLine += " (includes only survey scans)";
            }

            writer.WriteLine(pekFileInfoLine);

            int scanNumber;
            if (mOptions.RawDataExportOptions.RenumberScans)
            {
                scanNumber = spectrumExportCount;
            }
            else
            {
                scanNumber = currentScan.ScanNumber;
            }

            var fileInfoLine = "Filename: " + inputFileName + "." + scanNumber.ToString("00000");
            writer.WriteLine(fileInfoLine);

            if (fragmentationScan)
            {
                writer.WriteLine("ScanType: Fragmentation Scan");
            }
            else
            {
                writer.WriteLine("ScanType: Survey Scan");
            }

            writer.WriteLine("Charge state mass transform results:");
            writer.WriteLine("First CS,    Number of CS,   Abundance,   Mass,   Standard deviation");

            if (spectrum.IonCount > 0)
            {
                // Populate intensities and pointerArray()

                var intensities = new double[spectrum.IonCount];
                var pointerArray = new int[spectrum.IonCount];
                for (var ionIndex = 0; ionIndex < spectrum.IonCount; ionIndex++)
                {
                    intensities[ionIndex] = spectrum.IonsIntensity[ionIndex];
                    pointerArray[ionIndex] = ionIndex;
                }

                // Sort pointerArray() based on the intensities in intensities
                Array.Sort(intensities, pointerArray);

                int startIndex;

                if (mOptions.RawDataExportOptions.MaxIonCountPerScan > 0)
                {
                    // Possibly limit the number of ions to maxIonCount
                    startIndex = spectrum.IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan;
                    if (startIndex < 0)
                        startIndex = 0;
                }
                else
                {
                    startIndex = 0;
                }

                // Define the minimum data point intensity value
                var minimumIntensityCurrentScan = spectrum.IonsIntensity[pointerArray[startIndex]];

                // Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum);

                // If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, possibly update minimumIntensityCurrentScan
                if (mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0)
                {
                    minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio);
                }

                exportCount = 0;
                for (var ionIndex = 0; ionIndex < spectrum.IonCount; ionIndex++)
                {
                    if (spectrum.IonsIntensity[ionIndex] < minimumIntensityCurrentScan)
                        continue;

                    writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}",
                        "1",
                        "1",
                        spectrum.IonsIntensity[ionIndex],
                        spectrum.IonsMZ[ionIndex],
                        "0");

                    exportCount++;
                }
            }

            writer.WriteLine("Number of peaks in spectrum = " + spectrum.IonCount);
            writer.WriteLine("Number of isotopic distributions detected = " + exportCount);
            writer.WriteLine();
        }

        private void SaveRawDataToDiskWork(
            StreamWriter dataWriter,
            StreamWriter scanInfoWriter,
            ScanInfo currentScan,
            SpectraCache spectraCache,
            string inputFileName,
            bool fragmentationScan,
            ref int spectrumExportCount)
        {
            switch (mOptions.RawDataExportOptions.FileFormat)
            {
                case RawDataExportOptions.ExportRawDataFileFormatConstants.PEKFile:
                    SavePEKFileToDiskWork(dataWriter, currentScan, spectraCache, inputFileName, fragmentationScan, ref spectrumExportCount);
                    break;

                case RawDataExportOptions.ExportRawDataFileFormatConstants.CSVFile:
                    SaveCSVFilesToDiskWork(dataWriter, scanInfoWriter, currentScan, spectraCache, fragmentationScan, ref spectrumExportCount);
                    break;

                default:
                    // Unknown format
                    // This code should never be reached
                    break;
            }
        }
    }
}
