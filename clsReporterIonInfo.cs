namespace MASIC
{
    /// <summary>
    /// Information for a single reporter ion
    /// </summary>
    public class clsReporterIonInfo
    {
        // Ignore Spelling: Daltons, immonium

        /// <summary>
        /// Reporter ion m/z
        /// </summary>
        public double MZ { get; }

        /// <summary>
        /// Tolerance (in Daltons) for looking for this m/z in spectra
        /// </summary>
        public double MZToleranceDa { get; set; }

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
        public clsReporterIonInfo(double ionMZ)
        {
            MZ = ionMZ;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ionMZ"></param>
        /// <param name="isContaminantIon"></param>
        public clsReporterIonInfo(double ionMZ, bool isContaminantIon)
        {
            MZ = ionMZ;
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
