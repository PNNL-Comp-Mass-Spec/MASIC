using System;
using System.Collections.Generic;

namespace MASIC.Data
{
    /// <summary>
    /// Container for reporter ions to find
    /// </summary>
    public class ReporterIons
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: acet, acetylated, Acetylation, Alfaro, Amine, Carboxylic, Chengdong, Da, Du, Fracking
        // Ignore Spelling: Galnaz, immonium, Lys, Merkley, Nakayasu, plex, Xu

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
        /// Default reporter ion tolerance for 10-plex TMT reporter ions (also applies to 11-plex ans 16-plex TMT)
        /// </summary>
        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT10 = 0.015;

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
            /// Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
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
            /// Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            /// </remarks>
            TMTEighteenMZ = 20
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
        public List<ReporterIonInfo> ReporterIonList { get; }

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
        public ITraqIntensityCorrection.CorrectionFactorsiTRAQ4Plex ReporterIonITraq4PlexCorrectionFactorType { get; set; }

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
            ReporterIonList = new List<ReporterIonInfo>();
            InitializeReporterIonInfo();
        }

        /// <summary>
        /// Get the default reporter ions, using the default reporter ion m/z tolerance (customized based on reporterIonMassMode)
        /// </summary>
        /// <param name="reporterIonMassMode"></param>
        // ReSharper disable once UnusedMember.Global
        public static List<ReporterIonInfo> GetDefaultReporterIons(ReporterIonMassModeConstants reporterIonMassMode)
        {
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression

            switch (reporterIonMassMode)
            {
                case ReporterIonMassModeConstants.TMTSixMZ:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT6);

                case ReporterIonMassModeConstants.TMTTenMZ or ReporterIonMassModeConstants.TMTElevenMZ or
                     ReporterIonMassModeConstants.TMTSixteenMZ or ReporterIonMassModeConstants.TMTEighteenMZ:
                    return GetDefaultReporterIons(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT10);

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
        public static List<ReporterIonInfo> GetDefaultReporterIons(
            ReporterIonMassModeConstants reporterIonMassMode,
            double mzToleranceDa)
        {
            var reporterIons = new List<ReporterIonInfo>(16); // largest count is 16

            switch (reporterIonMassMode)
            {
                case ReporterIonMassModeConstants.ITraqFourMZ:
                    // ITRAQ, aka iTRAQ4
                    reporterIons.Add(new ReporterIonInfo(114.1112));
                    reporterIons.Add(new ReporterIonInfo(115.1083));
                    reporterIons.Add(new ReporterIonInfo(116.1116));
                    reporterIons.Add(new ReporterIonInfo(117.115));
                    break;

                case ReporterIonMassModeConstants.ITraqETDThreeMZ:
                    // ITRAQ ETD tags
                    reporterIons.Add(new ReporterIonInfo(101.107));
                    reporterIons.Add(new ReporterIonInfo(102.104));
                    reporterIons.Add(new ReporterIonInfo(104.1107));
                    break;

                case ReporterIonMassModeConstants.TMTTwoMZ:
                    // TMT duplex Isobaric tags (from Thermo)
                    reporterIons.Add(new ReporterIonInfo(126.1283));
                    reporterIons.Add(new ReporterIonInfo(127.1316));
                    break;

                case ReporterIonMassModeConstants.TMTSixMZ:
                    // TMT 6-plex Isobaric tags (from Thermo), aka TMT6
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // ' Old values:
                    reporterIons.Add(new ReporterIonInfo(126.127725));        // 126.1283
                    reporterIons.Add(new ReporterIonInfo(127.12476));         // 127.1316
                    reporterIons.Add(new ReporterIonInfo(128.134433));        // 128.135
                    reporterIons.Add(new ReporterIonInfo(129.131468));        // 129.1383
                    reporterIons.Add(new ReporterIonInfo(130.141141));        // 130.1417
                    reporterIons.Add(new ReporterIonInfo(131.138176));        // 131.1387
                    break;

                case ReporterIonMassModeConstants.TMTTenMZ:
                    // TMT 10-plex Isobaric tags (from Thermo), aka TMT10
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new ReporterIonInfo(126.127726));
                    reporterIons.Add(new ReporterIonInfo(127.124761));        // 127N
                    reporterIons.Add(new ReporterIonInfo(127.131081));        // 127C
                    reporterIons.Add(new ReporterIonInfo(128.128116));        // 128N
                    reporterIons.Add(new ReporterIonInfo(128.134436));        // 128C
                    reporterIons.Add(new ReporterIonInfo(129.131471));        // 129N
                    reporterIons.Add(new ReporterIonInfo(129.137790));        // 129C
                    reporterIons.Add(new ReporterIonInfo(130.134825));        // 130N
                    reporterIons.Add(new ReporterIonInfo(130.141145));        // 130C
                    reporterIons.Add(new ReporterIonInfo(131.138180));        // 131N
                    break;

                case ReporterIonMassModeConstants.TMTElevenMZ:
                    // TMT 11-plex Isobaric tags (from Thermo), aka TMT11
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new ReporterIonInfo(126.127726));        //
                    reporterIons.Add(new ReporterIonInfo(127.124761));        // 127N
                    reporterIons.Add(new ReporterIonInfo(127.131081));        // 127C
                    reporterIons.Add(new ReporterIonInfo(128.128116));        // 128N
                    reporterIons.Add(new ReporterIonInfo(128.134436));        // 128C
                    reporterIons.Add(new ReporterIonInfo(129.131471));        // 129N
                    reporterIons.Add(new ReporterIonInfo(129.137790));        // 129C
                    reporterIons.Add(new ReporterIonInfo(130.134825));        // 130N
                    reporterIons.Add(new ReporterIonInfo(130.141145));        // 130C
                    reporterIons.Add(new ReporterIonInfo(131.138180));        // 131N
                    reporterIons.Add(new ReporterIonInfo(131.144499));        // 131C
                    break;

                case ReporterIonMassModeConstants.TMTSixteenMZ:
                    // ReSharper disable once CommentTypo
                    // TMT 16-plex Isobaric tags (from Thermo), aka TMT16 or TMTpro
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new ReporterIonInfo(126.127726));        //
                    reporterIons.Add(new ReporterIonInfo(127.124761));        // 127N
                    reporterIons.Add(new ReporterIonInfo(127.131081));        // 127C
                    reporterIons.Add(new ReporterIonInfo(128.128116));        // 128N
                    reporterIons.Add(new ReporterIonInfo(128.134436));        // 128C
                    reporterIons.Add(new ReporterIonInfo(129.131471));        // 129N
                    reporterIons.Add(new ReporterIonInfo(129.13779));         // 129C
                    reporterIons.Add(new ReporterIonInfo(130.134825));        // 130N
                    reporterIons.Add(new ReporterIonInfo(130.141145));        // 130C
                    reporterIons.Add(new ReporterIonInfo(131.13818));         // 131N
                    reporterIons.Add(new ReporterIonInfo(131.144499));        // 131C
                    reporterIons.Add(new ReporterIonInfo(132.141535));        // 132N
                    reporterIons.Add(new ReporterIonInfo(132.147855));        // 132C
                    reporterIons.Add(new ReporterIonInfo(133.14489));         // 133N
                    reporterIons.Add(new ReporterIonInfo(133.15121));         // 133C
                    reporterIons.Add(new ReporterIonInfo(134.148245));        // 134N
                    break;

                case ReporterIonMassModeConstants.TMTEighteenMZ:
                    // ReSharper disable once CommentTypo
                    // TMT 18-plex Isobaric tags (from Thermo), aka TMT18
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new ReporterIonInfo(126.127726)); //
                    reporterIons.Add(new ReporterIonInfo(127.124761)); // 127N
                    reporterIons.Add(new ReporterIonInfo(127.131081)); // 127C
                    reporterIons.Add(new ReporterIonInfo(128.128116)); // 128N
                    reporterIons.Add(new ReporterIonInfo(128.134436)); // 128C
                    reporterIons.Add(new ReporterIonInfo(129.131471)); // 129N
                    reporterIons.Add(new ReporterIonInfo(129.13779));  // 129C
                    reporterIons.Add(new ReporterIonInfo(130.134825)); // 130N
                    reporterIons.Add(new ReporterIonInfo(130.141145)); // 130C
                    reporterIons.Add(new ReporterIonInfo(131.13818));  // 131N
                    reporterIons.Add(new ReporterIonInfo(131.144499)); // 131C
                    reporterIons.Add(new ReporterIonInfo(132.141535)); // 132N
                    reporterIons.Add(new ReporterIonInfo(132.147855)); // 132C
                    reporterIons.Add(new ReporterIonInfo(133.14489));  // 133N
                    reporterIons.Add(new ReporterIonInfo(133.15121));  // 133C
                    reporterIons.Add(new ReporterIonInfo(134.148245)); // 134N
                    reporterIons.Add(new ReporterIonInfo(134.154565)); // 134C
                    reporterIons.Add(new ReporterIonInfo(135.1516));   // 135N
                    break;

                case ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    // ITRAQ eight-plex Isobaric tags (iTRAQ8), High-Res MS/MS
                    reporterIons.Add(new ReporterIonInfo(113.107873));
                    reporterIons.Add(new ReporterIonInfo(114.111228));
                    reporterIons.Add(new ReporterIonInfo(115.108263));
                    reporterIons.Add(new ReporterIonInfo(116.111618));
                    reporterIons.Add(new ReporterIonInfo(117.114973));
                    reporterIons.Add(new ReporterIonInfo(118.112008));
                    reporterIons.Add(new ReporterIonInfo(119.115363));
                    reporterIons.Add(new ReporterIonInfo(121.122072));
                    break;

                case ReporterIonMassModeConstants.ITraqEightMZLowRes:
                    // ITRAQ eight-plex Isobaric tags (iTRAQ8), Low-Res MS/MS
                    reporterIons.Add(new ReporterIonInfo(113.107873));
                    reporterIons.Add(new ReporterIonInfo(114.111228));
                    reporterIons.Add(new ReporterIonInfo(115.108263));
                    reporterIons.Add(new ReporterIonInfo(116.111618));
                    reporterIons.Add(new ReporterIonInfo(117.114973));
                    reporterIons.Add(new ReporterIonInfo(118.112008));
                    reporterIons.Add(new ReporterIonInfo(119.115363));

                    // This corresponds to immonium ion loss from Phenylalanine (147.06841 - 26.9871 since Immonium is CO minus H)
                    reporterIons.Add(new ReporterIonInfo(120.08131, true));

                    reporterIons.Add(new ReporterIonInfo(121.122072));
                    break;

                case ReporterIonMassModeConstants.PCGalnaz:
                    // ReSharper disable CommentTypo

                    // Custom reporter ions for Josh Alfaro
                    reporterIons.Add(new ReporterIonInfo(204.0871934));     // C8H14NO5
                    reporterIons.Add(new ReporterIonInfo(300.130787));      // C11H18N5O5
                    reporterIons.Add(new ReporterIonInfo(503.2101566));     // C19H31N6O10
                    break;

                case ReporterIonMassModeConstants.HemeCFragment:
                    // Custom reporter ions for Eric Merkley
                    reporterIons.Add(new ReporterIonInfo(616.1767));
                    reporterIons.Add(new ReporterIonInfo(617.1845));
                    break;

                case ReporterIonMassModeConstants.LycAcetFragment:
                    // Custom reporter ions for Ernesto Nakayasu
                    reporterIons.Add(new ReporterIonInfo(126.09134));
                    reporterIons.Add(new ReporterIonInfo(127.094695));
                    break;

                case ReporterIonMassModeConstants.OGlcNAc:
                    // O-GlcNAc
                    reporterIons.Add(new ReporterIonInfo(204.0872));
                    reporterIons.Add(new ReporterIonInfo(300.13079));
                    reporterIons.Add(new ReporterIonInfo(503.21017));
                    break;

                case ReporterIonMassModeConstants.FrackingAmine20160217:
                    // Product ions associated with FrackingFluid_amine_1_02172016
                    reporterIons.Add(new ReporterIonInfo(157.089));
                    reporterIons.Add(new ReporterIonInfo(170.097));
                    reporterIons.Add(new ReporterIonInfo(234.059));
                    break;

                case ReporterIonMassModeConstants.FSFACustomCarbonyl:
                    // Custom product ions from Chengdong Xu
                    reporterIons.Add(new ReporterIonInfo(171.104));
                    reporterIons.Add(new ReporterIonInfo(236.074));
                    reporterIons.Add(new ReporterIonInfo(257.088));
                    break;

                case ReporterIonMassModeConstants.FSFACustomCarboxylic:
                    // Custom product ions from Chengdong Xu
                    reporterIons.Add(new ReporterIonInfo(171.104));
                    reporterIons.Add(new ReporterIonInfo(234.058));
                    reporterIons.Add(new ReporterIonInfo(336.174));
                    break;

                case ReporterIonMassModeConstants.FSFACustomHydroxyl:
                    // Custom product ions from Chengdong Xu
                    reporterIons.Add(new ReporterIonInfo(151.063));
                    reporterIons.Add(new ReporterIonInfo(166.087));
                    break;

                // ReSharper restore CommentTypo

                case ReporterIonMassModeConstants.Acetylation:
                    // Reporter ions for peptides with acetylated lysine residues
                    reporterIons.Add(new ReporterIonInfo(126.09134));
                    reporterIons.Add(new ReporterIonInfo(143.11789));
                    break;

                case ReporterIonMassModeConstants.NativeOGlcNAc:
                    // Native O-GlcNAc
                    reporterIons.Add(new ReporterIonInfo(126.055));
                    reporterIons.Add(new ReporterIonInfo(138.055));
                    reporterIons.Add(new ReporterIonInfo(144.065));
                    reporterIons.Add(new ReporterIonInfo(168.066));
                    reporterIons.Add(new ReporterIonInfo(186.076));
                    reporterIons.Add(new ReporterIonInfo(204.087));
                    reporterIons.Add(new ReporterIonInfo(366.14));
                    break;

                default:
                    // Includes ReporterIonMassModeConstants.CustomOrNone
                    reporterIons.Clear();
                    break;
            }

            foreach (var reporterIon in reporterIons)
            {
                reporterIon.MZToleranceDa = mzToleranceDa;
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
                // ReSharper disable once StringLiteralTypo
                ReporterIonMassModeConstants.TMTSixteenMZ => "16-plex TMT (aka TMTpro)",
                ReporterIonMassModeConstants.TMTEighteenMZ => "18-plex TMT",
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
            ReporterIonITraq4PlexCorrectionFactorType = ITraqIntensityCorrection.CorrectionFactorsiTRAQ4Plex.ABSciex;

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

                case ReporterIonMassModeConstants.TMTTenMZ or ReporterIonMassModeConstants.TMTElevenMZ or
                     ReporterIonMassModeConstants.TMTSixteenMZ or ReporterIonMassModeConstants.TMTEighteenMZ:
                    SetReporterIonMassMode(reporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_TMT10);
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
            List<ReporterIonInfo> reporterIons,
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
                if (reporterIon.MZToleranceDa < REPORTER_ION_TOLERANCE_DA_MINIMUM)
                {
                    reporterIon.MZToleranceDa = REPORTER_ION_TOLERANCE_DA_MINIMUM;
                }

                ReporterIonList.Add(reporterIon);
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

            foreach (var reporterIonMZ in reporterIonMZList)
            {
                var newReporterIon = new ReporterIonInfo(reporterIonMZ)
                {
                    MZToleranceDa = mzToleranceDa
                };

                ReporterIonList.Add(newReporterIon);
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

            MZIntensityFilterIgnoreRangeStart = ReporterIonList[0].MZ - ReporterIonList[0].MZToleranceDa * 2;
            MZIntensityFilterIgnoreRangeEnd = ReporterIonList[0].MZ + ReporterIonList[0].MZToleranceDa * 2;

            foreach (var reporterIon in ReporterIonList)
            {
                var mzStart = reporterIon.MZ - reporterIon.MZToleranceDa * 2;
                MZIntensityFilterIgnoreRangeStart = Math.Min(MZIntensityFilterIgnoreRangeStart, mzStart);

                var mzEnd = reporterIon.MZ + reporterIon.MZToleranceDa * 2;
                MZIntensityFilterIgnoreRangeEnd = Math.Max(MZIntensityFilterIgnoreRangeEnd, mzEnd);
            }
        }
    }
}
