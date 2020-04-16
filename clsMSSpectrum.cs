using System;
using System.Collections.Generic;

namespace MASIC
{
    /// <summary>
    /// Used to track the m/z and intensity values of a given mass spectrum
    /// </summary>
    public class clsMSSpectrum
    {
        /// <summary>
        /// Scan number
        /// </summary>
        /// <returns></returns>
        /// <remarks>0 if not in use</remarks>
        public int ScanNumber { get; set; }

        public int IonCount => IonsMZ.Count;

        /// <summary>
        /// List of m/z's
        /// </summary>
        public readonly List<double> IonsMZ;

        /// <summary>
        /// List of intensities
        /// </summary>
        public readonly List<double> IonsIntensity;

        /// <summary>
        /// Constructor that only sets the scan number
        /// </summary>
        public clsMSSpectrum(int intScanNumber)
        {
            ScanNumber = intScanNumber;
            IonsMZ = new List<double>();
            IonsIntensity = new List<double>();
        }

        /// <summary>
        /// Constructor that sets the scan number and stores m/z and intensity data (float intensities)
        /// </summary>
        public clsMSSpectrum(int intScanNumber, IList<double> mzList, IList<float> intensityList, int dataCount) : this(intScanNumber)
        {
            for (int i = 0; i <= dataCount - 1; i++)
            {
                IonsMZ.Add(mzList[i]);
                IonsIntensity.Add(intensityList[i]);
            }
        }

        /// <summary>
        /// Constructor that sets the scan number and stores m/z and intensity data (double intensities)
        /// </summary>
        public clsMSSpectrum(int intScanNumber, IList<double> mzList, IList<double> intensityList, int dataCount) : this(intScanNumber)
        {
            for (int i = 0; i <= dataCount - 1; i++)
            {
                IonsMZ.Add(mzList[i]);
                IonsIntensity.Add(intensityList[i]);
            }
        }

        /// <summary>
        /// Clear the m/z and intensity values (but leave the scan number unchanged)
        /// </summary>
        public void Clear()
        {
            IonsMZ.Clear();
            IonsIntensity.Clear();
        }

        /// <summary>
        /// Clear the m/z and intensity values, and update the scan number
        /// </summary>
        public void Clear(int newScanNumber)
        {
            IonsMZ.Clear();
            IonsIntensity.Clear();
            ScanNumber = newScanNumber;
        }

        public clsMSSpectrum Clone()
        {
            return Copy(this);
        }

        public clsMSSpectrum Copy(clsMSSpectrum objSource)
        {
            var newSpectrum = new clsMSSpectrum(objSource.ScanNumber, objSource.IonsMZ, objSource.IonsIntensity, objSource.IonsMZ.Count);
            return newSpectrum;
        }

        public void ReplaceData(clsMSSpectrum spectrum, int scanNumberOverride)
        {
            ScanNumber = spectrum.ScanNumber;
            if (ScanNumber != scanNumberOverride)
            {
                ScanNumber = scanNumberOverride;
            }

            IonsMZ.Clear();
            IonsMZ.AddRange(spectrum.IonsMZ);
            IonsIntensity.Clear();
            IonsIntensity.AddRange(spectrum.IonsIntensity);
        }

        public void ShrinkArrays(int ionCountNew)
        {
            if (ionCountNew > IonsMZ.Count)
            {
                throw new Exception("ShrinkArrays should only be called with a length less than or equal to the current length");
            }

            int countToRemove = IonsMZ.Count - ionCountNew;
            if (countToRemove == 0)
                return;
            IonsMZ.RemoveRange(ionCountNew, countToRemove);
            IonsIntensity.RemoveRange(ionCountNew, countToRemove);
        }
    }
}
