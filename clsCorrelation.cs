using System;
using System.Collections.Generic;
using System.Linq;
using MASIC.Options;
using PRISM;

namespace MASIC
{
    /// <summary>
    /// <para>
    /// This class can be used to correlate two lists of numbers (typically mass spectra) to determine their similarity
    /// The lists of numbers must have the same number of values
    /// Use the BinData function to bin a list of X,Y pairs into bins ranging from .BinStartX to .BinEndX
    /// </para>
    /// <para>
    /// These functions were originally written in VB6 and required the use of a C DLL
    /// They were later ported to VB.NET, then to C#
    /// </para>
    /// </summary>
    public class clsCorrelation : EventNotifier
    {
        // ReSharper disable once CommentTypo

        // Ignore Spelling: Spearman, Spearman's, zd

        /// <summary>
        /// Coefficients used by GammaLn
        /// </summary>
        private readonly double[] mCoefficients;

        /// <summary>
        /// Constructor
        /// </summary>
        public clsCorrelation()
            : this(GetDefaultBinningOptions())
        {
        }

        /// <summary>
        /// Constructor that includes binning options
        /// </summary>
        /// <param name="binningOptions"></param>
        public clsCorrelation(BinningOptions binningOptions)
        {
            mBinningOptions = binningOptions;
            NoiseThresholdIntensity = 0;

            mCoefficients = new[] { 76.180091729471457, -86.505320329416776,
                                    24.014098240830911, -1.231739572450155,
                                    0.001208650973866179, -0.000005395239384953 };
        }

        private const int MIN_NON_ZERO_ION_COUNT = 5;

        /// <summary>
        /// Correlation method
        /// </summary>
        public enum cmCorrelationMethodConstants
        {
            /// <summary>
            /// Pearson correlation
            /// </summary>
            Pearson = 0,

            /// <summary>
            /// Spearman correlation
            /// </summary>
            Spearman = 1,

            /// <summary>
            /// Kendall's tau
            /// </summary>
            Kendall = 2
        }

        private BinningOptions mBinningOptions;

        /// <summary>
        /// X-value of the first bin
        /// </summary>
        public float BinStartX
        {
            get => mBinningOptions.StartX;
            set => mBinningOptions.StartX = value;
        }

        /// <summary>
        /// X-value for the last bin
        /// </summary>
        public float BinEndX
        {
            get => mBinningOptions.EndX;
            set => mBinningOptions.EndX = value;
        }

        /// <summary>
        /// Bin size
        /// </summary>
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

        /// <summary>
        /// Intensity precision, as a value between 0 and 100
        /// </summary>
        public float BinnedDataIntensityPrecisionPercent
        {
            get => mBinningOptions.IntensityPrecisionPercent;
            set
            {
                if (value is < 0 or > 100)
                    value = 1;
                mBinningOptions.IntensityPrecisionPercent = value;
            }
        }

        /// <summary>
        /// Noise threshold intensity
        /// </summary>
        public float NoiseThresholdIntensity { get; set; }

        /// <summary>
        /// When true, normalize the values
        /// </summary>
        public bool NormalizeBinnedData
        {
            get => mBinningOptions.Normalize;
            set => mBinningOptions.Normalize = value;
        }

        /// <summary>
        /// Sum all of the intensities for binned ions of the same bin together
        /// </summary>
        public bool SumAllIntensitiesForBin
        {
            get => mBinningOptions.SumAllIntensitiesForBin;
            set => mBinningOptions.SumAllIntensitiesForBin = value;
        }

        /// <summary>
        /// Maximum number of bins to allow
        /// </summary>
        /// <remarks>
        /// Bin count is auto-determined as (EndX - StartX) / BinSize
        /// </remarks>
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

        private double BetaCF(double a, double b, double x)
        {
            const int MAX_ITERATIONS = 100;
            const double EPS = 0.0000003;
            const double FP_MIN = 1.0E-30;

            int m;

            var qab = a + b;
            var qap = a + 1.0;
            var qam = a - 1.0;
            var c = 1.0;
            var d = 1.0 - qab * x / qap;

            if (Math.Abs(d) < FP_MIN)
                d = FP_MIN;

            d = 1.0 / d;
            var h = d;

            for (m = 1; m <= MAX_ITERATIONS; m++)
            {
                var m2 = 2 * m;
                var aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1.0 + aa * d;

                if (Math.Abs(d) < FP_MIN)
                    d = FP_MIN;

                c = 1.0 + aa / c;

                if (Math.Abs(c) < FP_MIN)
                    c = FP_MIN;

                d = 1.0 / d;
                h *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1.0 + aa * d;

                if (Math.Abs(d) < FP_MIN)
                    d = FP_MIN;

                c = 1.0 + aa / c;

                if (Math.Abs(c) < FP_MIN)
                    c = FP_MIN;

                d = 1.0 / d;
                var del = d * c;
                h *= del;

                if (Math.Abs(del - 1.0) < EPS)
                    break;
            }

            if (m > MAX_ITERATIONS)
            {
                throw new Exception("a or b too big, or MAX_ITERATIONS too small in clsCorrelation->BetaCF");
            }

            return h;
        }

        private double BetaI(double a, double b, double x)
        {
            if (x is < 0.0 or > 1.0)
            {
                throw new Exception("Bad x in routine clsCorrelation->BetaI; should be between 0 and 1");
            }

            double bt;
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

            return 1.0 - bt * BetaCF(b, a, 1.0 - x) / b;
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
            binnedYData.Clear();
            binnedOffsetYData.Clear();

            try
            {
                var dataCount = xData.Count;
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

                var binCount = (int)Math.Round((mBinningOptions.EndX - mBinningOptions.StartX) / mBinningOptions.BinSize - 1);

                if (binCount < 1)
                    binCount = 1;

                if (binCount > mBinningOptions.MaximumBinCount)
                {
                    mBinningOptions.BinSize = (mBinningOptions.EndX - mBinningOptions.StartX) / mBinningOptions.MaximumBinCount;
                    binCount = (int)Math.Round((mBinningOptions.EndX - mBinningOptions.StartX) / mBinningOptions.BinSize - 1);
                }

                var bin2Offset = mBinningOptions.BinSize / 2;

                binnedYData.Capacity = binCount;
                binnedOffsetYData.Capacity = binCount;

                // Initialize the bins
                for (var i = 1; i <= binCount; i++)
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
                                 IList<float> binnedYData, int binCount, BinningOptions binningOptions, float offset)
        {
            try
            {
                var maximumIntensity = float.MinValue;
                for (var index = 0; index < dataCount; index++)
                {
                    if (yData[index] < NoiseThresholdIntensity)
                        continue;

                    var binNumber = ValueToBinNumber(xData[index], binningOptions.StartX + offset, binningOptions.BinSize);
                    if (binNumber >= 0 && binNumber < binCount)
                    {
                        if (binningOptions.SumAllIntensitiesForBin)
                        {
                            // Add this ion's intensity to the bin intensity
                            binnedYData[binNumber] += yData[index];
                        }
                        // Only change the bin's intensity if this ion's intensity is larger than the bin's intensity
                        // If it is, set the bin intensity to equal the ion's intensity
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

                if (!(maximumIntensity > float.MinValue))
                    maximumIntensity = 0;

                if (binningOptions.IntensityPrecisionPercent > 0)
                {
                    // Quantize the intensities to .IntensityPrecisionPercent of maximumIntensity
                    var intensityQuantizationValue = binningOptions.IntensityPrecisionPercent / 100 * maximumIntensity;
                    if (intensityQuantizationValue <= 0)
                        intensityQuantizationValue = 1;
                    if (intensityQuantizationValue > 1)
                        intensityQuantizationValue = (float)(Math.Round(intensityQuantizationValue, 0));

                    for (var index = 0; index < binCount; index++)
                    {
                        if (Math.Abs(binnedYData[index]) > float.Epsilon)
                        {
                            binnedYData[index] = (float)(Math.Round(binnedYData[index] / intensityQuantizationValue, 0)) * intensityQuantizationValue;
                        }
                    }
                }

                if (!binningOptions.Normalize || maximumIntensity <= 0)
                    return;

                for (var index = 0; index < binCount; index++)
                {
                    if (Math.Abs(binnedYData[index]) > float.Epsilon)
                    {
                        binnedYData[index] /= maximumIntensity * 100;
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
            IReadOnlyList<float> dataList1,
            IReadOnlyList<float> dataList2,
            cmCorrelationMethodConstants eCorrelationMethod)
        {
            // ReSharper disable once NotAccessedVariable
            float probabilityOfSignificance;

            // var dataList1Test = new float[] {1, 2, 2, 8, 9, 0, 0, 3, 9, 0, 5, 6};
            // var dataList2Test = new float[] {2, 3, 7, 7, 11, 1, 3, 2, 13, 0, 4, 10};

            try
            {
                var dataCount = dataList1.Count;
                if (dataList2.Count != dataList1.Count)
                {
                    return -1;
                }

                if (dataCount == 0)
                    return 0;

                // Determine the number of non-zero data points in the two spectra
                var nonZeroDataCount = 0;
                for (var index = 0; index < dataCount; index++)
                {
                    if (dataList1[index] > 0)
                        nonZeroDataCount++;
                }

                if (nonZeroDataCount < MIN_NON_ZERO_ION_COUNT)
                    return 0;

                nonZeroDataCount = 0;
                for (var index = 0; index < dataCount; index++)
                {
                    if (dataList2[index] > 0)
                        nonZeroDataCount++;
                }

                if (nonZeroDataCount < MIN_NON_ZERO_ION_COUNT)
                    return 0;

                switch (eCorrelationMethod)
                {
                    case cmCorrelationMethodConstants.Pearson:
                        CorrelatePearson(dataList1, dataList2, out var rValue, out probabilityOfSignificance, out _);
                        return rValue;

                    case cmCorrelationMethodConstants.Spearman:
                        CorrelateSpearman(dataList1, dataList2, out var diffInRanks, out _, out probabilityOfSignificance, out var RS, out _);
                        return RS;

                    case cmCorrelationMethodConstants.Kendall:
                        // ReSharper disable once IdentifierTypo
                        CorrelateKendall(dataList1, dataList2, out var kendallsTau, out _, out probabilityOfSignificance);
                        return kendallsTau;

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

        /// <summary>
        /// Performs a Pearson correlation (aka linear correlation) of the two lists
        /// The lists must have the same number of data points
        /// Code from Numerical Recipes in C
        /// </summary>
        /// <param name="dataList1"></param>
        /// <param name="dataList2"></param>
        /// <param name="rValue"></param>
        /// <param name="probabilityOfSignificance"></param>
        /// <param name="fishersZ"></param>
        private void CorrelatePearson(
            IReadOnlyList<float> dataList1,
            IReadOnlyList<float> dataList2,
            out float rValue,
            out float probabilityOfSignificance, out float fishersZ)
        {
            // TINY is used to "regularize" the unusual case of complete correlation
            const double TINY = 1.0E-20;

            // Given two arrays x[1..n] and y[1..n], this routine computes their correlation coefficient
            // r (returned as r), the significance level at which the null hypothesis of zero correlation is
            // disproved (prob whose small value indicates a significant correlation), and Fisher's z (returned
            // as z), whose value can be used in further statistical tests as described above.

            var syy = 0.0;
            var sxy = 0.0;
            var sxx = 0.0;
            var ay = 0.0;
            var ax = 0.0;

            rValue = 0;
            probabilityOfSignificance = 0;
            fishersZ = 0;

            var n = dataList1.Count;
            if (n != dataList2.Count)
            {
                throw new Exception("dataList1 and dataList2 must be lists of the same length");
            }

            if (n <= 0)
                return;

            // Find the means
            for (var j = 0; j < n; j++)
            {
                ax += dataList1[j];
                ay += dataList2[j];
            }

            ax /= n;
            ay /= n;

            // Compute the correlation coefficient
            for (var j = 0; j < n; j++)
            {
                var xt = dataList1[j] - ax;
                var yt = dataList2[j] - ay;
                sxx += xt * xt;
                syy += yt * yt;
                sxy += xt * yt;
            }

            rValue = (float)(sxy / (Math.Sqrt(sxx * syy) + TINY));

            // Fisher's z transformation
            fishersZ = (float)(0.5 * Math.Log((1.0 + rValue + TINY) / (1.0 - rValue + TINY)));
            double df = n - 2;

            var t = rValue * Math.Sqrt(df / ((1.0 - rValue + TINY) * (1.0 + rValue + TINY)));

            // Student's t probability
            probabilityOfSignificance = (float)(BetaI(0.5 * df, 0.5, df / (df + t * t)));
        }

        /// <summary>
        /// Performs a Kendall correlation (aka linear correlation) of the two lists
        /// The lists must have the same number of data points
        /// Code from Numerical Recipes in C
        /// </summary>
        /// <param name="dataList1"></param>
        /// <param name="dataList2"></param>
        /// <param name="kendallsTau"></param>
        /// <param name="z"></param>
        /// <param name="probabilityOfSignificance"></param>
        private void CorrelateKendall(
            IReadOnlyList<float> dataList1,
            IReadOnlyList<float> dataList2,
            // ReSharper disable once IdentifierTypo
            out float kendallsTau,
            out float z,
            out float probabilityOfSignificance)
        {
            // Given data arrays data1[1..n] and data2[1..n], this program returns Kendall's tau as tau,
            // its number of standard deviations from zero as z, and its two-sided significance level as prob.
            // Small values of prob indicate a significant correlation (tau positive) or anti correlation (tau
            // negative).

            long n2 = 0;
            long n1 = 0;
            var intIS = 0;

            kendallsTau = 0;
            z = 0;
            probabilityOfSignificance = 0;

            var n = dataList1.Count;
            if (n != dataList2.Count)
            {
                throw new Exception("dataList1 and dataList2 must be lists of the same length");
            }

            if (n <= 0)
                return;

            for (var j = 0; j < n - 1; j++)
            {
                for (var k = j + 1; k < n; k++)
                {
                    double a1 = dataList1[j] - dataList1[k];
                    double a2 = dataList2[j] - dataList2[k];
                    var aa = a1 * a2;
                    if (Math.Abs(aa) > double.Epsilon)
                    {
                        n1++;
                        n2++;
                        if (aa > 0)
                        {
                            intIS++;
                        }
                        else
                        {
                            intIS--;
                        }
                    }
                    else
                    {
                        if (Math.Abs(a1) > double.Epsilon)
                            n1++;
                        if (Math.Abs(a2) > double.Epsilon)
                            n2++;
                    }
                }
            }

            kendallsTau = (float)(intIS / (Math.Sqrt(n1) * Math.Sqrt(n2)));

            // ReSharper disable once IdentifierTypo
            var svar = (4.0 * n + 10.0) / (9.0 * n * (n - 1.0));

            z = (float)(kendallsTau / Math.Sqrt(svar));

            probabilityOfSignificance = (float)(ErfCC(Math.Abs(z) / 1.4142136));
        }

        /// <summary>
        /// Performs a Spearman correlation of the two lists
        /// The lists must have the same number of data points
        /// Code from Numerical Recipes in C
        /// </summary>
        /// <param name="dataList1"></param>
        /// <param name="dataList2"></param>
        /// <param name="DiffInRanks"></param>
        /// <param name="ZD"></param>
        /// <param name="probabilityOfSignificance"></param>
        /// <param name="RS"></param>
        /// <param name="ProbRS"></param>
        private void CorrelateSpearman(
            IReadOnlyCollection<float> dataList1,
            IReadOnlyCollection<float> dataList2,
            out float DiffInRanks,
            out float ZD,
            out float probabilityOfSignificance,
            out float RS,
            out float ProbRS)
        {
            // Note: data1 and data2 are re-ordered by this function; thus, they are passed ByVal

            // Given two data arrays, data1[0..n-1] and data2[0..n-1], this routine returns their sum-squared
            // difference of ranks as D, the number of standard deviations by which D deviates from its null hypothesis
            // expected value as zd, the two-sided significance level of this deviation as prob_d,
            // Spearman's rank correlation rs as rs, and the two-sided significance level of its deviation from
            // zero as prob_rs. The external routine CRank is used.  A small value of either prob_d or prob_rs indicates
            // a significant correlation (rs positive) or anti correlation (rs negative).

            DiffInRanks = 0;
            ZD = 0;
            probabilityOfSignificance = 0;
            RS = 0;
            ProbRS = 0;

            var n = dataList1.Count;
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
            CRank(n, data1, out var sf);

            // Sort data2, sorting data1 parallel to it
            Array.Sort(data2, data1);
            CRank(n, data2, out var sg);

            var DiffInRanksWork = 0.0;
            for (var j = 0; j < n; j++)
            {
                DiffInRanksWork += SquareNum(data1[j] - data2[j]);
            }

            DiffInRanks = (float)(DiffInRanksWork);

            double en = n;

            var en3n = en * en * en - en;
            var AvgD = en3n / 6.0 - (sf + sg) / 12.0;
            var fac = (1.0 - sf / en3n) * (1.0 - sg / en3n);

            // ReSharper disable once IdentifierTypo
            var vard = (en - 1.0) * en * en * SquareNum(en + 1.0) / 36.0 * fac;

            ZD = (float)((DiffInRanks - AvgD) / Math.Sqrt(vard));

            probabilityOfSignificance = (float)(ErfCC(Math.Abs(ZD) / 1.4142136));
            RS = (float)((1.0 - 6.0 / en3n * (DiffInRanks + (sf + sg) / 12.0)) / Math.Sqrt(fac));

            fac = (RS + 1.0) * (1.0 - RS);

            if (fac > 0.0)
            {
                var t = RS * Math.Sqrt((en - 2.0) / fac);
                var df = en - 2.0;
                ProbRS = (float)(BetaI(0.5 * df, 0.5, df / (df + t * t)));
            }
            else
            {
                ProbRS = 0.0F;
            }
        }

        /// <summary>
        /// Given a zero-based sorted array w(0..n-1), replaces the elements by their rank (1 .. n), including mid-ranking of ties,
        /// and returns as s the sum of f^3 - f, where f is the number of elements in each tie.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="w"></param>
        /// <param name="s"></param>
        private void CRank(int n, IList<float> w, out float s)
        {
            s = 0;
            var j = 0;
            while (j < n - 1)
            {
                if (Math.Abs(w[j + 1] - w[j]) > float.Epsilon)
                {
                    w[j] = j + 1;            // Rank = j + 1
                    j++;
                }
                else
                {
                    var jt = j + 1;
                    while (jt < n && Math.Abs(w[jt] - w[j]) < float.Epsilon)
                    {
                        jt++;
                    }

                    var rank = 0.5F * (j + jt - 1) + 1;

                    for (var ji = j; ji < jt; ji++)
                    {
                        w[ji] = rank;
                    }

                    float t = jt - j;
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
            var z = Math.Abs(x);
            var t = 1.0 / (1.0 + 0.5 * z);

            var ans = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                                   t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 +
                                   t * (-0.82215223 + t * 0.17087277)))))))));

            if (x >= 0.0)
            {
                return ans;
            }

            return 2.0 - ans;
        }

        /// <summary>
        /// Computes the natural logarithm of the Gamma Function
        /// </summary>
        /// <param name="xx"></param>
        private double GammaLn(double xx)
        {
            var x = xx;
            var y = x;

            var tmp = x + 5.5;
            tmp -= (x + 0.5) * Math.Log(tmp);
            var ser = 1.0000000001900149;

            for (var j = 0; j <= 5; j++)
            {
                y++;
                ser += mCoefficients[j] / y;
            }

            return -tmp + Math.Log(2.5066282746310007 * ser / x);
        }

        /// <summary>
        /// Get the default binning options
        /// </summary>
        public static BinningOptions GetDefaultBinningOptions()
        {
            return new BinningOptions
            {
                StartX = 50,
                EndX = 2000,
                BinSize = 1,
                IntensityPrecisionPercent = 1,
                Normalize = false,
                SumAllIntensitiesForBin = true,                     // Sum all of the intensities for binned ions of the same bin together
                MaximumBinCount = 100000
            };
        }

        /// <summary>
        /// Obtain an instance of the binning options
        /// </summary>
        /// <param name="binningOptions"></param>
        [Obsolete("Use GetDefaultBinningOptions, which returns an instance of clsBinningOptions")]
        public static void InitializeBinningOptions(out BinningOptions binningOptions)
        {
            binningOptions = GetDefaultBinningOptions();
        }

        /// <summary>
        /// Set the binning options
        /// </summary>
        /// <param name="binningOptions"></param>
        public void SetBinningOptions(BinningOptions binningOptions)
        {
            mBinningOptions = binningOptions;
        }

        private double SquareNum(double value)
        {
            if (Math.Abs(value) < double.Epsilon)
            {
                return 0;
            }

            return value * value;
        }

        private int ValueToBinNumber(float thisValue, float startValue, float histogramBinSize)
        {
            // First subtract StartValue from ThisValue
            // For example, if StartValue is 500 and ThisValue is 500.28, WorkingValue will be 0.28
            // Or, if StartValue is 500 and ThisValue is 530.83, WorkingValue will be 30.83
            var workingValue = thisValue - startValue;

            // Now, dividing WorkingValue by BinSize and rounding to nearest integer
            // actually gives the bin
            // For example, given WorkingValue = 0.28 and BinSize = 0.1, Bin = CInt(Round(2.8,0)) = 3
            // Or, given WorkingValue = 30.83 and BinSize = 0.1, Bin = CInt(Round(308.3,0)) = 308
            return (int)(Math.Round(workingValue / histogramBinSize, 0));
        }
    }
}
