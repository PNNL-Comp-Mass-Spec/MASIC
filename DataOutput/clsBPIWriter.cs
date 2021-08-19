using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MASIC.Options;

namespace MASIC.DataOutput
{
    /// <summary>
    /// BPI Writer
    /// </summary>
    public class clsBPIWriter : clsMasicEventNotifier
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: Appodization, Extrel, fwhm, Hanning, Parzen, Welch, xtitle, ytitle

        // ReSharper restore CommentTypo

        private string DynamicValueToString(double value)
        {
            if (value < 100)
            {
                return PRISM.StringUtilities.ValueToString(value, 5);
            }

            if (value < 1000)
            {
                return PRISM.StringUtilities.DblToString(value, 3);
            }

            return PRISM.StringUtilities.DblToString(value, 2);
        }

        /// <summary>
        /// Save BPIs as an ICR-2LS compatible .TIC file (using only the MS1 scans),
        /// plus two Decon2LS compatible .CSV files (one for the MS1 scans and one for the MS2, MS3, etc. scans)
        /// </summary>
        /// <param name="scanList"></param>
        /// <param name="spectraCache"></param>
        /// <param name="inputFilePathFull"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <remarks>SaveExtendedScanStatsFiles() creates a tab-delimited text file with the BPI and TIC information for each scan</remarks>
        /// <returns>True if success, false if an error</returns>
        public bool SaveBPIs(
            clsScanList scanList,
            clsSpectraCache spectraCache,
            string inputFilePathFull,
            string outputDirectoryPath)
        {
            var currentFilePath = "_MS_scans.csv";

            try
            {
                const int bpiStepCount = 3;

                UpdateProgress(0, "Saving chromatograms to disk");

                var inputFileName = Path.GetFileName(inputFilePathFull);

                // Disabled in April 2015 since not used
                // ' First, write a true TIC file (in ICR-2LS format)
                // outputFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, OutputFileTypeConstants.ICRToolsTICChromatogramByScan)
                // LogMessage("Saving ICR Tools TIC to " + Path.GetFileName(outputFilePath))

                // SaveICRToolsChromatogramByScan(scanList.SurveyScans, scanList.SurveyScans.Count, outputFilePath, False, True, inputFilePathFull)

                var stepsCompleted = 1;
                UpdateProgress((short)(stepsCompleted / (double)bpiStepCount * 100));

                // Second, write an MS-based _scans.csv file (readable with Decon2LS)
                var msScansFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.OutputFileTypeConstants.DeconToolsMSChromatogramFile);
                currentFilePath = string.Copy(msScansFilePath);

                ReportMessage("Saving Decon2LS MS Chromatogram File to " + Path.GetFileName(msScansFilePath));

                SaveDecon2LSChromatogram(scanList.SurveyScans, spectraCache, msScansFilePath);

                stepsCompleted++;
                UpdateProgress((short)(stepsCompleted / (double)bpiStepCount * 100));

                // Third, write an MSMS-based _scans.csv file (readable with Decon2LS)
                var msmsScansFilePath = clsDataOutput.ConstructOutputFilePath(inputFileName, outputDirectoryPath, clsDataOutput.OutputFileTypeConstants.DeconToolsMSMSChromatogramFile);
                currentFilePath = string.Copy(msmsScansFilePath);

                ReportMessage("Saving Decon2LS MSMS Chromatogram File to " + Path.GetFileName(msmsScansFilePath));

                SaveDecon2LSChromatogram(scanList.FragScans, spectraCache, msmsScansFilePath);

                UpdateProgress(100);
                return true;
            }
            catch (Exception ex)
            {
                ReportError("Error writing the BPI to: " + currentFilePath, ex, clsMASIC.MasicErrorCodes.OutputFileWriteError);
                return false;
            }
        }

        private void SaveDecon2LSChromatogram(
            ICollection<clsScanInfo> scanList,
            clsSpectraCache spectraCache,
            string outputFilePath)
        {
            var scansWritten = 0;
            var lastStatus = DateTime.UtcNow;

            using var writer = new StreamWriter(outputFilePath);

            // Write the file headers
            WriteDecon2LSScanFileHeaders(writer);

            // Step through the scans and write each one
            foreach (var scanItem in scanList)
            {
                WriteDecon2LSScanFileEntry(writer, scanItem, spectraCache);

                if (scansWritten % 250 == 0)
                {
                    UpdateCacheStats(spectraCache);

                    if (DateTime.UtcNow.Subtract(lastStatus).TotalSeconds >= 30)
                    {
                        lastStatus = DateTime.UtcNow;
                        ReportMessage(string.Format("  {0} / {1} scans processed", scansWritten, scanList.Count));
                    }
                }

                scansWritten++;
            }
        }

        [Obsolete("No longer used")]
        private void SaveICRToolsChromatogramByScan(
            MASICOptions masicOptions,
            IList<clsScanInfo> scanList,
            int scanCount,
            string outputFilePath,
            bool saveElutionTimeInsteadOfScan,
            bool saveTICInsteadOfBPI,
            string inputFilePathFull)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                // ReSharper disable StringLiteralTypo

                // Write the Header text
                writer.WriteLine("ICR-2LS Data File (GA Anderson & JE Bruce); output from MASIC by Matthew E Monroe");
                writer.WriteLine("Version " + masicOptions.MASICVersion);
                writer.WriteLine("FileName:");
                if (saveTICInsteadOfBPI)
                {
                    writer.WriteLine("title:" + Path.GetFileName(inputFilePathFull) + " TIC");
                    writer.WriteLine("Ytitle:Amplitude (TIC)");
                }
                else
                {
                    writer.WriteLine("title:" + Path.GetFileName(inputFilePathFull) + " BPI");
                    writer.WriteLine("Ytitle:Amplitude (BPI)");
                }

                if (saveElutionTimeInsteadOfScan)
                {
                    writer.WriteLine("Xtitle:Time (Minutes)");
                }
                else
                {
                    writer.WriteLine("Xtitle:Scan #");
                }

                writer.WriteLine("Comment:");
                writer.WriteLine("LCQfilename: " + inputFilePathFull);
                writer.WriteLine();
                writer.WriteLine("CommentEnd");
                writer.WriteLine("FileType: 5 ");
                writer.WriteLine(" ValidTypes:1=Time,2=Freq,3=Mass;4=TimeSeriesWithCalibrationFn;5=XYPairs");
                writer.WriteLine("DataType: 3 ");
                writer.WriteLine(" ValidTypes:1=Integer,no header,2=Floats,Sun Extrel,3=Floats with header,4=Excite waveform");
                writer.WriteLine("Appodization: 0");
                writer.WriteLine(" ValidFunctions:0=Square,1=Parzen,2=Hanning,3=Welch");
                writer.WriteLine("ZeroFills: 0 ");

                // Since we're using XY pairs, the buffer length needs to be two times scanCount
                var bufferLength = scanCount * 2;
                if (bufferLength < 1)
                    bufferLength = 1;

                writer.WriteLine("NumberOfSamples: " + bufferLength + " ");
                writer.WriteLine("SampleRate: 1 ");
                writer.WriteLine("LowMassFreq: 0 ");
                writer.WriteLine("FreqShift: 0 ");
                writer.WriteLine("NumberSegments: 0 ");
                writer.WriteLine("MaxPoint: 0 ");
                writer.WriteLine("CalType: 0 ");
                writer.WriteLine("CalA: 108205311.2284 ");
                writer.WriteLine("CalB:-1767155067.018 ");
                writer.WriteLine("CalC: 29669467490280 ");
                writer.WriteLine("Intensity: 0 ");
                writer.WriteLine("CurrentXmin: 0 ");
                if (scanCount > 0)
                {
                    if (saveElutionTimeInsteadOfScan)
                    {
                        writer.WriteLine("CurrentXmax: " + scanList[scanCount - 1].ScanTime.ToString(CultureInfo.InvariantCulture) + " ");
                    }
                    else
                    {
                        writer.WriteLine("CurrentXmax: " + scanList[scanCount - 1].ScanNumber + " ");
                    }
                }
                else
                {
                    writer.WriteLine("CurrentXmax: 0");
                }

                writer.WriteLine("Tags:");
                writer.WriteLine("TagsEnd");
                writer.WriteLine("End");

                // ReSharper restore StringLiteralTypo
            }

            // Wait 500 msec, then re-open the file using Binary IO
            System.Threading.Thread.Sleep(500);

            using (var writer = new BinaryWriter(new FileStream(outputFilePath, FileMode.Append)))
            {
                // Write an Escape character (Byte 1B)
                writer.Write((byte)27);

                for (var scanIndex = 0; scanIndex < scanCount; scanIndex++)
                {
                    var scan = scanList[scanIndex];
                    // Note: Using (float) to assure that we write out single precision numbers

                    if (saveElutionTimeInsteadOfScan)
                    {
                        writer.Write((float)scan.ScanTime);
                    }
                    else
                    {
                        writer.Write((float)scan.ScanNumber);
                    }

                    if (saveTICInsteadOfBPI)
                    {
                        writer.Write(clsUtilities.CFloatSafe(scan.TotalIonIntensity));
                    }
                    else
                    {
                        writer.Write(clsUtilities.CFloatSafe(scan.BasePeakIonIntensity));
                    }
                }
            }
        }

        /// <summary>
        /// Write the header line for a _isos.csv file
        /// </summary>
        /// <param name="writer"></param>
        public void WriteDecon2LSIsosFileHeaders(StreamWriter writer)
        {
            // ReSharper disable once StringLiteralTypo
            var headerNames = new List<string>
            {
                "scan_num",
                "charge",
                "abundance",
                "mz",
                "fit",
                "average_mw",
                "monoisotopic_mw",
                "mostabundant_mw",
                "fwhm",
                "signal_noise",
                "mono_abundance",
                "mono_plus2_abundance"
            };

            writer.WriteLine(string.Join(", ", headerNames));
        }

        /// <summary>
        /// Append a data line to the _isos.csv file
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="scanNumber"></param>
        /// <param name="charge"></param>
        /// <param name="intensity"></param>
        /// <param name="ionMZ"></param>
        /// <param name="isoFit"></param>
        /// <param name="averageMass"></param>
        /// <param name="monoisotopicMass"></param>
        /// <param name="mostAbundantMass"></param>
        /// <param name="peakFWHM"></param>
        /// <param name="signalToNoise"></param>
        /// <param name="monoisotopicAbu"></param>
        /// <param name="monoPlus2Abu"></param>
        public void WriteDecon2LSIsosFileEntry(
            StreamWriter writer,
            int scanNumber,
            int charge,
            double intensity,
            double ionMZ,
            float isoFit,
            double averageMass,
            double monoisotopicMass,
            double mostAbundantMass,
            float peakFWHM,
            double signalToNoise,
            float monoisotopicAbu,
            float monoPlus2Abu)
        {
            var dataValues = new List<string>
            {
                scanNumber.ToString(),
                charge.ToString(),
                intensity.ToString("0.000"),
                ionMZ.ToString("0.00000"),
                isoFit.ToString(CultureInfo.InvariantCulture),
                averageMass.ToString("0.00000"),
                monoisotopicMass.ToString("0.00000"),
                mostAbundantMass.ToString("0.00000"),
                peakFWHM.ToString(CultureInfo.InvariantCulture),
                signalToNoise.ToString("0.000"),
                monoisotopicAbu.ToString(CultureInfo.InvariantCulture),
                monoPlus2Abu.ToString(CultureInfo.InvariantCulture)
            };

            writer.WriteLine(string.Join(",", dataValues));
        }

        /// <summary>
        /// Write the header line to the _scans.csv file
        /// </summary>
        /// <param name="writer"></param>
        public void WriteDecon2LSScanFileHeaders(StreamWriter writer)
        {
            // Old Headers:      "scan_num,time,type,num_isotopic_signatures,num_peaks,tic,bpi_mz,bpi,time_domain_signal,peak_intensity_threshold,peptide_intensity_threshold")

            var headerNames = new List<string>
            {
                "scan_num",
                "scan_time",
                "type",
                "bpi",
                "bpi_mz",
                "tic",
                "num_peaks",
                "num_deisotoped"
            };

            writer.WriteLine(string.Join(", ", headerNames));
        }

        private void WriteDecon2LSScanFileEntry(
            StreamWriter writer,
            clsScanInfo currentScan,
            clsSpectraCache spectraCache)
        {
            int numPeaks;

            if (spectraCache == null)
            {
                numPeaks = 0;
            }
            else
            {
                if (!spectraCache.GetSpectrum(currentScan.ScanNumber, out var spectrum, true))
                {
                    SetLocalErrorCode(clsMASIC.MasicErrorCodes.ErrorUncachingSpectrum);
                    return;
                }

                numPeaks = spectrum.IonCount;
            }

            var scanNumber = currentScan.ScanNumber;

            var msLevel = currentScan.FragScanInfo.MSLevel;
            if (msLevel < 1)
                msLevel = 1;

            const int numIsotopicSignatures = 0;

            WriteDecon2LSScanFileEntry(writer, currentScan, scanNumber, msLevel, numPeaks, numIsotopicSignatures);
        }

        /// <summary>
        /// Append a data line to the _scans.csv file
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="currentScan"></param>
        /// <param name="scanNumber"></param>
        /// <param name="msLevel"></param>
        /// <param name="numPeaks"></param>
        /// <param name="numIsotopicSignatures"></param>
        public void WriteDecon2LSScanFileEntry(
            StreamWriter writer,
            clsScanInfo currentScan,
            int scanNumber,
            int msLevel,
            int numPeaks,
            int numIsotopicSignatures)
        {
            var dataLine = new List<string>
            {
                scanNumber.ToString(),
                currentScan.ScanTime.ToString("0.0000"),
                msLevel.ToString(),
                DynamicValueToString(currentScan.BasePeakIonIntensity),
                currentScan.BasePeakIonMZ.ToString("0.00000"),
                DynamicValueToString(currentScan.TotalIonIntensity),
                numPeaks.ToString(),
                numIsotopicSignatures.ToString()
            };

            writer.WriteLine(string.Join(",", dataLine));
        }
    }
}
