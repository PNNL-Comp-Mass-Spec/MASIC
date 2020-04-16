using System;
using System.Collections.Generic;

namespace MASIC
{
    public class clsUtilities
    {
        #region "Constants and Enums"
        // Const CHARGE_CARRIER_MASS_AVG As Double = 1.00739

        public const double CHARGE_CARRIER_MASS_MONOISOTOPIC = 1.00727649;

        #endregion

        public static bool CheckPointInMZIgnoreRange(double mz, double mzIgnoreRangeStart, double mzIgnoreRangeEnd)
        {
            if (mzIgnoreRangeStart > 0 || mzIgnoreRangeEnd > 0)
            {
                if (mz <= mzIgnoreRangeEnd && mz >= mzIgnoreRangeStart)
                {
                    // The m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compute the median value in a list of doubles
        /// </summary>
        /// <param name="values"></param>
        /// <returns>The median value, or 0 if the list is empty or null</returns>
        public static double ComputeMedian(IReadOnlyCollection<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            return MathNet.Numerics.Statistics.Statistics.Median(values);
        }

        public static double ConvoluteMass(double massMZ, short currentCharge, short desiredCharge = 1, double chargeCarrierMass = 0)
        {
            // Converts massMZ to the MZ that would appear at the given desiredCharge
            // To return the neutral mass, set desiredCharge to 0

            // If chargeCarrierMass is 0, uses CHARGE_CARRIER_MASS_MONOISOTOPIC

            double newMZ;
            if (Math.Abs(chargeCarrierMass) < double.Epsilon)
                chargeCarrierMass = CHARGE_CARRIER_MASS_MONOISOTOPIC;
            if (currentCharge == desiredCharge)
            {
                newMZ = massMZ;
            }
            else
            {
                if (currentCharge == 1)
                {
                    newMZ = massMZ;
                }
                else if (currentCharge > 1)
                {
                    // Convert massMZ to M+H
                    newMZ = massMZ * currentCharge - chargeCarrierMass * (currentCharge - 1);
                }
                else if (currentCharge == 0)
                {
                    // Convert massMZ (which is neutral) to M+H and store in newMZ
                    newMZ = massMZ + chargeCarrierMass;
                }
                else
                {
                    // Negative charges are not supported; return 0
                    return 0;
                }

                if (desiredCharge > 1)
                {
                    newMZ = (newMZ + chargeCarrierMass * (desiredCharge - 1)) / desiredCharge;
                }
                else if (desiredCharge == 1)
                {
                }
                // Return M+H, which is currently stored in newMZ
                else if (desiredCharge == 0)
                {
                    // Return the neutral mass
                    newMZ -= chargeCarrierMass;
                }
                else
                {
                    // Negative charges are not supported; return 0
                    newMZ = 0;
                }
            }

            return newMZ;
        }

        public static float CSngSafe(double value)
        {
            if (value > float.MaxValue)
                return float.MaxValue;
            if (value < float.MinValue)
                return float.MinValue;
            return Convert.ToSingle(value);
        }

        public static bool IsNumber(string value)
        {
            try
            {
                double argresult = 0;
                return double.TryParse(value, out argresult);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool ValuesMatch(float value1, float value2)
        {
            return ValuesMatch(value1, value2, -1);
        }

        public static bool ValuesMatch(float value1, float value2, int digitsOfPrecision)
        {
            if (digitsOfPrecision < 0)
            {
                if (Math.Abs(value1 - value2) < float.Epsilon)
                {
                    return true;
                }
            }
            else if (Math.Abs(Math.Round(value1, digitsOfPrecision) - Math.Round(value2, digitsOfPrecision)) < float.Epsilon)
            {
                return true;
            }

            return false;
        }

        public static bool ValuesMatch(double value1, double value2)
        {
            return ValuesMatch(value1, value2, -1);
        }

        public static bool ValuesMatch(double value1, double value2, int digitsOfPrecision)
        {
            if (digitsOfPrecision < 0)
            {
                if (Math.Abs(value1 - value2) < double.Epsilon)
                {
                    return true;
                }
            }
            else if (Math.Abs(Math.Round(value1, digitsOfPrecision) - Math.Round(value2, digitsOfPrecision)) < double.Epsilon)
            {
                return true;
            }

            return false;
        }

        #region "PPMToMassConversion"
        public static double MassToPPM(double massToConvert, double currentMZ)
        {
            // Converts massToConvert to ppm, based on the value of currentMZ

            return massToConvert * 1000000.0 / currentMZ;
        }

        public static double PPMToMass(double ppmToConvert, double currentMZ)
        {
            // Converts ppmToConvert to a mass value, which is dependent on currentMZ

            return ppmToConvert / 1000000.0 * currentMZ;
        }
        #endregion
    }
}
