using System;
using System.Collections.Generic;

namespace MASIC
{
    public class clsReporterIons
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: acet, acetylated, Alfaro, Amine, Carboxylic, Chengdong, Da, Du, Fracking
        // Ignore Spelling: immonium, Lys, Merkley, Nakayasu, plex, Xu

        // ReSharper restore CommentTypo

        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT = 0.5;
        public const double REPORTER_ION_TOLERANCE_DA_MINIMUM = 0.001;

        public const double REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES = 0.015;

        public enum ReporterIonMassModeConstants
        {
            CustomOrNone = 0,
            ITraqFourMZ = 1,
            ITraqETDThreeMZ = 2,
            TMTTwoMZ = 3,
            TMTSixMZ = 4,
            ITraqEightMZHighRes = 5,     // This version of 8-plex iTraq should be used when the reporter ion search tolerance is +/-0.03 Da or smaller
            ITraqEightMZLowRes = 6,      // This version of 8-plex iTraq will account for immonium loss from phenylalanine
            PCGalnaz = 7,
            HemeCFragment = 8,
            LycAcetFragment = 9,
            TMTTenMZ = 10,               // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da
            OGlcNAc = 11,
            FrackingAmine20160217 = 12,
            FSFACustomCarbonyl = 13,
            FSFACustomCarboxylic = 14,
            FSFACustomHydroxyl = 15,
            TMTElevenMZ = 16,
            Acetylation = 17,
            TMTSixteenMZ = 18,
            NativeOGlcNAc = 19
        }

        private double mReporterIonToleranceDaDefault;

        private ReporterIonMassModeConstants mReporterIonMassMode;

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

        public List<clsReporterIonInfo> ReporterIonList { get; }

        public bool ReporterIonStatsEnabled { get; set; }

        public bool ReporterIonApplyAbundanceCorrection { get; set; }

        public clsITraqIntensityCorrection.CorrectionFactorsiTRAQ4Plex ReporterIonITraq4PlexCorrectionFactorType { get; set; }

        /// <summary>
        /// This is ignored if mReporterIonApplyAbundanceCorrection is False
        /// </summary>
        public bool ReporterIonSaveUncorrectedIntensities { get; set; }

        public ReporterIonMassModeConstants ReporterIonMassMode
        {
            get => mReporterIonMassMode;
            set => SetReporterIonMassMode(value);
        }

        /// <summary>
        /// When true, observed m/z values of the reporter ions will be included in the _ReporterIons.txt file
        /// </summary>
        public bool ReporterIonSaveObservedMasses { get; set; }

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
        /// Constructor
        /// </summary>
        public clsReporterIons()
        {
            ReporterIonList = new List<clsReporterIonInfo>();
            InitializeReporterIonInfo();
        }

        public static List<clsReporterIonInfo> GetDefaultReporterIons(ReporterIonMassModeConstants eReporterIonMassMode)
        {
            if (eReporterIonMassMode == ReporterIonMassModeConstants.ITraqEightMZHighRes)
            {
                return GetDefaultReporterIons(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES);
            }
            else
            {
                return GetDefaultReporterIons(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT);
            }
        }

        public static List<clsReporterIonInfo> GetDefaultReporterIons(
            ReporterIonMassModeConstants eReporterIonMassMode,
            double mzToleranceDa)
        {
            var reporterIons = new List<clsReporterIonInfo>(16); // largest count is 16

            switch (eReporterIonMassMode)
            {
                case ReporterIonMassModeConstants.ITraqFourMZ:
                    // ITRAQ, aka iTRAQ4
                    reporterIons.Add(new clsReporterIonInfo(114.1112));
                    reporterIons.Add(new clsReporterIonInfo(115.1083));
                    reporterIons.Add(new clsReporterIonInfo(116.1116));
                    reporterIons.Add(new clsReporterIonInfo(117.115));
                    break;

                case ReporterIonMassModeConstants.ITraqETDThreeMZ:
                    // ITRAQ ETD tags
                    reporterIons.Add(new clsReporterIonInfo(101.107));
                    reporterIons.Add(new clsReporterIonInfo(102.104));
                    reporterIons.Add(new clsReporterIonInfo(104.1107));
                    break;

                case ReporterIonMassModeConstants.TMTTwoMZ:
                    // TMT duplex Isobaric tags (from Thermo)
                    reporterIons.Add(new clsReporterIonInfo(126.1283));
                    reporterIons.Add(new clsReporterIonInfo(127.1316));
                    break;

                case ReporterIonMassModeConstants.TMTSixMZ:
                    // TMT 6-plex Isobaric tags (from Thermo), aka TMT6
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // ' Old values:
                    reporterIons.Add(new clsReporterIonInfo(126.127725));        // 126.1283
                    reporterIons.Add(new clsReporterIonInfo(127.12476));         // 127.1316
                    reporterIons.Add(new clsReporterIonInfo(128.134433));        // 128.135
                    reporterIons.Add(new clsReporterIonInfo(129.131468));        // 129.1383
                    reporterIons.Add(new clsReporterIonInfo(130.141141));        // 130.1417
                    reporterIons.Add(new clsReporterIonInfo(131.138176));        // 131.1387
                    break;

                case ReporterIonMassModeConstants.TMTTenMZ:
                    // TMT 10-plex Isobaric tags (from Thermo), aka TMT10
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new clsReporterIonInfo(126.127726));
                    reporterIons.Add(new clsReporterIonInfo(127.124761));        // 127N
                    reporterIons.Add(new clsReporterIonInfo(127.131081));        // 127C
                    reporterIons.Add(new clsReporterIonInfo(128.128116));        // 128N
                    reporterIons.Add(new clsReporterIonInfo(128.134436));        // 128C
                    reporterIons.Add(new clsReporterIonInfo(129.131471));        // 129N
                    reporterIons.Add(new clsReporterIonInfo(129.137790));        // 129C
                    reporterIons.Add(new clsReporterIonInfo(130.134825));        // 130N
                    reporterIons.Add(new clsReporterIonInfo(130.141145));        // 130C
                    reporterIons.Add(new clsReporterIonInfo(131.138180));        // 131N
                    break;

                case ReporterIonMassModeConstants.TMTElevenMZ:
                    // TMT 11-plex Isobaric tags (from Thermo), aka TMT11
                    // These mass values are for HCD spectra; ETD spectra are exactly 12 Da lighter
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new clsReporterIonInfo(126.127726));        //
                    reporterIons.Add(new clsReporterIonInfo(127.124761));        // 127N
                    reporterIons.Add(new clsReporterIonInfo(127.131081));        // 127C
                    reporterIons.Add(new clsReporterIonInfo(128.128116));        // 128N
                    reporterIons.Add(new clsReporterIonInfo(128.134436));        // 128C
                    reporterIons.Add(new clsReporterIonInfo(129.131471));        // 129N
                    reporterIons.Add(new clsReporterIonInfo(129.137790));        // 129C
                    reporterIons.Add(new clsReporterIonInfo(130.134825));        // 130N
                    reporterIons.Add(new clsReporterIonInfo(130.141145));        // 130C
                    reporterIons.Add(new clsReporterIonInfo(131.138180));        // 131N
                    reporterIons.Add(new clsReporterIonInfo(131.144499));        // 131C
                    break;

                case ReporterIonMassModeConstants.TMTSixteenMZ:
                    // ReSharper disable once CommentTypo
                    // TMT 16-plex Isobaric tags (from Thermo), aka TMT16 or TMTpro
                    // Several of the reporter ion masses are just 49 ppm apart, thus you must use a very tight tolerance of +/-0.003 Da (which is +/-23 ppm)
                    reporterIons.Add(new clsReporterIonInfo(126.127726));        //
                    reporterIons.Add(new clsReporterIonInfo(127.124761));        // 127N
                    reporterIons.Add(new clsReporterIonInfo(127.131081));        // 127C
                    reporterIons.Add(new clsReporterIonInfo(128.128116));        // 128N
                    reporterIons.Add(new clsReporterIonInfo(128.134436));        // 128C
                    reporterIons.Add(new clsReporterIonInfo(129.131471));        // 129N
                    reporterIons.Add(new clsReporterIonInfo(129.13779));         // 129C
                    reporterIons.Add(new clsReporterIonInfo(130.134825));        // 130N
                    reporterIons.Add(new clsReporterIonInfo(130.141145));        // 130C
                    reporterIons.Add(new clsReporterIonInfo(131.13818));         // 131N
                    reporterIons.Add(new clsReporterIonInfo(131.144499));        // 131C
                    reporterIons.Add(new clsReporterIonInfo(132.141535));        // 132N
                    reporterIons.Add(new clsReporterIonInfo(132.147855));        // 132C
                    reporterIons.Add(new clsReporterIonInfo(133.14489));         // 133N
                    reporterIons.Add(new clsReporterIonInfo(133.15121));         // 133C
                    reporterIons.Add(new clsReporterIonInfo(134.148245));        // 134N
                    break;

                case ReporterIonMassModeConstants.ITraqEightMZHighRes:
                    // ITRAQ eight-plex Isobaric tags (iTRAQ8), High-Res MS/MS
                    reporterIons.Add(new clsReporterIonInfo(113.107873));
                    reporterIons.Add(new clsReporterIonInfo(114.111228));
                    reporterIons.Add(new clsReporterIonInfo(115.108263));
                    reporterIons.Add(new clsReporterIonInfo(116.111618));
                    reporterIons.Add(new clsReporterIonInfo(117.114973));
                    reporterIons.Add(new clsReporterIonInfo(118.112008));
                    reporterIons.Add(new clsReporterIonInfo(119.115363));
                    reporterIons.Add(new clsReporterIonInfo(121.122072));
                    break;

                case ReporterIonMassModeConstants.ITraqEightMZLowRes:
                    // ITRAQ eight-plex Isobaric tags (iTRAQ8), Low-Res MS/MS
                    reporterIons.Add(new clsReporterIonInfo(113.107873));
                    reporterIons.Add(new clsReporterIonInfo(114.111228));
                    reporterIons.Add(new clsReporterIonInfo(115.108263));
                    reporterIons.Add(new clsReporterIonInfo(116.111618));
                    reporterIons.Add(new clsReporterIonInfo(117.114973));
                    reporterIons.Add(new clsReporterIonInfo(118.112008));
                    reporterIons.Add(new clsReporterIonInfo(119.115363));

                    // This corresponds to immonium ion loss from Phenylalanine (147.06841 - 26.9871 since Immonium is CO minus H)
                    reporterIons.Add(new clsReporterIonInfo(120.08131, true));

                    reporterIons.Add(new clsReporterIonInfo(121.122072));
                    break;

                case ReporterIonMassModeConstants.PCGalnaz:
                    // ReSharper disable CommentTypo

                    // Custom reporter ions for Josh Alfaro
                    reporterIons.Add(new clsReporterIonInfo(204.0871934));     // C8H14NO5
                    reporterIons.Add(new clsReporterIonInfo(300.130787));      // C11H18N5O5
                    reporterIons.Add(new clsReporterIonInfo(503.2101566));     // C19H31N6O10
                    break;

                case ReporterIonMassModeConstants.HemeCFragment:
                    // Custom reporter ions for Eric Merkley
                    reporterIons.Add(new clsReporterIonInfo(616.1767));
                    reporterIons.Add(new clsReporterIonInfo(617.1845));
                    break;

                case ReporterIonMassModeConstants.LycAcetFragment:
                    // Custom reporter ions for Ernesto Nakayasu
                    reporterIons.Add(new clsReporterIonInfo(126.09134));
                    reporterIons.Add(new clsReporterIonInfo(127.094695));
                    break;

                case ReporterIonMassModeConstants.OGlcNAc:
                    // O-GlcNAc
                    reporterIons.Add(new clsReporterIonInfo(204.0872));
                    reporterIons.Add(new clsReporterIonInfo(300.13079));
                    reporterIons.Add(new clsReporterIonInfo(503.21017));
                    break;

                case ReporterIonMassModeConstants.FrackingAmine20160217:
                    // Product ions associated with FrackingFluid_amine_1_02172016
                    reporterIons.Add(new clsReporterIonInfo(157.089));
                    reporterIons.Add(new clsReporterIonInfo(170.097));
                    reporterIons.Add(new clsReporterIonInfo(234.059));
                    break;

                case ReporterIonMassModeConstants.FSFACustomCarbonyl:
                    // Custom product ions from Chengdong Xu
                    reporterIons.Add(new clsReporterIonInfo(171.104));
                    reporterIons.Add(new clsReporterIonInfo(236.074));
                    reporterIons.Add(new clsReporterIonInfo(257.088));
                    break;

                case ReporterIonMassModeConstants.FSFACustomCarboxylic:
                    // Custom product ions from Chengdong Xu
                    reporterIons.Add(new clsReporterIonInfo(171.104));
                    reporterIons.Add(new clsReporterIonInfo(234.058));
                    reporterIons.Add(new clsReporterIonInfo(336.174));
                    break;

                case ReporterIonMassModeConstants.FSFACustomHydroxyl:
                    // Custom product ions from Chengdong Xu
                    reporterIons.Add(new clsReporterIonInfo(151.063));
                    reporterIons.Add(new clsReporterIonInfo(166.087));
                    break;

                // ReSharper restore CommentTypo

                case ReporterIonMassModeConstants.Acetylation:
                    // Reporter ions for peptides with acetylated lysine residues
                    reporterIons.Add(new clsReporterIonInfo(126.09134));
                    reporterIons.Add(new clsReporterIonInfo(143.11789));
                    break;

                case ReporterIonMassModeConstants.NativeOGlcNAc:
                    // Native O-GlcNAc
                    reporterIons.Add(new clsReporterIonInfo(126.055));
                    reporterIons.Add(new clsReporterIonInfo(138.055));
                    reporterIons.Add(new clsReporterIonInfo(144.065));
                    reporterIons.Add(new clsReporterIonInfo(168.066));
                    reporterIons.Add(new clsReporterIonInfo(186.076));
                    reporterIons.Add(new clsReporterIonInfo(204.087));
                    reporterIons.Add(new clsReporterIonInfo(366.14));
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

        public static string GetReporterIonModeDescription(ReporterIonMassModeConstants eReporterIonMode)
        {
            return eReporterIonMode switch
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
                _ => "Unknown mode",
            };
        }

        private void InitializeReporterIonInfo()
        {
            ReporterIonList.Clear();

            SetReporterIonMassMode(ReporterIonMassModeConstants.CustomOrNone);

            ReporterIonToleranceDaDefault = REPORTER_ION_TOLERANCE_DA_DEFAULT;
            ReporterIonApplyAbundanceCorrection = true;
            ReporterIonITraq4PlexCorrectionFactorType = clsITraqIntensityCorrection.CorrectionFactorsiTRAQ4Plex.ABSciex;

            ReporterIonSaveObservedMasses = false;
            ReporterIonSaveUncorrectedIntensities = false;
        }

        public void SetReporterIonMassMode(ReporterIonMassModeConstants eReporterIonMassMode)
        {
            if (eReporterIonMassMode == ReporterIonMassModeConstants.ITraqEightMZHighRes)
            {
                SetReporterIonMassMode(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT_ITRAQ8_HIGH_RES);
            }
            else
            {
                SetReporterIonMassMode(eReporterIonMassMode, REPORTER_ION_TOLERANCE_DA_DEFAULT);
            }
        }

        public void SetReporterIonMassMode(
            ReporterIonMassModeConstants eReporterIonMassMode,
            double mzToleranceDa)
        {
            // Note: If eReporterIonMassMode = ReporterIonMassModeConstants.CustomOrNone then nothing is changed

            if (eReporterIonMassMode != ReporterIonMassModeConstants.CustomOrNone)
            {
                ReporterIonToleranceDaDefault = mzToleranceDa;

                var reporterIonInfo = GetDefaultReporterIons(eReporterIonMassMode, mzToleranceDa);

                SetReporterIons(reporterIonInfo, false);
                mReporterIonMassMode = eReporterIonMassMode;
            }
        }

        public void SetReporterIons(
            List<clsReporterIonInfo> reporterIons,
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
        /// <param name="reporterIonMZList"></param>
        /// <remarks>Will use REPORTER_ION_TOLERANCE_DA_DEFAULT for the search tolerance</remarks>
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
        /// <param name="reporterIonMZList"></param>
        /// <param name="mzToleranceDa">Search tolerance (half width); must be 0.001 or larger</param>
        /// <param name="customReporterIons">True if these are custom reporter ions</param>
        /// <remarks>When customReportIons is true, sets mReporterIonMassMode to ReporterIonMassModeConstants.CustomOrNone</remarks>
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
                var newReporterIon = new clsReporterIonInfo(reporterIonMZ)
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
