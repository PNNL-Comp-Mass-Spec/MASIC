using System;
using System.Collections.Generic;
using MASIC.Data;
using PRISM;

namespace MASIC
{
    /// <summary>
    /// This class looks for ions in a given m/z range and combines their intensity values
    /// (reporting either the summed value or the maximum value)
    /// </summary>
    public class DataAggregation : EventNotifier
    {
        // Ignore Spelling: MASIC, uncaching

        /// <summary>
        /// When returnMax is false, determine the sum of the data within the search mass tolerance
        /// When returnMax is true, determine the maximum of the data within the search mass tolerance
        /// </summary>
        /// <remarks>
        /// Note that this function performs a recursive search of msSpectrum.IonsMZ
        /// It is therefore very efficient regardless of the number of data points in the spectrum
        /// For sparse spectra, you can alternatively use FindMaxValueInMZRange
        /// </remarks>
        /// <param name="msSpectrum">MS spectrum</param>
        /// <param name="searchMZ">Target m/z</param>
        /// <param name="searchToleranceHalfWidth">Search tolerance half width</param>
        /// <param name="ionMatchCount">Output: matching ion count</param>
        /// <param name="closestMZ">Output: closest m/z</param>
        /// <param name="returnMax">
        /// When true, determine the maximum of the data within the search mass tolerance,
        /// otherwise determine the sum of the data within the search mass tolerance
        /// </param>
        /// <returns>The sum or maximum of the matching data; 0 if no matches</returns>
        public double AggregateIonsInRange(
            MSSpectrum msSpectrum,
            double searchMZ,
            double searchToleranceHalfWidth,
            out int ionMatchCount,
            out double closestMZ,
            bool returnMax)
        {
            ionMatchCount = 0;
            closestMZ = 0;

            try
            {
                var smallestDifference = double.MaxValue;

                if (msSpectrum.IonsMZ == null || msSpectrum.IonCount == 0)
                {
                    return 0;
                }

                if (!SumIonsFindValueInRange(msSpectrum.IonsMZ, searchMZ, searchToleranceHalfWidth, out var indexFirst, out var indexLast))
                {
                    return 0;
                }

                double ionSumOrMax = 0;

                for (var ionIndex = indexFirst; ionIndex <= indexLast; ionIndex++)
                {
                    if (returnMax)
                    {
                        // Return max
                        if (msSpectrum.IonsIntensity[ionIndex] > ionSumOrMax)
                        {
                            ionSumOrMax = msSpectrum.IonsIntensity[ionIndex];
                        }
                    }
                    else
                    {
                        // Return sum
                        ionSumOrMax += msSpectrum.IonsIntensity[ionIndex];
                    }

                    var testDifference = Math.Abs(msSpectrum.IonsMZ[ionIndex] - searchMZ);

                    if (testDifference < smallestDifference)
                    {
                        smallestDifference = testDifference;
                        closestMZ = msSpectrum.IonsMZ[ionIndex];
                    }
                }

                ionMatchCount = indexLast - indexFirst + 1;

                return ionSumOrMax;
            }
            catch (Exception)
            {
                ionMatchCount = 0;
                return 0;
            }
        }

        /// <summary>
        /// Searches currentScan.IonsMZ for the maximum intensity value between mzStart and mzEnd
        /// If a match is found, updates bestMatchMZ to the m/z of the match,
        /// updates matchIntensity to its intensity, and returns True
        /// </summary>
        /// <remarks>
        /// Note that this function performs a linear search of .IonsMZ
        /// It is therefore good for spectra with less than 10 data points and bad for spectra with more than 10 data points
        /// As an alternative to this function, use AggregateIonsInRange
        /// </remarks>
        /// <param name="spectraCache">Spectra cache</param>
        /// <param name="currentScan">Current scan</param>
        /// <param name="mzStart">m/z start</param>
        /// <param name="mzEnd">m/z end</param>
        /// <param name="bestMatchMZ">Output: m/z of the most intense ion in the given range</param>
        /// <param name="matchIntensity">Output: intensity of the most intense ion in the given range</param>
        /// <returns>True if a match is found, false if no data exists within the m/z range (or if the spectrum could not be obtained)</returns>
        public bool FindMaxValueInMZRange(
            SpectraCache spectraCache,
            ScanInfo currentScan,
            double mzStart,
            double mzEnd,
            out double bestMatchMZ,
            out double matchIntensity)
        {
            bestMatchMZ = 0;
            matchIntensity = 0;
            try
            {
                // ReSharper disable once RedundantArgumentDefaultValue
                if (!spectraCache.GetSpectrum(currentScan.ScanNumber, out var spectrum, true))
                {
                    OnErrorEvent("Error uncaching scan " + currentScan.ScanNumber);
                    return false;
                }

                return FindMaxValueInMZRange(
                    spectrum.IonsMZ,
                    spectrum.IonsIntensity,
                    spectrum.IonCount,
                    mzStart, mzEnd,
                    out bestMatchMZ, out matchIntensity);
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in FindMaxValueInMZRange (A): " + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Searches mzList for the maximum intensity value between mzStart and mzEnd
        /// If a match is found, updates bestMatchMZ to the m/z of the match,
        /// updates matchIntensity to its intensity, and returns True
        /// </summary>
        /// <remarks>
        /// Note that this function performs a linear search of .IonsMZ
        /// It is therefore good for spectra with less than 10 data points and bad for spectra with more than 10 data points
        /// As an alternative to this function, use AggregateIonsInRange
        /// </remarks>
        /// <param name="mzList"></param>
        /// <param name="intensityList"></param>
        /// <param name="ionCount"></param>
        /// <param name="mzStart"></param>
        /// <param name="mzEnd"></param>
        /// <param name="bestMatchMZ">Output: m/z of the most intense ion in the given range</param>
        /// <param name="matchIntensity">Output: intensity of the most intense ion in the given range</param>
        /// <returns>True if a match is found, false if no data exists within the m/z range (or if the spectrum could not be obtained)</returns>
        private bool FindMaxValueInMZRange(
            IList<double> mzList,
            IList<double> intensityList,
            int ionCount,
            double mzStart,
            double mzEnd,
            out double bestMatchMZ,
            out double matchIntensity)
        {
            int closestMatchIndex;
            var highestIntensity = 0.0;

            try
            {
                closestMatchIndex = -1;
                highestIntensity = 0;

                for (var dataIndex = 0; dataIndex < ionCount; dataIndex++)
                {
                    if (mzList[dataIndex] >= mzStart && mzList[dataIndex] <= mzEnd)
                    {
                        if (closestMatchIndex < 0)
                        {
                            closestMatchIndex = dataIndex;
                            highestIntensity = intensityList[dataIndex];
                        }
                        else if (intensityList[dataIndex] > highestIntensity)
                        {
                            closestMatchIndex = dataIndex;
                            highestIntensity = intensityList[dataIndex];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in FindMaxValueInMZRange (B): " + ex.Message, ex);
                closestMatchIndex = -1;
            }

            if (closestMatchIndex >= 0)
            {
                bestMatchMZ = mzList[closestMatchIndex];
                matchIntensity = highestIntensity;
                return true;
            }

            bestMatchMZ = 0;
            matchIntensity = 0;
            return false;
        }

        /// <summary>
        /// Searches dataValues for searchValue with a tolerance of +/-toleranceHalfWidth
        /// </summary>
        /// <param name="dataValues"></param>
        /// <param name="searchValue"></param>
        /// <param name="toleranceHalfWidth"></param>
        /// <param name="matchIndexStart">Output: start index of matching values</param>
        /// <param name="matchIndexEnd">Output: end index of matching values</param>
        /// <returns> True if a match is found, false if no match</returns>
        private bool SumIonsFindValueInRange(
            IReadOnlyList<double> dataValues,
            double searchValue,
            double toleranceHalfWidth,
            out int matchIndexStart,
            out int matchIndexEnd)
        {
            matchIndexStart = 0;
            matchIndexEnd = dataValues.Count - 1;

            if (dataValues.Count == 0)
            {
                matchIndexEnd = -1;
            }
            else if (dataValues.Count == 1)
            {
                if (Math.Abs(searchValue - dataValues[0]) > toleranceHalfWidth)
                {
                    // Only one data point, and it is not within tolerance
                    matchIndexEnd = -1;
                }
            }
            else
            {
                SumIonsBinarySearchRangeDbl(dataValues, searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }

            if (matchIndexStart <= matchIndexEnd)
            {
                return true;
            }

            matchIndexStart = -1;
            matchIndexEnd = -1;
            return false;
        }

        private void SumIonsBinarySearchRangeDbl(
            IReadOnlyList<double> dataValues,
            double searchValue,
            double toleranceHalfWidth,
            ref int matchIndexStart,
            ref int matchIndexEnd)
        {
            // Recursive search function

            var leftDone = false;
            var rightDone = false;

            var indexMidpoint = (matchIndexStart + matchIndexEnd) / 2;

            if (indexMidpoint == matchIndexStart)
            {
                // Min and Max are next to each other
                if (Math.Abs(searchValue - dataValues[matchIndexStart]) > toleranceHalfWidth)
                    matchIndexStart = matchIndexEnd;

                if (Math.Abs(searchValue - dataValues[matchIndexEnd]) > toleranceHalfWidth)
                    matchIndexEnd = indexMidpoint;
                return;
            }

            if (dataValues[indexMidpoint] > searchValue + toleranceHalfWidth)
            {
                // Out of range on the right
                matchIndexEnd = indexMidpoint;
                SumIonsBinarySearchRangeDbl(dataValues, searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else if (dataValues[indexMidpoint] < searchValue - toleranceHalfWidth)
            {
                // Out of range on the left
                matchIndexStart = indexMidpoint;
                SumIonsBinarySearchRangeDbl(dataValues, searchValue, toleranceHalfWidth, ref matchIndexStart, ref matchIndexEnd);
            }
            else
            {
                // Inside range; figure out the borders
                var leftIndex = indexMidpoint;

                do
                {
                    leftIndex--;

                    if (leftIndex < matchIndexStart)
                    {
                        leftDone = true;
                    }
                    else if (Math.Abs(searchValue - dataValues[leftIndex]) > toleranceHalfWidth)
                    {
                        leftDone = true;
                    }
                }
                while (!leftDone);

                var rightIndex = indexMidpoint;

                do
                {
                    rightIndex++;

                    if (rightIndex > matchIndexEnd)
                    {
                        rightDone = true;
                    }
                    else if (Math.Abs(searchValue - dataValues[rightIndex]) > toleranceHalfWidth)
                    {
                        rightDone = true;
                    }
                }
                while (!rightDone);

                matchIndexStart = leftIndex + 1;
                matchIndexEnd = rightIndex - 1;
            }
        }
    }
}
