using System;
using System.Collections.Generic;

namespace MASIC.Data
{
    /// <summary>
    /// Used to track the m/z and intensity values of a given mass spectrum
    /// </summary>
    public class MSSpectrum
    {
        /// <summary>
        /// Scan number
        /// </summary>
        /// <remarks>0 if not in use</remarks>
        public int ScanNumber { get; set; }

        /// <summary>
        /// Number of items in IonsMZ or IonsIntensity
        /// </summary>
        public int IonCount => IonsMZ.Count;

        /// <summary>
        /// List of m/z's
        /// </summary>
        public readonly List<double> IonsMZ;

        /// <summary>
        /// List of intensities; should be the same length as IonsMZ
        /// </summary>
        public readonly List<double> IonsIntensity;

        /// <summary>
        /// Constructor that only sets the scan number
        /// </summary>
        public MSSpectrum(int scanNumber)
        {
            ScanNumber = scanNumber;

            IonsMZ = new List<double>();
            IonsIntensity = new List<double>();
        }

        /// <summary>
        /// Constructor that sets the scan number and stores m/z and intensity data (float intensities)
        /// </summary>
        public MSSpectrum(int scanNumber, IList<double> mzList, IList<float> intensityList)
            : this(scanNumber)
        {
            IonsMZ.Capacity = mzList.Count;
            IonsIntensity.Capacity = IonsMZ.Capacity;

            for (var i = 0; i < mzList.Count; i++)
            {
                IonsMZ.Add(mzList[i]);
                IonsIntensity.Add(intensityList[i]);
            }
        }

        /// <summary>
        /// Constructor that sets the scan number and stores m/z and intensity data (double intensities)
        /// </summary>
        public MSSpectrum(int scanNumber, IList<double> mzList, IList<double> intensityList)
            : this(scanNumber)
        {
            IonsMZ.Capacity = mzList.Count;
            IonsIntensity.Capacity = IonsMZ.Capacity;

            for (var i = 0; i < mzList.Count; i++)
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
        public void Clear(int newScanNumber, int newCapacity = 0)
        {
            IonsMZ.Clear();
            IonsIntensity.Clear();
            if (newCapacity > 0)
            {
                var currentCapacity = IonsMZ.Capacity;
                if (newCapacity < currentCapacity / 2)
                {
                    currentCapacity = newCapacity;
                }
                IonsMZ.Capacity = Math.Max(currentCapacity, newCapacity);
                IonsIntensity.Capacity = Math.Max(currentCapacity, newCapacity);
            }
            ScanNumber = newScanNumber;
        }

        /// <summary>
        /// Make a deep copy of this mass spectrum
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public MSSpectrum Clone()
        {
            return Copy(this);
        }

        /// <summary>
        /// Make a deep copy of the given mass spectrum
        /// </summary>
        /// <param name="sourceSpectrum"></param>
        public MSSpectrum Copy(MSSpectrum sourceSpectrum)
        {
            return new MSSpectrum(sourceSpectrum.ScanNumber, sourceSpectrum.IonsMZ, sourceSpectrum.IonsIntensity);
        }

        /// <summary>
        /// Replace data stored in IonsMZ, IonsIntensity, and Scan Number
        /// </summary>
        /// <param name="spectrum"></param>
        /// <param name="scanNumberOverride"></param>
        public void ReplaceData(MSSpectrum spectrum, int scanNumberOverride)
        {
            ScanNumber = spectrum.ScanNumber;
            if (ScanNumber != scanNumberOverride)
            {
                ScanNumber = scanNumberOverride;
            }

            IonsMZ.Clear();
            IonsIntensity.Clear();

            if (IonsMZ.Capacity / 2 > spectrum.IonsMZ.Count)
            {
                IonsMZ.Capacity = spectrum.IonsMZ.Count;
                IonsIntensity.Capacity = spectrum.IonsIntensity.Count;
            }

            IonsMZ.AddRange(spectrum.IonsMZ);
            IonsIntensity.AddRange(spectrum.IonsIntensity);
        }

        /// <summary>
        /// Remove data from the end of IonsMZ and IonsIntensity
        /// </summary>
        /// <param name="ionCountNew"></param>
        public void ShrinkArrays(int ionCountNew)
        {
            if (ionCountNew > IonsMZ.Count)
            {
                throw new Exception("ShrinkArrays should only be called with a length less than or equal to the current length");
            }

            var countToRemove = IonsMZ.Count - ionCountNew;
            if (countToRemove == 0)
                return;

            IonsMZ.RemoveRange(ionCountNew, countToRemove);
            IonsIntensity.RemoveRange(ionCountNew, countToRemove);
        }
    }
}
