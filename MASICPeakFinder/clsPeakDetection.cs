using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MASICPeakFinder
{
    internal class clsPeakDetection
    {
        // Peak detection routines
        // Written by Matthew Monroe in roughly 2001 at UNC (Chapel Hill, NC)
        // Kevin Lan provided the concept of Magnitude Concavity fitting
        // Ported from LabView code to VB 6 in June 2003 at PNNL (Richland, WA)
        // Ported from VB 6 to VB.NET in October 2003
        // Switched from using the eols.dll least squares fitting routine to using a local function

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

            public double ParamResult;        // Stores the coefficient determined for the fit
        }

        //private bool mEolsDllNotFound;

        public double ComputeSlope(double[] xValuesZeroBased, double[] yValuesZeroBased, int startIndex, int endIndex)
        {
            const int POLYNOMIAL_ORDER = 1;

            double[] segmentX;
            double[] segmentY;

            double[] coefficients = null;

            if (xValuesZeroBased == null || xValuesZeroBased.Length == 0)
                return 0;
            int segmentCount = endIndex - startIndex + 1;

            segmentX = new double[segmentCount];
            segmentY = new double[segmentCount];

            // Copy the desired segment of data from xValues to segmentX and yValues to segmentY
            for (int index = startIndex; index <= endIndex; index++)
            {
                segmentX[index - startIndex] = xValuesZeroBased[index];
                segmentY[index - startIndex] = yValuesZeroBased[index];
            }

            // Compute the coefficients for the curve fit
            LeastSquaresFit(segmentX, segmentY, out coefficients, POLYNOMIAL_ORDER);

            return coefficients[1];
        }

        public List<clsPeakInfo> DetectPeaks(
            double[] xValuesZeroBased,
            double[] yValuesZeroBased,
            double intensityThresholdAbsoluteMinimum,
            int peakWidthPointsMinimum,
            int peakDetectIntensityThresholdPercentageOfMaximum = 0,
            int peakWidthInSigma = 4,
            bool useValleysForPeakWidth = true,
            bool movePeakLocationToMaxIntensity = true)
        {
            // Finds peaks in the parallel arrays xValuesZeroBased() and yValuesZeroBased()
            // intensityThreshold is the minimum absolute intensity allowable for a peak
            // peakDetectIntensityThresholdPercentageOfMaximum allows one to specify a minimum intensity as a percentage of the maximum peak intensity
            // Note that the maximum value of intensityThreshold vs. MaxValue*peakDetectIntensityThresholdPercentageOfMaximum is used as the minimum
            // For example, if intensityThreshold = 10 and peakDetectIntensityThresholdPercentageOfMaximum =  5 (indicating 5%),
            // then if the maximum of yValuesZeroBased() is 50, then the minimum intensity of identified peaks is 10, and not 2.5
            // However, if the maximum of yValuesZeroBased() is 500, then the minimum intensity of identified peaks is 50, and not 10

            // Returns the locations of the peaks in peakLocations() -- indices of the peak apexes in the source arrays
            // Returns the left edges of the peaks (in points, not actual units) in peakEdgesLeft()       -- These values could be negative if useValleysForPeakWidth = False
            // Returns the right edges of the peaks in peakEdgesRight()                                   -- These values could be larger than sourceDataCount-1 if useValleysForPeakWidth = False
            // Returns the areas of the peaks in peakAreas()

            // Note: Compute peak width using: peakWidthPoints = newPeak.RightEdge - newPeak.LeftEdge + 1

            // The function returns the number of peaks found; if none are found, returns 0

            // Uses the Magnitude-Concavity method, wherein a second order
            // polynomial is fit to the points within the window, giving a_2*x^2 + a_1*x + a_0
            // Given this, a_1 is the first derivative and a_2 is the second derivative
            // From this, the first derivative gives the index of the peak apex
            // The standard deviation (s) can be found using:
            // s = sqrt(-h(t_r) / h''(t_r))
            // where h(t_r) is the height of the peak at the peak center
            // and h''(t_r) is the height of the second derivative of the peak
            // In chromatography, the baseline peak widthInPoints = 4*sigma

            double[] firstDerivative;
            double[] secondDerivative;

            double[] xValuesForArea;
            double[] yValuesForArea;

            // Initialize the list of detected peaks
            var detectedPeaks = new List<clsPeakInfo>();

            try
            {
                int sourceDataCount = xValuesZeroBased.Length;
                if (sourceDataCount <= 0)
                    return detectedPeaks;

                // Reserve space for the first and second derivatives
                firstDerivative = new double[sourceDataCount];
                secondDerivative = new double[sourceDataCount];

                // The mid point width is the minimum width divided by 2, rounded down
                int peakHalfWidth = (int)Math.Floor(peakWidthPointsMinimum / 2.0);

                // Find the maximum intensity in the source data
                double maximumIntensity = 0;
                for (int index = 0; index < sourceDataCount; index++)
                {
                    if (yValuesZeroBased[index] > maximumIntensity)
                    {
                        maximumIntensity = yValuesZeroBased[index];
                    }
                }

                double intensityThreshold = maximumIntensity * (peakDetectIntensityThresholdPercentageOfMaximum / 100.0);
                if (intensityThreshold < intensityThresholdAbsoluteMinimum)
                {
                    intensityThreshold = intensityThresholdAbsoluteMinimum;
                }

                // Exit the function if none of the data is above the minimum threshold
                if (maximumIntensity < intensityThreshold)
                    return detectedPeaks;

                // Do the actual work
                FitSegments(xValuesZeroBased, yValuesZeroBased, sourceDataCount, peakWidthPointsMinimum, peakHalfWidth, ref firstDerivative, ref secondDerivative);

                if (peakWidthInSigma < 1)
                    peakWidthInSigma = 1;

                // Examine the First Derivative function and look for zero crossings (in the downward direction)
                // If looking for valleys, would look for zero crossings in the upward direction
                // Only significant if intensity of point is above threshold
                if (peakWidthPointsMinimum <= 0)
                    peakWidthPointsMinimum = 1;

                // We'll start looking for peaks halfway into peakWidthPointsMinimum
                int indexFirst = peakHalfWidth;
                int indexLast = sourceDataCount - 1 - peakHalfWidth;

                for (int index = indexFirst; index <= indexLast; index++)
                {
                    if (firstDerivative[index] > 0 && firstDerivative[index + 1] < 0)
                    {
                        // Possible peak
                        if (yValuesZeroBased[index] >= intensityThreshold || yValuesZeroBased[index + 1] >= intensityThreshold)
                        {
                            // Actual peak

                            var newPeak = new clsPeakInfo(index);

                            if (useValleysForPeakWidth)
                            {
                                // Determine the peak width by looking for the adjacent valleys
                                // If, while looking, we find peakWidthPointsMinimum / 2 points in a row with intensity values below intensityThreshold, then
                                // set the edge peakHalfWidth - 1 points closer to the peak maximum

                                if (index > 0)
                                {
                                    newPeak.LeftEdge = 0;
                                    int lowIntensityPointCount = 0;
                                    for (int compareIndex = index - 1; compareIndex >= 0; compareIndex--)
                                    {
                                        if (firstDerivative[compareIndex] <= 0 && firstDerivative[compareIndex + 1] >= 0)
                                        {
                                            // Found a valley; this is the left edge
                                            newPeak.LeftEdge = compareIndex + 1;
                                            break;
                                        }
                                        else if (yValuesZeroBased[compareIndex] < intensityThreshold)
                                        {
                                            lowIntensityPointCount += 1;
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
                                    int lowIntensityPointCount = 0;
                                    for (int compareIndex = index + 1; compareIndex <= sourceDataCount - 2; compareIndex++)
                                    {
                                        if (firstDerivative[compareIndex] <= 0 && firstDerivative[compareIndex + 1] >= 0)
                                        {
                                            // Found a valley; this is the right edge
                                            newPeak.RightEdge = compareIndex;
                                            break;
                                        }
                                        else if (yValuesZeroBased[compareIndex] < intensityThreshold)
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
                            else
                            {
                                // Examine the Second Derivative to determine peak Width (in points)

                                double sigma;

                                try
                                {
                                    // If secondDerivative(index)) is tiny, the following division will fail
                                    sigma = Math.Sqrt(Math.Abs(-yValuesZeroBased[index] / secondDerivative[index]));
                                }
                                catch (Exception ex)
                                {
                                    sigma = 0;
                                }

                                int widthInPoints = (int)Math.Ceiling(peakWidthInSigma * sigma);

                                if (widthInPoints > 4 * sourceDataCount)
                                {
                                    // Predicted width is over 4 times the data count
                                    // Set it to be 4 times the data count
                                    widthInPoints = sourceDataCount * 4;
                                }

                                if (widthInPoints < 2)
                                    widthInPoints = 2;

                                // If the peak width is odd, then center around index
                                // Otherwise, offset to the right of index
                                if (widthInPoints % 2 == 0)
                                {
                                    // Even number
                                    newPeak.LeftEdge = index - (int)(widthInPoints / 2.0);
                                    newPeak.RightEdge = index + (int)(widthInPoints / 2.0) - 1;
                                }
                                else
                                {
                                    // Odd number
                                    newPeak.LeftEdge = index - (int)((widthInPoints - 1) / 2.0);
                                    newPeak.RightEdge = index + (int)((widthInPoints - 1) / 2.0);
                                }
                            }

                            detectedPeaks.Add(newPeak);
                        }
                    }
                }

                // Compute the peak areas
                foreach (var peakItem in detectedPeaks)
                {
                    int thisPeakWidthInPoints = peakItem.RightEdge - peakItem.LeftEdge + 1;

                    if (thisPeakWidthInPoints > 0)
                    {
                        if (thisPeakWidthInPoints == 1)
                        {
                            // I don't think this can happen
                            // Just in case, we'll set the area equal to the peak intensity
                            peakItem.PeakArea = yValuesZeroBased[peakItem.PeakLocation];
                        }
                        else
                        {
                            xValuesForArea = new double[thisPeakWidthInPoints];
                            yValuesForArea = new double[thisPeakWidthInPoints];

                            int thisPeakStartIndex = peakItem.LeftEdge;
                            int thisPeakEndIndex = peakItem.RightEdge;

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

                            for (int areaValsCopyIndex = thisPeakStartIndex; areaValsCopyIndex <= thisPeakEndIndex; areaValsCopyIndex++)
                            {
                                xValuesForArea[areaValsCopyIndex - thisPeakStartIndex] = xValuesZeroBased[areaValsCopyIndex];
                                yValuesForArea[areaValsCopyIndex - thisPeakStartIndex] = yValuesZeroBased[areaValsCopyIndex];
                            }

                            peakItem.PeakArea = FindArea(xValuesForArea, yValuesForArea, thisPeakWidthInPoints);
                        }
                    }
                    else
                    {
                        // 0-width peak; this shouldn't happen
                        Console.WriteLine("Warning: 0-width peak; this shouldn't happen (clsPeakDetection->DetectPeaks)");
                        peakItem.PeakArea = 0;
                    }
                }

                if (movePeakLocationToMaxIntensity)
                {
                    foreach (var peakItem in detectedPeaks)
                    {
                        // The peak finder often determines the peak center to be a few points away from the peak apex -- check for this
                        // Define the maximum allowed peak apex shift to be 33% of peakWidthPointsMinimum
                        int dataIndexCheckStart = peakItem.PeakLocation - (int)Math.Floor(peakWidthPointsMinimum / 3.0);
                        if (dataIndexCheckStart < 0)
                            dataIndexCheckStart = 0;

                        int dataIndexCheckEnd = peakItem.PeakLocation + (int)Math.Floor(peakWidthPointsMinimum / 3.0);
                        if (dataIndexCheckEnd > sourceDataCount - 1)
                            dataIndexCheckEnd = sourceDataCount - 1;

                        maximumIntensity = yValuesZeroBased[peakItem.PeakLocation];
                        for (int dataIndexCheck = dataIndexCheckStart; dataIndexCheck <= dataIndexCheckEnd; dataIndexCheck++)
                        {
                            if (yValuesZeroBased[dataIndexCheck] > maximumIntensity)
                            {
                                peakItem.PeakLocation = dataIndexCheck;
                                maximumIntensity = yValuesZeroBased[dataIndexCheck];
                            }
                        }

                        if (peakItem.PeakLocation < peakItem.LeftEdge)
                            peakItem.LeftEdge = peakItem.PeakLocation;
                        if (peakItem.PeakLocation > peakItem.RightEdge)
                            peakItem.RightEdge = peakItem.PeakLocation;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Error in clsPeakDetection->DetectPeaks (or in a child function)" + Environment.NewLine + ex.Message);
            }

            return detectedPeaks;
        }

        private double FindArea(IList<double> xValues, IList<double> yValues, int arrayCount)
        {
            // yValues() should be 0-based

            // Finds the area under the curve, using trapezoidal integration

            double area = 0;
            for (int index = 0; index <= arrayCount - 2; index++)
            {
                // Area of a trapezoid (turned on its side) is:
                // 0.5 * d * (h1 + h2)
                // where d is the distance between two points, and h1 and h2 are the intensities
                // at the 2 points

                area += 0.5 * Math.Abs(xValues[index + 1] - xValues[index]) * (yValues[index] + yValues[index + 1]);
            }
            return area;
        }

        private void FitSegments(IList<double> xValues, IList<double> yValues, int sourceDataCount, int peakWidthPointsMinimum, int peakWidthMidPoint, ref double[] firstDerivative, ref double[] secondDerivative)
        {
            // xValues() and yValues() are zero-based arrays

            const int POLYNOMIAL_ORDER = 2;

            double[] segmentX;
            double[] segmentY;

            double[] coefficients = null;

            // if (POLYNOMIAL_ORDER < 2) POLYNOMIAL_ORDER = 2;
            // if (POLYNOMIAL_ORDER > 9) POLYNOMIAL_ORDER = 9;

            segmentX = new double[peakWidthPointsMinimum];
            segmentY = new double[peakWidthPointsMinimum];

            for (int startIndex = 0; startIndex <= sourceDataCount - peakWidthPointsMinimum - 1; startIndex++)
            {

                // Copy the desired segment of data from xValues to segmentX and yValues to segmentY
                for (int subIndex = startIndex; subIndex <= startIndex + peakWidthPointsMinimum - 1; subIndex++)
                {
                    segmentX[subIndex - startIndex] = xValues[subIndex];
                    segmentY[subIndex - startIndex] = yValues[subIndex];
                }

                // Compute the coefficients for the curve fit
                LeastSquaresFit(segmentX, segmentY, out coefficients, POLYNOMIAL_ORDER);

                // Compute the firstDerivative at the midpoint
                int midPointIndex = startIndex + peakWidthMidPoint;
                firstDerivative[midPointIndex] = 2 * coefficients[2] * xValues[midPointIndex] + coefficients[1];
                secondDerivative[midPointIndex] = 2 * coefficients[2];
            }
        }

        #region "LinearLeastSquaresFitting"

        private bool LeastSquaresFit(IList<double> xValues, IList<double> yValues, out double[] coefficients, int polynomialOrder)
        {
            // Code from article "Fit for Purpose" written by Steven Abbot
            // and published in the February 2003 issue of Hardcore Visual Basic.
            // Code excerpted from the VB6 program FitIt
            // URL: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnhcvb03/html/hcvb03b1.asp

            udtLeastSquaresFitEquationTermType[] equationTerms;
            // int term;
            // bool success;

            equationTerms = new udtLeastSquaresFitEquationTermType[polynomialOrder + 1];
            coefficients = new double[polynomialOrder + 1];

            if (xValues.Count < polynomialOrder + 1)
            {
                // Not enough data to fit a curve
                return false;
            }

            // Define equation for "ax^0 + bx^1 + cx^2", which is the same as "a + bx + cx^2"
            for (int term = 0; term <= polynomialOrder; term++)
            {
                // array of struct: Direct assignment, indexing the array every time, works.
                equationTerms[term].Coefficient = 1;                        // a, b, c in the above equation
                equationTerms[term].Func = eTermFunctionConstants.X;        // X
                equationTerms[term].Power = term;                           // Power that X is raised to
                equationTerms[term].Inverse = false;                        // Whether or not to inverse the entire term

                equationTerms[term].ParamResult = 0;
            }

            bool success = LLSqFit(xValues, yValues, ref equationTerms);
            for (int term = 0; term <= polynomialOrder; term++)
                coefficients[term] = equationTerms[term].ParamResult;

            return success;
        }

        private bool LLSqFit(IList<double> xValues, IList<double> yValues, ref udtLeastSquaresFitEquationTermType[] equationTerms)
        {
            // Linear Least Squares Fit

            int i, j, k, L, m;
            double ym;

            double[] Beta;
            double[,] CoVar;
            double[] PFuncVal;

            Beta = new double[xValues.Count];
            CoVar = new double[equationTerms.Length, equationTerms.Length];
            PFuncVal = new double[equationTerms.Length];

            for (i = 0; i < xValues.Count; i++)
            {
                GetLVals(xValues[i], ref equationTerms, ref PFuncVal);
                ym = yValues[i];
                for (L = 0; L < equationTerms.Length; L++)
                {
                    for (m = 0; m <= L; m++)
                        CoVar[L, m] += PFuncVal[L] * PFuncVal[m];
                    Beta[L] += ym * PFuncVal[L];
                }
            }

            for (j = 1; j < equationTerms.Length; j++)
            {
                for (k = 0; k < j; k++)
                    CoVar[k, j] = CoVar[j, k];
            }

            if (GaussJordan(ref CoVar, ref equationTerms, ref Beta))
            {
                for (L = 0; L < equationTerms.Length; L++)
                    equationTerms[L].ParamResult = Beta[L];

                return true;
            }
            else
            {
                // Error fitting; clear coefficients
                for (L = 0; L < equationTerms.Length; L++)
                    equationTerms[L].ParamResult = 0;

                return false;
            }
        }

        private void GetLVals(double X, ref udtLeastSquaresFitEquationTermType[] equationTerms, ref double[] PFuncVal)
        {
            // Get values for Linear Least Squares
            // equationTerms() is a 0-based array defining the form of each term

            int i;
            var v = default(double);

            // Use the following for a 2nd order polynomial fit
            // 'Define the formula via PFuncVal
            // 'In this case NTerms=3 and y=a+bx+cx^2
            // PFuncVal(1) = 1
            // PFuncVal(2) = X
            // PFuncVal(3) = X ^ 2

            // f = "1,X,Log(X),Log10(X),Exp(X),Sin(X),Cos(X),Tan(X),ATAN(X)"
            for (i = 0; i < equationTerms.Length; i++)
            {
                // Struct: No assignment is performed, we don't need to copy the end value back.
                var term = equationTerms[i];
                switch (term.Func)
                {
                    case eTermFunctionConstants.One:
                        v = 1;
                        break;
                    case eTermFunctionConstants.X:
                        v = Math.Pow(X, term.Power);
                        break;
                    case eTermFunctionConstants.LogX:
                        if (term.Coefficient * X <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            v = Math.Pow(Math.Log(term.Coefficient * X), term.Power);
                        }

                        break;
                    case eTermFunctionConstants.Log10X:
                        if (term.Coefficient * X <= 0)
                        {
                            v = 0;
                        }
                        else
                        {
                            v = Math.Pow(Math.Log10(term.Coefficient * X), term.Power);
                        }

                        break;
                    case eTermFunctionConstants.ExpX:
                        v = Math.Pow(Math.Exp(term.Coefficient * X), term.Power);
                        break;
                    case eTermFunctionConstants.SinX:
                        v = Math.Pow(Math.Sin(term.Coefficient * X), term.Power);
                        break;
                    case eTermFunctionConstants.CosX:
                        v = Math.Pow(Math.Cos(term.Coefficient * X), term.Power);
                        break;
                    case eTermFunctionConstants.TanX:
                        v = Math.Pow(Math.Tan(term.Coefficient * X), term.Power);
                        break;
                    case eTermFunctionConstants.ATanX:
                        v = Math.Pow(Math.Atan(term.Coefficient * X), term.Power);
                        break;
                }

                if (term.Inverse)
                {
                    if (Math.Abs(v) < double.Epsilon)
                    {
                        PFuncVal[i] = 0;
                    }
                    else // NOT V...
                    {
                        PFuncVal[i] = 1 / v;
                    }
                }
                else // INV(I) = FALSE
                {
                    PFuncVal[i] = v;
                }
            }
        }

        private bool GaussJordan(ref double[,] A, ref udtLeastSquaresFitEquationTermType[] equationTerms, ref double[] b)
        {
            // GaussJordan elimination for LLSq and LM solving
            // Returns True if success, False if an error

            int[] indxc, indxr, ipiv;
            int n;

            n = equationTerms.Length;

            indxc = new int[n];
            indxr = new int[n];
            ipiv = new int[n];

            int i, icol = 0, irow = 0, j, k, L, ll;
            double Big, Dum, PivInv;

            try
            {
                for (i = 0; i < n; i++)
                {
                    Big = 0;
                    for (j = 0; j < n; j++)
                    {
                        if (ipiv[j] != 1)
                        {
                            for (k = 0; k < n; k++)
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
                        for (L = 0; L < n; L++)
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

                    PivInv = 1 / A[icol, icol];
                    A[icol, icol] = 1;
                    for (L = 0; L < n; L++)
                        A[icol, L] *= PivInv;

                    b[icol] *= PivInv;
                    for (ll = 0; ll < n; ll++)
                    {
                        if (ll != icol)
                        {
                            Dum = A[ll, icol];
                            A[ll, icol] = 0;
                            for (L = 0; L < n; L++)
                                A[ll, L] -= A[icol, L] * Dum;
                            b[ll] -= b[icol] * Dum;
                        }
                    }
                }

                for (L = n - 1; L >= 0; L--)
                {
                    if (indxr[L] != indxc[L])
                    {
                        for (k = 0; k < n; k++)
                        {
                            Dum = A[k, indxr[L]];
                            A[k, indxr[L]] = A[k, indxc[L]];
                            A[k, indxc[L]] = Dum;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region "EolsDll_LeastSquaresFitting"

        //private const int EODOUBLE = 1;
        //private const int EOFLOAT = 2;
        //private const int EOLONG = 3;
        //private const int EOSHORT = 4;

        //[DllImport("eols.dll")]
        //private static extern int EoLeastSquaresFit(
        //    double[] XData,
        //    double[] YData,
        //    int iNDataPoints,
        //    int iNCoefficients,
        //    int fnUserEquation,
        //    double[] Coef,
        //    int iDataType,
        //    int iSaveStateFlag,
        //    int handle);

        //// The following is only needed when using eolsrt.dll; used for real-time least squares fitting, utilizing data buffering
        //[DllImport("eolsrt.dll")]
        //private static extern void EoLeastSquaresFitClose(long handle);

        //private void LeastSquaresFitEolsDll(double[] xValues, double[] yValues, ref double[] coefficients, int polynomialOrder)
        //{
        //    // Uses the EoLeastSquaresFit function in the eols.dll file to compute a least squares fit on the portion of the data between indexStart and indexEnd
        //    // polynomialOrder should be between 2 and 9
        //    // xValues[] should range from 0 to dataCount-1

        //    int returnCode;

        //    try
        //    {
        //        coefficients = new double[polynomialOrder];

        //        // Note: For a 2nd order equation, coefficients(0), (1), and (2) correspond to C0, C1, and C2 in the equation:
        //        //       y = C0 +  C1 x  +  C2 x^2
        //        returnCode = EoLeastSquaresFit(xValues, yValues, xValues.Length, polynomialOrder + 1, 0, coefficients, EODOUBLE, 0, 0);
        //        Debug.Assert(returnCode = 1, "Call to EoLeastSquaresFit failed (clsPeakDetection->LeastSquaresFitEolsDll)");
        //    }
        //    catch (Exception ex)
        //    {
        //        if (Err.Number == 52 || Err.Number == 53)
        //        {
        //            if (!mEolsDllNotFound)
        //            {
        //                mEolsDllNotFound = true;
        //                // Inform the user that we couldn't find eols.dll
        //                MessageBox.Show("Could not find the eols.dll file; please assure it is located in the application folder.");
        //            }
        //        }
        //        else
        //        {
        //            Debug.Assert(false, "Error in clsPeakDetection->LeastSquaresFitEolsDll: " + ex.Message);
        //        }
        //    }
        //}

        #endregion
    }
}