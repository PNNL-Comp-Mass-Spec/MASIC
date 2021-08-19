using System;
using System.Linq;

namespace MASIC
{
    /// <summary>
    /// Class for centroiding spectra
    /// </summary>
    public class Centroider
    {
        /// <summary>
        /// Centroid a profile mode spectrum using the ThermoFisher.CommonCore.Data centroiding logic
        /// </summary>
        /// <param name="scanInfo"></param>
        /// <param name="masses"></param>
        /// <param name="intensities"></param>
        /// <param name="centroidedPrecursorIonsMz"></param>
        /// <param name="centroidedPrecursorIonsIntensity"></param>
        public bool CentroidData(clsScanInfo scanInfo,
            double[] masses,
            double[] intensities,
            out double[] centroidedPrecursorIonsMz,
            out double[] centroidedPrecursorIonsIntensity)
        {
            const double massResolution = 10000;

            return CentroidData(scanInfo, masses, intensities, massResolution, out centroidedPrecursorIonsMz, out centroidedPrecursorIonsIntensity);
        }

        /// <summary>
        /// Centroid a profile mode spectrum using the ThermoFisher.CommonCore.Data centroiding logic
        /// </summary>
        /// <param name="scanInfo"></param>
        /// <param name="masses"></param>
        /// <param name="intensities"></param>
        /// <param name="massResolution"></param>
        /// <param name="centroidedPrecursorIonsMz"></param>
        /// <param name="centroidedPrecursorIonsIntensity"></param>
        public bool CentroidData(
            clsScanInfo scanInfo,
            double[] masses,
            double[] intensities,
            double massResolution,
            out double[] centroidedPrecursorIonsMz,
            out double[] centroidedPrecursorIonsIntensity)
        {
            try
            {
                var segmentedScan = ThermoFisher.CommonCore.Data.Business.SegmentedScan.FromMassesAndIntensities(masses, intensities);

                var scanStats = new ThermoFisher.CommonCore.Data.Business.ScanStatistics()
                {
                    PacketType = 2 + (2 << 16),
                    ScanNumber = scanInfo.ScanNumber,
                    StartTime = scanInfo.ScanTime,
                    BasePeakIntensity = scanInfo.BasePeakIonIntensity,
                    BasePeakMass = scanInfo.BasePeakIonMZ,
                    LowMass = masses.First(),
                    HighMass = masses.Last(),
                    TIC = scanInfo.TotalIonIntensity
                };

                var scan = new ThermoFisher.CommonCore.Data.Business.Scan()
                {
                    MassResolution = massResolution,
                    ScanType = scanInfo.ScanTypeName,
                    ToleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Ppm,     // Options are None, Amu, Mmu, Ppm
                    ScanStatistics = scanStats,
                    SegmentedScan = segmentedScan
                };

                var centroidScan = ThermoFisher.CommonCore.Data.Business.Scan.ToCentroid(scan);

                centroidedPrecursorIonsMz = centroidScan.PreferredMasses;
                centroidedPrecursorIonsIntensity = centroidScan.PreferredIntensities;

                return true;
            }
            catch (Exception)
            {
                centroidedPrecursorIonsMz = new double[1];
                centroidedPrecursorIonsIntensity = new double[1];
                return false;
            }
        }

        /// <summary>
        /// Calculates the mass tolerance for the profile peak
        /// </summary>
        /// <remarks>
        /// Uses the default tolerance factor, depending on isOrbitrapData
        /// </remarks>
        public double EstimateResolution(
            double mass,
            double defaultMassTolerance,
            bool isOrbitrapData)
        {
            var toleranceFactor = GetDefaultToleranceFactor(isOrbitrapData);
            var toleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Amu;

            return EstimateResolution(mass, toleranceFactor, defaultMassTolerance, isOrbitrapData, toleranceUnit);
        }

        /// <summary>
        /// Calculates the mass tolerance for the profile peak
        /// </summary>
        /// <param name="mass">Current mass tolerance value</param>
        /// <param name="toleranceFactor">Tolerance factor</param>
        /// <param name="defaultMassTolerance">Previous mass tolerance value</param>
        /// <param name="isOrbitrapData">True if processing LTQ-FT or Orbitrap data</param>
        /// <param name="toleranceUnit">tolerance unit</param>
        /// <returns>The calculated mass resolution for the profile peak</returns>
        public double EstimateResolution(
            double mass,
            double toleranceFactor,
            double defaultMassTolerance,
            bool isOrbitrapData,
            ThermoFisher.CommonCore.Data.Business.ToleranceMode toleranceUnit)
        {
            double massResolution;

            if (toleranceUnit == ThermoFisher.CommonCore.Data.Business.ToleranceMode.Ppm ||
                toleranceUnit == ThermoFisher.CommonCore.Data.Business.ToleranceMode.Mmu)
            {
                double massToleranceDa;
                if (toleranceUnit == ThermoFisher.CommonCore.Data.Business.ToleranceMode.Ppm)
                {
                    massToleranceDa = mass * 0.000001 * defaultMassTolerance;
                }
                else
                {
                    massToleranceDa = mass * 0.001 * defaultMassTolerance;
                }

                var deltaM = mass * mass * toleranceFactor;

                if (deltaM > massToleranceDa)
                {
                    massResolution = deltaM;
                }
                else
                {
                    massResolution = massToleranceDa;
                }
            }
            else if (isOrbitrapData)
            {
                massResolution = mass * Math.Sqrt(mass) * toleranceFactor;
            }
            else
            {
                massResolution = mass * mass * toleranceFactor;
            }

            return massResolution;
        }

        private double GetDefaultToleranceFactor(bool isOrbitrapData)
        {
            if (isOrbitrapData)
            {
                return 0.0000001;
            }

            return 0.000002;
        }
    }
}
