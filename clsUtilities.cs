using System;
using System.Collections.Generic;

namespace MASIC
{
    public class clsUtilities
    {
        // Const CHARGE_CARRIER_MASS_AVG As Double = 1.00739

        /// <summary>
        /// Monoisotopic charge carrier mass
        /// </summary>
        public const double CHARGE_CARRIER_MASS_MONOISOTOPIC = 1.00727649;

        /// <summary>
        /// Check whether the m/z value is in the specified range
        /// </summary>
        /// <param name="mz"></param>
        /// <param name="mzIgnoreRangeStart"></param>
        /// <param name="mzIgnoreRangeEnd"></param>
        /// <returns>True if the m/z value is between mzIgnoreRangeStart and mzIgnoreRangeEnd</returns>
        public static bool CheckPointInMZIgnoreRange(
            double mz,
            double mzIgnoreRangeStart,
            double mzIgnoreRangeEnd)
        {
            if (mzIgnoreRangeStart > 0 || mzIgnoreRangeEnd > 0)
            {
                return mz >= mzIgnoreRangeStart && mz <= mzIgnoreRangeEnd;
            }

            return false;
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

        /// <summary>
        /// Converts massMZ to the MZ that would appear at the given desiredCharge
        /// To return the neutral mass, set desiredCharge to 0
        /// If chargeCarrierMass is 0, uses CHARGE_CARRIER_MASS_MONOISOTOPIC
        /// </summary>
        /// <param name="massMZ"></param>
        /// <param name="currentCharge"></param>
        /// <param name="desiredCharge"></param>
        /// <param name="chargeCarrierMass"></param>
        /// <returns></returns>
        public static double ConvoluteMass(
            double massMZ,
            short currentCharge,
            short desiredCharge = 1,
            double chargeCarrierMass = 0)
        {


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
                    // Return M+H, which is currently stored in newMZ
                }
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

        /// <summary>
        /// Convert from a double to float
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>Assures that the value is between float.MinValue and float.MaxValue</remarks>
        public static float CFloatSafe(double value)
        {
            if (value > float.MaxValue)
                return float.MaxValue;

            if (value < float.MinValue)
                return float.MinValue;

            return (float)value;
        }

        /// <summary>
        /// Return true if the number can be parsed as a double
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNumber(string value)
        {
            try
            {
                return double.TryParse(value, out _);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Return true if the two values match, within float.Epsilon
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool ValuesMatch(float value1, float value2)
        {
            return ValuesMatch(value1, value2, -1);
        }

        /// <summary>
        /// Return true if the two values match, when rounded to the given number of digits after the decimal point
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="digitsOfPrecision">Digits to round the numbers to before comparing</param>
        /// <returns></returns>
        /// <remarks>If digitsOfPrecision is negative, the values must match within float.Epsilon</remarks>
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

        /// <summary>
        /// Return true if the two values match, within double.Epsilon
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool ValuesMatch(double value1, double value2)
        {
            return ValuesMatch(value1, value2, -1);
        }

        /// <summary>
        /// Return true if the two values match, when rounded to the given number of digits after the decimal point
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="digitsOfPrecision">Digits to round the numbers to before comparing</param>
        /// <returns></returns>
        /// <remarks>If digitsOfPrecision is negative, the values must match within double.Epsilon</remarks>
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

        /// <summary>
        /// Converts massToConvert to ppm, based on the value of currentMZ
        /// </summary>
        /// <param name="massToConvert"></param>
        /// <param name="currentMZ"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static double MassToPPM(double massToConvert, double currentMZ)
        {
            return massToConvert * 1000000.0 / currentMZ;
        }

        /// <summary>
        /// Converts ppmToConvert to a mass value, which is dependent on currentMZ
        /// </summary>
        /// <param name="ppmToConvert"></param>
        /// <param name="currentMZ"></param>
        /// <returns></returns>
        public static double PPMToMass(double ppmToConvert, double currentMZ)
        {
            return ppmToConvert / 1000000.0 * currentMZ;
        }
    }
}
