using System;
using System.Collections.Generic;
using System.Linq;

namespace MASIC.Data
{
    /// <summary>
    /// Container for reporter ions to find
    /// </summary>
    public class ReporterIons
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: acet, acetylated, Acetylation, Alfaro, Amine, Carboxylic, Chengdong, Da, Du, Fracking, ITraq
        // Ignore Spelling: Galnaz, immonium, Lys, MASIC, Merkley, Nakayasu, plex, TMT, Traq. Xu

        // ReSharper restore CommentTypo

        /// <summary>
        /// Default reporter ion tolerance, in Da
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT = 0.5;

        /// <summary>
        /// Default minimum allowed reporter ion tolerance, in Da
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_MINIMUM = 0.001;

        /// <summary>
        /// Default reporter ion tolerance for 8-plex iTRAQ
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES = 0.015;

        /// <summary>
        /// Default reporter ion tolerance for 6-plex TMT reporter ions
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT6 = 0.015;

        /// <summary>
        /// Default reporter ion tolerance for 10-plex TMT reporter ions (also applies to 11-plex)
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT10 = 0.015;

        /// <summary>
        /// Default reporter ion tolerance for 16-plex TMT reporter ions (also applies to 18-plex, 32-plex, and 35-plex TMT)
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT16 = 0.003;

        /// <summary>
        /// Reporter ion mass modes
        /// </summary>
        public enum ReporterIonMassModeConstants
        {
            /// <summary>
            /// Custom / none
            /// </summary>
            CustomOrNone = 0,

            /// <summary>
            /// 4-plex iTRAQ (aka iTRAQ4)
            /// </summary>
            ITraqFourMZ = 1,

            /// <summary>
            /// 3-plex iTRAQ
            /// </summary>
            ITraqETDThreeMZ = 2,

            /// <summary>
            /// 2-plex TMT (aka TMT2)
            /// </summary>
            TMTTwoMZ = 3,

            /// <summary>
            /// 6-plex TMT (aka TMT6)
            /// </summary>
            TMTSixMZ = 4,

            /// <summary>
            /// 8-plex iTRAQ (aka iTRAQ8)
            /// </summary>
            /// <remarks>
            /// This version of 8-plex iTraq should be used when the reporter ion search tolerance is +/-0.03 Da or smaller
            /// </remarks>
            ITraqEightMZHighRes = 5,

            /// <summary>
            /// 8-plex iTRAQ
            /// </summary>
            /// <remarks>
            /// This version of 8-plex iTraq will account for immonium loss from phenylalanine
            /// </remarks>
            ITraqEightMZLowRes = 6,

            /// <summary>
            /// PC Galnaz fragments
            /// </summary>
            PCGalnaz = 7,

            /// <summary>
            /// Heme-C fragments
            /// </summary>
            HemeCFragment = 8,

            /// <summary>
            /// Lys acet fragments
            /// </summary>
            LycAcetFragment = 9,

            /// <summary>
            /// 10-plex TMT (aka TMT10)
            /// </summary>
            /// <remarks>
            /// Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMTTenMZ = 10,

            /// <summary>
            /// O-GlcNAc fragments
            /// </summary>
            OGlcNAc = 11,

            /// <summary>
            /// Fracking amine fragments
            /// </summary>
            FrackingAmine20160217 = 12,

            /// <summary>
            /// FSFA carbonyl fragments
            /// </summary>
            FSFACustomCarbonyl = 13,

            /// <summary>
            /// FSFA carboxylic fragments
            /// </summary>
            FSFACustomCarboxylic = 14,

            /// <summary>
            /// FSFA hydroxyl fragments
            /// </summary>
            FSFACustomHydroxyl = 15,

            /// <summary>
            /// 11-plex TMT (aka TMT11)
            /// </summary>
            /// <remarks>
            /// Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMTElevenMZ = 16,

            /// <summary>
            /// Acetylation fragments
            /// </summary>
            Acetylation = 17,

            // ReSharper disable once CommentTypo

            /// <summary>
            /// 16-plex TMT (aka TMT16 or TMTpro)
            /// </summary>
            /// <remarks>
            /// Several of the reporter ion masses are just 47 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMTSixteenMZ = 18,

            /// <summary>
            /// Native O-GlcNAc fragments
            /// </summary>
            NativeOGlcNAc = 19,

            /// <summary>
            /// 18-plex TMT (aka TMT18)
            /// </summary>
            /// <remarks>
            /// Several of the reporter ion masses are just 47 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMTEighteenMZ = 20,

            /// <summary>
            /// 32-plex TMT (aka TMT32)
            /// </summary>
            /// <remarks>
            /// Several of the reporter ion masses are just 47 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMT32MZ = 21,

            /// <summary>
            /// 35-plex TMT (aka TMT35)
            /// </summary>
            /// <remarks>
            /// Several of the reporter ion masses are just 47 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMT35MZ = 22
        }

        private double mReporterIonToleranceDaDefault;

        private ReporterIonMassModeConstants mReporterIonMassMode;

        /// <summary>
        /// When true, always copy reporter ion data from MS3 spectra to MS2 spectra
        /// When false (the default), only copy if the MS2 data is sparse
        /// </summary>
        /// <remarks>Ignored if UseMS3ReporterIonsForParentMS2Spectra is false</remarks>
        public bool AlwaysUseMS3ReporterIonsForParents { get; set; }

        /// <summary>
        /// When ReporterIonStatsEnabled = True, MZIntensityFilterIgnoreRangeStart and MZIntensityFilterIgnoreRangeEnd
        /// will be populated with the m/z range of the reporter ions being processed
        /// </summary>
        public double MZIntensityFilterIgnoreRangeStart { get; set; }

        /// <summary>
        /// When ReporterIonStatsEnabled = True, MZIntensityFilterIgnoreRangeStart and MZIntensityFilterIgnoreRangeEnd
        /// will be populated with the m/z range of the reporter ions being processed
        /// </summary>
        public double MZIntensityFilterIgnoreRangeEnd { get; set; }

        /// <summary>
        /// List of reporter ions
        /// </summary>
        public SortedList<int, ReporterIonInfo> ReporterIonList { get; }

        /// <summary>
        /// When true,  Look for Reporter Ions in the fragmentation spectra
        /// </summary>
        public bool ReporterIonStatsEnabled { get; set; }

        /// <summary>
        /// When true, correct the reporter ion intensities using the Reporter Ion Intensity Corrector class
        /// </summary>
        public bool ReporterIonApplyAbundanceCorrection { get; set; }

        /// <summary>
        /// Correction factor to use when the reporter ion mass mode is ITraqFourMZ
        /// </summary>
        public ITraqIntensityCorrection.CorrectionFactorsITRAQ4Plex ReporterIonITraq4PlexCorrectionFactorType { get; set; }

        /// <summary>
        /// This is ignored if mReporterIonApplyAbundanceCorrection is False
        /// </summary>
        public bool ReporterIonSaveUncorrectedIntensities { get; set; }

        /// <summary>
        /// Reporter ion mass mode
        /// </summary>
        public ReporterIonMassModeConstants ReporterIonMassMode
        {
            get => mReporterIonMassMode;
            set => SetReporterIonMassMode(value);
        }

        /// <summary>
        /// When true, observed m/z values of the reporter ions will be included in the _ReporterIons.txt file
        /// </summary>
        public bool ReporterIonSaveObservedMasses { get; set; }

        /// <summary>
        /// Default reporter ion tolerance, in Da
        /// </summary>
        public double ReporterIonToleranceDaDefault
        {
            get
            {
                if (mReporterIonToleranceDaDefault < double.Epsilon)
                    mReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT;
                return mReporterIonToleranceDaDefault;
            }
            set
            {
                if (value < double.Epsilon)
                    value = REPORTER_ION_TOLERANCE_DA_DEFAULT;
                mReporterIonToleranceDaDefault = value;
            }
        }

        /// <summary>
        /// When true (the default), copy reporter ion data from MS3 spectra to parent MS2 spectra,
        /// always copying if AlwaysUseMS3ReporterIonsForParents is true, otherwise only copying if sparse
        /// </summary>
        public bool UseMS3ReporterIonsForParentMS2Spectra { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReporterIons()
        {
            ReporterIonList = new SortedList<int, ReporterIonInfo>();
            InitializeReporterIonInfo();
        }

        private static void AddReporterIon(IDictionary<int, ReporterIonInfo> reporterIons, double ionMZ, int ionNumber, bool isContaminantIon = false)
        {
            reporterIons.Add(ionNumber, new ReporterIonInfo(ionMZ, ionNumber, isContaminantIon));
        }

        /// <summary>
        /// Get the default reporter ions, using the default reporter ion m/z tolerance (customized based on reporterIonMassMode)
        /// </summary>
        /// <param name="reporterIonMassMode"></param>
        // ReSharper disable once UnusedMember.Global
        public static SortedList<int, ReporterIonInfo> GetDefaultReporterIons(ReporterIonMassModeConstants reporterIonMassMode)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression

            switch (reporterIonMassMode)
            {
                case ReporterIonMassModeConstants.TMTSixMZ:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT6);

                case ReporterIonMassModeConstants.TMTTenMZ or ReporterIonMassModeConstants.TMTElevenMZ:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT10);

                case ReporterIonMassModeConstants.TMTSixteenMZ or ReporterIonMassModeConstants.TMTEighteenMZ or
                    ReporterIonMassModeConstants.TMT32MZ or ReporterIonMassModeConstants.TMT35MZ:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT16);

                case ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES);

                default:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT);
            }
        }

        /// <summary>
        /// Get the reporter ion m/z values for the given reporter ion mass mode
        /// </summary>
        /// <param name="reporterIonMassMode"></param>
        /// <param name="mzToleranceDa"></param>
        public static SortedList<int, ReporterIonInfo> GetDefaultReporterIons(
            ReporterIonMassModeConstants reporterIonMassMode,
            double mzToleranceDa)
        {
            var reporterIons = new SortedList<int, ReporterIonInfo>(16); // largest count is 16

            switch (reporterIonMassMode)
            {
                case ReporterIonMassModeConstants.ITraqFourMZ:
                    // ITRAQ, aka iTRAQ4
                    AddReporterIon(reporterIons, 114.1112, 1);

                    AddReporterIon(reporterIons, 114.1112, 1);
                    AddReporterIon(reporterIons, 115.1083, 2);
                    AddReporterIon(reporterIons, 116.1116, 3);
                    AddReporterIon(reporterIons, 117.115, 4);
                    break;

                case ReporterIonMassModeConstants.ITraqETDThreeMZ:
                    // ITRAQ ETD tags
                    AddReporterIon(reporterIons, 101.107, 1);
                    AddReporterIon(reporterIons, 102.104, 2);
                    AddReporterIon(reporterIons, 104.1107, 3);
                    break;

                case ReporterIonMassModeConstants.TMTTwoMZ:
                    // TMT duplex Isobaric tags (from Thermo)
                    AddReporterIon(reporterIons, 126.1283, 1);
                    AddReporterIon(reporterIons, 127.1316, 2);
                    break;

                case ReporterIonMassModeConstants.TMTSixMZ:
                    // TMT 6-plex Isobaric tags (from Thermo), aka TMT6
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    //                                                           // Old Values:
                    AddReporterIon(reporterIons, 126.127725, 1);   // 126.1283
                    AddReporterIon(reporterIons, 127.124760, 2);   // 127.1316
                    AddReporterIon(reporterIons, 128.134433, 3);   // 128.135
                    AddReporterIon(reporterIons, 129.131468, 4);   // 129.1383
                    AddReporterIon(reporterIons, 130.141141, 5);   // 130.1417
                    AddReporterIon(reporterIons, 131.138176, 6);   // 131.1387
                    break;

                case ReporterIonMassModeConstants.TMTTenMZ:
                    // TMT 10-plex Isobaric tags (from Thermo), aka TMT10
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    AddReporterIon(reporterIons, 126.127726, 1);   // TMT10–zero
                    AddReporterIon(reporterIons, 127.124761, 2);   // 127N
                    AddReporterIon(reporterIons, 127.131081, 3);   // 127C
                    AddReporterIon(reporterIons, 128.128116, 4);   // 128N
                    AddReporterIon(reporterIons, 128.134436, 5);   // 128C
                    AddReporterIon(reporterIons, 129.131471, 6);   // 129N
                    AddReporterIon(reporterIons, 129.137790, 7);   // 129C
                    AddReporterIon(reporterIons, 130.134825, 8);   // 130N
                    AddReporterIon(reporterIons, 130.141145, 9);   // 130C
                    AddReporterIon(reporterIons, 131.138180, 10);  // 131N
                    break;

                case ReporterIonMassModeConstants.TMTElevenMZ:
                    // TMT 11-plex Isobaric tags (from Thermo), aka TMT11
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    AddReporterIon(reporterIons, 126.127726, 1);   // TMT11–zero
                    AddReporterIon(reporterIons, 127.124761, 2);   // 127N
                    AddReporterIon(reporterIons, 127.131081, 3);   // 127C
                    AddReporterIon(reporterIons, 128.128116, 4);   // 128N
                    AddReporterIon(reporterIons, 128.134436, 5);   // 128C
                    AddReporterIon(reporterIons, 129.131471, 6);   // 129N
                    AddReporterIon(reporterIons, 129.137790, 7);   // 129C
                    AddReporterIon(reporterIons, 130.134825, 8);   // 130N
                    AddReporterIon(reporterIons, 130.141145, 9);   // 130C
                    AddReporterIon(reporterIons, 131.138180, 10);  // 131N
                    AddReporterIon(reporterIons, 131.144499, 11);  // 131C
                    break;

                case ReporterIonMassModeConstants.TMTSixteenMZ:
                    // ReSharper disable once CommentTypo
                    // TMT 16-plex Isobaric tags (from Thermo), aka TMT16 or TMTpro
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    // ReSharper disable once CommentTypo
                    AddReporterIon(reporterIons, 126.127726, 1);   // TMTpro–zero
                    AddReporterIon(reporterIons, 127.124761, 2);   // 127N
                    AddReporterIon(reporterIons, 127.131081, 3);   // 127C
                    AddReporterIon(reporterIons, 128.128116, 4);   // 128N
                    AddReporterIon(reporterIons, 128.134436, 5);   // 128C
                    AddReporterIon(reporterIons, 129.131471, 6);   // 129N
                    AddReporterIon(reporterIons, 129.137791, 7);   // 129C
                    AddReporterIon(reporterIons, 130.134826, 8);   // 130N
                    AddReporterIon(reporterIons, 130.141146, 9);   // 130C
                    AddReporterIon(reporterIons, 131.138181, 10);  // 131N
                    AddReporterIon(reporterIons, 131.144501, 11);  // 131C
                    AddReporterIon(reporterIons, 132.141536, 12);  // 132N
                    AddReporterIon(reporterIons, 132.147856, 13);  // 132C
                    AddReporterIon(reporterIons, 133.144891, 14);  // 133N
                    AddReporterIon(reporterIons, 133.151211, 15);  // 133C
                    AddReporterIon(reporterIons, 134.148246, 16);  // 134N
                    break;

                case ReporterIonMassModeConstants.TMTEighteenMZ:
                    // ReSharper disable once CommentTypo
                    // TMT 18-plex Isobaric tags (from Thermo), aka TMT18
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    AddReporterIon(reporterIons, 126.127726, 1);   // TMTpro–zero
                    AddReporterIon(reporterIons, 127.124761, 2);   // 127N
                    AddReporterIon(reporterIons, 127.131081, 3);   // 127C
                    AddReporterIon(reporterIons, 128.128116, 4);   // 128N
                    AddReporterIon(reporterIons, 128.134436, 5);   // 128C
                    AddReporterIon(reporterIons, 129.131471, 6);   // 129N
                    AddReporterIon(reporterIons, 129.137791, 7);   // 129C
                    AddReporterIon(reporterIons, 130.134826, 8);   // 130N
                    AddReporterIon(reporterIons, 130.141146, 9);   // 130C
                    AddReporterIon(reporterIons, 131.138181, 10);  // 131N
                    AddReporterIon(reporterIons, 131.144501, 11);  // 131C
                    AddReporterIon(reporterIons, 132.141536, 12);  // 132N
                    AddReporterIon(reporterIons, 132.147856, 13);  // 132C
                    AddReporterIon(reporterIons, 133.144891, 14);  // 133N
                    AddReporterIon(reporterIons, 133.151211, 15);  // 133C
                    AddReporterIon(reporterIons, 134.148246, 16);  // 134N
                    AddReporterIon(reporterIons, 134.154566, 17);  // 134C
                    AddReporterIon(reporterIons, 135.151601, 18);  // 135N
                    break;

                case ReporterIonMassModeConstants.TMT32MZ:
                    // ReSharper disable once CommentTypo
                    // TMT 18-plex Isobaric tags (from Thermo), aka TMT18
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    AddReporterIon(reporterIons, 126.127726, 1);   // TMTpro–zero
                    AddReporterIon(reporterIons, 127.124761, 2);   // 127N
                    AddReporterIon(reporterIons, 127.131081, 3);   // 127C
                    AddReporterIon(reporterIons, 128.128116, 4);   // 128N
                    AddReporterIon(reporterIons, 128.134436, 5);   // 128C
                    AddReporterIon(reporterIons, 129.131471, 6);   // 129N
                    AddReporterIon(reporterIons, 129.137791, 7);   // 129C
                    AddReporterIon(reporterIons, 130.134826, 8);   // 130N
                    AddReporterIon(reporterIons, 130.141146, 9);   // 130C
                    AddReporterIon(reporterIons, 131.138181, 10);  // 131N
                    AddReporterIon(reporterIons, 131.144501, 11);  // 131C
                    AddReporterIon(reporterIons, 132.141536, 12);  // 132N
                    AddReporterIon(reporterIons, 132.147856, 13);  // 132C
                    AddReporterIon(reporterIons, 133.144891, 14);  // 133N
                    AddReporterIon(reporterIons, 133.151211, 15);  // 133C
                    AddReporterIon(reporterIons, 134.148246, 16);  // 134N
                    AddReporterIon(reporterIons, 127.134003, 17);  // 127D
                    AddReporterIon(reporterIons, 128.131038, 18);  // 128ND
                    AddReporterIon(reporterIons, 128.137358, 19);  // 128CD
                    AddReporterIon(reporterIons, 129.134393, 20);  // 129ND
                    AddReporterIon(reporterIons, 129.140713, 21);  // 129CD
                    AddReporterIon(reporterIons, 130.137748, 22);  // 130ND
                    AddReporterIon(reporterIons, 130.144068, 23);  // 130CD
                    AddReporterIon(reporterIons, 131.141103, 24);  // 131ND
                    AddReporterIon(reporterIons, 131.147423, 25);  // 131CD
                    AddReporterIon(reporterIons, 132.144458, 26);  // 132ND
                    AddReporterIon(reporterIons, 132.150778, 27);  // 132CD
                    AddReporterIon(reporterIons, 133.147813, 28);  // 133ND
                    AddReporterIon(reporterIons, 133.154133, 29);  // 133CD
                    AddReporterIon(reporterIons, 134.151171, 30);  // 134ND
                    AddReporterIon(reporterIons, 134.157491, 31);  // 134CD
                    AddReporterIon(reporterIons, 135.154526, 32);  // 135ND
                    break;

                case ReporterIonMassModeConstants.TMT35MZ:
                    // ReSharper disable once CommentTypo
                    // TMT 18-plex Isobaric tags (from Thermo), aka TMT18
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    AddReporterIon(reporterIons, 126.127726, 1);   // TMTpro–zero
                    AddReporterIon(reporterIons, 127.124761, 2);   // 127N
                    AddReporterIon(reporterIons, 127.131081, 3);   // 127C
                    AddReporterIon(reporterIons, 128.128116, 4);   // 128N
                    AddReporterIon(reporterIons, 128.134436, 5);   // 128C
                    AddReporterIon(reporterIons, 129.131471, 6);   // 129N
                    AddReporterIon(reporterIons, 129.137791, 7);   // 129C
                    AddReporterIon(reporterIons, 130.134826, 8);   // 130N
                    AddReporterIon(reporterIons, 130.141146, 9);   // 130C
                    AddReporterIon(reporterIons, 131.138181, 10);  // 131N
                    AddReporterIon(reporterIons, 131.144501, 11);  // 131C
                    AddReporterIon(reporterIons, 132.141536, 12);  // 132N
                    AddReporterIon(reporterIons, 132.147856, 13);  // 132C
                    AddReporterIon(reporterIons, 133.144891, 14);  // 133N
                    AddReporterIon(reporterIons, 133.151211, 15);  // 133C
                    AddReporterIon(reporterIons, 134.148246, 16);  // 134N
                    AddReporterIon(reporterIons, 134.154566, 17);  // 134C
                    AddReporterIon(reporterIons, 135.151601, 18);  // 135N
                    AddReporterIon(reporterIons, 127.134003, 19);  // 127D
                    AddReporterIon(reporterIons, 128.131038, 20);  // 128ND
                    AddReporterIon(reporterIons, 128.137358, 21);  // 128CD
                    AddReporterIon(reporterIons, 129.134393, 22);  // 129ND
                    AddReporterIon(reporterIons, 129.140713, 23);  // 129CD
                    AddReporterIon(reporterIons, 130.137748, 24);  // 130ND
                    AddReporterIon(reporterIons, 130.144068, 25);  // 130CD
                    AddReporterIon(reporterIons, 131.141103, 26);  // 131ND
                    AddReporterIon(reporterIons, 131.147423, 27);  // 131CD
                    AddReporterIon(reporterIons, 132.144458, 28);  // 132ND
                    AddReporterIon(reporterIons, 132.150778, 29);  // 132CD
                    AddReporterIon(reporterIons, 133.147813, 30);  // 133ND
                    AddReporterIon(reporterIons, 133.154133, 31);  // 133CD
                    AddReporterIon(reporterIons, 134.151171, 32);  // 134ND
                    AddReporterIon(reporterIons, 134.157491, 33);  // 134CD
                    AddReporterIon(reporterIons, 135.154526, 34);  // 135ND
                    AddReporterIon(reporterIons, 135.160846, 35);  // 135CD
                    break;

                case ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    // ITRAQ eight-plex Isobaric tags (iTRAQ8), High-Res MS/MS
                    AddReporterIon(reporterIons, 113.107873, 1);
                    AddReporterIon(reporterIons, 114.111228, 2);
                    AddReporterIon(reporterIons, 115.108263, 3);
                    AddReporterIon(reporterIons, 116.111618, 4);
                    AddReporterIon(reporterIons, 117.114973, 5);
                    AddReporterIon(reporterIons, 118.112008, 6);
                    AddReporterIon(reporterIons, 119.115363, 7);
                    AddReporterIon(reporterIons, 121.122072, 8);
                    break;

                case ReporterIonMassModeConstants.ITraqEightMZLowRes:
                    // ITRAQ eight-plex Isobaric tags (iTRAQ8), Low-Res MS/MS
                    AddReporterIon(reporterIons, 113.107873, 1);
                    AddReporterIon(reporterIons, 114.111228, 2);
                    AddReporterIon(reporterIons, 115.108263, 3);
                    AddReporterIon(reporterIons, 116.111618, 4);
                    AddReporterIon(reporterIons, 117.114973, 5);
                    AddReporterIon(reporterIons, 118.112008, 6);
                    AddReporterIon(reporterIons, 119.115363, 7);

                    // This corresponds to immonium ion loss from Phenylalanine (147.06841 - 26.9871 since Immonium is CO minus H)
                    AddReporterIon(reporterIons, 120.08131, 8, true);

                    AddReporterIon(reporterIons, 121.122072, 9);
                    break;

                case ReporterIonMassModeConstants.PCGalnaz:
                    // ReSharper disable CommentTypo

                    // Custom reporter ions for Josh Alfaro
                    AddReporterIon(reporterIons, 204.0871934, 1);  // C8H14NO5
                    AddReporterIon(reporterIons, 300.130787, 2);   // C11H18N5O5
                    AddReporterIon(reporterIons, 503.2101566, 3);  // C19H31N6O10
                    break;

                case ReporterIonMassModeConstants.HemeCFragment:
                    // Custom reporter ions for Eric Merkley
                    AddReporterIon(reporterIons, 616.1767, 1);
                    AddReporterIon(reporterIons, 617.1845, 2);
                    break;

                case ReporterIonMassModeConstants.LycAcetFragment:
                    // Custom reporter ions for Ernesto Nakayasu
                    AddReporterIon(reporterIons, 126.09134, 1);
                    AddReporterIon(reporterIons, 127.094695, 2);
                    break;

                case ReporterIonMassModeConstants.OGlcNAc:
                    // O-GlcNAc
                    AddReporterIon(reporterIons, 204.0872, 1);
                    AddReporterIon(reporterIons, 300.13079, 2);
                    AddReporterIon(reporterIons, 503.21017, 3);
                    break;

                case ReporterIonMassModeConstants.FrackingAmine20160217:
                    // Product ions associated with FrackingFluid_amine_1_02172016
                    AddReporterIon(reporterIons, 157.089, 1);
                    AddReporterIon(reporterIons, 170.097, 2);
                    AddReporterIon(reporterIons, 234.059, 3);
                    break;

                case ReporterIonMassModeConstants.FSFACustomCarbonyl:
                    // Custom product ions from Chengdong Xu
                    AddReporterIon(reporterIons, 171.104, 1);
                    AddReporterIon(reporterIons, 236.074, 2);
                    AddReporterIon(reporterIons, 257.088, 3);
                    break;

                case ReporterIonMassModeConstants.FSFACustomCarboxylic:
                    // Custom product ions from Chengdong Xu
                    AddReporterIon(reporterIons, 171.104, 1);
                    AddReporterIon(reporterIons, 234.058, 2);
                    AddReporterIon(reporterIons, 336.174, 3);
                    break;

                case ReporterIonMassModeConstants.FSFACustomHydroxyl:
                    // Custom product ions from Chengdong Xu
                    AddReporterIon(reporterIons, 151.063, 1);
                    AddReporterIon(reporterIons, 166.087, 2);
                    break;

                // ReSharper restore CommentTypo

                case ReporterIonMassModeConstants.Acetylation:
                    // Reporter ions for peptides with acetylated lysine residues
                    AddReporterIon(reporterIons, 126.09134, 1);
                    AddReporterIon(reporterIons, 143.11789, 2);
                    break;

                case ReporterIonMassModeConstants.NativeOGlcNAc:
                    // Native O-GlcNAc
                    AddReporterIon(reporterIons, 126.055, 1);
                    AddReporterIon(reporterIons, 138.055, 2);
                    AddReporterIon(reporterIons, 144.065, 3);
                    AddReporterIon(reporterIons, 168.066, 4);
                    AddReporterIon(reporterIons, 186.076, 5);
                    AddReporterIon(reporterIons, 204.087, 6);
                    AddReporterIon(reporterIons, 366.14, 7);
                    break;

                default:
                    // Includes ReporterIonMassModeConstants.CustomOrNone
                    reporterIons.Clear();
                    break;
            }

            foreach (var reporterIon in reporterIons)
            {
                reporterIon.Value.MZToleranceDa = mzToleranceDa;
            }

            return reporterIons;
        }

        /// <summary>
        /// Get a description of the given reporter ion mode
        /// </summary>
        /// <param name="reporterIonMode"></param>
        public static string GetReporterIonModeDescription(ReporterIonMassModeConstants reporterIonMode)
        {
            return reporterIonMode switch
            {
                ReporterIonMassModeConstants.CustomOrNone => "Custom/None",
                ReporterIonMassModeConstants.ITraqFourMZ => "4-plex iTraq",
                ReporterIonMassModeConstants.ITraqETDThreeMZ => "3-plex ETD iTraq",
                ReporterIonMassModeConstants.TMTTwoMZ => "2-plex TMT",
                ReporterIonMassModeConstants.TMTSixMZ => "6-plex TMT",
                ReporterIonMassModeConstants.TMTTenMZ => "10-plex TMT",
                ReporterIonMassModeConstants.TMTElevenMZ => "11-plex TMT",
                // ReSharper disable StringLiteralTypo
                ReporterIonMassModeConstants.TMTSixteenMZ => "16-plex TMT (aka TMTpro)",
                ReporterIonMassModeConstants.TMTEighteenMZ => "18-plex TMT (aka TMTpro-18)",
                ReporterIonMassModeConstants.TMT32MZ => "32-plex TMT (aka TMTpro-32)",
                ReporterIonMassModeConstants.TMT35MZ => "35-plex TMT (aka TMTpro-35)",
                // ReSharper restore StringLiteralTypo
                ReporterIonMassModeConstants.ITraqEightMZHighRes => "8-plex iTraq (High Res MS/MS)",
                ReporterIonMassModeConstants.ITraqEightMZLowRes => "8-plex iTraq (Low Res MS/MS)",
                ReporterIonMassModeConstants.PCGalnaz => "PCGalnaz (300.13 m/z and 503.21 m/z)",
                ReporterIonMassModeConstants.HemeCFragment => "Heme C (616.18 m/z and 616.19 m/z)",
                ReporterIonMassModeConstants.LycAcetFragment => "Lys Acet (126.091 m/z and 127.095 m/z)",
                ReporterIonMassModeConstants.OGlcNAc => "O-GlcNAc (204.087, 300.13, and 503.21 m/z)",
                ReporterIonMassModeConstants.NativeOGlcNAc => "Native O-GlcNAc (126.055, 138.055, 144.065, 168.066, 186.076, 204.087, and 366.14 m/z)",
                ReporterIonMassModeConstants.FrackingAmine20160217 => "Fracking Amine 20160217 (157.089, 170.097, and 234.059 m/z)",
                ReporterIonMassModeConstants.FSFACustomCarbonyl => "FSFA Custom Carbonyl (171.104, 236.074, 157.088 m/z)",
                ReporterIonMassModeConstants.FSFACustomCarboxylic => "FSFA Custom Carboxylic (171.104, 234.058, 336.174 m/z)",
                ReporterIonMassModeConstants.FSFACustomHydroxyl => "FSFA Custom Hydroxyl (151.063 and 166.087 m/z)",
                ReporterIonMassModeConstants.Acetylation => "Acetylated K (126.091 and 143.118 m/z)",
                _ => throw new ArgumentOutOfRangeException(nameof(reporterIonMode), reporterIonMode, null)
            };
        }

        private void InitializeReporterIonInfo()
        {
            ReporterIonList.Clear();

            SetReporterIonMassMode(ReporterIonMassModeConstants.CustomOrNone);

            ReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT;
            ReporterIonApplyAbundanceCorrection = true;
            ReporterIonITraq4PlexCorrectionFactorType = ITraqIntensityCorrection.CorrectionFactorsITRAQ4Plex.ABSciex;

            ReporterIonSaveObservedMasses = false;
            ReporterIonSaveUncorrectedIntensities = false;

            UseMS3ReporterIonsForParentMS2Spectra = true;
            AlwaysUseMS3ReporterIonsForParents = false;
        }

        /// <summary>
        /// Set the reporter ion mass mode
        /// </summary>
        /// <param name="reporterIonMassMode"></param>
        public void SetReporterIonMassMode(ReporterIonMassModeConstants reporterIonMassMode)
        {
            switch (reporterIonMassMode)
            {
                case ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    SetReporterIonMassMode(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES);
                    break;

                case ReporterIonMassModeConstants.TMTSixMZ:
                    SetReporterIonMassMode(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT6);
                    break;

                case ReporterIonMassModeConstants.TMTTenMZ or ReporterIonMassModeConstants.TMTElevenMZ:
                    SetReporterIonMassMode(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT10);
                    break;

                case ReporterIonMassModeConstants.TMTSixteenMZ or ReporterIonMassModeConstants.TMTEighteenMZ or
                    ReporterIonMassModeConstants.TMT32MZ or ReporterIonMassModeConstants.TMT35MZ:
                    SetReporterIonMassMode(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT16);
                    break;

                default:
                    SetReporterIonMassMode(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT);
                    break;
            }
        }

        /// <summary>
        /// Set the reporter ion mass mode
        /// </summary>
        /// <param name="reporterIonMassMode"></param>
        /// <param name="mzToleranceDa"></param>
        public void SetReporterIonMassMode(
            ReporterIonMassModeConstants reporterIonMassMode,
            double mzToleranceDa)
        {
            // Note: If reporterIonMassMode = ReporterIonMassModeConstants.CustomOrNone then nothing is changed

            if (reporterIonMassMode != ReporterIonMassModeConstants.CustomOrNone)
            {
                ReporterIonToleranceDaDefault = mzToleranceDa;

                var reporterIonInfo = GetDefaultReporterIons(reporterIonMassMode, mzToleranceDa);

                SetReporterIons(reporterIonInfo, false);
                mReporterIonMassMode = reporterIonMassMode;
            }
        }

        /// <summary>
        /// Define reporter ions to search for
        /// </summary>
        /// <param name="reporterIons"></param>
        /// <param name="customReporterIons"></param>
        public void SetReporterIons(
            SortedList<int, ReporterIonInfo> reporterIons,
            bool customReporterIons)
        {
            ReporterIonList.Clear();

            if (reporterIons == null || reporterIons.Count == 0)
            {
                return;
            }
            ReporterIonList.Capacity = reporterIons.Count;

            foreach (var reporterIon in reporterIons)
            {
                if (reporterIon.Value.MZToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM)
                {
                    reporterIon.Value.MZToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM;
                }

                ReporterIonList.Add(reporterIon.Key, reporterIon.Value);
            }

            if (customReporterIons)
            {
                mReporterIonMassMode = ReporterIonMassModeConstants.CustomOrNone;
            }
        }

        /// <summary>
        /// Define reporter ions to search for
        /// </summary>
        /// <remarks>Will use REPORTER_ION_TOLERANCE_DA_DEFAULT for the search tolerance</remarks>
        /// <param name="reporterIonMZList"></param>
        // ReSharper disable once UnusedMember.Global
        public void SetReporterIons(
            double[] reporterIonMZList)
        {
            SetReporterIons(reporterIonMZList, REPORTER_ION_TOLERANCE_DA_DEFAULT);
        }

        /// <summary>
        /// Define reporter ions to search for
        /// </summary>
        /// <param name="reporterIonMZList"></param>
        /// <param name="mzToleranceDa">Search tolerance (half width)</param>
        public void SetReporterIons(
            double[] reporterIonMZList,
            double mzToleranceDa)
        {
            SetReporterIons(reporterIonMZList, mzToleranceDa, true);
        }

        /// <summary>
        /// Define reporter ions to search for
        /// </summary>
        /// <remarks>When customReportIons is true, sets mReporterIonMassMode to ReporterIonMassModeConstants.CustomOrNone</remarks>
        /// <param name="reporterIonMZList"></param>
        /// <param name="mzToleranceDa">Search tolerance (half width); must be 0.001 or larger</param>
        /// <param name="customReporterIons">True if these are custom reporter ions</param>
        public void SetReporterIons(
            double[] reporterIonMZList,
            double mzToleranceDa,
            bool customReporterIons)
        {
            if (mzToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM)
            {
                mzToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM;
            }

            ReporterIonList.Clear();

            if (reporterIonMZList == null || reporterIonMZList.Length == 0)
            {
                mReporterIonMassMode = ReporterIonMassModeConstants.CustomOrNone;
                return;
            }

            var ionNumber = 0;

            foreach (var reporterIonMZ in reporterIonMZList)
            {
                ionNumber++;

                var newReporterIon = new ReporterIonInfo(reporterIonMZ, ionNumber)
                {
                    MZToleranceDa = mzToleranceDa
                };

                ReporterIonList.Add(ionNumber, newReporterIon);
            }

            if (customReporterIons)
            {
                mReporterIonMassMode = ReporterIonMassModeConstants.CustomOrNone;
            }
        }

        /// <summary>
        /// Look at the m/z values in ReporterIonList to determine the minimum and maximum m/z values
        /// Update MZIntensityFilterIgnoreRangeStart and MZIntensityFilterIgnoreRangeEnd to be
        /// 2x .MZToleranceDa away from the minimum and maximum
        /// </summary>
        public void UpdateMZIntensityFilterIgnoreRange()
        {
            if (!ReporterIonStatsEnabled || ReporterIonList.Count == 0)
            {
                MZIntensityFilterIgnoreRangeStart = 0;
                MZIntensityFilterIgnoreRangeEnd = 0;
                return;
            }

            MZIntensityFilterIgnoreRangeStart = ReporterIonList.First().Value.MZ - ReporterIonList.First().Value.MZToleranceDa * 2;
            MZIntensityFilterIgnoreRangeEnd = ReporterIonList.First().Value.MZ + ReporterIonList.First().Value.MZToleranceDa * 2;

            foreach (var reporterIon in ReporterIonList)
            {
                var mzStart = reporterIon.Value.MZ - reporterIon.Value.MZToleranceDa * 2;
                MZIntensityFilterIgnoreRangeStart = Math.Min(MZIntensityFilterIgnoreRangeStart, mzStart);

                var mzEnd = reporterIon.Value.MZ + reporterIon.Value.MZToleranceDa * 2;
                MZIntensityFilterIgnoreRangeEnd = Math.Max(MZIntensityFilterIgnoreRangeEnd, mzEnd);
            }
        }
    }
}
