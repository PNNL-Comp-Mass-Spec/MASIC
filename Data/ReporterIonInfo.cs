﻿namespace MASIC.Data
{
    /// <summary>
    /// Information for a single reporter ion
    /// </summary>
    public class ReporterIonInfo
    {
        // Ignore Spelling: Daltons, immonium, MASIC

        /// <summary>
        /// Reporter ion m/z
        /// </summary>
        public double MZ { get; }

        /// <summary>
        /// Tolerance (in Daltons) for looking for this m/z in spectra
        /// </summary>
        public double MZToleranceDa { get; set; }

        /// <summary>
        /// Ion number (1-based)
        /// </summary>
        /// <remarks>This is used for sorting the reporter ions when creating text files and plots</remarks>
        public int IonNumber { get; }

        /// <summary>
        /// Should be False for Reporter Ions and True for other ions, e.g. immonium loss from phenylalanine
        /// </summary>
        public bool ContaminantIon { get; set; }

        /// <summary>
        /// Signal/Noise ratio; only populated for FTMS MS2 spectra on Thermo instruments
        /// </summary>
        public double SignalToNoise { get; set; }

        /// <summary>
        /// Resolution; only populated for FTMS MS2 spectra on Thermo instruments
        /// </summary>
        public double Resolution { get; set; }

        /// <summary>
        /// m/z value for which the resolution and signal/noise value was computed
        /// Only populated for FTMS MS2 spectra on Thermo instruments
        /// </summary>
        public double LabelDataMZ { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReporterIonInfo(double ionMZ, int ionNumber)
        {
            MZ = ionMZ;
            IonNumber = ionNumber;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ionMZ"></param>
        /// <param name="ionNumber"></param>
        /// <param name="isContaminantIon"></param>
        public ReporterIonInfo(double ionMZ, int ionNumber, bool isContaminantIon)
        {
            MZ = ionMZ;
            IonNumber = ionNumber;
            ContaminantIon = isContaminantIon;
        }

        /// <summary>
        /// Show the reporter ion m/z and search tolerance
        /// </summary>
        public override string ToString()
        {
            return "m/z: " + MZ.ToString("0.0000") + " ±" + MZToleranceDa.ToString("0.0000");
        }
    }
}
