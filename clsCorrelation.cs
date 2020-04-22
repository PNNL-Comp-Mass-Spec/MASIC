using System;
using System.Collections.Generic;
using System.Linq;
using PRISM;

namespace MASIC
{
    /// <summary>
    /// This class can be used to correlate two lists of numbers (typically mass spectra) to determine their similarity
    /// The lists of numbers must have the same number of values
    /// Use the BinData function to bin a list of X,Y pairs into bins ranging from .BinStartX to .BinEndX
    ///
    /// These functions were originally written in VB6 and required the use of a C DLL
    /// They have since been ported to VB.NET
    /// </summary>
    public class clsCorrelation : EventNotifier
    {
        #region "Classwide variables"

        /// <summary>
        /// Coefficients used by GammaLn
        /// </summary>
        private readonly double[] mCoefficients;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public clsCorrelation()
            : this(GetDefaultBinningOptions())
        {
        }

        public clsCorrelation(clsBinningOptions binningOptions)
        {
            mBinningOptions = binningOptions;
            NoiseThresholdIntensity = 0;

            mCoefficients = new double[] { 76.180091729471457, -86.505320329416776,
                                           24.014098240830911, -1.231739572450155,
                                           0.001208650973866179, -0.000005395239384953 };
        }

        #region "Constants and Structures"
        private const int MIN_NON_ZERO_ION_COUNT = 5;

        public enum cmCorrelationMethodConstants
        {
            Pearson = 0,
            Spearman = 1,
            Kendall = 2
        }

        #endregion

        #region "Local Member Variables"
        private clsBinningOptions mBinningOptions;
        #endregion

        #region "Property Interface Functions"

        public float BinStartX
        {
            get => mBinningOptions.StartX;
            set => mBinningOptions.StartX = value;
        }

        public float BinEndX
        {
            get => mBinningOptions.EndX;
            set => mBinningOptions.EndX = value;
        }

        public float BinSize
        {
            get => mBinningOptions.BinSize;
            set
            {
                if (value <= 0)
                    value = 1;
                mBinningOptions.BinSize = value;
            }
        }

        public float BinnedDataIntensityPrecisionPercent
        {
            get => mBinningOptions.IntensityPrecisionPercent;
            set
            {
                if (value < 0 || value > 100)
                    value = 1;
                mBinningOptions.IntensityPrecisionPercent = value;
            }
        }

        public float NoiseThresholdIntensity { get; set; }

        public bool NormalizeBinnedData
        {
            get => mBinningOptions.Normalize;
            set => mBinningOptions.Normalize = value;
        }

        public bool SumAllIntensitiesForBin
        {
            get => mBinningOptions.SumAllIntensitiesForBin;
            set => mBinningOptions.SumAllIntensitiesForBin = value;
        }

        public int MaximumBinCount
        {
            get => mBinningOptions.MaximumBinCount;
            set
            {
                if (value < 2)
                    value = 10;
                if (value > 1000000)
                    value = 1000000;
                mBinningOptions.MaximumBinCount = value;
            }
        }
        #endregion

        private double BetaCF(double a, double b, double x)
        {
            const int MAX_ITERATIONS = 100;
            double EPS = 0.0000003;
            const double FPMIN = 1.0E-30;

            int m, m2;
            double aa, c, d, del, h, qab, qam, qap;

            qab = a + b;
            qap = a + 1.0;
            qam = a - 1.0;
            c = 1.0;
            d = 1.0 - qab * x / qap;
            if (Math.Abs(d) < FPMIN)
                d = FPMIN;
            d = 1.0 / d;
            h = d;
            for (m = 1; m <= MAX_ITERATIONS; m++)
            {
                m2 = 2 * m;
                aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < FPMIN)
                    d = FPMIN;
                c = 1.0 + aa / c;
                if (Math.Abs(c) < FPMIN)
                    c = FPMIN;
                d = 1.0 / d;
                h *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < FPMIN)
                    d = FPMIN;
                c = 1.0 + aa / c;
                if (Math.Abs(c) < FPMIN)
                    c = FPMIN;
                d = 1.0 / d;
                del = d * c;
                h *= del;
                if (Math.Abs(del - 1.0) < EPS)
                    break;
            }

            if (m > MAX_ITERATIONS)
            {
                throw new Exception("a or b too big, or MAX_ITERATIONS too small in clsCorrelation->BetaCF");
            }
            else
            {
                return h;
            }
        }

        private double BetaI(double a, double b, double x)
        {
            double bt;

            if (x < 0.0 || x > 1.0)
            {
                throw new Exception("Bad x in routine clsCorrelation->BetaI; should be between 0 and 1");
            }
            else
            {
                if (Math.Abs(x) < double.Epsilon || Math.Abs(x - 1.0) < double.Epsilon)
                {
                    bt = 0.0;
                }
                else
                {
                    bt = Math.Exp(GammaLn(a + b) - GammaLn(a) - GammaLn(b) + a * Math.Log(x) + b * Math.Log(1.0 - x));
                }

                if (x < (a + 1.0) / (a + b + 2.0))
                {
                    return bt * BetaCF(a, b, x) / a;
                }
                else
                {
                    return 1.0 - bt * BetaCF(b, a, 1.0 - x) / b;
                }
            }
        }

        /// <summary>
        /// Bins the data in xData() according to startBinXValue and binSize
        /// </summary>
        /// <param name="xData"></param>
        /// <param name="yData"></param>
        /// <param name="binnedYData">Binned y data (the calling class must instantiate this)</param>
        /// <param name="binnedOffsetYData">Binned y data, where the StartX value is offset by 50% of the bin size vs. binnedYData</param>
        /// <returns>True if successful, otherwise false</returns>
        public bool BinData(
            List<float> xData,
            List<float> yData,
            List<float> binnedYData,
            List<float> binnedOffsetYData)
        {
            int dataCount;

            float bin2Offset;

            binnedYData.Clear();
            binnedOffsetYData.Clear();

            try
            {
                dataCount = xData.Count;
                if (dataCount <= 0)
                {
                    return false;
                }

                if (mBinningOptions.BinSize <= 0)
                {
                    mBinningOptions.BinSize = 1;
                }

                if (mBinningOptions.StartX >= mBinningOptions.EndX)
                {
                    mBinningOptions.EndX = mBinningOptions.StartX + mBinningOptions.BinSize * 10;
                }

                int binCount = (int)Math.Round((mBinningOptions.EndX - mBinningOptions.StartX) / mBinningOptions.BinSize - 1);
                if (binCount < 1)
                    binCount = 1;

                if (binCount > mBinningOptions.MaximumBinCount)
                {
                    mBinningOptions.BinSize = (mBinningOptions.EndX - mBinningOptions.StartX) / mBinningOptions.MaximumBinCount;
                    binCount = (int)Math.Round((mBinningOptions.EndX - mBinningOptions.StartX) / mBinningOptions.BinSize - 1);
                }

                bin2Offset = mBinningOptions.BinSize / 2;

                // Initialize the bins
                for (int i = 1; i <= binCount; i++)
                {
                    binnedYData.Add(0);
                    binnedOffsetYData.Add(0);
                }

                // Fill binnedYData()
                BinDataWork(xData, yData, dataCount, binnedYData, binCount, mBinningOptions, 0);

                // Fill binnedOffsetYData(), using a StartX of startBinXValue + bin2Offset
                BinDataWork(xData, yData, dataCount, binnedOffsetYData, binCount, mBinningOptions, bin2Offset);
            }
            catch (Exception ex)
            {
                OnErrorEvent("BinData: " + ex.Message, ex);
                return false;
            }

            return true;
        }

        private void BinDataWork(IList<float> xData, IList<float> yData, int dataCount,
                                 IList<float> binnedYData, int binCount, clsBinningOptions binningOptions, float offset)
        {
            int index;
            int binNumber;

            float maximumIntensity;
            float intensityQuantizationValue;

            try
            {
                maximumIntensity = float.MinValue;
                for (index = 0; index <= dataCount - 1; index++)
                {
                    if (yData[index] >= NoiseThresholdIntensity)
                    {
                        binNumber = ValueToBinNumber(xData[index], binningOptions.StartX + offset, binningOptions.BinSize);
                        if (binNumber >= 0 && binNumber < binCount)
                        {
                            if (binningOptions.SumAllIntensitiesForBin)
                            {
                                // Add this ion's intensity to the bin intensity
                                binnedYData[binNumber] += yData[index];
                            }
                            // Only change the bin's intensity if this ion's intensity is larger than the bin's intensity
                            // If it is, then set the bin intensity to equal the ion's intensity
                            else if (yData[index] > binnedYData[binNumber])
                            {
                                binnedYData[binNumber] = yData[index];
                            }

                            if (binnedYData[binNumber] > maximumIntensity)
                            {
                                maximumIntensity = binnedYData[binNumber];
                            }
                        }
                        else
                        {
                            // Bin is Out of range; ignore the value
                        }
                    }
                }

                if (!(maximumIntensity > float.MinValue))
                    maximumIntensity = 0;

                if (binningOptions.IntensityPrecisionPercent > 0)
                {
                    // Quantize the intensities to .IntensityPrecisionPercent of maximumIntensity
                    intensityQuantizationValue = binningOptions.IntensityPrecisionPercent / 100 * maximumIntensity;
                    if (intensityQuantizationValue <= 0)
                        intensityQuantizationValue = 1;
                    if (intensityQuantizationValue > 1)
                        intensityQuantizationValue = (float)(Math.Round(intensityQuantizationValue, 0));

                    for (index = 0; index <= binCount - 1; index++)
                    {
                        if (Math.Abs(binnedYData[index]) > float.Epsilon)
                        {
                            binnedYData[index] = (float)(Math.Round(binnedYData[index] / intensityQuantizationValue, 0)) * intensityQuantizationValue;
                        }
                    }
                }

                if (binningOptions.Normalize && maximumIntensity > 0)
                {
                    for (index = 0; index <= binCount - 1; index++)
                    {
                        if (Math.Abs(binnedYData[index]) > float.Epsilon)
                        {
                            binnedYData[index] /= maximumIntensity * 100;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("BinDataWork: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Finds the correlation value between the two lists of data
        /// The lists must have the same number of data points
        /// If they have fewer than MIN_NON_ZERO_ION_COUNT non-zero values, the correlation value returned will be 0
        /// </summary>
        /// <param name="dataList1"></param>
        /// <param name="dataList2"></param>
        /// <param name="eCorrelationMethod"></param>
        /// <returns>Correlation value (0 to 1), or -1 if an error</returns>
        /// <remarks>If necessary, use the BinData function before calling this function to bin the data</remarks>
        public float Correlate(
            IReadOnlyCollection<float> dataList1,
            IReadOnlyCollection<float> dataList2,
            cmCorrelationMethodConstants eCorrelationMethod)
        {
            int index;
            int dataCount;
            int nonZeroDataCount;

            float RValue, ProbOfSignificance, FishersZ;
            float DiffInRanks, ZD, RS, ProbRS;
            float KendallsTau, Z;

            // '  Dim dataList1Test() As Single = New Single() {1, 2, 2, 8, 9, 0, 0, 3, 9, 0, 5, 6}
            // '  Dim dataList2Test() As Single = New Single() {2, 3, 7, 7, 11, 1, 3, 2, 13, 0, 4, 10}

            try
            {
                RValue = 0;
                RS = 0;
                KendallsTau = 0;

                dataCount = dataList1.Count;
                if (dataList2.Count != dataList1.Count)
                {
                    return -1;
                }

                // Determine the number of non-zero data points in the two spectra
                nonZeroDataCount = 0;
                for (index = 0; index <= dataCount - 1; index++)
                {
                    if (dataList1.ElementAtOrDefault(index) > 0)
                        nonZeroDataCount += 1;
                }

                if (nonZeroDataCount < MIN_NON_ZERO_ION_COUNT)
                    return 0;

                nonZeroDataCount = 0;
                for (index = 0; index <= dataCount - 1; index++)
                {
                    if (dataList2.ElementAtOrDefault(index) > 0)
                        nonZeroDataCount += 1;
                }

                if (nonZeroDataCount < MIN_NON_ZERO_ION_COUNT)
                    return 0;

                switch (eCorrelationMethod)
                {
                    case cmCorrelationMethodConstants.Pearson:
                        CorrelatePearson(dataList1, dataList2, out RValue, out ProbOfSignificance, out FishersZ);
                        return RValue;
                    case cmCorrelationMethodConstants.Spearman:
                        CorrelateSpearman(dataList1, dataList2, out DiffInRanks, out ZD, out ProbOfSignificance, out RS, out ProbRS);
                        return RS;
                    case cmCorrelationMethodConstants.Kendall:
                        CorrelateKendall(dataList1, dataList2, out KendallsTau, out Z, out ProbOfSignificance);
                        return KendallsTau;
                    default:
                        return -1;
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Correlate: " + ex.Message, ex);
                return -1;
            }
        }

        private void CorrelatePearson(
            IReadOnlyCollection<float> dataList1, IReadOnlyCollection<float> dataList2,
            out float RValue,
            out float ProbOfSignificance, out float FishersZ)
        {
            // Performs a Pearson correlation (aka linear correlation) of the two lists
            // The lists must have the same number of data points in each and should be 0-based arrays
            //
            // Code from Numerical Recipes in C

            // TINY is used to "regularize" the unusual case of complete correlation
            double TINY = 1.0E-20;

            // Given two arrays x[1..n] and y[1..n], this routine computes their correlation coefficient
            // r (returned as r), the significance level at which the null hypothesis of zero correlation is
            // disproved (prob whose small value indicates a significant correlation), and Fisher's z (returned
            // as z), whose value can be used in further statistical tests as described above.

            int n;
            int j;
            double yt, xt, t, df;
            double syy = 0.0;
            double sxy = 0.0;
            double sxx = 0.0;
            double ay = 0.0;
            double ax = 0.0;

            RValue = 0;
            ProbOfSignificance = 0;
            FishersZ = 0;

            n = dataList1.Count;
            if (n != dataList2.Count)
            {
                throw new Exception("dataList1 and dataList2 must be lists of the same length");
            }

            if (n <= 0)
                return;

            // Find the means
            for (j = 0; j <= n - 1; j++)
            {
                ax += dataList1.ElementAtOrDefault(j);
                ay += dataList2.ElementAtOrDefault(j);
            }

            ax /= n;
            ay /= n;

            // Compute the correlation coefficient
            for (j = 0; j <= n - 1; j++)
            {
                xt = dataList1.ElementAtOrDefault(j) - ax;
                yt = dataList2.ElementAtOrDefault(j) - ay;
                sxx += xt * xt;
                syy += yt * yt;
                sxy += xt * yt;
            }

            RValue = (float)(sxy / (Math.Sqrt(sxx * syy) + TINY));

            // Fisher's z transformation
            FishersZ = (float)(0.5 * Math.Log((1.0 + RValue + TINY) / (1.0 - RValue + TINY)));
            df = n - 2;

            t = RValue * Math.Sqrt(df / ((1.0 - RValue + TINY) * (1.0 + RValue + TINY)));

            // Student's t probability
            ProbOfSignificance = (float)(BetaI(0.5 * df, 0.5, df / (df + t * t)));
        }

        private void CorrelateKendall(
            IReadOnlyCollection<float> dataList1,
            IReadOnlyCollection<float> dataList2,
            out float KendallsTau,
            out float Z,
            out float ProbOfSignificance)
        {
            // Performs a Kendall correlation (aka linear correlation) of the two lists
            // The lists must have the same number of data points in each and should be 0-based arrays
            //
            // Code from Numerical Recipes in C

            // Given data arrays data1[1..n] and data2[1..n], this program returns Kendall's tau as tau,
            // its number of standard deviations from zero as z, and its two-sided significance level as prob.
            // Small values of prob indicate a significant correlation (tau positive) or anti correlation (tau
            // negative).

            int n;
            long n2 = 0;
            long n1 = 0;
            int k, j;
            int intIS = 0;

            double svar, aa, a2, a1;

            KendallsTau = 0;
            Z = 0;
            ProbOfSignificance = 0;

            n = dataList1.Count;
            if (n != dataList2.Count)
            {
                throw new Exception("dataList1 and dataList2 must be lists of the same length");
            }

            if (n <= 0)
                return;

            for (j = 0; j <= n - 2; j++)
            {
                for (k = j + 1; k <= n - 1; k++)
                {
                    a1 = dataList1.ElementAtOrDefault(j) - dataList1.ElementAtOrDefault(k);
                    a2 = dataList2.ElementAtOrDefault(j) - dataList2.ElementAtOrDefault(k);
                    aa = a1 * a2;
                    if (Math.Abs(aa) > double.Epsilon)
                    {
                        n1 += 1;
                        n2 += 1;
                        if (aa > 0)
                        {
                            intIS += 1;
                        }
                        else
                        {
                            intIS -= 1;
                        }
                    }
                    else
                    {
                        if (Math.Abs(a1) > double.Epsilon)
                            n1 += 1;
                        if (Math.Abs(a2) > double.Epsilon)
                            n2 += 1;
                    }
                }
            }

            KendallsTau = (float)(intIS / (Math.Sqrt(n1) * Math.Sqrt(n2)));

            svar = (4.0 * n + 10.0) / (9.0 * n * (n - 1.0));
            Z = (float)(KendallsTau / Math.Sqrt(svar));
            ProbOfSignificance = (float)(ErfCC(Math.Abs(Z) / 1.4142136));
        }

        private void CorrelateSpearman(
            IReadOnlyCollection<float> dataList1,
            IReadOnlyCollection<float> dataList2,
            out float DiffInRanks,
            out float ZD,
            out float ProbOfSignificance,
            out float RS,
            out float ProbRS)
        {
            // Performs a Spearman correlation of the two lists
            // The lists must have the same number of data points in each and should be 0-based arrays
            //
            // Code from Numerical Recipes in C

            // Note: data1 and data2 are re-ordered by this function; thus, they are passed ByVal

            // Given two data arrays, data1[0..n-1] and data2[0..n-1], this routine returns their sum-squared
            // difference of ranks as D, the number of standard deviations by which D deviates from its null hypothesis
            // expected value as zd, the two-sided significance level of this deviation as probd,
            // Spearman's rank correlation rs as rs, and the two-sided significance level of its deviation from
            // zero as probrs. The external routine CRank is used.  A small value of either probd or probrs indicates
            // a significant correlation (rs positive) or anti correlation (rs negative).

            int n;
            int j;

            float sg, sf;
            double vard, t, fac, en3n, en, df, AvgD;
            double DiffInRanksWork;

            DiffInRanks = 0;
            ZD = 0;
            ProbOfSignificance = 0;
            RS = 0;
            ProbRS = 0;

            n = dataList1.Count;
            if (n != dataList2.Count)
            {
                throw new Exception("dataList1a and dataList2a must be lists of the same length");
            }

            if (n <= 0)
                return;

            // Populate arrays so that we can sort the data
            var data1 = dataList1.ToArray();
            var data2 = dataList2.ToArray();

            // Sort data1, sorting data2 parallel to it
            Array.Sort(data1, data2);
            CRank(n, data1, out sf);

            // Sort data2, sorting data1 parallel to it
            Array.Sort(data2, data1);
            CRank(n, data2, out sg);

            DiffInRanksWork = 0.0;
            for (j = 0; j <= n - 1; j++)
                DiffInRanksWork += SquareNum(data1[j] - data2[j]);

            DiffInRanks = (float)(DiffInRanksWork);

            en = n;

            en3n = en * en * en - en;
            AvgD = en3n / 6.0 - (sf + sg) / 12.0;
            fac = (1.0 - sf / en3n) * (1.0 - sg / en3n);
            vard = (en - 1.0) * en * en * SquareNum(en + 1.0) / 36.0 * fac;
            ZD = (float)((DiffInRanks - AvgD) / Math.Sqrt(vard));

            ProbOfSignificance = (float)(ErfCC(Math.Abs(ZD) / 1.4142136));
            RS = (float)((1.0 - 6.0 / en3n * (DiffInRanks + (sf + sg) / 12.0)) / Math.Sqrt(fac));

            fac = (RS + 1.0) * (1.0 - RS);

            if (fac > 0.0)
            {
                t = RS * Math.Sqrt((en - 2.0) / fac);
                df = en - 2.0;
                ProbRS = (float)(BetaI(0.5 * df, 0.5, df / (df + t * t)));
            }
            else
            {
                ProbRS = 0.0F;
            }
        }

        private void CRank(int n, IList<float> w, out float s)
        {
            // Given a zero-based sorted array w(0..n-1), replaces the elements by their rank (1 .. n), including mid-ranking of ties,
            // and returns as s the sum of f^3 - f, where f is the number of elements in each tie.

            int j;
            int ji, jt;
            float t, rank;

            s = 0;
            j = 0;
            while (j < n - 1)
            {
                if (Math.Abs(w[j + 1] - w[j]) > float.Epsilon)
                {
                    w[j] = j + 1;            // Rank = j + 1
                    j += 1;
                }
                else
                {
                    jt = j + 1;
                    while (jt < n && Math.Abs(w[jt] - w[j]) < float.Epsilon)
                        jt += 1;

                    rank = 0.5F * (j + jt - 1) + 1;

                    for (ji = j; ji <= jt - 1; ji++)
                        w[ji] = rank;

                    t = jt - j;
                    s += t * t * t - t;          // t^3 - t
                    j = jt;
                }
            }

            if (j == n - 1)
            {
                w[n - 1] = n;
            }
        }

        private double ErfCC(double x)
        {
            double t, z, ans;

            z = Math.Abs(x);
            t = 1.0 / (1.0 + 0.5 * z);

            ans = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                               t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                               t * (-0.82215223 + t * 0.17087277)))))))));

            if (x >= 0.0)
            {
                return ans;
            }
            else
            {
                return 2.0 - ans;
            }
        }

        /// <summary>
        /// Computes the natural logarithm of the Gamma Function
        /// </summary>
        /// <param name="xx"></param>
        /// <returns></returns>
        private double GammaLn(double xx)
        {
            double x, y, tmp, ser;
            int j;

            x = xx;
            y = x;

            tmp = x + 5.5;
            tmp -= (x + 0.5) * Math.Log(tmp);
            ser = 1.0000000001900149;

            for (j = 0; j <= 5; j++)
            {
                y += 1;
                ser += mCoefficients[j] / y;
            }

            return -tmp + Math.Log(2.5066282746310007 * ser / x);
        }

        public static clsBinningOptions GetDefaultBinningOptions()
        {
            var binningOptions = new clsBinningOptions();

            binningOptions.StartX = 50;
            binningOptions.EndX = 2000;
            binningOptions.BinSize = 1;
            binningOptions.IntensityPrecisionPercent = 1;
            binningOptions.Normalize = false;
            binningOptions.SumAllIntensitiesForBin = true;                     // Sum all of the intensities for binned ions of the same bin together
            binningOptions.MaximumBinCount = 100000;

            return binningOptions;
        }

        [Obsolete("Use GetDefaultBinningOptions, which returns an instance of clsBinningOptions")]
        public static void InitializeBinningOptions(out clsBinningOptions binningOptions)
        {
            binningOptions = GetDefaultBinningOptions();
        }

        public void SetBinningOptions(clsBinningOptions binningOptions)
        {
            mBinningOptions = binningOptions;
        }

        private double SquareNum(double value)
        {
            if (Math.Abs(value) < double.Epsilon)
            {
                return 0;
            }
            else
            {
                return value * value;
            }
        }

        private int ValueToBinNumber(float thisValue, float startValue, float histogramBinSize)
        {
            // First subtract StartValue from ThisValue
            // For example, if StartValue is 500 and ThisValue is 500.28, then WorkingValue = 0.28
            // Or, if StartValue is 500 and ThisValue is 530.83, then WorkingValue = 30.83
            float workingValue = thisValue - startValue;

            // Now, dividing WorkingValue by BinSize and rounding to nearest integer
            // actually gives the bin
            // For example, given WorkingValue = 0.28 and BinSize = 0.1, Bin = CInt(Round(2.8,0)) = 3
            // Or, given WorkingValue = 30.83 and BinSize = 0.1, Bin = CInt(Round(308.3,0)) = 308
            return (int)(Math.Round(workingValue / histogramBinSize, 0));
        }
    }
}
