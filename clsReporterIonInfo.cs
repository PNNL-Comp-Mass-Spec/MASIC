namespace MASIC
{
    public class clsReporterIonInfo
    {
        public double MZ;
        public double MZToleranceDa;

        /// <summary>
        /// Should be False for Reporter Ions and True for other ions, e.g. immonium loss from phenylalanine
        /// </summary>
        public bool ContaminantIon;

        /// <summary>
        /// Signal/Noise ratio; only populated for FTMS MS2 spectra on Thermo instruments
        /// </summary>
        public double SignalToNoise;

        /// <summary>
        /// Resolution; only populated for FTMS MS2 spectra on Thermo instruments
        /// </summary>
        public double Resolution;

        /// <summary>
        /// m/z value for which the resolution and signal/noise value was computed
        /// Only populated for FTMS MS2 spectra on Thermo instruments
        /// </summary>
        public double LabelDataMZ;

        /// <summary>
        /// Constructor
        /// </summary>
        public clsReporterIonInfo(double ionMZ)
        {
            MZ = ionMZ;
        }

        public clsReporterIonInfo(double ionMZ, bool isContaminantIon)
        {
            MZ = ionMZ;
            ContaminantIon = isContaminantIon;
        }

        public override string ToString()
        {
            return "m/z: " + MZ.ToString("0.0000") + " ±" + MZToleranceDa.ToString("0.0000");
        }
    }
}