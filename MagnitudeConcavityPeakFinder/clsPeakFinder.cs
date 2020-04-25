using System;
using System.Collections.Generic;

namespace MagnitudeConcavityPeakFinder
{
    internal class PeakFinder
    {
        // Peak detection routines
        // Written by Matthew Monroe in roughly 2001 at UNC (Chapel Hill, NC)
        // Kevin Lan provided the concept of Magnitude Concavity fitting
        // Ported from LabView code to VB 6 in June 2003 at PNNL (Richland, WA)
        // Ported from VB 6 to VB.NET in October 2003
        // Switched from using the eols.dll least squares fitting routine to using a local function
        // Ported to C# in February 2015

        // Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
        // Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

        private enum eTermFunctionConstants
        {
            One = 0,
            X = 1,
            LogX = 2,
            Log10X = 3,
            ExpX = 4,
            SinX = 5,
            CosX = 6,
            TanX = 7,
            ATanX = 8
        }

        private struct udtLeastSquaresFitEquationTermType
        {
            public eTermFunctionConstants Func;
            public double Power;
            public double Coefficient;

            public bool Inverse;

            // Stores the coefficient determined for the fit
            public double ParamResult;
        }

        public double ComputeSlope(
            double[] xValuesZeroBased,
            double[] yValuesZeroBased,
            int startIndex,
            int endIndex)
        {
            const int POLYNOMIAL_ORDER = 1;

            if (xValuesZeroBased == null || xValuesZeroBased.Length == 0)
                return 0;

            var segmentCount = endIndex - startIndex + 1;

            var segmentX = new double[segmentCount];
            var segmentY = new double[segmentCount];

            // Copy the desired segment of data from xValues to segmentX and yValues to segmentY
            for (var i = startIndex; i <= endIndex; i++)
            {
                segmentX[i - startIndex] = xValuesZeroBased[i];
                segmentY[i - startIndex] = yValuesZeroBased[i];
            }

            // Compute the coefficients for the curve fit
            LeastSquaresFit(segmentX, segmentY, out var coefficients, POLYNOMIAL_ORDER);

            return coefficients[1];
        }

        public List<clsPeak> DetectPeaks(
            double[] xValuesZeroBased,
            double[] yValuesZeroBased,
            double intensityThresholdAbsoluteMinimum,
            int peakWidthPointsMinimum,
            int peakDetectIntensityThresholdPercentageOfMaximum = 0,
            int peakWidthInSigma = 4,
            bool useValleysForPeakWidth = true,
            bool movePeakLocationToMaxIntensity = true)
        {
            // Finds peaks in the parallel arrays xValuesZeroBased[] and yValuesZeroBased[]
            // intensityThreshold is the minimum absolute intensity allowable for a peak
            // peakDetectIntensityThresholdPercentageOfMaximum allows one to specify a minimum intensity as a percentage of the maximum peak intensity
            // Note that the maximum value of intensityThreshold vs. MaxValue * peakDetectIntensityThresholdPercentageOfMaximum is used as the minimum
            // For example, if intensityThreshold = 10 and peakDetectIntensityThresholdPercentageOfMaximum =  5 (indicating 5%),
            //   then if the maximum of yValuesZeroBased[] is 50, then the minimum intensity of identified peaks is 10, and not 2.5
            //   However, if the maximum of yValuesZeroBased[] is 500, then the minimum intensity of identified peaks is 50, and not 10

            // Returns the peaks in List<clsPeak>
            // Each peak has these values:
            //   LocationIndex  is the index of the peak apex in the source arrays
            //   LeftEdge       is the left edge of the peak (in points, not actual units); this value could be negative if blnUseValleysForPeakWidth = False
            //   RightEdge      is the right edge of the peak (in points, not actual units); this value could be larger than sourceDataCount-1 if blnUseValleysForPeakWidth = False
            //   Area
            //   IsValid

            // Note: Compute peak width using: peakWidthPoints = peakEdgesRight[intPeakLocationsCount] - peakEdgesLeft[intPeakLocationsCount] + 1

            // Uses the Magnitude-Concavity method, wherein a second order
            //   polynomial is fit to the points within the window, giving a_2*x^2 + a_1*x + a_0
            //   Given this, a_1 is the first derivative and a_2 is the second derivative
            // From this, the first derivative gives the index of the peak apex
            // The standard deviation (s) can be found using:
            //   s = sqrt(-h(t_r) / h''(t_r))
            //  where h(t_r) is the height of the peak at the peak center
            //  and h''(t_r) is the height of the second derivative of the peak
            // In chromatography, the baseline peak widthInPoints = 4*dblSigma

            var detectedPeaks = new List<clsPeak>();

            try
            {
                var sourceDataCount = xValuesZeroBased.Length;
                if (sourceDataCount <= 0)
                    return detectedPeaks;

                // The mid point width is the minimum width divided by 2, rounded down
                var peakHalfWidth = (int)Math.Floor(peakWidthPointsMinimum / 2.0);

                // Find the maximum intensity in the source data
                double maximumIntensity = 0;
                for (var dataIndex = 0; dataIndex < sourceDataCount; dataIndex++)
                {
                    if (yValuesZeroBased[dataIndex] > maximumIntensity)
                    {
                        maximumIntensity = yValuesZeroBased[dataIndex];
                    }
                }

                var intensityThreshold = maximumIntensity * (peakDetectIntensityThresholdPercentageOfMaximum / 100.0);
                if (intensityThreshold < intensityThresholdAbsoluteMinimum)
                {
                    intensityThreshold = intensityThresholdAbsoluteMinimum;
                }

                // Exit the function if none of the data is above the minimum threshold
                if (maximumIntensity < intensityThreshold)
                    return detectedPeaks;

                // Do the actual work
                FitSegments(xValuesZeroBased, yValuesZeroBased, sourceDataCount, peakWidthPointsMinimum,
                            peakHalfWidth, out var firstDerivative, out var secondDerivative);

                if (peakWidthInSigma < 1)
                    peakWidthInSigma = 1;

                // Examine the First Derivative function and look for zero crossings (in the downward direction)
                // If looking for valleys, would look for zero crossings in the upward direction
                // Only significant if intensity of point is above threshold

                if (peakWidthPointsMinimum <= 0)
                    peakWidthPointsMinimum = 1;

                // We'll start looking for peaks halfway into intPeakWidthPointsMinimum
                var indexFirst = peakHalfWidth;
                var indexLast = sourceDataCount - 1 - peakHalfWidth;

                for (var dataIndex = indexFirst; dataIndex <= indexLast; dataIndex++)
                {
                    if (firstDerivative[dataIndex] > 0 & firstDerivative[dataIndex + 1] < 0)
                    {
                        // Possible peak
                        if (yValuesZeroBased[dataIndex] >= intensityThreshold |
                            yValuesZeroBased[dataIndex + 1] >= intensityThreshold)
                        {
                            // Actual peak

                            var newPeak = new clsPeak
                            {
                                LocationIndex = dataIndex
                            };

                            detectedPeaks.Add(newPeak);

                            if (useValleysForPeakWidth)
                            {
                                DetectPeaksUseValleys(sourceDataCount, yValuesZeroBased, firstDerivative, newPeak, dataIndex, intensityThreshold, peakHalfWidth);
                            }
                            else
                            {
                                DetectPeaksSecondDerivative(sourceDataCount, yValuesZeroBased, secondDerivative, newPeak, dataIndex, peakWidthInSigma);
                            }
                        }
                    }
                }

                // Compute the peak areas
                foreach (var peak in detectedPeaks)
                {
                    ComputePeakArea(sourceDataCount, xValuesZeroBased, yValuesZeroBased, peak);

                    if (movePeakLocationToMaxIntensity)
                    {
                        MovePeakLocationToMax(sourceDataCount, yValuesZeroBased, peak, peakWidthPointsMinimum);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in clsPeakDetection->DetectPeaks (or in a child function) " + ex.Message);
            }

            return detectedPeaks;
        }

        private void ComputePeakArea(
            int sourceDataCount,
            IReadOnlyList<double> xValuesZeroBased,
            IReadOnlyList<double> yValuesZeroBased,
            clsPeak peak)
        {
            if (peak.PeakWidth == 0)
            {
                // 0-width peak; this shouldn't happen
                Console.WriteLine("Warning: 0-width peak; this shouldn't happen (clsPeakDetection->DetectPeaks)");
                peak.Area = 0;
                return;
            }

            if (peak.PeakWidth == 1)
            {
                // I don't think this can happen
                // Just in case, we'll set the area equal to the peak intensity
                peak.Area = yValuesZeroBased[peak.LocationIndex];
                return;
            }

            var thisPeakStartIndex = peak.LeftEdge;
            var thisPeakEndIndex = peak.RightEdge;

            if (thisPeakStartIndex < 0)
            {
                // This will happen if the width is too large, or if not all of the peak's data was included in the data arrays
                thisPeakStartIndex = 0;
            }

            if (thisPeakEndIndex >= sourceDataCount)
            {
                // This will happen if the width is too large, or if not all of the peak's data was included in the data arrays
                thisPeakEndIndex = sourceDataCount - 1;
            }

            var thisPeakWidthInPoints = thisPeakEndIndex - thisPeakStartIndex + 1;
            var xValuesForArea = new double[thisPeakWidthInPoints];
            var yValuesForArea = new double[thisPeakWidthInPoints];

            for (var areaValuesCopyIndex = thisPeakStartIndex; areaValuesCopyIndex <= thisPeakEndIndex; areaValuesCopyIndex++)
            {
                xValuesForArea[areaValuesCopyIndex - thisPeakStartIndex] = xValuesZeroBased[areaValuesCopyIndex];
                yValuesForArea[areaValuesCopyIndex - thisPeakStartIndex] = yValuesZeroBased[areaValuesCopyIndex];
            }

            peak.Area = FindArea(xValuesForArea, yValuesForArea, thisPeakWidthInPoints);
        }

        private void DetectPeaksUseValleys(
            int sourceDataCount,
            IReadOnlyList<double> yValuesZeroBased,
            IReadOnlyList<double> firstDerivative,
            clsPeak newPeak,
            int dataIndex,
            double intensityThreshold,
            int peakHalfWidth
            )
        {
            // Determine the peak width by looking for the adjacent valleys
            // If, while looking, we find intPeakWidthPointsMinimum / 2 points in a row with intensity values below intensityThreshold, then
            // set the edge intPeakHalfWidth - 1 points closer to the peak maximum

            int compareIndex;
            int lowIntensityPointCount;
            if (dataIndex > 0)
            {
                newPeak.LeftEdge = 0;
                lowIntensityPointCount = 0;
                for (compareIndex = dataIndex - 1; compareIndex >= 0; compareIndex += -1)
                {
                    if (firstDerivative[compareIndex] <= 0 &
                        firstDerivative[compareIndex + 1] >= 0)
                    {
                        // Found a valley; this is the left edge
                        newPeak.LeftEdge = compareIndex + 1;
                        break;
                    }

                    if (yValuesZeroBased[compareIndex] < intensityThreshold)
                    {
                        lowIntensityPointCount += 1;
                        if (lowIntensityPointCount > peakHalfWidth)
                        {
                            newPeak.LeftEdge = compareIndex +
                                               (peakHalfWidth - 1);
                            break;
                        }
                    }
                    else
                    {
                        lowIntensityPointCount = 0;
                    }
                }
            }
            else
            {
                newPeak.LeftEdge = 0;
            }

            if (dataIndex < sourceDataCount - 2)
            {
                newPeak.RightEdge = sourceDataCount - 1;
                lowIntensityPointCount = 0;
                for (compareIndex = dataIndex + 1;
                    compareIndex <= sourceDataCount - 2;
                    compareIndex++)
                {
                    if (firstDerivative[compareIndex] <= 0 &
                        firstDerivative[compareIndex + 1] >= 0)
                    {
                        // Found a valley; this is the right edge
                        newPeak.RightEdge = compareIndex;
                        break;
                    }

                    if (yValuesZeroBased[compareIndex] < intensityThreshold)
                    {
                        lowIntensityPointCount += 1;
                        if (lowIntensityPointCount > peakHalfWidth)
                        {
                            newPeak.RightEdge = compareIndex - (peakHalfWidth - 1);
                            break;
                        }
                    }
                    else
                    {
                        lowIntensityPointCount = 0;
                    }
                }
            }
            else
            {
                newPeak.RightEdge = sourceDataCount - 1;
            }

            if (newPeak.LeftEdge > newPeak.LocationIndex)
            {
                Console.WriteLine("Warning: Left edge is > peak center; this is unexpected (clsPeakDetection->DetectPeaks)");
                newPeak.LeftEdge = newPeak.LocationIndex;
            }

            if (newPeak.RightEdge < newPeak.LocationIndex)
            {
                Console.WriteLine("Warning: Right edge is < peak center; this is unexpected (clsPeakDetection->DetectPeaks)");
                newPeak.RightEdge = newPeak.LocationIndex;
            }
        }

        private void DetectPeaksSecondDerivative(
            int sourceDataCount,
            IReadOnlyList<double> yValuesZeroBased,
            IReadOnlyList<double> secondDerivative,
            clsPeak newPeak,
            int dataIndex,
            int peakWidthInSigma)
        {
            // Examine the Second Derivative to determine peak Width (in points)

            double sigma;
            try
            {
                // If secondDerivative[i]) is tiny, the following division will fail
                sigma = Math.Sqrt(Math.Abs(-yValuesZeroBased[dataIndex] / secondDerivative[dataIndex]));
            }
            catch (Exception)
            {
                sigma = 0;
            }
            var widthInPoints = (int)Math.Ceiling(peakWidthInSigma * sigma);

            if (widthInPoints > 4 * sourceDataCount)
            {
                // Predicted width is over 4 times the data count
                // Set it to be 4 times the data count
                widthInPoints = sourceDataCount * 4;
            }

            if (widthInPoints < 2)
            {
                widthInPoints = 2;
            }

            // If the peak width is odd, then center around index i
            // Otherwise, offset to the right of index i
            if (widthInPoints % 2 == 0)
            {
                // Even number
                newPeak.LeftEdge = dataIndex - widthInPoints / 2;
                newPeak.RightEdge = dataIndex + widthInPoints / 2 - 1;
            }
            else
            {
                // Odd number
                newPeak.LeftEdge = dataIndex - (widthInPoints - 1) / 2;
                newPeak.RightEdge = dataIndex + (widthInPoints - 1) / 2;
            }
        }

        private double FindArea(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues, int dataPointCount)
        {
            // yValues() should be 0-based

            // Finds the area under the curve, using trapezoidal integration

            double area = 0;
            for (var i = 0; i <= dataPointCount - 2; i++)
            {
                // Area of a trapezoid (turned on its side) is:
                //   0.5 * d * (h1 + h2)
                // where d is the distance between two points, and h1 and h2 are the intensities
                //   at the 2 points

                area += 0.5 * Math.Abs(xValues[i + 1] - xValues[i]) *
                           (yValues[i] + yValues[i + 1]);
            }

            return area;
        }

        private void FitSegments (
            IReadOnlyList<double> xValues,
            IReadOnlyList<double> yValues,
            int sourceDataCount,
            int peakWidthPointsMinimum,
            int peakWidthMidPoint,
            out double[] firstDerivative,
            out double[] secondDerivative)
        {
            // xValues() and yValues() are zero-based arrays

            const int POLYNOMIAL_ORDER = 2;

            // If POLYNOMIAL_ORDER < 2 Then POLYNOMIAL_ORDER = 2
            // If POLYNOMIAL_ORDER > 9 Then POLYNOMIAL_ORDER = 9

            var segmentX = new double[peakWidthPointsMinimum];
            var segmentY = new double[peakWidthPointsMinimum];

            firstDerivative = new double[sourceDataCount];
            secondDerivative = new double[sourceDataCount];

            for (var startIndex = 0;
                startIndex <= sourceDataCount - peakWidthPointsMinimum - 1;
                startIndex++)
            {
                // Copy the desired segment of data from xValues to segmentX and yValues to segmentY
                int subIndex;
                for (subIndex = startIndex;
                    subIndex <= startIndex + peakWidthPointsMinimum - 1;
                    subIndex++)
                {
                    segmentX[subIndex - startIndex] = xValues[subIndex];
                    segmentY[subIndex - startIndex] = yValues[subIndex];
                }

                // Compute the coefficients for the curve fit
                LeastSquaresFit(segmentX, segmentY, out var coefficients, POLYNOMIAL_ORDER);

                // Compute the dblFirstDerivative at the midpoint
                var midPointIndex = startIndex + peakWidthMidPoint;
                firstDerivative[midPointIndex] = 2 * coefficients[2] * xValues[midPointIndex] + coefficients[1];
                secondDerivative[midPointIndex] = 2 * coefficients[2];
            }
        }

        private void MovePeakLocationToMax(
            int sourceDataCount,
            IReadOnlyList<double> yValuesZeroBased,
            clsPeak peak,
            int peakWidthPointsMinimum)
        {
            // The peak finder often determines the peak center to be a few points away from the peak apex -- check for this
            // Define the maximum allowed peak apex shift to be 33% of intPeakWidthPointsMinimum
            var dataIndexCheckStart = peak.LocationIndex - (int)Math.Floor(peakWidthPointsMinimum / 3.0);
            if (dataIndexCheckStart < 0)
                dataIndexCheckStart = 0;

            var dataIndexCheckEnd = peak.LocationIndex + (int)Math.Floor(peakWidthPointsMinimum / 3.0);
            if (dataIndexCheckEnd > sourceDataCount - 1)
                dataIndexCheckEnd = sourceDataCount - 1;

            var maximumIntensity = yValuesZeroBased[peak.LocationIndex];

            for (var dataIndexCheck = dataIndexCheckStart;
                 dataIndexCheck <= dataIndexCheckEnd;
                 dataIndexCheck++)
            {
                if (yValuesZeroBased[dataIndexCheck] > maximumIntensity)
                {
                    peak.LocationIndex = dataIndexCheck;
                    maximumIntensity = yValuesZeroBased[dataIndexCheck];
                }
            }

            if (peak.LocationIndex < peak.LeftEdge)
                peak.LeftEdge = peak.LocationIndex;

            if (peak.LocationIndex > peak.RightEdge)
                peak.RightEdge = peak.LocationIndex;
        }

        #region "LinearLeastSquaresFitting"

        private void LeastSquaresFit(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues, out double[] coefficients, int polynomialOrder)
        {
            // Code from article "Fit for Purpose" written by Steven Abbot
            // and published in the February 2003 issue of Hardcore Visual Basic.
            // Code excerpted from the VB6 program FitIt
            // URL: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnhcvb03/html/hcvb03b1.asp

            var udtEquationTerms = new udtLeastSquaresFitEquationTermType[polynomialOrder + 1];
            coefficients = new double[polynomialOrder + 1];

            if (xValues.Count < polynomialOrder + 1)
            {
                // Not enough data to fit a curve
            }
            else
            {
                // Define equation for "ax^0 + bx^1 + cx^2", which is the same as "a + bx + cx^2"
                for (var term = 0; term <= polynomialOrder; term++)
                {
                    udtEquationTerms[term].Coefficient = 1;
                    // a, b, c in the above equation
                    udtEquationTerms[term].Func = eTermFunctionConstants.X;
                    // X
                    udtEquationTerms[term].Power = term;
                    // Power that X is raised to
                    udtEquationTerms[term].Inverse = false;
                    // Whether or not to inverse the entire term

                    udtEquationTerms[term].ParamResult = 0;
                }

                LLSqFit(xValues, yValues, ref udtEquationTerms);
                for (var term = 0; term <= polynomialOrder; term++)
                {
                    coefficients[term] = udtEquationTerms[term].ParamResult;
                }
            }
        }

        private void LLSqFit(IReadOnlyList<double> DataX, IReadOnlyList<double> DataY, ref udtLeastSquaresFitEquationTermType[] udtEquationTerms)
        {
            //Linear Least Squares Fit

            var Beta = new double[DataX.Count];
            var CoVar = new double[udtEquationTerms.Length, udtEquationTerms.Length];
            var PFuncVal = new double[udtEquationTerms.Length];

            for (var i = 0; i <= DataX.Count - 1; i++)
            {
                GetLValues(DataX[i], udtEquationTerms, ref PFuncVal);

                var ym = DataY[i];
                for (var L = 0; L <= udtEquationTerms.Length - 1; L++)
                {
                    for (var m = 0; m <= L; m++)
                    {
                        CoVar[L, m] += PFuncVal[L] * PFuncVal[m];
                    }
                    Beta[L] += ym * PFuncVal[L];
                }
            }

            for (var j = 1; j <= udtEquationTerms.Length - 1; j++)
            {
                for (var k = 0; k <= j - 1; k++)
                {
                    CoVar[k, j] = CoVar[j, k];
                }
            }

            if (GaussJordan(ref CoVar, udtEquationTerms.Length, ref Beta))
            {
                for (var L = 0; L <= udtEquationTerms.Length - 1; L++)
                {
                    udtEquationTerms[L].ParamResult = Beta[L];
                }

                return;
            }

            // Error fitting; clear coefficients
            for (var L = 0; L <= udtEquationTerms.Length - 1; L++)
            {
                udtEquationTerms[L].ParamResult = 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="X"></param>
        /// <param name="udtEquationTerms"></param>
        /// <param name="PFuncVal">LValues (output)</param>
        private void GetLValues(double X, IReadOnlyList<udtLeastSquaresFitEquationTermType> udtEquationTerms, ref double[] PFuncVal)
        {
            // Get values for Linear Least Squares
            // udtEquationTerms() is a 0-based array defining the form of each term

            // Use the following for a 2nd order polynomial fit
            //'Define the formula via PFuncVal
            //'In this case NTerms=3 and y=a+bx+cx^2
            //PFuncVal[1] = 1
            //PFuncVal[2] = X
            //PFuncVal[3] = X ^ 2

            if (PFuncVal == null)
                PFuncVal = new double[udtEquationTerms.Count];

            //f = "1,X,Log(X),Log10(X),Exp(X),Sin(X),Cos(X),Tan(X),ATAN(X)"
            for (var i = 0; i <= udtEquationTerms.Count - 1; i++)
            {
                var udtTerm = udtEquationTerms[i];
                double v = 0;

                switch (udtTerm.Func)
                {
                    case eTermFunctionConstants.One:
                        v = 1;
                        break;
                    case eTermFunctionConstants.X:
                        v = Math.Pow(X, udtTerm.Power);
                        break;
                    case eTermFunctionConstants.LogX:
                        if (udtTerm.Coefficient * X <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            v = Math.Pow(Math.Log(udtTerm.Coefficient * X), udtTerm.Power);
                        }
                        break;
                    case eTermFunctionConstants.Log10X:
                        if (udtTerm.Coefficient * X <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            v = Math.Pow(Math.Log10(udtTerm.Coefficient * X), udtTerm.Power);
                        }
                        break;
                    case eTermFunctionConstants.ExpX:
                        v = Math.Pow(Math.Exp(udtTerm.Coefficient * X), udtTerm.Power);
                        break;
                    case eTermFunctionConstants.SinX:
                        v = Math.Pow(Math.Sin(udtTerm.Coefficient * X), udtTerm.Power);
                        break;
                    case eTermFunctionConstants.CosX:
                        v = Math.Pow(Math.Cos(udtTerm.Coefficient * X), udtTerm.Power);
                        break;
                    case eTermFunctionConstants.TanX:
                        v = Math.Pow(Math.Tan(udtTerm.Coefficient * X), udtTerm.Power);
                        break;
                    case eTermFunctionConstants.ATanX:
                        v = Math.Pow(Math.Atan(udtTerm.Coefficient * X), udtTerm.Power);
                        break;
                }

                if (udtTerm.Inverse)
                {
                    if (Math.Abs(v) < double.Epsilon)
                    {
                        PFuncVal[i] = 0;
                        //NOT V...
                    }
                    else
                    {
                        PFuncVal[i] = 1 / v;
                    }
                    //INV(I) = FALSE
                }
                else
                {
                    PFuncVal[i] = v;
                }
            }
        }

        /// <summary>
        /// GaussJordan elimination for LLSq and LM solving
        /// </summary>
        /// <param name="A"></param>
        /// <param name="termCount"></param>
        /// <param name="b"></param>
        /// <returns>True if success, False if an error</returns>
        private bool GaussJordan(
            ref double[,] A,
            int termCount,
            ref double[] b)
        {

            var indexC = new int[termCount];
            var indexR = new int[termCount];

            // ReSharper disable once IdentifierTypo
            var ipiv = new int[termCount];

            var columnIndex = 0;
            var rowIndex = 0;

            try
            {
                for (var i = 0; i <= termCount - 1; i++)
                {
                    double bigValue = 0;
                    for (var j = 0; j <= termCount - 1; j++)
                    {
                        if (ipiv[j] != 1)
                        {
                            for (var k = 0; k <= termCount - 1; k++)
                            {
                                if (ipiv[k] == 0)
                                {
                                    if (Math.Abs(A[j, k]) >= bigValue)
                                    {
                                        bigValue = Math.Abs(A[j, k]);
                                        rowIndex = j;
                                        columnIndex = k;
                                    }
                                }
                            }
                        }
                    }

                    ipiv[columnIndex] += 1;
                    if (rowIndex != columnIndex)
                    {
                        double swapValue;
                        for (var L = 0; L <= termCount - 1; L++)
                        {
                            swapValue = A[rowIndex, L];
                            A[rowIndex, L] = A[columnIndex, L];
                            A[columnIndex, L] = swapValue;
                        }
                        swapValue = b[rowIndex];
                        b[rowIndex] = b[columnIndex];
                        b[columnIndex] = swapValue;
                    }

                    indexR[i] = rowIndex;
                    indexC[i] = columnIndex;
                    if (Math.Abs(A[columnIndex, columnIndex]) < double.Epsilon)
                    {
                        // Error, the matrix was singular
                        return false;
                    }

                    var PivInv = 1 / A[columnIndex, columnIndex];
                    A[columnIndex, columnIndex] = 1;
                    for (var L = 0; L <= termCount - 1; L++)
                    {
                        A[columnIndex, L] *= PivInv;
                    }

                    b[columnIndex] *= PivInv;
                    for (var ll = 0; ll <= termCount - 1; ll++)
                    {
                        if (ll != columnIndex)
                        {
                            var multiplier = A[ll, columnIndex];
                            A[ll, columnIndex] = 0;
                            for (var L = 0; L <= termCount - 1; L++)
                            {
                                A[ll, L] -= A[columnIndex, L] * multiplier;
                            }
                            b[ll] -= b[columnIndex] * multiplier;
                        }
                    }
                }

                for (var L = termCount - 1; L >= 0; L += -1)
                {
                    if (indexR[L] != indexC[L])
                    {
                        for (var k = 0; k <= termCount - 1; k++)
                        {
                            var swapValue = A[k, indexR[L]];
                            A[k, indexR[L]] = A[k, indexC[L]];
                            A[k, indexC[L]] = swapValue;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
