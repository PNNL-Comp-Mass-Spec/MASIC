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
            double[] xValsZeroBased,
            double[] yValsZeroBased,
            int startIndex,
            int endIndex)
        {

            const int POLYNOMIAL_ORDER = 1;

            if (xValsZeroBased == null || xValsZeroBased.Length == 0)
                return 0;

            var segmentCount = endIndex - startIndex + 1;

            var segmentX = new double[segmentCount];
            var segmentY = new double[segmentCount];

            // Copy the desired segment of data from xVals to segmentX and yVals to segmentY
            for (var i = startIndex; i <= endIndex; i++)
            {
                segmentX[i - startIndex] = xValsZeroBased[i];
                segmentY[i - startIndex] = yValsZeroBased[i];
            }

            // Compute the coefficients for the curve fit
            LeastSquaresFit(segmentX, segmentY, out var coefficients, POLYNOMIAL_ORDER);

            return coefficients[1];

        }

        public List<clsPeak> DetectPeaks(
            double[] xValsZeroBased,
            double[] yValsZeroBased,
            double intensityThresholdAbsoluteMinimum,
            int peakWidthPointsMinimum,
            int peakDetectIntensityThresholdPercentageOfMaximum = 0,
            int peakWidthInSigma = 4,
            bool useValleysForPeakWidth = true,
            bool movePeakLocationToMaxIntensity = true)
        {
            // Finds peaks in the parallel arrays xValsZeroBased[] and yValsZeroBased[]
            // intensityThreshold is the minimum absolute intensity allowable for a peak
            // peakDetectIntensityThresholdPercentageOfMaximum allows one to specify a minimum intensity as a percentage of the maximum peak intensity
            // Note that the maximum value of intensityThreshold vs. MaxValue * peakDetectIntensityThresholdPercentageOfMaximum is used as the minimum
            // For example, if intensityThreshold = 10 and peakDetectIntensityThresholdPercentageOfMaximum =  5 (indicating 5%),
            //   then if the maximum of yValsZeroBased[] is 50, then the minimum intensity of identified peaks is 10, and not 2.5
            //   However, if the maximum of yValsZeroBased[] is 500, then the minimum intensity of identified peaks is 50, and not 10

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

                var sourceDataCount = xValsZeroBased.Length;
                if (sourceDataCount <= 0)
                    return detectedPeaks;

                // The mid point width is the minimum width divided by 2, rounded down
                var peakHalfWidth = (int)Math.Floor(peakWidthPointsMinimum / 2.0);

                // Find the maximum intensity in the source data
                double maximumIntensity = 0;
                for (var dataIndex = 0; dataIndex <= sourceDataCount - 1; dataIndex++)
                {
                    if (yValsZeroBased[dataIndex] > maximumIntensity)
                    {
                        maximumIntensity = yValsZeroBased[dataIndex];
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
                FitSegments(xValsZeroBased, yValsZeroBased, sourceDataCount, peakWidthPointsMinimum,
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
                        if (yValsZeroBased[dataIndex] >= intensityThreshold |
                            yValsZeroBased[dataIndex + 1] >= intensityThreshold)
                        {
                            // Actual peak

                            var newPeak = new clsPeak
                            {
                                LocationIndex = dataIndex
                            };

                            detectedPeaks.Add(newPeak);

                            if (useValleysForPeakWidth)
                            {
                                DetectPeaksUseValleys(sourceDataCount, yValsZeroBased, firstDerivative, newPeak, dataIndex, intensityThreshold, peakHalfWidth);
                            }
                            else
                            {
                                DetectPeaksSecondDerivative(sourceDataCount, yValsZeroBased, secondDerivative, newPeak, dataIndex, peakWidthInSigma);
                            }
                        }
                    }
                }


                // Compute the peak areas
                foreach (var peak in detectedPeaks)
                {
                    ComputePeakArea(sourceDataCount, xValsZeroBased, yValsZeroBased, peak);

                    if (movePeakLocationToMaxIntensity)
                    {
                        MovePeakLocationToMax(sourceDataCount, yValsZeroBased, peak, peakWidthPointsMinimum);
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
            double[] xValsZeroBased,
            double[] yValsZeroBased,
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
                peak.Area = yValsZeroBased[peak.LocationIndex];
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
            var xValsForArea = new double[thisPeakWidthInPoints];
            var yValsForArea = new double[thisPeakWidthInPoints];


            for (var areaValsCopyIndex = thisPeakStartIndex; areaValsCopyIndex <= thisPeakEndIndex; areaValsCopyIndex++)
            {
                xValsForArea[areaValsCopyIndex - thisPeakStartIndex] = xValsZeroBased[areaValsCopyIndex];
                yValsForArea[areaValsCopyIndex - thisPeakStartIndex] = yValsZeroBased[areaValsCopyIndex];
            }

            peak.Area = FindArea(xValsForArea, yValsForArea, thisPeakWidthInPoints);

        }

        private void DetectPeaksUseValleys(
            int sourceDataCount,
            double[] yValsZeroBased,
            double[] firstDerivative,
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

                    if (yValsZeroBased[compareIndex] < intensityThreshold)
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

                    if (yValsZeroBased[compareIndex] < intensityThreshold)
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
            double[] yValsZeroBased,
            double[] secondDerivative,
            clsPeak newPeak,
            int dataIndex,
            int peakWidthInSigma)
        {
            // Examine the Second Derivative to determine peak Width (in points)

            double sigma;
            try
            {
                // If secondDerivative[i]) is tiny, the following division will fail
                sigma = Math.Sqrt(Math.Abs(-yValsZeroBased[dataIndex] / secondDerivative[dataIndex]));
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

        private double FindArea(double[] xVals, double[] yVals, int dataPointCount)
        {
            // yVals() should be 0-based

            // Finds the area under the curve, using trapezoidal integration

            double area = 0;
            for (var i = 0; i <= dataPointCount - 2; i++)
            {
                // Area of a trapezoid (turned on its side) is:
                //   0.5 * d * (h1 + h2)
                // where d is the distance between two points, and h1 and h2 are the intensities
                //   at the 2 points

                area += 0.5 * Math.Abs(xVals[i + 1] - xVals[i]) *
                           (yVals[i] + yVals[i + 1]);
            }

            return area;

        }

        private void FitSegments (
            double[] xVals,
            double[] yVals,
            int sourceDataCount,
            int peakWidthPointsMinimum,
            int peakWidthMidPoint,
            out double[] firstDerivative,
            out double[] secondDerivative)
        {
            // xVals() and yVals() are zero-based arrays

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
                // Copy the desired segment of data from xVals to segmentX and yVals to segmentY
                int subIndex;
                for (subIndex = startIndex;
                    subIndex <= startIndex + peakWidthPointsMinimum - 1;
                    subIndex++)
                {
                    segmentX[subIndex - startIndex] = xVals[subIndex];
                    segmentY[subIndex - startIndex] = yVals[subIndex];
                }

                // Compute the coefficients for the curve fit
                LeastSquaresFit(segmentX, segmentY, out var coefficients, POLYNOMIAL_ORDER);

                // Compute the dblFirstDerivative at the midpoint
                var midPointIndex = startIndex + peakWidthMidPoint;
                firstDerivative[midPointIndex] = 2 * coefficients[2] * xVals[midPointIndex] + coefficients[1];
                secondDerivative[midPointIndex] = 2 * coefficients[2];

            }

        }

        private void MovePeakLocationToMax(
            int sourceDataCount,
            double[] yValsZeroBased,
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

            var maximumIntensity = yValsZeroBased[peak.LocationIndex];

            for (var dataIndexCheck = dataIndexCheckStart;
                 dataIndexCheck <= dataIndexCheckEnd;
                 dataIndexCheck++)
            {
                if (yValsZeroBased[dataIndexCheck] > maximumIntensity)
                {
                    peak.LocationIndex = dataIndexCheck;
                    maximumIntensity = yValsZeroBased[dataIndexCheck];
                }
            }

            if (peak.LocationIndex < peak.LeftEdge)
                peak.LeftEdge = peak.LocationIndex;

            if (peak.LocationIndex > peak.RightEdge)
                peak.RightEdge = peak.LocationIndex;
        }

        #region "LinearLeastSquaresFitting"

        private void LeastSquaresFit(double[] xVals, double[] yVals, out double[] coefficients, int polynomialOrder)
        {

            // Code from article "Fit for Purpose" written by Steven Abbot
            // and published in the February 2003 issue of Hardcore Visual Basic.
            // Code excerpted from the VB6 program FitIt
            // URL: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnhcvb03/html/hcvb03b1.asp

            var udtEquationTerms = new udtLeastSquaresFitEquationTermType[polynomialOrder + 1];
            coefficients = new double[polynomialOrder + 1];

            if (xVals.Length < polynomialOrder + 1)
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

                LLSqFit(xVals, yVals, ref udtEquationTerms);
                for (var term = 0; term <= polynomialOrder; term++)
                {
                    coefficients[term] = udtEquationTerms[term].ParamResult;
                }
            }
        }

        private void LLSqFit(double[] DataX, double[] DataY, ref udtLeastSquaresFitEquationTermType[] udtEquationTerms)
        {

            //Linear Least Squares Fit

            var Beta = new double[DataX.Length];
            var CoVar = new double[udtEquationTerms.Length, udtEquationTerms.Length];
            var PFuncVal = new double[udtEquationTerms.Length];

            for (var i = 0; i <= DataX.Length - 1; i++)
            {
                GetLVals(DataX[i], udtEquationTerms, ref PFuncVal);

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
        private void GetLVals(double X, udtLeastSquaresFitEquationTermType[] udtEquationTerms, ref double[] PFuncVal)
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
                PFuncVal = new double[udtEquationTerms.Length];

            //f = "1,X,Log(X),Log10(X),Exp(X),Sin(X),Cos(X),Tan(X),ATAN(X)"
            for (var i = 0; i <= udtEquationTerms.Length - 1; i++)
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

        private bool GaussJordan(
            ref double[,] A,
            int termCount,
            ref double[] b)
        {

            // GaussJordan elimination for LLSq and LM solving
            // Returns True if success, False if an error

            var indxc = new int[termCount];
            var indxr = new int[termCount];
            var ipiv = new int[termCount];

            var icol = 0;
            var irow = 0;

            try
            {
                double Dum;
                for (var i = 0; i <= termCount - 1; i++)
                {
                    double Big = 0;
                    for (var j = 0; j <= termCount - 1; j++)
                    {
                        if (ipiv[j] != 1)
                        {
                            for (var k = 0; k <= termCount - 1; k++)
                            {
                                if (ipiv[k] == 0)
                                {
                                    if (Math.Abs(A[j, k]) >= Big)
                                    {
                                        Big = Math.Abs(A[j, k]);
                                        irow = j;
                                        icol = k;
                                    }
                                }
                            }
                        }
                    }

                    ipiv[icol] += 1;
                    if (irow != icol)
                    {
                        for (var L = 0; L <= termCount - 1; L++)
                        {
                            Dum = A[irow, L];
                            A[irow, L] = A[icol, L];
                            A[icol, L] = Dum;
                        }
                        Dum = b[irow];
                        b[irow] = b[icol];
                        b[icol] = Dum;
                    }

                    indxr[i] = irow;
                    indxc[i] = icol;
                    if (Math.Abs(A[icol, icol]) < double.Epsilon)
                    {
                        // Error, the matrix was singular
                        return false;
                    }

                    var PivInv = 1 / A[icol, icol];
                    A[icol, icol] = 1;
                    for (var L = 0; L <= termCount - 1; L++)
                    {
                        A[icol, L] *= PivInv;
                    }

                    b[icol] *= PivInv;
                    for (var ll = 0; ll <= termCount - 1; ll++)
                    {
                        if (ll != icol)
                        {
                            Dum = A[ll, icol];
                            A[ll, icol] = 0;
                            for (var L = 0; L <= termCount - 1; L++)
                            {
                                A[ll, L] -= A[icol, L] * Dum;
                            }
                            b[ll] -= b[icol] * Dum;
                        }
                    }
                }

                for (var L = termCount - 1; L >= 0; L += -1)
                {
                    if (indxr[L] != indxc[L])
                    {
                        for (var k = 0; k <= termCount - 1; k++)
                        {
                            Dum = A[k, indxr[L]];
                            A[k, indxr[L]] = A[k, indxc[L]];
                            A[k, indxc[L]] = Dum;
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
