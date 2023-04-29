using System;
using MASIC.Data;

namespace MASIC
{
    // ReSharper disable CommentTypo
    /// <summary>
    /// <para>
    /// This class corrects the intensities of iTraq or TMT data, based on the expected overlapping isotopic distributions
    /// It supports 4-plex and 8-plex iTraq
    /// It also supports TMT10, TMT11, TMT16 (aka TMTpro), and TMT18
    /// </para>
    /// <para>
    /// The isotopic distribution weights are provided by the iTraq or TMT manufacturer
    /// </para>
    /// </summary>
    /// <remarks>
    /// There are two options for the iTRAQ 4-plex weights:
    /// CorrectionFactorsiTRAQ4Plex.ABSciex
    /// CorrectionFactorsiTRAQ4Plex.BroadInstitute
    /// </remarks>
    // ReSharper restore CommentTypo
    public class ITraqIntensityCorrection
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: Biosystems, Da, Mertins, Philipp, plex, Proteomics, Sciex, Sickmann, Vaudel

        private const int FOUR_PLEX_MATRIX_LENGTH = 4;
        private const int EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH = 8;
        private const int EIGHT_PLEX_LOW_RES_MATRIX_LENGTH = 9;
        private const int TEN_PLEX_TMT_MATRIX_LENGTH = 10;
        private const int ELEVEN_PLEX_TMT_MATRIX_LENGTH = 11;
        private const int SIXTEEN_PLEX_TMT_MATRIX_LENGTH = 16;
        private const int EIGHTEEN_PLEX_TMT_MATRIX_LENGTH = 18;

        // ReSharper disable IdentifierTypo

        /// <summary>
        /// 4-plex iTRAQ correction factor type
        /// </summary>
        public enum CorrectionFactorsiTRAQ4Plex
        {
            /// <summary>
            /// AB Sciex
            /// </summary>
            ABSciex = 0,

            /// <summary>
            /// Broad Institute
            /// </summary>
            /// <remarks>
            /// Provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)
            /// </remarks>
            BroadInstitute = 1
        }

        // ReSharper restore IdentifierTypo

        // ReSharper restore CommentTypo

        private struct IsotopeContributionType
        {
            public float Minus2;
            public float Minus1;
            public float Zero;
            public float Plus1;
            public float Plus2;

            public override string ToString()
            {
                return string.Format("{0:F2}  {1:F2}  {2:F2}  {3:F2}  {4:F2}", Minus2, Minus1, Zero, Plus1, Plus2);
            }
        }

        /// <summary>
        /// Matrix of coefficients, derived from the isotope contribution table
        /// </summary>
        // ReSharper disable once IdentifierTypo
        private double[,] mCoeffs;

        private readonly MatrixDecompositionUtility.LUDecomposition mMatrixUtility;

        /// <summary>
        /// Reporter ion mode
        /// </summary>
        public ReporterIons.ReporterIonMassModeConstants ReporterIonMode { get; private set; }

        /// <summary>
        /// 4-plex iTRAQ correction factor type
        /// </summary>
        public CorrectionFactorsiTRAQ4Plex ITraq4PlexCorrectionFactorType { get; private set; }

        /// <summary>
        /// Constructor that assumes iTraqCorrectionFactorType = ABSciex
        /// </summary>
        /// <param name="reporterIonMode">iTRAQ or TMT mode</param>
        // ReSharper disable once UnusedMember.Global
        public ITraqIntensityCorrection(ReporterIons.ReporterIonMassModeConstants reporterIonMode)
            : this(reporterIonMode, CorrectionFactorsiTRAQ4Plex.ABSciex)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>The iTraqCorrectionFactorType parameter is only used if reporterIonMode is ITraqFourMZ</remarks>
        /// <param name="reporterIonMode">iTRAQ or TMT mode</param>
        /// <param name="iTraqCorrectionFactorType">Correction factor type for 4-plex iTRAQ</param>
        public ITraqIntensityCorrection(ReporterIons.ReporterIonMassModeConstants reporterIonMode, CorrectionFactorsiTRAQ4Plex iTraqCorrectionFactorType)
        {
            ReporterIonMode = reporterIonMode;
            ITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType;

            mMatrixUtility = new MatrixDecompositionUtility.LUDecomposition();

            if (ReporterIonMode == ReporterIons.ReporterIonMassModeConstants.CustomOrNone)
            {
                return;
            }

            InitializeCoefficients(false);
        }

        /// <summary>
        /// Change the reporter ion mode
        /// </summary>
        /// <param name="reporterIonMode"></param>
        // ReSharper disable once UnusedMember.Global
        public void UpdateReporterIonMode(ReporterIons.ReporterIonMassModeConstants reporterIonMode)
        {
            UpdateReporterIonMode(reporterIonMode, ITraq4PlexCorrectionFactorType);
        }

        /// <summary>
        /// Change the reporter ion mode
        /// </summary>
        /// <param name="reporterIonMode"></param>
        /// <param name="iTraqCorrectionFactorType"></param>
        public void UpdateReporterIonMode(ReporterIons.ReporterIonMassModeConstants reporterIonMode, CorrectionFactorsiTRAQ4Plex iTraqCorrectionFactorType)
        {
            if (ReporterIonMode != reporterIonMode || ITraq4PlexCorrectionFactorType != iTraqCorrectionFactorType)
            {
                ReporterIonMode = reporterIonMode;
                ITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType;
                InitializeCoefficients(true);
            }
        }

        /// <summary>
        /// Apply the correction factors to the reporter ions
        /// </summary>
        /// <param name="reporterIonIntensities"></param>
        /// <param name="debugShowIntensities">When true, show the old and new reporter ion intensities at the console</param>
        // ReSharper disable once UnusedMember.Global
        public bool ApplyCorrection(float[] reporterIonIntensities, bool debugShowIntensities = false)
        {
            var dataCount = reporterIonIntensities.Length - 1;

            var originalIntensities = new double[dataCount + 1];

            for (var index = 0; index <= dataCount; index++)
            {
                originalIntensities[index] = reporterIonIntensities[index];
            }

            if (ApplyCorrection(originalIntensities, debugShowIntensities))
            {
                for (var index = 0; index <= dataCount; index++)
                {
                    reporterIonIntensities[index] = (float)originalIntensities[index];
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply the correction factors to the reporter ions
        /// </summary>
        /// <param name="reporterIonIntensities"></param>
        /// <param name="debugShowIntensities">When true, show the old and new reporter ion intensities at the console</param>
        public bool ApplyCorrection(double[] reporterIonIntensities, bool debugShowIntensities = false)
        {
            var matrixSize = GetMatrixLength(ReporterIonMode);
            var reporterIonMode = ReporterIons.GetReporterIonModeDescription(ReporterIonMode);

            if (reporterIonIntensities.Length != matrixSize)
            {
                throw new InvalidOperationException(
                    "Length of ReporterIonIntensities array must be " + matrixSize +
                          " when using the " + reporterIonMode + " mode");
            }

            var correctedIntensities = mMatrixUtility.ProcessData(mCoeffs, matrixSize, reporterIonIntensities);

            var maxIntensity = 0.0;

            for (var index = 0; index < matrixSize; index++)
            {
                maxIntensity = Math.Max(maxIntensity, reporterIonIntensities[index]);
            }

            if (debugShowIntensities)
            {
                Console.WriteLine();
                Console.WriteLine("{0,-8} {1,-10} {2,-12}  {3}", "Index", "Intensity", "NewIntensity", "% Change");
            }

            // Now update reporterIonIntensities
            for (var index = 0; index < matrixSize; index++)
            {
                if (!(reporterIonIntensities[index] > 0))
                {
                    continue;
                }

                double newIntensity;

                if (correctedIntensities[index] < 0)
                {
                    newIntensity = 0;
                }
                else
                {
                    newIntensity = correctedIntensities[index];
                }

                if (debugShowIntensities)
                {
                    // Compute percent change vs. the maximum reporter ion intensity
                    var percentChange = (newIntensity - reporterIonIntensities[index]) / maxIntensity * 100;
                    var percentChangeRounded = (int)Math.Round(percentChange, 0);

                    string visualPercentChange;

                    if (percentChangeRounded > 0)
                    {
                        visualPercentChange = new string('+', percentChangeRounded);
                    }
                    else if (percentChangeRounded < 0)
                    {
                        visualPercentChange = new string('-', -percentChangeRounded);
                    }
                    else
                    {
                        visualPercentChange = string.Empty;
                    }

                    Console.WriteLine("{0,-8} {1,-10:0.0} {2,-12:0.0}{3,7:0.0}%   {4}", index, reporterIonIntensities[index], newIntensity, percentChange, visualPercentChange);
                }

                reporterIonIntensities[index] = newIntensity;
            }

            return true;
        }

        private int GetMatrixLength(ReporterIons.ReporterIonMassModeConstants reporterIonMode)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

            return reporterIonMode switch
            {
                ReporterIons.ReporterIonMassModeConstants.ITraqFourMZ => FOUR_PLEX_MATRIX_LENGTH,
                ReporterIons.ReporterIonMassModeConstants.ITraqEightMZHighRes => EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH,
                ReporterIons.ReporterIonMassModeConstants.ITraqEightMZLowRes => EIGHT_PLEX_LOW_RES_MATRIX_LENGTH,
                ReporterIons.ReporterIonMassModeConstants.TMTTenMZ => TEN_PLEX_TMT_MATRIX_LENGTH,
                ReporterIons.ReporterIonMassModeConstants.TMTElevenMZ => ELEVEN_PLEX_TMT_MATRIX_LENGTH,
                ReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ => SIXTEEN_PLEX_TMT_MATRIX_LENGTH,
                ReporterIons.ReporterIonMassModeConstants.TMTEighteenMZ => EIGHTEEN_PLEX_TMT_MATRIX_LENGTH,
                _ => throw new ArgumentOutOfRangeException("Invalid value for reporterIonMode in GetMatrixLength: " + reporterIonMode)
            };
        }

        /// <summary>
        /// Initialize the coefficients
        /// </summary>
        /// <param name="debugShowMatrixTable">When true, show a table of the coefficients at the console</param>
        private void InitializeCoefficients(bool debugShowMatrixTable)
        {
            // ReSharper disable TooWideLocalVariableScope

            // iTraq reporter ions
            IsotopeContributionType udtIsoPct113;
            IsotopeContributionType udtIsoPct114;
            IsotopeContributionType udtIsoPct115;
            IsotopeContributionType udtIsoPct116;
            IsotopeContributionType udtIsoPct117;
            IsotopeContributionType udtIsoPct118;
            IsotopeContributionType udtIsoPct119;
            IsotopeContributionType udtIsoPct120;
            IsotopeContributionType udtIsoPct121;

            // TMT reporter ions
            IsotopeContributionType udtIsoPct126;
            IsotopeContributionType udtIsoPct127N;
            IsotopeContributionType udtIsoPct127C;
            IsotopeContributionType udtIsoPct128N;
            IsotopeContributionType udtIsoPct128C;
            IsotopeContributionType udtIsoPct129N;
            IsotopeContributionType udtIsoPct129C;
            IsotopeContributionType udtIsoPct130N;
            IsotopeContributionType udtIsoPct130C;
            IsotopeContributionType udtIsoPct131N;
            IsotopeContributionType udtIsoPct131C;

            IsotopeContributionType udtIsoPct132N;
            IsotopeContributionType udtIsoPct132C;
            IsotopeContributionType udtIsoPct133N;
            IsotopeContributionType udtIsoPct133C;
            IsotopeContributionType udtIsoPct134N;
            IsotopeContributionType udtIsoPct134C;
            IsotopeContributionType udtIsoPct135N;

            // ReSharper restore TooWideLocalVariableScope

            var matrixSize = GetMatrixLength(ReporterIonMode);
            var maxIndex = matrixSize - 1;

            switch (ReporterIonMode)
            {
                case ReporterIons.ReporterIonMassModeConstants.ITraqFourMZ:
                    if (ITraq4PlexCorrectionFactorType == CorrectionFactorsiTRAQ4Plex.ABSciex)
                    {
                        // 4-plex ITraq, isotope contribution table
                        // Source percentages provided by Applied Biosystems

                        udtIsoPct114 = DefineIsotopeContribution(0, 1, 92.9F, 5.9F, 0.2F);
                        udtIsoPct115 = DefineIsotopeContribution(0, 2, 92.3F, 5.6F, 0.1F);
                        udtIsoPct116 = DefineIsotopeContribution(0, 3, 92.4F, 4.5F, 0.1F);
                        udtIsoPct117 = DefineIsotopeContribution(0.1F, 4, 92.3F, 3.5F, 0.1F);
                    }
                    else if (ITraq4PlexCorrectionFactorType == CorrectionFactorsiTRAQ4Plex.BroadInstitute)
                    {
                        // 4-plex ITraq, isotope contribution table
                        // Source percentages provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)

                        udtIsoPct114 = DefineIsotopeContribution(0, 0, 95.5F, 4.5F, 0);
                        udtIsoPct115 = DefineIsotopeContribution(0, 0.9F, 94.6F, 4.5F, 0);
                        udtIsoPct116 = DefineIsotopeContribution(0, 0.9F, 95.7F, 3.4F, 0);
                        udtIsoPct117 = DefineIsotopeContribution(0, 1.4F, 98.6F, 0, 0);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(ITraq4PlexCorrectionFactorType), "Unrecognized value for the iTRAQ 4 plex correction type");
                    }

                    // Goal is to generate either of these two matrices (depending on mITraq4PlexCorrectionFactorType):
                    // 0      1      2      3
                    // -----  -----  -----  -----
                    // 0   0.929  0.020    0      0
                    // 1   0.059  0.923  0.030  0.001
                    // 2   0.002  0.056  0.924  0.040
                    // 3     0    0.001  0.045  0.923

                    // 0      1      2      3
                    // -----  -----  -----  -----
                    // 0   0.955  0.009    0      0
                    // 1   0.045  0.946  0.009    0
                    // 2     0    0.045  0.957  0.014
                    // 3     0      0    0.034  0.986

                    mCoeffs = new double[maxIndex + 1, maxIndex + 1];

                    mCoeffs[0, 0] = udtIsoPct114.Zero;
                    mCoeffs[0, 1] = udtIsoPct115.Minus1;
                    mCoeffs[0, 2] = udtIsoPct116.Minus2;

                    mCoeffs[1, 0] = udtIsoPct114.Plus1;
                    mCoeffs[1, 1] = udtIsoPct115.Zero;
                    mCoeffs[1, 2] = udtIsoPct116.Minus1;
                    mCoeffs[1, 3] = udtIsoPct117.Minus2;

                    mCoeffs[2, 0] = udtIsoPct114.Plus2;
                    mCoeffs[2, 2] = udtIsoPct116.Zero;
                    mCoeffs[2, 1] = udtIsoPct115.Plus1;
                    mCoeffs[2, 3] = udtIsoPct117.Minus1;

                    mCoeffs[3, 1] = udtIsoPct115.Plus2;
                    mCoeffs[3, 2] = udtIsoPct116.Plus1;
                    mCoeffs[3, 3] = udtIsoPct117.Zero;
                    break;

                case ReporterIons.ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    // 8-plex ITraq, isotope contribution table for High Res MS/MS
                    // Source percentages provided by Applied Biosystems
                    // Note there is a 2 Da jump between 119 and 121, which is why 7.44 and 0.87 are not included in mCoeffs()

                    udtIsoPct113 = DefineIsotopeContribution(0, 0, 92.89F, 6.89F, 0.22F);
                    udtIsoPct114 = DefineIsotopeContribution(0, 0.94F, 93.01F, 5.9F, 0.16F);
                    udtIsoPct115 = DefineIsotopeContribution(0, 1.88F, 93.12F, 4.9F, 0.1F);
                    udtIsoPct116 = DefineIsotopeContribution(0, 2.82F, 93.21F, 3.9F, 0.07F);
                    udtIsoPct117 = DefineIsotopeContribution(0.06F, 3.77F, 93.29F, 2.88F, 0);
                    udtIsoPct118 = DefineIsotopeContribution(0.09F, 4.71F, 93.32F, 1.88F, 0);
                    udtIsoPct119 = DefineIsotopeContribution(0.14F, 5.66F, 93.34F, 0.87F, 0);
                    udtIsoPct121 = DefineIsotopeContribution(0.27F, 7.44F, 92.11F, 0.18F, 0);

                    // Goal is to generate this matrix:
                    //       0       1       2       3       4       5       6       7
                    //     ------  ------  ------  ------  ------  ------  ------  ------
                    // 0   0.9289  0.0094    0       0       0       0       0       0
                    // 1   0.0689  0.9301  0.0188    0       0       0       0       0
                    // 2   0.0022  0.0590  0.9312  0.0282  0.0006    0       0       0
                    // 3     0     0.0016  0.0490  0.9321  0.0377  0.0009    0       0
                    // 4     0       0     0.0010  0.0390  0.9329  0.0471  0.0014    0
                    // 5     0       0       0     0.0007  0.0288  0.9332  0.0566    0
                    // 6     0       0       0       0       0     0.0188  0.9334  0.0027
                    // 7     0       0       0       0       0       0       0     0.9211

                    mCoeffs = new double[maxIndex + 1, maxIndex + 1];

                    mCoeffs[0, 0] = udtIsoPct113.Zero;
                    mCoeffs[0, 1] = udtIsoPct114.Minus1;
                    mCoeffs[0, 2] = udtIsoPct115.Minus2;

                    mCoeffs[1, 0] = udtIsoPct113.Plus1;
                    mCoeffs[1, 1] = udtIsoPct114.Zero;
                    mCoeffs[1, 2] = udtIsoPct115.Minus1;
                    mCoeffs[1, 3] = udtIsoPct116.Minus2;

                    mCoeffs[2, 0] = udtIsoPct113.Plus2;
                    mCoeffs[2, 1] = udtIsoPct114.Plus1;
                    mCoeffs[2, 2] = udtIsoPct115.Zero;
                    mCoeffs[2, 3] = udtIsoPct116.Minus1;
                    mCoeffs[2, 4] = udtIsoPct117.Minus2;

                    mCoeffs[3, 1] = udtIsoPct114.Plus2;
                    mCoeffs[3, 2] = udtIsoPct115.Plus1;
                    mCoeffs[3, 3] = udtIsoPct116.Zero;
                    mCoeffs[3, 4] = udtIsoPct117.Minus1;
                    mCoeffs[3, 5] = udtIsoPct118.Minus2;

                    mCoeffs[4, 2] = udtIsoPct115.Plus2;
                    mCoeffs[4, 3] = udtIsoPct116.Plus1;
                    mCoeffs[4, 4] = udtIsoPct117.Zero;
                    mCoeffs[4, 5] = udtIsoPct118.Minus1;
                    mCoeffs[4, 6] = udtIsoPct119.Minus2;

                    mCoeffs[5, 3] = udtIsoPct116.Plus2;
                    mCoeffs[5, 4] = udtIsoPct117.Plus1;
                    mCoeffs[5, 5] = udtIsoPct118.Zero;
                    mCoeffs[5, 6] = udtIsoPct119.Minus1;
                    mCoeffs[5, 7] = 0;

                    mCoeffs[6, 4] = udtIsoPct117.Plus2;
                    mCoeffs[6, 5] = udtIsoPct118.Plus1;
                    mCoeffs[6, 6] = udtIsoPct119.Zero;
                    mCoeffs[6, 7] = udtIsoPct121.Minus2;

                    mCoeffs[7, 5] = 0;           // udtIsoPct118.Plus2
                    mCoeffs[7, 7] = udtIsoPct121.Zero;
                    break;

                case ReporterIons.ReporterIonMassModeConstants.ITraqEightMZLowRes:
                    // 8-plex ITraq, isotope contribution table for Low Res MS/MS

                    // ReSharper disable CommentTypo

                    // Source percentages come from page 664 in:
                    // Vaudel, M., Sickmann, A., and L. Martens. "Peptide and protein quantification: A map of the minefield",
                    // Proteomics 2010, 10, 650-670.

                    // ReSharper restore CommentTypo

                    udtIsoPct113 = DefineIsotopeContribution(0, 0, 92.89F, 6.89F, 0.22F);
                    udtIsoPct114 = DefineIsotopeContribution(0, 0.94F, 93.01F, 5.9F, 0.16F);
                    udtIsoPct115 = DefineIsotopeContribution(0, 1.88F, 93.12F, 4.9F, 0.1F);
                    udtIsoPct116 = DefineIsotopeContribution(0, 2.82F, 93.21F, 3.9F, 0.07F);
                    udtIsoPct117 = DefineIsotopeContribution(0.06F, 3.77F, 93.29F, 2.88F, 0);
                    udtIsoPct118 = DefineIsotopeContribution(0.09F, 4.71F, 93.32F, 1.88F, 0);
                    udtIsoPct119 = DefineIsotopeContribution(0.14F, 5.66F, 93.34F, 0.87F, 0);
                    udtIsoPct120 = DefineIsotopeContribution(0, 0, 91.01F, 8.62F, 0);
                    udtIsoPct121 = DefineIsotopeContribution(0.27F, 7.44F, 92.11F, 0.18F, 0);

                    // Goal is to generate this expanded matrix, which takes Phenylalanine into account
                    //       0       1       2       3       4       5       6       7       8
                    //     ------  ------  ------  ------  ------  ------  ------  ------  ------
                    // 0   0.9289  0.0094    0       0       0       0       0       0       0
                    // 1   0.0689  0.9301  0.0188    0       0       0       0       0       0
                    // 2   0.0022  0.0590  0.9312  0.0282  0.0006    0       0       0       0
                    // 3     0     0.0016  0.0490  0.9321  0.0377  0.0009    0       0       0
                    // 4     0       0     0.0010  0.0390  0.9329  0.0471  0.0014    0       0
                    // 5     0       0       0     0.0007  0.0288  0.9332  0.0566    0       0
                    // 6     0       0       0       0       0     0.0188  0.9334    0     0.0027
                    // 7     0       0       0       0       0       0     0.8700  0.9101  0.0744
                    // 8     0       0       0       0       0       0       0     0.0862  0.9211

                    mCoeffs = new double[maxIndex + 1, maxIndex + 1];

                    mCoeffs[0, 0] = udtIsoPct113.Zero;
                    mCoeffs[0, 1] = udtIsoPct114.Minus1;
                    mCoeffs[0, 2] = udtIsoPct115.Minus2;

                    mCoeffs[1, 0] = udtIsoPct113.Plus1;
                    mCoeffs[1, 1] = udtIsoPct114.Zero;
                    mCoeffs[1, 2] = udtIsoPct115.Minus1;
                    mCoeffs[1, 3] = udtIsoPct116.Minus2;

                    mCoeffs[2, 0] = udtIsoPct113.Plus2;
                    mCoeffs[2, 1] = udtIsoPct114.Plus1;
                    mCoeffs[2, 2] = udtIsoPct115.Zero;
                    mCoeffs[2, 3] = udtIsoPct116.Minus1;
                    mCoeffs[2, 4] = udtIsoPct117.Minus2;

                    mCoeffs[3, 1] = udtIsoPct114.Plus2;
                    mCoeffs[3, 2] = udtIsoPct115.Plus1;
                    mCoeffs[3, 3] = udtIsoPct116.Zero;
                    mCoeffs[3, 4] = udtIsoPct117.Minus1;
                    mCoeffs[3, 5] = udtIsoPct118.Minus2;

                    mCoeffs[4, 2] = udtIsoPct115.Plus2;
                    mCoeffs[4, 3] = udtIsoPct116.Plus1;
                    mCoeffs[4, 4] = udtIsoPct117.Zero;
                    mCoeffs[4, 5] = udtIsoPct118.Minus1;
                    mCoeffs[4, 6] = udtIsoPct119.Minus2;

                    mCoeffs[5, 3] = udtIsoPct116.Plus2;
                    mCoeffs[5, 4] = udtIsoPct117.Plus1;
                    mCoeffs[5, 5] = udtIsoPct118.Zero;
                    mCoeffs[5, 6] = udtIsoPct119.Minus1;
                    mCoeffs[5, 7] = 0;

                    mCoeffs[6, 4] = udtIsoPct117.Plus2;
                    mCoeffs[6, 5] = udtIsoPct118.Plus1;
                    mCoeffs[6, 6] = udtIsoPct119.Zero;
                    mCoeffs[6, 7] = 0;
                    mCoeffs[6, 8] = udtIsoPct121.Minus2;

                    mCoeffs[7, 5] = 0;
                    mCoeffs[7, 6] = udtIsoPct119.Plus1;
                    mCoeffs[7, 7] = udtIsoPct120.Zero;
                    mCoeffs[7, 8] = udtIsoPct121.Minus1;

                    mCoeffs[8, 6] = udtIsoPct119.Plus2;
                    mCoeffs[8, 7] = udtIsoPct120.Plus1;
                    mCoeffs[8, 8] = udtIsoPct121.Zero;
                    break;

                case ReporterIons.ReporterIonMassModeConstants.TMTTenMZ:
                case ReporterIons.ReporterIonMassModeConstants.TMTElevenMZ:
                    // 10-plex TMT and 11-plex TMT, isotope contribution table for High Res MS/MS
                    // Source percentages provided by Thermo

                    // TMT10plex lot RG234623
                    // TMT11plex lot SD250515
                    // udtIsoPct126 = DefineIsotopeContribution(0, 0, 95.1, 4.9, 0)
                    // udtIsoPct127N = DefineIsotopeContribution(0, 0.2, 94, 5.8, 0)
                    // udtIsoPct127C = DefineIsotopeContribution(0, 0.3, 94.9, 4.8, 0)
                    // udtIsoPct128N = DefineIsotopeContribution(0, 0.3, 96.1, 3.6, 0)
                    // udtIsoPct128C = DefineIsotopeContribution(0, 0.6, 95.5, 3.9, 0)
                    // udtIsoPct129N = DefineIsotopeContribution(0, 0.8, 96.2, 3, 0)
                    // udtIsoPct129C = DefineIsotopeContribution(0, 1.3, 95.8, 2.9, 0)
                    // udtIsoPct130N = DefineIsotopeContribution(0, 1.4, 93, 2.3, 3.3)
                    // udtIsoPct130C = DefineIsotopeContribution(0, 1.7, 96.1, 2.2, 0)
                    // udtIsoPct131N = DefineIsotopeContribution(0.2, 2, 95.6, 2.2, 0)
                    // udtIsoPct131C = DefineIsotopeContribution(0, 2.6, 94.5, 2.9, 0)

                    // TMT10plex lot A37725
                    // TMT11plex lot TB265130
                    // udtIsoPct126 = DefineIsotopeContribution(0, 0, 92.081, 7.551, 0.368)
                    // udtIsoPct127N = DefineIsotopeContribution(0, 0.093, 92.593, 7.315, 0)
                    // udtIsoPct127C = DefineIsotopeContribution(0, 0.468, 93.633, 5.899, 0)
                    // udtIsoPct128N = DefineIsotopeContribution(0, 0.658, 93.985, 5.357, 0)
                    // udtIsoPct128C = DefineIsotopeContribution(0.186, 1.484, 92.764, 5.566, 0)
                    // udtIsoPct129N = DefineIsotopeContribution(0, 2.326, 93.023, 4.651, 0)
                    // udtIsoPct129C = DefineIsotopeContribution(0, 2.158, 93.809, 4.034, 0)
                    // udtIsoPct130N = DefineIsotopeContribution(0, 2.533, 93.809, 3.659, 0)
                    // udtIsoPct130C = DefineIsotopeContribution(0, 1.628, 95.785, 2.586, 0)
                    // udtIsoPct131N = DefineIsotopeContribution(0, 3.625, 92.937, 3.439, 0)
                    // udtIsoPct131C = DefineIsotopeContribution(0, 3.471, 93.809, 2.72, 0)

                    // TMT10plex lot SG252258
                    // TMT11plex lot T4259309
                    udtIsoPct126 = DefineIsotopeContribution(0, 0, 100, 7.2F, 0.2F);
                    udtIsoPct127N = DefineIsotopeContribution(0, 0.4F, 100, 7.3F, 0.2F);
                    udtIsoPct127C = DefineIsotopeContribution(0, 0.5F, 100, 6.3F, 0);
                    udtIsoPct128N = DefineIsotopeContribution(0, 0.7F, 100, 5.7F, 0);
                    udtIsoPct128C = DefineIsotopeContribution(0, 1.4F, 100, 5.1F, 0);
                    udtIsoPct129N = DefineIsotopeContribution(0, 2.5F, 100, 5, 0);
                    udtIsoPct129C = DefineIsotopeContribution(0, 2.3F, 100, 4.3F, 0);
                    udtIsoPct130N = DefineIsotopeContribution(0, 2.7F, 100, 3.9F, 0);
                    udtIsoPct130C = DefineIsotopeContribution(0.4F, 2.9F, 100, 3.3F, 0);
                    udtIsoPct131N = DefineIsotopeContribution(0, 3.4F, 100, 3.3F, 0);
                    udtIsoPct131C = DefineIsotopeContribution(0, 3.7F, 100, 2.9F, 0);

                    // Goal is to generate this matrix (11-plex will not have the final row or final column)
                    //       0       1       2       3       4       5       6       7       8       9       10
                    //     ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------
                    // 0   0.9260    0     0.0050    0       0       0       0       0       0       0       0
                    // 1     0     0.9210    0     0.0070    0       0       0       0       0       0       0
                    // 2   0.0720    0     0.9320    0     0.0140    0       0       0       0       0       0
                    // 3     0     0.0730    0     0.9360    0     0.0250    0       0       0       0       0
                    // 4   0.0020    0     0.0630    0     0.9350    0     0.0230    0     0.0040    0       0
                    // 5     0     0.0020    0     0.0570    0     0.9250    0     0.0270    0       0       0
                    // 6     0       0       0       0     0.0510    0     0.9340    0     0.0290    0       0
                    // 7     0       0       0       0       0     0.0500    0     0.9340    0     0.0340    0
                    // 8     0       0       0       0       0       0     0.0430    0     0.9340    0     0.0370
                    // 9     0       0       0       0       0       0       0     0.0390    0     0.9330    0
                    // 10    0       0       0       0       0       0       0       0     0.0330    0     0.9340
                    //

                    mCoeffs = new double[maxIndex + 1, maxIndex + 1];

                    mCoeffs[0, 0] = udtIsoPct126.Zero;
                    mCoeffs[0, 1] = 0;
                    mCoeffs[0, 2] = udtIsoPct127C.Minus1;
                    mCoeffs[0, 3] = 0;
                    mCoeffs[0, 4] = udtIsoPct128C.Minus2;

                    mCoeffs[1, 0] = 0;
                    mCoeffs[1, 1] = udtIsoPct127N.Zero;
                    mCoeffs[1, 2] = 0;
                    mCoeffs[1, 3] = udtIsoPct128N.Minus1;
                    mCoeffs[1, 4] = 0;
                    mCoeffs[1, 5] = udtIsoPct129N.Minus2;

                    mCoeffs[2, 0] = udtIsoPct126.Plus1;
                    mCoeffs[2, 1] = 0;
                    mCoeffs[2, 2] = udtIsoPct127C.Zero;
                    mCoeffs[2, 3] = 0;
                    mCoeffs[2, 4] = udtIsoPct128C.Minus1;
                    mCoeffs[2, 5] = 0;
                    mCoeffs[2, 6] = udtIsoPct129C.Minus2;

                    mCoeffs[3, 0] = 0;
                    mCoeffs[3, 1] = udtIsoPct127N.Plus1;
                    mCoeffs[3, 2] = 0;
                    mCoeffs[3, 3] = udtIsoPct128N.Zero;
                    mCoeffs[3, 4] = 0;
                    mCoeffs[3, 5] = udtIsoPct129N.Minus1;
                    mCoeffs[3, 6] = 0;
                    mCoeffs[3, 7] = udtIsoPct130N.Minus2;

                    mCoeffs[4, 0] = udtIsoPct126.Plus2;
                    mCoeffs[4, 1] = 0;
                    mCoeffs[4, 2] = udtIsoPct127C.Plus1;
                    mCoeffs[4, 3] = 0;
                    mCoeffs[4, 4] = udtIsoPct128C.Zero;
                    mCoeffs[4, 5] = 0;
                    mCoeffs[4, 6] = udtIsoPct129C.Minus1;
                    mCoeffs[4, 7] = 0;
                    mCoeffs[4, 8] = udtIsoPct130C.Minus2;

                    mCoeffs[5, 1] = udtIsoPct127N.Plus2;
                    mCoeffs[5, 2] = 0;
                    mCoeffs[5, 3] = udtIsoPct128N.Plus1;
                    mCoeffs[5, 4] = 0;
                    mCoeffs[5, 5] = udtIsoPct129N.Zero;
                    mCoeffs[5, 6] = 0;
                    mCoeffs[5, 7] = udtIsoPct130N.Minus1;
                    mCoeffs[5, 8] = 0;
                    mCoeffs[5, 9] = udtIsoPct131N.Minus2;

                    mCoeffs[6, 2] = udtIsoPct127C.Plus2;
                    mCoeffs[6, 3] = 0;
                    mCoeffs[6, 4] = udtIsoPct128C.Plus1;
                    mCoeffs[6, 5] = 0;
                    mCoeffs[6, 6] = udtIsoPct129C.Zero;
                    mCoeffs[6, 7] = 0;
                    mCoeffs[6, 8] = udtIsoPct130C.Minus1;

                    mCoeffs[7, 3] = udtIsoPct128N.Plus2;
                    mCoeffs[7, 4] = 0;
                    mCoeffs[7, 5] = udtIsoPct129N.Plus1;
                    mCoeffs[7, 6] = 0;
                    mCoeffs[7, 7] = udtIsoPct130N.Zero;
                    mCoeffs[7, 8] = 0;
                    mCoeffs[7, 9] = udtIsoPct131N.Minus1;

                    mCoeffs[8, 4] = udtIsoPct128C.Plus2;
                    mCoeffs[8, 5] = 0;
                    mCoeffs[8, 6] = udtIsoPct129C.Plus1;
                    mCoeffs[8, 7] = 0;
                    mCoeffs[8, 8] = udtIsoPct130C.Zero;
                    mCoeffs[8, 9] = 0;

                    if (maxIndex >= 10)
                    {
                        mCoeffs[8, 10] = udtIsoPct131C.Minus1;
                    }

                    mCoeffs[9, 5] = udtIsoPct129N.Plus2;
                    mCoeffs[9, 6] = 0;
                    mCoeffs[9, 7] = udtIsoPct130N.Plus1;
                    mCoeffs[9, 8] = 0;
                    mCoeffs[9, 9] = udtIsoPct131N.Zero;

                    if (maxIndex >= 10)
                    {
                        mCoeffs[10, 6] = udtIsoPct129C.Plus2;
                        mCoeffs[10, 7] = 0;
                        mCoeffs[10, 8] = udtIsoPct130C.Plus1;
                        mCoeffs[10, 9] = 0;
                        mCoeffs[10, 10] = udtIsoPct131C.Zero;
                    }

                    break;

                case ReporterIons.ReporterIonMassModeConstants.TMTSixteenMZ:
                    // 16-plex TMT, isotope contribution table for High Res MS/MS
                    // Source percentages provided by Thermo

                    // ReSharper disable once CommentTypo
                    // TMTpro lot UH290428

                    // Column map
                    // PDF file column   Contribution field
                    // ---------------   ------------------
                    // -2x 13C           minus2
                    //    -13C           minus1
                    //      M+           zero
                    //    +13C           plus1
                    // +2x 13C           plus2

                    udtIsoPct126 = DefineIsotopeContribution(0, 0, 100, 7.73F, 0.22F);
                    udtIsoPct127N = DefineIsotopeContribution(0, 0, 100, 7.46F, 0.22F);
                    udtIsoPct127C = DefineIsotopeContribution(0, 0.71F, 100, 6.62F, 0.16F);
                    udtIsoPct128N = DefineIsotopeContribution(0, 0.75F, 100, 6.67F, 0.16F);
                    udtIsoPct128C = DefineIsotopeContribution(0.06F, 1.34F, 100, 5.31F, 0.11F);
                    udtIsoPct129N = DefineIsotopeContribution(0.01F, 1.29F, 100, 5.48F, 0.1F);
                    udtIsoPct129C = DefineIsotopeContribution(0.26F, 2.34F, 100, 4.87F, 0.08F);
                    udtIsoPct130N = DefineIsotopeContribution(0.39F, 2.36F, 100, 4.57F, 0.07F);
                    udtIsoPct130C = DefineIsotopeContribution(0.05F, 2.67F, 100, 3.85F, 0.15F);
                    udtIsoPct131N = DefineIsotopeContribution(0.05F, 2.71F, 100, 3.73F, 0.04F);
                    udtIsoPct131C = DefineIsotopeContribution(0.09F, 3.69F, 100, 2.77F, 0.01F);
                    udtIsoPct132N = DefineIsotopeContribution(0.09F, 2.51F, 100, 2.76F, 0.01F);
                    udtIsoPct132C = DefineIsotopeContribution(0.1F, 4.11F, 100, 1.63F, 0);
                    udtIsoPct133N = DefineIsotopeContribution(0.09F, 3.09F, 100, 1.58F, 0);
                    udtIsoPct133C = DefineIsotopeContribution(0.36F, 4.63F, 100, 0.88F, 0);
                    udtIsoPct134N = DefineIsotopeContribution(0.38F, 4.82F, 100, 0.86F, 0);

                    // Goal is to generate a 16x16 matrix
                    //       0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15
                    // --  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------
                    // 0   0.9205    0     0.0071    0     0.0006    0       0       0       0       0       0       0       0       0       0       0
                    // 1     0     0.9232    0     0.0075    0     0.0001    0       0       0       0       0       0       0       0       0       0
                    // 2   0.0773    0     0.9251    0     0.0134    0     0.0026    0       0       0       0       0       0       0       0       0
                    // 3     0     0.0746    0     0.9242    0     0.0129    0     0.0039    0       0       0       0       0       0       0       0
                    // 4   0.0022    0     0.0662    0     0.9318    0     0.0234    0     0.0005    0       0       0       0       0       0       0
                    // 5     0     0.0022    0     0.0667    0     0.9312    0     0.0236    0     0.0005    0       0       0       0       0       0
                    // 6     0       0     0.0016    0     0.0531    0     0.9245    0     0.0267    0     0.0009    0       0       0       0       0
                    // 7     0       0       0     0.0016    0     0.0548    0     0.9261    0     0.0271    0     0.0009    0       0       0       0
                    // 8     0       0       0       0     0.0011    0     0.0487    0     0.9328    0     0.0369    0     0.0010    0       0       0
                    // 9     0       0       0       0       0     0.0010    0     0.0457    0     0.9347    0     0.0251    0     0.0009    0       0
                    // 10    0       0       0       0       0       0     0.0008    0     0.0385    0     0.9344    0     0.0411    0     0.0036    0
                    // 11    0       0       0       0       0       0       0     0.0007    0     0.0373    0     0.9463    0     0.0309    0     0.0038
                    // 12    0       0       0       0       0       0       0       0     0.0015    0     0.0277    0     0.9416    0     0.0463    0
                    // 13    0       0       0       0       0       0       0       0       0     0.0004    0     0.0276    0     0.9524    0     0.0482
                    // 14    0       0       0       0       0       0       0       0       0       0     0.0001    0     0.0163    0     0.9413    0
                    // 15    0       0       0       0       0       0       0       0       0       0       0     0.0001    0     0.0158    0     0.9394

                    mCoeffs = new double[maxIndex + 1, maxIndex + 1];

                    mCoeffs[0, 0] = udtIsoPct126.Zero;
                    mCoeffs[0, 1] = 0;
                    mCoeffs[0, 2] = udtIsoPct127C.Minus1;
                    mCoeffs[0, 3] = 0;
                    mCoeffs[0, 4] = udtIsoPct128C.Minus2;

                    mCoeffs[1, 0] = 0;
                    mCoeffs[1, 1] = udtIsoPct127N.Zero;
                    mCoeffs[1, 2] = 0;
                    mCoeffs[1, 3] = udtIsoPct128N.Minus1;
                    mCoeffs[1, 4] = 0;
                    mCoeffs[1, 5] = udtIsoPct129N.Minus2;

                    mCoeffs[2, 0] = udtIsoPct126.Plus1;
                    mCoeffs[2, 1] = 0;
                    mCoeffs[2, 2] = udtIsoPct127C.Zero;
                    mCoeffs[2, 3] = 0;
                    mCoeffs[2, 4] = udtIsoPct128C.Minus1;
                    mCoeffs[2, 5] = 0;
                    mCoeffs[2, 6] = udtIsoPct129C.Minus2;

                    mCoeffs[3, 0] = 0;
                    mCoeffs[3, 1] = udtIsoPct127N.Plus1;
                    mCoeffs[3, 2] = 0;
                    mCoeffs[3, 3] = udtIsoPct128N.Zero;
                    mCoeffs[3, 4] = 0;
                    mCoeffs[3, 5] = udtIsoPct129N.Minus1;
                    mCoeffs[3, 6] = 0;
                    mCoeffs[3, 7] = udtIsoPct130N.Minus2;

                    mCoeffs[4, 0] = udtIsoPct126.Plus2;
                    mCoeffs[4, 1] = 0;
                    mCoeffs[4, 2] = udtIsoPct127C.Plus1;
                    mCoeffs[4, 3] = 0;
                    mCoeffs[4, 4] = udtIsoPct128C.Zero;
                    mCoeffs[4, 5] = 0;
                    mCoeffs[4, 6] = udtIsoPct129C.Minus1;
                    mCoeffs[4, 7] = 0;
                    mCoeffs[4, 8] = udtIsoPct130C.Minus2;

                    mCoeffs[5, 1] = udtIsoPct127N.Plus2;
                    mCoeffs[5, 2] = 0;
                    mCoeffs[5, 3] = udtIsoPct128N.Plus1;
                    mCoeffs[5, 4] = 0;
                    mCoeffs[5, 5] = udtIsoPct129N.Zero;
                    mCoeffs[5, 6] = 0;
                    mCoeffs[5, 7] = udtIsoPct130N.Minus1;
                    mCoeffs[5, 8] = 0;
                    mCoeffs[5, 9] = udtIsoPct131N.Minus2;

                    mCoeffs[6, 2] = udtIsoPct127C.Plus2;
                    mCoeffs[6, 3] = 0;
                    mCoeffs[6, 4] = udtIsoPct128C.Plus1;
                    mCoeffs[6, 5] = 0;
                    mCoeffs[6, 6] = udtIsoPct129C.Zero;
                    mCoeffs[6, 7] = 0;
                    mCoeffs[6, 8] = udtIsoPct130C.Minus1;
                    mCoeffs[6, 9] = 0;
                    mCoeffs[6, 10] = udtIsoPct131C.Minus2;

                    mCoeffs[7, 3] = udtIsoPct128N.Plus2;
                    mCoeffs[7, 4] = 0;
                    mCoeffs[7, 5] = udtIsoPct129N.Plus1;
                    mCoeffs[7, 6] = 0;
                    mCoeffs[7, 7] = udtIsoPct130N.Zero;
                    mCoeffs[7, 8] = 0;
                    mCoeffs[7, 9] = udtIsoPct131N.Minus1;
                    mCoeffs[7, 10] = 0;
                    mCoeffs[7, 11] = udtIsoPct132N.Minus2;

                    mCoeffs[8, 4] = udtIsoPct128C.Plus2;
                    mCoeffs[8, 5] = 0;
                    mCoeffs[8, 6] = udtIsoPct129C.Plus1;
                    mCoeffs[8, 7] = 0;
                    mCoeffs[8, 8] = udtIsoPct130C.Zero;
                    mCoeffs[8, 9] = 0;
                    mCoeffs[8, 10] = udtIsoPct131C.Minus1;
                    mCoeffs[8, 11] = 0;
                    mCoeffs[8, 12] = udtIsoPct132C.Minus2;

                    mCoeffs[9, 5] = udtIsoPct129N.Plus2;
                    mCoeffs[9, 6] = 0;
                    mCoeffs[9, 7] = udtIsoPct130N.Plus1;
                    mCoeffs[9, 8] = 0;
                    mCoeffs[9, 9] = udtIsoPct131N.Zero;
                    mCoeffs[9, 10] = 0;
                    mCoeffs[9, 11] = udtIsoPct132N.Minus1;
                    mCoeffs[9, 12] = 0;
                    mCoeffs[9, 13] = udtIsoPct133N.Minus2;

                    mCoeffs[10, 6] = udtIsoPct129C.Plus2;
                    mCoeffs[10, 7] = 0;
                    mCoeffs[10, 8] = udtIsoPct130C.Plus1;
                    mCoeffs[10, 9] = 0;
                    mCoeffs[10, 10] = udtIsoPct131C.Zero;
                    mCoeffs[10, 11] = 0;
                    mCoeffs[10, 12] = udtIsoPct132C.Minus1;
                    mCoeffs[10, 13] = 0;
                    mCoeffs[10, 14] = udtIsoPct133C.Minus2;

                    mCoeffs[11, 7] = udtIsoPct130N.Plus2;
                    mCoeffs[11, 8] = 0;
                    mCoeffs[11, 9] = udtIsoPct131N.Plus1;
                    mCoeffs[11, 10] = 0;
                    mCoeffs[11, 11] = udtIsoPct132N.Zero;
                    mCoeffs[11, 12] = 0;
                    mCoeffs[11, 13] = udtIsoPct133N.Minus1;
                    mCoeffs[11, 14] = 0;
                    mCoeffs[11, 15] = udtIsoPct134N.Minus2;

                    mCoeffs[12, 8] = udtIsoPct130C.Plus2;
                    mCoeffs[12, 9] = 0;
                    mCoeffs[12, 10] = udtIsoPct131C.Plus1;
                    mCoeffs[12, 11] = 0;
                    mCoeffs[12, 12] = udtIsoPct132C.Zero;
                    mCoeffs[12, 13] = 0;
                    mCoeffs[12, 14] = udtIsoPct133C.Minus1;

                    mCoeffs[13, 9] = udtIsoPct131N.Plus2;
                    mCoeffs[13, 10] = 0;
                    mCoeffs[13, 11] = udtIsoPct132N.Plus1;
                    mCoeffs[13, 12] = 0;
                    mCoeffs[13, 13] = udtIsoPct133N.Zero;
                    mCoeffs[13, 14] = 0;
                    mCoeffs[13, 15] = udtIsoPct134N.Minus1;

                    mCoeffs[14, 10] = udtIsoPct131C.Plus2;
                    mCoeffs[14, 11] = 0;
                    mCoeffs[14, 12] = udtIsoPct132C.Plus1;
                    mCoeffs[14, 13] = 0;
                    mCoeffs[14, 14] = udtIsoPct133C.Zero;

                    mCoeffs[15, 11] = udtIsoPct132N.Plus2;
                    mCoeffs[15, 12] = 0;
                    mCoeffs[15, 13] = udtIsoPct133N.Plus1;
                    mCoeffs[15, 14] = 0;
                    mCoeffs[15, 15] = udtIsoPct134N.Zero;
                    break;

                case ReporterIons.ReporterIonMassModeConstants.TMTEighteenMZ:
                    // 18-plex TMT, isotope contribution table for High Res MS/MS
                    // Source percentages provided by Thermo

                    // ReSharper disable once CommentTypo
                    // TMTpro lot UH290428

                    // Column map
                    // PDF file column   Contribution field
                    // ---------------   ------------------
                    // -2x 13C           minus2
                    //    -13C           minus1
                    //      M+           zero
                    //    +13C           plus1
                    // +2x 13C           plus2

                    udtIsoPct126 = DefineIsotopeContribution(0, 0, 100, 7.73F, 0.22F);
                    udtIsoPct127N = DefineIsotopeContribution(0, 0, 100, 7.46F, 0.22F);
                    udtIsoPct127C = DefineIsotopeContribution(0, 0.71F, 100, 6.62F, 0.16F);
                    udtIsoPct128N = DefineIsotopeContribution(0, 0.75F, 100, 6.67F, 0.16F);
                    udtIsoPct128C = DefineIsotopeContribution(0.06F, 1.34F, 100, 5.31F, 0.11F);
                    udtIsoPct129N = DefineIsotopeContribution(0.01F, 1.29F, 100, 5.48F, 0.1F);
                    udtIsoPct129C = DefineIsotopeContribution(0.26F, 2.34F, 100, 4.87F, 0.08F);
                    udtIsoPct130N = DefineIsotopeContribution(0.39F, 2.36F, 100, 4.57F, 0.07F);
                    udtIsoPct130C = DefineIsotopeContribution(0.05F, 2.67F, 100, 3.85F, 0.15F);
                    udtIsoPct131N = DefineIsotopeContribution(0.05F, 2.71F, 100, 3.73F, 0.04F);
                    udtIsoPct131C = DefineIsotopeContribution(0.09F, 3.69F, 100, 2.77F, 0.01F);
                    udtIsoPct132N = DefineIsotopeContribution(0.09F, 2.51F, 100, 2.76F, 0.01F);
                    udtIsoPct132C = DefineIsotopeContribution(0.1F, 4.11F, 100, 1.63F, 0);
                    udtIsoPct133N = DefineIsotopeContribution(0.09F, 3.09F, 100, 1.58F, 0);
                    udtIsoPct133C = DefineIsotopeContribution(0.36F, 4.63F, 100, 0.88F, 0);
                    udtIsoPct134N = DefineIsotopeContribution(0.38F, 4.82F, 100, 0.86F, 0);
                    udtIsoPct134C = DefineIsotopeContribution(0.14F, 5.81F, 100, 0, 0);
                    udtIsoPct135N = DefineIsotopeContribution(0.19F, 5.42F, 100, 0, 0);

                    // Goal is to generate an 18x18 matrix
                    //       0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17
                    // --  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------
                    // 0   0.9205    0     0.0071    0     0.0006    0       0       0       0       0       0       0       0       0       0       0       0       0
                    // 1     0     0.9232    0     0.0075    0     0.0001    0       0       0       0       0       0       0       0       0       0       0       0
                    // 2   0.0773    0     0.9251    0     0.0134    0     0.0026    0       0       0       0       0       0       0       0       0       0       0
                    // 3     0     0.0746    0     0.9242    0     0.0129    0     0.0039    0       0       0       0       0       0       0       0       0       0
                    // 4   0.0022    0     0.0662    0     0.9318    0     0.0234    0     0.0005    0       0       0       0       0       0       0       0       0
                    // 5     0     0.0022    0     0.0667    0     0.9312    0     0.0236    0     0.0005    0       0       0       0       0       0       0       0
                    // 6     0       0     0.0016    0     0.0531    0     0.9245    0     0.0267    0     0.0009    0       0       0       0       0       0       0
                    // 7     0       0       0     0.0016    0     0.0548    0     0.9261    0     0.0271    0     0.0009    0       0       0       0       0       0
                    // 8     0       0       0       0     0.0011    0     0.0487    0     0.9328    0     0.0369    0     0.0010    0       0       0       0       0
                    // 9     0       0       0       0       0     0.0010    0     0.0457    0     0.9347    0     0.0251    0     0.0009    0       0       0       0
                    // 10    0       0       0       0       0       0     0.0008    0     0.0385    0     0.9344    0     0.0411    0     0.0036    0       0       0
                    // 11    0       0       0       0       0       0       0     0.0007    0     0.0373    0     0.9463    0     0.0309    0     0.0038    0       0
                    // 12    0       0       0       0       0       0       0       0     0.0015    0     0.0277    0     0.9416    0     0.0463    0     0.0014    0
                    // 13    0       0       0       0       0       0       0       0       0     0.0004    0     0.0276    0     0.9524    0     0.0482    0     0.0019
                    // 14    0       0       0       0       0       0       0       0       0       0     0.0001    0     0.0163    0     0.9413    0     0.0581    0
                    // 15    0       0       0       0       0       0       0       0       0       0       0     0.0001    0     0.0158    0     0.9394    0     0.0542
                    // 16    0       0       0       0       0       0       0       0       0       0       0       0       0       0     0.0088    0     0.9405    0
                    // 17    0       0       0       0       0       0       0       0       0       0       0       0       0       0       0     0.0086    0     0.9439

                    mCoeffs = new double[maxIndex + 1, maxIndex + 1];

                    mCoeffs[0, 0] = udtIsoPct126.Zero;
                    mCoeffs[0, 1] = 0;
                    mCoeffs[0, 2] = udtIsoPct127C.Minus1;
                    mCoeffs[0, 3] = 0;
                    mCoeffs[0, 4] = udtIsoPct128C.Minus2;

                    mCoeffs[1, 0] = 0;
                    mCoeffs[1, 1] = udtIsoPct127N.Zero;
                    mCoeffs[1, 2] = 0;
                    mCoeffs[1, 3] = udtIsoPct128N.Minus1;
                    mCoeffs[1, 4] = 0;
                    mCoeffs[1, 5] = udtIsoPct129N.Minus2;

                    mCoeffs[2, 0] = udtIsoPct126.Plus1;
                    mCoeffs[2, 1] = 0;
                    mCoeffs[2, 2] = udtIsoPct127C.Zero;
                    mCoeffs[2, 3] = 0;
                    mCoeffs[2, 4] = udtIsoPct128C.Minus1;
                    mCoeffs[2, 5] = 0;
                    mCoeffs[2, 6] = udtIsoPct129C.Minus2;

                    mCoeffs[3, 0] = 0;
                    mCoeffs[3, 1] = udtIsoPct127N.Plus1;
                    mCoeffs[3, 2] = 0;
                    mCoeffs[3, 3] = udtIsoPct128N.Zero;
                    mCoeffs[3, 4] = 0;
                    mCoeffs[3, 5] = udtIsoPct129N.Minus1;
                    mCoeffs[3, 6] = 0;
                    mCoeffs[3, 7] = udtIsoPct130N.Minus2;

                    mCoeffs[4, 0] = udtIsoPct126.Plus2;
                    mCoeffs[4, 1] = 0;
                    mCoeffs[4, 2] = udtIsoPct127C.Plus1;
                    mCoeffs[4, 3] = 0;
                    mCoeffs[4, 4] = udtIsoPct128C.Zero;
                    mCoeffs[4, 5] = 0;
                    mCoeffs[4, 6] = udtIsoPct129C.Minus1;
                    mCoeffs[4, 7] = 0;
                    mCoeffs[4, 8] = udtIsoPct130C.Minus2;

                    mCoeffs[5, 1] = udtIsoPct127N.Plus2;
                    mCoeffs[5, 2] = 0;
                    mCoeffs[5, 3] = udtIsoPct128N.Plus1;
                    mCoeffs[5, 4] = 0;
                    mCoeffs[5, 5] = udtIsoPct129N.Zero;
                    mCoeffs[5, 6] = 0;
                    mCoeffs[5, 7] = udtIsoPct130N.Minus1;
                    mCoeffs[5, 8] = 0;
                    mCoeffs[5, 9] = udtIsoPct131N.Minus2;

                    mCoeffs[6, 2] = udtIsoPct127C.Plus2;
                    mCoeffs[6, 3] = 0;
                    mCoeffs[6, 4] = udtIsoPct128C.Plus1;
                    mCoeffs[6, 5] = 0;
                    mCoeffs[6, 6] = udtIsoPct129C.Zero;
                    mCoeffs[6, 7] = 0;
                    mCoeffs[6, 8] = udtIsoPct130C.Minus1;
                    mCoeffs[6, 9] = 0;
                    mCoeffs[6, 10] = udtIsoPct131C.Minus2;

                    mCoeffs[7, 3] = udtIsoPct128N.Plus2;
                    mCoeffs[7, 4] = 0;
                    mCoeffs[7, 5] = udtIsoPct129N.Plus1;
                    mCoeffs[7, 6] = 0;
                    mCoeffs[7, 7] = udtIsoPct130N.Zero;
                    mCoeffs[7, 8] = 0;
                    mCoeffs[7, 9] = udtIsoPct131N.Minus1;
                    mCoeffs[7, 10] = 0;
                    mCoeffs[7, 11] = udtIsoPct132N.Minus2;

                    mCoeffs[8, 4] = udtIsoPct128C.Plus2;
                    mCoeffs[8, 5] = 0;
                    mCoeffs[8, 6] = udtIsoPct129C.Plus1;
                    mCoeffs[8, 7] = 0;
                    mCoeffs[8, 8] = udtIsoPct130C.Zero;
                    mCoeffs[8, 9] = 0;
                    mCoeffs[8, 10] = udtIsoPct131C.Minus1;
                    mCoeffs[8, 11] = 0;
                    mCoeffs[8, 12] = udtIsoPct132C.Minus2;

                    mCoeffs[9, 5] = udtIsoPct129N.Plus2;
                    mCoeffs[9, 6] = 0;
                    mCoeffs[9, 7] = udtIsoPct130N.Plus1;
                    mCoeffs[9, 8] = 0;
                    mCoeffs[9, 9] = udtIsoPct131N.Zero;
                    mCoeffs[9, 10] = 0;
                    mCoeffs[9, 11] = udtIsoPct132N.Minus1;
                    mCoeffs[9, 12] = 0;
                    mCoeffs[9, 13] = udtIsoPct133N.Minus2;

                    mCoeffs[10, 6] = udtIsoPct129C.Plus2;
                    mCoeffs[10, 7] = 0;
                    mCoeffs[10, 8] = udtIsoPct130C.Plus1;
                    mCoeffs[10, 9] = 0;
                    mCoeffs[10, 10] = udtIsoPct131C.Zero;
                    mCoeffs[10, 11] = 0;
                    mCoeffs[10, 12] = udtIsoPct132C.Minus1;
                    mCoeffs[10, 13] = 0;
                    mCoeffs[10, 14] = udtIsoPct133C.Minus2;

                    mCoeffs[11, 7] = udtIsoPct130N.Plus2;
                    mCoeffs[11, 8] = 0;
                    mCoeffs[11, 9] = udtIsoPct131N.Plus1;
                    mCoeffs[11, 10] = 0;
                    mCoeffs[11, 11] = udtIsoPct132N.Zero;
                    mCoeffs[11, 12] = 0;
                    mCoeffs[11, 13] = udtIsoPct133N.Minus1;
                    mCoeffs[11, 14] = 0;
                    mCoeffs[11, 15] = udtIsoPct134N.Minus2;

                    mCoeffs[12, 8] = udtIsoPct130C.Plus2;
                    mCoeffs[12, 9] = 0;
                    mCoeffs[12, 10] = udtIsoPct131C.Plus1;
                    mCoeffs[12, 11] = 0;
                    mCoeffs[12, 12] = udtIsoPct132C.Zero;
                    mCoeffs[12, 13] = 0;
                    mCoeffs[12, 14] = udtIsoPct133C.Minus1;
                    mCoeffs[12, 15] = 0;
                    mCoeffs[12, 16] = udtIsoPct134C.Minus2;

                    mCoeffs[13, 9] = udtIsoPct131N.Plus2;
                    mCoeffs[13, 10] = 0;
                    mCoeffs[13, 11] = udtIsoPct132N.Plus1;
                    mCoeffs[13, 12] = 0;
                    mCoeffs[13, 13] = udtIsoPct133N.Zero;
                    mCoeffs[13, 14] = 0;
                    mCoeffs[13, 15] = udtIsoPct134N.Minus1;
                    mCoeffs[13, 16] = 0;
                    mCoeffs[13, 17] = udtIsoPct135N.Minus2;

                    mCoeffs[14, 10] = udtIsoPct131C.Plus2;
                    mCoeffs[14, 11] = 0;
                    mCoeffs[14, 12] = udtIsoPct132C.Plus1;
                    mCoeffs[14, 13] = 0;
                    mCoeffs[14, 14] = udtIsoPct133C.Zero;
                    mCoeffs[14, 15] = 0;
                    mCoeffs[14, 16] = udtIsoPct134C.Minus1;

                    mCoeffs[15, 11] = udtIsoPct132N.Plus2;
                    mCoeffs[15, 12] = 0;
                    mCoeffs[15, 13] = udtIsoPct133N.Plus1;
                    mCoeffs[15, 14] = 0;
                    mCoeffs[15, 15] = udtIsoPct134N.Zero;
                    mCoeffs[15, 16] = 0;
                    mCoeffs[15, 17] = udtIsoPct135N.Minus1;

                    mCoeffs[16, 12] = udtIsoPct132C.Plus2;
                    mCoeffs[16, 13] = 0;
                    mCoeffs[16, 14] = udtIsoPct133C.Plus1;
                    mCoeffs[16, 15] = 0;
                    mCoeffs[16, 16] = udtIsoPct134C.Zero;

                    mCoeffs[17, 13] = udtIsoPct133N.Plus2;
                    mCoeffs[17, 14] = 0;
                    mCoeffs[17, 15] = udtIsoPct134N.Plus1;
                    mCoeffs[17, 16] = 0;
                    mCoeffs[17, 17] = udtIsoPct135N.Zero;

                    break;

                default:
                    throw new Exception("Invalid reporter ion mode in IntensityCorrection.InitializeCoefficients");
            }

            // Now divide all of the weights by 100
            for (var i = 0; i <= maxIndex; i++)
            {
                for (var j = 0; j <= maxIndex; j++)
                {
                    mCoeffs[i, j] /= 100.0;
                }
            }

            if (debugShowMatrixTable)
            {
                // Print out the matrix
                Console.WriteLine();
                Console.WriteLine("Reporter Ion Correction Matrix; mode = " + ReporterIonMode);

                for (var i = 0; i <= maxIndex; i++)
                {
                    if (i == 0)
                    {
                        // Header line
                        Console.Write("     ");

                        for (var j = 0; j <= maxIndex; j++)
                        {
                            if (j < 10)
                            {
                                Console.Write("   " + j + "    ");
                            }
                            else
                            {
                                Console.Write("   " + j + "   ");
                            }
                        }

                        Console.WriteLine();

                        Console.Write("     ");

                        for (var k = 0; k <= maxIndex; k++)
                        {
                            Console.Write(" ------ ");
                        }

                        Console.WriteLine();
                    }

                    string indexSpacer;

                    if (i < 10)
                        indexSpacer = "  ";
                    else
                        indexSpacer = " ";

                    Console.Write("  " + i + indexSpacer);

                    for (var j = 0; j <= maxIndex; j++)
                    {
                        if (Math.Abs(mCoeffs[i, j]) < float.Epsilon)
                        {
                            Console.Write("   0    ");
                        }
                        else
                        {
                            Console.Write(" " + mCoeffs[i, j].ToString("0.0000") + " ");
                        }
                    }

                    Console.WriteLine();
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Given a set of isotope correction values
        /// </summary>
        /// <remarks>The values should sum to 100; however, if zero (aka the Monoisotopic Peak) is 0, its value will be auto-computed</remarks>
        /// <param name="minus2">Value between 0 and 100, but typically close to 0</param>
        /// <param name="minus1">Value between 0 and 100, but typically close to 0</param>
        /// <param name="zero">Value between 0 and 100, but typically close to 98; if this is 0 or 100, it is auto-computed</param>
        /// <param name="plus1">Value between 0 and 100, but typically close to 0</param>
        /// <param name="plus2">Value between 0 and 100, but typically close to 0</param>
        private IsotopeContributionType DefineIsotopeContribution(
            float minus2,
            float minus1,
            float zero,
            float plus1,
            float plus2)
        {
            IsotopeContributionType udtIsotopePct;

            if (Math.Abs(zero) < float.Epsilon ||
                zero < 0 ||
                minus2 + minus1 + plus1 + plus2 > 0 && Math.Abs(zero - 100) < float.Epsilon)
            {
                // Auto-compute the monoisotopic abundance
                zero = 100 - minus2 - minus1 - plus1 - plus2;
            }

            var sum = minus2 + minus1 + zero + plus1 + plus2;

            if (Math.Abs(100 - sum) > 0.05)
            {
                throw new Exception(string.Format("Parameters for DefineIsotopeContribution should add up to 100; current sum is {0:F1}", sum));
            }

            udtIsotopePct.Minus2 = minus2;
            udtIsotopePct.Minus1 = minus1;
            udtIsotopePct.Zero = zero;
            udtIsotopePct.Plus1 = plus1;
            udtIsotopePct.Plus2 = plus2;

            return udtIsotopePct;
        }
    }
}
