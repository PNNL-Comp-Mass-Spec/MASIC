using System;
using System.Collections.Generic;
using PRISM;

namespace MASIC
{
    public class clsDataAggregation : EventNotifier
    {
        /// <summary>
        /// When returnMax is false, determine the sum of the data within the search mass tolerance
        /// When returnMaxis true, determine the maximum of the data within the search mass tolerance
        /// </summary>
        /// <param name="objMSSpectrum"></param>
        /// <param name="searchMZ"></param>
        /// <param name="searchToleranceHalfWidth"></param>
        /// <param name="ionMatchCount"></param>
        /// <param name="closestMZ"></param>
        /// <param name="returnMax"></param>
        /// <returns>The sum or maximum of the matching data; 0 if no matches</returns>
        /// <remarks>
        /// Note that this function performs a recursive search of objMSSpectrum.IonsMZ
        /// It is therefore very efficient regardless of the number of data points in the spectrum
        /// For sparse spectra, you can alternatively use FindMaxValueInMZRange
        /// </remarks>
        public double AggregateIonsInRange(
        clsMSSpectrum objMSSpectrum,
        double searchMZ,
        double searchToleranceHalfWidth,
        out int ionMatchCount,
        out double closestMZ,
        bool returnMax)
        {
            ionMatchCount = 0;
            closestMZ = 0;
            double ionSumOrMax = 0;

            try
            {
                double smallestDifference = double.MaxValue;

                if (objMSSpectrum.IonsMZ != null && objMSSpectrum.IonCount > 0)
                {
                    int indexFirst, indexLast;
                    if (SumIonsFindValueInRange(objMSSpectrum.IonsMZ, searchMZ, searchToleranceHalfWidth, out indexFirst, out indexLast))
                    {
                        for (int ionIndex = indexFirst; ionIndex <= indexLast; ionIndex++)
                        {
                            if (returnMax)
                            {
                                // Return max
                                if (objMSSpectrum.IonsIntensity[ionIndex] > ionSumOrMax)
                                {
                                    ionSumOrMax = objMSSpectrum.IonsIntensity[ionIndex];
                                }
                            }
                            else
                            {
                                // Return sum
                                ionSumOrMax += objMSSpectrum.IonsIntensity[ionIndex];
                            }

                            double testDifference = Math.Abs(objMSSpectrum.IonsMZ[ionIndex] - searchMZ);
                            if (testDifference < smallestDifference)
                            {
                                smallestDifference = testDifference;
                                closestMZ = objMSSpectrum.IonsMZ[ionIndex];
                            }
                        }

                        ionMatchCount = indexLast - indexFirst + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                ionMatchCount = 0;
            }

            return ionSumOrMax;
        }

        public bool FindMaxValueInMZRange(
            clsSpectraCache spectraCache,
            clsScanInfo currentScan,
            double mzStart,
            double mzEnd,
            out double bestMatchMZ,
            out double matchIntensity)
        {
            // Searches currentScan.IonsMZ for the maximum value between mzStart and mzEnd
            // If a match is found, then updates bestMatchMZ to the m/z of the match, updates matchIntensity to its intensity,
            // and returns True
            //
            // Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
            // and bad for spectra with > 10 data points
            // As an alternative to this function, use AggregateIonsInRange

            int poolIndex;

            bestMatchMZ = 0;
            matchIntensity = 0;
            try
            {
                if (!spectraCache.ValidateSpectrumInPool(currentScan.ScanNumber, out poolIndex))
                {
                    OnErrorEvent("Error uncaching scan " + currentScan.ScanNumber);
                    return false;
                }
                else
                {
                    bool success = FindMaxValueInMZRange(
                        spectraCache.SpectraPool[poolIndex].IonsMZ,
                        spectraCache.SpectraPool[poolIndex].IonsIntensity,
                        spectraCache.SpectraPool[poolIndex].IonCount,
                        mzStart, mzEnd,
                        out bestMatchMZ, out matchIntensity);

                    return success;
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in FindMaxValueInMZRange (A): " + ex.Message, ex);
                return false;
            }
        }

        private bool FindMaxValueInMZRange(
            IList<double> mzList,
            IList<double> intensityList,
            int ionCount,
            double mzStart,
            double mzEnd,
            out double bestMatchMZ,
            out double matchIntensity)
        {
            // Searches mzList for the maximum value between mzStart and mzEnd
            // If a match is found, then updates bestMatchMZ to the m/z of the match, updates matchIntensity to its intensity,
            // and returns True
            //
            // Note that this function performs a linear search of .IonsMZ; it is therefore good for spectra with < 10 data points
            // and bad for spectra with > 10 data points
            // As an alternative to this function, use AggregateIonsInRange

            int dataIndex;
            int closestMatchIndex;
            var highestIntensity = default(double);

            try
            {
                closestMatchIndex = -1;
                highestIntensity = 0;

                for (dataIndex = 0; dataIndex <= ionCount - 1; dataIndex++)
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
            else
            {
                bestMatchMZ = 0;
                matchIntensity = 0;
                return false;
            }
        }

        private bool SumIonsFindValueInRange(
            IReadOnlyList<double> dataValues,
            double searchValue,
            double toleranceHalfWidth,
            out int matchIndexStart,
            out int matchIndexEnd)
        {
            // Searches dataValues for searchValue with a tolerance of +/-toleranceHalfWidth
            // Returns True if a match is found; in addition, populates matchIndexStart and matchIndexEnd
            // Otherwise, returns false

            bool matchFound;

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

            if (matchIndexStart > matchIndexEnd)
            {
                matchIndexStart = -1;
                matchIndexEnd = -1;
                matchFound = false;
            }
            else
            {
                matchFound = true;
            }

            return matchFound;
        }

        private void SumIonsBinarySearchRangeDbl(
            IReadOnlyList<double> dataValues,
            double searchValue,
            double toleranceHalfWidth,
            ref int matchIndexStart,
            ref int matchIndexEnd)
        {
            // Recursive search function

            int indexMidpoint;
            var leftDone = default(bool);
            var rightDone = default(bool);
            int leftIndex;
            int rightIndex;

            indexMidpoint = (matchIndexStart + matchIndexEnd) / 2;
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
                leftIndex = indexMidpoint;
                do
                {
                    leftIndex = leftIndex - 1;
                    if (leftIndex < matchIndexStart)
                    {
                        leftDone = true;
                    }
                    else if (Math.Abs(searchValue - dataValues[leftIndex]) > toleranceHalfWidth)
                        leftDone = true;
                }
                while (!leftDone);
                rightIndex = indexMidpoint;

                do
                {
                    rightIndex = rightIndex + 1;
                    if (rightIndex > matchIndexEnd)
                    {
                        rightDone = true;
                    }
                    else if (Math.Abs(searchValue - dataValues[rightIndex]) > toleranceHalfWidth)
                        rightDone = true;
                }
                while (!rightDone);

                matchIndexStart = leftIndex + 1;
                matchIndexEnd = rightIndex - 1;
            }
        }
    }
}
