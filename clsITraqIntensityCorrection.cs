using System;
using System.Linq;

namespace MASIC
{
    // This class corrects the intensities of iTraq or TMT data, based on the expected overlapping isotopic distributions
    // It supports 4-plex and 8-plex iTraq
    // It also supports TMT10, TMT11, and TMT16 (aka TMTpro)
    //
    // The isotopic distribution weights are provided by the iTraq or TMT manufacturer
    //
    // There are two options for the iTRAQ 4-plex weights:
    // eCorrectionFactorsiTRAQ4Plex.ABSciex
    // eCorrectionFactorsiTRAQ4Plex.BroadInstitute

    public class clsITraqIntensityCorrection
    {
        #region "Constants and Enums"

        private const int FOUR_PLEX_MATRIX_LENGTH = 4;
        private const int EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH = 8;
        private const int EIGHT_PLEX_LOW_RES_MATRIX_LENGTH = 9;
        private const int TEN_PLEX_TMT_MATRIX_LENGTH = 10;
        private const int ELEVEN_PLEX_TMT_MATRIX_LENGTH = 11;
        private const int SIXTEEN_PLEX_TMT_MATRIX_LENGTH = 16;

        public enum eCorrectionFactorsiTRAQ4Plex
        {
            ABSciex = 0,
            BroadInstitute = 1          // Provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)
        }
        #endregion

        #region "Structures"
        private struct udtIsotopeContributionType
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
        #endregion

        #region "Classwide Variables"
        private clsReporterIons.eReporterIonMassModeConstants mReporterIonMode;

        private eCorrectionFactorsiTRAQ4Plex mITraq4PlexCorrectionFactorType;

        // Matrix of coefficients, derived from the isotope contribution table
        private double[,] mCoeffs;

        private readonly MatrixDecompositionUtility.LUDecomposition mMatrixUtility;

        #endregion

        #region "Properties"

        public clsReporterIons.eReporterIonMassModeConstants ReporterIonMode => mReporterIonMode;

        public eCorrectionFactorsiTRAQ4Plex ITraq4PlexCorrectionFactorType => mITraq4PlexCorrectionFactorType;

        #endregion

        /// <summary>
        /// Constructor; assumes iTraqCorrectionFactorType = eCorrectionFactorsiTRAQ4Plex.ABSciex
        /// </summary>
        /// <param name="eReporterIonMode">iTRAQ or TMT mode</param>
        /// <remarks></remarks>
        public clsITraqIntensityCorrection(clsReporterIons.eReporterIonMassModeConstants eReporterIonMode)
            : this(eReporterIonMode, eCorrectionFactorsiTRAQ4Plex.ABSciex)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="eReporterIonMode">iTRAQ or TMT mode</param>
        /// <param name="iTraqCorrectionFactorType">Correction factor type for 4-plex iTRAQ</param>
        /// <remarks>The iTraqCorrectionFactorType parameter is only used if eReporterIonMode is ITraqFourMZ</remarks>
        public clsITraqIntensityCorrection(clsReporterIons.eReporterIonMassModeConstants eReporterIonMode, eCorrectionFactorsiTRAQ4Plex iTraqCorrectionFactorType)
        {
            mReporterIonMode = eReporterIonMode;
            mITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType;

            mMatrixUtility = new MatrixDecompositionUtility.LUDecomposition();

            if (mReporterIonMode == clsReporterIons.eReporterIonMassModeConstants.CustomOrNone)
            {
                return;
            }

            InitializeCoefficients(false);
        }

        /// <summary>
        /// Change the reporter ion mode
        /// </summary>
        /// <param name="eReporterIonMode"></param>
        public void UpdateReporterIonMode(clsReporterIons.eReporterIonMassModeConstants eReporterIonMode)
        {
            UpdateReporterIonMode(eReporterIonMode, mITraq4PlexCorrectionFactorType);
        }

        /// <summary>
        /// Change the reporter ion mode
        /// </summary>
        /// <param name="eReporterIonMode"></param>
        /// <param name="iTraqCorrectionFactorType"></param>
        public void UpdateReporterIonMode(clsReporterIons.eReporterIonMassModeConstants eReporterIonMode, eCorrectionFactorsiTRAQ4Plex iTraqCorrectionFactorType)
        {
            if (mReporterIonMode != eReporterIonMode || mITraq4PlexCorrectionFactorType != iTraqCorrectionFactorType)
            {
                mReporterIonMode = eReporterIonMode;
                mITraq4PlexCorrectionFactorType = iTraqCorrectionFactorType;
                InitializeCoefficients(true);
            }
        }

        /// <summary>
        /// Apply the correction factors to the reporter ions
        /// </summary>
        /// <param name="reporterIonIntensities"></param>
        /// <param name="debugShowIntensities">When true, show the old and new reporter ion intensities at the console</param>
        /// <returns></returns>
        public bool ApplyCorrection(ref float[] reporterIonIntensities, bool debugShowIntensities = false)
        {
            double[] originalIntensities;
            int dataCount = reporterIonIntensities.Count() - 1;

            originalIntensities = new double[dataCount + 1];
            for (int index = 0; index <= dataCount; index++)
                originalIntensities[index] = reporterIonIntensities[index];

            if (ApplyCorrection(originalIntensities, debugShowIntensities))
            {
                for (int index = 0; index <= dataCount; index++)
                    reporterIonIntensities[index] = Convert.ToSingle(originalIntensities[index]);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Apply the correction factors to the reporter ions
        /// </summary>
        /// <param name="reporterIonIntensities"></param>
        /// <param name="debugShowIntensities">When true, show the old and new reporter ion intensities at the console</param>
        /// <returns></returns>
        public bool ApplyCorrection(double[] reporterIonIntensities, bool debugShowIntensities = false)
        {
            int matrixSize = GetMatrixLength(mReporterIonMode);
            string eReporterIonMode = clsReporterIons.GetReporterIonModeDescription(mReporterIonMode);

            if (reporterIonIntensities.Length != matrixSize)
            {
                throw new InvalidOperationException("Length of ReporterIonIntensities array must be " + matrixSize.ToString() +
                                                    " when using the " + eReporterIonMode + " mode");
            }

            var correctedIntensities = mMatrixUtility.ProcessData(mCoeffs, matrixSize, reporterIonIntensities);

            var maxIntensity = default(double);
            for (int index = 0; index <= matrixSize - 1; index++)
                maxIntensity = Math.Max(maxIntensity, reporterIonIntensities[index]);

            if (debugShowIntensities)
            {
                Console.WriteLine();
                Console.WriteLine("{0,-8} {1,-10} {2,-12}  {3}", "Index", "Intensity", "NewIntensity", "% Change");
            }

            // Now update reporterIonIntensities
            for (int index = 0; index <= matrixSize - 1; index++)
            {
                if (reporterIonIntensities[index] > 0)
                {
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
                        double percentChange = (newIntensity - reporterIonIntensities[index]) / maxIntensity * 100;
                        int percentChangeRounded = Convert.ToInt32(Math.Round(percentChange, 0));

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
            }

            return true;
        }

        private int GetMatrixLength(clsReporterIons.eReporterIonMassModeConstants eReporterIonMode)
        {
            switch (eReporterIonMode)
            {
                case clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ:
                    return FOUR_PLEX_MATRIX_LENGTH;
                case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes:
                    return EIGHT_PLEX_HIGH_RES_MATRIX_LENGTH;
                case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes:
                    return EIGHT_PLEX_LOW_RES_MATRIX_LENGTH;
                case clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ:
                    return TEN_PLEX_TMT_MATRIX_LENGTH;
                case clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ:
                    return ELEVEN_PLEX_TMT_MATRIX_LENGTH;
                case clsReporterIons.eReporterIonMassModeConstants.TMTSixteenMZ:
                    return SIXTEEN_PLEX_TMT_MATRIX_LENGTH;
                default:
                    throw new ArgumentOutOfRangeException("Invalid value for eReporterIonMode in GetMatrixLength: " + eReporterIonMode.ToString());
                    break;
            }
        }

        /// <summary>
        /// Initialize the coefficients
        /// </summary>
        /// <param name="debugShowMatrixTable">When true, show a table of the coefficients at the console</param>
        private void InitializeCoefficients(bool debugShowMatrixTable)
        {
            // iTraq reporter ions
            udtIsotopeContributionType udtIsoPct113;
            udtIsotopeContributionType udtIsoPct114;
            udtIsotopeContributionType udtIsoPct115;
            udtIsotopeContributionType udtIsoPct116;
            udtIsotopeContributionType udtIsoPct117;
            udtIsotopeContributionType udtIsoPct118;
            udtIsotopeContributionType udtIsoPct119;
            udtIsotopeContributionType udtIsoPct120;
            udtIsotopeContributionType udtIsoPct121;

            // TMT reporter ions
            udtIsotopeContributionType udtIsoPct126;
            udtIsotopeContributionType udtIsoPct127N;
            udtIsotopeContributionType udtIsoPct127C;
            udtIsotopeContributionType udtIsoPct128N;
            udtIsotopeContributionType udtIsoPct128C;
            udtIsotopeContributionType udtIsoPct129N;
            udtIsotopeContributionType udtIsoPct129C;
            udtIsotopeContributionType udtIsoPct130N;
            udtIsotopeContributionType udtIsoPct130C;
            udtIsotopeContributionType udtIsoPct131N;
            udtIsotopeContributionType udtIsoPct131C;

            udtIsotopeContributionType udtIsoPct132N;
            udtIsotopeContributionType udtIsoPct132C;
            udtIsotopeContributionType udtIsoPct133N;
            udtIsotopeContributionType udtIsoPct133C;
            udtIsotopeContributionType udtIsoPct134N;

            int matrixSize = GetMatrixLength(mReporterIonMode);
            int maxIndex = matrixSize - 1;

            switch (mReporterIonMode)
            {
                case clsReporterIons.eReporterIonMassModeConstants.ITraqFourMZ:
                    if (mITraq4PlexCorrectionFactorType == eCorrectionFactorsiTRAQ4Plex.ABSciex)
                    {
                        // 4-plex ITraq, isotope contribution table
                        // Source percentages provided by Applied Biosystems

                        udtIsoPct114 = this.DefineIsotopeContribution((float)0, (float)1, 92.9F, 5.9F, 0.2F);
                        udtIsoPct115 = this.DefineIsotopeContribution((float)0, (float)2, 92.3F, 5.6F, 0.1F);
                        udtIsoPct116 = this.DefineIsotopeContribution((float)0, (float)3, 92.4F, 4.5F, 0.1F);
                        udtIsoPct117 = this.DefineIsotopeContribution(0.1F, (float)4, 92.3F, 3.5F, 0.1F);
                    }
                    else if (mITraq4PlexCorrectionFactorType == eCorrectionFactorsiTRAQ4Plex.BroadInstitute)
                    {
                        // 4-plex ITraq, isotope contribution table
                        // Source percentages provided by Philipp Mertins at the Broad Institute (pmertins@broadinstitute.org)

                        udtIsoPct114 = this.DefineIsotopeContribution((float)0, (float)0, 95.5F, 4.5F, (float)0);
                        udtIsoPct115 = this.DefineIsotopeContribution((float)0, 0.9F, 94.6F, 4.5F, (float)0);
                        udtIsoPct116 = this.DefineIsotopeContribution((float)0, 0.9F, 95.7F, 3.4F, (float)0);
                        udtIsoPct117 = this.DefineIsotopeContribution((float)0, 1.4F, 98.6F, (float)0, (float)0);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(mITraq4PlexCorrectionFactorType), "Unrecognized value for the iTRAQ 4 plex correction type");
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

                case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZHighRes:
                    // 8-plex ITraq, isotope contribution table for High Res MS/MS
                    // Source percentages provided by Applied Biosystems
                    // Note there is a 2 Da jump between 119 and 121, which is why 7.44 and 0.87 are not included in mCoeffs()

                    udtIsoPct113 = this.DefineIsotopeContribution((float)0, (float)0, 92.89F, 6.89F, 0.22F);
                    udtIsoPct114 = this.DefineIsotopeContribution((float)0, 0.94F, 93.01F, 5.9F, 0.16F);
                    udtIsoPct115 = this.DefineIsotopeContribution((float)0, 1.88F, 93.12F, 4.9F, 0.1F);
                    udtIsoPct116 = this.DefineIsotopeContribution((float)0, 2.82F, 93.21F, 3.9F, 0.07F);
                    udtIsoPct117 = this.DefineIsotopeContribution(0.06F, 3.77F, 93.29F, 2.88F, (float)0);
                    udtIsoPct118 = this.DefineIsotopeContribution(0.09F, 4.71F, 93.32F, 1.88F, (float)0);
                    udtIsoPct119 = this.DefineIsotopeContribution(0.14F, 5.66F, 93.34F, 0.87F, (float)0);
                    udtIsoPct121 = this.DefineIsotopeContribution(0.27F, 7.44F, 92.11F, 0.18F, (float)0);

                    // Goal is to generate this matrix:
                    // 0       1       2       3       4       5       6       7
                    // ------  ------  ------  ------  ------  ------  ------  ------
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

                case clsReporterIons.eReporterIonMassModeConstants.ITraqEightMZLowRes:
                    // 8-plex ITraq, isotope contribution table for Low Res MS/MS

                    // ReSharper disable CommentTypo

                    // Source percentages come from page 664 in:
                    // Vaudel, M., Sickmann, A., and L. Martens. "Peptide and protein quantification: A map of the minefield",
                    // Proteomics 2010, 10, 650-670.

                    // ReSharper restore CommentTypo

                    udtIsoPct113 = this.DefineIsotopeContribution((float)0, (float)0, 92.89F, 6.89F, 0.22F);
                    udtIsoPct114 = this.DefineIsotopeContribution((float)0, 0.94F, 93.01F, 5.9F, 0.16F);
                    udtIsoPct115 = this.DefineIsotopeContribution((float)0, 1.88F, 93.12F, 4.9F, 0.1F);
                    udtIsoPct116 = this.DefineIsotopeContribution((float)0, 2.82F, 93.21F, 3.9F, 0.07F);
                    udtIsoPct117 = this.DefineIsotopeContribution(0.06F, 3.77F, 93.29F, 2.88F, (float)0);
                    udtIsoPct118 = this.DefineIsotopeContribution(0.09F, 4.71F, 93.32F, 1.88F, (float)0);
                    udtIsoPct119 = this.DefineIsotopeContribution(0.14F, 5.66F, 93.34F, 0.87F, (float)0);
                    udtIsoPct120 = this.DefineIsotopeContribution((float)0, (float)0, 91.01F, 8.62F, (float)0);
                    udtIsoPct121 = this.DefineIsotopeContribution(0.27F, 7.44F, 92.11F, 0.18F, (float)0);

                    // Goal is to generate this expanded matrix, which takes Phenylalanine into account
                    // 0       1       2       3       4       5       6       7      8
                    // ------  ------  ------  ------  ------  ------  ------  ------  ------
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

                case clsReporterIons.eReporterIonMassModeConstants.TMTTenMZ:
                case clsReporterIons.eReporterIonMassModeConstants.TMTElevenMZ:
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
                    udtIsoPct126 = this.DefineIsotopeContribution((float)0, (float)0, (float)100, 7.2F, 0.2F);
                    udtIsoPct127N = this.DefineIsotopeContribution((float)0, 0.4F, (float)100, 7.3F, 0.2F);
                    udtIsoPct127C = this.DefineIsotopeContribution((float)0, 0.5F, (float)100, 6.3F, (float)0);
                    udtIsoPct128N = this.DefineIsotopeContribution((float)0, 0.7F, (float)100, 5.7F, (float)0);
                    udtIsoPct128C = this.DefineIsotopeContribution((float)0, 1.4F, (float)100, 5.1F, (float)0);
                    udtIsoPct129N = this.DefineIsotopeContribution((float)0, 2.5F, (float)100, (float)5, (float)0);
                    udtIsoPct129C = this.DefineIsotopeContribution((float)0, 2.3F, (float)100, 4.3F, (float)0);
                    udtIsoPct130N = this.DefineIsotopeContribution((float)0, 2.7F, (float)100, 3.9F, (float)0);
                    udtIsoPct130C = this.DefineIsotopeContribution(0.4F, 2.9F, (float)100, 3.3F, (float)0);
                    udtIsoPct131N = this.DefineIsotopeContribution((float)0, 3.4F, (float)100, 3.3F, (float)0);
                    udtIsoPct131C = this.DefineIsotopeContribution((float)0, 3.7F, (float)100, 2.9F, (float)0);

                    // Goal is to generate this matrix (11-plex will not have the final row or final column)
                    // 0       1       2       3       4       5       6       7       8       9       10
                    // ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------
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

                case clsReporterIons.eReporterIonMassModeConstants.TMTSixteenMZ:
                    // 16-plex TMT, isotope contribution table for High Res MS/MS
                    // Source percentages provided by Thermo

                    // TMTpro lot UH290428
                    udtIsoPct126 = this.DefineIsotopeContribution((float)0, (float)0, (float)100, 7.73F, 0.22F);
                    udtIsoPct127N = this.DefineIsotopeContribution((float)0, (float)0, (float)100, 7.46F, 0.22F);
                    udtIsoPct127C = this.DefineIsotopeContribution((float)0, 0.71F, (float)100, 6.62F, 0.16F);
                    udtIsoPct128N = this.DefineIsotopeContribution((float)0, 0.75F, (float)100, 6.67F, 0.16F);
                    udtIsoPct128C = this.DefineIsotopeContribution(0.06F, 1.34F, (float)100, 5.31F, 0.11F);
                    udtIsoPct129N = this.DefineIsotopeContribution(0.01F, 1.29F, (float)100, 5.48F, 0.1F);
                    udtIsoPct129C = this.DefineIsotopeContribution(0.26F, 2.34F, (float)100, 4.87F, 0.08F);
                    udtIsoPct130N = this.DefineIsotopeContribution(0.39F, 2.36F, (float)100, 4.57F, 0.07F);
                    udtIsoPct130C = this.DefineIsotopeContribution(0.05F, 2.67F, (float)100, 3.85F, 0.15F);
                    udtIsoPct131N = this.DefineIsotopeContribution(0.05F, 2.71F, (float)100, 3.73F, 0.04F);
                    udtIsoPct131C = this.DefineIsotopeContribution(0.09F, 3.69F, (float)100, 2.77F, 0.01F);
                    udtIsoPct132N = this.DefineIsotopeContribution(0.09F, 2.51F, (float)100, 2.76F, 0.01F);
                    udtIsoPct132C = this.DefineIsotopeContribution(0.1F, 4.11F, (float)100, 1.63F, (float)0);
                    udtIsoPct133N = this.DefineIsotopeContribution(0.09F, 3.09F, (float)100, 1.58F, (float)0);
                    udtIsoPct133C = this.DefineIsotopeContribution(0.36F, 4.63F, (float)100, 0.88F, (float)0);
                    udtIsoPct134N = this.DefineIsotopeContribution(0.38F, 4.82F, (float)100, 0.86F, (float)0);

                    // Goal is to generate a 16x16 matrix
                    // 0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15
                    // ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------  ------
                    // 0   0.9260    0     0.0050    0       0       0       0       0       0       0       0     etc.
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
                    // 11    etc.
                    // 12
                    // 13
                    // 14
                    // 15

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
                    mCoeffs[6, 10] = udtIsoPct130C.Minus2;

                    mCoeffs[7, 3] = udtIsoPct128N.Plus2;
                    mCoeffs[7, 4] = 0;
                    mCoeffs[7, 5] = udtIsoPct129N.Plus1;
                    mCoeffs[7, 6] = 0;
                    mCoeffs[7, 7] = udtIsoPct130N.Zero;
                    mCoeffs[7, 8] = 0;
                    mCoeffs[7, 9] = udtIsoPct131N.Minus1;
                    mCoeffs[7, 10] = 0;
                    mCoeffs[7, 11] = udtIsoPct131N.Minus2;

                    mCoeffs[8, 4] = udtIsoPct128C.Plus2;
                    mCoeffs[8, 5] = 0;
                    mCoeffs[8, 6] = udtIsoPct129C.Plus1;
                    mCoeffs[8, 7] = 0;
                    mCoeffs[8, 8] = udtIsoPct130C.Zero;
                    mCoeffs[8, 9] = 0;
                    mCoeffs[8, 10] = udtIsoPct131C.Minus1;
                    mCoeffs[8, 11] = 0;
                    mCoeffs[8, 12] = udtIsoPct131C.Minus2;

                    mCoeffs[9, 5] = udtIsoPct129N.Plus2;
                    mCoeffs[9, 6] = 0;
                    mCoeffs[9, 7] = udtIsoPct130N.Plus1;
                    mCoeffs[9, 8] = 0;
                    mCoeffs[9, 9] = udtIsoPct131N.Zero;
                    mCoeffs[9, 10] = 0;
                    mCoeffs[9, 11] = udtIsoPct131N.Minus1;
                    mCoeffs[9, 12] = 0;
                    mCoeffs[9, 13] = udtIsoPct131N.Minus2;

                    mCoeffs[10, 6] = udtIsoPct129C.Plus2;
                    mCoeffs[10, 7] = 0;
                    mCoeffs[10, 8] = udtIsoPct130C.Plus1;
                    mCoeffs[10, 9] = 0;
                    mCoeffs[10, 10] = udtIsoPct131C.Zero;
                    mCoeffs[10, 11] = 0;
                    mCoeffs[10, 12] = udtIsoPct131C.Minus1;
                    mCoeffs[10, 13] = 0;
                    mCoeffs[10, 14] = udtIsoPct131C.Minus2;

                    mCoeffs[11, 7] = udtIsoPct129C.Plus2;
                    mCoeffs[11, 8] = 0;
                    mCoeffs[11, 9] = udtIsoPct130C.Plus1;
                    mCoeffs[11, 10] = 0;
                    mCoeffs[11, 11] = udtIsoPct131C.Zero;
                    mCoeffs[11, 12] = 0;
                    mCoeffs[11, 13] = udtIsoPct131C.Minus1;
                    mCoeffs[11, 14] = 0;
                    mCoeffs[11, 15] = udtIsoPct131C.Minus2;

                    mCoeffs[12, 8] = udtIsoPct129C.Plus2;
                    mCoeffs[12, 9] = 0;
                    mCoeffs[12, 10] = udtIsoPct130C.Plus1;
                    mCoeffs[12, 11] = 0;
                    mCoeffs[12, 12] = udtIsoPct131C.Zero;
                    mCoeffs[12, 13] = 0;
                    mCoeffs[12, 14] = udtIsoPct131C.Minus1;

                    mCoeffs[13, 9] = udtIsoPct129C.Plus2;
                    mCoeffs[13, 10] = 0;
                    mCoeffs[13, 11] = udtIsoPct130C.Plus1;
                    mCoeffs[13, 12] = 0;
                    mCoeffs[13, 13] = udtIsoPct131C.Zero;
                    mCoeffs[13, 14] = 0;
                    mCoeffs[13, 15] = udtIsoPct131C.Minus1;

                    mCoeffs[14, 10] = udtIsoPct129C.Plus2;
                    mCoeffs[14, 11] = 0;
                    mCoeffs[14, 12] = udtIsoPct130C.Plus1;
                    mCoeffs[14, 13] = 0;
                    mCoeffs[14, 14] = udtIsoPct131C.Zero;

                    mCoeffs[15, 11] = udtIsoPct129C.Plus2;
                    mCoeffs[15, 12] = 0;
                    mCoeffs[15, 13] = udtIsoPct130C.Plus1;
                    mCoeffs[15, 14] = 0;
                    mCoeffs[15, 15] = udtIsoPct131C.Zero;
                    break;

                default:
                    throw new Exception("Invalid reporter ion mode in IntensityCorrection.InitializeCoefficients");
                    break;
            }

            // Now divide all of the weights by 100
            for (int i = 0; i <= maxIndex; i++)
            {
                for (int j = 0; j <= maxIndex; j++)
                    mCoeffs[i, j] /= 100.0;
            }

            if (debugShowMatrixTable)
            {
                // Print out the matrix
                Console.WriteLine();
                Console.WriteLine("Reporter Ion Correction Matrix; mode = " + mReporterIonMode.ToString());
                for (int i = 0; i <= maxIndex; i++)
                {
                    if (i == 0)
                    {
                        // Header line
                        Console.Write("     ");
                        for (int j = 0; j <= maxIndex; j++)
                        {
                            if (j < 10)
                            {
                                Console.Write("   " + j.ToString() + "    ");
                            }
                            else
                            {
                                Console.Write("   " + j.ToString() + "   ");
                            }
                        }

                        Console.WriteLine();

                        Console.Write("     ");
                        for (int k = 0; k <= maxIndex; k++)
                            Console.Write(" ------ ");

                        Console.WriteLine();
                    }

                    string indexSpacer;
                    if (i < 10)
                        indexSpacer = "  ";
                    else
                        indexSpacer = " ";

                    Console.Write("  " + i.ToString() + indexSpacer);
                    for (int j = 0; j <= maxIndex; j++)
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
        /// <param name="minus2">Value between 0 and 100, but typically close to 0</param>
        /// <param name="minus1">Value between 0 and 100, but typically close to 0</param>
        /// <param name="zero">Value between 0 and 100, but typically close to 98; if this is 0 or 100, it is auto-computed</param>
        /// <param name="plus1">Value between 0 and 100, but typically close to 0</param>
        /// <param name="plus2">Value between 0 and 100, but typically close to 0</param>
        /// <returns></returns>
        /// <remarks>The values should sum to 100; however, if zero (aka the Monoisotopic Peak) is 0, its value will be auto-computed</remarks>
        private udtIsotopeContributionType DefineIsotopeContribution(
            float minus2,
            float minus1,
            float zero,
            float plus1,
            float plus2)
        {
            udtIsotopeContributionType udtIsotopePct;

            if (Math.Abs(zero) < float.Epsilon ||
                zero < 0 ||
                minus2 + minus1 + plus1 + plus2 > 0 && Math.Abs(zero - 100) < float.Epsilon)
            {
                // Auto-compute the monoisotopic abundance
                zero = 100 - minus2 - minus1 - plus1 - plus2;
            }

            float sum = minus2 + minus1 + zero + plus1 + plus2;
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
