using System;
using System.Collections.Generic;

namespace MASICPeakFinder
{
    // ReSharper disable once CommentTypo

    /// <summary>
    /// <para>
    /// Peak detection methods
    /// Written by Matthew Monroe in roughly 2001 at UNC (Chapel Hill, NC)
    /// Kevin Lan provided the concept of Magnitude Concavity fitting
    /// Ported from LabView code to VB 6 in June 2003 at PNNL (Richland, WA)
    /// Ported from VB 6 to VB.NET in October 2003
    /// Switched from using the eols.dll least squares fitting method to using a local function
    /// Ported to C# in February 2015
    /// </para>
    /// <para>
    /// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
    /// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
    /// </para>
    /// </summary>
    internal class PeakDetection
    {
        // Ignore Spelling: cx, lan, struct

        private enum TermFunctionConstants
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

        private struct LeastSquaresFitEquationTermType
        {
            public TermFunctionConstants Func;
            public double Power;
            public double Coefficient;

            public bool Inverse;

            /// <summary>
            /// Stores the coefficient determined for the fit
            /// </summary>
            public double ParamResult;
        }

        /// <summary>
        /// Compute slope
        /// </summary>
        /// <param name="xValues"></param>
        /// <param name="yValues"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        // ReSharper disable once UnusedMember.Global
        public double ComputeSlope(
            double[] xValues,
            double[] yValues,
            int startIndex,
            int endIndex)
        {
            const int POLYNOMIAL_ORDER = 1;

            if (xValues == null || xValues.Length == 0)
                return 0;

            var segmentCount = endIndex - startIndex + 1;

            var segmentX = new double[segmentCount];
            var segmentY = new double[segmentCount];

            // Copy the desired segment of data from xValues to segmentX and yValues to segmentY
            for (var i = startIndex; i <= endIndex; i++)
            {
                segmentX[i - startIndex] = xValues[i];
                segmentY[i - startIndex] = yValues[i];
            }

            // Compute the coefficients for the curve fit
            LeastSquaresFit(segmentX, segmentY, out var coefficients, POLYNOMIAL_ORDER);

            return coefficients[1];
        }

        /// <summary>
        /// Finds peaks in the parallel arrays xValues() and yValues()
        /// </summary>
        /// <remarks>
        /// Note that the maximum value of intensityThreshold vs. MaxValue*peakDetectIntensityThresholdPercentageOfMaximum is used as the minimum
        /// For example, if intensityThreshold = 10 and peakDetectIntensityThresholdPercentageOfMaximum =  5 (indicating 5%),
        /// then if the maximum of yValues() is 50, the minimum intensity of identified peaks is 10, and not 2.5
        /// However, if the maximum of yValues() is 500, the minimum intensity of identified peaks is 50, and not 10
        /// </remarks>
        /// <param name="xValues"></param>
        /// <param name="yValues"></param>
        /// <param name="intensityThresholdAbsoluteMinimum">Minimum absolute intensity allowable for a peak</param>
        /// <param name="peakWidthPointsMinimum"></param>
        /// <param name="peakDetectIntensityThresholdPercentageOfMaximum">Use this to specify a minimum intensity as a percentage of the maximum peak intensity</param>
        /// <param name="peakWidthInSigma"></param>
        /// <param name="useValleysForPeakWidth"></param>
        /// <param name="movePeakLocationToMaxIntensity"></param>
        /// <returns>
        /// List of detected peaks (list of clsPeakInfo)
        /// .PeakLocation is the location of the peak (index of the peak apex in the source arrays)
        /// .LeftEdge is the left edge of the peak (in points, not actual units); this value could be negative if useValleysForPeakWidth = False
        /// .RightEdge is the right edge of the peak (in points); this value could be larger than sourceDataCount-1 if useValleysForPeakWidth = False
        /// .PeakArea is the peak area
        /// Compute peak width using: peakWidthPoints = newPeak.RightEdge - newPeak.LeftEdge + 1
        /// </returns>
        public List<PeakInfo> DetectPeaks(
            double[] xValues,
            double[] yValues,
            double intensityThresholdAbsoluteMinimum,
            int peakWidthPointsMinimum,
            int peakDetectIntensityThresholdPercentageOfMaximum = 0,
            int peakWidthInSigma = 4,
            bool useValleysForPeakWidth = true,
            bool movePeakLocationToMaxIntensity = true)
        {
            // This method uses the Magnitude-Concavity method, wherein a second order
            // polynomial is fit to the points within the window, giving a_2*x^2 + a_1*x + a_0
            // Given this, a_1 is the first derivative and a_2 is the second derivative
            // From this, the first derivative gives the index of the peak apex
            // The standard deviation (s) can be found using:
            //   s = sqrt(-h(t_r) / h''(t_r))
            // where h(t_r) is the height of the peak at the peak center
            // and h''(t_r) is the height of the second derivative of the peak
            // In chromatography, the baseline peak widthInPoints = 4*sigma

            // Initialize the list of detected peaks
            var detectedPeaks = new List<PeakInfo>(100);

            try
            {
                var sourceDataCount = xValues.Length;

                if (sourceDataCount == 0)
                    return detectedPeaks;

                // Reserve space for the first and second derivatives
                var firstDerivative = new double[sourceDataCount];
                var secondDerivative = new double[sourceDataCount];

                // The mid point width is the minimum width divided by 2, rounded down
                var peakHalfWidth = (int)Math.Floor(peakWidthPointsMinimum / 2.0);

                // Find the maximum intensity in the source data
                double maximumIntensity = 0;

                for (var dataIndex = 0; dataIndex < sourceDataCount; dataIndex++)
                {
                    if (yValues[dataIndex] > maximumIntensity)
                    {
                        maximumIntensity = yValues[dataIndex];
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
                FitSegments(xValues, yValues, sourceDataCount, peakWidthPointsMinimum,
                            peakHalfWidth, ref firstDerivative, ref secondDerivative);

                if (peakWidthInSigma < 1)
                    peakWidthInSigma = 1;

                // Examine the First Derivative function and look for zero crossings (in the downward direction)
                // If looking for valleys, would look for zero crossings in the upward direction
                // Only significant if intensity of point is above threshold
                if (peakWidthPointsMinimum <= 0)
                    peakWidthPointsMinimum = 1;

                // We'll start looking for peaks halfway into peakWidthPointsMinimum
                var indexFirst = peakHalfWidth;
                var indexLast = sourceDataCount - 1 - peakHalfWidth;

                for (var index = indexFirst; index <= indexLast; index++)
                {
                    if (firstDerivative[index] > 0 && firstDerivative[index + 1] < 0)
                    {
                        // Possible peak
                        if (yValues[index] >= intensityThreshold || yValues[index + 1] >= intensityThreshold)
                        {
                            // Actual peak

                            var newPeak = new PeakInfo(index);

                            if (useValleysForPeakWidth)
                            {
                                DetectPeaksUseValleys(sourceDataCount, yValues, firstDerivative, newPeak, index, intensityThreshold, peakHalfWidth);
                            }
                            else
                            {
                                DetectPeaksSecondDerivative(sourceDataCount, yValues, secondDerivative, newPeak, index, peakWidthInSigma);
                            }

                            detectedPeaks.Add(newPeak);
                        }
                    }
                }

                // Compute the peak areas
                foreach (var peak in detectedPeaks)
                {
                    ComputePeakArea(sourceDataCount, xValues, yValues, peak);

                    if (movePeakLocationToMaxIntensity)
                    {
                        MovePeakLocationToMax(sourceDataCount, yValues, peak, peakWidthPointsMinimum);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Error in clsPeakDetection->DetectPeaks (or in a child function)" + Environment.NewLine + ex.Message);
            }

            return detectedPeaks;
        }

        private void ComputePeakArea(
            int sourceDataCount,
            IReadOnlyList<double> xValues,
            IReadOnlyList<double> yValues,
            PeakInfo peak)
        {
            if (peak.PeakWidth == 0)
            {
                // 0-width peak; this shouldn't happen
                Console.WriteLine("Warning: 0-width peak; this shouldn't happen (clsPeakDetection->DetectPeaks)");
                peak.PeakArea = 0;
                return;
            }

            if (peak.PeakWidth == 1)
            {
                // I don't think this can happen
                // Just in case, we'll set the area equal to the peak intensity
                peak.PeakArea = yValues[peak.PeakLocation];
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
                xValuesForArea[areaValuesCopyIndex - thisPeakStartIndex] = xValues[areaValuesCopyIndex];
                yValuesForArea[areaValuesCopyIndex - thisPeakStartIndex] = yValues[areaValuesCopyIndex];
            }

            peak.PeakArea = FindArea(xValuesForArea, yValuesForArea, thisPeakWidthInPoints);
        }

        private void DetectPeaksUseValleys(
            int sourceDataCount,
            IReadOnlyList<double> yValues,
            IReadOnlyList<double> firstDerivative,
            PeakInfo newPeak,
            int index,
            double intensityThreshold,
            int peakHalfWidth
            )
        {
            // Determine the peak width by looking for the adjacent valleys
            // If, while looking, we find peakWidthPointsMinimum / 2 points in a row with intensity values below intensityThreshold,
            // set the edge peakHalfWidth - 1 points closer to the peak maximum

            if (index > 0)
            {
                newPeak.LeftEdge = 0;
                var lowIntensityPointCount = 0;

                for (var compareIndex = index - 1; compareIndex >= 0; compareIndex--)
                {
                    if (firstDerivative[compareIndex] <= 0 && firstDerivative[compareIndex + 1] >= 0)
                    {
                        // Found a valley; this is the left edge
                        newPeak.LeftEdge = compareIndex + 1;
                        break;
                    }

                    if (yValues[compareIndex] < intensityThreshold)
                    {
                        lowIntensityPointCount++;

                        if (lowIntensityPointCount > peakHalfWidth)
                        {
                            newPeak.LeftEdge = compareIndex + (peakHalfWidth - 1);
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

            if (index < sourceDataCount - 2)
            {
                newPeak.RightEdge = sourceDataCount - 1;
                var lowIntensityPointCount = 0;

                for (var compareIndex = index + 1; compareIndex < sourceDataCount - 1; compareIndex++)
                {
                    if (firstDerivative[compareIndex] <= 0 && firstDerivative[compareIndex + 1] >= 0)
                    {
                        // Found a valley; this is the right edge
                        newPeak.RightEdge = compareIndex;
                        break;
                    }

                    if (yValues[compareIndex] < intensityThreshold)
                    {
                        lowIntensityPointCount++;

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

            if (newPeak.LeftEdge > newPeak.PeakLocation)
            {
                Console.WriteLine("Warning: Left edge is > peak center; this is unexpected (clsPeakDetection->DetectPeaks)");
                newPeak.LeftEdge = newPeak.PeakLocation;
            }

            if (newPeak.RightEdge < newPeak.PeakLocation)
            {
                Console.WriteLine("Warning: Right edge is < peak center; this is unexpected (clsPeakDetection->DetectPeaks)");
                newPeak.RightEdge = newPeak.PeakLocation;
            }
        }

        private void DetectPeaksSecondDerivative(
            int sourceDataCount,
            IReadOnlyList<double> yValues,
            IReadOnlyList<double> secondDerivative,
            PeakInfo newPeak,
            int index,
            int peakWidthInSigma)
        {
            // Examine the Second Derivative to determine peak Width (in points)

            double sigma;

            try
            {
                // If secondDerivative[index]) is tiny, the following division will fail
                sigma = Math.Sqrt(Math.Abs(-yValues[index] / secondDerivative[index]));
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

            // If the peak width is odd, center around index
            // Otherwise, offset to the right of index
            if (widthInPoints % 2 == 0)
            {
                // Even number
                newPeak.LeftEdge = index - (int)Math.Round(widthInPoints / 2.0);
                newPeak.RightEdge = index + (int)Math.Round(widthInPoints / 2.0) - 1;
            }
            else
            {
                // Odd number
                newPeak.LeftEdge = index - (int)Math.Round((widthInPoints - 1) / 2.0);
                newPeak.RightEdge = index + (int)Math.Round((widthInPoints - 1) / 2.0);
            }
        }

        /// <summary>
        /// Finds the area under the curve, using trapezoidal integration
        /// </summary>
        /// <param name="xValues">X values</param>
        /// <param name="yValues">Y values (intensities)</param>
        /// <param name="dataPointCount"></param>
        private double FindArea(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues, int dataPointCount)
        {
            double area = 0;

            for (var index = 0; index < dataPointCount - 1; index++)
            {
                // Area of a trapezoid (turned on its side) is:
                // 0.5 * d * (h1 + h2)
                // where d is the distance between two points, and h1 and h2 are the intensities
                // at the 2 points

                area += 0.5 * Math.Abs(xValues[index + 1] - xValues[index]) * (yValues[index] + yValues[index + 1]);
            }

            return area;
        }

        private void FitSegments(
            IReadOnlyList<double> xValues,
            IReadOnlyList<double> yValues,
            int sourceDataCount,
            int peakWidthPointsMinimum,
            int peakWidthMidPoint,
            ref double[] firstDerivative,
            ref double[] secondDerivative)
        {
            const int POLYNOMIAL_ORDER = 2;

            // if (POLYNOMIAL_ORDER < 2) POLYNOMIAL_ORDER = 2;
            // if (POLYNOMIAL_ORDER > 9) POLYNOMIAL_ORDER = 9;

            var segmentX = new double[peakWidthPointsMinimum];
            var segmentY = new double[peakWidthPointsMinimum];

            for (var startIndex = 0; startIndex < sourceDataCount - peakWidthPointsMinimum; startIndex++)
            {
                // Copy the desired segment of data from xValues to segmentX and yValues to segmentY
                for (var subIndex = startIndex; subIndex < startIndex + peakWidthPointsMinimum; subIndex++)
                {
                    segmentX[subIndex - startIndex] = xValues[subIndex];
                    segmentY[subIndex - startIndex] = yValues[subIndex];
                }

                // Compute the coefficients for the curve fit
                LeastSquaresFit(segmentX, segmentY, out var coefficients, POLYNOMIAL_ORDER);

                // Compute the firstDerivative at the midpoint
                var midPointIndex = startIndex + peakWidthMidPoint;
                firstDerivative[midPointIndex] = 2 * coefficients[2] * xValues[midPointIndex] + coefficients[1];
                secondDerivative[midPointIndex] = 2 * coefficients[2];
            }
        }

        private void MovePeakLocationToMax(
            int sourceDataCount,
            IReadOnlyList<double> yValues,
            PeakInfo peak,
            int peakWidthPointsMinimum)
        {
            // The peak finder often determines the peak center to be a few points away from the peak apex -- check for this
            // Define the maximum allowed peak apex shift to be 33% of peakWidthPointsMinimum
            var dataIndexCheckStart = peak.PeakLocation - (int)Math.Floor(peakWidthPointsMinimum / 3.0);

            if (dataIndexCheckStart < 0)
                dataIndexCheckStart = 0;

            var dataIndexCheckEnd = peak.PeakLocation + (int)Math.Floor(peakWidthPointsMinimum / 3.0);

            if (dataIndexCheckEnd > sourceDataCount - 1)
                dataIndexCheckEnd = sourceDataCount - 1;

            var maximumIntensity = yValues[peak.PeakLocation];

            for (var dataIndexCheck = dataIndexCheckStart; dataIndexCheck <= dataIndexCheckEnd; dataIndexCheck++)
            {
                if (yValues[dataIndexCheck] > maximumIntensity)
                {
                    peak.PeakLocation = dataIndexCheck;
                    maximumIntensity = yValues[dataIndexCheck];
                }
            }

            if (peak.PeakLocation < peak.LeftEdge)
                peak.LeftEdge = peak.PeakLocation;

            if (peak.PeakLocation > peak.RightEdge)
                peak.RightEdge = peak.PeakLocation;
        }

        // ReSharper disable CommentTypo

        /// <summary>
        /// Least squares fit
        /// </summary>
        /// <remarks>
        /// Code from article "Fit for Purpose" written by Steven Abbot
        /// and published in the February 2003 issue of Hardcore Visual Basic.
        /// Code excerpted from the VB6 program FitIt, previously available at
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnhcvb03/html/hcvb03b1.asp
        /// </remarks>
        /// <param name="xValues"></param>
        /// <param name="yValues"></param>
        /// <param name="coefficients"></param>
        /// <param name="polynomialOrder"></param>
        // ReSharper restore CommentTypo
        private void LeastSquaresFit(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues, out double[] coefficients, int polynomialOrder)
        {
            var equationTerms = new LeastSquaresFitEquationTermType[polynomialOrder + 1];
            coefficients = new double[polynomialOrder + 1];

            if (xValues.Count < polynomialOrder + 1)
            {
                // Not enough data to fit a curve
                return;
            }

            // Define equation for "ax^0 + bx^1 + cx^2", which is the same as "a + bx + cx^2"
            for (var term = 0; term <= polynomialOrder; term++)
            {
                // array of struct: Direct assignment, indexing the array every time, works.
                equationTerms[term].Coefficient = 1;                        // a, b, c in the above equation
                equationTerms[term].Func = TermFunctionConstants.X;         // X
                equationTerms[term].Power = term;                           // Power that X is raised to
                equationTerms[term].Inverse = false;                        // Whether or not to inverse the entire term

                equationTerms[term].ParamResult = 0;
            }

            LLSqFit(xValues, yValues, ref equationTerms);

            for (var term = 0; term <= polynomialOrder; term++)
            {
                coefficients[term] = equationTerms[term].ParamResult;
            }
        }

        /// <summary>
        /// Linear Least Squares Fit
        /// </summary>
        /// <param name="xValues"></param>
        /// <param name="yValues"></param>
        /// <param name="equationTerms"></param>
        private void LLSqFit(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues, ref LeastSquaresFitEquationTermType[] equationTerms)
        {
            var beta = new double[xValues.Count];
            var coVariance = new double[equationTerms.Length, equationTerms.Length];
            var pFuncValue = new double[equationTerms.Length];

            for (var i = 0; i < xValues.Count; i++)
            {
                GetLValues(xValues[i], equationTerms, ref pFuncValue);

                var ym = yValues[i];

                for (var L = 0; L < equationTerms.Length; L++)
                {
                    for (var m = 0; m <= L; m++)
                    {
                        coVariance[L, m] += pFuncValue[L] * pFuncValue[m];
                    }

                    beta[L] += ym * pFuncValue[L];
                }
            }

            for (var j = 1; j < equationTerms.Length; j++)
            {
                for (var k = 0; k < j; k++)
                {
                    coVariance[k, j] = coVariance[j, k];
                }
            }

            if (GaussJordan(ref coVariance, equationTerms.Length, ref beta))
            {
                for (var L = 0; L < equationTerms.Length; L++)
                {
                    equationTerms[L].ParamResult = beta[L];
                }

                return;
            }

            // Error fitting; clear coefficients
            for (var L = 0; L < equationTerms.Length; L++)
            {
                equationTerms[L].ParamResult = 0;
            }
        }

        /// <summary>
        /// Get L values
        /// </summary>
        /// <param name="X"></param>
        /// <param name="equationTerms"></param>
        /// <param name="pFuncValue">LValues (output)</param>
        private void GetLValues(double X, IReadOnlyList<LeastSquaresFitEquationTermType> equationTerms, ref double[] pFuncValue)
        {
            // Get values for Linear Least Squares
            // equationTerms() is a 0-based array defining the form of each term

            var v = 0.0;

            // Use the following for a 2nd order polynomial fit
            // // Define the formula via pFuncValue
            // // In this case NTerms=3 and y=a+bx+cx^2
            // pFuncValue[1] = 1;
            // pFuncValue[2] = X;
            // pFuncValue[3] = X * X;

            // f = "1,X,Log(X),Log10(X),Exp(X),Sin(X),Cos(X),Tan(X),ATAN(X)"
            for (var i = 0; i < equationTerms.Count; i++)
            {
                // equationTerms is an array of structures: No assignment is performed, we don't need to copy the end value back.
                var term = equationTerms[i];

                switch (term.Func)
                {
                    case TermFunctionConstants.One:
                        v = 1;
                        break;

                    case TermFunctionConstants.X:
                        v = Math.Pow(X, term.Power);
                        break;

                    case TermFunctionConstants.LogX:
                        if (term.Coefficient * X <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            v = Math.Pow(Math.Log(term.Coefficient * X), term.Power);
                        }
                        break;

                    case TermFunctionConstants.Log10X:
                        if (term.Coefficient * X <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            v = Math.Pow(Math.Log10(term.Coefficient * X), term.Power);
                        }
                        break;

                    case TermFunctionConstants.ExpX:
                        v = Math.Pow(Math.Exp(term.Coefficient * X), term.Power);
                        break;

                    case TermFunctionConstants.SinX:
                        v = Math.Pow(Math.Sin(term.Coefficient * X), term.Power);
                        break;

                    case TermFunctionConstants.CosX:
                        v = Math.Pow(Math.Cos(term.Coefficient * X), term.Power);
                        break;

                    case TermFunctionConstants.TanX:
                        v = Math.Pow(Math.Tan(term.Coefficient * X), term.Power);
                        break;

                    case TermFunctionConstants.ATanX:
                        v = Math.Pow(Math.Atan(term.Coefficient * X), term.Power);
                        break;
                }

                if (term.Inverse)
                {
                    if (Math.Abs(v) < double.Epsilon)
                    {
                        pFuncValue[i] = 0;
                    }
                    else // NOT V...
                    {
                        pFuncValue[i] = 1 / v;
                    }
                }
                else // INV(I) = FALSE
                {
                    pFuncValue[i] = v;
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
        private bool GaussJordan(ref double[,] A, int termCount, ref double[] b)
        {
            var indexC = new int[termCount];
            var indexR = new int[termCount];

            // ReSharper disable once IdentifierTypo
            var ipiv = new int[termCount];

            var columnIndex = 0;
            var rowIndex = 0;

            try
            {
                for (var i = 0; i < termCount; i++)
                {
                    double bigValue = 0;

                    for (var j = 0; j < termCount; j++)
                    {
                        if (ipiv[j] != 1)
                        {
                            for (var k = 0; k < termCount; k++)
                            {
                                if (ipiv[k] == 0 && Math.Abs(A[j, k]) >= bigValue)
                                {
                                    bigValue = Math.Abs(A[j, k]);
                                    rowIndex = j;
                                    columnIndex = k;
                                }
                            }
                        }
                    }

                    ipiv[columnIndex]++;

                    if (rowIndex != columnIndex)
                    {
                        double swapValue;

                        for (var L = 0; L < termCount; L++)
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

                    for (var L = 0; L < termCount; L++)
                    {
                        A[columnIndex, L] *= PivInv;
                    }

                    b[columnIndex] *= PivInv;

                    for (var ll = 0; ll < termCount; ll++)
                    {
                        if (ll != columnIndex)
                        {
                            var multiplier = A[ll, columnIndex];
                            A[ll, columnIndex] = 0;

                            for (var L = 0; L < termCount; L++)
                            {
                                A[ll, L] -= A[columnIndex, L] * multiplier;
                            }

                            b[ll] -= b[columnIndex] * multiplier;
                        }
                    }
                }

                for (var L = termCount - 1; L >= 0; L--)
                {
                    if (indexR[L] != indexC[L])
                    {
                        for (var k = 0; k < termCount; k++)
                        {
                            // ReSharper disable once SwapViaDeconstruction
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
    }
}
