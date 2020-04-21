using System;
using System.IO;

namespace MASIC.DataOutput
{
    public class clsSpectrumDataWriter : clsMasicEventNotifier
    {
        #region "Classwide variables"
        private readonly clsBPIWriter mBPIWriter;
        private readonly clsMASICOptions mOptions;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public clsSpectrumDataWriter(clsBPIWriter bpiWriter, clsMASICOptions masicOptions)
        {
            mBPIWriter = bpiWriter;
            mOptions = masicOptions;
        }

        public bool ExportRawDataToDisk(
            clsScanList scanList,
            clsSpectraCache spectraCache,
            string inputFileName,
            string outputDirectoryPath)
        {
            string outputFilePath = "??";

            try
            {
                StreamWriter dataWriter;
                StreamWriter scanInfoWriter;

                switch (mOptions.RawDataExportOptions.FileFormat)
                {
                    case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile:
                        outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.PEKFile);
                        dataWriter = new StreamWriter(outputFilePath);
                        scanInfoWriter = null;
                        break;
                    case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile:
                        outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.DeconToolsIsosFile);

                        string outputFilePath2 = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.eOutputFileTypeConstants.DeconToolsScansFile);

                        dataWriter = new StreamWriter(outputFilePath);
                        scanInfoWriter = new StreamWriter(outputFilePath2);

                        // Write the file headers
                        mBPIWriter.WriteDecon2LSIsosFileHeaders(dataWriter);
                        mBPIWriter.WriteDecon2LSScanFileHeaders(scanInfoWriter);
                        break;

                    default:
                        // Unknown format
                        ReportError("Unknown raw data file format: " + mOptions.RawDataExportOptions.FileFormat.ToString());
                        return false;
                }

                int spectrumExportCount = 0;

                if (!mOptions.RawDataExportOptions.IncludeMSMS && mOptions.RawDataExportOptions.RenumberScans)
                {
                    mOptions.RawDataExportOptions.RenumberScans = true;
                }
                else
                {
                    mOptions.RawDataExportOptions.RenumberScans = false;
                }

                UpdateProgress(0, "Exporting raw data");

                for (int masterOrderIndex = 0; masterOrderIndex <= scanList.MasterScanOrderCount - 1; masterOrderIndex++)
                {
                    int scanPointer = scanList.MasterScanOrder[masterOrderIndex].ScanIndexPointer;
                    if (scanList.MasterScanOrder[masterOrderIndex].ScanType == clsScanList.eScanTypeConstants.SurveyScan)
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
                        UpdateProgress(Convert.ToInt16(masterOrderIndex / (double)(scanList.MasterScanOrderCount - 1) * 100));
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

                if (dataWriter != null)
                    dataWriter.Close();
                if (scanInfoWriter != null)
                    scanInfoWriter.Close();

                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error writing the raw spectra data to: " + outputFilePath, ex, clsMASIC.eMasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        private void SaveCSVFilesToDiskWork(
            StreamWriter dataWriter,
            StreamWriter scanInfoWriter,
            clsScanInfo currentScan,
            clsSpectraCache spectraCache,
            bool fragmentationScan,
            ref int spectrumExportCount)
        {
            int poolIndex;
            int scanNumber;
            double baselineNoiseLevel;

            if (!spectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, out poolIndex))
            {
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum);
                return;
            }

            spectrumExportCount += 1;

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

            int numIsotopicSignatures = 0;
            int numPeaks = spectraCache.SpectraPool[poolIndex].IonCount;

            baselineNoiseLevel = currentScan.BaselineNoiseStats.NoiseLevel;
            if (baselineNoiseLevel < 1)
                baselineNoiseLevel = 1;

            mBPIWriter.WriteDecon2LSScanFileEntry(scanInfoWriter, currentScan, scanNumber, msLevel, numPeaks, numIsotopicSignatures);

            var spectraPool = spectraCache.SpectraPool[poolIndex];
            // Now write an entry to the "_isos.csv" file

            if (spectraPool.IonCount > 0)
            {
                // Populate intensities and pointerArray()

                double[] intensities;
                int[] pointerArray;

                intensities = new double[spectraPool.IonCount];
                pointerArray = new int[spectraPool.IonCount];
                for (int ionIndex = 0; ionIndex <= spectraPool.IonCount - 1; ionIndex++)
                {
                    intensities[ionIndex] = spectraPool.IonsIntensity[ionIndex];
                    pointerArray[ionIndex] = ionIndex;
                }

                // Sort pointerArray() based on the intensities in intensities
                Array.Sort(intensities, pointerArray);

                int startIndex;
                if (mOptions.RawDataExportOptions.MaxIonCountPerScan > 0)
                {
                    // Possibly limit the number of ions to maxIonCount
                    startIndex = spectraPool.IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan;
                    if (startIndex < 0)
                        startIndex = 0;
                }
                else
                {
                    startIndex = 0;
                }

                // Define the minimum data point intensity value
                double minimumIntensityCurrentScan = spectraPool.IonsIntensity[pointerArray[startIndex]];

                // Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum);

                // If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update minimumIntensityCurrentScan
                if (mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0)
                {
                    minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio);
                }

                for (int ionIndex = 0; ionIndex <= spectraPool.IonCount - 1; ionIndex++)
                {
                    if (spectraPool.IonsIntensity[ionIndex] >= minimumIntensityCurrentScan)
                    {
                        int charge = 1;
                        int isoFit = 0;
                        double mass = clsUtilities.ConvoluteMass(spectraPool.IonsMZ[ionIndex], 1, 0);
                        int peakFWHM = 0;
                        double signalToNoise = spectraPool.IonsIntensity[ionIndex] / baselineNoiseLevel;
                        int monoisotopicAbu = -10;
                        int monoPlus2Abu = -10;

                        mBPIWriter.WriteDecon2LSIsosFileEntry(
                            dataWriter, scanNumber, charge,
                            spectraPool.IonsIntensity[ionIndex], spectraPool.IonsMZ[ionIndex],
                            isoFit, mass, mass, mass,
                            peakFWHM, signalToNoise, monoisotopicAbu, monoPlus2Abu);
                    }
                }
            }
        }

        private void SavePEKFileToDiskWork(
            TextWriter writer,
            clsScanInfo currentScan,
            clsSpectraCache spectraCache,
            string inputFileName,
            bool fragmentationScan,
            ref int spectrumExportCount)
        {
            int poolIndex;
            int exportCount = 0;

            if (!spectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, out poolIndex))
            {
                SetLocalErrorCode(clsMASIC.eMasicErrorCodes.ErrorUncachingSpectrum);
                return;
            }

            spectrumExportCount += 1;

            writer.WriteLine("Time domain signal level:" + "\t" + currentScan.BasePeakIonIntensity.ToString("0.000"));          // Store the base peak ion intensity as the time domain signal level value

            writer.WriteLine("MASIC " + mOptions.MASICVersion);                     // Software version
            string dataLine = "MS/MS-based PEK file";
            if (mOptions.RawDataExportOptions.IncludeMSMS)
            {
                dataLine += " (includes both survey scans and fragmentation spectra)";
            }
            else
            {
                dataLine += " (includes only survey scans)";
            }

            writer.WriteLine(dataLine);

            int scanNumber;
            if (mOptions.RawDataExportOptions.RenumberScans)
            {
                scanNumber = spectrumExportCount;
            }
            else
            {
                scanNumber = currentScan.ScanNumber;
            }

            dataLine = "Filename: " + inputFileName + "." + scanNumber.ToString("00000");
            writer.WriteLine(dataLine);

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

            var spectraPool = spectraCache.SpectraPool[poolIndex];

            if (spectraPool.IonCount > 0)
            {
                // Populate intensities and pointerArray()
                double[] intensities;
                int[] pointerArray;

                intensities = new double[spectraPool.IonCount];
                pointerArray = new int[spectraPool.IonCount];
                for (int ionIndex = 0; ionIndex <= spectraPool.IonCount - 1; ionIndex++)
                {
                    intensities[ionIndex] = spectraPool.IonsIntensity[ionIndex];
                    pointerArray[ionIndex] = ionIndex;
                }

                // Sort pointerArray() based on the intensities in intensities
                Array.Sort(intensities, pointerArray);

                int startIndex;

                if (mOptions.RawDataExportOptions.MaxIonCountPerScan > 0)
                {
                    // Possibly limit the number of ions to maxIonCount
                    startIndex = spectraPool.IonCount - mOptions.RawDataExportOptions.MaxIonCountPerScan;
                    if (startIndex < 0)
                        startIndex = 0;
                }
                else
                {
                    startIndex = 0;
                }

                // Define the minimum data point intensity value
                double minimumIntensityCurrentScan = spectraPool.IonsIntensity[pointerArray[startIndex]];

                // Update the minimum intensity if a higher minimum intensity is defined in .IntensityMinimum
                minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, mOptions.RawDataExportOptions.IntensityMinimum);

                // If mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio is > 0, then possibly update minimumIntensityCurrentScan
                if (mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio > 0)
                {
                    minimumIntensityCurrentScan = Math.Max(minimumIntensityCurrentScan, currentScan.BaselineNoiseStats.NoiseLevel * mOptions.RawDataExportOptions.MinimumSignalToNoiseRatio);
                }

                exportCount = 0;
                for (int ionIndex = 0; ionIndex <= spectraPool.IonCount - 1; ionIndex++)
                {
                    if (spectraPool.IonsIntensity[ionIndex] >= minimumIntensityCurrentScan)
                    {
                        string dataLine1 =
                            "1" + "\t" +
                            "1" + "\t" +
                            spectraPool.IonsIntensity[ionIndex] + "\t" +
                            spectraPool.IonsMZ[ionIndex] + "\t" +
                            "0";

                        writer.WriteLine(dataLine1);
                        exportCount += 1;
                    }
                }
            }

            writer.WriteLine("Number of peaks in spectrum = " + spectraPool.IonCount.ToString());
            writer.WriteLine("Number of isotopic distributions detected = " + exportCount.ToString());
            writer.WriteLine();
        }

        private void SaveRawDataToDiskWork(
            StreamWriter dataWriter,
            StreamWriter scanInfoWriter,
            clsScanInfo currentScan,
            clsSpectraCache spectraCache,
            string inputFileName,
            bool fragmentationScan,
            ref int spectrumExportCount)
        {
            switch (mOptions.RawDataExportOptions.FileFormat)
            {
                case clsRawDataExportOptions.eExportRawDataFileFormatConstants.PEKFile:
                    SavePEKFileToDiskWork(dataWriter, currentScan, spectraCache, inputFileName, fragmentationScan, ref spectrumExportCount);
                    break;
                case clsRawDataExportOptions.eExportRawDataFileFormatConstants.CSVFile:
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
